# Instance packet: the eight expeditions (2026-11-09)

Phase B packet per [INSTANCE_ENCOUNTER_VIDEO_GOAL_PROMPT.md](INSTANCE_ENCOUNTER_VIDEO_GOAL_PROMPT.md), same template and honesty rules as [INSTANCE_KELVORETH_PACKET_20261109.md](INSTANCE_KELVORETH_PACKET_20261109.md). Rows: the eight `expedition` lines of [content_retail_completeness_instances.csv](coverage/content_retail_completeness_instances.csv) (worlds 1232, 1319, 1627, 2149, 2183, 2188, 3180, 3404). No code, no git, no retail parity claim.

## Method, tiers and limits (read first)
* Tiers used: **Observed** = read from a retail-dated clip's subtitles (no frame was viewed for any expedition; all clips are 720p so cast bars are not legible anyway). **Correlated** = table name/text match, beta footage, or a single guide. **Correlated (weak)** = name-only. Nothing here is Mapped or Verified.
* "Single-source" = one clip or one commentator. The Maximus Grahamicus normal and veteran walkthroughs (all 2016-06-12) are one commentator, so they count as one source even when two clips agree. "Agreed" = two independent commentators.
* Evidence filter: `upload_date` non-empty and < 20181128. Clips are named by URL only (corpus ids were renumbered on 2026-11-09). "Subtitled" is the corpus `has_subs` flag; these are auto-captions, so names are often garbled (for example "Iron Tide Venom Snake" for Irontide the Venom Snake) and numbers can be mis-heard.
* Footage counts are title/description matches via `meta_fts`, then hand-pruned for incidental mentions (listed per section). Subtitled counts include thin ones (a few have under 130 cues).
* The scaffolds record only objective credit and routing (`PublicEventObjectiveCreditEntityScript`), phases and map hooks. None has boss casts, phases or add waves, except the Evil from the Ether combat scripts (section 8). So "Creature2 ids already in repo" below are credit filters, not mechanics.
* Spell4 ids are not per-dungeon (see the Kel Voreth correction); the Spell4 cast-time column is unreliable. Only a text anchor, an internal tag, or two-clip agreement binds a spell; name-only matches stay "Correlated (weak)". Almost no boss ability names are audible in these guides, so most sections have no spell candidates. That is a finding, not an omission.

## Summary table

| Expedition (world) | Matched clips (dated) | Post-launch, expedition-specific | Subtitled with usable text | Skeleton quality | Boss/spell binding | Verdict |
|---|---|---|---|---|---|---|
| Infestation (1232) | 18 | 14 | 5 | objective order, counts, 18 min gold timer; 2 sources partly agreed | none | objectives yes, boss mechanics missing input |
| Outpost M-13 (1319) | 16 | 10 | 3 (+3 beta) | objective order, wave and count; single-source | none | objectives yes, boss mechanics missing input |
| Rage Logic (1627) | 4 | 4 | 2 (one commentator) | objective order, counts; single-source | Spell4 43616 text anchor (driller hack) | thin: single commentator |
| Space Madness (2149) | 15 | 13 | 5 | objective order, hazard-meter rules, 30 min; 2 sources agreed on key rules | none | objectives yes |
| Gauntlet (2183) | 21 matched, 16 real | 16 | 10 flagged (9 with text) | objective order, 10/3/20 counts; 2 sources partly agreed | weak name-only | objectives yes, arena roster missing input |
| Deep Space Exploration (2188) | 14 matched, 13 real | 7 | 2 (one commentator) | objective order, counts; single-source | boss text anchors to Creature2 | objectives yes, boss mechanics missing input |
| Fragment Zero (3180) | 32 matched, 22 real | 22 | 9 with text | objective order, 30 min timer agreed by 2 clips | none | best-covered expedition for objectives |
| Evil from the Ether (3404) | 1 title match + 2 dev streams | 3 | 3 (dev commentary) | full run order twice (Prime 1), Katja phase, 2 dev runs | Spell4 81706 text anchor; 4 other ids name-only | best-sourced boss; Katja numbers unconfirmed |

Cross-cutting findings
* The only boss-level text anchor found in the whole set is **Spell4 81706 "Turned Ravenous"**, whose rows carry LocalizedText 690850/690851 "Katja Zarkhov has turned ravenous, becoming vicious and stronger." A dev stream (2017-03-14) shows Katja transforming. The other four Katja spell ids in the repo are name-only.
* Kal Scattergood's two "Gold Expeditions" guides (2016-02) state there are **seven veteran expeditions** with gold-medal achievements. That matches the seven pre-2017 rows here; Evil from the Ether is a 2017 Primal Matrix addition (dev stream 2017-01-23, partial caption: the three expeditions available with the scaling update are named as Gauntlet, Fragment Zero and this one). Observed, two commentators for the seven.
* Timer evidence is thin but consistent: Infestation 18 min, Space Madness 30 min, Fragment Zero 30 min (two clips). Gauntlet, M-13, Rage Logic, Deep Space show a timer but no readable total.

---

## 1. Infestation (world 1232, public event 95, 12 objectives)

### 1.1 Footage
18 dated matches (2014-03-18 to 2017-01-24): 3 are incidental (two Kaos Sverige contract mash-ups, 2015-10-08 and 10-09, one tradeskills video 2015-11-09), so 15 expedition-specific, 14 post-launch. 5 subtitled. Best clips:
* https://www.youtube.com/watch?v=3iDn5vuWiLo : Maximus Grahamicus, veteran walkthrough, 2016-06-12, 672 s, subtitled. Read in full.
* https://www.youtube.com/watch?v=Zuj7VAqp3XA : iBluairJgR, "Uncut" veteran gold run, 2015-10-26, 749 s, subtitled. Read in full (much off-topic chatter).
* https://www.youtube.com/watch?v=7SgNHFk8vGQ : official wildstar channel (German), 2014-07-06, 691 s, subtitled (German; the narrator says it is beta footage, so Correlated at best). Read 18 to 560 s.
* https://www.youtube.com/watch?v=A7cSGGukACg : Maximus, normal walkthrough, 2016-06-12, 361 s, subtitled. Not read.
* https://www.youtube.com/watch?v=RC-x3r7zRNA : Elijaa, veteran gold guide, 2015-02-20, 389 s, no subtitles. Not read.

### 1.2 Skeleton (Observed)
Objective order (veteran, Maximus 2016, single-source; the German beta clip agrees on the shape for the normal route):
1. Close three ship vents in the first room; a toxicity meter fills near vents and damages you, blocking the close action (Maximus, German beta: both).
2. Tag valuable cargo (six needed, more than six exist) and find the medical bay. Lights go dark and bulkheads rupture: shiphands are sucked toward vents shown as orange radar spots; saving every one is a gold condition (Maximus and iBluair agree on "do not let any shiphand fall"; the German beta clip calls it an optional task that gave a bronze-vs-silver difference).
3. Medical bay: three "strain" mobs spawn and morph into one large mob; a Lumbering Parasite appears when the room goes dark (iBluair) and is killed before moving on.
4. Kill more Lumbering Parasites and heal the Contaminated Shiphands (press T): four shiphands (Maximus "only four"; German beta "heal four ... and kill six of these large creatures"; agreed, but the second clip is beta). Healing an over-contaminated shiphand makes it explode (German beta only).
5. Final: Cyclopean Parasite. Splits into several blobs; a spin cast leaves damage on the ground; interrupt it (Maximus, single-source). Trash melee-mob interrupts are advised against ("your interrupt has a fairly long cooldown", Maximus).
Counts and timers: gold timer 18 min (iBluair: "they give you 18 minutes", and "13 minutes left" at about 5 min of clip, consistent); Maximus clears in 8 min, iBluair about 10 min. No health thresholds stated for any boss. No wave counts beyond the above.

### 1.3 Row binding
* Objectives (repo enum): 228 Close Ship Vents, 229 Kill Lumbering Parasites, 230 Find Medical Supplies, 231 Heal Contaminated Shiphand, 1005 Proceed Onto the Cargo Ship, 1006 Rescue the Shiphands, 1007 Seal Hull Breaches, 1438 Find the Medical Bay, 1500 Kill Cyclopean Parasite, 4698 Defeat the Attack on Medbay, 4699 Tag Valuable Cargo, 4703 Gold Medal Timer. Video agrees with the existence and order of vents, cargo, hull breach/shiphand rescue, medbay, heal, Cyclopean. Observed vs enum: Correlated.
* Creature2 already in repo: Ship Vent 22493, Medical Supplies 22489, Hull Breach 36844, Contaminated Shiphand 23837, Lumbering Parasite 22669, Cyclopean Parasite 26833, Lashing Fiend 69871 (objective 4698), Cargo Container 69877.
* Spell4 candidates: none. No ability name is spoken or legible; "spin" and "split" are descriptions only. Correlated (weak) cannot even be offered.

### 1.4 Timing
None measured. Only the 18 min timer statement and clear times (8 to 10 min).

### 1.5 Open inputs
Cyclopean Parasite and Lumbering Parasite cast list, split counts, spin interval, interrupt count; per-shiphand vent timing and count; the strain-mob count and morph threshold; gold-medal rules beyond "save every shiphand"; normal-versus-veteran differences (veteran adds objectives; which ones is not itemised); a second independent veteran guide with speech (the unsubtitled Elijaa and Bez Scemy clips would need subtitle-free viewing).

### Verdict
Infestation has an Observed objective order, counts (4 shiphands, 6 cargo, 3 vents) and an 18 min timer; boss mechanics are missing input and no Spell4 row can be bound.

---

## 2. Outpost M-13 (world 1319, public event 108, 19 objectives)

### 2.1 Footage
16 dated matches (2014-01-13 to 2018-11-14): 4 pre-launch beta (two beta instance videos 2014-01-13 and 2014-02-24, an Explorer's Journal 2014-02-13, a Let's Play 2014-05-08) and 2 incidental mentions (2014-06-11 achievements, 2017-07-24 leveling), leaving 10 post-launch expedition-specific runs. Subtitled usable: three Maximus clips; the beta clips are subtitled but pre-launch. Best:
* https://www.youtube.com/watch?v=7O7_ITabMfA : Maximus, veteran walkthrough, 2016-06-12, 620 s, subtitled. Read in full.
* https://www.youtube.com/watch?v=NYFEGpOgbjE : Maximus, normal walkthrough, 2016-06-12, 365 s, subtitled. Read 0 to 400 s.
* https://www.youtube.com/watch?v=SQuJeEN4jhY : Kando, beta instance, 2014-02-24, 1281 s, subtitled. Read in full; pre-launch, Correlated only.
* https://www.youtube.com/watch?v=pPHSLdoz2tc : Maximus, Esper playthrough, 2017-07-07, 898 s, subtitled. Not read.
* https://www.youtube.com/watch?v=u2cgFpamWUU : Elijaa, veteran gold walkthrough, 2015-02-15, 820 s, no subtitles. Not read.

### 2.2 Skeleton (Observed)
Veteran order (Maximus, single-source; beta clip agrees on talk Milo, Rorgh, Krause, search the mine, hive pods, Hive Queen):
1. Talk to Captain Milo.
2. Cargo hold: Novaburn Marauders come in waves from both sides and steal red crystals; aggro and pull them back, the crystal drops where they die. Gold needs no more than about three or four crystals lost ("more than three or four through the door could be a silver").
3. Boss "Ransacker" Rorgh (mini boss): has a cast to interrupt (Maximus: "he's going to cast that, we interrupt"). No cast name.
4. Go to the asteroid surface via the exit panel (cinematic skippable).
5. Collect 10 floating mining charges.
6. Find Foreman Krause (follow flares).
7. Collect 10 datachrons from dead M13 miner bodies; each opened body spawns a pack of mobs that chases you (more bodies exist than needed).
8. Gather outside the mine; a ship drops a second marauder wave (count not stated); then enter.
9. Defeat hive pods, infected miners and hive infectors (eggs are the quickest progress; they do not fight back) until the Hive Queen spawns. Queen is interruptible (Maximus, single-source). Beta clip: the queen is lured out with a stun available and a small number of adds.
10. Go to Milo's shuttle; gold reward. Normal route clear time 5 min (Maximus normal), veteran about 10 min.
Health thresholds: none stated. Timer total: not stated.

### 2.3 Row binding
* Objectives: 252 Defend the Accelerite Shards, 253 Kill Novaburn Marauders, 254 Kill Ransacker Rorgh, 256 Defeat Hive Pods, 257 Kill Hive Queen, 262 Go to the Asteroid Surface, 656 Collect Datachrons, 1439 Search the Mine, 1440 Find Foreman Krause, 1441 Talk to Captain Milo, 1442 Go to the Cargo Hold, 4696 Collect Floating Mining Charges, 4697 Kill the Novaburn Marauders, 4700 Kill the Novaburn Leader, 4701 Gather Outside the Space Mine, 4702 Gold Medal Timer, 4733 and 4734 Marauder Wave 1 and 2, 5134 Head to Milo's Shuttle. Video order matches the enum's phases (TalkToCaptainMilo ... HeadToMilosShuttle). Correlated.
* Creature2 already in repo: Captain Milo 41201, Foreman Krause 29468, Exit Panel 23470, Dead M13 Miner 29512, Rorgh 23455 ("Ransacker" Rorgh), Hive Queen 23513, cargo-hold marauders 23453, 23454, 69074, 69075, mine-cleanup rows 23536, 23514, 23500, 69080, 69079, 69077 (the 69xxx rows are the second set; role not verified).
* Name texts (checked with the table scan): 197767 = Creature2 23455 "Ransacker" Rorgh, 198236 = Creature2 23513 Hive Queen (and 191995 = 22669 Lumbering Parasite, 232734 = 26833 Cyclopean Parasite in section 1). These anchor names, not abilities.
* Spell4 candidates: none; no cast name audible.

### 2.4 Timing
None measured.

### 2.5 Open inputs
Rorgh and Hive Queen cast lists, health thresholds, adds; marauder wave sizes and times (waves 1, 2, drop-ship wave); crystal-loss rule exactness for gold; datachron/charge spawn lists; timer total; second independent veteran source.

### Verdict
M-13 has a clean Observed objective order and counts (10 charges, 10 datachrons, ~3 crystals) from one commentator; both bosses are interrupt-only descriptions, so boss slices stay missing input.

---

## 3. Rage Logic (world 1627, public events 213/214, 17 objectives)

### 3.1 Footage
Only 4 dated clips in the corpus (all post-launch, 2016-03-05 to 2016-07-01), 2 subtitled, both by one commentator. This is the thinnest set.
* https://www.youtube.com/watch?v=Gl80JVh8Fp4 : Maximus, veteran walkthrough, 2016-06-12, 817 s, subtitled. Read in full.
* https://www.youtube.com/watch?v=FTnnJb4gIVo : Maximus, normal walkthrough, 2016-06-12, 725 s, subtitled. Only the clear-time line read.
* https://www.youtube.com/watch?v=b7u_jhZeufk : BezScemy, 2016-07-01, 1319 s, no subtitles. Not viewed.
* https://www.youtube.com/watch?v=7mC_pC2SblQ : BezScemy, level 26, 2016-03-05, 1215 s, no subtitles. Not viewed.

### 3.2 Skeleton (Observed, single-source)
1. Choose a vehicle (rocket launcher, EMP/"microwave", heavy machine gun; all have a ram shield that gives speed and a shield and carries most of the damage).
2. Asteroid: destroy hovering Ragebots (marked on radar, shoot with the machine gun), place dynamite in three engines (press 4), ram 100 ragebots (only shield rams count; machine-gun kills do not) and destroy 50 ragebots (Maximus reads the objective text as "ram 100 / destroy 50").
3. Kill the overseers "simultaneously" (three overseers; solo they die in rapid succession with shield rams). Enter the factory.
4. Factory: destroy 20 ragebots and hack three Ragebot Drillers (press T; a hacked driller either becomes an ally or turns hostile; Maximus says a 20 s cooldown between hacks). Do not shoot drillers before hacking, hits interrupt the hack.
5. Wreck the factory: destroy factory pieces (piece count not clearly stated; one furnace hazard; a larger mob guards one piece and is interruptible; the broadcast transmitter piece needs "stay out of the beam").
6. Hack the Rage Logic signal relay (a pattern-memory minigame; a failure spawns bots before the portal opens).
7. Enter Axiom's control room via the portal; Axiom fires rockets into the air; clear time 11 min 43 s (veteran), 10 min 29 s (normal). Gold.
No health thresholds, no Axiom cast list. Timer total not stated.

### 3.3 Row binding
* Objectives: 781 Choose Vehicle, 783 Obliterate Ragebots Defending Asteroid, 784 Destroy Asteroid Engines, 786 Gain Access to Ragebot Factory, 787 Disable Ragebot Factory, 788 Restore Sanity to Infected Freebots, 789 Take Out Ragebots Defending Factory, 790 Enter Server Room via Teleporter, 791 Destroy Axiom, 1493 Obliterate Ragebot Scanners, 1529 Hack Rage Logic Signal Relay, 4688 Use Hoverbikes to Ram Ragebots, 4691 Destroy Broadcast Transmitter, 4704 Gold Medal Timer, 5107 Enter Axiom's Control Room.
* Creature2 in repo: vehicle rows 32249, 43809, 43810; asteroid thruster 32365; asteroid defender rows 69107, 69097, 32302, 69106, 69096, 32322, 69095, 69094, 32323, 44232, 43827, 32324. Axiom, the overseers and the driller are not in the repo yet.
* **New text anchor:** Spell4 43616 "Ragebot Driller Hack" carries LocalizedText 441132 "Hack $m(creature=32325) to turn them against the Ragebots." That binds the driller hack action to Creature2 32325 (not in repo; its name was not looked up here). The video's "press T to hack" agrees with the spell's existence. Tier: Correlated (text anchor, single video). The spell's raw cooldown column reads 2000 against the commentator's "20 seconds", an unreconciled mismatch; the cooldown column is unreliable, so neither is used.
* Other candidates (Correlated (weak)): Spell4 35942 "Axiom's Server Room" (name only, effect types 0x7/0x41, role not established).

### 3.4 Timing
None measured; clear times 10:29 and 11:43.

### 3.5 Open inputs
Axiom's cast list and phases; the large factory-guard mob; overseer simultaneity window; ram counts per phase (100/50 are the commentator's reading of the objective text and can be cross-checked against `PublicEventObjective` texts, not done); driller ally/hostile ratio; second independent commentator.

### Verdict
Rage Logic is footage-thin (one commentator, two clips) but has a full objective order; the single real binding is the driller hack text anchor. Axiom is missing input.

---

## 4. Space Madness (world 2149, public event 390, 26 objectives)

### 4.1 Footage
15 dated matches (2014-03-17 to 2017-11-13); 2 pre-launch, 13 post-launch; 5 subtitled. Best:
* https://www.youtube.com/watch?v=9bHnO9cLAJc : Kal Scattergood, "Gold Expeditions", 2016-02-16, 157 s, subtitled. Read in full.
* https://www.youtube.com/watch?v=SZ24eO43hos : iBluairJgR, "Uncut" veteran, 2015-10-31, 1076 s, subtitled. Read in full.
* https://www.youtube.com/watch?v=tOYw6J4leqw : TinyBlueGames, 2017-11-13, 733 s, subtitled. Read 400 to 480 s only.
* https://www.youtube.com/watch?v=7ieC9Iman2M : Bog Otter, 2014-06-11, 1220 s, subtitled. Not read.
* https://www.youtube.com/watch?v=XEB1TLO5KuE : Kaos Sverige, veteran gold, 2015-05-24, 1188 s, no subtitles. Not viewed.

### 4.2 Skeleton (Observed; Kal and iBluairJgR are independent, so items both state are agreed)
Order (Kal): talk to Captain Tero, talk to Major Lee Barmy, save panicked workers, collect crew datapads, access the observation deck computer (opens the next door), a hazard meter fills and each madness level lets you see an escaped experimental creature (three: Slinking Slank, Talking Rockmite, Party Down-Grazer); clean-air tanks reduce delusions. Rainbow Room; open the hazmat storage closet; waves of nightmare creatures; a vending machine comes alive and must be killed; get a hazmat suit; livestock hold: kill hallucinating livestock; avoid exploding Rowsdowers; give air helmets to two crew; engineering: clear the way to the air scrubber controls; the scrubbing process is a last wave encounter with a fire or electricity theme (random, Kal); engineering computer sends the all-clear and teleports you back to Tero.
Agreed (Kal and iBluair): hazard meter and critters visible only while hallucinating (three needed); a Rowsdower explosion hit costs gold; the vending machine is a fight; the air scrubbing ends with "air scrubbing complete, hallucinations fading".
iBluair adds (single-source): more mobs the madder you are; the gold timer is 30 minutes; he is unsure whether a minimum sanity level is needed to see all critters (rule unconfirmed).
Counts: 3 creatures, 2 helmets, a list of datapads unspecified. No health thresholds.

### 4.3 Row binding
* Objectives: 1579 Talk to Captain Tero, 1588 Talk to Major Lee Barmy, 1589 Access the Observation Deck Computer, 1590 Save Panicked Workers, 1591 Enter the Research Laboratory, 1592 Open Hazmat Closet, 1593 Kill Hallucinating Livestock, 1594 Give Air Helms to Workers, 1595 Activate Air Scrubber Controls, 1596 Access Crew Datapads, 1597 Defeat Hallucinations, 1602 Enter the Airlock, 1603 Send the All-Clear Signal, 1641 to 1643 Defeat Hallucinations 2 to 4, 1656 Equip a Hazmat Suit, 1662 Survive Your Nightmares, 2115 Survive the Air Scrubbing Process, 4705 Gold Medal Timer, 4708 Avoid Exploding Rowsdowers, 4709 to 4712 collect an experimental Slank, Rockmite, Party Down-Grazer and escaped creatures, 4736 Open Hazmat Storage Closet. Video agrees with the whole chain and the three-critter optional objective (Correlated).
* Creature2 in repo: Captain Tero 45900, Major Lee Barmy 45812, Observation Deck Computer 45972, Datapad 45993, Hazmat Control Panel 46092, Main Vent Lever 45861, Air Scrubber Controls 46437, Engineering Computer 45973, Hazmat Suit 45981, Hallucinating Venture Worker 45903 and 45904, Slinking Slank 69899, Talking Rockmite 69900, Party Down-Grazer 69901, nightmare rows 46483 and 46714 plus others.
* Spell4: none proposed. Note for the Evil from the Ether section: Spell4 46692, 46790 and 46797 sit next to rows tagged `W2149` (this world) but that adjacency is not a binding.

### 4.4 Timing
30 min gold timer (iBluair, single). No wave timings.

### 4.5 Open inputs
Hallucination spawn rules per sanity level; wave counts of the scrub fight and which theme sets exist (fire/electric, Kal); hazard-meter rate; Rowsdower count and trigger radius (Kal says "large Telegraphs"); vending machine abilities.

### Verdict
Space Madness has the most agreed rule set of the non-Fragment-Zero expeditions (objective chain, critters, Rowsdower fail, vending machine); wave and sanity numbers are missing input.

---

## 5. Gauntlet (world 2183, public event 446, 35 objectives)

### 5.1 Footage
21 title/description matches (2014-06-14 to 2018-11-01). 5 are other content (Skullcano Veteran Silver 2014-07-14, Skullcano Fire Gauntlet 2015-10-17, two Eldan Gauntlet imbuement clips 2014-07-18, Swordmaiden tutorial 2014-07-23), leaving 16 expedition runs, all post-launch. 10 are flagged subtitled (one, 2017-01-21, has about 100 characters of text).
* https://www.youtube.com/watch?v=vasW_FW_Fvw : Kal Scattergood, "Gold Expeditions", 2016-02-23, 143 s, subtitled. Read in full.
* https://www.youtube.com/watch?v=Z1I3zqwC_Kc : Maximus, veteran walkthrough, 2016-06-12, 743 s, subtitled. Read in full.
* https://www.youtube.com/watch?v=aJMOX5gtiOM : iBluairJgR, "Uncut" veteran, 2015-10-28, 1045 s, subtitled. Read 300 to 760 s.
* https://www.youtube.com/watch?v=Hm1bB3KeWAI : Maximus, normal walkthrough, 2016-06-12, 661 s, subtitled. Read 330 to 700 s.
* Not read: the three Maximus class playthroughs of 2017-07 (https://www.youtube.com/watch?v=nCJ0_qRseHA, https://www.youtube.com/watch?v=AvLLXHAruGQ, https://www.youtube.com/watch?v=Hkosub57mUM, 800 s, 1501 s, 993 s, subtitled) and https://www.youtube.com/watch?v=KHrtW_hTNB0 (2014-06-22, Esper, 1095 s, subtitled).

### 5.2 Skeleton (Observed)
Objective order (Kal and Maximus agree; both single-commentator for details):
1. Talk to Pilot Taboro, then step into the blue circle (cinematic; you are disabled/hazy briefly).
2. Laser room: find 10 golden skulls (more than 10 spawn, positions vary run to run) and activate 3 switches; golden skulls are collected across the whole instance, the last ones in the main-event stands (Kal, Maximus, iBluair: agreed that there are 10 and that they vary).
3. Fight waves of "Heathens"; swarm pit: survive the Splorg exploding mobs while collecting 20 Gauntlet score tokens (teddy bears) before they explode; the timer visibly speeds up detonations ("the more the timer runs out the faster these guys start blowing up", Maximus). Failing it (together with missing skulls) gave Maximus a bronze; Kal calls it the part most likely to be flubbed solo. Agreed by Kal and Maximus (20 tokens, hardest part).
4. Endorse a product (optional; any choice works, each opens a shortcut scene).
5. Faction friction arena and charnel chamber: random encounters from a pool (Kal: "variety of encounters"); brawling alley: rescue other contestants by killing everything.
6. Main event arena: waves of "heathens" then named fights; one encounter is the Shock King (Maximus normal: casts a knockback into the arena shield, interrupt it).
7. Final: Brick Braggor after a long dramatic entrance (Kal: "pretty awesome but skippable cutscene"; Maximus: "really long dramatic entrance"); then talk to Judge Kain or Agent Lex (faction hologram).
Timer: exists and ticks during cinematics (iBluair: "the timer ticks down during that"); total not stated. Dance-off achievement is a side activity (Kal, iBluair).

### 5.3 Row binding
* Objectives: 1818 Get Into Airlock, 1819 Find Out What Happened to You, 1821 Talk to Pilot Taboro, 1835 Kill the Opposing Faction's Team, 1837 Activate Switches, 1842 Gather at the Swarm Pit, 1854 Survive the First Arena, 1857 Enter the Faction Friction Arena, 1858 Enter the Main Event Arena, 1859 Enter the Charnel Chamber, 1864 Kill the Rockstar Yeti, 1865 Kill the Championator, 1866 Kill the Voodoo-kin, 1869 Defeat the Goon Squad, 1870 Defeat the Handler and His Pets, 1871 Defeat Slice and Dice, 1872 Defeat Pyro and Maniac, 1873 Defeat Showtime, 1874 Defeat the Shock King, 1875 Survive the Main Event, 1914 and 1915 Talk to Judge Kain / Agent Lex, 1951 Save Gauntlet Contestants, 1957 Collect Golden Skulls, 1967 Survive the Splorg, 1974 Collect Gauntlet Score Tokens, 1977 Enter the Chamber of Choices, 2001 Endorse a Product, 4671 Defeat Brick Braggor. The named-boss objectives are consistent with a random arena pool ("variety of encounters", Kal). Video names only the Shock King and Brick Braggor.
* Creature2 in repo: Pilot Taboro 48580, Judge Kain 48945, Agent Lex 48950; Championator 48529 and 69255; Professor Doctor Voodoo / Morticianatrix 48511, 48499, 69258, 69257; Rockstar Yeti 48491, 69254; Zoragg and Fist 48588, 48589, 48591, 69309, 69310, 69311, 52112; Handler and pets 48516, 48518, 48519, 69302 to 69304; Slice and Dice 48557, 48558, 69306, 69307; Pyro and Maniac 48493, 48510, 69300, 69301; Showtime 48579, 69308; Shock King 48554, 69305; Brick Braggor 69688; Goon Squad 48666, 48667, 48669, 48670, 69282 to 69285; first-arena frenzied rows and product endorsement rows 49218, 49429, 49486, 49536, 49573.
* Spell4 candidates (Correlated (weak), name-only): Splorg Detonation 42636 and 42731, Splorg Pop 33582 and 33963 (the exploding Splorg mobs); Laser Grid 25480 (the laser room). No text anchor tried or found; the Shock King's knockback cast has no audible name.

### 5.4 Timing
None measured.

### 5.5 Open inputs
Which arena encounters appear per run and in what order; the Shock King, Brick Braggor and other bosses' cast lists; swarm-pit wave and token timings (the "faster as timer runs" curve); total timer; golden-skull spawn pool; whether the objective ids 1864 to 1874 correspond to the same pool in both versions.

### Verdict
Gauntlet's objective order and counts (10 skulls, 3 switches, 20 tokens) are agreed by two commentators; the random arena pool and every boss cast are missing input.

---

## 6. Deep Space Exploration (world 2188, public event 447, 28 objectives)

### 6.1 Footage
14 dated matches; 1 is incidental (a closed-beta Let's Play, 2014-05-11), so 13 expedition runs, 6 of them pre-launch beta (2014-03-25 to 2014-05-22) and 7 post-launch; 2 subtitled post-launch, both Maximus.
* https://www.youtube.com/watch?v=2UgIv7TrNeM : Maximus, veteran, 2016-06-12, 1285 s, subtitled. Read in full.
* https://www.youtube.com/watch?v=diqMhhqKxmM : Maximus, normal, 2016-06-12, 1172 s, subtitled. Grep only (boss names, 18 min clear).
* https://www.youtube.com/watch?v=Vr070D9SYG8 : Maximus, gold veteran, 2015-12-24, 926 s, no subtitles. Not viewed.
* https://www.youtube.com/watch?v=jivxnj7UcLQ : Kaos Sverige, veteran gold, 2015-05-24, 2073 s, no subtitles. Not viewed.
* https://www.youtube.com/watch?v=Ml-EyuE4NNY : InTheLittleWood, 2014-05-19, 1131 s, subtitled, pre-launch beta. Not read.

### 6.2 Skeleton (Observed, single-source)
1. Talk to six crew members on the bridge (radar dots), then the captain; an armada assault starts and you are caged. Taunt the sleeping guard to be released (no hitting the cage).
2. Disable specimen containment cells: terminals; access codes are shown in a lore book and on the objective (codes include 3596 for the Protostar specimens; the client text 456588 contains "ROWSDOWER: 441 / HUMANOIDS: 975 / PROTOSTAR: 3596"); releasing or blowing up the specimens both complete it.
3. Kill Steelfin forces, using Ikthian guns found on the ground (optional objective needs 5 uses; they shrink named mobs; more than five can be used). Rescue the crew; kill Irontide the Venom Snake; collect engineer clamp parts, throw them into the three engines to clog the Steel Serpent, and assemble the robot at the table.
4. Captain Rotfin (named boss; a wandering add interrupts with an area spell and should be killed first). Access panel (colour-sequence minigame); escape hatch cinematic.
5. Zero-g maintenance drone section: repair several panels (radar dots; the count is not stated), restore life support; air lock; then freeze the clawing "dinosaur" mobs with T until the main-floor ones are done (count not stated); defense control switch: laser-grid jump puzzle to deactivate; turn off valves.
6. Final: kill Riptide the Stone Serpent in the central room; cinematic; run to the end circle for gold.
Observed clear time: 18 min for normal (Maximus). No health thresholds; timer total not stated.

### 6.3 Row binding
* Objectives: 1843 Gather at the Starhelm Command Deck, 1844 Talk to Tyrania, 1845 Disable Specimen Containment Cells, 1846 Kill Steelfin Forces, 1847 Rescue the Crew, 1848 Collect Engineer Clamp's Parts, 1849 Sabotage the Cortex, 1850 Get To..., 1851 Restore..., 1852 Kill 48704, 1853 Activate..., 1965/1966 access panels, 1968 Talk to Crew Members, 1979 Escape the Specimen Containment Cell, 1988 Kill 49404, 1998 Kill 48703, 1999 Infiltrate Rotfin's Command Quarters, 2000 Access..., 2100 Use..., 2108 Freeze..., 2112 Clog the Steel Serpent's Engines, 2118 Wait for the Galactic Observer to Stabilize, 2156 Assemble Engineer Clamp, 2435 Gather at the Starlab, 4683 Gather at the Defense Control Switch, 4687 Use Ikthian Guns on Steelfin Forces. The scaffold's enum names three kill objectives by Creature2 id: 1852 = 48704, 1988 = 49404, 1998 = 48703.
* **Boss text anchors (Correlated, Creature2 name text):** Captain Rotfin text 442535 = Creature2 48703 (and 643590 = 69267); Riptide the Stone Serpent text 442540 = Creature2 48704 (and 643839 = 69299); Irontide the Venom Snake text 445746 = Creature2 49404 (and 643831 = 69294). So the enum's `Kill48703/48704/49404` are Rotfin, Riptide, Irontide; this agrees with the video's three names (the video spells them Rotten, Riptide Stone Serpent, Iron Tide Venom Snake via auto-captions, so two independent subtitle sources are consistent with the table names; name-level only).
* Creature2 already in repo: Captain Tyrania 48900, crew rows 49474 to 49480, Steel Serpent Mainframe Cortex 48996, containment terminals 48991 and 49800 to 49802, Steelfin forces 49408, 48740, 48702, 49406, 48762, and others in the access/talk scripts. The three boss rows have no script yet.
* Spell4: none proposed. The Ikthian gun mechanic (shrink) could correspond to existing Ikthian rows but nothing ties them (not searched beyond a name list).

### 6.4 Timing
None measured; 18 min normal clear.

### 6.5 Open inputs
Boss cast lists and thresholds (Rotfin's add, Riptide, Irontide); the shrink gun effect row; zero-g drone section parameters; wave counts for the "armada" assault; second independent commentator for the veteran route.

### Verdict
Deep Space has an Observed objective order and verified-by-text boss identities (Rotfin 48703, Riptide 48704, Irontide 49404); boss mechanics are missing input and no spell row can be bound.

---

## 7. Fragment Zero (world 3180, public event 680, 38 objectives)

### 7.1 Footage
32 dated matches; 10 are incidental Skeech mentions (housing decor, Northern Wilds, lore), leaving 22 expedition runs, all post-launch (2014-12-17 to 2018-11-13). The best-covered expedition, with 9 clips of usable subtitled text.
* https://www.youtube.com/watch?v=6F1Nx8l9xhU : Maximus, veteran, 2016-06-12, 1065 s, subtitled. Read in full.
* https://www.youtube.com/watch?v=xdYLT9fRnmo : Readycheck, PTR veteran with two players, 2014-12-17, 1455 s, subtitled. Read in full (PTR build, Correlated).
* https://www.youtube.com/watch?v=0-eDez5J77E : LamixFace, 3-player veteran run, 2016-10-26, 812 s, subtitled. Read 80 to 700 s.
* https://www.youtube.com/watch?v=B1JaTpqnSas : WildStarOnline official stream, 2015-02-25, 2728 s, subtitled. Grep only.
* https://www.youtube.com/watch?v=bHhX3veSBsM : Maximus, normal, 2016-06-12, 820 s, subtitled. Not read.

### 7.2 Skeleton (Observed)
Objective chain (Maximus 6F1Nx8l9xhU, with PTR and LamixFace clips agreeing where marked):
1. Search for freight supervisor Lola's missing crew; follow the "friendly" Skeech into an ambush; a Skeech wave then a boss-like Skeech arrives.
2. Locate the ship's black box: it spawns at a random location (Maximus: "randomly spawned"; Readycheck: "not in the same location as my first run"; **agreed by two clips**).
3. Incubation complex: smash Xenobite (Skeech) eggs (optional) and defeat Prototype Alpha, Beta and Delta (one in the first room, one in the middle, one up a ramp; **agreed by Maximus and Readycheck, three prototypes**). Search for crewman Jo's corpse; return to the airlock.
4. Collect cargo crates and kill Skeech: progress is shown as a percentage; "looters" carry crate parts and a kill-percent bar (Maximus: about 60% is enough to proceed, 100% for the optional) (Maximus and Readycheck both describe cargo crates + kill bar).
5. Deactivate Fragment Zero's automated defenses (two panels plus a third near a corpse; Project Matron is killed here); search Syrus's corpse; return to the airlock.
6. Search for Captain Hugo; kill the Life Overseer (Maximus uses an interrupt; LamixFace describes the final boss throwing shuriken-like projectiles and a spider attack). LamixFace also describes a staff-wielding mini boss in the second wing with an interrupt-stun and a "red jump" (not identified as Matron or a prototype). Free Hugo.
7. Hugo's force-field escape: stay inside the bubble against waves of Skeech; it malfunctions partway and you fight out; reach the ship and talk to Hugo.
Timers: gold timer is about 30 minutes: Maximus "in under half an hour"; LamixFace shows "twenty-eight-ish minutes left" about 109 s into the run; **agreed by two clips**. No health thresholds stated for any boss.

### 7.3 Row binding
* Objectives: 4397 Search for Missing Crew, 4416 Eliminate Skeech, 4417 Defeat Project Matron, 4418 Return to the Airlock of the Biomatics Chamber, 4420 Search Inside the Biomatics Chamber for Syrus, 4421 Search Inside the Incubation Complex for Jo, 4422 Follow the Friendly Skeech, 4423 Defeat Prototype Alpha, 4424 Search Jo Inside the Incubation Complex, 4425 Survive the Skeech Ambush, 4426 Continue the Search for the Missing Crew, 4427 Return to the Airlock of the Incubation Complex, 4429 Continue the Search ... Life Overseer, 4430 Continue the Search for Hugo, 4431 Collect Cargo Crate, 4432 Search Crewmate Syrus's Corpse, 4443 Search for Hugo, 4444 Defeat the Life Overseer, 4445 Locate Hugo, 4446 Return to the Airlock of the Life Overseer's Creche, 4447 Stay Close to Hugo, 4448 Discover the Two Lost Recordings, 4449 Defeat Prototype Beta, 4450 Defeat Prototype Delta, 4454 and 4455 Ohmna's and Lucent's hidden recordings, 4584 to 4586 Eliminate the Hordes of Skeech (areas 1 to 3), 4632 Smash Xenobite Eggs, 4635 Deactivate Automated Defences, 4641/4642/4655 Hugo talk and wait objectives, 4684 Collect Cargo Crate 2, 4686 Kill Experimental Skeech, 4690 Gold Medal Timer, 4693 Locate the Ship's Black Box. Video order is consistent with the scaffold's phase list (ambush, prototypes, Jo, Syrus/Matron, Life Overseer, Hugo).
* Creature2 in repo (credit filters): Prototype Alpha 67526 and 69672, Beta 67527 and 69673, Delta 67528 and 69674, Project Matron 69088 and 69664, Life-Overseer 67522 and 69635, Jo's corpse 67966, Syrus's corpse 67931, black-box row 67965, Facility Defense Control Panel 68856, Hugo 68886 and 68854, locate-Hugo projection 67961, Ohmna's and Lucent's recordings 67967 and 67968, Skeech rows 68811, 68812, 67516, 67518, 67519, 67520.
* Spell4 candidates: none. The captions name no casts; "red jump", "shuriken" and "spider" are descriptions from a single commentator.

### 7.4 Timing
None measured. No clear time is stated by the read clips.

### 7.5 Open inputs
Boss cast lists and interrupt counts (Prototypes, Matron, Life Overseer); kill-bar percentages (60%/100% are one commentator's); Hugo bubble duration and malfunction time; Skeech wave sizes; black-box spawn pool.

### Verdict
Fragment Zero has the strongest Observed objective chain (two or three clips, 30 min timer agreed, 3 prototypes agreed); boss casts are missing input and nothing binds to Spell4.

---

## 8. Evil from the Ether (world 3404, public event 781, 32 objectives)

### 8.1 Footage
Only one title match, because this expedition arrived with the 2017 Primal Matrix update, but three dev-stream sources contain it. All are Prime 1 (level 50 version); no normal-mode footage exists in the corpus.
* https://www.youtube.com/watch?v=AY3W8n8Zh3c : WildStarOnline official stream "Coldblood & Evil from the Ether", 2017-01-23, 3246 s, subtitled. The full run is at about 180 to 1260 s with two developers commenting on a PTR run (apparently a single player). Read in full.
* https://www.youtube.com/watch?v=pRVAcqonX08 : "Power of the Primal Matrix Overview", 2017-03-14, 6601 s, subtitled. A second, independent run (a group of several players) at about 2790 to 3640 s. Read in full.
* https://www.youtube.com/watch?v=SlE5SZxaqNI : "Power of the Primal Matrix Preview Livestream", 2017-01-07, 3132 s, subtitled. Developer description only, at about 2371 to 2530 s; no run. Read that range.
* https://www.youtube.com/watch?v=18Z3Mn7AawQ : "Wildstar | Prime", 2017-04-16, 633 s: a one-line mention only.

### 8.2 Skeleton (Observed)
Run order from the 2017-01-23 stream (seconds are caption times, offset a few seconds from the action), single-run:
* 300: talk to Captain Weir (quest is available on normal at the early 20s; this Prime 1 run is the level 50 scaling). 430 to 540: ravenous-held area, a Reaper mob ("not a mini boss, just one of the big guys"), generators to power on, locked sealed bulkheads, crew data logs along the way.
* 562 to 640: Security Chief Kondovich, the expedition's mini boss: "a big giant whirlwind", a repeated "security sweep" that the player evades by moving far away, and "really solid interrupts". No thresholds.
* 701 to 780: laser hall (jump puzzle), stream computer, holdout crew on vital serum; more generators; the captain calls over comms (781 to 822).
* 822 to 903: first Etheric Organism cinematic; they are glowing balls of light, not ravenous. 903: etheric portal rifts, with an optional challenge to avoid all of them ("gold is preserved" later). 955: restore the teleporter function.
* 1091 to 1157: Katja Zarkhov floats suspended through an etheric portal; the group activates the self-destruct ("enabl[e] the self[-destruct]") and must fight her "while the ship is threatening to explode"; "the ship's blowing up" at about 1154; schematics for the drive drop (1241: "you got the schematics for the drive") and Weir ends the event. Dev says the expedition takes about 15 to 20 minutes.
Run order from the 2017-03-14 stream (independent run, agrees): ravenous area and dark laser room (~3000), etheric organisms after turning on the teleporter (3283), the glowing etheric drive room with organisms to kill (3389 to 3540), Katja appears (3559 to 3570), "she's about to wolf out ... she's gone ravenous now ... didn't have her daily dose of vitalis" (3581 to 3590), fight ends by about 3592 to 3600. **Agreed (two runs): Kondovich mid-run, organisms after the teleporter, Katja last, schematics on kill.** The ravenous transition is **single-source** (Overview only; the livestream does not show it). The livestream's text note about Katja's motivation and her spoken lines ("the one is coming") is also single-source.
Timing spread for the Katja fight from the two runs (Observed, caption time, ±3 s): Overview first sight 3559 to "done" 3592 = about 33 s, with the transition about 22 to 28 s after first sight; livestream first sight 1091 to the ship blowing up 1154 = about 63 s (PTR, apparently one player). These are different group sizes and builds, so no single number is a fact.

### 8.3 Row binding (and the origin of the Katja spell ids)
Creature2 already in repo: Captain Weir 70999, Katja Zarkhov 71037 and 71039 (names from texts 687239 and 687277; per CURRENT_STATUS target group 14051), floating Katja 71832, Drive Schematics 71821 and 75222, Security Chief Kondovich 71287 and 71288 (names from texts 691217 and 691222), Etheric Organism rows 71621, 71633, 71628, 71634, 71014, Tethered Organism 71133, Etheric Portals 71221 and 71132, crew log 71234, doors 71266, medbay rows 71283, 71322, 71323, 71371, 71374, 71375, 71631, controls 71228. Reaper and Kondovich scripts bind by script name, not creature id.
Objective text anchors (real, from `PublicEventObjective`): 4926 text 689924 "Kill the Ether-Charged Ravenous", 4939 texts 690453 and 690454 (Kondovich), 4942 text 690478 "Don't get hit by the exploding etheric portals" (matches the observed portal-rift challenge at 903; the scaffold marks objective 4942 as an unresolved exploding-portal item). 4924/4925 order (self-destruct, then Katja) agrees with the observed order.

**Katja spell ids in `KatjaZarkhovEntityScript.cs`, where they could have come from.** Git: the script was first written by the upstream author (Rawaho, commit 3c91a94d, 2024-12-01, "Added basic scripts for Ravenous Reaper, Security Chief Kondovich, Katja Zarkhov"), before the restoration effort; the message and code give no source for the ids. Later commits by BankaiNoJutsu only ported and reorganised (78974181 2026-05-14, 0526eb7e 2026-05-27, 20faaef2, 7d24dc50). What the tables say:
| Id | Spell4 name | Name unique in Spell4? | Anchor | Tier |
|---|---|---|---|---|
| 56037 | Clawed Fury (raw cd 3600) | yes, one row | none (base 36032; its only text is the spell name 490821) | Correlated (weak), name-only |
| 56358 | Slicing Wind (cd 2800) | yes | none (base 36084; name text only) | Correlated (weak) |
| 56390 | Pouncing Slice (cd 1500) | yes | none (base 36092; name text only) | Correlated (weak) |
| 81706 | Turned Ravenous (cd 0) | yes | **Spell4 text columns 690850 and 690851 "Katja Zarkhov has turned ravenous, becoming vicious and stronger."** and base 57204 name text 690849 "Turned Ravenous" | **Correlated (text anchor)**; the 2017-03-14 run shows the ravenous transition (single clip) |
| 82855 | Ravenous Burst (cd 500) | yes | none (base 58229; name text 718733 only) | Correlated (weak) |
| 2605, 2607 (auto attacks) | Slice, Dice | no | none | unverified |
The ids sit in unrelated neighbours (56037 among Beam and Bot Ability Strobe rows; 56358 and 56390 among Blitz, Strafing Run, Unsteady Miasma; 81706 among Homing Missile, Death Rattle; 82855 among Inferno, Hoverboard). So the likeliest sources are (a) the upstream author's own retail captures, which are unrecoverable here, or (b) name-picking from Spell4 for a ravenous/clawed creature. Option (b) fits the unique names and the "Turned Ravenous" text anchor but cannot explain the 8 s opener, 20 s repeats, 10 s and 7 s offsets or the 55% enrage threshold, none of which appear in any table. **No clip agrees or disagrees with those numbers**: neither stream states a threshold, cadence or cast order, and the raw Spell4 cooldown column (3600/2800/1500 vs the script's 20 s) is unreliable. The video agrees only with the shape "two-stage fight, second stage ravenous".
Other Evil from the Ether scripts use Kondovich: Focused Assault 46790 and Crushing Rush 46797; Reaper: Crushing Flurry 46692 and Focused Assault 46790; auto attacks Clobber 32920 and Smash 32921. All are name-only (their Spell4Base rows 30762, 30767, 30668 carry only the name texts 441109, 441276, 440422). They live next to `W2149` (Space Madness) tagged rows, so these are plausibly shared spells (adjacency is not a binding either way). The livestream shows Kondovich using "a big giant whirlwind" and a "security sweep" plus an interruptible cast; neither maps to Crushing Rush or Focused Assault by name. Text 254254 "Continuing security sweep." is a Creature2ActionText line, not a spell anchor.

### 8.4 Timing
Kondovich fight about 60 s on the livestream's Prime 1 run (caption 597 to 660). Katja fight 33 to 63 s as above. Whole run about 15 min (dev statement "15 20 minutes is that nice sweet spot"). No telegraph/health-bar event measurements (no frames viewed).

### 8.5 Open inputs
Any retail packet capture or sniff of Katja, Kondovich and the Reaper (the only possible source of cast order and cadence); Katja's health threshold for the ravenous stage (the 55% is unconfirmed); the self-destruct countdown length; normal-mode footage (none exists); which of 71037/71039 is Prime versus normal; Kondovich "whirlwind"/sweep Spell4 row; a text-anchor search of the portal-rift and organism spell rows (Etheric Burst, Etheric Beam, Etheric Energy Beam: Spell4 81986, 81640, 80503 exist by name, no bind attempted).

### Verdict
Evil from the Ether has the best-sourced boss narrative (two independent dev-stream runs) and one genuine spell text anchor (81706 Turned Ravenous = Katja), but no clip supports or contradicts the script's cadence or 55% threshold, so those stay unverified placeholders.

---

## What I did not verify
* No frame of any clip was viewed; every claim is subtitle text. Auto-caption errors may have altered names and numbers.
* Counts are keyword matches by title/description. A clip that never names the expedition in its title or description is missed; a few may exist. The subtitle search for Evil from the Ether and Katja found no additional clips.
* The Maximus normal, German beta and class-playthrough clips were skimmed or not read; not-read clips are marked.
* Spell4 cooldowns, cast times and effect types were read as raw columns only; none is used as evidence. No Creature2-to-spell table exists, so no boss binding goes beyond the text anchors listed.
* No script, test or other file was touched. Git was not used except to read history for the Katja script's origin.
