# Instance packet: Skullcano (world 1263, PE 148) (2026-11-09)

Phase B packet per [INSTANCE_ENCOUNTER_VIDEO_GOAL_PROMPT.md](INSTANCE_ENCOUNTER_VIDEO_GOAL_PROMPT.md). Evidence tier: Observed (subtitle reads by an agent; no frame viewed for this dungeon) and Correlated (client text/table scans run with the helper scripts). Agent-written, not re-derived by the lead and not re-viewed. No code, no retail parity claim. Corpus ids changed on 2026-11-09, so every clip is given by URL.

Scope of this repo scaffold: `Source/NexusForever.Script.Instance/Dungeon/Skullcano` (kill credit and objective routing only; no boss ability or phase logic). Five fights: Stew-Shaman Tugga, Thunderfoot, Bosun Octog, Quartermaster Gruh'ar (optional miniboss, objective 440), Mordechai Redmoon.

## 1. Footage
69 dated clips before 2018-11-28 (survey count), about 32 with subtitles, range 2014-03-12 to 2017-09-08. Beta before 2014-06-02 is Correlated at best. Clips actually read for this packet (all 1280x720-class; cast bars are not legible, so only speech/subtitles were used):

| Role | Date | URL | Length | Subs | Notes |
|---|---|---|---|---|---|
| Veteran silver guide (Vhalor Gaming) | 2014-06-23 | https://www.youtube.com/watch?v=Om4KvjGMxeo | 2653 s | yes | Best source. All five fights, optional objectives. One commentator = single-source. |
| 5-minute overview (GameOverManu) | 2015-04-28 | https://www.youtube.com/watch?v=5MPHtAxjsi8 | 282 s | yes (garbled auto-caption) | Independent second commentator, all five fights, brief. |
| Dev Plays Skullcano (official WildStarOnline channel, game director and community manager) | 2016-09-16 | https://www.youtube.com/watch?v=y7O-eCouQIc | 3207 s | yes | Normal mode, chaotic group; developer one-line summaries for Tugga, Thunderfoot, Octog, Mordechai. |
| Mordechai boss kill explained (ZybakTV) | 2014-03-31 | https://www.youtube.com/watch?v=9hILCb0H1s8 | 235 s | yes | Veteran, BETA (pre-launch); Correlated. |
| Boss No.4 Quartermaster Gruh'ar | 2014-09-29 | https://www.youtube.com/watch?v=bkVix8UNYeE | 61 s | yes | Kneecap/Dead Eye chatter only. |
| Veteran dungeon silver run with group calls | 2014-07-14 | https://www.youtube.com/watch?v=8L6jCcI2IVw | 2380 s | yes | Group calls only; confirms interrupts on the monkey, hook shot, fatal shot. Not mined for numbers. |
| Prime 10 Skullcano | 2017-07-16 | https://www.youtube.com/watch?v=uU2DkevB5dA | 1264 s | yes (garbled) | Prime difficulty, later than the other clips; used only for the Thunderfoot enrage remark. |
Other clips matched but not read: silver/gold runs 2014-06 to 2015-09 (e.g. `Q5fPKxzm-Go`, `NdoyKy578r0`, `KEW9Dh2aXBQ`, `jemnLe8f_N0`, `Q9fNxG9IK1Q`, `hE3tz_XL0q4`), per-boss no-subtitle clips 2014-06-05 and 2015-10-17, `NFheH5tGrCc` (boss guide, no subtitles). Tier mix (normal vs veteran vs Prime) is mostly veteran; normal appears only in the 2016 dev clip.

## 2. Skeleton (Observed; "A" = agreed by at least two independent clips/sources, "S" = single-source)
Independence note: the 2014-06-23 guide is the only deep source; the 5-minute overview and the dev clip are short, so agreement is mostly on ability names, not numbers.

**Stew-Shaman Tugga** (opening boss; interrupt-driven)
* A (guide, 5-min, dev): meat/food items appear around the cauldron/floor and he eats them; eating heals and strengthens him; players should remove the food (the guide says leaving it on the floor keeps him from the next phase; the dev says collect/destroy it). Client text anchor: achievement "Stew-Shaman Tugga's Diet" = defeat him on Prime "without allowing him to eat any meat from his stew" (text 300743/300839; PublicEventObjective 941, Achievement 2234). Tier: Correlated.
* S (guide): phase 1 has three abilities: a totem that tethers a player and fires cross-shaped telegraphs, Molten Rain (many circular telegraphs), and Into the Stew (one player thrown into the pot, button-hold minigame to escape); interrupt as much as possible. The 5-minute overview says "four abilities" and "three interrupt" (garbled) and a second phase. Phase 2 (guide): he walks to food on the floor and has no interrupt armor, interrupt one at a time. No health threshold is stated by any clip.

**Thunderfoot** (ground-pound ape; jump mechanic)
* A (guide, 5-min, and the 2014-06-12 all-bosses clip https://www.youtube.com/watch?v=iJKmPKHfx8M, 809 s, subtitles, partly unintelligible): big mushrooms on the floor stun him if one of his telegraph attacks hits one; the tank should position him so ground-pound telegraphs hit mushrooms; save some for late. The guide also says a ground pound ("Thunder slam", name uncertain) requires everyone to jump even outside the telegraph, and Pulverize (large circle on a player, roll out and jump) follows an enrage visual. Prime 10 clip: mushrooms should only be used after two enrage stacks, each stack about 35% damage (garbled, S).
* Client text: "Thunderfoot's Rage increases all Damage Dealt by (value) per stack" (text 292911, referenced from Spell4 36324 "Enraging Fury", column 62). Challenge "Jump... Jump Around" = defeat him without anyone hit by Seismic Tremor (text 303563/300757, PublicEventObjective 942, Achievement 2236). Tier: Correlated. The jump-over damage pulse is thus probably "Seismic Tremor" by the client's own text.
* Internal tag: Spell4 63355 and 69343 `[SK] Thunderfoot - Challenge - Failure Signal`.

**Bosun Octog** (hook shot, ink, monkey)
* A (guide, 5-min): large cone telegraph and a hook shot that grabs a player, stuns and slams (interrupt the boss or CC-break the grabbed player); his monkey ("Ruki", spelling uncertain) drops in, the tank picks it up, interrupting the monkey makes it fly away and it drops mine-like bombs; ink phase 2 to 3 times: a random player drops ink (go to the edge), octopus/squid adds chase players, one player at a time is blinded and gets repeated telegraphs. Guide: boss aggro is cleared after each ink phase (S).
* Bomb hit = stacking debuff: the guide guesses 5% more damage per stack (S, "could be wrong"). Client text: achievement "Defeat Bosun Octog on Prime difficulty when he has at least 10 stacks of Broken Armor" (text 300749/300840, Achievement 2235). So the debuff is called Broken Armor by the client; whether it is on the boss or on players is not established by the text alone (the achievement wording says "he has"). Tier: Correlated; the 5% figure stays single-source.
* Internal tag: Spell4 52790 `[Dungeon Boss] Bosun Octog - Ink - Two Targets`. Client text 300637 "Bosun Octog signals the Redmoon Terror for backup." (unreferenced yell/message).

**Quartermaster Gruh'ar** (optional miniboss, objective 440)
* A (guide, 5-min, a 2014-09-29 clip): Kneecap on a player, then that player is the target of repeated sniper "Dead Eye" shots (clip: three in a row, then a fourth), strafe sideways to avoid; three ground AoEs and a fourth that knocks down; then knockback plus disarm and "Fatal Shot" which must be interrupted or it one-shots a player. Guide S: players who get their weapon back late wipe; interrupt armor may stop the disarm (untested by the guide).

**Mordechai Redmoon** (final boss, terraformer lasers)
* A (guide, 2014-03-31 beta clip, 5-min, dev): large rotating "big" lasers that one-shot, small rotating lasers to jump (plus underground lasers in later phases, guide), circular lava/gold pools that stun and snare, direction of rotation unpredictable (guide). The boss lands for a damage window; basic attacks are forward/back telegraphs, so stand to the side; ground turrets shoot and the terraformer overload blinds; turn the boss to face the walls/centre to avoid blind. Client text: challenge "without anyone being blinded while blinding Mordechai each time the terraformer overloads" (text 300762/300843, Achievement 2237, plus LocalizedEnum 1147). Tier: Correlated. Overload = terraformer blind confirmed from three independent sources.
* S (guide, 2014-06-23): "Big Bang" is a jump-to-player stun that must be interrupted; large laser phase returns at about 33% health, and a very large circular telegraph near 10% health that is interrupted. The 2014-03-31 clip (beta) says he is "interrupt or die" at a mechanic but gives no percentages. Both numbers are single-source, one commentator, estimated by him ("I think").
* The clips agree that laser phases alternate with a damage window (the beta clip mentions the lasers reversing direction in the next phase); the number of laser phases is not agreed.

## 3. Row binding (Correlated unless stated; nothing here is Mapped)
Creature2 ids already in the repo scripts (normal and veteran order not stated in the code comments; do not assume which is which): Stew-Shaman Tugga 24493 / 24898 (objective 321, TargetGroup 2599); Thunderfoot 24475 / 24893 (322, TG 2600); Bosun Octog 24486 / 24894 (323, TG 2601); Quartermaster Gruh'ar 24490 / 24896 (440, TG 2869); Mordechai Redmoon 24489 / 24895 (324, TG 2602, script credit). Objective enum in `PublicEventObjective.cs` matches the optionals named in the guide: Free Captured Lopp 335 (guide: 8), Mine Gold-Infused Lava Cores 338 (guide: 10 gold), Rid Skullcano of Marauders 334 (guide: kill 40), Free the Redmoon Prisoners 336 (guide: 15), Hack Missile Consoles 365 (guide: 3 consoles), Gather Shiny Gold Objects 333 (guide: 5 pieces), Kill Gruh'ar and take stash 440, Dont Get Struck By Laveka 839 (name only).

Spell4 candidates (ids are not per-dungeon; all Correlated, tier given):
* Text anchor (strongest): 36324 "Enraging Fury" -> text 292911 Thunderfoot's Rage. A second "Enraging Fury" row 70631 has no anchor.
* Internal name: 52790 `[Dungeon Boss] Bosun Octog - Ink - Two Targets`; 43556 "Remove Tugga's IA" and 43557 "Reset Tugga's IA" (an effect of type 0xb with duration 15000 on the reset row; "IA" presumably interrupt armor, unverified, but fits the guide's "no interrupt armor in phase 2"); 63355 and 69343 `[SK] Thunderfoot - Challenge - Failure Signal`; 54567 "Mordechai Summon"; 54704/54707/54739 "Terraformer Flash"; 53038/53242 `e365 ... Skullcano` (Redmoon Stinger Missiles, objective 365).
* Name-only (Correlated weak): Into the Stew 63303/63304/63308/63327/63353/63356 (63304 carries three 20000 ms type 0x4/0xb effects, plausible for the pot minigame); Molten Rain 41016 and 63344; Magma Totem 63346 and Magma Totem Trio 63295 (the tethering cross totem; guess); Thunder Pound 36341/40909/63407 and Pulverize 36283/39090/47691/47693/51335/51338/63405/64925/66145; Hookshot 31283/40494/41144/41146/41523/41706/41708/42994/45468/61418 and Hookshot Stun 63298; Monkey Mine Throw 63339 (raw cooldown 10000, unverified column); Kneecap 41901 (7000 ms effect), Dead Eye 4626/25002/25007/25012, Fatal Shot 63677/63682; Big Bang 39491.
* The 633xx rows (63295-63360) are a shared revision batch: it also holds Sanctuary of the Swordmaiden rows (Plague Splatter 63321/63324/63360, Molten Wave 63347-63350), so the id range does not identify a dungeon (same lesson as the Kel Voreth correction). Several names (Hookshot, Pulverize, Whirlwind) are shared with unrelated creatures.
* The Spell4 "cast time" column is not milliseconds (values 1 to 9) and the cooldown column was not verified against any clip; neither is used here.

## 4. Timing
None measured. A telegraph-gap measurement (Blade-Wind style, 2 Hz with the boss selected) was not attempted; the only numbers are the single-source 33% and 10% for Mordechai and the single-source "5% per stack" for Octog. Rotation cadence of Mordechai's lasers ("continually speed up", 2014-03-31 clip) is a qualitative remark.

## 5. Open inputs
* Cast list per boss and per version (normal 24xxx vs veteran 248xx/249xx rows), cast order, intervals, interrupt armor counts (the guide gives none except Tugga phase 2 "none"), damage, telegraph sizes: need either a native read of the creature spell list or a packet capture; video cannot supply them.
* Health thresholds: Tugga phase change, Octog ink phases (guide: 2 or 3 times), Thunderfoot enrage stacks, Mordechai's laser phases; a second independent source for Mordechai's 33%/10% and Octog's 5%.
* Which of Tugga's tooltip texts ("enters a feeding frenzy!", text 291636, no Spell4 reference found; Spline2Node hit is a numeric coincidence) is a yell/banner vs a spell.
* Spawn positions: mushrooms, food, squid adds, mines, Mordechai pools/turrets; add counts.
* Normal-mode behaviour: only the 2016 dev stream shows normal (no detail).

## Verdict
Skullcano has an agreed ability list for all five fights from at least two clips and four client-text anchors for challenges (Tugga's Diet, Jump Jump Around/Seismic Tremor, Broken Armor, terraformer blind), plus internal-name Spell4 rows for Octog ink, Tugga interrupt armor and the Thunderfoot enrage. It has no agreed cast order, interval, interrupt count or health threshold. Per the prompt, no boss code is justified yet; allowed slices without invented values: disabled phase placeholders for Mordechai at 33% (single-source) and Tugga phase 2 (threshold unknown, so only a named stub), the Thunderfoot enrage stack using Spell4 36324 as an anchor once a stack value is read from the table, and the Tugga meat/food challenge hook from the achievement text. None started. Recommended first boss if implementation is wanted: Thunderfoot (best anchor).
