---
name: sprintlabs-json-question-api
description: Use for SprintLabs Questions APIs that read, insert, update, or validate flexible question JSON payloads stored in QuestionsJson.
---

# SprintLabs JSON Question API Skill

Use this skill for the question bank/question pack API.

## Existing pattern to preserve
The current model stores a question pack as JSON text:
- `Domain/Models/QuestionsJson.cs`
- `Application/Features/Questions/GetQuestionsQueryHandler.cs`
- `API/Controllers/QuestionController.cs`
- `Infrastructure/DataAccess/ApplicationDbContext.cs`

## Rules
1. Insert questions as JSON payloads; do not normalize every question type unless explicitly requested.
2. Validate that JSON is valid before saving.
3. Require `grade`.
4. Treat `assignment` as nullable if the current design allows general pool questions.
5. Do not trust client-provided `id`, `createdAt`, `updatedAt`, or `version` unless the contract explicitly says so.
6. If uniqueness is `Grade + Assignment + Version`, handle duplicate conflicts clearly.
7. Return the saved entity/DTO in the same shape as Get Questions when practical.
8. Use `JsonDocument.Parse` or equivalent to validate payload safely.
9. Avoid hardcoding every possible question type unless the story requires strict schema validation.

## Good insert flow
1. Receive request with grade, optional assignment, payloadJson, optional version if approved.
2. Validate grade range.
3. Validate payloadJson is present and valid JSON.
4. Optionally validate top-level `Questions` exists if contract requires it.
5. Create `QuestionsJson` with server-owned timestamps.
6. Save through repository and `SaveChangesAsync`.
7. Return DTO.

## Error cases
- Invalid grade => 400
- Missing/invalid JSON => 400
- Duplicate grade/assignment/version => 409 or existing project failure style
- Unexpected DB failure => allow global exception handler to handle unless project has a specific pattern

## Checklist
- [ ] No JSON normalization added accidentally.
- [ ] JSON validity checked.
- [ ] Client cannot override server-owned fields accidentally.
- [ ] Uses existing `QuestionsJson` entity.
- [ ] Uses existing `BaseResponse`/`GenericException` style.
