# Auth — Lessons

## Reviewer Lessons
- When a plan says "wrap all X elements with :disabled", count the actual elements in the template before accepting a "banner + one button" as equivalent. The deviation gap was large (1 control vs ~30+).
- Two separate `SaveChangesAsync` calls on repositories that share a DbContext create two separate transactions. Shared context does NOT mean shared transaction. Check for atomicity whenever a feature requires two aggregate mutations to be consistent.
- Vue Router 4 uses a scoring algorithm that prioritizes static segments over dynamic params regardless of route definition order — `/invite/error` (static) correctly wins over `/invite/:token` (dynamic) even when defined after it. Don't flag this as a bug without confirming Vue Router 4's ranking behavior first.
- The spec's acceptance criteria ("All configuration fields are visible but disabled") is more precise than the plan's implementation note ("banner and disabled Save button"). When a deviation is admitted in implementation.md, check both the plan AND the spec — the spec is the higher authority.

## PO Lessons
- Competitor research (7 tools) was high-value for grounding the role model decision. The privacy spectrum (Haystack → Pluralsight Flow) gave the user concrete options instead of abstract choices.
- The user's initial description ("I send an email they accept") sounded like automated email delivery, but simplifying to "generate a link, Admin copies it" removed an entire integration dependency (email service) with no UX loss for a small team.
- The "Settings visible but read-only" requirement only emerged when I challenged whether sensitive config should be visible at all. The user wanted transparency, not secrecy — good to probe this explicitly.
- Bootstrap flow (first login = Admin) resolved the chicken-and-egg problem cleanly. Worth proposing proactively for any auth feature.

## Architect Lessons
- FastEndpoints global pre-processor registration requires both DI registration (scoped service) AND configurator registration (in UseFastEndpoints). The plan only specified DI registration, which caused a deviation. When planning cross-cutting pre/post-processors, specify both registration sites explicitly.
- When a plan references HTTP status codes (409 Conflict), verify the exception type exists in the codebase before planning. The ConflictException gap was harmless here but could have blocked the developer if the exception hierarchy was more complex.
- The plan's Step 12 instruction for read-only enforcement ("wrap all button/input/select elements with :disabled") was correct in intent but underspecified the scope. The reviewer correctly caught that only the Save button was disabled initially. When planning UI enforcement across a large template, enumerate the sections or provide a count estimate so the reviewer has a concrete checklist.
- Skill gap logged: no auth-setup skill for Google OAuth middleware configuration (Step 4), no pre-processor skill for global request filtering (Step 8), no auth-frontend skill for route guards and 401 interception (Step 12). Three steps had no skill coverage — these are candidates for new skills if auth patterns recur.
- The plan's 12-step count hit the auto-approve threshold exactly. The feature was complex enough that a 13th step (splitting Step 12's three concerns) would have been cleaner. When a plan is at the boundary, prefer splitting for clarity over compressing to stay under the limit.
