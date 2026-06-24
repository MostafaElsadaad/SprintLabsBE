# Feature Specification: Community Access Foundation

**Feature Branch**: `003-community-access-foundation`

**Created**: 2026-06-23

**Status**: Draft

**Input**: User description: "Build the community access foundation for Sprint Labs. Users can only access communities where they have a valid CommunityUser role. This feature covers community access checking, community role checking, reusable authorization helpers or filters for Owner, Teacher, and Student roles, and GET /api/users/me/communities. Teacher management endpoints, student licenses, student management, grades, classes, analytics, owner dashboard, payment logic, and admin community creation/licensing are out of scope."

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Check Community Access (Priority: P1)

A signed-in user is allowed to interact with a community only when they have an active membership in that community.

**Why this priority**: Community ownership and isolation are the foundation for all future B2B school features. Future endpoints must be able to rely on one consistent access decision.

**Independent Test**: Can be fully tested by creating active, pending, removed, and missing memberships for a user, then checking access for each membership state.

**Acceptance Scenarios**:

1. **Given** a user has an Active community membership, **When** community access is checked for that community, **Then** access is allowed.
2. **Given** a user has no membership for a community, **When** community access is checked, **Then** access is denied.
3. **Given** a user has a Pending membership for a community, **When** community access is checked, **Then** access is denied.
4. **Given** a user has a Removed membership for a community, **When** community access is checked, **Then** access is denied.

---

### User Story 2 - Check Community Roles (Priority: P2)

A future community endpoint can require one or more community roles and receive a consistent answer for whether the current user has an active matching role.

**Why this priority**: Owner, Teacher, and Student experiences will need different permissions. The platform needs a reusable role check before those endpoints are built.

**Independent Test**: Can be fully tested by assigning users active Owner, Teacher, and Student memberships, checking each role against allowed role sets, and confirming non-matching or inactive roles are denied.

**Acceptance Scenarios**:

1. **Given** a user has an Active Owner membership, **When** a role check requires Owner, **Then** the check succeeds.
2. **Given** a user has an Active Teacher membership, **When** a role check allows Teacher or Owner, **Then** the check succeeds.
3. **Given** a user has an Active Student membership, **When** a role check requires Owner, **Then** the check fails.
4. **Given** a user has a Pending or Removed membership with an otherwise matching role, **When** the role check runs, **Then** the check fails.

---

### User Story 3 - List My Communities (Priority: P3)

An authenticated user can see the communities where they currently have active membership and the role they hold in each community.

**Why this priority**: Client applications need a safe identity-linked community list before showing school/community navigation or choosing a working context.

**Independent Test**: Can be fully tested by signing in as a user with active, pending, removed, and other-user memberships, then requesting the current user's community list.

**Acceptance Scenarios**:

1. **Given** an authenticated user has Active Owner, Teacher, or Student memberships, **When** the current user's communities are requested, **Then** those communities are returned with community details and membership role/status.
2. **Given** an authenticated user has Removed memberships, **When** the current user's communities are requested, **Then** removed memberships are not returned.
3. **Given** an authenticated user has Pending memberships, **When** the current user's communities are requested, **Then** pending memberships are not returned.
4. **Given** another user belongs to a community, **When** the current user's communities are requested, **Then** the other user's community is not returned.
5. **Given** no authenticated user is present, **When** the current user's communities are requested, **Then** the request is rejected.

### Edge Cases

- A platform admin has no CommunityUser membership for a community; community role access is denied because platform admin access is separate from community membership access.
- A user has multiple memberships across different communities; only active memberships for that specific user are returned or accepted.
- A community is Suspended but the user's membership is Active; the community may appear in the user's list with its suspended status, but access checks still require active membership and future endpoints may add their own community-status rule.
- A membership exists for a community that has been deleted or is otherwise unavailable; the access check fails safely and the missing community is not returned.
- A role check is called with no required roles; the check should deny access because no acceptable role was specified.
- Role names or status values are compared against the defined community membership values, not free-form text.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The system MUST provide a reusable community access check for a user and community.
- **FR-002**: The community access check MUST return success only when the user has a CommunityUser row for the requested community with Active status.
- **FR-003**: The community access check MUST deny users with no membership for the community.
- **FR-004**: The community access check MUST deny memberships with Pending or Removed status.
- **FR-005**: The system MUST provide a reusable community role check for a user, community, and one or more required roles.
- **FR-006**: The community role check MUST return success only when the user has Active membership in the community and the user's role matches at least one required role.
- **FR-007**: The community role check MUST support Owner, Teacher, and Student roles.
- **FR-008**: The community role check MUST deny access when no required role is supplied.
- **FR-009**: Platform admin status MUST remain separate from community membership role access and MUST NOT automatically grant community role access.
- **FR-010**: The system MUST provide reusable authorization helpers or filters that future endpoints can use for Owner, Teacher, and Student community roles.
- **FR-011**: `GET /api/users/me/communities` MUST require an authenticated user.
- **FR-012**: `GET /api/users/me/communities` MUST return only communities for the authenticated user.
- **FR-013**: `GET /api/users/me/communities` MUST return community id, name, slug, community status, user role, and membership status.
- **FR-014**: `GET /api/users/me/communities` MUST include Active Owner, Teacher, and Student memberships.
- **FR-015**: `GET /api/users/me/communities` MUST exclude Removed memberships.
- **FR-016**: `GET /api/users/me/communities` MUST exclude Pending memberships for access safety.
- **FR-017**: Existing platform-admin community management behavior MUST continue to rely on platform-admin identity and MUST NOT be replaced by community role checks.
- **FR-018**: The feature MUST NOT implement teacher management endpoints, student licenses, student management, grades, classes, analytics, owner dashboard, payment logic, or admin community creation/licensing.
- **FR-019**: All newly introduced responses and controlled failures MUST follow the product's existing response conventions.

### Key Entities

- **Community**: Existing B2B school/community account. This feature reads id, name, slug, and status for memberships visible to the current user.
- **CommunityUser**: Existing membership connecting a user to a community. This feature relies on CommunityId, UserId, Role, and Status to make access and role decisions.
- **User**: Existing shared login identity. This feature uses the authenticated user identity to resolve the current user's community memberships.
- **Community Access Decision**: A reusable result indicating whether a user has Active membership in a community.
- **Community Role Decision**: A reusable result indicating whether a user has Active membership and one of the required Owner, Teacher, or Student roles.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: 100% of checked users without active membership are denied community access.
- **SC-002**: 100% of Active Owner, Teacher, and Student memberships pass access checks for their own community.
- **SC-003**: 100% of Pending and Removed memberships fail access and role checks.
- **SC-004**: 100% of role checks fail when the user's active role is not in the required role set.
- **SC-005**: Authenticated users see only their own active community memberships in a single current-user community list.
- **SC-006**: Unauthenticated requests for the current user's communities are rejected.
- **SC-007**: Existing platform-admin community management remains usable without being converted to community role authorization.
- **SC-008**: The feature can be validated without implementing teacher management, student management, student licenses, grades, classes, analytics, dashboards, payments, or admin community setup workflows.

## Assumptions

- Admin Community Foundation has already created Communities, CommunityUsers, CommunityLicenses, role/status values, and platform-admin protected admin APIs.
- Active CommunityUser membership is the only valid community access signal for this feature.
- Pending memberships are excluded from the current user's community list to keep access behavior conservative and safe.
- Platform admins may still manage communities through existing admin endpoints, but community role checks are membership-based and separate.
- The current authentication token already contains enough identity information to resolve the current user.
- Future endpoints will use the reusable access and role checks rather than reimplementing membership queries.
