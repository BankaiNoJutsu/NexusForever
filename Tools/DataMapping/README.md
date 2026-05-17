# WildStar Client and Jabbithole Mapping

This folder contains the first repeatable pass for mapping NexusForever world data from the split dumps in:

- `wildstar_client_mysql`
- `jabbithole_mysql`

The mapper uses the split client dump as the authoritative source for stable client/game IDs and localized English names. It uses the local `jabbithole` MySQL database for fast relationship queries, with the split Jabbithole folder validated up front so the run is tied to the dump files the repository carries.

Current coverage is complete at the split-file level: all 387 `wildstar_client_mysql` files and all 106 `jabbithole_mysql` files are represented in the generated output. Curated maps cover the known gameplay relationships, and `client_source_*_map.csv` files cover remaining client-only reference/system tables.

## Quick Run

From the repository root:

```powershell
python Tools\DataMapping\map_wildstar_data.py
```

The default connection is:

- Host: `127.0.0.1`
- User: `bankai`
- Password: `bankai`
- Database: `jabbithole`
- MySQL client: `C:\Program Files\MySQL\MySQL Server 8.0\bin\mysql.exe`

For a quick smoke test:

```powershell
python Tools\DataMapping\map_wildstar_data.py `
  --limit-creatures 200 `
  --limit-relation-rows 500 `
  --limit-spawns 500 `
  --output-dir Tools\DataMapping\output_test
```

For the full run with proximity-based spline candidates:

```powershell
python Tools\DataMapping\map_wildstar_data.py --include-spline-candidates
```

## Generated Maps

The default output directory is `Tools\DataMapping\output`.

Core bridge:

- `creature_map.csv` maps Jabbithole creature IDs to client `Creature2.ID`.
- `creature_client_metadata_map.csv` adds action set, display group, outfit group, faction, path, vehicle, taxi, bind, and harvest metadata.

Spawn and world import candidates:

- `creature_spawn_map.csv` maps observed Jabbithole creature coordinates to client creature IDs and NexusForever entity type IDs.
- `world_entity_candidate.csv` projects those spawns into the active NexusForever `entity` table shape without assigning final entity IDs.
- `world_entity_stats_candidate.csv` projects health, level, shield, and interrupt armor into the active `entity_stats` table shape, also without final entity IDs.

Creature relationships:

- `creature_loot_map.csv` maps creatures to `Item2` drops from `versioned_item_drops`.
- `vendor_item_map.csv` maps vendor creatures to `Item2` vendor stock.
- `creature_spell_map.csv` maps creature spell observations to client `Spell4` IDs when Jabbithole has that bridge.
- `creature_quest_map.csv` maps starter, finisher, and objective quest creature relationships to `Quest2`.
- `quest_objective_map.csv` maps Jabbithole quest objective text/order to client `QuestObjective` rows through the `Quest2` objective slots.
- `quest_reward_item_map.csv` maps quest item rewards to `Item2`.
- `quest_reward_currency_map.csv` maps quest currency rewards to `CurrencyType`.
- `quest_reward_reputation_map.csv` maps quest reputation rewards to `Faction2` where possible.
- `quest_reward_tradeskill_map.csv` maps quest tradeskill rewards to `Tradeskill`.
- `creature_path_mission_map.csv` maps path mission creature relationships to `PathMission`.
- `creature_public_event_map.csv` maps public event creature relationships to `PublicEvent`.
- `public_event_objective_map.csv` maps Jabbithole public event objectives to client `PublicEventObjective` rows.
- `public_event_mission_map.csv` maps Jabbithole public event mission choices to parent and child `PublicEvent` IDs.
- `public_event_zone_map.csv` maps Jabbithole public event zone rows to client `WorldZone`.
- `creature_ai_action_map.csv` maps resolved `Creature2` rows to `Creature2Action` rows through `creature2ActionSetId`.
- `challenge_map.csv` maps Jabbithole challenges to client `Challenge`.
- `challenge_creature_map.csv` maps challenge target creatures to resolved `Creature2` rows.
- `challenge_reward_item_map.csv` maps direct challenge rewards and challenge reward-track item contents to `Item2` when a client item ID is available.
- `creature_spline_candidate_map.csv` is optional and maps spawns to nearby `Spline2` starting nodes by proximity. This is candidate-only because no direct Jabbithole creature-to-spline foreign key exists.

Path and exploration relationships:

- `player_path_map.csv` bridges Jabbithole player path rows to path type IDs.
- `path_episode_map.csv` maps Jabbithole path episodes to client `PathEpisode`.
- `path_episode_mission_map.csv` maps path episodes to `PathMission` rows.
- `path_episode_reward_map.csv` maps path episode item rewards to `Item2`.
- `path_episode_zone_map.csv` maps path episodes to `WorldZone`.
- `path_unlock_map.csv` maps path-level unlock rewards to items, spells, and character titles where possible.
- `path_ability_map.csv` maps path ability tiers and costs to client `Spell4`.

Spell and class/combat relationships:

- `spell_map.csv` bridges Jabbithole spell rows to client `Spell4` and base spells.
- `class_map.csv` bridges Jabbithole player classes to client `Class` rows and innate ability spells.
- `class_ability_map.csv` maps class ability tiers, levels, costs, and tier bonuses to client `Spell4`.
- `class_amp_map.csv` maps AMP tree nodes to classes, spells, parent AMPs, and item rewards where present.
- `class_unlock_map.csv` maps class level unlock rewards to class and spell references where the unlock is spell-backed.
- `rune_set_spell_map.csv` maps rune set item relations to client `Spell4`.

Achievement and contract relationships:

- `achievement_group_map.csv` maps Jabbithole achievement group hierarchy rows to client `AchievementCategory` where the game ID aligns.
- `achievement_map.csv` bridges Jabbithole achievement rows to client `Achievement`, category, subgroup, zone, and title references.
- `achievement_title_map.csv` maps achievement title rewards to client `CharacterTitle`.
- `character_achievement_map.csv` maps observed character achievement unlock rows to client `Achievement`.
- `contract_map.csv` maps contracts to their backing `Quest2` rows.
- `contract_reward_map.csv` maps contract reward items to `Item2`.
- `contract_creature_map.csv` maps contract quest creature targets through the creature bridge.

Housing, decor, and cosmetic relationships:

- `housing_decor_type_map.csv` maps Jabbithole decor types to client `HousingDecorType`.
- `housing_decor_map.csv` maps Jabbithole decor rows to client `HousingDecorInfo`, decor type, spell, item, and active prop creature references.
- `housing_enhancement_map.csv` maps housing enhancements to client `HousingPlugItem`, world, and upgrade references.
- `housing_sky_map.csv` maps housing sky rows to client `HousingWallpaperInfo`.
- `dye_map.csv` maps Jabbithole dye rows to client `DyeColorRamp`.
- `flair_map.csv` maps Jabbithole flair rows to client `PetFlair` and spell references.
- `mount_map.csv` maps mounts to base/summon spells, preview creatures, and hoverboard preview items.
- `pet_map.csv` maps pets to base/summon spells and preview creatures.
- `title_map.csv` maps Jabbithole titles to client `CharacterTitle`.

Tradeskill and schematic relationships:

- `schematic_map.csv` maps Jabbithole schematics to client `TradeskillSchematic2`, output items, parent schematics, and taught-by items where current IDs exist.
- `schematic_material_map.csv`, `schematic_circuit_map.csv`, and `schematic_microchip_map.csv` map schematic component rows to client schematics and `Item2` where possible.
- `tradeskill_talent_tier_map.csv` maps Jabbithole talent tier rows to client `TradeskillTalentTier`.
- `tradeskill_talent_map.csv` maps Jabbithole talents to client `TradeskillBonus`.
- `tradeskill_techtree_group_map.csv` maps tradeskill tech-tree groups to client `AchievementCategory`.
- `tradeskill_techtree_item_map.csv` maps tech-tree item rows to client `Achievement`.
- `tradeskill_techtree_schematic_map.csv` maps tech-tree achievement rows to schematic unlocks.

World, map, faction, and tail relationships:

- `continent_map.csv` maps Jabbithole continents to client `MapContinent`.
- `mapzone_poi_map.csv` maps Jabbithole map POIs to client `MapZonePOI` by zone, position, and normalized name, with sprite/icon matching through `MapZoneSprite`.
- `quest_category_map.csv` maps Jabbithole quest categories to client `QuestCategory`.
- `quest_episode_map.csv` maps Jabbithole quest episodes to client `Episode`.
- `quest_zone_map.csv` maps `quest_zones` and `quest_call_zones` to `Quest2` and `WorldZone`.
- `faction_map.csv` maps Jabbithole factions to client `Faction2` by direct ID where present, otherwise by normalized localized name.
- `faction_reward_item_map.csv` maps faction reward rows to `Faction2` and `Item2`.
- `attribute_contribution_map.csv` and `attribute_milestone_map.csv` map stat contribution/milestone rows through Jabbithole attributes to client `UnitProperty2` and `Class`.
- `item_instance_sigil_map.csv` maps item instance sigil slots to `Item2` where the source item ID is a current client item.
- `legacy_vendor_item_map.csv` preserves legacy `vendor_items` rows separately from the versioned vendor map, with creature and item bridges.
- `item_drop_aggregate_map.csv` maps versioned item-drop aggregate rows to `Item2`.
- `character_snapshot_map.csv`, `character_item_map.csv`, and `character_build_ability_map.csv` map the local character snapshot table to classes, paths, factions, equipment items, and build spells.
- `schema_migration_map.csv` and `empty_items_split_map.csv` preserve metadata-only Jabbithole split files.
- `unknown_table_loot_map.csv` parses the large unknown-table loot scrape from `.sql` and maps item/creature references where possible.

Item and reward relationships:

- `item_family_map.csv`, `item_category_map.csv`, `item_type_map.csv`, and `item_slot_map.csv` bridge Jabbithole item taxonomy rows to client item taxonomy tables.
- `item_spell_effect_map.csv` maps item effects and imbuements to Jabbithole spells and client `Spell4` IDs where available.
- `item_drop_source_map.csv` maps legacy `item_drops`, `item_drop4_drops`, and `item_drop5_drops` rows to `Item2` and resolved creatures.
- `item_container_map.csv` maps container items to contained `Item2` rows.
- `item_salvage_map.csv` maps salvaged item outputs.
- `item_class_requirement_map.csv` and `item_tradeskill_requirement_map.csv` map item use requirements.
- `item_chip_spell_map.csv` maps item chip spell relations.
- `item_component_map.csv` maps circuit and microchip component item relations.
- `item_instance_attribute_map.csv` maps item attribute bonuses to client `UnitProperty2`.

Client reference maps:

- `item_client_map.csv`
- `item_family_client_map.csv`
- `item_category_client_map.csv`
- `item_type_client_map.csv`
- `item_slot_client_map.csv`
- `class_client_map.csv`
- `spell_client_map.csv`
- `spell_effect_client_map.csv`
- `spell_level_client_map.csv`
- `spell_tier_requirement_client_map.csv`
- `achievement_client_map.csv`
- `achievement_checklist_client_map.csv`
- `achievement_group_client_map.csv`
- `achievement_category_client_map.csv`
- `achievement_subgroup_client_map.csv`
- `achievement_text_client_map.csv`
- `quest_client_map.csv`
- `quest_objective_client_map.csv`
- `quest_reward_client_map.csv`
- `path_mission_client_map.csv`
- `path_episode_client_map.csv`
- `path_reward_client_map.csv`
- `path_level_client_map.csv`
- `character_title_client_map.csv`
- `housing_decor_type_client_map.csv`
- `housing_decor_client_map.csv`
- `housing_decor_limit_category_client_map.csv`
- `housing_plug_item_client_map.csv`
- `housing_wallpaper_client_map.csv`
- `housing_residence_client_map.csv`
- `housing_build_client_map.csv`
- `housing_resource_client_map.csv`
- `housing_contribution_type_client_map.csv`
- `housing_contribution_info_client_map.csv`
- `housing_property_client_map.csv`
- `housing_neighborhood_client_map.csv`
- `housing_map_client_map.csv`
- `housing_plot_type_client_map.csv`
- `housing_plot_client_map.csv`
- `housing_mannequin_pose_client_map.csv`
- `housing_warplot_boss_token_client_map.csv`
- `housing_warplot_plug_info_client_map.csv`
- `dye_color_ramp_client_map.csv`
- `pet_flair_client_map.csv`
- `tradeskill_client_map.csv`
- `tradeskill_schematic_client_map.csv`
- `tradeskill_material_client_map.csv`
- `tradeskill_material_category_client_map.csv`
- `tradeskill_talent_tier_client_map.csv`
- `tradeskill_tier_client_map.csv`
- `tradeskill_additive_client_map.csv`
- `tradeskill_catalyst_client_map.csv`
- `tradeskill_catalyst_ordering_client_map.csv`
- `tradeskill_bonus_client_map.csv`
- `tradeskill_achievement_layout_client_map.csv`
- `tradeskill_achievement_reward_client_map.csv`
- `unit_property_client_map.csv`
- `faction_client_map.csv`
- `faction_relationship_client_map.csv`
- `quest_category_client_map.csv`
- `episode_client_map.csv`
- `episode_quest_client_map.csv`
- `public_event_client_map.csv`
- `public_event_objective_client_map.csv`
- `public_event_depot_client_map.csv`
- `public_event_virtual_item_depot_client_map.csv`
- `challenge_client_map.csv`
- `world_client_map.csv`
- `world_zone_client_map.csv`
- `map_continent_client_map.csv`
- `map_zone_client_map.csv`
- `map_zone_sprite_client_map.csv`
- `map_zone_poi_client_map.csv`
- `map_zone_world_join_client_map.csv`
- `world_location_client_map.csv`
- `bind_point_client_map.csv`
- `taxi_node_client_map.csv`
- `taxi_route_client_map.csv`
- `city_direction_client_map.csv`
- `quest_direction_client_map.csv`
- `quest_direction_entry_client_map.csv`
- `quest_hub_client_map.csv`
- `generic_map_client_map.csv`
- `generic_map_node_client_map.csv`
- `map_zone_hex_client_map.csv`
- `map_zone_hex_group_client_map.csv`
- `map_zone_hex_group_entry_client_map.csv`
- `map_zone_level_band_client_map.csv`
- `map_zone_nemesis_region_client_map.csv`
- `zone_completion_client_map.csv`
- `sound_zone_kit_client_map.csv`
- `world_socket_client_map.csv`
- `world_layer_client_map.csv`
- `world_clutter_client_map.csv`
- `world_sky_client_map.csv`
- `world_water_environment_client_map.csv`
- `world_water_fog_client_map.csv`
- `world_water_layer_client_map.csv`
- `world_water_type_client_map.csv`
- `world_water_wake_client_map.csv`
- `zone_map.csv`

Complete client source maps:

- `client_source_map_inventory.csv` lists every generic client-source export and row count.
- `client_source_*_map.csv` files cover each client table that does not already have a curated semantic CSV. These preserve all source columns and add `localizedTextId*` text columns plus best-effort labels for known foreign keys such as creatures, items, spells, quests, worlds, zones, factions, classes, achievements, tradeskills, public events, housing, maps, and taxis.

Run metadata:

- `mapping_summary.md`
- `run_manifest.json`
- `table_coverage_inventory.csv`
- `table_coverage_inventory.md`

## Creature ID Bridge

Jabbithole creature IDs are not client `Creature2.ID` values. The mapper bridges them with:

1. Jabbithole `creatures.name`
2. `wildstar_client_mysql\en-US.bin.sql` text
3. `Creature2.localizedTextIdName`

When several client creatures have the same localized name, candidates are scored by exact text, faction, level overlap, exact level range, difficulty, and datacube. The `match_status` column explains the result:

- `unique_name`: exactly one client creature had that normalized name.
- `scored_name`: multiple client creatures had that normalized name, and one scored higher.
- `ambiguous_name`: multiple client creatures tied for best score.
- `unmatched`: no client creature name matched.

Ambiguous rows are still emitted with the best deterministic candidate so the maps remain complete, but they should be reviewed before destructive imports.

## Reviewing Uncertain Creature Mappings

The mapper now generates a review queue for uncertain creature bridge rows:

- `Tools\DataMapping\output\creature_bridge_review.csv`
- `Tools\DataMapping\output\creature_bridge_override_audit.csv`

`creature_bridge_review.csv` contains one row per candidate for every `ambiguous_name` creature, plus fuzzy-name candidates for `unmatched` creatures when a plausible name exists. It includes the source creature, current selected candidate, candidate rank, score delta, name similarity, faction match, level overlap, exact level match, difficulty match, datacube match, and blank review columns.

Approved decisions live outside the generated output folder so remaps do not overwrite them:

- `Tools\DataMapping\review\creature_bridge_overrides.csv`

The mapper creates that file with the required header when it is missing. `Tools\DataMapping\creature_bridge_overrides.example.csv` is the tracked header template. To approve a mapping, add a row with:

- `source_table`: `creatures`
- `source_id`: Jabbithole creature ID
- `chosen_creature2_id`: approved client `Creature2.ID`
- `decision`: `approved`
- `reason`, `reviewer`, `reviewed_at`: short audit trail

On the next mapper run, approved rows become `match_status = reviewed`, keep their original candidate/status in the `original_*` columns, and flow into downstream creature relationship maps. The live DB apply scripts treat `reviewed` the same as `unique_name` and `scored_name`.

Two helper scripts make the remaining queue reviewable in smaller slices:

```powershell
python Tools\DataMapping\prioritize_creature_bridge_reviews.py
python Tools\DataMapping\promote_creature_bridge_suggestions.py
python Tools\DataMapping\promote_creature_bridge_suggestions.py --apply
```

`prioritize_creature_bridge_reviews.py` ranks uncertain bridge rows by downstream impact and optional existing-world spatial evidence. `promote_creature_bridge_suggestions.py` conservatively promotes only high-confidence spatial suggestions by default. The current localhost review pass promoted 944 approved overrides; 527 medium-confidence suggestions remain for manual review.

## Loading Into MySQL

`schema.sql` creates staging tables named `nf_map_*`. They are intended for review and import workflows, not as the final NexusForever runtime schema.

Preferred repeatable loader:

```powershell
python Tools\DataMapping\load_mapping_staging_tables.py
python Tools\DataMapping\load_mapping_staging_tables.py --apply
```

`load_mapping_staging_tables.py` creates/replaces each owned `nf_map_*` table in `nexus_forever_world` and loads every curated staging CSV covered by `schema.sql`. It uses `LOAD DATA LOCAL INFILE`, temporarily enables the local MySQL server setting when the login can do so, and restores it afterward by default. The current localhost apply loaded 95 `nf_map_*` tables and 6,383,288 exact rows.

Once staging is loaded, the safe runtime data can be imported with a single SQL script:

```powershell
Get-Content -Raw Tools\DataMapping\sql\apply_safe_world_imports_from_staging.sql |
  & "C:\Program Files\MySQL\MySQL Server 8.0\bin\mysql.exe" `
  --host=127.0.0.1 --user=bankai --password=bankai nexus_forever_world
```

That migration-style script imports vendor stock, creature loot, flat runtime creature loot groups, weighted item-container loot groups, and creature-info template overrides from `nf_map_*` into explicit `nexus_forever_world` runtime tables. Runtime code must consume those promoted tables only, never the staging/source databases. Creature-backed imports use only `unique_name`, `scored_name`, and `reviewed` creature bridges. `Tools\DataMapping\sql\verify_safe_world_imports.sql` prints the expected row counts after import.

Manual schema load example:

```powershell
& "C:\Program Files\MySQL\MySQL Server 8.0\bin\mysql.exe" `
  --host=127.0.0.1 --user=bankai --password=bankai nexus_forever_world `
  < Tools\DataMapping\schema.sql
```

Then load CSVs with `LOAD DATA LOCAL INFILE` or your preferred import tool. Keep `world_entity_candidate.csv` in staging until entity IDs have been assigned safely for the target world database. Some raw client sentinel values use the full uint32 range; the preferred loader widens bare staging `INT` columns to `BIGINT` during table creation for that reason.

## Applying To The World DB

The first real database apply scripts are intentionally narrow and repeatable:

```powershell
python Tools\DataMapping\apply_vendor_items.py
python Tools\DataMapping\apply_vendor_items.py --apply
```

`apply_vendor_items.py` imports safe vendor rows from `vendor_item_map.csv` into `nexus_forever_world.entity_vendor`, `entity_vendor_category`, and `entity_vendor_item`. By default it only uses `unique_name`, `scored_name`, and `reviewed` creature bridges, joins to existing `entity.creature` rows, and leaves ambiguous/unmatched vendor bridges out of the live DB. The current localhost tables contain 180 vendor entities, 180 default categories, and 7,480 vendor item rows.

```powershell
python Tools\DataMapping\apply_creature_loot.py
python Tools\DataMapping\apply_creature_loot.py --apply
```

`apply_creature_loot.py` creates/upserts `nexus_forever_world.creature_loot` from safe, de-duplicated `creature_loot_map.csv` rows. The current localhost apply upserted 198,735 creature-item loot rows for 3,795 creatures and 18,504 items. Runtime loot generation is now wired through `GlobalLootManager`; the SQL staging import also mirrors those rows into flat `loot_group`, `entity_loot`, and `loot_item` rows so the older loot-table path is populated too. The same SQL import now maps `item_container_map.csv` into `item_loot` groups for loot bags: 272 container items and 23,865 weighted contained-item rows on localhost.

```powershell
python Tools\DataMapping\apply_creature_info_overrides.py
python Tools\DataMapping\apply_creature_info_overrides.py --apply
```

`apply_creature_info_overrides.py` imports high-confidence creature template overrides from `creature_map.csv` into `creature_info_property` and `creature_info_stat`. By default it inserts only missing `BaseHealth`, `ShieldCapacityMax`, and `InterruptArmour` rows for `unique_name`/`scored_name`/`reviewed` creature bridges, and skips duplicate creature mappings with conflicting values. The current localhost SQL staging import contains 9,308 property overrides and 1,414 stat overrides after skipping 511 conflicting property keys and 14 conflicting stat keys.

## Current Limitations

- Rotation is not present in Jabbithole coordinates, so entity candidate rotations default to zero.
- Final entity IDs are intentionally blank in candidate files to avoid colliding with existing world content.
- Display and outfit IDs are selected from the first/highest-weight client display and outfit group entries.
- AI actions skip `creature2ActionSetId = 0` because the client has no matching `Creature2ActionSet` row for ID 0.
- Spline mapping is spatial only and disabled by default.
- Vendor import is live for existing world entities only; mapped vendor creatures without an `entity` row are reported but not inserted.
- Loot import is database-live and runtime-supported for mapped creature drops and mapped item-container loot bags. Generated creature loot rows default to one item per successful roll until aggregate stack semantics are reviewed further; generated item-container groups roll one weighted item per bag use.
- Creature info overrides are runtime-supported creature template data. Existing entity-specific stats/properties still take precedence over template overrides.
- `nf_map_*` staging imports are development/reference data only. Runtime code must not query them; feature work must promote reviewed rows into explicit runtime tables, scripts, or code-owned assets first.
- Quest, path mission, and public event maps preserve Jabbithole game version and source IDs so later import logic can choose version policy explicitly.
