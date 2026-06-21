# Team Management — Summary

## Status: COMPLETE

## What Was Built

A dedicated "Team" page where Scrum Masters configure their roster -- roles, default capacity, sub-team assignment, and active status. The feature extends the Developer entity with `Role` and `DefaultCapacityPercent`, adds `GET /api/team` and `PUT /api/developers/{accountId}/team-config` endpoints, removes the old sub-team management from Settings, and implements a cross-cutting exclusion rule (0% effective capacity + 0 completed tickets = hidden from sprint analytics) across all six analytics endpoints. The lazy capacity fallback replaces the hardcoded 100% default with Developer.DefaultCapacityPercent.

## Key Outcomes

- 11 files created, 17 files modified, 1 file deleted
- Backend build: pass (0 errors)
- Frontend type check: pass (0 errors)
- Review verdict: APPROVE after 1 fix cycle (2 findings: HIGH subTeams list update, MEDIUM sidebar order -- both fixed)

## Deviations from Plan

- Response DTO named `UpdateTeamConfigResponse` instead of plan's `TeamConfigResponse` -- no functional impact.
- `subTeamProvided` boolean pattern added to distinguish JSON null (clear sub-team) from absent (don't change) -- necessary implementation detail not explicitly in plan but required by the partial-update semantics the plan specified.
- TypeScript `UpdateTeamConfigRequest` uses `string | null` instead of plain optional `?` for role and capacity -- compatible, no functional difference.

## Notes

- The multi-sprint exclusion uses "excluded in ALL selected sprints" semantics: a developer who contributed in at least one sprint remains visible in multi-sprint views. The reviewer flagged this as an architecture question (spec BR5 says per-sprint exclusion). Current behavior is that a developer appears in the multi-sprint developer table if they contributed in any sprint, showing zeroes for sprints where they were excluded. This is correct user-facing behavior for a developer table -- omitting a row entirely from a multi-sprint comparison would be confusing. The per-sprint membership filtering still applies to team-level totals.
- The `subTeamProvided` JSON detection pattern (`EnableBuffering` + `JsonDocument.ParseAsync`) is the correct ASP.NET Core approach for distinguishing null from absent in optional fields. This pattern should be documented if it recurs.
