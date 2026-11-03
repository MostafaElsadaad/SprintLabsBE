# Teacher reporting API additions

Base paths and envelopes are unchanged. No new routes were added. All routes require the existing bearer authentication, current-community resolution and role checks. Existing student/search/statistics/history/game-result endpoints remain in place.

## Current term configuration

GET `/api/v1/Communities/me` returns an additive `data.term` field (null if unconfigured). PATCH `/api/v1/Communities/me` retains its existing owner-only name/slug fields and accepts optional `term`:

```json
{
  "name": "Your School",
  "slug": "your-school",
  "term": {
    "name": "Term 1 2026/2027",
    "startDate": "2026-09-01",
    "endDate": "2027-01-31"
  }
}
```

Example success: `{ "data": { "id": 1, "name": "Your School", "slug": "your-school", "status": "Active", "term": { "name": "Term 1 2026/2027", "startDate": "2026-09-01", "endDate": "2027-01-31", "progressDefinition": "..." } }, "message": "Success", "statusCode": 200, "errorCode": 1 }`.

Dates are ISO calendar dates, with endDate **inclusive**. All three values must be supplied together. Name is trimmed and 1–100 characters; startDate is at least 1000-01-01, endDate is at least startDate and before 9999-12-31. Same-day terms are valid. Missing/blank/overlong names, partial dates, invalid dates and reversed ranges return 400. Empty `term:{}` clears the stored term. Omitting term, or sending term:null, leaves it unchanged. Name/slug remain required by the existing PATCH contract, and ordinary profile edits preserve dates.

Only an active, nonsuspended school owner can change the term. Teachers cannot PATCH it (403). GET retains existing community-access rules. Unauthenticated calls return 401; unknown/scoped resources and invalid access retain existing 404/403 behavior. No client-supplied community/user ID can override the current staff context.

## Class term progress

GET `/api/v1/CommunityDashboard/dashboard` (teacher response): each entry in `data.classes` now includes `termProgress` and `term`. GET `/api/v1/Communities/classes` adds them to each existing paged item. GET `/api/v1/Communities/classes/{classId}` includes them in owner and teacher detail responses. Existing class create/update responses also reuse the detail projection.

```json
{
  "id": 1,
  "name": "Class 7A",
  "grade": 7,
  "studentsCount": 4,
  "nextSession": null,
  "termProgress": 41.53,
  "term": {
    "name": "Term 1 2026/2027",
    "startDate": "2026-09-01",
    "endDate": "2027-01-31",
    "progressDefinition": "Elapsed UTC calendar time from startDate midnight through the midnight after inclusive endDate, clamped to 0-100% and rounded to two decimals. This is not curriculum completion."
  }
}
```

Percentage formula: `round(clamp(100 * (utcNow - startDate midnight UTC) / (endDate + 1 day midnight UTC - startDate midnight UTC), 0, 100), 2)`. A two-day term is 25% after its first 12 hours, 50% at the second day's midnight, and 100% at the midnight following the final day. Values are zero before start and 100 after completion. Missing/invalid legacy configuration produces `termProgress:null, term:null`. This term applies school-wide; no per-class curriculum completion is inferred.

Class/student visibility is unchanged: teachers see assigned active classes only, and foreign/unassigned class detail remains 404. Admin dashboard shape is unchanged because its existing payload does not expose class rows. Class list pagination, versioning, ID types and existing error responses are unchanged.

## Average-score period

GET `/api/v1/Communities/students` includes `scorePeriod` beside each `avgScore`. GET `/api/v1/Communities/students/{studentId}` includes it in `analytics` beside `averageScore`:

```json
{
  "type": "ALL_TIME",
  "name": "All recorded activity",
  "startDate": null,
  "endDate": null,
  "definition": "Correct answers / (correct + wrong answers) * 100 across all persisted completed matches for this player and community. No term or academic-year date filter; no answers yields zero."
}
```

The null bounds mean an intentionally unbounded aggregate, not an unknown term. Existing scores and their calculation are unchanged: completed community matches, including activity outside the configured current term, contribute. Do not use the current term's name/dates as this score's subtitle. Existing student identifier, search, filters, stats, import/export/history/result behavior is not replaced or extended by a new API.

Class detail's existing `avgScore` remains its deferred zero placeholder; its `scorePeriod` uses `type:UNAVAILABLE`, `name:Not available`, null bounds, and a definition explaining that no class aggregate is available. It must not be presented as an observed zero or as term performance. This feature does not invent class scores or game history.

## Migration and seed

`20261103180226_CommunityTermReporting` adds nullable TermName/TermStartDate/TermEndDate to Communities with length/date constraints. Existing schools remain null. Apply through the existing migration process before serving the new queries.

When the existing Development-only `DemoCommunitySeed:Enabled` startup seeder runs, it fills the demo school's term only if all three fields are null. The term is explicitly named `Demo term <year> Q<quarter> (sample calendar)` and spans the UTC quarter containing the seed date. Reruns preserve real/custom settings and do not roll an expired stored term forward. Sample scores, match results, progression and production school dates are never fabricated. Seed code is implemented/tested; no live database migration, seed or deployment was executed by this task.
