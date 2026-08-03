# Implementation Plan: Invite-Only Teacher Authentication

**Branch**: Current working branch (no feature branch was created by setup) | **Date**: 2026-08-02 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `/specs/016-invite-only-teacher-authentication/spec.md`

## Summary

Replace public password registration and email confirmation with an invitation-only lifecycle by modifying the existing teacher authentication and teacher invitation slices. The existing Identity, JWT, refresh-token, SMTP, CQRS, repository, and `BaseResponse` stacks remain authoritative. Password login will accept email or username and issue tokens only after the teacher is proven eligible and has exactly one active teacher/community relationship. The owner invitation flow will reserve one community relationship, validation and completion will be anonymous and token-hash based, password recovery will remain enumeration-safe, and Google authentication will receive no behavioural or contract changes.

## Technical Context

**Language/Version**: C# / .NET 8

**Primary Dependencies**: ASP.NET Core Web API and Identity, MediatR, EF Core 8 with Pomelo MySQL, JWT bearer authentication, Swashbuckle, SMTP email infrastructure

**Storage**: Existing MySQL schema through `ApplicationDbContext`; ASP.NET Core Identity users; `CommunityUser`, `TeacherInvitation`, `RefreshToken`, `Community`, and `CommunityLicense` tables

**Testing**: xUnit, Moq, FluentAssertions, EF Core InMemory for focused tests, and opt-in MySQL integration tests for constraints and concurrency

**Target Platform**: ASP.NET Core backend deployed as a web API

**Project Type**: Layered web service (`API`, `Application`, `Domain`, `Infrastructure`, `Shared`)

**Performance Goals**: Preserve current authentication latency; use indexed normalized-user, membership, invitation, and token lookups; avoid extra token generation or profile creation during login

**Constraints**: One coordinated backend feature; no parallel auth stack; no destructive data rewrite; preserve refresh/logout and Google behaviour; keep Identity lockout/password/token providers; never persist or log raw secrets; enforce one active-or-pending teacher community relationship under concurrency

**Scale/Scope**: Existing SprintLabs teacher population and communities; changes are limited to teacher password authentication, teacher invitations, password recovery, related persistence/indexes, Swagger, documentation, and automated tests

## Constitution Check

### Pre-design gate

| Gate | Result | Evidence |
|---|---|---|
| Existing architecture and layering are preserved | PASS | API remains thin; CQRS stays in Application; service interfaces remain in Domain; implementations and DI remain in Infrastructure; shared cross-layer results remain in Shared. |
| Existing abstractions are reused | PASS | Extend `ITeacherIdentityService`, `ITeacherInvitationService`, `IAccessTokenService`, `IRefreshTokenService`, `IEmailService`, and `IBaseRepository<T>` usage rather than introducing another stack. |
| Security and tenant/community isolation are backend-enforced | PASS | Owner authorization and active community status are rechecked transactionally; invitation completion and login derive membership from persisted data. |
| Persistence changes are minimal and non-destructive | PASS | One nullable cooldown field and lookup indexes are planned; no table replacement, user rewrite, or unsafe global uniqueness constraint. |
| Testing is proportional to risk | PASS | Unit/contract coverage plus real-MySQL concurrency checks are planned, with unchanged Google suites retained as regression guards. |
| API changes include frontend-facing documentation | PASS | This plan produces `api.md`, `frontend.md`, and a detailed API contract. |

### Post-design gate

PASS. Phase 1 design introduces no new project reference, package, feature-specific repository, generic framework, duplicate authentication service, or destructive migration. Transaction-sensitive behavior remains inside the existing Infrastructure services because Infrastructure already owns Identity and EF Core. No constitution violation requires an exception.

## Current Implementation Map

| Concern | Current implementation | Planned disposition |
|---|---|---|
| Public registration / confirmation | `AccountController` plus `RegisterTeacher`, `ConfirmEmail`, and `ResendConfirmation` CQRS slices; `ITeacherIdentityService` and `TeacherIdentityService` methods | Remove public actions and now-unused application artifacts; remove only obsolete service/email methods. Keep Identity token-provider configuration because password reset depends on it. |
| Teacher password login | `TeacherAuthentication/LoginTeacher`; `TeacherIdentityService.AuthenticateAsync`; access and refresh token services | Change `Email` to `Identifier`; resolve normalized email or username; preserve lockout; enforce exactly one active Teacher membership in an active community before token issuance; return one community. |
| Forgot/reset password | Existing `ForgotPassword` and `ResetPassword` slices; Identity reset tokens; SMTP; refresh-token revocation | Modify in place for identifier/userId contracts, generic 202 response, eligibility and cooldown; preserve Identity reset and transactional refresh revocation. |
| Owner invitation | `CommunitiesController`; `InviteTeacher` slice; `TeacherInvitationService.IssueAsync` | Keep the same endpoint and service; remove owner-supplied teacher name; enforce one-community reservation transactionally; resend by revoking/reissuing the token without duplicating membership. |
| Invitation acceptance | Authorized `CommunityInvitationsController.Accept`; `AcceptInvitation` slice; `TeacherInvitationService.AcceptAsync` | Remove authenticated acceptance and replace it in the same controller/service with anonymous validate and complete slices. |
| Membership and invitation persistence | `CommunityUser`/`TeacherInvitation` mappings in `ApplicationDbContext`; existing unique normalized email and token-hash indexes | Preserve tables/data; add lookup indexes and row-lock/transaction rules. Do not add a global uniqueness constraint that breaks legacy or non-teacher relationships. |
| JWT / refresh / logout | `AccessTokenService`, `RefreshTokenService`, repository, existing handlers | Reuse unchanged token format and rotation/revocation logic; ensure login performs all eligibility checks before calling token issuance. |
| Google and player authentication | Existing Google handlers/services and player login workflow | Do not alter endpoints, DTOs, claims, validation, linking, or behavior. Only compile-safe interface call changes are permitted if unavoidable. |
| Identity and email | `API/Program.cs`, `Infrastructure/ServiceConfig.cs`, `SmtpEmailService`, options | Reuse password/lockout/token-provider, Email, Frontend, JWT, and refresh configuration. Change only invitation/reset link construction and obsolete confirmation surface. |

## Project Structure

### Documentation (this feature)

```text
specs/016-invite-only-teacher-authentication/
|-- plan.md
|-- research.md
|-- data-model.md
|-- quickstart.md
|-- api.md
|-- frontend.md
|-- contracts/
|   `-- invite-only-teacher-authentication-api.md
|-- checklists/
|   `-- requirements.md
`-- tasks.md                         # Created later by /speckit-tasks
```

### Source Code (repository root)

```text
API/
`-- Controllers/
    |-- AccountController.cs                         # modify/remove old actions
    |-- CommunitiesController.cs                     # retain owner invite route
    `-- CommunityInvitationsController.cs            # replace accept with validate/complete

Application/Features/
|-- Accounts/TeacherAuthentication/
|   |-- RegisterTeacher/                             # remove
|   |-- ConfirmEmail/                                # remove
|   |-- ResendConfirmation/                          # remove
|   |-- LoginTeacher/                                # modify identifier/eligibility/response
|   |-- ForgotPassword/                              # modify identifier/generic response
|   `-- ResetPassword/                               # modify userId contract
|-- Communities/Teachers/InviteTeacher/              # modify existing invitation slice
`-- CommunityInvitations/
    |-- AcceptInvitation/                            # remove
    |-- ValidateTeacherInvitation/                   # add query vertical slice
    `-- CompleteTeacherInvitation/                   # add command vertical slice

Domain/
|-- Models/TeacherInvitation.cs                      # modify only if audit fields are missing
`-- Services/
    |-- ITeacherIdentityService.cs                   # evolve existing interface
    `-- ITeacherInvitationService.cs                 # evolve existing interface

Infrastructure/
|-- DataAccess/
|   |-- User.cs                                      # add nullable reset cooldown timestamp
|   `-- ApplicationDbContext.cs                      # add lookup indexes/configuration
|-- Services/
|   |-- TeacherIdentityService.cs                    # modify authentication/recovery
|   |-- TeacherInvitationService.cs                  # modify issue/validate/complete
|   |-- SmtpEmailService.cs                          # update frontend links/templates
|   |-- AccessTokenService.cs                        # reuse without second implementation
|   `-- RefreshTokenService.cs                       # reuse rotation/revocation
|-- Migrations/                                      # one non-destructive migration
`-- ServiceConfig.cs                                 # update registration only if signatures require it

Shared/
|-- ErrorCode.cs                                     # add stable feature error codes
|-- Options/TeacherAuthenticationOptions.cs          # add reset cooldown option
`-- Responses/                                       # shared invitation/identity service results

*Tests/
|-- TeacherAuthentication/
|-- OwnerTeacherManagement/
|-- CommunityInvitations/
|-- B2CPlayerProfileSupport/
|-- StudentLicenseActivation/
`-- Integration/                                     # opt-in MySQL transaction tests
```

**Structure Decision**: Use the repository's existing layered projects and vertical-slice folders. New validate and complete actions each receive their own command/query, handler, request, and response/result files. Normal CRUD and lookups continue through `IBaseRepository<T>.AsQueryable()`; transaction and row-lock operations stay in the existing Infrastructure services that already coordinate Identity and EF Core.

## Implementation Strategy

### 1. Retire the public registration surface

Remove the `teachers/register`, `confirm-email`, and `resend-confirmation` actions from `AccountController` so they disappear from routing and Swagger. Delete the isolated CQRS requests, commands/queries, handlers, validators, tests, and service/email methods only after reference checks prove they are unused. Do not delete existing users, confirmation timestamps, email infrastructure, Identity default/custom token providers, password rules, or lockout configuration.

### 2. Modify owner invitation in place

Keep `POST /api/v1/Communities/{communityId}/teachers/invite`. The request contains email only. The handler continues to establish caller context and the existing service performs the authoritative transaction:

1. Recheck and lock the caller's active Owner membership and the active Community.
2. Normalize the invited email and lock/reuse the unique user row; create a teacher user only if none exists and account-type eligibility permits it.
3. Query all Active or Pending Teacher `CommunityUser` rows for that user.
4. Return HTTP 409 with `TeacherAlreadyBelongsToAnotherCommunity` if another community holds the active/pending relationship; make no changes.
5. For the same community, reuse the existing pending membership and its seat reservation. Revoke prior usable invitations and issue one replacement. If already active, do not duplicate membership or counters.
6. For a new/restored pending membership, lock and verify `CommunityLicense`, reserve capacity once, set Teacher/Pending, and increment `UsedTeachers` once.
7. Generate a cryptographically random raw token, store only its SHA-256 hash, expiration, creator/sender, and reissue metadata, then commit.
8. Send the raw frontend setup link after commit. SMTP is never part of the database transaction and raw tokens are never logged.

Use transaction isolation plus row locks for the user, membership, invitation, and license decisions. On a uniqueness/deadlock race, retry/requery once and map the final state to same-community resend or the stable cross-community conflict rather than returning an opaque duplicate error.

### 3. Replace authenticated acceptance with anonymous validation and completion

Give `CommunityInvitationsController` an explicit kebab-case versioned route and expose:

- `GET /api/v1/community-invitations/teacher/validate?token={rawToken}` with `[AllowAnonymous]`.
- `POST /api/v1/community-invitations/teacher/complete` with `[AllowAnonymous]`.

Validation is read-only: hash the token, load the invitation/membership/community without tracking, require unexpired/unrevoked/unused invitation plus a Pending Teacher membership in an active community, and return only community name, masked email, and expiration.

Completion executes once within a transaction. Lock the invitation, membership, and user; repeat all token/membership/community and one-community checks; validate a trimmed name and configured Identity password policy; set name and a deterministic email username only when missing; set teacher status and `EmailConfirmed`; use `UserManager.AddPasswordAsync` or Identity reset APIs rather than direct hash manipulation; activate the existing membership; mark the invitation accepted; revoke refresh tokens if an existing password was replaced; and commit. It creates no `CommunityUser`, counter increment, `PlayerProfile`, `StudentLicense`, access token, or refresh token. Replay/concurrent losers receive the same safe invalid-invitation response.

### 4. Tighten teacher password login before token issuance

Change the existing request/command from `Email` to `Identifier`. Resolve by normalized email or normalized username while avoiding ambiguity and enumeration. Require teacher account, completed invitation/password setup, confirmed email, active/non-suspended status, and non-lockout. Preserve `CheckPasswordSignInAsync(..., lockoutOnFailure: true)` and the current lockout policy.

After credentials succeed but before generating any tokens, load current Teacher relationships. Require exactly one Active Teacher membership whose community is active and no Pending Teacher relationship. Zero, pending-only, removed, expired/revoked, or multiple current memberships fail with the same generic credential response; inconsistent multiple relationships are warning-logged using internal IDs only. Return the existing access/refresh token representations plus a singular `{ id, name }` community. Never create player/student records or a community-selection flow.

### 5. Modify password recovery in place

Forgot password accepts `identifier`, always returns HTTP 202 with the fixed generic message, and only dispatches for an eligible completed teacher account with password, confirmed email, and exactly one active community. Resolve identifier safely and atomically claim a persisted resend cooldown timestamp with a conditional update. Generate the existing Identity password-reset token, URL-safe encode it, and send a frontend link containing `userId` and `token`; neither raw token nor account existence is logged or returned. Mail failure remains indistinguishable to the caller and is safely logged.

Reset password accepts `userId`, URL-safe token, and new password. Decode and validate through ASP.NET Core Identity, preserve configured password policy, use one safe invalid-or-expired error, revoke all active refresh tokens in the existing transaction after success, and do not log in or mutate community membership.

### 6. Preserve token, logout, Google, and player boundaries

Reuse the existing access-token and refresh-token services and response format. Do not change refresh rotation or logout. Do not edit Google endpoints, DTOs, commands, handlers, claims, validation, account linking, or tests. Firebase/player authentication is also outside scope; its existing pending-membership behavior is not refactored by this feature. Only a minimal compile-safe shared-interface call adjustment is allowed, with unchanged behavior proven by regression tests.

### 7. Configuration and migration

Reuse `Frontend.BaseUrl`, Email/SMTP, JWT/refresh token, and Identity password/lockout/token-provider sections. Add only a password-reset resend cooldown option to the existing teacher-authentication configuration. Invitation and reset links are built from `Frontend.BaseUrl`; no URL is hardcoded.

Create one non-destructive migration because persisted cooldown enforcement and efficient current-relationship lookups require schema support:

- Add nullable UTC `Users.LastPasswordResetEmailSentAt`.
- Add a non-unique composite lookup index on `CommunityUsers(UserId, Role, Status, CommunityId)`.
- Add a non-unique invitation-state lookup index on `TeacherInvitations(CommunityUserId, AcceptedAt, RevokedAt, ExpiresAt)` if the equivalent does not already exist.
- Retain existing unique normalized-email and token-hash indexes and all existing rows.

Do not add a global unique `CommunityUser.UserId` constraint or a filtered/generated teacher-only constraint: preserved legacy inconsistencies could make migration fail, and non-teacher roles must retain supported relationships. New writes are protected through transactional locks and existing uniqueness constraints; legacy teachers with multiple current relationships fail safely.

### 8. Stable errors, Swagger, and documentation

Extend the existing `ErrorCode` enum without changing the `BaseResponse` envelope. Add stable codes for cross-community conflict, invalid/expired/revoked/used invitation, and invalid/expired password-reset token. Keep credential and forgot-password responses enumeration-safe. Document enum names and numeric values once assigned.

Swashbuckle will remove retired operations when controller actions are removed and discover new actions/DTOs automatically. Add explicit `[AllowAnonymous]` and `ProducesResponseType` annotations to touched anonymous actions, document owner authorization and 409 responses, then inspect the generated Swagger document to verify versioned paths, bodies, response envelopes, and absence of retired endpoints.

## Transaction Boundaries and Concurrency

| Operation | Transaction boundary | Concurrency/data-integrity rule |
|---|---|---|
| Owner invitation | Authoritative authorization recheck through membership reservation and invitation-row commit | Lock owner/community, target user/current Teacher relationships, membership, invitations, and license; increment seat once; resend revokes earlier token. Email after commit. |
| Invitation validation | No transaction; no-tracking read | Token-hash lookup only; performs no activation or mutation. |
| Invitation completion | Token/user/membership checks through Identity password operation, activation, acceptance, and optional refresh revocation | Lock invitation first; only one transaction can move unused to accepted. Recheck one-community invariant inside the lock. |
| Password login | Read/Identity sign-in checks, then token service operations | Membership eligibility is complete before any access/refresh token is issued. Multiple current relationships fail safely. |
| Forgot password | Short atomic conditional cooldown update; email outside transaction | Unknown/ineligible/cooldown/mail failure all return the same 202 contract. |
| Reset password | Existing Identity reset plus refresh-token revocation transaction | Password changes and active-session revocation succeed together or fail safely. |

## Automated Test Plan

1. Add controller/reflection contract tests proving registration, confirmation, resend, and authenticated accept routes are absent; validate and complete are anonymous; owner invite route is unchanged. No new HTTP test package is required.
2. Add login handler/service tests for email, username, generic invalid password, pending invitation, no community, multiple memberships, suspended/locked accounts, inactive community, and the requirement that token services are not called before eligibility succeeds.
3. Add invitation tests for new email, unauthorized/non-owner, same-community resend without duplicate membership/counter, cross-community pending/active 409, normalized-email user reuse, token hashing/revocation, validation masking and all invalid states, completion effects, one-time use, no profile/license, and unchanged counter.
4. Add real-MySQL integration cases for concurrent cross-community invitations, same-community resends, license capacity, and concurrent/repeated completion. Parameterize the existing MySQL fixture through an environment variable before reuse; do not run a destructive fixture against an unapproved database.
5. Add forgot-password tests for email/username, unknown/ineligible account, cooldown, SMTP failure, exact generic 202 response, and no raw token leakage. Add reset success/invalid/expired/password-policy tests and assert all refresh tokens are revoked.
6. Retain and run refresh/logout suites, access-token suites, all Firebase/player suites, and existing Google authentication suites unchanged, including B2C compatibility, StudentLicense activation, and pending-teacher activation coverage.
7. Run `dotnet build SprintLabs.sln`, focused test projects/classes, then `dotnet test SprintLabs.sln`. Baseline before implementation: 227 passed, 0 failed, 0 skipped.

## Delivery Order

1. Add stable shared results/errors/options and non-destructive entity/index configuration.
2. Evolve the existing identity/invitation service interfaces and compile-safe callers without behavior duplication.
3. Modify owner invitation and add its data-integrity tests.
4. Add anonymous validation and transactional completion; remove authenticated acceptance.
5. Tighten login before token issuance.
6. Modify forgot/reset password contracts and behavior.
7. Remove public registration/confirmation/resend routes and dead artifacts.
8. Generate and inspect the single migration.
9. Update Swagger annotations, `api.md`, `frontend.md`, and automated contract/regression tests.
10. Run build, focused tests, full tests, and opt-in MySQL concurrency tests.

## Complexity Tracking

No constitution violations or additional architectural complexity require justification.
