# Plan: Eatah – stor uppdatering (master)

Stor refaktor i 9 faser. Varje fas har eget dokument i `docs/plans/phase-X-*.md`. Faserna är ordnade så att tidigare faser inte blockerar sena, och så att riskabla data­modellsändringar landar innan UI-features som beror på dem.

## Övergripande beslut (fastställda med användaren)

- **Auth-hash:** ASP.NET Identity default (PBKDF2). Inget BCrypt-paket.
- **E-post:** SMTP via `MailKit` med konfigurerbara settings → Mailtrap i dev, Brevo i prod.
- **Data-migration:** Drop & re-seed (appen är pre-launch). Ny seeder skapar demo-user + Personal workspace.
- **Workspace-modell:** Varje användare har 1 obligatoriskt **Personal** workspace + max 1 **Household** workspace (delas med foodbuddy/-ies).
- **CSS-strategi:** Behåll Tailwind via CDN. Lägg till `wwwroot/css/app.css` med **design-tokens** (CSS custom properties) + **komponentklasser** för navbar, header, profil-kort, badges, modaler. Tailwind = layout/utility, app.css = identitet.
- **Ingredient-DB:** Globalt **system-register** (ej raderbart, seedat med ~vanligaste 200 svenska livsmedel) + per-user **custom** ingredients (raderbara av ägaren). Autocomplete söker i båda.

## Fas-ordning och beroenden

| #   | Fas                                                                                         | Beror på             | Risk              |
| --- | ------------------------------------------------------------------------------------------- | -------------------- | ----------------- |
| 0   | Foundation – layout-shell, footer-navbar, design-tokens, ikon­system, modal-infra, bakgrund | –                    | Låg               |
| 1   | Matplan-redesign – header (vecka + profil + notis), kostprofil-kort, randomize-knapp        | 0                    | Låg               |
| 2   | Diet Profile som global modal (popup ersätter sidan)                                        | 0, 1                 | Låg               |
| 3   | Authentication – Identity, e-post­verifiering, login/registrering/reset, live-validering    | – (parallel med 0–2) | Hög               |
| 4   | Workspaces – Personal + max 1 Household, scoping av WeeklyPlan + DietProfile                | 3                    | Hög (data-modell) |
| 5   | Friends/Foodbuddies + Notifications                                                         | 4                    | Medel             |
| 6   | Profile-modal – konto-edit, danger zone, vänsökning                                         | 5                    | Låg               |
| 7   | Ingredient DB + Pantry (Skafferi) + Shopping list (Köplista)                                | 4 (workspace-scope)  | Hög (data-modell) |
| 8   | Chat – SignalR hub, edit, reaktioner, shopping-list-integration                             | 5, 7                 | Hög (realtid)     |

**Parallellisering:** Fas 0–2 (frontend-shell) kan utvecklas parallellt med fas 3 (backend auth). Faserna 5+ är seriella eftersom varje bygger på föregående datamodell.

## Tvärgående uppdateringar av `copilot-instructions.md`

Samlas i `docs/plans/copilot-instructions-updates.md`. Tillämpas när varje fas landar — inte i förväg.

Nya konventioner som dokumenteras:

1. **Design-tokens** (CSS custom properties) i `wwwroot/css/app.css`. Komponent­klasser med BEM-light naming. Tailwind = layout, app.css = identitet.
2. **Modal-infrastruktur** (`ModalService` + `<ModalHost />` i `MainLayout`). Modaler är komponenter, inte sidor. Footer-knappar kan trigga antingen navigation eller `ModalService.Show<T>()`.
3. **SVG-ikoner** via `<Icon Name="seedling" />` komponent som läser från `wwwroot/icons/*.svg` (kopierade från `resources/icons/`). En källa, inga inline SVG-paths spridda i kod.
4. **Multi-tenancy / scoping**: `WorkspaceContext` (scoped DI) bär `CurrentWorkspaceId`. Alla `WeeklyPlan`-, `DietProfile`-, `PantryItem`-, `ShoppingListItem`-queries filtreras automatiskt via `IWorkspaceScoped` interface + EF Core global query filters.
5. **Auth-konventioner**: Identity-tabeller med `users`/`user_roles` snake_case. JWT eller cookie? **Cookie-auth** (samma host MAUI WebView ↔ API). Endpoints kräver `RequireAuthorization()` om de inte är publika.
6. **SignalR-konventioner**: Hubs ligger i feature-mappar (`Features/Chat/ChatHub.cs`). Klient-services wrappar `HubConnection` (`ChatHubClient.cs`). Authorize via samma cookie.
7. **Ingredient-ownership-mönster**: `Ingredient.OwnerUserId` nullable (null = system). Dela­ringsregel: alla ser system-ingredienser, ägare ser sina egna; raderingsskydd om `OwnerUserId IS NULL` om inte admin.
8. **Notifications**: `INotificationService` + SignalR-push när användare är online, fallback DB-polling vid app-start.
9. **Felkoder**: nya per fas (auth_email_taken, workspace_full, friend_request_pending, ingredient_in_use, etc.). Lista samlas i `copilot-instructions-updates.md` och adderas till `Common/ErrorCodes.cs` när respektive fas implementeras.

## Verifikation per fas

Varje fasdokument har egen `**Verification**`-sektion. Övergripande:

- xUnit-tester för all ny servicelogik (Result-mönster, evaluators, generators)
- Integration­tester för nya endpoints via `EatahWebApplicationFactory`
- Manuell smoke-test i MAUI Windows-target efter varje fas

## Scope-gränser

**Inkluderat:** allt i de 9 faserna.
**Exkluderat (ej i denna plan):**

- Push-notiser till mobil OS-nivå (endast in-app)
- Bilduppladdning för meals / ingredients
- Översättning / i18n (svenska UI hårdkodat)
- Admin-UI (hantering av system-ingredienser görs via SQL/seeder tills vidare)
- Apple/Google sociala inloggningar (endast e-post)
- Betalning / subscriptions
