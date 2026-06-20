# SprintLabs Constitution

## 1. Vertical slices over layer-first work
Each feature must be delivered as a working vertical slice: database changes, domain/application logic, API contract, tests or verification, and documentation updates where needed. Do not create a full database for the entire SaaS before implementing features.

## 2. Existing architecture wins
SprintLabs uses .NET 8, ASP.NET Core, MediatR/CQRS, Domain/Application/Infrastructure/Shared separation, EF Core, MySQL, BaseResponse, and GenericException. New code must follow the current style unless an explicit refactor task exists.

## 3. SaaS data isolation
Any B2B/SaaS feature that stores school, tenant, teacher, class, student, assignment, match, purchase, mission, or progress data must define ownership and isolation rules before implementation.

## 4. JSON where flexibility matters
Question payloads may be stored as JSON blobs when the product needs flexible question types and does not need database-level querying inside each question. Do not prematurely normalize question internals.

## 5. Minimum useful implementation
Build only what the current story needs. Avoid speculative abstractions, future-proofing, and unused generic systems.

## 6. Quality gates
Every implemented story must pass `dotnet build SprintLabs.sln`. When logic is non-trivial, add the smallest useful test and run `dotnet test SprintLabs.sln` when possible.

## 7. Documentation is executable context
Epics, API contracts, DB notes, and task plans should live in `specs/` or `docs/features/` and be kept close to the code changes they guide.
