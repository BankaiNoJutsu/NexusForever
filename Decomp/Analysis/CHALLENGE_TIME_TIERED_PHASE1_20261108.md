# Time-tiered challenge completion: phase 1 implemented (2026-11-08)

Implements phase 1 of [CHALLENGE_TIME_TIERED_COMPLETION_DESIGN_20261108.md](CHALLENGE_TIME_TIERED_COMPLETION_DESIGN_20261108.md) on the user's go-ahead ("Implement phase 1", Thayd Sprint only, legacy reward path).
Evidence tier: Source/SQLite tests over inferred and agent-read inputs (tier rule: native read plus a 2016 clip; limit: agent-read footage; "finish = indicator location": inferred). **No retail parity claim.**

## What the code does now
* `ChallengeTimeTiers.RawGoals(entry, tierTable)`: the tier goals of a flag-8 row in millisecond units, zero goals kept, unset or missing tier ids skipped. `ServerChallengeUpdate.TierGoalCount` publishes these for
  flag-8 rows (count-tier rows still publish the compacted counts). `GoalCount`/`CurrentCount` are unchanged (decision D5).
* `IChallengeManager.TryCompleteTimeTiered(challengeId)`: needs the challenge active, time-tiered, with an **observed limit** (`ChallengeTimeLimits`, so today the seven exact readings), no durable attempt, and time
  remaining. Elapsed = limit minus remaining timer in whole milliseconds; tiers = `AchievedTierCount(limit, elapsed, rawGoals)`. It stores the tiers earned in `CurrentTier` (published as the update's
  `CurrentTier` for flag-8 rows), the medal index (`tiers - 1`) in `LastRewardTier`, then uses the legacy completion: `TierAchieved(medal)`, `Completed(medal)`, reward-track item of that medal, completion count,
  `CompleteChallenge` quest objective. Zero tiers (D1): `Completed` only, no `TierAchieved`, no item. A second call, an inactive challenge, no observed limit, or a timer at expiry returns false and sends nothing.
* `IChallengeManager.OnPlayerRelocated(worldId, position)`, called from `Player.OnRelocate`: for each active attempt of a challenge in `ChallengeTimeTiers.FinishesByReachingIndicator` (**1002 Thayd Sprint only**),
  when the player is on the indicator's world and inside its radius and vertical clamp (the `WorldLocationPrerequisiteHelper` rule, D6), it calls `TryCompleteTimeTiered`.
* Everything else is unchanged: timer expiry, abandon and zone leave still give no tier; flag-8 rows stay out of the durable producer (D4).

## Tests
`ChallengeManagerTests.TimeTiered.cs` (16 cases): raw goals (zero kept, missing skipped, null table), update packet with the unfiltered goals, count-tier row unchanged, completion at 10 s / 55 s / 65 s elapsed gives 3 / 2 / 1
tiers with matching results, item and packet state, last-millisecond completion (no medal, no reward), no-limit / inactive / double completion / at-limit / count-tier rejections, finish at the indicator, and no
finish for a position outside the radius, on another world, above the vertical clamp, without an active attempt, or for challenges 511 and 1500. Written before the code; first run after the implementation (one case
needed a test fix: two General challenges cannot be active at once). Challenge filter: 869 cases pass (Source/SQLite scope; not added to other scopes).

## Not covered and why
* The finish rule (indicator = finish) is inferred. Retail behaviour at zero tiers, `GoalCount`/`CurrentCount` use for time tiers and the reward-track credit for flag-8 medals are not witnessed.
* Test world and position are synthetic; the real Fortune's Ground world and the indicator radius (10, from the table) were not exercised against a live player.
* Relog continuity of `ActiveTimer` was not re-tested here (it relies on the existing persisted timer).
* Ravenok Racer needs a mount check; the other limit challenges (753, 956, 979, 273) have no usable finish; no other challenge has an observed limit.
