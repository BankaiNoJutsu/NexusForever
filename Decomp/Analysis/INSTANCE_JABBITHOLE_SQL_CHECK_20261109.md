# Instance encounters: Jabbithole / MySQL data check (2026-11-09)

Question: does the MySQL / SQL data (including Jabbithole) hold anything that closes the open inputs of the provisional encounters?
Evidence tier for everything below: **Correlated** (boss joined to ability by name; nothing here is Mapped to a Creature2 id, and nothing is retail parity).

## What was found on disk

| Source | Where | Content |
| --- | --- | --- |
| Jabbithole dump | `I:\GIT\NexusForever-assessment\jabbithole_mysql\*.sql` (17 files, 513 MB, rows created from 2014 onward; `last_seen_in` goes up to 7 and its mapping to client builds was not checked) | `creatures` 24,679, `spells` 12,901, **`creature_spells` 35,703**, `public_event_creatures`, `public_event_objectives`, `public_events`, `zones`, `coordinates` (443 MB), quests, achievements |
| Client table dump | `...\wildstar_client_mysql\*.tbl.sql` (241 MB) | the same `.tbl` tables already read from the binary extract; no new information |
| Repo SQL | `Obsolete/Database`, `Tools/DataMapping/sql/laughingws_*` | NexusForever and LaughingWS (emulator) world seeds; emulator-sourced, never evidence under the standing rules |
| Running MySQL | `mysqld` on 3306 | not queried (no credentials in hand); the dump files were parsed directly with `scripts/instance_jabbithole_links.py` |

Earlier statements that "no creature to spell table exists" are corrected: the **client** has none (`Creature2` rows carry no spell ids, checked for Blade-Wind 17160/33405 and Trogun 32531), but Jabbithole's `creature_spells` is a retail-sourced creature to spell list.

## Join quality

* `spells.game_id` is the Spell4 id. Checked against the 16042 Spell4 table by name: **12,359 of 12,901 (95.8 %)** match by id and name (every 5,000-id band between 81 % and 100 %; the weakest is 75000 to 79999). The remainder are older-build ids.
* `creatures.id` is **not** a Creature2 id (Blade-Wind is 109 and 3068 in Jabbithole, 17160 and 33405 in Creature2). Bosses are therefore joined by name, with the vet flag, health and level for a sanity check.
* Export: `coverage/jabbithole_boss_spell_links_20261109.csv` (454 rows for the 22 boss names; 453 name-matched against the 16042 table, 1 not in the table).

## Result 1: independent support for the scripted ability names

Jabbithole lists the ability on the same boss (vet row where one exists) for: Blade-Wind Thunder Cross and Lightning Strike; Aethros Torrent; Stormtalon Static Wave and Lightning Rod; Trogun Volcanic Strike, Forgemaster's Call and Exanite Weapon; Grond Thrash and Headbutt; Drokk Homing Barrage; Tugga Molten Rain; Thunderfoot Thunder Pound and Pulverize; Octog Noxious Ink; Skash Corruption Heartseeker; Rayna Molten Wave; Selene Lights Out! and Shade Prison; X-89 Strain Bomb; Kuralak Outbreak; Phage Maw Bombardment; Maelstrom Authority Lightning Rod.

Not listed by Jabbithole (so no extra support, not contradicted): Tugga Magma Totem Pair and Into the Stew; Octog Harpoon (Jabbithole has Hookshot 31283 instead); Mordechai Molten Torrent and Disperse Grenades (Jabbithole has Big Bang, Turret Grenade, Cross Shot, Pin, Vicious Barrage); Ondu Clover; Rayna Flame Leash; Wrathbone Rock; Kuralak Putrid Discharge, Cultivate, DNA Siphon; Phage Maw Detonation Bombs; Datascape Purge, Memory Wipe, Holo Hands and Holo Cannons; Coldblood Citadel (Hailstone, Ulfrid, Katla, Golag); Star-Eater (Red Moon Terror is after the scrape).

## Result 2: the tag rows and the Jabbithole rows are different Spell4 rows

For most abilities the repo scripts the tag row (`[STL] e312 ... Thunder Cross`, e.g. 38868) while Jabbithole links a differently numbered row of the same name (e.g. 63231, castTime 1500). The tag rows' `Spell4Base` names are inherited from cloned spells (38868 is named "Purge", 38836 "Restraint", 36104 "Marker Flare", 85530 "Noxious Ink"), so the client-visible name of a tag row is often unrelated. Effect shapes (Spell4Effects, type 8 = damage) were compared side by side; most tag rows are plausible damage or control rows and were left alone.

Four tag rows were not ability-shaped and have a Jabbithole creature-linked same-name replacement with damage effects, so the provisional plans now cast the replacement (disabled by default, still provisional):

| Scripted | Tag row (name, effects) | Now casts (Jabbithole link, 16042 name matches) |
| --- | --- | --- |
| Stormtalon Static Wave | 39717 "Reactive Critical Hit Boost", one 10 s stat effect | 39681 Static Wave, 10 s cast, damage effects |
| Stormtalon Lightning Rod | 39852 "Tier 4 - 1 Stat - Holder Spell", twelve stat effects, 30 min | 63110 Lightning Rod tier 2, 15 s cast, damage effects |
| X-89 Strain Bomb | 47353 "Activate Life Support Consoles", console effects | 47286 Strain Bomb, instant, damage effects |
| Maelstrom Lightning Rod | 44437 "Jetpack", jetpack effects | 44325 Lightning Rod, 20 s channel, one damage effect |

Left unchanged but flagged as weak by the same effect check: Octog Harpoon 31352 (disguise effects; Jabbithole's Hookshot 31283 is a candidate), Octog Ink 52872 (a single type-81 effect, no damage), Trogun Exanite Weapon 38882 (type 10; Jabbithole 52521 is a 14 s channel), Wrathbone Rock 77086, Coldblood Hailstone/Golag, Datascape Purge/Holo rows, Kuralak Putrid Discharge 57923 (schematic-grant effect), Phage Maw Detonation Bombs 60702, Tugga Magma Totem Pair 41052 (disguise). Which row retail casts is **not** settled by any source here; the Jabbithole rows are the ones the client would show with the named cast bar, the tag rows are the encounter-authored clones.

## Result 3: what Jabbithole adds

* Cast times by name for each boss (CSV column `cast_time_ms_16042`): Static Wave 10 s, Lightning Rod 15 s, Tempest 15 s (Aethros vet tier 2), Trogun Forgemaster's Call 7 s, Exanite Weapon 14 s channel, Grond Thrash 5.5 s, Selene Blackout 9 s, Skash Corrupting Revolver 5 s channel, Rayna Molten Wave 20 s channel, Maelstrom Lightning Rod 20 s channel, Avatus Obliteration Beam 12 s channel, Gloomclaw Corruption Pool 30 s channel.
* New candidate abilities per boss not yet scripted (names and cast bars only): Stormtalon Lightning Storm (10 to 15 s channel by tier) and Thunder Call; Aethros Tempest, Thunderbolt; Blade-Wind Electrostatic Pulse (2.5 s), Arcane Bolt; Drokk Enslave (2 s), Suppression Wave, Lockdown; Selene Blackout, Creeping Shadows, Nightmare Ripple; Rayna Raging Lava, Flame Geyser; Skash Tentacle Wrath; Mordechai Big Bang (3 s), Pin; Kuralak Chromosome Corruption, Vileness, Overload; Phage Maw Laser Blast, Crater (7.5 s), Plasma Flower; Avatus Annihilation (2.8 s), Phase Punch (3.3 s), Holo-Crush, Invalidate.
* No cooldown, interval, phase or health-threshold data: `spells.cooldown` is "No cooldown" for these rows and `public_event_*` tables carry names and ids only. **Intervals and first delays therefore stay ESTIMATES**, and the blocking input of the goal (cast order and timing, server-side) is unchanged.

## Not changed / not claimed

* No new scripts were added from the new candidate list: they have names and cast bars but no video or text evidence for when or in which phase they are cast.
* Jabbithole is a third-party scrape; it is not the retail client and its ids drift (about 4 % name mismatches by id). Treat as Correlated.
* The running MySQL instance was not queried; the dump was parsed directly. Coordinates are covered in the addendum below.
* Test run: `FullyQualifiedName~ProvisionalEncounter` passed 60/0 after the rebinding. The new test pins the Stormtalon rebinding; the other three rebindings have no per-id test.

## Addendum: coordinates and the runtime world `entity` rows (2026-11-09)

Sources: Jabbithole `coordinates.sql` (1,989,100 rows; location types Creature 751,368, QuestCallZone 1,233,971, PublicEvent 1,579, Challenge 420, PathMission 1,762) and the runtime world database copy `artifacts/character-list-hosted/database-copies/world.sql` (table `entity`, 41,923 rows; a snapshot dated 2026-11-06, the live MySQL was not queried). Script: `scripts/instance_jabbithole_coordinates.py`; output `coverage/jabbithole_boss_coordinates_20261109.csv`. Tier: Correlated (bosses joined by name; Jabbithole creature ids are not Creature2 ids).

* Jabbithole `Creature` coordinates are many sightings per creature (up to 100 kept per row) in the same x, y(height), z space as `entity` rows: where both exist the nearest sighting is within 0.6 to 10.1 units of the seeded spawn (Avatus 0.7, X-89 0.6, Kuralak 0.8, Tugga 1.0, Thunderfoot 1.6, Blade-Wind vet 2.1, Aethros 3.8, Drokk 5.5, Grond 5.6, Trogun 10.1).
* So the ten existing boss spawn rows (seeded by `Tools/DataMapping/sql/laughingws_instance_entity_wip_seed.sql`, an emulator-sourced WIP/GUESSED seed) are independently consistent with retail-sourced sightings. This supports their placement; it does not make them retail parity (Trogun is the loosest at about 10 units).
* Gap: of the 36 scripted creature ids that name-join to a Jabbithole boss, **only 10 have an `entity` row in the world data**; 26 have none. Ten bosses have no spawn row for either their normal or vet id, so their provisional scripts cannot attach in this database: Stormtalon (sightings median about 57, -11, 257), Bosun Octog (-661, -876, -1002), Mordechai (-751, -692, -440), Ondu (4048, -773, -2559), Skash (3606, -728, -3484), Rayna (5042, -758, -3320), Selene (4248, -785, -3311), Phage Maw (3734, -280, -124), Maelstrom Authority (7212, -250, -12352), Wrathbone (-15802, -902, -29602). The other missing ids are the second difficulty id of a boss whose first id has a row (for example Trogun 32533, Thunderfoot 24893, Tugga 24898, Grond 32535, Drokk 32539, Aethros 17166, Blade-Wind 17160); whether the engine spawns one row for both modes was not checked.
* Not done: no spawn rows were authored. A row needs area, faction, displayInfo and facing, which Jabbithole's x, y, z sightings do not give, and the medians come from wandering or moving creatures. The CSV is the candidate list for a later, separately reviewed seed, labelled Correlated and WIP/GUESSED.
* Other SQL checked: the repo's `Obsolete/Database` and `Tools/DataMapping/sql` seeds hold no spell, cast-order or timing data for these bosses; the LaughingWS source world database is not on disk (only its plan file), and it is emulator-sourced in any case; Jabbithole `public_event_*` tables carry names and ids only; no `SQL_Loot` or `Various SQL` directories exist on this machine.

### Spawn seed drafted for the ten missing bosses (2026-11-09)

`Tools/DataMapping/sql/jabbithole_instance_boss_wip_seed.sql`, generated by `Tools/DataMapping/build_jabbithole_instance_boss_seed.py` (inputs: the coordinates CSV above and the client Creature2 tables). Ten `entity` rows, ids 1100310001 to 1100310010 (inside the 1100300000 to 1100399999 range that `runtime_world_seed.sql` does not clean up): Stormtalon (33406, world 382), Bosun Octog (24486) and Mordechai (24489) (world 1263), Ondu (28721), Skash (28727), Rayna (28733), Selene (28735) (world 1271), Phage Maw (52974, world 1462), Maelstrom Authority (30497, world 1333), Wrathbone (67944, world 3173).

* Position: median of 52 to 100 Jabbithole sightings (Correlated).
* Faction and display info: **Mapped** from the client. Faction is Creature2 word 100 and display info is the single entry of the display group in word 11; both rules reproduce the stored values of all ten existing boss rows. Rayna and Selene resolve to the same display info 25705 (one shared display group), which looks like a shared placeholder and should be eyeballed in a client.
* World ids come from the repo's own map scripts (Stormtalon 382, Skullcano 1263, Sanctuary 1271, Genetic Archives 1462, Datascape 1333, Protogames Academy 3173); the Sanctuary and Phage Maw/Maelstrom assignments rest on the script filter ids and the Jabbithole zone bounds, not on a loaded map.
* Unknown and defaulted: area 0, facing 0, and **no `entity_event` row**, so each boss would exist for the whole instance instead of appearing at its event phase. Normal or veteran id was chosen to match the sibling seeded rows (Stormtalon veteran, Skullcano normal; Sanctuary normal by Creature2 word 9 = 1).
* Not wired into any setup script, not applied to any database and not run through MySQL (no credentials), so the SQL is syntax-checked only by inspection (19 columns, 19 values per row). Labelled WIP/GUESSED; no parity claim.
