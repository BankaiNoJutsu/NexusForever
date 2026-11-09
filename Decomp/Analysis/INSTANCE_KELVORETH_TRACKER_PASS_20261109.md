# Ruins of Kel Voreth: objective tracker pass (2026-11-09)

Asked by the user: apply the Gauntlet tracker technique to the other instances (first target Kel Voreth). Evidence tier: **Observed** (retail-era clips, tracker frames viewed by the lead; the Carbine stream is 640x360 so some counts are partly legible, the 2015 clips are 1280x720) plus **Mapped** (PublicEventObjective rows for event 161, read by the lead). No retail parity claim; nothing run in a client.

## Clips read (all uploaded before 2018-11-28; resolved by URL)
| Clip | Date | Difficulty | Tracker frames read |
| --- | --- | --- | --- |
| "WildStar - Ruins of Kel Voreth Dungeon with Carbine" https://www.youtube.com/watch?v=PBqB8CiSaWk (MMORPG.com stream with Carbine staff) | 2014-06-19 | not stated, level-20 group | Blood Pit 330 s, Grond 600 s, Drokk 760 to 1500 s, last section 1700 to 3250 s |
| "Guide to [Veteran] Ruins of Kel Voreth Silver" https://www.youtube.com/watch?v=PMeSNlGd1tE (Vhalor Gaming) | 2014-06-16 | veteran (minimap "Digsite: Scar (Veteran)") | Drokk section 262 s, plus commentary at 250 to 320 s and 640 to 780 s |
| "Dungeon guide: Veteran Ruins of Kel Voreth" https://www.youtube.com/watch?v=VCM6cmoxlXg (SywoGG) | 2015-08-25 | veteran | 150, 330, 440, 1000, 1400 to 1525, 1540, 1750 s |
| "WildStar Ruins of Kel Voreth" https://www.youtube.com/watch?v=rymYV9wq8ok (Steve Mayne) | 2015-10-28 | normal (level-25 group, no "(Veteran)" on the minimap) | 180, 220, 300, 700, 2450 s |

## Observed tracker rows (Mapped to PublicEventObjective rows)
| Phase | 2014-06-19 Carbine | 2014-06-16 veteran | 2015-08-25 veteran | 2015-10-28 normal |
| --- | --- | --- | --- | --- |
| Blood Pit | only "[2/3] Fight your way through the Blood Pit" | not read | not read | "[2/3] Fight your way...", **Deathless in the Dungeon**, **Timer Bonus** |
| Grond | only "Defeat Grond the Corpsemaker" | not read | Defeat Grond, **Timer Bonus**, **Deathless** (challenge icon) | Defeat Grond, Deathless, Timer Bonus |
| Drokk | Defeat Slavemaster Drokk, (Optional) [x/11] Kill Mechano Slavers and Eldan constructs (448), (Optional) [x/18] End the suffering of Slaves (450), (Optional) Find and defeat Darkwitch Gurka (453), (Challenge) Dodging the Defense (847) | Drokk, (Optional) [1/6] Access hidden Eldan Data Storage (456), (Challenge) Dodging the Defense | Drokk, Timer Bonus, 448 [1/11], 453, 456 [1/6], Deathless, Dodging the Defense | Drokk, Deathless, Timer Bonus, 448 [3/11], 456 [1/6], the Gurka row (partly hidden), Dodging the Defense |
| Last section start | Trogun, 450 (carried), (Optional) [0/5] Kill Battlesworn and Darkwitch Osun (447) | commentary: "burn supplies, forges and battlesworn and darkwitch" | Trogun, Timer Bonus, 447 [0/5], 456 [5/6] carried, 461 [0/6] "Destroy Kel Voreth forges", Deathless | Trogun, 447 [3/5] |
| Later (near the forges) | **(Challenge) (9:59) [0/15] Osun elimination (846)** added between 1850 s and 1900 s, with the sub "this is the Exanite Forges" at 1881 s | not read | **(9:40) [0/15] Osun elimination** added about 1485 to 1525 s, ahead of the pack at the resurrection point | not read |
| End | only "Defeat Forgemaster Trogun" (2800, 3250 s) | not read | not read | Timer Bonus and Deathless rows gone by 40:56 on the top timer ("we didn't deathless", bronze medal) |

Counts match the table: 448 count 11, 450 count 18, 447 count 5, 456 count 6, 461 count 6, 846 count 15 with failure time 600000 ms (words 11 and 13 of the objective rows).

## Statements in the commentary (Observed from speech)
* Vhalor: at the start of the second section "you can get anywhere from one to four" optional objectives, "a total of four all the way through"; his own run got one (data storage) here and three later. Carbine's run sums to four (448, 450, 453, 447), but the 2015 veteran run lists 448, 453, 456 and then 447, 461 (five). The "total of four" is therefore only a 2014 statement and is **not** used.
* Carbine dev: "for all of the medals you must do all of the optional objectives"; optional objective XP is only granted when the dungeon finishes.
* SywoGG: "when we hit close to this resurrection point then our quests are going to be updated"; "we only got one extra, it's called Osun elimination".

## Table facts (Mapped, event 161 objective rows)
* Seven rows carry the flag word `0x2000004` and category 1: 447, 448, 450, 451, 453, 456, 461 (the optional rows). 457, 459, 869 carry flags 516 (`0x204`) and read as hidden/conditional rows; 459 "Disable the activated Exo-Lab defenses" was never seen in any of the four Drokk-section reads.
* 845 "Finish the entire Dungeon without anyone dying" (type 5, category 3, count none) and 3202 "Complete the Ruins of Kel Voreth within 40 minutes for bonus" (type 25 ScriptWithoutMax, no failure time) are never activated by the previous script; 846 "Kill as many Voreth Osun as you can before time expires" (KillTargetGroup 3972, count 15, failure time 600000 ms) was also unused. The engine already credits KillTargetGroup objectives generically on kills (`UnitEntity.cs`), so 846 only needs activating.

## What the previous script did, and what changed
Before: the Drokk phase always activated 453 and 847 and coin-flipped 448, 450, 456 and 459; the last phase coin-flipped 461, 447 and 451; none of 845, 846 and 3202 was activated.

Now (`RuinsOfKelVorethEventScript`, `TheExaniteForgesMessageTriggerScript`):
* Blood Pit phase activates **845 Deathless** and **3202 Timer Bonus** (both normal and veteran in the 2015 clips). Deathless is credited at the final boss only if no player died (`OnDeath`); the Timer Bonus is credited only if the script-side run time (accumulated in `Update`) is at most 40 minutes, because the row has no failure time of its own.
* Drokk phase: 847 always (all four reads); **453 moved into the random pool** (the 2014 veteran run shows it absent); **459 removed** from the pool (never observed). 448, 450, 453, 456 stay a provisional coin flip (the real selection rule is not known).
* Last phase: **447 always** (all four reads); 461 and 451 stay random. 846 is **not** activated at phase start.
* The **Exanite Forges trigger** (owner 449) activates 846 once when a player enters during the Trogun phase.
* Tests (46/0 across `RuinsOfKelVorethEventScriptTests` and `RuinsOfKelVorethTriggerScriptTests`): the previous expectations of unconditional 453 and a random 459 were superseded and replaced; new tests cover Deathless and Timer Bonus activation and credit, death handling, the 40-minute cut-off, 447 always on, 459 off, 846 not at phase start, and the trigger (in phase, out of phase, once).

## Not implemented, and why
* **A failed Deathless row disappearing**, and the Timer Bonus row disappearing at 40 minutes: the engine has no script-side objective-fail call (only a failure timer), so the rows remain until the event ends. This is an engine change, not a script change.
* **The random selection rule** for the seven optional rows (per-section counts, weights, whether it differs by difficulty): four runs give four different subsets.
* **The exact 846 trigger.** Both clips show it appears late in the last section, near the Exanite Forges; the trigger identity is Correlated, not read from a table. The 2015 veteran guide shows it slightly before the Carbine run's forge arrival.
* **450 "End the suffering of Slaves"** only appears in the 2014 Carbine run; it is kept in the pool.
* Veteran versus normal: the rows read in the 2015 normal and veteran runs are the same, so nothing is gated on difficulty here.

## Pattern for the other dungeons (not applied)
Every dungeon enum already defines a Deathless row and a Timer Bonus row (Stormtalon's Lair 842 and 3201, Skullcano 840 and 3203, Sanctuary of the Swordmaiden 1506 and 3204, Coldblood Citadel 5323 and 5322), and none of their event scripts activates them. That they also show from the first phase is **not** evidenced for those dungeons; each needs its own tracker read before copying this slice.
