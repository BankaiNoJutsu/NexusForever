#!/usr/bin/env python3
"""
Extract a safe runtime store catalog seed from LaughingWS store/event SQL.

The main source file is a full table dump with DROP/CREATE TABLE statements.
Additional event store files may be additive REPLACE/INSERT scripts. This
script keeps only storefront rows and emits idempotent upserts for the current
NexusForever world schema.
"""

from __future__ import annotations

import argparse
import hashlib
import re
import sys
from collections import OrderedDict
from dataclasses import dataclass
from pathlib import Path
from typing import Iterable, Optional


INSERT_RE = re.compile(
    r"\b(?:INSERT(?:\s+IGNORE)?|REPLACE)\s+INTO\s+`?([A-Za-z0-9_]+)`?\s*"
    r"\((.*?)\)\s*VALUE(?:S)?\s*(.*?);",
    re.IGNORECASE | re.DOTALL,
)
GUID_OFFSET_RE = re.compile(r"^@GUID\s*\+\s*(\d+)$", re.IGNORECASE)

# WIP/GUESSED: reserve a deterministic, current-schema-safe range for hand-written
# event store placeholders whose source SQL used @GUID rather than real store ids.
BASE_WIP_STORE_ID = 2_120_000_000
SOURCE_STRIDE = 1_000
TYPE0_ACCOUNT_ITEM_MAX = 0x7FFF
FEATURED_CATEGORY_ID = "76"

# WIP/GUESSED: the stock client appears to filter store offers with unmet
# AccountItem prerequisites before Lua builds category grids. Keep Featured
# populated with existing no-prerequisite offers so the default store view has
# useful content without relying on banner loosedata.
FEATURED_CATEGORY_BACKFILL_ROWS: tuple[tuple[str, str, str, str], ...] = (
    ("2808", FEATURED_CATEGORY_ID, "13", "1"),  # Character Boost Bundle
    ("2807", FEATURED_CATEGORY_ID, "14", "1"),  # AMP & Ability Bundle
    ("3312", FEATURED_CATEGORY_ID, "15", "1"),  # Ho Ho Ho Mobile (Green)
    ("3315", FEATURED_CATEGORY_ID, "16", "1"),  # Ho Ho Ho Mobile (Blue)
    ("3318", FEATURED_CATEGORY_ID, "17", "1"),  # Ho Ho Ho Mobile (Gold)
    ("1727", FEATURED_CATEGORY_ID, "18", "1"),  # Ghastly Skeletal Warpig
    ("3332", FEATURED_CATEGORY_ID, "19", "1"),  # Cheer Gear Costume
    ("3223", FEATURED_CATEGORY_ID, "20", "1"),  # Metal Maniac Costume
    ("3125", FEATURED_CATEGORY_ID, "21", "1"),  # Deep Diver Diving Suit
    ("3123", FEATURED_CATEGORY_ID, "22", "1"),  # Surfboard Sampler Pack
    ("3124", FEATURED_CATEGORY_ID, "23", "1"),  # Alien Decor Pack
    ("3209", FEATURED_CATEGORY_ID, "24", "1"),  # Fancy Pants Decor Pack
)

LEGACY_TABLE_ALIASES = {
    "store_offer_category": "store_offer_group_category",
    "store_offer_data": "store_offer_item_data",
}


@dataclass(frozen=True)
class TableSpec:
    name: str
    columns: tuple[str, ...]
    key_columns: tuple[str, ...]
    text_columns: frozenset[str] = frozenset()


TABLE_SPECS: tuple[TableSpec, ...] = (
    TableSpec(
        "store_category",
        ("id", "parentId", "name", "description", "index", "visible"),
        ("id",),
        frozenset(("name", "description")),
    ),
    TableSpec(
        "store_offer_group",
        ("id", "displayFlags", "name", "description", "displayInfoOverride", "visible"),
        ("id",),
        frozenset(("name", "description")),
    ),
    TableSpec(
        "store_offer_group_category",
        ("id", "categoryId", "index", "visible"),
        ("id", "categoryId"),
    ),
    TableSpec(
        "store_offer_item",
        ("id", "groupId", "name", "description", "displayFlags", "field_6", "field_7", "visible"),
        ("id", "groupId"),
        frozenset(("name", "description")),
    ),
    TableSpec(
        "store_offer_item_data",
        ("id", "itemId", "type", "amount"),
        ("id", "itemId"),
    ),
    TableSpec(
        "store_offer_item_price",
        ("id", "currencyId", "price", "discountType", "discountValue", "field_14", "expiry"),
        ("id", "currencyId"),
    ),
)
TABLE_BY_NAME = {spec.name: spec for spec in TABLE_SPECS}

COLUMN_NAME_MAP = {
    "categoryid": "categoryId",
    "currencyid": "currencyId",
    "description": "description",
    "discounttype": "discountType",
    "discountvalue": "discountValue",
    "displayflags": "displayFlags",
    "displayinfooverride": "displayInfoOverride",
    "expiry": "expiry",
    "field_6": "field_6",
    "field_7": "field_7",
    "field_14": "field_14",
    "groupid": "groupId",
    "id": "id",
    "index": "index",
    "itemid": "itemId",
    "name": "name",
    "parentid": "parentId",
    "price": "price",
    "type": "type",
    "amount": "amount",
    "visible": "visible",
}


def split_top_level_commas(text: str) -> list[str]:
    values: list[str] = []
    start = 0
    depth = 0
    in_single = False
    in_double = False
    in_backtick = False
    index = 0
    while index < len(text):
        char = text[index]
        if in_single:
            if char == "\\":
                index += 2
                continue
            if char == "'":
                if index + 1 < len(text) and text[index + 1] == "'":
                    index += 2
                    continue
                in_single = False
            index += 1
            continue

        if char == "'" and not in_double and not in_backtick:
            in_single = True
        elif char == '"' and not in_backtick:
            in_double = not in_double
        elif char == "`" and not in_double:
            in_backtick = not in_backtick
        elif not in_double and not in_backtick:
            if char == "(":
                depth += 1
            elif char == ")" and depth > 0:
                depth -= 1
            elif char == "," and depth == 0:
                values.append(text[start:index])
                start = index + 1
        index += 1

    values.append(text[start:])
    return values


def split_value_rows(values_sql: str) -> list[str]:
    rows: list[str] = []
    start: Optional[int] = None
    depth = 0
    in_single = False
    index = 0
    while index < len(values_sql):
        char = values_sql[index]
        if in_single:
            if char == "\\":
                index += 2
                continue
            if char == "'":
                if index + 1 < len(values_sql) and values_sql[index + 1] == "'":
                    index += 2
                    continue
                in_single = False
            index += 1
            continue

        if char == "'":
            in_single = True
        elif char == "(":
            if depth == 0:
                start = index + 1
            depth += 1
        elif char == ")" and depth > 0:
            depth -= 1
            if depth == 0 and start is not None:
                rows.append(values_sql[start:index])
                start = None
        index += 1

    return rows


def normalise_column(column: str) -> str:
    name = column.strip().strip("`").strip('"').strip()
    return COLUMN_NAME_MAP.get(name.lower(), name)


def repair_known_sql_typos(text: str) -> str:
    # WIP/GUESSED: Dungeon Chase has one malformed `Store_offer_item` column list
    # (`Field_7, `Visible`). Repair only that known typo before normal parsing.
    return re.sub(r"`Field_7\s*,\s*`Visible`", "`Field_7`, `Visible`", text, flags=re.IGNORECASE)


def normalise_table_name(name: str) -> str:
    lowered = name.lower()
    return LEGACY_TABLE_ALIASES.get(lowered, lowered)


def translate_guid_expression(value: str, source_index: int) -> str:
    text = value.strip()
    match = GUID_OFFSET_RE.match(text)
    if match is None:
        return text

    return str(BASE_WIP_STORE_ID + (source_index * SOURCE_STRIDE) + int(match.group(1)))


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


def sql_value(column: str, raw_value: str, spec: TableSpec) -> str:
    text = raw_value.strip()
    if column in spec.text_columns:
        return sql_text_literal(decode_sql_string(text))
    if text.upper() == "NULL":
        return "NULL"
    return text


def skip_reason(skipped_by_reason: Optional[OrderedDict[str, int]], reason: str, count: int = 1) -> None:
    if skipped_by_reason is None:
        return
    skipped_by_reason[reason] = skipped_by_reason.get(reason, 0) + count


def prune_invalid_rows(
    tables: OrderedDict[str, OrderedDict[tuple[str, ...], dict[str, str]]],
    skipped_by_reason: Optional[OrderedDict[str, int]],
) -> None:
    invalid_offer_ids_by_item: OrderedDict[str, int] = OrderedDict()
    for row in tables["store_offer_item_data"].values():
        try:
            item_type = int(row["type"])
            item_id = int(row["itemId"])
        except ValueError:
            continue

        if item_type == 0 and item_id > TYPE0_ACCOUNT_ITEM_MAX:
            invalid_offer_ids_by_item[row["id"]] = item_id

    if not invalid_offer_ids_by_item:
        return

    for item_id in invalid_offer_ids_by_item.values():
        skip_reason(
            skipped_by_reason,
            f"WIP/GUESSED Dungeon Chase hidden store placeholder skipped: type 0 account item {item_id} exceeds current 15-bit store wire/schema proof",
        )
    invalid_offer_ids = set(invalid_offer_ids_by_item)
    for rows in tables.values():
        remove_keys = [
            key
            for key, row in rows.items()
            if row.get("id") in invalid_offer_ids
        ]
        for key in remove_keys:
            del rows[key]


def normalise_active_store_price_expiry(
    tables: OrderedDict[str, OrderedDict[tuple[str, ...], dict[str, str]]],
) -> None:
    # The runtime emits this scalar unchanged: <= 0 is active, > 0 is expired.
    # LaughingWS source rows are seeded as currently active catalog rows.
    for row in tables["store_offer_item_price"].values():
        try:
            expiry = int(row["expiry"])
        except ValueError:
            continue

        if expiry > 0:
            row["expiry"] = str(-expiry)


def parse_rows(
    source_sql: str,
    source_index: int = 0,
    skipped_by_reason: Optional[OrderedDict[str, int]] = None,
) -> OrderedDict[str, OrderedDict[tuple[str, ...], dict[str, str]]]:
    tables: OrderedDict[str, OrderedDict[tuple[str, ...], dict[str, str]]] = OrderedDict(
        (spec.name, OrderedDict()) for spec in TABLE_SPECS
    )

    for match in INSERT_RE.finditer(repair_known_sql_typos(source_sql)):
        table = normalise_table_name(match.group(1))
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
                column: sql_value(column, translate_guid_expression(values[index], source_index), spec)
                for index, column in enumerate(source_columns)
                if column in spec.columns
            }
            key = tuple(row_by_column[column] for column in spec.key_columns)
            tables[spec.name][key] = row_by_column

    prune_invalid_rows(tables, skipped_by_reason)
    normalise_active_store_price_expiry(tables)
    return tables


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


def merge_tables(
    target: OrderedDict[str, OrderedDict[tuple[str, ...], dict[str, str]]],
    source: OrderedDict[str, OrderedDict[tuple[str, ...], dict[str, str]]],
) -> None:
    for table_name, rows in source.items():
        target[table_name].update(rows)


def apply_featured_category_backfill(
    tables: OrderedDict[str, OrderedDict[tuple[str, ...], dict[str, str]]],
) -> None:
    offer_groups = tables["store_offer_group"]
    category_rows = tables["store_offer_group_category"]
    for group_id, category_id, index, visible in FEATURED_CATEGORY_BACKFILL_ROWS:
        if (group_id,) not in offer_groups:
            continue

        key = (group_id, category_id)
        if key in category_rows:
            continue

        category_rows[key] = {
            "id": group_id,
            "categoryId": category_id,
            "index": index,
            "visible": visible,
        }


def render_seed(
    source_paths: list[Path],
    tables: OrderedDict[str, OrderedDict[tuple[str, ...], dict[str, str]]],
    skipped_by_reason: OrderedDict[str, int],
) -> str:
    lines = [
        "-- Generated by Tools/DataMapping/extract_laughingws_store_catalog.py.",
        "-- Sources:",
    ]

    for source_path in source_paths:
        source_hash = hashlib.sha256(source_path.read_bytes()).hexdigest()
        lines.append(f"--   {source_path.as_posix()}")
        lines.append(f"--   SHA256: {source_hash}")

    lines.extend([
        "-- DDL, deletes, and broad table replacement statements from source dumps are intentionally omitted.",
        "-- WIP/GUESSED: Dungeon Chase's hidden in-game-store placeholder is parsed through legacy table aliases and a repaired source column typo, then skipped unless it fits current store wire/schema proof.",
        "",
        "USE `nexus_forever_world`;",
        "START TRANSACTION;",
        "",
    ])

    for spec in TABLE_SPECS:
        rows = list(tables[spec.name].values())
        lines.append(f"-- {spec.name}: {len(rows)} row(s)")
        lines.extend(render_insert(spec, rows, chunk_size=100))
        lines.append("")

    if skipped_by_reason:
        lines.append("-- Skipped rows:")
        for reason, count in skipped_by_reason.items():
            lines.append(f"--   {reason}: {count}")
        lines.append("")

    lines.extend(["COMMIT;", ""])
    return "\n".join(lines)


def resolve_default_sources(repo_root: Path) -> list[Path]:
    candidates = [
        repo_root.parent / "NexusForever.WorldDatabase.New-Zones-and-more" / "Store" / "StoreCatalog.sql",
        Path.cwd() / "Store" / "StoreCatalog.sql",
    ]
    for candidate in candidates:
        if candidate.exists():
            sources = [candidate]
            event_sources = [
                candidate.parent / "Store Events" / "Valentine's Day items.sql",
                candidate.parent.parent / "Events" / "Dungeon Chase.sql",
                candidate.parent.parent / "Events" / "Shades Eve.sql",
                candidate.parent.parent / "Events" / "Winterfest.sql",
            ]
            sources.extend(event_source for event_source in event_sources if event_source.exists())
            return sources
    return []


def parse_args() -> argparse.Namespace:
    repo_root = Path(__file__).resolve().parents[2]
    parser = argparse.ArgumentParser(
        description="Generate an idempotent store catalog seed from LaughingWS store SQL."
    )
    parser.add_argument(
        "--source-sql",
        type=Path,
        action="append",
        default=None,
        help="Path to a LaughingWS store SQL file. May be specified more than once.",
    )
    parser.add_argument(
        "--output-sql",
        type=Path,
        default=repo_root / "Tools" / "DataMapping" / "sql" / "laughingws_store_catalog_seed.sql",
        help="Output SQL seed path.",
    )
    return parser.parse_args()


def main() -> int:
    args = parse_args()
    repo_root = Path(__file__).resolve().parents[2]
    source_paths = [path.resolve() for path in (args.source_sql or resolve_default_sources(repo_root))]
    missing_sources = [str(path) for path in source_paths if not path.exists()]
    if not source_paths or missing_sources:
        if missing_sources:
            print(f"LaughingWS store SQL was not found: {', '.join(missing_sources)}", file=sys.stderr)
        else:
            print("LaughingWS store SQL was not found. Pass --source-sql.", file=sys.stderr)
        return 2

    tables: OrderedDict[str, OrderedDict[tuple[str, ...], dict[str, str]]] = OrderedDict(
        (spec.name, OrderedDict()) for spec in TABLE_SPECS
    )
    skipped_by_reason: OrderedDict[str, int] = OrderedDict()
    for source_index, source_path in enumerate(source_paths):
        merge_tables(
            tables,
            parse_rows(
                source_path.read_text(encoding="utf-8-sig", errors="replace"),
                source_index,
                skipped_by_reason,
            ),
        )

    apply_featured_category_backfill(tables)

    output_sql = args.output_sql.resolve()
    output_sql.parent.mkdir(parents=True, exist_ok=True)
    output_sql.write_text(render_seed(source_paths, tables, skipped_by_reason), encoding="ascii", newline="\n")

    total_rows = sum(len(rows) for rows in tables.values())
    print(f"Wrote {total_rows} store catalog row(s) from {len(source_paths)} source file(s) to {output_sql}")
    for reason, count in skipped_by_reason.items():
        print(f"  skipped {reason}: {count}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
