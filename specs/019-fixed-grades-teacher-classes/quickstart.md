# Quickstart and Validation Guide

This guide validates feature 019 after implementation. It intentionally separates non-destructive legacy audit, migration, focused tests, and Development demo setup.

## Prerequisites

- .NET 8 SDK
- A SprintLabs-compatible MySQL database for migration/provider checks
- Existing API user/login setup, or the Development demo seed described below
- Run commands from the repository root

## 1. Record the Focused Baseline Before Implementation Edits

Do not start with the entire solution suite. Record the result of the smallest existing relevant set:

```powershell
dotnet test SprintLabs.Tests/SprintLabs.Tests.csproj --filter "FullyQualifiedName~CommunityGradesClasses|FullyQualifiedName~OwnerTeacherManagement|FullyQualifiedName~CommunityStudentViewing|FullyQualifiedName~OwnerStudentLicenseManagement|FullyQualifiedName~CommunityAccessFoundation|FullyQualifiedName~CurrentCommunityResolution|FullyQualifiedName~CommunityAuthentication"
```

If a named namespace has no tests in the current checkout, keep the remaining filters and record that fact. Seed tests are added to this focused set once implemented.

## 2. Audit Legacy Grade Data Before Migration

Back up the target database according to normal deployment practice. Run these read-only queries before applying `FixedCommunityGrades`.

Recognizable candidates and duplicates:

```sql
SELECT
    g.`CommunityId`,
    CASE
        WHEN LOWER(TRIM(g.`Name`)) IN ('7', 'grade 7') THEN 7
        WHEN LOWER(TRIM(g.`Name`)) IN ('8', 'grade 8') THEN 8
        WHEN LOWER(TRIM(g.`Name`)) IN ('9', 'grade 9') THEN 9
        WHEN LOWER(TRIM(g.`Name`)) IN ('10', 'grade 10') THEN 10
        WHEN LOWER(TRIM(g.`Name`)) IN ('11', 'grade 11') THEN 11
        WHEN LOWER(TRIM(g.`Name`)) IN ('12', 'grade 12') THEN 12
        ELSE NULL
    END AS `RecognizedValue`,
    COUNT(*) AS `CandidateCount`,
    GROUP_CONCAT(g.`Id` ORDER BY g.`Id`) AS `GradeIds`
FROM `Grades` g
GROUP BY g.`CommunityId`, `RecognizedValue`
HAVING `RecognizedValue` IS NOT NULL AND COUNT(*) > 1;
```

Unsupported/custom rows and current reference counts:

```sql
SELECT
    g.`Id`,
    g.`CommunityId`,
    g.`Name`,
    g.`SortOrder`,
    (SELECT COUNT(*) FROM `Classes` c WHERE c.`GradeId` = g.`Id`) AS `ClassReferences`,
    (SELECT COUNT(*) FROM `StudentLicenses` sl WHERE sl.`GradeId` = g.`Id`) AS `StudentLicenseReferences`
FROM `Grades` g
WHERE LOWER(TRIM(g.`Name`)) NOT IN (
    '7', '8', '9', '10', '11', '12',
    'grade 7', 'grade 8', 'grade 9', 'grade 10', 'grade 11', 'grade 12'
)
ORDER BY g.`CommunityId`, g.`Id`;
```

Any duplicate recognizable group requires explicit operator remediation before production rollout. The migration also fails before transformation if such a group remains, so it never chooses or rewrites an ambiguous candidate. Unsupported/custom rows are documented for later remediation but are not deleted, remapped, or a blocker to inserting the six supported rows.

After migration, unresolved rows and references are reported with:

```sql
SELECT
    g.`Id`,
    g.`CommunityId`,
    g.`Name`,
    g.`SortOrder`,
    (SELECT COUNT(*) FROM `Classes` c WHERE c.`GradeId` = g.`Id`) AS `ClassReferences`,
    (SELECT COUNT(*) FROM `StudentLicenses` sl WHERE sl.`GradeId` = g.`Id`) AS `StudentLicenseReferences`
FROM `Grades` g
WHERE g.`Value` IS NULL
ORDER BY g.`CommunityId`, g.`Id`;
```

Verify the supported set:

```sql
SELECT `CommunityId`, `Value`, COUNT(*) AS `RowCount`
FROM `Grades`
WHERE `Value` IS NOT NULL
GROUP BY `CommunityId`, `Value`
ORDER BY `CommunityId`, `Value`;
```

Each Community must show one row for every value 7-12.

## 3. Generate/Review and Apply Migrations

Implementation creates these migrations in order:

1. `FixedCommunityGrades`
2. `TeacherClassAssignments`

Review their generated model metadata and hand-authored MySQL data SQL before applying:

```powershell
dotnet ef migrations list --project Infrastructure --startup-project API
dotnet ef database update --project Infrastructure --startup-project API
```

The grade migration preserves recognized IDs and existing foreign keys. Its rollback removes new constraints/column but does not destructively delete canonical rows inserted during upgrade; use the database backup for a complete data rollback.

## 4. Build and Run Focused Tests

```powershell
dotnet build SprintLabs.sln
dotnet test SprintLabs.Tests/SprintLabs.Tests.csproj --filter "FullyQualifiedName~CommunityGradesClasses|FullyQualifiedName~OwnerTeacherManagement|FullyQualifiedName~CommunityStudentViewing|FullyQualifiedName~OwnerStudentLicenseManagement|FullyQualifiedName~AdminCommunityManagement|FullyQualifiedName~CurrentCommunityResolution|FullyQualifiedName~DemoCommunitySeed"
```

Expected outcomes:

- Build succeeds with no errors.
- Grade tests prove exact 7-12 ordering, strict new references, preserved legacy reads, new-community initialization, route removal, and idempotent supported data.
- Assignment/roster tests prove atomic replacement, uniqueness, Active/same-tenant validation, paging/filtering/distinct results, assigned classes, and bounded identity loading.
- Existing Student paging/filter/search/status/detail/tenant suites remain green.
- Seed tests prove double gating, deterministic reconciliation, staff password validity, 20 students, assignments, and current counter semantics.

Run the full `dotnet test SprintLabs.sln` only if focused failures indicate cross-project diagnosis is necessary or it is explicitly requested.

## 5. Enable the Development Demo Seed

The checked-in default remains disabled. For a local Development run, set:

```powershell
$env:ASPNETCORE_ENVIRONMENT = "Development"
$env:DemoCommunitySeed__Enabled = "true"
dotnet run --project API
```

The seed must not run if either condition is missing. Re-run the API at least three times and verify record counts remain stable.

Development-only staff credentials:

| Role | Email | Password |
|---|---|---|
| Owner | `owner.demo@sprintlabs.local` | `SprintLabsDemo!2026` |
| Teacher | `teacher1.demo@sprintlabs.local` | `SprintLabsDemo!2026` |
| Teacher | `teacher2.demo@sprintlabs.local` | `SprintLabsDemo!2026` |
| Teacher | `teacher3.demo@sprintlabs.local` | `SprintLabsDemo!2026` |

Never enable or copy these credentials into production configuration.

## 6. End-to-End API Checks

Authenticate through the existing Community email/password login and use its bearer token as `$token`. No call below includes `communityId`.

Fixed Grades:

```powershell
Invoke-RestMethod -Headers @{ Authorization = "Bearer $token" } `
  -Uri "https://localhost:<port>/api/v1/Communities/grades"
```

Expected values: exactly `7, 8, 9, 10, 11, 12` in ascending order.

Paged Teacher roster and filters (Owner token):

```powershell
Invoke-RestMethod -Headers @{ Authorization = "Bearer $token" } `
  -Uri "https://localhost:<port>/api/v1/Communities/teachers?pageNumber=1&pageSize=2"

Invoke-RestMethod -Headers @{ Authorization = "Bearer $token" } `
  -Uri "https://localhost:<port>/api/v1/Communities/teachers?gradeId=<grade7Id>"

Invoke-RestMethod -Headers @{ Authorization = "Bearer $token" } `
  -Uri "https://localhost:<port>/api/v1/Communities/teachers?classId=<class7AId>"
```

Expected demo behavior: Class 7A and Grade 7 return multiple Teachers; Teachers are not duplicated; each item includes its Active Classes.

### Teacher roster performance validation (SC-007)

In a normal Development environment, prepare or reuse a deterministic current-community fixture containing at least 100 Teacher memberships and representative Active class assignments. This fixture is for focused Development verification only; do not expand the production seed or add a benchmarking framework.

Authenticate as that Community's Owner and request one roster page containing up to 100 Teachers with assignment details. Record the machine/runtime, database/provider, fixture size, request parameters, and elapsed time. For example:

```powershell
$rosterMeasurement = Measure-Command {
  Invoke-RestMethod -Headers @{ Authorization = "Bearer $token" } `
    -Uri "https://localhost:<port>/api/v1/Communities/teachers?pageNumber=1&pageSize=100" | Out-Null
}
$rosterMeasurement.TotalMilliseconds
```

Expected result: elapsed time is at most 2,000 milliseconds. Record the observed result in this section during implementation verification or in the T050 completion notes. This is a focused Development measurement, not a CI performance gate; do not add benchmarking packages or a new performance-test framework.

Replace assignments (Owner token):

```powershell
$body = @{ classIds = @(<class7AId>, <class9AId>) } | ConvertTo-Json
Invoke-RestMethod -Method Put -ContentType "application/json" `
  -Headers @{ Authorization = "Bearer $token" } `
  -Body $body `
  -Uri "https://localhost:<port>/api/v1/Communities/teachers/<teacherUserId>/classes"
```

Repeat with `{ "classIds": [] }` to verify clearing. A Teacher token must receive the established forbidden response.

Students:

```powershell
Invoke-RestMethod -Headers @{ Authorization = "Bearer $token" } `
  -Uri "https://localhost:<port>/api/v1/Communities/students?pageNumber=1&pageSize=5&gradeId=<grade7Id>"

Invoke-RestMethod -Headers @{ Authorization = "Bearer $token" } `
  -Uri "https://localhost:<port>/api/v1/Communities/students?pageNumber=1&pageSize=5&classId=<class7AId>"
```

Expected: paged current-community students using existing response conventions and the deterministic demo distribution.

## 7. Security and Compatibility Checks

- A foreign Community Grade/Class/Teacher ID returns no foreign data and changes nothing.
- An unsupported `Value = NULL` Grade cannot be selected for a new/updated Class or StudentLicense.
- Pending/Removed Teachers and inactive Classes cannot receive assignments.
- A Teacher cannot self-assign; assignment management remains Owner-only.
- `GET /api/v1/Communities/{communityId}` still supports its existing Student/backward-compatible behavior.
- Platform Admin routes still use explicit Community IDs.
- The removed staff Grade POST has no controller operation.
