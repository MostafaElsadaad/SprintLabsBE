# Tasks: Invite-Only Teacher Authentication

**Input**: Design documents from `/specs/016-invite-only-teacher-authentication/`

**Prerequisites**: `spec.md`, `plan.md`, `research.md`, `data-model.md`, `contracts/invite-only-teacher-authentication-api.md`, `quickstart.md`

**Tests**: Automated tests are required by the feature specification. Story-level tests are written before their implementation tasks, and MySQL concurrency plus full regression verification are completed before handoff.

**Organization**: This remains one coordinated backend feature. Tasks are grouped by dependency and then by the specification's user stories; no phase creates a separate authentication or invitation system.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel because it targets different files and does not depend on an incomplete task.
- **[Story]**: Maps the task to a user story from `spec.md`.
- All paths are relative to the repository root.

## Phase 1: Inspection and Contract Decisions

**Purpose**: Reconfirm the implementation map at execution time and freeze compatibility boundaries before editing code.

- [X] T001 Inspect the current teacher registration, confirmation, login, forgot/reset, refresh/logout, Google, and Firebase routes and record any drift from the plan in `specs/016-invite-only-teacher-authentication/research.md`, using `API/Controllers/AccountController.cs` and `Application/Features/Accounts/`
- [X] T002 [P] Inspect owner invitation and authenticated acceptance behavior, service call chains, and email dispatch boundaries and record any drift in `specs/016-invite-only-teacher-authentication/research.md`, using `API/Controllers/CommunitiesController.cs`, `API/Controllers/CommunityInvitationsController.cs`, `Application/Features/Communities/Teachers/InviteTeacher/`, and `Application/Features/CommunityInvitations/AcceptInvitation/`
- [X] T003 [P] Inspect the current `User`, `CommunityUser`, `CommunityLicense`, `TeacherInvitation`, and `RefreshToken` mappings/indexes and the latest model snapshot before schema edits, using `Infrastructure/DataAccess/User.cs`, `Domain/Models/Community.cs`, `Domain/Models/TeacherInvitation.cs`, `Domain/Models/RefreshToken.cs`, `Infrastructure/DataAccess/ApplicationDbContext.cs`, and `Infrastructure/Migrations/ApplicationDbContextModelSnapshot.cs`
- [X] T004 [P] Freeze the unchanged Google contract by recording the current route, command/response, claims, service signatures, and relevant regression files in `specs/016-invite-only-teacher-authentication/research.md`, inspecting `API/Controllers/AccountController.cs`, `Application/Features/Accounts/GoogleAuthenticate/`, `Domain/Services/IGoogleAuthenticationService.cs`, `Infrastructure/Services/GoogleAuthenticationService.cs`, and `SprintLabs.Tests/Features/B2CPlayerProfileSupport/B2CLoginCompatibilityTests.cs`
- [X] T005 Resolve implementation-level contract constants—new `ErrorCode` numeric values, generic login status convention, invalid-token status convention, and unchanged versioned route casing—and update `specs/016-invite-only-teacher-authentication/contracts/invite-only-teacher-authentication-api.md` without changing product decisions
- [X] T006 Run and record the pre-change baseline with `dotnet build SprintLabs.sln` and `dotnet test SprintLabs.sln --no-build --nologo` in `specs/016-invite-only-teacher-authentication/quickstart.md`, noting failures before feature edits

**Checkpoint**: Existing implementation, schema, contracts, and unchanged Google boundary are confirmed.

---

## Phase 2: Foundational Domain, Shared Contract, and Schema Code

**Purpose**: Add the blocking cross-story contracts and model configuration without generating the migration yet.

**Critical**: No user-story service implementation begins until this phase compiles.

- [X] T007 Add stable append-only enum/message entries for `TeacherAlreadyBelongsToAnotherCommunity`, `InvalidTeacherInvitation`, and `InvalidOrExpiredPasswordResetToken` without changing existing values in `Shared/Enums/ErrorCode.cs` and `Shared/Enums/ErrorMessage.cs`
- [X] T008 [P] Add `PasswordResetResendCooldownSeconds` to `Shared/Options/TeacherAuthenticationOptions.cs` and bind its default in `API/appsettings.json` while preserving existing Email, Frontend, JWT, Identity password, lockout, and token-provider settings
- [X] T009 [P] Add nullable UTC `LastPasswordResetEmailSentAt` to the Identity user entity in `Infrastructure/DataAccess/User.cs` with no existing-data backfill behavior
- [X] T010 Configure only missing non-unique lookup indexes for `CommunityUser(UserId, Role, Status, CommunityId)` and `TeacherInvitation(CommunityUserId, AcceptedAt, RevokedAt, ExpiresAt)` in `Infrastructure/DataAccess/ApplicationDbContext.cs`, preserving normalized-email/token-hash uniqueness and non-teacher relationship support
- [X] T011 [P] Add minimal cross-layer invitation validation/completion and updated identity dispatch result types as one public class per file under `Shared/Responses/`, including `TeacherInvitationValidationResult.cs` and updates to `PasswordResetDispatchResult.cs`; do not introduce a new response envelope
- [X] T012 Evolve existing service contracts in `Domain/Services/ITeacherIdentityService.cs`, `Domain/Services/ITeacherInvitationService.cs`, and `Domain/Services/IEmailService.cs` for identifier login/recovery, email-only invitation issue, validation, and completion while retaining refresh/JWT interfaces and avoiding Application references
- [X] T013 Update compile-only callers and registrations required by T012 in `Infrastructure/ServiceConfig.cs` and existing Application handlers, making no behavioral changes to Google, Firebase/player, refresh, or logout flows
- [X] T014 Run `dotnet build SprintLabs.sln` and resolve only interface/model compilation failures in the touched `Shared`, `Domain`, `Infrastructure`, `Application`, and `API` files before story work

**Checkpoint**: Shared contracts and schema model compile; no parallel service stack or project reference has been added.

---

## Phase 3: User Story 1 — Owner Invites One-Community Teacher (Priority: P1)

**Goal**: Keep the existing owner endpoint, reserve exactly one pending teacher/community relationship, support safe same-community reissue, and reject another community with HTTP 409.

**Independent Test**: An Active Owner can invite/reinvite one normalized email in the same community without duplicate user, membership, seat, or usable token; a non-owner is denied; another community receives the stable 409; no player/student records are created.

### Tests for User Story 1

- [X] T015 [P] [US1] Update command/controller contract tests for email-only input, unchanged owner route, active-owner enforcement, and HTTP 409 mapping in `SprintLabs.Tests/Features/OwnerTeacherManagement/InviteTeacherCommandHandlerTests.cs` and a new `SprintLabs.Tests/Features/InviteOnlyTeacherAuthentication/OwnerInvitationContractTests.cs`
- [X] T016 [P] [US1] Add failing invitation service tests for normalized-email reuse, new pending reservation, same-community reissue, cross-community Active/Pending conflict, token hash storage, prior-token revocation, capacity/counter idempotence, and no `PlayerProfile`/`StudentLicense` in `SprintLabs.Tests/Features/InviteOnlyTeacherAuthentication/TeacherInvitationIssueTests.cs`

### Implementation for User Story 1

- [X] T017 [P] [US1] Remove owner-supplied teacher name and retain email validation in `Application/Features/Communities/Teachers/InviteTeacher/InviteTeacherRequest.cs` and `Application/Features/Communities/Teachers/InviteTeacher/InviteTeacherCommand.cs`
- [X] T018 [US1] Implement authoritative Active Owner/community recheck, normalized user reuse, one-current-Teacher relationship enforcement, same-community membership reuse, locked capacity accounting, token supersession, SHA-256 storage, UTC audit data, and deterministic race mapping in the existing `Infrastructure/Services/TeacherInvitationService.cs`
- [X] T019 [US1] Update `Application/Features/Communities/Teachers/InviteTeacher/InviteTeacherCommandHandler.cs` to use the evolved service, build/send the raw link only after transaction commit, avoid token logging, and return the existing `BaseResponse` convention
- [X] T020 [US1] Keep only `POST /api/v{version}/Communities/{communityId}/teachers/invite` and document its Active Owner and 409 responses in `API/Controllers/CommunitiesController.cs`; do not add a second invitation action
- [X] T021 [P] [US1] Change invitation URL generation to `{Frontend.BaseUrl}/invitations/teacher/setup?token=...` and remove reliance on owner-supplied teacher name in `Application/Features/Accounts/TeacherAuthentication/Common/TeacherAuthenticationLinkBuilder.cs` and `Infrastructure/Services/SmtpEmailService.cs`
- [X] T022 [US1] Run the US1 tests in `SprintLabs.Tests/Features/OwnerTeacherManagement/InviteTeacherCommandHandlerTests.cs`, `SprintLabs.Tests/Features/InviteOnlyTeacherAuthentication/OwnerInvitationContractTests.cs`, and `SprintLabs.Tests/Features/InviteOnlyTeacherAuthentication/TeacherInvitationIssueTests.cs` and fix only invitation-issuance regressions

**Checkpoint**: Invitation issuance is independently functional and reserves one community safely.

---

## Phase 4: User Story 2 — Invited Teacher Validates and Completes Setup (Priority: P1)

**Goal**: Replace authenticated acceptance with anonymous read-only validation and one-time transactional completion that sets name/password, confirms the immutable email, and activates the existing membership without login.

**Independent Test**: A current token returns only community name/masked email/expiry; valid completion activates exactly once; invalid/expired/revoked/used tokens are safe; concurrent/repeated completion creates no duplicate membership, counter, player/student data, or tokens.

### Tests for User Story 2

- [X] T023 [P] [US2] Add failing validation tests for safe fields, masking, no state mutation, and generic invalid/expired/revoked/superseded/used handling in `SprintLabs.Tests/Features/InviteOnlyTeacherAuthentication/TeacherInvitationValidationTests.cs`
- [X] T024 [P] [US2] Add failing completion tests for name trimming, Identity password policy, immutable invited email, email confirmation, deterministic username, existing membership activation, one-community recheck/409, replay, no auto-login, no counter change, and no player/student records in `SprintLabs.Tests/Features/InviteOnlyTeacherAuthentication/TeacherInvitationCompletionTests.cs`
- [X] T025 [P] [US2] Add controller/authorization contract tests for anonymous validate/complete and removal of authenticated accept in `SprintLabs.Tests/Features/InviteOnlyTeacherAuthentication/CommunityInvitationControllerTests.cs`

### Implementation for User Story 2

- [X] T026 [P] [US2] Create `Application/Features/CommunityInvitations/ValidateTeacherInvitation/ValidateTeacherInvitationQuery.cs` for the raw-token query using the existing MediatR and response conventions
- [X] T027 [US2] Implement read-only validation mapping and safe errors in `Application/Features/CommunityInvitations/ValidateTeacherInvitation/ValidateTeacherInvitationQueryHandler.cs`, returning only the shared community name, masked email, and expiry result
- [X] T028 [P] [US2] Create exact token/name/password input files `Application/Features/CommunityInvitations/CompleteTeacherInvitation/CompleteTeacherInvitationRequest.cs` and `Application/Features/CommunityInvitations/CompleteTeacherInvitation/CompleteTeacherInvitationCommand.cs`
- [X] T029 [US2] Implement completion orchestration and safe `BaseResponse`/`GenericException` mapping in `Application/Features/CommunityInvitations/CompleteTeacherInvitation/CompleteTeacherInvitationCommandHandler.cs`
- [X] T030 [US2] Implement token-hash validation plus transactional row locking, repeated one-community checks, Identity `AddPasswordAsync`/`ResetPasswordAsync`, optional refresh revocation, existing membership activation, invitation acceptance, and replay protection in `Infrastructure/Services/TeacherInvitationService.cs`
- [X] T031 [US2] Replace the authorized accept action with explicit versioned kebab-case `[AllowAnonymous]` validate and complete actions plus response annotations in `API/Controllers/CommunityInvitationsController.cs`
- [X] T032 [US2] Delete the obsolete authenticated acceptance files under `Application/Features/CommunityInvitations/AcceptInvitation/` and remove `Shared/Responses/TeacherInvitationAcceptanceResponse.cs` only after `rg` proves there are no remaining callers
- [X] T033 [US2] Run the validation, completion, and controller tests under `SprintLabs.Tests/Features/InviteOnlyTeacherAuthentication/` and verify successful completion returns no access or refresh token

**Checkpoint**: An invited teacher can complete setup anonymously exactly once and must then navigate to login.

---

## Phase 5: User Story 3 — Eligible Teacher Signs In to One Community (Priority: P1)

**Goal**: Authenticate by normalized username or email while preserving lockout and issue existing tokens only after exactly one active teacher/community relationship is established.

**Independent Test**: Email and username succeed for one eligible active teacher; all unknown, invalid, pending, passwordless, unconfirmed, suspended, locked, no-community, inactive-community, or multiple-current-community cases fail generically before token issuance.

### Tests for User Story 3

- [X] T034 [P] [US3] Add failing Identity-service tests for normalized email/username resolution, ambiguous matches, generic invalid password, confirmed/password/teacher/suspension checks, and preserved lockout-on-failure in `SprintLabs.Tests/Features/InviteOnlyTeacherAuthentication/TeacherIdentityAuthenticationTests.cs`
- [X] T035 [P] [US3] Add failing login-handler tests for pending/no/removed/multiple memberships, inactive community, safe inconsistency logging, singular community response, token-service call ordering, and no player/student creation in `SprintLabs.Tests/Features/InviteOnlyTeacherAuthentication/TeacherLoginCommandHandlerTests.cs`

### Implementation for User Story 3

- [X] T036 [P] [US3] Change email input to `Identifier` in `Application/Features/Accounts/TeacherAuthentication/TeacherLogin/TeacherLoginRequest.cs` and `Application/Features/Accounts/TeacherAuthentication/TeacherLogin/TeacherLoginCommand.cs`
- [X] T037 [P] [US3] Change the success DTO from a community list to one `TeacherCommunityResponse` while preserving access/refresh field names and formats in `Shared/Responses/TeacherLoginResponse.cs`, `Shared/Responses/TeacherCommunityResponse.cs`, and related token results only where required
- [X] T038 [US3] Modify `Infrastructure/Services/TeacherIdentityService.cs` to normalize/resolve username or email, preserve generic failures and `CheckPasswordSignInAsync(..., lockoutOnFailure: true)`, and enforce teacher/setup/password/confirmation/suspension/lockout eligibility without touching Google services
- [X] T039 [US3] Modify `Application/Features/Accounts/TeacherAuthentication/TeacherLogin/TeacherLoginCommandHandler.cs` to require exactly one Active Teacher membership in an active community and zero Pending relationships before calling existing access/refresh token services; warning-log legacy multi-community data by internal ID only
- [X] T040 [US3] Update login binding and response annotations in `API/Controllers/AccountController.cs` without changing refresh, logout, Google, Firebase, admin, owner, or player actions
- [X] T041 [US3] Run `TeacherIdentityAuthenticationTests`, `TeacherLoginCommandHandlerTests`, and existing `SprintLabs.Tests/Features/TeacherEmailAuthentication/AccessTokenServiceTests.cs`, confirming failure paths never invoke token issuance

**Checkpoint**: Password login independently returns the existing token format and one community only for eligible teachers.

---

## Phase 6: User Story 4 — Teacher Recovers a Password Privately (Priority: P2)

**Goal**: Accept email or username, always return a generic HTTP 202, apply a persisted cooldown, reset through Identity, and revoke every active refresh token.

**Independent Test**: Known and unknown identifiers receive identical public responses; only eligible completed teachers receive reset mail; valid reset changes password and revokes sessions; invalid/expired tokens change nothing and reveal nothing.

### Tests for User Story 4

- [X] T042 [P] [US4] Add failing forgot-password tests for username/email, unknown/ineligible/passwordless/suspended/no-community accounts, atomic cooldown, SMTP failure, exact generic HTTP 202, URL-safe userId link, and no raw-token logging in `SprintLabs.Tests/Features/InviteOnlyTeacherAuthentication/ForgotPasswordTests.cs`
- [X] T043 [P] [US4] Add failing reset tests for valid/invalid/expired/used tokens, configured password policy, transactional all-refresh-token revocation, no auto-login, and no membership mutation in `SprintLabs.Tests/Features/InviteOnlyTeacherAuthentication/ResetPasswordTests.cs`

### Implementation for User Story 4

- [X] T044 [P] [US4] Change forgot-password input from email to identifier in `Application/Features/Accounts/TeacherAuthentication/ForgotPassword/ForgotPasswordRequest.cs` and `Application/Features/Accounts/TeacherAuthentication/ForgotPassword/ForgotPasswordCommand.cs`
- [X] T045 [US4] Implement safe identifier resolution, completed-teacher eligibility, exactly-one-active-community check, password presence, and atomic persisted resend-cooldown claim in `Infrastructure/Services/TeacherIdentityService.cs` and return `UserId` through `Shared/Responses/PasswordResetDispatchResult.cs`
- [X] T046 [US4] Return the fixed generic accepted result even for unknown/ineligible/cooldown/mail failure, and build `{Frontend.BaseUrl}/reset-password?userId=...&token=...` without logging the token in `Application/Features/Accounts/TeacherAuthentication/ForgotPassword/ForgotPasswordCommandHandler.cs` and `Application/Features/Accounts/TeacherAuthentication/Common/TeacherAuthenticationLinkBuilder.cs`
- [X] T047 [P] [US4] Change reset input from email to `UserId`, URL-safe token, and new password in `Application/Features/Accounts/TeacherAuthentication/ResetPassword/ResetPasswordRequest.cs` and `Application/Features/Accounts/TeacherAuthentication/ResetPassword/ResetPasswordCommand.cs`
- [X] T048 [US4] Reset through configured ASP.NET Core Identity APIs, map all invalid/expired account-token cases safely, and retain transactional active-refresh-token revocation in `Infrastructure/Services/TeacherIdentityService.cs` and `Application/Features/Accounts/TeacherAuthentication/ResetPassword/ResetPasswordCommandHandler.cs`
- [X] T049 [US4] Update forgot/reset action binding, HTTP 202 response, and safe response annotations in `API/Controllers/AccountController.cs` without modifying token-provider registration in `API/Program.cs`
- [X] T050 [US4] Run `ForgotPasswordTests`, `ResetPasswordTests`, and existing refresh token tests, checking that raw reset/access/refresh values are absent from captured logs and database assertions

**Checkpoint**: Password recovery is independently enumeration-safe and invalidates old refresh sessions after reset.

---

## Phase 7: User Story 5 — Retire Public Registration Without Breaking Existing Authentication (Priority: P1)

**Goal**: Remove every public registration/confirmation/resend entry point and dead application surface while preserving existing eligible teachers, refresh/logout, Identity reset providers, Google, Firebase/player, and persisted data.

**Independent Test**: Retired routes and authenticated accept are absent from routing/Swagger; no alternative public account-creation handler remains; existing eligible teachers, Google login, refresh rotation, and logout behave exactly as before except for the specified teacher-login contract.

### Tests for User Story 5

- [X] T051 [P] [US5] Add failing reflection/controller contract tests proving teacher register, confirm-email, resend-confirmation, and authenticated accept actions are absent while invite/login/validate/complete/forgot/reset routes remain in `SprintLabs.Tests/Features/InviteOnlyTeacherAuthentication/RetiredEndpointContractTests.cs`
- [X] T052 [P] [US5] Add refresh/logout compatibility coverage without creating a second token implementation in `SprintLabs.Tests/Features/InviteOnlyTeacherAuthentication/RefreshLogoutRegressionTests.cs`; retain Google test files unchanged

### Implementation for User Story 5

- [X] T053 [US5] Remove public register, confirm-email, and resend-confirmation actions/usings from `API/Controllers/AccountController.cs` without editing its Google, Firebase, refresh, or logout action contracts
- [X] T054 [US5] Delete obsolete CQRS/DTO files under `Application/Features/Accounts/TeacherAuthentication/RegisterTeacher/`, `Application/Features/Accounts/TeacherAuthentication/ConfirmEmail/`, and `Application/Features/Accounts/TeacherAuthentication/ResendConfirmation/` after reference checks
- [X] T055 [US5] Remove now-unused registration/confirmation/resend methods and result types from `Domain/Services/ITeacherIdentityService.cs`, `Infrastructure/Services/TeacherIdentityService.cs`, `Domain/Services/IEmailService.cs`, `Infrastructure/Services/SmtpEmailService.cs`, and obsolete files in `Shared/Responses/`, while preserving reset/invitation email infrastructure and all Identity token-provider configuration in `API/Program.cs`
- [X] T056 [US5] Run retired-route and refresh/logout regression tests, then use `rg` across `API/`, `Application/`, `Domain/`, `Infrastructure/`, and `Shared/` to prove no reachable public registration/confirmation/resend handler remains

**Checkpoint**: Invitation is the only new teacher-password entry path and unrelated authentication remains intact.

---

## Phase 8: Cross-Story Automated and Concurrency Tests

**Purpose**: Prove transactional invariants and the coordinated lifecycle across story boundaries before generating the migration.

- [X] T057 Make `SprintLabs.Tests/Fixtures/MysqlDatabaseFixture.cs` read a dedicated opt-in test connection string from a task-specific environment variable, refuse unsafe/shared database targets, and avoid the current hardcoded destructive credential/database behavior
- [ ] T058 Add real-MySQL concurrent different-community invite, same-community reissue, remaining-capacity accounting, and one-winner invitation completion cases in `SprintLabs.Tests/Features/InviteOnlyTeacherAuthentication/InviteOnlyTeacherAuthenticationConcurrencyTests.cs`
- [X] T059 [P] Add an end-to-end handler/service lifecycle test covering owner invite → anonymous validate → complete → password login → reset → refresh rejection, including no duplicate membership/counter/profile/license, in `SprintLabs.Tests/Features/InviteOnlyTeacherAuthentication/InviteOnlyTeacherAuthenticationLifecycleTests.cs`
- [X] T060 Run all tests under `SprintLabs.Tests/Features/InviteOnlyTeacherAuthentication/` plus `SprintLabs.Tests/Features/OwnerTeacherManagement/InviteTeacherCommandHandlerTests.cs` and fix cross-story failures without changing product scope

---

## Phase 9: Single Non-Destructive Migration

**Purpose**: Materialize only the reviewed model changes after behavior and model tests are stable.

- [X] T061 Generate exactly one EF Core migration named `InviteOnlyTeacherAuthentication` under `Infrastructure/Migrations/` using `Infrastructure/Infrastructure.csproj`, `API/API.csproj`, and `Infrastructure/DataAccess/ApplicationDbContextFactory.cs`
- [X] T062 Inspect the generated `Infrastructure/Migrations/*_InviteOnlyTeacherAuthentication.cs`, designer, and `Infrastructure/Migrations/ApplicationDbContextModelSnapshot.cs`; remove unintended changes and verify the migration only adds nullable `LastPasswordResetEmailSentAt` and missing non-unique lookup indexes with a non-destructive `Down`
- [ ] T063 Apply the migration only to the dedicated disposable MySQL test database, run the concurrency tests from `SprintLabs.Tests/Features/InviteOnlyTeacherAuthentication/InviteOnlyTeacherAuthenticationConcurrencyTests.cs`, and verify existing invitation/membership/user rows survive

---

## Phase 10: Swagger, Documentation, and Final Regression Verification

**Purpose**: Publish the final contract and prove no unrelated authentication behavior changed.

- [X] T064 [P] Reconcile implemented endpoints, final numeric error codes, request/response examples, permissions, and frontend states in `specs/016-invite-only-teacher-authentication/contracts/invite-only-teacher-authentication-api.md`, `specs/016-invite-only-teacher-authentication/api.md`, and `specs/016-invite-only-teacher-authentication/frontend.md`
- [ ] T065 Verify Swashbuckle output from `API/Program.cs` and touched controllers shows invite, validate, complete, identifier login, generic forgot, and userId reset contracts; confirm retired endpoints are absent and Google schemas/routes are unchanged, recording results in `specs/016-invite-only-teacher-authentication/quickstart.md`
- [X] T066 Run `dotnet build SprintLabs.sln` and resolve all new errors/warnings attributable to files changed by this feature
- [X] T067 Run the complete focused invite-only suite with `dotnet test SprintLabs.Tests/Compass.Tests.csproj --no-build --filter FullyQualifiedName~InviteOnlyTeacherAuthentication` and record results in `specs/016-invite-only-teacher-authentication/quickstart.md`
- [X] T068 Run unchanged Google behavior tests in `SprintLabs.Tests/Features/B2CPlayerProfileSupport/B2CLoginCompatibilityTests.cs`, `SprintLabs.Tests/Features/OwnerTeacherManagement/PendingTeacherActivationTests.cs`, and `SprintLabs.Tests/Features/StudentLicenseActivation/StudentLicenseActivationOnLoginTests.cs`; use `git diff` to verify `Application/Features/Accounts/GoogleAuthenticate/`, `Domain/Services/IGoogleAuthenticationService.cs`, `Infrastructure/Services/GoogleAuthenticationService.cs`, and these Google tests have no behavioral edits
- [X] T069 Run all Firebase/player tests under `SprintLabs.Tests/Features/FirebasePlayerAuthentication/` and confirm their existing pending-membership/player behavior remains unchanged
- [X] T070 Run refresh/logout and token-format regression tests in `SprintLabs.Tests/Features/InviteOnlyTeacherAuthentication/RefreshLogoutRegressionTests.cs` and `SprintLabs.Tests/Features/TeacherEmailAuthentication/AccessTokenServiceTests.cs`, confirming the implementation still uses `Infrastructure/Services/AccessTokenService.cs`, `Infrastructure/Services/RefreshTokenService.cs`, and `Infrastructure/Repositories/RefreshTokenRepository.cs`
- [X] T071 Run the full suite with `dotnet test SprintLabs.sln --no-build --nologo`, require no failed tests, and record pass/fail/skip totals plus any test-environment limitation in `specs/016-invite-only-teacher-authentication/quickstart.md`
- [ ] T072 Execute every smoke/security check in `specs/016-invite-only-teacher-authentication/quickstart.md`, including raw-token/log inspection, UTC timestamps, one-community failures, replay protection, no auto-login, no player/student artifacts, Swagger removal, and preservation of existing user data
- [X] T073 Run `git diff --check` and review the final diff for forbidden new project references/packages, feature-specific repositories, duplicate auth/token/invitation stacks, hardcoded URLs/secrets, destructive migration operations, or changes outside the single feature

---

## Dependencies and Execution Order

### Phase dependencies

- **Phase 1 — Inspection/contracts**: Starts immediately and freezes compatibility decisions.
- **Phase 2 — Foundation**: Depends on Phase 1 and blocks all user-story implementation.
- **US1 invitation**: Depends on Phase 2 and establishes the pending membership/token used by US2.
- **US2 validation/completion**: Depends on US1 service contracts and invitation state.
- **US3 login**: Depends on Phase 2 and final membership semantics from US2; token issuance remains existing infrastructure.
- **US4 recovery**: Depends on Phase 2 and reuses US3 eligibility rules plus existing refresh revocation.
- **US5 retirement**: Runs after replacement flows compile so obsolete methods can be removed safely without an intermediate broken solution.
- **Cross-story tests**: Depend on US1–US5 implementation.
- **Migration**: Depends on stable schema configuration and cross-story tests; only one migration is generated.
- **Swagger/final verification**: Depends on all implementation, tests, and migration work.

### User story dependency graph

```text
Inspection/contracts
        |
    Foundation
        |
       US1 Owner invitation
        |
       US2 Validate/complete
        |
       US3 Eligible login
        |
       US4 Password recovery
        |
       US5 Retire old public flow
        |
Tests -> Migration -> Swagger/docs -> Full regression
```

US3 and US4 can be developed in parallel after Phase 2 when their changes to `TeacherIdentityService.cs` and `AccountController.cs` are coordinated, but the recommended single-agent order is US1 → US2 → US3 → US4 → US5 to minimize file conflicts.

### Within each user story

- Add focused failing tests before changing behavior.
- Change Shared/Domain contracts before Infrastructure implementations.
- Complete service behavior before controller exposure/removal.
- Run the story's focused tests before crossing its checkpoint.
- Do not modify Google behavior to make a teacher-password test pass.

## Parallel Opportunities

- T002–T004 inspect independent surfaces in parallel after T001 starts.
- T008, T009, and T011 target different foundational files and can proceed after T007's enum decisions are known.
- Within US1, request/command work (T017) and link/template work (T021) can proceed while service tests are prepared.
- Within US2, validation query work (T026) and completion contract work (T028) target separate vertical slices.
- Within US3, request contract (T036) and response contract (T037) can proceed together.
- Within US4, forgot input (T044) and reset input (T047) target separate subfeatures.
- Lifecycle tests (T059) can be written while MySQL fixture/concurrency work (T057–T058) proceeds.
- Documentation reconciliation (T064) can begin while the final build/test commands are being prepared, after endpoint contracts stabilize.

## Parallel Example: User Story 2

```text
Task T023: Write validation tests in TeacherInvitationValidationTests.cs
Task T024: Write completion tests in TeacherInvitationCompletionTests.cs
Task T025: Write anonymous-route tests in CommunityInvitationControllerTests.cs

After the failing contracts are established:
Task T026: Create the validation query
Task T028: Create the completion request and command
```

## Implementation Strategy

### Coherent P1 delivery

US1 alone is not a deployable MVP because it would send invitations that teachers could not complete. The minimum coherent invitation-only authentication delivery is:

1. Phase 1 inspection and Phase 2 foundation.
2. US1 owner invitation.
3. US2 anonymous validation/completion.
4. US3 eligible one-community login.
5. US5 retirement of public registration with unchanged Google/refresh/logout behavior.
6. Cross-story tests, migration, Swagger/docs, and full regression gates.

US4 password recovery is P2 in the specification but is required before final production release because the requested coordinated backend change retains self-service recovery.

### Incremental checkpoints

1. Foundation compiles without changing public behavior.
2. Owner invitation safely reserves one community.
3. Anonymous completion activates exactly once without login.
4. Password login admits only one eligible active community.
5. Recovery is generic and revokes old sessions.
6. Old public entry points are removed only after replacements work.
7. One reviewed migration and all regression gates complete the feature.

## Notes

- `[P]` marks only tasks that can safely touch independent files at that point.
- Existing `IBaseRepository<T>`/`BaseRepository<T>` and current Infrastructure services remain the default; do not add one-line feature repositories.
- Infrastructure must not reference Application; Application must not reference Infrastructure; Shared remains dependency-free.
- All raw invitation, reset, access, refresh, and password values are prohibited from logs.
- Commit after each checkpoint or tightly related task group; do not mix unrelated refactors.
