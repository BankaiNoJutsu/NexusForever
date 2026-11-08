# Video witness V2: challenge tiers (Flitterfly Chaser), 2026-11-08

Goal: [VIDEO_WITNESS_GOAL_PROMPT.md](VIDEO_WITNESS_GOAL_PROMPT.md), question V2. Gates fed: prerequisite 70 "tier above Value" ([PREREQUISITE_CHALLENGE_TIER_20261107.md](PREREQUISITE_CHALLENGE_TIER_20261107.md)) and 5b challenge producers.
Evidence tier: **Observed** (one video) plus **Correlated** (three exact matches against the recovered Challenge/ChallengeTier/text tables). No code changed; no retail parity claim.

## Video
`Wildstar Tutorial - Everstar Grove - Challenges (Exile)/... (1080p_60fps_H264-128kbit_AAC).mp4` (tutorial series, Creative Commons gameplay footage; description lists #1 Flitterfly Chaser at 0:34 "Bronze: (5 Minutes) Catch 6 Flitterflies", #2 Guardian Crusher at 1:36 and #3 Barkblight Basher at 2:24, each "Bronze" only). The narration at 0:03 to 0:12 says each of the three challenges "is capped at the Bronze level, awards 100 challenge points and has a five minute time limit". Upload era not in the index (the UI is the late, Free-to-Play era HUD).

## Observed on screen
- 62 s: banner "Challenge Active!", a radial gauge with a clock, "5:00", counter "1 / 6", text "Collect flitterflies before time runs out"; tracker entry "Active (4:59) Flitterfly Chaser, General Challenge".
- 88 s: counter "5 / 6", time 4:34, the gauge shows a star medal instead of the clock.
- 93 s: banner "Bronze Completed: Flitterfly Chaser" with a "100 Challenge Points Earned" strip, a star with "Completed", and "Achievement Granted: Flitter Flyin'" (the challenge also feeds an achievement, as the narration says).

## Correlation with the recovered build-16042 tables (Challenge.tbl, ChallengeTier.tbl, en-US text)
- Challenge row 410 resolves to name "Flitterfly Chaser" and description "Collect flitterflies before time runs out": the description string equals the on-screen objective text.
- Its only populated tier column is ChallengeTier 929 with goal count 6: matches the "x / 6" counter. The other two tier columns are 0: matches "Bronze only".
- The 100 points are not in these two tables and were not correlated (the Challenge reward track 36 thresholds 300/600/900/1200 are the reward-track side, see [ZONE_REWARD_TRACK_35_EVIDENCE.md](ZONE_REWARD_TRACK_35_EVIDENCE.md)).

## Effect on the gates
- Type 70: nothing contradicts "tier above Value"; the footage shows tier completion arriving together with the goal reaching its count, which agrees with the server incrementing `CurrentTier` on tier-goal completion. It does not show the numeric tier value, and a single-tier challenge ends at completion, so the post-completion `CurrentTier` (1 or still 0) is **not** observed. Needed: a multi-tier challenge (Bronze then Silver then Gold) with the tracker visible across the transition.
- Producers (5b): the Everstar Grove set of three single-tier challenges is a retail-shaped content sample for later producer tests; odds are not observable.
- Still inferred: that the server's `CurrentTier` follows the client meaning during play.

## Method note
The three lookups needed a small reader for `Challenge.tbl` (record size 88, 22 columns, tier ids at columns 10 to 12) and `en-US.bin` (`XETL`, name text ids at columns 13 and 14); both formats are the ones the server's GameTable readers use. The scratch reader was not committed.
