# Authentication & Access Control — Review

## Reviewed By
`reviewer` (Sonnet agent, claude-sonnet-4-6)

---

## Cycle 1 Verdict: REQUEST CHANGES (superseded)

## Cycle 2 Verdict: APPROVE

No CRITICAL or HIGH issues. All three cycle-1 findings are correctly resolved.

---

## Cycle 2 Re-review: Fix Verification

### Fix 1 — [HIGH] SettingsView.vue read-only enforcement

**Status: RESOLVED**

Every interactive control in the non-User-Management sections now carries `:disabled="isReadOnly"` with `disabled:opacity-50 disabled:cursor-not-allowed` Tailwind classes. Verified by reading lines 457–1067 in full:

| Section | Controls verified |
|---------|-------------------|
| Jira Board | `<select>` (line 491), fallback `<input>` (line 516) — both `:disabled="isReadOnly"` |
| Done Statuses | remove `<button>` (line 535), `<select>` dropdown (line 543), Add `<button>` (line 557), fallback `<input>` (line 582), fallback Add `<button>` (line 584) |
| Excluded From Scope | remove `<button>` (line 601), `<select>` (line 609), Add `<button>` (line 621), Save Excluded Statuses `<button>` (line 644) |
| Workflow Stages | Re-detect `<button>` (line 670), up/down/remove `<button>`s (lines 704–706), sidelined add `<button>` (line 734), stage `<input>` (line 714), Add `<button>` (line 718) |
| Cycle Time Boundaries | start `<select>` (line 757), end `<select>` (line 775), Save Boundaries `<button>` (line 790) |
| Health Thresholds | all 6 `<input>` elements (lines 812, 816, 826, 830, 840, 844) |
| Health Weights | all 3 `<input>` elements (lines 858, 862, 866) |
| Bug Ratio Alerts | all 3 `<input>` elements (lines 896, 904, 918) |
| Save Settings | `<button>` (line 934) — pre-existing, was the only disabled control in cycle 1 |
| Sync | sprint count `<input>` (line 966), planning window `<input>` (line 975), Sync All `<button>` (line 988), Load Sprints `<button>` (line 1016), From sprint `<select>` (line 1029), To sprint `<select>` (line 1039), Sync Range `<button>` (line 1048) |

The User Management section (lines 1069+) is correctly behind `v-if="authStore.isAdmin"` — no `:disabled` needed there.

No controls missed. Fix is complete.

---

### Fix 2 — [MEDIUM] AuthConfiguration.cs atomic invitation acceptance

**Status: RESOLVED**

`AuthConfiguration.cs` lines 91–94:

```csharp
user = AppUser.CreateFromInvitation(googleId, email, displayName, avatarUrl, invitation.Role);
await appUserRepository.AddAsync(user);       // stages the add
invitation.Accept(DateTime.UtcNow);           // mutates the tracked invitation
await appUserRepository.SaveChangesAsync();   // saves both in one transaction
```

The second `invitationRepository.SaveChangesAsync()` call is gone. Both the new `AppUser` row and the invitation status update (`Accept`) are staged and flushed in a single `SaveChangesAsync` on `appUserRepository`. Since both repositories share the same scoped `FokusDbContext`, this is one database transaction. Partial-state risk eliminated.

---

### Fix 3 — [LOW] ActiveUserPreProcessor.cs error message differentiation

**Status: RESOLVED**

`ActiveUserPreProcessor.cs` lines 20–23:

```csharp
var user = await appUserRepository.GetByIdAsync(userId, ct);
if (user is not null && user.IsActive)
    return;

var message = user is null ? "Account not found." : "Your account has been deactivated.";
```

Exactly matches the suggested fix. Null user → "Account not found." Active-false user → "Your account has been deactivated." The condition structure (early return on happy path, differentiated message on failure) is clean and correct.

---

## Positive Observations (Cycle 2)

- The disabled Tailwind classes (`disabled:opacity-50 disabled:cursor-not-allowed`) are applied consistently across every control — not just the `:disabled` attribute. This gives Managers a clear visual signal on every field, not just cursor feedback.
- The atomic `SaveChangesAsync` fix uses the exact pattern the review suggested — no workarounds or partial solutions.
- The `ActiveUserPreProcessor` diff is minimal and surgical — only the message differentiation was added, no structural changes.

---

## Gaps (carried from cycle 1, no new gaps found in cycle 2)

- Backend validation: no error if bootstrap admin's email has no `@` (domain remains null, no domain enforcement ever applies). Acceptable per spec — noted for awareness.
- No per-login company domain re-validation on the regular login path. Acceptable per spec.

---

## Open Questions

None.

---

## Evidence

| Check | Result | Command | Output |
|-------|--------|---------|--------|
| Build (cycle 2) | PASS | `dotnet build Fokus.API.csproj --no-incremental -c Release` | `Build succeeded. 0 Error(s), 3 Warning(s)` — same pre-existing warnings as cycle 1, none introduced by these fixes |
| SettingsView.vue controls | PASS | Manual read lines 457–1067 | All sections verified — every `<input>`, `<select>`, `<button>` carries `:disabled="isReadOnly"` |
| AuthConfiguration.cs atomic save | PASS | Read lines 91–94 | Single `appUserRepository.SaveChangesAsync()` call; `invitationRepository.SaveChangesAsync()` removed |
| ActiveUserPreProcessor.cs message | PASS | Read lines 19–23 | Ternary differentiates null vs inactive |
