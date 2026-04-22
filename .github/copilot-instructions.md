# Copilot Instructions – Eatah

> Denna fil beskriver projektets arkitektur, konventioner och regler.
> AI-modeller ska alltid konsultera denna fil innan kod genereras.

## Meta-regel – håll denna fil aktuell

**Efter varje kodändring:** kontrollera om ändringen påverkar arkitekturen, konventionerna eller mönstren som beskrivs här. Om så är fallet, uppdatera denna fil som en del av samma ändring. Ingen PR/commit ska introducera ett nytt mönster utan att det dokumenteras här.

---

## Projektöversikt

Eatah är en veckoplaneringsapp för måltider. Användaren tilldelar maträtter till veckodagar, slumpar menyer och får feedback via ett kostregelssystem. Appen byggs med .NET 10, ASP.NET Core Minimal API, PostgreSQL och .NET MAUI Blazor Hybrid.

---

## Solution-struktur

```
Eatah.sln
├── src/
│   ├── Eatah.Api/              # ASP.NET Core Minimal API
│   │   ├── Common/             # Delad infrastruktur (Result, Error, ErrorCodes, extensions)
│   │   │   ├── Error.cs
│   │   │   ├── ErrorCodes.cs
│   │   │   ├── Result.cs
│   │   │   ├── ResultExtensions.cs
│   │   │   └── ValidationExtensions.cs
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
│   │   │   └── AI/
│   │   ├── Middleware/         # Global error handling, logging
│   │   └── Program.cs
│   ├── Eatah.Domain/           # Entiteter, enums, value objects (INGA beroenden)
│   │   └── Entities/
│   ├── Eatah.Infrastructure/   # EF Core, DbContext, konfigurationer, seeding
│   │   └── Persistence/
│   │       ├── Configurations/
│   │       ├── Migrations/
│   │       └── EatahDbContext.cs
│   └── Eatah.Client/           # .NET MAUI Blazor Hybrid
│       ├── Pages/
│       ├── Components/
│       │   ├── WeeklyPlan/
│       │   ├── Meals/
│       │   └── DietRules/
│       ├── Services/
│       └── wwwroot/
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
- Tailwind CSS via CDN (initialt)
- **Ingen inline styling** – använd Tailwind-klasser
- Mobile-first: designa för mobil först, lägg till breakpoints för desktop
- Konsekvent färgpalett (definieras i tailwind config)

### State Management
- Komponentlokal state för enkel UI-state
- Services (DI) för delad state mellan komponenter
- Undvik kaskaderande parametrar – använd DI istället

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

| Miljö | Databas | Logging | Swagger |
|-------|---------|---------|---------|
| Development | Lokal PostgreSQL | Console + Debug | Aktiverad |
| Test | In-memory / Test-PostgreSQL | Minimal | Ej aktiverad |
| Production | Cloud PostgreSQL | Structured JSON | Ej aktiverad |

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
