# RR-D — Reflekt Event-Correctness Verification

**Pass:** RR-D (`adhoc-ReflektEventCorrectness`) — Pass-5 arm of `docs/proposals/reflekt-refactor-triage.md`
**Type:** No-op verify pass. **No plan.md / implementation.md** — this is the record-of-record because there is no code change.
**Date:** 2026-06-21
**Author:** Architect
**Verdict:** **Verified-clean. RR-D withdraws as by-design.** Both event-correctness problem classes are absent across all 6 Reflekt `Behaviors`.

## Scope

Verify two problem classes (per ADR-003, the cited Fokus instances are exemplars of classes, not the whole scope):

- **B1 (Fokus `PersonCreated` class):** no `AddDomainEvent` raised *after* `SaveChangesAsync` — a post-save raise the interceptor never sees.
- **B2 (Fokus dead-event class):** every declared event under each `Events/` folder is actually raised — no declared-unraised events.

Across all 6 behavior files: `Team`, `Vote`, `Card`, `CardGroup`, `Participant`, `RetroSession` (`src/Services/Reflekt/Reflekt.Domain/*/Behaviors/*.cs`).

## Mechanism (what each class means here)

- `AggregateRoot.AddDomainEvent` appends to a private `_domainEvents` list — `src/BuildingBlocks/Blocks.Domain/Entities/AggregateRoot.cs`.
- `DispatchDomainEventsInterceptor` reads `DomainEvents` in `SavedChangesAsync` (post-save), publishes, then clears — `src/BuildingBlocks/Blocks.EntityFrameworkCore/Interceptors/DispatchDomainEventsInterceptor.cs`.

So a **B1** failure here = calling `AddDomainEvent` *after* the repository's `SaveChangesAsync` returns (e.g. in an endpoint after persistence). The interceptor, having already run for that save, would never publish it — the event would sit on the tracked entity until the next save or be lost.

## Event census — 23 declared events, 24 raise sites

All raise sites are `AddDomainEvent(...)` calls inside domain behavior methods. The 24th site is `GroupDiscussedChanged`, raised by both `MarkDiscussed` and `UnmarkDiscussed`.

| Aggregate | Declared events | Raise site(s) (`Behaviors/*.cs`) |
|-----------|-----------------|----------------------------------|
| CardGroup | CardGroupCreated, CardGroupTitleChanged, CardsGrouped, CardUngrouped, GroupDiscussedChanged | `Create`, `SetTitle`, `RecordCardAdded`, `RecordCardRemoved`, `MarkDiscussed` **+** `UnmarkDiscussed` |
| Card | CardCreated, CardUpdated, CardDeleted, EvidenceLinkAttached, EvidenceLinkFetched, EvidenceLinkDetached, ReviewStatusChanged | `Create`, `Update`, `MarkDeleted`, `AttachEvidenceLink`, `UpdateEvidenceLinkMetadata`, `DetachEvidenceLink`, `SetReviewStatus` |
| Participant | ParticipantJoined, ParticipantKicked | `Join`, `Kick` |
| Team | TeamCreated | `Create` |
| Vote | VoteCast, VoteRetracted | `Create`, `MarkRetracted` |
| RetroSession | RetroSessionCreated, ColumnConfigUpdated, RetroSessionStarted, PhaseAdvanced, RetroSessionClosed, GroupFocused, GroupFocusCleared | `Create`, `UpdateColumnConfig`, `Start`, `AdvancePhase`, `Close`, `SetFocus`, `ClearFocus` |

**23 / 23 declared events raised.**

## B1 — post-save raises: NONE

Every one of the 24 `AddDomainEvent` calls is inside a domain behavior (factory or mutator), before any save.

- Zero `AddDomainEvent` calls in `Reflekt.API` (grep clean).
- Zero event construction (`new *Event(`) anywhere outside `Behaviors/`.
- Representative endpoint `CloseRetroEndpoint.cs` confirms the canonical order: mutate (`session.Close()`, which queues the event) → `SaveChangesAsync` → respond. The interceptor publishes in `SavedChangesAsync`, after the raise. No post-save raise path exists.

## B2 — declared-unraised (dead) events: NONE

All 23 declared events appear in the census above with a live raise site. No dead events.

## Conclusion

RR-D produces no code change — it matches the triage's `verify` verdict resolving to compliant. Reflekt's event flow is correct on both classes: all raises are pre-save inside behaviors, and no declared event is orphaned. **Withdrawn as by-design.**

**N1 rider (empty `Sprints/` dirs):** not carried here — already resolved (folded into RR-B; zero empty `Sprints/` dirs confirmed by team-lead).
