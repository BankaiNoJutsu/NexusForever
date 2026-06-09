import unittest
from pathlib import Path

import analyze_laughingws_worlddb as audit


class LaughingWsWorldDbClassificationTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls) -> None:
        cls.repo_root = _find_repo_root()
        cls.laughingws_root = cls.repo_root / "artifacts" / "external" / "LaughingWS.WorldDatabase.New-Zones-and-more"
        if not cls.laughingws_root.exists():
            raise unittest.SkipTest(f"LaughingWS external snapshot not found: {cls.laughingws_root}")

        cls.files_by_path = {
            info.relative_path.replace("\\", "/").lower(): info
            for info in audit.scan_repository(cls.laughingws_root, cls.repo_root, include_script_check=False).values()
        }

    def test_broad_new_zone_sources_remain_blocked(self) -> None:
        for relative_path in [
            "Map/Open World/New Zones/Dreadmoor.sql",
            "Map/Open World/New Zones/Halon Ring.sql",
            "Map/Open World/New Zones/Murkmire.sql",
        ]:
            with self.subTest(relative_path=relative_path):
                recommendations = self._recommendations(relative_path)

                self.assertIn("blocked_laughingws_new_zone_broad_rows", recommendations)
                self.assertNotIn("extract_additive_world_overlay", recommendations)

    def test_test_unknown_unfinished_and_not_in_client_sources_remain_sandbox_only(self) -> None:
        sandbox_files = [
            info.relative_path
            for info in self.files_by_path.values()
            if "test zones/" in info.relative_path.replace("\\", "/").lower()
        ]

        self.assertEqual(23, len(sandbox_files))
        for relative_path in sandbox_files:
            with self.subTest(relative_path=relative_path):
                recommendations = self._recommendations(relative_path)

                self.assertIn("sandbox_only_client_asset_check_required", recommendations)
                self.assertNotIn("extract_additive_world_overlay", recommendations)

    def test_build_16042_tutorial_replacements_remain_audit_only(self) -> None:
        expectations = {
            "Map/Instances/Tutorial Zones/Dominion Arkship.sql": "rejected_historical_arkship_tutorial_rows",
            "Map/Instances/Tutorial Zones/Exile Arkship.sql": "rejected_historical_arkship_tutorial_rows",
            "Map/Instances/Tutorial Zones/New Tutorial.sql": "skip_replaces_required_riders_reef_import",
        }

        for relative_path, recommendation in expectations.items():
            with self.subTest(relative_path=relative_path):
                recommendations = self._recommendations(relative_path)

                self.assertIn(recommendation, recommendations)
                self.assertNotIn("extract_additive_world_overlay", recommendations)

    def test_open_world_residual_reviews_remain_blocked_or_rejected(self) -> None:
        expectations = {
            "Map/Open World/Alizar/Algoroc.sql": "blocked_laughingws_algoroc_residual_rows",
            "Map/Open World/Alizar/Celestion.sql": "blocked_laughingws_celestion_residual_rows",
            "Map/Open World/Alizar/Galeras.sql": "blocked_laughingws_galeras_residual_rows",
            "Map/Open World/Alizar/Thayd.sql": "blocked_laughingws_thayd_residual_rows",
            "Map/Open World/Alizar/Whitevale.sql": "blocked_laughingws_whitevale_residual_rows",
            "Map/Open World/Olyssia/Deradune.sql": "blocked_laughingws_deradune_residual_rows",
            "Map/Open World/Olyssia/Ellevar.sql": "blocked_laughingws_ellevar_residual_rows",
            "Map/Open World/Olyssia/Illium.sql": "rejected_laughingws_illium_orphan_vendor_stubs",
        }

        for relative_path, recommendation in expectations.items():
            with self.subTest(relative_path=relative_path):
                recommendations = self._recommendations(relative_path)

                self.assertIn(recommendation, recommendations)
                self.assertNotIn("extract_additive_world_overlay", recommendations)

    def test_small_world_partial_seed_sources_keep_residual_boundaries(self) -> None:
        expectations = {
            "Map/Open World/Alizar/Everstargrove.sql": "blocked_laughingws_everstar_grove_residual_rows",
            "Map/Open World/Alizar/Northern Wilds.sql": "blocked_laughingws_northern_wilds_residual_rows",
            "Map/Open World/Olyssia/Auroria.sql": "blocked_laughingws_auroria_residual_rows",
            "Map/Open World/Olyssia/Crimson Isle.sql": "blocked_laughingws_crimson_isle_residual_rows",
            "Map/Open World/Olyssia/Levian Bay.sql": "blocked_laughingws_levian_bay_residual_rows",
            "Map/Open World/Olyssia/Wilderrun.sql": "blocked_laughingws_wilderrun_dorian_rows",
        }

        for relative_path, recommendation in expectations.items():
            with self.subTest(relative_path=relative_path):
                recommendations = self._recommendations(relative_path)

                self.assertIn("covered_by_laughingws_small_world_wip_seed", recommendations)
                self.assertIn(recommendation, recommendations)
                self.assertNotIn("extract_additive_world_overlay", recommendations)

    def test_northern_wilds_duplicate_vendor_rows_stay_rejected(self) -> None:
        recommendations = self._recommendations("Map/Open World/Alizar/Northern Wilds.sql")

        self.assertIn("rejected_laughingws_northern_wilds_vendor_duplicate_rows", recommendations)
        self.assertNotIn("extract_additive_world_overlay", recommendations)

    def _recommendations(self, relative_path: str) -> list[str]:
        key = relative_path.replace("\\", "/").lower()
        info = self.files_by_path[key]
        return audit.build_recommendations(info)


def _find_repo_root() -> Path:
    directory = Path(__file__).resolve()
    for parent in [directory, *directory.parents]:
        if (parent / "Source" / "NexusForever.sln").exists():
            return parent

    raise RuntimeError("Unable to locate NexusForever repository root.")


if __name__ == "__main__":
    unittest.main()
