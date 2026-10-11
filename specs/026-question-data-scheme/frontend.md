# Question data frontend integration

This feature provides backend contracts only. It does not build an educational authoring tool or change Unity.

## Educational insertion workflow

An active platform administrator can publish through PUT `/api/v1/Question/bank`. A future educational tool can use that authenticated workflow. It must generate a UUID once per published question and stable shared item IDs once, then distribute identical records to both services.

Form fields: Curriculum, Grade, Language, Subject, Term, Unit, Lesson, QuestionId, QuestionType and QuestionText. Show only the chosen type's answer controls:

- MCQ: choice ID/text and exactly one correct choice.
- TrueOrFalse: correct boolean; allow false explicitly.
- FillBlank: accepted variants for one typed response field.
- Ordering: stable item ID/text and unique zero-based correct position.
- MatchingPairs: stable pair ID with approved left/right texts.
- DragAndDrop: stable option ID/text plus zero-based blank number/correct text; retain duplicate text as separate options.

Validate required metadata, lengths and complete answer structure before sending. Disable submission while publishing; retain IDs across retries. Empty states show no published questions; validation errors show the API message; 409 tells the operator to use a new published UUID for changed content. Do not build editing/deletion flows that overwrite published records. Publication retires neither the old question nor its history automatically.

Admin bank listing uses GET `/bank/admin` with grade required and the optional curriculum/language/subject/term/unit/lesson filters. Suggested columns are the existing metadata, QuestionId, QuestionType and QuestionText. Show loading, empty and error states. Only admins can view this bank with answer keys; ordinary users receive 403.

## Game display and server integration

Authenticated display reads use GET `/bank`; full trusted keys use GET `/bank/server` from the dedicated server. Render the display-only option/item arrays. Matching has separate left/right presentation IDs; the dedicated server resolves them against independently sorted sides in its full bank. Display shuffling never changes identity.

Clients submit intent to the game server. The game server validates the authenticated player, current controller and question/stage, derives time and verifies the final answer before POST `/history`. Do not send server credentials or correct answer keys to clients. This feature adds no direct client history-write route.

Questions have TimerSeconds and history has TimeTakenSeconds: seconds, up to three decimal places. Use those units in forms, clocks, requests and diagnostics. The compatible Unity payload calls its question field Timer, also in seconds.

Admin POST `/bank/import-legacy/{packId}` converts an old pack using approved DUMMY/und/1 placeholders, retaining Grade/Timer. Share the returned records/IDs with educational and diagnostics services. Import is atomic/idempotent and retains the original pack. Show malformed/unsupported pack errors; never silently omit questions. Placeholder classification requires corrected publication under new IDs before real educational analysis.

Existing Unity clients keep the unauthenticated GET `/api/v1/Question?grade=...`; optional unit/lesson/curriculum/subject/language/term filters select new-bank questions in the same nested payload shape. No auth header is required. The compatible response retains answer fields and adds shared IDs. Assignment selects old packs when provided. `/bank` remains an authenticated display-only alternative. Actual Unity identity/history wiring is separate work; no Unity models are changed here.

## Student history

GET `/history/players/{playerId}` supports the authenticated player's own profile, or platform admins. Filter optionally by matchId and paginate. Rows can display QuestionId, MatchId, elapsed seconds, the typed selected response and derived IsCorrect. Metadata/prompt can be resolved from the bank's shared UUID. No answer timestamp is stored, so do not invent dated history views.

Show a loading state, an empty-history state and permission/error messages. Keep HistoryId as the unique row identity. History is read-only. Owners/teachers do not gain access to every student's educational history through this feature; broader school-scoped visibility is not implemented.

## Diagnostics exchange

The authorised server export GET `/history/server/players/{playerId}` returns typed attempts without question content. Keep originating HistoryId for deduplication and resolve shared IDs from the data science bank. No diagnostics dashboard or automatic push scheduling is added.

Full request shapes, pagination, limits, retry boundaries and migration rollout: [api.md](api.md).
