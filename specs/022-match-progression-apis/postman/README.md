# Manual Postman integration collection

Import `SprintLabs.MatchProgression.postman_collection.json`. Set baseUrl (without trailing slash), serverCredential, playerAccessToken, and two distinct existing active backend player profile IDs privately in a Postman environment. Player token must belong to player 1 for the self/by-ID read checks. Never export filled credential/token variables.

Run folder 1 then folder 2 against a disposable test backend with both migrations and the server credential configured. Folder 1 creates a fresh ranked match and awards real XP/RP to both test humans. Completed-match retry asserts the exact saved snapshot. Every registration run creates new history/rewards; it does not undo progression. Capture IDs and clean up only using an approved test database procedure.

Folder 3 is optional: supply a community ID and replace playerAccessToken privately with an authorized owner/teacher/admin token. It tests staff reads; the created match above is global (communityId=null), so it does not appear in school history. Create a separate school match with licensed humans if testing school completion.

The collection has 13 requests. Its JSON and scripts are checked offline; no live API calls were executed during creation. Server credentials stay on the test runner/dedicated server, never a game client. Follow ../api.md and ../quickstart.md for auth restrictions, invalid-request scenarios and optional MySQL concurrency checks.
