# Ideas — F8-AzureDeployment

## Topic: How a deploy gets triggered (architect)

### A — Laptop deploy script
- Problem: ships quickly with tooling the owner already runs
- Status: dropped
- Parked concerns: every deploy depends on one machine and on remembering the steps
- Reason: owner picked the merged D+C shape (2026-10-05)

### B — Auto-deploy on every push to main
- Problem: nobody has to remember to deploy
- Status: dropped
- Parked concerns: a broken commit on main reaches the live app with no gate
- Reason: owner picked the merged D+C shape (2026-10-05)

### C — CI on every push, deploy on a manual GitHub "Run" (or tag)
- Problem: automated, but the owner decides when it goes live
- Status: merged
- Parked concerns: GitHub→Azure OIDC needs a one-time app registration in the work tenant
- Reason: merged into E

### D — One script for laptop and pipeline
- Problem: laptop and pipeline can never drift
- Status: merged
- Parked concerns: none
- Reason: merged into E

### E — One deploy script, run by a manual-trigger GitHub workflow (C + D)
- Problem: automated, gated by the owner's click, with a single deploy implementation
- Status: picked
- Parked concerns: OIDC setup in the work tenant unverified
- Critique: OIDC setup is operator-owed; laptop path stays as the fallback. Owner kept it (2026-10-05)

## Topic: Where the four database files live on Azure (architect)

No round ran — the owner closed the ideas stage early; decided in the interview.

### F — SQLite on `/home`, rollback-journal mode, built-in hourly backups
- Problem: SQLite's WAL mode is unsupported on the network share behind `/home`
- Status: dropped
- Parked concerns: none
- Reason: review found Microsoft calls any SQLite on this storage unsupported, so the mode switch fixes nothing; and B1 + backups cost money, but zero cost is binding (owner, 2026-10-05)

### G — Managed database (Postgres or Azure SQL)
- Problem: removes the unsupported-storage risk
- Status: dropped
- Parked concerns: none
- Reason: costs $15–20/month; zero cost is binding (owner, 2026-10-05)

### H — Keep SQLite on `/home` exactly as the live apps run it, on the free plan
- Problem: zero cost, proven by both live apps
- Status: picked
- Parked concerns: Microsoft calls it unsupported; no built-in backup on F1
- Critique: risk accepted — the team is a few people, so no simultaneous writes; nightly snapshot deferred to F10. Owner kept it (2026-10-05)
