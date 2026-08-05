---

description: "Ordered implementation tasks for platform-administrator password authentication"
---

# Tasks: Platform Administrator Password Authentication

**Input**: Design documents from `specs/017-platform-admin-password-authentication/`

**Prerequisites**: `plan.md`, `spec.md`, `research.md`, `data-model.md`, `contracts/platform-admin-password-authentication-api.md`, `quickstart.md`

**Tests**: Tests are required. Within every user-story phase, write the listed tests first and confirm that they fail for the expected missing behavior before implementing the story.

**Organization**: Tasks are grouped by user story. P1 stories are scheduled before P2 recovery work. Each task names its expected layer and concrete file path; paths marked as new are created only when that task is implemented.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: May run in parallel because the task targets different files and has no unmet dependency.
- **[Story]**: Maps the task to a user story in `spec.md`.
- Setup, foundational, and cross-cutting tasks intentionally have no story label.

## Phase 1: Inspection and Contract Confirmation

**Purpose**: Reconfirm the implementation surface against the current worktree before any source change.

- [ ] T001 [Inspection] Re-map the current Google login, Identity user/configuration, persisted-admin authorization, access-token signer, refresh rotation, logout, forgot/reset handlers, reset email/link services, and DI registrations in `API/Controllers/AccountController.cs`, `Application/Features/Accounts/GoogleAuthenticate/`, `Application/Features/Accounts/Common/`, `Application/Features/Accounts/TeacherAuthentication/`, `Application/Features/Admin/Communities/Common/AdminCommunityAuthorization.cs`, `Domain/Services/`, `Infrastructure/DataAccess/User.cs`, `Infrastructure/Services/`, `Application/ServiceConfig.cs`, and `Infrastructure/ServiceConfig.cs`; reconcile any changed paths in `specs/017-platform-admin-password-authentication/research.md` before coding.
- [ ] T002 [Contract] Confirm the exact versioned routes, `BaseResponse` envelopes, generic credential/recovery errors, stable setup conflict, trusted `IsPlatformAdmin` source, no-community behavior, and frozen Google contract against `API/Controllers/AccountController.cs`, `Shared/Responses/BaseResponse.cs`, `Shared/Enums/ErrorCode.cs`, `Shared/Enums/ErrorMessage.cs`, `specs/017-platform-admin-password-authentication/spec.md`, and `specs/017-platform-admin-password-authentication/contracts/platform-admin-password-authentication-api.md`; resolve documentation discrepancies before creating implementation files.
- [ ] T003 [P] [Infrastructure/Schema] Reinspect `Infrastructure/DataAccess/User.cs`, `Infrastructure/DataAccess/ApplicationDbContext.cs`, `Domain/Models/RefreshToken.cs`, `Infrastructure/Migrations/ApplicationDbContextModelSnapshot.cs`, and the existing Identity/teacher-auth migrations; record the result in `specs/017-platform-admin-password-authentication/data-model.md`, leave `Infrastructure/Migrations/` unchanged when the planned fields are present, and create a non-destructive migration there only if a genuinely missing required schema element is proven.
- [ ] T004 [Verification] Capture the pre-change baseline with `dotnet build SprintLabs.sln --no-restore --nologo` and `dotnet test SprintLabs.sln --no-build --nologo`, recording new deviations from the planning baseline in `specs/017-platform-admin-password-authentication/quickstart.md` without editing product code.

---

## Phase 2: Foundational Trusted Authentication Contracts

**Purpose**: Introduce the smallest shared contracts needed by login and refresh without creating a second JWT, refresh-token, or recovery stack.

**Critical**: Complete this phase before implementing a user story.

- [ ] T005 [P] [Shared] Add the non-persisted trusted `Teacher`/`PlatformAdmin` account-type contract and narrowly scoped platform-admin/rotation results in `Shared/Enums/AuthenticatedAccountType.cs`, `Shared/Responses/PlatformAdminIdentityResult.cs`, and `Shared/Responses/RotatedRefreshTokenResult.cs`; do not add an account-type database column.
- [ ] T006 [P] [Shared] Append the stable `PlatformAdminPasswordAlreadyConfigured` conflict without renumbering existing values in `Shared/Enums/ErrorCode.cs` and `Shared/Enums/ErrorMessage.cs`.
- [ ] T007 [P] [Domain] Define the narrow Identity boundary for normalized admin password sign-in and authenticated first-password setup in new `Domain/Services/IPlatformAdminIdentityService.cs`, and update `Domain/Services/IAccessTokenService.cs` and `Domain/Services/IRefreshTokenService.cs` to accept/return trusted account-type data while keeping raw tokens out of logs and persistence.
- [ ] T008 [Test] Add failing signer regression tests for explicit Teacher and PlatformAdmin `accountType` claims, unchanged standard claims/lifetime, and absence of community claims in `SprintLabs.Tests/Features/TeacherEmailAuthentication/AccessTokenServiceTests.cs`.
- [ ] T009 [Infrastructure/Application] Parameterize the existing signer in `Infrastructure/Services/AccessTokenService.cs` and update the existing teacher caller in `Application/Features/Accounts/TeacherAuthentication/TeacherLogin/TeacherLoginCommandHandler.cs` to pass `AuthenticatedAccountType.Teacher`; do not touch the legacy Google JWT implementation in `Infrastructure/Services/UserService.cs`.

**Checkpoint**: The existing access-token signer is account-type aware, teacher behavior is explicit and unchanged, and Domain/Application still do not reference Infrastructure.

---

## Phase 3: User Story 1 - Existing Platform Administrator Signs In with a Password (Priority: P1) — MVP

**Goal**: Allow an eligible persisted platform administrator to sign in with normalized username or email and receive the existing access/refresh credential formats without a community.

**Independent Test**: Sign in the same eligible administrator by mixed-case email and trimmed mixed-case username, call an existing protected admin operation, and verify no community/player/student/teacher records are required, selected, returned, or created.

### Tests for User Story 1 — write and run first

- [ ] T010 [P] [US1] [Infrastructure Test] Add failing `PlatformAdminIdentityService` tests for trim/Identity normalization, username and email lookup, ambiguous matches, confirmed-email policy, active/suspended state, password presence, generic unknown/wrong-password/non-admin/passwordless failures, lockout counting, current lockout, and successful-access-failure reset in new `SprintLabs.Tests/Features/PlatformAdminPasswordAuthentication/PlatformAdminIdentityServiceTests.cs`.
- [ ] T011 [P] [US1] [Application Test] Add failing handler tests for generic credential errors, trusted `PlatformAdmin` response data, UTC expirations, and reuse of the existing access/refresh services in new `SprintLabs.Tests/Features/PlatformAdminPasswordAuthentication/PlatformAdminLoginCommandHandlerTests.cs`.
- [ ] T012 [P] [US1] [API Test] Add failing route/model-binding/BaseResponse/Swagger contract tests for anonymous `POST /api/v{version}/Account/platform-admins/login`, its two-field request, generic 401 behavior, and documented lockout response in new `SprintLabs.Tests/Features/PlatformAdminPasswordAuthentication/PlatformAdminAuthenticationApiContractTests.cs`.
- [ ] T013 [P] [US1] [Integration Test] Add a failing end-to-end login test that uses both identifiers, validates JWT/refresh credentials against an existing admin-protected operation, and snapshots community/player/profile/license/teacher tables to prove no side effects in new `SprintLabs.Tests/Features/PlatformAdminPasswordAuthentication/PlatformAdminLoginIntegrationTests.cs`.

### Implementation for User Story 1

- [ ] T014 [P] [US1] [Application] Add the CQRS request/command/response contracts as one public type per file in new `Application/Features/Accounts/PlatformAdminAuthentication/PlatformAdminLogin/PlatformAdminLoginRequest.cs`, `PlatformAdminLoginCommand.cs`, and `PlatformAdminLoginResponse.cs`; accept only `identifier` and `password` and expose no community or client-supplied account type/admin flag.
- [ ] T015 [US1] [Application] Add `Application/Features/Accounts/PlatformAdminAuthentication/PlatformAdminLogin/PlatformAdminLoginValidator.cs` using the repository's existing validation/`GenericException` conventions (no new validation package), trim only the identifier, and route null/blank authentication input to the same safe public credential contract without echoing secrets.
- [ ] T016 [US1] [Infrastructure] Implement the login portion of new `Infrastructure/Services/PlatformAdminIdentityService.cs` with `UserManager.NormalizeName/NormalizeEmail`, ambiguity rejection, persisted `IsPlatformAdmin`, Active status, configured confirmed-email policy, `HasPasswordAsync`, `CheckPasswordSignInAsync(..., lockoutOnFailure: true)`, and a final current-state recheck before success; never query or create community/player/teacher data.
- [ ] T017 [US1] [Application] Implement new `Application/Features/Accounts/PlatformAdminAuthentication/PlatformAdminLogin/PlatformAdminLoginCommandHandler.cs` to call the validator/identity adapter, issue `AuthenticatedAccountType.PlatformAdmin` through `IAccessTokenService`, create the refresh credential through `IRefreshTokenService`, and map only the generic credential or safe lockout failures through existing `BaseResponse`/error conventions.
- [ ] T018 [P] [US1] [Infrastructure] Register `IPlatformAdminIdentityService` with `PlatformAdminIdentityService` in `Infrastructure/ServiceConfig.cs`; do not add a JWT, refresh, repository, or recovery registration.
- [ ] T019 [US1] [API] Add the thin anonymous platform-admin login action and only the Swagger response/auth metadata required for its contract in `API/Controllers/AccountController.cs`; preserve the existing `GoogleLogin` action, query parameter, response, and attributes byte-for-byte except for unavoidable adjacent additions.
- [ ] T020 [US1] [Authorization] Verify through `SprintLabs.Tests/Features/PlatformAdminPasswordAuthentication/PlatformAdminLoginIntegrationTests.cs` that the signed `userId` is accepted by the existing persisted-state check in `Application/Features/Admin/Communities/Common/AdminCommunityAuthorization.cs`; change that authorization file only if the test proves a compile/runtime incompatibility, and never authorize solely from returned/JWT `accountType`.
- [ ] T021 [US1] [Verification] Run all new US1 tests plus `SprintLabs.Tests/Features/TeacherEmailAuthentication/AccessTokenServiceTests.cs` and confirm the story's negative cases produce no access token, refresh token, or domain-record writes.

**Checkpoint**: US1 is independently usable as the MVP and existing teacher token issuance remains unchanged.

---

## Phase 4: User Story 2 - Google-Authenticated Administrator Configures a First Password (Priority: P1)

**Goal**: Let an authenticated, passwordless platform administrator configure only their own first Identity password, revoke all active refresh tokens, and continue using Google.

**Independent Test**: Authenticate a Google-only administrator, set a compliant password, verify no token is returned and old refresh credentials are revoked, then verify both password login and the unchanged Google flow still work.

### Tests for User Story 2 — write and run first

- [ ] T022 [P] [US2] [Application/API Test] Add failing handler and controller tests for bearer authentication, persisted-admin enforcement, self-only targeting from the signed `userId`, rejection of anonymous/non-admin users, omission/rejection of target-account fields, stable conflict, safe password-policy errors, success message, and no issued tokens in new `SprintLabs.Tests/Features/PlatformAdminPasswordAuthentication/SetPlatformAdminPasswordCommandHandlerTests.cs` and `SprintLabs.Tests/Features/PlatformAdminPasswordAuthentication/PlatformAdminAuthenticationApiContractTests.cs`.
- [ ] T023 [P] [US2] [Infrastructure Test] Add failing service tests proving `UserManager.AddPasswordAsync` semantics, no direct hash assignment, active/admin/passwordless rechecks, at-most-one concurrent success behavior, refresh revoke-all, and preservation of `IsPlatformAdmin`, `GoogleId`, Identity external logins, memberships, and profiles in `SprintLabs.Tests/Features/PlatformAdminPasswordAuthentication/PlatformAdminIdentityServiceTests.cs`.
- [ ] T024 [P] [US2] [Integration Test] Add the failing Google-login → set-password → password-login → Google-login journey, including pre-setup refresh revocation and no unrelated record writes, in new `SprintLabs.Tests/Features/PlatformAdminPasswordAuthentication/PlatformAdminFirstPasswordIntegrationTests.cs` without changing existing Google test expectations.

### Implementation for User Story 2

- [ ] T025 [P] [US2] [Application] Add the self-only CQRS contracts in new `Application/Features/Accounts/PlatformAdminAuthentication/SetPlatformAdminPassword/SetPlatformAdminPasswordRequest.cs`, `SetPlatformAdminPasswordCommand.cs`, and `SetPlatformAdminPasswordResponse.cs`; the HTTP request contains only `newPassword`, while the command receives the trusted authenticated user ID from the controller.
- [ ] T026 [US2] [Infrastructure] Implement first-password setup in `Infrastructure/Services/PlatformAdminIdentityService.cs` using a relational transaction and locked User row, persisted active-admin and `HasPasswordAsync` rechecks, `UserManager.AddPasswordAsync`, safe Identity policy error mapping, and existing `IRefreshTokenService` revoke-all before commit; return `PlatformAdminPasswordAlreadyConfigured` on an existing password/concurrent winner and do not alter Google/external-login data.
- [ ] T027 [US2] [Application] Implement new `Application/Features/Accounts/PlatformAdminAuthentication/SetPlatformAdminPassword/SetPlatformAdminPasswordCommandHandler.cs` to pass only the authenticated user ID/new password to the Identity boundary and return exactly `Password configured successfully. Please sign in again.` with no credential issuance.
- [ ] T028 [US2] [API] Add the `[Authorize]` set-password action, authenticated `userId` extraction, 200/400/401/403/409 Swagger metadata, and thin MediatR dispatch in `API/Controllers/AccountController.cs`; accept no target ID, username, email, role, account type, or admin flag.
- [ ] T029 [US2] [MySQL Test] Add opt-in concurrency coverage using `SprintLabs.Tests/Fixtures/MysqlDatabaseFixture.cs` in new `SprintLabs.Tests/Features/PlatformAdminPasswordAuthentication/PlatformAdminPasswordConcurrencyTests.cs` for two simultaneous setup calls and setup racing refresh rotation; require `SPRINTLABS_MYSQL_TEST_CONNECTION` and never fall back to a shared database.
- [ ] T030 [US2] [Verification] Run the US2 unit/API/integration tests and, when the safe MySQL connection exists, the setup concurrency cases; confirm the loser receives the stable conflict, every pre-setup session is revoked, and Google-linked state is unchanged.

**Checkpoint**: Existing Google-only admins can add a first password for themselves without gaining or changing any privilege or losing Google access.

---

## Phase 5: User Story 4 - Administrator Uses Shared Refresh and Logout Flows (Priority: P1)

**Goal**: Make platform-admin password sessions rotate and revoke through the existing shared refresh/logout endpoints while retaining current persisted-state authorization.

**Independent Test**: Rotate an admin login refresh token once, use the replacement access token on an admin-protected operation, log out the replacement, and prove neither old nor logged-out refresh credentials can rotate.

### Tests for User Story 4 — write and run first

- [ ] T031 [P] [US4] [Infrastructure/Application Test] Add failing account-aware rotation tests for active admin, PlatformAdmin precedence on dual-flag users, suspended/demoted/expired/reused tokens, trusted subject reconstruction, rotation hashing/replacement, and existing teacher behavior in `SprintLabs.Tests/Features/InviteOnlyTeacherAuthentication/RefreshLogoutRegressionTests.cs` and new `SprintLabs.Tests/Features/PlatformAdminPasswordAuthentication/PlatformAdminRefreshLogoutTests.cs`.
- [ ] T032 [P] [US4] [Integration Test] Add a failing login → refresh → protected-admin operation → logout → rejected-refresh journey, including no community claim assertions, in `SprintLabs.Tests/Features/PlatformAdminPasswordAuthentication/PlatformAdminRefreshLogoutTests.cs`.

### Implementation for User Story 4

- [ ] T033 [US4] [Infrastructure] Generalize `Infrastructure/Services/RefreshTokenService.cs` in place: retain its raw-token generation, SHA-256 hashing, expiry, conditional single-use revocation, replacement linkage, logout, and revoke-all rules; under the shared User-row transaction lock, recheck the submitted token and current Active eligible identity, derive trusted PlatformAdmin precedence over Teacher, and return `RotatedRefreshTokenResult` without trusting client account type.
- [ ] T034 [US4] [Application] Update `Application/Features/Accounts/TeacherAuthentication/RefreshToken/RefreshTokenCommandHandler.cs` to consume the account-aware rotation result and mint the next access token through the existing signer without a teacher-only identity reload; keep the route/request/response/error contract unchanged.
- [ ] T035 [US4] [Application/Infrastructure] Verify `Application/Features/Accounts/TeacherAuthentication/Logout/LogoutCommandHandler.cs` already delegates account-neutrally to `IRefreshTokenService` and make no administrator branch; implement only a failing-test-driven correction in that file or `Infrastructure/Services/RefreshTokenService.cs` if the submitted hash is not revoked by the shared path.
- [ ] T036 [US4] [Verification] Run platform-admin refresh/logout tests and all existing `SprintLabs.Tests/Features/InviteOnlyTeacherAuthentication/RefreshLogoutRegressionTests.cs` cases, confirming unchanged request/response JSON and teacher rotation behavior.

**Checkpoint**: Admin and teacher sessions share one hashing, rotation, expiry, revocation, and logout implementation.

---

## Phase 6: User Story 5 - Google SSO and Trusted Provisioning Remain Unchanged (Priority: P1)

**Goal**: Prove the feature is additive: Google behavior remains unchanged and no public input can manufacture platform-admin status.

**Independent Test**: Run unchanged Google suites before and after first-password setup and attempt login/setup with admin-like request values on a non-admin account.

### Tests and verification for User Story 5

- [ ] T037 [P] [US5] [Integration Test] Add coverage that password setup preserves `User.GoogleId`, Identity external-login rows, Player Google linkage, and the existing Google response/side effects in new `SprintLabs.Tests/Features/PlatformAdminPasswordAuthentication/PlatformAdminGoogleCompatibilityTests.cs`; do not alter expectations in existing Google suites.
- [ ] T038 [P] [US5] [API/Security Test] Extend `SprintLabs.Tests/Features/PlatformAdminPasswordAuthentication/PlatformAdminAuthenticationApiContractTests.cs` to prove login/setup DTOs cannot select another account or bind `IsPlatformAdmin`, account type, email domain, Google identity, community, registration, invitation, or promotion inputs, and that no public admin-provisioning route exists.
- [ ] T039 [US5] [Source Audit] Inspect the final diff for `Application/Features/Accounts/GoogleAuthenticate/`, `Application/Features/Accounts/Common/`, `Domain/Services/IGoogleAuthenticationService.cs`, `Infrastructure/Services/GoogleAuthenticationService.cs`, `Infrastructure/Services/UserService.cs`, `Shared/Responses/GoogleUserResponse.cs`, and `Shared/Responses/LoginResponse.cs`; revert no user work, but remove any feature-introduced Google behavioral change and keep `API/Controllers/AccountController.cs`/`Infrastructure/ServiceConfig.cs` edits to adjacent additions only.
- [ ] T040 [US5] [Verification] Run unchanged `SprintLabs.Tests/Features/B2CPlayerProfileSupport/B2CLoginCompatibilityTests.cs`, `SprintLabs.Tests/Features/StudentLicenseActivation/StudentLicenseActivationOnLoginTests.cs`, and `SprintLabs.Tests/Features/OwnerTeacherManagement/PendingTeacherActivationTests.cs` together with `PlatformAdminGoogleCompatibilityTests.cs`, comparing responses and side effects to the pre-change baseline.

**Checkpoint**: Google SSO remains supported before and after password setup, and persisted backend `IsPlatformAdmin` remains the only authorization source.

---

## Phase 7: User Story 3 - Platform Administrator Recovers a Password (Priority: P2)

**Goal**: Extend the existing generic forgot/reset flow to eligible password-enabled platform admins, revoke sessions after reset, and preserve Google/admin/community state.

**Independent Test**: Compare the forgot response for an eligible admin and unknown identifier, complete an Identity reset, verify old refresh credentials fail and no automatic login occurs, then verify Google still works.

### Tests for User Story 3 — write and run first

- [ ] T041 [P] [US3] [Application Test] Extend `SprintLabs.Tests/Features/InviteOnlyTeacherAuthentication/PasswordRecoveryHandlerTests.cs` with failing byte-equivalent public forgot responses for eligible admin versus unknown/non-admin/passwordless/suspended/unconfirmed/cooldown/delivery-failure cases, while preserving every existing teacher expectation.
- [ ] T042 [P] [US3] [Infrastructure/Integration Test] Add failing admin recovery tests for normalized identifier eligibility, existing Identity token provider/link/email path, valid reset, invalid/expired/used/wrong-user token, password policy errors, no auto-login, refresh revoke-all, and preservation of `IsPlatformAdmin`, Google/external-login, and community state in new `SprintLabs.Tests/Features/PlatformAdminPasswordAuthentication/PlatformAdminPasswordRecoveryTests.cs`.
- [ ] T043 [P] [US3] [MySQL Test] Extend `SprintLabs.Tests/Features/PlatformAdminPasswordAuthentication/PlatformAdminPasswordConcurrencyTests.cs` with the opt-in reset-versus-refresh race, proving no pre-reset original or replacement refresh credential can renew after successful reset.

### Implementation for User Story 3

- [ ] T044 [US3] [Infrastructure] Extend `Infrastructure/Services/TeacherIdentityService.cs` `CreatePasswordResetAsync` eligibility in place to include a normalized, unambiguous Active persisted platform admin with an Identity password, deliverable email, and current confirmed-email policy; retain the existing cooldown claim, Identity token provider, frontend link builder, email service, generic public response, and teacher branch without requiring a community.
- [ ] T045 [US3] [Infrastructure] Extend `Infrastructure/Services/TeacherIdentityService.cs` `ResetPasswordAsync` in place for eligible admins using the existing Identity reset token and password policy; coordinate the locked User-row transaction with refresh rotation, revoke all active refresh credentials before commit, and preserve `IsPlatformAdmin`, `GoogleId`, Identity external logins, memberships, and unrelated profiles.
- [ ] T046 [US3] [Application/API] Keep `Application/Features/Accounts/TeacherAuthentication/ForgotPassword/ForgotPasswordCommandHandler.cs`, `Application/Features/Accounts/TeacherAuthentication/ResetPassword/ResetPasswordCommandHandler.cs`, and their existing actions in `API/Controllers/AccountController.cs` on the same routes/contracts; make only test-required account-neutral result handling changes and add no administrator-specific recovery endpoint/provider/email method.
- [ ] T047 [US3] [Verification] Run all password-recovery tests, compare eligible-admin and unknown forgot responses, verify a successful reset emits no login token and invalidates prior refresh credentials, and run the safe MySQL reset race when configured.

**Checkpoint**: Platform admins recover through the one existing Identity/email flow, while teacher recovery and enumeration resistance remain unchanged.

---

## Phase 8: Documentation, Security, and Full Verification

**Purpose**: Synchronize published contracts, confirm no schema drift or secret exposure, and run the complete regression suite.

- [ ] T048 [P] [Documentation] Reconcile implemented routes, auth requirements, generic failures, lockout, password-policy errors, stable conflict, and frontend states in `specs/017-platform-admin-password-authentication/api.md`, `specs/017-platform-admin-password-authentication/frontend.md`, and `specs/017-platform-admin-password-authentication/contracts/platform-admin-password-authentication-api.md`; retain the Google endpoint documentation unchanged.
- [ ] T049 [P] [API/Swagger] Inspect generated Swagger for the new login and authenticated set-password actions from `API/Controllers/AccountController.cs`, confirming request schemas exclude target/admin/account-type fields and responses document 200/400/401/403/409/423 as applicable; make no global Swagger/package change unless the generated document proves one is necessary.
- [ ] T050 [Security] Audit changed production/test code for logging or persistence of passwords and raw access/refresh/reset tokens, confirm only refresh hashes persist, confirm UTC timestamps, and verify no direct `PasswordHash` assignment or Google login unlink call using targeted searches across `API/`, `Application/`, `Domain/`, `Infrastructure/`, `Shared/`, and `SprintLabs.Tests/`.
- [ ] T051 [Schema] Run `dotnet ef migrations list --project Infrastructure --startup-project API` and inspect `git diff -- Infrastructure/DataAccess/ApplicationDbContext.cs Infrastructure/Migrations`; when T003 confirmed the current schema, verify there is no feature-017 migration or snapshot/model diff and investigate rather than accepting any accidental schema change.
- [ ] T052 [Verification] Run `dotnet build SprintLabs.sln --no-restore --nologo`, then the complete `dotnet test SprintLabs.sln --no-build --nologo`; resolve feature-caused failures without changing established Google expectations and report any pre-existing failures separately.
- [ ] T053 [Verification] Re-run the three frozen Google suites and the complete `PlatformAdminPasswordAuthentication` test filter after the full suite, then execute the API smoke checks in `specs/017-platform-admin-password-authentication/quickstart.md` for login, protected authorization, set-password, recovery, refresh, logout, no side effects, and unchanged Google behavior.
- [ ] T054 [Verification] When `SPRINTLABS_MYSQL_TEST_CONNECTION` points to a dedicated safeguarded database, run every case in `PlatformAdminPasswordConcurrencyTests.cs`; otherwise record the concurrency suite as not run (not passed) in `specs/017-platform-admin-password-authentication/quickstart.md` and never substitute a shared/development database.

---

## Dependencies and Execution Order

### Phase dependencies

- **Phase 1 — Inspection**: Starts immediately; T002 uses the map from T001, T003 may run in parallel, and T004 establishes the baseline after inspection.
- **Phase 2 — Foundation**: Depends on Phase 1 and blocks implementation. T005-T007 can be prepared in parallel; T008 must precede T009.
- **US1 (Phase 3)**: Depends on Phase 2 and is the MVP.
- **US2 (Phase 4)**: Depends on Phase 2 contracts and shares `PlatformAdminIdentityService` with US1; coordinate T016/T026 if implemented concurrently. Its complete Google-to-password journey uses US1 login.
- **US4 (Phase 5)**: Core rotation work depends on Phase 2; its end-to-end independent test also needs US1 to issue an admin refresh credential.
- **US5 (Phase 6)**: Source/API audits can start after Phase 2; its full before/after-password test depends on US2.
- **US3 (Phase 7)**: Depends only on Phase 2 for shared token contracts and may be developed alongside other stories, but is scheduled after P1 delivery.
- **Phase 8 — Final verification**: Depends on every selected user story.

### Within each user story

1. Add the story's listed tests and confirm expected failures.
2. Add or extend shared/domain contracts before implementations.
3. Implement Infrastructure Identity/token behavior before Application handlers.
4. Add thin controller wiring and Swagger metadata after commands/handlers compile.
5. Run the focused story tests at its checkpoint before continuing.

### Parallel opportunities

- T003 can run while T001-T002 inspect contracts; T005-T007 target separate projects/files.
- US1 test tasks T010-T013 target separate test files and can be authored together before implementation.
- US2 tasks T022-T024 and US4 tasks T031-T032 can be authored in parallel after the foundational contracts stabilize.
- US3 recovery tests/implementation touch the legacy recovery service, while US5 Google audit/tests touch separate paths.
- T048 and T049 can run in parallel; T050-T054 follow the integrated implementation.

## Parallel Examples

### US1 test-first batch

```text
T010 PlatformAdminIdentityService tests
T011 PlatformAdminLoginCommandHandler tests
T012 Account controller/API contract tests
T013 End-to-end login/authorization/no-side-effect tests
```

### P1 work after the foundation

```text
Developer A: US1 login slice
Developer B: US2 first-password tests/contracts (coordinate shared Identity service file)
Developer C: US4 account-aware refresh tests/service
Developer D: US5 frozen-Google contract and source audit
```

## Implementation Strategy

### MVP first

1. Complete Phases 1 and 2.
2. Complete US1 (Phase 3).
3. Stop and run T021 to validate password login, protected authorization, generic failures, and no community/profile side effects.
4. Deliver/demo US1 only if an incremental release is desired.

### Incremental delivery

1. Foundation → trusted shared signer and identity contracts.
2. US1 → platform-admin password login (MVP).
3. US2 → authenticated first-password setup.
4. US4 → shared rotation/logout completeness.
5. US5 → frozen Google/provisioning compatibility proof.
6. US3 → shared forgot/reset recovery eligibility.
7. Phase 8 → docs, security/schema gates, full suite, and optional MySQL concurrency proof.

## Notes

- No public administrator registration, invitation, promotion, or account-targeting API is introduced.
- No community membership is required or selected for platform administrators.
- No second JWT, refresh-token, logout, reset-token, or email implementation is permitted.
- `IsPlatformAdmin` from current persisted backend state remains authoritative; `accountType` is signed response/session context, not the authorization source.
- Google authentication files and expectations are a behavioral freeze boundary.
- The inspected model requires no migration; an implementation-time model diff is evidence to investigate, not permission to generate one automatically.

