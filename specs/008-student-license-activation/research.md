# Research: Student License Activation on Login

## Decision 1: Extend the Existing Google Login Handler

**Decision**: Implement activation in `Application/Features/Accounts/GoogleAuthenticate/GoogleAuthenticationCommandHandler.cs`.

**Rationale**: The feature is triggered only by Google login and must preserve the existing response contract. The current handler already resolves the Google identity, creates/finds the shared `User`, activates pending teacher memberships, creates/finds `Player`, generates claims, and returns the login response.

**Alternatives considered**:

- Add a new endpoint: rejected because the feature explicitly says no new endpoint or invitation acceptance flow.
- Add a new invitation service: rejected because the feature only needs activation during existing login.
- Rewrite authentication flow: rejected because existing B2C and teacher activation behavior must remain intact.

## Decision 2: Activate Student Licenses After Player Profile Resolution

**Decision**: Run student license activation after the `User` is resolved and after the `Player` profile has been found or created.

**Rationale**: Student activation must set both `StudentLicense.UserId` and `StudentLicense.PlayerProfileId`. The existing handler already guarantees a player profile before JWT response generation, so this is the smallest point that has all required data.

**Alternatives considered**:

- Activate immediately after user resolution: rejected because it would need to duplicate or restructure player profile creation logic to set `PlayerProfileId`.
- Activate after JWT creation: rejected because the data mutation should complete before login returns success.

## Decision 3: Use Existing StudentLicense Schema Without Migration

**Decision**: Do not add a migration. Existing `StudentLicense` already has `UserId`, `PlayerProfileId`, `Status`, and `ActivatedAt`.

**Rationale**: The user requested no migration unless those planned fields are missing. Current `Domain/Models/StudentLicense.cs`, `ApplicationDbContext`, and the `OwnerStudentLicenseManagement` migration include the required fields and indexes for user/profile linkage.

**Alternatives considered**:

- Add new activation audit fields: rejected as future scope.
- Add a unique activation index: rejected because duplicate pending licenses are already prevented per community/email by owner license management.

## Decision 4: Match Pending Licenses by Normalized Email

**Decision**: Match `StudentLicense.Email` to the Google email using trimmed, lowercase normalization and `Status = Pending`.

**Rationale**: Owner student license management already normalizes stored license emails through `StudentLicenseValidation.NormalizeEmail`. Normalizing the Google email the same way preserves case-insensitive matching and avoids whitespace mismatches.

**Alternatives considered**:

- Match by exact email string: rejected because the spec requires normalized email matching.
- Match by user id: rejected because pending licenses are created before the student user may exist.

## Decision 5: Create or Restore Student Community Access

**Decision**: For each activated license, find a `CommunityUser` by `CommunityId` and resolved `UserId`. If none exists, create one with `Role = Student` and `Status = Active`. If an existing Student membership is Removed or Pending, update it to Active. If an existing non-Student membership exists, do not overwrite its role or create a duplicate.

**Rationale**: `CommunityUsers` has a unique `CommunityId + UserId` constraint, so activation must avoid duplicates. Restoring Removed student access satisfies the spec. Preserving non-Student roles avoids silently downgrading or changing owner/teacher permissions.

**Alternatives considered**:

- Always add a new Student membership: rejected because it violates the unique membership rule and repeated-login idempotency.
- Always overwrite the role to Student: rejected because it can break existing Owner or Teacher community access.

## Decision 6: Do Not Touch UsedStudents During Activation

**Decision**: Activation must not increment or otherwise update `CommunityLicense.UsedStudents`.

**Rationale**: Owner student license creation already reserves the seat by incrementing `UsedStudents`. Activation only changes license and access state.

**Alternatives considered**:

- Increment on activation: rejected because it would double count seats.
- Recalculate all used counts during login: rejected as too broad and risky for a login path.

## Decision 7: Preserve Pending Teacher Activation

**Decision**: Keep existing pending-teacher activation and add student activation without changing teacher seat counts or login response fields.

**Rationale**: Owner Teacher Management already added tests and behavior for teacher activation in the same handler. Student activation should be additive and compatible.

**Alternatives considered**:

- Move teacher and student activation into a new shared service: deferred unless the handler becomes too large; the current feature does not justify a new abstraction.
- Change teacher activation order: rejected because it is existing behavior with tests.
