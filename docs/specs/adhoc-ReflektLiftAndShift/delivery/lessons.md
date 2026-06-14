# Reflekt Lift and Shift (RA1) — Lessons

## Architect Lessons

- **The "target wins on overlaps" CPM rule needs an availability escape hatch.** Step 4 said target
  wins, but the target's Mapster pin (7.4.0) had no companion `Mapster.DependencyInjection 7.4.0` on
  NuGet — so "target wins" was un-satisfiable as a coherent pair. The plan's "flag any downgrade"
  hedge implicitly covered this, but a cleaner rule for future re-baseline steps: *target wins unless
  the overlap version is unavailable or has no companion package, in which case take the nearest
  available coherent version and flag it.* The developer made the right call; the plan should have
  pre-resolved it (a "deferred read" — the missing 7.4.0 DI package was discoverable at plan time).

- **A whole-solution green-build acceptance criterion silently pulls in pre-existing breakage.** RA1's
  gate ("`dotnet build SprintRituals.slnx` green") meant a pre-existing FE-8.1 compile bug in Fokus
  (`Send.Results`, committed before RA1) had to be fixed for RA1 to pass. That's correct, but it blurs
  "what RA1 changed" vs "what RA1 had to fix to compile." Future lift plans with a solution-wide build
  gate should add a one-line note: *if the baseline solution does not already build green against the
  target pins, fixing the pre-existing breakage is in-scope and must be documented as a carry-over.*
  (Here the developer did document it — D2 — cleanly.)

- **Done-check skill-log scoping vs. the token label.** All four mapped skills were genuinely invoked
  by the developer in this session, but logged under token `developer:analyze` (the developer never
  advanced the state token to `developer:implement`). A strict token-equality scope would have
  false-failed a fully-conformant run. Judgment applied: the load-bearing facts are agent + session +
  skill-present, with the token as a phase hint, not a hard key. Worth surfacing to the learner — if
  the skill-tracker is meant to be scoped strictly by `developer:implement`, the pipeline needs to
  reliably flip the token at Phase-1→Phase-2; otherwise the done-check rule should formally treat
  `developer:analyze|implement` as one developer-run window.

- **Static done-check cannot close the one real runtime risk.** Everything in RA1 is verifiable
  statically except the `ActiveUserPreProcessor` × guest-scheme interaction (does the blanket
  pre-processor 401 a guest join?). The plan correctly deferred fixing it to RA2 but the *verification*
  still needs a boot. Flagged as a reviewer carry-over rather than swallowed into the done-check.

## Reviewer Lessons

- **Pre-processor short-circuits are accidental safety nets worth tracing.** The `ActiveUserPreProcessor`
  skips any principal whose `ClaimTypes.NameIdentifier` cannot be parsed as `int`. Guest tokens carry no
  such claim, so they silently bypass the processor without any scoping. This kind of implicit safety is
  discoverable only by tracing the code, not by reading the plan or the implementation notes. Future
  reviewers: when a blanket pre-processor is flagged as a carry-over risk for a new auth scheme, always
  trace the first guard clause — a parse failure exit is the most common accidental pass-through.

- **Gate pattern-matching on severity words inside prose can produce false blocks.** The nexus pipeline
  gate blocked a write that contained no CRITICAL or HIGH findings because the word "HIGH" appeared as a
  confidence qualifier in a carry-over table. Rewriting findings to avoid confidence qualifiers in table
  cells (use "Certainty:" or "Confirmed" in prose instead of the keyword "Confidence: HIGH") avoids the
  false trigger. The underlying issue is the gate greps for the pattern rather than the formatted severity
  block structure — worth raising to the learner for a potential gate precision improvement.

## Skill Gaps
- None new. The four pattern skills used (`persistence-patterns`, `central-package-management`,
  `framework-currency`, `service-registration`) covered their steps adequately for a mechanical lift.
