# Backlog

Refactoring program from the refactor-triage session. **Binding constraints for every pass:** `docs/architecture/decisions.md` (ADR register) + `.claude/rules/engineering-discipline.md` (auto-loaded). Evidence: `docs/proposals/refactor-triage.md`.

Order is dependency-driven (skills before the domain work that applies them; mechanical before deep). Each pass is one pipeline run (architect plan → developer → reviewer). Manual testing after each pass (no automated suite by decision).

| ID | Slug | Pass | Status | Depends on |
|----|------|------|--------|-----------|
| 0 | adhoc-Pass0-Foundations | Foundations (cross-cutting, mechanical) | **Done** | — |
| 1 | adhoc-Pass1-SkillRepair | Repair/create skills (root cause) | Blocked | — (do before Pass 3) |
| 2 | adhoc-Pass2-SliceHygiene | Endpoint split + naming + Mapster | Blocked | Pass 1 |
| 3 | adhoc-Pass3-DomainEnrichment | VOs, domain services, no-DTO-in-domain | Blocked | Pass 1, Pass 2 |
| 4 | adhoc-Pass4-IdentityExtraction | Finish Fokus→Identity cut | Blocked | needs user scope (A2) |
| 5 | adhoc-Pass5-EventCorrectness | Domain-event dispatch fixes | Blocked | Pass 3 |

---

## Pass 0 — Foundations  (slug: `adhoc-Pass0-Foundations`, Status: Done)

Cross-cutting, mechanical, no per-feature semantics. No user input required — fully specified by the ADRs below.

**Scope:**
1. **Central Package Management** (ADR-013) — add `src/Directory.Packages.props` (`ManagePackageVersionsCentrally=true`); move every version to `<PackageVersion>`; strip `Version=` from all `<PackageReference>` across the 18 projects.
2. **Framework currency** (ADR-011, ADR-012) — replace deprecated `SendOkAsync` / legacy `Send*Async` with the modern `Send.*` API (**~89 calls across 54 files** — grep `Send(Ok|NoContent|String|Created|...)Async`); bump FastEndpoints (and core packages) to latest stable; fix any breaking changes.
3. **Thin composition root** (ADR-014) — move per-module wiring out of `Host/Program.cs` into each module's `Add{Module}Module` + `Map{Module}Endpoints`; introduce/use an `app.Migrate<TDbContext>()` helper; relocate MassTransit, gRPC server/client registration, and DB seeding into the owning module. `Program.cs` ends as Add / Init / Use only. Pattern: `service-registration` skill + reference (`<reference-repo>`).
4. **Convention alignment** (ADR-001, finding E1) — rewrite the stack-conventions block (CLAUDE.md / `docs/conventions/`) that describes "Services/ = Microservices", `ApiGateway/` (YARP), and `docker-compose` to reflect the decided modular monolith.

**Acceptance:** `dotnet build SprintRituals.slnx` green; no `SendOkAsync` remains; `Host/Program.cs` contains no per-module wiring; CPM active (no per-project versions); conventions text matches the modular monolith.

**Artifacts:** plan → `docs/specs/adhoc-Pass0-Foundations/delivery/plan.md`.

---

## Notes for the team-lead

- **Read first:** `docs/architecture/index.md` (→ `decisions.md`) and `.claude/rules/engineering-discipline.md`. They are binding.
- **Decision order (ADR-003):** correct skill → reference project `<reference-repo>` → fix/create the skill. Every fix is a problem *class* (fix all instances).
- **Pass 1 (skill repair)** has open design detail (the new `domain-service` skill shape) — the user wants to review that interactively, so checkpoint before finalizing it.
- **Pass 4 (Identity extraction)** needs the user to define what stays in Fokus vs moves to Identity before planning.
- Use the **nexus** pipeline agents (`/nexus:{agent}`), not OMC equivalents (per the `pipeline-guardrails` rule).
