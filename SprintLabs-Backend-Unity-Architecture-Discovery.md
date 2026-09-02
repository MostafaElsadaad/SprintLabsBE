# SprintLabs Backend Integration with Unity/Mirror

## Read-Only Architecture and API Discovery Report

**Discovery date:** 2026-09-01  
**Repositories inspected:**

- `K:\Projects\DotNet\SprintLabsbkp`
- `K:\Projects\Unity\U_NOXED_SprintLab`

No files were modified during discovery. Both repositories were clean after inspection.

## 1. CURRENT BACKEND PLAYER MODEL

The backend separates authentication identity from gameplay profile:

- `User` is the ASP.NET Identity account. It owns `UserId`, Google/Firebase identity, email, name, avatar, status, platform-admin status, and teacher-account state.
- `Player` is the gameplay profile. It owns `PlayerProfileId`, gameplay-facing name/avatar, demographics, and persistent progression.

The persisted player fields are:

| Field | Intended authority | Current writer |
|---|---|---|
| `Name`, `AvatarUrl`, `Age`, `Grade`, `SchoolName` | Backend | Authentication/profile-update workflows |
| `Gold` | Backend | Defaulted to zero; no reward writer found |
| `Experience`, `Level` | Backend | Defaulted; no production progression writer found |
| `Rp`, `RankTier`, `HighestRankTier` | Backend | Defaulted; no production progression writer found |
| `TotalMatches`, `TotalWins` | Backend | Defaulted; never updated in production code |

Sources: [Player.cs](K:/Projects/DotNet/SprintLabsbkp/Domain/Models/Player.cs), [User.cs](K:/Projects/DotNet/SprintLabsbkp/Infrastructure/DataAccess/User.cs), [ApplicationDbContext.cs](K:/Projects/DotNet/SprintLabsbkp/Infrastructure/DataAccess/ApplicationDbContext.cs#L53).

The match/progression persistence foundation already exists:

- `Match`: match code, Mirror room ID, community, match type/status, and lifecycle timestamps.
- `MatchPlayer`: one human player per match, position, winner flag, correct/wrong counts, streak, and aggregate timing.
- `MatchQuestionResult`: player/question/correctness/timing/streak.
- `MatchRewardResult`: XP and RP calculation result snapshots.
- `PlayerXpLog`: XP ledger by source.
- `PlayerRankLog`: RP/rank ledger by match.

Important limitations:

- `MatchPlayer` has no score.
- It requires a `PlayerProfileId`; it cannot represent a pure bot.
- It has no gameplay slot/participant identifier or controlling-actor history.
- `MatchQuestionResult` has no selected answer, `AnswerId`, attempt ID, actor type, topic/unit/lesson, or source/session type.
- `MatchQuestionResult` has no unique idempotency constraint.
- `Match.MatchCode` and `MirrorRoomId` are indexed but not unique.
- `PlayerRankLog` and `PlayerXpLog` lack per-match/player uniqueness constraints.

Sources: [Match.cs](K:/Projects/DotNet/SprintLabsbkp/Domain/Models/Match.cs), [MatchPlayer.cs](K:/Projects/DotNet/SprintLabsbkp/Domain/Models/MatchPlayer.cs), [MatchQuestionResult.cs](K:/Projects/DotNet/SprintLabsbkp/Domain/Models/MatchQuestionResult.cs), [MatchRewardResult.cs](K:/Projects/DotNet/SprintLabsbkp/Domain/Models/MatchRewardResult.cs).

These tables were introduced by the progression migration and are active EF `DbSet`s, but there is no production command, handler, repository flow, or controller that writes match-completion data.

## 2. CURRENT BACKEND APIs

All routes below were verified from source and use the `BaseResponse<T>` envelope: `data`, `message`, `statusCode`, and `errorCode`.

### Authentication

#### `POST /api/v1/Account/firebase-login`

Request:

```json
{
  "idToken": "firebase-id-token"
}
```

Response data:

```json
{
  "accessToken": "...",
  "userId": 123,
  "playerProfileId": 456,
  "name": "...",
  "email": "...",
  "pictureUrl": "...",
  "gold": 0,
  "experience": 0,
  "level": 1
}
```

#### `POST /api/v1/Account/google-login?googleAccessToken=...`

Returns the same `LoginResponse`.

The login response does **not** contain:

- `Rp`
- `RankTier` or displayable rank
- `HighestRankTier`
- `TotalMatches`
- `TotalWins`
- age, grade, or school

It also fills name and picture from the current external-provider login context, while progression comes from `Player`. Therefore login name/avatar can diverge from the persisted `Player.Name`/`AvatarUrl` after a player edits their profile.

Sources: [AccountController.cs](K:/Projects/DotNet/SprintLabsbkp/API/Controllers/AccountController.cs#L41), [LoginResponse.cs](K:/Projects/DotNet/SprintLabsbkp/Shared/Responses/LoginResponse.cs), [ExternalPlayerLoginWorkflow.cs](K:/Projects/DotNet/SprintLabsbkp/Application/Features/Accounts/Common/ExternalPlayerLoginWorkflow.cs#L27).

### Current identity

#### `GET /api/v1/Users/me`

Authentication: player bearer JWT.

Response data:

```json
{
  "userId": 123,
  "email": "...",
  "name": "...",
  "avatarUrl": "...",
  "status": "Active",
  "isPlatformAdmin": false,
  "playerProfileId": 456
}
```

This is the endpoint the dedicated Mirror server already calls to validate each player's token. It returns identity only, not progression.

Sources: [UsersController.cs](K:/Projects/DotNet/SprintLabsbkp/API/Controllers/UsersController.cs#L32), [CurrentUserResponse.cs](K:/Projects/DotNet/SprintLabsbkp/Application/Features/Users/GetCurrentUser/CurrentUserResponse.cs), [SprintLabsBackendCredentialValidator.cs](K:/Projects/Unity/U_NOXED_SprintLab/Assets/_Project/Code/NetworkBackendValidation/SprintLabsBackendCredentialValidator.cs#L12).

### Current player profile

#### `GET /api/v1/Users/me/player-profile`

Authentication: player bearer JWT.

Response data:

```json
{
  "id": 456,
  "name": "...",
  "email": "...",
  "pictureUrl": "...",
  "gold": 0,
  "experience": 0,
  "level": 1,
  "schoolName": "...",
  "age": 10,
  "grade": 5
}
```

It omits RP, rank tier, highest rank, totals, level threshold, and XP progress within the current level.

This is the correct existing endpoint to reuse and extend for profile refresh.

### Profile writes

- `PUT /api/v1/Account/profile`: updates `Name`, `SchoolName`, `Grade`, and `Age`.
- `PATCH /api/v1/player-profiles/me`: updates only `Age`, `Grade`, and `SchoolName`, with explicit omitted/null semantics.

These overlap. Neither updates avatar, progression, RP, rank, gold, or match history.

### Questions

#### `GET /api/v1/Question?grade={grade}&assignment={optional}`

Public endpoint returning:

- question-pack record ID
- grade
- assignment
- `payloadJson`
- version and timestamps

#### `PUT /api/v1/Question`

Authenticated bulk upsert of a complete JSON question pack.

Questions are stored as one MySQL JSON-text blob per grade. The backend only validates that `payloadJson.Questions` is a non-empty array; it does not enforce stable question IDs or answer IDs.

Sources: [QuestionController.cs](K:/Projects/DotNet/SprintLabsbkp/API/Controllers/QuestionController.cs), [QuestionsJson.cs](K:/Projects/DotNet/SprintLabsbkp/Domain/Models/QuestionsJson.cs).

### Missing API exposure

There is no controller or endpoint for:

- match creation/completion/history
- match participants/results
- question attempts/history
- XP/RP logs
- progression processing
- mission progress or events
- achievements

## 3. CURRENT PROGRESSION IMPLEMENTATION

The domain calculations already exist and should be reused.

### XP

Correct-answer XP is streak based:

- first correct: 10
- streak 2–3: 12 each
- streak 4–5: 14 each
- streak 6–7: 18 each
- streak 8–9: 22 each
- streak 10+: 30 each
- an incorrect answer resets the streak

Match-result XP:

- winner: 50
- participant: 20

Mission XP:

- Normal: 25
- Mid: 50
- Hard: 100

Source: [XpCalculationService.cs](K:/Projects/DotNet/SprintLabsbkp/Infrastructure/Services/XpCalculationService.cs).

### Level

For each level:

```text
required XP = ceil(150 × level^1.25)
```

Level is recomputed from cumulative total XP. Unlock calculation currently always returns an empty list.

Source: [LevelProgressionService.cs](K:/Projects/DotNet/SprintLabsbkp/Infrastructure/Services/LevelProgressionService.cs).

### RP and rank

Only ranked matches alter RP.

```text
percentile = (totalPlayers - position) / (totalPlayers - 1)
base RP = (percentile - 0.5) × 60
```

Positive RP uses the base value. Negative RP is multiplied by the current rank's loss multiplier:

- Student: 0
- Seeker: 0.25
- Challenger: 0.5
- Arcanist: 0.75
- Expert: 1
- Master: 1.25
- GrandMaster: 1.5
- Legend: 2

Rank thresholds:

- Student: `<100`
- Seeker: `<500`
- Challenger: `<1,000`
- Arcanist: `<2,000`
- Expert: `<3,500`
- Master: `<5,500`
- GrandMaster: `<8,000`
- Legend: `8,000+`

`Immortal` exists in the enum, but eligibility always returns false and no calculation produces it.

Source: [RpRankCalculationService.cs](K:/Projects/DotNet/SprintLabsbkp/Infrastructure/Services/RpRankCalculationService.cs).

### What currently affects progression

- Correctness and answer order affect answer XP.
- Winner status affects match XP.
- Placement affects RP in ranked matches.
- Score does not directly affect backend XP or RP.
- Wins/losses have persisted fields but are not updated.
- `MatchRewardResult`, `PlayerXpLog`, and `PlayerRankLog` are not actively written.

The backend therefore has calculation primitives, but it does **not** yet have enough orchestration to process a completed match. Missing pieces are validation, persistence workflow, transactions, idempotency, concurrency handling, match/player updates, logs, and API exposure.

## 4. CURRENT QUESTION-HISTORY IMPLEMENTATION

There is no active canonical question-history system today.

MySQL contains the intended `MatchQuestionResult` foundation:

- `MatchId`
- `PlayerProfileId`
- nullable `QuestionId`
- `QuestionType`
- `IsCorrect`
- `AnswerTimeMs`
- `QuestionTimeMs`
- streak before/after
- `CreatedAt`

It lacks:

- selected answer or `AnswerId`
- stable attempt ID
- human/bot actor type
- gameplay slot
- unit/lesson/topic
- source beyond the match relationship
- uniqueness/idempotency protection

No production code writes it.

No MongoDB, Cosmos DB, Redis history store, recommendation API, analysis API, or data-team question-history integration was found. The only outbound backend HTTP integration is an OpenAPI-document synchronization service, unrelated to student analytics. Community student analytics are currently returned as an empty/default object.

Based on the current architecture, SprintLabs/MySQL is the intended owner because the existing history model is attached directly to `Match` and `Player`. However, there is currently no canonical operational history. A future analytics/data-team service should consume or receive replicated events from this canonical transaction, not independently decide attribution.

## 5. CURRENT UNITY PROFILE FLOW

```text
Google → Firebase → POST firebase-login
       → BackendAuthenticationResult
       → AuthenticationSession (DontDestroyOnLoad)
       → player JWT used by Mirror authentication
       → GET /Users/me on dedicated server
       → trusted UserId + PlayerProfileId
```

Unity parses `Gold`, `Experience`, and `Level` from login, but `AuthenticationSession` only retains:

- access token
- `UserId`
- `PlayerProfileId`
- name
- email
- picture URL

Gold, XP, and level are discarded after parsing. RP and rank are not parsed at all.

Sources: [BackendAuthenticationResult.cs](K:/Projects/Unity/U_NOXED_SprintLab/Assets/_Project/Code/Authentication/Models/BackendAuthenticationResult.cs), [AuthenticationSession.cs](K:/Projects/Unity/U_NOXED_SprintLab/Assets/_Project/Code/Authentication/AuthenticationSession.cs).

Additional findings:

- Login UI displays session name/email/avatar.
- No reusable full profile/session object exists beyond `AuthenticationSession`.
- `LobbyPlayer` declares `PlayerName`, `PlayerLevel`, and `PlayerXP`, but nothing assigns them.
- Lobby roster names are currently network IDs converted to strings.
- Gameplay prefab `PlayerControl.PlayerName` is serialized as `"TestName"`.
- Both playable network prefabs retain that default.
- Gameplay avatar/name UI and results leaderboard read `PlayerControl.PlayerName`/`PlayerAvatar`, not backend profile data.
- Results UI displays `XP = "+" + PlayerScore`; this is presentation-only and does not match backend XP rules.
- Main-menu profile/rank/mission/leaderboard panels are primarily UI toggles without backend profile binding.

There is also a type risk: backend IDs are `long`, while login parsing and `AuthenticationSession` use `int`. Mirror's validated identity model uses `long`. Unity's login/session DTOs should eventually be aligned to 64-bit IDs.

## 6. CURRENT MATCH DATA FLOW

```text
Matchmaker creates server MatchSession and IDs
→ authenticated humans/bots become MatchMembers
→ additive server scene loads
→ lobby identities are replaced by gameplay identities
→ MultiplayerGameManager builds Players/Bots dictionaries
→ server selects questions
→ clients submit answers via Commands
→ server evaluates correctness
→ client presentation invokes further Commands for score/stat effects
→ server sorts connected humans for results
→ MatchRoom asks Matchmaker to finish
→ clients return to lobby
→ session and scene are cleaned up
```

Sources: [MatchSession.cs](K:/Projects/Unity/U_NOXED_SprintLab/Assets/_Project/Code/Network/Matchmaking/MatchSession.cs), [Matchmaker.cs](K:/Projects/Unity/U_NOXED_SprintLab/Assets/_Project/Code/Network/Matchmaker.cs), [MultiPlayerGameManager.cs](K:/Projects/Unity/U_NOXED_SprintLab/Assets/_Project/Code/Gameplay/MultiPlayerGameManager.cs).

### Current knowledge and trust

| Data | Available today | Trust assessment |
|---|---|---|
| Match ID/GUID, match kind | Yes, generated by dedicated server | Authoritative |
| User/Profile ID | Yes for authenticated human membership | Authoritative backend-validated identity |
| Display name | No canonical name in Mirror/gameplay | Default/network-ID placeholders |
| Human vs bot membership | Yes | Server authoritative |
| Current controller during temporary reconnect window | Only indirectly through gameplay `IsBot` | Insufficient as a durable attribution model |
| Questions shown | Server selects from pack | Server-known, but no stable backend question ID |
| Selected answer | Received from client Command | Client input; not retained |
| Correctness | Evaluated against server-held question | Server-computed, but Command timing/replay is not hardened |
| Score | Stored on server | Not safely authoritative: client can invoke `AddScoreCommand` independently |
| Win/round completion | Stored on server | Client invokes `SendPlayerWonCommand`; insufficiently hardened |
| Placement | Server sorts results | Derived from score/win/movement inputs that are not fully trusted |
| Dice values | Rolled on server inside Command | Value is server-generated; action eligibility is not fully hardened |
| Steps | Incremented server-side from dice result | More trustworthy, but no persistent ledger |
| Monster kills | Incremented by client Command after client presentation | Not authoritative |
| Power-up use/taken | Raw strings through client Commands | Not authoritative |
| Disconnect/reconnect/substitution | Yes | Server authoritative |
| Answer time | No | Not measured or stored |
| Match result payload | No | Nothing is sent to backend |

`SetAndSavePlayersScores()` only sorts and displays connected human players; it does not save anything. Bots and currently disconnected/substituted players are excluded from that result list.

## 7. BOT-SUBSTITUTION ANALYSIS

The current model cannot safely produce educational history for the example Q1/Q2 → bot Q3/Q4 → human Q5.

During a disconnect:

1. `MatchSession` retains the authenticated human membership for the reconnect window.
2. `MultiplayerGameManager` immediately moves the same gameplay identity and `PlayerControl` from `Players` to `Bots`.
3. The bot continues using that same `PlayerControl`, score, and aggregate question counters.
4. On reconnect, the same identity is rebound and `IsBot` is reset to false.
5. If the reconnect window expires, `MatchSession` creates a new bot member with empty identity fields, but the reused `NetworkPlayerData` component still contains the former human IDs.

Consequences:

- Human and bot answers accumulate in the same `RightQuestions_Stats`, `WrongQuestions_Stats`, score, and gameplay slot.
- No per-attempt actor is recorded.
- During the temporary reconnect window, authoritative membership still says “Human” while gameplay control says “Bot”.
- Looking only at `PlayerProfileId` or the gameplay identity would incorrectly attribute bot answers to the human.

The minimum required change, without redesigning gameplay, is a server-side attempt ledger that records at answer time:

- stable match/slot ID
- attempt ID
- controlling actor kind: `Human` or `Bot`
- nullable human `PlayerProfileId`
- selected answer and correctness
- question identity and timing

A bot-controlled attempt must have `ActorKind=Bot` and `PlayerProfileId=null`, even when the slot was originally owned by a human. Reconnection switches subsequent attempts back to the authenticated human. Aggregate counters cannot be used for educational attribution.

## 8. GAP ANALYSIS

### Missing backend pieces

- match-completion application feature/controller
- service authentication for dedicated servers
- match idempotency key/unique constraint
- transactional orchestration of all players
- score persistence
- bot/slot/controller representation
- answer/attempt IDs and selected answers
- active writes to progression fields and ledgers
- concurrency control for simultaneous player updates
- profile response fields for RP/rank
- match/history read APIs, if product UI later requires them
- mission models and mission-progress logic
- data-team/analytics delivery mechanism

### Missing Unity pieces

- canonical profile model persisted across scenes
- profile refresh API client
- backend name/avatar propagation into lobby/gameplay player data
- stable question/answer IDs
- authoritative per-attempt records
- server-authoritative score/win/stat mutation
- completed-match payload construction
- durable retry/spool behavior
- explicit controller actor state for substitution

## 9. PROPOSED APIs

### A. Reuse and extend profile retrieval

#### `GET /api/v1/Users/me/player-profile`

Caller: Unity client  
Authentication: existing player bearer JWT  
Authorization: token may read only its linked profile

Extend the existing response rather than create a new endpoint:

```json
{
  "id": 456,
  "name": "Player",
  "email": "...",
  "pictureUrl": "...",
  "gold": 100,
  "experience": 2450,
  "level": 8,
  "rp": 720,
  "rankTier": "Challenger",
  "highestRankTier": "Challenger",
  "totalMatches": 14,
  "totalWins": 5,
  "nextLevelXpRequirement": 2019,
  "schoolName": "...",
  "age": 10,
  "grade": 5
}
```

Database effects: none.  
Idempotency: not applicable.

The login response may also expose these fields for convenience, but the refresh endpoint remains necessary after a match.

### B. New authoritative match-completion API

#### `POST /api/v1/Matches/complete`

Caller: dedicated Mirror server only  
Authentication: dedicated game-server credential, never a player JWT  
Authorization: `game-server` scope/policy; may submit only server-owned match results

Minimum request shape:

```json
{
  "schemaVersion": 1,
  "matchId": "mirror-generated-stable-id",
  "matchType": "Ranked",
  "startedAt": "...",
  "endedAt": "...",
  "slots": [
    {
      "slotId": "stable-slot-id",
      "initialActorKind": "Human",
      "playerProfileId": 456,
      "userId": 123,
      "finalControllerKind": "Bot",
      "disconnected": true,
      "reconnected": false,
      "botSubstituted": true,
      "score": 300,
      "placement": 2,
      "isWinner": false
    }
  ],
  "questionAttempts": [
    {
      "attemptId": "match-and-attempt-unique-id",
      "slotId": "stable-slot-id",
      "actorKind": "Human",
      "playerProfileId": 456,
      "questionId": 901,
      "answerId": 4,
      "selectedAnswerJson": null,
      "questionType": "MCQ",
      "isCorrect": true,
      "answeredAt": "...",
      "responseTimeMs": 4250,
      "questionTimeMs": 15000
    }
  ]
}
```

For composite answers, `selectedAnswerJson` can carry ordering, matching, drag/drop, or fill-blank content; `AnswerId` is useful where the question type has stable choices.

Response:

```json
{
  "matchId": "...",
  "status": "Processed",
  "completedAt": "...",
  "players": [
    {
      "playerProfileId": 456,
      "answerXp": 34,
      "matchResultXp": 20,
      "missionXp": 0,
      "totalXpGained": 54,
      "oldExperience": 2450,
      "newExperience": 2504,
      "oldLevel": 8,
      "newLevel": 8,
      "rpChange": 9,
      "oldRp": 720,
      "newRp": 729,
      "oldRankTier": "Challenger",
      "newRankTier": "Challenger"
    }
  ]
}
```

Database effects, in one transaction:

- insert/complete `Match`
- insert human `MatchPlayer` facts
- insert eligible human `MatchQuestionResult` attempts
- calculate XP using the existing service
- calculate level using the existing service
- calculate RP/rank using the existing service
- update `Player`
- increment match/win totals under the agreed policy
- insert `MatchRewardResult`
- insert `PlayerXpLog`
- insert ranked `PlayerRankLog`

Bots must never create player progression or educational-history rows.

### C. Question attempts

For the initial integration, include attempts in the match-completion request.

Reasons:

- the existing XP algorithm depends on ordered correctness
- one transaction prevents progression/history divergence
- match payloads should remain modest
- one idempotency boundary is simpler than coordinating two APIs
- the backend can later publish committed attempts to analytics

A separate incremental question-attempt batch endpoint is justified only if product requirements demand surviving dedicated-server crashes before match completion. It should not be introduced merely for theoretical future analytics.

## 10. MATCH COMPLETION DESIGN

Use one bulk request per match, not per-player requests.

Why:

- `Match`, `MatchPlayer`, rewards, question results, and rank logs are naturally one aggregate.
- Placement and player count are match-wide facts.
- RP calculation depends on total players.
- One request permits all-or-nothing persistence.
- Per-player calls could leave half a match committed and complicate duplicate handling.

Transaction boundary: the complete match aggregate and all affected player progression rows.

### Authentication

No trusted service-to-service/game-server authentication exists today. The current JWT scheme represents users and has a single audience.

The smallest compatible secure mechanism is a dedicated game-server authentication scheme using a separately managed and rotatable server credential, accepted only by a `game-server` authorization policy. It should:

- be stored only in the dedicated-server secret environment
- require HTTPS
- be distinct from player JWT credentials
- be scoped only to authoritative server endpoints
- support rotation and revocation

The Mirror server must not impersonate any participant.

### Idempotency

- `matchId` is the natural idempotency key.
- Add a unique backend constraint for the Mirror match ID.
- Persist a normalized payload hash.
- Same match ID plus same hash: return the stored completion response with `AlreadyProcessed`; grant nothing again.
- Same match ID plus different hash: return `409 Conflict` for investigation.
- Unique per-attempt IDs add a secondary safeguard.

### Retry and failure behavior

- The dedicated server should serialize the final payload to a durable local spool before its first request.
- Retry timeouts, network failures, and 5xx responses with exponential backoff.
- Do not retry validation/authorization 4xx errors except credential refresh/rotation cases.
- Do not destroy the local spool until the backend confirms `Processed` or `AlreadyProcessed`.
- A duplicate completion must return the original result and must not grant XP, RP, level, rank, gold, or wins again.

Disconnected players are retained with explicit disconnect/reconnect/substitution facts. Whether they receive participation, placement, win, or RP rewards is a product rule the backend must apply—not something inferred by Unity.

Bots are represented as slots/controllers for match integrity but have no `PlayerProfileId` and receive no rewards/history.

## 11. QUESTION HISTORY DESIGN

Attempt attribution must use the controlling actor at the instant of answering.

| Question | Slot | Actor | PlayerProfileId | Persist as student history? |
|---|---|---|---:|---|
| Q1 | slot A | Human | 7 | Yes |
| Q2 | slot A | Human | 7 | Yes |
| Q3 | slot A | Bot | null | No |
| Q4 | slot A | Bot | null | No |
| Q5 | slot A | Human after reconnect | 7 | Yes |

The slot and actor are separate concepts. Never infer the actor from the slot's original owner, retained `NetworkPlayerData`, final controller, or aggregate counters.

The backend should validate that every human attempt references a human participant in the same submitted match. Bot attempts may be retained as match telemetry if useful, but must not be inserted into student educational history or XP input.

Stable `QuestionId` and answer identifiers need to be introduced into the question pack before reliable history can be submitted. The current `questionNumber` is a display/sequence value, not a canonical question ID.

## 12. OPTIONAL FUTURE MISSION TELEMETRY

Do not include mission telemetry in the first integration.

Although Unity tracks rolls, steps, monster defeats, and power-up strings, several counters are client-Command-driven and are not yet safe for persistent mission credit. The backend also has no mission entity or mission-progress system.

A later extension can introduce a typed, versioned game-event batch:

```text
DiceRolled
MovementCompleted
MonsterDefeated
PowerUpAcquired
PowerUpUsed
PlayerDisconnected
PlayerReconnected
BotControlStarted
BotControlEnded
```

Each event should have match ID, event ID, slot, actor, server timestamp, and typed payload. It should be added only after authoritative event generation and concrete mission requirements exist.

## 13. IMPLEMENTATION PHASES

1. Resolve product rules: profile-name authority, disconnected-player rewards, bot effects on placement/RP, stable question IDs, and gold policy.
2. Extend the existing profile response and Unity persistent session/profile model.
3. Introduce game-server authentication and match idempotency.
4. Add backend match-completion orchestration using existing calculation services.
5. Add Unity server-side attempt/actor recording and completed-match payload construction.
6. Harden score, winner, and educational event generation before granting progression.
7. Add durable retry/spooling and end-to-end duplicate tests.
8. Add analytics/data-team replication after the canonical transaction works.
9. Consider mission events later.

## 14. RISKS / OPEN PRODUCT DECISIONS

- Is `Player.Name` or external-provider `User.Name` the canonical display name?
- Should leaving/disconnected players receive participation XP?
- Does a disconnected human inherit final slot placement/win after bot control?
- Do bots count in ranked `totalPlayers` and percentile calculations?
- Should ranked games containing bots grant RP at all?
- Is score purely presentation, or should it affect backend rewards later?
- What does `Gold` reward, if anything?
- How are question and answer IDs assigned inside JSON packs?
- Should free-text selected answers be retained, considering student privacy?
- Is match-end-only durability acceptable, or must attempts survive a dedicated-server crash?
- Will a data-team service consume backend events or require synchronous delivery?
- Current `int` Unity IDs must be upgraded before backend `long` IDs exceed 32-bit range.
- Current question download uses plain HTTP, unlike authentication; this risks question-pack tampering in transit.
- Rank enum includes Immortal, but current logic cannot award it.

## 15. FINAL RECOMMENDATION

Reuse the existing player-profile endpoint and all three progression calculation services. Add exactly one initial server-write API: a bulk, idempotent, service-authenticated match-completion endpoint containing match facts and per-attempt actor attribution.

Before that endpoint can safely grant progression, Unity must produce server-authoritative score/result facts and separate gameplay slot ownership from the current controlling actor. Bot attempts must carry no human profile identity.

| API | Exists Today? | Needs Change? | Caller | Purpose | Priority |
|---|---:|---:|---|---|---|
| `POST /api/v1/Account/firebase-login` | Yes | Optional response expansion | Unity client | Login and initial identity/profile snapshot | Existing |
| `GET /api/v1/Users/me` | Yes | No for current purpose | Mirror server / Unity | Validate player token and identity | Existing |
| `GET /api/v1/Users/me/player-profile` | Yes | Yes: add RP/rank/totals/progress fields | Unity client | Canonical profile refresh | P0 |
| `PUT /api/v1/Account/profile` | Yes | Consolidation decision later | Unity/client UI | Name/demographic edits | Existing |
| `GET /api/v1/Question` | Yes | Question payload needs stable IDs | Unity/dedicated server | Retrieve question pack | P0 prerequisite |
| `POST /api/v1/Matches/complete` | No | New | Dedicated Mirror server | Atomic match facts, attempts, and progression | P0 |
| Question-attempt batch | No | Not initially | Dedicated Mirror server | Crash-resilient incremental attempts | P2/conditional |
| Mission-event batch | No | Future only | Dedicated Mirror server | Typed mission telemetry | Future |
