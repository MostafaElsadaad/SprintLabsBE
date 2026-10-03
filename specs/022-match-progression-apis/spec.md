# Match progression APIs

Date: 2026-10-03. Branch: `codex/022-match-progression-apis`.
Source: Jira NOX-84 and NOX-89 through NOX-92. User authorized autonomous backend implementation.

## Requirements

- Reuse progression models and the existing XP, level, and RP calculation services (NOX-85–88).
- A trusted Mirror server registers a match, receives its numeric backend ID, and sends one bulk completion request.
- Only a separate server credential may register or complete a match. User JWTs cannot write progression.
- A completed match returns its persisted reward snapshot on retry, without updating facts, totals, or logs.
- Save participant facts, chronological question results, rewards, profile totals, and audit logs atomically.
- Serialize competing completions at the match row and overlapping profile updates at ordered player rows.
- Derive streaks and aggregate validation from ordered question facts. Backend calculates rewards; clients never supply XP/RP/level rewards.
- Correct answer +10 XP, streak multipliers 2/4/6/8/10 = 1.2/1.4/1.8/2.2/3.0; wrong answers reset streak. Winner +50, other participants +20.
- Levels use cumulative thresholds with ceil(150 × level^1.25), including multiple level-ups.
- Ranked RP uses existing percentile, loss multipliers, whole-number rounding away from zero, and zero floor. Friendly/private preserve RP and rank.
- Expose authenticated self/authorized player progression and ranking reads.
- Expose paged global/community RP leaderboards, community grade/class filters, and suspended-user exclusion before paging.
- Expose self history, authorized match details, community history, and community player history.
- Preserve school isolation and teachers' currently assigned active classes. Owners retain their community's persisted history even after enrollment removal/archive.
- Document contracts, defaults, authorization, failure handling, and migration/integration steps.

## Decisions made without blocking implementation

- API routes follow existing `/api/v1/...` conventions; Jira's unversioned examples are conceptual.
- Add `POST /api/v1/Matches` as the minimum prerequisite for obtaining a real backend MatchId. MatchCode is a server-generated unique UUID string and registration retry key.
- Match type/community/room/start/roster are immutable after registration. Register active, backend-linked humans; no fabricated bot identity.
- TotalPlayers includes bots; human positions may therefore contain gaps and position 1 may belong to a bot. Bots get no persistent educational/progression records.
- Completion includes every registered human's server-derived facts, including disconnected humans' genuinely attributed actions. The server must stop human attribution when AI takes control; it must never report bot actions as human answers.
- QuestionResults preserve per-human chronological order. Wrong/unanswered outcomes must be included to reset streak. Redundant aggregates/streaks are checked, not trusted as reward inputs.
- A completed retry returns the original result even if the retry body differs. Retry cannot correct a settled match; corrections require a separate future audited workflow.
- Match rewards grant zero mission XP/coins. Existing mission XP helper remains available to a future authorized mission claim workflow; no client/server-supplied reward values are accepted here.
- Self progression/rank are private. Other-player reads require platform admin or owner/assigned teacher access; leaderboards contain only public profile fields.
- Community leaderboard/history routes are staff/admin views. No student access to school-wide educational records is introduced.
- Participants see all human placements but only their own educational statistics, question results, and reward snapshot. Other statistics are null. Teachers see only permitted players' detail rows.
- Immortal remains the explicitly requested NOX-88 placeholder; no dynamic Top-10 assignment is introduced. Existing Friendly/Private tiers remain intact.
- Completion honors the registered community context even if membership changes during play. New registration checks current active eligibility.
- History is completed matches only. Grade/class filters apply to community leaderboards, not history.
- Page size 1–100 (default 20), stable RP-descending/player-ID-ascending ties; match history completion-time-descending/ID-descending.
- Bounds: 128 slots/humans maximum, 1000 answers per human, 12800 answers total, UTC times with five-minute future-clock tolerance.

## Scope exclusions

No Unity runtime/client/server integration, deployment, live migration, mission engine/claims, match scheduling/options/join lifecycle, historical class-assignment snapshots, frontend UI, or unrelated dashboard changes.

## Acceptance verification

Focused service/HTTP tests cover successful writes and replay, invalid facts, roster/context mismatch, bot placement, non-ranked behavior, progression privacy, leaderboard isolation/filter/paging, historical owner access, and actual relational rollback after SQL writes. Optional isolated MySQL tests cover simultaneous same-match completion, simultaneous overlapping matches, and concurrent registration.
