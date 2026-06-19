---
name: sprintlabs-cqrs-api-slice
description: Use for SprintLabs .NET backend tasks that add or modify one API endpoint using Controllers, MediatR commands/queries, handlers, DTOs, BaseResponse, and GenericException.
---

# SprintLabs CQRS API Slice Skill

Use this skill when implementing one backend endpoint or one vertical application slice.

## First inspect
Before editing, inspect the closest existing examples:
- `API/Controllers/*Controller.cs`
- `Application/Features/<related feature>/`
- `Domain/Models/`
- `Domain/Repositories/`
- `Infrastructure/Repositories/`
- `Shared/Responses/BaseResponse.cs`
- `Shared/Exceptions/GenericException.cs`

## Implementation rules
1. Keep controllers thin.
2. Put business logic in the handler.
3. Use MediatR request/handler pattern.
4. Use existing repository interfaces where possible.
5. Use `GenericException` for controlled failures.
6. Use `BaseResponse<T>` consistently at the same layer used by the surrounding feature.
7. Do not add FluentValidation or new packages unless asked.
8. Do not create a new service layer if the handler can call the existing repository cleanly.
9. Preserve existing route/versioning style.
10. Add or update tests only for meaningful logic.

## Output before code
Return:
- Files inspected
- Existing pattern found
- Minimal file changes planned
- Validation/error cases

## Output after code
Return:
- Files changed
- Build/test commands run
- Any command failures
- Manual API test example

## Checklist
- [ ] Controller route matches existing style.
- [ ] Request model does not expose server-owned fields unless intended.
- [ ] Handler validates trust-boundary input.
- [ ] Handler returns DTO, not EF entity, unless existing feature does so.
- [ ] Controlled errors use `GenericException`.
- [ ] No unrelated refactor.
- [ ] `dotnet build SprintLabs.sln` run or failure explained.
