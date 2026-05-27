#!/usr/bin/env python3
"""Extract WIP Skyplot housing vendor rows from LaughingWS SQL."""

from __future__ import annotations

import argparse
import hashlib
import re
import sys
from collections import OrderedDict
from dataclasses import dataclass
from pathlib import Path
from typing import Iterable, Optional

from analyze_laughingws_worlddb import (
    INSERT_RE,
    normalise_column,
    read_text,
    split_top_level_commas,
    split_value_rows,
)


SET_WORLD_RE = re.compile(r"\bSET\s+@WORLD\s*:?=\s*(\d+)\s*;", re.IGNORECASE)
SET_GUID_RE = re.compile(
    r"\bSET\s+@GUID\s*=\s*\(SELECT\s+IFNULL\(MAX\(`?id`?\),\s*0\)\s+FROM\s+`?entity`?\)\s*;",
    re.IGNORECASE,
)
GUID_OFFSET_RE = re.compile(r"^@GUID\s*\+\s*(\d+)$", re.IGNORECASE)

SKYPLOT_SOURCE = Path("Map") / "Instances" / "Housing" / "Skyplot.sql"

# WIP/GUESSED: keep this outside the generic DataMapping/live-event ranges.
BASE_HOUSING_ENTITY_ID = 2_000_000_000
BLOCK_STRIDE = 1_000

HOUSING_TABLE_NAMES = (
    "entity",
    "entity_stats",
    "entity_vendor",
    "entity_vendor_category",
    "entity_vendor_item",
)


@dataclass(frozen=True)
class TableSpec:
    name: str
    columns: tuple[str, ...]
    key_columns: tuple[str, ...]
    id_columns: frozenset[str] = frozenset(("id",))


TABLE_SPECS: OrderedDict[str, TableSpec] = OrderedDict(
    (
        (
            "entity",
            TableSpec(
                "entity",
                (
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
                ),
                ("id",),
            ),
        ),
        (
            "entity_stats",
            TableSpec(
                "entity_stats",
                ("id", "stat", "value"),
                ("id", "stat"),
            ),
        ),
        (
            "entity_vendor",
            TableSpec(
                "entity_vendor",
                ("id", "buyPriceMultiplier", "sellPriceMultiplier"),
                ("id",),
            ),
        ),
        (
            "entity_vendor_category",
            TableSpec(
                "entity_vendor_category",
                ("id", "index", "localisedTextId"),
                ("id", "index"),
            ),
        ),
        (
            "entity_vendor_item",
            TableSpec(
                "entity_vendor_item",
                (
                    "id",
                    "index",
                    "categoryIndex",
                    "itemId",
                    "extraCost1ItemOrCurrencyId",
                    "extraCost1Quantity",
                    "extraCost1Type",
                    "extraCost2ItemOrCurrencyId",
                    "extraCost2Quantity",
                    "extraCost2Type",
                ),
                ("id", "index"),
            ),
        ),
    )
)

COLUMN_NAME_MAP = {
    "activepropid": "activePropId",
    "area": "area",
    "buypricemultiplier": "buyPriceMultiplier",
    "categoryindex": "categoryIndex",
    "creature": "creature",
    "displayinfo": "displayInfo",
    "extracost1itemorcurrencyid": "extraCost1ItemOrCurrencyId",
    "extracost1quantity": "extraCost1Quantity",
    "extracost1type": "extraCost1Type",
    "extracost2itemorcurrencyid": "extraCost2ItemOrCurrencyId",
    "extracost2quantity": "extraCost2Quantity",
    "extracost2type": "extraCost2Type",
    "faction1": "faction1",
    "faction2": "faction2",
    "id": "id",
    "index": "index",
    "itemid": "itemId",
    "localisedtextid": "localisedTextId",
    "mode": "mode",
    "outfitinfo": "outfitInfo",
    "questchecklistidx": "questChecklistIdx",
    "rx": "rx",
    "ry": "ry",
    "rz": "rz",
    "sellpricemultiplier": "sellPriceMultiplier",
    "stat": "stat",
    "type": "type",
    "value": "value",
    "world": "world",
    "worldsocketid": "worldSocketId",
    "x": "x",
    "y": "y",
    "z": "z",
}


@dataclass
class ParsedRow:
    table: str
    values: dict[str, str]

    @property
    def entity_id(self) -> int:
        return int(self.values["id"])

    @property
    def position_key(self) -> tuple[int, int, int, int, int, int]:
        return (
            int(float(self.values["world"])),
            int(float(self.values["area"])),
            int(float(self.values["creature"])),
            round(float(self.values["x"]) * 100),
            round(float(self.values["y"]) * 100),
            round(float(self.values["z"]) * 100),
        )


@dataclass
class ParseResult:
    rows_by_table: OrderedDict[str, list[ParsedRow]]
    skipped_by_reason: OrderedDict[str, int]
    adjusted_by_reason: OrderedDict[str, int]
    source_hash: str


def default_laughingws_path(repo_root: Path) -> Path:
    return repo_root / "artifacts" / "external" / "LaughingWS.WorldDatabase.New-Zones-and-more"


def default_official_worlddb_path(repo_root: Path) -> Path:
    return repo_root.parent / "NexusForever.WorldDatabase"


def sql_identifier(name: str) -> str:
    return "`" + name.replace("`", "``") + "`"


def normalise_source_column(column: str) -> str:
    name = normalise_column(column)
    return COLUMN_NAME_MAP.get(name.lower(), name)


def statement_events(text: str) -> list[tuple[int, str, re.Match[str]]]:
    events: list[tuple[int, str, re.Match[str]]] = []
    events.extend((match.start(), "set_world", match) for match in SET_WORLD_RE.finditer(text))
    events.extend((match.start(), "set_guid", match) for match in SET_GUID_RE.finditer(text))
    events.extend((match.start(), "insert", match) for match in INSERT_RE.finditer(text))
    return sorted(events, key=lambda item: item[0])


def translate_guid_expression(value: str, block_index: Optional[int]) -> str:
    text = value.strip()
    match = GUID_OFFSET_RE.match(text)
    if not match:
        return text
    if block_index is None:
        raise ValueError(f"{text} used before a SET @GUID block")

    translated_id = BASE_HOUSING_ENTITY_ID + (block_index * BLOCK_STRIDE) + int(match.group(1))
    return str(translated_id)


def resolve_value(value: str, world_id: Optional[int], block_index: Optional[int], is_id: bool) -> str:
    text = value.strip()
    text = re.sub(r"^([+-])\s+", r"\1", text)
    if text.upper() == "@WORLD":
        if world_id is None:
            raise ValueError("@WORLD used before SET @WORLD")
        return str(world_id)
    if is_id:
        return translate_guid_expression(text, block_index)
    return text


def default_row_values(spec: TableSpec) -> dict[str, str]:
    return {column: "0" for column in spec.columns}


def parse_skyplot_rows(laughingws_path: Path) -> ParseResult:
    source_path = laughingws_path / SKYPLOT_SOURCE
    rows_by_table: OrderedDict[str, list[ParsedRow]] = OrderedDict(
        (table_name, []) for table_name in HOUSING_TABLE_NAMES
    )
    skipped_by_reason: OrderedDict[str, int] = OrderedDict()
    adjusted_by_reason: OrderedDict[str, int] = OrderedDict()

    def skip(reason: str, count: int = 1) -> None:
        skipped_by_reason[reason] = skipped_by_reason.get(reason, 0) + count

    def adjust(reason: str, count: int = 1) -> None:
        adjusted_by_reason[reason] = adjusted_by_reason.get(reason, 0) + count

    text = read_text(source_path)
    source_hash = hashlib.sha256(source_path.read_bytes()).hexdigest()
    world_id: Optional[int] = None
    block_index: Optional[int] = None
    kept_entity_ids: set[int] = set()
    skipped_entity_ids: set[int] = set()

    for _, kind, match in statement_events(text):
        if kind == "set_world":
            world_id = int(match.group(1))
            continue
        if kind == "set_guid":
            block_index = 0 if block_index is None else block_index + 1
            continue

        table_name = match.group(1).lower()
        spec = TABLE_SPECS.get(table_name)
        if spec is None:
            continue

        source_columns = [
            normalise_source_column(column)
            for column in split_top_level_commas(match.group(2))
        ]
        missing_columns = [column for column in spec.columns if column not in source_columns]
        if missing_columns and table_name != "entity":
            skip(f"{table_name} missing {','.join(missing_columns)}")
            continue

        for row_sql in split_value_rows(match.group(3)):
            raw_values = split_top_level_commas(row_sql)
            if len(raw_values) != len(source_columns):
                skip(f"{table_name} malformed value count")
                continue

            values = default_row_values(spec)
            try:
                for index, column in enumerate(source_columns):
                    if column not in spec.columns:
                        continue
                    values[column] = resolve_value(
                        raw_values[index],
                        world_id,
                        block_index,
                        is_id=column in spec.id_columns,
                    )
            except ValueError as error:
                skip(str(error))
                continue

            if table_name == "entity" and "outfitInfo" not in source_columns and "activePropId" in source_columns:
                # WIP/GUESSED: Skyplot vendor rows omit OutfitInfo from the column list but supply an outfit-like value
                # before faction fields. The return pad keeps its large ActivePropId and does not match this shape.
                if (
                    int(float(values["faction1"])) > 1000
                    and int(float(values["activePropId"])) == int(float(values["faction2"]))
                ):
                    values["outfitInfo"] = values["faction1"]
                    values["faction1"] = values["faction2"]
                    values["activePropId"] = "0"
                    adjust("Skyplot vendor omitted OutfitInfo column corrected")

            row = ParsedRow(table_name, values)
            if table_name == "entity":
                kept_entity_ids.add(row.entity_id)
                rows_by_table[table_name].append(row)
                continue

            if row.entity_id in skipped_entity_ids:
                skip(f"{table_name} row for skipped entity skipped")
                continue
            if row.entity_id not in kept_entity_ids:
                skip(f"{table_name} row without kept entity skipped")
                continue
            rows_by_table[table_name].append(row)

    return ParseResult(rows_by_table, skipped_by_reason, adjusted_by_reason, source_hash)


def parse_official_position_keys(root: Path) -> set[tuple[int, int, int, int, int, int]]:
    keys: set[tuple[int, int, int, int, int, int]] = set()
    if not root.is_dir():
        return keys

    for path in sorted(root.rglob("*.sql")):
        text = read_text(path)
        world_id: Optional[int] = None
        for _, kind, match in statement_events(text):
            if kind == "set_world":
                world_id = int(match.group(1))
                continue
            if kind != "insert" or match.group(1).lower() != "entity":
                continue

            source_columns = [
                normalise_source_column(column)
                for column in split_top_level_commas(match.group(2))
            ]
            for row_sql in split_value_rows(match.group(3)):
                raw_values = split_top_level_commas(row_sql)
                if len(raw_values) != len(source_columns):
                    continue

                values = default_row_values(TABLE_SPECS["entity"])
                try:
                    for index, column in enumerate(source_columns):
                        if column not in values:
                            continue
                        values[column] = resolve_value(raw_values[index], world_id, None, is_id=False)
                    keys.add(ParsedRow("entity", values).position_key)
                except ValueError:
                    continue
    return keys


def filter_official_duplicates(result: ParseResult, official_keys: set[tuple[int, int, int, int, int, int]]) -> int:
    if not official_keys:
        return 0

    duplicate_entity_ids = {
        row.entity_id
        for row in result.rows_by_table["entity"]
        if row.position_key in official_keys
    }
    if not duplicate_entity_ids:
        return 0

    removed = 0
    for table_name, rows in result.rows_by_table.items():
        kept = [row for row in rows if row.entity_id not in duplicate_entity_ids]
        removed += len(rows) - len(kept)
        result.rows_by_table[table_name] = kept
    return removed


def chunked(rows: list[ParsedRow], size: int) -> Iterable[list[ParsedRow]]:
    for index in range(0, len(rows), size):
        yield rows[index : index + size]


def render_insert(spec: TableSpec, rows: list[ParsedRow]) -> list[str]:
    if not rows:
        return []

    statements: list[str] = []
    columns_sql = ", ".join(sql_identifier(column) for column in spec.columns)
    update_columns = [column for column in spec.columns if column not in spec.key_columns]
    update_sql = ", ".join(
        f"{sql_identifier(column)} = VALUES({sql_identifier(column)})"
        for column in update_columns
    )
    for chunk in chunked(rows, 120):
        value_lines = []
        for row in chunk:
            values = ", ".join(row.values[column] for column in spec.columns)
            value_lines.append(f"  ({values})")
        statements.append(
            "\n".join(
                [
                    f"INSERT INTO {sql_identifier(spec.name)} ({columns_sql}) VALUES",
                    ",\n".join(value_lines),
                    f"ON DUPLICATE KEY UPDATE {update_sql};",
                ]
            )
        )
    return statements


def render_seed(result: ParseResult, official_duplicate_rows: int) -> str:
    lines = [
        "-- Generated by Tools/DataMapping/extract_laughingws_housing_skyplot.py.",
        "-- Source: LaughingWS/NexusForever.WorldDatabase.New-Zones-and-more Map/Instances/Housing/Skyplot.sql.",
        "-- WIP/GUESSED: Skyplot housing vendor placement is branch-authored and not retail-verified.",
        "-- Purpose: preserve useful housing return-pad/vendor data with deterministic high entity IDs.",
        "-- Raw @GUID/MAX(id) allocation and the source DELETE FROM entity world replacement are intentionally omitted.",
        f"-- Source SHA256: {result.source_hash}",
        "",
        "USE `nexus_forever_world`;",
        "START TRANSACTION;",
        "",
    ]

    total_rows = 0
    for table_name, rows in result.rows_by_table.items():
        if not rows:
            continue
        total_rows += len(rows)
        lines.append(f"-- {table_name}: {len(rows)} row(s)")
        lines.extend(render_insert(TABLE_SPECS[table_name], rows))
        lines.append("")

    if total_rows == 0:
        lines.append("-- No WIP Skyplot housing rows were found.")
        lines.append("")

    if result.skipped_by_reason or result.adjusted_by_reason or official_duplicate_rows:
        lines.append("-- Skipped/adjusted rows:")
        if official_duplicate_rows:
            lines.append(f"--   official duplicate rows skipped: {official_duplicate_rows}")
        for reason, count in result.skipped_by_reason.items():
            lines.append(f"--   {reason}: {count}")
        for reason, count in result.adjusted_by_reason.items():
            lines.append(f"--   {reason}: {count}")
        lines.append("")

    lines.extend(["COMMIT;", ""])
    return "\n".join(lines)


def parse_args() -> argparse.Namespace:
    repo_root = Path(__file__).resolve().parents[2]
    parser = argparse.ArgumentParser(
        description="Generate a deterministic WIP Skyplot housing vendor overlay seed from LaughingWS SQL.",
    )
    parser.add_argument(
        "--laughingws-path",
        type=Path,
        default=default_laughingws_path(repo_root),
        help="Path to LaughingWS/NexusForever.WorldDatabase.New-Zones-and-more.",
    )
    parser.add_argument(
        "--official-worlddb-path",
        type=Path,
        default=default_official_worlddb_path(repo_root),
        help="Optional path to official NexusForever.WorldDatabase for duplicate placement filtering.",
    )
    parser.add_argument(
        "--output-sql",
        type=Path,
        default=repo_root / "Tools" / "DataMapping" / "sql" / "laughingws_housing_skyplot_wip_seed.sql",
        help="Output SQL seed path.",
    )
    return parser.parse_args()


def main() -> int:
    args = parse_args()
    laughingws_path = args.laughingws_path.resolve()
    official_path = args.official_worlddb_path.resolve()

    if not laughingws_path.is_dir():
        print(f"LaughingWS path does not exist: {laughingws_path}", file=sys.stderr)
        return 2
    if not (laughingws_path / SKYPLOT_SOURCE).is_file():
        print(f"LaughingWS Skyplot SQL does not exist: {laughingws_path / SKYPLOT_SOURCE}", file=sys.stderr)
        return 2

    result = parse_skyplot_rows(laughingws_path)
    official_duplicate_rows = filter_official_duplicates(result, parse_official_position_keys(official_path))

    output_sql = args.output_sql.resolve()
    output_sql.parent.mkdir(parents=True, exist_ok=True)
    output_sql.write_text(render_seed(result, official_duplicate_rows), encoding="ascii", newline="\n")

    table_counts = {
        table_name: len(rows)
        for table_name, rows in result.rows_by_table.items()
    }
    total_rows = sum(table_counts.values())
    print(f"Wrote {total_rows} WIP Skyplot housing row(s) to {output_sql}")
    for table_name, count in table_counts.items():
        print(f"  {table_name}: {count}")
    if official_duplicate_rows:
        print(f"  official duplicate rows skipped: {official_duplicate_rows}")
    for reason, count in result.skipped_by_reason.items():
        print(f"  skipped {reason}: {count}")
    for reason, count in result.adjusted_by_reason.items():
        print(f"  adjusted {reason}: {count}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
