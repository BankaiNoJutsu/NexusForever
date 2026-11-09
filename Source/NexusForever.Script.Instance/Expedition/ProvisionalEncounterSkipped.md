# Expeditions skipped for provisional encounter scripts (2026-11-09)

No file was written for any of the seven expeditions below. The bar was: a boss creature id verified by name against Creature2, plus Spell4 rows tied to that boss by a tag, text anchor or internal name (name-only matches do not count), plus an ability the packet reports from clips. Sources: INSTANCE_EXPEDITIONS_PACKET_20261109.md, INSTANCE_BOSS_ABILITY_CATALOG_20261109.md (tag families present: STL, KV, SK, CBC, GA, DS, RMT, IC, UP, UPJ, TS; none belongs to an expedition) and a search of every description in coverage/spell4_internal_tags_20261109.csv for the boss names and the world tags W1232, W1319, W1627, W2149, W2183, W2188 and W3180.

Creature ids were checked with instance_creature_names.py. The ids that match their boss by name are listed per expedition; the Spell4 side is what is missing.

## Infestation (world 1232)
* Creature ids verified: Lumbering Parasite 22669, Cyclopean Parasite 26833.
* Missing: no Spell4 row names or tags either parasite (only `Toxic Fumes - Sky Change - W1232` 30570, an environment row, and `Grazer Guardian (Corrupted - Life) - Infestation` 70647/70650, a different creature). The packet reports the Cyclopean split and spin only as descriptions with no cast name.

## Outpost M-13 (world 1319)
* Creature ids verified: "Ransacker" Rorgh 23455, Hive Queen 23513.
* Missing: no Spell4 row carries Rorgh, Ransacker or Hive Queen in its description, and no W1319 tag exists. The packet reports only "has a cast to interrupt" with no cast name.

## Rage Logic (world 1627)
* Axiom has no Creature2 id in the repo, so none could be verified. The overseers are Overseerbot 69107 (an asteroid defender row, not a boss).
* Spell4 43574, 43575, 43624 and 43625 are tagged `Exile Warbot (Axiom Only W1627) - Smash N' Trash` (TELEGRAPH rows). They are tied to Axiom's world by internal name, but the packet reports only "Axiom fires rockets into the air", which is not that ability, and the creature id is still missing.
* Spell4 43616 Ragebot Driller Hack (text anchor to Creature2 32325, name checked as Ragebot Driller) is a player action, not a boss cast.
* Missing: Axiom Creature2 id, and a packet-reported Axiom ability that maps to a Spell4 row.

## Space Madness (world 2149)
* Creature ids: bosses are not named in the packet (only the vending machine and the scrub wave), and no creature id was identified for them.
* Missing: the `Vending Machine` rows in the catalog (71203 to 79418) sit under `[UP] e2680` (Ultimate Protogames, Bev-O-Rage), not Space Madness, so they are not a binding. No W2149 boss row exists, and the packet gives no cast list.

## Gauntlet (world 2183)
* Creature ids verified: Shock King 48554 and 69305, "Brick" Braggor 69688.
* Spell4 79006 and 79007 are tagged `W2183 - PE446 - Electrical Shock - Electric Beam ... Brick Braggor Fight` (and 47294 and 47330 to 47339 for the arena hazards), but all are `[DEPRECATE]` rows and the packet reports no Braggor ability from clips (only a long entrance). The Shock King's reported knockback has no row.
* Missing: a non-deprecated Spell4 row for the Shock King knockback or for any Braggor cast, and a packet-reported ability to match. The random arena pool is also unresolved.

## Deep Space Exploration (world 2188)
* Creature ids verified by name: Captain Rotfin 48703 (69267), Riptide the Stone Serpent 48704 (69299), Irontide the Venom Snake 49404 (69294).
* Missing: no Spell4 description names Rotfin, Riptide or Irontide, no W2188 tag exists, and the packet reports no cast (only that a wandering add near Rotfin interrupts with an area spell). The name-text anchors bind the creatures, not any spell.

## Fragment Zero (world 3180)
* Creature ids verified by name: Prototype Alpha 67526, Beta 67527, Delta 67528, Project "Matron" 69088 and 69664, Life-Overseer 67522 and 69635.
* Missing: no Spell4 row is tied to any of them (the only `Prototype`/`Overseer` descriptions are discovery buffs and Eldan Protector or Stormtalon rows). The packet gives only descriptions ("red jump", "shuriken", "spider") with no cast name or row.
