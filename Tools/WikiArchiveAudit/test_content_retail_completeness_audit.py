import shutil
import tempfile
import unittest
from collections import Counter
from pathlib import Path

import content_retail_completeness_audit as audit


class ContentRetailCompletenessAuditTests(unittest.TestCase):
    def test_format_top_statuses_reports_omitted_status_rows(self) -> None:
        statuses = Counter({"a": 5, "b": 4, "c": 3})

        formatted = audit.format_top_statuses(statuses, limit=2)

        self.assertEqual("`a` (5); `b` (4); omitted statuses: 3 rows across 1 status", formatted)

    def test_subagent_lane_snapshot_emits_complete_generated_inventory_map(self) -> None:
        lines: list[str] = []

        audit.append_subagent_lane_snapshot(lines, Path("Decomp/Analysis/coverage"))

        lane_rows = [
            line
            for line in lines
            if line.startswith("| ")
            and not line.startswith("| Lane ")
            and not line.startswith("| --- ")
        ]
        actual_lanes = [row.split("|")[1].strip() for row in lane_rows]
        self.assertEqual(
            [
                "Quest inventory/objective coverage",
                "Dungeon, expedition, and raid inventory",
                "Rewards, loot, achievements, and progression hooks",
                "Scripts, encounters, and boss mechanics",
                "Database/data-mapping coverage",
                "Decompile/client evidence",
                "Wiki/archive/reference-data comparison",
                "Build, test, and static validation",
            ],
            actual_lanes,
        )

        text = "\n".join(lines)
        for csv_name in (
            audit.CHALLENGE_EVIDENCE_CSV_NAME,
            audit.CONTRACT_EVIDENCE_CSV_NAME,
            audit.INSTANCE_DEPENDENCY_CSV_NAME,
            audit.INSTANCE_ENTITY_CSV_NAME,
            audit.INSTANCE_PORTAL_EVIDENCE_CSV_NAME,
            audit.INSTANCE_REWARD_CSV_NAME,
            audit.INSTANCE_SCRIPT_HANDLER_EVIDENCE_CSV_NAME,
            audit.INSTANCE_CSV_NAME,
            audit.NEXT_SLICE_BLOCKER_CSV_NAME,
            audit.NEXT_SLICE_QUEUE_CSV_NAME,
            audit.PATH_MISSION_EVIDENCE_CSV_NAME,
            audit.PUBLIC_EVENT_AUXILIARY_EVIDENCE_CSV_NAME,
            audit.PUBLIC_EVENT_EVIDENCE_CSV_NAME,
            audit.QUEST_ACHIEVEMENT_CSV_NAME,
            audit.QUEST_CREATURE_CSV_NAME,
            audit.QUEST_EPISODE_EVIDENCE_CSV_NAME,
            audit.QUEST_LOOT_CSV_NAME,
            audit.QUEST_OBJECTIVE_EVIDENCE_CSV_NAME,
            audit.QUEST_OBJECTIVE_CSV_NAME,
            audit.QUEST_PREREQUISITE_CSV_NAME,
            audit.QUEST_REWARD_EVIDENCE_CSV_NAME,
            audit.QUEST_REWARD_CSV_NAME,
            audit.QUEST_SCRIPT_CSV_NAME,
            audit.QUEST_WORLD_DEPENDENCY_CSV_NAME,
            audit.QUEST_ZONE_EVIDENCE_CSV_NAME,
            audit.QUEST_CSV_NAME,
            audit.SCRIPT_EVENT_FLOW_EVIDENCE_CSV_NAME,
            audit.SCRIPT_OBJECTIVE_PRODUCER_EVIDENCE_CSV_NAME,
            audit.SCRIPT_PRESENTATION_EVIDENCE_CSV_NAME,
            audit.SCRIPT_QUEST_PROGRESSION_EVIDENCE_CSV_NAME,
            audit.SCRIPT_RUNTIME_ACTION_EVIDENCE_CSV_NAME,
        ):
            self.assertIn(csv_name, text)

    def test_selected_output_names_default_to_full_tracker_bundle(self) -> None:
        selected = audit.resolve_selected_output_names(None)

        self.assertEqual(audit.TRACKER_OUTPUT_NAME, selected[0])
        self.assertIn(audit.QUEST_CSV_NAME, selected)
        self.assertIn(audit.NEXT_SLICE_BLOCKER_CSV_NAME, selected)

    def test_selected_output_names_support_aliases_and_dependencies(self) -> None:
        selected = audit.resolve_selected_output_names(["public-event-evidence,next-slice"])

        self.assertEqual(
            (
                audit.PUBLIC_EVENT_EVIDENCE_CSV_NAME,
                audit.NEXT_SLICE_QUEUE_CSV_NAME,
                audit.NEXT_SLICE_BLOCKER_CSV_NAME,
            ),
            selected,
        )

    def test_selected_output_names_accept_csv_stems_and_add_blocker_queue_dependency(self) -> None:
        selected = audit.resolve_selected_output_names(
            [
                "content_retail_completeness_next_slice_blockers",
                "Decomp/Analysis/coverage/content_retail_completeness_public_event_evidence.csv",
            ]
        )

        self.assertEqual(
            (
                audit.PUBLIC_EVENT_EVIDENCE_CSV_NAME,
                audit.NEXT_SLICE_QUEUE_CSV_NAME,
                audit.NEXT_SLICE_BLOCKER_CSV_NAME,
            ),
            selected,
        )

    def test_selected_output_names_reject_unknown_outputs(self) -> None:
        with self.assertRaisesRegex(ValueError, "Unknown output"):
            audit.resolve_selected_output_names(["definitely-not-a-generated-output"])

    def test_quest_rows_apply_northern_wilds_runtime_summary_overrides(self) -> None:
        artifact_root = audit.ROOT / "artifacts"
        artifact_root.mkdir(exist_ok=True)
        script_root = Path(tempfile.mkdtemp(prefix="nf-content-audit-script-", dir=artifact_root))
        try:
            script_body = """
[ScriptFilterOwnerId({quest_id}u)]
public class Q{quest_id}QuestScript : IQuestScript
{{
    public void ObjectiveUpdate() {{ }}
}}
""".lstrip()
            for quest_id in (3671, 3741, 3777, 3781, 3783, 4526):
                (script_root / f"Q{quest_id}.cs").write_text(script_body.format(quest_id=quest_id), encoding="utf-8")

            def quest_row(quest_id: int, title_id: int, objective_ids: tuple[int, ...] = ()) -> dict[str, str]:
                row = {
                    "ID": str(quest_id),
                    "localizedTextIdTitle": str(title_id),
                    "worldZoneId": "426",
                    "questCategoryId": "0",
                    "groupId": "0",
                    "type": "0",
                    "quest2SubTypeId": "0",
                    "questContentFinderTypeEnum": "0",
                    "questPlayerFactionEnum": "0",
                    "conLevel": "0",
                    "preq_level": "0",
                    "preq_flags": "0",
                    "preq_quest0": "0",
                    "preq_quest01": "0",
                    "preq_quest02": "0",
                    "questIdExclusionPreq0": "0",
                    "questIdExclusionPreq1": "0",
                    "questIdExclusionPreq2": "0",
                    "prerequisiteId": "0",
                }
                for index, column in enumerate(audit.QUEST_OBJECTIVE_COLUMNS):
                    row[column] = str(objective_ids[index]) if index < len(objective_ids) else "0"
                for suffix in audit.PUSHED_ITEM_SUFFIXES:
                    row[f"pushed_itemId{suffix}"] = "0"
                    row[f"pushed_itemCount{suffix}"] = "0"
                for suffix in audit.VIRTUAL_PUSHED_ITEM_SUFFIXES:
                    row[f"virtualItemIdPushed{suffix}"] = "0"
                    row[f"virtualItemPushedCount{suffix}"] = "0"
                return row

            strings = {
                1: "Setting Up Camp",
                2: "Scattered Supplies",
                3: "By Leaps and Bounds",
                4: "Captives of the Dominion",
                5: "Off With Her Head",
                6: "Spatial Anomaly",
            }
            quest_rows = [
                quest_row(3671, 1),
                quest_row(3741, 2, (4813,)),
                quest_row(3777, 3, (5075, 5076, 4859)),
                quest_row(3781, 4, (4880,)),
                quest_row(3783, 5),
                quest_row(4526, 6, (6164, 6165)),
            ]
            objective_rows = [
                {"ID": str(objective_id), "type": "5", "flags": "0", "count": "1", "data": str(objective_id)}
                for objective_id in (4813, 5075, 5076, 4859, 4880, 6164, 6165)
            ]
            reward_rows = [
                {"ID": "6662", "quest2Id": "3671", "quest2RewardTypeId": "1"},
                {"ID": "8830", "quest2Id": "3783", "quest2RewardTypeId": "1"},
            ]

            rows = audit.build_quest_rows(Path("unused"), script_root, strings, quest_rows, objective_rows, reward_rows, [], [])
            by_quest = {row["quest_id"]: row for row in rows}

            self.assertEqual("no_objectives", by_quest[3671]["bucket"])
            self.assertEqual(
                "runtime_q3671_no_objective_deadeye_turnin_reward_lifecycle_tested_pending_client_smoke",
                by_quest[3671]["evidence_status"],
            )
            self.assertIn("Q3486/Q3671/Q3668 chain smoke", by_quest[3671]["blocker_summary"])
            self.assertEqual("no_objectives", by_quest[3783]["bucket"])
            self.assertEqual(
                "runtime_q3783_item_started_no_objective_durek_turnin_reward_tested_pending_client_smoke",
                by_quest[3783]["evidence_status"],
            )
            self.assertIn("item-started no-objective turn-in path", by_quest[3783]["blocker_summary"])
            self.assertEqual("generic_with_script", by_quest[3741]["bucket"])
            self.assertEqual(
                "runtime_q3741_supply_crate_spawn_and_credit_script_tested_pending_client_smoke",
                by_quest[3741]["evidence_status"],
            )
            self.assertIn("Q3741ScatteredSupplies", by_quest[3741]["evidence_sources"])
            self.assertEqual(
                "runtime_q3777_denner_loftite_crystal_spawn_and_credit_script_tested_pending_client_smoke",
                by_quest[3777]["evidence_status"],
            )
            self.assertEqual(
                "runtime_q3781_starter_captives_receiver_spawn_credit_and_duplicate_guard_tested_pending_client_smoke",
                by_quest[3781]["evidence_status"],
            )
            self.assertEqual(
                "runtime_q4526_wreckage_anomaly_credit_and_duplicate_guard_script_tested_pending_client_smoke",
                by_quest[4526]["evidence_status"],
            )
            self.assertTrue(all(row["completion_status"] == "not_retail_complete" for row in rows))
        finally:
            shutil.rmtree(script_root, ignore_errors=True)

    def test_objective_evidence_blocker_predicate_keeps_reviewed_bridge_rows_nonblocking(self) -> None:
        self.assertFalse(
            audit.is_objective_evidence_blocker(
                {
                    "quest_id": 3673,
                    "quest_link_status": "current_quest",
                    "completion_status": "not_retail_complete",
                    "match_status": "reviewed_match",
                    "evidence_status": "datamapping_jabbithole_objective_reviewed_match_pending_client_smoke",
                }
            )
        )
        self.assertTrue(
            audit.is_objective_evidence_blocker(
                {
                    "quest_id": 3673,
                    "quest_link_status": "current_quest",
                    "completion_status": "not_retail_complete",
                    "match_status": "matched",
                    "evidence_status": "datamapping_jabbithole_objective_matched_pending_text_order_smoke",
                }
            )
        )
        self.assertTrue(
            audit.is_objective_evidence_blocker(
                {
                    "quest_id": 3673,
                    "quest_link_status": "current_quest",
                    "completion_status": "not_retail_complete",
                    "match_status": "unmatched",
                    "evidence_status": "datamapping_jabbithole_objective_unmatched_blocked",
                }
            )
        )

    def test_focused_subagent_findings_summary_uses_tracked_source_inventory(self) -> None:
        lines: list[str] = []
        rows = [
            {
                "finding_id": "finding-1",
                "lane": "Quest inventory/objective coverage",
                "priority": "P0",
                "status": "open",
                "finding_type": "tracker_refinement",
                "scope": "quest 1",
                "source_inventory": audit.QUEST_CSV_NAME,
                "source_row_key": "quest_id=1",
                "related_ids": "objective 2",
                "tracker_paths": audit.QUEST_CSV_NAME,
                "evidence_sources": "Source/NexusForever.Game.Tests",
                "confidence": "Mapped",
                "blocker_summary": "Still blocked by client smoke.",
                "next_action": "Capture client smoke.",
                "notes": "test row",
            }
        ]

        audit.append_focused_subagent_findings_summary(
            lines,
            rows,
            audit.ROOT / "Tools" / "WikiArchiveAudit" / "reports" / "content-retail-subagent-findings.csv",
        )

        text = "\n".join(lines)
        self.assertIn("## Focused Subagent Findings Inventory", text)
        self.assertIn("Tools/WikiArchiveAudit/reports/content-retail-subagent-findings.csv", text.replace("\\", "/"))
        self.assertIn("finding-1", text)
        self.assertIn("Total focused subagent finding rows: 1", text)
        self.assertNotIn("content_retail_completeness_focused_subagent_findings.csv", text)

    def test_instance_milestone_scope_counts_world_story_but_keeps_adjacent_rows_inventory_only(self) -> None:
        self.assertEqual(
            "milestone_target_instance",
            audit.classify_instance_milestone_scope("dungeon", "mapped_only_pending_manual_smoke")[0],
        )
        self.assertEqual(
            "inventory_only_adjacent_scripted_content",
            audit.classify_instance_milestone_scope("event_instance", "mapped_only_pending_manual_smoke")[0],
        )
        self.assertEqual(
            "milestone_target_instance",
            audit.classify_instance_milestone_scope("world_story", "mapped_only_pending_manual_smoke")[0],
        )
        self.assertEqual(
            "inventory_only_unreleased_content",
            audit.classify_instance_milestone_scope("unreleased_world_story", "mapped_only_pending_manual_smoke")[0],
        )
        self.assertEqual(
            "inventory_only_client_table_only",
            audit.classify_instance_milestone_scope("raid", "observed_client_table_only")[0],
        )

    def test_hall_of_the_hundred_asset_counts_as_vault_world_story_row(self) -> None:
        script_root = Path(tempfile.mkdtemp(prefix="nf-content-audit-instance-scripts-", dir=audit.ROOT))
        try:
            script_dir = script_root / "WorldStory" / "HallOfTheHundred"
            script_dir.mkdir(parents=True)
            (script_dir / "HallOfTheHundredMapScript.cs").write_text(
                """
[ScriptFilterOwnerId(3009)]
public class HallOfTheHundredMapScript
{
    public uint PublicEventId => 666u;
}
""".lstrip(),
                encoding="utf-8",
            )

            rows = audit.build_instance_rows(
                {10: "Hall of the Hundred"},
                [
                    {
                        "ID": "3009",
                        "type": "11",
                        "localizedTextIdName": "10",
                        "assetPath": r"Map\HalloftheHundred",
                        "rewardRotationContentId": "0",
                    }
                ],
                [],
                [],
                [
                    {
                        "ID": "103",
                        "worldId": "3009",
                        "matchingGameTypeId": "75",
                        "achievementCategoryId": "175",
                    }
                ],
                [{"ID": "75", "matchTypeEnum": "9"}],
                [],
                script_root,
            )

            self.assertEqual(1, len(rows))
            self.assertEqual("world_story", rows[0]["content_type"])
            self.assertEqual("Vault of the Archon", rows[0]["name"])
            self.assertEqual(r"Map\HalloftheHundred", rows[0]["asset_path"])
            self.assertEqual("milestone_target_instance", rows[0]["milestone_scope"])
            self.assertEqual("75", rows[0]["matching_game_type_ids"])
            self.assertEqual("9", rows[0]["match_type_enums"])
            self.assertEqual("666", rows[0]["public_event_ids"])
            self.assertIn("released Vault of the Archon world-story scaffold", rows[0]["blocker_summary"])
        finally:
            shutil.rmtree(script_root, ignore_errors=True)

    def test_vault_opening_producer_overrides_clear_tested_trigger_rows(self) -> None:
        blocker_tokens = ("pending", "blocked", "mapped_only", "reviewed", "wip")
        instance_objective_ids = (4456, 4457, 4458, 4459, 4949, 4950, 4299)
        for objective_id in instance_objective_ids:
            override = audit.INSTANCE_OBJECTIVE_DEPENDENCY_OVERRIDES[(3009, objective_id)]
            self.assertFalse(any(token in override["evidence_status"] for token in blocker_tokens))
            self.assertEqual("automated_focus_pass", override["manual_validation"])

        producer_keys = (
            (
                r"Source\NexusForever.Script.Instance\WorldStory\HallOfTheHundred\Script\OpeningGatherGridTriggerEntityScript.cs",
                "4456;4457",
                "8396",
            ),
            (
                r"Source\NexusForever.Script.Instance\WorldStory\HallOfTheHundred\Script\OpeningConversationGridTriggerEntityScript.cs",
                "4458;4459;4949",
                "8278",
            ),
        )
        for key in producer_keys:
            override = audit.SCRIPT_OBJECTIVE_PRODUCER_OVERRIDES[key]
            self.assertFalse(any(token in override["evidence_status"] for token in blocker_tokens))
            self.assertEqual("automated_focus_pass", override["manual_validation"])

    def test_vault_navigation_producer_overrides_clear_tested_rows(self) -> None:
        blocker_tokens = ("pending", "blocked", "mapped_only", "reviewed", "wip")
        instance_objective_ids = (
            4301,
            4302,
            4303,
            4305,
            4306,
            4308,
            4310,
            4311,
            5002,
            5054,
            5133,
            5136,
            4340,
            4325,
        )
        for objective_id in instance_objective_ids:
            override = audit.INSTANCE_OBJECTIVE_DEPENDENCY_OVERRIDES[(3009, objective_id)]
            self.assertFalse(any(token in override["evidence_status"] for token in blocker_tokens))
            self.assertEqual("automated_focus_pass", override["manual_validation"])

    def test_vault_osun_exit_objective_has_runtime_evidence_overrides(self) -> None:
        instance_override = audit.INSTANCE_OBJECTIVE_DEPENDENCY_OVERRIDES[(3009, 4329)]
        self.assertIn("VaregorEntityScript.cs", instance_override["script_path"])
        self.assertEqual("666;7839;2450;67429;67430;14376", instance_override["script_owner_ids"])
        self.assertEqual(
            "runtime_vault_archon_osun_exit_blocker_exterminate_death_credit_tested_pending_wave_spawn_client_smoke",
            instance_override["evidence_status"],
        )

        producer_override = audit.SCRIPT_OBJECTIVE_PRODUCER_OVERRIDES[
            (
                r"Source\NexusForever.Script.Instance\WorldStory\HallOfTheHundred\Script\VaregorEntityScript.cs",
                "4329",
                "7839",
            )
        ]
        self.assertEqual("script_owner_entity_filter_mapped_to_public_event", producer_override["owner_link_status"])
        self.assertIn("Creature2 rows 67429 and 67430", producer_override["blocker_summary"])
        self.assertIn("TargetGroup 14376", producer_override["blocker_summary"])

    def test_vault_varegor_pass_navigation_override_tracks_trigger_credit(self) -> None:
        instance_override = audit.INSTANCE_OBJECTIVE_DEPENDENCY_OVERRIDES[(3009, 4384)]
        self.assertIn("HallOfTheHundredEventScript.cs", instance_override["script_path"])
        self.assertIn("LockedGateInvestigationGridTriggerEntityScript.cs", instance_override["script_path"])
        self.assertEqual("666;7879;48658", instance_override["script_owner_ids"])
        self.assertEqual(
            "runtime_vault_archon_varegor_pass_navigation_trigger_volume_credit_tested_pending_cold_environment_and_client_smoke",
            instance_override["evidence_status"],
        )
        self.assertIn("Varegor Pass navigation 4384", audit.SCRIPTED_INSTANCE_BLOCKERS[3009])

        producer_override = audit.SCRIPT_OBJECTIVE_PRODUCER_OVERRIDES[
            (
                r"Source\NexusForever.Script.Instance\WorldStory\HallOfTheHundred\Script\LockedGateInvestigationGridTriggerEntityScript.cs",
                "4384",
                "7879",
            )
        ]
        self.assertEqual("script_owner_entity_filter_mapped_to_public_event", producer_override["owner_link_status"])
        self.assertIn("WorldLocation2 48658", producer_override["blocker_summary"])
        self.assertIn("object 7879", producer_override["blocker_summary"])

    def test_vault_hall_vault_entrance_overrides_track_trigger_credit(self) -> None:
        instance_override = audit.INSTANCE_OBJECTIVE_DEPENDENCY_OVERRIDES[(3009, 5260)]
        self.assertIn("HallOfTheHundredEventScript.cs", instance_override["script_path"])
        self.assertIn("AccessTerminalGridTriggerEntityScript.cs", instance_override["script_path"])
        self.assertEqual("666;8298;2445;3989;50645", instance_override["script_owner_ids"])
        self.assertEqual(
            "runtime_vault_archon_vault_entrance_trigger_created_and_type_object_credit_tested_pending_path_choreography_client_smoke",
            instance_override["evidence_status"],
        )
        self.assertIn("vault entrance trigger 5260", audit.SCRIPTED_INSTANCE_BLOCKERS[3009])

        event_override = audit.SCRIPT_OBJECTIVE_PRODUCER_OVERRIDES[
            (
                r"Source\NexusForever.Script.Instance\WorldStory\HallOfTheHundred\HallOfTheHundredEventScript.cs",
                "5260",
                "8298",
            )
        ]
        self.assertEqual("script_owner_matches_public_event", event_override["owner_link_status"])
        self.assertIn("WorldLocation2 50645", event_override["blocker_summary"])
        self.assertIn("advances from objective 4324", event_override["blocker_summary"])
        self.assertIn("access-terminal trigger 4325", event_override["blocker_summary"])

        trigger_override = audit.SCRIPT_OBJECTIVE_PRODUCER_OVERRIDES[
            (
                r"Source\NexusForever.Script.Instance\WorldStory\HallOfTheHundred\Script\AccessTerminalGridTriggerEntityScript.cs",
                "5260",
                "8298",
            )
        ]
        self.assertEqual("script_owner_entity_filter_mapped_to_public_event", trigger_override["owner_link_status"])
        self.assertIn("VaultEntranceGridTriggerEntityScript", trigger_override["blocker_summary"])
        self.assertIn("ParticipantsInTriggerVolume", trigger_override["blocker_summary"])
        self.assertIn("object 8298", trigger_override["blocker_summary"])

    def test_vault_warhound_kennel_overrides_track_trigger_credit(self) -> None:
        parent_instance_override = audit.INSTANCE_OBJECTIVE_DEPENDENCY_OVERRIDES[(3009, 5294)]
        self.assertIn("TheWarhoundsOfVaregorEventScript.cs", parent_instance_override["script_path"])
        self.assertIn("WarhoundKennelGridTriggerEntityScript.cs", parent_instance_override["script_path"])
        self.assertEqual("666;693;5294;7861;48660;4403;8426", parent_instance_override["script_owner_ids"])
        self.assertEqual(
            "runtime_vault_warhound_parent_kennel_script_objective_activation_and_trigger_credit_tested_pending_route_and_client_smoke",
            parent_instance_override["evidence_status"],
        )
        instance_override = audit.INSTANCE_OBJECTIVE_DEPENDENCY_OVERRIDES[(3009, 4403)]
        self.assertIn("TheWarhoundsOfVaregorEventScript.cs", instance_override["script_path"])
        self.assertIn("WarhoundKennelGridTriggerEntityScript.cs", instance_override["script_path"])
        self.assertEqual("693;4403;8426;48660", instance_override["script_owner_ids"])
        self.assertEqual(
            "runtime_vault_warhound_kennel_trigger_activation_credit_tested_pending_combat_and_client_smoke",
            instance_override["evidence_status"],
        )
        self.assertIn("Warhound Kennel parent objective 5294 activation/credit", audit.SCRIPTED_INSTANCE_BLOCKERS[3009])
        watchhound_instance_override = audit.INSTANCE_OBJECTIVE_DEPENDENCY_OVERRIDES[(3009, 4404)]
        self.assertIn("TheWarhoundsOfVaregorEventScript.cs", watchhound_instance_override["script_path"])
        self.assertIn("VaregorEntityScript.cs", watchhound_instance_override["script_path"])
        self.assertEqual("693;4404;67884;51164", watchhound_instance_override["script_owner_ids"])
        self.assertEqual(
            "runtime_vault_warhound_watchhound_activation_and_death_credit_tested_pending_spawn_combat_client_smoke",
            watchhound_instance_override["evidence_status"],
        )
        depth_instance_override = audit.INSTANCE_OBJECTIVE_DEPENDENCY_OVERRIDES[(3009, 4434)]
        self.assertIn("TheWarhoundsOfVaregorEventScript.cs", depth_instance_override["script_path"])
        self.assertIn("WarhoundKennelGridTriggerEntityScript.cs", depth_instance_override["script_path"])
        self.assertEqual("693;4434;7881;48661", depth_instance_override["script_owner_ids"])
        self.assertEqual(
            "runtime_vault_warhound_kennel_depth_trigger_activation_credit_tested_pending_combat_and_client_smoke",
            depth_instance_override["evidence_status"],
        )
        cluster_instance_override = audit.INSTANCE_OBJECTIVE_DEPENDENCY_OVERRIDES[(3009, 4441)]
        self.assertIn("TheWarhoundsOfVaregorEventScript.cs", cluster_instance_override["script_path"])
        self.assertIn("VaregorEntityScript.cs", cluster_instance_override["script_path"])
        self.assertEqual("693;4441;12262;67940;67924;67925;67935;51165", cluster_instance_override["script_owner_ids"])
        self.assertEqual(
            "runtime_vault_warhound_cluster_frozen_lever_activation_and_death_credit_tested_pending_pack_spawn_combat_client_smoke",
            cluster_instance_override["evidence_status"],
        )
        self.assertIn("Warhound Kennel parent objective 5294 activation/credit and side objectives 4403/4434", audit.SCRIPTED_INSTANCE_BLOCKERS[3009])
        self.assertIn("4404 Varegor Watchhound activation/death credit", audit.SCRIPTED_INSTANCE_BLOCKERS[3009])
        self.assertIn("4441 Warhound cluster/Frozen Lever activation/death credit", audit.SCRIPTED_INSTANCE_BLOCKERS[3009])
        self.assertIn("Warhound pack 67924/67925/67935 spawn/combat routing", audit.SCRIPTED_INSTANCE_BLOCKERS[3009])

        parent_event_override = audit.SCRIPT_OBJECTIVE_PRODUCER_OVERRIDES[
            (
                r"Source\NexusForever.Script.Instance\WorldStory\HallOfTheHundred\TheWarhoundsOfVaregorEventScript.cs",
                "5294",
                "7861",
            )
        ]
        self.assertEqual("script_owner_matches_public_event", parent_event_override["owner_link_status"])
        self.assertIn("main public event 666 objective 5294", parent_event_override["blocker_summary"])
        self.assertIn("side-event kennel phase opens", parent_event_override["blocker_summary"])
        self.assertIn("WorldLocation2 48660", parent_event_override["blocker_summary"])

        parent_trigger_override = audit.SCRIPT_OBJECTIVE_PRODUCER_OVERRIDES[
            (
                r"Source\NexusForever.Script.Instance\WorldStory\HallOfTheHundred\Script\WarhoundKennelGridTriggerEntityScript.cs",
                "5294",
                "7861",
            )
        ]
        self.assertEqual("script_owner_entity_filter_mapped_to_public_event", parent_trigger_override["owner_link_status"])
        self.assertIn("credits parent Script objective 5294 once", parent_trigger_override["blocker_summary"])
        self.assertIn("existing 4403 typed trigger credit", parent_trigger_override["blocker_summary"])

        event_override = audit.SCRIPT_OBJECTIVE_PRODUCER_OVERRIDES[
            (
                r"Source\NexusForever.Script.Instance\WorldStory\HallOfTheHundred\TheWarhoundsOfVaregorEventScript.cs",
                "4403",
                "8426",
            )
        ]
        self.assertEqual("script_owner_matches_public_event", event_override["owner_link_status"])
        self.assertIn("public event 693", event_override["blocker_summary"])
        self.assertIn("WorldLocation2 48660", event_override["blocker_summary"])
        self.assertIn("mapped watchhound objective 4404", event_override["blocker_summary"])
        self.assertIn("Warhound pack 67924/67925/67935", event_override["blocker_summary"])

        trigger_override = audit.SCRIPT_OBJECTIVE_PRODUCER_OVERRIDES[
            (
                r"Source\NexusForever.Script.Instance\WorldStory\HallOfTheHundred\Script\WarhoundKennelGridTriggerEntityScript.cs",
                "4403",
                "8426",
            )
        ]
        self.assertEqual("script_owner_entity_filter_mapped_to_public_event", trigger_override["owner_link_status"])
        self.assertIn("ParticipantsInTriggerVolume object 8426", trigger_override["blocker_summary"])
        self.assertIn("Frozen Lever placement/destructible-state smoke", trigger_override["blocker_summary"])

        watchhound_event_override = audit.SCRIPT_OBJECTIVE_PRODUCER_OVERRIDES[
            (
                r"Source\NexusForever.Script.Instance\WorldStory\HallOfTheHundred\TheWarhoundsOfVaregorEventScript.cs",
                "4404",
                "",
            )
        ]
        self.assertEqual("script_owner_matches_public_event", watchhound_event_override["owner_link_status"])
        self.assertIn("KillEventObjectiveUnit count 1", watchhound_event_override["blocker_summary"])
        self.assertIn("Creature2 67884 Varegor Watchhound", watchhound_event_override["blocker_summary"])

        watchhound_credit_override = audit.SCRIPT_OBJECTIVE_PRODUCER_OVERRIDES[
            (
                r"Source\NexusForever.Script.Instance\WorldStory\HallOfTheHundred\Script\VaregorEntityScript.cs",
                "4404",
                "",
            )
        ]
        self.assertEqual("script_owner_entity_filter_mapped_to_public_event", watchhound_credit_override["owner_link_status"])
        self.assertIn("VaregorWatchhoundEntityScript", watchhound_credit_override["blocker_summary"])
        self.assertIn("credits objective 4404 once on death", watchhound_credit_override["blocker_summary"])

        depth_event_override = audit.SCRIPT_OBJECTIVE_PRODUCER_OVERRIDES[
            (
                r"Source\NexusForever.Script.Instance\WorldStory\HallOfTheHundred\TheWarhoundsOfVaregorEventScript.cs",
                "4434",
                "7881",
            )
        ]
        self.assertEqual("script_owner_matches_public_event", depth_event_override["owner_link_status"])
        self.assertIn("Explore further into the Warhound Kennel", depth_event_override["blocker_summary"])
        self.assertIn("WorldLocation2 48661", depth_event_override["blocker_summary"])
        self.assertIn("mapped cluster objective 4441", depth_event_override["blocker_summary"])

        cluster_event_override = audit.SCRIPT_OBJECTIVE_PRODUCER_OVERRIDES[
            (
                r"Source\NexusForever.Script.Instance\WorldStory\HallOfTheHundred\TheWarhoundsOfVaregorEventScript.cs",
                "4441",
                "12262",
            )
        ]
        self.assertEqual("script_owner_matches_public_event", cluster_event_override["owner_link_status"])
        self.assertIn("KillClusterEventObjectiveUnit object 12262", cluster_event_override["blocker_summary"])
        self.assertIn("Creature2 67940 Frozen Lever", cluster_event_override["blocker_summary"])
        self.assertIn("67924 Cerebogg", cluster_event_override["blocker_summary"])

        cluster_credit_override = audit.SCRIPT_OBJECTIVE_PRODUCER_OVERRIDES[
            (
                r"Source\NexusForever.Script.Instance\WorldStory\HallOfTheHundred\Script\VaregorEntityScript.cs",
                "4441",
                "12262",
            )
        ]
        self.assertEqual("script_owner_entity_filter_mapped_to_public_event", cluster_credit_override["owner_link_status"])
        self.assertIn("FrozenLeverEntityScript", cluster_credit_override["blocker_summary"])
        self.assertIn("credits objective 4441 once on death", cluster_credit_override["blocker_summary"])

        depth_trigger_override = audit.SCRIPT_OBJECTIVE_PRODUCER_OVERRIDES[
            (
                r"Source\NexusForever.Script.Instance\WorldStory\HallOfTheHundred\Script\WarhoundKennelGridTriggerEntityScript.cs",
                "4434",
                "7881",
            )
        ]
        self.assertEqual("script_owner_entity_filter_mapped_to_public_event", depth_trigger_override["owner_link_status"])
        self.assertIn("ParticipantsInTriggerVolume object 7881", depth_trigger_override["blocker_summary"])
        self.assertIn("WarhoundKennelDepthGridTriggerEntityScript", depth_trigger_override["blocker_summary"])

    def test_vault_cold_and_hungry_overrides_track_frozen_carcass_credit(self) -> None:
        carcass_instance = audit.INSTANCE_OBJECTIVE_DEPENDENCY_OVERRIDES[(3009, 4378)]
        self.assertIn("ColdAndHungryEventScript.cs", carcass_instance["script_path"])
        self.assertIn("BridgeObjectiveEntityScripts.cs", carcass_instance["evidence_sources"])
        self.assertEqual("677;4378;12213;67659;48735", carcass_instance["script_owner_ids"])
        self.assertEqual(
            "runtime_vault_yeti_cave_frozen_carcass_checklist_credit_tested_pending_placement_visual_state_client_smoke",
            carcass_instance["evidence_status"],
        )
        self.assertIn("FrozenCarcassEntityScript", carcass_instance["blocker_summary"])
        self.assertIn("ActivateTargetGroupChecklist 12213", carcass_instance["blocker_summary"])
        self.assertIn("Frozen Carcasses 4378 checklist activation credit", audit.SCRIPTED_INSTANCE_BLOCKERS[3009])

    def test_vault_pell_graveyard_overrides_track_checklist_side_event_producers(self) -> None:
        parent_instance = audit.INSTANCE_OBJECTIVE_DEPENDENCY_OVERRIDES[(3009, 5295)]
        self.assertIn("TheGraveyardEchoedEventScript.cs", parent_instance["script_path"])
        self.assertIn("PellGraveyardGridTriggerEntityScript.cs", parent_instance["script_path"])
        self.assertEqual("666;5295;7861;48673;696;4451;7895;48675", parent_instance["script_owner_ids"])
        self.assertEqual(
            "runtime_vault_pell_graveyard_parent_objective_activation_credit_tested",
            parent_instance["evidence_status"],
        )
        self.assertEqual("automated_focus_pass", parent_instance["manual_validation"])

        investigate_instance = audit.INSTANCE_OBJECTIVE_DEPENDENCY_OVERRIDES[(3009, 4451)]
        self.assertIn("TheGraveyardEchoedEventScript.cs", investigate_instance["script_path"])
        self.assertIn("PellGraveyardGridTriggerEntityScript.cs", investigate_instance["script_path"])
        self.assertEqual("696;4451;7895;48675", investigate_instance["script_owner_ids"])
        self.assertEqual(
            "runtime_vault_pell_graveyard_investigate_trigger_activation_credit_tested",
            investigate_instance["evidence_status"],
        )
        self.assertEqual("automated_focus_pass", investigate_instance["manual_validation"])

        incense_instance = audit.INSTANCE_OBJECTIVE_DEPENDENCY_OVERRIDES[(3009, 4452)]
        self.assertIn("BridgeObjectiveEntityScripts.cs", incense_instance["script_path"])
        self.assertEqual("696;4452;12275;67962;48730", incense_instance["script_owner_ids"])
        self.assertIn("incense_checklist_activation_credit", incense_instance["evidence_status"])

        altar_instance = audit.INSTANCE_OBJECTIVE_DEPENDENCY_OVERRIDES[(3009, 4453)]
        self.assertEqual("696;4453;12302;68012;48690", altar_instance["script_owner_ids"])
        self.assertIn("altar_checklist_activation_credit", altar_instance["evidence_status"])

        primal_echoes_instance = audit.INSTANCE_OBJECTIVE_DEPENDENCY_OVERRIDES[(3009, 4462)]
        self.assertIn("exact_wave_choreography", primal_echoes_instance["evidence_status"])
        primal_wraith_instance = audit.INSTANCE_OBJECTIVE_DEPENDENCY_OVERRIDES[(3009, 4463)]
        self.assertIn("VaregorEntityScript.cs", primal_wraith_instance["script_path"])
        self.assertIn("primal_wraith_activation_death_credit", primal_wraith_instance["evidence_status"])

        parent_event_producer = audit.SCRIPT_OBJECTIVE_PRODUCER_OVERRIDES[
            (
                r"Source\NexusForever.Script.Instance\WorldStory\HallOfTheHundred\TheGraveyardEchoedEventScript.cs",
                "5295",
                "7861",
            )
        ]
        self.assertEqual("script_owner_matches_public_event", parent_event_producer["owner_link_status"])
        self.assertEqual(
            "runtime_vault_pell_graveyard_parent_objective_activation_tested",
            parent_event_producer["evidence_status"],
        )
        self.assertEqual("automated_focus_pass", parent_event_producer["manual_validation"])
        self.assertIn("main public event 666 objective 5295", parent_event_producer["blocker_summary"])
        self.assertIn("side-event graveyard phase opens", parent_event_producer["blocker_summary"])
        self.assertIn("WorldLocation2 48673", parent_event_producer["blocker_summary"])

        parent_trigger_producer = audit.SCRIPT_OBJECTIVE_PRODUCER_OVERRIDES[
            (
                r"Source\NexusForever.Script.Instance\WorldStory\HallOfTheHundred\Script\PellGraveyardGridTriggerEntityScript.cs",
                "5295",
                "7861",
            )
        ]
        self.assertEqual("script_owner_entity_filter_mapped_to_public_event", parent_trigger_producer["owner_link_status"])
        self.assertEqual(
            "runtime_vault_pell_graveyard_parent_objective_trigger_credit_tested",
            parent_trigger_producer["evidence_status"],
        )
        self.assertEqual("automated_focus_pass", parent_trigger_producer["manual_validation"])
        self.assertIn("credits side trigger objective 4451 and main-event parent objective 5295", parent_trigger_producer["blocker_summary"])
        self.assertIn("ParticipantsInTriggerVolume object 7895", parent_trigger_producer["blocker_summary"])

        investigate_event_producer = audit.SCRIPT_OBJECTIVE_PRODUCER_OVERRIDES[
            (
                r"Source\NexusForever.Script.Instance\WorldStory\HallOfTheHundred\TheGraveyardEchoedEventScript.cs",
                "4451",
                "7895",
            )
        ]
        self.assertEqual("script_owner_matches_public_event", investigate_event_producer["owner_link_status"])
        self.assertEqual(
            "runtime_vault_pell_graveyard_investigate_trigger_activation_tested",
            investigate_event_producer["evidence_status"],
        )
        self.assertEqual("automated_focus_pass", investigate_event_producer["manual_validation"])

        investigate_trigger_producer = audit.SCRIPT_OBJECTIVE_PRODUCER_OVERRIDES[
            (
                r"Source\NexusForever.Script.Instance\WorldStory\HallOfTheHundred\Script\PellGraveyardGridTriggerEntityScript.cs",
                "4451",
                "7895",
            )
        ]
        self.assertEqual("script_owner_entity_filter_mapped_to_public_event", investigate_trigger_producer["owner_link_status"])
        self.assertEqual(
            "runtime_vault_pell_graveyard_investigate_trigger_credit_tested",
            investigate_trigger_producer["evidence_status"],
        )
        self.assertEqual("automated_focus_pass", investigate_trigger_producer["manual_validation"])

        incense_producer = audit.SCRIPT_OBJECTIVE_PRODUCER_OVERRIDES[
            (
                r"Source\NexusForever.Script.Instance\WorldStory\HallOfTheHundred\Script\BridgeObjectiveEntityScripts.cs",
                "4452",
                "12275",
            )
        ]
        self.assertEqual("script_owner_entity_filter_mapped_to_public_event", incense_producer["owner_link_status"])
        self.assertIn("ActivateTargetGroupChecklist object 12275", incense_producer["blocker_summary"])
        self.assertIn("Creature2 67962 Unlit Incense", incense_producer["blocker_summary"])

        altar_producer = audit.SCRIPT_OBJECTIVE_PRODUCER_OVERRIDES[
            (
                r"Source\NexusForever.Script.Instance\WorldStory\HallOfTheHundred\Script\BridgeObjectiveEntityScripts.cs",
                "4453",
                "12302",
            )
        ]
        self.assertIn("ActivateTargetGroupChecklist object 12302", altar_producer["blocker_summary"])
        self.assertIn("Creature2 68012 Sacred Bas-Relief", altar_producer["blocker_summary"])

        primal_echoes_producer = audit.SCRIPT_OBJECTIVE_PRODUCER_OVERRIDES[
            (
                r"Source\NexusForever.Script.Instance\WorldStory\HallOfTheHundred\TheGraveyardEchoedEventScript.cs",
                "4462",
                "",
            )
        ]
        self.assertIn("count-one Script row", primal_echoes_producer["blocker_summary"])
        self.assertIn("exact Primal Echo wave composition", primal_echoes_producer["blocker_summary"])

        primal_wraith_producer = audit.SCRIPT_OBJECTIVE_PRODUCER_OVERRIDES[
            (
                r"Source\NexusForever.Script.Instance\WorldStory\HallOfTheHundred\Script\VaregorEntityScript.cs",
                "4463",
                "",
            )
        ]
        self.assertIn("Creature2 68013 Primal Wraith", primal_wraith_producer["blocker_summary"])
        self.assertIn("credits objective 4463 once on death", primal_wraith_producer["blocker_summary"])
        self.assertIn("Pell Graveyard parent objective 5295 activation/credit", audit.SCRIPTED_INSTANCE_BLOCKERS[3009])
        self.assertIn("Pell Graveyard side objectives", audit.SCRIPTED_INSTANCE_BLOCKERS[3009])

    def test_war_of_the_wilds_moodie_totem_resourcepool_override_tracks_full_count_credit(self) -> None:
        destroy_override = audit.SCRIPT_OBJECTIVE_PRODUCER_OVERRIDES[
            (
                r"Source\NexusForever.Script.Instance\Adventure\WarOfTheWilds\Script\GiantMoodieTotemEntityScript.cs",
                "408",
                "2812",
            )
        ]
        self.assertEqual("158;408;2812;25952", destroy_override["script_owner_ids"])
        self.assertEqual("script_owner_entity_filter_mapped_to_public_event", destroy_override["owner_link_status"])
        self.assertEqual(
            "runtime_war_wilds_giant_moodie_totem_resourcepool_full_count_credit_tested_pending_health_pool_smoke",
            destroy_override["evidence_status"],
        )
        self.assertIn("ResourcePool object 2812, count 100", destroy_override["blocker_summary"])
        self.assertIn("Creature2 25952 Giant Moodie Totem", destroy_override["blocker_summary"])

        health_override = audit.SCRIPT_OBJECTIVE_PRODUCER_OVERRIDES[
            (
                r"Source\NexusForever.Script.Instance\Adventure\WarOfTheWilds\Script\GiantMoodieTotemEntityScript.cs",
                "1659",
                "2812",
            )
        ]
        self.assertEqual("158;1659;2812;25952", health_override["script_owner_ids"])
        self.assertEqual("script_owner_entity_filter_mapped_to_public_event", health_override["owner_link_status"])
        self.assertEqual(
            "runtime_war_wilds_moodie_totem_health_resourcepool_full_count_credit_tested_pending_progressive_health_smoke",
            health_override["evidence_status"],
        )
        self.assertIn("Moodie Totem Health", health_override["blocker_summary"])
        self.assertIn("Skeech health objective 1658", health_override["blocker_summary"])

    def test_ultimate_protogames_tank_room_overrides_track_placement_and_death_credit(self) -> None:
        destruct_dependency = audit.INSTANCE_OBJECTIVE_DEPENDENCY_OVERRIDES[(2980, 2676)]
        self.assertIn("UltimateProtogamesObjectiveEntityScripts.cs", destruct_dependency["script_path"])
        self.assertIn("UltimateProtogamesEventScript.cs", destruct_dependency["script_path"])
        self.assertIn("8193", destruct_dependency["script_owner_ids"])
        self.assertIn("6144867", destruct_dependency["script_owner_ids"])
        self.assertEqual(
            "runtime_up_destruct_o_derby_tank_room_jabbithole_placements_dynamic_max_and_death_credit_tested",
            destruct_dependency["evidence_status"],
        )

        tank_trample_dependency = audit.INSTANCE_OBJECTIVE_DEPENDENCY_OVERRIDES[(2980, 2868)]
        self.assertIn("UltimateProtogamesObjectiveEntityScripts.cs", tank_trample_dependency["script_path"])
        self.assertIn("UltimateProtogamesEventScript.cs", tank_trample_dependency["script_path"])
        self.assertIn("6144867", tank_trample_dependency["script_owner_ids"])
        self.assertIn("1100300060", tank_trample_dependency["script_owner_ids"])
        self.assertEqual(
            "runtime_up_tank_trample_jabbithole_placements_and_death_credit_tested",
            tank_trample_dependency["evidence_status"],
        )

        destruct_activation_producer = audit.SCRIPT_OBJECTIVE_PRODUCER_OVERRIDES[
            (
                r"Source\NexusForever.Script.Instance\Dungeon\UltimateProtogames\UltimateProtogamesEventScript.cs",
                "2676",
                "",
            )
        ]
        self.assertEqual("script_owner_matches_public_event", destruct_activation_producer["owner_link_status"])
        self.assertIn("TimedWin", destruct_activation_producer["blocker_summary"])
        self.assertIn("failureTimeMs 180000", destruct_activation_producer["blocker_summary"])
        self.assertIn("dynamic max count 3", destruct_activation_producer["blocker_summary"])

        destruct_credit_producer = audit.SCRIPT_OBJECTIVE_PRODUCER_OVERRIDES[
            (
                r"Source\NexusForever.Script.Instance\Dungeon\UltimateProtogames\Script\UltimateProtogamesObjectiveEntityScripts.cs",
                "2676",
                "",
            )
        ]
        self.assertEqual("script_owner_entity_filter_mapped_to_public_event", destruct_credit_producer["owner_link_status"])
        self.assertEqual(
            "runtime_up_destruct_o_derby_tank_room_death_credit_tested",
            destruct_credit_producer["evidence_status"],
        )
        self.assertIn("TimedWin", destruct_credit_producer["blocker_summary"])
        self.assertIn("failureTimeMs 180000", destruct_credit_producer["blocker_summary"])
        self.assertIn("credits objective 2676 once per tank death", destruct_credit_producer["blocker_summary"])

        can_crusher_dependency = audit.INSTANCE_OBJECTIVE_DEPENDENCY_OVERRIDES[(2980, 2869)]
        self.assertIn("6145453", can_crusher_dependency["script_owner_ids"])
        self.assertIn("1100300061", can_crusher_dependency["script_owner_ids"])

        going_green_dependency = audit.INSTANCE_OBJECTIVE_DEPENDENCY_OVERRIDES[(2980, 2872)]
        self.assertEqual(
            "594;2872;12671;62542;62543;62546;299;8182;28491;28496;28516;32287;6144867;6145453;6150582;1100300060;1100300061;1100300062",
            going_green_dependency["script_owner_ids"],
        )
        self.assertEqual(
            "runtime_up_going_green_all_three_junk_jabbithole_placements_dynamic_max_and_death_credit_tested",
            going_green_dependency["evidence_status"],
        )
        self.assertEqual(
            "automated_focus_pass_pending_timer_and_client_smoke",
            going_green_dependency["manual_validation"],
        )

        tank_trample_producer = audit.SCRIPT_OBJECTIVE_PRODUCER_OVERRIDES[
            (
                r"Source\NexusForever.Script.Instance\Dungeon\UltimateProtogames\Script\UltimateProtogamesObjectiveEntityScripts.cs",
                "2868",
                "12671",
            )
        ]
        self.assertEqual("script_owner_entity_filter_mapped_to_public_event", tank_trample_producer["owner_link_status"])
        self.assertIn("TargetGroup 12671", tank_trample_producer["blocker_summary"])
        self.assertIn("62542 Busted Red Tank", tank_trample_producer["blocker_summary"])
        self.assertIn("spawns the three reviewed Jabbithole/DataMapping tank placements", tank_trample_producer["blocker_summary"])

        can_crusher_producer = audit.SCRIPT_OBJECTIVE_PRODUCER_OVERRIDES[
            (
                r"Source\NexusForever.Script.Instance\Dungeon\UltimateProtogames\Script\UltimateProtogamesObjectiveEntityScripts.cs",
                "2869",
                "12671",
            )
        ]
        self.assertIn("sibling all-three dynamic timer semantics", can_crusher_producer["blocker_summary"])

        tank_room_activation_producer = audit.SCRIPT_OBJECTIVE_PRODUCER_OVERRIDES[
            (
                r"Source\NexusForever.Script.Instance\Dungeon\UltimateProtogames\UltimateProtogamesEventScript.cs",
                "2872",
                "12671",
            )
        ]
        self.assertEqual("script_owner_matches_public_event", tank_room_activation_producer["owner_link_status"])
        self.assertIn("TankRoom phase", tank_room_activation_producer["blocker_summary"])
        self.assertIn("dynamic max count 3", tank_room_activation_producer["blocker_summary"])
        self.assertIn("6144867, 6145453, and 6150582", tank_room_activation_producer["blocker_summary"])

        going_green_producer = audit.SCRIPT_OBJECTIVE_PRODUCER_OVERRIDES[
            (
                r"Source\NexusForever.Script.Instance\Dungeon\UltimateProtogames\Script\UltimateProtogamesObjectiveEntityScripts.cs",
                "2872",
                "12671",
            )
        ]
        self.assertEqual("script_owner_entity_filter_mapped_to_public_event", going_green_producer["owner_link_status"])
        self.assertEqual(
            "runtime_up_going_green_all_three_junk_jabbithole_placements_dynamic_max_and_death_credit_tested",
            going_green_producer["evidence_status"],
        )
        self.assertEqual(
            "automated_focus_pass_pending_timer_and_client_smoke",
            going_green_producer["manual_validation"],
        )
        self.assertIn("failureTimeMs 150000", going_green_producer["blocker_summary"])
        self.assertIn("Jabbithole database maps public event row 299", going_green_producer["blocker_summary"])
        self.assertIn("dynamic max count 3", going_green_producer["blocker_summary"])
        self.assertNotIn("dynamic max initialization", going_green_producer["blocker_summary"])
        self.assertIn("coordinate rows 6144867/6145453/6150582", going_green_producer["blocker_summary"])

        dependency_statuses = {
            2676: "runtime_up_destruct_o_derby_tank_room_jabbithole_placements_dynamic_max_and_death_credit_tested",
            2868: "runtime_up_tank_trample_jabbithole_placements_and_death_credit_tested",
            2869: "runtime_up_can_crusher_jabbithole_placements_and_death_credit_tested",
            2872: "runtime_up_going_green_all_three_junk_jabbithole_placements_dynamic_max_and_death_credit_tested",
        }

        for objective_id, evidence_status in dependency_statuses.items():
            with self.subTest(source="dependency", objective_id=objective_id):
                dependency = audit.INSTANCE_OBJECTIVE_DEPENDENCY_OVERRIDES[(2980, objective_id)]
                self.assertEqual(evidence_status, dependency["evidence_status"])

        event_script = r"Source\NexusForever.Script.Instance\Dungeon\UltimateProtogames\UltimateProtogamesEventScript.cs"
        entity_script = r"Source\NexusForever.Script.Instance\Dungeon\UltimateProtogames\Script\UltimateProtogamesObjectiveEntityScripts.cs"
        producer_statuses = {
            (event_script, "2676", ""): "runtime_up_destruct_o_derby_tank_room_phase_activation_jabbithole_placements_and_dynamic_max_tested",
            (event_script, "2868", "12671"): "runtime_up_tank_room_phase_activation_and_jabbithole_placements_tested",
            (event_script, "2869", "12671"): "runtime_up_tank_room_phase_activation_and_jabbithole_placements_tested",
            (event_script, "2872", "12671"): "runtime_up_tank_room_phase_activation_jabbithole_placements_and_dynamic_max_tested",
            (entity_script, "2676", ""): "runtime_up_destruct_o_derby_tank_room_death_credit_tested",
            (entity_script, "2868", "12671"): "runtime_up_tank_trample_jabbithole_placements_and_death_credit_tested",
            (entity_script, "2869", "12671"): "runtime_up_can_crusher_jabbithole_placements_and_death_credit_tested",
            (entity_script, "2872", "12671"): "runtime_up_going_green_all_three_junk_jabbithole_placements_dynamic_max_and_death_credit_tested",
        }

        for key, evidence_status in producer_statuses.items():
            with self.subTest(source="producer", key=key):
                producer = audit.SCRIPT_OBJECTIVE_PRODUCER_OVERRIDES[key]
                self.assertEqual(evidence_status, producer["evidence_status"])

        self.assertIn("tank-room objectives 2676/2868/2869/2872 activation, Jabbithole placement spawns, WIP handoff, all-three dynamic max count-3 initialization, and death credit", audit.SCRIPTED_INSTANCE_BLOCKERS[2980])
        self.assertNotIn("tank-room all-three timed cleanup objective 2872", audit.SCRIPTED_INSTANCE_BLOCKERS[2980])

    def test_ultimate_protogames_gilded_fowl_overrides_track_paired_death_credit(self) -> None:
        gilded_fowl_dependency = audit.INSTANCE_OBJECTIVE_DEPENDENCY_OVERRIDES[(2980, 2862)]
        self.assertIn("UltimateProtogamesObjectiveEntityScripts.cs", gilded_fowl_dependency["script_path"])
        self.assertIn("UltimateProtogamesEventScript.cs", gilded_fowl_dependency["script_path"])
        self.assertEqual(
            "594;2862;12263;63055;46473;299;2185;28278;8011506;1100300089;21895;1322;461770",
            gilded_fowl_dependency["script_owner_ids"],
        )
        self.assertEqual(
            "runtime_up_gilded_fowl_phase_activation_jabbithole_placement_and_resourcepool_death_credit_tested",
            gilded_fowl_dependency["evidence_status"],
        )

        gilded_fowl_full_count = audit.INSTANCE_OBJECTIVE_DEPENDENCY_OVERRIDES[(2980, 4442)]
        self.assertIn("UltimateProtogamesEventScript.cs", gilded_fowl_full_count["script_path"])
        self.assertEqual(
            "594;4442;12263;63055;46473;299;2185;28278;8011506;1100300089;21895;1322;461770;100",
            gilded_fowl_full_count["script_owner_ids"],
        )
        self.assertEqual(
            "runtime_up_gilded_fowl_phase_activation_jabbithole_placement_and_killcluster_full_count_credit_tested",
            gilded_fowl_full_count["evidence_status"],
        )

        resource_pool_producer = audit.SCRIPT_OBJECTIVE_PRODUCER_OVERRIDES[
            (
                r"Source\NexusForever.Script.Instance\Dungeon\UltimateProtogames\Script\UltimateProtogamesObjectiveEntityScripts.cs",
                "2862",
                "12263",
            )
        ]
        self.assertEqual("script_owner_entity_filter_mapped_to_public_event", resource_pool_producer["owner_link_status"])
        self.assertEqual(
            "runtime_up_gilded_fowl_phase_activation_jabbithole_placement_and_resourcepool_death_credit_tested",
            resource_pool_producer["evidence_status"],
        )
        self.assertIn("ResourcePool object 12263, count 1", resource_pool_producer["blocker_summary"])
        self.assertIn("Creature2 63055 Gilded Fowl", resource_pool_producer["blocker_summary"])
        self.assertIn("coordinate 8011506", resource_pool_producer["blocker_summary"])

        full_count_producer = audit.SCRIPT_OBJECTIVE_PRODUCER_OVERRIDES[
            (
                r"Source\NexusForever.Script.Instance\Dungeon\UltimateProtogames\Script\UltimateProtogamesObjectiveEntityScripts.cs",
                "4442",
                "12263",
            )
        ]
        self.assertEqual(
            "runtime_up_gilded_fowl_phase_activation_jabbithole_placement_and_killcluster_full_count_credit_tested",
            full_count_producer["evidence_status"],
        )
        self.assertIn("KillClusterTargetGroup object 12263, count 100", full_count_producer["blocker_summary"])
        self.assertIn("Power Plunge qualification", full_count_producer["blocker_summary"])
        self.assertIn("script-owned Gilded Fowl entity 1100300089", full_count_producer["blocker_summary"])

        resource_pool_event_producer = audit.SCRIPT_OBJECTIVE_PRODUCER_OVERRIDES[
            (
                r"Source\NexusForever.Script.Instance\Dungeon\UltimateProtogames\UltimateProtogamesEventScript.cs",
                "2862",
                "12263",
            )
        ]
        self.assertEqual("script_owner_matches_public_event", resource_pool_event_producer["owner_link_status"])
        self.assertEqual(
            "runtime_up_gilded_fowl_phase_activation_jabbithole_placement_tested",
            resource_pool_event_producer["evidence_status"],
        )
        self.assertIn("source coordinate 8011506", resource_pool_event_producer["blocker_summary"])
        self.assertIn("script-owned entity 1100300089", resource_pool_event_producer["blocker_summary"])

        full_count_event_producer = audit.SCRIPT_OBJECTIVE_PRODUCER_OVERRIDES[
            (
                r"Source\NexusForever.Script.Instance\Dungeon\UltimateProtogames\UltimateProtogamesEventScript.cs",
                "4442",
                "12263",
            )
        ]
        self.assertEqual("script_owner_matches_public_event", full_count_event_producer["owner_link_status"])
        self.assertEqual(
            "runtime_up_gilded_fowl_phase_activation_jabbithole_placement_tested",
            full_count_event_producer["evidence_status"],
        )
        self.assertIn("source coordinate 8011506", full_count_event_producer["blocker_summary"])
        self.assertIn("Power Plunge qualification", full_count_event_producer["blocker_summary"])
        self.assertIn(
            "Gilded Fowl objectives 2862/4442 WIP phase activation, Jabbithole/DataMapping placement spawn, and death credit",
            audit.SCRIPTED_INSTANCE_BLOCKERS[2980],
        )

    def test_ultimate_protogames_huthut_overrides_track_placement_and_death_credit(self) -> None:
        huthut_dependency = audit.INSTANCE_OBJECTIVE_DEPENDENCY_OVERRIDES[(2980, 2675)]
        self.assertIn("UltimateProtogamesObjectiveEntityScripts.cs", huthut_dependency["script_path"])
        self.assertIn("UltimateProtogamesEventScript.cs", huthut_dependency["script_path"])
        self.assertEqual(
            "594;2675;61417;42036;46229;299;8164;28298;30591;8208290;1100300091;29048;1322;2400000",
            huthut_dependency["script_owner_ids"],
        )
        self.assertEqual(
            "runtime_up_huthut_phase_activation_jabbithole_datamapping_placement_and_death_credit_tested",
            huthut_dependency["evidence_status"],
        )
        self.assertEqual(
            "automated_focus_pass_pending_football_scoring_and_client_smoke",
            huthut_dependency["manual_validation"],
        )

        entity_script = r"Source\NexusForever.Script.Instance\Dungeon\UltimateProtogames\Script\UltimateProtogamesObjectiveEntityScripts.cs"
        death_credit_producer = audit.SCRIPT_OBJECTIVE_PRODUCER_OVERRIDES[
            (
                entity_script,
                "2675",
                "",
            )
        ]
        self.assertEqual("script_owner_entity_filter_mapped_to_public_event", death_credit_producer["owner_link_status"])
        self.assertEqual(
            "runtime_up_huthut_direct_script_death_credit_tested",
            death_credit_producer["evidence_status"],
        )
        self.assertIn("Creature2 61417", death_credit_producer["blocker_summary"])
        self.assertIn("credits objective 2675 once on death", death_credit_producer["blocker_summary"])
        self.assertIn("football scoring", death_credit_producer["blocker_summary"])

        event_script = r"Source\NexusForever.Script.Instance\Dungeon\UltimateProtogames\UltimateProtogamesEventScript.cs"
        phase_producer = audit.SCRIPT_OBJECTIVE_PRODUCER_OVERRIDES[
            (
                event_script,
                "2675",
                "",
            )
        ]
        self.assertEqual("script_owner_matches_public_event", phase_producer["owner_link_status"])
        self.assertEqual(
            "runtime_up_huthut_phase_activation_and_datamapping_placement_tested",
            phase_producer["evidence_status"],
        )
        self.assertIn("source_coordinate_id 8208290", phase_producer["blocker_summary"])
        self.assertIn("script-owned Hut-Hut entity 1100300091", phase_producer["blocker_summary"])
        self.assertIn("Hut-Hut football scoring", audit.SCRIPTED_INSTANCE_BLOCKERS[2980])

    def test_fragment_zero_public_event_creature_overrides_cover_target_group_rows_only(self) -> None:
        expected_status_by_keys = {
            "runtime_fragment_zero_prototype_matron_public_event_creature_death_credit_tested_pending_spawn_route_and_client_smoke": {
                (680, 2094, 69664),
                (680, 2156, 69672),
                (680, 2160, 69673),
                (680, 2165, 69674),
                (680, 2224, 67528),
                (680, 2225, 69088),
                (680, 2278, 67527),
                (680, 2279, 67526),
            },
            "runtime_fragment_zero_life_overseer_public_event_creature_death_credit_tested_pending_encounter_and_client_smoke": {
                (680, 2090, 69635),
                (680, 2229, 67522),
            },
            "runtime_fragment_zero_target_group_public_event_creature_activation_credit_tested_pending_visuals_placement_and_client_smoke": {
                (680, 2164, 68856),
                (680, 2196, 67966),
                (680, 2208, 67931),
            },
            "runtime_fragment_zero_skeech_horde_public_event_creature_death_credit_tested_pending_spawn_route_and_client_smoke": {
                (680, 2367, 67519),
                (680, 2372, 67520),
            },
        }

        for expected_status, keys in expected_status_by_keys.items():
            for key in keys:
                with self.subTest(key=key):
                    override = audit.PUBLIC_EVENT_CREATURE_EVIDENCE_OVERRIDES[key]
                    self.assertEqual(expected_status, override["evidence_status"])
                    self.assertIn("Fragment Zero", override["blocker_summary"])
                    self.assertIn("no longer double-counted", override["blocker_summary"])
                    self.assertIn(key, audit.PUBLIC_EVENT_CREATURE_BLOCKER_EXEMPTIONS)

        still_blocked_keys = {
            (680, 2091, 69622),
            (680, 2096, 69794),
            (680, 2298, 69682),
            (680, 2299, 69681),
            (680, 2641, 37899),
        }
        for key in still_blocked_keys:
            with self.subTest(still_blocked_key=key):
                self.assertNotIn(key, audit.PUBLIC_EVENT_CREATURE_EVIDENCE_OVERRIDES)
                self.assertNotIn(key, audit.PUBLIC_EVENT_CREATURE_BLOCKER_EXEMPTIONS)

    def test_fragment_zero_cargo_crate_overrides_track_shared_virtualcollect_tiers(self) -> None:
        cases = [
            (
                4431,
                "680;4431;1176;67965",
                "runtime_fragment_zero_cargo_crate_10_count_virtualcollect_credit_tested_pending_placement_lifecycle_and_client_smoke",
                "runtime_fragment_zero_cargo_crate_10_count_activation_tested",
                "count 10",
            ),
            (
                4684,
                "680;4684;1176;67965",
                "runtime_fragment_zero_cargo_crate_100_count_virtualcollect_credit_tested_pending_placement_lifecycle_and_client_smoke",
                "runtime_fragment_zero_cargo_crate_100_count_activation_tested",
                "count 100",
            ),
        ]

        for objective_id, owner_ids, dependency_status, producer_status, count_summary in cases:
            with self.subTest(objective_id=objective_id):
                dependency = audit.INSTANCE_OBJECTIVE_DEPENDENCY_OVERRIDES[(3180, objective_id)]
                self.assertIn("FragmentZeroEventScript.cs", dependency["script_path"])
                self.assertIn("FragmentZeroObjectiveEntityScripts.cs", dependency["script_path"])
                self.assertEqual(owner_ids, dependency["script_owner_ids"])
                self.assertEqual(dependency_status, dependency["evidence_status"])

                producer = audit.SCRIPT_OBJECTIVE_PRODUCER_OVERRIDES[
                    (
                        r"Source\NexusForever.Script.Instance\Expedition\FragmentZero\FragmentZeroEventScript.cs",
                        str(objective_id),
                        "1176",
                    )
                ]
                self.assertEqual(owner_ids, producer["script_owner_ids"])
                self.assertEqual("script_owner_matches_public_event", producer["owner_link_status"])
                self.assertEqual(producer_status, producer["evidence_status"])
                self.assertIn(count_summary, producer["blocker_summary"])
                self.assertIn("Creature2 67965", producer["blocker_summary"])

        shared_producer = audit.SCRIPT_OBJECTIVE_PRODUCER_OVERRIDES[
            (
                r"Source\NexusForever.Script.Instance\Expedition\FragmentZero\Script\FragmentZeroObjectiveEntityScripts.cs",
                "4431;4684",
                "1176",
            )
        ]
        self.assertEqual("680;4431;4684;1176;67965", shared_producer["script_owner_ids"])
        self.assertEqual("script_owner_entity_filter_mapped_to_public_event", shared_producer["owner_link_status"])
        self.assertEqual("runtime_fragment_zero_cargo_crate_shared_virtualcollect_credit_tested", shared_producer["evidence_status"])
        self.assertIn("objectives 4431 and 4684", shared_producer["blocker_summary"])
        self.assertIn("VirtualCollect object 1176", shared_producer["blocker_summary"])

    def test_fragment_zero_xenobite_egg_overrides_track_jabbithole_backed_producer(self) -> None:
        dependency = audit.INSTANCE_OBJECTIVE_DEPENDENCY_OVERRIDES[(3180, 4632)]
        self.assertIn("FragmentZeroEventScript.cs", dependency["script_path"])
        self.assertIn("FragmentZeroObjectiveEntityScripts.cs", dependency["script_path"])
        self.assertEqual("680;4632;7892;68811;68812;28247;28351", dependency["script_owner_ids"])
        self.assertIn("jabbithole.creatures", dependency["evidence_sources"])

        activation_producer = audit.SCRIPT_OBJECTIVE_PRODUCER_OVERRIDES[
            (
                r"Source\NexusForever.Script.Instance\Expedition\FragmentZero\FragmentZeroEventScript.cs",
                "4632",
                "7892",
            )
        ]
        self.assertEqual("script_owner_matches_public_event", activation_producer["owner_link_status"])
        self.assertEqual("runtime_fragment_zero_xenobite_egg_objective_activation_tested", activation_producer["evidence_status"])
        self.assertIn("count 30", activation_producer["blocker_summary"])

        egg_producer = audit.SCRIPT_OBJECTIVE_PRODUCER_OVERRIDES[
            (
                r"Source\NexusForever.Script.Instance\Expedition\FragmentZero\Script\FragmentZeroObjectiveEntityScripts.cs",
                "4632",
                "7892",
            )
        ]
        self.assertEqual("script_owner_entity_filter_mapped_to_public_event", egg_producer["owner_link_status"])
        self.assertEqual("runtime_fragment_zero_xenobite_egg_direct_script_credit_tested", egg_producer["evidence_status"])
        self.assertIn("Creature2 rows 68811", egg_producer["blocker_summary"])
        self.assertIn("jabbithole creature ids 28247/28351", egg_producer["blocker_summary"])
        self.assertIn("Xenobite Egg objective 4632", audit.SCRIPTED_INSTANCE_BLOCKERS[3180])

    def test_fragment_zero_residual_search_corpse_ambush_and_timer_rows_are_concrete(self) -> None:
        cases = [
            (
                4397,
                "680;4397;48709;4397",
                "runtime_fragment_zero_search_crew_turnstile_trigger_activation_tested_pending_exact_trigger_replay_and_client_smoke",
            ),
            (
                4420,
                "680;4420;7919;48732;2340",
                "runtime_fragment_zero_biomatics_search_grid_trigger_credit_tested_pending_exact_trigger_replay_and_client_smoke",
            ),
            (
                4421,
                "680;4421;7892;48521;2308",
                "runtime_fragment_zero_incubation_search_grid_trigger_credit_tested_pending_exact_trigger_replay_and_client_smoke",
            ),
            (
                4416,
                "680;4416;12254;67516;67518;67519;67520",
                "runtime_fragment_zero_skeech_horde_base_kill_activation_and_death_credit_tested_pending_spawn_route_and_client_smoke",
            ),
            (
                4424,
                "680;4424;12274;67966;48716;2310",
                "runtime_fragment_zero_jo_corpse_target_group_activation_credit_tested_pending_placement_visuals_and_client_smoke",
            ),
            (
                4425,
                "680;4425;49225",
                "mapped_fragment_zero_skeech_ambush_blocked_missing_wave_completion_producer_client_smoke",
            ),
            (
                4584,
                "680;4584;12254;67516;67518;67519;67520",
                "runtime_fragment_zero_skeech_horde_tier1_activation_and_death_credit_tested_pending_spawn_route_and_client_smoke",
            ),
            (
                4585,
                "680;4585;12254;67516;67518;67519;67520",
                "runtime_fragment_zero_skeech_horde_tier2_activation_and_death_credit_tested_pending_spawn_route_and_client_smoke",
            ),
            (
                4586,
                "680;4586;12254;67516;67518;67519;67520",
                "runtime_fragment_zero_skeech_horde_tier3_activation_and_death_credit_tested_pending_spawn_route_and_client_smoke",
            ),
            (
                4423,
                "680;4423;12269;67526;69672",
                "runtime_fragment_zero_prototype_alpha_activation_and_death_credit_tested_pending_spawn_route_and_client_smoke",
            ),
            (
                4449,
                "680;4449;12270;67527;69673",
                "runtime_fragment_zero_prototype_beta_activation_and_death_credit_tested_pending_spawn_route_and_client_smoke",
            ),
            (
                4450,
                "680;4450;12271;67528;69674",
                "runtime_fragment_zero_prototype_delta_activation_and_death_credit_tested_pending_spawn_route_and_client_smoke",
            ),
            (
                4417,
                "680;4417;12255;69088;69664",
                "runtime_fragment_zero_project_matron_activation_and_death_credit_tested_pending_encounter_mechanics_and_client_smoke",
            ),
            (
                4444,
                "680;4444;12266;67522;69635",
                "runtime_fragment_zero_life_overseer_activation_and_death_credit_tested_pending_encounter_mechanics_and_client_smoke",
            ),
            (
                4432,
                "680;4432;12264;67931;48726;2347",
                "runtime_fragment_zero_syrus_corpse_target_group_activation_credit_tested_pending_placement_visuals_and_client_smoke",
            ),
            (
                4445,
                "680;4445;12267;67961;48674;2304",
                "runtime_fragment_zero_locate_hugo_target_group_activation_credit_tested_pending_projection_placement_and_client_smoke",
            ),
            (
                4690,
                "680;4690;1176;48727;1800000",
                "runtime_fragment_zero_gold_medal_timer_activation_and_final_credit_tested_pending_timer_reward_and_client_smoke",
            ),
        ]

        for objective_id, owner_ids, evidence_status in cases:
            with self.subTest(objective_id=objective_id):
                dependency = audit.INSTANCE_OBJECTIVE_DEPENDENCY_OVERRIDES[(3180, objective_id)]
                self.assertIn("FragmentZeroEventScript.cs", dependency["script_path"])
                self.assertEqual(owner_ids, dependency["script_owner_ids"])
                self.assertEqual(evidence_status, dependency["evidence_status"])

        event_producer_statuses = {
            ("4397", ""): "runtime_fragment_zero_search_crew_turnstile_activation_and_wip_trigger_spawn_tested_pending_exact_trigger_replay_and_client_smoke",
            ("4420", "7919"): "runtime_fragment_zero_biomatics_search_activation_and_grid_trigger_spawn_tested_pending_exact_trigger_replay_and_client_smoke",
            ("4421", "7892"): "runtime_fragment_zero_incubation_search_activation_and_grid_trigger_spawn_tested_pending_exact_trigger_replay_and_client_smoke",
            ("4424", "12274"): "runtime_fragment_zero_jo_corpse_objective_activation_tested_pending_placement_visuals_and_client_smoke",
            ("4425", ""): "mapped_fragment_zero_skeech_ambush_activation_only_blocked_missing_wave_completion_producer_client_smoke",
            ("4422", "7902"): "runtime_fragment_zero_follow_friendly_skeech_activation_and_wip_trigger_spawn_tested_pending_exact_trigger_replay_and_client_smoke",
            ("4426", "7903"): "runtime_fragment_zero_continue_search_missing_crew_activation_tested_pending_branch_credit_timing_and_client_smoke",
            ("4693", "1190"): "runtime_fragment_zero_black_box_activation_tested_pending_object_placement_credit_and_client_smoke",
            ("4427", "7896"): "runtime_fragment_zero_incubation_airlock_activation_tested_pending_airlock_trigger_and_client_smoke",
            ("4429", "7904"): "runtime_fragment_zero_life_overseer_search_activation_tested_pending_cargo_route_and_client_smoke",
            ("4418", "7916"): "runtime_fragment_zero_biomatics_airlock_activation_tested_pending_airlock_trigger_and_client_smoke",
            ("4430", "7905"): "runtime_fragment_zero_continue_search_hugo_activation_tested_pending_hugo_route_and_client_smoke",
            ("4443", "7891"): "runtime_fragment_zero_search_for_hugo_activation_tested_pending_projection_route_and_client_smoke",
            ("4446", "7893"): "runtime_fragment_zero_life_overseer_airlock_activation_and_wip_trigger_spawn_tested_pending_exact_trigger_replay_and_client_smoke",
            ("4635", "12461"): "runtime_fragment_zero_automated_defense_panel_checklist_activation_tested",
            ("4416", "12254"): "runtime_fragment_zero_skeech_horde_base_kill_activation_tested",
            ("4584", "12254"): "runtime_fragment_zero_skeech_horde_tier1_activation_tested",
            ("4585", "12254"): "runtime_fragment_zero_skeech_horde_tier2_activation_tested",
            ("4586", "12254"): "runtime_fragment_zero_skeech_horde_tier3_activation_tested",
            ("4423", "12269"): "runtime_fragment_zero_prototype_alpha_activation_tested",
            ("4449", "12270"): "runtime_fragment_zero_prototype_beta_activation_tested",
            ("4450", "12271"): "runtime_fragment_zero_prototype_delta_activation_tested",
            ("4417", "12255"): "runtime_fragment_zero_project_matron_activation_tested",
            ("4444", "12266"): "runtime_fragment_zero_life_overseer_activation_tested",
            ("4432", "12264"): "runtime_fragment_zero_syrus_corpse_objective_activation_tested_pending_placement_visuals_and_client_smoke",
            ("4445", "12267"): "runtime_fragment_zero_locate_hugo_objective_activation_tested_pending_projection_placement_and_client_smoke",
            ("4655", ""): "runtime_fragment_zero_wait_for_hugo_activation_and_zero_count_credit_tested_pending_hugo_timing_and_client_smoke",
            ("4641", "12471"): "runtime_fragment_zero_hugo_aura_talk_objective_activation_tested_pending_stay_close_and_client_smoke",
            ("4447", "12458"): "runtime_fragment_zero_stay_close_to_hugo_activation_and_zero_count_credit_tested_pending_exact_follow_timing_and_client_smoke",
            ("4642", "12458"): "runtime_fragment_zero_hugo_final_talk_objective_activation_tested_pending_stay_close_and_client_smoke",
        }

        for (objective_id, object_id), evidence_status in event_producer_statuses.items():
            with self.subTest(producer="event", objective_id=objective_id):
                producer = audit.SCRIPT_OBJECTIVE_PRODUCER_OVERRIDES[
                    (
                        r"Source\NexusForever.Script.Instance\Expedition\FragmentZero\FragmentZeroEventScript.cs",
                        objective_id,
                        object_id,
                    )
                ]
                self.assertEqual("script_owner_matches_public_event", producer["owner_link_status"])
                self.assertEqual(evidence_status, producer["evidence_status"])

        entity_producer_statuses = {
            ("4424", "12274"): "runtime_fragment_zero_jo_corpse_target_group_credit_tested_pending_placement_visuals_and_client_smoke",
            ("4416", "12254"): "runtime_fragment_zero_skeech_horde_base_kill_death_credit_tested",
            ("4584", "12254"): "runtime_fragment_zero_skeech_horde_tier1_death_credit_tested",
            ("4585", "12254"): "runtime_fragment_zero_skeech_horde_tier2_death_credit_tested",
            ("4586", "12254"): "runtime_fragment_zero_skeech_horde_tier3_death_credit_tested",
            ("4423", "12269"): "runtime_fragment_zero_prototype_alpha_death_credit_tested",
            ("4449", "12270"): "runtime_fragment_zero_prototype_beta_death_credit_tested",
            ("4450", "12271"): "runtime_fragment_zero_prototype_delta_death_credit_tested",
            ("4417", "12255"): "runtime_fragment_zero_project_matron_death_credit_tested",
            ("4444", "12266"): "runtime_fragment_zero_life_overseer_death_credit_tested",
            ("4432", "12264"): "runtime_fragment_zero_syrus_corpse_target_group_credit_tested_pending_placement_visuals_and_client_smoke",
            ("4445", "12267"): "runtime_fragment_zero_locate_hugo_target_group_credit_tested_pending_projection_placement_and_client_smoke",
            ("4448", ""): "runtime_fragment_zero_hidden_recordings_parent_entity_credit_tested",
            ("4635", "12461"): "runtime_fragment_zero_automated_defense_panel_checklist_entity_credit_tested",
            ("4454", "12276"): "runtime_fragment_zero_ohmna_hidden_recording_checklist_entity_credit_tested",
            ("4455", "12279"): "runtime_fragment_zero_lucent_hidden_recording_checklist_entity_credit_tested",
        }

        for (objective_id, object_id), evidence_status in entity_producer_statuses.items():
            with self.subTest(producer="entity", objective_id=objective_id):
                producer = audit.SCRIPT_OBJECTIVE_PRODUCER_OVERRIDES[
                    (
                        r"Source\NexusForever.Script.Instance\Expedition\FragmentZero\Script\FragmentZeroObjectiveEntityScripts.cs",
                        objective_id,
                        object_id,
                    )
                ]
                self.assertEqual("script_owner_entity_filter_mapped_to_public_event", producer["owner_link_status"])
                self.assertEqual(evidence_status, producer["evidence_status"])

        skeech_producer = audit.SCRIPT_OBJECTIVE_PRODUCER_OVERRIDES[
            (
                r"Source\NexusForever.Script.Instance\Expedition\FragmentZero\Script\FragmentZeroObjectiveEntityScripts.cs",
                "4416",
                "12254",
            )
        ]
        self.assertIn("67516, 67518, 67519, and 67520", skeech_producer["blocker_summary"])
        self.assertNotIn("unresolved TargetGroup 12254 leaf review", skeech_producer["blocker_summary"])

        blocker = audit.SCRIPTED_INSTANCE_BLOCKERS[3180]
        self.assertIn("Search for Missing Crew objective 4397", blocker)
        self.assertIn("Jo/Syrus/Hugo target-group rows 4424/4432/4445", blocker)
        self.assertIn("Jo/Syrus Script search rows 4421/4420 now spawn one-unit WIP grid triggers", blocker)
        self.assertIn("gold-medal timer 4690 has test-backed activation and final credit", blocker)
        self.assertIn("final Hugo bridge direct-credits zero-count wait/stay-close objectives 4655/4447", blocker)
        self.assertIn("hidden recording objectives 4454/4455 now credit their checklist target groups and parent objective 4448", blocker)
        self.assertIn("Fragment Zero automated-defense panel objective 4635", blocker)
        self.assertIn("branch and airlock activation rows 4422/4426/4693/4427/4429/4418/4430/4443/4446", blocker)
        self.assertIn("Skeech horde kill rows 4416/4584/4585/4586", blocker)
        self.assertIn("all four TargetGroup 12254 Creature2 leaves 67516/67518/67519/67520", blocker)
        self.assertNotIn("unresolved TargetGroup 12254 leaf review", blocker)
        self.assertIn("Prototype/Matron/Life Overseer kill rows 4423/4449/4450/4417/4444", blocker)
        self.assertIn("residual Skeech ambush 4425 remains mapped-only", blocker)

    def test_evil_from_the_ether_residual_rows_are_concrete(self) -> None:
        cases = [
            (
                4919,
                "781;4919;8242;50343;2433",
                "runtime_evil_ether_upper_deck_teleport_activation_trigger_credit_tested_pending_exact_destination_replay_and_client_smoke",
            ),
            (
                4921,
                "781;4921;14047;71132;71133;71134;71135;71221;50348",
                "runtime_evil_ether_bridge_access_portal_activation_death_credit_tested_pending_spawn_cadence_and_client_smoke",
            ),
            (
                4923,
                "781;4923;14047;71132;71133;71134;71135;71221;50535",
                "runtime_evil_ether_bridge_portals_activation_death_credit_tested_pending_three_portal_spawn_cadence_and_client_smoke",
            ),
            (
                4927,
                "781;4927;8242;8243;50343",
                "runtime_evil_ether_escape_teleporter_activation_trigger_credit_tested_pending_exact_destination_replay_and_client_smoke",
            ),
            (
                4942,
                "781;4942;8342",
                "mapped_evil_ether_exploding_portal_avoidance_blocked_missing_hit_failure_producer_client_smoke",
            ),
            (
                4947,
                "781;4947;14057;71234;50731",
                "runtime_evil_ether_crew_logs_activation_credit_and_communicators_tested_pending_placement_replay_and_client_smoke",
            ),
            (
                4974,
                "781;4974;71266;7024519;50622",
                "runtime_evil_ether_crew_quarters_door_entry_credit_tested_pending_door_route_and_client_smoke",
            ),
            (
                5013,
                "781;5013;14131;71821;50535",
                "runtime_evil_ether_drive_schematics_activation_credit_tested_pending_drop_visual_and_client_smoke",
            ),
        ]

        for objective_id, owner_ids, evidence_status in cases:
            with self.subTest(objective_id=objective_id):
                dependency = audit.INSTANCE_OBJECTIVE_DEPENDENCY_OVERRIDES[(3404, objective_id)]
                self.assertIn("EvilFromTheEther", dependency["script_path"])
                self.assertEqual(owner_ids, dependency["script_owner_ids"])
                self.assertEqual(evidence_status, dependency["evidence_status"])

        event_producer_statuses = {
            ("4895", "12996"): "runtime_evil_ether_captain_weir_initial_talk_activation_tested",
            ("4908", "8242"): "runtime_evil_ether_airlock_gather_activation_and_wip_trigger_spawn_tested_pending_exact_route_and_client_smoke",
            ("4937", "14064"): "runtime_evil_ether_open_medbay_activation_tested",
            ("4957", "14069"): "runtime_evil_ether_repair_door_activation_tested",
            ("4938", "14076"): "runtime_evil_ether_medbay_generator_activation_tested",
            ("4940", "8283"): "runtime_evil_ether_primary_power_plant_activation_and_seed_tested_pending_trigger_replay_and_client_smoke",
            ("4939", ""): "runtime_evil_ether_security_chief_activation_tested",
            ("4941", "14077"): "runtime_evil_ether_restart_generators_activation_tested",
            ("4961", "14083"): "runtime_evil_ether_generator_alpha_activation_tested",
            ("4962", "14084"): "runtime_evil_ether_generator_beta_activation_tested",
            ("4975", "14105"): "runtime_evil_ether_feasting_organism_activation_tested",
            ("4976", "8307"): "runtime_evil_ether_find_teleporter_activation_and_turnstile_spawn_tested_pending_exact_route_and_client_smoke",
            ("4977", "14106"): "runtime_evil_ether_teleporter_organism_activation_tested",
            ("4979", "8312"): "runtime_evil_ether_gather_teleporter_activation_and_trigger_spawn_tested_pending_exact_route_and_client_smoke",
            ("4920", "8260"): "runtime_evil_ether_bridge_access_hall_activation_and_trigger_spawn_tested_pending_exact_route_and_client_smoke",
            ("4922", "8261"): "runtime_evil_ether_shades_bridge_activation_and_trigger_spawn_tested_pending_exact_route_and_client_smoke",
            ("4924", "14050"): "runtime_evil_ether_self_destruct_activation_tested_pending_controls_visual_and_client_smoke",
            ("4925", "14051"): "runtime_evil_ether_katja_zarkhov_activation_tested",
            ("4948", "12996"): "runtime_evil_ether_captain_weir_final_talk_activation_tested",
            ("4919", "8242"): "runtime_evil_ether_upper_deck_teleport_phase_activation_and_trigger_spawn_tested_pending_exact_destination_and_client_smoke",
            ("4921", ""): "runtime_evil_ether_bridge_access_portal_objective_activation_tested_pending_spawn_cadence_and_client_smoke",
            ("4923", ""): "runtime_evil_ether_bridge_portals_objective_activation_tested_pending_three_portal_spawn_cadence_and_client_smoke",
            ("4927", "8242"): "runtime_evil_ether_escape_teleporter_phase_activation_and_trigger_spawn_tested_pending_exact_destination_and_client_smoke",
            ("4947", "14057"): "runtime_evil_ether_crew_logs_objective_activation_tested",
            ("4974", ""): "runtime_evil_ether_crew_quarters_objective_activation_tested_pending_door_route_and_client_smoke",
            ("5013", "14131"): "runtime_evil_ether_drive_schematics_objective_activation_tested_pending_drop_visual_and_client_smoke",
        }

        for (objective_id, object_id), evidence_status in event_producer_statuses.items():
            with self.subTest(producer="event", objective_id=objective_id):
                producer = audit.SCRIPT_OBJECTIVE_PRODUCER_OVERRIDES[
                    (
                        r"Source\NexusForever.Script.Instance\Expedition\EvilFromTheEther\EvilFromTheEtherEventScript.cs",
                        objective_id,
                        object_id,
                    )
                ]
                self.assertEqual("script_owner_matches_public_event", producer["owner_link_status"])
                self.assertEqual(evidence_status, producer["evidence_status"])

        emitted_producer_cases = [
            (
                r"Source\NexusForever.Script.Instance\Expedition\EvilFromTheEther\Script\CaptainWeirEntityScript.cs",
                "4895;4948",
                "12996",
                "script_owner_entity_filter_mapped_to_public_event",
                "runtime_evil_ether_captain_weir_talk_target_group_credit_tested",
            ),
            (
                r"Source\NexusForever.Script.Instance\Expedition\EvilFromTheEther\Script\EthericDriveControlsEntityScript.cs",
                "4924",
                "14050",
                "script_owner_entity_filter_mapped_to_public_event",
                "runtime_evil_ether_self_destruct_controls_target_group_credit_tested_pending_controls_visual_and_client_smoke",
            ),
            (
                r"Source\NexusForever.Script.Instance\Expedition\EvilFromTheEther\Script\MedbayObjectiveEntityScripts.cs",
                "4937",
                "14064",
                "script_owner_entity_filter_mapped_to_public_event",
                "runtime_evil_ether_open_medbay_door_control_credit_tested",
            ),
            (
                r"Source\NexusForever.Script.Instance\Expedition\EvilFromTheEther\Script\MedbayObjectiveEntityScripts.cs",
                "4956",
                "14066",
                "script_owner_entity_filter_mapped_to_public_event",
                "runtime_evil_ether_spare_parts_checklist_credit_tested",
            ),
            (
                r"Source\NexusForever.Script.Instance\Expedition\EvilFromTheEther\Script\MedbayObjectiveEntityScripts.cs",
                "4957",
                "14069",
                "script_owner_entity_filter_mapped_to_public_event",
                "runtime_evil_ether_repaired_medbay_door_control_credit_tested",
            ),
            (
                r"Source\NexusForever.Script.Instance\Expedition\EvilFromTheEther\Script\MedbayObjectiveEntityScripts.cs",
                "4938",
                "14076",
                "script_owner_entity_filter_mapped_to_public_event",
                "runtime_evil_ether_medbay_generator_controls_credit_tested",
            ),
            (
                r"Source\NexusForever.Script.Instance\Expedition\EvilFromTheEther\Script\MedbayObjectiveEntityScripts.cs",
                "4978",
                "14107",
                "script_owner_entity_filter_mapped_to_public_event",
                "runtime_evil_ether_teleporter_controls_checklist_credit_tested_pending_visual_and_client_smoke",
            ),
            (
                r"Source\NexusForever.Script.Instance\Expedition\EvilFromTheEther\Script\KatjaZarkhovEntityScript.cs",
                "4925",
                "14051",
                "script_owner_entity_filter_mapped_to_public_event",
                "runtime_evil_ether_katja_zarkhov_death_credit_tested",
            ),
            (
                r"Source\NexusForever.Script.Instance\Expedition\EvilFromTheEther\Script\PrimaryPowerPlantDoorEntityScript.cs",
                "4940",
                "8283",
                "script_owner_entity_filter_mapped_to_public_event",
                "runtime_evil_ether_primary_power_plant_trigger_credit_tested_pending_trigger_replay_and_client_smoke",
            ),
            (
                r"Source\NexusForever.Script.Instance\Expedition\EvilFromTheEther\Script\SecurityChiefKondovichEntityScript.cs",
                "4939",
                "",
                "script_owner_entity_filter_mapped_to_public_event",
                "runtime_evil_ether_security_chief_death_credit_tested",
            ),
            (
                r"Source\NexusForever.Script.Instance\Expedition\EvilFromTheEther\Script\UpperDeckTeleportGridTriggerEntityScript.cs",
                "4919",
                "8242",
                "script_owner_entity_or_trigger",
                "runtime_evil_ether_upper_deck_teleport_trigger_credit_tested_pending_exact_destination_and_client_smoke",
            ),
            (
                r"Source\NexusForever.Script.Instance\Expedition\EvilFromTheEther\Script\CaptainWeirTeleportGridTriggerEntityScript.cs",
                "4927",
                "8242",
                "script_owner_entity_or_trigger",
                "runtime_evil_ether_escape_teleporter_trigger_credit_tested_pending_exact_destination_and_client_smoke",
            ),
            (
                r"Source\NexusForever.Script.Instance\Expedition\EvilFromTheEther\Script\CrewLogEntityScript.cs",
                "4947",
                "14057",
                "script_owner_entity_filter_mapped_to_public_event",
                "runtime_evil_ether_crew_log_credit_and_communicator_tested",
            ),
            (
                r"Source\NexusForever.Script.Instance\Expedition\EvilFromTheEther\Script\CrewQuatersDoorEntityScript.cs",
                "4974",
                "",
                "script_owner_entity_filter_mapped_to_public_event",
                "runtime_evil_ether_crew_quarters_door_entry_credit_tested_pending_door_route_and_client_smoke",
            ),
            (
                r"Source\NexusForever.Script.Instance\Expedition\EvilFromTheEther\Script\EthericDriveSchematicsEntityScript.cs",
                "5013",
                "14131",
                "script_owner_entity_filter_mapped_to_public_event",
                "runtime_evil_ether_drive_schematics_pickup_credit_tested_pending_drop_visual_and_client_smoke",
            ),
        ]

        for source_path, objective_id, object_id, owner_status, evidence_status in emitted_producer_cases:
            with self.subTest(producer=source_path, objective_id=objective_id):
                producer = audit.SCRIPT_OBJECTIVE_PRODUCER_OVERRIDES[
                    (
                        source_path,
                        objective_id,
                        object_id,
                    )
                ]
                self.assertEqual(owner_status, producer["owner_link_status"])
                self.assertEqual(evidence_status, producer["evidence_status"])

        blocker = audit.SCRIPTED_INSTANCE_BLOCKERS[3404]
        self.assertIn("upper-deck and escape teleporter rows 4919/4927", blocker)
        self.assertIn("tethered portal rows 4921/4923", blocker)
        self.assertIn("exploding etheric portal avoidance row 4942 remains mapped-only", blocker)

    def test_infestation_jabbithole_backed_objective_placements_are_tracked(self) -> None:
        vent_dependency = audit.INSTANCE_OBJECTIVE_DEPENDENCY_OVERRIDES[(1232, 228)]
        self.assertIn("InfestationEventScript.cs", vent_dependency["script_path"])
        self.assertIn("ShipVentEntityScript.cs", vent_dependency["script_path"])
        self.assertEqual(
            "95;228;22493;2324;1361;5738;5739;5740;1100123001;1100123002;1100123003",
            vent_dependency["script_owner_ids"],
        )
        self.assertEqual(
            "runtime_infestation_ship_vent_jabbithole_placements_and_checklist_credit_tested_pending_rotation_visual_state_and_client_smoke",
            vent_dependency["evidence_status"],
        )
        self.assertIn("jabbithole.coordinates", vent_dependency["evidence_sources"])

        medical_dependency = audit.INSTANCE_OBJECTIVE_DEPENDENCY_OVERRIDES[(1232, 230)]
        self.assertIn("InfestationEventScript.cs", medical_dependency["script_path"])
        self.assertIn("MedicalSuppliesEntityScript.cs", medical_dependency["script_path"])
        self.assertEqual(
            "95;230;22489;2323;1349;5707;1100123016;1100123017",
            medical_dependency["script_owner_ids"],
        )
        self.assertEqual(
            "runtime_infestation_medical_supplies_jabbithole_placements_and_activation_credit_tested_pending_phase_cleanup_and_client_smoke",
            medical_dependency["evidence_status"],
        )
        self.assertIn("jabbithole.coordinates", medical_dependency["evidence_sources"])

        shiphand_dependency = audit.INSTANCE_OBJECTIVE_DEPENDENCY_OVERRIDES[(1232, 231)]
        self.assertIn("InfestationEventScript.cs", shiphand_dependency["script_path"])
        self.assertIn("ContaminatedShiphandEntityScript.cs", shiphand_dependency["script_path"])
        self.assertEqual(
            "95;231;23837;2333;1348;28034;18403;18404;18405;6130740;6129102;6129103;6129104;6130630;1100123018;1100123019;1100123020;1100123021",
            shiphand_dependency["script_owner_ids"],
        )
        self.assertEqual(
            "runtime_infestation_contaminated_shiphand_jabbithole_placements_and_heal_credit_tested_pending_medicine_cast_visual_state_and_client_smoke",
            shiphand_dependency["evidence_status"],
        )
        self.assertIn("creature_public_event_map.csv", shiphand_dependency["evidence_sources"])
        self.assertIn("jabbithole.coordinates", shiphand_dependency["evidence_sources"])

        lashing_fiend_dependency = audit.INSTANCE_OBJECTIVE_DEPENDENCY_OVERRIDES[(1232, 4698)]
        self.assertIn("InfestationEventScript.cs", lashing_fiend_dependency["script_path"])
        self.assertIn("ParasiteEntityScripts.cs", lashing_fiend_dependency["script_path"])
        self.assertEqual(
            "95;4698;12905;69871;28012;8251634;1100123022",
            lashing_fiend_dependency["script_owner_ids"],
        )
        self.assertEqual(
            "runtime_infestation_lashing_fiend_jabbithole_placement_and_death_credit_tested_pending_wave_choreography_combat_and_client_smoke",
            lashing_fiend_dependency["evidence_status"],
        )
        self.assertIn("world_entity_candidate.csv", lashing_fiend_dependency["evidence_sources"])
        self.assertIn("jabbithole.coordinates", lashing_fiend_dependency["evidence_sources"])

        hull_dependency = audit.INSTANCE_OBJECTIVE_DEPENDENCY_OVERRIDES[(1232, 1007)]
        self.assertIn("InfestationEventScript.cs", hull_dependency["script_path"])
        self.assertIn("HullBreachEntityScript.cs", hull_dependency["script_path"])
        self.assertEqual(
            "95;1007;36844;4516;1310;5593;5594;5595;5596;5597;5598;1100123010;1100123011;1100123012;1100123013;1100123014;1100123015",
            hull_dependency["script_owner_ids"],
        )
        self.assertEqual(
            "runtime_infestation_hull_breach_jabbithole_placements_and_checklist_credit_tested_pending_extra_coordinate_resolution_visual_state_and_client_smoke",
            hull_dependency["evidence_status"],
        )
        self.assertIn("jabbithole.public_event_creatures", hull_dependency["evidence_sources"])

        cargo_dependency = audit.INSTANCE_OBJECTIVE_DEPENDENCY_OVERRIDES[(1232, 4699)]
        self.assertIn("InfestationEventScript.cs", cargo_dependency["script_path"])
        self.assertIn("CargoContainerEntityScript.cs", cargo_dependency["script_path"])
        self.assertEqual(
            "95;4699;69877;12593;28158;6130161;6130162;6130163;6130164;6131402;6131403;1100123004;1100123005;1100123006;1100123007;1100123008;1100123009",
            cargo_dependency["script_owner_ids"],
        )
        self.assertEqual(
            "runtime_infestation_cargo_container_jabbithole_placements_and_checklist_credit_tested_pending_rotation_visual_state_and_client_smoke",
            cargo_dependency["evidence_status"],
        )
        self.assertIn("jabbithole.public_event_creatures", cargo_dependency["evidence_sources"])

        self.assertIn(
            "Ship Vents 228, Medical Supplies 230, Contaminated Shiphand healing 231, Hull Breaches 1007, Cargo Containers 4699, and the Lashing Fiend medbay attack objective 4698",
            audit.SCRIPTED_INSTANCE_BLOCKERS[1232],
        )

        event_producer = audit.SCRIPT_OBJECTIVE_PRODUCER_OVERRIDES[
            (
                r"Source\NexusForever.Script.Instance\Expedition\Infestation\InfestationEventScript.cs",
                "4698",
                "12905",
            )
        ]
        self.assertEqual(
            "runtime_infestation_lashing_fiend_jabbithole_spawn_and_objective_activation_tested_pending_wave_choreography_combat_and_client_smoke",
            event_producer["evidence_status"],
        )
        self.assertIn("coordinate 8251634", event_producer["blocker_summary"])

        credit_producer = audit.SCRIPT_OBJECTIVE_PRODUCER_OVERRIDES[
            (
                r"Source\NexusForever.Script.Instance\Expedition\Infestation\Script\ParasiteEntityScripts.cs",
                "4698",
                "12905",
            )
        ]
        self.assertEqual("script_owner_entity_filter_mapped_to_public_event", credit_producer["owner_link_status"])
        self.assertIn("Creature2 69871", credit_producer["blocker_summary"])

    def test_infestation_remaining_script_trigger_timer_rows_are_concrete_blockers(self) -> None:
        expected_statuses = {
            1006: "mapped_infestation_shiphand_hull_breach_rescue_blocked_missing_rescue_producer_release_semantics_client_smoke",
            1438: "mapped_infestation_medical_bay_trigger_blocked_missing_object_4899_trigger_placement_client_smoke",
            4703: "runtime_infestation_gold_medal_timer_activation_and_final_credit_tested_pending_timer_reward_and_client_smoke",
        }

        for objective_id, expected_status in expected_statuses.items():
            with self.subTest(objective_id=objective_id):
                dependency = audit.INSTANCE_OBJECTIVE_DEPENDENCY_OVERRIDES[(1232, objective_id)]
                self.assertEqual(expected_status, dependency["evidence_status"])
                self.assertNotEqual(
                    "mapped_public_event_objective_blocked_missing_runtime_producer_evidence",
                    dependency["evidence_status"],
                )
                self.assertIn("public_event_objective_client_map.csv", dependency["evidence_sources"])
                self.assertIn("smoke", dependency["blocker_summary"])

        rescue = audit.INSTANCE_OBJECTIVE_DEPENDENCY_OVERRIDES[(1232, 1006)]
        self.assertEqual("95;1006;2362", rescue["script_owner_ids"])
        self.assertIn("separate Seal Hull Breaches objective 1007", rescue["blocker_summary"])

        medical_bay = audit.INSTANCE_OBJECTIVE_DEPENDENCY_OVERRIDES[(1232, 1438)]
        self.assertEqual("95;1438;4899;2359", medical_bay["script_owner_ids"])
        self.assertIn("object 4899", medical_bay["blocker_summary"])

        gold_timer = audit.INSTANCE_OBJECTIVE_DEPENDENCY_OVERRIDES[(1232, 4703)]
        self.assertEqual("95;4703;1080000", gold_timer["script_owner_ids"])
        self.assertIn("failureTimeMs 1080000", gold_timer["blocker_summary"])
        self.assertIn("direct-credits it only after the final parasite-kill objective succeeds", gold_timer["blocker_summary"])

        self.assertIn("gold-medal timer 4703 has test-backed activation and final credit", audit.SCRIPTED_INSTANCE_BLOCKERS[1232])
        self.assertIn("Residual script/trigger rows 1006 and 1438 are mapped-only", audit.SCRIPTED_INSTANCE_BLOCKERS[1232])

    def test_redmoon40_laveka_and_hidden_turnstile_rows_are_concrete(self) -> None:
        laveka_dependency = audit.INSTANCE_OBJECTIVE_DEPENDENCY_OVERRIDES[(3102, 525)]
        self.assertIn("RedMoonTerror40ManEventScript.cs", laveka_dependency["script_path"])
        self.assertIn("RedMoonTerror40ManLavekaEntityScript.cs", laveka_dependency["script_path"])
        self.assertEqual("650;525;65997;1100300076;5996;38426;1351", laveka_dependency["script_owner_ids"])
        self.assertEqual(
            "runtime_redmoon40_laveka_activation_spawn_death_credit_finish_tested_pending_route_status_combat_and_client_smoke",
            laveka_dependency["evidence_status"],
        )
        self.assertIn("WIP entity 1100300076", laveka_dependency["blocker_summary"])

        event_producer = audit.SCRIPT_OBJECTIVE_PRODUCER_OVERRIDES[
            (
                r"Source\NexusForever.Script.Instance\Raid\RedMoonTerror\FortyMan\RedMoonTerror40ManEventScript.cs",
                "525",
                "",
            )
        ]
        self.assertEqual("script_owner_matches_public_event", event_producer["owner_link_status"])
        self.assertEqual(
            "runtime_redmoon40_laveka_objective_activation_and_spawn_tested_pending_route_status_combat_and_client_smoke",
            event_producer["evidence_status"],
        )

        death_credit_producer = audit.SCRIPT_OBJECTIVE_PRODUCER_OVERRIDES[
            (
                r"Source\NexusForever.Script.Instance\Raid\RedMoonTerror\FortyMan\Script\RedMoonTerror40ManLavekaEntityScript.cs",
                "525",
                "",
            )
        ]
        self.assertEqual("script_owner_entity_filter_mapped_to_public_event", death_credit_producer["owner_link_status"])
        self.assertIn("objective 525 on death", death_credit_producer["blocker_summary"])

        hidden_statuses = {
            4622: "mapped_redmoon40_medbay_turnstile_blocked_missing_object_8007_worldlocation_trigger_client_smoke",
            4624: "mapped_redmoon40_morgue_turnstile_blocked_missing_object_8008_worldlocation_trigger_client_smoke",
        }
        for objective_id, expected_status in hidden_statuses.items():
            with self.subTest(objective_id=objective_id):
                dependency = audit.INSTANCE_OBJECTIVE_DEPENDENCY_OVERRIDES[(3102, objective_id)]
                self.assertEqual(expected_status, dependency["evidence_status"])
                self.assertNotEqual(
                    "mapped_public_event_objective_blocked_missing_runtime_producer_evidence",
                    dependency["evidence_status"],
                )
                self.assertIn("public_event_objective_client_map.csv", dependency["evidence_sources"])
                self.assertIn("WorldLocation2", dependency["blocker_summary"])

        self.assertIn("Residual hidden turnstile rows 4622 and 4624", audit.SCRIPTED_INSTANCE_BLOCKERS[3102])

    def test_space_madness_public_event_creature_rows_use_runtime_exemptions(self) -> None:
        expected_status_by_keys = {
            "runtime_space_madness_captain_tero_public_event_creature_spawn_and_talk_credit_tested_pending_dialog_rewards_and_client_smoke": {
                (390, 28, 45900),
                (390, 2117, 45900),
            },
            "runtime_space_madness_major_lee_public_event_creature_spawn_and_talk_credit_tested_pending_dialog_rewards_and_client_smoke": {
                (390, 23, 45812),
                (390, 2120, 45812),
            },
            "runtime_space_madness_observation_deck_computer_public_event_creature_spawn_and_credit_tested_pending_visual_rewards_and_client_smoke": {
                (390, 27, 45972),
            },
            "runtime_space_madness_panicked_worker_nightmare_public_event_creature_spawned_and_death_credit_tested_pending_worker_state_rewards_and_client_smoke": {
                (390, 18, 46124),
                (390, 20, 46128),
                (390, 24, 46127),
                (390, 25, 46123),
                (390, 26, 46133),
                (390, 29, 46130),
                (390, 30, 46126),
                (390, 31, 46132),
                (390, 144, 58783),
                (390, 155, 46135),
                (390, 157, 46721),
                (390, 158, 58798),
            },
            "runtime_space_madness_datapad_public_event_creature_spawned_and_checklist_credit_tested_pending_visual_rewards_and_client_smoke": {
                (390, 19, 45993),
            },
            "runtime_space_madness_hazmat_control_panel_public_event_creature_spawn_and_credit_tested_pending_visual_rewards_and_client_smoke": {
                (390, 21, 46092),
            },
            "runtime_space_madness_hazmat_suit_public_event_creature_spawn_and_direct_credit_tested_pending_visual_rewards_and_client_smoke": {
                (390, 145, 45981),
            },
            "runtime_space_madness_engineering_computer_public_event_creature_spawn_and_credit_tested_pending_visual_rewards_and_client_smoke": {
                (390, 146, 45973),
            },
            "runtime_space_madness_air_scrubber_controls_public_event_creature_spawned_and_credit_tested_pending_visual_rewards_and_client_smoke": {
                (390, 147, 46437),
            },
            "runtime_space_madness_escaped_experiment_public_event_creature_spawned_and_collection_credit_tested_pending_route_rewards_and_client_smoke": {
                (390, 2017, 69901),
                (390, 2020, 69900),
                (390, 2033, 69899),
            },
            "runtime_space_madness_livestock_public_event_creature_spawn_and_death_credit_tested_pending_combat_rewards_and_client_smoke": {
                (390, 148, 46714),
                (390, 151, 46483),
            },
        }
        for expected_status, keys in expected_status_by_keys.items():
            for key in keys:
                with self.subTest(key=key):
                    override = audit.PUBLIC_EVENT_CREATURE_EVIDENCE_OVERRIDES[key]
                    self.assertEqual(expected_status, override["evidence_status"])
                    self.assertIn("Space Madness", override["blocker_summary"])
                    self.assertIn("no longer double-counted", override["blocker_summary"])
                    self.assertIn(key, audit.PUBLIC_EVENT_CREATURE_BLOCKER_EXEMPTIONS)

        still_blocked_keys = {
            (390, 22, 46131),
            (390, 153, 45903),
            (390, 2004, 69205),
        }
        for key in still_blocked_keys:
            with self.subTest(still_blocked_key=key):
                self.assertNotIn(key, audit.PUBLIC_EVENT_CREATURE_EVIDENCE_OVERRIDES)
                self.assertNotIn(key, audit.PUBLIC_EVENT_CREATURE_BLOCKER_EXEMPTIONS)

    def test_space_madness_hazmat_and_remaining_script_rows_are_concrete(self) -> None:
        hazmat_dependency = audit.INSTANCE_OBJECTIVE_DEPENDENCY_OVERRIDES[(2149, 1656)]
        self.assertIn("SpaceMadnessEventScript.cs", hazmat_dependency["script_path"])
        self.assertIn("SpaceMadnessObjectiveEntityScripts.cs", hazmat_dependency["script_path"])
        self.assertEqual("390;1656;5628;45981;1100300012;37708;6362", hazmat_dependency["script_owner_ids"])
        self.assertEqual(
            "runtime_space_madness_hazmat_suit_spawn_and_direct_credit_tested_pending_visual_state_rewards_and_client_smoke",
            hazmat_dependency["evidence_status"],
        )
        self.assertIn("direct-credits objective 1656 once", hazmat_dependency["blocker_summary"])

        event_producer = audit.SCRIPT_OBJECTIVE_PRODUCER_OVERRIDES[
            (
                r"Source\NexusForever.Script.Instance\Expedition\SpaceMadness\SpaceMadnessEventScript.cs",
                "1656",
                "5628",
            )
        ]
        self.assertEqual("script_owner_matches_public_event", event_producer["owner_link_status"])
        self.assertEqual(
            "runtime_space_madness_hazmat_suit_phase_activation_and_spawn_tested",
            event_producer["evidence_status"],
        )

        credit_producer = audit.SCRIPT_OBJECTIVE_PRODUCER_OVERRIDES[
            (
                r"Source\NexusForever.Script.Instance\Expedition\SpaceMadness\Script\SpaceMadnessObjectiveEntityScripts.cs",
                "1656",
                "5628",
            )
        ]
        self.assertEqual("script_owner_entity_filter_mapped_to_public_event", credit_producer["owner_link_status"])
        self.assertEqual(
            "runtime_space_madness_hazmat_suit_direct_credit_tested",
            credit_producer["evidence_status"],
        )

        expected_statuses = {
            1662: "mapped_space_madness_survive_nightmares_wave_counter_blocked_missing_hallucination_wave_credit_producer_client_smoke",
            2115: "mapped_space_madness_air_scrubbing_hallucination_wave_counter_blocked_missing_wave_credit_timer_producer_client_smoke",
            4705: "runtime_space_madness_gold_medal_timer_activation_and_final_credit_tested_pending_timer_reward_and_client_smoke",
            4708: "mapped_space_madness_volatile_rowsdower_explosion_avoidance_blocked_missing_explosion_hit_failure_producer_client_smoke",
        }
        for objective_id, expected_status in expected_statuses.items():
            with self.subTest(objective_id=objective_id):
                dependency = audit.INSTANCE_OBJECTIVE_DEPENDENCY_OVERRIDES[(2149, objective_id)]
                self.assertEqual(expected_status, dependency["evidence_status"])
                self.assertNotEqual(
                    "mapped_public_event_objective_blocked_missing_runtime_producer_evidence",
                    dependency["evidence_status"],
                )
                self.assertIn("public_event_objective_client_map.csv", dependency["evidence_sources"])
                self.assertIn("smoke", dependency["blocker_summary"])

        gold_timer = audit.INSTANCE_OBJECTIVE_DEPENDENCY_OVERRIDES[(2149, 4705)]
        self.assertEqual("390;4705;2100000", gold_timer["script_owner_ids"])
        self.assertIn("failureTimeMs 2100000", gold_timer["blocker_summary"])
        self.assertIn("direct-credits it only after SendTheAllClearSignal succeeds", gold_timer["blocker_summary"])

        wave_producer = audit.SCRIPT_OBJECTIVE_PRODUCER_OVERRIDES[
            (
                r"Source\NexusForever.Script.Instance\Expedition\SpaceMadness\SpaceMadnessEventScript.cs",
                "1662",
                "6447",
            )
        ]
        self.assertIn("activation_only_blocked_missing_wave_credit", wave_producer["evidence_status"])

        rowsdower_producer = audit.SCRIPT_OBJECTIVE_PRODUCER_OVERRIDES[
            (
                r"Source\NexusForever.Script.Instance\Expedition\SpaceMadness\SpaceMadnessEventScript.cs",
                "4708",
                "",
            )
        ]
        self.assertIn("missing_hit_failure_producer", rowsdower_producer["evidence_status"])
        self.assertIn("gold-medal timer 4705 objective producers are test-backed", audit.SCRIPTED_INSTANCE_BLOCKERS[2149])
        self.assertIn("Residual script/challenge rows 1662, 2115, and 4708 are mapped-only", audit.SCRIPTED_INSTANCE_BLOCKERS[2149])

    def test_space_madness_script_objective_producer_overrides_clear_tested_rows(self) -> None:
        space_event = r"Source\NexusForever.Script.Instance\Expedition\SpaceMadness\SpaceMadnessEventScript.cs"
        space_objectives = (
            r"Source\NexusForever.Script.Instance\Expedition\SpaceMadness\Script\SpaceMadnessObjectiveEntityScripts.cs"
        )
        captain_tero = r"Source\NexusForever.Script.Instance\Expedition\SpaceMadness\Script\CaptainTeroEntityScript.cs"
        major_lee = r"Source\NexusForever.Script.Instance\Expedition\SpaceMadness\Script\MajorLeeBarmyEntityScript.cs"
        observation_computer = (
            r"Source\NexusForever.Script.Instance\Expedition\SpaceMadness\Script\ObservationDeckComputerEntityScript.cs"
        )

        strings = {
            390: "Space Madness!",
            1579: "Talk To Captain Tero",
            1588: "Talk To Major Lee Barmy",
            1602: "Enter The Airlock",
            1589: "Access The Observation Deck Computer",
            1590: "Save Panicked Workers",
            1596: "Access Crew Datapads",
            4709: "Collect An Experimental Slank",
            4710: "Collect An Experimental Rockmite",
            4711: "Collect A Party Downgrazer",
            4712: "Collect Escaped Creatures",
            1591: "Enter The Research Laboratory",
            4736: "Open Hazmat Storage Closet",
            1662: "Survive Your Nightmares",
            1656: "Equip A Hazmat Suit",
            1593: "Kill Hallucinating Livestock",
            1594: "Give Air Helms To Workers",
            4708: "Avoid Exploding Rowsdowers",
            1595: "Activate Air Scrubber Controls",
            2115: "Survive The Air Scrubbing Process",
            1603: "Send The All Clear Signal",
            4705: "Gold Medal Timer",
            1592: "Open Hazmat Storage Closet hidden",
        }
        public_events = [
            {
                "ID": "390",
                "worldId": "2149",
                "worldZoneId": "0",
                "localizedTextIdName": "390",
                "publicEventTypeEnum": "0",
                "publicEventIdParent": "0",
            }
        ]

        def objective(
            objective_id: int,
            objective_type: int,
            count: int,
            object_id: int,
            display_order: int,
        ) -> dict[str, str]:
            return {
                "ID": str(objective_id),
                "publicEventId": "390",
                "publicEventObjectiveTypeEnum": str(objective_type),
                "count": str(count),
                "objectId": str(object_id),
                "localizedTextId": str(objective_id),
                "localizedTextIdShort": "0",
                "worldLocation2Id": "0",
                "targetGroupIdRewardPane": "0",
                "displayOrder": str(display_order),
            }

        public_event_objectives = [
            objective(1579, 17, 1, 6342, 1),
            objective(1588, 17, 1, 6358, 2),
            objective(1602, 6, 1, 5512, 3),
            objective(1589, 3, 1, 6356, 4),
            objective(1590, 16, 1, 0, 5),
            objective(1596, 4, 6, 6384, 6),
            objective(4709, 3, 1, 12595, 7),
            objective(4710, 3, 1, 12596, 8),
            objective(4711, 3, 1, 12597, 9),
            objective(4712, 5, 3, 0, 10),
            objective(1591, 6, 1, 5531, 11),
            objective(4736, 3, 1, 12651, 12),
            objective(1662, 5, 4, 6447, 13),
            objective(1656, 5, 1, 5628, 14),
            objective(1593, 8, 1, 0, 15),
            objective(1594, 17, 6, 6370, 16),
            objective(4708, 9, 0, 0, 17),
            objective(1595, 3, 1, 6371, 18),
            objective(2115, 5, 6, 0, 19),
            objective(1603, 3, 1, 6372, 20),
            objective(4705, 25, 0, 0, 21),
            objective(1592, 3, 1, 12651, 22),
        ]

        rows = audit.build_script_objective_producer_evidence_rows(
            strings,
            public_events,
            public_event_objectives,
            [audit.DEFAULT_SCRIPT_INSTANCE / "Expedition" / "SpaceMadness"],
        )
        rows_by_key: dict[tuple[object, object, object], list[dict[str, object]]] = {}
        for row in rows:
            key = (
                row["source_path"],
                str(row["matched_public_event_objective_ids"]),
                str(row["object_id"]),
            )
            rows_by_key.setdefault(key, []).append(row)

        expected_statuses = {
            (space_event, "1579", "6342"): "runtime_space_madness_captain_tero_talk_activation_tested",
            (space_event, "1588", "6358"): "runtime_space_madness_major_lee_barmy_talk_activation_tested",
            (space_event, "1602", "5512"): "runtime_space_madness_airlock_gather_activation_tested",
            (space_event, "1589", "6356"): "runtime_space_madness_observation_deck_computer_activation_tested",
            (space_event, "1590", ""): "runtime_space_madness_panicked_worker_save_activation_tested",
            (space_event, "1596", "6384"): "runtime_space_madness_crew_datapad_checklist_activation_tested",
            (space_event, "4709", "12595"): "runtime_space_madness_experimental_slank_activation_tested",
            (space_event, "4710", "12596"): "runtime_space_madness_experimental_rockmite_activation_tested",
            (space_event, "4711", "12597"): "runtime_space_madness_party_downgrazer_activation_tested",
            (space_event, "4712", ""): "runtime_space_madness_escaped_experiments_aggregate_activation_tested",
            (space_event, "1591", "5531"): "runtime_space_madness_research_laboratory_gather_activation_tested",
            (space_event, "4736", "12651"): "runtime_space_madness_hazmat_storage_closet_activation_tested",
            (space_event, "1656", "5628"): "runtime_space_madness_hazmat_suit_phase_activation_and_spawn_tested",
            (space_event, "1593", ""): "runtime_space_madness_livestock_phase_activation_tested",
            (space_event, "1594", "6370"): "runtime_space_madness_air_helm_worker_activation_tested",
            (space_event, "1595", "6371"): "runtime_space_madness_air_scrubber_controls_activation_tested",
            (space_event, "1603", "6372"): "runtime_space_madness_engineering_computer_all_clear_activation_tested",
            (space_event, "4705", ""): "runtime_space_madness_gold_medal_timer_activation_and_final_credit_tested",
            (space_objectives, "1590", ""): "runtime_space_madness_panicked_worker_save_death_credit_tested",
            (space_objectives, "4712", ""): "runtime_space_madness_escaped_experiments_aggregate_credit_tested",
            (space_objectives, "1593", ""): "runtime_space_madness_livestock_death_credit_tested",
            (space_objectives, "1596", "6384"): "runtime_space_madness_crew_datapad_checklist_credit_tested",
            (space_objectives, "1592;4736", "12651"): "runtime_space_madness_hazmat_control_panel_credit_tested",
            (space_objectives, "1595", "6371"): "runtime_space_madness_air_scrubber_controls_credit_tested",
            (space_objectives, "1603", "6372"): "runtime_space_madness_engineering_computer_all_clear_credit_tested",
            (space_objectives, "4709", "12595"): "runtime_space_madness_experimental_slank_credit_tested",
            (space_objectives, "4710", "12596"): "runtime_space_madness_experimental_rockmite_credit_tested",
            (space_objectives, "4711", "12597"): "runtime_space_madness_party_downgrazer_credit_tested",
            (space_objectives, "1656", "5628"): "runtime_space_madness_hazmat_suit_direct_credit_tested",
            (space_objectives, "1594", "6370"): "runtime_space_madness_hallucinating_worker_air_helm_talk_credit_tested",
            (captain_tero, "1579", "6342"): "runtime_space_madness_captain_tero_talk_credit_tested",
            (major_lee, "1588", "6358"): "runtime_space_madness_major_lee_barmy_talk_credit_tested",
            (observation_computer, "1589", "6356"): "runtime_space_madness_observation_deck_computer_credit_tested",
        }
        for key, expected_status in expected_statuses.items():
            with self.subTest(key=key):
                self.assertIn(key, rows_by_key)
                for row in rows_by_key[key]:
                    self.assertEqual(expected_status, row["evidence_status"])
                    self.assertFalse(audit.is_script_objective_producer_blocker(row))
                    self.assertNotRegex(row["evidence_status"], "pending|blocked|wip|mapped_only|reviewed")

        dependency_statuses = {
            1579: "runtime_space_madness_captain_tero_talk_objective_activation_and_credit_tested",
            1588: "runtime_space_madness_major_lee_barmy_talk_objective_activation_and_credit_tested",
        }
        for objective_id, expected_status in dependency_statuses.items():
            with self.subTest(dependency=objective_id):
                dependency = audit.INSTANCE_OBJECTIVE_DEPENDENCY_OVERRIDES[(2149, objective_id)]
                self.assertEqual(expected_status, dependency["evidence_status"])
                self.assertEqual("automated_focus_pass", dependency["manual_validation"])
                self.assertNotRegex(dependency["evidence_status"], "pending|blocked|wip|mapped_only|reviewed")

        blocked_statuses = {
            (space_event, "1662", "6447"): "activation_only_blocked_missing_wave_credit",
            (space_event, "2115", ""): "activation_only_blocked_missing_wave_timer_credit",
            (space_event, "4708", ""): "activation_only_blocked_missing_hit_failure_producer",
        }
        for key, expected_status_part in blocked_statuses.items():
            with self.subTest(key=key):
                self.assertIn(key, rows_by_key)
                row = rows_by_key[key][0]
                self.assertIn(expected_status_part, row["evidence_status"])
                self.assertTrue(audit.is_script_objective_producer_blocker(row))

    def test_initialization_core_door_and_boss_challenge_rows_are_concrete(self) -> None:
        door_statuses = {
            2681: (
                "595;2681;12539;68824;49903",
                "runtime_initialization_core_quarantine_panel_activation_credit_tested_pending_six_panel_placement_door_choreography_client_smoke",
                "TargetGroup 12539 contains Creature2 68824",
            ),
            2682: (
                "595;2682;12462;66047;49904",
                "runtime_initialization_core_quarantine_door_activation_credit_tested_pending_door_choreography_client_smoke",
                "TargetGroup 12462 contains Creature2 66047",
            ),
        }
        for objective_id, (owner_ids, expected_status, summary_text) in door_statuses.items():
            with self.subTest(objective_id=objective_id):
                dependency = audit.INSTANCE_OBJECTIVE_DEPENDENCY_OVERRIDES[(3040, objective_id)]
                self.assertIn("InitializationCoreY83EventScript.cs", dependency["script_path"])
                self.assertIn("InitializationCoreY83ObjectiveEntityScripts.cs", dependency["script_path"])
                self.assertEqual(owner_ids, dependency["script_owner_ids"])
                self.assertEqual(expected_status, dependency["evidence_status"])
                self.assertIn(summary_text, dependency["blocker_summary"])

        event_panel = audit.SCRIPT_OBJECTIVE_PRODUCER_OVERRIDES[
            (
                r"Source\NexusForever.Script.Instance\Raid\InitializationCoreY83\InitializationCoreY83EventScript.cs",
                "2681",
                "12539",
            )
        ]
        self.assertEqual("script_owner_matches_public_event", event_panel["owner_link_status"])

        panel_credit = audit.SCRIPT_OBJECTIVE_PRODUCER_OVERRIDES[
            (
                r"Source\NexusForever.Script.Instance\Raid\InitializationCoreY83\Script\InitializationCoreY83ObjectiveEntityScripts.cs",
                "2681",
                "12539",
            )
        ]
        self.assertEqual("script_owner_entity_filter_mapped_to_public_event", panel_credit["owner_link_status"])

        boss_statuses = {
            2683: "mapped_initialization_core_prime_operants_final_boss_blocked_missing_boss_spawn_death_credit_producer_client_smoke",
            2684: "mapped_initialization_core_immortal_prime_operants_blocked_missing_party_death_fail_state_reward_producer_client_smoke",
            4636: "mapped_initialization_core_evolutionary_revolutions_blocked_missing_phagetech_augmentation_active_state_boss_credit_client_smoke",
            4637: "mapped_initialization_core_challenge2_blocked_missing_challenge_identity_mechanics_reward_client_smoke",
        }
        for objective_id, expected_status in boss_statuses.items():
            with self.subTest(objective_id=objective_id):
                dependency = audit.INSTANCE_OBJECTIVE_DEPENDENCY_OVERRIDES[(3040, objective_id)]
                self.assertEqual(expected_status, dependency["evidence_status"])
                self.assertNotEqual(
                    "mapped_public_event_objective_blocked_missing_runtime_producer_evidence",
                    dependency["evidence_status"],
                )
                self.assertIn("public_event_objective_client_map.csv", dependency["evidence_sources"])
                self.assertIn("smoke", dependency["blocker_summary"])

        final_boss_producer = audit.SCRIPT_OBJECTIVE_PRODUCER_OVERRIDES[
            (
                r"Source\NexusForever.Script.Instance\Raid\InitializationCoreY83\InitializationCoreY83EventScript.cs",
                "2683",
                "",
            )
        ]
        self.assertIn("activation_only_blocked_missing_boss_death_credit", final_boss_producer["evidence_status"])
        self.assertIn("Residual boss/challenge rows 2683", audit.SCRIPTED_INSTANCE_BLOCKERS[3040])

    def test_ultimate_protogames_start_button_overrides_clear_tested_rows(self) -> None:
        blocker_tokens = ("pending", "blocked", "mapped_only", "reviewed", "wip")
        dependency = audit.INSTANCE_OBJECTIVE_DEPENDENCY_OVERRIDES[(2980, 3250)]
        self.assertFalse(any(token in dependency["evidence_status"] for token in blocker_tokens))
        self.assertEqual("automated_focus_pass", dependency["manual_validation"])

        producer_keys = (
            (
                r"Source\NexusForever.Script.Instance\Dungeon\UltimateProtogames\UltimateProtogamesEventScript.cs",
                "3250",
                "",
            ),
            (
                r"Source\NexusForever.Script.Instance\Dungeon\UltimateProtogames\Script\StartButtonEntityScript.cs",
                "3250",
                "",
            ),
        )
        for key in producer_keys:
            producer = audit.SCRIPT_OBJECTIVE_PRODUCER_OVERRIDES[key]
            self.assertFalse(any(token in producer["evidence_status"] for token in blocker_tokens))
            self.assertEqual("automated_focus_pass", producer["manual_validation"])

    def test_ultimate_protogames_optional_death_credit_overrides_track_creature_bindings(self) -> None:
        cases = [
            (
                2926,
                "10657",
                "594;2926;10657;62575;41745;299;2415;28306;7903034;1100300087;21714;1322;1369030",
                "runtime_up_mondos_monstrosity_phase_activation_jabbithole_placement_and_death_credit_tested",
                "KillTargetGroup object 10657, count 1",
                "Creature2 62575 Mondo's Monstrosity",
            ),
            (
                2941,
                "",
                "594;2941;62575;41745;299;2415;28306;7903034;1100300087;21714;1322;1369030",
                "runtime_up_mondos_quick_reflexes_phase_activation_jabbithole_placement_and_death_credit_tested",
                "KillEventObjectiveUnit object 0, count 1",
                "Creature2 62575 Mondo's Monstrosity",
            ),
            (
                2920,
                "",
                "594;2920;62549;41745;299;8190;2184;28277;6132219;1100300090;28700;1330;1410065408;2",
                "runtime_up_mondos_crate_phase_activation_jabbithole_placement_dynamic_max_and_death_credit_tested",
                "objective 2920 to a Script row, count 0",
                "Creature2 62549",
            ),
            (
                4561,
                "12390",
                "594;4561;12390;65794;299;2217;28408;7930052;1100300088;23396;1322;3420014;2",
                "runtime_up_ruffles_phase_activation_jabbithole_placement_and_death_credit_tested",
                "KillClusterTargetGroup object 12390, count 1",
                "Creature2 65794 Ruffles",
            ),
            (
                4657,
                "12528",
                "594;4657;12528;68949;41759;28416;8024837;1100300086;36776;1322;24",
                "runtime_up_deputy_phase_activation_datamapping_placement_and_death_credit_tested_pending_room_density_pathing_and_client_smoke",
                "KillTargetGroup object 12528, count 1",
                "Creature2 68949 Deputy",
            ),
            (
                4692,
                "12577",
                "594;4692;12577;63312;299;8141;28290;8469464;1100300080",
                "runtime_up_misplaced_mammoth_jabbithole_placement_and_death_credit_tested",
                "KillTargetGroup object 12577, count 1",
                "Creature2 63312 Misplaced Mammoth",
            ),
        ]

        for objective_id, object_id, owner_ids, status, objective_summary, creature_summary in cases:
            with self.subTest(objective_id=objective_id):
                dependency = audit.INSTANCE_OBJECTIVE_DEPENDENCY_OVERRIDES[(2980, objective_id)]
                self.assertIn("UltimateProtogamesObjectiveEntityScripts.cs", dependency["script_path"])
                if objective_id in {2920, 2926, 2941, 4561}:
                    self.assertIn("UltimateProtogamesEventScript.cs", dependency["script_path"])
                self.assertEqual(owner_ids, dependency["script_owner_ids"])
                self.assertEqual(status, dependency["evidence_status"])

                producer = audit.SCRIPT_OBJECTIVE_PRODUCER_OVERRIDES[
                    (
                        r"Source\NexusForever.Script.Instance\Dungeon\UltimateProtogames\Script\UltimateProtogamesObjectiveEntityScripts.cs",
                        str(objective_id),
                        object_id,
                    )
                ]
                self.assertEqual("script_owner_entity_filter_mapped_to_public_event", producer["owner_link_status"])
                self.assertEqual(owner_ids, producer["script_owner_ids"])
                self.assertIn(objective_summary, producer["blocker_summary"])
                self.assertIn(creature_summary, producer["blocker_summary"])

        mondo_event_producer = audit.SCRIPT_OBJECTIVE_PRODUCER_OVERRIDES[
            (
                r"Source\NexusForever.Script.Instance\Dungeon\UltimateProtogames\UltimateProtogamesEventScript.cs",
                "2926",
                "10657",
            )
        ]
        self.assertEqual("script_owner_matches_public_event", mondo_event_producer["owner_link_status"])
        self.assertEqual(
            "runtime_up_mondos_monstrosity_phase_activation_jabbithole_placement_tested",
            mondo_event_producer["evidence_status"],
        )
        self.assertIn("WorldLocation2 41745", mondo_event_producer["blocker_summary"])
        self.assertIn("coordinate 7903034", mondo_event_producer["blocker_summary"])
        self.assertIn("1100300087", mondo_event_producer["script_owner_ids"])

        mondo_quick_reflexes_event_producer = audit.SCRIPT_OBJECTIVE_PRODUCER_OVERRIDES[
            (
                r"Source\NexusForever.Script.Instance\Dungeon\UltimateProtogames\UltimateProtogamesEventScript.cs",
                "2941",
                "",
            )
        ]
        self.assertEqual("script_owner_matches_public_event", mondo_quick_reflexes_event_producer["owner_link_status"])
        self.assertEqual(
            "runtime_up_mondos_quick_reflexes_phase_activation_jabbithole_placement_tested",
            mondo_quick_reflexes_event_producer["evidence_status"],
        )
        self.assertIn("WorldLocation2 41745", mondo_quick_reflexes_event_producer["blocker_summary"])
        self.assertIn("coordinate 7903034", mondo_quick_reflexes_event_producer["blocker_summary"])
        self.assertIn("1100300087", mondo_quick_reflexes_event_producer["script_owner_ids"])

        mondo_crate_event_producer = audit.SCRIPT_OBJECTIVE_PRODUCER_OVERRIDES[
            (
                r"Source\NexusForever.Script.Instance\Dungeon\UltimateProtogames\UltimateProtogamesEventScript.cs",
                "2920",
                "",
            )
        ]
        self.assertEqual("script_owner_matches_public_event", mondo_crate_event_producer["owner_link_status"])
        self.assertEqual(
            "runtime_up_mondos_crate_phase_activation_jabbithole_placement_dynamic_max_tested",
            mondo_crate_event_producer["evidence_status"],
        )
        self.assertIn("WorldLocation2 41745", mondo_crate_event_producer["blocker_summary"])
        self.assertIn("coordinate 6132219", mondo_crate_event_producer["blocker_summary"])
        self.assertIn("script-owned Mondo's Crate entity 1100300090", mondo_crate_event_producer["blocker_summary"])

        ruffles_event_producer = audit.SCRIPT_OBJECTIVE_PRODUCER_OVERRIDES[
            (
                r"Source\NexusForever.Script.Instance\Dungeon\UltimateProtogames\UltimateProtogamesEventScript.cs",
                "4561",
                "12390",
            )
        ]
        self.assertEqual("script_owner_matches_public_event", ruffles_event_producer["owner_link_status"])
        self.assertEqual(
            "runtime_up_ruffles_phase_activation_jabbithole_placement_tested",
            ruffles_event_producer["evidence_status"],
        )
        self.assertIn("source coordinate 7930052", ruffles_event_producer["blocker_summary"])
        self.assertIn("script-owned entity 1100300088", ruffles_event_producer["blocker_summary"])
        self.assertIn("hunt route/pathing", ruffles_event_producer["blocker_summary"])

        self.assertIn(
            "Misplaced Mammoth objective 4692 WIP phase activation, Jabbithole placement spawn, and death credit",
            audit.SCRIPTED_INSTANCE_BLOCKERS[2980],
        )
        self.assertIn(
            "Mondo's Monstrosity objective 2926 WIP phase activation, WorldLocation2/Jabbithole/DataMapping placement spawn, and death credit",
            audit.SCRIPTED_INSTANCE_BLOCKERS[2980],
        )
        self.assertIn(
            "Mondo's Crate objective 2920 WIP phase activation, Jabbithole/DataMapping placement spawn, dynamic max count-1 initialization, and death credit",
            audit.SCRIPTED_INSTANCE_BLOCKERS[2980],
        )
        self.assertIn(
            "Ruffles objective 4561 WIP phase activation, Jabbithole/DataMapping placement spawn, and death credit",
            audit.SCRIPTED_INSTANCE_BLOCKERS[2980],
        )
        self.assertIn(
            "Deputy objective 4657 WIP phase activation, DataMapping/Jabbithole creature placement spawn, and death credit",
            audit.SCRIPTED_INSTANCE_BLOCKERS[2980],
        )

        deputy_event_producer = audit.SCRIPT_OBJECTIVE_PRODUCER_OVERRIDES[
            (
                r"Source\NexusForever.Script.Instance\Dungeon\UltimateProtogames\UltimateProtogamesEventScript.cs",
                "4657",
                "12528",
            )
        ]
        self.assertEqual("script_owner_matches_public_event", deputy_event_producer["owner_link_status"])
        self.assertEqual(
            "runtime_up_deputy_phase_activation_datamapping_placement_tested_pending_room_density_pathing_and_client_smoke",
            deputy_event_producer["evidence_status"],
        )
        self.assertIn("source coordinate 8024837", deputy_event_producer["blocker_summary"])
        self.assertIn("script-owned entity 1100300086", deputy_event_producer["blocker_summary"])
        self.assertIn("Deputy density/pathing", deputy_event_producer["blocker_summary"])

    def test_ultimate_protogames_misplaced_mammoth_overrides_track_placement_activation_and_death_credit(self) -> None:
        dependency = audit.INSTANCE_OBJECTIVE_DEPENDENCY_OVERRIDES[(2980, 4692)]
        self.assertIn("UltimateProtogamesEventScript.cs", dependency["script_path"])
        self.assertIn("UltimateProtogamesObjectiveEntityScripts.cs", dependency["script_path"])
        self.assertEqual(
            "594;4692;12577;63312;299;8141;28290;8469464;1100300080",
            dependency["script_owner_ids"],
        )
        self.assertEqual(
            "runtime_up_misplaced_mammoth_jabbithole_placement_and_death_credit_tested",
            dependency["evidence_status"],
        )

        event_producer = audit.SCRIPT_OBJECTIVE_PRODUCER_OVERRIDES[
            (
                r"Source\NexusForever.Script.Instance\Dungeon\UltimateProtogames\UltimateProtogamesEventScript.cs",
                "4692",
                "12577",
            )
        ]
        self.assertEqual("script_owner_matches_public_event", event_producer["owner_link_status"])
        self.assertEqual(
            "runtime_up_misplaced_mammoth_phase_activation_and_jabbithole_placement_tested",
            event_producer["evidence_status"],
        )
        self.assertIn("MisplacedMammoth phase", event_producer["blocker_summary"])
        self.assertIn("coordinate 8469464", event_producer["blocker_summary"])
        self.assertIn("script-owned entity 1100300080", event_producer["blocker_summary"])
        self.assertIn("30000 ms timer", event_producer["blocker_summary"])

        entity_producer = audit.SCRIPT_OBJECTIVE_PRODUCER_OVERRIDES[
            (
                r"Source\NexusForever.Script.Instance\Dungeon\UltimateProtogames\Script\UltimateProtogamesObjectiveEntityScripts.cs",
                "4692",
                "12577",
            )
        ]
        self.assertEqual("script_owner_entity_filter_mapped_to_public_event", entity_producer["owner_link_status"])
        self.assertIn("failureTimeMs 30000", entity_producer["blocker_summary"])
        self.assertIn("public event row 299", entity_producer["blocker_summary"])
        self.assertIn("objective row 8141", entity_producer["blocker_summary"])
        self.assertIn("reviewed coordinate 8469464", entity_producer["blocker_summary"])

    def test_ultimate_protogames_prototentiary_overrides_track_cage_and_alarm_placements(self) -> None:
        self.assertIn(
            "Prototentiary Gate Console 2847 alias placement bridge, Cage Console 2864, Prototentiary aggregate objective 2678 Warden-plus-free-intern activation, Fast Hands 2863 timed bonus activation, Alarm Panel 4648 WIP phase activation",
            audit.SCRIPTED_INSTANCE_BLOCKERS[2980],
        )
        self.assertIn("Warden 62324 DataMapping/Jabbithole placement spawn and aggregate death credit", audit.SCRIPTED_INSTANCE_BLOCKERS[2980])
        self.assertIn("remaining Prototentiary stealth/no-alarm semantics", audit.SCRIPTED_INSTANCE_BLOCKERS[2980])
        self.assertNotIn(
            "Gate Console placement remains blocked by the client 62987 versus Jabbithole/DataMapping 62427 mismatch",
            audit.SCRIPTED_INSTANCE_BLOCKERS[2980],
        )

        gate_dependency = audit.INSTANCE_OBJECTIVE_DEPENDENCY_OVERRIDES[(2980, 2847)]
        self.assertIn("UltimateProtogamesEventScript.cs", gate_dependency["script_path"])
        self.assertIn("SneakyPrisonEntityScripts.cs", gate_dependency["script_path"])
        self.assertIn("62987", gate_dependency["script_owner_ids"])
        self.assertIn("62427", gate_dependency["script_owner_ids"])
        self.assertIn("6152099", gate_dependency["script_owner_ids"])
        self.assertIn("1100300085", gate_dependency["script_owner_ids"])
        self.assertEqual(
            "runtime_up_sneaky_prison_gate_console_alias_placement_and_credit_tested_pending_route_door_state_and_client_smoke",
            gate_dependency["evidence_status"],
        )
        self.assertIn("Tools/DataMapping/output/creature_bridge_review.csv", gate_dependency["evidence_sources"])

        aggregate_dependency = audit.INSTANCE_OBJECTIVE_DEPENDENCY_OVERRIDES[(2980, 2678)]
        self.assertIn("UltimateProtogamesEventScript.cs", aggregate_dependency["script_path"])
        self.assertIn("SneakyPrisonEntityScripts.cs", aggregate_dependency["script_path"])
        self.assertIn("UltimateProtogamesObjectiveEntityScripts.cs", aggregate_dependency["script_path"])
        self.assertIn("8181", aggregate_dependency["script_owner_ids"])
        self.assertIn("10583", aggregate_dependency["script_owner_ids"])
        self.assertIn("62324", aggregate_dependency["script_owner_ids"])
        self.assertIn("7099765", aggregate_dependency["script_owner_ids"])
        self.assertIn("1100300092", aggregate_dependency["script_owner_ids"])
        self.assertEqual(
            "runtime_up_sneaky_prison_aggregate_warden_and_free_intern_phase_activation_placements_and_credit_tested_pending_stealth_no_alarm_client_smoke",
            aggregate_dependency["evidence_status"],
        )

        fast_hands_dependency = audit.INSTANCE_OBJECTIVE_DEPENDENCY_OVERRIDES[(2980, 2863)]
        self.assertIn("UltimateProtogamesEventScript.cs", fast_hands_dependency["script_path"])
        self.assertIn("SneakyPrisonEntityScripts.cs", fast_hands_dependency["script_path"])
        self.assertIn("8196", fast_hands_dependency["script_owner_ids"])
        self.assertIn("10583", fast_hands_dependency["script_owner_ids"])
        self.assertIn("210000", fast_hands_dependency["script_owner_ids"])
        self.assertEqual(
            "runtime_up_sneaky_prison_fast_hands_phase_activation_jabbithole_placement_and_credit_tested_pending_exact_timer_failure_and_client_smoke",
            fast_hands_dependency["evidence_status"],
        )

        cage_dependency = audit.INSTANCE_OBJECTIVE_DEPENDENCY_OVERRIDES[(2980, 2864)]
        self.assertIn("UltimateProtogamesEventScript.cs", cage_dependency["script_path"])
        self.assertIn("SneakyPrisonEntityScripts.cs", cage_dependency["script_path"])
        self.assertIn("6152147", cage_dependency["script_owner_ids"])
        self.assertIn("1100300081", cage_dependency["script_owner_ids"])
        self.assertEqual(
            "runtime_up_sneaky_prison_cage_console_phase_activation_jabbithole_placement_and_credit_tested_pending_route_cage_state_and_client_smoke",
            cage_dependency["evidence_status"],
        )

        alarm_dependency = audit.INSTANCE_OBJECTIVE_DEPENDENCY_OVERRIDES[(2980, 4648)]
        self.assertIn("UltimateProtogamesEventScript.cs", alarm_dependency["script_path"])
        self.assertIn("SneakyPrisonEntityScripts.cs", alarm_dependency["script_path"])
        self.assertIn("6494141", alarm_dependency["script_owner_ids"])
        self.assertIn("6845485", alarm_dependency["script_owner_ids"])
        self.assertIn("1100300084", alarm_dependency["script_owner_ids"])
        self.assertEqual(
            "runtime_up_sneaky_prison_alarm_panel_phase_activation_jabbithole_placements_and_credit_tested_pending_alarm_timer_client_smoke",
            alarm_dependency["evidence_status"],
        )

        gate_activation = audit.SCRIPT_OBJECTIVE_PRODUCER_OVERRIDES[
            (
                r"Source\NexusForever.Script.Instance\Dungeon\UltimateProtogames\UltimateProtogamesEventScript.cs",
                "2847",
                "10569",
            )
        ]
        self.assertEqual("script_owner_matches_public_event", gate_activation["owner_link_status"])
        self.assertIn("dynamic max 1", gate_activation["blocker_summary"])
        self.assertIn("source coordinate 6152099", gate_activation["blocker_summary"])
        self.assertIn("script-owned entity 1100300085", gate_activation["blocker_summary"])
        self.assertIn("Creature2 62427 alias", gate_activation["blocker_summary"])

        aggregate_activation = audit.SCRIPT_OBJECTIVE_PRODUCER_OVERRIDES[
            (
                r"Source\NexusForever.Script.Instance\Dungeon\UltimateProtogames\UltimateProtogamesEventScript.cs",
                "2678",
                "10583",
            )
        ]
        self.assertEqual("script_owner_matches_public_event", aggregate_activation["owner_link_status"])
        self.assertIn("objective row 8181", aggregate_activation["blocker_summary"])
        self.assertIn("dynamic max 2", aggregate_activation["blocker_summary"])
        self.assertIn("coordinate 7099765", aggregate_activation["blocker_summary"])
        self.assertIn("WardenEntityScript credits the Warden death leg", aggregate_activation["blocker_summary"])

        fast_hands_activation = audit.SCRIPT_OBJECTIVE_PRODUCER_OVERRIDES[
            (
                r"Source\NexusForever.Script.Instance\Dungeon\UltimateProtogames\UltimateProtogamesEventScript.cs",
                "2863",
                "10583",
            )
        ]
        self.assertEqual("script_owner_matches_public_event", fast_hands_activation["owner_link_status"])
        self.assertIn("failureTimeMs 210000", fast_hands_activation["blocker_summary"])
        self.assertIn("objective row 8196", fast_hands_activation["blocker_summary"])
        self.assertIn("dynamic max 1", fast_hands_activation["blocker_summary"])
        self.assertIn("active 2678/2863/2864 rows can advance together", fast_hands_activation["blocker_summary"])
        self.assertIn("stealth semantics for aggregate objective 2678", fast_hands_activation["blocker_summary"])

        cage_activation = audit.SCRIPT_OBJECTIVE_PRODUCER_OVERRIDES[
            (
                r"Source\NexusForever.Script.Instance\Dungeon\UltimateProtogames\UltimateProtogamesEventScript.cs",
                "2864",
                "10583",
            )
        ]
        self.assertEqual("script_owner_matches_public_event", cage_activation["owner_link_status"])
        self.assertIn("dynamic max 1", cage_activation["blocker_summary"])
        self.assertIn("source coordinate 6152147", cage_activation["blocker_summary"])
        self.assertIn("exact Fast Hands timer/failure smoke for 2863", cage_activation["blocker_summary"])
        self.assertIn("Gate Console alias proof/client smoke and door choreography", cage_activation["blocker_summary"])
        self.assertIn("Warden/stealth semantics for aggregate objective 2678", cage_activation["blocker_summary"])

        alarm_activation = audit.SCRIPT_OBJECTIVE_PRODUCER_OVERRIDES[
            (
                r"Source\NexusForever.Script.Instance\Dungeon\UltimateProtogames\UltimateProtogamesEventScript.cs",
                "4648",
                "12478",
            )
        ]
        self.assertEqual("script_owner_matches_public_event", alarm_activation["owner_link_status"])
        self.assertIn("coordinates 6494141, 6494142, and 6845485", alarm_activation["blocker_summary"])
        self.assertIn("script-owned entities 1100300082, 1100300083, and 1100300084", alarm_activation["blocker_summary"])

        gate_credit = audit.SCRIPT_OBJECTIVE_PRODUCER_OVERRIDES[
            (
                r"Source\NexusForever.Script.Instance\Dungeon\UltimateProtogames\Script\SneakyPrisonEntityScripts.cs",
                "2847",
                "10569",
            )
        ]
        self.assertEqual("script_owner_entity_filter_mapped_to_public_event", gate_credit["owner_link_status"])
        self.assertIn("objective row 8177", gate_credit["blocker_summary"])
        self.assertIn("coordinate 6152099", gate_credit["blocker_summary"])
        self.assertIn("Creature2 62427", gate_credit["blocker_summary"])
        self.assertIn("TargetGroup 10569 once on activation", gate_credit["blocker_summary"])

        cage_credit = audit.SCRIPT_OBJECTIVE_PRODUCER_OVERRIDES[
            (
                r"Source\NexusForever.Script.Instance\Dungeon\UltimateProtogames\Script\SneakyPrisonEntityScripts.cs",
                "2678;2858;2863;2864",
                "10583",
            )
        ]
        self.assertEqual("script_owner_entity_filter_mapped_to_public_event", cage_credit["owner_link_status"])
        self.assertEqual(
            "runtime_up_sneaky_prison_cage_console_aggregate_fast_hands_phase_activation_warden_pair_and_credit_tested_pending_route_timer_cage_state_and_client_smoke",
            cage_credit["evidence_status"],
        )
        self.assertIn("aggregate objective row 8181", cage_credit["blocker_summary"])
        self.assertIn("objective row 8202", cage_credit["blocker_summary"])
        self.assertIn("Fast Hands objective row 8196", cage_credit["blocker_summary"])
        self.assertIn("dynamic max 2", cage_credit["blocker_summary"])
        self.assertIn("Warden 1100300092", cage_credit["blocker_summary"])
        self.assertIn("WardenEntityScript credits the paired aggregate death leg", cage_credit["blocker_summary"])
        self.assertIn("no-alarm objective 2858 semantics", cage_credit["blocker_summary"])

        warden_credit = audit.SCRIPT_OBJECTIVE_PRODUCER_OVERRIDES[
            (
                r"Source\NexusForever.Script.Instance\Dungeon\UltimateProtogames\Script\UltimateProtogamesObjectiveEntityScripts.cs",
                "2678",
                "10583",
            )
        ]
        self.assertEqual("script_owner_entity_filter_mapped_to_public_event", warden_credit["owner_link_status"])
        self.assertEqual(
            "runtime_up_sneaky_prison_warden_placement_and_aggregate_death_credit_tested_pending_stealth_no_alarm_client_smoke",
            warden_credit["evidence_status"],
        )
        self.assertIn("TargetGroup 12474 contains Warden Creature2 62324", warden_credit["blocker_summary"])
        self.assertIn("coordinate 7099765", warden_credit["blocker_summary"])
        self.assertIn("script-owned Warden entity 1100300092", warden_credit["blocker_summary"])

        alarm_credit = audit.SCRIPT_OBJECTIVE_PRODUCER_OVERRIDES[
            (
                r"Source\NexusForever.Script.Instance\Dungeon\UltimateProtogames\Script\SneakyPrisonEntityScripts.cs",
                "4648",
                "12478",
            )
        ]
        self.assertEqual("script_owner_entity_filter_mapped_to_public_event", alarm_credit["owner_link_status"])
        self.assertIn("TargetGroup 12478", alarm_credit["blocker_summary"])
        self.assertIn("Alarm Panel creature row 30477", alarm_credit["blocker_summary"])

    def test_gauntlet_boss_producer_overrides_track_new_target_group_rows(self) -> None:
        cases = [
            (
                1866,
                "6854",
                "446;1866;6854;48511;48499;69258;69257",
                "runtime_gauntlet_voodoo_kin_death_credit_tested_pending_arena_order_spawn_route_and_client_smoke",
                "KillTargetGroup object 6854, count 2",
                "Creature2 48511/69258 Professor Doctor Voodoo",
            ),
            (
                1870,
                "6864",
                "446;1870;6864;48516;48518;48519;69302;69303;69304",
                "runtime_gauntlet_handler_and_pets_death_credit_tested_pending_arena_order_spawn_route_and_client_smoke",
                "KillClusterTargetGroup object 6864, count 1",
                "Creature2 48516/69302 The Handler",
            ),
            (
                1872,
                "6868",
                "446;1872;6868;48493;48510;69300;69301",
                "runtime_gauntlet_pyro_maniac_death_credit_tested_pending_arena_order_spawn_route_and_client_smoke",
                "KillClusterTargetGroup object 6868, count 1",
                "Creature2 48493/69300 Pyro",
            ),
            (
                1873,
                "6866",
                "446;1873;6866;48579;69308",
                "runtime_gauntlet_showtime_death_credit_tested_pending_arena_order_spawn_route_and_client_smoke",
                "KillTargetGroup object 6866, count 1",
                "Creature2 48579/69308 Showtime",
            ),
        ]

        for objective_id, object_id, owner_ids, status, objective_summary, creature_summary in cases:
            with self.subTest(objective_id=objective_id):
                dependency = audit.INSTANCE_OBJECTIVE_DEPENDENCY_OVERRIDES[(2183, objective_id)]
                self.assertIn("GauntletBossEntityScripts.cs", dependency["script_path"])
                self.assertEqual(owner_ids, dependency["script_owner_ids"])
                self.assertEqual(status, dependency["evidence_status"])

                producer = audit.SCRIPT_OBJECTIVE_PRODUCER_OVERRIDES[
                    (
                        r"Source\NexusForever.Script.Instance\Expedition\Gauntlet\Script\GauntletBossEntityScripts.cs",
                        str(objective_id),
                        object_id,
                    )
                ]
                self.assertEqual("script_owner_entity_filter_mapped_to_public_event", producer["owner_link_status"])
                self.assertEqual(owner_ids, producer["script_owner_ids"])
                self.assertIn(objective_summary, producer["blocker_summary"])
                self.assertIn(creature_summary, producer["blocker_summary"])

        additional_cases = [
            (
                1835,
                "446;1835;48666;48667;48669;48670;69282;69283;69284;69285",
                "runtime_gauntlet_opposing_faction_team_death_credit_tested_pending_faction_arena_route_and_client_smoke",
                "KillEventObjectiveUnit count 3",
                "OpposingFactionTeamEntityScript",
            ),
            (
                1864,
                "446;1864;6855;48491;69254",
                "runtime_gauntlet_rockstar_yeti_death_credit_tested_pending_arena_order_spawn_route_and_client_smoke",
                "KillTargetGroup object 6855, count 1",
                "RockstarYetiEntityScript",
            ),
            (
                1865,
                "446;1865;6856;48529;69255",
                "runtime_gauntlet_championator_death_credit_tested_pending_charnel_chamber_route_and_client_smoke",
                "KillTargetGroup object 6856, count 1",
                "ChampionatorEntityScript",
            ),
            (
                1869,
                "446;1869;6863;48588;48589;48591;69309;69310;69311;52112",
                "runtime_gauntlet_goon_squad_death_credit_tested_pending_main_event_route_and_client_smoke",
                "KillClusterTargetGroup object 6863, count 1",
                "GoonSquadEntityScript",
            ),
            (
                1871,
                "446;1871;6865;48557;48558;69306;69307",
                "runtime_gauntlet_slice_and_dice_death_credit_tested_pending_main_event_route_and_client_smoke",
                "KillClusterTargetGroup object 6865, count 1",
                "SliceAndDiceEntityScript",
            ),
            (
                1874,
                "446;1874;6867;48554;69305",
                "runtime_gauntlet_shock_king_death_credit_tested_pending_main_event_route_and_client_smoke",
                "KillTargetGroup object 6867, count 1",
                "ShockKingEntityScript",
            ),
            (
                4671,
                "446;4671;69688",
                "runtime_gauntlet_brick_braggor_death_credit_tested_pending_main_event_route_and_client_smoke",
                "KillEventObjectiveUnit count 1",
                "BrickBraggorEntityScript",
            ),
        ]
        for objective_id, owner_ids, status, objective_summary, script_summary in additional_cases:
            with self.subTest(additional_objective_id=objective_id):
                dependency = audit.INSTANCE_OBJECTIVE_DEPENDENCY_OVERRIDES[(2183, objective_id)]
                self.assertIn("GauntletBossEntityScripts.cs", dependency["script_path"])
                self.assertEqual(owner_ids, dependency["script_owner_ids"])
                self.assertEqual(status, dependency["evidence_status"])
                self.assertIn(objective_summary, dependency["blocker_summary"])
                self.assertIn(script_summary, dependency["blocker_summary"])

        self.assertIn(
            "Championator 1865 / Opposing Faction team 1835 / Rockstar Yeti 1864 / Voodoo Kin 1866",
            audit.SCRIPTED_INSTANCE_BLOCKERS[2183],
        )

    def test_gauntlet_script_producer_rows_reuse_instance_dependency_overrides(self) -> None:
        strings = {
            10: "Expedition: The Gauntlet",
            20: "Kill the opposing faction's team",
            21: "Defeat the Goon Squad",
            22: "Defeat the Shock King",
            23: "Defeat Brick Braggor",
            24: "Kill the Rockstar Yeti",
        }
        public_events = [
            {
                "ID": "446",
                "worldId": "2183",
                "worldZoneId": "0",
                "localizedTextIdName": "10",
                "publicEventTypeEnum": "3",
                "publicEventIdParent": "0",
            }
        ]
        public_event_objectives = [
            {
                "ID": "1835",
                "publicEventId": "446",
                "publicEventObjectiveTypeEnum": "8",
                "count": "3",
                "objectId": "0",
                "localizedTextId": "20",
                "localizedTextIdShort": "0",
                "worldLocation2Id": "0",
                "targetGroupIdRewardPane": "0",
                "displayOrder": "1",
            },
            {
                "ID": "1869",
                "publicEventId": "446",
                "publicEventObjectiveTypeEnum": "14",
                "count": "1",
                "objectId": "6863",
                "localizedTextId": "21",
                "localizedTextIdShort": "0",
                "worldLocation2Id": "0",
                "targetGroupIdRewardPane": "0",
                "displayOrder": "2",
            },
            {
                "ID": "1874",
                "publicEventId": "446",
                "publicEventObjectiveTypeEnum": "0",
                "count": "1",
                "objectId": "6867",
                "localizedTextId": "22",
                "localizedTextIdShort": "0",
                "worldLocation2Id": "0",
                "targetGroupIdRewardPane": "0",
                "displayOrder": "3",
            },
            {
                "ID": "4671",
                "publicEventId": "446",
                "publicEventObjectiveTypeEnum": "8",
                "count": "1",
                "objectId": "0",
                "localizedTextId": "23",
                "localizedTextIdShort": "0",
                "worldLocation2Id": "0",
                "targetGroupIdRewardPane": "0",
                "displayOrder": "4",
            },
            {
                "ID": "1864",
                "publicEventId": "446",
                "publicEventObjectiveTypeEnum": "0",
                "count": "1",
                "objectId": "6855",
                "localizedTextId": "24",
                "localizedTextIdShort": "0",
                "worldLocation2Id": "0",
                "targetGroupIdRewardPane": "0",
                "displayOrder": "5",
            },
        ]

        rows = audit.build_script_objective_producer_evidence_rows(
            strings,
            public_events,
            public_event_objectives,
            [audit.ROOT / "Source" / "NexusForever.Script.Instance" / "Expedition" / "Gauntlet"],
        )

        rows_by_source_objective = {
            (row["source_path"], row["source_symbol"], int(row["objective_id"])): row
            for row in rows
            if row["objective_id"] not in ("", 0)
        }
        event_row = rows_by_source_objective[
            (
                r"Source\NexusForever.Script.Instance\Expedition\Gauntlet\GauntletEventScript.cs",
                "PublicEventObjective.DefeatTheGoonSquad",
                1869,
            )
        ]
        self.assertEqual(
            "runtime_gauntlet_goon_squad_death_credit_tested_pending_main_event_route_and_client_smoke",
            event_row["evidence_status"],
        )
        self.assertEqual("script_owner_matches_public_event", event_row["owner_link_status"])
        self.assertIn("GoonSquadEntityScript", event_row["blocker_summary"])

        entity_row = rows_by_source_objective[
            (
                r"Source\NexusForever.Script.Instance\Expedition\Gauntlet\Script\GauntletBossEntityScripts.cs",
                "PublicEventObjective.KillTheRockstarYeti",
                1864,
            )
        ]
        self.assertEqual(
            "runtime_gauntlet_rockstar_yeti_death_credit_tested_pending_arena_order_spawn_route_and_client_smoke",
            entity_row["evidence_status"],
        )
        self.assertEqual("script_owner_entity_filter_mapped_to_public_event", entity_row["owner_link_status"])
        self.assertIn("RockstarYetiEntityScript", entity_row["blocker_summary"])

    def test_gauntlet_talk_objective_overrides_clear_tested_rows(self) -> None:
        blocker_tokens = ("pending", "blocked", "mapped_only", "reviewed", "wip")
        for objective_id in (1821, 1914, 1915):
            override = audit.INSTANCE_OBJECTIVE_DEPENDENCY_OVERRIDES[(2183, objective_id)]
            self.assertFalse(any(token in override["evidence_status"] for token in blocker_tokens))
            self.assertEqual("automated_focus_pass", override["manual_validation"])

    def test_gauntlet_public_event_creature_overrides_cover_script_owned_rows_only(self) -> None:
        first_arena_keys = {
            (446, 2073, 69237),
            (446, 2075, 69248),
            (446, 2076, 69239),
            (446, 2080, 69246),
            (446, 2082, 69243),
            (446, 2084, 69240),
            (446, 2085, 69247),
            (446, 2140, 69242),
            (446, 2142, 69250),
            (446, 2150, 69244),
            (446, 2181, 69241),
            (446, 2199, 69251),
            (446, 2275, 48865),
            (446, 2273, 48866),
            (446, 2339, 48461),
            (446, 2274, 48843),
            (446, 2310, 48833),
            (446, 2311, 48863),
            (446, 2340, 48793),
            (446, 2376, 48765),
            (446, 2377, 48835),
            (446, 2388, 48844),
            (446, 2389, 48842),
            (446, 2390, 48867),
        }
        boss_keys = {
            (446, 484, 48518),
            (446, 491, 48519),
            (446, 492, 48516),
            (446, 493, 48493),
            (446, 495, 48510),
            (446, 765, 48579),
            (446, 767, 48511),
            (446, 770, 48499),
            (446, 2070, 69301),
            (446, 2071, 69300),
            (446, 2077, 69302),
            (446, 2079, 69304),
            (446, 2088, 69303),
            (446, 2147, 69308),
            (446, 2182, 69257),
            (446, 2183, 69258),
        }
        product_keys = {
            (446, 498, 49536),
            (446, 764, 49573),
            (446, 766, 49429),
            (446, 802, 49486),
            (446, 811, 49218),
        }
        door_lock_keys = {(446, 490, 48682)}
        remaining_boss_keys = {
            (446, 483, 48529),
            (446, 488, 48554),
            (446, 752, 48591),
            (446, 753, 48588),
            (446, 754, 48558),
            (446, 755, 48491),
            (446, 757, 48589),
            (446, 758, 48557),
            (446, 2074, 69305),
            (446, 2087, 69688),
            (446, 2089, 69254),
            (446, 2139, 69306),
            (446, 2143, 69307),
            (446, 2144, 69255),
            (446, 2148, 69310),
            (446, 2149, 69309),
        }
        opposing_faction_keys = {
            (446, 798, 48667),
            (446, 799, 48669),
            (446, 859, 48670),
            (446, 860, 48666),
            (446, 2236, 69283),
            (446, 2237, 69284),
            (446, 2283, 69285),
            (446, 2284, 69282),
        }
        pilot_taboro_keys = {(446, 2138, 48580)}

        expected_status_by_keys = {
            "runtime_gauntlet_first_arena_public_event_creature_death_credit_tested_pending_arena_route_rewards_and_client_smoke": first_arena_keys,
            "runtime_gauntlet_selected_boss_public_event_creature_death_credit_tested_pending_arena_order_spawn_route_and_client_smoke": boss_keys,
            "runtime_gauntlet_product_endorsement_public_event_creature_activation_credit_tested_pending_product_side_effects_and_client_smoke": product_keys,
            "runtime_gauntlet_door_lock_public_event_creature_activation_credit_tested_pending_door_visual_state_and_client_smoke": door_lock_keys,
            "runtime_gauntlet_remaining_boss_public_event_creature_death_credit_tested_pending_arena_order_spawn_route_and_client_smoke": remaining_boss_keys,
            "runtime_gauntlet_opposing_faction_public_event_creature_death_credit_tested_pending_faction_arena_route_and_client_smoke": opposing_faction_keys,
            "runtime_gauntlet_pilot_taboro_public_event_creature_talk_credit_tested_pending_dialog_client_smoke": pilot_taboro_keys,
        }
        for expected_status, keys in expected_status_by_keys.items():
            for key in keys:
                with self.subTest(key=key):
                    override = audit.PUBLIC_EVENT_CREATURE_EVIDENCE_OVERRIDES[key]
                    self.assertEqual(expected_status, override["evidence_status"])
                    self.assertIn("Gauntlet", override["blocker_summary"])
                    self.assertIn("no longer double-counted", override["blocker_summary"])
                    self.assertIn(key, audit.PUBLIC_EVENT_CREATURE_BLOCKER_EXEMPTIONS)

        still_blocked_keys = {
            (446, 494, 49812),
            (446, 497, 14949),
            (446, 803, 23784),
            (446, 2078, 23784),
            (446, 2145, 52112),
        }
        for key in still_blocked_keys:
            with self.subTest(still_blocked_key=key):
                self.assertNotIn(key, audit.PUBLIC_EVENT_CREATURE_EVIDENCE_OVERRIDES)
                self.assertNotIn(key, audit.PUBLIC_EVENT_CREATURE_BLOCKER_EXEMPTIONS)

    def test_ruins_main_boss_overrides_track_target_group_death_credit(self) -> None:
        cases = [
            (
                444,
                "3841",
                "161;32534;32535;3841",
                "runtime_ruins_grond_main_boss_activation_spawn_and_death_credit_tested_pending_boss_mechanics_rewards_and_client_smoke",
                r"Source\NexusForever.Script.Instance\Dungeon\RuinsOfKelVoreth\Script\GrondTheCorpsemakerEntityScript.cs",
                "GrondTheCorpsemakerEntityScript",
                "Creature2 32534 and 32535 Grond the Corpsemaker",
            ),
            (
                446,
                "3842",
                "161;32536;32539;3842",
                "runtime_ruins_slavemaster_drokk_main_boss_activation_spawn_and_death_credit_tested_pending_boss_mechanics_rewards_and_client_smoke",
                r"Source\NexusForever.Script.Instance\Dungeon\RuinsOfKelVoreth\Script\SlavemasterDrokkEntityScript.cs",
                "SlavemasterDrokkEntityScript",
                "Creature2 32536 and 32539 Slavemaster Drokk",
            ),
        ]

        for objective_id, object_id, owner_ids, status, script_path, script_name, creature_summary in cases:
            with self.subTest(objective_id=objective_id):
                dependency = audit.INSTANCE_OBJECTIVE_DEPENDENCY_OVERRIDES[(1336, objective_id)]
                self.assertIn("RuinsOfKelVorethEventScript.cs", dependency["script_path"])
                self.assertIn(script_path.rsplit("\\", 1)[-1], dependency["script_path"])
                self.assertEqual(owner_ids, dependency["script_owner_ids"])
                self.assertEqual(status, dependency["evidence_status"])

                producer = audit.SCRIPT_OBJECTIVE_PRODUCER_OVERRIDES[(script_path, str(objective_id), object_id)]
                self.assertEqual("script_owner_entity_filter_mapped_to_public_event", producer["owner_link_status"])
                self.assertEqual(owner_ids, producer["script_owner_ids"])
                self.assertIn(f"KillTargetGroup object {object_id}", producer["blocker_summary"])
                self.assertIn(creature_summary, producer["blocker_summary"])
                self.assertIn(script_name, producer["blocker_summary"])

        self.assertIn("Grond the Corpsemaker 444 and Slavemaster Drokk 446", audit.SCRIPTED_INSTANCE_BLOCKERS[1336])

    def test_ruins_optional_checklist_overrides_track_activation_producers(self) -> None:
        cases = [
            (
                456,
                "4422",
                "161;456;4422;33155",
                "runtime_ruins_eldan_data_storage_checklist_activation_credit_tested_pending_optional_route_visual_state_and_client_smoke",
                "ActivateTargetGroupChecklist object 4422, count 6",
                "Creature2 33155 Eldan Schematic Data Storage",
            ),
            (
                451,
                "3916",
                "161;451;3916;33334",
                "runtime_ruins_war_supplies_checklist_activation_credit_tested_pending_optional_route_visual_state_and_client_smoke",
                "ActivateTargetGroupChecklist object 3916, count 10",
                "Creature2 33334 Kel Voreth War Supplies",
            ),
            (
                459,
                "4022",
                "161;459;4022;33768",
                "runtime_ruins_eldan_phase_monitor_checklist_activation_credit_tested_pending_optional_route_visual_state_and_client_smoke",
                "ActivateTargetGroupChecklist object 4022, count 5",
                "Creature2 33768 Eldan Phase Monitor",
            ),
            (
                461,
                "3903",
                "161;461;3903;33302",
                "runtime_ruins_kel_voreth_forge_checklist_activation_credit_tested_pending_optional_route_visual_state_and_client_smoke",
                "ActivateTargetGroupChecklist object 3903, count 6",
                "Creature2 33302 Kel Voreth Forge",
            ),
        ]

        for objective_id, object_id, owner_ids, status, objective_summary, creature_summary in cases:
            with self.subTest(objective_id=objective_id):
                dependency = audit.INSTANCE_OBJECTIVE_DEPENDENCY_OVERRIDES[(1336, objective_id)]
                self.assertIn("RuinsOfKelVorethObjectiveEntityScripts.cs", dependency["script_path"])
                self.assertEqual(owner_ids, dependency["script_owner_ids"])
                self.assertEqual(status, dependency["evidence_status"])

                producer = audit.SCRIPT_OBJECTIVE_PRODUCER_OVERRIDES[
                    (
                        r"Source\NexusForever.Script.Instance\Dungeon\RuinsOfKelVoreth\Script\RuinsOfKelVorethObjectiveEntityScripts.cs",
                        str(objective_id),
                        object_id,
                    )
                ]
                self.assertEqual("script_owner_entity_filter_mapped_to_public_event", producer["owner_link_status"])
                self.assertEqual(owner_ids, producer["script_owner_ids"])
                self.assertIn(objective_summary, producer["blocker_summary"])
                self.assertIn(creature_summary, producer["blocker_summary"])

        self.assertIn(
            "Eldan Schematic Data Storage 456, Kel Voreth War Supplies 451, Eldan Phase Monitor 459, and Kel Voreth Forge 461",
            audit.SCRIPTED_INSTANCE_BLOCKERS[1336],
        )

    def test_ruins_public_event_creature_rows_use_runtime_exemptions(self) -> None:
        expected_status_by_key = {
            (161, 114, 32534): "runtime_ruins_grond_public_event_creature_spawn_and_death_credit_tested_pending_boss_mechanics_rewards_and_client_smoke",
            (161, 216, 32535): "runtime_ruins_grond_public_event_creature_spawn_and_death_credit_tested_pending_boss_mechanics_rewards_and_client_smoke",
            (161, 122, 32536): "runtime_ruins_drokk_public_event_creature_spawn_and_death_credit_tested_pending_boss_mechanics_rewards_and_client_smoke",
            (161, 127, 32539): "runtime_ruins_drokk_public_event_creature_spawn_and_death_credit_tested_pending_boss_mechanics_rewards_and_client_smoke",
            (161, 47, 33049): "runtime_ruins_gurka_public_event_creature_death_credit_tested_pending_choreography_rewards_and_client_smoke",
            (161, 407, 33050): "runtime_ruins_gurka_public_event_creature_death_credit_tested_pending_choreography_rewards_and_client_smoke",
            (161, 111, 32555): "runtime_ruins_battlesworn_darkwitch_public_event_creature_death_credit_tested_pending_route_spawn_rewards_and_client_smoke",
            (161, 401, 32556): "runtime_ruins_battlesworn_darkwitch_public_event_creature_death_credit_tested_pending_route_spawn_rewards_and_client_smoke",
            (161, 107, 32618): "runtime_ruins_battlesworn_darkwitch_public_event_creature_death_credit_tested_pending_route_spawn_rewards_and_client_smoke",
            (161, 404, 32619): "runtime_ruins_battlesworn_darkwitch_public_event_creature_death_credit_tested_pending_route_spawn_rewards_and_client_smoke",
            (161, 50, 33155): "runtime_ruins_eldan_data_storage_public_event_creature_activation_credit_tested_pending_route_visual_state_and_client_smoke",
            (161, 112, 33334): "runtime_ruins_war_supplies_public_event_creature_activation_credit_tested_pending_route_visual_state_and_client_smoke",
            (161, 234, 33768): "runtime_ruins_eldan_phase_monitor_public_event_creature_activation_credit_tested_pending_route_visual_state_and_client_smoke",
            (161, 235, 33302): "runtime_ruins_kel_voreth_forge_public_event_creature_activation_credit_tested_pending_route_visual_state_and_client_smoke",
            (161, 108, 32531): "runtime_ruins_forgemaster_trogun_public_event_creature_spawn_and_death_credit_tested_pending_mechanics_rewards_and_client_smoke",
        }

        for key, expected_status in expected_status_by_key.items():
            with self.subTest(key=key):
                override = audit.PUBLIC_EVENT_CREATURE_EVIDENCE_OVERRIDES[key]
                self.assertEqual(expected_status, override["evidence_status"])
                self.assertIn("Ruins of Kel Voreth", override["blocker_summary"])
                self.assertIn("no longer double-counted", override["blocker_summary"])
                self.assertIn(key, audit.PUBLIC_EVENT_CREATURE_BLOCKER_EXEMPTIONS)

        still_blocked_keys = {
            (161, 46, 27548),
            (161, 48, 37113),
            (161, 49, 32547),
            (161, 560, 32533),
            (161, 232, 33764),
            (161, 1758, 59545),
        }
        for key in still_blocked_keys:
            with self.subTest(still_blocked_key=key):
                self.assertNotIn(key, audit.PUBLIC_EVENT_CREATURE_EVIDENCE_OVERRIDES)
                self.assertNotIn(key, audit.PUBLIC_EVENT_CREATURE_BLOCKER_EXEMPTIONS)

    def test_hycrest_insurrection_opening_overrides_track_intro_and_parent_objectives(self) -> None:
        report_override = audit.SCRIPT_OBJECTIVE_PRODUCER_OVERRIDES[
            (
                r"Source\NexusForever.Script.Instance\Adventure\TheHycrestInsurrection\TheHycrestInsurrectionIntroEventScript.cs",
                "2113",
                "7183",
            )
        ]
        self.assertEqual("418;2113;7183;18365", report_override["script_owner_ids"])
        self.assertEqual("script_owner_matches_public_event", report_override["owner_link_status"])
        self.assertEqual(
            "runtime_hycrest_intro_report_dropship_activation_tested",
            report_override["evidence_status"],
        )
        self.assertEqual("automated_focus_pass", report_override["manual_validation"])
        self.assertIn("MatchingGameMap 57", report_override["blocker_summary"])
        self.assertIn("world 1149", report_override["blocker_summary"])

        briefing_override = audit.SCRIPT_OBJECTIVE_PRODUCER_OVERRIDES[
            (
                r"Source\NexusForever.Script.Instance\Adventure\TheHycrestInsurrection\TheHycrestInsurrectionIntroEventScript.cs",
                "2155",
                "7183",
            )
        ]
        self.assertEqual(
            "runtime_hycrest_intro_dropship_briefing_script_credit_tested_pending_dialog_timing_smoke",
            briefing_override["evidence_status"],
        )

        meet_agent_override = audit.SCRIPT_OBJECTIVE_PRODUCER_OVERRIDES[
            (
                r"Source\NexusForever.Script.Instance\Adventure\TheHycrestInsurrection\TheHycrestInsurrectionIntroEventScript.cs",
                "189",
                "1994",
            )
        ]
        self.assertIn("Exiles' agent", meet_agent_override["blocker_summary"])

        parent_override = audit.SCRIPT_OBJECTIVE_PRODUCER_OVERRIDES[
            (
                r"Source\NexusForever.Script.Instance\Adventure\TheHycrestInsurrection\TheHycrestInsurrectionEventScript.cs",
                "1708",
                "1994",
            )
        ]
        self.assertIn("sub-events 420-434", parent_override["blocker_summary"])
        self.assertIn("The Hycrest Insurrection parent event", parent_override["blocker_summary"])

    def test_riot_in_the_void_opening_overrides_track_intro_and_parent_objectives(self) -> None:
        report_override = audit.SCRIPT_OBJECTIVE_PRODUCER_OVERRIDES[
            (
                r"Source\NexusForever.Script.Instance\Adventure\RiotInTheVoid\RiotInTheVoidIntroEventScript.cs",
                "2073",
                "7069",
            )
        ]
        self.assertEqual("178;2073;7069;27535", report_override["script_owner_ids"])
        self.assertEqual("script_owner_matches_public_event", report_override["owner_link_status"])
        self.assertEqual(
            "runtime_riot_intro_agent_triphon_report_activation_tested",
            report_override["evidence_status"],
        )
        self.assertEqual("automated_focus_pass", report_override["manual_validation"])
        self.assertIn("MatchingGameMap 58", report_override["blocker_summary"])
        self.assertIn("Dominion Only", report_override["blocker_summary"])

        mission_briefing = audit.SCRIPT_OBJECTIVE_PRODUCER_OVERRIDES[
            (
                r"Source\NexusForever.Script.Instance\Adventure\RiotInTheVoid\RiotInTheVoidIntroEventScript.cs",
                "610",
                "7069",
            )
        ]
        self.assertEqual(
            "runtime_riot_intro_mission_briefing_script_credit_tested_pending_dialog_timing_smoke",
            mission_briefing["evidence_status"],
        )

        holostation = audit.SCRIPT_OBJECTIVE_PRODUCER_OVERRIDES[
            (
                r"Source\NexusForever.Script.Instance\Adventure\RiotInTheVoid\RiotInTheVoidIntroEventScript.cs",
                "2074",
                "7071",
            )
        ]
        self.assertEqual(
            "runtime_riot_intro_warden_holostation_activation_tested",
            holostation["evidence_status"],
        )
        self.assertEqual("automated_focus_pass", holostation["manual_validation"])
        self.assertIn("Creature2 49990", holostation["blocker_summary"])

        movement_gate = audit.SCRIPT_OBJECTIVE_PRODUCER_OVERRIDES[
            (
                r"Source\NexusForever.Script.Instance\Adventure\RiotInTheVoid\RiotInTheVoidIntroEventScript.cs",
                "608",
                "3383",
            )
        ]
        self.assertIn("Warden's Office", movement_gate["blocker_summary"])

        parent_override = audit.SCRIPT_OBJECTIVE_PRODUCER_OVERRIDES[
            (
                r"Source\NexusForever.Script.Instance\Adventure\RiotInTheVoid\RiotInTheVoidEventScript.cs",
                "1714",
                "3346",
            )
        ]
        self.assertIn("public-event vote routing", parent_override["blocker_summary"])
        self.assertIn("PE 403-417", parent_override["blocker_summary"])

    def test_bay_of_betrayal_opening_overrides_track_intro_and_parent_objectives(self) -> None:
        talk_override = audit.SCRIPT_OBJECTIVE_PRODUCER_OVERRIDES[
            (
                r"Source\NexusForever.Script.Instance\Adventure\BayOfBetrayal\BayOfBetrayalIntroEventScript.cs",
                "4357",
                "12392",
            )
        ]
        self.assertEqual("672;4357;12392;68376", talk_override["script_owner_ids"])
        self.assertEqual("script_owner_matches_public_event", talk_override["owner_link_status"])
        self.assertEqual(
            "runtime_bay_betrayal_intro_herald_talk_activation_tested",
            talk_override["evidence_status"],
        )
        self.assertEqual("automated_focus_pass", talk_override["manual_validation"])
        self.assertIn("MatchingGameMap 86", talk_override["blocker_summary"])
        self.assertIn("world 3176", talk_override["blocker_summary"])
        self.assertIn("Creature2 68376", talk_override["blocker_summary"])

        speech_override = audit.SCRIPT_OBJECTIVE_PRODUCER_OVERRIDES[
            (
                r"Source\NexusForever.Script.Instance\Adventure\BayOfBetrayal\BayOfBetrayalIntroEventScript.cs",
                "4563",
                "12392",
            )
        ]
        self.assertEqual(
            "runtime_bay_betrayal_intro_herald_speech_script_credit_tested_pending_dialog_timing_smoke",
            speech_override["evidence_status"],
        )

        protect_override = audit.SCRIPT_OBJECTIVE_PRODUCER_OVERRIDES[
            (
                r"Source\NexusForever.Script.Instance\Adventure\BayOfBetrayal\BayOfBetrayalIntroEventScript.cs",
                "4564",
                "12392",
            )
        ]
        self.assertIn("Protect Herald Anku'mar", protect_override["blocker_summary"])

        parent_override = audit.SCRIPT_OBJECTIVE_PRODUCER_OVERRIDES[
            (
                r"Source\NexusForever.Script.Instance\Adventure\BayOfBetrayal\BayOfBetrayalEventScript.cs",
                "4358",
                "7845",
            )
        ]
        self.assertIn("Survive the Trials", parent_override["blocker_summary"])
        self.assertIn("PE 674 Trial of Will", parent_override["blocker_summary"])
        self.assertIn("technopathy/race/medal mechanics", parent_override["blocker_summary"])

    def test_protogames_final_leg_overrides_track_runtime_slice(self) -> None:
        super_dependency = audit.INSTANCE_OBJECTIVE_DEPENDENCY_OVERRIDES[(3173, 4499)]
        self.assertIn("ProtogamesAcademyFinalBossEntityScripts.cs", super_dependency["script_path"])
        self.assertIn("68096", super_dependency["script_owner_ids"])
        self.assertIn("creature_spawn_map.csv", super_dependency["evidence_sources"])

        last_event_trigger = audit.INSTANCE_OBJECTIVE_DEPENDENCY_OVERRIDES[(3173, 4500)]
        self.assertIn("48846;7939", last_event_trigger["script_owner_ids"])
        self.assertIn("world_location_client_map.csv", last_event_trigger["evidence_sources"])

        phineas_trigger = audit.INSTANCE_OBJECTIVE_DEPENDENCY_OVERRIDES[(3173, 4345)]
        self.assertIn("48850;7940", phineas_trigger["script_owner_ids"])

        wrathbone_dependency = audit.INSTANCE_OBJECTIVE_DEPENDENCY_OVERRIDES[(3173, 4346)]
        self.assertIn("67944", wrathbone_dependency["script_owner_ids"])
        self.assertIn("creature_quest_map.csv", wrathbone_dependency["evidence_sources"])

        producer_override = audit.SCRIPT_OBJECTIVE_PRODUCER_OVERRIDES[
            (
                r"Source\NexusForever.Script.Instance\Dungeon\ProtogamesAcademy\Script\ProtogamesAcademyFinalBossEntityScripts.cs",
                "4499",
                "12361",
            )
        ]
        self.assertEqual("script_owner_entity_filter_mapped_to_public_event", producer_override["owner_link_status"])
        self.assertIn("Super-Invulnotron", producer_override["blocker_summary"])
        self.assertIn("final Protogames leg", audit.SCRIPTED_INSTANCE_BLOCKERS[3173])

    def test_stormtalon_high_priest_overrides_clear_tested_trigger_rows(self) -> None:
        blocker_tokens = ("pending", "blocked", "mapped_only", "reviewed", "wip")
        dependency = audit.INSTANCE_OBJECTIVE_DEPENDENCY_OVERRIDES[(382, 540)]
        self.assertFalse(any(token in dependency["evidence_status"] for token in blocker_tokens))
        self.assertEqual("automated_focus_pass", dependency["manual_validation"])

        producer_keys = (
            (
                r"Source\NexusForever.Script.Instance\Dungeon\StormtalonsLair\StormtalonsLairEventScript.cs",
                "540",
                "1831",
            ),
            (
                r"Source\NexusForever.Script.Instance\Dungeon\StormtalonsLair\Script\StopTheThundercallHighPriestGridTriggerEntityScript.cs",
                "540",
                "1831",
            ),
        )
        for key in producer_keys:
            producer = audit.SCRIPT_OBJECTIVE_PRODUCER_OVERRIDES[key]
            self.assertFalse(any(token in producer["evidence_status"] for token in blocker_tokens))
            self.assertEqual("automated_focus_pass", producer["manual_validation"])

    def test_stormtalon_invokers_holocrypt_overrides_track_runtime_slice(self) -> None:
        cage_dependency = audit.INSTANCE_OBJECTIVE_DEPENDENCY_OVERRIDES[(382, 464)]
        self.assertIn("StormtalonOptionalObjectiveEntityScripts.cs", cage_dependency["script_path"])
        self.assertIn("3241;3242;3243;3244;20323", cage_dependency["script_owner_ids"])
        self.assertIn("1100038201", cage_dependency["script_owner_ids"])
        self.assertIn("jabbithole.coordinates", cage_dependency["evidence_sources"])
        self.assertIn("world_entity_candidate.csv", cage_dependency["evidence_sources"])

        cage_spawn_override = audit.SCRIPT_OBJECTIVE_PRODUCER_OVERRIDES[
            (
                r"Source\NexusForever.Script.Instance\Dungeon\StormtalonsLair\StormtalonsLairEventScript.cs",
                "464",
                "1661",
            )
        ]
        self.assertEqual("script_owner_matches_public_event", cage_spawn_override["owner_link_status"])
        self.assertIn("source_coordinate_ids 3241", cage_spawn_override["blocker_summary"])
        self.assertIn("five SimpleCollidable producers", cage_spawn_override["blocker_summary"])

        cage_credit_override = audit.SCRIPT_OBJECTIVE_PRODUCER_OVERRIDES[
            (
                r"Source\NexusForever.Script.Instance\Dungeon\StormtalonsLair\Script\StormtalonOptionalObjectiveEntityScripts.cs",
                "464",
                "1661",
            )
        ]
        self.assertEqual("script_owner_entity_filter_mapped_to_public_event", cage_credit_override["owner_link_status"])
        self.assertIn("SimpleCollidable owner path", cage_credit_override["blocker_summary"])

        flower_dependency = audit.INSTANCE_OBJECTIVE_DEPENDENCY_OVERRIDES[(382, 554)]
        self.assertIn("StormtalonsLairEventScript.cs", flower_dependency["script_path"])
        self.assertIn("StormtalonOptionalObjectiveEntityScripts.cs", flower_dependency["script_path"])
        self.assertIn("5195326", flower_dependency["script_owner_ids"])
        self.assertIn("5224306", flower_dependency["script_owner_ids"])
        self.assertIn("1100038210", flower_dependency["script_owner_ids"])
        self.assertIn("1100038241", flower_dependency["script_owner_ids"])
        self.assertIn("jabbithole.coordinates", flower_dependency["evidence_sources"])
        self.assertIn("world_entity_candidate.csv", flower_dependency["evidence_sources"])
        self.assertEqual(
            "runtime_stormtalon_tainted_flower_stem_jabbithole_placements_and_checklist_credit_tested_pending_route_visual_state_and_client_smoke",
            flower_dependency["evidence_status"],
        )

        flower_spawn_override = audit.SCRIPT_OBJECTIVE_PRODUCER_OVERRIDES[
            (
                r"Source\NexusForever.Script.Instance\Dungeon\StormtalonsLair\StormtalonsLairEventScript.cs",
                "554",
                "2548",
            )
        ]
        self.assertEqual("script_owner_matches_public_event", flower_spawn_override["owner_link_status"])
        self.assertIn("source creature 26518", flower_spawn_override["blocker_summary"])
        self.assertIn("32 unique-name Simple rows", flower_spawn_override["blocker_summary"])

        flower_credit_override = audit.SCRIPT_OBJECTIVE_PRODUCER_OVERRIDES[
            (
                r"Source\NexusForever.Script.Instance\Dungeon\StormtalonsLair\Script\StormtalonOptionalObjectiveEntityScripts.cs",
                "554",
                "2548",
            )
        ]
        self.assertEqual("script_owner_entity_filter_mapped_to_public_event", flower_credit_override["owner_link_status"])
        self.assertIn("TaintedFlowerStemEntityScript", flower_credit_override["blocker_summary"])

        dependency = audit.INSTANCE_OBJECTIVE_DEPENDENCY_OVERRIDES[(382, 4867)]
        self.assertIn("StormtalonOptionalObjectiveEntityScripts.cs", dependency["script_path"])
        self.assertIn("27244;20740;2975", dependency["script_owner_ids"])
        self.assertIn("world_location_client_map.csv", dependency["evidence_sources"])

        event_override = audit.SCRIPT_OBJECTIVE_PRODUCER_OVERRIDES[
            (
                r"Source\NexusForever.Script.Instance\Dungeon\StormtalonsLair\StormtalonsLairEventScript.cs",
                "4867",
                "",
            )
        ]
        self.assertEqual("script_owner_matches_public_event", event_override["owner_link_status"])
        self.assertIn("Invoker's Holocrypt", event_override["blocker_summary"])

        entity_override = audit.SCRIPT_OBJECTIVE_PRODUCER_OVERRIDES[
            (
                r"Source\NexusForever.Script.Instance\Dungeon\StormtalonsLair\Script\StormtalonOptionalObjectiveEntityScripts.cs",
                "4867",
                "",
            )
        ]
        self.assertEqual("script_owner_entity_filter_mapped_to_public_event", entity_override["owner_link_status"])
        self.assertIn("ImprovementConstructionPlatformEntityScript", entity_override["blocker_summary"])
        self.assertIn("1100038242", entity_override["script_owner_ids"])

        data_altar_dependency = audit.INSTANCE_OBJECTIVE_DEPENDENCY_OVERRIDES[(382, 555)]
        self.assertIn("824;3201;1100038243", data_altar_dependency["script_owner_ids"])
        self.assertIn("jabbithole.coordinates", data_altar_dependency["evidence_sources"])
        self.assertIn("world_entity_candidate.csv", data_altar_dependency["evidence_sources"])

        data_altar_spawn_override = audit.SCRIPT_OBJECTIVE_PRODUCER_OVERRIDES[
            (
                r"Source\NexusForever.Script.Instance\Dungeon\StormtalonsLair\StormtalonsLairEventScript.cs",
                "555",
                "2978",
            )
        ]
        self.assertEqual("script_owner_matches_public_event", data_altar_spawn_override["owner_link_status"])
        self.assertIn("source_coordinate_id 3201", data_altar_spawn_override["blocker_summary"])
        self.assertIn("1100038243", data_altar_spawn_override["blocker_summary"])

        launch_pad_dependency = audit.INSTANCE_OBJECTIVE_DEPENDENCY_OVERRIDES[(382, 559)]
        self.assertIn("1046;4296;4297", launch_pad_dependency["script_owner_ids"])
        self.assertIn("1100038252;1100038253", launch_pad_dependency["script_owner_ids"])
        self.assertIn("jabbithole.coordinates", launch_pad_dependency["evidence_sources"])

        launch_pad_spawn_override = audit.SCRIPT_OBJECTIVE_PRODUCER_OVERRIDES[
            (
                r"Source\NexusForever.Script.Instance\Dungeon\StormtalonsLair\StormtalonsLairEventScript.cs",
                "559",
                "3679",
            )
        ]
        self.assertIn("source_coordinate_ids 4296 and 4297", launch_pad_spawn_override["blocker_summary"])
        self.assertIn("1100038252 and 1100038253", launch_pad_spawn_override["blocker_summary"])

        storm_totem_dependency = audit.INSTANCE_OBJECTIVE_DEPENDENCY_OVERRIDES[(382, 562)]
        self.assertIn("5689;5690;5691;5692;5693;22995;22996;22997", storm_totem_dependency["script_owner_ids"])
        self.assertIn("1100038244", storm_totem_dependency["script_owner_ids"])
        self.assertIn("world_entity_stats_candidate.csv", storm_totem_dependency["evidence_sources"])

        storm_totem_spawn_override = audit.SCRIPT_OBJECTIVE_PRODUCER_OVERRIDES[
            (
                r"Source\NexusForever.Script.Instance\Dungeon\StormtalonsLair\StormtalonsLairEventScript.cs",
                "562",
                "4419",
            )
        ]
        self.assertIn("source_coordinate_ids 5689-5693 and 22995-22997", storm_totem_spawn_override["blocker_summary"])
        self.assertIn("1100038244-1100038251", storm_totem_spawn_override["blocker_summary"])
        self.assertIn("Invoker's Holocrypt 4867", audit.SCRIPTED_INSTANCE_BLOCKERS[382])
        self.assertIn("Launch Pads 559 reviewed placements", audit.SCRIPTED_INSTANCE_BLOCKERS[382])

    def test_stormtalon_remaining_script_bonus_rows_are_concrete_blockers(self) -> None:
        expected_statuses = {
            842: "mapped_stormtalon_deathless_bonus_blocked_missing_party_death_fail_state_reward_producer_client_smoke",
            844: "mapped_stormtalon_aethros_tornado_bonus_blocked_missing_tornado_hit_tracking_producer_client_smoke",
            1692: "mapped_stormtalon_blank_talkto_6522_blocked_missing_targetgroup_identity_repeat_semantics_client_smoke",
            2380: "mapped_stormtalon_stormchaser_challenge_blocked_missing_lightning_strike_channeler_tracking_producer_client_smoke",
            2381: "mapped_stormtalon_swift_sweeper_aethros_challenge_blocked_missing_gust_lifetime_tracking_producer_client_smoke",
            2382: "mapped_stormtalon_lightning_reflexes_challenge_blocked_missing_lightning_strike_hit_tracking_producer_client_smoke",
            2487: "mapped_stormtalon_swift_sweeper_gust_timer_blocked_missing_gust_spawn_timer_producer_client_smoke",
            3201: "mapped_stormtalon_40_minute_medal_bonus_blocked_missing_instance_timer_medal_reward_producer_client_smoke",
            5177: "mapped_stormtalon_rookie_reward_blocked_missing_rookie_detection_reward_producer_client_smoke",
            5343: "mapped_stormtalon_kill_enemies_object_mismatch_blocked_targetgroup_14476_collision_client_smoke",
        }

        for objective_id, expected_status in expected_statuses.items():
            with self.subTest(objective_id=objective_id):
                dependency = audit.INSTANCE_OBJECTIVE_DEPENDENCY_OVERRIDES[(382, objective_id)]
                self.assertEqual(expected_status, dependency["evidence_status"])
                self.assertNotEqual(
                    "mapped_public_event_objective_blocked_missing_runtime_producer_evidence",
                    dependency["evidence_status"],
                )
                self.assertIn("public_event_objective_client_map.csv", dependency["evidence_sources"])
                self.assertIn("smoke", dependency["blocker_summary"])

        blank_talk = audit.INSTANCE_OBJECTIVE_DEPENDENCY_OVERRIDES[(382, 1692)]
        self.assertEqual("145;1692;6522;47063;47062", blank_talk["script_owner_ids"])
        self.assertIn("TargetGroup members 47063 and 47062", blank_talk["blocker_summary"])

        collision = audit.INSTANCE_OBJECTIVE_DEPENDENCY_OVERRIDES[(382, 5343)]
        self.assertIn("Liquid Soulfrost sample container", collision["blocker_summary"])
        self.assertIn("Residual script/bonus rows 842", audit.SCRIPTED_INSTANCE_BLOCKERS[382])

    def test_sanctuary_turnstile_route_overrides_track_existing_triggers(self) -> None:
        cases = [
            (
                613,
                3419,
                22139,
                r"Source\NexusForever.Script.Instance\Dungeon\SanctuaryOfTheSwordmaiden\Script\LifeweaverTerraceGridTriggerEntityScript.cs",
                "runtime_sanctuary_lifeweaver_terrace_turnstile_activation_and_trigger_credit_tested_pending_trigger_placement_route_client_smoke",
                "runtime_sanctuary_lifeweaver_terrace_turnstile_phase_activation_tested",
                "runtime_sanctuary_lifeweaver_terrace_turnstile_trigger_credit_tested",
            ),
            (
                614,
                3420,
                22140,
                r"Source\NexusForever.Script.Instance\Dungeon\SanctuaryOfTheSwordmaiden\Script\MoldwoodCorruptionGridTriggerScript.cs",
                "runtime_sanctuary_moldwood_corruption_turnstile_activation_and_trigger_credit_tested_pending_trigger_placement_route_client_smoke",
                "runtime_sanctuary_moldwood_corruption_turnstile_phase_activation_tested",
                "runtime_sanctuary_moldwood_corruption_turnstile_trigger_credit_tested",
            ),
            (
                615,
                3421,
                22141,
                r"Source\NexusForever.Script.Instance\Dungeon\SanctuaryOfTheSwordmaiden\Script\TheTempleOfTheLifeSpeakerGridTriggerScript.cs",
                "runtime_sanctuary_temple_life_speaker_turnstile_activation_and_trigger_credit_tested_pending_trigger_placement_route_client_smoke",
                "runtime_sanctuary_temple_life_speaker_turnstile_phase_activation_tested",
                "runtime_sanctuary_temple_life_speaker_turnstile_trigger_credit_tested",
            ),
        ]

        event_script = r"Source\NexusForever.Script.Instance\Dungeon\SanctuaryOfTheSwordmaiden\SanctuaryOfTheSwordmaidenEventScript.cs"
        for objective_id, object_id, world_location_id, trigger_script, dependency_status, event_status, trigger_status in cases:
            with self.subTest(objective_id=objective_id):
                dependency = audit.INSTANCE_OBJECTIVE_DEPENDENCY_OVERRIDES[(1271, objective_id)]
                self.assertIn("SanctuaryOfTheSwordmaidenEventScript.cs", dependency["script_path"])
                self.assertIn(Path(trigger_script).name, dependency["script_path"])
                self.assertEqual(f"166;{objective_id};{object_id};{world_location_id}", dependency["script_owner_ids"])
                self.assertEqual(dependency_status, dependency["evidence_status"])
                self.assertIn("world_location_client_map.csv", dependency["evidence_sources"])

                event_producer = audit.SCRIPT_OBJECTIVE_PRODUCER_OVERRIDES[(event_script, str(objective_id), str(object_id))]
                self.assertEqual("script_owner_matches_public_event", event_producer["owner_link_status"])
                self.assertEqual(event_status, event_producer["evidence_status"])
                self.assertNotRegex(event_producer["evidence_status"], "pending|blocked|wip|mapped_only|reviewed")
                self.assertIn(f"WorldLocation2 {world_location_id}", event_producer["blocker_summary"])

                trigger_producer = audit.SCRIPT_OBJECTIVE_PRODUCER_OVERRIDES[(trigger_script, str(objective_id), str(object_id))]
                self.assertEqual("script_owner_entity_filter_mapped_to_public_event", trigger_producer["owner_link_status"])
                self.assertEqual(trigger_status, trigger_producer["evidence_status"])
                self.assertNotRegex(trigger_producer["evidence_status"], "pending|blocked|wip|mapped_only|reviewed")
                self.assertIn(f"owner-id filtered to {object_id}", trigger_producer["blocker_summary"])

    def test_sanctuary_event_script_activation_overrides_track_existing_phase_tests(self) -> None:
        cases = [
            (479, 3208, "DefeatDeadringerShallaos", "Enter phase", "runtime_sanctuary_deadringer_shallaos_boss_phase_activation_tested"),
            (495, 3372, "EliminateZealousTorine", "Enter phase", "runtime_sanctuary_zealous_torine_opening_phase_activation_tested"),
            (481, 3193, "CollectTorineSpiritRelics", "RandomPath phase", "runtime_sanctuary_torine_spirit_relic_phase_activation_tested"),
            (1366, 5316, "DefeatHammerfistMoldjaw", "TempleOfTheLifeSpeaker phase", "runtime_sanctuary_hammerfist_moldjaw_miniboss_phase_activation_tested"),
            (493, 3237, "DefeatCorruptedEdgesmithTorian", "MoldwoodCorruption phase", "runtime_sanctuary_corrupted_edgesmith_torian_miniboss_phase_activation_tested"),
            (486, 3210, "DefeatRaynaDarkspeaker", "RaynaDarkspeaker phase", "runtime_sanctuary_rayna_darkspeaker_boss_phase_activation_tested"),
            (499, 5756, "DestroyTorineTotemsOfFlame", "RaynaDarkspeaker phase", "runtime_sanctuary_torine_totem_flame_phase_activation_tested"),
            (483, 3209, "DefeatMoldwoodOverlordSkash", "MoldwoodOverlordSkash phase", "runtime_sanctuary_moldwood_overlord_skash_boss_phase_activation_tested"),
            (1504, 3209, "DefeatSkashOrHeWillCorruptThePrisoner", "MoldwoodOverlordSkash phase", "runtime_sanctuary_skash_prisoner_timer_phase_activation_tested"),
            (639, 3417, "DestroyTheElderMoldwoodRavager", "MoldwoodOverlordSkash phase", "runtime_sanctuary_elder_moldwood_ravager_miniboss_phase_activation_tested"),
            (494, 3238, "KillCorruptedLifecallerKhalee", "GoToLifeWeaverTerracePath2 phase", "runtime_sanctuary_corrupted_lifecaller_khalee_miniboss_phase_activation_tested"),
            (480, 3207, "DefeatOnduLifeweaver", "OnduLifeWeaver phase", "runtime_sanctuary_ondu_lifeweaver_boss_phase_activation_tested"),
            (629, 3420, "KillTheLifeweaverGuardian", "OnduLifeWeaver phase", "runtime_sanctuary_lifeweaver_guardian_miniboss_phase_activation_tested"),
            (482, 3164, "PlaceTheSpiritRelics", "PlaceSpiritRelics phase", "runtime_sanctuary_spirit_relic_holder_phase_activation_tested"),
            (492, 3211, "DefeatSpiritmotherSeleneTheCorrupted", "SpiritmotherSeleneTheCorrupted phase", "runtime_sanctuary_spiritmother_selene_corrupted_boss_phase_activation_tested"),
            (496, 5755, "SabotageLifeweaverTechClusters", "Enter-phase optional objective", "runtime_sanctuary_lifeweaver_tech_cluster_optional_phase_activation_tested"),
            (500, 3239, "KillCorruptedDeathbringerDareia", "Enter-phase optional objective", "runtime_sanctuary_corrupted_deathbringer_dareia_optional_phase_activation_tested"),
        ]

        event_script = r"Source\NexusForever.Script.Instance\Dungeon\SanctuaryOfTheSwordmaiden\SanctuaryOfTheSwordmaidenEventScript.cs"
        for objective_id, object_id, symbol, phase_fragment, expected_status in cases:
            with self.subTest(objective_id=objective_id):
                event_producer = audit.SCRIPT_OBJECTIVE_PRODUCER_OVERRIDES[(event_script, str(objective_id), str(object_id))]
                self.assertEqual(f"166;{objective_id};{object_id}", event_producer["script_owner_ids"])
                self.assertEqual("script_owner_matches_public_event", event_producer["owner_link_status"])
                self.assertEqual(expected_status, event_producer["evidence_status"])
                self.assertNotRegex(event_producer["evidence_status"], "pending|blocked|wip|mapped_only|reviewed")
                self.assertIn("SanctuaryOfTheSwordmaidenEventScript.cs", event_producer["evidence_sources"])
                self.assertIn("SanctuaryOfTheSwordmaidenEventScriptTests.cs", event_producer["evidence_sources"])
                self.assertIn(symbol, event_producer["blocker_summary"])
                self.assertIn(phase_fragment, event_producer["blocker_summary"])
                self.assertIn("Retail completion still needs", event_producer["blocker_summary"])

        flame_demon_key = (event_script, "504", "3421")
        self.assertNotIn(flame_demon_key, audit.SCRIPT_OBJECTIVE_PRODUCER_OVERRIDES)

    def test_sanctuary_optional_script_objective_producer_overrides_track_credit_scripts(self) -> None:
        event_script = r"Source\NexusForever.Script.Instance\Dungeon\SanctuaryOfTheSwordmaiden\SanctuaryOfTheSwordmaidenEventScript.cs"
        boss_script = r"Source\NexusForever.Script.Instance\Dungeon\SanctuaryOfTheSwordmaiden\Script\SanctuaryBossEntityScripts.cs"
        objective_script = r"Source\NexusForever.Script.Instance\Dungeon\SanctuaryOfTheSwordmaiden\Script\SanctuaryObjectiveEntityScripts.cs"

        cases = [
            (
                event_script,
                "627",
                "3391",
                "166;627;3391;29311;29310;72988;72999",
                "script_owner_matches_public_event",
                "runtime_sanctuary_distracted_moldwood_mauler_optional_phase_activation_tested",
                "KillDistractedMoldwoodMaulers",
            ),
            (
                boss_script,
                "627",
                "3391",
                "166;627;3391;29311;29310;72988;72999",
                "script_owner_entity_filter_mapped_to_public_event",
                "runtime_sanctuary_distracted_moldwood_mauler_script_objective_death_credit_tested",
                "DistractedMoldwoodMaulerEntityScript",
            ),
            (
                event_script,
                "497",
                "",
                "166;497;70947",
                "script_owner_matches_public_event",
                "runtime_sanctuary_soul_spore_optional_phase_activation_tested",
                "UseTheSoulSporeOnMoldwoodGorgers",
            ),
            (
                objective_script,
                "497",
                "",
                "166;497;70947",
                "script_owner_entity_filter_mapped_to_public_event",
                "runtime_sanctuary_soul_spore_script_objective_activation_credit_tested",
                "SoulSporeEntityScript",
            ),
        ]

        for script_path, objective_id, object_id, owner_ids, owner_status, evidence_status, summary_fragment in cases:
            with self.subTest(script_path=Path(script_path).name, objective_id=objective_id):
                producer = audit.SCRIPT_OBJECTIVE_PRODUCER_OVERRIDES[(script_path, objective_id, object_id)]
                self.assertEqual(owner_ids, producer["script_owner_ids"])
                self.assertEqual(owner_status, producer["owner_link_status"])
                self.assertEqual(evidence_status, producer["evidence_status"])
                self.assertNotRegex(producer["evidence_status"], "pending|blocked|wip|mapped_only|reviewed")
                self.assertIn("SanctuaryOfTheSwordmaidenEventScriptTests.cs", producer["evidence_sources"])
                self.assertIn(summary_fragment, producer["blocker_summary"])
                self.assertIn("Retail completion still needs", producer["blocker_summary"])

    def test_skullcano_public_event_creature_overrides_cover_reviewed_script_owned_rows_only(self) -> None:
        expected_statuses = {
            (148, 289, 24898): "runtime_skullcano_tugga_public_event_creature_death_credit_tested_pending_failure_condition_rewards_and_client_smoke",
            (148, 352, 24493): "runtime_skullcano_tugga_public_event_creature_death_credit_tested_pending_failure_condition_rewards_and_client_smoke",
            (148, 290, 24893): "runtime_skullcano_thunderfoot_public_event_creature_death_credit_tested_pending_boss_mechanics_rewards_and_client_smoke",
            (148, 348, 24475): "runtime_skullcano_thunderfoot_public_event_creature_death_credit_tested_pending_boss_mechanics_rewards_and_client_smoke",
            (148, 291, 24679): "runtime_skullcano_grim_grim_sack_public_event_creature_checklist_credit_tested_pending_placement_visual_state_and_client_smoke",
            (148, 339, 24675): "runtime_skullcano_gold_treasure_public_event_creature_checklist_credit_tested_pending_route_placement_visual_state_and_client_smoke",
            (148, 1450, 24675): "runtime_skullcano_gold_treasure_public_event_creature_checklist_credit_tested_pending_route_placement_visual_state_and_client_smoke",
            (148, 336, 25230): "runtime_skullcano_missile_panel_public_event_creature_checklist_credit_tested_pending_placement_visual_state_and_client_smoke",
            (148, 331, 24894): "runtime_skullcano_bosun_octog_public_event_creature_death_credit_tested_pending_boss_mechanics_rewards_and_client_smoke",
            (148, 361, 24486): "runtime_skullcano_bosun_octog_public_event_creature_death_credit_tested_pending_boss_mechanics_rewards_and_client_smoke",
            (148, 1875, 24896): "runtime_skullcano_quartermaster_gruhar_public_event_creature_death_credit_tested_pending_route_boss_mechanics_rewards_and_client_smoke",
            (148, 1895, 24490): "runtime_skullcano_quartermaster_gruhar_public_event_creature_death_credit_tested_pending_route_boss_mechanics_rewards_and_client_smoke",
            (148, 1944, 24490): "runtime_skullcano_quartermaster_gruhar_public_event_creature_death_credit_tested_pending_route_boss_mechanics_rewards_and_client_smoke",
            (148, 1946, 24896): "runtime_skullcano_quartermaster_gruhar_public_event_creature_death_credit_tested_pending_route_boss_mechanics_rewards_and_client_smoke",
            (148, 332, 24895): "runtime_skullcano_mordechai_redmoon_public_event_creature_death_credit_tested_pending_boss_mechanics_rewards_and_client_smoke",
            (148, 340, 24895): "runtime_skullcano_mordechai_redmoon_public_event_creature_death_credit_tested_pending_boss_mechanics_rewards_and_client_smoke",
            (148, 355, 24489): "runtime_skullcano_mordechai_redmoon_public_event_creature_death_credit_tested_pending_boss_mechanics_rewards_and_client_smoke",
            (148, 819, 24489): "runtime_skullcano_mordechai_redmoon_public_event_creature_death_credit_tested_pending_boss_mechanics_rewards_and_client_smoke",
        }

        for key, expected_status in expected_statuses.items():
            with self.subTest(key=key):
                override = audit.PUBLIC_EVENT_CREATURE_EVIDENCE_OVERRIDES[key]
                self.assertEqual(expected_status, override["evidence_status"])
                self.assertIn("Skullcano", override["blocker_summary"])
                self.assertIn("no longer double-counted", override["blocker_summary"])
                self.assertIn("retail completion still needs", override["blocker_summary"])
                self.assertIn(key, audit.PUBLIC_EVENT_CREATURE_BLOCKER_EXEMPTIONS)

        still_blocked_keys = {
            (148, 325, 24788),
            (148, 439, 24680),
            (148, 1180, 24680),
            (148, 1443, 24788),
            (148, 1619, 24680),
            (148, 327, 0),
            (148, 349, 0),
            (148, 1477, 0),
            (148, 1657, 0),
        }
        for key in still_blocked_keys:
            with self.subTest(still_blocked_key=key):
                self.assertNotIn(key, audit.PUBLIC_EVENT_CREATURE_EVIDENCE_OVERRIDES)
                self.assertNotIn(key, audit.PUBLIC_EVENT_CREATURE_BLOCKER_EXEMPTIONS)

    def test_skullcano_redmoon_marauder_public_event_creature_overrides_track_nested_target_group(self) -> None:
        redmoon_relation_keys = {
            (148, 324, 24920),
            (148, 326, 24920),
            (148, 328, 37752),
            (148, 329, 24916),
            (148, 330, 24914),
            (148, 333, 24919),
            (148, 334, 24919),
            (148, 335, 37752),
            (148, 337, 24908),
            (148, 338, 24914),
            (148, 341, 24916),
            (148, 347, 24618),
            (148, 350, 24581),
            (148, 351, 24580),
            (148, 353, 24514),
            (148, 354, 24581),
            (148, 356, 24580),
            (148, 357, 24579),
            (148, 358, 24579),
            (148, 359, 37751),
            (148, 360, 24618),
            (148, 362, 24578),
            (148, 363, 25561),
            (148, 364, 24512),
            (148, 365, 37751),
            (148, 366, 24510),
            (148, 408, 24911),
            (148, 409, 24910),
            (148, 456, 25562),
            (148, 457, 24913),
            (148, 1130, 25561),
            (148, 1182, 24578),
            (148, 1425, 24512),
            (148, 1513, 24510),
            (148, 1593, 24514),
            (148, 1647, 24908),
            (148, 1654, 24913),
            (148, 1655, 25562),
            (148, 1661, 24911),
            (148, 1662, 24913),
            (148, 1664, 24910),
        }

        for key in redmoon_relation_keys:
            with self.subTest(key=key):
                override = audit.PUBLIC_EVENT_CREATURE_EVIDENCE_OVERRIDES[key]
                self.assertEqual(
                    "runtime_skullcano_redmoon_marauder_public_event_creature_nested_target_group_credit_tested_pending_spawn_count_rewards_and_client_smoke",
                    override["evidence_status"],
                )
                self.assertIn("TargetGroup 3947", override["blocker_summary"])
                self.assertIn("every unique Creature2 member", override["blocker_summary"])
                self.assertIn("UnitEntityDamageResultTests.cs", override["evidence_sources"])
                self.assertIn(key, audit.PUBLIC_EVENT_CREATURE_BLOCKER_EXEMPTIONS)

    def test_skullcano_route_trigger_overrides_track_existing_triggers(self) -> None:
        cases = [
            (
                329,
                2821,
                18109,
                r"Source\NexusForever.Script.Instance\Dungeon\Skullcano\Script\ChasmGridTriggerEntityScript.cs",
                "runtime_skullcano_lava_chasm_turnstile_activation_and_trigger_credit_tested_pending_trigger_placement_route_client_smoke",
                "runtime_skullcano_lava_chasm_turnstile_phase_activation_tested_pending_trigger_placement_route_client_smoke",
                "runtime_skullcano_lava_chasm_turnstile_trigger_credit_tested_pending_trigger_placement_route_client_smoke",
                "PublicEventObjective.CrossTheLavaFilledChasm",
            ),
            (
                362,
                3953,
                18273,
                r"Source\NexusForever.Script.Instance\Dungeon\Skullcano\Script\FindChiefGridTriggerEntityScript.cs",
                "runtime_skullcano_find_chief_direct_trigger_activation_and_credit_tested_pending_trigger_owner_reconciliation_route_client_smoke",
                "runtime_skullcano_find_chief_phase_activation_tested_pending_trigger_owner_reconciliation_route_client_smoke",
                "runtime_skullcano_find_chief_direct_trigger_credit_tested_pending_trigger_owner_reconciliation_route_client_smoke",
                "PublicEventObjective.FindChiefKaskalak",
            ),
            (
                372,
                2909,
                18879,
                r"Source\NexusForever.Script.Instance\Dungeon\Skullcano\Script\PlatformTriggerGuidEntityScript.cs",
                "runtime_skullcano_redmoon_platform_gather_activation_and_trigger_credit_tested_pending_trigger_placement_route_client_smoke",
                "runtime_skullcano_redmoon_platform_gather_phase_activation_tested_pending_trigger_placement_route_client_smoke",
                "runtime_skullcano_redmoon_platform_gather_trigger_credit_tested_pending_trigger_placement_route_client_smoke",
                "PublicEventObjective.GatherOnThePlatform",
            ),
            (
                376,
                8218,
                18093,
                r"Source\NexusForever.Script.Instance\Dungeon\Skullcano\Script\TerraformerGridTriggerEntityScript.cs",
                "runtime_skullcano_terraformer_route_activation_and_trigger_credit_tested_pending_trigger_placement_route_client_smoke",
                "runtime_skullcano_terraformer_route_phase_activation_tested_pending_trigger_placement_route_client_smoke",
                "runtime_skullcano_terraformer_route_trigger_credit_tested_pending_trigger_placement_route_client_smoke",
                "",
            ),
        ]

        event_script = r"Source\NexusForever.Script.Instance\Dungeon\Skullcano\SkullcanoEventScript.cs"
        manual_rows_by_path = {
            str(row["source_path"]): row
            for row in audit.SCRIPT_OBJECTIVE_PRODUCER_MANUAL_DIRECT_OBJECTIVES
        }
        for objective_id, object_id, world_location_id, trigger_script, dependency_status, event_status, trigger_status, manual_symbol in cases:
            with self.subTest(objective_id=objective_id):
                dependency = audit.INSTANCE_OBJECTIVE_DEPENDENCY_OVERRIDES[(1263, objective_id)]
                self.assertIn("SkullcanoEventScript.cs", dependency["script_path"])
                self.assertIn(Path(trigger_script).name, dependency["script_path"])
                self.assertEqual(f"148;{objective_id};{object_id};{world_location_id}", dependency["script_owner_ids"])
                self.assertEqual(dependency_status, dependency["evidence_status"])
                self.assertIn("world_location_client_map.csv", dependency["evidence_sources"])

                event_producer = audit.SCRIPT_OBJECTIVE_PRODUCER_OVERRIDES[(event_script, str(objective_id), str(object_id))]
                self.assertEqual("script_owner_matches_public_event", event_producer["owner_link_status"])
                self.assertEqual(event_status, event_producer["evidence_status"])
                self.assertIn(f"WorldLocation2 {world_location_id}", event_producer["blocker_summary"])

                trigger_producer = audit.SCRIPT_OBJECTIVE_PRODUCER_OVERRIDES[(trigger_script, str(objective_id), str(object_id))]
                self.assertEqual("script_owner_entity_filter_mapped_to_public_event", trigger_producer["owner_link_status"])
                self.assertEqual(trigger_status, trigger_producer["evidence_status"])

                if manual_symbol:
                    manual_row = manual_rows_by_path[trigger_script]
                    self.assertEqual(objective_id, manual_row["objective_id"])
                    self.assertEqual(manual_symbol, manual_row["source_symbol"])

        self.assertIn("Find Chief direct trigger 362", audit.SCRIPTED_INSTANCE_BLOCKERS[1263])

    def test_skullcano_lava_core_overrides_track_existing_death_credit(self) -> None:
        dependency = audit.INSTANCE_OBJECTIVE_DEPENDENCY_OVERRIDES[(1263, 338)]
        self.assertIn("SkullcanoEventScript.cs", dependency["script_path"])
        self.assertIn("SkullcanoObjectiveEntityScripts.cs", dependency["script_path"])
        self.assertEqual("148;338;7766;24680;24921;18129", dependency["script_owner_ids"])
        self.assertEqual(
            "runtime_skullcano_gold_infused_lava_core_activation_and_death_credit_tested_pending_spawn_count_route_client_smoke",
            dependency["evidence_status"],
        )
        self.assertIn("client_source_targetgroup_map.csv", dependency["evidence_sources"])
        self.assertIn("Gold-Infused Lava Node death-credit producer 338", audit.SCRIPTED_INSTANCE_BLOCKERS[1263])

        event_producer = audit.SCRIPT_OBJECTIVE_PRODUCER_OVERRIDES[
            (
                r"Source\NexusForever.Script.Instance\Dungeon\Skullcano\SkullcanoEventScript.cs",
                "338",
                "7766",
            )
        ]
        self.assertEqual("script_owner_matches_public_event", event_producer["owner_link_status"])
        self.assertEqual(
            "runtime_skullcano_gold_infused_lava_core_phase_activation_tested_pending_spawn_count_route_client_smoke",
            event_producer["evidence_status"],
        )
        self.assertIn("TargetGroup 7766 contains Creature2 24680 and 24921", event_producer["blocker_summary"])

        death_credit_producer = audit.SCRIPT_OBJECTIVE_PRODUCER_OVERRIDES[
            (
                r"Source\NexusForever.Script.Instance\Dungeon\Skullcano\Script\SkullcanoObjectiveEntityScripts.cs",
                "338",
                "7766",
            )
        ]
        self.assertEqual("script_owner_entity_filter_mapped_to_public_event", death_credit_producer["owner_link_status"])
        self.assertEqual(
            "runtime_skullcano_gold_infused_lava_core_death_credit_tested_pending_spawn_count_route_client_smoke",
            death_credit_producer["evidence_status"],
        )
        self.assertIn("Creature2 24680 and 24921", death_credit_producer["blocker_summary"])

    def test_skullcano_escort_row_is_blocked_separately_from_chasm_trigger(self) -> None:
        dependency = audit.INSTANCE_OBJECTIVE_DEPENDENCY_OVERRIDES[(1263, 328)]

        self.assertEqual("", dependency["script_path"])
        self.assertEqual("148;328;2821;18110", dependency["script_owner_ids"])
        self.assertEqual(
            "mapped_skullcano_chief_kaskalak_escort_trigger_volume_blocked_shared_object_2821_pending_escort_choreography_client_smoke",
            dependency["evidence_status"],
        )
        self.assertIn("ChasmGridTriggerEntityScript.cs", dependency["evidence_sources"])
        self.assertIn("same object 2821 is used by chasm turnstile objective 329", dependency["blocker_summary"])
        self.assertIn("intentionally credits 329 directly and does not satisfy 328", dependency["blocker_summary"])
        self.assertIn("Escort objective 328 and remaining script/bonus rows", audit.SCRIPTED_INSTANCE_BLOCKERS[1263])

    def test_skullcano_chasm_station_and_chief_talk_overrides_track_existing_producers(self) -> None:
        cases = [
            (
                364,
                2673,
                "148;364;2673;25168",
                "runtime_skullcano_molten_chasm_monitoring_station_activation_credit_tested_pending_station_placement_route_client_smoke",
                "runtime_skullcano_molten_chasm_monitoring_station_phase_activation_tested_pending_station_placement_route_client_smoke",
                "runtime_skullcano_molten_chasm_monitoring_station_activation_credit_tested_pending_station_placement_route_client_smoke",
                "TargetGroup 2673 contains Creature2 25168",
            ),
            (
                820,
                3944,
                "148;820;3944;33452;24788;18273",
                "runtime_skullcano_chief_kaskalak_talk_activation_and_credit_tested_pending_escort_choreography_client_smoke",
                "runtime_skullcano_chief_kaskalak_talk_phase_activation_tested_pending_escort_choreography_client_smoke",
                "runtime_skullcano_chief_kaskalak_talk_credit_tested_pending_escort_choreography_client_smoke",
                "TargetGroup 3944 contains Chief Kaskalak Creature2 rows 33452 and 24788",
            ),
        ]

        event_script = r"Source\NexusForever.Script.Instance\Dungeon\Skullcano\SkullcanoEventScript.cs"
        entity_script = r"Source\NexusForever.Script.Instance\Dungeon\Skullcano\Script\SkullcanoObjectiveEntityScripts.cs"
        for objective_id, object_id, owner_ids, dependency_status, event_status, entity_status, event_summary_text in cases:
            with self.subTest(objective_id=objective_id):
                dependency = audit.INSTANCE_OBJECTIVE_DEPENDENCY_OVERRIDES[(1263, objective_id)]
                self.assertIn("SkullcanoEventScript.cs", dependency["script_path"])
                self.assertIn("SkullcanoObjectiveEntityScripts.cs", dependency["script_path"])
                self.assertEqual(owner_ids, dependency["script_owner_ids"])
                self.assertEqual(dependency_status, dependency["evidence_status"])
                self.assertIn("client_source_targetgroup_map.csv", dependency["evidence_sources"])

                event_producer = audit.SCRIPT_OBJECTIVE_PRODUCER_OVERRIDES[(event_script, str(objective_id), str(object_id))]
                self.assertEqual("script_owner_matches_public_event", event_producer["owner_link_status"])
                self.assertEqual(event_status, event_producer["evidence_status"])
                self.assertIn(event_summary_text, event_producer["blocker_summary"])

                entity_producer = audit.SCRIPT_OBJECTIVE_PRODUCER_OVERRIDES[(entity_script, str(objective_id), str(object_id))]
                self.assertEqual("script_owner_entity_filter_mapped_to_public_event", entity_producer["owner_link_status"])
                self.assertEqual(entity_status, entity_producer["evidence_status"])

        self.assertIn("Molten Chasm Monitoring Station producer 364", audit.SCRIPTED_INSTANCE_BLOCKERS[1263])
        self.assertIn("Chief Kaskalak TalkTo producer 820", audit.SCRIPTED_INSTANCE_BLOCKERS[1263])

    def test_skullcano_remaining_script_bonus_rows_are_concrete_blockers(self) -> None:
        expected_statuses = {
            336: "mapped_skullcano_redmoon_prisoner_jailor_script_blocked_missing_jailor_spawn_release_producer_client_smoke",
            337: "mapped_skullcano_primal_fire_essence_script_blocked_missing_essence_trigger_collection_producer_client_smoke",
            339: "mapped_skullcano_path_selector_339_blocked_missing_scriptee_owner_route_selector_producer_client_smoke",
            340: "mapped_skullcano_path_selector_340_blocked_missing_scriptee_owner_route_selector_producer_client_smoke",
            838: "mapped_skullcano_extreme_heat_bonus_blocked_missing_heat_aura_party_death_failure_producer_client_smoke",
            839: "mapped_skullcano_laveka_chasm_bonus_blocked_missing_laveka_hazard_hit_failure_producer_client_smoke",
            840: "mapped_skullcano_deathless_bonus_blocked_missing_party_death_fail_state_reward_producer_client_smoke",
            841: "mapped_skullcano_terraformer_timer_bonus_blocked_missing_mordechai_activation_timer_overload_producer_client_smoke",
            941: "mapped_skullcano_tugga_diet_challenge_blocked_missing_meat_eating_boss_mechanic_producer_client_smoke",
            942: "mapped_skullcano_thunderfoot_seismic_challenge_blocked_missing_seismic_tremor_hit_tracking_producer_client_smoke",
            943: "mapped_skullcano_bosun_broken_armor_challenge_blocked_missing_stack_tracking_death_condition_producer_client_smoke",
            944: "mapped_skullcano_mordechai_blind_challenge_blocked_missing_terraformer_overload_blind_tracking_producer_client_smoke",
            1989: "mapped_skullcano_blank_talkto_7028_blocked_missing_targetgroup_identity_repeat_semantics_client_smoke",
            3203: "mapped_skullcano_45_minute_medal_bonus_blocked_missing_instance_timer_medal_reward_producer_client_smoke",
            5179: "mapped_skullcano_rookie_reward_blocked_missing_rookie_detection_reward_producer_client_smoke",
            5345: "mapped_skullcano_kill_enemies_object_mismatch_blocked_targetgroup_14476_collision_client_smoke",
        }

        for objective_id, expected_status in expected_statuses.items():
            with self.subTest(objective_id=objective_id):
                dependency = audit.INSTANCE_OBJECTIVE_DEPENDENCY_OVERRIDES[(1263, objective_id)]
                self.assertEqual(expected_status, dependency["evidence_status"])
                self.assertNotEqual(
                    "mapped_public_event_objective_blocked_missing_runtime_producer_evidence",
                    dependency["evidence_status"],
                )
                self.assertIn("public_event_objective_client_map.csv", dependency["evidence_sources"])
                self.assertIn("smoke", dependency["blocker_summary"])

        collision = audit.INSTANCE_OBJECTIVE_DEPENDENCY_OVERRIDES[(1263, 5345)]
        self.assertIn("Liquid Soulfrost sample container", collision["blocker_summary"])
        self.assertIn("remaining script/bonus rows 336", audit.SCRIPTED_INSTANCE_BLOCKERS[1263])

    def test_crimelords_of_whitevale_parent_overrides_track_objective_spine(self) -> None:
        hoverbike_override = audit.SCRIPT_OBJECTIVE_PRODUCER_OVERRIDES[
            (
                r"Source\NexusForever.Script.Instance\Adventure\CrimelordsOfWhitevale\CrimelordsOfWhitevaleEventScript.cs",
                "309",
                "",
            )
        ]
        self.assertEqual("146;309", hoverbike_override["script_owner_ids"])
        self.assertEqual("script_owner_matches_public_event", hoverbike_override["owner_link_status"])
        self.assertIn("MatchingGameMap 25 and 51", hoverbike_override["blocker_summary"])
        self.assertIn("world 1323", hoverbike_override["blocker_summary"])

        biggest_gangsters = audit.SCRIPT_OBJECTIVE_PRODUCER_OVERRIDES[
            (
                r"Source\NexusForever.Script.Instance\Adventure\CrimelordsOfWhitevale\CrimelordsOfWhitevaleEventScript.cs",
                "318",
                "",
            )
        ]
        self.assertEqual(
            "runtime_crimelords_parent_biggest_gangsters_activation_tested_pending_loyalty_branch_smoke",
            biggest_gangsters["evidence_status"],
        )
        self.assertIn("PE 111-114", biggest_gangsters["blocker_summary"])

        final_override = audit.SCRIPT_OBJECTIVE_PRODUCER_OVERRIDES[
            (
                r"Source\NexusForever.Script.Instance\Adventure\CrimelordsOfWhitevale\CrimelordsOfWhitevaleEventScript.cs",
                "1484",
                "",
            )
        ]
        self.assertIn("Avenge the Blood Scions", final_override["blocker_summary"])
        self.assertIn("normal/veteran reward tables", final_override["blocker_summary"])

    def test_malgrave_trail_overrides_track_parent_and_exodus_opening_objectives(self) -> None:
        producer_override = audit.SCRIPT_OBJECTIVE_PRODUCER_OVERRIDES[
            (
                r"Source\NexusForever.Script.Instance\Adventure\TheMalgraveTrail\TheMalgraveTrailEventScript.cs",
                "66",
                "",
            )
        ]

        self.assertEqual("53;66", producer_override["script_owner_ids"])
        self.assertEqual("script_owner_matches_public_event", producer_override["owner_link_status"])
        self.assertEqual(
            "runtime_malgrave_parent_lead_caravan_activation_tested_pending_route_producers_smoke",
            producer_override["evidence_status"],
        )
        self.assertIn("MatchingGameMap 9 and 50", producer_override["blocker_summary"])
        self.assertIn("world 1181", producer_override["blocker_summary"])
        self.assertIn("Lead the caravan to Fort Westwatch safely", producer_override["blocker_summary"])
        self.assertIn("remaining Exodus PE 56", producer_override["blocker_summary"])
        self.assertIn("Abandoned Mine objectives 947/948", producer_override["blocker_summary"])
        self.assertIn("objectives 952/953", producer_override["blocker_summary"])

        rally_override = audit.SCRIPT_OBJECTIVE_PRODUCER_OVERRIDES[
            (
                r"Source\NexusForever.Script.Instance\Adventure\TheMalgraveTrail\TheMalgraveTrailExodusEventScript.cs",
                "69",
                "1951",
            )
        ]
        self.assertEqual("56;69;1951;170;171;41303", rally_override["script_owner_ids"])
        self.assertEqual("script_owner_matches_public_event", rally_override["owner_link_status"])
        self.assertEqual(
            "runtime_malgrave_exodus_town_center_survivor_rally_activation_tested_pending_targetgroup_dialog_smoke",
            rally_override["evidence_status"],
        )
        self.assertIn("TargetGroup 1951", rally_override["blocker_summary"])
        self.assertIn("WorldLocation2 41303", rally_override["blocker_summary"])

        thirsty_creek_override = audit.SCRIPT_OBJECTIVE_PRODUCER_OVERRIDES[
            (
                r"Source\NexusForever.Script.Instance\Adventure\TheMalgraveTrail\TheMalgraveTrailExodusEventScript.cs",
                "70",
                "3881",
            )
        ]
        self.assertEqual("56;70;3881;33173", thirsty_creek_override["script_owner_ids"])
        self.assertIn("Thirsty Creek", thirsty_creek_override["blocker_summary"])
        self.assertIn("Creature2 33173", thirsty_creek_override["blocker_summary"])

        kurg_override = audit.SCRIPT_OBJECTIVE_PRODUCER_OVERRIDES[
            (
                r"Source\NexusForever.Script.Instance\Adventure\TheMalgraveTrail\TheMalgraveTrailExodusEventScript.cs",
                "71",
                "3881",
            )
        ]
        self.assertEqual("56;71;3881;33173", kurg_override["script_owner_ids"])
        self.assertIn("near the kurg", kurg_override["blocker_summary"])

        story_override = audit.SCRIPT_OBJECTIVE_PRODUCER_OVERRIDES[
            (
                r"Source\NexusForever.Script.Instance\Adventure\TheMalgraveTrail\TheMalgraveTrailExodusEventScript.cs",
                "72",
                "2496",
            )
        ]
        self.assertEqual("56;72;2496;23639", story_override["script_owner_ids"])
        self.assertEqual(
            "runtime_malgrave_exodus_braithwait_story_timedwin_activation_tested_pending_timer_dialog_smoke",
            story_override["evidence_status"],
        )
        self.assertIn("TimedWin", story_override["blocker_summary"])
        self.assertIn("without direct-crediting", story_override["blocker_summary"])

    def test_siege_tempest_refuge_parent_overrides_track_mirrored_routes(self) -> None:
        exile_assault = audit.SCRIPT_OBJECTIVE_PRODUCER_OVERRIDES[
            (
                r"Source\NexusForever.Script.Instance\Adventure\TheSiegeOfTempestRefuge\TheSiegeOfTempestRefugeExileEventScript.cs",
                "564",
                "",
            )
        ]
        self.assertEqual("173;564", exile_assault["script_owner_ids"])
        self.assertEqual("script_owner_matches_public_event", exile_assault["owner_link_status"])
        self.assertIn("MatchingGameMap 52 and 53", exile_assault["blocker_summary"])
        self.assertIn("world 1233", exile_assault["blocker_summary"])
        self.assertIn("Defend against the Dominion assault", exile_assault["blocker_summary"])

        exile_generator = audit.SCRIPT_OBJECTIVE_PRODUCER_OVERRIDES[
            (
                r"Source\NexusForever.Script.Instance\Adventure\TheSiegeOfTempestRefuge\TheSiegeOfTempestRefugeExileEventScript.cs",
                "1701",
                "",
            )
        ]
        self.assertEqual(
            "runtime_siege_tempest_exile_generator_resourcepool_activation_tested_pending_damage_producers",
            exile_generator["evidence_status"],
        )
        self.assertIn("ResourcePool", exile_generator["blocker_summary"])

        exile_holdout = audit.SCRIPT_OBJECTIVE_PRODUCER_OVERRIDES[
            (
                r"Source\NexusForever.Script.Instance\Adventure\TheSiegeOfTempestRefuge\TheSiegeOfTempestRefugeExileEventScript.cs",
                "2204",
                "",
            )
        ]
        self.assertIn("TimedWin", exile_holdout["blocker_summary"])
        self.assertIn("finishes PE 173", exile_holdout["blocker_summary"])

        dominion_assault = audit.SCRIPT_OBJECTIVE_PRODUCER_OVERRIDES[
            (
                r"Source\NexusForever.Script.Instance\Adventure\TheSiegeOfTempestRefuge\TheSiegeOfTempestRefugeDominionEventScript.cs",
                "565",
                "",
            )
        ]
        self.assertEqual("174;565", dominion_assault["script_owner_ids"])
        self.assertIn("Defend against the Exile assault", dominion_assault["blocker_summary"])

        dominion_generator = audit.SCRIPT_OBJECTIVE_PRODUCER_OVERRIDES[
            (
                r"Source\NexusForever.Script.Instance\Adventure\TheSiegeOfTempestRefuge\TheSiegeOfTempestRefugeDominionEventScript.cs",
                "1702",
                "",
            )
        ]
        self.assertEqual(
            "runtime_siege_tempest_dominion_generator_resourcepool_activation_tested_pending_damage_producers",
            dominion_generator["evidence_status"],
        )
        self.assertIn("PE 173 and 174", dominion_generator["blocker_summary"])

        dominion_holdout = audit.SCRIPT_OBJECTIVE_PRODUCER_OVERRIDES[
            (
                r"Source\NexusForever.Script.Instance\Adventure\TheSiegeOfTempestRefuge\TheSiegeOfTempestRefugeDominionEventScript.cs",
                "2205",
                "",
            )
        ]
        self.assertIn("TimedWin", dominion_holdout["blocker_summary"])
        self.assertIn("finishes PE 174", dominion_holdout["blocker_summary"])

    def test_outpost_m13_exit_panel_overrides_track_asteroid_surface_objective(self) -> None:
        instance_override = audit.INSTANCE_OBJECTIVE_DEPENDENCY_OVERRIDES[(1319, 262)]
        self.assertIn("OutpostM13EventScript.cs", instance_override["script_path"])
        self.assertIn("ExitPanelEntityScript.cs", instance_override["script_path"])
        self.assertEqual("108;262;5294;23470;28830", instance_override["script_owner_ids"])
        self.assertEqual(
            "runtime_outpost_m13_exit_panel_activation_credit_tested_pending_teleport_visual_state_and_client_smoke",
            instance_override["evidence_status"],
        )

        producer_override = audit.SCRIPT_OBJECTIVE_PRODUCER_OVERRIDES[
            (
                r"Source\NexusForever.Script.Instance\Expedition\OutpostM13\Script\ExitPanelEntityScript.cs",
                "262",
                "5294",
            )
        ]
        self.assertEqual("script_owner_entity_filter_mapped_to_public_event", producer_override["owner_link_status"])
        self.assertIn("Creature2 23470 Exit Panel", producer_override["blocker_summary"])
        self.assertIn("WorldLocation2 28830", producer_override["blocker_summary"])
        self.assertIn("shuttle teleport/destination behavior", audit.SCRIPTED_INSTANCE_BLOCKERS[1319])

    def test_outpost_m13_residual_rows_are_concrete(self) -> None:
        cases = [
            (
                252,
                "108;252;23113",
                "mapped_outpost_m13_accelerite_defense_blocked_missing_defense_wave_resource_state_client_smoke",
            ),
            (
                256,
                "108;256;10835;23536;23514;23500;69080;69079;69077;17235",
                "runtime_outpost_m13_hive_mine_cleanup_activation_death_credit_tested_pending_spawn_density_and_client_smoke",
            ),
            (
                257,
                "108;257;5318;23513;17235",
                "runtime_outpost_m13_hive_queen_activation_death_credit_tested_pending_boss_spawn_mechanics_and_client_smoke",
            ),
            (
                1439,
                "108;1439;2673;17282",
                "mapped_outpost_m13_search_mine_blocked_missing_trigger_completion_client_smoke",
            ),
            (
                1440,
                "108;1440;5295;29468;33042",
                "runtime_outpost_m13_foreman_krause_talk_activation_credit_tested_pending_placement_and_client_smoke",
            ),
            (
                1441,
                "108;1441;5308;41201;22152;1100300017",
                "runtime_outpost_m13_captain_milo_spawn_talk_activation_credit_tested_pending_dialog_route_and_client_smoke",
            ),
            (
                4696,
                "108;4696;113;49531",
                "mapped_outpost_m13_mining_charges_blocked_missing_object_113_charge_collect_producer_client_smoke",
            ),
            (
                4697,
                "108;4697;113;49536",
                "mapped_outpost_m13_bonus_marauders_blocked_missing_script_wave_credit_producer_client_smoke",
            ),
            (
                4702,
                "108;4702;1200000",
                "runtime_outpost_m13_gold_medal_timer_activation_and_final_credit_tested_pending_timer_reward_and_client_smoke",
            ),
        ]

        for objective_id, owner_ids, evidence_status in cases:
            with self.subTest(objective_id=objective_id):
                dependency = audit.INSTANCE_OBJECTIVE_DEPENDENCY_OVERRIDES[(1319, objective_id)]
                self.assertIn("OutpostM13", dependency["script_path"])
                self.assertEqual(owner_ids, dependency["script_owner_ids"])
                self.assertEqual(evidence_status, dependency["evidence_status"])

        producer_cases = [
            (
                r"Source\NexusForever.Script.Instance\Expedition\OutpostM13\OutpostM13EventScript.cs",
                "1439",
                "2673",
                "script_owner_matches_public_event",
                "mapped_outpost_m13_search_mine_activation_only_blocked_missing_trigger_completion_client_smoke",
            ),
            (
                r"Source\NexusForever.Script.Instance\Expedition\OutpostM13\OutpostM13EventScript.cs",
                "1440",
                "5295",
                "script_owner_matches_public_event",
                "runtime_outpost_m13_foreman_krause_talk_objective_activation_tested_pending_placement_and_client_smoke",
            ),
            (
                r"Source\NexusForever.Script.Instance\Expedition\OutpostM13\Script\ForemanKrauseEntityScript.cs",
                "1440",
                "5295",
                "script_owner_entity_filter_mapped_to_public_event",
                "runtime_outpost_m13_foreman_krause_talk_credit_tested_pending_placement_and_client_smoke",
            ),
            (
                r"Source\NexusForever.Script.Instance\Expedition\OutpostM13\OutpostM13EventScript.cs",
                "1441",
                "5308",
                "script_owner_matches_public_event",
                "runtime_outpost_m13_captain_milo_objective_activation_and_spawn_tested_pending_dialog_route_and_client_smoke",
            ),
            (
                r"Source\NexusForever.Script.Instance\Expedition\OutpostM13\Script\CaptainMiloEntityScript.cs",
                "1441;1444",
                "5308",
                "script_owner_entity_filter_mapped_to_public_event",
                "runtime_outpost_m13_captain_milo_talk_credit_tested_pending_dialog_route_and_client_smoke",
            ),
            (
                r"Source\NexusForever.Script.Instance\Expedition\OutpostM13\OutpostM13EventScript.cs",
                "256",
                "",
                "script_owner_matches_public_event",
                "runtime_outpost_m13_hive_mine_cleanup_objective_activation_tested_pending_spawn_density_and_client_smoke",
            ),
            (
                r"Source\NexusForever.Script.Instance\Expedition\OutpostM13\Script\HiveQueenEntityScript.cs",
                "256",
                "",
                "script_owner_entity_filter_mapped_to_public_event",
                "runtime_outpost_m13_hive_mine_cleanup_death_credit_tested_pending_spawn_density_and_client_smoke",
            ),
            (
                r"Source\NexusForever.Script.Instance\Expedition\OutpostM13\OutpostM13EventScript.cs",
                "257",
                "5318",
                "script_owner_matches_public_event",
                "runtime_outpost_m13_hive_queen_objective_activation_tested_pending_boss_spawn_mechanics_and_client_smoke",
            ),
            (
                r"Source\NexusForever.Script.Instance\Expedition\OutpostM13\Script\HiveQueenEntityScript.cs",
                "257",
                "5318",
                "script_owner_entity_filter_mapped_to_public_event",
                "runtime_outpost_m13_hive_queen_death_credit_tested_pending_boss_spawn_mechanics_and_client_smoke",
            ),
        ]

        for source_path, objective_id, object_id, owner_status, evidence_status in producer_cases:
            with self.subTest(source_path=source_path, objective_id=objective_id):
                producer = audit.SCRIPT_OBJECTIVE_PRODUCER_OVERRIDES[
                    (
                        source_path,
                        objective_id,
                        object_id,
                    )
                ]
                self.assertEqual(owner_status, producer["owner_link_status"])
                self.assertEqual(evidence_status, producer["evidence_status"])

        blocker = audit.SCRIPTED_INSTANCE_BLOCKERS[1319]
        self.assertIn("Captain Milo 1441, Foreman Krause 1440", blocker)
        self.assertIn("Accelerite defense 252", blocker)
        self.assertIn("Mining Charges 4696", blocker)

    def test_omnicore_opening_event_overrides_track_talk_listen_and_choose_path_gate(self) -> None:
        belle_talk = audit.INSTANCE_OBJECTIVE_DEPENDENCY_OVERRIDES[(3045, 2740)]
        self.assertIn("JourneyIntoOMNICore1EventScript.cs", belle_talk["script_path"])
        self.assertEqual("605;2740;10592;62807;2218", belle_talk["script_owner_ids"])
        self.assertEqual(
            "runtime_omnicore_opening_belle_talk_activation_tested_pending_dialog_credit_smoke",
            belle_talk["evidence_status"],
        )

        axis_talk = audit.INSTANCE_OBJECTIVE_DEPENDENCY_OVERRIDES[(3045, 2741)]
        self.assertEqual("605;2741;10593;62809;2218", axis_talk["script_owner_ids"])

        belle_listen = audit.INSTANCE_OBJECTIVE_DEPENDENCY_OVERRIDES[(3045, 2742)]
        self.assertEqual(
            "runtime_omnicore_opening_belle_listen_script_credit_tested_pending_dialog_timing_smoke",
            belle_listen["evidence_status"],
        )

        choose_path = audit.INSTANCE_OBJECTIVE_DEPENDENCY_OVERRIDES[(3045, 2744)]
        self.assertEqual(
            "runtime_omnicore_choose_path_gate_activation_tested_pending_route_selection_smoke",
            choose_path["evidence_status"],
        )

        producer_override = audit.SCRIPT_OBJECTIVE_PRODUCER_OVERRIDES[
            (
                r"Source\NexusForever.Script.Instance\WorldStory\JourneyIntoOMNICore1\JourneyIntoOMNICore1EventScript.cs",
                "2743",
                "",
            )
        ]
        self.assertEqual("script_owner_matches_public_event", producer_override["owner_link_status"])
        self.assertIn("Listen to Axis Pheydra", producer_override["blocker_summary"])
        self.assertIn("first choose-path gate", audit.SCRIPTED_INSTANCE_BLOCKERS[3045])

    def test_omnicore_downstream_residual_rows_are_concrete_mapped_only_blockers(self) -> None:
        self.assertEqual(115, len(audit.OMNICORE1_DOWNSTREAM_BLOCKED_OBJECTIVE_IDS))

        for objective_id in audit.OMNICORE1_DOWNSTREAM_BLOCKED_OBJECTIVE_IDS:
            override = audit.INSTANCE_OBJECTIVE_DEPENDENCY_OVERRIDES[(3045, objective_id)]
            self.assertIn("JourneyIntoOMNICore1EventScript.cs", override["script_path"])
            self.assertEqual(f"605;{objective_id}", override["script_owner_ids"])
            self.assertEqual(
                "mapped_omnicore1_downstream_objective_blocked_missing_route_specific_producer_client_smoke",
                override["evidence_status"],
            )
            self.assertIn("objective 2744", override["blocker_summary"])
            self.assertIn("route-specific producer", override["blocker_summary"])

    def test_ultimate_protogames_residual_rows_are_concrete_mapped_only_blockers(self) -> None:
        self.assertEqual(92, len(audit.ULTIMATE_PROTOGAMES_RESIDUAL_BLOCKED_OBJECTIVE_IDS))

        for objective_id in audit.ULTIMATE_PROTOGAMES_RESIDUAL_BLOCKED_OBJECTIVE_IDS:
            override = audit.INSTANCE_OBJECTIVE_DEPENDENCY_OVERRIDES[(2980, objective_id)]
            self.assertIn("UltimateProtogamesEventScript.cs", override["script_path"])
            self.assertEqual(f"594;{objective_id}", override["script_owner_ids"])
            self.assertEqual(
                "mapped_ultimate_protogames_residual_objective_blocked_missing_room_specific_mechanics_client_smoke",
                override["evidence_status"],
            )
            self.assertIn("room-specific", override["blocker_summary"])
            self.assertIn("full dungeon client smoke", override["blocker_summary"])

        blocker = audit.SCRIPTED_INSTANCE_BLOCKERS[2980]
        self.assertIn("residual challenge, score, teleporter, hidden-holder, rookie, and room-mechanics rows", blocker)

    def test_sanctuary_residual_rows_are_split_between_runtime_and_concrete_blockers(self) -> None:
        self.assertEqual(23, len(audit.SANCTUARY_RESIDUAL_OBJECTIVE_IDS))

        for objective_id in audit.SANCTUARY_RUNTIME_CREDIT_OBJECTIVE_IDS:
            override = audit.INSTANCE_OBJECTIVE_DEPENDENCY_OVERRIDES[(1271, objective_id)]
            self.assertIn("SanctuaryOfTheSwordmaidenEventScript.cs", override["script_path"])
            self.assertIn("Sanctuary", override["script_path"])
            self.assertEqual(audit.SANCTUARY_RUNTIME_CREDIT_OWNER_IDS[objective_id], override["script_owner_ids"])
            self.assertEqual(
                "runtime_sanctuary_optional_objective_credit_tested_pending_route_spawn_and_client_smoke",
                override["evidence_status"],
            )
            self.assertIn("Soul Spore or distracted Moldwood Mauler", override["blocker_summary"])

        for objective_id in audit.SANCTUARY_ACTIVATION_ONLY_OBJECTIVE_IDS:
            override = audit.INSTANCE_OBJECTIVE_DEPENDENCY_OVERRIDES[(1271, objective_id)]
            self.assertEqual(f"166;{objective_id}", override["script_owner_ids"])
            self.assertEqual(
                "mapped_sanctuary_activation_only_objective_blocked_missing_completion_producer_client_smoke",
                override["evidence_status"],
            )
            self.assertIn("completion or failure producer", override["blocker_summary"])

        side_event_objective = audit.INSTANCE_OBJECTIVE_DEPENDENCY_OVERRIDES[(1271, 678)]
        self.assertEqual("202;678", side_event_objective["script_owner_ids"])

        for objective_id in set(audit.SANCTUARY_RESIDUAL_OBJECTIVE_IDS) - set(audit.SANCTUARY_RUNTIME_CREDIT_OBJECTIVE_IDS) - set(audit.SANCTUARY_ACTIVATION_ONLY_OBJECTIVE_IDS):
            override = audit.INSTANCE_OBJECTIVE_DEPENDENCY_OVERRIDES[(1271, objective_id)]
            self.assertEqual(
                "mapped_sanctuary_residual_objective_blocked_missing_route_challenge_producer_client_smoke",
                override["evidence_status"],
            )
            self.assertIn("timer/deathless/rookie/challenge semantics", override["blocker_summary"])

        blocker = audit.SCRIPTED_INSTANCE_BLOCKERS[1271]
        self.assertIn("Soul Spore objective 497", blocker)
        self.assertIn("activation-only runtime scaffold rows", blocker)
        self.assertIn("residual side-event ritual", blocker)

    def test_deep_space_residual_rows_are_split_between_runtime_and_concrete_blockers(self) -> None:
        self.assertEqual(20, len(audit.DEEP_SPACE_RESIDUAL_OBJECTIVE_IDS))

        for objective_id in audit.DEEP_SPACE_RUNTIME_OBJECTIVE_IDS:
            override = audit.INSTANCE_OBJECTIVE_DEPENDENCY_OVERRIDES[(2188, objective_id)]
            self.assertIn("DeepSpaceExplorationEventScript.cs", override["script_path"])
            self.assertEqual(audit.DEEP_SPACE_RUNTIME_OWNER_IDS[objective_id], override["script_owner_ids"])
            self.assertEqual(
                "runtime_deepspace_opening_objective_credit_tested_pending_later_route_client_smoke",
                override["evidence_status"],
            )
            self.assertIn("duplicate guards", override["blocker_summary"])

        for objective_id in set(audit.DEEP_SPACE_RESIDUAL_OBJECTIVE_IDS) - set(audit.DEEP_SPACE_RUNTIME_OBJECTIVE_IDS):
            override = audit.INSTANCE_OBJECTIVE_DEPENDENCY_OVERRIDES[(2188, objective_id)]
            self.assertEqual(f"447;{objective_id}", override["script_owner_ids"])
            self.assertEqual(
                "mapped_deepspace_residual_objective_blocked_missing_later_route_producer_client_smoke",
                override["evidence_status"],
            )
            self.assertIn("rescue/Engineer Clamp/defense-control", override["blocker_summary"])

        blocker = audit.SCRIPTED_INSTANCE_BLOCKERS[2188]
        self.assertIn("Captain Tyrania 1844", blocker)
        self.assertIn("mainframe-cortex 1849", blocker)
        self.assertIn("Residual rescue", blocker)

    def test_protogames_academy_residual_rows_are_concrete_mapped_only_blockers(self) -> None:
        self.assertEqual(20, len(audit.PROTOGAMES_ACADEMY_RESIDUAL_OBJECTIVE_IDS))

        start_button = audit.INSTANCE_OBJECTIVE_DEPENDENCY_OVERRIDES[(3173, 4341)]
        self.assertIn("ProtogamesAcademyEventScript.cs", start_button["script_path"])
        self.assertEqual("667;4341", start_button["script_owner_ids"])
        self.assertEqual(
            "mapped_protogames_academy_start_button_activation_blocked_missing_credit_client_smoke",
            start_button["evidence_status"],
        )
        self.assertIn("start-button entity", start_button["blocker_summary"])

        for objective_id in set(audit.PROTOGAMES_ACADEMY_RESIDUAL_OBJECTIVE_IDS) - set(audit.PROTOGAMES_ACADEMY_ACTIVATION_ONLY_OBJECTIVE_IDS):
            override = audit.INSTANCE_OBJECTIVE_DEPENDENCY_OVERRIDES[(3173, objective_id)]
            self.assertEqual(f"667;{objective_id}", override["script_owner_ids"])
            self.assertEqual(
                "mapped_protogames_academy_residual_objective_blocked_missing_challenge_timer_producer_client_smoke",
                override["evidence_status"],
            )
            self.assertIn("deathless, boss-challenge, DNT talk, medal timer, rookie reward", override["blocker_summary"])

        blocker = audit.SCRIPTED_INSTANCE_BLOCKERS[3173]
        self.assertIn("Start button row 4341", blocker)
        self.assertIn("residual deathless, boss-challenge, DNT talk", blocker)

    def test_rage_logic_residual_rows_are_split_between_runtime_and_concrete_blockers(self) -> None:
        self.assertEqual(13, len(audit.RAGE_LOGIC_RESIDUAL_OBJECTIVE_IDS))

        for objective_id in audit.RAGE_LOGIC_RUNTIME_OBJECTIVE_IDS:
            override = audit.INSTANCE_OBJECTIVE_DEPENDENCY_OVERRIDES[(1627, objective_id)]
            self.assertIn("RageLogic", override["script_path"])
            self.assertEqual(audit.RAGE_LOGIC_RUNTIME_OWNER_IDS[objective_id], override["script_owner_ids"])
            self.assertEqual(
                "runtime_ragelogic_vehicle_asteroid_assault_credit_tested_pending_factory_route_client_smoke",
                override["evidence_status"],
            )
            self.assertIn("vehicle/asteroid objective", override["blocker_summary"])

        residual_ids = set(audit.RAGE_LOGIC_RESIDUAL_OBJECTIVE_IDS) - set(audit.RAGE_LOGIC_RUNTIME_OBJECTIVE_IDS)
        for objective_id in residual_ids:
            override = audit.INSTANCE_OBJECTIVE_DEPENDENCY_OVERRIDES[(1627, objective_id)]
            self.assertEqual(f"214;{objective_id}", override["script_owner_ids"])
            self.assertEqual(
                "mapped_ragelogic_residual_objective_blocked_missing_factory_teleporter_axiom_producer_client_smoke",
                override["evidence_status"],
            )
            self.assertIn("factory disable/freebot/teleporter/Axiom", override["blocker_summary"])

        blocker = audit.SCRIPTED_INSTANCE_BLOCKERS[1627]
        self.assertIn("vehicle-choice row 781", blocker)
        self.assertIn("Factory, Freebot, teleporter, Axiom", blocker)

    def test_kel_voreth_residual_rows_are_split_between_runtime_and_concrete_blockers(self) -> None:
        self.assertEqual(12, len(audit.KEL_VORETH_RESIDUAL_OBJECTIVE_IDS))

        trogun = audit.INSTANCE_OBJECTIVE_DEPENDENCY_OVERRIDES[(1336, 449)]
        self.assertIn("ForgemasterTrogunEntityScript.cs", trogun["script_path"])
        self.assertEqual("161;449;32531;1100300044;ForgemasterTrogunEntityScript", trogun["script_owner_ids"])
        self.assertEqual(
            "runtime_kelvoreth_forgemaster_trogun_activation_spawn_death_credit_tested_pending_mechanics_client_smoke",
            trogun["evidence_status"],
        )
        self.assertIn("reviewed script-owned Trogun anchor", trogun["blocker_summary"])

        challenge = audit.INSTANCE_OBJECTIVE_DEPENDENCY_OVERRIDES[(1336, 847)]
        self.assertEqual(
            "mapped_kelvoreth_activation_only_objective_blocked_missing_failure_credit_client_smoke",
            challenge["evidence_status"],
        )
        self.assertIn("completion or failure producer", challenge["blocker_summary"])

        residual_ids = set(audit.KEL_VORETH_RESIDUAL_OBJECTIVE_IDS) - set(audit.KEL_VORETH_RUNTIME_OBJECTIVE_IDS) - set(audit.KEL_VORETH_ACTIVATION_ONLY_OBJECTIVE_IDS)
        for objective_id in residual_ids:
            override = audit.INSTANCE_OBJECTIVE_DEPENDENCY_OVERRIDES[(1336, objective_id)]
            self.assertEqual(f"161;{objective_id}", override["script_owner_ids"])
            self.assertEqual(
                "mapped_kelvoreth_residual_objective_blocked_missing_challenge_timer_producer_client_smoke",
                override["evidence_status"],
            )
            self.assertIn("health-station, hidden jump-puzzle", override["blocker_summary"])

        blocker = audit.SCRIPTED_INSTANCE_BLOCKERS[1336]
        self.assertIn("Forgemaster Trogun 449", blocker)
        self.assertIn("Drokk exo-defense challenge row 847", blocker)

    def test_gauntlet_residual_rows_are_split_between_runtime_and_concrete_blockers(self) -> None:
        self.assertEqual(10, len(audit.GAUNTLET_RESIDUAL_OBJECTIVE_IDS))

        for objective_id in audit.GAUNTLET_RUNTIME_OBJECTIVE_IDS:
            override = audit.INSTANCE_OBJECTIVE_DEPENDENCY_OVERRIDES[(2183, objective_id)]
            self.assertIn("GauntletEventScript.cs", override["script_path"])
            self.assertEqual(audit.GAUNTLET_RUNTIME_OWNER_IDS[objective_id], override["script_owner_ids"])
            self.assertEqual(
                "runtime_gauntlet_opening_and_first_arena_credit_tested_pending_arena_route_client_smoke",
                override["evidence_status"],
            )
            self.assertIn("first-arena objective", override["blocker_summary"])

        for objective_id in audit.GAUNTLET_ACTIVATION_ONLY_OBJECTIVE_IDS:
            override = audit.INSTANCE_OBJECTIVE_DEPENDENCY_OVERRIDES[(2183, objective_id)]
            self.assertEqual(f"446;{objective_id}", override["script_owner_ids"])
            self.assertEqual(
                "mapped_gauntlet_activation_only_objective_blocked_missing_completion_score_client_smoke",
                override["evidence_status"],
            )
            self.assertIn("completion/score producer", override["blocker_summary"])

        residual_ids = set(audit.GAUNTLET_RESIDUAL_OBJECTIVE_IDS) - set(audit.GAUNTLET_RUNTIME_OBJECTIVE_IDS) - set(audit.GAUNTLET_ACTIVATION_ONLY_OBJECTIVE_IDS)
        for objective_id in residual_ids:
            override = audit.INSTANCE_OBJECTIVE_DEPENDENCY_OVERRIDES[(2183, objective_id)]
            self.assertEqual(f"446;{objective_id}", override["script_owner_ids"])
            self.assertEqual(
                "mapped_gauntlet_residual_objective_blocked_missing_arena_score_timer_producer_client_smoke",
                override["evidence_status"],
            )
            self.assertIn("second/third-arena completion", override["blocker_summary"])

        blocker = audit.SCRIPTED_INSTANCE_BLOCKERS[2183]
        self.assertIn("FindOutWhatHappened ScriptWithoutCount direct objective-id credit 1819", blocker)
        self.assertIn("Survive-the-Main-Event 1875", blocker)

    def test_coldblood_residual_rows_are_split_between_runtime_and_concrete_blockers(self) -> None:
        self.assertEqual(10, len(audit.COLDBLOOD_RESIDUAL_OBJECTIVE_IDS))

        for objective_id in audit.COLDBLOOD_RUNTIME_OBJECTIVE_IDS:
            override = audit.INSTANCE_OBJECTIVE_DEPENDENCY_OVERRIDES[(3522, objective_id)]
            self.assertIn("ColdbloodCitadelEventScript.cs", override["script_path"])
            self.assertIn("ColdbloodBossObjectiveEntityScripts.cs", override["script_path"])
            self.assertEqual(audit.COLDBLOOD_RUNTIME_OWNER_IDS[objective_id], override["script_owner_ids"])
            self.assertEqual(
                "runtime_coldblood_coven_harizog_activation_death_credit_tested_pending_mechanics_client_smoke",
                override["evidence_status"],
            )
            self.assertIn("Coven or Harizog death objective", override["blocker_summary"])

        for objective_id in audit.COLDBLOOD_ACTIVATION_ONLY_OBJECTIVE_IDS:
            override = audit.INSTANCE_OBJECTIVE_DEPENDENCY_OVERRIDES[(3522, objective_id)]
            self.assertEqual(f"907;{objective_id}", override["script_owner_ids"])
            self.assertEqual(
                "mapped_coldblood_activation_only_objective_blocked_missing_completion_failure_client_smoke",
                override["evidence_status"],
            )
            self.assertIn("completion or failure producer", override["blocker_summary"])

        residual_ids = set(audit.COLDBLOOD_RESIDUAL_OBJECTIVE_IDS) - set(audit.COLDBLOOD_RUNTIME_OBJECTIVE_IDS) - set(audit.COLDBLOOD_ACTIVATION_ONLY_OBJECTIVE_IDS)
        for objective_id in residual_ids:
            override = audit.INSTANCE_OBJECTIVE_DEPENDENCY_OVERRIDES[(3522, objective_id)]
            self.assertEqual(f"907;{objective_id}", override["script_owner_ids"])
            self.assertEqual(
                "mapped_coldblood_residual_objective_blocked_missing_challenge_timer_producer_client_smoke",
                override["evidence_status"],
            )
            self.assertIn("timer, deathless, Pell Protection Plan", override["blocker_summary"])

        blocker = audit.SCRIPTED_INSTANCE_BLOCKERS[3522]
        self.assertIn("Iceblood Coven 5314 and Risen Harizog 5315", blocker)
        self.assertIn("Rescue the Pell Architect 5324", blocker)

    def test_datascape_residual_rows_are_split_between_runtime_and_concrete_blockers(self) -> None:
        self.assertEqual(83, len(audit.DATASCAPE_RESIDUAL_OBJECTIVE_IDS))

        for objective_id in audit.DATASCAPE_OPENING_CREDIT_OBJECTIVE_IDS:
            override = audit.INSTANCE_OBJECTIVE_DEPENDENCY_OVERRIDES[(1333, objective_id)]
            self.assertIn("DatascapeEventScript.cs", override["script_path"])
            self.assertIn("DatascapeObjectiveCreditEntityScripts.cs", override["script_path"])
            self.assertEqual(f"157;{objective_id}", override["script_owner_ids"])
            self.assertEqual(
                "runtime_datascape_opening_activation_death_credit_tested_pending_encounter_mechanics_client_smoke",
                override["evidence_status"],
            )

        for objective_id in audit.DATASCAPE_ROUTE_ACTIVATION_BLOCKED_OBJECTIVE_IDS:
            override = audit.INSTANCE_OBJECTIVE_DEPENDENCY_OVERRIDES[(1333, objective_id)]
            self.assertEqual(
                "mapped_datascape_route_activation_blocked_missing_objective_completion_producer_client_smoke",
                override["evidence_status"],
            )
            self.assertIn("completion producer", override["blocker_summary"])

        for objective_id in set(audit.DATASCAPE_RESIDUAL_OBJECTIVE_IDS) - set(audit.DATASCAPE_OPENING_CREDIT_OBJECTIVE_IDS) - set(audit.DATASCAPE_ROUTE_ACTIVATION_BLOCKED_OBJECTIVE_IDS):
            override = audit.INSTANCE_OBJECTIVE_DEPENDENCY_OVERRIDES[(1333, objective_id)]
            self.assertEqual(
                "mapped_datascape_residual_objective_blocked_missing_raid_mechanics_client_smoke",
                override["evidence_status"],
            )

        blocker = audit.SCRIPTED_INSTANCE_BLOCKERS[1333]
        self.assertIn("385/648/649/650", blocker)
        self.assertIn("Route activation/checklist rows", blocker)

    def test_shades_eve_residual_rows_are_split_between_runtime_and_concrete_blockers(self) -> None:
        self.assertEqual(64, len(audit.SHADES_EVE_RESIDUAL_OBJECTIVE_IDS))

        for objective_id in audit.SHADES_EVE_RUNTIME_OBJECTIVE_IDS:
            override = audit.INSTANCE_OBJECTIVE_DEPENDENCY_OVERRIDES[(3044, objective_id)]
            self.assertIn("ShadesEveMainEventScript.cs", override["script_path"])
            self.assertEqual(f"597;{objective_id}", override["script_owner_ids"])
            self.assertEqual(
                "runtime_shades_eve_opening_activation_spawn_tested_pending_objective_credit_client_smoke",
                override["evidence_status"],
            )

        talk_locals = audit.INSTANCE_OBJECTIVE_DEPENDENCY_OVERRIDES[(3044, 2726)]
        self.assertEqual(
            "mapped_shades_eve_talk_locals_activation_blocked_missing_talk_credit_vote_followup_client_smoke",
            talk_locals["evidence_status"],
        )
        self.assertIn("vote 64", talk_locals["blocker_summary"])

        side_objective = audit.INSTANCE_OBJECTIVE_DEPENDENCY_OVERRIDES[(3044, 3060)]
        self.assertEqual("632;3060", side_objective["script_owner_ids"])

        for objective_id in set(audit.SHADES_EVE_RESIDUAL_OBJECTIVE_IDS) - set(audit.SHADES_EVE_RUNTIME_OBJECTIVE_IDS) - set(audit.SHADES_EVE_ROUTE_ACTIVATION_BLOCKED_OBJECTIVE_IDS):
            override = audit.INSTANCE_OBJECTIVE_DEPENDENCY_OVERRIDES[(3044, objective_id)]
            self.assertEqual(
                "mapped_shades_eve_residual_objective_blocked_missing_event_route_producer_client_smoke",
                override["evidence_status"],
            )

        blocker = audit.SCRIPTED_INSTANCE_BLOCKERS[3044]
        self.assertIn("fountain objective 2705", blocker)
        self.assertIn("TalkWithTheLocals 2726", blocker)

    def test_supermall_residual_rows_are_concrete_mapped_only_blockers(self) -> None:
        self.assertEqual(40, len(audit.SUPERMALL_RESIDUAL_OBJECTIVE_IDS))

        for objective_id in audit.SUPERMALL_RESIDUAL_OBJECTIVE_IDS:
            override = audit.INSTANCE_OBJECTIVE_DEPENDENCY_OVERRIDES[(3094, objective_id)]
            self.assertIn("ProtostarsSuperMallInTheSkyEventScript.cs", override["script_path"])
            self.assertEqual(f"679;{objective_id}", override["script_owner_ids"])
            self.assertEqual(
                "mapped_supermall_residual_objective_blocked_missing_store_route_producer_client_smoke",
                override["evidence_status"],
            )
            self.assertIn("RandomPath", override["blocker_summary"])

        blocker = audit.SCRIPTED_INSTANCE_BLOCKERS[3094]
        self.assertIn("40 downstream store-route", blocker)

    def test_genetic_archives_residual_rows_are_concrete_mapped_only_blockers(self) -> None:
        self.assertEqual(36, len(audit.GENETIC_ARCHIVES_RESIDUAL_OBJECTIVE_IDS))

        for objective_id in audit.GENETIC_ARCHIVES_ROUTE_ACTIVATION_BLOCKED_OBJECTIVE_IDS:
            override = audit.INSTANCE_OBJECTIVE_DEPENDENCY_OVERRIDES[(1462, objective_id)]
            self.assertIn("GeneticArchivesEventScript.cs", override["script_path"])
            self.assertEqual(f"159;{objective_id}", override["script_owner_ids"])
            self.assertEqual(
                "mapped_genetic_archives_route_activation_blocked_missing_objective_completion_producer_client_smoke",
                override["evidence_status"],
            )

        for objective_id in set(audit.GENETIC_ARCHIVES_RESIDUAL_OBJECTIVE_IDS) - set(audit.GENETIC_ARCHIVES_ROUTE_ACTIVATION_BLOCKED_OBJECTIVE_IDS):
            override = audit.INSTANCE_OBJECTIVE_DEPENDENCY_OVERRIDES[(1462, objective_id)]
            self.assertEqual(
                "mapped_genetic_archives_residual_objective_blocked_missing_raid_mechanics_client_smoke",
                override["evidence_status"],
            )
            self.assertIn("power-core state", override["blocker_summary"])

        blocker = audit.SCRIPTED_INSTANCE_BLOCKERS[1462]
        self.assertIn("Route activation rows 415/416/2452/2453/2454/2493", blocker)

    def test_redmoon_terror_residual_rows_are_split_between_runtime_and_concrete_blockers(self) -> None:
        self.assertEqual(24, len(audit.REDMOON_TERROR_RESIDUAL_OBJECTIVE_IDS))

        for objective_id in audit.REDMOON_TERROR_RUNTIME_OBJECTIVE_IDS:
            override = audit.INSTANCE_OBJECTIVE_DEPENDENCY_OVERRIDES[(3032, objective_id)]
            self.assertIn("RedMoonTerrorEventScript.cs", override["script_path"])
            self.assertEqual(f"705;{objective_id}", override["script_owner_ids"])
            self.assertEqual(
                "runtime_redmoonterror_starmap_laveka_activation_death_credit_tested_pending_mechanics_client_smoke",
                override["evidence_status"],
            )

        shaft = audit.INSTANCE_OBJECTIVE_DEPENDENCY_OVERRIDES[(3032, 4604)]
        self.assertEqual(
            "mapped_redmoonterror_route_activation_blocked_missing_objective_completion_producer_client_smoke",
            shaft["evidence_status"],
        )

        for objective_id in set(audit.REDMOON_TERROR_RESIDUAL_OBJECTIVE_IDS) - set(audit.REDMOON_TERROR_RUNTIME_OBJECTIVE_IDS) - set(audit.REDMOON_TERROR_ROUTE_ACTIVATION_BLOCKED_OBJECTIVE_IDS):
            override = audit.INSTANCE_OBJECTIVE_DEPENDENCY_OVERRIDES[(3032, objective_id)]
            self.assertEqual(
                "mapped_redmoonterror_residual_objective_blocked_missing_raid_mechanics_client_smoke",
                override["evidence_status"],
            )

        blocker = audit.SCRIPTED_INSTANCE_BLOCKERS[3032]
        self.assertIn("Starmap/Laveka objectives 4620/4621", blocker)

    def test_next_slice_queue_blocker_detail_annotation_summarizes_blocker_csv(self) -> None:
        queue_rows = [
            {"queue_rank": 1, "slice_id": "quest-2", "lane": "quest", "owner_paths": r"Source\Quest.cs"},
            {"queue_rank": 2, "slice_id": "instance-9", "lane": "instance", "owner_paths": r"Source\Instance.cs"},
        ]
        blocker_rows = [
            {
                "slice_id": "quest-2",
                "blocker_rank": 2,
                "blocker_category": "quest_creature",
                "blocker_id": "quest-2:quest_creature:quest_id=2;relation_type=starter",
            },
            {
                "slice_id": "quest-2",
                "blocker_rank": 1,
                "blocker_category": "quest_reward",
                "blocker_id": "quest-2:quest_reward:quest_id=2;reward_id=300",
            },
            {
                "slice_id": "instance-9",
                "blocker_rank": 1,
                "blocker_category": "instance_dependency",
                "blocker_id": "instance-9:instance_dependency:world_id=9;dependency_id=10",
            },
        ]

        annotated_rows = audit.annotate_next_slice_queue_blocker_details(queue_rows, blocker_rows)

        quest_row = annotated_rows[0]
        self.assertEqual(2, quest_row["blocker_detail_count"])
        self.assertEqual("quest_creature=1;quest_reward=1", quest_row["blocker_count_by_category"])
        self.assertEqual("slice_id=quest-2", quest_row["blocker_detail_filter"])
        self.assertEqual(
            "quest-2:quest_reward:quest_id=2;reward_id=300 || quest-2:quest_creature:quest_id=2;relation_type=starter",
            quest_row["first_blocker_ids"],
        )
        self.assertEqual("quest_validation_ready", quest_row["actionability_state"])
        self.assertEqual("false", quest_row["split_required"])
        self.assertEqual("quest_creature", quest_row["primary_blocker_category"])
        self.assertEqual("false", quest_row["retail_claim_allowed"])

        instance_row = annotated_rows[1]
        self.assertEqual(1, instance_row["blocker_detail_count"])
        self.assertEqual("instance_dependency=1", instance_row["blocker_count_by_category"])
        self.assertEqual("instance_validation_ready", instance_row["actionability_state"])

    def test_next_slice_queue_actionability_marks_broad_low_coverage_instances_for_split(self) -> None:
        queue_rows = [
            {
                "queue_rank": 1,
                "slice_id": "instance-2980",
                "lane": "instance",
                "scripted_producer_coverage": "1/123",
                "owner_paths": r"Source\NexusForever.Script.Instance\Dungeon\UltimateProtogames\UltimateProtogamesEventScript.cs",
            }
        ]
        blocker_rows = [
            {
                "slice_id": "instance-2980",
                "blocker_rank": rank,
                "blocker_category": "instance_dependency",
                "blocker_id": f"instance-2980:instance_dependency:dependency_id={rank}",
            }
            for rank in range(1, 102)
        ]

        annotated_rows = audit.annotate_next_slice_queue_blocker_details(queue_rows, blocker_rows)

        row = annotated_rows[0]
        self.assertEqual(101, row["blocker_detail_count"])
        self.assertEqual("split_required_producer_gap", row["actionability_state"])
        self.assertEqual("true", row["split_required"])
        self.assertEqual("blocker_count_gt_100;producer_coverage_lt_50pct", row["split_reason"])
        self.assertEqual("instance_dependency", row["primary_blocker_category"])
        self.assertEqual("client_smoke", row["blocking_evidence_source"])
        self.assertEqual("Mapped", row["required_evidence_level"])

    def test_next_slice_queue_prefers_focused_evidence_and_excludes_broad_shared_rows(self) -> None:
        quest_rows = [
            {
                "quest_id": 1,
                "name": "Broad mapped quest",
                "bucket": "generic_table_driven",
                "evidence_status": "mapped_table_driven_pending_world_spawns_loot_smoke",
                "completion_status": "not_retail_complete",
                "objective_ids": "100",
                "quest2_reward_ids": "",
                "related_achievement_ids": "",
                "quest_virtual_loot_groups": 0,
                "script_paths": "",
                "evidence_sources": "Decomp/Analysis/CONTENT_RETAIL_COMPLETENESS_TRACKER.md",
                "blocker_summary": "Mapped row still needs world smoke.",
            },
            {
                "quest_id": 2,
                "name": "Focused quest",
                "bucket": "curated_full",
                "evidence_status": "implemented_with_focused_tests_pending_manual_smoke",
                "completion_status": "not_retail_complete",
                "objective_ids": "200;201",
                "quest2_reward_ids": "300",
                "related_achievement_ids": "400",
                "quest_virtual_loot_groups": 0,
                "script_paths": r"Source\NexusForever.Script.Main\Quests\FocusedQuest.cs",
                "evidence_sources": r"Source/NexusForever.Game.Tests/Quest/QuestTests.cs",
                "blocker_summary": "Implemented row still needs client smoke.",
            },
            {
                "quest_id": 3,
                "name": "Shared lifecycle only",
                "bucket": "no_objectives",
                "evidence_status": "mapped_only_no_objective_lifecycle_pending_row_specific_dialog_reward_achievement_smoke",
                "completion_status": "not_retail_complete",
                "objective_ids": "",
                "quest2_reward_ids": "",
                "related_achievement_ids": "",
                "quest_virtual_loot_groups": 0,
                "script_paths": "",
                "evidence_sources": "Source/NexusForever.Game.Tests/Quest/QuestTests.cs",
                "blocker_summary": "Shared lifecycle row still needs row-specific dialog smoke.",
            },
        ]
        instance_rows = [
            {
                "content_type": "dungeon",
                "world_id": 9,
                "name": "Focused Instance",
                "public_event_ids": "10",
                "public_event_objective_count": 18,
                "matching_game_map_ids": "11",
                "related_achievement_ids": "12",
                "script_paths": r"Source\NexusForever.Script.Instance\Dungeon\Focused\FocusedEventScript.cs",
                "milestone_scope": "milestone_target_instance",
                "implementation_status": "mapped_wip_scaffold",
                "evidence_status": "mapped_only_pending_manual_smoke",
                "completion_status": "not_retail_complete",
                "evidence_sources": "Source/NexusForever.Script.Instance",
                "blocker_summary": "Mapped instance still needs manual smoke.",
            },
            {
                "content_type": "event_instance",
                "world_id": 10,
                "name": "Adjacent Event Instance",
                "public_event_ids": "12",
                "public_event_objective_count": 60,
                "matching_game_map_ids": "13",
                "related_achievement_ids": "14",
                "script_paths": r"Source\NexusForever.Script.Instance\Dungeon\Adjacent\AdjacentMapScript.cs",
                "milestone_scope": "inventory_only_adjacent_scripted_content",
                "implementation_status": "mapped_wip_scaffold",
                "evidence_status": "mapped_only_pending_manual_smoke",
                "completion_status": "not_retail_complete",
                "evidence_sources": "Source/NexusForever.Script.Instance",
                "blocker_summary": "Adjacent row still needs manual smoke.",
            }
        ]
        producer_rows = [
            {"matched_public_event_ids": "10", "objective_id": "20"},
            {"matched_public_event_ids": "12", "objective_id": "21"},
        ]
        handler_rows = [
            {
                "world_id": "9",
                "script_path": r"Source\NexusForever.Script.Instance\Dungeon\Focused\FocusedTriggerScript.cs",
            },
            {
                "world_id": "10",
                "script_path": r"Source\NexusForever.Script.Instance\Dungeon\Adjacent\AdjacentTriggerScript.cs",
            }
        ]
        instance_entity_rows = [
            {
                "world_id": "9",
                "dependency_type": "entity",
                "dependency_id": "1100300001",
            },
            {
                "world_id": "9",
                "dependency_type": "entity_stats",
                "dependency_id": "1100300001:10",
            },
        ]
        quest_world_dependency_rows = [
            {
                "quest_id": "2",
                "world_id": "870",
                "dependency_type": "objective_indicator_location",
            }
        ]
        quest_reward_evidence_rows = [
            {
                "quest_id": "2",
                "source_reward_id": "5289",
                "client_reward_object_id": "13327",
                "game_reward_id": "2477",
                "canonical_reward_ids": "8569",
                "canonical_reward_match_status": "canonical_reward_id_mismatch",
                "evidence_status": "datamapping_jabbithole_item_reward_game_reward_id_mismatch_pending_review",
                "evidence_sources": "Tools/DataMapping/output/quest_reward_client_map.csv;wildstar_client_mysql/Quest2Reward.tbl.sql",
                "blocker_summary": "Canonical Quest2Reward row 8569 has the same quest/item/amount/flags.",
            }
        ]
        quest_creature_rows = [
            {
                "quest_id": "2",
                "quest_link_status": "current_quest",
                "completion_status": "not_retail_complete",
                "relation_type": "starter",
                "source_relation_id": "88",
                "jabbithole_creature_id": "23645",
                "creature2_id": "",
                "match_status": "unmatched",
                "datamapping_unpromoted_spawn_count": "1",
                "evidence_status": "datamapping_creature_bridge_unmatched_blocked",
                "evidence_sources": "Tools/DataMapping/output/creature_quest_map.csv",
            },
            {
                "quest_id": "2",
                "quest_link_status": "non_current_client_quest_reference",
                "completion_status": "not_retail_complete",
                "relation_type": "finisher",
                "source_relation_id": "99",
                "jabbithole_creature_id": "123",
                "creature2_id": "",
                "match_status": "unmatched",
                "datamapping_unpromoted_spawn_count": "0",
                "evidence_status": "datamapping_creature_bridge_unmatched_blocked",
                "evidence_sources": "Tools/DataMapping/output/creature_quest_map.csv",
            },
        ]
        quest_objective_evidence_rows = [
            {
                "quest_id": "2",
                "quest_link_status": "current_quest",
                "completion_status": "not_retail_complete",
                "jabbithole_quest_objective_id": "418",
                "objective_order": "0",
                "quest_objective_id": "",
                "evidence_status": "datamapping_jabbithole_objective_unmatched_blocked",
                "evidence_sources": "Tools/DataMapping/output/quest_objective_map.csv",
            }
        ]
        quest_zone_evidence_rows = [
            {
                "quest_id": "2",
                "quest_link_status": "current_quest",
                "completion_status": "not_retail_complete",
                "relation_source": "quest_zones",
                "source_relation_id": "77",
                "jabbithole_zone_id": "5",
                "world_zone_id": "6",
                "enabled": "true",
                "evidence_status": "datamapping_quest_zone_partial_match_pending_zone_review",
                "evidence_sources": "Tools/DataMapping/output/quest_zone_map.csv",
            },
            {
                "quest_id": "2",
                "quest_link_status": "current_quest",
                "completion_status": "not_retail_complete",
                "relation_source": "quest_zones",
                "source_relation_id": "78",
                "jabbithole_zone_id": "7",
                "world_zone_id": "8",
                "enabled": "false",
                "evidence_status": "datamapping_quest_zone_partial_match_pending_zone_review",
                "evidence_sources": "Tools/DataMapping/output/quest_zone_map.csv",
            },
        ]

        queue_rows = audit.build_next_restoration_slice_queue(
            quest_rows,
            instance_rows,
            [],
            [],
            quest_reward_evidence_rows,
            quest_creature_rows,
            quest_objective_evidence_rows,
            quest_zone_evidence_rows,
            quest_world_dependency_rows,
            [],
            producer_rows,
            handler_rows,
            [],
            instance_entity_rows,
            audit.DEFAULT_OUTPUT_DIR,
            quest_limit=5,
            instance_limit=5,
        )
        queue_rows_without_reward_evidence_blockers = audit.build_next_restoration_slice_queue(
            quest_rows,
            instance_rows,
            [],
            [],
            [],
            [],
            [],
            [],
            quest_world_dependency_rows,
            [],
            producer_rows,
            handler_rows,
            [],
            instance_entity_rows,
            audit.DEFAULT_OUTPUT_DIR,
            quest_limit=5,
            instance_limit=5,
        )

        self.assertEqual("quest-2", queue_rows[0]["slice_id"])
        self.assertEqual("P0", queue_rows[0]["priority"])
        self.assertEqual(queue_rows_without_reward_evidence_blockers[0]["priority_score"], queue_rows[0]["priority_score"])
        self.assertEqual("content_retail_completeness_quests.csv", queue_rows[0]["source_inventory"])
        self.assertIn("focused runtime tests", queue_rows[0]["candidate_reason"])
        self.assertIn("1 reward-evidence mismatch blocker row", queue_rows[0]["candidate_reason"])
        self.assertIn("1 objective-evidence blocker row", queue_rows[0]["candidate_reason"])
        self.assertIn("1 zone-evidence blocker row", queue_rows[0]["candidate_reason"])
        self.assertIn("1 creature-evidence blocker row", queue_rows[0]["candidate_reason"])
        self.assertIn("reward_evidence_blockers=5289:13327:2477", queue_rows[0]["related_ids"])
        self.assertIn("objective_evidence_blockers=418@0:unmatched", queue_rows[0]["related_ids"])
        self.assertIn("zone_evidence_blockers=quest_zones:77:5>6", queue_rows[0]["related_ids"])
        self.assertIn("creature_evidence_blockers=starter:88:23645:unmatched", queue_rows[0]["related_ids"])
        self.assertNotIn("quest_zones:78:7>8", queue_rows[0]["related_ids"])
        self.assertNotIn("finisher:99:123:unmatched", queue_rows[0]["related_ids"])
        self.assertIn("-WorldIds 870", queue_rows[0]["harness_command"])
        self.assertIn("content_retail_completeness_quest_reward_evidence.csv", queue_rows[0]["tracker_paths"])
        self.assertIn("content_retail_completeness_quest_objective_evidence.csv", queue_rows[0]["tracker_paths"])
        self.assertIn("content_retail_completeness_quest_zone_evidence.csv", queue_rows[0]["tracker_paths"])
        self.assertIn("content_retail_completeness_quest_creatures.csv", queue_rows[0]["tracker_paths"])
        self.assertIn("quest_reward_client_map.csv", queue_rows[0]["evidence_sources"])
        self.assertIn("quest_objective_map.csv", queue_rows[0]["evidence_sources"])
        self.assertIn("quest_zone_map.csv", queue_rows[0]["evidence_sources"])
        self.assertIn("creature_quest_map.csv", queue_rows[0]["evidence_sources"])
        self.assertIn("source_reward_id 5289 Item2 13327 game_reward_id 2477", queue_rows[0]["required_validation"])
        self.assertIn("canonical_reward_ids 8569", queue_rows[0]["required_validation"])
        self.assertIn("jabbithole_objective_id 418 order 0 -> unmatched", queue_rows[0]["required_validation"])
        self.assertIn("quest_zones 77 zone 5->6", queue_rows[0]["required_validation"])
        self.assertIn("starter 88 creature 23645->unmatched", queue_rows[0]["required_validation"])
        self.assertIn("source_reward_id 5289 Item2 13327 game_reward_id 2477", queue_rows[0]["blocker_summary"])
        self.assertIn("canonical_reward_ids 8569", queue_rows[0]["blocker_summary"])
        self.assertIn("canonical reward-id proof", queue_rows[0]["blocker_summary"])
        self.assertIn("Objective-evidence blocker", queue_rows[0]["blocker_summary"])
        self.assertIn("Zone-evidence blocker", queue_rows[0]["blocker_summary"])
        self.assertIn("Creature-evidence blocker", queue_rows[0]["blocker_summary"])
        focused_instance_row = next(row for row in queue_rows if row["slice_id"] == "instance-9")
        self.assertEqual(18, focused_instance_row["objective_row_count"])
        self.assertEqual(1, focused_instance_row["scripted_producer_count"])
        self.assertEqual(1, focused_instance_row["scripted_producer_objective_count"])
        self.assertEqual("1/18", focused_instance_row["scripted_producer_coverage"])
        self.assertEqual(1, focused_instance_row["handler_count"])
        self.assertEqual(1, focused_instance_row["seed_entity_count"])
        self.assertNotIn("quest-1", {row["slice_id"] for row in queue_rows})
        self.assertNotIn("quest-3", {row["slice_id"] for row in queue_rows})
        self.assertNotIn("instance-10", {row["slice_id"] for row in queue_rows})
        self.assertEqual(list(range(1, len(queue_rows) + 1)), [row["queue_rank"] for row in queue_rows])

    def test_sql_loader_handles_multi_row_inserts_and_escaped_strings(self) -> None:
        sql_root = Path(tempfile.mkdtemp(prefix="nf-content-audit-sql-"))
        try:
            (sql_root / "Example.tbl.sql").write_text(
                """
CREATE TABLE Example(ID INTEGER, name VARCHAR(64), amount INTEGER, PRIMARY KEY (ID));
INSERT INTO Example VALUES (1,'Alpha, Beta',10),(2,'Bob''s Quest',20)
ON DUPLICATE KEY UPDATE name = VALUES(name);
""".lstrip(),
                encoding="utf-8",
            )

            rows = audit.load_table(sql_root, "Example.tbl.sql", "Example")

            self.assertEqual(
                [
                    {"ID": "1", "name": "Alpha, Beta", "amount": "10"},
                    {"ID": "2", "name": "Bob's Quest", "amount": "20"},
                ],
                rows,
            )
        finally:
            shutil.rmtree(sql_root, ignore_errors=True)

    def test_next_slice_queue_allows_output_dir_outside_repo(self) -> None:
        output_dir = Path(tempfile.mkdtemp(prefix="nf-content-audit-outside-"))
        try:
            rows = audit.build_next_restoration_slice_queue(
                [
                    {
                        "quest_id": 2,
                        "name": "Focused quest",
                        "bucket": "curated_full",
                        "evidence_status": "implemented_with_focused_tests_pending_manual_smoke",
                        "completion_status": "not_retail_complete",
                        "objective_ids": "200",
                        "quest2_reward_ids": "",
                        "related_achievement_ids": "",
                        "quest_virtual_loot_groups": 0,
                        "script_paths": "",
                        "evidence_sources": "Decomp/Analysis/CONTENT_RETAIL_COMPLETENESS_TRACKER.md",
                        "blocker_summary": "Implemented row still needs client smoke.",
                    }
                ],
                [],
                [],
                [],
                [],
                [],
                [],
                [],
                [],
                [],
                [],
                [],
                [],
                [],
                output_dir,
                quest_limit=1,
                instance_limit=0,
            )

            self.assertEqual("quest-2", rows[0]["slice_id"])
            self.assertIn(str(output_dir), rows[0]["tracker_paths"])
        finally:
            shutil.rmtree(output_dir, ignore_errors=True)

    def test_next_slice_blocker_inventory_preserves_quest_and_instance_blockers(self) -> None:
        queue_rows = [
            {
                "queue_rank": 1,
                "slice_id": "quest-2",
                "lane": "quest",
                "priority": "P0",
                "content_type": "quest",
                "content_id": "2",
                "content_name": "Focused quest",
                "confidence_level": "implemented_pending_manual_smoke",
                "required_validation": "Quest validation.",
                "owner_paths": r"Source\NexusForever.Script.Main\Quests\FocusedQuest.cs",
                "evidence_sources": "Decomp/Analysis/CONTENT_RETAIL_COMPLETENESS_TRACKER.md",
            },
            {
                "queue_rank": 2,
                "slice_id": "instance-9",
                "lane": "instance",
                "priority": "P1",
                "content_type": "dungeon",
                "content_id": "9",
                "content_name": "Focused Instance",
                "confidence_level": "mapped_wip_scaffold_with_script_evidence",
                "required_validation": "Instance validation.",
                "owner_paths": r"Source\NexusForever.Script.Instance\Dungeon\Focused\FocusedEventScript.cs",
                "evidence_sources": "Decomp/Analysis/CONTENT_RETAIL_COMPLETENESS_TRACKER.md",
            },
        ]
        blockers = audit.build_next_restoration_slice_blockers(
            queue_rows,
            [
                {
                    "quest_id": "2",
                    "source_reward_id": "5289",
                    "client_reward_object_id": "13327",
                    "game_reward_id": "2477",
                    "canonical_reward_ids": "8569",
                    "canonical_reward_match_status": "canonical_reward_id_mismatch",
                    "reward_kind": "item",
                    "evidence_status": "datamapping_jabbithole_item_reward_game_reward_id_mismatch_pending_review",
                    "evidence_sources": "Tools/DataMapping/output/quest_reward_client_map.csv",
                    "manual_validation": "pending",
                    "completion_status": "not_retail_complete",
                    "blocker_summary": "Reward provenance needs review.",
                }
            ],
            [
                {
                    "quest_id": "2",
                    "quest_name": "Focused quest",
                    "quest_bucket": "generic_with_script",
                    "quest_completion_status": "not_retail_complete",
                    "reward_source": "Quest2Reward",
                    "reward_slot": "",
                    "reward_id": "300",
                    "reward_type": "1",
                    "object_id": "13327",
                    "object_amount": "1",
                    "flags": "1",
                    "support_status": "mapped_runtime_reward_path_pending_row_smoke",
                    "evidence_status": "runtime_focused_selectable_item_reward_grant_tested_pending_client_smoke",
                    "evidence_sources": "wildstar_client_mysql/Quest2Reward.tbl.sql;Source/NexusForever.Game/Entity/QuestManager.cs",
                    "manual_validation": "pending_or_not_applicable",
                    "completion_status": "not_retail_complete",
                    "blocker_summary": "Reward row needs UI and persistence smoke.",
                },
                {
                    "quest_id": "99",
                    "quest_name": "Other quest",
                    "reward_source": "Quest2Reward",
                    "reward_id": "990",
                    "reward_type": "1",
                    "object_id": "9900",
                    "object_amount": "1",
                    "flags": "1",
                    "support_status": "mapped_runtime_reward_path_pending_row_smoke",
                    "evidence_status": "mapped_runtime_reward_path_pending_row_smoke",
                    "evidence_sources": "wildstar_client_mysql/Quest2Reward.tbl.sql",
                    "manual_validation": "pending_or_not_applicable",
                    "completion_status": "not_retail_complete",
                    "blocker_summary": "Nonqueued reward should not appear.",
                },
            ],
            [
                {
                    "quest_id": "2",
                    "quest_name": "Focused quest",
                    "quest_bucket": "generic_with_script",
                    "quest_completion_status": "not_retail_complete",
                    "objective_slot": "0",
                    "objective_column": "objective0",
                    "objective_id": "418",
                    "objective_type": "5",
                    "objective_flags": "16",
                    "objective_data": "200",
                    "objective_count": "1",
                    "localized_text_full": "Activate the focused object.",
                    "localized_text_short": "Activate object",
                    "world_location_indicator_ids": "870;871",
                    "max_time_allowed_ms": "0",
                    "target_group_id_reward_pane": "300",
                    "quest_direction_id": "400",
                    "support_status": "mapped_generic_handler",
                    "evidence_status": "runtime_focused_objective_tested_pending_client_smoke",
                    "evidence_sources": "wildstar_client_mysql/Quest2.tbl.sql;wildstar_client_mysql/QuestObjective.tbl.sql;Source/NexusForever.Game/Quest/QuestObjective.cs",
                    "manual_validation": "pending_or_not_applicable",
                    "completion_status": "not_retail_complete",
                    "blocker_summary": "Canonical QuestObjective row needs end-to-end client smoke.",
                },
                {
                    "quest_id": "99",
                    "quest_name": "Other quest",
                    "objective_slot": "0",
                    "objective_column": "objective0",
                    "objective_id": "9900",
                    "objective_type": "5",
                    "evidence_status": "mapped_objective_handler_pending_world_trigger_smoke",
                    "evidence_sources": "wildstar_client_mysql/QuestObjective.tbl.sql",
                    "manual_validation": "pending_or_not_applicable",
                    "completion_status": "not_retail_complete",
                    "blocker_summary": "Nonqueued objective should not appear.",
                },
            ],
            [
                {
                    "quest_id": "2",
                    "quest_link_status": "current_quest",
                    "completion_status": "not_retail_complete",
                    "relation_type": "starter",
                    "source_relation_id": "88",
                    "jabbithole_creature_id": "23645",
                    "creature2_id": "",
                    "match_status": "unmatched",
                    "datamapping_unpromoted_spawn_count": "1",
                    "evidence_status": "datamapping_creature_bridge_unmatched_blocked",
                    "evidence_sources": "Tools/DataMapping/output/creature_quest_map.csv",
                    "manual_validation": "pending",
                    "blocker_summary": "Creature bridge needs review.",
                }
            ],
            [
                {
                    "quest_id": "2",
                    "quest_link_status": "current_quest",
                    "completion_status": "not_retail_complete",
                    "jabbithole_quest_objective_id": "418",
                    "objective_order": "0",
                    "quest_objective_id": "",
                    "match_status": "unmatched",
                    "evidence_status": "datamapping_jabbithole_objective_unmatched_blocked",
                    "evidence_sources": "Tools/DataMapping/output/quest_objective_map.csv",
                    "manual_validation": "pending",
                    "blocker_summary": "Objective bridge needs review.",
                }
            ],
            [
                {
                    "quest_id": "2",
                    "quest_link_status": "current_quest",
                    "completion_status": "not_retail_complete",
                    "relation_source": "quest_zones",
                    "source_relation_id": "77",
                    "jabbithole_zone_id": "5",
                    "world_zone_id": "6",
                    "match_status": "partial",
                    "enabled": "true",
                    "evidence_status": "datamapping_quest_zone_partial_match_pending_zone_review",
                    "evidence_sources": "Tools/DataMapping/output/quest_zone_map.csv",
                    "manual_validation": "pending",
                    "blocker_summary": "Zone bridge needs review.",
                },
                {
                    "quest_id": "2",
                    "quest_link_status": "current_quest",
                    "completion_status": "not_retail_complete",
                    "relation_source": "quest_zones",
                    "source_relation_id": "78",
                    "jabbithole_zone_id": "7",
                    "world_zone_id": "8",
                    "match_status": "partial",
                    "enabled": "false",
                    "evidence_status": "datamapping_quest_zone_partial_match_pending_zone_review",
                    "evidence_sources": "Tools/DataMapping/output/quest_zone_map.csv",
                    "manual_validation": "pending",
                    "blocker_summary": "Disabled zone row should be ignored.",
                },
            ],
            [
                {
                    "quest_id": "2",
                    "title": "Focused quest",
                    "quest_bucket": "generic_with_script",
                    "quest_completion_status": "not_retail_complete",
                    "dependency_source": "QuestObjective.worldLocationsIdIndicator",
                    "dependency_type": "objective_indicator_location",
                    "dependency_id": "418:worldLocationsIdIndicator00:870",
                    "dependency_name": "Focused objective location",
                    "objective_id": "418",
                    "objective_type": "5",
                    "source_slot": "worldLocationsIdIndicator00",
                    "world_location_id": "870",
                    "world_id": "426",
                    "world_zone_id": "35",
                    "world_zone_name": "Northern Wilds",
                    "position_x": "1.25",
                    "position_y": "2.5",
                    "position_z": "3.75",
                    "radius": "8",
                    "phases": "0",
                    "quest_direction_id": "",
                    "quest_direction_entry_id": "",
                    "target_group_id": "",
                    "target_group_type": "",
                    "target_group_data": "",
                    "prerequisite_id": "",
                    "evidence_status": "client_objective_indicator_location_pending_world_trigger_smoke",
                    "evidence_sources": "wildstar_client_mysql/QuestObjective.tbl.sql;wildstar_client_mysql/WorldLocation2.tbl.sql",
                    "manual_validation": "pending",
                    "completion_status": "not_retail_complete",
                    "blocker_summary": "World dependency needs location trigger smoke.",
                },
                {
                    "quest_id": "99",
                    "title": "Other quest",
                    "dependency_source": "Quest2.worldZoneId",
                    "dependency_type": "quest_world_zone",
                    "dependency_id": "99",
                    "world_zone_id": "99",
                    "evidence_status": "client_quest_world_zone_pending_map_and_activation_smoke",
                    "evidence_sources": "wildstar_client_mysql/Quest2.tbl.sql",
                    "manual_validation": "pending",
                    "completion_status": "not_retail_complete",
                    "blocker_summary": "Nonqueued world dependency should not appear.",
                },
                {
                    "quest_id": "2",
                    "title": "Focused quest",
                    "dependency_source": "QuestObjective.data",
                    "dependency_type": "target_group",
                    "dependency_id": "7288",
                    "objective_id": "418",
                    "objective_type": "8",
                    "target_group_id": "7288",
                    "target_group_type": "11",
                    "evidence_status": "client_target_group_pending_objective_entity_smoke",
                    "evidence_sources": "wildstar_client_mysql/TargetGroup.tbl.sql",
                    "manual_validation": "pending",
                    "completion_status": "not_retail_complete",
                    "blocker_summary": "Target-group rows are covered by creature/objective lanes and should not appear yet.",
                },
            ],
            [
                {
                    "quest_id": "2",
                    "title": "Focused quest",
                    "quest_bucket": "generic_with_script",
                    "quest_completion_status": "not_retail_complete",
                    "dependency_type": "achievement_checklist",
                    "achievement_id": "300",
                    "achievement_title": "Focused achievement",
                    "achievement_description": "Complete the focused quest.",
                    "achievement_progress_text": "Complete focused quest",
                    "achievement_type_id": "6",
                    "achievement_category_id": "99",
                    "achievement_flags": "0",
                    "world_zone_id": "6",
                    "source_field": "AchievementChecklist.objectId",
                    "object_id": "0",
                    "object_id_alt": "0",
                    "value": "0",
                    "character_title_id": "",
                    "prerequisite_id": "",
                    "prerequisite_id_server": "",
                    "prerequisite_id_objective": "",
                    "prerequisite_id_objective_alt": "",
                    "achievement_id_parent_tier": "",
                    "checklist_id": "400",
                    "checklist_bit": "2",
                    "checklist_object_id": "2",
                    "checklist_object_id_alt": "",
                    "checklist_prerequisite_id": "",
                    "checklist_prerequisite_id_alt": "",
                    "evidence_status": "client_achievement_checklist_row_pending_progression_hook_smoke",
                    "evidence_sources": "wildstar_client_mysql/Achievement.tbl.sql;wildstar_client_mysql/AchievementChecklist.tbl.sql",
                    "manual_validation": "pending",
                    "completion_status": "not_retail_complete",
                    "blocker_summary": "Achievement checklist needs client UI smoke.",
                },
                {
                    "quest_id": "99",
                    "title": "Other quest",
                    "dependency_type": "achievement_checklist",
                    "achievement_id": "301",
                    "checklist_id": "401",
                    "checklist_bit": "0",
                    "source_field": "AchievementChecklist.objectId",
                    "evidence_status": "client_achievement_checklist_row_pending_progression_hook_smoke",
                    "evidence_sources": "wildstar_client_mysql/Achievement.tbl.sql;wildstar_client_mysql/AchievementChecklist.tbl.sql",
                    "manual_validation": "pending",
                    "completion_status": "not_retail_complete",
                    "blocker_summary": "Nonqueued achievement should not appear.",
                },
            ],
            [
                {
                    "quest_id": "2",
                    "quest_name": "Focused quest",
                    "quest_bucket": "generic_with_script",
                    "quest_completion_status": "not_retail_complete",
                    "dependency_type": "required_quest",
                    "dependency_slot": "0",
                    "dependency_id": "1",
                    "dependency_name": "Previous quest",
                    "dependency_value": "",
                    "dependency_flags": "",
                    "object_id": "1",
                    "comparison_id": "",
                    "prerequisite_type_id": "",
                    "localized_failure_text": "Requires previous quest.",
                    "support_status": "mapped_quest_chain_validation_pending_chain_smoke",
                    "evidence_status": "client_quest2_required_quest_pending_chain_smoke",
                    "evidence_sources": "wildstar_client_mysql/Quest2.tbl.sql;wildstar_client_mysql/Prerequisite.tbl.sql",
                    "manual_validation": "pending_or_not_applicable",
                    "completion_status": "not_retail_complete",
                    "blocker_summary": "Required quest needs chain smoke.",
                },
                {
                    "quest_id": "99",
                    "quest_name": "Other quest",
                    "dependency_type": "level",
                    "dependency_slot": "",
                    "dependency_id": "",
                    "dependency_value": "3",
                    "evidence_status": "client_quest2_level_gate_pending_accept_smoke",
                    "evidence_sources": "wildstar_client_mysql/Quest2.tbl.sql",
                    "manual_validation": "pending_or_not_applicable",
                    "completion_status": "not_retail_complete",
                    "blocker_summary": "Nonqueued prerequisite should not appear.",
                },
            ],
            [
                {
                    "quest_id": "2",
                    "quest_name": "Focused quest",
                    "quest_bucket": "generic_with_script",
                    "quest_completion_status": "not_retail_complete",
                    "objective_id": "418",
                    "objective_slot": "0",
                    "objective_type": "32",
                    "objective_name": "Focused loot objective",
                    "dependency_type": "loot_item",
                    "loot_group_id": "1200000001",
                    "entity_id": "",
                    "item_type": "6",
                    "static_id": "265",
                    "probability": "100",
                    "min_count": "1",
                    "max_count": "1",
                    "condition_type": "8",
                    "condition_id": "418",
                    "comment": "Focused quest loot item.",
                    "evidence_status": "reviewed_laughingws_loot_item_pending_drop_smoke",
                    "evidence_sources": "Tools/DataMapping/sql/laughingws_quest_loot_seed.sql",
                    "manual_validation": "pending",
                    "completion_status": "not_retail_complete",
                    "blocker_summary": "Quest loot item needs drop smoke.",
                },
                {
                    "quest_id": "99",
                    "quest_name": "Other quest",
                    "quest_bucket": "generic_table_driven",
                    "quest_completion_status": "not_retail_complete",
                    "objective_id": "9900",
                    "dependency_type": "loot_group",
                    "loot_group_id": "1200000099",
                    "condition_type": "8",
                    "condition_id": "9900",
                    "evidence_status": "reviewed_laughingws_loot_group_pending_drop_smoke",
                    "evidence_sources": "Tools/DataMapping/sql/laughingws_quest_loot_seed.sql",
                    "manual_validation": "pending",
                    "completion_status": "not_retail_complete",
                    "blocker_summary": "Nonqueued loot should not appear.",
                },
            ],
            [
                {
                    "evidence_kind": "script_quest_objective_update",
                    "script_scope": "script_main",
                    "source_path": r"Source\NexusForever.Script.Main\Quests\FocusedQuest.cs",
                    "source_line": "11",
                    "script_owner_ids": "2",
                    "script_creature_ids": "",
                    "script_filter_name": "FocusedQuest",
                    "source_call": "ObjectiveUpdate",
                    "source_symbol": "FocusedObjective",
                    "progression_targeting": "direct_quest_objective",
                    "quest_id": "2",
                    "quest_title": "Focused quest",
                    "quest_bucket": "generic_with_script",
                    "quest_completion_status": "not_retail_complete",
                    "objective_id": "418",
                    "objective_type_id": "5",
                    "objective_type_name": "ActivateEntity",
                    "object_id": "200",
                    "update_count": "1",
                    "achievement_id": "",
                    "achievement_title": "",
                    "matched_quest_ids": "2",
                    "matched_quest_titles": "Focused quest",
                    "matched_quest_objective_ids": "418",
                    "matched_objective_text": "Focused quest objective",
                    "reference_link_status": "direct_objective_row_matched",
                    "owner_link_status": "script_owner_matches_quest",
                    "source_trace_status": "literal_or_constant_client_reference_tracked",
                    "source_trace_detail": "objective_expression=FocusedObjective;link_status=numeric_constant_resolved",
                    "source_trace_sources": "Source/NexusForever.Script.Main;wildstar_client_mysql",
                    "evidence_status": "script_quest_objective_update_matched_pending_trigger_smoke",
                    "evidence_sources": "Source/NexusForever.Script.Main;wildstar_client_mysql/QuestObjective.tbl.sql",
                    "manual_validation": "pending",
                    "completion_status": "not_retail_complete",
                    "blocker_summary": "Quest progression hook needs trigger smoke.",
                },
                {
                    "evidence_kind": "script_quest_add",
                    "script_scope": "script_main",
                    "source_path": r"Source\NexusForever.Script.Main\Quests\FocusedQuest.cs",
                    "source_line": "12",
                    "script_owner_ids": "",
                    "source_call": "QuestAdd",
                    "source_symbol": "info<-questId",
                    "progression_targeting": "quest_info",
                    "quest_id": "",
                    "reference_link_status": "dynamic_quest_info",
                    "owner_link_status": "script_owner_not_declared",
                    "source_trace_status": "dynamic_progression_source_expression_tracked",
                    "evidence_status": "script_quest_add_dynamic_quest_info_pending_trace",
                    "evidence_sources": "Source/NexusForever.Script.Main",
                    "manual_validation": "pending",
                    "completion_status": "not_retail_complete",
                    "blocker_summary": "Dynamic quest add should stay in evidence inventory only.",
                },
                {
                    "evidence_kind": "script_quest_objective_update",
                    "script_scope": "script_main",
                    "source_path": r"Source\NexusForever.Script.Main\Quests\OtherQuest.cs",
                    "source_line": "13",
                    "script_owner_ids": "99",
                    "source_call": "ObjectiveUpdate",
                    "source_symbol": "OtherObjective",
                    "progression_targeting": "direct_quest_objective",
                    "quest_id": "99",
                    "quest_title": "Other quest",
                    "objective_id": "9900",
                    "reference_link_status": "direct_objective_row_matched",
                    "owner_link_status": "script_owner_matches_quest",
                    "source_trace_status": "literal_or_constant_client_reference_tracked",
                    "evidence_status": "script_quest_objective_update_matched_pending_trigger_smoke",
                    "evidence_sources": "Source/NexusForever.Script.Main;wildstar_client_mysql/QuestObjective.tbl.sql",
                    "manual_validation": "pending",
                    "completion_status": "not_retail_complete",
                    "blocker_summary": "Nonqueued quest progression should not appear.",
                },
            ],
            [
                {
                    "owner_id": "2",
                    "quest_id": "2",
                    "quest_title": "Focused quest",
                    "quest_bucket": "generic_with_script",
                    "quest_completion_status": "not_retail_complete",
                    "quest_link_status": "current_quest",
                    "script_kind": "quest_script",
                    "script_class": "FocusedQuestScript",
                    "script_path": r"Source\NexusForever.Script.Main\Quests\FocusedQuest.cs",
                    "owner_ids_on_attribute": "2",
                    "evidence_status": "runtime_focused_quest_script_tested_pending_client_smoke",
                    "evidence_sources": "Source/NexusForever.Script.Main;Decomp/Analysis/QUEST_IMPLEMENTATION_STATUS.md",
                    "manual_validation": "pending",
                    "completion_status": "not_retail_complete",
                    "blocker_summary": "Quest script needs lifecycle and client smoke.",
                },
                {
                    "owner_id": "99",
                    "quest_id": "99",
                    "quest_title": "Other quest",
                    "quest_link_status": "current_quest",
                    "script_kind": "quest_script",
                    "script_class": "OtherQuestScript",
                    "script_path": r"Source\NexusForever.Script.Main\Quests\OtherQuest.cs",
                    "owner_ids_on_attribute": "99",
                    "evidence_status": "runtime_quest_script_hook_pending_review",
                    "evidence_sources": "Source/NexusForever.Script.Main",
                    "manual_validation": "pending",
                    "completion_status": "not_retail_complete",
                    "blocker_summary": "Nonqueued quest script should not appear.",
                },
                {
                    "owner_id": "2",
                    "quest_id": "2",
                    "quest_title": "Focused quest",
                    "quest_link_status": "current_quest",
                    "script_kind": "map_script",
                    "script_class": "FocusedMapScript",
                    "script_path": r"Source\NexusForever.Script.Main\Quests\FocusedMapScript.cs",
                    "owner_ids_on_attribute": "2",
                    "evidence_status": "runtime_map_script_owner_pending_zone_smoke",
                    "evidence_sources": "Source/NexusForever.Script.Main",
                    "manual_validation": "pending",
                    "completion_status": "not_retail_complete",
                    "blocker_summary": "Map script should not appear in quest_script blockers.",
                },
            ],
            [
                {
                    "quest_id": "2",
                    "quest_title": "Focused quest",
                    "quest_bucket": "generic_with_script",
                    "quest_completion_status": "not_retail_complete",
                    "quest_link_status": "current_quest",
                    "episode_quest_id": "3000",
                    "episode_id": "314",
                    "episode_name": "Focused episode",
                    "order_index": "2",
                    "flags": "1",
                    "episode_bridge_status": "mapped_episode",
                    "episode_match_status": "matched",
                    "jabbithole_episode_id": "16",
                    "jabbithole_episode_name": "Focused source episode",
                    "client_episode_name": "Focused episode",
                    "client_world_zone_id": "35",
                    "client_world_zone_name": "Northern Wilds",
                    "slug": "focused-episode-314",
                    "episode_quests_count": "4",
                    "enabled": "true",
                    "evidence_status": "datamapping_episode_quest_matched_pending_order_progression_smoke",
                    "evidence_sources": "Tools/DataMapping/output/episode_quest_client_map.csv;Tools/DataMapping/output/quest_episode_map.csv",
                    "manual_validation": "pending",
                    "completion_status": "not_retail_complete",
                    "blocker_summary": "Quest episode needs order and progression smoke.",
                },
                {
                    "quest_id": "99",
                    "quest_title": "Other quest",
                    "quest_link_status": "current_quest",
                    "episode_quest_id": "9990",
                    "episode_id": "999",
                    "episode_match_status": "matched",
                    "episode_bridge_status": "mapped_episode",
                    "evidence_status": "datamapping_episode_quest_matched_pending_order_progression_smoke",
                    "evidence_sources": "Tools/DataMapping/output/episode_quest_client_map.csv",
                    "manual_validation": "pending",
                    "completion_status": "not_retail_complete",
                    "blocker_summary": "Nonqueued episode should not appear.",
                },
            ],
            [
                {
                    "world_id": "9",
                    "dependency_type": "public_event",
                    "dependency_id": "10",
                    "dependency_name": "Focused Public Event",
                    "public_event_id": "10",
                    "evidence_status": "client_public_event_row_verified_for_test_mapping",
                    "evidence_sources": "wildstar_client_mysql/PublicEvent.tbl.sql",
                    "manual_validation": "verified",
                    "completion_status": "retail_complete",
                    "blocker_summary": "Complete public event dependency should only link producer evidence.",
                },
                {
                    "world_id": "9",
                    "dependency_type": "public_event_objective",
                    "dependency_id": "20",
                    "public_event_id": "10",
                    "objective_type": "0",
                    "object_id": "200",
                    "evidence_status": "client_public_event_objective_row_pending_runtime_mechanics_smoke",
                    "evidence_sources": "wildstar_client_mysql/PublicEventObjective.tbl.sql",
                    "manual_validation": "pending",
                    "completion_status": "not_retail_complete",
                    "blocker_summary": "Public event objective needs mechanics smoke.",
                }
            ],
            [
                {
                    "evidence_kind": "public_event_creature",
                    "source_map": "creature_public_event_map.csv",
                    "source_row_id": "500",
                    "jabbithole_public_event_id": "1000",
                    "public_event_id": "10",
                    "public_event_name": "Focused Public Event",
                    "client_public_event_name": "Focused Public Event",
                    "public_event_link_status": "current_public_event",
                    "parent_public_event_id": "",
                    "parent_event_link_status": "",
                    "child_public_event_id": "",
                    "child_event_link_status": "",
                    "related_jabbithole_id": "23645",
                    "related_client_id": "75508",
                    "related_source_name": "Focused source creature",
                    "related_client_name": "Focused creature",
                    "match_status": "ambiguous_name",
                    "detail_type": "11",
                    "detail_flags": "0",
                    "detail_count": "",
                    "world_zone_id": "6",
                    "world_location_id": "",
                    "quest_direction_id": "",
                    "target_group_id": "",
                    "evidence_status": "datamapping_public_event_creature_ambiguous_pending_review",
                    "evidence_sources": "Tools/DataMapping/output/creature_public_event_map.csv",
                    "manual_validation": "pending",
                    "completion_status": "not_retail_complete",
                    "blocker_summary": "Public-event creature bridge needs review.",
                },
                {
                    "evidence_kind": "public_event_zone",
                    "source_map": "public_event_zone_map.csv",
                    "source_row_id": "501",
                    "jabbithole_public_event_id": "1000",
                    "public_event_id": "10",
                    "public_event_name": "Focused Public Event",
                    "public_event_link_status": "current_public_event",
                    "related_jabbithole_id": "77",
                    "related_client_id": "6",
                    "related_source_name": "Focused source zone",
                    "related_client_name": "Focused zone",
                    "match_status": "matched",
                    "world_zone_id": "6",
                    "world_location_id": "",
                    "quest_direction_id": "",
                    "target_group_id": "",
                    "evidence_status": "datamapping_public_event_zone_matched_pending_visibility_smoke",
                    "evidence_sources": "Tools/DataMapping/output/public_event_zone_map.csv",
                    "manual_validation": "pending",
                    "completion_status": "not_retail_complete",
                    "blocker_summary": "Public-event zone needs visibility smoke.",
                },
                {
                    "evidence_kind": "public_event_objective",
                    "source_map": "public_event_objective_map.csv",
                    "source_row_id": "502",
                    "jabbithole_public_event_id": "1000",
                    "public_event_id": "10",
                    "public_event_name": "Focused Public Event",
                    "public_event_link_status": "current_public_event",
                    "related_jabbithole_id": "88",
                    "related_client_id": "",
                    "related_source_name": "Focused missing objective",
                    "related_client_name": "",
                    "match_status": "unmatched",
                    "detail_type": "0",
                    "detail_flags": "0",
                    "detail_count": "1",
                    "world_zone_id": "6",
                    "world_location_id": "870",
                    "quest_direction_id": "",
                    "target_group_id": "300",
                    "evidence_status": "datamapping_public_event_objective_unmatched_blocked",
                    "evidence_sources": "Tools/DataMapping/output/public_event_objective_map.csv",
                    "manual_validation": "pending",
                    "completion_status": "not_retail_complete",
                    "blocker_summary": "Public-event objective bridge needs reconciliation.",
                },
                {
                    "evidence_kind": "public_event_objective",
                    "source_map": "public_event_objective_map.csv",
                    "source_row_id": "503",
                    "jabbithole_public_event_id": "1000",
                    "public_event_id": "10",
                    "public_event_name": "Focused Public Event",
                    "public_event_link_status": "current_public_event",
                    "related_jabbithole_id": "89",
                    "related_client_id": "20",
                    "related_source_name": "Focused matched objective",
                    "related_client_name": "Focused objective",
                    "match_status": "matched",
                    "evidence_status": "datamapping_public_event_objective_matched_pending_runtime_mechanics_smoke",
                    "evidence_sources": "Tools/DataMapping/output/public_event_objective_map.csv",
                    "manual_validation": "pending",
                    "completion_status": "not_retail_complete",
                    "blocker_summary": "Matched public-event objective is already covered by instance_dependency.",
                },
                {
                    "evidence_kind": "public_event_creature",
                    "source_map": "creature_public_event_map.csv",
                    "source_row_id": "504",
                    "jabbithole_public_event_id": "1001",
                    "public_event_id": "11",
                    "public_event_name": "Other Public Event",
                    "public_event_link_status": "current_public_event",
                    "related_jabbithole_id": "999",
                    "related_client_id": "9999",
                    "match_status": "matched",
                    "evidence_status": "datamapping_public_event_creature_pending_review_and_smoke",
                    "evidence_sources": "Tools/DataMapping/output/creature_public_event_map.csv",
                    "manual_validation": "pending",
                    "completion_status": "not_retail_complete",
                    "blocker_summary": "Nonqueued public-event evidence should not appear.",
                },
            ],
            [
                {
                    "evidence_kind": "instance_portal_creature",
                    "source_table": "Creature2.tbl.sql",
                    "source_row_id": "75508",
                    "instance_portal_id": "41",
                    "instance_portal_name": "Focused Portal",
                    "instance_portal_type": "0",
                    "portal_min_level": "10",
                    "portal_max_level": "50",
                    "expected_completion_time": "30",
                    "portal_link_status": "client_creature_linked",
                    "creature2_id": "75508",
                    "creature_name": "Focused Portal",
                    "creature_min_level": "10",
                    "creature_max_level": "10",
                    "datamapping_bridge_status": "reviewed_datamapping_creature_bridge",
                    "datamapping_bridge_count": "1",
                    "jabbithole_creature_ids": "23645",
                    "datamapping_match_statuses": "reviewed",
                    "datamapping_review_decisions": "approved",
                    "source_names": "Focused source portal",
                    "world_ids": "9;12",
                    "world_zone_ids": "6",
                    "zone_ids": "5",
                    "evidence_status": "reviewed_instance_portal_creature_pending_entry_smoke",
                    "evidence_sources": "wildstar_client_mysql/InstancePortal.tbl.sql;wildstar_client_mysql/Creature2.tbl.sql;Tools/DataMapping/output/creature_map.csv",
                    "manual_validation": "pending",
                    "completion_status": "not_retail_complete",
                    "blocker_summary": "Portal link needs entry smoke.",
                },
                {
                    "evidence_kind": "instance_portal_creature",
                    "source_table": "Creature2.tbl.sql",
                    "source_row_id": "75509",
                    "instance_portal_id": "42",
                    "instance_portal_name": "Other Portal",
                    "portal_link_status": "client_creature_linked",
                    "creature2_id": "75509",
                    "datamapping_bridge_status": "reviewed_datamapping_creature_bridge",
                    "world_ids": "99",
                    "evidence_status": "reviewed_instance_portal_creature_pending_entry_smoke",
                    "evidence_sources": "wildstar_client_mysql/InstancePortal.tbl.sql;wildstar_client_mysql/Creature2.tbl.sql;Tools/DataMapping/output/creature_map.csv",
                    "manual_validation": "pending",
                    "completion_status": "not_retail_complete",
                    "blocker_summary": "Nonqueued portal should not appear.",
                },
                {
                    "evidence_kind": "instance_portal_definition",
                    "source_table": "InstancePortal.tbl.sql",
                    "source_row_id": "43",
                    "instance_portal_id": "43",
                    "instance_portal_name": "Definition Only Portal",
                    "portal_link_status": "definition_without_creature_link",
                    "creature2_id": "",
                    "datamapping_bridge_status": "no_client_creature_link",
                    "world_ids": "9",
                    "evidence_status": "client_instance_portal_definition_without_creature_link_blocked",
                    "evidence_sources": "wildstar_client_mysql/InstancePortal.tbl.sql",
                    "manual_validation": "pending",
                    "completion_status": "not_retail_complete",
                    "blocker_summary": "Definition-only portal should not appear.",
                },
                {
                    "evidence_kind": "instance_portal_creature",
                    "source_table": "Creature2.tbl.sql",
                    "source_row_id": "75510",
                    "instance_portal_id": "44",
                    "instance_portal_name": "Missing Bridge Portal",
                    "portal_link_status": "client_creature_linked",
                    "creature2_id": "75510",
                    "datamapping_bridge_status": "missing_datamapping_creature_bridge",
                    "world_ids": "9",
                    "evidence_status": "client_instance_portal_creature_link_pending_world_placement_review",
                    "evidence_sources": "wildstar_client_mysql/InstancePortal.tbl.sql;wildstar_client_mysql/Creature2.tbl.sql",
                    "manual_validation": "pending",
                    "completion_status": "not_retail_complete",
                    "blocker_summary": "Missing bridge portal should not appear yet.",
                },
            ],
            [
                {
                    "world_id": "9",
                    "script_paths": (
                        r"Source\NexusForever.Script.Instance\Dungeon\Focused\FocusedEventScript.cs;"
                        r"Source\NexusForever.Script.Instance\Dungeon\Focused\Script\FocusedChildScript.cs"
                    ),
                }
            ],
            [
                {
                    "script_scope": "script_instance",
                    "source_path": r"Source\NexusForever.Script.Instance\Dungeon\Focused\FocusedEventScript.cs",
                    "source_line": "25",
                    "script_owner_ids": "10",
                    "evidence_kind": "script_public_event_objective_reference",
                    "source_call": "ActivateObjective",
                    "source_symbol": "PublicEventObjective.Focused",
                    "enum_namespace": "NexusForever.Script.Template.PublicEventObjective",
                    "reference_id": "20",
                    "reference_link_status": "client_row_matched",
                    "owner_link_status": "script_owner_matches_objective_public_event",
                    "public_event_id": "10",
                    "public_event_name": "Focused Public Event",
                    "public_event_world_id": "9",
                    "public_event_world_zone_id": "6",
                    "public_event_type": "13",
                    "public_event_parent_id": "",
                    "objective_type": "0",
                    "objective_count": "1",
                    "objective_object_id": "200",
                    "objective_world_location_id": "",
                    "objective_target_group_id": "300",
                    "objective_display_order": "1",
                    "objective_text": "Focused objective",
                    "objective_short_text": "Focused",
                    "evidence_status": "script_public_event_objective_parent_event_matched_pending_activation_credit_smoke",
                    "evidence_sources": "Source/NexusForever.Script.Instance;wildstar_client_mysql/PublicEventObjective.tbl.sql",
                    "manual_validation": "pending",
                    "completion_status": "not_retail_complete",
                    "blocker_summary": "Event objective flow needs activation smoke.",
                },
                {
                    "script_scope": "script_instance",
                    "source_path": r"Source\NexusForever.Script.Instance\Dungeon\Focused\Script\FocusedChildScript.cs",
                    "source_line": "14",
                    "script_owner_ids": "10",
                    "evidence_kind": "script_public_event_phase_reference",
                    "source_call": "SetPhase",
                    "source_symbol": "PublicEventPhase.Focused",
                    "enum_namespace": "NexusForever.Script.Template.PublicEventPhase",
                    "reference_id": "2",
                    "reference_link_status": "source_expression_tracked",
                    "owner_link_status": "script_owner_matches_phase_public_event",
                    "public_event_id": "10",
                    "public_event_name": "Focused Public Event",
                    "public_event_world_id": "9",
                    "public_event_world_zone_id": "6",
                    "public_event_type": "13",
                    "public_event_parent_id": "",
                    "evidence_status": "script_public_event_phase_script_state_owner_matched_pending_phase_order_smoke",
                    "evidence_sources": "Source/NexusForever.Script.Instance;wildstar_client_mysql/PublicEvent.tbl.sql",
                    "manual_validation": "pending",
                    "completion_status": "not_retail_complete",
                    "blocker_summary": "Event phase flow needs order smoke.",
                },
                {
                    "script_scope": "script_instance",
                    "source_path": r"Source\NexusForever.Script.Instance\Dungeon\Other\OtherEventScript.cs",
                    "source_line": "26",
                    "script_owner_ids": "10",
                    "evidence_kind": "script_public_event_objective_reference",
                    "source_call": "ActivateObjective",
                    "source_symbol": "PublicEventObjective.Other",
                    "reference_id": "21",
                    "reference_link_status": "client_row_matched",
                    "owner_link_status": "script_owner_matches_objective_public_event",
                    "public_event_id": "10",
                    "public_event_name": "Focused Public Event",
                    "public_event_world_id": "9",
                    "public_event_world_zone_id": "6",
                    "evidence_status": "script_public_event_objective_parent_event_matched_pending_activation_credit_smoke",
                    "evidence_sources": "Source/NexusForever.Script.Instance;wildstar_client_mysql/PublicEventObjective.tbl.sql",
                    "manual_validation": "pending",
                    "completion_status": "not_retail_complete",
                    "blocker_summary": "Public-event-only flow should not appear.",
                },
                {
                    "script_scope": "script_instance",
                    "source_path": r"Source\NexusForever.Script.Instance\Dungeon\Focused\FocusedEventScript.cs",
                    "source_line": "28",
                    "script_owner_ids": "999",
                    "evidence_kind": "script_public_event_objective_reference",
                    "source_call": "ActivateObjective",
                    "source_symbol": "PublicEventObjective.WrongOwner",
                    "enum_namespace": "NexusForever.Script.Template.PublicEventObjective",
                    "reference_id": "9991",
                    "reference_link_status": "client_row_matched",
                    "owner_link_status": "script_owner_matches_objective_public_event",
                    "public_event_id": "999",
                    "public_event_name": "Unqueued Public Event",
                    "public_event_world_id": "9",
                    "public_event_world_zone_id": "6",
                    "evidence_status": "script_public_event_objective_parent_event_matched_pending_activation_credit_smoke",
                    "evidence_sources": "Source/NexusForever.Script.Instance;wildstar_client_mysql/PublicEventObjective.tbl.sql",
                    "manual_validation": "pending",
                    "completion_status": "not_retail_complete",
                    "blocker_summary": "Path-only event flow should not appear when the public event is not queued for this instance.",
                },
                {
                    "script_scope": "script_main",
                    "source_path": r"Source\NexusForever.Script.Main\Quests\FocusedQuest.cs",
                    "source_line": "27",
                    "script_owner_ids": "2",
                    "evidence_kind": "script_public_event_objective_reference",
                    "source_call": "ActivateObjective",
                    "source_symbol": "PublicEventObjective.Quest",
                    "reference_id": "22",
                    "reference_link_status": "client_row_matched",
                    "owner_link_status": "script_owner_matches_objective_public_event",
                    "public_event_id": "10",
                    "evidence_status": "script_public_event_objective_parent_event_matched_pending_activation_credit_smoke",
                    "evidence_sources": "Source/NexusForever.Script.Main;wildstar_client_mysql/PublicEventObjective.tbl.sql",
                    "manual_validation": "pending",
                    "completion_status": "not_retail_complete",
                    "blocker_summary": "Main-script event flow should not appear.",
                }
            ],
            [
                {
                    "script_scope": "script_instance",
                    "source_path": r"Source\NexusForever.Script.Instance\Dungeon\Focused\FocusedEventScript.cs",
                    "source_line": "24",
                    "script_owner_ids": "10;11",
                    "evidence_kind": "script_communicator_message",
                    "source_call": "ShowCommunicatorMessage",
                    "source_symbol": "CommunicatorMessage.Focused",
                    "reference_id": "1234",
                    "client_link_status": "client_row_matched",
                    "client_text": "Focused message",
                    "client_world_id": "9",
                    "client_world_zone_id": "6",
                    "client_creature_id": "200",
                    "runtime_payload_status": "",
                    "evidence_status": "script_communicator_message_client_row_matched_pending_trigger_targeting_ui_smoke",
                    "evidence_sources": "Source/NexusForever.Script.Instance;wildstar_client_mysql/CommunicatorMessages.tbl.sql",
                    "manual_validation": "pending",
                    "completion_status": "not_retail_complete",
                    "blocker_summary": "Communicator message needs UI smoke.",
                },
                {
                    "script_scope": "script_instance",
                    "source_path": r"Source\NexusForever.Script.Instance\Dungeon\Focused\Script\FocusedChildScript.cs",
                    "source_line": "13",
                    "script_owner_ids": "200",
                    "evidence_kind": "script_cinematic_reference",
                    "source_call": "CreateCinematic",
                    "source_symbol": "Cinematic.Focused",
                    "reference_id": "0",
                    "client_link_status": "zero_runtime_cinematic_id",
                    "client_text": "",
                    "client_world_id": "9",
                    "client_world_zone_id": "",
                    "client_creature_id": "",
                    "cinematic_interface": "IFocusedCinematic",
                    "cinematic_implementation_class": "FocusedCinematic",
                    "cinematic_implementation_path": r"Source\NexusForever.Script.Instance\Dungeon\Focused\FocusedCinematic.cs",
                    "cinematic_duration_ms": "12000",
                    "runtime_payload_status": "runtime_placeholder_or_wip_payload",
                    "evidence_status": "script_cinematic_placeholder_pending_payload_mapping",
                    "evidence_sources": "Source/NexusForever.Script.Instance;wildstar_client_mysql/Cinematic.tbl.sql",
                    "manual_validation": "pending",
                    "completion_status": "not_retail_complete",
                    "blocker_summary": "Cinematic placeholder needs payload mapping.",
                },
                {
                    "script_scope": "script_instance",
                    "source_path": r"Source\NexusForever.Script.Instance\Dungeon\Other\OtherEventScript.cs",
                    "source_line": "987",
                    "script_owner_ids": "10",
                    "evidence_kind": "script_communicator_message",
                    "source_call": "ShowCommunicatorMessage",
                    "source_symbol": "CommunicatorMessage.Other",
                    "reference_id": "9999",
                    "client_link_status": "client_row_matched",
                    "client_text": "Other message",
                    "runtime_payload_status": "",
                    "evidence_status": "script_communicator_message_client_row_matched_pending_trigger_targeting_ui_smoke",
                    "evidence_sources": "Source/NexusForever.Script.Instance;wildstar_client_mysql/CommunicatorMessages.tbl.sql",
                    "manual_validation": "pending",
                    "completion_status": "not_retail_complete",
                    "blocker_summary": "Owner-only presentation should not appear.",
                },
                {
                    "script_scope": "script_main",
                    "source_path": r"Source\NexusForever.Script.Main\Quests\FocusedQuest.cs",
                    "source_line": "15",
                    "script_owner_ids": "2",
                    "evidence_kind": "script_communicator_message",
                    "source_call": "ShowCommunicatorMessage",
                    "source_symbol": "CommunicatorMessage.Quest",
                    "reference_id": "8888",
                    "client_link_status": "client_row_matched",
                    "client_text": "Quest message",
                    "runtime_payload_status": "",
                    "evidence_status": "script_communicator_message_client_row_matched_pending_trigger_targeting_ui_smoke",
                    "evidence_sources": "Source/NexusForever.Script.Main;wildstar_client_mysql/CommunicatorMessages.tbl.sql",
                    "manual_validation": "pending",
                    "completion_status": "not_retail_complete",
                    "blocker_summary": "Main-script presentation should not appear in instance blockers.",
                }
            ],
            [
                {
                    "script_scope": "script_instance",
                    "source_path": r"Source\NexusForever.Script.Instance\Dungeon\Focused\FocusedEventScript.cs",
                    "source_line": "22",
                    "source_call": "ActivateObjective",
                    "producer_targeting": "direct_objective",
                    "objective_id": "20",
                    "objective_type_id": "0",
                    "objective_type_name": "Script",
                    "object_id": "200",
                    "update_count": "1",
                    "matched_public_event_objective_ids": "20",
                    "matched_public_event_ids": "10",
                    "reference_link_status": "direct_objective_row_matched",
                    "owner_link_status": "script_owner_matches_public_event",
                    "evidence_status": "script_objective_producer_matched_pending_credit_timing_smoke",
                    "evidence_sources": "Source/NexusForever.Script.Instance;wildstar_client_mysql/PublicEventObjective.tbl.sql",
                    "manual_validation": "pending",
                    "completion_status": "not_retail_complete",
                    "blocker_summary": "Scripted producer needs timing smoke.",
                },
                {
                    "script_scope": "script_instance",
                    "source_path": r"Source\NexusForever.Script.Instance\Dungeon\Focused\FocusedEventScript.cs",
                    "source_line": "23",
                    "source_call": "IncrementObjective",
                    "source_symbol": "PublicEventObjective.ScriptBroad",
                    "producer_targeting": "typed_objective",
                    "objective_id": "999",
                    "objective_type_id": "9",
                    "objective_type_name": "ScriptWithoutCount",
                    "object_id": "0",
                    "update_count": "1",
                    "matched_public_event_objective_ids": "20;21;+223",
                    "matched_public_event_ids": "10;11;+223",
                    "reference_link_status": "typed_objective_broad_match",
                    "owner_link_status": "script_owner_matches_public_event",
                    "evidence_status": "script_objective_producer_matched_pending_credit_timing_smoke",
                    "evidence_sources": "Source/NexusForever.Script.Instance;wildstar_client_mysql/PublicEventObjective.tbl.sql",
                    "manual_validation": "pending",
                    "completion_status": "not_retail_complete",
                    "blocker_summary": "Broad typed producer should stay out of blocker expansion.",
                }
            ],
            [
                {
                    "script_scope": "script_instance",
                    "source_path": r"Source\NexusForever.Script.Instance\Dungeon\Focused\FocusedEventScript.cs",
                    "source_line": "30",
                    "script_owner_ids": "10;11",
                    "script_filter_name": "FocusedEventScript",
                    "source_call": "Finish",
                    "action_category": "script_public_event_finish",
                    "reference_kind": "public_event_team",
                    "source_symbol": "Team.Blue",
                    "reference_id": "",
                    "reference_link_status": "source_expression_tracked",
                    "client_reference_status": "public_event_team_expression_tracked",
                    "client_reference_name": "Team.Blue",
                    "source_trace_status": "public_event_finish_team_source_tracked",
                    "source_trace_detail": "Team.Blue",
                    "evidence_status": "script_public_event_finish_pending_reward_completion_smoke",
                    "evidence_sources": "Source/NexusForever.Script.Instance",
                    "manual_validation": "pending",
                    "completion_status": "not_retail_complete",
                    "blocker_summary": "Runtime finish action needs reward/completion smoke.",
                },
                {
                    "script_scope": "script_instance",
                    "source_path": r"Source\NexusForever.Script.Instance\Dungeon\Focused\Script\FocusedChildScript.cs",
                    "source_line": "12",
                    "script_owner_ids": "200",
                    "script_filter_name": "FocusedChildScript",
                    "source_call": "CastSpell",
                    "action_category": "script_spell_cast",
                    "reference_kind": "spell4_id",
                    "source_symbol": "FocusedSpell",
                    "reference_id": "81640",
                    "reference_link_status": "numeric_constant_resolved",
                    "client_reference_status": "spell4_row_matched",
                    "client_reference_name": "Focused Spell",
                    "client_reference_sources": "wildstar_client_mysql/Spell4.tbl.sql",
                    "source_trace_status": "literal_or_constant_client_reference_tracked",
                    "source_trace_detail": "reference_id=81640;client_reference_status=spell4_row_matched",
                    "source_trace_sources": "Source/NexusForever.Script.Instance;wildstar_client_mysql/Spell4.tbl.sql",
                    "evidence_status": "script_spell_cast_pending_spell_effect_targeting_smoke",
                    "evidence_sources": "Source/NexusForever.Script.Instance",
                    "manual_validation": "pending",
                    "completion_status": "not_retail_complete",
                    "blocker_summary": "Child script spell action needs targeting smoke.",
                },
                {
                    "script_scope": "script_instance",
                    "source_path": r"Source\NexusForever.Script.Instance\Dungeon\Other\OtherEventScript.cs",
                    "source_line": "31",
                    "script_owner_ids": "11",
                    "script_filter_name": "OtherEventScript",
                    "source_call": "Finish",
                    "action_category": "script_public_event_finish",
                    "reference_kind": "public_event_team",
                    "source_symbol": "Team.Red",
                    "reference_id": "",
                    "reference_link_status": "source_expression_tracked",
                    "client_reference_status": "public_event_team_expression_tracked",
                    "client_reference_name": "Team.Red",
                    "source_trace_status": "public_event_finish_team_source_tracked",
                    "source_trace_detail": "Team.Red",
                    "evidence_status": "script_public_event_finish_pending_reward_completion_smoke",
                    "evidence_sources": "Source/NexusForever.Script.Instance",
                    "manual_validation": "pending",
                    "completion_status": "not_retail_complete",
                    "blocker_summary": "Nonqueued runtime action should not appear.",
                },
            ],
            [
                {
                    "world_id": "9",
                    "dependency_type": "entity",
                    "dependency_id": "1100300001",
                    "entity_id": "1100300001",
                    "creature_id": "75508",
                    "event_id": "907",
                    "phase": "1",
                    "script_name": "FocusedEntityScript",
                    "evidence_status": "reviewed_laughingws_instance_entity_pending_spawn_and_mechanics_smoke",
                    "evidence_sources": "Tools/DataMapping/sql/laughingws_instance_entity_wip_seed.sql",
                    "manual_validation": "pending",
                    "completion_status": "not_retail_complete",
                    "blocker_summary": "Instance entity needs spawn smoke.",
                }
            ],
            [
                {
                    "world_id": "9",
                    "reward_source": "world_reward_rotation_content",
                    "source_id": "9",
                    "reward_rotation_content_id": "12",
                    "item2_id": "",
                    "currency_type_id": "",
                    "evidence_status": "client_world_reward_rotation_content_pending_schedule_claim_smoke",
                    "evidence_sources": "wildstar_client_mysql/RewardRotationContent.tbl.sql",
                    "manual_validation": "pending",
                    "completion_status": "not_retail_complete",
                    "blocker_summary": "Instance reward needs claim smoke.",
                }
            ],
            [
                {
                    "world_id": "9",
                    "script_path": r"Source\NexusForever.Script.Instance\Dungeon\Focused\FocusedEventScript.cs",
                    "source_line": "16",
                    "script_class": "FocusedEventScript",
                    "handler_name": "OnActivate",
                    "handler_category": "script_lifecycle",
                    "script_owner_ids": "10",
                    "evidence_status": "instance_script_lifecycle_handler_pending_mechanics_smoke",
                    "evidence_sources": "Source/NexusForever.Script.Instance",
                    "manual_validation": "pending",
                    "completion_status": "not_retail_complete",
                    "blocker_summary": "Instance script handler needs mechanics smoke.",
                }
            ],
        )

        self.assertEqual(
            [
                "quest_reward_evidence",
                "quest_objective",
                "quest_objective_evidence",
                "quest_zone_evidence",
                "quest_creature",
                "quest_world_dependency",
                "quest_achievement",
                "quest_prerequisite",
                "quest_loot",
                "quest_reward",
                "script_quest_progression",
                "quest_script",
                "quest_episode_evidence",
                "instance_dependency",
                "instance_portal_evidence",
                "public_event_evidence",
                "public_event_evidence",
                "public_event_evidence",
                "script_presentation",
                "script_presentation",
                "script_event_flow",
                "script_event_flow",
                "script_objective_producer",
                "script_runtime_action",
                "script_runtime_action",
                "instance_entity",
                "instance_reward",
                "instance_script_handler",
            ],
            [row["blocker_category"] for row in blockers],
        )
        self.assertEqual(list(range(1, 14)), [row["blocker_rank"] for row in blockers if row["slice_id"] == "quest-2"])
        self.assertIn("source_reward_id=5289", blockers[0]["source_row_key"])
        self.assertIn("5289:13327:2477", blockers[0]["related_ids"])
        self.assertIn("5289:13327:2477->8569", blockers[0]["related_ids"])
        self.assertEqual(audit.QUEST_OBJECTIVE_CSV_NAME, blockers[1]["source_inventory"])
        self.assertEqual("5", blockers[1]["blocker_type"])
        self.assertIn("objective_id=418", blockers[1]["source_row_key"])
        self.assertIn("objective_type=5", blockers[1]["source_row_key"])
        self.assertIn("world_location_indicator_ids=870;871", blockers[1]["source_row_key"])
        self.assertIn("localized_text_full=Activate the focused object.", blockers[1]["related_ids"])
        self.assertIn("target_group_id_reward_pane=300", blockers[1]["related_ids"])
        self.assertIn("QuestObjective row end to end", blockers[1]["next_action"])
        self.assertNotIn("objective_id=9900", ";".join(row["source_row_key"] for row in blockers))
        self.assertIn("418@0:unmatched", blockers[2]["related_ids"])
        self.assertIn("quest_zones:77:5>6", blockers[3]["related_ids"])
        self.assertNotIn("source_relation_id=78", ";".join(row["source_row_key"] for row in blockers))
        self.assertIn("starter:88:23645:unmatched", blockers[4]["related_ids"])
        self.assertEqual(audit.QUEST_WORLD_DEPENDENCY_CSV_NAME, blockers[5]["source_inventory"])
        self.assertEqual("objective_indicator_location", blockers[5]["blocker_type"])
        self.assertIn("dependency_id=418:worldLocationsIdIndicator00:870", blockers[5]["source_row_key"])
        self.assertIn("world_location_id=870", blockers[5]["source_row_key"])
        self.assertIn("world_zone_name=Northern Wilds", blockers[5]["related_ids"])
        self.assertIn("path-arrow guidance", blockers[5]["next_action"])
        self.assertNotIn("dependency_id=7288", ";".join(row["source_row_key"] for row in blockers))
        self.assertEqual(audit.QUEST_ACHIEVEMENT_CSV_NAME, blockers[6]["source_inventory"])
        self.assertEqual("achievement_checklist", blockers[6]["blocker_type"])
        self.assertIn("achievement_id=300", blockers[6]["source_row_key"])
        self.assertIn("checklist_id=400", blockers[6]["source_row_key"])
        self.assertIn("achievement_title=Focused achievement", blockers[6]["related_ids"])
        self.assertIn("achievement UI updates", blockers[6]["next_action"])
        self.assertEqual(audit.QUEST_PREREQUISITE_CSV_NAME, blockers[7]["source_inventory"])
        self.assertEqual("required_quest", blockers[7]["blocker_type"])
        self.assertIn("dependency_type=required_quest", blockers[7]["source_row_key"])
        self.assertIn("dependency_id=1", blockers[7]["source_row_key"])
        self.assertIn("localized_failure_text=Requires previous quest.", blockers[7]["related_ids"])
        self.assertIn("accept/deny behavior", blockers[7]["next_action"])
        self.assertEqual(audit.QUEST_LOOT_CSV_NAME, blockers[8]["source_inventory"])
        self.assertEqual("loot_item", blockers[8]["blocker_type"])
        self.assertIn("loot_group_id=1200000001", blockers[8]["source_row_key"])
        self.assertIn("static_id=265", blockers[8]["source_row_key"])
        self.assertIn("objective_name=Focused loot objective", blockers[8]["related_ids"])
        self.assertIn("client-visible loot smoke", blockers[8]["next_action"])
        self.assertNotIn("loot_group_id=1200000099", ";".join(row["source_row_key"] for row in blockers))
        self.assertEqual(audit.QUEST_REWARD_CSV_NAME, blockers[9]["source_inventory"])
        self.assertEqual("Quest2Reward", blockers[9]["blocker_type"])
        self.assertIn("reward_id=300", blockers[9]["source_row_key"])
        self.assertIn("object_id=13327", blockers[9]["source_row_key"])
        self.assertIn("reward_source=Quest2Reward", blockers[9]["related_ids"])
        self.assertIn("reward presentation UI", blockers[9]["next_action"])
        self.assertIn("quest_reward_evidence", blockers[9]["next_action"])
        self.assertNotIn("reward_id=990", ";".join(row["source_row_key"] for row in blockers))
        self.assertEqual(audit.SCRIPT_QUEST_PROGRESSION_EVIDENCE_CSV_NAME, blockers[10]["source_inventory"])
        self.assertEqual("script_quest_objective_update", blockers[10]["blocker_type"])
        self.assertIn("quest_id=2", blockers[10]["source_row_key"])
        self.assertIn("objective_id=418", blockers[10]["source_row_key"])
        self.assertIn("matched_quest_objective_ids=418", blockers[10]["related_ids"])
        self.assertIn("source_trace_status=literal_or_constant_client_reference_tracked", blockers[10]["related_ids"])
        self.assertIn("quest progression hook trigger timing", blockers[10]["next_action"])
        self.assertNotIn("source_symbol=info<-questId", ";".join(row["source_row_key"] for row in blockers))
        self.assertNotIn("quest_id=99", ";".join(row["source_row_key"] for row in blockers))
        self.assertEqual(audit.QUEST_SCRIPT_CSV_NAME, blockers[11]["source_inventory"])
        self.assertEqual("quest_script", blockers[11]["blocker_type"])
        self.assertIn("script_class=FocusedQuestScript", blockers[11]["source_row_key"])
        self.assertIn(r"script_path=Source\NexusForever.Script.Main\Quests\FocusedQuest.cs", blockers[11]["source_row_key"])
        self.assertIn("owner_ids_on_attribute=2", blockers[11]["related_ids"])
        self.assertIn("quest script lifecycle", blockers[11]["next_action"])
        self.assertNotIn("script_class=OtherQuestScript", ";".join(row["source_row_key"] for row in blockers))
        self.assertNotIn("script_kind=map_script", ";".join(row["source_row_key"] for row in blockers))
        self.assertEqual(audit.QUEST_EPISODE_EVIDENCE_CSV_NAME, blockers[12]["source_inventory"])
        self.assertEqual("matched", blockers[12]["blocker_type"])
        self.assertIn("episode_quest_id=3000", blockers[12]["source_row_key"])
        self.assertIn("episode_id=314", blockers[12]["source_row_key"])
        self.assertIn("order_index=2", blockers[12]["source_row_key"])
        self.assertIn("episode_name=Focused episode", blockers[12]["related_ids"])
        self.assertIn("client_world_zone_name=Northern Wilds", blockers[12]["related_ids"])
        self.assertIn("episode UI/progression presentation", blockers[12]["next_action"])
        self.assertNotIn("episode_quest_id=9990", ";".join(row["source_row_key"] for row in blockers))
        self.assertIn("dependency_type=public_event_objective", blockers[13]["related_ids"])
        self.assertEqual(audit.INSTANCE_PORTAL_EVIDENCE_CSV_NAME, blockers[14]["source_inventory"])
        self.assertEqual("instance_portal_creature", blockers[14]["blocker_type"])
        self.assertIn("instance_portal_id=41", blockers[14]["source_row_key"])
        self.assertIn("source_row_id=75508", blockers[14]["source_row_key"])
        self.assertIn("world_id=9", blockers[14]["source_row_key"])
        self.assertIn("instance_portal_name=Focused Portal", blockers[14]["related_ids"])
        self.assertIn("datamapping_bridge_status=reviewed_datamapping_creature_bridge", blockers[14]["related_ids"])
        self.assertIn("entry/exit behavior", blockers[14]["next_action"])
        self.assertNotIn("instance_portal_id=42", ";".join(row["source_row_key"] for row in blockers))
        self.assertNotIn("instance_portal_id=43", ";".join(row["source_row_key"] for row in blockers))
        self.assertNotIn("instance_portal_id=44", ";".join(row["source_row_key"] for row in blockers))
        self.assertEqual(audit.PUBLIC_EVENT_EVIDENCE_CSV_NAME, blockers[15]["source_inventory"])
        self.assertEqual("public_event_creature", blockers[15]["blocker_type"])
        self.assertIn("public_event_id=10", blockers[15]["source_row_key"])
        self.assertIn("related_client_id=75508", blockers[15]["source_row_key"])
        self.assertIn("related_source_name=Focused source creature", blockers[15]["related_ids"])
        self.assertIn("creature bridge/spawn/objective credit", blockers[15]["next_action"])
        self.assertEqual("public_event_zone", blockers[16]["blocker_type"])
        self.assertIn("source_map=public_event_zone_map.csv", blockers[16]["source_row_key"])
        self.assertIn("world_zone_id=6", blockers[16]["related_ids"])
        self.assertEqual("public_event_objective", blockers[17]["blocker_type"])
        self.assertIn("match_status=unmatched", blockers[17]["source_row_key"])
        self.assertIn("target_group_id=300", blockers[17]["related_ids"])
        self.assertNotIn("source_row_id=503", ";".join(row["source_row_key"] for row in blockers))
        self.assertNotIn("public_event_id=11", ";".join(row["source_row_key"] for row in blockers))
        self.assertEqual(audit.SCRIPT_PRESENTATION_EVIDENCE_CSV_NAME, blockers[18]["source_inventory"])
        self.assertEqual("script_communicator_message", blockers[18]["blocker_type"])
        self.assertIn("client_link_status=client_row_matched", blockers[18]["related_ids"])
        self.assertIn("client_text=Focused message", blockers[18]["related_ids"])
        self.assertIn("communicator/cinematic trigger targeting", blockers[18]["next_action"])
        self.assertIn("reference_id=0", blockers[19]["source_row_key"])
        self.assertIn("runtime_payload_status=runtime_placeholder_or_wip_payload", blockers[19]["related_ids"])
        self.assertNotIn("source_line=987", ";".join(row["source_row_key"] for row in blockers))
        self.assertNotIn("source_line=15", ";".join(row["source_row_key"] for row in blockers))
        self.assertEqual(audit.SCRIPT_EVENT_FLOW_EVIDENCE_CSV_NAME, blockers[20]["source_inventory"])
        self.assertEqual("script_public_event_objective_reference", blockers[20]["blocker_type"])
        self.assertIn("public_event_id=10", blockers[20]["related_ids"])
        self.assertIn("enum_namespace=NexusForever.Script.Template.PublicEventObjective", blockers[20]["related_ids"])
        self.assertIn("objective_text=Focused objective", blockers[20]["related_ids"])
        self.assertIn("reference_id=20", blockers[20]["source_row_key"])
        self.assertIn("objective/phase flow", blockers[20]["next_action"])
        self.assertEqual("script_public_event_phase_reference", blockers[21]["blocker_type"])
        self.assertIn("source_call=SetPhase", blockers[21]["related_ids"])
        self.assertIn("enum_namespace=NexusForever.Script.Template.PublicEventPhase", blockers[21]["related_ids"])
        self.assertIn("reference_id=2", blockers[21]["source_row_key"])
        self.assertNotIn("source_line=28", ";".join(row["source_row_key"] for row in blockers))
        self.assertNotIn("source_line=26", ";".join(row["source_row_key"] for row in blockers))
        self.assertNotIn("source_line=27", ";".join(row["source_row_key"] for row in blockers))
        self.assertIn("matched_public_event_ids=10", blockers[22]["related_ids"])
        self.assertIn("objective_id=20", blockers[22]["source_row_key"])
        self.assertNotIn("objective_id=999", ";".join(row["source_row_key"] for row in blockers))
        self.assertIn("action_category=script_public_event_finish", blockers[23]["related_ids"])
        self.assertIn("source_call=Finish", blockers[23]["source_row_key"])
        self.assertNotIn("source_line=31", ";".join(row["source_row_key"] for row in blockers))
        self.assertEqual(audit.SCRIPT_RUNTIME_ACTION_EVIDENCE_CSV_NAME, blockers[23]["source_inventory"])
        self.assertEqual("script_public_event_finish", blockers[23]["blocker_type"])
        self.assertIn("reward/completion side effects", blockers[23]["next_action"])
        self.assertIn("action_category=script_spell_cast", blockers[24]["related_ids"])
        self.assertIn("reference_id=81640", blockers[24]["source_row_key"])
        self.assertIn("wildstar_client_mysql/Spell4.tbl.sql", blockers[24]["evidence_sources"])
        self.assertIn("creature_id=75508", blockers[25]["related_ids"])
        self.assertIn("reward_rotation_content_id=12", blockers[26]["related_ids"])
        self.assertIn("handler_name=OnActivate", blockers[27]["related_ids"])
        self.assertTrue(all(row["completion_status"] == "not_retail_complete" for row in blockers))

    def test_instance_entity_rows_expand_laughingws_seed_tables_and_updates(self) -> None:
        sql_root = Path(tempfile.mkdtemp(prefix="nf-content-audit-instance-"))
        try:
            seed_path = sql_root / "instance_entities.sql"
            seed_path.write_text(
                """
--   42: Example Boss; source creature 9001; event 100 phase 2; branch-authored WIP placement.
INSERT INTO `entity` (`id`, `type`, `creature`, `world`, `area`, `x`, `y`, `z`, `rx`, `ry`, `rz`, `displayInfo`, `outfitInfo`, `faction1`, `faction2`, `questChecklistIdx`, `activePropId`, `worldSocketId`, `mode`) VALUES
  (42, 0, 9001, 1, 2, 1.5, 2.5, 3.5, 0, 0, 0, 111, 222, 3, 4, 5, 6, 7, 8)
ON DUPLICATE KEY UPDATE `type` = VALUES(`type`), `creature` = VALUES(`creature`);
INSERT INTO `entity_event` (`id`, `eventId`, `phase`) VALUES
  (42, 100, 2)
ON DUPLICATE KEY UPDATE `id` = VALUES(`id`);
INSERT INTO `entity_script` (`id`, `scriptName`) VALUES
  (42, 'ExampleBossEntityScript')
ON DUPLICATE KEY UPDATE `id` = VALUES(`id`);
INSERT INTO `entity_stats` (`id`, `stat`, `value`) VALUES
  (42, 10, 50)
ON DUPLICATE KEY UPDATE `value` = VALUES(`value`);
--   Example official area repair.
UPDATE `entity`
SET `area` = 9
WHERE `world` = 1 AND `creature` = 9002 AND ABS(`x` - (10.0)) < 0.001 AND ABS(`y` - (20.0)) < 0.001 AND ABS(`z` - (30.0)) < 0.001;
""".lstrip(),
                encoding="utf-8",
            )
            instance_rows = [
                {
                    "content_type": "dungeon",
                    "world_id": 1,
                    "name": "Example Instance",
                    "evidence_status": "mapped_only_pending_manual_smoke",
                    "blocker_summary": "pending smoke",
                }
            ]

            rows = audit.build_instance_entity_rows(instance_rows, seed_path)

            self.assertEqual(5, len(rows))
            self.assertEqual(
                {"entity", "entity_event", "entity_script", "entity_stats", "official_entity_reconciliation"},
                {row["dependency_type"] for row in rows},
            )
            entity_row = next(row for row in rows if row["dependency_type"] == "entity")
            self.assertEqual("100:2", entity_row["related_events"])
            self.assertEqual("ExampleBossEntityScript", entity_row["related_scripts"])
            self.assertEqual("10=50", entity_row["related_stats"])
            self.assertIn("Example Boss", entity_row["source_comment"])
            update_row = next(row for row in rows if row["dependency_type"] == "official_entity_reconciliation")
            self.assertEqual("area", update_row["reconciliation_field"])
            self.assertEqual("9", update_row["reconciliation_value"])
            self.assertTrue(all(row["completion_status"] == "not_retail_complete" for row in rows))
        finally:
            shutil.rmtree(sql_root, ignore_errors=True)

    def test_q3963_target_group_dependency_uses_ship_controls_evidence_override(self) -> None:
        strings = {100: "More Important Than Revenge", 201: "Ship Controls"}
        quest = {
            "ID": "3963",
            "localizedTextIdTitle": "100",
            "worldZoneId": "0",
            "WorldLocation2IdReceiver": "0",
            "questDirectionIdCompletion": "0",
            "worldLocation2IdAltReceiver00": "0",
            "prerequisiteIdAltReceiver00": "0",
            "questDirectionIdAltReceiver00": "0",
            "worldLocation2IdAltReceiver01": "0",
            "prerequisiteIdAltReceiver01": "0",
            "questDirectionIdAltReceiver01": "0",
            "worldLocation2IdAltReceiver02": "0",
            "prerequisiteIdAltReceiver02": "0",
            "questDirectionIdAltReceiver02": "0",
            "objective0": "5201",
            "objective01": "0",
            "objective02": "0",
            "objective03": "0",
            "objective04": "0",
            "objective05": "0",
        }
        objective = {
            "ID": "5201",
            "type": "5",
            "data": "0",
            "count": "1",
            "worldLocationsIdIndicator00": "0",
            "worldLocationsIdIndicator01": "0",
            "worldLocationsIdIndicator02": "0",
            "worldLocationsIdIndicator03": "0",
            "targetGroupIdRewardPane": "7573",
            "questDirectionId": "0",
        }
        target_group = {
            "ID": "7573",
            "localizedTextIdDisplayString": "201",
            "type": "1",
            "data0": "27196",
            "data1": "0",
            "data2": "0",
            "data3": "0",
            "data4": "0",
            "data5": "0",
            "data6": "0",
        }

        rows = audit.build_quest_world_dependency_rows(
            strings,
            [quest],
            [objective],
            [{"quest_id": 3963, "bucket": "generic_with_script", "completion_status": "not_retail_complete"}],
            [],
            [],
            [target_group],
            [],
            [],
            runtime_entity_rows=[],
        )

        target_group_row = next(row for row in rows if row["dependency_type"] == "target_group")
        self.assertEqual(
            "runtime_q3963_ship_controls_target_group_spawn_and_activate_credit_tested_pending_client_smoke",
            target_group_row["evidence_status"],
        )
        self.assertIn("WorldLocationTeleporterEntityScript.cs", target_group_row["evidence_sources"])
        self.assertIn("source_coordinate_ids 5168 and 40678", target_group_row["blocker_summary"])

        member_row = next(row for row in rows if row["dependency_type"] == "target_group_member")
        self.assertEqual(
            "runtime_q3963_ship_controls_target_group_member_spawn_and_activate_credit_tested_pending_client_smoke",
            member_row["evidence_status"],
        )

    def test_quest_objective_rows_preserve_client_dependency_status(self) -> None:
        strings = {100: "Quest Title", 200: "Full objective", 201: "Short objective", 202: "Reward pane objective", 203: "Enter area objective", 204: "Holdout objective", 205: "Signal Flares", 206: "Rescue survivors", 207: "Follow the power cables", 208: "Collect Loftite Crystal Fragments", 209: "Talk to Scientist Lusk", 210: "Rescue Exile Prisoners", 211: "Kill Skeech", 212: "Kill Rootbrutes and yeti", 213: "Recover Exile Supplies"}
        quest_rows = [
            {
                "ID": "10",
                "localizedTextIdTitle": "100",
                "objective0": "300",
                "objective01": "301",
                "objective02": "302",
                "objective03": "0",
                "objective04": "0",
                "objective05": "0",
            },
            {
                "ID": "3673",
                "localizedTextIdTitle": "100",
                "objective0": "4748",
                "objective01": "4888",
                "objective02": "4889",
                "objective03": "4890",
                "objective04": "13391",
                "objective05": "0",
            },
            {
                "ID": "4696",
                "localizedTextIdTitle": "100",
                "objective0": "6508",
                "objective01": "6457",
                "objective02": "0",
                "objective03": "0",
                "objective04": "0",
                "objective05": "0",
            },
            {
                "ID": "4694",
                "localizedTextIdTitle": "100",
                "objective0": "16115",
                "objective01": "16116",
                "objective02": "0",
                "objective03": "0",
                "objective04": "0",
                "objective05": "0",
            },
            {
                "ID": "3479",
                "localizedTextIdTitle": "100",
                "objective0": "4467",
                "objective01": "4564",
                "objective02": "0",
                "objective03": "0",
                "objective04": "0",
                "objective05": "0",
            },
            {
                "ID": "3480",
                "localizedTextIdTitle": "100",
                "objective0": "4470",
                "objective01": "4565",
                "objective02": "0",
                "objective03": "0",
                "objective04": "0",
                "objective05": "0",
            },
            {
                "ID": "3486",
                "localizedTextIdTitle": "100",
                "objective0": "4987",
                "objective01": "4485",
                "objective02": "0",
                "objective03": "0",
                "objective04": "0",
                "objective05": "0",
            },
            {
                "ID": "3741",
                "localizedTextIdTitle": "100",
                "objective0": "4813",
                "objective01": "0",
                "objective02": "0",
                "objective03": "0",
                "objective04": "0",
                "objective05": "0",
            },
            {
                "ID": "3797",
                "localizedTextIdTitle": "100",
                "objective0": "4918",
                "objective01": "0",
                "objective02": "0",
                "objective03": "0",
                "objective04": "0",
                "objective05": "0",
            },
            {
                "ID": "3668",
                "localizedTextIdTitle": "100",
                "objective0": "4744",
                "objective01": "4745",
                "objective02": "4791",
                "objective03": "0",
                "objective04": "0",
                "objective05": "0",
            },
        ]
        objective_rows = [
            {
                "ID": "300",
                "type": "2",
                "flags": "8",
                "data": "500",
                "count": "3",
                "localizedTextIdFull": "200",
                "worldLocationsIdIndicator00": "900",
                "worldLocationsIdIndicator01": "0",
                "worldLocationsIdIndicator02": "0",
                "worldLocationsIdIndicator03": "0",
                "maxTimeAllowedMS": "60000",
                "localizedTextIdShort": "201",
                "targetGroupIdRewardPane": "700",
                "questDirectionId": "800",
            },
            {
                "ID": "301",
                "type": "5",
                "flags": "0",
                "data": "0",
                "count": "1",
                "localizedTextIdFull": "202",
                "worldLocationsIdIndicator00": "0",
                "worldLocationsIdIndicator01": "0",
                "worldLocationsIdIndicator02": "0",
                "worldLocationsIdIndicator03": "0",
                "maxTimeAllowedMS": "0",
                "localizedTextIdShort": "0",
                "targetGroupIdRewardPane": "4323",
                "questDirectionId": "0",
            },
            {
                "ID": "302",
                "type": "22",
                "flags": "0",
                "data": "1931",
                "count": "1",
                "localizedTextIdFull": "203",
                "worldLocationsIdIndicator00": "12649",
                "worldLocationsIdIndicator01": "0",
                "worldLocationsIdIndicator02": "0",
                "worldLocationsIdIndicator03": "0",
                "maxTimeAllowedMS": "0",
                "localizedTextIdShort": "0",
                "targetGroupIdRewardPane": "0",
                "questDirectionId": "0",
            },
            {
                "ID": "6508",
                "type": "5",
                "flags": "0",
                "data": "0",
                "count": "5",
                "localizedTextIdFull": "202",
                "worldLocationsIdIndicator00": "0",
                "worldLocationsIdIndicator01": "0",
                "worldLocationsIdIndicator02": "0",
                "worldLocationsIdIndicator03": "0",
                "maxTimeAllowedMS": "0",
                "localizedTextIdShort": "0",
                "targetGroupIdRewardPane": "4323",
                "questDirectionId": "0",
            },
            {
                "ID": "6457",
                "type": "22",
                "flags": "3",
                "data": "1931",
                "count": "1",
                "localizedTextIdFull": "203",
                "worldLocationsIdIndicator00": "12649",
                "worldLocationsIdIndicator01": "0",
                "worldLocationsIdIndicator02": "0",
                "worldLocationsIdIndicator03": "0",
                "maxTimeAllowedMS": "0",
                "localizedTextIdShort": "0",
                "targetGroupIdRewardPane": "0",
                "questDirectionId": "0",
            },
            {
                "ID": "16115",
                "type": "5",
                "flags": "32",
                "data": "50904",
                "count": "1",
                "localizedTextIdFull": "204",
                "worldLocationsIdIndicator00": "12649",
                "worldLocationsIdIndicator01": "0",
                "worldLocationsIdIndicator02": "0",
                "worldLocationsIdIndicator03": "0",
                "maxTimeAllowedMS": "0",
                "localizedTextIdShort": "0",
                "targetGroupIdRewardPane": "7407",
                "questDirectionId": "0",
            },
            {
                "ID": "16116",
                "type": "25",
                "flags": "0",
                "data": "111",
                "count": "1",
                "localizedTextIdFull": "204",
                "worldLocationsIdIndicator00": "12649",
                "worldLocationsIdIndicator01": "0",
                "worldLocationsIdIndicator02": "0",
                "worldLocationsIdIndicator03": "0",
                "maxTimeAllowedMS": "0",
                "localizedTextIdShort": "0",
                "targetGroupIdRewardPane": "0",
                "questDirectionId": "0",
            },
            {
                "ID": "4748",
                "type": "14",
                "flags": "0",
                "data": "1106",
                "count": "3",
                "localizedTextIdFull": "205",
                "worldLocationsIdIndicator00": "0",
                "worldLocationsIdIndicator01": "0",
                "worldLocationsIdIndicator02": "0",
                "worldLocationsIdIndicator03": "0",
                "maxTimeAllowedMS": "0",
                "localizedTextIdShort": "0",
                "targetGroupIdRewardPane": "0",
                "questDirectionId": "0",
            },
            {
                "ID": "4888",
                "type": "12",
                "flags": "40",
                "data": "12521",
                "count": "1",
                "localizedTextIdFull": "205",
                "worldLocationsIdIndicator00": "8867",
                "worldLocationsIdIndicator01": "0",
                "worldLocationsIdIndicator02": "0",
                "worldLocationsIdIndicator03": "0",
                "maxTimeAllowedMS": "0",
                "localizedTextIdShort": "0",
                "targetGroupIdRewardPane": "0",
                "questDirectionId": "0",
            },
            {
                "ID": "4889",
                "type": "12",
                "flags": "40",
                "data": "13150",
                "count": "1",
                "localizedTextIdFull": "205",
                "worldLocationsIdIndicator00": "8868",
                "worldLocationsIdIndicator01": "0",
                "worldLocationsIdIndicator02": "0",
                "worldLocationsIdIndicator03": "0",
                "maxTimeAllowedMS": "0",
                "localizedTextIdShort": "0",
                "targetGroupIdRewardPane": "0",
                "questDirectionId": "0",
            },
            {
                "ID": "4890",
                "type": "12",
                "flags": "40",
                "data": "13151",
                "count": "1",
                "localizedTextIdFull": "205",
                "worldLocationsIdIndicator00": "8869",
                "worldLocationsIdIndicator01": "0",
                "worldLocationsIdIndicator02": "0",
                "worldLocationsIdIndicator03": "0",
                "maxTimeAllowedMS": "0",
                "localizedTextIdShort": "0",
                "targetGroupIdRewardPane": "0",
                "questDirectionId": "0",
            },
            {
                "ID": "13391",
                "type": "5",
                "flags": "8",
                "data": "0",
                "count": "1",
                "localizedTextIdFull": "0",
                "worldLocationsIdIndicator00": "0",
                "worldLocationsIdIndicator01": "0",
                "worldLocationsIdIndicator02": "0",
                "worldLocationsIdIndicator03": "0",
                "maxTimeAllowedMS": "0",
                "localizedTextIdShort": "0",
                "targetGroupIdRewardPane": "0",
                "questDirectionId": "0",
            },
            {
                "ID": "4467",
                "type": "12",
                "flags": "16",
                "data": "11070",
                "count": "3",
                "localizedTextIdFull": "206",
                "worldLocationsIdIndicator00": "12155",
                "worldLocationsIdIndicator01": "12156",
                "worldLocationsIdIndicator02": "12157",
                "worldLocationsIdIndicator03": "12158",
                "maxTimeAllowedMS": "0",
                "localizedTextIdShort": "0",
                "targetGroupIdRewardPane": "0",
                "questDirectionId": "0",
            },
            {
                "ID": "4564",
                "type": "8",
                "flags": "4",
                "data": "7288",
                "count": "8",
                "localizedTextIdFull": "206",
                "worldLocationsIdIndicator00": "12155",
                "worldLocationsIdIndicator01": "12156",
                "worldLocationsIdIndicator02": "12157",
                "worldLocationsIdIndicator03": "12158",
                "maxTimeAllowedMS": "0",
                "localizedTextIdShort": "0",
                "targetGroupIdRewardPane": "0",
                "questDirectionId": "0",
            },
            {
                "ID": "4470",
                "type": "12",
                "flags": "16",
                "data": "11070",
                "count": "3",
                "localizedTextIdFull": "206",
                "worldLocationsIdIndicator00": "12155",
                "worldLocationsIdIndicator01": "12156",
                "worldLocationsIdIndicator02": "12157",
                "worldLocationsIdIndicator03": "12158",
                "maxTimeAllowedMS": "0",
                "localizedTextIdShort": "0",
                "targetGroupIdRewardPane": "0",
                "questDirectionId": "0",
            },
            {
                "ID": "4565",
                "type": "8",
                "flags": "4",
                "data": "1463",
                "count": "8",
                "localizedTextIdFull": "206",
                "worldLocationsIdIndicator00": "12155",
                "worldLocationsIdIndicator01": "12156",
                "worldLocationsIdIndicator02": "12157",
                "worldLocationsIdIndicator03": "12158",
                "maxTimeAllowedMS": "0",
                "localizedTextIdShort": "0",
                "targetGroupIdRewardPane": "0",
                "questDirectionId": "0",
            },
            {
                "ID": "4987",
                "type": "5",
                "flags": "0",
                "data": "0",
                "count": "1",
                "localizedTextIdFull": "207",
                "worldLocationsIdIndicator00": "7807",
                "worldLocationsIdIndicator01": "0",
                "worldLocationsIdIndicator02": "0",
                "worldLocationsIdIndicator03": "0",
                "maxTimeAllowedMS": "0",
                "localizedTextIdShort": "0",
                "targetGroupIdRewardPane": "0",
                "questDirectionId": "0",
            },
            {
                "ID": "4485",
                "type": "32",
                "flags": "7",
                "data": "206",
                "count": "5",
                "localizedTextIdFull": "208",
                "worldLocationsIdIndicator00": "7807",
                "worldLocationsIdIndicator01": "0",
                "worldLocationsIdIndicator02": "0",
                "worldLocationsIdIndicator03": "0",
                "maxTimeAllowedMS": "0",
                "localizedTextIdShort": "0",
                "targetGroupIdRewardPane": "4985",
                "questDirectionId": "0",
            },
            {
                "ID": "4918",
                "type": "8",
                "flags": "4",
                "data": "1177",
                "count": "8",
                "localizedTextIdFull": "212",
                "worldLocationsIdIndicator00": "9181",
                "worldLocationsIdIndicator01": "9182",
                "worldLocationsIdIndicator02": "9962",
                "worldLocationsIdIndicator03": "0",
                "maxTimeAllowedMS": "0",
                "localizedTextIdShort": "0",
                "targetGroupIdRewardPane": "0",
                "questDirectionId": "0",
            },
            {
                "ID": "4813",
                "type": "32",
                "flags": "7",
                "data": "363",
                "count": "6",
                "localizedTextIdFull": "213",
                "worldLocationsIdIndicator00": "9181",
                "worldLocationsIdIndicator01": "9182",
                "worldLocationsIdIndicator02": "9962",
                "worldLocationsIdIndicator03": "0",
                "maxTimeAllowedMS": "0",
                "localizedTextIdShort": "0",
                "targetGroupIdRewardPane": "4372",
                "questDirectionId": "0",
            },
            {
                "ID": "4744",
                "type": "3",
                "flags": "0",
                "data": "12484",
                "count": "1",
                "localizedTextIdFull": "209",
                "worldLocationsIdIndicator00": "8857",
                "worldLocationsIdIndicator01": "0",
                "worldLocationsIdIndicator02": "0",
                "worldLocationsIdIndicator03": "0",
                "maxTimeAllowedMS": "0",
                "localizedTextIdShort": "0",
                "targetGroupIdRewardPane": "0",
                "questDirectionId": "0",
            },
            {
                "ID": "4745",
                "type": "5",
                "flags": "7",
                "data": "0",
                "count": "5",
                "localizedTextIdFull": "210",
                "worldLocationsIdIndicator00": "8858",
                "worldLocationsIdIndicator01": "0",
                "worldLocationsIdIndicator02": "0",
                "worldLocationsIdIndicator03": "0",
                "maxTimeAllowedMS": "0",
                "localizedTextIdShort": "0",
                "targetGroupIdRewardPane": "7261",
                "questDirectionId": "254",
            },
            {
                "ID": "4791",
                "type": "8",
                "flags": "5",
                "data": "7293",
                "count": "5",
                "localizedTextIdFull": "211",
                "worldLocationsIdIndicator00": "8858",
                "worldLocationsIdIndicator01": "8859",
                "worldLocationsIdIndicator02": "0",
                "worldLocationsIdIndicator03": "0",
                "maxTimeAllowedMS": "0",
                "localizedTextIdShort": "0",
                "targetGroupIdRewardPane": "0",
                "questDirectionId": "254",
            },
        ]
        quest_output = [
            {"quest_id": 10, "bucket": "generic_table_driven", "completion_status": "not_retail_complete"},
            {"quest_id": 3673, "bucket": "curated_full", "completion_status": "not_retail_complete"},
            {"quest_id": 4696, "bucket": "curated_partial_stub_script", "completion_status": "not_retail_complete"},
            {"quest_id": 4694, "bucket": "generic_with_script", "completion_status": "not_retail_complete"},
            {"quest_id": 3479, "bucket": "curated_full", "completion_status": "not_retail_complete"},
            {"quest_id": 3480, "bucket": "curated_full", "completion_status": "not_retail_complete"},
            {"quest_id": 3486, "bucket": "curated_full", "completion_status": "not_retail_complete"},
            {"quest_id": 3741, "bucket": "generic_with_script", "completion_status": "not_retail_complete"},
            {"quest_id": 3797, "bucket": "generic_with_script", "completion_status": "not_retail_complete"},
            {"quest_id": 3668, "bucket": "generic_with_script", "completion_status": "not_retail_complete"},
        ]

        rows = audit.build_quest_objective_rows(strings, quest_rows, objective_rows, quest_output)

        self.assertEqual(23, len(rows))
        generic_row = next(row for row in rows if row["objective_id"] == 300)
        self.assertEqual("mapped_generic_handler", generic_row["support_status"])
        self.assertEqual("mapped_objective_handler_pending_world_trigger_smoke", generic_row["evidence_status"])
        self.assertEqual("Full objective", generic_row["localized_text_full"])
        self.assertEqual("900", generic_row["world_location_indicator_ids"])

        q3673_checklist = next(row for row in rows if row["objective_id"] == 4748)
        self.assertEqual("mapped_generic_handler", q3673_checklist["support_status"])
        self.assertEqual(
            "runtime_q3673_signal_flare_target_group_checklist_credit_tested_pending_client_smoke",
            q3673_checklist["evidence_status"],
        )
        self.assertEqual(1106, q3673_checklist["objective_data"])
        self.assertIn("TargetGroup.tbl.sql", q3673_checklist["evidence_sources"])
        self.assertIn("NorthernWildsMapScript.cs", q3673_checklist["evidence_sources"])
        self.assertIn("InteractionObjectiveUpdaterTests.cs", q3673_checklist["evidence_sources"])
        self.assertIn("checklist indexes 0, 1, and 2", q3673_checklist["blocker_summary"])

        q3673_signal_flare = next(row for row in rows if row["objective_id"] == 4889)
        self.assertEqual("mapped_generic_handler", q3673_signal_flare["support_status"])
        self.assertEqual(
            "runtime_q3673_signal_flare_csi_credit_tested_pending_client_smoke",
            q3673_signal_flare["evidence_status"],
        )
        self.assertEqual(13150, q3673_signal_flare["objective_data"])
        self.assertIn("Creature2.tbl.sql", q3673_signal_flare["evidence_sources"])
        self.assertIn("NorthernWildsBranchInteractionTests.cs", q3673_signal_flare["evidence_sources"])
        self.assertIn("SucceedCSI data 13150", q3673_signal_flare["blocker_summary"])

        q3673_hidden_activate = next(row for row in rows if row["objective_id"] == 13391)
        self.assertEqual("mapped_generic_handler", q3673_hidden_activate["support_status"])
        self.assertEqual(
            "runtime_q3673_hidden_completion_objective_duplicate_guard_script_tested_pending_client_smoke",
            q3673_hidden_activate["evidence_status"],
        )
        self.assertEqual(0, q3673_hidden_activate["objective_data"])
        self.assertIn("QuestObjective.cs", q3673_hidden_activate["evidence_sources"])
        self.assertIn("Q3673ContactWithThayd.cs", q3673_hidden_activate["evidence_sources"])
        self.assertIn("NorthernWildsQuestChainTests.cs", q3673_hidden_activate["evidence_sources"])
        self.assertIn("hidden required", q3673_hidden_activate["blocker_summary"])
        self.assertIn("objective 4748 is complete", q3673_hidden_activate["blocker_summary"])

        q3479_survivor = next(row for row in rows if row["quest_id"] == 3479 and row["objective_id"] == 4467)
        self.assertEqual("mapped_generic_handler", q3479_survivor["support_status"])
        self.assertEqual(
            "runtime_q3479_trapped_survivor_csi_spawn_and_credit_tested_external_video_progress_observed_pending_local_client_smoke",
            q3479_survivor["evidence_status"],
        )
        self.assertEqual(11070, q3479_survivor["objective_data"])
        self.assertIn("VIDEO_EVIDENCE_MAP.md", q3479_survivor["evidence_sources"])
        self.assertIn("NorthernWildsMapScript.cs", q3479_survivor["evidence_sources"])
        self.assertIn("InteractionObjectiveUpdaterTests.cs", q3479_survivor["evidence_sources"])
        self.assertIn("16 unique-name-mapped Trapped Survivor 11070 placements", q3479_survivor["blocker_summary"])
        self.assertIn("33% Rescue Trapped Survivors", q3479_survivor["blocker_summary"])
        self.assertIn("source_coordinate_ids 3406, 3407, 3408, 15032", q3479_survivor["blocker_summary"])
        self.assertIn("shared yeti kill objective has separate runtime spawn/credit coverage", q3479_survivor["blocker_summary"])

        q3479_yeti = next(row for row in rows if row["quest_id"] == 3479 and row["objective_id"] == 4564)
        self.assertEqual("mapped_generic_handler", q3479_yeti["support_status"])
        self.assertEqual(
            "runtime_q3479_yeti_target_group_spawn_and_kill_credit_tested_external_video_combat_progress_observed_pending_local_client_smoke",
            q3479_yeti["evidence_status"],
        )
        self.assertEqual(7288, q3479_yeti["objective_data"])
        self.assertIn("VIDEO_EVIDENCE_MAP.md", q3479_yeti["evidence_sources"])
        self.assertIn("TargetGroup.tbl.sql", q3479_yeti["evidence_sources"])
        self.assertIn("NorthernWildsMapScript.cs", q3479_yeti["evidence_sources"])
        self.assertIn("QuestTests.cs", q3479_yeti["evidence_sources"])
        self.assertIn("TargetGroup 7288", q3479_yeti["blocker_summary"])
        self.assertIn("Yeti Snowstalker combat", q3479_yeti["blocker_summary"])
        self.assertIn("source_coordinate_ids 7915699", q3479_yeti["blocker_summary"])
        self.assertIn("live/client combat kill smoke", q3479_yeti["blocker_summary"])

        q3480_survivor = next(row for row in rows if row["quest_id"] == 3480 and row["objective_id"] == 4470)
        self.assertEqual(
            "runtime_q3480_trapped_survivor_csi_spawn_and_credit_tested_external_video_progress_observed_pending_local_client_smoke",
            q3480_survivor["evidence_status"],
        )
        self.assertIn("VIDEO_EVIDENCE_MAP.md", q3480_survivor["evidence_sources"])
        self.assertIn("Q3480 tracker progress for Rescue Trapped Survivors", q3480_survivor["blocker_summary"])
        self.assertIn("shared yeti kill objective has separate runtime spawn/credit coverage", q3480_survivor["blocker_summary"])

        q3480_yeti = next(row for row in rows if row["quest_id"] == 3480 and row["objective_id"] == 4565)
        self.assertEqual(
            "runtime_q3480_yeti_target_group_spawn_and_kill_credit_tested_external_video_progress_observed_pending_local_client_smoke",
            q3480_yeti["evidence_status"],
        )
        self.assertEqual(1463, q3480_yeti["objective_data"])
        self.assertIn("VIDEO_EVIDENCE_MAP.md", q3480_yeti["evidence_sources"])
        self.assertIn("TargetGroup.tbl.sql", q3480_yeti["evidence_sources"])
        self.assertIn("NorthernWildsMapScript.cs", q3480_yeti["evidence_sources"])
        self.assertIn("QuestTests.cs", q3480_yeti["evidence_sources"])
        self.assertIn("TargetGroup 1463", q3480_yeti["blocker_summary"])
        self.assertIn("64% Kill yeti progress", q3480_yeti["blocker_summary"])
        self.assertIn("source_coordinate_ids 7915699", q3480_yeti["blocker_summary"])
        self.assertIn("live/client combat kill smoke", q3480_yeti["blocker_summary"])

        q3486_arrival = next(row for row in rows if row["quest_id"] == 3486 and row["objective_id"] == 4987)
        self.assertEqual("mapped_generic_handler", q3486_arrival["support_status"])
        self.assertEqual(
            "runtime_q3486_arrival_objective_zone_story_credit_tested_pending_client_smoke",
            q3486_arrival["evidence_status"],
        )
        self.assertEqual(0, q3486_arrival["objective_data"])
        self.assertEqual("7807", q3486_arrival["world_location_indicator_ids"])
        self.assertIn("story panel 1575", q3486_arrival["blocker_summary"])
        self.assertIn("already-in-zone accept path", q3486_arrival["blocker_summary"])

        q3486_loftite = next(row for row in rows if row["quest_id"] == 3486 and row["objective_id"] == 4485)
        self.assertEqual("mapped_generic_handler", q3486_loftite["support_status"])
        self.assertEqual(
            "runtime_q3486_loftite_virtual_collect_crystal_guardian_and_frostbite_credit_tested_pending_spawn_client_smoke",
            q3486_loftite["evidence_status"],
        )
        self.assertEqual(206, q3486_loftite["objective_data"])
        self.assertEqual(5, q3486_loftite["objective_count"])
        self.assertEqual(4985, q3486_loftite["target_group_id_reward_pane"])
        self.assertIn("Crystal Guardian kills", q3486_loftite["blocker_summary"])
        self.assertIn("Frostbite 11924 kill dispatch", q3486_loftite["blocker_summary"])

        q3741_crates = next(row for row in rows if row["quest_id"] == 3741 and row["objective_id"] == 4813)
        self.assertEqual("mapped_generic_handler", q3741_crates["support_status"])
        self.assertEqual(
            "runtime_q3741_supply_crate_spawn_and_virtual_collect_credit_tested_pending_client_smoke",
            q3741_crates["evidence_status"],
        )
        self.assertEqual(363, q3741_crates["objective_data"])
        self.assertEqual(6, q3741_crates["objective_count"])
        self.assertEqual("9181;9182;9962", q3741_crates["world_location_indicator_ids"])
        self.assertEqual(4372, q3741_crates["target_group_id_reward_pane"])
        self.assertIn("Q3741ScatteredSupplies.cs", q3741_crates["evidence_sources"])
        self.assertIn("26 current reviewed Exile Supply Crate 12919 placements", q3741_crates["blocker_summary"])
        self.assertIn("source_coordinate_ids 1218, 1219, 1220", q3741_crates["blocker_summary"])
        self.assertIn("115540", q3741_crates["blocker_summary"])
        self.assertIn("2634068", q3741_crates["blocker_summary"])
        self.assertIn("Creature2 11066 carries Q3741", q3741_crates["blocker_summary"])

        q3797_kill_targets = next(row for row in rows if row["quest_id"] == 3797 and row["objective_id"] == 4918)
        self.assertEqual("mapped_generic_handler", q3797_kill_targets["support_status"])
        self.assertEqual(
            "runtime_q3797_rootbrute_yeti_nested_kill_target_group_spawn_tested_pending_client_smoke",
            q3797_kill_targets["evidence_status"],
        )
        self.assertEqual(1177, q3797_kill_targets["objective_data"])
        self.assertEqual(8, q3797_kill_targets["objective_count"])
        self.assertEqual("9181;9182;9962", q3797_kill_targets["world_location_indicator_ids"])
        self.assertIn("TargetGroup 1177", q3797_kill_targets["blocker_summary"])
        self.assertIn("creature-id group 6406", q3797_kill_targets["blocker_summary"])
        self.assertIn("type-9 creature-list branches", q3797_kill_targets["blocker_summary"])
        self.assertIn("QuestTests.cs", q3797_kill_targets["evidence_sources"])
        self.assertIn("AssetManagerTargetGroupTests.cs", q3797_kill_targets["evidence_sources"])

        q3668_lusk = next(row for row in rows if row["quest_id"] == 3668 and row["objective_id"] == 4744)
        self.assertEqual("mapped_generic_handler", q3668_lusk["support_status"])
        self.assertEqual(
            "runtime_q3668_lusk_talk_objective_spawn_and_generic_talk_credit_tested_pending_client_smoke",
            q3668_lusk["evidence_status"],
        )
        self.assertEqual(12484, q3668_lusk["objective_data"])
        self.assertIn("Scientist Lusk", q3668_lusk["blocker_summary"])
        self.assertIn("source_coordinate_id 4272", q3668_lusk["blocker_summary"])
        self.assertIn("InteractionObjectiveUpdaterTests.cs", q3668_lusk["evidence_sources"])

        q3668_survivors = next(row for row in rows if row["quest_id"] == 3668 and row["objective_id"] == 4745)
        self.assertEqual("mapped_generic_handler", q3668_survivors["support_status"])
        self.assertEqual(
            "runtime_q3668_imprisoned_survivor_activate_target_group_spawn_tested_pending_client_smoke",
            q3668_survivors["evidence_status"],
        )
        self.assertEqual(5, q3668_survivors["objective_count"])
        self.assertEqual(7261, q3668_survivors["target_group_id_reward_pane"])
        self.assertIn("TargetGroup 7261", q3668_survivors["blocker_summary"])
        self.assertIn("source_coordinate_ids 2622", q3668_survivors["blocker_summary"])

        q3668_skeech = next(row for row in rows if row["quest_id"] == 3668 and row["objective_id"] == 4791)
        self.assertEqual("mapped_generic_handler", q3668_skeech["support_status"])
        self.assertEqual(
            "runtime_q3668_skeech_nested_kill_target_group_spawn_tested_pending_client_smoke",
            q3668_skeech["evidence_status"],
        )
        self.assertEqual(7293, q3668_skeech["objective_data"])
        self.assertIn("TargetGroup 7293", q3668_skeech["blocker_summary"])
        self.assertIn("14054 and 11913", q3668_skeech["blocker_summary"])

        reward_pane_row = next(row for row in rows if row["objective_id"] == 301)
        self.assertEqual("mapped_generic_handler", reward_pane_row["support_status"])
        self.assertEqual(
            "mapped_activate_entity_reward_pane_target_group_with_test_pending_spawn_credit_smoke",
            reward_pane_row["evidence_status"],
        )
        self.assertIn("AssetManagerTargetGroupTests.cs", reward_pane_row["evidence_sources"])
        self.assertIn("QuestObjectiveTests.cs", reward_pane_row["evidence_sources"])
        self.assertIn("member spawn availability", reward_pane_row["blocker_summary"])

        q4696_reward_pane_row = next(row for row in rows if row["objective_id"] == 6508)
        self.assertEqual("mapped_generic_handler", q4696_reward_pane_row["support_status"])
        self.assertEqual(
            "runtime_q4696_retreat_enemy_kill_to_activate_target_group_credit_tested_pending_client_smoke",
            q4696_reward_pane_row["evidence_status"],
        )
        self.assertIn("Q4696LeavingTheTempleOfOsiric.cs", q4696_reward_pane_row["evidence_sources"])
        self.assertIn("GalerasQuestScriptTests.cs", q4696_reward_pane_row["evidence_sources"])
        self.assertIn("QuestTests.cs", q4696_reward_pane_row["evidence_sources"])
        self.assertIn("Quest.cs", q4696_reward_pane_row["evidence_sources"])
        self.assertIn("TargetGroup 4323", q4696_reward_pane_row["blocker_summary"])
        self.assertIn("credits QuestObjectiveType.ActivateEntity", q4696_reward_pane_row["blocker_summary"])
        self.assertIn("live/client combat objective smoke", q4696_reward_pane_row["blocker_summary"])

        q4696_rally_row = next(row for row in rows if row["objective_id"] == 6457)
        self.assertEqual("mapped_generic_handler", q4696_rally_row["support_status"])
        self.assertEqual(
            "runtime_q4696_rally_enter_area_trigger_spawn_and_credit_tested_pending_client_smoke",
            q4696_rally_row["evidence_status"],
        )
        self.assertEqual(1931, q4696_rally_row["objective_data"])
        self.assertIn("GalerasMapScript.cs", q4696_rally_row["evidence_sources"])
        self.assertIn("GalerasMapScriptTests.cs", q4696_rally_row["evidence_sources"])
        self.assertIn("VolumeGridTriggerEntityTests.cs", q4696_rally_row["evidence_sources"])
        self.assertIn("WorldLocation2 12649", q4696_rally_row["blocker_summary"])
        self.assertIn("retreat/rally script-flow", q4696_rally_row["blocker_summary"])

        q4694_trooper_vog_row = next(row for row in rows if row["objective_id"] == 16115)
        self.assertEqual("mapped_generic_handler", q4694_trooper_vog_row["support_status"])
        self.assertEqual(
            "runtime_q4694_trooper_vog_activate_entity_credit_tested_pending_dialog_client_smoke",
            q4694_trooper_vog_row["evidence_status"],
        )
        self.assertEqual(50904, q4694_trooper_vog_row["objective_data"])
        self.assertIn("ClientActivateUnitHandlerTests.cs", q4694_trooper_vog_row["evidence_sources"])
        self.assertIn("Spell4Effects.tbl.sql", q4694_trooper_vog_row["evidence_sources"])
        self.assertIn("Spell4Effects Activate row", q4694_trooper_vog_row["blocker_summary"])
        self.assertIn("client-visible Vog dialog", q4694_trooper_vog_row["blocker_summary"])

        q4694_holdout_row = next(row for row in rows if row["objective_id"] == 16116)
        self.assertEqual("mapped_generic_handler", q4694_holdout_row["support_status"])
        self.assertEqual(
            "runtime_q4694_holdout_complete_event_kill_credit_and_spawn_tested_pending_client_smoke",
            q4694_holdout_row["evidence_status"],
        )
        self.assertEqual(111, q4694_holdout_row["objective_data"])
        self.assertIn("Q4694HoldTheLine.cs", q4694_holdout_row["evidence_sources"])
        self.assertIn("GalerasQuestScriptTests.cs", q4694_holdout_row["evidence_sources"])
        self.assertIn("runtime_world_seed.sql", q4694_holdout_row["evidence_sources"])
        self.assertIn("Creature2 23953", q4694_holdout_row["blocker_summary"])
        self.assertIn("CompleteEvent data 111", q4694_holdout_row["blocker_summary"])
        self.assertIn("93 Creature2 23953 rows", q4694_holdout_row["blocker_summary"])
        self.assertIn("23 rows near Trooper Vog", q4694_holdout_row["blocker_summary"])
        self.assertIn("Trooper Vog dialog", q4694_holdout_row["blocker_summary"])

        enter_area_row = next(row for row in rows if row["objective_id"] == 302)
        self.assertEqual("mapped_generic_handler", enter_area_row["support_status"])
        self.assertEqual(
            "mapped_enter_area_world_location_trigger_with_test_pending_trigger_placement_smoke",
            enter_area_row["evidence_status"],
        )
        self.assertIn("VolumeGridTriggerEntityTests.cs", enter_area_row["evidence_sources"])
        self.assertIn("actual trigger/entity placement", enter_area_row["blocker_summary"])

    def test_quest_reward_rows_include_rotation_and_pushed_dependencies(self) -> None:
        strings = {100: "Quest Title", 203: "Indigenous Intelligence", 204: "Contact with Thayd", 205: "Fiery Distraction", 206: "Setting Up Camp", 212: "Scattered Supplies", 213: "More Important Than Revenge", 214: "By Leaps and Bounds", 215: "From the Wreckage", 216: "Reporting for Duty"}
        quest_rows = [
            {
                "ID": "10",
                "localizedTextIdTitle": "100",
                "pushed_itemId0": "111",
                "pushed_itemId01": "0",
                "pushed_itemId02": "0",
                "pushed_itemId03": "0",
                "pushed_itemId04": "0",
                "pushed_itemId05": "0",
                "pushed_itemCount0": "2",
                "pushed_itemCount01": "0",
                "pushed_itemCount02": "0",
                "pushed_itemCount03": "0",
                "pushed_itemCount04": "0",
                "pushed_itemCount05": "0",
                "virtualItemIdPushed00": "222",
                "virtualItemIdPushed01": "0",
                "virtualItemIdPushed02": "0",
                "virtualItemIdPushed03": "0",
                "virtualItemPushedCount00": "1",
                "virtualItemPushedCount01": "0",
                "virtualItemPushedCount02": "0",
                "virtualItemPushedCount03": "0",
                "virtualItemPushedObjectiveFlagsEnum00": "4",
                "virtualItemPushedObjectiveFlagsEnum01": "0",
                "virtualItemPushedObjectiveFlagsEnum02": "0",
                "virtualItemPushedObjectiveFlagsEnum03": "0",
                "reward_xpOverride": "99",
                "reward_cashOverride": "0",
                "faction2IdRewardReputation00": "0",
                "faction2IdRewardReputation01": "0",
                "rewardReputationOverride00": "0",
                "rewardReputationOverride01": "0",
            }
        ]
        q3671_quest = {key: "0" for key in quest_rows[0]}
        q3671_quest["ID"] = "3671"
        q3671_quest["localizedTextIdTitle"] = "206"
        quest_rows.append(q3671_quest)
        q3479_quest = {key: "0" for key in quest_rows[0]}
        q3479_quest["ID"] = "3479"
        q3479_quest["localizedTextIdTitle"] = "215"
        quest_rows.append(q3479_quest)
        q3480_quest = {key: "0" for key in quest_rows[0]}
        q3480_quest["ID"] = "3480"
        q3480_quest["localizedTextIdTitle"] = "216"
        quest_rows.append(q3480_quest)
        q3668_quest = {key: "0" for key in quest_rows[0]}
        q3668_quest["ID"] = "3668"
        q3668_quest["localizedTextIdTitle"] = "203"
        quest_rows.append(q3668_quest)
        q3673_quest = {key: "0" for key in quest_rows[0]}
        q3673_quest["ID"] = "3673"
        q3673_quest["localizedTextIdTitle"] = "204"
        quest_rows.append(q3673_quest)
        q3777_quest = {key: "0" for key in quest_rows[0]}
        q3777_quest["ID"] = "3777"
        q3777_quest["localizedTextIdTitle"] = "214"
        q3777_quest["faction2IdRewardReputation00"] = "260"
        quest_rows.append(q3777_quest)
        q3741_quest = {key: "0" for key in quest_rows[0]}
        q3741_quest["ID"] = "3741"
        q3741_quest["localizedTextIdTitle"] = "212"
        quest_rows.append(q3741_quest)
        q3886_quest = {key: "0" for key in quest_rows[0]}
        q3886_quest["ID"] = "3886"
        q3886_quest["localizedTextIdTitle"] = "205"
        quest_rows.append(q3886_quest)
        q3963_quest = {key: "0" for key in quest_rows[0]}
        q3963_quest["ID"] = "3963"
        q3963_quest["localizedTextIdTitle"] = "213"
        quest_rows.append(q3963_quest)
        reward_rows = [
            {"ID": "501", "quest2Id": "10", "quest2RewardTypeId": "10", "objectId": "777", "objectAmount": "1", "flags": "0"},
            {"ID": "6662", "quest2Id": "3671", "quest2RewardTypeId": "1", "objectId": "19131", "objectAmount": "1", "flags": "1"},
            {"ID": "2476", "quest2Id": "3479", "quest2RewardTypeId": "1", "objectId": "1375", "objectAmount": "1", "flags": "1"},
            {"ID": "2479", "quest2Id": "3479", "quest2RewardTypeId": "1", "objectId": "13335", "objectAmount": "1", "flags": "1"},
            {"ID": "8569", "quest2Id": "3479", "quest2RewardTypeId": "1", "objectId": "13327", "objectAmount": "1", "flags": "1"},
            {"ID": "1676", "quest2Id": "3480", "quest2RewardTypeId": "1", "objectId": "30459", "objectAmount": "1", "flags": "1"},
            {"ID": "1677", "quest2Id": "3480", "quest2RewardTypeId": "1", "objectId": "30460", "objectAmount": "1", "flags": "1"},
            {"ID": "1678", "quest2Id": "3480", "quest2RewardTypeId": "1", "objectId": "30461", "objectAmount": "1", "flags": "1"},
            {"ID": "2190", "quest2Id": "3668", "quest2RewardTypeId": "1", "objectId": "81334", "objectAmount": "1", "flags": "1"},
            {"ID": "2191", "quest2Id": "3668", "quest2RewardTypeId": "1", "objectId": "81331", "objectAmount": "1", "flags": "1"},
            {"ID": "4768", "quest2Id": "3668", "quest2RewardTypeId": "1", "objectId": "81329", "objectAmount": "1", "flags": "1"},
            {"ID": "2187", "quest2Id": "3673", "quest2RewardTypeId": "1", "objectId": "12654", "objectAmount": "1", "flags": "1"},
            {"ID": "2188", "quest2Id": "3673", "quest2RewardTypeId": "1", "objectId": "12655", "objectAmount": "1", "flags": "1"},
            {"ID": "2189", "quest2Id": "3673", "quest2RewardTypeId": "1", "objectId": "12656", "objectAmount": "1", "flags": "1"},
            {"ID": "2481", "quest2Id": "3673", "quest2RewardTypeId": "1", "objectId": "13328", "objectAmount": "1", "flags": "1"},
            {"ID": "3001", "quest2Id": "3673", "quest2RewardTypeId": "1", "objectId": "14571", "objectAmount": "1", "flags": "1"},
            {"ID": "3846", "quest2Id": "3673", "quest2RewardTypeId": "1", "objectId": "17924", "objectAmount": "1", "flags": "1"},
            {"ID": "2244", "quest2Id": "3777", "quest2RewardTypeId": "1", "objectId": "28048", "objectAmount": "1", "flags": "1"},
            {"ID": "2388", "quest2Id": "3777", "quest2RewardTypeId": "1", "objectId": "28031", "objectAmount": "1", "flags": "1"},
            {"ID": "7191", "quest2Id": "3777", "quest2RewardTypeId": "1", "objectId": "76391", "objectAmount": "1", "flags": "1"},
            {"ID": "7192", "quest2Id": "3777", "quest2RewardTypeId": "1", "objectId": "76392", "objectAmount": "1", "flags": "1"},
            {"ID": "7193", "quest2Id": "3777", "quest2RewardTypeId": "1", "objectId": "76393", "objectAmount": "1", "flags": "1"},
            {"ID": "7284", "quest2Id": "3777", "quest2RewardTypeId": "1", "objectId": "76484", "objectAmount": "1", "flags": "1"},
            {"ID": "8867", "quest2Id": "3777", "quest2RewardTypeId": "1", "objectId": "28015", "objectAmount": "1", "flags": "1"},
            {"ID": "1712", "quest2Id": "3741", "quest2RewardTypeId": "1", "objectId": "81917", "objectAmount": "3", "flags": "0"},
            {"ID": "3476", "quest2Id": "3741", "quest2RewardTypeId": "1", "objectId": "29614", "objectAmount": "1", "flags": "0"},
            {"ID": "2192", "quest2Id": "3886", "quest2RewardTypeId": "1", "objectId": "27868", "objectAmount": "1", "flags": "1"},
            {"ID": "2430", "quest2Id": "3886", "quest2RewardTypeId": "1", "objectId": "13332", "objectAmount": "1", "flags": "1"},
            {"ID": "4767", "quest2Id": "3886", "quest2RewardTypeId": "1", "objectId": "27869", "objectAmount": "1", "flags": "1"},
            {"ID": "3706", "quest2Id": "3963", "quest2RewardTypeId": "1", "objectId": "27872", "objectAmount": "1", "flags": "1"},
            {"ID": "3707", "quest2Id": "3963", "quest2RewardTypeId": "1", "objectId": "17830", "objectAmount": "1", "flags": "1"},
            {"ID": "4771", "quest2Id": "3963", "quest2RewardTypeId": "1", "objectId": "17831", "objectAmount": "1", "flags": "1"},
        ]
        quest_output = [
            {"quest_id": 10, "bucket": "generic_table_driven", "completion_status": "not_retail_complete"},
            {"quest_id": 3671, "bucket": "no_objectives", "completion_status": "not_retail_complete"},
            {"quest_id": 3479, "bucket": "curated_full", "completion_status": "not_retail_complete"},
            {"quest_id": 3480, "bucket": "curated_full", "completion_status": "not_retail_complete"},
            {"quest_id": 3668, "bucket": "curated_full", "completion_status": "not_retail_complete"},
            {"quest_id": 3673, "bucket": "curated_full", "completion_status": "not_retail_complete"},
            {"quest_id": 3777, "bucket": "generic_with_script", "completion_status": "not_retail_complete"},
            {"quest_id": 3741, "bucket": "generic_with_script", "completion_status": "not_retail_complete"},
            {"quest_id": 3886, "bucket": "curated_full", "completion_status": "not_retail_complete"},
            {"quest_id": 3963, "bucket": "curated_full", "completion_status": "not_retail_complete"},
        ]

        rows = audit.build_quest_reward_rows(strings, quest_rows, reward_rows, quest_output)
        reward_support = {
            (row["reward_source"], row["reward_id"]): row["support_status"]
            for row in rows
        }
        source_support = {row["reward_source"]: row["support_status"] for row in rows if row["quest_id"] == 10}

        self.assertEqual("blocked_active_reward_rotation_schedule", reward_support[("Quest2Reward", 501)])
        rotation_reward = next(row for row in rows if row["reward_id"] == 501)
        self.assertIn("RewardRotationEssence.tbl.sql", rotation_reward["evidence_sources"])
        self.assertIn("QuestRewardType.RotationEssence", rotation_reward["blocker_summary"])
        self.assertIn("RewardRotation schedule/content-context", rotation_reward["blocker_summary"])
        self.assertIn("client claim timing", rotation_reward["blocker_summary"])
        self.assertEqual("mapped_client_state_dependency_pending_smoke", source_support["Quest2.pushed_item"])
        self.assertEqual("mapped_client_state_dependency_pending_smoke", source_support["Quest2.virtual_pushed_item"])
        self.assertEqual("mapped_inline_reward_pending_row_smoke", source_support["Quest2.reward_xp_override"])
        q3671_reward = next(row for row in rows if row["quest_id"] == 3671 and row["reward_id"] == 6662)
        self.assertEqual(
            "runtime_q3671_item_reward_completion_grant_tested_pending_client_smoke",
            q3671_reward["evidence_status"],
        )
        self.assertIn("quest_reward_item_map.csv", q3671_reward["evidence_sources"])
        self.assertIn("QuestTests.cs", q3671_reward["evidence_sources"])
        self.assertIn("item 19131 Life and Death", q3671_reward["blocker_summary"])
        self.assertIn("visible Deadeye Brightland 11063", q3671_reward["blocker_summary"])
        for reward_id, item_id in ((2476, 1375), (2479, 13335), (8569, 13327)):
            q3479_reward = next(row for row in rows if row["quest_id"] == 3479 and row["reward_id"] == reward_id)
            self.assertEqual(
                "runtime_q3479_selectable_item_reward_grant_tested_external_video_completion_dialog_observed_pending_local_client_smoke",
                q3479_reward["evidence_status"],
            )
            self.assertIn("VIDEO_EVIDENCE_MAP.md", q3479_reward["evidence_sources"])
            self.assertIn("Quest2Reward.tbl.sql", q3479_reward["evidence_sources"])
            self.assertIn("Item2.tbl.sql", q3479_reward["evidence_sources"])
            self.assertIn("QuestTests.cs", q3479_reward["evidence_sources"])
            self.assertIn(f"Item2 {item_id}", q3479_reward["blocker_summary"])
            self.assertIn("visible Deadeye receiver 11063", q3479_reward["blocker_summary"])
            self.assertIn("achievement checks for quest 3479", q3479_reward["blocker_summary"])
            self.assertIn("reward panel", q3479_reward["blocker_summary"])
            self.assertIn("not selectable-item choice behavior", q3479_reward["blocker_summary"])
            self.assertIn("survivor CSI smoke", q3479_reward["blocker_summary"])
        for reward_id, item_id in ((1676, 30459), (1677, 30460), (1678, 30461)):
            q3480_reward = next(row for row in rows if row["quest_id"] == 3480 and row["reward_id"] == reward_id)
            self.assertEqual(
                "runtime_q3480_selectable_item_reward_grant_tested_external_video_reward_panel_observed_pending_local_client_smoke",
                q3480_reward["evidence_status"],
            )
            self.assertIn("VIDEO_EVIDENCE_MAP.md", q3480_reward["evidence_sources"])
            self.assertIn("Quest2Reward.tbl.sql", q3480_reward["evidence_sources"])
            self.assertIn("Item2.tbl.sql", q3480_reward["evidence_sources"])
            self.assertIn("QuestTests.cs", q3480_reward["evidence_sources"])
            self.assertIn(f"Item2 {item_id}", q3480_reward["blocker_summary"])
            self.assertIn("visible Deadeye receiver 11063", q3480_reward["blocker_summary"])
            self.assertIn("achievement checks for quest 3480", q3480_reward["blocker_summary"])
            self.assertIn("reward panel with Scout's Thermal Gloves", q3480_reward["blocker_summary"])
            self.assertIn("not all selectable item choices or inventory persistence", q3480_reward["blocker_summary"])
            self.assertIn("Durek accept smoke", q3480_reward["blocker_summary"])
        for reward_id, item_id in ((2190, 81334), (2191, 81331), (4768, 81329)):
            q3668_reward = next(row for row in rows if row["quest_id"] == 3668 and row["reward_id"] == reward_id)
            self.assertEqual(
                "runtime_q3668_selectable_item_reward_grant_tested_pending_client_smoke",
                q3668_reward["evidence_status"],
            )
            self.assertIn("Quest2Reward.tbl.sql", q3668_reward["evidence_sources"])
            self.assertIn("Item2.tbl.sql", q3668_reward["evidence_sources"])
            self.assertIn("QuestTests.cs", q3668_reward["evidence_sources"])
            self.assertIn(f"Item2 {item_id}", q3668_reward["blocker_summary"])
            self.assertIn("visible Bartol Sunward receiver 12737", q3668_reward["blocker_summary"])
            self.assertIn("low-15-bit item-id path", q3668_reward["blocker_summary"])
            self.assertIn("achievement checks for quest 3668", q3668_reward["blocker_summary"])
        for reward_id, item_id in ((2187, 12654), (2188, 12655), (2189, 12656), (2481, 13328), (3001, 14571), (3846, 17924)):
            q3673_reward = next(row for row in rows if row["quest_id"] == 3673 and row["reward_id"] == reward_id)
            self.assertEqual(
                "runtime_q3673_selectable_item_reward_grant_tested_pending_client_smoke",
                q3673_reward["evidence_status"],
            )
            self.assertIn("Quest2Reward.tbl.sql", q3673_reward["evidence_sources"])
            self.assertIn("Item2.tbl.sql", q3673_reward["evidence_sources"])
            self.assertIn("QuestTests.cs", q3673_reward["evidence_sources"])
            self.assertIn(f"Item2 {item_id}", q3673_reward["blocker_summary"])
            self.assertIn("visible Deadeye receiver 11063", q3673_reward["blocker_summary"])
            self.assertIn("achievement checks for quest 3673", q3673_reward["blocker_summary"])
        for reward_id, item_id, selection_text in (
            (2244, 28048, "item-id path"),
            (2388, 28031, "item-id path"),
            (7191, 76391, "low-15-bit item-id path"),
            (7192, 76392, "low-15-bit item-id path"),
            (7193, 76393, "low-15-bit item-id path"),
            (7284, 76484, "low-15-bit item-id path"),
            (8867, 28015, "item-id path"),
        ):
            q3777_reward = next(row for row in rows if row["quest_id"] == 3777 and row["reward_id"] == reward_id)
            self.assertEqual(
                "runtime_q3777_selectable_item_reward_grant_tested_pending_client_smoke",
                q3777_reward["evidence_status"],
            )
            self.assertIn("Quest2Reward.tbl.sql", q3777_reward["evidence_sources"])
            self.assertIn("Item2.tbl.sql", q3777_reward["evidence_sources"])
            self.assertIn("QuestTests.cs", q3777_reward["evidence_sources"])
            self.assertIn(f"Item2 {item_id}", q3777_reward["blocker_summary"])
            self.assertIn("visible Denner Hazefall receiver 15759", q3777_reward["blocker_summary"])
            self.assertIn(selection_text, q3777_reward["blocker_summary"])
            self.assertIn("achievement checks for quest 3777", q3777_reward["blocker_summary"])
            self.assertIn("default faction 260 reputation reward amount 188", q3777_reward["blocker_summary"])
        q3777_reputation_reward = next(
            row for row in rows
            if row["quest_id"] == 3777 and row["reward_source"] == "Quest2.reward_reputation_override"
        )
        self.assertEqual(
            "runtime_q3777_default_reputation_reward_grant_tested_pending_client_smoke",
            q3777_reputation_reward["evidence_status"],
        )
        self.assertEqual(260, q3777_reputation_reward["object_id"])
        self.assertEqual(188, q3777_reputation_reward["object_amount"])
        self.assertIn("Quest2Difficulty.tbl.sql", q3777_reputation_reward["evidence_sources"])
        self.assertIn("XpPerLevel.tbl.sql", q3777_reputation_reward["evidence_sources"])
        self.assertIn("faction 260 reputation amount 188", q3777_reputation_reward["blocker_summary"])
        for reward_id, item_id, amount in ((1712, 81917, 3), (3476, 29614, 1)):
            q3741_reward = next(row for row in rows if row["quest_id"] == 3741 and row["reward_id"] == reward_id)
            self.assertEqual(
                "runtime_q3741_fixed_item_rewards_grant_all_tested_pending_client_smoke",
                q3741_reward["evidence_status"],
            )
            self.assertIn("Quest2Reward.tbl.sql", q3741_reward["evidence_sources"])
            self.assertIn("Item2.tbl.sql", q3741_reward["evidence_sources"])
            self.assertIn("QuestTests.cs", q3741_reward["evidence_sources"])
            self.assertIn(f"Item2 {item_id} amount {amount}", q3741_reward["blocker_summary"])
            self.assertIn("visible Land's Reach Commander Durek receiver 11066", q3741_reward["blocker_summary"])
            self.assertIn("grants all fixed reward rows", q3741_reward["blocker_summary"])
            self.assertIn("achievement checks for quest 3741", q3741_reward["blocker_summary"])
        for reward_id, item_id in ((2192, 27868), (2430, 13332), (4767, 27869)):
            q3886_reward = next(row for row in rows if row["quest_id"] == 3886 and row["reward_id"] == reward_id)
            self.assertEqual(
                "runtime_q3886_selectable_item_reward_grant_tested_pending_client_smoke",
                q3886_reward["evidence_status"],
            )
            self.assertIn("Quest2Reward.tbl.sql", q3886_reward["evidence_sources"])
            self.assertIn("Item2.tbl.sql", q3886_reward["evidence_sources"])
            self.assertIn("QuestTests.cs", q3886_reward["evidence_sources"])
            self.assertIn(f"Item2 {item_id}", q3886_reward["blocker_summary"])
            self.assertIn("visible Land's Reach Commander Durek receiver 11066", q3886_reward["blocker_summary"])
            self.assertIn("achievement checks for quest 3886", q3886_reward["blocker_summary"])
        for reward_id, item_id in ((3706, 27872), (3707, 17830), (4771, 17831)):
            q3963_reward = next(row for row in rows if row["quest_id"] == 3963 and row["reward_id"] == reward_id)
            self.assertEqual(
                "runtime_q3963_selectable_item_reward_grant_tested_pending_client_smoke",
                q3963_reward["evidence_status"],
            )
            self.assertIn("Quest2Reward.tbl.sql", q3963_reward["evidence_sources"])
            self.assertIn("Item2.tbl.sql", q3963_reward["evidence_sources"])
            self.assertIn("QuestTests.cs", q3963_reward["evidence_sources"])
            self.assertIn(f"Item2 {item_id}", q3963_reward["blocker_summary"])
            self.assertIn("visible Deadeye receiver 16622", q3963_reward["blocker_summary"])
            self.assertIn("achievement checks for quest 3963", q3963_reward["blocker_summary"])

    def test_quest_reward_evidence_rows_cover_reward_maps_and_noncurrent_rows(self) -> None:
        csv_root = Path(tempfile.mkdtemp(prefix="nf-content-audit-rewards-"))
        try:
            (csv_root / "quest_reward_item_map.csv").write_text(
                "\n".join(
                    [
                        "source_reward_id,jabbithole_quest_id,quest2_id,quest_name,client_quest_name,jabbithole_item_id,item2_id,item_name,game_reward_id,amount,fixed_reward,last_seen_in",
                        "1,1000,10,Quest Title,Quest Title,20,30,Reward Item,40,1,false,7",
                        "5,1000,10,Quest Title,Quest Title,21,31,Wrong Flag Item,41,1,false,7",
                        "5289,227,3479,From the Wreckage,From the Wreckage,1222,13327,Warm Yeti Fur Boots,2477,1,false,7",
                        "5319,234,3486,Empowered Tower,Empowered Tower,3576,1377,Frostbitten Gloves,2147,1,false,7",
                    ]
                )
                + "\n",
                encoding="utf-8",
            )
            (csv_root / "quest_reward_currency_map.csv").write_text(
                "\n".join(
                    [
                        "source_reward_id,jabbithole_quest_id,quest2_id,quest_name,client_quest_name,jabbithole_currency_id,currency_type_id,currency_name,client_currency_name,game_reward_id,amount,fixed_reward,last_seen_in",
                        "2,1000,10,Quest Title,Quest Title,1,1,Cash,,0,500,true,7",
                        "2457,227,3479,From the Wreckage,From the Wreckage,1,1,Cash,,0,83,true,7",
                        "1147,1454,3480,Reporting for Duty,Reporting for Duty,1,1,Cash,,0,83,true,7",
                    ]
                )
                + "\n",
                encoding="utf-8",
            )
            (csv_root / "quest_reward_reputation_map.csv").write_text(
                "\n".join(
                    [
                        "source_reward_id,jabbithole_quest_id,quest2_id,quest_name,client_quest_name,jabbithole_reputation_reward_id,faction2_id,reputation_name,client_faction_name,amount,fixed_reward,last_seen_in",
                        "3,1000,10,Quest Title,Quest Title,4,5,Reputation,Faction,25,true,7",
                    ]
                )
                + "\n",
                encoding="utf-8",
            )
            (csv_root / "quest_reward_tradeskill_map.csv").write_text(
                "\n".join(
                    [
                        "source_reward_id,jabbithole_quest_id,quest2_id,quest_name,client_quest_name,jabbithole_tradeskill_reward_id,tradeskill_id,tradeskill_name,client_tradeskill_name,game_reward_id,amount,fixed_reward,last_seen_in",
                        "7,1000,10,Quest Title,Quest Title,6,7,Tailor,Tailor,8,100,true,7",
                        "4,1001,9999,Old Quest,,6,7,Tailor,,8,100,true,7",
                    ]
                )
                + "\n",
                encoding="utf-8",
            )
            strings = {100: "Quest Title", 215: "From the Wreckage", 216: "Reporting for Duty", 217: "Empowered Tower"}
            quests = [
                {"ID": "10", "localizedTextIdTitle": "100"},
                {"ID": "3479", "localizedTextIdTitle": "215"},
                {"ID": "3480", "localizedTextIdTitle": "216"},
                {"ID": "3486", "localizedTextIdTitle": "217"},
            ]
            quest_output = [
                {"quest_id": 10, "bucket": "generic_table_driven", "completion_status": "not_retail_complete"},
                {"quest_id": 3479, "bucket": "curated_full", "completion_status": "not_retail_complete"},
                {"quest_id": 3480, "bucket": "curated_full", "completion_status": "not_retail_complete"},
                {"quest_id": 3486, "bucket": "curated_full", "completion_status": "not_retail_complete"},
            ]
            reward_rows = [
                {"ID": "40", "quest2Id": "10", "quest2RewardTypeId": "1", "objectId": "30", "objectAmount": "1", "flags": "1"},
                {"ID": "41", "quest2Id": "10", "quest2RewardTypeId": "1", "objectId": "31", "objectAmount": "1", "flags": "0"},
                {"ID": "42", "quest2Id": "10", "quest2RewardTypeId": "1", "objectId": "31", "objectAmount": "1", "flags": "1"},
                {"ID": "8569", "quest2Id": "3479", "quest2RewardTypeId": "1", "objectId": "13327", "objectAmount": "1", "flags": "1"},
                {"ID": "1707", "quest2Id": "3486", "quest2RewardTypeId": "1", "objectId": "1377", "objectAmount": "1", "flags": "1"},
                {"ID": "2147", "quest2Id": "3486", "quest2RewardTypeId": "1", "objectId": "14102", "objectAmount": "1", "flags": "1"},
            ]

            rows = audit.build_quest_reward_evidence_rows(strings, quests, reward_rows, quest_output, csv_root)

            self.assertEqual(10, len(rows))
            statuses = {
                row["reward_kind"]: row["evidence_status"]
                for row in rows
                if row["quest_id"] == 10 and row["reward_kind"] != "item"
            }
            self.assertEqual("datamapping_jabbithole_cash_reward_inline_pending_amount_smoke", statuses["currency"])
            self.assertEqual("datamapping_jabbithole_reputation_reward_matched_pending_amount_smoke", statuses["reputation"])
            self.assertEqual("datamapping_jabbithole_tradeskill_reward_matched_pending_grant_smoke", statuses["tradeskill"])
            q10_item = next(row for row in rows if row["quest_id"] == 10 and row["reward_kind"] == "item" and row["source_reward_id"] == 1)
            self.assertEqual("datamapping_jabbithole_item_reward_matched_pending_choice_smoke", q10_item["evidence_status"])
            self.assertIn("Item2.tbl.sql", q10_item["evidence_sources"])
            self.assertIn("quest_reward_item_map.csv source_reward_id 1", q10_item["blocker_summary"])
            self.assertIn("Quest2 10 item reward", q10_item["blocker_summary"])
            self.assertIn("Jabbithole item 20 (Reward Item)", q10_item["blocker_summary"])
            self.assertIn("Item2 30 (Reward Item)", q10_item["blocker_summary"])
            self.assertIn("Quest2Reward 40", q10_item["blocker_summary"])
            self.assertIn("amount 1", q10_item["blocker_summary"])
            self.assertIn("fixed_reward=false", q10_item["blocker_summary"])
            self.assertIn("runtime grant and persistence", q10_item["blocker_summary"])
            self.assertEqual("40", q10_item["canonical_reward_ids"])
            self.assertEqual("canonical_reward_id_matched", q10_item["canonical_reward_match_status"])
            q10_wrong_flag_item = next(row for row in rows if row["quest_id"] == 10 and row["reward_kind"] == "item" and row["source_reward_id"] == 5)
            self.assertEqual(
                "datamapping_jabbithole_item_reward_game_reward_id_mismatch_pending_review",
                q10_wrong_flag_item["evidence_status"],
            )
            self.assertEqual("42", q10_wrong_flag_item["canonical_reward_ids"])
            self.assertEqual("canonical_reward_id_mismatch", q10_wrong_flag_item["canonical_reward_match_status"])
            self.assertIn("game_reward_id 41", q10_wrong_flag_item["blocker_summary"])
            self.assertIn("Quest2Reward row 42", q10_wrong_flag_item["blocker_summary"])
            self.assertIn("flags 1", q10_wrong_flag_item["blocker_summary"])
            currency = next(row for row in rows if row["quest_id"] == 10 and row["reward_kind"] == "currency")
            self.assertEqual("", currency["game_reward_id"])
            self.assertIn("CurrencyType.tbl.sql", currency["evidence_sources"])
            self.assertIn("quest_reward_currency_map.csv source_reward_id 2", currency["blocker_summary"])
            self.assertIn("Jabbithole currency 1 (Cash)", currency["blocker_summary"])
            self.assertIn("CurrencyType 1", currency["blocker_summary"])
            self.assertIn("amount 500", currency["blocker_summary"])
            self.assertIn("fixed_reward=true", currency["blocker_summary"])
            self.assertIn("inline credits reward", currency["blocker_summary"])
            reputation = next(row for row in rows if row["quest_id"] == 10 and row["reward_kind"] == "reputation")
            self.assertIn("Faction2.tbl.sql", reputation["evidence_sources"])
            self.assertIn("quest_reward_reputation_map.csv source_reward_id 3", reputation["blocker_summary"])
            self.assertIn("Jabbithole reputation 4 (Reputation)", reputation["blocker_summary"])
            self.assertIn("Faction2 5 (Faction)", reputation["blocker_summary"])
            self.assertIn("amount 25", reputation["blocker_summary"])
            current_tradeskill = next(row for row in rows if row["quest_id"] == 10 and row["reward_kind"] == "tradeskill")
            self.assertIn("Tradeskill.tbl.sql", current_tradeskill["evidence_sources"])
            self.assertIn("quest_reward_tradeskill_map.csv source_reward_id 7", current_tradeskill["blocker_summary"])
            self.assertIn("Jabbithole tradeskill 6 (Tailor)", current_tradeskill["blocker_summary"])
            self.assertIn("Tradeskill 7 (Tailor)", current_tradeskill["blocker_summary"])
            self.assertIn("Quest2Reward 8", current_tradeskill["blocker_summary"])
            q3479_currency = next(row for row in rows if row["quest_id"] == 3479 and row["reward_kind"] == "currency")
            self.assertEqual(
                "runtime_q3479_fixed_cash_reward_grant_tested_external_video_reward_panel_observed_pending_local_client_smoke",
                q3479_currency["evidence_status"],
            )
            self.assertEqual(83, q3479_currency["amount"])
            self.assertIn("VIDEO_EVIDENCE_MAP.md", q3479_currency["evidence_sources"])
            self.assertIn("Quest2Difficulty.tbl.sql", q3479_currency["evidence_sources"])
            self.assertIn("GameFormula.tbl.sql", q3479_currency["evidence_sources"])
            self.assertIn("CurrencyType.Credits amount 83", q3479_currency["blocker_summary"])
            self.assertIn("visible Deadeye receiver 11063", q3479_currency["blocker_summary"])
            self.assertIn("reward panel", q3479_currency["blocker_summary"])
            self.assertIn("not local cash persistence/readback", q3479_currency["blocker_summary"])
            q3479_item = next(row for row in rows if row["quest_id"] == 3479 and row["reward_kind"] == "item")
            self.assertEqual(
                "datamapping_jabbithole_item_reward_stale_source_game_reward_id_canonical_tuple_matched_pending_choice_smoke",
                q3479_item["evidence_status"],
            )
            self.assertIn("quest_reward_client_map.csv", q3479_item["evidence_sources"])
            self.assertIn("Quest2Reward.tbl.sql", q3479_item["evidence_sources"])
            self.assertIn("source_reward_id 5289", q3479_item["blocker_summary"])
            self.assertIn("game_reward_id 2477", q3479_item["blocker_summary"])
            self.assertIn("not a current build 16042 Quest2Reward row for Q3479", q3479_item["blocker_summary"])
            self.assertIn("Quest2Reward row 8569", q3479_item["blocker_summary"])
            self.assertIn("Item2 13327", q3479_item["blocker_summary"])
            self.assertEqual("8569", q3479_item["canonical_reward_ids"])
            self.assertEqual("canonical_reward_id_source_stale_tuple_matched", q3479_item["canonical_reward_match_status"])
            q3486_item = next(row for row in rows if row["quest_id"] == 3486 and row["reward_kind"] == "item")
            self.assertEqual(
                "datamapping_jabbithole_item_reward_canonical_collision_reviewed_provenance_only_pending_choice_smoke",
                q3486_item["evidence_status"],
            )
            self.assertEqual("1707", q3486_item["canonical_reward_ids"])
            self.assertEqual("canonical_reward_id_collision_reviewed_provenance_only", q3486_item["canonical_reward_match_status"])
            self.assertIn("Quest2Reward 1707", q3486_item["blocker_summary"])
            self.assertIn("Quest2Reward 2147", q3486_item["blocker_summary"])
            self.assertIn("provenance-only alias evidence", q3486_item["blocker_summary"])
            self.assertNotIn("game_reward_id_mismatch", q3486_item["evidence_status"])
            q3480_currency = next(row for row in rows if row["quest_id"] == 3480 and row["reward_kind"] == "currency")
            self.assertEqual(
                "runtime_q3480_fixed_cash_reward_grant_tested_external_video_reward_panel_observed_pending_local_client_smoke",
                q3480_currency["evidence_status"],
            )
            self.assertEqual(83, q3480_currency["amount"])
            self.assertIn("VIDEO_EVIDENCE_MAP.md", q3480_currency["evidence_sources"])
            self.assertIn("Quest2Difficulty.tbl.sql", q3480_currency["evidence_sources"])
            self.assertIn("GameFormula.tbl.sql", q3480_currency["evidence_sources"])
            self.assertIn("CurrencyType.Credits amount 83", q3480_currency["blocker_summary"])
            self.assertIn("83c reward-panel presentation", q3480_currency["blocker_summary"])
            self.assertIn("Durek accept smoke", q3480_currency["blocker_summary"])
            tradeskill = next(row for row in rows if row["quest_link_status"] == "non_current_client_quest_reference")
            self.assertEqual("non_current_client_quest_reference", tradeskill["quest_link_status"])
            self.assertEqual("datamapping_reward_not_current_client_quest_row_blocked", tradeskill["evidence_status"])
            self.assertIn("Tools/DataMapping/README.md", tradeskill["evidence_sources"])
            self.assertIn("Quest2 9999 is not present", tradeskill["blocker_summary"])
            self.assertTrue(all(row["completion_status"] == "not_retail_complete" for row in rows))
        finally:
            shutil.rmtree(csv_root, ignore_errors=True)

    def test_quest_prerequisite_rows_include_direct_and_prerequisite_table_dependencies(self) -> None:
        strings = {
            100: "Quest Title",
            101: "Previous Quest",
            200: "Failure text",
            201: "Leaving the Temple of Osiric",
            202: "Burn It Down",
            203: "Empowered Tower",
            204: "Securing the Area",
            205: "Missing Prerequisite Quest",
            206: "Dregs and Thieves",
            207: "Ordnance Recovery",
        }
        quest_rows = [
            {
                "ID": "10",
                "localizedTextIdTitle": "100",
                "preq_level": "5",
                "preq_flags": "0",
                "preq_race": "0",
                "preq_class": "0",
                "preq_item": "123",
                "questPlayerFactionEnum": "2",
                "preq_quest0": "9",
                "preq_quest01": "0",
                "preq_quest02": "0",
                "questIdExclusionPreq0": "0",
                "questIdExclusionPreq1": "0",
                "questIdExclusionPreq2": "0",
                "factionIdPreq0": "77",
                "factionLevelPreq0": "3",
                "factionLevelCompPreq0": "0",
                "factionIdPreq01": "0",
                "factionLevelPreq01": "0",
                "factionLevelCompPreq01": "0",
                "factionIdPreq02": "0",
                "factionLevelPreq02": "0",
                "factionLevelCompPreq02": "0",
                "prerequisiteId": "500",
            },
            {
                "ID": "4696",
                "localizedTextIdTitle": "201",
                "preq_level": "16",
                "preq_flags": "0",
                "preq_race": "0",
                "preq_class": "0",
                "preq_item": "0",
                "questPlayerFactionEnum": "0",
                "preq_quest0": "4667",
                "preq_quest01": "0",
                "preq_quest02": "0",
                "questIdExclusionPreq0": "0",
                "questIdExclusionPreq1": "0",
                "questIdExclusionPreq2": "0",
                "factionIdPreq0": "0",
                "factionLevelPreq0": "0",
                "factionLevelCompPreq0": "0",
                "factionIdPreq01": "0",
                "factionLevelPreq01": "0",
                "factionLevelCompPreq01": "0",
                "factionIdPreq02": "0",
                "factionLevelPreq02": "0",
                "factionLevelCompPreq02": "0",
                "prerequisiteId": "0",
            },
            {
                "ID": "4667",
                "localizedTextIdTitle": "202",
                "preq_level": "0",
                "preq_flags": "0",
                "prerequisiteId": "0",
            },
            {
                "ID": "3797",
                "localizedTextIdTitle": "204",
                "preq_level": "0",
                "preq_flags": "0",
                "preq_race": "0",
                "preq_class": "0",
                "preq_item": "0",
                "questPlayerFactionEnum": "0",
                "preq_quest0": "3486",
                "preq_quest01": "0",
                "preq_quest02": "0",
                "questIdExclusionPreq0": "0",
                "questIdExclusionPreq1": "0",
                "questIdExclusionPreq2": "0",
                "factionIdPreq0": "0",
                "factionLevelPreq0": "0",
                "factionLevelCompPreq0": "0",
                "factionIdPreq01": "0",
                "factionLevelPreq01": "0",
                "factionLevelCompPreq01": "0",
                "factionIdPreq02": "0",
                "factionLevelPreq02": "0",
                "factionLevelCompPreq02": "0",
                "prerequisiteId": "0",
            },
            {
                "ID": "3486",
                "localizedTextIdTitle": "203",
                "preq_level": "0",
                "preq_flags": "0",
                "prerequisiteId": "0",
            },
            {
                "ID": "5597",
                "localizedTextIdTitle": "206",
                "preq_level": "1",
                "preq_flags": "0",
                "preq_race": "0",
                "preq_class": "0",
                "preq_item": "0",
                "questPlayerFactionEnum": "1",
                "preq_quest0": "5596",
                "preq_quest01": "0",
                "preq_quest02": "0",
                "questIdExclusionPreq0": "0",
                "questIdExclusionPreq1": "0",
                "questIdExclusionPreq2": "0",
                "factionIdPreq0": "0",
                "factionLevelPreq0": "0",
                "factionLevelCompPreq0": "0",
                "factionIdPreq01": "0",
                "factionLevelPreq01": "0",
                "factionLevelCompPreq01": "0",
                "factionIdPreq02": "0",
                "factionLevelPreq02": "0",
                "factionLevelCompPreq02": "0",
                "prerequisiteId": "0",
            },
            {
                "ID": "5596",
                "localizedTextIdTitle": "207",
                "preq_level": "0",
                "preq_flags": "0",
                "prerequisiteId": "0",
            },
            {
                "ID": "9",
                "localizedTextIdTitle": "101",
                "preq_level": "0",
                "preq_flags": "0",
                "prerequisiteId": "0",
            },
            {
                "ID": "20",
                "localizedTextIdTitle": "205",
                "preq_level": "0",
                "preq_flags": "0",
                "preq_race": "0",
                "preq_class": "0",
                "preq_item": "0",
                "questPlayerFactionEnum": "2",
                "preq_quest0": "0",
                "preq_quest01": "0",
                "preq_quest02": "0",
                "questIdExclusionPreq0": "0",
                "questIdExclusionPreq1": "0",
                "questIdExclusionPreq2": "0",
                "factionIdPreq0": "0",
                "factionLevelPreq0": "0",
                "factionLevelCompPreq0": "0",
                "factionIdPreq01": "0",
                "factionLevelPreq01": "0",
                "factionLevelCompPreq01": "0",
                "factionIdPreq02": "0",
                "factionLevelPreq02": "0",
                "factionLevelCompPreq02": "0",
                "prerequisiteId": "999",
            },
        ]
        prerequisite_rows = [
            {
                "ID": "500",
                "flags": "0",
                "prerequisiteTypeId0": "6",
                "prerequisiteTypeId1": "0",
                "prerequisiteTypeId2": "0",
                "prerequisiteComparisonId0": "1",
                "prerequisiteComparisonId1": "0",
                "prerequisiteComparisonId2": "0",
                "objectId0": "42",
                "objectId1": "0",
                "objectId2": "0",
                "value0": "2",
                "value1": "0",
                "value2": "0",
                "localizedTextIdFailure": "200",
            }
        ]
        quest_output = [
            {"quest_id": 10, "bucket": "generic_table_driven", "completion_status": "not_retail_complete"},
            {"quest_id": 9, "bucket": "generic_table_driven", "completion_status": "not_retail_complete"},
            {"quest_id": 4696, "bucket": "curated_partial_stub_script", "completion_status": "not_retail_complete"},
            {"quest_id": 4667, "bucket": "curated_full", "completion_status": "not_retail_complete"},
            {"quest_id": 3797, "bucket": "generic_with_script", "completion_status": "not_retail_complete"},
            {"quest_id": 3486, "bucket": "curated_full", "completion_status": "not_retail_complete"},
            {"quest_id": 5597, "bucket": "curated_full", "completion_status": "not_retail_complete"},
            {"quest_id": 5596, "bucket": "curated_full", "completion_status": "not_retail_complete"},
            {"quest_id": 20, "bucket": "generic_table_driven", "completion_status": "not_retail_complete"},
        ]

        rows = audit.build_quest_prerequisite_rows(strings, quest_rows, prerequisite_rows, quest_output)
        dependency_types = {row["dependency_type"] for row in rows}

        self.assertIn("level", dependency_types)
        self.assertIn("required_quest", dependency_types)
        self.assertIn("quest_faction", dependency_types)
        self.assertIn("faction_level", dependency_types)
        self.assertIn("prerequisite_slot", dependency_types)
        self.assertIn("Previous Quest", {row["dependency_name"] for row in rows})
        self.assertIn("Failure text", {row["localized_failure_text"] for row in rows})
        q10_level = next(row for row in rows if row["quest_id"] == 10 and row["dependency_type"] == "level")
        self.assertIn("Quest2 10 requires character level 5", q10_level["blocker_summary"])
        q10_item = next(row for row in rows if row["quest_id"] == 10 and row["dependency_type"] == "item")
        self.assertIn("Quest2 10 has item prerequisite objectId 123", q10_item["blocker_summary"])
        q10_required_quest = next(row for row in rows if row["quest_id"] == 10 and row["dependency_type"] == "required_quest")
        self.assertIn("prerequisite quest slot 0 -> Quest2 9 (Previous Quest)", q10_required_quest["blocker_summary"])
        q10_faction_level = next(row for row in rows if row["quest_id"] == 10 and row["dependency_type"] == "faction_level")
        self.assertIn("faction slot 0 objectId 77", q10_faction_level["blocker_summary"])
        self.assertIn("levelPreq=3;levelCompPreq=0", q10_faction_level["blocker_summary"])
        q10_prerequisite = next(row for row in rows if row["quest_id"] == 10 and row["dependency_type"] == "prerequisite_id")
        self.assertIn("Quest2 10 references Prerequisite 500", q10_prerequisite["blocker_summary"])
        self.assertIn("failure text 'Failure text'", q10_prerequisite["blocker_summary"])
        q10_prerequisite_slot = next(row for row in rows if row["quest_id"] == 10 and row["dependency_type"] == "prerequisite_slot")
        self.assertIn("Prerequisite 500 slot 0", q10_prerequisite_slot["blocker_summary"])
        self.assertIn("prerequisiteTypeId 6", q10_prerequisite_slot["blocker_summary"])
        self.assertIn("objectId 42", q10_prerequisite_slot["blocker_summary"])
        self.assertIn("value 2", q10_prerequisite_slot["blocker_summary"])
        self.assertIn("comparison 1", q10_prerequisite_slot["blocker_summary"])
        q4696_level = next(row for row in rows if row["quest_id"] == 4696 and row["dependency_type"] == "level")
        self.assertEqual("q4696_server_accept_path_level_gate_test_pending_client_dialog_smoke", q4696_level["evidence_status"])
        self.assertIn("QuestTests.cs", q4696_level["evidence_sources"])
        q4696_faction = next(row for row in rows if row["quest_id"] == 4696 and row["dependency_type"] == "quest_faction")
        self.assertEqual("Exile", q4696_faction["dependency_name"])
        self.assertEqual("q4696_server_accept_path_exile_faction_gate_test_pending_client_dialog_smoke", q4696_faction["evidence_status"])
        q4696_required_quest = next(row for row in rows if row["quest_id"] == 4696 and row["dependency_type"] == "required_quest")
        self.assertEqual(4667, q4696_required_quest["dependency_id"])
        self.assertEqual("Burn It Down", q4696_required_quest["dependency_name"])
        self.assertEqual("q4696_server_accept_path_q4667_required_quest_test_pending_client_dialog_smoke", q4696_required_quest["evidence_status"])
        q3797_required_quest = next(row for row in rows if row["quest_id"] == 3797 and row["dependency_type"] == "required_quest")
        self.assertEqual(3486, q3797_required_quest["dependency_id"])
        self.assertEqual("Empowered Tower", q3797_required_quest["dependency_name"])
        self.assertEqual("q3797_server_accept_path_q3486_required_quest_test_pending_client_dialog_smoke", q3797_required_quest["evidence_status"])
        self.assertIn("QuestTests.cs", q3797_required_quest["evidence_sources"])
        self.assertIn("Durek availability", q3797_required_quest["blocker_summary"])
        q5597_level = next(row for row in rows if row["quest_id"] == 5597 and row["dependency_type"] == "level")
        self.assertEqual("q5597_server_accept_path_level_gate_test_external_video_accept_observed_pending_local_dialog_smoke", q5597_level["evidence_status"])
        self.assertIn("QuestTests.cs", q5597_level["evidence_sources"])
        self.assertIn("visible Mondo Zax 24187", q5597_level["blocker_summary"])
        self.assertIn("level gate satisfied", q5597_level["blocker_summary"])
        self.assertIn("positive Mondo accept path is externally observed", q5597_level["blocker_summary"])
        self.assertIn("local client smoke remain blocked", q5597_level["blocker_summary"])
        q5597_faction = next(row for row in rows if row["quest_id"] == 5597 and row["dependency_type"] == "quest_faction")
        self.assertEqual("Dominion", q5597_faction["dependency_name"])
        self.assertEqual("q5597_server_accept_path_dominion_faction_gate_test_external_video_accept_observed_pending_local_dialog_smoke", q5597_faction["evidence_status"])
        self.assertIn("Dominion faction", q5597_faction["blocker_summary"])
        self.assertIn("denial text/UI", q5597_faction["blocker_summary"])
        q5597_required_quest = next(row for row in rows if row["quest_id"] == 5597 and row["dependency_type"] == "required_quest")
        self.assertEqual(5596, q5597_required_quest["dependency_id"])
        self.assertEqual("Ordnance Recovery", q5597_required_quest["dependency_name"])
        self.assertEqual("q5597_server_accept_path_q5596_required_quest_test_external_video_accept_observed_pending_local_dialog_smoke", q5597_required_quest["evidence_status"])
        self.assertIn("completed Q5596", q5597_required_quest["blocker_summary"])
        self.assertIn("positive Q5596-to-Q5597 accept path is externally observed", q5597_required_quest["blocker_summary"])
        self.assertIn("local quest-chain presentation", q5597_required_quest["blocker_summary"])
        missing_prerequisite = next(row for row in rows if row["quest_id"] == 20 and row["dependency_type"] == "prerequisite_id")
        self.assertEqual("blocked_missing_client_prerequisite_row_runtime_fail_closed", missing_prerequisite["support_status"])
        self.assertEqual("blocked_missing_client_prerequisite_row_runtime_fail_closed", missing_prerequisite["evidence_status"])
        self.assertIn("PrerequisiteManagerTests.cs", missing_prerequisite["evidence_sources"])
        self.assertIn("Prerequisite 999", missing_prerequisite["blocker_summary"])
        self.assertIn("fails closed", missing_prerequisite["blocker_summary"])
        self.assertTrue(all(row["completion_status"] == "not_retail_complete" for row in rows))

    def test_quest_loot_rows_expand_laughingws_seed_tables(self) -> None:
        sql_root = Path(tempfile.mkdtemp(prefix="nf-content-audit-loot-"))
        try:
            seed_path = sql_root / "quest_loot.sql"
            seed_path.write_text(
                """
INSERT INTO `loot_group` (`id`, `parentId`, `probability`, `minDrop`, `maxDrop`, `conditionType`, `condition`, `comment`) VALUES
  (1201, NULL, 100, 1, 1, 8, 300, CONVERT(0x4c6f6f742067726f7570 USING utf8mb4)),
  (1202, NULL, 100, 1, 1, 8, 999, CONVERT(0x4f727068616e2067726f7570 USING utf8mb4));
INSERT INTO `loot_item` (`id`, `type`, `staticId`, `probability`, `minCount`, `maxCount`, `comment`) VALUES
  (1201, 6, 999, 100, 1, 1, CONVERT(0x4c6f6f74206974656d USING utf8mb4)),
  (1202, 6, 1000, 100, 1, 1, CONVERT(0x4f727068616e206974656d USING utf8mb4));
INSERT INTO `entity_loot` (`id`, `lootGroupId`, `comment`) VALUES
  (5001, 1201, CONVERT(0x456e74697479206c6f6f74 USING utf8mb4)),
  (5002, 1202, CONVERT(0x4f727068616e20656e74697479206c6f6f74 USING utf8mb4));
""".lstrip(),
                encoding="utf-8",
            )
            strings = {100: "Quest Title", 200: "Objective text"}
            quest_rows = [
                {
                    "ID": "10",
                    "localizedTextIdTitle": "100",
                    "objective0": "300",
                    "objective01": "0",
                    "objective02": "0",
                    "objective03": "0",
                    "objective04": "0",
                    "objective05": "0",
                }
            ]
            objective_rows = [
                {
                    "ID": "300",
                    "type": "32",
                    "data": "999",
                    "count": "1",
                    "localizedTextIdFull": "200",
                    "localizedTextIdShort": "0",
                }
            ]
            quest_output = [{"quest_id": 10, "bucket": "generic_table_driven", "completion_status": "not_retail_complete"}]

            rows = audit.build_quest_loot_rows(strings, quest_rows, objective_rows, quest_output, seed_path)

            self.assertEqual({"loot_group", "loot_item", "entity_loot"}, {row["dependency_type"] for row in rows})
            self.assertIn("Loot item", {row["comment"] for row in rows})
            loot_group = next(row for row in rows if row["dependency_type"] == "loot_group" and row["objective_id"] == 300)
            self.assertIn("loot_group 1201", loot_group["blocker_summary"])
            self.assertIn("QuestObjective 300", loot_group["blocker_summary"])
            self.assertIn("conditionType 8 condition 300", loot_group["blocker_summary"])
            self.assertIn("probability 100", loot_group["blocker_summary"])
            self.assertIn("count 1-1", loot_group["blocker_summary"])
            self.assertIn("drop cadence", loot_group["blocker_summary"])
            self.assertEqual(audit.RUNTIME_VIRTUAL_COLLECT_LOOT_GROUP_STATUS, loot_group["evidence_status"])
            self.assertIn("LootInstanceItem.cs", loot_group["evidence_sources"])
            self.assertIn("LootInstanceDeliveryTests.cs", loot_group["evidence_sources"])
            self.assertIn("VirtualCollect QuestObjective 300", loot_group["blocker_summary"])
            self.assertIn("source/drop trigger", loot_group["blocker_summary"])
            loot_item = next(row for row in rows if row["dependency_type"] == "loot_item" and row["objective_id"] == 300)
            self.assertIn("loot_group 1201 item type 6 staticId 999", loot_item["blocker_summary"])
            self.assertIn("Loot item", loot_item["blocker_summary"])
            self.assertIn("drop chance/count behavior", loot_item["blocker_summary"])
            self.assertEqual(audit.RUNTIME_VIRTUAL_COLLECT_LOOT_ITEM_STATUS, loot_item["evidence_status"])
            self.assertIn("QuestObjectiveType.VirtualCollect", loot_item["blocker_summary"])
            self.assertIn("LootInstanceDeliveryTests.cs", loot_item["evidence_sources"])
            entity_loot = next(row for row in rows if row["dependency_type"] == "entity_loot" and row["objective_id"] == 300)
            self.assertIn("entity 5001 -> loot_group 1201", entity_loot["blocker_summary"])
            self.assertIn("conditionType 8 condition 300", entity_loot["blocker_summary"])
            self.assertIn("encounter/objective timing", entity_loot["blocker_summary"])
            self.assertEqual(audit.RUNTIME_VIRTUAL_COLLECT_ENTITY_LOOT_STATUS, entity_loot["evidence_status"])
            self.assertIn("drop generation trigger", entity_loot["blocker_summary"])
            orphaned_rows = [row for row in rows if row["objective_id"] == 999]
            self.assertEqual(
                {
                    audit.REJECTED_ORPHANED_LOOT_GROUP_STATUS,
                    audit.REJECTED_ORPHANED_LOOT_ITEM_STATUS,
                    audit.REJECTED_ORPHANED_ENTITY_LOOT_STATUS,
                },
                {row["evidence_status"] for row in orphaned_rows},
            )
            self.assertTrue(all(row["quest_id"] == 0 for row in orphaned_rows))
            self.assertTrue(all("QuestObjective 999" in row["blocker_summary"] for row in orphaned_rows))
            self.assertTrue(all("rejected for runtime promotion" in row["blocker_summary"] for row in orphaned_rows))
            self.assertTrue(all(row["completion_status"] == "not_retail_complete" for row in rows))
        finally:
            shutil.rmtree(sql_root, ignore_errors=True)

    def test_quest_world_dependency_rows_cover_locations_directions_and_target_groups(self) -> None:
        strings = {100: "Quest Title", 200: "Zone", 201: "Target group", 203: "Calm Before the Storm", 204: "Contact with Thayd", 205: "Spatial Anomaly", 206: "Setting Up Camp", 207: "From the Wreckage", 208: "Reporting for Duty", 209: "Empowered Tower", 210: "Indigenous Intelligence", 211: "Securing the Area", 212: "Scattered Supplies", 213: "More Important Than Revenge", 214: "Crimson Isle", 215: "Dregs and Thieves", 216: "Bloodstone Canyon", 217: "Scarhide Camp"}
        quest = {
            "ID": "10",
            "localizedTextIdTitle": "100",
            "worldZoneId": "20",
            "WorldLocation2IdReceiver": "300",
            "questDirectionIdCompletion": "400",
            "worldLocation2IdAltReceiver00": "301",
            "worldLocation2IdAltReceiver01": "0",
            "worldLocation2IdAltReceiver02": "0",
            "prerequisiteIdAltReceiver00": "700",
            "prerequisiteIdAltReceiver01": "0",
            "prerequisiteIdAltReceiver02": "0",
            "questDirectionIdAltReceiver00": "401",
            "questDirectionIdAltReceiver01": "0",
            "questDirectionIdAltReceiver02": "0",
            "objective0": "500",
            "objective01": "0",
            "objective02": "0",
            "objective03": "0",
            "objective04": "0",
            "objective05": "0",
        }
        q4696 = {
            **quest,
            "ID": "4696",
            "objective0": "6457",
            "objective01": "6508",
            "questDirectionIdCompletion": "0",
            "WorldLocation2IdReceiver": "17590",
            "worldLocation2IdAltReceiver00": "0",
            "questDirectionIdAltReceiver00": "0",
        }
        q4694 = {
            **quest,
            "ID": "4694",
            "objective0": "16115",
            "objective01": "16116",
            "questDirectionIdCompletion": "0",
            "WorldLocation2IdReceiver": "12649",
            "worldLocation2IdAltReceiver00": "0",
            "questDirectionIdAltReceiver00": "0",
        }
        q4526 = {
            **quest,
            "ID": "4526",
            "localizedTextIdTitle": "205",
            "WorldLocation2IdReceiver": "11669",
            "questDirectionIdCompletion": "0",
            "worldLocation2IdAltReceiver00": "0",
            "questDirectionIdAltReceiver00": "0",
            "objective0": "6164",
            "objective01": "6165",
        }
        q3486 = {
            **quest,
            "ID": "3486",
            "localizedTextIdTitle": "209",
            "WorldLocation2IdReceiver": "7778",
            "questDirectionIdCompletion": "250",
            "worldLocation2IdAltReceiver00": "0",
            "questDirectionIdAltReceiver00": "0",
            "objective0": "4987",
            "objective01": "4485",
        }
        q3797 = {
            **quest,
            "ID": "3797",
            "localizedTextIdTitle": "211",
            "WorldLocation2IdReceiver": "7727",
            "questDirectionIdCompletion": "0",
            "worldLocation2IdAltReceiver00": "0",
            "questDirectionIdAltReceiver00": "0",
            "objective0": "4918",
            "objective01": "0",
        }
        q3741 = {
            **quest,
            "ID": "3741",
            "localizedTextIdTitle": "212",
            "WorldLocation2IdReceiver": "7727",
            "questDirectionIdCompletion": "0",
            "worldLocation2IdAltReceiver00": "0",
            "questDirectionIdAltReceiver00": "0",
            "objective0": "4813",
            "objective01": "0",
        }
        q3668 = {
            **quest,
            "ID": "3668",
            "localizedTextIdTitle": "210",
            "WorldLocation2IdReceiver": "9121",
            "questDirectionIdCompletion": "0",
            "worldLocation2IdAltReceiver00": "0",
            "questDirectionIdAltReceiver00": "0",
            "objective0": "4744",
            "objective01": "4745",
            "objective02": "4791",
        }
        q5610 = {
            **quest,
            "ID": "5610",
            "WorldLocation2IdReceiver": "17803",
            "questDirectionIdCompletion": "0",
            "worldLocation2IdAltReceiver00": "0",
            "questDirectionIdAltReceiver00": "0",
            "objective0": "0",
        }
        q5597 = {
            **quest,
            "ID": "5597",
            "localizedTextIdTitle": "215",
            "worldZoneId": "622",
            "WorldLocation2IdReceiver": "17803",
            "questDirectionIdCompletion": "0",
            "worldLocation2IdAltReceiver00": "0",
            "questDirectionIdAltReceiver00": "0",
            "objective0": "8256",
        }
        q3670 = {
            **quest,
            "ID": "3670",
            "localizedTextIdTitle": "203",
            "WorldLocation2IdReceiver": "29526",
            "questDirectionIdCompletion": "0",
            "worldLocation2IdAltReceiver00": "12354",
            "prerequisiteIdAltReceiver00": "29356",
            "questDirectionIdAltReceiver00": "0",
            "objective0": "0",
        }
        q3671 = {
            **quest,
            "ID": "3671",
            "localizedTextIdTitle": "206",
            "WorldLocation2IdReceiver": "9340",
            "questDirectionIdCompletion": "0",
            "worldLocation2IdAltReceiver00": "12354",
            "prerequisiteIdAltReceiver00": "29356",
            "questDirectionIdAltReceiver00": "0",
            "objective0": "0",
        }
        q3673 = {
            **quest,
            "ID": "3673",
            "localizedTextIdTitle": "204",
            "WorldLocation2IdReceiver": "29526",
            "questDirectionIdCompletion": "0",
            "worldLocation2IdAltReceiver00": "12354",
            "prerequisiteIdAltReceiver00": "29356",
            "questDirectionIdAltReceiver00": "0",
            "objective0": "4748",
            "objective01": "4888",
            "objective02": "4889",
            "objective03": "4890",
        }
        q3479 = {
            **quest,
            "ID": "3479",
            "localizedTextIdTitle": "207",
            "WorldLocation2IdReceiver": "7726",
            "questDirectionIdCompletion": "0",
            "worldLocation2IdAltReceiver00": "0",
            "questDirectionIdAltReceiver00": "0",
            "objective0": "4467",
            "objective01": "4564",
        }
        q3480 = {
            **quest,
            "ID": "3480",
            "localizedTextIdTitle": "208",
            "WorldLocation2IdReceiver": "7726",
            "questDirectionIdCompletion": "0",
            "worldLocation2IdAltReceiver00": "0",
            "questDirectionIdAltReceiver00": "0",
            "objective0": "4470",
            "objective01": "4565",
        }
        q3963 = {
            **quest,
            "ID": "3963",
            "localizedTextIdTitle": "213",
            "worldZoneId": "35",
            "WorldLocation2IdReceiver": "12354",
            "questDirectionIdCompletion": "0",
            "worldLocation2IdAltReceiver00": "0",
            "questDirectionIdAltReceiver00": "0",
            "objective0": "0",
        }
        objective = {
            "ID": "500",
            "type": "2",
            "worldLocationsIdIndicator00": "302",
            "worldLocationsIdIndicator01": "0",
            "worldLocationsIdIndicator02": "0",
            "worldLocationsIdIndicator03": "0",
            "targetGroupIdRewardPane": "600",
            "questDirectionId": "402",
        }
        q4696_objective = {
            "ID": "6457",
            "type": "22",
            "worldLocationsIdIndicator00": "12649",
            "worldLocationsIdIndicator01": "0",
            "worldLocationsIdIndicator02": "0",
            "worldLocationsIdIndicator03": "0",
            "targetGroupIdRewardPane": "0",
            "questDirectionId": "0",
        }
        q4696_activate_objective = {
            "ID": "6508",
            "type": "5",
            "worldLocationsIdIndicator00": "17586",
            "worldLocationsIdIndicator01": "17588",
            "worldLocationsIdIndicator02": "17589",
            "worldLocationsIdIndicator03": "0",
            "targetGroupIdRewardPane": "4323",
            "questDirectionId": "0",
        }
        q4694_activate_objective = {
            "ID": "16115",
            "type": "5",
            "worldLocationsIdIndicator00": "12649",
            "worldLocationsIdIndicator01": "0",
            "worldLocationsIdIndicator02": "0",
            "worldLocationsIdIndicator03": "0",
            "targetGroupIdRewardPane": "7407",
            "questDirectionId": "0",
        }
        q4694_event_objective = {
            "ID": "16116",
            "type": "25",
            "worldLocationsIdIndicator00": "0",
            "worldLocationsIdIndicator01": "0",
            "worldLocationsIdIndicator02": "0",
            "worldLocationsIdIndicator03": "0",
            "targetGroupIdRewardPane": "0",
            "questDirectionId": "0",
        }
        q4526_wreckage_objective = {
            "ID": "6164",
            "type": "5",
            "worldLocationsIdIndicator00": "9706",
            "worldLocationsIdIndicator01": "0",
            "worldLocationsIdIndicator02": "0",
            "worldLocationsIdIndicator03": "0",
            "targetGroupIdRewardPane": "0",
            "questDirectionId": "0",
        }
        q4526_anomaly_objective = {
            "ID": "6165",
            "type": "5",
            "worldLocationsIdIndicator00": "40764",
            "worldLocationsIdIndicator01": "0",
            "worldLocationsIdIndicator02": "0",
            "worldLocationsIdIndicator03": "0",
            "targetGroupIdRewardPane": "0",
            "questDirectionId": "0",
        }
        q3486_arrival_objective = {
            "ID": "4987",
            "type": "5",
            "data": "0",
            "count": "1",
            "worldLocationsIdIndicator00": "7807",
            "worldLocationsIdIndicator01": "0",
            "worldLocationsIdIndicator02": "0",
            "worldLocationsIdIndicator03": "0",
            "targetGroupIdRewardPane": "0",
            "questDirectionId": "0",
        }
        q3486_loftite_objective = {
            "ID": "4485",
            "type": "32",
            "data": "206",
            "count": "5",
            "worldLocationsIdIndicator00": "7807",
            "worldLocationsIdIndicator01": "0",
            "worldLocationsIdIndicator02": "0",
            "worldLocationsIdIndicator03": "0",
            "targetGroupIdRewardPane": "4985",
            "questDirectionId": "0",
        }
        q3797_kill_objective = {
            "ID": "4918",
            "type": "8",
            "data": "1177",
            "count": "8",
            "worldLocationsIdIndicator00": "9181",
            "worldLocationsIdIndicator01": "9182",
            "worldLocationsIdIndicator02": "9962",
            "worldLocationsIdIndicator03": "0",
            "targetGroupIdRewardPane": "0",
            "questDirectionId": "0",
        }
        q3741_supply_objective = {
            "ID": "4813",
            "type": "32",
            "data": "363",
            "count": "6",
            "worldLocationsIdIndicator00": "9181",
            "worldLocationsIdIndicator01": "9182",
            "worldLocationsIdIndicator02": "9962",
            "worldLocationsIdIndicator03": "0",
            "targetGroupIdRewardPane": "4372",
            "questDirectionId": "0",
        }
        q5597_chua_explosives_objective = {
            "ID": "8256",
            "type": "32",
            "data": "364",
            "count": "6",
            "worldLocationsIdIndicator00": "17896",
            "worldLocationsIdIndicator01": "17898",
            "worldLocationsIdIndicator02": "17897",
            "worldLocationsIdIndicator03": "17899",
            "targetGroupIdRewardPane": "4373",
            "questDirectionId": "0",
        }
        q3673_checklist_objective = {
            "ID": "4748",
            "type": "14",
            "data": "1106",
            "worldLocationsIdIndicator00": "0",
            "worldLocationsIdIndicator01": "0",
            "worldLocationsIdIndicator02": "0",
            "worldLocationsIdIndicator03": "0",
            "targetGroupIdRewardPane": "0",
            "questDirectionId": "0",
        }
        q3673_flare_one_objective = {
            "ID": "4888",
            "type": "12",
            "worldLocationsIdIndicator00": "8867",
            "worldLocationsIdIndicator01": "0",
            "worldLocationsIdIndicator02": "0",
            "worldLocationsIdIndicator03": "0",
            "targetGroupIdRewardPane": "0",
            "questDirectionId": "0",
        }
        q3673_flare_two_objective = {
            "ID": "4889",
            "type": "12",
            "worldLocationsIdIndicator00": "8868",
            "worldLocationsIdIndicator01": "0",
            "worldLocationsIdIndicator02": "0",
            "worldLocationsIdIndicator03": "0",
            "targetGroupIdRewardPane": "0",
            "questDirectionId": "0",
        }
        q3673_flare_three_objective = {
            "ID": "4890",
            "type": "12",
            "worldLocationsIdIndicator00": "8869",
            "worldLocationsIdIndicator01": "0",
            "worldLocationsIdIndicator02": "0",
            "worldLocationsIdIndicator03": "0",
            "targetGroupIdRewardPane": "0",
            "questDirectionId": "0",
        }
        q3479_survivor_objective = {
            "ID": "4467",
            "type": "12",
            "data": "11070",
            "worldLocationsIdIndicator00": "12155",
            "worldLocationsIdIndicator01": "12156",
            "worldLocationsIdIndicator02": "12157",
            "worldLocationsIdIndicator03": "12158",
            "targetGroupIdRewardPane": "0",
            "questDirectionId": "0",
        }
        q3479_yeti_objective = {
            "ID": "4564",
            "type": "8",
            "data": "7288",
            "count": "8",
            "worldLocationsIdIndicator00": "12155",
            "worldLocationsIdIndicator01": "12156",
            "worldLocationsIdIndicator02": "12157",
            "worldLocationsIdIndicator03": "12158",
            "targetGroupIdRewardPane": "0",
            "questDirectionId": "0",
        }
        q3480_survivor_objective = {
            "ID": "4470",
            "type": "12",
            "data": "11070",
            "worldLocationsIdIndicator00": "12155",
            "worldLocationsIdIndicator01": "12156",
            "worldLocationsIdIndicator02": "12157",
            "worldLocationsIdIndicator03": "12158",
            "targetGroupIdRewardPane": "0",
            "questDirectionId": "0",
        }
        q3480_yeti_objective = {
            "ID": "4565",
            "type": "8",
            "data": "1463",
            "count": "8",
            "worldLocationsIdIndicator00": "12155",
            "worldLocationsIdIndicator01": "12156",
            "worldLocationsIdIndicator02": "12157",
            "worldLocationsIdIndicator03": "12158",
            "targetGroupIdRewardPane": "0",
            "questDirectionId": "0",
        }
        q3668_lusk_objective = {
            "ID": "4744",
            "type": "3",
            "data": "12484",
            "count": "1",
            "worldLocationsIdIndicator00": "8857",
            "worldLocationsIdIndicator01": "0",
            "worldLocationsIdIndicator02": "0",
            "worldLocationsIdIndicator03": "0",
            "targetGroupIdRewardPane": "0",
            "questDirectionId": "0",
        }
        q3668_survivor_objective = {
            "ID": "4745",
            "type": "5",
            "data": "0",
            "count": "5",
            "worldLocationsIdIndicator00": "8858",
            "worldLocationsIdIndicator01": "0",
            "worldLocationsIdIndicator02": "0",
            "worldLocationsIdIndicator03": "0",
            "targetGroupIdRewardPane": "7261",
            "questDirectionId": "254",
        }
        q3668_skeech_objective = {
            "ID": "4791",
            "type": "8",
            "data": "7293",
            "count": "5",
            "worldLocationsIdIndicator00": "8858",
            "worldLocationsIdIndicator01": "8859",
            "worldLocationsIdIndicator02": "0",
            "worldLocationsIdIndicator03": "0",
            "targetGroupIdRewardPane": "0",
            "questDirectionId": "254",
        }
        direction_base = {f"questDirectionEntryId{index:02d}": "0" for index in range(16)}
        quest_directions = [
            {"ID": "400", "questDirectionFlags": "0", **direction_base, "questDirectionEntryId00": "410", "questDirectionEntryId01": "999", "worldZoneIdExcludedZone": "0"},
            {"ID": "401", "questDirectionFlags": "0", **direction_base, "questDirectionEntryId00": "411", "worldZoneIdExcludedZone": "0"},
            {"ID": "402", "questDirectionFlags": "0", **direction_base, "questDirectionEntryId00": "412", "worldZoneIdExcludedZone": "0"},
            {"ID": "250", "questDirectionFlags": "0", **direction_base, "questDirectionEntryId00": "417", "questDirectionEntryId01": "416", "worldZoneIdExcludedZone": "0"},
            {"ID": "254", "questDirectionFlags": "0", **direction_base, "questDirectionEntryId00": "427", "worldZoneIdExcludedZone": "0"},
        ]
        quest_direction_entries = [
            {"ID": "410", "worldLocation2Id": "303", "worldLocation2IdInactive": "0", "worldZoneId": "20", "questDirectionEntryFlags": "0", "questDirectionFactionEnum": "0"},
            {"ID": "411", "worldLocation2Id": "304", "worldLocation2IdInactive": "305", "worldZoneId": "20", "questDirectionEntryFlags": "0", "questDirectionFactionEnum": "0"},
            {"ID": "412", "worldLocation2Id": "306", "worldLocation2IdInactive": "0", "worldZoneId": "20", "questDirectionEntryFlags": "0", "questDirectionFactionEnum": "0"},
            {"ID": "417", "worldLocation2Id": "7778", "worldLocation2IdInactive": "0", "worldZoneId": "852", "questDirectionEntryFlags": "0", "questDirectionFactionEnum": "0"},
            {"ID": "416", "worldLocation2Id": "27633", "worldLocation2IdInactive": "0", "worldZoneId": "852", "questDirectionEntryFlags": "0", "questDirectionFactionEnum": "0"},
            {"ID": "427", "worldLocation2Id": "27690", "worldLocation2IdInactive": "0", "worldZoneId": "604", "questDirectionEntryFlags": "0", "questDirectionFactionEnum": "0"},
        ]
        target_group = {
            "ID": "600",
            "localizedTextIdDisplayString": "201",
            "type": "1",
            "data0": "900",
            "data1": "901",
            "data2": "0",
            "data3": "0",
            "data4": "0",
            "data5": "0",
            "data6": "0",
        }
        q4696_target_group = {
            "ID": "4323",
            "localizedTextIdDisplayString": "201",
            "type": "1",
            "data0": "17189",
            "data1": "19595",
            "data2": "0",
            "data3": "0",
            "data4": "0",
            "data5": "0",
            "data6": "0",
        }
        q4694_target_group = {
            "ID": "7407",
            "localizedTextIdDisplayString": "201",
            "type": "1",
            "data0": "50904",
            "data1": "0",
            "data2": "0",
            "data3": "0",
            "data4": "0",
            "data5": "0",
            "data6": "0",
        }
        q3673_target_group = {
            "ID": "1106",
            "localizedTextIdDisplayString": "201",
            "type": "1",
            "data0": "12521",
            "data1": "13150",
            "data2": "13151",
            "data3": "0",
            "data4": "0",
            "data5": "0",
            "data6": "0",
        }
        q3486_target_group = {
            "ID": "4985",
            "localizedTextIdDisplayString": "201",
            "type": "1",
            "data0": "11924",
            "data1": "11925",
            "data2": "0",
            "data3": "0",
            "data4": "0",
            "data5": "0",
            "data6": "0",
        }
        q3741_supply_crate_target_group = {
            "ID": "4372",
            "localizedTextIdDisplayString": "201",
            "type": "1",
            "data0": "12919",
            "data1": "0",
            "data2": "0",
            "data3": "0",
            "data4": "0",
            "data5": "0",
            "data6": "0",
        }
        q5597_chua_explosives_target_group = {
            "ID": "4373",
            "localizedTextIdDisplayString": "201",
            "type": "1",
            "data0": "24286",
            "data1": "0",
            "data2": "0",
            "data3": "0",
            "data4": "0",
            "data5": "0",
            "data6": "0",
        }
        q3479_yeti_target_group = {
            "ID": "7288",
            "localizedTextIdDisplayString": "201",
            "type": "11",
            "data0": "7287",
            "data1": "1463",
            "data2": "0",
            "data3": "0",
            "data4": "0",
            "data5": "0",
            "data6": "0",
        }
        q3479_yeti_holdout_target_group = {
            "ID": "7287",
            "localizedTextIdDisplayString": "201",
            "type": "1",
            "data0": "36331",
            "data1": "36335",
            "data2": "51126",
            "data3": "0",
            "data4": "0",
            "data5": "0",
            "data6": "0",
        }
        q3480_yeti_target_group = {
            "ID": "1463",
            "localizedTextIdDisplayString": "201",
            "type": "1",
            "data0": "12844",
            "data1": "11945",
            "data2": "11948",
            "data3": "13116",
            "data4": "13117",
            "data5": "13959",
            "data6": "51126",
        }
        q3797_nested_target_group = {
            "ID": "1177",
            "localizedTextIdDisplayString": "201",
            "type": "11",
            "data0": "1113",
            "data1": "6406",
            "data2": "7777",
            "data3": "0",
            "data4": "0",
            "data5": "0",
            "data6": "0",
        }
        q3797_creature_list_target_group = {
            "ID": "1113",
            "localizedTextIdDisplayString": "201",
            "type": "9",
            "data0": "11963",
            "data1": "11962",
            "data2": "11956",
            "data3": "0",
            "data4": "0",
            "data5": "0",
            "data6": "0",
        }
        q3797_rootbrute_yeti_target_group = {
            "ID": "6406",
            "localizedTextIdDisplayString": "201",
            "type": "1",
            "data0": "11945",
            "data1": "11948",
            "data2": "12212",
            "data3": "12213",
            "data4": "0",
            "data5": "0",
            "data6": "0",
        }
        q3668_survivor_target_group = {
            "ID": "7261",
            "localizedTextIdDisplayString": "201",
            "type": "1",
            "data0": "14124",
            "data1": "0",
            "data2": "0",
            "data3": "0",
            "data4": "0",
            "data5": "0",
            "data6": "0",
        }
        q3668_nested_skeech_target_group = {
            "ID": "7293",
            "localizedTextIdDisplayString": "201",
            "type": "11",
            "data0": "7292",
            "data1": "960",
            "data2": "0",
            "data3": "0",
            "data4": "0",
            "data5": "0",
            "data6": "0",
        }
        q3668_named_skeech_target_group = {
            "ID": "7292",
            "localizedTextIdDisplayString": "201",
            "type": "1",
            "data0": "14054",
            "data1": "36429",
            "data2": "17545",
            "data3": "0",
            "data4": "0",
            "data5": "0",
            "data6": "0",
        }
        q3668_common_skeech_target_group = {
            "ID": "960",
            "localizedTextIdDisplayString": "201",
            "type": "1",
            "data0": "11907",
            "data1": "11910",
            "data2": "11912",
            "data3": "11913",
            "data4": "11917",
            "data5": "36884",
            "data6": "0",
        }
        world_locations = [
            {"ID": str(location_id), "radius": "1", "maxVerticalDistance": "0", "position0": str(location_id), "position1": "2", "position2": "3", "facing0": "0", "facing1": "0", "facing2": "0", "facing3": "1", "worldId": "1", "worldZoneId": "20", "phases": "1"}
            for location_id in range(300, 307)
        ]
        world_locations.append({"ID": "12649", "radius": "5", "maxVerticalDistance": "2", "position0": "6207.62", "position1": "-888.804", "position2": "-2818.61", "facing0": "0", "facing1": "0", "facing2": "0", "facing3": "1", "worldId": "51", "worldZoneId": "973", "phases": "1"})
        world_locations.extend([
            {"ID": "7778", "radius": "5", "maxVerticalDistance": "0", "position0": "4146.54", "position1": "-721.715", "position2": "-5729.8", "facing0": "0", "facing1": "0", "facing2": "0", "facing3": "1", "worldId": "426", "worldZoneId": "852", "phases": "1"},
            {"ID": "27633", "radius": "1", "maxVerticalDistance": "0", "position0": "4153.24", "position1": "-721.062", "position2": "-5716.75", "facing0": "0", "facing1": "0", "facing2": "0", "facing3": "1", "worldId": "426", "worldZoneId": "852", "phases": "4294967295"},
            {"ID": "7807", "radius": "96.4742", "maxVerticalDistance": "0", "position0": "4109.37", "position1": "-683.032", "position2": "-5880.66", "facing0": "0", "facing1": "0", "facing2": "0", "facing3": "1", "worldId": "426", "worldZoneId": "729", "phases": "1"},
            {"ID": "17590", "radius": "1", "maxVerticalDistance": "0", "position0": "6204.08", "position1": "-888.474", "position2": "-2809.91", "facing0": "0", "facing1": "0", "facing2": "0", "facing3": "1", "worldId": "51", "worldZoneId": "973", "phases": "4294967295"},
            {"ID": "17586", "radius": "25", "maxVerticalDistance": "0", "position0": "6180.92", "position1": "-838.147", "position2": "-3068.84", "facing0": "0", "facing1": "0", "facing2": "0", "facing3": "1", "worldId": "51", "worldZoneId": "976", "phases": "1"},
            {"ID": "17588", "radius": "25", "maxVerticalDistance": "0", "position0": "6281.38", "position1": "-820.08", "position2": "-3154.91", "facing0": "0", "facing1": "0", "facing2": "0", "facing3": "1", "worldId": "51", "worldZoneId": "976", "phases": "1"},
            {"ID": "17589", "radius": "25", "maxVerticalDistance": "0", "position0": "6367.08", "position1": "-843.955", "position2": "-3031.95", "facing0": "0", "facing1": "0", "facing2": "0", "facing3": "1", "worldId": "51", "worldZoneId": "976", "phases": "1"},
            {"ID": "17803", "radius": "1.76183", "maxVerticalDistance": "0", "position0": "-7662.75", "position1": "-942.948", "position2": "-671.901", "facing0": "0", "facing1": "0", "facing2": "0", "facing3": "1", "worldId": "870", "worldZoneId": "1227", "phases": "4294967295"},
            {"ID": "17896", "radius": "55", "maxVerticalDistance": "0", "position0": "-7133.34", "position1": "-994.206", "position2": "-856.854", "facing0": "0", "facing1": "0", "facing2": "0", "facing3": "1", "worldId": "870", "worldZoneId": "1218", "phases": "4294967295"},
            {"ID": "17898", "radius": "55", "maxVerticalDistance": "0", "position0": "-7022.49", "position1": "-995.437", "position2": "-946.002", "facing0": "0", "facing1": "0", "facing2": "0", "facing3": "1", "worldId": "870", "worldZoneId": "1325", "phases": "4294967295"},
            {"ID": "17897", "radius": "55", "maxVerticalDistance": "0", "position0": "-7270.21", "position1": "-992.368", "position2": "-828.018", "facing0": "0", "facing1": "0", "facing2": "0", "facing3": "1", "worldId": "870", "worldZoneId": "1325", "phases": "4294967295"},
            {"ID": "17899", "radius": "55", "maxVerticalDistance": "0", "position0": "-7144", "position1": "-995.343", "position2": "-1008.03", "facing0": "0", "facing1": "0", "facing2": "0", "facing3": "1", "worldId": "870", "worldZoneId": "1325", "phases": "4294967295"},
            {"ID": "29526", "radius": "5", "maxVerticalDistance": "0", "position0": "4486.46", "position1": "-724.843", "position2": "-5391.26", "facing0": "0", "facing1": "0", "facing2": "0", "facing3": "1", "worldId": "426", "worldZoneId": "597", "phases": "4294967295"},
            {"ID": "9340", "radius": "5", "maxVerticalDistance": "0", "position0": "4341.27", "position1": "-751.765", "position2": "-5682.93", "facing0": "0", "facing1": "0", "facing2": "0", "facing3": "1", "worldId": "426", "worldZoneId": "595", "phases": "1"},
            {"ID": "7726", "radius": "5", "maxVerticalDistance": "0", "position0": "4185.76", "position1": "-722.095", "position2": "-5695.75", "facing0": "0", "facing1": "0", "facing2": "0", "facing3": "1", "worldId": "426", "worldZoneId": "728", "phases": "1"},
            {"ID": "7727", "radius": "5", "maxVerticalDistance": "0", "position0": "4339.04", "position1": "-751.768", "position2": "-5678.82", "facing0": "0", "facing1": "0", "facing2": "0", "facing3": "1", "worldId": "426", "worldZoneId": "595", "phases": "1"},
            {"ID": "9121", "radius": "5", "maxVerticalDistance": "0", "position0": "4355.91", "position1": "-750.544", "position2": "-5681.91", "facing0": "0", "facing1": "0", "facing2": "0", "facing3": "1", "worldId": "426", "worldZoneId": "595", "phases": "1"},
            {"ID": "8857", "radius": "5", "maxVerticalDistance": "0", "position0": "4801.81", "position1": "-710.664", "position2": "-5651.88", "facing0": "0", "facing1": "0", "facing2": "0", "facing3": "1", "worldId": "426", "worldZoneId": "604", "phases": "1"},
            {"ID": "8858", "radius": "49.0244", "maxVerticalDistance": "0", "position0": "4906.76", "position1": "-709.183", "position2": "-5679.33", "facing0": "0", "facing1": "0", "facing2": "0", "facing3": "1", "worldId": "426", "worldZoneId": "609", "phases": "1"},
            {"ID": "8859", "radius": "85.6331", "maxVerticalDistance": "0", "position0": "5019.01", "position1": "-719.501", "position2": "-5722.97", "facing0": "0", "facing1": "0", "facing2": "0", "facing3": "1", "worldId": "426", "worldZoneId": "609", "phases": "1"},
            {"ID": "27690", "radius": "9.32868", "maxVerticalDistance": "0", "position0": "4828.95", "position1": "-706.667", "position2": "-5673.38", "facing0": "0", "facing1": "0", "facing2": "0", "facing3": "1", "worldId": "426", "worldZoneId": "604", "phases": "1"},
            {"ID": "12354", "radius": "5", "maxVerticalDistance": "0", "position0": "3832.91", "position1": "-1018.09", "position2": "-4642.18", "facing0": "0", "facing1": "0", "facing2": "0", "facing3": "1", "worldId": "51", "worldZoneId": "23", "phases": "1048573"},
            {"ID": "11669", "radius": "1", "maxVerticalDistance": "0", "position0": "4357.81", "position1": "-751.7", "position2": "-5717", "facing0": "0", "facing1": "0", "facing2": "0", "facing3": "1", "worldId": "426", "worldZoneId": "595", "phases": "1"},
            {"ID": "9706", "radius": "1", "maxVerticalDistance": "0", "position0": "4536.17", "position1": "-715.836", "position2": "-5801.43", "facing0": "0", "facing1": "0", "facing2": "0", "facing3": "1", "worldId": "426", "worldZoneId": "597", "phases": "1"},
            {"ID": "40764", "radius": "40", "maxVerticalDistance": "0", "position0": "4560.22", "position1": "-710.622", "position2": "-5803.48", "facing0": "0", "facing1": "0", "facing2": "0", "facing3": "1", "worldId": "426", "worldZoneId": "597", "phases": "4294967295"},
            {"ID": "8867", "radius": "1", "maxVerticalDistance": "0", "position0": "4386", "position1": "-744.902", "position2": "-5636.93", "facing0": "0", "facing1": "0", "facing2": "0", "facing3": "1", "worldId": "426", "worldZoneId": "595", "phases": "1"},
            {"ID": "8868", "radius": "1", "maxVerticalDistance": "0", "position0": "4367.45", "position1": "-743.67", "position2": "-5638.63", "facing0": "0", "facing1": "0", "facing2": "0", "facing3": "1", "worldId": "426", "worldZoneId": "595", "phases": "1"},
            {"ID": "8869", "radius": "1", "maxVerticalDistance": "0", "position0": "4347.79", "position1": "-744.507", "position2": "-5636.98", "facing0": "0", "facing1": "0", "facing2": "0", "facing3": "1", "worldId": "426", "worldZoneId": "595", "phases": "1"},
            {"ID": "12155", "radius": "72.1792", "maxVerticalDistance": "0", "position0": "4153.63", "position1": "-723.256", "position2": "-5374.74", "facing0": "0", "facing1": "0", "facing2": "0", "facing3": "1", "worldId": "426", "worldZoneId": "647", "phases": "1"},
            {"ID": "12156", "radius": "82.2293", "maxVerticalDistance": "0", "position0": "3998.79", "position1": "-721.519", "position2": "-5396.13", "facing0": "0", "facing1": "0", "facing2": "0", "facing3": "1", "worldId": "426", "worldZoneId": "646", "phases": "1"},
            {"ID": "12157", "radius": "78.6236", "maxVerticalDistance": "0", "position0": "4049.09", "position1": "-735.689", "position2": "-5470.09", "facing0": "0", "facing1": "0", "facing2": "0", "facing3": "1", "worldId": "426", "worldZoneId": "651", "phases": "1"},
            {"ID": "12158", "radius": "60.1585", "maxVerticalDistance": "0", "position0": "4175.22", "position1": "-737.899", "position2": "-5463.8", "facing0": "0", "facing1": "0", "facing2": "0", "facing3": "1", "worldId": "426", "worldZoneId": "651", "phases": "1"},
            {"ID": "9181", "radius": "112.416", "maxVerticalDistance": "0", "position0": "4554.93", "position1": "-740.494", "position2": "-5512.9", "facing0": "0", "facing1": "0", "facing2": "0", "facing3": "1", "worldId": "426", "worldZoneId": "597", "phases": "1"},
            {"ID": "9182", "radius": "84.8811", "maxVerticalDistance": "0", "position0": "4400.1", "position1": "-744.5", "position2": "-5502.49", "facing0": "0", "facing1": "0", "facing2": "0", "facing3": "1", "worldId": "426", "worldZoneId": "597", "phases": "1"},
            {"ID": "9962", "radius": "97.561", "maxVerticalDistance": "0", "position0": "4510.16", "position1": "-741.433", "position2": "-5644.08", "facing0": "0", "facing1": "0", "facing2": "0", "facing3": "1", "worldId": "426", "worldZoneId": "597", "phases": "1"},
        ])
        world_zones = [
            {"ID": "20", "localizedTextIdName": "200", "parentZoneId": "0", "allowAccess": "1", "color": "0", "soundZoneKitId": "0", "worldLocation2IdExit": "0", "flags": "0", "zonePvpRulesEnum": "0", "rewardRotationContentId": "0"},
            {"ID": "973", "localizedTextIdName": "200", "parentZoneId": "0", "allowAccess": "1", "color": "0", "soundZoneKitId": "0", "worldLocation2IdExit": "0", "flags": "0", "zonePvpRulesEnum": "0", "rewardRotationContentId": "0"},
            {"ID": "976", "localizedTextIdName": "200", "parentZoneId": "0", "allowAccess": "1", "color": "0", "soundZoneKitId": "0", "worldLocation2IdExit": "0", "flags": "0", "zonePvpRulesEnum": "0", "rewardRotationContentId": "0"},
            {"ID": "1885", "localizedTextIdName": "200", "parentZoneId": "0", "allowAccess": "1", "color": "0", "soundZoneKitId": "0", "worldLocation2IdExit": "0", "flags": "0", "zonePvpRulesEnum": "0", "rewardRotationContentId": "0"},
            {"ID": "622", "localizedTextIdName": "214", "parentZoneId": "0", "allowAccess": "1", "color": "0", "soundZoneKitId": "0", "worldLocation2IdExit": "0", "flags": "0", "zonePvpRulesEnum": "0", "rewardRotationContentId": "0"},
            {"ID": "1218", "localizedTextIdName": "216", "parentZoneId": "622", "allowAccess": "1", "color": "0", "soundZoneKitId": "0", "worldLocation2IdExit": "0", "flags": "0", "zonePvpRulesEnum": "0", "rewardRotationContentId": "0"},
            {"ID": "1227", "localizedTextIdName": "216", "parentZoneId": "622", "allowAccess": "1", "color": "0", "soundZoneKitId": "0", "worldLocation2IdExit": "0", "flags": "0", "zonePvpRulesEnum": "0", "rewardRotationContentId": "0"},
            {"ID": "1325", "localizedTextIdName": "217", "parentZoneId": "1218", "allowAccess": "1", "color": "0", "soundZoneKitId": "0", "worldLocation2IdExit": "0", "flags": "0", "zonePvpRulesEnum": "0", "rewardRotationContentId": "0"},
            {"ID": "597", "localizedTextIdName": "200", "parentZoneId": "0", "allowAccess": "1", "color": "0", "soundZoneKitId": "0", "worldLocation2IdExit": "0", "flags": "0", "zonePvpRulesEnum": "0", "rewardRotationContentId": "0"},
            {"ID": "595", "localizedTextIdName": "200", "parentZoneId": "0", "allowAccess": "1", "color": "0", "soundZoneKitId": "0", "worldLocation2IdExit": "0", "flags": "0", "zonePvpRulesEnum": "0", "rewardRotationContentId": "0"},
            {"ID": "604", "localizedTextIdName": "200", "parentZoneId": "0", "allowAccess": "1", "color": "0", "soundZoneKitId": "0", "worldLocation2IdExit": "0", "flags": "0", "zonePvpRulesEnum": "0", "rewardRotationContentId": "0"},
            {"ID": "609", "localizedTextIdName": "200", "parentZoneId": "0", "allowAccess": "1", "color": "0", "soundZoneKitId": "0", "worldLocation2IdExit": "0", "flags": "0", "zonePvpRulesEnum": "0", "rewardRotationContentId": "0"},
            {"ID": "729", "localizedTextIdName": "200", "parentZoneId": "0", "allowAccess": "1", "color": "0", "soundZoneKitId": "0", "worldLocation2IdExit": "0", "flags": "0", "zonePvpRulesEnum": "0", "rewardRotationContentId": "0"},
            {"ID": "728", "localizedTextIdName": "200", "parentZoneId": "0", "allowAccess": "1", "color": "0", "soundZoneKitId": "0", "worldLocation2IdExit": "0", "flags": "0", "zonePvpRulesEnum": "0", "rewardRotationContentId": "0"},
            {"ID": "852", "localizedTextIdName": "200", "parentZoneId": "0", "allowAccess": "1", "color": "0", "soundZoneKitId": "0", "worldLocation2IdExit": "0", "flags": "0", "zonePvpRulesEnum": "0", "rewardRotationContentId": "0"},
            {"ID": "23", "localizedTextIdName": "200", "parentZoneId": "0", "allowAccess": "1", "color": "0", "soundZoneKitId": "0", "worldLocation2IdExit": "0", "flags": "0", "zonePvpRulesEnum": "0", "rewardRotationContentId": "0"},
            {"ID": "35", "localizedTextIdName": "200", "parentZoneId": "0", "allowAccess": "1", "color": "0", "soundZoneKitId": "0", "worldLocation2IdExit": "0", "flags": "0", "zonePvpRulesEnum": "0", "rewardRotationContentId": "0"},
            {"ID": "646", "localizedTextIdName": "200", "parentZoneId": "0", "allowAccess": "1", "color": "0", "soundZoneKitId": "0", "worldLocation2IdExit": "0", "flags": "0", "zonePvpRulesEnum": "0", "rewardRotationContentId": "0"},
            {"ID": "647", "localizedTextIdName": "200", "parentZoneId": "0", "allowAccess": "1", "color": "0", "soundZoneKitId": "0", "worldLocation2IdExit": "0", "flags": "0", "zonePvpRulesEnum": "0", "rewardRotationContentId": "0"},
            {"ID": "651", "localizedTextIdName": "200", "parentZoneId": "0", "allowAccess": "1", "color": "0", "soundZoneKitId": "0", "worldLocation2IdExit": "0", "flags": "0", "zonePvpRulesEnum": "0", "rewardRotationContentId": "0"},
        ]
        quest_output = [
            {"quest_id": 10, "bucket": "generic_table_driven", "completion_status": "not_retail_complete"},
            {"quest_id": 3673, "bucket": "curated_full", "completion_status": "not_retail_complete"},
            {"quest_id": 4696, "bucket": "curated_partial_stub_script", "completion_status": "not_retail_complete"},
            {"quest_id": 4694, "bucket": "generic_with_script", "completion_status": "not_retail_complete"},
            {"quest_id": 4526, "bucket": "generic_with_script", "completion_status": "not_retail_complete"},
            {"quest_id": 3486, "bucket": "curated_full", "completion_status": "not_retail_complete"},
            {"quest_id": 3741, "bucket": "generic_with_script", "completion_status": "not_retail_complete"},
            {"quest_id": 3797, "bucket": "generic_with_script", "completion_status": "not_retail_complete"},
            {"quest_id": 5610, "bucket": "no_objectives", "completion_status": "not_retail_complete"},
            {"quest_id": 5597, "bucket": "curated_full", "completion_status": "not_retail_complete"},
            {"quest_id": 3670, "bucket": "no_objectives", "completion_status": "not_retail_complete"},
            {"quest_id": 3671, "bucket": "no_objectives", "completion_status": "not_retail_complete"},
            {"quest_id": 3668, "bucket": "generic_with_script", "completion_status": "not_retail_complete"},
            {"quest_id": 3479, "bucket": "generic_with_script", "completion_status": "not_retail_complete"},
            {"quest_id": 3480, "bucket": "generic_with_script", "completion_status": "not_retail_complete"},
            {"quest_id": 3963, "bucket": "curated_full", "completion_status": "not_retail_complete"},
        ]

        rows = audit.build_quest_world_dependency_rows(
            strings,
            [quest, q3673, q4696, q4694, q4526, q3486, q3741, q3797, q3668, q5610, q5597, q3670, q3671, q3479, q3480, q3963],
            [objective, q3673_checklist_objective, q3673_flare_one_objective, q3673_flare_two_objective, q3673_flare_three_objective, q3479_survivor_objective, q3479_yeti_objective, q3480_survivor_objective, q3480_yeti_objective, q3741_supply_objective, q5597_chua_explosives_objective, q3797_kill_objective, q3668_lusk_objective, q3668_survivor_objective, q3668_skeech_objective, q4696_objective, q4696_activate_objective, q4694_activate_objective, q4694_event_objective, q4526_wreckage_objective, q4526_anomaly_objective, q3486_arrival_objective, q3486_loftite_objective],
            quest_output,
            quest_directions,
            quest_direction_entries,
            [target_group, q3673_target_group, q4696_target_group, q4694_target_group, q3479_yeti_target_group, q3479_yeti_holdout_target_group, q3480_yeti_target_group, q3486_target_group, q3741_supply_crate_target_group, q5597_chua_explosives_target_group, q3797_nested_target_group, q3797_creature_list_target_group, q3797_rootbrute_yeti_target_group, q3668_survivor_target_group, q3668_nested_skeech_target_group, q3668_named_skeech_target_group, q3668_common_skeech_target_group],
            world_locations,
            world_zones,
            runtime_entity_rows=[
                {"id": "100", "type": "0", "creature": "900", "world": "1", "area": "20"},
                {"id": "101", "type": "0", "creature": "19595", "world": "51", "area": "976"},
            ],
        )
        dependency_types = {row["dependency_type"] for row in rows}

        self.assertIn("quest_world_zone", dependency_types)
        self.assertIn("quest_receiver_location", dependency_types)
        self.assertIn("quest_alt_receiver_location", dependency_types)
        self.assertIn("quest_direction", dependency_types)
        self.assertIn("quest_direction_entry", dependency_types)
        self.assertIn("quest_direction_entry_inactive_location", dependency_types)
        self.assertIn("objective_indicator_location", dependency_types)
        self.assertIn("target_group", dependency_types)
        self.assertIn("target_group_member", dependency_types)
        q10_zone = next(row for row in rows if row["quest_id"] == 10 and row["dependency_type"] == "quest_world_zone")
        self.assertIn("Quest2 10 is assigned to WorldZone 20 (Zone)", q10_zone["blocker_summary"])
        self.assertIn("quest activation/visibility", q10_zone["blocker_summary"])
        q10_receiver = next(row for row in rows if row["quest_id"] == 10 and row["dependency_type"] == "quest_receiver_location")
        self.assertIn("receiver WorldLocation2 300 in WorldZone 20 (Zone) at 300,2,3 radius 1", q10_receiver["blocker_summary"])
        self.assertIn("completion routing", q10_receiver["blocker_summary"])
        q10_alt_receiver = next(row for row in rows if row["quest_id"] == 10 and row["dependency_type"] == "quest_alt_receiver_location")
        self.assertIn("alternate receiver slot 00 WorldLocation2 301", q10_alt_receiver["blocker_summary"])
        self.assertIn("with prerequisite 700", q10_alt_receiver["blocker_summary"])
        q10_direction = next(row for row in rows if row["quest_id"] == 10 and row["dependency_type"] == "quest_direction" and row["quest_direction_id"] == 400)
        self.assertIn("QuestDirection 400", q10_direction["blocker_summary"])
        self.assertIn("path-arrow routing", q10_direction["blocker_summary"])
        missing_direction_entry = next(row for row in rows if row["quest_id"] == 10 and row["dependency_type"] == "quest_direction_entry" and row["quest_direction_entry_id"] == 999)
        self.assertEqual("blocked_missing_client_quest_direction_entry_row_runtime_fail_closed", missing_direction_entry["evidence_status"])
        self.assertIn("QuestTests.cs", missing_direction_entry["evidence_sources"])
        self.assertIn("fails closed", missing_direction_entry["blocker_summary"])
        q10_direction_entry = next(row for row in rows if row["quest_id"] == 10 and row["dependency_type"] == "quest_direction_entry" and row["quest_direction_entry_id"] == 410)
        self.assertIn("QuestDirection 400 entry 410", q10_direction_entry["blocker_summary"])
        self.assertIn("WorldLocation2 303", q10_direction_entry["blocker_summary"])
        q10_inactive_direction = next(row for row in rows if row["quest_id"] == 10 and row["dependency_type"] == "quest_direction_entry_inactive_location")
        self.assertIn("inactive QuestDirection 401 entry 411", q10_inactive_direction["blocker_summary"])
        self.assertIn("WorldLocation2 305", q10_inactive_direction["blocker_summary"])
        q10_indicator = next(row for row in rows if row["quest_id"] == 10 and row["dependency_type"] == "objective_indicator_location" and row["objective_id"] == 500)
        self.assertIn("objective 500 type 2 indicator slot worldLocationsIdIndicator00 WorldLocation2 302", q10_indicator["blocker_summary"])
        self.assertIn("objective credit timing", q10_indicator["blocker_summary"])
        q10_target_group = next(row for row in rows if row["quest_id"] == 10 and row["dependency_type"] == "target_group" and row["target_group_id"] == 600)
        self.assertIn("objective 500 type 2 references TargetGroup 600 (Target group) type 1", q10_target_group["blocker_summary"])
        self.assertIn(900, {row["target_group_data"] for row in rows if row["dependency_type"] == "target_group_member"})
        runtime_member = next(row for row in rows if row["dependency_type"] == "target_group_member" and row["target_group_data"] == 900)
        self.assertEqual("runtime_entity_spawn_present_pending_credit_client_smoke", runtime_member["evidence_status"])
        self.assertIn("runtime_world_seed.sql", runtime_member["evidence_sources"])
        self.assertIn("world(s) 1", runtime_member["blocker_summary"])
        pending_member = next(row for row in rows if row["dependency_type"] == "target_group_member" and row["target_group_data"] == 901)
        self.assertEqual("client_target_group_member_pending_spawn_and_credit_smoke", pending_member["evidence_status"])
        self.assertIn("TargetGroup 600 (Target group) member slot data1 references Creature2/object 901", pending_member["blocker_summary"])
        self.assertIn("kill/interact credit semantics", pending_member["blocker_summary"])
        q4696_credit_member = next(row for row in rows if row["quest_id"] == 4696 and row["dependency_type"] == "target_group_member" and row["target_group_data"] == 19595)
        self.assertEqual("runtime_q4696_runtime_spawn_enemy_kill_credit_tested_pending_client_smoke", q4696_credit_member["evidence_status"])
        self.assertIn("Q4696LeavingTheTempleOfOsiric.cs", q4696_credit_member["evidence_sources"])
        self.assertIn("GalerasQuestScriptTests.cs", q4696_credit_member["evidence_sources"])
        self.assertIn("QuestTests.cs", q4696_credit_member["evidence_sources"])
        self.assertIn("world(s) 51", q4696_credit_member["blocker_summary"])
        self.assertIn("Target-group member 19595", q4696_credit_member["blocker_summary"])
        self.assertIn("credits QuestObjectiveType.ActivateEntity", q4696_credit_member["blocker_summary"])
        self.assertIn("live/client combat objective smoke", q4696_credit_member["blocker_summary"])
        q4696_script_spawn_member = next(row for row in rows if row["quest_id"] == 4696 and row["dependency_type"] == "target_group_member" and row["target_group_data"] == 17189)
        self.assertEqual("runtime_q4696_script_spawn_enemy_kill_credit_tested_pending_client_smoke", q4696_script_spawn_member["evidence_status"])
        self.assertIn("Creature2.tbl.sql", q4696_script_spawn_member["evidence_sources"])
        self.assertIn("GalerasMapScriptTests.cs", q4696_script_spawn_member["evidence_sources"])
        self.assertIn("Q4696LeavingTheTempleOfOsiric.cs", q4696_script_spawn_member["evidence_sources"])
        self.assertIn("GalerasQuestScriptTests.cs", q4696_script_spawn_member["evidence_sources"])
        self.assertIn("17586/17588/17589", q4696_script_spawn_member["blocker_summary"])
        self.assertIn("credits QuestObjectiveType.ActivateEntity", q4696_script_spawn_member["blocker_summary"])
        q4694_script_spawn_member = next(row for row in rows if row["quest_id"] == 4694 and row["dependency_type"] == "target_group_member" and row["target_group_data"] == 50904)
        self.assertEqual("runtime_script_spawn_present_with_activate_credit_test_pending_dialog_client_smoke", q4694_script_spawn_member["evidence_status"])
        self.assertIn("Creature2.tbl.sql", q4694_script_spawn_member["evidence_sources"])
        self.assertIn("creature_spawn_map.csv", q4694_script_spawn_member["evidence_sources"])
        self.assertIn("Spell4Effects.tbl.sql", q4694_script_spawn_member["evidence_sources"])
        self.assertIn("runtime_world_seed.sql", q4694_script_spawn_member["evidence_sources"])
        self.assertIn("ClientActivateUnitHandlerTests.cs", q4694_script_spawn_member["evidence_sources"])
        self.assertIn("Q4694HoldTheLine.cs", q4694_script_spawn_member["evidence_sources"])
        self.assertIn("GalerasMapScriptTests.cs", q4694_script_spawn_member["evidence_sources"])
        self.assertIn("GalerasQuestScriptTests.cs", q4694_script_spawn_member["evidence_sources"])
        self.assertIn("Q4694 Trooper Vog holdout flag unit", q4694_script_spawn_member["blocker_summary"])
        self.assertIn("tested direct ClientActivateUnit credit", q4694_script_spawn_member["blocker_summary"])
        self.assertIn("activate spell 50151", q4694_script_spawn_member["blocker_summary"])
        self.assertIn("CompleteEvent data 111", q4694_script_spawn_member["blocker_summary"])
        self.assertIn("93 Creature2 23953 rows", q4694_script_spawn_member["blocker_summary"])
        self.assertIn("23 near Trooper Vog", q4694_script_spawn_member["blocker_summary"])
        q4696_location = next(row for row in rows if row["quest_id"] == 4696 and row["dependency_type"] == "objective_indicator_location")
        self.assertEqual("runtime_world_location_trigger_spawned_with_test_pending_client_smoke", q4696_location["evidence_status"])
        self.assertIn("GalerasMapScript.cs", q4696_location["evidence_sources"])
        self.assertIn("GalerasMapScriptTests.cs", q4696_location["evidence_sources"])
        self.assertIn("live/client quest smoke", q4696_location["blocker_summary"])
        q4696_scout_location = next(row for row in rows if row["quest_id"] == 4696 and row["dependency_type"] == "objective_indicator_location" and row["world_location_id"] == 17586)
        self.assertEqual("runtime_script_spawn_location_covered_with_test_pending_client_smoke", q4696_scout_location["evidence_status"])
        self.assertIn("Creature2.tbl.sql", q4696_scout_location["evidence_sources"])
        self.assertIn("GalerasMapScriptTests.cs", q4696_scout_location["evidence_sources"])
        self.assertIn("quest-state gating", q4696_scout_location["blocker_summary"])
        q3479_receiver_location = next(row for row in rows if row["quest_id"] == 3479 and row["dependency_type"] == "quest_receiver_location")
        self.assertEqual(7726, q3479_receiver_location["world_location_id"])
        self.assertEqual(
            "runtime_q3479_deadeye_receiver_location_spawn_and_receiver_cache_tested_external_video_dialog_observed_pending_local_dialog_ui_smoke",
            q3479_receiver_location["evidence_status"],
        )
        self.assertIn("VIDEO_EVIDENCE_MAP.md", q3479_receiver_location["evidence_sources"])
        self.assertIn("NorthernWildsBranchInteractionTests.cs", q3479_receiver_location["evidence_sources"])
        self.assertIn("GlobalQuestManagerPartialTableTests.cs", q3479_receiver_location["evidence_sources"])
        self.assertIn("source_coordinate_id 4370", q3479_receiver_location["blocker_summary"])
        self.assertIn("fallback spawn coverage at 7726", q3479_receiver_location["blocker_summary"])
        self.assertIn("indexes build 16042 Creature2 11063 as the Q3479 receiver", q3479_receiver_location["blocker_summary"])
        self.assertIn("Deadeye Brightland's completion dialog", q3479_receiver_location["blocker_summary"])
        q3480_receiver_location = next(row for row in rows if row["quest_id"] == 3480 and row["dependency_type"] == "quest_receiver_location")
        self.assertEqual(7726, q3480_receiver_location["world_location_id"])
        self.assertEqual(
            "runtime_q3480_deadeye_receiver_location_spawn_and_receiver_cache_tested_external_video_dialog_observed_pending_local_dialog_ui_smoke",
            q3480_receiver_location["evidence_status"],
        )
        self.assertIn("VIDEO_EVIDENCE_MAP.md", q3480_receiver_location["evidence_sources"])
        self.assertIn("GlobalQuestManagerPartialTableTests.cs", q3480_receiver_location["evidence_sources"])
        self.assertIn("Deadeye Brightland completion dialog", q3480_receiver_location["blocker_summary"])
        self.assertIn("indexes build 16042 Creature2 11063 as the Q3480 receiver", q3480_receiver_location["blocker_summary"])
        q3479_survivor_location = next(row for row in rows if row["quest_id"] == 3479 and row["dependency_type"] == "objective_indicator_location" and row["world_location_id"] == 12156)
        self.assertEqual("runtime_q3479_q3480_trapped_survivor_spawn_location_and_csi_credit_tested_external_video_progress_observed_pending_local_client_smoke", q3479_survivor_location["evidence_status"])
        self.assertIn("VIDEO_EVIDENCE_MAP.md", q3479_survivor_location["evidence_sources"])
        self.assertIn("NorthernWildsMapScript.cs", q3479_survivor_location["evidence_sources"])
        self.assertIn("InteractionObjectiveUpdaterTests.cs", q3479_survivor_location["evidence_sources"])
        self.assertIn("16 unique-name-mapped Trapped Survivor 11070 placements", q3479_survivor_location["blocker_summary"])
        self.assertIn("Q3479 Survivor progress", q3479_survivor_location["blocker_summary"])
        self.assertIn("source_coordinate_ids 3406, 3407, 3408, 15032", q3479_survivor_location["blocker_summary"])
        self.assertIn("shared yeti kill target objective has separate runtime spawn/credit coverage", q3479_survivor_location["blocker_summary"])
        q3480_survivor_location = next(row for row in rows if row["quest_id"] == 3480 and row["dependency_type"] == "objective_indicator_location" and row["world_location_id"] == 12156)
        self.assertEqual("runtime_q3480_trapped_survivor_spawn_location_and_csi_credit_tested_external_video_map_progress_observed_pending_local_client_smoke", q3480_survivor_location["evidence_status"])
        self.assertIn("VIDEO_EVIDENCE_MAP.md", q3480_survivor_location["evidence_sources"])
        self.assertIn("target-area map and 33% Rescue Trapped Survivors tracker progress", q3480_survivor_location["blocker_summary"])
        self.assertIn("shared yeti kill target objective has separate runtime spawn/credit coverage", q3480_survivor_location["blocker_summary"])
        q3479_yeti_location = next(row for row in rows if row["quest_id"] == 3479 and row["objective_id"] == 4564 and row["dependency_type"] == "objective_indicator_location" and row["world_location_id"] == 12158)
        self.assertEqual("runtime_q3479_q3480_shared_yeti_spawn_location_and_kill_credit_tested_external_video_combat_progress_observed_pending_local_client_smoke", q3479_yeti_location["evidence_status"])
        self.assertIn("VIDEO_EVIDENCE_MAP.md", q3479_yeti_location["evidence_sources"])
        self.assertIn("TargetGroup.tbl.sql", q3479_yeti_location["evidence_sources"])
        self.assertIn("NorthernWildsMapScript.cs", q3479_yeti_location["evidence_sources"])
        self.assertIn("QuestTests.cs", q3479_yeti_location["evidence_sources"])
        self.assertIn("source_coordinate_ids", q3479_yeti_location["blocker_summary"])
        self.assertIn("Yeti Snowstalker combat", q3479_yeti_location["blocker_summary"])
        self.assertIn("client combat kill smoke", q3479_yeti_location["blocker_summary"])
        q3480_yeti_location = next(row for row in rows if row["quest_id"] == 3480 and row["objective_id"] == 4565 and row["dependency_type"] == "objective_indicator_location" and row["world_location_id"] == 12155)
        self.assertEqual("runtime_q3480_shared_yeti_spawn_location_and_kill_credit_tested_external_video_map_progress_observed_pending_local_client_smoke", q3480_yeti_location["evidence_status"])
        self.assertIn("VIDEO_EVIDENCE_MAP.md", q3480_yeti_location["evidence_sources"])
        self.assertIn("Q3480 kill-objective indicator location 12155", q3480_yeti_location["blocker_summary"])
        self.assertIn("64% Kill yeti tracker progress", q3480_yeti_location["blocker_summary"])
        q3479_yeti_target_group = next(row for row in rows if row["quest_id"] == 3479 and row["objective_id"] == 4564 and row["dependency_type"] == "target_group" and row["target_group_id"] == 7288)
        self.assertEqual("QuestObjective.data", q3479_yeti_target_group["dependency_source"])
        self.assertEqual(11, q3479_yeti_target_group["target_group_type"])
        q3479_nested_yeti_group = next(row for row in rows if row["quest_id"] == 3479 and row["objective_id"] == 4564 and row["dependency_type"] == "nested_target_group" and row["target_group_id"] == 7288 and row["target_group_data"] == 1463)
        self.assertEqual("client_nested_target_group_pending_member_smoke", q3479_nested_yeti_group["evidence_status"])
        q3479_yeti_member = next(row for row in rows if row["quest_id"] == 3479 and row["objective_id"] == 4564 and row["dependency_type"] == "target_group_member" and row["target_group_id"] == 1463 and row["target_group_data"] == 11945)
        self.assertEqual("runtime_q3479_yeti_snowstalker_target_group_member_spawn_and_kill_credit_tested_external_video_combat_observed_pending_local_client_smoke", q3479_yeti_member["evidence_status"])
        self.assertIn("VIDEO_EVIDENCE_MAP.md", q3479_yeti_member["evidence_sources"])
        self.assertIn("NorthernWildsMapScript.cs", q3479_yeti_member["evidence_sources"])
        self.assertIn("QuestTests.cs", q3479_yeti_member["evidence_sources"])
        self.assertIn("TargetGroup 7288", q3479_yeti_member["blocker_summary"])
        self.assertIn("visibly names Yeti Snowstalker", q3479_yeti_member["blocker_summary"])
        self.assertIn("source_coordinate_ids 7915699", q3479_yeti_member["blocker_summary"])
        q3480_yeti_member = next(row for row in rows if row["quest_id"] == 3480 and row["objective_id"] == 4565 and row["dependency_type"] == "target_group_member" and row["target_group_id"] == 1463 and row["target_group_data"] == 11948)
        self.assertEqual("runtime_q3479_q3480_shared_yeti_target_group_member_spawn_and_kill_credit_tested_pending_client_smoke", q3480_yeti_member["evidence_status"])
        self.assertIn("TargetGroup 1463", q3480_yeti_member["blocker_summary"])
        self.assertIn("source_coordinate_ids 8110657", q3480_yeti_member["blocker_summary"])
        q4696_receiver_location = next(row for row in rows if row["quest_id"] == 4696 and row["dependency_type"] == "quest_receiver_location")
        self.assertEqual(17590, q4696_receiver_location["world_location_id"])
        self.assertEqual(
            "runtime_q4696_darby_receiver_location_spawn_and_receiver_cache_tested_pending_dialog_client_smoke",
            q4696_receiver_location["evidence_status"],
        )
        self.assertIn("WorldLocation2.tbl.sql", q4696_receiver_location["evidence_sources"])
        self.assertIn("GalerasMapScript.cs", q4696_receiver_location["evidence_sources"])
        self.assertIn("GalerasMapScriptTests.cs", q4696_receiver_location["evidence_sources"])
        self.assertIn("GlobalQuestManagerPartialTableTests.cs", q4696_receiver_location["evidence_sources"])
        self.assertIn("QuestTests.cs", q4696_receiver_location["evidence_sources"])
        self.assertIn("Quest2 receiver WorldLocation2 position 6204.08,-888.474,-2809.91", q4696_receiver_location["blocker_summary"])
        self.assertIn("runtime seed entity 1000010020", q4696_receiver_location["blocker_summary"])
        self.assertIn("visible Darby instead of starter 17175", q4696_receiver_location["blocker_summary"])
        q4526_receiver_location = next(row for row in rows if row["quest_id"] == 4526 and row["dependency_type"] == "quest_receiver_location")
        self.assertEqual(11669, q4526_receiver_location["world_location_id"])
        self.assertEqual(
            "runtime_q4526_pristan_receiver_location_spawn_tested_pending_dialog_client_smoke",
            q4526_receiver_location["evidence_status"],
        )
        self.assertIn("WorldLocation2.tbl.sql", q4526_receiver_location["evidence_sources"])
        self.assertIn("NorthernWildsMapScript.cs", q4526_receiver_location["evidence_sources"])
        self.assertIn("NorthernWildsBranchInteractionTests.cs", q4526_receiver_location["evidence_sources"])
        self.assertIn("Quest2 receiver WorldLocation2 position 4357.81,-751.7,-5717", q4526_receiver_location["blocker_summary"])
        self.assertIn("source_coordinate_id 116070", q4526_receiver_location["blocker_summary"])
        self.assertIn("QuestIdGiven and QuestIdReceive 4526", q4526_receiver_location["blocker_summary"])
        q3486_receiver_location = next(row for row in rows if row["quest_id"] == 3486 and row["dependency_type"] == "quest_receiver_location")
        self.assertEqual(7778, q3486_receiver_location["world_location_id"])
        self.assertEqual(
            "runtime_q3486_master_control_panel_receiver_location_spawn_tested_pending_dialog_client_smoke",
            q3486_receiver_location["evidence_status"],
        )
        self.assertIn("Master Control Panel", q3486_receiver_location["blocker_summary"])
        self.assertIn("source_coordinate_id 2501", q3486_receiver_location["blocker_summary"])
        self.assertIn("Q3486/Q3671/Q3668 chain smoke", q3486_receiver_location["blocker_summary"])
        q3486_arrival_location = next(row for row in rows if row["quest_id"] == 3486 and row["objective_id"] == 4987 and row["dependency_type"] == "objective_indicator_location")
        self.assertEqual(7807, q3486_arrival_location["world_location_id"])
        self.assertEqual(
            "runtime_q3486_arrival_indicator_and_story_credit_tested_pending_client_smoke",
            q3486_arrival_location["evidence_status"],
        )
        self.assertIn("story panel 1575", q3486_arrival_location["blocker_summary"])
        q3486_loftite_location = next(row for row in rows if row["quest_id"] == 3486 and row["objective_id"] == 4485 and row["dependency_type"] == "objective_indicator_location")
        self.assertEqual(
            "runtime_q3486_loftite_indicator_crystal_guardian_spawn_and_credit_tested_pending_client_smoke",
            q3486_loftite_location["evidence_status"],
        )
        self.assertIn("TargetGroup 4985", q3486_loftite_location["blocker_summary"])
        self.assertIn("source_coordinate_ids 7798992", q3486_loftite_location["blocker_summary"])
        q3486_target_group = next(row for row in rows if row["quest_id"] == 3486 and row["objective_id"] == 4485 and row["dependency_type"] == "target_group")
        self.assertEqual(4985, q3486_target_group["target_group_id"])
        self.assertEqual(1, q3486_target_group["target_group_type"])
        q3486_frostbite_member = next(row for row in rows if row["quest_id"] == 3486 and row["dependency_type"] == "target_group_member" and row["target_group_data"] == 11924)
        self.assertEqual(
            "runtime_q3486_frostbite_target_group_member_script_credit_tested_pending_spawn_client_smoke",
            q3486_frostbite_member["evidence_status"],
        )
        self.assertIn("Frostbite", q3486_frostbite_member["blocker_summary"])
        self.assertIn("virtual item 206", q3486_frostbite_member["blocker_summary"])
        q3486_guardian_member = next(row for row in rows if row["quest_id"] == 3486 and row["dependency_type"] == "target_group_member" and row["target_group_data"] == 11925)
        self.assertEqual(
            "runtime_q3486_crystal_guardian_spawn_and_virtual_collect_credit_tested_pending_client_smoke",
            q3486_guardian_member["evidence_status"],
        )
        self.assertIn("NorthernWildsMapScript.cs", q3486_guardian_member["evidence_sources"])
        self.assertIn("Q3486EmpoweredTower.cs", q3486_guardian_member["evidence_sources"])
        self.assertIn("virtual item 206", q3486_guardian_member["blocker_summary"])
        self.assertIn("Frostbite 11924 spawn/drop smoke", q3486_guardian_member["blocker_summary"])
        q3741_indicator_location = next(row for row in rows if row["quest_id"] == 3741 and row["objective_id"] == 4813 and row["dependency_type"] == "objective_indicator_location" and row["world_location_id"] == 9181)
        self.assertEqual(
            "runtime_q3741_supply_crate_indicator_spawn_and_virtual_collect_credit_tested_pending_client_smoke",
            q3741_indicator_location["evidence_status"],
        )
        self.assertIn("Exile Supply Crate 12919", q3741_indicator_location["blocker_summary"])
        self.assertIn("26 current reviewed Exile Supply Crate 12919 placements", q3741_indicator_location["blocker_summary"])
        self.assertIn("source_coordinate_ids 1218, 1219, 1220", q3741_indicator_location["blocker_summary"])
        self.assertIn("2634068", q3741_indicator_location["blocker_summary"])
        q3741_target_group = next(row for row in rows if row["quest_id"] == 3741 and row["objective_id"] == 4813 and row["dependency_type"] == "target_group")
        self.assertEqual(4372, q3741_target_group["target_group_id"])
        self.assertEqual(1, q3741_target_group["target_group_type"])
        q3741_supply_crate_member = next(row for row in rows if row["quest_id"] == 3741 and row["objective_id"] == 4813 and row["dependency_type"] == "target_group_member" and row["target_group_data"] == 12919)
        self.assertEqual(
            "runtime_q3741_supply_crate_target_group_member_spawn_and_virtual_collect_credit_tested_pending_client_smoke",
            q3741_supply_crate_member["evidence_status"],
        )
        self.assertIn("Q3741ScatteredSupplies.cs", q3741_supply_crate_member["evidence_sources"])
        self.assertIn("26 current reviewed Exile Supply Crate 12919 placements", q3741_supply_crate_member["blocker_summary"])
        self.assertIn("source_coordinate_ids 1218, 1219, 1220", q3741_supply_crate_member["blocker_summary"])
        self.assertIn("2634068", q3741_supply_crate_member["blocker_summary"])
        self.assertIn("client Creature2 11066 for Land's Reach Durek", q3741_supply_crate_member["blocker_summary"])
        q3797_receiver_location = next(row for row in rows if row["quest_id"] == 3797 and row["dependency_type"] == "quest_receiver_location")
        self.assertEqual(7727, q3797_receiver_location["world_location_id"])
        self.assertEqual(
            "runtime_q3797_lands_reach_durek_receiver_location_spawn_and_giver_receiver_cache_tested_pending_dialog_client_smoke",
            q3797_receiver_location["evidence_status"],
        )
        self.assertIn("WorldLocation2.tbl.sql", q3797_receiver_location["evidence_sources"])
        self.assertIn("CommunicatorMessages.tbl.sql", q3797_receiver_location["evidence_sources"])
        self.assertIn("GlobalQuestManagerPartialTableTests.cs", q3797_receiver_location["evidence_sources"])
        self.assertIn("Quest2 receiver WorldLocation2 position 4339.04,-751.768,-5678.82", q3797_receiver_location["blocker_summary"])
        self.assertIn("Creature2 11066 carries Q3797", q3797_receiver_location["blocker_summary"])
        self.assertIn("original 435/11061 bridge as source-spawn context", q3797_receiver_location["blocker_summary"])
        self.assertIn("source_relation_id 166 and 1693", q3797_receiver_location["blocker_summary"])
        q3797_indicator_location = next(row for row in rows if row["quest_id"] == 3797 and row["objective_id"] == 4918 and row["dependency_type"] == "objective_indicator_location" and row["world_location_id"] == 9181)
        self.assertEqual(
            "runtime_q3797_rootbrute_yeti_indicator_spawn_and_nested_kill_credit_tested_pending_client_smoke",
            q3797_indicator_location["evidence_status"],
        )
        self.assertIn("TargetGroup 1177", q3797_indicator_location["blocker_summary"])
        self.assertIn("type-9/non-spawned branch review", q3797_indicator_location["blocker_summary"])
        q3797_target_group = next(row for row in rows if row["quest_id"] == 3797 and row["objective_id"] == 4918 and row["dependency_type"] == "target_group" and row["target_group_id"] == 1177)
        self.assertEqual("QuestObjective.data", q3797_target_group["dependency_source"])
        self.assertEqual(11, q3797_target_group["target_group_type"])
        q3797_nested_group = next(row for row in rows if row["quest_id"] == 3797 and row["objective_id"] == 4918 and row["dependency_type"] == "nested_target_group" and row["target_group_id"] == 1177 and row["target_group_data"] == 6406)
        self.assertEqual("client_nested_target_group_pending_member_smoke", q3797_nested_group["evidence_status"])
        q3797_missing_nested_group = next(row for row in rows if row["quest_id"] == 3797 and row["objective_id"] == 4918 and row["dependency_type"] == "nested_target_group" and row["target_group_id"] == 1177 and row["target_group_data"] == 7777)
        self.assertEqual("blocked_missing_client_nested_target_group_row_runtime_fail_closed", q3797_missing_nested_group["evidence_status"])
        self.assertIn("AssetManagerTargetGroupTests.cs", q3797_missing_nested_group["evidence_sources"])
        self.assertIn("fails closed", q3797_missing_nested_group["blocker_summary"])
        q3797_type9_member = next(row for row in rows if row["quest_id"] == 3797 and row["objective_id"] == 4918 and row["dependency_type"] == "target_group_member" and row["target_group_id"] == 1113 and row["target_group_data"] == 11963)
        self.assertEqual("client_target_group_member_pending_spawn_and_credit_smoke", q3797_type9_member["evidence_status"])
        q3797_snowstalker_member = next(row for row in rows if row["quest_id"] == 3797 and row["objective_id"] == 4918 and row["dependency_type"] == "target_group_member" and row["target_group_id"] == 6406 and row["target_group_data"] == 11945)
        self.assertEqual("runtime_q3797_rootbrute_yeti_target_group_member_kill_credit_tested_pending_client_smoke", q3797_snowstalker_member["evidence_status"])
        self.assertIn("Q3797-specific Snowstalker placement/density still needs review", q3797_snowstalker_member["blocker_summary"])
        q3797_rootbrute_member = next(row for row in rows if row["quest_id"] == 3797 and row["objective_id"] == 4918 and row["dependency_type"] == "target_group_member" and row["target_group_id"] == 6406 and row["target_group_data"] == 12212)
        self.assertEqual("runtime_q3797_rootbrute_yeti_target_group_member_spawn_and_kill_credit_tested_pending_client_smoke", q3797_rootbrute_member["evidence_status"])
        self.assertIn("source_coordinate_ids 8272460", q3797_rootbrute_member["blocker_summary"])
        self.assertIn("QuestTests.cs", q3797_rootbrute_member["evidence_sources"])
        q5610_receiver_location = next(row for row in rows if row["quest_id"] == 5610 and row["dependency_type"] == "quest_receiver_location")
        self.assertEqual(17803, q5610_receiver_location["world_location_id"])
        self.assertEqual(
            "runtime_script_receiver_location_spawn_tested_pending_dialog_client_smoke",
            q5610_receiver_location["evidence_status"],
        )
        self.assertIn("CrimsonIsleMapScript.cs", q5610_receiver_location["evidence_sources"])
        self.assertIn("CrimsonIsleBranchInteractionTests.cs", q5610_receiver_location["evidence_sources"])
        self.assertIn("QuestTests.cs", q5610_receiver_location["evidence_sources"])
        self.assertIn("Mondo Zax", q5610_receiver_location["blocker_summary"])
        self.assertIn("source_coordinate_id 15089", q5610_receiver_location["blocker_summary"])
        self.assertIn("alternate-receiver prerequisite routing", q5610_receiver_location["blocker_summary"])
        q5597_world_zone = next(row for row in rows if row["quest_id"] == 5597 and row["dependency_type"] == "quest_world_zone")
        self.assertEqual(622, q5597_world_zone["world_zone_id"])
        self.assertEqual("Crimson Isle", q5597_world_zone["world_zone_name"])
        self.assertEqual(
            "runtime_q5597_crimson_isle_world_zone_assets_tested_external_video_observed_pending_local_client_visibility_smoke",
            q5597_world_zone["evidence_status"],
        )
        self.assertIn("CrimsonIsleMapScript.cs", q5597_world_zone["evidence_sources"])
        self.assertIn("CrimsonIsleBranchInteractionTests.cs", q5597_world_zone["evidence_sources"])
        self.assertIn("world_zone_client_map.csv", q5597_world_zone["evidence_sources"])
        self.assertIn("BLOCKER_EVIDENCE_PLAN.md", q5597_world_zone["evidence_sources"])
        self.assertIn("Decomp/Analysis/VIDEO_EVIDENCE_MAP.md", q5597_world_zone["evidence_sources"])
        self.assertIn("I:/Videos/Wildstar Tutorial - Crimson Isle - Quests Part 1 (Dominion)", q5597_world_zone["evidence_sources"])
        self.assertIn("I:/Videos/Wildstar Tutorial - Crimson Isle - Quests Part 2 (Dominion)", q5597_world_zone["evidence_sources"])
        self.assertIn("Quest2.worldZoneId 622 (Crimson Isle)", q5597_world_zone["blocker_summary"])
        self.assertIn("Crimson Isle world 870 map surface", q5597_world_zone["blocker_summary"])
        self.assertIn("Mondo Zax 24187", q5597_world_zone["blocker_summary"])
        self.assertIn("QuestObjective 8256 (VirtualCollect 364)", q5597_world_zone["blocker_summary"])
        self.assertIn("QuestManager and chain coverage", q5597_world_zone["blocker_summary"])
        self.assertIn("External Crimson Isle quest video corroborates the Q5597 client flow", q5597_world_zone["blocker_summary"])
        self.assertIn("not local client visibility", q5597_world_zone["blocker_summary"])
        self.assertIn("local build 16042 evidence bundle", q5597_world_zone["blocker_summary"])
        self.assertIn("Q5596->Q5597->Q5604 smoke", q5597_world_zone["blocker_summary"])
        q5597_receiver_location = next(row for row in rows if row["quest_id"] == 5597 and row["dependency_type"] == "quest_receiver_location")
        self.assertEqual(17803, q5597_receiver_location["world_location_id"])
        self.assertEqual(870, q5597_receiver_location["world_id"])
        self.assertEqual(1227, q5597_receiver_location["world_zone_id"])
        self.assertEqual("Bloodstone Canyon", q5597_receiver_location["world_zone_name"])
        self.assertEqual(
            "runtime_q5597_mondo_receiver_spawn_accept_complete_tested_external_video_observed_pending_local_dialog_ui_smoke",
            q5597_receiver_location["evidence_status"],
        )
        self.assertIn("CrimsonIsleMapScript.cs", q5597_receiver_location["evidence_sources"])
        self.assertIn("QuestTests.cs", q5597_receiver_location["evidence_sources"])
        self.assertIn("CrimsonIsleQuestChainTests.cs", q5597_receiver_location["evidence_sources"])
        self.assertIn("I:/Videos/Wildstar Tutorial - Crimson Isle - Quests Part 1 (Dominion)", q5597_receiver_location["evidence_sources"])
        self.assertIn("Quest2.WorldLocation2IdReceiver", q5597_receiver_location["blocker_summary"])
        self.assertIn("WorldZone 1227 Bloodstone Canyon", q5597_receiver_location["blocker_summary"])
        self.assertIn("all phases 4294967295", q5597_receiver_location["blocker_summary"])
        self.assertIn("source_coordinate_id 15089", q5597_receiver_location["blocker_summary"])
        self.assertIn("accepts Q5597 only with visible Mondo", q5597_receiver_location["blocker_summary"])
        self.assertIn("completes achieved Q5597 only with visible Mondo", q5597_receiver_location["blocker_summary"])
        self.assertIn("Quest2Reward rows 3000 and 5236", q5597_receiver_location["blocker_summary"])
        self.assertIn("Bloodstone achievement 4134", q5597_receiver_location["blocker_summary"])
        self.assertIn("Q5596QuestScript grants Q5597", q5597_receiver_location["blocker_summary"])
        self.assertIn("Q5597QuestScript grants Q5604", q5597_receiver_location["blocker_summary"])
        self.assertIn("Mondo accept, completion, follow-up, and reward-presentation questions are externally observed", q5597_receiver_location["blocker_summary"])
        self.assertIn("receiver placement/subzone/phase visibility", q5597_receiver_location["blocker_summary"])
        self.assertIn("Q5596->Q5597->Q5604 smoke", q5597_receiver_location["blocker_summary"])
        q5597_indicator_17896 = next(row for row in rows if row["quest_id"] == 5597 and row["objective_id"] == 8256 and row["dependency_type"] == "objective_indicator_location" and row["world_location_id"] == 17896)
        self.assertEqual("worldLocationsIdIndicator00", q5597_indicator_17896["source_slot"])
        self.assertEqual(870, q5597_indicator_17896["world_id"])
        self.assertEqual(1218, q5597_indicator_17896["world_zone_id"])
        self.assertEqual("Bloodstone Canyon", q5597_indicator_17896["world_zone_name"])
        self.assertEqual(
            "runtime_q5597_chua_explosives_indicator17896_spawn_and_credit_tested_external_video_observed_pending_local_client_ui_smoke",
            q5597_indicator_17896["evidence_status"],
        )
        self.assertIn("CrimsonIsleMapScript.cs", q5597_indicator_17896["evidence_sources"])
        self.assertIn("CrimsonIsleBranchInteractionTests.cs", q5597_indicator_17896["evidence_sources"])
        self.assertIn("Source/NexusForever.Game/Entity", q5597_indicator_17896["evidence_sources"])
        self.assertIn("worldLocationsIdIndicator00", q5597_indicator_17896["blocker_summary"])
        self.assertIn("WorldLocation2 17896", q5597_indicator_17896["blocker_summary"])
        self.assertIn("WorldZone 1218 Bloodstone Canyon", q5597_indicator_17896["blocker_summary"])
        self.assertIn("VirtualCollect data 364 count 6", q5597_indicator_17896["blocker_summary"])
        self.assertIn("TargetGroup 4373", q5597_indicator_17896["blocker_summary"])
        self.assertIn("all 30 fallbacks", q5597_indicator_17896["blocker_summary"])
        self.assertIn("credit virtual item 364", q5597_indicator_17896["blocker_summary"])
        self.assertIn("avoid duplicate credit", q5597_indicator_17896["blocker_summary"])
        self.assertIn("external Chua Explosives Collect/objective UI corroboration", q5597_indicator_17896["blocker_summary"])
        self.assertIn("not local client map guidance for indicator 17896", q5597_indicator_17896["blocker_summary"])
        self.assertIn("Q5596->Q5597->Q5604 smoke", q5597_indicator_17896["blocker_summary"])
        q5597_indicator_17898 = next(row for row in rows if row["quest_id"] == 5597 and row["objective_id"] == 8256 and row["dependency_type"] == "objective_indicator_location" and row["world_location_id"] == 17898)
        self.assertEqual("worldLocationsIdIndicator01", q5597_indicator_17898["source_slot"])
        self.assertEqual(870, q5597_indicator_17898["world_id"])
        self.assertEqual(1325, q5597_indicator_17898["world_zone_id"])
        self.assertEqual("Scarhide Camp", q5597_indicator_17898["world_zone_name"])
        self.assertEqual(
            "runtime_q5597_chua_explosives_indicator17898_spawn_and_credit_tested_external_video_observed_pending_local_client_ui_smoke",
            q5597_indicator_17898["evidence_status"],
        )
        self.assertIn("CrimsonIsleMapScript.cs", q5597_indicator_17898["evidence_sources"])
        self.assertIn("CrimsonIsleBranchInteractionTests.cs", q5597_indicator_17898["evidence_sources"])
        self.assertIn("Source/NexusForever.Game/Entity", q5597_indicator_17898["evidence_sources"])
        self.assertIn("worldLocationsIdIndicator01", q5597_indicator_17898["blocker_summary"])
        self.assertIn("WorldLocation2 17898", q5597_indicator_17898["blocker_summary"])
        self.assertIn("WorldZone 1325 Scarhide Camp", q5597_indicator_17898["blocker_summary"])
        self.assertIn("position -7022.49,-995.437,-946.002", q5597_indicator_17898["blocker_summary"])
        self.assertIn("VirtualCollect data 364 count 6", q5597_indicator_17898["blocker_summary"])
        self.assertIn("TargetGroup 4373", q5597_indicator_17898["blocker_summary"])
        self.assertIn("all 30 fallbacks", q5597_indicator_17898["blocker_summary"])
        self.assertIn("credit virtual item 364", q5597_indicator_17898["blocker_summary"])
        self.assertIn("avoid duplicate credit", q5597_indicator_17898["blocker_summary"])
        self.assertIn("external Chua Explosives Collect/objective UI corroboration", q5597_indicator_17898["blocker_summary"])
        self.assertIn("not local client map guidance for indicator 17898", q5597_indicator_17898["blocker_summary"])
        self.assertIn("Scarhide Camp density/placement", q5597_indicator_17898["blocker_summary"])
        self.assertIn("Q5596->Q5597->Q5604 smoke", q5597_indicator_17898["blocker_summary"])
        q5597_indicator_17897 = next(row for row in rows if row["quest_id"] == 5597 and row["objective_id"] == 8256 and row["dependency_type"] == "objective_indicator_location" and row["world_location_id"] == 17897)
        self.assertEqual("worldLocationsIdIndicator02", q5597_indicator_17897["source_slot"])
        self.assertEqual(870, q5597_indicator_17897["world_id"])
        self.assertEqual(1325, q5597_indicator_17897["world_zone_id"])
        self.assertEqual("Scarhide Camp", q5597_indicator_17897["world_zone_name"])
        self.assertEqual(
            "runtime_q5597_chua_explosives_indicator17897_spawn_and_credit_tested_external_video_observed_pending_local_client_ui_smoke",
            q5597_indicator_17897["evidence_status"],
        )
        self.assertIn("CrimsonIsleMapScript.cs", q5597_indicator_17897["evidence_sources"])
        self.assertIn("CrimsonIsleBranchInteractionTests.cs", q5597_indicator_17897["evidence_sources"])
        self.assertIn("Source/NexusForever.Game/Entity", q5597_indicator_17897["evidence_sources"])
        self.assertIn("worldLocationsIdIndicator02", q5597_indicator_17897["blocker_summary"])
        self.assertIn("WorldLocation2 17897", q5597_indicator_17897["blocker_summary"])
        self.assertIn("WorldZone 1325 Scarhide Camp", q5597_indicator_17897["blocker_summary"])
        self.assertIn("position -7270.21,-992.368,-828.018", q5597_indicator_17897["blocker_summary"])
        self.assertIn("VirtualCollect data 364 count 6", q5597_indicator_17897["blocker_summary"])
        self.assertIn("TargetGroup 4373", q5597_indicator_17897["blocker_summary"])
        self.assertIn("all 30 fallbacks", q5597_indicator_17897["blocker_summary"])
        self.assertIn("credit virtual item 364", q5597_indicator_17897["blocker_summary"])
        self.assertIn("avoid duplicate credit", q5597_indicator_17897["blocker_summary"])
        self.assertIn("external Chua Explosives Collect/objective UI corroboration", q5597_indicator_17897["blocker_summary"])
        self.assertIn("not local client map guidance for indicator 17897", q5597_indicator_17897["blocker_summary"])
        self.assertIn("Scarhide Camp density/placement", q5597_indicator_17897["blocker_summary"])
        self.assertIn("Q5596->Q5597->Q5604 smoke", q5597_indicator_17897["blocker_summary"])
        q5597_indicator_17899 = next(row for row in rows if row["quest_id"] == 5597 and row["objective_id"] == 8256 and row["dependency_type"] == "objective_indicator_location" and row["world_location_id"] == 17899)
        self.assertEqual("worldLocationsIdIndicator03", q5597_indicator_17899["source_slot"])
        self.assertEqual(870, q5597_indicator_17899["world_id"])
        self.assertEqual(1325, q5597_indicator_17899["world_zone_id"])
        self.assertEqual("Scarhide Camp", q5597_indicator_17899["world_zone_name"])
        self.assertEqual(
            "runtime_q5597_chua_explosives_indicator17899_spawn_and_credit_tested_external_video_observed_pending_local_client_ui_smoke",
            q5597_indicator_17899["evidence_status"],
        )
        self.assertIn("CrimsonIsleMapScript.cs", q5597_indicator_17899["evidence_sources"])
        self.assertIn("CrimsonIsleBranchInteractionTests.cs", q5597_indicator_17899["evidence_sources"])
        self.assertIn("Source/NexusForever.Game/Entity", q5597_indicator_17899["evidence_sources"])
        self.assertIn("worldLocationsIdIndicator03", q5597_indicator_17899["blocker_summary"])
        self.assertIn("WorldLocation2 17899", q5597_indicator_17899["blocker_summary"])
        self.assertIn("WorldZone 1325 Scarhide Camp", q5597_indicator_17899["blocker_summary"])
        self.assertIn("position -7144,-995.343,-1008.03", q5597_indicator_17899["blocker_summary"])
        self.assertIn("VirtualCollect data 364 count 6", q5597_indicator_17899["blocker_summary"])
        self.assertIn("TargetGroup 4373", q5597_indicator_17899["blocker_summary"])
        self.assertIn("all 30 fallbacks", q5597_indicator_17899["blocker_summary"])
        self.assertIn("credit virtual item 364", q5597_indicator_17899["blocker_summary"])
        self.assertIn("avoid duplicate credit", q5597_indicator_17899["blocker_summary"])
        self.assertIn("external Chua Explosives Collect/objective UI corroboration", q5597_indicator_17899["blocker_summary"])
        self.assertIn("not local client map guidance for indicator 17899", q5597_indicator_17899["blocker_summary"])
        self.assertIn("Scarhide Camp density/placement", q5597_indicator_17899["blocker_summary"])
        self.assertIn("Q5596->Q5597->Q5604 smoke", q5597_indicator_17899["blocker_summary"])
        q3670_receiver_location = next(row for row in rows if row["quest_id"] == 3670 and row["dependency_type"] == "quest_receiver_location")
        self.assertEqual(29526, q3670_receiver_location["world_location_id"])
        self.assertEqual(
            "runtime_script_receiver_location_spawn_and_receiver_cache_tested_pending_dialog_client_smoke",
            q3670_receiver_location["evidence_status"],
        )
        self.assertIn("NorthernWildsMapScript.cs", q3670_receiver_location["evidence_sources"])
        self.assertIn("NorthernWildsBranchInteractionTests.cs", q3670_receiver_location["evidence_sources"])
        self.assertIn("GlobalQuestManager.cs", q3670_receiver_location["evidence_sources"])
        self.assertIn("GlobalQuestManagerPartialTableTests.cs", q3670_receiver_location["evidence_sources"])
        self.assertIn("QuestTests.cs", q3670_receiver_location["evidence_sources"])
        self.assertIn("Deadeye Brightland", q3670_receiver_location["blocker_summary"])
        self.assertIn("source_coordinate_id 5142", q3670_receiver_location["blocker_summary"])
        self.assertIn("receiver override for Creature2 11063", q3670_receiver_location["blocker_summary"])
        q3670_alt_receiver_location = next(row for row in rows if row["quest_id"] == 3670 and row["dependency_type"] == "quest_alt_receiver_location")
        self.assertEqual(12354, q3670_alt_receiver_location["world_location_id"])
        self.assertEqual(29356, q3670_alt_receiver_location["prerequisite_id"])
        self.assertEqual(
            "runtime_q3670_alt_receiver_location_spawn_and_receiver_cache_tested_pending_dialog_client_smoke",
            q3670_alt_receiver_location["evidence_status"],
        )
        self.assertIn("Prerequisite.tbl.sql", q3670_alt_receiver_location["evidence_sources"])
        self.assertIn("GalerasMapScript.cs", q3670_alt_receiver_location["evidence_sources"])
        self.assertIn("GalerasMapScriptTests.cs", q3670_alt_receiver_location["evidence_sources"])
        self.assertIn("GlobalQuestManagerPartialTableTests.cs", q3670_alt_receiver_location["evidence_sources"])
        self.assertIn("source_coordinate_id 837", q3670_alt_receiver_location["blocker_summary"])
        self.assertIn("Creature2 16622", q3670_alt_receiver_location["blocker_summary"])
        self.assertIn("zone 23 under parent 459 under zone 13", q3670_alt_receiver_location["blocker_summary"])
        self.assertIn("prerequisite-selection UI smoke", q3670_alt_receiver_location["blocker_summary"])
        q3963_receiver_location = next(row for row in rows if row["quest_id"] == 3963 and row["dependency_type"] == "quest_receiver_location")
        self.assertEqual(12354, q3963_receiver_location["world_location_id"])
        self.assertEqual(
            "runtime_q3963_deadeye_receiver_location_spawn_and_receiver_cache_tested_pending_dialog_client_smoke",
            q3963_receiver_location["evidence_status"],
        )
        self.assertIn("GalerasMapScript.cs", q3963_receiver_location["evidence_sources"])
        self.assertIn("GalerasMapScriptTests.cs", q3963_receiver_location["evidence_sources"])
        self.assertIn("GlobalQuestManagerPartialTableTests.cs", q3963_receiver_location["evidence_sources"])
        self.assertIn("QuestTests.cs", q3963_receiver_location["evidence_sources"])
        self.assertIn("Creature2 16622 carries QuestIdReceive 3963", q3963_receiver_location["blocker_summary"])
        self.assertIn("source_coordinate_id 837", q3963_receiver_location["blocker_summary"])
        self.assertIn("source_coordinate_id 2587271", q3963_receiver_location["blocker_summary"])
        self.assertIn("standard client-table indexing caches 16622", q3963_receiver_location["blocker_summary"])
        self.assertIn("Q3487-to-Q3963 client flow smoke", q3963_receiver_location["blocker_summary"])
        q3671_receiver_location = next(row for row in rows if row["quest_id"] == 3671 and row["dependency_type"] == "quest_receiver_location")
        self.assertEqual(9340, q3671_receiver_location["world_location_id"])
        self.assertEqual(
            "runtime_q3671_deadeye_receiver_location_spawn_and_quest_lifecycle_tested_pending_dialog_client_smoke",
            q3671_receiver_location["evidence_status"],
        )
        self.assertIn("NorthernWildsMapScript.cs", q3671_receiver_location["evidence_sources"])
        self.assertIn("Q3671.cs", q3671_receiver_location["evidence_sources"])
        self.assertIn("QuestTests.cs", q3671_receiver_location["evidence_sources"])
        self.assertIn("Quest2 receiver WorldLocation2 position 4341.27,-751.765,-5682.93", q3671_receiver_location["blocker_summary"])
        self.assertIn("source_coordinate_id 4368", q3671_receiver_location["blocker_summary"])
        self.assertIn("QuestIdGiven and QuestIdReceive 3671", q3671_receiver_location["blocker_summary"])
        self.assertIn("reward row 6662 granting item 19131", q3671_receiver_location["blocker_summary"])
        q3668_receiver_location = next(row for row in rows if row["quest_id"] == 3668 and row["dependency_type"] == "quest_receiver_location")
        self.assertEqual(9121, q3668_receiver_location["world_location_id"])
        self.assertEqual(
            "runtime_q3668_bartol_receiver_location_spawn_tested_pending_dialog_client_smoke",
            q3668_receiver_location["evidence_status"],
        )
        self.assertIn("NorthernWildsMapScript.cs", q3668_receiver_location["evidence_sources"])
        self.assertIn("WorldLocation2.tbl.sql", q3668_receiver_location["evidence_sources"])
        self.assertIn("Quest2 receiver WorldLocation2 position 4355.91,-750.544,-5681.91", q3668_receiver_location["blocker_summary"])
        self.assertIn("source_coordinate_id 159", q3668_receiver_location["blocker_summary"])
        self.assertIn("QuestIdReceive 3668", q3668_receiver_location["blocker_summary"])
        q3668_lusk_location = next(row for row in rows if row["quest_id"] == 3668 and row["objective_id"] == 4744 and row["dependency_type"] == "objective_indicator_location")
        self.assertEqual(8857, q3668_lusk_location["world_location_id"])
        self.assertEqual(
            "runtime_q3668_lusk_talk_indicator_spawn_and_generic_talk_credit_tested_pending_client_smoke",
            q3668_lusk_location["evidence_status"],
        )
        self.assertIn("Scientist Lusk", q3668_lusk_location["blocker_summary"])
        self.assertIn("source_coordinate_id 4272", q3668_lusk_location["blocker_summary"])
        q3668_survivor_location = next(row for row in rows if row["quest_id"] == 3668 and row["objective_id"] == 4745 and row["dependency_type"] == "objective_indicator_location")
        self.assertEqual(8858, q3668_survivor_location["world_location_id"])
        self.assertEqual(
            "runtime_q3668_imprisoned_survivor_indicator_spawn_and_activate_credit_tested_pending_client_smoke",
            q3668_survivor_location["evidence_status"],
        )
        self.assertIn("TargetGroup 7261", q3668_survivor_location["blocker_summary"])
        self.assertIn("source_coordinate_ids 2622", q3668_survivor_location["blocker_summary"])
        q3668_survivor_member = next(row for row in rows if row["quest_id"] == 3668 and row["objective_id"] == 4745 and row["dependency_type"] == "target_group_member" and row["target_group_data"] == 14124)
        self.assertEqual(
            "runtime_q3668_imprisoned_survivor_target_group_member_spawn_and_activate_credit_tested_pending_client_smoke",
            q3668_survivor_member["evidence_status"],
        )
        self.assertIn("InteractionObjectiveUpdaterTests.cs", q3668_survivor_member["evidence_sources"])
        self.assertIn("TargetGroup 7261", q3668_survivor_member["blocker_summary"])
        q3668_skeech_location = next(row for row in rows if row["quest_id"] == 3668 and row["objective_id"] == 4791 and row["dependency_type"] == "objective_indicator_location" and row["world_location_id"] == 8859)
        self.assertEqual(
            "runtime_q3668_skeech_indicator_spawn_and_nested_kill_credit_tested_pending_client_smoke",
            q3668_skeech_location["evidence_status"],
        )
        self.assertIn("TargetGroup 7293", q3668_skeech_location["blocker_summary"])
        self.assertIn("14054 and 11913", q3668_skeech_location["blocker_summary"])
        q3668_nested_group = next(row for row in rows if row["quest_id"] == 3668 and row["objective_id"] == 4791 and row["dependency_type"] == "nested_target_group" and row["target_group_id"] == 7293 and row["target_group_data"] == 960)
        self.assertEqual("client_nested_target_group_pending_member_smoke", q3668_nested_group["evidence_status"])
        q3668_spawned_skeech_member = next(row for row in rows if row["quest_id"] == 3668 and row["objective_id"] == 4791 and row["dependency_type"] == "target_group_member" and row["target_group_id"] == 960 and row["target_group_data"] == 11907)
        self.assertEqual(
            "runtime_q3668_skeech_target_group_member_spawn_and_kill_credit_tested_pending_client_smoke",
            q3668_spawned_skeech_member["evidence_status"],
        )
        self.assertIn("source_coordinate_id 7766435", q3668_spawned_skeech_member["blocker_summary"])
        q3668_spawned_named_member = next(row for row in rows if row["quest_id"] == 3668 and row["objective_id"] == 4791 and row["dependency_type"] == "target_group_member" and row["target_group_id"] == 7292 and row["target_group_data"] == 36429)
        self.assertEqual(
            "runtime_q3668_skeech_target_group_member_spawn_and_kill_credit_tested_pending_client_smoke",
            q3668_spawned_named_member["evidence_status"],
        )
        self.assertIn("source_coordinate_id 6216334", q3668_spawned_named_member["blocker_summary"])
        q3668_missing_named_member = next(row for row in rows if row["quest_id"] == 3668 and row["objective_id"] == 4791 and row["dependency_type"] == "target_group_member" and row["target_group_id"] == 7292 and row["target_group_data"] == 14054)
        self.assertEqual("client_target_group_member_pending_spawn_and_credit_smoke", q3668_missing_named_member["evidence_status"])
        self.assertIn("no DataMapping spawn row", q3668_missing_named_member["blocker_summary"])
        self.assertIn("fallback spawning is blocked", q3668_missing_named_member["blocker_summary"])
        q3668_missing_common_member = next(row for row in rows if row["quest_id"] == 3668 and row["objective_id"] == 4791 and row["dependency_type"] == "target_group_member" and row["target_group_id"] == 960 and row["target_group_data"] == 11913)
        self.assertEqual("client_target_group_member_pending_spawn_and_credit_smoke", q3668_missing_common_member["evidence_status"])
        self.assertIn("[DEPR] Skeech Diviner", q3668_missing_common_member["blocker_summary"])
        self.assertIn("fallback spawning is blocked", q3668_missing_common_member["blocker_summary"])
        q3673_receiver_location = next(row for row in rows if row["quest_id"] == 3673 and row["dependency_type"] == "quest_receiver_location")
        self.assertEqual(29526, q3673_receiver_location["world_location_id"])
        self.assertEqual(
            "runtime_script_receiver_location_spawn_tested_pending_dialog_client_smoke",
            q3673_receiver_location["evidence_status"],
        )
        self.assertIn("NorthernWildsMapScript.cs", q3673_receiver_location["evidence_sources"])
        self.assertIn("NorthernWildsQuestChainTests.cs", q3673_receiver_location["evidence_sources"])
        self.assertIn("QuestIdReceive 3673", q3673_receiver_location["blocker_summary"])
        self.assertIn("prerequisite flow from Q3886", q3673_receiver_location["blocker_summary"])
        q3673_flare_location = next(row for row in rows if row["quest_id"] == 3673 and row["dependency_type"] == "objective_indicator_location" and row["world_location_id"] == 8868)
        self.assertEqual(
            "runtime_q3673_signal_flare_spawn_location_and_csi_credit_tested_pending_client_smoke",
            q3673_flare_location["evidence_status"],
        )
        self.assertIn("Creature2.tbl.sql", q3673_flare_location["evidence_sources"])
        self.assertIn("InteractionObjectiveUpdaterTests.cs", q3673_flare_location["evidence_sources"])
        self.assertIn("Creature2 13150", q3673_flare_location["blocker_summary"])
        self.assertIn("checklist index 1", q3673_flare_location["blocker_summary"])
        q3673_target_group = next(row for row in rows if row["quest_id"] == 3673 and row["dependency_type"] == "target_group")
        self.assertEqual("QuestObjective.data", q3673_target_group["dependency_source"])
        self.assertEqual(1106, q3673_target_group["target_group_id"])
        q3673_member = next(row for row in rows if row["quest_id"] == 3673 and row["dependency_type"] == "target_group_member" and row["target_group_data"] == 13151)
        self.assertEqual(
            "runtime_script_spawn_present_with_activate_target_group_checklist_credit_test_pending_client_smoke",
            q3673_member["evidence_status"],
        )
        self.assertIn("NorthernWildsMapScript.cs", q3673_member["evidence_sources"])
        self.assertIn("InteractionObjectiveUpdaterTests.cs", q3673_member["evidence_sources"])
        self.assertIn("Signal Flare 3", q3673_member["blocker_summary"])
        self.assertIn("checklist index 2", q3673_member["blocker_summary"])
        self.assertTrue(all(row["completion_status"] == "not_retail_complete" for row in rows))

    def test_quest_achievement_rows_cover_direct_and_checklist_references(self) -> None:
        strings = {100: "Quest Title", 200: "Achievement Title", 201: "Achievement Desc", 202: "Progress", 203: "Indigenous Intelligence", 204: "Contact with Thayd", 205: "Fiery Distraction", 212: "Scattered Supplies", 213: "More Important Than Revenge", 214: "By Leaps and Bounds", 215: "From the Wreckage", 216: "Reporting for Duty"}
        quests = [
            {"ID": "10", "localizedTextIdTitle": "100"},
            {"ID": "3479", "localizedTextIdTitle": "215"},
            {"ID": "3480", "localizedTextIdTitle": "216"},
            {"ID": "3668", "localizedTextIdTitle": "203"},
            {"ID": "3673", "localizedTextIdTitle": "204"},
            {"ID": "3777", "localizedTextIdTitle": "214"},
            {"ID": "3741", "localizedTextIdTitle": "212"},
            {"ID": "3886", "localizedTextIdTitle": "205"},
            {"ID": "3963", "localizedTextIdTitle": "213"},
        ]
        quest_output = [
            {"quest_id": 10, "bucket": "generic_table_driven", "completion_status": "not_retail_complete"},
            {"quest_id": 3479, "bucket": "curated_full", "completion_status": "not_retail_complete"},
            {"quest_id": 3480, "bucket": "curated_full", "completion_status": "not_retail_complete"},
            {"quest_id": 3668, "bucket": "curated_full", "completion_status": "not_retail_complete"},
            {"quest_id": 3673, "bucket": "curated_full", "completion_status": "not_retail_complete"},
            {"quest_id": 3777, "bucket": "generic_with_script", "completion_status": "not_retail_complete"},
            {"quest_id": 3741, "bucket": "generic_with_script", "completion_status": "not_retail_complete"},
            {"quest_id": 3886, "bucket": "curated_full", "completion_status": "not_retail_complete"},
            {"quest_id": 3963, "bucket": "curated_full", "completion_status": "not_retail_complete"},
        ]
        achievements = [
            {
                "ID": "300",
                "achievementTypeId": "3",
                "achievementCategoryId": "20",
                "flags": "1",
                "worldZoneId": "30",
                "localizedTextIdTitle": "200",
                "localizedTextIdDesc": "201",
                "localizedTextIdProgress": "202",
                "objectId": "10",
                "objectIdAlt": "0",
                "value": "1",
                "characterTitleId": "40",
                "prerequisiteId": "50",
                "prerequisiteIdServer": "51",
                "prerequisiteIdObjective": "52",
                "prerequisiteIdObjectiveAlt": "53",
                "achievementIdParentTier": "299",
            },
            {
                "ID": "301",
                "achievementTypeId": "45",
                "achievementCategoryId": "21",
                "flags": "0",
                "worldZoneId": "30",
                "localizedTextIdTitle": "200",
                "localizedTextIdDesc": "201",
                "localizedTextIdProgress": "202",
                "objectId": "0",
                "objectIdAlt": "0",
                "value": "2",
                "characterTitleId": "0",
                "prerequisiteId": "0",
                "prerequisiteIdServer": "0",
                "prerequisiteIdObjective": "0",
                "prerequisiteIdObjectiveAlt": "0",
                "achievementIdParentTier": "0",
            },
            {
                "ID": "3491",
                "achievementTypeId": "6",
                "achievementCategoryId": "179",
                "flags": "0",
                "worldZoneId": "35",
                "localizedTextIdTitle": "200",
                "localizedTextIdDesc": "201",
                "localizedTextIdProgress": "202",
                "objectId": "0",
                "objectIdAlt": "0",
                "value": "3",
                "characterTitleId": "0",
                "prerequisiteId": "19",
                "prerequisiteIdServer": "19",
                "prerequisiteIdObjective": "0",
                "prerequisiteIdObjectiveAlt": "0",
                "achievementIdParentTier": "0",
            },
            {
                "ID": "3490",
                "achievementTypeId": "6",
                "achievementCategoryId": "179",
                "flags": "0",
                "worldZoneId": "35",
                "localizedTextIdTitle": "200",
                "localizedTextIdDesc": "201",
                "localizedTextIdProgress": "202",
                "objectId": "0",
                "objectIdAlt": "0",
                "value": "3",
                "characterTitleId": "0",
                "prerequisiteId": "19",
                "prerequisiteIdServer": "19",
                "prerequisiteIdObjective": "0",
                "prerequisiteIdObjectiveAlt": "0",
                "achievementIdParentTier": "0",
            },
            {
                "ID": "1432",
                "achievementTypeId": "6",
                "achievementCategoryId": "178",
                "flags": "0",
                "worldZoneId": "35",
                "localizedTextIdTitle": "200",
                "localizedTextIdDesc": "201",
                "localizedTextIdProgress": "202",
                "objectId": "0",
                "objectIdAlt": "0",
                "value": "1",
                "characterTitleId": "0",
                "prerequisiteId": "0",
                "prerequisiteIdServer": "0",
                "prerequisiteIdObjective": "0",
                "prerequisiteIdObjectiveAlt": "0",
                "achievementIdParentTier": "0",
            },
            {
                "ID": "1433",
                "achievementTypeId": "6",
                "achievementCategoryId": "178",
                "flags": "0",
                "worldZoneId": "35",
                "localizedTextIdTitle": "200",
                "localizedTextIdDesc": "201",
                "localizedTextIdProgress": "202",
                "objectId": "0",
                "objectIdAlt": "0",
                "value": "1",
                "characterTitleId": "0",
                "prerequisiteId": "0",
                "prerequisiteIdServer": "0",
                "prerequisiteIdObjective": "0",
                "prerequisiteIdObjectiveAlt": "0",
                "achievementIdParentTier": "0",
            },
            {
                "ID": "1434",
                "achievementTypeId": "6",
                "achievementCategoryId": "178",
                "flags": "0",
                "worldZoneId": "35",
                "localizedTextIdTitle": "200",
                "localizedTextIdDesc": "201",
                "localizedTextIdProgress": "202",
                "objectId": "0",
                "objectIdAlt": "0",
                "value": "1",
                "characterTitleId": "0",
                "prerequisiteId": "0",
                "prerequisiteIdServer": "0",
                "prerequisiteIdObjective": "0",
                "prerequisiteIdObjectiveAlt": "0",
                "achievementIdParentTier": "0",
            },
            {
                "ID": "5327",
                "achievementTypeId": "6",
                "achievementCategoryId": "179",
                "flags": "0",
                "worldZoneId": "35",
                "localizedTextIdTitle": "200",
                "localizedTextIdDesc": "201",
                "localizedTextIdProgress": "202",
                "objectId": "0",
                "objectIdAlt": "0",
                "value": "3",
                "characterTitleId": "0",
                "prerequisiteId": "29533",
                "prerequisiteIdServer": "0",
                "prerequisiteIdObjective": "0",
                "prerequisiteIdObjectiveAlt": "0",
                "achievementIdParentTier": "0",
            },
            {
                "ID": "3523",
                "achievementTypeId": "6",
                "achievementCategoryId": "182",
                "flags": "0",
                "worldZoneId": "35",
                "localizedTextIdTitle": "200",
                "localizedTextIdDesc": "201",
                "localizedTextIdProgress": "202",
                "objectId": "0",
                "objectIdAlt": "0",
                "value": "3",
                "characterTitleId": "0",
                "prerequisiteId": "19",
                "prerequisiteIdServer": "19",
                "prerequisiteIdObjective": "0",
                "prerequisiteIdObjectiveAlt": "0",
                "achievementIdParentTier": "0",
            },
        ]
        checklists = [
            {
                "ID": "400",
                "achievementId": "301",
                "bit": "2",
                "objectId": "10",
                "objectIdAlt": "0",
                "prerequisiteId": "60",
                "prerequisiteIdAlt": "61",
            },
            {
                "ID": "4260",
                "achievementId": "3490",
                "bit": "1",
                "objectId": "3668",
                "objectIdAlt": "0",
                "prerequisiteId": "0",
                "prerequisiteIdAlt": "0",
            },
            {
                "ID": "836",
                "achievementId": "1432",
                "bit": "0",
                "objectId": "3479",
                "objectIdAlt": "0",
                "prerequisiteId": "0",
                "prerequisiteIdAlt": "0",
            },
            {
                "ID": "839",
                "achievementId": "1433",
                "bit": "0",
                "objectId": "3479",
                "objectIdAlt": "0",
                "prerequisiteId": "0",
                "prerequisiteIdAlt": "0",
            },
            {
                "ID": "840",
                "achievementId": "1434",
                "bit": "0",
                "objectId": "3479",
                "objectIdAlt": "0",
                "prerequisiteId": "0",
                "prerequisiteIdAlt": "0",
            },
            {
                "ID": "6910",
                "achievementId": "5327",
                "bit": "2",
                "objectId": "3480",
                "objectIdAlt": "0",
                "prerequisiteId": "29533",
                "prerequisiteIdAlt": "0",
            },
            {
                "ID": "4262",
                "achievementId": "3490",
                "bit": "3",
                "objectId": "3673",
                "objectIdAlt": "0",
                "prerequisiteId": "0",
                "prerequisiteIdAlt": "0",
            },
            {
                "ID": "4420",
                "achievementId": "3523",
                "bit": "3",
                "objectId": "3777",
                "objectIdAlt": "0",
                "prerequisiteId": "0",
                "prerequisiteIdAlt": "0",
            },
            {
                "ID": "4261",
                "achievementId": "3490",
                "bit": "2",
                "objectId": "3741",
                "objectIdAlt": "0",
                "prerequisiteId": "0",
                "prerequisiteIdAlt": "0",
            },
            {
                "ID": "4263",
                "achievementId": "3490",
                "bit": "4",
                "objectId": "3886",
                "objectIdAlt": "0",
                "prerequisiteId": "0",
                "prerequisiteIdAlt": "0",
            },
            {
                "ID": "4270",
                "achievementId": "3491",
                "bit": "1",
                "objectId": "3963",
                "objectIdAlt": "0",
                "prerequisiteId": "0",
                "prerequisiteIdAlt": "0",
            }
        ]

        rows = audit.build_quest_achievement_rows(strings, quests, quest_output, achievements, checklists)

        self.assertEqual({"achievement", "achievement_checklist"}, {row["dependency_type"] for row in rows})
        self.assertIn("Achievement Title", {row["achievement_title"] for row in rows})
        self.assertIn(400, {row["checklist_id"] for row in rows if row["dependency_type"] == "achievement_checklist"})
        self.assertIn(2, {row["checklist_bit"] for row in rows if row["dependency_type"] == "achievement_checklist"})
        q10_achievement = next(row for row in rows if row["quest_id"] == 10 and row["dependency_type"] == "achievement")
        self.assertEqual("client_achievement_row_pending_progression_hook_smoke", q10_achievement["evidence_status"])
        self.assertIn("Achievement 300 (Achievement Title) type 3 category 20 flags 1 worldZone 30", q10_achievement["blocker_summary"])
        self.assertIn("progress text 'Progress'", q10_achievement["blocker_summary"])
        self.assertIn("Achievement.objectId=10", q10_achievement["blocker_summary"])
        self.assertIn("parent tier 299", q10_achievement["blocker_summary"])
        self.assertIn("character title 40", q10_achievement["blocker_summary"])
        self.assertIn("prerequisites 50;51;52;53", q10_achievement["blocker_summary"])
        self.assertIn("achievement persistence", q10_achievement["blocker_summary"])
        q10_checklist = next(row for row in rows if row["quest_id"] == 10 and row["dependency_type"] == "achievement_checklist")
        self.assertEqual("client_achievement_checklist_row_pending_progression_hook_smoke", q10_checklist["evidence_status"])
        self.assertIn("AchievementChecklist 400", q10_checklist["blocker_summary"])
        self.assertIn("Achievement 301 (Achievement Title)", q10_checklist["blocker_summary"])
        self.assertIn("bit 2", q10_checklist["blocker_summary"])
        self.assertIn("objectId 10", q10_checklist["blocker_summary"])
        self.assertIn("objectIdAlt 0", q10_checklist["blocker_summary"])
        self.assertIn("AchievementChecklist.objectId", q10_checklist["blocker_summary"])
        self.assertIn("checklist prerequisites 60;61", q10_checklist["blocker_summary"])
        self.assertIn("duplicate checklist behavior", q10_checklist["blocker_summary"])
        for checklist_id in (836, 839, 840):
            q3479_checklist = next(row for row in rows if row["quest_id"] == 3479 and row["checklist_id"] == checklist_id)
            self.assertEqual(
                "runtime_q3479_achievement_checklist_completion_hooks_tested_pending_client_ui_smoke",
                q3479_checklist["evidence_status"],
            )
            self.assertIn("AchievementChecklist.tbl.sql", q3479_checklist["evidence_sources"])
            self.assertIn("QuestTests.cs", q3479_checklist["evidence_sources"])
            self.assertIn("QuestCompleteChecklistCount", q3479_checklist["blocker_summary"])
            self.assertIn("visible Deadeye receiver 11063", q3479_checklist["blocker_summary"])
            self.assertIn("selected reward grant", q3479_checklist["blocker_summary"])
        q3480_checklist = next(row for row in rows if row["quest_id"] == 3480 and row["checklist_id"] == 6910)
        self.assertEqual(
            "runtime_q3480_achievement_checklist_completion_hooks_tested_pending_client_ui_smoke",
            q3480_checklist["evidence_status"],
        )
        self.assertIn("AchievementChecklist.tbl.sql", q3480_checklist["evidence_sources"])
        self.assertIn("QuestTests.cs", q3480_checklist["evidence_sources"])
        self.assertIn("QuestCompleteChecklistCount", q3480_checklist["blocker_summary"])
        self.assertIn("visible Deadeye receiver 11063", q3480_checklist["blocker_summary"])
        self.assertIn("prerequisite 29533", q3480_checklist["blocker_summary"])
        self.assertIn("selected reward grant", q3480_checklist["blocker_summary"])
        q3668_checklist = next(row for row in rows if row["quest_id"] == 3668 and row["checklist_id"] == 4260)
        self.assertEqual(
            "runtime_q3668_achievement_checklist_completion_hooks_tested_pending_client_ui_smoke",
            q3668_checklist["evidence_status"],
        )
        self.assertIn("AchievementChecklist.tbl.sql", q3668_checklist["evidence_sources"])
        self.assertIn("QuestTests.cs", q3668_checklist["evidence_sources"])
        self.assertIn("QuestCompleteChecklistCount", q3668_checklist["blocker_summary"])
        self.assertIn("visible Bartol Sunward receiver 12737", q3668_checklist["blocker_summary"])
        self.assertIn("selected reward grant", q3668_checklist["blocker_summary"])
        q3673_checklist = next(row for row in rows if row["quest_id"] == 3673 and row["checklist_id"] == 4262)
        self.assertEqual(
            "runtime_q3673_achievement_checklist_completion_hooks_tested_pending_client_ui_smoke",
            q3673_checklist["evidence_status"],
        )
        self.assertIn("AchievementChecklist.tbl.sql", q3673_checklist["evidence_sources"])
        self.assertIn("QuestTests.cs", q3673_checklist["evidence_sources"])
        self.assertIn("QuestCompleteChecklistCount", q3673_checklist["blocker_summary"])
        self.assertIn("visible Deadeye receiver 11063", q3673_checklist["blocker_summary"])
        q3777_checklist = next(row for row in rows if row["quest_id"] == 3777 and row["checklist_id"] == 4420)
        self.assertEqual(
            "runtime_q3777_achievement_checklist_completion_hooks_tested_pending_client_ui_smoke",
            q3777_checklist["evidence_status"],
        )
        self.assertIn("AchievementChecklist.tbl.sql", q3777_checklist["evidence_sources"])
        self.assertIn("QuestTests.cs", q3777_checklist["evidence_sources"])
        self.assertIn("QuestCompleteChecklistCount", q3777_checklist["blocker_summary"])
        self.assertIn("visible Denner Hazefall receiver 15759", q3777_checklist["blocker_summary"])
        self.assertIn("default reputation grants", q3777_checklist["blocker_summary"])
        q3741_checklist = next(row for row in rows if row["quest_id"] == 3741 and row["checklist_id"] == 4261)
        self.assertEqual(
            "runtime_q3741_achievement_checklist_completion_hooks_tested_pending_client_ui_smoke",
            q3741_checklist["evidence_status"],
        )
        self.assertIn("AchievementChecklist.tbl.sql", q3741_checklist["evidence_sources"])
        self.assertIn("QuestTests.cs", q3741_checklist["evidence_sources"])
        self.assertIn("QuestCompleteChecklistCount", q3741_checklist["blocker_summary"])
        self.assertIn("visible Land's Reach Commander Durek receiver 11066", q3741_checklist["blocker_summary"])
        self.assertIn("both fixed reward item rows", q3741_checklist["blocker_summary"])
        q3886_checklist = next(row for row in rows if row["quest_id"] == 3886 and row["checklist_id"] == 4263)
        self.assertEqual(
            "runtime_q3886_achievement_checklist_completion_hooks_tested_pending_client_ui_smoke",
            q3886_checklist["evidence_status"],
        )
        self.assertIn("AchievementChecklist.tbl.sql", q3886_checklist["evidence_sources"])
        self.assertIn("QuestTests.cs", q3886_checklist["evidence_sources"])
        self.assertIn("QuestCompleteChecklistCount", q3886_checklist["blocker_summary"])
        self.assertIn("visible Land's Reach Commander Durek receiver 11066", q3886_checklist["blocker_summary"])
        q3963_checklist = next(row for row in rows if row["quest_id"] == 3963 and row["checklist_id"] == 4270)
        self.assertEqual(
            "runtime_q3963_achievement_checklist_completion_hooks_tested_pending_client_ui_smoke",
            q3963_checklist["evidence_status"],
        )
        self.assertIn("AchievementChecklist.tbl.sql", q3963_checklist["evidence_sources"])
        self.assertIn("QuestTests.cs", q3963_checklist["evidence_sources"])
        self.assertIn("QuestCompleteChecklistCount", q3963_checklist["blocker_summary"])
        self.assertIn("visible Deadeye receiver 16622", q3963_checklist["blocker_summary"])
        self.assertTrue(all(row["completion_status"] == "not_retail_complete" for row in rows))

    def test_quest_script_rows_classify_current_quests_and_related_owner_hooks(self) -> None:
        script_root = Path(tempfile.mkdtemp(prefix="nf-content-audit-scripts-"))
        try:
            (script_root / "Quests").mkdir()
            (script_root / "Quests" / "ExampleQuestScript.cs").write_text(
                """
[ScriptFilterOwnerId(10u)]
public class ExampleQuestScript : FollowUpQuestScript<ExampleQuestScript>
{
    protected override ushort NextQuestId => 11;
}

[ScriptFilterOwnerId(11u)]
public class EmptyQuestScript : IQuestScript
{
    public void OnLoad(IQuest owner)
    {
    }
}

[ScriptFilterOwnerId(12u)]
public class EmptyCuratedQuestScript : IQuestScript
{
    public void OnLoad(IQuest owner)
    {
    }
}

[ScriptFilterOwnerId(4694u)]
public class Q4694HoldTheLineStormwingStrikerEntityScript : IUnitScript
{
}

[ScriptFilterOwnerId(3486u)]
public class Q3486EmpoweredTowerQuestScript : IQuestScript
{
    public void OnQuestStateChange()
    {
        ObjectiveUpdate();
    }
}

[ScriptFilterOwnerId(3797u)]
public class Q3797SecuringTheAreaQuestScript : IQuestScript
{
    public void OnLoad(IQuest owner)
    {
    }
}

[ScriptFilterOwnerId(3741u)]
public class Q3741ScatteredSuppliesQuestScript : IQuestScript
{
    public void OnLoad(IQuest owner)
    {
    }
}

[ScriptFilterOwnerId(3668u)]
public class Q3668QuestScript : IQuestScript
{
    public void OnLoad(IQuest owner)
    {
    }
}

[ScriptFilterOwnerId(3671u)]
public class Q3671QuestScript : IQuestScript
{
    public void OnLoad(IQuest owner)
    {
    }
}
""".lstrip(),
                encoding="utf-8",
            )
            (script_root / "Quests" / "ExampleMapScript.cs").write_text(
                """
[ScriptFilterOwnerId(900)]
public class ExampleMapScript
{
}
""".lstrip(),
                encoding="utf-8",
            )
            (script_root / "Spells").mkdir()
            (script_root / "Spells" / "ExampleSpellScript.cs").write_text(
                """
[ScriptFilterOwnerId(300u, 301u)]
public class ExampleSpellScript
{
}
""".lstrip(),
                encoding="utf-8",
            )
            strings = {100: "Quest Title", 101: "Stub Quest", 102: "Curated Quest", 103: "Hold the Line", 104: "Empowered Tower", 105: "Indigenous Intelligence", 106: "Securing the Area", 107: "Scattered Supplies", 108: "Setting Up Camp"}
            quests = [
                {"ID": "10", "localizedTextIdTitle": "100"},
                {"ID": "11", "localizedTextIdTitle": "101"},
                {"ID": "12", "localizedTextIdTitle": "102"},
                {"ID": "4694", "localizedTextIdTitle": "103"},
                {"ID": "3486", "localizedTextIdTitle": "104"},
                {"ID": "3741", "localizedTextIdTitle": "107"},
                {"ID": "3797", "localizedTextIdTitle": "106"},
                {"ID": "3668", "localizedTextIdTitle": "105"},
                {"ID": "3671", "localizedTextIdTitle": "108"},
            ]
            quest_output = [
                {"quest_id": 10, "bucket": "generic_with_script", "completion_status": "not_retail_complete"},
                {"quest_id": 11, "bucket": "curated_partial_stub_script", "completion_status": "not_retail_complete"},
                {"quest_id": 12, "bucket": "curated_full", "completion_status": "not_retail_complete"},
                {"quest_id": 4694, "bucket": "generic_with_script", "completion_status": "not_retail_complete"},
                {"quest_id": 3486, "bucket": "curated_full", "completion_status": "not_retail_complete"},
                {"quest_id": 3741, "bucket": "generic_with_script", "completion_status": "not_retail_complete"},
                {"quest_id": 3797, "bucket": "generic_with_script", "completion_status": "not_retail_complete"},
                {"quest_id": 3668, "bucket": "generic_with_script", "completion_status": "not_retail_complete"},
                {"quest_id": 3671, "bucket": "no_objectives", "completion_status": "not_retail_complete"},
            ]

            rows = audit.build_quest_script_rows(strings, quests, quest_output, script_root)

            self.assertEqual(12, len(rows))
            quest_row = next(row for row in rows if row["owner_id"] == 10)
            self.assertEqual("current_quest", quest_row["quest_link_status"])
            self.assertEqual("quest_script", quest_row["script_kind"])
            self.assertEqual("runtime_generic_quest_script_hook_pending_world_smoke", quest_row["evidence_status"])
            self.assertEqual("Quest Title", quest_row["quest_title"])
            stub_row = next(row for row in rows if row["owner_id"] == 11)
            self.assertEqual("runtime_stub_quest_script_blocked", stub_row["evidence_status"])
            self.assertIn("no implementation markers", stub_row["blocker_summary"])
            curated_row = next(row for row in rows if row["owner_id"] == 12)
            self.assertEqual("runtime_curated_quest_script_pending_manual_smoke", curated_row["evidence_status"])
            self.assertNotIn("no implementation markers", curated_row["blocker_summary"])
            q4694_row = next(row for row in rows if row["owner_id"] == 4694)
            self.assertEqual("current_quest", q4694_row["quest_link_status"])
            self.assertEqual("quest_related_runtime_script", q4694_row["script_kind"])
            self.assertEqual(
                "runtime_quest_related_holdout_credit_script_tested_pending_dialog_client_smoke",
                q4694_row["evidence_status"],
            )
            self.assertIn("Creature2 23953", q4694_row["blocker_summary"])
            self.assertIn("93 Creature2 23953 rows", q4694_row["blocker_summary"])
            self.assertIn("23 near Trooper Vog", q4694_row["blocker_summary"])
            self.assertIn("Trooper Vog direct activation credit", q4694_row["blocker_summary"])
            q3486_row = next(row for row in rows if row["owner_id"] == 3486)
            self.assertEqual("current_quest", q3486_row["quest_link_status"])
            self.assertEqual("quest_script", q3486_row["script_kind"])
            self.assertEqual(
                "runtime_q3486_arrival_loftite_guardian_and_followup_script_tested_pending_client_smoke",
                q3486_row["evidence_status"],
            )
            self.assertIn("Crystal Guardian virtual item 206 kill credit", q3486_row["blocker_summary"])
            self.assertIn("Q3486/Q3671/Q3668 chain smoke", q3486_row["blocker_summary"])
            q3741_row = next(row for row in rows if row["owner_id"] == 3741)
            self.assertEqual("current_quest", q3741_row["quest_link_status"])
            self.assertEqual("quest_script", q3741_row["script_kind"])
            self.assertEqual(
                "runtime_q3741_supply_crate_spawn_and_credit_script_tested_pending_client_smoke",
                q3741_row["evidence_status"],
            )
            self.assertIn("26 current reviewed Exile Supply Crate 12919 placements", q3741_row["blocker_summary"])
            self.assertIn("2634068", q3741_row["blocker_summary"])
            self.assertIn("Land's Reach Commander Durek Creature2 11066", q3741_row["blocker_summary"])
            self.assertIn("starter/receiver route", q3741_row["blocker_summary"])
            q3797_row = next(row for row in rows if row["owner_id"] == 3797)
            self.assertEqual("current_quest", q3797_row["quest_link_status"])
            self.assertEqual("quest_script", q3797_row["script_kind"])
            self.assertEqual(
                "runtime_q3797_durek_receiver_objective_spawn_and_generic_credit_paths_tested_pending_client_smoke",
                q3797_row["evidence_status"],
            )
            self.assertIn("Commander Durek", q3797_row["blocker_summary"])
            self.assertIn("eight reviewed rootbrute/yeti objective targets", q3797_row["blocker_summary"])
            self.assertIn("Q3486 prerequisite flow smoke", q3797_row["blocker_summary"])
            q3668_row = next(row for row in rows if row["owner_id"] == 3668)
            self.assertEqual("current_quest", q3668_row["quest_link_status"])
            self.assertEqual("quest_script", q3668_row["script_kind"])
            self.assertEqual(
                "runtime_q3668_receiver_objective_spawn_and_generic_credit_paths_tested_pending_client_smoke",
                q3668_row["evidence_status"],
            )
            self.assertIn("Bartol Sunward", q3668_row["blocker_summary"])
            self.assertIn("TargetGroup 7293", q3668_row["blocker_summary"])
            self.assertIn("generic TalkTo", q3668_row["blocker_summary"])
            self.assertIn("14054 and 11913", q3668_row["blocker_summary"])
            q3671_row = next(row for row in rows if row["owner_id"] == 3671)
            self.assertEqual("current_quest", q3671_row["quest_link_status"])
            self.assertEqual("quest_script", q3671_row["script_kind"])
            self.assertEqual(
                "implemented_no_objective_script_lifecycle_tests_pending_dialog_smoke",
                q3671_row["evidence_status"],
            )
            self.assertIn("QuestTests.cs", q3671_row["evidence_sources"])
            self.assertIn("row-tested", q3671_row["blocker_summary"])
            map_row = next(row for row in rows if row["owner_id"] == 900)
            self.assertEqual("non_quest_script_owner", map_row["quest_link_status"])
            self.assertEqual("map_script", map_row["script_kind"])
            self.assertEqual("runtime_map_script_owner_pending_zone_smoke", map_row["evidence_status"])
            spell_rows = [row for row in rows if row["script_kind"] == "spell_script"]
            self.assertEqual({300, 301}, {row["owner_id"] for row in spell_rows})
            self.assertEqual({"300;301"}, {row["owner_ids_on_attribute"] for row in spell_rows})
            self.assertTrue(all(row["completion_status"] == "not_retail_complete" for row in rows))
        finally:
            shutil.rmtree(script_root, ignore_errors=True)

    def test_quest_blocker_summary_names_stub_script_gap(self) -> None:
        generic_summary = audit.quest_blocker_summary(
            "generic_table_driven",
            quest_id=1805,
            world_zone_id=262,
            faction_enum=2,
            con_level=1,
            prereq_level=1,
            objective_ids="1890;6807",
            objective_types="15;32",
            objective_data="1890:15:4976:2;6807:32:1:3",
            reward_ids="700",
            reward_types="1",
            quest_virtual_loot_groups=2,
            achievement_ids="400",
        )
        self.assertIn("Quest2 1805", generic_summary)
        self.assertIn("WorldZone 262", generic_summary)
        self.assertIn("objectives 1890;6807", generic_summary)
        self.assertIn("objective data/type/count 1890:15:4976:2;6807:32:1:3", generic_summary)
        self.assertIn("Quest2Reward rows 700", generic_summary)
        self.assertIn("quest virtual loot groups 2", generic_summary)
        self.assertIn("achievement hooks 400", generic_summary)
        self.assertIn("not a retail-complete gameplay claim", generic_summary)
        no_objective_summary = audit.quest_blocker_summary(
            "no_objectives",
            quest_id=1177,
            quest_name="Home on the Range",
            world_zone_id=1480,
            quest_type=6,
            quest_subtype_id=71,
            con_level=6,
            prereq_level=3,
            prereq_flags=8,
        )
        self.assertIn("Quest2 1177 (Home on the Range)", no_objective_summary)
        self.assertIn("type 6", no_objective_summary)
        self.assertIn("subtype 71", no_objective_summary)
        self.assertIn("zero-objective row", no_objective_summary)
        self.assertIn(
            "no implementation markers",
            audit.quest_blocker_summary(
                "curated_partial_stub_script",
                quest_id=5604,
                objective_ids="1000",
                script_paths=r"Source\NexusForever.Script.Main\Quests\Q5604.cs",
            ),
        )
        self.assertIn(
            "not a retail-complete gameplay claim",
            audit.quest_blocker_summary("generic_table_driven"),
        )

    def test_quest_creature_rows_cover_current_unmatched_and_noncurrent_relations(self) -> None:
        csv_root = Path(tempfile.mkdtemp(prefix="nf-content-audit-creatures-"))
        try:
            creature_map = csv_root / "creature_quest_map.csv"
            creature_map.write_text(
                "\n".join(
                    [
                        "relation_type,source_relation_id,jabbithole_creature_id,creature2_id,source_name,creature2_name,match_status,jabbithole_quest_id,quest2_id,quest_name,quest_level,quest_min_level,quest_type,path_quest_type,last_seen_in",
                        "starter,1,10,20,Source NPC,Creature NPC,reviewed,1000,10,Quest Title,5,3,0,-1,7",
                        "objective,2,11,,Missing NPC,,unmatched,1001,10,Quest Title,5,3,0,-1,7",
                        "finisher,3,12,22,Old NPC,Old NPC,reviewed,1002,9999,Old Quest,50,50,5,-1,7",
                        "starter,4,13,23,Ambiguous NPC,Ambiguous NPC,ambiguous_name,1003,10,Quest Title,5,3,0,-1,7",
                        "starter,295,2124,11061,Commander Durek,Commander Durek,ambiguous_name,700,4696,Leaving the Temple of Osiric,20,16,0,-1,7",
                        "finisher,182,2141,17053,Corporal Darby,Corporal Darby,reviewed,700,4696,Leaving the Temple of Osiric,20,16,0,-1,7",
                        "objective,718,2199,50904,Trooper Vog,Trooper Vog,ambiguous_name,710,4694,Hold the Line,20,17,0,-1,7",
                        "finisher,422,3367,24187,Mondo Zax,Mondo Zax,reviewed,402,5610,Dreg Mutations,4,3,11,-1,7",
                        "starter,1919,3367,24187,Mondo Zax,Mondo Zax,reviewed,942,5573,Powering Down,3,1,0,-1,4",
                        "finisher,1558,3367,24187,Mondo Zax,Mondo Zax,reviewed,942,5573,Powering Down,3,1,0,-1,4",
                        "starter,1922,3367,24187,Mondo Zax,Mondo Zax,reviewed,945,5597,Dregs and Thieves,4,1,0,-1,4",
                        "finisher,1564,3367,24187,Mondo Zax,Mondo Zax,reviewed,945,5597,Dregs and Thieves,4,1,0,-1,4",
                        "objective,1090,3462,24286,Chua Explosives,Chua Explosives,reviewed,945,5597,Dregs and Thieves,4,1,0,-1,7",
                        "starter,2212,3385,24158,Kezrek Warbringer,Kezrek Warbringer,reviewed,901,5575,Seizing Power,4,1,11,-1,7",
                        "starter,1228,9708,12560,Scientist Pristan,Scientist Pristan,reviewed,348,4526,Spatial Anomaly,6,3,0,-1,7",
                        "finisher,1891,9708,12560,Scientist Pristan,Scientist Pristan,reviewed,348,4526,Spatial Anomaly,6,3,0,-1,7",
                        "starter,155,619,11194,Master Control Panel,Master Control Panel,reviewed,234,3486,Empowered Tower,3,1,0,-1,7",
                        "finisher,105,619,11194,Master Control Panel,Master Control Panel,reviewed,234,3486,Empowered Tower,3,1,0,-1,7",
                        "objective,139,133,11925,Crystal Guardian,Crystal Guardian,scored_name,234,3486,Empowered Tower,3,1,0,-1,7",
                        "objective,234,293,12919,Exile Supply Crate,Exile Supply Crate,reviewed,194,3741,Scattered Supplies,4,1,0,-1,7",
                        "starter,166,619,11061,Commander Durek,Commander Durek,ambiguous_name,278,3797,Securing the Area,4,1,11,-1,7",
                        "finisher,1693,619,11061,Commander Durek,Commander Durek,ambiguous_name,278,3797,Securing the Area,4,1,11,-1,7",
                        "objective,105,816,12212,Rootbrute Grimspore,Rootbrute Grimspore,ambiguous_name,278,3797,Securing the Area,4,1,11,-1,7",
                        "objective,5204,244,11945,Yeti Snowstalker,Yeti Snowstalker,ambiguous_name,278,3797,Securing the Area,4,1,11,-1,7",
                        "objective,106,995,11948,Yeti Frostclaw,Yeti Frostclaw,scored_name,278,3797,Securing the Area,4,1,11,-1,7",
                        "objective,3055,817,12213,Rootbrute Deathcap,Rootbrute Deathcap,scored_name,278,3797,Securing the Area,4,1,11,-1,7",
                        "starter,47,1073,11063,Deadeye Brightland,Deadeye Brightland,reviewed,67,3668,Indigenous Intelligence,5,1,0,-1,7",
                        "finisher,37,34,12737,Bartol Sunward,Bartol Sunward,ambiguous_name,67,3668,Indigenous Intelligence,5,1,0,-1,7",
                        "objective,40,674,14124,Imprisoned Survivor,Imprisoned Survivor,unique_name,67,3668,Indigenous Intelligence,5,1,0,-1,7",
                        "objective,43,1043,12484,Scientist Lusk,Scientist Lusk,ambiguous_name,67,3668,Indigenous Intelligence,5,1,0,-1,7",
                        "objective,3053,475,36429,Fierce Skeech Scratcher,Fierce Skeech Scratcher,scored_name,67,3668,Indigenous Intelligence,5,1,0,-1,7",
                        "objective,3054,215,17545,Skeech Dagunlord,Skeech Dagunlord,unique_name,67,3668,Indigenous Intelligence,5,1,0,-1,7",
                        "objective,6808,27648,11910,Skeech Scratcher,Skeech Scratcher,unique_name,67,3668,Indigenous Intelligence,5,1,0,-1,7",
                        "objective,6809,27646,11912,Skeech Shaman,Skeech Shaman,scored_name,67,3668,Indigenous Intelligence,5,1,0,-1,7",
                        "objective,6810,27650,11907,Skeech Diviner,Skeech Diviner,scored_name,67,3668,Indigenous Intelligence,5,1,0,-1,7",
                        "objective,6811,27689,14124,Imprisoned Survivor,Imprisoned Survivor,unique_name,67,3668,Indigenous Intelligence,5,1,0,-1,7",
                        "objective,6813,27645,11917,Skeech Frostlord,Skeech Frostlord,scored_name,67,3668,Indigenous Intelligence,5,1,0,-1,7",
                        "objective,6918,27684,36884,Coldburrow Xenobreeder,Coldburrow Xenobreeder,unique_name,67,3668,Indigenous Intelligence,5,1,0,-1,7",
                        "finisher,38,1073,11063,Deadeye Brightland,Deadeye Brightland,reviewed,69,3670,Calm Before the Storm,5,2,0,-1,7",
                        "finisher,100,1073,11063,Deadeye Brightland,Deadeye Brightland,reviewed,226,3671,Setting Up Camp,4,1,0,-1,7",
                        "starter,1229,1073,11063,Deadeye Brightland,Deadeye Brightland,reviewed,226,3671,Setting Up Camp,4,1,0,-1,7",
                        "finisher,1492,201,16622,Deadeye Brightland,Deadeye Brightland,reviewed,69,3670,Calm Before the Storm,5,2,0,-1,7",
                        "starter,150,752,11062,Bosun Redmark,Bosun Redmark,reviewed,227,3479,From the Wreckage,3,1,11,-1,7",
                        "finisher,101,1073,11063,Deadeye Brightland,Deadeye Brightland,reviewed,227,3479,From the Wreckage,3,1,11,-1,7",
                        "objective,217,868,11070,Trapped Survivor,Trapped Survivor,unique_name,227,3479,From the Wreckage,3,1,11,-1,7",
                        "objective,216,244,11945,Yeti Snowstalker,Yeti Snowstalker,ambiguous_name,227,3479,From the Wreckage,3,1,11,-1,7",
                        "objective,218,995,11948,Yeti Frostclaw,Yeti Frostclaw,scored_name,227,3479,From the Wreckage,3,1,11,-1,7",
                        "objective,3449,3834,36331,Fierce Yeti Icefang,Fierce Yeti Icefang,scored_name,227,3479,From the Wreckage,3,1,11,-1,7",
                        "starter,821,435,11061,Commander Durek,Commander Durek,ambiguous_name,1454,3480,Reporting for Duty,3,1,11,-1,7",
                        "starter,2133,23645,,Unknown,,unmatched,1454,3480,Reporting for Duty,3,1,11,-1,4",
                        "finisher,579,1073,11063,Deadeye Brightland,Deadeye Brightland,reviewed,1454,3480,Reporting for Duty,3,1,11,-1,7",
                        "objective,2131,868,11070,Trapped Survivor,Trapped Survivor,unique_name,1454,3480,Reporting for Duty,3,1,11,-1,7",
                        "objective,2129,244,11945,Yeti Snowstalker,Yeti Snowstalker,ambiguous_name,1454,3480,Reporting for Duty,3,1,11,-1,7",
                        "objective,2130,995,11948,Yeti Frostclaw,Yeti Frostclaw,scored_name,1454,3480,Reporting for Duty,3,1,11,-1,7",
                        "objective,3419,3834,36331,Fierce Yeti Icefang,Fierce Yeti Icefang,scored_name,1454,3480,Reporting for Duty,3,1,11,-1,7",
                        "starter,49,1073,11063,Deadeye Brightland,Deadeye Brightland,reviewed,70,3673,Contact with Thayd,5,1,11,-1,7",
                        "finisher,40,1073,11063,Deadeye Brightland,Deadeye Brightland,reviewed,70,3673,Contact with Thayd,5,1,11,-1,7",
                        "finisher,1842,23645,,Unknown,,unmatched,70,3673,Contact with Thayd,5,1,11,-1,7",
                        "objective,405,756,12521,Signal Flare,Signal Flare,ambiguous_name,70,3673,Contact with Thayd,5,1,11,-1,7",
                        "finisher,96,201,16622,Deadeye Brightland,Deadeye Brightland,reviewed,208,3963,More Important Than Revenge,5,2,0,-1,7",
                        "starter,1621,1073,11063,Deadeye Brightland,Deadeye Brightland,reviewed,208,3963,More Important Than Revenge,5,2,0,-1,4",
                        "starter,2130,23645,,Unknown,,unmatched,208,3963,More Important Than Revenge,5,2,0,-1,7",
                    ]
                )
                + "\n",
                encoding="utf-8",
            )
            strings = {100: "Quest Title", 200: "Leaving the Temple of Osiric", 201: "Hold the Line", 202: "Dreg Mutations", 203: "Calm Before the Storm", 204: "Contact with Thayd", 205: "Spatial Anomaly", 206: "Setting Up Camp", 207: "From the Wreckage", 208: "Reporting for Duty", 209: "Empowered Tower", 210: "Indigenous Intelligence", 211: "Securing the Area", 212: "Scattered Supplies", 213: "More Important Than Revenge", 214: "Dregs and Thieves", 215: "Seizing Power", 216: "Powering Down"}
            quests = [
                {"ID": "10", "localizedTextIdTitle": "100"},
                {"ID": "3673", "localizedTextIdTitle": "204"},
                {"ID": "4696", "localizedTextIdTitle": "200"},
                {"ID": "4694", "localizedTextIdTitle": "201"},
                {"ID": "4526", "localizedTextIdTitle": "205"},
                {"ID": "3486", "localizedTextIdTitle": "209"},
                {"ID": "3741", "localizedTextIdTitle": "212"},
                {"ID": "3797", "localizedTextIdTitle": "211"},
                {"ID": "3668", "localizedTextIdTitle": "210"},
                {"ID": "5610", "localizedTextIdTitle": "202"},
                {"ID": "5597", "localizedTextIdTitle": "214"},
                {"ID": "3670", "localizedTextIdTitle": "203"},
                {"ID": "3671", "localizedTextIdTitle": "206"},
                {"ID": "3479", "localizedTextIdTitle": "207"},
                {"ID": "3480", "localizedTextIdTitle": "208"},
                {"ID": "3963", "localizedTextIdTitle": "213"},
                {"ID": "5573", "localizedTextIdTitle": "216"},
                {"ID": "5575", "localizedTextIdTitle": "215"},
            ]
            quest_output = [
                {"quest_id": 10, "bucket": "generic_table_driven", "completion_status": "not_retail_complete"},
                {"quest_id": 3673, "bucket": "curated_full", "completion_status": "not_retail_complete"},
                {"quest_id": 4696, "bucket": "curated_partial_stub_script", "completion_status": "not_retail_complete"},
                {"quest_id": 4694, "bucket": "generic_with_script", "completion_status": "not_retail_complete"},
                {"quest_id": 4526, "bucket": "generic_with_script", "completion_status": "not_retail_complete"},
                {"quest_id": 3486, "bucket": "curated_full", "completion_status": "not_retail_complete"},
                {"quest_id": 3741, "bucket": "generic_with_script", "completion_status": "not_retail_complete"},
                {"quest_id": 3797, "bucket": "generic_with_script", "completion_status": "not_retail_complete"},
                {"quest_id": 3668, "bucket": "generic_with_script", "completion_status": "not_retail_complete"},
                {"quest_id": 5610, "bucket": "no_objectives", "completion_status": "not_retail_complete"},
                {"quest_id": 5597, "bucket": "curated_full", "completion_status": "not_retail_complete"},
                {"quest_id": 5573, "bucket": "curated_full", "completion_status": "not_retail_complete"},
                {"quest_id": 3670, "bucket": "no_objectives", "completion_status": "not_retail_complete"},
                {"quest_id": 3671, "bucket": "no_objectives", "completion_status": "not_retail_complete"},
                {"quest_id": 3479, "bucket": "generic_with_script", "completion_status": "not_retail_complete"},
                {"quest_id": 3480, "bucket": "generic_with_script", "completion_status": "not_retail_complete"},
                {"quest_id": 3963, "bucket": "curated_full", "completion_status": "not_retail_complete"},
                {"quest_id": 5575, "bucket": "curated_full", "completion_status": "not_retail_complete"},
            ]
            q5597_mondo_zax_spawns = [
                {
                    "source_coordinate_id": str(source_coordinate_id),
                    "jabbithole_creature_id": "3367",
                    "creature2_id": "24187",
                    "source_name": "Mondo Zax",
                    "creature2_name": "Mondo Zax",
                    "match_status": "reviewed",
                    "entity_type": "0",
                    "zone_id": "9",
                    "worldid": "870",
                    "worldzoneid": "1885",
                    "x": str(x),
                    "y": str(y),
                    "z": str(z),
                }
                for source_coordinate_id, x, y, z in [
                    (15087, -7582, -953, -1292),
                    (15088, -8229, -993, -230),
                    (15089, -7664, -942, -672),
                    (21174, -7488, -978, -828),
                    (136782, -7488, -978, -829),
                    (1863809, -7489, -978, -828),
                    (2480207, -7489, -978, -829),
                    (2561752, -7489, -978, -827),
                ]
            ]
            q5597_chua_explosive_spawns = [
                {
                    "source_coordinate_id": str(source_coordinate_id),
                    "jabbithole_creature_id": "3462",
                    "creature2_id": "24286",
                    "source_name": "Chua Explosives",
                    "creature2_name": "Chua Explosives",
                    "match_status": "reviewed",
                    "entity_type": "8",
                    "zone_id": "9",
                    "worldid": "870",
                    "worldzoneid": "1218",
                    "x": str(x),
                    "y": str(y),
                    "z": str(z),
                }
                for source_coordinate_id, x, y, z in [
                    (15489, -7140, -995, -979),
                    (15490, -7074, -994, -953),
                    (15491, -7084, -995, -863),
                    (15492, -7182, -993, -855),
                    (15493, -7077, -995, -885),
                    (15494, -7147, -993, -948),
                    (15857, -7298, -990, -830),
                    (15858, -7358, -992, -883),
                    (15859, -7290, -991, -860),
                    (15860, -7295, -991, -873),
                    (15861, -7225, -991, -807),
                    (15862, -7247, -991, -791),
                    (38874, -7276, -991, -787),
                    (51929, -7267, -990, -785),
                    (59776, -7110, -995, -860),
                    (59777, -7073, -993, -1033),
                    (59778, -7123, -995, -1030),
                    (59779, -7100, -994, -1043),
                    (59780, -7104, -995, -1012),
                    (59781, -7115, -995, -1022),
                    (64334, -7102, -995, -991),
                    (101493, -7129, -994, -1040),
                    (101494, -7143, -995, -1066),
                    (127169, -7031, -993, -1029),
                    (134970, -7037, -994, -980),
                    (134971, -6994, -992, -1030),
                    (134972, -6991, -992, -1061),
                    (136408, -7086, -995, -1018),
                    (136864, -7136, -995, -1013),
                    (147754, -7003, -993, -1003),
                ]
            ]
            q5575_kezrek_spawns = [
                {
                    "source_coordinate_id": str(source_coordinate_id),
                    "jabbithole_creature_id": "3385",
                    "creature2_id": "24158",
                    "source_name": "Kezrek Warbringer",
                    "creature2_name": "Kezrek Warbringer",
                    "match_status": "reviewed",
                    "entity_type": "0",
                    "zone_id": "9",
                    "worldid": "870",
                    "worldzoneid": "1284",
                    "x": str(x),
                    "y": str(y),
                    "z": str(z),
                }
                for source_coordinate_id, x, y, z in [
                    (15181, -7588, -953, -1300),
                    (170390, -7666, -942, -678),
                ]
            ]

            rows = audit.build_quest_creature_rows(
                strings,
                quests,
                quest_output,
                creature_map,
                runtime_entity_rows=[
                    {"id": "100", "type": "0", "creature": "20", "world": "1", "area": "2", "x": "10", "y": "20", "z": "30"},
                    {"id": "101", "type": "0", "creature": "23", "world": "1", "area": "3"},
                    {"id": "102", "type": "0", "creature": "17053", "world": "51", "area": "973", "x": "6203", "y": "-888", "z": "-2809"},
                    {"id": "1002587271", "type": "0", "creature": "16622", "world": "51", "area": "23", "x": "3026", "y": "-1001", "z": "-4310"},
                ],
                creature_spawn_rows=q5597_mondo_zax_spawns + q5575_kezrek_spawns + [
                    {
                        "source_coordinate_id": "501",
                        "jabbithole_creature_id": "10",
                        "creature2_id": "20",
                        "source_name": "Source NPC",
                        "creature2_name": "Creature NPC",
                        "match_status": "reviewed",
                        "entity_type": "0",
                        "zone_id": "1",
                        "worldid": "1",
                        "worldzoneid": "2",
                        "x": "10",
                        "y": "20",
                        "z": "30",
                    },
                    {
                        "source_coordinate_id": "10020",
                        "jabbithole_creature_id": "2141",
                        "creature2_id": "17053",
                        "source_name": "Corporal Darby",
                        "creature2_name": "Corporal Darby",
                        "match_status": "reviewed",
                        "entity_type": "0",
                        "zone_id": "12",
                        "worldid": "51",
                        "worldzoneid": "973",
                        "x": "6203",
                        "y": "-888",
                        "z": "-2809",
                    },
                    {
                        "source_coordinate_id": "99084",
                        "jabbithole_creature_id": "2141",
                        "creature2_id": "17053",
                        "source_name": "Corporal Darby",
                        "creature2_name": "Corporal Darby",
                        "match_status": "reviewed",
                        "entity_type": "0",
                        "zone_id": "12",
                        "worldid": "51",
                        "worldzoneid": "973",
                        "x": "6176",
                        "y": "-880",
                        "z": "-2910",
                    },
                    {
                        "source_coordinate_id": "10200",
                        "jabbithole_creature_id": "2199",
                        "creature2_id": "50904",
                        "source_name": "Trooper Vog",
                        "creature2_name": "Trooper Vog",
                        "match_status": "ambiguous_name",
                        "entity_type": "0",
                        "zone_id": "12",
                        "worldid": "51",
                        "worldzoneid": "973",
                        "x": "6184",
                        "y": "-880",
                        "z": "-2911",
                    },
                    {
                        "source_coordinate_id": "10201",
                        "jabbithole_creature_id": "2199",
                        "creature2_id": "50904",
                        "source_name": "Trooper Vog",
                        "creature2_name": "Trooper Vog",
                        "match_status": "ambiguous_name",
                        "entity_type": "0",
                        "zone_id": "12",
                        "worldid": "51",
                        "worldzoneid": "973",
                        "x": "6202",
                        "y": "-888",
                        "z": "-2816",
                    },
                    {
                        "source_coordinate_id": "3742220",
                        "jabbithole_creature_id": "2199",
                        "creature2_id": "50904",
                        "source_name": "Trooper Vog",
                        "creature2_name": "Trooper Vog",
                        "match_status": "ambiguous_name",
                        "entity_type": "0",
                        "zone_id": "12",
                        "worldid": "51",
                        "worldzoneid": "973",
                        "x": "6202",
                        "y": "-888",
                        "z": "-2817",
                    },
                    {
                        "source_coordinate_id": "116070",
                        "jabbithole_creature_id": "9708",
                        "creature2_id": "12560",
                        "source_name": "Scientist Pristan",
                        "creature2_name": "Scientist Pristan",
                        "match_status": "reviewed",
                        "entity_type": "0",
                        "zone_id": "3",
                        "worldid": "426",
                        "worldzoneid": "595",
                        "x": "4357",
                        "y": "-751",
                        "z": "-5716",
                    },
                    {
                        "source_coordinate_id": "2501",
                        "jabbithole_creature_id": "619",
                        "creature2_id": "11194",
                        "source_name": "Master Control Panel",
                        "creature2_name": "Master Control Panel",
                        "match_status": "reviewed",
                        "entity_type": "0",
                        "zone_id": "3",
                        "worldid": "426",
                        "worldzoneid": "852",
                        "x": "4146",
                        "y": "-722",
                        "z": "-5729",
                    },
                    {
                        "source_coordinate_id": "7798992",
                        "jabbithole_creature_id": "133",
                        "creature2_id": "11925",
                        "source_name": "Crystal Guardian",
                        "creature2_name": "Crystal Guardian",
                        "match_status": "scored_name",
                        "entity_type": "0",
                        "zone_id": "1",
                        "worldid": "426",
                        "worldzoneid": "729",
                        "x": "4149",
                        "y": "-696",
                        "z": "-5887",
                    },
                    {
                        "source_coordinate_id": "7798993",
                        "jabbithole_creature_id": "133",
                        "creature2_id": "11925",
                        "source_name": "Crystal Guardian",
                        "creature2_name": "Crystal Guardian",
                        "match_status": "scored_name",
                        "entity_type": "0",
                        "zone_id": "1",
                        "worldid": "426",
                        "worldzoneid": "729",
                        "x": "4100",
                        "y": "-696",
                        "z": "-5862",
                    },
                    {
                        "source_coordinate_id": "7855831",
                        "jabbithole_creature_id": "133",
                        "creature2_id": "11925",
                        "source_name": "Crystal Guardian",
                        "creature2_name": "Crystal Guardian",
                        "match_status": "scored_name",
                        "entity_type": "0",
                        "zone_id": "1",
                        "worldid": "426",
                        "worldzoneid": "729",
                        "x": "4187",
                        "y": "-699",
                        "z": "-5890",
                    },
                    {
                        "source_coordinate_id": "7857737",
                        "jabbithole_creature_id": "133",
                        "creature2_id": "11925",
                        "source_name": "Crystal Guardian",
                        "creature2_name": "Crystal Guardian",
                        "match_status": "scored_name",
                        "entity_type": "0",
                        "zone_id": "1",
                        "worldid": "426",
                        "worldzoneid": "729",
                        "x": "4201",
                        "y": "-698",
                        "z": "-5919",
                    },
                    {
                        "source_coordinate_id": "8102237",
                        "jabbithole_creature_id": "133",
                        "creature2_id": "11925",
                        "source_name": "Crystal Guardian",
                        "creature2_name": "Crystal Guardian",
                        "match_status": "scored_name",
                        "entity_type": "0",
                        "zone_id": "1",
                        "worldid": "426",
                        "worldzoneid": "729",
                        "x": "4050",
                        "y": "-691",
                        "z": "-5849",
                    },
                    {
                        "source_coordinate_id": "1218",
                        "jabbithole_creature_id": "293",
                        "creature2_id": "12919",
                        "source_name": "Exile Supply Crate",
                        "creature2_name": "Exile Supply Crate",
                        "match_status": "reviewed",
                        "entity_type": "8",
                        "zone_id": "1",
                        "worldid": "426",
                        "worldzoneid": "597",
                        "x": "4582",
                        "y": "-734",
                        "z": "-5562",
                    },
                    {
                        "source_coordinate_id": "1219",
                        "jabbithole_creature_id": "293",
                        "creature2_id": "12919",
                        "source_name": "Exile Supply Crate",
                        "creature2_name": "Exile Supply Crate",
                        "match_status": "reviewed",
                        "entity_type": "8",
                        "zone_id": "1",
                        "worldid": "426",
                        "worldzoneid": "597",
                        "x": "4567",
                        "y": "-733",
                        "z": "-5588",
                    },
                    {
                        "source_coordinate_id": "1220",
                        "jabbithole_creature_id": "293",
                        "creature2_id": "12919",
                        "source_name": "Exile Supply Crate",
                        "creature2_name": "Exile Supply Crate",
                        "match_status": "reviewed",
                        "entity_type": "8",
                        "zone_id": "1",
                        "worldid": "426",
                        "worldzoneid": "597",
                        "x": "4558",
                        "y": "-732",
                        "z": "-5632",
                    },
                    {
                        "source_coordinate_id": "1221",
                        "jabbithole_creature_id": "293",
                        "creature2_id": "12919",
                        "source_name": "Exile Supply Crate",
                        "creature2_name": "Exile Supply Crate",
                        "match_status": "reviewed",
                        "entity_type": "8",
                        "zone_id": "1",
                        "worldid": "426",
                        "worldzoneid": "597",
                        "x": "4606",
                        "y": "-722",
                        "z": "-5647",
                    },
                    {
                        "source_coordinate_id": "1222",
                        "jabbithole_creature_id": "293",
                        "creature2_id": "12919",
                        "source_name": "Exile Supply Crate",
                        "creature2_name": "Exile Supply Crate",
                        "match_status": "reviewed",
                        "entity_type": "8",
                        "zone_id": "1",
                        "worldid": "426",
                        "worldzoneid": "597",
                        "x": "4489",
                        "y": "-743",
                        "z": "-5526",
                    },
                    {
                        "source_coordinate_id": "1223",
                        "jabbithole_creature_id": "293",
                        "creature2_id": "12919",
                        "source_name": "Exile Supply Crate",
                        "creature2_name": "Exile Supply Crate",
                        "match_status": "reviewed",
                        "entity_type": "8",
                        "zone_id": "1",
                        "worldid": "426",
                        "worldzoneid": "597",
                        "x": "4592",
                        "y": "-716",
                        "z": "-5731",
                    },
                    {
                        "source_coordinate_id": "1763",
                        "jabbithole_creature_id": "619",
                        "creature2_id": "11061",
                        "source_name": "Commander Durek",
                        "creature2_name": "Commander Durek",
                        "match_status": "ambiguous_name",
                        "entity_type": "0",
                        "zone_id": "3",
                        "worldid": "426",
                        "worldzoneid": "595",
                        "x": "4339",
                        "y": "-751",
                        "z": "-5678",
                    },
                    {
                        "source_coordinate_id": "40899",
                        "jabbithole_creature_id": "435",
                        "creature2_id": "11061",
                        "source_name": "Commander Durek",
                        "creature2_name": "Commander Durek",
                        "match_status": "ambiguous_name",
                        "entity_type": "0",
                        "zone_id": "1",
                        "worldid": "426",
                        "worldzoneid": "595",
                        "x": "4125",
                        "y": "-686",
                        "z": "-5246",
                    },
                    {
                        "source_coordinate_id": "8272460",
                        "jabbithole_creature_id": "816",
                        "creature2_id": "12212",
                        "source_name": "Rootbrute Grimspore",
                        "creature2_name": "Rootbrute Grimspore",
                        "match_status": "ambiguous_name",
                        "entity_type": "0",
                        "zone_id": "3",
                        "worldid": "426",
                        "worldzoneid": "597",
                        "x": "4553",
                        "y": "-733",
                        "z": "-5611",
                    },
                    {
                        "source_coordinate_id": "8272461",
                        "jabbithole_creature_id": "816",
                        "creature2_id": "12212",
                        "source_name": "Rootbrute Grimspore",
                        "creature2_name": "Rootbrute Grimspore",
                        "match_status": "ambiguous_name",
                        "entity_type": "0",
                        "zone_id": "3",
                        "worldid": "426",
                        "worldzoneid": "597",
                        "x": "4607",
                        "y": "-728",
                        "z": "-5559",
                    },
                    {
                        "source_coordinate_id": "8272462",
                        "jabbithole_creature_id": "816",
                        "creature2_id": "12212",
                        "source_name": "Rootbrute Grimspore",
                        "creature2_name": "Rootbrute Grimspore",
                        "match_status": "ambiguous_name",
                        "entity_type": "0",
                        "zone_id": "3",
                        "worldid": "426",
                        "worldzoneid": "597",
                        "x": "4536",
                        "y": "-736",
                        "z": "-5608",
                    },
                    {
                        "source_coordinate_id": "8272464",
                        "jabbithole_creature_id": "816",
                        "creature2_id": "12212",
                        "source_name": "Rootbrute Grimspore",
                        "creature2_name": "Rootbrute Grimspore",
                        "match_status": "ambiguous_name",
                        "entity_type": "0",
                        "zone_id": "3",
                        "worldid": "426",
                        "worldzoneid": "597",
                        "x": "4494",
                        "y": "-740",
                        "z": "-5676",
                    },
                    {
                        "source_coordinate_id": "3837530",
                        "jabbithole_creature_id": "817",
                        "creature2_id": "12213",
                        "source_name": "Rootbrute Deathcap",
                        "creature2_name": "Rootbrute Deathcap",
                        "match_status": "scored_name",
                        "entity_type": "0",
                        "zone_id": "3",
                        "worldid": "426",
                        "worldzoneid": "597",
                        "x": "4397",
                        "y": "-744",
                        "z": "-5511",
                    },
                    {
                        "source_coordinate_id": "3785147",
                        "jabbithole_creature_id": "817",
                        "creature2_id": "12213",
                        "source_name": "Rootbrute Deathcap",
                        "creature2_name": "Rootbrute Deathcap",
                        "match_status": "scored_name",
                        "entity_type": "0",
                        "zone_id": "3",
                        "worldid": "426",
                        "worldzoneid": "597",
                        "x": "4497",
                        "y": "-740",
                        "z": "-5590",
                    },
                    {
                        "source_coordinate_id": "159",
                        "jabbithole_creature_id": "34",
                        "creature2_id": "12737",
                        "source_name": "Bartol Sunward",
                        "creature2_name": "Bartol Sunward",
                        "match_status": "ambiguous_name",
                        "entity_type": "0",
                        "zone_id": "3",
                        "worldid": "426",
                        "worldzoneid": "595",
                        "x": "4355",
                        "y": "-750",
                        "z": "-5681",
                    },
                    {
                        "source_coordinate_id": "4272",
                        "jabbithole_creature_id": "1043",
                        "creature2_id": "12484",
                        "source_name": "Scientist Lusk",
                        "creature2_name": "Scientist Lusk",
                        "match_status": "ambiguous_name",
                        "entity_type": "0",
                        "zone_id": "1",
                        "worldid": "426",
                        "worldzoneid": "604",
                        "x": "4800",
                        "y": "-710",
                        "z": "-5651",
                    },
                    {
                        "source_coordinate_id": "2622",
                        "jabbithole_creature_id": "674",
                        "creature2_id": "14124",
                        "source_name": "Imprisoned Survivor",
                        "creature2_name": "Imprisoned Survivor",
                        "match_status": "unique_name",
                        "entity_type": "0",
                        "zone_id": "1",
                        "worldid": "426",
                        "worldzoneid": "609",
                        "x": "5034",
                        "y": "-711",
                        "z": "-5676",
                    },
                    {
                        "source_coordinate_id": "5228",
                        "jabbithole_creature_id": "674",
                        "creature2_id": "14124",
                        "source_name": "Imprisoned Survivor",
                        "creature2_name": "Imprisoned Survivor",
                        "match_status": "unique_name",
                        "entity_type": "0",
                        "zone_id": "1",
                        "worldid": "426",
                        "worldzoneid": "609",
                        "x": "5016",
                        "y": "-708",
                        "z": "-5702",
                    },
                    {
                        "source_coordinate_id": "5594684",
                        "jabbithole_creature_id": "27689",
                        "creature2_id": "14124",
                        "source_name": "Imprisoned Survivor",
                        "creature2_name": "Imprisoned Survivor",
                        "match_status": "unique_name",
                        "entity_type": "0",
                        "zone_id": "1",
                        "worldid": "426",
                        "worldzoneid": "609",
                        "x": "5034",
                        "y": "-711",
                        "z": "-5676",
                    },
                    {
                        "source_coordinate_id": "6216334",
                        "jabbithole_creature_id": "475",
                        "creature2_id": "36429",
                        "source_name": "Fierce Skeech Scratcher",
                        "creature2_name": "Fierce Skeech Scratcher",
                        "match_status": "scored_name",
                        "entity_type": "0",
                        "zone_id": "1",
                        "worldid": "426",
                        "worldzoneid": "604",
                        "x": "4642",
                        "y": "-728",
                        "z": "-5715",
                    },
                    {
                        "source_coordinate_id": "3983957",
                        "jabbithole_creature_id": "215",
                        "creature2_id": "17545",
                        "source_name": "Skeech Dagunlord",
                        "creature2_name": "Skeech Dagunlord",
                        "match_status": "unique_name",
                        "entity_type": "0",
                        "zone_id": "1",
                        "worldid": "426",
                        "worldzoneid": "604",
                        "x": "4643",
                        "y": "-728",
                        "z": "-5718",
                    },
                    {
                        "source_coordinate_id": "7903135",
                        "jabbithole_creature_id": "27648",
                        "creature2_id": "11910",
                        "source_name": "Skeech Scratcher",
                        "creature2_name": "Skeech Scratcher",
                        "match_status": "unique_name",
                        "entity_type": "0",
                        "zone_id": "1",
                        "worldid": "426",
                        "worldzoneid": "609",
                        "x": "4965",
                        "y": "-705",
                        "z": "-5672",
                    },
                    {
                        "source_coordinate_id": "7433711",
                        "jabbithole_creature_id": "27646",
                        "creature2_id": "11912",
                        "source_name": "Skeech Shaman",
                        "creature2_name": "Skeech Shaman",
                        "match_status": "scored_name",
                        "entity_type": "0",
                        "zone_id": "1",
                        "worldid": "426",
                        "worldzoneid": "609",
                        "x": "4971",
                        "y": "-706",
                        "z": "-5674",
                    },
                    {
                        "source_coordinate_id": "7766435",
                        "jabbithole_creature_id": "27650",
                        "creature2_id": "11907",
                        "source_name": "Skeech Diviner",
                        "creature2_name": "Skeech Diviner",
                        "match_status": "scored_name",
                        "entity_type": "0",
                        "zone_id": "1",
                        "worldid": "426",
                        "worldzoneid": "609",
                        "x": "4900",
                        "y": "-709",
                        "z": "-5686",
                    },
                    {
                        "source_coordinate_id": "5611704",
                        "jabbithole_creature_id": "27645",
                        "creature2_id": "11917",
                        "source_name": "Skeech Frostlord",
                        "creature2_name": "Skeech Frostlord",
                        "match_status": "scored_name",
                        "entity_type": "0",
                        "zone_id": "1",
                        "worldid": "426",
                        "worldzoneid": "609",
                        "x": "5003",
                        "y": "-720",
                        "z": "-5728",
                    },
                    {
                        "source_coordinate_id": "5594149",
                        "jabbithole_creature_id": "27684",
                        "creature2_id": "36884",
                        "source_name": "Coldburrow Xenobreeder",
                        "creature2_name": "Coldburrow Xenobreeder",
                        "match_status": "unique_name",
                        "entity_type": "0",
                        "zone_id": "1",
                        "worldid": "426",
                        "worldzoneid": "609",
                        "x": "5057",
                        "y": "-711",
                        "z": "-5791",
                    },
                    {
                        "source_coordinate_id": "4368",
                        "jabbithole_creature_id": "1073",
                        "creature2_id": "11063",
                        "source_name": "Deadeye Brightland",
                        "creature2_name": "Deadeye Brightland",
                        "match_status": "reviewed",
                        "entity_type": "0",
                        "zone_id": "3",
                        "worldid": "426",
                        "worldzoneid": "597",
                        "x": "4341",
                        "y": "-751",
                        "z": "-5683",
                    },
                    {
                        "source_coordinate_id": "4370",
                        "jabbithole_creature_id": "1073",
                        "creature2_id": "11063",
                        "source_name": "Deadeye Brightland",
                        "creature2_name": "Deadeye Brightland",
                        "match_status": "reviewed",
                        "entity_type": "0",
                        "zone_id": "3",
                        "worldid": "426",
                        "worldzoneid": "597",
                        "x": "4185",
                        "y": "-722",
                        "z": "-5695",
                    },
                    {
                        "source_coordinate_id": "5142",
                        "jabbithole_creature_id": "1073",
                        "creature2_id": "11063",
                        "source_name": "Deadeye Brightland",
                        "creature2_name": "Deadeye Brightland",
                        "match_status": "reviewed",
                        "entity_type": "0",
                        "zone_id": "3",
                        "worldid": "426",
                        "worldzoneid": "597",
                        "x": "4486",
                        "y": "-724",
                        "z": "-5391",
                    },
                    {
                        "source_coordinate_id": "837",
                        "jabbithole_creature_id": "201",
                        "creature2_id": "16622",
                        "source_name": "Deadeye Brightland",
                        "creature2_name": "Deadeye Brightland",
                        "match_status": "reviewed",
                        "entity_type": "0",
                        "zone_id": "13",
                        "worldid": "51",
                        "worldzoneid": "23",
                        "x": "3833",
                        "y": "-1018",
                        "z": "-4642",
                    },
                    {
                        "source_coordinate_id": "2587271",
                        "jabbithole_creature_id": "201",
                        "creature2_id": "16622",
                        "source_name": "Deadeye Brightland",
                        "creature2_name": "Deadeye Brightland",
                        "match_status": "reviewed",
                        "entity_type": "0",
                        "zone_id": "13",
                        "worldid": "51",
                        "worldzoneid": "23",
                        "x": "3026",
                        "y": "-1001",
                        "z": "-4310",
                    },
                    {
                        "source_coordinate_id": "3406",
                        "jabbithole_creature_id": "868",
                        "creature2_id": "11070",
                        "source_name": "Trapped Survivor",
                        "creature2_name": "Trapped Survivor",
                        "match_status": "unique_name",
                        "entity_type": "0",
                        "zone_id": "1",
                        "worldid": "426",
                        "worldzoneid": "646",
                        "x": "3980",
                        "y": "-714",
                        "z": "-5371",
                    },
                    {
                        "source_coordinate_id": "3407",
                        "jabbithole_creature_id": "868",
                        "creature2_id": "11070",
                        "source_name": "Trapped Survivor",
                        "creature2_name": "Trapped Survivor",
                        "match_status": "unique_name",
                        "entity_type": "0",
                        "zone_id": "1",
                        "worldid": "426",
                        "worldzoneid": "646",
                        "x": "3937",
                        "y": "-704",
                        "z": "-5335",
                    },
                    {
                        "source_coordinate_id": "3408",
                        "jabbithole_creature_id": "868",
                        "creature2_id": "11070",
                        "source_name": "Trapped Survivor",
                        "creature2_name": "Trapped Survivor",
                        "match_status": "unique_name",
                        "entity_type": "0",
                        "zone_id": "1",
                        "worldid": "426",
                        "worldzoneid": "646",
                        "x": "3974",
                        "y": "-707",
                        "z": "-5359",
                    },
                    {
                        "source_coordinate_id": "15032",
                        "jabbithole_creature_id": "868",
                        "creature2_id": "11070",
                        "source_name": "Trapped Survivor",
                        "creature2_name": "Trapped Survivor",
                        "match_status": "unique_name",
                        "entity_type": "0",
                        "zone_id": "1",
                        "worldid": "426",
                        "worldzoneid": "646",
                        "x": "4009",
                        "y": "-723",
                        "z": "-5401",
                    },
                    {
                        "source_coordinate_id": "17868",
                        "jabbithole_creature_id": "868",
                        "creature2_id": "11070",
                        "source_name": "Trapped Survivor",
                        "creature2_name": "Trapped Survivor",
                        "match_status": "unique_name",
                        "entity_type": "0",
                        "zone_id": "1",
                        "worldid": "426",
                        "worldzoneid": "646",
                        "x": "3923",
                        "y": "-702",
                        "z": "-5374",
                    },
                    {
                        "source_coordinate_id": "40681",
                        "jabbithole_creature_id": "868",
                        "creature2_id": "11070",
                        "source_name": "Trapped Survivor",
                        "creature2_name": "Trapped Survivor",
                        "match_status": "unique_name",
                        "entity_type": "0",
                        "zone_id": "1",
                        "worldid": "426",
                        "worldzoneid": "646",
                        "x": "4202",
                        "y": "-731",
                        "z": "-5407",
                    },
                    {
                        "source_coordinate_id": "40682",
                        "jabbithole_creature_id": "868",
                        "creature2_id": "11070",
                        "source_name": "Trapped Survivor",
                        "creature2_name": "Trapped Survivor",
                        "match_status": "unique_name",
                        "entity_type": "0",
                        "zone_id": "1",
                        "worldid": "426",
                        "worldzoneid": "646",
                        "x": "4198",
                        "y": "-725",
                        "z": "-5378",
                    },
                    {
                        "source_coordinate_id": "40683",
                        "jabbithole_creature_id": "868",
                        "creature2_id": "11070",
                        "source_name": "Trapped Survivor",
                        "creature2_name": "Trapped Survivor",
                        "match_status": "unique_name",
                        "entity_type": "0",
                        "zone_id": "1",
                        "worldid": "426",
                        "worldzoneid": "646",
                        "x": "4193",
                        "y": "-728",
                        "z": "-5427",
                    },
                    {
                        "source_coordinate_id": "41559",
                        "jabbithole_creature_id": "868",
                        "creature2_id": "11070",
                        "source_name": "Trapped Survivor",
                        "creature2_name": "Trapped Survivor",
                        "match_status": "unique_name",
                        "entity_type": "0",
                        "zone_id": "1",
                        "worldid": "426",
                        "worldzoneid": "646",
                        "x": "4187",
                        "y": "-705",
                        "z": "-5317",
                    },
                    {
                        "source_coordinate_id": "42765",
                        "jabbithole_creature_id": "868",
                        "creature2_id": "11070",
                        "source_name": "Trapped Survivor",
                        "creature2_name": "Trapped Survivor",
                        "match_status": "unique_name",
                        "entity_type": "0",
                        "zone_id": "1",
                        "worldid": "426",
                        "worldzoneid": "646",
                        "x": "4142",
                        "y": "-722",
                        "z": "-5381",
                    },
                    {
                        "source_coordinate_id": "114996",
                        "jabbithole_creature_id": "868",
                        "creature2_id": "11070",
                        "source_name": "Trapped Survivor",
                        "creature2_name": "Trapped Survivor",
                        "match_status": "unique_name",
                        "entity_type": "0",
                        "zone_id": "1",
                        "worldid": "426",
                        "worldzoneid": "646",
                        "x": "4193",
                        "y": "-734",
                        "z": "-5459",
                    },
                    {
                        "source_coordinate_id": "114997",
                        "jabbithole_creature_id": "868",
                        "creature2_id": "11070",
                        "source_name": "Trapped Survivor",
                        "creature2_name": "Trapped Survivor",
                        "match_status": "unique_name",
                        "entity_type": "0",
                        "zone_id": "1",
                        "worldid": "426",
                        "worldzoneid": "646",
                        "x": "4149",
                        "y": "-736",
                        "z": "-5434",
                    },
                    {
                        "source_coordinate_id": "115687",
                        "jabbithole_creature_id": "868",
                        "creature2_id": "11070",
                        "source_name": "Trapped Survivor",
                        "creature2_name": "Trapped Survivor",
                        "match_status": "unique_name",
                        "entity_type": "0",
                        "zone_id": "1",
                        "worldid": "426",
                        "worldzoneid": "646",
                        "x": "4096",
                        "y": "-737",
                        "z": "-5450",
                    },
                    {
                        "source_coordinate_id": "121680",
                        "jabbithole_creature_id": "868",
                        "creature2_id": "11070",
                        "source_name": "Trapped Survivor",
                        "creature2_name": "Trapped Survivor",
                        "match_status": "unique_name",
                        "entity_type": "0",
                        "zone_id": "1",
                        "worldid": "426",
                        "worldzoneid": "646",
                        "x": "3951",
                        "y": "-711",
                        "z": "-5416",
                    },
                    {
                        "source_coordinate_id": "156500",
                        "jabbithole_creature_id": "868",
                        "creature2_id": "11070",
                        "source_name": "Trapped Survivor",
                        "creature2_name": "Trapped Survivor",
                        "match_status": "unique_name",
                        "entity_type": "0",
                        "zone_id": "1",
                        "worldid": "426",
                        "worldzoneid": "646",
                        "x": "4124",
                        "y": "-745",
                        "z": "-5562",
                    },
                    {
                        "source_coordinate_id": "156501",
                        "jabbithole_creature_id": "868",
                        "creature2_id": "11070",
                        "source_name": "Trapped Survivor",
                        "creature2_name": "Trapped Survivor",
                        "match_status": "unique_name",
                        "entity_type": "0",
                        "zone_id": "1",
                        "worldid": "426",
                        "worldzoneid": "646",
                        "x": "4064",
                        "y": "-738",
                        "z": "-5479",
                    },
                    {
                        "source_coordinate_id": "7915699",
                        "jabbithole_creature_id": "244",
                        "creature2_id": "11945",
                        "source_name": "Yeti Snowstalker",
                        "creature2_name": "Yeti Snowstalker",
                        "match_status": "ambiguous_name",
                        "entity_type": "0",
                        "zone_id": "1",
                        "worldid": "426",
                        "worldzoneid": "651",
                        "x": "3977",
                        "y": "-713",
                        "z": "-5402",
                    },
                    {
                        "source_coordinate_id": "7915700",
                        "jabbithole_creature_id": "244",
                        "creature2_id": "11945",
                        "source_name": "Yeti Snowstalker",
                        "creature2_name": "Yeti Snowstalker",
                        "match_status": "ambiguous_name",
                        "entity_type": "0",
                        "zone_id": "1",
                        "worldid": "426",
                        "worldzoneid": "651",
                        "x": "4022",
                        "y": "-725",
                        "z": "-5413",
                    },
                    {
                        "source_coordinate_id": "7956687",
                        "jabbithole_creature_id": "244",
                        "creature2_id": "11945",
                        "source_name": "Yeti Snowstalker",
                        "creature2_name": "Yeti Snowstalker",
                        "match_status": "ambiguous_name",
                        "entity_type": "0",
                        "zone_id": "1",
                        "worldid": "426",
                        "worldzoneid": "651",
                        "x": "4067",
                        "y": "-738",
                        "z": "-5476",
                    },
                    {
                        "source_coordinate_id": "8401261",
                        "jabbithole_creature_id": "244",
                        "creature2_id": "11945",
                        "source_name": "Yeti Snowstalker",
                        "creature2_name": "Yeti Snowstalker",
                        "match_status": "ambiguous_name",
                        "entity_type": "0",
                        "zone_id": "1",
                        "worldid": "426",
                        "worldzoneid": "651",
                        "x": "4187",
                        "y": "-738",
                        "z": "-5481",
                    },
                    {
                        "source_coordinate_id": "8110657",
                        "jabbithole_creature_id": "995",
                        "creature2_id": "11948",
                        "source_name": "Yeti Frostclaw",
                        "creature2_name": "Yeti Frostclaw",
                        "match_status": "scored_name",
                        "entity_type": "0",
                        "zone_id": "1",
                        "worldid": "426",
                        "worldzoneid": "597",
                        "x": "4000",
                        "y": "-711",
                        "z": "-5342",
                    },
                    {
                        "source_coordinate_id": "8158349",
                        "jabbithole_creature_id": "995",
                        "creature2_id": "11948",
                        "source_name": "Yeti Frostclaw",
                        "creature2_name": "Yeti Frostclaw",
                        "match_status": "scored_name",
                        "entity_type": "0",
                        "zone_id": "1",
                        "worldid": "426",
                        "worldzoneid": "597",
                        "x": "4035",
                        "y": "-716",
                        "z": "-5373",
                    },
                    {
                        "source_coordinate_id": "8204800",
                        "jabbithole_creature_id": "995",
                        "creature2_id": "11948",
                        "source_name": "Yeti Frostclaw",
                        "creature2_name": "Yeti Frostclaw",
                        "match_status": "scored_name",
                        "entity_type": "0",
                        "zone_id": "1",
                        "worldid": "426",
                        "worldzoneid": "597",
                        "x": "3930",
                        "y": "-704",
                        "z": "-5376",
                    },
                    {
                        "source_coordinate_id": "8251657",
                        "jabbithole_creature_id": "995",
                        "creature2_id": "11948",
                        "source_name": "Yeti Frostclaw",
                        "creature2_name": "Yeti Frostclaw",
                        "match_status": "scored_name",
                        "entity_type": "0",
                        "zone_id": "1",
                        "worldid": "426",
                        "worldzoneid": "597",
                        "x": "4183",
                        "y": "-724",
                        "z": "-5373",
                    },
                    {
                        "source_coordinate_id": "2960",
                        "jabbithole_creature_id": "756",
                        "creature2_id": "12521",
                        "source_name": "Signal Flare",
                        "creature2_name": "Signal Flare",
                        "match_status": "ambiguous_name",
                        "entity_type": "10",
                        "zone_id": "3",
                        "worldid": "426",
                        "worldzoneid": "595",
                        "x": "4367",
                        "y": "-744",
                        "z": "-5639",
                    },
                    {
                        "source_coordinate_id": "2961",
                        "jabbithole_creature_id": "756",
                        "creature2_id": "12521",
                        "source_name": "Signal Flare",
                        "creature2_name": "Signal Flare",
                        "match_status": "ambiguous_name",
                        "entity_type": "10",
                        "zone_id": "3",
                        "worldid": "426",
                        "worldzoneid": "595",
                        "x": "4387",
                        "y": "-745",
                        "z": "-5637",
                    },
                    {
                        "source_coordinate_id": "2962",
                        "jabbithole_creature_id": "756",
                        "creature2_id": "12521",
                        "source_name": "Signal Flare",
                        "creature2_name": "Signal Flare",
                        "match_status": "ambiguous_name",
                        "entity_type": "10",
                        "zone_id": "3",
                        "worldid": "426",
                        "worldzoneid": "595",
                        "x": "4348",
                        "y": "-745",
                        "z": "-5637",
                    },
                ] + q5597_chua_explosive_spawns,
            )

            self.assertEqual(62, len(rows))
            starter = next(row for row in rows if row["source_relation_id"] == 1)
            self.assertEqual("current_quest", starter["quest_link_status"])
            self.assertEqual("runtime_spawn_present_reviewed_starter_creature_relation_pending_dialog_credit_smoke", starter["evidence_status"])
            self.assertEqual(1, starter["runtime_spawn_count"])
            self.assertEqual("1", starter["runtime_spawn_worlds"])
            self.assertEqual("2", starter["runtime_spawn_areas"])
            self.assertEqual(1, starter["runtime_datamapping_position_match_count"])
            self.assertEqual("501", starter["runtime_matched_source_coordinate_ids"])
            self.assertEqual("100", starter["runtime_matched_entity_ids"])
            self.assertEqual("", starter["datamapping_unpromoted_spawn_count"])
            self.assertEqual("", starter["datamapping_unpromoted_source_coordinate_ids"])
            self.assertEqual("", starter["datamapping_unpromoted_position_sample"])
            self.assertIn("runtime_world_seed.sql", starter["evidence_sources"])
            self.assertIn("source_coordinate_id(s) 501", starter["blocker_summary"])
            unmatched = next(row for row in rows if row["relation_type"] == "objective")
            self.assertEqual("datamapping_creature_bridge_unmatched_blocked", unmatched["evidence_status"])
            self.assertEqual("", unmatched["creature2_id"])
            self.assertIn("creature_map.csv", unmatched["evidence_sources"])
            self.assertIn("creature_spawn_map.csv", unmatched["evidence_sources"])
            self.assertIn("jabbithole_mysql/creatures.sql", unmatched["evidence_sources"])
            self.assertIn("jabbithole_mysql/quest_creatures.sql", unmatched["evidence_sources"])
            self.assertIn("Creature2.tbl.sql", unmatched["evidence_sources"])
            self.assertIn("Tools/DataMapping/README.md", unmatched["evidence_sources"])
            self.assertIn("objective source_relation_id 2", unmatched["blocker_summary"])
            self.assertIn("Quest2 10", unmatched["blocker_summary"])
            self.assertIn("Jabbithole creature 11 (Missing NPC)", unmatched["blocker_summary"])
            self.assertIn("match_status unmatched", unmatched["blocker_summary"])
            self.assertIn("no current Creature2 bridge", unmatched["blocker_summary"])
            noncurrent = next(row for row in rows if row["quest_link_status"] == "non_current_client_quest_reference")
            self.assertEqual("datamapping_quest_relation_not_current_client_quest_row_blocked", noncurrent["evidence_status"])
            self.assertEqual("non_current_client_quest_reference", noncurrent["quest_bucket"])
            ambiguous = next(row for row in rows if row["source_relation_id"] == 4)
            self.assertEqual("datamapping_creature_bridge_ambiguous_pending_review", ambiguous["evidence_status"])
            self.assertEqual(1, ambiguous["runtime_spawn_count"])
            self.assertIn("creature_map.csv", ambiguous["evidence_sources"])
            self.assertIn("creature_spawn_map.csv", ambiguous["evidence_sources"])
            self.assertIn("jabbithole_mysql/quest_starter_creatures.sql", ambiguous["evidence_sources"])
            self.assertIn("Creature2.tbl.sql", ambiguous["evidence_sources"])
            self.assertIn("runtime_world_seed.sql", ambiguous["evidence_sources"])
            self.assertIn("Tools/DataMapping/README.md", ambiguous["evidence_sources"])
            self.assertIn("starter source_relation_id 4", ambiguous["blocker_summary"])
            self.assertIn("Jabbithole creature 13 (Ambiguous NPC)", ambiguous["blocker_summary"])
            self.assertIn("Creature2 23 (Ambiguous NPC)", ambiguous["blocker_summary"])
            self.assertIn("Creature2 bridge remains ambiguous", ambiguous["blocker_summary"])
            self.assertIn("reviewed bridge disambiguation", ambiguous["blocker_summary"])
            q4696_starter = next(row for row in rows if row["quest_id"] == 4696 and row["relation_type"] == "starter")
            self.assertEqual(17175, q4696_starter["creature2_id"])
            self.assertEqual("client_creature2_quest_relation_reviewed", q4696_starter["match_status"])
            self.assertEqual(
                "runtime_script_spawn_present_client_creature2_starter_relation_with_quest_giver_cache_and_accept_path_tests_pending_dialog_smoke",
                q4696_starter["evidence_status"],
            )
            self.assertIn("Creature2.tbl.sql", q4696_starter["evidence_sources"])
            self.assertIn("GalerasMapScriptTests.cs", q4696_starter["evidence_sources"])
            self.assertIn("GlobalQuestManagerPartialTableTests.cs", q4696_starter["evidence_sources"])
            self.assertIn("QuestTests.cs", q4696_starter["evidence_sources"])
            self.assertIn("source_coordinate_id 9972", q4696_starter["blocker_summary"])
            self.assertIn("quest-giver cache", q4696_starter["blocker_summary"])
            self.assertIn("server-side accept coverage", q4696_starter["blocker_summary"])
            q4696_finisher = next(row for row in rows if row["quest_id"] == 4696 and row["relation_type"] == "finisher")
            self.assertEqual(17053, q4696_finisher["creature2_id"])
            self.assertEqual(1, q4696_finisher["runtime_spawn_count"])
            self.assertEqual("51", q4696_finisher["runtime_spawn_worlds"])
            self.assertEqual("973", q4696_finisher["runtime_spawn_areas"])
            self.assertEqual(2, q4696_finisher["datamapping_spawn_count"])
            self.assertEqual("10020;99084", q4696_finisher["datamapping_source_coordinate_id_sample"])
            self.assertEqual("51", q4696_finisher["datamapping_spawn_worlds"])
            self.assertEqual("973", q4696_finisher["datamapping_spawn_areas"])
            self.assertEqual("51/973/6203/-888/-2809;51/973/6176/-880/-2910", q4696_finisher["datamapping_spawn_position_sample"])
            self.assertEqual(1, q4696_finisher["runtime_datamapping_position_match_count"])
            self.assertEqual("10020", q4696_finisher["runtime_matched_source_coordinate_ids"])
            self.assertEqual("102", q4696_finisher["runtime_matched_entity_ids"])
            self.assertEqual(1, q4696_finisher["datamapping_unpromoted_spawn_count"])
            self.assertEqual("99084", q4696_finisher["datamapping_unpromoted_source_coordinate_ids"])
            self.assertEqual("51/973/6176/-880/-2910", q4696_finisher["datamapping_unpromoted_position_sample"])
            self.assertEqual(
                "runtime_script_receiver_location_spawn_and_receiver_cache_tested_pending_dialog_client_smoke",
                q4696_finisher["evidence_status"],
            )
            self.assertIn("GlobalQuestManagerPartialTableTests.cs", q4696_finisher["evidence_sources"])
            self.assertIn("QuestTests.cs", q4696_finisher["evidence_sources"])
            self.assertIn("creature_spawn_map.csv", q4696_finisher["evidence_sources"])
            self.assertIn("GalerasMapScript.cs", q4696_finisher["evidence_sources"])
            self.assertIn("GalerasMapScriptTests.cs", q4696_finisher["evidence_sources"])
            self.assertIn("receiver-cache coverage", q4696_finisher["blocker_summary"])
            self.assertIn("source_coordinate_id 10020", q4696_finisher["blocker_summary"])
            self.assertIn("source_coordinate_id 99084", q4696_finisher["blocker_summary"])
            self.assertIn("Quest2 receiver location 17590", q4696_finisher["blocker_summary"])
            self.assertIn("6204.08,-888.474,-2809.91", q4696_finisher["blocker_summary"])
            self.assertIn("6203,-888,-2809", q4696_finisher["blocker_summary"])
            self.assertIn("6176,-880,-2910", q4696_finisher["blocker_summary"])
            self.assertIn("visible 17053", q4696_finisher["blocker_summary"])
            q4694_vog = next(row for row in rows if row["quest_id"] == 4694 and row["relation_type"] == "objective")
            self.assertEqual(50904, q4694_vog["creature2_id"])
            self.assertEqual("client_creature2_quest_relation_reviewed", q4694_vog["match_status"])
            self.assertEqual(3, q4694_vog["datamapping_spawn_count"])
            self.assertEqual("10200;10201;3742220", q4694_vog["datamapping_source_coordinate_id_sample"])
            self.assertEqual("51/973/6184/-880/-2911;51/973/6202/-888/-2816;51/973/6202/-888/-2817", q4694_vog["datamapping_spawn_position_sample"])
            self.assertEqual("", q4694_vog["runtime_spawn_count"])
            self.assertEqual(3, q4694_vog["datamapping_unpromoted_spawn_count"])
            self.assertEqual("10200;10201;3742220", q4694_vog["datamapping_unpromoted_source_coordinate_ids"])
            self.assertEqual(
                "runtime_script_spawn_present_client_creature2_objective_relation_with_activation_and_holdout_credit_tests_pending_dialog_smoke",
                q4694_vog["evidence_status"],
            )
            self.assertIn("ClientActivateUnitHandlerTests.cs", q4694_vog["evidence_sources"])
            self.assertIn("Spell4Effects.tbl.sql", q4694_vog["evidence_sources"])
            self.assertIn("runtime_world_seed.sql", q4694_vog["evidence_sources"])
            self.assertIn("Q4694HoldTheLine.cs", q4694_vog["evidence_sources"])
            self.assertIn("GalerasMapScriptTests.cs", q4694_vog["evidence_sources"])
            self.assertIn("GalerasQuestScriptTests.cs", q4694_vog["evidence_sources"])
            self.assertIn("Q4694 Trooper Vog holdout flag unit", q4694_vog["blocker_summary"])
            self.assertIn("ActivateEntity data 50904", q4694_vog["blocker_summary"])
            self.assertIn("activate spell 50151", q4694_vog["blocker_summary"])
            self.assertIn("Creature2 23953", q4694_vog["blocker_summary"])
            self.assertIn("CompleteEvent data 111", q4694_vog["blocker_summary"])
            self.assertIn("93 promoted runtime_world_seed.sql rows", q4694_vog["blocker_summary"])
            self.assertIn("23 near Trooper Vog", q4694_vog["blocker_summary"])
            q5610_mondo = next(row for row in rows if row["quest_id"] == 5610 and row["relation_type"] == "finisher")
            self.assertEqual(24187, q5610_mondo["creature2_id"])
            self.assertEqual("reviewed", q5610_mondo["match_status"])
            self.assertEqual("", q5610_mondo["runtime_spawn_count"])
            self.assertEqual(8, q5610_mondo["datamapping_spawn_count"])
            self.assertEqual("15087;15088;15089;21174;136782;1863809;2480207;2561752", q5610_mondo["datamapping_source_coordinate_id_sample"])
            self.assertEqual(8, q5610_mondo["datamapping_unpromoted_spawn_count"])
            self.assertEqual(
                "runtime_script_spawn_present_client_creature2_finisher_relation_tested_pending_dialog_smoke",
                q5610_mondo["evidence_status"],
            )
            self.assertIn("CrimsonIsleMapScript.cs", q5610_mondo["evidence_sources"])
            self.assertIn("CrimsonIsleBranchInteractionTests.cs", q5610_mondo["evidence_sources"])
            self.assertIn("QuestTests.cs", q5610_mondo["evidence_sources"])
            self.assertIn("Mondo Zax", q5610_mondo["blocker_summary"])
            self.assertIn("source_coordinate_id 15089", q5610_mondo["blocker_summary"])
            self.assertIn("visible-receiver completion", q5610_mondo["blocker_summary"])
            q5597_mondo_starter = next(row for row in rows if row["quest_id"] == 5597 and row["relation_type"] == "starter")
            self.assertEqual(1922, q5597_mondo_starter["source_relation_id"])
            self.assertEqual(24187, q5597_mondo_starter["creature2_id"])
            self.assertEqual("reviewed", q5597_mondo_starter["match_status"])
            self.assertEqual(8, q5597_mondo_starter["datamapping_spawn_count"])
            self.assertEqual("15087;15088;15089;21174;136782;1863809;2480207;2561752", q5597_mondo_starter["datamapping_source_coordinate_id_sample"])
            self.assertEqual(8, q5597_mondo_starter["datamapping_unpromoted_spawn_count"])
            self.assertEqual(
                "runtime_q5597_mondo_starter_spawn_accept_gate_and_followup_tested_external_video_observed_pending_local_dialog_ui_smoke",
                q5597_mondo_starter["evidence_status"],
            )
            self.assertIn("QuestTests.cs", q5597_mondo_starter["evidence_sources"])
            self.assertIn("CrimsonIsleQuestChainTests.cs", q5597_mondo_starter["evidence_sources"])
            self.assertIn("CrimsonIsleMapScript.cs", q5597_mondo_starter["evidence_sources"])
            self.assertIn("QuestIdGiven 5597", q5597_mondo_starter["blocker_summary"])
            self.assertIn("8 reviewed Mondo Zax 24187 placements", q5597_mondo_starter["blocker_summary"])
            self.assertIn("Q5596QuestScript grants Q5597", q5597_mondo_starter["blocker_summary"])
            self.assertIn("visible Mondo Zax 24187", q5597_mondo_starter["blocker_summary"])
            self.assertIn("completed Q5596", q5597_mondo_starter["blocker_summary"])
            self.assertIn("Dominion faction", q5597_mondo_starter["blocker_summary"])
            self.assertIn("level gate satisfied", q5597_mondo_starter["blocker_summary"])
            self.assertIn("rejects missing Mondo", q5597_mondo_starter["blocker_summary"])
            self.assertIn("Mondo accept/follow-up path is externally observed", q5597_mondo_starter["blocker_summary"])
            self.assertIn("denial text/UI", q5597_mondo_starter["blocker_summary"])
            self.assertIn("end-to-end Q5596->Q5597->Q5604 smoke", q5597_mondo_starter["blocker_summary"])
            q5597_mondo_finisher = next(row for row in rows if row["quest_id"] == 5597 and row["relation_type"] == "finisher")
            self.assertEqual(1564, q5597_mondo_finisher["source_relation_id"])
            self.assertEqual(24187, q5597_mondo_finisher["creature2_id"])
            self.assertEqual("reviewed", q5597_mondo_finisher["match_status"])
            self.assertEqual(
                "runtime_q5597_mondo_finisher_spawn_rewards_achievement_and_followup_tested_external_video_observed_pending_local_dialog_ui_smoke",
                q5597_mondo_finisher["evidence_status"],
            )
            self.assertIn("QuestTests.cs", q5597_mondo_finisher["evidence_sources"])
            self.assertIn("CrimsonIsleMapScript.cs", q5597_mondo_finisher["evidence_sources"])
            self.assertIn("Q5597QuestScript grants Q5604", q5597_mondo_finisher["blocker_summary"])
            self.assertIn("fixed Quest2Reward rows 3000 and 5236", q5597_mondo_finisher["blocker_summary"])
            self.assertIn("Bloodstone achievement 4134", q5597_mondo_finisher["blocker_summary"])
            self.assertIn("reward presentation are externally observed", q5597_mondo_finisher["blocker_summary"])
            self.assertIn("local reward inventory persistence", q5597_mondo_finisher["blocker_summary"])
            self.assertIn("achievement UI/progression", q5597_mondo_finisher["blocker_summary"])
            q5573_mondo_starter = next(row for row in rows if row["quest_id"] == 5573 and row["relation_type"] == "starter")
            self.assertEqual(1919, q5573_mondo_starter["source_relation_id"])
            self.assertEqual(24187, q5573_mondo_starter["creature2_id"])
            self.assertEqual("reviewed", q5573_mondo_starter["match_status"])
            self.assertEqual(8, q5573_mondo_starter["datamapping_spawn_count"])
            self.assertEqual("15087;15088;15089;21174;136782;1863809;2480207;2561752", q5573_mondo_starter["datamapping_source_coordinate_id_sample"])
            self.assertEqual(
                "runtime_q5573_mondo_starter_spawn_and_giver_cache_tested_pending_dialog_smoke",
                q5573_mondo_starter["evidence_status"],
            )
            self.assertIn("CrimsonIsleMapScript.cs", q5573_mondo_starter["evidence_sources"])
            self.assertIn("GlobalQuestManagerPartialTableTests.cs", q5573_mondo_starter["evidence_sources"])
            self.assertIn("QuestTests.cs", q5573_mondo_starter["evidence_sources"])
            self.assertIn("QuestIdGiven", q5573_mondo_starter["blocker_summary"])
            self.assertIn("source_coordinate_id 15089", q5573_mondo_starter["blocker_summary"])
            self.assertIn("accepts Q5573 only when Mondo is visible", q5573_mondo_starter["blocker_summary"])
            self.assertFalse(audit.is_creature_evidence_blocker(q5573_mondo_starter))
            q5573_mondo_finisher = next(row for row in rows if row["quest_id"] == 5573 and row["relation_type"] == "finisher")
            self.assertEqual(1558, q5573_mondo_finisher["source_relation_id"])
            self.assertEqual(24187, q5573_mondo_finisher["creature2_id"])
            self.assertEqual("reviewed", q5573_mondo_finisher["match_status"])
            self.assertEqual(8, q5573_mondo_finisher["datamapping_spawn_count"])
            self.assertEqual(
                "runtime_q5573_mondo_finisher_spawn_and_receiver_cache_tested_pending_dialog_smoke",
                q5573_mondo_finisher["evidence_status"],
            )
            self.assertIn("QuestIdReceive", q5573_mondo_finisher["blocker_summary"])
            self.assertIn("source_coordinate_id 15089", q5573_mondo_finisher["blocker_summary"])
            self.assertIn("completes achieved Q5573 only when Mondo is visible", q5573_mondo_finisher["blocker_summary"])
            self.assertFalse(audit.is_creature_evidence_blocker(q5573_mondo_finisher))
            q5575_kezrek_starter = next(row for row in rows if row["quest_id"] == 5575 and row["relation_type"] == "starter")
            self.assertEqual(2212, q5575_kezrek_starter["source_relation_id"])
            self.assertEqual(24158, q5575_kezrek_starter["creature2_id"])
            self.assertEqual("reviewed", q5575_kezrek_starter["match_status"])
            self.assertEqual(2, q5575_kezrek_starter["datamapping_spawn_count"])
            self.assertEqual("15181;170390", q5575_kezrek_starter["datamapping_source_coordinate_id_sample"])
            self.assertEqual(
                "runtime_q5575_kezrek_starter_spawn_and_giver_receiver_cache_tested_pending_dialog_smoke",
                q5575_kezrek_starter["evidence_status"],
            )
            self.assertIn("CrimsonIsleMapScript.cs", q5575_kezrek_starter["evidence_sources"])
            self.assertIn("GlobalQuestManagerPartialTableTests.cs", q5575_kezrek_starter["evidence_sources"])
            self.assertIn("directly carries Q5575 in QuestIdGiven and QuestIdReceive", q5575_kezrek_starter["blocker_summary"])
            self.assertIn("source_coordinate_id 15181", q5575_kezrek_starter["blocker_summary"])
            self.assertIn("Q5595QuestScript also grants Q5575", q5575_kezrek_starter["blocker_summary"])
            self.assertFalse(audit.is_creature_evidence_blocker(q5575_kezrek_starter))
            q5597_chua_objective = next(row for row in rows if row["quest_id"] == 5597 and row["relation_type"] == "objective" and row["source_relation_id"] == 1090)
            self.assertEqual(24286, q5597_chua_objective["creature2_id"])
            self.assertEqual("reviewed", q5597_chua_objective["match_status"])
            self.assertEqual(30, q5597_chua_objective["datamapping_spawn_count"])
            self.assertEqual("15489;15490;15491;15492;15493;15494;15857;15858;15859;15860;15861;15862;...+18", q5597_chua_objective["datamapping_source_coordinate_id_sample"])
            self.assertEqual(30, q5597_chua_objective["datamapping_unpromoted_spawn_count"])
            self.assertEqual("870", q5597_chua_objective["datamapping_spawn_worlds"])
            self.assertEqual("1218", q5597_chua_objective["datamapping_spawn_areas"])
            self.assertEqual(
                "runtime_q5597_chua_explosives_30_fallbacks_virtual_collect_guards_tested_external_video_observed_pending_local_client_ui_smoke",
                q5597_chua_objective["evidence_status"],
            )
            self.assertIn("CrimsonIsleMapScript.cs", q5597_chua_objective["evidence_sources"])
            self.assertIn("Q5597.cs", q5597_chua_objective["evidence_sources"])
            self.assertIn("CrimsonIsleBranchInteractionTests.cs", q5597_chua_objective["evidence_sources"])
            self.assertIn("30 reviewed Chua Explosives 24286 placements", q5597_chua_objective["blocker_summary"])
            self.assertIn("VirtualCollect data 364 count 6", q5597_chua_objective["blocker_summary"])
            self.assertIn("TargetGroup 4373", q5597_chua_objective["blocker_summary"])
            self.assertIn("Creature wrapper", q5597_chua_objective["blocker_summary"])
            self.assertIn("ignore missing quest state", q5597_chua_objective["blocker_summary"])
            self.assertIn("suppress repeated activation", q5597_chua_objective["blocker_summary"])
            self.assertIn("all 30 fallback activations", q5597_chua_objective["blocker_summary"])
            self.assertIn("activation/objective UI path is externally observed", q5597_chua_objective["blocker_summary"])
            self.assertIn("respawn/despawn visual-state smoke", q5597_chua_objective["blocker_summary"])
            self.assertIn("duplicate/negative activation/completion cases", q5597_chua_objective["blocker_summary"])
            self.assertIn("end-to-end Q5597->Q5604 smoke", q5597_chua_objective["blocker_summary"])
            q4526_pristan_starter = next(row for row in rows if row["quest_id"] == 4526 and row["relation_type"] == "starter")
            self.assertEqual(12560, q4526_pristan_starter["creature2_id"])
            self.assertEqual("reviewed", q4526_pristan_starter["match_status"])
            self.assertEqual("", q4526_pristan_starter["runtime_spawn_count"])
            self.assertEqual(1, q4526_pristan_starter["datamapping_spawn_count"])
            self.assertEqual("116070", q4526_pristan_starter["datamapping_source_coordinate_id_sample"])
            self.assertEqual(1, q4526_pristan_starter["datamapping_unpromoted_spawn_count"])
            self.assertEqual(
                "runtime_script_spawn_present_client_creature2_starter_relation_with_receiver_location_tested_pending_dialog_smoke",
                q4526_pristan_starter["evidence_status"],
            )
            self.assertIn("WorldLocation2.tbl.sql", q4526_pristan_starter["evidence_sources"])
            self.assertIn("NorthernWildsMapScript.cs", q4526_pristan_starter["evidence_sources"])
            self.assertIn("NorthernWildsBranchInteractionTests.cs", q4526_pristan_starter["evidence_sources"])
            self.assertIn("QuestIdGiven 4526", q4526_pristan_starter["blocker_summary"])
            self.assertIn("Quest2 receiver location 11669", q4526_pristan_starter["blocker_summary"])
            self.assertIn("source_coordinate_id 116070", q4526_pristan_starter["blocker_summary"])
            q4526_pristan_finisher = next(row for row in rows if row["quest_id"] == 4526 and row["relation_type"] == "finisher")
            self.assertEqual(12560, q4526_pristan_finisher["creature2_id"])
            self.assertEqual(
                "runtime_script_spawn_present_client_creature2_finisher_relation_with_receiver_location_tested_pending_dialog_smoke",
                q4526_pristan_finisher["evidence_status"],
            )
            self.assertIn("QuestIdReceive 4526", q4526_pristan_finisher["blocker_summary"])
            self.assertIn("completion dialog/activation smoke", q4526_pristan_finisher["blocker_summary"])
            q3486_panel_starter = next(row for row in rows if row["quest_id"] == 3486 and row["relation_type"] == "starter")
            self.assertEqual(11194, q3486_panel_starter["creature2_id"])
            self.assertEqual("reviewed", q3486_panel_starter["match_status"])
            self.assertEqual(1, q3486_panel_starter["datamapping_spawn_count"])
            self.assertEqual("2501", q3486_panel_starter["datamapping_source_coordinate_id_sample"])
            self.assertEqual(
                "runtime_q3486_master_control_panel_starter_relation_spawn_tested_pending_dialog_smoke",
                q3486_panel_starter["evidence_status"],
            )
            self.assertIn("creature_bridge_overrides.csv", q3486_panel_starter["evidence_sources"])
            self.assertIn("Quest2 receiver location 7778", q3486_panel_starter["blocker_summary"])
            self.assertIn("source_coordinate_id 2501", q3486_panel_starter["blocker_summary"])
            q3486_panel_finisher = next(row for row in rows if row["quest_id"] == 3486 and row["relation_type"] == "finisher")
            self.assertEqual(11194, q3486_panel_finisher["creature2_id"])
            self.assertEqual(
                "runtime_q3486_master_control_panel_finisher_relation_spawn_tested_pending_dialog_smoke",
                q3486_panel_finisher["evidence_status"],
            )
            self.assertIn("completion dialog/activation smoke", q3486_panel_finisher["blocker_summary"])
            self.assertIn("Q3486 follow-up handoff smoke", q3486_panel_finisher["blocker_summary"])
            q3486_guardian = next(row for row in rows if row["quest_id"] == 3486 and row["relation_type"] == "objective")
            self.assertEqual(11925, q3486_guardian["creature2_id"])
            self.assertEqual("scored_name", q3486_guardian["match_status"])
            self.assertEqual(5, q3486_guardian["datamapping_spawn_count"])
            self.assertEqual("7798992;7798993;7855831;7857737;8102237", q3486_guardian["datamapping_source_coordinate_id_sample"])
            self.assertEqual("426", q3486_guardian["datamapping_spawn_worlds"])
            self.assertEqual("729", q3486_guardian["datamapping_spawn_areas"])
            self.assertEqual(
                "runtime_q3486_crystal_guardian_objective_relation_spawn_and_virtual_collect_credit_tested_pending_client_smoke",
                q3486_guardian["evidence_status"],
            )
            self.assertIn("TargetGroup 4985", q3486_guardian["blocker_summary"])
            self.assertIn("virtual item 206", q3486_guardian["blocker_summary"])
            self.assertIn("source_coordinate_ids 7798992", q3486_guardian["blocker_summary"])
            self.assertIn("Frostbite spawn/drop smoke", q3486_guardian["blocker_summary"])
            q3741_crate = next(row for row in rows if row["quest_id"] == 3741 and row["relation_type"] == "objective")
            self.assertEqual(12919, q3741_crate["creature2_id"])
            self.assertEqual("reviewed", q3741_crate["match_status"])
            self.assertEqual(6, q3741_crate["datamapping_spawn_count"])
            self.assertEqual("1218;1219;1220;1221;1222;1223", q3741_crate["datamapping_source_coordinate_id_sample"])
            self.assertEqual("426", q3741_crate["datamapping_spawn_worlds"])
            self.assertEqual("597", q3741_crate["datamapping_spawn_areas"])
            self.assertEqual(
                "runtime_q3741_supply_crate_objective_relation_spawn_and_virtual_collect_credit_tested_pending_client_smoke",
                q3741_crate["evidence_status"],
            )
            self.assertIn("TargetGroup 4372", q3741_crate["blocker_summary"])
            self.assertIn("26 current reviewed Exile Supply Crate 12919 placements", q3741_crate["blocker_summary"])
            self.assertIn("source_coordinate_ids 1218, 1219, 1220", q3741_crate["blocker_summary"])
            self.assertIn("2634068", q3741_crate["blocker_summary"])
            self.assertIn("Land's Reach Durek fallback", q3741_crate["blocker_summary"])
            self.assertIn("Creature2 11066", q3741_crate["blocker_summary"])
            q3797_durek_starter = next(row for row in rows if row["quest_id"] == 3797 and row["relation_type"] == "starter")
            self.assertEqual(11066, q3797_durek_starter["creature2_id"])
            self.assertEqual(1, q3797_durek_starter["datamapping_spawn_count"])
            self.assertEqual("1763", q3797_durek_starter["datamapping_source_coordinate_id_sample"])
            self.assertEqual(
                "runtime_q3797_durek_starter_relation_spawn_and_giver_cache_tested_pending_dialog_smoke",
                q3797_durek_starter["evidence_status"],
            )
            self.assertIn("GlobalQuestManagerPartialTableTests.cs", q3797_durek_starter["evidence_sources"])
            self.assertIn("original 435/11061 bridge remains preserved as source-spawn context", q3797_durek_starter["blocker_summary"])
            self.assertIn("Creature2 11066 Land's Reach Commander Durek", q3797_durek_starter["blocker_summary"])
            self.assertIn("relation override 166", q3797_durek_starter["blocker_summary"])
            q3797_durek_finisher = next(row for row in rows if row["quest_id"] == 3797 and row["relation_type"] == "finisher")
            self.assertEqual(11066, q3797_durek_finisher["creature2_id"])
            self.assertEqual(
                "runtime_q3797_durek_finisher_relation_spawn_and_receiver_cache_tested_pending_dialog_smoke",
                q3797_durek_finisher["evidence_status"],
            )
            self.assertIn("Quest2 receiver WorldLocation2 7727", q3797_durek_finisher["blocker_summary"])
            self.assertIn("Creature2 11066 Land's Reach Commander Durek", q3797_durek_finisher["blocker_summary"])
            self.assertIn("original 435/11061 bridge remains preserved as source-spawn context", q3797_durek_finisher["blocker_summary"])
            self.assertIn("relation override 1693", q3797_durek_finisher["blocker_summary"])
            q3797_grimspore = next(row for row in rows if row["quest_id"] == 3797 and row["relation_type"] == "objective" and row["source_relation_id"] == 105)
            self.assertEqual(12212, q3797_grimspore["creature2_id"])
            self.assertEqual(4, q3797_grimspore["datamapping_spawn_count"])
            self.assertEqual("8272460;8272461;8272462;8272464", q3797_grimspore["datamapping_source_coordinate_id_sample"])
            self.assertEqual(
                "runtime_q3797_rootbrute_objective_relation_spawn_and_kill_credit_tested_pending_client_smoke",
                q3797_grimspore["evidence_status"],
            )
            self.assertIn("TargetGroup 1177", q3797_grimspore["blocker_summary"])
            self.assertIn("source_coordinate_ids 8272460", q3797_grimspore["blocker_summary"])
            q3797_snowstalker = next(row for row in rows if row["quest_id"] == 3797 and row["relation_type"] == "objective" and row["source_relation_id"] == 5204)
            self.assertEqual(11945, q3797_snowstalker["creature2_id"])
            self.assertEqual(
                "runtime_q3797_yeti_snowstalker_objective_relation_credit_tested_pending_q3797_spawn_review_client_smoke",
                q3797_snowstalker["evidence_status"],
            )
            self.assertIn("Q3797-specific Snowstalker placement", q3797_snowstalker["blocker_summary"])
            q3797_frostclaw = next(row for row in rows if row["quest_id"] == 3797 and row["relation_type"] == "objective" and row["source_relation_id"] == 106)
            self.assertEqual(11948, q3797_frostclaw["creature2_id"])
            self.assertEqual(
                "runtime_q3797_yeti_frostclaw_objective_relation_spawn_and_kill_credit_tested_pending_client_smoke",
                q3797_frostclaw["evidence_status"],
            )
            self.assertIn("source_coordinate_ids 8122314", q3797_frostclaw["blocker_summary"])
            q3797_deathcap = next(row for row in rows if row["quest_id"] == 3797 and row["relation_type"] == "objective" and row["source_relation_id"] == 3055)
            self.assertEqual(12213, q3797_deathcap["creature2_id"])
            self.assertEqual(2, q3797_deathcap["datamapping_spawn_count"])
            self.assertIn("3837530", q3797_deathcap["datamapping_source_coordinate_id_sample"])
            self.assertIn("3785147", q3797_deathcap["datamapping_source_coordinate_id_sample"])
            self.assertEqual(
                "runtime_q3797_rootbrute_deathcap_objective_relation_spawn_and_kill_credit_tested_pending_client_smoke",
                q3797_deathcap["evidence_status"],
            )
            q3670_deadeye = next(row for row in rows if row["quest_id"] == 3670 and row["relation_type"] == "finisher" and row["source_relation_id"] == 38)
            self.assertEqual(11063, q3670_deadeye["creature2_id"])
            self.assertEqual("reviewed", q3670_deadeye["match_status"])
            self.assertEqual("", q3670_deadeye["runtime_spawn_count"])
            self.assertEqual(3, q3670_deadeye["datamapping_spawn_count"])
            self.assertEqual("4368;4370;5142", q3670_deadeye["datamapping_source_coordinate_id_sample"])
            self.assertEqual(3, q3670_deadeye["datamapping_unpromoted_spawn_count"])
            self.assertEqual(
                "runtime_script_spawn_present_client_creature2_finisher_relation_with_receiver_cache_tested_pending_dialog_smoke",
                q3670_deadeye["evidence_status"],
            )
            self.assertIn("NorthernWildsMapScript.cs", q3670_deadeye["evidence_sources"])
            self.assertIn("NorthernWildsBranchInteractionTests.cs", q3670_deadeye["evidence_sources"])
            self.assertIn("GlobalQuestManager.cs", q3670_deadeye["evidence_sources"])
            self.assertIn("GlobalQuestManagerPartialTableTests.cs", q3670_deadeye["evidence_sources"])
            self.assertIn("QuestTests.cs", q3670_deadeye["evidence_sources"])
            self.assertIn("Deadeye Brightland", q3670_deadeye["blocker_summary"])
            self.assertIn("source_coordinate_id 5142", q3670_deadeye["blocker_summary"])
            self.assertIn("visible-receiver completion", q3670_deadeye["blocker_summary"])
            self.assertIn("receiver override for Creature2 11063", q3670_deadeye["blocker_summary"])
            q3671_deadeye_finisher = next(row for row in rows if row["quest_id"] == 3671 and row["relation_type"] == "finisher")
            self.assertEqual(11063, q3671_deadeye_finisher["creature2_id"])
            self.assertEqual("reviewed", q3671_deadeye_finisher["match_status"])
            self.assertEqual(3, q3671_deadeye_finisher["datamapping_spawn_count"])
            self.assertEqual("4368;4370;5142", q3671_deadeye_finisher["datamapping_source_coordinate_id_sample"])
            self.assertEqual(
                "runtime_script_spawn_present_client_creature2_finisher_relation_with_receiver_location_and_reward_tested_pending_dialog_smoke",
                q3671_deadeye_finisher["evidence_status"],
            )
            self.assertIn("Q3671.cs", q3671_deadeye_finisher["evidence_sources"])
            self.assertIn("Quest2Reward.tbl.sql", q3671_deadeye_finisher["evidence_sources"])
            self.assertIn("QuestTests.cs", q3671_deadeye_finisher["evidence_sources"])
            self.assertIn("QuestIdReceive 3671", q3671_deadeye_finisher["blocker_summary"])
            self.assertIn("Quest2 receiver location 9340", q3671_deadeye_finisher["blocker_summary"])
            self.assertIn("source_coordinate_id 4368", q3671_deadeye_finisher["blocker_summary"])
            self.assertIn("reward row 6662 granting item 19131", q3671_deadeye_finisher["blocker_summary"])
            q3671_deadeye_starter = next(row for row in rows if row["quest_id"] == 3671 and row["relation_type"] == "starter")
            self.assertEqual(11063, q3671_deadeye_starter["creature2_id"])
            self.assertEqual(
                "runtime_script_spawn_present_client_creature2_starter_relation_with_receiver_location_tested_pending_dialog_smoke",
                q3671_deadeye_starter["evidence_status"],
            )
            self.assertIn("QuestIdGiven 3671", q3671_deadeye_starter["blocker_summary"])
            self.assertIn("Q3486/Q3671/Q3668 chain smoke", q3671_deadeye_starter["blocker_summary"])
            q3668_deadeye_starter = next(row for row in rows if row["quest_id"] == 3668 and row["relation_type"] == "starter")
            self.assertEqual(11063, q3668_deadeye_starter["creature2_id"])
            self.assertEqual(
                "runtime_q3668_deadeye_starter_relation_spawn_and_followup_chain_tested_pending_dialog_smoke",
                q3668_deadeye_starter["evidence_status"],
            )
            self.assertIn("QuestIdGiven 3668", q3668_deadeye_starter["blocker_summary"])
            self.assertIn("Q3486/Q3671/Q3668 chain smoke", q3668_deadeye_starter["blocker_summary"])
            q3668_bartol = next(row for row in rows if row["quest_id"] == 3668 and row["relation_type"] == "finisher")
            self.assertEqual(12737, q3668_bartol["creature2_id"])
            self.assertEqual(1, q3668_bartol["datamapping_spawn_count"])
            self.assertEqual("159", q3668_bartol["datamapping_source_coordinate_id_sample"])
            self.assertEqual(
                "runtime_q3668_bartol_finisher_relation_spawn_tested_pending_dialog_smoke",
                q3668_bartol["evidence_status"],
            )
            self.assertIn("Quest2 receiver location 9121", q3668_bartol["blocker_summary"])
            self.assertIn("source_coordinate_id 159", q3668_bartol["blocker_summary"])
            q3668_lusk = next(row for row in rows if row["quest_id"] == 3668 and row["relation_type"] == "objective" and row["source_relation_id"] == 43)
            self.assertEqual(12484, q3668_lusk["creature2_id"])
            self.assertEqual("4272", q3668_lusk["datamapping_source_coordinate_id_sample"])
            self.assertEqual(
                "runtime_q3668_lusk_objective_relation_spawn_and_talk_credit_tested_pending_client_smoke",
                q3668_lusk["evidence_status"],
            )
            self.assertIn("TalkTo data 12484", q3668_lusk["blocker_summary"])
            q3668_survivor = next(row for row in rows if row["quest_id"] == 3668 and row["relation_type"] == "objective" and row["source_relation_id"] == 40)
            self.assertEqual(14124, q3668_survivor["creature2_id"])
            self.assertEqual(2, q3668_survivor["datamapping_spawn_count"])
            self.assertEqual("2622;5228", q3668_survivor["datamapping_source_coordinate_id_sample"])
            self.assertEqual(
                "runtime_q3668_imprisoned_survivor_objective_relation_spawn_and_activate_credit_tested_pending_client_smoke",
                q3668_survivor["evidence_status"],
            )
            self.assertIn("TargetGroup 7261", q3668_survivor["blocker_summary"])
            q3668_duplicate_survivor = next(row for row in rows if row["quest_id"] == 3668 and row["source_relation_id"] == 6811)
            self.assertEqual(14124, q3668_duplicate_survivor["creature2_id"])
            self.assertIn("later duplicate", q3668_duplicate_survivor["blocker_summary"])
            q3668_diviner = next(row for row in rows if row["quest_id"] == 3668 and row["source_relation_id"] == 6810)
            self.assertEqual(11907, q3668_diviner["creature2_id"])
            self.assertEqual("7766435", q3668_diviner["datamapping_source_coordinate_id_sample"])
            self.assertEqual(
                "runtime_q3668_skeech_objective_relation_spawn_and_kill_credit_tested_pending_client_smoke",
                q3668_diviner["evidence_status"],
            )
            self.assertIn("TargetGroup 960", q3668_diviner["blocker_summary"])
            self.assertIn("objective 4791", q3668_diviner["blocker_summary"])
            q3668_fierce_scratcher = next(row for row in rows if row["quest_id"] == 3668 and row["source_relation_id"] == 3053)
            self.assertEqual(36429, q3668_fierce_scratcher["creature2_id"])
            self.assertIn("source_coordinate_id 6216334", q3668_fierce_scratcher["blocker_summary"])
            self.assertIn("TargetGroup member 14054", q3668_fierce_scratcher["blocker_summary"])
            q3668_xenobreeder = next(row for row in rows if row["quest_id"] == 3668 and row["source_relation_id"] == 6918)
            self.assertEqual(36884, q3668_xenobreeder["creature2_id"])
            self.assertEqual("5594149", q3668_xenobreeder["datamapping_source_coordinate_id_sample"])
            q3479_deadeye = next(row for row in rows if row["quest_id"] == 3479 and row["relation_type"] == "finisher" and row["source_relation_id"] == 101)
            self.assertEqual(11063, q3479_deadeye["creature2_id"])
            self.assertEqual("reviewed", q3479_deadeye["match_status"])
            self.assertEqual(
                "runtime_q3479_deadeye_finisher_relation_spawn_and_receiver_cache_tested_external_video_dialog_observed_pending_local_dialog_smoke",
                q3479_deadeye["evidence_status"],
            )
            self.assertIn("VIDEO_EVIDENCE_MAP.md", q3479_deadeye["evidence_sources"])
            self.assertIn("NorthernWildsMapScript.cs", q3479_deadeye["evidence_sources"])
            self.assertIn("GlobalQuestManagerPartialTableTests.cs", q3479_deadeye["evidence_sources"])
            self.assertIn("Quest2 routes Q3479 to receiver WorldLocation2 7726", q3479_deadeye["blocker_summary"])
            self.assertIn("source_coordinate_id 4370", q3479_deadeye["blocker_summary"])
            self.assertIn("indexes Creature2 11063 as the Q3479 receiver", q3479_deadeye["blocker_summary"])
            self.assertIn("Deadeye completion dialog", q3479_deadeye["blocker_summary"])
            q3480_deadeye = next(row for row in rows if row["quest_id"] == 3480 and row["relation_type"] == "finisher" and row["source_relation_id"] == 579)
            self.assertEqual(11063, q3480_deadeye["creature2_id"])
            self.assertEqual(
                "runtime_q3480_deadeye_finisher_relation_spawn_and_receiver_cache_tested_external_video_dialog_observed_pending_local_dialog_smoke",
                q3480_deadeye["evidence_status"],
            )
            self.assertIn("VIDEO_EVIDENCE_MAP.md", q3480_deadeye["evidence_sources"])
            self.assertIn("Deadeye completion dialog/reward panel", q3480_deadeye["blocker_summary"])
            self.assertIn("Durek starter activation smoke", q3480_deadeye["blocker_summary"])
            self.assertIn("indexes Creature2 11063 as the Q3480 receiver", q3480_deadeye["blocker_summary"])
            q3480_durek = next(row for row in rows if row["quest_id"] == 3480 and row["relation_type"] == "starter" and row["source_relation_id"] == 821)
            self.assertEqual(11061, q3480_durek["creature2_id"])
            self.assertEqual(
                "runtime_q3480_commander_durek_starter_relation_spawn_and_giver_cache_tested_external_video_accept_observed_pending_local_dialog_smoke",
                q3480_durek["evidence_status"],
            )
            self.assertIn("VIDEO_EVIDENCE_MAP.md", q3480_durek["evidence_sources"])
            self.assertIn("source_coordinate_id 40899", q3480_durek["blocker_summary"])
            self.assertIn("Commander Durek offering Reporting for Duty", q3480_durek["blocker_summary"])
            self.assertIn("source_relation_id 2133 is recorded separately as stale/replaced context", q3480_durek["blocker_summary"])
            q3480_unknown_starter = next(row for row in rows if row["quest_id"] == 3480 and row["relation_type"] == "starter" and row["source_relation_id"] == 2133)
            self.assertEqual(
                "datamapping_q3480_stale_unknown_starter_relation_replaced_by_reviewed_durek_starter",
                q3480_unknown_starter["evidence_status"],
            )
            self.assertEqual("reviewed_stale_replaced_by_current_starter", q3480_unknown_starter["match_status"])
            self.assertIn("source_relation_id 821", q3480_unknown_starter["blocker_summary"])
            self.assertIn("Treat relation 2133 as stale/replaced DataMapping context", q3480_unknown_starter["blocker_summary"])
            q3479_survivor = next(row for row in rows if row["quest_id"] == 3479 and row["relation_type"] == "objective" and row["source_relation_id"] == 217)
            self.assertEqual(11070, q3479_survivor["creature2_id"])
            self.assertEqual("unique_name", q3479_survivor["match_status"])
            self.assertEqual(16, q3479_survivor["datamapping_spawn_count"])
            self.assertEqual(
                "3406;3407;3408;15032;17868;40681;40682;40683;41559;42765;114996;114997;...+4",
                q3479_survivor["datamapping_source_coordinate_id_sample"],
            )
            self.assertEqual("426", q3479_survivor["datamapping_spawn_worlds"])
            self.assertEqual("646", q3479_survivor["datamapping_spawn_areas"])
            self.assertEqual(
                "runtime_q3479_trapped_survivor_objective_relation_spawn_and_csi_credit_tested_external_video_progress_observed_pending_local_client_smoke",
                q3479_survivor["evidence_status"],
            )
            self.assertIn("VIDEO_EVIDENCE_MAP.md", q3479_survivor["evidence_sources"])
            self.assertIn("NorthernWildsMapScript.cs", q3479_survivor["evidence_sources"])
            self.assertIn("InteractionObjectiveUpdaterTests.cs", q3479_survivor["evidence_sources"])
            self.assertIn("16 unique-name-mapped Trapped Survivor 11070 placements", q3479_survivor["blocker_summary"])
            self.assertIn("Survivor progress in the tracker", q3479_survivor["blocker_summary"])
            self.assertIn("source_coordinate_ids 3406, 3407, 3408, 15032", q3479_survivor["blocker_summary"])
            self.assertIn("shared yeti kill objective has separate runtime spawn/credit coverage", q3479_survivor["blocker_summary"])
            q3480_survivor = next(row for row in rows if row["quest_id"] == 3480 and row["relation_type"] == "objective" and row["source_relation_id"] == 2131)
            self.assertEqual(11070, q3480_survivor["creature2_id"])
            self.assertEqual(
                "runtime_q3480_trapped_survivor_objective_relation_spawn_and_csi_credit_tested_external_video_progress_observed_pending_local_client_smoke",
                q3480_survivor["evidence_status"],
            )
            self.assertIn("VIDEO_EVIDENCE_MAP.md", q3480_survivor["evidence_sources"])
            self.assertIn("Q3480 Survivor progress in the tracker", q3480_survivor["blocker_summary"])
            self.assertIn("shared yeti kill objective has separate runtime spawn/credit coverage", q3480_survivor["blocker_summary"])
            q3479_snowstalker = next(row for row in rows if row["quest_id"] == 3479 and row["source_relation_id"] == 216)
            self.assertEqual(11945, q3479_snowstalker["creature2_id"])
            self.assertEqual("ambiguous_name", q3479_snowstalker["match_status"])
            self.assertEqual(4, q3479_snowstalker["datamapping_spawn_count"])
            self.assertEqual("7915699;7915700;7956687;8401261", q3479_snowstalker["datamapping_source_coordinate_id_sample"])
            self.assertEqual("426", q3479_snowstalker["datamapping_spawn_worlds"])
            self.assertEqual("651", q3479_snowstalker["datamapping_spawn_areas"])
            self.assertEqual(
                "runtime_q3479_yeti_snowstalker_objective_relation_spawn_and_kill_credit_tested_external_video_combat_observed_pending_local_client_smoke",
                q3479_snowstalker["evidence_status"],
            )
            self.assertIn("VIDEO_EVIDENCE_MAP.md", q3479_snowstalker["evidence_sources"])
            self.assertIn("NorthernWildsMapScript.cs", q3479_snowstalker["evidence_sources"])
            self.assertIn("QuestTests.cs", q3479_snowstalker["evidence_sources"])
            self.assertIn("TargetGroup 7288", q3479_snowstalker["blocker_summary"])
            self.assertIn("visibly names Yeti Snowstalker", q3479_snowstalker["blocker_summary"])
            self.assertIn("source_coordinate_ids 7915699", q3479_snowstalker["blocker_summary"])
            self.assertIn("live/client combat kill smoke", q3479_snowstalker["blocker_summary"])
            q3479_frostclaw = next(row for row in rows if row["quest_id"] == 3479 and row["source_relation_id"] == 218)
            self.assertEqual(11948, q3479_frostclaw["creature2_id"])
            self.assertEqual("scored_name", q3479_frostclaw["match_status"])
            self.assertEqual(4, q3479_frostclaw["datamapping_spawn_count"])
            self.assertEqual("8110657;8158349;8204800;8251657", q3479_frostclaw["datamapping_source_coordinate_id_sample"])
            self.assertEqual("426", q3479_frostclaw["datamapping_spawn_worlds"])
            self.assertEqual("597", q3479_frostclaw["datamapping_spawn_areas"])
            self.assertEqual(
                "runtime_q3479_q3480_shared_yeti_objective_relation_spawn_and_kill_credit_tested_pending_client_smoke",
                q3479_frostclaw["evidence_status"],
            )
            self.assertIn("TargetGroup 7288", q3479_frostclaw["blocker_summary"])
            self.assertIn("source_coordinate_ids 8110657", q3479_frostclaw["blocker_summary"])
            q3479_fierce_yeti = next(row for row in rows if row["quest_id"] == 3479 and row["source_relation_id"] == 3449)
            self.assertEqual(36331, q3479_fierce_yeti["creature2_id"])
            self.assertEqual("scored_name", q3479_fierce_yeti["match_status"])
            self.assertEqual(
                "runtime_q3479_fierce_yeti_relation_spawn_present_mapped_only_objective_credit_blocked",
                q3479_fierce_yeti["evidence_status"],
            )
            self.assertIn("NorthernWildsMapScript.cs", q3479_fierce_yeti["evidence_sources"])
            self.assertIn("four area-651 placements", q3479_fierce_yeti["blocker_summary"])
            self.assertIn("TargetGroup 1463 to Creature2 11945 and 11948 only", q3479_fierce_yeti["blocker_summary"])
            self.assertIn("mapped-only spawn/context evidence", q3479_fierce_yeti["blocker_summary"])
            self.assertFalse(audit.is_creature_evidence_blocker(q3479_fierce_yeti))
            q3479_bosun = next(row for row in rows if row["quest_id"] == 3479 and row["relation_type"] == "starter" and row["source_relation_id"] == 150)
            self.assertEqual(11062, q3479_bosun["creature2_id"])
            self.assertEqual("reviewed", q3479_bosun["match_status"])
            self.assertEqual(
                "runtime_q3479_bosun_starter_relation_spawn_tested_external_video_accept_observed_pending_local_dialog_smoke",
                q3479_bosun["evidence_status"],
            )
            self.assertIn("VIDEO_EVIDENCE_MAP.md", q3479_bosun["evidence_sources"])
            self.assertIn("source_coordinate_id 2956", q3479_bosun["blocker_summary"])
            self.assertIn("Bosun Redmark offering From the Wreckage", q3479_bosun["blocker_summary"])
            self.assertIn("Bosun accept dialog", q3479_bosun["blocker_summary"])
            q3480_snowstalker = next(row for row in rows if row["quest_id"] == 3480 and row["source_relation_id"] == 2129)
            self.assertEqual(11945, q3480_snowstalker["creature2_id"])
            self.assertEqual(
                "runtime_q3479_q3480_shared_yeti_objective_relation_spawn_and_kill_credit_tested_pending_client_smoke",
                q3480_snowstalker["evidence_status"],
            )
            self.assertIn("TargetGroup 1463", q3480_snowstalker["blocker_summary"])
            self.assertIn("source_coordinate_ids 7915699", q3480_snowstalker["blocker_summary"])
            q3480_frostclaw = next(row for row in rows if row["quest_id"] == 3480 and row["source_relation_id"] == 2130)
            self.assertEqual(11948, q3480_frostclaw["creature2_id"])
            self.assertEqual(
                "runtime_q3479_q3480_shared_yeti_objective_relation_spawn_and_kill_credit_tested_pending_client_smoke",
                q3480_frostclaw["evidence_status"],
            )
            self.assertIn("TargetGroup 1463", q3480_frostclaw["blocker_summary"])
            self.assertIn("source_coordinate_ids 8110657", q3480_frostclaw["blocker_summary"])
            q3480_fierce_yeti = next(row for row in rows if row["quest_id"] == 3480 and row["source_relation_id"] == 3419)
            self.assertEqual(36331, q3480_fierce_yeti["creature2_id"])
            self.assertEqual("scored_name", q3480_fierce_yeti["match_status"])
            self.assertEqual(
                "runtime_q3480_fierce_yeti_relation_spawn_present_mapped_only_objective_credit_blocked",
                q3480_fierce_yeti["evidence_status"],
            )
            self.assertIn("NorthernWildsMapScript.cs", q3480_fierce_yeti["evidence_sources"])
            self.assertIn("four area-651 placements", q3480_fierce_yeti["blocker_summary"])
            self.assertIn("QuestTests coverage exclude Creature2 36331", q3480_fierce_yeti["blocker_summary"])
            self.assertIn("mapped-only spawn/context evidence", q3480_fierce_yeti["blocker_summary"])
            self.assertFalse(audit.is_creature_evidence_blocker(q3480_fierce_yeti))
            q3670_galeras_deadeye = next(row for row in rows if row["quest_id"] == 3670 and row["relation_type"] == "finisher" and row["source_relation_id"] == 1492)
            self.assertEqual(16622, q3670_galeras_deadeye["creature2_id"])
            self.assertEqual("reviewed", q3670_galeras_deadeye["match_status"])
            self.assertEqual(1, q3670_galeras_deadeye["runtime_spawn_count"])
            self.assertEqual(2, q3670_galeras_deadeye["datamapping_spawn_count"])
            self.assertEqual("837;2587271", q3670_galeras_deadeye["datamapping_source_coordinate_id_sample"])
            self.assertEqual(1, q3670_galeras_deadeye["runtime_datamapping_position_match_count"])
            self.assertEqual("2587271", q3670_galeras_deadeye["runtime_matched_source_coordinate_ids"])
            self.assertEqual("1002587271", q3670_galeras_deadeye["runtime_matched_entity_ids"])
            self.assertEqual(1, q3670_galeras_deadeye["datamapping_unpromoted_spawn_count"])
            self.assertEqual("837", q3670_galeras_deadeye["datamapping_unpromoted_source_coordinate_ids"])
            self.assertEqual(
                "runtime_script_spawn_present_client_creature2_alt_finisher_relation_with_receiver_cache_tested_pending_dialog_smoke",
                q3670_galeras_deadeye["evidence_status"],
            )
            self.assertIn("GalerasMapScript.cs", q3670_galeras_deadeye["evidence_sources"])
            self.assertIn("GalerasMapScriptTests.cs", q3670_galeras_deadeye["evidence_sources"])
            self.assertIn("GlobalQuestManagerPartialTableTests.cs", q3670_galeras_deadeye["evidence_sources"])
            self.assertIn("source_coordinate_id 837", q3670_galeras_deadeye["blocker_summary"])
            self.assertIn("source_coordinate_id 2587271", q3670_galeras_deadeye["blocker_summary"])
            self.assertIn("receiver override for Creature2 16622", q3670_galeras_deadeye["blocker_summary"])
            self.assertIn("prerequisite-selection UI smoke", q3670_galeras_deadeye["blocker_summary"])
            q3963_galeras_deadeye = next(row for row in rows if row["quest_id"] == 3963 and row["relation_type"] == "finisher" and row["source_relation_id"] == 96)
            self.assertEqual(16622, q3963_galeras_deadeye["creature2_id"])
            self.assertEqual("reviewed", q3963_galeras_deadeye["match_status"])
            self.assertEqual(1, q3963_galeras_deadeye["runtime_spawn_count"])
            self.assertEqual(2, q3963_galeras_deadeye["datamapping_spawn_count"])
            self.assertEqual("837;2587271", q3963_galeras_deadeye["datamapping_source_coordinate_id_sample"])
            self.assertEqual(
                "runtime_q3963_deadeye_finisher_relation_spawn_and_receiver_cache_tested_pending_dialog_smoke",
                q3963_galeras_deadeye["evidence_status"],
            )
            self.assertIn("GalerasMapScript.cs", q3963_galeras_deadeye["evidence_sources"])
            self.assertIn("GlobalQuestManagerPartialTableTests.cs", q3963_galeras_deadeye["evidence_sources"])
            self.assertIn("QuestTests.cs", q3963_galeras_deadeye["evidence_sources"])
            self.assertIn("Creature2 16622 carries QuestIdReceive 3963", q3963_galeras_deadeye["blocker_summary"])
            self.assertIn("source_coordinate_id 837", q3963_galeras_deadeye["blocker_summary"])
            self.assertIn("source_coordinate_id 2587271", q3963_galeras_deadeye["blocker_summary"])
            self.assertIn("standard client-table indexing caches 16622", q3963_galeras_deadeye["blocker_summary"])
            q3963_deadeye_starter = next(row for row in rows if row["quest_id"] == 3963 and row["relation_type"] == "starter" and row["source_relation_id"] == 1621)
            self.assertEqual(11063, q3963_deadeye_starter["creature2_id"])
            self.assertEqual("reviewed", q3963_deadeye_starter["match_status"])
            self.assertEqual(
                "client_q3963_starter_relation_mapped_to_followup_handoff_pending_visible_starter_proof",
                q3963_deadeye_starter["evidence_status"],
            )
            self.assertIn("Q3487Shellshock.cs", q3963_deadeye_starter["evidence_sources"])
            self.assertIn("does not expose Q3963 in QuestIdGiven", q3963_deadeye_starter["blocker_summary"])
            self.assertIn("Q3487 FollowUpQuestScript handoff", q3963_deadeye_starter["blocker_summary"])
            self.assertIn("Q3963 completion route is the client-table-backed 16622 receiver", q3963_deadeye_starter["blocker_summary"])
            q3963_unknown_starter = next(row for row in rows if row["quest_id"] == 3963 and row["relation_type"] == "starter" and row["source_relation_id"] == 2130)
            self.assertEqual(
                "datamapping_q3963_stale_unknown_starter_relation_replaced_by_followup_handoff_context",
                q3963_unknown_starter["evidence_status"],
            )
            self.assertEqual("reviewed_stale_replaced_by_followup_handoff_context", q3963_unknown_starter["match_status"])
            self.assertIn("source creature 23645 (Unknown)", q3963_unknown_starter["blocker_summary"])
            self.assertIn("Q3487 FollowUpQuestScript handoff", q3963_unknown_starter["blocker_summary"])
            self.assertIn("separate relation 1621 row still tracks", q3963_unknown_starter["blocker_summary"])
            q3673_deadeye_starter = next(row for row in rows if row["quest_id"] == 3673 and row["relation_type"] == "starter")
            self.assertEqual(11063, q3673_deadeye_starter["creature2_id"])
            self.assertEqual("reviewed", q3673_deadeye_starter["match_status"])
            self.assertEqual(
                "runtime_script_spawn_present_client_creature2_starter_relation_tested_pending_dialog_smoke",
                q3673_deadeye_starter["evidence_status"],
            )
            self.assertIn("NorthernWildsMapScript.cs", q3673_deadeye_starter["evidence_sources"])
            self.assertIn("NorthernWildsQuestChainTests.cs", q3673_deadeye_starter["evidence_sources"])
            self.assertIn("QuestIdReceive 3673", q3673_deadeye_starter["blocker_summary"])
            self.assertIn("prerequisite flow from Q3886", q3673_deadeye_starter["blocker_summary"])
            q3673_deadeye_finisher = next(row for row in rows if row["quest_id"] == 3673 and row["relation_type"] == "finisher")
            self.assertEqual(
                "runtime_script_spawn_present_client_creature2_finisher_relation_tested_pending_dialog_smoke",
                q3673_deadeye_finisher["evidence_status"],
            )
            self.assertIn("completion dialog", q3673_deadeye_finisher["blocker_summary"])
            q3673_unknown_finisher = next(row for row in rows if row["quest_id"] == 3673 and row["relation_type"] == "finisher" and row["source_relation_id"] == 1842)
            self.assertEqual(
                "datamapping_q3673_stale_unknown_finisher_relation_replaced_by_reviewed_deadeye_finisher",
                q3673_unknown_finisher["evidence_status"],
            )
            self.assertEqual("reviewed_stale_replaced_by_current_finisher", q3673_unknown_finisher["match_status"])
            self.assertIn("source_relation_id 40", q3673_unknown_finisher["blocker_summary"])
            self.assertIn("Treat relation 1842 as stale/replaced DataMapping context", q3673_unknown_finisher["blocker_summary"])
            q3673_signal_flare = next(row for row in rows if row["quest_id"] == 3673 and row["relation_type"] == "objective")
            self.assertEqual(12521, q3673_signal_flare["creature2_id"])
            self.assertEqual("client_creature2_target_group_reviewed", q3673_signal_flare["match_status"])
            self.assertEqual(3, q3673_signal_flare["datamapping_spawn_count"])
            self.assertEqual("2960;2961;2962", q3673_signal_flare["datamapping_source_coordinate_id_sample"])
            self.assertEqual(
                "runtime_script_spawn_present_client_creature2_objective_relation_with_signal_flare_credit_tests_pending_client_smoke",
                q3673_signal_flare["evidence_status"],
            )
            self.assertIn("TargetGroup.tbl.sql", q3673_signal_flare["evidence_sources"])
            self.assertIn("InteractionObjectiveUpdaterTests.cs", q3673_signal_flare["evidence_sources"])
            self.assertIn("Creature2 12521, 13150, and 13151", q3673_signal_flare["blocker_summary"])
            self.assertIn("world locations 8867, 8868, and 8869", q3673_signal_flare["blocker_summary"])
            self.assertTrue(all(row["completion_status"] == "not_retail_complete" for row in rows))
        finally:
            shutil.rmtree(csv_root, ignore_errors=True)

    def test_runtime_owned_creature_relation_rows_do_not_block_on_unpromoted_spawns(self) -> None:
        exempted_relations = [
            (3479, "starter", 150),
            (3479, "finisher", 101),
            (3479, "objective", 216),
            (3479, "objective", 217),
            (3479, "objective", 218),
            (3479, "objective", 3449),
            (3480, "starter", 821),
            (3480, "starter", 2133),
            (3480, "finisher", 579),
            (3480, "objective", 2129),
            (3480, "objective", 2130),
            (3480, "objective", 3419),
            (3480, "objective", 2131),
            (3486, "starter", 155),
            (3486, "finisher", 105),
            (3486, "objective", 139),
            (3668, "starter", 47),
            (3668, "finisher", 37),
            (3668, "objective", 39),
            (3668, "objective", 40),
            (3668, "objective", 41),
            (3668, "objective", 42),
            (3668, "objective", 43),
            (3668, "objective", 44),
            (3668, "objective", 3053),
            (3668, "objective", 3054),
            (3668, "objective", 6808),
            (3668, "objective", 6809),
            (3668, "objective", 6810),
            (3668, "objective", 6811),
            (3668, "objective", 6813),
            (3668, "objective", 6918),
            (3673, "starter", 49),
            (3673, "finisher", 40),
            (3673, "finisher", 1842),
            (3673, "objective", 405),
            (3886, "starter", 1231),
            (3886, "finisher", 102),
            (3886, "objective", 343),
            (3886, "objective", 344),
            (3886, "objective", 8506),
            (3797, "starter", 166),
            (3797, "finisher", 1693),
            (3797, "objective", 105),
            (3797, "objective", 106),
            (3797, "objective", 3055),
            (3963, "finisher", 96),
            (3963, "objective", 445),
            (3963, "starter", 2130),
            (5573, "starter", 1919),
            (5573, "finisher", 1558),
            (5573, "objective", 1065),
            (5575, "objective", 2561),
            (5575, "starter", 2212),
            (5580, "starter", 465),
            (5580, "finisher", 1559),
            (5580, "objective", 1053),
            (5597, "starter", 1922),
            (5597, "finisher", 1564),
            (5597, "objective", 1090),
        ]

        for quest_id, relation_type, source_relation_id in exempted_relations:
            with self.subTest(quest_id=quest_id, relation_type=relation_type, source_relation_id=source_relation_id):
                self.assertFalse(
                    audit.is_creature_evidence_blocker(
                        {
                            "quest_id": str(quest_id),
                            "quest_link_status": "current_quest",
                            "completion_status": "not_retail_complete",
                            "relation_type": relation_type,
                            "source_relation_id": str(source_relation_id),
                            "match_status": "scored_name",
                            "datamapping_unpromoted_spawn_count": "99",
                            "evidence_status": f"runtime_q{quest_id}_relation_spawn_and_credit_tested_pending_client_smoke",
                        }
                    )
                )

        self.assertTrue(
            audit.is_creature_evidence_blocker(
                {
                    "quest_id": "3668",
                    "quest_link_status": "current_quest",
                    "completion_status": "not_retail_complete",
                    "relation_type": "objective",
                    "source_relation_id": "9999",
                    "match_status": "scored_name",
                    "datamapping_unpromoted_spawn_count": "99",
                    "evidence_status": "runtime_q3668_skeech_objective_relation_spawn_and_kill_credit_tested_pending_client_smoke",
                }
            )
        )
        self.assertTrue(
            audit.is_creature_evidence_blocker(
                {
                    "quest_id": "3963",
                    "quest_link_status": "current_quest",
                    "completion_status": "not_retail_complete",
                    "relation_type": "starter",
                    "source_relation_id": "1621",
                    "match_status": "reviewed",
                    "datamapping_unpromoted_spawn_count": "3",
                    "evidence_status": "client_q3963_starter_relation_mapped_to_followup_handoff_pending_visible_starter_proof",
                }
            )
        )
        self.assertTrue(
            audit.is_creature_evidence_blocker(
                {
                    "quest_id": "3797",
                    "quest_link_status": "current_quest",
                    "completion_status": "not_retail_complete",
                    "relation_type": "objective",
                    "source_relation_id": "5204",
                    "match_status": "reviewed",
                    "datamapping_unpromoted_spawn_count": "104",
                    "evidence_status": "runtime_q3797_yeti_snowstalker_objective_relation_credit_tested_pending_q3797_spawn_review_client_smoke",
                }
            )
        )

    def test_quest_objective_evidence_rows_cover_matched_unmatched_and_noncurrent_rows(self) -> None:
        csv_root = Path(tempfile.mkdtemp(prefix="nf-content-audit-objectives-"))
        try:
            objective_map = csv_root / "quest_objective_map.csv"
            objective_map.write_text(
                "\n".join(
                    [
                        "jabbithole_quest_objective_id,jabbithole_quest_id,quest2_id,quest_name,client_quest_name,objective_order,quest_objective_id,match_status,jabbithole_objective_text,client_objective_text,objective_type,objective_data,objective_count,quest_direction_id,world_location_0,world_location_1,world_location_2,world_location_3,last_seen_in",
                        "1,1000,10,Quest Title,Quest Title,0,300,matched,Do the thing,Do the thing,2,500,3,800,900,,,,7",
                        "2,1000,10,Quest Title,Quest Title,1,,unmatched,Missing objective,,,,,,,,,,7",
                        "3,1001,9999,Old Quest,,0,0,unmatched,Old objective,,,,,,,,,,7",
                        "418,227,3479,From the Wreckage,From the Wreckage,0,,unmatched,Rescue Trapped Survivors north of the crash site,,,,,,,,,,7",
                        "419,227,3479,From the Wreckage,From the Wreckage,1,4564,matched,Kill yeti on your way to the Ancient Tower,Kill yeti on your way to the Ancient Tower,8,7288,8,0,12155,12156,12157,12158,7",
                        "2336,1454,3480,Reporting for Duty,Reporting for Duty,0,,unmatched,Rescue Trapped Survivors north of the crash site,,,,,,,,,,7",
                        "2337,1454,3480,Reporting for Duty,Reporting for Duty,1,4565,matched,Kill yeti on your way to the Ancient Tower,Kill yeti on your way to the Ancient Tower,8,1463,8,0,12155,12156,12157,12158,7",
                        "436,234,3486,Empowered Tower,Empowered Tower,0,,unmatched,Follow the power cables that lead north of the Ancient Tower,,,,,,,,,,7",
                        "437,234,3486,Empowered Tower,Empowered Tower,1,4485,matched,Collect Loftite Crystal Fragments outside Exo-Lab 729 by jumping into them or by destroying Crystal Guardians,Collect $m(vitem=206) outside Exo-Lab 729 by jumping into them or by destroying $m(creature=11925),32,206,5,0,7807,0,0,0,7",
                        "410,220,3777,By Leaps and Bounds,By Leaps and Bounds,0,,unmatched,Travel to the Loftite Cliffs,,,,,,,,,,7",
                        "411,220,3777,By Leaps and Bounds,By Leaps and Bounds,1,5076,matched,Collect a Pure Loftite Fragment from the Loftite Cliffs by jumping through it,Collect a $(item=6998) from the Loftite Cliffs by jumping through it,5,6952,1,0,9578,0,0,0,1",
                        "412,220,3777,By Leaps and Bounds,By Leaps and Bounds,2,4859,matched,Collect Pure Loftite Fragments for Denner Hazefall,Collect $m(item=6998) for $(creature=15759),4,6998,8,0,9578,0,0,0,1",
                        "1453,944,5596,Ordnance Recovery,Ordnance Recovery,0,,unmatched,Find the Blimp Crash Site near the Forward Operating Base,,,,,,,,,,3",
                        "1454,944,5596,Ordnance Recovery,Ordnance Recovery,1,8468,matched,Search for Dead Dominion Demolitions Experts at Crash Site Alpha,Search for Dead $m(creature=24703) at Crash Site Alpha,14,2619,3,0,17853,17852,0,0,3",
                        "132,67,3668,Indigenous Intelligence,Indigenous Intelligence,0,,unmatched,Find Scientist Lusk at the Skeech camp outside Settler's Reach,,,,,,,,,,7",
                        "133,67,3668,Indigenous Intelligence,Indigenous Intelligence,1,4745,matched,Rescue Exile Prisoners inside Coldburrow Cavern,Rescue Exile Prisoners inside Coldburrow Cavern,5,0,5,254,8858,0,0,0,7",
                        "134,67,3668,Indigenous Intelligence,Indigenous Intelligence,2,4791,matched,Kill Skeech inside Coldburrow Cavern,Kill Skeech inside Coldburrow Cavern,8,7293,5,254,8858,8859,0,0,7",
                        "137,70,3673,Contact with Thayd,Contact with Thayd,0,,unmatched,Light the Signal Flares at the landing site outside Settler's Reach,,,,,,,,,,7",
                        "138,70,3673,Contact with Thayd,Contact with Thayd,1,4888,matched,Signal Flares,Signal Flares,12,12521,1,0,8867,0,0,0,7",
                        "139,70,3673,Contact with Thayd,Contact with Thayd,2,4889,matched,Signal Flares,Signal Flares,12,13150,1,0,8868,0,0,0,7",
                        "140,70,3673,Contact with Thayd,Contact with Thayd,3,4890,matched,Signal Flares,Signal Flares,12,13151,1,0,8869,0,0,0,7",
                        "389,208,3963,More Important Than Revenge,More Important Than Revenge,0,,unmatched,Activate the Ship Controls inside one of the Dominion ships in Camp Icefury,,,,,,,,,,1",
                        "429,230,3886,Fiery Distraction,Fiery Distraction,0,,unmatched,Grab a Burning Torch outside Coldburrow Cavern,,,,,,,,,,7",
                        "430,230,3886,Fiery Distraction,Fiery Distraction,1,5052,matched,Burn Skeech Huts outside Coldburrow Cavern,Burn Skeech Huts outside Coldburrow Cavern,14,1460,3,0,7770,0,0,0,7",
                    ]
                )
                + "\n",
                encoding="utf-8",
            )
            strings = {
                100: "Quest Title",
                207: "From the Wreckage",
                208: "Reporting for Duty",
                209: "Empowered Tower",
                210: "Indigenous Intelligence",
                211: "Contact with Thayd",
                212: "Fiery Distraction",
                213: "More Important Than Revenge",
                214: "By Leaps and Bounds",
                215: "Ordnance Recovery",
            }
            quests = [
                {"ID": "10", "localizedTextIdTitle": "100"},
                {"ID": "3479", "localizedTextIdTitle": "207"},
                {"ID": "3480", "localizedTextIdTitle": "208"},
                {"ID": "3486", "localizedTextIdTitle": "209"},
                {"ID": "3668", "localizedTextIdTitle": "210"},
                {"ID": "3673", "localizedTextIdTitle": "211"},
                {"ID": "3886", "localizedTextIdTitle": "212"},
                {"ID": "3963", "localizedTextIdTitle": "213"},
                {"ID": "3777", "localizedTextIdTitle": "214"},
                {"ID": "5596", "localizedTextIdTitle": "215"},
            ]
            quest_output = [
                {"quest_id": 10, "bucket": "generic_table_driven", "completion_status": "not_retail_complete"},
                {"quest_id": 3479, "bucket": "generic_with_script", "completion_status": "not_retail_complete"},
                {"quest_id": 3480, "bucket": "generic_with_script", "completion_status": "not_retail_complete"},
                {"quest_id": 3486, "bucket": "curated_full", "completion_status": "not_retail_complete"},
                {"quest_id": 3668, "bucket": "curated_full", "completion_status": "not_retail_complete"},
                {"quest_id": 3673, "bucket": "curated_full", "completion_status": "not_retail_complete"},
                {"quest_id": 3886, "bucket": "curated_full", "completion_status": "not_retail_complete"},
                {"quest_id": 3963, "bucket": "curated_full", "completion_status": "not_retail_complete"},
                {"quest_id": 3777, "bucket": "curated_full", "completion_status": "not_retail_complete"},
                {"quest_id": 5596, "bucket": "curated_full", "completion_status": "not_retail_complete"},
            ]

            rows = audit.build_quest_objective_evidence_rows(strings, quests, quest_output, objective_map)

            self.assertEqual(24, len(rows))
            matched = next(row for row in rows if row["match_status"] == "matched")
            self.assertEqual("current_quest", matched["quest_link_status"])
            self.assertEqual(300, matched["quest_objective_id"])
            self.assertEqual("datamapping_jabbithole_objective_matched_pending_text_order_smoke", matched["evidence_status"])
            self.assertIn("Jabbithole objective 1", matched["blocker_summary"])
            self.assertIn("Quest2 10 objective order 0", matched["blocker_summary"])
            self.assertIn("source text 'Do the thing'", matched["blocker_summary"])
            self.assertIn("QuestObjective 300 type 2 data 500 count 3", matched["blocker_summary"])
            self.assertIn("client text 'Do the thing'", matched["blocker_summary"])
            self.assertIn("text/order reconciliation", matched["blocker_summary"])
            self.assertIn("objective-credit ownership", matched["blocker_summary"])
            q3479_survivor = next(row for row in rows if row["quest_id"] == 3479 and row["quest_objective_id"] == 4467)
            self.assertEqual("reviewed_match", q3479_survivor["match_status"])
            self.assertEqual("datamapping_jabbithole_objective_reviewed_match_pending_client_smoke", q3479_survivor["evidence_status"])
            self.assertEqual(11070, q3479_survivor["objective_data"])
            self.assertIn("QuestObjective.tbl.sql", q3479_survivor["evidence_sources"])
            self.assertIn("Jabbithole objective 418", q3479_survivor["blocker_summary"])
            q3479_yeti = next(row for row in rows if row["quest_id"] == 3479 and row["jabbithole_quest_objective_id"] == 419)
            self.assertEqual(4564, q3479_yeti["quest_objective_id"])
            self.assertEqual("reviewed_match", q3479_yeti["match_status"])
            self.assertEqual(
                "datamapping_jabbithole_objective_reviewed_match_pending_client_smoke",
                q3479_yeti["evidence_status"],
            )
            self.assertEqual(8, q3479_yeti["objective_type"])
            self.assertEqual(7288, q3479_yeti["objective_data"])
            self.assertEqual(8, q3479_yeti["objective_count"])
            self.assertEqual(12155, q3479_yeti["world_location_0"])
            self.assertEqual(12158, q3479_yeti["world_location_3"])
            self.assertIn("TargetGroup.tbl.sql", q3479_yeti["evidence_sources"])
            self.assertIn("Creature2.tbl.sql", q3479_yeti["evidence_sources"])
            self.assertIn("QuestObjective 4564", q3479_yeti["blocker_summary"])
            self.assertIn("TargetGroup 7288", q3479_yeti["blocker_summary"])
            self.assertIn("TargetGroup 1463", q3479_yeti["blocker_summary"])
            self.assertIn("Creature2 11945", q3479_yeti["blocker_summary"])
            self.assertIn("Creature2 11948", q3479_yeti["blocker_summary"])
            self.assertFalse(audit.is_objective_evidence_blocker(q3479_yeti))
            q3480_survivor = next(row for row in rows if row["quest_id"] == 3480 and row["quest_objective_id"] == 4470)
            self.assertEqual("reviewed_match", q3480_survivor["match_status"])
            self.assertEqual("datamapping_jabbithole_objective_reviewed_match_pending_client_smoke", q3480_survivor["evidence_status"])
            self.assertEqual(11070, q3480_survivor["objective_data"])
            self.assertIn("Jabbithole objective 2336", q3480_survivor["blocker_summary"])
            q3480_yeti = next(row for row in rows if row["quest_id"] == 3480 and row["jabbithole_quest_objective_id"] == 2337)
            self.assertEqual(4565, q3480_yeti["quest_objective_id"])
            self.assertEqual("reviewed_match", q3480_yeti["match_status"])
            self.assertEqual(
                "datamapping_jabbithole_objective_reviewed_match_pending_client_smoke",
                q3480_yeti["evidence_status"],
            )
            self.assertEqual(8, q3480_yeti["objective_type"])
            self.assertEqual(1463, q3480_yeti["objective_data"])
            self.assertEqual(8, q3480_yeti["objective_count"])
            self.assertEqual(12155, q3480_yeti["world_location_0"])
            self.assertEqual(12158, q3480_yeti["world_location_3"])
            self.assertIn("TargetGroup.tbl.sql", q3480_yeti["evidence_sources"])
            self.assertIn("Creature2.tbl.sql", q3480_yeti["evidence_sources"])
            self.assertIn("QuestObjective 4565", q3480_yeti["blocker_summary"])
            self.assertIn("TargetGroup 1463", q3480_yeti["blocker_summary"])
            self.assertIn("Creature2 11945", q3480_yeti["blocker_summary"])
            self.assertIn("Creature2 11948", q3480_yeti["blocker_summary"])
            self.assertFalse(audit.is_objective_evidence_blocker(q3480_yeti))
            q3486_arrival = next(row for row in rows if row["quest_id"] == 3486 and row["quest_objective_id"] == 4987)
            self.assertEqual("reviewed_match", q3486_arrival["match_status"])
            self.assertEqual("datamapping_jabbithole_objective_reviewed_match_pending_client_smoke", q3486_arrival["evidence_status"])
            self.assertEqual(0, q3486_arrival["objective_data"])
            self.assertEqual(7807, q3486_arrival["world_location_0"])
            self.assertIn("Q3486", q3486_arrival["blocker_summary"])
            self.assertIn("story-panel credit", q3486_arrival["blocker_summary"])
            q3486_loftite = next(row for row in rows if row["quest_id"] == 3486 and row["jabbithole_quest_objective_id"] == 437)
            self.assertEqual(4485, q3486_loftite["quest_objective_id"])
            self.assertEqual("reviewed_match", q3486_loftite["match_status"])
            self.assertEqual(
                "datamapping_jabbithole_objective_reviewed_match_pending_client_smoke",
                q3486_loftite["evidence_status"],
            )
            self.assertEqual(32, q3486_loftite["objective_type"])
            self.assertEqual(206, q3486_loftite["objective_data"])
            self.assertEqual(5, q3486_loftite["objective_count"])
            self.assertEqual(7807, q3486_loftite["world_location_0"])
            self.assertIn("TargetGroup.tbl.sql", q3486_loftite["evidence_sources"])
            self.assertIn("Creature2.tbl.sql", q3486_loftite["evidence_sources"])
            self.assertIn("QuestObjective 4485", q3486_loftite["blocker_summary"])
            self.assertIn("virtual item 206", q3486_loftite["blocker_summary"])
            self.assertIn("TargetGroup 4985", q3486_loftite["blocker_summary"])
            self.assertIn("Creature2 11925", q3486_loftite["blocker_summary"])
            self.assertIn("Creature2 11924", q3486_loftite["blocker_summary"])
            self.assertIn("reward source 5319", q3486_loftite["blocker_summary"])
            self.assertFalse(audit.is_objective_evidence_blocker(q3486_loftite))
            q3777_travel = next(row for row in rows if row["quest_id"] == 3777 and row["quest_objective_id"] == 5075)
            self.assertEqual("reviewed_match", q3777_travel["match_status"])
            self.assertEqual("datamapping_jabbithole_objective_reviewed_match_pending_client_smoke", q3777_travel["evidence_status"])
            self.assertEqual(17, q3777_travel["objective_type"])
            self.assertEqual(219, q3777_travel["objective_data"])
            self.assertEqual(9578, q3777_travel["world_location_0"])
            self.assertIn("EnterZone data 219", q3777_travel["blocker_summary"])
            q3777_first_fragment = next(row for row in rows if row["quest_id"] == 3777 and row["jabbithole_quest_objective_id"] == 411)
            self.assertEqual(5076, q3777_first_fragment["quest_objective_id"])
            self.assertEqual("reviewed_match", q3777_first_fragment["match_status"])
            self.assertEqual(
                "datamapping_jabbithole_objective_reviewed_match_pending_client_smoke",
                q3777_first_fragment["evidence_status"],
            )
            self.assertEqual(5, q3777_first_fragment["objective_type"])
            self.assertEqual(6952, q3777_first_fragment["objective_data"])
            self.assertEqual(1, q3777_first_fragment["objective_count"])
            self.assertEqual(9578, q3777_first_fragment["world_location_0"])
            self.assertIn("TargetGroup.tbl.sql", q3777_first_fragment["evidence_sources"])
            self.assertIn("Creature2.tbl.sql", q3777_first_fragment["evidence_sources"])
            self.assertIn("Item2.tbl.sql", q3777_first_fragment["evidence_sources"])
            self.assertIn("QuestObjective 5076", q3777_first_fragment["blocker_summary"])
            self.assertIn("TargetGroup 7534", q3777_first_fragment["blocker_summary"])
            self.assertIn("Creature2 13120", q3777_first_fragment["blocker_summary"])
            self.assertFalse(audit.is_objective_evidence_blocker(q3777_first_fragment))
            q3777_collect = next(row for row in rows if row["quest_id"] == 3777 and row["jabbithole_quest_objective_id"] == 412)
            self.assertEqual(4859, q3777_collect["quest_objective_id"])
            self.assertEqual("reviewed_match", q3777_collect["match_status"])
            self.assertEqual(
                "datamapping_jabbithole_objective_reviewed_match_pending_client_smoke",
                q3777_collect["evidence_status"],
            )
            self.assertEqual(4, q3777_collect["objective_type"])
            self.assertEqual(6998, q3777_collect["objective_data"])
            self.assertEqual(8, q3777_collect["objective_count"])
            self.assertEqual(9578, q3777_collect["world_location_0"])
            self.assertIn("TargetGroup.tbl.sql", q3777_collect["evidence_sources"])
            self.assertIn("Creature2.tbl.sql", q3777_collect["evidence_sources"])
            self.assertIn("Item2.tbl.sql", q3777_collect["evidence_sources"])
            self.assertIn("QuestObjective 4859", q3777_collect["blocker_summary"])
            self.assertIn("Item2 6998", q3777_collect["blocker_summary"])
            self.assertIn("Denner Hazefall 15759", q3777_collect["blocker_summary"])
            self.assertFalse(audit.is_objective_evidence_blocker(q3777_collect))
            q5596_crash_site = next(row for row in rows if row["quest_id"] == 5596 and row["jabbithole_quest_objective_id"] == 1453)
            self.assertEqual(8255, q5596_crash_site["quest_objective_id"])
            self.assertEqual("reviewed_match", q5596_crash_site["match_status"])
            self.assertEqual(
                "datamapping_jabbithole_objective_reviewed_match_pending_client_smoke",
                q5596_crash_site["evidence_status"],
            )
            self.assertEqual(22, q5596_crash_site["objective_type"])
            self.assertEqual(2788, q5596_crash_site["objective_data"])
            self.assertEqual(1, q5596_crash_site["objective_count"])
            self.assertEqual(17853, q5596_crash_site["world_location_0"])
            self.assertIn("CrimsonIsleMapScript.cs", q5596_crash_site["evidence_sources"])
            self.assertIn("VolumeGridTriggerEntityTests.cs", q5596_crash_site["evidence_sources"])
            self.assertIn("QuestObjective 8255", q5596_crash_site["blocker_summary"])
            self.assertIn("EnterArea type 22 data 2788", q5596_crash_site["blocker_summary"])
            self.assertIn("WorldLocation2 17853", q5596_crash_site["blocker_summary"])
            self.assertIn("objective 8255", q5596_crash_site["blocker_summary"])
            self.assertFalse(audit.is_objective_evidence_blocker(q5596_crash_site))
            q5596_search = next(row for row in rows if row["quest_id"] == 5596 and row["jabbithole_quest_objective_id"] == 1454)
            self.assertEqual(8468, q5596_search["quest_objective_id"])
            self.assertEqual("reviewed_match", q5596_search["match_status"])
            self.assertEqual(
                "datamapping_jabbithole_objective_reviewed_match_pending_client_smoke",
                q5596_search["evidence_status"],
            )
            self.assertEqual(14, q5596_search["objective_type"])
            self.assertEqual(2619, q5596_search["objective_data"])
            self.assertEqual(3, q5596_search["objective_count"])
            self.assertEqual(17853, q5596_search["world_location_0"])
            self.assertEqual(17852, q5596_search["world_location_1"])
            self.assertIn("TargetGroup.tbl.sql", q5596_search["evidence_sources"])
            self.assertIn("Creature2.tbl.sql", q5596_search["evidence_sources"])
            self.assertIn("QuestObjective 8468", q5596_search["blocker_summary"])
            self.assertIn("TargetGroup 2619", q5596_search["blocker_summary"])
            self.assertIn("Creature2 24703", q5596_search["blocker_summary"])
            self.assertFalse(audit.is_objective_evidence_blocker(q5596_search))
            q3668_lusk = next(row for row in rows if row["quest_id"] == 3668 and row["jabbithole_quest_objective_id"] == 132)
            self.assertEqual(4744, q3668_lusk["quest_objective_id"])
            self.assertEqual("reviewed_match", q3668_lusk["match_status"])
            self.assertEqual(
                "datamapping_jabbithole_objective_reviewed_match_pending_client_smoke",
                q3668_lusk["evidence_status"],
            )
            self.assertEqual(12484, q3668_lusk["objective_data"])
            self.assertEqual(8857, q3668_lusk["world_location_0"])
            self.assertIn("Scientist Lusk", q3668_lusk["blocker_summary"])
            self.assertIn("QuestObjective 4744", q3668_lusk["blocker_summary"])
            self.assertIn("TalkTo Creature2 12484", q3668_lusk["blocker_summary"])
            self.assertIn("source_coordinate_id 4272", q3668_lusk["blocker_summary"])
            q3668_survivors = next(row for row in rows if row["quest_id"] == 3668 and row["jabbithole_quest_objective_id"] == 133)
            self.assertEqual("reviewed_match", q3668_survivors["match_status"])
            self.assertEqual(
                "datamapping_jabbithole_objective_reviewed_match_pending_client_smoke",
                q3668_survivors["evidence_status"],
            )
            self.assertIn("TargetGroup 7261", q3668_survivors["blocker_summary"])
            q3668_skeech = next(row for row in rows if row["quest_id"] == 3668 and row["jabbithole_quest_objective_id"] == 134)
            self.assertEqual("reviewed_match", q3668_skeech["match_status"])
            self.assertEqual(
                "datamapping_jabbithole_objective_reviewed_match_pending_client_smoke",
                q3668_skeech["evidence_status"],
            )
            self.assertIn("14054 and 11913", q3668_skeech["blocker_summary"])
            q3673_signal_flares = next(row for row in rows if row["quest_id"] == 3673 and row["jabbithole_quest_objective_id"] == 137)
            self.assertEqual(4748, q3673_signal_flares["quest_objective_id"])
            self.assertEqual("reviewed_match", q3673_signal_flares["match_status"])
            self.assertEqual(
                "datamapping_jabbithole_objective_reviewed_match_pending_client_smoke",
                q3673_signal_flares["evidence_status"],
            )
            self.assertEqual(14, q3673_signal_flares["objective_type"])
            self.assertEqual(1106, q3673_signal_flares["objective_data"])
            self.assertEqual(3, q3673_signal_flares["objective_count"])
            self.assertIn("TargetGroup.tbl.sql", q3673_signal_flares["evidence_sources"])
            self.assertIn("NorthernWildsMapScript.cs", q3673_signal_flares["evidence_sources"])
            self.assertIn("QuestObjective 4748", q3673_signal_flares["blocker_summary"])
            self.assertIn("TargetGroup 1106", q3673_signal_flares["blocker_summary"])
            q3673_third_flare = next(row for row in rows if row["quest_id"] == 3673 and row["jabbithole_quest_objective_id"] == 140)
            self.assertEqual(4890, q3673_third_flare["quest_objective_id"])
            self.assertEqual("reviewed_match", q3673_third_flare["match_status"])
            self.assertEqual(
                "datamapping_jabbithole_objective_reviewed_match_pending_client_smoke",
                q3673_third_flare["evidence_status"],
            )
            self.assertEqual(13151, q3673_third_flare["objective_data"])
            self.assertEqual(8869, q3673_third_flare["world_location_0"])
            self.assertIn("Creature2 13151", q3673_third_flare["blocker_summary"])
            self.assertIn("WorldLocation2 8869", q3673_third_flare["blocker_summary"])
            q3886_torch = next(row for row in rows if row["quest_id"] == 3886 and row["jabbithole_quest_objective_id"] == 429)
            self.assertEqual(5053, q3886_torch["quest_objective_id"])
            self.assertEqual("reviewed_match", q3886_torch["match_status"])
            self.assertEqual(
                "datamapping_jabbithole_objective_reviewed_match_pending_client_smoke",
                q3886_torch["evidence_status"],
            )
            self.assertEqual(12, q3886_torch["objective_type"])
            self.assertEqual(13630, q3886_torch["objective_data"])
            self.assertEqual(1, q3886_torch["objective_count"])
            self.assertEqual(7770, q3886_torch["world_location_0"])
            self.assertIn("Creature2.tbl.sql", q3886_torch["evidence_sources"])
            self.assertIn("QuestObjective 5053", q3886_torch["blocker_summary"])
            self.assertIn("Burning Torch", q3886_torch["blocker_summary"])
            self.assertIn("source_coordinate_ids 2425, 124555, 142844, and 1585277", q3886_torch["blocker_summary"])
            self.assertFalse(audit.is_objective_evidence_blocker(q3886_torch))
            q3886_hut = next(row for row in rows if row["quest_id"] == 3886 and row["jabbithole_quest_objective_id"] == 430)
            self.assertEqual(5052, q3886_hut["quest_objective_id"])
            self.assertEqual("reviewed_match", q3886_hut["match_status"])
            self.assertEqual(
                "datamapping_jabbithole_objective_reviewed_match_pending_client_smoke",
                q3886_hut["evidence_status"],
            )
            self.assertEqual(14, q3886_hut["objective_type"])
            self.assertEqual(1460, q3886_hut["objective_data"])
            self.assertEqual(3, q3886_hut["objective_count"])
            self.assertIn("TargetGroup.tbl.sql", q3886_hut["evidence_sources"])
            self.assertIn("QuestObjective 5052", q3886_hut["blocker_summary"])
            self.assertIn("TargetGroup 1460", q3886_hut["blocker_summary"])
            self.assertIn("source_coordinate_ids 2309, 2310, 2311, 2312, 2313, 2314, 14843, 115795, and 115796", q3886_hut["blocker_summary"])
            self.assertIn("checklist indexes 0 through 8", q3886_hut["blocker_summary"])
            self.assertFalse(audit.is_objective_evidence_blocker(q3886_hut))
            q3963_ship_controls = next(row for row in rows if row["quest_id"] == 3963 and row["jabbithole_quest_objective_id"] == 389)
            self.assertEqual(5201, q3963_ship_controls["quest_objective_id"])
            self.assertEqual("reviewed_match", q3963_ship_controls["match_status"])
            self.assertEqual(
                "datamapping_jabbithole_objective_reviewed_match_pending_client_smoke",
                q3963_ship_controls["evidence_status"],
            )
            self.assertEqual(5, q3963_ship_controls["objective_type"])
            self.assertEqual(0, q3963_ship_controls["objective_data"])
            self.assertEqual(1, q3963_ship_controls["objective_count"])
            self.assertEqual(45401, q3963_ship_controls["world_location_0"])
            self.assertEqual(45402, q3963_ship_controls["world_location_1"])
            self.assertIn("TargetGroup.tbl.sql", q3963_ship_controls["evidence_sources"])
            self.assertIn("Creature2.tbl.sql", q3963_ship_controls["evidence_sources"])
            self.assertIn("QuestObjective 5201", q3963_ship_controls["blocker_summary"])
            self.assertIn("TargetGroup 7573", q3963_ship_controls["blocker_summary"])
            self.assertIn("Creature2 27196", q3963_ship_controls["blocker_summary"])
            self.assertFalse(audit.is_objective_evidence_blocker(q3963_ship_controls))
            unmatched = next(row for row in rows if row["quest_id"] == 10 and row["match_status"] == "unmatched")
            self.assertEqual("", unmatched["quest_objective_id"])
            self.assertEqual("datamapping_jabbithole_objective_unmatched_blocked", unmatched["evidence_status"])
            self.assertIn("jabbithole_mysql/quest_objectives.sql", unmatched["evidence_sources"])
            self.assertIn("Jabbithole objective 2", unmatched["blocker_summary"])
            self.assertIn("Quest2 10 objective order 1", unmatched["blocker_summary"])
            self.assertIn("match_status unmatched", unmatched["blocker_summary"])
            self.assertIn("no current QuestObjective id", unmatched["blocker_summary"])
            self.assertIn("current build Quest2 objective slot", unmatched["blocker_summary"])
            noncurrent = next(row for row in rows if row["quest_link_status"] == "non_current_client_quest_reference")
            self.assertEqual("non_current_client_quest_reference", noncurrent["quest_bucket"])
            self.assertEqual("", noncurrent["quest_objective_id"])
            self.assertEqual("datamapping_objective_not_current_client_quest_row_blocked", noncurrent["evidence_status"])
            self.assertIn("jabbithole_mysql/quest_objectives.sql", noncurrent["evidence_sources"])
            self.assertIn("not present in the current client Quest2 table", noncurrent["blocker_summary"])
            self.assertTrue(all(row["completion_status"] == "not_retail_complete" for row in rows))
        finally:
            shutil.rmtree(csv_root, ignore_errors=True)

    def test_quest_objective_evidence_overrides_review_next_slice_text_order_rows(self) -> None:
        csv_root = Path(tempfile.mkdtemp(prefix="nf-content-audit-objective-review-"))
        try:
            objective_map = csv_root / "quest_objective_map.csv"
            objective_map.write_text(
                "\n".join(
                    [
                        "jabbithole_quest_objective_id,jabbithole_quest_id,quest2_id,quest_name,client_quest_name,objective_order,quest_objective_id,match_status,jabbithole_objective_text,client_objective_text,objective_type,objective_data,objective_count,quest_direction_id,world_location_0,world_location_1,world_location_2,world_location_3,last_seen_in",
                        "1450,900,5573,Powering Down,Powering Down,0,8524,matched,Find the Megatech Shield Generator further inland from the Savage Coast,Find the Megatech Shield Generator further inland from the Savage Coast,17,1217,1,0,0,0,0,0,7",
                        "1451,900,5573,Powering Down,Powering Down,1,8229,matched,Disable the Power Regulators at the Megatech Shield Generator,Disable the $m(creature=24999) at the Megatech Shield Generator,14,2541,3,0,17816,17929,17930,0,7",
                        "2703,901,5575,Seizing Power,Seizing Power,0,8523,matched,Find the Megatech Shield Generators inland from the Savage Coast,Find the Megatech Shield Generators inland from the Savage Coast,17,1217,1,0,0,0,0,0,7",
                        "2704,901,5575,Seizing Power,Seizing Power,1,8371,matched,Disable the Power Regulators at the Megatech Shield Generator,Disable the $m(creature=24999) at the Megatech Shield Generator,14,2541,3,0,17816,17929,17930,0,7",
                        "1452,902,5580,Enforced Radio Silence,Enforced Radio Silence,0,8227,matched,Sabotage the Tower Controls at Megatech Station,Sabotage the $(creature=26559) at Megatech Station,14,2889,2,0,28105,24831,0,0,7",
                        "732,903,3797,Securing the Area,Securing the Area,0,4918,matched,Kill Rootbrutes and yeti outside Settler's Reach,Kill Rootbrute Grimspores Yeti Frostclaws and Yeti Snowstalkers near Settler's Reach,8,1177,8,0,9181,9182,9962,0,7",
                    ]
                )
                + "\n",
                encoding="utf-8",
            )
            strings = {
                1: "Powering Down",
                2: "Seizing Power",
                3: "Enforced Radio Silence",
                4: "Securing the Area",
            }
            quests = [
                {"ID": "5573", "localizedTextIdTitle": "1"},
                {"ID": "5575", "localizedTextIdTitle": "2"},
                {"ID": "5580", "localizedTextIdTitle": "3"},
                {"ID": "3797", "localizedTextIdTitle": "4"},
            ]
            quest_output = [
                {"quest_id": 5573, "bucket": "curated_full", "completion_status": "not_retail_complete"},
                {"quest_id": 5575, "bucket": "curated_full", "completion_status": "not_retail_complete"},
                {"quest_id": 5580, "bucket": "curated_full", "completion_status": "not_retail_complete"},
                {"quest_id": 3797, "bucket": "curated_full", "completion_status": "not_retail_complete"},
            ]

            rows = audit.build_quest_objective_evidence_rows(strings, quests, quest_output, objective_map)

            self.assertEqual(6, len(rows))
            for row in rows:
                self.assertEqual("reviewed_match", row["match_status"])
                self.assertEqual("datamapping_jabbithole_objective_reviewed_match_pending_client_smoke", row["evidence_status"])
                self.assertFalse(audit.is_objective_evidence_blocker(row))
            q5580 = next(row for row in rows if row["quest_id"] == 5580)
            self.assertIn("Tower Controls", q5580["blocker_summary"])
            self.assertIn("source_coordinate_ids 15122 and 15123", q5580["blocker_summary"])
            q3797 = next(row for row in rows if row["quest_id"] == 3797)
            self.assertIn("Rootbrutes and yeti", q3797["blocker_summary"])
            self.assertIn("objective 4918", q3797["blocker_summary"])
        finally:
            shutil.rmtree(csv_root, ignore_errors=True)

    def test_quest_zone_evidence_rows_cover_match_statuses_and_noncurrent_rows(self) -> None:
        csv_root = Path(tempfile.mkdtemp(prefix="nf-content-audit-zones-"))
        try:
            zone_map = csv_root / "quest_zone_map.csv"
            zone_map.write_text(
                "\n".join(
                    [
                        "source_relation_id,relation_source,jabbithole_quest_id,quest2_id,jabbithole_quest_name,client_quest_name,jabbithole_zone_id,world_zone_id,jabbithole_zone_name,client_world_zone_name,match_status,enabled,last_seen_in",
                        "1,quest_call_zones,1000,10,Quest Title,Quest Title,20,30,Source Zone,Client Zone,matched,true,7",
                        "2,quest_zones,1000,10,Quest Title,Quest Title,21,31,Partial Zone,Client Partial,partial,true,7",
                        "3,quest_zones,1000,10,Quest Title,Quest Title,22,,Missing Zone,,unmatched,true,7",
                        "4,quest_zones,1001,9999,Old Quest,,23,33,Old Zone,Old Client,matched,true,7",
                        "5,quest_call_zones,1000,10,Quest Title,Quest Title,5,6,Auroria,,partial,true,0",
                    ]
                )
                + "\n",
                encoding="utf-8",
            )
            strings = {100: "Quest Title"}
            quests = [{"ID": "10", "localizedTextIdTitle": "100"}]
            quest_output = [{"quest_id": 10, "bucket": "generic_table_driven", "completion_status": "not_retail_complete"}]

            rows = audit.build_quest_zone_evidence_rows(strings, quests, quest_output, zone_map)

            self.assertEqual(5, len(rows))
            statuses = {row["source_relation_id"]: row["evidence_status"] for row in rows}
            self.assertEqual("datamapping_quest_zone_matched_pending_runtime_visibility_smoke", statuses[1])
            self.assertEqual("datamapping_quest_zone_partial_match_pending_zone_review", statuses[2])
            self.assertEqual("datamapping_quest_zone_unmatched_blocked", statuses[3])
            self.assertEqual("datamapping_quest_zone_partial_match_pending_zone_review", statuses[5])
            matched = next(row for row in rows if row["source_relation_id"] == 1)
            self.assertIn("jabbithole_mysql/quest_zones.sql", matched["evidence_sources"])
            self.assertIn("jabbithole_mysql/quest_call_zones.sql", matched["evidence_sources"])
            self.assertIn("quest_call_zones row 1", matched["blocker_summary"])
            self.assertIn("Quest2 10", matched["blocker_summary"])
            self.assertIn("Jabbithole zone 20 (Source Zone)", matched["blocker_summary"])
            self.assertIn("WorldZone 30 (Client Zone)", matched["blocker_summary"])
            self.assertIn("match_status matched", matched["blocker_summary"])
            self.assertIn("runtime quest visibility/routing", matched["blocker_summary"])
            partial = next(row for row in rows if row["source_relation_id"] == 2)
            self.assertIn("jabbithole_mysql/quest_zones.sql", partial["evidence_sources"])
            self.assertIn("jabbithole_mysql/quest_call_zones.sql", partial["evidence_sources"])
            self.assertIn("quest_zones row 2", partial["blocker_summary"])
            self.assertIn("Quest2 10", partial["blocker_summary"])
            self.assertIn("Jabbithole zone 21 (Partial Zone)", partial["blocker_summary"])
            self.assertIn("WorldZone 31 (Client Partial)", partial["blocker_summary"])
            self.assertIn("match_status partial", partial["blocker_summary"])
            self.assertIn("found a current WorldZone row/name", partial["blocker_summary"])
            self.assertIn("reviewed current WorldZone bridge", partial["blocker_summary"])
            stale_partial = next(row for row in rows if row["source_relation_id"] == 5)
            self.assertIn("quest_call_zones row 5", stale_partial["blocker_summary"])
            self.assertIn("Jabbithole zone 5 (Auroria)", stale_partial["blocker_summary"])
            self.assertIn("WorldZone 6", stale_partial["blocker_summary"])
            self.assertIn("no current build 16042 WorldZone row/name", stale_partial["blocker_summary"])
            self.assertIn("WorldZone.tbl", stale_partial["blocker_summary"])
            self.assertIn("accepted live client route evidence", stale_partial["blocker_summary"])
            unmatched = next(row for row in rows if row["source_relation_id"] == 3)
            self.assertEqual("", unmatched["world_zone_id"])
            self.assertIn("match_status unmatched", unmatched["blocker_summary"])
            self.assertIn("no current WorldZone id", unmatched["blocker_summary"])
            noncurrent = next(row for row in rows if row["quest_link_status"] == "non_current_client_quest_reference")
            self.assertEqual("non_current_client_quest_reference", noncurrent["quest_bucket"])
            self.assertEqual("datamapping_zone_not_current_client_quest_row_blocked", noncurrent["evidence_status"])
            self.assertIn("Quest2 9999", noncurrent["blocker_summary"])
            self.assertIn("not present in the current client Quest2 table", noncurrent["blocker_summary"])
            self.assertTrue(all(row["completion_status"] == "not_retail_complete" for row in rows))
        finally:
            shutil.rmtree(csv_root, ignore_errors=True)

    def test_quest_zone_evidence_rows_apply_q5597_crimson_isle_call_zone_override(self) -> None:
        csv_root = Path(tempfile.mkdtemp(prefix="nf-content-audit-q5597-zone-"))
        try:
            zone_map = csv_root / "quest_zone_map.csv"
            zone_map.write_text(
                "\n".join(
                    [
                        "source_relation_id,relation_source,jabbithole_quest_id,quest2_id,jabbithole_quest_name,client_quest_name,jabbithole_zone_id,world_zone_id,jabbithole_zone_name,client_world_zone_name,match_status,enabled,last_seen_in",
                        "311,quest_call_zones,945,5597,Dregs and Thieves,Dregs and Thieves,9,12,Crimson Isle,,matched,true,3",
                    ]
                )
                + "\n",
                encoding="utf-8",
            )
            strings = {100: "Dregs and Thieves"}
            quests = [{"ID": "5597", "localizedTextIdTitle": "100"}]
            quest_output = [{"quest_id": 5597, "bucket": "curated_full", "completion_status": "not_retail_complete"}]

            rows = audit.build_quest_zone_evidence_rows(strings, quests, quest_output, zone_map)

            self.assertEqual(1, len(rows))
            q5597_zone = rows[0]
            self.assertEqual(
                "datamapping_q5597_crimson_isle_call_zone_worldzone12_blank_reviewed_current_zone622_pending_route_proof",
                q5597_zone["evidence_status"],
            )
            self.assertIn("world_zone_client_map.csv", q5597_zone["evidence_sources"])
            self.assertIn("quest_call_zones row 311", q5597_zone["blocker_summary"])
            self.assertIn("WorldZone 12 has no client name", q5597_zone["blocker_summary"])
            self.assertIn("Quest2.worldZoneId 622", q5597_zone["blocker_summary"])
            self.assertIn("child WorldZone rows 623, 629, 1217, 1218, 1219, 1227, and 1284", q5597_zone["blocker_summary"])
            self.assertIn("reviewed current WorldZone replacement", q5597_zone["blocker_summary"])
            self.assertIn("accepted live client route smoke", q5597_zone["blocker_summary"])
            self.assertEqual("not_retail_complete", q5597_zone["completion_status"])
        finally:
            shutil.rmtree(csv_root, ignore_errors=True)

    def test_quest_zone_evidence_rows_apply_q5597_illium_call_zone_override(self) -> None:
        csv_root = Path(tempfile.mkdtemp(prefix="nf-content-audit-q5597-zone-"))
        try:
            zone_map = csv_root / "quest_zone_map.csv"
            zone_map.write_text(
                "\n".join(
                    [
                        "source_relation_id,relation_source,jabbithole_quest_id,quest2_id,jabbithole_quest_name,client_quest_name,jabbithole_zone_id,world_zone_id,jabbithole_zone_name,client_world_zone_name,match_status,enabled,last_seen_in",
                        "4637,quest_call_zones,945,5597,Dregs and Thieves,Dregs and Thieves,46,78,Illium,Western Ellevar,matched,true,0",
                    ]
                )
                + "\n",
                encoding="utf-8",
            )
            strings = {100: "Dregs and Thieves"}
            quests = [{"ID": "5597", "localizedTextIdTitle": "100"}]
            quest_output = [{"quest_id": 5597, "bucket": "curated_full", "completion_status": "not_retail_complete"}]

            rows = audit.build_quest_zone_evidence_rows(strings, quests, quest_output, zone_map)

            self.assertEqual(1, len(rows))
            q5597_zone = rows[0]
            self.assertEqual(
                "datamapping_q5597_illium_call_zone_worldzone78_ellevar_reviewed_nonprimary_pending_route_proof",
                q5597_zone["evidence_status"],
            )
            self.assertIn("world_zone_client_map.csv", q5597_zone["evidence_sources"])
            self.assertIn("quest_call_zones row 4637", q5597_zone["blocker_summary"])
            self.assertIn("archived Jabbithole zone 46 (Illium)", q5597_zone["blocker_summary"])
            self.assertIn("WorldZone 78 (Western Ellevar)", q5597_zone["blocker_summary"])
            self.assertIn("source last_seen_in 0", q5597_zone["blocker_summary"])
            self.assertIn("WorldZone 2191", q5597_zone["blocker_summary"])
            self.assertIn("Quest2.worldZoneId remains 622", q5597_zone["blocker_summary"])
            self.assertIn("reviewed current WorldZone replacement", q5597_zone["blocker_summary"])
            self.assertIn("accepted live client route smoke", q5597_zone["blocker_summary"])
            self.assertEqual("not_retail_complete", q5597_zone["completion_status"])
        finally:
            shutil.rmtree(csv_root, ignore_errors=True)

    def test_quest_zone_evidence_rows_apply_q5597_housing_skymap_call_zone_override(self) -> None:
        csv_root = Path(tempfile.mkdtemp(prefix="nf-content-audit-q5597-zone-"))
        try:
            zone_map = csv_root / "quest_zone_map.csv"
            zone_map.write_text(
                "\n".join(
                    [
                        "source_relation_id,relation_source,jabbithole_quest_id,quest2_id,jabbithole_quest_name,client_quest_name,jabbithole_zone_id,world_zone_id,jabbithole_zone_name,client_world_zone_name,match_status,enabled,last_seen_in",
                        "4638,quest_call_zones,945,5597,Dregs and Thieves,Dregs and Thieves,40,60,Housing Skymap,Mozyk Quarry,matched,true,0",
                    ]
                )
                + "\n",
                encoding="utf-8",
            )
            strings = {100: "Dregs and Thieves"}
            quests = [{"ID": "5597", "localizedTextIdTitle": "100"}]
            quest_output = [{"quest_id": 5597, "bucket": "curated_full", "completion_status": "not_retail_complete"}]

            rows = audit.build_quest_zone_evidence_rows(strings, quests, quest_output, zone_map)

            self.assertEqual(1, len(rows))
            q5597_zone = rows[0]
            self.assertEqual(
                "datamapping_q5597_housing_skymap_call_zone_worldzone60_mozyk_quarry_reviewed_nonprimary_pending_route_proof",
                q5597_zone["evidence_status"],
            )
            self.assertIn("world_zone_client_map.csv", q5597_zone["evidence_sources"])
            self.assertIn("quest_call_zones row 4638", q5597_zone["blocker_summary"])
            self.assertIn("archived Jabbithole zone 40 (Housing Skymap)", q5597_zone["blocker_summary"])
            self.assertIn("WorldZone 60 (Mozyk Quarry)", q5597_zone["blocker_summary"])
            self.assertIn("source last_seen_in 0", q5597_zone["blocker_summary"])
            self.assertIn("WorldZone 1630 (Housing)", q5597_zone["blocker_summary"])
            self.assertIn("WorldZone 1136 and 4373 (Skymap)", q5597_zone["blocker_summary"])
            self.assertIn("WorldZone 1265 and 4837 (Community Skymap)", q5597_zone["blocker_summary"])
            self.assertIn("Quest2.worldZoneId remains 622", q5597_zone["blocker_summary"])
            self.assertIn("reviewed current WorldZone replacement", q5597_zone["blocker_summary"])
            self.assertIn("accepted live client route smoke", q5597_zone["blocker_summary"])
            self.assertEqual("not_retail_complete", q5597_zone["completion_status"])
        finally:
            shutil.rmtree(csv_root, ignore_errors=True)

    def test_quest_zone_evidence_rows_apply_q5597_northern_wastes_call_zone_override(self) -> None:
        csv_root = Path(tempfile.mkdtemp(prefix="nf-content-audit-q5597-zone-"))
        try:
            zone_map = csv_root / "quest_zone_map.csv"
            zone_map.write_text(
                "\n".join(
                    [
                        "source_relation_id,relation_source,jabbithole_quest_id,quest2_id,jabbithole_quest_name,client_quest_name,jabbithole_zone_id,world_zone_id,jabbithole_zone_name,client_world_zone_name,match_status,enabled,last_seen_in",
                        "4639,quest_call_zones,945,5597,Dregs and Thieves,Dregs and Thieves,34,41,Northern Wastes,Fort Glory,matched,true,0",
                    ]
                )
                + "\n",
                encoding="utf-8",
            )
            strings = {100: "Dregs and Thieves"}
            quests = [{"ID": "5597", "localizedTextIdTitle": "100"}]
            quest_output = [{"quest_id": 5597, "bucket": "curated_full", "completion_status": "not_retail_complete"}]

            rows = audit.build_quest_zone_evidence_rows(strings, quests, quest_output, zone_map)

            self.assertEqual(1, len(rows))
            q5597_zone = rows[0]
            self.assertEqual(
                "datamapping_q5597_northern_wastes_call_zone_worldzone41_fort_glory_reviewed_nonprimary_pending_route_proof",
                q5597_zone["evidence_status"],
            )
            self.assertIn("world_zone_client_map.csv", q5597_zone["evidence_sources"])
            self.assertIn("quest_call_zones row 4639", q5597_zone["blocker_summary"])
            self.assertIn("archived Jabbithole zone 34 (Northern Wastes)", q5597_zone["blocker_summary"])
            self.assertIn("WorldZone 41 (Fort Glory)", q5597_zone["blocker_summary"])
            self.assertIn("source last_seen_in 0", q5597_zone["blocker_summary"])
            self.assertIn("WorldZone 1784", q5597_zone["blocker_summary"])
            self.assertIn("child WorldZone 1785", q5597_zone["blocker_summary"])
            self.assertIn("Quest2.worldZoneId remains 622", q5597_zone["blocker_summary"])
            self.assertIn("reviewed current WorldZone replacement", q5597_zone["blocker_summary"])
            self.assertIn("accepted live client route smoke", q5597_zone["blocker_summary"])
            self.assertEqual("not_retail_complete", q5597_zone["completion_status"])
        finally:
            shutil.rmtree(csv_root, ignore_errors=True)

    def test_quest_zone_evidence_rows_apply_q5597_malgrave_call_zone_override(self) -> None:
        csv_root = Path(tempfile.mkdtemp(prefix="nf-content-audit-q5597-zone-"))
        try:
            zone_map = csv_root / "quest_zone_map.csv"
            zone_map.write_text(
                "\n".join(
                    [
                        "source_relation_id,relation_source,jabbithole_quest_id,quest2_id,jabbithole_quest_name,client_quest_name,jabbithole_zone_id,world_zone_id,jabbithole_zone_name,client_world_zone_name,match_status,enabled,last_seen_in",
                        "4640,quest_call_zones,945,5597,Dregs and Thieves,Dregs and Thieves,35,42,Malgrave,Protostar Honeyworks,matched,true,0",
                    ]
                )
                + "\n",
                encoding="utf-8",
            )
            strings = {100: "Dregs and Thieves"}
            quests = [{"ID": "5597", "localizedTextIdTitle": "100"}]
            quest_output = [{"quest_id": 5597, "bucket": "curated_full", "completion_status": "not_retail_complete"}]

            rows = audit.build_quest_zone_evidence_rows(strings, quests, quest_output, zone_map)

            self.assertEqual(1, len(rows))
            q5597_zone = rows[0]
            self.assertEqual(
                "datamapping_q5597_malgrave_call_zone_worldzone42_protostar_honeyworks_reviewed_nonprimary_pending_route_proof",
                q5597_zone["evidence_status"],
            )
            self.assertIn("world_zone_client_map.csv", q5597_zone["evidence_sources"])
            self.assertIn("quest_call_zones row 4640", q5597_zone["blocker_summary"])
            self.assertIn("archived Jabbithole zone 35 (Malgrave)", q5597_zone["blocker_summary"])
            self.assertIn("WorldZone 42 (Protostar Honeyworks)", q5597_zone["blocker_summary"])
            self.assertIn("source last_seen_in 0", q5597_zone["blocker_summary"])
            self.assertIn("WorldZone 733 (Protostar Honeyworks Master)", q5597_zone["blocker_summary"])
            self.assertIn("WorldZone 377 (Malgrave)", q5597_zone["blocker_summary"])
            self.assertIn("WorldZone 1711 (The Malgrave Trail)", q5597_zone["blocker_summary"])
            self.assertIn("Quest2.worldZoneId remains 622", q5597_zone["blocker_summary"])
            self.assertIn("reviewed current WorldZone replacement", q5597_zone["blocker_summary"])
            self.assertIn("accepted live client route smoke", q5597_zone["blocker_summary"])
            self.assertEqual("not_retail_complete", q5597_zone["completion_status"])
        finally:
            shutil.rmtree(csv_root, ignore_errors=True)

    def test_quest_zone_evidence_rows_apply_q5597_auroria_call_zone_override(self) -> None:
        csv_root = Path(tempfile.mkdtemp(prefix="nf-content-audit-q5597-zone-"))
        try:
            zone_map = csv_root / "quest_zone_map.csv"
            zone_map.write_text(
                "\n".join(
                    [
                        "source_relation_id,relation_source,jabbithole_quest_id,quest2_id,jabbithole_quest_name,client_quest_name,jabbithole_zone_id,world_zone_id,jabbithole_zone_name,client_world_zone_name,match_status,enabled,last_seen_in",
                        "4641,quest_call_zones,945,5597,Dregs and Thieves,Dregs and Thieves,5,6,Auroria,,partial,true,0",
                    ]
                )
                + "\n",
                encoding="utf-8",
            )
            strings = {100: "Dregs and Thieves"}
            quests = [{"ID": "5597", "localizedTextIdTitle": "100"}]
            quest_output = [{"quest_id": 5597, "bucket": "curated_full", "completion_status": "not_retail_complete"}]

            rows = audit.build_quest_zone_evidence_rows(strings, quests, quest_output, zone_map)

            self.assertEqual(1, len(rows))
            q5597_zone = rows[0]
            self.assertEqual(
                "datamapping_q5597_auroria_call_zone_worldzone6_missing_reviewed_nonprimary_pending_route_proof",
                q5597_zone["evidence_status"],
            )
            self.assertIn("world_zone_client_map.csv", q5597_zone["evidence_sources"])
            self.assertIn("quest_call_zones row 4641", q5597_zone["blocker_summary"])
            self.assertIn("archived Jabbithole zone 5 (Auroria)", q5597_zone["blocker_summary"])
            self.assertIn("WorldZone 6", q5597_zone["blocker_summary"])
            self.assertIn("match_status partial", q5597_zone["blocker_summary"])
            self.assertIn("source last_seen_in 0", q5597_zone["blocker_summary"])
            self.assertIn("no WorldZone 6 row or client name", q5597_zone["blocker_summary"])
            self.assertIn("WorldZone 36 (Auroria)", q5597_zone["blocker_summary"])
            self.assertIn("WorldZone rows 697 (Northeastern Auroria)", q5597_zone["blocker_summary"])
            self.assertIn("1228 (Northwestern Auroria)", q5597_zone["blocker_summary"])
            self.assertIn("1302 (Central Auroria)", q5597_zone["blocker_summary"])
            self.assertIn("1303 (Southern Auroria)", q5597_zone["blocker_summary"])
            self.assertIn("Quest2.worldZoneId remains 622", q5597_zone["blocker_summary"])
            self.assertIn("reviewed current WorldZone replacement", q5597_zone["blocker_summary"])
            self.assertIn("accepted live client route smoke", q5597_zone["blocker_summary"])
            self.assertEqual("not_retail_complete", q5597_zone["completion_status"])
        finally:
            shutil.rmtree(csv_root, ignore_errors=True)

    def test_quest_zone_evidence_rows_apply_q5597_crimson_isle_quest_zone_override(self) -> None:
        csv_root = Path(tempfile.mkdtemp(prefix="nf-content-audit-q5597-zone-"))
        try:
            zone_map = csv_root / "quest_zone_map.csv"
            zone_map.write_text(
                "\n".join(
                    [
                        "source_relation_id,relation_source,jabbithole_quest_id,quest2_id,jabbithole_quest_name,client_quest_name,jabbithole_zone_id,world_zone_id,jabbithole_zone_name,client_world_zone_name,match_status,enabled,last_seen_in",
                        "633,quest_zones,945,5597,Dregs and Thieves,Dregs and Thieves,9,12,Crimson Isle,,matched,true,3",
                    ]
                )
                + "\n",
                encoding="utf-8",
            )
            strings = {100: "Dregs and Thieves"}
            quests = [{"ID": "5597", "localizedTextIdTitle": "100"}]
            quest_output = [{"quest_id": 5597, "bucket": "curated_full", "completion_status": "not_retail_complete"}]

            rows = audit.build_quest_zone_evidence_rows(strings, quests, quest_output, zone_map)

            self.assertEqual(1, len(rows))
            q5597_zone = rows[0]
            self.assertEqual(
                "datamapping_q5597_crimson_isle_quest_zone_worldzone12_blank_reviewed_current_zone622_pending_route_proof",
                q5597_zone["evidence_status"],
            )
            self.assertIn("world_zone_client_map.csv", q5597_zone["evidence_sources"])
            self.assertIn("quest_zones row 633", q5597_zone["blocker_summary"])
            self.assertIn("WorldZone 12 has no client name", q5597_zone["blocker_summary"])
            self.assertIn("source last_seen_in 3", q5597_zone["blocker_summary"])
            self.assertIn("Quest2.worldZoneId 622", q5597_zone["blocker_summary"])
            self.assertIn("child WorldZone rows 623, 629, 1217, 1218, 1219, 1227, and 1284", q5597_zone["blocker_summary"])
            self.assertIn("reviewed current WorldZone replacement", q5597_zone["blocker_summary"])
            self.assertIn("accepted live client route smoke", q5597_zone["blocker_summary"])
            self.assertEqual("not_retail_complete", q5597_zone["completion_status"])
        finally:
            shutil.rmtree(csv_root, ignore_errors=True)

    def test_quest_episode_evidence_rows_cover_matched_missing_and_noncurrent_rows(self) -> None:
        csv_root = Path(tempfile.mkdtemp(prefix="nf-content-audit-episodes-"))
        try:
            (csv_root / "quest_episode_map.csv").write_text(
                "\n".join(
                    [
                        "jabbithole_episode_id,episode_id,jabbithole_episode_name,client_episode_name,match_status,client_world_zone_id,client_world_zone_name,slug,quests_count,enabled,last_seen_in",
                        "1,20,Source Episode,Client Episode,matched,30,Client Zone,source-episode,2,true,7",
                    ]
                )
                + "\n",
                encoding="utf-8",
            )
            (csv_root / "episode_quest_client_map.csv").write_text(
                "\n".join(
                    [
                        "episode_quest_id,episode_id,episode_name,quest2_id,quest_name,order_index,flags",
                        "100,20,Client Episode,10,Quest Title,1,0",
                        "101,21,Missing Episode,10,Quest Title,2,1",
                        "102,20,Client Episode,9999,Old Quest,3,0",
                    ]
                )
                + "\n",
                encoding="utf-8",
            )
            strings = {100: "Quest Title"}
            quests = [{"ID": "10", "localizedTextIdTitle": "100"}]
            quest_output = [{"quest_id": 10, "bucket": "generic_table_driven", "completion_status": "not_retail_complete"}]

            rows = audit.build_quest_episode_evidence_rows(strings, quests, quest_output, csv_root)

            self.assertEqual(3, len(rows))
            matched = next(row for row in rows if row["episode_quest_id"] == 100)
            self.assertEqual("mapped_episode", matched["episode_bridge_status"])
            self.assertEqual("datamapping_episode_quest_matched_pending_order_progression_smoke", matched["evidence_status"])
            self.assertIn("EpisodeQuest row 100", matched["blocker_summary"])
            self.assertIn("Quest2 10 order 1 flags 0", matched["blocker_summary"])
            self.assertIn("Episode 20 (Client Episode)", matched["blocker_summary"])
            self.assertIn("WorldZone 30 (Client Zone)", matched["blocker_summary"])
            self.assertIn("match_status matched", matched["blocker_summary"])
            self.assertIn("Jabbithole episode 1 (Source Episode)", matched["blocker_summary"])
            self.assertIn("slug source-episode", matched["blocker_summary"])
            self.assertIn("episode quest count 2", matched["blocker_summary"])
            self.assertIn("runtime episode ordering/progression semantics", matched["blocker_summary"])
            missing = next(row for row in rows if row["episode_quest_id"] == 101)
            self.assertEqual("missing_episode_bridge", missing["episode_bridge_status"])
            self.assertEqual("datamapping_episode_quest_missing_episode_bridge_blocked", missing["evidence_status"])
            self.assertIn("jabbithole_mysql/quest_episodes.sql", missing["evidence_sources"])
            self.assertIn("EpisodeQuest row 101", missing["blocker_summary"])
            self.assertIn("Quest2 10 order 2", missing["blocker_summary"])
            self.assertIn("Episode 21", missing["blocker_summary"])
            self.assertIn("quest_episode_map.csv has no bridge", missing["blocker_summary"])
            self.assertIn("current Episode/EpisodeQuest bridge", missing["blocker_summary"])
            noncurrent = next(row for row in rows if row["quest_link_status"] == "non_current_client_quest_reference")
            self.assertEqual("non_current_client_quest_reference", noncurrent["quest_bucket"])
            self.assertEqual("datamapping_episode_not_current_client_quest_row_blocked", noncurrent["evidence_status"])
            self.assertIn("jabbithole_mysql/quest_episodes.sql", noncurrent["evidence_sources"])
            self.assertIn("Quest2 9999", noncurrent["blocker_summary"])
            self.assertIn("not present in the current client Quest2 table", noncurrent["blocker_summary"])
            self.assertTrue(all(row["completion_status"] == "not_retail_complete" for row in rows))
        finally:
            shutil.rmtree(csv_root, ignore_errors=True)

    def test_quest_episode_evidence_rows_apply_q5597_episode_chain_override(self) -> None:
        csv_root = Path(tempfile.mkdtemp(prefix="nf-content-audit-q5597-episode-"))
        try:
            (csv_root / "quest_episode_map.csv").write_text(
                "\n".join(
                    [
                        "jabbithole_episode_id,episode_id,jabbithole_episode_name,client_episode_name,match_status,client_world_zone_id,client_world_zone_name,slug,quests_count,enabled,last_seen_in",
                        "98,464,The Guns of Bloodstone,The Guns of Bloodstone,matched,622,Crimson Isle,the-guns-of-bloodstone-464,3,true,7",
                    ]
                )
                + "\n",
                encoding="utf-8",
            )
            (csv_root / "episode_quest_client_map.csv").write_text(
                "\n".join(
                    [
                        "episode_quest_id,episode_id,episode_name,quest2_id,quest_name,order_index,flags",
                        "2398,464,The Guns of Bloodstone,5597,Dregs and Thieves,2,0",
                    ]
                )
                + "\n",
                encoding="utf-8",
            )
            strings = {100: "Dregs and Thieves"}
            quests = [{"ID": "5597", "localizedTextIdTitle": "100"}]
            quest_output = [{"quest_id": 5597, "bucket": "curated_full", "completion_status": "not_retail_complete"}]

            rows = audit.build_quest_episode_evidence_rows(strings, quests, quest_output, csv_root)

            self.assertEqual(1, len(rows))
            q5597 = rows[0]
            self.assertEqual(
                "runtime_q5597_episode_order_q5596_q5597_q5604_handoff_tested_external_video_observed_pending_local_episode_smoke",
                q5597["evidence_status"],
            )
            self.assertIn("EpisodeQuest row 2398", q5597["blocker_summary"])
            self.assertIn("Episode 464 (The Guns of Bloodstone)", q5597["blocker_summary"])
            self.assertIn("order 2 flags 0", q5597["blocker_summary"])
            self.assertIn("Q5596 -> Q5597 -> Q5604", q5597["blocker_summary"])
            self.assertIn("Q5604TacticalDemolitionsQuestScript granting Q5580/Q5583", q5597["blocker_summary"])
            self.assertIn("external client-flow corroboration", q5597["blocker_summary"])
            self.assertIn("Local episode tracker/order UI presentation", q5597["blocker_summary"])
            self.assertIn("CrimsonIsleQuestChainTests.cs", q5597["evidence_sources"])
            self.assertEqual("not_retail_complete", q5597["completion_status"])
        finally:
            shutil.rmtree(csv_root, ignore_errors=True)

    def test_public_event_evidence_rows_cover_objective_mission_zone_and_creature_maps(self) -> None:
        csv_root = Path(tempfile.mkdtemp(prefix="nf-content-audit-public-events-"))
        try:
            (csv_root / "public_event_objective_map.csv").write_text(
                "\n".join(
                    [
                        "jabbithole_public_event_objective_id,jabbithole_public_event_id,public_event_id,jabbithole_public_event_name,client_public_event_name,public_event_objective_id,match_status,public_event_id_from_objective,jabbithole_objective_text,client_objective_text,jabbithole_objective_type,objective_type,jabbithole_objective_category,objective_category,objective_flags,objective_type_specific_flags,objective_count,objective_object_id,world_location_id,public_event_team_id,public_event_team_name,parent_public_event_objective_id,quest_direction_id,display_order,medal_point_value,failure_time_ms,target_group_id_reward_pane,last_seen_in",
                        "1,1000,10,Source Event,Client Event,20,matched,10,Do event,Do event,0,5,0,0,17,0,1,0,30,1,Public Team,0,40,0,0,0,50,7",
                        "6,1000,10,Source Event,Client Event,21,matched,10,Kill rods,Kill rods,0,20,0,0,17,0,1,14056,30,1,Public Team,0,40,0,0,0,0,7",
                        "7,1000,10,Source Event,Client Event,22,matched,10,Gather here,Gather here,0,6,0,0,17,0,1,8283,33174,1,Public Team,0,40,0,0,0,0,7",
                        "8,1000,10,Source Event,Client Event,23,matched,10,Pass through,Pass through,0,19,0,0,17,0,1,7936,18109,1,Public Team,0,40,0,0,0,0,7",
                        "9,1000,10,Source Event,Client Event,24,matched,10,Kill target group,Kill target group,0,0,0,0,17,0,1,14470,30,1,Public Team,0,40,0,0,0,0,7",
                        "10,1000,10,Source Event,Client Event,25,matched,10,Kill cluster,Kill cluster,0,14,0,0,17,0,1,11121,30,1,Public Team,0,40,0,0,0,0,7",
                        "11,1000,10,Source Event,Client Event,26,matched,10,Collect virtual,Collect virtual,0,22,0,0,17,0,2,265,30,1,Public Team,0,40,0,0,0,0,7",
                        "12,1000,10,Source Event,Client Event,27,matched,10,Kill event unit,Kill event unit,0,1,0,0,17,0,3,0,30,1,Public Team,0,40,0,0,0,0,7",
                        "13,1000,10,Source Event,Client Event,28,matched,10,Kill event objective unit,Kill event objective unit,0,8,0,0,17,0,3,12262,30,1,Public Team,0,40,0,0,0,0,7",
                        "14,1000,10,Source Event,Client Event,29,matched,10,Kill unknown object,Kill unknown object,0,8,0,0,17,0,3,4349,30,1,Public Team,0,40,0,0,0,0,7",
                        "15,1000,10,Source Event,Client Event,30,matched,10,Talk workers,Talk workers,0,17,0,0,17,0,2,700,30,1,Public Team,0,40,0,0,0,0,7",
                    ]
                )
                + "\n",
                encoding="utf-8",
            )
            (csv_root / "client_source_targetgroup_map.csv").write_text(
                "\n".join(
                    [
                        "ID,localizedTextIdDisplayString,localizedTextIdDisplayString_text,type,data0,data1,data2,data3,data4,data5,data6",
                        "700,0,,1,75509,75510,0,0,0,0,0",
                        "12262,0,,1,67940,0,0,0,0,0,0",
                    ]
                )
                + "\n",
                encoding="utf-8",
            )
            (csv_root / "public_event_mission_map.csv").write_text(
                "\n".join(
                    [
                        "source_public_event_mission_id,mission_name,description,parent_jabbithole_public_event_id,parent_public_event_id,parent_public_event_name,client_parent_public_event_name,child_jabbithole_public_event_id,child_public_event_id,child_public_event_name,client_child_public_event_name,last_seen_in",
                        "2,Choose path,,1000,10,Source Event,Client Event,1001,11,Child Event,Client Child,7",
                        "12,Choose text,Text only,1000,10,Source Event,Client Event,,,,,7",
                    ]
                )
                + "\n",
                encoding="utf-8",
            )
            (csv_root / "public_event_zone_map.csv").write_text(
                "\n".join(
                    [
                        "source_public_event_zone_id,jabbithole_public_event_id,public_event_id,public_event_name,client_public_event_name,jabbithole_zone_id,world_zone_id,jabbithole_zone_name,client_zone_name,map_asset,continent_id,last_seen_in",
                        "3,1000,10,Source Event,Client Event,30,40,Source Zone,Client Zone,mapasset,1,7",
                    ]
                )
                + "\n",
                encoding="utf-8",
            )
            (csv_root / "creature_public_event_map.csv").write_text(
                "\n".join(
                    [
                        "source_relation_id,jabbithole_creature_id,creature2_id,source_name,creature2_name,match_status,jabbithole_public_event_id,public_event_id,public_event_name,event_type,reward_type,real_zone_id,parent_public_event_id,last_seen_in",
                        "4,500,600,Source Creature,Client Creature,reviewed,1000,10,Source Event,1,5,40,,7",
                        "6,502,75509,Source Worker,Client Worker,ambiguous_name,1000,10,Source Event,1,5,40,,7",
                        "5,501,,Missing Creature,,unmatched,1002,9999,Old Event,1,0,40,,7",
                    ]
                )
                + "\n",
                encoding="utf-8",
            )
            strings = {100: "Client Event", 101: "Client Child"}
            public_events = [
                {"ID": "10", "localizedTextIdName": "100"},
                {"ID": "11", "localizedTextIdName": "101"},
            ]

            rows = audit.build_public_event_evidence_rows(strings, public_events, csv_root)

            self.assertEqual(17, len(rows))
            objective_rows = [
                row for row in rows if row["public_event_id"] == 10 and row["evidence_kind"] == "public_event_objective"
            ]
            generic_objective = next(row for row in objective_rows if row["related_client_id"] == 20)
            exterminate_objective = next(row for row in objective_rows if row["related_client_id"] == 21)
            trigger_objective = next(row for row in objective_rows if row["related_client_id"] == 22)
            turnstile_objective = next(row for row in objective_rows if row["related_client_id"] == 23)
            kill_target_group_objective = next(row for row in objective_rows if row["related_client_id"] == 24)
            kill_cluster_objective = next(row for row in objective_rows if row["related_client_id"] == 25)
            virtual_collect_objective = next(row for row in objective_rows if row["related_client_id"] == 26)
            event_unit_objective = next(row for row in objective_rows if row["related_client_id"] == 27)
            event_target_group_objective = next(row for row in objective_rows if row["related_client_id"] == 28)
            event_unknown_objective = next(row for row in objective_rows if row["related_client_id"] == 29)
            self.assertEqual(
                "mapped_public_event_objective_blocked_missing_runtime_producer_evidence",
                generic_objective["evidence_status"],
            )
            self.assertIn("type Script (5)", generic_objective["blocker_summary"])
            self.assertIn("reviewed script producer", generic_objective["blocker_summary"])
            self.assertEqual(
                "runtime_public_event_exterminate_target_group_kill_credit_tested_pending_spawn_event_smoke",
                exterminate_objective["evidence_status"],
            )
            self.assertEqual(14056, exterminate_objective["target_group_id"])
            self.assertIn("Source/NexusForever.Game/Entity/UnitEntity.cs", exterminate_objective["evidence_sources"])
            self.assertIn("TargetGroup-backed ObjectId", exterminate_objective["blocker_summary"])
            self.assertEqual(
                "runtime_public_event_participants_trigger_volume_world_location_credit_tested_pending_trigger_placement_smoke",
                trigger_objective["evidence_status"],
            )
            self.assertIn("Source/NexusForever.Game/Entity/Trigger/VolumeGridTriggerEntity.cs", trigger_objective["evidence_sources"])
            self.assertIn("WorldLocation2 33174", trigger_objective["blocker_summary"])
            self.assertEqual(
                "runtime_public_event_turnstile_world_location_entry_credit_tested_pending_trigger_placement_smoke",
                turnstile_objective["evidence_status"],
            )
            self.assertIn("Source/NexusForever.Game/Entity/Trigger/TurnstileTriggerEntity.cs", turnstile_objective["evidence_sources"])
            self.assertIn("WorldLocation2 18109", turnstile_objective["blocker_summary"])
            self.assertEqual(
                "runtime_public_event_kill_target_group_credit_tested_pending_spawn_event_smoke",
                kill_target_group_objective["evidence_status"],
            )
            self.assertEqual(14470, kill_target_group_objective["target_group_id"])
            self.assertIn("Source/NexusForever.Game/Entity/UnitEntity.cs", kill_target_group_objective["evidence_sources"])
            self.assertIn("TargetGroup ObjectId 14470", kill_target_group_objective["blocker_summary"])
            self.assertEqual(
                "runtime_public_event_kill_cluster_target_group_credit_tested_pending_spawn_event_smoke",
                kill_cluster_objective["evidence_status"],
            )
            self.assertEqual(11121, kill_cluster_objective["target_group_id"])
            self.assertIn("Source/NexusForever.Game/Entity/UnitEntity.cs", kill_cluster_objective["evidence_sources"])
            self.assertIn("TargetGroup ObjectId 11121", kill_cluster_objective["blocker_summary"])
            self.assertEqual(
                "runtime_public_event_virtual_collect_loot_delivery_credit_tested_pending_source_smoke",
                virtual_collect_objective["evidence_status"],
            )
            self.assertIn("Source/NexusForever.Game/Loot/LootInstanceItem.cs", virtual_collect_objective["evidence_sources"])
            self.assertIn("virtual item ObjectId 265", virtual_collect_objective["blocker_summary"])
            self.assertEqual(
                "runtime_public_event_event_owned_unit_kill_credit_tested_pending_event_spawn_smoke",
                event_unit_objective["evidence_status"],
            )
            self.assertEqual("", event_unit_objective["target_group_id"])
            self.assertIn("object 0 event-owned-unit credit", event_unit_objective["blocker_summary"])
            self.assertIn("Source/NexusForever.Game.Tests/Entity/UnitEntityDamageResultTests.cs", event_unit_objective["evidence_sources"])
            self.assertEqual(
                "runtime_public_event_event_owned_unit_kill_credit_tested_pending_event_spawn_smoke",
                event_target_group_objective["evidence_status"],
            )
            self.assertEqual(12262, event_target_group_objective["target_group_id"])
            self.assertIn("TargetGroup-backed ObjectId 12262", event_target_group_objective["blocker_summary"])
            self.assertEqual(
                "mapped_public_event_objective_blocked_missing_runtime_producer_evidence",
                event_unknown_objective["evidence_status"],
            )
            self.assertIn("TargetGroup-backed ObjectId", event_unknown_objective["blocker_summary"])
            mission = next(row for row in rows if row["evidence_kind"] == "public_event_mission" and row["source_row_id"] == 2)
            self.assertEqual(
                "runtime_public_event_mission_child_event_template_routing_tested_pending_transition_ui_and_client_smoke",
                mission["evidence_status"],
            )
            mission_text = next(row for row in rows if row["evidence_kind"] == "public_event_mission" and row["source_row_id"] == 12)
            self.assertEqual(
                "mapped_public_event_mission_text_blocked_missing_child_event_transition_evidence",
                mission_text["evidence_status"],
            )
            self.assertIn("child PublicEvent missing", mission_text["blocker_summary"])
            zone = next(row for row in rows if row["evidence_kind"] == "public_event_zone")
            self.assertEqual("mapped_public_event_zone_blocked_missing_runtime_visibility_bridge_evidence", zone["evidence_status"])
            self.assertIn("WorldZone 40", zone["blocker_summary"])
            creature = next(row for row in rows if row["evidence_kind"] == "public_event_creature" and row["public_event_id"] == 10)
            self.assertEqual("reviewed_public_event_creature_blocked_spawn_credit_smoke", creature["evidence_status"])
            self.assertIn("reviewed Creature2 bridge", creature["blocker_summary"])
            multi_member_creature = next(row for row in rows if row["evidence_kind"] == "public_event_creature" and row["source_row_id"] == 6)
            self.assertEqual(
                "blocked_public_event_creature_ambiguous_multi_member_targetgroup_requires_relation_smoke",
                multi_member_creature["evidence_status"],
            )
            self.assertEqual("700", multi_member_creature["target_group_id"])
            self.assertIn("objective 30 TargetGroup 700 members 75509;75510", multi_member_creature["blocker_summary"])
            self.assertIn("one-to-one global creature bridge", multi_member_creature["blocker_summary"])
            self.assertIn("relation- or spawn-scoped bridge review", multi_member_creature["blocker_summary"])
            self.assertIn("client_source_targetgroup_map.csv", multi_member_creature["evidence_sources"])
            noncurrent = next(row for row in rows if row["public_event_link_status"] == "non_current_public_event_reference")
            self.assertEqual("datamapping_public_event_not_current_client_event_row_blocked", noncurrent["evidence_status"])
            self.assertTrue(all(row["completion_status"] == "not_retail_complete" for row in rows))
        finally:
            shutil.rmtree(csv_root, ignore_errors=True)

    def test_stormtalon_public_event_creature_rows_use_runtime_exemptions(self) -> None:
        csv_root = Path(tempfile.mkdtemp(prefix="nf-content-audit-stormtalon-pe-creatures-"))
        try:
            (csv_root / "public_event_objective_map.csv").write_text(
                "jabbithole_public_event_objective_id,jabbithole_public_event_id,public_event_id,jabbithole_public_event_name,client_public_event_name,public_event_objective_id,match_status,public_event_id_from_objective,jabbithole_objective_text,client_objective_text,jabbithole_objective_type,objective_type,jabbithole_objective_category,objective_category,objective_flags,objective_type_specific_flags,objective_count,objective_object_id,world_location_id,public_event_team_id,public_event_team_name,parent_public_event_objective_id,quest_direction_id,display_order,medal_point_value,failure_time_ms,target_group_id_reward_pane,last_seen_in\n",
                encoding="utf-8",
            )
            (csv_root / "client_source_targetgroup_map.csv").write_text(
                "ID,localizedTextIdDisplayString,localizedTextIdDisplayString_text,type,data0,data1,data2,data3,data4,data5,data6\n",
                encoding="utf-8",
            )
            (csv_root / "public_event_mission_map.csv").write_text(
                "source_public_event_mission_id,mission_name,description,parent_jabbithole_public_event_id,parent_public_event_id,parent_public_event_name,client_parent_public_event_name,child_jabbithole_public_event_id,child_public_event_id,child_public_event_name,client_child_public_event_name,last_seen_in\n",
                encoding="utf-8",
            )
            (csv_root / "public_event_zone_map.csv").write_text(
                "source_public_event_zone_id,jabbithole_public_event_id,public_event_id,public_event_name,client_public_event_name,jabbithole_zone_id,world_zone_id,jabbithole_zone_name,client_zone_name,map_asset,continent_id,last_seen_in\n",
                encoding="utf-8",
            )
            (csv_root / "creature_public_event_map.csv").write_text(
                "\n".join(
                    [
                        "source_relation_id,jabbithole_creature_id,creature2_id,source_name,creature2_name,match_status,jabbithole_public_event_id,public_event_id,public_event_name,event_type,reward_type,real_zone_id,parent_public_event_id,last_seen_in",
                        "2,109,17160,Blade-Wind the Invoker,Blade-Wind the Invoker,scored_name,4,145,Stormtalon's Lair,1,5,271,,7",
                        "10,837,17191,Thundercall Cage,Thundercall Cage,unique_name,4,145,Stormtalon's Lair,1,5,271,,7",
                        "13,967,33361,Overseer Drift-Catcher,Overseer Drift-Catcher,scored_name,4,145,Stormtalon's Lair,1,5,271,,7",
                        "999,1,17154,Thundercall Disciple,Thundercall Disciple,scored_name,4,145,Stormtalon's Lair,1,5,271,,7",
                    ]
                )
                + "\n",
                encoding="utf-8",
            )
            strings = {1450: "Stormtalon's Lair"}
            public_events = [{"ID": "145", "localizedTextIdName": "1450"}]

            rows = audit.build_public_event_evidence_rows(strings, public_events, csv_root)

            self.assertEqual(4, len(rows))
            blade_wind = next(row for row in rows if row["source_row_id"] == 2)
            cage = next(row for row in rows if row["source_row_id"] == 10)
            overseer = next(row for row in rows if row["source_row_id"] == 13)
            disciple = next(row for row in rows if row["source_row_id"] == 999)

            self.assertEqual(
                "runtime_stormtalon_blade_wind_public_event_creature_death_credit_tested_pending_boss_mechanics_rewards_and_client_smoke",
                blade_wind["evidence_status"],
            )
            self.assertIn("BladeWindTheInvokerVeteranEntityScript.cs", blade_wind["evidence_sources"])
            self.assertIn("no longer double-counted", blade_wind["blocker_summary"])
            self.assertFalse(audit.is_public_event_evidence_blocker(blade_wind))

            self.assertEqual(
                "runtime_stormtalon_thundercall_cage_public_event_creature_spawned_and_checklist_credit_tested_pending_route_visual_state_and_client_smoke",
                cage["evidence_status"],
            )
            self.assertIn("StormtalonOptionalObjectiveEntityScripts.cs", cage["evidence_sources"])
            self.assertIn("source coordinates 3241-3244", cage["blocker_summary"])
            self.assertFalse(audit.is_public_event_evidence_blocker(cage))

            self.assertEqual(
                "runtime_stormtalon_overseer_drift_catcher_public_event_creature_death_credit_tested_pending_route_combat_client_smoke",
                overseer["evidence_status"],
            )
            self.assertIn("StormtalonMinibossEntityScripts.cs", overseer["evidence_sources"])
            self.assertFalse(audit.is_public_event_evidence_blocker(overseer))

            self.assertEqual(
                "mapped_public_event_creature_blocked_missing_reviewed_bridge_spawn_credit_smoke",
                disciple["evidence_status"],
            )
            self.assertTrue(audit.is_public_event_evidence_blocker(disciple))
            self.assertTrue(all(row["completion_status"] == "not_retail_complete" for row in rows))
        finally:
            shutil.rmtree(csv_root, ignore_errors=True)

    def test_ultimate_protogames_bevorage_producer_rows_are_concrete(self) -> None:
        dependency_statuses = {
            2680: "runtime_bevorage_phase_activation_and_boss_death_credit_tested",
            3206: "runtime_bevorage_caffeinated_child_objective_activation_and_death_credit_tested",
            3210: "runtime_bevorage_out_of_order_child_objective_activation_and_death_credit_tested",
            4669: "runtime_bevorage_vendatron_target_group_activation_credit_tested",
        }

        for objective_id, evidence_status in dependency_statuses.items():
            with self.subTest(source="dependency", objective_id=objective_id):
                dependency = audit.INSTANCE_OBJECTIVE_DEPENDENCY_OVERRIDES[(2980, objective_id)]
                self.assertIn("UltimateProtogames", dependency["script_path"])
                self.assertEqual(evidence_status, dependency["evidence_status"])

        event_script = r"Source\NexusForever.Script.Instance\Dungeon\UltimateProtogames\UltimateProtogamesEventScript.cs"
        entity_script = r"Source\NexusForever.Script.Instance\Dungeon\UltimateProtogames\Script\BevORageEntityScript.cs"
        producer_statuses = {
            (event_script, "2680", "12179"): "runtime_bevorage_defeat_objective_phase_activation_tested",
            (event_script, "4669", "12549"): "runtime_bevorage_vendatron_objective_phase_activation_tested",
            (event_script, "3206", ""): "runtime_bevorage_caffeinated_child_objective_phase_activation_tested",
            (event_script, "3210", ""): "runtime_bevorage_out_of_order_child_objective_phase_activation_tested",
            (entity_script, "2680", "12179"): "runtime_bevorage_boss_death_objective_credit_tested",
            (entity_script, "3206", ""): "runtime_bevorage_caffeinated_child_objective_death_credit_tested",
            (entity_script, "3210", ""): "runtime_bevorage_out_of_order_child_objective_death_credit_tested",
            (entity_script, "4669", "12549"): "runtime_bevorage_vendatron_target_group_activation_credit_tested",
        }

        for key, evidence_status in producer_statuses.items():
            with self.subTest(source="producer", key=key):
                producer = audit.SCRIPT_OBJECTIVE_PRODUCER_OVERRIDES[key]
                self.assertIn("script_owner", producer["owner_link_status"])
                self.assertEqual(evidence_status, producer["evidence_status"])

    def test_ultimate_protogames_public_event_creature_rows_use_runtime_exemptions(self) -> None:
        csv_root = Path(tempfile.mkdtemp(prefix="nf-content-audit-up-pe-creatures-"))
        try:
            (csv_root / "public_event_objective_map.csv").write_text(
                "jabbithole_public_event_objective_id,jabbithole_public_event_id,public_event_id,jabbithole_public_event_name,client_public_event_name,public_event_objective_id,match_status,public_event_id_from_objective,jabbithole_objective_text,client_objective_text,jabbithole_objective_type,objective_type,jabbithole_objective_category,objective_category,objective_flags,objective_type_specific_flags,objective_count,objective_object_id,world_location_id,public_event_team_id,public_event_team_name,parent_public_event_objective_id,quest_direction_id,display_order,medal_point_value,failure_time_ms,target_group_id_reward_pane,last_seen_in\n",
                encoding="utf-8",
            )
            (csv_root / "client_source_targetgroup_map.csv").write_text(
                "ID,localizedTextIdDisplayString,localizedTextIdDisplayString_text,type,data0,data1,data2,data3,data4,data5,data6\n",
                encoding="utf-8",
            )
            (csv_root / "public_event_mission_map.csv").write_text(
                "source_public_event_mission_id,mission_name,description,parent_jabbithole_public_event_id,parent_public_event_id,parent_public_event_name,client_parent_public_event_name,child_jabbithole_public_event_id,child_public_event_id,child_public_event_name,client_child_public_event_name,last_seen_in\n",
                encoding="utf-8",
            )
            (csv_root / "public_event_zone_map.csv").write_text(
                "source_public_event_zone_id,jabbithole_public_event_id,public_event_id,public_event_name,client_public_event_name,jabbithole_zone_id,world_zone_id,jabbithole_zone_name,client_zone_name,map_asset,continent_id,last_seen_in\n",
                encoding="utf-8",
            )
            (csv_root / "creature_public_event_map.csv").write_text(
                "\n".join(
                    [
                        "source_relation_id,jabbithole_creature_id,creature2_id,source_name,creature2_name,match_status,jabbithole_public_event_id,public_event_id,public_event_name,event_type,reward_type,real_zone_id,parent_public_event_id,last_seen_in",
                        "2186,28283,61463,Bev-O-Rage,Bev-O-Rage,reviewed,299,594,Ultimate Protogames,1,5,4330,,7",
                        "2244,28533,62427,Gate Console,Gate Console,reviewed,299,594,Ultimate Protogames,1,5,4333,,7",
                        "2232,28491,62546,Malfunctioning Yellow Tank,Malfunctioning Yellow Tank,reviewed,299,594,Ultimate Protogames,1,5,4336,,7",
                        "1993,27987,62472,Frenzied Gale,Frenzied Gale,unique_name,299,594,Ultimate Protogames,1,5,4330,,7",
                    ]
                )
                + "\n",
                encoding="utf-8",
            )
            strings = {5940: "Ultimate Protogames"}
            public_events = [{"ID": "594", "localizedTextIdName": "5940"}]

            rows = audit.build_public_event_evidence_rows(strings, public_events, csv_root)

            self.assertEqual(4, len(rows))
            bevorage = next(row for row in rows if row["source_row_id"] == 2186)
            gate_console = next(row for row in rows if row["source_row_id"] == 2244)
            tank = next(row for row in rows if row["source_row_id"] == 2232)
            ambient_creature = next(row for row in rows if row["source_row_id"] == 1993)

            self.assertEqual(
                "runtime_up_bevorage_public_event_creature_death_credit_tested_pending_route_boss_rewards_and_client_smoke",
                bevorage["evidence_status"],
            )
            self.assertIn("BevORageEntityScript.cs", bevorage["evidence_sources"])
            self.assertFalse(audit.is_public_event_evidence_blocker(bevorage))

            self.assertEqual(
                "runtime_up_gate_console_public_event_creature_spawned_and_activation_credit_tested_pending_alias_client_smoke",
                gate_console["evidence_status"],
            )
            self.assertIn("SneakyPrisonEntityScripts.cs", gate_console["evidence_sources"])
            self.assertIn("62987/62427 alias bridge", gate_console["blocker_summary"])
            self.assertFalse(audit.is_public_event_evidence_blocker(gate_console))

            self.assertEqual(
                "runtime_up_tank_room_public_event_creature_spawned_and_death_credit_tested_pending_timer_visual_client_smoke",
                tank["evidence_status"],
            )
            self.assertIn("MalfunctioningTankEntityScript", tank["blocker_summary"])
            self.assertFalse(audit.is_public_event_evidence_blocker(tank))

            self.assertEqual(
                "mapped_public_event_creature_blocked_missing_reviewed_bridge_spawn_credit_smoke",
                ambient_creature["evidence_status"],
            )
            self.assertTrue(audit.is_public_event_evidence_blocker(ambient_creature))
            self.assertTrue(all(row["completion_status"] == "not_retail_complete" for row in rows))
        finally:
            shutil.rmtree(csv_root, ignore_errors=True)

    def test_sanctuary_public_event_creature_rows_use_runtime_exemptions(self) -> None:
        csv_root = Path(tempfile.mkdtemp(prefix="nf-content-audit-sanctuary-pe-creatures-"))
        try:
            (csv_root / "public_event_objective_map.csv").write_text(
                "jabbithole_public_event_objective_id,jabbithole_public_event_id,public_event_id,jabbithole_public_event_name,client_public_event_name,public_event_objective_id,match_status,public_event_id_from_objective,jabbithole_objective_text,client_objective_text,jabbithole_objective_type,objective_type,jabbithole_objective_category,objective_category,objective_flags,objective_type_specific_flags,objective_count,objective_object_id,world_location_id,public_event_team_id,public_event_team_name,parent_public_event_objective_id,quest_direction_id,display_order,medal_point_value,failure_time_ms,target_group_id_reward_pane,last_seen_in\n",
                encoding="utf-8",
            )
            (csv_root / "client_source_targetgroup_map.csv").write_text(
                "ID,localizedTextIdDisplayString,localizedTextIdDisplayString_text,type,data0,data1,data2,data3,data4,data5,data6\n",
                encoding="utf-8",
            )
            (csv_root / "public_event_mission_map.csv").write_text(
                "source_public_event_mission_id,mission_name,description,parent_jabbithole_public_event_id,parent_public_event_id,parent_public_event_name,client_parent_public_event_name,child_jabbithole_public_event_id,child_public_event_id,child_public_event_name,client_child_public_event_name,last_seen_in\n",
                encoding="utf-8",
            )
            (csv_root / "public_event_zone_map.csv").write_text(
                "source_public_event_zone_id,jabbithole_public_event_id,public_event_id,public_event_name,client_public_event_name,jabbithole_zone_id,world_zone_id,jabbithole_zone_name,client_zone_name,map_asset,continent_id,last_seen_in\n",
                encoding="utf-8",
            )
            (csv_root / "creature_public_event_map.csv").write_text(
                "\n".join(
                    [
                        "source_relation_id,jabbithole_creature_id,creature2_id,source_name,creature2_name,match_status,jabbithole_public_event_id,public_event_id,public_event_name,event_type,reward_type,real_zone_id,parent_public_event_id,last_seen_in",
                        "543,6774,28599,Deadringer Shallaos,Deadringer Shallaos,scored_name,53,166,Sanctuary of the Swordmaiden,1,5,53,,7",
                        "535,6858,28732,Rayna Darkspeaker,Rayna Darkspeaker,ambiguous_name,53,166,Sanctuary of the Swordmaiden,1,5,53,,7",
                        "1102,6864,28728,Moldwood Overlord Skash,Moldwood Overlord Skash,ambiguous_name,53,166,Sanctuary of the Swordmaiden,1,5,53,,7",
                        "1387,18040,43171,Lifeweaver Tech Cluster,Lifeweaver Tech Cluster,unique_name,53,166,Sanctuary of the Swordmaiden,1,5,53,,7",
                        "1252,16941,29255,Flame-Crazed Demon,Flame-Crazed Demon,scored_name,53,166,Sanctuary of the Swordmaiden,1,5,53,,7",
                        "533,6736,28647,Zealous Lifesinger,Zealous Lifesinger,scored_name,53,166,Sanctuary of the Swordmaiden,1,5,53,,7",
                    ]
                )
                + "\n",
                encoding="utf-8",
            )
            strings = {1660: "Sanctuary of the Swordmaiden"}
            public_events = [{"ID": "166", "localizedTextIdName": "1660"}]

            rows = audit.build_public_event_evidence_rows(strings, public_events, csv_root)

            self.assertEqual(6, len(rows))
            deadringer = next(row for row in rows if row["source_row_id"] == 543)
            rayna = next(row for row in rows if row["source_row_id"] == 535)
            skash = next(row for row in rows if row["source_row_id"] == 1102)
            tech_cluster = next(row for row in rows if row["source_row_id"] == 1387)
            flame_crazed_demon = next(row for row in rows if row["source_row_id"] == 1252)
            ambient_creature = next(row for row in rows if row["source_row_id"] == 533)

            self.assertEqual(
                "runtime_sanctuary_deadringer_shallaos_public_event_creature_death_credit_tested_pending_boss_mechanics_spawn_rewards_and_client_smoke",
                deadringer["evidence_status"],
            )
            self.assertIn("DeadringerShallaosEntityScript", deadringer["blocker_summary"])
            self.assertFalse(audit.is_public_event_evidence_blocker(deadringer))

            self.assertEqual(
                "runtime_sanctuary_rayna_darkspeaker_public_event_creature_death_credit_tested_pending_route_boss_mechanics_rewards_and_client_smoke",
                rayna["evidence_status"],
            )
            self.assertIn("normal/veteran Rayna rows", rayna["blocker_summary"])
            self.assertFalse(audit.is_public_event_evidence_blocker(rayna))

            self.assertEqual(
                "runtime_sanctuary_moldwood_overlord_skash_public_event_creature_death_credit_tested_pending_route_boss_mechanics_rewards_and_client_smoke",
                skash["evidence_status"],
            )
            self.assertIn("Skash objective-credit scripts", skash["blocker_summary"])
            self.assertFalse(audit.is_public_event_evidence_blocker(skash))

            self.assertEqual(
                "runtime_sanctuary_lifeweaver_tech_cluster_public_event_creature_checklist_credit_tested_pending_placement_visual_state_and_client_smoke",
                tech_cluster["evidence_status"],
            )
            self.assertIn("LifeweaverTechClusterEntityScript", tech_cluster["blocker_summary"])
            self.assertFalse(audit.is_public_event_evidence_blocker(tech_cluster))

            self.assertEqual(
                "mapped_public_event_creature_blocked_missing_reviewed_bridge_spawn_credit_smoke",
                flame_crazed_demon["evidence_status"],
            )
            self.assertTrue(audit.is_public_event_evidence_blocker(flame_crazed_demon))

            self.assertEqual(
                "mapped_public_event_creature_blocked_missing_reviewed_bridge_spawn_credit_smoke",
                ambient_creature["evidence_status"],
            )
            self.assertTrue(audit.is_public_event_evidence_blocker(ambient_creature))
            self.assertTrue(all(row["completion_status"] == "not_retail_complete" for row in rows))
        finally:
            shutil.rmtree(csv_root, ignore_errors=True)

    def test_sanctuary_public_event_script_objective_rows_use_runtime_overrides(self) -> None:
        csv_root = Path(tempfile.mkdtemp(prefix="nf-content-audit-sanctuary-pe-script-objectives-"))
        try:
            (csv_root / "public_event_objective_map.csv").write_text(
                "\n".join(
                    [
                        "jabbithole_public_event_objective_id,jabbithole_public_event_id,public_event_id,jabbithole_public_event_name,client_public_event_name,public_event_objective_id,match_status,public_event_id_from_objective,jabbithole_objective_text,client_objective_text,jabbithole_objective_type,objective_type,jabbithole_objective_category,objective_category,objective_flags,objective_type_specific_flags,objective_count,objective_object_id,world_location_id,public_event_team_id,public_event_team_name,parent_public_event_objective_id,quest_direction_id,display_order,medal_point_value,failure_time_ms,target_group_id_reward_pane,last_seen_in",
                        "497,53,166,Sanctuary of the Swordmaiden,Sanctuary of the Swordmaiden,497,matched,166,Use Soul Spore,Use the Soul Spore on Moldwood Gorgers,Script,5,,,0,0,3,0,22133,,,,0,0,0,0,0,7",
                        "627,53,166,Sanctuary of the Swordmaiden,Sanctuary of the Swordmaiden,627,matched,166,Kill distracted Moldwood Maulers,Kill Moldwood Maulers after distracting them with the decoy splorg,Script,5,,,0,0,5,3391,23186,,,,0,0,0,3391,7",
                        "1503,53,166,Sanctuary of the Swordmaiden,Sanctuary of the Swordmaiden,1503,matched,166,Dodge Totem,Dodge Torine Totem of Flame,Script,5,,,0,0,1,6005,22136,,,,0,0,0,0,7",
                    ]
                )
                + "\n",
                encoding="utf-8",
            )
            (csv_root / "client_source_targetgroup_map.csv").write_text(
                "ID,localizedTextIdDisplayString,localizedTextIdDisplayString_text,type,data0,data1,data2,data3,data4,data5,data6\n"
                "3391,0,Distracted Moldwood Maulers,0,29311,29310,72988,72999,0,0,0\n",
                encoding="utf-8",
            )
            (csv_root / "public_event_mission_map.csv").write_text(
                "source_public_event_mission_id,mission_name,description,parent_jabbithole_public_event_id,parent_public_event_id,parent_public_event_name,client_parent_public_event_name,child_jabbithole_public_event_id,child_public_event_id,child_public_event_name,client_child_public_event_name,last_seen_in\n",
                encoding="utf-8",
            )
            (csv_root / "public_event_zone_map.csv").write_text(
                "source_public_event_zone_id,jabbithole_public_event_id,public_event_id,public_event_name,client_public_event_name,jabbithole_zone_id,world_zone_id,jabbithole_zone_name,client_zone_name,map_asset,continent_id,last_seen_in\n",
                encoding="utf-8",
            )
            (csv_root / "creature_public_event_map.csv").write_text(
                "source_relation_id,jabbithole_creature_id,creature2_id,source_name,creature2_name,match_status,jabbithole_public_event_id,public_event_id,public_event_name,event_type,reward_type,real_zone_id,parent_public_event_id,last_seen_in\n",
                encoding="utf-8",
            )
            strings = {1660: "Sanctuary of the Swordmaiden"}
            public_events = [{"ID": "166", "localizedTextIdName": "1660"}]

            rows = audit.build_public_event_evidence_rows(strings, public_events, csv_root)

            soul_spore = next(row for row in rows if row["related_client_id"] == 497)
            mauler = next(row for row in rows if row["related_client_id"] == 627)
            totem_dodge = next(row for row in rows if row["related_client_id"] == 1503)

            self.assertEqual(
                "runtime_sanctuary_soul_spore_script_objective_credit_tested_pending_route_spawn_and_client_smoke",
                soul_spore["evidence_status"],
            )
            self.assertIn("SoulSporeEntityScript", soul_spore["blocker_summary"])
            self.assertEqual("automated_focus_pass_pending_optional_route_spawn_rewards_and_client_smoke", soul_spore["manual_validation"])
            self.assertFalse(audit.is_public_event_evidence_blocker(soul_spore))

            self.assertEqual(
                "runtime_sanctuary_distracted_moldwood_mauler_script_objective_credit_tested_pending_route_spawn_and_client_smoke",
                mauler["evidence_status"],
            )
            self.assertIn("DistractedMoldwoodMaulerEntityScript", mauler["blocker_summary"])
            self.assertEqual("automated_focus_pass_pending_optional_route_spawn_rewards_and_client_smoke", mauler["manual_validation"])
            self.assertFalse(audit.is_public_event_evidence_blocker(mauler))

            self.assertEqual(
                "mapped_public_event_objective_blocked_missing_runtime_producer_evidence",
                totem_dodge["evidence_status"],
            )
            self.assertIn("missing a reviewed script producer", totem_dodge["blocker_summary"])
            self.assertTrue(all(row["completion_status"] == "not_retail_complete" for row in rows))
        finally:
            shutil.rmtree(csv_root, ignore_errors=True)

    def test_public_event_auxiliary_evidence_rows_cover_object_refs_and_script_votes(self) -> None:
        csv_root = Path(tempfile.mkdtemp(prefix="nf-content-audit-public-event-aux-"))
        script_root = Path(tempfile.mkdtemp(prefix="nf-content-audit-public-event-votes-"))
        try:
            (csv_root / "public_event_depot_client_map.csv").write_text(
                "\n".join(
                    [
                        "public_event_depot_id,creature2_id,creature2_name,item2_id,item_name",
                        "2,21131,Expedition Depot,13050,Expedition Supplies",
                        "12,19986,JVB Depot,12834,JVB Supplies",
                    ]
                )
                + "\n",
                encoding="utf-8",
            )
            (csv_root / "public_event_virtual_item_depot_client_map.csv").write_text(
                "\n".join(
                    [
                        "public_event_virtual_item_depot_id,creature2_id,creature2_name,virtual_item_slot,virtual_item_id,virtual_item_name,button_icon,item2_type_id,item_quality_id",
                        "3,47943,Spiny Fragment Depot,0,99,Spiny Fragment,icon,0,0",
                        "13,19986,JVB Depot,0,100,JVB Fury,icon,0,0",
                    ]
                )
                + "\n",
                encoding="utf-8",
            )
            (csv_root / "client_source_publiceventcustomstat_map.csv").write_text(
                "\n".join(
                    [
                        "ID,publicEventTypeEnum,publicEventId,publicEventId_label,statIndex,localizedTextIdStatName,localizedTextIdStatName_text,iconPath",
                        "4,21,10,Client Event,0,1000,Damage Done,icon",
                        "14,21,0,,2,1001,Resources Deposited,icon",
                    ]
                )
                + "\n",
                encoding="utf-8",
            )
            (csv_root / "client_source_publiceventstatdisplay_map.csv").write_text(
                "\n".join(
                    [
                        "ID,publicEventTypeEnum,publicEventId,publicEventId_label,flags",
                        "5,21,0,,133049",
                    ]
                )
                + "\n",
                encoding="utf-8",
            )
            (csv_root / "client_source_publiceventrewardmodifier_map.csv").write_text(
                "\n".join(
                    [
                        "ID,publicEventId,publicEventId_label,rewardPropertyId,data,offset",
                        "6,10,Client Event,39,15,0.5",
                    ]
                )
                + "\n",
                encoding="utf-8",
            )
            (csv_root / "client_source_publiceventobjectivebombdeployment_map.csv").write_text(
                "\n".join(
                    [
                        "ID,creature2IdBomb,creature2IdBomb_label,spell4IdCarrying,spell4IdCarrying_label",
                        "7,11550,PvP Time Bomb,40862,Time Bomb",
                    ]
                )
                + "\n",
                encoding="utf-8",
            )
            (csv_root / "client_source_publiceventobjectivegatherresource_map.csv").write_text(
                "\n".join(
                    [
                        "ID,publicEventObjectiveGatherResourceEnumFlag,creature2IdContainer,creature2IdContainer_label,creature2IdResource,creature2IdResource_label,spell4IdResource,spell4IdResource_label,creature2IdStolenResource,creature2IdStolenResource_label,spell4IdStolenResource,spell4IdStolenResource_label,publicEventObjectiveGatherResourceIdOpposing",
                        "8,1,10416,Blue Team Moodie Totem,3009,Moodie Mask,1495,Mask Carrier,3011,Blue Team Moodie Mask,1490,Blue Mask Carrier,9",
                    ]
                )
                + "\n",
                encoding="utf-8",
            )
            (csv_root / "client_source_publiceventobjectivestate_map.csv").write_text(
                "\n".join(
                    [
                        "ID,localizedTextIdState00,localizedTextIdState00_text,localizedTextIdState01,localizedTextIdState01_text,localizedTextIdState02,localizedTextIdState02_text,localizedTextIdState03,localizedTextIdState03_text,localizedTextIdState04,localizedTextIdState04_text,localizedTextIdState05,localizedTextIdState05_text,localizedTextIdState06,localizedTextIdState06_text,localizedTextIdState07,localizedTextIdState07_text,localizedTextIdState08,localizedTextIdState08_text,localizedTextIdState09,localizedTextIdState09_text,localizedTextIdState10,localizedTextIdState10_text,localizedTextIdState11,localizedTextIdState11_text,localizedTextIdState12,localizedTextIdState12_text,localizedTextIdState13,localizedTextIdState13_text,localizedTextIdState14,localizedTextIdState14_text,localizedTextIdState15,localizedTextIdState15_text",
                        "9,2000,State A,2001,State B,0,,0,,0,,0,,0,,0,,0,,0,,0,,0,,0,,0,,0,,0,,0,,0,",
                    ]
                )
                + "\n",
                encoding="utf-8",
            )
            (csv_root / "client_source_publiceventvote_map.csv").write_text(
                "\n".join(
                    [
                        "ID,localizedTextIdTitle,localizedTextIdTitle_text,localizedTextIdDescription,localizedTextIdDescription_text,localizedTextIdOption00,localizedTextIdOption00_text,localizedTextIdOption01,localizedTextIdOption01_text,localizedTextIdOption02,localizedTextIdOption02_text,localizedTextIdOption03,localizedTextIdOption03_text,localizedTextIdOption04,localizedTextIdOption04_text,localizedTextIdLabel00,localizedTextIdLabel00_text,localizedTextIdLabel01,localizedTextIdLabel01_text,localizedTextIdLabel02,localizedTextIdLabel02_text,localizedTextIdLabel03,localizedTextIdLabel03_text,localizedTextIdLabel04,localizedTextIdLabel04_text,durationMS,assetPathSprite",
                        "64,3000,Choose Your Path,3001,Choose,3002,Option A,3003,Option B,0,,0,,0,,4000,A,4001,B,0,,0,,0,,90000,",
                        "65,3004,Unused Vote,3005,Unused,3006,Only Option,0,,0,,0,,0,,4002,A,0,,0,,0,,0,,30000,",
                    ]
                )
                + "\n",
                encoding="utf-8",
            )
            (script_root / "ExampleEventScript.cs").write_text(
                """
public class ExampleEventScript
{
    private const uint ChoosePathVoteId = 64u;

    public void Run(dynamic publicEvent)
    {
        publicEvent.StartVote(PublicEventTeam.PublicTeam, ChoosePathVoteId, 1u);
    }
}
""".lstrip(),
                encoding="utf-8",
            )
            strings = {100: "Client Event"}
            public_events = [{"ID": "10", "localizedTextIdName": "100"}]
            public_event_objectives = [
                {"ID": "101", "publicEventId": "10", "publicEventObjectiveTypeEnum": "7", "objectId": "2"},
                {"ID": "102", "publicEventId": "10", "publicEventObjectiveTypeEnum": "22", "objectId": "99"},
                {"ID": "103", "publicEventId": "10", "publicEventObjectiveTypeEnum": "31", "objectId": "7"},
                {"ID": "104", "publicEventId": "10", "publicEventObjectiveTypeEnum": "26", "objectId": "8"},
                {"ID": "105", "publicEventId": "10", "publicEventObjectiveTypeEnum": "24", "objectId": "9"},
                {"ID": "106", "publicEventId": "10", "publicEventObjectiveTypeEnum": "7", "objectId": "12"},
            ]

            rows = audit.build_public_event_auxiliary_evidence_rows(strings, public_events, public_event_objectives, csv_root, script_root)

            self.assertEqual(13, len(rows))
            depot = next(row for row in rows if row["evidence_kind"] == "public_event_depot" and row["source_row_id"] == 2)
            self.assertEqual("101", depot["linked_public_event_objective_ids"])
            self.assertEqual("blocked_public_event_depot_objective_linked_missing_depot_interaction_evidence", depot["evidence_status"])
            depot_with_choices = next(row for row in rows if row["evidence_kind"] == "public_event_depot" and row["source_row_id"] == 12)
            self.assertEqual("106", depot_with_choices["linked_public_event_objective_ids"])
            self.assertEqual(
                "runtime_public_event_depot_virtual_item_choices_tested_pending_depot_interaction_smoke",
                depot_with_choices["evidence_status"],
            )
            self.assertIn("PublicEventTemplate.cs", depot_with_choices["evidence_sources"])
            self.assertIn("PublicEventDataBackedMetadataTests.cs", depot_with_choices["evidence_sources"])
            virtual = next(row for row in rows if row["evidence_kind"] == "public_event_virtual_item_depot" and row["source_row_id"] == 3)
            self.assertEqual("102", virtual["linked_public_event_objective_ids"])
            self.assertEqual(
                "runtime_public_event_virtual_item_depot_objective_linked_virtual_collect_credit_tested_pending_depot_acquisition_smoke",
                virtual["evidence_status"],
            )
            self.assertIn("LootInstanceItem.cs", virtual["evidence_sources"])
            self.assertIn("LootInstanceDeliveryTests.cs", virtual["evidence_sources"])
            self.assertIn("PublicEventFlowTests.cs", virtual["evidence_sources"])
            virtual_choice = next(row for row in rows if row["evidence_kind"] == "public_event_virtual_item_depot" and row["source_row_id"] == 13)
            self.assertEqual("106", virtual_choice["linked_public_event_objective_ids"])
            self.assertEqual(7, virtual_choice["objective_type"])
            self.assertEqual(12, virtual_choice["objective_object_id"])
            self.assertEqual(
                "runtime_public_event_virtual_item_depot_interact_depot_choices_tested_pending_depot_acquisition_smoke",
                virtual_choice["evidence_status"],
            )
            self.assertIn("PublicEventTemplate.cs", virtual_choice["evidence_sources"])
            self.assertIn("PublicEventDataBackedMetadataTests.cs", virtual_choice["evidence_sources"])
            bomb = next(row for row in rows if row["evidence_kind"] == "public_event_objective_bomb_deployment")
            self.assertEqual("blocked_public_event_bomb_deployment_objective_linked_missing_carrier_deploy_evidence", bomb["evidence_status"])
            gather = next(row for row in rows if row["evidence_kind"] == "public_event_objective_gather_resource")
            self.assertEqual("blocked_public_event_gather_resource_objective_linked_missing_resource_carry_deposit_evidence", gather["evidence_status"])
            state = next(row for row in rows if row["evidence_kind"] == "public_event_objective_state")
            self.assertEqual(2, state["option_count"])
            custom_stat = next(row for row in rows if row["evidence_kind"] == "public_event_custom_stat" and row["source_row_id"] == 4)
            self.assertEqual("current_public_event", custom_stat["public_event_link_status"])
            self.assertEqual(
                "runtime_public_event_custom_stat_event_linked_scoreboard_dependency_tested_pending_stat_producer_smoke",
                custom_stat["evidence_status"],
            )
            type_custom_stat = next(row for row in rows if row["evidence_kind"] == "public_event_custom_stat" and row["source_row_id"] == 14)
            self.assertEqual("type_or_object_scoped", type_custom_stat["public_event_link_status"])
            self.assertEqual(
                "runtime_public_event_custom_stat_type_scoped_scoreboard_dependency_tested_pending_stat_producer_smoke",
                type_custom_stat["evidence_status"],
            )
            stat_display = next(row for row in rows if row["evidence_kind"] == "public_event_stat_display")
            self.assertEqual("type_or_object_scoped", stat_display["public_event_link_status"])
            self.assertEqual(
                "runtime_public_event_stat_display_type_scoped_standard_combat_stats_tested_pending_event_mapping_flags_and_client_smoke",
                stat_display["evidence_status"],
            )
            self.assertIn("UnitEntity.cs", stat_display["evidence_sources"])
            self.assertIn("PublicEventScoreboardTests.cs", stat_display["evidence_sources"])
            referenced_vote = next(row for row in rows if row["evidence_kind"] == "public_event_vote" and row["source_row_id"] == 64)
            self.assertIn("ExampleEventScript.cs", referenced_vote["script_references"])
            self.assertEqual("runtime_public_event_vote_script_referenced_vote_flow_tested_pending_choice_semantics_and_client_smoke", referenced_vote["evidence_status"])
            self.assertIn("PublicEventVoteTests.cs", referenced_vote["evidence_sources"])
            self.assertIn("ClientPublicEventVoteHandlerTests.cs", referenced_vote["evidence_sources"])
            self.assertIn("ShadesEveEventScriptTests.cs", referenced_vote["evidence_sources"])
            unreferenced_vote = next(row for row in rows if row["evidence_kind"] == "public_event_vote" and row["source_row_id"] == 65)
            self.assertEqual("blocked_public_event_vote_row_unlinked_missing_script_mapping", unreferenced_vote["evidence_status"])
            self.assertTrue(all(row["completion_status"] == "not_retail_complete" for row in rows))
        finally:
            shutil.rmtree(csv_root, ignore_errors=True)
            shutil.rmtree(script_root, ignore_errors=True)

    def test_challenge_evidence_rows_cover_definition_creature_and_reward_maps(self) -> None:
        csv_root = Path(tempfile.mkdtemp(prefix="nf-content-audit-challenges-"))
        try:
            (csv_root / "challenge_map.csv").write_text(
                "\n".join(
                    [
                        "jabbithole_challenge_id,challenge_id,jabbithole_challenge_name,client_challenge_name,match_status,description,goals,is_timed,challenge_type,client_challenge_type,zone_id,client_world_zone_id,zone_restriction_subzone,start_location,challenge_reward_track_id,client_reward_track_id,client_target,client_completion_count,challenge_reward_items_count,challenge_creatures_count,is_dominion,is_exile,enabled,last_seen_in",
                        "1,10,Source Challenge,Client Challenge,matched,Do challenge,1/2/3,false,0,0,20,30,,,40,50,60,3,1,2,true,true,true,7",
                    ]
                )
                + "\n",
                encoding="utf-8",
            )
            (csv_root / "challenge_creature_map.csv").write_text(
                "\n".join(
                    [
                        "source_relation_id,jabbithole_challenge_id,challenge_id,challenge_name,client_challenge_name,challenge_type,goals,zone_id,jabbithole_creature_id,creature2_id,source_name,creature2_name,match_status,last_seen_in",
                        "2,1,10,Source Challenge,Client Challenge,0,1/2/3,20,100,200,Source Creature,Client Creature,reviewed,7",
                        "3,1,10,Source Challenge,Client Challenge,0,1/2/3,20,101,,Missing Creature,,unmatched,7",
                    ]
                )
                + "\n",
                encoding="utf-8",
            )
            (csv_root / "challenge_reward_item_map.csv").write_text(
                "\n".join(
                    [
                        "relation_source,source_relation_id,jabbithole_challenge_id,challenge_id,challenge_name,client_challenge_name,challenge_reward_track_id,jabbithole_challenge_reward_id,challenge_reward_id,reward_index,reward_type,cost,tier,amount,jabbithole_item_id,item2_id,item_name,last_seen_in",
                        "direct_challenge_reward_items,4,1,10,Source Challenge,Client Challenge,40,,300,1,1,,2,5,400,500,Reward Item,7",
                        "reward_track_challenge_item,5,1,10,Source Challenge,Client Challenge,40,,301,0,0,,0,1,401,501,Track Reward Item,7",
                    ]
                )
                + "\n",
                encoding="utf-8",
            )

            rows = audit.build_challenge_evidence_rows(csv_root)

            self.assertEqual(5, len(rows))
            definition = next(row for row in rows if row["evidence_kind"] == "challenge")
            self.assertEqual("runtime_challenge_lifecycle_progress_tier_reward_track_tested_pending_content_smoke", definition["evidence_status"])
            reviewed = next(row for row in rows if row["related_jabbithole_id"] == 100)
            self.assertEqual("runtime_challenge_creature_target_group_hook_tested_pending_spawn_smoke", reviewed["evidence_status"])
            unmatched = next(row for row in rows if row["related_jabbithole_id"] == 101)
            self.assertEqual("datamapping_challenge_creature_unmatched_blocked", unmatched["evidence_status"])
            direct_reward = next(row for row in rows if row["evidence_kind"] == "challenge_reward_item" and row["relation_source"] == "direct_challenge_reward_items")
            self.assertEqual("datamapping_challenge_direct_reward_item_pending_reward_track_bridge", direct_reward["evidence_status"])
            track_reward = next(row for row in rows if row["evidence_kind"] == "challenge_reward_item" and row["relation_source"] == "reward_track_challenge_item")
            self.assertEqual("runtime_challenge_reward_track_item_grant_tested_pending_ui_selection_smoke", track_reward["evidence_status"])
            self.assertTrue(all(row["completion_status"] == "not_retail_complete" for row in rows))
        finally:
            shutil.rmtree(csv_root, ignore_errors=True)

    def test_path_mission_evidence_rows_cover_definition_episode_creature_reward_and_zone_maps(self) -> None:
        csv_root = Path(tempfile.mkdtemp(prefix="nf-content-audit-path-"))
        try:
            (csv_root / "path_mission_client_map.csv").write_text(
                "\n".join(
                    [
                        "path_mission_id,path_mission_name,path_type,mission_type,display_type,object_id,creature2_id_unlock,creature2_id_contact_override,world_location_0,world_location_1,world_location_2,world_location_3,faction_enum",
                        "33,Northern Wilds Soldier Mission,0,0,3,12,0,0,100,0,0,0,1",
                        "160,Northern Wilds Scientist Vitalium,2,2,3,130,0,0,100,0,0,0,1",
                        "10,Sample Mission,1,2,3,4,0,0,100,0,0,0,1",
                    ]
                )
                + "\n",
                encoding="utf-8",
            )
            (csv_root / "path_episode_map.csv").write_text(
                "\n".join(
                    [
                        "jabbithole_path_episode_id,path_episode_id,match_status,jabbithole_player_path_id,path_type,client_path_type,path_name,jabbithole_name,client_path_episode_name,jabbithole_summary,client_path_episode_summary,world_zone_text,datamined_zone_id,world_zone_id,zone_name,client_world_id,client_world_zone_id,client_world_zone_name,path_episode_rewards_count,path_episode_zones_count,path_episode_missions_count,is_dominion,is_exile,last_seen_in",
                        "1,8,matched,0,0,0,Soldier,Source Episode,Client Episode,Source Summary,Client Summary,Zone,1,1,Northern Wilds,426,35,Client Zone,1,1,1,false,true,7",
                        "2,21,unmatched,1,1,,Soldier,Old Episode,,Source Summary,,Zone,30,40,Zone,,,,0,0,0,true,,7",
                    ]
                )
                + "\n",
                encoding="utf-8",
            )
            (csv_root / "path_episode_mission_map.csv").write_text(
                "\n".join(
                    [
                        "source_relation_id,jabbithole_path_episode_id,path_episode_id,path_episode_name,client_path_episode_name,jabbithole_path_mission_id,path_mission_id,path_mission_name,client_path_mission_name,player_path_id,mission_type,mission_subtype,needed,last_seen_in",
                        "3,1,8,Source Episode,Client Episode,100,33,Northern Wilds Soldier Mission,Northern Wilds Soldier Mission,0,0,0,1,7",
                        "4,2,21,Old Episode,,101,10,Sample Mission,Sample Mission,1,2,0,1,7",
                    ]
                )
                + "\n",
                encoding="utf-8",
            )
            (csv_root / "path_episode_reward_map.csv").write_text(
                "\n".join(
                    [
                        "source_relation_id,jabbithole_path_episode_id,path_episode_id,path_episode_name,client_path_episode_name,item2_id,item_name,amount,last_seen_in",
                        "5,1,8,Source Episode,Client Episode,200,Reward Item,1,7",
                    ]
                )
                + "\n",
                encoding="utf-8",
            )
            (csv_root / "path_episode_zone_map.csv").write_text(
                "\n".join(
                    [
                        "source_relation_id,jabbithole_path_episode_id,path_episode_id,path_episode_name,client_path_episode_name,jabbithole_zone_id,world_zone_id,zone_name,client_zone_name,map_asset,continent_id,last_seen_in",
                        "6,1,8,Source Episode,Client Episode,1,1,Northern Wilds,Client Zone,mapasset,1,7",
                    ]
                )
                + "\n",
                encoding="utf-8",
            )
            (csv_root / "creature_path_mission_map.csv").write_text(
                "\n".join(
                    [
                        "source_relation_id,jabbithole_creature_id,creature2_id,source_name,creature2_name,match_status,jabbithole_path_mission_id,path_mission_id,path_mission_name,player_path_id,mission_type,mission_subtype,needed,real_zone_id,datamined_zone_id,last_seen_in",
                        "7,300,11139,Source Creature,Client Creature,unique_name,100,33,Northern Wilds Soldier Mission,0,0,0,1,1,1,7",
                        "9,302,15888,Vitalium Crystal,Vitalium Crystal,ambiguous_name,657,160,Northern Wilds Scientist Vitalium,4,2,3,100,1,1,7",
                        "8,301,,Missing Creature,,unmatched,100,10,Sample Mission,1,2,0,1,40,40,7",
                    ]
                )
                + "\n",
                encoding="utf-8",
            )

            rows = audit.build_path_mission_evidence_rows(csv_root)

            self.assertEqual(12, len(rows))
            implemented_definition = next(row for row in rows if row["evidence_kind"] == "path_mission" and row["path_mission_id"] == 33)
            self.assertEqual("implemented_northern_wilds_path_mission_runtime_verified", implemented_definition["evidence_status"])
            mapped_definition = next(row for row in rows if row["evidence_kind"] == "path_mission" and row["path_mission_id"] == 10)
            self.assertEqual("mapped_only_path_mission_blocked_runtime_owner_and_path_smoke", mapped_definition["evidence_status"])
            matched_episode = next(row for row in rows if row["evidence_kind"] == "path_episode" and row["path_episode_id"] == 8)
            self.assertEqual("implemented_northern_wilds_path_episode_activation_verified", matched_episode["evidence_status"])
            unmatched_episode_mission = next(row for row in rows if row["evidence_kind"] == "path_episode_mission" and row["path_episode_id"] == 21)
            self.assertEqual("blocked_path_episode_mission_missing_episode_bridge", unmatched_episode_mission["evidence_status"])
            reward = next(row for row in rows if row["evidence_kind"] == "path_episode_reward")
            self.assertEqual(
                "runtime_northern_wilds_path_episode_reward_type2_grant_tested_pending_presentation_smoke",
                reward["evidence_status"],
            )
            self.assertIn("PathManagerTests.cs", reward["evidence_sources"])
            self.assertIn("completed-episode reward delivery", reward["blocker_summary"])
            zone = next(row for row in rows if row["evidence_kind"] == "path_episode_zone")
            self.assertEqual("implemented_northern_wilds_runtime_episode_zone_bridge_verified", zone["evidence_status"])
            reviewed = next(row for row in rows if row["related_jabbithole_id"] == 300)
            self.assertEqual("implemented_northern_wilds_path_mission_creature_credit_verified", reviewed["evidence_status"])
            reviewed_ambiguous = next(row for row in rows if row["related_jabbithole_id"] == 302)
            self.assertEqual("implemented_northern_wilds_path_mission_creature_credit_verified", reviewed_ambiguous["evidence_status"])
            unmatched = next(row for row in rows if row["related_jabbithole_id"] == 301)
            self.assertEqual("blocked_path_mission_creature_unmatched_bridge", unmatched["evidence_status"])
            self.assertTrue(all(row["completion_status"] == "not_retail_complete" for row in rows))
        finally:
            shutil.rmtree(csv_root, ignore_errors=True)

    def test_contract_evidence_rows_cover_contract_creature_and_reward_maps(self) -> None:
        csv_root = Path(tempfile.mkdtemp(prefix="nf-content-audit-contracts-"))
        try:
            (csv_root / "contract_map.csv").write_text(
                "\n".join(
                    [
                        "jabbithole_contract_id,contract_game_id,jabbithole_quest_id,quest2_id,jabbithole_quest_name,client_quest_name,match_status,quality,contract_type,reward_track_type,xp,is_exile,is_dominion,enabled,first_seen_in,last_seen_in",
                        "1,10,1000,10,Source Contract,Client Contract,matched,2,1,0,50,true,true,true,0,7",
                    ]
                )
                + "\n",
                encoding="utf-8",
            )
            (csv_root / "contract_creature_map.csv").write_text(
                "\n".join(
                    [
                        "source_relation_id,jabbithole_quest_id,quest2_id,jabbithole_quest_name,client_quest_name,jabbithole_creature_id,creature2_id,source_name,creature2_name,match_status,relation_match_status,enabled,last_seen_in",
                        "2,1000,10,Source Contract,Client Contract,200,300,Source Creature,Client Creature,reviewed,matched,true,7",
                        "3,1000,10,Source Contract,Client Contract,201,,Missing Creature,,unmatched,unmatched_creature,true,7",
                    ]
                )
                + "\n",
                encoding="utf-8",
            )
            (csv_root / "contract_reward_map.csv").write_text(
                "\n".join(
                    [
                        "source_reward_id,contract_reward_game_id,item2_id,item_name,match_status,tier,raw_item_id,asset,contract_type,is_casque,cost,amount,choice_idx,trid,is_exile,is_dominion,enabled,last_seen_in",
                        "4,500,600,Reward Item,matched,1,700,,0,false,1000,1,2,4,true,true,true,7",
                    ]
                )
                + "\n",
                encoding="utf-8",
            )
            strings = {100: "Client Contract"}
            quests = [{"ID": "10", "localizedTextIdTitle": "100"}]
            quest_output = [{"quest_id": 10, "bucket": "generic_table_driven", "completion_status": "not_retail_complete"}]

            rows = audit.build_contract_evidence_rows(strings, quests, quest_output, csv_root)

            self.assertEqual(4, len(rows))
            contract = next(row for row in rows if row["evidence_kind"] == "contract")
            self.assertEqual("implemented_contract_quest_manager_backed_pending_rotation_client_smoke", contract["evidence_status"])
            reviewed = next(row for row in rows if row["related_jabbithole_id"] == 200)
            self.assertEqual("implemented_contract_creature_reviewed_bridge_objective_credit_pending_spawn_client_smoke", reviewed["evidence_status"])
            unmatched = next(row for row in rows if row["related_jabbithole_id"] == 201)
            self.assertEqual("datamapping_contract_creature_unmatched_blocked", unmatched["evidence_status"])
            reward = next(row for row in rows if row["evidence_kind"] == "contract_reward_item")
            self.assertEqual("not_quest_scoped", reward["quest_link_status"])
            self.assertEqual("mapped_only_contract_reward_track_item_pending_choice_presentation_client_smoke", reward["evidence_status"])
            self.assertTrue(all(row["completion_status"] == "not_retail_complete" for row in rows))
        finally:
            shutil.rmtree(csv_root, ignore_errors=True)

    def test_instance_portal_evidence_rows_cover_client_links_and_datamapping_bridge(self) -> None:
        csv_root = Path(tempfile.mkdtemp(prefix="nf-content-audit-instance-portals-"))
        try:
            (csv_root / "creature_map.csv").write_text(
                "\n".join(
                    [
                        "jabbithole_creature_id,creature2_id,source_name,client_name,match_status,entity_type,creature_type,zone_id,worldid,worldzoneid,review_decision",
                        "1,100,Source Portal,Reviewed Portal,reviewed,14,InstancePortal,2,51,1000,approved",
                        "2,101,Source Portal,Candidate Portal,unique_name,14,InstancePortal,3,52,1001,",
                    ]
                )
                + "\n",
                encoding="utf-8",
            )
            strings = {
                1000: "Portal A",
                1001: "Portal B",
                1002: "Portal C",
                2000: "Reviewed Portal",
                2001: "Candidate Portal",
                2002: "Client Only Portal",
                2003: "Missing Definition Portal",
            }
            instance_portals = [
                {"ID": "10", "localizedTextIdName": "1000", "minLevel": "10", "maxLevel": "50", "expectedCompletionTime": "30", "instancePortalTypeEnum": "1"},
                {"ID": "11", "localizedTextIdName": "1001", "minLevel": "1", "maxLevel": "1", "expectedCompletionTime": "0", "instancePortalTypeEnum": "2"},
                {"ID": "12", "localizedTextIdName": "1002", "minLevel": "0", "maxLevel": "0", "expectedCompletionTime": "0", "instancePortalTypeEnum": "3"},
            ]
            creatures = [
                {"ID": "100", "localizedTextIdName": "2000", "instancePortalId": "10", "minLevel": "10", "maxLevel": "10"},
                {"ID": "101", "localizedTextIdName": "2001", "instancePortalId": "10", "minLevel": "11", "maxLevel": "11"},
                {"ID": "102", "localizedTextIdName": "2002", "instancePortalId": "11", "minLevel": "1", "maxLevel": "1"},
                {"ID": "103", "localizedTextIdName": "2003", "instancePortalId": "99", "minLevel": "1", "maxLevel": "1"},
            ]

            rows = audit.build_instance_portal_evidence_rows(strings, instance_portals, creatures, csv_root)

            self.assertEqual(5, len(rows))
            reviewed = next(row for row in rows if row["creature2_id"] == 100)
            self.assertEqual("reviewed_datamapping_creature_bridge", reviewed["datamapping_bridge_status"])
            self.assertEqual("reviewed_instance_portal_creature_pending_entry_smoke", reviewed["evidence_status"])
            candidate = next(row for row in rows if row["creature2_id"] == 101)
            self.assertEqual("candidate_datamapping_creature_bridge", candidate["datamapping_bridge_status"])
            self.assertEqual("datamapping_instance_portal_creature_pending_review_and_entry_smoke", candidate["evidence_status"])
            client_only = next(row for row in rows if row["creature2_id"] == 102)
            self.assertEqual("missing_datamapping_creature_bridge", client_only["datamapping_bridge_status"])
            self.assertEqual("client_instance_portal_creature_link_pending_world_placement_review", client_only["evidence_status"])
            missing_definition = next(row for row in rows if row["creature2_id"] == 103)
            self.assertEqual("creature_references_missing_portal_definition", missing_definition["portal_link_status"])
            unlinked_definition = next(row for row in rows if row["evidence_kind"] == "instance_portal_definition")
            self.assertEqual(12, unlinked_definition["instance_portal_id"])
            self.assertEqual("client_instance_portal_definition_without_creature_link_blocked", unlinked_definition["evidence_status"])
            self.assertTrue(all(row["completion_status"] == "not_retail_complete" for row in rows))
        finally:
            shutil.rmtree(csv_root, ignore_errors=True)

    def test_script_presentation_evidence_rows_cover_communicators_cinematics_and_finish_hooks(self) -> None:
        temp_root = Path(tempfile.mkdtemp(prefix="nf-content-audit-presentation-"))
        try:
            csv_root = temp_root / "csv"
            script_root = temp_root / "scripts"
            cinematic_root = temp_root / "cinematics"
            (script_root / "Example" / "Script").mkdir(parents=True)
            cinematic_root.mkdir(parents=True)
            csv_root.mkdir()

            (csv_root / "client_source_communicatormessages_map.csv").write_text(
                "\n".join(
                    [
                        "ID,localizedTextIdMessage_text,worldId,worldZoneId,creatureId",
                        "10,Test callout,1,2,3",
                    ]
                )
                + "\n",
                encoding="utf-8",
            )
            (csv_root / "client_source_cinematic_map.csv").write_text("ID\n35\n", encoding="utf-8")
            (script_root / "Example" / "CommunicatorMessage.cs").write_text(
                """
namespace NexusForever.Script.Instance.Example
{
    public enum CommunicatorMessage
    {
        Callout = 10
    }
}
""".lstrip(),
                encoding="utf-8",
            )
            (script_root / "Example" / "Script" / "ExampleScript.cs").write_text(
                """
namespace NexusForever.Script.Instance.Example.Script
{
    [ScriptFilterOwnerId(42)]
    public class ExampleScript
    {
        public void Run()
        {
            Send(CommunicatorMessage.Callout);
            QueueWipGuessedCinematic<IExampleCinematic>();
        }

        public void OnCinematicFinish(IPlayer player, uint cinematicId)
        {
        }
    }
}
""".lstrip(),
                encoding="utf-8",
            )
            (cinematic_root / "ExampleCinematic.cs").write_text(
                """
public class ExampleCinematic : CinematicBase, IExampleCinematic
{
    protected override void Setup()
    {
        Duration = 0;
        CinematicId = 0;
    }
}
""".lstrip(),
                encoding="utf-8",
            )

            rows = audit.build_script_presentation_evidence_rows(csv_root, [script_root], cinematic_root)

            self.assertEqual(
                {"script_communicator_message", "script_cinematic_reference", "script_cinematic_finish_hook"},
                {row["evidence_kind"] for row in rows},
            )
            communicator = next(row for row in rows if row["evidence_kind"] == "script_communicator_message")
            self.assertEqual(10, communicator["reference_id"])
            self.assertEqual("client_row_matched", communicator["client_link_status"])
            self.assertEqual("Test callout", communicator["client_text"])
            self.assertEqual("42", communicator["script_owner_ids"])

            cinematic = next(row for row in rows if row["evidence_kind"] == "script_cinematic_reference")
            self.assertEqual("IExampleCinematic", cinematic["cinematic_interface"])
            self.assertEqual("zero_runtime_cinematic_id", cinematic["client_link_status"])
            self.assertEqual("runtime_placeholder_or_wip_payload", cinematic["runtime_payload_status"])
            self.assertEqual("script_cinematic_placeholder_pending_payload_mapping", cinematic["evidence_status"])

            finish_hook = next(row for row in rows if row["evidence_kind"] == "script_cinematic_finish_hook")
            self.assertEqual("runtime_callback_without_exact_client_id_gate", finish_hook["client_link_status"])
            self.assertTrue(all(row["completion_status"] == "not_retail_complete" for row in rows))
        finally:
            shutil.rmtree(temp_root, ignore_errors=True)

    def test_script_event_flow_evidence_rows_link_objectives_and_script_phases(self) -> None:
        script_root = Path(tempfile.mkdtemp(prefix="nf-content-audit-event-flow-"))
        try:
            (script_root / "Example" / "Script").mkdir(parents=True)
            (script_root / "Example" / "PublicEventObjective.cs").write_text(
                """
namespace NexusForever.Script.Instance.Example
{
    public enum PublicEventObjective
    {
        First = 100
    }
}
""".lstrip(),
                encoding="utf-8",
            )
            (script_root / "Example" / "PublicEventPhase.cs").write_text(
                """
namespace NexusForever.Script.Instance.Example
{
    public enum PublicEventPhase
    {
        Start,
        Finish
    }
}
""".lstrip(),
                encoding="utf-8",
            )
            (script_root / "Example" / "Script" / "ExampleEventScript.cs").write_text(
                """
namespace NexusForever.Script.Instance.Example.Script
{
    [ScriptFilterOwnerId(50)]
    public class ExampleEventScript
    {
        public void Run()
        {
            publicEvent.SetPhase(PublicEventPhase.Start);
            publicEvent.ActivateObjective(PublicEventObjective.First);
        }

        public void OnPublicEventPhase(uint phase)
        {
            switch ((PublicEventPhase)phase)
            {
                case PublicEventPhase.Start:
                    break;
            }
        }

        public void OnPublicEventObjectiveStatus(IPublicEventObjective objective)
        {
            switch ((PublicEventObjective)objective.Entry.Id)
            {
                case PublicEventObjective.First:
                    break;
            }
        }
    }
}
""".lstrip(),
                encoding="utf-8",
            )
            strings = {10: "Example Event", 20: "Example Objective", 21: "Short"}
            public_events = [
                {
                    "ID": "50",
                    "worldId": "1",
                    "worldZoneId": "2",
                    "localizedTextIdName": "10",
                    "publicEventTypeEnum": "3",
                    "publicEventIdParent": "0",
                }
            ]
            public_event_objectives = [
                {
                    "ID": "100",
                    "publicEventId": "50",
                    "publicEventObjectiveTypeEnum": "7",
                    "count": "2",
                    "objectId": "300",
                    "worldLocation2Id": "400",
                    "targetGroupIdRewardPane": "500",
                    "displayOrder": "1",
                    "localizedTextId": "20",
                    "localizedTextIdShort": "21",
                }
            ]

            rows = audit.build_script_event_flow_evidence_rows(strings, public_events, public_event_objectives, [script_root])

            self.assertEqual(4, len(rows))
            self.assertEqual(
                {"script_public_event_objective_reference", "script_public_event_phase_reference"},
                {row["evidence_kind"] for row in rows},
            )
            objective_rows = [row for row in rows if row["evidence_kind"] == "script_public_event_objective_reference"]
            self.assertEqual(2, len(objective_rows))
            self.assertTrue(all(row["reference_id"] == 100 for row in objective_rows))
            self.assertTrue(all(row["reference_link_status"] == "client_row_matched" for row in objective_rows))
            self.assertTrue(all(row["owner_link_status"] == "script_owner_matches_objective_public_event" for row in objective_rows))
            self.assertEqual("Example Objective", objective_rows[0]["objective_text"])
            self.assertEqual(300, objective_rows[0]["objective_object_id"])

            phase_rows = [row for row in rows if row["evidence_kind"] == "script_public_event_phase_reference"]
            self.assertEqual(2, len(phase_rows))
            self.assertTrue(all(row["reference_id"] == 0 for row in phase_rows))
            self.assertTrue(all(row["reference_link_status"] == "script_enum_value_resolved" for row in phase_rows))
            self.assertTrue(all(row["owner_link_status"] == "script_owner_matches_public_event" for row in phase_rows))
            self.assertTrue(all(row["completion_status"] == "not_retail_complete" for row in rows))
        finally:
            shutil.rmtree(script_root, ignore_errors=True)

    def test_script_event_flow_overrides_review_tested_downsizer_rows(self) -> None:
        strings = {
            642: "Ultimate Protogames Downsizer",
            3197: "Defeat the Downsizer",
            3266: "Voltaic Conversion",
            3267: "Overcharge",
            3268: "Electrostatic Dynamo",
            3269: "Electromagnetic Induction",
        }
        public_events = [
            {
                "ID": "642",
                "worldId": "3041",
                "worldZoneId": "0",
                "localizedTextIdName": "642",
                "publicEventTypeEnum": "0",
                "publicEventIdParent": "0",
            }
        ]
        public_event_objectives = [
            {
                "ID": str(objective_id),
                "publicEventId": "642",
                "publicEventObjectiveTypeEnum": "0",
                "count": "1",
                "objectId": "0",
                "worldLocation2Id": "0",
                "targetGroupIdRewardPane": "0",
                "displayOrder": str(index),
                "localizedTextId": str(objective_id),
                "localizedTextIdShort": "0",
            }
            for index, objective_id in enumerate((3197, 3266, 3267, 3268, 3269), start=1)
        ]

        rows = audit.build_script_event_flow_evidence_rows(
            strings,
            public_events,
            public_event_objectives,
            [audit.DEFAULT_SCRIPT_INSTANCE],
        )
        rows_by_key = {
            (
                row["source_path"],
                int(row["source_line"]),
                row["evidence_kind"],
                row["source_call"],
                row["source_symbol"],
            ): row
            for row in rows
        }

        for key, override in audit.SCRIPT_EVENT_FLOW_EVIDENCE_OVERRIDES.items():
            self.assertIn(key, rows_by_key)
            row = rows_by_key[key]
            self.assertEqual(override["evidence_status"], row["evidence_status"])
            self.assertNotRegex(row["evidence_status"], "pending|blocked|wip|mapped_only|reviewed")
            self.assertEqual("not_retail_complete", row["completion_status"])

    def test_script_objective_producer_overrides_track_downsizer_rows(self) -> None:
        strings = {
            642: "Ultimate Protogames Downsizer",
            3197: "Defeat the Downsizer",
            3266: "Voltaic Conversion",
            3267: "Overcharge",
            3268: "Electrostatic Dynamo",
            3269: "Electromagnetic Induction",
        }
        public_events = [
            {
                "ID": "642",
                "worldId": "3041",
                "worldZoneId": "0",
                "localizedTextIdName": "642",
                "publicEventTypeEnum": "0",
                "publicEventIdParent": "0",
            }
        ]
        public_event_objectives = [
            {
                "ID": str(objective_id),
                "publicEventId": "642",
                "publicEventObjectiveTypeEnum": "0",
                "count": "1",
                "objectId": "0",
                "worldLocation2Id": "0",
                "targetGroupIdRewardPane": "0",
                "displayOrder": str(index),
                "localizedTextId": str(objective_id),
                "localizedTextIdShort": "0",
            }
            for index, objective_id in enumerate((3197, 3266, 3267, 3268, 3269), start=1)
        ]

        rows = audit.build_script_objective_producer_evidence_rows(
            strings,
            public_events,
            public_event_objectives,
            [audit.DEFAULT_SCRIPT_INSTANCE / "Dungeon" / "UltimateProtogames" / "Downsizer"],
        )
        rows_by_key = {
            (
                row["source_path"],
                str(row["matched_public_event_objective_ids"]),
                str(row["object_id"]),
            ): row
            for row in rows
        }
        event_script = (
            r"Source\NexusForever.Script.Instance\Dungeon\UltimateProtogames\Downsizer\UltimateProtogamesEventScript.cs"
        )
        downsizer_script = (
            r"Source\NexusForever.Script.Instance\Dungeon\UltimateProtogames\Downsizer\Script\DownsizerEntityScript.cs"
        )

        cleared_statuses = {
            (event_script, "3197", ""): "runtime_up_downsizer_defeat_objective_activation_tested",
            (downsizer_script, "3197", ""): "runtime_up_downsizer_death_credit_tested",
        }
        for key, expected_status in cleared_statuses.items():
            with self.subTest(key=key):
                self.assertIn(key, rows_by_key)
                row = rows_by_key[key]
                self.assertEqual(expected_status, row["evidence_status"])
                self.assertFalse(audit.is_script_objective_producer_blocker(row))
                self.assertNotRegex(row["evidence_status"], "pending|blocked|wip|mapped_only|reviewed")

        blocked_statuses = {
            (event_script, "3266", ""): "mapped_up_downsizer_voltaic_conversion_activation_only_blocked_missing_ability_mechanics_client_smoke",
            (event_script, "3267", ""): "mapped_up_downsizer_overcharge_activation_only_blocked_missing_ability_mechanics_client_smoke",
            (event_script, "3268", ""): "mapped_up_downsizer_electrostatic_dynamo_activation_only_blocked_missing_ability_mechanics_client_smoke",
            (event_script, "3269", ""): "mapped_up_downsizer_electromagnetic_induction_activation_only_blocked_missing_ability_mechanics_client_smoke",
        }
        for key, expected_status in blocked_statuses.items():
            with self.subTest(key=key):
                self.assertIn(key, rows_by_key)
                row = rows_by_key[key]
                self.assertEqual(expected_status, row["evidence_status"])
                self.assertTrue(audit.is_script_objective_producer_blocker(row))

    def test_script_objective_producer_overrides_clear_evil_ether_medbay_generator_rows(self) -> None:
        strings = {
            781: "Evil from the Ether",
            4937: "Open Medbay",
            4947: "Download Crew Logs",
            4956: "Scavenge Spare Parts",
            4957: "Repair Door",
            4938: "Activate Medbay Generator",
            4941: "Restart Main Generators",
            4961: "Restore Generator Alpha Power",
            4962: "Restore Generator Beta Power",
        }
        public_events = [
            {
                "ID": "781",
                "worldId": "3404",
                "worldZoneId": "0",
                "localizedTextIdName": "781",
                "publicEventTypeEnum": "0",
                "publicEventIdParent": "0",
            }
        ]

        def objective(
            objective_id: int,
            objective_type: int,
            count: int,
            object_id: int,
            display_order: int,
        ) -> dict[str, str]:
            return {
                "ID": str(objective_id),
                "publicEventId": "781",
                "publicEventObjectiveTypeEnum": str(objective_type),
                "count": str(count),
                "objectId": str(object_id),
                "localizedTextId": str(objective_id),
                "localizedTextIdShort": "0",
                "worldLocation2Id": "0",
                "targetGroupIdRewardPane": "0",
                "displayOrder": str(display_order),
            }

        public_event_objectives = [
            objective(4937, 3, 1, 14064, 1),
            objective(4947, 3, 7, 14057, 2),
            objective(4956, 4, 3, 14066, 3),
            objective(4957, 3, 1, 14069, 4),
            objective(4938, 3, 1, 14076, 5),
            objective(4941, 3, 2, 14077, 6),
            objective(4961, 3, 1, 14083, 7),
            objective(4962, 3, 1, 14084, 8),
        ]

        rows = audit.build_script_objective_producer_evidence_rows(
            strings,
            public_events,
            public_event_objectives,
            [audit.DEFAULT_SCRIPT_INSTANCE / "Expedition" / "EvilFromTheEther"],
        )
        rows_by_key = {
            (
                row["source_path"],
                str(row["matched_public_event_objective_ids"]),
                str(row["object_id"]),
            ): row
            for row in rows
        }
        event_script = r"Source\NexusForever.Script.Instance\Expedition\EvilFromTheEther\EvilFromTheEtherEventScript.cs"
        medbay_scripts = (
            r"Source\NexusForever.Script.Instance\Expedition\EvilFromTheEther\Script\MedbayObjectiveEntityScripts.cs"
        )
        crew_log_script = r"Source\NexusForever.Script.Instance\Expedition\EvilFromTheEther\Script\CrewLogEntityScript.cs"
        expected_statuses = {
            (event_script, "4937", "14064"): "runtime_evil_ether_open_medbay_activation_tested",
            (event_script, "4947", "14057"): "runtime_evil_ether_crew_logs_objective_activation_tested",
            (event_script, "4956", "14066"): "runtime_evil_ether_spare_parts_checklist_activation_tested",
            (event_script, "4957", "14069"): "runtime_evil_ether_repair_door_activation_tested",
            (event_script, "4938", "14076"): "runtime_evil_ether_medbay_generator_activation_tested",
            (event_script, "4941", "14077"): "runtime_evil_ether_restart_generators_activation_tested",
            (event_script, "4961", "14083"): "runtime_evil_ether_generator_alpha_activation_tested",
            (event_script, "4962", "14084"): "runtime_evil_ether_generator_beta_activation_tested",
            (medbay_scripts, "4937", "14064"): "runtime_evil_ether_open_medbay_door_control_credit_tested",
            (medbay_scripts, "4956", "14066"): "runtime_evil_ether_spare_parts_checklist_credit_tested",
            (medbay_scripts, "4957", "14069"): "runtime_evil_ether_repaired_medbay_door_control_credit_tested",
            (medbay_scripts, "4938", "14076"): "runtime_evil_ether_medbay_generator_controls_credit_tested",
            (crew_log_script, "4947", "14057"): "runtime_evil_ether_crew_log_credit_and_communicator_tested",
        }
        for key, expected_status in expected_statuses.items():
            with self.subTest(key=key):
                self.assertIn(key, rows_by_key)
                row = rows_by_key[key]
                self.assertEqual(expected_status, row["evidence_status"])
                self.assertFalse(audit.is_script_objective_producer_blocker(row))
                self.assertNotRegex(row["evidence_status"], "pending|blocked|wip|mapped_only|reviewed")

        dependency_statuses = {
            4938: "runtime_medbay_generator_objective_producer_tested",
            4956: "runtime_spare_parts_objective_producer_tested",
            4957: "runtime_repaired_medbay_door_objective_producer_tested",
        }
        for objective_id, expected_status in dependency_statuses.items():
            with self.subTest(dependency=objective_id):
                dependency = audit.INSTANCE_OBJECTIVE_DEPENDENCY_OVERRIDES[(3404, objective_id)]
                self.assertEqual(expected_status, dependency["evidence_status"])
                self.assertEqual("automated_focus_pass", dependency["manual_validation"])
                self.assertNotRegex(dependency["evidence_status"], "pending|blocked|wip|mapped_only|reviewed")

    def test_script_objective_producer_rows_link_direct_typed_and_credit_base_updates(self) -> None:
        script_root = Path(tempfile.mkdtemp(prefix="nf-content-audit-objective-producer-"))
        try:
            (script_root / "Example" / "Script").mkdir(parents=True)
            (script_root / "Example" / "PublicEventObjective.cs").write_text(
                """
namespace NexusForever.Script.Instance.Example
{
    public enum PublicEventObjective
    {
        Direct = 100,
        Scripted = 101,
        Boss = 102,
        Activated = 103,
        Optional = 104,
        Seeded = 105,
        Muburu = 106,
        Timed = 107,
        BaseTyped = 108,
        BossTimed = 109,
        BossBonus = 110
    }
}
""".lstrip(),
                encoding="utf-8",
            )
            (script_root / "Example" / "Script" / "ExampleBaseTypedEntityScript.cs").write_text(
                """
namespace NexusForever.Script.Instance.Example.Script
{
    public class ExampleBaseTypedEntityScript
    {
        public ExampleBaseTypedEntityScript()
            : base(PublicEventObjectiveType.ActivateTargetGroupChecklist, 888u, true)
        {
        }
    }
}
""".lstrip(),
                encoding="utf-8",
            )
            (script_root / "Example" / "Script" / "ExampleProducerScript.cs").write_text(
                """
namespace NexusForever.Script.Instance.Example.Script
{
    [ScriptFilterOwnerId(50)]
    public class ExampleProducerScript
    {
        public void Run()
        {
            publicEvent.UpdateObjective(PublicEventObjective.Direct, 1);
            publicEvent.UpdateObjective(PublicEventObjectiveType.Script, 777u, 1);
            publicEvent.UpdateObjective(Objective, 1);
            publicEvent.UpdateObjective(PublicEventObjective.Timed, 1);
            publicEvent.ActivateObjective(PublicEventObjective.Activated, 2);
            publicEvent.ActivateObjective(PublicEventObjective.Muburu);
            ActivateWipOptionalObjective(PublicEventObjective.Optional);
            SeedParticipantsInRangeObjective(PublicEventObjective.Seeded, 1u, 2u);
        }

        private void ActivateWipOptionalObjective(PublicEventObjective objective)
        {
            publicEvent.ActivateObjective(objective);
        }

        private void SeedParticipantsInRangeObjective(PublicEventObjective objectiveId, params uint[] entityGuids)
        {
            publicEvent.UpdateObjective(objectiveId, entityGuids.Length);
        }
    }
}
""".lstrip(),
                encoding="utf-8",
            )
            (script_root / "Example" / "Script" / "ExampleBossEntityScript.cs").write_text(
                """
namespace NexusForever.Script.Instance.Example.Script
{

    [ScriptFilterScriptName("ExampleBossEntityScript")]
    public class ExampleBossEntityScript : PublicEventObjectiveCreditEntityScript
    {
        public ExampleBossEntityScript(IFactory<ISpellParameters> spellParametersFactory, IGameTableManager gameTableManager)
            : base(
                spellParametersFactory,
                gameTableManager,
                (uint)PublicEventObjective.Boss,
                (uint)PublicEventObjective.BossTimed,
                (uint)PublicEventObjective.BossBonus)
        {
        }
    }
}
""".lstrip(),
                encoding="utf-8",
            )
            strings = {
                10: "Example Event",
                20: "Direct Objective",
                21: "Script Objective",
                22: "Boss Objective",
                23: "Activated Objective",
                24: "Optional Objective",
                25: "Seeded Objective",
                26: "Muburu Objective",
                27: "Timed Objective",
                28: "Base Typed Objective",
                29: "Boss Timed Objective",
                30: "Boss Bonus Objective",
            }
            public_events = [
                {
                    "ID": "50",
                    "worldId": "1",
                    "worldZoneId": "2",
                    "localizedTextIdName": "10",
                    "publicEventTypeEnum": "3",
                    "publicEventIdParent": "0",
                }
            ]
            public_event_objectives = [
                {
                    "ID": "100",
                    "publicEventId": "50",
                    "publicEventObjectiveTypeEnum": "0",
                    "count": "1",
                    "objectId": "0",
                    "localizedTextId": "20",
                    "localizedTextIdShort": "0",
                    "worldLocation2Id": "0",
                    "targetGroupIdRewardPane": "0",
                    "displayOrder": "1",
                },
                {
                    "ID": "101",
                    "publicEventId": "50",
                    "publicEventObjectiveTypeEnum": "5",
                    "count": "1",
                    "objectId": "777",
                    "localizedTextId": "21",
                    "localizedTextIdShort": "0",
                    "worldLocation2Id": "0",
                    "targetGroupIdRewardPane": "0",
                    "displayOrder": "2",
                },
                {
                    "ID": "102",
                    "publicEventId": "50",
                    "publicEventObjectiveTypeEnum": "9",
                    "count": "1",
                    "objectId": "0",
                    "localizedTextId": "22",
                    "localizedTextIdShort": "0",
                    "worldLocation2Id": "0",
                    "targetGroupIdRewardPane": "0",
                    "displayOrder": "3",
                },
                {
                    "ID": "103",
                    "publicEventId": "50",
                    "publicEventObjectiveTypeEnum": "25",
                    "count": "1",
                    "objectId": "0",
                    "localizedTextId": "23",
                    "localizedTextIdShort": "0",
                    "worldLocation2Id": "0",
                    "targetGroupIdRewardPane": "0",
                    "displayOrder": "4",
                },
                {
                    "ID": "104",
                    "publicEventId": "50",
                    "publicEventObjectiveTypeEnum": "8",
                    "count": "1",
                    "objectId": "0",
                    "localizedTextId": "24",
                    "localizedTextIdShort": "0",
                    "worldLocation2Id": "0",
                    "targetGroupIdRewardPane": "0",
                    "displayOrder": "5",
                },
                {
                    "ID": "105",
                    "publicEventId": "50",
                    "publicEventObjectiveTypeEnum": "8",
                    "count": "1",
                    "objectId": "0",
                    "localizedTextId": "25",
                    "localizedTextIdShort": "0",
                    "worldLocation2Id": "0",
                    "targetGroupIdRewardPane": "0",
                    "displayOrder": "6",
                },
                {
                    "ID": "106",
                    "publicEventId": "50",
                    "publicEventObjectiveTypeEnum": "8",
                    "count": "1",
                    "objectId": "0",
                    "localizedTextId": "26",
                    "localizedTextIdShort": "0",
                    "worldLocation2Id": "0",
                    "targetGroupIdRewardPane": "0",
                    "displayOrder": "7",
                },
                {
                    "ID": "107",
                    "publicEventId": "50",
                    "publicEventObjectiveTypeEnum": "25",
                    "count": "0",
                    "objectId": "0",
                    "localizedTextId": "27",
                    "localizedTextIdShort": "0",
                    "worldLocation2Id": "0",
                    "targetGroupIdRewardPane": "0",
                    "displayOrder": "8",
                },
                {
                    "ID": "108",
                    "publicEventId": "50",
                    "publicEventObjectiveTypeEnum": "4",
                    "count": "1",
                    "objectId": "888",
                    "localizedTextId": "28",
                    "localizedTextIdShort": "0",
                    "worldLocation2Id": "0",
                    "targetGroupIdRewardPane": "0",
                    "displayOrder": "9",
                },
                {
                    "ID": "109",
                    "publicEventId": "50",
                    "publicEventObjectiveTypeEnum": "5",
                    "count": "1",
                    "objectId": "0",
                    "localizedTextId": "29",
                    "localizedTextIdShort": "0",
                    "worldLocation2Id": "0",
                    "targetGroupIdRewardPane": "0",
                    "displayOrder": "10",
                },
                {
                    "ID": "110",
                    "publicEventId": "50",
                    "publicEventObjectiveTypeEnum": "25",
                    "count": "0",
                    "objectId": "0",
                    "localizedTextId": "30",
                    "localizedTextIdShort": "0",
                    "worldLocation2Id": "0",
                    "targetGroupIdRewardPane": "0",
                    "displayOrder": "11",
                },
            ]

            rows = audit.build_script_objective_producer_evidence_rows(strings, public_events, public_event_objectives, [script_root])

            self.assertEqual(11, len(rows))
            direct = next(row for row in rows if row["source_symbol"] == "PublicEventObjective.Direct")
            self.assertEqual("direct_objective", direct["producer_targeting"])
            self.assertEqual(0, direct["objective_type_id"])
            self.assertEqual("KillTargetGroup", direct["objective_type_name"])
            self.assertEqual("direct_objective_row_matched", direct["reference_link_status"])
            self.assertEqual("script_owner_matches_public_event", direct["owner_link_status"])
            self.assertEqual("direct_objective_id_matches_client_kill_type_pending_timing_and_script_type_review", direct["evidence_status"])
            self.assertIn("do not treat this as generic kill-style routing", direct["blocker_summary"])

            timed = next(row for row in rows if row["source_symbol"] == "PublicEventObjective.Timed")
            self.assertEqual(25, timed["objective_type_id"])
            self.assertEqual("ScriptWithoutMax", timed["objective_type_name"])
            self.assertEqual("script_without_max_direct_objective_credit_matched_pending_timing_smoke", timed["evidence_status"])
            self.assertIn("script credit matches a client ScriptWithoutMax public-event objective row", timed["blocker_summary"])
            self.assertIn("separate from KillTargetGroup/KillEventUnit routing", timed["blocker_summary"])

            activated = next(row for row in rows if row["source_symbol"] == "PublicEventObjective.Activated")
            self.assertEqual("script_activate_objective_call", activated["evidence_kind"])
            self.assertEqual("ActivateObjective", activated["source_call"])
            self.assertEqual(103, activated["objective_id"])
            self.assertEqual("ScriptWithoutMax", activated["objective_type_name"])
            self.assertEqual("script_without_max_direct_objective_activation_matched_pending_timing_smoke", activated["evidence_status"])
            self.assertIn("script activation matches a client ScriptWithoutMax public-event objective row", activated["blocker_summary"])
            self.assertEqual("2", activated["update_count"])
            self.assertEqual("script_owner_matches_public_event", activated["owner_link_status"])

            muburu = next(row for row in rows if row["source_symbol"] == "PublicEventObjective.Muburu")
            self.assertEqual(106, muburu["objective_id"])
            self.assertEqual("script_activate_objective_call", muburu["evidence_kind"])

            optional = next(row for row in rows if row["source_symbol"] == "PublicEventObjective.Optional")
            self.assertEqual("script_activate_objective_helper_call", optional["evidence_kind"])
            self.assertEqual("wip_optional_roll", optional["update_count"])

            seeded = next(row for row in rows if row["source_symbol"] == "PublicEventObjective.Seeded")
            self.assertEqual("script_update_objective_helper_call", seeded["evidence_kind"])
            self.assertEqual("range_participant_count", seeded["update_count"])

            typed = next(row for row in rows if row["source_call"] == "UpdateObjective" and row["producer_targeting"] == "typed_objective")
            self.assertEqual(101, typed["objective_id"])
            self.assertEqual(5, typed["objective_type_id"])
            self.assertEqual(777, typed["object_id"])
            self.assertEqual("101", typed["matched_public_event_objective_ids"])
            self.assertEqual("typed_objective_row_matched", typed["reference_link_status"])

            typed_base = next(row for row in rows if row["evidence_kind"] == "script_typed_objective_credit_base")
            self.assertEqual("typed_objective_base", typed_base["source_call"])
            self.assertEqual("PublicEventObjectiveType.ActivateTargetGroupChecklist", typed_base["source_symbol"])
            self.assertEqual(108, typed_base["objective_id"])
            self.assertEqual(4, typed_base["objective_type_id"])
            self.assertEqual(888, typed_base["object_id"])
            self.assertEqual("108", typed_base["matched_public_event_objective_ids"])
            self.assertEqual("typed_objective_row_matched", typed_base["reference_link_status"])

            self.assertFalse(any(row["source_symbol"] == "Objective" for row in rows))

            credit_rows = [row for row in rows if row["evidence_kind"] == "script_objective_credit_base"]
            self.assertEqual(
                ["PublicEventObjective.Boss", "PublicEventObjective.BossBonus", "PublicEventObjective.BossTimed"],
                sorted(row["source_symbol"] for row in credit_rows),
            )

            credit = next(row for row in credit_rows if row["source_symbol"] == "PublicEventObjective.Boss")
            self.assertEqual("ExampleBossEntityScript", credit["script_filter_name"])
            self.assertEqual(102, credit["objective_id"])
            self.assertEqual(9, credit["objective_type_id"])
            self.assertEqual("ScriptWithoutCount", credit["objective_type_name"])
            self.assertEqual("script_without_count_direct_objective_credit_matched_pending_owner_flow_review", credit["evidence_status"])
            self.assertIn("script credit matches a client ScriptWithoutCount public-event objective row", credit["blocker_summary"])
            self.assertIn("separate from KillTargetGroup/KillEventUnit routing", credit["blocker_summary"])
            self.assertEqual("script_owner_unknown", credit["owner_link_status"])

            boss_timed = next(row for row in credit_rows if row["source_symbol"] == "PublicEventObjective.BossTimed")
            self.assertEqual(109, boss_timed["objective_id"])
            self.assertEqual(5, boss_timed["objective_type_id"])
            self.assertEqual("Script", boss_timed["objective_type_name"])

            boss_bonus = next(row for row in credit_rows if row["source_symbol"] == "PublicEventObjective.BossBonus")
            self.assertEqual(110, boss_bonus["objective_id"])
            self.assertEqual(25, boss_bonus["objective_type_id"])
            self.assertEqual("ScriptWithoutMax", boss_bonus["objective_type_name"])
            self.assertTrue(all(row["completion_status"] == "not_retail_complete" for row in rows))
        finally:
            shutil.rmtree(script_root, ignore_errors=True)

    def test_script_quest_progression_rows_link_script_main_progression_hooks(self) -> None:
        script_root = Path(tempfile.mkdtemp(prefix="nf-content-audit-quest-progression-"))
        try:
            (script_root / "Example").mkdir(parents=True)
            (script_root / "Example" / "ExampleQuestScript.cs").write_text(
                """
namespace NexusForever.Script.Main.Example
{
    [ScriptFilterOwnerId(100u)]
    public class ExampleQuestScript
    {
        private const uint DirectObjective = 200u;
        private const uint ZoneId = 900u;
        private const ushort FollowUpQuest = 101;
        private const ushort AchievementId = 300;

        public void Run()
        {
            owner.ObjectiveUpdate(DirectObjective, 1u);
            owner.Player.QuestManager.ObjectiveUpdate(QuestObjectiveType.EnterZone, ZoneId, 1u);
            owner.Player.QuestManager.QuestAchieve(100);
            owner.Player.QuestManager.QuestMention(FollowUpQuest);
            IQuestInfo info = globalQuestManager.GetQuestInfo(FollowUpQuest);
            owner.Player.QuestManager.QuestAdd(info);
            owner.Player.AchievementManager.GrantAchievement(AchievementId);
        }
    }

    [ScriptFilterCreatureId(555u)]
    public class ExampleCreatureScript
    {
        private const ushort CreatureQuest = 100;
        private const uint CreatureObject = 777u;

        public void Run(IQuestObjective objective)
        {
            if (player.QuestManager.GetQuestState(CreatureQuest) != QuestState.Accepted)
                return;

            player.QuestManager.ObjectiveUpdate(QuestObjectiveType.ActivateEntity, CreatureObject, 1u);
            player.QuestManager.ObjectiveUpdate(QuestObjectiveType.ActivateEntity, owner.CreatureId, 1u);
            player.QuestManager.ObjectiveUpdate(objective.ObjectiveInfo.Id, 1u);
        }
    }
}
""".lstrip(),
                encoding="utf-8",
            )
            enter_zone_type = audit.load_quest_objective_type_values()["EnterZone"]
            strings = {10: "Primary Quest", 11: "Follow-up Quest", 20: "Direct objective", 21: "Zone objective", 30: "Achievement"}
            quests = [
                {
                    "ID": "100",
                    "localizedTextIdTitle": "10",
                    "objective0": "200",
                    "objective01": "201",
                    "objective02": "0",
                    "objective03": "0",
                    "objective04": "0",
                    "objective05": "0",
                },
                {
                    "ID": "101",
                    "localizedTextIdTitle": "11",
                    "objective0": "0",
                    "objective01": "0",
                    "objective02": "0",
                    "objective03": "0",
                    "objective04": "0",
                    "objective05": "0",
                },
            ]
            quest_output = [
                {"quest_id": 100, "bucket": "generic_with_script", "completion_status": "not_retail_complete"},
                {"quest_id": 101, "bucket": "generic_table_driven", "completion_status": "not_retail_complete"},
            ]
            objectives = [
                {"ID": "200", "type": "5", "data": "777", "localizedTextIdFull": "20", "localizedTextIdShort": "0"},
                {"ID": "201", "type": str(enter_zone_type), "data": "900", "localizedTextIdFull": "21", "localizedTextIdShort": "0"},
            ]
            achievements = [
                {
                    "ID": "300",
                    "achievementTypeId": "3",
                    "objectId": "100",
                    "objectIdAlt": "0",
                    "localizedTextIdTitle": "30",
                }
            ]

            rows = audit.build_script_quest_progression_evidence_rows(strings, quests, quest_output, objectives, achievements, script_root)

            self.assertEqual(9, len(rows))
            kinds = {row["evidence_kind"] for row in rows}
            self.assertEqual(
                {
                    "script_achievement_grant",
                    "script_quest_achieve",
                    "script_quest_add",
                    "script_quest_mention",
                    "script_quest_objective_update",
                    "script_quest_typed_objective_update",
                },
                kinds,
            )

            direct = next(row for row in rows if row["source_symbol"] == "DirectObjective")
            self.assertEqual("direct_quest_objective", direct["progression_targeting"])
            self.assertEqual("direct_objective_row_matched", direct["reference_link_status"])
            self.assertEqual("script_owner_matches_quest", direct["owner_link_status"])
            self.assertEqual("100", direct["script_owner_ids"])
            self.assertEqual("", direct["script_creature_ids"])
            self.assertEqual(100, direct["quest_id"])
            self.assertEqual(200, direct["objective_id"])

            typed = next(row for row in rows if row["evidence_kind"] == "script_quest_typed_objective_update")
            self.assertEqual("QuestObjectiveType.EnterZone", typed["source_symbol"])
            self.assertEqual(enter_zone_type, typed["objective_type_id"])
            self.assertEqual(900, typed["object_id"])
            self.assertEqual("201", typed["matched_quest_objective_ids"])
            self.assertEqual("typed_objective_row_matched", typed["reference_link_status"])

            dynamic_typed = next(
                row
                for row in rows
                if row["source_symbol"] == "QuestObjectiveType.ActivateEntity"
                and row["reference_link_status"] == "typed_objective_dynamic_object"
            )
            self.assertEqual("", dynamic_typed["script_owner_ids"])
            self.assertEqual("555", dynamic_typed["script_creature_ids"])
            self.assertEqual(100, dynamic_typed["quest_id"])
            self.assertEqual("100", dynamic_typed["matched_quest_ids"])
            self.assertEqual("typed_objective_dynamic_object", dynamic_typed["reference_link_status"])
            self.assertIn("guard_quest_ids=100", dynamic_typed["source_trace_detail"])

            guarded_typed = next(
                row
                for row in rows
                if row["source_symbol"] == "QuestObjectiveType.ActivateEntity"
                and row["object_id"] == 777
            )
            self.assertEqual("QuestObjectiveType.ActivateEntity", guarded_typed["source_symbol"])
            self.assertEqual("quest_state_guard_matches_quest", guarded_typed["owner_link_status"])
            self.assertEqual("script_quest_typed_objective_update_matched_pending_trigger_smoke", guarded_typed["evidence_status"])
            self.assertIn("guard_quest_ids=100", guarded_typed["source_trace_detail"])

            quest_add = next(row for row in rows if row["evidence_kind"] == "script_quest_add")
            self.assertEqual("info<-FollowUpQuest", quest_add["source_symbol"])
            self.assertEqual(101, quest_add["quest_id"])
            self.assertEqual("quest_row_matched", quest_add["reference_link_status"])

            achievement = next(row for row in rows if row["evidence_kind"] == "script_achievement_grant")
            self.assertEqual("Achievement", achievement["achievement_title"])
            self.assertEqual("100", achievement["matched_quest_ids"])
            self.assertEqual("achievement_row_matched", achievement["reference_link_status"])

            dynamic = next(row for row in rows if row["source_symbol"] == "objective.ObjectiveInfo.Id")
            self.assertEqual("dynamic_objective", dynamic["progression_targeting"])
            self.assertEqual("dynamic_objective_expression", dynamic["reference_link_status"])
            self.assertEqual("", dynamic["script_owner_ids"])
            self.assertEqual("555", dynamic["script_creature_ids"])
            self.assertEqual("dynamic_progression_source_expression_tracked", dynamic["source_trace_status"])
            self.assertTrue(all(row["completion_status"] == "not_retail_complete" for row in rows))
        finally:
            shutil.rmtree(script_root, ignore_errors=True)

    def test_script_quest_progression_overrides_review_tested_queue_rows(self) -> None:
        virtual_collect_type = audit.load_quest_objective_type_values()["VirtualCollect"]

        def quest_row(quest_id: int, title_text_id: int, objective_ids: list[int]) -> dict[str, str]:
            row = {"ID": str(quest_id), "localizedTextIdTitle": str(title_text_id)}
            for index, column in enumerate(audit.QUEST_OBJECTIVE_COLUMNS):
                row[column] = str(objective_ids[index]) if index < len(objective_ids) else "0"
            return row

        strings = {
            3486: "Empowered Tower",
            3673: "Contact with Thayd",
            3797: "Securing the Area",
            5573: "Powering Down",
            5575: "Seizing Power",
            5597: "Dregs and Thieves",
            4987: "Arrival objective",
            4485: "Loftite crystal objective",
            13391: "Signal flare hidden objective",
            12870: "Powering Down hidden objective",
            12871: "Seizing Power hidden objective",
            8256: "Chua explosives objective",
        }
        quests = [
            quest_row(3486, 3486, [4987, 4485]),
            quest_row(3673, 3673, [13391]),
            quest_row(3797, 3797, []),
            quest_row(5573, 5573, [12870]),
            quest_row(5575, 5575, [12871]),
            quest_row(5597, 5597, [8256]),
        ]
        quest_output = [
            {"quest_id": quest_id, "bucket": "curated_full", "completion_status": "not_retail_complete"}
            for quest_id in (3486, 3673, 3797, 5573, 5575, 5597)
        ]
        objectives = [
            {"ID": "4987", "type": "5", "data": "0", "localizedTextIdFull": "4987", "localizedTextIdShort": "0"},
            {"ID": "4485", "type": str(virtual_collect_type), "data": "206", "localizedTextIdFull": "4485", "localizedTextIdShort": "0"},
            {"ID": "13391", "type": "5", "data": "0", "localizedTextIdFull": "13391", "localizedTextIdShort": "0"},
            {"ID": "12870", "type": "5", "data": "0", "localizedTextIdFull": "12870", "localizedTextIdShort": "0"},
            {"ID": "12871", "type": "5", "data": "0", "localizedTextIdFull": "12871", "localizedTextIdShort": "0"},
            {"ID": "8256", "type": str(virtual_collect_type), "data": "364", "localizedTextIdFull": "8256", "localizedTextIdShort": "0"},
        ]

        rows = audit.build_script_quest_progression_evidence_rows(
            strings,
            quests,
            quest_output,
            objectives,
            [],
            audit.DEFAULT_SCRIPT_MAIN,
        )
        rows_by_key = {
            (
                row["source_path"],
                int(row["source_line"]),
                row["evidence_kind"],
                row["source_symbol"],
            ): row
            for row in rows
        }

        for key, override in audit.SCRIPT_QUEST_PROGRESSION_EVIDENCE_OVERRIDES.items():
            self.assertIn(key, rows_by_key)
            row = rows_by_key[key]
            self.assertEqual(override["evidence_status"], row["evidence_status"])
            self.assertNotRegex(row["evidence_status"], "pending|blocked|wip|mapped_only|reviewed")
            self.assertEqual("not_retail_complete", row["completion_status"])

    def test_script_runtime_action_rows_track_runtime_mechanics(self) -> None:
        script_root = Path(tempfile.mkdtemp(prefix="nf-content-audit-runtime-action-"))
        try:
            (script_root / "Example").mkdir(parents=True)
            (script_root / "Example" / "ExampleRuntimeScript.cs").write_text(
                """
namespace NexusForever.Script.Instance.Example
{
    [ScriptFilterOwnerId(50)]
    [ScriptFilterScriptName("ExampleRuntimeScript")]
    public class ExampleRuntimeScript
    {
        private const uint SpellId = 123u;
        private const ushort WorldId = 1;

        public void Run()
        {
            player.CastSpell(SpellId, spellParameters);
            player.TeleportTo(WorldId, 2f, 3f, 4f);
            player.TeleportToLocal(destination.Position, false);
            var trigger = publicEvent.CreateEntity<IWorldLocationVolumeGridTriggerEntity>();
            mapInstance.EnqueueAdd(trigger, position);
            trigger.RemoveFromMap();
            publicEvent.Finish(PublicEventTeam.PublicTeam);
        }
    }
}
""".lstrip(),
                encoding="utf-8",
            )

            rows = audit.build_script_runtime_action_evidence_rows(
                [script_root],
                [{"ID": "123", "description": "Fixture spell"}],
                [{"ID": "1", "assetPath": "Map\\Fixture"}],
            )

            categories = {row["action_category"] for row in rows}
            self.assertEqual(
                {
                    "script_entity_add_to_map",
                    "script_entity_create",
                    "script_entity_remove_from_map",
                    "script_local_teleport",
                    "script_public_event_finish",
                    "script_spell_cast",
                    "script_teleport",
                },
                categories,
            )
            self.assertEqual(7, len(rows))
            spell = next(row for row in rows if row["action_category"] == "script_spell_cast")
            self.assertEqual("SpellId", spell["source_symbol"])
            self.assertEqual(123, spell["reference_id"])
            self.assertEqual("numeric_constant_resolved", spell["reference_link_status"])
            self.assertEqual("spell4_row_matched", spell["client_reference_status"])
            self.assertEqual("Fixture spell", spell["client_reference_name"])
            self.assertEqual("literal_or_constant_client_reference_tracked", spell["source_trace_status"])

            teleport = next(row for row in rows if row["action_category"] == "script_teleport")
            self.assertEqual("WorldId", teleport["source_symbol"])
            self.assertEqual(1, teleport["reference_id"])
            self.assertEqual("numeric_constant_resolved", teleport["reference_link_status"])
            self.assertEqual("world_row_matched", teleport["client_reference_status"])
            self.assertEqual("Map\\Fixture", teleport["client_reference_name"])
            self.assertEqual("literal_or_constant_client_reference_tracked", teleport["source_trace_status"])

            create = next(row for row in rows if row["action_category"] == "script_entity_create")
            self.assertEqual("IWorldLocationVolumeGridTriggerEntity", create["source_symbol"])
            self.assertEqual("generic_entity_type_tracked", create["reference_link_status"])
            self.assertEqual("runtime_entity_type_tracked", create["client_reference_status"])
            self.assertEqual("runtime_entity_type_source_tracked", create["source_trace_status"])

            finish = next(row for row in rows if row["action_category"] == "script_public_event_finish")
            self.assertEqual("PublicEventTeam.PublicTeam", finish["source_symbol"])
            self.assertEqual("source_expression_tracked", finish["reference_link_status"])
            self.assertEqual("public_event_team_expression_tracked", finish["client_reference_status"])
            self.assertEqual("public_event_finish_team_source_tracked", finish["source_trace_status"])
            self.assertTrue(all(row["completion_status"] == "not_retail_complete" for row in rows))
        finally:
            shutil.rmtree(script_root, ignore_errors=True)

    def test_script_runtime_action_traces_helper_spell_constants(self) -> None:
        script_root = Path(tempfile.mkdtemp(prefix="nf-content-audit-runtime-action-helper-"))
        try:
            (script_root / "Example").mkdir(parents=True)
            (script_root / "Example" / "ExampleHelperScript.cs").write_text(
                """
namespace NexusForever.Script.Main.Example
{
    public class ExampleHelperScript
    {
        private const uint HoverboardSprintVisualSpellId = 82298u;

        public void Run()
        {
            CastPlayerSpell(HoverboardSprintVisualSpellId);
        }

        private void CastPlayerSpell(uint spellId)
        {
            owner.Player.CastSpell(spellId, spellParameters);
        }
    }
}
""".lstrip(),
                encoding="utf-8",
            )

            rows = audit.build_script_runtime_action_evidence_rows(
                [script_root],
                [{"ID": "82298", "description": "Hoverboard Sprint - Visual"}],
                [],
            )

            spell = next(row for row in rows if row["action_category"] == "script_spell_cast")
            self.assertEqual("spellId<-HoverboardSprintVisualSpellId", spell["source_symbol"])
            self.assertEqual(82298, spell["reference_id"])
            self.assertEqual("helper_numeric_constant_resolved", spell["reference_link_status"])
            self.assertEqual("spell4_row_matched", spell["client_reference_status"])
            self.assertEqual("starter_tutorial_helper_spell_constant_traced", spell["source_trace_status"])
        finally:
            shutil.rmtree(script_root, ignore_errors=True)

    def test_script_runtime_action_overrides_review_tested_downsizer_rows(self) -> None:
        rows = audit.build_script_runtime_action_evidence_rows([audit.DEFAULT_SCRIPT_INSTANCE])
        rows_by_key = {
            (
                row["source_path"],
                int(row["source_line"]),
                row["action_category"],
                row["source_call"],
                row["source_symbol"],
            ): row
            for row in rows
        }

        for key, override in audit.SCRIPT_RUNTIME_ACTION_EVIDENCE_OVERRIDES.items():
            self.assertIn(key, rows_by_key)
            row = rows_by_key[key]
            self.assertEqual(override["evidence_status"], row["evidence_status"])
            self.assertNotRegex(row["evidence_status"], "pending|blocked|wip|mapped_only|reviewed")
            self.assertEqual("not_retail_complete", row["completion_status"])

    def test_script_runtime_action_overrides_cover_promoted_expedition_trigger_rows(self) -> None:
        rows = audit.build_script_runtime_action_evidence_rows([audit.DEFAULT_SCRIPT_INSTANCE])
        rows_by_key = {
            (
                row["source_path"],
                int(row["source_line"]),
                row["action_category"],
                row["source_call"],
                row["source_symbol"],
            ): row
            for row in rows
        }
        expected_statuses = {
            (
                r"Source\NexusForever.Script.Instance\Expedition\FragmentZero\FragmentZeroEventScript.cs",
                215,
                "script_entity_create",
                "CreateEntity",
                "ITurnstileGridTriggerEntity",
            ): "runtime_fragment_zero_missing_crew_turnstile_trigger_create_action_tested",
            (
                r"Source\NexusForever.Script.Instance\Expedition\FragmentZero\FragmentZeroEventScript.cs",
                225,
                "script_entity_create",
                "CreateEntity",
                "IWorldLocationVolumeGridTriggerEntity",
            ): "runtime_fragment_zero_worldlocation_trigger_create_action_tested",
            (
                r"Source\NexusForever.Script.Instance\Expedition\FragmentZero\FragmentZeroEventScript.cs",
                236,
                "script_entity_create",
                "CreateEntity",
                "IGridTriggerEntity",
            ): "runtime_fragment_zero_search_grid_trigger_create_action_tested",
            (
                r"Source\NexusForever.Script.Instance\Expedition\Gauntlet\GauntletEventScript.cs",
                145,
                "script_entity_create",
                "CreateEntity",
                "IWorldLocationVolumeGridTriggerEntity",
            ): "runtime_gauntlet_gather_worldlocation_trigger_create_action_tested",
            (
                r"Source\NexusForever.Script.Instance\Expedition\SpaceMadness\SpaceMadnessEventScript.cs",
                528,
                "script_entity_create",
                "CreateEntity",
                "IWorldLocationVolumeGridTriggerEntity",
            ): "runtime_space_madness_gather_worldlocation_trigger_create_action_tested",
            (
                r"Source\NexusForever.Script.Instance\Expedition\SpaceMadness\SpaceMadnessEventScript.cs",
                541,
                "script_entity_create",
                "CreateEntity",
                "INonPlayerEntity",
            ): "runtime_space_madness_talk_npc_create_action_tested",
            (
                r"Source\NexusForever.Script.Instance\Expedition\SpaceMadness\SpaceMadnessEventScript.cs",
                555,
                "script_entity_create",
                "CreateEntity",
                "ISimpleEntity",
            ): "runtime_space_madness_simple_placement_create_action_tested",
            (
                r"Source\NexusForever.Script.Instance\Expedition\SpaceMadness\SpaceMadnessEventScript.cs",
                568,
                "script_entity_create",
                "CreateEntity",
                "ISimpleEntity",
            ): "runtime_space_madness_datapad_simple_placement_create_action_tested",
            (
                r"Source\NexusForever.Script.Instance\Expedition\SpaceMadness\SpaceMadnessEventScript.cs",
                583,
                "script_entity_create",
                "CreateEntity",
                "INonPlayerEntity",
            ): "runtime_space_madness_npc_placement_create_action_tested",
            (
                r"Source\NexusForever.Script.Instance\Expedition\SpaceMadness\SpaceMadnessEventScript.cs",
                601,
                "script_entity_create",
                "CreateEntity",
                "INonPlayerEntity",
            ): "runtime_space_madness_hallucinating_livestock_create_action_tested",
            (
                r"Source\NexusForever.Script.Instance\Expedition\SpaceMadness\SpaceMadnessEventScript.cs",
                677,
                "script_entity_add_to_map",
                "EnqueueAdd",
                "entity",
            ): "runtime_space_madness_shared_enqueue_add_action_tested",
            (
                r"Source\NexusForever.Script.Instance\Expedition\SpaceMadness\SpaceMadnessEventScript.cs",
                733,
                "script_public_event_finish",
                "Finish",
                "PublicEventTeam.PublicTeam",
            ): "runtime_space_madness_final_signal_finish_action_tested",
        }

        for key, expected_status in expected_statuses.items():
            self.assertIn(key, rows_by_key)
            row = rows_by_key[key]
            self.assertEqual(expected_status, row["evidence_status"])
            self.assertFalse(audit.is_script_runtime_action_blocker(row))

    def test_instance_dependency_rows_cover_client_and_script_dependencies(self) -> None:
        strings = {10: "Instance", 20: "Event", 30: "Event objective", 40: "Queue map", 50: "Achievement"}
        instance_rows = [
            {
                "content_type": "dungeon",
                "world_id": 1,
                "name": "Instance",
                "public_event_ids": "100",
                "related_achievement_ids": "300",
                "script_paths": "Source/NexusForever.Script.Instance/Dungeon/Example/ExampleMapScript.cs",
                "script_owner_ids": "1;100",
                "evidence_status": "mapped_only_pending_manual_smoke",
                "blocker_summary": "pending smoke",
            }
        ]
        public_events = [
            {
                "ID": "100",
                "worldId": "1",
                "worldZoneId": "2",
                "localizedTextIdName": "20",
                "worldLocation2Id": "200",
                "publicEventFlags": "4",
                "rewardRotationContentId": "5",
            }
        ]
        public_event_objectives = [
            {
                "ID": "101",
                "publicEventId": "100",
                "publicEventObjectiveFlags": "8",
                "localizedTextId": "30",
                "localizedTextIdShort": "0",
                "publicEventObjectiveTypeEnum": "6",
                "count": "7",
                "objectId": "201",
            }
        ]
        matching_maps = [
            {
                "ID": "200",
                "localizedTextIdName": "40",
                "matchingGameTypeId": "201",
                "worldId": "1",
                "matchingGameMapEnumFlags": "1",
                "achievementCategoryId": "301",
            }
        ]
        matching_types = [{"ID": "201", "matchTypeEnum": "2"}]
        achievements = [
            {
                "ID": "300",
                "achievementTypeId": "12",
                "achievementCategoryId": "301",
                "flags": "0",
                "worldZoneId": "2",
                "localizedTextIdTitle": "50",
                "objectId": "202",
                "value": "3",
            }
        ]
        data_mapping_output = Path(tempfile.mkdtemp(prefix="nf-content-audit-instance-dependencies-"))

        rows = audit.build_instance_dependency_rows(
            strings,
            instance_rows,
            public_events,
            public_event_objectives,
            matching_maps,
            matching_types,
            achievements,
            data_mapping_output,
        )

        self.assertEqual(
            {"public_event", "public_event_objective", "matching_game_map", "achievement", "script"},
            {row["dependency_type"] for row in rows},
        )
        self.assertTrue(all(row["completion_status"] == "not_retail_complete" for row in rows))
        self.assertIn("runtime_script_present_pending_mechanics_and_smoke", {row["evidence_status"] for row in rows})

    def test_instance_script_handler_rows_track_tracked_instance_lifecycle_methods(self) -> None:
        script_root = Path(tempfile.mkdtemp(prefix="nf-content-audit-instance-handlers-"))
        try:
            (script_root / "Expedition" / "Example").mkdir(parents=True)
            (script_root / "Expedition" / "Example" / "ExampleMapScript.cs").write_text(
                """
using NexusForever.Script.Template.Filter;

namespace NexusForever.Script.Instance.Expedition.Example
{
    [ScriptFilterOwnerId(1)]
    public class ExampleMapScript : EventBaseContentMapScript
    {
        public override void OnAddToMap(IGridEntity entity)
        {
        }
    }
}
""".lstrip(),
                encoding="utf-8",
            )
            (script_root / "Expedition" / "Example" / "ExampleEventScript.cs").write_text(
                """
using NexusForever.Script.Template.Filter;

namespace NexusForever.Script.Instance.Expedition.Example
{
    [ScriptFilterOwnerId(100)]
    [ScriptFilterScriptName("Example Event Script")]
    public class ExampleEventScript : IPublicEventScript, IOwnedScript<IPublicEvent>
    {
        public void OnLoad(IPublicEvent owner)
        {
        }

        public void OnPublicEventPhase(uint phase)
        {
        }

        public void OnPublicEventObjectiveStatus(IPublicEventObjective objective)
        {
        }

        public void OnCinematicFinish(IPlayer player, uint cinematicId)
        {
        }

        private void OnHelper()
        {
        }
    }
}
""".lstrip(),
                encoding="utf-8",
            )
            instance_rows = [
                {
                    "content_type": "expedition",
                    "world_id": 1,
                    "name": "Example Expedition",
                    "public_event_ids": "100",
                    "script_paths": (
                        r"Source\NexusForever.Script.Instance\Expedition\Example\ExampleMapScript.cs;"
                        r"Source\NexusForever.Script.Instance\Expedition\Example\ExampleEventScript.cs"
                    ),
                }
            ]

            rows = audit.build_instance_script_handler_evidence_rows(instance_rows, script_root)

            self.assertEqual(5, len(rows))
            self.assertEqual(
                {
                    "map_entity_lifecycle",
                    "script_lifecycle",
                    "public_event_lifecycle",
                    "cinematic_lifecycle",
                },
                {row["handler_category"] for row in rows},
            )
            self.assertNotIn("OnHelper", {row["handler_name"] for row in rows})

            map_add = next(row for row in rows if row["handler_name"] == "OnAddToMap")
            self.assertEqual("Example Expedition", map_add["instance_name"])
            self.assertEqual("map_script", map_add["script_kind"])
            self.assertEqual("1", map_add["script_owner_ids"])
            self.assertEqual("script_owner_matches_instance_world", map_add["owner_link_status"])

            event_load = next(row for row in rows if row["handler_name"] == "OnLoad")
            self.assertEqual("public_event_script", event_load["script_kind"])
            self.assertEqual("100", event_load["script_owner_ids"])
            self.assertEqual("Example Event Script", event_load["script_filter_name"])
            self.assertEqual("script_owner_matches_instance_public_event", event_load["owner_link_status"])
            self.assertTrue(all(row["completion_status"] == "not_retail_complete" for row in rows))
        finally:
            shutil.rmtree(script_root, ignore_errors=True)

    def test_instance_script_handler_overrides_review_tested_downsizer_rows(self) -> None:
        instance_rows = [
            {
                "content_type": "expedition",
                "world_id": 2149,
                "name": "Space Madness",
                "public_event_ids": "390",
                "script_paths": r"Source\NexusForever.Script.Instance\Expedition\SpaceMadness\SpaceMadnessMapScript.cs",
            },
            {
                "content_type": "expedition",
                "world_id": 3180,
                "name": "Fragment Zero",
                "public_event_ids": "680",
                "script_paths": r"Source\NexusForever.Script.Instance\Expedition\FragmentZero\FragmentZeroMapScript.cs",
            },
            {
                "content_type": "expedition",
                "world_id": 3404,
                "name": "Evil from the Ether",
                "public_event_ids": "781",
                "script_paths": r"Source\NexusForever.Script.Instance\Expedition\EvilFromTheEther\EvilFromTheEtherMapScript.cs",
            },
            {
                "content_type": "dungeon",
                "world_id": 3041,
                "name": r"Map\UltimateProtogamesRaid",
                "public_event_ids": "642",
                "script_paths": r"Source\NexusForever.Script.Instance\Dungeon\UltimateProtogames\Downsizer\UltimateProtogamesEventScript.cs",
            }
        ]

        rows = audit.build_instance_script_handler_evidence_rows(
            instance_rows,
            audit.DEFAULT_SCRIPT_INSTANCE,
        )
        rows_by_key = {
            (
                int(row["world_id"]),
                row["script_path"],
                int(row["source_line"]),
                row["handler_name"],
            ): row
            for row in rows
        }

        for key, override in audit.INSTANCE_SCRIPT_HANDLER_EVIDENCE_OVERRIDES.items():
            self.assertIn(key, rows_by_key)
            row = rows_by_key[key]
            self.assertEqual(override["evidence_status"], row["evidence_status"])
            self.assertNotRegex(row["evidence_status"], "pending|blocked|wip|mapped_only|reviewed")
            self.assertEqual("not_retail_complete", row["completion_status"])

    def test_instance_reward_rows_cover_rotation_and_matching_random_dependencies(self) -> None:
        instance_rows = [
            {
                "content_type": "dungeon",
                "world_id": 1,
                "name": "Instance",
                "reward_rotation_content_id": 12,
            }
        ]
        instance_dependency_rows = [
            {
                "content_type": "dungeon",
                "world_id": 1,
                "instance_name": "Instance",
                "dependency_type": "public_event",
                "dependency_id": 100,
                "dependency_name": "Event",
                "reward_rotation_content_id": 13,
                "match_type_enum": "",
            },
            {
                "content_type": "dungeon",
                "world_id": 1,
                "instance_name": "Instance",
                "dependency_type": "matching_game_map",
                "dependency_id": 200,
                "dependency_name": "Queue map",
                "reward_rotation_content_id": "",
                "match_type_enum": 1,
            },
        ]
        reward_rotation_content = [
            {"ID": "12", "contentTypeEnum": "1"},
            {"ID": "13", "contentTypeEnum": "1"},
            {"ID": "46", "contentTypeEnum": "6"},
        ]
        reward_rotation_items = [{"ID": "3"}]
        reward_rotation_modifiers = [{"ID": "5"}]
        reward_rotation_essences = [{"ID": "7"}]
        match_type_reward_rotation_content = [
            {"ID": "1", "matchTypeEnum": "1", "rewardRotationContentIdRandomNormal": "46", "rewardRotationContentIdRandomVeteran": "0"}
        ]
        matching_random_rewards = [
            {
                "ID": "2",
                "matchTypeEnum": "1",
                "item2Id": "92289",
                "itemCount": "1",
                "currencyTypeId": "1",
                "currencyValue": "250000",
                "xpEarned": "125000",
                "item2IdLevelScale": "0",
                "itemCountLevelScale": "0",
                "currencyTypeIdLevelScale": "1",
                "currencyValueLevelScale": "125000",
                "xpEarnedLevelScale": "62500",
                "worldDifficultyEnum": "0",
            }
        ]

        rows = audit.build_instance_reward_rows(
            instance_rows,
            instance_dependency_rows,
            reward_rotation_content,
            reward_rotation_items,
            reward_rotation_modifiers,
            reward_rotation_essences,
            match_type_reward_rotation_content,
            matching_random_rewards,
        )

        self.assertEqual(
            {
                "world_reward_rotation_content",
                "public_event_reward_rotation_content",
                "match_type_reward_rotation_content_random_normal",
                "matching_random_reward",
            },
            {row["reward_source"] for row in rows},
        )
        self.assertIn(46, {row["reward_rotation_content_id"] for row in rows if row["reward_rotation_content_id"]})
        self.assertIn(92289, {row["item2_id"] for row in rows if row["item2_id"]})
        self.assertTrue(all(row["global_reward_rotation_item_ids"] == "3" for row in rows))
        self.assertTrue(all(row["completion_status"] == "not_retail_complete" for row in rows))


if __name__ == "__main__":
    unittest.main()
