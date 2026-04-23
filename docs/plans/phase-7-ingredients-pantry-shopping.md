# Phase 7 – Ingredient DB + Pantry (Skafferi) + Shopping list (Köplista)

Stor fas. Lyfter `Ingredient` till första­klassentitet med globalt system­register + per-user custom. Lägger till Skafferi och Köplista per workspace.

## Steps

### 7.1 Domän­omstrukturering: Ingredient

**Idag**: `Ingredient { Id, Name, MealId }` — varje meal äger sina egna ingredient-rader. Det skapar dubletter.

**Ny modell:**

- `Ingredient` (master):
  - `Id` (Guid)
  - `Name` (string, 100, unique inom (`OwnerUserId`, `Name`))
  - `OwnerUserId` (Guid?, null = system)
  - `IsSystem` (bool, computed `OwnerUserId == null`)
  - `Category` (enum, valfritt: `Produce, Dairy, Meat, Pantry, ...` för framtida sortering)
  - `CreatedAt`
- `MealIngredient` (join):
  - `MealId`, `IngredientId` (composite PK)
  - `Quantity` (string?, t.ex. "2 dl" — fritt textfält i denna fas)
- `PantryItem` (skafferi, scoped per workspace):
  - `Id`, `WorkspaceId`, `IngredientId`, `AddedAt`, `UpdatedAt`
  - Composite unique: (WorkspaceId, IngredientId)
- `ShoppingListItem` (köplista, scoped per workspace):
  - `Id`, `WorkspaceId`, `IngredientId`, `Note` (t.ex. "Tacos v17"), `AddedAt`, `CheckedAt` (nullable)
  - Composite unique: (WorkspaceId, IngredientId) — checking flyttar till pantry, removing tar bort.

### 7.2 Migration & seed

- Drop & re-seed (eller stor migration som bygger upp ingredient-master från befintliga `Ingredient.Name`-värden).
- Ny seed: ~200 vanliga svenska livsmedel som system-ingredienser. Fil: `src/Eatah.Infrastructure/Persistence/Seed/SystemIngredients.cs` med kategoriindelad lista.

### 7.3 Ingredient-feature

`src/Eatah.Api/Features/Ingredients/`:

- `SearchIngredients.cs` — `GET /api/ingredients/search?q={q}&limit=10` → returnerar matchande (system + user-owned), sorterat: exakt match först, sedan startsWith, sedan contains, system före user.
- `CreateIngredient.cs` — `POST /api/ingredients` body `{ name, category? }` → skapar user-owned, returnerar 200 om redan finns (idempotent på (`OwnerUserId`, `Name`)).
- `DeleteIngredient.cs` — `DELETE /api/ingredients/{id}` → endast user-owned + endast egen. System-ingredienser → 403 `ingredient_system_protected`. Om i bruk i meal/pantry/shopping → 409 `ingredient_in_use`.

### 7.4 Pantry-feature

`src/Eatah.Api/Features/Pantry/`:

- `GetPantry.cs` — `GET /api/pantry` → lista i workspace, sorterat.
- `AddToPantry.cs` — `POST /api/pantry` body `{ ingredientId }` → idempotent.
- `RemoveFromPantry.cs` — `DELETE /api/pantry/{id}`.

### 7.5 Shopping-list-feature

`src/Eatah.Api/Features/Shopping/`:

- `GetShoppingList.cs` — `GET /api/shopping` → uppdelad: `{ pantry: [...], shoppingList: [...] }` (eller två separata endpoints och klienten hämtar båda).
- `AddToShoppingList.cs` — `POST /api/shopping` body `{ ingredientId, note? }` → idempotent (samma ingredient → uppdatera note).
- `CheckShoppingItem.cs` — `POST /api/shopping/{id}/check` → flyttar till pantry, tar bort från shopping (transaktion).
- `RemoveShoppingItem.cs` — `DELETE /api/shopping/{id}` → endast tar bort från shopping (köpte inte).
- `SyncFromWeeklyPlan.cs` — `POST /api/shopping/sync?planId={id}` → går igenom alla meals i planen, samlar ingredient-listor, för varje ingredient som **inte** finns i pantry **och inte** redan på shoppinglistan: lägg till med note `"{MealName} v{WeekNumber}"`. Returnera diff `{ added: [...], skipped: [...] }`.

### 7.6 Auto-sync trigger

- När en meal assignas till en day i `WeeklyPlan` (eller hela veckan randomiseras): triggar `SyncFromWeeklyPlan` automatiskt på server-sidan (i `WeeklyPlanService`). Detta håller köplistan up-to-date utan klient-anrop.
- Diff returneras i `WeeklyPlanResponse` som ny property `ShoppingListChanges?` så UI kan visa toast "5 nya ingredienser lades till på köplistan".

### 7.7 Klient-sidor

- Ny: `src/Eatah.Client/Pages/ShoppingList.razor` — route `/shopping`. Två kolumner side-by-side (mobile: stacked):
  - Vänster: **Skafferi** — lista pantry items med "lade till YYYY-MM-DD" + papperskorg.
  - Höger: **Köplista** — lista shopping items med note + checkbox (check → flytta till skafferi) + papperskorg (ta bort).
  - Sökbar add-input med autocomplete från `SearchIngredients`.
  - Stil: glas-gradient kort som fas 1.
- Nya komponenter: `Components/Shopping/PantryColumn.razor`, `ShoppingColumn.razor`, `IngredientAutocomplete.razor`.
- `IngredientAutocomplete`: textfält + dropdown med träffar; vid Enter på okänd → POST create + add. Skydd mot dubletter genom server-side idempotens.
- Aktivera Köplista-knappen i navbar → navigerar till `/shopping`.
- Uppdatera `src/Eatah.Client/Components/Meals/IngredientInput.razor` att använda samma `IngredientAutocomplete` så meal-skapande också går genom master-tabellen.
- I `IngredientChecklist`-komponenten på Dashboard: visa även status:
  - Om i pantry: "I skafferi (sedan {date})"
  - Om på shopping-listan: "På köplistan"
  - Om ingenstans: "Saknas"

### 7.8 ApiClient

- Lägg till metoder för alla nya endpoints. Strukturera per feature i ApiClient.cs.

### 7.9 Felkoder

- `ingredient_not_found`, `ingredient_system_protected`, `ingredient_in_use`, `ingredient_name_required`
- `pantry_item_not_found`, `pantry_item_already_exists`
- `shopping_item_not_found`

### 7.10 Tester

- Unit: `IngredientService.SearchAsync` ranking, dubblett­skydd. `ShoppingService.SyncFromWeeklyPlan` (skip om i pantry/på listan).
- Integration: full flow — assign meal → shopping list auto-fyllt → check → flyttat till pantry → re-sync skippar.

## Relevant files

- Nya entiteter
- Refaktor av `Meal` ↔ `Ingredient` relation (join-table `MealIngredient`)
- Migration (stor)
- Nya feature-mappar `Ingredients`, `Pantry`, `Shopping`
- Klient-sidan + komponenter + autocomplete
- Aktivera Köplista-knappen

## Verification

1. Söka "tom" → får "Tomater", "Tomatpuré" etc. från system.
2. Skapa custom "Min hemmagjorda BBQ-sås" → läggs som user-owned.
3. Försöka radera system-ingrediens → 403.
4. Assigna meal till dag → ingredients som saknas i pantry hamnar på shopping list.
5. Check shopping item → flyttas till pantry med dagens datum.
6. Status visas i Dashboard ingredient-checklist.
7. User i Personal ser inte Householdets pantry/shopping.

## Decisions

- **Master-ingredient-tabell** med globalt + per-user scope. Dubblettskydd via unique index på (`OwnerUserId`, lower(`Name`)).
- **Quantity som fritext** i `MealIngredient` — strukturerad mängd/enhet skjuts upp.
- **Auto-sync från weekly plan** — sker server-side automatiskt vid plan-mutation. Klienten behöver inte trigga.
- **Pantry & shopping per workspace** (inte per user) → matchar att Household delar.

## Further Considerations

1. **Migration-strategin** — om vi behåller seedad data från fas 0/4, måste befintliga `Ingredient`-rader splittas till master + join. Pre-launch → enklare med drop & re-seed. Rekommendation: drop.
2. **Synkning vid clear/randomize** — när dag clearas, ska shopping list rensas från ingredients vars enda referens var den dagen? Beslut: nej, det skapar surprise-borttaganden. Användaren måste manuellt ta bort. Alternativt: lägg till "auto-rensa orphans"-knapp i UI.
3. **Förslag: en separat tabell `IngredientNameAlias`** för dubblettskydd ("morötter" ↔ "morot"). Skjuts upp till v2.
