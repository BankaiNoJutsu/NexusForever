# V8: the challenge reward chooser is retired in build 16042 (2026-11-08)

Goal: [VIDEO_WITNESS_GOAL_PROMPT.md](VIDEO_WITNESS_GOAL_PROMPT.md), item V8; opened by [CHALLENGE_TIME_TIERED_20261108.md](CHALLENGE_TIME_TIERED_20261108.md). Gate 5b (retail odds).
Evidence tier: **native read (no code references) + extracted Lua + text table + dated retail footage (Observed)**. No code change; the conclusion is that none is needed for build 16042. No retail parity claim.

## What the footage shows (dated retail)
- 2014-06-16, `Wildstar- Top of The World Challenge Easy Way`: after a Bronze finish the card reads "You reached the Bronze tier! Select your desired reward to increase the chance of getting that item by 2x!" with a "Best Odds" row of loot icons each marked "2x", then a "Spinning!" panel (roulette) and the loot.
- 2015-01-25, `Wildstar - Challenge - Back to Basic - Gold`: the Gold card says "... by 10x!" with the columns "Best Odds", "Average Odds", "Good Luck!".
- The multiplier therefore follows the tier: Bronze 2x, Gold 10x (Silver not seen; likely in between). The 2013 beta stream used "2x" without naming the tier.

## What the final build contains
- The text table of build 16042 still holds the strings: id 439195 "You reached the %s tier! Select your desired reward to increase the chance of getting that item by %.fx!", 446206 and 446212 (the `$1n`/`$2cx` variants), 534445 "Best Odds", 534219 "Average Odds", 88587 "Good Luck!". Vestigial text.
- The extracted addons have no chooser window. `ChallengeTracker.lua` registers `ChallengeRewardListReady` and `ChallengeRewardReady`, but `OnChallengeRewardReady` has no definition, `OnChallengeRewardListReady` only rebuilds the tracker row, the context menu's "Loot Rewards" button is created with `Enable(false)`, and `LootChallenge` only clears a local table entry. The completion card shows points (`Challenges_Points`) from `ChallengesLib.GetRewardTrackInfo`, which is the reward-track system dated 2016 (Arcterra preview).
- Natively, a string search finds **no code reference** to either `ChallengeRewardListReady` or `ChallengeRewardReady` (only data-table entries), while `ChallengeTierAchieved` has its code reference in the result consumer `FUN_14048e2b0`. So the final client never raises the chooser events.

## Conclusion
The loot chooser (2013 to 2015 eras) was replaced by reward-track points before build 16042; the server's lack of a reward-list/choice message is consistent with 16042 and needs no work. For odds of the old loot tables the multipliers above (Bronze 2x, Gold 10x) are historical only and must not be applied to 16042. Remaining 5b question (the reward-track points per tier and their multipliers) is a separate, existing item.
