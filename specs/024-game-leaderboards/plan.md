# Plan
Reuse RankingController, MediatR progression slices, IProgressionReadService and existing persistence.
Extend list requests/responses additively; separate own-standing CQRS slice. Isolate leaderboard-specific queries in a partial ProgressionReadService file while preserving shared caller, staff scope, paging and filter helpers.
Project public entries and period aggregate points in SQL; calculate own ordinal by count of better scores/tied lower IDs. Never load a full leaderboard to determine own standing.
Use TimeProvider for deterministic boundaries. No schema change: existing MatchRewardResults plus Matches CompletedAt supply settled ranking facts.
Verify service behavior, TestHost HTTP binding/envelopes/identity, SQLite SQL translation and an optional isolated MySQL execution test, then solution build/test. Preserve unrelated appsettings and dashboard edits.
