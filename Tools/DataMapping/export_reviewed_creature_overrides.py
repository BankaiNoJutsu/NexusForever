#!/usr/bin/env python3
"""Recover reviewed creature bridge decisions from authoring staging into the tracked override CSV.

`Tools/DataMapping/review/` is git-ignored, so reviewed decisions that only lived there (or only in
`nexus_forever_mapping.nf_map_creature`) are lost on a clean checkout and silently revert to
ambiguous/scored matches on the next mapper run. This script copies every `reviewed` row that is not
already in `creature_bridge_overrides.csv` into that tracked file, with an explicit `evidence_tier`.

Existing tracked rows always win; nothing already tracked is changed except that the `evidence_tier`
column is filled in (inferred from reviewer/reason, matching map_wildstar_data.infer_review_evidence_tier).

    python Tools/DataMapping/export_reviewed_creature_overrides.py            # dry run
    python Tools/DataMapping/export_reviewed_creature_overrides.py --apply
"""
from __future__ import annotations

import argparse
import csv
import subprocess
import sys
from pathlib import Path

FIELDS = [
    "source_table",
    "source_id",
    "source_name",
    "chosen_creature2_id",
    "decision",
    "reason",
    "reviewer",
    "reviewed_at",
    "evidence_tier",
]
DEFAULT_MYSQL = r"C:\Program Files\MySQL\MySQL Server 8.0\bin\mysql.exe"


def infer_tier(row: dict) -> str:
    explicit = (row.get("evidence_tier") or "").strip()
    if explicit:
        return explicit
    reason = (row.get("reason") or "").strip().lower()
    reviewer = (row.get("reviewer") or "").strip().lower()
    if reason.startswith("existing_entity_spatial") or "spatial evidence review" in reviewer:
        return "spatial_heuristic"
    return "manual_evidence"


def read_tracked(path: Path) -> list[dict]:
    with path.open("r", encoding="utf-8", newline="") as handle:
        rows = []
        for row in csv.DictReader(handle):
            extra = row.pop(None, None)
            if extra:
                # unquoted commas inside `reason` spilled into extra cells; fold them back in.
                row["reason"] = ",".join([row.get("reason", "")] + [e for e in extra])
            rows.append(row)
        return rows


def staging_rows(args: argparse.Namespace) -> list[dict]:
    query = (
        "SELECT jabbithole_creature_id, source_name, creature2_id, review_decision, "
        "REPLACE(REPLACE(review_reason, '\\n', ' '), '\\t', ' '), reviewer, reviewed_at "
        f"FROM {args.mapping_db}.nf_map_creature WHERE match_status = 'reviewed' ORDER BY jabbithole_creature_id"
    )
    result = subprocess.run(
        [str(args.mysql_exe), f"--host={args.host}", f"--user={args.user}", f"--password={args.password}",
         "--default-character-set=utf8mb4", "-B", "-N", "-e", query],
        capture_output=True, text=True, encoding="utf-8",
    )
    if result.returncode != 0:
        raise RuntimeError(result.stderr.strip())
    rows = []
    for line in result.stdout.splitlines():
        parts = line.split("\t")
        if len(parts) < 7:
            continue
        rows.append(
            {
                "source_table": "creatures",
                "source_id": parts[0],
                "source_name": parts[1],
                "chosen_creature2_id": parts[2],
                "decision": parts[3] or "approved",
                "reason": parts[4],
                "reviewer": parts[5],
                "reviewed_at": parts[6],
            }
        )
    return rows


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument("--tracked", type=Path, default=Path(__file__).with_name("creature_bridge_overrides.csv"))
    parser.add_argument("--mapping-db", default="nexus_forever_mapping")
    parser.add_argument("--mysql-exe", type=Path, default=Path(DEFAULT_MYSQL))
    parser.add_argument("--host", default="127.0.0.1")
    parser.add_argument("--user", default="bankai")
    parser.add_argument("--password", default="bankai")
    parser.add_argument("--apply", action="store_true")
    args = parser.parse_args()

    tracked = read_tracked(args.tracked)
    tracked_ids = {row["source_id"] for row in tracked}
    recovered = [row for row in staging_rows(args) if row["source_id"] not in tracked_ids]
    for row in tracked + recovered:
        row["evidence_tier"] = infer_tier(row)

    tiers: dict[str, int] = {}
    for row in tracked + recovered:
        tiers[row["evidence_tier"]] = tiers.get(row["evidence_tier"], 0) + 1
    print(f"tracked rows: {len(tracked)}; recovered from staging: {len(recovered)}; tiers: {tiers}")
    if not args.apply:
        print("dry run; pass --apply to rewrite", args.tracked)
        return 0

    with args.tracked.open("w", encoding="utf-8", newline="") as handle:
        writer = csv.DictWriter(handle, fieldnames=FIELDS, lineterminator="\n", extrasaction="ignore")
        writer.writeheader()
        writer.writerows(tracked + recovered)
    print("wrote", args.tracked)
    return 0


if __name__ == "__main__":
    sys.exit(main())
