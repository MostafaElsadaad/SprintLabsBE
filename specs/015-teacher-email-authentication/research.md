# Research: Teacher Email Authentication

## Current-State Findings

- `API/Program.cs` registers `IdentityCore<User>` with roles and EF stores, but it does not register `SignInManager`, default token providers, or the required Identity options.
- `Infrastructure/DataAccess/ApplicationDbContext.cs` explicitly ignores `EmailConfirmed`, `LockoutEnd`, `AccessFailedCount`, and `LockoutEnabled`; the current database snapshot therefore lacks those columns.
- `Infrastructure/ServiceConfig.cs` validates JWT lifetime but disables issuer, audience, and signing-key validation.
- `API/Program.cs` writes the JWT secret, issuer, and audience to console output at startup.
- `Infrastructure/Services/UserService.cs` creates existing Google-login access tokens with a hardcoded seven-day lifetime and local time. `Shared/Responses/LoginResponse.cs` is player-oriented and has no refresh-token fields.
- There is no SprintLabs refresh-token entity, repository, service, or table. `Shared/Responses/GoogleTokenResponse.cs` is unrelated third-party token data.
- The current Owner invite handler activates a Google-linked user immediately and the Google login handler activates all pending Teacher memberships through `ICommunityLoginActivationService`.
- `GET /api/v1/Users/me/communities` already returns only active memberships and supports zero or multiple communities.
- Normal CRUD uses `IBaseRepository<T>`. Refresh rotation and invitation issuance are the two persistence paths complex enough to justify narrow feature-specific persistence coordination.
- Existing tests use xUnit, FluentAssertions, Moq, EF Core InMemory, SQLite availability through Infrastructure, and an optional local MySQL fixture.

## Decision 1: Keep Identity Behind an Infrastructure Service

**Decision**: Add `ITeacherIdentityService` in `Domain/Services` and implement it in Infrastructure with `UserManager<User>`, `SignInManager<User>`, and the scoped `ApplicationDbContext`. Application CQRS handlers will orchestrate HTTP-independent use cases through this interface.

**Rationale**: The Identity `User` type is currently in Infrastructure. Injecting `UserManager<User>` or `SignInManager<User>` into Application would create the forbidden Application-to-Infrastructure dependency. A focused teacher identity service preserves the current dependency direction without moving or redesigning the existing user model.

**Alternatives considered**:

- Move `User` into Domain: rejected as an unrelated identity-architecture refactor.
- Add all behavior to `IUserService`: rejected because it would turn the existing general user lookup/JWT service into a large authentication service.
- Put Identity logic directly in controllers: rejected because controllers must remain thin.

## Decision 2: Register Sign-In and Separate Identity Token Providers

**Decision**: Extend the existing `AddIdentityCore<User>()` chain with configured password, lockout, unique-email, confirmed-email, `AddSignInManager()`, `AddDefaultTokenProviders()`, and two custom data-protection providers: 24-hour email confirmation and one-hour password reset.

**Rationale**: Confirmation and reset require different lifetimes. Microsoft documents custom `DataProtectorTokenProvider<TUser>` registrations for changing token lifetimes, while `AddSignInManager()` and default providers integrate with the current Identity store. The custom providers will live in Infrastructure; their configured durations come from Shared options.

**Alternatives considered**:

- One default data-protection lifespan for both tokens: rejected because it cannot satisfy both required lifetimes.
- Persist confirmation/reset tokens: rejected because Identity data-protection tokens are purpose-bound and need not be stored.
- Build custom token formats: rejected because Identity already provides the required security behavior.

Reference: [Microsoft account confirmation and password recovery guidance](https://learn.microsoft.com/en-us/aspnet/core/blazor/security/account-confirmation-and-password-recovery?view=aspnetcore-8.0).

## Decision 3: Harden Bearer Validation Without Redesigning Google SSO

**Decision**: Enable issuer, audience, signing-key, and lifetime validation globally; remove all secret logging; add clock-skew configuration; and issue new teacher access tokens through `IAccessTokenService` with UTC timestamps, `jti`, `sub`, `userId`, email, name, and `accountType=Teacher`. Do not add community roles. Leave `LoginResponse` and the Google player-profile flow intact.

**Rationale**: All tokens accepted by the API must pass complete validation. A separate teacher response avoids adding empty refresh/community fields to the existing Google response. Existing authorization reads `userId`, so that claim remains. Teacher tokens need no player-profile claim and cannot authorize a community without an active database membership.

**Alternatives considered**:

- Rebuild Google login on the new refresh-token flow: rejected as explicitly out of scope.
- Extend `LoginResponse`: rejected because it changes the serialized Google response contract.
- Put community roles in teacher tokens: rejected because membership changes must take effect from the database immediately.

Reference: [Microsoft JWT bearer validation guidance](https://learn.microsoft.com/en-us/aspnet/core/security/authentication/configure-jwt-bearer-authentication?view=aspnetcore-10.0).

## Decision 4: Use a Custom Refresh Repository for Atomic Rotation

**Decision**: Add `IRefreshTokenRepository`/`RefreshTokenRepository` plus `IRefreshTokenService`. Normal queries and creation remain narrow, while rotation uses a transaction and a conditional update on an unrevoked, unexpired token before inserting its replacement.

**Rationale**: Rotation, reuse prevention, logout, and revoke-all are reused by login, refresh, logout, and password reset. A one-line generic repository wrapper would be unnecessary, but this repository owns special atomic persistence behavior. The conditional update must affect exactly one row; concurrent attempts then allow only one replacement.

**Alternatives considered**:

- Only `IBaseRepository<RefreshToken>`: rejected because a read-then-write sequence is vulnerable to concurrent reuse.
- Store raw tokens: rejected by the security requirements.
- Stateless refresh tokens: rejected because logout, reset revocation, reuse detection, and rotation require server state.

## Decision 5: Use SHA-256 Hashes and Cryptographic Random Tokens

**Decision**: Generate 64 random bytes for refresh and invitation tokens, encode them with Base64URL, and persist only lowercase SHA-256 hexadecimal hashes. Configure each hash as fixed-length 64 characters with a unique index.

**Rationale**: Random tokens have sufficient entropy, Base64URL survives links and JSON without transformation, and deterministic hashes support indexed lookup without retaining bearer secrets.

**Alternatives considered**:

- Password hashing for lookup tokens: rejected because salted hashes cannot support direct indexed lookup and the tokens already have high entropy.
- GUID tokens: rejected because the required secure random payload provides more entropy and a consistent mechanism for both token types.

## Decision 6: Use a Focused Invitation Service for Identity Plus Seat Transactions

**Decision**: Add `ITeacherInvitationService` with an Infrastructure implementation. The Application invite handler retains Owner authorization and email orchestration; the service owns the transaction that finds/creates or marks the teacher identity, creates/restores a pending membership, reserves a seat once, revokes prior active invitations, and creates a new invitation.

**Rationale**: The `User` identity and EF context both live in Infrastructure. One feature-specific service can begin a database transaction shared by `UserManager` and `ApplicationDbContext`, preventing capacity, membership, invitation, and placeholder changes from being partially committed. It is narrower than a generic unit-of-work abstraction.

**Alternatives considered**:

- Keep the current handler-only sequence: rejected because concurrent invites can overrun `UsedTeachers` and split identity/membership changes.
- Add a generic unit of work: rejected as unnecessary abstraction.
- Make `CommunityUser.CommunityId` nullable for placeholders: rejected because a teacher account does not need a membership and every membership remains community-owned.

## Decision 7: Supersede Teacher Auto-Activation Only

**Decision**: Remove `ActivatePendingTeacherMembershipsAsync` from the login activation interface/service and remove its call from `GoogleAuthenticationCommandHandler`. Keep `ActivatePendingStudentLicensesAsync` and all player creation behavior unchanged. Every newly issued password-teacher invitation remains pending until explicit acceptance.

**Rationale**: This is the smallest compatibility change that enforces consent. Existing active memberships remain active; old pending memberships receive a new persisted invitation when the Owner resends.

**Alternatives considered**:

- Keep Google auto-activation for Google teachers: rejected because it bypasses explicit acceptance.
- Remove the whole login activation service: rejected because student-license activation remains required.

## Decision 8: Backfill Existing Teacher Markers

**Decision**: Add `Users.IsTeacherAccount` with default `false`, then backfill it to `true` for users having any existing `CommunityUser` with `Role=Teacher`. Add `LastConfirmationEmailSentAt` nullable. Persist the four currently ignored Identity confirmation/lockout fields with safe defaults.

**Rationale**: Existing teacher identities must remain recognizable, including passwordless placeholders and active Google teachers. Backfill does not grant community access because authorization still requires an active membership.

**Alternatives considered**:

- Leave all existing users false: rejected because existing invited teachers could be misclassified and duplicated.
- Infer teacher status from membership on every login: rejected because teachers may legitimately have zero memberships.

## Decision 9: Use Configured SMTP Without a New Package

**Decision**: Implement `IEmailService` with the .NET `System.Net.Mail` SMTP client for this Mailpit/Mailtrap-compatible phase. Bind host, port, SSL, sender, username, password, and timeout from `EmailOptions`; use no credentials when username/password are absent. Keep HTML templates inside the implementation.

**Rationale**: This meets the requested SMTP development workflow with no new package, which follows the repository's no-unapproved-packages rule. The service will create and dispose a client per send, use the cancellation-aware asynchronous API, and translate delivery failures to a controlled error without logging credentials or link tokens.

**Alternatives considered**:

- Add MailKit: technically preferred for modern production SMTP, but rejected until package approval is explicit.
- Use a vendor HTTP email API: rejected because the requested transport is SMTP.

Microsoft notes that `SmtpClient` is supported in .NET 8 but not recommended for new production development; production-grade provider modernization is therefore a documented follow-up, not hidden scope. Reference: [System.Net.Mail.SmtpClient](https://learn.microsoft.com/en-us/dotnet/api/system.net.mail.smtpclient?view=net-8.0).

## Decision 10: Preserve BaseResponse and Versioned Routes

**Decision**: Expose new operations under the existing versioned controller routes and wrap results in `BaseResponse`/`BaseResponse<T>`. Registration and generic email-dispatch operations return HTTP 202; successful state-changing confirmation, reset, refresh, logout, and acceptance return HTTP 200.

**Rationale**: The logical routes in the feature brief map to the established `/api/v1/...` convention. The response envelope is a non-negotiable project convention.

**Alternatives considered**:

- Return the unwrapped example payloads: rejected because it would create a second API response style.
- Add an unversioned account controller: rejected because it would bypass project routing conventions.

## Decision 11: Commit State Before External Email Delivery

**Decision**: Commit registration or invitation state and generate the link before sending email. If SMTP delivery fails, return a controlled service-unavailable error while leaving a valid unconfirmed account or pending invitation that can be resent; never roll back a successfully delivered email.

**Rationale**: SMTP cannot participate in the database transaction. Committing first avoids sending a link to nonexistent state. Resend cooldown and invitation reissue provide recovery.

**Alternatives considered**:

- Send inside the database transaction: rejected because slow external I/O extends locks and cannot be rolled back.
- Roll back after a send failure: rejected because delivery outcome can be ambiguous and a retry can safely reuse/reissue state.

## Package and Project Dependency Decision

- **New project references**: none.
- **New NuGet packages**: none.
- **Existing dependencies reused**: ASP.NET Core Identity EF stores, JWT bearer, EF Core 8, Pomelo MySQL, SQLite, MediatR, xUnit, FluentAssertions, and Moq.
- **Deferred option**: MailKit requires explicit approval if production SMTP needs modern authentication or capabilities beyond `System.Net.Mail`.

