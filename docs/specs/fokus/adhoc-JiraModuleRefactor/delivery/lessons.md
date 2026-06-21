# JiraModuleRefactor — Lessons

## Architect Lessons
- [APPLIED] When planning namespace/reference refactors, enumerate ALL files that use the moved type — not just the obvious ones (behavior partials). Entity definition files in the same namespace don't need a using directive for same-namespace types, but once the type moves to a different namespace, they do. A grep for the type name (not just the using directive) catches these.
- [APPLIED] When a plan removes project references, explicitly call out "verify transitive dependencies" as a sub-task. Removing a reference to project A also removes transitive access to everything A references. The ExtractJiraModule lessons already flagged this pattern — this plan should have applied that lesson.
- [TRACKED] The circular dependency resolution strategy (move the shared type to the lower-level project) is a clean pattern worth remembering: when A depends on B and B depends on A through a single type, move that type to whichever side should be the dependency root.

## Developer Lessons
- [APPLIED] When removing project references that break transitive dependency chains, check for transitive packages/projects that the removed reference was providing. In this case, `Jira.RestApi` lost access to `Blocks.Exceptions` and `Microsoft.Extensions.Options.DataAnnotations` when `Fokus.JiraContracts` and the `Fokus.Domain` transitive chain were removed.
- [APPLIED] When moving a type out of a namespace, check not just behavior files (which have explicit usings) but also entity definition files in the same namespace — if they reference the moved type, they need a new using directive even though they didn't need one before.

## Skill Gaps
- None identified. All steps were mechanical refactoring with no repeatable pattern warranting a skill.
