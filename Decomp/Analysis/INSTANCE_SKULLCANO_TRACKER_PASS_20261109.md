# Skullcano: objective tracker pass (2026-11-09)

Follow-up to [INSTANCE_KELVORETH_TRACKER_PASS_20261109.md](INSTANCE_KELVORETH_TRACKER_PASS_20261109.md) and [INSTANCE_STORMTALON_TRACKER_PASS_20261109.md](INSTANCE_STORMTALON_TRACKER_PASS_20261109.md). Evidence tier: **Observed** (retail-era clips, tracker frames viewed by the lead, 1280x720) plus **Mapped** (PublicEventObjective rows for event 148, read by the lead). No retail parity claim; nothing run in a client. Only one structural clip was usable for the middle of the dungeon, so every "observed" row below is a single run unless stated.

## Clips read (uploaded before 2018-11-28; resolved by URL)
| Clip | Date | Use |
| --- | --- | --- |
| "Wildstar - Gold dungeon run: Skullcano" https://www.youtube.com/watch?v=Q9fNxG9IK1Q (SywoGG) | 2015-09-04 | veteran gold run; tracker read at 60, 120, 300, 420, 660, 900, 1050, 1200 s. **Older build text**: see below. |
| "What Really Happens in a Veteran Dungeon / WildStar / Veteran Skullcano" https://www.youtube.com/watch?v=bz3a5NcbGoc (LamixFace) | 2016-12-06 | veteran, 57:25 on the top timer, last-boss tracker only (100 s frame) |
| "Prime 10 Skullcano | Wildstar" https://www.youtube.com/watch?v=uU2DkevB5dA (badummtish) | 2017-07-16 | Prime difficulty (different tracker layout and rows); read for the timer text only, not used for the script |
| "Dev Plays Skullcano" https://www.youtube.com/watch?v=y7O-eCouQIc (official WildStar channel) | 2016-09-16 | read and **unusable**: the group runs "Rallied" with the events tracker collapsed in every frame sampled |

## Build dating (Observed, important)
The 2015-09-04 gold run shows a countdown row "(44:14) Remaining for Silver or Gold medal", counting down from 45:00 in step with the top timer (43:14 at 1:49). **No such string exists in the 16042 text table**; the row 3203 in 16042 reads "Timer Bonus" (short text) and "Complete Skullcano within 45 minutes for bonus points towards a medal." (long text), and the clips from October 2015 onward (the Kel Voreth normal run, the Stormtalon normal guide, the 2017 Prime run) show the static "Timer Bonus" form. The same clip also shows "Rid Skullcano of Redmoon Marauders" with a count of 40 where the 16042 row has 35. So this clip predates the final tracker text and counts; it is used here only for **which rows appear when**, not for text, counts or the countdown form. (The Kel Voreth and Stormtalon reads used clips from 2015-08-24 onward that already show "Timer Bonus", consistent with 16042.)

## Observed tracker rows, 2015-09-04 live veteran gold run (Mapped to PublicEventObjective rows)
| Time | Phase | Rows shown |
| --- | --- | --- |
| 60 s | Enter | Defeat Thunderfoot (322), the medal timer row, (Optional) **Defeat Stew-Shaman Tugga** (321), **[0/8] Free captured Lopp** (335), **Deathless in the Dungeon** (840), challenge **Stew-Shaman Tugga's Diet** (941) |
| 120 s | Enter | Defeat Thunderfoot, timer, [5/8] Free captured Lopp, Deathless (Tugga rows gone) |
| 300 s | Chasm | **[Waiting for 5 more] Cross the Lava-Filled Chasm** (329), timer, **Avoid Laveka's attacks** (839), Deathless. No Primal Fire Essences (337) and no "find a more efficient way" row (364). |
| 420 s | Bosun | Defeat Bosun Octog (323), timer, **[1/40] Rid Skullcano of Redmoon Marauders** (334), **[3/13] Free the Redmoon Prisoners** (336), Deathless |
| 660 s | Bosun | Defeat Bosun Octog, timer, [9/40] Rid Skullcano, Deathless, challenge **Bury the Bosun** (943) |
| 900 s | Get to Redmoon | **[Waiting for 5 more] Reach the Eldan Terraformer** (376), timer, **[0/5] Gather Shiny Objects of pure gold** (333), [22/40] Rid Skullcano (carried), **[2/3] Hack the Redmoon's Cluster Missile Consoles** (365) |
| 1050 s | Get to Redmoon | Reach the Eldan Terraformer, timer, [1/5] Gather Shiny Objects, [32/40] Rid Skullcano, Deathless, **(8:35) Mordechai Redmoon and the Terraformer** (841, 10 minute timer) |
| 1200 s | Redmoon | Defeat Mordechai Redmoon (324), timer, Deathless, (6:05) Mordechai Redmoon and the Terraformer, challenge **Like Starin' at the Sun** (944) |

2016-12-06 veteran run, 57:25 on the top timer (last boss): "Defeat Mordechai Redmoon" and "[3/5] Gather Shiny Objects of pure gold" only; the timer, Deathless and Terraformer rows are gone (45-minute limit passed, party deaths, 10-minute challenge expired), consistent with those rows being removed on failure.

2017-07 Prime 10 run (not used for the script): a different tracker layout with "Timer Bonus", a "Kill Enemies 0%" bar (row 5345), "Free captured Lopp [0/8]" in the first phase, "Bury the Bosun" in the Bosun phase, and "Find Quartermaster Gruh'ar and take his stash" as an optional row in the final approach; no Deathless row read and no "Rid Skullcano" row.

## What the previous script did, and what changed
Before: the Enter phase activated 322, 321 and coin-flipped 335; the chasm phase always activated 329, 364, 337 and 839; the final boss only finished the event; none of 840 and 3203 was activated.

Now (`SkullcanoEventScript`):
* Enter activates **840 Deathless** and **3203 Timer Bonus**, with 322 and 321.
* **337 Gather Primal Fire Essences moved into the provisional random pool** (the one chasm-phase read shows it absent).
* At the final boss: 840 is credited only if no player died (`OnDeath`), 3203 only if the script-side run time (`Update`) is at most **45 minutes** (the row text; Kel Voreth and Stormtalon use 40).
* The rest of the script already matched the one run read: 329 with the player count, 839 in the chasm phase, 323 with the 334 and 336 pool, 376 and 333 and 365 in the final approach, 841 after the Gruh'ar kill.
* Tests (58/0 across `SkullcanoEventScriptTests` and `SkullcanoTriggerScriptTests`): the previous expectation that 337 is always activated was superseded; new tests cover Deathless and Timer Bonus activation and credit, death handling, the 45-minute cut-off and the Primal Fire pool. Written together with the code, not run red first.

## Not implemented, and why
* **Failed rows disappearing** (Deathless after a death, Timer Bonus after 45 minutes): no script-side objective-fail call in the engine (same limit as the other dungeons).
* **Challenges 941 Tugga's Diet, 942 Jump Jump Around, 943 Bury the Bosun and 944 Like Starin' at the Sun**: they appear at fight start in the one run read (941 at 60 s, 943 at 660 s but not at 420 s, 944 in the Redmoon phase) and need boss-fight mechanics (diet stacks, jump detection, spore stacks, terraformer blinding) that do not exist in scripts. Not activated, to avoid uncreditable rows. 942 was not seen at all.
* **Which optional rows appear**: one run gives one set (Lopp, Prisoners, Gold Objects, Missile Consoles present; Primal Fire absent); the pool stays a random choice. The cave route (Find Chief, Escort, Molten Cavern heat) was not in the one run read, so its rows are untouched.
* **Prime difficulty** (the Kill Enemies bar, no Rid Skullcano row, Gruh'ar as an optional row) is a different difficulty and is not modelled.
* **Counts**: the live Sept 2015 clip shows 40 marauders against 35 in the 16042 row; the script uses the table row, not the clip.

## Pattern for the remaining dungeons
Sanctuary of the Swordmaiden (1506 and 3204, 60 minutes) and Coldblood Citadel (5323 and 5322, 40 minutes) still define a Deathless and a Timer Bonus row that no event script activates. Kel Voreth, Stormtalon and Skullcano show them from the first phase in the clips read; each of the remaining two needs its own tracker read before copying the change.
