# Jira Sync — Lessons

## Architect Lessons
- [TRACKED] Plan should specify JSON property name attributes for Jira custom fields (story points is `customfield_10016` in Jira Cloud, not a built-in field name) — the spec said "standard field" but Jira's implementation uses a custom field ID
- [TRACKED] Plan step for rate limiting should note that lease acquisition must happen inside the retry loop, not outside — the retry pattern and rate limiting pattern interact
- [APPLIED] When planning backlog sync endpoints, specify error reporting shape upfront (failure counts vs failure lists) to avoid reviewer findings

## Developer Lessons

## Reviewer Lessons
