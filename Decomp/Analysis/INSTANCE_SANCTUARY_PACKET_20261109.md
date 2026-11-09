# Instance packet: Sanctuary of the Swordmaiden (world 1271, PE 166 and 202) (2026-11-09)

Phase B packet per [INSTANCE_ENCOUNTER_VIDEO_GOAL_PROMPT.md](INSTANCE_ENCOUNTER_VIDEO_GOAL_PROMPT.md). Evidence tier: Observed (subtitle reads by an agent; no frame viewed) and Correlated (client text and table scans). Agent-written, not re-derived by the lead and not re-viewed. No code, no retail parity claim. Corpus ids changed on 2026-11-09; clips are given by URL.

Repo scaffold: `Source/NexusForever.Script.Instance/Dungeon/SanctuaryOfTheSwordmaiden` (kill credit and objective routing; `AdditionalPublicEventIds => [202u]`; no boss ability or phase logic). Public event 202 was not examined in this packet.

## 1. Footage
52 dated clips (survey count), about 30 with subtitles, 2014-01 to 2016-06. Boss identification below comes from client challenge text, not from clip titles, because the two best guides do not say boss names aloud (the speech is "the fire boss", "the third boss", ...).

| Role | Date | URL | Length | Subs | Notes |
|---|---|---|---|---|---|
| Veteran silver guide (Vhalor Gaming) | 2014-06-28 | https://www.youtube.com/watch?v=T7acBMuJRSk | 4079 s | yes | Post-launch. Full run, all five main bosses, 6+ optional minibosses, hold-out event. Single commentator = single-source. |
| Dungeon guide, live (SywoGG) | 2015-09-15 | https://www.youtube.com/watch?v=8lJX5YzUk8c | 2177 s | yes | Post-launch, independent commentator; first 4 bosses, three minibosses. Does not reach the final boss. |
| Veteran silver, Drow (second full run) | 2014-06-28 | https://www.youtube.com/watch?v=4MzIC3Jj-t4 | 4390 s | yes | Not mined. |
| Other silver/gold runs with subtitles | 2014-06 to 2015-05 | `I8RzeC10em4`, `XlYp1YjZUvY`, `_JG3G1fK3jU`, `wHtjE8nSnvc`, `HKblb5MMt1c`, `433skEp1M7k`, `iYadYMt1uMA`, `ruOmdr9YWro`, `5E-aCD8zy0k`, `lwn9zM5b_Yg` | 3000-6800 s | yes | Group callouts; not mined. |
| Per-boss engineer-tank clips | 2014-07-20/23 | `HAHuzDb_39o` (Shallaos), `NiLFkgLiwp0` (Skash), `gxpXtsOwISk` (Ondu), `KqUcC4i7aAU` (Selene) | 180-335 s | yes | Read: mostly chat noise, no usable mechanics. |
| Vet kill clips (Ondu, Skash) | 2014-05-15 | https://www.youtube.com/watch?v=YLPy7oUmVXY, https://www.youtube.com/watch?v=G9Nl_lvkYkc | 361 s, 376 s | yes | Beta, group chatter. |
| "Strategy Guide" per-boss clips | 2014-04-20 | `ZGG6ycDG99w` (Shallaos), `-QaJI9ezrdI`/`HdOTAcZ-CfY` (Dareia), `OXIrPfQ5ntA` (Selene), `3BLdY7FuvxM`/`tS_fna1Jp8M` (event) | 75-383 s | yes | Beta; subtitles are music/chat, no information. |
| Last-boss debug (German) | 2014-09-09 | https://www.youtube.com/watch?v=QoJ5JbDX1ZU | 1115 s | auto-translated, unusable | Final boss in German; subtitle quality too poor to read. |
| Normal playthrough | 2016-06-26 | https://www.youtube.com/watch?v=4IhmjnZMPHM | 2212 s | no | Not read. |
Public-event 166 vs 202 split and PE 202's contents: not examined.

## 2. Skeleton (Observed; "A" = two independent clips and/or client text, "S" = single-source)
Boss identity is bound through Prime-difficulty challenge texts (Correlated): Shallaos = Resonance, Rayna = Molten Waves, Ondu = Plague Splatter, Skash = Corruption Heartseeker, Selene = Darkness Effigy / "Lights Out!". Prime texts exist (Prime was added after launch); normal vs veteran vs Prime tuning of these fights was not compared.

**Deadringer Shallaos (boss 1, objective 479)**
* A (Vhalor guide, SywoGG guide, client text): no aggro table. Aggro goes to whoever has the most "Resonance" stacks; stacks come from AoE hits and from hitting the rotating "chimes", so the tank hits a chime to get a few stacks. The tank must keep more stacks than anyone else; too many stacks raises damage taken. Challenge: group stays under 25 stacks (SywoGG guide: "no more than 25 stacks as a group"; client text 409261/409262 "without your party gaining a total of 25 or more stacks of Resonance"; a zero-stack variant 565631). Client text anchors: "Attackers gain Resonance." (text 375312) is referenced from Spell4 42041 "Percussion Chime" (col 62); "You currently have the most Resonance. Shallaos is hunting you down!" (text 375313) from Spell4 42024 "Acoustic Beacon" (col 63). Tier: Correlated (two Spell4 text anchors plus two clips). Related names: Spell4 42019 and 39122-39126/39388/39389 "Resonance", 42462 "Resonance Buffer", 42042/42043 "Reverberation", 42027 "Echo".
* A (both guides): the fight alternates a short tank-and-spank phase (good DPS skips it in seconds) with a phase where incoming waves must be dodged while chimes randomly explode; pillars at the back project telegraphs. Interrupts are needed (SywoGG: bring three as tank).
* S (Vhalor): standing in the room middle and watching the south-east and south-west corners makes the waves predictable.
* The 2014-07-23 engineer-tank clip (https://www.youtube.com/watch?v=HAHuzDb_39o) has a caption at 172 s beginning "embrace the silence ...", probably a Shallaos yell; its text id was not looked up and it is unbound.

**Rayna Darkspeaker (boss 2, objective 486; "the fire boss")**
* A (both guides, challenge text): tethers one player and repeatedly flamethrows them (CC break ends it; a tank can sidestep); circular telegraphs under players that grow and erupt into lava pools (do not drop them on the boss); a phase where players are pushed into the air and HP drops (SywoGG: "drops your HP to really low"), then waves of fire sweep from one side with one gap (Vhalor: always one break in each wave; SywoGG: the damage from a wave hit also fails the challenge); a later pass has a wave from a random one of the four walls while she keeps tethering and dropping circles. Challenge text: "without anyone in your party getting hit by any Molten Waves" (text 409252/409253, 524775). Spell4 "Molten Wave" 42155/42184/42236/42295 and 63347-63350; Spell4Base name-text anchors for Molten Wave are 26349/26376/26428/26486 (the Spell4-to-Spell4Base join was not run). Totem: Spell4 43140 "Totem Incineration" carries text 404686 "Burn the Totem of Flame." (col 31); Creature2 43173 is "Torine Totem of Flame" (text 398216); objectives 499/1503 in the repo. Tier: Correlated.
* S (Vhalor): a banner "the ground begins to rumble" before the wave (that exact text was not found in the client text table in the Sanctuary range; unbound).

**Ondu Lifeweaver (boss 3, objective 480)**
* A (both guides, challenge text): "Clover Leaf" pools of darkness on the ground that tick quickly (client name Plague Splatter: challenge "without anyone taking damage from the first pulse of Plague Splatter", text 409258/409259, 524777); a phase that roots four players while the unrooted fifth, with a shield, runs through bombs coming from the door; a channel to one of three Totems of Life that heals him fully unless interrupted (SywoGG: needs three interrupt armor, Vhalor: "just interrupt it"); he enrages (hits much harder) if the fight is slow; a yellow-shield phase where he spams the pools and must be kited (Vhalor). Spell4 "Plague Splatter" 42423-42426 (three marked DEPRECATE), 43153, 43316, 63321, 63324, 63360. Totem of Life text 390446 is on Creature2 42486. Tier: Correlated.
* S (SywoGG): tank next to the door to lengthen the walk to the back of the room for the root/shield phase.
* No health thresholds stated.

**Moldwood Overlord Skash (boss 4, objective 483, 1504 prisoner timer)**
* A (Vhalor 2014-06-28, SywoGG 2015-09-15 for the swarm and eggs): spawns many spider adds, so the group stacks; "Tentacle/Strain Overgrowth" (captions differ; Spell4 only has "Strain Overgrowth" 43088-43090/43862/45500) is a large telegraph with square safe zones, interrupt the first one; a "hammer phase" puts red circles under every player and sends chasing circles that spawn spiders when they reach a target, so the tank stands between the group and the boss; eggs spawn and are tethered to players, stun them to break the tether so the eggs become eyeballs that fire telegraphs; keep the boss off the room centre because the hammer phase forces everyone to the centre; he has two interrupt armor (SywoGG, S). Challenge "without anyone hit by a Corruption Heartseeker" (text 409264/409265, 524779): Spell4 42062, 42149, 42151, 43027, 45010, 63459, 63479-63482, 63494, 63561 "Corruption Heartseeker" (the Prime challenge ability; its relation to the eggs/eyes is not established). Tier: Correlated.
* The caption conflict between "Tentacle Overgrowth" (2014) and "Strain Overgrowth" (2015, attributed there to a miniboss) is unresolved; the 2015 clip also gives "Strain Overgrowth" a role on an optional miniboss.

**Spiritmother Selene the Corrupted (final, objective 492, escort 662)**
* S (Vhalor only; the German clip is unreadable): preceded by a hold-out event of four to five timed waves after four spirit relics are placed (waves include a whirlwind swordmaiden that must be interrupted, a well-of-life caster, crawlers with a caustic barrage, a vile-tongue-lashing corruptor and a death-sting bee; one person talks to Selene to start it). Boss phases: phase 1 "Shadowmeld", one move to interrupt; phase 2 "the whole room goes dark" with safe light zones, raid damage, and grapples (mark the healer); phase 3 shadows that must be stunned before they vanish, periodic blind plus a "Nightmare" wave to dodge (also damages on blind); phase 4 Shade Prison (interrupt; tether, CC-break), a wave you dodge by standing behind her; last phase repeats with safe zone and a shadow add.
* Client text corroborates the second phase and the add: challenge "interrupting or killing at least one Darkness Effigy during each 'Lights Out!'" (text 409255/409256; Spell4 42675 "Lights Out!", Spell4 42290 "Shade Prison"; Creature2 41238 is "Darkness Effigy", text 379791). Tier: Correlated, but the phase order and the number of cycles are single-source.
* No health thresholds stated.

**Optional minibosses and elites (all single-source, Vhalor 2014-06-28 unless noted; names not stated in speech except where marked)**
* Hammerfist Moldjaw (objective 1366): leaps at players; a quest-item bomb placed on the floor stuns him for 10 s when he leaps onto it (Spell4 43423/43428 "Mighty Leap"; the 2015 guide also names Mighty Leap on a mauler-type elite). Name binding of the leaper to Moldjaw is by guess.
* Corrupted Lifecaller Khalee (494): "Well of Life" must be interrupted, she spawns an add that channels an AoE (Spell4 43244/43245 "Well of Life", Spell4Base text anchors 27422/27423 for the name text 401127/401136; name-bound, add unnamed).
* Corrupted Edgesmith Torian (493), Corrupted Deathbringer Dareia (500), Lifeweaver Guardian (629), Elder Moldwood Ravager (639): described in the guides as warrior-charge/whirlwind/razor-disc, taste-of-death/large circle, and "Call of Darkness / Strain Overgrowth", but the guide never names them, so the bindings are not made.
* Shared elites: Death Sting hive defender (interrupt), mold corruptors (Vile Tongue Lashing 43042/43043, interrupt), "Whirlwind" must be interrupted (three interrupts, SywoGG).

## 3. Row binding (Correlated; nothing Mapped)
Creature2 ids already in repo scripts: Shallaos 28597 / 28599 / 28600; Ondu 28719 / 28720 / 28721; Skash 28728 / 28727; Rayna 28733 / 28732; Selene 28736 / 28735; Moldjaw 41219 / 41220; Torian 28985 / 28986; Khalee 28993 / 28992; Dareia 28995 / 28996; Lifeweaver Guardian 28774 / 28775; Elder Moldwood Ravager 29198 / 29199; Corrupted Veteran Swordmaiden 29267 / 15658. Which id is normal, veteran or the Prime/echo variant is not stated in the repo comments.
Objective enum anchors in `PublicEventObjective.cs`: 2383 The Sound and the Fury (Resonance), 2384 The Fire and the Dark, 2385 Splatter Skipper (Plague Splatter), 2386 Vitara's Heart (Heartseeker), 2387 Don't Blink; these correspond to the client challenge names (text 524773-524779 are the achievement/objective names).
Spell4 id block: the beta authoring cluster is roughly 42019 to 43430 (Resonance, Acoustic Beacon, Percussion Chime, Lights Out!, Shade Prison, Molten Wave, Plague Splatter, Well of Life, Vile Tongue Lashing, Mighty Leap, Death Sting, Strain Overgrowth all live there), with a second revision block at 63321 to 63360 shared with Skullcano. Ids are not per-dungeon; the 42xxx range is also full of unrelated content (transport spells, skies, deprecated placeholders).

## 4. Timing
None measured. Only qualitative remarks: Resonance chimes "randomly" explode; Selene's fight is "timer-dominated" by the 1 hour 15 minute dungeon limit (Vhalor), not a boss timer.

## 5. Open inputs
* Cast list per boss, order, intervals, interrupt-armor counts (only Skash 2 and Ondu's totem heal 3 are stated), damage, telegraph sizes and spawn positions: need a native read of creature spell lists or a packet capture.
* Health thresholds for every phase change (none stated in any clip read).
* A second independent clip for Selene (full fight) and for Skash's phases; the Selene German clip would need a human transcript.
* Public event 202 contents and how the hold-out event is wired; wave composition and timing.
* Which minibosses are which (Torian/Dareia/Guardian/Ravager) and their versions.
* Whether Prime differs from the veteran fights in the clips (the challenge texts are Prime-labelled; the 2014/2015 clips predate Prime).

## Verdict
Sanctuary has two independent full-boss sources only for the first four bosses (Vhalor 2014-06-28 plus SywoGG 2015-09-15), with client challenge texts that name each boss's signature mechanic and two Spell4 text anchors for Shallaos (Percussion Chime, Acoustic Beacon). Selene has a single full source and no thresholds anywhere. No agreed cast order, interval or threshold exists for any fight, so per the prompt no boss script is justified. Smallest allowed slice with no invented value: the Shallaos Resonance aggro rule (highest stack gets aggro, stacks from chime hits and AoE) is agreed by two clips and two text anchors, but needs the numbers from Spell4 42019/42024/42041 effects to implement; a Prime-mode challenge counter (25 stacks) is possible from text alone. None started.
