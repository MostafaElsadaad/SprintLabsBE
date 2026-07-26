# Implementation Plan: Teacher Email Authentication

**Branch**: `015-teacher-email-authentication` (planned; current worktree has no feature branch) | **Date**: 2026-07-25 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `specs/015-teacher-email-authentication/spec.md`

## Summary

Add a teacher-only email/password identity path alongside the unchanged Google/player login path. The implementation will persist Identity confirmation and lockout state, issue 15-minute teacher JWTs plus hashed single-use 30-day refresh tokens, deliver confirmation/reset/invitation emails through configured SMTP, and replace teacher auto-activation with explicit invitation acceptance. Teacher identity remains independent of community membership; authorization continues to query Active `CommunityUser` rows.

The Application layer will expose one CQRS slice per operation. Infrastructure will encapsulate ASP.NET Core Identity, JWT creation, refresh persistence, SMTP, and the transactional invitation workflow behind Domain service interfaces. Two new Domain entities and one migration add refresh-token and invitation history without changing `CommunityUser.CommunityId`, Player, or StudentLicense.

## Technical Context

**Language/Version**: C# / .NET 8

**Primary Dependencies**: Existing ASP.NET Core Identity and EF stores, `UserManager<User>`, `SignInManager<User>`, Identity data-protection token providers, JWT bearer authentication, MediatR, EF Core 8, Pomelo MySQL, `System.Security.Cryptography`, `System.Net.Mail`, `IBaseRepository<T>`, `BaseResponse`, and `GenericException`

**Storage**: Existing MySQL `Users`, `Communities`, `CommunityUsers`, and `CommunityLicenses`; new `RefreshTokens` and `TeacherInvitations`; one EF Core migration

**Testing**: Existing xUnit 2.9.2, FluentAssertions 6.12.1, Moq 4.20.72, EF Core InMemory/SQLite, optional MySQL fixture, `dotnet build SprintLabs.sln`, and `dotnet test SprintLabs.sln`

**Target Platform**: SprintLabs ASP.NET Core backend API on Linux/Windows-hosted .NET 8

**Project Type**: Versioned backend web API with API/Application/Domain/Infrastructure/Shared projects

**Performance Goals**: Bounded indexed user/token/invitation lookups; login returns active memberships in one scoped query; refresh and acceptance complete with one short database transaction; SMTP is outside database transactions

**Constraints**: No Google SSO redesign, player/admin password login, player/profile/license creation for teachers, global Teacher role, community roles in JWT, nullable `CommunityUser.CommunityId`, raw token persistence, unrelated refactor, unapproved package, or Infrastructure-to-Application reference

**Scale/Scope**: Eight account operations, one updated Owner invitation operation, one invitation-acceptance operation, two tables, six User fields, five focused infrastructure services, one custom refresh repository, documentation, migration, and targeted regression tests

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-checked after Phase 1 design.*

- **Vertical slices over layer-first work**: PASS. Each phase ends in usable account, email, session, recovery, or invitation behavior and includes contracts/tests.
- **Existing architecture wins**: PASS. Controllers use MediatR; Application uses Domain interfaces; Identity/EF/SMTP/JWT implementations remain in Infrastructure; Shared remains dependency-free.
- **SaaS data isolation**: PASS. Invitation issuance requires Active Owner membership in the exact route community. Acceptance can mutate only the invitation's required `CommunityUser` and validates normalized authenticated email.
- **JSON where flexibility matters**: PASS. No question payload or JSON persistence behavior changes.
- **Minimum useful implementation**: PASS. Two specialized persistence paths are added only where concurrency/transaction behavior exceeds `IBaseRepository<T>`; there is no generic auth framework, unit of work, account linking, or permissions redesign.
- **Quality gates**: PASS. The plan requires migration inspection, focused security/concurrency tests, full build, relevant tests, and Mailpit verification.
- **Documentation is executable context**: PASS. Spec, research, data model, contract, API/frontend guidance, and quickstart are colocated under feature 015.

No constitution violation requires an exception.

## Existing Pattern and Impact

### Patterns Preserved

- Routes remain `api/v{version:apiVersion}/[controller]` with API version 1.
- Controllers extract `userId` from the authenticated principal and return `BaseResponse`.
- One command/query, handler, request, and response class per file.
- Controlled failures use `GenericException`, `ErrorMessage`, and existing numeric `ErrorCode`.
- Simple queries continue using `IBaseRepository<T>.AsQueryable()`.
- Active database membership remains the only source of community authorization.

### Existing Behavior Deliberately Replaced

- `InviteTeacherCommandHandler` must no longer set an existing Google-linked invite directly to Active.
- `GoogleAuthenticationCommandHandler` must no longer activate Pending Teacher memberships.
- `CommunityLoginActivationService` retains student-license activation but removes teacher activation.
- Tests and older feature documentation that describe activation-on-login must be updated to point to explicit acceptance.

## Project Structure and Exact File Plan

`[M]` modify, `[N]` create, `[G]` generated migration, `[R]` rename/update.

### Documentation

```text
specs/015-teacher-email-authentication/
|-- spec.md
|-- plan.md
|-- research.md
|-- data-model.md
|-- quickstart.md
|-- api.md
|-- frontend.md
|-- contracts/
|   `-- teacher-authentication-api.md
`-- checklists/
    `-- requirements.md

specs/005-owner-teacher-management/
|-- api.md                                      [M: supersession note]
`-- frontend.md                                 [M: remove auto-activation guidance]
```

### API

```text
API/
|-- Program.cs                                  [M]
|-- appsettings.json                            [M]
|-- appsettings.Development.json                [M]
`-- Controllers/
    |-- AccountController.cs                    [M]
    `-- CommunityInvitationsController.cs       [N]
```

`CommunitiesController.cs` and `UsersController.cs` need no structural change: the existing invite route and current-community route remain valid.

### Application

```text
Application/Features/
|-- Accounts/
|   |-- GoogleAuthenticate/
|   |   `-- GoogleAuthenticationCommandHandler.cs                 [M]
|   `-- TeacherAuthentication/
|       |-- Common/
|       |   `-- TeacherAuthenticationLinkBuilder.cs                [N]
|       |-- RegisterTeacher/
|       |   |-- RegisterTeacherRequest.cs                           [N]
|       |   |-- RegisterTeacherCommand.cs                           [N]
|       |   |-- RegisterTeacherCommandHandler.cs                    [N]
|       |   `-- RegisterTeacherResponse.cs                          [N]
|       |-- ConfirmEmail/
|       |   |-- ConfirmEmailRequest.cs                              [N]
|       |   |-- ConfirmEmailCommand.cs                              [N]
|       |   `-- ConfirmEmailCommandHandler.cs                       [N]
|       |-- ResendConfirmation/
|       |   |-- ResendConfirmationRequest.cs                       [N]
|       |   |-- ResendConfirmationCommand.cs                       [N]
|       |   |-- ResendConfirmationCommandHandler.cs                [N]
|       |   `-- ResendConfirmationResponse.cs                      [N]
|       |-- TeacherLogin/
|       |   |-- TeacherLoginRequest.cs                              [N]
|       |   |-- TeacherLoginCommand.cs                              [N]
|       |   `-- TeacherLoginCommandHandler.cs                       [N]
|       |-- RefreshToken/
|       |   |-- RefreshTokenRequest.cs                              [N]
|       |   |-- RefreshTokenCommand.cs                              [N]
|       |   `-- RefreshTokenCommandHandler.cs                       [N]
|       |-- Logout/
|       |   |-- LogoutRequest.cs                                    [N]
|       |   |-- LogoutCommand.cs                                    [N]
|       |   `-- LogoutCommandHandler.cs                             [N]
|       |-- ForgotPassword/
|       |   |-- ForgotPasswordRequest.cs                            [N]
|       |   |-- ForgotPasswordCommand.cs                            [N]
|       |   `-- ForgotPasswordCommandHandler.cs                     [N]
|       `-- ResetPassword/
|           |-- ResetPasswordRequest.cs                             [N]
|           |-- ResetPasswordCommand.cs                             [N]
|           `-- ResetPasswordCommandHandler.cs                      [N]
|-- Communities/Teachers/
|   |-- InviteTeacher/
|   |   `-- InviteTeacherCommandHandler.cs                          [M]
|   `-- RemoveTeacher/
|       `-- RemoveTeacherCommandHandler.cs                          [M]
`-- CommunityInvitations/AcceptInvitation/
    |-- AcceptInvitationRequest.cs                                  [N]
    |-- AcceptInvitationCommand.cs                                  [N]
    `-- AcceptInvitationCommandHandler.cs                           [N]
```

Feature request classes bind HTTP bodies; command classes additionally carry server-derived `UserId`, `CreatedByIp`, or `RevokedByIp`. Handlers contain orchestration and controlled-error mapping only.

### Domain

```text
Domain/
|-- Models/
|   |-- RefreshToken.cs                              [N]
|   `-- TeacherInvitation.cs                         [N]
|-- Repositories/
|   `-- IRefreshTokenRepository.cs                   [N]
`-- Services/
    |-- IAccessTokenService.cs                       [N]
    |-- IEmailService.cs                             [N]
    |-- IRefreshTokenService.cs                      [N]
    |-- ITeacherIdentityService.cs                   [N]
    |-- ITeacherInvitationService.cs                 [N]
    `-- ICommunityLoginActivationService.cs          [M]
```

The two new entities are separate files. No new repository is added for simple User, Community, membership, or invitation reads; the invitation service owns its special multi-entity transaction, and `IRefreshTokenRepository` exists only for atomic/reused lifecycle operations.

### Infrastructure

```text
Infrastructure/
|-- DataAccess/
|   |-- User.cs                                      [M]
|   `-- ApplicationDbContext.cs                      [M]
|-- Identity/
|   |-- EmailConfirmationTokenProviderOptions.cs     [N]
|   |-- EmailConfirmationTokenProvider.cs            [N]
|   |-- PasswordResetTokenProviderOptions.cs         [N]
|   `-- PasswordResetTokenProvider.cs                [N]
|-- Repositories/
|   `-- RefreshTokenRepository.cs                    [N]
|-- Services/
|   |-- Common/
|   |   `-- SecureTokenGenerator.cs                  [N, internal]
|   |-- AccessTokenService.cs                        [N]
|   |-- SmtpEmailService.cs                          [N]
|   |-- RefreshTokenService.cs                       [N]
|   |-- TeacherIdentityService.cs                    [N]
|   |-- TeacherInvitationService.cs                 [N]
|   |-- UserService.cs                               [M]
|   `-- CommunityLoginActivationService.cs           [M]
|-- ServiceConfig.cs                                 [M]
`-- Migrations/
    |-- <timestamp>_TeacherEmailAuthentication.cs             [G]
    |-- <timestamp>_TeacherEmailAuthentication.Designer.cs    [G]
    `-- ApplicationDbContextModelSnapshot.cs                  [M/G]
```

The timestamped migration prefix is assigned by `dotnet ef`; the class/suffix is exactly `TeacherEmailAuthentication`.

### Shared

```text
Shared/
|-- Enums/
|   `-- ErrorMessage.cs                              [M]
|-- Options/
|   |-- JWTOptions.cs                                [M]
|   |-- EmailOptions.cs                              [N]
|   |-- FrontendOptions.cs                           [N]
|   `-- TeacherAuthenticationOptions.cs              [N]
`-- Responses/
    |-- UserIdentityResponse.cs                      [M]
    |-- AccessTokenResult.cs                         [N]
    |-- RefreshTokenResult.cs                        [N]
    |-- TeacherIdentityResult.cs                     [N]
    |-- TeacherRegistrationResult.cs                 [N]
    |-- ConfirmationDispatchResult.cs                [N]
    |-- PasswordResetDispatchResult.cs               [N]
    |-- TeacherTokenResponse.cs                      [N]
    |-- TeacherCommunityResponse.cs                  [N]
    |-- TeacherLoginResponse.cs                      [N]
    |-- TeacherInvitationIssueResult.cs              [N]
    `-- TeacherInvitationAcceptanceResponse.cs       [N]
```

`Shared/Responses/LoginResponse.cs` remains unchanged for Google/player compatibility.

### Tests

```text
SprintLabs.Tests/Features/
|-- TeacherEmailAuthentication/
|   |-- TeacherAuthenticationTestHelper.cs                    [N]
|   |-- TeacherIdentityServiceTests.cs                         [N]
|   |-- AccessTokenServiceTests.cs                             [N]
|   |-- RefreshTokenServiceTests.cs                            [N]
|   |-- RegisterConfirmResendHandlerTests.cs                   [N]
|   |-- TeacherLoginCommandHandlerTests.cs                     [N]
|   |-- PasswordRecoveryHandlerTests.cs                        [N]
|   |-- TeacherInvitationServiceTests.cs                       [N]
|   |-- AcceptInvitationCommandHandlerTests.cs                 [N]
|   |-- TeacherAuthenticationControllerContractTests.cs        [N]
|   `-- TeacherAuthenticationPersistenceTests.cs               [N]
|-- OwnerTeacherManagement/
|   |-- InviteTeacherCommandHandlerTests.cs                    [M]
|   `-- ExplicitTeacherInvitationCompatibilityTests.cs         [R from PendingTeacherActivationTests.cs]
|-- StudentLicenseActivation/
|   `-- StudentLicenseActivationOnLoginTests.cs                [M]
`-- B2CPlayerProfileSupport/
    `-- B2CLoginCompatibilityTests.cs                          [M only if constructor/setup changes]
```

No new test project is created.

**Structure Decision**: Keep feature use cases in Application vertical slices; isolate Identity-dependent and transaction-dependent work in narrow Infrastructure services behind Domain interfaces; use Shared only for option/result contracts that cross layers. This preserves the existing project references and avoids a feature-specific repository for simple CRUD.

## New Database Columns and Tables

### Users

- `IsTeacherAccount tinyint(1) NOT NULL DEFAULT 0`
- `LastConfirmationEmailSentAt datetime(6) NULL`
- `EmailConfirmed tinyint(1) NOT NULL DEFAULT 0`
- `LockoutEnd datetime(6) NULL` (Identity `DateTimeOffset?` mapping verified in generated migration)
- `AccessFailedCount int NOT NULL DEFAULT 0`
- `LockoutEnabled tinyint(1) NOT NULL DEFAULT 1`
- Make `EmailIndex` on `NormalizedEmail` unique after duplicate precheck.
- Backfill `IsTeacherAccount=1` for users with existing Teacher memberships.

### RefreshTokens

`Id`, `UserId`, `TokenHash`, `ExpiresAt`, `CreatedAt`, nullable `RevokedAt`, nullable `ReplacedByTokenHash`, nullable `CreatedByIp`, nullable `RevokedByIp`; unique `TokenHash`, index `UserId`, and active-session lookup index `(UserId, RevokedAt, ExpiresAt)`.

### TeacherInvitations

`Id`, `CommunityUserId`, `InvitedEmail`, `TokenHash`, `ExpiresAt`, nullable `AcceptedAt`, nullable `RevokedAt`, `CreatedAt`, `CreatedByUserId`, nullable `LastSentAt`; unique `TokenHash`, index `CommunityUserId`, active-invitation lookup index, and inviter index.

Full types, relationships, deletion behavior, backfill, and state transitions are in [data-model.md](./data-model.md).

## Options and Dependency Registration

### Identity

Configure in the existing `AddIdentityCore<User>()` registration:

- unique email required;
- confirmed email required for password sign-in;
- minimum password length 8;
- uppercase, lowercase, digit, and non-alphanumeric required;
- lockout allowed for new users;
- five failed attempts;
- 15-minute lockout;
- `AddSignInManager()`;
- `AddDefaultTokenProviders()`;
- custom named email-confirmation and password-reset providers.

### Options

- `JWTOptions`: existing issuer/audience/secret plus `AccessTokenLifetimeMinutes=15`, `RefreshTokenLifetimeDays=30`, and `ClockSkewSeconds`.
- `TeacherAuthenticationOptions`: confirmation lifetime 24 hours, password-reset lifetime 60 minutes, confirmation cooldown 60 seconds, invitation lifetime 7 days.
- `EmailOptions`: host, port, SSL, sender email/name, optional username/password, timeout.
- `FrontendOptions`: client base URL.

Use options validation on startup. Sensitive values come from user secrets/environment variables. Remove the startup console output of JWT settings and remove the tracked JWT secret value.

### Dependencies

- Register all five Domain services and `IRefreshTokenRepository` in `Infrastructure/ServiceConfig.cs`.
- Keep the existing scoped lifetime so Identity, repositories, and DbContext share one request scope/transaction.
- Add no project reference and no NuGet package.

## New Project and Package Dependencies

- **New project references**: none. The existing API -> Application/Infrastructure, Application -> Domain, Infrastructure -> Domain, Domain -> Shared graph is sufficient.
- **New NuGet packages**: none. Identity EF stores, JWT bearer, EF Core/MySQL, SQLite, and the test libraries are already referenced.
- **Framework APIs reused**: `System.Security.Cryptography` for secure random values and SHA-256; `System.Net.Mail` for the requested SMTP phase.
- **Deferred dependency**: MailKit is not planned without explicit package approval. If production SMTP later requires modern authentication unsupported by `System.Net.Mail`, evaluate it as a separate change.

## API Contracts

The canonical contract is [contracts/teacher-authentication-api.md](./contracts/teacher-authentication-api.md). Planned versioned routes:

1. `POST /api/v1/Account/teachers/register`
2. `POST /api/v1/Account/confirm-email`
3. `POST /api/v1/Account/resend-confirmation`
4. `POST /api/v1/Account/teachers/login`
5. `POST /api/v1/Account/refresh-token`
6. `POST /api/v1/Account/logout`
7. `POST /api/v1/Account/forgot-password`
8. `POST /api/v1/Account/reset-password`
9. Existing `POST /api/v1/Communities/{communityId}/teachers/invite`
10. `POST /api/v1/CommunityInvitations/accept`
11. Existing `GET /api/v1/Users/me/communities`

New public Account actions are explicitly `[AllowAnonymous]`. Invitation acceptance is `[Authorize]`. Refresh/logout authenticate the opaque refresh token rather than requiring a still-valid access token.

## Implementation Phases

### Phase 1: Identity and Security Foundation

1. Add User teacher/cooldown fields and stop ignoring confirmation/lockout Identity fields.
2. Add `RefreshToken` entity, DbSet/configuration, custom repository, service contract/implementation, and secure token helper.
3. Configure Identity options, SignInManager, default/custom token providers, and distinct lifetimes.
4. Extend `JWTOptions`, add access-token service, and harden bearer validation.
5. Remove JWT secret console output and move secret material out of tracked settings.
6. Add migration and inspect generated SQL/model snapshot before any API slice depends on it.
7. Add focused foundation tests for options, JWT claims/validation, hash-only persistence, and atomic refresh rotation.

**Exit gate**: migration compiles; wrong issuer/audience/signature/expiry fail; one refresh token can rotate once; Google `LoginResponse` remains unchanged.

### Phase 2: Email Infrastructure

1. Add Shared email/frontend/authentication options and bind/validate them.
2. Add `IEmailService` and configured `SmtpEmailService`.
3. Implement confirmation, reset, and invitation HTML/text templates with encoded client links.
4. Add Mailpit defaults only to development settings; credentials remain empty/external.
5. Map SMTP failures to a controlled service-unavailable error and log only event type/recipient domain/correlation context—never credentials or token-bearing links.
6. Test link construction and email-message composition with a fake email service; manually verify actual SMTP through Mailpit.

**Exit gate**: all three messages reach Mailpit with valid routes; logs/config contain no secrets.

### Phase 3: Teacher Registration and Confirmation

1. Implement `ITeacherIdentityService` registration transaction:
   - normalize email;
   - create new teacher or reuse eligible passwordless placeholder;
   - reject non-teacher Google/player identity conflict;
   - set name, teacher marker, password, `EmailConfirmed=false`, and confirmation-send timestamp;
   - create no Player, StudentLicense, or CommunityUser.
2. Add RegisterTeacher CQRS slice/controller action and send confirmation after commit.
3. Add ConfirmEmail slice with Base64URL decoding and idempotent already-confirmed behavior.
4. Add ResendConfirmation slice with an atomic persisted cooldown claim and generic unknown/confirmed response.
5. Add registration/confirmation/resend API and frontend documentation examples.

**Exit gate**: standalone and invited-placeholder registration work; unconfirmed accounts cannot password-login; cooldown survives service restart; no membership/profile/license side effects.

### Phase 4: Teacher Login and Tokens

1. Implement password sign-in through `SignInManager.CheckPasswordSignInAsync(..., lockoutOnFailure:true)`.
2. Enforce teacher marker, password presence, confirmation, active status, and lockout without exposing account existence.
3. Query all Active memberships with community names; return `[]` when none.
4. Issue teacher access token and persist one raw-to-client/hash-to-database refresh token.
5. Add refresh handler using atomic rotation and user eligibility revalidation.
6. Add idempotent logout/revocation using the request network address when available.
7. Keep Google handler and `LoginResponse` player fields unchanged except removal of teacher auto-activation in Phase 6.

**Exit gate**: confirmed teacher can log in with zero/multiple communities; lockout works; refresh rotates once; logout revokes; JWT contains no community roles.

### Phase 5: Password Recovery

1. Add generic forgot-password handler; only eligible teachers receive a URL-safe one-hour reset link.
2. Add reset handler and enforce the same password policy through UserManager.
3. Wrap reset, security-stamp update if required by Identity result, and active refresh-token revocation in one database transaction.
4. Return controlled generic failures for invalid/expired tokens without leaking Identity internals.

**Exit gate**: forgot response is indistinguishable; new password works; old password and all old refresh tokens fail.

### Phase 6: Invitation Acceptance

1. Add `TeacherInvitation` entity/configuration and `ITeacherInvitationService`.
2. Update existing invite handler:
   - retain Active Owner check for route community;
   - find/create eligible placeholder and set teacher marker;
   - keep every new/restored password-teacher membership Pending;
   - reserve a seat only for new/restored counted membership;
   - reissue by revoking prior active invitation without seat change;
   - send email only after the identity/membership/license/invitation transaction commits.
3. Remove teacher auto-activation from Google handler and `ICommunityLoginActivationService`; retain student activation.
4. Add acceptance CQRS/controller:
   - hash token and find invitation;
   - validate expiry/revocation, user status, teacher marker, email confirmation, normalized email match, Teacher role, and Pending/Active state;
   - atomically set membership Active and invitation AcceptedAt;
   - return existing Active membership idempotently;
   - never change `UsedTeachers`.
5. Update remove handler to revoke active invitations in the same membership/seat transaction.
6. Support independent invitations/memberships across multiple communities.
7. Update feature 005 docs/tests that describe auto-activation.

**Exit gate**: login alone never activates a teacher; matching acceptance does; wrong/expired/revoked links do not; seats and membership uniqueness remain correct under repeat/concurrent operations.

### Phase 7: Testing and Verification

1. Unit-test secure token generation/hashing, JWT claims/expiry, link encoding, option validation, and email composition.
2. Service-test Identity registration/reuse/conflict, confirmation, cooldown, sign-in/lockout, reset, and session revocation using a real Identity service collection over an isolated provider.
3. Handler-test every CQRS boundary, BaseResponse status, generic forgot/resend behavior, empty/multiple community responses, and suspended users.
4. Persistence-test refresh concurrency, invite/reissue/accept/remove transactions, unique hashes, seat capacity, idempotency, and multiple communities.
5. Regression-test Google login/player creation, student-license activation, Active community authorization, and `/Users/me/communities`.
6. Inspect migration and model snapshot.
7. Run `dotnet build SprintLabs.sln`, focused tests, then `dotnet test SprintLabs.sln`.
8. Execute [quickstart.md](./quickstart.md) with Mailpit and record manual results.

**Exit gate**: build and relevant/full tests pass; migration applies cleanly; Mailpit receives all three templates; no raw token or JWT secret appears in persistence/logs.

## Transaction Boundaries

| Operation | Atomic database work | External work |
|---|---|---|
| Register | Identity create/add-password/name/marker/cooldown timestamp | Confirmation email after commit |
| Resend confirmation | Conditional cooldown timestamp update | Confirmation email after successful claim |
| Login | Identity failed-count/lockout update; refresh insert before response | None |
| Refresh | Conditional old revoke + replacement insert | None |
| Logout | Conditional revoke | None |
| Reset password | Reset/security stamp + revoke-all refresh tokens | None |
| Invite/reissue | User eligibility/marker + membership + seat + old invitation revoke + new invitation | Invitation email after commit |
| Accept | Validate invitation/membership + activate + AcceptedAt | None |
| Remove | Membership Removed + one seat decrement + invitations revoked | None |

No SMTP call runs inside a transaction. Conditional updates or row locks protect refresh reuse and seat capacity. All timestamps use UTC.

## Security Risks and Mitigations

| Risk | Mitigation / test |
|---|---|
| Existing JWT secret exposure | Remove console writes and tracked secret; validate options on startup; inspect logs |
| Forged/wrong-context JWT | Validate issuer, audience, signing key, and lifetime; test each failure |
| Account enumeration | Generic login/forgot/resend behavior where required; no Identity error detail |
| Password brute force | Identity lockout at five failures for 15 minutes; persisted fields |
| Refresh-token theft/reuse | Secure random raw token, SHA-256-only storage, 30-day expiry, single-use rotation, logout/reset revocation |
| Concurrent refresh | Conditional revoke must affect one row before replacement insert |
| Token leakage through logs | Never log request bodies, raw tokens, password, JWT secret, or email links |
| Invitation theft | Authenticated confirmed teacher required; normalized email must match; expiry/revocation/single-use checks |
| Seat over-allocation | Transactional license capacity check and one unique membership per community/user |
| Stale invitation reactivation | Reissue/removal revokes active invitation; acceptance validates current Pending Teacher membership |
| Community privilege staleness | No community roles in JWT; every community action continues database authorization |
| SMTP ambiguity | Commit state before send; controlled failure; safe resend/reissue recovery |
| Identity-token lifetime coupling | Separate custom providers and tests for 24-hour confirmation vs one-hour reset |

## Backward Compatibility Risks

| Risk | Expected change / mitigation |
|---|---|
| Pending teachers previously activated on Google login | Intentional replacement; update handler, service interface, tests, and old docs |
| Existing Active teachers | Remain Active; no retroactive acceptance |
| Existing pending teacher rows lack invitation token | Owner resend creates first persisted invitation without incrementing seat usage |
| Existing teacher marker absent | Migration backfills from Teacher memberships; new placeholders set marker |
| Existing Google/player API consumers | `LoginResponse`, Google endpoint, player profile, and student-license activation remain unchanged |
| Existing JWTs under hardened validation | Tokens already issued with current configured issuer/audience/signature continue validating if configuration is unchanged; deployment must coordinate signing settings |
| Access-token lifetime | New 15-minute lifetime applies to teacher tokens; existing Google token issuance is not redesigned |
| Unique normalized email migration | Pre-deployment duplicate query is mandatory; abort migration and resolve duplicates rather than deleting/merging automatically |
| API envelope vs feature examples | Contracts use existing `BaseResponse`; frontend docs show the envelope |
| SMTP unavailable | Registration/pending invitation persists and resend/reissue provides recovery |

## Test Strategy

### Unit / Provider-Independent

- Access token: issuer, audience, UTC expiration, `jti`, `sub`, `userId`, no community role.
- Secure tokens: entropy length, Base64URL shape, deterministic SHA-256, raw value absent from entity.
- Identity: password policy, placeholder reuse, player conflict, confirmation, cooldown, lockout, reset.
- Handlers: public/generic responses, suspended/unconfirmed/locked states, empty community list, IP propagation.
- Email: correct recipient/template/link; no secret logged on failure.

### Persistence / Concurrency

- Refresh unique hash, expired/revoked rejection, one-winner concurrent rotation, logout idempotency, revoke-all.
- Invitation unique hash, reissue revocation, wrong-account rejection, one-winner acceptance.
- New/restore seat increment once; resend/accept zero; remove decrement once.
- Transaction rollback leaves no partial membership/license/invitation mutation.
- Required `CommunityId` and unique `(CommunityId, UserId)` remain enforced.

### Regression

- Existing Google player login returns current `LoginResponse`.
- Player profile creation/reuse still works.
- Pending student licenses still activate on Google login.
- Pending teachers do not activate on login.
- Existing active community access and `/Users/me/communities` behavior remain.
- Owner invitation/list/removal authorization remains Active Owner-only.

### Commands

```powershell
dotnet build SprintLabs.sln
dotnet test SprintLabs.Tests/Compass.Tests.csproj --filter "FullyQualifiedName~TeacherEmailAuthentication|FullyQualifiedName~OwnerTeacherManagement|FullyQualifiedName~B2CPlayerProfileSupport|FullyQualifiedName~StudentLicenseActivation"
dotnet test SprintLabs.sln
```

## Phase 0 and Phase 1 Artifacts

- [research.md](./research.md)
- [data-model.md](./data-model.md)
- [contracts/teacher-authentication-api.md](./contracts/teacher-authentication-api.md)
- [quickstart.md](./quickstart.md)
- [api.md](./api.md)
- [frontend.md](./frontend.md)

## Post-Design Constitution Check

- **Vertical slices over layer-first work**: PASS. Seven phases each have an independently verifiable exit gate and end-to-end contracts.
- **Existing architecture wins**: PASS. Exact files follow current controller, MediatR, service, repository, Identity, EF, error, and test placement.
- **SaaS data isolation**: PASS. Route community authorization, required membership ownership, normalized email binding, and database authorization are explicit.
- **JSON where flexibility matters**: PASS. Not applicable.
- **Minimum useful implementation**: PASS. No player/admin password support, account linking, global roles, custom unit of work, production email-domain work, or unrelated refactor.
- **Quality gates**: PASS. Migration, security, concurrency, build, automated tests, regression tests, and Mailpit checks are all required.
- **Documentation is executable context**: PASS. HTTP/frontend/runbook artifacts are complete and linked.

## Complexity Tracking

No constitution violations identified. The custom refresh repository and invitation service are justified by atomic concurrency and cross-Identity transaction requirements; simple CRUD remains on existing patterns.
