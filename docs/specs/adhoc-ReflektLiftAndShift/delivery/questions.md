# Questions — adhoc-ReflektLiftAndShift

**Status:** All Answered (Q1=A confirmed; Q2, Q3 answered by architect at done-check, 2026-06-14)
**Pass:** RA1 (lift-and-shift) — Reflekt Integration Program
**Binding input:** `docs/architecture/decisions.md` ADR-016…020 + `docs/backlog.md` RA1 row. No spec.md (adhoc).

## Q1 — Auth scope for the lift (hard compile/run blocker)

**To:** user
**Status:** Answered (Q1=A in plan)

Reflekt registers its own auth in `Reflekt.API/Auth/AuthenticationConfiguration.cs`:
`AddAuthentication(Cookie default) → AddCookie() → AddGoogle(...) → AddScheme<…, GuestTokenAuthenticationHandler>(GuestToken)`, and `OnGoogleTicketReceived` writes Reflekt's **own** `User` via `UserRepository`.

The target Host already wires Google + cookie via the **Identity** module (`AddIdentityModule`) and runs a global `ActiveUserPreProcessor` on every endpoint. Two `AddAuthentication` default-scheme builders **cannot coexist** in one container — so Reflekt's auth cannot be lifted intact. A green, runnable build forces a choice:

- **A — RA1-minimal (recommended).** Register only Reflekt's **guest-token scheme** onto the Host's existing auth builder. Do **not** wire Reflekt's Google/cookie or `OnGoogleTicketReceived`, and do **not** map the `Login`/`Logout`/`Me`/`JoinSprintGoogle` endpoints (stub/unmap). Reflekt's `User` model + repo stay in place but unused by auth. Result: compiles + boots; Fokus/Identity unaffected; Reflekt guest paths work; **Reflekt Google login is non-functional until RA2** (Identity unification). Keeps the lift mechanical and the RA1/RA2 boundary clean.
- **B — RA1-plus.** Also bridge Reflekt's authenticated endpoints onto Identity's cookie now (read `NameIdentifier` from Identity, retire Reflekt's Google ticket handler). Pulls RA2's Identity work forward; blurs the lift/refactor boundary.

**Recommendation:** A. **Confidence: high** — preserves a clean mechanical-lift checkpoint, matches the owner-chosen lift-and-shift strategy, and the monorepo isn't deployed, so a temporarily-dead Google path costs nothing. The full Identity bridge + `Participant`→`Person` link is RA2 by design (ADR-016).

### Answer

Q1=A confirmed per plan. Implemented in Step 7.

---

## Planned approach (architect's call — flag if you disagree, not a blocker)

- **Migration strategy:** keep Reflekt's existing 15 migrations and add **one** migration for the four audit columns the target `AggregateRoot` introduces. `reflekt.db` is created fresh in the monorepo (no production data), so this is low-risk. Alternative (collapse to a single fresh `InitialCreate`) is cleaner-looking but discards valid history for no benefit.
- **`User` aggregate:** lifted as-is in RA1 (it compiles and the `Participant.UserId` FK depends on it); **removed in RA2**, not now.
- **Frontend:** RA1 lands the backend + `/hubs/retro` only. Reflekt's Vue SPA is not served by the Host until RA5 (frontend merge). Reflekt's own `MapFallbackToFile` is dropped (Host owns the fallback, currently Fokus's wwwroot).

---

## Q2 — Reflekt.API csproj: Sdk.Web → class library conversion not called out in plan

**From:** developer
**To:** architect
**Status:** Answered
**Step:** Step 1 (Move projects + solution registration)
**File:** `<reflekt-repo>/src/Services/Reflekt/Reflekt.API/Reflekt.API.csproj`

**Context:** `Reflekt.API.csproj` currently uses `Sdk="Microsoft.NET.Sdk.Web"` — it is a standalone web startup project, with `Program.cs`, `Properties/`, `appsettings*.json`, `wwwroot/`, `deploy.zip`. All other API class libraries in the target use `Sdk="Microsoft.NET.Sdk"` + `<FrameworkReference Include="Microsoft.AspNetCore.App" />` (see `Fokus.API.csproj`, `Identity.API.csproj`). The Host is the sole `Sdk.Web` project.

Step 1 says "copy" the three projects, and Step 2 says "repoint BuildingBlocks". Neither explicitly calls out changing `Sdk.Web` → `Sdk` + FrameworkReference, removing `Program.cs` (Reflekt's standalone startup), dropping `Properties/`, and omitting `appsettings*.json` and `wwwroot/` from the copy. These are required to make `Reflekt.API` a composable class library rather than a competing startup.

**Question:** Is the `Sdk.Web` → class library conversion (`Sdk="Microsoft.NET.Sdk"` + `FrameworkReference`) for `Reflekt.API.csproj`, removal of `Program.cs`, and omission of standalone-only artifacts (`Properties/`, `appsettings*.json`, `wwwroot/`) within the scope of Step 1 (copy-and-restructure) or Step 2 (repoint)?

**Recommendation:** Fold into Step 1 as a mandatory part of the copy step — it is purely mechanical and required for `dotnet restore` to resolve cleanly in a class library composition. The plan's "do not copy Reflekt's `src/BuildingBlocks/*` or the Vue app" acceptance criterion implies these standalone artifacts are also excluded; this just makes it explicit.
**Confidence:** high — Fokus.API and Identity.API are the established pattern (class library + FrameworkReference + no Program.cs), and a `Sdk.Web` project in the solution would attempt to produce its own executable conflicting with the Host.

### Answer
Architect (done-check, 2026-06-14): **Confirmed — fold into Step 1, exactly as recommended.** The
Sdk.Web→Sdk + `<FrameworkReference Include="Microsoft.AspNetCore.App" />` conversion, removal of
`Program.cs`/`Properties/`/`appsettings*.json`/`wwwroot/`, and dropping Host-owned packages
(Authentication.Google, OpenApi, Scalar) are mandatory mechanical parts of the copy — `Reflekt.API`
must be a composable class library, never a competing startup. This matches the established
Fokus.API/Identity.API pattern (CLAUDE.md: "Host is the only Web SDK project"). **Verified at
done-check:** `Reflekt.API.csproj` is now `Sdk="Microsoft.NET.Sdk"` + FrameworkReference, no
`Program.cs`, and the full solution builds green with Reflekt.API as a class library. No plan change
needed — this is a clarification within Step 1's stated scope, not a new decision.

---

## Q3 — Mapster version: Reflekt built with 10.0.0, target CPM pins 7.4.0 (significant downgrade)

**From:** developer
**To:** architect
**Status:** Answered
**Step:** Step 4 (Central Package Management — re-baseline)
**File:** `src/Directory.Packages.props`

**Context:** Reflekt's `Directory.Packages.props` has `Mapster` at `10.0.0` and `Mapster.DependencyInjection` at `10.0.0`. The target's CPM has `Mapster` at `7.4.0` (and no `Mapster.DependencyInjection` entry yet). The plan says "target wins on overlaps" and to "flag any Reflekt package whose target pin is a downgrade."

Mapster 10.x → 7.4.x is a significant downgrade (two major versions). Reflekt's DI registration pattern (`TypeAdapterConfig.GlobalSettings.Scan(...)`, `ServiceMapper`, `MapsterMapper.IMapper`) is present in 7.x, so API compatibility is likely for the patterns used. But Mapster 10 introduced new features and the mapping configs in Reflekt's `Mappings/` folder may use APIs unavailable in 7.4.

**Question:** Should the developer verify Reflekt's mapping code compiles against 7.4.0 during Step 4, or proceed with 7.4.0 as a hard rule (target wins) and let build errors surface naturally during green-build verification (Step 9)?

**Recommendation:** Proceed with target's 7.4.0 (target wins is the stated rule). If Mapster APIs used in Reflekt's Mappings are 10.x-only, they will be build errors surfaced at Step 9 — the natural checkpoint. No pre-investigation needed unless the architect knows of a specific 10.x API in Reflekt's mapping code.
**Confidence:** medium — the stated patterns (`Scan`, `ServiceMapper`, `TypeAdapterConfig.GlobalSettings`) are stable across 7.x→10.x, but without scanning every file in `Reflekt.API/Mappings/` I cannot rule out a 10.x-specific API.

### Answer
Architect (done-check, 2026-06-14): **Go to 10.0.0 for both `Mapster` and `Mapster.DependencyInjection`
— not 7.4.0.** The "target wins on overlaps" rule does not apply cleanly here because the target's
7.4.0 pin has **no companion `Mapster.DependencyInjection 7.4.0`** on NuGet (NU1603) — 7.4.0 is not a
coherent installable pair. The rule's intent (don't gratuitously change the target's pins) is
subordinate to "the solution must build." Take the nearest available coherent version (10.0.0, which
Reflekt already built with) and bump the target's `Mapster` to match, since the user-decision in play
is "lift to a green build," not "preserve 7.4.0." This is consistent with Q1's lift-and-shift intent
and does **not** reverse any user decision — the owner picked a green-build checkpoint, not a specific
Mapster version. **Verified at done-check:** `Directory.Packages.props` pins both at 10.0.0; the full
solution — including Fokus.API, Identity.API, and Fokus.Domain.Tests, all heavy Mapster consumers
(`.Adapt<>`, `IRegister`, `ServiceMapper`) — builds green with 0 errors, so no 10.x API breakage
exists in either Reflekt or the existing modules. Plan Step 4 already sanctioned flagging downgrades;
this is the same path resolving toward an upgrade. Recorded as Deviation D1 in `review.md`.
