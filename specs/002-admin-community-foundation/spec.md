# Feature Specification: Admin Community Foundation

**Feature Branch**: `002-admin-community-foundation`

**Created**: 2026-06-23

**Status**: Draft

**Input**: User description: "Build the platform admin foundation for creating and managing B2B schools/communities in Sprint Labs. Allow a platform admin to create a community/school, assign an owner, and configure license limits. This feature covers Communities, CommunityUsers, CommunityLicenses, platform admin authorization for admin actions, creating communities, listing communities, assigning an owner, and creating/updating community license limits. Teachers, students, grades, classes, student licenses, analytics, dashboards, owner self-management, invite emails, and payment integration are out of scope."

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Create a School Community (Priority: P1)

A platform admin creates a new school or community so Sprint Labs can represent a B2B customer account before teachers or students are onboarded.

**Why this priority**: Community creation is the foundation for all later B2B setup. Without a community record, owners and license limits have nowhere to attach.

**Independent Test**: Can be fully tested by signing in as a platform admin, creating a community with a name and slug, and confirming the created community is returned with active status.

**Acceptance Scenarios**:

1. **Given** an authenticated platform admin and an unused slug, **When** the admin creates a community, **Then** the community is created with Active status and returned to the admin.
2. **Given** an authenticated platform admin and an existing slug, **When** the admin creates another community with that slug, **Then** the request is rejected without creating a duplicate.
3. **Given** no authenticated user, **When** a community creation action is attempted, **Then** the request is rejected before any community is created.
4. **Given** an authenticated user who is not a platform admin, **When** a community creation action is attempted, **Then** the request is rejected before any community is created.

---

### User Story 2 - List School Communities (Priority: P2)

A platform admin lists existing communities and sees enough owner and license summary information to understand each account at a glance.

**Why this priority**: Admins need visibility into existing communities to avoid duplicates, confirm setup state, and manage accounts.

**Independent Test**: Can be fully tested by creating communities with and without owners or license records, then listing communities as a platform admin and verifying the summaries.

**Acceptance Scenarios**:

1. **Given** one or more communities exist, **When** a platform admin lists communities, **Then** each community is returned with its status, license summary when available, and owner summary when available.
2. **Given** a community has no owner yet, **When** a platform admin lists communities, **Then** the community is still returned with no owner summary.
3. **Given** a community has no license record yet, **When** a platform admin lists communities, **Then** the community is still returned with no license summary.
4. **Given** an authenticated non-admin user, **When** the user attempts to list communities, **Then** the request is rejected.

---

### User Story 3 - Assign a Community Owner (Priority: P3)

A platform admin assigns an owner to a community by email so a school can have a named responsible account before owner self-management exists.

**Why this priority**: B2B onboarding requires a durable owner relationship, even though owner-facing workflows are out of scope for this foundation.

**Independent Test**: Can be fully tested by assigning an owner email to an existing community, repeating the same assignment, and confirming only one active owner membership exists for that user and community.

**Acceptance Scenarios**:

1. **Given** an existing community and an email that belongs to an existing user, **When** a platform admin assigns that email as owner, **Then** an active owner membership is created or updated for that community.
2. **Given** an existing community and an email with no existing user, **When** a platform admin assigns that email as owner, **Then** a user is created and assigned as an active owner.
3. **Given** the same user is assigned as owner for the same community more than once, **When** the assignment is repeated, **Then** no duplicate membership row is created.
4. **Given** the target community does not exist, **When** a platform admin assigns an owner, **Then** the request is rejected without creating an orphan membership.

---

### User Story 4 - Configure Community License Limits (Priority: P4)

A platform admin creates or updates a community's license limits so the platform can enforce future teacher and student capacity rules.

**Why this priority**: License limits are needed before teacher and student onboarding, but actual teacher and student license consumption is out of scope.

**Independent Test**: Can be fully tested by configuring limits for an existing community, updating those limits, and confirming usage counts are preserved.

**Acceptance Scenarios**:

1. **Given** an existing community without a license record, **When** a platform admin configures license limits, **Then** a license record is created and returned.
2. **Given** an existing community with a license record, **When** a platform admin updates license limits, **Then** the configured limits are updated and returned.
3. **Given** a license record already has used student or teacher counts, **When** limits are updated, **Then** used counts are not manually decreased or overwritten by the limit update.
4. **Given** the target community does not exist, **When** a platform admin configures license limits, **Then** the request is rejected without creating an orphan license.

### Edge Cases

- A platform admin attempts any admin community action while authenticated as a suspended user; access is rejected according to the platform's controlled access behavior.
- A community slug differs only by casing or surrounding whitespace from an existing slug; duplicate prevention treats it as the same slug.
- Owner assignment uses an email that differs only by casing or surrounding whitespace from an existing user email; the existing user is reused.
- Owner assignment is repeated after an existing membership was previously removed; the membership is restored or updated to active owner rather than duplicated.
- License limits are set below current usage counts; the update is rejected because it would make the account overused immediately.
- License limit values are negative or otherwise invalid; the request is rejected without changing existing limits.
- A community exists without an owner or license during staged setup; listing communities still succeeds and clearly shows missing summaries.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The system MUST allow only authenticated platform admins to perform admin community management actions.
- **FR-002**: The system MUST reject unauthenticated admin community management attempts.
- **FR-003**: The system MUST reject authenticated users who are not platform admins from admin community management actions.
- **FR-004**: The system MUST represent a community or school account with id, name, slug, status, created timestamp, and updated timestamp.
- **FR-005**: The system MUST support community statuses of Active and Suspended.
- **FR-006**: The system MUST create new communities with Active status.
- **FR-007**: The system MUST require each community slug to be unique.
- **FR-008**: The system MUST reject duplicate community slugs without creating another community.
- **FR-009**: The system MUST allow platform admins to list communities.
- **FR-010**: The community list MUST include each community's basic details, owner summary when available, and license summary when available.
- **FR-011**: The system MUST represent a community membership connecting one user to one community with a role, status, created timestamp, and updated timestamp.
- **FR-012**: The system MUST support community membership roles of Owner, Teacher, and Student while this feature only creates or updates Owner memberships.
- **FR-013**: The system MUST support community membership statuses of Active, Pending, and Removed.
- **FR-014**: The system MUST allow platform admins to assign an owner to an existing community by email and name.
- **FR-015**: Owner assignment MUST find an existing user by email before creating a new user.
- **FR-016**: Owner assignment MUST create a user with the supplied email and name when no user with that email exists.
- **FR-017**: Owner assignment MUST create or update an active Owner membership for the user and community.
- **FR-018**: Owner assignment MUST NOT create duplicate membership rows for the same user and community.
- **FR-019**: Owner assignment MUST reject requests for communities that do not exist.
- **FR-020**: The system MUST represent a community license record linked to one community with maximum student count, used student count, maximum teacher count, used teacher count, student email change limit, created timestamp, and updated timestamp.
- **FR-021**: Each community MUST have no more than one community license record.
- **FR-022**: The system MUST allow platform admins to create license limits for a community that does not yet have a license record.
- **FR-023**: The system MUST allow platform admins to update license limits for a community that already has a license record.
- **FR-024**: License limit updates MUST preserve existing used student and used teacher counts unless a separate usage workflow changes them.
- **FR-025**: License limit updates MUST reject maximum values that are lower than the corresponding current used counts.
- **FR-026**: License limit values and student email change limits MUST be non-negative whole numbers.
- **FR-027**: License configuration MUST reject requests for communities that do not exist.
- **FR-028**: The system MUST NOT implement teacher onboarding, student onboarding, grades, classes, student licenses, analytics, community dashboards, owner self-management, invite emails, or payment integration in this feature.
- **FR-029**: All admin community responses and controlled failures MUST follow the product's existing response conventions.

### Key Entities

- **Community**: A B2B school or community account that can later contain owners, teachers, students, classes, and related operations. Key information includes id, name, slug, status, and creation/update timestamps.
- **CommunityUser**: A membership connecting a user to a community. Key information includes id, community, user, role, status, and creation/update timestamps. This feature only creates or updates owner memberships.
- **CommunityLicense**: License limits for one community. Key information includes maximum student count, used student count, maximum teacher count, used teacher count, student email change limit, and creation/update timestamps.
- **User**: Existing shared login identity from the User Identity Foundation. Platform admin access is determined by the user's platform-admin flag, and owner assignment reuses or creates users by email.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: 100% of unauthenticated admin community management attempts are rejected before changing community, membership, or license data.
- **SC-002**: 100% of authenticated non-admin admin community management attempts are rejected before changing community, membership, or license data.
- **SC-003**: A platform admin can create a new community with an unused slug in a single request.
- **SC-004**: 100% of duplicate-slug creation attempts are rejected without creating another community.
- **SC-005**: A platform admin can list all communities and see owner and license summaries for configured communities.
- **SC-006**: A platform admin can assign an owner by email in a single request, whether or not a user already exists for that email.
- **SC-007**: Repeating the same owner assignment for the same user and community creates zero duplicate memberships.
- **SC-008**: A platform admin can create or update community license limits in a single request while preserving existing usage counts.
- **SC-009**: The feature can be validated without any teacher, student, grade, class, student-license, dashboard, invitation, analytics, or payment workflow.

## Assumptions

- The User Identity Foundation is already available and user records include email, name, platform-admin flag, and status.
- Platform-admin authorization is based on the existing user's platform-admin flag.
- Admin community management is performed by trusted internal platform administrators, not by community owners.
- Email matching for owner assignment is case-insensitive and ignores surrounding whitespace.
- Slug uniqueness is case-insensitive and ignores surrounding whitespace.
- Creating a user during owner assignment does not send invitations or require the owner to complete onboarding in this feature.
- Teacher and student roles are included in membership role values because they are part of the planned model, but this feature does not create teacher or student memberships.
- Used student and teacher counts start at zero when a license record is first created.
