# F34-DeveloperDetailPage — Questions

## Q1: Analytics Targets settings placement
**Status:** Answered — subsection in Sync tab (user confirmed)
**To:** PO
**Context:** The spec says the bug ratio target is configurable in Settings under a new "Analytics Targets" section. The SettingsView currently uses tabs: Jira, Workflow, Health, Sync, Xray, Users. The Sync tab already houses bug ratio alert settings (threshold, consecutive sprint count, default SP per bug).
**Question:** Should "Analytics Targets" be a new tab in Settings, or a new subsection within the existing Sync tab? A new tab for a single field (bugRatioTarget) feels light. Adding it to Sync keeps related analytics config together, but the name "Sync" doesn't obviously cover it. Alternatively, renaming the Sync tab to "Analytics" and grouping both bug ratio alerts and analytics targets there.
**Options:**
  1. New "Analytics" tab (rename is not possible without breaking existing links — so a new separate tab)
  2. New subsection within the existing Sync tab
  3. New dedicated "Analytics Targets" tab (single-field tab)
**Recommendation:** Option 2 (subsection in Sync tab) — keeps related analytics config together and avoids a single-field tab.
