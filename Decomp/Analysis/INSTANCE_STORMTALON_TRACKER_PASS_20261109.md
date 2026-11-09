# Stormtalon's Lair: objective tracker pass (2026-11-09)

Follow-up to [INSTANCE_KELVORETH_TRACKER_PASS_20261109.md](INSTANCE_KELVORETH_TRACKER_PASS_20261109.md). Evidence tier: **Observed** (retail-era clips, tracker frames viewed by the lead, 1280x720) plus **Mapped** (PublicEventObjective rows for event 145, read by the lead). No retail parity claim; nothing run in a client. Only two clips were read, one per difficulty, so every "always" below means "in both reads".

## Clips read (uploaded before 2018-11-28; resolved by URL)
| Clip | Date | Difficulty | Tracker frames read |
| --- | --- | --- | --- |
| "Wildstar - Dungeon guide: Veteran Stormtalon's Lair" https://www.youtube.com/watch?v=T2OJbSH7aD8 (SywoGG) | 2015-08-24 | veteran (minimap "Stormtalon's Lair (Veteran)") | 60, 75, 90, 200, 420, 540, 600, 800, 1000, 1300 s |
| "Wildstar Stormtalon's Lair Guide (Normal)" https://www.youtube.com/watch?v=_Y0MWGDS8QU (TinyBlueGames) | 2015-10-08 | normal (level-20 group, no "(Veteran)") | 100, 300, 450, 700, 950 s |

## Observed tracker rows (Mapped to PublicEventObjective rows)
| Phase | Veteran 2015-08-24 | Normal 2015-10-08 |
| --- | --- | --- |
| Enter (zealot rush) | 60 s: **[11/12] Survive the Thundercall Pell Zealots** (539), **Timer Bonus** (3201), **[0/15] Gather Tainted Stem samples** (554), **Deathless in the Dungeon** (842), **(14:48) [9/27] Kill as many Thundercall Pell as you can before time expires** (843). No "Eliminate Thundercall Pell" row. | not read |
| Blade-Wind | 75 s: Defeat Blade-Wind, Timer Bonus, 554, **[0/19] Eliminate Thundercall Pell** (557), **[0/8] Hijack power from the Thundercall Storm Totems** (562), Deathless, 843 (14:33 [12/27]) | 100 s: Deathless, Timer Bonus, 843 (13:22 [14/27]), **[2/19] Eliminate Thundercall Pell**, **[0/8] Hijack power**, Defeat Blade-Wind; no stem-samples row |
| Aethros | 420 to 800 s: Eliminate Aethros, 554, **Defeat Arcanist Breeze-Binder for the encryption key** (556), 557, **[x/6] Disable the Thundercall Pell with Grenades** (561), 562, Deathless, 843, **Tornado Chasing** (844); at 800 s two **Swift Sweeper** challenge rows (2381 and a red 0:08 countdown, probably 2487, 15 s) | 450 and 700 s: Deathless, Timer Bonus, 843 (5:27 [23/27]), 557, 562, **[1/5] Free the Thundercall Sacrificial Prisoners** (464), 561, Eliminate Aethros, 844, 556 |
| High Priest | 1000 s: **[Waiting for 5 more] Stop the Thundercall High Priest** (540), Timer Bonus, 556, 561 [5/6], 562 [7/8], Deathless | not read |
| Stormtalon | 1300 s: Destroy Stormtalon, Timer Bonus, Deathless, **Lightning Reflexes** (2382) | 950 s: Timer Bonus, 562 [7/8], Destroy Stormtalon (no challenge row; the Deathless row was already lost to a death) |

Counts match the table: 554 count 15, 557 count 19, 562 count 8, 561 count 6, 464 count 5, 843 count 27 with failure time 900000 ms (15 min), 539 count 12. The veteran guide's speaker says "every dungeon has deathless in the dungeon, basically if you die then you lose this challenge"; the normal guide's speaker says the 27-kill Pell challenge is probably the only part of the dungeon you might not finish on time and that it does not fail the dungeon.

Early 2014 observation (not re-read): the 2014-06-09 Blade-Wind clip showed "Stormchaser" (2380) in the Blade-Wind phase; neither 2015 clip shows it.

## What the previous script did, and what changed
Before: the Enter phase activated 539 and 557 and coin-flipped 843, 554, 558 and 4867; Blade-Wind coin-flipped 555, 562, 559; Aethros coin-flipped 464 and 561 and chose 556 or 828; none of 842, 844 and 3201 was activated.

Now (`StormtalonsLairEventScript`):
* Enter activates **842 Deathless**, **3201 Timer Bonus** and (always) **843**, with 539; **557 moved out of Enter into the Blade-Wind phase** (it appears with the Blade-Wind row in both reads, at 0/19 after the zealot kills).
* Aethros phase activates **844 Tornado Chasing** (both reads). It is display only: no tornado-hit detection exists, so it is never credited.
* At the final boss: 842 is credited only if no player died (`OnDeath`), 3201 only if the script-side run time (`Update`) is at most 40 minutes (the row has no failure time of its own).
* 554, 555, 558, 559, 561, 562, 464, 4867 and the 556/828 choice stay in the provisional random pool.
* Tests (50/0 across `StormtalonsLairEventScriptTests` and `StormtalonsLairTriggerScriptTests`): the two previous expectations that 557 is activated in the Enter phase and that 843 is optional were superseded; new tests cover Deathless and Timer Bonus activation and credit, death handling, the 40-minute cut-off, 557 in Blade-Wind, and 844. Written together with the code, not run red first.

## Not implemented, and why
* **Failed rows disappearing** (Deathless after a death, Timer Bonus after 40 minutes, Tornado Chasing after a tornado hit): no script-side objective-fail call in the engine (same limit as Kel Voreth).
* **Swift Sweeper** (2381 and the 15 s row 2487) and **Lightning Reflexes** (2382, veteran Stormtalon phase only in the one read): their start and credit conditions depend on Gusts of Aethros and Lightning Rod mechanics that do not exist in scripts. Lightning Reflexes was not seen in the normal read, so its difficulty gating is unknown.
* **Stormchaser** (2380): seen once in 2014, absent in both 2015 reads; not activated.
* **Which optional rows appear**: two runs give two different sets (554 and 556 in the veteran run; 464 and 556 in the normal run, with 554 absent). Whether 562 and 561 are really always present (present in both reads) or random is not settled. The hidden-flag rows 558, 559 and 4867 (flags 516) were never seen in either run and are left as they were, unlike Kel Voreth's 459 which had four reads.
* The earlier statement that the optional pool has "four in total" (Kel Voreth commentary) was not tested here.

## Pattern for the remaining dungeons
Skullcano (840 and 3203), Sanctuary of the Swordmaiden (1506 and 3204) and Coldblood Citadel (5323 and 5322) still define a Deathless and a Timer Bonus row that no event script activates. Kel Voreth and Stormtalon both show them from the first phase in both difficulties in the 2015 clips, but each remaining dungeon needs its own tracker read before copying the change.
