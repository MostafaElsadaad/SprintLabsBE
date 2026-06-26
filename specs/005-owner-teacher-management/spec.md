# Feature Specification: Owner Teacher Management

**Feature Branch**: `005-owner-teacher-management`

**Created**: 2026-06-26

**Status**: Draft

**Input**: User description: "Build teacher management for community owners. Community Owners can invite teachers by email, list community teachers, remove teacher memberships without hard delete, activate pending teacher memberships when a matching teacher logs in, and enforce teacher license limits. Email sending, teacher dashboards, student management, grades, classes, student licenses, analytics, payments, and admin teacher management are out of scope."

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Invite a Teacher Within License Capacity (Priority: P1)

A community Owner invites a teacher by email so the teacher can reserve a teacher seat and later gain access to the community after signing in with the matching email.

**Why this priority**: Inviting teachers is the core owner workflow for expanding a community beyond the initial owner. License capacity must be enforced at the moment a seat is reserved.

**Independent Test**: Can be fully tested by signing in as an active community Owner, inviting a new teacher when teacher capacity is available, and confirming that a teacher membership is created with pending access and no duplicate membership rows are created.

**Acceptance Scenarios**:

1. **Given** an authenticated user with an Active Owner membership and available teacher capacity, **When** the Owner invites a teacher by email and name, **Then** the target user is found or created and a Pending Teacher membership is created for that community.
2. **Given** an authenticated Active Owner invites an email that already belongs to a user, **When** the invitation is submitted, **Then** the existing user is used instead of creating a duplicate user identity.
3. **Given** an authenticated Active Owner invites the same teacher twice, **When** both invitations target the same community and teacher, **Then** the community has only one membership row for that teacher and community.
4. **Given** teacher license capacity is already full, **When** an Owner attempts to invite another teacher, **Then** the invitation is rejected and no teacher membership is created or restored.
5. **Given** an authenticated user without an Active Owner membership, **When** the user attempts to invite a teacher, **Then** the request is denied and no teacher membership is created.

---

### User Story 2 - List Community Teachers (Priority: P2)

A community Owner views the teachers in the community so they can understand who has been invited, who is active, and who has been removed.

**Why this priority**: Owners need visibility into teacher membership state before deciding whether to resend an invitation outside this feature, remove a teacher, or manage license capacity.

**Independent Test**: Can be fully tested by creating teacher memberships in different statuses for one community and confirming that an active Owner sees the expected teacher list with identity and membership details.

**Acceptance Scenarios**:

1. **Given** an authenticated user with an Active Owner membership, **When** the Owner requests the community teacher list, **Then** teacher entries for that community are returned with user id, name, email, status, and created date.
2. **Given** a community has no teacher memberships, **When** an active Owner requests the teacher list, **Then** an empty list is returned.
3. **Given** a teacher belongs to another community, **When** an Owner lists teachers for this community, **Then** that teacher is not included.
4. **Given** an authenticated non-owner community member, **When** the user requests the community teacher list, **Then** the request is denied.

---

### User Story 3 - Remove a Teacher Without Hard Delete (Priority: P3)

A community Owner removes a teacher from the community so the teacher no longer has community access while retaining historical membership records.

**Why this priority**: Owners need a safe way to revoke teacher access and free teacher capacity without deleting records that may matter for future auditing or recovery.

**Independent Test**: Can be fully tested by removing an existing Pending or Active Teacher membership and confirming the membership is marked Removed, access is no longer granted, and license usage is adjusted only when appropriate.

**Acceptance Scenarios**:

1. **Given** an authenticated Active Owner and an Active Teacher membership in the same community, **When** the Owner removes the teacher, **Then** the membership status becomes Removed and the teacher no longer has active community access.
2. **Given** an authenticated Active Owner and a Pending Teacher membership in the same community, **When** the Owner removes the teacher, **Then** the membership status becomes Removed and the reserved teacher seat is released.
3. **Given** a Teacher membership is already Removed, **When** an Owner removes that teacher again, **Then** the operation does not decrement teacher usage below the correct count.
4. **Given** the target user is an Owner, **When** the Owner-removal workflow is used, **Then** the request is rejected and the Owner membership is not removed.
5. **Given** an authenticated non-owner community member, **When** the user attempts to remove a teacher, **Then** the request is denied and no membership status changes.

---

### User Story 4 - Activate Pending Teacher on Matching Login (Priority: P4)

An invited teacher signs in with the same email address used in the invitation and automatically receives active teacher membership in the matching communities.

**Why this priority**: The invite flow intentionally excludes email delivery and custom invite acceptance, so matching login is the simplest path for a pending teacher to become active without a separate acceptance workflow.

**Independent Test**: Can be fully tested by creating a Pending Teacher membership for an email, signing in as a user with that email, and confirming the membership becomes Active without increasing teacher license usage again.

**Acceptance Scenarios**:

1. **Given** a Pending Teacher membership exists for an email, **When** a user signs in with that same email, **Then** the pending teacher membership becomes Active.
2. **Given** multiple Pending Teacher memberships exist for the same email in different communities, **When** the matching user signs in, **Then** each matching Pending Teacher membership becomes Active.
3. **Given** an invited teacher signs in with a different email, **When** login completes, **Then** the unrelated Pending Teacher membership remains Pending.
4. **Given** a pending teacher seat was already counted when invited, **When** the membership activates on login, **Then** teacher usage is not incremented a second time.

### Edge Cases

- An Owner invites a teacher with an invalid, empty, or whitespace-only email; the invite is rejected.
- An Owner invites a teacher with an empty or whitespace-only name; the invite is rejected.
- Email matching uses normalized email values so casing and surrounding whitespace do not create duplicate users or memberships.
- A removed teacher is invited again while license capacity is available; the existing membership is restored to Pending rather than creating a duplicate membership.
- A removed teacher is invited again when license capacity is full; the restore is rejected and the membership remains Removed.
- A community has no license record or no teacher capacity configured; the invite is rejected because capacity cannot be confirmed.
- Teacher usage cannot be decremented below zero.
- Teacher usage cannot exceed the configured maximum teacher count.
- A user with a Pending or Removed Owner membership cannot manage teachers.
- A platform admin without Active Owner membership does not gain owner teacher-management access through this feature.
- Existing player login and community profile behavior continues to work after teacher activation is added to login.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The system MUST allow an authenticated user with an Active Owner membership in a community to invite a teacher to that community by email and name.
- **FR-002**: The system MUST deny teacher invitation attempts from unauthenticated users.
- **FR-003**: The system MUST deny teacher invitation attempts from users who do not have an Active Owner membership in the target community.
- **FR-004**: Teacher invitation MUST find an existing user by normalized email when one exists.
- **FR-005**: Teacher invitation MUST create a basic user identity when no user exists for the normalized email.
- **FR-006**: A user created through teacher invitation MUST NOT automatically become a platform admin.
- **FR-007**: A teacher invitation MUST create or restore a community membership with Teacher role and Pending status by default.
- **FR-008**: Teacher invitation MUST NOT create duplicate membership rows for the same user and community.
- **FR-009**: Restoring a Removed Teacher membership MUST reuse the existing membership row rather than creating a duplicate.
- **FR-010**: Teacher invitation MUST enforce the community's teacher license limit before creating or restoring a teacher membership.
- **FR-011**: Pending and Active Teacher memberships MUST both count against teacher license usage.
- **FR-012**: If teacher usage is greater than or equal to maximum teacher capacity, the system MUST reject new teacher invitations and teacher membership restores.
- **FR-013**: A successful teacher invitation or restore MUST reserve one teacher seat exactly once.
- **FR-014**: The system MUST allow an authenticated Active Owner to list Teacher memberships for the Owner's community.
- **FR-015**: Teacher lists MUST include each teacher's user id, name, email, membership status, and membership creation date.
- **FR-016**: Teacher lists MUST only include Teacher memberships for the requested community.
- **FR-017**: The system MUST deny teacher list requests from unauthenticated users and users without Active Owner membership in the target community.
- **FR-018**: The system MUST allow an authenticated Active Owner to remove a Teacher membership from the Owner's community without hard deleting the membership.
- **FR-019**: Teacher removal MUST set the Teacher membership status to Removed.
- **FR-020**: Teacher removal MUST NOT allow removing Owner memberships through the teacher-removal workflow.
- **FR-021**: Teacher removal MUST release one teacher seat only when the membership previously counted as Pending or Active.
- **FR-022**: Teacher removal MUST NOT reduce teacher usage below zero.
- **FR-023**: Removed Teacher memberships MUST NOT grant community access.
- **FR-024**: When a user signs in with an email matching Pending Teacher memberships, the system MUST activate those Pending Teacher memberships.
- **FR-025**: Teacher activation on matching sign-in MUST NOT increment teacher usage because the invite already reserved the seat.
- **FR-026**: Teacher activation on matching sign-in MUST only activate Pending memberships with Teacher role.
- **FR-027**: Existing user identity, player login, admin community, community access, and community profile behavior MUST continue to work after teacher management is added.
- **FR-028**: The feature MUST NOT send invitation emails, add teacher dashboards, manage students, grades, classes, student licenses, analytics, payments, or admin teacher-management workflows.

### Key Entities

- **User**: A login identity that may be invited as a teacher by email. Relevant information includes email, name, platform-admin flag, and status.
- **Community**: The school or community where teacher memberships are managed.
- **Community Membership**: The relationship between a user and community. Teacher management creates, lists, restores, activates, and removes memberships with Teacher role.
- **Community License**: The community's capacity record that defines maximum teacher seats and current teacher-seat usage.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: 100% of tested Active Owners can invite a teacher when teacher capacity is available and valid teacher details are provided.
- **SC-002**: 100% of tested non-owner, Pending Owner, Removed Owner, and unauthenticated invitation attempts are denied without creating or restoring teacher memberships.
- **SC-003**: 100% of tested invitations at full teacher capacity are rejected without increasing teacher usage.
- **SC-004**: 100% of tested duplicate invitations for the same teacher and community leave only one membership row.
- **SC-005**: 100% of tested teacher lists shown to Active Owners contain only Teacher memberships for the requested community with user id, name, email, status, and created date.
- **SC-006**: 100% of tested teacher removals by Active Owners mark the Teacher membership as Removed without hard deletion.
- **SC-007**: 100% of tested Pending or Active teacher removals release exactly one teacher seat, and repeated removal does not reduce usage below the correct value.
- **SC-008**: 100% of tested Removed Teacher memberships fail active community access checks after removal.
- **SC-009**: 100% of tested Pending Teacher memberships matching a signing-in user's normalized email become Active during login without increasing teacher usage again.
- **SC-010**: Existing admin community, community access, owner community profile, and player login flows remain demonstrably functional after this feature is added.

## Assumptions

- Existing authentication provides a reliable current user identity for owner actions and login activation.
- Active Owner membership is the only role allowed to manage teachers in this feature.
- Platform-admin status alone does not grant owner teacher-management access.
- Teacher invitation creates Pending memberships by default.
- Pending and Active Teacher memberships both reserve teacher seats.
- A missing community license or missing teacher capacity means teacher capacity cannot be confirmed, so teacher invitation is rejected.
- Email normalization trims surrounding whitespace and compares emails case-insensitively.
- Re-inviting a Removed Teacher restores the same membership to Pending when capacity allows.
- Teacher lists include Removed teachers unless a later clarification narrows the list to only Pending and Active teacher memberships.
- No email delivery or invitation-token acceptance workflow is included.
