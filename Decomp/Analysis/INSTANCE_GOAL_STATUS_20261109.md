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

## Update (2026-11-09, after the user authorised provisional encounters)
Provisional, disabled-by-default boss scripts now exist for Stormtalon's Lair (3 bosses), Ruins of Kel Voreth (3), Skullcano (4), Sanctuary of the Swordmaiden (4), Protogames Academy (Wrathbone), Coldblood Citadel (4), Genetic Archives (3) and Datascape (3): see [INSTANCE_PROVISIONAL_ENCOUNTERS_20261109.md](INSTANCE_PROVISIONAL_ENCOUNTERS_20261109.md). Every other row has an exact stated missing input in that record or in the `ProvisionalEncounterSkipped.md` files. All behaviour is Source-tier over Observed/Mapped inputs with estimated intervals; none is retail parity, and none has been run in a client.

## Closure table, all 27 rows (2026-11-09, final state of this goal)
The prompt's stop condition per row is "implemented to its evidence tier, or an exact statement of the missing input and why nothing available supplies it". Evidence tier of everything below: Source/Observed/Mapped inputs, no retail parity, nothing run in a client; packets and scripts are agent-written and only partly lead-verified (see [INSTANCE_LEAD_VERIFICATION_20261109.md](INSTANCE_LEAD_VERIFICATION_20261109.md)).

| Row (world) | Outcome | Where stated |
|---|---|---|
| Stormtalon's Lair (382) | provisional scripts for 3 bosses; the rest missing input (adds, disciples, cast order/intervals) | PROVISIONAL, packets |
| Infestation (1232) | missing input: no spell row tied to a verified boss, no packet-reported ability | Expedition/ProvisionalEncounterSkipped.md |
| Skullcano (1263) | provisional scripts for 4 bosses | PROVISIONAL |
| Sanctuary of the Swordmaiden (1271) | provisional scripts for 4 bosses; Shallaos missing input | PROVISIONAL |
| Outpost M-13 (1319) | missing input | Expedition/ProvisionalEncounterSkipped.md |
| Datascape (1333) | provisional scripts for 3 bosses; others missing input | PROVISIONAL, script header |
| Ruins of Kel Voreth (1336) | provisional scripts for 3 bosses; Gurka, Blood Pit not scripted | PROVISIONAL |
| Genetic Archives (1462) | provisional scripts for 3 bosses; Prototypes, Convergence, Ohmna missing input | PROVISIONAL, script header |
| Rage Logic (1627) | missing input: Axiom has no creature id, no packet-reported matching ability | Expedition/ProvisionalEncounterSkipped.md |
| Space Madness (2149) | missing input | Expedition/ProvisionalEncounterSkipped.md |
| Gauntlet (2183) | missing input: only deprecated rows | Expedition/ProvisionalEncounterSkipped.md |
| Deep Space Exploration (2188) | missing input | Expedition/ProvisionalEncounterSkipped.md |
| Ultimate Protogames (2980) | no boss script justified (minigames, state-driven fights); missing input | INSTANCE_ULTIMATE_PROTOGAMES_PACKET |
| Vault of the Archon (3009) | missing input: the one boss clip never names its boss, spell ids unreliable | Raid/ProvisionalEncounterSkipped.md |
| Red Moon Terror (3032) | missing input: no readable clip for Engineers or Star-Eater | Raid/ProvisionalEncounterSkipped.md |
| Augmentor's Raid / Initialization Core Y-83 (3040) | script removed by lead; missing input: creature-to-spell binding | Raid/ProvisionalEncounterSkipped.md |
| Ultimate Protogames raid (3041) | missing input: zero footage, text or strings | INSTANCE_ULTIMATE_PROTOGAMES_PACKET |
| BAD (3042) | missing input: placeholder (no event, map entry or script) | INSTANCE_RAIDS_EVENTS_PACKET section 5 |
| Shade's Eve (3044) | missing input: no clip shows an ability | Raid/ProvisionalEncounterSkipped.md |
| Journey into OMNICore-1 (3045) | missing input: no verifiable spell rows | Raid/ProvisionalEncounterSkipped.md |
| Protostar Winterfest (3094) | missing input: no boss (event instance) | Raid/ProvisionalEncounterSkipped.md |
| Red Moon Terror 40-man (3102) | missing input: no footage | INSTANCE_RAIDS_EVENTS_PACKET |
| Protogames Academy (3173) | provisional script for Wrathbone; six other bosses missing input (no creature id in the repo, a guessed binding) | PROVISIONAL, script header |
| Fragment Zero (3180) | missing input | Expedition/ProvisionalEncounterSkipped.md |
| Evil from the Ether (3404) | existing Katja script (55% enrage, 20 s casts) unchanged; no clip agrees or disagrees; other bosses missing input | INSTANCE_EXPEDITIONS_PACKET |
| Coldblood Citadel (3522) | provisional scripts for 4 bosses; Risen Harizog missing input | PROVISIONAL |
| Red Moon Terror (3524) | missing input: no footage | INSTANCE_RAIDS_EVENTS_PACKET |

Reading this table against the stop condition: 8 rows have provisional (not retail) behaviour plus stated missing inputs for what is not modelled, and 19 rows have only the stated missing input. Phase A items still open: invulnerable adds and shield removal, spawn/door/platform restore on a wipe.
