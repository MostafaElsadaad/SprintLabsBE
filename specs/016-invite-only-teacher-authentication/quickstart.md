# Quickstart: Invite-Only Teacher Authentication

This is an implementation and verification guide for the plan; it does not authorize production data changes.

## Prerequisites

- .NET 8 SDK.
- Existing SprintLabs development configuration for MySQL, JWT, Email, Frontend, and Identity.
- A dedicated disposable MySQL test database only for opt-in integration/concurrency tests. Do not point destructive fixtures at a shared or production database.

## Implementation sequence

1. Add shared error/result/option changes and the nullable user cooldown field/index configuration.
2. Evolve the existing identity and invitation interfaces/implementations; do not introduce parallel services.
3. Modify owner invitation, then implement anonymous validation and completion.
4. Tighten login before token issuance and modify forgot/reset password.
5. Remove registration/confirmation/resend/authenticated-accept routes and dead artifacts.
6. Generate exactly one migration and inspect it for non-destructive scope.
7. Update Swagger annotations and automated tests.

## Build and baseline tests

```powershell
dotnet restore SprintLabs.sln
dotnet build SprintLabs.sln --no-restore
dotnet test SprintLabs.sln --no-build --nologo
```

Baseline observed during planning:

```text
Passed: 227
Failed: 0
Skipped: 0
```

Existing compiler/test warnings are not part of this feature unless a touched file introduces a new warning.

## Migration verification

Generate one migration using the repository's existing startup/project pattern. Before applying it, inspect both migration directions and the model snapshot.

Expected `Up` scope:

- Nullable `Users.LastPasswordResetEmailSentAt`.
- Missing non-unique `CommunityUsers(UserId, Role, Status, CommunityId)` lookup index.
- Missing non-unique `TeacherInvitations(CommunityUserId, AcceptedAt, RevokedAt, ExpiresAt)` lookup index.

Reject/regenerate if it deletes or rewrites data, replaces a table, changes unrelated columns, adds a duplicate refresh/invitation table, changes existing error/token values, or adds a broad unique relationship constraint.

## Focused smoke verification

Use Swagger or an API client against a local environment with email redirected to a safe test inbox.

1. Confirm registration, confirm-email, resend-confirmation, and authenticated accept routes are absent from Swagger and return no mapped endpoint.
2. As an Active Owner, invite a new email. Confirm one teacher user, one Pending Teacher membership, one seat increment, and one hashed invitation record; confirm the email link uses `/invitations/teacher/setup`.
3. Validate the raw token. Confirm only community name, masked email, and expiration return and that membership remains Pending.
4. Complete using token/name/password. Confirm email, password, name, and the existing membership become active; confirm no new membership/profile/license/token and no counter increment.
5. Complete again. Confirm a safe invalid-invitation response and no data change.
6. Login by email, then by username. Confirm existing token format and one community. Confirm pending/no-community/multiple-community teachers receive the same generic failure and no tokens.
7. Resend from the same community. Confirm the membership/counter are unchanged and only the newest token validates.
8. Invite the same target from another community. Confirm HTTP 409 `TeacherAlreadyBelongsToAnotherCommunity` and no mutation.
9. Request password reset with a known and unknown identifier. Confirm both receive identical HTTP 202 responses. Reset with a valid link and confirm old refresh tokens are revoked.
10. Execute existing Google, Firebase/player, refresh, and logout regression tests without changing their expectations.

## Concurrency verification

Run against the dedicated real MySQL test database; EF Core InMemory cannot prove row locks, unique indexes, isolation, or transaction semantics.

- Submit two different-community invitations for the same normalized email concurrently: one reservation succeeds and the other returns the stable 409.
- Submit same-community resend concurrently: one membership and seat reservation remain; only one final usable invitation exists.
- Complete one invitation concurrently: one request activates/accepts; the other receives the generic invalid-invitation error; the counter remains unchanged.
- Invite different emails at remaining capacity concurrently: locked license accounting prevents `UsedTeachers` from exceeding capacity.

## Security inspection

Search application logs and persisted rows from the smoke run. Raw invitation, Identity reset, access, refresh, and password values must not appear. Invitation rows contain only SHA-256 hashes. All feature timestamps and comparisons use UTC. Authorization and community/role decisions come only from backend claims and persisted relationships.

## Completion criteria

- `dotnet build SprintLabs.sln` succeeds.
- Full automated suite succeeds, including unchanged Google tests.
- Opt-in MySQL concurrency cases succeed when a test database is configured.
- Swagger and feature docs match the contract.
- The migration is single, non-destructive, and limited to the documented field/index changes.

## Implementation verification record (2026-08-02)

- Targeted whitespace formatting completed for the feature files. The repository-wide
  `dotnet format --verify-no-changes` check still reports pre-existing whitespace and
  line-ending differences outside this feature.
- `dotnet build SprintLabs.sln --no-restore --nologo` succeeded with zero errors. It
  reports eight pre-existing compiler warnings in unrelated repository files; no
  warnings were introduced by the feature files.
- `dotnet test SprintLabs.sln --no-build --nologo` succeeded: 247 passed, 0 failed,
  0 skipped.
- The invite-only focused tests, Google compatibility tests, Firebase/player tests, and
  refresh/logout regression tests passed. Google endpoint, handler, service, claims,
  and test files have no behavioral changes in this feature diff.
- Controller and reflection contract coverage confirms Swagger discovers the invite,
  anonymous validate/complete, identifier login, generic forgot-password, and user-id
  reset-password actions; retired register/confirm/resend/accept actions are absent.
  A live Swagger HTTP smoke run remains pending a permitted local API launch.
- The migration `20260802001536_InviteOnlyTeacherAuthentication` was inspected and is
  non-destructive: it adds the nullable reset-email cooldown timestamp and two lookup
  indexes only. It has not been applied because `SPRINTLABS_MYSQL_TEST_CONNECTION` is
  not configured. Consequently, the real-MySQL concurrency and persistence smoke
  cases remain pending.
