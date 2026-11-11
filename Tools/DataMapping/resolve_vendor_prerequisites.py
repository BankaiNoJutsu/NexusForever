#!/usr/bin/env python3
"""Resolve Jabbithole vendor prerequisite text onto client `Prerequisite` rows.

Jabbithole `versioned_vendor_items.prerequisite` is free text ("Requires Level 50",
"Requires 8000 reputation with The Algoroc Accord", ...). The runtime vendor packet already has a
`PrerequisiteId` field that points at the client Prerequisite table, and the runtime now stores and
enforces it (`entity_vendor_item.prerequisiteId`). This script produces `vendor_prerequisite_map.csv`
(one row per distinct text) so the safe-import SQL can fill that column.

Evidence tiers (never a retail parity claim):

* `semantic_equivalent_row` - a single-clause client Prerequisite row whose type, object and value equal the
  parsed text with a >= comparison. The client has many identical rows (one per consumer), so the lowest id
  is taken; which exact row retail used is unknown.
* `side_specific_pair` - "<Exile faction> or <Dominion faction>" reputation text. Each side resolves to its own
  equivalent row (`prerequisite_id_exile`, `prerequisite_id_dominion`); the import picks one from the vendor's side
  (`creature_map.source_side`, from Jabbithole's disposition flags) and leaves the row ungated when the side is
  unknown or contradictory. Evidence tier: inferred vendor side.
* `unresolved` - multi-clause text (medals, instance completion, "or" between unrelated requirements),
  an unmatched faction name, or no equivalent row. Left at 0 (no gate) and reported, not guessed.

    python Tools/DataMapping/resolve_vendor_prerequisites.py --output-dir Tools/DataMapping/output
    python Tools/DataMapping/resolve_vendor_prerequisites.py --from-staging-db   # when only staging is loaded

Faction names come from the mapper's `faction_map.csv` (`nf_map_faction`): both the Jabbithole name and the
client name resolve to the same Faction2 id.
"""
from __future__ import annotations

import argparse
import csv
import re
import subprocess
import sys
from dataclasses import dataclass
from pathlib import Path
from typing import Dict, Iterable, List, Optional, Sequence, Tuple

DEFAULT_MYSQL = r"C:\Program Files\MySQL\MySQL Server 8.0\bin\mysql.exe"

# Runtime enum PrerequisiteType / PrerequisiteComparison (Game.Static).
TYPE_LEVEL = 1
TYPE_REPUTATION = 5
TYPE_CLASS = 3
COMPARISON_EQUAL = 1
COMPARISON_GREATER_OR_EQUAL = 4

LEVEL_RE = re.compile(r"^(?:requires level (\d+)\.?|you must be level (\d+) to [^.]*\.?)$", re.IGNORECASE)
REPUTATION_RE = re.compile(r"^requires (\d+) reputation with (.+?)\.?$", re.IGNORECASE)
CLASS_RE = re.compile(r"^must be an? ([a-z]+)\.?$", re.IGNORECASE)

OUTPUT_FIELDS = [
    "prerequisite_text",
    "kind",
    "required_value",
    "faction_text",
    "faction2_id",
    "prerequisite_id",
    "prerequisite_id_exile",
    "prerequisite_id_dominion",
    "resolution",
    "detail",
]


@dataclass(frozen=True)
class ParsedPrerequisite:
    kind: str  # level | reputation | unparsed
    value: int = 0
    faction_text: str = ""


def normalize_text(value: object) -> str:
    text = str(value or "").replace("\\n", " ").replace("\n", " ")
    return re.sub(r"\s+", " ", text).strip()


def normalize_name(value: object) -> str:
    text = normalize_text(value).lower()
    text = re.sub(r"^the\s+", "", text)
    return re.sub(r"[^a-z0-9]+", " ", text).strip()


def parse_prerequisite_text(text: object) -> ParsedPrerequisite:
    cleaned = normalize_text(text)
    match = LEVEL_RE.match(cleaned)
    if match:
        return ParsedPrerequisite("level", int(match.group(1) or match.group(2)))
    match = REPUTATION_RE.match(cleaned)
    if match:
        return ParsedPrerequisite("reputation", int(match.group(1)), match.group(2).strip())
    match = CLASS_RE.match(cleaned)
    if match:
        return ParsedPrerequisite("class", 0, match.group(1))
    return ParsedPrerequisite("unparsed")


def faction_alternatives(faction_text: str) -> List[str]:
    """Jabbithole writes renamed factions as "<name A> or <name B>"; each side is a candidate."""
    parts = re.split(r"\s+or\s+", faction_text)
    return [part for part in (normalize_name(p) for p in parts) if part]


def resolve_faction(faction_text: str, faction_ids_by_name: Dict[str, List[int]]) -> Tuple[Optional[int], str]:
    found: List[int] = []
    for alternative in faction_alternatives(faction_text):
        for faction_id in faction_ids_by_name.get(alternative, []):
            if faction_id not in found:
                found.append(faction_id)
    if len(found) == 1:
        return found[0], "name_matched"
    if len(found) > 1:
        if len(faction_alternatives(faction_text)) > 1:
            return None, (
                f"side-specific faction pair ({','.join(str(i) for i in found)}): needs the vendor's side, not guessed"
            )
        return None, f"ambiguous faction name ({','.join(str(i) for i in found)})"
    return None, "faction name not found in client Faction2"


def lowest_equivalent_prerequisite(
    rows: Iterable[Dict[str, int]],
    prerequisite_type: int,
    object_id: int,
    value: int,
    comparison: int = COMPARISON_GREATER_OR_EQUAL,
) -> Optional[int]:
    """Lowest-id single-clause Prerequisite with type/object/value and the given comparison (default >=)."""
    best: Optional[int] = None
    for row in rows:
        if (
            row["type0"] == prerequisite_type
            and row["type1"] == 0
            and row["type2"] == 0
            and row["comparison0"] == comparison
            and row["object0"] == object_id
            and row["value0"] == value
        ):
            if best is None or row["id"] < best:
                best = row["id"]
    return best


def resolve_text(
    text: str,
    prerequisite_rows: Sequence[Dict[str, int]],
    faction_ids_by_name: Dict[str, List[int]],
    class_ids_by_name: Optional[Dict[str, int]] = None,
    faction_sides: Optional[Dict[int, str]] = None,
) -> Dict[str, object]:
    parsed = parse_prerequisite_text(text)
    row: Dict[str, object] = {
        "prerequisite_text": text,  # raw Jabbithole text: the exact join key used by the SQL import
        "kind": parsed.kind,
        "required_value": parsed.value or "",
        "faction_text": parsed.faction_text,
        "faction2_id": "",
        "prerequisite_id": 0,
        "prerequisite_id_exile": 0,
        "prerequisite_id_dominion": 0,
        "resolution": "unresolved",
        "detail": "",
    }
    if parsed.kind == "unparsed":
        row["detail"] = "text is not a single level, class or reputation clause"
        return row

    if parsed.kind == "class":
        class_id = (class_ids_by_name or {}).get(normalize_name(parsed.faction_text))
        if class_id is None:
            row["detail"] = "class name not found in client Class"
            return row
        prerequisite_id = lowest_equivalent_prerequisite(prerequisite_rows, TYPE_CLASS, 0, class_id, COMPARISON_EQUAL)
        if prerequisite_id is None:
            row["detail"] = "no single-clause equality Class Prerequisite row for this class"
            return row
        row["required_value"] = class_id
        row["prerequisite_id"] = prerequisite_id
        row["resolution"] = "semantic_equivalent_row"
        row["detail"] = "lowest-id equivalent client Prerequisite row; retail row identity unknown"
        return row

    if parsed.kind == "level":
        prerequisite_id = lowest_equivalent_prerequisite(prerequisite_rows, TYPE_LEVEL, 0, parsed.value)
        if prerequisite_id is None:
            row["detail"] = "no single-clause >= Prerequisite row with this type/object/value"
            return row
        row["prerequisite_id"] = prerequisite_id
        row["resolution"] = "semantic_equivalent_row"
        row["detail"] = "lowest-id equivalent client Prerequisite row; retail row identity unknown"
        return row

    faction_id, note = resolve_faction(parsed.faction_text, faction_ids_by_name)
    if faction_id is not None:
        row["faction2_id"] = faction_id
        prerequisite_id = lowest_equivalent_prerequisite(prerequisite_rows, TYPE_REPUTATION, faction_id, parsed.value)
        if prerequisite_id is None:
            row["detail"] = "no single-clause >= Prerequisite row with this type/object/value"
            return row
        row["prerequisite_id"] = prerequisite_id
        row["resolution"] = "semantic_equivalent_row"
        row["detail"] = "lowest-id equivalent client Prerequisite row; retail row identity unknown"
        return row

    # "<name A> or <name B>": try one faction per side.
    alternatives = faction_alternatives(parsed.faction_text)
    if len(alternatives) == 2 and faction_sides:
        by_side: Dict[str, List[int]] = {"exile": [], "dominion": []}
        for alternative in alternatives:
            for candidate in faction_ids_by_name.get(alternative, []):
                side = faction_sides.get(candidate)
                if side in by_side and candidate not in by_side[side]:
                    by_side[side].append(candidate)
        if len(by_side["exile"]) == 1 and len(by_side["dominion"]) == 1:
            exile_id = lowest_equivalent_prerequisite(prerequisite_rows, TYPE_REPUTATION, by_side["exile"][0], parsed.value)
            dominion_id = lowest_equivalent_prerequisite(prerequisite_rows, TYPE_REPUTATION, by_side["dominion"][0], parsed.value)
            if exile_id is not None and dominion_id is not None:
                row["prerequisite_id_exile"] = exile_id
                row["prerequisite_id_dominion"] = dominion_id
                row["resolution"] = "side_specific_pair"
                row["detail"] = (
                    f"exile faction {by_side['exile'][0]} / dominion faction {by_side['dominion'][0]}; the vendor's side "
                    "(creature_map.source_side) selects one, an unknown side stays ungated"
                )
                return row
    row["detail"] = note
    return row


def mysql_rows(args: argparse.Namespace, database: str, query: str) -> List[List[str]]:
    result = subprocess.run(
        [str(args.mysql_exe), f"--host={args.host}", f"--user={args.user}", f"--password={args.password}",
         "--default-character-set=utf8mb4", "-B", "-N", database, "-e", query],
        capture_output=True, text=True, encoding="utf-8",
    )
    if result.returncode != 0:
        raise RuntimeError(result.stderr.strip())
    return [line.split("\t") for line in result.stdout.splitlines()]


def load_prerequisite_rows(args: argparse.Namespace) -> List[Dict[str, int]]:
    query = (
        "SELECT ID, prerequisiteTypeId0, prerequisiteTypeId1, prerequisiteTypeId2, prerequisiteComparisonId0, "
        "objectId0, value0 FROM Prerequisite WHERE prerequisiteTypeId0 IN (1, 3, 5)"
    )
    rows = []
    for fields in mysql_rows(args, args.client_db, query):
        rows.append(
            {
                "id": int(fields[0]),
                "type0": int(fields[1]),
                "type1": int(fields[2]),
                "type2": int(fields[3]),
                "comparison0": int(fields[4]),
                "object0": int(fields[5]),
                "value0": int(fields[6]),
            }
        )
    return rows


def add_faction_name(index: Dict[str, List[int]], name: object, faction_id: object) -> None:
    key = normalize_name(name)
    if not key or faction_id in (None, ""):
        return
    try:
        value = int(float(faction_id))
    except ValueError:
        return  # non-numeric (e.g. a NULL marker): not a usable faction id
    if value not in index.setdefault(key, []):
        index[key].append(value)


def load_faction_ids_by_name(output_dir: Path) -> Dict[str, List[int]]:
    path = output_dir / "faction_map.csv"
    index: Dict[str, List[int]] = {}
    with path.open("r", encoding="utf-8", newline="") as handle:
        for row in csv.DictReader(handle):
            add_faction_name(index, row.get("jabbithole_faction_name"), row.get("faction2_id"))
            add_faction_name(index, row.get("client_faction_name"), row.get("faction2_id"))
    return index


def load_class_ids_by_name(output_dir: Path) -> Dict[str, int]:
    index: Dict[str, int] = {}
    with (output_dir / "class_map.csv").open("r", encoding="utf-8", newline="") as handle:
        for row in csv.DictReader(handle):
            class_id = row.get("class_id")
            if class_id:
                for name in (row.get("jabbithole_class_name"), row.get("client_class_name")):
                    if name:
                        index[normalize_name(name)] = int(float(class_id))
    return index


def faction_side(is_exile: object, is_dominion: object) -> Optional[str]:
    exile = str(is_exile).strip().lower() == "true"
    dominion = str(is_dominion).strip().lower() == "true"
    if exile and not dominion:
        return "exile"
    if dominion and not exile:
        return "dominion"
    return None


def load_faction_sides(output_dir: Path) -> Dict[int, str]:
    sides: Dict[int, str] = {}
    with (output_dir / "faction_map.csv").open("r", encoding="utf-8", newline="") as handle:
        for row in csv.DictReader(handle):
            side = faction_side(row.get("is_exile"), row.get("is_dominion"))
            if side and row.get("faction2_id"):
                sides[int(float(row["faction2_id"]))] = side
    return sides


def distinct_prerequisite_texts(output_dir: Path) -> List[str]:
    path = output_dir / "vendor_item_map.csv"
    texts = set()
    with path.open("r", encoding="utf-8", newline="") as handle:
        for row in csv.DictReader(handle):
            text = row.get("prerequisite") or ""
            if normalize_text(text):
                texts.add(text)
    return sorted(texts)


def load_from_staging_db(args: argparse.Namespace) -> Tuple[List[str], Dict[str, List[int]], Dict[str, int], Dict[int, str]]:
    texts = sorted(
        {
            fields[0]
            for fields in mysql_rows(
                args,
                args.mapping_db,
                "SELECT DISTINCT prerequisite FROM nf_map_vendor_item WHERE prerequisite IS NOT NULL AND prerequisite <> ''",
            )
            if fields and fields[0] and normalize_text(fields[0])
        }
    )
    index: Dict[str, List[int]] = {}
    sides: Dict[int, str] = {}
    for fields in mysql_rows(
        args,
        args.mapping_db,
        "SELECT jabbithole_faction_name, client_faction_name, faction2_id, is_exile, is_dominion FROM nf_map_faction "
        "WHERE faction2_id IS NOT NULL",
    ):
        add_faction_name(index, fields[0], fields[2])
        add_faction_name(index, fields[1], fields[2])
        side = faction_side(fields[3], fields[4])
        if side:
            sides[int(float(fields[2]))] = side
    classes: Dict[str, int] = {}
    for fields in mysql_rows(
        args, args.mapping_db,
        "SELECT jabbithole_class_name, client_class_name, class_id FROM nf_map_class WHERE class_id IS NOT NULL",
    ):
        for name in fields[:2]:
            if name and name != "NULL":
                classes[normalize_name(name)] = int(float(fields[2]))
    return texts, index, classes, sides


def main(argv: Optional[Sequence[str]] = None) -> int:
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument("--output-dir", type=Path, default=Path(__file__).with_name("output"))
    parser.add_argument("--client-db", default="wildstar_client")
    parser.add_argument("--mapping-db", default="nexus_forever_mapping")
    parser.add_argument("--from-staging-db", action="store_true", help="read texts and faction names from nf_map_* instead of CSVs")
    parser.add_argument("--mysql-exe", type=Path, default=Path(DEFAULT_MYSQL))
    parser.add_argument("--host", default="127.0.0.1")
    parser.add_argument("--user", default="bankai")
    parser.add_argument("--password", default="bankai")
    args = parser.parse_args(argv)

    prerequisite_rows = load_prerequisite_rows(args)
    if args.from_staging_db:
        texts, faction_ids_by_name, class_ids_by_name, faction_sides = load_from_staging_db(args)
    else:
        faction_ids_by_name = load_faction_ids_by_name(args.output_dir)
        class_ids_by_name = load_class_ids_by_name(args.output_dir)
        faction_sides = load_faction_sides(args.output_dir)
        texts = distinct_prerequisite_texts(args.output_dir)
    rows = [resolve_text(text, prerequisite_rows, faction_ids_by_name, class_ids_by_name, faction_sides) for text in texts]

    args.output_dir.mkdir(parents=True, exist_ok=True)
    out_path = args.output_dir / "vendor_prerequisite_map.csv"
    with out_path.open("w", encoding="utf-8", newline="") as handle:
        writer = csv.DictWriter(handle, fieldnames=OUTPUT_FIELDS, lineterminator="\n")
        writer.writeheader()
        writer.writerows(rows)

    resolved = sum(1 for r in rows if r["resolution"] in ("semantic_equivalent_row", "side_specific_pair"))
    print(f"{out_path}: {len(rows)} distinct texts, {resolved} resolved, {len(rows) - resolved} unresolved")
    return 0


if __name__ == "__main__":
    sys.exit(main())
