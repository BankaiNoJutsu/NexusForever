import argparse
import csv
import tempfile
import unittest
from pathlib import Path

import apply_creature_loot
import apply_vendor_items


def write_csv(path: Path, fieldnames, rows) -> None:
    with path.open("w", encoding="utf-8", newline="") as handle:
        writer = csv.DictWriter(handle, fieldnames=fieldnames, lineterminator="\n")
        writer.writeheader()
        writer.writerows(rows)


class ApplyVendorItemsPrerequisiteTests(unittest.TestCase):
    def setUp(self) -> None:
        self.dir = Path(tempfile.mkdtemp())
        vendor_fields = ["vendor_item_source_id", "creature2_id", "item2_id", "match_status", "game_version",
                         "main_currency_id", "main_price", "alt_currency_id", "alt_price", "source_name", "item_name",
                         "prerequisite"]
        write_csv(self.dir / "vendor_item_map.csv", vendor_fields, [
            {"vendor_item_source_id": 1, "creature2_id": 10, "item2_id": 100, "match_status": "unique_name",
             "game_version": 7, "main_currency_id": 1, "main_price": 5, "alt_currency_id": 0, "alt_price": 0,
             "source_name": "V", "item_name": "Gated", "prerequisite": "Requires Level 50"},
            {"vendor_item_source_id": 2, "creature2_id": 10, "item2_id": 101, "match_status": "unique_name",
             "game_version": 7, "main_currency_id": 1, "main_price": 5, "alt_currency_id": 0, "alt_price": 0,
             "source_name": "V", "item_name": "Unresolved", "prerequisite": "Must be a Medic"},
            {"vendor_item_source_id": 3, "creature2_id": 10, "item2_id": 102, "match_status": "unique_name",
             "game_version": 7, "main_currency_id": 1, "main_price": 5, "alt_currency_id": 0, "alt_price": 0,
             "source_name": "V", "item_name": "Free", "prerequisite": ""},
        ])
        write_csv(self.dir / "vendor_prerequisite_map.csv", ["prerequisite_text", "prerequisite_id", "resolution"], [
            {"prerequisite_text": "Requires Level 50", "prerequisite_id": 627, "resolution": "semantic_equivalent_row"},
            {"prerequisite_text": "Must be a Medic", "prerequisite_id": 0, "resolution": "unresolved"},
        ])
        self.args = argparse.Namespace(
            vendor_map=self.dir / "vendor_item_map.csv",
            prerequisite_map=self.dir / "vendor_prerequisite_map.csv",
            include_ambiguous=False,
            include_unmatched=False,
        )

    def test_resolved_prerequisite_is_imported_and_unresolved_is_not_guessed(self) -> None:
        rows, _ = apply_vendor_items.load_vendor_rows(self.args)
        by_item = {row.item2_id: row.prerequisite_id for row in rows}
        self.assertEqual({100: 627, 101: 0, 102: 0}, by_item)

    def test_side_specific_pair_follows_the_vendor_side(self) -> None:
        self.assertEqual(7, apply_vendor_items.resolve_prerequisite_id({"exile": 7, "dominion": 8}, "exile"))
        self.assertEqual(8, apply_vendor_items.resolve_prerequisite_id({"exile": 7, "dominion": 8}, "dominion"))
        self.assertEqual(0, apply_vendor_items.resolve_prerequisite_id({"exile": 7, "dominion": 8}, ""))
        self.assertEqual(627, apply_vendor_items.resolve_prerequisite_id({"any": 627}, ""))

    def test_missing_prerequisite_map_stops_the_import(self) -> None:
        self.args.prerequisite_map = self.dir / "missing.csv"
        with self.assertRaises(FileNotFoundError):
            apply_vendor_items.load_vendor_rows(self.args)


class ApplyCreatureLootPolicyTests(unittest.TestCase):
    def test_loader_applies_the_normalization_policy(self) -> None:
        directory = Path(tempfile.mkdtemp())
        loot_fields = ["source_drop_id", "jabbithole_creature_id", "creature2_id", "match_status", "item2_id",
                       "game_version", "drop_times", "aggregate_drop_sum", "aggregate_drop_count",
                       "versioned_item_drop_aggregate_id", "versioned_creature_drop_aggregate_id", "last_seen_in",
                       "source_name", "item_name", "drop_probability"]

        def loot(drop_id, jh, item, version, drops, agg_sum, agg_id):
            return {"source_drop_id": drop_id, "jabbithole_creature_id": jh, "creature2_id": 1,
                    "match_status": "unique_name", "item2_id": item, "game_version": version, "drop_times": drops,
                    "aggregate_drop_sum": agg_sum, "aggregate_drop_count": 2, "versioned_item_drop_aggregate_id": 0,
                    "versioned_creature_drop_aggregate_id": agg_id, "last_seen_in": version, "source_name": "C",
                    "item_name": f"I{item}", "drop_probability": "0.5"}

        write_csv(directory / "creature_loot_map.csv", loot_fields, [
            loot(1, 10, 100, 4, 5, 10, 800),   # older version: dropped
            loot(2, 10, 100, 7, 1, 4, 900),
            loot(3, 10, 101, 7, 3, 4, 900),
            loot(4, 11, 999, 7, 9, 9, 901),    # veteran source: dropped
        ])
        write_csv(directory / "creature_map.csv", ["jabbithole_creature_id", "source_is_veteran"], [
            {"jabbithole_creature_id": 10, "source_is_veteran": "false"},
            {"jabbithole_creature_id": 11, "source_is_veteran": "true"},
        ])
        args = argparse.Namespace(
            loot_map=directory / "creature_loot_map.csv",
            creature_map=directory / "creature_map.csv",
            include_ambiguous=False,
            include_unmatched=False,
        )
        rows, stats = apply_creature_loot.load_loot_rows(args)
        self.assertEqual({100: "0.25000000", 101: "0.75000000"}, {r.item2_id: r.chance for r in rows})
        self.assertEqual(1, stats["policy_veteran_rows_dropped"])
        self.assertEqual(1, stats["policy_older_version_rows_dropped"])

    def test_old_creature_map_without_veteran_column_is_rejected(self) -> None:
        directory = Path(tempfile.mkdtemp())
        write_csv(directory / "creature_map.csv", ["jabbithole_creature_id"], [{"jabbithole_creature_id": 1}])
        with self.assertRaises(RuntimeError):
            apply_creature_loot.load_veteran_jabbithole_creature_ids(directory / "creature_map.csv")


if __name__ == "__main__":
    unittest.main()
