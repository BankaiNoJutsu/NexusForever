#!/usr/bin/env python3
"""Extract WIP quest-instance entity rows from LaughingWS SQL."""

from __future__ import annotations

import argparse
import re
from dataclasses import dataclass
from pathlib import Path
from typing import Iterable

from analyze_laughingws_worlddb import (
    normalise_column,
    read_text,
    split_top_level_commas,
    split_value_rows,
)


SET_WORLD_RE = re.compile(r"\bSET\s+@WORLD\s*:?=\s*(\d+)\s*;", re.IGNORECASE)
INSERT_ENTITY_RE = re.compile(
    r"\bINSERT\s+INTO\s+`?entity`?\s*\((?P<columns>.*?)\)\s*"
    r"VALUE(?:S)?\s*(?P<values>.*?);",
    re.IGNORECASE | re.DOTALL,
)

SOURCE_FILE = Path("Map") / "Open World" / "The Dust Stalker.sql"
BASE_QUEST_INSTANCE_ENTITY_ID = 1_100_100_000

OUTPUT_COLUMNS = (
    "id",
    "type",
    "creature",
    "world",
    "area",
    "x",
    "y",
    "z",
    "rx",
    "ry",
    "rz",
    "displayInfo",
    "outfitInfo",
    "faction1",
    "faction2",
    "questChecklistIdx",
    "activePropId",
    "worldSocketId",
    "mode",
)

COLUMN_NAME_MAP = {
    "activepropid": "activePropId",
    "area": "area",
    "creature": "creature",
    "displayinfo": "displayInfo",
    "faction1": "faction1",
    "faction2": "faction2",
    "id": "id",
    "mode": "mode",
    "outfitinfo": "outfitInfo",
    "questchecklistidx": "questChecklistIdx",
    "rx": "rx",
    "ry": "ry",
    "rz": "rz",
    "type": "type",
    "world": "world",
    "worldsocketid": "worldSocketId",
    "x": "x",
    "y": "y",
    "z": "z",
}

# WIP/GUESSED: The branch's bridge-control row uses entity type 20, which is
# Player in the current NexusForever enum. DataMapping creature/spawn evidence
# maps creature 16733 to SimpleCollidable (32), so the seed emits 32 instead.
ENTITY_VALUE_OVERRIDES: dict[int, dict[str, str]] = {
    16733: {"type": "32"},
}

# WIP/GUESSED: The source SQL sets Boss Xagg health to 1. DataMapping
# creature/spawn evidence for quest 4516 maps Boss Xagg to level 19, health
# 8767, shield 2352, and interrupt armor 0; emit those values instead.
ENTITY_STAT_OVERRIDES: dict[int, tuple[tuple[int, int], ...]] = {
    16707: ((0, 8767), (10, 19), (20, 2352), (21, 0)),
}


@dataclass(frozen=True)
class EntityRow:
    source_index: int
    values: dict[str, str]

    @property
    def creature(self) -> int:
        return int(self.values["creature"], 0)


def default_laughingws_path(repo_root: Path) -> Path:
    return repo_root / "artifacts" / "external" / "LaughingWS.WorldDatabase.New-Zones-and-more"


def normalise_source_column(column: str) -> str:
    name = normalise_column(column)
    return COLUMN_NAME_MAP.get(name.lower(), name)


def resolve_token(token: str, world_id: int | None) -> str:
    value = token.strip()
    if value.upper() == "@WORLD":
        if world_id is None:
            raise ValueError("@WORLD used before a SET @WORLD declaration")
        return str(world_id)
    return value


def parse_world_variable(text: str) -> int | None:
    match = SET_WORLD_RE.search(text)
    if match:
        return int(match.group(1))
    return None


def parse_entity_rows(path: Path) -> list[EntityRow]:
    text = read_text(path)
    world_id = parse_world_variable(text)
    rows: list[EntityRow] = []

    for insert_match in INSERT_ENTITY_RE.finditer(text):
        columns = [
            normalise_source_column(column)
            for column in split_top_level_commas(insert_match.group("columns"))
        ]

        for row_sql in split_value_rows(insert_match.group("values")):
            raw_values = split_top_level_commas(row_sql)
            values = {
                column: resolve_token(raw_values[index], world_id)
                for index, column in enumerate(columns)
                if index < len(raw_values)
            }

            if "creature" not in values or "world" not in values:
                continue

            for column in OUTPUT_COLUMNS:
                values.setdefault(column, "0")

            creature = int(values["creature"], 0)
            for column, value in ENTITY_VALUE_OVERRIDES.get(creature, {}).items():
                values[column] = value

            rows.append(EntityRow(len(rows), values))

    return rows


def sql_identifier(name: str) -> str:
    return "`" + name.replace("`", "``") + "`"


def sql_row(values: Iterable[str]) -> str:
    return "(" + ", ".join(values) + ")"


def duplicate_update_clause(columns: Iterable[str], key_columns: set[str]) -> str:
    assignments = [
        f"{sql_identifier(column)} = VALUES({sql_identifier(column)})"
        for column in columns
        if column not in key_columns
    ]
    return "ON DUPLICATE KEY UPDATE " + ", ".join(assignments) + ";"


def build_entity_values(rows: list[EntityRow]) -> list[list[str]]:
    output_rows: list[list[str]] = []
    for index, row in enumerate(rows):
        entity_id = BASE_QUEST_INSTANCE_ENTITY_ID + index + 1
        output_rows.append([str(entity_id)] + [row.values[column] for column in OUTPUT_COLUMNS[1:]])
    return output_rows


def build_entity_stat_values(rows: list[EntityRow]) -> list[list[str]]:
    output_rows: list[list[str]] = []
    for index, row in enumerate(rows):
        entity_id = BASE_QUEST_INSTANCE_ENTITY_ID + index + 1
        for stat, value in ENTITY_STAT_OVERRIDES.get(row.creature, ()):
            output_rows.append([str(entity_id), str(stat), str(value)])
    return output_rows


def append_insert_block(
    lines: list[str],
    table: str,
    columns: tuple[str, ...],
    values: list[list[str]],
    key_columns: set[str],
) -> None:
    if not values:
        return

    column_sql = ", ".join(sql_identifier(column) for column in columns)
    lines.append(f"-- {table}: {len(values)} row(s)")
    lines.append(f"INSERT INTO {sql_identifier(table)} ({column_sql}) VALUES")
    for index, row in enumerate(values):
        suffix = "," if index + 1 < len(values) else ""
        lines.append(f"  {sql_row(row)}{suffix}")
    lines.append(duplicate_update_clause(columns, key_columns))
    lines.append("")


def write_seed(output_path: Path, rows: list[EntityRow]) -> None:
    output_path.parent.mkdir(parents=True, exist_ok=True)

    lines = [
        "-- Generated by Tools/DataMapping/extract_laughingws_quest_instances.py.",
        "-- Source: LaughingWS Map/Open World/The Dust Stalker.sql.",
        "-- Purpose: preserve the small quest-instance overlay for quest 4516 without importing the source world delete or @GUID allocation.",
        "-- WIP/GUESSED: Boss Xagg and the bridge controls are corroborated by DataMapping quest/objective/spawn evidence, but the exit panel is source-only and marked 'place for real' upstream.",
        "-- WIP/GUESSED: current-schema overrides replace the source bridge-control type 20 with SimpleCollidable type 32 and replace source Boss Xagg health=1 with DataMapping stats.",
        "",
        "USE `nexus_forever_world`;",
        "",
        "START TRANSACTION;",
        "",
    ]

    entity_values = build_entity_values(rows)
    append_insert_block(lines, "entity", OUTPUT_COLUMNS, entity_values, {"id"})

    stat_values = build_entity_stat_values(rows)
    append_insert_block(lines, "entity_stats", ("id", "stat", "value"), stat_values, {"id", "stat"})

    lines.extend([
        "-- Sources:",
        f"--   {SOURCE_FILE.as_posix()}",
        "",
        "COMMIT;",
        "",
    ])

    output_path.write_text("\n".join(lines), encoding="ascii", newline="\n")


def parse_args() -> argparse.Namespace:
    repo_root = Path(__file__).resolve().parents[2]
    parser = argparse.ArgumentParser(
        description="Extract WIP LaughingWS quest-instance entity rows into a safe seed.",
    )
    parser.add_argument(
        "--laughingws-path",
        type=Path,
        default=default_laughingws_path(repo_root),
        help="Path to LaughingWS/NexusForever.WorldDatabase.New-Zones-and-more.",
    )
    parser.add_argument(
        "--output-sql",
        type=Path,
        default=repo_root / "Tools" / "DataMapping" / "sql" / "laughingws_quest_instance_wip_seed.sql",
        help="Output SQL seed path.",
    )
    return parser.parse_args()


def main() -> int:
    args = parse_args()
    source_path = args.laughingws_path.resolve() / SOURCE_FILE

    if not source_path.is_file():
        raise SystemExit(f"LaughingWS quest-instance source does not exist: {source_path}")

    rows = parse_entity_rows(source_path)
    write_seed(args.output_sql, rows)

    print(
        f"Wrote {len(rows)} WIP quest-instance entity row(s) to {args.output_sql.resolve()} "
        f"from {SOURCE_FILE.as_posix()}."
    )
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
