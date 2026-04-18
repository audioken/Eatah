# Eatah

Veckoplaneringsapp för måltider, byggd med .NET 8, ASP.NET Core Minimal API, PostgreSQL och .NET MAUI Blazor Hybrid.

## Solution-struktur

```
src/
  Eatah.Api/            # ASP.NET Core Minimal API
  Eatah.Domain/         # Domänentiteter (inga beroenden)
  Eatah.Infrastructure/ # EF Core / PostgreSQL
  Eatah.Client/         # .NET MAUI Blazor Hybrid
tests/
  Eatah.Api.Tests/      # xUnit-tester
```

Se [.github/copilot-instructions.md](.github/copilot-instructions.md) för arkitekturregler och konventioner.

## Komma igång lokalt

Förutsättningar:

- .NET 8 SDK
- PostgreSQL 14+ (lokal eller via Docker)

Standard-connection string för utveckling finns i `src/Eatah.Api/appsettings.Development.json`.

```powershell
dotnet build
dotnet run --project src/Eatah.Api
```

Swagger UI finns på `/swagger` i Development. Health check finns på `/health`.

### AI-nyckel

Spara API-nyckeln säkert via user-secrets (kör i `src/Eatah.Api`):

```powershell
dotnet user-secrets set "AiSettings:ApiKey" "<din-nyckel>"
```

## Köra med Docker

Hela stacken (API + PostgreSQL) startas via Docker Compose:

```powershell
docker compose up --build
```

Detta startar:

- `postgres` på `localhost:5432`
- `api` på `http://localhost:8080` (Production-miljö, structured JSON-loggar)

Health check: `GET http://localhost:8080/health` ska svara `Healthy`.

### Miljövariabler

| Variabel                               | Beskrivning                                        | Default                      |
| -------------------------------------- | -------------------------------------------------- | ---------------------------- |
| `ASPNETCORE_ENVIRONMENT`               | Miljö (`Development`, `Production`, `Testing`)     | `Production` (i Docker)      |
| `ConnectionStrings__DefaultConnection` | PostgreSQL connection string                       | sätts i `docker-compose.yml` |
| `AI_API_KEY`                           | OpenAI-nyckel som mappas till `AiSettings__ApiKey` | tomt                         |

Skapa en `.env`-fil bredvid `docker-compose.yml` för att sätta hemligheter:

```env
AI_API_KEY=sk-...
```

## Logging

Loggning sker via **Serilog**.

- **Development:** Console + Debug, läsbart format
- **Production:** Structured JSON (`CompactJsonFormatter`), enrichat med `MachineName` och `EnvironmentName`
- **Testing:** Endast Console, Warning-nivå

Konfigurationen ligger i respektive `appsettings.{Environment}.json` under `Serilog`-sektionen.

## Tester

```powershell
dotnet test
```

## .NET MAUI Client

`Eatah.Client` är ett .NET MAUI Blazor Hybrid-projekt. Det kräver att MAUI-workloads är installerade:

```powershell
dotnet workload install maui
```

Tills workloads finns på maskinen byggs klienten inte automatiskt – bygg API och tester via:

```powershell
dotnet build src/Eatah.Api
dotnet build tests/Eatah.Api.Tests
```
