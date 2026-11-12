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
  is in the room), **Add bot** (to test with one player) and **Close lobby**.
- Running: stat cards (players, round x/10, right this round, leader), current question with right/wrong/waiting
  bar, leaderboard with each player's answer to the current question, activity feed, **End game** (confirm).
- Completed: final standings table.


## Question builder (2026-11-12)

The create dialog is replaced by a page, `/live-games/new` (`?set={id}` loads a saved set).

1. **Choose content**: game name, curriculum, grade, subject (optional), unit, lesson (from `/live-games/lessons`, or all
   lessons in the unit), question count (5–30), players. **Pick questions automatically** calls `auto-select`; the warning
   is shown when the lesson is short.
2. **Review questions**: ordered cards showing type, source (Sprintlab library / Edited copy / My question), unit and lesson,
   thinking skill and the correct answer. Per card: move up/down, edit, replace (another library question, excluding all
   current ones), remove. Cards without a correct answer are outlined red and block **Create game**.
   - **Add questions** dialog: *Sprintlab Library* (paged `GET /Question/bank/admin`, unit/lesson/type filters) and
     *My Questions* (search and filters). Duplicates are skipped with a notice.
   - **Write a question**: editor for all six types; saving always stores it in My Questions, then adds it.
   - Editing a library question changes only this game's copy (optionally also saves a copy to My Questions); editing a
     personal question can also update it in My Questions.
   - **Save as set**: name; when a set is loaded, choose "Update" or "Save as a new set".
3. Sticky footer: question and round count, **Create game**.

Live Games page tabs: **Games** (as before, now with the chosen question count), **My Questions** (filters, new, edit,
delete), **Saved sets** (Use → builder, Delete). The live view shows rounds as `x / questionCount` (10 for filter games).
