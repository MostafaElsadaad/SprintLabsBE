# Feature Specification: Fixed Grades, Teacher Classes, and Demo Community

**Feature Branch**: `codex/fixed-grades-teacher-classes`

**Created**: 2026-08-30

**Status**: Draft

**Input**: User description: "Provide fixed grades 7-12, Owner-managed teacher class assignments, paginated teacher filtering by grade and class, verification of the existing student roster filters, and an opt-in development demo community with representative staff and students."

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Use the Same Six Supported Grades in Every Community (Priority: P1)

Community Owners and Teachers select from the fixed SprintLabs grades 7, 8, 9, 10, 11, and 12 when working with new classes and students. They can list these grades but cannot create, rename, reorder, or delete them. Unsupported legacy grade rows may remain temporarily for migration safety, but they are not part of the supported grade set.

**Why this priority**: Grades are the shared reference data for classes, teacher filters, student placement, and demo data. The remaining journeys depend on one reliable grade set.

**Independent Test**: Create a community, list its grades as an Owner and as a Teacher, and verify that each receives exactly one integer grade for every value from 7 through 12 in ascending order and that no staff grade-mutation operation is available.

**Acceptance Scenarios**:

1. **Given** a newly created community, **When** its setup completes, **Then** it has exactly six grade records with values 7, 8, 9, 10, 11, and 12.
2. **Given** an existing community with missing supported grades and unsupported legacy grade rows, **When** the fixed-grade backfill runs, **Then** only missing supported grades are added, safely recognized grade identifiers and references are preserved, and unsupported legacy rows remain unchanged and are reported for manual remediation.
3. **Given** an Active Owner or Teacher in a community with one row for each supported grade, **When** the user requests `GET /api/v1/Communities/grades` without a community identifier, **Then** the response contains exactly grades 7 through 12 in numerical order, excludes unsupported legacy rows, and derives the community from the authenticated staff context.
4. **Given** any Community Owner or Teacher, **When** the user attempts `POST /api/v1/Communities/grades` or another grade mutation, **Then** no staff-facing grade creation, editing, or deletion operation is available.
5. **Given** repeated startup, backfill, or seed execution, **When** grade records are inspected, **Then** the community still has exactly one record for each supported value and no unsupported legacy row was silently deleted or remapped.

---

### User Story 2 - Replace a Teacher's Class Assignments (Priority: P1)

A Community Owner assigns an Active Teacher to zero, one, or multiple Active classes in the Owner's current community. The submitted set replaces the Teacher's complete current class assignment set.

**Why this priority**: Teacher-to-class ownership is the core new relationship and is required before class and grade teacher filters can be useful.

**Independent Test**: As an Active Owner, replace an Active Teacher's assignments with one class, several classes across grades, another set, and an empty set; verify each result and verify that invalid or foreign resources leave the original set unchanged.

**Acceptance Scenarios**:

1. **Given** an Active Teacher and one Active class in the same current community, **When** the Owner sends `PUT /api/v1/Communities/teachers/{teacherUserId}/classes` with that class identifier, **Then** the Teacher is assigned to exactly that class.
2. **Given** an Active Teacher and several Active same-community classes, **When** the Owner submits all their identifiers, **Then** the Teacher is assigned to every submitted class, including classes from different grades.
3. **Given** an existing assignment set, **When** the Owner submits a different valid set, **Then** assignments absent from the request are removed and submitted assignments are present after one atomic replacement.
4. **Given** an existing assignment set, **When** the Owner submits an empty `classIds` array, **Then** all current class assignments for that Teacher are removed.
5. **Given** duplicate class identifiers in a valid request, **When** the replacement is processed, **Then** duplicates are treated as one requested assignment and no duplicate relationship is created.
6. **Given** a Pending or Removed Teacher, a non-Teacher membership, a Teacher from another community, an inactive class, or a class from another community, **When** replacement is attempted, **Then** the request is rejected and the previous assignment set is unchanged.
7. **Given** an Active Teacher, **When** that Teacher attempts to assign themselves or another Teacher, **Then** the Owner-only operation is denied.

---

### User Story 3 - Page and Filter the Teacher Roster (Priority: P1)

A Community Owner views a paginated Teacher roster for the current community, optionally searches or filters it by membership status, assigned class, or grade. Each Teacher row includes the Teacher's assigned Active classes and their fixed grade values.

**Why this priority**: Owners need a practical way to understand staffing and confirm class assignments without loading or scanning the entire Teacher roster.

**Independent Test**: Populate Teachers with overlapping and empty assignment sets, request multiple pages and every supported filter combination, and verify total counts, unique Teacher rows, assignment details, and tenant isolation.

**Acceptance Scenarios**:

1. **Given** more Teachers than fit on one page, **When** the Owner requests `GET /api/v1/Communities/teachers` with `pageNumber` and `pageSize`, **Then** the standard paginated response returns the requested page and correct total record and page counts.
2. **Given** Teachers with and without assignments, **When** no grade or class filter is supplied, **Then** all otherwise eligible Teacher memberships can appear and Teachers with no assignments return an empty class collection.
3. **Given** multiple Teachers assigned to one Active class, **When** that same-community `classId` is supplied, **Then** only Teachers assigned to that exact class are returned.
4. **Given** Teachers assigned to Active classes in one grade, **When** that same-community `gradeId` is supplied, **Then** each matching Teacher is returned once even if assigned to multiple classes in that grade.
5. **Given** matching `gradeId` and `classId` values for one current-community class, **When** both filters are supplied, **Then** the result contains Teachers assigned to that class.
6. **Given** a class that does not belong to the supplied grade, a foreign-community grade or class, or an inactive class, **When** the filter is submitted, **Then** the request fails safely without revealing another community's ownership or Teacher data.
7. **Given** a search value or membership-status filter, **When** it is supplied, **Then** it is combined with pagination and any grade/class filters using the existing Teacher identity and membership meanings.
8. **Given** a returned Teacher, **When** their class collection is inspected, **Then** each Active assignment contains class identifier, class name, grade identifier, and integer grade value, with no separate Teacher-grade record.

---

### User Story 4 - Preserve the Existing Student Roster Experience (Priority: P2)

Community Owners and Teachers continue using the existing paginated student roster and detail operations with status, search, grade, and class filters. The new grade representation and Teacher features do not redesign student placement or require a new Student-grade relationship.

**Why this priority**: Student filtering already satisfies the needed workflow and is security-sensitive. The feature must verify and preserve it instead of replacing working behavior.

**Independent Test**: Exercise the existing student list with pagination, each filter, combined filters, search, status, foreign identifiers, and both staff roles; then open a returned student detail and compare the established response shape.

**Acceptance Scenarios**:

1. **Given** students spread across several classes and grades, **When** an Owner or Teacher requests `GET /api/v1/Communities/students` with page parameters, **Then** the existing paginated response and counts are preserved.
2. **Given** a valid current-community grade, **When** `gradeId` is supplied, **Then** only students placed in that grade through the existing license/class relationships are returned.
3. **Given** a valid Active current-community class, **When** `classId` is supplied, **Then** only students placed in that class are returned.
4. **Given** matching grade and class filters, **When** both are supplied, **Then** only students satisfying that placement are returned; mismatched filters fail through the established safe error behavior.
5. **Given** existing status and search filters, **When** they are combined with pagination or placement filters, **Then** their current behavior remains available.
6. **Given** a grade, class, license, or student profile owned by another community, **When** it is supplied to a roster or detail request, **Then** no foreign-community data is returned.
7. **Given** the recently established staff-community context, **When** either student operation is called, **Then** the frontend sends no community identifier and existing handler-level role and tenant checks still apply.

---

### User Story 5 - Start with a Complete Development Demo School (Priority: P2)

A developer can explicitly enable one development-only demo school and immediately test Owner login, Teacher login, grade/class filtering, Teacher assignments, student filtering, pagination, and student detail without manually constructing data.

**Why this priority**: Representative, repeatable data shortens development and verification time for every user-facing workflow in this feature.

**Independent Test**: Enable the demo seed in a Development environment, start the application twice, sign in using each documented staff credential, exercise the grade, Teacher, and student operations, and verify stable record counts and relationships after the second run.

**Acceptance Scenarios**:

1. **Given** the application is not running in Development or the demo seed is not explicitly enabled, **When** startup occurs, **Then** no demo community or demo identity is created.
2. **Given** Development and an explicit enabled setting, **When** startup occurs, **Then** one Active community named `SprintLabs Demo School` with slug `sprintlabs-demo-school` is available with fixed grades 7-12 and sufficient configured capacity under the current license rules.
3. **Given** the demo community, **When** classes are inspected, **Then** it contains at least Class 7A, Class 7B, Class 8A, Class 8B, Class 9A, and Class 10A, linked to their matching fixed grades.
4. **Given** the documented Owner and three Teacher credentials, **When** each signs in through the existing community password-login operation, **Then** login succeeds immediately with one Active staff membership in the demo community.
5. **Given** the three demo Teachers, **When** assignments are inspected, **Then** Teacher 1 teaches 7A and 7B, Teacher 2 teaches 8A and 8B, and Teacher 3 teaches 7A, 9A, and 10A.
6. **Given** the demo community, **When** the student roster is listed, **Then** at least 20 deterministic Active students are available across multiple classes and grades, with multiple students per class and usable student details.
7. **Given** repeated enabled Development startups, **When** demo records are inspected, **Then** no duplicate community, grade, class, identity, membership, student license, student profile, or Teacher assignment exists and license counters still agree with the existing domain counting rules.

### Edge Cases

- A legacy grade name is a trimmed integer from `7` through `12` or the case-insensitive text `Grade 7` through `Grade 12`; it can be recognized without changing the grade's identity or breaking references.
- A legacy grade has a custom or ambiguous name or an out-of-range value. It remains stored for migration safety, is excluded from the normal fixed-grade API, cannot be selected by new grade-linked records, and is reported for manual remediation without changing existing references.
- Multiple recognizable legacy rows represent the same supported value. They are reported as a data-integrity conflict and are not arbitrarily selected, merged, remapped, or deleted; the conflict requires safe remediation while preserving referenced identifiers where possible.
- A community has supported grades missing alongside unsupported legacy rows; only missing supported values are added, unsupported rows remain unchanged, and repeated backfill creates no duplicate supported rows.
- A new Class, StudentLicense, or other new grade-linked record supplies an unsupported legacy Grade identifier; the operation is rejected without changing the legacy Grade or any existing record that references it.
- The same Teacher/class replacement is submitted repeatedly or concurrently; the final relationship set contains at most one row per Teacher and class.
- One invalid class identifier appears among otherwise valid replacement identifiers; validation rejects the whole operation before any assignment changes.
- A class becomes inactive after being assigned; it is no longer a valid replacement target or an Active class match and is omitted from active assignment details.
- A Teacher membership becomes Pending or Removed after assignments exist; assignment management remains denied and the membership status remains authoritative for access.
- A Teacher is assigned to several classes in one grade; the Teacher appears only once in a grade-filtered page while all relevant Active class details can be returned.
- Pagination parameters are zero, negative, or outside accepted limits; established request validation rejects them without running an unbounded roster query.
- A supplied grade and class both belong to the current community but the class belongs to another grade; the request fails before roster data is returned.
- Demo seed configuration is accidentally enabled outside Development; the environment restriction prevents all demo creation.
- Demo capacity or usage counters are reconciled using the current Community license and membership rules; seeding does not introduce a new Teacher-seat or Student-license counting rule.
- A demo email already identifies an incompatible existing identity; seeding stops with an actionable error rather than changing privileges, passwords, or community ownership.
- Demo seeding is interrupted partway through; a later run reconciles deterministic records without duplicating completed items or making counters exceed actual active usage.

## Requirements *(mandatory)*

### Functional Requirements

#### Fixed Grade Reference Data

- **FR-001**: SprintLabs MUST support exactly the integer grade values 7, 8, 9, 10, 11, and 12 for community grades; no grade-system, curriculum, catalog, category, or configurable-grade concept is part of this feature.
- **FR-002**: Each community MUST have exactly one supported Grade row for each value 7 through 12 and MUST NOT have duplicate supported rows for the same community and value. A new or fully remediated community therefore has exactly six Grade rows; a community awaiting manual remediation MAY temporarily contain additional unsupported legacy Grade rows.
- **FR-003**: Every successfully created community MUST receive all six fixed grades automatically without a separate Owner or Teacher action.
- **FR-004**: Existing communities MUST receive any missing supported grades through a repeatable backfill that does not create duplicate supported rows and does not delete or remap unsupported legacy rows.
- **FR-005**: Existing grade identifiers and all class, student-license, and other discovered references MUST be preserved whenever a legacy row is safely recognized as one of the six supported values; existing records referencing unsupported legacy rows MUST remain unchanged unless handled by a separate explicit remediation.
- **FR-006**: Automatic recognition of legacy text MUST be limited to a trimmed integer `7`-`12` or case-insensitive `Grade 7`-`Grade 12`. Custom, ambiguous, out-of-range, or duplicate-equivalent legacy data MUST remain available for migration safety and MUST be reported and documented for manual remediation rather than guessed, silently remapped, merged, or deleted.
- **FR-007**: Supported Grade rows and all new grade contracts MUST use an integer value rather than a configurable display name or sort order. Unsupported legacy rows MAY retain the state needed to preserve them safely until explicit remediation.
- **FR-008**: Active Community Owners and Teachers MUST be able to list current-community grades through `GET /api/v1/Communities/grades` without supplying a community identifier.
- **FR-009**: The normal grade list MUST return exactly six entries containing `id` and integer `value`, ordered 7 through 12. Unsupported legacy rows MUST be excluded; a duplicate or ambiguous supported-value conflict MUST fail safely rather than arbitrarily selecting a row.
- **FR-010**: The staff-facing `POST /api/v1/Communities/grades` operation MUST be removed or disabled, and no Community Owner or Teacher operation MAY create, edit, delete, or reorder grades.
- **FR-011**: Every newly created Class, StudentLicense, or other newly created grade-linked entity discovered in scope MUST reference a Grade that belongs to the current Community and has a supported integer value from 7 through 12. Classes MUST continue to reference a Grade identifier. Existing records that reference unsupported legacy Grades MUST NOT be destructively changed automatically.

#### Teacher Class Assignments

- **FR-012**: A Teacher MUST be able to have zero, one, or multiple class assignments, including assignments to classes in different grades.
- **FR-013**: A Teacher's grades MUST be derived only from assigned classes and their grades; no separate Teacher-grade relationship MAY be created.
- **FR-014**: Each Teacher/class pair MUST be unique, including under repeated or concurrent requests.
- **FR-015**: The system MUST expose Owner-only `PUT /api/v1/Communities/teachers/{teacherUserId}/classes` with a `classIds` array and no request-side community identifier.
- **FR-016**: The replacement operation MUST treat the request as the Teacher's complete desired assignment set; an empty set removes all assignments and duplicate identifiers are normalized to one requested class.
- **FR-017**: Before changing any assignment, the system MUST verify that the target user has an Active Teacher membership in the resolved current community and that every requested class is Active and owned by that same community.
- **FR-018**: The replacement MUST validate the complete requested set before modifying anything and MUST either apply the entire valid set or leave the previous set unchanged.
- **FR-019**: A Teacher, Pending or Removed member, Student, unrelated user, or platform-admin-only user without the required Owner membership MUST NOT manage Teacher class assignments.
- **FR-020**: Cross-community Teacher or class identifiers MUST produce the established safe not-found/forbidden behavior without revealing which community owns the resource.
- **FR-021**: An inactive class MUST not be accepted as an assignment target or treated as an Active Teacher-filter match.

#### Paginated Teacher Roster and Filters

- **FR-022**: The existing Owner-only Teacher roster operation MUST remain Owner-only and return the standard paginated response rather than an unpaged list.
- **FR-023**: `GET /api/v1/Communities/teachers` MUST support `pageNumber` and `pageSize`, defaulting to the established values of 1 and 10.
- **FR-024**: The Teacher roster MUST support optional `gradeId` and `classId` filters and MUST continue to accept no request-side community identifier.
- **FR-025**: The Teacher roster MUST support optional search and membership-status filters consistent with current identity and Community membership meanings.
- **FR-026**: A class filter MUST be validated as an Active class in the current community and MUST return only Teacher memberships assigned to that exact class.
- **FR-027**: A grade filter MUST be validated as a grade in the current community and MUST return a Teacher once when the Teacher is assigned to at least one Active class in that grade.
- **FR-028**: When both grade and class filters are supplied, the class MUST belong to the grade and both MUST belong to the current community before any Teacher results are returned.
- **FR-029**: Foreign, stale, mismatched, or inactive filter resources MUST fail safely without exposing Teacher data or foreign ownership.
- **FR-030**: With no grade or class filter, the roster MUST preserve the current Teacher membership visibility, including Teachers with zero class assignments and membership status in the response.
- **FR-031**: Each Teacher response MUST contain user identifier, name, email, membership status, creation time, and an `classes` collection.
- **FR-032**: Each returned Active class assignment MUST contain class identifier, class name, grade identifier, and integer grade value.
- **FR-033**: Filtering, distinct Teacher counting, and pagination MUST be applied before the page is returned so Teachers are not duplicated and page metadata reflects the filtered result set; identity and assignment retrieval MUST remain bounded per page rather than adding a separate retrieval for every Teacher.

#### Student Roster Compatibility Verification

- **FR-034**: The existing `GET /api/v1/Communities/students` operation MUST remain available to Active Owners and Teachers through server-resolved current-community context.
- **FR-035**: The student roster MUST preserve its standard paginated response, `pageNumber`, `pageSize`, optional `gradeId`, `classId`, `status`, and `search` behavior.
- **FR-036**: Student grade and class filtering MUST continue to derive placement from the existing student-license and class relationships; no new Student-grade relationship MAY be introduced.
- **FR-037**: A student grade or class filter MUST be validated against the current community, and combined grade/class filters MUST require the class to belong to the selected grade.
- **FR-038**: Student filtering, counting, and pagination MUST remain tenant-scoped and MUST not return or count students from another community.
- **FR-039**: Existing student detail access and its response contract MUST remain compatible with the roster and current-community authorization.
- **FR-040**: The student roster implementation MUST be changed only where focused verification proves a defect introduced or exposed by fixed grades; otherwise its existing behavior MUST be preserved.

#### Development Demo Community

- **FR-041**: Demo seeding MUST require both `DemoCommunitySeed:Enabled` set to true and a Development environment; it MUST create no demo data in any other environment.
- **FR-042**: Enabled Development seeding MUST create or reconcile exactly one Active `SprintLabs Demo School` community with slug `sprintlabs-demo-school`.
- **FR-043**: The demo community MUST contain exactly one of each fixed grade value and at least Class 7A, Class 7B, Class 8A, Class 8B, Class 9A, and Class 10A linked to their matching grades.
- **FR-044**: The demo community MUST have sufficient configured capacity for all seeded data under the existing Community license, StudentLicense, and Teacher-membership rules. Every seeded maximum and usage counter MUST match the current domain's counting semantics; this feature MUST NOT introduce or change Teacher-seat, Student-license, or other license-consumption rules.
- **FR-045**: The seed MUST create or reconcile one Active Owner with email `owner.demo@sprintlabs.local` and password `SprintLabsDemo!2026` using the established account password mechanism, never a manually constructed password representation.
- **FR-046**: The seed MUST create or reconcile three Active Teachers with emails `teacher1.demo@sprintlabs.local`, `teacher2.demo@sprintlabs.local`, and `teacher3.demo@sprintlabs.local`, each using password `SprintLabsDemo!2026`, the existing Teacher-authentication eligibility flags, and exactly one Active Teacher membership in the demo community.
- **FR-047**: The seeded Owner and Teachers MUST be immediately eligible for the existing community password-login operation without invitation acceptance, while normal non-seed Teacher invitation rules remain unchanged.
- **FR-048**: Demo Teacher assignments MUST be Teacher 1 to 7A/7B, Teacher 2 to 8A/8B, and Teacher 3 to 7A/9A/10A so the specified class and grade filters return overlapping results.
- **FR-049**: The seed MUST create or reconcile at least 20 deterministic Active students distributed across multiple classes and grades, including multiple students in at least four classes and enough records to exercise pagination.
- **FR-050**: Each demo student MUST contain the real existing identity, membership, license, profile, class, and supported fixed-grade state required for both student list and student detail operations; working student password authentication is not required.
- **FR-051**: Demo seeding MUST be idempotent across repeated and interrupted runs and MUST use stable business identifiers so no duplicate community, grade, class, user, membership, license, profile, or assignment is created.
- **FR-052**: If a stable demo identifier belongs to incompatible existing data, the seed MUST stop with an actionable error instead of overwriting privileges, passwords, memberships, or tenant ownership.

#### Server-Owned Context, Security, Compatibility, and Delivery

- **FR-053**: Every staff operation in this feature MUST derive the community from the authenticated user's trusted identifier and current persisted staff membership through the existing current-community resolution behavior.
- **FR-054**: No grade list, Teacher list/filter, Teacher assignment, student list, or student detail request MAY accept a community identifier from the frontend.
- **FR-055**: Current-community resolution MUST remain tenant-context resolution only; existing handler-level account-status, role, membership, and resource-ownership authorization MUST remain in force.
- **FR-056**: The one-current-staff-community invariant across Owner and Teacher roles and Pending/Active statuses MUST remain enforced and fail closed; this feature MUST NOT weaken invitation, activation, login, refresh, or Owner-assignment protections.
- **FR-057**: `GET /api/v1/Communities/{communityId}` MUST remain available for its established Student and backward-compatible community-profile access.
- **FR-058**: Existing Platform Admin operations MUST retain their explicit community identifiers and MUST not adopt the staff current-community contract.
- **FR-059**: The feature MUST preserve the established response envelope, controlled error, repository, request/response, and vertical-slice conventions without introducing a generic tenancy, curriculum, catalog, or repository framework.
- **FR-060**: Any schema evolution MUST be non-destructive, preserve supported and unsupported legacy Grade rows, existing grade references, and unrelated data, and add only constraints that remain compatible with preserved legacy data, Student memberships, Removed staff memberships, and the supported database behavior.
- **FR-061**: The feature MUST include focused verification for fixed-grade creation/backfill/idempotency, preservation and reporting of unsupported legacy grades, exclusion of unsupported grades from the normal grade API and all new grade-linked records, new-community grades, absence of grade mutation, assignment replacement/security, Teacher pagination/filtering/response, existing student filters/security, demo content/idempotency under current license rules, and current-community route compatibility.
- **FR-062**: Feature API documentation MUST describe endpoint, method, authorization, request, success response, common errors, and frontend usage notes for every added or changed contract.
- **FR-063**: Feature frontend documentation MUST describe fixed-grade display with no mutation UI, Teacher roster pagination/filters/class assignment, preserved student roster behavior, loading/empty/error/permission states, `DemoCommunitySeed:Enabled`, and all four exact Development-only demo credentials.
- **FR-064**: The implementation MUST remain limited to fixed grades, Teacher class assignments and roster filtering, student-filter verification, demo data, required schema evolution, focused tests, and documentation; authentication redesign, new grade systems, curriculum catalogs, Student redesign, generic tenancy, and unrelated refactors remain out of scope.
- **FR-065**: This feature MUST supersede the mutable-grade contracts in features 006 and 018 while preserving feature 018's community-ID-free staff routes, Student compatibility route, and explicitly scoped Platform Admin routes.

### Key Entities *(include if feature involves data)*

- **Grade**: A Community-owned grade record. Supported Grades have one of the fixed integer values 7-12 and are the only Grades selectable by new grade-linked data. Unsupported legacy Grade rows may remain temporarily for safe remediation but are excluded from the normal grade list.
- **Class**: Community-managed instructional grouping with a name, one Grade, and Active or inactive lifecycle state. New Classes must use a supported fixed Grade; existing legacy references are preserved until explicit remediation.
- **Teacher Class Assignment**: The unique relationship between one Teacher identity and one Class. The relationship grants no role or tenant access and derives the Teacher's grades through the assigned Class.
- **Community Membership**: The authoritative relationship that identifies a user as Owner, Teacher, or Student and controls whether the user is Active, Pending, or Removed in a Community.
- **Teacher Roster Item**: A Teacher identity and membership summary plus zero or more Active assigned classes, returned within the standard paginated result.
- **Student License**: The existing Community-owned student placement and license record that relates a student to Grade and Class and remains authoritative for roster filtering. New Student Licenses must use a supported fixed Grade; existing legacy references are not automatically rewritten.
- **Demo Community Dataset**: The deterministic, Development-only community, staff identities, memberships, grades, classes, assignments, students, profiles, licenses, and capacity counters used for manual development verification.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: After new-community setup, backfill, or any repeated enabled seed run, 100% of communities contain exactly one supported Grade row for each value 7-12 and zero duplicate supported community/value pairs; preserved unsupported legacy rows do not count toward those six.
- **SC-002**: 100% of tested valid grade-list responses return exactly six integer values in ascending order, return zero unsupported legacy rows, and 100% of tested staff grade-mutation requests have no available operation that changes grade reference data.
- **SC-003**: 100% of valid Teacher assignment replacements produce exactly the normalized requested set, while every tested invalid or foreign-resource request changes zero assignments.
- **SC-004**: Across sequential and concurrent verification, every Teacher/class pair occurs at most once and an empty replacement removes 100% of that Teacher's assignments.
- **SC-005**: Every tested Teacher page reports accurate total records/pages after filters; class and grade filters return only matching current-community Teachers, and a Teacher assigned to multiple same-grade classes appears once.
- **SC-006**: Every returned Teacher roster item contains an assignment collection; Teachers with no assignments contain an empty collection and assigned classes carry the correct integer grade value.
- **SC-007**: A roster page of up to 100 Teachers with assignment details is available to the Owner within two seconds under normal development test conditions.
- **SC-008**: 100% of existing focused student pagination, grade, class, combined-filter, search, status, role, and tenant-isolation scenarios continue to pass without a new Student placement model.
- **SC-009**: With demo seeding disabled or outside Development, zero demo records are created; with it enabled in Development, one usable demo community with one Owner, three Teachers, the required assignments, and at least 20 students is available.
- **SC-010**: Running the enabled Development seed at least three times leaves the same deterministic record counts, no duplicate stable-key records, and license maximum/usage counters equal to the values required by the existing domain counting rules.
- **SC-011**: All four documented demo staff accounts can complete the existing community password-login flow on the first valid attempt and resolve only `SprintLabs Demo School`.
- **SC-012**: 100% of cross-community grade, class, Teacher, and student identifier tests return no foreign data and make no foreign changes, while current Student profile and Platform Admin compatibility operations remain functional.
- **SC-013**: Frontend developers can implement every changed screen and call from the feature API/frontend documents without adding a community selector, grade mutation UI, Teacher-grade model, or undocumented demo setup step.
- **SC-014**: 100% of tested attempts to create a Class, StudentLicense, or other new grade-linked record with an unsupported or foreign-community Grade are rejected, while existing records referencing unsupported legacy Grades remain unchanged.

## Assumptions

- The current server-owned staff community resolver remains the sole tenant source for Owner and Teacher operations introduced or changed here.
- The current Teacher roster remains an Owner-only management view; this feature does not expand Teacher-to-Teacher visibility.
- The standard paginated request defaults remain page 1 and page size 10, and established validation supplies the accepted upper page-size bound.
- Unfiltered Teacher listing preserves current membership-status visibility, while class/grade matches are derived from assignments to Active classes.
- Duplicate class identifiers are normalized because the replacement body represents a set, not a sequence.
- Existing student roster and detail response fields remain compatible; any fixed grade displayed through a legacy grade-name field may be formatted from the integer without forcing an unrelated student contract redesign.
- Existing `Player` or question grade integers are not community Grade foreign keys unless implementation inspection proves otherwise; they are not migrated merely because they contain a grade-like number.
- Legacy grade rows matching only the explicitly recognized formats can be migrated automatically while preserving their identifiers where safely possible. Unsupported or conflicting legacy rows may remain stored and referenced during manual remediation, but they are excluded from supported-grade selection and the normal fixed-grade API.
- The current Community license, StudentLicense, Teacher invitation/membership, and removal rules remain authoritative for capacity and usage counters. Demo seeding supplies sufficient capacity and reconciles counters to those rules without adding a new consumption model.
- Demo students need the authentic domain state required by roster and detail operations but do not need passwords or a new authentication journey.
- Development credentials are intentionally shared demo secrets and must be documented as DEVELOPMENT ONLY; they are never a production provisioning mechanism.
- Classes remain Community-managed, including their existing create, list, edit, and soft-delete behavior.
