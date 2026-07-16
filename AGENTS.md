# SprintLabs Agent Instructions


## Application feature folder structure

Do not put multiple commands, queries, handlers, requests, responses, and helper classes in one large file.

Follow this structure:

Application/Features/{FeatureName}/{SubFeatureName}/

* {SubFeatureName}Command.cs or {SubFeatureName}Query.cs
* {SubFeatureName}CommandHandler.cs or {SubFeatureName}QueryHandler.cs
* {SubFeatureName}Request.cs if needed
* {SubFeatureName}Response.cs or {SubFeatureName}Dto.cs if needed

For shared DTOs/helpers used by multiple subfeatures, use:

Application/Features/{FeatureName}/Common/

* SharedDto.cs
* SharedMapper.cs
* SharedAuthorization.cs

Rules:

* One public class per file unless the existing project clearly does otherwise.
* Handler files must contain only the handler and private helper methods directly related to that handler.
* Command/query files must not contain handlers.
* Request/response DTOs must not be mixed into handler files.
* Do not create a “god file” containing an entire feature.
* Match the existing SprintLabs feature folder style.

## Repository usage rule

Use the existing `IBaseRepository<T>` / `BaseRepository<T>` for normal CRUD and simple queries.

Do not create feature-specific repositories unless:

* the query is complex and reused in multiple places
* the feature needs special persistence behavior
* the existing base repository cannot support the required operation cleanly
* the user explicitly asks for a custom repository

For simple checks such as existence, uniqueness, find by email, find by slug, list with includes, or update entity fields, prefer:

* `IBaseRepository<T>.AsQueryable()`
* `AddAsync`
* `UpdateAsync`
* `SaveChangesAsync`

Avoid creating repository classes that only wrap one-line EF Core queries.

## Layering Rules

Follow the existing SprintLabs dependency direction:

- API -> Application, Infrastructure
- Application -> Domain
- Infrastructure -> Domain
- Domain -> Shared
- Shared -> nothing

Do not add new project references unless explicitly requested.

Service placement:

- Service interfaces go in `Domain/Services`
- Service implementations go in `Infrastructure/Services`
- Shared DTOs/results used by both interface and implementation go in `Shared`
- DI registration goes in `Infrastructure/ServiceConfig.cs`

Forbidden:

- Infrastructure must not reference Application
- Domain must not contain service implementations
- Shared must not reference any other project
- Application must not reference Infrastructure

Before adding/refactoring a service, inspect similar existing services and follow the same pattern.

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


## API and frontend documentation rule

Every backend feature that adds or changes APIs must include frontend-facing documentation.

For each feature, create or update:

* `specs/{feature-folder}/api.md`
* `specs/{feature-folder}/frontend.md`

`api.md` must include:

* endpoint
* method
* auth/role requirements
* request body
* success response
* common error responses
* frontend usage notes

`frontend.md` must include:

* required frontend pages/sections
* user actions
* forms and fields
* table columns
* validation rules
* loading/empty/error states
* permissions/visibility rules
* API calls used by each action

Do not invent frontend behavior that is outside the feature spec. If something is unclear, mark it as an open question.

## Backfilling documentation

When asked to document past features:

* Treat it as a docs-only task.
* Inspect actual controllers, DTOs, commands, queries, handlers, auth logic, and specs.
* Do not change implementation code.
* Do not invent behavior that does not exist.
* Mark unclear behavior as an open question.
* Prefer creating:

  * `specs/{feature-folder}/api.md`
  * `specs/{feature-folder}/frontend.md`


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

<!-- SPECKIT START -->
For additional context about technologies to be used, project structure,
shell commands, and other important information, read the current plan
at specs/013-level-progression-service/plan.md
<!-- SPECKIT END -->
