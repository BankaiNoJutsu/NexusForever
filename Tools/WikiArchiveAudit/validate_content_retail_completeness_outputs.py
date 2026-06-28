#!/usr/bin/env python3
"""Validate generated content retail-completeness tracker outputs."""

from __future__ import annotations

import argparse
import csv
import re
import sys
from collections import Counter
from dataclasses import dataclass
from pathlib import Path
from typing import Sequence

_CSV_FIELD_SIZE_LIMIT = sys.maxsize
while True:
    try:
        csv.field_size_limit(_CSV_FIELD_SIZE_LIMIT)
        break
    except OverflowError:
        _CSV_FIELD_SIZE_LIMIT //= 10

ROOT = Path(__file__).resolve().parents[2]
DEFAULT_OUTPUT_DIR = ROOT / "Decomp" / "Analysis" / "coverage"
DEFAULT_TRACKER = ROOT / "Decomp" / "Analysis" / "CONTENT_RETAIL_COMPLETENESS_TRACKER.md"
DEFAULT_ROLLUP_DOCS = (
    ROOT / "CURRENT_STATUS.md",
    ROOT / "Decomp" / "Analysis" / "MISSING_FEATURE_MATRIX.md",
)
DEFAULT_SUBAGENT_FINDINGS_INVENTORY = ROOT / "Tools" / "WikiArchiveAudit" / "reports" / "content-retail-subagent-findings.csv"

REQUIRED_COLUMNS = (
    "evidence_status",
    "evidence_sources",
    "manual_validation",
    "completion_status",
    "blocker_summary",
)
NEXT_SLICE_QUEUE_CSV_NAME = "content_retail_completeness_next_slice_queue.csv"
NEXT_SLICE_BLOCKER_CSV_NAME = "content_retail_completeness_next_slice_blockers.csv"
QUEST_CSV_NAME = "content_retail_completeness_quests.csv"
QUEST_OBJECTIVE_CSV_NAME = "content_retail_completeness_quest_objectives.csv"
QUEST_ACHIEVEMENT_CSV_NAME = "content_retail_completeness_quest_achievements.csv"
QUEST_CREATURE_CSV_NAME = "content_retail_completeness_quest_creatures.csv"
QUEST_LOOT_CSV_NAME = "content_retail_completeness_quest_loot.csv"
QUEST_OBJECTIVE_EVIDENCE_CSV_NAME = "content_retail_completeness_quest_objective_evidence.csv"
QUEST_PREREQUISITE_CSV_NAME = "content_retail_completeness_quest_prerequisites.csv"
QUEST_REWARD_EVIDENCE_CSV_NAME = "content_retail_completeness_quest_reward_evidence.csv"
QUEST_REWARD_CSV_NAME = "content_retail_completeness_quest_rewards.csv"
QUEST_SCRIPT_CSV_NAME = "content_retail_completeness_quest_scripts.csv"
QUEST_WORLD_DEPENDENCY_CSV_NAME = "content_retail_completeness_quest_world_dependencies.csv"
QUEST_ZONE_EVIDENCE_CSV_NAME = "content_retail_completeness_quest_zone_evidence.csv"
QUEST_EPISODE_EVIDENCE_CSV_NAME = "content_retail_completeness_quest_episode_evidence.csv"
PUBLIC_EVENT_EVIDENCE_CSV_NAME = "content_retail_completeness_public_event_evidence.csv"
PUBLIC_EVENT_AUXILIARY_EVIDENCE_CSV_NAME = "content_retail_completeness_public_event_auxiliary_evidence.csv"
CHALLENGE_EVIDENCE_CSV_NAME = "content_retail_completeness_challenge_evidence.csv"
PATH_MISSION_EVIDENCE_CSV_NAME = "content_retail_completeness_path_mission_evidence.csv"
CONTRACT_EVIDENCE_CSV_NAME = "content_retail_completeness_contract_evidence.csv"
INSTANCE_CSV_NAME = "content_retail_completeness_instances.csv"
INSTANCE_DEPENDENCY_CSV_NAME = "content_retail_completeness_instance_dependencies.csv"
INSTANCE_ENTITY_CSV_NAME = "content_retail_completeness_instance_entities.csv"
INSTANCE_PORTAL_EVIDENCE_CSV_NAME = "content_retail_completeness_instance_portal_evidence.csv"
INSTANCE_REWARD_CSV_NAME = "content_retail_completeness_instance_rewards.csv"
INSTANCE_SCRIPT_HANDLER_EVIDENCE_CSV_NAME = "content_retail_completeness_instance_script_handler_evidence.csv"
SCRIPT_EVENT_FLOW_EVIDENCE_CSV_NAME = "content_retail_completeness_script_event_flow_evidence.csv"
SCRIPT_OBJECTIVE_PRODUCER_EVIDENCE_CSV_NAME = "content_retail_completeness_script_objective_producer_evidence.csv"
SCRIPT_PRESENTATION_EVIDENCE_CSV_NAME = "content_retail_completeness_script_presentation_evidence.csv"
SCRIPT_QUEST_PROGRESSION_EVIDENCE_CSV_NAME = "content_retail_completeness_script_quest_progression_evidence.csv"
SCRIPT_RUNTIME_ACTION_EVIDENCE_CSV_NAME = "content_retail_completeness_script_runtime_action_evidence.csv"
QUEUE_REQUIRED_COLUMNS = (
    "queue_rank",
    "slice_id",
    "lane",
    "priority",
    "source_inventory",
    "source_row_key",
    "objective_row_count",
    "scripted_producer_count",
    "scripted_producer_objective_count",
    "scripted_producer_coverage",
    "handler_count",
    "seed_entity_count",
    "blocker_detail_count",
    "blocker_count_by_category",
    "blocker_detail_filter",
    "first_blocker_ids",
    "actionability_state",
    "split_required",
    "split_reason",
    "primary_blocker_category",
    "implementation_surface",
    "retail_claim_allowed",
    "required_evidence_level",
    "blocking_evidence_source",
    "validation_bundle_name",
    "narrow_test_filter",
    "narrow_test_command",
    "harness_command",
)
ALLOWED_QUEUE_PRIORITIES = {"P0", "P1", "P2"}
ALLOWED_QUEUE_LANES = {"quest", "instance"}
ALLOWED_QUEUE_ACTIONABILITY_STATES = {
    "quest_validation_ready",
    "quest_mapping_then_smoke",
    "instance_validation_ready",
    "split_required_high_blocker_volume",
    "split_required_producer_gap",
    "validation_ready",
    "mapped_only",
}
ALLOWED_BOOLEAN_TEXT = {"false", "true"}
ALLOWED_REQUIRED_EVIDENCE_LEVELS = {"Mapped", "Verified"}
ALLOWED_BLOCKING_EVIDENCE_SOURCES = {"client_smoke", "datamapping_review", "runtime_test"}
QUEUE_SPLIT_BLOCKER_DETAIL_THRESHOLD = 100
QUEUE_SPLIT_PRODUCER_COVERAGE_PERCENT = 50
ALLOWED_COMPLETION_STATUSES = {"not_retail_complete", "retail_complete", "rejected"}
PENDING_MANUAL_VALUES = {"", "pending", "todo", "tbd", "unknown"}
RETAIL_COMPLETE_EVIDENCE_STATUSES = {
    "implemented_with_automated_and_manual_validation",
    "verified_retail_complete",
}
OPEN_EVIDENCE_STATUS_MARKERS = ("blocked", "pending", "wip", "mapped_only", "observed", "correlated")
OPEN_BLOCKER_MARKERS = ("blocked", "pending", "needs ", "remain", "unknown", "wip")
EVIDENCE_SOURCE_SEPARATOR = ";"
EXPECTED_CSV_NAMES = (
    "content_retail_completeness_challenge_evidence.csv",
    "content_retail_completeness_contract_evidence.csv",
    "content_retail_completeness_instance_dependencies.csv",
    "content_retail_completeness_instance_entities.csv",
    "content_retail_completeness_instance_portal_evidence.csv",
    "content_retail_completeness_instance_rewards.csv",
    "content_retail_completeness_instance_script_handler_evidence.csv",
    "content_retail_completeness_instances.csv",
    "content_retail_completeness_next_slice_blockers.csv",
    "content_retail_completeness_next_slice_queue.csv",
    "content_retail_completeness_path_mission_evidence.csv",
    "content_retail_completeness_public_event_auxiliary_evidence.csv",
    "content_retail_completeness_public_event_evidence.csv",
    "content_retail_completeness_quest_achievements.csv",
    "content_retail_completeness_quest_creatures.csv",
    "content_retail_completeness_quest_episode_evidence.csv",
    "content_retail_completeness_quest_loot.csv",
    "content_retail_completeness_quest_objective_evidence.csv",
    "content_retail_completeness_quest_objectives.csv",
    "content_retail_completeness_quest_prerequisites.csv",
    "content_retail_completeness_quest_reward_evidence.csv",
    "content_retail_completeness_quest_rewards.csv",
    "content_retail_completeness_quest_scripts.csv",
    "content_retail_completeness_quest_world_dependencies.csv",
    "content_retail_completeness_quest_zone_evidence.csv",
    "content_retail_completeness_quests.csv",
    "content_retail_completeness_script_event_flow_evidence.csv",
    "content_retail_completeness_script_objective_producer_evidence.csv",
    "content_retail_completeness_script_presentation_evidence.csv",
    "content_retail_completeness_script_quest_progression_evidence.csv",
    "content_retail_completeness_script_runtime_action_evidence.csv",
)

BLOCKER_REQUIRED_COLUMNS = (
    "queue_rank",
    "slice_id",
    "lane",
    "priority",
    "blocker_rank",
    "blocker_id",
    "blocker_category",
    "blocker_type",
    "source_inventory",
    "source_row_key",
    "related_ids",
    "required_validation",
    "next_action",
    "tracker_paths",
)
BLOCKER_CATEGORY_SOURCE_INVENTORIES = {
    "quest_reward_evidence": "content_retail_completeness_quest_reward_evidence.csv",
    "quest_reward": "content_retail_completeness_quest_rewards.csv",
    "quest_objective": "content_retail_completeness_quest_objectives.csv",
    "quest_objective_evidence": "content_retail_completeness_quest_objective_evidence.csv",
    "quest_zone_evidence": "content_retail_completeness_quest_zone_evidence.csv",
    "quest_episode_evidence": "content_retail_completeness_quest_episode_evidence.csv",
    "quest_creature": "content_retail_completeness_quest_creatures.csv",
    "quest_world_dependency": "content_retail_completeness_quest_world_dependencies.csv",
    "quest_achievement": "content_retail_completeness_quest_achievements.csv",
    "quest_prerequisite": "content_retail_completeness_quest_prerequisites.csv",
    "quest_loot": "content_retail_completeness_quest_loot.csv",
    "quest_script": "content_retail_completeness_quest_scripts.csv",
    "script_quest_progression": "content_retail_completeness_script_quest_progression_evidence.csv",
    "instance_dependency": "content_retail_completeness_instance_dependencies.csv",
    "public_event_evidence": "content_retail_completeness_public_event_evidence.csv",
    "script_presentation": "content_retail_completeness_script_presentation_evidence.csv",
    "script_event_flow": "content_retail_completeness_script_event_flow_evidence.csv",
    "script_objective_producer": "content_retail_completeness_script_objective_producer_evidence.csv",
    "script_runtime_action": "content_retail_completeness_script_runtime_action_evidence.csv",
    "instance_entity": "content_retail_completeness_instance_entities.csv",
    "instance_portal_evidence": "content_retail_completeness_instance_portal_evidence.csv",
    "instance_reward": "content_retail_completeness_instance_rewards.csv",
    "instance_script_handler": "content_retail_completeness_instance_script_handler_evidence.csv",
}
VIRTUAL_SOURCE_ROW_KEY_FIELDS = {
    "content_retail_completeness_instance_portal_evidence.csv": ("world_id",),
}
QUEUE_SOURCE_ID_FIELDS = {
    QUEST_CSV_NAME: "quest_id",
    INSTANCE_CSV_NAME: "world_id",
}
TOP_GAP_ROW_SOURCES = {
    "Quest identity and objective coverage": (
        QUEST_CSV_NAME,
        QUEST_OBJECTIVE_CSV_NAME,
        QUEST_OBJECTIVE_EVIDENCE_CSV_NAME,
    ),
    "Quest world, zone, and creature placement proof": (
        QUEST_WORLD_DEPENDENCY_CSV_NAME,
        QUEST_ZONE_EVIDENCE_CSV_NAME,
        QUEST_CREATURE_CSV_NAME,
    ),
    "Quest prerequisites and episode chain semantics": (
        QUEST_PREREQUISITE_CSV_NAME,
        QUEST_EPISODE_EVIDENCE_CSV_NAME,
    ),
    "Quest rewards, loot, and achievement hooks": (
        QUEST_REWARD_CSV_NAME,
        QUEST_REWARD_EVIDENCE_CSV_NAME,
        QUEST_LOOT_CSV_NAME,
        QUEST_ACHIEVEMENT_CSV_NAME,
    ),
    "Public events, challenges, path missions, and contracts": (
        PUBLIC_EVENT_EVIDENCE_CSV_NAME,
        PUBLIC_EVENT_AUXILIARY_EVIDENCE_CSV_NAME,
        CHALLENGE_EVIDENCE_CSV_NAME,
        PATH_MISSION_EVIDENCE_CSV_NAME,
        CONTRACT_EVIDENCE_CSV_NAME,
    ),
    "Instance mechanics, portals, and encounter scripting": (
        INSTANCE_CSV_NAME,
        INSTANCE_DEPENDENCY_CSV_NAME,
        INSTANCE_ENTITY_CSV_NAME,
        INSTANCE_PORTAL_EVIDENCE_CSV_NAME,
        INSTANCE_SCRIPT_HANDLER_EVIDENCE_CSV_NAME,
    ),
    "Instance rewards and rotations": (INSTANCE_REWARD_CSV_NAME,),
    "Script producers, presentation, progression, and runtime actions": (
        QUEST_SCRIPT_CSV_NAME,
        SCRIPT_PRESENTATION_EVIDENCE_CSV_NAME,
        SCRIPT_EVENT_FLOW_EVIDENCE_CSV_NAME,
        SCRIPT_OBJECTIVE_PRODUCER_EVIDENCE_CSV_NAME,
        SCRIPT_QUEST_PROGRESSION_EVIDENCE_CSV_NAME,
        SCRIPT_RUNTIME_ACTION_EVIDENCE_CSV_NAME,
    ),
    "Next restoration slice queue": (NEXT_SLICE_QUEUE_CSV_NAME,),
}
SUBAGENT_LANE_SNAPSHOT_HEADERS = (
    "Lane",
    "IDs and tracker paths",
    "Evidence sources",
    "Confidence",
    "Blockers",
    "Next action",
)
SUBAGENT_LANE_SNAPSHOT_PATHS = {
    "Quest inventory/objective coverage": (
        QUEST_CSV_NAME,
        QUEST_OBJECTIVE_CSV_NAME,
        QUEST_WORLD_DEPENDENCY_CSV_NAME,
        QUEST_PREREQUISITE_CSV_NAME,
        QUEST_EPISODE_EVIDENCE_CSV_NAME,
    ),
    "Dungeon, expedition, and raid inventory": (
        INSTANCE_CSV_NAME,
        INSTANCE_DEPENDENCY_CSV_NAME,
        INSTANCE_SCRIPT_HANDLER_EVIDENCE_CSV_NAME,
        INSTANCE_PORTAL_EVIDENCE_CSV_NAME,
    ),
    "Rewards, loot, achievements, and progression hooks": (
        QUEST_REWARD_CSV_NAME,
        QUEST_LOOT_CSV_NAME,
        QUEST_ACHIEVEMENT_CSV_NAME,
        INSTANCE_REWARD_CSV_NAME,
    ),
    "Scripts, encounters, and boss mechanics": (
        QUEST_SCRIPT_CSV_NAME,
        SCRIPT_PRESENTATION_EVIDENCE_CSV_NAME,
        SCRIPT_OBJECTIVE_PRODUCER_EVIDENCE_CSV_NAME,
        SCRIPT_EVENT_FLOW_EVIDENCE_CSV_NAME,
        SCRIPT_QUEST_PROGRESSION_EVIDENCE_CSV_NAME,
        SCRIPT_RUNTIME_ACTION_EVIDENCE_CSV_NAME,
        PUBLIC_EVENT_EVIDENCE_CSV_NAME,
        PUBLIC_EVENT_AUXILIARY_EVIDENCE_CSV_NAME,
        CHALLENGE_EVIDENCE_CSV_NAME,
        PATH_MISSION_EVIDENCE_CSV_NAME,
        CONTRACT_EVIDENCE_CSV_NAME,
    ),
    "Database/data-mapping coverage": (
        QUEST_CREATURE_CSV_NAME,
        QUEST_OBJECTIVE_EVIDENCE_CSV_NAME,
        QUEST_REWARD_EVIDENCE_CSV_NAME,
        QUEST_ZONE_EVIDENCE_CSV_NAME,
        INSTANCE_ENTITY_CSV_NAME,
    ),
    "Decompile/client evidence": (
        "Decomp/Analysis/function_labels.csv",
        "Decomp/Analysis/mapping_artifacts/*.csv",
        "Decomp/Analysis/coverage/opcode_coverage_inventory.csv",
    ),
    "Wiki/archive/reference-data comparison": (
        "Tools/WikiArchiveAudit/reports/quests-wiki-audit.md",
        "Tools/WikiArchiveAudit/audit_wildstar_wiki.py",
        "Tools/WikiArchiveAudit/quest_implementation_audit.py",
    ),
    "Build, test, and static validation": (
        "Tools/WikiArchiveAudit/validate_content_retail_completeness_outputs.py",
        "Tools/WikiArchiveAudit/test_content_retail_completeness_audit.py",
        "Tools/WikiArchiveAudit/test_content_retail_completeness_validator.py",
        "Source/NexusForever.Game.Tests",
        NEXT_SLICE_QUEUE_CSV_NAME,
        NEXT_SLICE_BLOCKER_CSV_NAME,
    ),
}
SUBAGENT_LANE_SNAPSHOT_REQUIRED_TEXT_COLUMNS = (
    "IDs and tracker paths",
    "Evidence sources",
    "Confidence",
    "Blockers",
    "Next action",
)
SUBAGENT_LANE_SNAPSHOT_PLACEHOLDERS = {"", "status", "next", "todo", "tbd", "unknown", "placeholder"}
SUBAGENT_FINDINGS_REQUIRED_COLUMNS = (
    "finding_id",
    "lane",
    "priority",
    "status",
    "finding_type",
    "scope",
    "source_inventory",
    "source_row_key",
    "related_ids",
    "tracker_paths",
    "evidence_sources",
    "confidence",
    "blocker_summary",
    "next_action",
    "notes",
)
ALLOWED_SUBAGENT_FINDING_STATUSES = {"open", "blocked", "in_progress", "closed", "rejected"}
SUBAGENT_FINDING_PLACEHOLDERS = {"", "status", "next", "todo", "tbd", "unknown", "placeholder"}
SUBAGENT_FINDINGS_SECTION_HEADERS = (
    "Finding",
    "Priority",
    "Lane",
    "Scope",
    "Source row",
    "Confidence",
    "Next action",
)


@dataclass(frozen=True)
class MarkdownCountTableSpec:
    table_name: str
    name_column: str
    count_column: str
    csv_field: str
    allow_blank_name: bool = False


@dataclass(frozen=True)
class MarkdownCompositeCountTableSpec:
    table_name: str
    name_columns: tuple[str, ...]
    count_column: str
    csv_fields: tuple[str, ...]


@dataclass(frozen=True)
class MarkdownSummarySectionSpec:
    heading: str
    csv_name: str
    total_label: str
    tables: tuple[MarkdownCountTableSpec, ...]
    composite_tables: tuple[MarkdownCompositeCountTableSpec, ...] = ()


DOMAIN_SUMMARY_SECTION_SPECS = (
    MarkdownSummarySectionSpec(
        heading="## Quest Summary",
        csv_name=QUEST_CSV_NAME,
        total_label="Total Quest2 rows",
        tables=(
            MarkdownCountTableSpec("Quest Summary buckets", "Bucket", "Rows", "bucket"),
            MarkdownCountTableSpec("Quest Summary evidence statuses", "Evidence status", "Rows", "evidence_status"),
        ),
    ),
    MarkdownSummarySectionSpec(
        heading="## Quest Objective Dependency Summary",
        csv_name=QUEST_OBJECTIVE_CSV_NAME,
        total_label="Total objective rows",
        tables=(
            MarkdownCountTableSpec("Quest Objective Dependency Summary support statuses", "Support status", "Rows", "support_status"),
            MarkdownCountTableSpec("Quest Objective Dependency Summary evidence statuses", "Evidence status", "Rows", "evidence_status"),
        ),
    ),
    MarkdownSummarySectionSpec(
        heading="## Quest Reward Dependency Summary",
        csv_name=QUEST_REWARD_CSV_NAME,
        total_label="Total reward/dependency rows",
        tables=(
            MarkdownCountTableSpec("Quest Reward Dependency Summary reward sources", "Reward source", "Rows", "reward_source"),
            MarkdownCountTableSpec("Quest Reward Dependency Summary support statuses", "Support status", "Rows", "support_status"),
        ),
    ),
    MarkdownSummarySectionSpec(
        heading="## Quest Prerequisite Dependency Summary",
        csv_name=QUEST_PREREQUISITE_CSV_NAME,
        total_label="Total prerequisite/dependency rows",
        tables=(
            MarkdownCountTableSpec("Quest Prerequisite Dependency Summary dependency types", "Dependency type", "Rows", "dependency_type"),
            MarkdownCountTableSpec("Quest Prerequisite Dependency Summary support statuses", "Support status", "Rows", "support_status"),
        ),
    ),
    MarkdownSummarySectionSpec(
        heading="## Quest Loot Dependency Summary",
        csv_name=QUEST_LOOT_CSV_NAME,
        total_label="Total quest-loot rows",
        tables=(
            MarkdownCountTableSpec("Quest Loot Dependency Summary dependency types", "Dependency type", "Rows", "dependency_type"),
            MarkdownCountTableSpec("Quest Loot Dependency Summary evidence statuses", "Evidence status", "Rows", "evidence_status"),
        ),
    ),
    MarkdownSummarySectionSpec(
        heading="## Quest World/Interaction Dependency Summary",
        csv_name=QUEST_WORLD_DEPENDENCY_CSV_NAME,
        total_label="Total world/interaction dependency rows",
        tables=(
            MarkdownCountTableSpec("Quest World/Interaction Dependency Summary dependency types", "Dependency type", "Rows", "dependency_type"),
            MarkdownCountTableSpec("Quest World/Interaction Dependency Summary evidence statuses", "Evidence status", "Rows", "evidence_status"),
        ),
    ),
    MarkdownSummarySectionSpec(
        heading="## Quest Achievement/Progression Dependency Summary",
        csv_name=QUEST_ACHIEVEMENT_CSV_NAME,
        total_label="Total achievement/progression dependency rows",
        tables=(
            MarkdownCountTableSpec("Quest Achievement/Progression Dependency Summary dependency types", "Dependency type", "Rows", "dependency_type"),
            MarkdownCountTableSpec("Quest Achievement/Progression Dependency Summary evidence statuses", "Evidence status", "Rows", "evidence_status"),
        ),
    ),
    MarkdownSummarySectionSpec(
        heading="## Quest Script Hook Summary",
        csv_name=QUEST_SCRIPT_CSV_NAME,
        total_label="Total Script.Main owner hook rows",
        tables=(
            MarkdownCountTableSpec("Quest Script Hook Summary script kinds", "Script kind", "Rows", "script_kind"),
            MarkdownCountTableSpec("Quest Script Hook Summary quest link statuses", "Quest link status", "Rows", "quest_link_status"),
            MarkdownCountTableSpec("Quest Script Hook Summary evidence statuses", "Evidence status", "Rows", "evidence_status"),
        ),
    ),
    MarkdownSummarySectionSpec(
        heading="## Quest Creature/NPC Relationship Summary",
        csv_name=QUEST_CREATURE_CSV_NAME,
        total_label="Total DataMapping creature relationship rows",
        tables=(
            MarkdownCountTableSpec("Quest Creature/NPC Relationship Summary relation types", "Relation type", "Rows", "relation_type"),
            MarkdownCountTableSpec("Quest Creature/NPC Relationship Summary quest link statuses", "Quest link status", "Rows", "quest_link_status"),
            MarkdownCountTableSpec("Quest Creature/NPC Relationship Summary creature match statuses", "Creature match status", "Rows", "match_status"),
            MarkdownCountTableSpec("Quest Creature/NPC Relationship Summary evidence statuses", "Evidence status", "Rows", "evidence_status"),
        ),
    ),
    MarkdownSummarySectionSpec(
        heading="## Quest Jabbithole Objective Evidence Summary",
        csv_name=QUEST_OBJECTIVE_EVIDENCE_CSV_NAME,
        total_label="Total DataMapping/Jabbithole objective evidence rows",
        tables=(
            MarkdownCountTableSpec("Quest Jabbithole Objective Evidence Summary quest link statuses", "Quest link status", "Rows", "quest_link_status"),
            MarkdownCountTableSpec("Quest Jabbithole Objective Evidence Summary objective match statuses", "Objective match status", "Rows", "match_status"),
            MarkdownCountTableSpec("Quest Jabbithole Objective Evidence Summary evidence statuses", "Evidence status", "Rows", "evidence_status"),
        ),
    ),
    MarkdownSummarySectionSpec(
        heading="## Quest Jabbithole Reward Evidence Summary",
        csv_name=QUEST_REWARD_EVIDENCE_CSV_NAME,
        total_label="Total DataMapping/Jabbithole reward evidence rows",
        tables=(
            MarkdownCountTableSpec("Quest Jabbithole Reward Evidence Summary reward kinds", "Reward kind", "Rows", "reward_kind"),
            MarkdownCountTableSpec("Quest Jabbithole Reward Evidence Summary quest link statuses", "Quest link status", "Rows", "quest_link_status"),
            MarkdownCountTableSpec("Quest Jabbithole Reward Evidence Summary evidence statuses", "Evidence status", "Rows", "evidence_status"),
        ),
    ),
    MarkdownSummarySectionSpec(
        heading="## Quest Jabbithole Zone Evidence Summary",
        csv_name=QUEST_ZONE_EVIDENCE_CSV_NAME,
        total_label="Total DataMapping/Jabbithole zone evidence rows",
        tables=(
            MarkdownCountTableSpec("Quest Jabbithole Zone Evidence Summary relation sources", "Relation source", "Rows", "relation_source"),
            MarkdownCountTableSpec("Quest Jabbithole Zone Evidence Summary quest link statuses", "Quest link status", "Rows", "quest_link_status"),
            MarkdownCountTableSpec("Quest Jabbithole Zone Evidence Summary zone match statuses", "Zone match status", "Rows", "match_status"),
            MarkdownCountTableSpec("Quest Jabbithole Zone Evidence Summary evidence statuses", "Evidence status", "Rows", "evidence_status"),
        ),
    ),
    MarkdownSummarySectionSpec(
        heading="## Quest Episode Evidence Summary",
        csv_name=QUEST_EPISODE_EVIDENCE_CSV_NAME,
        total_label="Total DataMapping episode-quest evidence rows",
        tables=(
            MarkdownCountTableSpec("Quest Episode Evidence Summary quest link statuses", "Quest link status", "Rows", "quest_link_status"),
            MarkdownCountTableSpec("Quest Episode Evidence Summary episode bridge statuses", "Episode bridge status", "Rows", "episode_bridge_status"),
            MarkdownCountTableSpec("Quest Episode Evidence Summary episode match statuses", "Episode match status", "Rows", "episode_match_status"),
            MarkdownCountTableSpec("Quest Episode Evidence Summary evidence statuses", "Evidence status", "Rows", "evidence_status"),
        ),
    ),
    MarkdownSummarySectionSpec(
        heading="## Public Event DataMapping Evidence Summary",
        csv_name=PUBLIC_EVENT_EVIDENCE_CSV_NAME,
        total_label="Total DataMapping public-event evidence rows",
        tables=(
            MarkdownCountTableSpec("Public Event DataMapping Evidence Summary evidence kinds", "Evidence kind", "Rows", "evidence_kind"),
            MarkdownCountTableSpec("Public Event DataMapping Evidence Summary public event link statuses", "Public event link status", "Rows", "public_event_link_status"),
            MarkdownCountTableSpec("Public Event DataMapping Evidence Summary match statuses", "Match status", "Rows", "match_status"),
            MarkdownCountTableSpec("Public Event DataMapping Evidence Summary evidence statuses", "Evidence status", "Rows", "evidence_status"),
        ),
    ),
    MarkdownSummarySectionSpec(
        heading="## Public Event Auxiliary Evidence Summary",
        csv_name=PUBLIC_EVENT_AUXILIARY_EVIDENCE_CSV_NAME,
        total_label="Total public-event auxiliary evidence rows",
        tables=(
            MarkdownCountTableSpec("Public Event Auxiliary Evidence Summary evidence kinds", "Evidence kind", "Rows", "evidence_kind"),
            MarkdownCountTableSpec("Public Event Auxiliary Evidence Summary public event link statuses", "Public event link status", "Rows", "public_event_link_status"),
            MarkdownCountTableSpec("Public Event Auxiliary Evidence Summary evidence statuses", "Evidence status", "Rows", "evidence_status"),
        ),
    ),
    MarkdownSummarySectionSpec(
        heading="## Challenge DataMapping Evidence Summary",
        csv_name=CHALLENGE_EVIDENCE_CSV_NAME,
        total_label="Total DataMapping challenge evidence rows",
        tables=(
            MarkdownCountTableSpec("Challenge DataMapping Evidence Summary evidence kinds", "Evidence kind", "Rows", "evidence_kind"),
            MarkdownCountTableSpec("Challenge DataMapping Evidence Summary challenge link statuses", "Challenge link status", "Rows", "challenge_link_status"),
            MarkdownCountTableSpec("Challenge DataMapping Evidence Summary match statuses", "Match status", "Rows", "match_status"),
            MarkdownCountTableSpec("Challenge DataMapping Evidence Summary evidence statuses", "Evidence status", "Rows", "evidence_status"),
        ),
    ),
    MarkdownSummarySectionSpec(
        heading="## Path Mission DataMapping Evidence Summary",
        csv_name=PATH_MISSION_EVIDENCE_CSV_NAME,
        total_label="Total DataMapping path mission evidence rows",
        tables=(
            MarkdownCountTableSpec("Path Mission DataMapping Evidence Summary evidence kinds", "Evidence kind", "Rows", "evidence_kind"),
            MarkdownCountTableSpec("Path Mission DataMapping Evidence Summary path mission link statuses", "Path mission link status", "Rows", "path_mission_link_status"),
            MarkdownCountTableSpec("Path Mission DataMapping Evidence Summary path episode link statuses", "Path episode link status", "Rows", "path_episode_link_status"),
            MarkdownCountTableSpec("Path Mission DataMapping Evidence Summary evidence statuses", "Evidence status", "Rows", "evidence_status"),
        ),
    ),
    MarkdownSummarySectionSpec(
        heading="## Contract DataMapping Evidence Summary",
        csv_name=CONTRACT_EVIDENCE_CSV_NAME,
        total_label="Total DataMapping contract evidence rows",
        tables=(
            MarkdownCountTableSpec("Contract DataMapping Evidence Summary evidence kinds", "Evidence kind", "Rows", "evidence_kind"),
            MarkdownCountTableSpec("Contract DataMapping Evidence Summary quest link statuses", "Quest link status", "Rows", "quest_link_status"),
            MarkdownCountTableSpec("Contract DataMapping Evidence Summary match statuses", "Match status", "Rows", "match_status"),
            MarkdownCountTableSpec("Contract DataMapping Evidence Summary evidence statuses", "Evidence status", "Rows", "evidence_status"),
        ),
    ),
    MarkdownSummarySectionSpec(
        heading="## Instance Portal Evidence Summary",
        csv_name=INSTANCE_PORTAL_EVIDENCE_CSV_NAME,
        total_label="Total instance portal evidence rows",
        tables=(
            MarkdownCountTableSpec("Instance Portal Evidence Summary evidence kinds", "Evidence kind", "Rows", "evidence_kind"),
            MarkdownCountTableSpec("Instance Portal Evidence Summary portal link statuses", "Portal link status", "Rows", "portal_link_status"),
            MarkdownCountTableSpec("Instance Portal Evidence Summary DataMapping bridge statuses", "DataMapping bridge status", "Rows", "datamapping_bridge_status"),
            MarkdownCountTableSpec("Instance Portal Evidence Summary evidence statuses", "Evidence status", "Rows", "evidence_status"),
        ),
    ),
    MarkdownSummarySectionSpec(
        heading="## Script Presentation Evidence Summary",
        csv_name=SCRIPT_PRESENTATION_EVIDENCE_CSV_NAME,
        total_label="Total script communicator/cinematic evidence rows",
        tables=(
            MarkdownCountTableSpec("Script Presentation Evidence Summary evidence kinds", "Evidence kind", "Rows", "evidence_kind"),
            MarkdownCountTableSpec("Script Presentation Evidence Summary script scopes", "Script scope", "Rows", "script_scope"),
            MarkdownCountTableSpec("Script Presentation Evidence Summary client/runtime link statuses", "Client/runtime link status", "Rows", "client_link_status"),
            MarkdownCountTableSpec(
                "Script Presentation Evidence Summary runtime payload statuses",
                "Runtime payload status",
                "Rows",
                "runtime_payload_status",
                allow_blank_name=True,
            ),
            MarkdownCountTableSpec("Script Presentation Evidence Summary evidence statuses", "Evidence status", "Rows", "evidence_status"),
        ),
    ),
    MarkdownSummarySectionSpec(
        heading="## Script Public Event Flow Evidence Summary",
        csv_name=SCRIPT_EVENT_FLOW_EVIDENCE_CSV_NAME,
        total_label="Total script public-event flow evidence rows",
        tables=(
            MarkdownCountTableSpec("Script Public Event Flow Evidence Summary evidence kinds", "Evidence kind", "Rows", "evidence_kind"),
            MarkdownCountTableSpec("Script Public Event Flow Evidence Summary script scopes", "Script scope", "Rows", "script_scope"),
            MarkdownCountTableSpec("Script Public Event Flow Evidence Summary reference link statuses", "Reference link status", "Rows", "reference_link_status"),
            MarkdownCountTableSpec("Script Public Event Flow Evidence Summary owner link statuses", "Owner link status", "Rows", "owner_link_status"),
            MarkdownCountTableSpec("Script Public Event Flow Evidence Summary evidence statuses", "Evidence status", "Rows", "evidence_status"),
        ),
    ),
    MarkdownSummarySectionSpec(
        heading="## Dungeon/Raid/Instance Summary",
        csv_name=INSTANCE_CSV_NAME,
        total_label="Total tracked world/script rows",
        tables=(
            MarkdownCountTableSpec("Dungeon/Raid/Instance Summary content types", "Content type", "Rows", "content_type"),
            MarkdownCountTableSpec("Dungeon/Raid/Instance Summary milestone scopes", "Milestone scope", "Rows", "milestone_scope"),
            MarkdownCountTableSpec("Dungeon/Raid/Instance Summary evidence statuses", "Evidence status", "Rows", "evidence_status"),
        ),
    ),
    MarkdownSummarySectionSpec(
        heading="## Instance Dependency Summary",
        csv_name=INSTANCE_DEPENDENCY_CSV_NAME,
        total_label="Total dependency rows",
        tables=(
            MarkdownCountTableSpec("Instance Dependency Summary dependency types", "Dependency type", "Rows", "dependency_type"),
        ),
    ),
    MarkdownSummarySectionSpec(
        heading="## Instance Entity/Encounter Dependency Summary",
        csv_name=INSTANCE_ENTITY_CSV_NAME,
        total_label="Total entity/encounter rows",
        tables=(
            MarkdownCountTableSpec("Instance Entity/Encounter Dependency Summary dependency types", "Dependency type", "Rows", "dependency_type"),
            MarkdownCountTableSpec("Instance Entity/Encounter Dependency Summary evidence statuses", "Evidence status", "Rows", "evidence_status"),
        ),
    ),
    MarkdownSummarySectionSpec(
        heading="## Instance Reward Dependency Summary",
        csv_name=INSTANCE_REWARD_CSV_NAME,
        total_label="Total reward rows",
        tables=(
            MarkdownCountTableSpec("Instance Reward Dependency Summary reward sources", "Reward source", "Rows", "reward_source"),
            MarkdownCountTableSpec("Instance Reward Dependency Summary evidence statuses", "Evidence status", "Rows", "evidence_status"),
        ),
    ),
    MarkdownSummarySectionSpec(
        heading="## Script Objective Producer Evidence Summary",
        csv_name=SCRIPT_OBJECTIVE_PRODUCER_EVIDENCE_CSV_NAME,
        total_label="Total script objective producer evidence rows",
        tables=(
            MarkdownCountTableSpec("Script Objective Producer Evidence Summary evidence kinds", "Evidence kind", "Rows", "evidence_kind"),
            MarkdownCountTableSpec("Script Objective Producer Evidence Summary script scopes", "Script scope", "Rows", "script_scope"),
            MarkdownCountTableSpec("Script Objective Producer Evidence Summary producer targeting", "Producer targeting", "Rows", "producer_targeting"),
            MarkdownCountTableSpec("Script Objective Producer Evidence Summary reference link statuses", "Reference link status", "Rows", "reference_link_status"),
            MarkdownCountTableSpec("Script Objective Producer Evidence Summary owner link statuses", "Owner link status", "Rows", "owner_link_status"),
            MarkdownCountTableSpec("Script Objective Producer Evidence Summary evidence statuses", "Evidence status", "Rows", "evidence_status"),
        ),
        composite_tables=(
            MarkdownCompositeCountTableSpec(
                "Script Objective Producer Evidence Summary scope/objective types",
                ("Script scope", "Client objective type"),
                "Rows",
                ("script_scope", "objective_type_name"),
            ),
        ),
    ),
    MarkdownSummarySectionSpec(
        heading="## Script Runtime Action Evidence Summary",
        csv_name=SCRIPT_RUNTIME_ACTION_EVIDENCE_CSV_NAME,
        total_label="Total script runtime action evidence rows",
        tables=(
            MarkdownCountTableSpec("Script Runtime Action Evidence Summary action categories", "Action category", "Rows", "action_category"),
            MarkdownCountTableSpec("Script Runtime Action Evidence Summary script scopes", "Script scope", "Rows", "script_scope"),
            MarkdownCountTableSpec("Script Runtime Action Evidence Summary reference link statuses", "Reference link status", "Rows", "reference_link_status"),
            MarkdownCountTableSpec("Script Runtime Action Evidence Summary client reference statuses", "Client reference status", "Rows", "client_reference_status"),
            MarkdownCountTableSpec("Script Runtime Action Evidence Summary source trace statuses", "Source trace status", "Rows", "source_trace_status"),
            MarkdownCountTableSpec("Script Runtime Action Evidence Summary evidence statuses", "Evidence status", "Rows", "evidence_status"),
        ),
    ),
    MarkdownSummarySectionSpec(
        heading="## Script Quest Progression Evidence Summary",
        csv_name=SCRIPT_QUEST_PROGRESSION_EVIDENCE_CSV_NAME,
        total_label="Total Script.Main quest progression evidence rows",
        tables=(
            MarkdownCountTableSpec("Script Quest Progression Evidence Summary evidence kinds", "Evidence kind", "Rows", "evidence_kind"),
            MarkdownCountTableSpec("Script Quest Progression Evidence Summary progression targeting", "Progression targeting", "Rows", "progression_targeting"),
            MarkdownCountTableSpec("Script Quest Progression Evidence Summary reference link statuses", "Reference link status", "Rows", "reference_link_status"),
            MarkdownCountTableSpec("Script Quest Progression Evidence Summary owner link statuses", "Owner link status", "Rows", "owner_link_status"),
            MarkdownCountTableSpec("Script Quest Progression Evidence Summary source trace statuses", "Source trace status", "Rows", "source_trace_status"),
            MarkdownCountTableSpec("Script Quest Progression Evidence Summary evidence statuses", "Evidence status", "Rows", "evidence_status"),
        ),
    ),
    MarkdownSummarySectionSpec(
        heading="## Instance Script Handler Evidence Summary",
        csv_name=INSTANCE_SCRIPT_HANDLER_EVIDENCE_CSV_NAME,
        total_label="Total instance script handler evidence rows",
        tables=(
            MarkdownCountTableSpec("Instance Script Handler Evidence Summary content types", "Content type", "Rows", "content_type"),
            MarkdownCountTableSpec("Instance Script Handler Evidence Summary script kinds", "Script kind", "Rows", "script_kind"),
            MarkdownCountTableSpec("Instance Script Handler Evidence Summary handler categories", "Handler category", "Rows", "handler_category"),
            MarkdownCountTableSpec("Instance Script Handler Evidence Summary owner link statuses", "Owner link status", "Rows", "owner_link_status"),
            MarkdownCountTableSpec("Instance Script Handler Evidence Summary evidence statuses", "Evidence status", "Rows", "evidence_status"),
        ),
    ),
)


@dataclass(frozen=True)
class ValidationSummary:
    file_count: int
    row_count: int
    completion_counts: Counter[str]


def load_csv(path: Path) -> tuple[list[str], list[dict[str, str]]]:
    with path.open("r", encoding="utf-8-sig", newline="") as handle:
        reader = csv.DictReader(handle)
        return list(reader.fieldnames or []), list(reader)


def format_named_counts(counter: Counter[str]) -> str:
    return ";".join(f"{name}={counter[name]}" for name in sorted(counter) if name)


def format_text_sample(values: Sequence[str], limit: int = 8, separator: str = " || ") -> str:
    seen: set[str] = set()
    result: list[str] = []
    for value in values:
        text = str(value)
        if text in {"", "0"} or text in seen:
            continue
        seen.add(text)
        result.append(text)

    if len(result) <= limit:
        return separator.join(result)
    return separator.join(result[:limit] + [f"...+{len(result) - limit}"])


def parse_producer_coverage(value: str) -> tuple[int, int] | None:
    text = (value or "").strip()
    if not text or "/" not in text:
        return None
    covered_text, total_text = text.split("/", 1)
    try:
        covered = int(covered_text)
        total = int(total_text)
    except ValueError:
        return None
    if total <= 0:
        return None
    return covered, total


def primary_blocker_category(category_counts: Counter[str]) -> str:
    if not category_counts:
        return ""
    return sorted(category_counts.items(), key=lambda item: (-item[1], item[0]))[0][0]


def blocking_evidence_source_for_category(category: str) -> str:
    if not category:
        return "client_smoke"
    if category.endswith("_evidence") or category in {"quest_creature", "instance_entity", "instance_portal_evidence"}:
        return "datamapping_review"
    if category.startswith("script_") or category in {"quest_script", "instance_script_handler"}:
        return "runtime_test"
    if any(token in category for token in ("reward", "loot", "achievement")):
        return "client_smoke"
    if category in {"instance_dependency", "quest_world_dependency", "quest_objective", "quest_prerequisite"}:
        return "client_smoke"
    return "client_smoke"


def expected_queue_actionability_fields(
    queue_row: dict[str, str],
    blocker_count: int,
    category_counts: Counter[str],
) -> dict[str, str]:
    split_reasons: list[str] = []
    if blocker_count > QUEUE_SPLIT_BLOCKER_DETAIL_THRESHOLD:
        split_reasons.append("blocker_count_gt_100")

    coverage = parse_producer_coverage(queue_row.get("scripted_producer_coverage", ""))
    producer_gap = False
    if coverage is not None:
        covered, total = coverage
        if covered * 100 < total * QUEUE_SPLIT_PRODUCER_COVERAGE_PERCENT:
            producer_gap = True
            split_reasons.append("producer_coverage_lt_50pct")

    implementation_surface = queue_row.get("owner_paths", "").strip()
    split_required = bool(split_reasons)
    if split_required:
        actionability_state = "split_required_producer_gap" if producer_gap else "split_required_high_blocker_volume"
    elif queue_row.get("lane", "").strip() == "quest" and queue_row.get("confidence_level", "").strip() == "mapped_scripted_row_pending_world_smoke":
        actionability_state = "quest_mapping_then_smoke"
    elif queue_row.get("lane", "").strip() == "quest":
        actionability_state = "quest_validation_ready"
    elif queue_row.get("lane", "").strip() == "instance":
        actionability_state = "instance_validation_ready"
    elif implementation_surface:
        actionability_state = "validation_ready"
    else:
        actionability_state = "mapped_only"

    primary_category = primary_blocker_category(category_counts)
    return {
        "actionability_state": actionability_state,
        "split_required": "true" if split_required else "false",
        "split_reason": ";".join(split_reasons),
        "primary_blocker_category": primary_category,
        "implementation_surface": implementation_surface,
        "retail_claim_allowed": "false",
        "required_evidence_level": "Mapped" if producer_gap else "Verified",
        "blocking_evidence_source": blocking_evidence_source_for_category(primary_category),
    }


def is_path_like_evidence_source(source: str) -> bool:
    normalised = source.replace("\\", "/")
    return "/" in normalised or "*" in normalised or "?" in normalised or "[" in normalised


def evidence_source_exists(root_path: Path, source: str) -> bool:
    normalised = source.replace("\\", "/")
    if any(character in normalised for character in "*?["):
        return any(root_path.glob(normalised))

    candidate = Path(normalised)
    if not candidate.is_absolute():
        candidate = root_path / candidate

    return candidate.exists()


EvidenceSourceCache = dict[tuple[Path, str], bool]
SourceRowMatchCache = dict[tuple[str, tuple[tuple[str, str], ...]], int]
SourceRowIndexCache = dict[tuple[str, tuple[str, ...]], Counter[tuple[str, ...]]]


def validate_evidence_sources(
    path: Path,
    row_number: int,
    sources: str,
    root_path: Path,
    evidence_source_cache: EvidenceSourceCache | None = None,
) -> list[str]:
    errors: list[str] = []
    for source in [value.strip() for value in sources.split(EVIDENCE_SOURCE_SEPARATOR)]:
        if not source or not is_path_like_evidence_source(source):
            continue
        cache_key = (root_path, source)
        if evidence_source_cache is not None:
            exists = evidence_source_cache.get(cache_key)
            if exists is None:
                exists = evidence_source_exists(root_path, source)
                evidence_source_cache[cache_key] = exists
        else:
            exists = evidence_source_exists(root_path, source)
        if not exists:
            errors.append(f"{path.name}:{row_number}: evidence source `{source}` does not exist")
    return errors


def validate_next_slice_queue(path: Path, fieldnames: list[str], rows: list[dict[str, str]]) -> list[str]:
    errors: list[str] = []
    missing_columns = [column for column in QUEUE_REQUIRED_COLUMNS if column not in fieldnames]
    if missing_columns:
        return [f"{path.name}: missing queue columns: {', '.join(missing_columns)}"]

    ranks: list[int] = []
    slice_ids: set[str] = set()
    source_inventory_names = set(EXPECTED_CSV_NAMES) - {NEXT_SLICE_QUEUE_CSV_NAME, NEXT_SLICE_BLOCKER_CSV_NAME}
    for row_number, row in enumerate(rows, start=2):
        rank_text = row.get("queue_rank", "").strip()
        try:
            rank = int(rank_text)
            ranks.append(rank)
        except ValueError:
            errors.append(f"{path.name}:{row_number}: queue_rank `{rank_text}` is not an integer")

        slice_id = row.get("slice_id", "").strip()
        if not slice_id:
            errors.append(f"{path.name}:{row_number}: blank slice_id")
        elif slice_id in slice_ids:
            errors.append(f"{path.name}:{row_number}: duplicate slice_id `{slice_id}`")
        slice_ids.add(slice_id)

        priority = row.get("priority", "").strip()
        if priority not in ALLOWED_QUEUE_PRIORITIES:
            errors.append(f"{path.name}:{row_number}: unsupported priority `{priority}`")

        lane = row.get("lane", "").strip()
        if lane not in ALLOWED_QUEUE_LANES:
            errors.append(f"{path.name}:{row_number}: unsupported lane `{lane}`")

        actionability_state = row.get("actionability_state", "").strip()
        if actionability_state not in ALLOWED_QUEUE_ACTIONABILITY_STATES:
            errors.append(f"{path.name}:{row_number}: unsupported actionability_state `{actionability_state}`")

        split_required = row.get("split_required", "").strip()
        if split_required not in ALLOWED_BOOLEAN_TEXT:
            errors.append(f"{path.name}:{row_number}: split_required `{split_required}` must be true or false")

        if split_required == "true" and not row.get("split_reason", "").strip():
            errors.append(f"{path.name}:{row_number}: split_required true needs split_reason")
        if split_required == "false" and row.get("split_reason", "").strip():
            errors.append(f"{path.name}:{row_number}: split_required false must have blank split_reason")
        if split_required == "true" and not actionability_state.startswith("split_required_"):
            errors.append(f"{path.name}:{row_number}: split_required true must use a split_required actionability_state")

        retail_claim_allowed = row.get("retail_claim_allowed", "").strip()
        if retail_claim_allowed != "false":
            errors.append(f"{path.name}:{row_number}: retail_claim_allowed must stay false")

        required_evidence_level = row.get("required_evidence_level", "").strip()
        if required_evidence_level not in ALLOWED_REQUIRED_EVIDENCE_LEVELS:
            errors.append(f"{path.name}:{row_number}: unsupported required_evidence_level `{required_evidence_level}`")

        blocking_evidence_source = row.get("blocking_evidence_source", "").strip()
        if blocking_evidence_source not in ALLOWED_BLOCKING_EVIDENCE_SOURCES:
            errors.append(f"{path.name}:{row_number}: unsupported blocking_evidence_source `{blocking_evidence_source}`")

        source_inventory = row.get("source_inventory", "").strip()
        if not source_inventory:
            errors.append(f"{path.name}:{row_number}: blank source_inventory")
        elif source_inventory not in source_inventory_names:
            errors.append(f"{path.name}:{row_number}: source_inventory `{source_inventory}` is not a generated source inventory")

        if not row.get("source_row_key", "").strip():
            errors.append(f"{path.name}:{row_number}: blank source_row_key")

        validation_bundle_name = row.get("validation_bundle_name", "").strip()
        narrow_test_filter = row.get("narrow_test_filter", "").strip()
        narrow_test_command = row.get("narrow_test_command", "").strip()
        harness_command = row.get("harness_command", "").strip()
        for column, value in (
            ("validation_bundle_name", validation_bundle_name),
            ("narrow_test_filter", narrow_test_filter),
            ("narrow_test_command", narrow_test_command),
            ("harness_command", harness_command),
        ):
            if not value:
                errors.append(f"{path.name}:{row_number}: blank queue command column `{column}`")

        if narrow_test_filter and narrow_test_command and narrow_test_filter not in narrow_test_command:
            errors.append(f"{path.name}:{row_number}: narrow_test_command does not include narrow_test_filter")
        if validation_bundle_name and harness_command and validation_bundle_name not in harness_command:
            errors.append(f"{path.name}:{row_number}: harness_command does not include validation_bundle_name")
        if harness_command and "Start-BlockerEvidenceHarness.ps1" not in harness_command:
            errors.append(f"{path.name}:{row_number}: harness_command does not invoke Start-BlockerEvidenceHarness.ps1")

    if ranks and sorted(ranks) != list(range(1, len(rows) + 1)):
        errors.append(f"{path.name}: queue_rank values must be unique and contiguous from 1")

    return errors


def validate_next_slice_blockers(path: Path, fieldnames: list[str], rows: list[dict[str, str]]) -> list[str]:
    errors: list[str] = []
    missing_columns = [column for column in BLOCKER_REQUIRED_COLUMNS if column not in fieldnames]
    if missing_columns:
        return [f"{path.name}: missing blocker columns: {', '.join(missing_columns)}"]

    blocker_ids: set[str] = set()
    source_inventory_names = set(EXPECTED_CSV_NAMES) - {NEXT_SLICE_QUEUE_CSV_NAME, NEXT_SLICE_BLOCKER_CSV_NAME}
    for row_number, row in enumerate(rows, start=2):
        blocker_id = row.get("blocker_id", "").strip()
        if not blocker_id:
            errors.append(f"{path.name}:{row_number}: blank blocker_id")
        elif blocker_id in blocker_ids:
            errors.append(f"{path.name}:{row_number}: duplicate blocker_id `{blocker_id}`")
        blocker_ids.add(blocker_id)

        for column in ("queue_rank", "blocker_rank"):
            value = row.get(column, "").strip()
            try:
                int(value)
            except ValueError:
                errors.append(f"{path.name}:{row_number}: {column} `{value}` is not an integer")

        lane = row.get("lane", "").strip()
        if lane not in ALLOWED_QUEUE_LANES:
            errors.append(f"{path.name}:{row_number}: unsupported lane `{lane}`")

        priority = row.get("priority", "").strip()
        if priority not in ALLOWED_QUEUE_PRIORITIES:
            errors.append(f"{path.name}:{row_number}: unsupported priority `{priority}`")

        source_inventory = row.get("source_inventory", "").strip()
        if not source_inventory:
            errors.append(f"{path.name}:{row_number}: blank source_inventory")
        elif source_inventory not in source_inventory_names:
            errors.append(f"{path.name}:{row_number}: source_inventory `{source_inventory}` is not a generated blocker source inventory")

        blocker_category = row.get("blocker_category", "").strip()
        expected_source_inventory = BLOCKER_CATEGORY_SOURCE_INVENTORIES.get(blocker_category)
        if blocker_category and expected_source_inventory is None:
            errors.append(f"{path.name}:{row_number}: unsupported blocker_category `{blocker_category}`")
        elif expected_source_inventory and source_inventory and source_inventory != expected_source_inventory:
            errors.append(
                f"{path.name}:{row_number}: blocker_category `{blocker_category}` must use source_inventory "
                f"`{expected_source_inventory}`, not `{source_inventory}`"
            )

        for column in ("slice_id", "blocker_category", "blocker_type", "source_row_key", "next_action", "tracker_paths"):
            if not row.get(column, "").strip():
                errors.append(f"{path.name}:{row_number}: blank blocker column `{column}`")

    return errors


def validate_csv_rows(
    path: Path,
    fieldnames: list[str],
    rows: list[dict[str, str]],
    root_path: Path = ROOT,
    evidence_source_cache: EvidenceSourceCache | None = None,
) -> tuple[list[str], int, Counter[str]]:
    errors: list[str] = []
    missing_columns = [column for column in REQUIRED_COLUMNS if column not in fieldnames]
    if missing_columns:
        errors.append(f"{path.name}: missing required columns: {', '.join(missing_columns)}")
        return errors, len(rows), Counter()

    completion_counts: Counter[str] = Counter()
    for row_number, row in enumerate(rows, start=2):
        for column in REQUIRED_COLUMNS:
            if not csv_cell(row, column).strip():
                errors.append(f"{path.name}:{row_number}: blank required column `{column}`")

        errors.extend(
            validate_evidence_sources(
                path,
                row_number,
                csv_cell(row, "evidence_sources"),
                root_path,
                evidence_source_cache,
            )
        )

        completion_status = csv_cell(row, "completion_status").strip()
        completion_counts[completion_status] += 1
        if completion_status not in ALLOWED_COMPLETION_STATUSES:
            errors.append(f"{path.name}:{row_number}: unsupported completion_status `{completion_status}`")
            continue

        if completion_status != "retail_complete":
            continue

        manual_validation = csv_cell(row, "manual_validation").strip().lower()
        evidence_status = csv_cell(row, "evidence_status").strip().lower()
        blocker_summary = csv_cell(row, "blocker_summary").strip().lower()

        if manual_validation in PENDING_MANUAL_VALUES:
            errors.append(f"{path.name}:{row_number}: retail_complete row still has pending manual_validation")
        if evidence_status not in RETAIL_COMPLETE_EVIDENCE_STATUSES:
            errors.append(f"{path.name}:{row_number}: retail_complete row lacks terminal retail-complete evidence_status")
        if any(marker in evidence_status for marker in OPEN_EVIDENCE_STATUS_MARKERS):
            errors.append(f"{path.name}:{row_number}: retail_complete row still has an open evidence_status")
        if any(marker in blocker_summary for marker in OPEN_BLOCKER_MARKERS):
            errors.append(f"{path.name}:{row_number}: retail_complete row still has an open blocker_summary")

    if path.name == NEXT_SLICE_QUEUE_CSV_NAME:
        errors.extend(validate_next_slice_queue(path, fieldnames, rows))
    if path.name == NEXT_SLICE_BLOCKER_CSV_NAME:
        errors.extend(validate_next_slice_blockers(path, fieldnames, rows))

    return errors, len(rows), completion_counts


def validate_csv(path: Path, root_path: Path = ROOT) -> tuple[list[str], int, Counter[str]]:
    fieldnames, rows = load_csv(path)
    return validate_csv_rows(path, fieldnames, rows, root_path, {})


def csv_cell(row: dict[str, str], column: str) -> str:
    value = row.get(column, "")
    if value is None:
        return ""

    return str(value)


def validate_tracker_links(csv_paths: list[Path], tracker_path: Path) -> list[str]:
    if not tracker_path.exists():
        return [f"Missing tracker: {tracker_path}"]

    tracker_text = tracker_path.read_text(encoding="utf-8", errors="replace")
    errors: list[str] = []
    for csv_path in csv_paths:
        if csv_path.name not in tracker_text:
            errors.append(f"{tracker_path.name}: missing generated inventory link for {csv_path.name}")
    return errors


def validate_expected_inventory(csv_paths: list[Path], expected_csv_names: Sequence[str]) -> list[str]:
    actual_names = {path.name for path in csv_paths}
    expected_names = set(expected_csv_names)
    errors: list[str] = []

    for missing_name in sorted(expected_names - actual_names):
        errors.append(f"Missing expected content retail-completeness inventory: {missing_name}")
    for unexpected_name in sorted(actual_names - expected_names):
        errors.append(f"Unexpected content retail-completeness inventory: {unexpected_name}")

    return errors


def display_count(value: int) -> str:
    return f"{value:,}"


def contains_repo_path(text: str, root_path: Path, path: Path) -> bool:
    try:
        relative = path.resolve().relative_to(root_path.resolve())
    except ValueError:
        relative = path

    forward = relative.as_posix()
    backward = str(relative)
    return forward in text or backward in text


def contains_count_phrase(text: str, value: int, phrase: str) -> bool:
    formatted = display_count(value)
    return any(
        candidate in text
        for candidate in (
            f"`{formatted}` {phrase}",
            f"{formatted} {phrase}",
            f"`{value}` {phrase}",
            f"{value} {phrase}",
        )
    )


def parse_markdown_row(line: str) -> list[str]:
    stripped = line.strip()
    if not stripped.startswith("|") or not stripped.endswith("|"):
        return []
    return [cell.strip() for cell in stripped.strip("|").split("|")]


def is_markdown_separator_row(cells: Sequence[str]) -> bool:
    return bool(cells) and all(re.fullmatch(r":?-{3,}:?", cell.replace(" ", "")) for cell in cells)


def clean_markdown_cell(value: str) -> str:
    value = value.strip()
    if len(value) >= 2 and value.startswith("`") and value.endswith("`"):
        return value[1:-1].strip()
    return value


def parse_markdown_count(value: str) -> int | None:
    text = clean_markdown_cell(value).replace(",", "")
    match = re.search(r"-?\d+", text)
    if not match:
        return None
    return int(match.group(0))


def parse_markdown_total_count(section_text: str, total_label: str) -> int | None:
    prefix = f"- {total_label}:"
    for line in section_text.splitlines():
        stripped = line.strip()
        if stripped.startswith(prefix):
            return parse_markdown_count(stripped[len(prefix) :])
    return None


def extract_markdown_section(text: str, heading: str) -> str | None:
    lines = text.splitlines()
    start: int | None = None
    for index, line in enumerate(lines):
        if line.strip() == heading:
            start = index + 1
            break
    if start is None:
        return None

    end = len(lines)
    for index in range(start, len(lines)):
        if lines[index].startswith("## "):
            end = index
            break
    return "\n".join(lines[start:end])


def parse_markdown_table(section_text: str, headers: Sequence[str]) -> list[dict[str, str]] | None:
    lines = section_text.splitlines()
    for index, line in enumerate(lines):
        cells = parse_markdown_row(line)
        if cells != list(headers):
            continue
        if index + 1 >= len(lines):
            return []
        separator_cells = parse_markdown_row(lines[index + 1])
        if not is_markdown_separator_row(separator_cells):
            return []

        rows: list[dict[str, str]] = []
        for row_line in lines[index + 2:]:
            row_cells = parse_markdown_row(row_line)
            if not row_cells:
                break
            if len(row_cells) < len(headers):
                break
            rows.append(dict(zip(headers, row_cells[: len(headers)])))
        return rows
    return None


def validate_named_count_table(
    tracker_path: Path,
    table_name: str,
    rows: list[dict[str, str]] | None,
    name_column: str,
    count_column: str,
    expected_counts: Counter[str],
    allow_blank_name: bool = False,
) -> list[str]:
    if rows is None:
        return [f"{tracker_path.name}: missing `{table_name}` table"]

    errors: list[str] = []
    actual_counts: dict[str, int] = {}
    for row_number, row in enumerate(rows, start=1):
        name = clean_markdown_cell(row.get(name_column, ""))
        count = parse_markdown_count(row.get(count_column, ""))
        if not name and not allow_blank_name:
            errors.append(f"{tracker_path.name}: `{table_name}` row {row_number} has blank `{name_column}`")
            continue
        if count is None:
            errors.append(f"{tracker_path.name}: `{table_name}` row `{name}` has non-integer `{count_column}`")
            continue
        actual_counts[name] = count

    for name, expected_count in sorted(expected_counts.items()):
        if name not in actual_counts:
            errors.append(f"{tracker_path.name}: `{table_name}` missing `{name}` count `{expected_count}`")
        elif actual_counts[name] != expected_count:
            errors.append(
                f"{tracker_path.name}: `{table_name}` count for `{name}` is `{actual_counts[name]}`, expected `{expected_count}`"
            )

    for name in sorted(set(actual_counts) - set(expected_counts)):
        errors.append(f"{tracker_path.name}: `{table_name}` has unexpected `{name}` count `{actual_counts[name]}`")

    return errors


def markdown_cell_contains_path(cell: str, expected_path: str) -> bool:
    normalised_cell = cell.replace("\\", "/")
    normalised_expected = expected_path.replace("\\", "/")
    if any(character in normalised_expected for character in "*?[]"):
        return normalised_expected in normalised_cell
    return normalised_expected in normalised_cell or Path(normalised_expected).name in normalised_cell


def validate_subagent_lane_snapshot_source_coverage(tracker_path: Path) -> list[str]:
    source_counts: Counter[str] = Counter()
    errors: list[str] = []
    for lane, expected_paths in SUBAGENT_LANE_SNAPSHOT_PATHS.items():
        for expected_path in expected_paths:
            if expected_path in EXPECTED_CSV_NAMES:
                source_counts[expected_path] += 1
            elif expected_path.startswith("content_retail_completeness_") and expected_path.endswith(".csv"):
                errors.append(
                    f"{tracker_path.name}: `Parallel Subagent Lane Snapshot` lane `{lane}` includes unknown generated CSV `{expected_path}`"
                )

    for csv_name in sorted(set(EXPECTED_CSV_NAMES) - set(source_counts)):
        errors.append(f"{tracker_path.name}: `Parallel Subagent Lane Snapshot` source map omits expected generated CSV `{csv_name}`")

    for csv_name, count in sorted(source_counts.items()):
        if count > 1:
            errors.append(f"{tracker_path.name}: `Parallel Subagent Lane Snapshot` source map includes `{csv_name}` {count} times")

    return errors


def validate_subagent_lane_snapshot_section(tracker_path: Path, text: str) -> list[str]:
    section = extract_markdown_section(text, "## Parallel Subagent Lane Snapshot")
    if section is None:
        return [f"{tracker_path.name}: missing `Parallel Subagent Lane Snapshot` section"]

    rows = parse_markdown_table(section, SUBAGENT_LANE_SNAPSHOT_HEADERS)
    if rows is None:
        return [f"{tracker_path.name}: missing `Parallel Subagent Lane Snapshot` table"]

    errors: list[str] = []
    errors.extend(validate_subagent_lane_snapshot_source_coverage(tracker_path))
    lane_counts = Counter(clean_markdown_cell(row.get("Lane", "")) for row in rows)
    expected_lanes = set(SUBAGENT_LANE_SNAPSHOT_PATHS)
    for lane in sorted(expected_lanes - set(lane_counts)):
        errors.append(f"{tracker_path.name}: `Parallel Subagent Lane Snapshot` missing lane `{lane}`")
    for lane in sorted(set(lane_counts) - expected_lanes):
        errors.append(f"{tracker_path.name}: `Parallel Subagent Lane Snapshot` has unexpected lane `{lane}`")
    for lane, count in sorted(lane_counts.items()):
        if lane in expected_lanes and count > 1:
            errors.append(f"{tracker_path.name}: `Parallel Subagent Lane Snapshot` lane `{lane}` appears {count} times")

    rows_by_lane = {clean_markdown_cell(row.get("Lane", "")): row for row in rows}
    for lane, expected_paths in SUBAGENT_LANE_SNAPSHOT_PATHS.items():
        row = rows_by_lane.get(lane)
        if row is None:
            continue

        evidence_cells = f"{row.get('IDs and tracker paths', '')} {row.get('Evidence sources', '')}"
        for expected_path in expected_paths:
            if not markdown_cell_contains_path(evidence_cells, expected_path):
                errors.append(
                    f"{tracker_path.name}: `Parallel Subagent Lane Snapshot` row `{lane}` missing expected tracker/evidence path `{expected_path}`"
                )

        for column in SUBAGENT_LANE_SNAPSHOT_REQUIRED_TEXT_COLUMNS:
            value = clean_markdown_cell(row.get(column, "")).strip().lower()
            if value in SUBAGENT_LANE_SNAPSHOT_PLACEHOLDERS:
                errors.append(f"{tracker_path.name}: `Parallel Subagent Lane Snapshot` row `{lane}` has placeholder `{column}`")

    return errors


def validate_subagent_findings_inventory(
    path: Path,
    root_path: Path,
    evidence_source_cache: EvidenceSourceCache | None = None,
) -> tuple[list[dict[str, str]], list[str]]:
    if not path.exists():
        return [], [f"Missing focused subagent findings inventory: {path}"]

    fieldnames, rows = load_csv(path)
    errors: list[str] = []
    missing_columns = [column for column in SUBAGENT_FINDINGS_REQUIRED_COLUMNS if column not in fieldnames]
    if missing_columns:
        return rows, [f"{path.name}: missing focused subagent findings columns: {', '.join(missing_columns)}"]

    finding_ids: set[str] = set()
    for row_number, row in enumerate(rows, start=2):
        for column in SUBAGENT_FINDINGS_REQUIRED_COLUMNS:
            if not row.get(column, "").strip():
                errors.append(f"{path.name}:{row_number}: blank focused subagent finding column `{column}`")

        finding_id = row.get("finding_id", "").strip()
        if finding_id in finding_ids:
            errors.append(f"{path.name}:{row_number}: duplicate finding_id `{finding_id}`")
        finding_ids.add(finding_id)

        lane = row.get("lane", "").strip()
        if lane and lane not in SUBAGENT_LANE_SNAPSHOT_PATHS:
            errors.append(f"{path.name}:{row_number}: unknown focused subagent lane `{lane}`")

        priority = row.get("priority", "").strip()
        if priority and priority not in ALLOWED_QUEUE_PRIORITIES:
            errors.append(f"{path.name}:{row_number}: unsupported focused subagent priority `{priority}`")

        status = row.get("status", "").strip()
        if status and status not in ALLOWED_SUBAGENT_FINDING_STATUSES:
            errors.append(f"{path.name}:{row_number}: unsupported focused subagent status `{status}`")

        source_inventory = row.get("source_inventory", "").strip()
        if source_inventory.startswith("content_retail_completeness_") and source_inventory.endswith(".csv"):
            if source_inventory not in EXPECTED_CSV_NAMES:
                errors.append(f"{path.name}:{row_number}: source_inventory `{source_inventory}` is not an expected generated CSV")

        errors.extend(
            validate_evidence_sources(
                path,
                row_number,
                row.get("tracker_paths", ""),
                root_path,
                evidence_source_cache,
            )
        )
        errors.extend(
            validate_evidence_sources(
                path,
                row_number,
                row.get("evidence_sources", ""),
                root_path,
                evidence_source_cache,
            )
        )

        for column in ("confidence", "blocker_summary", "next_action"):
            value = row.get(column, "").strip().lower()
            if value in SUBAGENT_FINDING_PLACEHOLDERS:
                errors.append(f"{path.name}:{row_number}: placeholder focused subagent finding `{column}`")

    return rows, errors


def validate_subagent_findings_section(
    tracker_path: Path,
    text: str,
    rows: list[dict[str, str]],
    inventory_path: Path,
    root_path: Path,
) -> list[str]:
    section = extract_markdown_section(text, "## Focused Subagent Findings Inventory")
    if section is None:
        return [f"{tracker_path.name}: missing `Focused Subagent Findings Inventory` section"]

    errors: list[str] = []
    if not contains_repo_path(section, root_path, inventory_path):
        errors.append(f"{tracker_path.name}: `Focused Subagent Findings Inventory` missing source inventory path `{inventory_path}`")

    total = parse_markdown_total_count(section, "Total focused subagent finding rows")
    if total is None:
        errors.append(f"{tracker_path.name}: `Focused Subagent Findings Inventory` missing total focused subagent finding rows")
    elif total != len(rows):
        errors.append(
            f"{tracker_path.name}: `Focused Subagent Findings Inventory` total focused subagent finding rows is `{total}`, expected `{len(rows)}`"
        )

    lane_rows = parse_markdown_table(section, ("Lane", "Rows"))
    lane_counts = Counter(row.get("lane", "") for row in rows)
    errors.extend(
        validate_named_count_table(
            tracker_path,
            "Focused Subagent Findings Inventory lanes",
            lane_rows,
            "Lane",
            "Rows",
            lane_counts,
        )
    )

    status_rows = parse_markdown_table(section, ("Status", "Rows"))
    status_counts = Counter(row.get("status", "") for row in rows)
    errors.extend(
        validate_named_count_table(
            tracker_path,
            "Focused Subagent Findings Inventory statuses",
            status_rows,
            "Status",
            "Rows",
            status_counts,
        )
    )

    finding_rows = parse_markdown_table(section, SUBAGENT_FINDINGS_SECTION_HEADERS)
    if finding_rows is None:
        errors.append(f"{tracker_path.name}: missing `Focused Subagent Findings Inventory` detail table")
    else:
        actual_findings = {clean_markdown_cell(row.get("Finding", "")): row for row in finding_rows}
        for source_row in rows:
            finding_id = source_row.get("finding_id", "")
            if finding_id not in actual_findings:
                errors.append(f"{tracker_path.name}: `Focused Subagent Findings Inventory` missing finding `{finding_id}`")
                continue
            actual = actual_findings[finding_id]
            for column, source_column in (
                ("Priority", "priority"),
                ("Lane", "lane"),
                ("Confidence", "confidence"),
                ("Next action", "next_action"),
            ):
                value = clean_markdown_cell(actual.get(column, "")).strip()
                expected = source_row.get(source_column, "").strip()
                if value != expected:
                    errors.append(
                        f"{tracker_path.name}: `Focused Subagent Findings Inventory` finding `{finding_id}` column `{column}` is `{value}`, expected `{expected}`"
                    )

    return errors


def validate_top_gap_rows(
    tracker_path: Path,
    rows: list[dict[str, str]] | None,
    expected_paths_by_area: dict[str, tuple[str, ...]],
) -> list[str]:
    if rows is None:
        return []

    errors: list[str] = []
    rows_by_area = {clean_markdown_cell(row.get("Area", "")): row for row in rows}
    for area, expected_paths in sorted(expected_paths_by_area.items()):
        row = rows_by_area.get(area)
        if row is None:
            continue

        tracker_paths = row.get("Tracker paths", "")
        for expected_path in expected_paths:
            if not markdown_cell_contains_path(tracker_paths, expected_path):
                errors.append(
                    f"{tracker_path.name}: `Top Gap Rollup` row `{area}` missing expected tracker path `{expected_path}`"
                )

        for column in ("Current evidence/status signal", "Next validation action"):
            value = clean_markdown_cell(row.get(column, "")).strip().lower()
            if value in {"", "status", "next", "todo", "tbd"}:
                errors.append(f"{tracker_path.name}: `Top Gap Rollup` row `{area}` has placeholder `{column}`")

    return errors


def validate_top_gap_source_coverage(tracker_path: Path) -> list[str]:
    expected_sources = set(EXPECTED_CSV_NAMES) - {NEXT_SLICE_BLOCKER_CSV_NAME}
    source_counts: Counter[str] = Counter()
    errors: list[str] = []
    for area, csv_names in TOP_GAP_ROW_SOURCES.items():
        for csv_name in csv_names:
            if csv_name == NEXT_SLICE_BLOCKER_CSV_NAME:
                errors.append(f"{tracker_path.name}: `Top Gap Rollup` row `{area}` includes blocker detail CSV `{csv_name}`")
            elif csv_name not in EXPECTED_CSV_NAMES:
                errors.append(f"{tracker_path.name}: `Top Gap Rollup` row `{area}` includes unknown CSV `{csv_name}`")
            source_counts[csv_name] += 1

    for csv_name in sorted(expected_sources - set(source_counts)):
        errors.append(f"{tracker_path.name}: `Top Gap Rollup` source map omits expected non-blocker CSV `{csv_name}`")

    for csv_name, count in sorted(source_counts.items()):
        if csv_name in expected_sources and count > 1:
            errors.append(f"{tracker_path.name}: `Top Gap Rollup` source map includes `{csv_name}` {count} times")

    return errors


def format_composite_key(values: tuple[str, ...]) -> str:
    return " | ".join(values)


def validate_composite_count_table(
    tracker_path: Path,
    table_name: str,
    rows: list[dict[str, str]] | None,
    name_columns: Sequence[str],
    count_column: str,
    expected_counts: Counter[tuple[str, ...]],
) -> list[str]:
    if rows is None:
        return [f"{tracker_path.name}: missing `{table_name}` table"]

    errors: list[str] = []
    actual_counts: dict[tuple[str, ...], int] = {}
    for row_number, row in enumerate(rows, start=1):
        names = tuple(clean_markdown_cell(row.get(column, "")) for column in name_columns)
        count = parse_markdown_count(row.get(count_column, ""))
        if any(not name for name in names):
            errors.append(f"{tracker_path.name}: `{table_name}` row {row_number} has blank key column")
            continue
        if count is None:
            errors.append(f"{tracker_path.name}: `{table_name}` row `{format_composite_key(names)}` has non-integer `{count_column}`")
            continue
        actual_counts[names] = count

    for names, expected_count in sorted(expected_counts.items()):
        if names not in actual_counts:
            errors.append(f"{tracker_path.name}: `{table_name}` missing `{format_composite_key(names)}` count `{expected_count}`")
        elif actual_counts[names] != expected_count:
            errors.append(
                f"{tracker_path.name}: `{table_name}` count for `{format_composite_key(names)}` is `{actual_counts[names]}`, expected `{expected_count}`"
            )

    for names in sorted(set(actual_counts) - set(expected_counts)):
        errors.append(f"{tracker_path.name}: `{table_name}` has unexpected `{format_composite_key(names)}` count `{actual_counts[names]}`")

    return errors


def validate_domain_summary_sections(
    tracker_path: Path,
    text: str,
    inventory_data: dict[str, tuple[list[str], list[dict[str, str]]]],
) -> list[str]:
    errors: list[str] = []
    for section_spec in DOMAIN_SUMMARY_SECTION_SPECS:
        if section_spec.csv_name not in inventory_data:
            continue

        fieldnames, rows = inventory_data[section_spec.csv_name]
        section = extract_markdown_section(text, section_spec.heading)
        if section is None:
            errors.append(f"{tracker_path.name}: missing `{section_spec.heading.lstrip('# ').strip()}` section")
            continue

        actual_total = parse_markdown_total_count(section, section_spec.total_label)
        expected_total = len(rows)
        if actual_total is None:
            errors.append(f"{tracker_path.name}: `{section_spec.heading}` missing `{section_spec.total_label}` total")
        elif actual_total != expected_total:
            errors.append(
                f"{tracker_path.name}: `{section_spec.heading}` total `{section_spec.total_label}` is `{actual_total}`, expected `{expected_total}`"
            )

        for table_spec in section_spec.tables:
            if table_spec.csv_field not in fieldnames:
                errors.append(
                    f"{section_spec.csv_name}: missing `{table_spec.csv_field}` required by `{section_spec.heading}` markdown summary"
                )
                continue

            expected_counts = Counter(row.get(table_spec.csv_field, "") for row in rows)
            table_rows = parse_markdown_table(section, (table_spec.name_column, table_spec.count_column))
            errors.extend(
                validate_named_count_table(
                    tracker_path,
                    table_spec.table_name,
                    table_rows,
                    table_spec.name_column,
                    table_spec.count_column,
                    expected_counts,
                    table_spec.allow_blank_name,
                )
            )

        for table_spec in section_spec.composite_tables:
            missing_fields = [csv_field for csv_field in table_spec.csv_fields if csv_field not in fieldnames]
            if missing_fields:
                errors.append(
                    f"{section_spec.csv_name}: missing `{', '.join(missing_fields)}` required by `{section_spec.heading}` markdown summary"
                )
                continue

            expected_counts = Counter(
                values
                for row in rows
                for values in (tuple(row.get(csv_field, "") for csv_field in table_spec.csv_fields),)
                if all(values)
            )
            table_rows = parse_markdown_table(section, (*table_spec.name_columns, table_spec.count_column))
            errors.extend(
                validate_composite_count_table(
                    tracker_path,
                    table_spec.table_name,
                    table_rows,
                    table_spec.name_columns,
                    table_spec.count_column,
                    expected_counts,
                )
            )

    return errors


def sort_by_int_field(rows: list[dict[str, str]], field: str) -> list[dict[str, str]]:
    def key(row: dict[str, str]) -> int:
        try:
            return int(row.get(field, ""))
        except ValueError:
            return 0

    return sorted(rows, key=key)


def validate_queue_preview_table(
    tracker_path: Path,
    rows: list[dict[str, str]] | None,
    queue_rows: list[dict[str, str]],
) -> list[str]:
    if rows is None:
        return [f"{tracker_path.name}: missing `Next Restoration Slice Queue preview` table"]

    errors: list[str] = []
    expected_rows = sort_by_int_field(queue_rows, "queue_rank")
    if len(rows) != len(expected_rows):
        errors.append(
            f"{tracker_path.name}: queue detail table has `{len(rows)}` rows, expected `{len(expected_rows)}`"
        )

    for index, expected in enumerate(expected_rows):
        if index >= len(rows):
            break
        actual = rows[index]
        actual_rank = parse_markdown_count(actual.get("Rank", ""))
        actual_lane = clean_markdown_cell(actual.get("Lane", ""))
        actual_score = parse_markdown_count(actual.get("Score", ""))
        actual_blocker_count = parse_markdown_count(actual.get("Blockers", ""))
        actual_actionability = clean_markdown_cell(actual.get("Action", ""))
        actual_content_match = re.search(r"`([^`]+)`", actual.get("Content", ""))
        actual_content_id = actual_content_match.group(1).strip() if actual_content_match else ""

        expected_rank = int(expected.get("queue_rank", "0") or "0")
        expected_score = int(expected.get("priority_score", "0") or "0")
        expected_blocker_count = int(expected.get("blocker_detail_count", "0") or "0")
        if actual_rank != expected_rank:
            errors.append(f"{tracker_path.name}: queue preview row {index + 1} rank `{actual_rank}` expected `{expected_rank}`")
        if actual_lane != expected.get("lane", ""):
            errors.append(f"{tracker_path.name}: queue preview rank `{expected_rank}` lane `{actual_lane}` expected `{expected.get('lane', '')}`")
        if actual_content_id != expected.get("content_id", ""):
            errors.append(
                f"{tracker_path.name}: queue preview rank `{expected_rank}` content id `{actual_content_id}` expected `{expected.get('content_id', '')}`"
            )
        if actual_score != expected_score:
            errors.append(f"{tracker_path.name}: queue preview rank `{expected_rank}` score `{actual_score}` expected `{expected_score}`")
        if actual_blocker_count != expected_blocker_count:
            errors.append(
                f"{tracker_path.name}: queue preview rank `{expected_rank}` blockers `{actual_blocker_count}` expected `{expected_blocker_count}`"
            )
        if actual_actionability != expected.get("actionability_state", ""):
            errors.append(
                f"{tracker_path.name}: queue preview rank `{expected_rank}` action `{actual_actionability}` expected `{expected.get('actionability_state', '')}`"
            )

    return errors


def validate_next_slice_queue_summary_section(
    tracker_path: Path,
    text: str,
    queue_rows: list[dict[str, str]],
    blocker_rows: list[dict[str, str]],
) -> list[str]:
    section = extract_markdown_section(text, "## Next Restoration Slice Queue Summary")
    if section is None:
        return [f"{tracker_path.name}: missing `Next Restoration Slice Queue Summary` section"]

    errors: list[str] = []
    queue_total = parse_markdown_total_count(section, "Total queue rows")
    if queue_total is None:
        errors.append(f"{tracker_path.name}: `Next Restoration Slice Queue Summary` missing `Total queue rows` total")
    elif queue_total != len(queue_rows):
        errors.append(
            f"{tracker_path.name}: `Next Restoration Slice Queue Summary` total `Total queue rows` is `{queue_total}`, expected `{len(queue_rows)}`"
        )

    blocker_total = parse_markdown_total_count(section, "Total queue blocker detail rows")
    if blocker_total is None:
        errors.append(f"{tracker_path.name}: `Next Restoration Slice Queue Summary` missing `Total queue blocker detail rows` total")
    elif blocker_total != len(blocker_rows):
        errors.append(
            f"{tracker_path.name}: `Next Restoration Slice Queue Summary` total `Total queue blocker detail rows` is `{blocker_total}`, expected `{len(blocker_rows)}`"
        )

    lane_rows = parse_markdown_table(section, ("Lane", "Rows"))
    lane_counts = Counter(row.get("lane", "") for row in queue_rows)
    errors.extend(
        validate_named_count_table(
            tracker_path,
            "Next Restoration Slice Queue Summary lanes",
            lane_rows,
            "Lane",
            "Rows",
            lane_counts,
        )
    )

    blocker_category_rows = parse_markdown_table(section, ("Blocker category", "Rows"))
    blocker_category_counts = Counter(row.get("blocker_category", "") for row in blocker_rows)
    errors.extend(
        validate_named_count_table(
            tracker_path,
            "Next Restoration Slice Queue Summary blocker categories",
            blocker_category_rows,
            "Blocker category",
            "Rows",
            blocker_category_counts,
        )
    )

    return errors


def validate_tracker_markdown_rollups(
    tracker_path: Path,
    row_counts_by_name: dict[str, int],
    inventory_data: dict[str, tuple[list[str], list[dict[str, str]]]],
) -> list[str]:
    if NEXT_SLICE_QUEUE_CSV_NAME not in inventory_data or NEXT_SLICE_BLOCKER_CSV_NAME not in inventory_data:
        return []
    if not tracker_path.exists():
        return []

    text = tracker_path.read_text(encoding="utf-8", errors="replace")
    errors: list[str] = []

    errors.extend(validate_top_gap_source_coverage(tracker_path))
    errors.extend(validate_domain_summary_sections(tracker_path, text, inventory_data))
    errors.extend(validate_subagent_lane_snapshot_section(tracker_path, text))

    top_gap_section = extract_markdown_section(text, "## Top Gap Rollup")
    if top_gap_section is None:
        errors.append(f"{tracker_path.name}: missing `Top Gap Rollup` section")
    else:
        top_gap_rows = parse_markdown_table(
            top_gap_section,
            ("Area", "Rows", "Tracker paths", "Current evidence/status signal", "Next validation action"),
        )
        top_gap_expected = Counter(
            {
                area: sum(row_counts_by_name.get(csv_name, 0) for csv_name in csv_names)
                for area, csv_names in TOP_GAP_ROW_SOURCES.items()
            }
        )
        top_gap_expected["Validation coverage"] = sum(
            count for name, count in row_counts_by_name.items() if name != NEXT_SLICE_BLOCKER_CSV_NAME
        )
        errors.extend(validate_named_count_table(tracker_path, "Top Gap Rollup", top_gap_rows, "Area", "Rows", top_gap_expected))
        expected_paths_by_area = {
            area: csv_names
            for area, csv_names in TOP_GAP_ROW_SOURCES.items()
        }
        expected_paths_by_area["Validation coverage"] = ("Tools/WikiArchiveAudit/validate_content_retail_completeness_outputs.py",)
        errors.extend(validate_top_gap_rows(tracker_path, top_gap_rows, expected_paths_by_area))

    _, queue_rows = inventory_data[NEXT_SLICE_QUEUE_CSV_NAME]
    _, blocker_rows = inventory_data[NEXT_SLICE_BLOCKER_CSV_NAME]
    errors.extend(validate_next_slice_queue_summary_section(tracker_path, text, queue_rows, blocker_rows))

    queue_section = extract_markdown_section(text, "## Next Restoration Slice Queue")
    if queue_section is None:
        errors.append(f"{tracker_path.name}: missing `Next Restoration Slice Queue` section")
    else:
        lane_rows = parse_markdown_table(queue_section, ("Lane", "Rows"))
        lane_counts = Counter(row.get("lane", "") for row in queue_rows)
        errors.extend(validate_named_count_table(tracker_path, "Next Restoration Slice Queue lanes", lane_rows, "Lane", "Rows", lane_counts))

        actionability_rows = parse_markdown_table(queue_section, ("Actionability", "Rows"))
        actionability_counts = Counter(row.get("actionability_state", "") for row in queue_rows)
        errors.extend(
            validate_named_count_table(
                tracker_path,
                "Next Restoration Slice Queue actionability",
                actionability_rows,
                "Actionability",
                "Rows",
                actionability_counts,
            )
        )

        preview_rows = parse_markdown_table(queue_section, ("Rank", "Lane", "Content", "Score", "Blockers", "Action", "Reason"))
        errors.extend(validate_queue_preview_table(tracker_path, preview_rows, queue_rows))

        blocker_category_rows = parse_markdown_table(queue_section, ("Blocker category", "Rows"))
        blocker_category_counts = Counter(row.get("blocker_category", "") for row in blocker_rows)
        errors.extend(
            validate_named_count_table(
                tracker_path,
                "Next Restoration Slice Queue blocker categories",
                blocker_category_rows,
                "Blocker category",
                "Rows",
                blocker_category_counts,
            )
        )

    return errors


def parse_source_row_key(
    source_row_key: str,
    source_fieldnames: Sequence[str],
    extra_split_fieldnames: Sequence[str] = (),
) -> list[tuple[str, str]]:
    if not source_row_key.strip():
        return []

    source_fields = set(source_fieldnames)
    split_fieldnames = (*source_fieldnames, *extra_split_fieldnames)
    field_pattern = "|".join(re.escape(field) for field in sorted(split_fieldnames, key=len, reverse=True) if field)
    if not field_pattern:
        return []

    separator = re.compile(rf";(?=(?:{field_pattern})=)")
    pairs: list[tuple[str, str]] = []
    for part in separator.split(source_row_key):
        if "=" not in part:
            continue
        key, value = part.split("=", 1)
        key = key.strip()
        if key in source_fields:
            pairs.append((key, value.strip()))
    return pairs


def find_source_row_match_count(
    source_inventory: str,
    source_row_key: str,
    inventory_data: dict[str, tuple[list[str], list[dict[str, str]]]],
    match_cache: SourceRowMatchCache,
    source_row_index_cache: SourceRowIndexCache,
) -> tuple[int, list[tuple[str, str]]]:
    source = inventory_data.get(source_inventory)
    if source is None:
        return 0, []

    source_fieldnames, source_rows = source
    pairs = parse_source_row_key(
        source_row_key,
        source_fieldnames,
        VIRTUAL_SOURCE_ROW_KEY_FIELDS.get(source_inventory, ()),
    )
    cache_key = (source_inventory, tuple(pairs))
    if cache_key in match_cache:
        return match_cache[cache_key], pairs

    if not pairs:
        match_cache[cache_key] = 0
        return 0, pairs

    key_fields = tuple(key for key, _ in pairs)
    expected_values = tuple(value for _, value in pairs)
    source_index_key = (source_inventory, key_fields)
    source_row_index = source_row_index_cache.get(source_index_key)
    if source_row_index is None:
        source_row_index = Counter(
            tuple(str(row.get(key, "")).strip() for key in key_fields)
            for row in source_rows
        )
        source_row_index_cache[source_index_key] = source_row_index

    match_count = source_row_index.get(expected_values, 0)
    match_cache[cache_key] = match_count
    return match_count, pairs


def validate_source_row_references(
    path: Path,
    rows: list[dict[str, str]],
    inventory_data: dict[str, tuple[list[str], list[dict[str, str]]]],
    match_cache: SourceRowMatchCache,
    source_row_index_cache: SourceRowIndexCache,
) -> list[str]:
    errors: list[str] = []
    for row_number, row in enumerate(rows, start=2):
        source_inventory = row.get("source_inventory", "").strip()
        source_row_key = row.get("source_row_key", "").strip()
        if not source_inventory or not source_row_key or source_inventory not in inventory_data:
            continue

        match_count, matched_pairs = find_source_row_match_count(
            source_inventory,
            source_row_key,
            inventory_data,
            match_cache,
            source_row_index_cache,
        )
        if not matched_pairs:
            errors.append(f"{path.name}:{row_number}: source_row_key `{source_row_key}` has no fields from `{source_inventory}`")
        elif match_count == 0:
            errors.append(f"{path.name}:{row_number}: source_row_key `{source_row_key}` does not match a row in `{source_inventory}`")
        elif match_count > 1:
            errors.append(f"{path.name}:{row_number}: source_row_key `{source_row_key}` matches multiple rows in `{source_inventory}`")

        if path.name == NEXT_SLICE_QUEUE_CSV_NAME:
            source_id_field = QUEUE_SOURCE_ID_FIELDS.get(source_inventory)
            if source_id_field is None:
                continue
            source_ids = [value for key, value in matched_pairs if key == source_id_field]
            content_id = row.get("content_id", "").strip()
            if not source_ids:
                errors.append(
                    f"{path.name}:{row_number}: source_row_key `{source_row_key}` must include `{source_id_field}` for `{source_inventory}`"
                )
            elif source_ids[0] != content_id:
                errors.append(
                    f"{path.name}:{row_number}: content_id `{content_id}` does not match source_row_key "
                    f"`{source_id_field}={source_ids[0]}`"
                )

    return errors


def validate_queue_blocker_detail_rollups(
    queue_path: Path,
    queue_rows: list[dict[str, str]],
    blocker_rows: list[dict[str, str]],
) -> list[str]:
    errors: list[str] = []
    queue_by_slice: dict[str, dict[str, str]] = {}
    declared_blocker_total = 0
    for queue_row in queue_rows:
        slice_id = queue_row.get("slice_id", "").strip()
        if slice_id:
            queue_by_slice[slice_id] = queue_row

    blockers_by_slice: dict[str, list[dict[str, str]]] = {}
    for blocker in blocker_rows:
        blockers_by_slice.setdefault(blocker.get("slice_id", ""), []).append(blocker)

    for blocker_number, blocker in enumerate(blocker_rows, start=2):
        slice_id = blocker.get("slice_id", "").strip()
        queue_row = queue_by_slice.get(slice_id)
        if queue_row is None:
            errors.append(f"{NEXT_SLICE_BLOCKER_CSV_NAME}:{blocker_number}: blocker slice_id `{slice_id}` has no matching queue row")
            continue

        for column in ("queue_rank", "lane", "priority"):
            actual_value = blocker.get(column, "").strip()
            expected_value = queue_row.get(column, "").strip()
            if actual_value != expected_value:
                errors.append(
                    f"{NEXT_SLICE_BLOCKER_CSV_NAME}:{blocker_number}: {column} `{actual_value}` expected "
                    f"`{expected_value}` for `{slice_id}`"
                )

    def blocker_sort_key(blocker: dict[str, str]) -> int:
        try:
            return int(blocker.get("blocker_rank", "0") or "0")
        except ValueError:
            return 0

    for slice_id, slice_blockers in blockers_by_slice.items():
        ranks: list[int] = []
        for blocker in slice_blockers:
            try:
                ranks.append(int(blocker.get("blocker_rank", "") or ""))
            except ValueError:
                pass
        if ranks and sorted(ranks) != list(range(1, len(slice_blockers) + 1)):
            errors.append(f"{NEXT_SLICE_BLOCKER_CSV_NAME}: blocker_rank values for `{slice_id}` must be unique and contiguous from 1")

    for row_number, queue_row in enumerate(queue_rows, start=2):
        slice_id = queue_row.get("slice_id", "").strip()
        slice_blockers = sorted(blockers_by_slice.get(slice_id, []), key=blocker_sort_key)
        expected_count = len(slice_blockers)
        try:
            actual_count = int(queue_row.get("blocker_detail_count", ""))
        except ValueError:
            errors.append(
                f"{queue_path.name}:{row_number}: blocker_detail_count `{queue_row.get('blocker_detail_count', '')}` is not an integer"
            )
            actual_count = -1
        if actual_count >= 0:
            declared_blocker_total += actual_count
        if actual_count != expected_count:
            errors.append(
                f"{queue_path.name}:{row_number}: blocker_detail_count `{actual_count}` expected `{expected_count}` for `{slice_id}`"
            )

        expected_category_counts = format_named_counts(Counter(blocker.get("blocker_category", "") for blocker in slice_blockers))
        actual_category_counts = queue_row.get("blocker_count_by_category", "").strip()
        if actual_category_counts != expected_category_counts:
            errors.append(
                f"{queue_path.name}:{row_number}: blocker_count_by_category `{actual_category_counts}` expected `{expected_category_counts}` for `{slice_id}`"
            )

        expected_filter = f"slice_id={slice_id}"
        actual_filter = queue_row.get("blocker_detail_filter", "").strip()
        if actual_filter != expected_filter:
            errors.append(f"{queue_path.name}:{row_number}: blocker_detail_filter `{actual_filter}` expected `{expected_filter}`")

        expected_first_blocker_ids = format_text_sample([blocker.get("blocker_id", "") for blocker in slice_blockers])
        actual_first_blocker_ids = queue_row.get("first_blocker_ids", "").strip()
        if actual_first_blocker_ids != expected_first_blocker_ids:
            errors.append(
                f"{queue_path.name}:{row_number}: first_blocker_ids `{actual_first_blocker_ids}` expected `{expected_first_blocker_ids}` for `{slice_id}`"
            )

        expected_actionability = expected_queue_actionability_fields(queue_row, expected_count, Counter(blocker.get("blocker_category", "") for blocker in slice_blockers))
        for column, expected_value in expected_actionability.items():
            actual_value = queue_row.get(column, "").strip()
            if actual_value != expected_value:
                errors.append(
                    f"{queue_path.name}:{row_number}: {column} `{actual_value}` expected `{expected_value}` for `{slice_id}`"
                )

    if declared_blocker_total != len(blocker_rows):
        errors.append(
            f"{queue_path.name}: summed blocker_detail_count `{declared_blocker_total}` expected `{len(blocker_rows)}`"
        )

    return errors


def validate_rollup_docs(
    rollup_docs: Sequence[Path],
    root_path: Path,
    tracker_path: Path,
    csv_file_count: int,
    total_rows: int,
    blocker_row_count: int,
) -> list[str]:
    errors: list[str] = []
    for doc_path in rollup_docs:
        path = doc_path if doc_path.is_absolute() else root_path / doc_path
        if not path.exists():
            errors.append(f"Missing content retail-completeness rollup doc: {path}")
            continue

        text = path.read_text(encoding="utf-8", errors="replace")
        if not contains_repo_path(text, root_path, tracker_path):
            errors.append(f"{path.name}: missing rollup link to {tracker_path.name}")
        if NEXT_SLICE_BLOCKER_CSV_NAME not in text:
            errors.append(f"{path.name}: missing rollup link to {NEXT_SLICE_BLOCKER_CSV_NAME}")
        if not contains_count_phrase(text, csv_file_count, "generated CSVs"):
            errors.append(f"{path.name}: missing current generated CSV count `{csv_file_count}`")
        if not contains_count_phrase(text, total_rows, "rows"):
            errors.append(f"{path.name}: missing current generated row count `{display_count(total_rows)}`")
        if not contains_count_phrase(text, blocker_row_count, "blocker detail rows"):
            errors.append(f"{path.name}: missing current blocker detail row count `{display_count(blocker_row_count)}`")

    return errors


def validate_outputs(
    output_dir: Path = DEFAULT_OUTPUT_DIR,
    tracker_path: Path = DEFAULT_TRACKER,
    root_path: Path = ROOT,
    expected_csv_names: Sequence[str] = EXPECTED_CSV_NAMES,
    rollup_docs: Sequence[Path] = DEFAULT_ROLLUP_DOCS,
    subagent_findings_inventory: Path | None = None,
) -> tuple[ValidationSummary, list[str]]:
    csv_paths = sorted(output_dir.glob("content_retail_completeness_*.csv"))
    errors: list[str] = []
    total_rows = 0
    completion_counts: Counter[str] = Counter()
    row_counts_by_name: dict[str, int] = {}

    if not output_dir.exists():
        errors.append(f"Missing output directory: {output_dir}")
        return ValidationSummary(0, 0, completion_counts), errors
    if not csv_paths:
        errors.append(f"No content retail-completeness CSVs found under {output_dir}")

    inventory_data = {path.name: load_csv(path) for path in csv_paths}
    match_cache: SourceRowMatchCache = {}
    source_row_index_cache: SourceRowIndexCache = {}
    evidence_source_cache: EvidenceSourceCache = {}
    errors.extend(validate_expected_inventory(csv_paths, expected_csv_names))
    errors.extend(validate_tracker_links(csv_paths, tracker_path))
    for csv_path in csv_paths:
        fieldnames, rows = inventory_data[csv_path.name]
        csv_errors, row_count, csv_completion_counts = validate_csv_rows(
            csv_path,
            fieldnames,
            rows,
            root_path,
            evidence_source_cache,
        )
        errors.extend(csv_errors)
        total_rows += row_count
        row_counts_by_name[csv_path.name] = row_count
        completion_counts.update(csv_completion_counts)
        if csv_path.name in {NEXT_SLICE_QUEUE_CSV_NAME, NEXT_SLICE_BLOCKER_CSV_NAME}:
            errors.extend(
                validate_source_row_references(
                    csv_path,
                    rows,
                    inventory_data,
                    match_cache,
                    source_row_index_cache,
                )
            )

    if NEXT_SLICE_QUEUE_CSV_NAME in inventory_data and NEXT_SLICE_BLOCKER_CSV_NAME in inventory_data:
        _, queue_rows = inventory_data[NEXT_SLICE_QUEUE_CSV_NAME]
        _, blocker_rows = inventory_data[NEXT_SLICE_BLOCKER_CSV_NAME]
        errors.extend(validate_queue_blocker_detail_rollups(output_dir / NEXT_SLICE_QUEUE_CSV_NAME, queue_rows, blocker_rows))

    errors.extend(validate_tracker_markdown_rollups(tracker_path, row_counts_by_name, inventory_data))
    if subagent_findings_inventory is not None:
        subagent_rows, subagent_errors = validate_subagent_findings_inventory(
            subagent_findings_inventory,
            root_path,
            evidence_source_cache,
        )
        errors.extend(subagent_errors)
        if tracker_path.exists():
            tracker_text = tracker_path.read_text(encoding="utf-8", errors="replace")
            errors.extend(
                validate_subagent_findings_section(
                    tracker_path,
                    tracker_text,
                    subagent_rows,
                    subagent_findings_inventory,
                    root_path,
                )
            )
    errors.extend(
        validate_rollup_docs(
            rollup_docs,
            root_path,
            tracker_path,
            len(csv_paths),
            total_rows,
            row_counts_by_name.get(NEXT_SLICE_BLOCKER_CSV_NAME, 0),
        )
    )

    return ValidationSummary(len(csv_paths), total_rows, completion_counts), errors


def main() -> int:
    parser = argparse.ArgumentParser(description="Validate generated content retail-completeness tracker outputs.")
    parser.add_argument("--output-dir", type=Path, default=DEFAULT_OUTPUT_DIR)
    parser.add_argument("--tracker", type=Path, default=DEFAULT_TRACKER)
    parser.add_argument("--root", type=Path, default=ROOT, help="Repository root used to resolve relative evidence_sources.")
    parser.add_argument("--subagent-findings-inventory", type=Path, default=DEFAULT_SUBAGENT_FINDINGS_INVENTORY)
    args = parser.parse_args()

    summary, errors = validate_outputs(
        args.output_dir,
        args.tracker,
        args.root,
        subagent_findings_inventory=args.subagent_findings_inventory,
    )
    print(f"files={summary.file_count}")
    print(f"rows={summary.row_count}")
    for status, count in sorted(summary.completion_counts.items()):
        print(f"completion_status[{status}]={count}")

    if errors:
        for error in errors:
            print(error, file=sys.stderr)
        return 1
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
