# Frontend student mappings

Use the current Communities endpoints; do not add a second dashboard/student API family. Keep response.data.items for the roster and use returned numeric license `id` for detail navigation. Legacy player-ID URLs require `idType=playerProfile`. These are API compatibility changes; frontend implementation is outside this backend task.

Roster columns: studentCode, fullName/email, class.name, grade.value/name, status, joinedAt, avgScore, sessionsCount. ACTIVE/PENDING/INACTIVE are enrollment labels; INACTIVE corresponds to REVOKED licenseStatus. Do not describe a student's enrollment status as recent gameplay attendance. Pending fullName may be their email until their profile exists. Activation/date/avatar/linked identity can be absent.

Search sends name/email text with URL encoding. Status filter sends ACTIVE, PENDING or INACTIVE, or omits status for all. Keep class/grade filters and page/pageSize. Loading disables repeated fetch actions; an empty page is a valid result. Invalid filters are 400; 403 hides unauthorized actions; 404 scoped resources should not disclose another class/community.

Statistics cards call GET students/stats with the same search/class/grade filters. Render total, active, inactive and pending separately. Counts are enrollment records, including retained revoked history, not unique login accounts. Revoking restores seat availability but does not reduce the historical total count.

Student details call GET students/{list.id}. Show email/class/grade/license status and enrollment/activation dates. Stored completed community performance uses answer accuracy percentage, match count, wins/win percentage, answer counts and last activity. Numeric zero with no observations means no recorded completed community activity; null lastActivityAt is displayed as unavailable. Progression values are zero for an enrollment without a linked profile. completedAssignments is not integrated.

Import is owner-only: choose CSV/XLSX, multipart field file, display required email/gradeId/classId columns and limits. Use actual class/grade option IDs. Show per-row imported/skipped/failed results and safe errors even after HTTP 200. Refresh roster/statistics on completion; retries skip existing non-revoked enrollments. Do not create login accounts/password fields in the import form. A file failure before processing causes no enrollment; cancellation after earlier committed rows can be retried.

Export is visible to owner/assigned teacher and calls GET students/export with current filters and csv/xlsx format. Treat the success as a binary/text blob and use Content-Disposition for the filename; parse JSON errors instead of saving them as spreadsheet files. Export contains all matching permitted rows up to the limit, independent of page. Large-result 400 should prompt narrower filters. CSV formula protection may add a leading apostrophe to dangerous text when opened in Excel.

Attendance is deferred. No attendance UI/history API is supplied. Global matches, other schools' data, teacher-recorded attendance, account email editing and unrelated match/question flows are excluded.

Testing artifacts and run instructions are in postman/. Default run creates unique emails, exercises CSV uploads automatically, downloads both export formats and revokes only its new licenses. Optional XLSX import requires a prepared local file.
