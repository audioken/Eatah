# Eatah – Refaktorplan (9 faser)

Detta är planen för en stor uppdatering av Eatah-applikationen, indelad i 9 faser. Varje fas har ett eget dokument med detaljerade steg, beroenden och verifikationspunkter.

## Status

| #   | Fas                                                                     | Status         |
| --- | ----------------------------------------------------------------------- | -------------- |
| 0   | [Foundation](./phase-0-foundation.md)                                   | ✅ Klar        |
| 1   | [Matplan-redesign](./phase-1-matplan-redesign.md)                       | ✅ Klar        |
| 2   | [Diet Profile som modal](./phase-2-diet-profile-modal.md)               | ⏳ Ej påbörjad |
| 3   | [Authentication](./phase-3-auth.md)                                     | ⏳ Ej påbörjad |
| 4   | [Workspaces](./phase-4-workspaces.md)                                   | ⏳ Ej påbörjad |
| 5   | [Friends & Notifications](./phase-5-friends-notifications.md)           | ⏳ Ej påbörjad |
| 6   | [Profile-modal](./phase-6-profile-modal.md)                             | ⏳ Ej påbörjad |
| 7   | [Ingredients/Pantry/Shopping](./phase-7-ingredients-pantry-shopping.md) | ⏳ Ej påbörjad |
| 8   | [Chat med SignalR](./phase-8-chat-signalr.md)                           | ⏳ Ej påbörjad |

Master-översikten finns i [plan.md](./plan.md).
Planerade tillägg till `.github/copilot-instructions.md` per fas finns i [copilot-instructions-updates.md](./copilot-instructions-updates.md).

## Hur man startar nästa fas i en ny chat-session

1. Öppna en **ny Copilot-chat** i denna workspace.
2. Skriv:

   > Kör Fas N enligt `docs/plans/phase-N-<namn>.md`. Läs hela planen, bekräfta att du förstår, och kör sedan implementationen i taget. Uppdatera `.github/copilot-instructions.md` enligt `docs/plans/copilot-instructions-updates.md` när fasen landar. Markera fasen som klar i `docs/plans/README.md` när du är färdig.

3. Bekräfta att agenten har förstått innan implementationen kör igång.

## Varför ny session per fas

- Faserna är medvetet designade som självständiga arbetspaket med tydliga beroenden.
- Att starta fresh håller kontext-fönstret rent och minskar risken för kompaktering.
- Plandokumenten (denna mapp) + `.github/copilot-instructions.md` + `/memories/` ger nya sessioner all bakgrund de behöver.

## Övergripande beslut (fastställda)

- **Auth-hash:** ASP.NET Identity default (PBKDF2)
- **E-post:** MailKit, Mailtrap (dev) / Brevo (prod)
- **Data-migration:** Drop & re-seed (pre-launch)
- **Workspace-modell:** 1 obligatoriskt Personal + max 1 Household
- **CSS-strategi:** Tailwind CDN + `wwwroot/css/app.css` med design-tokens
- **Ingredient-DB:** Globalt system-register + per-user custom
