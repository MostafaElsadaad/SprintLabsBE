# Staging dashboard fixes — API contract

Implemented locally on 2026-11-07; not deployed by this task. No new endpoints,
packages, seed records or database migrations.

## Optional teacher filter

The existing endpoints accept an optional positive Int64 `teacherId` query parameter:

- `GET /api/v1/Communities/students`
- `GET /api/v1/Communities/students/stats`
- `GET /api/v1/Communities/students/export?format=csv|xlsx`

It intersects the caller's authorized classes with classes assigned to that teacher.
It composes with the existing search, grade, class and enrollment-status filters.
Filtering precedes pagination, statistics and export. Multiple teachers per class
remain supported. A foreign or unassigned teacher returns no matching rows; the
filter does not grant access to another tenant or another teacher's classes.
Zero/negative IDs return the existing 400 response. Responses remain unchanged.

Example: `/api/v1/Communities/students?teacherId=11&status=ACTIVE&page=1&pageSize=10`.

## Email links

New owner and teacher setup emails use `{Frontend:BaseUrl}/auth/register?token=...`.
Password reset uses `{Frontend:BaseUrl}/auth/reset-password?userId=...&token=...`.
The existing token encoding and validation remain unchanged; the configured base
path is retained. Staging deployment explicitly sets `Frontend__BaseUrl` to
`https://dashboard.noxed.tech`. Existing email contents are not rewritten.

## Validation

Release suite: 792 passed, 15 opt-in MySQL checks skipped, zero failed.
Focused cases cover prefixed URLs/token encoding and teacher filtering before
paging/statistics, including foreign-tenant filtering. Skipped tests do not establish
live MySQL concurrency correctness. Real delivered email clicks require deployment
of both repositories followed by issuing fresh setup/reset links.
