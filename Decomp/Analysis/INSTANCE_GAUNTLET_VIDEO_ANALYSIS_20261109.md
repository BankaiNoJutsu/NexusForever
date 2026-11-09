# The Gauntlet: video analysis of the score tokens, gold medal timer and Splorg (2026-11-09)

Asked by the user: "check for gauntlet videos, analyse them and implement what can be". Evidence tier: **Observed** (retail-era clips, frames viewed by the lead; all three clips are 640x360 so digits are read from upscaled crops and a few are uncertain) plus **Mapped** (client tables, read by the lead). No retail parity claim; nothing run in a client.

## Videos
The corpus holds 27 videos mentioning the Gauntlet; the Shiphand expedition clips with subtitles (all uploaded before 2018-11-28) are AB-Gaming 2014-06-14, Elloa 2014-06-22, iBluair 2015-10-28, Kal Scattergood 2016-02-23, Maximus Grahamicus normal and veteran 2016-06-12, BezScemy 2017-01-21 and three Maximus class playthroughs 2017-07. Skullcano "fire gauntlet" and Eldan Gauntlet gear clips are unrelated. Files are under `I:\Videos\_Additional`.

## New table finding (Mapped)
* Creature2 **49577 "Gauntlet Score Token"** exists (the earlier name search missed it). Its activate spell (Creature2 word 15) is Spell4 **47579 "Collect Token"**, cast time 750 ms, effects RavelSignal (0x51, ids 16935 and 17354) twice and DespawnUnit (0x61). Creature2 **49375 "Golden Skull"** also exists but has no activate spell (word 15 is 0).
* PublicEventObjective rows (event 446, words read by the lead): 4670 type Script (5) count 20, ObjectId 6867; 4707 type ScriptWithoutMax (25), failure time 1,200,000 ms; 1967 TimedWin (13) failure time 132,000 ms; 1957 CollectItem (2) count 10, ObjectId 0; 1974 VirtualCollect (22) count 100, ObjectId 0; 1942 type 25 count 100, ObjectId 6867.
* World 2183 has only three `entity` rows in the runtime world data, and none is a token or skull.

## Observed (tracker frames and subtitles)
| Clip and time | What the tracker or narration shows |
| --- | --- |
| Kal Scattergood, Gold Expeditions (veteran, https://www.youtube.com/watch?v=vasW_FW_Fvw), frame at about 72 s | Four lines under "Expedition: The Gauntlet", all marked Optional: "[15:30] Remaining for Gold medal", "[5/10] Collect Golden Skulls", "[0:55] Survive the Splorg", "[18/20] Collect Gauntlet Score Tokens"; a "Collect" interaction prompt on a token. Minimap label "Chamber of Choices (Veteran)". |
| Same clip, narration 54 to 62 s | "in this section you need to interact with 20 gauntlet score tokens ... this number remains the same whether you're soloing or in a group ... a total mess of murderous RNG" and "assign people to quadrants"; tokens vanish when the Splorg despawns. |
| Same clip, narration 28 to 33 s | "collect 10 golden skulls, more than 10 will spawn but they're always in different places". |
| Maximus veteran (https://www.youtube.com/watch?v=Z1I3zqwC_Kc), 100 s | "(Optional) [19:01] Remaining for Gold medal", "[0/3] Activate Switches" (not optional), "(Optional) [1/10] Collect Golden Skulls" in "The Proving Ground (Veteran)". |
| Same, 260 s | "[16:2x] Remaining for Gold medal", "[5/10] Collect Golden Skulls", "Survive the Splorg", "(Optional) [5/20] Collect Gauntlet Score Tokens" in "Chamber of Choices (Veteran)". |
| Maximus normal (https://www.youtube.com/watch?v=Hm1bB3KeWAI), 123 s and 133 s | "[0/3] Activate Switches" and "(Optional) [0/10] Collect Golden Skulls" only: **no gold medal line**. Narration at 89 s: "grab 10 golden skulls is an optional objective". |
| Same, 240 s | "(Optional) [4/10] Collect Golden Skulls" and "(Optional) Survive the Splorg" with a countdown: **no token line, no medal line**. |
| Same, 420 s | "[6/10] Collect Golden Skulls", "(Waiting for 1 more) Enter the Charnel Chamber", "(Optional) [0/3] Save Gauntlet Contestants from the Darkspur". |

Conclusions: the gold medal countdown (4707, 20 minutes) is shown from the first room in the **veteran** run only; the 20-token line (4670) appears during the Splorg in the veteran run only; neither is in the normal run; the separate 100-count gauge (1974) and "Current Scoring" (1942) are not shown in any frame; Survive the Splorg is an Optional row with a countdown in both difficulties.

## Implemented (provisional where stated)
* `GauntletEventScript`: **veteran only** (difficulty read from the players in the instance, `InstanceDifficulty`), activate 4707 from the first early phase that has a player (tried at each early phase so it starts once), activate 4670 in the Splorg phase, and credit 4707 just before `Finish` (the engine ignores the update if the 20 minutes expired). **1974 is no longer activated** (no frame shows it). Tests: five new in `GauntletEventScriptTests`.
* **Bug fix found on the way:** Survive the Splorg is a TimedWin row, and the engine fails a TimedWin objective at expiry unless a controller calls `EnableTimedWinCompletion` (the Hycrest script does). The Gauntlet script advances only on success, so after the 132 s timer the run would have stalled. The Splorg phase now opts the row in (test added).
* `GauntletScoreTokenEntityScript` (Creature2 49577): on activation credits 4670 once and removes the token, same pattern as the Cargo Crate script. Tests: filter binding and once-only credit.
* Tests run: `FullyQualifiedName~GauntletEventScriptTests` passed 43/0 (existing 36 plus 7 new). New tests were written with the code, not run red first.

## Not implemented, and why
* **Token spawns**: where and when the 20 tokens appear (random, per quadrant, solo or group) is not in any table or clip read; world 2183 has no token rows. Without spawns the 4670 row cannot be completed in a client.
* **Golden Skulls (1957)**: Creature2 49375 has no activate spell and the pickup mechanism ("jump to collect") is not shown by any clip read; the random spawn pool (more than 10, three to four in the first room) has no positions. Nothing was written.
* **1942 Current Scoring and the RavelSignal ids 16935 and 17354** that the token spell sends: no consumer known.
* Arena boss abilities remain unreadable (no named abilities in any clip); the swarm-pit and airlock triggers are still guessed positions.
* The start time of 4707 is inferred (visible at 19:01 about 60 s into the veteran clip, with iBluair saying the timer ticks during the opening cutscene); it is activated at the first early phase, not at a verified instant.
