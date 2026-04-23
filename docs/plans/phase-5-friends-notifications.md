# Phase 5 – Friends (Foodbuddies) + Notifications

Foodbuddy = någon som delar Household med dig. Inbjudningssystem + notiser i header.

## Steps

### 5.1 Domän

- `FriendRequest` entitet (i `Eatah.Domain/Entities/`):
  - `Id` (Guid), `FromUserId`, `ToUserId`, `HouseholdWorkspaceId` (vilket household som inviten gäller — den inbjudande äger), `Status` (enum `RequestStatus { Pending, Accepted, Rejected, Cancelled }`), `CreatedAt`, `RespondedAt`.
  - Unique constraint: (FromUserId, ToUserId, HouseholdWorkspaceId, Status=Pending) — endast en pending åt gången.
- `Notification` entitet:
  - `Id`, `UserId` (mottagare), `Type` (enum `NotificationType { FriendRequest, FriendRequestAccepted, ChatMessage, ChatMention }`), `Payload` (JSON-string med t.ex. requestId, fromDisplayName), `CreatedAt`, `ReadAt` (nullable).

### 5.2 Friends-feature

`src/Eatah.Api/Features/Friends/`:

- `SearchUsers.cs` — `GET /api/users/search?q={query}` → returnerar topp 10 users med `DisplayName.Contains(q)`, exklusive självet och redan-pending/accepterade. Min 2 chars.
- `SendFriendRequest.cs` — `POST /api/friends/requests` body `{ toUserId }` → kräver att avsändare har Household; om ej → returnera `friend_request_no_household` med hint att skapa Household först, alternativt skapa Household automatiskt vid första invite (beslut: skapa automatiskt med default-namn "Hushåll").
- `RespondToFriendRequest.cs` — `POST /api/friends/requests/{id}/respond` body `{ accept: bool }`. Vid accept: lägg till `WorkspaceMember` rad för mottagaren i avsändarens Household. Mottagaren får inte ha redan ett Household — annars `workspace_household_already_exists`.
- `CancelFriendRequest.cs` — `DELETE /api/friends/requests/{id}` (avsändaren).
- `GetMyFriends.cs` — `GET /api/friends` → users i samma Household.

### 5.3 Notifications-feature

`src/Eatah.Api/Features/Notifications/`:

- `GetMyNotifications.cs` — `GET /api/notifications?unreadOnly=true` → senaste 50.
- `MarkAsRead.cs` — `POST /api/notifications/{id}/read`.
- `MarkAllAsRead.cs` — `POST /api/notifications/read-all`.
- `INotificationService` (intern, anropas från andra services):
  - `NotifyAsync(Guid userId, NotificationType type, object payload, CancellationToken ct)` → skriver till DB + (om SignalR uppe i fas 8) pushar via hub.

### 5.4 Notifications-realtid

- I fas 5 räcker **polling**: klient hämtar `GET /api/notifications?unreadOnly=true` var 30:e sekund + vid app foregrounding.
- Förbered för SignalR-push i fas 8 men implementera inte här.

### 5.5 Klient

- `src/Eatah.Client/Services/NotificationState.cs`:
  - Properties: `IReadOnlyList<NotificationResponse> Notifications`, `int UnreadCount`, `event OnChange`.
  - Background `Timer` 30s polling.
- `src/Eatah.Client/Components/Shared/NotificationBell.razor`:
  - Renderas i headerns högerslot (ersätter platshållaren från fas 0).
  - Badge med `UnreadCount` om > 0.
  - Klick → liten dropdown/popover (inte modal) med notiserna. Varje notis är klickbar:
    - `FriendRequest` → öppnar `FriendRequestModal` med Acceptera/Avvisa.
    - `FriendRequestAccepted` → markeras läst, navigerar ev. till Profile-modalen.
- `src/Eatah.Client/Components/Friends/FriendRequestModal.razor`:
  - Visar avsändarens displayName + "vill bli din foodbuddy och dela sin meny med dig."
  - Knappar: Acceptera, Avvisa.
- I Profile-modal (fas 6 — placeholder här) finns sök-rutan; skapas där.

### 5.6 Felkoder

- `friend_request_not_found`, `friend_request_already_pending`, `friend_request_self`, `friend_request_cannot_invite_household_member`, `friend_request_no_household`.
- `notification_not_found`, `notification_access_denied`.

### 5.7 Tester

- Unit: `FriendsService` invarianter (kan inte invita sig själv, kan inte invita befintlig medlem, household auto-skapas).
- Integration: full flow A bjuder B → B accepterar → B är member i A:s household → B:s `WorkspaceState` reflekterar nytt val.

## Relevant files

- Nya domän­entiteter
- Nya feature-mappar `Features/Friends/`, `Features/Notifications/`
- Klient: `Services/NotificationState.cs`, `Components/Shared/NotificationBell.razor`, `Components/Friends/FriendRequestModal.razor`
- Migration `AddFriendsAndNotifications`

## Verification

1. User A söker user B → träff i sök-resultatet.
2. A skickar invite → B får notis (efter ≤30s polling).
3. B accepterar → A får notis "B accepterade din inbjudan".
4. B:s `WorkspaceSwitcher` visar nu Household och kan switcha.
5. A och B ser samma data i Household.
6. Försök bjuda in en redan-Household-medlem → 409.
7. Notifications mark-as-read fungerar; bell badge uppdateras.

## Decisions

- **Household skapas automatiskt** vid första friend invite (om avsändaren inte har ett) → enklare UX än att tvinga skapa först.
- **Mottagaren måste vara household-fri** för att kunna acceptera (max 1 Household-regel).
- **Notifications via polling** i denna fas; SignalR-push läggs till transparent i fas 8.

## Further Considerations

1. **Avgå från Household** — sköts via `LeaveHousehold` (fas 4). När siste lämnar → household raderas och plan/data försvinner. Visa varning.
2. **Foodbuddy-listan är platt** — alla i samma Household är foodbuddies med varandra. Ingen riktningsspecificitet.
