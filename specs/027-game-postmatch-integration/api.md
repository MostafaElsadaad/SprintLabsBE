# Atomic post-match question history

POST `/api/v1/Matches` still uses GameServer authentication and returns matchId/status, plus `supportsQuestionHistory: true`. Unity must wait for this capability before sending typed history through completion; older deployments do not support the feature and must leave reports queued rather than silently discard selections.

POST `/api/v1/Matches/{matchId}/complete` retains its existing GameServer-only bulk contract. Each questionResults entry can optionally include `questionResponse`, in the existing QuestionHistoryRequest shape (playerId, shared UUID questionId, matchId, timeTakenSeconds, and exactly one typed response). Older clients may omit it. Unanswered summaries omit it; no chosen answer is invented. Legacy outer questionId remains null for shared-UUID answers. The nested shared UUID is the educational identity.

Nested player/match/type/elapsed time must match the outer fact, and correctness is re-derived from the immutable published question and must match isCorrect. Bounds and typed references reuse question-history validation. Seconds remain the history unit; only existing legacy completion summary fields use milliseconds. Responses are batch-loaded and staged once, in the same locked transaction as XP/RP/rewards. A validation failure rolls back the whole operation. Completed retries return saved rewards before any new history is inserted. No migration or mission event/reward is introduced.

Success remains MatchCompletionResponse. Each saved reward also returns xpIntoCurrentLevel and nextLevelXpRequirement, calculated from the backend level rules for the saved XP snapshot. These are response fields only, with no storage migration. Errors remain 400 invalid facts/selections (including an unknown nested question), 401 wrong authentication, 404 unknown match, 409 invalid match state, 503 persistence contention/failure. Display only backend rewards, not client requested XP/RP. HTTP transport/body limits and existing permission rules are unchanged.

The compatible question-bank projection adds Ordering.CorrectItemIds alongside ItemIds so duplicate item labels still preserve the correct educational identities. No existing question fields are removed.

Example `questionResults` entry within the existing bulk completion body:

```json
{
  "playerProfileId": 101,
  "questionId": null,
  "questionType": "TrueOrFalse",
  "isCorrect": true,
  "answerTimeMs": 1234,
  "questionTimeMs": 10000,
  "streakBeforeAnswer": 0,
  "streakAfterAnswer": 1,
  "questionResponse": {
    "playerId": 101,
    "questionId": "11111111-1111-4111-8111-111111111111",
    "matchId": 42,
    "timeTakenSeconds": 1.234,
    "selectedAnswer": false
  }
}
```

Use actual registered player/match IDs and a published question UUID; the example assumes its correct answer is false. The surrounding registration and completion bodies remain documented in [match progression](../022-match-progression-apis/api.md).

## Staging rollout

Merge into `development` to run `.github/workflows/deploy-staging.yml` (restore, build, test, image build and container replacement). The workflow preserves the VM's `/opt/sprintlabs/.env`. Configure `GameServer__ApiKey` there through the server's private operational access before deployment, and supply the matching value only to the trusted game-server process as `SPRINTLAB_GAME_SERVER_API_KEY` or the local launcher password field. Never put it in a player build, source, logs, or shared screenshots. No database migration is added by this feature.

After deployment, verify normal question reads, authorized registration's capability flag, and completion/history atomicity. Retrying a completed match must return its saved rewards without adding history or XP again. Gold and mission rewards remain outside this integration.
