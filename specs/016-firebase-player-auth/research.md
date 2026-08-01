# Research: Firebase Player Authentication

## Decision 1: Firebase Admin SDK Version and Project Placement

**Decision**: Add `FirebaseAdmin` 3.6.0 only to `Infrastructure/Infrastructure.csproj`.

**Rationale**: FirebaseAdmin 3.6.0 is the current stable .NET package and supports .NET 6 or higher, with .NET 8 or higher recommended. SprintLabs targets .NET 8. Keeping the package in Infrastructure ensures Firebase SDK types never cross into Domain, Application, Shared, or API contracts. [NuGet package and framework support](https://www.nuget.org/packages/FirebaseAdmin/3.6.0)

**Alternatives considered**:

- Older FirebaseAdmin 3.x: rejected because there is no compatibility benefit for the current .NET 8 target.
- Manual JWT verification: rejected because the feature explicitly requires Firebase Admin SDK verification and the official SDK manages key rotation and project validation.
- Adding FirebaseAdmin to API or Application: rejected because it would leak provider dependencies across the repository's dependency boundaries.

## Decision 2: Resolve the Existing Google.Apis.Auth Version Conflict

**Decision**: Raise the existing direct `Google.Apis.Auth` reference from 1.68.0 to 1.75.0 when adding FirebaseAdmin 3.6.0.

**Rationale**: FirebaseAdmin 3.6.0 declares `Google.Apis.Auth >= 1.73.0`, while Infrastructure currently pins 1.68.0 and directly uses `GoogleJsonWebSignature` for the existing Google login. Updating the direct reference avoids a package-downgrade restore failure and keeps the directly used dependency explicit. Google.Apis.Auth 1.75.0 supports .NET 6+, including the repository's .NET 8 target. [FirebaseAdmin dependency listing](https://www.nuget.org/packages/FirebaseAdmin/3.6.0), [Google.Apis.Auth 1.75.0](https://www.nuget.org/packages/Google.Apis.Auth/1.75.0)

**Alternatives considered**:

- Pin exactly 1.73.0: compatible, but rejected because 1.75.0 is the current stable direct dependency and remains within the supported API line.
- Remove the direct reference: rejected because `GoogleAuthenticationService` directly compiles against Google.Apis.Auth.
- Keep 1.68.0: rejected because it is below FirebaseAdmin's minimum.

## Decision 3: One Firebase Application Using ADC and Explicit Project ID

**Decision**: Register one singleton `FirebaseApp`/`FirebaseAuth` using `GoogleCredential.GetApplicationDefault()` and `Authentication:Firebase:ProjectId`.

**Rationale**: Firebase recommends Application Default Credentials for Google-hosted environments such as Cloud Run. ADC automatically uses the Cloud Run service identity and honors `GOOGLE_APPLICATION_CREDENTIALS` for local non-Google environments. Passing the project ID explicitly removes ambiguity and binds token audience/issuer verification to the intended Firebase project. [Firebase Admin setup](https://firebase.google.com/docs/admin/setup)

**Alternatives considered**:

- Load a service-account JSON file from the repository: rejected because it would create a credential-leak risk and violates the feature requirements.
- Create `FirebaseApp` inside each request: rejected because the SDK app holds shared configuration/state and the feature requires one-time initialization.
- Rely only on project ID embedded in a credential: rejected because Cloud Run ADC and local user/application credentials do not always provide the intended Firebase project unambiguously.

## Decision 4: Verify Tokens With Revocation Checking

**Decision**: Call `FirebaseAuth.VerifyIdTokenAsync(idToken, checkRevoked: true, cancellationToken)`.

**Rationale**: The SDK verifies signature, expiration, and the Firebase project associated with the auth instance. Setting `checkRevoked` also rejects tokens revoked after issuance, matching the feature specification. The official .NET reference notes that revocation checking performs an additional remote API call, so the plan budgets for that latency. [Verify Firebase ID tokens](https://firebase.google.com/docs/auth/admin/verify-id-tokens), [VerifyIdTokenAsync .NET reference](https://firebase.google.com/docs/reference/admin/dotnet/class/firebase-admin/auth/abstract-firebase-auth)

**Alternatives considered**:

- Default `VerifyIdTokenAsync(idToken)` without revocation: lower latency, but rejected because it cannot prove revocation status.
- Decode JWT claims without verification: rejected because client-supplied claims cannot be trusted.
- Cache decoded tokens in SprintLabs: rejected because it complicates revocation and expiry behavior without a current requirement.

## Decision 5: Keep Firebase Claims Behind a Neutral Response

**Decision**: `IFirebaseAuthenticationService` returns `Shared.Responses.FirebaseUserResponse`, containing UID, email, email verification, name, picture, sign-in provider, and optional Google provider ID.

**Rationale**: Domain needs an interface outside Infrastructure, but it must not depend on `FirebaseToken`, `FirebaseAuthException`, or the SDK's claim representation. Infrastructure will parse verified token claims defensively, including the nested Google provider identity when present, and map only required values.

**Alternatives considered**:

- Return `FirebaseToken`: rejected because it leaks the SDK into Domain/Application and forces tests to construct SDK objects.
- Return a raw claim dictionary: rejected because it spreads provider-specific parsing and weak typing into Application.
- Call Firebase user-management APIs after every verification: rejected because the verified ID token already carries the required login identity and an extra network lookup is not needed for this story.

## Decision 6: Ordered Identity Resolution With Full Conflict Preflight

**Decision**: Resolve by FirebaseUid, then verified Google provider ID, then verified normalized email, but query and compare all available trusted candidates before changing data.

**Rationale**: Strict precedence selects the authoritative candidate, while full preflight detects when a lower-priority identity points to another account. Returning 409 before mutation prevents account takeover or accidental merges. An unverified email is never a linking key. Existing normalized email uniqueness is still checked to prevent a duplicate insert.

**Alternatives considered**:

- Stop after the first match without checking other identities: rejected because a token could bind conflicting identifiers to different SprintLabs users.
- Always merge by email: rejected because email must be verified and external identities may conflict.
- Create another user when an unverified email already exists: rejected because the existing unique email constraint prevents it and silently bypassing that constraint would create ambiguous identity ownership.

## Decision 7: Reuse IUserService and IPlayerRepository

**Decision**: Extend `IUserService` for Firebase resolution and `IPlayerRepository` for safe legacy player lookup; do not add provider-specific repositories.

**Rationale**: `UserService` already owns UserManager normalization/creation and user identity projection. `PlayerRepository` already owns player lookup and persistence. The required queries are simple and used only by the login workflow, so a new repository would wrap one-line EF operations and violate the repository/YAGNI rules.

**Alternatives considered**:

- `IFirebaseUserRepository`: rejected as a thin provider-specific CRUD wrapper.
- Put EF queries in Application: rejected because Application must not depend on Infrastructure/EF.
- Rewrite all identity persistence into a generic external-account subsystem: rejected as out of scope.

## Decision 8: Shared Application Login Completion Workflow

**Decision**: Extract suspension, player resolution, activation, claims, SprintLabs token issuance, and response mapping into `IExternalPlayerLoginWorkflow` implemented in Application and used by both provider handlers.

**Rationale**: These steps are provider-neutral and would otherwise be duplicated when Firebase is added. Provider handlers remain responsible only for verification and provider-specific user resolution/context mapping. Application registration belongs in `Application/ServiceConfig.cs`, preserving project direction.

**Alternatives considered**:

- Duplicate the Google handler into a Firebase handler: rejected because the user explicitly requested a shared workflow and duplicated activation/claim logic would drift.
- Infrastructure implementation of the workflow: rejected because Infrastructure cannot reference Application and should not own MediatR use-case orchestration.
- A generic authentication framework: rejected because two providers need only one narrow shared completion path.

## Decision 9: Provider-Specific Teacher Activation Policy

**Decision**: The shared workflow always applies existing student-license activation. It applies eligible teacher activation only when the Firebase handler requests it.

**Rationale**: The feature specification explicitly requires pending teacher activation for Firebase while also requiring the current Google behavior to remain unchanged. Current repository tests deliberately keep Google Teacher memberships pending until explicit acceptance. A provider policy is therefore necessary to satisfy both. Firebase activation validates the resolved internal user, verified normalized email, membership role/status, and invitation expiry/revocation, marks a valid invitation accepted when present, supports legacy pending memberships without invitation records, and never changes seat usage.

**Alternatives considered**:

- Activate teachers for both providers: rejected because it breaks the existing Google behavior and feature 015 regression tests.
- Activate no teachers: rejected because it fails feature 016.
- Ignore invitation state: rejected because it could activate expired, revoked, superseded, or mismatched invitations.

## Decision 10: Database Constraints Are the Concurrency Backstop

**Decision**: Add a unique nullable FirebaseUid index and retain unique Player UserId/GoogleId indexes; catch/re-read uniqueness races and return the consistent winner or 409.

**Rationale**: Application-level preflight prevents normal conflicts, but concurrent requests can pass the preflight together. Database uniqueness is the only reliable final guard. MySQL permits multiple NULL values in a unique index, which supports Firebase-only players after GoogleId becomes nullable.

**Alternatives considered**:

- Distributed lock: rejected as unnecessary infrastructure for indexed identity creation.
- Process-local lock: rejected because Cloud Run may have multiple instances.
- Remove uniqueness and rely on code: rejected because it cannot prevent cross-instance duplicates.

## Decision 11: Profile Ownership Uses Internal userId Everywhere

**Decision**: Refactor the legacy Account profile command/controller to use the JWT `userId` claim and `GetByUserIdAsync`; leave the already-correct current-player profile endpoint intact.

**Rationale**: Firebase `sub` is a Firebase UID while Google `sub` is a Google subject. Neither is the internal player owner key. The access token already includes internal `userId` and the newer PlayerProfiles endpoint already follows this safe pattern.

**Alternatives considered**:

- Put Firebase UID into `Player.GoogleId`: rejected by the identity model and would corrupt provider semantics.
- Branch profile lookup by provider: rejected because authorization should not depend on external identities.
- Remove the legacy route: rejected because compatibility requires a scoped correction, not a public API deletion.
