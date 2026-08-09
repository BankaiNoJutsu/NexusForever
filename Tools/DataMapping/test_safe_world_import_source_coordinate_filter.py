import unittest
from pathlib import Path


REPO_ROOT = Path(__file__).resolve().parents[2]
APPLY_SQL_PATH = (
    REPO_ROOT
    / "Tools"
    / "DataMapping"
    / "sql"
    / "apply_safe_world_imports_from_staging.sql"
)
EXPORTER_PATH = REPO_ROOT / "Tools" / "DataMapping" / "export_runtime_world_seed.py"
RUNTIME_SEED_PATH = (
    REPO_ROOT / "Tools" / "DataMapping" / "sql" / "runtime_world_seed.sql"
)
VERIFIER_PATH = (
    REPO_ROOT / "Tools" / "DataMapping" / "sql" / "verify_safe_world_imports.sql"
)


class SafeWorldImportSourceCoordinateFilterTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.sql = APPLY_SQL_PATH.read_text(encoding="utf-8")
        cls.exporter = EXPORTER_PATH.read_text(encoding="utf-8")
        cls.runtime_seed = RUNTIME_SEED_PATH.read_text(encoding="utf-8")
        cls.verifier = VERIFIER_PATH.read_text(encoding="utf-8")

    def test_source_coordinate_filter_is_disabled_by_default(self):
        self.assertIn(
            "SET @nf_safe_import_entity_spawn_source_coordinate_id := "
            "IFNULL(@nf_safe_import_entity_spawn_source_coordinate_id, 0);",
            self.sql,
        )

    def test_staged_candidate_path_requires_selected_source_coordinate(self):
        self.assertIn(
            "AND (@nf_safe_import_entity_spawn_source_coordinate_id = 0 "
            "OR c.source_coordinate_id = "
            "@nf_safe_import_entity_spawn_source_coordinate_id)",
            self.sql,
        )

    def test_direct_jabbithole_path_requires_selected_source_coordinate(self):
        self.assertIn(
            "AND (@nf_safe_import_entity_spawn_source_coordinate_id = 0 "
            "OR co.id = @nf_safe_import_entity_spawn_source_coordinate_id)",
            self.sql,
        )

    def test_exporter_owns_exact_ruins_portal_row(self):
        self.assertIn("    1_000_009_115,", self.exporter)
        self.assertIn(
            "(1000009115,14,40771,1336,1661,-15,-737,1007,"
            "0,0,0,26914,0,219,219,0,0,0,0);",
            self.exporter,
        )

    def test_exporter_owns_exact_ultimate_protogames_portal_row(self):
        self.assertIn("    1_006_132_630,", self.exporter)
        self.assertIn(
            "(1006132630,14,67513,2980,4376,-12512,-788,-6877,"
            "0,0,0,29666,0,219,219,0,0,0,0);",
            self.exporter,
        )
        self.assertIn("(1006132630,21,0);", self.exporter)
        self.assertIn(
            '("entity_stats", f"{primary_entity_filter} AND id NOT IN ({reviewed_entity_ids})")',
            self.exporter,
        )

    def test_exporter_owns_exact_ultimate_protogames_tea_time_pair(self):
        self.assertIn("    1_006_289_549,", self.exporter)
        self.assertIn("    1_006_289_665,", self.exporter)
        self.assertIn(
            "(1006289549,0,69876,2980,4337,-28714,-25,-2788,"
            "0,0,0,21631,0,219,219,0,0,0,0),",
            self.exporter,
        )
        self.assertIn(
            "(1006289665,10,69883,2980,4337,-28715,-25,-2788,"
            "0,0,0,28132,0,219,219,0,0,0,0);",
            self.exporter,
        )
        for stat_row in [
            "(1006289549,0,101),",
            "(1006289549,10,1),",
            "(1006289549,20,0),",
            "(1006289549,21,0),",
            "(1006289665,21,0);",
        ]:
            with self.subTest(stat_row=stat_row):
                self.assertIn(stat_row, self.exporter)

    def test_exporter_owns_exact_stormtalon_exit_portal_row(self):
        self.assertIn("    1_000_003_084,", self.exporter)
        self.assertIn(
            "(1000003084,14,40770,382,271,10,-9,237,"
            "0,0,0,26914,0,219,219,0,0,0,0);",
            self.exporter,
        )
        self.assertIn("(1000003084,21,0);", self.exporter)

    def test_exporter_owns_current_sanctuary_exit_portal_row(self):
        self.assertIn("    1_005_221_261,", self.exporter)
        self.assertIn(
            "(1005221261,14,41148,1271,2260,4244,-774,-3252,"
            "0,0,0,26914,0,219,219,0,0,0,0);",
            self.exporter,
        )
        self.assertIn("(1005221261,21,0);", self.exporter)
        self.assertNotIn("(1000054254,", self.exporter)

    def test_exporter_owns_current_fragment_zero_transport_portal_row(self):
        self.assertIn("    1_006_129_819,", self.exporter)
        self.assertIn(
            "(1006129819,14,69013,3180,4619,9861,-760,-5810,"
            "0,0,0,22991,0,219,219,0,0,0,0);",
            self.exporter,
        )
        self.assertIn("(1006129819,21,0);", self.exporter)

    def test_exporter_owns_both_current_skullcano_exit_portal_rows(self):
        self.assertIn("    1_000_285_998,", self.exporter)
        self.assertIn("    1_000_415_265,", self.exporter)
        self.assertIn(
            "(1000285998,14,32419,1263,1220,674,-1002,-88,"
            "0,0,0,22402,0,219,219,0,0,0,0),",
            self.exporter,
        )
        self.assertIn(
            "(1000415265,14,32419,1263,1220,-699,-692,-413,"
            "0,0,0,22402,0,219,219,0,0,0,0);",
            self.exporter,
        )
        self.assertIn("(1000285998,21,0),", self.exporter)
        self.assertIn("(1000415265,21,0);", self.exporter)

    def test_exporter_owns_current_gauntlet_exit_but_not_ambiguous_return_portal(self):
        self.assertIn("    1_000_107_802,", self.exporter)
        self.assertIn(
            "(1000107802,14,48933,2183,2613,-1003,3,800,"
            "0,0,0,23861,0,219,219,0,0,0,0);",
            self.exporter,
        )
        self.assertIn("(1000107802,21,0);", self.exporter)
        self.assertNotIn("(1000964413,", self.exporter)
        self.assertNotIn("(1000016830,", self.exporter)

    def test_runtime_seed_contains_only_reviewed_portal_candidates(self):
        self.assertIn(
            "(1000009115,14,40771,1336,1661,-15,-737,1007,"
            "0,0,0,26914,0,219,219,0,0,0,0);",
            self.runtime_seed,
        )
        self.assertIn(
            "(1006132630,14,67513,2980,4376,-12512,-788,-6877,"
            "0,0,0,29666,0,219,219,0,0,0,0);",
            self.runtime_seed,
        )
        self.assertIn("(1006132630,21,0);", self.runtime_seed)
        self.assertIn(
            "(1006289549,0,69876,2980,4337,-28714,-25,-2788,"
            "0,0,0,21631,0,219,219,0,0,0,0),",
            self.runtime_seed,
        )
        self.assertIn(
            "(1006289665,10,69883,2980,4337,-28715,-25,-2788,"
            "0,0,0,28132,0,219,219,0,0,0,0);",
            self.runtime_seed,
        )
        self.assertIn("(1006289549,0,101),", self.runtime_seed)
        self.assertIn("(1006289549,10,1),", self.runtime_seed)
        self.assertIn("(1006289549,20,0),", self.runtime_seed)
        self.assertIn("(1006289549,21,0),", self.runtime_seed)
        self.assertIn("(1006289665,21,0);", self.runtime_seed)
        self.assertIn(
            "(1000003084,14,40770,382,271,10,-9,237,"
            "0,0,0,26914,0,219,219,0,0,0,0);",
            self.runtime_seed,
        )
        self.assertIn("(1000003084,21,0);", self.runtime_seed)
        self.assertIn(
            "(1005221261,14,41148,1271,2260,4244,-774,-3252,"
            "0,0,0,26914,0,219,219,0,0,0,0);",
            self.runtime_seed,
        )
        self.assertIn("(1005221261,21,0);", self.runtime_seed)
        self.assertNotIn("(1000054254,", self.runtime_seed)
        self.assertIn(
            "(1006129819,14,69013,3180,4619,9861,-760,-5810,"
            "0,0,0,22991,0,219,219,0,0,0,0);",
            self.runtime_seed,
        )
        self.assertIn("(1006129819,21,0);", self.runtime_seed)
        self.assertIn(
            "(1000285998,14,32419,1263,1220,674,-1002,-88,"
            "0,0,0,22402,0,219,219,0,0,0,0),",
            self.runtime_seed,
        )
        self.assertIn(
            "(1000415265,14,32419,1263,1220,-699,-692,-413,"
            "0,0,0,22402,0,219,219,0,0,0,0);",
            self.runtime_seed,
        )
        self.assertIn("(1000285998,21,0),", self.runtime_seed)
        self.assertIn("(1000415265,21,0);", self.runtime_seed)
        self.assertIn(
            "(1000107802,14,48933,2183,2613,-1003,3,800,"
            "0,0,0,23861,0,219,219,0,0,0,0);",
            self.runtime_seed,
        )
        self.assertIn("(1000107802,21,0);", self.runtime_seed)
        self.assertNotIn("(1000964413,", self.runtime_seed)
        self.assertNotIn("(1000016830,", self.runtime_seed)
        self.assertNotIn("(1000008833,", self.runtime_seed)
        self.assertNotIn("(1000011694,", self.runtime_seed)
        self.assertNotIn("(1000011894,", self.runtime_seed)
        self.assertNotIn("(1000011992,", self.runtime_seed)
        self.assertNotIn("(1006132631,", self.runtime_seed)

    def test_runtime_verifier_checks_exact_portal_and_rejects_extras(self):
        self.assertIn(
            "'expected_ruins_kel_voreth_reviewed_exit_portal_rows_1_mismatch'",
            self.verifier,
        )
        self.assertIn(
            "'unexpected_ruins_kel_voreth_exit_portal_rows'",
            self.verifier,
        )
        self.assertIn("id = 1000009115", self.verifier)
        self.assertIn("type = 14", self.verifier)
        self.assertIn("creature = 40771", self.verifier)

    def test_runtime_verifier_checks_exact_ultimate_protogames_portal(self):
        self.assertIn(
            "'expected_ultimate_protogames_reviewed_exit_portal_rows_1_mismatch'",
            self.verifier,
        )
        self.assertIn(
            "'unexpected_ultimate_protogames_exit_portal_rows'",
            self.verifier,
        )
        self.assertIn("id = 1006132630", self.verifier)
        self.assertIn("creature = 67513", self.verifier)
        self.assertIn(
            "'expected_ultimate_protogames_reviewed_exit_portal_stat_rows_1_mismatch'",
            self.verifier,
        )

    def test_runtime_verifier_checks_exact_ultimate_protogames_tea_time_pair(self):
        self.assertIn(
            "'expected_ultimate_protogames_tea_time_rows_2_mismatch'",
            self.verifier,
        )
        self.assertIn(
            "'unexpected_ultimate_protogames_tea_time_rows'",
            self.verifier,
        )
        self.assertIn("id = 1006289549", self.verifier)
        self.assertIn("creature = 69876", self.verifier)
        self.assertIn("id = 1006289665", self.verifier)
        self.assertIn("creature = 69883", self.verifier)
        self.assertIn(
            "'expected_ultimate_protogames_tea_time_stat_rows_5_mismatch'",
            self.verifier,
        )

    def test_runtime_verifier_checks_exact_stormtalon_exit_portal(self):
        self.assertIn(
            "'expected_stormtalon_reviewed_exit_portal_rows_1_mismatch'",
            self.verifier,
        )

    def test_runtime_verifier_checks_current_sanctuary_exit_portal(self):
        self.assertIn(
            "'expected_sanctuary_reviewed_exit_portal_rows_1_mismatch'",
            self.verifier,
        )
        self.assertIn(
            "'unexpected_sanctuary_exit_portal_rows'",
            self.verifier,
        )
        self.assertIn("id = 1005221261", self.verifier)
        self.assertIn("creature = 41148", self.verifier)
        self.assertIn(
            "'expected_sanctuary_reviewed_exit_portal_stat_rows_1_mismatch'",
            self.verifier,
        )
        self.assertIn(
            "'unexpected_sanctuary_older_exit_portal_rows'",
            self.verifier,
        )
        self.assertIn("id = 1000054254", self.verifier)

    def test_runtime_verifier_checks_current_fragment_zero_transport_portal(self):
        self.assertIn(
            "'expected_fragment_zero_reviewed_transport_portal_rows_1_mismatch'",
            self.verifier,
        )
        self.assertIn(
            "'unexpected_fragment_zero_transport_portal_rows'",
            self.verifier,
        )
        self.assertIn("id = 1006129819", self.verifier)
        self.assertIn("creature = 69013", self.verifier)
        self.assertIn(
            "'expected_fragment_zero_reviewed_transport_portal_stat_rows_1_mismatch'",
            self.verifier,
        )

    def test_runtime_verifier_checks_both_current_skullcano_exit_portals(self):
        self.assertIn(
            "'expected_skullcano_reviewed_exit_portal_rows_2_mismatch'",
            self.verifier,
        )
        self.assertIn(
            "'unexpected_skullcano_exit_portal_rows'",
            self.verifier,
        )
        self.assertIn("id = 1000285998", self.verifier)
        self.assertIn("id = 1000415265", self.verifier)
        self.assertIn("creature = 32419", self.verifier)
        self.assertIn(
            "'expected_skullcano_reviewed_exit_portal_stat_rows_2_mismatch'",
            self.verifier,
        )

    def test_runtime_verifier_checks_gauntlet_exit_and_rejects_ambiguous_return(self):
        self.assertIn(
            "'expected_gauntlet_reviewed_exit_portal_rows_1_mismatch'",
            self.verifier,
        )
        self.assertIn(
            "'unexpected_gauntlet_exit_portal_rows'",
            self.verifier,
        )
        self.assertIn("id = 1000107802", self.verifier)
        self.assertIn("creature = 48933", self.verifier)
        self.assertIn(
            "'expected_gauntlet_reviewed_exit_portal_stat_rows_1_mismatch'",
            self.verifier,
        )
        self.assertIn(
            "'unexpected_gauntlet_ambiguous_return_portal_rows'",
            self.verifier,
        )
        self.assertIn("id = 1000964413", self.verifier)

    def test_runtime_verifier_rejects_ambiguous_space_madness_exit_portal(self):
        self.assertIn(
            "'unexpected_space_madness_ambiguous_exit_portal_rows'",
            self.verifier,
        )
        self.assertIn("id = 1000016830", self.verifier)
        self.assertIn("creature IN (46014, 46015)", self.verifier)
        self.assertIn(
            "'unexpected_stormtalon_exit_portal_rows'",
            self.verifier,
        )
        self.assertIn("id = 1000003084", self.verifier)
        self.assertIn("creature = 40770", self.verifier)
        self.assertIn(
            "'expected_stormtalon_reviewed_exit_portal_stat_rows_1_mismatch'",
            self.verifier,
        )


if __name__ == "__main__":
    unittest.main()
