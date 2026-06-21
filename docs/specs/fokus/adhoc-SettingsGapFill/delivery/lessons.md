# Settings Gap Fill — Lessons

## Developer Lessons
- The `DeveloperRepository` already had `GetAllAsync()` — no new repository method needed for the GET /api/developers endpoint. Checking the repository before adding methods avoids duplication.
- Frontend `Promise.allSettled` destructuring order must match the array order exactly. Adding new parallel calls requires extending both the destructuring and the result handling below.
- `vue-tsc --noEmit` exits with no output on success (exit code 0). Empty output = pass.
- When adding a new field to `AppSettings` interface in TypeScript, all three places need updating: `types/index.ts`, the store default object in `settingsStore.ts`, and the `form` reactive object in `SettingsView.vue` — otherwise TypeScript compilation will fail.
- `SetSubTeamEndpoint` needed no validator class because the only constraint is existence (404), not input shape. Avoiding an empty or trivial validator class keeps the folder lean.

## Reviewer Lessons
- When a migration adds an int column with `nullable: false`, EF Core auto-generates `defaultValue: 0`. For settings fields with meaningful non-zero defaults (e.g. `SyncBackSprintCount = 20`), the generated default must be manually corrected to match the domain default. Always compare migration `defaultValue` against the domain class property initializer and `CreateDefault()`.
- The `SaveSettings` endpoint pattern (construct brand-new `AppSettings` from command) silently drops any domain fields not present in `SaveSettingsCommand`. When reviewing features that add a field to this path, also check whether other existing fields on `AppSettings` are missing from the command — they will be reset to defaults on save. Flag this as a systemic gap even if it is pre-existing.
- Prior migrations in the same project are the best reference for correct `defaultValue` — compare against `AddBugRatioAlertSettings` which correctly sets `50` and `2`.

## Architect Lessons
- Multi-gap plans that land on a single view file work well when scoped together — the plan correctly predicted merge friction avoidance and the developer executed all three gaps in one pass without conflicts.
- The plan did not call out that `SaveSettingsEndpoint` constructs a brand-new `AppSettings` object, which silently drops fields not in `SaveSettingsCommand` (like `ExcludedFromScopeStatuses`). The reviewer caught this as a MEDIUM finding. Future plans that add fields to the settings path should include a step to audit the SaveSettings endpoint for field preservation — or better, the plan should reference the systemic pattern: "read-then-update" vs "replace-whole-object".
- EF migration `defaultValue` must match the domain property initializer. The plan said "no EF configuration change needed — it's a simple int, convention handles it" but convention generates `defaultValue: 0`, not the domain default of 20. Plans that add columns with non-zero defaults should explicitly note: "verify migration defaultValue matches domain default."
- The Step 1 done check passed cleanly because the plan was specific about file paths, patterns to follow, and field names. Named identifiers as binding contracts (from the plan writing rules) made cross-referencing mechanical.

## Skill Gaps
- Missing skill: No skill exists for "add field to existing settings aggregate" — covers the pattern of updating domain class + response DTO + command + validator + endpoint mapping + EF migration in lockstep. Suggested name: `add-settings-field`. Reference files: `AppSettings.cs`, `GetSettingsQuery.cs`, `SaveSettingsCommand.cs`, `GetSettingsEndpoint.cs`, `SaveSettingsEndpoint.cs`.
