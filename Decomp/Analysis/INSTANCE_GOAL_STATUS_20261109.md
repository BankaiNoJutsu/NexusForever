# Instance encounter goal: status per instance row (2026-11-09)

Status of [INSTANCE_ENCOUNTER_VIDEO_GOAL_PROMPT.md](INSTANCE_ENCOUNTER_VIDEO_GOAL_PROMPT.md) against the 27 rows of `coverage/content_retail_completeness_instances.csv`. Every packet below except Blade-Wind and the first Kel Voreth packet is **agent-written, not re-derived or re-viewed by the lead**; the lead verified only the text anchors for Spell4 36324 and 81706 and the Spell4 tag finding. No retail parity claim anywhere.

## Outcome per row (phase B packet written; phase C not started except Blade-Wind)
| Row (world) | Packet | Outcome |
|---|---|---|
| Stormtalon's Lair (382) | BLADEWIND, AETHROS_STORMTALON | Skeletons agreed in part; thresholds disputed between clips (Aethros 80/40 vs 66/33; Blade-Wind 60 vs 50); Spell4 tag rows (e312 to e314) bind every ability; cast order and intervals missing. Code: disabled Blade-Wind phase placeholder. |
| Ruins of Kel Voreth (1336) | KELVORETH, KELVORETH_SECONDSOURCE | Trogun 50% and four armor agreed; Grond and Drokk thresholds not confirmed; `[KV]` tags e444/e446/e449 bind rows. |
| Skullcano (1263) | SKULLCANO | Ability lists agreed for five fights; four text anchors (e.g. Spell4 36324 lead-verified). |
| Sanctuary of the Swordmaiden (1271) | SANCTUARY | Resonance mechanic text-anchored (Spell4 42041, 42024); boss ability names from two clips. |
| Protogames Academy (3173) | PROTOGAMES_ACADEMY | Packet written; no Spell4 bindings yet (re-run against tag CSV `[UPJ]`/`[UP]` as applicable). |
| Ultimate Protogames (2980, 3041, 3042 area) | ULTIMATE_PROTOGAMES | 2980 partly covered; 3041 (Downsizer) has zero footage and text: missing input. |
| Coldblood Citadel (3522) | COLDBLOOD_CITADEL | One official PTR walkthrough narrates four fights, no numbers, no second clip. |
| Genetic Archives (1462) | GENETICARCHIVES | X-89, Kuralak, Phage Maw, Convergence agreed by two or more clips; Prototypes, Gravitron single-source; `[GA]` tags bind rows. |
| Datascape (1333) | DATASCAPE | Only System Daemons has two narrated sources; four test creatures and others without usable footage; `[DS]` tags bind rows. |
| Infestation (1232), Outpost M-13 (1319), Rage Logic (1627), Space Madness (2149), Gauntlet (2183), Deep Space Exploration (2188), Fragment Zero (3180), Evil from the Ether (3404) | EXPEDITIONS | One section each; Fragment Zero best covered; Rage Logic four clips from one commentator; Evil from the Ether: `Turned Ravenous` 81706 text-anchored (lead-verified), no clip agrees or disagrees with the existing Katja script numbers (55%, 20 s). |
| Redmoon Terror (3032, 3524), Redmoon Terror 40-man (3102), Augmentor's Raid (3040), BAD (3042), Vault of the Archon (3009), OMNICore-1 (3045), Shade's Eve (3044), Protostar Winterfest (3094) | RAIDS_EVENTS | BAD is a placeholder (no event, map entry or script); 3524 and 3102 have no footage; `[RMT]` 920 tag rows; Robomination only two-clip agreement; Winterfest structure from two official streams. |

## What the goal still lacks
* Phase C slices for almost every boss: a boss needs cast order and intervals, which no source here supplies; thresholds are single-source or disputed for most bosses.
* Phase A open items: interrupt effect on a pending scripted cast, invulnerable adds and shields, wipe reset, wiring of `EncounterHealthPhases`.
* Re-run every packet's row binding against `coverage/spell4_internal_tags_20261109.csv` (tags give boss and ability, not creature id or order).
* Lead verification of the agent-written packets.
* Missing-input statements per row exist inside each packet's "Open inputs" section; none is lead-verified.
* A native read or packet capture of the creature spell list and AI (no table links creatures to spells) is the one source that could supply cast order and intervals; it has not been commissioned.
