# Restoration gap 4: time-tiered challenges, server part (2026-11-08)

Part of [RESTORATION_GAPS_GOAL_PROMPT.md](RESTORATION_GAPS_GOAL_PROMPT.md). Inputs: [CHALLENGE_TIME_TIERED_20261108.md](CHALLENGE_TIME_TIERED_20261108.md), [CHALLENGE_TIME_TIERED_TRIGGER_CENSUS_20261108.md](CHALLENGE_TIME_TIERED_TRIGGER_CENSUS_20261108.md)
and [CHALLENGE_TIME_TIERED_OCR_PASS2_20261108.md](CHALLENGE_TIME_TIERED_OCR_PASS2_20261108.md) (agent-written, not re-viewed by the lead). Evidence tier of the code: Observed (dated retail footage, agent-read) for the limits,
native-read plus a 2016 retail clip for the tier rule; Source/SQLite tests. No retail parity claim.

## What was added
* `ChallengeTimeLimits` (`ChallengeTimeTiers.cs`): exact start-timer readings by challenge id: 273 Bringin' the Bingberries 360 s, 481 Up the Falls 120 s (not time-tiered), 511 Ravenok Racer 60 s, 753 Top of the
  World 80 s, 956 Back to Basic 65 s, 979 Doing Things The Hard Way 1,200 s, 1002 Thayd Sprint 120 s. Probable values and lower bounds (Windspire Ascent, Entrance Exam, Rock Climber, Race to the Unknown, Grounding
  the Storm) are deliberately **not** in the table; the second "Doing Things The Hard Way" row (981) was not read either.
* `ChallengeManager` uses the observed limit as the activation timer and as `TimeTotalActive` in `ServerChallengeUpdate`; every other challenge keeps the 300 s default. This also makes the active-timer expiry
  (`HandleActiveTimerExpired`) use the right length for those seven.
* `ChallengeTimeTiers.AchievedTierCount(limit, elapsed, goals)`: a tier with goal `g` (a minimum remaining time in ms) is lost once `elapsed >= limit - g`, kept while `elapsed < limit - g`; a goal at or beyond the
  limit is never reachable; goal 0 is kept for as long as the timer runs. This is the rule established earlier (the client's per-tier counter is the main timer minus the goal; a 2016 clip shows silver = main - 180 s).
  It is a pure function with no caller yet.
Tests (added to `ChallengeManagerTests`, 8 limit cases, one exactness case, 8 tier-boundary cases and one edge case) were written before the code and first run after it; green on the first run, not red.
Filter Challenge: 853 cases pass (Source/SQLite scope).

## What is deliberately not done, and why
These touch the persisted medal logic built earlier (terminal tier == medal, derived published tier), so each needs a design decision rather than a guess:
1. **Completion path for time-tiered challenges.** `TryAdvanceProgress` has no production caller, and the tier goals are minutes-as-milliseconds that `GetTierGoalCounts` reads as counts and compacts (a zero first goal
   is dropped, so Bronze is lost on 51 rows; 10 single-tier `[0]` rows get `goalCount` 0 and can never complete). A time-tiered completion needs its own path: complete when the finish condition is met, tier =
   `AchievedTierCount(limit, elapsed, rawGoals)` with the raw (uncompacted) goals, published as the medal. Decision needed: how the raw goals are carried (the packet's `TierGoalCount` for these rows should
   carry the unfiltered millisecond goals so the client's counters match the tables) and how the persisted tier invariant treats a completion that earns fewer tiers than the goal list.
2. **Finish detection.** The only group sharing one shape is "reach the indicator location" (34 rows, 28 with a usable indicator; indicator = finish is inferred, 4 of 4 checks plus 13 of 14 "climb" names). Of those,
   only 1002 Thayd Sprint and 511 Ravenok Racer have an exact limit today, so enabling finish detection would affect two challenges until more limits are read. It needs a position hook
   (`Player.OnRelocate`) and the indicator radius plus vertical clamp (`WorldLocationVolumeGridTriggerEntity` exists for quest EnterArea).
3. **Counts for interact/kill groups (21 rows)**: the tables hold goals in milliseconds; the object counts are unknown.
4. **Hazard, waypoint and carry rules (18 rows)** and the text-less QA rows: no data.
Nothing here was guessed into the server: with no completion path, a flagged challenge can still be activated and shown with the right timer for the seven known ones, but it cannot be completed.

## Footage part (pass 3 done)
[CHALLENGE_TIME_TIERED_OCR_PASS3_20261108.md](CHALLENGE_TIME_TIERED_OCR_PASS3_20261108.md) (agent-written, not re-viewed): the remaining 413 cue windows (all 1,163 now swept) and a name search found **no new challenge limit**;
four single-clip rows became probable values and Race to the Unknown is now about 112 s. 69 of the 80 flagged challenges have no retail footage in the library, so the footage lane is close to exhausted at this
library: only second clips of the probable rows could add exact limits, and the library appears not to contain them. The table therefore stays at the seven exact readings, which means the finish-detection phase
can enable Thayd Sprint (and Ravenok Racer with a mount check) and nothing else until limits arrive from another source.

## Phase 1 of completion implemented (2026-11-08)
See [CHALLENGE_TIME_TIERED_PHASE1_20261108.md](CHALLENGE_TIME_TIERED_PHASE1_20261108.md): raw goals published, `TryCompleteTimeTiered` and reach-detection for Thayd Sprint only. Items 1 and 2 of "What is deliberately not done"
are now done for that one challenge (legacy reward path); items 3 and 4 and every other challenge are unchanged.

