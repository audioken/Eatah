# Copilot Instructions – Eatah

> Denna fil beskriver projektets arkitektur, konventioner och regler.
> AI-modeller ska alltid konsultera denna fil innan kod genereras.

---

## Projektöversikt

Eatah är en veckoplaneringsapp för måltider. Användaren tilldelar maträtter till veckodagar, slumpar menyer och får feedback via ett kostregelssystem. Appen byggs med .NET 8, ASP.NET Core Minimal API, PostgreSQL och .NET MAUI Blazor Hybrid.

---

## Solution-struktur

```
Eatah.sln
├── src/
│   ├── Eatah.Api/              # ASP.NET Core Minimal API
│   │   ├── Features/
│   │   │   ├── Meals/          # Endpoints, service, repo, DTOs, validator
│   │   │   ├── WeeklyPlan/
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
- `{Feature}Endpoints.cs` – Minimal API endpoint-mappning
- `{Feature}Service.cs` – Affärslogik
- `{Feature}Repository.cs` + `I{Feature}Repository.cs` – Dataåtkomst
- `{Feature}Dtos.cs` – Request/response DTOs
- `{Feature}Validator.cs` – FluentValidation-validatorer

### Beroenden
```
Api → Domain, Infrastructure
Infrastructure → Domain
Domain → (inga beroenden)
Client → (kommunicerar med Api via HTTP)
```

**Domain-projektet får ALDRIG ha beroenden mot andra projekt eller infrastrukturpaket.**

### Dependency Injection
Alla services och repositories registreras via DI i `Program.cs` eller via extension methods per feature:
```csharp
// I varje feature:
public static class MealServiceExtensions
{
    public static IServiceCollection AddMealFeature(this IServiceCollection services)
    {
        services.AddScoped<IMealRepository, MealRepository>();
        services.AddScoped<MealService>();
        return services;
    }
}
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
```csharp
public static class MealEndpoints
{
    public static void MapMealEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/meals")
            .WithTags("Meals")
            .WithOpenApi();

        group.MapGet("/", GetAllMeals);
        group.MapGet("/{id:guid}", GetMealById);
        group.MapPost("/", CreateMeal);
        group.MapPut("/{id:guid}", UpdateMeal);
        group.MapDelete("/{id:guid}", DeleteMeal);
    }

    private static async Task<IResult> GetAllMeals(MealService service)
    {
        var meals = await service.GetAllAsync();
        return Results.Ok(meals);
    }
    // ...
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
```csharp
public class ApiClient
{
    private readonly HttpClient _http;

    public ApiClient(HttpClient http)
    {
        _http = http;
    }

    public async Task<List<MealResponse>> GetMealsAsync()
    {
        return await _http.GetFromJsonAsync<List<MealResponse>>("api/meals") ?? [];
    }
}
```

---

## Felhantering

### API
- Global exception handler middleware som returnerar `ProblemDetails`
- Feature-specifika exceptions ärver från `ApplicationException`
- Logga alla unhandled exceptions
- Returnera ALDRIG stack traces i produktion

```csharp
// Standard error response:
{
    "type": "https://tools.ietf.org/html/rfc7807",
    "title": "Validation Error",
    "status": 400,
    "detail": "Maträttens namn får inte vara tomt.",
    "errors": { "name": ["Namnet är obligatoriskt."] }
}
```

### Klient
- Visa användarvänliga felmeddelanden på svenska
- Loading states på alla asynkrona operationer
- Retry-logik för nätverksfel (Polly eller manuell)

---

## Validering

- Använd **FluentValidation** för alla request-DTOs
- Validera i API-lagret INNAN affärslogik körs
- Returnera 400 Bad Request med strukturerade felmeddelanden

```csharp
public class CreateMealValidator : AbstractValidator<CreateMealRequest>
{
    public CreateMealValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Maträttens namn är obligatoriskt.")
            .MaximumLength(200).WithMessage("Namnet får vara max 200 tecken.");
        RuleFor(x => x.Ingredients)
            .NotEmpty().WithMessage("Minst en ingrediens krävs.");
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

### Strictness-parameter
- `0.0` = helt slumpmässigt (inga regler beaktas)
- `0.5` = försöker följa regler men tillåter avvikelser
- `1.0` = strikt – slumpar om tills reglerna uppfylls (med max iterations)

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
