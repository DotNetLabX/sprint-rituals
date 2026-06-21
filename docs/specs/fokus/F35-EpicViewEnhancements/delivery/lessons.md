# F35-EpicViewEnhancements — Lessons

## Developer Lessons

- **`ref<Set>` mutation does not trigger Vue reactivity — always reassign `.value`.** Calling `.add()`/`.delete()` on a `ref<Set>` in-place is silent: Vue tracks the `.value` reference, not internal Set mutations. Fix: `const next = new Set(ref.value); next.add/delete(...); ref.value = next`. Applies equally to `ref<Map>`. Flag any `ref<Set>` or `ref<Map>` mutation pattern in carry-over so the reviewer can confirm at review time.

- **Passing a store function as a plain prop is safe for reactivity when the parent re-renders on state change.** `isColumnVisible: (columnId: string) => boolean` is passed as a prop to `EpicTable`. Vue cannot track the function's internal reads, but since `hiddenColumns.value` reassignment triggers a re-render of `EpicsView`, the prop is re-passed and `EpicTable` re-renders. This works in practice but is a reactive design smell — prefer passing the `hiddenColumns` ref directly if the child needs to react independently of the parent.

## Reviewer Lessons

- **`ref<Set<string>>` reactivity trap is a recurring Vue 3 pattern.** Calling `.add()`/`.delete()` directly on the Set inside a `ref<Set>` does NOT trigger reactivity — Vue only tracks `.value` reassignment. The developer correctly flagged this in the Carry-Over table. Fix pattern: build `new Set(hiddenColumns.value)`, mutate the new Set, then reassign `hiddenColumns.value = next`. This applies to any `ref<Set>` or `ref<Map>` — always reassign the `.value` reference after mutation, never mutate in place.

- **Sibling guard pattern signals missing guard in new code.** When a helper (`IsStartedInSprint`) guards `if (startIndex < 0) return early` before using `>= startIndex`, and new code uses the identical `>= startIndex` condition without the guard, the missing guard is a logic bug. When reviewing new code that uses the same condition as an existing pattern, check whether the existing pattern's guards are also replicated.

- **Developer-flagged carry-over items require explicit verdict — not acknowledgment.** The carry-over table in `implementation.md` exists specifically so the reviewer investigates and confirms or refutes. "Worth verifying at runtime" (developer's language) means: reviewer confirms it is a bug (CRITICAL) or proves it is safe. Do not carry it forward — resolve it.

- **`new Date('YYYY-MM-DD')` is a UTC-midnight trap.** The ISO 8601 date-only string form is parsed as UTC by the spec and all major engines. In any timezone west of UTC (negative offset), `new Date('2025-01-15').toLocaleDateString()` renders as Jan 14. When displaying date-only values from an API, always parse with the multi-arg constructor `new Date(y, m-1, d)` to get local midnight. Flag any `new Date(isoDateString)` that feeds a display or day-difference calculation.

- **Sort column validation should cover the full renderable gate, not just the hidden set.** The initial BR11 implementation only reset sort when the user hid the exact sorted column. The correct invariant is: the active sort column must be both visible AND satisfy any feature gate (QA gate, future gates). Encode this as a single `isSortableColumn(col, hiddenCols, hasQaData)` predicate and apply it at every point where either input changes — hydration, column toggle, and (if applicable) after API response changes `hasQaData`.

- **Performance fixes adjacent to correctness fixes need a scope check.** Hoisting `transitionsByTicket` out of the per-epic loop is a correctness-adjacent performance fix (O(n) → O(1) allocations). Verify the hoisted value's key space and lookup semantics are unchanged at all call sites — here, `OrdinalIgnoreCase` on `TicketId` was preserved correctly. When a data structure moves scope, check every `.GetValueOrDefault` / `[]` call site against the new construction site.
