# Phase 6 – Profile-modal

Konto-administration som global modal från **Profil**-knappen i navbar. Innehåller även vänsökning (Phase 5-funktionalitet kopplas in här).

## Steps

### 6.1 Modal-komponent

`src/Eatah.Client/Components/Profile/ProfileModal.razor`:

**Sektioner uppifrån:**

1. **Användardata** — visar `DisplayName` och `Email` med edit-pen jämte vardera.
   - Klick på pen → inline-edit-läge med spara/avbryt (eller liten sub-modal `EditFieldModal`).
   - Email-edit kräver verifiering: skickar ny `confirmation`-mail till nya adressen, ändras först vid klick på länken.
   - DisplayName: live-uniqueness-check (`GET /api/auth/check-displayname`).
2. **Lösenordsknapp** — "Byt lösenord" → öppnar `ChangePasswordModal` (form med current+new+öga, samma `PasswordInput`-komponent som fas 3).
3. **Vänner** — sök-input + lista över befintliga foodbuddies.
   - Sök: live på input (debouncad 300ms), `ApiClient.SearchUsersAsync(q)`.
   - Resultat: rader med `DisplayName` + "Lägg till foodbuddy"-knapp → `ApiClient.SendFriendRequestAsync(toUserId)`.
   - Lista-sektion: nuvarande household-medlemmar med "Ta bort"-knapp (rejectar/leavar).
4. **Workspace** — visar nuvarande Household-namn med rename-pen + "Lämna hushåll"-knapp.
5. **Danger zone** — röd sektion längst ner:
   - Knapp "Ta bort konto" → `DeleteAccountModal` med text-bekräftelse (skriv `RADERA` för att bekräfta) + lösenord.
   - Endpoint: `DELETE /api/auth/me` body `{ password, confirmation }` → cascade delete user + workspaces där user är ende member + alla notifications.

### 6.2 Nya endpoints

- `PATCH /api/auth/me` — uppdatera displayName.
- `POST /api/auth/email-change/request` — startar email-byte med token.
- `POST /api/auth/email-change/confirm` — slutför.
- `DELETE /api/auth/me` — radera konto (kräver lösenord + bekräftelse).
- `GET /api/auth/check-displayname?name=...` — uniqueness-check (publik, inget auth).

### 6.3 Felkoder

- `auth_password_required_for_destructive_action`, `auth_account_delete_confirmation_invalid`, `auth_email_change_pending`.

### 6.4 Tester

- Integration: account delete cascade (user borta, workspaces borta om sista member, notifications borta).
- Unit: validators för delete-confirmation (kräver exakt "RADERA").

## Relevant files

- Nya endpoints i `Features/Auth/`
- Klient: `Components/Profile/ProfileModal.razor`, `EditFieldModal.razor`, `ChangePasswordModal.razor`, `DeleteAccountModal.razor`
- Aktivera Profil-knappen i `src/Eatah.Client/Components/Shared/AppNavbar.razor`

## Verification

1. Profil-knappen öppnar modalen.
2. Edit displayName → sparas + uppdaterar `AuthState`.
3. Email-byte: nytt mail anländer, klick → bekräftat.
4. Lösenordsbyte fungerar.
5. Vänsökning + invite från modalen + accepterad blir foodbuddy.
6. Lämna household → workspace borta från switchern.
7. Radera konto → utloggad → kan inte logga in igen.

## Decisions

- Modal i modal undviks där det går (sub-modaler tillåts för separata flöden som "byt lösenord").
- Email-byte kräver bekräftelse av nya adressen för säkerhet.
- Delete confirmation = textfält + lösenord (dubbel barriär).

## Further Considerations

1. **Soft-delete vs hard-delete** — för enkelhet hard-delete i pre-launch. Lägg till soft-delete senare om GDPR-export behövs.
