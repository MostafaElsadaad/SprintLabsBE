# Curriculum journeys (local, not deployed)

GET `/api/v1/Question/journeys` returns the usual envelope with data as an array of
`{ curriculum, grade, subject, unit }`. It exposes metadata only, distinct and sorted.
Use profile curriculum + grade to populate subjects, then subject to populate units.
GET `/api/v1/Question?curriculum=...&grade=10&subject=Physics&unit=1`
returns the existing legacy question payload. URI-encode text query values.

POST `/api/v1/admin/development-players/journey-demo` has no request body. It requires
an active platform-admin bearer and the configured development-authentication header,
and remains unavailable when the existing development guard disallows the environment.
No credential belongs in this document. Use the existing approved local operator setup.

It verifies all eight catalogue development accounts before writing, publishes twelve fixed
UUID MCQs (three per Physics/Chemistry unit1/2), and sets only their personal education to
SprintLab Test High School, grade10, English. School/class membership IDs remain unchanged;
these demo membership grade IDs are separate from the question-bank grade10 value.
No rewards, entitlements, names or identity fields change. Repeating safely repairs a partial
interrupted run. After deployment and invocation, sign out/in to refresh Unity profile data.

Manual checks: change subject and ensure units update; public players with different subjects
must not share a match; identical selections can match; private guests use the host selection;
cancel while catalogue loading and disconnect/reconnect must not enqueue a stale request.
Concurrent matches must fetch their own selection and preserve exact scene isolation.
