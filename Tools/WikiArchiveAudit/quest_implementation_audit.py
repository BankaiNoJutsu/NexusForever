#!/usr/bin/env python3
"""Offline quest implementation audit against client Quest2 tables and server scripts."""

from __future__ import annotations

import argparse
import re
import sys
from collections import Counter
from datetime import date
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
DEFAULT_SQL = ROOT / "wildstar_client_mysql"
DEFAULT_SCRIPT_MAIN = ROOT / "Source" / "NexusForever.Script.Main"
DEFAULT_OUTPUT = ROOT / "Decomp" / "Analysis" / "coverage" / "QUEST_IMPLEMENTATION_AUDIT.md"


def iter_insert_rows(path: Path, table_name: str):
    text = path.read_text(encoding="utf-8", errors="replace")
    pattern = re.compile(
        rf"INSERT\s+INTO\s+`?{re.escape(table_name)}`?\s+VALUES\s*\((.*?)\);",
        re.DOTALL | re.IGNORECASE,
    )
    for match in pattern.finditer(text):
        row: list[str] = []
        current: list[str] = []
        in_string = False
        escape = False
        for ch in match.group(1):
            if escape:
                current.append(ch)
                escape = False
                continue
            if ch == "\\" and in_string:
                escape = True
                current.append(ch)
                continue
            if ch == "'" and not escape:
                in_string = not in_string
                current.append(ch)
                continue
            if ch == "," and not in_string:
                row.append("".join(current).strip())
                current = []
                continue
            current.append(ch)
        if current:
            row.append("".join(current).strip())
        yield [value.strip().strip("'") for value in row]


def parse_quests(sql_dir: Path):
    rows = []
    for values in iter_insert_rows(sql_dir / "Quest2.tbl.sql", "Quest2"):
        rows.append(
            {
                "id": int(values[0]),
                "objective_ids": tuple(int(value) for value in values[30:36] if int(value) > 0),
            }
        )
    return rows


def parse_objectives(sql_dir: Path):
    rows: dict[int, dict[str, int]] = {}
    for values in iter_insert_rows(sql_dir / "QuestObjective.tbl.sql", "QuestObjective"):
        rows[int(values[0])] = {
            "id": int(values[0]),
            "type": int(values[1]),
            "flags": int(values[2]),
            "count": int(values[4]),
        }
    return rows


# Objective types with a mapped generic server trigger path (see QuestManager, interactions, kills, etc.)
GENERIC = {
    2,
    3,
    5,
    8,
    9,
    10,
    12,
    14,
    15,
    16,
    17,
    18,
    22,
    32,
    37,
    38,
    47,
}
PARTIAL = {11, 13, 23, 35, 36}
UNSUPPORTED = {4, 19, 20, 21, 24, 25, 27, 28, 29, 31, 33, 39, 40, 41, 42, 44, 46, 48}

# Hand-verified quest chains called out in CURRENT_STATUS / focused script work.
CURATED_FULL = {
    10513,
    10521,
    10527,
    10532,
    10518,
    10524,
    10519,
    10520,
    10522,
    10523,
    10525,
    10526,
    10528,
    10530,
    10540,
    10541,
    3479,
    3480,
    3667,
    3486,
    3671,
    3668,
    3797,
    3487,
    3963,
    3886,
    3673,
    3670,
    5573,
    5575,
    5580,
    5583,
    5584,
    5594,
    5595,
    5596,
    5597,
    5604,
    5610,
    8855,
}
PATH_STUB_RANGE = range(10544, 10571)


def load_script_ids(script_main: Path) -> set[int]:
    script_ids: set[int] = set()
    for path in script_main.rglob("*.cs"):
        text = path.read_text(encoding="utf-8", errors="replace")
        for match in re.finditer(r"ScriptFilterOwnerId\(([^)]+)\)", text):
            for num in re.findall(r"\d+", match.group(1)):
                script_ids.add(int(num))
    return script_ids


def classify(quest, objectives: dict[int, dict[str, int]], script_ids: set[int]) -> str:
    qid = quest["id"]
    obj_types = []
    for objective_id in quest["objective_ids"]:
        objective = objectives.get(objective_id)
        if objective:
            obj_types.append(objective["type"])

    has_script = qid in script_ids

    if not obj_types:
        return "no_objectives"

    if qid in CURATED_FULL:
        return "curated_full"

    if qid in PATH_STUB_RANGE or qid == 10510:
        return "curated_partial_stub_script"

    unsupported = [obj_type for obj_type in obj_types if obj_type in UNSUPPORTED]
    partial = [obj_type for obj_type in obj_types if obj_type in PARTIAL]

    if unsupported:
        if has_script:
            return "blocked_objectives_with_script"
        return "blocked_objectives"

    if partial:
        if has_script:
            return "partial_objectives_with_script"
        return "partial_objectives_only"

    if has_script:
        return "generic_with_script"

    return "generic_table_driven"


def build_report(sql_dir: Path, script_main: Path) -> list[str]:
    quests = parse_quests(sql_dir)
    objectives = parse_objectives(sql_dir)
    script_ids = load_script_ids(script_main)

    buckets = Counter()
    obj_type_quest_counts = Counter()

    for quest in quests:
        bucket = classify(quest, objectives, script_ids)
        buckets[bucket] += 1
        for objective_id in quest["objective_ids"]:
            objective = objectives.get(objective_id)
            if objective:
                obj_type_quest_counts[objective["type"]] += 1

    total = len(quests)
    with_objectives = sum(1 for quest in quests if quest["objective_ids"])
    curated_full = buckets["curated_full"]
    any_script = len(script_ids)
    generic_playable = buckets["generic_table_driven"] + buckets["generic_with_script"]
    no_objectives = buckets["no_objectives"]
    blocked = buckets["blocked_objectives"] + buckets["blocked_objectives_with_script"]
    partial = (
        buckets["partial_objectives_only"]
        + buckets["partial_objectives_with_script"]
        + buckets["curated_partial_stub_script"]
    )

    lines = [
        "# Quest Implementation Audit",
        "",
        f"- Generated: {date.today().isoformat()}",
        f"- Generator: `Tools/WikiArchiveAudit/quest_implementation_audit.py`",
        f"- Client SQL: `{sql_dir.relative_to(ROOT)}`",
        f"- Server scripts: `{script_main.relative_to(ROOT)}`",
        f"- Status summary: `Decomp/Analysis/QUEST_IMPLEMENTATION_STATUS.md`",
        "",
        "## Executive summary",
        "",
        "| Metric | Count | % of total |",
        "| --- | ---: | ---: |",
        f"| Total client quests (`Quest2`) | {total} | 100% |",
        f"| Quests with at least one objective | {with_objectives} | {with_objectives * 100 / total:.1f}% |",
        f"| Quests with **no objectives** (mention/placeholder rows) | {no_objectives} | {no_objectives * 100 / total:.1f}% |",
        f"| **Fully curated** (hand-tuned script chains) | {curated_full} | {curated_full * 100 / total:.1f}% |",
        f"| **Generic table-driven** (supported objective types; no custom script) | {generic_playable} | {generic_playable * 100 / total:.1f}% |",
        f"| **Partial** (partial objective mix or stub path scripts) | {partial} | {partial * 100 / total:.1f}% |",
        f"| **Blocked / missing gameplay** (unsupported objective types) | {blocked} | {blocked * 100 / total:.1f}% |",
        f"| Distinct quest ids with `ScriptFilterOwnerId` | {any_script} | {any_script * 100 / total:.1f}% |",
        "",
        "### Quests with objectives only",
        "",
        "| Tier | Count | % of quests with objectives |",
        "| --- | ---: | ---: |",
        f"| Completable (generic + curated) | {generic_playable + curated_full - buckets['generic_with_script']} | {(generic_playable + curated_full - buckets['generic_with_script']) * 100 / with_objectives:.1f}% |",
        f"| Partial | {partial} | {partial * 100 / with_objectives:.1f}% |",
        f"| Blocked | {blocked} | {blocked * 100 / with_objectives:.1f}% |",
        "",
        "## Interpretation",
        "",
        "- **Lifecycle** (accept, track, abandon, complete, rewards) is implemented for all `Quest2` rows via `GlobalQuestManager` / `QuestManager`.",
        "- **Fully curated** means a dedicated `IQuestScript` chain was hand-built and verified (tutorial, Northern Wilds, Crimson Isle focus areas).",
        "- **Generic table-driven** quests can progress through shared kill/talk/activate/CSI/enter-zone/virtual-collect handlers without a per-quest script.",
        "- **Blocked** quests contain objective types with no mapped server trigger (for example `CollectItem`, `CompleteEvent`, `CraftSchematic`, `Unknown31`).",
        "- Counts are objective-type coverage, not in-game QA. Generic quests still need world spawns, volumes, and loot.",
        "",
        "## Bucket detail",
        "",
        "| Bucket | Quests | Notes |",
        "| --- | ---: | --- |",
    ]

    bucket_notes = {
        "curated_full": "Hand-tuned quest script; verified starter/endgame focus chains",
        "curated_partial_stub_script": "Has quest script file but only logging / path-tutorial stubs",
        "generic_table_driven": "All objectives supported generically; no custom script",
        "generic_with_script": "Supported objectives plus extra script (often follow-up grant only)",
        "partial_objectives_only": "Mix of supported + partial objective types (e.g. SpellSuccess)",
        "partial_objectives_with_script": "Partial objective types plus a script hook",
        "blocked_objectives": "Contains unsupported objective types; will not progress without new handlers",
        "blocked_objectives_with_script": "Unsupported objectives; script present but cannot fix type gaps alone",
        "no_objectives": "No objective slots populated in Quest2",
    }

    for bucket, count in buckets.most_common():
        note = bucket_notes.get(bucket, "")
        lines.append(f"| `{bucket}` | {count} | {note} |")

    lines.extend(
        [
            "",
            "## Unsupported objective types (quest objective slots)",
            "",
            "| Type | Slots |",
            "| --- | ---: |",
        ]
    )

    for obj_type, count in sorted(
        ((obj_type, count) for obj_type, count in obj_type_quest_counts.items() if obj_type in UNSUPPORTED),
        key=lambda item: -item[1],
    ):
        lines.append(f"| {obj_type} | {count} |")

    lines.extend(
        [
            "",
            "## Curated quest ids",
            "",
            "```text",
            ", ".join(str(quest_id) for quest_id in sorted(CURATED_FULL)),
            "```",
            "",
            "## Custom script ids (all `ScriptFilterOwnerId` values)",
            "",
            "```text",
            ", ".join(str(quest_id) for quest_id in sorted(script_ids)),
            "```",
        ]
    )

    return lines


def main() -> int:
    parser = argparse.ArgumentParser(description="Audit quest implementation coverage from client tables and server scripts.")
    parser.add_argument("--client-sql-dir", type=Path, default=DEFAULT_SQL, help="Directory containing Quest2.tbl.sql")
    parser.add_argument("--script-main", type=Path, default=DEFAULT_SCRIPT_MAIN, help="NexusForever.Script.Main project root")
    parser.add_argument("--output", type=Path, default=DEFAULT_OUTPUT, help="Markdown report path")
    args = parser.parse_args()

    if not args.client_sql_dir.exists():
        print(f"Missing client SQL dir: {args.client_sql_dir}", file=sys.stderr)
        return 2

    if not args.script_main.exists():
        print(f"Missing script project: {args.script_main}", file=sys.stderr)
        return 2

    lines = build_report(args.client_sql_dir, args.script_main)
    args.output.parent.mkdir(parents=True, exist_ok=True)
    args.output.write_text("\n".join(lines) + "\n", encoding="utf-8")
    print(args.output)
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
