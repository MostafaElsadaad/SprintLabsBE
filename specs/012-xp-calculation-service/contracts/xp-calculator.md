# Internal Contract: XP Calculator

This feature exposes no HTTP API and no frontend contract. The contract below documents the expected internal calculator behavior for future match completion code.

## Calculator Capabilities

The XP calculator must provide behavior equivalent to:

- Calculate answer XP from ordered question outcomes.
- Calculate match result XP from winner/non-winner participant state.
- Calculate mission XP from mission difficulty.
- Calculate total XP from answer XP, match result XP, and mission XP.

## Answer XP Contract

Input:
- Ordered answer outcomes, each with `IsCorrect`.

Output:
- Integer answer XP.

Rules:
- Correct answer base XP is 10.
- Wrong answers give 0 XP.
- Streak is computed from ordered outcomes.
- Wrong answer resets streak to 0.
- Streak 1 uses multiplier 1.0.
- Streak 2 or 3 uses multiplier 1.2.
- Streak 4 or 5 uses multiplier 1.4.
- Streak 6 or 7 uses multiplier 1.8.
- Streak 8 or 9 uses multiplier 2.2.
- Streak 10 or greater uses multiplier 3.0.

## Match Result XP Contract

Input:
- `IsWinner`.

Output:
- 50 XP when `IsWinner` is true.
- 20 XP when `IsWinner` is false.

## Mission XP Contract

Input:
- Mission difficulty: `Normal`, `Mid`, or `Hard`.

Output:
- Normal: 25 XP.
- Mid: 50 XP.
- Hard: 100 XP.

Mission XP is XP only. It must not calculate or imply RP, rank, level, mission persistence, or mission completion.

## Total XP Contract

Input:
- `AnswerXp`
- `MatchResultXp`
- `MissionXp`

Output:
- `AnswerXp + MatchResultXp + MissionXp`
