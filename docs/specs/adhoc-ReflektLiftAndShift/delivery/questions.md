# Questions — adhoc-ReflektLiftAndShift

**Status:** Open
**Pass:** RA1 (lift-and-shift) — Reflekt Integration Program
**Binding input:** `docs/architecture/decisions.md` ADR-016…020 + `docs/backlog.md` RA1 row. No spec.md (adhoc).

## Q1 — Auth scope for the lift (hard compile/run blocker)

**To:** user
**Status:** Open

Reflekt registers its own auth in `Reflekt.API/Auth/AuthenticationConfiguration.cs`:
`AddAuthentication(Cookie default) → AddCookie() → AddGoogle(...) → AddScheme<…, GuestTokenAuthenticationHandler>(GuestToken)`, and `OnGoogleTicketReceived` writes Reflekt's **own** `User` via `UserRepository`.

The target Host already wires Google + cookie via the **Identity** module (`AddIdentityModule`) and runs a global `ActiveUserPreProcessor` on every endpoint. Two `AddAuthentication` default-scheme builders **cannot coexist** in one container — so Reflekt's auth cannot be lifted intact. A green, runnable build forces a choice:

- **A — RA1-minimal (recommended).** Register only Reflekt's **guest-token scheme** onto the Host's existing auth builder. Do **not** wire Reflekt's Google/cookie or `OnGoogleTicketReceived`, and do **not** map the `Login`/`Logout`/`Me`/`JoinSprintGoogle` endpoints (stub/unmap). Reflekt's `User` model + repo stay in place but unused by auth. Result: compiles + boots; Fokus/Identity unaffected; Reflekt guest paths work; **Reflekt Google login is non-functional until RA2** (Identity unification). Keeps the lift mechanical and the RA1/RA2 boundary clean.
- **B — RA1-plus.** Also bridge Reflekt's authenticated endpoints onto Identity's cookie now (read `NameIdentifier` from Identity, retire Reflekt's Google ticket handler). Pulls RA2's Identity work forward; blurs the lift/refactor boundary.

**Recommendation:** A. **Confidence: high** — preserves a clean mechanical-lift checkpoint, matches the owner-chosen lift-and-shift strategy, and the monorepo isn't deployed, so a temporarily-dead Google path costs nothing. The full Identity bridge + `Participant`→`Person` link is RA2 by design (ADR-016).

## Planned approach (architect's call — flag if you disagree, not a blocker)

- **Migration strategy:** keep Reflekt's existing 15 migrations and add **one** migration for the four audit columns the target `AggregateRoot` introduces. `reflekt.db` is created fresh in the monorepo (no production data), so this is low-risk. Alternative (collapse to a single fresh `InitialCreate`) is cleaner-looking but discards valid history for no benefit.
- **`User` aggregate:** lifted as-is in RA1 (it compiles and the `Participant.UserId` FK depends on it); **removed in RA2**, not now.
- **Frontend:** RA1 lands the backend + `/hubs/sprint` only. Reflekt's Vue SPA is not served by the Host until RA5 (frontend merge). Reflekt's own `MapFallbackToFile` is dropped (Host owns the fallback, currently Fokus's wwwroot).
