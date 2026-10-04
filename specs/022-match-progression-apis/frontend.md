# Game/frontend integration guide

This feature exposes backend APIs; it does not add UI, scheduling, missions, or a Unity HTTP adapter.

## Lobby profile and ranking

- After normal authentication, use `GET /api/v1/Progression/me` to refresh lifetime XP, level progress bar, gold, RP, current/highest rank and match/win counts.
- For a compact competitive panel, use `GET /api/v1/Ranking/me`.
- XP progress bar uses xpIntoCurrentLevel / nextLevelXpRequirement; experience is cumulative and must not be used as the current-level numerator.
- Show loading while reading, an empty/unavailable profile state on 404, sign-in on 401, and blocked-account feedback on 403. Keep the last successful profile when a transient read fails; offer retry.

## Match results

- Mirror server registers a match once with POST Matches. Preserve its MatchCode/start timestamp/ID for retries. Dedicated-server credentials must stay entirely server-side.
- Mirror server sends one complete request for all registered humans. It supplies authoritative placements and ordered genuinely human-attributed question facts, never client-selected XP/RP/levels.
- Show the server's immediate placement result while backend rewards are pending. Wait approximately 3–5 seconds; if slow, show “Rewards syncing”. Do not freeze return-to-lobby.
- On completion, select the owning human's reward by authenticated PlayerProfileId. Show xpGained and old/new level; show RP change only for Ranked. Friendly/Private RP remains unchanged.
- The server retries timeout/503 with bounded backoff and the same MatchId. The client refreshes progression later. Repeated settled results must replace the displayed summary, never accumulate XP locally.
- No mission rewards/claim button, currency form, or editable XP/RP/level field belongs to this feature. Completion is an automatic server action.
- Bots have no backend profile ID. When a human disconnects and AI controls the retained actor, stop attributing AI answers to that human. TotalPlayers still counts gameplay slots; only genuine human actions enter their result.

## Leaderboard sections

- Global table: Position, Name/Avatar, RP, Rank, Level, Total Wins. Use Ranking/leaderboard with page/pageSize.
- Community staff table: same columns, optionally Grade/Class filters from the existing authorized community lists. Use Communities/{communityId}/ranking/leaderboard with gradeId/classId. Both filters intersect; no new grade/class forms.
- Owner/admin visibility follows community permission; teachers see only assigned active classes. Ordinary players cannot request school-wide educational views.
- Empty items is a valid empty leaderboard. Loading, retryable errors, no-access errors, and paging controls should be explicit. Reset page to 1 after changing a filter. Page size maximum 100. Ties use stable ordinal positions.

## Match history sections

- Player history calls Matches/me/history. Table columns: Match Code, Type, Start/End, Position, XP gained, RP change. Null data remains unavailable until a settled reward exists.
- Match detail calls Matches/{matchId}; show authorized human placements, available statistics, question outcomes/timing and reward summaries. Other players' null statistics/rewards mean private, not zero. QuestionResults are ordered per human and do not contain answer text.
- Community staff history calls Communities/{communityId}/matches; authorized player drill-down calls Communities/{communityId}/players/{playerProfileId}/matches. Use existing students/roster selections; no editable identity form.
- Owners retain their school history after enrollment changes. Teachers see current assigned active class scope; moving/removing students can change a teacher's visible history.
- Pagination matches leaderboard bounds. Empty history is normal. 401 sign-in, 403 no-access, 404 unavailable resource, 400 invalid filter/page; show a retry action for transient failures.

## Later decisions

Actual Unity adapter/retry ownership and result UI wiring, dynamic Immortal Top 10, historical class-assignment snapshots, result correction workflow, and mission rewards remain separate tasks. No frontend is implemented here.

## Game leaderboard extension (2026-10-04)

The current game leaderboard contract adds enrolled-player school access, currentPlayer standing, availableFilters and UTC period net-RP points. Follow [the updated frontend guide](../024-game-leaderboards/frontend.md) for those screens; the staff/history guidance above remains applicable.
