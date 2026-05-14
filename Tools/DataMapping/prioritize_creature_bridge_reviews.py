#!/usr/bin/env python3
"""
Prioritize uncertain creature bridge review rows.

This reads `creature_bridge_review.csv` and downstream relation maps, then emits
review packs sorted by gameplay impact. When a world database is available, it
also compares Jabbithole spawn coordinates against existing NexusForever entity
coordinates for each candidate Creature2 id.
"""

from __future__ import annotations

import argparse
import csv
import json
import math
import subprocess
import sys
import time
from collections import Counter, defaultdict
from dataclasses import dataclass
from pathlib import Path
from typing import DefaultDict, Dict, Iterable, Optional, Sequence, Tuple


DEFAULT_MYSQL = r"C:\Program Files\MySQL\MySQL Server 8.0\bin\mysql.exe"

RELATION_SPECS = [
    ("spawn_rows", "creature_spawn_map.csv", "jabbithole_creature_id", 4),
    ("loot_rows", "creature_loot_map.csv", "jabbithole_creature_id", 8),
    ("vendor_rows", "vendor_item_map.csv", "jabbithole_creature_id", 30),
    ("spell_rows", "creature_spell_map.csv", "jabbithole_creature_id", 10),
    ("quest_rows", "creature_quest_map.csv", "jabbithole_creature_id", 25),
    ("path_mission_rows", "creature_path_mission_map.csv", "jabbithole_creature_id", 18),
    ("public_event_rows", "creature_public_event_map.csv", "jabbithole_creature_id", 18),
    ("challenge_rows", "challenge_creature_map.csv", "jabbithole_creature_id", 16),
    ("contract_rows", "contract_creature_map.csv", "jabbithole_creature_id", 16),
    ("spline_candidate_rows", "creature_spline_candidate_map.csv", "jabbithole_creature_id", 3),
]

SUMMARY_FIELDS = [
    "source_id",
    "source_name",
    "current_status",
    "current_creature2_id",
    "current_client_name",
    "candidate_count",
    "exact_candidate_count",
    "fuzzy_candidate_count",
    "review_priority_score",
    "total_relation_rows",
    "spawn_rows",
    "loot_rows",
    "vendor_rows",
    "spell_rows",
    "quest_rows",
    "path_mission_rows",
    "public_event_rows",
    "challenge_rows",
    "contract_rows",
    "spline_candidate_rows",
    "recommended_creature2_id",
    "recommended_client_name",
    "recommendation_kind",
    "recommendation_confidence",
    "recommendation_reason",
    "recommended_nearest_entity_distance",
    "recommended_spatial_match_count",
    "recommended_entity_match_count",
    "recommended_candidate_rank",
    "recommended_candidate_score",
    "recommended_name_similarity",
    "recommended_faction_match",
    "recommended_level_overlap",
    "recommended_exact_level_match",
    "recommended_difficulty_match",
    "recommended_datacube_match",
]

SUGGESTION_FIELDS = [
    "source_table",
    "source_id",
    "source_name",
    "chosen_creature2_id",
    "decision",
    "reason",
    "reviewer",
    "reviewed_at",
    "recommendation_confidence",
    "recommendation_kind",
    "review_priority_score",
    "nearest_entity_distance",
    "spatial_match_count",
    "entity_match_count",
]


@dataclass(frozen=True)
class EntityPoint:
    entity_id: int
    creature2_id: int
    world: int
    x: float
    y: float
    z: float


@dataclass(frozen=True)
class SpawnPoint:
    source_id: int
    world: int
    x: float
    y: float
    z: float


def to_int(value, default: Optional[int] = None) -> Optional[int]:
    if value is None:
        return default
    text = str(value).strip()
    if not text:
        return default
    try:
        return int(float(text))
    except ValueError:
        return default


def to_float(value, default: Optional[float] = None) -> Optional[float]:
    if value is None:
        return default
    text = str(value).strip()
    if not text:
        return default
    try:
        return float(text)
    except ValueError:
        return default


def sql_string(value: str) -> str:
    return "'" + str(value).replace("\\", "\\\\").replace("'", "''") + "'"


def mysql_command(args: argparse.Namespace, extra: Optional[Sequence[str]] = None) -> list[str]:
    command = [
        str(args.mysql_exe),
        f"--host={args.host}",
        f"--port={args.port}",
        f"--user={args.user}",
        f"--password={args.password}",
        "--default-character-set=utf8mb4",
        "--batch",
        "--raw",
        "--skip-column-names",
    ]
    if extra:
        command.extend(extra)
    return command


def mysql_rows(args: argparse.Namespace, database: str, query: str) -> list[list[str]]:
    result = subprocess.run(
        mysql_command(args, [database, "-e", query]),
        text=True,
        encoding="utf-8",
        errors="replace",
        stdout=subprocess.PIPE,
        stderr=subprocess.PIPE,
        check=False,
    )
    if result.returncode != 0:
        raise RuntimeError(result.stderr.strip() or f"mysql exited with {result.returncode}")
    return [line.split("\t") for line in result.stdout.splitlines() if line]


def load_csv(path: Path) -> list[dict[str, str]]:
    with path.open("r", encoding="utf-8", newline="") as handle:
        return list(csv.DictReader(handle))


def write_csv(path: Path, fieldnames: Sequence[str], rows: Iterable[dict[str, object]]) -> int:
    path.parent.mkdir(parents=True, exist_ok=True)
    count = 0
    with path.open("w", encoding="utf-8", newline="") as handle:
        writer = csv.DictWriter(handle, fieldnames=fieldnames, extrasaction="ignore")
        writer.writeheader()
        for row in rows:
            writer.writerow(row)
            count += 1
    return count


def count_by_source(path: Path, source_column: str, allowed_source_ids: set[int]) -> Counter[int]:
    counts: Counter[int] = Counter()
    if not path.exists():
        return counts
    with path.open("r", encoding="utf-8", newline="") as handle:
        reader = csv.DictReader(handle)
        for row in reader:
            source_id = to_int(row.get(source_column))
            if source_id in allowed_source_ids:
                counts[source_id] += 1
    return counts


def load_relation_counts(output_dir: Path, source_ids: set[int]) -> tuple[dict[str, Counter[int]], dict[str, int]]:
    relation_counts: dict[str, Counter[int]] = {}
    weights: dict[str, int] = {}
    for output_name, filename, source_column, weight in RELATION_SPECS:
        relation_counts[output_name] = count_by_source(output_dir / filename, source_column, source_ids)
        weights[output_name] = weight
    return relation_counts, weights


def load_spawns(output_dir: Path, source_ids: set[int]) -> dict[int, list[SpawnPoint]]:
    path = output_dir / "creature_spawn_map.csv"
    spawns: DefaultDict[int, list[SpawnPoint]] = defaultdict(list)
    if not path.exists():
        return {}
    with path.open("r", encoding="utf-8", newline="") as handle:
        reader = csv.DictReader(handle)
        for row in reader:
            source_id = to_int(row.get("jabbithole_creature_id"))
            if source_id not in source_ids:
                continue
            world = to_int(row.get("worldid"))
            x = to_float(row.get("x"))
            y = to_float(row.get("y"))
            z = to_float(row.get("z"))
            if world is None or x is None or y is None or z is None:
                continue
            spawns[source_id].append(SpawnPoint(source_id=source_id, world=world, x=x, y=y, z=z))
    return dict(spawns)


def load_entity_points(args: argparse.Namespace, candidate_ids: set[int]) -> dict[tuple[int, int], list[EntityPoint]]:
    if args.no_world_entity_evidence or not candidate_ids:
        return {}
    chunks = []
    sorted_ids = sorted(candidate_ids)
    for index in range(0, len(sorted_ids), 1000):
        chunks.append(",".join(str(value) for value in sorted_ids[index : index + 1000]))

    entities: DefaultDict[tuple[int, int], list[EntityPoint]] = defaultdict(list)
    for chunk in chunks:
        rows = mysql_rows(
            args,
            args.world_db,
            (
                "SELECT id, creature, world, x, y, z "
                "FROM entity "
                f"WHERE creature IN ({chunk}) AND world <> 0;"
            ),
        )
        for row in rows:
            entity_id = to_int(row[0], 0) or 0
            creature2_id = to_int(row[1], 0) or 0
            world = to_int(row[2], 0) or 0
            x = to_float(row[3], 0.0) or 0.0
            y = to_float(row[4], 0.0) or 0.0
            z = to_float(row[5], 0.0) or 0.0
            entities[(world, creature2_id)].append(
                EntityPoint(entity_id=entity_id, creature2_id=creature2_id, world=world, x=x, y=y, z=z)
            )
    return dict(entities)


def build_entity_grid(
    entities: dict[tuple[int, int], list[EntityPoint]],
    cell_size: float,
) -> dict[tuple[int, int], dict[tuple[int, int], list[EntityPoint]]]:
    grids: dict[tuple[int, int], dict[tuple[int, int], list[EntityPoint]]] = {}
    for key, points in entities.items():
        grid: DefaultDict[tuple[int, int], list[EntityPoint]] = defaultdict(list)
        for point in points:
            grid[(math.floor(point.x / cell_size), math.floor(point.z / cell_size))].append(point)
        grids[key] = dict(grid)
    return grids


def spatial_evidence(
    spawns: Sequence[SpawnPoint],
    candidate_id: int,
    grids: dict[tuple[int, int], dict[tuple[int, int], list[EntityPoint]]],
    radius: float,
    cell_size: float,
) -> dict[str, object]:
    nearest_distance: Optional[float] = None
    nearest_entity_id = ""
    spatial_match_count = 0
    entity_match_ids: set[int] = set()
    search_cells = max(1, math.ceil(radius / cell_size))

    for spawn in spawns:
        grid = grids.get((spawn.world, candidate_id))
        if not grid:
            continue
        cx = math.floor(spawn.x / cell_size)
        cz = math.floor(spawn.z / cell_size)
        spawn_matched = False
        for dx in range(-search_cells, search_cells + 1):
            for dz in range(-search_cells, search_cells + 1):
                for point in grid.get((cx + dx, cz + dz), []):
                    distance = math.sqrt((spawn.x - point.x) ** 2 + (spawn.y - point.y) ** 2 + (spawn.z - point.z) ** 2)
                    if nearest_distance is None or distance < nearest_distance:
                        nearest_distance = distance
                        nearest_entity_id = point.entity_id
                    if distance <= radius:
                        spawn_matched = True
                        entity_match_ids.add(point.entity_id)
        if spawn_matched:
            spatial_match_count += 1

    return {
        "nearest_entity_distance": "" if nearest_distance is None else f"{nearest_distance:.3f}",
        "nearest_entity_id": nearest_entity_id,
        "spatial_match_count": spatial_match_count,
        "entity_match_count": len(entity_match_ids),
    }


def truthy(value) -> bool:
    return str(value).strip().lower() in {"true", "1", "yes", "y"}


def recommendation_for_group(candidates: list[dict[str, object]], radius: float) -> dict[str, object]:
    spatial_candidates = [
        row
        for row in candidates
        if to_int(row.get("spatial_match_count"), 0)
    ]
    if spatial_candidates:
        spatial_candidates.sort(
            key=lambda row: (
                -(to_int(row.get("spatial_match_count"), 0) or 0),
                to_float(row.get("nearest_entity_distance"), 999999.0) or 999999.0,
                to_int(row.get("candidate_rank"), 999999) or 999999,
            )
        )
        best = spatial_candidates[0]
        second = spatial_candidates[1] if len(spatial_candidates) > 1 else None
        nearest = to_float(best.get("nearest_entity_distance"), 999999.0) or 999999.0
        confidence = "high" if second is None and nearest <= radius else "medium"
        reason = "candidate has existing entity near Jabbithole spawn"
        if second is None:
            reason = "only candidate with existing entity near Jabbithole spawn"
        return {
            **best,
            "recommendation_kind": "existing_entity_spatial",
            "recommendation_confidence": confidence,
            "recommendation_reason": reason,
        }

    sorted_candidates = sorted(
        candidates,
        key=lambda row: (
            -(to_float(row.get("name_similarity"), 0.0) or 0.0),
            to_int(row.get("candidate_rank"), 999999) or 999999,
        ),
    )
    if not sorted_candidates:
        return {}

    best = sorted_candidates[0]
    status = str(best.get("current_status", ""))
    similarity = to_float(best.get("name_similarity"), 0.0) or 0.0
    score_delta_next = None
    if len(sorted_candidates) > 1:
        score_delta_next = (to_int(best.get("candidate_score"), 0) or 0) - (to_int(sorted_candidates[1].get("candidate_score"), 0) or 0)

    if status == "unmatched" and similarity >= 0.94 and (score_delta_next is None or score_delta_next >= 8):
        return {
            **best,
            "recommendation_kind": "strong_fuzzy_name",
            "recommendation_confidence": "medium",
            "recommendation_reason": "high name similarity and score gap, but no spatial entity evidence",
        }

    return {
        **best,
        "recommendation_kind": "",
        "recommendation_confidence": "",
        "recommendation_reason": "",
    }


def build_reports(args: argparse.Namespace) -> tuple[list[dict[str, object]], list[dict[str, object]], list[dict[str, object]], dict[str, object]]:
    review_path = args.output_dir / "creature_bridge_review.csv"
    if not review_path.exists():
        raise FileNotFoundError(review_path)

    review_rows = load_csv(review_path)
    source_ids = {to_int(row.get("source_id")) for row in review_rows}
    source_ids = {value for value in source_ids if value is not None}
    candidate_ids = {to_int(row.get("candidate_creature2_id")) for row in review_rows}
    candidate_ids = {value for value in candidate_ids if value is not None}

    relation_counts, weights = load_relation_counts(args.output_dir, source_ids)
    spawns = load_spawns(args.output_dir, source_ids)
    entities = load_entity_points(args, candidate_ids)
    grids = build_entity_grid(entities, args.entity_grid_cell_size)

    candidate_rows: list[dict[str, object]] = []
    by_source: DefaultDict[int, list[dict[str, object]]] = defaultdict(list)
    for row in review_rows:
        source_id = to_int(row.get("source_id"))
        candidate_id = to_int(row.get("candidate_creature2_id"))
        if source_id is None:
            continue
        enriched: dict[str, object] = dict(row)
        for output_name, counts in relation_counts.items():
            enriched[output_name] = counts[source_id]
        total_relation_rows = sum(relation_counts[name][source_id] for name in relation_counts)
        priority_score = sum(relation_counts[name][source_id] * weights[name] for name in relation_counts)
        enriched["total_relation_rows"] = total_relation_rows
        enriched["review_priority_score"] = priority_score
        if candidate_id is not None:
            enriched.update(
                spatial_evidence(
                    spawns.get(source_id, []),
                    candidate_id,
                    grids,
                    args.entity_match_radius,
                    args.entity_grid_cell_size,
                )
            )
        else:
            enriched.update(
                {
                    "nearest_entity_distance": "",
                    "nearest_entity_id": "",
                    "spatial_match_count": 0,
                    "entity_match_count": 0,
                }
            )
        candidate_rows.append(enriched)
        by_source[source_id].append(enriched)

    summary_rows: list[dict[str, object]] = []
    suggestion_rows: list[dict[str, object]] = []
    for source_id, rows in by_source.items():
        first = rows[0]
        recommended = recommendation_for_group(rows, args.entity_match_radius)
        exact_count = sum(1 for row in rows if row.get("candidate_reason") == "exact_name")
        fuzzy_count = sum(1 for row in rows if row.get("candidate_reason") == "fuzzy_name")
        summary = {
            "source_id": source_id,
            "source_name": first.get("source_name", ""),
            "current_status": first.get("current_status", ""),
            "current_creature2_id": first.get("current_creature2_id", ""),
            "current_client_name": first.get("current_client_name", ""),
            "candidate_count": len(rows),
            "exact_candidate_count": exact_count,
            "fuzzy_candidate_count": fuzzy_count,
            "review_priority_score": first.get("review_priority_score", 0),
            "total_relation_rows": first.get("total_relation_rows", 0),
        }
        for output_name, _, _, _ in RELATION_SPECS:
            summary[output_name] = first.get(output_name, 0)
        summary.update(
            {
                "recommended_creature2_id": recommended.get("candidate_creature2_id", ""),
                "recommended_client_name": recommended.get("candidate_client_name", ""),
                "recommendation_kind": recommended.get("recommendation_kind", ""),
                "recommendation_confidence": recommended.get("recommendation_confidence", ""),
                "recommendation_reason": recommended.get("recommendation_reason", ""),
                "recommended_nearest_entity_distance": recommended.get("nearest_entity_distance", ""),
                "recommended_spatial_match_count": recommended.get("spatial_match_count", ""),
                "recommended_entity_match_count": recommended.get("entity_match_count", ""),
                "recommended_candidate_rank": recommended.get("candidate_rank", ""),
                "recommended_candidate_score": recommended.get("candidate_score", ""),
                "recommended_name_similarity": recommended.get("name_similarity", ""),
                "recommended_faction_match": recommended.get("faction_match", ""),
                "recommended_level_overlap": recommended.get("level_overlap", ""),
                "recommended_exact_level_match": recommended.get("exact_level_match", ""),
                "recommended_difficulty_match": recommended.get("difficulty_match", ""),
                "recommended_datacube_match": recommended.get("datacube_match", ""),
            }
        )
        summary_rows.append(summary)

        if summary["recommendation_kind"]:
            suggestion_rows.append(
                {
                    "source_table": "creatures",
                    "source_id": source_id,
                    "source_name": first.get("source_name", ""),
                    "chosen_creature2_id": recommended.get("candidate_creature2_id", ""),
                    "decision": "suggested",
                    "reason": recommended.get("recommendation_reason", ""),
                    "reviewer": "prioritize_creature_bridge_reviews.py",
                    "reviewed_at": time.strftime("%Y-%m-%d"),
                    "recommendation_confidence": recommended.get("recommendation_confidence", ""),
                    "recommendation_kind": recommended.get("recommendation_kind", ""),
                    "review_priority_score": first.get("review_priority_score", 0),
                    "nearest_entity_distance": recommended.get("nearest_entity_distance", ""),
                    "spatial_match_count": recommended.get("spatial_match_count", 0),
                    "entity_match_count": recommended.get("entity_match_count", 0),
                }
            )

    summary_rows.sort(
        key=lambda row: (
            -to_int(row.get("review_priority_score"), 0),
            str(row.get("source_name", "")),
            to_int(row.get("source_id"), 0) or 0,
        )
    )
    candidate_rows.sort(
        key=lambda row: (
            -to_int(row.get("review_priority_score"), 0),
            to_int(row.get("source_id"), 0) or 0,
            to_int(row.get("candidate_rank"), 999999) or 999999,
        )
    )
    suggestion_rows.sort(
        key=lambda row: (
            {"high": 0, "medium": 1, "low": 2}.get(str(row.get("recommendation_confidence")), 99),
            -to_int(row.get("review_priority_score"), 0),
        )
    )

    recommendation_counts = Counter(str(row.get("recommendation_kind", "")) or "none" for row in summary_rows)
    confidence_counts = Counter(str(row.get("recommendation_confidence", "")) or "none" for row in summary_rows)
    report = {
        "generated_at": time.strftime("%Y-%m-%d %H:%M:%S"),
        "review_rows": len(review_rows),
        "review_sources": len(by_source),
        "candidate_ids": len(candidate_ids),
        "world_entity_evidence_enabled": not args.no_world_entity_evidence,
        "world_entity_candidate_rows": sum(len(values) for values in entities.values()),
        "entity_match_radius": args.entity_match_radius,
        "recommendation_counts": dict(recommendation_counts),
        "confidence_counts": dict(confidence_counts),
        "suggestions": len(suggestion_rows),
    }
    return summary_rows, candidate_rows, suggestion_rows, report


def parse_args(argv: Optional[Sequence[str]] = None) -> argparse.Namespace:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--repo-root", type=Path, default=Path.cwd())
    parser.add_argument("--output-dir", type=Path)
    parser.add_argument("--report-out", type=Path)
    parser.add_argument("--priority-out", type=Path)
    parser.add_argument("--candidate-out", type=Path)
    parser.add_argument("--suggestion-out", type=Path)
    parser.add_argument("--mysql-exe", type=Path, default=Path(DEFAULT_MYSQL))
    parser.add_argument("--host", default="127.0.0.1")
    parser.add_argument("--port", type=int, default=3306)
    parser.add_argument("--user", default="bankai")
    parser.add_argument("--password", default="bankai")
    parser.add_argument("--world-db", default="nexus_forever_world")
    parser.add_argument("--no-world-entity-evidence", action="store_true")
    parser.add_argument("--entity-match-radius", type=float, default=30.0)
    parser.add_argument("--entity-grid-cell-size", type=float, default=50.0)
    args = parser.parse_args(argv)
    args.repo_root = args.repo_root.resolve()
    args.output_dir = (args.output_dir or args.repo_root / "Tools" / "DataMapping" / "output").resolve()
    args.priority_out = (args.priority_out or args.output_dir / "creature_bridge_review_priority.csv").resolve()
    args.candidate_out = (args.candidate_out or args.output_dir / "creature_bridge_review_candidates_ranked.csv").resolve()
    args.suggestion_out = (args.suggestion_out or args.output_dir / "creature_bridge_override_suggestions.csv").resolve()
    args.report_out = (args.report_out or args.output_dir / "creature_bridge_review_priority_report.json").resolve()
    return args


def main(argv: Optional[Sequence[str]] = None) -> int:
    args = parse_args(argv)
    if not args.output_dir.exists():
        raise FileNotFoundError(args.output_dir)
    if not args.no_world_entity_evidence and not args.mysql_exe.exists():
        raise FileNotFoundError(args.mysql_exe)

    summary_rows, candidate_rows, suggestion_rows, report = build_reports(args)
    write_csv(args.priority_out, SUMMARY_FIELDS, summary_rows)

    candidate_fields = list(load_csv(args.output_dir / "creature_bridge_review.csv")[0].keys()) + [
        "review_priority_score",
        "total_relation_rows",
        "spawn_rows",
        "loot_rows",
        "vendor_rows",
        "spell_rows",
        "quest_rows",
        "path_mission_rows",
        "public_event_rows",
        "challenge_rows",
        "contract_rows",
        "spline_candidate_rows",
        "nearest_entity_distance",
        "nearest_entity_id",
        "spatial_match_count",
        "entity_match_count",
    ]
    write_csv(args.candidate_out, candidate_fields, candidate_rows)
    write_csv(args.suggestion_out, SUGGESTION_FIELDS, suggestion_rows)
    args.report_out.write_text(json.dumps(report, indent=2), encoding="utf-8")
    print(json.dumps(report, indent=2))
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
