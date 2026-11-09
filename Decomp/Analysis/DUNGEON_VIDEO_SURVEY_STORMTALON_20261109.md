# Dungeon video survey: Stormtalon's Lair (2026-11-09)

Question: can retail-era video be used to implement dungeon encounters? This is a survey of one dungeon, not an implementation. Evidence tier: **Observed** (agent/lead subtitle reads only; no frame was viewed in this pass). No retail parity claim.

## Corpus counts (corrected)
The counts quoted in chat earlier on 2026-11-09 included videos with an empty `upload_date`; undated uploads are never evidence. Corrected counts, pre-2018-11-28 and dated, by title/description match (not verified per boss):

| Content | Videos | With subtitles |
|---|---|---|
| Stormtalon's Lair | 149 | 72 |
| Kel Voreth | 136 | 76 |
| Genetic Archives | 122 | 81 |
| Datascape | 116 | 86 |
| Halls of the Bloodsworn | 69 | 35 |
| Skullcano | 69 | 32 |
| Sanctuary of the Swordmaiden | 52 | 30 |
| Protogames | 45 | 22 |
| Ohmna | 24 | 15 |
| Gauntlet | 21 | 14 |
| Avatus | 17 | 14 |
| Kuralak | 16 | 13 |
| Redmoon Terror | 15 | 7 |
| Maelstrom | 6 | 2 |
| Coldblood Citadel / Laveka | 1 / 1 | 1 / 0 |
| Infinite Generator | 0 | none |

Tool: `py -3.13 -I` with `corpus.db` `meta_fts`, filter `upload_date <> '' and upload_date < '20181128'`.

## Stormtalon's Lair footage
* About 140 dated clips, 2013-09-02 to 2017-08-21. Pre-launch beta clips (2013-09 to 2014-05) can differ from the 16042 tuning; prefer 2014-06 onwards (launch) and the 2015 clips.
* Best single-boss sources (post-launch, subtitled, one commentator each, so a single-source reading):
  * Blade-Wind the Invoker: `https://www.youtube.com/watch?v=8M53-Kf90a4` (2015-09-28).
  * Aethros: `https://www.youtube.com/watch?v=A7d1h77Ag60` (2015-10-04).
  * Stormtalon: `https://www.youtube.com/watch?v=99dod9Ba-Ks` (2015-10-05).
  * Veteran full runs with subtitles: `T2OJbSH7aD8` (2015-08-24), `_Y0MWGDS8QU` (2015-10-08, normal), plus many 2014-06/07 silver and gold runs.
* Resolve videos by URL; corpus ids changed in the 2026-11-09 rebuild.

## What the guide subtitles say (Observed from speech, not re-viewed)
Blade-Wind the Invoker (veteran, 3 phases, 2 interrupt armor): phase 1 tank and spank, four invulnerable disciples that pulse minor damage, Thundercross (high damage, knockdown, interruptible). Phase 2 at 60%: disarm everyone, boss invulnerable, Lightning Strike on random targets in two telegraph stages, disciples gain a shield removed only by the boss's Lightning Strike, dead disciples leave rotating static wisps (stun), Thundercross continues with always two Lightning Strikes between casts, constant group pulse damage. Phase 3 after all four disciples: like phase 1 with wisps, no Thundercross, Electrostatic Pulse cast three times in succession (nine interrupts in total).

Aethros (veteran, 3 phases, infinite interrupt armor, four interrupts needed for one mechanic): phase 1 tank and spank, Torrent (lightning on all players, knockdown). Phase 2 at 80%: boss leaves, three adds immune to CC and taunt, Static Shock leaves permanent floor telegraphs. Phase 3: teleport to the entrance of the chamber, Tempest wipes after 15 s unless interrupted, run through tornadoes and straight-line lightning. Repeats; next phase 2 at 40%; tornadoes alternate slow and fast.

Stormtalon (veteran): cone auto attack chaining lightning, slows by 90%; Static Charge (random-player telegraphs that strike lightning and leave a puddle, stacking damage and cast-speed buff, soft enrage); at 90% runs to the centre, knockback and stun, Static Wave with four interrupt armor, repeated "every now and then" until about 50%; at 50% Lightning Rod (everyone knocked down, one player disoriented controls the safe spot).

## What this gives and what it does not
* **Gives (Observed):** phase thresholds (Blade-Wind 60%; Aethros 80% and 40%; Stormtalon 90% and 50%), ability names and order within a phase, interrupt counts, which mechanics are optional-objective conditions. The counts and thresholds are single-source.
* **Does not give:** cooldowns or cast intervals (the guides say "periodically"), damage numbers, telegraph sizes, spawn positions or add stats. These need either Spell4 table rows or multiple-clip timing.
* **Spell identity is ambiguous by name.** Searching Spell4 names finds roughly 40 "Lightning Strike" rows and several each of Static Shock, Tempest, Thunder Cross, Electrostatic Pulse and Torrent (for example Thunder Cross 38786, 63231, 63525; Electrostatic Pulse 39074, 47206, 47210, 70364; Tempest 39682, 63114, 74367, 74374, 74383), shared across zones and veteran/normal variants. The table has no creature-to-spell link: `Creature2Action` is client-side talk/emote data. So which row belongs to which boss version cannot be read from the tables; it needs clip evidence (cast bar text with timing, telegraph shape) or a native/packet source.
* **Server state:** `Source/NexusForever.Script.Instance/Dungeon/StormtalonsLair` has kill credit and objective routing only (the boss scripts are `PublicEventObjectiveCreditEntityScript` subclasses). No boss ability or phase logic exists. The entity script event `EntityCastEvent` exists in `NexusForever.Script.Template`, so scripted casts are possible in principle.

## Decision and next step
Video can supply the ordering and phase skeleton for Stormtalon; it cannot supply the exact cast timers or the spell-row binding. A first implementation could stay at Observed tier: phase transitions by health and a fixed cast order, with cast rows chosen only where a clip shows the cast name and a matching telegraph. Recommended first target: Blade-Wind (fewest hidden inputs). The next step is a frame pass on 2-3 full-run clips to read cast-bar names and measure the gap between casts at 0.5 s resolution, then a failing phase-transition test. Not started.

## Frame pass, Blade-Wind (2026-11-09, lead viewed, Observed)
Clip `https://www.youtube.com/watch?v=P7eQtXKxVlM` (2014-06-09, post-launch, 1280x720). A 12-frame sheet at 4 s spacing (20 to 68 s) and one full frame at 24 s, with a 3x crop of the target frame, were viewed.
* **Cast names are not readable.** The target frame shows the boss health bar, the interrupt-armor shield counter ("0" at 24 s) and a red-bordered frame, but no cast-bar name. All 21 candidate Stormtalon clips I probed are 1280x720 (one 640x360), so this is a property of the library, not of one clip. Binding casts to Spell4 rows by reading cast names off video is therefore not possible here.
* **What is readable:** the boss yell lines in the chat log and top banner ("Kneel before the soon-born god" at 24 s), the Events panel (optional objectives "Gather Corrupted Stem samples 0/15", "Eliminate Thundercall Pell 9/20", challenge "Stormchaser"), telegraph shapes (red cone/line and blue circle at 4 s spacing), and the number of players alive.
* **Telegraph timing is the only cast-timing signal** (red/blue area appearing and ending). Measuring gaps from it needs frame diffing at about 0.5 s and was not done; the 4 s sheet only shows that a red telegraph and a blue circle overlap in phase 1.

## Decision
No phase-transition test was written. The phase thresholds are single-source (one guide per boss), the server has no health-threshold or scripted-cast framework for these scripts, and a test would encode an invented cast order. Two evidence routes remain that can bind casts to rows: (a) the boss yell strings, which can be matched to `Creature2ActionText`/text rows and may be tied to phase changes; (b) a packet or native source for the creature spell list. Telegraph-gap measurement from video is possible but only gives Observed timing, not rows.
