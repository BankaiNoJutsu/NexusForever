# Sanctuary of the Swordmaiden: objective tracker pass (2026-11-09)

Follow-up to the [Kel Voreth](INSTANCE_KELVORETH_TRACKER_PASS_20261109.md), [Stormtalon](INSTANCE_STORMTALON_TRACKER_PASS_20261109.md) and [Skullcano](INSTANCE_SKULLCANO_TRACKER_PASS_20261109.md) tracker passes. Evidence tier: **Observed** (retail-era clips, tracker frames viewed by the lead, 1280x720) plus **Mapped** (PublicEventObjective rows for event 166, read by the lead). No retail parity claim; nothing run in a client. Two veteran runs only, and the second is an earlier build, so every "observed" row below is a single run unless stated.

## Clips read (uploaded before 2018-11-28; resolved by URL)
| Clip | Date | Use |
| --- | --- | --- |
| "Wildstar: Sanctuary of the Swordmaiden" https://www.youtube.com/watch?v=5E-aCD8zy0k (Tibolt) | 2015-11-29 | veteran ("Moldwood Corruption (Veteran)" on the minimap); main reference; tracker read at 60, 250, 450, 600 (full frame), 1200, 1500, 1800, 2100, 2400, 2950 s. Shows "Timer Bonus", the text 16042 uses. |
| "WildStar - Sanctuary of the Swordmaiden - Veteran Silver Run #4" https://www.youtube.com/watch?v=ruOmdr9YWro (Kaos Sverige) | 2015-05-29 | veteran; **earlier build** (countdown "Remaining for Silver or Gold medal", 66:53 at about 8 minutes into the run, so a 75 minute limit against the 60 minutes in the 16042 row text); used only for which rows appear (frames 40, 80, 110, 150 s, Moldwood route) |
| "Wildstar - Dungeon guide: Sanctuary of the Swordmaiden (SSM)" https://www.youtube.com/watch?v=8lJX5YzUk8c (SywoGG) | 2015-09-15 | **unusable**: the local file fails to decode (invalid NAL units) |

## Observed tracker rows, 2015-11-29 veteran run (Mapped to PublicEventObjective rows)
| Time | Phase | Rows shown |
| --- | --- | --- |
| 60 s | Enter | Defeat Deadringer Shallaos (479), **Deathless in the Dungeon** (1506), **Timer Bonus** (3204). Nothing else. |
| 250 s | Enter | Defeat Deadringer Shallaos, Timer Bonus (the Deathless row is gone, so a party member had died) |
| 450 s | Moldwood Corruption | Timer Bonus, **[0/10] Free the Spirits** (505), **[Waiting for 1 more] Reach the Moldwood Corruption** (614), **Defeat Corrupted Edgesmith Torian** (493), **[1/3] Collect Torine Spirit-Relics** (481) |
| 600 s | Skash | Timer Bonus, [1/10] Free the Spirits, [1/3] Collect Torine Spirit-Relics, **[0/3] Use the Soul Spore on Moldwood Gorgers** (497), Defeat Moldwood Overlord Skash (483), **Destroy the Elder Moldwood Ravager** (639), **(19:22) Defeat Skash or he'll corrupt the prisoner** (1504, 20 minute timer) |
| 1200 s | Skash | Timer Bonus, [1/10] Free the Spirits, [1/3] relics, Defeat Skash, challenge **Vitara's Heart** (2386) |
| 1500 s | Terrace path | Timer Bonus, [1/10] Free the Spirits, [2/3] relics, **Kill Corrupted Lifecaller Khalee** (494), **[Waiting for 1 more] Enter Lifeweaver Terrace** (613) |
| 1800 s | Ondu | [2/10] Free the Spirits, [2/3] relics, **[1/3] Sabotage Lifeweaver Tech Clusters** (496), Defeat Ondu Lifeweaver (480), **Kill the Lifeweaver Guardian** (629) |
| 2100 s | Ondu | [2/10] Free the Spirits, [2/3] relics, Defeat Ondu Lifeweaver |
| 2400 s | Place relics | **[0/3] Place the Spirit-Relics** (482), [8/10] Free the Spirits |
| 2950 s | Selene | Defeat Spiritmother Selene the Corrupted (492) only |

Counts match the table: 505 count 10, 481 count 3, 497 count 3, 496 count 3, 482 count 3, 1504 failure time 1,200,000 ms.

## Observed tracker rows, 2015-05-29 earlier build (Moldwood route, 40 to 150 s)
"[1/4] Collect Torine Spirit-Relics", "[Waiting for 1 more] Reach the Moldwood Corruption", the countdown row, "Defeat Corrupted Edgesmith Torian", **"[7/8] Eliminate Zealous Torine"** (495) and "Deathless in the Dungeon". Counts differ from 16042 (4 relics against 3), confirming this clip predates the final rows.

## What the previous script did, and what changed
Before: the Enter phase activated 479 and 495 and coin-flipped 496 and 500; none of 1506 and 3204 was activated; the final boss only finished the event.

Now (`SanctuaryOfTheSwordmaidenEventScript`):
* Enter activates **1506 Deathless** and **3204 Timer Bonus**, with 479.
* **495 Eliminate Zealous Torine moved into the provisional random pool**: present in the May run, absent from every frame of the November run.
* **496 Sabotage Lifeweaver Tech Clusters moved from the Enter pool to the Ondu phase pool**: the November run first lists it in the Ondu phase and not at 450, 1200 or 1500 s.
* At the final boss: 1506 is credited only if no player died (`OnDeath`), 3204 only if the script-side run time (`Update`) is at most **60 minutes** (the row text).
* The rest already matched the November run: 505 in the Moldwood phase, 493 and 614, 481 from the relic handoff, 483 with 1504 and 639 and 497 in the Skash phase, 494 and 613 on the terrace path, 480 with 629 in the Ondu phase, 482 in the relic phase.
* Tests (58/0 across `SanctuaryOfTheSwordmaidenEventScriptTests` and `SanctuaryOfTheSwordmaidenTriggerScriptTests`): the earlier expectations that 495 is always activated and that 496 is an Enter-phase optional were superseded; new tests cover Deathless and Timer Bonus activation and credit, death handling, the 60-minute cut-off, the 495 pool and 496 in Ondu. Written together with the code, not run red first.

## Not implemented, and why
* **Failed rows disappearing** (Deathless after a death, Timer Bonus after the limit): no script-side objective-fail call in the engine (same limit as the other dungeons).
* **Challenge rows 2383 to 2387 and 2211/2212** (The Sound and the Fury, Rayna's Molten Wake, Splatter Skipper, Vitara's Heart, Don't Blink, the relic and revival challenges): seen only as a name ("Vitara's Heart" at 1200 s); they need fight mechanics that do not exist in scripts.
* **Which optional rows appear** and the route (Temple versus Moldwood): the November run took the Moldwood route only; the Temple route's rows (Hammerfist Moldjaw and others) were not read.
* **The Timer Bonus limit against the November frames**: the Timer Bonus row is missing from the 2400 s crop (run clock 58:37 on the top timer by my offset estimate) while the 60 minute row text would still allow it; the clock offset is not verified, so the 60 minute figure comes from the row text, not from this clip.
* **Counts**: the May 2015 clip shows 4 relics against 3 in the row; the script uses the table row.

## Pattern for the remaining dungeon
Coldblood Citadel (5323 Deathless and 5322 Timer Bonus, 40 minutes) still has both rows unactivated. The corpus holds one dated Coldblood video with subtitles and none read for this pass, so it has no tracker read yet.
