# Time-tiered challenge completion: design (2026-11-08)

Part of restoration gap 4 ([RESTORATION_GAP4_TIME_TIERED_CHALLENGES_20261108.md](RESTORATION_GAP4_TIME_TIERED_CHALLENGES_20261108.md)). **Design only; no code changed by this document.** Phase 1 was implemented afterwards: see [CHALLENGE_TIME_TIERED_PHASE1_20261108.md](CHALLENGE_TIME_TIERED_PHASE1_20261108.md) (D1 to D3, D5 to D7 as recommended; D4 still deferred). Inputs:
[CHALLENGE_TIME_TIERED_20261108.md](CHALLENGE_TIME_TIERED_20261108.md), [CHALLENGE_TIME_TIERED_TRIGGER_CENSUS_20261108.md](CHALLENGE_TIME_TIERED_TRIGGER_CENSUS_20261108.md),
[CHALLENGE_TIME_TIERED_OCR_PASS2_20261108.md](CHALLENGE_TIME_TIERED_OCR_PASS2_20261108.md), the persisted-tier work in [VIDEO_WITNESS_CHALLENGE_PERSISTED_TIER_20261108.md](VIDEO_WITNESS_CHALLENGE_PERSISTED_TIER_20261108.md).
Evidence tiers: the tier rule is native plus a retail clip; "finish = indicator location" is **inferred**; the limits are agent-read footage. Each decision below says what it rests on.

## 1. What the code does today (facts)
* `GetTierGoalCounts` keeps only tier rows with `Count > 0`, so a time-tiered row's zero first goal is dropped (Bronze lost on 51 rows) and a single-tier `[0]` row (10 rows) has no goal at all;
  `TryAdvanceProgress` returns false when `goalCount == 0`, and has no production caller.
* The flag-8 rows are excluded from the durable (reward track) producer (`SupportsDurableProducer`), so a completion would use the legacy path: `CompleteChallenge(state, entry, null)` sends
  `TierAchieved(LastRewardTier)` then `Completed` and grants the reward-track item for `LastRewardTier`.
* `ActivateChallenge` sets `ActiveTimer` to the limit (now the observed one for seven challenges); `Update` counts it down and `HandleActiveTimerExpired` deactivates with `TimerExpired` and no tier.
  `ActiveTimer` is persisted, so elapsed time survives a relog as `limit - ActiveTimer`.
* The published `TierGoalCount` is `PadTierGoals(compacted goals)`, three words; `GoalCount` is the compacted goal of the current tier.
* `Player.OnRelocate` already fans out position changes (`ZoneMapManager`, `PathManager.CompleteCurrentExplorerExploreZoneMission`, ...). `WorldLocation2` carries `WorldId`, `Position0..2` and `Radius`;
  `WorldLocationVolumeGridTriggerEntity` implements radius plus vertical clamp for quest EnterArea.
* Client semantics (native, agent-read): the tier counters are derived by the client from the main timer minus each goal; the goals are minimum remaining times; `CurrentTier` in the update row and the result's tier
  argument are tier counts / 0-based as already implemented for count tiers.

## 2. Design
### 2.1 Raw goals (no compaction for flag 8)
`ChallengeTimeTiers.RawGoals(entry, tierTable)` returns, in order, the `ChallengeTierId00..02` rows that exist, **keeping zero counts** (a goal of 0 is a real Bronze). Used for flag-8 rows only, so count-tier challenges
and their persisted invariants are untouched. `SendChallengeUpdate` publishes `TierGoalCount = PadTierGoals(RawGoals)` for these rows so the client's counters match the tables (the OCR threshold lists matched the
tables exactly). `GoalCount` and `CurrentCount` keep their current meaning (see decision D5).

### 2.2 Completion
New `IChallengeManager.TryCompleteTimeTiered(ushort challengeId)`:
1. Preconditions: `CharacterRewardsReady`, the challenge is active and `Activated`, `ChallengeTimeTiers.IsTimeTiered(entry)`, and **an observed limit exists** (`ChallengeTimeLimits.GetSeconds`). Without one the call
   returns false and traces "time-tiered challenge has no observed limit" (decision D2): a tier computed from the 300 s default would be wrong.
2. `limitMs = limit * 1000`, `elapsedMs = limitMs - ActiveTimer * 1000` (clamped to `[0, limitMs]`); a completion at or after expiry is rejected (the timer expiry path owns that).
3. `tiers = AchievedTierCount(limitMs, elapsedMs, RawGoals)`; the medal index is `tiers - 1`.
4. Legacy path first: set `LastRewardTier = tiers - 1`, then `CompleteChallenge(state, entry, null)`, which already sends `TierAchieved`, `Completed`, grants the reward-track item for the tier and counts the
   completion. `tiers == 0` (finishing in the last millisecond, or a first goal at or above the limit): completion without a medal (decision D1).
5. Idempotence: the generation/attempt checks used by `TryAdvanceProgress` guard against re-entry from the notice callbacks.

### 2.3 Finish detection ("reach")
`IChallengeManager.OnPlayerRelocated(Vector3 position)`, called from `Player.OnRelocate`. For each active, activated, time-tiered `General` challenge with `WorldLocation2IdIndicator != 0` on the player's world:
inside the indicator when the horizontal distance to `Position0/2` is within `Radius` and the vertical clamp passes (reuse the quest EnterArea rule, decision D6), then call `TryCompleteTimeTiered`.
Only challenges with an observed limit are eligible, so today: **1002 Thayd Sprint** (indicator 41555, radius 10, Fortune's Ground) and **511 Ravenok Racer** (a mount race; the mounted check of group V is not
modelled, decision D3). 753 Top of the World, 956 Back to Basic, 979 Doing Things The Hard Way and 273 Bingberries have limits but no usable finish or different mechanics (climb with no location, waypoints, hazards,
interact counts) and are not enabled by this phase.

### 2.4 Failure and cleanup
Timer expiry, abandon and zone leave keep their current behaviour (no tier). Starting a new attempt resets `LastRewardTier`. Nothing in this design changes cooldown handling.

### 2.5 Durable (reward track) producer: separate phase
With raw goals a time-tiered medal is `tiers - 1` in `0..2`, which satisfies the persisted invariants (`Tier < 3`, terminal tier equals the medal), so `SupportsDurableProducer` could later admit flag 8 and publish
through `QueueProducerTransition`. That path has configured MySQL gates this environment cannot run, so it is deferred until they can be run; until then flag-8 completions use the legacy grant.

## 3. Decisions (recommendation first; none is decided by evidence)
| # | Decision | Recommendation | Rests on |
|---|---|---|---|
| D1 | Completion with zero tiers | Count it as completed, no medal and no reward item | no witness; zero tiers only occurs at the last millisecond or when the first goal is at or beyond the limit |
| D2 | Require an observed limit | Yes; a table (`ChallengeTimeLimits`) is the single switch, grown as limits are read | the earlier "do not select tiers without a limit source" rule |
| D3 | Initial enablement | Thayd Sprint only; add Ravenok Racer with a mount check | 1002 has 5 exact readings and a verified destination name; 511 has one reading |
| D4 | Phase 2 durable producer | After MySQL gates can run | persisted tier invariants |
| D5 | `GoalCount` / `CurrentCount` for flag 8 | Leave as is until a retail packet or a native read of how the client uses them for time tiers | not read |
| D6 | Vertical clamp for the finish | Reuse the quest EnterArea rule | indicator radii of 1 to 30 and a median +81 units of height gain suggest a vertical test matters; unverified |
| D7 | The 10 single-tier `[0]` rows | Complete by finish like the others (tier 1 while the timer runs) | the same goal-0 rule |

## 4. Tests (written first, in this order)
1. `RawGoals`: keeps a zero first goal, skips missing tier ids, three-goal and single-goal rows.
2. Update packet: a flag-8 row publishes the unfiltered millisecond goals; a count-tier row is unchanged.
3. `TryCompleteTimeTiered`: no observed limit -> false and nothing sent; Thayd Sprint at 10 s / 55 s / 65 s elapsed -> tiers 3 / 2 / 1 (goals 1 / 60000 / 70000 ms, limit 120 s) with the matching result packets and
   reward item index; at the limit -> false; twice -> second call false; inactive challenge -> false; relog continuity (`ActiveTimer` restored) gives the same elapsed.
4. Finish detection: inside and outside the radius, wrong world, challenge not active, challenge without a limit, count-tier challenge ignored.
5. Regression: the existing 853 Challenge cases and the persisted-tier cases stay green.

## 5. What this design does not claim
Whether retail completed Thayd Sprint at the indicator (inferred), what it did at zero tiers, how `GoalCount` is used for time tiers, and the reward-track credit for flag-8 medals are not witnessed. The two eligible
challenges are the only ones whose limits are exact; more become eligible only as limits are read (see the OCR pass 3 record when it lands).
