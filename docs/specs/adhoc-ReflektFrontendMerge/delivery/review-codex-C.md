# RA5-C Review (Codex)

> Recovered by the team lead from the Codex runtime job log (`task-mqm5hkow-nqf96p.log`) —
> Codex computed the verdict but its file write was blocked by the read-only session sandbox
> (known strand). Content below is verbatim from the run. Team lead independently verified the
> BLOCKER and the `/join/google` INFO against source before routing (see communication-log.md).

**VERDICT: NO-GO**
The rename is not fully complete: one live SignalR payload still sends `sprintId`, and multiple board-meaning `sprintId` identifiers remain in backend and merged frontend code, although the carve-outs and EF rename migration are intact.

| Severity | Check# | File | Issue |
| --- | --- | --- | --- |
| BLOCKER | 1 | `useSprintHub.ts:144-155`; `BroadcastColumnConfigUpdatedHandler.cs:12-18` | `ColumnConfigUpdated` wire-incompatible: client gates on `payload.retroSessionId`, server broadcasts `sprintId = ev.RetroSession.Id`. Column config updates received but ignored at runtime. **TL-VERIFIED: real — server line 14 emits `sprintId`, client line 155 reads `retroSessionId` (undefined → guard false → dropped).** |
| WARNING | 2 | `RetroHub.cs` (many lines) | Hub still uses board-meaning locals/params `sprintId`/`claimSprintId`/`parsedSprintId`/`sprintIdClaim` (`JoinRetro(int sprintId)`, etc.). Non-breaking. |
| WARNING | 2 | `TimerService.cs`; `DependencyInjection.cs:76,79-81`; `BroadcastPhaseChangedHandler.cs` | Timer infra still treats board id as `sprintId` (`Start(int sprintId)`, expiry `async sprintId =>`, `var sprintId = ev.RetroSession.Id`). Non-breaking. |
| WARNING | 2 | 11 endpoint files (ListCards/CreateCard/GetCurrentParticipant/GetParticipants/CastVote/GetMyVotes/RetractVote/GetGroups/GetPreviousActions/GetParticipantReport/GetSprintReport) | Guest-claim checks parse the renamed `"retroSessionId"` claim into locals named `claimSprintId`/`sprintIdClaim`. Non-breaking. |
| WARNING | 2 | `TeamRepository.cs:33-58` | `DeleteTeamWithCascadeAsync` stores retro-session ids in local `sprintIds`. Non-breaking. |
| WARNING | 2 | retro `router/index.ts`; `guest.ts`; `useRetroRoom.ts`; `useRetroBoardActions.ts`; `useReadiness.ts`; `TimerControls.vue` | Frontend retains board-meaning `/sprint` route segments, `sprintId` route params/props, and `reflekt:sprint:${sprintId}:token` storage key. Not API mismatches; violate the rename end-state. |
| INFO (real mismatch) | 2 | `participant.ts:34`; `JoinSprintEndpoint.cs:10` | Frontend `joinWithGoogle()` calls `/api/retro-sessions/${id}/join/google`; server exposes only `/join` (handles cookie-claim path). **TL-VERIFIED: real 404; pre-existing carry-over.** |
| OK | 2 | card.ts / sprint.ts / ListCardsEndpoint / RetroHub / CloseSprintHandler | Runtime strings renamed: `/api/retro-sessions/…`, group `retro:{…}`, event `RetroClosed`. No `/api/sprints/`, no `sprint:{}` group, no `SprintClosed` literal remain. |
| OK | 2 | Domain Card/CardGroup/Participant + EF configs + snapshot | Live EF model clean: board props/columns use `RetroSessionId`/`ReviewedInRetroSessionId`. |
| OK | 3 | GetSprintContext* / ISprintAnalyticsService / FokusSprintSnapshot / FokusSprintRef | All carve-outs intact (correctly keep Fokus-sprint semantics). |
| OK | 4 | `20260620090016_RenameBoardFkSprintIdToRetroSessionId.cs` + snapshot | Migration uses `RenameColumn` on Participants/Cards (×2)/CardGroups with paired `RenameIndex`. Data-preserving. |

**Reconciliation (team lead):** BLOCKER + `/join/google` are real runtime breaks → fix this cycle. WARNINGs are non-breaking board-meaning *local identifier* / frontend-route-segment naming — the enumerated locked-decision scope (DB columns, C# properties, API routes, wire strings) is verifiably DONE; the WARNINGs concern whether "eliminate every identifier" also mandates local-name cleanup (architect + reviewer ruled acceptable/deferred; user scope decision).
