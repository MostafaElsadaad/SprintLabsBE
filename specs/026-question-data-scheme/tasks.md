# Implementation tasks

- [x] T001: Relational entities, explicit shared UUID binary mapping, FKs, constraints and indexes; SQLite schema check.
- [x] T002: Immutable/idempotent publication, six type validation and paged question reads.
- [x] T003: Atomic typed history, roster/reference validation, own/admin/server reads, derived correctness and known-ID replay.
- [x] T004: CQRS/controller/DI integration; HTTP auth, private-history and display-redaction tests.
- [x] T005: Independent review, docs, migration/SQL checks, full tests and living handoff complete.

No live database migration, deployment, educational-tool UI, Unity integration or diagnostics delivery is part of this task. Source rollout instructions live in api.md.

- [x] T006: Seconds-based fields, safe timing source migration, dummy legacy import/export and compatible GET adapter; tests and independent review complete.

- [x] T007: Compatible optional count with database metadata filtering and unchanged Unity payload; bounds/empty/import HTTP regressions.
- [x] T008: Import staging's four legacy packs through the existing service; verify 97 rows, retained packs, duplicate-free retries and matching shared IDs.
- [x] T009: Collapse grading-equivalent legacy fill variants, retaining UUID derivation; regression test.
- [ ] T010: Publish and deploy the user-authorised fixes, then verify count=10, no-count/assignment compatibility and HTTP import replay on staging.
- [x] T010a: PR #38 deployed successfully; count=10 and eight grade/assignment comparisons verified. Live authenticated HTTP replay lacked credentials; service replay and integration tests covered idempotency.
- [ ] T011 (2026-10-16): Make GET relational-only, reject assignment by explicit choice, verify six types with legacy table physically absent and legacy-only 404, publish/deploy and compare current grade-only game responses.
