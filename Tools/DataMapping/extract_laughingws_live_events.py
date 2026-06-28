#!/usr/bin/env python3
"""Extract WIP live-event entity/vendor rows from LaughingWS event SQL."""

from __future__ import annotations

import argparse
import hashlib
import re
import sys
from collections import OrderedDict, defaultdict
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

BASE_EVENT_ENTITY_ID = 2_100_000_000
# Keep per-source ranges wide enough for Shades Eve's large @GUID block count
# while staying below the signed INT max used by current runtime entity IDs.
SOURCE_STRIDE = 2_000_000
BLOCK_STRIDE = 10_000
PLACEHOLDER_COORDINATE_EPSILON = 1.0

# These hand-authored live-event rows visually duplicate current official world
# rows. They are kept out even when an official-world checkout is unavailable.
OFFICIAL_DUPLICATE_POSITION_KEYS = {
    (51, 2546, 67312, 419363, -81322, -245825),
    (51, 2546, 67312, 429142, -80819, -245461),
}

EVENT_TABLE_NAMES = (
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
    source_file: str
    source_index: int
    table: str
    block_index: int
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

    @property
    def is_placeholder_position(self) -> bool:
        return all(
            abs(float(self.values[column])) <= PLACEHOLDER_COORDINATE_EPSILON
            for column in ("x", "y", "z")
        )


@dataclass
class ParseResult:
    rows_by_source: OrderedDict[str, OrderedDict[str, list[ParsedRow]]]
    source_notes: dict[str, list[str]]
    skipped_by_reason: OrderedDict[str, int]
    source_hashes: dict[str, str]


def default_laughingws_path(repo_root: Path) -> Path:
    return repo_root / "artifacts" / "external" / "LaughingWS.WorldDatabase.New-Zones-and-more"


def default_official_worlddb_path(repo_root: Path) -> Path:
    return repo_root.parent / "NexusForever.WorldDatabase"


def iter_event_sql_files(root: Path) -> Iterable[Path]:
    events_root = root / "Events"
    if not events_root.is_dir():
        return
    yield from sorted(events_root.rglob("*.sql"))


def normalise_source_path(path: Path, root: Path) -> str:
    return str(path.relative_to(root)).replace("\\", "/")


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


def translate_guid_expression(value: str, source_index: int, block_index: Optional[int]) -> str:
    text = value.strip()
    match = GUID_OFFSET_RE.match(text)
    if not match:
        return text
    if block_index is None:
        raise ValueError(f"{text} used before a SET @GUID block")

    offset = int(match.group(1))
    translated_id = BASE_EVENT_ENTITY_ID + (source_index * SOURCE_STRIDE) + (block_index * BLOCK_STRIDE) + offset
    return str(translated_id)


def resolve_value(value: str, world_id: Optional[int], source_index: int, block_index: Optional[int], is_id: bool) -> str:
    text = value.strip()
    text = re.sub(r"^([+-])\s+", r"\1", text)
    if text.upper() == "@WORLD":
        if world_id is None:
            raise ValueError("@WORLD used before SET @WORLD")
        return str(world_id)
    if is_id:
        return translate_guid_expression(text, source_index, block_index)
    return text


def extract_source_notes(text: str) -> list[str]:
    notes: list[str] = []
    for line in text.splitlines():
        stripped = line.strip()
        if not stripped.startswith("--"):
            continue
        note = stripped[2:].strip()
        if not note or set(note) == {"-"}:
            continue
        lowered = note.lower()
        if any(token in lowered for token in ("wip", "guess", "todo", "video", "screenshot", "made by hand")):
            notes.append(note)

    if not notes:
        notes.append("WIP: branch live-event rows are hand-authored and not retail-verified.")
    return list(dict.fromkeys(notes))


def default_row_values(spec: TableSpec) -> dict[str, str]:
    return {column: "0" for column in spec.columns}


def parse_laughingws_event_rows(root: Path) -> ParseResult:
    rows_by_source: OrderedDict[str, OrderedDict[str, list[ParsedRow]]] = OrderedDict()
    source_notes: dict[str, list[str]] = {}
    skipped_by_reason: OrderedDict[str, int] = OrderedDict()
    source_hashes: dict[str, str] = {}

    def skip(reason: str, count: int = 1) -> None:
        skipped_by_reason[reason] = skipped_by_reason.get(reason, 0) + count

    source_position_keys: set[tuple[int, int, int, int, int, int]] = set()
    kept_entity_ids: set[int] = set()
    skipped_entity_ids: set[int] = set()

    for source_index, path in enumerate(iter_event_sql_files(root)):
        relative_path = normalise_source_path(path, root)
        if relative_path.lower().endswith("remove any event.sql"):
            skip("event remover script excluded")
            continue

        text = read_text(path)
        source_hashes[relative_path] = hashlib.sha256(path.read_bytes()).hexdigest()
        source_notes[relative_path] = extract_source_notes(text)
        rows_by_table: OrderedDict[str, list[ParsedRow]] = OrderedDict(
            (table_name, []) for table_name in EVENT_TABLE_NAMES
        )
        rows_by_source[relative_path] = rows_by_table

        world_id: Optional[int] = None
        block_index: Optional[int] = None
        row_index = 0
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
            if missing_columns and table_name == "entity":
                # Entity rows can omit newer current-schema columns; defaults are filled below.
                pass
            elif missing_columns:
                skip(f"{relative_path}: {table_name} missing {','.join(missing_columns)}")
                continue

            for row_sql in split_value_rows(match.group(3)):
                raw_values = split_top_level_commas(row_sql)
                if len(raw_values) != len(source_columns):
                    skip(f"{relative_path}: {table_name} malformed value count")
                    continue

                values = default_row_values(spec)
                try:
                    for index, column in enumerate(source_columns):
                        if column not in spec.columns:
                            continue
                        values[column] = resolve_value(
                            raw_values[index],
                            world_id,
                            source_index,
                            block_index,
                            is_id=column in spec.id_columns,
                        )
                except ValueError as error:
                    skip(f"{relative_path}: {error}")
                    continue

                row = ParsedRow(relative_path, row_index, table_name, block_index or 0, values)
                row_index += 1

                if table_name == "entity":
                    if row.is_placeholder_position:
                        skipped_entity_ids.add(row.entity_id)
                        skip("placeholder event entity coordinate skipped")
                        continue
                    if row.position_key in OFFICIAL_DUPLICATE_POSITION_KEYS:
                        skipped_entity_ids.add(row.entity_id)
                        skip("official duplicate event entity coordinate skipped")
                        continue
                    if row.position_key in source_position_keys:
                        skipped_entity_ids.add(row.entity_id)
                        skip("duplicate event entity coordinate skipped")
                        continue
                    source_position_keys.add(row.position_key)
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

    return ParseResult(rows_by_source, source_notes, skipped_by_reason, source_hashes)


def parse_official_position_keys(root: Path) -> set[tuple[int, int, int, int, int, int]]:
    keys: set[tuple[int, int, int, int, int, int]] = set()
    if not root.is_dir():
        return keys

    for path in sorted(root.rglob("*.sql")):
        text = read_text(path)
        world_id: Optional[int] = None
        block_index: Optional[int] = None
        for _, kind, match in statement_events(text):
            if kind == "set_world":
                world_id = int(match.group(1))
                continue
            if kind == "set_guid":
                block_index = 0 if block_index is None else block_index + 1
                continue
            table_name = match.group(1).lower()
            if table_name != "entity":
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
                for index, column in enumerate(source_columns):
                    if column not in values:
                        continue
                    values[column] = resolve_value(
                        raw_values[index],
                        world_id,
                        source_index=0,
                        block_index=block_index,
                        is_id=False,
                    )

                try:
                    row = ParsedRow("", 0, "entity", 0, values)
                    if not row.is_placeholder_position:
                        keys.add(row.position_key)
                except ValueError:
                    continue
    return keys


def filter_official_duplicates(result: ParseResult, official_keys: set[tuple[int, int, int, int, int, int]]) -> int:
    if not official_keys:
        return 0

    duplicate_entity_ids: set[int] = set()
    removed = 0
    for tables in result.rows_by_source.values():
        kept_entities: list[ParsedRow] = []
        for row in tables["entity"]:
            if row.position_key in official_keys:
                duplicate_entity_ids.add(row.entity_id)
                removed += 1
            else:
                kept_entities.append(row)
        tables["entity"] = kept_entities

    for tables in result.rows_by_source.values():
        for table_name in EVENT_TABLE_NAMES[1:]:
            original = tables[table_name]
            tables[table_name] = [row for row in original if row.entity_id not in duplicate_entity_ids]
            removed += len(original) - len(tables[table_name])
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
        "-- Generated by Tools/DataMapping/extract_laughingws_live_events.py.",
        "-- Source: LaughingWS/NexusForever.WorldDatabase.New-Zones-and-more Events SQL.",
        "-- WIP/GUESSED: live-event entity placement is branch-authored and not retail-verified.",
        "-- Purpose: preserve usable event spawn/vendor data with deterministic high entity IDs.",
        "-- Raw @GUID/MAX(id), deletes, store rows, and malformed unsupported rows are intentionally omitted.",
        "",
        "USE `nexus_forever_world`;",
        "START TRANSACTION;",
        "",
    ]

    total_rows = 0
    for source_file, tables in result.rows_by_source.items():
        source_total = sum(len(rows) for rows in tables.values())
        if source_total == 0:
            continue
        total_rows += source_total

        lines.append(f"-- Source: {source_file}")
        lines.append(f"-- SHA256: {result.source_hashes[source_file]}")
        lines.append("-- WIP/GUESSED: imported from hand-authored LaughingWS live-event SQL; verify in client before claiming retail parity.")
        for note in result.source_notes[source_file]:
            lines.append(f"-- Source note: {note}")
        lines.append("")

        for table_name, rows in tables.items():
            if not rows:
                continue
            lines.append(f"-- {table_name}: {len(rows)} row(s)")
            lines.extend(render_insert(TABLE_SPECS[table_name], rows))
            lines.append("")

    if total_rows == 0:
        lines.append("-- No WIP live-event overlay rows were found.")
        lines.append("")

    if result.skipped_by_reason or official_duplicate_rows:
        lines.append("-- Skipped rows:")
        if official_duplicate_rows:
            lines.append(f"--   official duplicate rows skipped: {official_duplicate_rows}")
        for reason, count in result.skipped_by_reason.items():
            lines.append(f"--   {reason}: {count}")
        lines.append("")

    lines.extend(["COMMIT;", ""])
    return "\n".join(lines)


def parse_args() -> argparse.Namespace:
    repo_root = Path(__file__).resolve().parents[2]
    parser = argparse.ArgumentParser(
        description="Generate a deterministic WIP live-event overlay seed from LaughingWS event SQL.",
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
        default=repo_root / "Tools" / "DataMapping" / "sql" / "laughingws_live_event_wip_seed.sql",
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

    result = parse_laughingws_event_rows(laughingws_path)
    official_keys = parse_official_position_keys(official_path)
    official_duplicate_rows = filter_official_duplicates(result, official_keys)

    output_sql = args.output_sql.resolve()
    output_sql.parent.mkdir(parents=True, exist_ok=True)
    output_sql.write_text(render_seed(result, official_duplicate_rows), encoding="ascii", newline="\n")

    table_counts = {
        table_name: sum(len(tables[table_name]) for tables in result.rows_by_source.values())
        for table_name in EVENT_TABLE_NAMES
    }
    total_rows = sum(table_counts.values())
    print(f"Wrote {total_rows} WIP live-event row(s) to {output_sql}")
    for table_name, count in table_counts.items():
        print(f"  {table_name}: {count}")
    if official_duplicate_rows:
        print(f"  official duplicate rows skipped: {official_duplicate_rows}")
    for reason, count in result.skipped_by_reason.items():
        print(f"  skipped {reason}: {count}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
