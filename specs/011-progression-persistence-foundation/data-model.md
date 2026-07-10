# Data Model: Progression Persistence Foundation

## Enums

### MatchType

| Value | Meaning |
|-------|---------|
| Ranked | Ranked match that can affect RP/rank |
| Friendly | Non-ranked public or casual match |
| Private | Private match |

### MatchStatus

| Value | Meaning |
|-------|---------|
| Created | Match exists but has not started |
| Started | Match is in progress |
| Completed | Match ended and completion records can be written |
| Cancelled | Match will not complete |

### RankTier

Values: Student, Seeker, Challenger, Arcanist, Expert, Master, GrandMaster, Legend, Immortal.

Default for current and highest rank: Student.

### XpSourceType

Values: MatchAnswer, MatchResult, Mission.

## Existing Entity: Player

Represents the existing PlayerProfile persistence entity.

Existing fields retained:

| Field | Default | Notes |
|-------|---------|-------|
| Experience | 0 | Total XP |
| Level | 1 | Current level |
| Gold | 0 | Player currency |

New fields:

| Field | Default | Notes |
|-------|---------|-------|
| Rp | 0 | Ranked points |
| RankTier | Student | Current rank tier |
| HighestRankTier | Student | Highest achieved rank tier |
| TotalMatches | 0 | Total completed matches |
| TotalWins | 0 | Total wins |

Relationships:

- Has many MatchPlayers.
- Has many MatchQuestionResults.
- Has many MatchRewardResults.
- Has many PlayerXpLogs.
- Has many PlayerRankLogs.

Indexes:

- Rp.
- RankTier.
- Level.
- Experience.
- TotalWins.

## Match

Represents a game session record for future completion workflows.

Fields:

| Field | Required | Notes |
|-------|----------|-------|
| Id | Yes | Primary key |
| MatchCode | Yes | Match lookup code |
| MirrorRoomId | No | Optional room/session id |
| CommunityId | No | Null for B2C, community id for B2B |
| MatchType | Yes | Ranked, Friendly, Private |
| Status | Yes | Created, Started, Completed, Cancelled |
| StartedAt | Yes | Match start time |
| EndedAt | No | Match end time |
| CompletedAt | No | Completion persistence time |
| CreatedAt | Yes | Creation time |

Relationships:

- Optional Community.
- Has many MatchPlayers.
- Has many MatchQuestionResults.
- Has many MatchRewardResults.

Indexes:

- MatchCode.
- MirrorRoomId.
- CommunityId.
- Status.
- CompletedAt.

## MatchPlayer

Represents one player's aggregate participation summary in a match.

Fields:

| Field | Required | Notes |
|-------|----------|-------|
| Id | Yes | Primary key |
| MatchId | Yes | Parent match |
| PlayerProfileId | Yes | Existing Player id |
| CommunityId | No | Null for B2C, community id for B2B |
| Position | Yes | Finishing position |
| IsWinner | Yes | Winner flag |
| CorrectAnswers | Yes | Correct answer count |
| WrongAnswers | Yes | Wrong answer count |
| MaxStreak | Yes | Best streak |
| AnswerTimeTotalMs | Yes | Total answer time |
| QuestionTimeTotalMs | Yes | Total question time |
| CreatedAt | Yes | Creation time |

Relationships:

- Required Match.
- Required Player.
- Optional Community.

Constraints and indexes:

- Unique MatchId + PlayerProfileId.
- Index MatchId.
- Index PlayerProfileId.
- Index CommunityId.

## MatchQuestionResult

Represents one player's result for one question in a match.

Fields:

| Field | Required | Notes |
|-------|----------|-------|
| Id | Yes | Primary key |
| MatchId | Yes | Parent match |
| PlayerProfileId | Yes | Existing Player id |
| QuestionId | No | Nullable because some question sources may not map to a stored question |
| QuestionType | Yes | Stored question type label/value |
| IsCorrect | Yes | Answer correctness |
| AnswerTimeMs | Yes | Answer time |
| QuestionTimeMs | Yes | Question time |
| StreakBeforeAnswer | Yes | Streak before answer |
| StreakAfterAnswer | Yes | Streak after answer |
| CreatedAt | Yes | Creation time |

Relationships:

- Required Match.
- Required Player.

Indexes:

- MatchId.
- PlayerProfileId.
- QuestionId.

## MatchRewardResult

Represents one player's reward/progression result for one match.

Fields:

| Field | Required | Notes |
|-------|----------|-------|
| Id | Yes | Primary key |
| MatchId | Yes | Parent match |
| PlayerProfileId | Yes | Existing Player id |
| CommunityId | No | Null for B2C, community id for B2B |
| AnswerXp | Yes | XP from answers |
| MatchResultXp | Yes | XP from match placement/result |
| MissionXp | Yes | XP from missions, calculation out of scope |
| TotalXp | Yes | Total XP awarded |
| OldLevel | Yes | Level before reward |
| NewLevel | Yes | Level after reward |
| OldTotalXp | Yes | Total XP before reward |
| NewTotalXp | Yes | Total XP after reward |
| RpChange | Yes | RP delta |
| OldRp | Yes | RP before reward |
| NewRp | Yes | RP after reward |
| OldRankTier | Yes | Rank before reward |
| NewRankTier | Yes | Rank after reward |
| CreatedAt | Yes | Creation time |

Relationships:

- Required Match.
- Required Player.
- Optional Community.

Constraints and indexes:

- Unique MatchId + PlayerProfileId.
- Index MatchId.
- Index PlayerProfileId.
- Index CommunityId.

## PlayerXpLog

Represents one XP audit log entry.

Fields:

| Field | Required | Notes |
|-------|----------|-------|
| Id | Yes | Primary key |
| PlayerProfileId | Yes | Existing Player id |
| CommunityId | No | Null for B2C, community id for B2B |
| SourceType | Yes | MatchAnswer, MatchResult, Mission |
| SourceId | No | Optional source record id |
| BaseXp | Yes | XP before multiplier |
| Multiplier | Yes | XP multiplier |
| FinalXp | Yes | Final awarded XP |
| CreatedAt | Yes | Creation time |

Relationships:

- Required Player.
- Optional Community.

Indexes:

- PlayerProfileId.
- CommunityId.
- SourceType.
- CreatedAt.

## PlayerRankLog

Represents one RP/rank audit log entry.

Fields:

| Field | Required | Notes |
|-------|----------|-------|
| Id | Yes | Primary key |
| PlayerProfileId | Yes | Existing Player id |
| CommunityId | No | Null for B2C, community id for B2B |
| MatchId | Yes | Related match |
| OldRp | Yes | RP before change |
| RpChange | Yes | RP delta |
| NewRp | Yes | RP after change |
| OldRankTier | Yes | Rank before change |
| NewRankTier | Yes | Rank after change |
| Position | Yes | Finishing position |
| TotalPlayers | Yes | Match participant count |
| CreatedAt | Yes | Creation time |

Relationships:

- Required Player.
- Required Match.
- Optional Community.

Indexes:

- PlayerProfileId.
- CommunityId.
- MatchId.
- CreatedAt.

## Persistence Rules

- `CommunityId` is nullable anywhere B2C records are allowed.
- No progression table requires `CommunityUser`.
- No progression table requires `StudentLicense`.
- Match participant idempotency uses unique MatchId + PlayerProfileId.
- Match reward idempotency uses unique MatchId + PlayerProfileId.
- Delete behavior should avoid MySQL cascade cycles and protect audit data; use Restrict or NoAction where needed.

## State Transitions Stored, Not Calculated

This feature stores lifecycle/status/progression values but does not calculate or transition them.

Future features may write:

```text
Match Created -> Started -> Completed
Player XP/RP before match -> reward result -> Player XP/RP after match
Rank before match -> rank log -> Rank after match
```

Those transitions are not implemented here.
