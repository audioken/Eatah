# Copilot-instructions: planerade tillägg

Dessa avsnitt läggs in i `.github/copilot-instructions.md` löpande, **i samband med att respektive fas implementeras** (per meta-regeln "ingen ändring utan dokumentation"). Lista här är förslag — färdig text skrivs när fasen körs.

---

## ✅ Fas 0 (Foundation) — TILLAGT

> Tillägg från fas 0 redan inlagda i `.github/copilot-instructions.md` (Frontend-konventioner-sektionen). Se commit-historik.

---

## ✅ Fas 1 (Matplan-redesign) — TILLAGT

> Layout-shell-sektionen i `.github/copilot-instructions.md` uppdaterad: pages sätter header-innehåll via `HeaderState` (singleton DI) i `OnInitialized` och rensar i `Dispose`. `AppHeader` prenumererar på state och faller tillbaka till default-platshållare. Inline `<AppHeader>` med RenderFragment-parametrar är fortfarande stödd som opt-in.

---

## Att läggas till efter Fas 3 (Auth)

### Authentication

- ASP.NET Identity med `EatahUser : IdentityUser<Guid>` (i `Eatah.Infrastructure/Identity/`, ej Domain).
- **Cookie-auth** (HttpOnly, SameSite=Lax, Secure i prod). Inte JWT.
- Identity-tabeller mappas till snake_case (`users`, `user_roles`, ...) via fluent config.
- Alla feature-endpoints kräver `.RequireAuthorization()` om de inte explicit är publika (auth, ingredient-search för publik autocomplete).
- Lösenordskrav: min 8 tecken, versal+gemen+siffra+specialtecken. Definierat en gång i `Program.cs` och i klient-validator.
- E-postutskick via `IEmailSender` (SMTP MailKit). Konfigurerat per miljö (dev: Mailtrap, prod: Brevo).
- Rate limiting på `/api/auth/login` och `/api/auth/password-reset/request` (fixed window, 5/min/IP).
- **CSRF**: SameSite=Lax + custom header `X-Requested-With` på state-changing requests. MAUI WebView är trusted origin.

### Klient-state för auth

- `AuthState`-service (singleton/scoped) med `CurrentUser`, `IsAuthenticated`, events.
- `<AuthGuard>` i `MainLayout` renderar login-flöde om ej inloggad.

---

## Att läggas till efter Fas 4 (Workspaces)

### Multi-tenancy / data scoping

- Workspace-modell: varje user har 1 obligatorisk Personal + max 1 Household.
- All workspace-scoped data har `WorkspaceId` (Guid).
- **Global query filter** i `EatahDbContext` baserat på `WorkspaceContext` (scoped DI). DbContext-konstruktor injicerar `WorkspaceContext`.
- System-data (system-meals, Livsmedelsverket-profil, system-ingredienser): `WorkspaceId IS NULL` är synligt för alla workspaces.
- Klienten skickar `X-Eatah-Workspace: {guid}` på varje request via `WorkspaceHttpMessageHandler`.
- Middleware `WorkspaceResolutionMiddleware` validerar header och fyller `WorkspaceContext` innan endpoint-handlers körs.
- I services: vid create-operationer på workspace-scoped entiteter, sätt `WorkspaceId = workspaceContext.CurrentWorkspaceId` explicit.

---

## Att läggas till efter Fas 5 (Friends/Notifications)

### Notifications

- `INotificationService.NotifyAsync(userId, type, payload, ct)` är den enda vägen att skapa notiser.
- Implementation skriver till DB + (efter fas 8) pushar via SignalR om mottagaren är online.
- Klienten pollar `/api/notifications?unreadOnly=true` var 30:e sekund som fallback.

---

## Att läggas till efter Fas 7 (Ingredients/Pantry/Shopping)

### Ingredient-ownership

- `Ingredient.OwnerUserId` nullable. `null = system` (skyddad mot radering, syns för alla).
- User-owned ingredients raderbara av ägaren om inte i bruk.
- `MealIngredient` är join-tabell (composite PK `(MealId, IngredientId)`) med valfritt `Quantity` fritext.
- Autocomplete-flöde: alla ingredient-input går genom `<IngredientAutocomplete>` som anropar `GET /api/ingredients/search` och POSTar nya custom-ingredients idempotent.
- **Auto-sync**: shopping list uppdateras automatiskt server-side när weekly plan muteras. Klienten ska inte trigga sync manuellt.

---

## Att läggas till efter Fas 8 (Chat/SignalR)

### SignalR-konventioner

- Hubs ligger i feature-mappar (`Features/{Feature}/{Feature}Hub.cs`).
- `[Authorize]` på alla hubs (samma cookie-auth som REST).
- **Push-only**: alla writes går via REST-endpoints. Hubs skickar bara server→client events. Detta håller validering+felkoder konsekvent.
- Klient-wrappers: `{Feature}HubClient.cs` i `Services/`, exponerar typade events.
- Map-route­konvention: `app.MapHub<XxxHub>("/hubs/{name}").RequireAuthorization()`.

### Köplista ↔ Chat-integration

- Strukturerade chat-meddelanden via `MessageKind` enum + JSON-payload. Render­specialiserade bubble-typer i klient.
