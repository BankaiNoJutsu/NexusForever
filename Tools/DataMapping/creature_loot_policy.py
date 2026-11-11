"""Selection and normalization policy for mapped creature loot.

Jabbithole `versioned_item_drops.drop_times` counts how often an item was seen dropping from a creature;
the creature's aggregate `versioned_item_drops_sum` is the total of `drop_times` over *all of that
creature's item rows*. `drop_times / sum` is therefore each item's share of the creature's observed
drops. It is NOT a per-kill probability: Jabbithole records no kill counts, so the true per-kill chance
is unknown. The runtime rolls each row independently at `chance`, so for one consistent source the shares
add to 1.0 (about one item per kill); the original import broke that by

* unioning rows from game versions 4, 6 and 7 for the same creature,
* unioning several Jabbithole creatures that bridge to the same Creature2 without pooling their
  denominators, and
* letting veteran-difficulty Jabbithole creatures (`is_veteran`) feed the normal creature.

Policy, per Creature2:

1. Drop veteran-sourced rows when at least one non-veteran source has loot (veteran-only Creature2s keep
   their veteran loot - there is nothing else to use).
2. Keep only the latest game version that remains.
3. Pool the remaining Jabbithole creatures: per item `sum(drop_times)`, denominator = sum of the distinct
   creature aggregates' `aggregate_drop_sum`. Shares therefore add to at most 1.0.

No retail parity claim: this removes a known inflation, it does not recover the real drop rate.
"""
from __future__ import annotations

from collections import defaultdict
from typing import Dict, Iterable, List, Mapping, Set, Tuple


def _int(value, default: int = 0) -> int:
    try:
        text = str(value).strip()
        return int(float(text)) if text else default
    except ValueError:
        return default


_STATUS_STRENGTH = {"scored_name": 0, "reviewed": 1, "unique_name": 2}


def normalize_creature_loot(
    rows: Iterable[Mapping[str, str]],
    veteran_jabbithole_creature_ids: Set[int],
) -> Tuple[List[Dict[str, object]], Dict[str, int]]:
    """Return one pooled row per (creature2_id, item2_id) and policy statistics.

    `rows` must already be filtered to eligible statuses with creature2_id and item2_id set.
    """
    by_creature: Dict[int, List[Mapping[str, str]]] = defaultdict(list)
    for row in rows:
        by_creature[_int(row.get("creature2_id"))].append(row)

    stats = {
        "creatures": 0,
        "veteran_rows_dropped": 0,
        "older_version_rows_dropped": 0,
        "veteran_only_creatures_kept": 0,
        "multi_source_creatures_pooled": 0,
    }
    pooled_rows: List[Dict[str, object]] = []

    for creature2_id in sorted(by_creature):
        rows_for_creature = by_creature[creature2_id]
        stats["creatures"] += 1

        def is_veteran(row: Mapping[str, str]) -> bool:
            return _int(row.get("jabbithole_creature_id")) in veteran_jabbithole_creature_ids

        has_normal = any(not is_veteran(r) for r in rows_for_creature)
        if has_normal:
            selected = [r for r in rows_for_creature if not is_veteran(r)]
            stats["veteran_rows_dropped"] += len(rows_for_creature) - len(selected)
        else:
            selected = list(rows_for_creature)
            stats["veteran_only_creatures_kept"] += 1

        latest_version = max(_int(r.get("game_version")) for r in selected)
        latest = [r for r in selected if _int(r.get("game_version")) == latest_version]
        stats["older_version_rows_dropped"] += len(selected) - len(latest)

        # One aggregate row per (Jabbithole creature, version); its sum is the denominator.
        aggregate_sums: Dict[int, int] = {}
        for r in latest:
            aggregate_id = _int(r.get("versioned_creature_drop_aggregate_id"))
            aggregate_sums.setdefault(aggregate_id, _int(r.get("aggregate_drop_sum")))
        denominator = sum(aggregate_sums.values())
        if len(aggregate_sums) > 1:
            stats["multi_source_creatures_pooled"] += 1

        per_item: Dict[int, List[Mapping[str, str]]] = defaultdict(list)
        for r in latest:
            per_item[_int(r.get("item2_id"))].append(r)

        for item2_id in sorted(per_item):
            item_rows = per_item[item2_id]
            drop_times = sum(_int(r.get("drop_times")) for r in item_rows)
            chance = min(1.0, drop_times / denominator) if denominator > 0 else 0.0
            weakest = min((str(r.get("match_status", "")) for r in item_rows), key=lambda s: _STATUS_STRENGTH.get(s, -1))
            best_row = max(item_rows, key=lambda r: _int(r.get("source_drop_id")))
            pooled_rows.append(
                {
                    "creature2_id": creature2_id,
                    "item2_id": item2_id,
                    "chance": chance,
                    "drop_times": drop_times,
                    "aggregate_drop_sum": denominator,
                    "aggregate_drop_count": len(per_item),
                    "game_version": latest_version,
                    "source_drop_id": _int(best_row.get("source_drop_id")),
                    "versioned_item_drop_aggregate_id": _int(best_row.get("versioned_item_drop_aggregate_id")),
                    "versioned_creature_drop_aggregate_id": _int(best_row.get("versioned_creature_drop_aggregate_id")),
                    "last_seen_in": max(_int(r.get("last_seen_in")) for r in item_rows),
                    "match_status": weakest,
                    "source_name": str(best_row.get("source_name", "")),
                    "item_name": str(best_row.get("item_name", "")),
                }
            )
    return pooled_rows, stats
