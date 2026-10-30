# Backend + Unity starter prompt

Replace paths with your own. Open a workspace that permits access to both repositories. Start with one concrete feature.

```text
Work on this feature across the SprintLabs backend and Unity game:
TASK: <one feature/integration and its expected player behavior>

Backend checkout: <absolute backend repo path>
Unity checkout: <absolute Unity repo path>

Read each repo's AGENTS.md and applicable nested instructions.
Read backend docs/ai/current-state.md, docs/ai/game-developer-guide.md,
and the owning feature's api.md/frontend.md under specs.
Read Unity PROJECT_HANDOFF.md and inspect existing adapters, UI and server code.
Verify old handoff claims against current source.

Inspect git status, branches, remotes and commits in both repos. Preserve unrelated
work. Create feature branches in the repos needing edits from the intended updated
bases; use isolated worktrees if needed. Do not reset someone else's changes.

Before editing, summarize inspected files, existing patterns, the smallest plan
and material assumptions. Then implement across both repos, including small
backend fixes or content tooling needed to unblock the game. Reuse existing APIs
and seed mechanisms before adding new ones. Follow local skills and architecture;
preserve existing game API compatibility and authorization.

Use local backend/database and local game-server testing by default. Shared staging
writes require task authorization for that target and intended changes. Keep all
credentials private and server credentials out of player builds. Preserve shared
question UUIDs, retry-safe purchases/completion and authoritative progression.

Run backend build/tests and appropriate Unity checks. Follow Unity's rule against
a competing batch process on an open project. Separate checks actually run from
manual checks remaining. Verify persistence through read APIs and verify retries
cannot duplicate purchases, rewards or question history where relevant.

Update owning API/frontend docs and integration examples when contracts change.
Update backend current-state.md and Unity PROJECT_HANDOFF.md for material changes.
Finish with files changed, validation results, known limits and exact testing steps.
Do not push, merge or deploy unless this task explicitly authorizes it.
```

For the existing B2C setup/shop work, replace TASK above with:

```text
Inspect the Unity integration for B2C first-login setup and the coin shop. Complete
missing integration using backend specs/030-coin-shop/game-integration.md, verified
against current source. Wire existing setup/shop screens, real bundled asset keys,
buy-once purchases, Use/Equip and saved selection. Reuse working pieces. Make only
backend fixes/content tooling needed for this integration. Report missing UI/assets
as concrete blockers rather than inventing production assets.
```

For content-only tasks specify the target environment, stable keys, desired records
and whether to create, update or deactivate each record.
