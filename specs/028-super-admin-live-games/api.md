# Super-admin live games — API

A platform admin creates a live game with a question filter (curriculum, grade, optional subject/unit).
Any signed-in player joins it in the game app with the 5-character code, whatever their own grade.
The game server polls the backend over HTTP; dashboards get live updates over SignalR.

## Admin endpoints (`Authorization: Bearer <platform admin JWT>`)

All return `BaseResponse<LiveGameResponse>` (or a list). Non-admins get `403`.

| Method | Route | Body | Notes |
| --- | --- | --- | --- |
| POST | `/api/v1/admin/live-games` | `{ title, curriculum, grade, subject?, unit?, lesson?, term?, language?, maxPlayers }` | `400` when no question matches the filter, `maxPlayers` is outside 1–8, or the grade is outside 1–12. Returns the game with its `code`. |
| GET | `/api/v1/admin/live-games` | — | Latest 50 games, newest first. |
| GET | `/api/v1/admin/live-games/{id}` | — | Full snapshot; use after a page reload. `404` if missing. |
| POST | `/api/v1/admin/live-games/{id}/start` | — | Lobby → Running. `400` when nobody has joined or the game has ended. |
| POST | `/api/v1/admin/live-games/{id}/bots` | â€” | Lobby only: asks the game server to add one bot (random bot name). `400` when the room is full or the game started. |
| POST | `/api/v1/admin/live-games/{id}/end` | — | Lobby → Cancelled, Running → Completed. The game server ends the match on its next poll. |

`LiveGameResponse`: `id, code, title, status (Lobby|Running|Completed|Cancelled), curriculum, grade, subject, unit,
lesson, term, language, maxPlayers, createdAt, startedAt, endedAt, gameServerConnected, gameServerLastSeenAt,
questionsAsked, currentQuestion {number, type, text, openedAt, correctCount, wrongCount},
players [{playerProfileId, displayName, connected, score, correctAnswers, wrongAnswers, currentAnswer (correct|wrong|null), rank}],
activity [{at, message}]`. Times are UTC.

Curriculum/grade/subject/unit options come from the existing anonymous `GET /api/v1/Question/journeys`.

## SignalR hub

`/hubs/live-games` (JWT in `access_token` query string, platform admin only).

- Client → server: `Watch(liveGameId)`, `Unwatch(liveGameId)`.
- Server → client: `LiveGameUpdated(LiveGameResponse)` after every change.

nginx on the staging VM does not forward WebSocket upgrade headers yet, so the dashboard uses
WebSockets when possible and long polling otherwise, and also refetches the snapshot every few seconds.

## Game-server endpoints (`Authorization: GameServer <key>`)

| Method | Route | Purpose |
| --- | --- | --- |
| GET | `/api/v1/game-server/live-games/active` | `[{ code, status, maxPlayers }]` for lobbies, running games and games ended in the last 10 minutes. Also marks the game server as connected. |
| POST | `/api/v1/game-server/live-games/{code}/events` | Up to 200 `LiveGameEventRequest` facts; idempotent by `eventId`. Returns how many were new. |
| GET | `/api/v1/game-server/live-games/{code}/questions` | `{ payloadJson }` in the legacy pack format, shuffled, up to 20 questions. |

Event types: `player_joined`, `player_left`, `match_started`, `start_failed`, `question_opened`, `answer_graded`,
`score_changed`, `player_result`, `match_ended`.

## Limits and known gaps

- Live state (roster, scores, current question) is held in memory by the single API instance. A restart during a
  game loses the live view; final standings are stored in `LiveGames.ResultJson` when the game ends.
- At most 8 players per game: more than 8 pawns on one tile breaks the board on the phones.
- Reconnects during a running match are not reported yet, so a player who drops and returns may show "Left".


## Question builder (2026-11-12)

Implements the "Teacher Question Experience" brief for the super-admin create flow. All routes need a platform-admin JWT;
My Questions and sets are scoped to the calling user.

| Method | Route | Body / query | Notes |
| --- | --- | --- | --- |
| GET | `/api/v1/admin/live-games/lessons` | `curriculum, grade, unit, subject?` | `[{ lesson, questionCount }]` from the library. |
| POST | `/api/v1/admin/live-games/auto-select` | `{ curriculum, grade, subject?, unit?, lesson?, count (1–30), excludeQuestionIds[] }` | Library questions balanced across question types; never generates questions. `{ questions, availableCount, warning }`; `warning` when the lesson has fewer than `count`. With `count: 1` and exclusions it is the "replace" action. |
| POST | `/api/v1/admin/live-games` | adds `questions: LiveGameQuestionItem[]` | When present, the game plays these questions in order (1–30), one round each. Each must have a correct answer; duplicates (same id, or same type + text) are rejected with the question number. Without `questions` the old filter behaviour stays. |
| GET | `/api/v1/admin/my-questions` | `curriculum?, grade?, unit?, lesson?, questionType?, search?` | Private questions, newest first (max 200). |
| POST / PUT / DELETE | `/api/v1/admin/my-questions[/{id}]` | `{ thinkingSkill?, question: QuestionDataRequest }` | Validated with the library rules; a new UUID is assigned. Never added to the shared library. Deleting does not change games/sets that copied it. |
| GET | `/api/v1/admin/live-game-sets[/{id}]` | — | List (without questions) or one set with its ordered questions. |
| POST / PUT / DELETE | `/api/v1/admin/live-game-sets[/{id}]` | `{ name, curriculum, grade, subject?, unit?, lesson?, questions }` | PUT overwrites; to keep the original, POST a new set. |

`LiveGameQuestionItem`: `{ source: "library" | "edited" | "mine", teacherQuestionId?, thinkingSkill?, question: QuestionDataRequest }`.
A `library` item whose content differs from the stored library question is saved as `edited` (a game-only copy; the shared
question is never changed). `thinkingSkill` is one of Remember, Understand, Apply, Analyze, Evaluate, Create, or null.

`LiveGameResponse` adds `questionCount` and `questions`.

Game server: `GET /game-server/live-games/{code}/questions` returns the chosen questions in order. Only unchanged library
questions keep their UUID; edited and personal questions are sent with an empty UUID, so the game server leaves their
answers out of the match report (the backend could not validate them) and the rest of the match, XP included, is saved.

Not covered: the library has no thinking-skill tags, so auto-select balances question types only. Personal questions
can carry a confirmed thinking skill, but nothing uses it in AI insights yet.
