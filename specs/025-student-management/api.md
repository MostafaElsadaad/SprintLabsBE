# Student APIs

Base path: `/api/v1/Communities`. All calls require a staff bearer token. Community and caller IDs come from the authenticated account, never query/body identity fields. Owner can read its community; a teacher can read only assigned active classes. Imports are owner-only. Attendance is deferred.

JSON success responses keep `{data,message,statusCode:200,errorCode:1}`. Files are direct download responses. Use the roster's numeric **license ID** when opening a student, not the user's account ID.

## Existing API audit

| Request | Before | This feature |
| --- | --- | --- |
| GET students | Name/email search existed; only UNKNOWN status accepted; trimmed gameplay row | Enrollment fields and ACTIVE/PENDING/INACTIVE filtering, includes revoked history, actual stored community performance |
| GET students/{id} | Player-profile ID lookup; email/class/grade/license already present; placeholder analytics | License-ID default, explicit playerProfile selector, assigned-class scope, pending/revoked detail, stored analytics |
| GET students/stats | Absent | Added |
| POST students/import | Absent | Added CSV/XLSX row enrollment |
| GET students/export | Absent | Added CSV/XLSX scoped file download |
| Student attendance | Absent | Deferred by user |

## GET /students

Queries: `page` (preferred) or `pageNumber` (default 1), `pageSize` (1–100, default 10), `search` (name/email), `classId`, `gradeId`, `status`, `sort=name:asc|name:desc`. Existing sort parser accepts its prior forms; see original dashboard documentation. Status accepts ACTIVE/PENDING/INACTIVE case-insensitively. Omission includes all enrollment states. UNKNOWN and other unsupported values return 400.

`data` example:

```json
{
  "items": [{
    "id": 42,
    "studentCode": "STU-000042",
    "fullName": "Student Name",
    "email": "student@example.com",
    "class": {"id": 1, "name": "Class 7A"},
    "grade": {"id": 33, "value": 7, "name": "Grade 7"},
    "status": "ACTIVE",
    "licenseStatus": "ACTIVE",
    "joinedAt": "2026-10-10T12:00:00Z",
    "activatedAt": "2026-10-10T12:00:00Z",
    "avgScore": 75,
    "sessionsCount": 1
  }],
  "page": 1, "pageSize": 10, "total": 1
}
```

Status uses enrollment: Active → ACTIVE, Pending → PENDING, Revoked → INACTIVE. `licenseStatus` is ACTIVE/PENDING/REVOKED. Student code derives from license ID. `joinedAt` is enrollment CreatedAt; activation may be null. Pending rows without a profile display their email as fullName. `avgScore` is aggregate correct-answer percentage, not board position; `sessionsCount` is completed community matches. No observations gives numeric zero.

Errors: 401 unauthenticated, 403 invalid/suspended/nonstaff context, 404 class outside caller scope, 400 status/paging/sort validation. Encode email search (`+` → `%2B`).

## GET /students/{studentId}

`studentId` defaults to the roster's license ID. `?idType=playerProfile` explicitly supports the old player ID lookup. Never guess from a numeric ID. Legacy callers must add this query parameter; old URLs without it now select a license. Unsupported idType gives 400. When a player has historical enrollments, the explicit player lookup prefers non-revoked, then most recently joined permitted enrollment.

Returned fields: id, studentCode, userId/playerProfileId (nullable), name/fullName, email, status, licenseStatus, class, grade, classId/className, gradeId/gradeName, joinedAt/createdAt, activatedAt, avatarUrl, gold, experience, level, analytics. Name/progression compatibility fields are retained. Pending or revoked enrollments can be inspected without an active login profile. No profile gives zero progression values and null identity/avatar fields.

`analytics` fields: completedAssignments (0; no assignment integration), matchesPlayed, sessionsCount, averageScore (answer accuracy percentage), winRate (percentage), wins, correctAnswers, wrongAnswers, lastActivityAt. Only persisted Completed matches whose Match and MatchPlayer both belong to the resolved community contribute; global or other-community activity is excluded. No observations gives zero numeric aggregates and null lastActivityAt.

Errors: 401/403 context, 400 invalid ID/idType, 404 unknown/foreign/unassigned student. This closes the old teacher-wide detail lookup; teacher permissions now match the roster.

## GET /students/stats

Queries: classId, gradeId, search. No status filter: the cards partition the same scoped population.

```json
{"data":{"total":20,"active":15,"inactive":2,"pending":3},"message":"Success","statusCode":200,"errorCode":1}
```

Total includes active, pending and revoked enrollment records; active + inactive + pending equals total. These are enrollment counts, not distinct login-account counts. Same 401/403/404 scope and 400 invalid-filter errors as the roster.

## POST /students/import

Owner only. Multipart field `file`; UTF-8 `.csv` or `.xlsx`. Worksheet 1 for XLSX, shared/inline strings supported; `.xls`, macros, external worksheet targets and formula cells unsupported. Header names are case-insensitive and must be unique, with required `email,gradeId,classId`. Extra columns do not modify accounts.

```csv
email,gradeId,classId
student@example.com,33,1
```

Limits: uploaded file 5 MiB (multipart request 6 MiB), 1–1,000 data rows, 64 columns, 4,096 characters/cell; XLSX at most 128 ZIP parts and 20 MiB uncompressed content, row/cell/dimension bounds and DTD prohibition. File-structure validation occurs before any enrollment. Use integer database IDs, not grade values or class names.

Existing AddStudentLicense enrollment is reused: validated grade/class ownership, available seat, normal activation for an eligible existing account, otherwise pending enrollment. It does not create a login/password. Existing non-revoked email enrollments and successful duplicate rows are skipped. A revoked enrollment follows existing reenrollment rules. Row failures do not stop other rows. Add/revoke/admin capacity writes now share a serializable seat transaction; failed additions roll back saved profile/membership/license changes. Existing teacher seat workflows retain their serializable transactions.

```json
{
  "data": {
    "totalRows": 3, "imported": 1, "skipped": 1, "failed": 1,
    "rows": [
      {"rowNumber":2,"email":"student@example.com","outcome":"IMPORTED","licenseId":42,"reasonCode":null,"reason":null},
      {"rowNumber":3,"email":"student@example.com","outcome":"SKIPPED","licenseId":null,"reasonCode":"ALREADY_ENROLLED","reason":"A non-revoked enrollment already exists."},
      {"rowNumber":4,"email":"bad-email","outcome":"FAILED","licenseId":null,"reasonCode":"INVALID_ROW","reason":"Valid email, gradeId and classId are required."}
    ]
  },
  "message":"Success", "statusCode":200, "errorCode":1
}
```

NO_AVAILABLE_SEATS identifies exhausted/unconfigured student capacity. ENROLLMENT_REJECTED covers invalid grade/class or conflicting membership. HTTP 200 means file processing completed; inspect failed rows. Infrastructure/cancellation failures propagate rather than being counted as successful imports. An interrupted file may have earlier committed rows; reimport safely skips them. Errors: 401/403, 400 malformed/unsupported/header/row-bound file, 413 file/request-size limit. Framework multipart/model validation follows existing API behavior.

## GET /students/export

Owner and assigned teachers. `format=csv|xlsx` (default csv), `search`, `status`, `classId`, `gradeId`. Exports all matching scoped rows, not one UI page; above 10,000 rows returns 400, never silently truncates.

CSV Content-Type `text/csv; charset=utf-8`; XLSX `application/vnd.openxmlformats-officedocument.spreadsheetml.sheet`; Content-Disposition filename `students-<UTC timestamp>.<format>`. No JSON envelope on download. Columns: studentCode, fullName, email, status, licenseStatus, classId, className, gradeId, grade, gradeName, joinedAt, activatedAt, avgScore, sessionsCount. Dates are UTC ISO strings. CSV uses BOM/quoting and neutralizes text that can begin spreadsheet formulas; XLSX writes text cells. Files contain no account credentials.

Errors: 401/403/404 scope, 400 format/filter/row cap. Frontend should handle JSON error responses separately from file content.
