# SprintLabs Agent Instructions

## Project context
SprintLabs is a .NET 8 backend for an education/game SaaS system.

Current architecture:
- API project contains controllers and HTTP concerns.
- Application project contains CQRS features, commands, queries, handlers, DTOs, and validation logic.
- Domain project contains entities, enums, repository/service interfaces, and domain rules.
- Infrastructure project contains EF Core DbContext, repositories, migrations, seed data, and external service implementations.
- Shared project contains BaseResponse, errors, common requests/responses, options, and shared exceptions.
- Database is MySQL through EF Core.
- Patterns already used: ASP.NET Core controllers, API versioning, MediatR, repository interfaces, BaseResponse, GenericException.

## Non-negotiable working rules
- First inspect existing files related to the task before changing code.
- Follow existing project style even if a different style would be cleaner.
- Modify the fewest files possible.
- Do not rewrite architecture during a feature task.
- Do not add new packages unless explicitly approved.
- Do not create generic abstractions, factories, managers, or base classes unless the existing code already requires them.
- Keep one vertical slice working before moving to the next one.
- When uncertain, prefer a small working implementation over a big flexible system.

## Ponytail / YAGNI rule
Before writing code, stop at the first option that solves the task:
1. Does this need to be built now?
2. Can the current code already do it?
3. Can EF Core, ASP.NET Core, MediatR, or .NET standard library do it?
4. Can an existing repository/service pattern do it?
5. Can this be solved with fewer files?
6. Only then write new code.

Do not be lazy about:
- input validation at API boundaries
- authorization and tenant isolation
- data integrity
- migrations
- tests for non-trivial logic
- error handling that prevents bad data or data loss

## Response format for implementation tasks
Before editing, output:
1. Files inspected
2. Existing pattern found
3. Minimal implementation plan
4. Risks or assumptions

After editing, output:
1. Files changed
2. Build/test commands run
3. Any failures
4. Manual verification steps

## API conventions
- Controllers should stay thin.
- Controllers should call MediatR commands/queries.
- Use `BaseResponse<T>` consistently at the controller boundary unless the existing feature already returns it from the handler.
- Use `GenericException` for controlled API errors.
- Use `ErrorMessage` and `ErrorCode` from Shared when possible.
- Keep route format consistent with existing controllers: `api/v{version:apiVersion}/[controller]`.
- Keep API versioning attributes consistent with existing controllers.

## CQRS feature conventions
For a new endpoint, prefer this structure:

```text
Application/Features/<FeatureName>/<ActionName>/
  <ActionName>Command.cs or <ActionName>Query.cs
  <ActionName>CommandHandler.cs or <ActionName>QueryHandler.cs
  <ActionName>Request.cs if needed
  <ActionName>Response.cs or Dto.cs if needed
```

If the existing feature uses a flatter folder, follow the existing folder style for that feature.

## Database conventions
- Use EF Core migrations for schema changes.
- Configure important constraints, max lengths, required fields, defaults, and indexes in `ApplicationDbContext.OnModelCreating` or equivalent configuration if the project later moves to separate configuration classes.
- For MySQL JSON-like payloads, store JSON as text/string unless the current schema already uses a native JSON column pattern.
- Do not normalize JSON payloads unless the feature explicitly requires querying inside the JSON.
- Always consider tenant/school ownership when adding new SaaS tables.

## Testing expectations
Run the smallest useful checks first:

```bash
dotnet build SprintLabs.sln
```

For testable logic, also run:

```bash
dotnet test SprintLabs.sln
```

If database integration tests require a local MySQL instance and cannot run, state that clearly and still run build.

## Git and task discipline
- Work on one user story at a time.
- One branch per feature or user story.
- Do not mix unrelated refactors with feature implementation.
- Keep commits small: schema, implementation, tests, fixups.
- Do not touch `.vs`, `bin`, `obj`, generated build output, or unrelated config.

## SprintLabs priority order
When starting execution, build in this order:
1. Shared SaaS foundation: tenants, organizations/schools, users/roles, teacher/student profiles, audit fields.
2. First vertical slice: Questions system, starting from existing Get Questions and Insert Questions JSON API.
3. Assignments and question selection.
4. Match creation/joining and game session lifecycle.
5. XP, ranking, match completion.
6. Missions.
7. Shop and inventory.
8. Admin/configuration and reporting.
