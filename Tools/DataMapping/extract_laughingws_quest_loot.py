#!/usr/bin/env python3
"""Extract safe virtual quest loot rows from LaughingWS CreatureQuestLoot.sql."""

from __future__ import annotations

import argparse
import hashlib
import re
import sys
from collections import OrderedDict
from dataclasses import dataclass
from pathlib import Path
from typing import Iterable

from analyze_laughingws_worlddb import split_top_level_commas, split_value_rows


INSERT_RE = re.compile(
    r"\b(?:INSERT(?:\s+IGNORE)?|REPLACE)\s+INTO\s+`?([A-Za-z0-9_]+)`?\s*"
    r"\((.*?)\)\s*VALUE(?:S)?\s*(.*?);",
    re.IGNORECASE | re.DOTALL,
)

BASE_LOOT_GROUP_ID = 1_200_000_000
COMMENT_PREFIX = "LaughingWS quest loot: "


@dataclass(frozen=True)
class TableSpec:
    name: str
    columns: tuple[str, ...]
    key_columns: tuple[str, ...]
    group_id_columns: tuple[str, ...] = ()
    text_columns: frozenset[str] = frozenset()


TABLE_SPECS: tuple[TableSpec, ...] = (
    TableSpec(
        "loot_group",
        ("id", "parentId", "probability", "minDrop", "maxDrop", "conditionType", "condition", "comment"),
        ("id",),
        ("id", "parentId"),
        frozenset(("comment",)),
    ),
    TableSpec(
        "loot_item",
        ("id", "type", "staticId", "probability", "minCount", "maxCount", "comment"),
        ("id", "type", "staticId"),
        ("id",),
        frozenset(("comment",)),
    ),
    TableSpec(
        "entity_loot",
        ("id", "lootGroupId", "comment"),
        ("id", "lootGroupId"),
        ("lootGroupId",),
        frozenset(("comment",)),
    ),
)
TABLE_BY_NAME = {spec.name: spec for spec in TABLE_SPECS}

COLUMN_NAME_MAP = {
    "condition": "condition",
    "conditiontype": "conditionType",
    "comment": "comment",
    "id": "id",
    "lootgroupid": "lootGroupId",
    "maxcount": "maxCount",
    "maxdrop": "maxDrop",
    "mincount": "minCount",
    "mindrop": "minDrop",
    "parentid": "parentId",
    "probability": "probability",
    "staticid": "staticId",
    "type": "type",
}


def normalise_column(column: str) -> str:
    name = column.strip().strip("`").strip('"').strip()
    return COLUMN_NAME_MAP.get(name.lower(), name)


def decode_sql_string(value: str) -> str:
    text = value.strip()
    if not (len(text) >= 2 and text[0] == "'" and text[-1] == "'"):
        return text

    result: list[str] = []
    index = 1
    end = len(text) - 1
    while index < end:
        char = text[index]
        if char == "\\" and index + 1 < end:
            index += 1
            escaped = text[index]
            result.append(
                {
                    "0": "\0",
                    "b": "\b",
                    "n": "\n",
                    "r": "\r",
                    "t": "\t",
                    "Z": "\x1a",
                    "\\": "\\",
                    "'": "'",
                    '"': '"',
                }.get(escaped, escaped)
            )
        elif char == "'" and index + 1 < end and text[index + 1] == "'":
            result.append("'")
            index += 1
        else:
            result.append(char)
        index += 1

    return "".join(result)


def sql_identifier(name: str) -> str:
    return "`" + name.replace("`", "``") + "`"


def sql_text_literal(value: str) -> str:
    if value == "":
        return "''"
    return "CONVERT(0x" + value.encode("utf-8").hex() + " USING utf8mb4)"


def translate_group_id(raw_value: str) -> str:
    value = raw_value.strip()
    if value.upper() == "NULL":
        return "NULL"
    return str(BASE_LOOT_GROUP_ID + int(value, 0))


def sql_value(column: str, raw_value: str, spec: TableSpec) -> str:
    text = raw_value.strip()
    if column in spec.group_id_columns:
        return translate_group_id(text)
    if column in spec.text_columns:
        decoded = decode_sql_string(text)
        if decoded.upper() == "NULL":
            return "NULL"
        comment = (COMMENT_PREFIX + decoded)[:200]
        return sql_text_literal(comment)
    if text.upper() == "NULL":
        return "NULL"
    return text


def parse_rows(source_sql: str) -> OrderedDict[str, OrderedDict[tuple[str, ...], dict[str, str]]]:
    tables: OrderedDict[str, OrderedDict[tuple[str, ...], dict[str, str]]] = OrderedDict(
        (spec.name, OrderedDict()) for spec in TABLE_SPECS
    )

    for match in INSERT_RE.finditer(source_sql):
        table = match.group(1).lower()
        spec = TABLE_BY_NAME.get(table)
        if spec is None:
            continue

        source_columns = [normalise_column(column) for column in split_top_level_commas(match.group(2))]
        missing = [column for column in spec.columns if column not in source_columns]
        if missing:
            raise ValueError(f"{table} insert is missing required column(s): {', '.join(missing)}")

        for row_text in split_value_rows(match.group(3)):
            values = split_top_level_commas(row_text)
            if len(values) != len(source_columns):
                raise ValueError(
                    f"{table} row has {len(values)} value(s), expected {len(source_columns)}"
                )

            row_by_column = {
                column: sql_value(column, values[index], spec)
                for index, column in enumerate(source_columns)
                if column in spec.columns
            }
            key = tuple(row_by_column[column] for column in spec.key_columns)
            tables[spec.name][key] = row_by_column

    validate_references(tables)
    return tables


def validate_references(tables: OrderedDict[str, OrderedDict[tuple[str, ...], dict[str, str]]]) -> None:
    group_ids = {row["id"] for row in tables["loot_group"].values()}

    missing_entity_groups = sorted({
        row["lootGroupId"]
        for row in tables["entity_loot"].values()
        if row["lootGroupId"] not in group_ids
    })
    missing_item_groups = sorted({
        row["id"]
        for row in tables["loot_item"].values()
        if row["id"] not in group_ids
    })
    missing_parent_groups = sorted({
        row["parentId"]
        for row in tables["loot_group"].values()
        if row["parentId"] != "NULL" and row["parentId"] not in group_ids
    })

    missing = missing_entity_groups + missing_item_groups + missing_parent_groups
    if missing:
        preview = ", ".join(missing[:10])
        raise ValueError(f"Quest loot references missing translated loot group id(s): {preview}")


def chunked(values: list[dict[str, str]], size: int) -> Iterable[list[dict[str, str]]]:
    for index in range(0, len(values), size):
        yield values[index : index + size]


def render_insert(spec: TableSpec, rows: list[dict[str, str]], chunk_size: int) -> list[str]:
    if not rows:
        return []

    statements: list[str] = []
    columns_sql = ", ".join(sql_identifier(column) for column in spec.columns)
    update_columns = [column for column in spec.columns if column not in spec.key_columns]
    updates_sql = ", ".join(
        f"{sql_identifier(column)} = VALUES({sql_identifier(column)})"
        for column in update_columns
    )

    for chunk in chunked(rows, chunk_size):
        values_lines = []
        for row in chunk:
            value_sql = ", ".join(row[column] for column in spec.columns)
            values_lines.append(f"  ({value_sql})")

        statements.append(
            "\n".join(
                [
                    f"INSERT INTO {sql_identifier(spec.name)} ({columns_sql}) VALUES",
                    ",\n".join(values_lines),
                    f"ON DUPLICATE KEY UPDATE {updates_sql};",
                ]
            )
        )

    return statements


def render_seed(source_path: Path, tables: OrderedDict[str, OrderedDict[tuple[str, ...], dict[str, str]]]) -> str:
    source_hash = hashlib.sha256(source_path.read_bytes()).hexdigest()
    lines = [
        "-- Generated by Tools/DataMapping/extract_laughingws_quest_loot.py.",
        f"-- Source: {source_path.as_posix()}",
        f"-- Source SHA256: {source_hash}",
        f"-- Source loot_group ids are shifted by +{BASE_LOOT_GROUP_ID} to avoid low-id collisions.",
        "-- DDL and foreign-key toggles from the source dump are intentionally omitted.",
        "",
        "USE `nexus_forever_world`;",
        "START TRANSACTION;",
        "",
    ]

    for spec in TABLE_SPECS:
        rows = list(tables[spec.name].values())
        lines.append(f"-- {spec.name}: {len(rows)} row(s)")
        lines.extend(render_insert(spec, rows, chunk_size=100))
        lines.append("")

    lines.extend(["COMMIT;", ""])
    return "\n".join(lines)


def resolve_default_source(repo_root: Path) -> Path | None:
    candidates = [
        repo_root.parent / "NexusForever.WorldDatabase.New-Zones-and-more" / "Loot" / "CreatureQuestLoot.sql",
        Path.cwd() / "Loot" / "CreatureQuestLoot.sql",
    ]
    for candidate in candidates:
        if candidate.exists():
            return candidate
    return None


def parse_args() -> argparse.Namespace:
    repo_root = Path(__file__).resolve().parents[2]
    parser = argparse.ArgumentParser(
        description="Generate an idempotent virtual quest loot seed from LaughingWS CreatureQuestLoot.sql.",
    )
    parser.add_argument(
        "--source-sql",
        type=Path,
        default=resolve_default_source(repo_root),
        help="Path to LaughingWS Loot/CreatureQuestLoot.sql.",
    )
    parser.add_argument(
        "--output-sql",
        type=Path,
        default=repo_root / "Tools" / "DataMapping" / "sql" / "laughingws_quest_loot_seed.sql",
        help="Output SQL seed path.",
    )
    return parser.parse_args()


def main() -> int:
    args = parse_args()
    if args.source_sql is None or not args.source_sql.exists():
        print("LaughingWS CreatureQuestLoot.sql was not found. Pass --source-sql.", file=sys.stderr)
        return 2

    source_sql = args.source_sql.resolve()
    tables = parse_rows(source_sql.read_text(encoding="utf-8-sig", errors="replace"))
    output_sql = args.output_sql.resolve()
    output_sql.parent.mkdir(parents=True, exist_ok=True)
    output_sql.write_text(render_seed(source_sql, tables), encoding="ascii", newline="\n")

    total_rows = sum(len(rows) for rows in tables.values())
    print(f"Wrote {total_rows} quest loot row(s) to {output_sql}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
