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

## Yell and spell binding from the tables (2026-11-09, lead ran the scans, Correlated)
Scripts (scratchpad, not committed): text search on `en-US.bin`, a column scan of every `.tbl` in `I:\tools\extract\DB` for given LocalizedText ids, and a Spell4 row dump with names.
* **The Blade-Wind yell lines are a Stormtalon's Lair text block.** `LocalizedText` 534470 to 534487 are contiguous: 534470 "Some may perish, but you cannot defeat us all!", 534471 "One of the faithful has fallen. Yet still I live!", 534472 "You dare shed the blood of Stormtalon's disciples?", 534481 "Cower before the fury of the coming storm!", 534482 "Behold! The ancient power of the Masters!", 534483 "Witness the power that I have been granted!", 534484 "Fall beneath my shattering thunder!", 534485 "Kneel before the soon-born god!" (seen on screen at 24 s), 534487 "Gathering power to release a devastating storm.".
* **Which tables reference them:** only 534472 is referenced by game data (five `Creature2Action` rows, ids 5647, 5664, 5699, 5741, 5764, column 2); 534481 to 534485 are not referenced by any `.tbl`, so the yells are fired by server script, not data. That means the order and trigger of the yells cannot be read from tables, only from clips.
* **One text-to-spell binding:** `Spell4` 63114 "Tempest" references 534487 in column 62 (an icon/tooltip text column; the exact column name is unverified), and this is the only table link found between a Stormtalon text and a spell. It matches the guide's Aethros Tempest (a wipe after 15 s unless interrupted) and the row's raw cooldown column of 15000. Tier: Correlated (text match plus name and guide match; no client read).
* **The Stormtalon ability rows sit in one id block, 63110 to 63267, among unrelated spells (Spell4 ids are authored in time order, not per zone).** Within it, with names only (Correlated by name, id adjacency and the two internal `[STL]` rows 63171 and 63183 "e312 - Invoker - Static Barrier"): Lightning Rod 63110, Tempest 63114, Lightning Storm 63116, Static Wave 63124 and 63125, Static Charge 63127, Static Shock 63141, Static Field 63142, Lightning Strike 63157, Thunder Cross 63231, Static Shock 63251 and 63260, Lightning Strike 63252 to 63259 and 63267. Raw cooldown column: Lightning Rod 15000, Tempest 15000, Static Charge 2000, Thunder Cross 1500, Lightning Strike 63252/63257 to 63259 1500. The "cast time" column holds values 1 to 5, so that column is probably not milliseconds; do not use it as a cast time. Electrostatic Pulse is not in this block (ids 39074, 47206, 47210, 70364 elsewhere), so the phase 3 pulse is unbound.
* **Evidence limit:** nothing here proves which row a boss casts in which version (normal or veteran), the cast order, or any interval. The cooldown column is a spell-table value, not an observed gap.

## Updated decision
The yells and the Tempest row give two table anchors, but still no cast list and no phase framework. The remaining route to a first encounter slice is a clip-side measurement (telegraph appearance gaps at 0.5 s) against these candidate rows, or a native or packet read of the creature spell list; neither was run.
