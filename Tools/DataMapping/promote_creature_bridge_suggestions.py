#!/usr/bin/env python3
"""
Promote high-confidence creature bridge suggestions into reviewed overrides.

Default policy is intentionally conservative:

* recommendation_confidence must be high
* recommendation_kind must be existing_entity_spatial

Use --apply to update Tools/DataMapping/review/creature_bridge_overrides.csv.
"""

from __future__ import annotations

import argparse
import csv
import json
import time
from pathlib import Path
from typing import Iterable, Optional, Sequence


OVERRIDE_FIELDS = [
    "source_table",
    "source_id",
    "source_name",
    "chosen_creature2_id",
    "decision",
    "reason",
    "reviewer",
    "reviewed_at",
]


def load_csv(path: Path) -> list[dict[str, str]]:
    if not path.exists():
        return []
    with path.open("r", encoding="utf-8", newline="") as handle:
        return list(csv.DictReader(handle))


def write_csv(path: Path, fieldnames: Sequence[str], rows: Iterable[dict[str, object]]) -> int:
    path.parent.mkdir(parents=True, exist_ok=True)
    count = 0
    with path.open("w", encoding="utf-8", newline="") as handle:
        writer = csv.DictWriter(handle, fieldnames=fieldnames, extrasaction="ignore")
        writer.writeheader()
        for row in rows:
            writer.writerow(row)
            count += 1
    return count


def suggestion_allowed(args: argparse.Namespace, row: dict[str, str]) -> bool:
    confidence = row.get("recommendation_confidence", "")
    kind = row.get("recommendation_kind", "")
    allowed_confidence = {value.strip() for value in args.confidence.split(",") if value.strip()}
    allowed_kind = {value.strip() for value in args.kind.split(",") if value.strip()}
    if allowed_confidence and confidence not in allowed_confidence:
        return False
    if allowed_kind and kind not in allowed_kind:
        return False
    return bool(row.get("source_id") and row.get("chosen_creature2_id"))


def build_override(row: dict[str, str], reviewer: str, reviewed_at: str) -> dict[str, object]:
    reason = (
        f"{row.get('recommendation_kind', '')}: {row.get('reason', '')}; "
        f"nearest_entity_distance={row.get('nearest_entity_distance', '')}; "
        f"spatial_match_count={row.get('spatial_match_count', '')}; "
        f"entity_match_count={row.get('entity_match_count', '')}; "
        f"priority={row.get('review_priority_score', '')}"
    ).strip()
    return {
        "source_table": row.get("source_table", "creatures") or "creatures",
        "source_id": row.get("source_id", ""),
        "source_name": row.get("source_name", ""),
        "chosen_creature2_id": row.get("chosen_creature2_id", ""),
        "decision": "approved",
        "reason": reason,
        "reviewer": reviewer,
        "reviewed_at": reviewed_at,
    }


def parse_args(argv: Optional[Sequence[str]] = None) -> argparse.Namespace:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--repo-root", type=Path, default=Path.cwd())
    parser.add_argument("--suggestion-in", type=Path)
    parser.add_argument("--override-out", type=Path)
    parser.add_argument("--report-out", type=Path)
    parser.add_argument("--confidence", default="high")
    parser.add_argument("--kind", default="existing_entity_spatial")
    parser.add_argument("--reviewer", default="Codex spatial evidence review")
    parser.add_argument("--reviewed-at", default="")
    parser.add_argument("--replace-existing", action="store_true")
    parser.add_argument("--apply", action="store_true")
    args = parser.parse_args(argv)
    args.repo_root = args.repo_root.resolve()
    output_dir = args.repo_root / "Tools" / "DataMapping" / "output"
    review_dir = args.repo_root / "Tools" / "DataMapping" / "review"
    args.suggestion_in = (args.suggestion_in or output_dir / "creature_bridge_override_suggestions.csv").resolve()
    args.override_out = (args.override_out or review_dir / "creature_bridge_overrides.csv").resolve()
    args.report_out = (
        args.report_out or output_dir / "creature_bridge_suggestion_promotion_report.json"
    ).resolve()
    if not args.reviewed_at:
        args.reviewed_at = time.strftime("%Y-%m-%d")
    return args


def main(argv: Optional[Sequence[str]] = None) -> int:
    args = parse_args(argv)
    suggestions = load_csv(args.suggestion_in)
    existing = load_csv(args.override_out)
    existing_by_source = {row.get("source_id", ""): row for row in existing if row.get("source_id")}

    promoted = []
    skipped_existing = 0
    for row in suggestions:
        if not suggestion_allowed(args, row):
            continue
        source_id = row.get("source_id", "")
        if source_id in existing_by_source and not args.replace_existing:
            skipped_existing += 1
            continue
        promoted.append(build_override(row, args.reviewer, args.reviewed_at))

    merged_by_source = dict(existing_by_source)
    for row in promoted:
        merged_by_source[str(row["source_id"])] = row

    merged = sorted(
        merged_by_source.values(),
        key=lambda row: int(row.get("source_id", "0")) if str(row.get("source_id", "")).isdigit() else 0,
    )

    report = {
        "generated_at": time.strftime("%Y-%m-%d %H:%M:%S"),
        "mode": "apply" if args.apply else "dry-run",
        "suggestion_in": str(args.suggestion_in),
        "override_out": str(args.override_out),
        "confidence": args.confidence,
        "kind": args.kind,
        "existing_overrides": len(existing_by_source),
        "promoted_overrides": len(promoted),
        "skipped_existing": skipped_existing,
        "final_override_rows": len(merged),
    }
    args.report_out.parent.mkdir(parents=True, exist_ok=True)
    args.report_out.write_text(json.dumps(report, indent=2), encoding="utf-8")
    print(json.dumps(report, indent=2))

    if args.apply:
        write_csv(args.override_out, OVERRIDE_FIELDS, merged)
        print(f"Wrote {len(merged)} override rows to {args.override_out}")

    return 0


if __name__ == "__main__":
    raise SystemExit(main())
