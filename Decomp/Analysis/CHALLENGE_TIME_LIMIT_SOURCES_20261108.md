# Time-tiered challenge limits: online source search (2026-11-08)

Goal: [VIDEO_WITNESS_GOAL_PROMPT.md](VIDEO_WITNESS_GOAL_PROMPT.md), next-queue item 1; unblocks [CHALLENGE_TIME_TIERED_20261108.md](CHALLENGE_TIME_TIERED_20261108.md). Evidence tier: **leads only** (community pages and unofficial repositories, reported by a research subagent and not independently re-fetched by me except where stated); nothing here changes code. No retail parity claim.

## Result
No bulk source of per-challenge time limits exists on the open web. Of the 80 flagged challenges, 2 got a usable limit and both agree with dated footage; the rest have nothing.

| id | name | limit | source (date) | confidence |
|---|---|---|---|---|
| 1002 | Thayd Sprint | 2:00 | nexusnuggets.wordpress.com/2014/06/24/thayd-challenges/ (post 2014-06-24) | medium; equals the footage (2:00) |
| 546 | Powering Down | 3:00 (a comment says Gold needs 2:00 or less elapsed, i.e. at least 1:00 remaining, equal to goal 60000) | Jabbithole challenge page comment, Wayback snapshot 2015-10-13 | medium-low (player comment); consistent with the minimum-remaining reading |
| 979/981 | Doing Things The Hard Way | none (a comment says "under a minute") | Jabbithole, search snippets only | low; does not contradict the 20:00 footage reading |

- Jabbithole lists "Timed: Yes" with no duration for 753 Top of the World, 166 Windrunner, 470 Kickin' It, 477 Splashin' Dash, 923 Entrance Exam, 957 Just Keep Swinging, 743 Grounding the Storm, 690 Peep Chase, 559 One Small Hop, 22 Treeclimber and 47 Supply Run (two of these confirmed from archived pages).
- The Fandom WildStar wiki (MediaWiki API, about 225 challenge pages and 108 patch pages) shows bronze/silver/gold fields equal to the raw client goals as mm:ss (Thayd Sprint 00:00 / 01:00 / 01:10, page revision 2014-10-17): no independent thresholds and no limit. Its Challenge page (revision 2016-07-14): failure by timeout, leaving the area or abandoning, then a 30-minute cooldown.
- Patch notes: the 2014-07-01 patch raised Snoglug Squasher and Shiny Snacks from 3 to 5 minutes (limits were per challenge and tuned, neither is on the flagged list); the 2016-04-13 patch fixed countdown challenges giving the wrong points at Gold and Silver.
- Client addon source (public Carbine addon mirror): the displayed limit is `GetDuration()` from the server's `TimeTotalActive`, and the elapsed threshold per tier is `GetDuration() - goal`; this agrees with my reading of the extracted Lua.

## Emulator code
- Upstream NexusForever: PR 498 "Challenges - messages only" (merged 2025-05-27) adds the structs; `ServerChallengeUpdate.TimeTotalActive` is commented "varies, most commonly 5 minutes", `TimeTotalCooldown` "typically 30 minutes", `TimeTotalAreaFail` "typically 10 seconds". No manager, no per-challenge durations.
- Fork `th3bluecarp/nexus-reborn` (`ChallengeManager.cs`): flat 5 min active, 30 min cooldown, 10 s grace; its comment says per-challenge values are not in the client data; it grades tiers as `elapsed <= goal` when all goals are at least 1000, which reads the goals as limits and is the inverse of the client rule (grep reading only). Treat it as a warning, not a reference.
- Other emulators and tools (bezgelor JSON exports, WildSandbox structs, others) carry no durations.

## Gaps and cautions
About 77 of the 80 challenges have no information; ids 314 (goals 2683995 to 2683997, placeholder-like) and 244 to 247 look unusual (goals 10000, 20000, 30000; type 0 and 2). The community pages are leads: use them only to corroborate a footage reading. The wiki dump is saved only in the session scratchpad and is not committed.
