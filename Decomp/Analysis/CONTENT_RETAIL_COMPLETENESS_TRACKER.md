# Content Retail Completeness Tracker

Generated: 2026-06-27

This is the row-level tracker for the build 16042 quests, dungeons, raids, and related scripted instance content used by the NexusForever retail-completeness goal.
It records evidence and status; it does not claim retail completion unless a row has implementation, automated verification, and manual/client validation where automation cannot prove gameplay.

## Generated Inventories

- Quest rows: `Decomp\Analysis\coverage\content_retail_completeness_quests.csv`
- Quest objective rows: `Decomp\Analysis\coverage\content_retail_completeness_quest_objectives.csv`
- Quest reward/dependency rows: `Decomp\Analysis\coverage\content_retail_completeness_quest_rewards.csv`
- Quest prerequisite rows: `Decomp\Analysis\coverage\content_retail_completeness_quest_prerequisites.csv`
- Quest loot rows: `Decomp\Analysis\coverage\content_retail_completeness_quest_loot.csv`
- Quest world/interaction dependency rows: `Decomp\Analysis\coverage\content_retail_completeness_quest_world_dependencies.csv`
- Quest achievement/progression dependency rows: `Decomp\Analysis\coverage\content_retail_completeness_quest_achievements.csv`
- Quest script hook rows: `Decomp\Analysis\coverage\content_retail_completeness_quest_scripts.csv`
- Quest creature/NPC relationship rows: `Decomp\Analysis\coverage\content_retail_completeness_quest_creatures.csv`
- Quest Jabbithole/DataMapping objective evidence rows: `Decomp\Analysis\coverage\content_retail_completeness_quest_objective_evidence.csv`
- Quest Jabbithole/DataMapping reward evidence rows: `Decomp\Analysis\coverage\content_retail_completeness_quest_reward_evidence.csv`
- Quest Jabbithole/DataMapping zone evidence rows: `Decomp\Analysis\coverage\content_retail_completeness_quest_zone_evidence.csv`
- Quest DataMapping episode evidence rows: `Decomp\Analysis\coverage\content_retail_completeness_quest_episode_evidence.csv`
- Public event DataMapping evidence rows: `Decomp\Analysis\coverage\content_retail_completeness_public_event_evidence.csv`
- Public event auxiliary evidence rows: `Decomp\Analysis\coverage\content_retail_completeness_public_event_auxiliary_evidence.csv`
- Challenge DataMapping evidence rows: `Decomp\Analysis\coverage\content_retail_completeness_challenge_evidence.csv`
- Path mission DataMapping evidence rows: `Decomp\Analysis\coverage\content_retail_completeness_path_mission_evidence.csv`
- Contract DataMapping evidence rows: `Decomp\Analysis\coverage\content_retail_completeness_contract_evidence.csv`
- Instance portal evidence rows: `Decomp\Analysis\coverage\content_retail_completeness_instance_portal_evidence.csv`
- Script communicator/cinematic presentation evidence rows: `Decomp\Analysis\coverage\content_retail_completeness_script_presentation_evidence.csv`
- Script public-event flow evidence rows: `Decomp\Analysis\coverage\content_retail_completeness_script_event_flow_evidence.csv`
- Script objective producer evidence rows: `Decomp\Analysis\coverage\content_retail_completeness_script_objective_producer_evidence.csv`
- Script quest progression evidence rows: `Decomp\Analysis\coverage\content_retail_completeness_script_quest_progression_evidence.csv`
- Script runtime action evidence rows: `Decomp\Analysis\coverage\content_retail_completeness_script_runtime_action_evidence.csv`
- Instance script handler evidence rows: `Decomp\Analysis\coverage\content_retail_completeness_instance_script_handler_evidence.csv`
- Dungeon/raid/instance rows: `Decomp\Analysis\coverage\content_retail_completeness_instances.csv`
- Dungeon/raid/instance dependency rows: `Decomp\Analysis\coverage\content_retail_completeness_instance_dependencies.csv`
- Dungeon/raid/instance entity and encounter rows: `Decomp\Analysis\coverage\content_retail_completeness_instance_entities.csv`
- Dungeon/raid/instance reward rows: `Decomp\Analysis\coverage\content_retail_completeness_instance_rewards.csv`
- Next restoration slice queue rows: `Decomp\Analysis\coverage\content_retail_completeness_next_slice_queue.csv`
- Next restoration slice blocker detail rows: `Decomp\Analysis\coverage\content_retail_completeness_next_slice_blockers.csv`

## Sources

- Build 16042 client SQL: `wildstar_client_mysql` (`Quest2`, `QuestObjective`, `Quest2Reward`, `QuestDirection`, `QuestDirectionEntry`, `TargetGroup`, `WorldLocation2`, `WorldZone`, `World`, `Spell4`, `PublicEvent`, `MatchingGameMap`, `MatchingGameType`, `Challenge`, `PeriodicQuest*`, `InstancePortal`, `Achievement`, `AchievementChecklist`, and localized text).
- Runtime implementation: `Source/NexusForever.Script.Main`, `Source/NexusForever.Script.Instance`, quest/public-event/path/loot/achievement runtime managers, and focused tests.
- Data overlays and generated mapping evidence: reviewed DataMapping SQL, especially `laughingws_quest_loot_seed.sql` and `laughingws_instance_entity_wip_seed.sql`, plus `Tools/DataMapping/output/creature_quest_map.csv`, `quest_objective_map.csv`, `quest_reward_*_map.csv`, `quest_zone_map.csv`, `quest_episode_map.csv`, `episode_quest_client_map.csv`, `public_event_*_map.csv`, `creature_public_event_map.csv`, `client_source_publicevent*_map.csv`, `client_source_communicatormessages_map.csv`, `client_source_cinematic_map.csv`, `challenge_*_map.csv`, `path_*_map.csv`, and `contract_*_map.csv`.
- Existing evidence docs: `QUEST_IMPLEMENTATION_STATUS.md`, `QUEST_IMPLEMENTATION_AUDIT.md`, `LAUGHINGWS_INSTANCES_AND_MORE_AUDIT.md`, dungeon/raid smoke reviews, and `CURRENT_STATUS.md`.

## Quest Summary

- Total Quest2 rows: 5194

| Bucket | Rows |
| --- | ---: |
| `curated_full` | 49 |
| `generic_table_driven` | 3840 |
| `generic_with_script` | 14 |
| `no_objectives` | 1291 |

| Evidence status | Rows |
| --- | ---: |
| `implemented_with_focused_tests_pending_manual_smoke` | 49 |
| `mapped_only_no_objective_lifecycle_pending_row_specific_dialog_reward_achievement_smoke` | 1289 |
| `mapped_table_driven_pending_world_spawns_loot_smoke` | 3840 |
| `mapped_table_driven_with_script_hook_pending_world_smoke` | 10 |
| `runtime_q3671_no_objective_deadeye_turnin_reward_lifecycle_tested_pending_client_smoke` | 1 |
| `runtime_q3741_supply_crate_spawn_and_credit_script_tested_pending_client_smoke` | 1 |
| `runtime_q3777_denner_loftite_crystal_spawn_and_credit_script_tested_pending_client_smoke` | 1 |
| `runtime_q3781_starter_captives_receiver_spawn_credit_and_duplicate_guard_tested_pending_client_smoke` | 1 |
| `runtime_q3783_item_started_no_objective_durek_turnin_reward_tested_pending_client_smoke` | 1 |
| `runtime_q4526_wreckage_anomaly_credit_and_duplicate_guard_script_tested_pending_client_smoke` | 1 |

## Quest Objective Dependency Summary

- Total objective rows: 7108

| Support status | Rows |
| --- | ---: |
| `mapped_generic_handler` | 7108 |

| Evidence status | Rows |
| --- | ---: |
| `mapped_activate_entity_reward_pane_target_group_with_test_pending_spawn_credit_smoke` | 365 |
| `mapped_enter_area_world_location_trigger_with_test_pending_trigger_placement_smoke` | 467 |
| `mapped_objective_handler_pending_world_trigger_smoke` | 6237 |
| `runtime_q3479_trapped_survivor_csi_spawn_and_credit_tested_external_video_progress_observed_pending_local_client_smoke` | 1 |
| `runtime_q3479_yeti_target_group_spawn_and_kill_credit_tested_external_video_combat_progress_observed_pending_local_client_smoke` | 1 |
| `runtime_q3480_trapped_survivor_csi_spawn_and_credit_tested_external_video_progress_observed_pending_local_client_smoke` | 1 |
| `runtime_q3480_yeti_target_group_spawn_and_kill_credit_tested_external_video_progress_observed_pending_local_client_smoke` | 1 |
| `runtime_q3486_arrival_objective_zone_story_credit_tested_pending_client_smoke` | 1 |
| `runtime_q3486_loftite_virtual_collect_crystal_guardian_and_frostbite_credit_tested_pending_spawn_client_smoke` | 1 |
| `runtime_q3667_master_control_panel_activate_credit_tested_pending_client_smoke` | 1 |
| `runtime_q3668_imprisoned_survivor_activate_target_group_spawn_tested_pending_client_smoke` | 1 |
| `runtime_q3668_lusk_talk_objective_spawn_and_generic_talk_credit_tested_pending_client_smoke` | 1 |
| `runtime_q3668_skeech_nested_kill_target_group_spawn_tested_pending_client_smoke` | 1 |
| `runtime_q3673_hidden_completion_objective_duplicate_guard_script_tested_pending_client_smoke` | 1 |
| `runtime_q3673_signal_flare_csi_credit_tested_pending_client_smoke` | 3 |
| `runtime_q3673_signal_flare_target_group_checklist_credit_tested_pending_client_smoke` | 1 |
| `runtime_q3741_supply_crate_spawn_and_virtual_collect_credit_tested_pending_client_smoke` | 1 |
| `runtime_q3777_loftite_cliffs_enter_zone_credit_tested_pending_client_smoke` | 1 |
| `runtime_q3777_loftite_crystal_spawn_and_collect_item_credit_tested_pending_client_smoke` | 1 |
| `runtime_q3777_loftite_crystal_spawn_and_first_fragment_credit_tested_pending_client_smoke` | 1 |
| `runtime_q3781_captive_exile_soldier_spawn_rescue_credit_and_duplicate_guard_tested_pending_client_smoke` | 1 |
| `runtime_q3797_rootbrute_yeti_nested_kill_target_group_spawn_tested_pending_client_smoke` | 1 |
| `runtime_q3886_burning_torch_csi_spawn_and_credit_tested_pending_client_smoke` | 1 |
| `runtime_q3886_skeech_hut_checklist_spawn_and_credit_tested_pending_client_smoke` | 1 |
| `runtime_q3963_ship_controls_spawn_and_activate_entity_credit_tested_pending_client_smoke` | 1 |
| `runtime_q4526_spatial_anomaly_catch_credit_and_duplicate_guard_tested_pending_client_smoke` | 1 |
| `runtime_q4526_wreckage_world_location_credit_tested_pending_client_smoke` | 1 |
| `runtime_q4694_holdout_complete_event_kill_credit_and_spawn_tested_pending_client_smoke` | 1 |
| `runtime_q4694_trooper_vog_activate_entity_credit_tested_pending_dialog_client_smoke` | 1 |
| `runtime_q4696_rally_enter_area_trigger_spawn_and_credit_tested_pending_client_smoke` | 1 |
| `runtime_q4696_retreat_enemy_kill_to_activate_target_group_credit_tested_pending_client_smoke` | 1 |
| `runtime_q5573_hidden_completion_objective_tested_pending_client_smoke` | 1 |
| `runtime_q5573_power_regulator_spawn_and_checklist_credit_tested_pending_client_smoke` | 1 |
| `runtime_q5575_hidden_completion_objective_tested_pending_client_smoke` | 1 |
| `runtime_q5575_power_regulator_spawn_and_checklist_credit_tested_pending_client_smoke` | 1 |
| `runtime_q5580_tower_controls_spawn_and_checklist_credit_tested_pending_client_smoke` | 1 |
| `runtime_q5583_heavy_armor_megatech_spawn_and_kill_credit_path_tested_pending_client_smoke` | 1 |
| `runtime_q5583_heavy_armor_tank_spawn_and_kill_credit_path_tested_pending_client_smoke` | 1 |
| `runtime_q5597_chua_explosives_spawn_and_virtual_collect_credit_tested_external_video_observed_pending_local_client_smoke` | 1 |
| `runtime_q5604_exile_anti_air_cannon_spawn_and_checklist_credit_tested_pending_client_smoke` | 1 |

## Quest Reward Dependency Summary

- Total reward/dependency rows: 8767

| Reward source | Rows |
| --- | ---: |
| `Quest2.pushed_item` | 117 |
| `Quest2.reward_cash_override` | 118 |
| `Quest2.reward_reputation_override` | 2809 |
| `Quest2.reward_xp_override` | 104 |
| `Quest2.virtual_pushed_item` | 204 |
| `Quest2Reward` | 5415 |

| Support status | Rows |
| --- | ---: |
| `blocked_active_reward_rotation_schedule` | 86 |
| `mapped_client_state_dependency_pending_smoke` | 321 |
| `mapped_inline_reward_pending_row_smoke` | 3030 |
| `mapped_runtime_reward_path_pending_row_smoke` | 5329 |
| `runtime_q3777_default_reputation_reward_grant_tested_pending_client_smoke` | 1 |

## Quest Prerequisite Dependency Summary

- Total prerequisite/dependency rows: 14965

| Dependency type | Rows |
| --- | ---: |
| `class` | 1 |
| `excluded_quest` | 903 |
| `faction_level` | 6 |
| `item` | 165 |
| `level` | 5157 |
| `preq_flags` | 223 |
| `prerequisite_id` | 1236 |
| `prerequisite_slot` | 1505 |
| `quest_faction` | 3939 |
| `required_quest` | 1830 |

| Support status | Rows |
| --- | ---: |
| `blocked_missing_client_prerequisite_row_runtime_fail_closed` | 2 |
| `mapped_client_flag_dependency_pending_semantic_smoke` | 223 |
| `mapped_prerequisite_manager_pending_row_smoke` | 2739 |
| `mapped_quest_accept_validation_pending_reputation_smoke` | 6 |
| `mapped_quest_accept_validation_pending_row_smoke` | 9258 |
| `mapped_quest_chain_validation_pending_chain_smoke` | 1827 |
| `mapped_quest_exclusion_validation_pending_chain_smoke` | 903 |
| `server_quest_accept_validation_with_focused_test_pending_client_smoke` | 7 |

## Quest Loot Dependency Summary

- Total quest-loot rows: 7650

| Dependency type | Rows |
| --- | ---: |
| `entity_loot` | 5302 |
| `loot_group` | 1174 |
| `loot_item` | 1174 |

| Evidence status | Rows |
| --- | ---: |
| `rejected_orphaned_entity_loot_missing_current_client_quest_objective` | 6 |
| `rejected_orphaned_loot_group_missing_current_client_quest_objective` | 6 |
| `rejected_orphaned_loot_item_missing_current_client_quest_objective` | 6 |
| `runtime_virtual_collect_entity_loot_binding_pending_source_drop_client_smoke` | 5296 |
| `runtime_virtual_collect_loot_group_delivery_path_tested_pending_source_drop_client_smoke` | 1168 |
| `runtime_virtual_item_loot_delivery_tested_pending_source_drop_client_smoke` | 1168 |

## Quest World/Interaction Dependency Summary

- Total world/interaction dependency rows: 39038

| Dependency type | Rows |
| --- | ---: |
| `nested_target_group` | 1402 |
| `objective_indicator_location` | 6939 |
| `quest_alt_receiver_location` | 336 |
| `quest_direction` | 1122 |
| `quest_direction_entry` | 3747 |
| `quest_direction_entry_inactive_location` | 107 |
| `quest_receiver_location` | 3253 |
| `quest_world_zone` | 4954 |
| `target_group` | 4483 |
| `target_group_member` | 12695 |

| Evidence status | Rows |
| --- | ---: |
| `blocked_missing_client_nested_target_group_row_runtime_fail_closed` | 12 |
| `blocked_missing_client_quest_direction_entry_row_runtime_fail_closed` | 9 |
| `client_nested_target_group_pending_member_smoke` | 1390 |
| `client_objective_indicator_location_pending_world_trigger_smoke` | 6884 |
| `client_quest_alt_receiver_location_pending_prereq_dialog_smoke` | 334 |
| `client_quest_direction_entry_pending_location_smoke` | 3738 |
| `client_quest_direction_inactive_location_pending_state_smoke` | 107 |
| `client_quest_direction_pending_path_arrow_smoke` | 1122 |
| `client_quest_receiver_location_pending_dialog_smoke` | 3234 |
| `client_quest_world_zone_pending_map_and_activation_smoke` | 4953 |
| `client_target_group_member_pending_spawn_and_credit_smoke` | 12629 |
| `client_target_group_pending_objective_entity_smoke` | 4467 |
| `runtime_entity_spawn_present_pending_credit_client_smoke` | 13 |
| `runtime_q3479_deadeye_receiver_location_spawn_and_receiver_cache_tested_external_video_dialog_observed_pending_local_dialog_ui_smoke` | 1 |
| `runtime_q3479_q3480_shared_yeti_spawn_location_and_kill_credit_tested_external_video_combat_progress_observed_pending_local_client_smoke` | 4 |
| `runtime_q3479_q3480_shared_yeti_target_group_member_spawn_and_kill_credit_tested_pending_client_smoke` | 3 |
| `runtime_q3479_q3480_trapped_survivor_spawn_location_and_csi_credit_tested_external_video_progress_observed_pending_local_client_smoke` | 1 |
| `runtime_q3479_yeti_snowstalker_target_group_member_spawn_and_kill_credit_tested_external_video_combat_observed_pending_local_client_smoke` | 1 |
| `runtime_q3480_deadeye_receiver_location_spawn_and_receiver_cache_tested_external_video_dialog_observed_pending_local_dialog_ui_smoke` | 1 |
| `runtime_q3480_shared_yeti_spawn_location_and_kill_credit_tested_external_video_map_progress_observed_pending_local_client_smoke` | 4 |
| `runtime_q3480_trapped_survivor_spawn_location_and_csi_credit_tested_external_video_map_progress_observed_pending_local_client_smoke` | 1 |
| `runtime_q3486_arrival_indicator_and_story_credit_tested_pending_client_smoke` | 1 |
| `runtime_q3486_crystal_guardian_spawn_and_virtual_collect_credit_tested_pending_client_smoke` | 1 |
| `runtime_q3486_frostbite_target_group_member_script_credit_tested_pending_spawn_client_smoke` | 1 |
| `runtime_q3486_loftite_indicator_crystal_guardian_spawn_and_credit_tested_pending_client_smoke` | 1 |
| `runtime_q3486_master_control_panel_receiver_location_spawn_tested_pending_dialog_client_smoke` | 1 |
| `runtime_q3667_master_control_panel_indicator_spawn_and_activate_credit_tested_pending_client_smoke` | 1 |
| `runtime_q3667_master_control_panel_receiver_location_spawn_and_credit_tested_pending_dialog_client_smoke` | 1 |
| `runtime_q3668_bartol_receiver_location_spawn_tested_pending_dialog_client_smoke` | 1 |
| `runtime_q3668_imprisoned_survivor_indicator_spawn_and_activate_credit_tested_pending_client_smoke` | 1 |
| `runtime_q3668_imprisoned_survivor_target_group_member_spawn_and_activate_credit_tested_pending_client_smoke` | 1 |
| `runtime_q3668_lusk_talk_indicator_spawn_and_generic_talk_credit_tested_pending_client_smoke` | 1 |
| `runtime_q3668_skeech_indicator_spawn_and_nested_kill_credit_tested_pending_client_smoke` | 2 |
| `runtime_q3668_skeech_target_group_member_spawn_and_kill_credit_tested_pending_client_smoke` | 7 |
| `runtime_q3670_alt_receiver_location_spawn_and_receiver_cache_tested_pending_dialog_client_smoke` | 1 |
| `runtime_q3671_deadeye_receiver_location_spawn_and_quest_lifecycle_tested_pending_dialog_client_smoke` | 1 |
| `runtime_q3673_signal_flare_spawn_location_and_csi_credit_tested_pending_client_smoke` | 3 |
| `runtime_q3741_supply_crate_indicator_spawn_and_virtual_collect_credit_tested_pending_client_smoke` | 3 |
| `runtime_q3741_supply_crate_target_group_member_spawn_and_virtual_collect_credit_tested_pending_client_smoke` | 1 |
| `runtime_q3777_denner_receiver_location_spawn_tested_pending_dialog_client_smoke` | 1 |
| `runtime_q3777_loftite_cliffs_indicator_spawn_and_enter_zone_credit_tested_pending_client_smoke` | 1 |
| `runtime_q3777_loftite_crystal_indicator_spawn_and_collect_item_credit_tested_pending_client_smoke` | 1 |
| `runtime_q3777_loftite_crystal_indicator_spawn_and_first_fragment_credit_tested_pending_client_smoke` | 1 |
| `runtime_q3777_loftite_crystal_target_group_member_spawn_and_collect_item_credit_tested_pending_client_smoke` | 1 |
| `runtime_q3777_loftite_crystal_target_group_member_spawn_and_first_fragment_credit_tested_pending_client_smoke` | 1 |
| `runtime_q3781_captive_indicator_spawn_and_rescue_credit_tested_pending_client_smoke` | 1 |
| `runtime_q3781_captive_target_group_member_script_credit_tested_pending_spawn_client_smoke` | 2 |
| `runtime_q3781_captive_target_group_member_spawn_and_rescue_credit_tested_pending_client_smoke` | 1 |
| `runtime_q3781_deadeye_alt_receiver_location_spawn_and_receiver_cache_tested_pending_dialog_client_smoke` | 1 |
| `runtime_q3783_lands_reach_durek_receiver_location_spawn_and_no_objective_turnin_tested_pending_dialog_client_smoke` | 1 |
| `runtime_q3797_lands_reach_durek_receiver_location_spawn_and_giver_receiver_cache_tested_pending_dialog_client_smoke` | 1 |
| `runtime_q3797_rootbrute_yeti_indicator_spawn_and_nested_kill_credit_tested_pending_client_smoke` | 3 |
| `runtime_q3797_rootbrute_yeti_target_group_member_kill_credit_tested_pending_client_smoke` | 1 |
| `runtime_q3797_rootbrute_yeti_target_group_member_spawn_and_kill_credit_tested_pending_client_smoke` | 3 |
| `runtime_q3886_burning_torch_indicator_spawn_and_csi_credit_tested_pending_client_smoke` | 1 |
| `runtime_q3886_lands_reach_durek_receiver_location_spawn_tested_pending_dialog_client_smoke` | 1 |
| `runtime_q3886_skeech_hut_indicator_spawn_and_checklist_credit_tested_pending_client_smoke` | 1 |
| `runtime_q3886_skeech_hut_target_group_member_spawn_and_checklist_credit_tested_pending_client_smoke` | 1 |
| `runtime_q3963_deadeye_receiver_location_spawn_and_receiver_cache_tested_pending_dialog_client_smoke` | 1 |
| `runtime_q3963_ship_controls_indicator_spawn_and_activate_credit_tested_pending_client_smoke` | 2 |
| `runtime_q3963_ship_controls_target_group_member_spawn_and_activate_credit_tested_pending_client_smoke` | 1 |
| `runtime_q3963_ship_controls_target_group_spawn_and_activate_credit_tested_pending_client_smoke` | 1 |
| `runtime_q4526_anomaly_spawn_location_catch_credit_and_duplicate_guard_tested_pending_client_smoke` | 1 |
| `runtime_q4526_pristan_receiver_location_spawn_tested_pending_dialog_client_smoke` | 1 |
| `runtime_q4526_wreckage_world_location_credit_tested_pending_client_smoke` | 1 |
| `runtime_q4696_darby_receiver_location_spawn_and_receiver_cache_tested_pending_dialog_client_smoke` | 1 |
| `runtime_q4696_runtime_spawn_enemy_kill_credit_tested_pending_client_smoke` | 1 |
| `runtime_q4696_script_spawn_enemy_kill_credit_tested_pending_client_smoke` | 1 |
| `runtime_q5573_q5575_power_regulator_indicator_spawn_and_checklist_credit_tested_pending_client_smoke` | 6 |
| `runtime_q5573_q5575_power_regulator_target_group_member_spawn_and_checklist_credit_tested_pending_client_smoke` | 2 |
| `runtime_q5573_q5575_power_regulator_target_group_spawn_and_checklist_credit_tested_pending_client_smoke` | 2 |
| `runtime_q5580_kezrek_receiver_location_spawn_and_receiver_override_tested_pending_dialog_client_smoke` | 1 |
| `runtime_q5580_tower_controls_indicator_spawn_and_checklist_credit_tested_pending_client_smoke` | 2 |
| `runtime_q5580_tower_controls_target_group_member_spawn_and_checklist_credit_tested_pending_client_smoke` | 1 |
| `runtime_q5580_tower_controls_target_group_spawn_and_checklist_credit_tested_pending_client_smoke` | 1 |
| `runtime_q5583_heavy_armor_megatech_indicator_spawn_and_kill_credit_path_tested_pending_client_smoke` | 1 |
| `runtime_q5583_heavy_armor_tank_indicator_spawn_and_kill_credit_path_tested_pending_client_smoke` | 1 |
| `runtime_q5583_heavy_armor_target_group_member_fallback_spawn_and_kill_credit_path_tested_pending_client_smoke` | 16 |
| `runtime_q5583_kezrek_receiver_location_spawn_and_receiver_override_tested_pending_dialog_client_smoke` | 1 |
| `runtime_q5583_megatech_child_target_group_partial_spawn_and_expansion_tested_pending_client_smoke` | 6 |
| `runtime_q5583_megatech_nested_target_group_expansion_and_partial_spawns_tested_pending_client_smoke` | 2 |
| `runtime_q5583_tank_target_group_spawn_and_kill_credit_path_tested_pending_client_smoke` | 2 |
| `runtime_q5597_chua_explosives_indicator17896_spawn_and_credit_tested_external_video_observed_pending_local_client_ui_smoke` | 1 |
| `runtime_q5597_chua_explosives_indicator17897_spawn_and_credit_tested_external_video_observed_pending_local_client_ui_smoke` | 1 |
| `runtime_q5597_chua_explosives_indicator17898_spawn_and_credit_tested_external_video_observed_pending_local_client_ui_smoke` | 1 |
| `runtime_q5597_chua_explosives_indicator17899_spawn_and_credit_tested_external_video_observed_pending_local_client_ui_smoke` | 1 |
| `runtime_q5597_chua_explosives_target_group_member_spawn_and_virtual_collect_credit_tested_external_video_observed_pending_local_client_smoke` | 1 |
| `runtime_q5597_chua_explosives_target_group_spawn_and_virtual_collect_credit_tested_external_video_observed_pending_local_client_smoke` | 1 |
| `runtime_q5597_crimson_isle_world_zone_assets_tested_external_video_observed_pending_local_client_visibility_smoke` | 1 |
| `runtime_q5597_mondo_receiver_spawn_accept_complete_tested_external_video_observed_pending_local_dialog_ui_smoke` | 1 |
| `runtime_q5604_exile_anti_air_cannon_indicator_spawn_and_checklist_credit_tested_pending_client_smoke` | 1 |
| `runtime_q5604_exile_anti_air_cannon_target_group_member_spawn_and_checklist_credit_tested_pending_client_smoke` | 1 |
| `runtime_q5604_exile_anti_air_cannon_target_group_spawn_and_checklist_credit_tested_pending_client_smoke` | 1 |
| `runtime_script_receiver_location_spawn_and_receiver_cache_tested_pending_dialog_client_smoke` | 1 |
| `runtime_script_receiver_location_spawn_tested_pending_dialog_client_smoke` | 2 |
| `runtime_script_spawn_location_covered_with_test_pending_client_smoke` | 3 |
| `runtime_script_spawn_present_with_activate_credit_test_pending_dialog_client_smoke` | 1 |
| `runtime_script_spawn_present_with_activate_target_group_checklist_credit_test_pending_client_smoke` | 3 |
| `runtime_world_location_trigger_spawned_with_test_pending_client_smoke` | 1 |

## Quest Achievement/Progression Dependency Summary

- Total achievement/progression dependency rows: 2056

| Dependency type | Rows |
| --- | ---: |
| `achievement_checklist` | 2056 |

| Evidence status | Rows |
| --- | ---: |
| `client_achievement_checklist_row_pending_progression_hook_smoke` | 2045 |
| `runtime_q3479_achievement_checklist_completion_hooks_tested_pending_client_ui_smoke` | 3 |
| `runtime_q3480_achievement_checklist_completion_hooks_tested_pending_client_ui_smoke` | 1 |
| `runtime_q3668_achievement_checklist_completion_hooks_tested_pending_client_ui_smoke` | 1 |
| `runtime_q3673_achievement_checklist_completion_hooks_tested_pending_client_ui_smoke` | 1 |
| `runtime_q3741_achievement_checklist_completion_hooks_tested_pending_client_ui_smoke` | 1 |
| `runtime_q3777_achievement_checklist_completion_hooks_tested_pending_client_ui_smoke` | 1 |
| `runtime_q3886_achievement_checklist_completion_hooks_tested_pending_client_ui_smoke` | 1 |
| `runtime_q3963_achievement_checklist_completion_hooks_tested_pending_client_ui_smoke` | 1 |
| `runtime_q5597_achievement_checklist_completion_hook_tested_external_video_completion_observed_bundle_recorded_pending_local_client_ui_smoke` | 1 |

## Quest Script Hook Summary

- Total Script.Main owner hook rows: 88

| Script kind | Rows |
| --- | ---: |
| `map_script` | 10 |
| `public_event_script` | 1 |
| `quest_related_runtime_script` | 1 |
| `quest_script` | 66 |
| `spell_script` | 10 |

| Quest link status | Rows |
| --- | ---: |
| `current_quest` | 67 |
| `non_quest_script_owner` | 21 |

| Evidence status | Rows |
| --- | ---: |
| `implemented_no_objective_script_lifecycle_tests_pending_dialog_smoke` | 3 |
| `runtime_curated_quest_script_pending_manual_smoke` | 35 |
| `runtime_generic_quest_script_followup_4694_tested_pending_objective_and_client_smoke` | 1 |
| `runtime_generic_quest_script_hook_pending_world_smoke` | 8 |
| `runtime_map_script_owner_pending_zone_smoke` | 10 |
| `runtime_public_event_script_owner_pending_event_smoke` | 1 |
| `runtime_q3486_arrival_loftite_guardian_and_followup_script_tested_pending_client_smoke` | 1 |
| `runtime_q3667_master_control_panel_spawn_and_credit_script_tested_pending_client_smoke` | 1 |
| `runtime_q3668_receiver_objective_spawn_and_generic_credit_paths_tested_pending_client_smoke` | 1 |
| `runtime_q3673_signal_flare_hidden_completion_duplicate_guard_and_followup_script_tested_pending_client_smoke` | 1 |
| `runtime_q3741_supply_crate_spawn_and_credit_script_tested_pending_client_smoke` | 1 |
| `runtime_q3777_denner_loftite_crystal_spawn_and_credit_script_tested_pending_client_smoke` | 1 |
| `runtime_q3781_starter_captives_receiver_spawn_credit_and_duplicate_guard_tested_pending_client_smoke` | 1 |
| `runtime_q3783_item_started_no_objective_durek_turnin_reward_tested_pending_client_smoke` | 1 |
| `runtime_q3797_durek_receiver_objective_spawn_and_generic_credit_paths_tested_pending_client_smoke` | 1 |
| `runtime_q3886_torch_hut_spawn_and_generic_credit_paths_tested_pending_client_smoke` | 1 |
| `runtime_q3963_ship_controls_and_deadeye_receiver_paths_tested_pending_client_smoke` | 1 |
| `runtime_q4526_wreckage_anomaly_credit_and_duplicate_guard_script_tested_pending_client_smoke` | 1 |
| `runtime_q5573_power_regulator_spawn_checklist_credit_and_completion_script_tested_pending_client_smoke` | 1 |
| `runtime_q5575_power_regulator_spawn_checklist_credit_and_completion_script_tested_pending_client_smoke` | 1 |
| `runtime_q5580_tower_controls_kezrek_receiver_and_merge_gate_script_tested_pending_client_smoke` | 1 |
| `runtime_q5583_heavy_armor_targets_kezrek_receiver_and_merge_gate_script_tested_pending_client_smoke` | 1 |
| `runtime_q5594_kezrek_receiver_ship_controls_and_warbot_credit_guard_tested_pending_client_smoke` | 1 |
| `runtime_q5597_chua_explosives_spawn_credit_and_followup_script_tested_external_video_observed_pending_local_client_smoke` | 1 |
| `runtime_q5604_exile_anti_air_cannon_spawn_checklist_credit_and_completion_script_tested_pending_client_smoke` | 1 |
| `runtime_quest_related_holdout_credit_script_tested_pending_dialog_client_smoke` | 1 |
| `runtime_spell_script_owner_pending_spell_smoke` | 10 |

## Quest Creature/NPC Relationship Summary

- Total DataMapping creature relationship rows: 13412

| Relation type | Rows |
| --- | ---: |
| `finisher` | 2231 |
| `objective` | 8602 |
| `starter` | 2579 |

| Quest link status | Rows |
| --- | ---: |
| `current_quest` | 13295 |
| `non_current_client_quest_reference` | 117 |

| Creature match status | Rows |
| --- | ---: |
| `ambiguous_name` | 3208 |
| `client_creature2_quest_relation_reviewed` | 12 |
| `client_creature2_target_group_reviewed` | 2 |
| `reviewed` | 1247 |
| `reviewed_stale_replaced_by_current_finisher` | 1 |
| `reviewed_stale_replaced_by_current_starter` | 1 |
| `reviewed_stale_replaced_by_followup_handoff_context` | 1 |
| `scored_name` | 1276 |
| `unique_name` | 7452 |
| `unmatched` | 212 |

| Evidence status | Rows |
| --- | ---: |
| `client_q3963_starter_relation_mapped_to_followup_handoff_pending_visible_starter_proof` | 1 |
| `datamapping_creature_bridge_ambiguous_pending_review` | 3157 |
| `datamapping_creature_bridge_unmatched_blocked` | 207 |
| `datamapping_finisher_creature_relation_pending_review_and_smoke` | 1033 |
| `datamapping_objective_creature_relation_pending_review_and_smoke` | 6562 |
| `datamapping_q3480_stale_unknown_starter_relation_replaced_by_reviewed_durek_starter` | 1 |
| `datamapping_q3673_stale_unknown_finisher_relation_replaced_by_reviewed_deadeye_finisher` | 1 |
| `datamapping_q3963_stale_unknown_starter_relation_replaced_by_followup_handoff_context` | 1 |
| `datamapping_quest_relation_not_current_client_quest_row_blocked` | 117 |
| `datamapping_starter_creature_relation_pending_review_and_smoke` | 1064 |
| `reviewed_finisher_creature_relation_pending_spawn_dialog_credit_smoke` | 361 |
| `reviewed_objective_creature_relation_pending_spawn_dialog_credit_smoke` | 355 |
| `reviewed_starter_creature_relation_pending_spawn_dialog_credit_smoke` | 422 |
| `runtime_q3479_bosun_starter_relation_spawn_tested_external_video_accept_observed_pending_local_dialog_smoke` | 1 |
| `runtime_q3479_deadeye_finisher_relation_spawn_and_receiver_cache_tested_external_video_dialog_observed_pending_local_dialog_smoke` | 1 |
| `runtime_q3479_fierce_yeti_relation_spawn_present_mapped_only_objective_credit_blocked` | 1 |
| `runtime_q3479_q3480_shared_yeti_objective_relation_spawn_and_kill_credit_tested_pending_client_smoke` | 3 |
| `runtime_q3479_trapped_survivor_objective_relation_spawn_and_csi_credit_tested_external_video_progress_observed_pending_local_client_smoke` | 1 |
| `runtime_q3479_yeti_snowstalker_objective_relation_spawn_and_kill_credit_tested_external_video_combat_observed_pending_local_client_smoke` | 1 |
| `runtime_q3480_commander_durek_starter_relation_spawn_and_giver_cache_tested_external_video_accept_observed_pending_local_dialog_smoke` | 1 |
| `runtime_q3480_deadeye_finisher_relation_spawn_and_receiver_cache_tested_external_video_dialog_observed_pending_local_dialog_smoke` | 1 |
| `runtime_q3480_fierce_yeti_relation_spawn_present_mapped_only_objective_credit_blocked` | 1 |
| `runtime_q3480_trapped_survivor_objective_relation_spawn_and_csi_credit_tested_external_video_progress_observed_pending_local_client_smoke` | 1 |
| `runtime_q3486_crystal_guardian_objective_relation_spawn_and_virtual_collect_credit_tested_pending_client_smoke` | 1 |
| `runtime_q3486_master_control_panel_finisher_relation_spawn_tested_pending_dialog_smoke` | 1 |
| `runtime_q3486_master_control_panel_starter_relation_spawn_tested_pending_dialog_smoke` | 1 |
| `runtime_q3667_deadeye_starter_relation_spawn_covered_pending_accept_dialog_smoke` | 1 |
| `runtime_q3667_master_control_panel_finisher_relation_spawn_and_credit_tested_pending_dialog_smoke` | 1 |
| `runtime_q3668_bartol_finisher_relation_spawn_tested_pending_dialog_smoke` | 1 |
| `runtime_q3668_deadeye_starter_relation_spawn_and_followup_chain_tested_pending_dialog_smoke` | 1 |
| `runtime_q3668_imprisoned_survivor_objective_relation_spawn_and_activate_credit_tested_pending_client_smoke` | 2 |
| `runtime_q3668_lusk_objective_relation_spawn_and_talk_credit_tested_pending_client_smoke` | 1 |
| `runtime_q3668_skeech_duplicate_objective_relation_spawn_and_kill_credit_tested_pending_client_smoke` | 4 |
| `runtime_q3668_skeech_objective_relation_spawn_and_kill_credit_tested_pending_client_smoke` | 7 |
| `runtime_q3741_lands_reach_durek_finisher_relation_spawn_and_receiver_cache_tested_pending_dialog_smoke` | 1 |
| `runtime_q3741_lands_reach_durek_starter_relation_spawn_and_giver_cache_tested_pending_dialog_smoke` | 1 |
| `runtime_q3741_supply_crate_objective_relation_spawn_and_virtual_collect_credit_tested_pending_client_smoke` | 1 |
| `runtime_q3777_denner_finisher_relation_spawn_tested_pending_dialog_smoke` | 1 |
| `runtime_q3777_denner_starter_relation_spawn_tested_pending_dialog_smoke` | 1 |
| `runtime_q3777_loftite_crystal_objective_relation_target_group_member_spawn_and_credit_tested_pending_client_smoke` | 1 |
| `runtime_q3781_captive_objective_relation_target_group_member_spawn_and_credit_tested_pending_client_smoke` | 1 |
| `runtime_q3781_dead_exile_soldier_starter_relation_spawn_and_giver_cache_tested_pending_dialog_smoke` | 1 |
| `runtime_q3781_deadeye_finisher_relation_spawn_and_receiver_cache_tested_pending_dialog_smoke` | 1 |
| `runtime_q3783_lands_reach_durek_finisher_relation_spawn_and_reward_turnin_tested_pending_dialog_smoke` | 1 |
| `runtime_q3797_durek_finisher_relation_spawn_and_receiver_cache_tested_pending_dialog_smoke` | 1 |
| `runtime_q3797_durek_starter_relation_spawn_and_giver_cache_tested_pending_dialog_smoke` | 1 |
| `runtime_q3797_rootbrute_deathcap_objective_relation_spawn_and_kill_credit_tested_pending_client_smoke` | 1 |
| `runtime_q3797_rootbrute_objective_relation_spawn_and_kill_credit_tested_pending_client_smoke` | 1 |
| `runtime_q3797_yeti_frostclaw_objective_relation_spawn_and_kill_credit_tested_pending_client_smoke` | 1 |
| `runtime_q3797_yeti_snowstalker_objective_relation_credit_tested_pending_q3797_spawn_review_client_smoke` | 1 |
| `runtime_q3886_burning_torch_duplicate_relation_spawn_and_csi_credit_tested_pending_client_smoke` | 1 |
| `runtime_q3886_burning_torch_objective_relation_spawn_and_csi_credit_tested_pending_client_smoke` | 1 |
| `runtime_q3886_durek_finisher_relation_spawn_tested_pending_dialog_smoke` | 1 |
| `runtime_q3886_durek_starter_relation_spawn_tested_pending_dialog_smoke` | 1 |
| `runtime_q3886_skeech_hut_objective_relation_spawn_and_checklist_credit_tested_pending_client_smoke` | 1 |
| `runtime_q3963_deadeye_finisher_relation_spawn_and_receiver_cache_tested_pending_dialog_smoke` | 1 |
| `runtime_q3963_ship_controls_objective_relation_spawn_and_activate_credit_tested_pending_client_smoke` | 1 |
| `runtime_q5573_mondo_finisher_spawn_and_receiver_cache_tested_pending_dialog_smoke` | 1 |
| `runtime_q5573_mondo_starter_spawn_and_giver_cache_tested_pending_dialog_smoke` | 1 |
| `runtime_q5573_power_regulator_objective_relation_spawn_and_checklist_credit_tested_pending_client_smoke` | 1 |
| `runtime_q5575_kezrek_starter_spawn_and_giver_receiver_cache_tested_pending_dialog_smoke` | 1 |
| `runtime_q5575_power_regulator_objective_relation_spawn_and_checklist_credit_tested_pending_client_smoke` | 1 |
| `runtime_q5580_kezrek_finisher_relation_spawn_and_receiver_override_tested_pending_dialog_smoke` | 1 |
| `runtime_q5580_kezrek_starter_relation_spawn_tested_pending_dialog_slot_explanation_smoke` | 1 |
| `runtime_q5580_tower_controls_objective_relation_spawn_and_checklist_credit_tested_pending_client_smoke` | 1 |
| `runtime_q5583_hellfire_tank_objective_relation_spawn_and_kill_credit_path_tested_pending_client_smoke` | 1 |
| `runtime_q5583_kezrek_finisher_relation_spawn_and_receiver_override_tested_pending_dialog_smoke` | 1 |
| `runtime_q5583_kezrek_starter_relation_spawn_tested_pending_dialog_slot_explanation_smoke` | 1 |
| `runtime_q5583_megatech_contractor_objective_relation_spawn_and_kill_credit_path_tested_pending_client_smoke` | 1 |
| `runtime_q5583_megatech_gunner_objective_relation_spawn_and_kill_credit_path_tested_pending_client_smoke` | 1 |
| `runtime_q5583_megatech_merc_objective_relation_spawn_and_kill_credit_path_tested_pending_client_smoke` | 1 |
| `runtime_q5583_megatech_shock_trooper_objective_relation_spawn_and_kill_credit_path_tested_pending_client_smoke` | 1 |
| `runtime_q5583_megatech_swordsmaster_objective_relation_spawn_and_kill_credit_path_tested_pending_client_smoke` | 1 |
| `runtime_q5583_megatech_warbot_objective_relation_spawn_and_kill_credit_path_tested_pending_client_smoke` | 1 |
| `runtime_q5583_vindicator_tank_objective_relation_spawn_and_kill_credit_path_tested_pending_client_smoke` | 1 |
| `runtime_q5594_kezrek_finisher_relation_spawn_and_receiver_cache_tested_pending_dialog_smoke` | 1 |
| `runtime_q5594_kezrek_starter_relation_spawn_and_giver_cache_tested_pending_dialog_smoke` | 1 |
| `runtime_q5597_chua_explosives_30_fallbacks_virtual_collect_guards_tested_external_video_observed_pending_local_client_ui_smoke` | 1 |
| `runtime_q5597_mondo_finisher_spawn_rewards_achievement_and_followup_tested_external_video_observed_pending_local_dialog_ui_smoke` | 1 |
| `runtime_q5597_mondo_starter_spawn_accept_gate_and_followup_tested_external_video_observed_pending_local_dialog_ui_smoke` | 1 |
| `runtime_q5604_exile_anti_air_cannon_objective_relation_spawn_and_checklist_credit_tested_pending_client_smoke` | 1 |
| `runtime_script_receiver_location_spawn_and_receiver_cache_tested_pending_dialog_client_smoke` | 1 |
| `runtime_script_spawn_present_client_creature2_alt_finisher_relation_with_receiver_cache_tested_pending_dialog_smoke` | 1 |
| `runtime_script_spawn_present_client_creature2_finisher_relation_tested_pending_dialog_smoke` | 2 |
| `runtime_script_spawn_present_client_creature2_finisher_relation_with_receiver_cache_tested_pending_dialog_smoke` | 1 |
| `runtime_script_spawn_present_client_creature2_finisher_relation_with_receiver_location_and_reward_tested_pending_dialog_smoke` | 1 |
| `runtime_script_spawn_present_client_creature2_finisher_relation_with_receiver_location_tested_pending_dialog_smoke` | 1 |
| `runtime_script_spawn_present_client_creature2_objective_relation_with_activation_and_holdout_credit_tests_pending_dialog_smoke` | 1 |
| `runtime_script_spawn_present_client_creature2_objective_relation_with_signal_flare_credit_tests_pending_client_smoke` | 1 |
| `runtime_script_spawn_present_client_creature2_starter_relation_tested_pending_dialog_smoke` | 1 |
| `runtime_script_spawn_present_client_creature2_starter_relation_with_quest_giver_cache_and_accept_path_tests_pending_dialog_smoke` | 1 |
| `runtime_script_spawn_present_client_creature2_starter_relation_with_receiver_location_tested_pending_dialog_smoke` | 2 |
| `runtime_spawn_present_datamapping_finisher_creature_relation_pending_bridge_review_credit_smoke` | 1 |
| `runtime_spawn_present_datamapping_objective_creature_relation_pending_bridge_review_credit_smoke` | 11 |
| `runtime_spawn_present_datamapping_starter_creature_relation_pending_bridge_review_credit_smoke` | 2 |
| `runtime_spawn_present_reviewed_finisher_creature_relation_pending_dialog_credit_smoke` | 13 |
| `runtime_spawn_present_reviewed_objective_creature_relation_pending_dialog_credit_smoke` | 1 |
| `runtime_spawn_present_reviewed_starter_creature_relation_pending_dialog_credit_smoke` | 9 |

## Quest Jabbithole Objective Evidence Summary

- Total DataMapping/Jabbithole objective evidence rows: 4558

| Quest link status | Rows |
| --- | ---: |
| `current_quest` | 4522 |
| `non_current_client_quest_reference` | 36 |

| Objective match status | Rows |
| --- | ---: |
| `matched` | 4459 |
| `reviewed_match` | 29 |
| `unmatched` | 70 |

| Evidence status | Rows |
| --- | ---: |
| `datamapping_jabbithole_objective_matched_pending_text_order_smoke` | 4458 |
| `datamapping_jabbithole_objective_matched_runtime_path_tested_pending_client_smoke` | 1 |
| `datamapping_jabbithole_objective_reviewed_match_external_video_observed_pending_local_client_smoke` | 1 |
| `datamapping_jabbithole_objective_reviewed_match_pending_client_smoke` | 27 |
| `datamapping_jabbithole_objective_reviewed_match_runtime_path_tested_pending_client_smoke` | 1 |
| `datamapping_jabbithole_objective_unmatched_blocked` | 34 |
| `datamapping_objective_not_current_client_quest_row_blocked` | 36 |

## Quest Jabbithole Reward Evidence Summary

- Total DataMapping/Jabbithole reward evidence rows: 8618

| Reward kind | Rows |
| --- | ---: |
| `currency` | 2643 |
| `item` | 3246 |
| `reputation` | 2422 |
| `tradeskill` | 307 |

| Quest link status | Rows |
| --- | ---: |
| `current_quest` | 8601 |
| `non_current_client_quest_reference` | 17 |

| Evidence status | Rows |
| --- | ---: |
| `datamapping_jabbithole_cash_reward_inline_pending_amount_smoke` | 2273 |
| `datamapping_jabbithole_currency_reward_matched_pending_grant_smoke` | 365 |
| `datamapping_jabbithole_item_reward_canonical_collision_reviewed_provenance_only_pending_choice_smoke` | 1 |
| `datamapping_jabbithole_item_reward_matched_pending_choice_smoke` | 3228 |
| `datamapping_jabbithole_item_reward_stale_source_game_reward_id_canonical_tuple_matched_pending_choice_smoke` | 3 |
| `datamapping_jabbithole_reputation_reward_matched_pending_amount_smoke` | 2422 |
| `datamapping_jabbithole_tradeskill_reward_matched_pending_grant_smoke` | 307 |
| `datamapping_reward_not_current_client_quest_row_blocked` | 17 |
| `runtime_q3479_fixed_cash_reward_grant_tested_external_video_reward_panel_observed_pending_local_client_smoke` | 1 |
| `runtime_q3480_fixed_cash_reward_grant_tested_external_video_reward_panel_observed_pending_local_client_smoke` | 1 |

## Quest Jabbithole Zone Evidence Summary

- Total DataMapping/Jabbithole zone evidence rows: 11285

| Relation source | Rows |
| --- | ---: |
| `quest_call_zones` | 7795 |
| `quest_zones` | 3490 |

| Quest link status | Rows |
| --- | ---: |
| `current_quest` | 11186 |
| `non_current_client_quest_reference` | 99 |

| Zone match status | Rows |
| --- | ---: |
| `matched` | 6167 |
| `partial` | 5079 |
| `unmatched` | 39 |

| Evidence status | Rows |
| --- | ---: |
| `datamapping_q3479_algoroc_call_zone_worldzone17_dom_zone_reviewed_nonprimary_pending_route_proof` | 1 |
| `datamapping_q3479_everstar_grove_call_zone_worldzone27_rockridge_hollow_reviewed_nonprimary_pending_route_proof` | 1 |
| `datamapping_q3479_everstar_passage_call_zone_worldzone397_vein_of_the_flow_reviewed_nonprimary_pending_route_proof` | 1 |
| `datamapping_q3479_exolab71_call_zone_worldzone395_sterling_croft_reviewed_nonprimary_pending_route_proof` | 1 |
| `datamapping_q3479_farside_call_zone_worldzone28_excavation_base_camp_reviewed_nonprimary_pending_route_proof` | 1 |
| `datamapping_q3479_galeras_call_zone_worldzone16_dom_zone_reviewed_nonprimary_pending_route_proof` | 1 |
| `datamapping_q3479_hidden_grotto_call_zone_worldzone425_supply_station19_reviewed_nonprimary_pending_route_proof` | 1 |
| `datamapping_q3479_housing_skymap_call_zone_worldzone60_mozyk_quarry_reviewed_nonprimary_pending_route_proof` | 1 |
| `datamapping_q3479_northern_wilds_call_zone_worldzone1_missing_reviewed_current_zone35_pending_route_proof` | 1 |
| `datamapping_q3479_northern_wilds_quest_zone_worldzone1_missing_reviewed_current_zone35_pending_route_proof` | 1 |
| `datamapping_q3479_thayd_call_zone_worldzone14_missing_reviewed_pending_route_proof` | 1 |
| `datamapping_q3479_whitevale_call_zone_worldzone2_missing_reviewed_pending_route_proof` | 1 |
| `datamapping_q3480_algoroc_call_zone_worldzone17_dom_zone_reviewed_nonprimary_pending_route_proof` | 1 |
| `datamapping_q3480_northern_wilds_call_zone_worldzone1_missing_reviewed_current_zone35_pending_route_proof` | 1 |
| `datamapping_q3480_northern_wilds_quest_zone_worldzone1_missing_reviewed_current_zone35_pending_route_proof` | 1 |
| `datamapping_q3486_northern_wilds_quest_zone_worldzone1_missing_reviewed_current_zone35_pending_route_proof` | 1 |
| `datamapping_q3668_coldburrow_cavern_quest_zone_worldzone219_loftite_cliffs_reviewed_nonprimary_pending_route_proof` | 1 |
| `datamapping_q3668_northern_wilds_quest_zone_worldzone1_missing_reviewed_current_zone35_pending_route_proof` | 1 |
| `datamapping_q3673_northern_wilds_call_zone_worldzone1_missing_reviewed_current_zone35_pending_route_proof` | 1 |
| `datamapping_q3673_northern_wilds_quest_zone_worldzone1_missing_reviewed_current_zone35_pending_route_proof` | 1 |
| `datamapping_q3797_northern_wilds_call_zone_worldzone1_missing_reviewed_current_zone35_pending_route_proof` | 1 |
| `datamapping_q3797_northern_wilds_quest_zone_worldzone1_missing_reviewed_current_zone35_pending_route_proof` | 1 |
| `datamapping_q3886_coldburrow_cavern_call_zone_worldzone219_loftite_cliffs_reviewed_nonprimary_pending_route_proof` | 1 |
| `datamapping_q3886_coldburrow_cavern_quest_zone_worldzone219_loftite_cliffs_reviewed_nonprimary_pending_route_proof` | 1 |
| `datamapping_q3886_northern_wilds_call_zone_worldzone1_missing_reviewed_current_zone35_pending_route_proof` | 1 |
| `datamapping_q3886_northern_wilds_quest_zone_worldzone1_missing_reviewed_current_zone35_pending_route_proof` | 1 |
| `datamapping_q3963_algoroc_call_zone_worldzone17_dom_zone_reviewed_nonprimary_pending_route_proof` | 1 |
| `datamapping_q3963_northern_wilds_call_zone_worldzone1_missing_reviewed_current_zone35_pending_route_proof` | 1 |
| `datamapping_q3963_northern_wilds_quest_zone_worldzone1_missing_reviewed_current_zone35_pending_route_proof` | 1 |
| `datamapping_q5573_crimson_badlands_call_zone_worldzone131_missing_reviewed_nonprimary_pending_route_proof` | 1 |
| `datamapping_q5573_crimson_isle_call_zone_worldzone12_blank_reviewed_current_zone622_pending_route_proof` | 1 |
| `datamapping_q5573_crimson_isle_quest_zone_worldzone12_blank_reviewed_current_zone622_pending_route_proof` | 1 |
| `datamapping_q5575_crimson_isle_call_zone_worldzone12_blank_reviewed_current_zone622_pending_route_proof` | 1 |
| `datamapping_q5575_crimson_isle_quest_zone_worldzone12_blank_reviewed_current_zone622_pending_route_proof` | 1 |
| `datamapping_q5580_crimson_isle_call_zone_worldzone12_blank_reviewed_current_zone622_pending_route_proof` | 1 |
| `datamapping_q5580_crimson_isle_quest_zone_worldzone12_blank_reviewed_current_zone622_pending_route_proof` | 1 |
| `datamapping_q5597_auroria_call_zone_worldzone6_missing_reviewed_nonprimary_pending_route_proof` | 1 |
| `datamapping_q5597_crimson_isle_call_zone_worldzone12_blank_reviewed_current_zone622_pending_route_proof` | 1 |
| `datamapping_q5597_crimson_isle_quest_zone_worldzone12_blank_reviewed_current_zone622_pending_route_proof` | 1 |
| `datamapping_q5597_housing_skymap_call_zone_worldzone60_mozyk_quarry_reviewed_nonprimary_pending_route_proof` | 1 |
| `datamapping_q5597_illium_call_zone_worldzone78_ellevar_reviewed_nonprimary_pending_route_proof` | 1 |
| `datamapping_q5597_malgrave_call_zone_worldzone42_protostar_honeyworks_reviewed_nonprimary_pending_route_proof` | 1 |
| `datamapping_q5597_northern_wastes_call_zone_worldzone41_fort_glory_reviewed_nonprimary_pending_route_proof` | 1 |
| `datamapping_quest_zone_matched_pending_runtime_visibility_smoke` | 6142 |
| `datamapping_quest_zone_partial_match_pending_zone_review` | 5001 |
| `datamapping_zone_not_current_client_quest_row_blocked` | 99 |

## Quest Episode Evidence Summary

- Total DataMapping episode-quest evidence rows: 1497

| Quest link status | Rows |
| --- | ---: |
| `current_quest` | 1488 |
| `non_current_client_quest_reference` | 9 |

| Episode bridge status | Rows |
| --- | ---: |
| `mapped_episode` | 1178 |
| `missing_episode_bridge` | 319 |

| Episode match status | Rows |
| --- | ---: |
| `matched` | 1178 |
| `missing_episode_bridge` | 319 |

| Evidence status | Rows |
| --- | ---: |
| `datamapping_episode_not_current_client_quest_row_blocked` | 9 |
| `datamapping_episode_quest_matched_pending_order_progression_smoke` | 1174 |
| `datamapping_episode_quest_missing_episode_bridge_blocked` | 313 |
| `runtime_q5597_episode_order_q5596_q5597_q5604_handoff_tested_external_video_observed_pending_local_episode_smoke` | 1 |

## Public Event DataMapping Evidence Summary

- Total DataMapping public-event evidence rows: 5013

| Evidence kind | Rows |
| --- | ---: |
| `public_event_creature` | 2645 |
| `public_event_mission` | 77 |
| `public_event_objective` | 1842 |
| `public_event_zone` | 449 |

| Public event link status | Rows |
| --- | ---: |
| `current_public_event` | 5004 |
| `non_current_public_event_reference` | 9 |

| Match status | Rows |
| --- | ---: |
| `ambiguous_name` | 620 |
| `child_event_mapped` | 37 |
| `event_mismatch` | 36 |
| `matched` | 2248 |
| `missing_client_zone` | 1 |
| `reviewed` | 111 |
| `scored_name` | 903 |
| `text_only_or_missing_child_event` | 40 |
| `unique_name` | 912 |
| `unmatched` | 100 |
| `unmatched_event` | 2 |
| `unmatched_objective` | 3 |

| Evidence status | Rows |
| --- | ---: |
| `blocked_public_event_creature_ambiguous_multi_member_targetgroup_requires_relation_smoke` | 39 |
| `blocked_public_event_creature_ambiguous_requires_bridge_review` | 444 |
| `blocked_public_event_creature_unmatched_missing_client_creature_bridge` | 89 |
| `datamapping_public_event_not_current_client_event_row_blocked` | 9 |
| `datamapping_public_event_objective_event_mismatch_blocked` | 36 |
| `datamapping_public_event_objective_unmatched_blocked` | 3 |
| `datamapping_public_event_parent_not_current_blocked` | 415 |
| `datamapping_public_event_zone_missing_client_zone_blocked` | 1 |
| `mapped_public_event_creature_blocked_missing_reviewed_bridge_spawn_credit_smoke` | 1347 |
| `mapped_public_event_mission_text_blocked_missing_child_event_transition_evidence` | 40 |
| `mapped_public_event_objective_blocked_missing_runtime_producer_evidence` | 1304 |
| `mapped_public_event_zone_blocked_missing_runtime_visibility_bridge_evidence` | 445 |
| `reviewed_public_event_creature_blocked_spawn_credit_smoke` | 63 |
| `runtime_fragment_zero_life_overseer_public_event_creature_death_credit_tested_pending_encounter_and_client_smoke` | 2 |
| `runtime_fragment_zero_prototype_matron_public_event_creature_death_credit_tested_pending_spawn_route_and_client_smoke` | 8 |
| `runtime_fragment_zero_skeech_horde_public_event_creature_death_credit_tested_pending_spawn_route_and_client_smoke` | 2 |
| `runtime_fragment_zero_target_group_public_event_creature_activation_credit_tested_pending_visuals_placement_and_client_smoke` | 3 |
| `runtime_gauntlet_door_lock_public_event_creature_activation_credit_tested_pending_door_visual_state_and_client_smoke` | 1 |
| `runtime_gauntlet_first_arena_public_event_creature_death_credit_tested_pending_arena_route_rewards_and_client_smoke` | 24 |
| `runtime_gauntlet_opposing_faction_public_event_creature_death_credit_tested_pending_faction_arena_route_and_client_smoke` | 8 |
| `runtime_gauntlet_pilot_taboro_public_event_creature_talk_credit_tested_pending_dialog_client_smoke` | 1 |
| `runtime_gauntlet_product_endorsement_public_event_creature_activation_credit_tested_pending_product_side_effects_and_client_smoke` | 5 |
| `runtime_gauntlet_remaining_boss_public_event_creature_death_credit_tested_pending_arena_order_spawn_route_and_client_smoke` | 16 |
| `runtime_gauntlet_selected_boss_public_event_creature_death_credit_tested_pending_arena_order_spawn_route_and_client_smoke` | 16 |
| `runtime_public_event_event_owned_unit_kill_credit_tested_pending_event_spawn_smoke` | 167 |
| `runtime_public_event_exterminate_target_group_kill_credit_tested_pending_spawn_event_smoke` | 41 |
| `runtime_public_event_kill_cluster_target_group_credit_tested_pending_spawn_event_smoke` | 32 |
| `runtime_public_event_kill_target_group_credit_tested_pending_spawn_event_smoke` | 141 |
| `runtime_public_event_mission_child_event_template_routing_tested_pending_transition_ui_and_client_smoke` | 37 |
| `runtime_public_event_participants_trigger_volume_world_location_credit_tested_pending_trigger_placement_smoke` | 82 |
| `runtime_public_event_turnstile_world_location_entry_credit_tested_pending_trigger_placement_smoke` | 22 |
| `runtime_public_event_virtual_collect_loot_delivery_credit_tested_pending_source_smoke` | 9 |
| `runtime_ruins_battlesworn_darkwitch_public_event_creature_death_credit_tested_pending_route_spawn_rewards_and_client_smoke` | 4 |
| `runtime_ruins_drokk_public_event_creature_spawn_and_death_credit_tested_pending_boss_mechanics_rewards_and_client_smoke` | 2 |
| `runtime_ruins_eldan_data_storage_public_event_creature_activation_credit_tested_pending_route_visual_state_and_client_smoke` | 1 |
| `runtime_ruins_eldan_phase_monitor_public_event_creature_activation_credit_tested_pending_route_visual_state_and_client_smoke` | 1 |
| `runtime_ruins_forgemaster_trogun_public_event_creature_spawn_and_death_credit_tested_pending_mechanics_rewards_and_client_smoke` | 1 |
| `runtime_ruins_grond_public_event_creature_spawn_and_death_credit_tested_pending_boss_mechanics_rewards_and_client_smoke` | 2 |
| `runtime_ruins_gurka_public_event_creature_death_credit_tested_pending_choreography_rewards_and_client_smoke` | 2 |
| `runtime_ruins_kel_voreth_forge_public_event_creature_activation_credit_tested_pending_route_visual_state_and_client_smoke` | 1 |
| `runtime_ruins_war_supplies_public_event_creature_activation_credit_tested_pending_route_visual_state_and_client_smoke` | 1 |
| `runtime_sanctuary_corrupted_deathbringer_dareia_public_event_creature_death_credit_tested_pending_route_combat_client_smoke` | 2 |
| `runtime_sanctuary_corrupted_edgesmith_torian_public_event_creature_death_credit_tested_pending_route_combat_client_smoke` | 2 |
| `runtime_sanctuary_corrupted_lifecaller_khalee_public_event_creature_death_credit_tested_pending_route_combat_client_smoke` | 2 |
| `runtime_sanctuary_deadringer_shallaos_public_event_creature_death_credit_tested_pending_boss_mechanics_spawn_rewards_and_client_smoke` | 2 |
| `runtime_sanctuary_distracted_moldwood_mauler_script_objective_credit_tested_pending_route_spawn_and_client_smoke` | 1 |
| `runtime_sanctuary_elder_moldwood_ravager_public_event_creature_death_credit_tested_pending_route_combat_client_smoke` | 2 |
| `runtime_sanctuary_hammerfist_moldjaw_public_event_creature_death_credit_tested_pending_route_combat_client_smoke` | 2 |
| `runtime_sanctuary_lifeweaver_guardian_public_event_creature_death_credit_tested_pending_route_combat_client_smoke` | 2 |
| `runtime_sanctuary_lifeweaver_tech_cluster_public_event_creature_checklist_credit_tested_pending_placement_visual_state_and_client_smoke` | 1 |
| `runtime_sanctuary_moldwood_overlord_skash_public_event_creature_death_credit_tested_pending_route_boss_mechanics_rewards_and_client_smoke` | 2 |
| `runtime_sanctuary_ondu_lifeweaver_public_event_creature_death_credit_tested_pending_boss_mechanics_spawn_rewards_and_client_smoke` | 2 |
| `runtime_sanctuary_rayna_darkspeaker_public_event_creature_death_credit_tested_pending_route_boss_mechanics_rewards_and_client_smoke` | 2 |
| `runtime_sanctuary_soul_spore_script_objective_credit_tested_pending_route_spawn_and_client_smoke` | 1 |
| `runtime_sanctuary_spiritmother_selene_corrupted_public_event_creature_death_credit_tested_pending_boss_mechanics_rewards_and_client_smoke` | 2 |
| `runtime_sanctuary_torine_totem_flame_public_event_creature_checklist_credit_tested_pending_placement_visual_state_and_client_smoke` | 1 |
| `runtime_sanctuary_zealous_torine_public_event_creature_death_credit_tested_pending_spawn_count_route_client_smoke` | 4 |
| `runtime_skullcano_bosun_octog_public_event_creature_death_credit_tested_pending_boss_mechanics_rewards_and_client_smoke` | 2 |
| `runtime_skullcano_gold_treasure_public_event_creature_checklist_credit_tested_pending_route_placement_visual_state_and_client_smoke` | 2 |
| `runtime_skullcano_grim_grim_sack_public_event_creature_checklist_credit_tested_pending_placement_visual_state_and_client_smoke` | 1 |
| `runtime_skullcano_missile_panel_public_event_creature_checklist_credit_tested_pending_placement_visual_state_and_client_smoke` | 1 |
| `runtime_skullcano_mordechai_redmoon_public_event_creature_death_credit_tested_pending_boss_mechanics_rewards_and_client_smoke` | 4 |
| `runtime_skullcano_quartermaster_gruhar_public_event_creature_death_credit_tested_pending_route_boss_mechanics_rewards_and_client_smoke` | 4 |
| `runtime_skullcano_redmoon_marauder_public_event_creature_nested_target_group_credit_tested_pending_spawn_count_rewards_and_client_smoke` | 41 |
| `runtime_skullcano_thunderfoot_public_event_creature_death_credit_tested_pending_boss_mechanics_rewards_and_client_smoke` | 2 |
| `runtime_skullcano_tugga_public_event_creature_death_credit_tested_pending_failure_condition_rewards_and_client_smoke` | 2 |
| `runtime_space_madness_air_scrubber_controls_public_event_creature_spawned_and_credit_tested_pending_visual_rewards_and_client_smoke` | 1 |
| `runtime_space_madness_captain_tero_public_event_creature_spawn_and_talk_credit_tested_pending_dialog_rewards_and_client_smoke` | 2 |
| `runtime_space_madness_datapad_public_event_creature_spawned_and_checklist_credit_tested_pending_visual_rewards_and_client_smoke` | 1 |
| `runtime_space_madness_engineering_computer_public_event_creature_spawn_and_credit_tested_pending_visual_rewards_and_client_smoke` | 1 |
| `runtime_space_madness_escaped_experiment_public_event_creature_spawned_and_collection_credit_tested_pending_route_rewards_and_client_smoke` | 3 |
| `runtime_space_madness_hazmat_control_panel_public_event_creature_spawn_and_credit_tested_pending_visual_rewards_and_client_smoke` | 1 |
| `runtime_space_madness_hazmat_suit_public_event_creature_spawn_and_direct_credit_tested_pending_visual_rewards_and_client_smoke` | 1 |
| `runtime_space_madness_livestock_public_event_creature_spawn_and_death_credit_tested_pending_combat_rewards_and_client_smoke` | 2 |
| `runtime_space_madness_major_lee_public_event_creature_spawn_and_talk_credit_tested_pending_dialog_rewards_and_client_smoke` | 2 |
| `runtime_space_madness_observation_deck_computer_public_event_creature_spawn_and_credit_tested_pending_visual_rewards_and_client_smoke` | 1 |
| `runtime_space_madness_panicked_worker_nightmare_public_event_creature_spawned_and_death_credit_tested_pending_worker_state_rewards_and_client_smoke` | 12 |
| `runtime_stormtalon_aethros_public_event_creature_death_credit_tested_pending_boss_mechanics_rewards_and_client_smoke` | 2 |
| `runtime_stormtalon_arcanist_breeze_binder_public_event_creature_death_credit_tested_pending_route_combat_client_smoke` | 2 |
| `runtime_stormtalon_blade_wind_public_event_creature_death_credit_tested_pending_boss_mechanics_rewards_and_client_smoke` | 2 |
| `runtime_stormtalon_final_boss_public_event_creature_death_credit_tested_pending_boss_mechanics_rewards_and_client_smoke` | 2 |
| `runtime_stormtalon_improvement_platform_public_event_creature_spawned_and_activation_credit_tested_pending_route_visual_state_and_client_smoke` | 1 |
| `runtime_stormtalon_launch_pad_public_event_creature_spawned_and_checklist_credit_tested_pending_route_visual_state_and_client_smoke` | 1 |
| `runtime_stormtalon_overseer_drift_catcher_public_event_creature_death_credit_tested_pending_route_combat_client_smoke` | 2 |
| `runtime_stormtalon_tainted_flower_stem_public_event_creature_spawned_and_checklist_credit_tested_pending_route_visual_state_and_client_smoke` | 1 |
| `runtime_stormtalon_thundercall_cage_public_event_creature_spawned_and_checklist_credit_tested_pending_route_visual_state_and_client_smoke` | 1 |
| `runtime_stormtalon_thundercall_data_altar_public_event_creature_spawned_and_checklist_credit_tested_pending_route_visual_state_and_client_smoke` | 1 |
| `runtime_stormtalon_thundercall_storm_totem_public_event_creature_spawned_and_activation_credit_tested_pending_route_visual_state_and_client_smoke` | 1 |
| `runtime_up_bevorage_public_event_creature_death_credit_tested_pending_route_boss_rewards_and_client_smoke` | 2 |
| `runtime_up_cage_console_public_event_creature_spawned_and_activation_credit_tested_pending_prototentiary_client_smoke` | 1 |
| `runtime_up_gate_console_public_event_creature_spawned_and_activation_credit_tested_pending_alias_client_smoke` | 1 |
| `runtime_up_gilded_fowl_public_event_creature_spawned_and_death_credit_tested_pending_power_plunge_client_smoke` | 1 |
| `runtime_up_misplaced_mammoth_public_event_creature_spawned_and_death_credit_tested_pending_route_boss_client_smoke` | 1 |
| `runtime_up_mondos_crate_public_event_creature_spawned_and_death_credit_tested_pending_route_timer_client_smoke` | 1 |
| `runtime_up_mondos_monstrosity_public_event_creature_spawned_and_death_credit_tested_pending_route_boss_client_smoke` | 1 |
| `runtime_up_ruffles_public_event_creature_spawned_and_death_credit_tested_pending_route_hunt_client_smoke` | 1 |
| `runtime_up_tank_room_public_event_creature_spawned_and_death_credit_tested_pending_timer_visual_client_smoke` | 4 |
| `runtime_up_warden_public_event_creature_spawned_and_death_credit_tested_pending_prototentiary_client_smoke` | 1 |

## Public Event Auxiliary Evidence Summary

- Total public-event auxiliary evidence rows: 162

| Evidence kind | Rows |
| --- | ---: |
| `public_event_custom_stat` | 24 |
| `public_event_depot` | 36 |
| `public_event_objective_bomb_deployment` | 6 |
| `public_event_objective_gather_resource` | 4 |
| `public_event_objective_state` | 1 |
| `public_event_reward_modifier` | 6 |
| `public_event_stat_display` | 10 |
| `public_event_virtual_item_depot` | 11 |
| `public_event_vote` | 64 |

| Public event link status | Rows |
| --- | ---: |
| `current_public_event` | 12 |
| `type_or_object_scoped` | 150 |

| Evidence status | Rows |
| --- | ---: |
| `blocked_public_event_bomb_deployment_objective_linked_missing_carrier_deploy_evidence` | 6 |
| `blocked_public_event_depot_objective_linked_missing_depot_interaction_evidence` | 29 |
| `blocked_public_event_depot_unlinked_missing_objective_mapping` | 6 |
| `blocked_public_event_gather_resource_objective_linked_missing_resource_carry_deposit_evidence` | 4 |
| `blocked_public_event_reward_modifier_event_linked_missing_reward_property_timing_evidence` | 6 |
| `blocked_public_event_state_objective_linked_missing_state_progression_evidence` | 1 |
| `blocked_public_event_virtual_item_depot_unlinked_missing_objective_or_depot_mapping` | 9 |
| `blocked_public_event_vote_row_unlinked_missing_script_mapping` | 63 |
| `runtime_public_event_custom_stat_event_linked_scoreboard_dependency_tested_pending_stat_producer_smoke` | 4 |
| `runtime_public_event_custom_stat_type_scoped_scoreboard_dependency_tested_pending_stat_producer_smoke` | 20 |
| `runtime_public_event_depot_virtual_item_choices_tested_pending_depot_interaction_smoke` | 1 |
| `runtime_public_event_stat_display_event_linked_standard_combat_stats_tested_pending_display_flags_and_client_smoke` | 2 |
| `runtime_public_event_stat_display_type_scoped_standard_combat_stats_tested_pending_event_mapping_flags_and_client_smoke` | 8 |
| `runtime_public_event_virtual_item_depot_interact_depot_choices_tested_pending_depot_acquisition_smoke` | 1 |
| `runtime_public_event_virtual_item_depot_objective_linked_virtual_collect_credit_tested_pending_depot_acquisition_smoke` | 1 |
| `runtime_public_event_vote_script_referenced_vote_flow_tested_pending_choice_semantics_and_client_smoke` | 1 |

## Challenge DataMapping Evidence Summary

- Total DataMapping challenge evidence rows: 22721

| Evidence kind | Rows |
| --- | ---: |
| `challenge` | 406 |
| `challenge_creature` | 5779 |
| `challenge_reward_item` | 16536 |

| Challenge link status | Rows |
| --- | ---: |
| `current_challenge` | 22721 |

| Match status | Rows |
| --- | ---: |
| `ambiguous_name` | 831 |
| `matched` | 16942 |
| `reviewed` | 174 |
| `scored_name` | 788 |
| `unique_name` | 3871 |
| `unmatched` | 115 |

| Evidence status | Rows |
| --- | ---: |
| `datamapping_challenge_creature_ambiguous_pending_review` | 831 |
| `datamapping_challenge_creature_runtime_hook_tested_pending_review_and_spawn_smoke` | 4659 |
| `datamapping_challenge_creature_unmatched_blocked` | 115 |
| `datamapping_challenge_direct_reward_item_pending_reward_track_bridge` | 3732 |
| `runtime_challenge_creature_target_group_hook_tested_pending_spawn_smoke` | 174 |
| `runtime_challenge_lifecycle_progress_tier_reward_track_tested_pending_content_smoke` | 406 |
| `runtime_challenge_reward_track_item_grant_tested_pending_ui_selection_smoke` | 12804 |

## Path Mission DataMapping Evidence Summary

- Total DataMapping path mission evidence rows: 4623

| Evidence kind | Rows |
| --- | ---: |
| `path_episode` | 93 |
| `path_episode_mission` | 976 |
| `path_episode_reward` | 72 |
| `path_episode_zone` | 176 |
| `path_mission` | 1064 |
| `path_mission_creature` | 2242 |

| Path mission link status | Rows |
| --- | ---: |
| `current_path_mission` | 4241 |
| `no_path_mission_reference` | 341 |
| `non_current_path_mission_reference` | 41 |

| Path episode link status | Rows |
| --- | ---: |
| `current_path_episode` | 1245 |
| `no_path_episode_reference` | 3314 |
| `non_current_path_episode_reference` | 64 |

| Evidence status | Rows |
| --- | ---: |
| `blocked_path_episode_mission_missing_episode_bridge` | 16 |
| `blocked_path_episode_unmatched_bridge` | 13 |
| `blocked_path_episode_zone_missing_episode_bridge` | 43 |
| `blocked_path_mission_creature_ambiguous_requires_bridge_review` | 265 |
| `blocked_path_mission_creature_missing_client_mission` | 25 |
| `blocked_path_mission_creature_unmatched_bridge` | 48 |
| `implemented_northern_wilds_path_episode_activation_verified` | 4 |
| `implemented_northern_wilds_path_episode_mission_activation_verified` | 13 |
| `implemented_northern_wilds_path_mission_creature_credit_verified` | 17 |
| `implemented_northern_wilds_path_mission_runtime_verified` | 13 |
| `implemented_northern_wilds_runtime_episode_zone_bridge_verified` | 4 |
| `mapped_only_path_episode_blocked_runtime_zone_bridge_and_progression_smoke` | 76 |
| `mapped_only_path_episode_mission_blocked_activation_order_objective_smoke` | 947 |
| `mapped_only_path_episode_reward_item_blocked_reward_grant_presentation_evidence` | 68 |
| `mapped_only_path_episode_zone_blocked_runtime_visibility_smoke` | 129 |
| `mapped_only_path_mission_blocked_runtime_owner_and_path_smoke` | 1051 |
| `mapped_only_path_mission_creature_blocked_review_and_spawn_credit_smoke` | 1745 |
| `mapped_only_reviewed_path_mission_creature_blocked_spawn_credit_smoke` | 142 |
| `runtime_northern_wilds_path_episode_reward_type2_grant_tested_pending_presentation_smoke` | 4 |

## Contract DataMapping Evidence Summary

- Total DataMapping contract evidence rows: 3666

| Evidence kind | Rows |
| --- | ---: |
| `contract` | 42 |
| `contract_creature` | 2888 |
| `contract_reward_item` | 736 |

| Quest link status | Rows |
| --- | ---: |
| `current_quest` | 2930 |
| `not_quest_scoped` | 736 |

| Match status | Rows |
| --- | ---: |
| `ambiguous_name` | 372 |
| `matched` | 778 |
| `reviewed` | 52 |
| `scored_name` | 481 |
| `unique_name` | 1945 |
| `unmatched` | 38 |

| Evidence status | Rows |
| --- | ---: |
| `datamapping_contract_creature_ambiguous_pending_review` | 372 |
| `datamapping_contract_creature_unmatched_blocked` | 38 |
| `implemented_contract_creature_reviewed_bridge_objective_credit_pending_spawn_client_smoke` | 52 |
| `implemented_contract_quest_manager_backed_pending_rotation_client_smoke` | 42 |
| `mapped_only_contract_creature_pending_bridge_review_spawn_client_smoke` | 2426 |
| `mapped_only_contract_reward_track_item_pending_choice_presentation_client_smoke` | 736 |

## Instance Portal Evidence Summary

- Total instance portal evidence rows: 282

| Evidence kind | Rows |
| --- | ---: |
| `instance_portal_creature` | 264 |
| `instance_portal_definition` | 18 |

| Portal link status | Rows |
| --- | ---: |
| `client_creature_linked` | 263 |
| `creature_references_missing_portal_definition` | 1 |
| `definition_without_creature_link` | 18 |

| DataMapping bridge status | Rows |
| --- | ---: |
| `candidate_datamapping_creature_bridge` | 64 |
| `missing_datamapping_creature_bridge` | 180 |
| `no_client_creature_link` | 18 |
| `reviewed_datamapping_creature_bridge` | 20 |

| Evidence status | Rows |
| --- | ---: |
| `client_instance_portal_creature_link_pending_world_placement_review` | 180 |
| `client_instance_portal_definition_without_creature_link_blocked` | 18 |
| `datamapping_instance_portal_creature_pending_review_and_entry_smoke` | 64 |
| `reviewed_instance_portal_creature_pending_entry_smoke` | 20 |

## Script Presentation Evidence Summary

- Total script communicator/cinematic evidence rows: 107

| Evidence kind | Rows |
| --- | ---: |
| `script_cinematic_finish_hook` | 4 |
| `script_cinematic_reference` | 29 |
| `script_communicator_message` | 74 |

| Script scope | Rows |
| --- | ---: |
| `script_instance` | 96 |
| `script_main` | 11 |

| Client/runtime link status | Rows |
| --- | ---: |
| `client_row_matched` | 78 |
| `missing_runtime_cinematic_id` | 7 |
| `runtime_callback_without_exact_client_id_gate` | 4 |
| `zero_runtime_cinematic_id` | 18 |

| Runtime payload status | Rows |
| --- | ---: |
| `` | 74 |
| `runtime_finish_callback_present` | 4 |
| `runtime_payload_present` | 4 |
| `runtime_placeholder_or_wip_payload` | 25 |

| Evidence status | Rows |
| --- | ---: |
| `script_cinematic_finish_hook_pending_exact_cinematic_id_gate_and_smoke` | 4 |
| `script_cinematic_placeholder_pending_payload_mapping` | 25 |
| `script_cinematic_runtime_payload_present_pending_trigger_timing_client_smoke` | 4 |
| `script_communicator_message_client_row_matched_pending_trigger_targeting_ui_smoke` | 74 |

## Script Public Event Flow Evidence Summary

- Total script public-event flow evidence rows: 1785

| Evidence kind | Rows |
| --- | ---: |
| `script_public_event_objective_reference` | 1101 |
| `script_public_event_phase_reference` | 684 |

| Script scope | Rows |
| --- | ---: |
| `script_instance` | 1785 |

| Reference link status | Rows |
| --- | ---: |
| `client_row_matched` | 1101 |
| `script_enum_value_resolved` | 684 |

| Owner link status | Rows |
| --- | ---: |
| `script_owner_is_related_public_event` | 5 |
| `script_owner_matches_objective_public_event` | 887 |
| `script_owner_matches_public_event` | 676 |
| `script_owner_not_public_event` | 217 |

| Evidence status | Rows |
| --- | ---: |
| `runtime_up_downsizer_defeat_objective_activation_flow_tested` | 1 |
| `runtime_up_downsizer_defeat_success_finish_boundary_tested` | 1 |
| `runtime_up_downsizer_electromagnetic_induction_activation_flow_tested` | 1 |
| `runtime_up_downsizer_electrostatic_dynamo_activation_flow_tested` | 1 |
| `runtime_up_downsizer_objective_credit_entity_flow_tested` | 1 |
| `runtime_up_downsizer_overcharge_activation_flow_tested` | 1 |
| `runtime_up_downsizer_voltaic_conversion_activation_flow_tested` | 1 |
| `script_public_event_objective_client_row_matched_pending_owner_flow_review` | 213 |
| `script_public_event_objective_parent_event_matched_pending_activation_credit_smoke` | 881 |
| `script_public_event_phase_script_state_owner_matched_pending_phase_order_smoke` | 676 |
| `script_public_event_phase_script_state_pending_owner_mapping_and_smoke` | 8 |

## Script Objective Producer Evidence Summary

- Total script objective producer evidence rows: 863

| Evidence kind | Rows |
| --- | ---: |
| `script_activate_objective_call` | 467 |
| `script_activate_objective_helper_call` | 38 |
| `script_objective_credit_base` | 164 |
| `script_typed_objective_credit_base` | 33 |
| `script_update_objective_call` | 156 |
| `script_update_objective_helper_call` | 2 |
| `script_update_objective_inherited_trigger` | 3 |

| Script scope | Rows |
| --- | ---: |
| `script_instance` | 862 |
| `script_main` | 1 |

| Producer targeting | Rows |
| --- | ---: |
| `direct_objective` | 756 |
| `typed_objective` | 97 |
| `unresolved` | 10 |

| Script scope | Client objective type | Rows |
| --- | --- | ---: |
| `script_instance` | `ActivateTargetGroup` | 78 |
| `script_instance` | `ActivateTargetGroupChecklist` | 79 |
| `script_instance` | `CollectItem` | 1 |
| `script_instance` | `Exterminate` | 20 |
| `script_instance` | `KillClusterEventObjectiveUnit` | 16 |
| `script_instance` | `KillClusterTargetGroup` | 24 |
| `script_instance` | `KillEventObjectiveUnit` | 45 |
| `script_instance` | `KillEventUnit` | 7 |
| `script_instance` | `KillTargetGroup` | 179 |
| `script_instance` | `ParticipantsInTriggerVolume` | 90 |
| `script_instance` | `ResourcePool` | 18 |
| `script_instance` | `Script` | 166 |
| `script_instance` | `ScriptWithoutCount` | 23 |
| `script_instance` | `ScriptWithoutMax` | 14 |
| `script_instance` | `TalkTo` | 36 |
| `script_instance` | `TalkToChecklist` | 3 |
| `script_instance` | `TimedWin` | 16 |
| `script_instance` | `Turnstile` | 17 |
| `script_instance` | `VirtualCollect` | 7 |
| `script_instance` | `unresolved` | 10 |
| `script_main` | `KillEventObjectiveUnit` | 1 |

| Reference link status | Rows |
| --- | ---: |
| `direct_objective_row_matched` | 743 |
| `no_client_objective_match` | 10 |
| `typed_objective_row_matched` | 97 |
| `unresolved_symbol` | 13 |

| Owner link status | Rows |
| --- | ---: |
| `script_owner_entity_filter_mapped_to_public_event` | 213 |
| `script_owner_entity_or_trigger` | 13 |
| `script_owner_is_different_public_event` | 2 |
| `script_owner_matches_public_event` | 539 |
| `script_owner_unknown` | 96 |

| Evidence status | Rows |
| --- | ---: |
| `direct_objective_id_matches_client_kill_type_pending_owner_and_script_type_review` | 55 |
| `direct_objective_id_matches_client_kill_type_pending_timing_and_script_type_review` | 75 |
| `mapped_fragment_zero_skeech_ambush_activation_only_blocked_missing_wave_completion_producer_client_smoke` | 1 |
| `mapped_gauntlet_activation_only_objective_blocked_missing_completion_score_client_smoke` | 4 |
| `mapped_initialization_core_evolutionary_revolutions_activation_only_blocked_missing_augmentation_state_client_smoke` | 1 |
| `mapped_initialization_core_immortal_prime_operants_activation_only_blocked_missing_party_death_fail_state_client_smoke` | 1 |
| `mapped_initialization_core_prime_operants_activation_only_blocked_missing_boss_death_credit_producer_client_smoke` | 1 |
| `mapped_outpost_m13_search_mine_activation_only_blocked_missing_trigger_completion_client_smoke` | 1 |
| `mapped_space_madness_air_scrubbing_activation_only_blocked_missing_wave_timer_credit_producer_client_smoke` | 1 |
| `mapped_space_madness_rowsdower_avoidance_activation_only_blocked_missing_hit_failure_producer_client_smoke` | 1 |
| `mapped_space_madness_survive_nightmares_activation_only_blocked_missing_wave_credit_producer_client_smoke` | 1 |
| `mapped_up_downsizer_electromagnetic_induction_activation_only_blocked_missing_ability_mechanics_client_smoke` | 1 |
| `mapped_up_downsizer_electrostatic_dynamo_activation_only_blocked_missing_ability_mechanics_client_smoke` | 1 |
| `mapped_up_downsizer_overcharge_activation_only_blocked_missing_ability_mechanics_client_smoke` | 1 |
| `mapped_up_downsizer_voltaic_conversion_activation_only_blocked_missing_ability_mechanics_client_smoke` | 1 |
| `runtime_bay_betrayal_intro_herald_speech_script_credit_tested_pending_dialog_timing_smoke` | 2 |
| `runtime_bay_betrayal_intro_herald_talk_activation_tested` | 1 |
| `runtime_bay_betrayal_intro_protect_herald_activation_tested_pending_encounter_smoke` | 1 |
| `runtime_bay_betrayal_parent_survive_trials_activation_tested_pending_trial_branch_smoke` | 1 |
| `runtime_bevorage_boss_death_objective_credit_tested` | 1 |
| `runtime_bevorage_caffeinated_child_objective_death_credit_tested` | 1 |
| `runtime_bevorage_caffeinated_child_objective_phase_activation_tested` | 1 |
| `runtime_bevorage_defeat_objective_phase_activation_tested` | 1 |
| `runtime_bevorage_out_of_order_child_objective_death_credit_tested` | 1 |
| `runtime_bevorage_out_of_order_child_objective_phase_activation_tested` | 1 |
| `runtime_bevorage_vendatron_objective_phase_activation_tested` | 1 |
| `runtime_bevorage_vendatron_target_group_activation_credit_tested` | 1 |
| `runtime_coldblood_liquid_soulfrost_sample_checklist_activation_credit_tested_pending_placement_visual_state_and_client_smoke` | 1 |
| `runtime_coldblood_rally_pell_drum_checklist_activation_credit_tested_pending_placement_repeat_and_client_smoke` | 1 |
| `runtime_coldblood_soulfrost_shards_checklist_activation_credit_tested_pending_placement_visual_state_and_client_smoke` | 1 |
| `runtime_coldblood_soulfrost_trap_checklist_activation_credit_tested_pending_placement_visual_state_and_client_smoke` | 1 |
| `runtime_coldblood_soulrot_canister_checklist_activation_credit_tested_pending_placement_visual_state_and_client_smoke` | 1 |
| `runtime_coldblood_winterfury_prisoner_cage_checklist_activation_credit_tested_pending_placement_visual_state_and_client_smoke` | 1 |
| `runtime_crimelords_parent_avenge_blood_scions_activation_tested_pending_boss_smoke` | 1 |
| `runtime_crimelords_parent_biggest_gangsters_activation_tested_pending_loyalty_branch_smoke` | 1 |
| `runtime_crimelords_parent_final_onslaught_activation_tested_pending_wave_smoke` | 1 |
| `runtime_crimelords_parent_hoverbike_objective_activation_tested_pending_vehicle_smoke` | 1 |
| `runtime_crimelords_parent_murder_investigation_activation_tested_pending_branch_producers` | 1 |
| `runtime_crimelords_parent_return_clubhouse_activation_tested_pending_finale_routing_smoke` | 1 |
| `runtime_drive_diagnostics_objective_activated_tested_pending_visual_state_and_client_smoke` | 1 |
| `runtime_drive_diagnostics_target_group_activation_credit_tested_pending_visual_state_and_client_smoke` | 1 |
| `runtime_ether_charged_ravenous_exterminate_credit_tested` | 1 |
| `runtime_ether_charged_ravenous_objective_activation_tested` | 1 |
| `runtime_evil_ether_airlock_gather_activation_and_wip_trigger_spawn_tested_pending_exact_route_and_client_smoke` | 1 |
| `runtime_evil_ether_bridge_access_hall_activation_and_trigger_spawn_tested_pending_exact_route_and_client_smoke` | 1 |
| `runtime_evil_ether_bridge_access_portal_objective_activation_tested_pending_spawn_cadence_and_client_smoke` | 1 |
| `runtime_evil_ether_bridge_portals_objective_activation_tested_pending_three_portal_spawn_cadence_and_client_smoke` | 1 |
| `runtime_evil_ether_captain_weir_final_talk_activation_tested` | 1 |
| `runtime_evil_ether_captain_weir_initial_talk_activation_tested` | 1 |
| `runtime_evil_ether_captain_weir_talk_target_group_credit_tested` | 1 |
| `runtime_evil_ether_crew_log_credit_and_communicator_tested` | 1 |
| `runtime_evil_ether_crew_logs_objective_activation_tested` | 1 |
| `runtime_evil_ether_crew_quarters_door_entry_credit_tested_pending_door_route_and_client_smoke` | 1 |
| `runtime_evil_ether_crew_quarters_objective_activation_tested_pending_door_route_and_client_smoke` | 1 |
| `runtime_evil_ether_drive_schematics_objective_activation_tested_pending_drop_visual_and_client_smoke` | 1 |
| `runtime_evil_ether_drive_schematics_pickup_credit_tested_pending_drop_visual_and_client_smoke` | 1 |
| `runtime_evil_ether_escape_teleporter_phase_activation_and_trigger_spawn_tested_pending_exact_destination_and_client_smoke` | 1 |
| `runtime_evil_ether_escape_teleporter_trigger_credit_tested_pending_exact_destination_and_client_smoke` | 1 |
| `runtime_evil_ether_feasting_organism_activation_tested` | 1 |
| `runtime_evil_ether_feasting_organism_target_group_death_credit_tested` | 1 |
| `runtime_evil_ether_find_teleporter_activation_and_turnstile_spawn_tested_pending_exact_route_and_client_smoke` | 1 |
| `runtime_evil_ether_gather_teleporter_activation_and_trigger_spawn_tested_pending_exact_route_and_client_smoke` | 1 |
| `runtime_evil_ether_generator_alpha_activation_tested` | 1 |
| `runtime_evil_ether_generator_beta_activation_tested` | 1 |
| `runtime_evil_ether_gold_timer_activation_and_final_credit_tested_pending_timer_medal_and_client_smoke` | 2 |
| `runtime_evil_ether_katja_zarkhov_activation_tested` | 1 |
| `runtime_evil_ether_katja_zarkhov_death_credit_tested` | 1 |
| `runtime_evil_ether_medbay_generator_activation_tested` | 1 |
| `runtime_evil_ether_medbay_generator_controls_credit_tested` | 1 |
| `runtime_evil_ether_open_medbay_activation_tested` | 1 |
| `runtime_evil_ether_open_medbay_door_control_credit_tested` | 1 |
| `runtime_evil_ether_primary_power_plant_activation_and_seed_tested_pending_trigger_replay_and_client_smoke` | 4 |
| `runtime_evil_ether_primary_power_plant_trigger_credit_tested_pending_trigger_replay_and_client_smoke` | 2 |
| `runtime_evil_ether_repair_door_activation_tested` | 1 |
| `runtime_evil_ether_repaired_medbay_door_control_credit_tested` | 1 |
| `runtime_evil_ether_restart_generators_activation_tested` | 1 |
| `runtime_evil_ether_security_chief_activation_tested` | 1 |
| `runtime_evil_ether_security_chief_death_credit_tested` | 1 |
| `runtime_evil_ether_self_destruct_activation_tested_pending_controls_visual_and_client_smoke` | 1 |
| `runtime_evil_ether_self_destruct_controls_target_group_credit_tested_pending_controls_visual_and_client_smoke` | 1 |
| `runtime_evil_ether_shades_bridge_activation_and_trigger_spawn_tested_pending_exact_route_and_client_smoke` | 1 |
| `runtime_evil_ether_spare_parts_checklist_activation_tested` | 1 |
| `runtime_evil_ether_spare_parts_checklist_credit_tested` | 1 |
| `runtime_evil_ether_teleporter_controls_checklist_credit_tested_pending_visual_and_client_smoke` | 1 |
| `runtime_evil_ether_teleporter_organism_activation_tested` | 1 |
| `runtime_evil_ether_teleporter_organism_target_group_death_credit_tested` | 1 |
| `runtime_evil_ether_upper_deck_teleport_phase_activation_and_trigger_spawn_tested_pending_exact_destination_and_client_smoke` | 1 |
| `runtime_evil_ether_upper_deck_teleport_trigger_credit_tested_pending_exact_destination_and_client_smoke` | 1 |
| `runtime_fragment_zero_automated_defense_panel_checklist_activation_tested` | 1 |
| `runtime_fragment_zero_automated_defense_panel_checklist_entity_credit_tested` | 1 |
| `runtime_fragment_zero_biomatics_airlock_activation_tested_pending_airlock_trigger_and_client_smoke` | 1 |
| `runtime_fragment_zero_biomatics_search_activation_and_grid_trigger_spawn_tested_pending_exact_trigger_replay_and_client_smoke` | 1 |
| `runtime_fragment_zero_biomatics_search_grid_trigger_credit_tested_pending_exact_trigger_replay_and_client_smoke` | 1 |
| `runtime_fragment_zero_black_box_activation_tested_pending_object_placement_credit_and_client_smoke` | 1 |
| `runtime_fragment_zero_cargo_crate_100_count_activation_tested` | 1 |
| `runtime_fragment_zero_cargo_crate_10_count_activation_tested` | 1 |
| `runtime_fragment_zero_cargo_crate_shared_virtualcollect_credit_tested` | 1 |
| `runtime_fragment_zero_continue_search_hugo_activation_tested_pending_hugo_route_and_client_smoke` | 1 |
| `runtime_fragment_zero_continue_search_missing_crew_activation_tested_pending_branch_credit_timing_and_client_smoke` | 1 |
| `runtime_fragment_zero_follow_friendly_skeech_activation_and_wip_trigger_spawn_tested_pending_exact_trigger_replay_and_client_smoke` | 1 |
| `runtime_fragment_zero_gold_medal_timer_activation_and_final_credit_tested_pending_timer_reward_and_client_smoke` | 2 |
| `runtime_fragment_zero_hidden_recordings_parent_activation_tested` | 1 |
| `runtime_fragment_zero_hidden_recordings_parent_entity_credit_tested` | 2 |
| `runtime_fragment_zero_hugo_aura_talk_objective_activation_tested_pending_stay_close_and_client_smoke` | 1 |
| `runtime_fragment_zero_hugo_final_talk_objective_activation_tested_pending_stay_close_and_client_smoke` | 1 |
| `runtime_fragment_zero_incubation_airlock_activation_tested_pending_airlock_trigger_and_client_smoke` | 1 |
| `runtime_fragment_zero_incubation_search_activation_and_grid_trigger_spawn_tested_pending_exact_trigger_replay_and_client_smoke` | 1 |
| `runtime_fragment_zero_incubation_search_grid_trigger_credit_tested_pending_exact_trigger_replay_and_client_smoke` | 1 |
| `runtime_fragment_zero_jo_corpse_objective_activation_tested_pending_placement_visuals_and_client_smoke` | 1 |
| `runtime_fragment_zero_jo_corpse_target_group_credit_tested_pending_placement_visuals_and_client_smoke` | 1 |
| `runtime_fragment_zero_life_overseer_activation_tested` | 1 |
| `runtime_fragment_zero_life_overseer_airlock_activation_and_wip_trigger_spawn_tested_pending_exact_trigger_replay_and_client_smoke` | 1 |
| `runtime_fragment_zero_life_overseer_death_credit_tested` | 1 |
| `runtime_fragment_zero_life_overseer_search_activation_tested_pending_cargo_route_and_client_smoke` | 1 |
| `runtime_fragment_zero_locate_hugo_objective_activation_tested_pending_projection_placement_and_client_smoke` | 1 |
| `runtime_fragment_zero_locate_hugo_target_group_credit_tested_pending_projection_placement_and_client_smoke` | 1 |
| `runtime_fragment_zero_lucent_hidden_recording_checklist_entity_credit_tested` | 1 |
| `runtime_fragment_zero_lucent_hidden_recording_objective_activation_tested` | 1 |
| `runtime_fragment_zero_ohmna_hidden_recording_checklist_entity_credit_tested` | 1 |
| `runtime_fragment_zero_ohmna_hidden_recording_objective_activation_tested` | 1 |
| `runtime_fragment_zero_project_matron_activation_tested` | 1 |
| `runtime_fragment_zero_project_matron_death_credit_tested` | 1 |
| `runtime_fragment_zero_prototype_alpha_activation_tested` | 1 |
| `runtime_fragment_zero_prototype_alpha_death_credit_tested` | 1 |
| `runtime_fragment_zero_prototype_beta_activation_tested` | 1 |
| `runtime_fragment_zero_prototype_beta_death_credit_tested` | 1 |
| `runtime_fragment_zero_prototype_delta_activation_tested` | 1 |
| `runtime_fragment_zero_prototype_delta_death_credit_tested` | 1 |
| `runtime_fragment_zero_search_crew_turnstile_activation_and_wip_trigger_spawn_tested_pending_exact_trigger_replay_and_client_smoke` | 1 |
| `runtime_fragment_zero_search_for_hugo_activation_tested_pending_projection_route_and_client_smoke` | 1 |
| `runtime_fragment_zero_skeech_horde_base_kill_activation_tested` | 1 |
| `runtime_fragment_zero_skeech_horde_base_kill_death_credit_tested` | 1 |
| `runtime_fragment_zero_skeech_horde_tier1_activation_tested` | 1 |
| `runtime_fragment_zero_skeech_horde_tier1_death_credit_tested` | 1 |
| `runtime_fragment_zero_skeech_horde_tier2_activation_tested` | 1 |
| `runtime_fragment_zero_skeech_horde_tier2_death_credit_tested` | 1 |
| `runtime_fragment_zero_skeech_horde_tier3_activation_tested` | 1 |
| `runtime_fragment_zero_skeech_horde_tier3_death_credit_tested` | 1 |
| `runtime_fragment_zero_stay_close_to_hugo_activation_and_zero_count_credit_tested_pending_exact_follow_timing_and_client_smoke` | 2 |
| `runtime_fragment_zero_syrus_corpse_objective_activation_tested_pending_placement_visuals_and_client_smoke` | 1 |
| `runtime_fragment_zero_syrus_corpse_target_group_credit_tested_pending_placement_visuals_and_client_smoke` | 1 |
| `runtime_fragment_zero_target_group_entity_scripts_credit_panel_and_hidden_recordings_tested` | 6 |
| `runtime_fragment_zero_wait_for_hugo_activation_and_zero_count_credit_tested_pending_hugo_timing_and_client_smoke` | 2 |
| `runtime_fragment_zero_xenobite_egg_direct_script_credit_tested` | 1 |
| `runtime_fragment_zero_xenobite_egg_objective_activation_tested` | 1 |
| `runtime_gauntlet_agent_lex_talk_objective_activation_and_credit_tested` | 1 |
| `runtime_gauntlet_brick_braggor_death_credit_tested_pending_main_event_route_and_client_smoke` | 2 |
| `runtime_gauntlet_championator_death_credit_tested_pending_charnel_chamber_route_and_client_smoke` | 2 |
| `runtime_gauntlet_electric_room_door_lock_activation_credit_tested_pending_door_visual_state_and_client_smoke` | 2 |
| `runtime_gauntlet_goon_squad_death_credit_tested_pending_main_event_route_and_client_smoke` | 2 |
| `runtime_gauntlet_handler_and_pets_death_credit_tested_pending_arena_order_spawn_route_and_client_smoke` | 1 |
| `runtime_gauntlet_judge_kain_talk_objective_activation_and_credit_tested` | 1 |
| `runtime_gauntlet_opening_and_first_arena_credit_tested_pending_arena_route_client_smoke` | 4 |
| `runtime_gauntlet_opposing_faction_team_death_credit_tested_pending_faction_arena_route_and_client_smoke` | 2 |
| `runtime_gauntlet_pilot_taboro_talk_objective_activation_and_credit_tested` | 1 |
| `runtime_gauntlet_product_endorsement_activation_credit_tested_pending_product_side_effects_and_client_smoke` | 2 |
| `runtime_gauntlet_pyro_maniac_death_credit_tested_pending_arena_order_spawn_route_and_client_smoke` | 1 |
| `runtime_gauntlet_rockstar_yeti_death_credit_tested_pending_arena_order_spawn_route_and_client_smoke` | 1 |
| `runtime_gauntlet_shock_king_death_credit_tested_pending_main_event_route_and_client_smoke` | 2 |
| `runtime_gauntlet_showtime_death_credit_tested_pending_arena_order_spawn_route_and_client_smoke` | 1 |
| `runtime_gauntlet_slice_and_dice_death_credit_tested_pending_main_event_route_and_client_smoke` | 2 |
| `runtime_gauntlet_voodoo_kin_death_credit_tested_pending_arena_order_spawn_route_and_client_smoke` | 1 |
| `runtime_hycrest_intro_dropship_briefing_script_credit_tested_pending_dialog_timing_smoke` | 2 |
| `runtime_hycrest_intro_meet_exile_agent_activation_tested_pending_world_placement_smoke` | 1 |
| `runtime_hycrest_intro_report_dropship_activation_tested` | 1 |
| `runtime_hycrest_parent_victory_objective_activation_tested_pending_branch_selection_smoke` | 1 |
| `runtime_infestation_gold_medal_timer_activation_and_final_credit_tested_pending_timer_reward_and_client_smoke` | 2 |
| `runtime_infestation_lashing_fiend_death_credit_tested_pending_wave_choreography_combat_and_client_smoke` | 1 |
| `runtime_infestation_lashing_fiend_jabbithole_spawn_and_objective_activation_tested_pending_wave_choreography_combat_and_client_smoke` | 1 |
| `runtime_initialization_core_quarantine_door_activation_credit_tested_pending_door_choreography_client_smoke` | 1 |
| `runtime_initialization_core_quarantine_door_phase_activation_tested_pending_door_choreography_client_smoke` | 1 |
| `runtime_initialization_core_quarantine_panel_activation_credit_tested_pending_six_panel_client_smoke` | 1 |
| `runtime_initialization_core_quarantine_panel_phase_activation_tested_pending_six_panel_placement_client_smoke` | 1 |
| `runtime_malgrave_exodus_braithwait_kurg_activation_tested_pending_dialog_smoke` | 1 |
| `runtime_malgrave_exodus_braithwait_story_timedwin_activation_tested_pending_timer_dialog_smoke` | 1 |
| `runtime_malgrave_exodus_braithwait_thirsty_creek_activation_tested_pending_dialog_smoke` | 1 |
| `runtime_malgrave_exodus_town_center_survivor_rally_activation_tested_pending_targetgroup_dialog_smoke` | 1 |
| `runtime_malgrave_parent_lead_caravan_activation_tested_pending_route_producers_smoke` | 1 |
| `runtime_omnicore_choose_path_gate_activation_tested_pending_route_selection_smoke` | 1 |
| `runtime_omnicore_opening_axis_listen_script_credit_tested_pending_dialog_timing_smoke` | 2 |
| `runtime_omnicore_opening_axis_talk_activation_tested_pending_dialog_credit_smoke` | 1 |
| `runtime_omnicore_opening_belle_listen_script_credit_tested_pending_dialog_timing_smoke` | 2 |
| `runtime_omnicore_opening_belle_talk_activation_tested_pending_dialog_credit_smoke` | 1 |
| `runtime_outpost_m13_captain_milo_objective_activation_and_spawn_tested_pending_dialog_route_and_client_smoke` | 1 |
| `runtime_outpost_m13_captain_milo_talk_credit_tested_pending_dialog_route_and_client_smoke` | 1 |
| `runtime_outpost_m13_exit_panel_activation_credit_tested_pending_teleport_visual_state_and_client_smoke` | 1 |
| `runtime_outpost_m13_foreman_krause_talk_credit_tested_pending_placement_and_client_smoke` | 1 |
| `runtime_outpost_m13_foreman_krause_talk_objective_activation_tested_pending_placement_and_client_smoke` | 1 |
| `runtime_outpost_m13_gold_medal_timer_activation_and_final_credit_tested_pending_timer_reward_and_client_smoke` | 2 |
| `runtime_outpost_m13_hive_mine_cleanup_death_credit_tested_pending_spawn_density_and_client_smoke` | 1 |
| `runtime_outpost_m13_hive_mine_cleanup_objective_activation_tested_pending_spawn_density_and_client_smoke` | 1 |
| `runtime_outpost_m13_hive_queen_death_credit_tested_pending_boss_spawn_mechanics_and_client_smoke` | 1 |
| `runtime_outpost_m13_hive_queen_objective_activation_tested_pending_boss_spawn_mechanics_and_client_smoke` | 1 |
| `runtime_protogames_last_event_teleporter_trigger_activation_tested_pending_teleport_timing_and_client_smoke` | 1 |
| `runtime_protogames_phineas_meet_trigger_activation_tested_pending_npc_choreography_and_client_smoke` | 1 |
| `runtime_protogames_super_invulnotron_death_credit_tested_pending_combat_and_client_smoke` | 1 |
| `runtime_protogames_super_invulnotron_phase_activation_and_spawn_tested_pending_combat_and_client_smoke` | 1 |
| `runtime_protogames_wrathbone_death_credit_tested_pending_combat_rewards_and_client_smoke` | 1 |
| `runtime_protogames_wrathbone_phase_activation_and_spawn_tested_pending_combat_rewards_and_client_smoke` | 1 |
| `runtime_public_event_checklist_objective_bitfield_routing_tested_pending_content_smoke` | 24 |
| `runtime_redmoon40_laveka_death_credit_finish_tested_pending_combat_rewards_and_client_smoke` | 1 |
| `runtime_redmoon40_laveka_objective_activation_and_spawn_tested_pending_route_status_combat_and_client_smoke` | 1 |
| `runtime_riot_intro_agent_triphon_report_activation_tested` | 1 |
| `runtime_riot_intro_mission_briefing_script_credit_tested_pending_dialog_timing_smoke` | 2 |
| `runtime_riot_intro_situation_briefing_script_credit_tested_pending_dialog_timing_smoke` | 2 |
| `runtime_riot_intro_warden_holostation_activation_tested` | 1 |
| `runtime_riot_intro_warden_office_movement_gate_activation_tested_pending_trigger_smoke` | 1 |
| `runtime_riot_parent_quell_objective_activation_tested_pending_vote_branch_smoke` | 1 |
| `runtime_ruins_battlesworn_darkwitch_target_group_death_credit_tested_pending_spawn_cadence_rewards_and_client_smoke` | 1 |
| `runtime_ruins_darkwitch_gurka_miniboss_death_credit_tested_pending_boss_mechanics_rewards_and_client_smoke` | 1 |
| `runtime_ruins_eldan_data_storage_checklist_activation_credit_tested_pending_optional_route_visual_state_and_client_smoke` | 1 |
| `runtime_ruins_eldan_phase_monitor_checklist_activation_credit_tested_pending_optional_route_visual_state_and_client_smoke` | 1 |
| `runtime_ruins_grond_main_boss_death_credit_tested_pending_boss_mechanics_rewards_and_client_smoke` | 1 |
| `runtime_ruins_kel_voreth_forge_checklist_activation_credit_tested_pending_optional_route_visual_state_and_client_smoke` | 1 |
| `runtime_ruins_slavemaster_drokk_main_boss_death_credit_tested_pending_boss_mechanics_rewards_and_client_smoke` | 1 |
| `runtime_ruins_war_supplies_checklist_activation_credit_tested_pending_optional_route_visual_state_and_client_smoke` | 1 |
| `runtime_sanctuary_corrupted_deathbringer_dareia_miniboss_activation_and_death_credit_tested` | 1 |
| `runtime_sanctuary_corrupted_deathbringer_dareia_optional_phase_activation_tested` | 1 |
| `runtime_sanctuary_corrupted_edgesmith_torian_miniboss_death_credit_tested` | 1 |
| `runtime_sanctuary_corrupted_edgesmith_torian_miniboss_phase_activation_tested` | 1 |
| `runtime_sanctuary_corrupted_lifecaller_khalee_miniboss_death_credit_tested` | 1 |
| `runtime_sanctuary_corrupted_lifecaller_khalee_miniboss_phase_activation_tested` | 1 |
| `runtime_sanctuary_deadringer_shallaos_boss_death_credit_tested` | 1 |
| `runtime_sanctuary_deadringer_shallaos_boss_phase_activation_tested` | 1 |
| `runtime_sanctuary_distracted_moldwood_mauler_optional_phase_activation_tested` | 1 |
| `runtime_sanctuary_distracted_moldwood_mauler_script_objective_death_credit_tested` | 1 |
| `runtime_sanctuary_elder_moldwood_ravager_miniboss_death_credit_tested` | 1 |
| `runtime_sanctuary_elder_moldwood_ravager_miniboss_phase_activation_tested` | 1 |
| `runtime_sanctuary_hammerfist_moldjaw_miniboss_death_credit_tested` | 1 |
| `runtime_sanctuary_hammerfist_moldjaw_miniboss_phase_activation_tested` | 1 |
| `runtime_sanctuary_lifeweaver_guardian_miniboss_death_credit_tested` | 1 |
| `runtime_sanctuary_lifeweaver_guardian_miniboss_phase_activation_tested` | 1 |
| `runtime_sanctuary_lifeweaver_tech_cluster_checklist_activation_credit_tested` | 1 |
| `runtime_sanctuary_lifeweaver_tech_cluster_optional_phase_activation_tested` | 1 |
| `runtime_sanctuary_lifeweaver_terrace_turnstile_phase_activation_tested` | 2 |
| `runtime_sanctuary_lifeweaver_terrace_turnstile_trigger_credit_tested` | 1 |
| `runtime_sanctuary_moldwood_corruption_turnstile_phase_activation_tested` | 1 |
| `runtime_sanctuary_moldwood_corruption_turnstile_trigger_credit_tested` | 1 |
| `runtime_sanctuary_moldwood_overlord_skash_boss_death_credit_tested` | 1 |
| `runtime_sanctuary_moldwood_overlord_skash_boss_phase_activation_tested` | 1 |
| `runtime_sanctuary_ondu_lifeweaver_boss_death_credit_tested` | 1 |
| `runtime_sanctuary_ondu_lifeweaver_boss_phase_activation_tested` | 1 |
| `runtime_sanctuary_rayna_darkspeaker_boss_death_credit_tested` | 1 |
| `runtime_sanctuary_rayna_darkspeaker_boss_phase_activation_tested` | 1 |
| `runtime_sanctuary_skash_prisoner_timer_death_credit_tested` | 1 |
| `runtime_sanctuary_skash_prisoner_timer_phase_activation_tested` | 1 |
| `runtime_sanctuary_soul_spore_optional_phase_activation_tested` | 1 |
| `runtime_sanctuary_soul_spore_script_objective_activation_credit_tested` | 1 |
| `runtime_sanctuary_spirit_relic_holder_checklist_activation_credit_tested` | 1 |
| `runtime_sanctuary_spirit_relic_holder_phase_activation_tested` | 1 |
| `runtime_sanctuary_spiritmother_selene_corrupted_boss_death_credit_tested` | 1 |
| `runtime_sanctuary_spiritmother_selene_corrupted_boss_phase_activation_tested` | 1 |
| `runtime_sanctuary_temple_life_speaker_turnstile_phase_activation_tested` | 1 |
| `runtime_sanctuary_temple_life_speaker_turnstile_trigger_credit_tested` | 1 |
| `runtime_sanctuary_torine_spirit_relic_checklist_activation_credit_tested` | 1 |
| `runtime_sanctuary_torine_spirit_relic_phase_activation_tested` | 1 |
| `runtime_sanctuary_torine_totem_flame_checklist_activation_credit_tested` | 1 |
| `runtime_sanctuary_torine_totem_flame_phase_activation_tested` | 1 |
| `runtime_sanctuary_zealous_torine_opening_kill_target_group_death_credit_tested` | 1 |
| `runtime_sanctuary_zealous_torine_opening_phase_activation_tested` | 1 |
| `runtime_siege_tempest_dominion_defend_assault_activation_tested_pending_wave_producers` | 1 |
| `runtime_siege_tempest_dominion_fallback_timedwin_activation_tested_pending_timer_smoke` | 1 |
| `runtime_siege_tempest_dominion_generator_resourcepool_activation_tested_pending_damage_producers` | 1 |
| `runtime_siege_tempest_dominion_holdout_timedwin_finish_tested_pending_timer_smoke` | 1 |
| `runtime_siege_tempest_exile_defend_assault_activation_tested_pending_wave_producers` | 1 |
| `runtime_siege_tempest_exile_fallback_timedwin_activation_tested_pending_timer_smoke` | 1 |
| `runtime_siege_tempest_exile_generator_resourcepool_activation_tested_pending_damage_producers` | 1 |
| `runtime_siege_tempest_exile_holdout_timedwin_finish_tested_pending_timer_smoke` | 1 |
| `runtime_skullcano_bosun_octog_boss_death_credit_tested_pending_boss_mechanics_rewards_and_client_smoke` | 1 |
| `runtime_skullcano_chief_kaskalak_talk_credit_tested_pending_escort_choreography_client_smoke` | 1 |
| `runtime_skullcano_chief_kaskalak_talk_phase_activation_tested_pending_escort_choreography_client_smoke` | 1 |
| `runtime_skullcano_cluster_missile_panel_checklist_activation_credit_tested_pending_placement_visual_state_and_client_smoke` | 1 |
| `runtime_skullcano_find_chief_direct_trigger_credit_tested_pending_trigger_owner_reconciliation_route_client_smoke` | 1 |
| `runtime_skullcano_find_chief_phase_activation_tested_pending_trigger_owner_reconciliation_route_client_smoke` | 1 |
| `runtime_skullcano_gold_infused_lava_core_death_credit_tested_pending_spawn_count_route_client_smoke` | 1 |
| `runtime_skullcano_gold_infused_lava_core_phase_activation_tested_pending_spawn_count_route_client_smoke` | 1 |
| `runtime_skullcano_gold_treasure_checklist_activation_credit_tested_pending_route_placement_visual_state_and_client_smoke` | 1 |
| `runtime_skullcano_gold_treasure_optional_objective_activation_tested_pending_route_placement_visual_state_and_client_smoke` | 1 |
| `runtime_skullcano_grim_grim_foe_sack_checklist_activation_credit_tested_pending_placement_visual_state_and_client_smoke` | 1 |
| `runtime_skullcano_lava_chasm_turnstile_phase_activation_tested_pending_trigger_placement_route_client_smoke` | 1 |
| `runtime_skullcano_lava_chasm_turnstile_trigger_credit_tested_pending_trigger_placement_route_client_smoke` | 1 |
| `runtime_skullcano_molten_chasm_monitoring_station_activation_credit_tested_pending_station_placement_route_client_smoke` | 1 |
| `runtime_skullcano_molten_chasm_monitoring_station_phase_activation_tested_pending_station_placement_route_client_smoke` | 1 |
| `runtime_skullcano_mordechai_redmoon_final_boss_death_credit_tested_pending_boss_mechanics_rewards_and_client_smoke` | 1 |
| `runtime_skullcano_quartermaster_gruhar_boss_death_credit_tested_pending_route_boss_mechanics_rewards_and_client_smoke` | 1 |
| `runtime_skullcano_redmoon_marauder_objective_activation_and_nested_target_group_kill_credit_tested_pending_spawn_count_rewards_and_client_smoke` | 2 |
| `runtime_skullcano_redmoon_platform_gather_phase_activation_tested_pending_trigger_placement_route_client_smoke` | 1 |
| `runtime_skullcano_redmoon_platform_gather_trigger_credit_tested_pending_trigger_placement_route_client_smoke` | 1 |
| `runtime_skullcano_terraformer_route_phase_activation_tested_pending_trigger_placement_route_client_smoke` | 1 |
| `runtime_skullcano_terraformer_route_trigger_credit_tested_pending_trigger_placement_route_client_smoke` | 1 |
| `runtime_skullcano_thunderfoot_opening_boss_death_credit_tested_pending_boss_mechanics_rewards_and_client_smoke` | 1 |
| `runtime_skullcano_tugga_opening_boss_death_credit_tested_pending_failure_condition_rewards_and_client_smoke` | 1 |
| `runtime_space_madness_air_helm_worker_activation_tested` | 1 |
| `runtime_space_madness_air_scrubber_controls_activation_tested` | 1 |
| `runtime_space_madness_air_scrubber_controls_credit_tested` | 1 |
| `runtime_space_madness_airlock_gather_activation_tested` | 1 |
| `runtime_space_madness_captain_tero_talk_activation_tested` | 1 |
| `runtime_space_madness_captain_tero_talk_credit_tested` | 1 |
| `runtime_space_madness_crew_datapad_checklist_activation_tested` | 1 |
| `runtime_space_madness_crew_datapad_checklist_credit_tested` | 1 |
| `runtime_space_madness_engineering_computer_all_clear_activation_tested` | 1 |
| `runtime_space_madness_engineering_computer_all_clear_credit_tested` | 1 |
| `runtime_space_madness_escaped_experiments_aggregate_activation_tested` | 1 |
| `runtime_space_madness_escaped_experiments_aggregate_credit_tested` | 3 |
| `runtime_space_madness_experimental_rockmite_activation_tested` | 1 |
| `runtime_space_madness_experimental_rockmite_credit_tested` | 1 |
| `runtime_space_madness_experimental_slank_activation_tested` | 1 |
| `runtime_space_madness_experimental_slank_credit_tested` | 1 |
| `runtime_space_madness_gold_medal_timer_activation_and_final_credit_tested` | 2 |
| `runtime_space_madness_hallucinating_worker_air_helm_talk_credit_tested` | 1 |
| `runtime_space_madness_hazmat_control_panel_credit_tested` | 1 |
| `runtime_space_madness_hazmat_storage_closet_activation_tested` | 1 |
| `runtime_space_madness_hazmat_suit_direct_credit_tested` | 1 |
| `runtime_space_madness_hazmat_suit_phase_activation_and_spawn_tested` | 1 |
| `runtime_space_madness_livestock_death_credit_tested` | 1 |
| `runtime_space_madness_livestock_phase_activation_tested` | 1 |
| `runtime_space_madness_major_lee_barmy_talk_activation_tested` | 1 |
| `runtime_space_madness_major_lee_barmy_talk_credit_tested` | 1 |
| `runtime_space_madness_observation_deck_computer_activation_tested` | 1 |
| `runtime_space_madness_observation_deck_computer_credit_tested` | 1 |
| `runtime_space_madness_panicked_worker_save_activation_tested` | 1 |
| `runtime_space_madness_panicked_worker_save_death_credit_tested` | 1 |
| `runtime_space_madness_party_downgrazer_activation_tested` | 1 |
| `runtime_space_madness_party_downgrazer_credit_tested` | 1 |
| `runtime_space_madness_research_laboratory_gather_activation_tested` | 1 |
| `runtime_stormtalon_aethros_boss_death_credit_tested_pending_boss_mechanics_rewards_and_client_smoke` | 1 |
| `runtime_stormtalon_aethros_objective_activation_tested_pending_boss_mechanics_rewards_and_client_smoke` | 1 |
| `runtime_stormtalon_arcanist_breeze_binder_optional_miniboss_activation_and_death_credit_tested_pending_route_combat_client_smoke` | 1 |
| `runtime_stormtalon_arcanist_breeze_binder_optional_miniboss_activation_tested_pending_route_combat_client_smoke` | 1 |
| `runtime_stormtalon_blade_wind_boss_death_credit_tested_pending_boss_mechanics_rewards_and_client_smoke` | 1 |
| `runtime_stormtalon_blade_wind_objective_activation_tested_pending_boss_mechanics_rewards_and_client_smoke` | 1 |
| `runtime_stormtalon_data_altar_activation_credit_tested_pending_route_visual_state_and_client_smoke` | 1 |
| `runtime_stormtalon_final_boss_death_credit_tested_pending_boss_mechanics_rewards_and_client_smoke` | 1 |
| `runtime_stormtalon_final_boss_objective_activation_and_event_finish_tested_pending_boss_mechanics_rewards_and_client_smoke` | 1 |
| `runtime_stormtalon_grenade_disable_objective_activation_tested_pending_grenade_mechanics_and_client_smoke` | 1 |
| `runtime_stormtalon_high_priest_trigger_credit_tested` | 1 |
| `runtime_stormtalon_high_priest_trigger_objective_activation_tested` | 1 |
| `runtime_stormtalon_improvement_platform_optional_objective_activation_and_jabbithole_placement_spawn_tested_pending_route_visual_state_and_client_smoke` | 1 |
| `runtime_stormtalon_improvement_platform_script_activation_credit_and_reviewed_placement_tested_pending_route_visual_state_and_client_smoke` | 1 |
| `runtime_stormtalon_invokers_holocrypt_optional_objective_activation_and_jabbithole_platform_spawn_tested_pending_route_visual_state_and_client_smoke` | 1 |
| `runtime_stormtalon_invokers_holocrypt_script_activation_credit_and_reviewed_platform_placement_tested_pending_route_visual_state_and_client_smoke` | 1 |
| `runtime_stormtalon_launch_pad_checklist_credit_tested_pending_route_visual_state_and_client_smoke` | 1 |
| `runtime_stormtalon_launch_pad_optional_objective_activation_and_jabbithole_placement_spawn_tested_pending_route_visual_state_and_client_smoke` | 1 |
| `runtime_stormtalon_overseer_drift_catcher_optional_miniboss_activation_and_death_credit_tested_pending_route_combat_client_smoke` | 1 |
| `runtime_stormtalon_overseer_drift_catcher_optional_miniboss_activation_tested_pending_route_combat_client_smoke` | 1 |
| `runtime_stormtalon_storm_totem_activation_credit_tested_pending_route_visual_state_and_client_smoke` | 1 |
| `runtime_stormtalon_tainted_flower_stem_checklist_credit_tested_pending_visual_state_and_client_smoke` | 1 |
| `runtime_stormtalon_tainted_flower_stem_jabbithole_placements_spawned_tested_pending_route_visual_state_and_client_smoke` | 1 |
| `runtime_stormtalon_thundercall_cage_checklist_credit_tested_pending_visual_state_and_client_smoke` | 1 |
| `runtime_stormtalon_thundercall_cage_jabbithole_placements_spawned_tested_pending_route_visual_state_and_client_smoke` | 1 |
| `runtime_stormtalon_thundercall_data_altar_optional_objective_activation_and_jabbithole_placement_spawn_tested_pending_route_visual_state_and_client_smoke` | 1 |
| `runtime_stormtalon_thundercall_pell_objective_activation_tested_pending_route_counts_rewards_and_client_smoke` | 1 |
| `runtime_stormtalon_thundercall_storm_totem_optional_objective_activation_and_jabbithole_placement_spawn_tested_pending_route_visual_state_and_client_smoke` | 1 |
| `runtime_stormtalon_thundercall_zealot_rush_death_credit_tested_pending_wave_cadence_rewards_and_client_smoke` | 1 |
| `runtime_stormtalon_thundercall_zealot_rush_objective_activation_tested_pending_wave_cadence_rewards_and_client_smoke` | 1 |
| `runtime_stormtalon_timed_thundercall_pell_bonus_activation_tested_pending_route_timer_rewards_and_client_smoke` | 1 |
| `runtime_up_can_crusher_jabbithole_placements_and_death_credit_tested` | 1 |
| `runtime_up_deputy_phase_activation_datamapping_placement_and_death_credit_tested_pending_room_density_pathing_and_client_smoke` | 1 |
| `runtime_up_deputy_phase_activation_datamapping_placement_tested_pending_room_density_pathing_and_client_smoke` | 1 |
| `runtime_up_destruct_o_derby_tank_room_death_credit_tested` | 1 |
| `runtime_up_destruct_o_derby_tank_room_phase_activation_jabbithole_placements_and_dynamic_max_tested` | 1 |
| `runtime_up_downsizer_death_credit_tested` | 1 |
| `runtime_up_downsizer_defeat_objective_activation_tested` | 1 |
| `runtime_up_gilded_fowl_phase_activation_jabbithole_placement_and_killcluster_full_count_credit_tested` | 1 |
| `runtime_up_gilded_fowl_phase_activation_jabbithole_placement_and_resourcepool_death_credit_tested` | 1 |
| `runtime_up_gilded_fowl_phase_activation_jabbithole_placement_tested` | 2 |
| `runtime_up_going_green_all_three_junk_jabbithole_placements_dynamic_max_and_death_credit_tested` | 1 |
| `runtime_up_huthut_direct_script_death_credit_tested` | 1 |
| `runtime_up_huthut_phase_activation_and_datamapping_placement_tested` | 1 |
| `runtime_up_misplaced_mammoth_jabbithole_placement_and_death_credit_tested` | 1 |
| `runtime_up_misplaced_mammoth_phase_activation_and_jabbithole_placement_tested` | 1 |
| `runtime_up_mondos_crate_phase_activation_jabbithole_placement_dynamic_max_and_death_credit_tested` | 1 |
| `runtime_up_mondos_crate_phase_activation_jabbithole_placement_dynamic_max_tested` | 1 |
| `runtime_up_mondos_monstrosity_phase_activation_jabbithole_placement_and_death_credit_tested` | 1 |
| `runtime_up_mondos_monstrosity_phase_activation_jabbithole_placement_tested` | 1 |
| `runtime_up_mondos_quick_reflexes_phase_activation_jabbithole_placement_and_death_credit_tested` | 1 |
| `runtime_up_mondos_quick_reflexes_phase_activation_jabbithole_placement_tested` | 1 |
| `runtime_up_ruffles_phase_activation_jabbithole_placement_and_death_credit_tested` | 1 |
| `runtime_up_ruffles_phase_activation_jabbithole_placement_tested` | 1 |
| `runtime_up_sneaky_prison_aggregate_warden_and_free_intern_phase_activation_placements_and_credit_tested_pending_stealth_no_alarm_client_smoke` | 1 |
| `runtime_up_sneaky_prison_alarm_panel_phase_activation_and_jabbithole_placements_tested_pending_alarm_timer_client_smoke` | 1 |
| `runtime_up_sneaky_prison_alarm_panel_phase_activation_jabbithole_placements_and_credit_tested_pending_alarm_timer_client_smoke` | 1 |
| `runtime_up_sneaky_prison_cage_console_aggregate_fast_hands_phase_activation_warden_pair_and_credit_tested_pending_route_timer_cage_state_and_client_smoke` | 1 |
| `runtime_up_sneaky_prison_cage_console_phase_activation_and_jabbithole_placement_tested_pending_route_cage_state_and_client_smoke` | 1 |
| `runtime_up_sneaky_prison_fast_hands_phase_activation_and_jabbithole_placement_tested_pending_exact_timer_failure_and_client_smoke` | 1 |
| `runtime_up_sneaky_prison_gate_console_alias_placement_credit_tested_pending_route_door_state_and_client_smoke` | 1 |
| `runtime_up_sneaky_prison_gate_console_phase_activation_and_alias_placement_tested_pending_route_door_state_and_client_smoke` | 1 |
| `runtime_up_sneaky_prison_warden_placement_and_aggregate_death_credit_tested_pending_stealth_no_alarm_client_smoke` | 1 |
| `runtime_up_start_button_one_shot_credit_tested` | 1 |
| `runtime_up_start_button_phase_activation_and_datamapping_spawn_tested` | 1 |
| `runtime_up_tank_room_phase_activation_and_jabbithole_placements_tested` | 2 |
| `runtime_up_tank_room_phase_activation_jabbithole_placements_and_dynamic_max_tested` | 1 |
| `runtime_up_tank_trample_jabbithole_placements_and_death_credit_tested` | 1 |
| `runtime_vault_archon_access_terminal_trigger_created_and_type_object_credit_tested` | 2 |
| `runtime_vault_archon_artemis_conversation_trigger_created_and_type_object_credited_tested` | 1 |
| `runtime_vault_archon_artemis_revive_objective_activated_and_direct_credit_tested` | 1 |
| `runtime_vault_archon_bloodhearth_tree_dialog_script_objective_activated_and_credited_tested_pending_dialog_choreography_client_smoke` | 2 |
| `runtime_vault_archon_bridge_meet_trigger_created_and_type_object_credited_tested` | 2 |
| `runtime_vault_archon_bridge_weak_point_explosive_checklist_activation_credit_tested_pending_placement_client_smoke` | 2 |
| `runtime_vault_archon_courtyard_key_targetgroup_activation_credit_and_pickup_cleanup_tested_pending_key_placement_client_smoke` | 4 |
| `runtime_vault_archon_courtyard_statue_trigger_created_and_type_object_credit_tested_pending_inner_door_and_client_smoke` | 1 |
| `runtime_vault_archon_courtyard_statue_trigger_volume_credit_tested_pending_inner_door_client_smoke` | 1 |
| `runtime_vault_archon_darkwitch_uhrga_add_objective_activated_and_death_credit_tested_pending_spawn_cadence_client_smoke` | 2 |
| `runtime_vault_archon_darkwitch_yotul_optional_boss_activation_and_death_credit_tested_pending_spawn_combat_client_smoke` | 2 |
| `runtime_vault_archon_dorian_conversation_trigger_created_and_type_object_credited_tested` | 1 |
| `runtime_vault_archon_dorian_revive_objective_activated_and_direct_credit_tested` | 1 |
| `runtime_vault_archon_follow_back_to_courtyard_script_objective_activated_and_zero_count_credited_tested` | 2 |
| `runtime_vault_archon_follow_inside_hall_script_objective_activated_and_credited_tested_pending_path_choreography_client_smoke` | 2 |
| `runtime_vault_archon_follow_into_kel_havik_script_objective_activated_and_zero_count_credited_tested` | 2 |
| `runtime_vault_archon_follow_into_vault_script_without_count_credit_tested_pending_path_choreography_client_smoke` | 2 |
| `runtime_vault_archon_follow_to_mysterious_tree_script_objective_zero_count_credit_tested_pending_path_choreography_client_smoke` | 2 |
| `runtime_vault_archon_follow_to_vault_entrance_script_objective_activated_and_zero_count_credited_tested_pending_path_choreography_client_smoke` | 2 |
| `runtime_vault_archon_force_field_wait_script_objective_activated_and_credited_tested_pending_choreography_client_smoke` | 2 |
| `runtime_vault_archon_forcefield_power_link_direct_script_credit_tested_pending_construct_spell_portal_client_smoke` | 1 |
| `runtime_vault_archon_harizog_ice_prison_witness_script_zero_count_credit_tested_pending_visual_choreography_client_smoke` | 2 |
| `runtime_vault_archon_harizog_objective_activated_and_boss_death_credit_tested_pending_spawn_combat_client_smoke` | 2 |
| `runtime_vault_archon_havik_honorguard_add_objective_activated_and_death_credit_tested_pending_spawn_cadence_client_smoke` | 2 |
| `runtime_vault_archon_havik_shiverhound_add_objective_activated_and_death_credit_tested_pending_spawn_cadence_client_smoke` | 2 |
| `runtime_vault_archon_holocube_explanation_script_objective_activated_and_credited_tested_pending_presentation_timing_client_smoke` | 2 |
| `runtime_vault_archon_holocube_talk_objective_activation_tested_pending_dialog_choreography_client_smoke` | 1 |
| `runtime_vault_archon_holocube_talk_target_group_credit_once_per_player_tested_pending_dialog_choreography_client_smoke` | 1 |
| `runtime_vault_archon_icebound_overlord_optional_boss_activation_and_death_credit_tested_pending_spawn_combat_client_smoke` | 2 |
| `runtime_vault_archon_kel_havik_courtyard_regroup_trigger_created_and_base_type_object_credit_tested` | 1 |
| `runtime_vault_archon_kel_havik_gate_key_placement_checklist_activation_credit_and_lock_cleanup_tested_pending_socket_placement_client_smoke` | 2 |
| `runtime_vault_archon_kel_havik_regroup_trigger_created_and_base_type_object_credit_tested` | 1 |
| `runtime_vault_archon_key_fragment_targetgroup_activation_credit_and_pickup_cleanup_tested_pending_fragment_placement_client_smoke` | 2 |
| `runtime_vault_archon_listen_to_plan_script_objective_activated_and_credited_tested` | 2 |
| `runtime_vault_archon_locked_gate_script_trigger_created_and_direct_objective_credited_tested` | 2 |
| `runtime_vault_archon_opening_conversation_shared_trigger_credit_tested` | 1 |
| `runtime_vault_archon_opening_gather_shared_trigger_credit_tested` | 1 |
| `runtime_vault_archon_opening_gather_sibling_trigger_created_and_type_object_credited_tested` | 1 |
| `runtime_vault_archon_opening_gather_trigger_created_and_type_object_credited_tested` | 1 |
| `runtime_vault_archon_osun_exit_blocker_exterminate_death_credit_tested_pending_wave_spawn_client_smoke` | 2 |
| `runtime_vault_archon_rejoin_group_trigger_created_and_type_object_credited_tested` | 1 |
| `runtime_vault_archon_tablet_study_script_objective_activated_and_credited_tested` | 2 |
| `runtime_vault_archon_targetgroup_objective_base_helper_covered_by_concrete_key_and_lock_scripts_tested_pending_placement_client_smoke` | 1 |
| `runtime_vault_archon_unbound_flame_elemental_optional_boss_activation_and_death_credit_tested_pending_spawn_combat_client_smoke` | 2 |
| `runtime_vault_archon_varegor_objective_activated_and_boss_death_credit_tested_pending_spawn_combat_client_smoke` | 2 |
| `runtime_vault_archon_varegor_pass_navigation_trigger_volume_credit_tested_pending_cold_environment_and_client_smoke` | 2 |
| `runtime_vault_archon_vault_door_gather_trigger_created_and_type_object_credit_tested_pending_door_choreography_client_smoke` | 2 |
| `runtime_vault_archon_vault_door_wait_script_zero_count_credit_tested_pending_door_choreography_client_smoke` | 2 |
| `runtime_vault_archon_vault_entrance_trigger_created_and_type_object_credit_tested_pending_path_choreography_client_smoke` | 1 |
| `runtime_vault_archon_vault_entrance_trigger_credit_tested_pending_path_choreography_client_smoke` | 1 |
| `runtime_vault_archon_vault_exit_trigger_created_and_type_object_credit_tested_pending_exit_choreography_client_smoke` | 2 |
| `runtime_vault_archon_watchtower_first_floor_trigger_created_and_base_type_object_credit_tested` | 1 |
| `runtime_vault_archon_watchtower_return_elevator_objective_activated_and_creature_interaction_zero_count_credit_tested` | 1 |
| `runtime_vault_archon_watchtower_top_floor_trigger_created_and_base_type_object_credit_tested` | 1 |
| `runtime_vault_archon_watchtower_turnstile_created_and_object_credit_path_tested` | 1 |
| `runtime_vault_archon_watchtower_upper_floors_trigger_created_and_base_type_object_credit_tested` | 1 |
| `runtime_vault_pell_graveyard_altar_checklist_activation_credit_tested_pending_placement_visual_state_client_smoke` | 1 |
| `runtime_vault_pell_graveyard_altar_objective_activation_tested_pending_placement_visual_state_client_smoke` | 1 |
| `runtime_vault_pell_graveyard_incense_checklist_activation_credit_tested_pending_placement_visual_state_client_smoke` | 1 |
| `runtime_vault_pell_graveyard_incense_objective_activation_tested_pending_placement_visual_state_client_smoke` | 1 |
| `runtime_vault_pell_graveyard_investigate_trigger_activation_tested` | 1 |
| `runtime_vault_pell_graveyard_investigate_trigger_credit_tested` | 1 |
| `runtime_vault_pell_graveyard_parent_objective_activation_tested` | 1 |
| `runtime_vault_pell_graveyard_parent_objective_trigger_credit_tested` | 1 |
| `runtime_vault_pell_graveyard_primal_echoes_script_gate_credit_tested_pending_exact_wave_choreography` | 2 |
| `runtime_vault_pell_graveyard_primal_wraith_death_credit_tested_pending_spawn_combat_client_smoke` | 1 |
| `runtime_vault_pell_graveyard_primal_wraith_objective_activation_tested_pending_spawn_combat_client_smoke` | 1 |
| `runtime_vault_soulrot_body_script_gate_credit_tested_pending_body_state_client_smoke` | 2 |
| `runtime_vault_soulrot_body_targetgroup_activation_credit_tested_pending_body_state_client_smoke` | 2 |
| `runtime_vault_soulrot_canister_trigger_activation_credit_tested_pending_route_client_smoke` | 2 |
| `runtime_vault_soulrot_cave_parent_objective_activation_credit_tested_pending_route_client_smoke` | 2 |
| `runtime_vault_soulrot_sample_script_credit_tested_pending_count_placement_client_smoke` | 1 |
| `runtime_vault_soulrot_soulless_kill_objective_activation_tested_pending_creature_owner_evidence` | 1 |
| `runtime_vault_soulrot_waste_targetgroup_activation_credit_tested_pending_disposal_state_client_smoke` | 2 |
| `runtime_vault_warhound_cluster_activation_tested_pending_pack_spawn_combat_client_smoke` | 1 |
| `runtime_vault_warhound_cluster_frozen_lever_death_credit_tested_pending_pack_spawn_combat_client_smoke` | 1 |
| `runtime_vault_warhound_kennel_depth_trigger_activation_tested_pending_combat_and_client_smoke` | 1 |
| `runtime_vault_warhound_kennel_depth_trigger_credit_tested_pending_combat_and_client_smoke` | 1 |
| `runtime_vault_warhound_kennel_trigger_activation_tested_pending_combat_and_client_smoke` | 1 |
| `runtime_vault_warhound_kennel_trigger_credit_tested_pending_combat_and_client_smoke` | 1 |
| `runtime_vault_warhound_parent_kennel_script_objective_activation_tested_pending_route_and_client_smoke` | 1 |
| `runtime_vault_warhound_parent_kennel_script_objective_trigger_credit_tested_pending_route_and_client_smoke` | 1 |
| `runtime_vault_warhound_watchhound_activation_tested_pending_spawn_combat_client_smoke` | 1 |
| `runtime_vault_warhound_watchhound_death_credit_tested_pending_spawn_combat_client_smoke` | 1 |
| `runtime_vault_yeti_cave_escape_script_gate_credit_tested_pending_chase_timing_client_smoke` | 2 |
| `runtime_vault_yeti_cave_frozen_carcass_checklist_credit_tested_pending_placement_visual_state_client_smoke` | 2 |
| `runtime_vault_yeti_cave_leave_trigger_activation_credit_tested_pending_chase_client_smoke` | 2 |
| `runtime_vault_yeti_cave_parent_objective_activation_credit_tested_pending_carcass_chase_client_smoke` | 2 |
| `runtime_vault_yeti_cave_yeti_death_credit_tested_pending_spawn_chase_combat_client_smoke` | 2 |
| `runtime_war_wilds_giant_moodie_totem_resourcepool_full_count_credit_tested_pending_health_pool_smoke` | 1 |
| `runtime_war_wilds_moodie_totem_health_resourcepool_full_count_credit_tested_pending_progressive_health_smoke` | 1 |
| `script_objective_credit_base_unresolved_objective_blocked` | 3 |
| `script_objective_producer_checklist_matched_pending_owner_flow_review` | 5 |
| `script_objective_producer_matched_pending_credit_timing_smoke` | 85 |
| `script_objective_producer_matched_pending_owner_flow_review` | 18 |
| `script_objective_producer_no_client_objective_match_blocked` | 6 |
| `script_objective_producer_unresolved_symbol_blocked` | 7 |
| `script_without_count_direct_objective_activation_matched_pending_timing_smoke` | 11 |
| `script_without_count_direct_objective_credit_matched_pending_owner_flow_review` | 3 |
| `script_without_max_direct_objective_activation_matched_pending_timing_smoke` | 2 |

## Script Quest Progression Evidence Summary

- Total Script.Main quest progression evidence rows: 38

| Evidence kind | Rows |
| --- | ---: |
| `script_achievement_grant` | 3 |
| `script_quest_achieve` | 1 |
| `script_quest_add` | 4 |
| `script_quest_mention` | 2 |
| `script_quest_objective_update` | 18 |
| `script_quest_typed_objective_update` | 10 |

| Progression targeting | Rows |
| --- | ---: |
| `achievement_id` | 3 |
| `direct_quest_objective` | 16 |
| `dynamic_objective` | 2 |
| `quest_id` | 3 |
| `quest_info` | 4 |
| `typed_quest_objective` | 10 |

| Reference link status | Rows |
| --- | ---: |
| `achievement_row_matched` | 3 |
| `direct_objective_row_matched` | 16 |
| `dynamic_objective_expression` | 2 |
| `dynamic_quest_info` | 4 |
| `quest_row_matched` | 3 |
| `typed_objective_dynamic_object` | 3 |
| `typed_objective_row_matched` | 7 |

| Owner link status | Rows |
| --- | ---: |
| `quest_state_guard_matches_quest` | 9 |
| `script_owner_is_other_current_quest` | 5 |
| `script_owner_matches_quest` | 7 |
| `script_owner_not_current_quest` | 5 |
| `script_owner_not_declared` | 12 |

| Source trace status | Rows |
| --- | ---: |
| `dynamic_progression_source_expression_tracked` | 9 |
| `literal_or_constant_client_reference_tracked` | 29 |

| Evidence status | Rows |
| --- | ---: |
| `runtime_q3486_accept_zone_arrival_story_panel_credit_tested` | 1 |
| `runtime_q3486_crystal_guardian_arrival_credit_hook_tested` | 1 |
| `runtime_q3486_crystal_guardian_virtual_collect_hook_tested` | 1 |
| `runtime_q3486_loftite_crystal_arrival_credit_hook_tested` | 1 |
| `runtime_q3486_loftite_crystal_virtual_collect_hook_tested` | 1 |
| `runtime_q3486_q3797_followup_mention_hook_tested` | 1 |
| `runtime_q5573_power_regulator_hidden_completion_hook_tested` | 1 |
| `runtime_q5575_power_regulator_hidden_completion_hook_tested` | 1 |
| `runtime_q5597_chua_explosives_virtual_collect_hook_tested_external_video_observed` | 1 |
| `script_achievement_grant_target_matched_pending_unlock_smoke` | 3 |
| `script_quest_achieve_target_matched_pending_state_smoke` | 1 |
| `script_quest_add_dynamic_quest_info_pending_trace` | 4 |
| `script_quest_mention_target_matched_pending_visibility_smoke` | 1 |
| `script_quest_objective_update_dynamic_runtime_context_pending_trace` | 2 |
| `script_quest_objective_update_matched_pending_owner_review` | 8 |
| `script_quest_objective_update_matched_pending_trigger_smoke` | 3 |
| `script_quest_typed_objective_update_dynamic_runtime_context_pending_trace` | 2 |
| `script_quest_typed_objective_update_matched_pending_trigger_smoke` | 5 |

## Script Runtime Action Evidence Summary

- Total script runtime action evidence rows: 210

| Action category | Rows |
| --- | ---: |
| `script_entity_add_to_map` | 29 |
| `script_entity_create` | 80 |
| `script_entity_remove_from_map` | 33 |
| `script_local_teleport` | 7 |
| `script_public_event_finish` | 39 |
| `script_spell_cast` | 14 |
| `script_teleport` | 8 |

| Script scope | Rows |
| --- | ---: |
| `script_instance` | 175 |
| `script_main` | 35 |

| Reference link status | Rows |
| --- | ---: |
| `generic_entity_type_tracked` | 80 |
| `helper_numeric_constant_resolved` | 1 |
| `numeric_constant_resolved` | 8 |
| `numeric_literal_resolved` | 4 |
| `source_expression_empty` | 33 |
| `source_expression_tracked` | 84 |

| Client reference status | Rows |
| --- | ---: |
| `dynamic_local_position_pending_source_trace` | 4 |
| `dynamic_spell_reference_pending_source_trace` | 2 |
| `dynamic_world_reference_pending_source_trace` | 7 |
| `literal_local_position_tracked` | 3 |
| `public_event_team_expression_tracked` | 39 |
| `runtime_entity_add_expression_tracked` | 29 |
| `runtime_entity_remove_receiver_tracked` | 33 |
| `runtime_entity_type_tracked` | 80 |
| `spell4_row_matched` | 12 |
| `world_row_matched` | 1 |

| Source trace status | Rows |
| --- | ---: |
| `combat_ai_aggro_spell_profile_source_tracked` | 1 |
| `combat_ai_auto_attack_spell4_runtime_table_guarded` | 1 |
| `direct_transport_world_rows_matched` | 1 |
| `dynamic_world_reference_pending_source_trace` | 1 |
| `literal_local_position_source_tracked` | 3 |
| `literal_or_constant_client_reference_tracked` | 12 |
| `matching_map_entrance_runtime_lookup_tracked` | 4 |
| `public_event_finish_team_source_tracked` | 39 |
| `runtime_entity_add_source_tracked` | 29 |
| `runtime_entity_remove_source_tracked` | 33 |
| `runtime_entity_type_source_tracked` | 80 |
| `starter_departure_routing_world_rows_matched` | 2 |
| `starter_tutorial_helper_spell_constant_traced` | 1 |
| `tutorial_combat_projector_worldlocation_rows_matched` | 1 |
| `tutorial_housing_projector_worldlocation_rows_matched` | 1 |
| `world_location_teleporter_worldlocation_rows_matched` | 1 |

| Evidence status | Rows |
| --- | ---: |
| `runtime_evil_ether_airlock_gather_trigger_create_action_tested` | 1 |
| `runtime_evil_ether_bridge_access_gather_trigger_create_action_tested` | 1 |
| `runtime_evil_ether_bridge_access_marker_remove_action_tested` | 1 |
| `runtime_evil_ether_drive_spark_create_action_tested` | 1 |
| `runtime_evil_ether_escape_teleporter_trigger_create_action_tested` | 1 |
| `runtime_evil_ether_final_captain_weir_finish_action_tested` | 1 |
| `runtime_evil_ether_find_teleporter_turnstile_create_action_tested` | 1 |
| `runtime_evil_ether_medbay_door_control_remove_action_tested` | 1 |
| `runtime_evil_ether_medbay_gather_ring_remove_action_tested` | 1 |
| `runtime_evil_ether_medbay_gather_trigger_remove_action_tested` | 1 |
| `runtime_evil_ether_medbay_local_teleport_action_tested` | 1 |
| `runtime_evil_ether_shades_bridge_gather_trigger_create_action_tested` | 1 |
| `runtime_evil_ether_shades_bridge_marker_remove_action_tested` | 1 |
| `runtime_evil_ether_shared_enqueue_add_action_tested` | 1 |
| `runtime_evil_ether_teleporter_gather_trigger_create_action_tested` | 1 |
| `runtime_evil_ether_upper_deck_trigger_create_action_tested` | 1 |
| `runtime_fragment_zero_final_hugo_finish_action_tested` | 1 |
| `runtime_fragment_zero_missing_crew_turnstile_trigger_create_action_tested` | 1 |
| `runtime_fragment_zero_search_grid_trigger_create_action_tested` | 1 |
| `runtime_fragment_zero_shared_enqueue_add_action_tested` | 1 |
| `runtime_fragment_zero_supervisor_lola_create_action_tested` | 1 |
| `runtime_fragment_zero_worldlocation_trigger_create_action_tested` | 1 |
| `runtime_gauntlet_airlock_marker_create_action_tested` | 1 |
| `runtime_gauntlet_final_npc_finish_action_tested` | 1 |
| `runtime_gauntlet_gather_worldlocation_trigger_create_action_tested` | 1 |
| `runtime_gauntlet_pilot_taboro_create_action_tested` | 1 |
| `runtime_gauntlet_shared_enqueue_add_action_tested` | 1 |
| `runtime_hoth_bridge_meet_trigger_create_action_tested` | 1 |
| `runtime_hoth_cold_and_hungry_finish_action_tested` | 1 |
| `runtime_hoth_cold_and_hungry_leave_trigger_create_action_tested` | 1 |
| `runtime_hoth_cold_and_hungry_leave_trigger_enqueue_add_action_tested` | 1 |
| `runtime_hoth_cold_and_hungry_leave_trigger_remove_action_tested` | 1 |
| `runtime_hoth_cold_soup_canister_trigger_create_action_tested` | 1 |
| `runtime_hoth_cold_soup_canister_trigger_enqueue_add_action_tested` | 1 |
| `runtime_hoth_cold_soup_canister_trigger_remove_action_tested` | 1 |
| `runtime_hoth_cold_soup_finish_action_tested` | 1 |
| `runtime_hoth_graveyard_finish_action_tested` | 1 |
| `runtime_hoth_graveyard_trigger_create_action_tested` | 1 |
| `runtime_hoth_graveyard_trigger_enqueue_add_action_tested` | 1 |
| `runtime_hoth_graveyard_trigger_remove_action_tested` | 1 |
| `runtime_hoth_kel_havik_regroup_trigger_create_action_tested` | 1 |
| `runtime_hoth_locked_gate_trigger_create_action_tested` | 1 |
| `runtime_hoth_opening_gather_trigger_create_action_tested` | 1 |
| `runtime_hoth_shared_trigger_enqueue_add_action_tested` | 1 |
| `runtime_hoth_shared_trigger_remove_action_tested` | 1 |
| `runtime_hoth_shared_world_location_trigger_create_action_tested` | 1 |
| `runtime_hoth_vault_exit_public_event_finish_action_tested` | 1 |
| `runtime_hoth_warhounds_kennel_trigger_create_action_tested` | 1 |
| `runtime_hoth_warhounds_kennel_trigger_enqueue_add_action_tested` | 1 |
| `runtime_hoth_warhounds_kennel_trigger_remove_action_tested` | 1 |
| `runtime_hoth_watchtower_turnstile_create_action_tested` | 1 |
| `runtime_kel_voreth_boss_spawn_create_action_tested` | 1 |
| `runtime_kel_voreth_boss_spawn_enqueue_add_action_tested` | 1 |
| `runtime_kel_voreth_final_boss_finish_action_tested` | 1 |
| `runtime_skullcano_final_boss_finish_action_tested` | 1 |
| `runtime_skullcano_opening_boss_enqueue_add_action_tested` | 1 |
| `runtime_skullcano_stew_shaman_tugga_create_action_tested` | 1 |
| `runtime_skullcano_thunderfoot_create_action_tested` | 1 |
| `runtime_space_madness_datapad_simple_placement_create_action_tested` | 1 |
| `runtime_space_madness_final_signal_finish_action_tested` | 1 |
| `runtime_space_madness_gather_worldlocation_trigger_create_action_tested` | 1 |
| `runtime_space_madness_hallucinating_livestock_create_action_tested` | 1 |
| `runtime_space_madness_npc_placement_create_action_tested` | 1 |
| `runtime_space_madness_shared_enqueue_add_action_tested` | 1 |
| `runtime_space_madness_simple_placement_create_action_tested` | 1 |
| `runtime_space_madness_talk_npc_create_action_tested` | 1 |
| `runtime_stormtalon_aethros_create_action_tested` | 1 |
| `runtime_stormtalon_blade_wind_create_action_tested` | 1 |
| `runtime_stormtalon_construction_platform_create_action_tested` | 1 |
| `runtime_stormtalon_data_altar_create_action_tested` | 1 |
| `runtime_stormtalon_final_boss_finish_action_tested` | 1 |
| `runtime_stormtalon_high_priest_trigger_create_action_tested` | 1 |
| `runtime_stormtalon_high_priest_trigger_remove_action_tested` | 1 |
| `runtime_stormtalon_launch_pad_create_action_tested` | 1 |
| `runtime_stormtalon_shared_enqueue_add_action_tested` | 1 |
| `runtime_stormtalon_storm_totem_create_action_tested` | 1 |
| `runtime_stormtalon_tainted_flower_stem_create_action_tested` | 1 |
| `runtime_stormtalon_thundercall_cage_create_action_tested` | 1 |
| `runtime_swordmaiden_final_boss_finish_action_tested` | 1 |
| `runtime_swordmaiden_flame_crazed_demon_create_action_tested` | 1 |
| `runtime_swordmaiden_flame_crazed_demon_enqueue_add_action_tested` | 1 |
| `runtime_up_bevorage_create_action_tested` | 1 |
| `runtime_up_downsizer_public_event_finish_action_tested` | 1 |
| `runtime_up_gilded_fowl_create_action_tested` | 1 |
| `runtime_up_hut_hut_create_action_tested` | 1 |
| `runtime_up_misplaced_mammoth_create_action_tested` | 1 |
| `runtime_up_mondos_crate_create_action_tested` | 1 |
| `runtime_up_mondos_monstrosity_create_action_tested` | 1 |
| `runtime_up_prototentiary_console_create_action_tested` | 1 |
| `runtime_up_prototentiary_deputy_create_action_tested` | 1 |
| `runtime_up_prototentiary_warden_create_action_tested` | 1 |
| `runtime_up_ruffles_create_action_tested` | 1 |
| `runtime_up_shared_enqueue_add_action_tested` | 1 |
| `runtime_up_start_button_create_action_tested` | 1 |
| `runtime_up_tank_room_create_action_tested` | 1 |
| `script_entity_add_to_map_pending_spawn_lifecycle_smoke` | 15 |
| `script_entity_create_pending_spawn_initialise_lifecycle_smoke` | 24 |
| `script_entity_remove_from_map_pending_cleanup_smoke` | 22 |
| `script_local_teleport_pending_destination_access_smoke` | 6 |
| `script_public_event_finish_pending_reward_completion_smoke` | 26 |
| `script_spell_cast_pending_spell_effect_targeting_smoke` | 14 |
| `script_teleport_pending_destination_access_smoke` | 8 |

## Instance Script Handler Evidence Summary

- Total instance script handler evidence rows: 13

| Content type | Rows |
| --- | ---: |
| `dungeon` | 2 |
| `event_instance` | 2 |
| `expedition` | 5 |
| `raid` | 3 |
| `world_story` | 1 |

| Script kind | Rows |
| --- | ---: |
| `map_script` | 8 |
| `public_event_script` | 5 |

| Handler category | Rows |
| --- | ---: |
| `map_entity_lifecycle` | 8 |
| `public_event_lifecycle` | 3 |
| `script_lifecycle` | 2 |

| Owner link status | Rows |
| --- | ---: |
| `script_owner_matches_instance_public_event` | 5 |
| `script_owner_matches_instance_world` | 8 |

| Evidence status | Rows |
| --- | ---: |
| `instance_map_entity_lifecycle_handler_pending_mechanics_smoke` | 5 |
| `instance_public_event_lifecycle_handler_pending_mechanics_smoke` | 2 |
| `instance_script_lifecycle_handler_pending_mechanics_smoke` | 1 |
| `runtime_evil_from_ether_map_onadd_player_join_and_cinematic_hook_tested` | 1 |
| `runtime_fragment_zero_map_onadd_player_join_and_cinematic_hook_tested` | 1 |
| `runtime_space_madness_map_onadd_player_join_and_cinematic_hook_tested` | 1 |
| `runtime_up_downsizer_objective_status_finish_handler_tested` | 1 |
| `runtime_up_downsizer_onload_objective_activation_handler_tested` | 1 |

## Dungeon/Raid/Instance Summary

- Total tracked world/script rows: 27

| Content type | Rows |
| --- | ---: |
| `dungeon` | 8 |
| `event_instance` | 2 |
| `expedition` | 8 |
| `raid` | 7 |
| `world_story` | 2 |

| Milestone scope | Rows |
| --- | ---: |
| `inventory_only_adjacent_scripted_content` | 2 |
| `inventory_only_client_table_only` | 2 |
| `milestone_target_instance` | 23 |

| Evidence status | Rows |
| --- | ---: |
| `mapped_only_pending_manual_smoke` | 25 |
| `observed_client_table_only` | 2 |

## Instance Dependency Summary

- Total dependency rows: 1495

| Dependency type | Rows |
| --- | ---: |
| `achievement` | 260 |
| `matching_game_map` | 47 |
| `public_event` | 40 |
| `public_event_objective` | 1116 |
| `script` | 32 |

## Instance Entity/Encounter Dependency Summary

- Total entity/encounter rows: 265

| Dependency type | Rows |
| --- | ---: |
| `entity` | 72 |
| `entity_event` | 64 |
| `entity_script` | 35 |
| `entity_stats` | 80 |
| `official_entity_reconciliation` | 14 |

| Evidence status | Rows |
| --- | ---: |
| `reviewed_laughingws_entity_event_pending_phase_smoke` | 64 |
| `reviewed_laughingws_entity_script_pending_mechanics_smoke` | 35 |
| `reviewed_laughingws_entity_stat_pending_combat_smoke` | 80 |
| `reviewed_laughingws_instance_entity_pending_spawn_and_mechanics_smoke` | 72 |
| `reviewed_official_entity_reconciliation_pending_smoke` | 14 |

## Instance Reward Dependency Summary

- Total reward rows: 80

| Reward source | Rows |
| --- | ---: |
| `match_type_reward_rotation_content_random_normal` | 5 |
| `matching_random_reward` | 45 |
| `public_event_reward_rotation_content` | 15 |
| `world_reward_rotation_content` | 15 |

| Evidence status | Rows |
| --- | ---: |
| `client_match_type_reward_rotation_content_pending_queue_reward_smoke` | 5 |
| `client_matching_random_reward_pending_queue_reward_smoke` | 45 |
| `client_public_event_reward_rotation_content_pending_event_reward_smoke` | 15 |
| `client_world_reward_rotation_content_pending_schedule_claim_smoke` | 15 |

## Next Restoration Slice Queue Summary

- Total queue rows: 23

| Lane | Rows |
| --- | ---: |
| `instance` | 11 |
| `quest` | 12 |

- Total queue blocker detail rows: 1779

| Blocker category | Rows |
| --- | ---: |
| `instance_dependency` | 684 |
| `instance_entity` | 105 |
| `instance_portal_evidence` | 11 |
| `instance_reward` | 48 |
| `public_event_evidence` | 339 |
| `quest_achievement` | 15 |
| `quest_creature` | 7 |
| `quest_episode_evidence` | 12 |
| `quest_loot` | 20 |
| `quest_objective` | 28 |
| `quest_objective_evidence` | 1 |
| `quest_prerequisite` | 36 |
| `quest_reward` | 44 |
| `quest_script` | 12 |
| `quest_world_dependency` | 87 |
| `quest_zone_evidence` | 3 |
| `script_objective_producer` | 319 |
| `script_presentation` | 3 |
| `script_quest_progression` | 5 |

## Parallel Subagent Lane Snapshot

Initial milestone lanes are deliberately narrow and read-only unless a later pass assigns a disjoint write set. Each lane records the ID-bearing inventories, evidence paths, confidence boundary, blockers, and next action needed before any retail-complete claim.

| Lane | IDs and tracker paths | Evidence sources | Confidence | Blockers | Next action |
| --- | --- | --- | --- | --- | --- |
| Quest inventory/objective coverage | `Decomp\Analysis\coverage\content_retail_completeness_quests.csv` (`quest_id`), `Decomp\Analysis\coverage\content_retail_completeness_quest_objectives.csv` (`objective_id`), `Decomp\Analysis\coverage\content_retail_completeness_quest_world_dependencies.csv` (`quest_id`, `objective_id`, `dependency_id`), `Decomp\Analysis\coverage\content_retail_completeness_quest_prerequisites.csv` (`quest_id`, `prerequisite_id`), `Decomp\Analysis\coverage\content_retail_completeness_quest_episode_evidence.csv` (`quest_id`, `episode_id`) | `wildstar_client_mysql/Quest2.tbl.sql`, `QuestObjective.tbl.sql`, `QuestDirection*.tbl.sql`, `TargetGroup.tbl.sql`, `WorldLocation2.tbl.sql`, `Source/NexusForever.Script.Main`, focused quest tests | Mapped for table coverage; Implemented where focused runtime tests are cited; Blocked for row-specific client/manual smoke. | Dialog/receiver UI, objective trigger placement, spawn density, loot cadence, reward UI, achievement UI, and end-to-end quest smoke. | Prioritize rows with runtime-tested objective credit but pending client smoke, then reconcile missing/ambiguous world and target-group evidence. |
| Dungeon, expedition, and raid inventory | `Decomp\Analysis\coverage\content_retail_completeness_instances.csv` (`world_id`), `Decomp\Analysis\coverage\content_retail_completeness_instance_dependencies.csv` (`dependency_id`), `Decomp\Analysis\coverage\content_retail_completeness_instance_script_handler_evidence.csv` (`world_id`, `script_path`), `Decomp\Analysis\coverage\content_retail_completeness_instance_portal_evidence.csv` (`world_id`, `instance_portal_id`) | `wildstar_client_mysql/World.tbl.sql`, `PublicEvent*.tbl.sql`, `MatchingGame*.tbl.sql`, `Achievement*.tbl.sql`, `Source/NexusForever.Script.Instance`, LaughingWS smoke/audit docs | Mapped/WIP scaffold for scripted instances; Observed client-table-only rows remain visible instead of being excluded. | Boss mechanics, phase choreography, trigger timing, portals, difficulty/queue behavior, rewards, loot, achievements, and manual instance smoke. | Promote one instance at a time from mapped/WIP scaffold to verified encounter mechanics with focused scripts, tests, and manual smoke notes. |
| Rewards, loot, achievements, and progression hooks | `Decomp\Analysis\coverage\content_retail_completeness_quest_rewards.csv` (`reward_id`, `object_id`), `Decomp\Analysis\coverage\content_retail_completeness_quest_loot.csv` (`loot_group_id`, `quest_objective_id`), `Decomp\Analysis\coverage\content_retail_completeness_quest_achievements.csv` (`achievement_id`, `checklist_id`), `Decomp\Analysis\coverage\content_retail_completeness_instance_rewards.csv` (`reward_rotation_content_id`, `match_type_enum`) | `Quest2Reward.tbl.sql`, `Achievement*.tbl.sql`, `RewardRotation*.tbl.sql`, `MatchingRandomReward.tbl.sql`, `Tools/DataMapping/sql/laughingws_quest_loot_seed.sql`, `QuestManager`, achievement/reward tests | Mapped for client/runtime reward rows; Implemented where focused grant/hook tests are cited; Blocked for active rotation and client presentation proof. | Reward-choice UI, inventory persistence, active reward-rotation schedule, loot drop cadence, achievement UI/tier behavior, and progression smoke. | Attach row-specific reward and achievement tests to the highest-priority quest/instance rows before claiming manual smoke readiness. |
| Scripts, encounters, and boss mechanics | `Decomp\Analysis\coverage\content_retail_completeness_quest_scripts.csv` (`quest_id`, `script_path`), `Decomp\Analysis\coverage\content_retail_completeness_script_presentation_evidence.csv` (`reference_id`, `source_path`), `Decomp\Analysis\coverage\content_retail_completeness_script_objective_producer_evidence.csv` (`objective_id`, `source_path`), `Decomp\Analysis\coverage\content_retail_completeness_script_event_flow_evidence.csv` (`public_event_id`, `source_path`), `Decomp\Analysis\coverage\content_retail_completeness_script_quest_progression_evidence.csv` (`quest_id`, `source_path`), `Decomp\Analysis\coverage\content_retail_completeness_script_runtime_action_evidence.csv` (`reference_id`, `source_path`), `Decomp\Analysis\coverage\content_retail_completeness_public_event_evidence.csv` (`public_event_id`), `Decomp\Analysis\coverage\content_retail_completeness_public_event_auxiliary_evidence.csv` (`public_event_id`), `Decomp\Analysis\coverage\content_retail_completeness_challenge_evidence.csv` (`challenge_id`), `Decomp\Analysis\coverage\content_retail_completeness_path_mission_evidence.csv` (`path_mission_id`), `Decomp\Analysis\coverage\content_retail_completeness_contract_evidence.csv` (`contract_id`) | `Source/NexusForever.Script.Main`, `Source/NexusForever.Script.Instance`, `PublicEventObjective.tbl.sql`, `Spell4.tbl.sql`, `World.tbl.sql`, script and instance tests | Correlated/Mapped for script references and direct objective producers; Implemented only where runtime tests are named; Blocked for exact retail timing/mechanics. | Script owner proof, kill-vs-script objective semantics, trigger cleanup, spawn lifecycle, combat choreography, cinematic payloads, and full content smoke. | Convert direct objective producer rows with current tests into per-encounter validation bundles, leaving broad typed producers blocked until owner flow is proven. |
| Database/data-mapping coverage | `Decomp\Analysis\coverage\content_retail_completeness_quest_creatures.csv` (`source_creature_id`, `creature2_id`, `source_coordinate_ids`), `Decomp\Analysis\coverage\content_retail_completeness_quest_objective_evidence.csv`, `Decomp\Analysis\coverage\content_retail_completeness_quest_reward_evidence.csv`, `Decomp\Analysis\coverage\content_retail_completeness_quest_zone_evidence.csv`, `Decomp\Analysis\coverage\content_retail_completeness_instance_entities.csv` (`entity_id`) | `Tools/DataMapping/output/*.csv`, `Tools/DataMapping/sql/runtime_world_seed.sql`, `laughingws_instance_entity_wip_seed.sql`, `laughingws_quest_loot_seed.sql`, `Tools/DataMapping/README.md` | Mapped for generated/reference comparisons; Verified only when promoted runtime rows and tests are cited; Blocked for authoring-only or WIP/GUESSED rows. | Ambiguous/unmatched creature bridges, unpromoted source coordinates, WIP/GUESSED overlays, authoring-only tables, and missing local smoke for promoted rows. | Keep runtime-owned promotion explicit, then run safe-import verification and row-specific smoke before upgrading any data-backed claim. |
| Decompile/client evidence | `Decomp/Analysis/function_labels.csv`, `Decomp/Analysis/mapping_artifacts/*.csv`, `Decomp/Analysis/coverage/opcode_coverage_inventory.csv`, current tracker CSV evidence fields | Cached build 16042 Ghidra exports, function labels, packet/model coverage, `CONTINUATION_GUIDE.md`, `INITIAL_FINDINGS.md`, and mapping artifacts | Observed/Correlated/Mapped for native anchors unless paired with runtime tests or live/client smoke; no decompiled source is copied into tracker rows. | Packet producer/apply ownership, exact UI timing, active reward rotation, instance mechanics, and live-client captures for manual-only behavior. | Use focused decompile passes only for the specific blocker named by a tracker row, then mirror durable labels/findings before implementation. |
| Wiki/archive/reference-data comparison | `Tools/WikiArchiveAudit/reports/quests-wiki-audit.md`, `Tools/WikiArchiveAudit/audit_wildstar_wiki.py`, `Tools/WikiArchiveAudit/quest_implementation_audit.py`, generated content tracker CSVs | Offline WildStar wiki archive, build 16042 client SQL, DataMapping/Jabbithole maps, and committed wiki audit reports | Correlated external/reference evidence only; client SQL and runtime-owned rows remain the implementation authority. | Wiki staleness, missing local archive artifacts, client/wiki disagreement, and archived facts that lack runtime/script validation. | Use wiki/archive facts to find gaps and corroborate names/chains, then require client SQL plus runtime or smoke proof before status upgrades. |
| Build, test, and static validation | `Tools/WikiArchiveAudit/validate_content_retail_completeness_outputs.py`, `Tools/WikiArchiveAudit/test_content_retail_completeness_audit.py`, `Tools/WikiArchiveAudit/test_content_retail_completeness_validator.py`, `Source/NexusForever.Game.Tests`, `Decomp\Analysis\coverage\content_retail_completeness_next_slice_queue.csv`, `Decomp\Analysis\coverage\content_retail_completeness_next_slice_blockers.csv` | Validator output, focused Python unit tests, xUnit quest/instance tests, and C# build/test commands in `AGENTS.md` | Verified for generated tracker invariants when validator/tests pass; gameplay remains Blocked where manual/client smoke is pending. | Full solution build/test scope, local runtime services, WildStar client availability, and manual smoke coverage. | Run the generator, validator, Python tests, then the narrow C# tests/builds touched by each implementation slice. |

## Focused Subagent Findings Inventory

- Source inventory: `Tools\WikiArchiveAudit\reports\content-retail-subagent-findings.csv`
- Total focused subagent finding rows: 36

| Lane | Rows |
| --- | ---: |
| `Build, test, and static validation` | 1 |
| `Dungeon, expedition, and raid inventory` | 3 |
| `Quest inventory/objective coverage` | 31 |
| `Rewards, loot, achievements, and progression hooks` | 1 |

| Status | Rows |
| --- | ---: |
| `open` | 36 |

| Confidence | Rows |
| --- | ---: |
| `High for client IDs and PE/objective mappings; Blocked for runtime placements and playable route` | 1 |
| `High for emulator 30-fallback spawn, virtual collect credit, and activation guards; Blocked for client UI smoke` | 1 |
| `High for emulator completion, rewards, achievement, and follow-up; Blocked for client UI smoke` | 1 |
| `High for emulator indicator spawn and virtual collect credit; Blocked for client map/activation UI smoke` | 4 |
| `High for emulator map-surface assets; Blocked for client visibility and map guidance smoke` | 1 |
| `High for emulator receiver spawn, accept/complete gates, rewards, achievement, and follow-up; Blocked for client dialog/UI smoke` | 1 |
| `High for emulator starter spawn, accept gate, and follow-up coverage; Blocked for client dialog/UI smoke` | 1 |
| `High on tracked reports CSV design; Medium on schema until workflow matures` | 1 |
| `Mapped EpisodeQuest order; Verified emulator chain handoff tests; Blocked retail episode/client smoke` | 1 |
| `Mapped Quest2 world-zone surface; Verified emulator map-load coverage; Blocked retail smoke` | 1 |
| `Mapped archived call-zone relation; Blocked current WorldZone replacement or route smoke` | 5 |
| `Mapped archived partial call-zone relation; Blocked current WorldZone replacement or route smoke` | 1 |
| `Mapped archived quest-zone relation; Blocked current WorldZone replacement or route smoke` | 1 |
| `Mapped objective bridge and stale source reward tuple` | 1 |
| `Mapped objective bridge; Blocked reward source-id collision` | 1 |
| `Mapped objective bridge; Verified emulator unit-test coverage; Blocked retail smoke` | 8 |
| `Mapped prerequisite accept gates; Verified emulator unit-test coverage; Blocked retail smoke` | 1 |
| `Mapped source/test scaffold; Blocked retail raid mechanics` | 1 |
| `Mapped stale source IDs for Q3479/Q3777; Blocked same-quest collision for Q3486` | 1 |
| `Mapped table/source alignment; Verified emulator unit-test coverage; Blocked retail smoke` | 1 |
| `Verified runtime objective/reward/achievement paths; Blocked retail smoke` | 1 |
| `Verified source-test scaffold; Blocked retail Downsizer semantics` | 1 |

| Finding | Priority | Lane | Scope | Source row | Confidence | Next action |
| --- | --- | --- | --- | --- | --- | --- |
| `2026-06-19-q5597-chua-explosives-indicator-17899-activation-ui-smoke-review` | `P0` | `Quest inventory/objective coverage` | quest 5597 Dregs and Thieves | `content_retail_completeness_quest_world_dependencies.csv` `quest_id=5597;dependency_source=QuestObjective.worldLocationsIdIndicator;dependency_type=objective_indicator_location;dependency_id=8256:worldLocationsIdIndicator03:17899;objective_id=8256;objective_type=32;source_slot=worldLocationsIdIndicator03;world_location_id=17899;world_id=870;world_zone_id=1325;quest_direction_id=;quest_direction_entry_id=;target_group_id=;target_group_type=;target_group_data=;prerequisite_id=` | High for emulator indicator spawn and virtual collect credit; Blocked for client map/activation UI smoke | Keep Q5597 not retail complete; capture client/manual indicator 17899 visibility and routing, Chua Explosives activation/objective UI, Scarhide Camp density/placement, despawn/respawn visual state, duplicate/negative cases, Mondo dialog, reward/achievement UI, and full Q5596->Q5597->Q5604 smoke. |
| `2026-06-19-q5597-chua-explosives-indicator-17897-activation-ui-smoke-review` | `P0` | `Quest inventory/objective coverage` | quest 5597 Dregs and Thieves | `content_retail_completeness_quest_world_dependencies.csv` `quest_id=5597;dependency_source=QuestObjective.worldLocationsIdIndicator;dependency_type=objective_indicator_location;dependency_id=8256:worldLocationsIdIndicator02:17897;objective_id=8256;objective_type=32;source_slot=worldLocationsIdIndicator02;world_location_id=17897;world_id=870;world_zone_id=1325;quest_direction_id=;quest_direction_entry_id=;target_group_id=;target_group_type=;target_group_data=;prerequisite_id=` | High for emulator indicator spawn and virtual collect credit; Blocked for client map/activation UI smoke | Keep Q5597 not retail complete; capture client/manual indicator 17897 visibility and routing, Chua Explosives activation/objective UI, Scarhide Camp density/placement, despawn/respawn visual state, duplicate/negative cases, Mondo dialog, reward/achievement UI, and full Q5596->Q5597->Q5604 smoke. |
| `2026-06-19-q5597-chua-explosives-indicator-17898-activation-ui-smoke-review` | `P0` | `Quest inventory/objective coverage` | quest 5597 Dregs and Thieves | `content_retail_completeness_quest_world_dependencies.csv` `quest_id=5597;dependency_source=QuestObjective.worldLocationsIdIndicator;dependency_type=objective_indicator_location;dependency_id=8256:worldLocationsIdIndicator01:17898;objective_id=8256;objective_type=32;source_slot=worldLocationsIdIndicator01;world_location_id=17898;world_id=870;world_zone_id=1325;quest_direction_id=;quest_direction_entry_id=;target_group_id=;target_group_type=;target_group_data=;prerequisite_id=` | High for emulator indicator spawn and virtual collect credit; Blocked for client map/activation UI smoke | Keep Q5597 not retail complete; capture client/manual indicator 17898 visibility and routing, Chua Explosives activation/objective UI, Scarhide Camp density/placement, despawn/respawn visual state, duplicate/negative cases, Mondo dialog, reward/achievement UI, and full Q5596->Q5597->Q5604 smoke. |
| `2026-06-19-q5597-chua-explosives-indicator-17896-activation-ui-smoke-review` | `P0` | `Quest inventory/objective coverage` | quest 5597 Dregs and Thieves | `content_retail_completeness_quest_world_dependencies.csv` `quest_id=5597;dependency_source=QuestObjective.worldLocationsIdIndicator;dependency_type=objective_indicator_location;dependency_id=8256:worldLocationsIdIndicator00:17896;objective_id=8256;objective_type=32;source_slot=worldLocationsIdIndicator00;world_location_id=17896;world_id=870;world_zone_id=1218;quest_direction_id=;quest_direction_entry_id=;target_group_id=;target_group_type=;target_group_data=;prerequisite_id=` | High for emulator indicator spawn and virtual collect credit; Blocked for client map/activation UI smoke | Keep Q5597 not retail complete; capture client/manual indicator 17896 visibility and routing, Chua Explosives activation/objective UI, density/placement, despawn/respawn visual state, duplicate/negative cases, Mondo dialog, reward/achievement UI, and full Q5596->Q5597->Q5604 smoke. |
| `2026-06-19-q5597-mondo-receiver-location-dialog-completion-smoke-review` | `P0` | `Quest inventory/objective coverage` | quest 5597 Dregs and Thieves | `content_retail_completeness_quest_world_dependencies.csv` `quest_id=5597;dependency_source=Quest2.WorldLocation2IdReceiver;dependency_type=quest_receiver_location;dependency_id=17803;objective_id=;objective_type=;source_slot=WorldLocation2IdReceiver;world_location_id=17803;world_id=870;world_zone_id=1227;quest_direction_id=;quest_direction_entry_id=;target_group_id=;target_group_type=;target_group_data=;prerequisite_id=` | High for emulator receiver spawn, accept/complete gates, rewards, achievement, and follow-up; Blocked for client dialog/UI smoke | Keep Q5597 not retail complete; capture client/manual receiver placement/subzone/phase visibility, Mondo accept and completion dialog, reward/inventory, achievement UI, duplicate/negative cases, and full Q5596->Q5597->Q5604 smoke. |
| `2026-06-19-q5597-crimson-isle-world-zone-map-visibility-smoke-review` | `P0` | `Quest inventory/objective coverage` | quest 5597 Dregs and Thieves | `content_retail_completeness_quest_world_dependencies.csv` `quest_id=5597;dependency_source=Quest2.worldZoneId;dependency_type=quest_world_zone;dependency_id=622;objective_id=;objective_type=;source_slot=;world_location_id=;world_id=;world_zone_id=622;quest_direction_id=;quest_direction_entry_id=;target_group_id=;target_group_type=;target_group_data=;prerequisite_id=` | High for emulator map-surface assets; Blocked for client visibility and map guidance smoke | Keep Q5597 not retail complete; capture client/manual map-zone activation, visibility, guidance, dialog, objective UI, rewards, achievements, duplicate/negative cases, and full Q5596->Q5597->Q5604 smoke. |
| `2026-06-13-q3486-objective-reward-audit` | `P0` | `Quest inventory/objective coverage` | quest 3486 Empowered Tower | `content_retail_completeness_quest_objective_evidence.csv` `quest_id=3486;jabbithole_quest_objective_id=436` | Mapped objective bridge; Blocked reward source-id collision | Keep Q3486 not retail complete; resolve reward provenance collision and capture client smoke before any status upgrade. |
| `2026-06-13-q3777-objective-reward-audit` | `P0` | `Quest inventory/objective coverage` | quest 3777 By Leaps and Bounds | `content_retail_completeness_quest_objective_evidence.csv` `quest_id=3777;jabbithole_quest_objective_id=410` | Mapped objective bridge and stale source reward tuple | Keep Q3777 not retail complete; validate dialog, crystal behavior, rewards, and achievements with focused smoke. |
| `2026-06-13-next-slice-reward-source-id-sweep` | `P0` | `Rewards, loot, achievements, and progression hooks` | quests 3479 3486 3777 reward evidence | `content_retail_completeness_quest_reward_evidence.csv` `quest_id=3479;source_reward_id=5289` | Mapped stale source IDs for Q3479/Q3777; Blocked same-quest collision for Q3486 | Treat stale source IDs as provenance aliases only; keep Q3486 reward collision blocked pending source review. |
| `2026-06-13-q3479-q3480-runtime-verified-smoke-blockers-audit` | `P0` | `Quest inventory/objective coverage` | quests 3479 From the Wreckage and 3480 Reporting for Duty | `content_retail_completeness_next_slice_blockers.csv` `slice_id=quest-3479` | Verified runtime objective/reward/achievement paths; Blocked retail smoke | Run Q3479/Q3480 client smoke bundles before changing completion status; keep stale reward source 2477 as provenance-only. |
| `2026-06-13-q3668-objective-creature-reward-audit` | `P0` | `Quest inventory/objective coverage` | quest 3668 Indigenous Intelligence | `content_retail_completeness_next_slice_blockers.csv` `slice_id=quest-3668` | Mapped table/source alignment; Verified emulator unit-test coverage; Blocked retail smoke | Keep Q3668 not retail complete; gather client/manual smoke for dialog, objective credit timing, reward UI/persistence, achievements, prerequisite chain, pushed item 6912, and missing Creature2 14054/11913 placement evidence. |
| `2026-06-13-q3673-objective-evidence-bridge-audit` | `P0` | `Quest inventory/objective coverage` | quest 3673 Contact with Thayd | `content_retail_completeness_quest_objective_evidence.csv` `quest_id=3673;jabbithole_quest_objective_id=137` | Mapped objective bridge; Verified emulator unit-test coverage; Blocked retail smoke | Keep Q3673 not retail complete; capture client/manual smoke for Deadeye, Signal Flares, cinematic timing, reward and achievement UI, prerequisite and alternate receiver routing, and full Q3886->Q3673->Q3670 flow. |
| `2026-06-13-instance-3041-ultimate-protogames-downsizer-audit` | `P1` | `Dungeon, expedition, and raid inventory` | dungeon-side Downsizer scaffold world 3041 Map\\UltimateProtogamesRaid | `content_retail_completeness_next_slice_blockers.csv` `slice_id=instance-3041` | Verified source-test scaffold; Blocked retail Downsizer semantics | Record scaffold coverage separately from retail blockers; capture targeted dungeon-side smoke and mechanic evidence before implementation claims. |
| `2026-06-13-instance-1333-datascape-audit` | `P1` | `Dungeon, expedition, and raid inventory` | raid world 1333 Datascape public event 157 | `content_retail_completeness_next_slice_blockers.csv` `slice_id=instance-1333` | Mapped source/test scaffold; Blocked retail raid mechanics | Split one Datascape wing or objective chain for the next pass; gather manual client smoke plus focused tests for timing, credit, negative/replay behavior, rewards, loot, and UI. |
| `2026-06-13-subagent-findings-inventory-design` | `P0` | `Build, test, and static validation` | content-retail subagent findings inventory contract | `Tools/WikiArchiveAudit/content_retail_completeness_audit.py` `subagent-findings-inventory` | High on tracked reports CSV design; Medium on schema until workflow matures | Have the generator read and render this report CSV, validate it separately, and keep it out of EXPECTED_CSV_NAMES and generated row counts. |
| `2026-06-13-q3886-objective-evidence-bridge-audit` | `P0` | `Quest inventory/objective coverage` | quest 3886 Fiery Distraction | `content_retail_completeness_quest_objective_evidence.csv` `quest_id=3886;jabbithole_quest_objective_id=429` | Mapped objective bridge; Verified emulator unit-test coverage; Blocked retail smoke | Keep Q3886 not retail complete; capture client/manual smoke for Durek, Burning Torch, Skeech Hut interaction, visual state/respawn, reward and achievement UI, and full Q3886->Q3673 flow. |
| `2026-06-13-q3963-objective-evidence-bridge-audit` | `P0` | `Quest inventory/objective coverage` | quest 3963 More Important Than Revenge | `content_retail_completeness_quest_objective_evidence.csv` `quest_id=3963;jabbithole_quest_objective_id=389` | Mapped objective bridge; Verified emulator unit-test coverage; Blocked retail smoke | Keep Q3963 not retail complete; capture client/manual smoke for Ship Controls activation, Q3487->Q3963 handoff, Deadeye receiver dialog, rewards, achievements, duplicate/negative cases, and reconcile Deadeye starter identity 11063/12959/23645. |
| `2026-06-13-q3479-objective-evidence-bridge-audit` | `P0` | `Quest inventory/objective coverage` | quest 3479 From the Wreckage | `content_retail_completeness_quest_objective_evidence.csv` `quest_id=3479;jabbithole_quest_objective_id=419` | Mapped objective bridge; Verified emulator unit-test coverage; Blocked retail smoke | Keep Q3479 not retail complete; capture client/manual smoke for survivor CSI, shared-yeti combat, Deadeye receiver dialog, reward and achievement UI, alternate receiver/prerequisite behavior, duplicate/negative cases, and full Q3479->Q3667 flow. |
| `2026-06-13-q3480-objective-evidence-bridge-audit` | `P0` | `Quest inventory/objective coverage` | quest 3480 Reporting for Duty | `content_retail_completeness_quest_objective_evidence.csv` `quest_id=3480;jabbithole_quest_objective_id=2337` | Mapped objective bridge; Verified emulator unit-test coverage; Blocked retail smoke | Keep Q3480 not retail complete; capture client/manual smoke for survivor CSI, shared-yeti combat, Deadeye receiver dialog, reward and achievement UI, alternate receiver/prerequisite behavior, starter bridge routing, duplicate/negative cases, and full Q3480 flow. |
| `2026-06-13-q3486-loftite-objective-evidence-bridge-audit` | `P0` | `Quest inventory/objective coverage` | quest 3486 Empowered Tower | `content_retail_completeness_quest_objective_evidence.csv` `quest_id=3486;jabbithole_quest_objective_id=437` | Mapped objective bridge; Verified emulator unit-test coverage; Blocked retail smoke | Keep Q3486 not retail complete; capture client/manual smoke for Loftite crystal collection, Crystal Guardian combat, Master Control Panel dialogs, reward/achievement UI, reward source 5319 provenance, TargetGroup member 11924 behavior, duplicate/negative cases, and full Q3667->Q3486->Q3797 flow. |
| `2026-06-13-q3777-loftite-objective-evidence-bridge-audit` | `P0` | `Quest inventory/objective coverage` | quest 3777 By Leaps and Bounds | `content_retail_completeness_quest_objective_evidence.csv` `quest_id=3777;jabbithole_quest_objective_id=411;412` | Mapped objective bridge; Verified emulator unit-test coverage; Blocked retail smoke | Keep Q3777 not retail complete; capture client/manual smoke for Denner dialogs, Loftite crystal collection and respawn/despawn timing, reward/reputation/achievement UI, duplicate/negative cases, remaining zone/creature bridges, and full Q3777 flow. |
| `2026-06-13-q5596-objective-evidence-bridge-audit` | `P0` | `Quest inventory/objective coverage` | quest 5596 Ordnance Recovery | `content_retail_completeness_quest_objective_evidence.csv` `quest_id=5596;jabbithole_quest_objective_id=1453;1454` | Mapped objective bridge; Verified emulator unit-test coverage; Blocked retail smoke | Keep Q5596 not retail complete; capture client/manual smoke for Crash Site Alpha entry, dead Dominion Demolitions Expert interactions, Mondo Zax dialogs, rewards, achievements, prerequisite branch routing, duplicate/negative cases, and full Q5573/Q5575->Q5596->Q5597 flow. |
| `2026-06-13-instance-3009-hall-of-the-hundred-audit` | `P1` | `Dungeon, expedition, and raid inventory` | released world-story world 3009 Map\HalloftheHundred public event 666 | `content_retail_completeness_instances.csv` `world_id=3009` | High for client IDs and PE/objective mappings; Blocked for runtime placements and playable route | Locate authoritative world 3009 entity/event/trigger rows for Varegor 67457, Harizog 67444, optional bosses 71577/71173/71414, objects 72108/71617-71619/72367/72958, then promote reviewed additive seed rows and test objective credit. |
| `2026-06-19-q5597-prerequisite-accept-gate-review` | `P0` | `Quest inventory/objective coverage` | quest 5597 Dregs and Thieves | `content_retail_completeness_quest_prerequisites.csv` `quest_id=5597;dependency_type=level;quest_faction;required_quest` | Mapped prerequisite accept gates; Verified emulator unit-test coverage; Blocked retail smoke | Keep Q5597 not retail complete; capture client/manual smoke for Mondo availability and denial behavior, Q5596-to-Q5597 chain presentation, rewards, achievements, duplicate/negative cases, and end-to-end Q5597 flow. |
| `2026-06-19-q5597-crimson-isle-world-zone-map-surface-review` | `P0` | `Quest inventory/objective coverage` | quest 5597 Dregs and Thieves | `content_retail_completeness_quest_world_dependencies.csv` `quest_id=5597;dependency_type=quest_world_zone;dependency_id=622` | Mapped Quest2 world-zone surface; Verified emulator map-load coverage; Blocked retail smoke | Keep Q5597 not retail complete; capture client/manual smoke for map and zone visibility, path/map guidance, Mondo dialog, Chua Explosives objective UI, rewards, achievements, duplicate/negative cases, and end-to-end Q5597 flow. |
| `2026-06-19-q5597-episode-order-chain-handoff-review` | `P0` | `Quest inventory/objective coverage` | quest 5597 Dregs and Thieves | `content_retail_completeness_quest_episode_evidence.csv` `quest_id=5597;episode_quest_id=2398;episode_id=464;order_index=2;flags=0` | Mapped EpisodeQuest order; Verified emulator chain handoff tests; Blocked retail episode/client smoke | Keep Q5597 not retail complete; capture client/manual smoke for episode tracker/order presentation, Q5596-to-Q5597-to-Q5604 visibility and turn-in flow, Mondo dialog, Chua Explosives objective UI, rewards, achievements, duplicate/negative cases, and end-to-end Q5597 flow. |
| `2026-06-19-q5597-crimson-isle-call-zone-worldzone12-bridge-review` | `P0` | `Quest inventory/objective coverage` | quest 5597 Dregs and Thieves | `content_retail_completeness_quest_zone_evidence.csv` `quest_id=5597;relation_source=quest_call_zones;source_relation_id=311;jabbithole_zone_id=9;world_zone_id=12` | Mapped archived call-zone relation; Blocked current WorldZone replacement or route smoke | Keep Q5597 not retail complete; capture client/manual smoke or table/decompile evidence for the current Crimson Isle route/visibility zone, map guidance, episode/zone ordering, Mondo dialog, objective UI, rewards, achievements, duplicate/negative cases, and end-to-end Q5597 flow. |
| `2026-06-19-q5597-illium-call-zone-worldzone78-ellevar-bridge-review` | `P0` | `Quest inventory/objective coverage` | quest 5597 Dregs and Thieves | `content_retail_completeness_quest_zone_evidence.csv` `quest_id=5597;relation_source=quest_call_zones;source_relation_id=4637;jabbithole_zone_id=46;world_zone_id=78` | Mapped archived call-zone relation; Blocked current WorldZone replacement or route smoke | Keep Q5597 not retail complete; capture client/manual smoke or table/decompile evidence for the current Crimson Isle route/visibility zone, map guidance, episode/zone ordering, Mondo dialog, objective UI, rewards, achievements, duplicate/negative cases, and end-to-end Q5597 flow. |
| `2026-06-19-q5597-housing-skymap-call-zone-worldzone60-mozyk-quarry-bridge-review` | `P0` | `Quest inventory/objective coverage` | quest 5597 Dregs and Thieves | `content_retail_completeness_quest_zone_evidence.csv` `quest_id=5597;relation_source=quest_call_zones;source_relation_id=4638;jabbithole_zone_id=40;world_zone_id=60` | Mapped archived call-zone relation; Blocked current WorldZone replacement or route smoke | Keep Q5597 not retail complete; capture client/manual smoke or table/decompile evidence for the current Crimson Isle route/visibility zone, map guidance, episode/zone ordering, Mondo dialog, objective UI, rewards, achievements, duplicate/negative cases, and end-to-end Q5597 flow. |
| `2026-06-19-q5597-northern-wastes-call-zone-worldzone41-fort-glory-bridge-review` | `P0` | `Quest inventory/objective coverage` | quest 5597 Dregs and Thieves | `content_retail_completeness_quest_zone_evidence.csv` `quest_id=5597;relation_source=quest_call_zones;source_relation_id=4639;jabbithole_zone_id=34;world_zone_id=41` | Mapped archived call-zone relation; Blocked current WorldZone replacement or route smoke | Keep Q5597 not retail complete; capture client/manual smoke or table/decompile evidence for the current Crimson Isle route/visibility zone, map guidance, episode/zone ordering, Mondo dialog, objective UI, rewards, achievements, duplicate/negative cases, and end-to-end Q5597 flow. |
| `2026-06-19-q5597-malgrave-call-zone-worldzone42-protostar-honeyworks-bridge-review` | `P0` | `Quest inventory/objective coverage` | quest 5597 Dregs and Thieves | `content_retail_completeness_quest_zone_evidence.csv` `quest_id=5597;relation_source=quest_call_zones;source_relation_id=4640;jabbithole_zone_id=35;world_zone_id=42` | Mapped archived call-zone relation; Blocked current WorldZone replacement or route smoke | Keep Q5597 not retail complete; capture client/manual smoke or table/decompile evidence for the current Crimson Isle route/visibility zone, map guidance, episode/zone ordering, Mondo dialog, objective UI, rewards, achievements, duplicate/negative cases, and end-to-end Q5597 flow. |
| `2026-06-19-q5597-auroria-call-zone-worldzone6-missing-bridge-review` | `P0` | `Quest inventory/objective coverage` | quest 5597 Dregs and Thieves | `content_retail_completeness_quest_zone_evidence.csv` `quest_id=5597;relation_source=quest_call_zones;source_relation_id=4641;jabbithole_zone_id=5;world_zone_id=6` | Mapped archived partial call-zone relation; Blocked current WorldZone replacement or route smoke | Keep Q5597 not retail complete; capture client/manual smoke or table/decompile evidence for the current Crimson Isle route/visibility zone, map guidance, episode/zone ordering, Mondo dialog, objective UI, rewards, achievements, duplicate/negative cases, and end-to-end Q5597 flow. |
| `2026-06-19-q5597-crimson-isle-quest-zone-worldzone12-bridge-review` | `P0` | `Quest inventory/objective coverage` | quest 5597 Dregs and Thieves | `content_retail_completeness_quest_zone_evidence.csv` `quest_id=5597;relation_source=quest_zones;source_relation_id=633;jabbithole_zone_id=9;world_zone_id=12` | Mapped archived quest-zone relation; Blocked current WorldZone replacement or route smoke | Keep Q5597 not retail complete; capture client/manual smoke or table/decompile evidence for the current Crimson Isle route/visibility zone, map guidance, episode/zone ordering, Mondo dialog, objective UI, rewards, achievements, duplicate/negative cases, and end-to-end Q5597 flow. |
| `2026-06-19-q5597-mondo-finisher-reward-achievement-ui-smoke-review` | `P0` | `Quest inventory/objective coverage` | quest 5597 Dregs and Thieves | `content_retail_completeness_quest_creatures.csv` `quest_id=5597;relation_type=finisher;source_relation_id=1564;jabbithole_creature_id=3367;creature2_id=24187` | High for emulator completion, rewards, achievement, and follow-up; Blocked for client UI smoke | Keep Q5597 not retail complete; capture client/manual smoke for Mondo completion dialog, reward UI/inventory persistence, achievement UI/progression, duplicate and negative completion cases, and full Q5597->Q5604 flow. |
| `2026-06-19-q5597-chua-explosives-objective-activation-ui-smoke-review` | `P0` | `Quest inventory/objective coverage` | quest 5597 Dregs and Thieves | `content_retail_completeness_quest_creatures.csv` `quest_id=5597;relation_type=objective;source_relation_id=1090;jabbithole_creature_id=3462;creature2_id=24286` | High for emulator 30-fallback spawn, virtual collect credit, and activation guards; Blocked for client UI smoke | Keep Q5597 not retail complete; capture client/manual activation/objective UI, density, despawn/respawn visual state, Mondo dialog, reward/inventory, achievement UI, duplicate/negative cases, and full Q5597->Q5604 flow. |
| `2026-06-19-q5597-mondo-starter-accept-dialog-prereq-ui-smoke-review` | `P0` | `Quest inventory/objective coverage` | quest 5597 Dregs and Thieves | `content_retail_completeness_quest_creatures.csv` `quest_id=5597;relation_type=starter;source_relation_id=1922;jabbithole_creature_id=3367;creature2_id=24187` | High for emulator starter spawn, accept gate, and follow-up coverage; Blocked for client dialog/UI smoke | Keep Q5597 not retail complete; capture client/manual Mondo accept dialog, prerequisite visibility and denial UI, exact spawn choice/density, reward/achievement UI side effects, negative cases, and full Q5596->Q5597->Q5604 flow. |

## Top Gap Rollup

These grouped gaps cover every non-blocker generated inventory. They are prioritization signals, not retail-complete claims.

| Area | Rows | Tracker paths | Current evidence/status signal | Next validation action |
| --- | ---: | --- | --- | --- |
| Quest identity and objective coverage | 16860 | `Decomp\Analysis\coverage\content_retail_completeness_quests.csv`, `Decomp\Analysis\coverage\content_retail_completeness_quest_objectives.csv`, `Decomp\Analysis\coverage\content_retail_completeness_quest_objective_evidence.csv` | `mapped_objective_handler_pending_world_trigger_smoke` (6237); `datamapping_jabbithole_objective_matched_pending_text_order_smoke` (4458); `mapped_table_driven_pending_world_spawns_loot_smoke` (3840); `mapped_only_no_objective_lifecycle_pending_row_specific_dialog_reward_achievement_smoke` (1289); omitted statuses: 1036 rows across 53 statuses | Validate Quest2/objective identity, objective text/data matching, and row-specific objective smoke before status upgrades. |
| Quest world, zone, and creature placement proof | 63735 | `Decomp\Analysis\coverage\content_retail_completeness_quest_world_dependencies.csv`, `Decomp\Analysis\coverage\content_retail_completeness_quest_zone_evidence.csv`, `Decomp\Analysis\coverage\content_retail_completeness_quest_creatures.csv` | `client_target_group_member_pending_spawn_and_credit_smoke` (12629); `client_objective_indicator_location_pending_world_trigger_smoke` (6884); `datamapping_objective_creature_relation_pending_review_and_smoke` (6562); `datamapping_quest_zone_matched_pending_runtime_visibility_smoke` (6142); omitted statuses: 31518 rows across 238 statuses | Resolve world/zone/creature bridges, promoted runtime placement, dialog/receiver and objective interaction smoke. |
| Quest prerequisites and episode chain semantics | 16462 | `Decomp\Analysis\coverage\content_retail_completeness_quest_prerequisites.csv`, `Decomp\Analysis\coverage\content_retail_completeness_quest_episode_evidence.csv` | prerequisites: `mapped_quest_accept_validation_pending_row_smoke` (9258); `mapped_prerequisite_manager_pending_row_smoke` (2739); `mapped_quest_chain_validation_pending_chain_smoke` (1827); `mapped_quest_exclusion_validation_pending_chain_smoke` (903); omitted statuses: 238 rows across 4 statuses; episodes: `datamapping_episode_quest_matched_pending_order_progression_smoke` (1174); `datamapping_episode_quest_missing_episode_bridge_blocked` (313); `datamapping_episode_not_current_client_quest_row_blocked` (9); `runtime_q5597_episode_order_q5596_q5597_q5604_handoff_tested_external_video_observed_pending_local_episode_smoke` (1) | Prove prerequisite/episode chain visibility, faction routing, and accept/complete sequencing. |
| Quest rewards, loot, and achievement hooks | 27091 | `Decomp\Analysis\coverage\content_retail_completeness_quest_rewards.csv`, `Decomp\Analysis\coverage\content_retail_completeness_quest_reward_evidence.csv`, `Decomp\Analysis\coverage\content_retail_completeness_quest_loot.csv`, `Decomp\Analysis\coverage\content_retail_completeness_quest_achievements.csv` | `mapped_runtime_reward_path_pending_row_smoke` (5297); `runtime_virtual_collect_entity_loot_binding_pending_source_drop_client_smoke` (5296); `datamapping_jabbithole_item_reward_matched_pending_choice_smoke` (3228); `mapped_inline_reward_pending_row_smoke` (3030); omitted statuses: 10240 rows across 36 statuses | Prove grants, loot cadence, reward UI/inventory persistence, achievement UI/progression. |
| Public events, challenges, path missions, and contracts | 36185 | `Decomp\Analysis\coverage\content_retail_completeness_public_event_evidence.csv`, `Decomp\Analysis\coverage\content_retail_completeness_public_event_auxiliary_evidence.csv`, `Decomp\Analysis\coverage\content_retail_completeness_challenge_evidence.csv`, `Decomp\Analysis\coverage\content_retail_completeness_path_mission_evidence.csv`, `Decomp\Analysis\coverage\content_retail_completeness_contract_evidence.csv` | `runtime_challenge_reward_track_item_grant_tested_pending_ui_selection_smoke` (12804); `datamapping_challenge_creature_runtime_hook_tested_pending_review_and_spawn_smoke` (4659); `datamapping_challenge_direct_reward_item_pending_reward_track_bridge` (3732); `mapped_only_contract_creature_pending_bridge_review_spawn_client_smoke` (2426); omitted statuses: 12564 rows across 142 statuses | Keep inventory-only until safe queue keys exist; validate event/objective/challenge/path/contract runtime hooks per slice. |
| Instance mechanics, portals, and encounter scripting | 2082 | `Decomp\Analysis\coverage\content_retail_completeness_instances.csv`, `Decomp\Analysis\coverage\content_retail_completeness_instance_dependencies.csv`, `Decomp\Analysis\coverage\content_retail_completeness_instance_entities.csv`, `Decomp\Analysis\coverage\content_retail_completeness_instance_portal_evidence.csv`, `Decomp\Analysis\coverage\content_retail_completeness_instance_script_handler_evidence.csv` | `client_achievement_row_pending_progression_hook_smoke` (260); `client_instance_portal_creature_link_pending_world_placement_review` (180); `mapped_omnicore1_downstream_objective_blocked_missing_route_specific_producer_client_smoke` (115); `mapped_ultimate_protogames_residual_objective_blocked_missing_room_specific_mechanics_client_smoke` (92); omitted statuses: 1435 rows across 392 statuses | Validate portals, public-event dependencies, entity/script handlers, phase/boss mechanics, and manual instance smoke. |
| Instance rewards and rotations | 80 | `Decomp\Analysis\coverage\content_retail_completeness_instance_rewards.csv` | `client_matching_random_reward_pending_queue_reward_smoke` (45); `client_world_reward_rotation_content_pending_schedule_claim_smoke` (15); `client_public_event_reward_rotation_content_pending_event_reward_smoke` (15); `client_match_type_reward_rotation_content_pending_queue_reward_smoke` (5) | Validate reward rotation/difficulty/item/currency presentation and grant persistence. |
| Script producers, presentation, progression, and runtime actions | 3091 | `Decomp\Analysis\coverage\content_retail_completeness_quest_scripts.csv`, `Decomp\Analysis\coverage\content_retail_completeness_script_presentation_evidence.csv`, `Decomp\Analysis\coverage\content_retail_completeness_script_event_flow_evidence.csv`, `Decomp\Analysis\coverage\content_retail_completeness_script_objective_producer_evidence.csv`, `Decomp\Analysis\coverage\content_retail_completeness_script_quest_progression_evidence.csv`, `Decomp\Analysis\coverage\content_retail_completeness_script_runtime_action_evidence.csv` | `script_public_event_objective_parent_event_matched_pending_activation_credit_smoke` (881); `script_public_event_phase_script_state_owner_matched_pending_phase_order_smoke` (676); `script_public_event_objective_client_row_matched_pending_owner_flow_review` (213); `script_objective_producer_matched_pending_credit_timing_smoke` (85); omitted statuses: 1236 rows across 651 statuses; runtime refs: `runtime_entity_type_tracked` (80); `public_event_team_expression_tracked` (39); `runtime_entity_remove_receiver_tracked` (33); `runtime_entity_add_expression_tracked` (29); omitted statuses: 29 rows across 6 statuses | Prove script owner flow, producer semantics, presentation/cinematic payloads, quest progression and runtime actions. |
| Next restoration slice queue | 23 | `Decomp\Analysis\coverage\content_retail_completeness_next_slice_queue.csv` | `quest_validation_ready` (12); `split_required_high_blocker_volume` (9); `split_required_producer_gap` (1); `instance_validation_ready` (1) | Use queue/blocker CSVs to drive focused implementation and smoke bundles; queue membership is not a retail-complete claim. |
| Validation coverage | 165609 | `Tools/WikiArchiveAudit/validate_content_retail_completeness_outputs.py` | 165609 non-blocker inventory rows remain `not_retail_complete`; validator rejects unsafe completion claims. | Run generator, validator, Python tests, and narrow C#/runtime validation after each content slice. |

## Next Restoration Slice Queue

Generated queue: `Decomp\Analysis\coverage\content_retail_completeness_next_slice_queue.csv`
Generated queue blocker details: `Decomp\Analysis\coverage\content_retail_completeness_next_slice_blockers.csv`

This queue is derived from the current inventories and highlights not-retail-complete rows with the strongest existing implementation and validation evidence. It is a work queue only; queue membership is not a retail-completeness claim.

All current queue rows are listed below; the generated queue CSV is the authoritative worklist for filters, blockers, commands, and harness bundles.

| Lane | Rows |
| --- | ---: |
| `instance` | 11 |
| `quest` | 12 |

| Actionability | Rows |
| --- | ---: |
| `instance_validation_ready` | 1 |
| `quest_validation_ready` | 12 |
| `split_required_high_blocker_volume` | 9 |
| `split_required_producer_gap` | 1 |

| Rank | Lane | Content | Score | Blockers | Action | Reason |
| ---: | --- | --- | ---: | ---: | --- | --- |
| 1 | `quest` | `3741` Scattered Supplies | 178 | 26 | `quest_validation_ready` | row-specific runtime evidence already cited; 8 child runtime/test evidence rows; 1 objective-evidence blocker row(s); 2 zone-evidence blocker row(s); 3 creature-evidence blocker row(s); script owner `Source\NexusForever.Script.Main\Quests\NorthernWilds\Q3741ScatteredSupplies.cs`; objectives `4813`; rewards `1712;3476`; achievements `3490` |
| 2 | `quest` | `3777` By Leaps and Bounds | 173 | 28 | `quest_validation_ready` | row-specific runtime evidence already cited; 18 child runtime/test evidence rows; 1 zone-evidence blocker row(s); 3 creature-evidence blocker row(s); script owner `Source\NexusForever.Script.Main\Quests\NorthernWilds\Q3777ByLeapsAndBounds.cs`; objectives `5075;5076;4859`; rewards `2244;2388;7191;7192;7193;7284;...+1`; achievements `3523` |
| 3 | `quest` | `5597` Dregs and Thieves | 173 | 21 | `quest_validation_ready` | focused runtime tests already cited; 12 child runtime/test evidence rows; script owner `Source\NexusForever.Script.Main\Quests\CrimsonIsle\Q5597.cs`; objectives `8256`; rewards `3000;5236`; achievements `4134` |
| 4 | `quest` | `3479` From the Wreckage | 168 | 26 | `quest_validation_ready` | focused runtime tests already cited; 16 child runtime/test evidence rows; script owner `Source\NexusForever.Script.Main\Quests\NorthernWilds\Q3479FromTheWreckage.cs`; objectives `4467;4564`; rewards `2476;2479;8569`; achievements `1432;1433;1434` |
| 5 | `quest` | `3480` Reporting for Duty | 168 | 22 | `quest_validation_ready` | focused runtime tests already cited; 14 child runtime/test evidence rows; script owner `Source\NexusForever.Script.Main\Quests\NorthernWilds\Q3480.cs`; objectives `4470;4565`; rewards `1676;1677;1678`; achievements `5327` |
| 6 | `quest` | `3486` Empowered Tower | 168 | 29 | `quest_validation_ready` | focused runtime tests already cited; 7 child runtime/test evidence rows; script owner `Source\NexusForever.Script.Main\Quests\NorthernWilds\Q3486EmpoweredTower.cs`; objectives `4987;4485`; rewards `1706;1707;2147`; achievements `3469;5327` |
| 7 | `quest` | `3668` Indigenous Intelligence | 168 | 25 | `quest_validation_ready` | focused runtime tests already cited; 20 child runtime/test evidence rows; script owner `Source\NexusForever.Script.Main\Quests\NorthernWilds\Q3668.cs`; objectives `4744;4745;4791`; rewards `2190;2191;4768`; achievements `3490` |
| 8 | `quest` | `3673` Contact with Thayd | 168 | 24 | `quest_validation_ready` | focused runtime tests already cited; 15 child runtime/test evidence rows; script owner `Source\NexusForever.Script.Main\Quests\NorthernWilds\Q3673ContactWithThayd.cs`; objectives `4748;4888;4889;4890;13391`; rewards `2187;2188;2189;2481;3001;3846`; achievements `3490` |
| 9 | `quest` | `3886` Fiery Distraction | 168 | 14 | `quest_validation_ready` | focused runtime tests already cited; 10 child runtime/test evidence rows; script owner `Source\NexusForever.Script.Main\Quests\NorthernWilds\Q3886FieryDistraction.cs`; objectives `5053;5052`; rewards `2192;2430;4767`; achievements `3490` |
| 10 | `quest` | `3963` More Important Than Revenge | 168 | 15 | `quest_validation_ready` | focused runtime tests already cited; 10 child runtime/test evidence rows; 1 creature-evidence blocker row(s); script owner `Source\NexusForever.Script.Main\Quests\NorthernWilds\Q3963MoreImportantThanRevenge.cs`; objectives `5201`; rewards `3706;3707;4771`; achievements `3491` |
| 11 | `quest` | `5573` Powering Down | 163 | 22 | `quest_validation_ready` | focused runtime tests already cited; 7 child runtime/test evidence rows; script owner `Source\NexusForever.Script.Main\Quests\CrimsonIsle\Q5573PoweringDown.cs`; objectives `8524;8229;12870`; rewards `2989;2991;2992`; achievements `4132` |
| 12 | `quest` | `5575` Seizing Power | 163 | 18 | `quest_validation_ready` | focused runtime tests already cited; 7 child runtime/test evidence rows; script owner `Source\NexusForever.Script.Main\Quests\CrimsonIsle\Q5575.cs`; objectives `8523;8371;12871`; rewards `2993;2994;2995`; achievements `4133` |
| 13 | `instance` | `2980` Ultimate Protogames | 125 | 291 | `split_required_producer_gap` | public events `594`; 123 objective rows; 45 scripted objective producers; 0 tracked script handlers; achievements `5864;5865;5866;5867;5868;5869;...+117` |
| 14 | `instance` | `382` Stormtalon's Lair | 114 | 101 | `split_required_high_blocker_volume` | public events `145`; 27 objective rows; 31 scripted objective producers; 0 tracked script handlers; achievements `2414;3462` |
| 15 | `instance` | `3009` Vault of the Archon | 100 | 205 | `split_required_high_blocker_volume` | public events `666;668;669;677;678;693;...+3`; 85 objective rows; 133 scripted objective producers; 0 tracked script handlers; achievements `4360;4361;4362;4363;4366;4586;...+27` |
| 16 | `instance` | `1271` Sanctuary of the Swordmaiden | 97 | 152 | `split_required_high_blocker_volume` | public events `166;202`; 52 objective rows; 63 scripted objective producers; 0 tracked script handlers; achievements `2639;3464;5306;5307;5308;5309;...+2` |
| 17 | `instance` | `3180` Fragment Zero | 93 | 131 | `split_required_high_blocker_volume` | public events `680`; 38 objective rows; 67 scripted objective producers; 1 tracked script handlers; achievements `6005;6020;6027` |
| 18 | `instance` | `1263` Skullcano | 91 | 110 | `split_required_high_blocker_volume` | public events `148`; 33 objective rows; 38 scripted objective producers; 0 tracked script handlers; achievements `5293;5294;5295;5296;5297` |
| 19 | `instance` | `2183` Gauntlet | 91 | 124 | `split_required_high_blocker_volume` | public events `446`; 35 objective rows; 39 scripted objective producers; 0 tracked script handlers; achievements `3718;4186;6011;6026` |
| 20 | `instance` | `3404` Evil from the Ether | 91 | 107 | `split_required_high_blocker_volume` | public events `781`; 32 objective rows; 55 scripted objective producers; 1 tracked script handlers; achievements `7048;7049;7050;7051` |
| 21 | `instance` | `2149` Space Madness | 89 | 163 | `split_required_high_blocker_volume` | public events `390`; 26 objective rows; 39 scripted objective producers; 1 tracked script handlers; achievements `3716;4180;6009;6024` |
| 22 | `instance` | `3041` Map\UltimateProtogamesRaid | 89 | 14 | `instance_validation_ready` | public events `642`; 5 objective rows; 6 scripted objective producers; 2 tracked script handlers |
| 23 | `instance` | `1336` Ruins of Kel Voreth | 81 | 111 | `split_required_high_blocker_volume` | public events `161`; 24 objective rows; 23 scripted objective producers; 0 tracked script handlers; achievements `2637;3463;5299;5300;5301;5302;...+3` |

| Blocker category | Rows |
| --- | ---: |
| `instance_dependency` | 684 |
| `instance_entity` | 105 |
| `instance_portal_evidence` | 11 |
| `instance_reward` | 48 |
| `public_event_evidence` | 339 |
| `quest_achievement` | 15 |
| `quest_creature` | 7 |
| `quest_episode_evidence` | 12 |
| `quest_loot` | 20 |
| `quest_objective` | 28 |
| `quest_objective_evidence` | 1 |
| `quest_prerequisite` | 36 |
| `quest_reward` | 44 |
| `quest_script` | 12 |
| `quest_world_dependency` | 87 |
| `quest_zone_evidence` | 3 |
| `script_objective_producer` | 319 |
| `script_presentation` | 3 |
| `script_quest_progression` | 5 |

## Current Milestone State

- No row in the generated inventories is marked `retail_complete` yet.
- Quest rows with generic objective support are tracked as table-driven coverage pending world-spawn, trigger, loot, reward, achievement, and smoke validation.
- Quest world/interaction rows are tracked as client guidance, receiver, world-location, and target-group evidence pending spawn/dialog/path/credit smoke.
- Quest achievement/progression rows are tracked as client Achievement/AchievementChecklist evidence pending runtime hook, tier, reward, and UI smoke.
- Quest script hook rows split current Quest2-owned IQuestScript hooks from map, public-event, spell, and other Script.Main owner ids that need owner-family proof before they support quest retail-completeness claims.
- Quest creature/NPC rows track DataMapping starter, finisher, and objective creature relationships, including unmatched, ambiguous, and non-current quest references, plus DataMapping source-coordinate samples, exact promoted-runtime placement matches, and reviewed/unpromoted placement gaps where available; dialog, objective credit, and client/manual smoke proof remain pending.
- Instance rows carry `milestone_scope` and `milestone_scope_reason` so event/world-story and client-table-only rows remain inventory-only until explicit promotion evidence exists.
- Quest Jabbithole objective rows track archived objective text/order evidence against current client objective ids, keeping unmatched and non-current references blocked until reconciled.
- Quest Jabbithole reward rows track item, currency, reputation, and tradeskill reward evidence against client ids and game-reward rows, pending grant and UI smoke proof.
- Quest Jabbithole zone rows track archived quest-zone and quest-call-zone relationships against client WorldZone ids, pending zone review, map guidance, visibility, and routing smoke.
- Quest episode rows track DataMapping episode-to-quest order/progression evidence against episode bridge status, keeping missing episode bridges and non-current quest references blocked.
- Public event DataMapping rows track objective, mission, zone, and creature bridge evidence for event mechanics and instance dependencies, keeping mismatches and non-current event references blocked.
- Public event auxiliary rows track depot, virtual-item depot, custom stat, stat display, reward modifier, bomb deployment, gather-resource, state, and vote dependencies pending producer semantics, UI, objective credit, and event smoke proof.
- Challenge DataMapping rows track challenge definitions, target creatures, and reward items as progression/content dependencies pending activation, objective, reward, and UI smoke proof.
- Path mission DataMapping rows now split the implemented Northern Wilds runtime-owned episode/mission/creature/zone subset from mapped-only and blocked rows that still need reviewed runtime ownership, creature bridge proof, reward presentation/timing evidence, or client path smoke.
- Contract DataMapping rows now split implemented manager-backed Quest2 contract lifecycle evidence from mapped-only creature bridge, reward-track choice/presentation, rotation, board timing, and client-smoke blockers.
- Instance portal rows track InstancePortal definitions, Creature2 portal links, and DataMapping placement/review evidence pending portal entry/exit, difficulty, prerequisite, and client smoke proof.
- Instance portal definition rows without a normalized Creature2/world link remain blocked for portal placement and entry/exit proof rather than supporting instance-complete claims.
- Script presentation rows track communicator-message references, cinematic queue references, runtime cinematic payload/link status, and cinematic-finish hooks pending exact trigger timing, UI/payload proof, id gating, and client smoke.
- Script public-event flow rows track PublicEventObjective and PublicEventPhase references in scripts, linking objective constants to client PublicEventObjective/PublicEvent rows while keeping script-only phase state pending retail order and smoke proof.
- Script objective producer rows track scripted UpdateObjective calls and PublicEventObjectiveCreditEntityScript hooks, linking direct objective ids or objective-type/object-id producers to client PublicEventObjective rows pending exact producer timing and content smoke.
- Script quest progression rows track Script.Main ObjectiveUpdate, QuestAchieve, QuestAdd, QuestMention, and AchievementManager.GrantAchievement hooks against client Quest2, QuestObjective, and Achievement rows pending exact trigger timing, chain behavior, and content smoke.
- Script runtime action rows track casts, teleports, entity lifecycle calls, and public-event finish calls, with client-row linkage for literal/constant Spell4 and World references plus source-trace status for routed dynamic calls.
- Instance script handler rows track On* lifecycle, public-event, map-entity, range-trigger, cinematic, combat, callback, and interaction methods for scripts attached to tracked dungeon/raid/instance worlds pending trigger-order, state-transition, cleanup, reward/loot, and smoke proof.
- Dungeon, expedition, and raid rows with scripts are tracked as mapped/WIP scaffolds pending exact mechanics, choreography, reward/loot proof, and manual smoke.
- Reviewed instance entity rows are tracked as placement, phase, script, stat, and official-import reconciliation evidence pending encounter mechanics and manual smoke.
- Instance reward rows are tracked as reward-rotation and matching-random reward evidence pending schedule, grant, claim, and presentation smoke.
- Raid rows with no current instance reward rows or reward-rotation content remain explicit reward-proof blockers until client reward schedules, grants, and presentation are mapped.
- Client-only dungeon/raid rows with no current script remain visible as review-required or missing-runtime-script rows instead of being silently excluded.

## Regenerate

```powershell
python Tools\WikiArchiveAudit\content_retail_completeness_audit.py
python Tools\WikiArchiveAudit\validate_content_retail_completeness_outputs.py
```

The generator is offline and uses only local repo/reference data. It should be rerun whenever quest scripts, instance scripts, DataMapping overlays, or client/Jabbithole evidence change.
The validator checks the expected generated inventory set, tracker links, required evidence/status/manual-validation fields, local `evidence_sources` paths/globs, and unsafe `retail_complete` claims.
