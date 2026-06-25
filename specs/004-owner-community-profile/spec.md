# Feature Specification: Owner Community Profile

**Feature Branch**: `004-owner-community-profile`

**Created**: 2026-06-25

**Status**: Draft

**Input**: User description: "Build community profile viewing and editing for community owners. Active community members can view basic community information, while only active Owners can update the community name and unique slug. Platform admin access remains separate from community membership access."

## User Scenarios & Testing *(mandatory)*

### User Story 1 - View Community Profile (Priority: P1)

An authenticated community member views the basic profile of a community where they have an active membership so they can confirm which community they are working in.

**Why this priority**: Community details are the minimum context needed by any future Owner, Teacher, or Student experience. The read flow also establishes the access boundary used by the update flow.

**Independent Test**: Can be fully tested by creating active Owner, Teacher, and Student memberships, requesting the associated community profile for each user, and confirming that the same basic community details are returned.

**Acceptance Scenarios**:

1. **Given** an authenticated user with an Active Owner membership, **When** the user requests that community's profile, **Then** the community id, name, slug, and status are returned.
2. **Given** an authenticated user with an Active Teacher membership, **When** the user requests that community's profile, **Then** the community details are returned.
3. **Given** an authenticated user with an Active Student membership, **When** the user requests that community's profile, **Then** the community details are returned.
4. **Given** an authenticated user with no membership for the community, **When** the user requests the community profile, **Then** access is denied without exposing the community details.
5. **Given** an authenticated user whose membership is Pending or Removed, **When** the user requests the community profile, **Then** access is denied.
6. **Given** no authenticated user, **When** a community profile is requested, **Then** the request is rejected.

---

### User Story 2 - Update Community Profile as Owner (Priority: P2)

An authenticated community Owner updates the community name and slug so the community's basic identity remains accurate.

**Why this priority**: Owners need limited self-service over community identity without gaining access to platform-admin setup, licensing, or member-management capabilities.

**Independent Test**: Can be fully tested by signing in as an active Owner, changing the name and slug, and confirming the updated profile is returned and visible in a later profile read.

**Acceptance Scenarios**:

1. **Given** an authenticated user with an Active Owner membership and valid profile values, **When** the user updates the community profile, **Then** the new name and normalized slug are saved and returned.
2. **Given** an active Owner keeps the community's current slug, **When** the profile is updated, **Then** the update succeeds.
3. **Given** an active Owner submits a slug already used by another community, **When** the profile is updated, **Then** the request is rejected and the original community profile remains unchanged.
4. **Given** an authenticated Teacher or Student with Active membership, **When** the user attempts to update the profile, **Then** access is denied and no profile values change.
5. **Given** an Owner membership with Pending or Removed status, **When** the user attempts to update the profile, **Then** access is denied and no profile values change.
6. **Given** an authenticated platform admin without an Active Owner membership, **When** the user attempts to update through the owner community profile flow, **Then** access is denied.
7. **Given** no authenticated user, **When** a community profile update is attempted, **Then** the request is rejected without changing the community.

### Edge Cases

- The requested community does not exist; the operation is rejected without revealing or changing unrelated community data.
- The community id is missing, invalid, or non-positive; the operation is rejected as invalid.
- The submitted name or slug is empty or contains only whitespace; the update is rejected.
- A submitted slug differs from the current slug only by casing or surrounding whitespace; it is treated as the same normalized slug and the update succeeds.
- A submitted slug differs from another community's slug only by casing or surrounding whitespace; it is treated as a duplicate and the update is rejected.
- An Owner loses active membership between viewing and submitting an update; authorization is checked again and the update is denied.
- A community is Suspended while the membership remains Active; its status is still returned, and access continues to follow active membership because this feature does not define a separate community-status restriction.
- Two Owners attempt conflicting profile updates; each completed update must preserve slug uniqueness, and a rejected update must not partially change the profile.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The system MUST allow an authenticated user to request the basic profile of a specific community.
- **FR-002**: A community profile read MUST succeed only when the requesting user has an Active membership in that community.
- **FR-003**: Active Owner, Teacher, and Student memberships MUST each grant permission to view the community profile.
- **FR-004**: Pending and Removed memberships MUST NOT grant permission to view the community profile.
- **FR-005**: A successful community profile read MUST return the community id, name, slug, and status.
- **FR-006**: A community profile read MUST NOT expose details for a community where the requesting user lacks active membership.
- **FR-007**: The system MUST allow an authenticated user with an Active Owner membership to update the name and slug of that community.
- **FR-008**: Active Teacher and Student memberships MUST NOT grant permission to update the community profile.
- **FR-009**: Pending and Removed Owner memberships MUST NOT grant permission to update the community profile.
- **FR-010**: Platform-admin status alone MUST NOT grant permission to view or update a community through this membership-based feature.
- **FR-011**: An update request MUST provide a non-empty name and non-empty slug.
- **FR-012**: The system MUST trim surrounding whitespace from submitted names and slugs.
- **FR-013**: The system MUST normalize submitted slugs to lowercase before uniqueness comparison and storage.
- **FR-014**: A community slug MUST remain unique across all communities.
- **FR-015**: An update MUST allow the community to keep its own current normalized slug.
- **FR-016**: An update MUST reject a normalized slug used by any other community.
- **FR-017**: A rejected update MUST leave the existing community name and slug unchanged.
- **FR-018**: A successful update MUST record that the community was updated using the product's existing update timestamp behavior.
- **FR-019**: A successful update MUST return the updated community id, name, slug, and status.
- **FR-020**: Authorization MUST be evaluated against the requesting user's current membership at the time of each read or update.
- **FR-021**: Unauthenticated profile reads and updates MUST be rejected.
- **FR-022**: Requests for a missing community MUST produce a controlled absence outcome.
- **FR-023**: Profile reads and updates MUST follow the product's existing success and controlled-error response conventions.
- **FR-024**: The feature MUST reuse the existing community access and role rules rather than creating a separate meaning of active access or Owner membership.
- **FR-025**: The feature MUST NOT add platform-admin community management, teacher or student management, grades, classes, student licenses, analytics, dashboards, license management, or community member management.

### Key Entities

- **Community**: The school or community whose basic identity is being viewed or updated. Relevant information is id, name, slug, status, and the time it was last updated.
- **Community Membership**: The relationship between a user and community. Its current role and status determine whether the user can view or update the profile.
- **User**: The authenticated person requesting the operation. Platform-admin status remains distinct from community membership authorization.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: 100% of tested Active Owner, Teacher, and Student memberships can view their own community's basic profile.
- **SC-002**: 100% of tested users without Active membership are denied community profile access without receiving the profile data.
- **SC-003**: 100% of tested Active Owners can complete a valid community name and slug update in one request.
- **SC-004**: 100% of tested Teacher, Student, Pending Owner, and Removed Owner update attempts are denied without changing community data.
- **SC-005**: 100% of tested duplicate slugs belonging to another community are rejected, while the current community's own normalized slug remains reusable.
- **SC-006**: Every successful update is visible in the next community profile read and includes the updated name and slug.
- **SC-007**: No tested platform admin without Active community membership or Active Owner role gains membership-based view or update access.
- **SC-008**: The feature can be demonstrated and validated without introducing any teacher, student, class, grade, license-management, analytics, dashboard, payment, or member-management workflow.

## Assumptions

- Existing authentication provides a reliable current user identity for each request.
- Existing community access behavior defines valid access as an Active membership for the exact user and community.
- Existing community role behavior defines update permission as an Active Owner membership for the exact user and community.
- Name and slug are both required in the update request; partial updates of only one field are not included in this feature.
- Slug normalization follows the established community creation behavior: trim surrounding whitespace and convert to lowercase.
- Community status is returned for display but does not independently override active membership access in this feature.
- Existing suspended-user/session behavior continues to apply before community-specific authorization.
- No audit history beyond the existing community update timestamp is required.
