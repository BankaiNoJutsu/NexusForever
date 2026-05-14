#!/usr/bin/env python3
"""
Apply mapped Jabbithole vendor stock to the NexusForever world database.

The script is intentionally conservative:

* It reads the reviewed `vendor_item_map.csv` output.
* By default it imports only `unique_name` and `scored_name` creature bridges.
* It joins vendor creature IDs to existing `nexus_forever_world.entity.creature`
  values, so it only applies stock to entities that already exist in the world DB.
* It is idempotent unless `--replace-existing` is supplied.
"""

from __future__ import annotations

import argparse
import csv
import json
import subprocess
import sys
import time
from collections import Counter, defaultdict
from dataclasses import dataclass
from pathlib import Path
from typing import Dict, Iterable, List, Optional, Sequence, Tuple


DEFAULT_MYSQL = r"C:\Program Files\MySQL\MySQL Server 8.0\bin\mysql.exe"
SAFE_MATCH_STATUSES = {"unique_name", "scored_name", "reviewed"}


@dataclass(frozen=True)
class VendorRow:
    creature2_id: int
    item2_id: int
    item_index: int
    source_id: int
    source_name: str
    item_name: str
    match_status: str
    game_version: int
    cost1_type: int
    cost1_quantity: int
    cost1_id: int
    cost2_type: int
    cost2_quantity: int
    cost2_id: int


def to_int(value, default: int = 0) -> int:
    if value is None:
        return default
    text = str(value).strip()
    if not text:
        return default
    try:
        return int(float(text))
    except ValueError:
        return default


def sql_string(value: str) -> str:
    return "'" + str(value).replace("\\", "\\\\").replace("'", "''") + "'"


def mysql_command(args: argparse.Namespace, extra: Optional[Sequence[str]] = None) -> List[str]:
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


def mysql_rows(args: argparse.Namespace, database: str, query: str) -> List[List[str]]:
    command = mysql_command(args, [database, "-e", query])
    result = subprocess.run(
        command,
        text=True,
        encoding="utf-8",
        errors="replace",
        stdout=subprocess.PIPE,
        stderr=subprocess.PIPE,
        check=False,
    )
    if result.returncode != 0:
        raise RuntimeError(result.stderr.strip() or f"mysql exited with {result.returncode}")
    rows: List[List[str]] = []
    for line in result.stdout.splitlines():
        if not line:
            continue
        rows.append(line.split("\t"))
    return rows


def chunked(values: Sequence[int], size: int = 500) -> Iterable[Sequence[int]]:
    for index in range(0, len(values), size):
        yield values[index : index + size]


def load_vendor_rows(args: argparse.Namespace) -> Tuple[List[VendorRow], Dict[str, int]]:
    status_counter: Counter[str] = Counter()
    latest_by_creature_item: Dict[Tuple[int, int], Dict[str, str]] = {}
    input_count = 0

    allowed_statuses = set(SAFE_MATCH_STATUSES)
    if args.include_ambiguous:
        allowed_statuses.add("ambiguous_name")
    if args.include_unmatched:
        allowed_statuses.add("unmatched")

    with args.vendor_map.open("r", encoding="utf-8", newline="") as handle:
        reader = csv.DictReader(handle)
        for row in reader:
            input_count += 1
            match_status = row.get("match_status", "")
            status_counter[match_status] += 1
            if match_status not in allowed_statuses:
                continue
            creature2_id = to_int(row.get("creature2_id"))
            item2_id = to_int(row.get("item2_id"))
            if creature2_id == 0 or item2_id == 0:
                continue
            key = (creature2_id, item2_id)
            current = latest_by_creature_item.get(key)
            if current is None:
                latest_by_creature_item[key] = row
                continue
            current_rank = (to_int(current.get("game_version")), to_int(current.get("vendor_item_source_id")))
            next_rank = (to_int(row.get("game_version")), to_int(row.get("vendor_item_source_id")))
            if next_rank > current_rank:
                latest_by_creature_item[key] = row

    rows_by_creature: Dict[int, List[Dict[str, str]]] = defaultdict(list)
    for (creature2_id, _), row in latest_by_creature_item.items():
        rows_by_creature[creature2_id].append(row)

    output: List[VendorRow] = []
    for creature2_id in sorted(rows_by_creature):
        source_rows = sorted(
            rows_by_creature[creature2_id],
            key=lambda item: (
                str(item.get("source_name", "")),
                to_int(item.get("item2_id")),
                to_int(item.get("vendor_item_source_id")),
            ),
        )
        for index, row in enumerate(source_rows, start=1):
            main_currency_id = to_int(row.get("main_currency_id"))
            main_price = to_int(row.get("main_price"))
            alt_currency_id = to_int(row.get("alt_currency_id"))
            alt_price = to_int(row.get("alt_price"))
            output.append(
                VendorRow(
                    creature2_id=creature2_id,
                    item2_id=to_int(row.get("item2_id")),
                    item_index=index,
                    source_id=to_int(row.get("vendor_item_source_id")),
                    source_name=str(row.get("source_name", "")),
                    item_name=str(row.get("item_name", "")),
                    match_status=str(row.get("match_status", "")),
                    game_version=to_int(row.get("game_version")),
                    cost1_type=2 if main_currency_id and main_price > 0 else 0,
                    cost1_quantity=main_price if main_currency_id and main_price > 0 else 0,
                    cost1_id=main_currency_id if main_currency_id and main_price > 0 else 0,
                    cost2_type=2 if alt_currency_id and alt_price > 0 else 0,
                    cost2_quantity=alt_price if alt_currency_id and alt_price > 0 else 0,
                    cost2_id=alt_currency_id if alt_currency_id and alt_price > 0 else 0,
                )
            )

    stats = {
        "input_rows": input_count,
        "rows_after_status_filter": sum(
            count for status, count in status_counter.items() if status in allowed_statuses
        ),
        "deduped_creature_item_rows": len(output),
        "allowed_statuses": ",".join(sorted(allowed_statuses)),
    }
    for status, count in sorted(status_counter.items()):
        stats[f"source_status_{status}"] = count
    return output, stats


def load_world_entities(args: argparse.Namespace, creature_ids: Sequence[int]) -> Dict[int, List[int]]:
    entities_by_creature: Dict[int, List[int]] = defaultdict(list)
    if not creature_ids:
        return entities_by_creature
    for group in chunked(sorted(set(creature_ids))):
        id_list = ",".join(str(value) for value in group)
        query = f"SELECT creature, id FROM entity WHERE creature IN ({id_list}) ORDER BY creature, id;"
        for creature, entity_id in mysql_rows(args, args.world_db, query):
            entities_by_creature[to_int(creature)].append(to_int(entity_id))
    return entities_by_creature


def load_existing_keys(args: argparse.Namespace, entity_ids: Sequence[int]) -> Tuple[set[int], set[Tuple[int, int]], set[Tuple[int, int]]]:
    vendor_ids: set[int] = set()
    category_keys: set[Tuple[int, int]] = set()
    item_keys: set[Tuple[int, int]] = set()
    if not entity_ids:
        return vendor_ids, category_keys, item_keys
    for group in chunked(sorted(set(entity_ids))):
        id_list = ",".join(str(value) for value in group)
        for (entity_id,) in mysql_rows(args, args.world_db, f"SELECT id FROM entity_vendor WHERE id IN ({id_list});"):
            vendor_ids.add(to_int(entity_id))
        for entity_id, index in mysql_rows(args, args.world_db, f"SELECT id, `index` FROM entity_vendor_category WHERE id IN ({id_list});"):
            category_keys.add((to_int(entity_id), to_int(index)))
        for entity_id, index in mysql_rows(args, args.world_db, f"SELECT id, `index` FROM entity_vendor_item WHERE id IN ({id_list});"):
            item_keys.add((to_int(entity_id), to_int(index)))
    return vendor_ids, category_keys, item_keys


def build_summary(
    vendor_rows: Sequence[VendorRow],
    stats: Dict[str, int],
    entities_by_creature: Dict[int, List[int]],
    existing_vendor_ids: set[int],
    existing_category_keys: set[Tuple[int, int]],
    existing_item_keys: set[Tuple[int, int]],
) -> Dict[str, object]:
    matched_rows = [row for row in vendor_rows if row.creature2_id in entities_by_creature]
    entity_ids = sorted({entity_id for ids in entities_by_creature.values() for entity_id in ids})
    planned_item_keys = {
        (entity_id, row.item_index)
        for row in vendor_rows
        for entity_id in entities_by_creature.get(row.creature2_id, [])
    }
    summary: Dict[str, object] = dict(stats)
    summary.update(
        {
            "mapped_vendor_creatures": len({row.creature2_id for row in vendor_rows}),
            "mapped_vendor_rows_matching_world_entities": len(matched_rows),
            "world_vendor_creatures_matched": len(entities_by_creature),
            "world_vendor_entities_matched": len(entity_ids),
            "existing_entity_vendor_rows_for_matched_entities": len(existing_vendor_ids),
            "existing_entity_vendor_category_rows_for_matched_entities": len(existing_category_keys),
            "existing_entity_vendor_item_rows_for_matched_entities": len(existing_item_keys),
            "would_insert_entity_vendor_rows": len([entity_id for entity_id in entity_ids if entity_id not in existing_vendor_ids]),
            "would_insert_default_category_rows": len([entity_id for entity_id in entity_ids if (entity_id, 0) not in existing_category_keys]),
            "would_insert_entity_vendor_item_rows": len(planned_item_keys - existing_item_keys),
        }
    )
    return summary


def build_apply_sql(
    args: argparse.Namespace,
    vendor_rows: Sequence[VendorRow],
    entities_by_creature: Dict[int, List[int]],
) -> str:
    applicable = [row for row in vendor_rows if row.creature2_id in entities_by_creature]
    lines = [
        f"USE `{args.world_db}`;",
        "START TRANSACTION;",
        "CREATE TEMPORARY TABLE tmp_nf_vendor_item_import (",
        "  creature2_id INT UNSIGNED NOT NULL,",
        "  item_index INT UNSIGNED NOT NULL,",
        "  item2_id INT UNSIGNED NOT NULL,",
        "  extraCost1Type TINYINT UNSIGNED NOT NULL,",
        "  extraCost1Quantity INT UNSIGNED NOT NULL,",
        "  extraCost1ItemOrCurrencyId INT UNSIGNED NOT NULL,",
        "  extraCost2Type TINYINT UNSIGNED NOT NULL,",
        "  extraCost2Quantity INT UNSIGNED NOT NULL,",
        "  extraCost2ItemOrCurrencyId INT UNSIGNED NOT NULL,",
        "  source_id INT UNSIGNED NOT NULL,",
        "  source_name VARCHAR(255) NOT NULL,",
        "  item_name VARCHAR(255) NOT NULL,",
        "  match_status VARCHAR(32) NOT NULL,",
        "  game_version INT UNSIGNED NOT NULL,",
        "  PRIMARY KEY (creature2_id, item_index),",
        "  KEY ix_tmp_nf_vendor_item_import_item (item2_id)",
        ") ENGINE=MEMORY;",
    ]

    for group in chunked(list(range(len(applicable))), 500):
        values = []
        for row_index in group:
            row = applicable[row_index]
            values.append(
                "("
                f"{row.creature2_id},{row.item_index},{row.item2_id},"
                f"{row.cost1_type},{row.cost1_quantity},{row.cost1_id},"
                f"{row.cost2_type},{row.cost2_quantity},{row.cost2_id},"
                f"{row.source_id},{sql_string(row.source_name)},{sql_string(row.item_name)},"
                f"{sql_string(row.match_status)},{row.game_version}"
                ")"
            )
        lines.append(
            "INSERT INTO tmp_nf_vendor_item_import "
            "(creature2_id,item_index,item2_id,extraCost1Type,extraCost1Quantity,extraCost1ItemOrCurrencyId,"
            "extraCost2Type,extraCost2Quantity,extraCost2ItemOrCurrencyId,source_id,source_name,item_name,match_status,game_version) VALUES\n"
            + ",\n".join(values)
            + ";"
        )

    affected_creatures = sorted(entities_by_creature)
    if args.replace_existing and affected_creatures:
        creature_list = ",".join(str(value) for value in affected_creatures)
        lines.extend(
            [
                "DELETE evi FROM entity_vendor_item evi JOIN entity e ON e.id = evi.id "
                f"WHERE e.creature IN ({creature_list});",
                "DELETE evc FROM entity_vendor_category evc JOIN entity e ON e.id = evc.id "
                f"WHERE e.creature IN ({creature_list});",
                "DELETE ev FROM entity_vendor ev JOIN entity e ON e.id = ev.id "
                f"WHERE e.creature IN ({creature_list});",
            ]
        )

    lines.extend(
        [
            "INSERT IGNORE INTO entity_vendor (id, buyPriceMultiplier, sellPriceMultiplier)",
            "SELECT DISTINCT e.id, 1, 1",
            "FROM entity e",
            "JOIN tmp_nf_vendor_item_import m ON m.creature2_id = e.creature;",
            "",
            "INSERT IGNORE INTO entity_vendor_category (id, `index`, localisedTextId)",
            "SELECT DISTINCT e.id, 0, 0",
            "FROM entity e",
            "JOIN tmp_nf_vendor_item_import m ON m.creature2_id = e.creature;",
            "",
            "INSERT IGNORE INTO entity_vendor_item (",
            "  id, `index`, categoryIndex, itemId,",
            "  extraCost1ItemOrCurrencyId, extraCost1Quantity, extraCost1Type,",
            "  extraCost2ItemOrCurrencyId, extraCost2Quantity, extraCost2Type",
            ")",
            "SELECT",
            "  e.id,",
            "  m.item_index,",
            "  0,",
            "  m.item2_id,",
            "  m.extraCost1ItemOrCurrencyId,",
            "  m.extraCost1Quantity,",
            "  m.extraCost1Type,",
            "  m.extraCost2ItemOrCurrencyId,",
            "  m.extraCost2Quantity,",
            "  m.extraCost2Type",
            "FROM entity e",
            "JOIN tmp_nf_vendor_item_import m ON m.creature2_id = e.creature;",
            "COMMIT;",
        ]
    )
    return "\n".join(lines) + "\n"


def run_apply_sql(args: argparse.Namespace, sql: str) -> None:
    command = mysql_command(args)
    result = subprocess.run(
        command,
        input=sql,
        text=True,
        encoding="utf-8",
        errors="replace",
        stdout=subprocess.PIPE,
        stderr=subprocess.PIPE,
        check=False,
    )
    if result.returncode != 0:
        raise RuntimeError(result.stderr.strip() or f"mysql exited with {result.returncode}")
    if result.stdout.strip():
        print(result.stdout.strip())
    if result.stderr.strip():
        print(result.stderr.strip(), file=sys.stderr)


def parse_args(argv: Optional[Sequence[str]] = None) -> argparse.Namespace:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--repo-root", type=Path, default=Path.cwd())
    parser.add_argument("--vendor-map", type=Path)
    parser.add_argument("--report-out", type=Path)
    parser.add_argument("--sql-out", type=Path)
    parser.add_argument("--mysql-exe", type=Path, default=Path(DEFAULT_MYSQL))
    parser.add_argument("--host", default="127.0.0.1")
    parser.add_argument("--port", type=int, default=3306)
    parser.add_argument("--user", default="bankai")
    parser.add_argument("--password", default="bankai")
    parser.add_argument("--world-db", default="nexus_forever_world")
    parser.add_argument("--include-ambiguous", action="store_true")
    parser.add_argument("--include-unmatched", action="store_true")
    parser.add_argument("--replace-existing", action="store_true")
    parser.add_argument("--apply", action="store_true", help="Actually apply the generated import SQL. Omit for dry-run.")
    args = parser.parse_args(argv)
    args.repo_root = args.repo_root.resolve()
    args.vendor_map = (args.vendor_map or args.repo_root / "Tools" / "DataMapping" / "output" / "vendor_item_map.csv").resolve()
    args.report_out = (args.report_out or args.repo_root / "Tools" / "DataMapping" / "output" / "vendor_item_apply_report.json").resolve()
    if args.sql_out:
        args.sql_out = args.sql_out.resolve()
    return args


def main(argv: Optional[Sequence[str]] = None) -> int:
    args = parse_args(argv)
    if not args.vendor_map.exists():
        raise FileNotFoundError(args.vendor_map)
    if not args.mysql_exe.exists():
        raise FileNotFoundError(args.mysql_exe)

    vendor_rows, stats = load_vendor_rows(args)
    entities_by_creature = load_world_entities(args, [row.creature2_id for row in vendor_rows])
    entity_ids = sorted({entity_id for ids in entities_by_creature.values() for entity_id in ids})
    existing_vendor_ids, existing_category_keys, existing_item_keys = load_existing_keys(args, entity_ids)
    summary = build_summary(vendor_rows, stats, entities_by_creature, existing_vendor_ids, existing_category_keys, existing_item_keys)
    summary["generated_at"] = time.strftime("%Y-%m-%d %H:%M:%S")
    summary["world_db"] = args.world_db
    summary["vendor_map"] = str(args.vendor_map)
    summary["mode"] = "apply" if args.apply else "dry-run"
    summary["replace_existing"] = args.replace_existing

    args.report_out.parent.mkdir(parents=True, exist_ok=True)
    args.report_out.write_text(json.dumps(summary, indent=2), encoding="utf-8")

    print(json.dumps(summary, indent=2))

    if args.sql_out or args.apply:
        sql = build_apply_sql(args, vendor_rows, entities_by_creature)
        if args.sql_out:
            args.sql_out.parent.mkdir(parents=True, exist_ok=True)
            args.sql_out.write_text(sql, encoding="utf-8")
            print(f"Wrote SQL to {args.sql_out}")
        if args.apply:
            run_apply_sql(args, sql)
            print("Applied vendor item import.")

    return 0


if __name__ == "__main__":
    raise SystemExit(main())
