# Dark Theme Contrast & Visual Hierarchy — Questions

## Q1: "I'm done" button styling direction
**From:** architect
**To:** user
**Status:** Answered
**Step:** Phase 1 analysis
**File:** `web/reflekt-web/src/components/retro/RetroBoardPanel.vue`

**Context:** The spec (BR5, BR7) requires "I'm done" to be a filled/high-contrast button — not the current ghost/outline style. It must also be "at least as visually prominent" as the facilitator's "Next" button (which uses `bg-primary-600 text-white` — indigo filled). The ready/done state is already `bg-emerald-600 text-white` (green filled).

The question is what the **not-ready** state should look like. Three options, all using existing colors (no new tokens per BR9):

- **Option A: Emerald outline with white text** — `border-2 border-emerald-600 text-emerald-400 bg-surface` in dark mode. Visually prominent via the bright green border/text, clearly distinct from the indigo "Next" button. Toggles to solid green on click. Still reads as a strong secondary button.
- **Option B: Emerald filled (lighter)** — `bg-emerald-600/80 text-white`. A slightly translucent green fill. Prominent and clearly "ready to click." Almost matches the done state, distinguished by opacity and label change.
- **Option C: Primary filled (indigo)** — `bg-primary-600 text-white`. Same visual weight as "Next" button — equally prominent, but harder to distinguish the two buttons at a glance since both are indigo.

**Question:** Which direction do you prefer for the "I'm done" not-ready state? I recommend **Option B** (emerald filled, slightly lighter) because it gives the button its own semantic identity (green = completion), is unambiguously prominent, and clearly differentiates from the facilitator's indigo "Next" button.

### Answer
Option B — emerald filled translucent (`bg-emerald-600/80 text-white`) for the not-ready state. Confirmed by user.
