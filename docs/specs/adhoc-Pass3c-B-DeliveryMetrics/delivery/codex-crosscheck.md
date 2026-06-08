## Verdict
GO — I found no behavior-preservation regressions in the checked metric-helper, wire-map, or gRPC output paths; the only remaining issue is comment-level wire-name leakage inside .

## Findings Table
| Severity | File:Line | Issue | Evidence |
|---|---|---|---|
| OK | ; ; ; ; ;  | MetricCardBuilder preserves the old string-polarity card path, the bool  path, and the  overloads. |  still derives  and falls back to  on the string path,  still rounds both  and  to 1 dp, and both  /  still exist. Current call sites still send  cards through  in  and still use the  helper path for ticket-count deltas in  and . I compared those current lines against the deleted helper bodies in  , , , , and . |
| OK |  |  remains the literal  instead of a computed  call. | The current  /  / ticket-count delta fields call , but line 141 still emits  exactly as the deleted  did. |
| OK | ; ; ; ;  | All five  files keep wire-field parity with the deleted service record hierarchies, and their  maps cover the renamed domain result types. | I read the deleted record hierarchies from  for all five deleted services, then compared them with the current wire records and mapping registrations. No dropped wire fields, renamed wire fields, or missing  / delta-bundle maps that would alter JSON output were found. |
| OK | ; ;  |  and  still emit the same gRPC wire fields after switching to domain result types. |  still reads , , and .  still uses , , , , and . The only observed change versus  is the parameter types moving from API response types to domain  types with the same accessed field names. |
| INFO | ; ; ; ; ;  | Wire-flavored names still appear inside  comments. | These comment lines mention , , , and . No public domain declarations use those wire names, so this is textual/comment leakage rather than runtime coupling. |

## Summary
- The shared delta/polarity extraction is behavior-aligned with the deleted helpers, including the  short-circuit and the  bool-polarity overloads.
-  preserved the one critical carve-out:  is still the literal .
- The five new  files and the gRPC mappers preserve the prior wire shapes and field accesses after the move to domain  results.
- The only deviation found is comment-level wire-name leakage in ; there is no code-level behavior change from it.
