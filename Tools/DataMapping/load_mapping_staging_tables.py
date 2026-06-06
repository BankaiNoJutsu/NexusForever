#!/usr/bin/env python3
"""
Load curated mapping CSV outputs into nf_map_* tables in the authoring database.

The nf_map_* tables are staging/reference tables. Loading them into the real
runtime world database is no longer the default; clean deployments should not
need these tables at all.
"""

from __future__ import annotations

import argparse
import csv
import json
import re
import shutil
import subprocess
import sys
import tempfile
import time
from dataclasses import dataclass
from pathlib import Path
from typing import Dict, Iterable, Optional, Sequence


DEFAULT_MYSQL = r"C:\Program Files\MySQL\MySQL Server 8.0\bin\mysql.exe"
DEFAULT_MAPPING_DB = "nexus_forever_mapping"
SPECIAL_CSV_NAMES = {
    "nf_map_world_entity_candidate": "world_entity_candidate.csv",
    "nf_map_world_entity_stats_candidate": "world_entity_stats_candidate.csv",
    "nf_map_table_coverage_inventory": "table_coverage_inventory.csv",
}
NUMERIC_TYPE_MARKERS = (
    "INT",
    "FLOAT",
    "DOUBLE",
    "DECIMAL",
    "NUMERIC",
    "BIT",
)


@dataclass(frozen=True)
class ColumnSpec:
    name: str
    type_sql: str
    is_numeric: bool


@dataclass(frozen=True)
class TableSpec:
    name: str
    create_sql: str
    columns: list[ColumnSpec]


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


def sql_identifier(value: str) -> str:
    if not re.fullmatch(r"[A-Za-z0-9_]+", value):
        raise ValueError(f"Unsafe MySQL identifier {value!r}. Use letters, numbers, and underscores only.")
    return f"`{value}`"


def mysql_command(args: argparse.Namespace, extra: Optional[Sequence[str]] = None) -> list[str]:
    command = [
        str(args.mysql_exe),
        f"--host={args.host}",
        f"--port={args.port}",
        f"--user={args.user}",
        f"--password={args.password}",
        "--default-character-set=utf8mb4",
        "--local-infile=1",
        "--batch",
        "--raw",
        "--skip-column-names",
    ]
    if extra:
        command.extend(extra)
    return command


def mysql_execute(args: argparse.Namespace, database: str, sql: str) -> str:
    result = subprocess.run(
        mysql_command(args, [database]),
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
    if result.stderr.strip():
        print(result.stderr.strip(), file=sys.stderr)
    return result.stdout


def mysql_rows(
    args: argparse.Namespace,
    database: str,
    query: str,
    *,
    expected_rows: bool = False,
) -> list[list[str]]:
    for attempt in range(3):
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
        rows = [line.split("\t") for line in result.stdout.splitlines() if line]
        if rows or not expected_rows or attempt == 2:
            return rows
        time.sleep(0.25)
    return []


def get_local_infile(args: argparse.Namespace) -> str:
    rows = mysql_rows(args, args.world_db, "SHOW GLOBAL VARIABLES LIKE 'local_infile';", expected_rows=True)
    if not rows or len(rows[0]) < 2:
        return "UNKNOWN"
    return rows[0][1].upper()


def set_local_infile(args: argparse.Namespace, enabled: bool) -> None:
    mysql_execute(args, args.world_db, f"SET GLOBAL local_infile={1 if enabled else 0};")


def ensure_mapping_database(args: argparse.Namespace) -> None:
    mysql_execute(
        args,
        "information_schema",
        f"CREATE DATABASE IF NOT EXISTS {sql_identifier(args.mapping_db)} CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;",
    )


def parse_schema(schema_sql: str) -> list[TableSpec]:
    pattern = re.compile(
        r"CREATE TABLE IF NOT EXISTS\s+(nf_map_[A-Za-z0-9_]+)\s*\((.*?)\);",
        re.IGNORECASE | re.DOTALL,
    )
    specs: list[TableSpec] = []
    for match in pattern.finditer(schema_sql):
        table_name = match.group(1)
        body = match.group(2)
        create_sql = match.group(0)
        columns: list[ColumnSpec] = []
        for raw_line in body.splitlines():
            line = raw_line.strip().rstrip(",")
            if not line:
                continue
            upper = line.upper()
            if upper.startswith(("PRIMARY ", "KEY ", "UNIQUE ", "CONSTRAINT ", "INDEX ")):
                continue
            parts = line.split(None, 2)
            if len(parts) < 2:
                continue
            name = parts[0].strip("`")
            type_sql = parts[1].upper()
            is_numeric = any(marker in type_sql for marker in NUMERIC_TYPE_MARKERS)
            columns.append(ColumnSpec(name=name, type_sql=parts[1], is_numeric=is_numeric))
        specs.append(TableSpec(name=table_name, create_sql=create_sql, columns=columns))
    return specs


def csv_path_for_table(output_dir: Path, table_name: str) -> Path:
    if table_name in SPECIAL_CSV_NAMES:
        return output_dir / SPECIAL_CSV_NAMES[table_name]
    base_name = table_name.removeprefix("nf_map_")
    return output_dir / f"{base_name}_map.csv"


def table_exists(args: argparse.Namespace, table_name: str) -> bool:
    query = (
        "SELECT COUNT(*) FROM information_schema.TABLES "
        f"WHERE TABLE_SCHEMA = {sql_string(args.world_db)} AND TABLE_NAME = {sql_string(table_name)};"
    )
    rows = mysql_rows(args, "information_schema", query, expected_rows=True)
    return bool(rows) and to_int(rows[0][0]) > 0


def table_count(args: argparse.Namespace, table_name: str) -> int:
    if not table_exists(args, table_name):
        return 0
    rows = mysql_rows(args, args.world_db, f"SELECT COUNT(*) FROM `{table_name}`;", expected_rows=True)
    if not rows:
        raise RuntimeError(f"mysql returned no rows while counting {table_name}")
    return to_int(rows[0][0])


def escape_tsv_text(value: str) -> str:
    return (
        value.replace("\\", "\\\\")
        .replace("\t", "\\t")
        .replace("\r", "\\r")
        .replace("\n", "\\n")
    )


def write_load_file(csv_path: Path, table: TableSpec, temp_dir: Path) -> tuple[Path, int, list[str]]:
    output_path = temp_dir / f"{table.name}.tsv"
    column_names = [column.name for column in table.columns]
    missing_columns: list[str] = []
    row_count = 0

    with csv_path.open("r", encoding="utf-8", newline="") as input_handle:
        reader = csv.DictReader(input_handle)
        csv_fields = set(reader.fieldnames or [])
        missing_columns = [column for column in column_names if column not in csv_fields]
        if missing_columns:
            return output_path, 0, missing_columns

        with output_path.open("w", encoding="utf-8", newline="\n") as output_handle:
            for row in reader:
                values: list[str] = []
                for column in table.columns:
                    value = row.get(column.name, "")
                    if value is None:
                        value = ""
                    value = str(value)
                    if column.is_numeric and value.strip() == "":
                        values.append(r"\N")
                    else:
                        values.append(escape_tsv_text(value))
                output_handle.write("\t".join(values))
                output_handle.write("\n")
                row_count += 1

    return output_path, row_count, missing_columns


def load_table_sql(args: argparse.Namespace, table: TableSpec, load_path: Path) -> str:
    normalized_path = str(load_path).replace("\\", "/")
    column_list = ",".join(f"`{column.name}`" for column in table.columns)
    truncate_sql = f"TRUNCATE TABLE `{table.name}`;" if args.replace_existing else ""
    load_mode = "REPLACE" if args.replace_existing else "IGNORE"
    return f"""
USE `{args.world_db}`;
{truncate_sql}
LOAD DATA LOCAL INFILE {sql_string(normalized_path)}
{load_mode} INTO TABLE `{table.name}`
CHARACTER SET utf8mb4
FIELDS TERMINATED BY '\\t' ESCAPED BY '\\\\'
LINES TERMINATED BY '\\n'
({column_list});
""".strip() + "\n"


def create_table_sql(args: argparse.Namespace, table: TableSpec) -> str:
    create_sql = table.create_sql.strip().rstrip(";")
    # Staging tables preserve raw client/datamine ids. Some fields contain
    # uint32 sentinels such as 4294967295, so widen bare INT columns here while
    # leaving TINYINT and existing BIGINT declarations intact.
    create_sql = re.sub(r"\bINT\b", "BIGINT", create_sql, flags=re.IGNORECASE)
    statements = [f"USE `{args.world_db}`;"]
    if args.replace_existing:
        statements.append(f"DROP TABLE IF EXISTS `{table.name}`;")
    statements.append(create_sql + ";")
    return "\n".join(statements)


def create_schema_sql(args: argparse.Namespace, tables: Sequence[TableSpec]) -> str:
    return "\n\n".join(create_table_sql(args, table) for table in tables) + "\n"


def select_tables(args: argparse.Namespace, tables: Sequence[TableSpec]) -> list[TableSpec]:
    include = {name.strip() for name in args.only_table.split(",") if name.strip()} if args.only_table else set()
    skip = {name.strip() for name in args.skip_table.split(",") if name.strip()} if args.skip_table else set()
    selected = []
    for table in tables:
        if include and table.name not in include:
            continue
        if table.name in skip:
            continue
        selected.append(table)
    return selected


def load_tables(args: argparse.Namespace, tables: Sequence[TableSpec]) -> list[dict[str, object]]:
    results: list[dict[str, object]] = []
    temp_root = Path(tempfile.mkdtemp(prefix="nf_map_load_"))
    args.local_infile_previous = get_local_infile(args)
    args.local_infile_changed = False
    try:
        if args.manage_local_infile and args.local_infile_previous == "OFF":
            set_local_infile(args, True)
            args.local_infile_changed = True

        for index, table in enumerate(tables, start=1):
            csv_path = csv_path_for_table(args.output_dir, table.name)
            table_result: dict[str, object] = {
                "table": table.name,
                "csv": str(csv_path),
                "status": "pending",
                "csv_rows": 0,
                "post_rows": None,
            }
            if not csv_path.exists():
                table_result["status"] = "missing_csv"
                results.append(table_result)
                print(f"[{index}/{len(tables)}] skipped {table.name}: missing {csv_path.name}")
                continue

            mysql_execute(args, args.world_db, create_table_sql(args, table))
            load_path, row_count, missing_columns = write_load_file(csv_path, table, temp_root)
            table_result["csv_rows"] = row_count
            if missing_columns:
                table_result["status"] = "missing_columns"
                table_result["missing_columns"] = missing_columns
                results.append(table_result)
                print(f"[{index}/{len(tables)}] skipped {table.name}: missing columns {', '.join(missing_columns)}")
                continue

            mysql_execute(args, args.world_db, load_table_sql(args, table, load_path))
            post_rows = table_count(args, table.name)
            table_result["status"] = "loaded"
            table_result["post_rows"] = post_rows
            results.append(table_result)
            print(f"[{index}/{len(tables)}] loaded {table.name}: {post_rows} rows")

            try:
                load_path.unlink()
            except FileNotFoundError:
                pass
    finally:
        if (
            args.manage_local_infile
            and args.restore_local_infile
            and args.local_infile_changed
            and args.local_infile_previous == "OFF"
        ):
            set_local_infile(args, False)
        if args.keep_temp:
            print(f"Kept temp directory {temp_root}")
        else:
            shutil.rmtree(temp_root, ignore_errors=True)
    return results


def dry_run_tables(args: argparse.Namespace, tables: Sequence[TableSpec]) -> list[dict[str, object]]:
    results: list[dict[str, object]] = []
    for table in tables:
        csv_path = csv_path_for_table(args.output_dir, table.name)
        result: dict[str, object] = {
            "table": table.name,
            "csv": str(csv_path),
            "status": "ready" if csv_path.exists() else "missing_csv",
            "existing_rows": table_count(args, table.name),
        }
        if csv_path.exists():
            with csv_path.open("r", encoding="utf-8", newline="") as handle:
                reader = csv.reader(handle)
                headers = next(reader, [])
                result["csv_columns"] = len(headers)
                result["missing_columns"] = [column.name for column in table.columns if column.name not in headers]
        results.append(result)
    return results


def build_summary(args: argparse.Namespace, results: Sequence[dict[str, object]]) -> Dict[str, object]:
    loaded = sum(1 for result in results if result.get("status") == "loaded")
    ready = sum(1 for result in results if result.get("status") == "ready")
    missing_csv = sum(1 for result in results if result.get("status") == "missing_csv")
    missing_columns = sum(1 for result in results if result.get("status") == "missing_columns")
    return {
        "generated_at": time.strftime("%Y-%m-%d %H:%M:%S"),
        "mapping_db": args.mapping_db,
        "schema": str(args.schema),
        "output_dir": str(args.output_dir),
        "mode": "apply" if args.apply else "dry-run",
        "replace_existing": args.replace_existing,
        "selected_tables": len(results),
        "loaded_tables": loaded,
        "ready_tables": ready,
        "missing_csv_tables": missing_csv,
        "missing_column_tables": missing_columns,
        "local_infile_previous": getattr(args, "local_infile_previous", None),
        "local_infile_changed": getattr(args, "local_infile_changed", False),
        "local_infile_restored": bool(
            getattr(args, "local_infile_changed", False)
            and args.manage_local_infile
            and args.restore_local_infile
        ),
        "tables": list(results),
    }


def parse_args(argv: Optional[Sequence[str]] = None) -> argparse.Namespace:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--repo-root", type=Path, default=Path.cwd())
    parser.add_argument("--schema", type=Path)
    parser.add_argument("--output-dir", type=Path)
    parser.add_argument("--report-out", type=Path)
    parser.add_argument("--mysql-exe", type=Path, default=Path(DEFAULT_MYSQL))
    parser.add_argument("--host", default="127.0.0.1")
    parser.add_argument("--port", type=int, default=3306)
    parser.add_argument("--user", default="bankai")
    parser.add_argument("--password", default="bankai")
    parser.add_argument("--mapping-db", default=DEFAULT_MAPPING_DB, help="Authoring database that will hold nf_map_* staging tables.")
    parser.add_argument("--world-db", default="", help="Deprecated alias for --mapping-db.")
    parser.add_argument("--only-table", default="", help="Comma-separated nf_map_* tables to load.")
    parser.add_argument("--skip-table", default="", help="Comma-separated nf_map_* tables to skip.")
    parser.add_argument("--keep-temp", action="store_true")
    parser.add_argument("--manage-local-infile", action="store_true", default=True)
    parser.add_argument("--no-manage-local-infile", action="store_false", dest="manage_local_infile")
    parser.add_argument("--restore-local-infile", action="store_true", default=True)
    parser.add_argument("--leave-local-infile-on", action="store_false", dest="restore_local_infile")
    parser.add_argument("--replace-existing", action="store_true", default=True)
    parser.add_argument("--append", action="store_false", dest="replace_existing")
    parser.add_argument("--apply", action="store_true", help="Actually create and load nf_map_* tables. Omit for dry-run.")
    args = parser.parse_args(argv)
    args.repo_root = args.repo_root.resolve()
    args.schema = (args.schema or args.repo_root / "Tools" / "DataMapping" / "schema.sql").resolve()
    args.output_dir = (args.output_dir or args.repo_root / "Tools" / "DataMapping" / "output").resolve()
    args.report_out = (
        args.report_out or args.output_dir / "mapping_staging_load_report.json"
    ).resolve()
    if args.world_db:
        print(
            "warning: --world-db is deprecated for staging loads; use --mapping-db instead.",
            file=sys.stderr,
        )
        args.mapping_db = args.world_db
    args.world_db = args.mapping_db
    return args


def main(argv: Optional[Sequence[str]] = None) -> int:
    args = parse_args(argv)
    if not args.schema.exists():
        raise FileNotFoundError(args.schema)
    if not args.output_dir.exists():
        raise FileNotFoundError(args.output_dir)
    if not args.mysql_exe.exists():
        raise FileNotFoundError(args.mysql_exe)

    tables = select_tables(args, parse_schema(args.schema.read_text(encoding="utf-8")))
    if args.apply:
        ensure_mapping_database(args)
        results = load_tables(args, tables)
    else:
        results = dry_run_tables(args, tables)

    summary = build_summary(args, results)
    args.report_out.parent.mkdir(parents=True, exist_ok=True)
    args.report_out.write_text(json.dumps(summary, indent=2), encoding="utf-8")
    print(json.dumps({k: v for k, v in summary.items() if k != "tables"}, indent=2))
    print(f"Wrote report to {args.report_out}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
