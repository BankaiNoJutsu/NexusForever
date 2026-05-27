#!/usr/bin/env python3
"""
Build a row-level review worksheet from the LaughingWS WorldDB audit output.

The analyzer emits exact normalized row deltas. This script keeps that data
intact, but projects commonly reviewed fields into first-class CSV columns so
the remaining broad/residual replacement files can be reviewed without hand
parsing row_json values.
"""

from __future__ import annotations

import argparse
import csv
import json
from collections import Counter
from pathlib import Path


DEFAULT_AUDIT_DIR = Path("artifacts/laughingws_worlddb_audit_latest")

REVIEW_RECOMMENDATION_MARKERS = (
    "extract_additive_world_overlay",
    "blocked_laughingws_",
)

PROJECTED_ROW_FIELDS = (
    "world",
    "eventId",
    "id",
    "creature",
    "displayInfo",
    "outfitInfo",
    "area",
    "type",
    "x",
    "y",
    "z",
    "rx",
    "ry",
    "rz",
    "questChecklistIdx",
    "entityId",
    "vendorId",
    "itemId",
    "categoryId",
    "lootGroupId",
    "stat",
    "value",
    "splineId",
    "mode",
    "speed",
    "scriptName",
    "property",
    "triggerId",
)

ROW_FIELD_ALIASES = {
    "activePropId": ("activepropid",),
    "buyPriceMultiplier": ("buypricemultiplier",),
    "categoryId": ("categoryid",),
    "displayInfo": ("displayinfo",),
    "entityId": ("entityid",),
    "eventId": ("eventid",),
    "itemId": ("itemid",),
    "lootGroupId": ("lootgroupid",),
    "outfitInfo": ("outfitinfo",),
    "questChecklistIdx": ("questchecklistidx",),
    "scriptName": ("scriptname",),
    "sellPriceMultiplier": ("sellpricemultiplier",),
    "splineId": ("splineid",),
    "triggerId": ("triggerid",),
    "vendorId": ("vendorid",),
    "worldSocketId": ("worldsocketid",),
}

OUTPUT_FIELDS = [
    "file",
    "official_file",
    "recommendations",
    "side",
    "table",
    "count",
    "row_hash",
    "exists_in_official_comparison",
    *PROJECTED_ROW_FIELDS,
    "review_decision",
    "review_reason",
    "evidence_source",
    "reviewer",
    "reviewed_at",
    "row_json",
]


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(
        description="Build a review worksheet from LaughingWS row reconciliation output.",
    )
    parser.add_argument(
        "--audit-dir",
        type=Path,
        default=DEFAULT_AUDIT_DIR,
        help="Directory containing laughingws_worlddb_row_reconciliation.csv.",
    )
    parser.add_argument(
        "--input",
        type=Path,
        help="Optional explicit reconciliation CSV path.",
    )
    parser.add_argument(
        "--output",
        type=Path,
        help="Optional output CSV path.",
    )
    parser.add_argument(
        "--inventory",
        type=Path,
        help="Optional file inventory CSV path used to refresh per-file recommendations.",
    )
    parser.add_argument(
        "--include-all",
        action="store_true",
        help="Include every reconciliation row, not just open review recommendations.",
    )
    return parser.parse_args()


def load_inventory_recommendations(path: Path) -> dict[str, str]:
    if not path.exists():
        return {}

    with path.open("r", encoding="utf-8", newline="") as handle:
        reader = csv.DictReader(handle)
        return {
            row.get("file", ""): row.get("recommendations", "")
            for row in reader
            if row.get("file")
        }


def is_review_row(row: dict[str, str]) -> bool:
    recommendations = row.get("recommendations", "")
    return any(marker in recommendations for marker in REVIEW_RECOMMENDATION_MARKERS)


def get_projected_value(values: dict[str, str], field: str) -> str:
    candidates = (field, field.lower(), *ROW_FIELD_ALIASES.get(field, ()))
    for candidate in candidates:
        value = values.get(candidate)
        if value is not None:
            return value
    return ""


def load_rows(
    path: Path,
    include_all: bool,
    inventory_recommendations: dict[str, str],
) -> list[dict[str, str]]:
    with path.open("r", encoding="utf-8", newline="") as handle:
        reader = csv.DictReader(handle)
        rows = []
        for row in reader:
            file_recommendations = inventory_recommendations.get(row.get("file", ""))
            if file_recommendations:
                row["recommendations"] = file_recommendations
            if include_all or is_review_row(row):
                rows.append(row)

    projected_rows: list[dict[str, str]] = []
    for row in rows:
        row_json = row.get("row_json", "")
        try:
            values = json.loads(row_json)
        except json.JSONDecodeError:
            values = {}

        projected = {
            "file": row.get("file", ""),
            "official_file": row.get("official_file", ""),
            "recommendations": row.get("recommendations", ""),
            "side": row.get("side", ""),
            "table": row.get("table", ""),
            "count": row.get("count", ""),
            "row_hash": row.get("row_hash", ""),
            "exists_in_official_comparison": "yes" if row.get("side") == "official_only" else "no",
        }
        for field in PROJECTED_ROW_FIELDS:
            projected[field] = get_projected_value(values, field)

        projected.update(
            {
                "review_decision": "",
                "review_reason": "",
                "evidence_source": "",
                "reviewer": "",
                "reviewed_at": "",
                "row_json": row_json,
            }
        )
        projected_rows.append(projected)

    projected_rows.sort(
        key=lambda row: (
            row["file"],
            row["table"],
            row["side"],
            row["creature"],
            row["world"],
            row["x"],
            row["y"],
            row["z"],
            row["row_hash"],
        )
    )
    return projected_rows


def write_csv(path: Path, rows: list[dict[str, str]]) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    with path.open("w", encoding="utf-8", newline="") as handle:
        writer = csv.DictWriter(handle, fieldnames=OUTPUT_FIELDS)
        writer.writeheader()
        writer.writerows(rows)


def print_summary(rows: list[dict[str, str]], output_path: Path) -> None:
    by_file = Counter(row["file"] for row in rows)
    by_recommendation: Counter[str] = Counter()
    for row in rows:
        for recommendation in row["recommendations"].split(";"):
            if recommendation:
                by_recommendation[recommendation] += 1

    print(f"Wrote {len(rows)} review rows to {output_path}")
    print(f"Files represented: {len(by_file)}")
    for file, count in by_file.most_common():
        print(f"- {file}: {count}")
    print("Recommendations represented:")
    for recommendation, count in by_recommendation.most_common():
        print(f"- {recommendation}: {count}")


def main() -> int:
    args = parse_args()
    input_path = args.input or args.audit_dir / "laughingws_worlddb_row_reconciliation.csv"
    output_path = args.output or args.audit_dir / "laughingws_worlddb_row_review_queue.csv"
    inventory_path = args.inventory or args.audit_dir / "laughingws_worlddb_file_inventory.csv"

    if not input_path.exists():
        raise FileNotFoundError(f"Reconciliation CSV not found: {input_path}")

    inventory_recommendations = load_inventory_recommendations(inventory_path)
    rows = load_rows(input_path, args.include_all, inventory_recommendations)
    write_csv(output_path, rows)
    print_summary(rows, output_path)
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
