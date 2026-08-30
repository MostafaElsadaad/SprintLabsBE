# Frontend Guide: Fixed Grades, Teacher Classes, and Demo Data

## Shared Tenant Context

Do not add a Community selector or send `communityId` on Grade, Teacher, assignment, or Student staff calls. The API derives it from the signed-in staff user. Treat current-context/forbidden failures as an authorization state, not as a prompt to choose a Community.

## Fixed Grades

Required behavior:

- Load `GET /api/v1/Communities/grades` for Grade dropdowns and filters.
- Display the integer `value` (7-12) and submit its `id` where an existing Class or StudentLicense request requires `gradeId`.
- Order by the returned sequence or numeric `value`.
- Remove Grade create/edit/delete/reorder buttons, forms, calls, and optimistic state.

States:

- Loading: disable Grade-dependent controls until the list arrives.
- Error: show the existing API error; do not construct a local fallback Grade ID set.
- Empty/incomplete: treat as a server data error. A valid response always has exactly six rows.
- Permissions: Owners and Teachers may read; neither role may mutate Grades.

## Teacher Roster

Required page/section:

- Paginated Teacher table visible to Owners only.
- Query controls: Grade, Class, optional search and status.
- Columns: name, email, membership status, created time, assigned Classes.
- Use server `pageNumber`, `pageSize`, `totalRecords`, and `totalPages`; do not load all Teachers and page/filter locally.

Filter behavior:

- Grade options come from fixed Grade API.
- Class options come from the current server-owned Class API.
- `gradeId` finds Teachers assigned to an Active Class in that Grade.
- `classId` finds Teachers assigned to that exact Active Class.
- With both filters, offer only Classes in the selected Grade where practical; still handle the API's safe validation error.
- Reset to page 1 after changing a filter.

States:

- Loading: keep filters stable and show table loading state.
- Empty: distinguish “no Teachers” from “no matches.”
- Error: show the established error without disclosing assumed tenant ownership.
- Permissions: hide the roster and assignment actions from Teachers, Students, and platform-admin-only users without an Owner membership.

## Replace Teacher Class Assignments

Owner action: edit a Teacher's complete class set.

- Initialize the selection from `teacher.classes`.
- Submit `PUT /api/v1/Communities/teachers/{teacherUserId}/classes` with `{ "classIds": [...] }`.
- An empty array intentionally clears all assignments.
- De-duplicate IDs client-side for clean requests; the API also normalizes them.
- Classes are selected by current Class IDs and can span multiple Grades.
- Do not provide a separate Teacher Grade selector; Grade labels are derived from Classes.
- On success, replace the displayed assignment list with the returned `classes` collection.
- On failure, retain the last confirmed UI state because the API applies replacement atomically.

## Student Roster

Keep the existing page and calls:

- `GET /api/v1/Communities/students`
- `GET /api/v1/Communities/students/{playerProfileId}`
- Existing page/page-size, Grade, Class, status, and search controls.
- Existing loading, empty, error, and Owner/Teacher visibility rules.

Use fixed Grade IDs in new filters and new StudentLicense forms. Do not redesign Student placement or add StudentGrade state. Existing legacy-linked records may still render a retained legacy Grade label while operators remediate data.

## Development Demo Environment

The backend must be running with:

```json
{
  "DemoCommunitySeed": {
    "Enabled": true
  }
}
```

and the ASP.NET Core environment must be `Development`. Both conditions are mandatory. Production/default configuration remains disabled.

Development-only staff credentials:

| Role | Email | Password |
|---|---|---|
| Owner | `owner.demo@sprintlabs.local` | `SprintLabsDemo!2026` |
| Teacher | `teacher1.demo@sprintlabs.local` | `SprintLabsDemo!2026` |
| Teacher | `teacher2.demo@sprintlabs.local` | `SprintLabsDemo!2026` |
| Teacher | `teacher3.demo@sprintlabs.local` | `SprintLabsDemo!2026` |

These credentials are **DEVELOPMENT ONLY**. Use the existing Community email/password login. The demo exposes overlapping Teacher assignments and 20 students for Grade/Class filters and pagination. Do not copy these credentials or enablement into production configuration.

## Migration from Mutable Grades

| Previous frontend behavior | New behavior |
|---|---|
| POST a custom Grade | Removed; load fixed Grades 7-12. |
| Display Grade `name`/`sortOrder` from Grade API | Display integer `value`. |
| Local unpaged Teacher roster | Consume paged response and query filters. |
| No Teacher class editor | Owner replaces the full set through the PUT endpoint. |
| Send/restore `communityId` | Never send it on staff routes; server owns Community context. |

Open questions: none for the approved feature scope.
