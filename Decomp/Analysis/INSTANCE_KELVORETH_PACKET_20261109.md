# Instance packet: Ruins of Kel Voreth (2026-11-09)

Phase B packet per [INSTANCE_ENCOUNTER_VIDEO_GOAL_PROMPT.md](INSTANCE_ENCOUNTER_VIDEO_GOAL_PROMPT.md). Evidence tier: Observed (subtitle reads by the lead; no frame viewed for this dungeon) and Correlated (table names). Single-source throughout. No code, no retail parity claim.

## 1. Footage
About 136 dated clips (corrected count, 2014-02-17 to 2018-06-27), 76 with subtitles. The three boss guides below are one commentator (moepork, 2015-10-13), each 144 to 188 s, subtitles, one clip per boss, so every number is single-source until a second clip agrees:
* Forgemaster Tro'gun: https://www.youtube.com/watch?v=_K6WIpU09G4
* Grond, the Corpsemaker: https://www.youtube.com/watch?v=AMIEBH8VoUc
* Slavemaster Drokk: https://www.youtube.com/watch?v=w3kbhbj9CcA
Other sources not yet read: `1980` guide "Veteran Ruins of Kel Voreth Silver" (2014-06-16, 1042 s, subtitles), a 2014-06-07 level-20 run (no subtitles) and the rest of the 136 clips. Resolve videos by URL; corpus ids changed on 2026-11-09.

## 2. Skeleton (Observed, single-source, from the guides)
**Forgemaster Tro'gun** (four interrupt armor): fight starts with Trogun channelling Exanite Weapon at his forge; volcanoes at the map edge spit random fireballs (ranged attacks of 25 m or more can reach him); Volcanic Strike periodically, tracks the tank, high burst, knockdown, leaves a volcano; Forgemaster's Call summons seven fire orbs that drift to Trogun, a player picking one up gets a stacking Surge of Primal Fire buff (5% damage), Trogun picking one up buffs him and makes his attacks explode for group damage; challenge: Trogun must not take any orb. At 50%: runs back to the forge and channels Exanite Weapon again with Exanite shards flying out like a bullet pattern; afterwards Forgemaster's Call also shoots Exanite shards.
**Grond, the Corpsemaker** (infinite interrupt armor, interruptible abilities): trap ring around the room (damage and stun); frontal cone auto attack; Buck (cone behind); Headbutt (rush at a random player, knockback); Thrash (line that rotates 360 degrees counterclockwise, starts behind the boss); at 75% unlocks Bellow (knockback and disarm), usually followed by Scratch Itch (three fixating fleas that cast Bloodburst; an uninterrupted burst kills and spawns three more adds; adds are lured onto the traps); at 50% unlocks Mutilate (leap to a random target, knockback, knockdown, then a cone damage-over-time); challenge: no one hits a single trap.
**Slavemaster Drokk** (said to be the hardest, four phases): phase 1 tank and spank with Homing Barrage on random players (challenge: avoid every barrage); phase 2 at 85%: Drokk vanishes, an army of bombshell constructs explodes about 10 s after arriving, one must be destroyed first; he returns and keeps summoning constructs at random players; phase 3 at 65%: vanishes, places a tracking beacon (strong damage over time) on a random player while constructs chase that player, three beacons in total, then returns with Lockdown (tether on a random player, breakable by crowd-control break or damage); at 45%: Enslave (group teleported to the back and tethered with low-HP tethers) while Drokk chain-casts Suppression Wave (slow expanding attack, high damage, knockdown); phase 4 at 25%: vanishes again and repeats a tracking-beacon phase with bombshell constructs added, then repeats the previous phase.
Thresholds recorded: Trogun 50%; Grond 75%, 50%; Drokk 85%, 65%, 45%, 25%. All single-source.

## 3. Row binding (Correlated by name only; nothing Mapped)
Creature2: Grond 32534 and 32535 (script filter); Drokk 32536 and 32539 (script filter); Trogun matched by script name only, creature ids not yet read from tables.
Spell4 names matching guide abilities (ids, all in the loose 63090 to 63260 block): Bloodburst 63099, Volcanic Strike 63106, Exanite Cuts 63107, Bellow 63109, Suppression Wave 63119 and 63120, Thrash 63121 and 63123, Scratch Itch 63129, Suppression Field 63131, Lockdown 63143, Bombshell Target 63144 and 63145, Exanite Shards 63156, Mutilate 63209. Name matches only; no text anchor or internal tag found for this dungeon yet.
**Correction to the Stormtalon survey:** that record described ids 63110 to 63267 as the Stormtalon block. The same id range also holds these Kel Voreth abilities, so it is a shared block of level-20 dungeon spells authored together, not a per-dungeon block. The Stormtalon binding rests on names plus the `[STL]` rows and the text anchor, not on the id range. Spell4 id adjacency is not a per-dungeon signal.

## 4. Timing
None measured. Needed: two clips per boss, telegraph/health events at 2 Hz or better with the boss selected (see the Blade-Wind timing attempt, which was inconclusive).

## 5. Open inputs
Second source for every threshold (all single-source); cast order and intervals; Trogun creature ids; which rows belong to normal versus veteran; spawn counts and positions; construct, flea and orb creature ids; the dungeon's own text anchors (`LocalizedText` ids referenced from these spell rows) not yet searched.

## Verdict
Kel Voreth has the richest threshold list of the first two dungeons (seven thresholds across three bosses), but it is single-source and has no cast intervals. Allowed next slices without invented values: disabled phase-tracking placeholders per boss using `EncounterHealthPhases` (Grond 75% and 50%, Drokk 85%, 65%, 45%, 25%, Trogun 50%), following the Blade-Wind pattern; a text-anchor search of the dungeon's spell rows; a second-clip check of the thresholds in the 2014-06-16 silver guide. None started.

Correction (2026-11-09): any "raw cooldown column" figure above is the spell's `CastTime` in milliseconds and the "cast time column" values 1 to 5 are the tier index; see the correction at the end of [DUNGEON_VIDEO_SURVEY_STORMTALON_20261109.md](DUNGEON_VIDEO_SURVEY_STORMTALON_20261109.md). Cast times are a table value (Mapped-level input), not an observed interval.

Update (2026-11-09, later): the second-source packet [INSTANCE_KELVORETH_SECONDSOURCE_PACKET_20261109.md](INSTANCE_KELVORETH_SECONDSOURCE_PACKET_20261109.md) did not confirm several numbers recorded above: Grond's 75% unlock is not found and Mutilate at 50% is contradicted (three 2014 clips say 20 to 30%); Drokk's 85/65/45/25 are not found; Trogun's 50% forge phase and four interrupt armor are agreed. Treat the thresholds above as unconfirmed single-source. The `[KV]` internal tag does exist in the Spell4 description column (183 rows, encounters e444 Grond, e446 Drokk, e449 Trogun); see [INSTANCE_SPELL4_INTERNAL_TAGS_20261109.md](INSTANCE_SPELL4_INTERNAL_TAGS_20261109.md).
