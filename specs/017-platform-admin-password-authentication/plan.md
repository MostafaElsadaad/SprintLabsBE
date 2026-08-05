# Implementation Plan: Platform Administrator Password Authentication

**Branch**: `feature/teacher-email-authentication` (current working branch; setup created no feature branch) | **Date**: 2026-08-05 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `/specs/017-platform-admin-password-authentication/spec.md`

## Summary

Add a separate platform-administrator password login and authenticated first-password setup slice while preserving Google SSO exactly as it behaves today. The new endpoints will use a narrow platform-admin Identity adapter, the existing JWT signer, the existing hashed refresh-token store and rotation/logout routes, the existing password-reset provider, and the existing SMTP/frontend-link flow. Shared token and recovery paths will be made account-aware without changing teacher behavior or creating administrator-specific token/recovery implementations. The inspected Identity/User/RefreshToken schema already contains every required field and index, so this feature plans no EF Core migration.

## Technical Context

**Language/Version**: C# / .NET 8

**Primary Dependencies**: ASP.NET Core Web API and Identity, MediatR, EF Core 8 with Pomelo MySQL, JWT bearer authentication, Swashbuckle, SMTP email infrastructure

**Storage**: Existing MySQL `Users`, Identity tables, and `RefreshTokens`; no new table, column, relationship, or index

**Testing**: xUnit, Moq, FluentAssertions, EF Core InMemory for focused Identity/handler tests, and the existing opt-in MySQL fixture for row-lock concurrency verification

**Target Platform**: ASP.NET Core backend web API

**Project Type**: Layered web service (`API`, `Application`, `Domain`, `Infrastructure`, `Shared`)

**Performance Goals**: Preserve current password-authentication and token-rotation latency; identifier lookup uses existing normalized Identity indexes and refresh lookup uses the existing unique token-hash index

**Constraints**: Reuse one JWT/refresh/logout/recovery/email/Identity stack; persisted `IsPlatformAdmin` is authoritative; no community dependency; no player/teacher/student side effects; Google endpoint, validation, account lookup/linking, player workflow, claims, JWT, response, and regression expectations remain unchanged; no secrets in logs

**Scale/Scope**: Existing SprintLabs platform-administrator population; two new endpoint slices plus narrowly shared access-token, refresh, recovery, documentation, and test changes

## Constitution Check

### Pre-design gate

| Gate | Result | Evidence |
|---|---|---|
| Vertical slice delivery | PASS | Login and first-password setup include controller contracts, CQRS slices, Infrastructure Identity behavior, shared token integration, tests, Swagger, and frontend-facing documentation. |
| Existing architecture wins | PASS | API remains thin; Application uses MediatR; the service interface is in Domain; implementation and Identity/EF behavior remain in Infrastructure; shared results/errors stay in Shared. |
| SaaS isolation | PASS | Platform administrators require no community and the feature explicitly performs no community selection, membership creation, or community-role claim issuance. |
| Minimum useful implementation | PASS | One narrow platform-admin Identity adapter is added; existing token, refresh, logout, recovery, email, repositories, and configuration are extended in place. |
| Quality gates | PASS | Baseline build succeeds and 247 tests pass; the plan adds focused auth/security tests plus unchanged Google regression execution. |
| Documentation | PASS | The plan produces a contract, quickstart, `api.md`, and `frontend.md` next to the spec. |
| Persistence safety | PASS | Snapshot and migrations prove all required fields/indexes exist; no migration is planned. |

### Post-design gate

PASS. Phase 1 design adds no project reference, package, custom repository, duplicate token/password-reset provider, community dependency, or schema change. The narrow platform-admin service owns only Identity eligibility and first-password orchestration; it delegates session storage/revocation to existing services. No constitution exception is required.

## Current Implementation Map

| Concern | Current implementation | Planned disposition |
|---|---|---|
| Google login | `AccountController.GoogleLogin` → `GoogleAuthenticationCommandHandler` → `GoogleAuthenticationService`, `UserService.FindOrCreateGoogleUser`, and `ExternalPlayerLoginWorkflow` | Preserve unchanged, including the current player creation/linking behavior and legacy Google JWT/response. New password flows never call this workflow. |
| User/Identity schema | `User : IdentityUser<long>` plus `IsPlatformAdmin`, `GoogleId`, `Status`, reset cooldown; Identity normalized/password/confirmation/lockout fields in snapshot | Reuse unchanged; no migration. |
| Identity policy | `API/Program.cs` configures unique email, confirmed email, password validators, lockout, and custom reset provider | Reuse dynamically for platform-admin eligibility/password operations; do not add a second policy/provider. |
| Platform-admin authorization | `[Authorize]` controller, signed `userId` claim, then `AdminCommunityAuthorization` reloads persisted `IsPlatformAdmin` and suspension state | Reuse exactly; `accountType` remains informational and no new weaker policy is introduced. |
| Password access token | `AccessTokenService` creates UTC short-lived JWT but hardcodes `accountType=Teacher` | Parameterize with a trusted internal account-type enum; existing teacher callers pass Teacher and new admin caller passes PlatformAdmin. Google does not call this service. |
| Refresh token | `RefreshTokenService` hashes/rotates/revokes, but rotation is teacher-only; refresh handler reloads only a teacher | Keep one route/store/service; generalize rotation result and eligibility for active confirmed platform admins, with PlatformAdmin precedence for dual-flag users. |
| Logout | Existing handler delegates to `IRefreshTokenService.RevokeAsync` | Reuse with no behavior change. |
| Forgot/reset | Existing Account routes and handlers call `TeacherIdentityService`, Identity reset provider, link builder, SMTP, cooldown, and refresh revocation | Extend eligibility in place for platform admins without a new endpoint or provider; preserve teacher branch behavior. |
| Email | `IEmailService.SendPasswordResetEmailAsync` and `SmtpEmailService` use configured SMTP; link builder uses `Frontend.BaseUrl` | Reuse unchanged. |
| Error envelope | `BaseResponse`, `GenericException`, `ErrorCode`, `ErrorMessage` | Add only `PlatformAdminPasswordAlreadyConfigured`; retain generic invalid credentials and existing reset error. |

## Project Structure

### Documentation (this feature)

```text
specs/017-platform-admin-password-authentication/
|-- spec.md
|-- plan.md
|-- research.md
|-- data-model.md
|-- quickstart.md
|-- api.md
|-- frontend.md
|-- contracts/
|   `-- platform-admin-password-authentication-api.md
|-- checklists/
|   `-- requirements.md
`-- tasks.md                         # Created later by /speckit-tasks
```

### Source Code (repository root)

```text
API/
`-- Controllers/
    `-- AccountController.cs                         # add two actions only; preserve Google action

Application/Features/Accounts/
|-- PlatformAdminAuthentication/
|   |-- PlatformAdminLogin/
|   |   |-- PlatformAdminLoginCommand.cs
|   |   |-- PlatformAdminLoginCommandHandler.cs
|   |   |-- PlatformAdminLoginRequest.cs
|   |   `-- PlatformAdminLoginResponse.cs
|   `-- SetPlatformAdminPassword/
|       |-- SetPlatformAdminPasswordCommand.cs
|       |-- SetPlatformAdminPasswordCommandHandler.cs
|       |-- SetPlatformAdminPasswordRequest.cs
|       `-- SetPlatformAdminPasswordResponse.cs
`-- TeacherAuthentication/
    |-- ForgotPassword/                              # retain handler/route; shared eligibility changes below
    |-- ResetPassword/                               # retain handler/route; shared eligibility changes below
    `-- RefreshToken/
        `-- RefreshTokenCommandHandler.cs             # consume account-aware rotation result

Domain/Services/
|-- IPlatformAdminIdentityService.cs                 # add narrow login/setup boundary
|-- IAccessTokenService.cs                           # trusted account-type parameter
`-- IRefreshTokenService.cs                          # account-aware rotation result

Infrastructure/Services/
|-- PlatformAdminIdentityService.cs                  # add Identity login/setup implementation
|-- AccessTokenService.cs                            # parameterize; preserve claims/lifetime
|-- RefreshTokenService.cs                           # shared eligibility/locking/rotation
`-- TeacherIdentityService.cs                        # extend existing forgot/reset eligibility only

Infrastructure/
`-- ServiceConfig.cs                                 # register platform-admin service; preserve Google registration

Shared/
|-- Enums/
|   |-- AuthenticatedAccountType.cs                  # trusted non-persisted Teacher/PlatformAdmin values
|   |-- ErrorCode.cs                                 # add stable conflict code
|   `-- ErrorMessage.cs                              # add stable conflict message
`-- Responses/
    |-- PlatformAdminIdentityResult.cs               # trusted persisted identity projection
    `-- RotatedRefreshTokenResult.cs                 # replacement token plus trusted token subject

SprintLabs.Tests/Features/
|-- PlatformAdminPasswordAuthentication/             # new focused feature tests
|-- TeacherEmailAuthentication/
|   `-- AccessTokenServiceTests.cs                   # retain teacher assertions; add admin claims
`-- InviteOnlyTeacherAuthentication/
    |-- PasswordRecoveryHandlerTests.cs              # existing contract remains
    |-- RefreshLogoutRegressionTests.cs              # update shared refresh dependency
    `-- TeacherIdentityServiceTests.cs                # teacher regression remains
```

No file under `Infrastructure/Migrations/` or the model snapshot is changed.

**Structure Decision**: Keep the two new administrator actions in their own Application vertical slices. Add one narrow Domain/Infrastructure Identity adapter because Application cannot depend on `UserManager<User>` directly and platform-admin eligibility is separate from teacher authentication. Extend the existing recovery and token services rather than introducing administrator-specific refresh, logout, email, or reset stacks. Normal persistence continues through the existing Identity/EF context and refresh repository; no feature-specific repository is justified.

## Implementation Strategy

### 1. Freeze the Google authentication path

Treat the Google map in [research.md](./research.md) as a behavioral freeze. Do not modify Google command/request/response types, token validation, allowed audiences, `FindOrCreateGoogleUser`, external player resolution, claims, legacy JWT lifetime, response population, or Google regression expectations. `AccountController` receives adjacent actions only; its Google method body/signature remains byte-for-byte behaviorally equivalent. `ServiceConfig` receives only the new platform-admin registration; existing Google registrations remain unchanged.

The existing Google JWT already contains signed `userId`, so a Google-authenticated administrator can call the new authenticated setup action through the current bearer authentication and persisted admin check. Do not add claims to Google JWTs to support this endpoint.

### 2. Add the platform-admin Identity boundary

Add `IPlatformAdminIdentityService` in Domain and `PlatformAdminIdentityService` in Infrastructure with two responsibilities:

1. Authenticate a normalized username/email plus password.
2. Configure the authenticated administrator's first password.

Login trims the identifier, computes Identity-normalized email and username, resolves at most one user, and treats ambiguous lookup as no match. Before password verification require persisted `IsPlatformAdmin`, Active status, a configured password, and confirmed email only when the current Identity sign-in policy requires it. Use `CheckPasswordSignInAsync(..., lockoutOnFailure: true)` so wrong passwords update/reset access-failure state exactly as Identity currently does. Unknown, wrong-password, non-admin, and passwordless cases use the same 401 `Failure`/`InvalidAccessToken`; locked-out uses the existing safe 423 form.

Return only trusted persisted identity values (`UserId`, `Name`, `UserName`, `Email`). The service never queries or writes communities, players, student licenses, teacher state, `GoogleId`, or Identity external-login rows.

### 3. Add the password-login vertical slice

Add anonymous `POST platform-admins/login` under the versioned Account controller. The controller maps the request and source IP into `PlatformAdminLoginCommand`; the handler calls the new Identity service, then the existing access-token and refresh-token services.

Parameterize `IAccessTokenService.Create` with `AuthenticatedAccountType`. Preserve issuer, audience, signing algorithm, lifetime, UTC timestamps, `sub`, `jti`, `userId`, email, and name claims. Existing teacher callers pass Teacher; the new handler passes PlatformAdmin. Do not add community IDs/roles or trust `accountType` supplied by a client.

Map the required response fields including persisted username and `accountType = PlatformAdmin`. Issue tokens only after all eligibility checks succeed. This slice calls no Google/player or community workflow.

### 4. Add authenticated self-only first-password setup

Add `[Authorize] POST platform-admins/set-password`. The controller reads only the signed `userId` claim and sends it with `newPassword`; the request contains no target identity or privilege field. The Infrastructure service reloads and locks that user, rechecks Active persisted `IsPlatformAdmin`, rejects an existing password with HTTP 409 and `PlatformAdminPasswordAlreadyConfigured`, and calls `UserManager.AddPasswordAsync` for a passwordless account.

Map Identity password-validator failures to the established validation envelope using Identity error codes/descriptions only; never echo or log the password. On relational storage, acquire the existing User row inside a transaction before the password check, add the password, revoke all active refresh tokens through `IRefreshTokenService.RevokeAllForUserAsync`, and commit. On a concurrency failure, reload password state: a concurrent winner maps to the stable conflict; other Identity errors remain safe validation/failure results.

Return only `Password configured successfully. Please sign in again.` and no tokens. `AddPasswordAsync` must be the only password write and no Google association is read, removed, or replaced.

### 5. Generalize the existing refresh path, not its public contract

Keep `POST Account/refresh-token`, `POST Account/logout`, `RefreshToken`, its repository, raw-token generation, SHA-256 hashing, expiration, conditional revocation, replacement links, and JSON response shape.

Change rotation to return a trusted account-aware result containing user ID, email, name, and `AuthenticatedAccountType` along with the replacement refresh token. Inside one relational transaction:

1. Hash and locate the submitted token.
2. Lock the associated User row.
3. Reload/recheck the token after obtaining the user lock.
4. Require Active status and current confirmation policy; accept persisted platform admins or the existing eligible teacher path.
5. Choose PlatformAdmin when `IsPlatformAdmin` is true, otherwise Teacher. This deterministic precedence avoids persisting token-origin metadata and guarantees an administrator refresh is never presented as a community/teacher session.
6. Conditionally revoke the submitted hash and create the replacement before commit.

The refresh handler creates the new access token directly from that trusted result and no longer reloads only through `ITeacherIdentityService`. Logout remains unchanged and account-agnostic.

Use the same User-row lock ordering in first-password setup and reset so rotation either commits before session-wide revocation (and its replacement is then revoked) or waits and observes the post-password revocation state. EF InMemory tests cover business outcomes; the existing opt-in MySQL fixture validates the actual row-lock race without any schema change.

### 6. Extend forgot/reset in place

Retain the existing Account endpoints, requests, handlers, `TeacherAuthenticationLinkBuilder`, `PasswordResetDispatchResult`, cooldown field, Identity provider, `IEmailService`, SMTP implementation, generic 202 response, and URL-safe reset link.

Extend `TeacherIdentityService.CreatePasswordResetAsync` eligibility as a union:

- Existing teacher eligibility remains unchanged, including its community rule.
- Platform-admin eligibility requires persisted `IsPlatformAdmin`, Active status, a configured password, deliverable email, and confirmed email only when the current policy requires it; it performs no community query.

Extend `ResetPasswordAsync` with the same platform-admin eligibility while retaining teacher behavior. Use `UserManager.ResetPasswordAsync`, classify invalid-token errors into the existing safe reset code, expose password-policy errors only through safe validation data, lock the User row, revoke all active refresh tokens in the same transaction, issue no session, and leave `IsPlatformAdmin`, `GoogleId`, Identity login rows, and all memberships unchanged.

The legacy `ITeacherIdentityService` name remains for these existing methods to avoid a broad rename/refactor. No admin-specific forgot/reset service, handler, route, email method, token provider, or table is added.

### 7. Preserve authorization trust

Do not add a client-controlled or response-based authorization mechanism. New JWTs include the signed `userId` used today. Existing admin handlers continue calling `AdminCommunityAuthorization`, which reloads current persisted state and requires `IsPlatformAdmin` and non-suspension. Add a regression test that a password-issued token's user ID reaches an existing admin handler successfully and that a token/response account type cannot elevate a persisted non-admin.

### 8. Confirm no migration

Do not generate an EF Core migration. Inspection proves the current model/snapshot already contains normalized username/email and their indexes, password hash, email confirmation, failed count, lockout fields, security/concurrency stamps, `IsPlatformAdmin`, account status, Google identifier, reset cooldown, Identity login tables, and the complete hashed refresh-token table/index set.

Use existing rows only. If implementation produces a model diff, stop and inspect it as an unintended change rather than accepting a migration by default.

### 9. Errors, Swagger, and documentation

Append `PlatformAdminPasswordAlreadyConfigured` to the existing error enum/message without renumbering prior values. Keep invalid credentials generic and forgot-password enumeration-safe.

Add authorization/response annotations to the two new actions so Swagger shows anonymous login, authenticated setup, 401/423 login outcomes, 401/403/409 setup outcomes, safe password validation, and existing BaseResponse envelopes. Verify the Google operation and schemas remain present and unchanged. Keep [api.md](./api.md), [frontend.md](./frontend.md), and the detailed contract synchronized.

## Transaction and Concurrency Boundaries

| Operation | Boundary | Required outcome |
|---|---|---|
| Platform-admin login | Identity lookup/sign-in, then existing token issue calls | No token before admin/password/status/confirmation/lockout success; failed password applies configured lockout. |
| First-password setup | User-row lock through `AddPasswordAsync`, revoke-all, and commit | At most one concurrent setup succeeds; no pre-setup refresh token remains renewable. |
| Refresh rotation | Token lookup, User-row lock, token recheck, conditional revoke, replacement insert, commit | Rotation is single-use and serializes with password setup/reset; eligibility/account type comes from current persisted state. |
| Forgot password | Existing atomic cooldown claim; email outside database operation | Known, unknown, ineligible, cooldown, and delivery failure remain publicly indistinguishable. |
| Reset password | User-row lock through Identity reset, revoke-all, and commit | Password and refresh revocation succeed safely together; Google/admin/community state is untouched. |
| Logout | Existing atomic conditional refresh-token revoke | Submitted token becomes unusable; no account-specific branch. |

## Automated Test Plan

1. Add API contract tests for both new routes, `[AllowAnonymous]` versus `[Authorize]`, request shapes, self-only setup command mapping, BaseResponse types, and continued discovery of the unchanged Google route.
2. Add platform-admin Identity tests for normalized email/username, trimming/casing, ambiguous lookup, generic unknown/wrong/non-admin/passwordless failures, Active/confirmation/lockout rules, failed-attempt lockout, and success reset behavior.
3. Add login-handler tests for PlatformAdmin response fields, existing access/refresh service calls, valid UTC tokens, no community/player/student/teacher repository calls, and zero token calls on failed identity authentication.
4. Extend access-token tests for trusted Teacher and PlatformAdmin account types, standard claims, JTI/UTC lifetime, and absence of community or role claims.
5. Add setup tests for anonymous/non-admin denial, self-only targeting, `AddPasswordAsync` success, password-policy errors, stable already-configured conflict, concurrent winner, refresh revocation, no token issuance, and unchanged `GoogleId`/Identity login rows.
6. Extend recovery tests for admin email/username eligibility without community, confirmed-email policy, identical generic 202 for admin/unknown, valid reset, password policy, revoke-all, and unchanged Google/admin/community state; retain teacher cases unchanged.
7. Extend shared refresh tests for admin eligibility, PlatformAdmin reconstruction, teacher regression, demotion/suspension rejection, rotation/replay, and logout revocation. Add an opt-in real-MySQL race for rotation versus setup/reset using only a dedicated safe test database.
8. Run existing Google compatibility suites unchanged: `B2CLoginCompatibilityTests`, `StudentLicenseActivationOnLoginTests`, and `PendingTeacherActivationTests`. Add a feature regression proving Google login still works after `AddPasswordAsync` without editing those expectations.
9. Run `dotnet build SprintLabs.sln`, focused feature tests, then `dotnet test SprintLabs.sln`. Planning baseline: build succeeds with 33 pre-existing warnings; 247 passed, 0 failed, 0 skipped.

## Delivery Order

1. Add the trusted account-type/result/error contracts and narrow platform-admin service interface.
2. Implement and register platform-admin Identity login/setup with focused tests.
3. Add the login and setup CQRS slices/controller actions and API contract tests.
4. Parameterize access-token creation and update teacher callers/tests explicitly.
5. Generalize shared refresh rotation/handler and add session/concurrency tests.
6. Extend forgot/reset eligibility and password-policy mapping in place.
7. Add Google-preservation and existing-admin-authorization regression coverage.
8. Update Swagger annotations, `api.md`, `frontend.md`, and quickstart verification.
9. Confirm there is no model diff/migration; run build, focused tests, full tests, and opt-in MySQL race tests when configured.

## Complexity Tracking

No constitution violations or unjustified complexity. The single new Identity adapter is required by project layering and feature separation; it does not duplicate JWT, refresh, logout, email, or reset infrastructure.
