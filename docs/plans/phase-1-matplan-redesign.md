# Phase 1 – Matplan-redesign

Bygger om Dashboard så headern visar veckonummer, och en ny **Kostprofil-card** ersätter dagens score-sektion längst ner. Beteendet (randomisering + evaluering) är oförändrat — endast omplacering och visuell förnyelse.

## Steps

### 1.1 Header-innehåll på Dashboard

- I `src/Eatah.Client/Pages/Dashboard.razor`: fyll `AppHeader.Center` med `"Vecka {WeekNumber}"` (från `WeeklyPlanResponse.WeekNumber`).
- Vänster (workspace-switcher): hårdkodad text "Personligt" (riktig switcher kommer i fas 4).
- Höger: notisbell-platshållare (utan badge, ingen handler — aktiveras i fas 5).
- Använd `<HeadContent>` eller cascade-pattern — välj cascade via `AppHeader` parametrar.

### 1.2 Ny komponent: `DietProfileSelectorCard`

Plats: `src/Eatah.Client/Components/WeeklyPlan/DietProfileSelectorCard.razor`.

**Props:**

- `[Parameter] Guid PlanId`
- `[Parameter] EventCallback<Guid> OnRandomize` (vidarebefordrar profil-id)
- `[Parameter] EventCallback OnProfileChanged`

**Internt state:**

- `IReadOnlyList<DietProfileResponse> profiles` — laddas via `ApiClient.GetDietProfilesAsync()`
- `Guid? selectedProfileId`
- `DietEvaluationResponse? evaluation` — laddas via `ApiClient.EvaluateWeeklyPlanAsync(planId, profileId)` när profil byts ELLER när parent signalerar att planen ändrats
- Persistens: spara senast valda `selectedProfileId` i `Preferences` (MAUI `Preferences.Set/Get`) så den minns mellan sessions

**Layout (efter användarens CSS-spec):**

- `.eatah-card` med glass-gradient, 70px hög, full bredd minus marginal.
- Vänster sektion (~123px):
  - Titel "Kostprofil" + dropdown med profilnamn (HTML `<select>` styled) → `selectedProfileId`. Chevron-ikon till höger.
  - Pill med procent: visar `evaluation.OverallScore.ToString("0")%`. Pill bg-färg interpoleras: röd vid 0%, gul vid 50%, grön (`#A2AE00`) vid 100%. Använd HSL-interpolation eller `linear-gradient(90deg, ...)` med `width:{score}%`.
- Mitten-sektion (~138px): 5 kategori-ikoner i rad, 28px bredd vardera.
  - Ikon = `.eatah-category-icon--{category}` (cirkulär bakgrund i kategori-färg) + vit `<Icon>` ovanpå.
  - Pill under varje ikon: `{Min}-{Max}` om regel finns, dimmad om saknas. Pill bg-färg: grön (`#5AAE00`) om antal i veckan är inom [Min,Max], röd (`#F58383`) om utanför. Bygg på `evaluation.RuleResults` matchat på `Category`.
- Höger sektion (~30px): rund randomize-knapp, cyan (`#4ED4F2`), tärnings-ikon (`<Icon Name="dice" />`), klick → `OnRandomize.InvokeAsync(selectedProfileId)`.

### 1.3 Dashboard refactor

- Ta bort gammal score-sektion längst ner (`DietScoreGauge` + `RuleResultCard`-rad).
- Ta bort den gamla "Slumpa veckan"-knappen om den ligger separat — funktionaliteten flyttas in i kortets randomize-knapp.
- AI-genereringsknappen (magic wand) — **behålls** men flyttas till samma rad som randomize-knappen i kortet (matchar bilden där en lila magic-knapp ligger jämte tärningen vid TORSDAG-raden). Bilden visar att magic-wand syns kontextuellt vid en dag — bekräfta med användaren om det ska vara per-dag eller globalt.
- Lägg `<DietProfileSelectorCard PlanId="@plan.Id" OnRandomize="HandleRandomize" />` direkt under header.
- `HandleRandomize(Guid? profileId)` anropar `ApiClient.RandomizeWeekAsync(planId, profileId)` (befintligt endpoint).
- Efter randomize: trigga `OnProfileChanged` så kortet om-evaluerar.

### 1.4 Re-evaluering vid plan-ändringar

- Dashboard exponerar ett event `PlanChanged` som triggas efter assign/clear/randomize.
- `DietProfileSelectorCard` lyssnar och kör om `EvaluateWeeklyPlanAsync`.
- Eller enklare: parent ger ner `[Parameter] WeeklyPlanResponse Plan` → `OnParametersSetAsync` triggar evaluering vid plan-version-byte (jämför `plan.Days.Select(d => d.MealId)`).

### 1.5 Kategori-badges på dagskort (bilden)

- I `src/Eatah.Client/Components/WeeklyPlan/DayCard.razor`: säkerställ att kategori-pillen uppe i högra hörnet på varje dagskort använder samma färgsystem som design-tokens (`--color-vegan` etc.), och visar svensk text: Veganskt, Vegetariskt, Fisk, Fågel, Rött.

## Relevant files

- `src/Eatah.Client/Pages/Dashboard.razor` — header-innehåll, ta bort gamla score-sektionen, montera kort
- `src/Eatah.Client/Components/WeeklyPlan/DayCard.razor` — verifiera kategori-pill-styling mot tokens
- Befintliga som inte längre används direkt på Dashboard: `DietScoreGauge.razor`, `RuleResultCard.razor` — behåll filerna tills fas 2 klar (ev. återanvänds i diet-modal-detaljvy), men ta bort referenser från Dashboard
- Ny: `Components/WeeklyPlan/DietProfileSelectorCard.razor` (+ ev. `.razor.css` för komponent-scoped styling)
- `src/Eatah.Client/Services/ApiClient.cs` — inga nya metoder

## Verification

1. Dashboard renderar headern med "Vecka {N}" centrerat (manuellt jämför med bilden).
2. Profil-kortet visar dropdown med alla profiler från API; default-val sparas via `Preferences`.
3. Procent-pillen ändrar färg och bredd när annan profil väljs.
4. Kategori-pills under varje ikon färgas grön när antal är i intervall, annars röd.
5. Tärnings-knappen randomiserar veckan med vald profil → kortet om-evaluerar.
6. Inga referenser till gamla score-komponenterna kvar i Dashboard.
7. Snapshot-test (manuellt) av kortet matchar design-spec (50px ikoner, 70px höjd, glass-gradient).

## Decisions

- Dropdownen för profil är HTML-`<select>` (inte custom) i fas 1 för enkelhet. Vid behov ersätts den med custom dropdown i fas 2.
- Magic-wand-knappen lämnas där den är på dag-nivå (befintligt beteende). Om bilden tolkas som global magic-knapp, hanteras det i en mindre uppföljning.
- Profil-val persisteras endast lokalt (MAUI Preferences). Efter fas 4 kan val flyttas till workspace-state om det önskas.

## Further Considerations

1. **Färg-interpolering på procent-pillen** — använd `color-mix()` i CSS (modern, stöd i WebView2/iOS 16.4+/Android Chrome 111+) eller SVG-gradient. Rekommendation: `color-mix(in srgb, var(--color-pill-bad) {100-score}%, var(--color-pill-good))`.
2. **Tom plan / inga profiler** — visa "Skapa en kostprofil" som CTA i kortet (öppnar fas 2-modalen). I fas 1: visa bara "Ingen profil vald".
