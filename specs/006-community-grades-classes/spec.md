# Feature Specification: Community Grades and Classes

**Feature Branch**: `006-community-grades-classes`

**Created**: 2026-06-26

**Status**: Draft

**Input**: User description: "Build grade and class management for Sprint Labs communities. Community Owners and Teachers can create grades, list grades with class counts, create classes under grades, list classes, update classes, and soft delete classes. Student licenses, student assignment to classes, student management, analytics, teacher dashboard, grade update/delete endpoints, import/export, and payment logic are out of scope."

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Create and List Grades (Priority: P1)

A community Owner or Teacher creates grades and views the community grade list so the community can organize future classes by grade level.

**Why this priority**: Grades are the parent structure for classes. Without grade creation and listing, class organization has no stable structure.

**Independent Test**: Can be fully tested by signing in as an active Owner or Teacher, creating grades for one community, listing grades, and confirming each returned grade includes its class count.

**Acceptance Scenarios**:

1. **Given** an authenticated user with an Active Owner membership, **When** the user creates a grade with name and sort order, **Then** the grade is created for that community and returned.
2. **Given** an authenticated user with an Active Teacher membership, **When** the user creates a grade with name and sort order, **Then** the grade is created for that community and returned.
3. **Given** existing grades for a community, **When** an active Owner or Teacher lists grades, **Then** the community's grades are returned with class counts.
4. **Given** a grade belongs to another community, **When** an Owner or Teacher lists grades for this community, **Then** the other community's grade is not returned.
5. **Given** an authenticated Student, Pending member, Removed member, or user with no membership, **When** the user attempts to create or list grades, **Then** access is denied.

---

### User Story 2 - Create and List Classes (Priority: P2)

A community Owner or Teacher creates classes under grades and lists classes so students can later be organized into the correct groups.

**Why this priority**: Classes are the actionable grouping unit for future student organization and assignments, and must be tied to valid grades within the same community.

**Independent Test**: Can be fully tested by creating grades, creating classes under those grades, listing all classes, filtering by grade, and confirming classes from other communities or deleted classes are excluded.

**Acceptance Scenarios**:

1. **Given** an authenticated Active Owner and a grade in the same community, **When** the user creates a class under that grade, **Then** the class is created and returned.
2. **Given** an authenticated Active Teacher and a grade in the same community, **When** the user creates a class under that grade, **Then** the class is created and returned.
3. **Given** a class creation request uses a grade from another community, **When** the request is submitted, **Then** the request is rejected and no class is created.
4. **Given** classes exist in a community, **When** an active Owner or Teacher lists classes without a grade filter, **Then** non-deleted classes for the community are returned.
5. **Given** classes exist under multiple grades, **When** an active Owner or Teacher lists classes with a grade filter, **Then** only non-deleted classes under that grade are returned.
6. **Given** a grade filter references a grade outside the community, **When** classes are listed, **Then** the request is rejected.

---

### User Story 3 - Update Classes (Priority: P3)

A community Owner or Teacher updates class names or moves classes between grades in the same community so class organization stays accurate as the school changes.

**Why this priority**: Class names and grade placement often change after initial setup. Updating classes keeps the structure useful without requiring deletion and recreation.

**Independent Test**: Can be fully tested by updating an existing class name, moving it to another grade in the same community, and confirming attempts to move it to another community's grade are rejected.

**Acceptance Scenarios**:

1. **Given** an authenticated Active Owner and an existing non-deleted class, **When** the user updates the class name, **Then** the updated class is returned and later lists show the new name.
2. **Given** an authenticated Active Teacher and an existing non-deleted class, **When** the user moves the class to another grade in the same community, **Then** the updated class is returned under the new grade.
3. **Given** the new grade belongs to another community, **When** the class update is submitted, **Then** the request is rejected and the class remains unchanged.
4. **Given** a class belongs to another community, **When** the user attempts to update it through this community, **Then** the request is rejected.
5. **Given** the class is already soft deleted, **When** the user attempts to update it, **Then** the request is rejected.

---

### User Story 4 - Soft Delete Classes (Priority: P4)

A community Owner or Teacher soft deletes a class so it no longer appears in active class lists or grade class counts while preserving the record.

**Why this priority**: Communities need to remove obsolete classes without hard deletion, which protects future auditability and avoids destructive data loss.

**Independent Test**: Can be fully tested by soft deleting an existing class and confirming it no longer appears in class lists or grade class counts while the stored class record remains.

**Acceptance Scenarios**:

1. **Given** an authenticated Active Owner and an existing non-deleted class in the community, **When** the user deletes the class, **Then** the class is soft deleted and not hard deleted.
2. **Given** an authenticated Active Teacher and an existing non-deleted class in the community, **When** the user deletes the class, **Then** the class is soft deleted.
3. **Given** a class has been soft deleted, **When** classes are listed, **Then** the class is not returned.
4. **Given** a class has been soft deleted, **When** grades are listed, **Then** that class is not included in the grade's class count.
5. **Given** an authenticated Student, Pending member, Removed member, platform admin without membership, or user with no membership, **When** the user attempts to delete a class, **Then** access is denied.

### Edge Cases

- A grade create request has an empty or whitespace-only name; the request is rejected.
- A grade create request has a negative sort order; the request is rejected.
- A class create or update request has an empty or whitespace-only name; the request is rejected.
- A class create or update request references a missing grade; the request is rejected.
- A class list grade filter references a missing grade or a grade in another community; the request is rejected.
- A class update or delete request references a missing class; the request is rejected.
- A class update or delete request references a class in another community; the request is rejected.
- A soft-deleted class is deleted again; the operation does not hard delete the class and does not change grade class counts incorrectly.
- A platform admin without an Active Owner or Teacher membership cannot manage grades or classes through this feature.
- Grade class counts exclude all soft-deleted classes.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The system MUST allow authenticated users with Active Owner or Active Teacher membership in a community to create grades for that community.
- **FR-002**: The system MUST deny grade creation to unauthenticated users, Students, Pending members, Removed members, users without membership, and platform admins without Active Owner or Teacher membership.
- **FR-003**: A grade create request MUST include a non-empty grade name.
- **FR-004**: A grade create request MUST include a non-negative sort order.
- **FR-005**: A successful grade creation MUST return the grade id, community id, name, sort order, and created date.
- **FR-006**: The system MUST allow authenticated Active Owners and Active Teachers to list grades for their community.
- **FR-007**: Grade listing MUST return only grades for the requested community.
- **FR-008**: Grade listing MUST include class count for each grade.
- **FR-009**: Grade class counts MUST exclude soft-deleted classes.
- **FR-010**: The system MUST allow authenticated Active Owners and Active Teachers to create classes for their community.
- **FR-011**: Class creation MUST require a non-empty class name.
- **FR-012**: Class creation MUST require a grade that exists in the same community.
- **FR-013**: Class creation MUST reject grades that are missing or belong to another community.
- **FR-014**: A successful class creation MUST return the class id, community id, grade id, name, status or deletion state, and created date.
- **FR-015**: The system MUST allow authenticated Active Owners and Active Teachers to list non-deleted classes for their community.
- **FR-016**: Class listing MUST exclude soft-deleted classes.
- **FR-017**: Class listing MUST support an optional grade filter.
- **FR-018**: Class listing with a grade filter MUST validate that the grade belongs to the requested community.
- **FR-019**: The system MUST allow authenticated Active Owners and Active Teachers to update non-deleted classes in their community.
- **FR-020**: Class update MUST allow changing class name.
- **FR-021**: Class update MUST allow moving a class to another grade only when the new grade belongs to the same community.
- **FR-022**: Class update MUST reject missing classes, classes in another community, soft-deleted classes, missing grades, and grades in another community.
- **FR-023**: The system MUST allow authenticated Active Owners and Active Teachers to soft delete classes in their community.
- **FR-024**: Class deletion MUST be soft deletion and MUST NOT hard delete the class record.
- **FR-025**: Soft-deleted classes MUST NOT appear in class lists.
- **FR-026**: Soft-deleted classes MUST NOT count toward grade class counts.
- **FR-027**: Student, Pending, Removed, no-membership, unauthenticated, and platform-admin-only users MUST NOT manage grades or classes through this feature.
- **FR-028**: The feature MUST NOT implement student licenses, student assignment to classes, student management, analytics, teacher dashboard, grade update/delete endpoints, import/export, or payment logic.

### Key Entities

- **Grade**: A community-owned grade grouping. Relevant information includes community id, name, sort order, and created date.
- **Class**: A community-owned class under a grade. Relevant information includes community id, grade id, name, created date, and soft-deletion state.
- **Community Membership**: The relationship that authorizes users. Only Active Owner and Active Teacher memberships can manage grades and classes.
- **Community**: The school or community that owns grades and classes and provides the tenant boundary for all operations.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: 100% of tested Active Owner and Active Teacher users can create valid grades in their community.
- **SC-002**: 100% of tested Student, Pending, Removed, no-membership, unauthenticated, and platform-admin-only users are denied grade and class management actions.
- **SC-003**: 100% of tested grade lists return only grades for the requested community and include class counts that exclude soft-deleted classes.
- **SC-004**: 100% of tested valid class creation requests create classes under grades in the same community.
- **SC-005**: 100% of tested class creation requests using grades from another community are rejected without creating a class.
- **SC-006**: 100% of tested class list requests exclude soft-deleted classes and correctly apply the optional grade filter.
- **SC-007**: 100% of tested class updates can rename or move a class within the same community and reject moves to grades from another community.
- **SC-008**: 100% of tested class deletions soft delete the class without hard deletion.
- **SC-009**: After soft deletion, 100% of tested deleted classes are absent from class lists and excluded from grade class counts.
- **SC-010**: The feature can be demonstrated without adding student assignment, student management, analytics, teacher dashboard, grade update/delete, import/export, payment, or license workflows.

## Assumptions

- Existing authentication provides a reliable current user identity for each request.
- Existing community role checks define valid Owner and Teacher access as Active membership with matching role.
- Platform-admin status remains separate and does not bypass community role checks.
- Grade names are not required to be globally unique unless later planning discovers an existing project convention.
- Class names are not required to be unique within a grade unless later planning discovers an existing project convention.
- Grade update and delete are intentionally not included.
- Soft delete for classes may use either a status value or an `IsDeleted` flag, whichever best matches existing project conventions during planning and implementation.
- Student assignment to classes will be handled by a later feature, so deleting a class does not need to move or validate students in this feature.
