---
name: sprintlabs-test-writer
description: Use for adding focused SprintLabs tests for handlers, validation, repositories, EF Core behavior, and API contracts without creating heavy test infrastructure.
---

# SprintLabs Test Writer Skill

Use this skill when adding tests for a completed or planned backend slice.

## Test philosophy
- Add the smallest test that would catch a real regression.
- Prefer handler tests for business logic.
- Prefer validation/error tests for API boundary logic.
- Use repository/EF tests only when relationship, index, query, or persistence behavior matters.
- Do not build a large fake framework.

## Existing test stack
The project already references:
- xUnit
- FluentAssertions
- Moq
- EF Core InMemory
- MySQL test fixture

Use what already exists.

## Checklist
- [ ] Test targets one behavior.
- [ ] Test name states expected behavior.
- [ ] No dependency on real external services.
- [ ] Database test clearly states if it requires local MySQL.
- [ ] `dotnet test SprintLabs.sln` run or limitation explained.
