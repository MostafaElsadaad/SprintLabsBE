# Tasks: Firebase Player Authentication

**Input**: Design documents from `specs/016-firebase-player-auth/`

**Prerequisites**: [plan.md](./plan.md), [spec.md](./spec.md), [research.md](./research.md), [data-model.md](./data-model.md), [HTTP contract](./contracts/firebase-player-authentication-api.md), [quickstart.md](./quickstart.md)

**Tests**: Automated tests are required by the feature specification. Within each user-story phase, write the listed tests first, run them to confirm they fail for the missing behavior, then implement the story.

**Organization**: Tasks are grouped by user story so each journey can be implemented and verified as an incremental vertical slice.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel with other marked tasks after its phase prerequisites are satisfied because it targets different files and does not depend on their incomplete implementation.
- **[Story]**: Maps the task to User Story 1-5 from `spec.md`.
- Every task names the exact file or repository path it changes or validates.

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Add the approved provider dependency and non-secret configuration surface before feature code.

- [x] T001 Update `Infrastructure/Infrastructure.csproj` to add FirebaseAdmin 3.6.0 and raise the direct Google.Apis.Auth reference to 1.75.0 without adding packages to other projects
- [x] T002 [P] Create `Shared/Options/FirebaseAuthenticationOptions.cs` with the required ProjectId configuration contract
- [x] T003 [P] Add the non-secret `Authentication:Firebase:ProjectId` configuration shape to `API/appsettings.json` and `API/appsettings.Development.json` without credential paths or service-account data
- [x] T004 Restore and inspect dependencies with `SprintLabs.sln` and `Infrastructure/Infrastructure.csproj`, resolving all package downgrade or compatibility warnings before continuing

**Checkpoint**: FirebaseAdmin restores only through Infrastructure and tracked configuration contains no secret material.

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Establish the neutral contracts, schema, repository contracts, and shared workflow boundaries required by every story.

**Critical**: No user-story implementation starts until this phase and its migration review are complete.

- [x] T005 [P] Create the SDK-neutral verified identity DTO in `Shared/Responses/FirebaseUserResponse.cs`
- [x] T006 [P] Extend the internal identity projection with nullable FirebaseUid in `Shared/Responses/UserIdentityResponse.cs`
- [x] T007 Create `Domain/Services/IFirebaseAuthenticationService.cs` returning `FirebaseUserResponse` with cancellation support and no Firebase SDK types
- [x] T008 Extend `Domain/Services/IUserService.cs` with conflict-safe Firebase user resolution using the neutral response
- [x] T009 [P] Extend `Domain/Repositories/IPlayerRepository.cs` for nullable Google ID lookup and normalized verified-email fallback without adding a Firebase repository
- [x] T010 [P] Extend `Domain/Services/ICommunityLoginActivationService.cs` with the Firebase-only eligible Teacher activation operation while retaining student activation
- [x] T011 [P] Add nullable `FirebaseUid` to `Infrastructure/DataAccess/User.cs` independently from GoogleId
- [x] T012 [P] Make `GoogleId` nullable in `Domain/Models/Player.cs` without changing progression defaults
- [x] T013 Configure nullable unique User.FirebaseUid and nullable unique Player.GoogleId mappings in `Infrastructure/DataAccess/ApplicationDbContext.cs`
- [x] T014 Generate the `AddFirebasePlayerAuthentication` migration into `Infrastructure/Migrations/` using the API startup project and without editing any older migration
- [x] T015 Inspect and correct the new timestamped migration and `Infrastructure/Migrations/ApplicationDbContextModelSnapshot.cs` so Up preserves legacy data and Down neither deletes players nor copies FirebaseUid into GoogleId
- [x] T016 [P] Create the provider-neutral completion input in `Application/Features/Accounts/Common/ExternalPlayerLoginContext.cs`
- [x] T017 Create `Application/Features/Accounts/Common/IExternalPlayerLoginWorkflow.cs` for shared Google/Firebase player-login completion

**Checkpoint**: Domain/Application compile without Firebase namespaces; schema constraints and provider-neutral workflow contracts are ready.

---

## Phase 3: User Story 1 - Register or Sign In a Firebase Player (Priority: P1) MVP

**Goal**: A valid first Firebase login creates one SprintLabs user/player with current defaults, and repeated login returns the same records and existing `BaseResponse<LoginResponse>`.

**Independent Test**: Mock a valid Firebase identity, submit first and repeated login with no community/license state, and verify one User, one Player, stable IDs, default progression, and all required response fields.

### Tests for User Story 1

- [x] T018 [P] [US1] Write failing handler tests using a mocked `IFirebaseAuthenticationService` for valid identity, missing UID/email, and workflow delegation in `SprintLabs.Tests/Features/FirebasePlayerAuthentication/FirebaseAuthenticationCommandHandlerTests.cs`
- [x] T019 [P] [US1] Write failing workflow tests for first player creation, repeated login, B2C login, defaults, claims, and LoginResponse mapping in `SprintLabs.Tests/Features/FirebasePlayerAuthentication/ExternalPlayerLoginWorkflowTests.cs`
- [x] T020 [P] [US1] Write failing controller tests for the JSON body, HTTP 200, and `BaseResponse<LoginResponse>` wrapper in `SprintLabs.Tests/Features/FirebasePlayerAuthentication/FirebaseAuthenticationControllerTests.cs`
- [x] T021 [P] [US1] Write failing provider-independent persistence tests for nullable FirebaseUid/GoogleId and one-user/one-player repeat behavior in `SprintLabs.Tests/Features/FirebasePlayerAuthentication/FirebaseAuthenticationPersistenceTests.cs`

### Implementation for User Story 1

- [x] T022 [P] [US1] Implement one-time FirebaseApp/FirebaseAuth use, ADC, configured project ID, token verification, neutral claim mapping, and safe basic 401 conversion in `Infrastructure/Services/FirebaseAuthenticationService.cs`
- [x] T023 [P] [US1] Implement first-user creation and exact FirebaseUid repeat resolution with Identity normalization and existing User defaults in `Infrastructure/Services/UserService.cs`
- [x] T024 [P] [US1] Implement suspension enforcement, UserId-first player creation/reuse, default preservation, JWT claims, SprintLabs token issuance, and LoginResponse mapping in `Application/Features/Accounts/Common/ExternalPlayerLoginWorkflow.cs`
- [x] T025 [P] [US1] Create the `idToken` body contract in `Application/Features/Accounts/FirebaseAuthenticate/FirebaseAuthenticationRequest.cs`
- [x] T026 [P] [US1] Create the MediatR command in `Application/Features/Accounts/FirebaseAuthenticate/FirebaseAuthenticationCommand.cs`
- [x] T027 [US1] Implement verification, required-identity validation, Firebase user resolution, and shared-workflow delegation in `Application/Features/Accounts/FirebaseAuthenticate/FirebaseAuthenticationCommandHandler.cs`
- [x] T028 [US1] Add anonymous `POST firebase-login` without changing `google-login` in `API/Controllers/AccountController.cs`
- [x] T029 [US1] Register the shared Application workflow in `Application/ServiceConfig.cs`
- [x] T030 [US1] Bind/validate Firebase options and register singleton Firebase app/auth service instances in `Infrastructure/ServiceConfig.cs`
- [x] T031 [US1] Run the US1 tests in `SprintLabs.Tests/Compass.Tests.csproj` and build `SprintLabs.sln`, fixing only Firebase first/repeat-login failures before the MVP checkpoint

**Checkpoint**: User Story 1 is deployable as a B2C Firebase registration/login MVP with no Firebase calls from automated tests.

---

## Phase 4: User Story 2 - Safely Link an Existing Google Player (Priority: P1)

**Goal**: Firebase links an eligible legacy Google/email identity to the existing user/player without duplicates, progression loss, provider-field corruption, or unsafe merges.

**Independent Test**: Seed legacy Google and verified-email cases, log in through mocked Firebase identities, and verify ordered resolution, stable user/player IDs, unchanged progression, FirebaseUid attachment, and 409/no mutation for conflicts.

### Tests for User Story 2

- [x] T032 [P] [US2] Write failing ordered-resolution tests for FirebaseUid, verified Google provider ID, verified normalized email, unverified email, and conflicting candidates in `SprintLabs.Tests/Features/FirebasePlayerAuthentication/FirebaseUserIdentityResolutionTests.cs`
- [x] T033 [P] [US2] Extend `SprintLabs.Tests/Features/FirebasePlayerAuthentication/ExternalPlayerLoginWorkflowTests.cs` with failing legacy Google/email player attachment, cross-user rejection, nullable GoogleId, and progression-preservation cases
- [x] T034 [P] [US2] Extend `SprintLabs.Tests/Features/FirebasePlayerAuthentication/FirebaseAuthenticationPersistenceTests.cs` with failing concurrent FirebaseUid/email/user/player uniqueness cases

### Implementation for User Story 2

- [x] T035 [P] [US2] Implement full FirebaseUid-then-GoogleId-then-verified-NormalizedEmail user selection, all-candidate preflight, safe identity attachment, and 409 mapping in `Infrastructure/Services/UserService.cs`
- [x] T036 [P] [US2] Implement nullable Google lookup and normalized email legacy lookup with ownership-safe persistence behavior in `Infrastructure/Repositories/PlayerRepository.cs`
- [x] T037 [US2] Add verified legacy player fallback, ambiguity/ownership conflicts, identity-field attachment, and uniqueness-race re-read behavior to `Application/Features/Accounts/Common/ExternalPlayerLoginWorkflow.cs`
- [x] T038 [US2] Run US2 identity/linking tests in `SprintLabs.Tests/Compass.Tests.csproj` and confirm the migration constraints in `Infrastructure/Migrations/ApplicationDbContextModelSnapshot.cs` enforce one Firebase user and one player

**Checkpoint**: User Story 2 safely upgrades legacy accounts without changing progression or placing Firebase UID in a Google field.

---

## Phase 5: User Story 3 - Preserve Login-Time Business Rules (Priority: P2)

**Goal**: Firebase login enforces suspension and applies eligible student/teacher activation while keeping B2C access, invitation rules, roles, and seat counters intact.

**Independent Test**: Run mocked Firebase login for suspended, B2C, pending-student, valid pending-teacher, legacy pending-teacher, invalid-invitation, repeated-login, and role-conflict cases and verify exact state/counter outcomes.

### Tests for User Story 3

- [x] T039 [P] [US3] Write failing suspended-user, B2C, pending-student, repeated-login, and role-conflict tests in `SprintLabs.Tests/Features/FirebasePlayerAuthentication/FirebaseBusinessBehaviorTests.cs`
- [x] T040 [P] [US3] Write failing valid/legacy/expired/revoked/superseded/mismatched Teacher activation and UsedTeachers invariance tests in `SprintLabs.Tests/Features/FirebasePlayerAuthentication/FirebaseLoginActivationTests.cs`

### Implementation for User Story 3

- [x] T041 [P] [US3] Implement verified-email eligible Teacher membership/invitation activation with one save and zero seat-counter mutation in `Infrastructure/Services/CommunityLoginActivationService.cs`
- [x] T042 [P] [US3] Integrate existing student activation for both providers and the explicit Firebase-only Teacher activation policy in `Application/Features/Accounts/Common/ExternalPlayerLoginWorkflow.cs`
- [x] T043 [US3] Run US3 tests plus existing student/invitation tests in `SprintLabs.Tests/Compass.Tests.csproj`, confirming idempotency and no community mutation for ordinary B2C login

**Checkpoint**: User Story 3 preserves SaaS ownership and activation invariants while delivering the specified Firebase login behavior.

---

## Phase 6: User Story 4 - Reject Untrusted or Conflicting Identities (Priority: P2)

**Goal**: Invalid tokens return safe 401 responses, identity/player conflicts return 409, concurrency cannot create duplicates, and no Firebase detail/token leaks publicly or to normal logs.

**Independent Test**: Exercise every invalid-token and identity-conflict category through mocks/provider-neutral persistence tests and verify status, generic message, no mutation/token issuance, and no raw token/provider text.

### Tests for User Story 4

- [x] T044 [P] [US4] Extend `SprintLabs.Tests/Features/FirebasePlayerAuthentication/FirebaseAuthenticationCommandHandlerTests.cs` with failing blank, malformed, expired, revoked, wrong-project, provider-failure, and incomplete-identity 401 cases using only service mocks
- [x] T045 [P] [US4] Extend `SprintLabs.Tests/Features/FirebasePlayerAuthentication/FirebaseUserIdentityResolutionTests.cs` with the complete UID/Google/email disagreement matrix and different-existing-FirebaseUid/GoogleId cases
- [x] T046 [P] [US4] Extend `SprintLabs.Tests/Features/FirebasePlayerAuthentication/FirebaseAuthenticationPersistenceTests.cs` with failing uniqueness-race re-read and no-partial-mutation assertions
- [x] T047 [P] [US4] Extend `SprintLabs.Tests/Features/FirebasePlayerAuthentication/FirebaseAuthenticationControllerTests.cs` with safe 401/403/409 envelope and token/provider-detail non-disclosure assertions

### Implementation for User Story 4

- [x] T048 [P] [US4] Harden `Infrastructure/Services/FirebaseAuthenticationService.cs` to call revoked-token verification with cancellation and convert all expected Firebase/argument/claim-shape failures to safe GenericException responses before global middleware
- [x] T049 [P] [US4] Harden uniqueness-failure re-query, multi-candidate detection, and no-overwrite/no-partial-link behavior in `Infrastructure/Services/UserService.cs`
- [x] T050 [P] [US4] Harden player ambiguity, cross-user ownership, uniqueness-race recovery, and controlled 409 behavior in `Infrastructure/Repositories/PlayerRepository.cs` and `Application/Features/Accounts/Common/ExternalPlayerLoginWorkflow.cs`
- [x] T051 [US4] Run US4 tests in `SprintLabs.Tests/Compass.Tests.csproj` and scan `Infrastructure/`, `Application/`, `API/`, and test output to confirm raw tokens, Firebase exceptions, credentials, and SDK types outside Infrastructure are absent

**Checkpoint**: User Story 4 fails closed, creates no conflicting data, and exposes no provider-sensitive details.

---

## Phase 7: User Story 5 - Preserve Google Login and User-Owned Profile Updates (Priority: P3)

**Goal**: Google login keeps its public/current business behavior while both profile update routes select the player exclusively through the internal JWT `userId`.

**Independent Test**: Run the existing Google B2C/student/teacher regressions, then use Google- and Firebase-shaped SprintLabs claims to update profiles and verify only internal UserId controls ownership.

### Tests for User Story 5

- [x] T052 [P] [US5] Update `SprintLabs.Tests/Features/B2CPlayerProfileSupport/B2CLoginCompatibilityTests.cs` with failing shared-workflow integration assertions while preserving the current Google LoginResponse
- [x] T053 [P] [US5] Update `SprintLabs.Tests/Features/StudentLicenseActivation/StudentLicenseActivationOnLoginTests.cs` with failing shared-workflow Google student-activation regression coverage
- [x] T054 [P] [US5] Update `SprintLabs.Tests/Features/OwnerTeacherManagement/PendingTeacherActivationTests.cs` to prove Google login still leaves pending Teacher memberships unchanged after workflow extraction
- [x] T055 [P] [US5] Write failing internal-UserId, suspended, validation, not-found, and other-user protection tests in `SprintLabs.Tests/Features/AccountProfile/UpdateProfileCommandHandlerTests.cs`
- [x] T056 [P] [US5] Write failing controller claim tests for internal `userId`, missing/unparseable userId, and subject-only tokens in `SprintLabs.Tests/Features/AccountProfile/AccountControllerProfileTests.cs`

### Implementation for User Story 5

- [x] T057 [P] [US5] Refactor `Application/Features/Accounts/GoogleAuthenticate/GoogleAuthenticationCommandHandler.cs` to delegate provider-neutral completion to the shared workflow while preserving verification, Google user resolution, claims, response fields, and Firebase-disabled Teacher activation policy
- [x] T058 [P] [US5] Replace server-only GoogleId with internal UserId in `Application/Features/Accounts/UpdateProfile/UpdateProfileCommand.cs`
- [x] T059 [P] [US5] Resolve the player by UserId and preserve validation, suspension, not-found, update, and BaseResponse behavior in `Application/Features/Accounts/UpdateProfile/UpdateProfileCommandHandler.cs`
- [x] T060 [US5] Change only the legacy profile action's ownership extraction to the `userId` claim while retaining the Firebase and Google public routes in `API/Controllers/AccountController.cs`
- [x] T061 [US5] Run all Google, student, teacher, B2C, and profile regression tests in `SprintLabs.Tests/Compass.Tests.csproj` and compare the existing Google API shape against `specs/016-firebase-player-auth/contracts/firebase-player-authentication-api.md`

**Checkpoint**: User Story 5 proves backward compatibility and provider-independent profile ownership.

---

## Phase 8: Polish & Cross-Cutting Verification

**Purpose**: Reconcile documentation with the completed code, inspect generated artifacts, and run repository-wide quality/security gates.

- [x] T062 [P] Update final endpoint, auth, request, success, 401/403/409, and usage details in `specs/016-firebase-player-auth/api.md`
- [x] T063 [P] Update Unity actions, fields, loading/empty/error states, visibility, retry, and token-handling guidance in `specs/016-firebase-player-auth/frontend.md`
- [x] T064 [P] Reconcile actual response/error/migration behavior with `specs/016-firebase-player-auth/contracts/firebase-player-authentication-api.md` and `specs/016-firebase-player-auth/quickstart.md`
- [x] T065 Restore and list the final dependency graph using `SprintLabs.sln` and `Infrastructure/Infrastructure.csproj`, confirming FirebaseAdmin 3.6.0, Google.Apis.Auth 1.75.0, and no downgrade warnings
- [x] T066 Inspect the final new files in `Infrastructure/Migrations/` and `Infrastructure/Migrations/ApplicationDbContextModelSnapshot.cs`, confirming no old migration, unrelated schema, progression default, or provider identity was modified
- [x] T067 Run `dotnet build SprintLabs.sln` and resolve all compilation warnings/errors introduced by feature 016 without unrelated refactors
- [x] T068 Run focused Firebase, Google, student, teacher, B2C, and profile tests in `SprintLabs.Tests/Compass.Tests.csproj`
- [x] T069 Run `dotnet test SprintLabs.sln`, documenting any unavailable external MySQL prerequisite separately from provider-independent failures
- [ ] T070 Execute `specs/016-firebase-player-auth/quickstart.md` against a configured development Firebase project and complete the credential, raw-token, namespace-boundary, wrong-project, Google-regression, migration, and profile-ownership security inspection

---

## Dependencies & Execution Order

### Phase Dependencies

- **Phase 1 Setup**: Starts immediately.
- **Phase 2 Foundational**: Depends on Phase 1; blocks all user stories.
- **Phase 3 US1**: Depends on Phase 2 and is the MVP.
- **Phase 4 US2**: Depends on the US1 Firebase endpoint/workflow but is independently testable with seeded legacy identities.
- **Phase 5 US3**: Depends on US1's shared workflow; it does not require US2 linking cases to test activation for an already-resolved user.
- **Phase 6 US4**: Depends on US1 and US2 so it can harden the complete token/linking/conflict surface.
- **Phase 7 US5**: Depends on US1's shared workflow; run after US3/US4 to perform the final Google/profile compatibility integration.
- **Phase 8 Polish**: Depends on every selected story.

### User Story Dependency Graph

```text
Setup -> Foundational -> US1 (MVP)
                         |\
                         | +-> US2 -> US4
                         |
                         +----> US3
                         |
                         `----> US5

US2 + US3 + US4 + US5 -> Polish
```

### Within Each User Story

- Write the story's tests first and confirm they fail for the missing behavior.
- Complete neutral contracts/models before services.
- Complete services/workflow before controller integration.
- Run focused tests at the phase checkpoint.
- Do not move a task into an earlier story if doing so would make that earlier increment unsafe.

## Parallel Opportunities

- In Setup, T002 and T003 can run together after T001 is understood.
- In Foundational, neutral DTO/interface/model tasks marked [P] can run together; EF mapping/migration remains ordered after models.
- US1 test files T018-T021 can run in parallel, then T022-T026 can be split by layer before handler/controller/DI integration.
- US2 identity, workflow, and persistence tests T032-T034 can run in parallel; T035 and T036 can be implemented in parallel before T037.
- US3 student/suspension tests T039 and teacher tests T040 can run in parallel; activation service and workflow integration can be split across T041/T042.
- US4 security test files T044-T047 can run in parallel; provider/user/player hardening T048-T050 targets separate primary files.
- US5 regression/profile test files T052-T056 can run in parallel; Google handler, command, and handler work T057-T059 targets separate files before controller integration.
- Final documentation tasks T062-T064 can run in parallel.

## Parallel Example: User Story 1

```text
Task T018: Handler tests in FirebaseAuthenticationCommandHandlerTests.cs
Task T019: Workflow tests in ExternalPlayerLoginWorkflowTests.cs
Task T020: Controller tests in FirebaseAuthenticationControllerTests.cs
Task T021: Persistence/model tests in FirebaseAuthenticationPersistenceTests.cs
```

## Parallel Example: User Story 2

```text
Task T032: User identity resolution tests
Task T033: Legacy player workflow tests
Task T034: Concurrency/persistence tests
```

## Parallel Example: User Story 3

```text
Task T039: Suspension/B2C/student behavior tests
Task T040: Teacher invitation/membership activation tests
```

## Parallel Example: User Story 4

```text
Task T044: Invalid token handler tests
Task T045: Complete identity conflict matrix tests
Task T046: Persistence race/no-partial-mutation tests
Task T047: Public error-envelope disclosure tests
```

## Parallel Example: User Story 5

```text
Task T052: Google B2C response regression
Task T053: Google student activation regression
Task T054: Google pending Teacher regression
Task T055: Profile handler ownership tests
Task T056: Profile controller claim tests
```

## Implementation Strategy

### MVP First

1. Complete Setup.
2. Complete Foundational schema/contracts.
3. Complete US1.
4. Stop and verify first/repeated Firebase B2C login with no community/license prerequisite.
5. Do not deploy the MVP unless invalid-token handling in the US1 implementation is safely mapped and its listed tests pass.

### Incremental Delivery

1. **US1**: New/repeated Firebase B2C login.
2. **US2**: Safe legacy Google/email linking and progression preservation.
3. **US3**: Suspension, student activation, and Firebase-only eligible Teacher activation.
4. **US4**: Exhaustive invalid/conflict/concurrency hardening.
5. **US5**: Google workflow regression and internal-user profile ownership.
6. **Polish**: Full documentation, migration, build, test, manual Firebase, and security gates.

### Parallel Team Strategy

After Setup/Foundation:

- One implementer owns US1 because it establishes the shared workflow.
- After US1, separate implementers may take US2, US3, and US5 using their isolated test files.
- US4 follows US2 because it validates the complete linking conflict matrix.
- Reconcile shared workflow and `AccountController.cs` changes before the final test phase.

## Notes

- `[P]` means the task can run concurrently only after its stated phase prerequisites.
- `[US1]` through `[US5]` map directly to specification user stories.
- No automated test may initialize FirebaseApp, resolve ADC, or call Firebase.
- Do not edit historical migrations.
- Do not store Firebase UID in either User.GoogleId or Player.GoogleId.
- Do not change the Google endpoint's public request/response behavior.
- Keep one public class per file and follow the existing Accounts feature folders.
- Use existing repositories/services before adding abstractions.
- Commit after each task or cohesive task group; stop at every checkpoint for focused verification.
