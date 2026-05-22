# Copilot Instructions – Eatah

> Denna fil beskriver projektets arkitektur, konventioner och regler.
> AI-modeller ska alltid konsultera denna fil innan kod genereras.

## Meta-regel – håll denna fil aktuell

**Efter varje kodändring:** kontrollera om ändringen påverkar arkitekturen, konventionerna eller mönstren som beskrivs här. Om så är fallet, uppdatera denna fil som en del av samma ändring. Ingen PR/commit ska introducera ett nytt mönster utan att det dokumenteras här.

---

## Projektöversikt

Eatah är en veckoplaneringsapp för måltider. Användaren tilldelar maträtter till veckodagar, slumpar menyer och får feedback via ett kostregelssystem. Appen byggs med .NET 10, ASP.NET Core Minimal API, PostgreSQL, .NET MAUI Blazor Hybrid och Blazor WebAssembly.

Det finns **två klientprojekt** som delar exakt samma UI-kod:
- `Eatah.Client` – .NET MAUI Blazor Hybrid (iOS/Android/Mac/Windows)
- `Eatah.WebClient` – Blazor WebAssembly, driftas på GitHub Pages (`audioken.github.io/Eatah/`)

**Regel:** Varje ändring i `Eatah.Client` (komponenter, sidor, services, CSS) måste fungera i `Eatah.WebClient` också. Verifiera alltid att båda projekten bygger efter en ändring.

---

## Solution-struktur

```
Eatah.sln
├── src/
│   ├── Eatah.Api/              # ASP.NET Core Minimal API
│   │   ├── Common/             # Delad infrastruktur (Result, Error, ErrorCodes, extensions, ICurrentUser, IWorkspaceContext)
│   │   │   ├── CurrentUser.cs
│   │   │   ├── Error.cs
│   │   │   ├── ErrorCodes.cs
│   │   │   ├── Result.cs
│   │   │   ├── ResultExtensions.cs
│   │   │   ├── ValidationExtensions.cs
│   │   │   └── WorkspaceContext.cs
│   │   ├── Features/
│   │   │   ├── Meals/          # Endpoints, handlers, service, repo, DTOs, validator
│   │   │   │   ├── MealEndpoints.cs      # Slim router + DI extension
│   │   │   │   ├── GetAllMeals.cs        # En fil per endpoint
│   │   │   │   ├── GetMealById.cs
│   │   │   │   ├── CreateMeal.cs
│   │   │   │   ├── UpdateMeal.cs
│   │   │   │   ├── DeleteMeal.cs
│   │   │   │   ├── MealService.cs
│   │   │   │   ├── MealRepository.cs
│   │   │   │   ├── IMealRepository.cs
│   │   │   │   ├── MealDtos.cs
│   │   │   │   └── MealValidator.cs
│   │   │   ├── WeeklyPlan/     # Samma struktur
│   │   │   ├── DietRules/
│   │   │   ├── AI/
│   │   │   ├── Auth/           # ASP.NET Identity-baserade endpoints (cookie eatah.auth)
│   │   │   ├── Workspaces/     # Personal/Household workspaces + membership
│   │   │   ├── Friends/        # Friend requests + foodbuddies via shared households
│   │   │   ├── Notifications/  # Per-user notifikationer (jsonb payload)
│   │   │   ├── Pantry/         # IngredientMaster + PantryItem + ShoppingItem
│   │   │   └── Chat/           # Group-trådar per workspace (SignalR ChatHub för chat + realtidsbroadcast)
│   │   ├── Middleware/         # GlobalExceptionHandler + WorkspaceResolutionMiddleware
│   │   └── Program.cs
│   ├── Eatah.Domain/           # Entiteter, enums, value objects (INGA beroenden)
│   │   └── Entities/
│   ├── Eatah.Infrastructure/   # EF Core, DbContext, konfigurationer, seeding
│   │   └── Persistence/
│   │       ├── Configurations/
│   │       ├── Migrations/
│   │       └── EatahDbContext.cs
│   ├── Eatah.Client/           # .NET MAUI Blazor Hybrid (iOS/Android/Mac/Win)
│   │   ├── Pages/
│   │   ├── Components/
│   │   │   ├── WeeklyPlan/
│   │   │   ├── Meals/
│   │   │   └── DietRules/
│   │   ├── Services/
│   │   └── wwwroot/
│   │       └── css/
│   │           └── app.css     # ENDA CSS-källan (länkas in i WebClient via .csproj)
│   └── Eatah.WebClient/        # Blazor WASM – GitHub Pages (audioken.github.io/Eatah/)
│       ├── Program.cs          # WASM-specifik DI-setup
│       └── wwwroot/
│           ├── index.html
│           └── css/
│               └── app.webclient.css  # WebClient-specifika overrides (laddas efter app.css)
└── tests/
    └── Eatah.Api.Tests/
```

---

## Arkitekturregler

### Feature Slicing

Kod organiseras per feature, INTE per lager. Varje feature-mapp (t.ex. `Meals/`) innehåller:

- `{Feature}Endpoints.cs` – Slim router (endast `MapGet/MapPost/...`) + DI extension `Add{Feature}Feature()`
- `{Action}.cs` (t.ex. `GetAllMeals.cs`) – **En fil per endpoint**, `public static class` med `Handle`-metod
- `{Feature}Service.cs` – Affärslogik; returnerar `Result<T>` / `Result` (ALDRIG kastar domain exceptions)
- `{Feature}Repository.cs` + `I{Feature}Repository.cs` – Dataåtkomst
- `{Feature}Dtos.cs` – Request/response DTOs
- `{Feature}Validator.cs` – FluentValidation-validatorer (felmeddelanden på **engelska**)

**En fil per endpoint är obligatoriskt.** Lägg ALDRIG handler-logik direkt i `{Feature}Endpoints.cs`.

### Beroenden

```
Api → Domain, Infrastructure
Infrastructure → Domain
Domain → (inga beroenden)
Client → (kommunicerar med Api via HTTP)
WebClient → länkar komponenter, sidor, services och CSS från Client via .csproj
```

**Domain-projektet får ALDRIG ha beroenden mot andra projekt eller infrastrukturpaket.**

### Dependency Injection

Alla services och repositories registreras via DI-extension i `{Feature}Endpoints.cs` och anropas från `Program.cs`:

```csharp
// I {Feature}Endpoints.cs:
public static IServiceCollection AddMealFeature(this IServiceCollection services)
{
    services.AddScoped<IMealRepository, MealRepository>();
    services.AddScoped<MealService>();
    services.AddScoped<IValidator<CreateMealRequest>, CreateMealValidator>();
    return services;
}

// I Program.cs:
builder.Services.AddMealFeature();
builder.Services.AddWeeklyPlanFeature();
// osv.
```

---

## Kodkonventioner

### Generellt

- **Språk:** All kod, kommentarer och variabelnamn på **engelska**
- **UI-texter och felmeddelanden:** På **svenska** (användarvisade strängar)
- **C# version:** Senaste stabila (12+)
- **Nullable reference types:** Aktiverat i alla projekt
- **Implicit usings:** Aktiverat

### Namngivning

- Klasser: `PascalCase`
- Metoder: `PascalCase`
- Lokala variabler: `camelCase`
- Privata fält: `_camelCase`
- Interfaces: `I{Name}` (t.ex. `IMealRepository`)
- DTOs: `{Feature}{Action}Request` / `{Feature}{Action}Response`
- Konstanter: `PascalCase`

### Minimal API-mönster

**`{Feature}Endpoints.cs`** – endast routing och DI-registrering:

```csharp
public static class MealEndpoints
{
    public static void MapMealEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/meals")
            .WithTags("Meals")
            .WithOpenApi();

        group.MapGet("/", GetAllMeals.Handle);
        group.MapGet("/{id:guid}", GetMealById.Handle);
        group.MapPost("/", CreateMeal.Handle);
        group.MapPut("/{id:guid}", UpdateMeal.Handle);
        group.MapDelete("/{id:guid}", DeleteMeal.Handle);
    }

    public static IServiceCollection AddMealFeature(this IServiceCollection services)
    {
        services.AddScoped<IMealRepository, MealRepository>();
        services.AddScoped<MealService>();
        services.AddScoped<IValidator<CreateMealRequest>, CreateMealValidator>();
        return services;
    }
}
```

**`{Action}.cs`** – en fil per endpoint, `public static class` med `Handle`:

```csharp
public static class GetAllMeals
{
    public static async Task<IResult> Handle(MealService service, CancellationToken ct)
    {
        var meals = await service.GetAllAsync(ct);
        return Results.Ok(meals);
    }
}
```

```csharp
public static class CreateMeal
{
    public static async Task<IResult> Handle(
        CreateMealRequest request,
        IValidator<CreateMealRequest> validator,
        MealService service,
        CancellationToken ct)
    {
        var validationError = await validator.ValidateRequestAsync(request, ct);
        if (validationError is not null)
            return Result<MealResponse>.Failure(validationError).ToHttpResult();

        var result = await service.CreateAsync(request, ct);
        return result.ToCreatedResult(r => $"/api/meals/{r.Id}");
    }
}
```

### Async/Await

- Alla I/O-operationer ska vara `async`
- Använd `CancellationToken` i alla async-metoder
- Suffixera INTE metoder med `Async` i endpoints (gör det i services/repos)

### DTOs

- Använd `record` för request/response-objekt
- Mappa mellan entiteter och DTOs i service-lagret
- Exponera ALDRIG domänentiteter direkt i API-svar

```csharp
public record CreateMealRequest(string Name, List<string> Ingredients, MealCategory Category);
public record MealResponse(Guid Id, string Name, List<string> Ingredients, MealCategory Category);
```

---

## Databaskonventioner

### Entity Framework Core

- **Approach:** Code First med migrations
- **Provider:** Npgsql (PostgreSQL)
- **Konfiguration:** Fluent API (INTE data annotations på entiteter)
- Tabellnamn: pluralform, snake_case (`meals`, `weekly_plans`, `diet_rules`)
- Kolumnnamn: snake_case (`created_at`, `meal_id`)

```csharp
public class MealConfiguration : IEntityTypeConfiguration<Meal>
{
    public void Configure(EntityTypeBuilder<Meal> builder)
    {
        builder.ToTable("meals");
        builder.HasKey(m => m.Id);
        builder.Property(m => m.Name)
            .HasColumnName("name")
            .IsRequired()
            .HasMaxLength(200);
        builder.Property(m => m.CreatedAt)
            .HasColumnName("created_at")
            .HasDefaultValueSql("NOW()");
    }
}
```

### Migrations

- Namnge med beskrivande namn: `AddMealCategoryColumn`, `CreateWeeklyPlanTable`
- Kör ALDRIG `EnsureCreated()` – använd alltid migrations
- Seed-data läggs i en separat `DataSeeder`-klass

---

## Frontend-konventioner (Blazor)

### Komponentstruktur

- En komponent per fil
- Komponentnamn: `PascalCase.razor`
- Parametrar markeras med `[Parameter]`
- Callbacks: `EventCallback<T>`

### Styling

- **Två lager**:
  - **Tailwind CDN** = layout & utility (flex, grid, padding, typografi-skala, spacing).
  - **`Eatah.Client/wwwroot/css/app.css`** = visuell identitet (färger, gradients, radii, shadows, komponent-form). Definierar **design-tokens** som CSS custom properties under `:root` och **komponentklasser** som `.eatah-card`, `.eatah-navbar`, `.eatah-pill`, `.eatah-category-icon`.
- **Ingen inline styling**. Använd Tailwind för layout, komponentklasser för identitet. Inline `style=""` tillåts endast för dynamiska värden som inte kan uttryckas i CSS (t.ex. `--icon-url` på `<Icon>` eller en beräknad procent-bredd).
- Naming på komponentklasser: `eatah-{component}__{element}--{modifier}` (BEM-light).
- Mobile-first: designa för mobil först, lägg till breakpoints för desktop.
- Konsekvent färgpalett via tokens i `app.css` (kategori­färger, brand, glas-gradient).

### CSS-delning mellan Client och WebClient

`Eatah.Client/wwwroot/css/app.css` är **enda källan för all CSS**. `Eatah.WebClient` har ingen lokal kopia — filen länkas in via `.csproj`:

```xml
<!-- Eatah.WebClient.csproj -->
<Content Include="..\Eatah.Client\wwwroot\css\app.css" Link="wwwroot/css/app.css" />
```

`Eatah.WebClient/wwwroot/css/app.webclient.css` laddas _efter_ `app.css` och används **enbart** för WebClient-specifika overrides. Håll den minimal — lägg hellre ny CSS i `app.css` om den gäller båda klienterna.

**Regel:** Lägg ALDRIG CSS i en separat `app.css` i `Eatah.WebClient`. Filen existerar inte — all ny CSS går i `Eatah.Client/wwwroot/css/app.css`.

### Layout-shell

Standardstruktur i `Shared/MainLayout.razor`:

```
<div class="eatah-shell">
    <AppHeader />        // 3 slots: Left | Center | Right (workspace, titel, notisbell)
    <main class="eatah-shell__main">@Body</main>
    <AppNavbar />        // 5 items: Kost, Köplista, Matplan, Chat, Profil
</div>
<ModalHost />            // global modal stack
<ToastHost />            // korta meddelanden
```

- **Ingen sidobar / hamburgermeny**. All primär navigering går via `AppNavbar`.
- Sidor som behöver sätta header-innehåll använder `HeaderState` (singleton DI). Anropa `HeaderState.Set(center, left, right)` i `OnInitialized` och `HeaderState.Clear()` i `Dispose`. `AppHeader` (rendrad en gång av `MainLayout`) prenumererar och fallback-render­ar default-platshållare när inget är satt. Inline-render av `<AppHeader>` med `Center`/`Left`/`Right` `RenderFragment`-parametrar är fortfarande tillåtet om en specifik sida vill bypassa state-mekanismen.

### Modal-mönster

- Modaler är **komponenter, inte routes**. Visa via `ModalService.Show<TComponent>(parameters?)`.
- `ModalHost` mountas en gång i `MainLayout`. Backdrop-klick stänger top-modalen; ESC-stängning hanteras via JS interop om behov uppstår.
- Modal-komponenter får tillgång till sin `ModalInstance` via `[CascadingParameter]` om de behöver returnera ett resultat (`ModalService.Close(instance, result)`).
- En modal kan öppna en annan, men undvik djupa stackar — föredra inline-confirm i raden framför modal-i-modal.

### Ikoner

- En enda komponent: `<Icon Name="dice" Size="24" CssClass="..." />` i `Components/Shared/Icon.razor`.
- SVG-källfiler ligger i `wwwroot/icons/{name}.svg`. Lägg ALDRIG SVG-paths inline i razor-filer.
- Renderas som `<span>` med CSS-mask så `currentColor` styr färgen — fungerar tvärs MAUI-WebViews utan JS interop.
- Semantiska namn (t.ex. `vegan`, `chat`, `bell`) mappas till FontAwesome-filnamn i `IconMap` inuti `Icon.razor`. Lägg till nya namn där.

### Toast & feedback

- Korta, icke-blockerande meddelanden via `ToastService.Show("...")`. Renderas av `ToastHost` i `MainLayout`. Använd för "Kommer snart"-platshållare och bekräftelser av lyckade åtgärder. För kritiska fel: använd modal eller inline-fält.

### State Management

- Komponentlokal state för enkel UI-state.
- Singleton-services (DI) för applikationsvid state (`ModalService`, `ToastService`, `LoadingState`, `IngredientCheckState`).
- Undvik kaskaderande parametrar för logik — använd DI. Kaskad­parametrar är OK för rena UI-kontexter (t.ex. `ModalInstance`).

### API-kommunikation

All HTTP-kommunikation sker via `ApiClient`. Den kastar `ApiException` vid fel – använd ALDRIG `EnsureSuccessStatusCode()` direkt.

```csharp
// Alla publika metoder i ApiClient följer detta mönster:
public async Task<MealResponse?> CreateMealAsync(CreateMealRequest request, CancellationToken ct = default)
{
    var response = await _http.PostAsJsonAsync("api/meals", request, ct);
    await EnsureSuccessAsync(response, ct);   // kastar ApiException vid fel
    return await response.Content.ReadFromJsonAsync<MealResponse>(cancellationToken: ct);
}
```

`ApiErrorResponse`, `ApiErrorCodes` och `ApiException` finns i `Services/Contracts/ApiError.cs`.

---

## Result-mönster

Services returnerar `Result<T>` eller `Result` (non-generic) – **ALDRIG** egna domain-exceptions för förväntade felfall.

### `Result<T>` och `Error`

```csharp
// Lyckad:
return Result<MealResponse>.Success(response); // eller implicit: return response;

// Misslyckad:
return Result<MealResponse>.Failure(Error.NotFound(ErrorCodes.MealNotFound, "Meal not found."));
```

Tillgängliga `Error`-fabriker:

- `Error.NotFound(code, message)` → 404
- `Error.Conflict(code, message)` → 409
- `Error.Validation(code, message, validationErrors?)` → 400
- `Error.BadRequest(code, message)` → 400
- `Error.Upstream(code, message)` → 502
- `Error.Unexpected(code, message)` → 500

### `ErrorCodes` – string-konstanter

Alla felkoder definieras i `Common/ErrorCodes.cs`. Lägg till nya koder där när en ny feature tillkommer. Koder är snake_case, t.ex. `meal_not_found`, `weekly_plan_conflict`.

### `ResultExtensions` – konvertera till `IResult`

```csharp
// I handler-filen:
return result.ToHttpResult();                          // Ok / ProblemDetails
return result.ToCreatedResult(r => $"/api/meals/{r.Id}");  // Created / ProblemDetails
return result.ToNoContentResult();                     // NoContent / ProblemDetails
```

### Felresponse-format (RFC 7807 + `errorCode`)

Alla fel returneras på engelska med ett stabilt `errorCode`-fält som klienten switchar på:

```json
{
  "type": "https://tools.ietf.org/html/rfc7807",
  "title": "Not Found",
  "status": 404,
  "detail": "Meal with ID ... was not found.",
  "errorCode": "meal_not_found"
}
```

Validationsfel använder `HttpValidationProblemDetails` med `errors`-fältet:

```json
{
  "status": 400,
  "errorCode": "validation_error",
  "errors": { "name": ["Meal name is required."] }
}
```

**Aldrig:** kasta exceptions för förväntade fel (not found, conflict). Reservera exceptions för genuint oväntade tillstånd som fångas av `GlobalExceptionHandler`.

### Tillgängliga `Error`-fabriker

- `Error.NotFound` → 404
- `Error.Conflict` → 409
- `Error.Validation` → 400 (med `validationErrors`-dict)
- `Error.BadRequest` → 400
- `Error.Forbidden` → 403
- `Error.Unauthorized` → 401
- `Error.Upstream` → 502
- `Error.Unexpected` → 500

`Error` har även en egen `ToHttpResult()`-extension så endpoints kan returnera ett standalone `Error` direkt utan att packa in det i `Result<T>` (t.ex. när userId saknas: `Error.Unauthorized(...).ToHttpResult()`).

---

## Multi-tenancy & workspace-scoping

All användardata är scopad till en **workspace** (Personal eller Household). Klienten skickar headern `X-Eatah-Workspace: {guid}` på varje request. `WorkspaceResolutionMiddleware` (efter `UseAuthorization`):

1. Läser headern, validerar att inloggad användare är medlem.
2. Om headern saknas → fallback till användarens Personal workspace.
3. Om headern är ogiltig eller användaren inte är medlem → 403 `workspace_access_denied`.
4. Sätter `IWorkspaceContext.CurrentWorkspaceId`.

### Repository- och service-mönster

**Inga EF global query filters.** Filtrera ALLTID explicit i repos/services:

```csharp
// Läsning (system + workspace-ägt synligt):
var wsId = _workspace.CurrentWorkspaceId;
return await _db.Meals.Where(m => m.WorkspaceId == null || m.WorkspaceId == wsId).ToListAsync(ct);

// Skrivning (kräver aktiv workspace):
meal.WorkspaceId ??= _workspace.RequireCurrent();
```

System-ägda entiteter (`WorkspaceId == null`) är seedade default-data som alla workspaces kan läsa men inte mutera. Workspace-ägda entiteter är fullt isolerade — inga cross-workspace queries.

### Workspace-typer

- **Personal** — skapas automatiskt vid `ConfirmEmailAndSetCredentials` via `WorkspaceService.EnsurePersonalAsync`.
- **Household** — skapas på begäran (`POST /api/workspaces/households`) eller automatiskt vid första skickade vänförfrågan.

### Testning

Integrationstest-factoryn anropar `DataSeeder.EnsurePersonalWorkspaceAsync` så testanvändaren alltid har en aktiv workspace. `TestAuthHandler.TestUserId` är fix.

## Realtid & concurrency (multi-user workspaces)

Workspace-data kan muteras samtidigt av flera medlemmar (matplan, köplista, skafferi). Tre lager skyddar konsekvensen:

### 1. Optimistic concurrency (xmin)

Entiteter som muteras av flera samtidigt har PostgreSQL `xmin` som concurrency token via `b.UseXminAsConcurrencyToken()` (helper i `Infrastructure/Persistence/Configurations/NpgsqlBuilderExtensions.cs`). Aktiv på: `PantryItem`, `PantryItemMealCoverage`, `ShoppingItem`, `WeeklyPlan`, `DayPlan`.

En konflikt (`DbUpdateConcurrencyException`) mappas av `GlobalExceptionHandler` till **HTTP 409** med `errorCode: concurrency_conflict`. Klienten reagerar genom att refetcha — realtidssyncen (se nedan) gör detta automatiskt.

Lägg `UseXminAsConcurrencyToken()` på nya entiteter som flera workspace-medlemmar kan mutera samtidigt.

### 2. Per-workspace mutation locks

`WorkspaceLockProvider` (singleton i `Common/`) ger korta in-process locks per `(scope, workspaceId)` för operationer där två klienter inte får köra parallellt även med xmin (multi-step mutationer, batch-skrivningar):

```csharp
using var _lock = await _locks.AcquireAsync(WorkspaceLockProvider.ScopeRandomize, wsId, ct);
// ... multi-step mutation ...
```

Använt av: `WeeklyPlanService.RandomizeAsync` / `RandomizeDayAsync` (scope `randomize`), `ShoppingListService.SyncFromWeeklyPlanAsync` / `SyncFromCurrentWeeklyPlanAsync` (scope `shopping_sync`).

Gäller endast inom en process — antar single-instance deployment. xmin sköter cross-instance integritet.

### 3. Realtidsbroadcast (SignalR)

`IRealtimeNotifier` (`Common/RealtimeNotifier.cs`) skickar **lättviktiga invalidations-events** via `ChatHub` till gruppen `workspace:{id}`. Klienten är redan medlem (joinas av `WorkspaceState`).

Events och payload:

- `ShoppingListChanged` → `{ workspaceId }`
- `PantryChanged` → `{ workspaceId }`
- `WeeklyPlanChanged` → `{ workspaceId, planId, year, weekNumber }`

**Regel:** Anropa lämplig `_notifier.XxxChangedAsync(wsId, ct)` efter varje `SaveChangesAsync` som muterar en av dessa entiteter. Skicka inte hela payloaden — klienten refetchar.

På klienten:

- `ChatHubService` exponerar events `ShoppingListChanged`, `PantryChanged`, `WeeklyPlanChanged`.
- `RealtimeSyncService` (singleton, startad i `MauiProgram` / `Program.cs`) prenumererar och refetchar `PantryStateService` / `ShoppingStateService` när eventet gäller aktiv workspace.
- Sidor som visar veckoplan (t.ex. `Dashboard.razor`) prenumererar själva på `Hub.WeeklyPlanChanged` och kallar `LoadAsync()`.

Cross-workspace events ignoreras alltid (`Workspaces.CurrentId != workspaceId`).

## Felhantering

### API

- `GlobalExceptionHandler` middleware fångar oväntade exceptions och returnerar `ProblemDetails` med `errorCode: unexpected_error`
- Logga alla unhandled exceptions
- Returnera ALDRIG stack traces i produktion

### Klient

- `ApiClient` kastar `ApiException` (definierad i `Services/Contracts/ApiError.cs`) vid alla non-2xx svar
- `ApiException.ErrorCode` matchar en av konstanterna i `ApiErrorCodes` (client-side spegel av `ErrorCodes`)
- UI-lagret switchar på `ErrorCode` för att visa användarvänliga felmeddelanden på **svenska**
- Loading states på alla asynkrona operationer

```csharp
try
{
    await ApiClient.UpdateMealAsync(id, request);
}
catch (ApiException ex) when (ex.ErrorCode == ApiErrorCodes.MealNotFound)
{
    // Visa "Maträtten hittades inte." i UI
}
catch (ApiException ex) when (ex.ErrorCode == ApiErrorCodes.ValidationError)
{
    // Visa ex.Error.Errors i ett formulär
}
```

---

## Validering

- Använd **FluentValidation** för alla request-DTOs
- Validera i handler-filen (INTE i service) med `ValidationExtensions.ValidateRequestAsync`
- Returnera 400 Bad Request med strukturerade felmeddelanden
- Alla valideringsmeddelanden ska vara på **engelska** (klienten mappar dem vid behov)

```csharp
public class CreateMealValidator : AbstractValidator<CreateMealRequest>
{
    public CreateMealValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Meal name is required.")
            .MaximumLength(200).WithMessage("Name must be at most 200 characters.");
        RuleFor(x => x.Ingredients)
            .NotEmpty().WithMessage("At least one ingredient is required.");
    }
}
```

---

## Testning

### Verktyg

- **xUnit** – testramverk
- **FluentAssertions** – assertions
- **Moq** – mocking
- **WebApplicationFactory** – integrationstester

### Konventioner

- Testklassnamn: `{Klass}Tests`
- Testmetodnamn: `{Metod}_Should{Förväntat}_{Villkor}`
  - Exempel: `Evaluate_ShouldReturnFullScore_WhenAllRulesAreMet`
- Arrange-Act-Assert-mönster
- En assertion per test (med rimliga undantag)

---

## Kostregler – Teknisk specifikation

### Utvärderingslogik

Regler är heuristiska och baseras på antal tillfällen per vecka per kategori:

```
Poäng per regel = 1.0 om inom [min, max], annars proportionellt avdrag
Totalpoäng = genomsnitt av alla regelpoäng × 100
```

### Strikthet

Slumpmässig generering är alltid maximalt strikt. Om en kostprofil är angiven används alltid max antal iterationer för att hitta bästa möjliga menyplan som uppfyller reglerna.

### AI-generering av regler

- Prompten ska specificera att output ska vara JSON med fälten: `category`, `minPerWeek`, `maxPerWeek`, `description`
- Validera att AI-svaret är parsbart och rimligt innan det sparas
- Fallback till default-profil om AI-generering misslyckas

---

## Miljöer och konfiguration

| Miljö       | Databas                     | Logging         | Swagger      |
| ----------- | --------------------------- | --------------- | ------------ |
| Development | Lokal PostgreSQL            | Console + Debug | Aktiverad    |
| Test        | In-memory / Test-PostgreSQL | Minimal         | Ej aktiverad |
| Production  | Cloud PostgreSQL            | Structured JSON | Ej aktiverad |

### Secrets

- Använd `dotnet user-secrets` för lokala hemligheter
- AI API-nyckel: `AiSettings:ApiKey`
- Databas-lösenord: via connection string i secrets

---

## Git-konventioner

### Commit-meddelanden

Format: `type: kort beskrivning`

Typer:

- `feat:` ny funktionalitet
- `fix:` buggfix
- `refactor:` omstrukturering utan ny funktionalitet
- `test:` tester
- `docs:` dokumentation
- `chore:` övrigt (config, paket, etc.)

### Branching

- `main` – stabil, deploybar kod
- `feature/{feature-name}` – ny funktionalitet
- `fix/{description}` – bugfix
