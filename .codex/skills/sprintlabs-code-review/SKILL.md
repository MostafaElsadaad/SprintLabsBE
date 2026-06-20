---
name: sprintlabs-code-review
description: Use to review SprintLabs backend diffs for architecture fit, SaaS isolation, CQRS consistency, EF Core/MySQL safety, tests, and quota-wasting overengineering.
---

# SprintLabs Code Review Skill

Use this skill after Codex or another agent changes the code.

## Review priorities
1. Does it compile?
2. Does it follow the existing architecture?
3. Did it touch unrelated files?
4. Is tenant/school ownership handled where needed?
5. Are API contracts correct?
6. Are database constraints/indexes sensible?
7. Are input validation and controlled errors handled?
8. Is there unnecessary abstraction or dependency creep?
9. Are tests/checks appropriate?

## Output format
Return findings grouped by severity:

```text
Blockers
- ...

Should fix
- ...

Nice to have
- ...

Good parts
- ...

Recommended next patch
- ...
```

## Review rules
- Do not rewrite code unless asked.
- Be specific: mention file and line/function when possible.
- Prefer small patch suggestions.
- Flag overengineering.
- Flag missing authorization or tenant isolation immediately.
