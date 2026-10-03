# Progression API contract

All routes below use `/api/v1`. Success is the existing BaseResponse envelope:
`{ "data": ..., "message": "Success", "statusCode": 200, "errorCode": 1 }`.
Error envelopes follow existing GenericException conventions. Check HTTP status; do not depend on numeric enum serialization for errorCode.

## Authentication

Read routes: normal SprintLabs `Authorization: Bearer <access token>`. Caller identity comes from the authenticated `userId` claim. UserId query/body parameters do not select the caller. Suspended callers are forbidden.

Write routes: `Authorization: GameServer <server credential>`. Configure a random credential of 32–512 characters via the API process environment `GameServer__ApiKey` or the deployment secret store. Give it only to the dedicated Mirror server via its own server-only secret mechanism. Never ship it in an APK, scene, source, ScriptableObject, browser app, URL, logs, or committed config. Missing/weak configuration fails closed. Ordinary user/admin JWTs do not authorize writes. Use HTTPS and redact Authorization in proxy/application request logging.

## Register a match (registration prerequisite)

`POST /Matches` — game-server credential only. Body (human IDs are Int64):

```json
{
  "matchCode": "a-server-generated-uuid",
  "mirrorRoomId": "room-123",
  "matchType": "Ranked",
  "communityId": null,
  "startedAt": "2026-10-03T12:00:00Z",
  "totalPlayers": 2,
  "playerProfileIds": [101, 102]
}
```

`matchType`: Ranked/Friendly/Private (names, case insensitive). MatchCode required, maximum 64 characters; room maximum 128. TotalPlayers 1–128 and at least the human count; Ranked requires at least 2 slots. All human profiles must exist and belong to active backend users. School matches require active licensed humans in active classes of the selected active community. UTC start cannot be over five minutes in the future.

Data response: `{ "matchId": 1, "status": "Started" }`.
Same MatchCode with identical registration returns the same backend ID/current status. Changed registration returns 409. Preserve the original start timestamp on retries; backend normalizes its precision for MySQL. Registration is limited to 64 KiB.

## Complete a match (NOX-89)

`POST /Matches/{matchId}/complete` — game-server credential only. One bulk body per match:

```json
{
  "matchType": "Ranked",
  "communityId": null,
  "mirrorRoomId": "room-123",
  "endedAt": "2026-10-03T12:10:00Z",
  "players": [
    { "playerProfileId": 101, "position": 1, "correctAnswers": 1, "wrongAnswers": 0,
      "maxStreak": 1, "answerTimeTotalMs": 4000, "questionTimeTotalMs": 10000 },
    { "playerProfileId": 102, "position": 2, "correctAnswers": 0, "wrongAnswers": 0,
      "maxStreak": 0, "answerTimeTotalMs": 0, "questionTimeTotalMs": 0 }
  ],
  "questionResults": [
    { "playerProfileId": 101, "questionId": null, "questionType": "MCQ", "isCorrect": true,
      "answerTimeMs": 4000, "questionTimeMs": 10000, "streakBeforeAnswer": 0, "streakAfterAnswer": 1 }
  ]
}
```

Players must exactly match the registered human roster. Positions are unique in 1..registered TotalPlayers; bot positions can cause human gaps. Include only genuine human-attributed answers, in chronological order per human. Include false results for wrong/unanswered questions. Streak starts at zero and resets on false. Timing is nonnegative; questionTimeMs > 0; answerTimeMs <= questionTimeMs. QuestionId is null or positive, type nonblank/max 64. Aggregates must equal the answer details. EndedAt is UTC, not before start, and at most five minutes into the future. Maximum 1000 answers/human, 12800 total, and 4 MiB body.

The server supplies validated facts (position, correctness, timing); backend derives persisted XP/RP/level facts. There are no requested reward/mission XP fields.

Data response:

```json
{
  "matchId": 1,
  "status": "Completed",
  "rewards": [
    { "playerProfileId": 101, "answerXp": 10, "matchResultXp": 50, "missionXp": 0,
      "xpGained": 60, "oldLevel": 1, "newLevel": 1, "oldTotalXp": 0, "newTotalXp": 60,
      "rpChange": 30, "oldRp": 0, "newRp": 30, "oldRankTier": "Student", "newRankTier": "Student" },
    { "playerProfileId": 102, "answerXp": 0, "matchResultXp": 20, "missionXp": 0,
      "xpGained": 20, "oldLevel": 1, "newLevel": 1, "oldTotalXp": 0, "newTotalXp": 20,
      "rpChange": 0, "oldRp": 0, "newRp": 0, "oldRankTier": "Student", "newRankTier": "Student" }
  ]
}
```

Rewards are returned by ascending human profile ID. Retry the same match ID after timeout/503 with bounded backoff. Completed requests return original persisted snapshots, even after another match changes the profile or a retry body differs. No additional profile updates/logs occur. A 400 is a fact/contract error; 404 a missing match; 409 a cancelled/not-started match or progression overflow; 503 a retryable database failure. Mission XP is zero. Friendly/Private return RP delta zero and preserve rank. Immortal remains a placeholder.

## Progression and rank reads (NOX-90)

| Method | Route | Permission | Body | Data |
|---|---|---|---|---|
| GET | `/Progression/me` | Current player | None | PlayerProgressionResponse |
| GET | `/Progression/players/{playerProfileId}` | Self, platform admin, same-community owner or assigned teacher | None | PlayerProgressionResponse |
| GET | `/Ranking/me` | Current player | None | PlayerRankingResponse |
| GET | `/Ranking/players/{playerProfileId}` | Same rules as player progression | None | PlayerRankingResponse |

Progression fields: playerProfileId, experience (lifetime XP), level, gold, rp, rankTier, highestRankTier, totalMatches, totalWins, xpIntoCurrentLevel, nextLevelXpRequirement. Level/current-level XP are derived from cumulative experience. Rank fields: playerProfileId, rp, rankTier, highestRankTier. Rank names serialize as strings (`GrandMaster`, `Legend`, etc.). Unauthorized target IDs return 404 to avoid profile enumeration; suspended caller 403; no profile 404.

## Leaderboards (NOX-91)

`GET /Ranking/leaderboard?page=1&pageSize=20` — authenticated active user.

`GET /Communities/{communityId}/ranking/leaderboard?page=1&pageSize=20&gradeId=...&classId=...` — platform admin or community owner/teacher. Teachers see their assigned active classes only. Community filters use grade/class IDs belonging to that community; both filters intersect. Global grade/class filters return 400.

Data: `{ "items": [ { "position": 1, "playerProfileId": 101, "name": "Player", "avatarUrl": null, "rp": 480, "rankTier": "Seeker", "level": 2, "totalWins": 1 } ], "page": 1, "pageSize": 20, "total": 1 }`.

Sort: RP descending, profile ID ascending. Position is ordinal within the filtered leaderboard, including previous pages; ties remain stable. Suspended/unlinked users are excluded before counting/paging. Community leaderboards require active enrollment and active classes. PageSize 1–100; large offsets that exceed Int32 return 400. Empty/out-of-range valid page returns empty items with total. Unauthorized community 403, unavailable community or foreign filters 404.

## History and details (NOX-92)

| Method | Route | Permission | Body | Data |
|---|---|---|---|---|
| GET | `/Matches/me/history?page=1&pageSize=20` | Current player | None | Paged MatchHistoryResponse |
| GET | `/Matches/{matchId}` | Participant, platform admin, community owner or assigned teacher | None | MatchDetailResponse |
| GET | `/Communities/{communityId}/matches?page=1&pageSize=20` | Platform admin/community owner/teacher | None | Paged MatchHistoryResponse |
| GET | `/Communities/{communityId}/players/{playerProfileId}/matches?page=1&pageSize=20` | Same scope, authorized player | None | Paged MatchHistoryResponse |

History uses completed matches only, newest completion time then match ID descending. Grade/class query fields are rejected for history. History row: matchId, matchCode, matchType, status, communityId, startedAt, endedAt, completedAt, totalPlayers, position, reward. Position/reward are populated for self/specific-player history; community-wide rows have null position/reward. Times are UTC.

Details: `{ "match": <history row>, "players": [<participant rows>], "questionResults": [<answer facts>] }`.
Participant rows: playerProfileId, position, isWinner, correctAnswers, wrongAnswers, maxStreak, answerTimeTotalMs, questionTimeTotalMs, reward. Clients see all human placements but other humans' statistics/rewards are null; only the caller's question results are returned. Teachers see only assigned permitted humans. Owners/admins see all authorized match humans. Answer facts use the completion schema; order is chronological per human. No user email, credentials, raw answers, or token data is returned.

Owners retain school-owned historical records after enrollment revocation/class archive; teacher access depends on current assigned active classes (historical class snapshots are deferred). Unauthorized/missing match 404 or forbidden community 403. Valid empty history returns items=[] and total=0. Apply the same page bounds as leaderboards.
