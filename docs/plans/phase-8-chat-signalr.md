# Phase 8 – Chat med SignalR + reaktioner + shopping-list-integration

Realtidschat mellan foodbuddies i samma Household. Gruppchat + 1-på-1. Reaktioner via long-press. Integration med Köplista.

## Steps

### 8.1 Paket

- `Microsoft.AspNetCore.SignalR` (ingår i ASP.NET Core).
- Klient: `Microsoft.AspNetCore.SignalR.Client` v10.

### 8.2 Domän

- `ChatThread`:
  - `Id`, `WorkspaceId` (Household), `Type` (enum `ThreadType { Group, Direct }`), `CreatedAt`
  - `Participants` (List<ChatParticipant>) — för Direct: 2 users; för Group: alla i Household automatiskt.
- `ChatParticipant`:
  - `ThreadId`, `UserId` (composite PK), `JoinedAt`, `LastReadAt`.
- `ChatMessage`:
  - `Id`, `ThreadId`, `SenderId`, `Body` (text, max 4000), `CreatedAt`, `EditedAt` (nullable), `DeletedAt` (nullable, soft-delete).
  - Optional `MessageKind` (enum: `Text`, `ShoppingRequest`) + JSON-payload för structured payloads (lista av ingredient-ids).
- `ChatReaction`:
  - `MessageId`, `UserId`, `ReactionType` (enum: `ThumbsUp, ThumbsDown, Laugh, Sad, Heart`), `CreatedAt`. Composite PK.

### 8.3 Chat-feature

`src/Eatah.Api/Features/Chat/`:

- Endpoints (`/api/chat`):
  - `GetThreads.cs` — lista threads i current workspace + senaste meddelande + unread count.
  - `GetMessages.cs` — `GET /threads/{id}/messages?before={cursor}&limit=50` → paginated.
  - `SendMessage.cs` — `POST /threads/{id}/messages` body `{ body, kind?, payload? }`. Skriv till DB + push via hub.
  - `EditMessage.cs` — `PUT /messages/{id}` body `{ body }`. Endast egna. Sätter `EditedAt`. Push.
  - `DeleteMessage.cs` — `DELETE /messages/{id}`. Endast egna. Sätter `DeletedAt`. Push event så klienter visar "Meddelande borttaget".
  - `AddReaction.cs` — `POST /messages/{id}/reactions` body `{ type }`. Idempotent — om samma user+type finns: ta bort (toggle).
  - `MarkThreadRead.cs` — `POST /threads/{id}/read`.
- Auto-skapande av threads:
  - När Household får sin andra medlem → skapa Group-thread + N×(N-1)/2 Direct-threads. Trigger i `WorkspaceService` när member adderas.
- Service `ChatService` returnerar `Result<T>`. Validering: avsändaren måste vara Participant. Body inte tomt.

### 8.4 SignalR Hub

- `src/Eatah.Api/Features/Chat/ChatHub.cs`:
  - `[Authorize]` på Hub.
  - `OnConnectedAsync`: lägg connection till groups för varje thread user är participant i (`Groups.AddToGroupAsync(connectionId, $"thread:{threadId}")`).
  - Server-pushed events (anropas från `ChatService` efter writes):
    - `ReceiveMessage(MessageDto)`
    - `MessageEdited(MessageDto)`
    - `MessageDeleted(messageId)`
    - `ReactionChanged(messageId, reactionsSummary)`
  - Hub har inga client→server-metoder (allt går via REST + hub är push-only). Detta håller validering+error­hantering konsekvent.
- Map: `app.MapHub<ChatHub>("/hubs/chat").RequireAuthorization()`.

### 8.5 Notifications-integration

- Vid `SendMessage`: om mottagare inte är online (ingen aktiv hub-connection eller `LastReadAt` nyligen) → skapa `Notification` med type `ChatMessage`.
- `INotificationService.NotifyAsync` pushar via SignalR om online (annars bara DB).

### 8.6 Klient

- `src/Eatah.Client/Services/ChatHubClient.cs`:
  - Wrappar `HubConnection` (SignalR-klient).
  - Connect på app-start (efter auth), reconnect-policy.
  - Exponerar `event Action<MessageDto> OnMessageReceived` etc.
- `src/Eatah.Client/Services/ChatState.cs`:
  - Cache av threads + messages per thread.
  - Reagerar på hub events och uppdaterar cache + event för UI.
- `src/Eatah.Client/Components/Chat/ChatModal.razor`:
  - Triggas av Chat-knappen i navbar.
  - Layout: vänsterlist med threads (Group överst, Direct under). Höger: vald thread.
  - Meddelande-bubblor med avsändare + tid + reactions-rad under.
  - Long-press på en bubbla → reaction-picker (5 emojis horisontellt). Long-press = pointer down + 500ms timer i JS interop. Kan implementeras i C# med `@onpointerdown` + `Task.Delay` + cancel vid `@onpointerup`.
  - Edit-mode på egna meddelanden via long-press → meny "Redigera / Radera".
  - Input-rad nederst med skicka-knapp.
- `src/Eatah.Client/Components/Chat/ChatBubble.razor` — en meddelande­bubbla.
- `src/Eatah.Client/Components/Chat/ReactionPicker.razor`.

### 8.7 Köplista-integration

- I `ShoppingList.razor`: lägg till multi-select-läge.
  - "Markera"-knapp aktiverar checkboxar per item.
  - När ≥1 markerad: visa floating action-knappar:
    - "Be någon köpa" → öppnar mini-modal: välj thread (Group eller Direct) + free-text → skickar `ChatMessage` med `Kind=ShoppingRequest`, payload `{ ingredientIds: [...], note }`.
    - "Jag köper detta" → samma men med pre-fyllt meddelande "Jag köper:".
- I `ChatBubble`: om `Kind == ShoppingRequest`, rendera special-layout med ingrediens-lista + "Markera som köpt"-knapp (som via API checkar items i shopping list → flyttar till pantry).

### 8.8 Felkoder

- `chat_thread_not_found`, `chat_thread_access_denied`, `chat_message_not_found`, `chat_message_not_owned`, `chat_message_too_long`, `chat_reaction_invalid`.

### 8.9 Tester

- Unit: `ChatService` (ownership, validering, reaction toggle).
- Integration: full flow — A skickar → B får via hub (test med `WebApplicationFactory.Server.CreateClient` + `HubConnectionBuilder` mot test-hosten). Om SignalR-tester är komplicerade: mocka `IHubContext<ChatHub>` och verifiera att `ReceiveMessage` triggas.

## Relevant files

- Nya entiteter + migration
- Hela `Features/Chat/`
- Hub + map i `Program.cs`
- Klient: `Services/ChatHubClient.cs`, `ChatState.cs`, `Components/Chat/*`
- Aktivera Chat-knapp i navbar
- Uppdatera `ShoppingList.razor` med multi-select + chat-integration
- Aktivera SignalR-push i `INotificationService` (ersätter polling-fallback)

## Verification

1. A skickar meddelande → B ser direkt utan reload (SignalR push).
2. Long-press på bubbla → reaction-picker → välj 👍 → båda sidor ser reaktion.
3. A redigerar sitt meddelande → B ser uppdaterad text + "(redigerad)"-stämpel.
4. A raderar → B ser "Meddelande borttaget".
5. Notis dyker upp för B om hen har Chat-modalen stängd.
6. Markera ingredients i Köplista → "Be Hannah köpa" → meddelande dyker upp i Direct-thread → Hannah klickar "Markera som köpt" → items flyttas till pantry.
7. Reconnect: stäng API → klient försöker återansluta → meddelanden köas/syncas vid reconnect (SignalR built-in).

## Decisions

- **Hub är push-only**, alla writes går via REST. Enklare validering, samma error-handling.
- **Reactions via long-press** med pointer events + 500ms delay (cross-platform i Blazor).
- **Group-thread auto-skapas** vid Household-bildning.
- **Soft-delete på messages** så historik bevaras även om innehåll döljs.

## Further Considerations

1. **Read receipts** — `LastReadAt` per participant räcker för "lästa" indikator. Visa kryss/dubbelkryss à la WhatsApp.
2. **Typing indicators** — kräver client→hub-metod. Kan läggas till i v2.
3. **Bilduppladdning i chat** — utanför scope, v2.
4. **Push notifications när app är stängd** — utanför scope (kräver FCM/APNs).
