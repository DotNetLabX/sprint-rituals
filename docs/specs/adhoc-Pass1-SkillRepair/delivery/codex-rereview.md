## Re-verification Report — adhoc-Pass1-SkillRepair

1. RESOLVED | Step 3 pt4 | Point 4 now states that the endpoint always defines its own response DTO and Mapster map, even when the wire and domain shapes are identical, which closes the prior 1:1 reuse loophole.
2. RESOLVED | Step 3 pt5 | Point 5 now narrows the ban to external/integration/response DTOs, explicitly allows intrinsic primitives and enums, and requires a value object only when the input carries domain meaning or an invariant.
3. RESOLVED | Step 3 pt3 | Point 3 replaces the old repository escape hatch with a narrow domain-owned read port declared in `{Module}.Domain`, returning domain types only, and explicitly forbids concrete repositories, `RepositoryBase`, and `IQueryable`.
4. RESOLVED | Step 1 Edits pt1; Step 1 Accept | Step 1 now requires a whole-skill reframe away from API/service framing and adds the requested grep acceptance for `\.API/Features`, `response record`, and `Service\.cs` with zero remaining hits in the repaired skill.
5. RESOLVED | Step 1 Edits pt2 | Point 2 now names the replacement boundary type explicitly as dedicated domain result records or VOs, with `BugRatioResult` and domain-typed `Single` / `Multi` members given as the concrete example.
6. RESOLVED | GENERAL-SKILL MANDATE pt2; Step 2b; Step 2 Accept | The revised plan now says to keep the MediatR/Application content, label it as the full-stack variant, and add the light-stack FastEndpoints guidance alongside it instead of scrubbing it.
7. RESOLVED | Step 4 pt3; Revision notes pt1 | Step 4 point 3 now covers all three package-version forms (`Version=`, child `<Version>`, and `VersionOverride=`), and Revision note 1 explicitly explains that this broadening is required for general-skill correctness even though SprintRituals is already clean.
8. RESOLVED | Step 5 pt4; Step 5 Accept | Step 5 now specifies the required two-stage process: first search for unqualified legacy `Send*Async` calls inside endpoint classes, then manually inspect the receiver so qualified non-FastEndpoints calls are not migrated.
9. RESOLVED | Step 3 pt7; Step 7 pt6 | Step 3 point 7 now adds the required `domain-patterns` and `persistence-patterns` cross-references, and Step 7 check 6 verifies those exact links during the audit.

## New issues

- HIGH | Step 3 pt1 and pt3 | The new full-stack variant is internally inconsistent: Step 3 pt1 presents `reference-patterns-domain.md` §6 `ArticleAccessChecker` as the full-stack variant of `domain-service`, but the referenced pattern is explicitly an Application-layer service, not a domain service (`questions.md` Q2 lines 47-51; `reference-patterns-domain.md` lines 453-495), and it directly uses `SubmissionDbContext`, which conflicts with Step 3 pt3's domain-owned read-port purity rule; as written, the foundational skill blurs two different patterns.
- MED | Skill Mapping; Disposition notes; Steps 1-5 | The revision now makes `improve-skills` the binding authoring workflow for all five skill edits, but the referenced authoring skill is scoped to native `.claude/skills/` scaffolding and explicitly says it does not create non-native skill formats (`plugins/nexus/skills/improve-skills/SKILL.md` lines 21-35 and 111); without an explicit adaptation rule for `plugins/nexus-dotnet/skills/`, the mandated process is still internally inconsistent.

## Overall

NO-GO. The revised plan does resolve all nine findings from the prior review, but it introduces a new high-severity ambiguity in the foundational Step 3 `domain-service` design by treating the reference repo's `ArticleAccessChecker` Application-service pattern as if it were the full-stack variant of the same concept while simultaneously imposing domain-service purity rules that the cited pattern does not follow. Because Step 1 explicitly specializes Step 3, that ambiguity would propagate into the analytics repair and future consumers of the new skill. The plan should be revised so the full-stack contrast is framed as a separate Application-service pattern rather than a `domain-service` variant, or the new skill should be renamed/re-scoped to match the broader abstraction; the `improve-skills` workflow binding also needs an explicit plugin-repo adaptation note.


## Touch-up confirmation

1. RESOLVED | Binding ADR → skill map ADR-005 row; Step 3 pt1 `Concept & location (one coherent pattern)` + contrast block; Step 3 pt6 `DI registration`; Step 3 Accept | The revised plan now scopes `domain-service` to the single pure Domain-layer pattern and frames `ArticleAccessChecker` as a distinct Application-layer contrast, with the checked locations explicitly excluding Application-layer/`DbContext`/concrete-repository dependencies for the domain service.
2. RESOLVED | Skill Mapping → Disposition notes → `improve-skills format/location adaptation (FIX 2)` | The new note explicitly states that the plugin `skills/` directory uses native `SKILL.md` format, keeps `improve-skills` authoritative for structure/frontmatter/Quality Gate/housekeeping, and assigns the plugin-repo location to this plan plus Q1.
3. New issues: None detected.
4. GO | Both targeted fixes are now internally consistent in the revised plan and no new issue was introduced by the touch-up.
