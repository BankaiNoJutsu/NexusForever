# Coldblood Citadel: objective tracker pass (2026-11-09)

Follow-up to the [Kel Voreth](INSTANCE_KELVORETH_TRACKER_PASS_20261109.md), [Stormtalon](INSTANCE_STORMTALON_TRACKER_PASS_20261109.md), [Skullcano](INSTANCE_SKULLCANO_TRACKER_PASS_20261109.md) and [Sanctuary](INSTANCE_SANCTUARY_TRACKER_PASS_20261109.md) tracker passes. Evidence tier: **Observed** (tracker frames viewed by the lead, 1280x720, one clip) plus **Mapped** (PublicEventObjective rows for event 907, read by the lead). No retail parity claim; nothing run in a client. **One clip only**, so every "observed" row below is a single run.

## Clip read (uploaded before 2018-11-28; official Carbine material; resolved by URL)
| Clip | Date | Use |
| --- | --- | --- |
| "WildStar: Coldblood & Evil from the Ether Livestream" https://www.youtube.com/watch?v=AY3W8n8Zh3c (WildStarOnline, official) | 2017-01-23 | developer demonstration, **solo, Prime difficulty** ("Winterfury Vanguard (Prime)" on the minimap), dungeon section from about 1380 s to 3030 s; tracker panel cropped every 30 s from 1380 s to 2970 s (the panel is hidden or covered from about 2940 s). |

The other corpus video that mentions Coldblood ("WildStar - All Prime Dungeons (Part 1)", uploaded 2018-11-29) is after the cut-off date and was not used. No other dated Coldblood video exists in the corpus.

Caveats that bound everything below: the run is a **developer demo**, solo, on **Prime** (which can change the row set; the Kill Enemies bar, row 5342, was not seen in the panel), and the clip comes from a later build than the 16042 table in at least one respect: the first dungeon phase shows a "Join the Pell assaulting the gates" row whose text exists (763515/763516) but for which **no PublicEventObjective row exists in the 16042 table** (rows 5327 to 5332 are absent), so it cannot be activated.

## Observed tracker rows (Mapped to PublicEventObjective rows)
Run clock: the top timer reads 00:26 at 1400 s, so the run started about 1374 s.

| Time (clip) | Phase | Rows shown |
| --- | --- | --- |
| 1380 to 1440 | Enter | **Timer Bonus** (5322), **Deathless in the Dungeon** (5323), **[Waiting for 1 more] Find the Pell attacking Coldblood Citadel** (5326) |
| 1470 to 1590 | Hailstone | Timer Bonus, Deathless, **[x/10] Gather Soulfrost Shards** (5317), **[Waiting for 1 more] Join the Pell assaulting the gates** (no 16042 row), **[x/15] Save Pell fighting the Osun** (5333, challenge) |
| 1620 to 1770 | Hailstone | the same, with the Join row replaced by **Defeat the Hailstone Gatecrasher** (5313) and **Pell Protection Plan** (5334, challenge) |
| 1800 | Hailstone, boss dead | Defeat the Hailstone Gatecrasher still listed, Pell Protection Plan gone (counted as complete or failed; not told apart) |
| 1830 to 1890 | Iceblood Coven | Timer Bonus, Deathless, Gather Soulfrost Shards, Save Pell, **[0/8] Rescue Winterfury Prisoners** (5319), **[0/4] Kill Krovak Summoners and their Frostguards** (5321), **Defeat the Iceblood Coven** (5314), **Rescue the Pell Architect** (5324), **Concurrent Coven Collapse** (5335, challenge) |
| 1920 to 2010 | Iceblood Coven | the same, with Rescue the Pell Architect replaced by **Kill the Pell Architect** (5325) |
| 2040 to 2280 | Iceblood Coven | Kill the Pell Architect gone, prisoners 1/8 up to 6/8, Krovak 1/4 up to 3/4 (4/4 by 2310, row gone) |
| 2310 to 2550 | Iceblood Coven | Timer Bonus, Deathless, Gather Soulfrost Shards 3/10, Save Pell 13/15, Rescue Prisoners 6/8, Defeat the Iceblood Coven, Concurrent Coven Collapse |
| 2580 to 2730 | Risen Harizog | the carried rows, **Defeat the risen Harizog** (5315), **Infusion Interdiction** (5336, challenge) |
| 2760 to 2790 | Risen Harizog | **Deathless in the Dungeon is gone** (the developer died in the Harizog fight, which fits the row failing on a player death) |
| 2820 | Risen Harizog | Timer Bonus still listed; Infusion Interdiction still listed |
| 2850 to 2910 | Risen Harizog | Timer Bonus still listed; Infusion Interdiction gone (failed or completed; not told apart) |

Rows **never seen** in this run: Rally the Winterfury Pell (5316), Steal Sample of Liquid Soulfrost (5337), Destroy Soulrot Canisters (5318), Disable the Osun's Soulfrost Traps (5320). Rows seen as optional: Gather Soulfrost Shards, Rescue Winterfury Prisoners (and Krovak, which the script already always activated).

The Timer Bonus row is still listed in the last readable frame (2910 s, about 25.6 minutes after the run start by the 1374 s estimate), which is inside the 40 minutes of the row text and consistent with it; the clip ends before the limit could be tested, so the 40 minute figure comes from the row text (5322: "Complete Coldblood Citadel within 40 minutes for bonus points towards a medal"), not from the clip.

## What the previous script did, and what changed
Before: the Enter phase spawned the gather ring and trigger; neither 5322 nor 5323 was ever activated; the final boss only finished the event.

Now (`ColdbloodCitadelEventScript`):
* Enter activates **5323 Deathless** and **5322 Timer Bonus** (idempotent, so a repeated Enter phase is harmless), matching the first tracker frame.
* At the final boss: 5323 is credited only if no player died (`OnDeath`), 5322 only if the script-side run time (`Update`) is at most **40 minutes** (the row text).
* Everything else already agreed with the clip: Hailstone phase activates 5313, 5333 and a random pool of 5337/5317/5316 (the clip shows only 5317); Iceblood phase activates 5314, 5324, 5321, 5335 and a random pool of 5319/5318/5320 (the clip shows only 5319); Harizog phase activates 5315 and 5336.
* Tests (47/0 across the Coldblood test classes): five new tests cover Deathless and Timer Bonus activation, credit, death handling (player and non-player) and the 40-minute cut-off. Written together with the code, not run red first.

## Not implemented, and why
* **Failed rows disappearing** (Deathless after a death, Timer Bonus after the limit, Infusion Interdiction): the engine has no script-side objective-fail call (same limit as the other dungeons).
* **"Join the Pell assaulting the gates"**: seen in the clip, no 16042 row; the script keeps activating Defeat the Hailstone Gatecrasher at the start of the Hailstone phase instead of after a join row. Not changed.
* **Pell Protection Plan (5334)**: seen at 1620 s with the boss row, never activated by the script; it is a challenge needing the Snow-Stalker counting mechanic, so it is left unactivated like the other challenge rows.
* **Rescue the Pell Architect to Kill the Pell Architect (5324 to 5325)**: the clip shows 5325 appearing when 5324 leaves, but no script or entity credits 5324 today (no Pell Architect entity script), so there is nothing to hang the hand-off on. Open.
* **Optional pool**: one solo Prime run gives one set, so the random pool stays a provisional choice.
* **Prime difficulty** (Kill Enemies bar 5342 and any scaled rows) is not modelled.
* **Challenge rows 5333, 5334, 5335, 5336**: not credited; their mechanics are not in scripts.

## State of the dungeon sweep
With this pass every dungeon that defines a Deathless and a Timer Bonus row (Kel Voreth, Stormtalon's Lair, Skullcano, Sanctuary of the Swordmaiden, Coldblood Citadel) now activates both and credits them at the final boss. Coldblood rests on one developer-demo clip; a retail group run would be better evidence.
