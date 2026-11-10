# Suite Scaffolding — Lessons

## Developer Lessons

- [TRACKED] **Class library vs Web SDK for service API projects:** When converting a FastEndpoints project from Web SDK to class library (`Microsoft.NET.Sdk`), all ASP.NET Core types (`SameSiteMode`, `CookieSecurePolicy`, `[Tags]`, etc.) become unavailable unless `<FrameworkReference Include="Microsoft.AspNetCore.App" />` is added to the csproj. This is the correct approach for service API class libraries — don't rely on transitive framework references.

- **`[Tags]` attribute namespace:** `[Tags]` comes from `Microsoft.AspNetCore.Http` in ASP.NET Core 10 (not from a NuGet package). When a class library needs it, add `global using Microsoft.AspNetCore.Http;` to GlobalUsings.cs after adding the FrameworkReference.

- **`ILogger<>` in class library:** `Microsoft.Extensions.Logging` is not implicitly included in class libraries. Add `global using Microsoft.Extensions.Logging;` to GlobalUsings.cs.

- **FastEndpoints v6 multi-assembly scanning:** The correct API is `services.AddFastEndpoints(o => o.Assemblies = [...])` on IServiceCollection, not via the `UseFastEndpoints` configurator. The `c.Assemblies` property does not exist on the `UseFastEndpoints` `Config` object in v6.

- **GlobalUsings must wait for types to exist:** Do not add `global using` references to domain namespaces in a project's GlobalUsings.cs until the domain classes actually exist. Doing so causes CS0246 errors that fail the Step 4 acceptance criteria. Strip forward-referencing usings from the skeleton step; add them in Step 5 once the domain is created.

- **Identity `DesignTimeDbContextFactory` path:** Using `Directory.GetCurrentDirectory()` in a DesignTimeDbContextFactory resolves to the EF tools' working directory, not the project directory. Use a hardcoded connection string (e.g., `Data Source=data/identity.db`) for design-time factory — it only needs to instantiate the DbContext, not match production config.

- [TRACKED] **Cross-service FK constraint removal:** When porting a service that had FK references to an entity that moves to another service (different DB), the FK constraint must be dropped from EF configuration. The field (e.g., `InvitedByUserId`) can remain as an unconstrained int — just document that it's a cross-service reference. This is intentional and follows the physical DB boundary.

- **create-service skill requires CLAUDE.md:** The `create-service` skill hard-errors if `src/Services/{Name}/CLAUDE.md` is missing. For ad-hoc scaffolding without an architect phase, create a minimal CLAUDE.md manually before invoking the skill, or implement the skeleton directly from the plan.

## Skill Gaps

- **Missing skill: multi-service Host composition:** No skill covers the pattern of composing multiple service class libraries into a single Host project (Host.csproj, Program.cs with AddXModule × N, multi-assembly FastEndpoints, migrations for both DbContexts). The `service-registration` skill covers the DI pattern but not the Host Program.cs orchestration. Suggested skill name: `compose-host`, covering: csproj setup, Program.cs three-section structure, multi-DbContext migration, FastEndpoints multi-assembly, ActiveUserPreProcessor global registration.

## Architect Lessons

- **Cross-repo scaffolding plans need explicit cleanup verification:** When a plan involves copying a service and then removing parts of it (e.g., port Fokus, remove AppUser), the done check must verify both that removed files are gone AND that empty directories are cleaned up. Empty directories left behind (`Fokus.API/Auth/`, `Fokus.API/Features/Auth/Login/`, etc.) are harmless under the no-`.gitkeep` policy but signal incomplete cleanup.

- **Ad-hoc scaffolding plans should include CLAUDE.md creation in the step that needs it:** Step 4 used the `create-service` skill which requires `CLAUDE.md`, but the plan did not mention creating it. The developer had to create it manually (noted as a deviation). When a plan step references a skill with known prerequisites, the plan should explicitly include those prerequisites or note them.

- **Reference project pattern works well for cross-repo domain modeling:** Pointing to `<reference-repo>/src/Services/Auth/` as a reference with a clear "what to adopt / what NOT to adopt" table gave the developer unambiguous guidance. The implementation matched the reference patterns precisely (partial class split, manual IAggregateRoot on User, IdentityDbContext inheritance). This table format should be reused in future plans that reference external codebases.

- **Modular monolith Host composition is under-documented:** The plan covered Host wiring in Steps 3 and 9, but the actual composition pattern (multi-assembly FastEndpoints scanning, global pre-processor registration, multi-DbContext migration ordering) is complex enough to warrant a dedicated skill. The developer's lesson about the missing `compose-host` skill confirms this gap.

## Reviewer Lessons

- **When porting a service, diff authorization attributes against the original:** A systematic `diff` of `[Authorize]`/`[AllowAnonymous]` attributes between the original and ported code is the fastest way to detect dropped access controls. Running `diff <(grep -rn "Authorize" original/) <(grep -rn "Authorize" ported/)` immediately surfaces any missing decorators. Do this as a standard step in all service-port reviews.

- [TRACKED] **Invite token round-trip is an easy-to-miss regression:** When porting an OAuth flow, any data stored in `AuthenticationProperties.Items` at the login endpoint must be explicitly read in the `OnTicketReceived` handler. If the handler is rewritten from scratch, this round-trip is silently lost. Review rule: for every `properties.Items["key"] = value` in a login endpoint, verify a corresponding `context.Properties?.Items["key"]` read in the ticket handler.

- **`IsActive` / deactivation is a cross-cutting concern that needs explicit plan coverage:** The plan mentioned "validate active" for the login path but did not specify adding an `IsActive` field to the `User` entity. Without this explicit instruction, the developer reasonably omitted it. Plans that involve porting auth should include a dedicated step: "Add `IsActive` field to User with `Deactivate()`/`Reactivate()` domain methods."

- **Check NuGet NU1903 warnings in build output:** Known high-severity vulnerability warnings from transitive NuGet dependencies appear in `dotnet build` output as `NU1903`. Always check the full build output — not just "Build succeeded / N errors" — for these warnings. They are easy to miss when the build passes.

- [TRACKED] **SQLite email uniqueness needs NOCASE collation or normalized storage:** SQLite's default UNIQUE index is case-sensitive (BINARY collation). Any unique index on an email field in SQLite requires either `.UseCollation("NOCASE")` in the EF configuration or storing a pre-normalized (lowercased) value. Check this in every new SQLite persistence layer.

- **Always verify the migration file when a fix cycle adds a new column:** When a fix adds a property to an EF-tracked entity (e.g., `IsActive` on `User`), check that a new migration file was generated — not just that the C# code compiles. A successful build does not guarantee the column exists in the schema. Checklist: count migration files before and after, grep the new file for the expected column name. If only one migration file exists and its timestamp predates the fix, the column is missing from the database.

- [TRACKED] **`return` consistency after response writes in pre-processors:** A pre-processor that writes a response and calls `CompleteAsync()` should always `return` immediately after. Flag missing `return` as MEDIUM when one branch has it and another does not — FastEndpoints's `HasStarted` guard makes it safe today, but the inconsistency is a latent bug if the method is ever reused or reordered.

