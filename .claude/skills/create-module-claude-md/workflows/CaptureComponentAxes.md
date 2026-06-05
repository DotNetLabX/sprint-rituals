# Capture Component Axes

Only run this workflow if `CaptureCommonAxes.md` resolved to archetype = **Component**.

A Component module has a `.Contracts` project (interfaces) and one or more implementation projects (e.g., `FileService.AzureBlob`, `FileService.MongoGridFs`). Host services reference exactly one implementation — the contract stays stable, implementations swap.

## Axes

| Axis | Values | Notes |
|------|--------|-------|
| Contract interface name | `I{Something}` (PascalCase) | E.g., `IFileStorage`, `IEmailSender` |
| First implementation name | PascalCase short name | E.g., `AzureBlob`, `SendGrid`, `Smtp` — becomes `{Name}.{FirstImpl}` project |
| First implementation tech | 1-line description | E.g., "Azure Blob Storage via Azure.Storage.Blobs SDK" — for CLAUDE.md |
| Options class needed | Y / N | Usually Y — e.g., `AzureBlobFileStorageOptions` with connection string + container name |
| DI extension method name | PascalCase | E.g., `AddAzureBlobFileStorage()` — the single entry point host services call |

## Pre-fill from conversation

- If the user mentioned "IFileStorage", "IEmailSender", or similar → `{ContractName}`
- If the user mentioned "Azure Blob", "SendGrid", "SMTP" → `{FirstImpl}`
- Default `Options class needed = Y` unless the impl has zero configuration

## Second-implementation short-circuit

If `src/Modules/{Name}/{Name}.Contracts/` **already exists** (second+ run of the skill for the same module), skip asking about the contract interface. The contract is fixed. Only ask about:

- New implementation name
- New implementation tech
- New options class name
- New DI extension method name

The skill will still rewrite (or append to) the CLAUDE.md to add the new implementation to the `Implementations:` list.

## Ask-in-one-batch rule

As before — confirm pre-filled values, ask about unanswered axes in one batch, wait for confirmation.

## After this step

You have all axes needed for Component archetype. Next: `WriteClaudeMd.md`.
