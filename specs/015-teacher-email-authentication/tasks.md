# Tasks: Teacher Email Authentication

**Input**: Design documents from `specs/015-teacher-email-authentication/`

**Prerequisites**: `plan.md`, `spec.md`, `research.md`, `data-model.md`, `contracts/teacher-authentication-api.md`, `quickstart.md`

**Tests**: Automated tests are required by the feature specification. Write the phase's tests first and confirm they fail for the expected missing behavior before implementing that phase.

**Organization**: Tasks use only the eight requested phases. Story labels preserve traceability to the specification:

- **US1**: Register and confirm a standalone teacher account
- **US2**: Sign in and maintain a teacher session
- **US3**: Recover a forgotten password
- **US4**: Invite a teacher without duplicating seats
- **US5**: Explicitly accept a community invitation
- **US6**: Resend email confirmation safely

## Format: `[ID] [P?] [Story?] Description`

- **[P]**: May run in parallel after its phase prerequisites because it uses different files and does not depend on another incomplete task.
- **[Story]**: Required for tasks implementing a specification user story; omitted for inspection, shared foundation, and final verification.
- Every task names the concrete file or path it reads or changes.

## Phase 1: Inspect current implementation

**Purpose**: Reconfirm the current worktree before implementation. This phase is read-only and must finish before code changes.

- [ ] T001 Read `AGENTS.md` and `specs/015-teacher-email-authentication/plan.md` completely; record any changed repository constraints before continuing
- [ ] T002 [P] Inspect Identity registration, roles, EF stores, SignInManager availability, and token-provider setup in `API/Program.cs` and `Infrastructure/ServiceConfig.cs`
- [ ] T003 [P] Inspect the Identity user and current ignored/persisted fields in `Infrastructure/DataAccess/User.cs` and `Infrastructure/DataAccess/ApplicationDbContext.cs`
- [ ] T004 [P] Inspect JWT generation, claims, lifetime, validation flags, and secret handling in `Infrastructure/Services/UserService.cs`, `Infrastructure/ServiceConfig.cs`, `Shared/Options/JWTOptions.cs`, `API/Program.cs`, and `API/appsettings*.json`
- [ ] T005 [P] Inspect Google/player login, current response compatibility, and login activation calls in `Application/Features/Accounts/GoogleAuthenticate/GoogleAuthenticationCommandHandler.cs`, `Shared/Responses/LoginResponse.cs`, `Domain/Services/ICommunityLoginActivationService.cs`, and `Infrastructure/Services/CommunityLoginActivationService.cs`
- [ ] T006 [P] Inspect the existing Owner invite route, request, command, handler, user lookup, and response in `API/Controllers/CommunitiesController.cs`, `Application/Features/Communities/Teachers/InviteTeacher/`, `Domain/Services/IUserService.cs`, and `Infrastructure/Services/UserService.cs`
- [ ] T007 [P] Inspect required membership ownership, teacher capacity, duplicate prevention, removal, and active-community filtering in `Domain/Models/Community.cs`, `Infrastructure/DataAccess/ApplicationDbContext.cs`, `Application/Features/Communities/Teachers/RemoveTeacher/RemoveTeacherCommandHandler.cs`, and `Application/Features/Users/GetCurrentUserCommunities/`
- [ ] T008 [P] Inspect dependency direction, service placement, generic repository capabilities, and existing package references in `API/API.csproj`, `Application/Application.csproj`, `Domain/Domain.csproj`, `Infrastructure/Infrastructure.csproj`, `Shared/Shared.csproj`, `Domain/Repositories/IBaseRepository.cs`, and `Infrastructure/Repositories/BaseRepository.cs`
- [ ] T009 [P] Inspect migration conventions and existing authentication/community tests in `Infrastructure/Migrations/`, `SprintLabs.Tests/Compass.Tests.csproj`, `SprintLabs.Tests/Features/OwnerTeacherManagement/`, `SprintLabs.Tests/Features/B2CPlayerProfileSupport/`, `SprintLabs.Tests/Features/StudentLicenseActivation/`, and `SprintLabs.Tests/Fixtures/MysqlDatabaseFixture.cs`

**Checkpoint**: Current assumptions in the plan still match the worktree; any mismatch is resolved in the smallest affected task before editing.

## Phase 2: Authentication foundation

**Purpose**: Establish blocking Identity, JWT, refresh-token, options, repository, and service infrastructure.

**Critical**: Phase 2 blocks every teacher authentication and invitation story.

- [ ] T010 [P] Add `IsTeacherAccount` and `LastConfirmationEmailSentAt` to the Identity user without changing player/admin behavior in `Infrastructure/DataAccess/User.cs`
- [ ] T011 Stop ignoring and configure `EmailConfirmed`, `LockoutEnd`, `AccessFailedCount`, and `LockoutEnabled`, including safe defaults, in `Infrastructure/DataAccess/ApplicationDbContext.cs` (depends on T010)
- [ ] T012 [P] Add configurable confirmation, reset, resend-cooldown, invitation, lockout, and password settings in `Shared/Options/TeacherAuthenticationOptions.cs`
- [ ] T013 [P] Extend `Shared/Options/JWTOptions.cs` with teacher access/refresh lifetimes and clock skew, and add cross-layer token contracts in `Shared/Responses/AccessTokenResult.cs`, `Shared/Responses/RefreshTokenResult.cs`, and `Shared/Responses/TeacherTokenResponse.cs`
- [ ] T014 Add distinct confirmation and password-reset data-protection provider/options classes in `Infrastructure/Identity/EmailConfirmationTokenProviderOptions.cs`, `Infrastructure/Identity/EmailConfirmationTokenProvider.cs`, `Infrastructure/Identity/PasswordResetTokenProviderOptions.cs`, and `Infrastructure/Identity/PasswordResetTokenProvider.cs` (depends on T012)
- [ ] T015 Configure unique email, confirmed-email password sign-in, minimum-eight complex passwords, five-attempt/15-minute lockout, `AddSignInManager()`, default providers, and the custom token providers in `API/Program.cs` (depends on T011 and T014)
- [ ] T016 Enable issuer, audience, signing-key, and lifetime validation in `Infrastructure/ServiceConfig.cs`; remove JWT-secret console output from `API/Program.cs`; keep secret material out of `API/appsettings.json`
- [ ] T017 [P] Create the hash-only refresh-token domain entity with expiry, revocation, rotation lineage, and IP metadata in `Domain/Models/RefreshToken.cs`
- [ ] T018 Add `DbSet<RefreshToken>`, required fields, fixed-length unique hash, user FK, and active-session indexes in `Infrastructure/DataAccess/ApplicationDbContext.cs` (depends on T017)
- [ ] T019 [P] Define atomic find, create, rotate, revoke, and revoke-all operations in `Domain/Repositories/IRefreshTokenRepository.cs`
- [ ] T020 Implement conditional single-winner rotation and idempotent revocation using the scoped EF context in `Infrastructure/Repositories/RefreshTokenRepository.cs` (depends on T017, T018, and T019)
- [ ] T021 [P] Define teacher access-token and refresh-token lifecycle contracts in `Domain/Services/IAccessTokenService.cs` and `Domain/Services/IRefreshTokenService.cs`
- [ ] T022 [P] Implement cryptographically secure Base64URL token generation and SHA-256 hashing in `Infrastructure/Services/Common/SecureTokenGenerator.cs`
- [ ] T023 Implement UTC teacher JWT creation with `jti`, `sub`, `userId`, email, name, and account type but no community roles in `Infrastructure/Services/AccessTokenService.cs`; implement hash-only issue/rotation/logout/revoke-all behavior in `Infrastructure/Services/RefreshTokenService.cs` (depends on T013 and T019–T022)
- [ ] T024 Bind and validate authentication/JWT options and register `IRefreshTokenRepository`, `IAccessTokenService`, and `IRefreshTokenService` with scoped lifetimes in `Infrastructure/ServiceConfig.cs` (depends on T012–T023)

**Checkpoint**: Identity policy and secure bearer validation are configured; refresh persistence can issue, rotate once, revoke, and revoke all without storing raw values.

## Phase 3: Email infrastructure

**Purpose**: Provide configured SMTP delivery, client links, templates, and safe development testing for US1, US3, US4, and US6.

- [ ] T025 [P] Add SMTP host, port, SSL, sender, optional credentials, and timeout settings in `Shared/Options/EmailOptions.cs`
- [ ] T026 [P] Add the configurable client base URL in `Shared/Options/FrontendOptions.cs`
- [ ] T027 [P] Define confirmation, password-reset, and community-invitation delivery methods in `Domain/Services/IEmailService.cs`
- [ ] T028 [P] Add Base64URL Identity-token encode/decode helpers in `Shared/Helpers/UrlSafeTokenHelper.cs` and confirmation/reset/invitation URL construction in `Application/Features/Accounts/TeacherAuthentication/Common/TeacherAuthenticationLinkBuilder.cs`
- [ ] T029 [P] Add failing SMTP composition/error-safety tests for all three message types in `SprintLabs.Tests/Features/TeacherEmailAuthentication/SmtpEmailServiceTests.cs`
- [ ] T030 Implement configured cancellation-aware SMTP transport, controlled failures, and secret-free logging in `Infrastructure/Services/SmtpEmailService.cs` (depends on T025 and T027)
- [ ] T031 Add the teacher confirmation subject/body and `{FrontendBaseUrl}/confirm-email` link rendering in `Infrastructure/Services/SmtpEmailService.cs` (depends on T028 and T030)
- [ ] T032 Add the password-reset subject/body and `{FrontendBaseUrl}/reset-password` link rendering in `Infrastructure/Services/SmtpEmailService.cs` (depends on T028 and T030)
- [ ] T033 Add the community-invitation subject/body and `{FrontendBaseUrl}/invitations/accept` link rendering in `Infrastructure/Services/SmtpEmailService.cs` (depends on T028 and T030)
- [ ] T034 Add non-secret Mailpit-compatible host `localhost`, port `1025`, SSL `false`, sender, and local frontend URL values in `API/appsettings.Development.json`; add non-secret option shapes only in `API/appsettings.json`
- [ ] T035 Bind/validate `EmailOptions` and `FrontendOptions` and register `IEmailService` in `Infrastructure/ServiceConfig.cs` (depends on T025–T034)

**Checkpoint**: Tests cover message/link composition, and configured Mailpit/Mailtrap delivery is available without hardcoded credentials.

## Phase 4: Teacher registration and confirmation

**Purpose**: Deliver US1 registration/confirmation and US6 safe confirmation resend without community or player side effects.

**Independent test**: Register a new teacher and an eligible invited placeholder, confirm the email, and verify that no Player, StudentLicense, or CommunityUser is created/activated; resend is rate-limited across service restarts.

- [ ] T036 [P] [US1] Add failing Identity-service tests for new registration, passwordless invited-user reuse, Google/player conflict, confirmation, repeated confirmation, and absence of Player/StudentLicense/CommunityUser side effects in `SprintLabs.Tests/Features/TeacherEmailAuthentication/TeacherIdentityServiceTests.cs`
- [ ] T037 [P] [US6] Add failing cooldown, generic-response, already-confirmed, unknown-email, restart-persistence, and concurrent-resend tests in `SprintLabs.Tests/Features/TeacherEmailAuthentication/ConfirmationResendTests.cs`
- [ ] T038 [P] [US1] Add failing CQRS and controller contract tests for `202` register, `200` confirm, safe errors, and `BaseResponse` envelopes in `SprintLabs.Tests/Features/TeacherEmailAuthentication/RegisterConfirmHandlerTests.cs` and `SprintLabs.Tests/Features/TeacherEmailAuthentication/TeacherAuthenticationControllerContractTests.cs`
- [ ] T039 [P] [US1] Create register and confirmation API models in `Application/Features/Accounts/TeacherAuthentication/RegisterTeacher/RegisterTeacherRequest.cs`, `RegisterTeacherCommand.cs`, `RegisterTeacherResponse.cs`, `Application/Features/Accounts/TeacherAuthentication/ConfirmEmail/ConfirmEmailRequest.cs`, and `ConfirmEmailCommand.cs`
- [ ] T040 [P] [US1] Add boundary validation for trimmed name, normalized valid email, positive user ID, required token, and password shape without adding a package in `Application/Features/Accounts/TeacherAuthentication/Common/TeacherAuthenticationValidation.cs`
- [ ] T041 [P] [US1] Define teacher registration/confirmation identity operations and cross-layer results in `Domain/Services/ITeacherIdentityService.cs`, `Shared/Responses/TeacherIdentityResult.cs`, and `Shared/Responses/TeacherRegistrationResult.cs`
- [ ] T042 [US1] Implement the registration transaction with `UserManager<User>` in `Infrastructure/Services/TeacherIdentityService.cs`: create or reuse only an eligible passwordless invited User, update name, set `IsTeacherAccount`, add password, force unconfirmed state, persist resend timestamp, generate URL-safe confirmation token, and reject unsupported Google/player linking
- [ ] T043 [US1] Implement `RegisterTeacherCommandHandler` to validate, call the identity service, send confirmation after commit, and return resend time without creating Player/StudentLicense/CommunityUser in `Application/Features/Accounts/TeacherAuthentication/RegisterTeacher/RegisterTeacherCommandHandler.cs`
- [ ] T044 [US1] Implement idempotent URL-safe token decoding and `UserManager.ConfirmEmailAsync` handling in `Infrastructure/Services/TeacherIdentityService.cs` and `Application/Features/Accounts/TeacherAuthentication/ConfirmEmail/ConfirmEmailCommandHandler.cs`
- [ ] T045 [US6] Create resend request/response/command files and implement atomic persisted cooldown plus generic unknown/confirmed responses in `Application/Features/Accounts/TeacherAuthentication/ResendConfirmation/ResendConfirmationRequest.cs`, `ResendConfirmationCommand.cs`, `ResendConfirmationResponse.cs`, `ResendConfirmationCommandHandler.cs`, and `Infrastructure/Services/TeacherIdentityService.cs`
- [ ] T046 [US1] Add public versioned register and confirm-email actions with thin MediatR mapping and `BaseResponse` status codes in `API/Controllers/AccountController.cs`
- [ ] T047 [US6] Add the public versioned resend-confirmation action and generic `202` envelope in `API/Controllers/AccountController.cs`

**Checkpoint**: US1 and US6 tests pass independently; registration never creates membership/player/license data and confirmation never accepts an invitation.

## Phase 5: Login, refresh and logout

**Purpose**: Deliver US2 teacher password login, active-community response, refresh rotation, and logout.

**Independent test**: A confirmed active teacher with zero communities logs in with `communities: []`, a multi-community teacher receives only active memberships, one refresh rotates successfully, reuse fails, and logout revokes the replacement.

- [ ] T048 [P] [US2] Add failing JWT tests for UTC expiry, issuer, audience, signing key, `jti`, required teacher claims, and absence of community roles in `SprintLabs.Tests/Features/TeacherEmailAuthentication/AccessTokenServiceTests.cs`
- [ ] T049 [P] [US2] Add failing refresh tests for hash-only storage, expiry/revocation rejection, one-winner concurrent rotation, suspended-user rejection, idempotent logout, and revoke-all in `SprintLabs.Tests/Features/TeacherEmailAuthentication/RefreshTokenServiceTests.cs`
- [ ] T050 [P] [US2] Add failing login/refresh/logout handler and controller tests for teacher marker, confirmation, password, lockout, suspension, active/empty communities, response envelopes, and IP propagation in `SprintLabs.Tests/Features/TeacherEmailAuthentication/TeacherLoginCommandHandlerTests.cs` and `SprintLabs.Tests/Features/TeacherEmailAuthentication/TeacherAuthenticationControllerContractTests.cs`
- [ ] T051 [P] [US2] Create teacher login, community, and token response contracts in `Shared/Responses/TeacherLoginResponse.cs`, `Shared/Responses/TeacherCommunityResponse.cs`, and `Shared/Responses/TeacherTokenResponse.cs`; create request/command files in `Application/Features/Accounts/TeacherAuthentication/TeacherLogin/TeacherLoginRequest.cs` and `TeacherLoginCommand.cs`
- [ ] T052 [US2] Extend `ITeacherIdentityService` and `TeacherIdentityService` to normalize email, require `IsTeacherAccount`, require a password and confirmed email, reject suspended users, and call `SignInManager.CheckPasswordSignInAsync` with lockout-on-failure in `Domain/Services/ITeacherIdentityService.cs` and `Infrastructure/Services/TeacherIdentityService.cs`
- [ ] T053 [US2] Implement `TeacherLoginCommandHandler` to return safe credential/lockout errors and generate the access token in `Application/Features/Accounts/TeacherAuthentication/TeacherLogin/TeacherLoginCommandHandler.cs` (depends on T023 and T052)
- [ ] T054 [US2] Query all and only Active `CommunityUser` memberships with community names and return an empty list when none in `Application/Features/Accounts/TeacherAuthentication/TeacherLogin/TeacherLoginCommandHandler.cs`
- [ ] T055 [US2] Issue and persist a hash-only refresh token before completing login and populate both expirations in `Application/Features/Accounts/TeacherAuthentication/TeacherLogin/TeacherLoginCommandHandler.cs` and `Infrastructure/Services/RefreshTokenService.cs`
- [ ] T056 [US2] Create refresh request/command/handler files and perform suspended-teacher revalidation plus atomic single-use rotation in `Application/Features/Accounts/TeacherAuthentication/RefreshToken/RefreshTokenRequest.cs`, `RefreshTokenCommand.cs`, and `RefreshTokenCommandHandler.cs`
- [ ] T057 [US2] Create logout request/command/handler files and idempotently revoke the submitted refresh token hash in `Application/Features/Accounts/TeacherAuthentication/Logout/LogoutRequest.cs`, `LogoutCommand.cs`, and `LogoutCommandHandler.cs`
- [ ] T058 [US2] Add public versioned teacher-login, refresh-token, and logout endpoints with server-derived client IP and `BaseResponse` envelopes in `API/Controllers/AccountController.cs`

**Checkpoint**: US2 passes with zero/multiple communities, persisted lockout, secure token rotation, reuse rejection, and logout.

## Phase 6: Password recovery

**Purpose**: Deliver US3 enumeration-safe forgot/reset password behavior and revoke existing sessions.

**Independent test**: Existing and unknown emails receive the same forgot response; a valid one-hour link changes the password and invalidates all refresh tokens, while expired/invalid/reused links change nothing.

- [ ] T059 [P] [US3] Add failing forgot/reset Identity and handler tests for generic responses, eligible-teacher email delivery, invalid/expired/reused tokens, password policy, old-password rejection, and refresh-token revocation in `SprintLabs.Tests/Features/TeacherEmailAuthentication/PasswordRecoveryHandlerTests.cs`
- [ ] T060 [US3] Add failing forgot/reset controller contract cases to `SprintLabs.Tests/Features/TeacherEmailAuthentication/TeacherAuthenticationControllerContractTests.cs`
- [ ] T061 [P] [US3] Create forgot/reset request and command files in `Application/Features/Accounts/TeacherAuthentication/ForgotPassword/ForgotPasswordRequest.cs`, `ForgotPasswordCommand.cs`, `Application/Features/Accounts/TeacherAuthentication/ResetPassword/ResetPasswordRequest.cs`, and `ResetPasswordCommand.cs`; add `Shared/Responses/PasswordResetDispatchResult.cs`
- [ ] T062 [US3] Extend `ITeacherIdentityService` and `TeacherIdentityService` to generate URL-safe one-hour reset tokens only for eligible teacher password accounts and reset through `UserManager` in `Domain/Services/ITeacherIdentityService.cs` and `Infrastructure/Services/TeacherIdentityService.cs`
- [ ] T063 [US3] Implement the exact generic forgot-password response and reset-email send without account disclosure in `Application/Features/Accounts/TeacherAuthentication/ForgotPassword/ForgotPasswordCommandHandler.cs`
- [ ] T064 [US3] Implement reset, security-stamp handling when required, and revoke-all active refresh tokens in one transaction in `Infrastructure/Services/TeacherIdentityService.cs` and `Application/Features/Accounts/TeacherAuthentication/ResetPassword/ResetPasswordCommandHandler.cs`
- [ ] T065 [US3] Add public versioned forgot-password and reset-password endpoints with the documented success/error envelopes in `API/Controllers/AccountController.cs`
- [ ] T066 [US3] Extend boundary validation for required reset email/token and compliant new password in `Application/Features/Accounts/TeacherAuthentication/Common/TeacherAuthenticationValidation.cs`

**Checkpoint**: US3 passes without email enumeration, and successful reset invalidates both the old password and every prior refresh token.

## Phase 7: Community invitation acceptance

**Purpose**: Deliver US4 transactional Owner invitation/reissue and US5 explicit authenticated acceptance.

**Independent test**: An Owner invites or reissues one Pending Teacher seat, matching confirmed teacher acceptance activates it without another seat, repeats are safe, wrong/expired/revoked tokens fail, and two community invitations can be accepted independently.

- [ ] T067 [P] [US4] Add failing invite/reissue/removal tests for placeholder creation, teacher marker, Pending status for registered/unregistered users, capacity, one-time seat increment/decrement, old-token revocation, email delivery, and conflict with unsupported non-teacher Google/player accounts in `SprintLabs.Tests/Features/TeacherEmailAuthentication/TeacherInvitationServiceTests.cs` and `SprintLabs.Tests/Features/OwnerTeacherManagement/InviteTeacherCommandHandlerTests.cs`
- [ ] T068 [P] [US5] Add failing acceptance tests for authentication, confirmation, teacher marker, normalized email match, expiry/revocation, Pending-to-Active transition, repeated acceptance, zero seat increment, and multiple communities in `SprintLabs.Tests/Features/TeacherEmailAuthentication/AcceptInvitationCommandHandlerTests.cs`
- [ ] T069 [P] [US4] Create the persisted invitation entity with required membership/inviter IDs, normalized email, unique token hash, expiry, acceptance, revocation, creation, and send timestamps in `Domain/Models/TeacherInvitation.cs`
- [ ] T070 [US4] Add `DbSet<TeacherInvitation>`, required `CommunityUserId`, unique hash, active lookup indexes, membership cascade, and inviter restrict mapping in `Infrastructure/DataAccess/ApplicationDbContext.cs` (depends on T069)
- [ ] T071 [P] [US4] Define specialized transactional issue/reissue, lookup, accept, and revoke operations in `Domain/Repositories/ITeacherInvitationRepository.cs`
- [ ] T072 [US4] Implement the narrow invitation repository with transaction/conditional-update protection for membership, seat, and invitation state in `Infrastructure/Repositories/TeacherInvitationRepository.cs` (depends on T069–T071)
- [ ] T073 [P] [US4] Define invitation issue/accept service operations and Shared results in `Domain/Services/ITeacherInvitationService.cs`, `Shared/Responses/TeacherInvitationIssueResult.cs`, and `Shared/Responses/TeacherInvitationAcceptanceResponse.cs`
- [ ] T074 [US4] Implement cryptographically secure invitation generation, SHA-256 hash-only persistence, seven-day expiry, and reissue revocation in `Infrastructure/Services/TeacherInvitationService.cs` using `Infrastructure/Services/Common/SecureTokenGenerator.cs`
- [ ] T075 [US4] Implement normalized identity lookup plus eligible placeholder creation/reuse with `IsTeacherAccount=true`, while rejecting unsupported Google/player linking, in `Infrastructure/Services/TeacherInvitationService.cs` and expose teacher state in `Infrastructure/Services/UserService.cs` and `Shared/Responses/UserIdentityResponse.cs`
- [ ] T076 [US4] Update `Application/Features/Communities/Teachers/InviteTeacher/InviteTeacherCommandHandler.cs` to retain Active Owner authorization, call the transactional invitation service, keep `CommunityId` required, create/restore one Pending membership, increment `UsedTeachers` only for a newly counted seat, and send the invitation email after commit
- [ ] T077 [US4] Remove teacher auto-activation while preserving student-license activation in `Domain/Services/ICommunityLoginActivationService.cs`, `Infrastructure/Services/CommunityLoginActivationService.cs`, and `Application/Features/Accounts/GoogleAuthenticate/GoogleAuthenticationCommandHandler.cs`
- [ ] T078 [US4] Revoke all usable invitation tokens in the same one-time seat-release operation when a teacher is removed in `Application/Features/Communities/Teachers/RemoveTeacher/RemoveTeacherCommandHandler.cs` and `Infrastructure/Repositories/TeacherInvitationRepository.cs`
- [ ] T079 [P] [US5] Create acceptance request/command files in `Application/Features/CommunityInvitations/AcceptInvitation/AcceptInvitationRequest.cs` and `AcceptInvitationCommand.cs`
- [ ] T080 [US5] Implement authenticated teacher lookup, confirmed-email/active-status/marker checks, hash lookup, normalized email match, expiry/revocation checks, atomic Pending-to-Active update, `AcceptedAt`, and idempotent repeat behavior in `Infrastructure/Services/TeacherInvitationService.cs` and `Application/Features/CommunityInvitations/AcceptInvitation/AcceptInvitationCommandHandler.cs`
- [ ] T081 [US5] Add the authorized versioned acceptance endpoint, derive `userId` from claims, and return membership data through `BaseResponse<T>` in `API/Controllers/CommunityInvitationsController.cs`
- [ ] T082 [US5] Verify independent acceptance and active-community visibility across multiple communities in `SprintLabs.Tests/Features/TeacherEmailAuthentication/AcceptInvitationCommandHandlerTests.cs` and `SprintLabs.Tests/Features/CommunityAccessFoundation/GetCurrentUserCommunitiesQueryHandlerTests.cs`
- [ ] T083 [US4] Rename `SprintLabs.Tests/Features/OwnerTeacherManagement/PendingTeacherActivationTests.cs` to `SprintLabs.Tests/Features/OwnerTeacherManagement/ExplicitTeacherInvitationCompatibilityTests.cs` and replace activation-on-login expectations with explicit-acceptance compatibility expectations there, in `SprintLabs.Tests/Features/StudentLicenseActivation/StudentLicenseActivationOnLoginTests.cs`, `specs/005-owner-teacher-management/api.md`, and `specs/005-owner-teacher-management/frontend.md`
- [ ] T084 [US4] Register `ITeacherInvitationRepository` and `ITeacherInvitationService` with scoped lifetimes in `Infrastructure/ServiceConfig.cs`

**Checkpoint**: US4 and US5 pass; login alone never activates a teacher, invitation state is hash-only and transactional, and community seat counts remain correct.

## Phase 8: Migration and verification

**Purpose**: Generate and inspect the schema change, run all quality gates, confirm compatibility/layering/security, and remove accidental scope.

- [ ] T085 Generate the EF Core migration named `AddTeacherEmailAuthentication` in `Infrastructure/Migrations/*_AddTeacherEmailAuthentication.cs`, its Designer file, and `Infrastructure/Migrations/ApplicationDbContextModelSnapshot.cs` using `dotnet ef migrations add AddTeacherEmailAuthentication --project Infrastructure --startup-project API`
- [ ] T086 Inspect `Infrastructure/Migrations/*_AddTeacherEmailAuthentication.cs` for the six User columns, teacher-marker backfill, unique normalized-email protection, `RefreshTokens`, `TeacherInvitations`, fixed-length unique hashes, indexes, foreign keys, defaults, and a reversible Down method
- [ ] T087 Verify `CommunityUsers.CommunityId` remains non-nullable in `Infrastructure/Migrations/ApplicationDbContextModelSnapshot.cs` and verify neither raw refresh nor raw invitation tokens have a column in `Infrastructure/Migrations/*_AddTeacherEmailAuthentication.cs`
- [ ] T088 Run `dotnet build SprintLabs.sln` against `SprintLabs.sln` and record all warnings/errors attributable to the feature
- [ ] T089 Run focused authentication/invitation/regression tests through `SprintLabs.Tests/Compass.Tests.csproj` using the filter from `specs/015-teacher-email-authentication/quickstart.md`
- [ ] T090 Run the full relevant suite with `dotnet test SprintLabs.sln` against `SprintLabs.sln`; if optional MySQL tests are unavailable, record the environment limitation while keeping provider-independent tests passing
- [ ] T091 Fix only feature-caused build/test failures in `API/`, `Application/Features/Accounts/TeacherAuthentication/`, `Application/Features/CommunityInvitations/`, `Application/Features/Communities/Teachers/`, `Domain/`, `Infrastructure/`, `Shared/`, and `SprintLabs.Tests/Features/TeacherEmailAuthentication/`, then rerun T088–T090
- [ ] T092 Inspect `API/API.csproj`, `Application/Application.csproj`, `Domain/Domain.csproj`, `Infrastructure/Infrastructure.csproj`, and `Shared/Shared.csproj`; confirm no new project/package dependency and no Infrastructure-to-Application reference was introduced
- [ ] T093 Review Google/player compatibility in `Application/Features/Accounts/GoogleAuthenticate/GoogleAuthenticationCommandHandler.cs`, `Shared/Responses/LoginResponse.cs`, `SprintLabs.Tests/Features/B2CPlayerProfileSupport/B2CLoginCompatibilityTests.cs`, and `SprintLabs.Tests/Features/StudentLicenseActivation/StudentLicenseActivationOnLoginTests.cs`
- [ ] T094 Review invite, reissue, accept, and remove seat transitions against `Domain/Models/Community.cs`, `Application/Features/Communities/Teachers/`, `Infrastructure/Repositories/TeacherInvitationRepository.cs`, and `SprintLabs.Tests/Features/TeacherEmailAuthentication/TeacherInvitationServiceTests.cs`
- [ ] T095 Execute the registration, confirmation, reset, invitation, rotation, and logout Mailpit scenarios in `specs/015-teacher-email-authentication/quickstart.md` and confirm logs/persistence contain no JWT secret, password, or raw token
- [ ] T096 Reconcile implemented routes, envelopes, errors, loading/empty states, and explicit acceptance behavior with `specs/015-teacher-email-authentication/contracts/teacher-authentication-api.md`, `specs/015-teacher-email-authentication/api.md`, and `specs/015-teacher-email-authentication/frontend.md` without adding frontend implementation
- [ ] T097 Review `git diff` for `API/`, `Application/`, `Domain/`, `Infrastructure/`, `Shared/`, `SprintLabs.Tests/`, and `specs/`; remove unrelated refactors, generated `bin/obj/.vs` output, secrets, and any out-of-scope player/admin password, Google-linking, class, payment, report, or dashboard changes

**Checkpoint**: Migration, build, tests, manual email validation, layering, compatibility, security, documentation, and diff-scope reviews all pass.

## Dependencies & Execution Order

### Phase Dependencies

- **Phase 1** has no dependency and is read-only.
- **Phase 2** depends on Phase 1 and blocks all later implementation.
- **Phase 3** depends on Phase 2 because email links and delivery use shared authentication options and services.
- **Phase 4** depends on Phases 2–3.
- **Phase 5** depends on Phase 2; execute after Phase 4 in the requested order so real confirmed teachers can exercise login.
- **Phase 6** depends on Phases 2, 3, and 5 because reset revokes refresh tokens.
- **Phase 7** depends on Phases 2–5 because acceptance requires a confirmed authenticated teacher and email delivery.
- **Phase 8** depends on all implementation phases.

### User Story Dependencies

```text
US1 Registration/Confirmation ──> US2 Login/Session ──> US3 Recovery
          │                           │
          └──> US6 Resend             └──> US5 Acceptance
US4 Owner Invitation ─────────────────────> US5 Acceptance
```

- **US1** is independently testable after the shared foundation/email phases.
- **US6** shares the US1 confirmation state but is independently testable with a seeded unconfirmed teacher.
- **US2** can be service-tested with a seeded confirmed teacher; end-to-end validation follows US1.
- **US3** can be tested with a seeded teacher/session; it depends on refresh revocation infrastructure from US2.
- **US4** can be tested with an Owner and capacity independently of teacher login.
- **US5** requires an invitation from US4 and an authenticated confirmed teacher from US1/US2.

### Within Each Phase

1. Write the listed tests and confirm they fail for the missing behavior.
2. Add contracts/entities/options before implementations.
3. Add persistence mapping/repositories before transactional services.
4. Add services before CQRS handlers.
5. Add handlers before controller endpoints.
6. Run the phase's focused tests before advancing.

## Parallel Opportunities

- After T001, inspection tasks T002–T009 can run in parallel.
- In Phase 2, T010, T012, T013, T017, T019, T021, and T022 touch independent files.
- In Phase 3, T025–T029 can be prepared in parallel; T030–T033 serialize on `SmtpEmailService.cs`.
- In Phase 4, failing tests T036–T038 and contract/model tasks T039–T041 can be prepared in parallel before service implementation.
- In Phase 5, tests T048–T050 can be written in parallel; response models T051 can proceed while they are prepared.
- In Phase 7, T067–T069, T071, T073, and T079 use independent files; repository/service/handler tasks then follow their stated dependencies.
- Do not parallel-edit `API/Program.cs`, `Infrastructure/ServiceConfig.cs`, `Infrastructure/DataAccess/ApplicationDbContext.cs`, `Infrastructure/Services/TeacherIdentityService.cs`, or `API/Controllers/AccountController.cs`.

## Parallel Examples

```text
Phase 2:
- T010 User teacher/cooldown fields
- T012 Shared teacher-authentication options
- T013 JWT/token result contracts
- T017 RefreshToken entity
- T019 Refresh-token repository contract
- T021 Token service contracts
- T022 Secure token generator

Phase 7:
- T067 Owner invitation tests
- T068 acceptance tests
- T069 TeacherInvitation entity
- T071 invitation repository contract
- T073 invitation service/results contracts
- T079 acceptance request/command
```

## Implementation Strategy

### MVP

The smallest demonstrable teacher-authentication increment is:

1. Phase 1 inspection
2. Phase 2 authentication foundation
3. Phase 3 email infrastructure
4. Phase 4 US1 registration/confirmation plus US6 resend

This MVP creates and verifies standalone teacher accounts but intentionally does not claim login/session completion until Phase 5.

### Incremental Delivery

1. Complete Phases 1–3 and verify the shared foundation.
2. Complete Phase 4; stop and validate registration, confirmation, resend, and no side effects.
3. Complete Phase 5; validate login, empty/multiple communities, refresh rotation, and logout.
4. Complete Phase 6; validate recovery and session revocation.
5. Complete Phase 7; validate Owner invite/reissue and explicit acceptance.
6. Complete Phase 8 once all desired stories are implemented.

## Excluded Work

Do not add tasks or code for frontend implementation, player password authentication, platform-admin password authentication, Google account linking, game SSO redesign, PlayerProfile creation for teachers, teacher class assignment, payment features, reports, or dashboards.
