# WildStar Data Mapping Progress

Last updated: 2026-06-17 19:59 Europe/Zurich

## Current Goal

Map every usable relationship from `wildstar_client_mysql` and `jabbithole_mysql` into reviewable outputs, then promote safe subsets into NexusForever world/database import paths.

## Current Run

Command:

```powershell
python Tools\DataMapping\map_wildstar_data.py --output-dir Tools\DataMapping\output
```

Primary output folder:

- `Tools\DataMapping\output`

Generated tracking outputs:

- `mapping_summary.md`
- `run_manifest.json`
- `table_coverage_inventory.csv`
- `table_coverage_inventory.md`
- `vendor_item_apply_report.json`
- `creature_loot_apply_report.json`
- `creature_info_apply_report.json`
- `apply_vendor_items.generated.sql`
- `apply_creature_info_overrides.generated.sql`
- `creature_bridge_review.csv`
- `creature_bridge_override_audit.csv`
- `creature_bridge_review_priority.csv`
- `creature_bridge_review_candidates_ranked.csv`
- `creature_bridge_override_suggestions.csv`
- `creature_bridge_review_priority_report.json`
- `creature_bridge_suggestion_promotion_report.json`
- `mapping_staging_load_report.json`

## Coverage Snapshot

Source table inventory:

| Source | Total SQL files | Mapped | Partial | Unmapped |
|---|---:|---:|---:|---:|
| `wildstar_client_mysql` | 387 | 387 | 0 | 0 |
| `jabbithole_mysql` | 106 | 106 | 0 | 0 |
| **Total** | **493** | **493** | **0** | **0** |

The mapper now covers every split SQL file in both `wildstar_client_mysql` and `jabbithole_mysql`. Curated semantic maps cover the high-value gameplay relationships; the remaining client-only tables are exported through complete `client_source_*_map.csv` files with source columns, localized text expansion, and known foreign-key labels. It emits a file-by-file coverage tracker at:

- `Tools\DataMapping\output\table_coverage_inventory.md`
- `Tools\DataMapping\output\table_coverage_inventory.csv`

Current generated output count: 485 files.

## Review Queue Snapshot

Creature bridge adjudication is now implemented.

- Manual override input: `Tools\DataMapping\review\creature_bridge_overrides.csv`
- Tracked header template: `Tools\DataMapping\creature_bridge_overrides.example.csv`
- Generated review queue: `Tools\DataMapping\output\creature_bridge_review.csv`
- Override audit output: `Tools\DataMapping\output\creature_bridge_override_audit.csv`
- Prioritized review output: `Tools\DataMapping\output\creature_bridge_review_priority.csv`
- Ranked candidate output: `Tools\DataMapping\output\creature_bridge_review_candidates_ranked.csv`
- Suggested override output: `Tools\DataMapping\output\creature_bridge_override_suggestions.csv`

Current review queue:

- `creature_bridge_review.csv`: 31,528 remaining candidate rows
- `creature_bridge_override_audit.csv`: 974 rows
- Current approved reviewed mappings in `creature_map.csv`: 966
- Remaining prioritized sources: 7,045
- Remaining medium-confidence suggestions: 531
- Remaining high-confidence suggestions: 16, none currently tied to public-event creature blockers

Review behavior:

- `ambiguous_name` rows emit exact-name candidates up to `--review-candidate-limit`.
- `unmatched` rows emit fuzzy-name candidates when a plausible client name exists.
- Approved overrides become `match_status = reviewed` on the next mapper run.
- Reviewed rows retain original candidate/status columns in `creature_map.csv`.
- Vendor, loot, and creature-info apply scripts now treat `reviewed` as safe alongside `unique_name` and `scored_name`.
- The first spatial evidence pass promoted high-confidence creature bridge overrides. The 2026-06-17 public-event bridge pass promoted 17 additional high-confidence spatial overrides and left no high-confidence public-event suggestions.
- Current `creature_map.csv` statuses: 13,852 `unique_name`, 2,816 `scored_name`, 966 `reviewed`, 6,068 `ambiguous_name`, 977 `unmatched`.

## Live Database Apply Snapshot

Target database: `nexus_forever_world` on localhost with the `bankai` login.

Applied scripts:

- `apply_vendor_items.py --apply`
- `apply_creature_loot.py --apply`
- `apply_creature_info_overrides.py --apply`
- `load_mapping_staging_tables.py --apply`
- `sql/apply_safe_world_imports_from_staging.sql`

Inserted/updated live world data:

- `entity_vendor`: 180 rows
- `entity_vendor_category`: 180 rows
- `entity_vendor_item`: 7,480 rows
- `creature_loot`: 198,735 rows
- mapped creature `loot_group`: 3,795 rows
- mapped creature `entity_loot`: 3,795 rows
- mapped creature `loot_item`: 198,735 rows
- mapped item-container `loot_group`: 272 rows
- mapped item-container `item_loot`: 272 rows
- mapped item-container `loot_item`: 23,865 rows
- `creature_info_property`: 9,308 rows
- `creature_info_stat`: 1,414 rows
- `nf_map_*`: 96 staging/reference tables loaded with 6,160,158 exact rows

Safety policy used:

- Vendor and loot applies used only `unique_name`, `scored_name`, and reviewed creature bridges by default.
- Creature info override apply used only `unique_name`, `scored_name`, and reviewed creature bridges by default.
- `ambiguous_name` and `unmatched` source rows remain in mapping CSVs for review but were not applied to live runtime tables.
- Duplicate creature template override candidates with conflicting values were skipped instead of guessed: 511 property keys and 14 stat keys.
- Vendor stock was applied only for mapped vendor creatures that already have matching rows in `entity.creature`.
- Vendor and creature-info backups were written under `Tools\DataMapping\output\backups` before live imports.
- `nf_map_*` staging loads are additive development/reference data and must not be consumed by runtime code. Runtime features must first promote reviewed rows into explicit `nexus_forever_world` tables, scripts, or code-owned assets. `nf_map_creature` now includes review/original mapping audit columns.
- Staging load compatibility permits older optional CSVs to omit nullable/defaulted schema columns. The current `nf_map_creature_spline_candidate` load uses the older optional spline CSV and omits expanded classification/runtime columns; regenerate with `--include-spline-candidates` before relying on those fields.
- `Tools\DataMapping\sql\apply_safe_world_imports_from_staging.sql` can now repeat the same safe runtime import directly from loaded `nf_map_*` staging tables. It materialises mapped creature drops and item-container loot bags into the runtime loot tables. `Tools\DataMapping\sql\verify_safe_world_imports.sql` verifies counts and bridge-status distribution.

## Implemented Maps

Creature/entity foundation:

- `creature_map.csv`: 24,679 rows
- `creature_client_metadata_map.csv`: 24,679 rows
- `creature_spawn_map.csv`: 732,694 rows
- `world_entity_candidate.csv`: 732,694 rows
- `world_entity_stats_candidate.csv`: 2,217,390 rows
- `creature_spline_candidate_map.csv`: 188,480 rows

Creature relationships:

- `creature_loot_map.csv`: 522,130 rows
- `vendor_item_map.csv`: 20,650 rows
- `creature_spell_map.csv`: 35,703 rows
- `creature_quest_map.csv`: 13,412 rows
- `creature_path_mission_map.csv`: 2,242 rows
- `creature_public_event_map.csv`: 2,645 rows
- `creature_ai_action_map.csv`: 6,117 rows

Quest expansion:

- `quest_client_map.csv`: 5,194 rows
- `quest_objective_client_map.csv`: 7,108 rows
- `quest_reward_client_map.csv`: 5,415 rows
- `quest_objective_map.csv`: 4,558 rows
- `quest_reward_item_map.csv`: 3,246 rows
- `quest_reward_currency_map.csv`: 2,643 rows
- `quest_reward_reputation_map.csv`: 2,422 rows
- `quest_reward_tradeskill_map.csv`: 307 rows

Public event expansion:

- `public_event_client_map.csv`: 642 rows
- `public_event_objective_client_map.csv`: 3,703 rows
- `public_event_objective_map.csv`: 1,842 rows
- `public_event_mission_map.csv`: 77 rows
- `public_event_zone_map.csv`: 449 rows
- `public_event_depot_client_map.csv`: 36 rows
- `public_event_virtual_item_depot_client_map.csv`: 11 rows

Path and exploration expansion:

- `path_episode_client_map.csv`: 115 rows
- `path_reward_client_map.csv`: 302 rows
- `path_level_client_map.csv`: 120 rows
- `character_title_client_map.csv`: 427 rows
- `player_path_map.csv`: 4 rows
- `path_episode_map.csv`: 93 rows
- `path_episode_mission_map.csv`: 976 rows
- `path_episode_reward_map.csv`: 72 rows
- `path_episode_zone_map.csv`: 176 rows
- `path_unlock_map.csv`: 306 rows
- `path_ability_map.csv`: 35 rows

Spell and class/combat expansion:

- `class_client_map.csv`: 6 rows
- `spell_client_map.csv`: 66,383 rows
- `spell_effect_client_map.csv`: 131,010 rows
- `spell_level_client_map.csv`: 288 rows
- `spell_tier_requirement_client_map.csv`: 9 rows
- `spell_map.csv`: 12,901 rows
- `class_map.csv`: 6 rows
- `class_ability_map.csv`: 1,620 rows
- `class_amp_map.csv`: 522 rows
- `class_unlock_map.csv`: 1,774 rows
- `rune_set_spell_map.csv`: 632 rows

Achievement and contract expansion:

- `achievement_client_map.csv`: 4,943 rows
- `achievement_checklist_client_map.csv`: 6,568 rows
- `achievement_group_client_map.csv`: 9 rows
- `achievement_category_client_map.csv`: 273 rows
- `achievement_subgroup_client_map.csv`: 8 rows
- `achievement_text_client_map.csv`: 55 rows
- `achievement_group_map.csv`: 180 rows
- `achievement_map.csv`: 2,324 rows
- `achievement_title_map.csv`: 162 rows
- `character_achievement_map.csv`: 66 rows
- `contract_map.csv`: 42 rows
- `contract_reward_map.csv`: 736 rows
- `contract_creature_map.csv`: 2,888 rows

Housing/decor/cosmetic expansion:

- `housing_decor_type_client_map.csv`: 64 rows
- `housing_decor_client_map.csv`: 3,248 rows
- `housing_decor_limit_category_client_map.csv`: 4 rows
- `housing_plug_item_client_map.csv`: 514 rows
- `housing_wallpaper_client_map.csv`: 280 rows
- `housing_residence_client_map.csv`: 23 rows
- `housing_build_client_map.csv`: 10 rows
- `housing_resource_client_map.csv`: 6 rows
- `housing_contribution_type_client_map.csv`: 4 rows
- `housing_contribution_info_client_map.csv`: 326 rows
- `housing_property_client_map.csv`: 65 rows
- `housing_neighborhood_client_map.csv`: 13 rows
- `housing_map_client_map.csv`: 7 rows
- `housing_plot_type_client_map.csv`: 27 rows
- `housing_plot_client_map.csv`: 420 rows
- `housing_mannequin_pose_client_map.csv`: 3 rows
- `housing_warplot_boss_token_client_map.csv`: 10 rows
- `housing_warplot_plug_info_client_map.csv`: 112 rows
- `dye_color_ramp_client_map.csv`: 422 rows
- `pet_flair_client_map.csv`: 336 rows
- `housing_decor_type_map.csv`: 61 rows
- `housing_decor_map.csv`: 1,405 rows
- `housing_enhancement_map.csv`: 117 rows
- `housing_sky_map.csv`: 22 rows
- `dye_map.csv`: 190 rows
- `flair_map.csv`: 159 rows
- `mount_map.csv`: 34 rows
- `pet_map.csv`: 93 rows
- `title_map.csv`: 274 rows

Tradeskill/schematic expansion:

- `tradeskill_client_map.csv`: 13 rows
- `tradeskill_schematic_client_map.csv`: 2,867 rows
- `tradeskill_material_client_map.csv`: 300 rows
- `tradeskill_material_category_client_map.csv`: 17 rows
- `tradeskill_talent_tier_client_map.csv`: 92 rows
- `tradeskill_tier_client_map.csv`: 56 rows
- `tradeskill_additive_client_map.csv`: 129 rows
- `tradeskill_catalyst_client_map.csv`: 164 rows
- `tradeskill_catalyst_ordering_client_map.csv`: 2 rows
- `tradeskill_bonus_client_map.csv`: 248 rows
- `tradeskill_achievement_layout_client_map.csv`: 1,609 rows
- `tradeskill_achievement_reward_client_map.csv`: 761 rows
- `schematic_map.csv`: 4,574 rows
- `schematic_material_map.csv`: 12,115 rows
- `schematic_circuit_map.csv`: 2,163 rows
- `schematic_microchip_map.csv`: 951 rows
- `tradeskill_talent_tier_map.csv`: 47 rows
- `tradeskill_talent_map.csv`: 123 rows
- `tradeskill_techtree_group_map.csv`: 113 rows
- `tradeskill_techtree_item_map.csv`: 2,825 rows
- `tradeskill_techtree_schematic_map.csv`: 2,869 rows

World/map/faction/tail expansion:

- `world_zone_client_map.csv`: 3,436 rows
- `unit_property_client_map.csv`: 200 rows
- `faction_client_map.csv`: 1,189 rows
- `faction_relationship_client_map.csv`: 1,964 rows
- `quest_category_client_map.csv`: 53 rows
- `episode_client_map.csv`: 300 rows
- `episode_quest_client_map.csv`: 1,497 rows
- `map_continent_client_map.csv`: 85 rows
- `map_zone_client_map.csv`: 490 rows
- `map_zone_sprite_client_map.csv`: 130 rows
- `map_zone_poi_client_map.csv`: 670 rows
- `map_zone_world_join_client_map.csv`: 489 rows
- `continent_map.csv`: 79 rows
- `mapzone_poi_map.csv`: 659 rows
- `quest_category_map.csv`: 44 rows
- `quest_episode_map.csv`: 229 rows
- `quest_zone_map.csv`: 11,285 rows
- `faction_map.csv`: 58 rows
- `faction_reward_item_map.csv`: 1,550 rows
- `attribute_contribution_map.csv`: 54 rows
- `attribute_milestone_map.csv`: 762 rows
- `item_instance_sigil_map.csv`: 1,308 rows
- `legacy_vendor_item_map.csv`: 20,650 rows
- `item_drop_aggregate_map.csv`: 46,438 rows
- `character_snapshot_map.csv`: 4 rows
- `character_item_map.csv`: 75 rows
- `character_build_ability_map.csv`: 30 rows
- `schema_migration_map.csv`: 214 rows
- `empty_items_split_map.csv`: 1 row
- `unknown_table_loot_map.csv`: 167,756 rows

World/location/navigation client expansion:

- `world_location_client_map.csv`: 33,396 rows
- `bind_point_client_map.csv`: 62 rows
- `taxi_node_client_map.csv`: 261 rows
- `taxi_route_client_map.csv`: 238 rows
- `city_direction_client_map.csv`: 103 rows
- `quest_direction_client_map.csv`: 1,255 rows
- `quest_direction_entry_client_map.csv`: 2,520 rows
- `quest_hub_client_map.csv`: 209 rows
- `generic_map_client_map.csv`: 2 rows
- `generic_map_node_client_map.csv`: 54 rows
- `map_zone_hex_client_map.csv`: 117,452 rows
- `map_zone_hex_group_client_map.csv`: 1,542 rows
- `map_zone_hex_group_entry_client_map.csv`: 46,168 rows
- `map_zone_level_band_client_map.csv`: 0 rows
- `map_zone_nemesis_region_client_map.csv`: 47 rows
- `zone_completion_client_map.csv`: 28 rows
- `sound_zone_kit_client_map.csv`: 1,427 rows
- `world_socket_client_map.csv`: 1,971 rows
- `world_layer_client_map.csv`: 1,117 rows
- `world_clutter_client_map.csv`: 528 rows
- `world_sky_client_map.csv`: 886 rows
- `world_water_environment_client_map.csv`: 1 row
- `world_water_fog_client_map.csv`: 12 rows
- `world_water_layer_client_map.csv`: 84 rows
- `world_water_type_client_map.csv`: 17 rows
- `world_water_wake_client_map.csv`: 10 rows

Complete client source coverage:

- `client_source_map_inventory.csv`: 272 rows
- `client_source_*_map.csv`: 272 files covering every client table that did not already have a curated semantic map, including `Creature2ActionSet`.
- Largest generic exports include localized string tables (`539,251` rows each for `StringsdeDE`, `StringsfrFR`, and `LocalizedText`), visuals/sounds/spell visuals, prerequisites, random text, character customization, and model/item display tables.

Item and reward expansion:

- `item_client_map.csv`: 71,918 rows
- `item_family_client_map.csv`: 36 rows
- `item_category_client_map.csv`: 155 rows
- `item_type_client_map.csv`: 542 rows
- `item_slot_client_map.csv`: 50 rows
- `item_family_map.csv`: 27 rows
- `item_category_map.csv`: 91 rows
- `item_type_map.csv`: 358 rows
- `item_slot_map.csv`: 15 rows
- `item_spell_effect_map.csv`: 6,482 rows
- `item_drop_source_map.csv`: 767,066 rows
- `item_container_map.csv`: 23,865 rows
- `item_salvage_map.csv`: 337,550 rows
- `item_class_requirement_map.csv`: 1,519 rows
- `item_tradeskill_requirement_map.csv`: 757 rows
- `item_chip_spell_map.csv`: 655 rows
- `item_component_map.csv`: 32,902 rows
- `item_instance_attribute_map.csv`: 156,390 rows

Challenge expansion:

- `challenge_client_map.csv`: 643 rows
- `challenge_map.csv`: 406 rows
- `challenge_creature_map.csv`: 5,779 rows
- `challenge_reward_item_map.csv`: 16,536 rows

Other reference maps:

- `path_mission_client_map.csv`: 1,064 rows
- `world_client_map.csv`: 2,729 rows
- `zone_map.csv`: 416 rows

## Known Caveats

- Jabbithole creature IDs are not client `Creature2.ID` values. The current bridge uses localized English names and scores duplicate names by faction, level range, difficulty, and datacube.
- Current creature bridge statuses: 13,852 `unique_name`, 2,816 `scored_name`, 966 `reviewed`, 6,068 `ambiguous_name`, 977 `unmatched`.
- Quest objective bridge statuses: 4,488 `matched`, 70 `unmatched`. The previous order-0 false-unmatched bucket is fixed; remaining unmatched rows are true missing/non-current objective-slot cases or require row review.
- Public event objective bridge statuses: 1,800 `matched`, 36 `event_mismatch`, 3 `unmatched_objective`, 2 `unmatched_event`, 1 `unmatched`.
- Item slot bridge statuses: 9 `matched`, 6 `unmatched`. Some Jabbithole slot IDs use older enum values, such as chest slot `game_id = 0`, that do not directly equal client `ItemSlot.ID`.
- `item_drop_source_map.csv` preserves legacy `item_drops`, `item_drop4_drops`, and `item_drop5_drops` separately from the versioned loot map so later import logic can choose the preferred source.
- Candidate world entity files intentionally leave final entity IDs blank.
- Jabbithole coordinates do not provide rotation, so generated entity candidates use zero rotation.
- Spline mapping is proximity-only; there is no direct creature-to-spline foreign key in the currently mapped source.
- Challenge reward-track item IDs are emitted as `item2_id` when the ID exists in `Item2`; otherwise the raw source ID is still preserved for later investigation.
- Spell bridge statuses: 12,734 `matched`, 167 `unmatched`. Unmatched Jabbithole spell rows generally have missing or non-client `game_id` values and are still preserved with raw metadata.
- Class unlock statuses: 324 `matched`, 1,450 `non_spell_reward`. Non-spell unlocks are LAS slots, zones, features, or other raw unlock reward types rather than `Spell4` rows.
- Achievement title bridge statuses: 159 `matched`, 3 `unmatched`.
- Housing/decor/cosmetic bridge statuses: decor types 61 `matched`; decor 1,403 `matched`, 2 `unmatched`; enhancements 117 `matched`; skies 22 `matched`; dyes 190 `matched`; flairs 159 `matched`; mounts 34 `matched`; pets 93 `matched`; titles 271 `matched`, 3 `unmatched`.
- Tradeskill bridge statuses: schematics 2,669 `matched`, 1,905 `unmatched`; schematic materials 292 `matched`, 7,741 `partial`, 4,082 `unmatched`; schematic circuits 39 `matched`, 339 `partial`, 1,785 `unmatched`; schematic microchips 8 `matched`, 254 `partial`, 689 `unmatched`; talent tiers 31 `matched`, 16 `unmatched`; talents 123 `matched`; tech-tree groups 113 `matched`; tech-tree items 2,825 `matched`; tech-tree schematic unlocks 1,365 `matched`, 1,504 `partial`.
- World/map/faction tail statuses: continents 78 `matched`, 1 `unmatched`; map POIs 659 `matched`; quest categories 44 `matched`; quest episodes 221 `matched`, 8 `unmatched`; quest zones 6,167 `matched`, 5,079 `partial`, 39 `unmatched`; factions 47 `name_matched`, 10 `ambiguous_name`, 1 `unmatched`; faction reward items 1,550 `matched`.
- Attribute tail statuses: contributions 44 `matched`, 10 `partial`; milestones 642 `matched`, 120 `partial`; item instance sigils 81 `matched`, 1,227 `unmatched`.
- Legacy/raw loot tail statuses: legacy vendor rows use creature bridge statuses 16,042 `unique_name`, 3,552 `ambiguous_name`, 703 `scored_name`, 353 `unmatched`; item drop aggregates 46,438 `matched`; unknown-table loot rows 105,116 `unique_name` plus item matched, 40,749 `scored_name` plus item matched, 20,735 `ambiguous_name` plus item matched, 1,156 creature-unmatched plus item matched.
- Character snapshot statuses: 4 character snapshots `matched`; 75 equipped items `matched`; 29 build abilities `matched`, 1 `unmatched`.
- Contract creature rows use the same creature name bridge as other creature relations: 1,951 `unique_name`, 485 `scored_name`, 373 `ambiguous_name`, 41 `reviewed`, 38 `unmatched`.
- Generic `client_source_*_map.csv` files are complete source exports plus best-effort labels. Columns such as `objectId*`, enum payloads, and table-specific polymorphic IDs still need table-family-specific interpretation before they become safe import data.

## Completed Phases

- Phase 1: Split dump parser for client `.tbl.sql` and Jabbithole schema validation.
- Phase 2: Creature name bridge from Jabbithole names to `Creature2.localizedTextIdName`.
- Phase 3: Creature spawns, entity candidates, and entity stat candidates.
- Phase 4: Creature loot, vendors, spells, quests, path missions, public events, AI actions, and spline proximity candidates.
- Phase 5: Generated all-table coverage inventory.
- Phase 6: Challenge definitions, challenge creatures, and challenge reward items.
- Phase 7: Quest objectives and quest item/currency/reputation/tradeskill rewards.
- Phase 8: Public event objectives, missions, zones, and client depot item relations.
- Phase 9: Item taxonomy, effects, drops, containers, salvage, requirements, components, and stat attributes.
- Phase 10: Path episodes, path unlocks, path episode rewards/zones/missions, and path abilities.
- Phase 11: Spell/class combat maps, including `Spell4`, spell effects, class abilities, AMPs, class unlocks, and rune set spells.
- Phase 12: Achievements and contracts, including achievement title rewards and contract creature/reward relations.
- Phase 13: Housing, decor, dyes, flairs, mounts, pets, and titles.
- Phase 14: Tradeskill schematics, schematic materials/circuits/microchips, talent tiers/talents, and tech-tree achievement unlocks.
- Phase 15: Jabbithole tail coverage: continents, map POIs, quest categories/episodes/zones, factions/reward items, attributes/milestones, legacy vendors, drop aggregates, character snapshots, schema metadata, empty split metadata, and the unknown-table loot scrape.
- Phase 16: Client world/location/navigation coverage: `WorldLocation2`, bind points, taxi nodes/routes, city/quest direction rows, quest hubs, generic maps, map hexes/groups, zone completion, sound zone kits, world sockets/layers/clutter/sky, and water environment tables.
- Phase 17: Complete client source coverage for every previously unmapped or partial client table via `client_source_*_map.csv` plus `client_source_map_inventory.csv`.
- Phase 18: Live world database apply phase started: imported mapped vendor stock into existing vendor tables and imported safe de-duplicated creature loot into a new `creature_loot` table.
- Phase 19: Extended safe real DB imports: imported creature template health/shield/interrupt armor overrides, loaded all curated `nf_map_*` staging/reference tables into `nexus_forever_world`, and wired DB entity initialisation to apply creature template overrides before entity-specific rows.
- Phase 20: Creature bridge adjudication loop: generated candidate review queue, added manual override input, preserved original match audit columns, and made `reviewed` mappings eligible for safe apply scripts.
- Phase 21: Unblocked high-confidence uncertain creature bridges with spatial live-entity evidence, promoted 944 reviewed overrides, hardened staging-table reloads, and reapplied all safe real DB imports with reviewed mappings included.
- Phase 22: Added migration-style SQL import scripts that load safe vendor, loot, and creature-info data directly from `nf_map_*` staging tables, plus a verification SQL script for row counts and bridge-status checks.
- Phase 23: Materialised safe mapped `creature_loot` rows into flat runtime `loot_group`, `entity_loot`, and `loot_item` rows, verified 3,795 mapped creature groups and 198,735 mapped loot items on localhost, and wired the server to prefer those old-table rows without duplicating the direct `creature_loot` path.
- Phase 24: Extended runtime item loot import by materialising `item_container_map.csv` into weighted `item_loot` groups for loot bags: 272 mapped container groups and 23,865 mapped contained-item rows on localhost.

## Next Phases

1. Semantic refinement for generic client clusters:
   - Archive/account/customization, prerequisites, visuals, sound, UI, tutorials, path side tables, random text, model/item display, and reward rotation tables now have complete generic exports. Promote any cluster that needs gameplay import behavior into curated maps with table-specific bridge logic.

2. Import hardening:
   - Keep broadening runtime loot verification, especially grouped players, master loot assignment, roll timeouts, and aggregate-derived stack-count policy.
   - Promote selected `nf_map_*` staging families into server-owned runtime tables/assets only after the table has direct semantics and conflict policy. Good next candidates are creature spell/action inspection, quest/public-event/path read models, and reviewed spline candidates.
   - Review the 527 remaining medium-confidence creature bridge suggestions and polymorphic client `objectId*` columns before broadening apply scripts beyond safe `unique_name`/`scored_name`/`reviewed` rows.

## Implementation Notes

- Keep generated maps reviewable and non-destructive until ambiguous bridge rows have been resolved.
- Prefer adding table families to `map_wildstar_data.py` with clear CSV outputs and matching `nf_map_*` staging tables in `schema.sql`.
- Update this file after every mapping family expansion.
