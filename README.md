# SprintLabs AI Agent Setup Pack

Copy these files into the root of your SprintLabs repository:

```text
AGENTS.md
.codex/skills/*/SKILL.md
.specify/memory/constitution.md
docs/ai/*
docs/features/_template/feature-brief.md
```

Then initialize Spec Kit in your actual repo using the official CLI:

```bash
specify init . --force --integration codex --integration-options="--skills"
```

Recommended first Codex task:

```text
Read AGENTS.md and docs/ai/agent-workflow.md. Inspect the repository and summarize the current architecture and build/test commands. Do not modify files.
```

Recommended first feature:

```text
Use $sprintlabs-json-question-api and $sprintlabs-cqrs-api-slice.
Implement Insert Questions API based on existing Get Questions API.
Do not normalize question internals.
```
