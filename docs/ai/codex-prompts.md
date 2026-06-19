# SprintLabs Codex Prompt Pack

## 1. Repository reconnaissance
```text
Read AGENTS.md. Inspect the solution structure and summarize the architecture, key projects, important patterns, and build/test commands. Do not modify files.
```

## 2. Convert one user story into implementation plan
```text
Use the SprintLabs instructions.
User story: <paste one story>
Epic docs: <path>
Existing files to inspect first: <paths>
Create a minimal implementation plan. Do not modify files yet.
```

## 3. Implement one vertical slice
```text
Use $sprintlabs-cqrs-api-slice and any other relevant SprintLabs skill.
Implement this one story only: <story>
Spec path: <path>
Do not touch unrelated files.
First inspect existing patterns and output the plan, then implement.
After implementation run dotnet build SprintLabs.sln and report the result.
```

## 4. Database change
```text
Use $sprintlabs-efcore-mysql.
Implement only the schema changes required for: <story>
Use EF Core and MySQL-safe constraints/indexes.
Generate a migration if possible.
Do not implement APIs yet unless this story explicitly includes them.
```

## 5. Questions JSON insert API
```text
Use $sprintlabs-json-question-api and $sprintlabs-cqrs-api-slice.
Implement Insert Questions API based on the existing Get Questions API.
Insert questions as JSON only. Do not normalize question internals.
Validate grade and JSON payload.
Return response using existing BaseResponse style.
Run dotnet build SprintLabs.sln.
```

## 6. Review another agent's diff
```text
Use $sprintlabs-code-review.
Review the current git diff only. Do not modify files.
Focus on architecture fit, tenant isolation, API contract, EF Core/MySQL safety, tests, and overengineering.
```
