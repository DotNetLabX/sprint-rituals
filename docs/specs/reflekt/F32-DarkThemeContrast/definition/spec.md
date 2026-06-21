# Dark Theme Contrast & Visual Hierarchy

**Traces to:** F31 spec BR6 (WCAG AA contrast compliance)
**Source:** Scratch — user visual testing feedback after F31 shipped
**Dependencies:** F31-DarkLightTheme (complete)
**Status:** Done
**Plan:** [`docs/specs/F32-DarkThemeContrast/delivery/plan.md`](../delivery/plan.md)

---

## Purpose

F31 shipped a full dark/light theme with a well-structured token system. Post-launch visual testing revealed several contrast and hierarchy issues in dark mode that reduce readability and discoverability. The muted text token fails the spec's own WCAG AA requirement for body text, cards don't visually separate from their column backgrounds, and the participant's primary action ("I'm done") is visually weaker than the facilitator's "Next" button — creating a role-confusion risk. This spec defines the fixes needed to bring dark mode up to the contrast standard F31 committed to.

## User Flows

```
Flow 1: Participant reads the retro board in dark mode
1. Participant opens the retro board during the Writing phase.
2. Each column's cards are visually distinct from the column background — cards appear to float above the column surface.
3. The "Add a note..." input fields at the bottom of each column have visible borders, clearly indicating where to type.
4. Placeholder text and empty state messages ("No cards yet") are readable without straining.
```

```
Flow 2: Participant finishes writing
1. Participant has added their cards and is ready to signal completion.
2. The "I'm done" button is clearly visible and prominent — it reads as the participant's primary action, not a secondary control.
3. Participant clicks "I'm done."
4. The facilitator's readiness counter (e.g., "1/4 done") updates and is easily readable in the sprint header area.
```

```
Flow 3: Facilitator monitors progress
1. Facilitator views the retro board during the Writing phase.
2. The readiness counter shows how many participants have finished (e.g., "2/5 done") — legible at a glance.
3. The phase stepper shows "Writing" as active and the remaining phases (Grouping, Voting, Discussing, Reporting) as inactive but clearly visible — a first-time user can understand the multi-phase flow without explanation.
4. The "Next: Grouping" button is visible but does not visually overpower the participant-facing "I'm done" button.
```

## Business Rules

1. All text rendered using the muted text token must meet WCAG AA 4.5:1 contrast ratio against both the page background and the card surface in dark mode. Placeholder text in input fields may use 3:1 per common accessibility practice (placeholder text disappears on input), but all other uses of muted text — empty state messages, phase labels, metadata — must meet 4.5:1.
2. The current muted text value (~3.3:1 against page background, ~3.0:1 against card surface) fails BR1. The token value must be adjusted upward to meet 4.5:1 against the card surface (the harder target — page background compliance follows automatically).
3. Cards in retro columns must be visually distinguishable from their column background in dark mode. F31's card design principle is "border-only depth in dark mode" — retro cards should use the existing border token to create a visible card boundary.
4. Input fields in the retro board ("Add a note...") must have visible borders in dark mode.
5. The participant's primary action button ("I'm done") must be visually more prominent than ghost/outline controls. It should read as a primary or strong-secondary button — not a subtle outline that blends into the dark background.
6. Phase stepper labels must meet WCAG AA contrast thresholds in dark mode — both active and inactive states. Active/completed phase labels currently use a dark indigo shade that achieves only ~2.3:1 against the card surface, which is worse than the inactive labels (~3.0:1). Both must be fixed: active labels to >= 4.5:1, inactive labels to >= 3:1 minimum (>= 4.5:1 preferred).
7. Visual hierarchy during Writing phase: participant-facing actions ("I'm done," "Add" buttons) must be at least as visually prominent as facilitator-only controls to avoid role confusion.
8. All adjustments target dark mode only. Light mode tokens and styling are unchanged unless the same element also fails WCAG AA in light mode.
9. No new design tokens are introduced. Fixes reuse the F31 token system — adjusted values for existing tokens, or existing tokens applied to elements that were missing them.

## Acceptance Criteria

- [ ] Muted text token achieves >= 4.5:1 contrast ratio against the card surface (#13131a) in dark mode — the harder target that guarantees page background compliance.
- [ ] "No cards yet" empty state text is clearly readable in dark mode.
- [ ] Retro column cards have a visible border (using the existing border token) separating them from the column background in dark mode.
- [ ] "Add a note..." input fields in retro columns have visible borders in dark mode.
- [ ] "I'm done" button is styled as a filled or high-contrast button — not a subtle ghost/outline button.
- [ ] Active/completed phase stepper labels achieve >= 4.5:1 contrast ratio against the card surface in dark mode.
- [ ] Inactive phase stepper labels achieve >= 3:1 contrast ratio against the card surface in dark mode.
- [ ] All body text still meets WCAG AA 4.5:1 in dark mode after token adjustments.
- [ ] Light mode appearance is unchanged — no regressions from dark mode fixes.
- [ ] No new CSS custom properties are introduced — fixes reuse the F31 token system.

## Out of Scope

- **Light mode adjustments** — Light mode passes WCAG AA with current tokens. Edge cases found during implementation can be fixed opportunistically but are not the target.
- **Phase stepper redesign** — This spec improves contrast of the existing stepper layout. A redesigned stepper (larger icons, different layout) is a separate initiative.
- **Role-based button visibility** — The hierarchy fix is visual (prominence), not functional (visibility). Hiding "Next: Grouping" from participants or "I'm done" from the facilitator is a separate auth/UX decision.
- **New animation or transition effects** — No motion design changes.
