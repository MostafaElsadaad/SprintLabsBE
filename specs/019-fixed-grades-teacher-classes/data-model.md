# Data Model: Fixed Grades, Teacher Classes, and Demo Community

## Grade (modified)

Community-owned reference data. The existing entity and identifiers remain.

| Field | Type | Rules |
|---|---|---|
| `Id` | `long` | Existing primary key; preserved for recognized legacy rows. |
| `CommunityId` | `long` | Required FK to Community; tenant owner. |
| `Value` | `int?` | New. Non-null values are restricted to 7-12. `NULL` means preserved unsupported/unresolved legacy data. |
| `Name` | `string` | Retained legacy/remediation metadata. Canonical supported rows use `Grade N`; not exposed as the fixed-grade contract. |
| `SortOrder` | `int` | Retained for schema compatibility. Canonical supported rows use `N`; fixed API ordering uses `Value`. |
| `CreatedAt` | `DateTime` | Existing timestamp. |

Relationships:

- Community 1 -> many Grades.
- Grade 1 -> many Classes (`Restrict` delete).
- Grade 1 -> many StudentLicenses (`Restrict` delete).

Constraints:

- Check: `Value IS NULL OR Value IN (7,8,9,10,11,12)`.
- Unique: `(CommunityId, Value)`. Multiple `NULL` legacy values are permitted by MySQL.
- A valid/current community has exactly one non-null row for every value 7-12.
- New Class, StudentLicense, or other in-scope grade-linked data may reference only a same-community Grade with a supported non-null Value.
- Existing references to a `Value = NULL` Grade remain readable and are not automatically repointed.

Legacy state transitions:

```text
unique exact legacy representation -> same row gains supported Value (ID/FKs preserved)
no legacy representation           -> canonical supported row inserted
custom/out-of-range representation -> Value remains NULL; manual remediation
duplicate exact representations    -> migration aborts unchanged; manual remediation, then rerun
```

There is no automatic transition from unresolved legacy data to a supported value after migration; that requires explicit operator remediation outside this feature. The runtime Grade API also verifies the exact supported set and fails closed if the persisted invariant is later incomplete.

## Class (existing, relationship extended)

Relevant existing fields: `Id`, `CommunityId`, `GradeId`, `Name`, `Status`, `CreatedAt`.

New/clarified rules:

- New and updated Classes must reference a Grade in the same Community with Value 7-12.
- Only `ClassStatus.Active` Classes may be newly assigned to Teachers or used as Teacher filter matches.
- Existing Classes on unsupported legacy Grades are preserved.
- Add navigation collection to TeacherClassAssignments.

## TeacherClassAssignment (new)

Scheduling join data between an Identity user acting as a Teacher and a Class.

| Field | Type | Rules |
|---|---|---|
| `Id` | `long` | Primary key, following entity conventions. |
| `TeacherUserId` | `long` | Required FK to Identity User; application requires an Active Teacher CommunityUser in the Class Community. |
| `ClassId` | `long` | Required FK to Class. |
| `CreatedAt` | `DateTime` | Required/default timestamp following existing conventions. |
| `Class` | `Class` | Navigation used to derive Community and Grade. |

Constraints and indexes:

- Unique `(TeacherUserId, ClassId)`.
- Index `ClassId` for roster/filter joins.
- User and Class foreign keys use `Restrict` delete behavior.
- No `CommunityId`, `GradeId`, or TeacherGrade relationship is stored.

Application invariants on create/replace:

- Target CommunityUser has Role Teacher, Status Active, and CommunityId equal to the resolved current Community.
- Class is Active, belongs to that Community, and references a supported Grade in that Community.
- The requesting user independently passes existing Active Owner authorization.
- Assignment existence never grants Community authorization.

Replacement state transition:

```text
existing set + normalized fully validated desired set
    -> retain intersection
    -> delete existing minus desired
    -> insert desired minus existing
    -> one successful SaveChanges commits the complete set
```

An empty desired set removes all target assignments in the current Community. Any validation failure preserves the previous set.

## CommunityUser (existing, unchanged)

Relevant roles/statuses:

- Owner/Active: may manage assignments and Teacher roster.
- Teacher/Active: may use Owner-or-Teacher grade/student/class reads but cannot manage assignments or roster where existing rules are Owner-only.
- Teacher/Pending or Removed: may appear in an unfiltered Owner roster according to existing behavior, but cannot receive a new class assignment.
- Student: remains unrelated to TeacherClassAssignment.

The existing one-current-staff-community invariant across Owner and Teacher and Pending/Active states is unchanged. Teacher assignments do not participate in or weaken it.

## Teacher Roster Projection (modified contract)

`TeacherResponse`:

| Field | Type |
|---|---|
| `UserId` | `long` |
| `Name` | `string` |
| `Email` | `string` |
| `Status` | `string` |
| `CreatedAt` | `DateTime` |
| `Classes` | `List<TeacherClassResponse>` |

`TeacherClassResponse`:

| Field | Type |
|---|---|
| `ClassId` | `long` |
| `ClassName` | `string` |
| `GradeId` | `long` |
| `Grade` | `int` (7-12) |

Only Active same-community Classes on supported Grades appear in `Classes`. The roster is a paged projection, not a persisted Teacher profile.

## Community and CommunityLicense (existing, seed usage)

The demo Community is keyed by slug `sprintlabs-demo-school`, Active, and owns its Grades, Classes, CommunityUsers, StudentLicenses, and CommunityLicense.

Existing license meanings remain:

- `UsedStudents` = count of demo-community StudentLicenses whose status is not Revoked.
- `UsedTeachers` = count of demo-community Teacher CommunityUsers whose status is Pending or Active.
- Owner membership does not consume Teacher capacity.
- Maximums remain at least actual use and the configured demo baseline; a reconciliation never reduces compatible existing capacity.

## Demo Identity and Student Graph (existing entities, deterministic records)

Staff graph:

```text
Identity User
  -> Active CommunityUser (Owner or Teacher)
  -> TeacherClassAssignment(s) for Teachers only
```

Student graph:

```text
Identity User (no password required)
  -> Player / PlayerProfileId
  -> Active Student CommunityUser
  -> Active StudentLicense
       -> Community
       -> supported Grade
       -> Active Class
       -> AssignedBy demo Owner
```

Deterministic class distribution:

| Class | Students |
|---|---:|
| Class 7A | 4 |
| Class 7B | 4 |
| Class 8A | 4 |
| Class 8B | 3 |
| Class 9A | 3 |
| Class 10A | 2 |

Stable lookup keys are community slug, normalized email, community/value, community/grade/class name, community/user, Teacher/Class, and student identity/license links. A stable key pointing to incompatible tenant/role/password state is an error, not an update target.

## Unchanged Related Data

- `Player.Grade` and `QuestionsJson.Grade` are scalar fields, not Grade foreign keys, and are not part of the Grade migration.
- Student placement remains derived from StudentLicense/Class/Grade; no StudentGrade entity is introduced.
- Platform Admin community selection and Student community-profile compatibility routes do not change.
