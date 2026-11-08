# OCR sweep of the dated retail challenge footage: time limits (2026-11-08)

Goal: [VIDEO_WITNESS_GOAL_PROMPT.md](VIDEO_WITNESS_GOAL_PROMPT.md), next-queue item 1; follows [CHALLENGE_TIME_TIERED_20261108.md](CHALLENGE_TIME_TIERED_20261108.md) and [CHALLENGE_TIME_LIMIT_SOURCES_20261108.md](CHALLENGE_TIME_LIMIT_SOURCES_20261108.md).
Evidence tier: **Observed** (dated retail footage, OCR-read countdowns, key frames re-viewed by the lead). No code change; no retail parity claim.

## Method
All 160 dated (before 2018-11-28) retail videos whose title or description mentions "challenge" (54 hours) were sampled every 2 to 30 seconds (at most 120 frames per video) and read with the Windows built-in OCR (`scripts/ocr_server.ps1`, driver `scripts/ocr_challenges.py`, analysis `scripts/ocr_analyze.py`; results under `I:\tools\video_index\ocr`, not committed). A running countdown keeps `frame time + remaining` constant; 82 such clusters in 65 videos were found and 18 carry a name from the 80 flagged challenges. The first reading of a cluster is a **lower bound** of the limit (the widget can be visible before the first OCR hit); a limit is called exact only when several independent videos agree on the first reading or the video shows the activation banner. Most clusters belong to other timers (explorer missions, quests, unflagged challenges); the 160 titles include about 60 housing "Custom-Built Decor Challenge" and "Wildman Challenge" playthrough videos that contain no relevant countdown.

## Limits observed
| Challenge | Limit | Basis |
|---|---|---|
| Thayd Sprint (1002) | **120 s** | 2014-06-05 video 585 (first reading 1:58); and the 2018-06-07 final-era video 6764: main timer 1:57, tier-loss timers 0:47 and 0:45 a few seconds apart (see below) |
| Back to Basic (956) | **about 65 s** | 2015-01-25 video 1731 (first visible 1:04, readings 1:03 at 49 s) |
| Top of the World (753) | **80 s** | five dated videos (1886, 1932, 1969, 2471, 2992) agree on a first reading of 78 to 79 s; replaces the earlier estimate of about 90 s |
| Doing Things The Hard Way (979) | **about 1200 s (20:00)** | 2014-06-16 video 1823 (first reading 19:55 at 39 s) |
| Grounding the Storm (743) | **about 120 s** | 2014-06-22 video 1887 (first reading 1:58) |
| Ravenok Racer (511) | **about 60 s** | 2014-04-23 video 6641 (first reading 0:58 at 3 s) |
| Race to the Unknown (680) | **at least 111 s**, probably 120 | 2014-06-27 video 6793 (frames at 37 and 41 s read 1:50 and 1:46; the widget is visible earlier) |
| Up the Falls (481, not time-tiered: flags 4, goal [1]) | **120 s** | 2014-07-29 video 1826 (01:59 at the "Challenge begins!" banner). A single-tier challenge also has its own limit, so limits are per challenge across the whole table, not only for the 80 flagged ones |
Rejected clusters (wrong challenge): video 5343 "Wildstar PvP Challenge", video 6593 "Spirited Snowdrift Sprint" (a different Arcterra challenge; its banner read gives about 30 s, not matched to a table row), video 6721 (Malgrave farm video, a 782 s timer of unknown origin).

## Confirmation of the tier rule in the final-era HUD
The 2018-06-07 Thayd Sprint clip (retail, final-era UI, in-game chat channel "Nexus") shows the main countdown and, under it, **time-until-the-tier-is-lost counters** (0:47, 0:45, 0:43 ... falling in step with the main timer). With limit 120 s, goal 70000 ms and about 3 s elapsed the rule `limit - goal - elapsed` gives 47 s: this is exactly the addon code in `ChallengeDisplay.lua` (`nTierTimeRemaining = max(0, nTimeLimit - nTierLimit - nTimeElapsed)`), so the minimum-remaining rule and the 120 s limit are confirmed by one frame of final-era footage. (In the 2014 HUD the threshold list was a static `:00 / 1:00 / 1:10`; only the final-era widget counts down.)

## Effect
Eight challenges now have an observed limit (seven time-tiered ones plus one single-tier row), against 80 flagged rows and 643 challenges overall; the server's single 300 s default is wrong for every one of them (60 s to 20 min). Implementation is still gated on completion triggers ([CHALLENGE_TIME_TIERED_TRIGGER_CENSUS_20261108.md](CHALLENGE_TIME_TIERED_TRIGGER_CENSUS_20261108.md)) and on limits for the remaining challenges.

## Still missing
Limits for the other flagged challenges with no usable footage (about 60 of 80); a clean read of Race to the Unknown's start; an independent video of Grounding the Storm and Ravenok Racer. A second OCR pass restricted to the widget region (an upscaled crop of the top-right area) would raise the read rate (about half of the frames were read).
