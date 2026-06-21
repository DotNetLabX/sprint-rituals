# Xray Integration — Guide

## Xray Integration Toggle

Controls whether Fokus connects to Xray Cloud for QA analytics. When enabled, sprint syncs will also fetch test execution data from Xray, enriching your sprint tickets with test coverage and pass/fail results. When disabled, Fokus behaves exactly as before — no Xray API calls are made, and no QA data appears anywhere. Previously synced QA data is preserved and will reappear if you re-enable the integration.

## Xray Client ID

The Client ID for authenticating with the Xray Cloud API. You can generate API keys in your Xray Global Settings under the API Keys section. This credential is specific to Xray — it is separate from your Jira credentials. The Client ID does not expire, but the bearer token it generates is refreshed automatically every 24 hours.

## Xray Client Secret

The Client Secret paired with your Client ID for Xray Cloud API authentication. Generated alongside the Client ID in Xray Global Settings > API Keys. This value is stored securely and never displayed in plaintext after saving — you will see a masked value instead. If you need to change it, enter the new secret and save.

## Test Connection

Validates that your Xray credentials are correct and that Fokus can access your project's test data. The test performs two checks: first, it authenticates with Xray using your Client ID and Secret; then, it makes a small query to confirm your credentials have permission to read test executions. If either step fails, the error message will indicate whether the issue is with authentication or data access.

## Sync QA Data

Fetches the latest test execution results from Xray for the selected sprint(s) without re-syncing Jira sprint data. Select one or more sprints from the dropdown (defaults to the most recently synced sprint), then click Sync QA Data. Use this when test executions have been updated in Xray since the last sprint sync and you want to refresh the QA data without pulling all Jira data again. The sync reads issue links from Jira to discover linked Test Executions, then queries Xray's API for the test run results (pass, fail, in progress). Re-syncing is safe — it overwrites previous QA data with fresh results.
