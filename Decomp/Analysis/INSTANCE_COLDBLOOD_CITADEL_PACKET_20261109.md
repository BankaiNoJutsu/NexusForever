# Instance packet: Coldblood Citadel (world 3522, PE 907) (2026-11-09)

Phase B packet per [INSTANCE_ENCOUNTER_VIDEO_GOAL_PROMPT.md](INSTANCE_ENCOUNTER_VIDEO_GOAL_PROMPT.md). Evidence tier: Observed (subtitle reads by an agent; no frame viewed) and Correlated (client text/table scans). Agent-written, not re-derived by the lead and not re-viewed. No code, no retail parity claim. Corpus ids changed on 2026-11-09; clips are given by URL.

Repo scaffold: `Source/NexusForever.Script.Instance/Dungeon/ColdbloodCitadel` (kill credit and objective routing plus `IcebloodCovenEntityScript`, `HarizogColdbloodEntityScript : CombatAI`, `HailstoneGatecrasherEntityScript : CombatAI` by script name; no boss ability logic). The prompt expected a stub packet with one clip; the library actually holds one developer walkthrough with real per-boss content, so this packet is a normal one, but single-source and from a pre-release test server.

## 1. Footage
Dated clips (before 2018-11-28) that contain Coldblood Citadel content: two, both from the official channel and both in January 2017, i.e. about two months before the 2017-03 live update. No community runs, no guides, no live-server clips exist in the library (searches: title/description FTS for Coldblood, Harzog/Herzog, Primal Matrix; subtitle FTS for Hailstone, "ice blood", "cold blood", citadel). Emulator footage and undated uploads were ignored.

| Role | Date | URL | Length | Subs | Notes |
|---|---|---|---|---|---|
| Coldblood Citadel and Evil from the Ether livestream (official WildStarOnline channel, two Carbine developers, PTR build) | 2017-01-23 | https://www.youtube.com/watch?v=AY3W8n8Zh3c | 3246 s (Coldblood segment about 1291 to 3000 s) | yes (auto-captions, names garbled) | The only boss-level source. The developers use dev cheats, solo a five-person dungeon, and say "this will not represent a real run". 1280x720-class; cast bars not legible. Prime 1. The first 1290 s covers the expedition Evil from the Ether (a different instance row, not packeted here). |
| Power of the Primal Matrix preview livestream (official channel) | 2017-01-07 | https://www.youtube.com/watch?v=SlE5SZxaqNI | 3132 s | yes | About 60 s on Coldblood (2293 to 2370 s): lore and fight order only (Osun who serve Harizog, then the dark witches, then the resurrected Harizog), plus the Prime-level scaling. |
Both are pre-live; the live update (Primal Matrix, March 2017) may have changed tuning. Whether the client in this repo (build 16042) equals the final live build is not established by these clips.

## 2. Skeleton (Observed; single-source throughout except the fight order, which both clips give)
Named by the developers in the 2017-01-23 narration (garbled caption spellings noted). Instance flow: Pell (Winterfury) allies siege the gates; trash packs with "shared health pool" clusters (soul-bonded, damage to one is damage to all, about 4x effective health each) and slide ("Vector slide") ice sections; boss 1 Hailstone Gatecrasher; a miniboss; a staircase; the Iceblood Coven (three dark sisters); then the resurrected Harizog Coldblood (fight order: A across both clips).

**Hailstone Gatecrasher (boss 1; objective 5313)** — a giant ice elemental infused with Soulfrost; the Pell summoned him to break the gate:
* Being near him slowly freezes the screen edges and applies a worsening snare; jumping clears the debuff, so players must keep moving.
* Ice spikes (the developers call him a spike lover).
* Berserker Rage: he hits much harder; a limited number of Pell channelers appear for the fight (helpers; the garbled caption says he kills the Pell channeler that did it to him, unclear); the rage can be removed in a timely manner (the caption shows a moment of opportunity/interrupt "takes off the berserk" after dragging him). On higher Prime levels the removal matters because of one-shots and the limited number of removals makes a real DPS check; the developers compare the design to Thunderfoot's mushrooms.
* An interruptible cast that is "a group activity" (needs multiple interrupters).

**Miniboss "Karck the Soul-rotten" (caption "carck"; name not confirmed by any text)** — described as a former comrade ("one of his bros") of the Osun Tower Engineer who recruited the player (caption "Ren hakul/renle"); already corrupted by Soulrot:
* Infects players with a Soulrot contagion; with several players it links them together and damages them; the link must be broken; a cast can be interrupted. The developers stress that the mechanics depend on more than one player (group interrupt, link breaking).

**Iceblood Coven: Darksister Ulfrid (caption "Ulri/alred"), Katla, Golag (client text 762731 to 762733)** — one fight, three sisters of different colours; more of them activate as the fight goes on; Ulfrid can be killed before Katla appears but Katla comes immediately:
* Ulfrid: chasing shadowy spikes. Katla: blood magic, blood tornadoes. Golag: slow, hits very hard in front (kite her), infused with Soul Frost (snare), leaves a Soul Frost trail behind her, casts Soul Frost spikes; "the toughest, saved till last".
* DPS check: if the group is slow all three are up at once (more damage and more to avoid); scales with Prime level.
* The sisters' spirits are the final sacrifice for Harizog's resurrection (cutscene); the last sister killed (normally Golag) determines which element Harizog is infused with first and so his available abilities.

**Risen Harizog Coldblood (final; objective 5315; Creature2 75459 in the repo)**:
* Opens with Soulfrost Surge: drops a pool of Soulfrost that threatens players and is a defensive field for him, so he must be moved out of it; large arena used fully.
* Summons Primal Echoes from six soul channels around the room (spread the group); they die in a few hits; every echo that reaches him is absorbed ("Primal Infusion") and hurts the group and raises his damage.
* Late in the fight he uses the sisters' abilities (blood tornado, spikes) together with his own; end of fight "real exciting".
* Warm-up time at the start (he takes a few seconds to engage).
No health thresholds are stated by the developers. Interrupt armor counts: none stated.

## 3. Row binding (Correlated; nothing Mapped)
Creature2 ids already in the repo scripts: Harizog Coldblood 75459; Darksister rows 75472 / 75473 / 75474 (objective 5314 `IcebloodCovenEntityScript`, the repo comment says Golag, Katla and the third sister); Pell War Drum 75706; Soulfrost Shards 75731 to 75733; Soulrot Canister 75708; Winterfury Prisoner Cage 75737; Krovak trap 75747; Liquid Soulfrost sample 75736. The Hailstone Gatecrasher script is bound by script name only (objective 5313); its Creature2 id is not in the repo comments.
Client text: "Darksister Golag/Katla/Ulfrid" 762731 to 762733, "Hailstone Gatecrasher" 763065, "Coldblood Citadel" 763067/763108 (not looked up on Spell4).
Spell4 candidates, all Correlated (name stated by a developer in the narration and a Spell4 row of that name exists; no text anchor, no creature link), in one authored block at 88195 to 88400:
* Soulfrost Surge 88210; Soulfrost Well 88211 and 88393 (probably the pool; guess); Soulfrost Trail 88252 and 88253; Soulfrost Armor 88262; Primal Echo 88295; Spirit Infusion 88296; Primal Power 88297 (the developers say "Primal Infusion"; closest rows by name).
* Ulfrid's Cry 88212, Golag's Cry 88213, Katla's Cry 88214; Seeking Shadow 88202, 88203, 88260, 88261 (chasing shadows, guess); Blood Boil 88205, Blood Burst 88206 and 88216, Blood Bolt 88218 (Katla, guess).
* Soulrot Contagion 88275 to 88277 and Remove Soulrot Contagion 88278; Soulrot Leakage 88282; Soulrot Explosion 88286; Soul Binding 88270 to 88274 (the link, guess).
* Frost Fields 88293, Ice Storm 88285, Freezing Blast 88195, Frozen 88197, Hate Aura 88284 (possible Hailstone proximity freeze, guess), Hammer Smash 88283, Sovereign Slam 88198.
* Internal tag: Spell4 88392 `[CBC] Pell Boss Music` (CBC is most plausibly Coldblood Citadel; unverified, id-adjacency to the block above).
* Older/shared names that also match and are weaker: Ice Spike 61275/61289/74398/79425-79427, Ice Spikes 81722, Hailstone 80035 and 80036 (a spell, not the boss), Berserker 51534/59077/60915, Soulrot Sickness 41201/50071, Soulrot 69372/69373/69386.
This block also contains unrelated content (Love Launcher, Nano Spike, Maelstrom, Locus of Power, Darkspur Chariots, Portable Black Hole), so the block is not a per-dungeon id range.

## 4. Timing
None measured. The only timing is the developers' remark that Harizog has a warm-up and that the Berserker Rage "will matter more as Prime scales"; neither is a number.

## 5. Open inputs
* Everything numeric: cast order, intervals, thresholds, interrupt armor, damage, the count of Pell channelers, the Berserker Rage trigger, the Primal Echo spawn cadence, the contagion link range. The one clip is a dev demonstration with cheats and tuning not final; a numeric source could only be a native read of the creature spell list / script data or a live-server packet capture. No other video in this library supplies it (no community footage exists).
* A second independent clip: none in the corpus. A normal/veteran/Prime comparison: none (Prime 1 only).
* Miniboss identity and Creature2 id (Karck/"carck") and the Hailstone Gatecrasher Creature2 id: read from the table, not done.
* PE 907 objective timing and the Hailstone "cadence" blocker named in the instance row: not addressed by video.

## Verdict
Coldblood Citadel has one narrated dev walkthrough, two sources for fight order and no second source for anything else. It is a complete outcome as "ability skeleton from one developer-narrated clip, no numeric input, no second clip": every fight has a described mechanic list with Spell4 name matches in one block (88195 to 88400), but nothing is agreed by two clips and no threshold, interval or cast order exists. Per the prompt, no boss code is justified; the smallest slice with no invented value would be disabled placeholders keyed on the developer-stated names (Soulfrost Surge, Primal Echo spawn from six channels, Soulrot Contagion link, Berserker Rage removal) with every number left unset. The exact missing input is: a second independent source (live-server run footage, a packet capture or a native/script read) for any of those numbers; no other source in the video library can supply it.
