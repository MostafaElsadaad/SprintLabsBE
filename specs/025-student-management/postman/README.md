# Run student API scenarios

Import `SprintLabs.Students.postman_collection.json` and `SprintLabs.Students.postman_environment.json`.
Select **SprintLabs Students Local**, set `baseUrl` to your test API, and enter `seedPassword` locally using the existing demo-seed password. The exported environment has no password/token values. Keep the configured seeded owner and Teacher 1 emails unless your test database uses different seeded accounts. Setup checks the expected SprintLabs Demo School and roles before mutations.

Run the **whole collection in order**, one iteration, without a data file. Leave `runXlsxImportTests=false` for the default run. The API must run the updated backend code; no migration is needed for this feature. Enable persisted responses for inspection, turn console logs off, leave Keep variable values off, and allow later cleanup after assertion failures. Do not run simultaneous collections against the same test community. Requires **two free student seats**; the optional XLSX flow needs **four** during the run.

## Automatic default run

37 requests are defined; **33 run by default** and four optional XLSX-upload/cleanup requests are skipped. The collection:

- Logs in seeded owner/teacher, discovers an assigned class and its grade, and checks the roster fields, student details, explicit legacy player ID lookup, search, enrollment filters, statistics and permissions.
- Generates two fresh `example.invalid` enrollment emails from a new UUID every run/iteration. CSV is built as a real multipart upload in the pre-request script, so the default run requires no local upload file.
- Imports two pending enrollments, skips the duplicate CSV row and reports two invalid rows. Reimport skips the now-enrolled emails without duplicates or extra seats. No login accounts or emails are created/sent for these disposable rows.
- Downloads CSV and XLSX as both owner and assigned teacher. Inspect responses, and use **Send and Download** on the export requests when you want to save a file.
- Revokes only license IDs returned for that run's generated emails; repeats revocation to check idempotency; verifies inactive rows and restored active/pending statistics.

Revoked enrollment history remains stored. Final total/inactive statistics therefore increase by the number imported; live active/pending counts return to their baseline. Do not expect the total count to reset to the original seed count. Numeric performance can be nonzero when completed community matches exist. Permission/validation requests intentionally return 400/401/403/404 and pass when the status matches.

## Optional fresh XLSX upload

Postman requires a local file for a binary XLSX upload. Request 04's response supplies the actual `id` (class ID) and `grade.id` for the teacher-assigned class. Use those values to generate the fixture:

```powershell
python prepare_import_files.py --grade-id <grade.id> --class-id <id>
```

The script creates `local-fixtures/students-import.xlsx` and `.csv`, with fresh UUID emails, two valid rows, one duplicate and one invalid email. It uses Python's standard library only. In requests **28 and 29**, choose that same XLSX file for the `file` field. Alternatively copy it to Postman's configured working directory as `students-import.xlsx`. Set `runXlsxImportTests=true` and run the whole collection. Regenerate the fixture before each optional run; using an old fixture produces skipped rows instead of the asserted fresh imports. Do not import the companion CSV too, because it deliberately contains the same emails as the XLSX file.

## Failure and cleanup

Missing credentials, a different seed community, or no assigned teacher class stop setup. Dependent requests guard missing captured IDs. If interrupted after enrollment, inspect requests 17/28 for the created `licenseId` values and revoke those only using `DELETE /api/v1/Communities/student-licenses/{licenseId}`. If capacity is exhausted, the import reports failed rows; free test seats through the existing lifecycle rather than changing seed identities.

Generated state and bearer tokens use run-local variables; no script logs credentials or saves tokens to environment/collection variables. Do not export authentication request/response histories or credential-filled environments. The earlier dashboard collection's exact student-row assertions target its older contract; use this collection for the enriched student API.

## Verification limits

Backend tests cover CSV/XLSX parsing and export, real multipart HTTP requests, import/reimport, scope, stored performance, rollback and concurrent enrollment/revocation in SQLite. Collection scripts and repeated setup/body generation are validated offline. Live Postman/staging HTTP and MySQL concurrency require your test environment and remain unverified by artifact generation. Attendance is deferred; there is no attendance request.

## Import failure correction (2026-10-11)
The earlier demo seeder left no student headroom when its capacity equaled the 20 seeded enrollments. The Development-only enabled demo seed now reserves at least four spare student seats while retaining any higher limit. Stop the running API, rebuild and restart in Development with DemoCommunitySeed:Enabled=true, then re-import this collection. Production capacity enforcement remains intact. Import now reports NO_AVAILABLE_SEATS explicitly. The collection keeps the root import assertion with row reasons and skips dependent requests without extra setup failures when no generated ID exists; any partially created enrollment is still captured and cleaned up. Optional requests 28/29/32/33 are deliberately skipped unless XLSX testing is enabled; Postman may display Request could not be sent for these skips.
