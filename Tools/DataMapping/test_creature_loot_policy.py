import unittest

from creature_loot_policy import normalize_creature_loot


def row(creature2, jh_creature, item, version, drops, agg_sum, agg_id, status="unique_name", drop_id=None):
    return {
        "creature2_id": str(creature2),
        "jabbithole_creature_id": str(jh_creature),
        "item2_id": str(item),
        "game_version": str(version),
        "drop_times": str(drops),
        "aggregate_drop_sum": str(agg_sum),
        "versioned_creature_drop_aggregate_id": str(agg_id),
        "versioned_item_drop_aggregate_id": "0",
        "source_drop_id": str(drop_id if drop_id is not None else item * 100 + version),
        "last_seen_in": str(version),
        "match_status": status,
        "source_name": "Creature",
        "item_name": f"Item {item}",
    }


class CreatureLootPolicyTests(unittest.TestCase):
    def test_single_source_shares_add_up_to_one(self) -> None:
        rows = [row(1, 10, 100, 7, 3, 10, 900), row(1, 10, 101, 7, 7, 10, 900)]
        out, _ = normalize_creature_loot(rows, set())
        self.assertAlmostEqual(1.0, sum(r["chance"] for r in out))

    def test_only_the_latest_game_version_is_used(self) -> None:
        rows = [
            row(1, 10, 100, 4, 5, 10, 800),
            row(1, 10, 101, 4, 5, 10, 800),
            row(1, 10, 100, 7, 1, 4, 900),
            row(1, 10, 102, 7, 3, 4, 900),
        ]
        out, stats = normalize_creature_loot(rows, set())
        self.assertEqual({100, 102}, {r["item2_id"] for r in out})
        self.assertEqual({7}, {r["game_version"] for r in out})
        self.assertEqual(2, stats["older_version_rows_dropped"])
        self.assertAlmostEqual(1.0, sum(r["chance"] for r in out))

    def test_veteran_source_is_dropped_when_a_normal_source_exists(self) -> None:
        rows = [row(1, 10, 100, 7, 1, 1, 900), row(1, 11, 999, 7, 1, 1, 901)]
        out, stats = normalize_creature_loot(rows, {11})
        self.assertEqual([100], [r["item2_id"] for r in out])
        self.assertEqual(1, stats["veteran_rows_dropped"])

    def test_veteran_only_creature_keeps_its_loot(self) -> None:
        out, stats = normalize_creature_loot([row(1, 11, 999, 7, 1, 1, 901)], {11})
        self.assertEqual([999], [r["item2_id"] for r in out])
        self.assertEqual(1, stats["veteran_only_creatures_kept"])

    def test_sources_bridging_to_one_creature_pool_their_denominators(self) -> None:
        # two Jabbithole creatures, same Creature2, same version: 6/10 and 2/10 pooled over 20.
        rows = [
            row(1, 10, 100, 7, 6, 10, 900),
            row(1, 10, 101, 7, 4, 10, 900),
            row(1, 12, 100, 7, 2, 10, 902),
            row(1, 12, 102, 7, 8, 10, 902),
        ]
        out, stats = normalize_creature_loot(rows, set())
        by_item = {r["item2_id"]: r for r in out}
        self.assertAlmostEqual(8 / 20, by_item[100]["chance"])
        self.assertAlmostEqual(1.0, sum(r["chance"] for r in out))
        self.assertEqual(1, stats["multi_source_creatures_pooled"])
        self.assertEqual(8, by_item[100]["drop_times"])

    def test_weakest_match_status_is_reported_for_pooled_rows(self) -> None:
        rows = [row(1, 10, 100, 7, 1, 2, 900, "unique_name"), row(1, 12, 100, 7, 1, 2, 902, "scored_name")]
        out, _ = normalize_creature_loot(rows, set())
        self.assertEqual("scored_name", out[0]["match_status"])

    def test_chance_is_never_above_one(self) -> None:
        out, _ = normalize_creature_loot([row(1, 10, 100, 7, 50, 10, 900)], set())
        self.assertEqual(1.0, out[0]["chance"])

    def test_zero_denominator_yields_zero_chance(self) -> None:
        out, _ = normalize_creature_loot([row(1, 10, 100, 7, 5, 0, 900)], set())
        self.assertEqual(0.0, out[0]["chance"])


if __name__ == "__main__":
    unittest.main()
