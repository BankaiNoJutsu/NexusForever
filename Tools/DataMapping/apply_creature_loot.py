#!/usr/bin/env python3
"""
Apply mapped creature loot to the NexusForever world database.

This creates/populates a `creature_loot` table from `creature_loot_map.csv`.
Runtime loot generation is separate; this script makes the mapped loot data
available in the real world database in a repeatable, idempotent way.
"""

from __future__ import annotations

import argparse
import csv
import json
import subprocess
import sys
import time
from collections import Counter
from dataclasses import dataclass
from pathlib import Path
from typing import Dict, Iterable, Optional, Sequence, Tuple


DEFAULT_MYSQL = r"C:\Program Files\MySQL\MySQL Server 8.0\bin\mysql.exe"
SAFE_MATCH_STATUSES = {"unique_name", "scored_name", "reviewed"}


@dataclass(frozen=True)
class LootRow:
    creature2_id: int
    item2_id: int
    chance: str
    drop_times: int
    aggregate_drop_sum: int
    aggregate_drop_count: int
    game_version: int
    source_drop_id: int
    versioned_item_drop_aggregate_id: int
    versioned_creature_drop_aggregate_id: int
    last_seen_in: int
    match_status: str
    source_name: str
    item_name: str


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


def to_probability(value) -> str:
    try:
        number = float(str(value).strip())
    except (TypeError, ValueError):
        number = 0.0
    if number < 0:
        number = 0.0
    if number > 1:
        number = 1.0
    return f"{number:.8f}"


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


def load_loot_rows(args: argparse.Namespace) -> Tuple[list[LootRow], Dict[str, int]]:
    status_counter: Counter[str] = Counter()
    latest_by_creature_item: Dict[Tuple[int, int], dict[str, str]] = {}
    input_count = 0
    rows_after_status_filter = 0
    skipped_missing_creature_id_rows = 0
    skipped_missing_item2_id_rows = 0

    allowed_statuses = set(SAFE_MATCH_STATUSES)
    if args.include_ambiguous:
        allowed_statuses.add("ambiguous_name")
    if args.include_unmatched:
        allowed_statuses.add("unmatched")

    with args.loot_map.open("r", encoding="utf-8", newline="") as handle:
        reader = csv.DictReader(handle)
        for row in reader:
            input_count += 1
            match_status = row.get("match_status", "")
            status_counter[match_status] += 1
            if match_status not in allowed_statuses:
                continue
            rows_after_status_filter += 1
            creature2_id = to_int(row.get("creature2_id"))
            item2_id = to_int(row.get("item2_id"))
            if creature2_id == 0:
                skipped_missing_creature_id_rows += 1
                continue
            if item2_id == 0:
                skipped_missing_item2_id_rows += 1
                continue
            key = (creature2_id, item2_id)
            current = latest_by_creature_item.get(key)
            if current is None:
                latest_by_creature_item[key] = row
                continue
            current_rank = (to_int(current.get("game_version")), to_int(current.get("source_drop_id")))
            next_rank = (to_int(row.get("game_version")), to_int(row.get("source_drop_id")))
            if next_rank > current_rank:
                latest_by_creature_item[key] = row

    rows: list[LootRow] = []
    for (creature2_id, item2_id), row in sorted(latest_by_creature_item.items()):
        rows.append(
            LootRow(
                creature2_id=creature2_id,
                item2_id=item2_id,
                chance=to_probability(row.get("drop_probability")),
                drop_times=to_int(row.get("drop_times")),
                aggregate_drop_sum=to_int(row.get("aggregate_drop_sum")),
                aggregate_drop_count=to_int(row.get("aggregate_drop_count")),
                game_version=to_int(row.get("game_version")),
                source_drop_id=to_int(row.get("source_drop_id")),
                versioned_item_drop_aggregate_id=to_int(row.get("versioned_item_drop_aggregate_id")),
                versioned_creature_drop_aggregate_id=to_int(row.get("versioned_creature_drop_aggregate_id")),
                last_seen_in=to_int(row.get("last_seen_in")),
                match_status=str(row.get("match_status", "")),
                source_name=str(row.get("source_name", "")),
                item_name=str(row.get("item_name", "")),
            )
        )

    stats = {
        "input_rows": input_count,
        "rows_after_status_filter": rows_after_status_filter,
        "skipped_status_rows": input_count - rows_after_status_filter,
        "skipped_missing_creature_id_rows": skipped_missing_creature_id_rows,
        "skipped_missing_item2_id_rows": skipped_missing_item2_id_rows,
        "invalid_item2_id_rows": skipped_missing_item2_id_rows,
        "skipped_duplicate_creature_item_rows": (
            rows_after_status_filter
            - skipped_missing_creature_id_rows
            - skipped_missing_item2_id_rows
            - len(rows)
        ),
        "skipped_rows_total": input_count - len(rows),
        "deduped_creature_item_rows": len(rows),
        "allowed_statuses": ",".join(sorted(allowed_statuses)),
    }
    for status, count in sorted(status_counter.items()):
        stats[f"source_status_{status}"] = count
    return rows, stats


def table_exists(args: argparse.Namespace, table_name: str) -> bool:
    query = (
        "SELECT COUNT(*) FROM information_schema.TABLES "
        f"WHERE TABLE_SCHEMA = {sql_string(args.world_db)} AND TABLE_NAME = {sql_string(table_name)};"
    )
    return to_int(mysql_rows(args, "information_schema", query)[0][0]) > 0


def existing_count(args: argparse.Namespace) -> int:
    if not table_exists(args, "creature_loot"):
        return 0
    return to_int(mysql_rows(args, args.world_db, "SELECT COUNT(*) FROM creature_loot;")[0][0])


def load_mapped_flat_creature_ids(args: argparse.Namespace) -> tuple[set[int], str]:
    if not table_exists(args, "entity_loot"):
        return set(), "unavailable: missing entity_loot table"
    if not table_exists(args, "loot_group"):
        return set(), "unavailable: missing loot_group table"

    query = """
SELECT DISTINCT el.id
FROM entity_loot el
JOIN loot_group lg ON lg.id = el.lootGroupId
WHERE lg.comment LIKE 'DataMapping creature_loot%';
""".strip()
    return {to_int(row[0]) for row in mysql_rows(args, args.world_db, query)}, "available"


def create_table_sql(args: argparse.Namespace) -> str:
    replace_sql = "TRUNCATE TABLE creature_loot;" if args.replace_existing else ""
    return f"""
USE `{args.world_db}`;
CREATE TABLE IF NOT EXISTS creature_loot (
  creatureId INT UNSIGNED NOT NULL,
  itemId INT UNSIGNED NOT NULL,
  chance DECIMAL(12,8) NOT NULL DEFAULT 0,
  dropTimes INT UNSIGNED NOT NULL DEFAULT 0,
  aggregateDropSum INT UNSIGNED NOT NULL DEFAULT 0,
  aggregateDropCount INT UNSIGNED NOT NULL DEFAULT 0,
  gameVersion INT UNSIGNED NOT NULL DEFAULT 0,
  sourceDropId INT UNSIGNED NOT NULL DEFAULT 0,
  versionedItemDropAggregateId INT UNSIGNED NOT NULL DEFAULT 0,
  versionedCreatureDropAggregateId INT UNSIGNED NOT NULL DEFAULT 0,
  lastSeenIn INT UNSIGNED NOT NULL DEFAULT 0,
  matchStatus VARCHAR(32) NOT NULL DEFAULT '',
  sourceName VARCHAR(255) NOT NULL DEFAULT '',
  itemName VARCHAR(255) NOT NULL DEFAULT '',
  PRIMARY KEY (creatureId, itemId),
  KEY ix_creature_loot_item (itemId),
  KEY ix_creature_loot_chance (chance)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
{replace_sql}
""".strip() + "\n"


def insert_sql(rows: Sequence[LootRow]) -> Iterable[str]:
    yield (
        "INSERT INTO creature_loot ("
        "creatureId,itemId,chance,dropTimes,aggregateDropSum,aggregateDropCount,gameVersion,sourceDropId,"
        "versionedItemDropAggregateId,versionedCreatureDropAggregateId,lastSeenIn,matchStatus,sourceName,itemName"
        ") VALUES\n"
    )
    values = []
    for row in rows:
        values.append(
            "("
            f"{row.creature2_id},{row.item2_id},{row.chance},{row.drop_times},{row.aggregate_drop_sum},"
            f"{row.aggregate_drop_count},{row.game_version},{row.source_drop_id},"
            f"{row.versioned_item_drop_aggregate_id},{row.versioned_creature_drop_aggregate_id},"
            f"{row.last_seen_in},{sql_string(row.match_status)},{sql_string(row.source_name)},{sql_string(row.item_name)}"
            ")"
        )
    yield ",\n".join(values)
    yield (
        "\nON DUPLICATE KEY UPDATE "
        "chance=VALUES(chance), dropTimes=VALUES(dropTimes), aggregateDropSum=VALUES(aggregateDropSum), "
        "aggregateDropCount=VALUES(aggregateDropCount), gameVersion=VALUES(gameVersion), sourceDropId=VALUES(sourceDropId), "
        "versionedItemDropAggregateId=VALUES(versionedItemDropAggregateId), "
        "versionedCreatureDropAggregateId=VALUES(versionedCreatureDropAggregateId), lastSeenIn=VALUES(lastSeenIn), "
        "matchStatus=VALUES(matchStatus), sourceName=VALUES(sourceName), itemName=VALUES(itemName);\n"
    )


def chunked(values: Sequence[LootRow], size: int = 1000) -> Iterable[Sequence[LootRow]]:
    for index in range(0, len(values), size):
        yield values[index : index + size]


def build_sql(args: argparse.Namespace, rows: Sequence[LootRow]) -> Iterable[str]:
    yield create_table_sql(args)
    yield "START TRANSACTION;\n"
    for group in chunked(rows, args.chunk_size):
        yield from insert_sql(group)
    yield "COMMIT;\n"


def apply_sql(args: argparse.Namespace, rows: Sequence[LootRow]) -> None:
    process = subprocess.Popen(
        mysql_command(args),
        stdin=subprocess.PIPE,
        stdout=subprocess.PIPE,
        stderr=subprocess.PIPE,
        text=True,
        encoding="utf-8",
        errors="replace",
    )
    assert process.stdin is not None
    for part in build_sql(args, rows):
        process.stdin.write(part)
    process.stdin.close()
    stdout = process.stdout.read() if process.stdout is not None else ""
    stderr = process.stderr.read() if process.stderr is not None else ""
    return_code = process.wait()
    if stdout.strip():
        print(stdout.strip())
    if stderr.strip():
        print(stderr.strip(), file=sys.stderr)
    if return_code != 0:
        raise RuntimeError(f"mysql exited with {return_code}")


def parse_args(argv: Optional[Sequence[str]] = None) -> argparse.Namespace:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--repo-root", type=Path, default=Path.cwd())
    parser.add_argument("--loot-map", type=Path)
    parser.add_argument("--report-out", type=Path)
    parser.add_argument("--sql-out", type=Path)
    parser.add_argument("--mysql-exe", type=Path, default=Path(DEFAULT_MYSQL))
    parser.add_argument("--host", default="127.0.0.1")
    parser.add_argument("--port", type=int, default=3306)
    parser.add_argument("--user", default="bankai")
    parser.add_argument("--password", default="bankai")
    parser.add_argument("--world-db", default="nexus_forever_world")
    parser.add_argument("--chunk-size", type=int, default=1000)
    parser.add_argument("--include-ambiguous", action="store_true")
    parser.add_argument("--include-unmatched", action="store_true")
    parser.add_argument("--replace-existing", action="store_true")
    parser.add_argument("--apply", action="store_true", help="Actually apply the generated import SQL. Omit for dry-run.")
    args = parser.parse_args(argv)
    args.repo_root = args.repo_root.resolve()
    args.loot_map = (args.loot_map or args.repo_root / "Tools" / "DataMapping" / "output" / "creature_loot_map.csv").resolve()
    args.report_out = (args.report_out or args.repo_root / "Tools" / "DataMapping" / "output" / "creature_loot_apply_report.json").resolve()
    if args.sql_out:
        args.sql_out = args.sql_out.resolve()
    return args


def main(argv: Optional[Sequence[str]] = None) -> int:
    args = parse_args(argv)
    if not args.loot_map.exists():
        raise FileNotFoundError(args.loot_map)
    if not args.mysql_exe.exists():
        raise FileNotFoundError(args.mysql_exe)

    rows, stats = load_loot_rows(args)
    mapped_flat_creature_ids, mapped_flat_report_status = load_mapped_flat_creature_ids(args)
    runtime_skipped_by_flat_group = [
        row for row in rows
        if row.creature2_id in mapped_flat_creature_ids
    ]
    summary: Dict[str, object] = dict(stats)
    summary.update(
        {
            "generated_at": time.strftime("%Y-%m-%d %H:%M:%S"),
            "world_db": args.world_db,
            "loot_map": str(args.loot_map),
            "mode": "apply" if args.apply else "dry-run",
            "replace_existing": args.replace_existing,
            "creature_loot_table_exists": table_exists(args, "creature_loot"),
            "existing_creature_loot_rows": existing_count(args),
            "would_upsert_creature_loot_rows": len(rows),
            "mapped_loot_creatures": len({row.creature2_id for row in rows}),
            "mapped_loot_items": len({row.item2_id for row in rows}),
            "mapped_flat_group_report_status": mapped_flat_report_status,
            "existing_mapped_flat_loot_group_creatures": len(mapped_flat_creature_ids),
            "runtime_direct_rows_skipped_by_existing_mapped_flat_groups": len(runtime_skipped_by_flat_group),
            "runtime_direct_creatures_skipped_by_existing_mapped_flat_groups": len({
                row.creature2_id for row in runtime_skipped_by_flat_group
            }),
        }
    )

    args.report_out.parent.mkdir(parents=True, exist_ok=True)
    args.report_out.write_text(json.dumps(summary, indent=2), encoding="utf-8")
    print(json.dumps(summary, indent=2))

    if args.sql_out:
        args.sql_out.parent.mkdir(parents=True, exist_ok=True)
        with args.sql_out.open("w", encoding="utf-8") as handle:
            for part in build_sql(args, rows):
                handle.write(part)
        print(f"Wrote SQL to {args.sql_out}")

    if args.apply:
        apply_sql(args, rows)
        print("Applied creature loot import.")

    return 0


if __name__ == "__main__":
    raise SystemExit(main())
