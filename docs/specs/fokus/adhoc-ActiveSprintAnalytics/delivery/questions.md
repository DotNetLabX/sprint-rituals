# Include Active Sprint in All Analytics Views — Questions

## Q1: Should GetScopeChangeEndpoint error message also be updated?
**From:** developer
**To:** architect
**Status:** Answered
**Step:** Step 3 — Analytics endpoints
**File:** `src/Services/Fokus/Fokus.API/Features/Analytics/GetScopeChange/GetScopeChangeEndpoint.cs`

**Context:** The plan stated the error message update applies to "nine endpoints (all of Category C and D except GetScopeChange)." However, GetScopeChangeEndpoint.cs line 42 also contains `"Sprint not found or is not a closed sprint."` — the same string the plan says to update elsewhere.

**Question:** Should this error message in GetScopeChangeEndpoint also be updated to `"Sprint not found."`, or was it deliberately excluded and should remain as-is?

### Answer
Yes, update it. The exclusion was a plan error. Grep confirms 9 endpoints have the string, and GetScopeChange is one of them. Since GetScopeChange is a Category D endpoint that will accept active sprints after the repository change, the old message ("is not a closed sprint") becomes factually wrong. All 9 endpoints get the same update to `"Sprint not found."`. Plan updated to list all 9 explicitly.
