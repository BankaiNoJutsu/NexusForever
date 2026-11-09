# Instance packet: large raids, World Story and event instances (2026-11-09)

Phase B packet per [INSTANCE_ENCOUNTER_VIDEO_GOAL_PROMPT.md](INSTANCE_ENCOUNTER_VIDEO_GOAL_PROMPT.md), same format as [INSTANCE_KELVORETH_PACKET_20261109.md](INSTANCE_KELVORETH_PACKET_20261109.md). Rows: Redmoon Terror (3032, 3524, 3102), Augmentor's Raid (3040), BAD (3042), Vault of the Archon (3009), Journey into OMNICore-1 (3045), Shade's Eve (3044), Protostar Winterfest (3094). No code, no git, no retail parity claim, nothing here is Verified.

Evidence tiers used: **Observed** (subtitle read of a dated retail clip, or a table row read directly), **Correlated** (table name/tag/text anchor or clip agreement; "weak" = name only), **Mapped** (already in repo scripts). Every clip is reported by URL (corpus ids were renumbered 2026-11-09). "Single-source" = one clip or one commentator. All subtitles quoted below are YouTube auto-captions (garbled); I did not view frames or run ffmpeg for this packet, so no cast name was read from a cast bar.

## 0. Cross-cutting findings (read these first, they change what "missing input" means)

1. **Spell4 internal tags bind rows to encounters for the raids, much better than for the dungeons.** `Spell4.description` (the "description" column in `wildstar_client_mysql/Spell4.tbl.sql`) carries the authors' tag. Measured counts: `[RMT]` 955 spell rows and 251 creature rows; `[IC]` 103 spells and 33 creatures; `w3009` 117 spells and 215 creatures; `w3045` 98 spells and 269 creatures. For Redmoon Terror the tag even carries the PublicEvent 705 objective id of the encounter (`[RMT] e4550 ...` Engineers = objective 4550, `e4554` Mordechai, `e4556` Star-Eater, `e4558` Swabbie Ski'li), plus boss-name tags (`Robomination`, `Mordechai`, `Starmap`, `Laveka`, `Miniboss`, `Trash`). For Y-83 the tag is `[IC] e2681 - Augmentors - ...` (2681 is the first objective of PE 595, not the boss objective 2683). That is the "internal tag" binding the template allows, tier Correlated. It tells which rows belong to which boss; it does not give cast order, intervals or which phase. `[SE14]` (268 spell rows) and `[WF14]` (172) are event-wide (they also cover open-world holiday content), so they are weaker for the two seasonal instances.
2. **Boss yell lines are bindable to a creature through the table, not only through clips.** `Creature2.creature2ActionTextId` (column 154) points at `Creature2ActionText` (binary table only, not in the SQL dump), a row of three slots of four text ids plus a float 100.0. I decoded it for: Prime Evolutionary Operant 50472 (row 1079, one slot: 682949 to 682952), Laveka 65997 (1151: 766645, 766646, 766647), Swabbie Ski'Li 68655 (1139), Star-Eater 72758 (1150), Mordechai 65800 (1146), Engineers 65758 and 65759 (1144, 1145). The slot meaning (aggro, death, killed-a-player) is inferred from the text content only. Mid-fight yells (for example 682958 to 683012 for the Y-83 boss) are not referenced by any table I scanned (`instance_tbl_scan.py`), so their order and trigger can only come from clips; this matches the pilot's note. Row-level cast binding is still absent (no creature-to-spell table).
3. **Challenge objective texts are direct spell/creature anchors.** `LocalizedText` for PE 705 challenge rows embed `$(spell=...)` and `$(creature=...)`: Swabbie "launching 20 Risen with Scrubber Bubbles (Spell4 83944) within 3 seconds", Star-Eater "interrupting every Supernova (Spell4 85642) in under 18 seconds", Robomination "without killing any Cannon Arm (66084) while a Flailing Arm (66083) is alive", Mordechai "in under 4 minutes", Engineers "without letting the Engine Heat reach 60%", Starmap "without letting any Cosmic Debris (66231), Rogue Asteroid (66233) or World Ender (66234) impact Alpha Cassus (66154)", Laveka "Destroy 25 Tortured Apparition (65998) within 15 seconds then defeat Laveka". These are table facts (Observed in table), the strongest anchors in this packet.
4. **Subtitle quality is the limiting input.** Of the Redmoon Terror clips flagged `has_subs`, only five carry usable text; the 6288 s GreenyNeko stream has 6 cues. The long raid streams are raid-lead chatter, not boss guides.
5. **Medal thresholds are not in any table I scanned.** The client Lua `UI/DungeonMedals/DungeonMedals.lua` (from `I:\tools\extract_lua`) reads `peCurrent:GetRewardThreshold(Bronze|Silver|Gold)` and `PublicEventStatType.MedalPoints`, i.e. server-supplied. `PublicEventObjective.medalPointValue` has non-zero values only for PE 679 (Winterfest, 33 rows) among the events in scope. `PublicEvent.rewardRotationContentId` and `World.rewardRotationContentId` are 0 for every row in scope, and `PublicEventRewardModifier` has 6 rows, none for these events (I checked the event id column only).
6. **Footage dates vs build.** The tables are build 16042 (final). Redmoon Terror went live in 2016-08 (the 2016-06-28 news clip says "this August"; PTR clips from 2016-02 exist), Y-83 in 2015-05, OMNICore-1 in 2014-11, Shade's Eve and the SuperMall in 2015, Vault of the Archon in 2016-03/05 with Arcterra. Changes between a clip's date and the final build are not tracked; two-clip agreement across different years is therefore worth more than agreement within one stream.

### Summary matrix

| Instance (world) | Dated retail clips by title/description (distinct) | Usable text clips | Skeleton agreed by 2 clips | Strongest table anchors | Verdict |
|---|---|---|---|---|---|
| Redmoon Terror 20-man (3032) | 19 (2016-05-29 to 2018-07-13) | 5 | Robomination only (arms, lasers, room crush) | `[RMT]` tags, challenge texts, yell rows, 20 comm messages | partial; Robomination first |
| Redmoon Terror 40-man (3102) | 0 | 0 | none | PE 650 objective 525, Laveka 65997 | missing input |
| Redmoon Terror dup (3524) | 0 | 0 | none | none | missing input |
| Augmentor's Raid (3040) | 18 (2015-05-05 to 2018-05-01) | 6 | none | `[IC] e2681` 103 spells, yell rows, 682950 to 683012 | usable inventory, no skeleton |
| BAD (3042) | 0 | 0 | none | World row only | missing input, determination below |
| Vault of the Archon (3009) | 35 by title (about 7 gameplay) | 3 | none | `w3009` rows, PE 874 medal rows, summon rows 4952 to 4954 | partial, good table side |
| Journey into OMNICore-1 (3045) | 16 (2014-09-04 to 2017-03-26) | 5 | route skeleton (VO identical across clips) | `w3045` rows, comm 7355, boss objective 3031 | partial |
| Shade's Eve (3044) | 34 by title (about 6 instance) | 3 | story order (2 clips agree on the opening poem only) | `SE14 PE597 W3044` rows, text ids 594772/594774/594968/625568 | partial |
| Protostar Winterfest (3094) | 25 by title (about 6 SuperMall) | 3 | event structure (2 official streams agree) | PE 679 table incl. medals and timers, announcer text ids | best of the seasonal two |

Counts are `meta_fts` (title/description) matches with `upload_date` non-empty and before 20181128, unioned over the terms listed per section; they include non-gameplay clips (news, housing, cinematics). "About N gameplay" is my reading of titles, not verified clip by clip.

---

## 1. Redmoon Terror 20-man (worlds 3032 and 3524, public event 705)

### 1. Footage
Terms: "Redmoon Terror", "Red Moon Terror", Laveka, Robomination, Star-Eater, Swabbie, Lockjaw, Starmap, Navigation Core: 19 distinct dated clips, 9 flagged with subtitles, of which 5 carry usable text. Do NOT use the Skullcano "Mordechai Redmoon" guides (2014): that is a different Mordechai (dungeon boss), not Creature2 65800.
Best clips (all post-launch of the raid, 2016-08 onward):
* https://www.youtube.com/watch?v=lwjQxCnwpIM | 2016-08-18 | 11685 s | subtitled (3110 cues) | Swabbie Ski'li / Shredder progression with the raid lead's briefing at about 1240 to 1560 s.
* https://www.youtube.com/watch?v=uTsaUpvS-14 | 2017-03-01 | 647 s | subtitled (87 cues) | Robomination kill with callouts.
* https://www.youtube.com/watch?v=ecXo6yJo7wk | 2018-06-04 | 840 s | subtitled (142 cues) | Robomination guild-first, callouts.
* https://www.youtube.com/watch?v=Q_ouU6JgUCs | 2016-08-18 | 2647 s | subtitled (1222 cues) | early trash/miniboss run (Brig/deck; "Warden", "Shackles" talk).
* https://www.youtube.com/watch?v=Eqb7B8gDeN4 | 2018-05-01 | 9876 s | subtitled (392 cues) | description jump point "0:00:02 Starmap Tries" then Y-83; no usable Starmap speech.
Unreadable (no subtitle text) but dated: https://www.youtube.com/watch?v=e7QHMK2FJHE (2017-06-23, 396 s, Starmap kill), https://www.youtube.com/watch?v=a_lt_UB7fd4 (2018-03-20, 10238 s; description: Star Eater 1:31 to 7:15 kill, Star Map tries 20:03), https://www.youtube.com/watch?v=xcbzLgyHF4Q (2016-11-29, miniboss), https://www.youtube.com/watch?v=hJqytqM3D1M (2017-06-10, Lockjaw wipes), https://www.youtube.com/watch?v=yTj8ms0Caj4 (2018-01-16, 6288 s, 6 cues), https://www.youtube.com/watch?v=uAXmkedHAjA (2016-08-18, 11704 s). Pre-live PTR clip (Correlated at best, not opened): https://www.youtube.com/watch?v=TGEcmIcIjWQ (2016-02-28, 10601 s, no subs). Tier (normal/veteran) is not stated in any readable cue.

### 2. Skeleton
Table-derived structure (Observed in table, PE 705, 57 objectives, minimum level 50, type 22): approach (Shredder 4547, break back in 4557, brig 4590/4591 with Chief Warden Lockjaw 75214, elevator 4600, waste shafts 4604), then bosses: Swabbie Ski'li 4558 (68655), Robomination 4549 (66085), Engineers 4550 (Chief Engineer Wilbargh 65758 and Head Engineer Orvulgh 65759), Mordechai Redmoon 4554 (65800, with Oxygen counter 4653 as child), Star-Eater the Voracious 4556 (72758), Navigation Core "Starmap simulation" 4620 (73622), Laveka the Dark-Hearted 4621 (65997) with spirit-form challenge 5268. Minibosses: 4592 to 4599, 4613, 4623, 4625, 5141 (Fry-Cook Yagar, Mixmaster Kino, Cropper Gho'Deke, Rackmaster Antru, Lootwatcher Korgh, Bilgebreath, Munitions Specialist Luk'ki, Torpedo Technician Dud'li, Bonedoctor Muburu, Headshrinker W'gasa, Untombed Horror, Tiny), Marauder Officers 4617. Mechanic counters in the table: Engine Heat 5269 (max 600) with Spark Plug 5287, Cooling Turbine 5300, Lubricant Nozzle 5301, Fusion Core 5302; Reinforcements Remaining 5309 (max 5) and the line "Cassus will periodically resurrect fallen allies" (Starmap).
**Robomination (two clips agree on the callouts; two different commentators, still ASR):** arms and lasers arrive together; the Cannon Arm is the priority and is interrupted; a Flailing Arm must be killed in time; a crushing room mechanic (ASR "room shrinking" in uTsaUpvS-14 at about 613 s); a "spew" callout in both. Table side: Compactor "Slow Crush" (Spell4 75108 and 53 variants) with "Safe Zone" (75109), "The Sky Is Falling" tracking crush (75126), Laser "Incineration Laser" (75496) with "Melted Armor" debuff (83814), Cannon Arm "Cannon Fire" (83825) and "Decomposition" DoT (84118), Flail Arm "Seismic Smash" (75666), "Vile Revulsion" player vomit (83941), "Noxious Belch" raid damage (83824), "Summon Flail" and "Summon Cannon" from the compactor (85485, 85486). Challenge 5349 (above) agrees with "kill the cannon arm only after the flail is dead". Tier: Correlated.
**Swabbie Ski'li (single source, https://www.youtube.com/watch?v=lwjQxCnwpIM):** three add waves (the second contains a big add that casts something needing at least two interrupt armor plus a kick; telegraph the next wave on its corpse; the third wave spawns at the centre), then a "Shredder phase" (everyone stacks on the bomb, runs, dodges backward when circle telegraphs appear), kicks needed again in a "mid phase", and an attempt that reached 66% after the mid phase. Table side (`e4558`): Shredder pull/lift (77715 Scrubber Bubbles, 77717 Shredder Intake), Risen zombies (77841) with "Rise Again" (86917), Grund adds (Bilerush 84382, Bile Blast 84386, Oozing Bile 84321), Oghra (Necrotic Lash 84320), Razortail (Frenzied Pounce 85462), Grund Champion "Braugh the Bloated" with Deathwail stun AOE (86782) and Gravedigger knockdown AOE (86783), Shredding Solvent gauntlet (86798), "Skeedaddle" phase finish boost (86848), "Clean Sweep" raid wipe (86850), intro leap "Swabbie Swoop" (86929). The "big add needing interrupts" plausibly is the Grund Champion (Correlated, weak).
**Not covered by any readable clip:** Engineers, Mordechai, Star-Eater, Starmap, Laveka, all minibosses. Their skeletons exist only as table inventories (section 3).

### 3. Row binding
Creature2 ids already in repo scripts (`Source/NexusForever.Script.Instance/Raid/RedMoonTerror`, kill credit only): 75214 Chief Warden Lockjaw (obj 4590), 68655 Swabbie Ski'Li (4558), 66085 Robomination (4549), 72872 Assistant Technician Skooty (4552), 72873 Chief Engine Scrubber Thrag (4553), 65759 and 65758 Engineers (4550), 65800 Mordechai Redmoon (4554), 72758 Star-Eater (4556), 75645 Anti-Boarding Turret (4555), 72884 to 72887 Marauder Officers (4617), 72888 Bonedoctor Muburu (4613), 72889 Headshrinker W'gasa (4623), 72890 Untombed Horror (4625), 72891 Tiny (5141), 73622 Navigation Core (4620), script-name `LavekaTheDarkHeartedEntityScript` (Laveka 65997, 4621). Not yet scripted: minibosses 72874 to 72881 (4592 to 4599), Cassus 66165, Alpha Cassus 66154, Cannon Arm 66084, Flailing Arm 66083, Tortured Apparition 65998, Ishamel (comm caller) 75351.
Spell4 groups (internal tag, Correlated; raw `Spell4.castTime` shown nowhere because the column is unreliable): Robomination 88 rows (75108 to 86925); `e4558` Swabbie 57 (77715 to 87532); `e4550` Engineers 67 (75219 to 87498: Rev Up 83805, Overdrive 83911, Jumpstart 84012, Fastening Hook 84009, Ignition Spark 84038, Ion Clash 84050, Atomic Attraction 84052, Neutron Rupture 84054, Oil Slick 84072, Supercharger 84153, Liquidate 83810, Electroshock 83796); `e4556` Star-Eater 87 (75324 to 87159: Supernova 85642, Chaos Orbs 85577, Chaos Orb 85578, Chaos Nova 85585, Chaos Shield 85588, Astral Shard 85610, Astral Shield 85643, Moment of Opportunity 85655, Noxious Ink 85530, Flamethrower 85544, Summon Squirgling(s) 85536/85597, Jetpack 85427, Hookshot 85447, Tether Bomb 75324, Detonation 77716); Mordechai 63 (75340 to 87620: Vicious Barrage 85622, Kinetic Orb family 85561 to 85661, Anchor 85516 and Tether to the Deck 85515, Airlock family 75356/86825/86826, Pressure Shield 85542, Moment of Opportunity 85543, Shatter Shock 86749, Cross Shot 75359); Starmap 63 (75691 to 87745: Solar Winds 84298, Solar Flare 84300, Proton Storm 84388, Black Hole 85457 to 85459, Pulsar 84049, Planet Collision 75776, Deep Impact 75728, Comet 75716, Asteroid 75763, Wormhole 84340, Critical Mass 84345, Core Collapse 85519, Supernova 85461, Catastrophic Solar Event 87545, planet buffs 87533 to 87535, Return from the Cosmos 87540); Laveka 70 (75439 to 87905: Soul Rip 75507, Essence Void 75524, Fury of the Necroshaman 75531, Expulsion of Souls 75550, Soulfire 75574, Animate Bones 75584, Reanimate 75585, Cacophony of Souls 75591, Rend the Spirit Veil 75595, Devour Souls 75652, Boneclaw Gaze 85609, Soul Eater 86819, Soul Siphon 86898 "Hardmode - Activate", Manifest 87082, Spirit Ire 87766, Barrier of Souls 87774, Realm of the Living 75440 and Realm of the Dead 75528); Miniboss 250 rows (84293 to 87961); Trash 61. Two text-anchored rows: 83944 Scrubber Bubbles and 85642 Supernova (challenge texts). The Laveka row "Soul Siphon ... Hardmode - Activate" is the only hint of a hardmode switch (Correlated, weak).
Yell binding (table): Laveka 766645/766646/766647, Swabbie 753665, 761982, 761983, Star-Eater 766591 to 766593, Mordechai 761999, 762000, 762001, Engineers 761985, 761986, 761988, 761989. Ishamel the Bloodied (75351) has 20 CommunicatorMessages rows (8080 to 8085, 8102 to 8108, 8136 to 8139, 8141, 8145, 8146) keyed by creature, not by world; the repo enum `CommunicatorMessage.cs` already uses 8083 and 8108.
Objective ids per version: the table has one PE 705 (no normal/veteran split found); `Immortal:` rows 5280 to 5286 and challenge rows 5348 to 5354 are `cat 3`.

### 4. Timing
None measured (no 0.5 s resolution work, no readable clip for six of seven bosses). The only numbers are the table's: challenge limits 3 s, 18 s, 4 min, 15 s; Engine Heat 600 max and 60% threshold; Reinforcements 5.

### 5. Open inputs
Per boss: cast order, intervals, phase thresholds (none of Engineers, Mordechai, Star-Eater, Starmap, Laveka has a readable clip); which `[RMT]` rows are Normal-only versus the Laveka "Hardmode"; add counts and spawn positions; the Starmap planet/sun spawn geometry. Could another source supply it: unreadable clips above would need frame work at 720p (names not legible, so ability names must come from the tags above, which already exist); `a_lt_UB7fd4` and `e7QHMK2FJHE` are the candidates for health/phase timing. Normal versus hardmode cannot be separated from any clip read so far.

### Verdict
Table side is unusually strong (tags, challenge anchors, yells), footage is thin: Robomination is the only boss with two-clip agreement, so it is the sole candidate for a first slice; the other six bosses stop at "inventory, no skeleton".

---

## 2. Redmoon Terror 40-man (world 3102, public event 650)

### 1. Footage
None. Retail Redmoon Terror footage in the corpus is the 20-man raid (above); a title/description search for "40-man" returns only Datascape/Genetic Archives items (I did not check every cue). I found no clip of a 40-man Redmoon Terror.
### 2. Skeleton
Table only: PE 650 (type 0, flags 4, minimum level 50, `worldLocation2Id` 47473) has three objectives: 525 "Defeat Laveka the Dark-Hearted" (text, no creature id embedded), 4622 "(4622) [Hidden] Travel to Medbay" and 4624 "[Hidden] Travel to Morgue" (object ids 8007, 8008). Objective 525 is a low (2014-era) id, which suggests an early prototype fight; no more can be stated.
### 3. Row binding
Repo: `RedMoonTerror40ManEventScript` (owner 650) spawns Laveka from Creature2 65997 using the 20-man placement and model (WIP anchor), script name `RedMoonTerror40ManLavekaEntityScript`. Laveka spells/yells are the 20-man ones above, Correlated only because both fights use the same creature row; no evidence that the 40-man fight is the same encounter.
### 4. Timing
None.
### 5. Open inputs
Everything beyond "Laveka dies, event finishes": fight skeleton, version scaling (40 players), turnstile triggers 8007/8008. No other source identified; a native read of the world 3102 spawn data would be the only route.
### Verdict
Missing input: no footage and a three-row event; keep the scaffold as it is.

---

## 3. Redmoon Terror duplicate row (world 3524)

### 1. Footage
None specific. No table or Lua references world 3524 by a world-named column (`worldref.py` scan of the 79 SQL-dump tables for 3032, 3102, 3040 found their PublicEvent/WorldLocation2 rows, and none for 3524 or 3042); the only raw-value hits for 3042 and 3524 are id collisions in other tables (two hits for 3042, `Challenge.target` 434 and `Prerequisite.objectId0` 1894, are unexplained but not world references by their column labels, not examined further).
### 2 to 4
Table fact (World.tbl.sql): `(3524,'Map\RedMoonTerror',1,9,...,900,900,1100,1100,0,0,0,0,0,0,0,0,0)` versus `(3032,'Map\RedMoonTerror',1,9,...,900,900,1100,1100,0,753844,...,3,0,0)`: same asset, flags and chunk bounds; 3524 has no name text and `veteranTierScalingType` 0 where 3032 has 3. No PublicEvent, no MatchingGameMap, no script. 3524 sits next to 3522 (Coldblood Citadel) in id order, which suggests a late-added row; that is an inference.
### 5. Open inputs
What 3524 is for (second instance id of the same map, perhaps a scaled or replay variant). Nothing readable supplies it; only an entity/spawn or portal reference from the native side could.
### Verdict
Missing input; keep blocked. Do not bind any footage to 3524.

---

## 4. Augmentor's Raid (world 3040, public event 595; repo folder `InitializationCoreY83`)

### 1. Footage
Terms: Augmentor, Initialization Core, Y-83, Y83, Prime Evolutionary: 18 distinct dated clips (2015-05-05 to 2018-05-01), 13 flagged subtitled; text depth varies. In-game the raid is Initialization Core Y-83 (text 578430; World 3040's own name is "Augmentor's Raid", text 578435).
Best clips:
* https://www.youtube.com/watch?v=Eqb7B8gDeN4 | 2018-05-01 | 9876 s | subtitled (392 cues) | description jump points: Y-83 0:39:53, Prime Evolutionary Operants pull 0:52:17 (3137 s), kill 2:23:28 (8608 s); boss yells appear in the captions.
* https://www.youtube.com/watch?v=ldoJUDKCwxI | 2015-07-13 | 10920 s | subtitled (3649 cues) | 20-man learning run (positions, "star" telegraph, freebots, door).
* https://www.youtube.com/watch?v=fzFIHxEIS3c | 2015-11-17 | 347 s | subtitled (84 cues) | normal mode kill with compass callouts, infection, cleanse.
* https://www.youtube.com/watch?v=wgga7IzwGuY | 2015-11-16 | 365 s | subtitled (90 cues) | Mystic versus hardmode Augmentors.
* https://www.youtube.com/watch?v=EZTG4g7PA14 | 2015-05-06 | 283 s | subtitled (66 cues) | "world first (normal mode)".
Also https://www.youtube.com/watch?v=pMMqDOC67TQ (2016-01-26, "Hardmentors first kill", 10 cues), https://www.youtube.com/watch?v=C_7HtoPSGlQ (2018-01-21, HM first kill, no subs). Titles say "Hardmentors/Hardmode"; the table has one boss encounter, so which row is normal versus hard is unknown.

### 2. Skeleton
Table (PE 595, only 7 objectives): unlock door by activating 6 panels (2681, Creature2 68824, TargetGroup 12539), open Quarantine Door (2682, Creature2 66047, TG 12462), "Contain the Strain by defeating the Prime Evolutionary Operants" (2683, one encounter, plural boss), Immortal 2684, "Kill the Phagetouched Freebots" 4633 (TG 12574, count 4), "Evolutionary Revolutions" 4636 ("with the Organic Incinerator (65932) active"), "Challenge 2" 4637.
Video, single stream with ASR-to-text matching (Eqb7B8gDeN4, Observed): the boss's cycle of yells matches table text ids: 682959 "Operator relays compromised. Initiating annihilation sequence." (ASR hits at about 3514, 5776, 6703, 7422 s), 683010 "Data corruption detected. Radiation fields deployed." (3533, 6721, 7441), 683011 "Designating radiation quarantine. Seeking additional anomalies." (3894, 6267, 6759, 7739), 683012 "Corruption vector targeted. Initiating Bio-Phage eradication." (3741, 4095, 4473, 5794, 6400, 7782, 8422), 682992 "Techno-Phage transmission feeds established." (many). Intro/communicator: "The quarantine door is unlocked, friends! Destroy those corrupted constructs..." is CommunicatorMessages 7610 (text 678630), and 7609 (678629) precedes it. The stream includes wipes, so no cycle length can be stated. Normal-mode clips (2015) show compass-position callouts, an infection debuff that must be cleansed, "push"/"kill" calls on telegraphs, adds that should be cleaved down near the end ("7 percent" call in fzFIHxEIS3c), and a "giant explosion" after which a group enters. These are single-clip impressions, not agreed.

### 3. Row binding
Repo (kill credit and objective routing only): Creature2 66047 (Quarantine Door boss room) and 68824 (Control Panel); owner ids 3040, 595, 2681. The boss creatures are not in any script: 50472 Prime Evolutionary Operant, 50423 Prime Phage Distributor (`[IC] e2681 ... Phaged Evolutionary Operant`), 65780 and 65932 Organic Incinerator (laser ball/panel), 50425 Sternum Buster (chestacabra add), 50428 Augment Pool, 61597 Circuit Breaker, 65752 Fallout Unit, 69224 to 69780 Infested Corpse variants, trash 68859 Phagetouched Freebot, 69130 Drillbot. Spell4 `[IC] e2681` inventory (Correlated, internal tag): Strain Injection 48599/71280/71446/75143, Chest Burst 48608, Digitize 48699, Compromised Circuitry 48735, Radiation Bath 71164 with Radiation Aura 71165, Circuit Corruption 71262, Corruption Spike 71281 to 71283, Fallout 71335 to 71349 and 75182 to 75198, Transmission 71356/75352, Volatile Contagion 75326 to 75607, Disintegrate (Laser) 75258/75259 with Interrupted Proc 75256, Degeneration 79892, System Reboot 80412, Nanostrain Infusion 50075/80483, Disintegration Resistant Coating 79895; trash `[IC] Trash`: Aggressor (Grinder 77970, Mega Punch 78204), Mining Bot (Recursive Reaction 78265, Tainted Suppression 78286, Pop Corpse 78632), Infested Corpse Burst 78311. Yell binding: 50472 -> `Creature2ActionText` 1079 (682949 to 682952); 682958 to 682999, 683009 to 683012 are the mid-fight lines (unreferenced).
The "Laser/Disintegrate" rows with "Interrupt Proc" and the challenge row 4636 (Organic Incinerator active) describe the same mechanic; Correlated.

### 4. Timing
None; the 2018 stream contains wipes and no segment was cut.

### 5. Open inputs
Phase thresholds, cast order, intervals, adds counts, positions, and the normal/hard split; the identity of the Prime Phage Distributor in the fight (two boss rows for a plural objective); Phagetech Augmentation mechanics for 4636 and the party-death fail state for 2684 (also noted in the coverage CSV). Candidate sources: `ldoJUDKCwxI` (3649 cues) for a threshold read-through; native read of the creature spell list (not commissioned).

### Verdict
Y-83 has the best yell/spell inventory of the three raids (103 tagged spells, the full yell list) but no agreed skeleton; next safe step is a text-id-keyed yell table and disabled phase placeholders, not casts.

---

## 5. BAD (world 3042)

### Determination from tables, Lua and text
* Table: `INSERT INTO World VALUES (3042,'Map\Datascape',0,9,...,0,0,2048,2048,0,578429,0,0,0,0,0,0,0)`: type 9 (raid), flags 0, asset path `Map\Datascape` (the same asset as world 1333, which has flags 1 and a real name 439159), `localizedTextIdName` 578429 = "BAD", all other columns zero. 
* Text: 578429 "BAD" sits in a block of developer strings next to 578430 "Initialization Core Y-83", 578435 "Augmentor's Raid", 578432 and 578433 "[UP] e2680 - Vending Machine ..." and 578434 "Dance Floor", i.e. it was authored in the same pass as the Y-83/Ultimate Protogames strings. WorldZone 4362 references 578430 only.
* Cross-references: no PublicEvent, no MatchingGameMap/Type, no WorldLocation2, no Spell4/Creature2 description containing "BAD" for this world, no repo script, no Lua file (`I:\tools\extract_lua`) mentioning 3042, 3040, 3032, 3102 or 3524. `anyref.py` over all 384 extracted tables found only id collisions for 3042.
* Conclusion (Observed + inference): 3042 is a placeholder/duplicate raid world row pointing at the Datascape map asset with a developer name; nothing shows it was ever playable or populated. It is not a separate encounter. There is no footage.
### Open inputs
Whether the row is reachable in the shipped client (only a native read of world/map load or a live probe could say). Nothing in the corpus supplies it.
### Verdict
Missing input: keep `client_recorded_review_required`; no encounter work.

---

## 6. Vault of the Archon (world 3009, World Story, public events 666, 668, 669, 677, 678, 693, 696, 874, 875)

### 1. Footage
Terms: Vault of the Archon, Hall of the Hundred, Varegor, Harizog, Arcterra: 35 distinct dated clips by title/description, most are Arcterra zone content; about seven are instance gameplay or official statements. Matching entry: MatchingGameMap 103 "Vault of the Archon" uses game type 75 "World Story - Group" (level 50, team size 5); a solo type 74 exists but is not what row 103 points at, while the official stream (below) says solo play is possible, so how the solo entry works is not shown by the tables I read.
Best clips:
* https://www.youtube.com/watch?v=Keza16SraM4 | 2016-05-13 | 1071 s | subtitled (408 cues) | "Part 3": a boss fight at about 504 to 650 s, then "silver medal" at about 678 to 698 s with rewards.
* https://www.youtube.com/watch?v=vF2fQvphDtA | 2016-05-09 | 1075 s | subtitled (646 cues) | "Part 1": opening, Dorian Walker, Bloodhearth tree, optional Pell Graveyard (at about 465 s), gate at Varegor Pass.
* https://www.youtube.com/watch?v=mRGjTg3MUZU | 2016-02-09 | 4112 s | official Carbine weekly stream | states the instance can be played solo or in a group.
* https://www.youtube.com/watch?v=CYA9tafpjRg | 2016-06-06 | 2049 s | no subs | solo Dominion run.
* https://www.youtube.com/watch?v=EwoFqQ9Kw5Y | 2017-04-08 | 2637 s | no subs | medic walkthrough.
Parts 2 and 4 onward of the Button Mash series did not match my search terms; I did not search for them under other titles.

### 2. Skeleton
Table (85 objectives across 9 events): PE 666 main line: Dorian/Artemis opening, locked gate 4299, **Varegor the Abominable 4300** (bridge to Kel Havik), Kel Havik regroup/tablets 4306/4308, watchtower with optional **Unbound Flame Elemental 4318**, optional **Icebound Overlord 4972**, optional **Darkwitch Yotul 5001**, courtyard statue 4320, keys 4322, force-field power link 4327 ("Kill constructs to get Levitation Field Energy (Spell4 83784), then jump through the Force Field Power Link"), **Harizog Coldblood 4328** with three summoned-ally rows **4952 Havik Shiverhound, 4953 Darkwitch Uhrga, 4954 Havik Honorguard "summoned by Harizog"**, ice-prison witness 4973, exit blocker 4329. PE 668/669 protect NPCs; PE 693 Warhound Kennel (Varegor Watchhound 67884; Cerebogg 67924, Fenrogg 67925, Orthogg 67935); PE 696 Pell Graveyard (Primal Wraith 68013); PE 677 Cold and Hungry (Frorefang the Starving 67657); PE 678 Cold Soup (Soulless 67862 owner not found); **PE 874 "Complete Optional Encounters to Earn a Silver Medal" (5153, count 3) and "...a Gold Medal" (5154, count 2)**; PE 875 escape: "Escape Kel Havik by running to the Transport Ship" (4333), summon Arctic Shredder (5266).
Video (single clip, Keza16SraM4, Observed): the fight at about 504 to 650 s has the boss summoning helpers "in different phases", buffing himself, deflecting damage, "cyclones doing the rounds", and an apparent return ("he's back") after a phase; the cue for the boss name is garbled ("hair is out cold", I read it as Harizog but that is not established, and "Harizog" appears in no readable cue; a title/cue search for Harizog returned 0). Afterwards the transports land across the bridge, a "silver medal" is announced, and the vault cache gives "an item level 80 chest guard, ... and 630 renown" (ASR). Table agreement with this clip: summoned allies (4952 to 4954) and "silver medal" semantics (874).

### 3. Row binding
Repo (`WorldStory/HallOfTheHundred`): Creature2 ids in scripts include 67423 Dorian Walker, 67425 Artemis Zin, 67429/67430 Havik Icebreaker/Greycaster, 67444 Harizog Coldblood, 67457 Varegor the Abominable, 67657 Frorefang, 67659 Frozen Carcass, 67778 Body, 67797 Soulrot Containment Basin, 67841/67842 Soulrot Container, 67851 Havik Shiverhound, 67853 Havik Honorguard, 67855 Darkwitch Uhrga, 67884 Varegor Watchhound, 67940 Frozen Lever, 67962 Unlit Incense, 68012 Sacred Bas-Relief, 68013 Primal Wraith, 71173 Darkwitch Yotul, 71414 Unbound Flame Elemental, 71577 Icebound Overlord, 71617 to 71619 Kel Havik weapons, 72108 Weak Point, 72199 Elevator Control, 72367 Door Lock, 72903 Force Field Power Link, 72958 Key Fragment; owner ids 3009, 666, 677, 678, 693, 696 and event-object ids 7834 to 8426.
Spell4 `w3009` tag rows (Correlated, internal tag; 117 rows 74401 to 87134, raw cast columns not used): Harizog/boss fight ("Part 6 - Fight" and "Boss Fight"): Snow Shield "Final Boss Immunity" 83431, Snow Devil boss hazard 81623 to 81627, Violent Snowstorm 81714, Firestorm 81828/81830, Heroic Presence 76505/76691, First Aid 83947 and First Aid Toss 83948 (sidekick heal), Boss Leap 83961 (tagged [DEV]), Pull Sidekick 76274, environment kill spells Avalanche 76264, Molten Metal 76320, Thick Smoke 76377, Fire Barrier 76537/76551; Osun Witch boss (Uhrga/Yotul): Horrors Arise! 81660, Summon Adds 81661, Dark Healing (add) 81650, Wake Horror 83054/84358; Fire Elemental boss: Volatile Spark Knockback 83053; side missions (Soulrot Rend Souls 83739, Ice Breaker 76689, Frozen 76634, Blistering Cold 76081 to 76084 hazard for objective 4332). Creature2 `W3009 - Part 6` rows: Harizog 67444, Havik Hexer 67473, Avalanche Weakpoint 67636, Fractured Pipe 67637, Vent Lever 67638, Molten Metal caster 67713, Vent caster 67735, Avalanche caster 67662. A separate Coldblood Citadel Harizog (75459, `[CBC]`, Spell4 87686 to 88632) is a different, later encounter: do NOT use those rows for 3009. The `[DEV]`/`WS0` rows (Archon portal, Cyclopean horrors, Strain Bomb 77533) are deprecated prototype content.

### 4. Timing
None measured. Table timers: none stated for the events in scope.

### 5. Open inputs
Harizog phase structure and thresholds; which of Uhrga/Shiverhound/Honorguard comes in which wave; boss identity of the Part 3 fight; Varegor mechanics; the three optional bosses (4318, 4972, 5001); medal point values and thresholds (874 is a count of optional encounters 3 and 2, not points; thresholds are server-side per section 0); reward items (the "item level 80 chest guard, 630 renown" is ASR from one clip). Candidate sources: `EwoFqQ9Kw5Y` and `CYA9tafpjRg` (no subs) would need frame work; a native read for the reward pane.

### Verdict
Best table side of the world stories (tagged boss rows, summon rows, medal rows) and a single clip that agrees with it; Harizog is the one candidate for a slice, but it needs a second clip first.

---

## 7. Journey into OMNICore-1 (world 3045, public events 605 and 631)

### 1. Footage
Terms: OMNICore, OMNI-Core, Omnicore, Omni-Core 1: 16 distinct dated clips (2014-09-04 to 2017-03-26), 12 flagged subtitled. Matching entry: MatchingGameMap 102 uses game type 74 "World Story - Solo" (team size 1, level 50).
Best clips:
* https://www.youtube.com/watch?v=afOvcqC0zlc | 2017-03-26 | 2884 s | subtitled | medic playthrough; the Caretaker/Axis Pheydra voice lines are captured cleanly.
* https://www.youtube.com/watch?v=CV9BmshkMJs | 2015-07-16 | 4012 s | subtitled | Aurin Esper solo playthrough (Cortex Controller fight at about 3406 to 3606 s).
* https://www.youtube.com/watch?v=aivqCvQlEiA | 2014-12-31 | 2672 s | subtitled | launch-month playthrough.
* https://www.youtube.com/watch?v=3fa0VmjymnE | 2015-01-11 | 3303 s | subtitled | second playthrough.
* https://www.youtube.com/watch?v=e28j8USbluE | 2016-11-27 | 276 s | subtitled | all cinematics.
Pre-launch/PTR (Correlated at best): https://www.youtube.com/watch?v=zQ7TPBpTKyo (2014-09-07, 2819 s, no subs).

### 2. Skeleton
Table (PE 605: 127 objectives listed; PE 631 returned no objective rows in my query): opening talk/listen with Belle Walker and Axis Pheydra (2740 to 2743, repo-mapped), **Choose path A or B 2744**, firewalls (2745, 2746, 2896, 2971), circuit boards and generator power (2747, 2972, 3175 to 3177, 3189), Power Continuum door/bridge (2748, 2755, 2950, 3015), constructs (2752, 2756 OMNICore Recovery Construct 62777), Memory Core (2761 to 2763, 3030, **3031 "Defeat Avatus' prototype holo-creature"**), data-matrix code decryption (2942 to 2946, 3154 to 3157), maintenance shaft route (3040 Shaft Security Construct 65082, 3041 to 3052 with override code and Main Power/Power Router), Cortex (3034, **2773 "Kill the Cortex Controller" 65186**, 2771 speak inside the Cortex, 2772/2774 flip the OMNICore Killswitch), holo-projection groups (3134 to 3136 Wilderrun/Whitevale/Malgrave).
Video (three subtitled clips: afOvcqC0zlc, CV9BmshkMJs, aivqCvQlEiA; the voice lines are scripted and identical across clips, so this is agreement of text, not independent timing): kill switch goal; Avatus deploys firewalls blocking the access tunnels; two possible courses ("Two tasks, two agents. Choose one task and I shall carry out the other"); firewalls and power conduits; Power Continuum; Memory Core ("All three memory cores retrieved"); teleporter versus shaft to the Cortex; Security construct locked in with you; Cortex fight where "the controller's healing itself behind that shield, take out its constructs", lasers "another force field ... stop the controller from regenerating". 

### 3. Row binding
Repo: owner ids 3045 and 605; scripted objectives only 2740 to 2744 (opening talk, listen, choose path); no Creature2 filters. Table anchors: `w3045` tag rows (98 spells 72537 to 83892, 269 creatures): Cortex Controller fight: Force Field "Cortex Boss Immunity" 75762, Cortex Rotating Laser "Assault Beam" 75080, Cortex Deadly Rays 75775, Memory Core Healing Protocol 74984, Hologram Mimic 75673; Avatus holo-boss rows "Projecting Avatus" 73280, Avatus Blast 74675, Avatus Shock 74676, Avatus Hold 74765, Smash Avatus 74779, Avatus Overload 74780, Shock Wave "Boss Disengage knock back" 75487; Holo Cannon Shock Ball 72889 to 72891 and Holo Blast 75160 to 75176; Grapply Hook 72643/73214/74792; Maintenance Shaft Escape Protocol 74772; Decryption Sequence 73031. Creature2 `w3045`: 65186 Cortex Controller (actionSet 3700), 62759 to 62777 OMNICore Blaster/Auxillary/Conduit/Protector/Eraser/Recovery, 65912 Main Power Generator, 67840 Kill Switch. Voice text: CommunicatorMessages 7355 -> text 620849 ("All three memory cores retrieved...") is the one table-bound line I found; 611216, 615415, 616114 and 621849 are not referenced by tables I scanned.

### 4. Timing
None.

### 5. Open inputs
Path A versus path B objective split; which Creature2 are the "memory core constructs" ("kill all at once" 2763); Cortex Controller phase thresholds, laser rotation and shield cadence; the holo-creature (3031) fight. A second independent route would be silent-commentary playthroughs; the three subtitled clips only repeat the same VO.

### Verdict
Route skeleton is well covered by the VO text; boss mechanics (holo-creature, Cortex Controller) stop at tagged spell inventories. Not a candidate for a boss slice yet.

---

## 8. Shade's Eve instance (world 3044, `Map\ShadesEveInstance`, public events 597, 627, 630, 632, 633)

### 1. Footage
Terms: Shade's Eve, Shades Eve, Quiet Downs, Jack Shade: 34 distinct dated clips (2014-08-21 to 2018-10-25), mostly housing/costume content; about six show the instance. Matching game: MatchingGameMap 98 "Holiday Expedition: Quiet Downs" (type 71 "Expeditions - 10+", 5 players, level 10 to 50; it scales to 50 per one clip). Event was limited-time (October).
Best clips:
* https://www.youtube.com/watch?v=KqVO--Ntybw | 2015-10-21 | 1212 s | subtitled (dialogue-only captions) | full instance, the Angel/Eva/mayor/Jack Shade voice.
* https://www.youtube.com/watch?v=uocU6-cxbPk | 2017-10-22 | 842 s | subtitled | player impressions; confirms the opening poem, scaling to 50, the mayor turning into Jack Shade.
* https://www.youtube.com/watch?v=dVoV1zb2SMU | 2016-10-22 | 512 s | subtitled | vlog, almost no instance content.
* https://www.youtube.com/watch?v=OrGcB42qLS0 | 2015-11-06 | 1226 s | subtitled | capital quests and housing, adjacent only.

### 2. Skeleton
Table (PE 597 with children 627, 630, 632, 633): forest escape to the fountain (2705, 2724), Quiet Downs town with a choice of "Option 1: Investigate" (clues 2976, 3060 in PE 632, Etty/Eva talks 2737, 3057) or "Option 2: Assist with the festival" (candles 2848, effigies 2849, barrels 2850, cauldron 2851; vandals in PE 633, 3061), barn/outsider escort and ambush (3093 to 3102, 3146 to 3166), costume (2875, 3292), ritual eavesdrop (2967, 2968), key (3065/3106), bunker and lab doors (2728, 2866), **Defeat the Mayor 2823**, **Kill the Shadestricken cultists (22) 2838**, morgue gather 2839, **"Defeat Jack Shade with the help of the Angel" 2840 (object 10567)**, escape 2841/2854, "Witness ... the rebirth of Jack Shade" 4770, optional challenges `cat 1`: destroy 9 lab equipment 2842, kill 3 Plaguebringers 2843 (11081), "Escape the laboratory without being touched by the Shades plague" 2844, "Don't get turned into a shadeling" 2846, "Don't get caught" 3063.
Video (KqVO--Ntybw, with the opening poem also in uocU6-cxbPk): Observed order of dialogue: forest/thicket and the Angel's warnings, the opening poem ("Put on your mask, hide with care, and pray the Angel finds you there", Spell4 anchor 629832 on companion spell 75795 "Inoculation of the Angel"), Quiet Downs and Eva, the mayor's notes, the barn meeting, disguises, the ritual and the Key Master, the bunker and lab; mayor lines at about 713 to 723 s ("You cannot stop us! Jack Shade will be reborn!", text 625568), Jack Shade's lines at about 751 to 766 s and 900 to 902 s (texts 594772, 594774, 594968), the Angel at about 1123 s ("the mayor is defeated ... run down the hall, don't let it touch you") and the plague-escape at 1143 to 1182 s. These timestamps are subtitle times only. The yell text ids above are not referenced by any table (script-fired). Single source for the order except the poem (two clips).

### 3. Row binding
Repo: `EventInstance/ShadesEve` owners 3044 and 597; `CommunicatorMessage` enum 7152/7153 (the Angel). No Creature2 filters in the scripts. Table anchors: creature rows tagged `Holiday SE14 - PE597 W3044`: **62681 Jack Shade (Instance Boss)**, 62682 The Angel (Instance Boss), 65933 The Angel (Comm Call), 62793/67288 Mayor Goodthorpe, Plaguebringers 62810 to 62813 and 63367 to 63375, Shadestricken cultists 62830 to 62832 and 62814/62815, Experimental Shadeling 62824, door/switch objects 63026 to 63057, plague hazards 63134 to 63136, "Jack Birth VFX" 63490, Jack's Hat 66102 and Unstable Singularity 66144 ("Jacks Revenge"), Virulent Lab Jabbit 65798 (names spell 75289) and 67722 (names spell 76333). Spell4: instance tags `SE14 PE597 W3044` (Shade Plague hazards 72856 to 72861, Jack Spawning Hazard 75888/75889, Darkness skybox 73236, Jack's Angry skybox 73277, door/refight Yes/No spells 72798/72799/72801/72824); the boss's abilities appear only under `Jack Shade (Group Boss)` / `(Warplot Boss)` templates: Hat Trick 76330 to 76332, Virulent Eruption 76333/76334/76339, Terrorize 76335/76336, Boomerang Derby 76337/76347, Poison Jets 75348/75353, Plague Nova 76595 to 76597, Jack's Last Stand 75660 (Warplot variants share names). Which template belongs to creature 62681 is not stated by any table (the `Instance Boss` creature versus `Group Boss` spell templates); Correlated (weak). Communicator rows keyed to zone 4382: 7152 to 7155, 7220, 7384.
Scoring/rewards from footage: only that the instance gives experience (about 40% of a level at 30, one clip) and the player expects cosmetic rewards (one clip, opinion). From tables: no `medalPointValue` in PE 597, `rewardRotationContentId` 0, optional challenges are `cat 1` rows without point values. Rewards must come from tables not yet located (loot/quest reward tables for the holiday expedition).

### 4. Timing
None.

### 5. Open inputs
Jack Shade fight skeleton (phases, health thresholds, which templates are used), the Mayor fight, plague-escape hazards pattern (Circle/Rectangle/Pathing hazard rows exist, geometry not), the reward/loot tables, how the 2823 and 2840 fights map to PE 627/630. Candidate sources: `Kaos Sverige` Shade's Eve parts 5 to 7 (no subs, 10473 s, 1619 s, 4501 s) for health/phase frame work; native read for the spell list.

### Verdict
Story order and boss yell text ids are solid; the boss mechanics and rewards are missing; limited-time content, so footage that exists (one full-run clip) is the only witness.

---

## 9. Protostar Winterfest / SuperMall-in-the-Sky (world 3094, `Map\ProtostarWinterfest`, public event 679)

### 1. Footage
Terms: SuperMall, Super Mall, Winterfest: 25 distinct dated clips (2014-12-16 to 2018-11-26), mostly housing and cash shop; about six concern the SuperMall. MatchingGameMap 97 "Holiday Expedition: SuperMall-in-the-Sky" (type 70 "Expeditions - 10+", 5 players). Limited-time (December).
Best clips:
* https://www.youtube.com/watch?v=wi9FoOqkvR8 | 2015-12-14 (stream of 12-11) | 4389 s | subtitled | official WildStar stream: live demo of Shop 'Til You Drop, Fire Sale, Chain Store, Q&A on scoring.
* https://www.youtube.com/watch?v=BUM_nL1s_LM | 2016-12-19 | 3398 s | subtitled | official 2016 preview stream (repeat of the demo, randomisation statement).
* https://www.youtube.com/watch?v=mr3vKbWp_x4 | 2015-12-18 | 1552 s | subtitled | full run with the announcer lines in order.
* https://www.youtube.com/watch?v=c6-nhfHcV8w | 2015-12-28 | 1396 s | no subs | medic healer run.
* https://www.youtube.com/watch?v=5YEP2MiM90U | 2018-11-26 | 4295 s | no subs | Thayd questing including the SuperMall (not verified).

### 2. Skeleton
Table (PE 679, 64 objectives): grand-entrance gather 4388, 13 per-store gather rows (Fire Sale 4575, Chain Store 4615, Bargain Basement 4609, Slashing Prices 4616, Last One in Stock 4618, Shop 'Til You Drop 4603, BOGO 4573, Doorbuster 4537, Batteries Not Included 4562, AutoMania 4526, Ferris Wheel Heights 4567, Gift Tower Center 4574, Main Event Showroom 4578) and per-store objectives with failure timers and medal points (below), Main Event 4577 (type 16, count 3, 300 points). Many `[DEV] Invis` controller rows.
Video, official dev streams, two clips from two years agree: events in the mall are randomised on each entry, "five of the random stores and then the main event" (2016: "You will have five of the random stores and then the main event"; 2015: "you have to do five of them before you get the main event"), about a dozen events in total (table: 13 gather rows). Every event except the main event is non-combat; solo, small group and a full group of five all work and the encounter scales with group size. The 2015-12-18 full run shows the announcer order in that run (not a fixed order): emergency gift retrieval ("pest control", festive slanks), Fire Sale, Shop 'Til You Drop, AutoMania, a limited-time offer (price bots), then the Main Event. Announcer lines are `LocalizedText` ids not referenced by any table (script-fired): 636229, 635724 (Doorbuster), 636365 (AutoMania), 637106 (Fire Sale), 637159 (BOGO), 638547 (Toybot), 638559 (Shop Till You Drop), 638724 (Bargain Basement), 638807 (pricing bots), 638811 (Chain Store), 637565 and 730943 (Main Event), 638560 (Shop Till You Drop explainer), 636376 to 636378 (Mega Mount sale completion lines).
**Scoring and rewards, footage versus tables:** footage (Observed, official streams): optionals add more rewards "at the very end"; medals bronze, silver, gold ("headed for the bronze, silver probably"); better performance gives more currency ("Cold Cash") per event; the Shop 'Til You Drop timer is "pretty tight" and the Chain Store timer is "almost five minutes"; the Fire Sale fails if five pieces of merchandise burn; chain bots cannot be killed; holiday vendors sell decor/costumes; the SuperMall also drops decor. Tables: `PublicEventObjective.medalPointValue` (PE 679 only, 33 rows): of the 33 rows with points, 23 carry 100 (4475, 4476, 4479, 4480, 4481, 4484, 4488, 4489, 4525, 4527, 4539, 4540, 4566, 4568, 4570, 4571, 4572, 4581, 4601, 4608, 4646, 4647, 5122), 5 carry 200 (4509 toybots, 4542 BOGO, 4543 PrICE bar, 4559 doorbuster, 4602 shop), the four door-buster door rows 4502 to 4505 carry 25 each, and the Main Event 4577 carries 300; the `cat 1` rows are the optional objectives; failure timers (`failureTimeMs`): 4539 Chain Store 300000, 4646 300000 (price bots), 4602 Shop 'Til You Drop 250000, 4509 Toybots 250000, 4559 Doorbuster 270000, 4527 mount displays 210000, 4542/4543 120000, 4608 plushies 240000, 4481 260000, 4484 160000, 4488 140000, 4489 60000. The table's 300000 ms for the Chain Store agrees with the "almost five minute" remark (Correlated). **Not in any table scanned:** the bronze/silver/gold thresholds (`GetRewardThreshold` in the client Lua comes from the server) and the reward items; footage gives neither a number.

### 3. Row binding
Repo: `EventInstance/ProtostarsSuperMallInTheSky`, owners 3094 and 679, objective 4388 only (greeter gather, coarse gate); no Creature2 filters. Table anchors: `[WF14]` is event-wide (172 spells, 336 creatures): BOGO Event buff 77164, Advertising Bot 77919, Take My Money! 77986; creature rows used by the instance: Protostar Mall Greeter, Slank nests/Festive Catcher targets and Toybots are in the PE 679 object ids (12343, 12429) but I did not resolve each Creature2 name. `[WF14]`-tagged rows cannot be assigned to 3094 without that resolution.

### 4. Timing
Table timers listed above are the only numbers; footage remarks ("almost five minutes", "tight") are consistent but unmeasured.

### 5. Open inputs
Per-store scripts (spawn counts, weights in Shop 'Til You Drop, chain-bot patterns, fire spread), medal thresholds, rewards (cold cash amounts, vendor/decor drops), how many stores are drawn per entry and the draw rule (footage says five plus the main event, the table has 13 gather rows). Candidate sources: the 2015-12-18 full run and the two official streams for per-event rules; thresholds would need the server-side or native side.

### Verdict
Best-documented of the two seasonal instances: official footage states the run structure, the table has points and timers per objective; thresholds and rewards remain missing.

---

## What I did not verify
* No frame was viewed and no ffmpeg job was run; all "Observed" video items are subtitle reads (YouTube auto-captions); ability names were never read from cast bars.
* Normal versus veteran/hard tier of any clip.
* Whether other clips exist under titles my meta/cue terms did not match; counts are title/description matches, unioned per section, and include non-gameplay clips.
* The ASR-to-text matches for the Y-83 yells (phonetic, by me); the Part 3 Vault of the Archon boss identity; the Creature2ActionText slot meanings.
* Whether Spell4 `[RMT]`/`[IC]`/`w3009`/`w3045` rows are used by the shipped encounter versus prototype leftovers (some rows are tagged `[OLD]`, `[DEV]`, `UNUSED`); the raw `Spell4.castTime`/cooldown columns were not used.
* PublicEvent flag meanings (516, 512, 8, 4) and the PE 705 type enum 22; the `Challenge.target` 434 and `Prerequisite` 1894 hits for 3042.

## Method notes
Helper scripts used from `Decomp/Analysis/scripts` (unmodified): `instance_cues.py`, `instance_cue_grep.py`, `instance_text_search.py`, `instance_tbl_scan.py`. Throwaway scripts (corpus reads, SQL-dump loaders, tag searches, Creature2ActionText decode, comm-message dump) live only in the session scratchpad and are not part of the repo. Table sources: `I:\GIT\NexusForever-assessment\wildstar_client_mysql\*.tbl.sql` (Spell4, Creature2, PublicEvent*, MatchingGame*, World, CommunicatorMessages), `I:\tools\extract\DB\*.tbl` (Creature2ActionText, anyref scan), `I:\tools\extract_en` (text), `I:\tools\extract_lua` (DungeonMedals UI).
