# Authentication & Access Control — Summary

## Status: COMPLETE

## What Was Built
Google OAuth authentication with cookie-based sessions, an invitation-only onboarding system, two-role authorization (Admin/Manager), and full enforcement across all existing endpoints. The frontend gained a login page, invite acceptance flow, route guards, 401 interception, user management UI in Settings (Admin only), and read-only Settings enforcement for Managers.

## Key Outcomes
- 38 files created, 33 files modified (26 endpoint files for AllowAnonymous removal, 7 for Admin role gates)
- Build: PASS (0 errors, 3 pre-existing warnings)
- Review: APPROVED after 1 fix cycle (cycle 1: REQUEST CHANGES, cycle 2: APPROVE)
- All 12 plan steps implemented

## Deviations from Plan
- ConflictException added (Step 7): Plan referenced 409 responses for duplicate invitations but no ConflictException existed. Developer created it alongside ForbiddenException in Blocks.Exceptions. Reasonable gap fill.
- FastEndpoints global pre-processor registration (Step 8): Plan said to register ActiveUserPreProcessor in DependencyInjection.cs. Developer registered it both as a scoped service and in the UseFastEndpoints configurator, which is required for FastEndpoints to discover global pre-processors. Correct deviation from incomplete plan instruction.

## Notes
- Google OAuth requires User Secrets configuration before first run: Google:ClientId and Google:ClientSecret must be set via dotnet user-secrets.
- The first user to log in becomes the bootstrap Admin and their email domain becomes the company domain restriction for all future invitations.
- The EF migration AddAuth must be applied before running the updated app.
