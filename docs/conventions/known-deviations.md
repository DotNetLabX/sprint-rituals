# Known Deviations

Recurring implementation mistakes tracked across features. Developer reads during Phase 1 pre-flight. Learner populates from lessons.md analysis.

| Pattern | Wrong | Correct | Frequency | First Seen |
|---------|-------|---------|-----------|------------|
| Not updating all call sites when a method signature changes | Updating the primary call site only; leaving other callers broken | Search for all call sites of the changed method before marking the step done | 2+ (F28, F31) | F28-PerDeveloperStoryQuality |
| Dead code left after mid-step refactor | Leaving unused variables, empty loop bodies, or unreachable branches after changing approach mid-step | Before finishing any computation helper or service method, scan for unused variables, empty loops, and unreachable code | 2+ (F28, F31) | F31-EpicTestHealth |
| Response DTO naming deviates from codebase convention | Using plan-specified name that doesn't match existing `{Concept}Response` suffix pattern | Grep for existing DTO names in the feature area before adopting a plan-specified name; document deviation if overriding | 2+ (F28, F29) | F28-PerDeveloperStoryQuality |
| Skill precondition not met silently | Invoking `create-service` or other prerequisite-requiring skills without the required CLAUDE.md or infrastructure | Read skill's preconditions before invoking; if precondition is missing, create it or note the deviation | 2+ (adhoc-SuiteScaffolding, implied others) | adhoc-SuiteScaffolding |
| KB updates skipped after implementation | Completing all numbered plan steps but missing KB Impact section at bottom of plan | Treat KB Impact as an implicit final step; add it to implementation.md even when not a numbered plan step | 2+ (F32 architect lesson, recurring) | F32-DailyDeveloperProgress |
| Build verification batched to end | Running `dotnet build` only after all steps are complete | Run build verification after each step; a type error introduced in step 3 compounds through steps 4-8 | 2+ (F30, implied others) | F30-TestExecutionTimeline |
