# Feature Specification: Community Student Viewing

**Feature Branch**: `009-community-student-viewing`

**Created**: 2026-07-02

**Status**: Draft

**Input**: User description: "Feature name: Community Student Viewing. Build student viewing endpoints for community Owners and Teachers. Goal: Allow community Owners and Teachers to view students in their community, including list filtering, pagination, and student detail. This feature only covers GET /api/communities/{communityId}/students, GET /api/communities/{communityId}/students/{playerProfileId}, student list filters, pagination using the existing project PagedRequest/PagedResponse pattern, and student detail with placeholder analytics object. Creating student licenses, updating student licenses, revoking student licenses, student login activation, student analytics calculation, assignments, reports, parent accounts, and export/import are out of scope."

## User Scenarios & Testing *(mandatory)*

### User Story 1 - List Community Students (Priority: P1)

A community Owner or Teacher views a paginated student list for their community so they can see pending and active students assigned through student licenses.

**Why this priority**: Student visibility is the core value of the feature. Owners and Teachers need a reliable student roster before later assignment, reporting, or analytics work can be useful.

**Independent Test**: Can be fully tested by signing in as an active Owner or Teacher, requesting the student list for a community with pending and active student licenses, and confirming the response contains only students from that community with the required roster fields.

**Acceptance Scenarios**:

1. **Given** an authenticated user with Active Owner membership in a community, **When** the user views the community student list, **Then** the response returns a paginated list of student license records for that community.
2. **Given** an authenticated user with Active Teacher membership in a community, **When** the user views the community student list, **Then** the response returns a paginated list of student license records for that community.
3. **Given** pending student licenses exist without linked user or player profiles, **When** an active Owner or Teacher views the student list, **Then** those pending students appear with nullable user and player profile fields.
4. **Given** student licenses exist in another community, **When** an active Owner or Teacher views this community's student list, **Then** students from other communities are not returned.

---

### User Story 2 - Filter, Search, and Page Students (Priority: P2)

A community Owner or Teacher narrows the roster by grade, class, status, or search text and navigates through pages so they can find the right students quickly.

**Why this priority**: Communities may have many students. Filtering and pagination make the list usable for real classroom and school workflows.

**Independent Test**: Can be fully tested by creating student licenses across different statuses, grades, classes, emails, users, and player names, then applying each filter and confirming only matching records are returned with correct page metadata.

**Acceptance Scenarios**:

1. **Given** student licenses exist across multiple grades, **When** an active Owner or Teacher filters by a valid grade in the community, **Then** only students assigned to that grade are returned.
2. **Given** student licenses exist across multiple classes, **When** an active Owner or Teacher filters by a valid class in the community, **Then** only students assigned to that class are returned.
3. **Given** both grade and class filters are provided, **When** the class belongs to the selected grade in the community, **Then** only students matching both filters are returned.
4. **Given** student licenses exist with different statuses, **When** an active Owner or Teacher filters by status, **Then** only students with that license status are returned.
5. **Given** search text matches a license email, linked user email, or linked user/player name, **When** an active Owner or Teacher searches the list, **Then** matching students are returned.
6. **Given** more students exist than fit on one page, **When** an active Owner or Teacher requests a page, **Then** the response includes only that page of students and pagination information.

---

### User Story 3 - View Student Detail (Priority: P3)

A community Owner or Teacher opens a student's detail view to see profile, license, class placement, progression, and a placeholder analytics summary.

**Why this priority**: Detail view gives staff enough context to inspect an individual student while keeping real analytics out of scope until a later feature.

**Independent Test**: Can be fully tested by opening a detail request for a player profile linked to a non-revoked student license in the community and confirming the response includes profile, license, grade/class, progression, and placeholder analytics fields.

**Acceptance Scenarios**:

1. **Given** an active or non-revoked student license in the community is linked to a player profile, **When** an active Owner or Teacher views detail for that player profile, **Then** the response returns the student's detail information.
2. **Given** a player profile is linked only to a student license in another community, **When** an active Owner or Teacher requests that player profile through this community, **Then** the student is not returned.
3. **Given** no matching non-revoked student license exists in the route community, **When** detail is requested for a player profile, **Then** the request returns a not-found outcome.
4. **Given** student detail is returned, **When** the response is inspected, **Then** analytics are present only as placeholder null, zero, or default values.

---

### User Story 4 - Enforce Student Viewing Permissions (Priority: P4)

The system prevents students, inactive members, platform-admin-only users, and users without community membership from viewing community student rosters or student details.

**Why this priority**: Student roster visibility is sensitive community data. Access must be limited to active staff roles in the community.

**Independent Test**: Can be fully tested by attempting list and detail requests as a Student, Pending member, Removed member, platform-admin-only user, unauthenticated user, and user with no community membership, then confirming access is denied or unauthenticated as appropriate.

**Acceptance Scenarios**:

1. **Given** an unauthenticated request, **When** the student list or detail is requested, **Then** the request is rejected as unauthenticated.
2. **Given** an authenticated Student member, **When** the student list or detail is requested, **Then** access is denied.
3. **Given** an authenticated Pending or Removed member, **When** the student list or detail is requested, **Then** access is denied.
4. **Given** an authenticated user with no membership in the community, **When** the student list or detail is requested, **Then** access is denied.
5. **Given** an authenticated platform admin without Active Owner or Teacher membership in the community, **When** the student list or detail is requested, **Then** access is denied.

### Edge Cases

- A grade filter references a missing grade or a grade in another community.
- A class filter references a missing class, a deleted class, or a class in another community.
- Both grade and class filters are provided but the class does not belong to the selected grade.
- A status filter uses an unsupported student license status value.
- Search text is empty, whitespace-only, mixed case, or matches only part of an email or name.
- Pending student licenses have no linked user, no linked player profile, no name, and no avatar.
- Active student licenses have linked profile information, but linked profile fields may still have nullable optional values.
- The requested page has no results because the page number is beyond the available result set.
- Multiple licenses have similar emails or names; search results must remain scoped to the route community.
- A player profile belongs to a student in another community.
- A player profile is linked to a revoked license in the requested community.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The system MUST require authentication before allowing community student list or detail access.
- **FR-002**: The system MUST allow authenticated users with Active Owner membership in the route community to view the community student list and student detail.
- **FR-003**: The system MUST allow authenticated users with Active Teacher membership in the route community to view the community student list and student detail.
- **FR-004**: The system MUST deny community student viewing to Students, Pending members, Removed members, users without community membership, and platform-admin-only users without Active Owner or Teacher membership in the route community.
- **FR-005**: The student list MUST return only student license records that belong to the route community.
- **FR-006**: The student list MUST include Pending, Active, and Revoked student licenses unless a status filter narrows the result set.
- **FR-007**: The student list MUST support filtering by student license status.
- **FR-008**: The student list MUST support filtering by grade when the grade belongs to the route community.
- **FR-009**: The student list MUST support filtering by class when the class belongs to the route community.
- **FR-010**: When both grade and class filters are provided, the system MUST validate that the class belongs to the selected grade and route community.
- **FR-011**: The system MUST reject grade or class filters that reference missing records, records from another community, or invalid grade/class combinations.
- **FR-012**: The student list MUST support search by student license email, linked user email, linked user name, and linked player name when those values are available.
- **FR-013**: Search MUST allow pending students without linked user or player profile data to remain searchable by license email.
- **FR-014**: The student list MUST be paginated and MUST return pagination information with each list response.
- **FR-015**: Each student list item MUST include license id, email, status, nullable user id, nullable player profile id, nullable player name, nullable avatar URL, grade id, grade name, class id, class name, nullable activation date, and created date.
- **FR-016**: Student list item user and player profile fields MUST be nullable because Pending students may not have activated accounts.
- **FR-017**: Student detail MUST locate a non-revoked student license in the route community linked to the requested player profile.
- **FR-018**: Student detail MUST NOT return a player profile unless the linked non-revoked student license belongs to the route community.
- **FR-019**: Student detail MUST return a not-found outcome when no matching non-revoked student license exists in the route community.
- **FR-020**: Student detail MUST include user id, player profile id, name, email, nullable avatar URL, gold, experience, level, license status, grade id, grade name, class id, class name, nullable activation date, created date, and analytics placeholder object.
- **FR-021**: The analytics object MUST contain placeholder null, zero, or default values only and MUST NOT calculate real analytics in this feature.
- **FR-022**: The feature MUST NOT create, update, revoke, or activate student licenses.
- **FR-023**: The feature MUST NOT add new database tables or change existing stored student, license, community, grade, class, or player data.
- **FR-024**: The feature MUST NOT implement assignments, reports, parent accounts, export/import, or real analytics calculation.

### Key Entities *(include if feature involves data)*

- **Student License**: The community-owned student roster source. It supplies license id, email, status, grade/class assignment, activation date, created date, and optional links to user and player profile records.
- **User**: The login identity that may be linked to an active student license. User information may contribute email and name when available.
- **Player Profile**: The game profile linked to an activated student license. It supplies profile id, name, avatar, and progression values for detail.
- **Grade**: The community-owned grade used for filtering and display.
- **Class**: The community-owned class used for filtering and display, and associated with a grade.
- **Community Membership**: The relationship that authorizes Owners and Teachers to view students and prevents unauthorized roles from accessing roster data.
- **Student Analytics Placeholder**: A non-calculated detail object reserved for future analytics fields and populated only with default values in this feature.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: 100% of tested Active Owners can view paginated student lists for their communities.
- **SC-002**: 100% of tested Active Teachers can view paginated student lists for their communities.
- **SC-003**: 100% of tested Student, Pending, Removed, no-membership, unauthenticated, and platform-admin-only users are prevented from viewing community student data.
- **SC-004**: 100% of tested list responses contain only students from the requested community.
- **SC-005**: 100% of tested grade, class, status, and search filters return the expected matching students.
- **SC-006**: 100% of tested invalid grade/class filters are rejected without returning cross-community or mismatched data.
- **SC-007**: 100% of tested pending student licenses appear in list results with nullable user and player profile fields.
- **SC-008**: 100% of tested paginated list requests return the requested result page and pagination information.
- **SC-009**: 100% of tested student detail requests return data only when the player profile is linked to a non-revoked student license in the requested community.
- **SC-010**: 100% of tested student detail responses include placeholder analytics without calculating real analytics.
- **SC-011**: The complete feature can be demonstrated without creating, updating, revoking, or activating licenses, and without assignments, reports, parent accounts, export/import, or real analytics.

## Assumptions

- Existing authentication provides a reliable current user identity for each request.
- Existing community role checks define valid viewing access as Active Owner or Active Teacher membership in the route community.
- Platform-admin status remains separate and does not bypass community role checks for this feature.
- Student licenses are the roster source of truth for this feature, including Pending students that have not logged in yet.
- Existing grade and class records are the source of truth for filter validation and display names.
- Existing pagination behavior will be reused so student list responses are consistent with other paged SprintLabs responses.
- Student detail is intended for activated students with linked player profiles; Pending students without player profiles remain visible in the list but cannot be opened through a player-profile detail route.
- Revoked student licenses are visible in the list unless filtered out, but revoked licenses do not qualify for student detail.
- Placeholder analytics fields are intentionally non-authoritative and will be replaced by a later analytics feature.
