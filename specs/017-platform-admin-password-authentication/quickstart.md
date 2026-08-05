# Quickstart: Platform Administrator Password Authentication

This guide validates the implementation described by [plan.md](./plan.md). It does not authorize production user or database changes.

## Prerequisites

- .NET 8 SDK.
- Existing SprintLabs development configuration for MySQL, JWT, Identity, Email, and Frontend.
- Test users provisioned through trusted backend data:
  - Active platform admin with confirmed email and password.
  - Active Google-linked platform admin with confirmed email and no password.
  - Active non-admin user with a password.
  - Suspended platform admin.
- A dedicated disposable MySQL test database whose name satisfies the repository fixture safeguards when running optional concurrency tests.

Never use the destructive test fixture against a shared, development, staging, or production database.

## Implementation sequence

1. Add the trusted runtime account-type/result contracts and stable setup conflict.
2. Add the platform-admin Identity service and first focused unit tests.
3. Add the shared three-role login and shared invitation-registration CQRS slices/controller actions.
4. Parameterize the existing access-token service and keep teacher callers explicit.
5. Generalize shared refresh rotation/handler and coordinate its User-row lock with setup/reset.
6. Extend existing forgot/reset eligibility for admins without adding endpoints or providers.
7. Update Swagger/API/frontend docs and add regression tests.
8. Confirm the EF model has no diff; do not generate a migration.

## Build and baseline

```powershell
dotnet restore SprintLabs.sln
dotnet build SprintLabs.sln --no-restore --nologo
dotnet test SprintLabs.sln --no-build --nologo
```

Planning baseline on 2026-08-05:

```text
Build: 0 errors, 33 pre-existing warnings
Tests: 247 passed, 0 failed, 0 skipped
```

## Focused automated verification

Run the new feature tests first, then the established regressions:

```powershell
dotnet test SprintLabs.Tests/Compass.Tests.csproj --no-build --nologo --filter "FullyQualifiedName~PlatformAdminPasswordAuthentication"
dotnet test SprintLabs.Tests/Compass.Tests.csproj --no-build --nologo --filter "FullyQualifiedName~AccessTokenServiceTests|FullyQualifiedName~RefreshLogoutRegressionTests|FullyQualifiedName~PasswordRecoveryHandlerTests"
dotnet test SprintLabs.Tests/Compass.Tests.csproj --no-build --nologo --filter "FullyQualifiedName~B2CLoginCompatibilityTests|FullyQualifiedName~StudentLicenseActivationOnLoginTests|FullyQualifiedName~PendingTeacherActivationTests"
```

The existing Google tests must pass without changed expected responses.

## API smoke verification

Use Swagger or an API client against a local backend with email redirected to a safe test inbox. See the exact payloads in [contracts/platform-admin-password-authentication-api.md](./contracts/platform-admin-password-authentication-api.md).

### Password login

1. Submit the eligible administrator's mixed-case email and password. Confirm PlatformAdmin response, UTC expirations, and one refresh-token hash row.
2. Submit the same account's mixed-case username with surrounding whitespace. Confirm the same user succeeds.
3. Call an existing `/api/v1/admin/communities` operation with the access token. Confirm persisted admin authorization succeeds.
4. Repeat for unknown identifier, wrong password, non-admin, and passwordless admin. Confirm the same 401 error body and no token row.
5. Repeat wrong password until configured lockout. Confirm the safe 423 response and no token.
6. Inspect community/player/student/teacher tables. Confirm no write occurred.

### Platform administrator provisioning

1. Seed an active persisted platform administrator through the trusted backend process using `UserManager.CreateAsync(user, password)`; do not set `PasswordHash` directly and do not use a shared/default password.
2. Confirm no `platform-admins/set-password` operation appears in Swagger.
3. Sign in through `community-login` using the seeded username/email and password; confirm `role: PlatformAdmin` and no community.
4. Confirm Google behavior remains unchanged for existing Google-linked users; the seeded password flow does not modify Google/external-login state.

### Forgot and reset password

1. Request forgot-password for an eligible admin and an unknown identifier. Confirm byte-equivalent public 202 envelope/message.
2. Confirm only the eligible account enters reset email delivery and the link uses configured Frontend base URL.
3. Reset with a valid credential and compliant password. Confirm prior refresh tokens fail, no automatic login occurs, and Google/admin/community state is unchanged.
4. Try invalid/expired token and invalid password policy. Confirm the documented safe errors.

### Refresh and logout

1. Log in as a platform admin and rotate the refresh token once.
2. Confirm the old token cannot rotate and the replacement access token still authorizes the existing admin operation.
3. Logout with the replacement refresh token and confirm it cannot rotate afterward.
4. Suspend or demote the admin before rotation and confirm renewal fails.

## Google behavioral freeze verification

Compare before/after behavior for:

- Google endpoint method, route, and query parameter.
- Allowed-audience token validation.
- `FindOrCreateGoogleUser` lookup/create/update behavior.
- `GoogleId` and player association behavior.
- Player creation, student-license activation, no pending-teacher activation.
- Google claims, legacy token lifetime, and `LoginResponse` fields.

No Google authentication source file should require a behavioral diff. Shared `AccountController`/`ServiceConfig` edits must be adjacent additions only.

## Migration verification

Do not run `dotnet ef migrations add` for this feature. Verify instead:

```powershell
dotnet ef migrations list --project Infrastructure --startup-project API
git diff -- Infrastructure/DataAccess/ApplicationDbContext.cs Infrastructure/Migrations
```

Expected result: no feature-017 migration, no snapshot diff, and no Identity/User/RefreshToken schema change.

## Concurrency verification

When `SPRINTLABS_MYSQL_TEST_CONNECTION` points to a dedicated safe test database, run the focused MySQL cases for:

- Two concurrent first-password requests: at most one succeeds; the loser returns the stable conflict.
- Refresh rotation racing with first-password setup: after setup succeeds, neither the original nor any pre-setup replacement refresh token remains usable.
- Refresh rotation racing with password reset: after reset succeeds, no pre-reset session can renew.

EF Core InMemory tests do not prove MySQL row-lock or transaction behavior. If the environment variable is unavailable, report these cases as not run; do not substitute a shared database.

## Security inspection

- Search test logs and persisted diagnostic/audit data for submitted passwords and raw reset/access/refresh tokens; none may appear.
- Confirm only token hashes are stored in `RefreshTokens`.
- Confirm login/setup requests cannot supply `IsPlatformAdmin`, account type, or target user.
- Confirm access tokens contain no community IDs/roles.
- Confirm protected admin handlers reload persisted `IsPlatformAdmin` rather than trusting returned/JWT account type alone.

## Completion criteria

- Solution build succeeds with no new warnings in touched files.
- Full automated suite passes, including unchanged Google expectations.
- New platform-admin login/setup/recovery/refresh/logout/authorization tests pass.
- Opt-in MySQL concurrency cases pass when a dedicated database is configured.
- Swagger and feature documentation match the contract.
- No EF model or migration diff exists.
