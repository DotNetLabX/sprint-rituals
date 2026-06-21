# Dark / Light Theme

**Traces to:** None (new capability, not in product spec)
**Source:** Scratch — user feedback on visual polish
**Dependencies:** None
**Status:** Done
**Plan:** [`docs/specs/F31-DarkLightTheme/delivery/plan.md`](../delivery/plan.md)

---

## Purpose

Reflekt's interface is functional but visually plain — a white, flat design that lacks personality and feels dated next to modern collaboration tools. Adding a dark/light theme with dark as the default modernizes the look, reduces eye strain during long retro sessions, and establishes visual cohesion with Fokus (the companion app in the same suite). The theme system uses shared foundational tokens across the suite while preserving each app's identity through its own accent color.

## Design System

### Suite Cohesion Principle

Reflekt and Fokus are part of the same product suite. Users navigating between them should feel they're in the same family without confusing which app they're in. This is achieved by:

- **Shared foundation:** Surface colors, border system, text hierarchy, and toggle mechanism are identical across the suite.
- **Distinct accent:** Reflekt uses indigo; Fokus uses periwinkle. The accent color is the primary "you are here" signal.
- **Domain-specific personality:** Reflekt's retro column colors (green, red, amber, etc.) are unique to Reflekt and provide additional differentiation.

### Dark Theme Palette

Surface layers create depth through tonal steps — no gradients, no heavy shadows.

| Token | Value | Role |
|-------|-------|------|
| Page background | #0a0a0f | Root page — near-black with cold blue cast |
| Card / panel surface | #13131a | Cards, header, raised containers |
| Elevated surface | #1c1c26 | Dropdowns, modals, tooltips |
| Default border | #2a2a38 | Card edges, dividers — subtle warm-indigo |
| Subtle border | #1e1e2a | Nearly invisible separators |
| Primary text | #f0f0f5 | Headlines, values — slightly warm white |
| Secondary text | #a0a0b8 | Labels, metadata — mid-grey with blue tint |
| Muted text | #60607a | Timestamps, helper text, placeholders |
| Accent | #6366f1 | Active states, primary actions — Reflekt indigo |
| Accent hover | #4f46e5 | Hover/pressed state of accent elements |

### Light Theme Palette

Light mode uses warm-tinted whites, not pure white. Cards float above the page background through tonal contrast, not shadows.

| Token | Value | Role |
|-------|-------|------|
| Page background | #f4f4f8 | Root page — soft lavender-grey |
| Card / panel surface | #ffffff | Cards, header, raised containers |
| Elevated surface | #eaeaf0 | Dropdowns, modals, tooltips |
| Default border | #e2e2ea | Card edges, dividers |
| Subtle border | #ededf3 | Nearly invisible separators |
| Primary text | #1a1a2e | Headlines, values |
| Secondary text | #6b6b80 | Labels, metadata |
| Muted text | #9b9bab | Timestamps, helper text, placeholders |
| Accent | #6366f1 | Active states, primary actions — same indigo |
| Accent hover | #4f46e5 | Hover/pressed state of accent elements |

### Retro Column Colors in Dark Mode

Reflekt's retro columns use distinct colors for headers and background tints. In dark mode, the light background fills used in light mode would be visually jarring. Instead:

- **Column header accent:** The full saturated color (e.g., green #10b981) — unchanged between themes.
- **Column background tint:** The saturated color at 10-15% opacity on the dark surface (e.g., green at 10% opacity on #13131a). This provides a subtle wash of color without blinding contrast.
- **Column text accent:** The full saturated color — unchanged between themes.

This applies to all 8 palette colors (green, red, amber, blue, purple, teal, pink, slate) and the fixed Action Items blue.

**Column color token strategy:** Each column color has a "light" variant token (used for column backgrounds). These tokens become theme-aware — they resolve to different values depending on the active theme:
- **Light mode:** The current fixed hex values (e.g., green-light = #d1fae5).
- **Dark mode:** The saturated color at low opacity over the dark surface (e.g., green at 10% opacity).

Existing component code that references the "light" variant continues to work unchanged — the token resolves to the correct value per theme. No component-level conditional logic is needed.

### Semantic Color Variants (Badges, Alerts, Status)

Status badges, warning banners, and status indicators use semantic colors (success, danger, warning, info). These need distinct dark-mode treatments:

| Variant | Light mode | Dark mode |
|---------|-----------|-----------|
| Success (Active badge, done status) | Light green background, dark green text | Green at 10% opacity background, bright green text |
| Danger (Closed badge, delete actions) | Light red background, dark red text | Red at 10% opacity background, bright red text |
| Warning (alerts, carried-over status) | Light amber background, dark amber text | Amber at 10% opacity background, bright amber text |
| Info (informational badges) | Light blue background, dark blue text | Blue at 10% opacity background, bright blue text |
| Default (Draft badge, neutral) | Light grey background, dark grey text | Grey at 10% opacity background, light grey text |
| Primary (accent-colored elements) | Light indigo background, dark indigo text | Indigo at 10% opacity background, bright indigo text |

The pattern is consistent: in dark mode, use the semantic color at low opacity for backgrounds and the full/bright variant for text. This matches the column color approach and the alpha transparency pattern used in the suite.

### Card Design

- **Card background:** Uses the card/panel surface color (#13131a in dark, #ffffff in light).
- **Card border:** Uses the default border color (#2a2a38 in dark, #e2e2ea in light).
- **Card hover:** Subtle background shift toward the elevated surface color, with a slight vertical lift (1-2px translate).
- **Shadows:** In dark mode, cards use borders only — no drop shadows (depth comes from border and background contrast, matching the suite pattern). In light mode, existing card shadows are preserved — this feature does not change the current light-mode card appearance beyond the palette alignment.

### Scrollbar Styling

In dark mode, scrollbars are styled to blend with the surface:
- Thumb color matches the default border color.
- Track is transparent.
- Thumb width is narrow (6px).

Light mode uses browser-default scrollbars.

### Token Mapping

Spec tokens map to CSS variable names. Existing variables are reused where applicable; new ones are introduced for missing concepts.

| Spec Token | CSS Variable | Existing? |
|------------|-------------|-----------|
| Page background | `--color-background` | Yes (redefine per theme) |
| Card / panel surface | `--color-surface` | Yes (redefine per theme) |
| Elevated surface | `--color-surface-raised` | Yes (redefine per theme) |
| Default border | `--color-border` | New |
| Subtle border | `--color-border-subtle` | New |
| Primary text | `--color-text-primary` | New |
| Secondary text | `--color-text-secondary` | New |
| Muted text | `--color-text-muted` | New |
| Accent | `--color-primary-500` | Yes (unchanged, not theme-dependent) |
| Accent hover | `--color-primary-600` | Yes (unchanged, not theme-dependent) |
| Scrollbar thumb | `--color-scrollbar-thumb` | New |
| Focus ring offset | `--color-focus-offset` | New (matches page background per theme) |

Column color tokens (`--color-col-{name}`) remain unchanged (theme-independent saturated values). Column light tokens (`--color-col-{name}-light`) become theme-aware — different values per theme as described in the column color strategy above.

## User Flows

```
Flow 1: Toggle theme
1. User sees a small icon button in the top-right area of the app header (next to the user avatar).
2. In dark mode, the button shows a sun icon. In light mode, it shows a moon icon.
3. User clicks the button.
4. The entire interface transitions to the opposite theme instantly (no page reload).
5. The preference is saved and persists across browser sessions and page refreshes.
```

```
Flow 2: First visit (dark default)
1. A new user visits Reflekt for the first time (no saved preference).
2. The app renders in dark mode by default.
3. The theme toggle shows a sun icon (indicating "switch to light").
```

```
Flow 3: Returning visit with saved preference
1. A returning user visits Reflekt.
2. The app reads the saved theme preference before rendering.
3. The app renders in the user's preferred theme with no flash of the wrong theme.
```

```
Flow 4: Retro board in dark mode
1. SM or participant is in the retro room with dark mode active.
2. Column headers display their configured color (green, red, amber, etc.) at full saturation.
3. Column backgrounds show a subtle tinted wash of the column color (low opacity on the dark surface).
4. Cards are dark-surfaced with subtle borders.
5. The phase stepper, timer, and all controls use the dark theme tokens.
6. All text remains readable — primary text is near-white, secondary text is mid-grey.
```

```
Flow 5: Sprint list and creation in dark mode
1. SM views the sprint list in dark mode.
2. Sprint cards use the dark card surface with borders.
3. The "New Sprint" form uses dark input fields with visible borders and light text.
4. Color swatch pickers for column colors remain vibrant — they show the full saturated color regardless of theme.
5. Badges (Active, Closed, Draft) use their semantic colors with appropriate contrast on the dark surface.
```

## Business Rules

1. The app defaults to dark mode for first-time visitors with no saved preference.
2. Theme preference is stored per-browser (not per-user account). A user on two different browsers may have different theme preferences.
3. Theme changes take effect instantly — no page reload required.
4. The selected theme applies to the entire application — there is no per-page or per-component theme override.
5. There must be no visible flash of the wrong theme on page load. The theme must be resolved before the first paint — this requires a blocking inline script in the HTML shell that reads the saved preference and sets the theme class on the root element before the application framework mounts.
6. All text must meet WCAG AA contrast ratio (4.5:1 for body text, 3:1 for large text) in both themes.
7. The retro column color palette (green, red, amber, blue, purple, teal, pink, slate) must remain visually distinct and identifiable in both themes.
8. Color swatch pickers in the sprint creation form always display the full saturated color regardless of the active theme.
9. Status badges (Active, Closed, Draft) must remain readable in both themes.
10. The theme toggle button is visible on all pages where the app header appears.
11. The toggle icon reflects the action it will take: sun icon in dark mode (will switch to light), moon icon in light mode (will switch to dark).

## Acceptance Criteria

- [ ] App renders in dark mode by default on first visit (no saved preference).
- [ ] A theme toggle button (sun/moon icon) is visible in the app header on all pages.
- [ ] Clicking the toggle switches between dark and light themes instantly without page reload.
- [ ] Theme preference persists across page refreshes and browser sessions.
- [ ] No flash of wrong theme on page load — theme is resolved before first paint.
- [ ] Dark mode: page background is near-black (#0a0a0f), cards use dark surface (#13131a) with subtle borders (#2a2a38).
- [ ] Light mode: page background is tinted white (#f4f4f8), cards are white (#ffffff) with light borders (#e2e2ea).
- [ ] Retro column headers display at full color saturation in both themes.
- [ ] Retro column backgrounds use low-opacity tints of the column color in dark mode.
- [ ] All body text meets WCAG AA contrast ratio (4.5:1) in both themes.
- [ ] Sprint list, sprint creation form, and sprint detail panel render correctly in both themes.
- [ ] Retro room (board, phase stepper, timer, participant list, card input) renders correctly in both themes.
- [ ] Report view renders correctly in both themes.
- [ ] Welcome/login page renders correctly in both themes.
- [ ] Team dashboard and team selector render correctly in both themes.
- [ ] Modals (delete team, confirmation dialogs) render correctly in both themes.
- [ ] Toast notifications render correctly in both themes.
- [ ] Empty states render correctly in both themes.
- [ ] Color swatch pickers display full saturated colors regardless of active theme.
- [ ] Status badges (Active, Closed, Draft) are readable in both themes.
- [ ] Loading spinner is visible in both themes.
- [ ] Scrollbars in dark mode use a subtle, blended style (narrow thumb, transparent track).
- [ ] The join form (name picker for participants) renders correctly in both themes.
- [ ] Previous actions panel (action items from last sprint) renders correctly in both themes.
- [ ] Closed-sprint warning banner renders correctly in both themes.
- [ ] Focus rings on interactive elements (buttons, inputs) are visible in both themes — no white gaps around focused elements on dark backgrounds.
- [ ] Inline column name editing (SM clicks column header) uses theme-appropriate input styling in both themes.

## Out of Scope

- **Per-user server-side theme preference** — Theme is stored in the browser only. Server-side storage would require a user settings API and adds complexity with no clear benefit for v1. Revisit if users frequently switch devices and complain about re-setting their theme.
- **System preference detection (prefers-color-scheme)** — Could auto-detect OS dark/light preference. Deferred because the default (dark) is already the likely preference for the target audience (developers/engineering teams). Revisit if non-technical users adopt Reflekt.
- **Theme transition animation** — No fade or crossfade between themes. The switch is instant. Animations add complexity and can cause layout flicker with CSS variable swaps.
- **Per-component or per-page theme override** — The theme is global. No "dark header with light content" or similar split-theme layouts.
- **Custom accent color selection** — The accent is fixed at Reflekt indigo. User-selectable accent colors are a v3 personalization feature.
- **High contrast mode** — A third theme variant for accessibility. Deferred — WCAG AA compliance in both themes is sufficient for now.
