# Communication Log — adhoc-Pass4-IdentityExtraction

Pass 4 of the refactor program: finish the Identity extraction (triage item **A2**). Binding constraints: `docs/architecture/decisions.md` (ADR register) + `.claude/rules/engineering-discipline.md`. Evidence: `docs/proposals/refactor-triage.md` (A2), `docs/backlog.md` (Pass 4).

## Launch decisions (locked by user, 2026-06-09)

| # | Decision | Choice | Implication |
|---|----------|--------|-------------|
| L1 | Invitation aggregate + 4 Auth features (Create/Revoke/Get/Validate) home | **Stay in Fokus** | Pass 4 does NOT relocate `Fokus.Domain/Auth/Invitation` or `Fokus.API/Features/Auth/*`. The redemption flow must therefore bridge Identity (login) → Fokus (invitation) cross-module. |
| L2 | Pass 4 breadth | **Full cut (incl. deferred infra)** | Beyond finishing the extraction seams, Pass 4 also delivers the three deferred infra items: (a) Invitation **redemption-at-login**, (b) **CompanyDomain auto-seed**, (c) gRPC **PersonGrpcService**. |
| L3 | Team mode | **Standard+Codex** | Step-2 review = nexus reviewer + Codex in parallel (independent cross-checks). |
| L4 | Branch | `main` | Consistent with Passes 0–5. |
| L5 | Dirty tree | Scope commits to Pass 4 paths only | Unrelated leftover artifacts (7× `lessons.md`, `docs/audit/`, `.claude/` tooling) must NOT be swept into Pass 4 commits. |

**Note on classification:** L2 (full cut) introduces NEW user-facing behavior (redemption-at-login, auto-seed) on top of structural work → this run starts at the **PO** (shape a spec) before the architect plans, unlike the mechanical earlier passes that went straight to architect.

## Message Log

| # | From | To | Type | Summary | Problem |
|---|------|-----|------|---------|---------|
| 1 | team-lead | PO | dispatch | Shape Pass 4 spec from A2 evidence + locked decisions L1/L2 | — |
| 2 | PO | team-lead | blocked | Reported spec written + blocked on questions | **Malfunction M1** — claimed artifacts absent on disk |
| 3 | team-lead | PO | dispatch | Re-persist spec.md + questions.md; restate questions inline | — |
| 4 | PO | team-lead | blocked | Spec (Draft) + 12 questions persisted; all To:user | M1 resolved — artifacts now on disk |
| 5 | team-lead | user | relay | Applied PO defaults for Q1/Q3/Q4/Q5/Q6/Q7/Q8/Q10; escalated Q2/Q9/Q11/Q12 to user | — |
| 6 | user | team-lead | answer | Q2=enforce match, Q9=Fokus→Identity upsert, Q11=promote User, Q12=API-only — **all 12 resolve to PO defaults** | — |
| 7 | team-lead | PO | dispatch | Record 12 answers in questions.md; finalize spec → Ready | — |
| 8 | PO | team-lead | done | Spec **Ready**; all 12 questions Answered; Open Questions resolved | — |
| 9 | team-lead | architect | dispatch | Phase 1: Analyze adhoc-Pass4-IdentityExtraction | **Held** — superseded by M2 remediation (spec review owed first) |
| 10 | user | team-lead | answer | M2 remediation: run **Critic (Mode 1)** spec review before the architect plans | — |
| 11 | team-lead | critic | dispatch | Mode 1: spec-vs-evidence cross-reference (architect launch held until findings cleared) | — |
| 12 | critic | team-lead | findings | Mode 1: 0 BLOCKER / 1 HIGH (H1 gRPC unnamed) / 4 MED / 5 LOW; no ADR violation, no scope change. Findings in spec-critic-findings.md | Inline result terse → read scratch file (relay fallback worked) |
| 13 | team-lead | PO | dispatch | Fix H1+M1–M4 (+L1–L5) in spec; M3 scope-flag caveat; re-confirm Ready | — |
| 14 | PO | team-lead | done | Spec Critic-clean, Status: Ready; no M3 scope flag (resolved in-scope) | — |
| 15 | team-lead | — | verify | Spot-checked H1/M1/M2/M3/M4 + L4/L5 in spec — all landed; verdict validated | — |
| 16 | team-lead | architect | dispatch | Phase 1: Analyze adhoc-Pass4-IdentityExtraction (held launch released — M2 closed) | — |

**M2 disposition:** Closed. The spec-review gate the PO skipped was run (Critic Mode 1) and its findings (1H/4M/5L) folded back into the spec before any planning. Net effect: a stronger spec than if the offer had been taken at first draft.

| 17 | architect | team-lead | blocked (P1) | Phase 1 analyze done; Q13–Q15 raised; correctly stopped at checkpoint | — |
| 18 | team-lead | user | escalate | Q13 = scope change (2nd gRPC contract for sync redemption) + review mode | — |
| 19 | user | team-lead | answer | Q13 = **Build now, Option A+A1** (two opposite-direction gRPC contracts); review mode = **critic (Mode 2)** | — |
| 20 | team-lead | — | resolve | Q14 = option (a), predicate `Role==Admin && active-admin-count==1`; Q15 = yes (forced by Q9). Recorded in questions.md | — |
| 21 | team-lead | architect | resume (P2) | Write the plan. Answers Q13/Q14/Q15 in questions.md. Review mode: critic | — |
| 22 | architect | team-lead | plan done | 11-step plan (2 Build/3 None/6 Follow); did NOT self-review; "critic review owed on plan.md"; requests code-grounded Mode 2 | 11 steps unsplit — accepted (single dependency chain; split would create cross-sub-plan deps; not an approval gate) |
| 23 | team-lead | critic | dispatch | Mode 2 (plan vs spec), **code-grounded** — validate Build steps 3 & 9 + Step 1 promotion against source + reference repo | — |
| 24 | critic | team-lead | findings | Mode 2: 1 HIGH (H1 Jira-no-email) / 5 MED / 5 LOW; no locked-decision contradiction; code-grounded. Inline complete + scratch file | — |
| 25 | team-lead | user | escalate | H1 is a scope decision (Jira devs have no email; Person upsert keys on email) | — |
| 26 | user→TL→user | answer | H1 first = "pull email from Jira"; **team-lead feasibility check** (Jira Cloud, no user endpoint, email hidden by default) → re-decided **best-effort email + key by ExternalId, Person.Email optional** | Avoided sending architect down an infeasible path |
| 27 | team-lead | architect | resume (P2) | Fold all 11 findings + H1 resolution into plan + `## Plan Review`; revise affected steps | — |
| 28 | team-lead | PO | dispatch | Parallel spec patch: AC-8 + Person email-optional / ExternalId key | — |

**H1 disposition (locked):** Jira→Person upsert keys Jira-sourced Persons by `ExternalId` (=Jira AccountId, Source=Jira); `Person.Email` becomes optional (nullable, unique index scoped to non-null); a best-effort Jira `/rest/api/3/user` fetch fills email when Cloud exposes it. Adds a 2nd Identity EF migration + a new Jira API method. Google-sourced Persons still match by email.

**Scope note (locked at Q13):** Pass 4 now stands up **two** new gRPC code-first contracts in **opposite directions** — `PersonGrpcService` (Fokus→Identity, Identity-hosted server, Person upsert) and `IInvitationService` (Identity→Fokus, Fokus-hosted server, atomic validate+match+Accept→role) — **plus** gRPC client infrastructure built from scratch (CPM packages + `Blocks.AspNetCore` client helper), User promoted `Entity<int>`→`AggregateRoot<int>`, and a `RemoveUserRoleDbDefault`-style migration. This is the largest pass of the program.

## Runtime / Tooling Malfunctions Log

| ID | When | Symptom | Disposition |
|----|------|---------|-------------|
| M1 | PO Phase 1 (shape) | PO completed with result "Blocked. Awaiting user answers. Spec: …/definition/spec.md" but **no spec.md and no questions.md were written to disk** (only communication-log present); the inline result carried no actual questions, so nothing was relayable. Did not read the raw agent transcript (context-overflow risk). | Recoverable — re-engaged the same PO agent (by agentId) to persist both artifacts and restate questions inline. No decision lost. |
| M2 | PO finalize (→ Ready) | PO skipped its mandated **"Spec review offer"** step (agents/po.md): it must offer the user a quality gate — self cross-check vs critic Mode 1 (spec vs product docs) — before setting `Status: Ready`. It set Ready directly without offering. Spec reached Ready without the review its own definition requires. | Open — remediable before the architect plans: offer the spec-review gate now (self vs critic Mode 1). Pending user choice. |
