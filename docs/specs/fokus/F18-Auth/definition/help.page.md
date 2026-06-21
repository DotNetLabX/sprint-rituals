# Auth — Help

## Sign in with Google
Fokus uses your company Google account for authentication. Click "Sign in with Google" and select your company email. Only accounts from your organization's Google Workspace domain are allowed — personal Gmail accounts cannot access Fokus.

## User Management
The User Management section lets Admins control who can access Fokus. From here you can invite new users, change roles, and deactivate accounts. Only Admins can see and use this section.

## Invite Email
Enter the full email address of the person you want to invite (e.g., jane@yourcompany.com). The email must belong to your company domain — addresses from other domains will be rejected. Each email can only have one pending invitation at a time.

## Invite Link
After creating an invitation, you'll see a link to share with the invited person. Copy it and send it via email, Slack, or any channel you prefer. The link is valid for 7 days — if it expires before being used, you can send a new invitation. The recipient must sign in with the exact Google account matching the invited email.

## Role: Admin
Admins have full control over Fokus. They can view all analytics data, modify system settings (Jira connection, health thresholds, workflow stages, team configuration), and manage users (invite, change roles, deactivate). There must always be at least one active Admin.

## Role: Manager
Managers can view all analytics data and dashboards — they see exactly the same sprint, developer, and epic metrics as Admins. The difference is that Managers cannot modify settings or manage users. They can view the Settings page to understand how the system is configured, but all fields are read-only.

## Pending Invitations
This list shows invitations that have been sent but not yet accepted. Each invitation shows the invited email, assigned role, and time remaining before expiry. You can revoke a pending invitation if you change your mind — the invite link will stop working immediately.

## Deactivate User
Deactivating a user immediately blocks their access to Fokus. They will be signed out on their next interaction and see a "deactivated" message if they try to sign in again. Their account data is preserved — you can reactivate them at any time to restore access.

## Reactivate User
Reactivating a user restores their access to Fokus with their previous role. They can sign in with Google again immediately after reactivation.

## Settings (Read-Only)
As a Manager, you can view the full system configuration — Jira connection details, board settings, health score thresholds, workflow stages, and team setup. This transparency helps you understand how metrics are calculated and what drives the analytics you see. To request changes, contact an Admin on your team.

## Company Domain
Your company domain (e.g., acme.com) determines which Google accounts can access Fokus. It was set automatically when the first user signed in, based on their email address. All invitations and logins are restricted to this domain — this ensures only your organization's members can access sensitive sprint and developer analytics.
