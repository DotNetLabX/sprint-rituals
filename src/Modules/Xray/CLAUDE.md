# Xray Module

**Type:** Component (infra adapter)
**Contract:** IXrayClient
**Implementations:** GraphQL

## Purpose

Typed HTTP client for the Xray Cloud GraphQL API. Handles bearer token authentication (with 23-hour proactive refresh), GraphQL query execution, rate limiting, and error mapping. All Xray API contracts (IXrayClient, XrayTestExecutionResult, XrayTestExecutionDto, XrayTestRunDto) live in `Xray.Contracts`.

## Structure

- `Xray.Contracts/` — interface IXrayClient and all Xray API DTOs. Has zero project references.
- `Xray.GraphQL/` — Bearer token manager, typed HttpClient wrapping Xray Cloud GraphQL API with pagination and rate limiting. DI extension: `AddGraphQLXray()`

## Host wiring

Host services reference `Xray.Contracts` + `Xray.GraphQL`. The DI extension registers `IXrayClient` as `GraphQLXrayClient` (scoped) and `XrayBearerTokenManager` as singleton (token cache is cross-request). Credentials are NOT read from appsettings.json — they are runtime-configurable via AppSettings (stored in DB). Credentials are passed per-call to `IXrayClient` methods, not via IOptions at startup.

## Credential passing pattern

Unlike JiraClient (which reads credentials from IOptions at startup), XrayClient receives credentials per-call because they are runtime-configurable via the Settings UI. The bearer token manager caches tokens keyed by clientId to handle credential changes without restart.

## Authentication

POST `https://xray.cloud.getxray.app/api/v2/authenticate` with `{ "client_id": "...", "client_secret": "..." }` — response body is a raw JWT string (not JSON-wrapped). Tokens expire after 24 hours; cached for up to 23 hours.

## Rate limiting

Standard tier: 300 requests per 5-minute window. A sliding window counter limits concurrent sync requests. Exceeding the window results in exponential backoff.

## Adding a new implementation

Re-run `create-module Xray` with the new impl name. The skill detects the existing `.Contracts` project and scaffolds only the new implementation project.
