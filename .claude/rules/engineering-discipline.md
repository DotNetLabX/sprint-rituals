# Engineering Discipline

Universal coding-quality constraints. Applies to every agent that plans, writes, or reviews code. Backed by `docs/architecture/decisions.md` (the binding decision register).

## Problem-class rule

Any reported issue is an **example of a class**, not a one-off. When you fix it:
1. Fix **every instance** across the codebase (grep the pattern, don't stop at the cited file).
2. Fix the **root cause** — the skill, convention, or process that let it happen — not just the symptom.

## Decision order when code smells

1. **A correct skill exists** → apply the skill exactly.
2. **No skill, or the skill itself smells** → derive the pattern from the **reference project `<reference-repo>`**, then **fix or create the skill** so the next person inherits the right way.

Never invent a pattern when the skill or reference already defines one.

## Framework currency

- Always use the **latest stable** versions of the core stack. Target framework: **.NET 10**.
- Use current framework APIs, not deprecated ones. For FastEndpoints: `Send.*` (e.g. `Send.OkAsync`), never `SendOkAsync`.
- Package versions are centrally managed in `src/Directory.Packages.props` (`<PackageReference>` carries no `Version`).

## Skill-adherence guards (root-cause fixes)

Skills shipped wrong before because the guards below didn't exist. They are now mandatory:
- **Plans map every step to a skill** (or justify inline detail). No skill-less steps.
- **"Adapt" requires written justification.** "Skill exists but I did something different" without a recorded reason is a deviation, not a disposition.
- **Reviewer checks the skill mapping** — a step that ignored its skill is a review failure.
- **Skills must be portable and compilable** — no app-specific names leaking into a skill; no non-compiling snippets. A skill that fails either test is itself a defect to fix.
- **Architecture docs defer to skills** — they reference skills for implementation patterns rather than inlining them.

## Key coding decisions (see `docs/architecture/decisions.md` for full text)

- Computation is **domain logic** → domain services in the Domain layer returning domain types; **no API service classes** as a dumping ground; **no DTO enters the Domain**.
- Rich aggregates: factory + behavior partial split + value objects; no flat-scalar god entities.
- Feature = `{F}Endpoint.cs` + `{F}Command.cs`/`{F}Query.cs`; naming `{Action}{Entity}Command/Query/Response`, validator `{Command}Validator`; no `Request` suffix.
- Trivial mapping → Mapster (matching field names); non-trivial → factory in behavior partial.
- Thin `Program.cs` (Add/Init/Use); module-owned wiring via `Add{Module}Module` + `app.Migrate<T>()`.
