# Video witness goal: using `I:\Videos` as retail Observed evidence (2026-11-08)

Created 2026-11-08 after [LEDGER_GATE_PASS_20261107E.md](LEDGER_GATE_PASS_20261107E.md) found the Source/SQLite lane with no unblocked slice. This goal opens a new evidence lane for the items that gate on "a retail witness"; it extends, and does not replace, [UNIFIED_ASSESSMENT_RETAIL_RESTORATION_GOAL_PROMPT.md](UNIFIED_ASSESSMENT_RETAIL_RESTORATION_GOAL_PROMPT.md) (same worktree, branch, push target, commit and test rules).

## What the library is
`I:\Videos` is about 1.2 TB, 6,878 indexed videos (`_INDEX\index.csv`, `INDEX.md`), of which 4,076 have subtitles (3.57 million cues). It is footage of the retail game (2014 to 2018: dungeons, raids, housing, paths, PvP, tutorials, patch previews, developer streams, quest walkthroughs). It is **not** packet-level: no wire bytes, no table rows, no server logs.

## Evidence tier rules (non-negotiable)
- A video moment is **Observed** at most. It becomes **Correlated** only when it agrees with a client-binary read or a table row; **Mapped/Verified** needs the usual independent corroboration (a second video from a different uploader and build era counts as one more Observed, not as Verified).
- Never implement a server behaviour from one video. Videos can: (a) falsify an inference (cheap and decisive), (b) supply a number or ordering that a client read or table row then confirms, (c) turn a "no witness" gate into "Observed, awaiting correlation".
- Build applicability: record the upload date and the patch era visible on screen. Videos older than the final build (16042) can show behaviour that later changed; say so. Commentary speech and descriptions are leads, not evidence; on-screen UI and combat/event behaviour is the evidence.
- No Computer Use, no native client launch. Frame extraction is `ffmpeg` (Panda3D 2013 build at `C:\Panda3D-1.10.16-x64\bin\ffmpeg.exe`, enough for `-ss -i -frames:v 1`), viewed with the image reader.
- Do not copy videos or frames into the repository; records carry file name, upload date, URL (from the index), timestamp and a prose description of what is seen.

## Tooling (built)
[scripts/video_corpus.py](scripts/video_corpus.py): `build` (FTS5 corpus at `I:\tools\video_index\corpus.db`, derived and not committed), `find "<fts query>" [--limit N] [--type T]`, `frame "<relative file>" <seconds>`.

## Witness questions to answer (priority order; each needs a concrete finding or an exact "not visible" note)
| # | Gate it feeds | Question the footage can answer | Search leads |
|---|---|---|---|
| V1 | Public 07CE / reward tracks (queue item 1, 5a) | How many milestones does a zone reward track show, what point values, is there a claim button, how are unclaimed milestones and the Signature 25% bonus presented, what happens at the end of the period | "reward track", "reward points", Arcterra patch preview, F2P perks video, Livestream @5056s |
| V2 | Type 70 / challenge producers (5b) | Does the challenge tracker show a tier number rising during play (Value < CurrentTier semantic), what reward quality appears at each tier, does the first reward appear at tier 0 | Everstar Grove / Crimson Isle "Challenges" tutorials, "gold/bronze tier", "challenge" |
| V3 | Public-event prerequisites 107/119/122/199/200 | What do the event tracker UI and contribution/participation displays show (contribution tiers, join/leave, completion states) | "public event" (99 videos), world-boss and zone-event footage |
| V4 | Telegraph shapes (5i) | Shape 6 hit area and box extents, directly measurable against the arena floor | "telegraph" (594 videos), boss fights |
| V5 | Quest reward type 10 (5j) and Q5597 smoke (5h) | Reward panel contents for quests whose reward row carries type 10; Q5597 objective order and spawn locations | quest walkthroughs; titles naming the quest |
| V6 | Taxi 604, Hycrest triggers (2e, 5g) | Travel path/time, trigger placement | "taxi", Hycrest / Algoroc / Northern Wilds series |
| V7 | AMP / healing / piercing (5c-5e) | Visible damage/heal numbers for clean A/B cases; only as a falsifier for inferred formulas | class gameplay, only if a clean controlled case exists |

## Loop (repeat per question)
1. Query the corpus; list candidates with dates; pick at most 3 informative videos.
2. Extract frames at the hit moments (and a few seconds either side), read them, write the observation.
3. Compare with what the server and the client reads currently claim. State: confirms, contradicts, or adds a number.
4. Write a focused record `Decomp/Analysis/VIDEO_WITNESS_<topic>_<date>.md` (question, videos with date/URL/timestamp, what is seen, tier, effect on the gate) and a dated note at the top of `ASSESSMENT_REMAINING_WORK.md` linking it.
5. Only when an Observed finding **falsifies** current behaviour, or **agrees with a client/table read**, change code: failing test first, owning project, affected tests only. Otherwise update the gate text with the sharper witness.
6. Commit explicit paths with the `Co-Authored-By: Claude Sonnet 5.5 <noreply@anthropic.com>` line, `git push origin HEAD:legacy-di-storefront-hardening` (normal pushes, fetch first), then the next question.

## Stop condition
Every row V1 to V7 has either a record with a concrete finding, or an exact statement of what footage would be needed and why the 6,878 videos do not contain it (with the queries tried). Not completion of retail restoration, and not a parity claim.
