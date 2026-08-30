# Feature Specification: Trusted Current-Community Resolution

**Feature Branch**: `feature/staff-community-context`

**Created**: 2026-08-30

**Status**: Draft

**Input**: User description: "Resolve the current Community for Community Owners and Teachers from the authenticated user and current database membership, remove client-selected community IDs from staff APIs, enforce one current staff community across Owner and Teacher roles, and preserve Student and Platform Admin compatibility."

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Use Staff APIs Without Selecting a Tenant (Priority: P1)

An authenticated Community Owner or Teacher uses community-management APIs for their own current community without the frontend sending a community identifier. The system derives the tenant from the authenticated identity and live membership data, so the caller cannot select another community.

**Why this priority**: Removing client-controlled tenant selection is the primary security goal. Every affected staff workflow depends on resolving the correct community before data is read or changed.

**Independent Test**: Can be fully tested by authenticating a user with exactly one Active Owner or Teacher membership in an Active community, calling each route without a community identifier, and confirming that each operation is scoped to that membership's community.

**Acceptance Scenarios**:

1. **Given** an authenticated user has exactly one current staff membership and it is Active in an Active community, **When** the user calls an affected staff route without a community identifier, **Then** the operation uses that community as its tenant context.
2. **Given** an authenticated user has an Active staff membership in Community A, **When** the user supplies an identifier belonging to a resource in Community B, **Then** the operation is rejected by the existing tenant and role checks and no Community B data is returned or changed.
3. **Given** an authenticated user has no Active staff community, **When** the user calls an affected staff route, **Then** current-community resolution fails and no community-scoped operation runs.
4. **Given** an authenticated user has current staff memberships spanning more than one community, **When** the user calls an affected staff route, **Then** resolution fails without choosing either community.
5. **Given** an authenticated user has one Active Owner or Teacher membership in a Suspended community, **When** the user calls an affected staff route, **Then** the community is not accepted as a valid current staff community.
6. **Given** an authenticated user is suspended, **When** current-community resolution identifies a tenant, **Then** the existing suspended-user authorization behavior still denies the operation.

---

### User Story 2 - Preserve Existing Staff Permissions (Priority: P1)

Community Owners and Teachers retain their existing permissions after tenant selection moves to the backend. Tenant resolution identifies where an operation applies; it does not grant permission to perform the operation.

**Why this priority**: A correct tenant can still be accessed by the wrong role. Existing Owner-only and Owner-or-Teacher boundaries protect community administration, licenses, rosters, and student information.

**Independent Test**: Can be fully tested by calling each resolved route as an Active Owner, Active Teacher, Student, platform-admin-only user, Pending member, Removed member, suspended user, and unrelated user, then comparing the result with the route's established authorization rule.

**Acceptance Scenarios**:

1. **Given** an Active Owner with one current staff community, **When** the Owner updates the community profile, manages teachers, or manages student licenses, **Then** the operation remains available according to its existing Owner-only rules.
2. **Given** an Active Teacher with one current staff community, **When** the Teacher attempts an Owner-only operation, **Then** the operation is denied even though the tenant was resolved successfully.
3. **Given** an Active Owner or Active Teacher with one current staff community, **When** the user manages grades/classes or views students, **Then** the operation remains available according to its existing Owner-or-Teacher rule.
4. **Given** a Student, Pending member, Removed member, unrelated user, or platform-admin-only user, **When** the user calls a membership-protected staff route, **Then** access is denied.
5. **Given** an authenticated user has platform-administrator status and no qualifying staff membership, **When** the user calls a Communities staff route, **Then** platform-administrator status does not supply or bypass the required community membership.

---

### User Story 3 - Prevent Conflicting Staff Memberships (Priority: P1)

The system prevents a user from gaining current Owner or Teacher relationships in more than one community, regardless of which staff role or membership status is involved.

**Why this priority**: Trusted tenant resolution is only safe and deterministic when membership mutation paths preserve the one-current-staff-community invariant.

**Independent Test**: Can be fully tested by attempting each Owner/Teacher and Pending/Active combination across two communities through invitation, completion, activation, restoration, and owner assignment, and confirming that at most one current staff community remains.

**Acceptance Scenarios**:

1. **Given** a user has a Pending or Active Owner membership in Community A, **When** an operation attempts to create, restore, or activate an Owner or Teacher membership in Community B, **Then** the operation is rejected without changing either community's membership, invitation, or capacity state.
2. **Given** a user has a Pending or Active Teacher membership in Community A, **When** an operation attempts to create, restore, or activate an Owner or Teacher membership in Community B, **Then** the operation is rejected without changing either community's membership, invitation, or capacity state.
3. **Given** two concurrent operations attempt to make the same user current staff in different communities, **When** both complete, **Then** at most one community has a Pending or Active staff membership for that user and the losing operation leaves no partial side effects.
4. **Given** a user's only former staff memberships are Removed, **When** an authorized operation assigns or invites the user to another community, **Then** the reassignment is allowed if all other rules pass.
5. **Given** a user is re-invited or completes setup for the same valid current community, **When** the established idempotent/reissue conditions are met, **Then** the existing membership is reused and no duplicate current membership or capacity reservation is created.

---

### User Story 4 - Fail Closed on Legacy Conflicts (Priority: P2)

Existing accounts with conflicting legacy staff memberships cannot obtain an arbitrary tenant through current-community APIs, community password login, or session refresh.

**Why this priority**: Preventing new conflicts does not repair old data. Authentication and tenant selection must remain secure until legacy records are explicitly corrected.

**Independent Test**: Can be fully tested by seeding conflicting Owner/Teacher memberships across communities, then attempting current-community resolution, community login, and refresh and verifying that none chooses a community or issues a usable staff session.

**Acceptance Scenarios**:

1. **Given** legacy data contains multiple Active staff communities, **When** current-community resolution, community login, or refresh is attempted, **Then** the operation fails and no community is selected.
2. **Given** legacy data contains one Active staff community and a Pending staff membership in another community, **When** current-community resolution, community login, or refresh is attempted, **Then** the operation fails rather than ignoring the conflict.
3. **Given** legacy data contains only Pending staff memberships, **When** current-community resolution or community login is attempted, **Then** no tenant or staff session is returned.
4. **Given** legacy data contains Removed memberships in other communities and exactly one valid Active staff membership, **When** resolution, login, or refresh is attempted, **Then** Removed memberships do not block the valid current community.
5. **Given** a conflicting user is also a platform administrator, **When** community login or refresh evaluates the account, **Then** platform-administrator status does not cause conflicting staff data to be silently accepted or converted into a selected staff tenant.

---

### User Story 5 - Migrate Frontend Routes Without Breaking Student or Admin Flows (Priority: P2)

Frontend clients move Owner and Teacher workflows to current-community routes while Student community-profile access and Platform Admin multi-community operations retain their explicit community-scoped contracts.

**Why this priority**: The security change must be adoptable without removing the intentional Student profile endpoint or the explicit tenant selection that Platform Admin workflows require.

**Independent Test**: Can be fully tested through route-contract verification that all 17 staff routes accept no request-side community identifier, the Student-compatible profile route still works, and both Platform Admin routes still require their route community identifier.

**Acceptance Scenarios**:

1. **Given** an Owner or Teacher frontend uses a migrated route, **When** it sends the existing non-tenant fields only, **Then** the request can be completed without a community selector or stored community identifier.
2. **Given** an Active Student membership, **When** the Student requests `GET /api/v1/Communities/{communityId}` for that community, **Then** the existing profile access remains available.
3. **Given** a Platform Admin manages an owner or license for a selected community, **When** the Admin calls the existing admin route with its route community identifier, **Then** the explicit scope remains required and functional.
4. **Given** frontend documentation for the affected workflows, **When** a developer follows the migration guide, **Then** the old and new routes, permissions, request fields, loading/empty/error states, and compatibility exceptions are clear without inventing new frontend behavior.

### Edge Cases

- A user has an Active Owner membership in one community and an Active Teacher membership in another.
- A user has any Owner/Teacher role combination whose statuses are a mix of Pending and Active across communities.
- A user has one Active staff membership in an Active community and another Active staff membership in a Suspended community; the conflicting current membership still cannot be silently ignored when enforcing the membership invariant.
- A user has only a Pending staff membership; Pending counts for the invariant but grants no access and cannot become a resolved tenant.
- A user has one Active staff membership plus Removed Owner or Teacher memberships elsewhere; Removed memberships neither grant access nor prevent reassignment.
- A user has an Active Student membership in another community in addition to one valid staff community; Student membership is not a staff relationship and does not participate in staff resolution.
- A user is a platform administrator and also has one valid staff membership; Communities routes still require and authorize through that membership, while Admin routes retain their separate rules.
- A resource identifier such as teacher user ID, class ID, grade ID, student license ID, or player profile ID belongs to another community.
- Two invitations, an invitation and owner assignment, or two owner assignments race for the same user in different communities.
- A same-community invitation is resent, completed twice, or completed after the target membership was removed or changed to an incompatible role.
- The authenticated credential has no parseable trusted user identifier, references a missing user, or belongs to a suspended user.
- A client continues calling a removed staff route that contains a community identifier; it must not reach an alternate tenant-selecting action.

## Requirements *(mandatory)*

### Functional Requirements

#### Trusted Current-Community Resolution

- **FR-001**: The system MUST resolve staff tenant context exclusively from the authenticated user's trusted identifier and current persisted community membership data.
- **FR-002**: The system MUST NOT accept a client-supplied community identifier in the path, query, or request body of any migrated Owner/Teacher route.
- **FR-003**: The current-community resolver MUST consider both Owner and Teacher roles as staff roles.
- **FR-004**: A staff membership is current when its status is Pending or Active; Removed memberships are not current.
- **FR-005**: A resolvable staff community MUST have an Active Owner or Teacher membership for the authenticated user.
- **FR-006**: A resolvable staff community MUST itself be Active wherever current staff login and access semantics require an Active community.
- **FR-007**: The resolver MUST return a tenant context only when exactly one valid Active staff community remains and no current staff membership exists in another community.
- **FR-008**: The resolver MUST fail closed when there is no valid Active staff community, when current staff memberships span more than one community, or when the only candidate community is not eligible for current staff access.
- **FR-009**: The resolver MUST NOT select a community using ordering, lowest/highest identifier, earliest/latest membership, preferred role, token response data, or any other arbitrary tie-breaker.
- **FR-010**: Pending memberships MUST NOT grant community access, even though they count when detecting conflicting current staff communities.
- **FR-011**: Removed memberships MUST NOT grant access and MUST NOT permanently prevent a later valid staff assignment.
- **FR-012**: Student memberships MUST NOT be treated as staff memberships by the resolver.
- **FR-013**: Current-community resolution MUST identify tenant context only; every affected operation MUST retain its existing user-status, role, resource-ownership, and tenant-isolation authorization checks.
- **FR-014**: Platform-administrator status alone MUST NOT resolve a tenant or bypass membership-based authorization in Communities routes.
- **FR-015**: Community membership changes MUST take effect on the next resolution or authorization decision; cached or credential-embedded community selection MUST NOT remain authoritative after membership changes.
- **FR-016**: This feature MUST NOT add a community identifier or community role to access credentials solely to support current-community resolution.

#### Staff Route Contract

- **FR-017**: The system MUST expose `GET /api/v1/Communities/me` for an authenticated Owner or Teacher to view the profile of the resolved current staff community.
- **FR-018**: The system MUST expose `PATCH /api/v1/Communities/me` for an authenticated Owner to update the resolved current community profile.
- **FR-019**: The system MUST expose `GET /api/v1/Communities/teachers`, `POST /api/v1/Communities/teachers/invite`, and `DELETE /api/v1/Communities/teachers/{teacherUserId}` under the resolved current community; the established teacher-management operations remain Owner-only.
- **FR-020**: The system MUST expose `POST /api/v1/Communities/grades` and `GET /api/v1/Communities/grades` under the resolved current community; the established operations remain available to Active Owners and Active Teachers.
- **FR-021**: The system MUST expose `POST /api/v1/Communities/classes`, `GET /api/v1/Communities/classes`, `PATCH /api/v1/Communities/classes/{classId}`, and `DELETE /api/v1/Communities/classes/{classId}` under the resolved current community; the established operations remain available to Active Owners and Active Teachers.
- **FR-022**: The system MUST expose `POST /api/v1/Communities/student-licenses`, `GET /api/v1/Communities/student-licenses`, `PATCH /api/v1/Communities/student-licenses/{licenseId}`, and `DELETE /api/v1/Communities/student-licenses/{licenseId}` under the resolved current community; the established operations remain Owner-only.
- **FR-023**: The system MUST expose `GET /api/v1/Communities/students` and `GET /api/v1/Communities/students/{playerProfileId}` under the resolved current community; the established operations remain available to Active Owners and Active Teachers.
- **FR-024**: Existing non-tenant request fields and query filters for profile updates, teacher invitations, grades, classes, student licenses, and student viewing MUST remain supported unless a field is specifically the removed community identifier.
- **FR-025**: Community identifiers MAY remain in successful response data where they are part of the established response contract; this feature removes client tenant selection, not useful server-returned identifiers.
- **FR-026**: The previous staff routes containing `{communityId}` MUST no longer provide Owner/Teacher tenant selection, except for the intentionally retained community-profile compatibility route in FR-027.
- **FR-027**: `GET /api/v1/Communities/{communityId}` MUST remain available with its existing Active Owner, Teacher, and Student membership rules so Student profile access and backward compatibility are not broken.
- **FR-028**: `POST /api/v1/admin/communities/{communityId}/owner` and `PATCH /api/v1/admin/communities/{communityId}/licenses` MUST retain their explicit community identifiers and existing Platform Admin authorization because those operations intentionally manage multiple communities.
- **FR-029**: A missing, malformed, or untrusted authenticated user identifier MUST NOT produce a tenant context or allow a community-scoped operation.
- **FR-030**: Resolution and authorization failures MUST use established safe response conventions and MUST NOT disclose another community's data or membership details.

#### One Current Staff Community Invariant

- **FR-031**: A user MUST have at most one current staff community across all Owner and Teacher memberships.
- **FR-032**: The invariant MUST reject Owner-to-Owner, Teacher-to-Teacher, Owner-to-Teacher, and Teacher-to-Owner current relationships that span different communities.
- **FR-033**: The invariant MUST apply to every Pending/Active status combination across different communities.
- **FR-034**: Every operation that creates, restores, changes, or activates an Owner or Teacher membership MUST validate the invariant immediately before the membership becomes current.
- **FR-035**: Teacher invitation issuance and reissue MUST reject an invited identity that has any current Owner or Teacher membership in another community, not only a Teacher membership.
- **FR-036**: Community-owner setup invitation issuance MUST reject an invited identity that has any current Owner or Teacher membership in another community, not only an Owner membership.
- **FR-037**: Invitation completion for either Owner or Teacher MUST recheck all current Owner and Teacher memberships before activating the target membership.
- **FR-038**: Automatic or provider-login teacher activation MUST activate no membership when doing so would create multiple current staff communities; it MUST never activate multiple cross-community staff memberships or choose one arbitrarily.
- **FR-039**: Platform Admin owner assignment MUST reject creating, restoring, or converting a membership when the target user already has a current Owner or Teacher membership in another community.
- **FR-040**: Removing a staff membership MUST cause that membership to stop counting toward the invariant and MUST allow later authorized reassignment without deleting the historical row.
- **FR-041**: Same-community reissue, completion, or restoration MAY reuse an existing compatible membership but MUST NOT create duplicate membership rows, duplicate capacity reservations, or silent Owner/Teacher role conversion.
- **FR-042**: Rejected membership operations MUST leave user identity, membership status/role, invitations, community capacity counters, and unrelated records unchanged.
- **FR-043**: Concurrent membership operations for the same user MUST preserve the invariant so that at most one community contains a Pending or Active Owner/Teacher membership after the operations finish.
- **FR-044**: The system MUST enforce the invariant in all current staff membership write paths discovered during planning or implementation, including any path not named explicitly in this specification.

#### Authentication and Legacy Conflict Safety

- **FR-045**: Community password login MUST derive staff role and community only from current persisted Owner/Teacher memberships and MUST issue no staff credentials when legacy current staff memberships span multiple communities.
- **FR-046**: Community password login MUST preserve its existing fail-closed handling for zero valid Active staff communities, pending-only state, Active membership in an ineligible community, and conflicting current memberships.
- **FR-047**: Refresh MUST re-evaluate current persisted account and staff membership state before issuing replacement credentials and MUST issue no replacement when legacy current staff memberships span multiple communities.
- **FR-048**: Refresh MUST NOT reconstruct a staff account type or community by selecting the first matching membership when more than one current staff community exists.
- **FR-049**: Platform-administrator account handling MUST NOT be used as a bypass that silently accepts legacy conflicting staff memberships during community login or refresh.
- **FR-050**: Existing credential hashing, expiration, single-use rotation, revocation, logout, password recovery, and account suspension behavior MUST remain unchanged except where stricter membership conflict rejection is required by this feature.
- **FR-051**: This feature MUST NOT bulk rewrite, delete, merge, or automatically choose among legacy conflicting memberships; such data remains stored and fail-closed until corrected through an authorized remediation outside this feature.

#### Tenant Isolation, Documentation, and Verification

- **FR-052**: Every resource lookup or mutation under a resolved route MUST remain constrained to the resolved community, including teacher users, grades, classes, student licenses, and player profiles.
- **FR-053**: Supplying an identifier for a resource owned by another community MUST return no cross-community data and MUST make no cross-community change.
- **FR-054**: Existing Owner-only routes MUST remain Owner-only, and existing Owner-or-Teacher routes MUST remain Owner-or-Teacher after route migration.
- **FR-055**: Existing Pending, Removed, Student, no-membership, suspended-user, and platform-admin-only denial behavior MUST remain in force for staff routes.
- **FR-056**: API documentation MUST describe all migrated routes, authentication and role requirements, request and success contracts, common errors, and the Student and Platform Admin compatibility exceptions.
- **FR-057**: Frontend documentation MUST include a complete old-to-new route migration table and state that the frontend must remove community identifiers from paths, query parameters, request bodies, stored current-community selection, and tenant-selection UI for these staff workflows.
- **FR-058**: Frontend documentation MUST describe required pages/sections, user actions, forms/fields, table columns, validation, loading/empty/error states, permission visibility, and the API calls used by each action without adding behavior outside the affected features.
- **FR-059**: Focused automated verification MUST cover the resolver's success and failure matrix, all cross-role and Pending/Active invariant combinations, Removed reassignment, concurrency, invitation/activation/owner-assignment mutation paths, community login, refresh, route contracts, role preservation, cross-tenant resource IDs, Student profile compatibility, and unchanged Admin routes.
- **FR-060**: Existing focused tests for community profile, teacher management, grades/classes, student licenses, student viewing, Student activation, community login, refresh/logout, and Platform Admin authorization MUST continue to pass.
- **FR-061**: The feature MUST update existing affected API/frontend documentation or create consolidated `api.md` and `frontend.md` documentation within this feature directory, with one authoritative and internally consistent migration guide.
- **FR-062**: The feature MUST reuse the existing community access, request/response, error, and vertical-slice conventions and MUST NOT introduce a generic tenant framework, global request behavior, parallel membership store, or new external dependency.

### Staff Route Migration

| Existing staff route | Required staff route | Authorization after migration |
|---|---|---|
| `GET /api/v1/Communities/{communityId}` | `GET /api/v1/Communities/me` for Owner/Teacher; retain the existing route for Student/backward compatibility | Active Owner or Teacher for `/me`; existing Active Owner, Teacher, or Student rule for `/{communityId}` |
| `PATCH /api/v1/Communities/{communityId}` | `PATCH /api/v1/Communities/me` | Active Owner only |
| `GET /api/v1/Communities/{communityId}/teachers` | `GET /api/v1/Communities/teachers` | Active Owner only |
| `POST /api/v1/Communities/teachers/invite` | Unchanged route; continue accepting no community identifier | Active Owner only |
| `DELETE /api/v1/Communities/{communityId}/teachers/{teacherUserId}` | `DELETE /api/v1/Communities/teachers/{teacherUserId}` | Active Owner only |
| `POST /api/v1/Communities/{communityId}/grades` | `POST /api/v1/Communities/grades` | Active Owner or Teacher |
| `GET /api/v1/Communities/{communityId}/grades` | `GET /api/v1/Communities/grades` | Active Owner or Teacher |
| `POST /api/v1/Communities/{communityId}/classes` | `POST /api/v1/Communities/classes` | Active Owner or Teacher |
| `GET /api/v1/Communities/{communityId}/classes` | `GET /api/v1/Communities/classes` | Active Owner or Teacher |
| `PATCH /api/v1/Communities/{communityId}/classes/{classId}` | `PATCH /api/v1/Communities/classes/{classId}` | Active Owner or Teacher |
| `DELETE /api/v1/Communities/{communityId}/classes/{classId}` | `DELETE /api/v1/Communities/classes/{classId}` | Active Owner or Teacher |
| `POST /api/v1/Communities/{communityId}/student-licenses` | `POST /api/v1/Communities/student-licenses` | Active Owner only |
| `GET /api/v1/Communities/{communityId}/student-licenses` | `GET /api/v1/Communities/student-licenses` | Active Owner only |
| `PATCH /api/v1/Communities/{communityId}/student-licenses/{licenseId}` | `PATCH /api/v1/Communities/student-licenses/{licenseId}` | Active Owner only |
| `DELETE /api/v1/Communities/{communityId}/student-licenses/{licenseId}` | `DELETE /api/v1/Communities/student-licenses/{licenseId}` | Active Owner only |
| `GET /api/v1/Communities/{communityId}/students` | `GET /api/v1/Communities/students` | Active Owner or Teacher |
| `GET /api/v1/Communities/{communityId}/students/{playerProfileId}` | `GET /api/v1/Communities/students/{playerProfileId}` | Active Owner or Teacher |

### Key Entities *(include if feature involves data)*

- **Authenticated User**: The trusted signed-in identity whose persisted user status and memberships are evaluated for every request. Client-supplied user or tenant assertions are not authoritative.
- **Community**: The tenant boundary for community profile, teacher, grade, class, student-license, and student data. An Active community can be selected as current staff context only through qualifying membership.
- **Community Membership**: The relationship between a user and a community, with Owner, Teacher, or Student role and Pending, Active, or Removed status. Owner and Teacher are staff roles; Pending and Active are current; only Active grants access; Removed is historical.
- **Current Staff Community**: The single Active community derived for a user from trusted current Owner/Teacher membership state. It is a request-time tenant context, not a client preference or credential claim.
- **Teacher Invitation / Community Owner Setup Invitation**: A pending staff-membership lifecycle that can create, restore, or activate current staff state and therefore must preserve the invariant during issue and completion.
- **Refresh Credential**: A renewable session credential whose rotation must re-evaluate trusted current account and staff-membership state and fail on legacy conflicts.
- **Community-Owned Resource**: A teacher membership, grade, class, student license, or student profile association whose community ownership must match the resolved tenant.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: All 17 affected Owner/Teacher routes can be called without a request-side community identifier, and none allows the caller to select another tenant.
- **SC-002**: 100% of tested users with exactly one Active Owner or Teacher membership in an Active community resolve to that community, while 100% of zero-community and conflicting-community cases resolve to no tenant.
- **SC-003**: 100% of tested Pending and Removed memberships grant no staff-route access; Removed memberships do not prevent a later valid reassignment.
- **SC-004**: All Owner/Teacher role pairings and all Pending/Active status pairings across two communities are rejected before they can produce more than one current staff community.
- **SC-005**: In concurrent cross-community membership attempts for one user, at most one operation establishes a current staff community and the other leaves zero partial membership, invitation, or capacity changes.
- **SC-006**: 100% of tested legacy conflicts fail current-community resolution, community login, and refresh without arbitrary community selection or usable replacement staff credentials.
- **SC-007**: 100% of cross-community teacher, class, grade, student-license, and player-profile identifier tests return no foreign-tenant data and make no foreign-tenant changes.
- **SC-008**: 100% of tested Owner-only operations deny Teachers, and 100% of tested Owner-or-Teacher operations retain access for both roles when all other conditions pass.
- **SC-009**: The retained `GET /api/v1/Communities/{communityId}` continues to serve every tested eligible Student case, and the two explicitly scoped Platform Admin routes continue to require a community identifier.
- **SC-010**: Membership removal followed by authorized reassignment succeeds in every tested role transition where no other current staff community exists.
- **SC-011**: Frontend developers can migrate every affected call using one documented old-to-new route table, with no community selector or client-held current-community identifier required for Owner/Teacher workflows.
- **SC-012**: All focused new security tests and existing affected community/authentication regression tests pass with no unrelated feature expansion.

## Assumptions

- The authenticated access credential's signed `userId` claim remains the trusted request identity and is sufficient to load current user and membership state.
- Community membership is mutable, so persisted membership state is re-evaluated when resolving tenant context or renewing a staff session.
- Existing response payloads may continue returning community identifiers; only request-side tenant selection is removed.
- Existing handlers remain the authority for Owner-only versus Owner-or-Teacher authorization and for scoping resource identifiers to a community.
- The established safe authorization error is used when current-community resolution returns no tenant; login and refresh retain their existing public error conventions while issuing no credentials.
- Student memberships may exist in multiple communities and do not participate in the staff invariant.
- Legacy conflicting records are not automatically repaired by this feature; operational remediation can be designed separately if required.
- Frontend implementation is outside this backend feature, but complete frontend-facing API and migration documentation is required.
- Planning will determine the smallest concurrency-safe enforcement mechanism using existing persistence capabilities; no generic tenant framework, new package, or community claim is presumed.
