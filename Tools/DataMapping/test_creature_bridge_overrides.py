import csv
import unittest
from pathlib import Path


class SpaceMadnessCreatureBridgeOverrideTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls) -> None:
        override_path = Path(__file__).with_name("creature_bridge_overrides.csv")
        with override_path.open("r", encoding="utf-8", newline="") as handle:
            cls.rows = {
                int(row["source_id"]): row
                for row in csv.DictReader(handle)
                if row["source_table"] == "creatures"
            }

    def test_normal_crazed_handler_bridge_is_approved_from_exact_row_evidence(self) -> None:
        row = self.rows[3631]

        self.assertEqual("Crazed Handler", row["source_name"])
        self.assertEqual("46671", row["chosen_creature2_id"])
        self.assertEqual("approved", row["decision"])
        for evidence in [
            "PE390 relation2253",
            "exact level32 faction218 race1",
            "107 world2149 area2421 source coordinates",
            "TargetGroup12672 containing 46671",
        ]:
            with self.subTest(evidence=evidence):
                self.assertIn(evidence, row["reason"])

    def test_normal_crazed_handler_bridge_does_not_collapse_same_name_variants(self) -> None:
        normal_row = self.rows[3631]
        prime_row = self.rows[28088]

        self.assertNotIn(normal_row["chosen_creature2_id"], {"46672", "69143", "69144"})
        self.assertEqual("69143", prime_row["chosen_creature2_id"])
        self.assertNotEqual(
            normal_row["chosen_creature2_id"],
            prime_row["chosen_creature2_id"],
        )

    def test_skullcano_exit_portal_bridge_uses_exact_client_portal_link(self) -> None:
        row = self.rows[12396]

        self.assertEqual("Leave Skullcano", row["source_name"])
        self.assertEqual("32419", row["chosen_creature2_id"])
        self.assertEqual("approved", row["decision"])
        for evidence in [
            "Creature2 32419 references InstancePortal27",
            "Creature2 40772 references InstancePortal43",
            "source coordinates 285998 and 415265",
            "current-last-seen7",
        ]:
            with self.subTest(evidence=evidence):
                self.assertIn(evidence, row["reason"])

    def test_skullcano_exit_portal_bridge_preserves_both_current_placements(self) -> None:
        row = self.rows[12396]

        self.assertIn("both placements are retained", row["reason"])
        self.assertIn("activation destination return prerequisite behavior", row["reason"])
        self.assertNotEqual("40772", row["chosen_creature2_id"])

    def test_ultimate_protogames_tea_cup_bridge_uses_exact_achievement_spell(self) -> None:
        row = self.rows[29896]

        self.assertEqual("Tea Cup", row["source_name"])
        self.assertEqual("69883", row["chosen_creature2_id"])
        self.assertEqual("approved", row["decision"])
        for evidence in [
            "coordinate6289665",
            "beside uniquely mapped Wiggle Wellingsworth",
            "Creature2 69883 is explicitly [UP] Tea Cup - Achievement NPC",
            "activate spell79328 plus prerequisite37093",
            "effect208728 is AchievementAdvance with achievement5881 amount1",
        ]:
            with self.subTest(evidence=evidence):
                self.assertIn(evidence, row["reason"])

    def test_ultimate_protogames_tea_cup_bridge_rejects_generic_grimvault_variant(self) -> None:
        row = self.rows[29896]

        self.assertIn("candidate56874 is a Grimvault flavor NPC", row["reason"])
        self.assertIn("with no activate spell", row["reason"])
        self.assertNotEqual("56874", row["chosen_creature2_id"])


if __name__ == "__main__":
    unittest.main()
