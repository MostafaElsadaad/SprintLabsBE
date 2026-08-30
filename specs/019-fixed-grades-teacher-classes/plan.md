# Implementation Plan: Fixed Grades, Teacher Classes, and Demo Community

**Branch**: `codex/fixed-grades-teacher-classes` | **Date**: 2026-08-30 | **Spec**: [spec.md](spec.md)

**Input**: Feature specification from `specs/019-fixed-grades-teacher-classes/spec.md`

## Summary

Replace mutable community grade setup with six fixed integer grades (7-12), preserving recognizable Grade identifiers and retaining ambiguous legacy rows for manual remediation. Add an Owner-managed `TeacherClassAssignment` join entity and a database-paged Teacher roster with class/grade filters and bounded identity loading. Keep the existing student roster implementation, changing only strict validation for newly written grade references and fixed-grade response formatting. Add a doubly gated, transactional, idempotent Development demo seeder using the real Identity, membership, license, Player, and current-community models.

The implementation stays inside existing controllers, MediatR slices, `IBaseRepository<T>`, EF Core/MySQL mappings, `IUserService`, `BaseResponse`, and `GenericException`. Staff HTTP contracts continue resolving `CommunityId` from the authenticated `userId`; existing handler authorization remains authoritative.

## Technical Context

**Language/Version**: C# 12 / .NET 8

**Primary Dependencies**: ASP.NET Core, API versioning, MediatR, ASP.NET Core Identity/UserManager, EF Core 8, Pomelo MySQL provider

**Storage**: MySQL through `ApplicationDbContext`; migrations in `Infrastructure/Migrations`

**Testing**: xUnit, Moq, EF Core InMemory for focused handler/model tests, existing opt-in MySQL fixture for provider-specific constraints where available

**Target Platform**: SprintLabs ASP.NET Core web API

**Project Type**: Layered .NET web service with CQRS vertical slices (`API`, `Application`, `Domain`, `Infrastructure`, `Shared`, `SprintLabs.Tests`)

**Performance Goals**: A filtered Teacher page of up to 100 records, including Active assignments, completes within two seconds in normal development conditions; filtering, counting, and paging execute in the database; identity and assignment loading remain bounded per page.

**Constraints**: No client-supplied `communityId` on staff routes; no Grade catalog/system, `TeacherGrade`, tenancy middleware, new repository abstraction, authentication redesign, new package, or destructive legacy-grade cleanup. New grade-linked writes accept only same-community supported grades. Demo data requires both Development and explicit enablement.

**Scale/Scope**: Six supported grades per community; one new join table; one replacement endpoint; one paged roster contract; existing student list/detail compatibility; one deterministic demo community with four staff users and 20 students.

**Clarifications**: None. Phase 0 research resolved the schema, MySQL uniqueness, identity batching, student compatibility, license counters, and seed reconciliation decisions.

## Constitution Check

*GATE: Passed before research and re-checked after design.*

| Principle | Design compliance |
|---|---|
| Vertical slices | Fixed grades, assignment replacement, roster, seed, migrations, focused tests, and frontend-facing documents are delivered together. |
| Existing architecture wins | Reuses thin controller actions, MediatR handlers, `IBaseRepository<T>`, `IUserService`, `ApplicationDbContext`, `UserManager`, `BaseResponse`, and `GenericException`. |
| SaaS data isolation | Every staff endpoint receives a server-resolved current Community; handlers retain Owner or Owner/Teacher membership checks and validate every Grade/Class/Teacher within that Community. |
| JSON where flexibility matters | Not applicable; no question payload changes. |
| Minimum useful implementation | Uses one join entity, two ordered migrations, no configurable grade abstraction, no custom repository, and no student redesign. |
| Quality gates | Implementation begins with a focused baseline, adds focused security/regression tests, and finishes with `dotnet build SprintLabs.sln`; a full suite is reserved for diagnosis or explicit request. |
| Documentation is executable context | This plan, research, data model, contract, API guide, frontend guide, and quickstart stay under feature 019. |

**Post-design re-check**: Passed. The nullable legacy-compatible `Grade.Value`, two migrations, direct join entity, and static gated seeder are the smallest changes that satisfy integrity and compatibility requirements. No complexity exception is required.

## Project Structure

### Documentation (this feature)

```text
specs/019-fixed-grades-teacher-classes/
├── spec.md
├── plan.md
├── research.md
├── data-model.md
├── quickstart.md
├── api.md
├── frontend.md
├── contracts/
│   └── fixed-grades-teacher-classes-api.md
└── tasks.md                              # Created later by /speckit-tasks
```

### Source Code (repository root)

```text
API/
├── Controllers/CommunitiesController.cs
├── Program.cs
└── appsettings.json

Application/Features/
├── Admin/Communities/CreateCommunity/CreateCommunityCommandHandler.cs
└── Communities/
    ├── GradesClasses/
    │   ├── Common/{GradeResponse.cs,CommunityGradesClassesAuthorization.cs}
    │   ├── CreateClass/CreateClassCommandHandler.cs
    │   ├── UpdateClass/UpdateClassCommandHandler.cs
    │   ├── ListGrades/ListGradesQueryHandler.cs
    │   └── CreateGrade/                  # Removed with obsolete POST contract
    ├── Teachers/
    │   ├── Common/{TeacherResponse.cs,TeacherClassResponse.cs}
    │   ├── ListTeachers/{ListTeachersQuery.cs,ListTeachersQueryHandler.cs}
    │   └── ReplaceTeacherClassAssignments/
    │       ├── ReplaceTeacherClassAssignmentsRequest.cs
    │       ├── ReplaceTeacherClassAssignmentsCommand.cs
    │       └── ReplaceTeacherClassAssignmentsCommandHandler.cs
    ├── StudentLicenses/Common/
    │   ├── StudentLicenseValidation.cs
    │   └── StudentLicenseMapper.cs
    └── Students/
        ├── ListStudents/ListStudentsQueryHandler.cs
        └── GetStudentDetail/GetStudentDetailQueryHandler.cs

Domain/Models/
├── Community.cs
└── TeacherClassAssignment.cs

Infrastructure/
├── DataAccess/ApplicationDbContext.cs
├── Migrations/
│   ├── <timestamp>_FixedCommunityGrades.cs
│   ├── <timestamp>_TeacherClassAssignments.cs
│   └── ApplicationDbContextModelSnapshot.cs
└── Seed/DemoCommunitySeeder.cs

SprintLabs.Tests/Features/
├── CommunityGradesClasses/
├── OwnerTeacherManagement/
├── OwnerStudentLicenseManagement/
├── CommunityStudentViewing/
├── AdminCommunityManagement/
├── CurrentCommunityResolution/
└── DemoCommunitySeed/
```

**Structure Decision**: Extend the existing layered solution and its current feature folders. New CQRS types use one public class per file. Normal queries and persistence continue through `IBaseRepository<T>` and the scoped EF Core context; the seeder uses the existing startup/Identity pattern. No project reference or package is added.

### Expected File Impact

| Change | Files |
|---|---|
| Modify API/startup/config | `API/Controllers/CommunitiesController.cs`, `API/Program.cs`, `API/appsettings.json` |
| Modify fixed Grade model/persistence | `Domain/Models/Community.cs`, `Infrastructure/DataAccess/ApplicationDbContext.cs`, `Infrastructure/Migrations/ApplicationDbContextModelSnapshot.cs` |
| Add schema migrations | `Infrastructure/Migrations/<timestamp>_FixedCommunityGrades.cs` plus designer, then `<timestamp>_TeacherClassAssignments.cs` plus designer |
| Modify Grade/Class CQRS | `Application/Features/Communities/GradesClasses/Common/GradeResponse.cs`, `ListGrades/ListGradesQueryHandler.cs`, `CreateClass/CreateClassCommandHandler.cs`, `UpdateClass/UpdateClassCommandHandler.cs` |
| Remove mutable Grade CQRS | All files under `Application/Features/Communities/GradesClasses/CreateGrade/` and `SprintLabs.Tests/Features/CommunityGradesClasses/CreateGradeCommandHandlerTests.cs` |
| Initialize new-community Grades | `Application/Features/Admin/Communities/CreateCommunity/CreateCommunityCommandHandler.cs` |
| Add Teacher assignments | `Domain/Models/TeacherClassAssignment.cs`; `Application/Features/Communities/Teachers/ReplaceTeacherClassAssignments/{Request,Command,CommandHandler}.cs` |
| Add assignment transaction boundary | `Domain/Services/ITeacherClassAssignmentService.cs`, `Infrastructure/Services/TeacherClassAssignmentService.cs`, `Infrastructure/ServiceConfig.cs` |
| Modify Teacher roster/contracts | `Application/Features/Communities/Teachers/Common/TeacherResponse.cs`, new `TeacherClassResponse.cs`, `Teachers/ListTeachers/ListTeachersQuery.cs`, `ListTeachersQueryHandler.cs` |
| Strict StudentLicense writes / compatible reads | `StudentLicenses/Common/StudentLicenseValidation.cs`, `StudentLicenseMapper.cs`, `Students/ListStudents/ListStudentsQueryHandler.cs`, `Students/GetStudentDetail/GetStudentDetailQueryHandler.cs` |
| Add demo seed | `Infrastructure/Seed/DemoCommunitySeeder.cs` |
| Focused tests | Existing Grade/Class, OwnerTeacherManagement, OwnerStudentLicenseManagement, CommunityStudentViewing, and CurrentCommunityResolution test files; new `AdminCommunityManagement/CreateCommunityCommandHandlerTests.cs`, `OwnerTeacherManagement/ReplaceTeacherClassAssignmentsCommandHandlerTests.cs`, Grade model/migration constraint tests, and `DemoCommunitySeed/DemoCommunitySeederTests.cs` |
| Feature docs | All feature 019 artifacts listed above; add only targeted supersession notes to feature 006/018 API/frontend docs if their mutable Grade tables would otherwise remain misleading |

## Schema and Migration Plan

### Migration 1: `FixedCommunityGrades`

1. Before any DDL or data change, run a duplicate-recognition safety check against the existing Name values. If multiple rows in one community recognize as the same supported value, abort with a concise actionable error directing operators to the documented legacy-grade audit and remediation process. Do not select, merge, remap, or delete any candidate. Detailed Community/value/Grade-ID diagnostics remain the responsibility of the pre-migration queries in `quickstart.md`, not the EF migration.
2. Add nullable `Grade.Value` (`int?`). Nullable values identify preserved unsupported or unresolved legacy rows; `Name` and `SortOrder` remain for compatibility and remediation metadata.
3. Recognize only trimmed `7`-`12` and case-insensitive exact `Grade 7`-`Grade 12` values. If exactly one row represents a supported community/value pair, set `Value` on that existing row so its identifier and all `Class.GradeId` and `StudentLicense.GradeId` references remain unchanged.
4. If no row represents a supported value in a community, insert one canonical row with `Value = N`, `Name = "Grade N"`, and `SortOrder = N`.
5. Leave all custom, ambiguous, and out-of-range rows with `Value = NULL`. Existing references stay intact, but these rows are excluded from the fixed-grade API and rejected by all new grade-linked writes.
6. Add a check constraint equivalent to `Value IS NULL OR Value IN (7,8,9,10,11,12)` and a unique index on `(CommunityId, Value)`. MySQL permits multiple `NULL` values in a unique index, preserving multiple legacy rows while preventing duplicate supported rows.
7. Retain the existing `SortOrder` index and foreign-key delete restrictions. Do not alter unrelated scalar fields such as `Player.Grade` or `QuestionsJson.Grade`.

The migration uses explicit, reviewed MySQL SQL/CASE expressions, a supported-value derived table, and a fail-fast duplicate safety check; it never casts arbitrary text or attempts to produce a detailed remediation report. Before production application, operators run the audit from `quickstart.md` to list unresolved rows, reference counts, and duplicate recognizable groups. A successfully applied migration leaves every Community with six supported rows; the normal API still verifies that invariant and fails closed if later corruption or incomplete data is encountered. The `Down` migration removes the check/index/column but intentionally does not delete inserted grade rows or attempt destructive data rollback.

### Migration 2: `TeacherClassAssignments`

Create `TeacherClassAssignments` with `Id`, `TeacherUserId`, `ClassId`, and `CreatedAt`; foreign keys to Identity Users and Classes use `Restrict`. Add a unique `(TeacherUserId, ClassId)` index and a `ClassId` lookup index. Do not store `CommunityId` or Grade because both are derived through `Class` and validated in application logic. The database uniqueness is the concurrent duplicate backstop; membership role/status and same-community rules remain transactional application validations.

The migrations are separate and ordered so grade data can be audited/remediated independently of Teacher assignments. No other schema migration is planned.

## API and CQRS Plan

### Fixed grades and strict references

- Replace the Grade response with `{ id, value }`; list only supported rows, order by `Value`, and verify the exact set 7-12 before returning. Zero/incomplete/duplicate supported context produces a controlled conflict instead of an arbitrary choice.
- Remove `POST /api/v1/Communities/grades`, its controller action, obsolete `CreateGrade` slice, and creation tests. No edit/delete routes are added.
- Initialize six Grade children on `Community` before the first `SaveChangesAsync` in `CreateCommunityCommandHandler`, keeping community + fixed-grade creation in the existing unit of work.
- Add same-community and supported-Value checks to Create/Update Class and shared Add/Update StudentLicense validation. Existing legacy records are not rewritten. Assignment class validation also requires a supported grade.
- Keep class and student-license route shapes unchanged and keep all current handler authorization.

### Teacher class assignment

- Add `PUT /api/v1/Communities/teachers/{teacherUserId}/classes`. The thin controller obtains the authenticated `userId`, resolves the current staff community with `ICommunityAccessService`, and supplies internal `CommunityId` to the command; no request-side community identifier exists.
- Normalize duplicate class IDs, validate all IDs before staging changes, require the caller to be an Active Owner, the target to be an Active Teacher in that Community, and every target Class to be Active, same-community, and linked to a supported Grade.
- After complete validation, perform the replacement through the existing relational transaction pattern and serialize competing replacements for the same Teacher. Within that transaction, replace the complete current-community set and commit it as one unit; `[]` removes all current-community assignments. Two valid concurrent requests are never merged: every successful commit represents one request's exact normalized set, and if both requests commit, the later successful transaction's set is final. A transaction that fails validation or loses a database transaction conflict changes nothing. The unique `(TeacherUserId, ClassId)` index prevents duplicate pairs, but it is not treated as merge logic. Do not add optimistic concurrency tokens or version columns solely for this feature.
- Assignment data is descriptive scheduling data only. It never substitutes for membership/role authorization.

### Teacher roster

- Make `ListTeachersQuery` follow `PagedRequest` and return `PagedResponse<TeacherResponse>`. Accept `pageNumber`, `pageSize` (maximum 100), optional `gradeId`, `classId`, `search`, and `status`; preserve Owner-only authorization and current status visibility when filters are absent.
- Validate the Grade as supported/current-community, the Class as Active/current-community/on a supported Grade, and the combined relationship before returning data. Use safe not-found behavior for foreign/stale/mismatched filter IDs.
- Build a tenant-scoped `CommunityUser` `IQueryable`, add search IDs once through `IUserService.SearchUserIds`, add assignment `Any` predicates for class/grade, count distinct memberships, then order/page in the database.
- Batch-load the page's users once through `IUserService.GetUsersByIds` and load all page assignment/Class/Grade details in one query. Include only Active, same-community classes on supported Grades, group by Teacher, and return each Teacher once. Teachers without assignments receive `classes: []`.
- Extend `TeacherResponse` with initialized `Classes`; add the small shared `TeacherClassResponse`. Invite/remove responses remain backward-compatible through the additive empty collection.

### Student compatibility

- Preserve the current student list/detail queries, filters, pagination, status/search behavior, database filtering, tenant checks, and server-owned context.
- Do not add supported-grade validation to read filters, because existing records tied to unsupported legacy rows must remain inspectable.
- Where existing responses expose a grade name, format supported `Grade.Value` as invariant numeric text and fall back to retained legacy `Name` only for existing unsupported rows. Apply the same compatibility mapping to StudentLicense responses.
- Add or update focused tests only where fixed-grade fixtures/formatting expose a real compatibility change.

## Development Demo Seed Plan

- Add a static `DemoCommunitySeeder` invoked from the existing startup scope after migrations and the platform-admin seed. Run only when both `IHostEnvironment.IsDevelopment()` and `DemoCommunitySeed:Enabled` are true; add the base setting as `false`.
- Use the existing scoped `ApplicationDbContext` and `UserManager<User>`. On relational providers, use one serializable transaction so failed or interrupted reconciliation leaves no partial committed demo set.
- Reconcile deterministic records using stable keys: community slug; community/value Grades; community/grade/class name; normalized user email; community/user membership; Teacher/class pair; student email/user/player/license relationship.
- Create/reconcile Active confirmed Owner and Teacher accounts using `UserManager` and verify the specified password/eligibility flags. Teachers have `IsTeacherAccount = true`; the Owner does not. Refuse to take over incompatible identities, current staff memberships, tenant ownership, role, or password state.
- Seed classes 7A, 7B, 8A, 8B, 9A, 10A and the specified overlapping Teacher assignments.
- Seed exactly 20 deterministic students (4/4/4/3/3/2 across those classes) through the real Identity User, Player, Active Student `CommunityUser`, Active `StudentLicense`, Grade, Class, and `PlayerProfileId` links. Student passwords are not required.
- Before reconciling the demo CommunityLicense, inspect and follow the current implementation's actual accounting rules for `UsedStudents`, `UsedTeachers`, included StudentLicense statuses, included Teacher membership statuses, and any Owner capacity consumption. If the current code confirms that non-Revoked StudentLicenses count as used students, Pending/Active Teacher memberships count as used teachers, and Owners consume no Teacher capacity, apply those existing rules. If the implementation differs, preserve the actual current rules instead. Recompute usage from persisted demo-community records under those existing semantics and ensure configured maxima are sufficient without reducing existing capacity. This feature must not hardcode or redefine license-accounting business behavior.
- Repeated or compatible partially seeded runs reconcile missing/incorrect demo-owned state without duplicates. Incompatible stable-key state raises an actionable error rather than overwriting access or tenant data.

## Focused Test Strategy

Implementation follows the repository's baseline-before-edit workflow:

1. Before implementation edits, run the smallest existing filters for Community grades/classes, Owner Teacher management, Community students, Student licenses, current-community access/authentication, and any current seed tests. Record the count; do not begin with the full solution suite.
2. Add tests before or with each vertical slice:
   - Grade model/migration metadata, supported ordering/exclusion/fail-closed behavior, new-community creation, strict new Class/StudentLicense references, POST route removal, and idempotent backfill expectations.
   - Assignment single/multiple/replace/empty/duplicate/concurrent behavior, atomic invalid requests, status/role checks, and cross-community Teacher/Class isolation.
   - Teacher page metadata, page-size validation, class/grade/combined filters, distinct results, zero-assignment Teachers, returned classes, batch identity loading, and tenant isolation.
   - Existing student paging/filter/search/status/detail suites, supported numeric formatting, legacy read fallback, and unchanged tenant security.
   - Seed gating, full deterministic content, repeat/partial reconciliation, login password validity, assignment/student distribution, counter semantics, and incompatible-state rollback.
3. After each affected area, run only its focused test namespace/filter. Provider-specific check/unique behavior uses the existing guarded MySQL fixture if configured; no new integration-test framework is introduced.
4. Finish with `dotnet build SprintLabs.sln`, then rerun the combined focused feature filters. Run `dotnet test SprintLabs.sln` only if needed to diagnose cross-feature failures or explicitly requested.

## Documentation and Compatibility

- `api.md` and the contract document define fixed Grade listing, removed mutation, paged Teacher roster, assignment replacement, unchanged Student operations, server-owned context, response envelopes, and safe errors.
- `frontend.md` removes Grade mutation UI, documents Teacher pagination/filters/classes, retains Student behavior, and marks demo configuration/credentials Development-only.
- Feature 019 documentation explicitly supersedes mutable-grade portions of specs 006 and 018. It does not modify `GET /api/v1/Communities/{communityId}` Student/backward compatibility or explicit Platform Admin community-scoped routes.
- `quickstart.md` contains the legacy audit, configuration, migration, focused verification, and end-to-end calls. Ambiguous legacy rows are an operator-visible remediation condition, not silently repaired.

## Complexity Tracking

No constitution violations or additional architecture are required.
