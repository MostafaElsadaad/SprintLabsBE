# Feature Specification: Owner Student License Management

**Feature Branch**: `007-owner-student-license-management`

**Created**: 2026-06-27

**Status**: Draft

**Input**: User description: "Build student license management for community owners. Allow a community Owner to add, list, update, and revoke student licenses using the community license capacity. This includes adding student licenses by email, listing student licenses, updating pending student license email, revoking student licenses, enforcing student license limits, and validating grade/class assignment. Student login activation, accepting invitations, email sending, dashboards, analytics, bulk import, payments, parent accounts, teacher management, and grade/class management are out of scope."

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Add Student Licenses (Priority: P1)

A community Owner adds a student license for a student email and assigns it to an existing grade and class so the community can reserve one student seat for later activation.

**Why this priority**: Adding a license is the core business action. Without it, owners cannot allocate student seats or use purchased student capacity.

**Independent Test**: Can be fully tested by signing in as an active Owner, adding a student license with a valid email, grade, and class, and confirming the license is pending and community student usage increases by one.

**Acceptance Scenarios**:

1. **Given** an authenticated user with an Active Owner membership and available student capacity, **When** the user adds a student license with a valid email, grade, and class in the community, **Then** a Pending student license is created and used student count increases by one.
2. **Given** the community student capacity is full, **When** the Owner tries to add another student license, **Then** the request is rejected and used student count is unchanged.
3. **Given** the grade or class belongs to another community, **When** the Owner submits the license, **Then** the request is rejected and no license is created.
4. **Given** the class does not belong to the selected grade, **When** the Owner submits the license, **Then** the request is rejected and no license is created.
5. **Given** a Pending or Active student license already exists for the same email in the same community, **When** the Owner adds another license for that email, **Then** the duplicate is rejected.

---

### User Story 2 - List Student Licenses (Priority: P2)

A community Owner lists student licenses so they can review invited students, assigned grades/classes, status, email change usage, assignment history, and activation state.

**Why this priority**: Owners need visibility into allocated seats before they can manage corrections or revocations safely.

**Independent Test**: Can be fully tested by creating licenses with different statuses, grades, classes, and emails, then listing with and without filters and confirming only matching licenses from the Owner's community are returned.

**Acceptance Scenarios**:

1. **Given** student licenses exist for a community, **When** an active Owner lists licenses, **Then** the response includes license id, email, status, grade, class, email change count, assigned-by user, activation date when present, and created date.
2. **Given** licenses exist across multiple communities, **When** an Owner lists one community's licenses, **Then** licenses from other communities are not returned.
3. **Given** filters for status, grade, class, or search text, **When** the Owner lists licenses, **Then** only licenses matching those filters are returned.
4. **Given** a non-Owner user, inactive member, or user without membership, **When** they try to list student licenses, **Then** access is denied.

---

### User Story 3 - Update Pending Student Licenses (Priority: P3)

A community Owner corrects a pending student license email or changes its grade/class assignment before the student activates the license.

**Why this priority**: Email typos and class assignment changes are common before student onboarding, but changes must be bounded to avoid abuse and preserve seat accounting.

**Independent Test**: Can be fully tested by creating a Pending license, changing its email within the allowed limit, updating its grade/class to another valid pair, and confirming used student count is unchanged.

**Acceptance Scenarios**:

1. **Given** a Pending student license and remaining email changes, **When** the Owner changes the email to a non-duplicate email, **Then** the license email is updated and email change count increases by one.
2. **Given** a Pending student license has reached the email change limit, **When** the Owner tries to change the email, **Then** the request is rejected and the license is unchanged.
3. **Given** an Active or Revoked student license, **When** the Owner tries to change the email, **Then** the request is rejected.
4. **Given** a valid same-community grade/class pair, **When** the Owner updates the license assignment, **Then** the grade/class assignment is updated and used student count is unchanged.
5. **Given** a target grade/class is missing, outside the community, or mismatched, **When** the Owner updates the license, **Then** the request is rejected.

---

### User Story 4 - Revoke Student Licenses (Priority: P4)

A community Owner revokes a student license so the student seat is released without deleting license history.

**Why this priority**: Owners need to recover capacity and remove access when students leave or an invitation was created by mistake.

**Independent Test**: Can be fully tested by revoking Pending and Active licenses, confirming status becomes Revoked, used student count decreases only for previously counted licenses, and linked student community access is removed for active licenses.

**Acceptance Scenarios**:

1. **Given** a Pending student license, **When** the Owner revokes it, **Then** the license status becomes Revoked and used student count decreases by one.
2. **Given** an Active student license, **When** the Owner revokes it, **Then** the license status becomes Revoked, used student count decreases by one, and the linked Student community access is removed or marked removed.
3. **Given** a Revoked student license, **When** the Owner revokes it again, **Then** the operation does not hard delete the license and does not decrease used student count again.
4. **Given** a linked user has other memberships, **When** one active student license is revoked, **Then** unrelated community memberships are not affected.

### Edge Cases

- A license add or update request has an invalid, empty, or whitespace-only email.
- A license add request is made when the community has no configured student license capacity.
- A license add request is made when used student count already equals or exceeds maximum student capacity.
- A duplicate email exists with Revoked status only; the Owner may create a new Pending license for that email.
- A duplicate email differs only by casing or whitespace.
- A grade belongs to the community but the selected class belongs to another grade.
- A grade or class has been deleted or is otherwise inactive.
- The requested license id belongs to another community.
- A Pending or Removed Owner attempts to manage student licenses.
- A Teacher, Student, platform-admin-only user, or no-membership user attempts to manage student licenses.
- Used student count is already inconsistent with license rows; revocation must not reduce it below zero.
- Updating only grade/class should not consume an email change.
- Updating email to the same normalized value should not consume an email change.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The system MUST allow only authenticated users with Active Owner membership in a community to manage student licenses for that community.
- **FR-002**: The system MUST deny student license management to Teachers, Students, Pending members, Removed members, users without membership, and platform-admin-only users.
- **FR-003**: The system MUST allow an Owner to add a student license by email when the community has available student capacity.
- **FR-004**: A student license add request MUST include a valid email, grade, and class.
- **FR-005**: The system MUST validate that the selected grade belongs to the route community.
- **FR-006**: The system MUST validate that the selected class belongs to the route community.
- **FR-007**: The system MUST validate that the selected class belongs to the selected grade.
- **FR-008**: A new student license MUST be created as Pending and MUST reserve one student seat.
- **FR-009**: A new student license MUST store the assigning Owner, created date, zero email changes, and no activation date.
- **FR-010**: The system MUST reject adding a student license when used student count is greater than or equal to maximum student capacity.
- **FR-011**: The system MUST reject duplicate non-revoked student license emails within the same community.
- **FR-012**: The system MUST NOT create student community access when creating a Pending student license.
- **FR-013**: The system MUST allow an Owner to list student licenses for their community.
- **FR-014**: Student license listing MUST support filtering by status, grade, class, and email search text.
- **FR-015**: Student license listing MUST return license id, email, status, grade, class, email change count, assigning user, activation date when present, and created date.
- **FR-016**: Student license listing MUST return only licenses from the requested community.
- **FR-017**: The system MUST allow an Owner to update grade/class assignment for a license in the same community when the target grade/class pair is valid.
- **FR-018**: The system MUST allow changing email only while a student license is Pending.
- **FR-019**: The system MUST enforce the community's student email change limit for Pending license email changes.
- **FR-020**: The system MUST increment email change count only when the normalized email value actually changes.
- **FR-021**: The system MUST reject email changes for Active or Revoked licenses.
- **FR-022**: The system MUST reject updates that would create a duplicate non-revoked email in the same community.
- **FR-023**: Student license updates MUST NOT change used student count.
- **FR-024**: The system MUST allow an Owner to revoke a student license in the same community.
- **FR-025**: Revocation MUST set license status to Revoked and MUST NOT hard delete the license.
- **FR-026**: Revoking a Pending or Active student license MUST decrease used student count by one without allowing used count to fall below zero.
- **FR-027**: Revoking a Revoked student license MUST NOT decrease used student count again.
- **FR-028**: Revoking an Active license with linked student access MUST remove or mark removed only the matching Student access for that community.
- **FR-029**: Revocation MUST NOT remove unrelated user memberships or non-student roles.
- **FR-030**: The feature MUST NOT implement student login activation, student invitation acceptance, email sending, student dashboards, analytics, bulk import, payments, parent accounts, teacher management, or grade/class management.

### Key Entities *(include if feature involves data)*

- **Student License**: A community-owned student seat assigned to an email, grade, and class. It tracks pending/active/revoked status, optional linked user or player profile, email change usage, assigning Owner, activation date, creation date, and last update date.
- **Community License**: The community's capacity record. It defines maximum student seats, current used student seats, and the student email change limit.
- **Grade**: The community-owned grade used to organize a student license assignment.
- **Class**: The community-owned class under a grade used to organize a student license assignment.
- **Community Membership**: The relationship that authorizes Owners and may hold linked Student access that must be removed when an active student license is revoked.
- **User**: A login identity that may be linked to an active student license and related Student community access.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: 100% of tested Active Owners can add valid student licenses when capacity is available.
- **SC-002**: 100% of tested non-Owner, inactive, and no-membership users are denied student license management actions.
- **SC-003**: 100% of tested full-capacity add attempts are rejected without changing used student count.
- **SC-004**: 100% of tested grade/class mismatches or cross-community grade/class selections are rejected without creating or updating a license.
- **SC-005**: 100% of tested duplicate non-revoked email attempts are rejected within the same community.
- **SC-006**: Owners can find expected licenses through status, grade, class, and search filters in all tested cases.
- **SC-007**: Pending license email updates succeed only within the configured email change limit and correctly update email change count.
- **SC-008**: Active and Revoked license email changes are rejected in all tested cases.
- **SC-009**: Revoking Pending or Active licenses reduces used student count exactly once and preserves the license record.
- **SC-010**: Revoking Active licenses removes matching Student community access without affecting unrelated memberships in all tested cases.
- **SC-011**: The complete feature can be demonstrated without student login activation, email delivery, dashboards, analytics, bulk import, payments, teacher management, or grade/class management.

## Assumptions

- Existing authentication reliably identifies the current user for each request.
- Existing community role checks define an Active Owner as the only valid actor for student license management.
- Platform-admin status remains separate and does not bypass Owner membership requirements.
- Email uniqueness for licenses is evaluated after trimming and normalizing email casing.
- Pending and Active student licenses count as used student seats; Revoked licenses do not.
- Existing grade and class records are the source of truth for valid grade/class assignment.
- A class that is deleted or inactive should not be accepted for new or updated license assignment.
- Student activation will be handled by a later feature and must reuse the reserved seat rather than incrementing used student count again.
- Email sending and invitation delivery are intentionally excluded; adding a license only records the pending seat.
