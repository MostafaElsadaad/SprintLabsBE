# Question data API

All routes use `/api/v1/Question` and the existing BaseResponse envelope. This feature adds relational storage; JSON request/response bodies are transport only. The old GET/PUT `/api/v1/Question` pack routes remain compatible.

## Relational-only game reads (2026-10-16)

GET `/api/v1/Question` now queries only Questions and its typed answer tables. It never queries LegacyQuestionPacks, including for headers, and has no legacy JSON fallback. The route stays anonymous, metadata/count filtering stays unchanged, and `data.payloadJson.Questions` retains the six existing Unity question shapes, choices, answers, shared IDs and seconds-based Timer fields. All assignment query parameters (including blank) return 400 by explicit user choice. Empty relational results return 404 even if a legacy pack exists.

Outer transport fields are always `id: 0`, `assignment: null`, `version: 1`, `createdAt: 1970-01-01T00:00:00Z`, `updatedAt: null`. These neutral values preserve the response shape without using legacy metadata; actual shared question IDs remain inside Questions. Unity's current adapter only consumes payloadJson and supplies grade without assignment, so no game update is needed. No database migration, table deletion or ID regeneration occurs. Legacy rows and the admin import endpoint remain for migration/recovery. The old PUT `/Question` still maintains legacy packs and does not publish to the game; use the existing admin PUT `/Question/bank` for new relational questions.

## Compatible selection and legacy import (2026-10-13)

Anonymous GET `/api/v1/Question?curriculum=DUMMY&grade=7&unit=1&count=10` filters relational rows before selecting up to 10 questions in stable QuestionId order. Count is optional and must be 1..2000; omitted count preserves full-bank behavior (maximum 2000). A smaller bank returns fewer questions. Empty filtered results return 404 without legacy fallback. Count with legacy assignment returns 400. Paged `/bank` routes continue using pageNumber/pageSize. The compatible response remains `data.payloadJson.Questions`, including the same Type/Prompt/Choices/CorrectIndex/Timer etc. and shared IDs; timers stay in seconds. No schema or authentication change is introduced.

Admin import uses the existing POST `/bank/import-legacy/{packId}` with no body. It validates/copies the whole pack into Questions and typed answer tables, retains the legacy row, returns shared IDs and is idempotent for unchanged content. Legacy fill-answer casing/outer-whitespace variants collapse to their first spelling using the existing grading normalization. Original UUID derivation remains unchanged. Normal publication still rejects equivalent duplicate variants.

Staging's four packs were imported on 2026-10-13 using a temporary EF runner calling the existing service: pack 1=grade 5 (55), 2=grade 6 (14), 3=grade 7 (14), 4=grade 8 (14). Total 97, source rows retained, repeated import unchanged. Imported curriculum/subject are DUMMY, language und, term/unit/lesson 1; original grade and timer are retained. Exported IDs live outside Git in SprintLab-Artifacts/question-data-scheme/staging-import-shared-identities.json. This publication enables the corrected HTTP importer and count limit on staging; verify both after deployment.

```sh
curl.exe -X POST "https://noxed.ddns.net/api/v1/Question/bank/import-legacy/3" -H "Authorization: Bearer ADMIN_ACCESS_TOKEN"
curl.exe "https://noxed.ddns.net/api/v1/Question?curriculum=DUMMY&grade=7&unit=1&count=10"
curl.exe "https://noxed.ddns.net/api/v1/Question?grade=7&count=10"
```

The import token must belong to an active platform administrator. GET needs no authentication. Curriculum filters must match stored values; imported rows do not have Egypt classification.

## Routes and permissions

| Method | Route suffix | Authentication / authorisation | Request | Success data |
|---|---|---|---|---|
| PUT | `/bank` | Player JWT belonging to an active platform administrator | QuestionDataRequest below | Shared UUID string |
| POST | `/bank/import-legacy/{packId}` | Active platform administrator JWT | No body | Converted records and shared IDs |
| GET | `/bank` | Active account JWT | QuestionBankFilter query | Paged display-only questions |
| GET | `/bank/admin` | Active platform administrator JWT | Same query | Paged complete questions including answer keys |
| GET | `/bank/server` | Existing GameServer credential | Same query | Paged complete questions including answer keys |
| POST | `/history` | Existing GameServer credential only | QuestionHistoryRequest below | Stored attempt with HistoryId and calculated IsCorrect |
| PUT | `/history/{historyId}` | Existing GameServer credential only | Identical original QuestionHistoryRequest | Existing unchanged attempt; no new rows |
| GET | `/history/players/{playerId}` | Active account JWT; own player profile or platform administrator | Optional matchId, pageNumber, pageSize | Paged typed history |
| GET | `/history/server/players/{playerId}` | Existing GameServer credential only | Same query | Paged typed history for diagnostics export |

No new credential or role is introduced. The educational insertion tool publishes through an authorised platform-admin workflow. Game-server credentials never belong in a student's client, educational document or frontend application.

## Question publishing

Example metadata/IDs below are illustrative, not source-file metadata. The educational team supplies the actual metadata and generates the shared UUID once. Both backends retain the same UUID and answer/item IDs.

```json
{
  "questionId": "550e8400-e29b-41d4-a716-446655440000",
  "curriculum": "Egypt",
  "grade": 7,
  "language": "en",
  "subject": "Science",
  "term": 1,
  "unit": 1,
  "lesson": 1,
  "questionType": "MCQ",
  "questionText": "The building and structural unit of all matter is the @.",
  "timerSeconds": 10,
  "choices": [
    { "choiceId": "q001-c0", "choiceText": "electron", "isCorrect": false },
    { "choiceId": "q001-c1", "choiceText": "molecule", "isCorrect": false },
    { "choiceId": "q001-c2", "choiceText": "atom", "isCorrect": true },
    { "choiceId": "q001-c3", "choiceText": "cell", "isCorrect": false }
  ]
}
```

Exactly one answer shape is allowed:

| questionType | Answer properties |
|---|---|
| MCQ | choices: choiceId, choiceText, isCorrect; at least two choices, exactly one true |
| TrueOrFalse | correctAnswer: true or false (required, not null) |
| FillBlank | acceptedAnswers: nonempty array of text variants for one typed response |
| Ordering | items: itemId, itemText, correctPosition; consecutive positions from 0 |
| MatchingPairs | pairs: pairId, leftText, rightText; each row is an approved correct pair |
| DragAndDrop | options: optionId, optionText; blankAnswers: blankNumber, correctText |

Question types are exact names, not the old Unity numeric enum. Item IDs are globally unique within their respective tables, case-sensitive ASCII strings of 1..80 characters; retain them under display shuffling. Metadata strings: curriculum/subject 120 characters; language 20; prompt 16000. Grade, unit and lesson must be positive; term must be nonnegative. Term 0 represents a year without semesters. Choice/item/pair/option collections are limited to 100 entries; option/item text to 4000 characters; accepted variants to 255 characters and 100 entries. Request body limit: 1 MiB.

All non-applicable collections may be omitted or empty, but cannot be explicitly null. Do not mix types. Duplicate option text is preserved with different option IDs. In the current drag format an option instance is used once; repeated correct text therefore needs repeated option instances. Matching source arrays must be approved as corresponding correct pairs before import.

Identical republishing with the same UUID and content is a no-op, including collection reordering. Changed published content returns 409; publish a new UUID and child IDs instead. Do not delete old published rows. Same-key concurrent database conflicts are safe to retry after the competing transaction finishes; content must still match.

TimerSeconds is the question's time limit in seconds, default 10 when omitted. TimeTakenSeconds is elapsed response time in seconds and is required in history bodies. Both use decimal(10,3) with up to three decimal places; timer must be positive, elapsed time nonnegative. Values beyond 9999999.999 or with extra precision are rejected rather than rounded. Bodies using only timeTakenMs are rejected. Unity Timer values are already seconds and remain in seconds.

## Filtered reads

Example: `GET /api/v1/Question/bank/server?grade=7&unit=1&lesson=1&subject=Science&language=en&term=1&curriculum=Egypt&pageNumber=1&pageSize=50`.

Grade is required. Unit, lesson, curriculum, language, subject and term are optional filters. Page defaults to 1, size to 50; allowed page 1..100000 and size 1..200. Results are ordered by shared UUID, not publication date. Page data uses `{ items, page, pageSize, total }`.

The public display response contains shared metadata, questionId, questionType and questionText; choices/items/options are `{ id, text }` arrays. It includes matchingLeft and matchingRight arrays plus computed blankCount. It never contains IsCorrect, CorrectAnswer, AcceptedAnswers, CorrectPosition, stored PairIds or correct blank answers. Server/admin reads return the full QuestionDataRequest shape.

Matching display IDs are side-local presentation IDs (`left-0`, `right-0`, etc.), not shared PairIds. Each side is independently ordered by its text using ordinal comparison, then PairId as tie-breaker. The game server retrieves the full bank, recreates this ordering and resolves presentation selections to educational PairIds before ingestion. It may shuffle display positions while retaining presentation identities. Do not assume equal numeric suffixes mean a correct pair. Other displayed choice/item IDs are their shared IDs.

## History writes

```json
{
  "playerId": 101,
  "questionId": "550e8400-e29b-41d4-a716-446655440000",
  "matchId": 10,
  "timeTakenSeconds": 4.2,
  "selectedChoiceId": "q001-c1"
}
```

| Type | Submitted response |
|---|---|
| MCQ | selectedChoiceId |
| TrueOrFalse | selectedAnswer: boolean; false is valid |
| FillBlank | answerText: original text, at most 4000 characters; empty text is an incorrect submitted answer |
| Ordering | ordering: itemId, selectedPosition for every item |
| MatchingPairs | matching: leftPairId, selectedRightPairId for every left item |
| DragAndDrop | dragDrop: blankNumber, selectedOptionId for every blank |

The player must be in the existing match's authenticated-human roster. Server ingestion checks question ownership of every selected ID, type, response completeness, unique positions, one-to-one matching and unique drag option instances. The trusted game server must additionally verify the current human controller, active question/stage and deadline, and measure elapsed time; backend history fields do not encode those runtime facts. Never attribute temporary AI/bot responses to a disconnected human's retained player ID. Body limit: 128 KiB.

HistoryId is generated by the backend. Correctness is returned as a derived value, not stored in the five-column history table. FillBlank comparison trims outer whitespace and ignores case with invariant rules, preserving original submitted text. Other types compare exact item identities/positions or exact option text as specified by the approved schema.

**Retry boundary:** every POST creates a new attempt, including legitimate repeated answers to the same question in the same match. Once HistoryId is known, PUT to that ID safely confirms an identical attempt; changed data returns 409 and unknown IDs return 404. Without a known ID, do not blindly resend POST after an ambiguous timeout: reconcile against server-exported history first. The approved columns cannot guarantee exactly-once creation after a lost first response; a durable submission ID would require a separately approved extension.

## Success and error responses

The envelope follows the existing backend: `data`, `message`, `statusCode`, `errorCode`. History data contains HistoryId, PlayerId, QuestionId, MatchId, TimeTakenSeconds, IsCorrect and the applicable selected response. Non-applicable response collections are empty and scalar responses null. Read history pages with the same page shape as the bank.

| HTTP status | Meaning |
|---|---|
| 400 | Missing/invalid UUID or metadata, invalid question/response type, bad references, incomplete response, negative time, invalid pagination |
| 401 | Missing/wrong authentication scheme; ordinary player credentials cannot use server routes |
| 403 | Non-admin publication/key access, inactive user, another player's private history |
| 404 | Unknown question or replay history ID |
| 409 | Published ID/content conflict, child ID collision, changed replay payload, database reference conflict |

The normal API exception middleware supplies controlled error envelopes. No database exception details or credentials are returned.

## Manual verification after rollout

1. Back up an isolated MySQL test database and apply the reviewed migration through the normal deployment procedure. Verify the legacy pack row count/content is unchanged under LegacyQuestionPacks.
2. With a platform-admin JWT, publish the example MCQ, then repeat it: expect the same UUID and one question with four choices. Change a choice under that UUID: expect 409 without modification.
3. Read /bank as a player: expect display options without keys. Read /bank/admin as that player: expect 403. Read /bank/server with a player token: expect 401.
4. Register a human match through the existing Matches API. From the trusted game server, POST /history using its real match/player and a choice belonging to this question. Inspect the stored parent/child rows and returned correctness.
5. PUT the identical response to the returned HistoryId: expect no duplicate. Change time/response: expect 409. Submit a foreign choice or non-roster player: expect 400 and no new history.
6. Read the player's history with their JWT and another player's JWT: expect 200 and 403 respectively. Verify the server export contains shared UUIDs and selected IDs without question content.
7. Publish and answer the other five types, including false, text variants, duplicate drag text and incorrect ordering/matching. Check complete child rows and derived correctness.
8. Verify HEX(QuestionId) equals UUID text without hyphens and without byte swapping. Publish accent-distinct FillBlank variants such as cafe/café to verify binary key collation.

These are rollout/manual checks, not claims of live execution in this task.

## Database rollout and compatibility

Migration: `20261011001014_QuestionDataScheme`. Upgrade renames the existing Questions pack table to LegacyQuestionPacks and preserves every row, then creates the 15 approved tables with restrictive foreign keys and indexes. Never apply an earlier unreviewed auto-scaffolded migration that repurposes the old table. Shared UUIDs use RFC/network byte order, compatible with unswapped MySQL UUID_TO_BIN(uuid, 0); APIs use standard UUID text.

Apply both source migrations: `20261011001014_QuestionDataScheme` and `20261011010334_QuestionTimingSeconds`. The latter adds TimerSeconds (default 10) and preserves any earlier typed-history values by renaming/widening TimeTakenMs, converting those old values to seconds and setting decimal(10,3). No elapsed values are dropped. Existing legacy progression AnswerTimeMs/QuestionTimeMs contracts remain unchanged.

Positive-term correction (2026-10-12): retain the previously applied `20261012173525_AllowQuestionTermZero` migration and apply the forward migration `20261012180205_RestorePositiveQuestionTerms`. It restores `Term > 0` while retaining positive grade/unit/lesson rules. Verify that no questions have `Term <= 0` before rollout; if any exist, stop and obtain their correct classification without deleting or silently relabeling them. The migration first adds a temporary positive-term check, so incompatible rows stop the upgrade before the existing constraint is removed. Existing positive rows are unchanged. This historical correction is superseded by the forward compatibility migration below.

Term 0 restoration (2026-10-12): retain both historical term migrations and apply `20261012194637_ReinstateQuestionTermZero`. Publishing and all bank filters accept Term 0 and existing positive terms; negative terms return 400. Atlas offers terms 0, 1, 2 and 3; the backend deliberately retains positive-term compatibility without a new upper bound. The migration uses a temporary full metadata check while replacing `CK_Questions_Numbers`; grade/unit/lesson stay positive and existing rows/IDs are unchanged. Rollback first validates a positive-term check and stops safely if Term 0 rows exist; obtain their correct classification before retrying, without deletion or silent relabeling.

Packs are not converted automatically during migration. After rollout, an admin calls POST `/bank/import-legacy/{packId}`. Approved placeholders: Curriculum/Subject DUMMY, Language und, Term/Unit/Lesson 1; source Grade is retained. Source Timer seconds are retained or default to 10. Conversion validates/saves the whole pack atomically with batched queries/writes, retaining every original LegacyQuestionPacks row. Identical retries reuse IDs and create no duplicates. Limits: 16 MiB, 1..2000 questions, six supported Unity wrapper types; ImageMCQ/malformed packs fail without partial import.

Valid source QuestionId UUIDs are preserved; missing IDs receive stable UUIDv5 identities from this tool's namespace, source pack identity and canonical snapshot/index. Child IDs are deterministic and question-scoped. Source changes generate new IDs; property reordering does not. Export the returned complete QuestionDataRequest records to the educational tool and diagnostics backend. They must reuse those IDs, not derive IDs from their own local pack numbers. Replace placeholder classification by publishing corrected records under new IDs before final educational analysis.

GET `/api/v1/Question?grade=5` keeps its envelope and payloadJson.Questions/numeric Type/nested bodies while using only relational rows. Timer remains seconds. QuestionId and ChoiceIds/ItemIds/PairIds/OptionIds are additive fields. Ordering uses item-text values for the current Unity contract; TrueOrFalse includes CorrectAnswer. Unconverted packs no longer supply game responses; import them first. Assignment requests return 400.

GET `/api/v1/Question` is explicitly AllowAnonymous: no credential is required. It returns relational questions in the original Unity payload shape, including answer fields, filtered by grade and optional curriculum/language/subject/term/unit/lesson/count. Auth headers do not gate this read. Empty results return 404. Compatible packs are capped at 2000 questions and read on one snapshot; use the paged bank API for larger sets. Transport sentinels are always Id=0, Version=1, CreatedAt=UnixEpoch, Assignment=null and UpdatedAt=null; these are not question identities or publication timestamps. Assignment is no longer supported. Publishing, conversion, typed bank routes and history routes retain their authorisation.

Existing numeric MatchQuestionResults remain legacy progression summaries, separate from typed educational history. Existing completion/rewards are not recalculated from new rows.

Applying this source migration is a deployment step requiring a coordinated API/database release because old API binaries still expect the old table name. Database rollback drops new tables and therefore loses new-schema data; export/backup before rollback. No live migration or deployment was executed by this implementation task.

Diagnostics receives paged typed history through the authorised export route and deduplicates by originating HistoryId. It already stores the same published question/item IDs; prompts and metadata are not repeated per attempt. Automated diagnostics delivery and the educational authoring UI are separate work.
