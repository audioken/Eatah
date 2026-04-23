# Phase 0 – Foundation: Layout-shell, navbar, design-tokens, ikon­system

> **Status: ✅ Klar.** Implementerad. Behåll detta dokument som referens.

Skapar grundinfrastruktur som alla efterföljande UI-faser bygger på. Inga nya backend-endpoints. Inga datamodellsändringar.

## Steps

### 0.1 Design tokens & app.css

- Skapa `src/Eatah.Client/wwwroot/css/app.css` (utöka befintlig).
- Definiera `:root` CSS custom properties:
  - Färger: `--color-veg`, `--color-vegan`, `--color-fish`, `--color-poultry`, `--color-meat`, `--color-brand`, `--color-brand-dark`, `--color-bg-card`, `--color-bg-card-end`, `--color-navbar-bg` (`rgba(155, 186, 154, 0.25)`), `--color-text-on-card` (`#FFFFFF`), `--color-text-dark` (`#333E2F`).
  - Gradients: `--gradient-card-glass: linear-gradient(270deg, rgba(85,101,104,0.5346) 0%, rgba(174,207,214,0.8316) 100%)`.
  - Radius: `--radius-card: 15px`, `--radius-pill: 15px`.
  - Spacing/shadow tokens.
- Komponentklasser: `.eatah-card`, `.eatah-navbar`, `.eatah-navbar__item`, `.eatah-header`, `.eatah-pill`, `.eatah-pill--ok`, `.eatah-pill--bad`, `.eatah-category-icon` (+ modifiers `--vegan`/`--veg`/`--fish`/`--poultry`/`--meat`).
- Import i `src/Eatah.Client/wwwroot/index.html` efter Tailwind CDN.

### 0.2 SVG ikon-system

- Kopiera SVG från `resources/icons/` till `src/Eatah.Client/wwwroot/icons/` (build-resource).
- Ny komponent `src/Eatah.Client/Components/Shared/Icon.razor` med parametrar `Name`, `Size`, `CssClass`. Renderar `<svg><use href="/icons/{Name}.svg#icon" /></svg>` eller laddar SVG-innehåll inline för `currentColor`-stöd.
- Mappning av semantiska namn: `vegan→seedling-solid`, `vegetarian→carrot-solid`, `fish→fish-solid`, `poultry→drumstick-bite-solid`, `meat→bacon-solid`, `dice→dice-solid`, `magic→wand-magic-sparkles-solid`, `bell→...`, `basket→...`, `chat→comments-solid`, `profile→circle-user-solid`, `calendar→calendar-days-solid`, `trash→...`, `edit→...`, `eye→...`, `eye-off→...`, `chevron-down→angle-down-solid`.
- **Saknade SVG** (att lägga till i `resources/icons/` innan kopiering): `bell-solid`, `basket-shopping-solid`, `trash-solid`, `pen-to-square-solid`, `eye-solid`, `eye-slash-solid`, `xmark-solid`, `plus-solid`, `house-solid`, `user-solid`. Hämtas från Font Awesome free.

### 0.3 Modal-infrastruktur

- `src/Eatah.Client/Services/ModalService.cs`: scoped DI service med metoder `Show<TComponent>(parameters)`, `Close(result)`, `OnChange` event.
- `src/Eatah.Client/Components/Shared/ModalHost.razor`: lyssnar på `ModalService.OnChange`, renderar aktiv modal med backdrop, focus-trap, ESC-stängning, body-scroll-lock.
- Ny base-klass `EatahModalBase : ComponentBase` med `[CascadingParameter] ModalContext Context` så modaler kan stänga sig själva.
- Mountas i `src/Eatah.Client/Shared/MainLayout.razor` som syskon till `@Body`.

### 0.4 Bakgrundsbild

- Kopiera `resources/images/bg.png` → `src/Eatah.Client/wwwroot/images/bg.png` (build-resource).
- I `app.css`: `body::before { content: ''; position: fixed; inset: -22px -16px; background: url('/images/bg.png') center/cover; filter: blur(4px); z-index: -1; }`.
- Säkerställ att `MainLayout` har transparent bakgrund så `bg.png` syns.

### 0.5 Header-komponent

- `src/Eatah.Client/Components/Shared/AppHeader.razor` (förberedd för fas 1 + 4).
- Layout: vänster = workspace-switcher slot (placeholder text "Personligt" tills fas 4), mitten = sid-titel slot (Dashboard fyller med "Vecka XX" i fas 1), höger = notis-bell slot (placeholder, aktiveras i fas 5).
- Använder `.eatah-header` klass.
- `[Parameter] public RenderFragment? Center { get; set; }` etc., så varje sida kan injicera innehåll.

### 0.6 Footer-navbar

- `src/Eatah.Client/Components/Shared/AppNavbar.razor`.
- 5 items, vänster→höger: **Kost** (modal-trigger, fas 2), **Köplista** (sida, fas 7), **Matplan** (sida `/`, aktiv default), **Chat** (modal-trigger, fas 8), **Profil** (modal-trigger, fas 6).
- Datadriven från lista `NavItem(Label, IconName, Action)` där `Action` är antingen `NavigateTo(url)` eller `OpenModal<T>()`. Inaktiva (icke-implementerade) items renderas men gör no-op + visar "Kommer snart"-toast tills fasen som äger dem landar.
- Aktiv item får brand-färgad ikon + label, övriga grå.
- CSS från användaren: `.eatah-navbar { position: fixed; bottom: 0; left: 0; right: 0; background: rgba(155,186,154,0.25); border-radius: 15px 15px 0 0; backdrop-filter: blur(8px); padding-bottom: var(--safe-area-inset-bottom); }`.

### 0.7 Ta bort gammal navigering

- Ersätt sidebar i `src/Eatah.Client/Shared/MainLayout.razor`: ta bort hamburger + sidebar-state. Ny struktur: `<AppHeader />` överst, `<main>@Body</main>`, `<AppNavbar />` nederst, `<ModalHost />` ovanpå.
- `src/Eatah.Client/Shared/NavMenu.razor`: ta bort eller töm (ingen sidebar längre).
- Säkerställ att `<main>` har `padding-bottom` för navbar-höjd + safe-area, `padding-top` för header-höjd.

## Relevant files

- `src/Eatah.Client/wwwroot/css/app.css` — utöka med tokens + komponentklasser
- `src/Eatah.Client/wwwroot/index.html` — säkerställ att app.css laddas, ev. ta bort overlap med Tailwind theme
- `src/Eatah.Client/Shared/MainLayout.razor` — strukturell omskrivning
- `src/Eatah.Client/Shared/NavMenu.razor` — pensioneras
- `src/Eatah.Client/MauiProgram.cs` — registrera `ModalService`
- Nya: `Components/Shared/{Icon,ModalHost,AppHeader,AppNavbar}.razor`, `Services/ModalService.cs`, `Components/Shared/EatahModalBase.cs`

## Verification

1. App startar på Windows MAUI-target och visar `bg.png` blurrad bakom UI.
2. Navbar syns nederst, alla 5 items renderas med rätt ikon+label.
3. Kost/Chat/Profil triggar toast "Kommer snart" (placeholder för fas 2/8/6).
4. Matplan-knappen är aktiv på `/`, Köplista-knappen navigerar (även om sidan visar "Kommer snart" i fas 0).
5. Header visar "Personligt" till vänster och "Eatah" till höger som platshållare.
6. `ModalService.Show<TestModal>()` (manuellt anrop från Dashboard.razor) öppnar modal med backdrop, ESC stänger.
7. Ingen sidobar-hamburgermeny kvar någonstans.

## Decisions

- Navbar-knappar är inte separata routes per knapp — modal-triggers anropar `ModalService` direkt. Detta håller routing-tabellen ren.
- Ikoner laddas via `<img src>` eller inline-SVG? **Inline-SVG via cached fetch** för att stödja `currentColor`. Cache i `IconService` (memory). _Faktisk implementation: CSS-mask-baserad `<span>` med `currentColor` som `background-color` — ingen JS interop nödvändig._
- Tailwind CDN behålls. `app.css` är komplement, inte ersättare.

## Further Considerations

1. **Body-scroll-lock i MAUI WebView** — kan kräva JS-interop. Verifiera att `document.body.style.overflow = 'hidden'` fungerar i Android/iOS WebView. Plan B: en CSS-klass på `<html>`.
2. **Safe-area-bottom på navbar** — befintlig `ISafeAreaInsetsProvider` injicerar CSS-vars, återanvänd `var(--safe-area-inset-bottom, 0px)`.

## Implementation notes (fas faktiskt körd)

- ESC-to-close i ModalHost är **inte** wired up ännu — endast backdrop-klick stänger. Liten gap att lyfta i fas 1 eller 6.
- 27 SVG ikoner finns i `wwwroot/icons/` (17 kopierade + 10 nya tagna från FontAwesome free).
- `ToastService` + `ToastHost` också skapade i denna fas (ej explicit i ursprungsplanen — behövs för "Kommer snart"-toast).
- `MauiProgram.cs` registrerar `ModalService` och `ToastService` som **singletons** (inte scoped, eftersom MAUI-host).
