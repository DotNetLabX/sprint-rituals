# Poker — Product Specification

**Owner:** Laurentiu  
**Status:** Draft  
**Date:** May 16, 2026 (updated Jun 21, 2026)  
**Suite:** Agentic Rituals Suite — planning ceremony tool  

---

## 1. What It Is

A real-time planning poker tool for sprint estimation. The Scrum Master selects a sprint, the team estimates tickets collaboratively, and final story points are written back to Jira automatically.

**Dual purpose:**
- **Team tool** — replaces external planning poker apps, integrated with the suite's identity, sprint, and Jira infrastructure
- **Course material** — demonstrates real-time collaboration + third-party API write-back in the Claude Code for .NET course

**What it is not:**
- Not a standalone app — lives in the monorepo as a module, depends on Fokus for sprint/ticket data and Jira write-back
- Not a backlog grooming tool — estimates tickets already in the sprint
- Not a Jira plugin — reads and writes via REST API through Fokus

---

## 2. Target User

Scrum Master running sprint planning with a team of 3–15 developers. Tickets are already moved into the sprint in Jira before estimation begins.

---

## 3. Integration Points

### 3.1 Identity Module

Same dual-path authentication as Reflekt:
- **Google auth** — links to Person entity in Identity, which correlates to Fokus Developer and Reflekt Participant records
- **Guest** — pick a display name, join immediately, no account required

Google-authed voters are identifiable across tools. Guest voters are session-scoped.

### 3.2 Fokus Module (gRPC)

Fokus is the Jira gateway. Poker never touches Jira directly.

| gRPC Call | Direction | Purpose |
|-----------|-----------|---------|
| `GetAvailableSprints` | Poker → Fokus | Sprint list including future state (extend existing call) |
| `GetSprintTickets(sprintId)` | Poker → Fokus | Ticket key + summary + current story points. **Open: live pass-through vs. cached read** — see §3.2.1 |
| `SetStoryPoints(ticketKey, points)` | Poker → Fokus | Write estimate back to Jira |

**Fokus extensions required:**
- Sprint sync must include `state=future` sprints (currently Active/Closed only)
- New gRPC endpoint: `GetSprintTickets` — returns tickets for a given sprint
- New gRPC endpoint: `SetStoryPoints` — writes story points to Jira via REST API (Fokus already has write capability for sprint metadata)

#### 3.2.1 `GetSprintTickets` data freshness — RATIFIED: Option A (live pass-through)

The problem: the SM may move tickets in/out of the sprint in Jira *during* a planning session. If Poker reads from Fokus's cached sprint data, the ticket list goes stale mid-session. Two options:

**Option A — Live Jira pass-through (no cache).**
`GetSprintTickets` hits Jira REST live on every call via Fokus, returning current sprint contents. Poker UI gets a **refresh button** to re-pull on demand. Always accurate; cost is a Jira API round-trip per refresh. Aligns with the stateless pass-through pattern (Fokus owns the call, caches nothing for this path).

**Option B — Cached read from Fokus sync.**
`GetSprintTickets` reads from Fokus's synced sprint snapshot. Faster, no live Jira dependency, but stale if tickets move after the last sync. Would need a sync trigger or accept staleness.

**Decided: Option A** (PO+architect consensus) — accuracy during the live ceremony matters more than the round-trip cost, and the refresh button makes the freshness explicit to the SM. `IJiraClient.GetSprintIssuesAsync(sprintId)` already exists, so the live path is cheap. This sets the `GetSprintTickets` gRPC contract shape (live pass-through, caches nothing on this path).

### 3.3 Reflekt (None in v1)

No direct integration with Reflekt. Both consume from Fokus independently. Future consideration: linking estimation sessions to retro action items ("we underestimated last sprint").

---

## 4. Domain Model

Four entities. Own database (`poker.db`).

### 4.1 PlanningSession

```
PlanningSession
├── Id              Guid
├── SprintId        int (Fokus sprint ID)
├── SprintName      string (snapshot at creation)
├── CreatedByUserId int (FK → Identity.User)
├── Status          enum (Active, Completed)
├── CreatedAt       DateTime
├── CompletedAt     DateTime (nullable)
```

One session per sprint planning. SM creates it, selects the sprint, shares the room link.

### 4.2 SessionParticipant

```
SessionParticipant
├── Id              Guid
├── SessionId       Guid (FK → PlanningSession)
├── PersonId        int (FK → Identity.Person)
├── DisplayName     string
├── IsVoter         bool (default true — observers are v2)
├── JoinedAt        DateTime
```

All participants vote in v1. Observer role (PO watching without voting) is deferred.

### 4.3 EstimationRound

```
EstimationRound
├── Id              Guid
├── SessionId       Guid (FK → PlanningSession)
├── TicketKey       string ("PROJ-123")
├── TicketSummary   string (snapshot from Fokus)
├── Status          enum (Voting, Revealed, Decided, Skipped)
├── FinalEstimate   decimal (nullable — set when SM picks winner)
├── DecidedAt       DateTime (nullable)
├── Sequence        int (order within session)
```

One round per ticket. No re-vote — single round, SM picks winner from submitted votes.

### 4.4 Vote

```
Vote
├── Id              Guid
├── RoundId         Guid (FK → EstimationRound)
├── ParticipantId   Guid (FK → SessionParticipant)
├── Value           string ("1", "2", "3", "5", "8", "13", "21", "?")
├── SubmittedAt     DateTime
```

Value is string to accommodate "?" (uncertainty). Numeric values are parsed for the write-back.

---

## 5. Estimation Scale

Fibonacci: **1, 2, 3, 5, 8, 13, 21, ?**

"?" means "I don't have enough information to estimate." It cannot be selected as the winner — the SM must resolve the uncertainty first (discussion, then pick a numeric value from the votes).

Scale is hardcoded in v1. Configurable scales (T-shirt sizes, custom sequences) are v2.

---

## 6. Flow

### 6.1 Session Setup (SM only)

1. SM navigates to Poker in the suite
2. Sprint selector shows available sprints from Fokus — **default: first future sprint** (the one after the current active sprint). Dropdown includes last 2–3 sprints for flexibility.
3. SM creates a planning session for the selected sprint
4. Room link generated (same pattern as Reflekt: `/poker/{sessionId}`)
5. SM shares link with the team

### 6.2 Joining

1. Participant opens room link
2. Chooses Google auth or guest (display name)
3. Enters the planning room — sees participant list, ticket list, estimation area

### 6.3 Estimation Loop

```
SM selects ticket
  → Voting phase (everyone picks a card, hidden)
  → Reveal (SM triggers, or auto-reveal when all votes are in)
  → All votes displayed with participant names
  → SM picks winner from the submitted vote values
  → Story points written to Jira via Fokus
  → SM selects next ticket (or ends session)
```

**Auto-reveal:** When every participant has voted, votes are revealed automatically. SM can also force-reveal early if someone is AFK.

**Winner selection:** SM sees the distinct values that were voted (e.g., "3× voted 5, 2× voted 8") and taps one to confirm. Only values from the actual votes are selectable. The winning value is written to Jira immediately.

**Skip:** SM can skip a ticket without estimating (status → Skipped). No write-back to Jira.

### 6.4 Session End

SM ends the session. Status → Completed. Summary shows: tickets estimated (count), tickets skipped (count), total story points assigned.

---

## 7. Real-Time (SignalR)

Same hub pattern as Reflekt. One hub: `PlanningHub`.

### 7.1 Server → Client Events

| Event | Payload | When |
|-------|---------|------|
| `ParticipantJoined` | displayName, participantId | Someone joins the room |
| `ParticipantLeft` | participantId | Someone disconnects |
| `TicketSelected` | ticketKey, ticketSummary, roundId | SM picks a ticket to estimate |
| `VoteReceived` | participantId (no value) | A vote is in — show checkmark, not the value |
| `VotesRevealed` | list of {participantId, displayName, value} | All votes visible |
| `EstimateDecided` | ticketKey, finalEstimate | SM picked winner, written to Jira |
| `SessionCompleted` | summary stats | SM ended session |

### 7.2 Client → Server Events

| Event | Payload | Who |
|-------|---------|-----|
| `SelectTicket` | ticketKey | SM only |
| `SubmitVote` | value | Any participant |
| `ChangeVote` | value | Any participant (before reveal only) |
| `RevealVotes` | — | SM only |
| `DecideEstimate` | value (from votes) | SM only |
| `SkipTicket` | — | SM only |
| `EndSession` | — | SM only |

### 7.3 Authorization

SM actions gated by `CreatedByUserId` check, same pattern as Reflekt's phase control authorization.

---

## 8. UI

### 8.1 Planning Room — Main View

Three zones:

**Left panel — Ticket list**
- All tickets from the sprint (key + summary)
- Current story points shown if already estimated
- Visual indicator: estimated (green) / current (highlighted) / pending (default) / skipped (grey)
- SM clicks a ticket to start estimation

**Center — Estimation area**
- Current ticket: key + summary displayed prominently
- Card fan at the bottom: 1, 2, 3, 5, 8, 13, 21, ?
- Participant picks a card — selected card highlights, others dim
- Can change selection before reveal
- After reveal: all votes displayed as cards with participant names above them
- Winner selection: SM taps a vote value, confirmation prompt, then written to Jira

**Right panel — Participants**
- List of participants with status indicators
- During voting: checkmark (voted) / waiting (not yet voted)
- After reveal: their vote value shown next to name

### 8.2 Session Summary

Shown on session end. Simple stats:
- Tickets estimated: N
- Tickets skipped: N  
- Total story points assigned: N
- List of ticket key + final estimate

### 8.3 Design Direction

- Consistent with suite aesthetic (Reflekt + Fokus)
- Card fan should feel tactile — cards, not buttons
- Reveal animation: cards flip simultaneously
- Keep it simple — no gamification, no avatars, no confetti

---

## 9. Schema Changes to Other Modules

### 9.1 Fokus

| Change | Detail |
|--------|--------|
| Sprint sync includes `state=future` | **Already done.** `SprintState.Future` exists and `POST /api/sync/backlog` already syncs future sprints. Only a new Poker-scoped sprint query (Active + Future + recent) is needed — do **not** widen `GetAnalyticsSprintsAsync` (shared by ~20 analytics endpoints; it deliberately excludes Future). |
| New gRPC: `GetSprintTickets` | Returns `List<SprintTicket>` (Key, Summary, StoryPoints, IssueType). **Live pass-through (Option A ratified, §3.2.1).** |
| New gRPC: `SetStoryPoints` | Accepts ticketKey + points. **NET-NEW Jira write path — the feature's highest build risk.** Today Fokus's only write is `UpdateSprintAsync` → `POST /rest/agile/1.0/sprint/{id}` (Agile API, sprint metadata). Story points need `PUT /rest/api/3/issue/{key}` with `{fields:{customfield_10016}}` — a different REST API and a net-new `IJiraClient`+`IJiraApi` method. This is **not** an extension of existing write capability; scope it explicitly. |

### 9.2 Identity

No changes. Poker uses existing Person/User entities.

### 9.3 Reflekt

No changes.

---

## 10. Tech Stack

| Layer | Choice | Reasoning |
|-------|--------|-----------|
| Backend | .NET 10 Minimal API | Suite consistency |
| ORM | EF Core + SQLite | Same as all modules, `poker.db` |
| Real-time | SignalR | Same as Reflekt, proven for room-based collaboration |
| Frontend | Vue 3 + TypeScript + Pinia | Suite consistency |
| CSS | Tailwind CSS | Suite consistency |

---

## 11. What's Deferred (v2+)

| Item | Reasoning |
|------|-----------|
| Re-vote rounds | Edge case — votes cluster enough in practice that the SM always has a reasonable winner |
| Observer role | PO/stakeholders watching without voting — not needed for current team size |
| Configurable scales | T-shirt sizes, custom sequences — Fibonacci covers the need |
| Winner from outside votes | Selecting an arbitrary value not in the submitted votes |
| Backlog browsing | Tickets are moved into the sprint before planning — no need to browse backlog |
| Timer per ticket | Optional countdown for voting — not needed for current team dynamics |
| Estimation accuracy analytics | Comparing estimates vs. actuals using Fokus throughput data — v3+ |
| Ticket detail panel | Showing description, acceptance criteria, comments from Jira — keep Jira open instead |
| Session history / past sessions | Browsing previous planning sessions — no current need |
| Integration event to Reflekt | `EstimationCompleted` event — no concrete consumer yet |

---

## 12. Monorepo Changes

```
src/
  Host/Program.cs               # Add: builder.Services.AddPokerModule();
                                #       app.MapPokerEndpoints();
  BuildingBlocks/
    Contracts/
      IPokerService.cs          # (if needed for cross-module calls)
      IFokusService.cs          # Extend: GetSprintTickets, SetStoryPoints
  Poker/
    Poker.API/
      Endpoints/
      Hubs/PlanningHub.cs
      ServiceRegistration.cs
    Poker.Domain/
      Entities/
      Enums/
    Poker.Persistence/
      PokerDbContext.cs          # → poker.db
  client/
    src/modules/poker/           # Vue pages + components
```

Same module shape as Reflekt and Fokus. No architectural novelty.

---

## 13. Build Estimate

**Target: 0.5–1 day with OMC**

| Phase | Estimate |
|-------|----------|
| Poker module scaffolding (.NET + Vue) | 30 min |
| Domain model + EF Core + SQLite | 30 min |
| Fokus gRPC extensions (3 endpoints) | 1 hour |
| SignalR hub + real-time flow | 1.5 hours |
| API endpoints (session CRUD, vote, decide) | 1 hour |
| Vue planning room (3 zones) | 2 hours |
| Jira write-back integration | 30 min |

Parallelizable: backend (gRPC + hub + endpoints) and frontend (planning room UI) can run simultaneously once the SignalR contract is defined.

---

## 14. Open Questions

1. ~~**Story points field**~~ — **RESOLVED: `customfield_10016`.** Fokus already reads story points from `customfield_10016` (`Jira.Contracts/JiraIssue.cs`); the write-back reuses the same field id. No discovery needed.
2. **"?" vote resolution** — if someone votes "?" and it triggers discussion, the SM just picks a numeric value from the other votes. If everyone votes "?", the SM skips the ticket. Is this sufficient, or should the SM be able to type a custom value in this edge case?
