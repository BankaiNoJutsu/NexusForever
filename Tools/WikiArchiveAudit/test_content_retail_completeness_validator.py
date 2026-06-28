import csv
import shutil
import tempfile
import unittest
from collections import Counter
from pathlib import Path

import validate_content_retail_completeness_outputs as validator


class ContentRetailCompletenessValidatorTests(unittest.TestCase):
    def setUp(self) -> None:
        self.root = Path(tempfile.mkdtemp(prefix="nf-content-validator-"))
        self.output_dir = self.root / "coverage"
        self.output_dir.mkdir(parents=True)
        self.tracker = self.root / "CONTENT_RETAIL_COMPLETENESS_TRACKER.md"
        (self.root / "Source" / "Existing").mkdir(parents=True)
        (self.root / "Source" / "Existing" / "Evidence.cs").write_text("// test evidence\n", encoding="utf-8")
        (self.root / "Source" / "Existing" / "DirectoryEvidence").mkdir()
        (self.root / "Decomp" / "Analysis").mkdir(parents=True)
        (self.root / "Decomp" / "Analysis" / "KNOWN_SMOKE_REVIEW.md").write_text("# test\n", encoding="utf-8")

    def tearDown(self) -> None:
        shutil.rmtree(self.root, ignore_errors=True)

    def write_csv(self, name: str, rows: list[dict[str, str]]) -> Path:
        path = self.output_dir / name
        fieldnames = list(rows[0].keys()) if rows else list(validator.REQUIRED_COLUMNS)
        with path.open("w", encoding="utf-8", newline="") as handle:
            writer = csv.DictWriter(handle, fieldnames=fieldnames)
            writer.writeheader()
            writer.writerows(rows)
        return path

    def write_tracker(self, *names: str) -> None:
        self.tracker.write_text("\n".join(f"- `{name}`" for name in names), encoding="utf-8")

    def write_rollup_doc(self, path: Path, csv_count: int, row_count: int, blocker_count: int) -> None:
        path.parent.mkdir(parents=True, exist_ok=True)
        path.write_text(
            "\n".join(
                [
                    "- `Decomp/Analysis/CONTENT_RETAIL_COMPLETENESS_TRACKER.md`",
                    "- `content_retail_completeness_next_slice_blockers.csv`",
                    f"- Validator covers `{csv_count}` generated CSVs / `{row_count:,}` rows.",
                    f"- Queue blocker detail coverage has `{blocker_count:,}` blocker detail rows.",
                ]
            ),
            encoding="utf-8",
        )

    def top_gap_counts_for_csvs(self, csv_names: list[str], overrides: dict[str, int] | None = None) -> dict[str, int]:
        row_counts: dict[str, int] = {}
        for csv_name in csv_names:
            csv_path = self.output_dir / csv_name
            if not csv_path.exists():
                continue

            with csv_path.open("r", encoding="utf-8", newline="") as handle:
                row_counts[csv_name] = sum(1 for _ in csv.DictReader(handle))

        counts = {
            area: sum(row_counts.get(csv_name, 0) for csv_name in csv_names_for_area)
            for area, csv_names_for_area in validator.TOP_GAP_ROW_SOURCES.items()
        }
        counts["Validation coverage"] = sum(
            count
            for csv_name, count in row_counts.items()
            if csv_name != validator.NEXT_SLICE_BLOCKER_CSV_NAME
        )
        if overrides is not None:
            counts.update(overrides)
        return counts

    def write_tracker_with_markdown_rollups(
        self,
        csv_names: list[str],
        top_gap_counts: dict[str, int],
        lane_counts: dict[str, int],
        queue_preview_rows: list[dict[str, str]],
        blocker_counts: dict[str, int],
    ) -> None:
        lines = [f"- `{name}`" for name in csv_names]
        self.append_domain_summary_sections(lines, csv_names)
        self.append_subagent_lane_snapshot_section(lines)
        lines.extend(
            [
                "",
                "## Top Gap Rollup",
                "",
                "| Area | Rows | Tracker paths | Current evidence/status signal | Next validation action |",
                "| --- | ---: | --- | --- | --- |",
            ]
        )
        for area, count in top_gap_counts.items():
            if area == "Validation coverage":
                tracker_paths = "`Tools/WikiArchiveAudit/validate_content_retail_completeness_outputs.py`"
            else:
                tracker_paths = ", ".join(
                    f"`{csv_name}`" for csv_name in validator.TOP_GAP_ROW_SOURCES.get(area, ())
                )
            lines.append(f"| {area} | {count} | {tracker_paths} | current evidence | validation action |")

        lines.extend(
            [
                "",
                "## Next Restoration Slice Queue Summary",
                "",
                f"- Total queue rows: {sum(lane_counts.values())}",
                "",
                "| Lane | Rows |",
                "| --- | ---: |",
            ]
        )
        for lane, count in lane_counts.items():
            lines.append(f"| `{lane}` | {count} |")

        lines.extend(
            [
                "",
                f"- Total queue blocker detail rows: {sum(blocker_counts.values())}",
                "",
                "| Blocker category | Rows |",
                "| --- | ---: |",
            ]
        )
        for category, count in blocker_counts.items():
            lines.append(f"| `{category}` | {count} |")

        lines.extend(
            [
                "",
                "## Next Restoration Slice Queue",
                "",
                "- `content_retail_completeness_next_slice_queue.csv`",
                "- `content_retail_completeness_next_slice_blockers.csv`",
                "",
                "| Lane | Rows |",
                "| --- | ---: |",
            ]
        )
        for lane, count in lane_counts.items():
            lines.append(f"| `{lane}` | {count} |")

        actionability_counts = Counter(row["actionability_state"] for row in queue_preview_rows)
        lines.extend(["", "| Actionability | Rows |", "| --- | ---: |"])
        for state, count in sorted(actionability_counts.items()):
            lines.append(f"| `{state}` | {count} |")

        lines.extend(["", "| Rank | Lane | Content | Score | Blockers | Action | Reason |", "| ---: | --- | --- | ---: | ---: | --- | --- |"])
        for row in queue_preview_rows:
            lines.append(
                f"| {row['queue_rank']} | `{row['lane']}` | `{row['content_id']}` {row['content_name']} | "
                f"{row['priority_score']} | {row['blocker_detail_count']} | `{row['actionability_state']}` | {row['candidate_reason']} |"
            )

        lines.extend(["", "| Blocker category | Rows |", "| --- | ---: |"])
        for category, count in blocker_counts.items():
            lines.append(f"| `{category}` | {count} |")

        self.tracker.write_text("\n".join(lines), encoding="utf-8")

    def append_subagent_lane_snapshot_section(self, lines: list[str]) -> None:
        lines.extend(
            [
                "",
                "## Parallel Subagent Lane Snapshot",
                "",
                "| Lane | IDs and tracker paths | Evidence sources | Confidence | Blockers | Next action |",
                "| --- | --- | --- | --- | --- | --- |",
            ]
        )
        for lane, expected_paths in validator.SUBAGENT_LANE_SNAPSHOT_PATHS.items():
            tracker_paths = ", ".join(f"`{expected_path}`" for expected_path in expected_paths)
            lines.append(
                f"| {lane} | {tracker_paths} | `client_sql`, `Source/Existing/Evidence.cs` | mapped with evidence | blocked by smoke | validate focused row |"
            )

    def append_domain_summary_sections(self, lines: list[str], csv_names: list[str]) -> None:
        for section_spec in validator.DOMAIN_SUMMARY_SECTION_SPECS:
            if section_spec.csv_name not in csv_names:
                continue
            csv_path = self.output_dir / section_spec.csv_name
            if not csv_path.exists():
                continue

            _, rows = validator.load_csv(csv_path)
            lines.extend(["", section_spec.heading, "", f"- {section_spec.total_label}: {len(rows)}"])
            for table_spec in section_spec.tables:
                lines.extend(["", f"| {table_spec.name_column} | {table_spec.count_column} |", "| --- | ---: |"])
                counts = Counter(row.get(table_spec.csv_field, "") for row in rows)
                for value, count in sorted(counts.items()):
                    lines.append(f"| `{value}` | {count} |")
            for table_spec in section_spec.composite_tables:
                header = " | ".join((*table_spec.name_columns, table_spec.count_column))
                separator = " | ".join(["---"] * len(table_spec.name_columns) + ["---:"])
                lines.extend(["", f"| {header} |", f"| {separator} |"])
                counts = Counter(tuple(row.get(csv_field, "") for csv_field in table_spec.csv_fields) for row in rows)
                for values, count in sorted(counts.items()):
                    formatted_values = " | ".join(f"`{value}`" for value in values)
                    lines.append(f"| {formatted_values} | {count} |")

    def validate_outputs(self, expected_names: list[str] | None = None) -> tuple[validator.ValidationSummary, list[str]]:
        if expected_names is None:
            expected_names = sorted(path.name for path in self.output_dir.glob("content_retail_completeness_*.csv"))
        return validator.validate_outputs(self.output_dir, self.tracker, self.root, expected_names, rollup_docs=())

    def validate_outputs_with_rollup_docs(
        self,
        rollup_docs: list[Path],
        expected_names: list[str] | None = None,
    ) -> tuple[validator.ValidationSummary, list[str]]:
        if expected_names is None:
            expected_names = sorted(path.name for path in self.output_dir.glob("content_retail_completeness_*.csv"))
        return validator.validate_outputs(self.output_dir, self.tracker, self.root, expected_names, rollup_docs=rollup_docs)

    def valid_row(self, **overrides: str) -> dict[str, str]:
        row = {
            "content_id": "1",
            "evidence_status": "mapped_pending_smoke",
            "evidence_sources": "client_sql;Source/Existing/Evidence.cs",
            "manual_validation": "pending",
            "completion_status": "not_retail_complete",
            "blocker_summary": "Mapped row still needs content smoke.",
        }
        row.update(overrides)
        return row

    def valid_queue_row(self, **overrides: str) -> dict[str, str]:
        row = self.valid_row(
            queue_rank="1",
            slice_id="quest-1",
            lane="quest",
            priority="P1",
            source_inventory="content_retail_completeness_quests.csv",
            source_row_key="quest_id=1",
            objective_row_count="",
            scripted_producer_count="",
            scripted_producer_objective_count="",
            scripted_producer_coverage="",
            handler_count="",
            seed_entity_count="",
            blocker_detail_count="0",
            blocker_count_by_category="",
            blocker_detail_filter="slice_id=quest-1",
            first_blocker_ids="",
            actionability_state="quest_validation_ready",
            split_required="false",
            split_reason="",
            primary_blocker_category="",
            implementation_surface="Source/Existing/Evidence.cs",
            retail_claim_allowed="false",
            required_evidence_level="Verified",
            blocking_evidence_source="client_smoke",
            validation_bundle_name="quest-1-test",
            narrow_test_filter="FullyQualifiedName~Q1",
            narrow_test_command='dotnet test Source\\NexusForever.Game.Tests\\NexusForever.Game.Tests.csproj -v minimal --nologo --filter "FullyQualifiedName~Q1"',
            harness_command='.\\Decomp\\Analysis\\Start-BlockerEvidenceHarness.ps1 -BundleName "quest-1-test" -ClientDirectory "I:\\WildStar" -WorldIds 426 -ObjectiveIds 1 -PromptForRootPassword',
            owner_paths="Source/Existing/Evidence.cs",
        )
        row.update(overrides)
        return row

    def valid_blocker_row(self, **overrides: str) -> dict[str, str]:
        row = self.valid_row(
            queue_rank="1",
            slice_id="quest-1",
            lane="quest",
            priority="P1",
            blocker_rank="1",
            blocker_id="quest-1:quest_objective_evidence:quest_id=1;jabbithole_quest_objective_id=10",
            blocker_category="quest_objective_evidence",
            blocker_type="unmatched",
            source_inventory="content_retail_completeness_quest_objective_evidence.csv",
            source_row_key="quest_id=1;jabbithole_quest_objective_id=10",
            related_ids="10@0:unmatched",
            required_validation="Reconcile objective evidence.",
            next_action="Review the objective bridge and run row-specific quest smoke.",
            tracker_paths="content_retail_completeness_quest_objective_evidence.csv",
        )
        row.update(overrides)
        return row

    def valid_subagent_finding_row(self, **overrides: str) -> dict[str, str]:
        row = {
            "finding_id": "finding-1",
            "lane": "Quest inventory/objective coverage",
            "priority": "P0",
            "status": "open",
            "finding_type": "tracker_refinement",
            "scope": "quest 1",
            "source_inventory": validator.QUEST_CSV_NAME,
            "source_row_key": "quest_id=1",
            "related_ids": "objective 10",
            "tracker_paths": "Source/Existing/Evidence.cs",
            "evidence_sources": "Source/Existing/Evidence.cs",
            "confidence": "Mapped with source evidence",
            "blocker_summary": "Still blocked by client smoke.",
            "next_action": "Capture focused client smoke.",
            "notes": "test row",
        }
        row.update(overrides)
        return row

    def write_subagent_findings_inventory(self, rows: list[dict[str, str]]) -> Path:
        path = self.root / "Tools" / "WikiArchiveAudit" / "reports" / "content-retail-subagent-findings.csv"
        path.parent.mkdir(parents=True, exist_ok=True)
        fieldnames = list(rows[0].keys()) if rows else list(validator.SUBAGENT_FINDINGS_REQUIRED_COLUMNS)
        with path.open("w", encoding="utf-8", newline="") as handle:
            writer = csv.DictWriter(handle, fieldnames=fieldnames)
            writer.writeheader()
            writer.writerows(rows)
        return path

    def apply_queue_actionability(self, queue_rows: list[dict[str, str]], blocker_rows: list[dict[str, str]]) -> None:
        for queue_row in queue_rows:
            slice_id = queue_row.get("slice_id", "")
            slice_blockers = [blocker for blocker in blocker_rows if blocker.get("slice_id", "") == slice_id]
            category_counts = Counter(blocker.get("blocker_category", "") for blocker in slice_blockers)
            queue_row.update(validator.expected_queue_actionability_fields(queue_row, len(slice_blockers), category_counts))

    def test_accepts_mapped_rows_with_required_status_columns_and_tracker_link(self) -> None:
        csv_path = self.write_csv("content_retail_completeness_example.csv", [self.valid_row()])
        self.write_tracker(csv_path.name)

        summary, errors = self.validate_outputs()

        self.assertEqual([], errors)
        self.assertEqual(1, summary.file_count)
        self.assertEqual(1, summary.row_count)
        self.assertEqual(1, summary.completion_counts["not_retail_complete"])

    def test_rejects_missing_tracker_link_and_required_columns(self) -> None:
        self.write_csv(
            "content_retail_completeness_example.csv",
            [
                {
                    "content_id": "1",
                    "evidence_status": "mapped_pending_smoke",
                    "completion_status": "not_retail_complete",
                }
            ],
        )
        self.write_tracker()

        _, errors = self.validate_outputs()

        self.assertTrue(any("missing generated inventory link" in error for error in errors))
        self.assertTrue(any("missing required columns" in error for error in errors))

    def test_accepts_status_rollup_docs_with_current_tracker_counts(self) -> None:
        self.tracker = self.root / "Decomp" / "Analysis" / "CONTENT_RETAIL_COMPLETENESS_TRACKER.md"
        blocker_path = self.write_csv("content_retail_completeness_next_slice_blockers.csv", [self.valid_blocker_row()])
        source_path = self.write_csv(
            "content_retail_completeness_quest_objective_evidence.csv",
            [self.valid_row(quest_id="1", jabbithole_quest_objective_id="10")],
        )
        self.write_tracker(blocker_path.name, source_path.name)
        current_status = self.root / "CURRENT_STATUS.md"
        missing_matrix = self.root / "Decomp" / "Analysis" / "MISSING_FEATURE_MATRIX.md"
        self.write_rollup_doc(current_status, 2, 2, 1)
        self.write_rollup_doc(missing_matrix, 2, 2, 1)

        summary, errors = self.validate_outputs_with_rollup_docs(
            [current_status, missing_matrix],
            [blocker_path.name, source_path.name],
        )

        self.assertEqual([], errors)
        self.assertEqual(2, summary.row_count)

    def test_rejects_stale_status_rollup_docs(self) -> None:
        self.tracker = self.root / "Decomp" / "Analysis" / "CONTENT_RETAIL_COMPLETENESS_TRACKER.md"
        blocker_path = self.write_csv(
            "content_retail_completeness_next_slice_blockers.csv",
            [
                self.valid_blocker_row(),
                self.valid_blocker_row(
                    blocker_rank="2",
                    blocker_id="quest-1:quest_reward:quest_id=1;reward_id=300",
                    blocker_category="quest_reward",
                    source_inventory="content_retail_completeness_quest_rewards.csv",
                    source_row_key="quest_id=1;reward_id=300",
                    tracker_paths="content_retail_completeness_quest_rewards.csv",
                ),
            ],
        )
        self.write_tracker(blocker_path.name)
        current_status = self.root / "CURRENT_STATUS.md"
        current_status.write_text(
            "\n".join(
                [
                    "- Stale content tracker rollup.",
                    "- Validator covers `0` generated CSVs / `1` rows.",
                    "- Queue blocker detail coverage has `1` blocker detail rows.",
                ]
            ),
            encoding="utf-8",
        )

        _, errors = self.validate_outputs_with_rollup_docs([current_status], [blocker_path.name])

        self.assertTrue(any("missing rollup link to CONTENT_RETAIL_COMPLETENESS_TRACKER.md" in error for error in errors))
        self.assertTrue(any("missing rollup link to content_retail_completeness_next_slice_blockers.csv" in error for error in errors))
        self.assertTrue(any("missing current generated CSV count `1`" in error for error in errors))
        self.assertTrue(any("missing current generated row count `2`" in error for error in errors))
        self.assertTrue(any("missing current blocker detail row count `2`" in error for error in errors))

    def test_accepts_tracker_markdown_rollups_matching_generated_csvs(self) -> None:
        rows_by_name = {
            validator.QUEST_CSV_NAME: [self.valid_row(quest_id="1", bucket="curated_full")],
            validator.QUEST_OBJECTIVE_CSV_NAME: [self.valid_row(quest_id="1", objective_id="10", support_status="mapped_generic_handler")],
            validator.QUEST_WORLD_DEPENDENCY_CSV_NAME: [
                self.valid_row(quest_id="1", dependency_id="10", dependency_type="quest_direction")
            ],
            validator.QUEST_PREREQUISITE_CSV_NAME: [
                self.valid_row(prerequisite_id="100", dependency_type="level", support_status="mapped_quest_accept_validation_pending_row_smoke")
            ],
            validator.QUEST_CREATURE_CSV_NAME: [
                self.valid_row(
                    creature_id="11063",
                    relation_type="finisher",
                    quest_link_status="matched_current_quest",
                    match_status="matched_client_creature",
                )
            ],
            validator.QUEST_REWARD_CSV_NAME: [
                self.valid_row(reward_id="300", reward_source="Quest2Reward", support_status="mapped_runtime_reward_path_pending_row_smoke")
            ],
            validator.QUEST_LOOT_CSV_NAME: [self.valid_row(loot_id="400", dependency_type="loot_item")],
            validator.QUEST_ACHIEVEMENT_CSV_NAME: [self.valid_row(achievement_id="500", dependency_type="achievement_checklist")],
            validator.INSTANCE_CSV_NAME: [
                self.valid_row(world_id="20", content_type="dungeon", milestone_scope="milestone_target")
            ],
            validator.INSTANCE_DEPENDENCY_CSV_NAME: [self.valid_row(world_id="20", dependency_id="21", dependency_type="public_event_objective")],
            validator.INSTANCE_ENTITY_CSV_NAME: [self.valid_row(world_id="20", entity_id="200", dependency_type="entity")],
            validator.INSTANCE_REWARD_CSV_NAME: [self.valid_row(world_id="20", reward_source="reward_rotation")],
            validator.SCRIPT_OBJECTIVE_PRODUCER_EVIDENCE_CSV_NAME: [
                self.valid_row(
                    evidence_kind="producer_call",
                    script_scope="instance",
                    producer_targeting="direct_objective",
                    objective_type_name="Script",
                    reference_link_status="matched_client_objective",
                    owner_link_status="matched_owner",
                    script_path="Script.cs",
                )
            ],
            validator.SCRIPT_RUNTIME_ACTION_EVIDENCE_CSV_NAME: [
                self.valid_row(
                    evidence_kind="runtime_action",
                    script_scope="instance",
                    action_category="teleport",
                    reference_link_status="matched_client_reference",
                    client_reference_status="matched_client_row",
                    source_trace_status="source_trace_present",
                    action_id="1",
                )
            ],
        }
        queue_rows = [
            self.valid_queue_row(
                queue_rank="1",
                slice_id="quest-1",
                lane="quest",
                priority="P0",
                content_type="quest",
                content_id="1",
                content_name="Quest One",
                priority_score="91",
                source_inventory=validator.QUEST_WORLD_DEPENDENCY_CSV_NAME,
                source_row_key="quest_id=1;dependency_id=10",
                candidate_reason="quest reason",
            ),
            self.valid_queue_row(
                queue_rank="2",
                slice_id="instance-20",
                lane="instance",
                content_id="20",
                content_name="Instance Twenty",
                priority_score="77",
                source_inventory=validator.INSTANCE_CSV_NAME,
                source_row_key="world_id=20",
                candidate_reason="instance reason",
                validation_bundle_name="instance-20-test",
                narrow_test_filter="FullyQualifiedName~Instance20",
                narrow_test_command='dotnet test Source\\NexusForever.Game.Tests\\NexusForever.Game.Tests.csproj -v minimal --nologo --filter "FullyQualifiedName~Instance20"',
                harness_command='.\\Decomp\\Analysis\\Start-BlockerEvidenceHarness.ps1 -BundleName "instance-20-test" -ClientDirectory "I:\\WildStar" -WorldIds 20 -ObjectiveIds 1 -PromptForRootPassword',
            ),
        ]
        blocker_rows = [
            self.valid_blocker_row(
                priority="P0",
                blocker_id="quest-1:quest_world_dependency:quest_id=1;dependency_id=10",
                blocker_category="quest_world_dependency",
                blocker_type="quest_direction",
                source_inventory=validator.QUEST_WORLD_DEPENDENCY_CSV_NAME,
                source_row_key="quest_id=1;dependency_id=10",
                tracker_paths=validator.QUEST_WORLD_DEPENDENCY_CSV_NAME,
            ),
            self.valid_blocker_row(
                queue_rank="2",
                slice_id="instance-20",
                lane="instance",
                blocker_id="instance-20:instance_entity:world_id=20;entity_id=200",
                blocker_category="instance_entity",
                blocker_type="entity",
                source_inventory=validator.INSTANCE_ENTITY_CSV_NAME,
                source_row_key="world_id=20;entity_id=200",
                tracker_paths=validator.INSTANCE_ENTITY_CSV_NAME,
            ),
        ]
        queue_rows[0].update(
            blocker_detail_count="1",
            blocker_count_by_category="quest_world_dependency=1",
            blocker_detail_filter="slice_id=quest-1",
            first_blocker_ids=blocker_rows[0]["blocker_id"],
        )
        queue_rows[1].update(
            blocker_detail_count="1",
            blocker_count_by_category="instance_entity=1",
            blocker_detail_filter="slice_id=instance-20",
            first_blocker_ids=blocker_rows[1]["blocker_id"],
        )
        self.apply_queue_actionability(queue_rows, blocker_rows)

        csv_names = [self.write_csv(name, rows).name for name, rows in rows_by_name.items()]
        queue_path = self.write_csv(validator.NEXT_SLICE_QUEUE_CSV_NAME, queue_rows)
        blocker_path = self.write_csv(validator.NEXT_SLICE_BLOCKER_CSV_NAME, blocker_rows)
        csv_names.extend([queue_path.name, blocker_path.name])
        self.write_tracker_with_markdown_rollups(
            csv_names,
            self.top_gap_counts_for_csvs(csv_names),
            {"instance": 1, "quest": 1},
            queue_rows,
            {"instance_entity": 1, "quest_world_dependency": 1},
        )

        summary, errors = self.validate_outputs(csv_names)

        self.assertEqual([], errors)
        self.assertEqual(18, summary.row_count)

    def test_accepts_explicit_blank_markdown_summary_bucket(self) -> None:
        csv_path = self.write_csv(
            validator.SCRIPT_PRESENTATION_EVIDENCE_CSV_NAME,
            [
                self.valid_row(
                    evidence_kind="communicator_message",
                    script_scope="quest",
                    client_link_status="matched_client_reference",
                    runtime_payload_status="",
                )
            ],
        )
        lines: list[str] = [f"- `{csv_path.name}`"]
        self.append_domain_summary_sections(lines, [csv_path.name])
        fieldnames, rows = validator.load_csv(csv_path)

        errors = validator.validate_domain_summary_sections(
            self.tracker,
            "\n".join(lines),
            {csv_path.name: (fieldnames, rows)},
        )

        self.assertEqual([], errors)

    def test_rejects_stale_tracker_markdown_rollups(self) -> None:
        source_path = self.write_csv(validator.QUEST_WORLD_DEPENDENCY_CSV_NAME, [self.valid_row(quest_id="1", dependency_id="10")])
        queue_row = self.valid_queue_row(
            content_id="1",
            content_name="Quest One",
            priority_score="91",
            source_inventory=source_path.name,
            source_row_key="quest_id=1;dependency_id=10",
            candidate_reason="quest reason",
        )
        queue_row.update(
            blocker_detail_count="1",
            blocker_count_by_category="quest_world_dependency=1",
            blocker_detail_filter="slice_id=quest-1",
            first_blocker_ids="quest-1:quest_objective_evidence:quest_id=1;jabbithole_quest_objective_id=10",
        )
        blocker_rows = [
            self.valid_blocker_row(
                blocker_category="quest_world_dependency",
                blocker_type="quest_direction",
                source_inventory=source_path.name,
                source_row_key="quest_id=1;dependency_id=10",
                tracker_paths=source_path.name,
            )
        ]
        self.apply_queue_actionability([queue_row], blocker_rows)
        queue_path = self.write_csv(validator.NEXT_SLICE_QUEUE_CSV_NAME, [queue_row])
        blocker_path = self.write_csv(
            validator.NEXT_SLICE_BLOCKER_CSV_NAME,
            blocker_rows,
        )
        csv_names = [source_path.name, queue_path.name, blocker_path.name]
        self.write_tracker_with_markdown_rollups(
            csv_names,
            self.top_gap_counts_for_csvs(
                csv_names,
                {
                    "Quest world, zone, and creature placement proof": 2,
                    "Validation coverage": 1,
                },
            ),
            {"quest": 2},
            [
                {
                    **queue_row,
                    "queue_rank": "1",
                    "lane": "quest",
                    "content_id": "999",
                    "priority_score": "1",
                }
            ],
            {"quest_world_dependency": 2},
        )

        _, errors = self.validate_outputs(csv_names)

        self.assertTrue(any("Top Gap Rollup` count for `Quest world, zone, and creature placement proof` is `2`, expected `1`" in error for error in errors))
        self.assertTrue(any("Validation coverage` is `1`, expected `2`" in error for error in errors))
        self.assertTrue(any("Next Restoration Slice Queue lanes` count for `quest` is `2`, expected `1`" in error for error in errors))
        self.assertTrue(any("queue preview rank `1` content id `999` expected `1`" in error for error in errors))
        self.assertTrue(any("queue preview rank `1` score `1` expected `91`" in error for error in errors))
        self.assertTrue(any("blocker categories` count for `quest_world_dependency` is `2`, expected `1`" in error for error in errors))

    def test_rejects_stale_domain_summary_rollups(self) -> None:
        quest_path = self.write_csv(validator.QUEST_CSV_NAME, [self.valid_row(quest_id="1", bucket="curated_full")])
        source_path = self.write_csv(
            validator.QUEST_WORLD_DEPENDENCY_CSV_NAME,
            [self.valid_row(quest_id="1", dependency_id="10", dependency_type="quest_direction")],
        )
        queue_row = self.valid_queue_row(
            content_id="1",
            content_name="Quest One",
            priority_score="91",
            source_inventory=source_path.name,
            source_row_key="quest_id=1;dependency_id=10",
            candidate_reason="quest reason",
        )
        blocker_row = self.valid_blocker_row(
            blocker_category="quest_world_dependency",
            blocker_type="quest_direction",
            source_inventory=source_path.name,
            source_row_key="quest_id=1;dependency_id=10",
            tracker_paths=source_path.name,
        )
        queue_row.update(
            blocker_detail_count="1",
            blocker_count_by_category="quest_world_dependency=1",
            blocker_detail_filter="slice_id=quest-1",
            first_blocker_ids=blocker_row["blocker_id"],
        )
        self.apply_queue_actionability([queue_row], [blocker_row])
        queue_path = self.write_csv(validator.NEXT_SLICE_QUEUE_CSV_NAME, [queue_row])
        blocker_path = self.write_csv(validator.NEXT_SLICE_BLOCKER_CSV_NAME, [blocker_row])
        csv_names = [quest_path.name, source_path.name, queue_path.name, blocker_path.name]
        self.write_tracker_with_markdown_rollups(
            csv_names,
            self.top_gap_counts_for_csvs(csv_names),
            {"quest": 1},
            [queue_row],
            {"quest_world_dependency": 1},
        )
        text = self.tracker.read_text(encoding="utf-8")
        text = text.replace("- Total Quest2 rows: 1", "- Total Quest2 rows: 2", 1)
        text = text.replace("| `curated_full` | 1 |", "| `curated_full` | 2 |", 1)
        self.tracker.write_text(text, encoding="utf-8")

        _, errors = self.validate_outputs(csv_names)

        self.assertTrue(any("`## Quest Summary` total `Total Quest2 rows` is `2`, expected `1`" in error for error in errors))
        self.assertTrue(any("Quest Summary buckets` count for `curated_full` is `2`, expected `1`" in error for error in errors))

    def test_rejects_stale_top_gap_paths_and_placeholders(self) -> None:
        errors = validator.validate_top_gap_rows(
            self.tracker,
            [
                {
                    "Area": "Quest world, zone, and creature placement proof",
                    "Tracker paths": "`stale.csv`",
                    "Current evidence/status signal": "status",
                    "Next validation action": "next",
                }
            ],
            {"Quest world, zone, and creature placement proof": (validator.QUEST_WORLD_DEPENDENCY_CSV_NAME,)},
        )

        self.assertTrue(any("missing expected tracker path" in error for error in errors))
        self.assertTrue(any("placeholder `Current evidence/status signal`" in error for error in errors))
        self.assertTrue(any("placeholder `Next validation action`" in error for error in errors))

    def test_rejects_stale_subagent_lane_snapshot_paths_and_placeholders(self) -> None:
        lines: list[str] = []
        self.append_subagent_lane_snapshot_section(lines)
        text = "\n".join(lines)
        text = text.replace(validator.QUEST_OBJECTIVE_CSV_NAME, "stale.csv", 1)
        text = text.replace("mapped with evidence", "todo", 1)
        text = text.replace("validate focused row", "tbd", 1)

        errors = validator.validate_subagent_lane_snapshot_section(self.tracker, text)

        self.assertTrue(any("missing expected tracker/evidence path" in error for error in errors))
        self.assertTrue(any("placeholder `Confidence`" in error for error in errors))
        self.assertTrue(any("placeholder `Next action`" in error for error in errors))

    def test_rejects_missing_and_unexpected_subagent_lane_snapshot_lanes(self) -> None:
        lines: list[str] = []
        self.append_subagent_lane_snapshot_section(lines)
        text = "\n".join(lines).replace(
            "Quest inventory/objective coverage",
            "Stale lane",
            1,
        )

        errors = validator.validate_subagent_lane_snapshot_section(self.tracker, text)

        self.assertTrue(any("missing lane `Quest inventory/objective coverage`" in error for error in errors))
        self.assertTrue(any("unexpected lane `Stale lane`" in error for error in errors))

    def test_subagent_lane_snapshot_assigns_every_expected_generated_csv_once(self) -> None:
        errors = validator.validate_subagent_lane_snapshot_source_coverage(self.tracker)

        self.assertEqual([], errors)

    def test_accepts_subagent_findings_inventory_and_tracker_section(self) -> None:
        row = self.valid_subagent_finding_row()
        report_path = self.write_subagent_findings_inventory([row])
        section_text = "\n".join(
            [
                "## Focused Subagent Findings Inventory",
                "",
                f"- Source inventory: `{report_path.relative_to(self.root).as_posix()}`",
                "- Total focused subagent finding rows: 1",
                "",
                "| Lane | Rows |",
                "| --- | ---: |",
                "| `Quest inventory/objective coverage` | 1 |",
                "",
                "| Status | Rows |",
                "| --- | ---: |",
                "| `open` | 1 |",
                "",
                "| Confidence | Rows |",
                "| --- | ---: |",
                "| `Mapped with source evidence` | 1 |",
                "",
                "| Finding | Priority | Lane | Scope | Source row | Confidence | Next action |",
                "| --- | --- | --- | --- | --- | --- | --- |",
                "| `finding-1` | `P0` | `Quest inventory/objective coverage` | quest 1 | `content_retail_completeness_quests.csv` `quest_id=1` | Mapped with source evidence | Capture focused client smoke. |",
            ]
        )

        rows, inventory_errors = validator.validate_subagent_findings_inventory(report_path, self.root, {})
        section_errors = validator.validate_subagent_findings_section(
            self.tracker,
            section_text,
            rows,
            report_path,
            self.root,
        )

        self.assertEqual([], inventory_errors)
        self.assertEqual([], section_errors)

    def test_rejects_invalid_subagent_findings_inventory_rows(self) -> None:
        report_path = self.write_subagent_findings_inventory(
            [
                self.valid_subagent_finding_row(
                    lane="Stale lane",
                    priority="P9",
                    status="mystery",
                    source_inventory="content_retail_completeness_missing.csv",
                    confidence="todo",
                    next_action="tbd",
                )
            ]
        )

        _, errors = validator.validate_subagent_findings_inventory(report_path, self.root, {})

        self.assertTrue(any("unknown focused subagent lane `Stale lane`" in error for error in errors))
        self.assertTrue(any("unsupported focused subagent priority `P9`" in error for error in errors))
        self.assertTrue(any("unsupported focused subagent status `mystery`" in error for error in errors))
        self.assertTrue(any("source_inventory `content_retail_completeness_missing.csv` is not an expected generated CSV" in error for error in errors))
        self.assertTrue(any("placeholder focused subagent finding `confidence`" in error for error in errors))
        self.assertTrue(any("placeholder focused subagent finding `next_action`" in error for error in errors))

    def test_rejects_stale_script_composite_summary_rollup(self) -> None:
        source_path = self.write_csv(
            validator.QUEST_WORLD_DEPENDENCY_CSV_NAME,
            [self.valid_row(quest_id="1", dependency_id="10", dependency_type="quest_direction")],
        )
        script_path = self.write_csv(
            validator.SCRIPT_OBJECTIVE_PRODUCER_EVIDENCE_CSV_NAME,
            [
                self.valid_row(
                    evidence_kind="producer_call",
                    script_scope="instance",
                    producer_targeting="direct_objective",
                    objective_type_name="Script",
                    reference_link_status="matched_client_objective",
                    owner_link_status="matched_owner",
                )
            ],
        )
        queue_row = self.valid_queue_row(
            content_id="1",
            content_name="Quest One",
            priority_score="91",
            source_inventory=source_path.name,
            source_row_key="quest_id=1;dependency_id=10",
            candidate_reason="quest reason",
        )
        blocker_row = self.valid_blocker_row(
            blocker_category="quest_world_dependency",
            blocker_type="quest_direction",
            source_inventory=source_path.name,
            source_row_key="quest_id=1;dependency_id=10",
            tracker_paths=source_path.name,
        )
        queue_row.update(
            blocker_detail_count="1",
            blocker_count_by_category="quest_world_dependency=1",
            blocker_detail_filter="slice_id=quest-1",
            first_blocker_ids=blocker_row["blocker_id"],
        )
        self.apply_queue_actionability([queue_row], [blocker_row])
        queue_path = self.write_csv(validator.NEXT_SLICE_QUEUE_CSV_NAME, [queue_row])
        blocker_path = self.write_csv(validator.NEXT_SLICE_BLOCKER_CSV_NAME, [blocker_row])
        csv_names = [source_path.name, script_path.name, queue_path.name, blocker_path.name]
        self.write_tracker_with_markdown_rollups(
            csv_names,
            self.top_gap_counts_for_csvs(csv_names),
            {"quest": 1},
            [queue_row],
            {"quest_world_dependency": 1},
        )
        text = self.tracker.read_text(encoding="utf-8")
        text = text.replace("| `instance` | `Script` | 1 |", "| `instance` | `Script` | 2 |", 1)
        self.tracker.write_text(text, encoding="utf-8")

        _, errors = self.validate_outputs(csv_names)

        self.assertTrue(any("scope/objective types` count for `instance | Script` is `2`, expected `1`" in error for error in errors))

    def test_rejects_stale_next_slice_queue_summary_totals(self) -> None:
        source_path = self.write_csv(
            validator.QUEST_WORLD_DEPENDENCY_CSV_NAME,
            [self.valid_row(quest_id="1", dependency_id="10", dependency_type="quest_direction")],
        )
        queue_row = self.valid_queue_row(
            content_id="1",
            content_name="Quest One",
            priority_score="91",
            source_inventory=source_path.name,
            source_row_key="quest_id=1;dependency_id=10",
            candidate_reason="quest reason",
        )
        blocker_row = self.valid_blocker_row(
            blocker_category="quest_world_dependency",
            blocker_type="quest_direction",
            source_inventory=source_path.name,
            source_row_key="quest_id=1;dependency_id=10",
            tracker_paths=source_path.name,
        )
        queue_row.update(
            blocker_detail_count="1",
            blocker_count_by_category="quest_world_dependency=1",
            blocker_detail_filter="slice_id=quest-1",
            first_blocker_ids=blocker_row["blocker_id"],
        )
        self.apply_queue_actionability([queue_row], [blocker_row])
        queue_path = self.write_csv(validator.NEXT_SLICE_QUEUE_CSV_NAME, [queue_row])
        blocker_path = self.write_csv(validator.NEXT_SLICE_BLOCKER_CSV_NAME, [blocker_row])
        csv_names = [source_path.name, queue_path.name, blocker_path.name]
        self.write_tracker_with_markdown_rollups(
            csv_names,
            self.top_gap_counts_for_csvs(csv_names),
            {"quest": 1},
            [queue_row],
            {"quest_world_dependency": 1},
        )
        text = self.tracker.read_text(encoding="utf-8")
        text = text.replace("- Total queue rows: 1", "- Total queue rows: 2", 1)
        text = text.replace("- Total queue blocker detail rows: 1", "- Total queue blocker detail rows: 2", 1)
        self.tracker.write_text(text, encoding="utf-8")

        _, errors = self.validate_outputs(csv_names)

        self.assertTrue(any("`Next Restoration Slice Queue Summary` total `Total queue rows` is `2`, expected `1`" in error for error in errors))
        self.assertTrue(any("`Next Restoration Slice Queue Summary` total `Total queue blocker detail rows` is `2`, expected `1`" in error for error in errors))

    def test_rejects_incomplete_next_slice_queue_detail_table(self) -> None:
        quest_source_path = self.write_csv(
            validator.QUEST_WORLD_DEPENDENCY_CSV_NAME,
            [self.valid_row(quest_id="1", dependency_id="10", dependency_type="quest_direction")],
        )
        instance_source_path = self.write_csv(
            validator.INSTANCE_CSV_NAME,
            [self.valid_row(world_id="20", content_type="dungeon", milestone_scope="milestone_target")],
        )
        queue_rows = [
            self.valid_queue_row(
                content_id="1",
                content_name="Quest One",
                priority_score="91",
                source_inventory=quest_source_path.name,
                source_row_key="quest_id=1;dependency_id=10",
                candidate_reason="quest reason",
            ),
            self.valid_queue_row(
                queue_rank="2",
                slice_id="instance-20",
                lane="instance",
                content_id="20",
                content_name="Instance Twenty",
                priority_score="77",
                source_inventory=instance_source_path.name,
                source_row_key="world_id=20",
                candidate_reason="instance reason",
                validation_bundle_name="instance-20-test",
                narrow_test_filter="FullyQualifiedName~Instance20",
                narrow_test_command='dotnet test Source\\NexusForever.Game.Tests\\NexusForever.Game.Tests.csproj -v minimal --nologo --filter "FullyQualifiedName~Instance20"',
                harness_command='.\\Decomp\\Analysis\\Start-BlockerEvidenceHarness.ps1 -BundleName "instance-20-test" -ClientDirectory "I:\\WildStar" -WorldIds 20 -ObjectiveIds 1 -PromptForRootPassword',
            ),
        ]
        blocker_row = self.valid_blocker_row(
            blocker_category="quest_world_dependency",
            blocker_type="quest_direction",
            source_inventory=quest_source_path.name,
            source_row_key="quest_id=1;dependency_id=10",
            tracker_paths=quest_source_path.name,
        )
        self.apply_queue_actionability(queue_rows, [blocker_row])
        queue_path = self.write_csv(validator.NEXT_SLICE_QUEUE_CSV_NAME, queue_rows)
        blocker_path = self.write_csv(validator.NEXT_SLICE_BLOCKER_CSV_NAME, [blocker_row])
        csv_names = [quest_source_path.name, instance_source_path.name, queue_path.name, blocker_path.name]
        self.write_tracker_with_markdown_rollups(
            csv_names,
            self.top_gap_counts_for_csvs(csv_names),
            {"instance": 1, "quest": 1},
            [queue_rows[0]],
            {"quest_objective_evidence": 1},
        )

        _, errors = self.validate_outputs(csv_names)

        self.assertTrue(any("queue detail table has `1` rows, expected `2`" in error for error in errors))

    def test_rejects_stale_queue_blocker_detail_rollups(self) -> None:
        source_rows = [
            self.valid_row(quest_id="1", dependency_id="10"),
            self.valid_row(quest_id="1", dependency_id="11"),
        ]
        source_path = self.write_csv(validator.QUEST_WORLD_DEPENDENCY_CSV_NAME, source_rows)
        queue_row = self.valid_queue_row(
            content_id="1",
            content_name="Quest One",
            priority_score="91",
            source_inventory=source_path.name,
            source_row_key="quest_id=1;dependency_id=10",
            candidate_reason="quest reason",
            blocker_detail_count="1",
            blocker_count_by_category="quest_world_dependency=1",
            blocker_detail_filter="slice_id=wrong",
            first_blocker_ids="stale-blocker-id",
        )
        blocker_rows = [
            self.valid_blocker_row(
                blocker_id="quest-1:quest_world_dependency:quest_id=1;dependency_id=10",
                blocker_category="quest_world_dependency",
                blocker_type="quest_direction",
                source_inventory=source_path.name,
                source_row_key="quest_id=1;dependency_id=10",
                tracker_paths=source_path.name,
            ),
            self.valid_blocker_row(
                blocker_rank="2",
                blocker_id="quest-1:quest_world_dependency:quest_id=1;dependency_id=11",
                blocker_category="quest_world_dependency",
                blocker_type="quest_direction",
                source_inventory=source_path.name,
                source_row_key="quest_id=1;dependency_id=11",
                tracker_paths=source_path.name,
            ),
        ]
        queue_path = self.write_csv(validator.NEXT_SLICE_QUEUE_CSV_NAME, [queue_row])
        blocker_path = self.write_csv(validator.NEXT_SLICE_BLOCKER_CSV_NAME, blocker_rows)
        csv_names = [source_path.name, queue_path.name, blocker_path.name]
        self.write_tracker(queue_path.name, blocker_path.name, source_path.name)

        _, errors = self.validate_outputs(csv_names)

        self.assertTrue(any("blocker_detail_count `1` expected `2`" in error for error in errors))
        self.assertTrue(any("blocker_count_by_category `quest_world_dependency=1` expected `quest_world_dependency=2`" in error for error in errors))
        self.assertTrue(any("blocker_detail_filter `slice_id=wrong` expected `slice_id=quest-1`" in error for error in errors))
        self.assertTrue(any("first_blocker_ids `stale-blocker-id` expected" in error for error in errors))

    def test_rejects_orphan_mismatched_and_noncontiguous_blockers(self) -> None:
        source_rows = [
            self.valid_row(quest_id="1", dependency_id="10"),
            self.valid_row(quest_id="1", dependency_id="11"),
            self.valid_row(quest_id="2", dependency_id="12"),
        ]
        source_path = self.write_csv(validator.QUEST_WORLD_DEPENDENCY_CSV_NAME, source_rows)
        category_counts = Counter({"quest_world_dependency": 2})
        blocker_ids = ["mismatch-blocker", "noncontiguous-blocker"]
        queue_row = self.valid_queue_row(
            content_id="1",
            content_name="Quest One",
            priority_score="91",
            source_inventory=source_path.name,
            source_row_key="quest_id=1;dependency_id=10",
            candidate_reason="quest reason",
            blocker_detail_count="2",
            blocker_count_by_category=validator.format_named_counts(category_counts),
            first_blocker_ids=validator.format_text_sample(blocker_ids),
        )
        queue_row.update(validator.expected_queue_actionability_fields(queue_row, 2, category_counts))
        blocker_rows = [
            self.valid_blocker_row(
                queue_rank="2",
                lane="instance",
                priority="P2",
                blocker_id=blocker_ids[0],
                blocker_category="quest_world_dependency",
                blocker_type="quest_direction",
                source_inventory=source_path.name,
                source_row_key="quest_id=1;dependency_id=10",
                tracker_paths=source_path.name,
            ),
            self.valid_blocker_row(
                blocker_rank="3",
                blocker_id=blocker_ids[1],
                blocker_category="quest_world_dependency",
                blocker_type="quest_direction",
                source_inventory=source_path.name,
                source_row_key="quest_id=1;dependency_id=11",
                tracker_paths=source_path.name,
            ),
            self.valid_blocker_row(
                slice_id="quest-2",
                blocker_id="orphan-blocker",
                blocker_category="quest_world_dependency",
                blocker_type="quest_direction",
                source_inventory=source_path.name,
                source_row_key="quest_id=2;dependency_id=12",
                tracker_paths=source_path.name,
            ),
        ]
        queue_path = self.write_csv(validator.NEXT_SLICE_QUEUE_CSV_NAME, [queue_row])
        blocker_path = self.write_csv(validator.NEXT_SLICE_BLOCKER_CSV_NAME, blocker_rows)
        csv_names = [source_path.name, queue_path.name, blocker_path.name]
        self.write_tracker(queue_path.name, blocker_path.name, source_path.name)

        _, errors = self.validate_outputs(csv_names)

        self.assertTrue(any("queue_rank `2` expected `1` for `quest-1`" in error for error in errors))
        self.assertTrue(any("lane `instance` expected `quest` for `quest-1`" in error for error in errors))
        self.assertTrue(any("priority `P2` expected `P1` for `quest-1`" in error for error in errors))
        self.assertTrue(any("blocker slice_id `quest-2` has no matching queue row" in error for error in errors))
        self.assertTrue(any("blocker_rank values for `quest-1` must be unique and contiguous from 1" in error for error in errors))
        self.assertTrue(any("summed blocker_detail_count `2` expected `3`" in error for error in errors))

    def test_accepts_source_row_key_values_with_semicolons(self) -> None:
        blocker_path = self.write_csv(
            "content_retail_completeness_next_slice_blockers.csv",
            [
                self.valid_blocker_row(
                    blocker_id="quest-1:quest_objective:quest_id=1;objective_id=10",
                    blocker_category="quest_objective",
                    blocker_type="5",
                    source_inventory="content_retail_completeness_quest_objectives.csv",
                    source_row_key="quest_id=1;objective_id=10;world_location_indicator_ids=870;871;target_group_id_reward_pane=300",
                    tracker_paths="content_retail_completeness_quest_objectives.csv",
                )
            ],
        )
        source_path = self.write_csv(
            "content_retail_completeness_quest_objectives.csv",
            [
                self.valid_row(
                    quest_id="1",
                    objective_id="10",
                    world_location_indicator_ids="870;871",
                    target_group_id_reward_pane="300",
                )
            ],
        )
        self.write_tracker(blocker_path.name, source_path.name)

        _, errors = self.validate_outputs([blocker_path.name, source_path.name])

        self.assertEqual([], errors)

    def test_rejects_source_row_key_without_unique_source_match(self) -> None:
        blocker_path = self.write_csv(
            "content_retail_completeness_next_slice_blockers.csv",
            [
                self.valid_blocker_row(
                    blocker_id="quest-1:quest_objective_evidence:quest_id=99;jabbithole_quest_objective_id=10",
                    source_row_key="quest_id=99;jabbithole_quest_objective_id=10",
                ),
                self.valid_blocker_row(
                    blocker_rank="2",
                    blocker_id="quest-1:quest_objective_evidence:quest_id=1;jabbithole_quest_objective_id=10",
                    source_row_key="quest_id=1;jabbithole_quest_objective_id=10",
                ),
            ],
        )
        source_path = self.write_csv(
            "content_retail_completeness_quest_objective_evidence.csv",
            [
                self.valid_row(quest_id="1", jabbithole_quest_objective_id="10"),
                self.valid_row(quest_id="1", jabbithole_quest_objective_id="10"),
            ],
        )
        self.write_tracker(blocker_path.name, source_path.name)

        _, errors = self.validate_outputs([blocker_path.name, source_path.name])

        self.assertTrue(any("does not match a row" in error for error in errors))
        self.assertTrue(any("matches multiple rows" in error for error in errors))

    def test_rejects_queue_source_identity_mismatches(self) -> None:
        quest_path = self.write_csv(
            validator.QUEST_CSV_NAME,
            [
                self.valid_row(content_id="1", quest_id="1"),
                self.valid_row(content_id="2", quest_id="2"),
            ],
        )
        instance_path = self.write_csv(
            validator.INSTANCE_CSV_NAME,
            [
                self.valid_row(content_id="20", world_id="20"),
                self.valid_row(content_id="21", world_id="21"),
            ],
        )
        queue_path = self.write_csv(
            validator.NEXT_SLICE_QUEUE_CSV_NAME,
            [
                self.valid_queue_row(
                    content_type="quest",
                    content_id="1",
                    source_inventory=quest_path.name,
                    source_row_key="quest_id=2",
                ),
                self.valid_queue_row(
                    queue_rank="2",
                    slice_id="instance-20",
                    lane="instance",
                    content_type="instance",
                    content_id="20",
                    source_inventory=instance_path.name,
                    source_row_key="world_id=21",
                    validation_bundle_name="instance-20-test",
                    narrow_test_filter="FullyQualifiedName~Instance20",
                    narrow_test_command='dotnet test Source\\NexusForever.Game.Tests\\NexusForever.Game.Tests.csproj -v minimal --nologo --filter "FullyQualifiedName~Instance20"',
                    harness_command='.\\Decomp\\Analysis\\Start-BlockerEvidenceHarness.ps1 -BundleName "instance-20-test" -ClientDirectory "I:\\WildStar" -WorldIds 20 -ObjectiveIds 1 -PromptForRootPassword',
                ),
            ],
        )
        self.write_tracker(quest_path.name, instance_path.name, queue_path.name)

        _, errors = self.validate_outputs([quest_path.name, instance_path.name, queue_path.name])

        self.assertTrue(any("content_id `1` does not match source_row_key `quest_id=2`" in error for error in errors))
        self.assertTrue(any("content_id `20` does not match source_row_key `world_id=21`" in error for error in errors))

    def test_rejects_retail_complete_rows_with_pending_manual_validation_or_blockers(self) -> None:
        csv_path = self.write_csv(
            "content_retail_completeness_example.csv",
            [
                self.valid_row(
                    evidence_status="mapped_pending_smoke",
                    manual_validation="pending",
                    completion_status="retail_complete",
                    blocker_summary="Blocked pending smoke.",
                )
            ],
        )
        self.write_tracker(csv_path.name)

        _, errors = self.validate_outputs()

        self.assertTrue(any("pending manual_validation" in error for error in errors))
        self.assertTrue(any("lacks terminal retail-complete evidence_status" in error for error in errors))
        self.assertTrue(any("open evidence_status" in error for error in errors))
        self.assertTrue(any("open blocker_summary" in error for error in errors))

    def test_rejects_retail_complete_rows_with_pending_evidence_status_even_when_manual_and_blocker_are_closed(self) -> None:
        csv_path = self.write_csv(
            "content_retail_completeness_example.csv",
            [
                self.valid_row(
                    evidence_status="implemented_pending_manual_smoke",
                    manual_validation="validated: local smoke bundle 2026-06-11",
                    completion_status="retail_complete",
                    blocker_summary="none",
                )
            ],
        )
        self.write_tracker(csv_path.name)

        _, errors = self.validate_outputs()

        self.assertTrue(any("lacks terminal retail-complete evidence_status" in error for error in errors))
        self.assertTrue(any("open evidence_status" in error for error in errors))

    def test_accepts_retail_complete_rows_with_verification_and_closed_blocker(self) -> None:
        csv_path = self.write_csv(
            "content_retail_completeness_example.csv",
            [
                self.valid_row(
                    evidence_status="implemented_with_automated_and_manual_validation",
                    manual_validation="validated: local smoke bundle 2026-06-11",
                    completion_status="retail_complete",
                    blocker_summary="none",
                )
            ],
        )
        self.write_tracker(csv_path.name)

        summary, errors = self.validate_outputs()

        self.assertEqual([], errors)
        self.assertEqual(1, summary.completion_counts["retail_complete"])

    def test_rejects_missing_repo_relative_evidence_sources(self) -> None:
        csv_path = self.write_csv(
            "content_retail_completeness_example.csv",
            [
                self.valid_row(
                    evidence_sources="Source/Existing/Evidence.cs;Source/Missing/Evidence.cs;Decomp/Analysis/MISSING_*_REVIEW.md"
                )
            ],
        )
        self.write_tracker(csv_path.name)

        _, errors = self.validate_outputs()

        self.assertTrue(any("Source/Missing/Evidence.cs" in error for error in errors))
        self.assertTrue(any("Decomp/Analysis/MISSING_*_REVIEW.md" in error for error in errors))

    def test_accepts_existing_directories_and_glob_evidence_sources(self) -> None:
        csv_path = self.write_csv(
            "content_retail_completeness_example.csv",
            [
                self.valid_row(
                    evidence_sources="Source/Existing/DirectoryEvidence;Decomp/Analysis/KNOWN_*_REVIEW.md"
                )
            ],
        )
        self.write_tracker(csv_path.name)

        summary, errors = self.validate_outputs()

        self.assertEqual([], errors)
        self.assertEqual(1, summary.row_count)

    def test_reuses_evidence_source_cache_for_repeated_paths(self) -> None:
        csv_path = self.write_csv(
            "content_retail_completeness_example.csv",
            [
                self.valid_row(evidence_sources="client_sql;Source/Existing/Evidence.cs"),
                self.valid_row(content_id="2", evidence_sources="client_sql;Source/Existing/Evidence.cs"),
            ],
        )
        fieldnames, rows = validator.load_csv(csv_path)
        calls: list[str] = []
        original_exists = validator.evidence_source_exists

        def record_exists(root_path: Path, source: str) -> bool:
            calls.append(source)
            return original_exists(root_path, source)

        validator.evidence_source_exists = record_exists
        try:
            errors, row_count, _ = validator.validate_csv_rows(csv_path, fieldnames, rows, self.root, {})
        finally:
            validator.evidence_source_exists = original_exists

        self.assertEqual([], errors)
        self.assertEqual(2, row_count)
        self.assertEqual(["Source/Existing/Evidence.cs"], calls)

    def test_reports_none_required_cells_instead_of_crashing(self) -> None:
        csv_path = self.output_dir / "content_retail_completeness_example.csv"
        row = self.valid_row()
        row["manual_validation"] = None

        errors, row_count, completion_counts = validator.validate_csv_rows(
            csv_path,
            list(validator.REQUIRED_COLUMNS),
            [row],
            self.root,
            {},
        )

        self.assertEqual(1, row_count)
        self.assertEqual(1, completion_counts["not_retail_complete"])
        self.assertTrue(any("blank required column `manual_validation`" in error for error in errors))

    def test_rejects_missing_and_unexpected_inventory_files(self) -> None:
        csv_path = self.write_csv("content_retail_completeness_unexpected.csv", [self.valid_row()])
        self.write_tracker(csv_path.name)

        _, errors = self.validate_outputs(["content_retail_completeness_required.csv"])

        self.assertTrue(any("Missing expected content retail-completeness inventory" in error for error in errors))
        self.assertTrue(any("content_retail_completeness_required.csv" in error for error in errors))
        self.assertTrue(any("Unexpected content retail-completeness inventory" in error for error in errors))
        self.assertTrue(any("content_retail_completeness_unexpected.csv" in error for error in errors))

    def test_accepts_valid_next_slice_queue_metadata(self) -> None:
        csv_path = self.write_csv("content_retail_completeness_next_slice_queue.csv", [self.valid_queue_row()])
        self.write_tracker(csv_path.name)

        summary, errors = self.validate_outputs()

        self.assertEqual([], errors)
        self.assertEqual(1, summary.row_count)

    def test_accepts_valid_next_slice_blocker_metadata(self) -> None:
        blocker_path = self.write_csv(
            "content_retail_completeness_next_slice_blockers.csv",
            [
                self.valid_blocker_row(),
                self.valid_blocker_row(
                    blocker_rank="2",
                    blocker_id="quest-1:quest_episode_evidence:quest_id=1;episode_quest_id=3000",
                    blocker_category="quest_episode_evidence",
                    blocker_type="matched",
                    source_inventory="content_retail_completeness_quest_episode_evidence.csv",
                    source_row_key="quest_id=1;episode_quest_id=3000",
                    related_ids="quest_id=1;episode_id=314;episode_name=Arrival",
                    tracker_paths="content_retail_completeness_quest_episode_evidence.csv",
                ),
                self.valid_blocker_row(
                    blocker_rank="3",
                    blocker_id="instance-10:public_event_evidence:public_event_id=10;evidence_kind=public_event_creature",
                    blocker_category="public_event_evidence",
                    blocker_type="public_event_creature",
                    source_inventory="content_retail_completeness_public_event_evidence.csv",
                    source_row_key="public_event_id=10;evidence_kind=public_event_creature",
                    related_ids="public_event_id=10;related_client_id=75508",
                    tracker_paths="content_retail_completeness_public_event_evidence.csv",
                ),
                self.valid_blocker_row(
                    blocker_rank="4",
                    blocker_id="quest-1:quest_objective:quest_id=1;objective_id=10;objective_type=5",
                    blocker_category="quest_objective",
                    blocker_type="5",
                    source_inventory="content_retail_completeness_quest_objectives.csv",
                    source_row_key="quest_id=1;objective_id=10;objective_type=5",
                    related_ids="quest_id=1;objective_id=10;support_status=mapped_generic_handler",
                    tracker_paths="content_retail_completeness_quest_objectives.csv",
                ),
                self.valid_blocker_row(
                    blocker_rank="5",
                    blocker_id="quest-1:quest_script:quest_id=1;script_kind=quest_script;script_class=FocusedQuest",
                    blocker_category="quest_script",
                    blocker_type="quest_script",
                    source_inventory="content_retail_completeness_quest_scripts.csv",
                    source_row_key="quest_id=1;script_kind=quest_script;script_class=FocusedQuest",
                    related_ids="quest_id=1;script_class=FocusedQuest;owner_ids_on_attribute=1",
                    tracker_paths="content_retail_completeness_quest_scripts.csv",
                ),
                self.valid_blocker_row(
                    blocker_rank="6",
                    blocker_id="instance-10:instance_portal_evidence:instance_portal_id=41;world_id=10",
                    blocker_category="instance_portal_evidence",
                    blocker_type="instance_portal_creature",
                    source_inventory="content_retail_completeness_instance_portal_evidence.csv",
                    source_row_key="instance_portal_id=41;evidence_kind=instance_portal_creature;world_id=10",
                    related_ids="queue_world_id=10;creature2_id=75508",
                    tracker_paths="content_retail_completeness_instance_portal_evidence.csv",
                ),
            ],
        )
        source_path = self.write_csv(
            "content_retail_completeness_quest_objective_evidence.csv",
            [self.valid_row(quest_id="1", jabbithole_quest_objective_id="10")],
        )
        episode_path = self.write_csv(
            "content_retail_completeness_quest_episode_evidence.csv",
            [self.valid_row(quest_id="1", episode_quest_id="3000")],
        )
        public_event_path = self.write_csv(
            "content_retail_completeness_public_event_evidence.csv",
            [self.valid_row(public_event_id="10", evidence_kind="public_event_creature")],
        )
        quest_objective_path = self.write_csv(
            "content_retail_completeness_quest_objectives.csv",
            [self.valid_row(quest_id="1", objective_id="10", objective_type="5")],
        )
        quest_script_path = self.write_csv(
            "content_retail_completeness_quest_scripts.csv",
            [self.valid_row(quest_id="1", script_kind="quest_script", script_class="FocusedQuest")],
        )
        portal_path = self.write_csv(
            "content_retail_completeness_instance_portal_evidence.csv",
            [self.valid_row(instance_portal_id="41", evidence_kind="instance_portal_creature")],
        )
        self.write_tracker(
            blocker_path.name,
            source_path.name,
            episode_path.name,
            public_event_path.name,
            quest_objective_path.name,
            quest_script_path.name,
            portal_path.name,
        )

        summary, errors = self.validate_outputs(
            [
                blocker_path.name,
                source_path.name,
                episode_path.name,
                public_event_path.name,
                quest_objective_path.name,
                quest_script_path.name,
                portal_path.name,
            ]
        )

        self.assertEqual([], errors)
        self.assertEqual(12, summary.row_count)

    def test_rejects_invalid_next_slice_blocker_metadata(self) -> None:
        blocker_path = self.write_csv(
            "content_retail_completeness_next_slice_blockers.csv",
            [
                self.valid_blocker_row(
                    queue_rank="first",
                    slice_id="",
                    lane="unknown",
                    priority="P9",
                    blocker_rank="one",
                    blocker_id="",
                    blocker_category="",
                    blocker_type="",
                    source_inventory="content_retail_completeness_next_slice_queue.csv",
                    source_row_key="",
                    next_action="",
                    tracker_paths="",
                ),
                self.valid_blocker_row(
                    queue_rank="1",
                    blocker_rank="1",
                    blocker_id="quest-1:quest_objective_evidence:quest_id=1;jabbithole_quest_objective_id=10",
                    source_inventory="content_retail_completeness_missing.csv",
                ),
                self.valid_blocker_row(
                    queue_rank="1",
                    blocker_rank="2",
                    blocker_id="quest-1:quest_objective_evidence:quest_id=1;jabbithole_quest_objective_id=10",
                    source_inventory="content_retail_completeness_quest_objective_evidence.csv",
                ),
                self.valid_blocker_row(
                    queue_rank="1",
                    blocker_rank="3",
                    blocker_id="quest-1:script_event_flwo:source_path=FocusedEventScript.cs",
                    blocker_category="script_event_flwo",
                    source_inventory="content_retail_completeness_quest_objective_evidence.csv",
                ),
                self.valid_blocker_row(
                    queue_rank="1",
                    blocker_rank="4",
                    blocker_id="quest-1:script_event_flow:source_path=FocusedEventScript.cs",
                    blocker_category="script_event_flow",
                    source_inventory="content_retail_completeness_quest_objective_evidence.csv",
                ),
                self.valid_blocker_row(
                    queue_rank="1",
                    blocker_rank="5",
                    blocker_id="quest-1:script_quest_progression:source_path=FocusedQuest.cs",
                    blocker_category="script_quest_progression",
                    source_inventory="content_retail_completeness_quest_objective_evidence.csv",
                ),
                self.valid_blocker_row(
                    queue_rank="1",
                    blocker_rank="6",
                    blocker_id="quest-1:quest_reward:reward_id=300",
                    blocker_category="quest_reward",
                    source_inventory="content_retail_completeness_quest_objective_evidence.csv",
                ),
                self.valid_blocker_row(
                    queue_rank="1",
                    blocker_rank="7",
                    blocker_id="quest-1:quest_loot:loot_group_id=1200000001",
                    blocker_category="quest_loot",
                    source_inventory="content_retail_completeness_quest_objective_evidence.csv",
                ),
                self.valid_blocker_row(
                    queue_rank="1",
                    blocker_rank="8",
                    blocker_id="quest-1:quest_world_dependency:dependency_id=418",
                    blocker_category="quest_world_dependency",
                    source_inventory="content_retail_completeness_quest_objective_evidence.csv",
                ),
                self.valid_blocker_row(
                    queue_rank="1",
                    blocker_rank="9",
                    blocker_id="quest-1:quest_achievement:achievement_id=300",
                    blocker_category="quest_achievement",
                    source_inventory="content_retail_completeness_quest_objective_evidence.csv",
                ),
                self.valid_blocker_row(
                    queue_rank="1",
                    blocker_rank="10",
                    blocker_id="quest-1:quest_prerequisite:dependency_type=level",
                    blocker_category="quest_prerequisite",
                    source_inventory="content_retail_completeness_quest_objective_evidence.csv",
                ),
                self.valid_blocker_row(
                    queue_rank="1",
                    blocker_rank="11",
                    blocker_id="quest-1:quest_episode_evidence:episode_quest_id=3000",
                    blocker_category="quest_episode_evidence",
                    source_inventory="content_retail_completeness_quest_objective_evidence.csv",
                ),
                self.valid_blocker_row(
                    queue_rank="1",
                    blocker_rank="12",
                    blocker_id="quest-1:public_event_evidence:public_event_id=10",
                    blocker_category="public_event_evidence",
                    source_inventory="content_retail_completeness_quest_objective_evidence.csv",
                ),
                self.valid_blocker_row(
                    queue_rank="1",
                    blocker_rank="13",
                    blocker_id="quest-1:quest_objective:quest_id=1;objective_id=10",
                    blocker_category="quest_objective",
                    source_inventory="content_retail_completeness_quest_objective_evidence.csv",
                ),
                self.valid_blocker_row(
                    queue_rank="1",
                    blocker_rank="14",
                    blocker_id="quest-1:quest_script:quest_id=1;script_kind=quest_script",
                    blocker_category="quest_script",
                    source_inventory="content_retail_completeness_quest_objective_evidence.csv",
                ),
                self.valid_blocker_row(
                    queue_rank="1",
                    blocker_rank="15",
                    blocker_id="instance-10:instance_portal_evidence:instance_portal_id=41;world_id=10",
                    blocker_category="instance_portal_evidence",
                    source_inventory="content_retail_completeness_quest_objective_evidence.csv",
                ),
            ],
        )
        source_path = self.write_csv("content_retail_completeness_quest_objective_evidence.csv", [self.valid_row()])
        self.write_tracker(blocker_path.name, source_path.name)

        _, errors = self.validate_outputs([blocker_path.name, source_path.name])

        self.assertTrue(any("blank blocker_id" in error for error in errors))
        self.assertTrue(any("queue_rank `first` is not an integer" in error for error in errors))
        self.assertTrue(any("blocker_rank `one` is not an integer" in error for error in errors))
        self.assertTrue(any("unsupported lane" in error for error in errors))
        self.assertTrue(any("unsupported priority" in error for error in errors))
        self.assertTrue(any("not a generated blocker source inventory" in error for error in errors))
        self.assertTrue(any("blank blocker column `slice_id`" in error for error in errors))
        self.assertTrue(any("blank blocker column `blocker_category`" in error for error in errors))
        self.assertTrue(any("blank blocker column `blocker_type`" in error for error in errors))
        self.assertTrue(any("blank blocker column `source_row_key`" in error for error in errors))
        self.assertTrue(any("blank blocker column `next_action`" in error for error in errors))
        self.assertTrue(any("blank blocker column `tracker_paths`" in error for error in errors))
        self.assertTrue(any("duplicate blocker_id" in error for error in errors))
        self.assertTrue(any("unsupported blocker_category `script_event_flwo`" in error for error in errors))
        self.assertTrue(any("must use source_inventory `content_retail_completeness_script_event_flow_evidence.csv`" in error for error in errors))
        self.assertTrue(any("must use source_inventory `content_retail_completeness_script_quest_progression_evidence.csv`" in error for error in errors))
        self.assertTrue(any("must use source_inventory `content_retail_completeness_quest_rewards.csv`" in error for error in errors))
        self.assertTrue(any("must use source_inventory `content_retail_completeness_quest_loot.csv`" in error for error in errors))
        self.assertTrue(any("must use source_inventory `content_retail_completeness_quest_world_dependencies.csv`" in error for error in errors))
        self.assertTrue(any("must use source_inventory `content_retail_completeness_quest_achievements.csv`" in error for error in errors))
        self.assertTrue(any("must use source_inventory `content_retail_completeness_quest_prerequisites.csv`" in error for error in errors))
        self.assertTrue(any("must use source_inventory `content_retail_completeness_quest_episode_evidence.csv`" in error for error in errors))
        self.assertTrue(any("must use source_inventory `content_retail_completeness_public_event_evidence.csv`" in error for error in errors))
        self.assertTrue(any("must use source_inventory `content_retail_completeness_quest_objectives.csv`" in error for error in errors))
        self.assertTrue(any("must use source_inventory `content_retail_completeness_quest_scripts.csv`" in error for error in errors))
        self.assertTrue(any("must use source_inventory `content_retail_completeness_instance_portal_evidence.csv`" in error for error in errors))

    def test_rejects_invalid_next_slice_queue_metadata(self) -> None:
        csv_path = self.write_csv(
            "content_retail_completeness_next_slice_queue.csv",
            [
                self.valid_queue_row(
                    queue_rank="1",
                    slice_id="quest-1",
                    priority="P9",
                    source_inventory="",
                    source_row_key="",
                    validation_bundle_name="",
                    narrow_test_filter="FullyQualifiedName~Q1",
                    narrow_test_command='dotnet test Source\\NexusForever.Game.Tests\\NexusForever.Game.Tests.csproj -v minimal --nologo --filter "FullyQualifiedName~Q2"',
                    harness_command='.\\Decomp\\Analysis\\Start-BlockerEvidenceHarness.ps1 -BundleName "wrong-bundle" -ClientDirectory "I:\\WildStar" -WorldIds 426 -ObjectiveIds 1 -PromptForRootPassword',
                    actionability_state="unsupported_state",
                    split_required="maybe",
                    retail_claim_allowed="true",
                    required_evidence_level="Observed",
                    blocking_evidence_source="native_dump",
                ),
                self.valid_queue_row(
                    queue_rank="1",
                    slice_id="quest-1",
                    lane="unknown",
                    source_inventory="content_retail_completeness_missing.csv",
                    source_row_key="quest_id=2",
                    harness_command='Write-Host "missing harness"',
                    actionability_state="quest_validation_ready",
                    split_required="true",
                    split_reason="",
                ),
                self.valid_queue_row(
                    queue_rank="3",
                    slice_id="quest-3",
                    split_required="false",
                    split_reason="blocker_count_gt_100",
                ),
            ],
        )
        self.write_tracker(csv_path.name)

        _, errors = self.validate_outputs()

        self.assertTrue(any("unsupported priority" in error for error in errors))
        self.assertTrue(any("unsupported actionability_state `unsupported_state`" in error for error in errors))
        self.assertTrue(any("split_required `maybe` must be true or false" in error for error in errors))
        self.assertTrue(any("split_required true needs split_reason" in error for error in errors))
        self.assertTrue(any("split_required true must use a split_required actionability_state" in error for error in errors))
        self.assertTrue(any("split_required false must have blank split_reason" in error for error in errors))
        self.assertTrue(any("retail_claim_allowed must stay false" in error for error in errors))
        self.assertTrue(any("unsupported required_evidence_level `Observed`" in error for error in errors))
        self.assertTrue(any("unsupported blocking_evidence_source `native_dump`" in error for error in errors))
        self.assertTrue(any("blank source_inventory" in error for error in errors))
        self.assertTrue(any("blank source_row_key" in error for error in errors))
        self.assertTrue(any("duplicate slice_id" in error for error in errors))
        self.assertTrue(any("unsupported lane" in error for error in errors))
        self.assertTrue(any("not a generated source inventory" in error for error in errors))
        self.assertTrue(any("blank queue command column `validation_bundle_name`" in error for error in errors))
        self.assertTrue(any("narrow_test_command does not include narrow_test_filter" in error for error in errors))
        self.assertTrue(any("harness_command does not include validation_bundle_name" in error for error in errors))
        self.assertTrue(any("harness_command does not invoke Start-BlockerEvidenceHarness.ps1" in error for error in errors))
        self.assertTrue(any("queue_rank values must be unique and contiguous" in error for error in errors))


if __name__ == "__main__":
    unittest.main()
