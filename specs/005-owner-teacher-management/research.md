# Research: Owner Teacher Management

## Decision 1: Reuse Existing Community Role Checks

**Decision**: Every owner-facing teacher-management handler will call `HasCommunityRole(userId, communityId, new[] { CommunityUserRole.Owner })`.

**Rationale**: The existing community access service already defines Active membership and role semantics. Reusing it keeps Pending and Removed memberships from granting access and keeps platform-admin access separate from owner community access.

**Alternatives considered**:

- Query `CommunityUser` directly in each handler: rejected because it duplicates community-role rules already built for previous features.
- Add authorization policies or a new permissions framework: rejected because the feature explicitly forbids new permission infrastructure and the project does not require it here.
- Let platform admins bypass owner checks: rejected because platform-admin authorization is separate and the feature is owner-scoped.

## Decision 2: Model Invites as Pending Teacher Memberships

**Decision**: A teacher invite creates or restores a `CommunityUser` row with `Role = Teacher` and `Status = Pending`.

**Rationale**: `CommunityUser` already represents community membership and has the exact role/status values needed. Pending status reserves a teacher seat without granting active access until matching login activation.

**Alternatives considered**:

- Add invitation tables or tokens: rejected because email sending and invite acceptance are out of scope.
- Create Active memberships immediately for existing users: rejected as the default because the feature says Pending by default and activation should occur on matching login.
- Store teacher profile records: rejected because teacher dashboards and teacher profiles are outside this feature.

## Decision 3: Enforce Teacher Seat Capacity with UsedTeachers

**Decision**: Invite and restore flows will require a `CommunityLicense` record and reject the operation when `UsedTeachers >= MaxTeachers`. Pending and Active Teacher memberships count as used seats.

**Rationale**: The feature explicitly defines `UsedTeachers` as counting Pending plus Active teacher seats. A missing license means capacity cannot be verified safely.

**Alternatives considered**:

- Recalculate usage from memberships on every request and ignore `UsedTeachers`: rejected because the existing license model stores usage counts that the feature asks to update.
- Allow invites without a license: rejected because it would bypass limits.
- Count only Active teachers: rejected because pending invites reserve seats by requirement.

## Decision 4: Increment and Decrement Seats Only on Counted State Transitions

**Decision**: `UsedTeachers` increments only when a new Pending/Active teacher seat is created or a Removed teacher is restored to Pending. It decrements only when a previously Pending or Active Teacher membership is set to Removed. Pending-to-Active activation does not change usage.

**Rationale**: This preserves the invariant that `UsedTeachers` reflects Pending plus Active teacher seats while preventing double-counting on login activation and repeated removals.

**Alternatives considered**:

- Increment on every invite request: rejected because duplicate invitations would inflate usage.
- Increment again during activation: rejected because pending invites already reserve seats.
- Decrement every time remove is called: rejected because repeated removal could drive usage below the true count.

## Decision 5: Use Generic Repositories and Scoped EF Queries

**Decision**: Handlers will inject `IBaseRepository<User>`, `IBaseRepository<CommunityUser>`, and `IBaseRepository<CommunityLicense>` as needed, using `AsQueryable()` with EF Core async operators for scoped lookups and includes.

**Rationale**: The required operations are simple existence, lookup, list, and update operations that the existing generic repository supports. This follows the repository rule and avoids a custom repository that only wraps one-line queries.

**Alternatives considered**:

- Create a teacher or community repository: rejected because there is no complex reused query or special persistence behavior.
- Use `GetByIdAsync` for community/license/user lookup: rejected where entity IDs are `long` because the current base method has an `int` signature.

## Decision 6: Activate Pending Teachers in the Existing Google Login Flow

**Decision**: After Google login creates or finds the `User` by normalized email, the flow will find Pending Teacher `CommunityUser` rows for that user and set them to Active without updating `UsedTeachers`.

**Rationale**: Activation depends on a verified login email and must preserve existing game/player login behavior. The safest touchpoint is immediately after user identity is resolved by email.

**Alternatives considered**:

- Add a separate acceptance endpoint: rejected because custom invite acceptance is out of scope.
- Activate by email string only before user identity is finalized: rejected because membership rows are tied to `UserId`; user resolution should happen first.
- Add background activation: rejected because login is the required trigger and is easier to validate.

## Decision 7: Keep Teacher Listing Owner-Scoped and Status-Aware

**Decision**: The teacher list returns Teacher memberships for the requested community, including user id, name, email, status, and created date. Removed teachers may appear with `status = Removed` so owners can understand historical/removal state.

**Rationale**: The spec asks to list teachers and include status, while separately requiring Removed teachers not to have active access. Showing status avoids implying Removed teachers are active.

**Alternatives considered**:

- Hide Removed teachers from the list: viable, but rejected for this plan because the spec does not limit the list to active/pending and asks for status visibility.
- Include all roles: rejected because this is teacher management and owner removal through this endpoint must not affect Owners.

## Decision 8: Focused Tests Around Authorization, Capacity, and State Transitions

**Decision**: Add handler-level tests for owner-only access, invite capacity, duplicate invite, removed restore, list scoping, soft removal, seat decrement rules, and login activation.

**Rationale**: The highest-risk behavior is role isolation and license accounting. Handler tests can verify these rules without introducing heavy integration infrastructure.

**Alternatives considered**:

- Build-only validation: rejected because seat accounting and role isolation are non-trivial.
- Full controller integration suite only: rejected because existing SprintLabs patterns favor focused handler tests for application logic.
