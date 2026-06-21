# Team Management

**Traces to:** `docs/product/v1.md` §Developer entity, §Settings (sub-team tagging)
**Source:** Scratch
**Dependencies:** F2 (Domain Model & Persistence), F5 (Sprint Sync), F7 (App Shell & Navigation), F9 (Developer Throughput — DeveloperSprintCapacity entity)
**Status:** Done
**Plan:** `docs/plans/TeamManagement/plan.md`

---

## Purpose

Scrum Masters need a dedicated place to configure their team — who's on it, what role they play, how much capacity they bring, and which sub-team they belong to. Today, developers appear from Jira sync with no way to set default capacity or roles, sub-team assignment exists in Settings but belongs alongside the rest of the team configuration, and people who don't contribute SP (POs, testers, TLs with reduced capacity) clutter every analytics view. This feature adds a "Team" page to the sidebar where Scrum Masters manage their roster and set defaults that flow into all analytics via a cross-cutting exclusion rule. The existing sub-team management section in Settings is removed — the Team page is the single place for all developer configuration.

## Entities

**Extended: Developer**
- Default capacity percent — integer 0–100, default 100. Represents the person's typical sprint availability. Used by analytics as a fallback when no sprint-level capacity override exists (lazy resolution).
- Role — string, default "Developer". Descriptive label for the team page (e.g., "Developer", "Tech Lead", "Tester", "PO"). No business logic attached — purely informational.

**Existing, relocated to Team page:**
- Sub-team — already on Developer entity. Currently managed in Settings; this feature moves it to the Team page and removes the Settings UI section.
- Is active — already on Developer entity. Team page provides the toggle UI.
- Display name, avatar URL — from Jira sync, read-only on Team page.

**Existing, behavior changed:**
- Developer Sprint Capacity (from F9) — sprint-level override. When no override record exists for a developer+sprint pair, analytics now fall back to the developer's default capacity percent instead of assuming 100%.

## User Flows

```
Flow 1: View Team Page
1. User navigates to "Team" in the sidebar (positioned after "Developers")
2. The page displays a table of all developers (populated by Jira sync)
3. Rows are grouped by sub-team, with an "Unassigned" group at the bottom
4. Each row shows: avatar, display name, role, default capacity %, sub-team, active toggle
5. Within each group, rows are sorted alphabetically by display name
6. Inactive developers appear greyed out at the bottom of their group
7. The page has no sprint selector — this is sprint-independent configuration
```

```
Flow 2: Edit Developer Role
1. User clicks the role cell on a developer row
2. An inline dropdown appears with options: Developer, Tech Lead, Tester, PO
3. User selects a role
4. Change saves immediately (optimistic UI)
```

```
Flow 3: Edit Default Capacity
1. User clicks the default capacity cell on a developer row
2. An inline input appears showing the current percentage
3. User enters a value (0–100)
4. Change saves immediately (optimistic UI)
5. Developers with non-default capacity (anything other than 100%) show a visual indicator
```

```
Flow 4: Assign Sub-Team
1. User clicks the sub-team cell on a developer row
2. An inline combobox appears listing existing sub-team names plus an option to clear
3. User selects an existing sub-team, types a new sub-team name, or clears it (sets to unassigned)
4. Typing a name that doesn't exist creates that sub-team on save
5. Change saves immediately (optimistic UI)
6. The row moves to the selected sub-team group (or to "Unassigned")
```

```
Flow 5: Toggle Active/Inactive
1. User toggles the active switch on a developer row
2. Change saves immediately (optimistic UI)
3. Inactive developers are greyed out and move to the bottom of their group
4. Inactive developers are excluded from all analytics (existing F9 behavior, now with UI)
```

## API Surface

| Method | Route | Auth | Request | Response | Status codes |
|--------|-------|------|---------|----------|--------------|
| GET | /api/team | None | — | Team roster | 200 |
| PUT | /api/developers/{accountId}/team-config | None | Body (see below) | Updated developer | 200, 400, 404 |

**Superseded endpoints:**
- `PUT /api/developers/{accountId}/sub-team` — replaced by the `subTeam` field on the team-config endpoint. Remove this endpoint and its Settings UI caller.
- `PUT /api/developers/{accountId}/capacity` — NOT superseded. This handles sprint-level capacity overrides (F9) and remains unchanged.

**GET /api/team:**
- Returns all developers with their team configuration fields.
- 200: Always succeeds. Returns empty list if no developers have been synced.

**Response shape:**

Top level:
- `developers` — list of developer entries (see below)
- `subTeams` — distinct list of sub-team names currently in use (for dropdown population)

Per developer entry:
- Account ID (string — Jira account identifier)
- Display name
- Avatar URL (nullable)
- Role (string, default "Developer")
- Default capacity percent (int, default 100)
- Sub-team (string, nullable)
- Is active (bool)

**PUT /api/developers/{accountId}/team-config:**
- Updates any combination of team config fields. All fields are optional in the request — omitted fields are not changed.
- 200: Config saved. Returns the updated developer entry.
- 400: `defaultCapacityPercent` outside 0–100 range, or `role` is empty/whitespace.
- 404: Developer with given accountId not found.

**Request shape:**
- `role` (string, optional) — developer's role label
- `defaultCapacityPercent` (int, optional) — 0–100
- `subTeam` (string, nullable, optional) — sub-team name, null to clear
- `isActive` (bool, optional) — active status

## Business Rules

1. **Developers are populated by Jira sync only.** The Team page does not support adding or removing developers. Developers appear when sprints are synced and their tickets are imported. The Team page configures metadata on existing developers.

2. **Lazy capacity fallback.** When analytics compute effective capacity for a developer in a sprint, they check for a DeveloperSprintCapacity record first. If none exists, they use the developer's default capacity percent from the Team page. This replaces the previous assumption of 100% when no override exists. This supersedes F9 Business Rule 9 ("defaulting to 100") — the default is now the developer's DefaultCapacityPercent, which itself defaults to 100 for unconfigured developers.

3. **Exclusion rule: 0% effective capacity AND 0 completed tickets = hidden.** When a developer's effective capacity for a sprint is 0% (whether from default or sprint-level override) and they completed 0 tickets (zero tickets, regardless of story points, with a final status in the done statuses list) in that sprint, they are excluded from all analytics for that sprint:
   - Their tickets do not count toward sprint-level totals (total SP, committed SP, burnup, scope change, carry-over rate, disruption rate, bug ratio)
   - They do not appear in per-developer tables or charts
   - Their tickets are excluded from dashboard metric cards and health score inputs
   - This is a cross-cutting rule that affects F8 (Dashboard), F9 (Developer Throughput), F10 (Scope Change), F11 (Carry-Over), F12 (Cycle Time), F13 (Bug Ratio)
   - This overrides F9 rule 4 ("Zero-ticket developers are visible") and F9 rule 9 ("A developer with 0% capacity still appears") for the 0%-capacity-and-0-completed-tickets case specifically

4. **0% capacity with work done = visible.** If a developer has 0% effective capacity but completed at least one ticket in a sprint, they appear in all analytics normally for that sprint. The exclusion requires both conditions.

5. **Exclusion is per-sprint.** A developer may be hidden in sprint 25 (0% capacity, no completions) but visible in sprint 26 (completed 3 tickets). In multi-sprint views, they appear for sprints where they contributed and are absent for sprints where they didn't.

6. **Role is informational only.** The role field has no business logic — it does not affect capacity defaults, analytics, or filtering. It exists for team page readability.

7. **Role options are fixed.** Available roles: Developer, Tech Lead, Tester, PO. No custom roles. Default is "Developer".

8. **Sub-team assignment resolves GAP-3.** Sub-team assignment moves from Settings to the Team page. The existing sub-team management section in Settings (including its UI and the `PUT /api/developers/{accountId}/sub-team` endpoint) is removed. The SubTeam field on Developer remains unchanged — only the management UI location and API route change.

9. **Active toggle uses existing IsActive field.** Toggling active/inactive on the Team page sets the same IsActive field that all analytics already check. Inactive developers are excluded from all analytics regardless of capacity or work done.

10. **Default capacity changes affect unoverridden sprints retroactively.** Because of lazy fallback, changing a developer's default capacity immediately changes their effective capacity in every sprint where no explicit DeveloperSprintCapacity record exists — including past sprints. This is intentional.

11. **Sub-teams are free-text, not first-class entities.** Sub-team names are strings on the Developer entity. A sub-team exists when at least one developer is assigned to it. The combobox is populated from distinct existing values and supports typing new names. No separate sub-team CRUD.

## Acceptance Criteria

- [ ] "Team" appears as a sidebar navigation entry after "Developers"
- [ ] Team page displays a table of all synced developers
- [ ] Table rows are grouped by sub-team with an "Unassigned" group at the bottom
- [ ] Within each group, rows are sorted alphabetically by display name
- [ ] Each row shows: avatar, display name, role, default capacity %, sub-team, active toggle
- [ ] Role is editable via inline dropdown with options: Developer, Tech Lead, Tester, PO
- [ ] Role defaults to "Developer" for unconfigured developers
- [ ] Default capacity is editable via inline input (0–100)
- [ ] Default capacity defaults to 100 for unconfigured developers
- [ ] Developers with non-default capacity (not 100%) have a visual indicator
- [ ] Sub-team is editable via inline combobox listing existing sub-team names
- [ ] Sub-team combobox supports typing a new sub-team name (creates on save)
- [ ] Sub-team can be cleared (set to unassigned)
- [ ] Changing sub-team moves the row to the correct group visually
- [ ] Existing sub-team management section in Settings is removed
- [ ] Existing `PUT /api/developers/{accountId}/sub-team` endpoint is removed
- [ ] Active toggle switches developer between active and inactive
- [ ] Inactive developers are greyed out and appear at the bottom of their group
- [ ] All field changes save immediately with optimistic UI
- [ ] GET /api/team returns all developers with team config fields
- [ ] GET /api/team response includes a distinct list of sub-team names
- [ ] PUT /api/developers/{accountId}/team-config updates only the provided fields
- [ ] PUT /api/developers/{accountId}/team-config returns 400 for capacity outside 0–100
- [ ] PUT /api/developers/{accountId}/team-config returns 400 for empty role
- [ ] PUT /api/developers/{accountId}/team-config returns 404 for unknown accountId
- [ ] Lazy fallback: when no DeveloperSprintCapacity record exists, the developer's default capacity percent is used
- [ ] Exclusion: developer with 0% effective capacity and 0 completed tickets is hidden from sprint-level metrics
- [ ] Exclusion: hidden developer's tickets do not count toward sprint total SP, committed SP, burnup, scope change, carry-over, disruption, bug ratio
- [ ] Exclusion: hidden developer does not appear in per-developer tables or charts
- [ ] Exclusion: hidden developer's tickets are excluded from dashboard metric cards and health score
- [ ] Inclusion: developer with 0% capacity but at least one completed ticket appears normally
- [ ] Exclusion is per-sprint: same developer can be hidden in one sprint and visible in another
- [ ] Inactive developers are excluded from all analytics regardless of capacity or work
- [ ] Changing default capacity retroactively affects past sprints without explicit overrides
- [ ] No manual add/remove of developers — roster comes from Jira sync only
- [ ] Page has no sprint selector — this is sprint-independent configuration
- [ ] Sub-team management is accessible from Team page (GAP-3 resolved)
- [ ] Lazy fallback supersedes F9 rule 9: default capacity comes from Developer.DefaultCapacityPercent, not hardcoded 100%

## Out of Scope

- **Manual developer add/remove** — developers come from Jira sync. Manual roster management adds complexity for a team that already uses Jira as source of truth. Deferred.
- **Custom role labels** — fixed list (Developer, Tech Lead, Tester, PO) covers common cases. Custom roles add settings complexity without analytics value. Deferred.
- **Role-based default capacity** — auto-setting capacity when a role is selected (e.g., "Tech Lead" automatically sets 60%). Assumes fixed capacity per role, which varies by team. The Scrum Master sets both explicitly. Deferred.
- **Sub-team CRUD** — creating, renaming, or deleting sub-teams as first-class entities. Sub-teams remain free-text tags. Deferred.
- **Capacity history/audit log** — no record of when defaults changed. Deferred.
- **Bulk editing** — no multi-select to set capacity or role for multiple developers at once. Team size is small enough for individual edits. Deferred.
- **Sprint-level capacity override on this page** — sprint capacity overrides remain on the Developers page (F9). Team page manages defaults only.
- **Snapshot at sync** — copying default capacity into DeveloperSprintCapacity at sync time. Lazy fallback was chosen for simplicity. Can be added later if retroactive changes cause problems.
- **Real-time updates** — no SignalR push when config changes. Single-user tool.
