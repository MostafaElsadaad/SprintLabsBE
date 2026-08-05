# Research: Platform Administrator Password Authentication

## Existing implementation inventory

### Account routes and CQRS

- `API/Controllers/AccountController.cs` publishes Google, Firebase, teacher password login, refresh, logout, forgot-password, reset-password, and profile actions under `api/v{version}/Account`.
- Teacher password slices are under `Application/Features/Accounts/TeacherAuthentication`. They establish the repository's command/request/handler separation and return `BaseResponse` at the controller.
- The refresh and logout routes are publicly shared contracts even though their internal namespaces and token response type still carry teacher names.

### User and ASP.NET Core Identity

- `Infrastructure/DataAccess/User.cs` inherits `IdentityUser<long>`, providing `UserName`, `NormalizedUserName`, `Email`, `NormalizedEmail`, `PasswordHash`, `EmailConfirmed`, `AccessFailedCount`, `LockoutEnabled`, `LockoutEnd`, security/concurrency stamps, and Identity login relationships.
- The same entity already contains `IsPlatformAdmin`, `IsTeacherAccount`, `GoogleId`, `Status`, and `LastPasswordResetEmailSentAt`.
- `API/Program.cs` configures unique email; confirmed-email sign-in; password length, casing, digit, and symbol rules; failed-attempt lockout; and the existing custom password-reset token provider.
- `ApplicationDbContext` and its snapshot contain unique normalized username/email indexes and every required Identity/platform-admin field.
- `UserStatus` currently has Active and Suspended. Requiring `Status == Active` safely excludes every current non-active state without adding a disabled column.

### Platform-administrator authorization

- `API/Controllers/AdminCommunitiesController.cs` requires bearer authentication, extracts the signed `userId` claim, and sends it to CQRS handlers.
- `Application/Features/Admin/Communities/Common/AdminCommunityAuthorization.cs` reloads the user through `IUserService.GetCurrentUser` and requires persisted `IsPlatformAdmin` plus non-suspension.
- There is no current `IsPlatformAdmin` JWT claim or ASP.NET authorization policy. The trusted convention is signed `userId` followed by a current database authorization check. A response/JWT `accountType` is descriptive, not sufficient authorization.

### Password JWT, refresh, and logout

- `Domain/Services/IAccessTokenService.cs` and `Infrastructure/Services/AccessTokenService.cs` are the modern password-token signer. They use configured issuer/audience/key/lifetime, UTC timestamps, HS512, `sub`, `jti`, `userId`, email, and name, but currently hardcode `accountType=Teacher`.
- `RefreshTokenService`, `RefreshTokenRepository`, `RefreshToken`, and `SecureTokenGenerator` implement the one reusable refresh stack: 64 random bytes, Base64url raw token, SHA-256 hash at rest, UTC expiry, unique hash, conditional single-use revocation, replacement hash, rotation, logout, and revoke-all.
- Rotation currently requires Active, confirmed `IsTeacherAccount`, and `RefreshTokenCommandHandler` then reloads only through `ITeacherIdentityService`. These are the two blockers for administrator refresh.
- `LogoutCommandHandler` is already account-neutral and delegates to the same hashed-token revocation service.

### Forgot/reset password and email

- `AccountController`, `ForgotPasswordCommandHandler`, and `ResetPasswordCommandHandler` expose one existing recovery flow.
- `TeacherIdentityService.CreatePasswordResetAsync` resolves trimmed normalized username/email, requires current teacher eligibility, atomically claims the existing cooldown, and generates the configured Identity reset token.
- `TeacherIdentityService.ResetPasswordAsync` calls `UserManager.ResetPasswordAsync` and revokes all refresh tokens in a transaction.
- `TeacherAuthenticationLinkBuilder` URL-safe-encodes the reset token and uses `FrontendOptions.BaseUrl`.
- `IEmailService`/`SmtpEmailService` already sends the generic SprintLabs reset email and logs only the recipient domain on delivery failure.
- The public controller always returns the same accepted response, and the handler suppresses delivery failure to prevent enumeration.

## Google authentication file map and behavioral freeze

### Active endpoint and application flow

- `API/Controllers/AccountController.cs` — `GoogleLogin(string googleAccessToken)` query-parameter contract and BaseResponse wrapping.
- `Application/Features/Accounts/GoogleAuthenticate/GoogleAuthenticationCommand.cs` — carries only `IdToken`.
- `Application/Features/Accounts/GoogleAuthenticate/GoogleAuthenticationCommandHandler.cs` — validates Google info, finds/creates the user, and enters the external-player workflow with teacher activation disabled.
- `Application/Features/Accounts/Common/IExternalPlayerLoginWorkflow.cs`
- `Application/Features/Accounts/Common/ExternalPlayerLoginContext.cs`
- `Application/Features/Accounts/Common/ExternalPlayerLoginWorkflow.cs` — rejects suspended users, creates/links a Player, activates matching student licenses, builds Google/player claims, and populates the legacy login response.

### Validation, identity linking, and JWT

- `Domain/Services/IGoogleAuthenticationService.cs`
- `Infrastructure/Services/GoogleAuthenticationService.cs` — validates Google ID tokens against configured allowed client IDs and maps subject/email/name/picture/domain.
- `Domain/Services/IUserService.cs`
- `Infrastructure/Services/UserService.cs` — `FindOrCreateGoogleUser` normalizes email, creates a new non-admin user or updates `GoogleId`/name/avatar, returns persisted `IsPlatformAdmin`, and `Authenticate` signs the legacy seven-day Google/Firebase JWT.
- `Infrastructure/ServiceConfig.cs` — registers Google validation and the legacy user service/JWT dependencies.
- `Application/ServiceConfig.cs` — registers the external-player workflow.

### Stored association and supporting types

- `Infrastructure/DataAccess/User.cs` — `GoogleId` association.
- `Domain/Models/Player.cs` and `Infrastructure/Repositories/PlayerRepository.cs` — player-side Google association used by current login behavior.
- `Shared/Responses/GoogleUserResponse.cs`
- `Shared/Responses/LoginResponse.cs`
- `API/appsettings.json` — `Authentication:Google:AllowedClientIds`.
- `API/API.csproj` and `Infrastructure/Infrastructure.csproj` — existing Google packages.

### Present but unused legacy Google types

- `Application/Features/Accounts/GoogleAuthenticate/GoogleAuthResponse.cs`
- `Shared/Responses/GoogleTokenResponse.cs`
- `Shared/Options/GoogleOptions.cs`
- `IGoogleAuthenticationService.GenerateGoogleClaims` is present but the current Google handler does not call it.

These types must not be repurposed for password login. Google Cloud Storage files are Google-branded but are storage infrastructure, not authentication, and are outside this feature.

### Existing Google regression coverage

- `SprintLabs.Tests/Features/B2CPlayerProfileSupport/B2CLoginCompatibilityTests.cs`
- `SprintLabs.Tests/Features/StudentLicenseActivation/StudentLicenseActivationOnLoginTests.cs`
- `SprintLabs.Tests/Features/OwnerTeacherManagement/PendingTeacherActivationTests.cs`

Their expectations remain unchanged. Current Google login creates/links a Player even when the persisted user is a platform administrator; preserving Google behavior means leaving that result intact. Password login and setup bypass the external-player workflow and therefore create no player/community records.

## Decisions

### 1. Add a narrow platform-admin Identity adapter

**Decision**: Add `IPlatformAdminIdentityService`/`PlatformAdminIdentityService` for administrator password authentication and first-password setup only.

**Rationale**: Application cannot depend on Infrastructure's `User`/`UserManager<User>`. A narrow adapter follows existing layering, keeps the feature separate from teacher login, and reuses the configured UserManager, SignInManager, DbContext, and refresh revocation service.

**Alternatives considered**:

- Put administrator methods on `ITeacherIdentityService`: rejected because it blurs the explicitly separate account type.
- Rename the entire teacher service into a generic account service: rejected as a broad mechanical refactor across stable teacher code/tests.
- Implement Identity directly in Application/controller: rejected by layering and thin-controller rules.

### 2. Extend recovery in place

**Decision**: Keep the existing forgot/reset endpoints, handlers, Identity provider, link builder, email method, cooldown, and `TeacherIdentityService` recovery methods; add platform-admin eligibility branches without a new recovery service.

**Rationale**: This retains one public response and one token/email/revocation flow. The service name is legacy, but a rename or second stack costs more and adds no product value.

**Alternatives considered**: Administrator-specific forgot/reset endpoints or provider were rejected as duplication and an enumeration risk.

### 3. Parameterize the modern access-token signer

**Decision**: Add a trusted non-persisted `AuthenticatedAccountType` parameter to `IAccessTokenService.Create` and update password-auth callers explicitly.

**Rationale**: It preserves every claim/lifetime/signing rule while replacing the only hardcoded teacher value. Google does not call this service, so its JWT remains unchanged.

**Alternatives considered**: A second admin JWT service was rejected; changing `UserService.Authenticate` was rejected because it would alter Google behavior.

### 4. Make shared refresh rotation account-aware

**Decision**: Keep one refresh service/route/repository, perform eligibility and account-type selection inside rotation, and return the trusted subject data needed to mint the next access token.

**Rationale**: Current teacher-only checks are the only incompatibility. Returning trusted subject data removes the teacher-only reload and closes the state-change window between rotation and access-token creation.

**Alternatives considered**:

- Add an administrator refresh endpoint/service: rejected as a parallel stack.
- Add account type to the RefreshToken table: rejected because authorization already uses persisted state and deterministic PlatformAdmin precedence handles dual-flag users without a migration.
- Trust an account type from the client: rejected as privilege escalation.

### 5. Use persisted platform-admin precedence during refresh

**Decision**: If a current user has `IsPlatformAdmin`, refresh reconstructs a PlatformAdmin token; otherwise it preserves the eligible Teacher result.

**Rationale**: Refresh tokens do not store origin. PlatformAdmin precedence satisfies the requirement that an administrator session not silently become a teacher/community session, while actual authorization remains the current persisted admin check.

**Alternatives considered**: Teacher precedence would contradict the admin-refresh requirement for dual-flag users; token-origin persistence is unnecessary schema complexity.

### 6. Serialize password operations with refresh rotation

**Decision**: On MySQL, first-password setup, password reset, and refresh rotation use the existing User row as their common transaction lock. Rotation rechecks token state after acquiring the user lock.

**Rationale**: Without a shared lock, rotation can create a replacement after revoke-all and leave a pre-password session renewable. The existing row provides coordination without new schema or a distributed lock.

**Alternatives considered**: Best-effort revoke after password change was rejected because it has a known race; a new lock table/service was rejected as unnecessary.

### 7. Preserve the existing authorization convention

**Decision**: New tokens contain signed `userId` and an informational trusted `accountType`; existing protected handlers continue reloading `IsPlatformAdmin`.

**Rationale**: It works with current endpoints and immediately reflects suspension/demotion. Community claims and a new policy are unnecessary.

**Alternatives considered**: Authorizing only on an `accountType` claim was rejected as weaker and stale.

### 8. Do not change Google confirmation behavior

**Decision**: First-password setup does not set `EmailConfirmed` and the Google path is not changed. Password login/recovery follow the current Identity confirmed-email policy.

**Rationale**: The requested Google behavior is a hard compatibility boundary. Trusted administrator provisioning must ensure any Google-only administrator satisfies the current confirmation policy before password login.

**Alternatives considered**: Marking email confirmed during Google login or setup was rejected because it changes account/Google behavior beyond the specified endpoint effects.

### 9. Use Identity for all password writes and errors

**Decision**: Use `CheckPasswordSignInAsync(..., true)`, `AddPasswordAsync`, and the existing `ResetPasswordAsync`. Map only Identity error codes/descriptions into safe validation responses; never expose the submitted password.

**Rationale**: Existing validators, lockout, security stamp, concurrency stamp, and reset provider remain authoritative. `AddPasswordAsync`/`ResetPasswordAsync` do not remove `GoogleId` or Identity login rows.

**Alternatives considered**: Direct `PasswordHash` writes or a custom password validator were rejected.

### 10. Add one stable setup conflict

**Decision**: Append `PlatformAdminPasswordAlreadyConfigured` to existing `ErrorCode`/`ErrorMessage` and return HTTP 409 when setup targets an already-password-enabled admin.

**Rationale**: It gives the frontend a stable action while keeping login failures generic.

**Alternatives considered**: Reusing generic InvalidInput would make the intended reset-password redirect unreliable.

## Migration conclusion

**Decision**: No EF Core migration is required or planned.

**Evidence**:

- Identity normalized identifiers, unique username/email indexes, password hash, confirmation, access-failure, lockout, security stamp, and concurrency stamp exist in the current snapshot.
- `IsPlatformAdmin`, `Status`, and `GoogleId` already exist.
- `LastPasswordResetEmailSentAt` already exists from `20260802001536_InviteOnlyTeacherAuthentication`.
- Refresh-token hash, expiry, revocation, replacement, IP fields, foreign key, and lookup indexes already exist from `20260725150207_AddTeacherEmailAuthentication`.
- Identity external-login tables already exist through `IdentityDbContext` even though the active Google path uses `User.GoogleId`.
- No community relationship is required, so no membership/schema addition is needed.

Any implementation-time model diff is treated as an unintended change to investigate, not a reason to generate a migration automatically.

## Baseline

- `dotnet build SprintLabs.sln --no-restore --nologo`: succeeded with 33 pre-existing warnings and 0 errors.
- `dotnet test SprintLabs.sln --no-build --nologo`: 247 passed, 0 failed, 0 skipped.

## Remaining unknowns

None require product clarification. The only deployment precondition to verify is that existing Google-only platform administrators have `EmailConfirmed` when the current policy requires it; this feature deliberately does not alter Google login or silently confirm email.
