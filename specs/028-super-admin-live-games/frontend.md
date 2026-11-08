# Super-admin live games — frontend

Visible only to `PlatformAdmin` (sidebar tab "Live Games", routes `/live-games` and `/live-games/:liveGameId`).

## List page `/live-games`

- Table: Game, Join code, Questions (curriculum · grade · subject · unit), Players (`connected / maxPlayers`),
  Status, Created. A row opens the detail page. Refreshes every 5 s.
- Empty state: "No live games yet. Create one to get a join code."
- "Create live game" dialog (react-hook-form + yup):
  - Game name (required, ≤ 100 chars)
  - Curriculum (required), Grade (required, filtered by curriculum), Subject (optional, "All subjects"),
    Unit (optional, "All units") — options from `GET /api/v1/Question/journeys`
  - Players 2–8 (default 8)
  - On success: toast with the code and navigate to the detail page. Backend `400` messages are shown as toasts.

## Detail page `/live-games/:id`

- Loads `GET /api/v1/admin/live-games/{id}`, then subscribes to SignalR `Watch(id)`; every `LiveGameUpdated`
  replaces the cached snapshot. Falls back to polling (4 s when live updates are off).
- Header: name, status badge, question filter, "Game server connected" and "Live updates" indicators.
- Lobby: large join code with copy button and joining instructions; **Start game** (disabled until one player
  is in the room) and **Close lobby**.
- Running: stat cards (players, round x/10, right this round, leader), current question with right/wrong/waiting
  bar, leaderboard with each player's answer to the current question, activity feed, **End game** (confirm).
- Completed: final standings table.
