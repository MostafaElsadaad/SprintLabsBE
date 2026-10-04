# Game leaderboards
Date: 2026-10-04. Branch: `codex/024-game-leaderboards`.
User scope: leaderboard APIs only; questions, inventory, shop, cosmetics, rank metadata and profile changes excluded.

## Requirements
- Preserve existing versioned global/community leaderboard routes and public row fields.
- Add authenticated own standing independent of the requested page, also included on list responses.
- AllTime uses current RP; Week/Month/Year use signed net RP change from completed Ranked matches persisted within the current UTC calendar period. Weeks start Monday. Period participants only; zero/negative scores allowed; no XP-based standings.
- Use backend CompletedAt, not client EndedAt, for period inclusion. Community periods count only that community's matches. Period boundaries are [start,end); future completions are excluded using one request clock instant.
- Return permitted grade/class ID/name pairs for filter controls without student roster access.
- Sort points descending then profile ID ascending. Page 1+, size 1-100, bounded offsets. Points Int64.
- Active linked users only. School boards require an active community and licensed active class; staff behavior stays owner/admin all eligible and teacher assigned classes.
- Permit active enrolled players to see public school standings and filter only their enrolled grade/class IDs. No other school access, answers, emails, membership details or private progression projections.
- Students require a license bound to both authenticated user and player profile. Revocation/archive/suspension removes access. No second mutable ranking authority or recalculation of rewards.
- Invalid period/paging/IDs -> 400; missing community/foreign filters -> 404; unauthorized school/class -> 403. Valid unranked own standing is null; no player profile on own-only endpoint -> 404.
- No migration/dependencies/deployment or Unity change required; use existing reward records. No past-period selection, historical enrollment snapshots, seasons or dynamic Immortal behavior.

## Verification
Solution build passed with existing warnings; full suite 519 passed, 10 MySQL-dependent skips, zero failures. Focused MatchProgression suite 67 passed, four skips. SQLite executes scope/aggregation/ordinal queries; TestHost validates envelopes, identity, routes, parameter binding and UTC dates. MySQL runtime, staging deployment and Unity are unverified.
