# Phase 4 – Workspaces (Personal + max 1 Household)

Inför multi-tenancy: data scopas per workspace. Varje user har alltid 1 Personal workspace, och kan ha max 1 Household som delas med en eller flera foodbuddies.

## Steps

### 4.1 Domän­modell

Nya entiteter i `src/Eatah.Domain/Entities/`:

- `Workspace`:
  - `Id` (Guid)
  - `Name` (string, 100) — t.ex. "Personligt", "Hushållet Karlsson"
  - `Type` (enum `WorkspaceType { Personal, Household }`)
  - `CreatedAt`
  - `Members` (List<WorkspaceMember>)
- `WorkspaceMember`:
  - `WorkspaceId` (Guid, FK)
  - `UserId` (Guid, FK till `EatahUser`)
  - `Role` (enum `MemberRole { Owner, Member }`)
  - `JoinedAt`
  - Composite PK (WorkspaceId, UserId).

Befintliga entiteter får ny property:

- `WeeklyPlan.WorkspaceId` (Guid, FK)
- `DietProfile.WorkspaceId` (Guid?, FK) — `null` = system-profil (Livsmedelsverket) synlig för alla
- `Meal.WorkspaceId` (Guid?, FK) — `null` = system-mealmall (seeded, alla ser); user-skapade meals scopas

**Invariant**: en användare kan ha exakt 1 Personal-workspace (skapas vid email-confirm) + max 1 Household.

### 4.2 Workspace-feature

`src/Eatah.Api/Features/Workspaces/`:

- Endpoints under `/api/workspaces`:
  - `GetMyWorkspaces.cs` — `GET /` → returnerar Personal + ev. Household för inloggad user.
  - `CreateHousehold.cs` — `POST /household` body `{ name }` → skapar Household, gör usern till Owner. Felkod `workspace_household_already_exists` om hen redan har ett.
  - `LeaveHousehold.cs` — `DELETE /household` → tar bort user från Household; om sista Member → ta bort hela Household.
  - `RenameWorkspace.cs` — `PATCH /{id}` body `{ name }`.
- Service `WorkspaceService` + Repository.
- DTOs: `WorkspaceResponse(Id, Name, Type, MemberCount, IsOwner)`.

### 4.3 WorkspaceContext (request-scoped)

- `src/Eatah.Api/Common/WorkspaceContext.cs`:
  - Scoped DI service.
  - Property `Guid CurrentWorkspaceId`.
  - Sätts av middleware `WorkspaceResolutionMiddleware` baserat på header `X-Eatah-Workspace: {guid}` eller fallback till users Personal.
  - Validering: middleware kollar att inloggad user är member i requested workspace, annars 403 `workspace_access_denied`.
- Klienten skickar headern via `WorkspaceHttpMessageHandler` (ny i `Services/`).

### 4.4 Global query filter

- I `EatahDbContext.OnModelCreating`:
  - `modelBuilder.Entity<WeeklyPlan>().HasQueryFilter(p => p.WorkspaceId == _workspaceContext.CurrentWorkspaceId);`
  - Samma för `DietProfile` (men inkludera system: `p.WorkspaceId == null || p.WorkspaceId == ctx.CurrentWorkspaceId`).
  - Samma för `Meal`.
- DbContext får `WorkspaceContext` via DI i konstruktor.
- **Risk**: query filter på `null = system` är subtil — täck med tester.

### 4.5 Datamigration

- Ny migration `AddWorkspaces`:
  - Skapar `workspaces`, `workspace_members`.
  - Lägger till `workspace_id`-kolumner.
  - **Pre-launch → drop & re-seed** (ingen back-fill).
- Uppdatera `DataSeeder`:
  - Skapar dev-user → Personal workspace → seedar meals (med workspaceId), Livsmedelsverket-profil (workspaceId=null).

### 4.6 Befintliga services

- `MealService.GetAllAsync` etc. — ingen ändring, query filter sköter scopningen.
- `WeeklyPlanService.GetCurrentAsync` — vid create sätt `WorkspaceId = workspaceContext.CurrentWorkspaceId`.
- `DietRuleService` — vid AI-generering: sätt `WorkspaceId = currentWorkspace`. System-profiler (`null`) skapas endast av seeder/admin.
- `RandomMealGenerator` — meals kommer redan filtrerade. System-meals + workspace-meals slås ihop automatiskt.

### 4.7 Klient: workspace-switcher

- `src/Eatah.Client/Services/WorkspaceState.cs`:
  - Properties: `IReadOnlyList<WorkspaceResponse> Workspaces`, `WorkspaceResponse Current`, `event OnChange`.
  - Metoder: `LoadAsync()`, `SwitchAsync(id)` (sparar i `Preferences`).
  - Vid switch: skicka events så alla sidor reladdar data.
- `WorkspaceHttpMessageHandler` lägger till `X-Eatah-Workspace`-header från `WorkspaceState.Current.Id`.
- `src/Eatah.Client/Components/Shared/WorkspaceSwitcher.razor`:
  - Renderar dropdown i headerns vänsterslot.
  - Items: "Personligt" + ev. "Hushållet" + "Skapa hushåll..." (sista öppnar modal `CreateHouseholdModal`).
- Dashboard reagerar på `WorkspaceState.OnChange` → om-laddar plan + profile.

### 4.8 Felkoder

- `workspace_not_found`, `workspace_access_denied`, `workspace_household_already_exists`, `workspace_personal_protected` (kan inte raderas).

### 4.9 Tester

- Unit: `WorkspaceService` (skapa Personal vid registrering, single-Household-invariant).
- Integration: `WorkspaceEndpointsTests`. Också uppdatera **alla befintliga endpoint-tester** så de skapar user + workspace i Arrange-fasen och skickar header.
- Query filter-test: skapa 2 users, 2 workspaces, säkerställ att `GET /api/meals` med olika headers returnerar olika dataset.

## Relevant files

- Nya: `src/Eatah.Domain/Entities/{Workspace,WorkspaceMember,WorkspaceType,MemberRole}.cs`
- Nya: hela `src/Eatah.Api/Features/Workspaces/`
- Ny: `src/Eatah.Api/Common/WorkspaceContext.cs` + `Middleware/WorkspaceResolutionMiddleware.cs`
- Ändras: `src/Eatah.Domain/Entities/Meal.cs`, `WeeklyPlan.cs`, `DietProfile.cs` — `WorkspaceId`
- Ändras: `src/Eatah.Infrastructure/Persistence/EatahDbContext.cs` — query filters, ctor injection
- Ändras: alla Configurations under `src/Eatah.Infrastructure/Persistence/Configurations/`
- Ändras: alla service-klasser i features (sätt WorkspaceId vid create)
- Ändras: `src/Eatah.Infrastructure/Persistence/DataSeeder.cs`
- Ny migration
- Klient: `Services/WorkspaceState.cs`, `Services/WorkspaceHttpMessageHandler.cs`, `Components/Shared/WorkspaceSwitcher.razor`, `Components/Workspace/CreateHouseholdModal.razor`
- Header: aktivera vänsterslot med `WorkspaceSwitcher`

## Verification

1. Migration kör rent från 0.
2. Skapa 2 users; varje får automatiskt Personal vid registreringens confirm-steg (utöka `ConfirmEmailAndSetCredentials` att skapa Personal workspace).
3. Switcher visar bara "Personligt" tills Household skapas.
4. Skapa Household → switcher visar 2 alternativ. Försök skapa till → 409 `workspace_household_already_exists`.
5. Switch i UI → `X-Eatah-Workspace` ändras → Dashboard laddar data från det workspace.
6. User A kan inte se User B:s workspaces (403 om hen försöker).
7. System-profil (Livsmedelsverket) syns i båda workspaces.
8. Alla befintliga integration-tester passerar efter uppdatering med auth+workspace setup.

## Decisions

- **Max 1 Household** (förenklat per användarbeslut). Schema stöder N (många-till-många via `WorkspaceMember`) men service-lagret enforcar 1.
- **Personal workspace kan inte raderas** — skapas vid registrering, lever med kontot.
- **Household namn**: defaultar till "Hushåll" om inget anges.
- **System-data via `WorkspaceId = NULL`** är synligt för alla (query filter inkluderar null).
- **Header-baserad workspace** (inte URL-baserad) håller endpoint-strukturen oförändrad.

## Further Considerations

1. **WeeklyPlan unique constraint** — idag finns plan unique på (Year, WeekNumber). Måste bli (WorkspaceId, Year, WeekNumber). Uppdatera config + repository-`UpsertCurrentAsync`-logik.
2. **Migration vid foodbuddy-merge (fas 5)** — när någon joinar ett Household, vad händer med deras Personal-data? Beslut: Personal förblir privat, Household har egen data. Inget merge.
3. **Cache av query filter** — DbContext är scoped, så `_workspaceContext` läses en gång per request. OK.
