# Phase 2 – Diet Profile som global modal

Ersätter DietProfiles-sidan med en global popup som triggas från **Kost**-knappen i navbar (eller från CTA i Phase 1-kortet). Stilen ska matcha "Ingredienslistan" i appen (befintlig komponent som vi mimar — verifiera vid implementation).

## Steps

### 2.1 Ny modal-komponent

`src/Eatah.Client/Components/DietRules/DietProfileModal.razor` (extends `EatahModalBase`).

**Innehåll, top-down:**

1. **Modal-header**: rubrik "Kostprofiler", stäng-knapp (X) uppe i hörnet.
2. **Förklaringssektion** (kort, 2–3 meningar svenska): "En kostprofil beskriver hur du vill balansera olika proteinkällor under en vecka. AI hjälper dig generera en profil utifrån ett mål."
3. **Generera-formulär**:
   - Input: `Namn` (textfält, validerad: min 2 max 100 tecken)
   - Textarea: `Beskrivning / Mål` (max 500 tecken)
   - Knapp: "Generera profil" → `ApiClient.GenerateDietProfileAsync(req)` (befintligt endpoint).
   - Loading-state under generering, fel visas inline.
4. **Lista över profiler**:
   - Varje rad = `<DietProfileRow Profile="@p" Expanded="@(expandedId == p.Id)" OnExpand="..." OnDelete="..." />`.
   - Rad kollapsad: namn + chevron + papperskorg.
   - Klick på rad/chevron → expanderar **endast en åt gången** (sätt `expandedId`).
   - Expanderad: visar `Description` + lista av `Rules` med kategori-ikon (`<Icon>`) + "Rött kött: 1–2 ggr/v" formaterat.
   - Papperskorg → bekräftelsedialog (mini-confirm, inte full modal — t.ex. swipe-style röd bekräftelse) → `ApiClient.DeleteDietProfileAsync(id)`.
5. **Scroll-beteende**: modal-höjd max `90vh`. Inre lista scrollar om antalet profiler överskrider plats. Generera-formuläret är sticky upptill (eller alltid synligt).

### 2.2 Nya endpoints

- `POST /api/dietprofiles` — skapa (om inte redan finns för manuell skapning utan AI). Idag finns endast `POST /api/dietprofiles/generate`. För denna fas räcker `generate`-endpointen som skapande, men:
- `DELETE /api/dietprofiles/{id}` — **ny** endpoint, behöver implementeras:
  - Handler: `DeleteDietProfile.Handle` (one file per endpoint)
  - Service: `DietRuleService.DeleteAsync(Guid id, CancellationToken ct)` returnerar `Result`
  - Repo: `DietProfileRepository.DeleteAsync(Guid id, CancellationToken ct)`
  - Kontroll: får man radera en profil som används i en pågående evaluering? Plan: tillåt alltid; evalueringar är stateless. Inga FK på profil från `WeeklyPlan` så ingen kaskad behövs.
  - Felkod: ny `DietProfileNotFound` (finns redan).
- `ErrorCodes` + `ApiErrorCodes`: ingen ny kod behövs.

### 2.3 Pensionera DietProfiles-sidan

- `src/Eatah.Client/Pages/DietProfiles.razor` — **ta bort** filen.
- Inga route-länkar finns längre kvar (sidobaren togs bort i fas 0).
- "Kost"-knappen i navbar (förberedd som modal-trigger i fas 0) aktiveras: `OpenModal<DietProfileModal>()`.
- Phase 1-kortets "ingen profil"-CTA öppnar samma modal.

### 2.4 ApiClient

- Lägg till `DeleteDietProfileAsync(Guid id, CancellationToken ct)` i `src/Eatah.Client/Services/ApiClient.cs`.
- `GetDietProfilesAsync` finns redan.
- Efter delete/create: trigga ett event (`DietProfileChanged`) som Phase 1-kortet kan lyssna på via en delad service `DietProfileState : INotifyPropertyChanged` eller enklare en `event Action OnChange` → injektion i `DietProfileSelectorCard` så listan reaktivt uppdateras.

### 2.5 Tester

- Unit: `DietRuleServiceTests` — `DeleteAsync_ShouldReturnNotFound_WhenProfileMissing`, `DeleteAsync_ShouldReturnSuccess_WhenProfileExists`.
- Integration: `DietRuleEndpointsTests.Delete_*`.

## Relevant files

- Ny: `src/Eatah.Api/Features/DietRules/DeleteDietProfile.cs`
- `src/Eatah.Api/Features/DietRules/DietRuleEndpoints.cs` — registrera DELETE-route
- `src/Eatah.Api/Features/DietRules/DietRuleService.cs` — `DeleteAsync`
- `src/Eatah.Api/Features/DietRules/DietProfileRepository.cs` — `DeleteAsync`
- Ny: `src/Eatah.Client/Components/DietRules/DietProfileModal.razor` (+ `DietProfileRow.razor`)
- Ny: `src/Eatah.Client/Services/DietProfileState.cs`
- Ta bort: `src/Eatah.Client/Pages/DietProfiles.razor`
- `src/Eatah.Client/Services/ApiClient.cs` — `DeleteDietProfileAsync`
- `src/Eatah.Client/Components/Shared/AppNavbar.razor` — aktivera Kost-knappen

## Verification

1. Klick på "Kost" i navbar öppnar modalen från valfri sida.
2. Generera-formuläret skapar profil via AI och listan uppdateras direkt.
3. Klick på chevron expanderar 1 profil; klick på en annan kollapsar den första automatiskt.
4. Papperskorg → confirm → DELETE-anrop → rad försvinner. Vid 404 visas felmeddelande.
5. Phase 1-kortets dropdown speglar listan i modalen i realtid (via `DietProfileState`).
6. Inga referenser till gammal `/dietprofiles` route kvar.
7. Integration-test för DELETE 200 + 404 grön.

## Decisions

- Profil-skapande sker endast via AI-generering (befintligt). Manuell skapning skjuts upp.
- Edit av profil = ej supportad (användaren skrev "Edit är inte nödvändigt").
- Confirm-dialog för delete är inline i raden, inte en separat modal (modal-i-modal undviks).

## Further Considerations

1. **Default-profil flagga** — Livsmedelsverket-profilen seedas alltid och bör inte gå att ta bort. Lägg till `IsSystem` bool på `DietProfile`-entiteten + DB-migration. Service kontrollerar och returnerar `Error.Conflict("diet_profile_system_protected", "...")`.
2. **Sortering** — system-profiler först, sedan alfabetiskt eller efter skapelsedatum.
