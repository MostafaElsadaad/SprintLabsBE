# Feature Specification: Student License Activation on Login

**Feature Branch**: `008-student-license-activation`

**Created**: 2026-06-30

**Status**: Draft

**Input**: User description: "Build student license activation during Google login. When a student logs in with an email that was added by a community owner, activate their pending student license, link their User and PlayerProfile, and create active student community access. This includes detecting pending student licenses during Google login, matching pending licenses by email, creating or finding the player's PlayerProfile, activating student licenses, linking StudentLicense to User and PlayerProfile, and creating or restoring CommunityUser membership as Student. Owner student license management, student dashboards, profile editing, analytics, parent accounts, manual invitation acceptance, payment logic, and grade/class management are out of scope."

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Activate Pending Student License on Login (Priority: P1)

A student who was assigned a pending license by a community Owner signs in with the matching Google email and immediately receives active student access for the licensed community.

**Why this priority**: This is the core onboarding path that turns an Owner-created pending student seat into usable student access without requiring a separate invitation acceptance flow.

**Independent Test**: Can be fully tested by creating a pending student license for an email, signing in with Google using that email, and confirming the license is active, linked to the signed-in identity and player profile, and grants active student community access.

**Acceptance Scenarios**:

1. **Given** a Pending student license exists for an email, **When** the student signs in with Google using that email, **Then** the license becomes Active, is linked to the signed-in user, is linked to a player profile, and has an activation date.
2. **Given** the student has no player profile before login, **When** the pending student license is activated, **Then** a player profile exists and is linked to the activated license.
3. **Given** the student has no membership in the licensed community, **When** the pending student license is activated, **Then** an Active Student membership is created for that community.
4. **Given** the student has a Removed membership in the licensed community, **When** the pending student license is activated, **Then** the membership is restored as Active Student access.
5. **Given** multiple Pending student licenses exist for the same email across different communities, **When** the student signs in with Google using that email, **Then** all valid pending licenses are activated.

---

### User Story 2 - Preserve Existing Login Behavior (Priority: P2)

A user signs in with Google and receives the same normal login outcome whether or not student license activation happens in the background.

**Why this priority**: Student activation must not break existing B2C player login behavior, existing user identity behavior, or already implemented teacher activation behavior.

**Independent Test**: Can be fully tested by signing in as users with no student licenses, pending teacher memberships, pending student licenses, and combinations of those states, then confirming the login succeeds and returns the expected authenticated identity and player profile information.

**Acceptance Scenarios**:

1. **Given** a B2C player has no pending student license, **When** the player signs in with Google, **Then** the login succeeds with the existing identity, player profile, and authentication response behavior.
2. **Given** a user has pending teacher memberships, **When** the user signs in with Google, **Then** existing teacher activation behavior still works.
3. **Given** a user has both pending teacher memberships and pending student licenses, **When** the user signs in with Google, **Then** both eligible activation paths complete without preventing login.
4. **Given** a pending student license activation occurs, **When** login completes, **Then** the response still includes the normal signed-in user, player profile when available, and authentication token information.

---

### User Story 3 - Keep Activation Idempotent (Priority: P3)

A student can sign in repeatedly without consuming additional seats, duplicating community access, or reprocessing licenses that are already active or revoked.

**Why this priority**: Login can happen many times. Activation must be safe to repeat and must not corrupt community seat counts or membership records.

**Independent Test**: Can be fully tested by signing in once to activate a pending license, recording license, membership, and used-student counts, then signing in again and confirming those records remain stable.

**Acceptance Scenarios**:

1. **Given** a student license was already activated on a previous login, **When** the student signs in again, **Then** no duplicate Student membership is created.
2. **Given** a student license was already activated on a previous login, **When** the student signs in again, **Then** used student count does not increase.
3. **Given** a Revoked student license exists for the student's email, **When** the student signs in, **Then** the Revoked license is not activated.
4. **Given** an Active student license exists for the student's email, **When** the student signs in, **Then** the existing Active license is not reactivated or counted again.

### Edge Cases

- A pending license email differs from the Google email only by casing or surrounding whitespace.
- Multiple pending licenses exist for the same email in different communities.
- A pending license has a matching user who already has a player profile.
- A pending license has a matching user who does not yet have a player profile.
- A matching community membership already exists with Active Student status.
- A matching community membership exists with Removed status.
- A matching community membership exists for the same user and community with a non-Student role.
- A matching student license is Revoked or already Active.
- Used student count is already at the community maximum before activation.
- Login includes pending teacher activation and pending student license activation for the same user.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The system MUST check for Pending student licenses matching the signed-in Google email after the user identity has been created or found.
- **FR-002**: Student license email matching MUST be case-insensitive and ignore surrounding whitespace.
- **FR-003**: The system MUST activate every valid Pending student license matching the signed-in email across communities.
- **FR-004**: The system MUST NOT activate Revoked student licenses.
- **FR-005**: The system MUST NOT reactivate Active student licenses.
- **FR-006**: The system MUST ensure a player profile exists for the signed-in user before completing student license activation.
- **FR-007**: When activating a pending student license, the system MUST link the license to the signed-in user.
- **FR-008**: When activating a pending student license, the system MUST link the license to the user's player profile.
- **FR-009**: When activating a pending student license, the system MUST set the license status to Active.
- **FR-010**: When activating a pending student license, the system MUST record the activation date.
- **FR-011**: The system MUST create an Active Student community membership when no membership exists for the activated license's community and user.
- **FR-012**: The system MUST restore a matching Removed community membership to Active Student access when activating a pending student license.
- **FR-013**: The system MUST NOT create duplicate community membership records for the same user and community during activation.
- **FR-014**: The system MUST NOT increase used student count during activation because the pending license already reserved the seat.
- **FR-015**: Repeated Google login for the same activated student MUST NOT create duplicate memberships, change used student count, or reprocess already Active licenses.
- **FR-016**: Existing B2C player login behavior MUST continue to work for users without pending student licenses.
- **FR-017**: Existing Google login responses MUST continue to include the normal signed-in user, player profile when available, and authentication token information.
- **FR-018**: Existing pending teacher activation behavior MUST continue to work when present.
- **FR-019**: If a matching same-community membership already exists with a non-Student role, the system MUST NOT create a duplicate membership or overwrite that role during student activation.
- **FR-020**: The feature MUST NOT implement owner student license management, student dashboards, student profile editing, analytics, parent accounts, email invitation acceptance, manual invitation acceptance, payment logic, or grade/class management.

### Key Entities *(include if feature involves data)*

- **Student License**: A community-owned student seat assigned to an email. It transitions from Pending to Active on matching Google login and stores links to the signed-in user, player profile, and activation date.
- **User**: The shared login identity created or found during Google login and linked to activated student licenses.
- **Player Profile**: The game profile associated with the signed-in user and linked to activated student licenses.
- **Community Membership**: The access record that grants the signed-in user Active Student access to the licensed community.
- **Community License**: The community's student capacity record whose used student count was already reserved when the pending student license was created.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: 100% of tested students with a valid Pending license can sign in with the matching Google email and receive an Active student license.
- **SC-002**: 100% of tested activated licenses have a linked user, linked player profile, Active status, and activation date.
- **SC-003**: 100% of tested activations create or restore Active Student community access without creating duplicate membership rows.
- **SC-004**: 100% of tested activation attempts leave used student count unchanged.
- **SC-005**: 100% of tested repeated logins do not duplicate Student memberships or increase used student count.
- **SC-006**: 100% of tested Revoked and already Active licenses are ignored by activation.
- **SC-007**: 100% of tested users without pending student licenses continue to complete existing Google login successfully.
- **SC-008**: 100% of tested users with pending teacher activation continue to receive existing teacher activation behavior.
- **SC-009**: A student with pending licenses in multiple communities receives active access for every valid matching community in a single login.
- **SC-010**: The complete feature can be demonstrated without student dashboards, manual invitation acceptance, email sending, analytics, payment logic, or grade/class management changes.

## Assumptions

- Existing Google login is the only activation entry point for this feature.
- Pending student licenses were already created by the Owner student license management feature and already reserved student capacity.
- A user may have zero or one player profile, and login can create or find it using the existing player profile behavior.
- Platform admin status does not replace or bypass Student community membership for this feature.
- If an existing same-community membership has a non-Student role, activation preserves that role and does not create a duplicate membership; the student license remains available for follow-up rather than silently changing the user's community role.
- Matching by email uses normalized email values so casing and surrounding whitespace do not prevent activation.
- Activation is expected to be atomic enough that a license is not marked Active unless its required user, player profile, and community access updates also succeed.
