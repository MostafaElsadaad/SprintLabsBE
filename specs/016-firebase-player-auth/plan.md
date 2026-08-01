# Implementation Plan: Firebase Player Authentication

**Branch**: `feature/teacher-email-authentication` (current worktree; no feature 016 branch was created) | **Date**: 2026-07-29 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `specs/016-firebase-player-auth/spec.md`

## Summary

Add a Firebase-backed player registration/login path beside the unchanged Google endpoint. The Unity client sends a Firebase ID token to `POST /api/v1/Account/firebase-login`; Infrastructure verifies it with the Firebase Admin SDK, maps SDK data into a neutral response, and Application resolves the SprintLabs user and player without merging conflicting identities. A shared Application workflow will finish both Google and Firebase player logins by enforcing suspension, resolving/creating the player by internal `UserId`, applying provider-appropriate login activations, adding internal claims, issuing the existing SprintLabs access token, and mapping the existing `LoginResponse`.

The schema adds nullable unique `User.FirebaseUid` and makes legacy `Player.GoogleId` nullable. Firebase-only players never place their UID in a Google field. The existing Google public contract and its current teacher-invitation behavior remain unchanged. Firebase login additionally completes only eligible pending teacher invitations/memberships using the verified internal user and email while preserving invitation validity and seat-count invariants.

## Technical Context

**Language/Version**: C# / .NET 8

**Primary Dependencies**: Existing ASP.NET Core 8, API versioning, MediatR, ASP.NET Core Identity, EF Core 8, Pomelo MySQL, JWT bearer authentication, `IBaseRepository<T>`, `BaseResponse`, and `GenericException`; add `FirebaseAdmin` 3.6.0 to Infrastructure and raise the existing direct `Google.Apis.Auth` reference from 1.68.0 to 1.75.0

**Storage**: Existing MySQL `Users`, `Players`, `CommunityUsers`, `TeacherInvitations`, `StudentLicenses`, and `CommunityLicenses`; one new EF Core migration, no new tables

**Testing**: Existing xUnit 2.9.2, FluentAssertions 6.12.1, Moq 4.20.72, EF Core InMemory/SQLite, optional MySQL fixture; Firebase verification is mocked through `IFirebaseAuthenticationService` and automated tests make no Firebase calls

**Target Platform**: ASP.NET Core backend on Cloud Run/Linux with local Windows/Linux development support; Unity is the API client but Unity code is outside this repository

**Project Type**: Versioned backend web API split into API, Application, Domain, Infrastructure, and Shared projects

**Performance Goals**: Normal login completes within the specification's five-second user target; identity and player resolution use bounded indexed lookups; Firebase is initialized once; each request performs one token verification plus an additional revocation check when enabled, then a small bounded set of database operations

**Constraints**: Firebase SDK types remain in Infrastructure; no service-account JSON in source; Application Default Credentials work on Cloud Run and through `GOOGLE_APPLICATION_CREDENTIALS` locally; Firebase UID is never written to Google identity fields; Google route/input/response and current post-login behavior remain stable; profile ownership uses only the internal `userId` claim

**Scale/Scope**: One new login endpoint and CQRS slice, one shared external-player-login workflow, two column changes, one migration, extensions to four existing services/repositories, one legacy profile route correction, documentation, and focused regression/security tests

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-checked after Phase 1 design.*

- **Vertical slices over layer-first work**: PASS. The plan covers schema, provider verification, CQRS/API, identity and player behavior, activation, migration, tests, and frontend-facing documentation.
- **Existing architecture wins**: PASS. Controllers remain thin; Application owns orchestration; Domain exposes service/repository contracts; Infrastructure owns Firebase, Identity, EF, and provider exceptions; Shared carries SDK-neutral options/results.
- **SaaS data isolation**: PASS. Firebase identity resolves one internal user; community changes are limited to that user's eligible memberships/licenses and retain community role, invitation, and capacity checks.
- **JSON where flexibility matters**: PASS. No Questions JSON behavior changes.
- **Minimum useful implementation**: PASS. One requested provider, one endpoint, no auth framework rewrite, no custom Firebase repository, no new table, and no Unity UI work.
- **Quality gates**: PASS. The plan requires migration inspection, focused identity/conflict/concurrency tests, Google regressions, full build, and relevant/full test runs.
- **Documentation is executable context**: PASS. Spec, research, data model, HTTP contract, API/frontend guidance, quickstart, and plan remain together in feature 016.

No constitution violation requires an exception.

## Existing Pattern and Impact

### Patterns Preserved

- Account routes stay under `api/v{version:apiVersion}/Account`.
- Controllers send MediatR commands and wrap successful results in `BaseResponse<T>`.
- Controlled authentication and identity failures use `GenericException`, `ErrorMessage`, and `ErrorCode`.
- `IGoogleAuthenticationService` and the new `IFirebaseAuthenticationService` live in Domain; implementations live in Infrastructure and are registered in `Infrastructure/ServiceConfig.cs`.
- SDK-neutral provider response types live in Shared so Domain/Application never reference Firebase SDK types.
- Simple user queries continue through the existing `UserManager<User>`/EF-backed `IUserService`; player CRUD remains in `IPlayerRepository`.
- The current student-license activation service remains the source of login-time student activation rules.
- The Google endpoint still accepts its existing `googleAccessToken` input and returns the same `BaseResponse<LoginResponse>`.

### Existing Behavior Corrected or Extended

- The legacy `PUT /api/v1/Account/profile` path currently treats `ClaimTypes.NameIdentifier`/subject as GoogleId. It will instead parse the internal `userId` claim and query `Player.UserId`, matching the newer `PATCH /api/v1/PlayerProfiles/me` path.
- `UserService.FindOrCreateGoogleUser` is not redesigned in this feature; Google compatibility is protected by regression tests. Firebase receives a separate conflict-safe resolution method.
- `Player.GoogleId` becomes nullable so Firebase-only players can be stored without a fabricated Google identifier.
- Firebase login may activate eligible pending teacher state; Google login continues leaving pending Teacher memberships pending. Invitation records are validated and kept consistent, and `UsedTeachers` never changes during login activation.

## Project Structure and Exact File Plan

`[M]` modify, `[N]` create, `[G]` generated migration.

### Documentation

```text
specs/016-firebase-player-auth/
|-- spec.md
|-- plan.md
|-- research.md
|-- data-model.md
|-- quickstart.md
|-- api.md
|-- frontend.md
|-- contracts/
|   `-- firebase-player-authentication-api.md
`-- checklists/
    `-- requirements.md
```

### API

```text
API/
|-- Controllers/
|   `-- AccountController.cs                         [M: Firebase action; userId-based legacy profile action]
|-- appsettings.json                                 [M: empty/non-secret Firebase project configuration key]
`-- appsettings.Development.json                     [M only if a non-secret local project-id placeholder is useful]
```

No Firebase initialization is placed in the controller or per-request middleware.

### Application

```text
Application/
|-- ServiceConfig.cs                                 [M: register shared Application workflow]
`-- Features/Accounts/
    |-- Common/
    |   |-- ExternalPlayerLoginContext.cs            [N]
    |   |-- IExternalPlayerLoginWorkflow.cs          [N]
    |   `-- ExternalPlayerLoginWorkflow.cs           [N]
    |-- FirebaseAuthenticate/
    |   |-- FirebaseAuthenticationRequest.cs         [N]
    |   |-- FirebaseAuthenticationCommand.cs         [N]
    |   `-- FirebaseAuthenticationCommandHandler.cs  [N]
    |-- GoogleAuthenticate/
    |   `-- GoogleAuthenticationCommandHandler.cs    [M: delegate common completion work]
    `-- UpdateProfile/
        |-- UpdateProfileCommand.cs                   [M: internal UserId replaces GoogleId]
        `-- UpdateProfileCommandHandler.cs            [M: resolve by UserId]
```

The shared workflow receives a resolved `UserIdentityResponse` plus an `ExternalPlayerLoginContext`. Context contains the external subject used for the JWT `sub`, optional verified Google provider ID, normalized identity fields, and an explicit Firebase-only teacher-activation policy. It does not contain Firebase SDK objects.

### Domain

```text
Domain/
|-- Models/
|   `-- Player.cs                                    [M: nullable GoogleId]
|-- Repositories/
|   `-- IPlayerRepository.cs                         [M: nullable Google lookup and verified-email fallback]
`-- Services/
    |-- IFirebaseAuthenticationService.cs            [N]
    |-- IUserService.cs                              [M: Firebase identity resolution]
    `-- ICommunityLoginActivationService.cs          [M: eligible teacher-login activation]
```

No feature-specific Firebase repository is introduced. Identity matching is part of the existing user service because it already owns Identity/EF user creation and normalization.

### Infrastructure

```text
Infrastructure/
|-- Infrastructure.csproj                            [M: FirebaseAdmin 3.6.0; Google.Apis.Auth 1.75.0]
|-- DataAccess/
|   |-- User.cs                                      [M: nullable FirebaseUid]
|   `-- ApplicationDbContext.cs                      [M: nullable/unique mappings]
|-- Repositories/
|   `-- PlayerRepository.cs                          [M: nullable Google and normalized email lookup]
|-- Services/
|   |-- FirebaseAuthenticationService.cs             [N]
|   |-- UserService.cs                               [M: ordered Firebase resolution/conflict checks]
|   `-- CommunityLoginActivationService.cs           [M: Firebase-only eligible Teacher activation]
|-- ServiceConfig.cs                                 [M: options, singleton app/auth service registration]
`-- Migrations/
    |-- <timestamp>_AddFirebasePlayerAuthentication.cs          [G]
    |-- <timestamp>_AddFirebasePlayerAuthentication.Designer.cs [G]
    `-- ApplicationDbContextModelSnapshot.cs                    [G/M]
```

`FirebaseApp`/`FirebaseAuth` are created once through singleton DI using `GoogleCredential.GetApplicationDefault()` and explicit project ID. Only `FirebaseAuthenticationService.cs` and Infrastructure registration reference Firebase types.

### Shared

```text
Shared/
|-- Options/
|   `-- FirebaseAuthenticationOptions.cs             [N]
`-- Responses/
    |-- FirebaseUserResponse.cs                      [N]
    `-- UserIdentityResponse.cs                      [M: optional FirebaseUid visibility to internal callers/tests]
```

`FirebaseUserResponse` contains only strings/booleans needed by Application: Firebase UID, email, email-verification flag, display name, picture URL, sign-in provider, and optional Google provider ID.

### Tests

```text
SprintLabs.Tests/Features/
|-- FirebasePlayerAuthentication/
|   |-- FirebaseAuthenticationCommandHandlerTests.cs [N]
|   |-- FirebaseUserIdentityResolutionTests.cs       [N]
|   |-- ExternalPlayerLoginWorkflowTests.cs          [N]
|   |-- FirebaseLoginActivationTests.cs              [N]
|   |-- FirebaseAuthenticationControllerTests.cs     [N]
|   `-- FirebaseAuthenticationPersistenceTests.cs    [N]
|-- B2CPlayerProfileSupport/
|   |-- B2CLoginCompatibilityTests.cs                [M: shared workflow constructor]
|   `-- UpdateCurrentPlayerProfileCommandHandlerTests.cs [M only if shared setup changes]
|-- StudentLicenseActivation/
|   `-- StudentLicenseActivationOnLoginTests.cs      [M: shared workflow coverage]
|-- OwnerTeacherManagement/
|   `-- PendingTeacherActivationTests.cs             [M: Google remains pending; Firebase eligible activation]
`-- AccountProfile/
    `-- UpdateProfileCommandHandlerTests.cs           [N]
```

No automated test resolves Application Default Credentials, creates `FirebaseApp`, or calls Firebase. `IFirebaseAuthenticationService` is mocked at every handler/controller boundary.

**Structure Decision**: Keep provider verification in Infrastructure, provider-neutral orchestration in Application, existing Identity/player persistence services in Infrastructure behind Domain contracts, and cross-layer DTO/options in Shared. This preserves the dependency graph and avoids a repository or generic authentication framework that the story does not need.

## Phase 0: Research Decisions

Research is recorded in [research.md](./research.md). Key decisions:

1. Use `FirebaseAdmin` 3.6.0, compatible with .NET 8, in Infrastructure only.
2. Upgrade direct `Google.Apis.Auth` to 1.75.0 because FirebaseAdmin 3.6.0 requires at least 1.73.0 and the current project pins 1.68.0.
3. Initialize one Firebase app/auth instance from ADC with the configured project ID.
4. Verify with `VerifyIdTokenAsync(idToken, checkRevoked: true, cancellationToken)`; the revocation check adds one provider API call but satisfies the specification's revoked-token rejection.
5. Parse trusted standard claims plus the verified `firebase.identities["google.com"]` value defensively inside Infrastructure, producing no SDK types outside that layer.
6. Use database uniqueness as the final concurrency guard for Firebase UID, normalized email, player UserId, and player GoogleId.

## Phase 1: Schema and Provider Foundation

1. Add `FirebaseAdmin` 3.6.0 and update `Google.Apis.Auth` to 1.75.0 in `Infrastructure.csproj`; restore and inspect the resolved dependency graph before source changes.
2. Add `FirebaseAuthenticationOptions.ProjectId`, bind `Authentication:Firebase`, reject empty configuration at startup, and keep only non-secret placeholders in tracked settings.
3. Add `User.FirebaseUid` as nullable `varchar(128)` with a unique index.
4. Make `Player.GoogleId` nullable while retaining its max length and unique index.
5. Generate `AddFirebasePlayerAuthentication` through EF Core; never edit older migrations.
6. Inspect Up/Down operations, snapshot, MySQL types, index names, and nullable-unique behavior. Ensure rollback handles Firebase-only null Google IDs deterministically rather than losing player rows.

**Exit gate**: package restore has no downgrade warnings; the migration compiles; existing rows need no data backfill; Firebase UID is unique; multiple Firebase-only players may have null GoogleId.

## Phase 2: Firebase Verification Boundary

1. Add SDK-neutral `FirebaseUserResponse` and `IFirebaseAuthenticationService`.
2. Implement `FirebaseAuthenticationService` with a singleton `FirebaseAuth` created from one singleton `FirebaseApp`.
3. Build the app with `GoogleCredential.GetApplicationDefault()` and explicit configured project ID.
4. Call `VerifyIdTokenAsync` with cancellation and revoked-token checking.
5. Extract UID, email, email verification, name, picture, sign-in provider, and optional Google provider ID from the verified token.
6. Treat missing UID/email and all provider validation failures as `GenericException(ErrorMessage.InvalidAccessToken, 401)`.
7. Catch provider/argument failures before the global exception middleware so raw Firebase errors and tokens never enter public errors or normal logs. Log only a safe event category/correlation context if logging is needed.

**Exit gate**: Infrastructure is the only project containing Firebase namespaces; invalid token paths map to a safe 401; startup uses ADC and initializes once.

## Phase 3: Conflict-Safe User Resolution

1. Add `IUserService.FindOrCreateFirebaseUser(FirebaseUserResponse, CancellationToken)`.
2. Normalize email with `UserManager.NormalizeEmail`.
3. Query and classify candidates independently by Firebase UID, verified Google provider ID, and verified normalized email.
4. Resolve in strict order: Firebase UID, Google provider identity, verified email.
5. Before mutation, compare all available trusted candidates. More than one user ID, duplicate candidates for one identifier, an existing different Firebase UID, or a conflicting GoogleId returns controlled 409.
6. For unverified email, do not use the email candidate for linking; still detect a normalized-email collision before attempted creation and return 409 rather than violating the unique email index.
7. Create a new active user only when no trusted match/conflict exists. Use email as fallback name when display name is empty and permit an empty picture.
8. Attach FirebaseUid and a verified Google provider ID only when the selected user has no conflicting value.
9. On a uniqueness race, re-query by FirebaseUid and normalized email. Return the same consistent winner or controlled 409; never expose an EF/Identity provider message.

**Exit gate**: first login creates one user; repeat login returns it; each linking order and conflict matrix is tested; unverified email never claims an existing account.

## Phase 4: Shared External Player Login Workflow

1. Add `IExternalPlayerLoginWorkflow` and register it in `Application/ServiceConfig.cs`.
2. Move suspension enforcement, player resolution/creation, login activation, JWT claim assembly, token issuance, and `LoginResponse` mapping out of `GoogleAuthenticationCommandHandler`.
3. Resolve player first by internal UserId.
4. If absent, try verified Google provider ID; if still absent and email is verified, try normalized player email for safe legacy attachment.
5. Reject an ambiguous candidate or a player already linked to another user with controlled 409.
6. Create new players with internal UserId and optional Google provider ID. Never use Firebase UID as Player.GoogleId.
7. Preserve progression/profile fields on repeat login. Only fill identity linkage/profile identity fields when safely attaching or creating.
8. Handle unique-index races by re-reading UserId/Google/email candidates and returning the consistent player or controlled 409.
9. Apply student-license activation for both providers.
10. For Firebase only, invoke eligible pending-teacher activation. Google passes a policy that preserves current pending-until-explicit-acceptance behavior.
11. Add provider subject, email, and name claims plus internal `userId` and `playerProfileId`; issue the existing SprintLabs access token through `IUserService.Authenticate`.
12. Return the unchanged `LoginResponse` fields from the resolved user/player.

**Exit gate**: Google and Firebase handlers both delegate common completion work; Firebase-only players have null GoogleId; all progression values survive repeat/linking flows.

## Phase 5: Login-Time Activation Compatibility

1. Keep `ActivatePendingStudentLicensesAsync` behavior unchanged and call it from the shared workflow.
2. Add an eligible pending-teacher activation operation for Firebase login only.
3. Select pending Teacher memberships belonging to the resolved user.
4. Require verified normalized email equality.
5. For a current invitation, require unexpired, unrevoked, unaccepted matching invitation data; activate the membership and mark that invitation accepted in the same save.
6. Allow legacy pending Teacher memberships with no invitation record to activate for their already-linked internal user.
7. Leave memberships pending when their only invitations are expired, revoked, superseded, mismatched, or otherwise ineligible.
8. Never increment/decrement `UsedTeachers` during login activation because the seat was reserved at invitation/membership creation.
9. Preserve non-Teacher roles and other users' memberships.

**Exit gate**: eligible Firebase teacher state activates once; invalid invitation state does not; repeated login is idempotent; Google teacher regression remains pending; student activation still passes.

## Phase 6: API and Profile Ownership

1. Add `FirebaseAuthenticationRequest` with `idToken`, a command, and handler.
2. The handler verifies the token, validates required neutral identity, resolves/creates the user, then calls the shared workflow with Firebase subject/provider data and Firebase teacher-activation policy.
3. Add `[AllowAnonymous] POST "firebase-login"` to `AccountController`; accept JSON body and return `BaseResponse<LoginResponse>` with HTTP 200.
4. Keep `google-login` route, query input, HTTP status, and response envelope unchanged while switching its handler internals to the shared workflow.
5. Change legacy `UpdateProfileCommand.GoogleId` to server-derived `UserId`.
6. Change `AccountController.UpdateProfile` to read only the `userId` claim, reject missing/unparseable values with 401, and never use NameIdentifier/subject.
7. Change the legacy handler to `GetByUserIdAsync`; retain validation and response envelope. Add the same suspended-user protection already present in the newer current-player profile path.
8. Keep `PATCH /PlayerProfiles/me` unchanged except for regression coverage confirming it already follows internal UserId.

**Exit gate**: Firebase request/response/errors match the contract; both profile routes scope by internal userId; the existing Google contract is unchanged.

## Phase 7: Tests, Documentation, and Verification

1. Handler/controller tests mock `IFirebaseAuthenticationService` and never instantiate Firebase SDK types.
2. Identity service tests cover new user, UID repeat, Google linking, verified-email linking, unverified-email collision, every cross-identity conflict, suspended user, and concurrency/unique-index outcomes.
3. Workflow tests cover internal UserId priority, legacy Google/email attachment, player conflict, Firebase-only null GoogleId, defaults, repeat progression preservation, B2C, claims, and response mapping.
4. Activation tests cover teacher invitation acceptance-on-Firebase-login, legacy pending teacher state, expired/revoked/mismatched invitations, repeated login, student activation, role conflict, and seat invariants.
5. Profile tests cover the legacy Account route/handler using userId and rejecting subject-only/missing-userId tokens.
6. Update Google B2C, student-license, and pending-teacher tests for the shared workflow without changing their expected behavior.
7. Inspect generated migration and model snapshot.
8. Run package restore/listing, build, focused tests, then full tests.
9. Execute [quickstart.md](./quickstart.md) against a configured development Firebase project only during manual verification.

**Exit gate**: automated tests make zero Firebase calls; build and focused/full tests pass; manual valid/wrong-project token checks pass; tracked files contain no credential JSON or token values.

## Identity Resolution Matrix

| Firebase UID match | Google provider match | Verified email match | Result |
|---|---|---|---|
| One user A | none/A | none/A | Resolve A; attach missing safe identities |
| none | one user B | none/B | Resolve B; attach Firebase UID |
| none | none | one user C | Resolve C only when email verified; attach Firebase UID |
| none | none | none | Create one user if required identity is present and email does not collide |
| user A | user B | any | 409; no mutation |
| user A | none/A | user B | 409; no mutation |
| none | user A | user B | 409; no mutation |
| selected user has different FirebaseUid or GoogleId | any | any | 409; no overwrite |
| email unverified and existing email candidate | none | candidate exists | 409; do not link or create duplicate |

All queries treat multiple rows for one supposedly unique external identity as a conflict requiring data repair.

## Transaction and Concurrency Boundaries

| Operation | Required consistency |
|---|---|
| Firebase user create/link | Evaluate all trusted candidates before mutation; unique FirebaseUid and normalized-email indexes are final guards |
| Legacy player attach/create | Check UserId/Google/email ownership; unique UserId and GoogleId indexes are final guards |
| Student activation | Existing service saves membership/license changes together through the shared scoped DbContext |
| Teacher login activation | Membership status and invitation AcceptedAt change in one SaveChanges transaction; no seat mutation |
| Token issuance | Occurs only after identity/player/activation state succeeds |

When a database uniqueness race occurs, the operation re-reads the authoritative records. It returns the consistent winner only when all trusted identities point to that same user/player; otherwise it returns 409.

## Error Mapping

| Condition | HTTP | Public message |
|---|---:|---|
| Missing/blank/malformed/expired/revoked/wrong-project Firebase token | 401 | Existing invalid-access-token message |
| Verified token missing UID or email | 401 | Existing invalid-access-token message |
| Suspended resolved user | 403 | Existing invalid-access-token behavior |
| External identities point to different records | 409 | Existing record/conflict message |
| Legacy player belongs to another user or is ambiguous | 409 | Existing record/conflict message |
| Missing internal userId on profile update | 401 | Empty/standard unauthorized response |
| Firebase project/ADC missing at startup | Startup failure | Safe configuration error; no credential details |

Provider exception messages and raw tokens never reach `GlobalExceptionHandler`; Infrastructure converts expected verification failures to `GenericException` first.

## Security and Deployment

- Add only `Authentication:Firebase:ProjectId` to configuration; it is an identifier, not a credential.
- Cloud Run uses its attached service account through ADC.
- Local development may set `GOOGLE_APPLICATION_CREDENTIALS` to a file outside the repository.
- `.gitignore` and tracked-file inspection must confirm no service-account JSON is added.
- Firebase app/auth objects are singleton-scoped and never recreated per request.
- The configured project ID is passed explicitly to `AppOptions`, binding issuer/audience verification to the intended project.
- `VerifyIdTokenAsync(..., checkRevoked: true, ...)` performs the requested revocation rejection at the cost of an additional remote lookup.
- Never log request bodies, ID tokens, decoded claim dictionaries, provider exception text, credential paths, or service-account data.
- SprintLabs JWT includes external subject for compatibility but authorization/profile ownership uses internal `userId`.

## Backward Compatibility Risks

| Risk | Mitigation |
|---|---|
| Google handler refactor changes public response | Snapshot existing fields/claims in regression tests before extraction; preserve Google context values |
| `Google.Apis.Auth` upgrade affects Google validation | Run existing Google login tests and a manual valid/invalid Google token check in development |
| Nullable Player.GoogleId affects queries | Update repository signature/query null handling; retain unique index; test Firebase-only and Google players |
| Email-based legacy linking claims another account | Permit only verified email, check all identities first, and return 409 on ambiguity/conflict |
| Firebase and Google subjects differ | Keep provider subject only in `sub`; always add/use internal `userId` for ownership |
| Firebase teacher activation conflicts with explicit invitations | Apply only on Firebase, validate invitation/user/email/status/expiry/revocation, mark accepted consistently, and leave Google behavior unchanged |
| Global middleware exposes provider detail | Catch/map Firebase exceptions inside Infrastructure; add safe-error tests and log inspection |
| Startup fails without Firebase configuration | Validate early with a clear non-secret configuration error; document local/Cloud Run setup |
| Migration rollback encounters null GoogleId | Inspect/customize generated Down path so rollback uses deterministic non-sensitive placeholders or requires documented pre-rollback repair without deleting players |

## Test Strategy

### Provider-Independent Unit/Handler Tests

- Firebase handler: valid neutral response, missing UID/email, safe 401, cancellation, workflow invocation.
- Shared workflow: suspension, lookup precedence, defaults, response fields, JWT claims, provider policy.
- Legacy profile: userId resolution, missing claim, validation, suspension, not found.
- Error mapping: 401/403/409 messages contain no raw token/provider detail.

### Identity/Persistence Tests

- User resolution matrix including duplicate/cross-identity candidates.
- Unique nullable FirebaseUid and nullable Player.GoogleId model behavior.
- First/repeated/concurrent user and player creation.
- Legacy player attachment and cross-user conflict.
- No progression reset on repeat/link.
- Teacher invitation/member and student-license state changes with seat invariants.

### Regression Tests

- Existing Google B2C login response and player creation/reuse.
- Existing Google pending Teacher memberships remain Pending.
- Existing Google student-license activation remains Active/idempotent.
- Existing Google token verification service still functions after dependency upgrade.
- Both profile routes use internal userId.
- Existing teacher email authentication and explicit invitation endpoint remain operational.

### Commands

```powershell
dotnet restore SprintLabs.sln
dotnet list Infrastructure/Infrastructure.csproj package --include-transitive
dotnet build SprintLabs.sln
dotnet test SprintLabs.Tests/Compass.Tests.csproj --filter "FullyQualifiedName~FirebasePlayerAuthentication|FullyQualifiedName~B2CPlayerProfileSupport|FullyQualifiedName~StudentLicenseActivation|FullyQualifiedName~OwnerTeacherManagement|FullyQualifiedName~AccountProfile"
dotnet test SprintLabs.sln
```

## Phase 0 and Phase 1 Artifacts

- [research.md](./research.md)
- [data-model.md](./data-model.md)
- [contracts/firebase-player-authentication-api.md](./contracts/firebase-player-authentication-api.md)
- [quickstart.md](./quickstart.md)
- [api.md](./api.md)
- [frontend.md](./frontend.md)

## Post-Design Constitution Check

- **Vertical slices over layer-first work**: PASS. Seven phases end in independently verifiable schema, verification, identity, player, activation, API, and quality outcomes.
- **Existing architecture wins**: PASS. Exact files follow current project references, CQRS layout, Domain service contracts, Infrastructure implementations, repositories, envelopes, exceptions, and DI.
- **SaaS data isolation**: PASS. Community mutations are constrained to the resolved user and preserve invitation/license ownership, roles, and seat counters.
- **JSON where flexibility matters**: PASS. Not applicable.
- **Minimum useful implementation**: PASS. Only one package family, one endpoint, one new external service, one shared workflow, and required schema fields are added.
- **Quality gates**: PASS. Restore, migration inspection, build, focused/full tests, no-network automated tests, credential scan, and manual provider verification are explicit.
- **Documentation is executable context**: PASS. All requested and mandated artifacts are linked and colocated.

## Complexity Tracking

No constitution violations identified. The Application workflow is justified because two provider handlers otherwise duplicate player resolution, activation, claims, token issuance, and response mapping. The Firebase-specific service boundary is required to keep SDK types and provider errors in Infrastructure. No custom repository or new project is added.
