# LeaderboardBreakdown — Lessons

## Architect Lessons

- When a plan step modifies an existing service method (Step 1: extend ComputeLeaderboard), explicitly note the DI registration file as a potential touchpoint. The developer had to register LeaderboardService in DependencyInjection.cs — this was implied by the "create-feature" skill reference but not explicitly listed as a file to modify. For new services, always list the DI registration file.
- The plan correctly identified that SprintSummaryService's existing leaderboard did NOT apply excluded-from-scope statuses, and flagged this as an alignment change. This kind of behavioral delta (pre-existing behavior changing) should always be called out explicitly in plans — it has user-visible impact and needs testing attention.
- Structuring the plan with the backend service (Step 2) independent of the sprint summary extension (Step 1) allowed the developer to implement them in parallel. When two backend computation changes share no code dependency, keep them as separate steps to enable parallel work.
- The "pattern reference" annotations in plan steps (e.g., "mirrors GetBugRatioEndpoint") proved effective — the developer followed the pattern exactly, resulting in zero deviations. For analytics endpoints that follow an established convention (query params, mode dispatch, excluded developer filter), pointing to the canonical example is sufficient; no need to over-specify.
- Plan step ordering should match implementation dependency order, not conceptual order. Steps 8 and 9 were numbered parent-then-children but implemented children-first. Next time, number child components before their container when there's a clear import dependency.

## Developer Lessons

- When a service method has a delta field with hardcoded "neutral" polarity (not positive-up or positive-down), pass the string literal directly instead of routing through a `DeltaPolarity` helper — the helper cannot express "always neutral regardless of sign."
- When building L3 prop-only components that accept a union of multi/single entry types, a TypeScript type narrowing function (`isSingleEntry`) cleanly discriminates based on the presence of a discriminating property (`delta`) without adding a `mode` prop to every conditional block.
- Steps in the plan were numbered 8 (tab container) then 9 (child components), but implementation correctly reversed this order since the container imports the children. The plan note "can be developed in parallel" acknowledged this flexibility.
- The app process running in the background locks output DLLs. Using `-o <temp>/fokus-build-check` in `dotnet build` redirects outputs to a temp directory and allows clean compilation verification without stopping the running app.
- Frontend TypeScript verification (`npx vue-tsc --noEmit`) produces no output on success — empty output = pass.

## Reviewer Lessons

- When a service method accepts a parameter (`excludedDeveloperIds`) but never uses it internally, the exclusion may still be correctly applied via the caller pre-filtering the input list before passing it in. Verify the call site before flagging as a logic error — it may be intentional (caller-side exclusion pattern) rather than a missing guard.
- A double sub-team filter (FilterDevelopers on the developer list + AssigneeSubTeam check inside GetDeveloperMemberships) is always redundant when the membership loop iterates only over developers already scoped to the sub-team. Safe to flag as LOW cleanup rather than a logic concern.
- Build failures due to file locking (MSB3027/MSB3021) when the app is already running in debug mode are resolved by building with `-c Release` which targets a different output directory. Always verify with a clean configuration before concluding the build fails.

## Skill Gaps

- **Missing skill:** No skill covers "extend an existing service with new breakdown fields while maintaining backward compatibility" — the create-feature skill handles new features but not additive extensions to existing ones. Suggested name: `extend-service-response`. Coverage: adding fields to existing records, updating call sites, maintaining backward compat.
