# Tasks: User Identity Foundation

**Input**: Design documents from `specs/001-user-identity-foundation/`

**Prerequisites**: [plan.md](./plan.md), [spec.md](./spec.md), [research.md](./research.md), [data-model.md](./data-model.md), [contracts/users-api.openapi.yaml](./contracts/users-api.openapi.yaml), [quickstart.md](./quickstart.md)

**Tests**: Implementation validation belongs in Phase 3. Run `dotnet build SprintLabs.sln` after implementation, and run `dotnet test SprintLabs.sln` when the existing test project can execute without new external infrastructure.

**Organization**: Split into three implementation phases requested by the user. Phase 1 is database/entities/migration only. Phase 2 updates Google login and JWT behavior. Phase 3 adds `/api/users/me` endpoints and tests.

## Format: `[ID] [P?] Description`

- **[P]**: Can run in parallel because it touches different files and does not depend on incomplete tasks.
- Every task includes an exact file path.
- No source code should be changed until implementation begins.

---

## Phase 1: DB, Entities, and Migration Only

**Goal**: Add the shared identity schema and nullable player-to-user relationship without changing login or endpoint behavior.

**Exit Criteria**: The model and migration express `Users` identity fields, `Players.UserId` remains nullable, and the User 1 -> 0..1 Player relationship is configured with the intended indexes.

- [X] T001 Inspect current entity and EF Core mapping files before edits in Infrastructure/DataAccess/User.cs, Domain/Models/Player.cs, Infrastructure/DataAccess/ApplicationDbContext.cs
- [X] T002 [P] Inspect current migration snapshot and latest player migration in Infrastructure/Migrations/ApplicationDbContextModelSnapshot.cs and Infrastructure/Migrations/20260430105439_AddPlayerIndexesAndMaxLengths.cs
- [X] T003 [P] Add UserStatus enum with Active and Suspended values in Domain/Enums/UserStatus.cs
- [X] T004 Extend existing identity user with GoogleId, Name, AvatarUrl, IsPlatformAdmin, Status, CreatedAt, UpdatedAt, and Player navigation in Infrastructure/DataAccess/User.cs
- [X] T005 Add nullable UserId and User navigation to the current player profile entity in Domain/Models/Player.cs
- [X] T006 Configure User fields, max lengths, defaults, Users.Email unique index, and Users.GoogleId optional index in Infrastructure/DataAccess/ApplicationDbContext.cs
- [X] T007 Configure User 1 -> 0..1 Player relationship, nullable Players.UserId foreign key, and unique nullable Players.UserId index in Infrastructure/DataAccess/ApplicationDbContext.cs
- [X] T008 Generate EF Core migration for Users identity fields and nullable Players.UserId in Infrastructure/Migrations/
- [X] T009 Verify generated migration keeps Players.UserId nullable and creates only the intended Users/Players schema changes in Infrastructure/Migrations/
- [X] T010 Verify ApplicationDbContextModelSnapshot reflects User fields, UserStatus storage, nullable Players.UserId, and intended indexes in Infrastructure/Migrations/ApplicationDbContextModelSnapshot.cs

**Checkpoint**: Database/entities/migration phase is complete. No Google login, JWT, controller, handler, or endpoint behavior should be changed in this phase.

---

## Phase 2: Google Login and JWT Update

**Goal**: Extend existing Google login so a player login creates/finds a shared User, links/creates the Player profile, returns UserId and PlayerProfileId, and emits JWT claims that can resolve the authenticated User.

**Exit Criteria**: `POST /api/v1/Account/google-login` preserves current game login behavior while returning the shared identity fields and a token containing UserId plus backward-compatible player/Google identifiers.

- [ ] T011 Inspect current Google login, JWT, response, and repository files before edits in API/Controllers/AccountController.cs, Application/Features/Accounts/GoogleAuthenticate/GoogleAuthenticationCommandHandler.cs, Infrastructure/Services/UserService.cs, Infrastructure/Services/GoogleAuthenticationService.cs, Shared/Responses/LoginResponse.cs, Domain/Repositories/IPlayerRepository.cs, Infrastructure/Repositories/PlayerRepository.cs
- [ ] T012 [P] Add user repository contract for email/id lookup, create/update, and optional Player include access in Domain/Repositories/IUserRepository.cs
- [ ] T013 Implement user repository methods using ApplicationDbContext and the existing Identity user table in Infrastructure/Repositories/UserRepository.cs
- [ ] T014 Register IUserRepository with UserRepository in Infrastructure/ServiceConfig.cs
- [ ] T015 Extend IPlayerRepository with lookup by UserId and safe link/update helpers in Domain/Repositories/IPlayerRepository.cs
- [ ] T016 Implement PlayerRepository lookup by UserId and safe link/update helpers in Infrastructure/Repositories/PlayerRepository.cs
- [ ] T017 Extend LoginResponse with UserId and nullable PlayerProfileId while preserving existing login response fields in Shared/Responses/LoginResponse.cs
- [ ] T018 Update IUserService authentication contract if needed to accept final claim lists containing UserId and optional player id in Domain/Services/IUserService.cs
- [ ] T019 Update UserService JWT creation to preserve existing claims and support UserId/player claims in Infrastructure/Services/UserService.cs
- [ ] T020 Update GoogleAuthenticationCommandHandler to find or create User by Google email before player lookup in Application/Features/Accounts/GoogleAuthenticate/GoogleAuthenticationCommandHandler.cs
- [ ] T021 Update GoogleAuthenticationCommandHandler to update User.GoogleId, Name, AvatarUrl, Status, and UpdatedAt from Google identity data in Application/Features/Accounts/GoogleAuthenticate/GoogleAuthenticationCommandHandler.cs
- [ ] T022 Update GoogleAuthenticationCommandHandler to find existing Player by UserId or GoogleId and link it to UserId only when safe in Application/Features/Accounts/GoogleAuthenticate/GoogleAuthenticationCommandHandler.cs
- [ ] T023 Update GoogleAuthenticationCommandHandler to create Player with nullable-safe UserId, GoogleId, Email, Name, AvatarUrl, and existing default progression when no profile exists in Application/Features/Accounts/GoogleAuthenticate/GoogleAuthenticationCommandHandler.cs
- [ ] T024 Add controlled duplicate/ambiguous-link handling with GenericException for unsafe player-to-user matches in Application/Features/Accounts/GoogleAuthenticate/GoogleAuthenticationCommandHandler.cs
- [ ] T025 Generate JWT claims containing UserId and existing Google/player identifiers needed for backward compatibility in Application/Features/Accounts/GoogleAuthenticate/GoogleAuthenticationCommandHandler.cs
- [ ] T026 Populate LoginResponse.UserId and LoginResponse.PlayerProfileId while preserving Name, Email, PictureUrl, Gold, Experience, and Level in Application/Features/Accounts/GoogleAuthenticate/GoogleAuthenticationCommandHandler.cs
- [ ] T027 Verify AccountController response envelope remains BaseResponse<LoginResponse> for google-login in API/Controllers/AccountController.cs
- [ ] T028 Verify POST /api/v1/Account/google-login response fields against specs/001-user-identity-foundation/contracts/users-api.openapi.yaml

**Checkpoint**: Google login and JWT phase is complete. `/api/users/me` endpoints should not be added until Phase 3.

---

## Phase 3: /api/users/me Endpoints and Tests

**Goal**: Add authenticated current-user endpoints and validate the full feature with build/tests and quickstart scenarios.

**Exit Criteria**: `GET /api/v1/Users/me` and `GET /api/v1/Users/me/player-profile` return only the authenticated user's data, handle missing/suspended users with controlled errors, and the solution builds.

- [ ] T029 Inspect current API controller and CQRS feature conventions before endpoint edits in API/Controllers/QuestionController.cs, API/Controllers/AccountController.cs, Application/Features/Questions/GetQuestionsQuery.cs, Application/Features/Questions/GetQuestionsQueryHandler.cs
- [ ] T030 [P] Create CurrentUserResponse DTO with UserId, Email, Name, AvatarUrl, Status, IsPlatformAdmin, and PlayerProfileId in Application/Features/Users/GetCurrentUser/CurrentUserResponse.cs
- [ ] T031 [P] Create GetCurrentUserQuery carrying authenticated UserId in Application/Features/Users/GetCurrentUser/GetCurrentUserQuery.cs
- [ ] T032 Implement GetCurrentUserQueryHandler to load current User with optional Player and reject missing/suspended users with GenericException in Application/Features/Users/GetCurrentUser/GetCurrentUserQueryHandler.cs
- [ ] T033 [P] Create GetCurrentPlayerProfileQuery carrying authenticated UserId in Application/Features/Users/GetCurrentPlayerProfile/GetCurrentPlayerProfileQuery.cs
- [ ] T034 Implement GetCurrentPlayerProfileQueryHandler to load Player by UserId, reject missing/suspended users with GenericException, and map to PlayerProfileResponse in Application/Features/Users/GetCurrentPlayerProfile/GetCurrentPlayerProfileQueryHandler.cs
- [ ] T035 Add UsersController with api/v{version:apiVersion}/[controller], ApiVersion 1.0, Authorize, IMediator injection, and GET me action in API/Controllers/UsersController.cs
- [ ] T036 Add GET me/player-profile action to UsersController using MediatR and BaseResponse<PlayerProfileResponse> in API/Controllers/UsersController.cs
- [ ] T037 Extract UserId claim in UsersController and send current-user queries through MediatR in API/Controllers/UsersController.cs
- [ ] T038 Verify GET /api/v1/Users/me contract response fields against specs/001-user-identity-foundation/contracts/users-api.openapi.yaml
- [ ] T039 Verify GET /api/v1/Users/me/player-profile contract response fields and 404 behavior against specs/001-user-identity-foundation/contracts/users-api.openapi.yaml
- [ ] T040 [P] Add focused tests for Google login user creation/reuse and Player.UserId linking in SprintLabs.Tests/
- [ ] T041 [P] Add focused tests for GetCurrentUserQueryHandler success, missing user, suspended user, and no-player-profile cases in SprintLabs.Tests/
- [ ] T042 [P] Add focused tests for GetCurrentPlayerProfileQueryHandler success, no profile, wrong user isolation, and suspended user cases in SprintLabs.Tests/
- [ ] T043 Review touched files for existing style, thin controllers, CQRS boundaries, BaseResponse usage, GenericException usage, and no unrelated refactors in API/Controllers/AccountController.cs, API/Controllers/UsersController.cs, Application/Features/Accounts/GoogleAuthenticate/GoogleAuthenticationCommandHandler.cs, Application/Features/Users/, Domain/, Infrastructure/, Shared/Responses/LoginResponse.cs
- [ ] T044 Verify no communities, CommunityUsers, licenses, teachers, owners, admin community management, grades/classes, or student-license artifacts were added by scanning API/, Application/, Domain/, Infrastructure/, and Shared/
- [ ] T045 Run dotnet build for the solution in SprintLabs.sln
- [ ] T046 Run dotnet test for the solution in SprintLabs.sln if the existing test project can execute without new external infrastructure
- [ ] T047 Execute quickstart validation scenarios and record any environment limitations in specs/001-user-identity-foundation/quickstart.md

**Checkpoint**: Current-user endpoints, tests, and final validation are complete.

---

## Dependencies & Execution Order

### Phase Dependencies

- **Phase 1: DB/entities/migration only**: No implementation dependencies. Must complete before Phase 2 and Phase 3.
- **Phase 2: Google login and JWT update**: Depends on Phase 1 schema/entity work.
- **Phase 3: /api/users/me endpoints and tests**: Depends on Phase 1 schema/entity work and Phase 2 JWT UserId claim support for end-to-end authenticated validation.

### Parallel Opportunities

- T002 and T003 can run in parallel in Phase 1.
- T012 can run before repository implementation in Phase 2 while LoginResponse changes are reviewed.
- T030, T031, and T033 can run in parallel in Phase 3.
- T040, T041, and T042 can run in parallel after the relevant handlers are available.

### Validation Strategy

1. After Phase 1, inspect the migration and model snapshot before touching login behavior.
2. After Phase 2, validate new and repeated Google login manually or with focused tests before adding `/api/users/me`.
3. After Phase 3, run build, tests where feasible, contract checks, and quickstart scenarios.

### Task Discipline

- Keep Phase 1 strictly limited to DB, entities, enum, DbContext configuration, migration, and snapshot verification.
- Do not add `/api/users/me` endpoints before Phase 3.
- Do not add packages.
- Do not introduce communities or licenses.
- Do not make `Players.UserId` required in this feature.
