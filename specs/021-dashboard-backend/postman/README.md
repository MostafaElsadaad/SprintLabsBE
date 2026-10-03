# Run the seeded dashboard collection

Files:
- SprintLabs.Dashboard.postman_collection.json
- SprintLabs.DemoLocal.postman_environment.json

## First run
1. Restart the backend API against the intended Development/test database. Startup applies pending migrations and runs the enabled demo seeder; this collection does not start the API or configure its database.
2. Import both JSON files into the current Postman desktop app. Select **SprintLabs Demo Local** as the environment.
3. Enter **seedPassword** locally using the value declared at Infrastructure/Seed/DemoCommunitySeeder.cs line 15. All three configured demo accounts share it. The exported environment deliberately contains no password/token values.
4. baseUrl defaults to **https://localhost:7264**. Trust the local ASP.NET development certificate if needed; do not disable TLS verification. Use the actual local HTTPS port if yours differs.
5. Open **SprintLabs Dashboard - Seeded End-to-End Tests**, choose **Run**, select **Functional > Local**, select all folders in their existing order, and use **1 iteration**, **0 delay**, no data file. In Advanced settings, keep **Persist responses for a session** enabled to inspect response bodies; enable **Turn off logs during run** so request headers/bodies are not copied to the console. Leave **Keep variable values** off and clear **Stop run if an error occurs** so later cleanup can still be attempted. Setup failures explicitly stop through the scripts. Click **Start run**. Run with Postman's desktop HTTP agent if using its web UI.
6. Open each request result to inspect its status, response body and assertion results. Expected error responses (400/401/403/404/409) are passing tests when they match the scenario.

There are **87 request definitions in 8 folders**. The default configuration executes **72** and skips **15 email-flow requests**. Login, tokens, grade/class/teacher IDs, dates and cleanup bodies are captured automatically. Do not rearrange the run or run only a dependent folder. Missing credentials or the wrong demo community stops setup; failed dependencies are identified and skipped.

## Default coverage
All 24 current dashboard/management operations are represented, plus community login. The default run covers positive reads, frontend response fields, numeric zero placeholders, assigned-class visibility, permissions, paging/filters, create/update/duplicate class checks, additive/idempotent teacher assignments, existing full teacher reassignment, teacher name/title edits and restoration, read-only login email, in-app notification delivery/privacy, class archive/retry, and removed duplicate routes.
The invite/resend/cancel/teacher-remove endpoints have permission or unknown-resource checks in the default run. Their successful lifecycle tests are in the optional email folder.

## Successful invitation/removal lifecycle (optional)
Set **runEmailInvitationTests=true** and **invitationEmail** to a fresh disposable mailbox you control, with working test SMTP delivery configured on the backend. Run the whole collection again. Seeded @sprintlabs.local identities are rejected here; a mailbox already present in teacher membership records disables this optional folder instead of changing that account. Use a fresh mailbox/alias on another email-enabled run because removed memberships remain stored.
The flow sends two invitations plus one resend, captures/rotates invitation IDs, checks obsolete cancellation rejection, cancels twice, re-invites, then removes only the disposable pending teacher twice and verifies its INACTIVE/NOT_ASSIGNED state. It does not accept an emailed registration token or remove a seeded teacher.
Resend handles 429 by retrying at roughly one-second intervals for at most **resendMaxWaitSeconds** (default 120). Repeated attempts appear in the Runner. Raise that value if your configured cooldown is longer. No manual invitation-token copying is needed.

## Persistent effects and cleanup
A complete run restores Teacher 1's original name/title and original active class IDs. It archives only the uniquely named class created by this run. Teacher 2's temporary assignment is on that archived class and is no longer visible in active class views. All seeded memberships remain active.
One unread test notification, its management activity records, and the archived temporary class remain per run because no deletion/read API was requested. The optional email flow also leaves a removed disposable teacher membership and revoked invitation history. This collection therefore belongs on the test database.
If stopped before cleanup, use the corresponding request responses to identify the temporary class and original teacher profile/classes, restore those values through the existing PATCH/PUT endpoints, and archive the temporary class via DELETE. Avoid simultaneous runs against the same seeded teacher.
Access tokens and generated request state use **run-local pm.variables**, not collection/environment writes. Their scope ends with the collection run. Do not export login request/response results or an environment after adding credentials.

## Validation and limits
The collection JSON was checked against the official Postman v2.1 schema structure, all scripts were syntax checked, variable references/credential storage/cleanup targets were inspected, and setup/body serialization and optional skip behavior were checked locally. No live API/database call or email delivery was performed while creating these artifacts. Run results depend on your seed data, migration state, HTTPS trust and SMTP configuration. The backend's previously completed regression run has 416 passed / 2 MySQL-fixture skips.

Postman references: [Collection schema](https://schema.postman.com/), [run-local variables](https://learning.postman.com/docs/tests-and-scripts/write-scripts/postman-sandbox-reference/pm-variables/), [skip/retry execution](https://learning.postman.com/latest-v-12/docs/tests-and-scripts/write-scripts/postman-sandbox-reference/pm-execution), [Runner settings and response inspection](https://learning.postman.com/latest-v-12/docs/tests-and-scripts/running-collections/intro-to-collection-runs).
