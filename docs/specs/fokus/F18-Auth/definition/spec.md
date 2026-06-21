# Authentication & Access Control

**Traces to:** `docs/product/v1.md` §3.2 (extends — v1 declared no user auth; this adds it)
**Source:** Scratch
**Dependencies:** None
**Status:** Done
**Plan:** `docs/plans/Auth/plan.md`

---

## Purpose

Fokus runs fully open today — no login, no access control. As it expands to serve multiple managers, it needs authentication (who are you?) and authorization (what can you do?) to protect sensitive sprint and developer analytics. Google OAuth with invitation-based access gates the app to company members. Two roles separate configuration authority (Admin) from data consumption (Manager).

## Entities

### AppUser

An identity representing a person who can access Fokus.

- **GoogleId** — string, unique. The Google subject identifier (`sub` claim) that permanently identifies this account.
- **Email** — string, unique, case-insensitive. Must belong to the company domain.
- **DisplayName** — string. Sourced from the Google profile on login.
- **AvatarUrl** — string, nullable. Google profile picture URL.
- **Role** — one of: Admin, Manager. Determines what the user can change — not what they can see.
- **CreatedAt** — DateTime. When the account was created.
- **LastLoginAt** — DateTime, nullable. Updated on each successful authentication.
- **IsActive** — bool, default true. When false, the user is blocked from accessing the app.

An AppUser is created through one of two paths:
- **Bootstrap:** the first Google login from the company domain auto-creates an Admin.
- **Invitation acceptance:** authenticating via a valid invite link creates an account with the role specified in the invitation.

### Invitation

A time-limited token granting a specific email the right to create an account.

- **Email** — string. The invited email address. Must belong to the company domain.
- **Role** — one of: Admin, Manager. Default: Manager. The role assigned upon acceptance.
- **InvitedBy** — the Admin who created this invitation.
- **Token** — string, unique. URL-safe random token embedded in the invite link.
- **CreatedAt** — DateTime.
- **ExpiresAt** — DateTime. 7 days after creation.
- **AcceptedAt** — DateTime, nullable. Set when the recipient authenticates.
- **Status** — one of: Pending, Accepted, Expired, Revoked.

Transitions: Pending → Accepted (used), Pending → Revoked (Admin cancels), Pending → Expired (7 days elapse).

### CompanyDomain (configuration, not entity)

- **Domain** — string. The Google Workspace domain allowed to access Fokus (e.g., `acme.com`).
- Captured automatically from the first user's email during bootstrap.
- Stored as an application setting. Not user-editable after bootstrap.

## User Flows

```
Flow 1: Bootstrap (First User)
1. User navigates to the app
2. System detects no authenticated session → shows login page with "Sign in with Google"
3. User clicks sign in → redirected to Google OAuth consent
4. User authenticates with a company Google account
5. System detects no AppUser records exist → creates the user as Admin
6. System captures the email domain as the company domain
7. User lands on the Dashboard
```

```
Flow 2: Invite a User
1. Admin navigates to User Management (within Settings)
2. Admin enters an email address (role defaults to Manager)
3. System validates the email belongs to the company domain → rejects if not
4. System creates an Invitation with a unique token and 7-day expiry
5. System displays the invite link for the Admin to copy
6. Admin shares the link with the recipient (email, Slack, etc.)
7. Invitation appears in the Pending list with expiry countdown
```

```
Flow 3: Accept Invitation
1. Recipient opens the invite link in their browser
2. System validates the token: exists, status is Pending, not expired
3. If invalid → error page ("expired", "revoked", or "already used")
4. If valid → redirects to Google OAuth
5. User authenticates with Google
6. System verifies the authenticated email matches the invitation email (case-insensitive)
7. If mismatch → error: "Sign in with the email this invitation was sent to"
8. System creates an AppUser with the invitation's role
9. Invitation status → Accepted
10. User lands on the Dashboard
```

```
Flow 4: Regular Login
1. User navigates to the app
2. No authenticated session → login page
3. User signs in with Google
4. System looks up AppUser by GoogleId
5. Found + active → session created, Dashboard
6. Found + inactive → "Your account has been deactivated. Contact your administrator."
7. Not found → "Access denied. You need an invitation to use Fokus."
```

```
Flow 5: Manage Users
1. Admin navigates to User Management
2. Table shows: Name, Email, Role, Last Login, Status
3. Admin can:
   a. Change role (Manager ↔ Admin)
   b. Deactivate a user (blocks access, preserves data)
   c. Reactivate a previously deactivated user
4. Changes take effect on the user's next request
```

```
Flow 6: Revoke Invitation
1. Admin views Pending Invitations in User Management
2. Admin clicks Revoke on a pending invitation
3. Invitation status → Revoked
4. Recipient opening the link sees: "This invitation has been revoked."
```

```
Flow 7: Settings (Manager View)
1. Manager navigates to Settings
2. All configuration fields are visible but disabled (read-only)
3. A visual indicator communicates: "View only — contact an Admin to make changes"
4. User Management section is not visible to Managers
```

## API Surface

| Method | Route | Auth | Request body | Response | Status codes |
|--------|-------|------|-------------|----------|-------------|
| GET | `/api/auth/me` | Any authenticated | — | `{ id, email, displayName, avatarUrl, role }` | 200, 401 |
| POST | `/api/auth/logout` | Any authenticated | — | — | 204, 401 |
| GET | `/api/users` | Admin | — | `[{ id, email, displayName, avatarUrl, role, lastLoginAt, isActive }]` | 200, 403 |
| PUT | `/api/users/{id}/role` | Admin | `{ role }` | Updated user | 200, 400, 403, 404 |
| PUT | `/api/users/{id}/status` | Admin | `{ isActive }` | Updated user | 200, 400, 403, 404 |
| POST | `/api/invitations` | Admin | `{ email, role? }` | `{ id, email, role, inviteLink, expiresAt }` | 201, 400, 403, 409 |
| GET | `/api/invitations` | Admin | — | `[{ id, email, role, status, createdAt, expiresAt }]` | 200, 403 |
| DELETE | `/api/invitations/{id}` | Admin | — | — | 204, 403, 404 |
| GET | `/api/invitations/{token}/validate` | Anonymous | — | `{ valid, email, error? }` | 200 |

**Auth flow routes** (ASP.NET Core middleware, not manual endpoints):
- `GET /auth/login` — triggers Google OAuth challenge
- `GET /auth/callback` — Google OAuth callback (middleware-handled)

**Error conditions:**
- 400 — invalid email domain, invalid role value, last-Admin protection violation
- 401 — no authenticated session
- 403 — Manager attempting an Admin-only operation
- 404 — user or invitation not found
- 409 — email already has an account or a pending invitation

## Business Rules

1. **Company domain lock.** The company domain is captured from the first user's email during bootstrap. All subsequent invitations and logins must match this domain. Email domain comparison is case-insensitive.
2. **Bootstrap is one-time.** Auto-create Admin only triggers when zero AppUser records exist. After that, all access requires an invitation.
3. **Invitation expiry.** Invitations expire 7 days after creation. Expired invitations cannot be accepted. A new invitation can be sent to the same email after the previous one expires or is revoked.
4. **Email uniqueness.** Each email can have at most one active AppUser and one Pending invitation. Duplicate attempts return 409.
5. **Last Admin protection.** At least one active Admin must exist at all times. Demoting or deactivating the last Admin is rejected (400).
6. **Role controls writes, not reads.** All authenticated users see all data on all pages. Role only determines who can modify Settings and manage users/invitations.
7. **Deactivation enforcement.** Deactivated users are blocked on their next API call. The system checks IsActive on every authenticated request — not just at login.
8. **Google profile sync.** DisplayName and AvatarUrl refresh from Google on each login. Email and GoogleId are immutable after account creation.
9. **No automated email.** The system generates an invite link. It does not send emails. The Admin copies and delivers the link manually.
10. **Invitation email match.** The Google account used to accept an invitation must match the invited email. Authenticating with a different email is rejected, even if it belongs to the company domain.
11. **Cookie security.** Session uses persistent cookies with SameSite=Lax. API endpoints require JSON content-type, providing implicit CSRF protection.
12. **Frontend 401 handling.** If any API call returns 401, the frontend redirects to the login page automatically.

## Acceptance Criteria

- [ ] Unauthenticated users see a login page — no dashboard data is visible or fetchable
- [ ] First Google login creates an Admin account and lands on the Dashboard
- [ ] Company domain is captured from the first user's email and enforced on all future access
- [ ] Admin can create an invitation for a company-domain email and receives a copyable invite link
- [ ] Inviting a non-company-domain email returns 400 with a clear error
- [ ] Inviting an email that already has an account or pending invitation returns 409
- [ ] Recipient opens invite link → authenticates with Google → lands on Dashboard as Manager
- [ ] Expired invitation link (>7 days) shows an expiry error page
- [ ] Revoked invitation link shows a revocation error page
- [ ] Accepting with a different Google email than invited shows a mismatch error
- [ ] Manager sees all dashboard pages with identical data to Admin
- [ ] Manager sees Settings in read-only mode with a visual indicator
- [ ] Manager cannot see User Management and cannot call Admin-only endpoints (403)
- [ ] Admin can change a user's role (Manager ↔ Admin)
- [ ] Admin can deactivate a user; the user is blocked on next request
- [ ] Admin can reactivate a previously deactivated user
- [ ] Demoting or deactivating the last Admin returns 400
- [ ] `GET /api/auth/me` returns current user profile and role
- [ ] Logout clears the session and returns to the login page
- [ ] All existing API endpoints return 401 for unauthenticated requests

## Out of Scope

- **Automated email delivery** — invite links are generated, not emailed. Admin distributes manually. Email service integration (SendGrid, SES) is a future enhancement.
- **SAML / enterprise SSO** — Google OAuth only. SAML is appropriate for larger deployments.
- **SCIM provisioning** — user lifecycle is manual (invite/deactivate). Automated sync from Google Workspace is a future enhancement.
- **Fine-grained data visibility** — no per-team or per-developer scoping. All authenticated users see all data. Role-based data filtering is a future enhancement.
- **Audit logging** — no log of who changed settings, invited whom, or deactivated whom. Can be added as a cross-cutting concern later.
- **Multi-tenancy** — single organization, single company domain.
- **Password-based login** — Google OAuth is the only authentication method.
- **Self-registration** — no one can create an account without an invitation (except the bootstrap Admin).
