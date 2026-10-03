# Missions API
All paths use `/api/v1`. Responses use the existing `BaseResponse<T>` with numeric Int64 identities. Enum inputs are named strings, case-insensitive; outputs use canonical names. Dates must include `Z` (UTC).

| Method/path | Authentication | Request | Success data |
| --- | --- | --- | --- |
| GET /Missions/me | Player JWT, active user/profile | None; caller identity comes from claims | Array of current PlayerMissionResponse; assigns once per active period, settles expired missions |
| POST /Missions/{playerMissionId}/claim | Owning active player JWT | No body | Array containing one MissionClaimResponse; retry returns original claim snapshot |
| POST /Missions/claim-all | Active player JWT | No body | Array of completed mission claims; empty if none |
| POST /game-server/missions/events | GameServer credential | MissionEventRequest | Array containing one MissionEventResponse |
| POST /game-server/missions/events/bulk | GameServer credential | Array of 1–100 MissionEventRequest | Array in request order, all-or-nothing transaction |
| GET /admin/missions/templates | Active platform admin JWT | None | First 100 templates ordered by ID, `{missionTemplateId,isActive,definition}` |
| POST /admin/missions/templates | Active platform admin JWT | MissionTemplateRequest, below | Created numeric template ID |
| POST /admin/missions/activations | Active platform admin JWT | MissionActivationRequest, below | Created numeric activation ID |
| POST /admin/missions/reset/run | Active platform admin JWT | No body | `{endedActivations,expiredMissions,autoClaimedMissions}` |
| POST /admin/missions/seed | Active platform admin JWT | No body | Three demo activation IDs; repeat-safe per weekly/monthly period and open activation |

GameServer authorization reuses `Authorization: GameServer <credential>`, configured privately with `GameServer__ApiKey`. No player JWT or client mission-progress value is accepted as a server fact. Never serialize the credential into Unity assets or ship it to clients.

## Template creation
```json
{
 "missionKey":"answer_10_unit_1", "title":"Practice Unit 1", "description":"Answer ten questions correctly in Unit 1.",
 "category":"Progress", "periodType":"Weekly", "eventType":"CorrectAnswer", "progressType":"Count", "targetValue":10,
 "conditions":{"unitId":1}, "progressField":"value", "resetEventType":null, "matchScoped":false, "repeatValue":false,
 "rewards":[{"rewardType":"XP","amount":50},{"rewardType":"Coins","amount":100}]
}
```
Categories: Exploration, Progress, Competitive, Lucky. Periods: Weekly, Monthly, Open. Events: PowerupUsed, MonsterDefeated, CorrectAnswer, WrongAnswer, StreakReached, MatchWon, MatchCompleted, DiceRolled, UnitUnlocked, PathDiscovered, MonsterFightLost. Initial progress types: Count, Boolean, MaxValue, Streak, UniqueCount. Custom evaluators are not included.
Conditions use exact case-sensitive payload keys and scalar equality. Number comparisons use decimal values. At most 16 fields; keys up to 64 characters and string values up to 200 characters. Nested objects/arrays are rejected. Unknown condition keys require that same field from the trusted server; no executable expressions.
Count increments once per matching unique event; Boolean target must be 1; MaxValue reads nonnegative integer `eventData[progressField]`; UniqueCount reads scalar `eventData[progressField]`. Streak increments matching events and resets on a condition mismatch or configured `resetEventType`. Set `repeatValue:true` for a streak of identical scalar values (e.g. dice rolls); a changed value restarts at 1. `matchScoped:true` requires matchId and resets state when the match changes. WrongAnswer/MonsterFightLost reset events must be emitted by the server when required by configuration. Progress is clamped to target; completed missions stop progressing.
Targets: 1–10,000. Rewards: 1–10 per template; XP/Coins amounts 1–1,000,000 with no product; Box uses `shopProductId` with no amount. Box catalog must have 1–100 valid item entries with positive bounded weights/quantities. Uses minimal Items/ShopProducts/BoxRewardEntries/PlayerInventoryItems persistence; no purchase/equip APIs are added.

## Activation creation
```json
{
 "name":"Weekly missions 2026-10-05", "periodType":"Weekly",
 "startsAt":"2026-10-05T00:00:00Z", "endsAt":"2026-10-12T00:00:00Z",
 "randomMissionCount":1, "autoClaimCompletedOnReset":true,
 "items":[{"missionTemplateId":1,"assignmentMode":"Fixed","weight":1,"sortOrder":0},
          {"missionTemplateId":2,"assignmentMode":"RandomPool","weight":2,"sortOrder":1}]
}
```
Global only, active upon creation. Name must be unique. Timed periods require end > start and end > current UTC; Open has no end. Up to 100 unique template items from the matching period. Weights 1–100,000. Random count must fit the configured pool; weighted selection is without replacement. Backend stores an assignment receipt including zero random selections. Mission rules, rewards and box odds are copied into player assignment snapshots. Repeated reads/events cannot reroll assignments.
Published configuration is immutable through this MVP API. Create a new template/activation for a new period. Optional template patch/delete, reward CRUD, activation patch/item mutation and activation list are deferred. Do not alter live periods directly in SQL; operational cancellation requires a reviewed data procedure until admin lifecycle APIs are added.

## Server event
```json
{
 "eventId":"match-500-player-10-answer-7", "playerProfileId":10, "communityId":null, "matchId":500,
 "eventType":"CorrectAnswer", "occurredAt":"2026-10-06T12:00:00Z",
 "eventData":{"unitId":1,"questionId":123,"streak":3}
}
```
Use a durable eventId per actual human occurrence (1–100 characters, no boundary whitespace). Player + eventId deduplicates; an identical retry returns the saved result with `duplicate:true`; changed context/data for the same key returns 409. New events must be within the last 24 hours and at most five minutes ahead; existing accepted events may replay later. All event fields remain identical on retry. MySQL timestamps normalize to microseconds.
If matchId is supplied, the registered match must include the human and match community; occurredAt must lie within its started/ended range. If communityId is supplied without matchId, current active school/class/license membership must match canonical user/profile. Omit backend identifiers for bots and for AI-controlled actions; the dedicated server must resolve actual human attribution before emitting facts. Match events are not automatically synthesized from completion, preventing duplicate ingestion paths.
Only currently active periods with startsAt <= occurredAt < endsAt progress. Events can establish assignments before the first lobby visit. Late events do not revive expired periods; relevant out-of-order facts return 409. Send facts chronologically for each player; equal timestamps follow bulk order. Response includes `{eventId,duplicate,updatedMissions}`; completed entries let Mirror notify the owning player after successful persistence.

## Player responses and claims
Mission entries include playerMissionId, missionKey, title, description, category, periodType, currentProgress, targetProgress, status, expiresAt, rewards. States: Assigned, InProgress, Completed, Claimed, Expired, AutoClaimed. `/me` hides expired/ended/cancelled periods and includes claimed entries for current periods.
Claim result includes playerMissionId, status, rewards, newXp, newLevel, newCoins. XP uses the existing level calculator and creates a Mission XP audit log; coins update Player.Gold. RP/match reward snapshots are unaffected. Box rewards roll immediately, increase item quantity and return boxName plus rewardItem metadata and quantityOwned. Saved claims replay the original roll and balance snapshot; refresh `/Progression/me` for current balances.
On expiry, incomplete missions expire. Completed missions auto-claim only when configured; cancellation expires rather than grants. Reset settles each profile transactionally, then marks timed activations ended. Partial reset failures are safe to resume from a fresh scope. Open missions with no end are ignored. Reset also settles already-earned rewards for suspended accounts; interactive player/event operations remain blocked. No recurring scheduler is installed.

## Errors and bounds
401 missing/wrong authentication. 403 non-platform-admin configuration/reset. 404 unknown/ineligible profile, wrong owner or invalid match/community context. 400 invalid configuration, unsupported data/progress values, stale new event, date/batch bounds. 409 claim not completed/expired, changed event replay, out-of-order event, duplicate configuration, balance overflow or invalid stored catalog. 503 persistence contention/failure; retry a fresh HTTP request with identical event IDs/claim ID. Bulk invalid events roll back prior events in the same batch. Request limits: events 512 KiB, configuration 128 KiB.
