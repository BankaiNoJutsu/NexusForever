# Lead verification of three bosses: Blade-Wind, Trogun, Kuralak (2026-11-09)

Narrowed goal chosen by the user on 2026-11-09 ("Narrow to a few bosses"): verify and refine only these three bosses against retail clips. Method: the lead searched the subtitles of every retail-dated (non-empty upload date before 2018-11-28), subtitled clip whose title or description names the boss, then read the cited passages. Evidence is Observed (auto-generated subtitles, no frame viewed, cast bars are not legible at 720p); nothing is retail parity; cast intervals remain estimates. Tools: scratchpad `boss_grep.py` and `vid_dump.py` (same corpus queries as `scripts/instance_cue_grep.py`).

## Blade-Wind the Invoker (Stormtalon's Lair, creatures 17160/33405)
Searched 7 clips. Lead-read facts:
* **Phase 2 at about 60%: agreed by two independent post-launch sources.** 2014-06-09 (Seatin Man of Legends) https://www.youtube.com/watch?v=P7eQtXKxVlM at about 66 s: "when Blade-Wind reaches about sixty percent"; 2015-09-28 (moepork) https://www.youtube.com/watch?v=8M53-Kf90a4 at about 36 s: "Phase 2 begins when the boss reaches 60%". The "50%" an agent reported was not found in any of the 7 clips (the only nearby "fifty" is an unintelligible caption fragment at 69 s of the 2014-06-09 clip). This resolves the earlier "disputed" label: the lead found no clip that says 50%.
* **Two interrupt armor:** moepork 2015 at about 12 s; single source.
* **Thundercross** (called "lightning cross" in the 2014 clip) in phase 1 and phase 2: both sources.
* **Electrostatic Pulse in phase 3:** moepork 2015 and a 2014-05-24 beta run https://www.youtube.com/watch?v=TshOwD4sWts at about 244 to 304 s ("a big telegraph that takes the entire room"). Three casts in a row: moepork only.
* **Interrupt, interrupt, dodge** rotation: 2014-06-09 at about 66 s, compatible with two armor.
Effect on code: the Blade-Wind phase placeholder and the provisional plan now say 60% is two-clip agreed (the placeholder stays disabled and not wired to behaviour).

## Forgemaster Trogun (Kel Voreth, creatures 32531/32533)
Searched 11 clips. Lead-read facts:
* **Forge retreat at 50%:** moepork 2015-10-13 https://www.youtube.com/watch?v=_K6WIpU09G4 (about 80 s, "at 50% Trogun will run back to the forge and begin casting Exanite Weapon") and the 2014-05-25 beta run https://www.youtube.com/watch?v=K4_L4d4kl0s at about 593 s ("second quarter ... at about fifty percent you will again return to the forge"; pre-launch, so Correlated). Two sources read by the lead; the other two agent-reported clips were not found by the title search.
* **Four interrupt armor:** moepork only. **Volcanic Strike** ("volcanic hammer" in a 2014-07-13 Dulfy veteran guide https://www.youtube.com/watch?v=bRC17Hepq5k at 91 s) and **Forgemaster's Call** ("I call upon the power of my ancient forges", Dulfy at 129 s) are named; the guide's timing gives no cast interval.
Effect on code: the evidence wording in the Kel Voreth plan and the Trogun placeholder now says two clips read by the lead.

## Kuralak the Defiler (Genetic Archives, creature 52969)
Searched 13 clips; the only mechanically complete narration is moepork 2015-11-13 https://www.youtube.com/watch?v=qVzVREgWB7A, read by the lead through about 230 s:
* **Phase 2 begins at 73%** (81 s). The agent-reported 72% was not found in any clip by the lead; the plan now uses 73% (single source read by the lead).
* **Phase 1:** pillars explode then Vanish into Darkness (boss stealths behind an outer pillar, must be found and interrupted or it wipes; zero interrupt armor); lasers rotate around pillars all of phase 1; **Putrid Discharge** "frequently" on three random players (high damage and a massive slow), continuing through the whole encounter (68 to 95 s).
* **Phase 2:** lasers fade, pillars randomly explode and leave telegraphs; **Cultivate Corruption** makes an egg that must be interrupted or the player dies, interrupters get the **Chromosome Corruption** debuff (+100% damage and healing done, heavy damage over time); **Outbreak** "occasionally" kills anyone standing in it unless interrupted (about 133 s); **DNA Siphon** "occasionally" stuns the current tank (about 143 s); egg counts 1, 2, 4, 8, 16 per Cultivate Corruption (about 221 to 228 s).
* **Not found:** Tainted Ventilation (an agent-selected row) is not mentioned in the guide.
Effect on code: the Genetic Archives plan was changed: phase 2 at 73%; phase 1 casts only Putrid Discharge; phase 2 casts Putrid Discharge, Cultivate (`[GA] e412 Cultivate Base` 60546, the egg cast, replaces Chromosome Corruption which is the resulting debuff), DNA Siphon and Outbreak; Tainted Ventilation and the Vanish row stay out.

## Tests
After the change the filter covering all provisional encounter tests plus the Blade-Wind, Trogun, health-phase and invulnerability tests passed 108, 0 failed (one scope; not added to earlier counts). The Kuralak threshold assertion was updated from 0.72 to 0.73 deliberately because the evidence changed.

## Still unverified for these bosses
Every cast interval and first delay (estimates); which creature casts which row; Blade-Wind's phase 3 and disciples; Trogun's orbs and volcanoes; Kuralak's egg counts, pillar geometry and enrage; frame-level confirmation of anything; behaviour in a client.

## Extension: Skullcano abilities (2026-11-09, lead read, Observed)
Searched 36 retail subtitled Skullcano clips and read the Vhalor Gaming veteran guide https://www.youtube.com/watch?v=Om4KvjGMxeo (2014-06-23) myself:
* **Tugga (plan: Molten Rain 41088, Magma Totem Pair 41052, Into the Stew 63378):** at 52 to 70 s the guide names exactly these three phase 1 abilities: a totem that tethers someone and shoots cross-shaped telegraphs, Molten Rain (many circular telegraphs) and Into the Stew (one player thrown into the pot); phase 2 at 90 to 95 s he walks to food on the floor with no interrupt armor. Single guide for the ability list (a second channel's overview says "four abilities"), so the three casts are single-source; the Magma Totem Pair row binding is by name.
* **Thunderfoot (plan: Thunder Pound 36437, Pulverize 36385):** same guide at 312 to 374 s names "Thunder slam"/"Thunder pound" (everyone jumps) and Pulverize (a very large circular telegraph around one targeted player, preceded by his enrage). Pulverize is also in a second clip per the exact-name check; single narrator for the rest.
* **Octog (plan: Harpoon 31352, Ink 52872):** the guide's hook shot and ink are echoed by two more channels: Argick 2014-09-29 https://www.youtube.com/watch?v=-QvVjBUQW2Q at 40 s ("after hook shot") and 168 to 170 s ("ink"), and the 2015-04-28 overview https://www.youtube.com/watch?v=5MPHtAxjsi8 at 136 to 141 s ("the boss will try to hook you"). Hook shot is the commentators' name for the Harpoon row (name mapping, Correlated).
* **Mordechai (plan: Molten Torrent 39469, Disperse Grenades 39566):** lasers are named by several channels (Zybak 2014-03-31, Yogscast, the Argick clip) and a "terraformer" by seven channels in the exact-name check; Disperse Grenades was not found by name. The 33% and 10% thresholds were not found by the lead in these clips and stay out of the plan.
No code changed: the Skullcano plan already used only these abilities and no thresholds.

## Extension: Sanctuary of the Swordmaiden (2026-11-09, lead read, Observed plus client text)
Searched 39 retail subtitled clips with the boss and ability names, and looked up the client achievement and challenge texts. Confirmed and kept: Ondu Clover Leaf (SywoGG https://www.youtube.com/watch?v=8lJX5YzUk8c at about 1078 s), Skash Corruption Heartseeker ("heart seeker" in two clips; this replaced the Summon the Swarm row 42135, which no clip named for him), Rayna Flame Leash ("leash" said by three channels, e.g. Zybak https://www.youtube.com/watch?v=_JG3G1fK3jU at about 68 s) and Molten Wave (Prime challenge text 409252: "without anyone ... getting hit by any Molten Waves"), Selene Lights Out! (Prime text 409255) and Shade Prison (said in one clip, text 376412). Dropped as not confirmed by any clip or client text the lead found: Ondu Root phase 42526, Rayna Smoldering Pool 42418, Selene Shadow Meld 42880, Skash Summon the Swarm 42135. Code: `SanctuaryProvisionalEncounterScripts.cs` and its test now use only the kept rows (Ondu 43253; Skash 45081; Rayna 42171, 42264; Selene 42757, 42386); the Sanctuary and provisional-encounter filter passed 56, 0 failed (one scope). Intervals remain estimates; nothing has been run in a client.

## Extension: Datascape and Coldblood Citadel (2026-11-09, lead read, Observed)
* **Datascape (97 retail subtitled clips searched):** Purge is said by many channels (System Daemons: Zybak 2014-08-18 https://www.youtube.com/watch?v=zzKAFrQFCcI at 215 s, moepork 2015-12-03 https://www.youtube.com/watch?v=V5PPaN7cIK8 at 73 s, spythe, Putty Puppy and others); Memory Wipe by three videos; Maelstrom Authority's "lightning rod" by spythe 2014-10-21 https://www.youtube.com/watch?v=n6igCclG-xI at 54 s and Zybak 2014-10-22 https://www.youtube.com/watch?v=68JGdWZng8k at 657 s; Avatus "hands" and "cannons" by spythe (2015-03-01), Nanomancy, Agent Zero and Kaos Sverige, which the developers' rows Summon Holo Hands and Summon Holo Cannons plausibly name (name mapping, Correlated). No code change: the Datascape plan already used only these rows and no thresholds.
* **Coldblood Citadel:** the single official clip https://www.youtube.com/watch?v=AY3W8n8Zh3c (2017-01-23, developers narrating the PTR build) was read for the plan's abilities: Hailstone Gatecrasher "is all about ice spikes" (1687 s) and has a Berserker Rage (1705 to 1781 s; the lead dropped the guessed Ice Rage row 87983, so this ability is not scripted); Ulfrid "casts a bunch of chasing shadowy spikes" (2498 s); Katla "blood tornadoes" (2504 s); Golag "leaving a trail of Soulfrost" and "Soulfrost spikes" (2538 to 2546 s). The clip gives no numbers, intervals or thresholds; single source. No code change.

## Extension: remaining Kel Voreth and Genetic Archives plan rows (2026-11-09, lead read, Observed)
* **Grond:** Thrash is named by four channels (TheSparrowsJourney https://www.youtube.com/watch?v=K4_L4d4kl0s at 136 s, imbonked https://www.youtube.com/watch?v=q8Xu2a0RcAU at 88 s, Dulfy https://www.youtube.com/watch?v=JJtxySqCr34 at 111 s, moepork https://www.youtube.com/watch?v=AMIEBH8VoUc at 45 s "occasionally cast thrash"); Headbutt is in the moepork guide read earlier ("headbut ... rush towards a random player" at about 39 s; the captions misspell it, which is why an exact-name search found none). Single narrator for Headbutt.
* **Drokk:** Homing Barrage in the moepork guide https://www.youtube.com/watch?v=w3kbhbj9CcA at 23 to 25 s ("continuously cast homing barrage ... random players"); single source. His thresholds stay unconfirmed and out of the plan.
* **X-89 (Strain Bomb 47353):** commentators call the mechanic a "big bomb" and "small bomb" (Zybak https://www.youtube.com/watch?v=no2K8V1ZhwU at 40 s and 2014-06-25 at 84 s, spythe, klorinator); mapping the developers' Strain Bomb row to these is a name inference (Correlated).
* **Phage Maw (Detonation Bombs 60702, Aerial Bombardment 57306):** Zybak https://www.youtube.com/watch?v=vnlONelOB7U at 29 s ("transition into the bomb phase") and a 2014-01 beta clip ("it's going to bombard you", pre-launch); row-to-phrase mapping is a name inference.
No code change. All of these rest on one or two narrators, and every interval remains an estimate.

## Lead check of the missing-input statements (2026-11-09, script `scripts/instance_verify_missing_inputs.py`)
For each unscripted row the script lists the `PublicEventObjective` ids in the repo enum, how many of those numbers appear as `[TAG] eNNN`/`wNNN` encounter numbers in `coverage/spell4_internal_tags_20261109.csv`, and the number of retail-dated subtitled clips (title or description naming the instance) with their distinct channels.

| Row | Objective ids | Ids with tag rows | Tag rows | Retail subtitled clips (channels) |
|---|---|---|---|---|
| Infestation | 12 | 0 | 0 | 5 (3) |
| Outpost M-13 | 19 | 0 | 0 | 3 (3) |
| Rage Logic | 15 | 0 | 0 | 2 (1) |
| Space Madness | 26 | 0 | 0 | 5 (5) |
| Gauntlet | 29 | 0 | 0 | 15 (10) |
| Deep Space Exploration | 27 | 0 | 0 | 7 (4) |
| Fragment Zero | 38 | 0 | 0 | 12 (8) |
| Evil from the Ether | 31 | 0 | 0 | 1 (1) |
| Vault of the Archon / Hall of the Hundred | 73 | 0 | 0 | 3 (2) |
| Journey into OMNICore-1 | 5 | 0 | 0 | 7 (6) |
| Protostar Winterfest / Super Mall | 63 | 0 | 0 | 17 (7) |
| Initialization Core Y-83 | 7 | 1 | 79 | 15 (8) |
| Red Moon Terror | 60 | 3 | 220 | 7 (6) |
| Shade's Eve | 92 | 1 | 8 | 16 (11) |
| Ultimate Protogames | 113 | 7 | 531 | 5 (4) |

What this confirms (lead-run, mechanical): the statement "no spell row is tied to a boss" holds for the eleven rows with zero matching tag numbers (the ten above plus Evil from the Ether), so a provisional script there would have to bind rows by name only, which the rules forbid; the existing Evil from the Ether Katja script's Spell4 ids therefore have no tag binding either. It also shows that the footage-poor statements are broadly right for Rage Logic (2 clips, one channel), Outpost M-13 and Evil from the Ether. What it does not confirm: the claims that depend on clip content (Red Moon Terror's Engineers and Star-Eater "no readable clip", Shade's Eve "no clip shows a Jack Shade ability", the Vault boss clip never naming its boss, Y-83's creature binding) and the creature-id claims; those remain agent-written. Y-83, Red Moon Terror, Shade's Eve and Ultimate Protogames do have tag rows, so for those rows the missing input is the creature-to-spell binding and a readable boss clip, not the absence of rows.

## Correction found by the lead: Star-Eater does have a readable clip (2026-11-09)
Checking the content-dependent missing-input claims, the lead counted boss-name mentions in the 14 retail subtitled Redmoon Terror clips and found the official developer stream "WildStar - Redmoon Mutiny Preview Stream" (2016-11-09, 4,531 s, https://www.youtube.com/watch?v=5HfpsQr6slE) mentions Star-Eater 21 times. The raids packet and `Raid/ProvisionalEncounterSkipped.md` said "Engineers and Star-Eater have no readable clip" and that Swabbie's clip names no cast; the Star-Eater half is wrong. Lead-read at about 2319 to 2705 s: Star-Eater the Voracious (formerly "Space Octag", the same race as Bosun Octog); players use a jetpack and ground slam from an extra action bar; she hits hard and breaks armor; a hook shot pulls players across the arena; players jump star to star, take the orange power-up stars and smash into her to break her shield "so she can't cast Supernova and kill everyone". The `[RMT] e4556` tag rows match this: `Hookshot - Pull` 85530, `Supernova - Base` 85713 (CastTime 1500 ms), `Astral Shield` buff, removal and immunity rows 85714, 85724, 85745, and the `Jetpack` rows (these reuse "Bosun Octog" in their developer names). Creature ids 72758 and 75658 were verified by name against Creature2 (`scripts/instance_creature_names.py` family; lookup by name).
Code: added a provisional Star-Eater script (`Raid/RedMoonTerror/Script/RedMoonTerrorProvisionalEncounterScripts.cs`, hook shot pull and Supernova, estimated delays) with `RedMoonTerrorProvisionalEncounterTests` (3 tests); the filter with the other provisional tests passed 59, 0 failed (one scope). Engineers and Swabbie remain without a lead-verified clip or binding; the shield, stars and jetpack mechanics are not modelled. This also shows that the agent-written "missing input" statements can be wrong in both directions, which is why the lead check of them is not finished: the content-dependent claims for the other rows (Shade's Eve, Vault of the Archon, Y-83, the expeditions' footage) still need the same clip-count check.
