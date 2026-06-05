# Lessons — adhoc-Pass0-Foundations

## Architect Lessons

- **Ad-hoc refactoring passes have no spec — review against the binding doc, not a spec.** Pass 0's requirements live in `docs/backlog.md` + `docs/architecture/decisions.md` (ADRs). The plan-vs-spec critic (Mode 2) has nothing to diff against; the correct gate is a self-review cross-referencing plan steps to ADR acceptance criteria. Recommend this explicitly so the team-lead doesn't spawn a critic with no artifact.

- **Broad mechanical greps need false-positive guards baked into the plan.** The `Send*Async` class includes one `System.Net.Http.HttpClient.SendAsync` (`GraphQLXrayClient.cs:296`) that must NOT be migrated. A regex-only "fix all `Send*Async`" instruction would have corrupted it. The plan must name the false positive with its file:line and a DO-NOT-TOUCH directive. Generalize: when a problem-class fix is grep-driven, list the known non-matches explicitly.

- **Refactoring plans must enumerate behavior-preserving side effects that look incidental.** Moving the migration block out of `Program.cs` would silently drop the SQLite parent-directory `Directory.CreateDirectory` bootstrap (SQLite won't create it), breaking first-run startup. Inline infra code often hides load-bearing side effects — the plan must call them out as parity constraints, not assume the move is mechanical.

- **Problem-class scope needs explicit boundary-drawing when instances span future-pass territory.** The stale "Microservices/ApiGateway" text appears in 13 files, but only `project-rules.md` is E1-scope. Skill files use the same words legitimately (scaffolding vocabulary = Pass 1); the agent file is a learner-owned flow artifact; the Fokus CLAUDE.md drift overlaps Pass 4. Distinguishing "instance of the class" from "same words, different concern" prevents a Pass 0 plan from bleeding into Passes 1/4. Flag the out-of-scope instances for routing rather than silently fixing or ignoring.

- **Decision-order (ADR-003) for missing-skill steps = derive from reference + log the gap, don't inline-and-forget.** CPM and framework-currency have no skill. The plan derives both from `<reference-repo>` (the props file, FastEndpoints 7 `Send.*`) and logs the skill gaps to lessons for Pass 1, rather than treating "no skill" as license to freestyle. The reference project read paid off — exact props shape and the `Migrate<T>()` helper were ported verbatim.

## Skill Gaps (for Pass 1 — skill repair)

- **`central-package-management` skill — MISSING.** No skill covers CPM. Pattern source: `<reference-repo>\src\Directory.Packages.props` (`ManagePackageVersionsCentrally` + `CentralPackageTransitivePinningEnabled`, bare `<PackageReference>` in csproj). Create in Pass 1.
- **`framework-currency` rule/skill — MISSING.** No rule enforces "latest stable + modern `Send.*` API." The `Send.*` migration map and the "verify method names against the installed package, not memory" guard should become a skill or convention. Reference: FastEndpoints 7.0.1 `Send.*` surface.
- **`Migrate<TDbContext>()` helper** — built in this pass into `Blocks.EntityFrameworkCore`; `service-registration` already references `app.Migrate<T>()` but the helper didn't exist locally (a "Build then Follow" case). Once built, `service-registration` becomes fully Followable for Host composition.
