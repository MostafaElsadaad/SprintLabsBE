# SprintLabs Production Go-Live Checklist

Use this checklist before promoting the SprintLabs backend from staging to Production. Do not copy the staging `.env` file into Production.

## 1. Release preparation

- [ ] Merge the approved release commit into `main` through the normal review process.
- [ ] Record the exact release commit SHA and container image tag/digest.
- [ ] Run `dotnet build SprintLabs.sln` and `dotnet test SprintLabs.sln` against the release commit.
- [ ] Review the release diff for migrations, configuration changes, secrets, and unrelated files.
- [ ] Back up the Production database and verify that the backup can be restored.
- [ ] Review pending EF Core migrations and apply them through the normal deployment process.

## 2. Production environment configuration

Set Production configuration explicitly:

```text
ASPNETCORE_ENVIRONMENT=Production
DevelopmentAuthentication__Enabled=false
DevelopmentAuthentication__SeedPlayers=false
```

- [ ] Remove `DevelopmentAuthentication__ApiKey` from Production configuration; it has no Production use.
- [ ] Confirm `ASPNETCORE_ENVIRONMENT` is present inside the running container and equals `Production`.
- [ ] Confirm no staging `.env`, database connection string, Firebase credential, JWT signing secret, SMTP credential, or development key was copied into the Production image or repository.
- [ ] Load Production secrets only from the deployment platform's secret store or protected environment configuration.
- [ ] Use Production-specific database, Firebase, frontend URL, email, and JWT issuer/audience values.
- [ ] Restrict the Production environment file and secret mounts to the service account that runs the container.

The backend guard rejects development authentication whenever the runtime environment is Production, even if a development flag is accidentally enabled. The explicit `false` values above are still required as defense in depth.

## 3. Development-player data decision

The development players use account keys `dev-player-01` through `dev-player-08` and emails under `development.sprintlabs.invalid`.

- [ ] Confirm the Production database does not contain these development Users or Players.
- [ ] If they were accidentally seeded, take a database backup and inspect all foreign-key relationships before removal.
- [ ] Remove accidental development data only through an approved, reviewed cleanup procedure; do not run an ad-hoc broad delete.
- [ ] Confirm normal customer/player records are not matched by the cleanup criteria.

Leaving the rows present does not enable development login in Production, but removing accidental seed data avoids test identities appearing in reporting or administration tools.

## 4. Authentication verification

- [ ] Verify `GET /api/v1/Account/development-players` is unavailable in Production.
- [ ] Verify `POST /api/v1/Account/development-login` is unavailable in Production.
- [ ] Repeat both checks with development flags deliberately set to `true` in an isolated Production-mode validation run; both routes must remain unavailable.
- [ ] Verify `POST /api/v1/Account/firebase-login` still issues the normal SprintLabs JWT.
- [ ] Use that JWT with `GET /api/v1/Users/me` and confirm the trusted `UserId` and `PlayerProfileId` match the authenticated account.
- [ ] Verify expired, malformed, wrong-issuer, and wrong-audience JWTs are rejected.
- [ ] Verify no authentication endpoint logs access tokens, provider tokens, credentials, or request secrets.

## 5. Deployment and runtime checks

- [ ] Deploy an immutable image built from the recorded release commit.
- [ ] Preserve the required Firebase credential mount and use read-only mode.
- [ ] Preserve the intended Docker network, published ports, reverse-proxy/TLS configuration, and restart policy.
- [ ] Confirm database connectivity and migration completion before accepting traffic.
- [ ] Confirm health checks and representative authenticated API calls succeed.
- [ ] Review startup logs for errors and verify development-player seeding did not run.
- [ ] Confirm HTTPS is enforced at the public ingress and only required ports are exposed.
- [ ] Confirm monitoring, alerting, log retention, and database backup schedules are active.

## 6. Security review

- [ ] Scan tracked files and the container image for database passwords, JWT secrets, Firebase tokens, Google tokens, SMTP credentials, API keys, and private keys.
- [ ] Rotate any secret that was shared with staging, exposed in logs, or stored outside the approved secret system.
- [ ] Confirm Production JWT signing material is different from development/staging material.
- [ ] Confirm the staging development-authentication key is not available to the Production container.
- [ ] Review access to the Production host, registry, database, secret store, and deployment pipeline.

## 7. Rollback readiness

- [ ] Keep the prior known-good image tag/digest available.
- [ ] Document the container rollback command for the Production host without embedding secrets.
- [ ] Identify whether any migration in the release is destructive or not backward-compatible.
- [ ] Define the rollback decision owner and the monitoring signals that trigger rollback.
- [ ] After rollback, repeat authentication, `/Users/me`, database, and health checks.

## 8. Final sign-off

- [ ] Engineering confirms the deployed commit, build, tests, migrations, and runtime checks.
- [ ] Security confirms secret handling and development-authentication denial.
- [ ] Product/operations confirms smoke-test results and monitoring coverage.
- [ ] Record deployment time, image digest, migration version, approvers, and any accepted risks.
