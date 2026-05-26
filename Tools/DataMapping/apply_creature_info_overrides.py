#!/usr/bin/env python3
"""
Apply high-confidence creature template overrides to the NexusForever world DB.

This imports only data that fits existing runtime tables:

* creature_info_property: BaseHealth and ShieldCapacityMax
* creature_info_stat: InterruptArmour

By default the script inserts missing rows only. Use --replace-existing to
refresh just these owned properties/stats from creature_map.csv.
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
from typing import Dict, Iterable, Optional, Sequence, Tuple


DEFAULT_MYSQL = r"C:\Program Files\MySQL\MySQL Server 8.0\bin\mysql.exe"
SAFE_MATCH_STATUSES = {"unique_name", "scored_name", "reviewed"}

PROPERTY_BASE_HEALTH = 7
PROPERTY_SHIELD_CAPACITY_MAX = 41
STAT_INTERRUPT_ARMOUR = 21


@dataclass(frozen=True)
class OverrideRow:
    creature2_id: int
    override_id: int
    value: str
    source_name: str
    match_status: str
    last_seen_in: int


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


def to_float_text(value) -> Optional[str]:
    if value is None:
        return None
    text = str(value).strip()
    if not text:
        return None
    try:
        number = float(text)
    except ValueError:
        return None
    if number <= 0:
        return None
    if number.is_integer():
        return str(int(number))
    return f"{number:.6f}".rstrip("0").rstrip(".")


def template_health_text(row: Dict[str, str]) -> Optional[str]:
    template_health = to_float_text(row.get("template_base_health"))
    if template_health:
        return template_health

    health_min = to_float_text(row.get("health_min"))
    health_max = to_float_text(row.get("health_max"))
    if health_min and health_max:
        if health_min == health_max:
            return health_min
        return None
    return health_min or health_max


def has_skipped_health_range(row: Dict[str, str]) -> bool:
    health_min = to_float_text(row.get("health_min"))
    health_max = to_float_text(row.get("health_max"))
    return bool(health_min and health_max and health_min != health_max)


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


def add_candidate(
    candidates: Dict[Tuple[int, int], list[OverrideRow]],
    creature2_id: int,
    override_id: int,
    value: Optional[str],
    row: dict[str, str],
) -> None:
    if not value:
        return
    candidates[(creature2_id, override_id)].append(
        OverrideRow(
            creature2_id=creature2_id,
            override_id=override_id,
            value=value,
            source_name=str(row.get("source_name", "")),
            match_status=str(row.get("match_status", "")),
            last_seen_in=to_int(row.get("last_seen_in")),
        )
    )


def resolve_candidates(
    candidates: Dict[Tuple[int, int], list[OverrideRow]]
) -> Tuple[list[OverrideRow], int, list[dict[str, object]]]:
    rows: list[OverrideRow] = []
    conflicts: list[dict[str, object]] = []

    for key, values in sorted(candidates.items()):
        distinct_values = {candidate.value for candidate in values}
        if len(distinct_values) != 1:
            conflicts.append(
                {
                    "creature2_id": key[0],
                    "override_id": key[1],
                    "values": sorted(distinct_values),
                    "source_names": sorted({candidate.source_name for candidate in values})[:10],
                }
            )
            continue

        rows.append(
            sorted(
                values,
                key=lambda candidate: (
                    candidate.last_seen_in,
                    {"reviewed": 2, "unique_name": 1}.get(candidate.match_status, 0),
                    candidate.source_name,
                ),
                reverse=True,
            )[0]
        )

    return rows, len(conflicts), conflicts[:25]


def load_override_rows(
    args: argparse.Namespace,
) -> Tuple[list[OverrideRow], list[OverrideRow], Dict[str, object]]:
    allowed_statuses = set(SAFE_MATCH_STATUSES)
    if args.include_ambiguous:
        allowed_statuses.add("ambiguous_name")
    if args.include_unmatched:
        allowed_statuses.add("unmatched")

    status_counter: Counter[str] = Counter()
    property_candidates: Dict[Tuple[int, int], list[OverrideRow]] = defaultdict(list)
    stat_candidates: Dict[Tuple[int, int], list[OverrideRow]] = defaultdict(list)
    input_count = 0
    rows_after_status_filter = 0
    ranged_health_rows_skipped = 0

    with args.creature_map.open("r", encoding="utf-8", newline="") as handle:
        reader = csv.DictReader(handle)
        for row in reader:
            input_count += 1
            match_status = row.get("match_status", "")
            status_counter[match_status] += 1
            if match_status not in allowed_statuses:
                continue
            rows_after_status_filter += 1

            creature2_id = to_int(row.get("creature2_id"))
            if creature2_id == 0:
                continue

            health = template_health_text(row)
            if health is None and has_skipped_health_range(row):
                ranged_health_rows_skipped += 1
            shield = to_float_text(row.get("shield"))
            interrupt_armour = to_float_text(row.get("interrupt_armor_max"))

            add_candidate(property_candidates, creature2_id, PROPERTY_BASE_HEALTH, health, row)
            add_candidate(property_candidates, creature2_id, PROPERTY_SHIELD_CAPACITY_MAX, shield, row)
            add_candidate(stat_candidates, creature2_id, STAT_INTERRUPT_ARMOUR, interrupt_armour, row)

    property_rows, property_conflict_count, property_conflicts = resolve_candidates(property_candidates)
    stat_rows, stat_conflict_count, stat_conflicts = resolve_candidates(stat_candidates)

    stats: Dict[str, object] = {
        "input_rows": input_count,
        "rows_after_status_filter": rows_after_status_filter,
        "allowed_statuses": ",".join(sorted(allowed_statuses)),
        "property_candidate_keys": len(property_candidates),
        "stat_candidate_keys": len(stat_candidates),
        "ranged_health_rows_skipped": ranged_health_rows_skipped,
        "property_conflict_keys_skipped": property_conflict_count,
        "stat_conflict_keys_skipped": stat_conflict_count,
        "property_conflict_examples": property_conflicts,
        "stat_conflict_examples": stat_conflicts,
    }
    for status, count in sorted(status_counter.items()):
        stats[f"source_status_{status}"] = count

    return property_rows, stat_rows, stats


def table_exists(args: argparse.Namespace, table_name: str) -> bool:
    query = (
        "SELECT COUNT(*) FROM information_schema.TABLES "
        f"WHERE TABLE_SCHEMA = {sql_string(args.world_db)} AND TABLE_NAME = {sql_string(table_name)};"
    )
    return to_int(mysql_rows(args, "information_schema", query)[0][0]) > 0


def existing_count(args: argparse.Namespace, table_name: str) -> int:
    if not table_exists(args, table_name):
        return 0
    return to_int(mysql_rows(args, args.world_db, f"SELECT COUNT(*) FROM `{table_name}`;")[0][0])


def existing_owned_count(args: argparse.Namespace, table_name: str, id_column: str, ids: Sequence[int]) -> int:
    if not table_exists(args, table_name):
        return 0
    id_list = ",".join(str(value) for value in ids)
    query = f"SELECT COUNT(*) FROM `{table_name}` WHERE `{id_column}` IN ({id_list});"
    return to_int(mysql_rows(args, args.world_db, query)[0][0])


def create_tables_sql(args: argparse.Namespace) -> str:
    replace_sql = ""
    if args.replace_existing:
        replace_sql = f"""
DELETE FROM creature_info_property
WHERE `property` IN ({PROPERTY_BASE_HEALTH},{PROPERTY_SHIELD_CAPACITY_MAX});
DELETE FROM creature_info_stat
WHERE `stat` IN ({STAT_INTERRUPT_ARMOUR});
""".strip()

    return f"""
USE `{args.world_db}`;
CREATE TABLE IF NOT EXISTS creature_info_property (
  `id` int(10) unsigned NOT NULL,
  `property` tinyint(3) unsigned NOT NULL,
  `value` float NOT NULL,
  PRIMARY KEY (`id`, `property`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

SET @hasOldCreatureInfoPropertyValue = (
  SELECT COUNT(*)
  FROM information_schema.columns
  WHERE table_schema = DATABASE()
    AND table_name = 'creature_info_property'
    AND column_name = 'alue'
);
SET @hasCreatureInfoPropertyValue = (
  SELECT COUNT(*)
  FROM information_schema.columns
  WHERE table_schema = DATABASE()
    AND table_name = 'creature_info_property'
    AND column_name = 'value'
);
SET @repairCreatureInfoPropertyValue = IF(
  @hasOldCreatureInfoPropertyValue > 0 AND @hasCreatureInfoPropertyValue = 0,
  'ALTER TABLE creature_info_property CHANGE COLUMN `alue` `value` float NOT NULL',
  'SELECT 1'
);
PREPARE repairCreatureInfoPropertyValueStatement FROM @repairCreatureInfoPropertyValue;
EXECUTE repairCreatureInfoPropertyValueStatement;
DEALLOCATE PREPARE repairCreatureInfoPropertyValueStatement;

CREATE TABLE IF NOT EXISTS creature_info_stat (
  `id` int(10) unsigned NOT NULL,
  `stat` tinyint(3) unsigned NOT NULL,
  `value` float NOT NULL,
  PRIMARY KEY (`id`, `stat`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;
{replace_sql}
""".strip() + "\n"


def chunked(values: Sequence[OverrideRow], size: int) -> Iterable[Sequence[OverrideRow]]:
    for index in range(0, len(values), size):
        yield values[index : index + size]


def insert_sql(table_name: str, id_column: str, rows: Sequence[OverrideRow], replace_existing: bool) -> Iterable[str]:
    if not rows:
        return

    verb = "INSERT INTO" if replace_existing else "INSERT IGNORE INTO"
    yield f"{verb} {table_name} (`id`,`{id_column}`,`value`) VALUES\n"
    yield ",\n".join(f"({row.creature2_id},{row.override_id},{row.value})" for row in rows)
    if replace_existing:
        yield "\nON DUPLICATE KEY UPDATE `value`=VALUES(`value`);\n"
    else:
        yield ";\n"


def build_sql(
    args: argparse.Namespace,
    property_rows: Sequence[OverrideRow],
    stat_rows: Sequence[OverrideRow],
) -> Iterable[str]:
    yield create_tables_sql(args)
    yield "START TRANSACTION;\n"
    for group in chunked(property_rows, args.chunk_size):
        yield from insert_sql("creature_info_property", "property", group, args.replace_existing)
    for group in chunked(stat_rows, args.chunk_size):
        yield from insert_sql("creature_info_stat", "stat", group, args.replace_existing)
    yield "COMMIT;\n"


def apply_sql(
    args: argparse.Namespace,
    property_rows: Sequence[OverrideRow],
    stat_rows: Sequence[OverrideRow],
) -> None:
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
    for part in build_sql(args, property_rows, stat_rows):
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
    parser.add_argument("--creature-map", type=Path)
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
    args.creature_map = (
        args.creature_map or args.repo_root / "Tools" / "DataMapping" / "output" / "creature_map.csv"
    ).resolve()
    args.report_out = (
        args.report_out or args.repo_root / "Tools" / "DataMapping" / "output" / "creature_info_apply_report.json"
    ).resolve()
    if args.sql_out:
        args.sql_out = args.sql_out.resolve()
    return args


def write_report(args: argparse.Namespace, summary: Dict[str, object]) -> None:
    args.report_out.parent.mkdir(parents=True, exist_ok=True)
    args.report_out.write_text(json.dumps(summary, indent=2), encoding="utf-8")
    print(json.dumps(summary, indent=2))


def main(argv: Optional[Sequence[str]] = None) -> int:
    args = parse_args(argv)
    if not args.creature_map.exists():
        raise FileNotFoundError(args.creature_map)
    if not args.mysql_exe.exists():
        raise FileNotFoundError(args.mysql_exe)

    property_rows, stat_rows, stats = load_override_rows(args)
    summary: Dict[str, object] = dict(stats)
    summary.update(
        {
            "generated_at": time.strftime("%Y-%m-%d %H:%M:%S"),
            "world_db": args.world_db,
            "creature_map": str(args.creature_map),
            "mode": "apply" if args.apply else "dry-run",
            "replace_existing": args.replace_existing,
            "creature_info_property_table_exists": table_exists(args, "creature_info_property"),
            "creature_info_stat_table_exists": table_exists(args, "creature_info_stat"),
            "existing_creature_info_property_rows": existing_count(args, "creature_info_property"),
            "existing_creature_info_stat_rows": existing_count(args, "creature_info_stat"),
            "existing_owned_property_rows": existing_owned_count(
                args, "creature_info_property", "property", [PROPERTY_BASE_HEALTH, PROPERTY_SHIELD_CAPACITY_MAX]
            ),
            "existing_owned_stat_rows": existing_owned_count(args, "creature_info_stat", "stat", [STAT_INTERRUPT_ARMOUR]),
            "would_insert_property_rows": len(property_rows),
            "would_insert_stat_rows": len(stat_rows),
            "mapped_property_creatures": len({row.creature2_id for row in property_rows}),
            "mapped_interrupt_armour_creatures": len({row.creature2_id for row in stat_rows}),
        }
    )

    if args.sql_out:
        args.sql_out.parent.mkdir(parents=True, exist_ok=True)
        with args.sql_out.open("w", encoding="utf-8") as handle:
            for part in build_sql(args, property_rows, stat_rows):
                handle.write(part)
        print(f"Wrote SQL to {args.sql_out}")

    if args.apply:
        apply_sql(args, property_rows, stat_rows)
        summary.update(
            {
                "post_creature_info_property_rows": existing_count(args, "creature_info_property"),
                "post_creature_info_stat_rows": existing_count(args, "creature_info_stat"),
                "post_owned_property_rows": existing_owned_count(
                    args, "creature_info_property", "property", [PROPERTY_BASE_HEALTH, PROPERTY_SHIELD_CAPACITY_MAX]
                ),
                "post_owned_stat_rows": existing_owned_count(
                    args, "creature_info_stat", "stat", [STAT_INTERRUPT_ARMOUR]
                ),
            }
        )
        print("Applied creature info override import.")

    write_report(args, summary)
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
