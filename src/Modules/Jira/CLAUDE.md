# Jira Module

**Type:** Component (infra adapter)
**Contract:** IJiraClient
**Implementations:** RestApi

## Purpose

Typed HTTP client for the Jira REST API. Handles authentication, pagination, and error mapping. All Jira API contracts (JiraBoard, JiraSprint, JiraIssue, JiraChangelog, JiraPagedResult, SprintState) live in `Jira.Contracts`.

## Structure

- `Jira.Contracts/` — interface IJiraClient and all Jira API DTOs (JiraBoard, JiraSprint, JiraIssue, JiraChangelog, JiraPagedResult, SprintState). Has zero project references.
- `Jira.RestApi/` — Refit typed HTTP client wrapping Jira REST API with pagination and Basic Auth. DI extension: `AddRestApiJira()`

## Host wiring

Host services reference `Jira.Contracts` + exactly one implementation project. The DI extension registers the concrete type against IJiraClient. Host config supplies `JiraOptions` (instance URL, email, API token) via `appsettings.json` under the `"Jira"` section.

## Adding a new implementation

Re-run `create-module Jira` with the new impl name. The skill detects the existing `.Contracts` project and scaffolds only the new implementation project.
