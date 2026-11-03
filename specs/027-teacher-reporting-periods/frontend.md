# Frontend mapping

Teacher dashboard class cards consume existing GET CommunityDashboard/dashboard data.classes. Add a calendar progress bar from termProgress, with term.name and ISO startDate/endDate underneath. The value is elapsed term time, not learning/curriculum completion. Render 0% and 100% normally; for null progress/term show an unavailable state rather than a zero-progress bar. Use the backend percentage/definition, including its UTC inclusive-end convention; do not independently calculate local-time progress.

Class list and existing class details expose the same fields. Keep existing role-specific fields and pagination: teacher card grade remains numeric; owner/list grade objects stay unchanged. Teacher visibility remains assigned active classes, with current 401/403/404 handling. Do not add scheduling/start/join flows.

If the school-profile form manages term settings, only the owner sees editing controls. Reuse GET/PATCH Communities/me, retain required name/slug in the PATCH, and send a term object with name/startDate/endDate together. Display validation errors for blank/overlong names or invalid/reversed ranges. Send term:{} to explicitly clear; omission/null preserves current configuration. Show loading/saving/error states using existing form conventions. Teacher forms remain read-only for term settings. No frontend production changes were made here.

Student score subtitles use scorePeriod.name/type. ALL_TIME is labeled All recorded activity, with null unbounded dates; never subtitle it with the configured term. Average score remains answer accuracy across completed community matches. No extra request or term filter is needed. Class detail's placeholder score has UNAVAILABLE metadata: display Not available instead of interpreting zero as measured performance.

Use scorePeriod.definition and term.progressDefinition for explanatory text/tooltips. No enrollment, student, match-history/result or search integration is a new feature here. Preserve their existing API mappings. No fake term dates should be invented client-side; the only automatic sample configuration belongs to the explicitly labeled Development demo school.

Verification: owner sets/clears a term through the existing profile endpoint, then teacher reloads dashboard/class detail and confirms consistent term fields. Check ALL_TIME beside real student scores and UNAVAILABLE beside class placeholders. Existing strict JSON key assertions must accept the documented additive fields; older Postman collections with exact-key checks target their original contracts.
