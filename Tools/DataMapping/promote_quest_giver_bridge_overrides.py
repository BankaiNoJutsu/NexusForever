#!/usr/bin/env python3
"""Resolve ambiguous creature bridges from the client's own quest-giver data.

`Creature2.questIdGiven00..16` / `questIdReceive00..24` list the quests a client creature starts and
finishes. Jabbithole records starter/finisher quests per creature (and its quest ids are exact `Quest2`
ids via `quests.game_id`). For an `ambiguous_name` creature the exact-name candidates tie on name, faction
and level, but if exactly ONE candidate's give/receive list contains one of the creature's Jabbithole
quests and no other candidate's does, that is independent evidence for that candidate.

Evidence tier `client_quest_giver`: a client table link, stronger than the `spatial_heuristic` tier and not
a retail capture. It identifies which same-name Creature2 gives/receives the quest in the client data; it
says nothing about spawn placement, loot or vendor stock. Version drift (a quest moved to another NPC) is
possible but would show up as "no candidate matches", which is left unresolved.

    python Tools/DataMapping/promote_quest_giver_bridge_overrides.py            # dry run
    python Tools/DataMapping/promote_quest_giver_bridge_overrides.py --apply

Needs the mapper outputs (`creature_bridge_review.csv`) and the authoring staging DB.
"""
from __future__ import annotations

import argparse
import csv
import subprocess
import sys
from collections import defaultdict
from pathlib import Path
from typing import Dict, Iterable, List, Mapping, Optional, Sequence, Set, Tuple

DEFAULT_MYSQL = r"C:\Program Files\MySQL\MySQL Server 8.0\bin\mysql.exe"
EVIDENCE_TIER = "client_quest_giver"
REVIEWER = "Codex client quest-giver evidence"
FIELDS = [
    "source_table", "source_id", "source_name", "chosen_creature2_id", "decision", "reason", "reviewer",
    "reviewed_at", "evidence_tier",
]


def resolve_by_quest_giver(
    candidate_ids: Sequence[int],
    candidate_quests: Mapping[int, Set[int]],
    source_quests: Set[int],
) -> Optional[Tuple[int, Set[int]]]:
    """Return (candidate, matching quests) when exactly one candidate lists any of the source's quests."""
    if not source_quests:
        return None
    hits = [(cid, candidate_quests.get(cid, set()) & source_quests) for cid in candidate_ids]
    hits = [(cid, matched) for cid, matched in hits if matched]
    if len(hits) != 1:
        return None
    return hits[0]


def mysql_rows(args: argparse.Namespace, database: str, query: str) -> List[List[str]]:
    result = subprocess.run(
        [str(args.mysql_exe), f"--host={args.host}", f"--user={args.user}", f"--password={args.password}",
         "--default-character-set=utf8mb4", "-B", "-N", database, "-e", query],
        capture_output=True, text=True, encoding="utf-8",
    )
    if result.returncode != 0:
        raise RuntimeError(result.stderr.strip())
    return [line.split("\t") for line in result.stdout.splitlines()]


def load_client_quests(args: argparse.Namespace) -> Dict[int, Set[int]]:
    give = " UNION ALL ".join(f"SELECT ID, questIdGiven{i:02d} FROM Creature2 WHERE questIdGiven{i:02d} > 0" for i in range(17))
    receive = " UNION ALL ".join(f"SELECT ID, questIdReceive{i:02d} FROM Creature2 WHERE questIdReceive{i:02d} > 0" for i in range(25))
    quests: Dict[int, Set[int]] = defaultdict(set)
    for creature, quest in mysql_rows(args, args.client_db, give + " UNION ALL " + receive):
        quests[int(creature)].add(int(quest))
    return quests


def load_source_quests(args: argparse.Namespace) -> Dict[int, Set[int]]:
    quests: Dict[int, Set[int]] = defaultdict(set)
    for source, quest in mysql_rows(
        args, args.mapping_db,
        "SELECT jabbithole_creature_id, quest2_id FROM nf_map_creature_quest "
        "WHERE relation_type IN ('starter', 'finisher') AND quest2_id IS NOT NULL",
    ):
        quests[int(source)].add(int(quest))
    return quests


MIN_FUZZY_NAME_SIMILARITY = 0.6
# Reviewed rejections: the quest link exists but the client NPC has a different person name, which looks like a
# quest handed to another NPC between game versions (version drift), not a rename of the same NPC.
REVIEWED_REJECTIONS = {
    129: "Tech Sergeant Kreng vs client 'Tech Sergeant Prox': different person",
    7883: "Pilot Paska vs client 'Pilot Frazza': different person",
}


def load_ambiguous_candidates(review_csv: Path) -> Dict[int, List[dict]]:
    """Exact-name candidates of ambiguous creatures, and fuzzy-name candidates of unmatched (renamed) ones.

    A fuzzy candidate needs a name similarity of at least MIN_FUZZY_NAME_SIMILARITY so a quest link alone
    cannot pair unrelated names; the quest link then decides among those similar-named candidates.
    """
    candidates: Dict[int, List[dict]] = defaultdict(list)
    with review_csv.open("r", encoding="utf-8", newline="") as handle:
        for row in csv.DictReader(handle):
            status = row.get("current_status")
            reason = row.get("candidate_reason", "")
            if status == "ambiguous_name" and reason == "exact_name":
                candidates[int(row["source_id"])].append(row)
            elif status == "unmatched" and reason.startswith("fuzzy"):
                try:
                    similarity = float(row.get("name_similarity") or 0)
                except ValueError:
                    similarity = 0.0
                if similarity >= MIN_FUZZY_NAME_SIMILARITY:
                    candidates[int(row["source_id"])].append(row)
    return candidates


def read_tracked(path: Path) -> List[dict]:
    with path.open("r", encoding="utf-8", newline="") as handle:
        return list(csv.DictReader(handle))


def main(argv: Optional[Sequence[str]] = None) -> int:
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    here = Path(__file__).resolve().parent
    parser.add_argument("--review-csv", type=Path, default=here / "output" / "creature_bridge_review.csv")
    parser.add_argument("--tracked", type=Path, default=here / "creature_bridge_overrides.csv")
    parser.add_argument("--client-db", default="wildstar_client")
    parser.add_argument("--mapping-db", default="nexus_forever_mapping")
    parser.add_argument("--mysql-exe", type=Path, default=Path(DEFAULT_MYSQL))
    parser.add_argument("--host", default="127.0.0.1")
    parser.add_argument("--user", default="bankai")
    parser.add_argument("--password", default="bankai")
    parser.add_argument("--reviewed-at", default="2026-11-11")
    parser.add_argument("--apply", action="store_true")
    args = parser.parse_args(argv)

    tracked = read_tracked(args.tracked)
    tracked_ids = {row["source_id"] for row in tracked}
    client_quests = load_client_quests(args)
    source_quests = load_source_quests(args)
    candidates = load_ambiguous_candidates(args.review_csv)

    new_rows: List[dict] = []
    changed_guess = 0
    for source_id in sorted(candidates):
        if str(source_id) in tracked_ids or source_id in REVIEWED_REJECTIONS:
            continue
        rows = candidates[source_id]
        ids = [int(r["candidate_creature2_id"]) for r in rows]
        resolved = resolve_by_quest_giver(ids, client_quests, source_quests.get(source_id, set()))
        if resolved is None:
            continue
        chosen, matched = resolved
        unmatched = rows[0].get("current_status") == "unmatched"
        if not unmatched and str(chosen) != rows[0]["current_creature2_id"]:
            changed_guess += 1
        chosen_name = next(r["candidate_client_name"] for r in rows if int(r["candidate_creature2_id"]) == chosen)
        quest_list = "/".join(str(q) for q in sorted(matched))
        new_rows.append(
            {
                "source_table": "creatures",
                "source_id": source_id,
                "source_name": rows[0]["source_name"],
                "chosen_creature2_id": chosen,
                "decision": "approved",
                "reason": (
                    f"client_quest_giver: Creature2 {chosen} lists Quest2 {quest_list} in questIdGiven/questIdReceive "
                    f"and none of the other {len(ids) - 1} {'similar-name' if unmatched else 'exact-name'} candidates list any of "
                    f"this creature's {len(source_quests[source_id])} Jabbithole starter/finisher quests"
                    + (f"; renamed NPC (client name '{chosen_name}', no exact-name match)" if unmatched else "")
                ),
                "reviewer": REVIEWER,
                "reviewed_at": args.reviewed_at,
                "evidence_tier": EVIDENCE_TIER,
            }
        )

    print(f"candidate sources (ambiguous + unmatched with similar names): {len(candidates)}; newly resolvable by client quest-giver data: {len(new_rows)} "
          f"(replaces a different current best guess for {changed_guess})")
    if not args.apply:
        print("dry run; pass --apply to append to", args.tracked)
        return 0
    with args.tracked.open("w", encoding="utf-8", newline="") as handle:
        writer = csv.DictWriter(handle, fieldnames=FIELDS, lineterminator="\n", extrasaction="ignore")
        writer.writeheader()
        writer.writerows(tracked + new_rows)
    print("wrote", args.tracked)
    return 0


if __name__ == "__main__":
    sys.exit(main())
