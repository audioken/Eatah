# Phase 3 – Authentication (ASP.NET Identity + e-postverifiering)

Inför inloggning, registrering, e-postverifiering och lösenords­återställning. Pre-launch app → drop & re-seed DB.

## Steps

### 3.1 Paket & projektstruktur

- Lägg till i `src/Eatah.Api/Eatah.Api.csproj`:
  - `Microsoft.AspNetCore.Identity.EntityFrameworkCore` v10.0.x
  - `MailKit` v4.x (för SMTP)
- Ny feature: `src/Eatah.Api/Features/Auth/`.

### 3.2 Identity-setup

- Ny entitet `EatahUser : IdentityUser<Guid>` i `src/Eatah.Domain/Entities/EatahUser.cs`.
  - Properties (utöver Identity-bas): `DisplayName` (string, 50, unique), `CreatedAt`.
  - **Domain-projektet kan inte referera Identity** — Identity-pakettypen `IdentityUser<Guid>` ligger i ASP.NET Identity. Lösning: **flytta `EatahUser` till `Eatah.Infrastructure`** (eller skapa interface i Domain). Beslut: lägg `EatahUser` i `Eatah.Infrastructure/Identity/EatahUser.cs` för att inte bryta beroenderegeln. Domain-entiteter som behöver `UserId` använder bara `Guid UserId`.
- Uppdatera `src/Eatah.Infrastructure/Persistence/EatahDbContext.cs`:
  - Ärv från `IdentityDbContext<EatahUser, IdentityRole<Guid>, Guid>` istället för `DbContext`.
  - Override `OnModelCreating` så att Identity-tabellerna får snake_case namn (`users`, `user_roles`, etc.) — använd en konventions-helper eller manuell mapping.
- Migration: **drop alla befintliga migrations** (pre-launch), generera ny `InitialCreate` som inkluderar Identity-tabeller + alla feature-tabeller.

### 3.3 Auth-konfiguration i `Program.cs`

- `builder.Services.AddIdentity<EatahUser, IdentityRole<Guid>>(opts => { ... password rules ... }).AddEntityFrameworkStores<EatahDbContext>().AddDefaultTokenProviders()`.
- Password options: min 8, uppercase, lowercase, digit, non-alphanumeric, unique chars 1.
- `SignIn.RequireConfirmedEmail = true`.
- **Auth-mekanism**: Cookie auth (samma host MAUI WebView ↔ API → cookies fungerar; JWT skulle krångla med token-storage i WebView).
  - `services.ConfigureApplicationCookie(opts => { opts.Cookie.HttpOnly = true; opts.Cookie.SameSite = SameSiteMode.Lax; opts.Cookie.SecurePolicy = CookieSecurePolicy.Always; opts.ExpireTimeSpan = TimeSpan.FromDays(30); opts.SlidingExpiration = true; opts.Events.OnRedirectToLogin = ctx => { ctx.Response.StatusCode = 401; return Task.CompletedTask; }; opts.Events.OnRedirectToAccessDenied = ctx => { ctx.Response.StatusCode = 403; return Task.CompletedTask; }; })`.
- `app.UseAuthentication(); app.UseAuthorization();`
- **Alla befintliga endpoints**: lägg till `.RequireAuthorization()` på feature-grupper i `MealEndpoints`, `WeeklyPlanEndpoints`, `DietRuleEndpoints`, `AiEndpoints`. Auth-endpoints är publika.

### 3.4 E-post (MailKit)

- `src/Eatah.Api/Features/Auth/Email/SmtpSettings.cs` (record): `Host, Port, Username, Password, FromEmail, FromName, UseSsl`.
- `src/Eatah.Api/Features/Auth/Email/IEmailSender.cs` + `SmtpEmailSender.cs`.
- Två appsettings-profiler:
  - **Mailtrap (dev)**: `appsettings.Development.json` → `smtp.mailtrap.io:2525`.
  - **Brevo (prod)**: `appsettings.Production.json` → `smtp-relay.brevo.com:587`.
  - Credentials i user-secrets / env vars, **aldrig i appsettings**.
- Templates som C#-strings (svenska): `EmailTemplates.cs` med `BuildConfirmationEmail(link)`, `BuildPasswordResetEmail(link)`.

### 3.5 Auth-endpoints (one file per endpoint)

- `Features/Auth/AuthEndpoints.cs`: router + DI extension `AddAuthFeature()`.
- Endpoints (alla publika, prefix `/api/auth`):
  - `RegisterEmail.cs` — `POST /register` body `{ email }` → skapa user med `EmailConfirmed=false`, generera `ConfirmationToken`, skicka mail med länk till `eatah://confirm?token=...&userId=...` (deep link) eller webb-URL i dev.
  - `ConfirmEmailAndSetCredentials.cs` — `POST /confirm` body `{ userId, token, displayName, password }` → validera token, sätt password, sätt displayName, `EmailConfirmed=true`, sign in.
  - `Login.cs` — `POST /login` body `{ emailOrUsername, password, rememberMe }` → `SignInManager.PasswordSignInAsync`.
  - `Logout.cs` — `POST /logout` → `SignOutAsync`.
  - `RequestPasswordReset.cs` — `POST /password-reset/request` body `{ email }` → generera token, skicka mail. Returnerar alltid 200 för att inte avslöja om email finns.
  - `ResetPassword.cs` — `POST /password-reset` body `{ userId, token, newPassword }`.
  - `ChangePassword.cs` — `POST /password-change` body `{ currentPassword, newPassword }` (kräver auth).
  - `Me.cs` — `GET /me` → returnerar inloggad users `{ id, email, displayName }` (för klient-state).
- Validators (FluentValidation, engelska meddelanden):
  - `RegisterEmailValidator`, `ConfirmEmailValidator` (password-reglerna), `LoginValidator`, `RequestPasswordResetValidator`, `ResetPasswordValidator`, `ChangePasswordValidator`.
  - Password-regler: min 8, max 128, måste ha versal, gemen, siffra, specialtecken.
  - DisplayName: min 3, max 50, alfanumeriska + `_-`.

### 3.6 Felkoder (ny i `Common/ErrorCodes.cs`)

- `auth_email_taken`, `auth_email_not_confirmed`, `auth_invalid_credentials`, `auth_invalid_token`, `auth_token_expired`, `auth_password_invalid`, `auth_user_not_found`, `auth_display_name_taken`, `auth_email_required_for_reset`.
- Spegla i `src/Eatah.Client/Services/Contracts/ApiError.cs`.

### 3.7 Klient-sidor & state

- Ny: `src/Eatah.Client/Services/AuthState.cs` — håller `CurrentUser : UserResponse?`, `event OnChange`, metoder `LoginAsync`, `LogoutAsync`, `RegisterAsync`, etc. (wrappar `ApiClient`).
- Ny: `src/Eatah.Client/Services/AuthHttpMessageHandler.cs` — säkerställer cookies skickas (`HttpClientHandler.UseCookies = true`).
- Ny: `src/Eatah.Client/Components/Shared/AuthGuard.razor` — wrapper som kollar `AuthState.IsAuthenticated`; om ej → renderar `LoginPage`. Mounteras i `App.razor` / `MainLayout`.
- Nya pages (icke-routade när inloggad — visas som "skärmar" via `AuthGuard`):
  - `src/Eatah.Client/Pages/Auth/Login.razor`
  - `src/Eatah.Client/Pages/Auth/Register.razor` (steg 1: enbart e-post)
  - `src/Eatah.Client/Pages/Auth/ConfirmEmail.razor` (steg 2: visas när användaren öppnar deep-link, formulär med displayName + password + öga)
  - `src/Eatah.Client/Pages/Auth/ForgotPassword.razor`
  - `src/Eatah.Client/Pages/Auth/ResetPassword.razor` (visas via deep-link)
  - `src/Eatah.Client/Pages/Auth/ChangePassword.razor` (öppnas från Profile-modal i fas 6 — kan finnas i fas 3 som standalone-route också)

### 3.8 Live-validering (klient)

- Återanvändbar komponent `src/Eatah.Client/Components/Shared/PasswordInput.razor`:
  - Props: `[Parameter] string Value, EventCallback<string> ValueChanged, bool ShowStrength`.
  - Öga-toggle (`<Icon Name="eye"/>`/`<Icon Name="eye-off"/>`) växlar `type="password"` ↔ `text`.
  - Strength-meter (om `ShowStrength`): 4 dots/checkboxar:
    - ≥8 tecken
    - har versal
    - har gemen
    - har siffra
    - har specialtecken
  - Live-uppdatering på `oninput`.
- `src/Eatah.Client/Components/Shared/UsernameInput.razor`:
  - Live: ≥3 tecken, endast a-zA-Z0-9\_- (regex), och valfri uniqueness-check via `GET /api/auth/check-displayname?name=...` debouncad 300ms.
- E-postvalidering: HTML5 + regex live.

### 3.9 Deep linking (MAUI)

- Konfigurera URI-scheme `eatah://` i:
  - `Platforms/Android/AndroidManifest.xml` — `<intent-filter>` med `<data android:scheme="eatah" />`
  - `Platforms/iOS/Info.plist` — `CFBundleURLTypes`
  - Windows: protocol activation
- Hantera incoming deep link i `App.xaml.cs` → routa till `ConfirmEmail` / `ResetPassword` med query params.
- Web fallback: även länk till `https://eatah.app/confirm?...` som redirectar till deep link (skjuts upp om hosting ej finns ännu — i dev: visa länken i konsolen + `Console.WriteLine` så man kan kopiera).

### 3.10 Tester

- Unit: validators (alla password-regler-paths), `AuthService` om mellanlager skapas.
- Integration: `AuthEndpointsTests`:
  - Register → 200 + email skickat (mocka `IEmailSender`)
  - Confirm med ogiltig token → 400 `auth_invalid_token`
  - Login utan email-confirm → 401 `auth_email_not_confirmed`
  - Full flow: register → confirm → login → /me

## Relevant files

- `src/Eatah.Api/Eatah.Api.csproj` — paket
- `src/Eatah.Api/Program.cs` — Identity, cookie, email, RequireAuthorization globalt
- `src/Eatah.Infrastructure/Persistence/EatahDbContext.cs` — `IdentityDbContext<...>`
- Ny: `src/Eatah.Infrastructure/Identity/EatahUser.cs`
- Ny: hela `src/Eatah.Api/Features/Auth/`
- Ta bort: alla 3 befintliga migrations + generera ny
- `src/Eatah.Api/Common/ErrorCodes.cs` + `src/Eatah.Client/Services/Contracts/ApiError.cs` — nya koder
- `src/Eatah.Client/Services/ApiClient.cs` — auth-metoder, cookie-handler
- Ny: hela `src/Eatah.Client/Pages/Auth/` + `Components/Shared/{PasswordInput,UsernameInput,AuthGuard}.razor` + `Services/AuthState.cs`
- Befintliga endpoints: lägg `.RequireAuthorization()` i alla `*Endpoints.cs`
- `src/Eatah.Infrastructure/Persistence/DataSeeder.cs` — seed dev-user `dev@eatah.local` med konfirmerad email + lösen `Dev123!@#`

## Verification

1. Migrations: `dotnet ef database update` skapar Identity-tabeller med snake_case.
2. Manuell registrering → mail anländer i Mailtrap.
3. Klick på länk → ConfirmEmail-sidan → fyll displayName + password → inloggad.
4. Login med fel password → felmeddelande på svenska "Fel e-post eller lösenord."
5. Login utan confirm → felmeddelande "Verifiera din e-post först."
6. Forgot password → mail → reset-sida → inloggad.
7. Befintliga endpoints (t.ex. `GET /api/meals`) returnerar 401 utan cookie, 200 med.
8. Live-validering på register-formuläret reagerar omedelbart vid `oninput`.
9. Öga-toggle visar/döljer lösenord.
10. Integration-tester gröna.

## Decisions

- **Cookie-auth** (inte JWT) för enklare hantering i MAUI WebView.
- `EatahUser` ligger i `Infrastructure` för att respektera Domain-beroenderegeln.
- **Drop & re-seed** — alla 3 befintliga migrations tas bort, ny `InitialCreate` inkluderar allt.
- Email confirmation och password reset använder Identity's `UserManager.GenerateEmailConfirmationTokenAsync` resp. `GeneratePasswordResetTokenAsync` (HMAC-baserat, time-limited).
- **DisplayName uniqueness** är hård (skapar konflikt) — krävs för fas 5 friend-search.

## Further Considerations

1. **Identity-tabellnamn snake_case** — kräver explicit fluent mapping för `AspNetUsers`, `AspNetUserClaims`, `AspNetUserLogins`, `AspNetUserRoles`, `AspNetUserTokens`, `AspNetRoles`, `AspNetRoleClaims`. En helper-metod i `EatahDbContext.OnModelCreating`. Alternativ A: behåll `AspNet*`-namnen för Identity (avviker från konvention men minst friktion). **Rekommendation: snake_case för konsistens.**
2. **Brevo i prod** — kräver verifierad domän + DKIM/SPF. Skjuts upp tills hosting bestäms.
3. **Rate limiting** på `/auth/login` och `/auth/password-reset/request` — använd `Microsoft.AspNetCore.RateLimiting` med fixed-window 5/min/IP. Lägg till i denna fas.
4. **Anti-forgery / CSRF** — cookie-auth + APIs anropas från MAUI WebView. SameSite=Lax + custom header (`X-Requested-With`) räcker för MAUI-scenariot. Dokumentera i `copilot-instructions.md`.
