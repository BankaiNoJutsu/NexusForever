import csv
import unittest
from pathlib import Path

import map_wildstar_data as mapper


class CreatureScoringTests(unittest.TestCase):
    def jabbit(self, **overrides):
        row = {"source_name": "Guard", "faction": "166", "level_min": "10", "level_max": "12", "difficulty": "1",
               "datacube_id": "", "race": "1", "gender": "0", "archetype_id": "5"}
        row.update(overrides)
        return row

    def client(self, **overrides):
        row = {"clientName": "Guard", "factionId": "166", "minLevel": "10", "maxLevel": "12",
               "creature2DifficultyId": "1", "datacubeId": "", "unitRaceId": "69", "creature2ArcheTypeId": "5"}
        row.update(overrides)
        return row

    def test_difficulty_enum_and_row_id_equality_is_not_evidence(self) -> None:
        equal = mapper.score_creature_match(self.jabbit(difficulty="1"), self.client(creature2DifficultyId="1"))
        unequal = mapper.score_creature_match(self.jabbit(difficulty="1"), self.client(creature2DifficultyId="9"))
        self.assertEqual(equal, unequal)

    def test_tiebreak_signals_do_not_change_the_primary_score(self) -> None:
        with_signals = mapper.score_creature_match(self.jabbit(), self.client())
        without = mapper.score_creature_match(self.jabbit(race="", archetype_id=""), self.client(unitRaceId="0", creature2ArcheTypeId="0"))
        self.assertEqual(with_signals, without)

    def test_tiebreak_prefers_matching_archetype_and_race(self) -> None:
        good = mapper.score_creature_tiebreak(self.jabbit(), self.client())
        wrong = mapper.score_creature_tiebreak(self.jabbit(), self.client(unitRaceId="999", creature2ArcheTypeId="6"))
        self.assertGreater(good, wrong)

    def test_race_compat_only_uses_well_supported_pairs(self) -> None:
        table = mapper.race_compat_table()
        self.assertIn(69, table[(1, 0)])
        self.assertIn(70, table[(1, 1)])
        # (1, 1) -> 69 appears in fewer than RACE_COMPAT_MIN_ROWS unique_name rows, so it is not trusted.
        self.assertNotIn(69, table[(1, 1)])

    def test_race_compat_file_declares_its_evidence_tier(self) -> None:
        with mapper.RACE_COMPAT_FILE.open("r", encoding="utf-8", newline="") as handle:
            tiers = {row["evidence_tier"] for row in csv.DictReader(handle)}
        self.assertEqual({"empirical_unique_name"}, tiers)

    def test_tiebreak_never_creates_a_scored_name(self) -> None:
        creatures = {
            1: {"ID": 1, "clientName": "Guard", "clientNameNormalized": "guard", "factionId": 166, "minLevel": 10,
                "maxLevel": 12, "unitRaceId": 69, "creature2ArcheTypeId": 5},
            2: {"ID": 2, "clientName": "Guard", "clientNameNormalized": "guard", "factionId": 166, "minLevel": 10,
                "maxLevel": 12, "unitRaceId": 999, "creature2ArcheTypeId": 6},
        }
        jabbit = dict(self.jabbit(), jabbithole_creature_id="7")

        class Args:
            review_candidate_limit = 4

        rows, _, _ = mapper.build_creature_map(Args(), [jabbit], creatures, {})
        self.assertEqual("ambiguous_name", rows[0]["match_status"])  # tie on the primary score stays ambiguous
        self.assertEqual(1, rows[0]["creature2_id"])  # ...but the better-supported candidate is the emitted guess
        self.assertEqual(4, rows[0]["match_tiebreak_score"])


class HealthAndSideTests(unittest.TestCase):
    def test_placeholder_and_int32_overflow_health_are_not_template_health(self) -> None:
        for value in ["1", "5", "10", "1410065408"]:
            with self.subTest(value=value):
                self.assertIsNone(mapper.template_health_value(value, value))

    def test_real_health_is_kept(self) -> None:
        self.assertEqual(341575936.0, mapper.template_health_value("341575936", "341575936"))
        self.assertEqual(11.0, mapper.template_health_value("11", "11"))

    def test_vendor_side_needs_exactly_one_friendly_side(self) -> None:
        self.assertEqual("exile", mapper.creature_side({"disposition_exile": "2", "disposition_dominion": "0"}))
        self.assertEqual("dominion", mapper.creature_side({"disposition_exile": "", "disposition_dominion": "2"}))
        self.assertEqual("", mapper.creature_side({"disposition_exile": "2", "disposition_dominion": "2"}))
        self.assertEqual("", mapper.creature_side({}))


class JabbitholeIdDomainTests(unittest.TestCase):
    def test_zone_game_id_is_a_map_zone_id_resolved_to_its_world_zone(self) -> None:
        client = {"map_zones": {2: {"worldZoneId": 137}, 9: {"worldZoneId": 0}}}
        row = {"world_zone_id": "2"}
        mapper.apply_jabbithole_zone(row, client)
        self.assertEqual((2, 137), (row["map_zone_id"], row["world_zone_id"]))
        unknown = {"world_zone_id": "9999"}
        mapper.apply_jabbithole_zone(unknown, client)
        self.assertEqual("", unknown["world_zone_id"])  # unknown MapZone is never passed off as a WorldZone

    def test_title_resolves_by_unique_client_text_not_by_jabbithole_id(self) -> None:
        client = {"character_titles": {
            251: {"characterTitleText": "Blade-Wind"},
            7: {"characterTitleText": "Test All Title 2"},
            8: {"characterTitleText": "Dup"}, 9: {"characterTitleText": "dup"},
        }}
        self.assertEqual("251", mapper.resolve_client_title_id(client, "Blade-Wind"))
        self.assertEqual("", mapper.resolve_client_title_id(client, "Dup"))  # ambiguous
        self.assertEqual("", mapper.resolve_client_title_id(client, "Nonexistent"))


class ReviewEvidenceTierTests(unittest.TestCase):
    def test_spatial_heuristic_rows_are_tagged(self) -> None:
        row = {"reason": "existing_entity_spatial: only candidate with existing entity near Jabbithole spawn",
               "reviewer": "Codex spatial evidence review"}
        self.assertEqual("spatial_heuristic", mapper.infer_review_evidence_tier(row))

    def test_hand_reviewed_rows_are_manual_evidence(self) -> None:
        row = {"reason": "PE390 relation 2253, TargetGroup 12672", "reviewer": "Codex DM-FND review"}
        self.assertEqual("manual_evidence", mapper.infer_review_evidence_tier(row))

    def test_explicit_tier_wins(self) -> None:
        row = {"reason": "existing_entity_spatial: x", "reviewer": "someone", "evidence_tier": "retail_capture"}
        self.assertEqual("retail_capture", mapper.infer_review_evidence_tier(row))


class TrackedOverrideFileTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls) -> None:
        with Path(__file__).with_name("creature_bridge_overrides.csv").open("r", encoding="utf-8", newline="") as handle:
            cls.rows = list(csv.DictReader(handle))

    def test_every_row_parses_without_spilled_columns_and_has_a_tier(self) -> None:
        for row in self.rows:
            self.assertNotIn(None, row)
            self.assertIn(row["evidence_tier"], {"spatial_heuristic", "manual_evidence", "client_quest_giver", "client_target_group"})
            if row["evidence_tier"] in ("client_quest_giver", "client_target_group"):
                self.assertTrue(row["reason"].startswith(row["evidence_tier"] + ":"))

    def test_source_ids_are_unique(self) -> None:
        ids = [row["source_id"] for row in self.rows]
        self.assertEqual(len(ids), len(set(ids)))


if __name__ == "__main__":
    unittest.main()
