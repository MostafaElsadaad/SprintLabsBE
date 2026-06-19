# SprintLabs AI Agent Workflow

## Daily flow
1. Pick one user story only.
2. Create or update its spec in `specs/<number>-<feature-name>/spec.md`.
3. Create/update the technical plan.
4. Generate tasks.
5. Give Codex one small task batch.
6. Review diff manually.
7. Run build/tests.
8. Commit.

## Best prompt shape for Codex

```text
Use the SprintLabs instructions and the relevant skill.
Task: <one user story>
Relevant docs: <spec path>, <api contract path>, <db design path>
Relevant files to inspect first: <file paths>
Do not touch unrelated files.
First inspect the current pattern and output a short plan.
Then implement the smallest working vertical slice.
Run dotnet build SprintLabs.sln and report results.
```

## Work splitting with another developer
Do not split by database/API/frontend layers. Split by owned systems.

Good split:
- Developer A: Questions + Assignments
- Developer B: Missions + Shop/Inventory
- Shared small phase: Tenant/User/Role foundation

Bad split:
- Developer A: database only
- Developer B: APIs only

## Quota-saving rules
- Give exact file paths.
- Give one user story per run.
- Ask Codex to inspect before coding.
- Prefer skills over long repeated prompts.
- Keep AGENTS.md concise.
- Store detailed epic docs in `docs/features` or `specs`, then reference only the needed doc.
- Do not paste the full project plan into every prompt.
