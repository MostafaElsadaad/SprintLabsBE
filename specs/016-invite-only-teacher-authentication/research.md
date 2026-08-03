# Research: Invite-Only Teacher Authentication

## Existing implementation inventory

- `AccountController` currently publishes teacher registration, confirmation, resend, password login, forgot/reset password, refresh/logout, Google, and Firebase actions under the versioned Account route.
- `CommunitiesController` already owns the only teacher invitation action. `InviteTeacherCommandHandler` establishes the owner context and delegates persistence to `TeacherInvitationService`.
- `CommunityInvitationsController` currently requires authentication and exposes `AcceptInvitation`; completion therefore cannot serve an anonymous invited teacher.
- `TeacherIdentityService` is the existing ASP.NET Core Identity boundary. It already preserves password lockout and produces Identity password-reset tokens.
- `TeacherInvitationService` already generates random invitation tokens, stores SHA-256 hashes, uses an EF transaction, reuses users and memberships in some cases, and sends no password.
- `AccessTokenService` and `RefreshTokenService` are the single JWT/refresh implementations. Refresh tokens are hashed, rotated conditionally, and revoked by logout/reset operations.
- `ApplicationDbContext` already protects normalized user email and invitation token hashes with unique indexes. There is no safe teacher-only one-community database constraint for preserved legacy data.
- `SmtpEmailService` already uses `FrontendOptions.BaseUrl`; its invitation and reset routes/contracts need modification, not replacement.
- `Program.cs` configures unique email, confirmed-email sign-in, password/lockout rules, default Identity token providers, and custom email-confirmation/reset providers.
- Baseline `dotnet test SprintLabs.sln --no-restore --nologo`: 227 passed, 0 failed, 0 skipped.

## Decisions

### 1. Modify the existing service stack

**Decision**: Evolve `ITeacherIdentityService`/`TeacherIdentityService` and `ITeacherInvitationService`/`TeacherInvitationService`, while continuing to call the existing access-token, refresh-token, email, repository, and Identity APIs.

**Rationale**: These services already own the necessary Identity, EF Core, JWT, refresh, and SMTP behavior. A second stack would create divergent token formats and recovery/invitation rules and would violate repository layering/YAGNI rules.

**Rejected**: A new invite-only authentication service or a feature-specific repository. The existing base repository query surface and Infrastructure services can express the required behavior.

### 2. Remove routing and application artifacts for the old public flow

**Decision**: Remove public controller actions for teacher registration, email confirmation, and resend confirmation, then delete only the now-unreferenced CQRS/validation/service methods.

**Rationale**: Absence of routes is the strongest guarantee that public account creation is unavailable and automatically removes them from Swagger. Existing users and Identity token-provider infrastructure remain untouched.

**Rejected**: Leaving public actions in Swagger but returning a disabled response. That still advertises an unsupported account-creation API.

### 3. Resolve identifier and check membership before tokens

**Decision**: Normalize the login identifier using Identity normalizers, resolve it by normalized email or username, preserve `CheckPasswordSignInAsync(..., true)`, and require exactly one Active Teacher relationship in an active community before calling token issuance.

**Rationale**: It preserves lockout and token formats while closing the current gap where tokens are issued before membership cardinality is known. If email and username resolution ever identify different users, fail generically and log an internal inconsistency without the raw identifier.

**Rejected**: Issuing a token and encoding a community choice. Multi-community teacher support and community selection are explicitly out of scope.

### 4. Enforce one community with transactions and locks, not a destructive constraint

**Decision**: During issue and completion, use an EF relational transaction with explicit MySQL row locking for the user, current Teacher memberships, invitation, community, and license rows. Add lookup indexes, but not a global or generated unique relationship constraint.

**Rationale**: MySQL InnoDB locking reads protect the decision against concurrent invitations/completions when all writes follow the same locking order. A broad unique `UserId` would incorrectly constrain non-teacher roles; a new teacher-only generated unique index could fail on existing inconsistent teacher data that must be preserved. See [MySQL InnoDB locking](https://dev.mysql.com/doc/refman/8.0/en/innodb-locking.html) and [locks set by statements](https://dev.mysql.com/doc/refman/8.0/en/innodb-locks-set.html).

**Rejected**: Application-only prechecks without a transaction, which are vulnerable to time-of-check/time-of-use races; a destructive cleanup migration, which conflicts with the specification.

### 5. Treat same-community reinvitation as token reissue

**Decision**: Reuse a same-community Pending Teacher `CommunityUser`, keep its single seat reservation, revoke any current invitation, and create a replacement token. A different current community returns HTTP 409 with `TeacherAlreadyBelongsToAnotherCommunity` and makes no changes.

**Rationale**: This preserves the reservation model, prevents duplicate membership/counters, and gives resends one-time-token semantics.

**Rejected**: Creating another membership/invitation without revocation or silently moving the user.

### 6. Split invitation validation and completion but retain the existing service

**Decision**: Add read-only anonymous validation and transactional anonymous completion vertical slices to the existing controller/service. Validation returns only community name, masked email, and expiry. Completion accepts raw token, name, and password and activates the existing pending membership.

**Rationale**: The frontend can render the setup screen safely, while the service still centralizes token hashing, membership invariants, Identity password operations, and replay protection.

**Rejected**: Reusing authenticated acceptance. The invited teacher has no completed credentials yet, and completion must not automatically authenticate.

### 7. Use Identity APIs for every password state

**Decision**: Use `UserManager.AddPasswordAsync` for users without a password. If an eligible reused user already has a password but invitation completion supplies a new one, use an Identity reset token and `ResetPasswordAsync`, then revoke existing refresh tokens within completion's transaction.

**Rationale**: This enforces configured validators and avoids direct password-hash mutation. Session revocation prevents a pre-existing password session from surviving a credential replacement.

**Rejected**: Assigning `PasswordHash` directly or inventing a separate password validator.

### 8. Persist the forgot-password cooldown

**Decision**: Add nullable `User.LastPasswordResetEmailSentAt` and an existing teacher-auth option for cooldown seconds. Atomically claim a send window with a conditional update, generate the Identity token, and send email outside the short database operation. Always return the exact generic 202 response, including SMTP failure.

**Rationale**: A process-memory throttle is not reliable across replicas/restarts. EF Core's set-based conditional update is suitable for an atomic claim, but it does not implicitly create a transaction with other calls, so boundaries must be explicit where multiple operations must be atomic. See [EF Core ExecuteUpdate](https://learn.microsoft.com/en-us/ef/core/saving/execute-insert-update-delete) and [EF Core transactions](https://learn.microsoft.com/en-us/ef/core/saving/transactions).

**Rejected**: Reusing the confirmation cooldown field, which mixes unrelated security workflows; revealing cooldown/account/email errors to callers.

### 9. Use userId in the reset contract

**Decision**: Change reset input and link from email to `userId`, retain the existing URL-safe token helper and Identity validation, and keep refresh-token revocation transactional.

**Rationale**: It matches the specified contract, avoids placing email in the reset URL, and reuses the established secure token provider.

**Rejected**: A custom reset-token table or storing raw Identity tokens.

### 10. Add stable feature errors without replacing BaseResponse

**Decision**: Extend the existing `ErrorCode` enum for `TeacherAlreadyBelongsToAnotherCommunity`, a single safe invalid teacher invitation error, and a safe invalid/expired reset token error. Preserve `BaseResponse` and generic credential/recovery wording.

**Rationale**: Stable errors aid frontend behavior without leaking existence or detailed token state.

**Rejected**: A new error envelope or distinct invitation errors for expired/revoked/used, which disclose unnecessary state.

### 11. Preserve Google and player authentication behavior

**Decision**: Do not edit Google endpoints, handlers, services, DTOs, claims, validators, or account-linking decisions. Keep Firebase/player flow unchanged as explicitly out of scope. If a shared interface signature forces a compile update, make only the smallest behavior-neutral caller adjustment and prove it with unchanged regression tests.

**Rationale**: Google behavior is a hard product boundary. Firebase currently has its own pending-membership activation behavior; changing it would broaden this teacher-password feature into player authentication.

**Rejected**: Consolidating external and password authentication or changing external account linking.

### 12. Let controllers drive Swagger discovery

**Decision**: Remove old actions, add new actions/DTOs with authorization and response annotations, and inspect generated Swagger. Do not add packages or a global operation filter.

**Rationale**: Swashbuckle already derives the document from MVC actions. This is the smallest reliable change and avoids unrelated global Swagger behavior.

**Rejected**: Maintaining a manual parallel OpenAPI document.

## Migration conclusion

One non-destructive migration is warranted for a persisted password-reset cooldown and missing lookup indexes. It must preserve all users, memberships, invitations, and refresh tokens; add no duplicate table; and contain no cleanup, delete, rename, or broad relationship uniqueness operation.

## Remaining unknowns

None require product clarification. Exact enum numeric values and migration names are implementation details to select consistently with the current repository. The plan intentionally interprets the invitation-completion rule as replacing the old teacher acceptance/password path, while preserving explicitly out-of-scope Firebase/player behavior.
