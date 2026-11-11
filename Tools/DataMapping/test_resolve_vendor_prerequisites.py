import unittest

import resolve_vendor_prerequisites as resolver


def prereq(row_id, type0, object0, value0, comparison=4, type1=0, type2=0):
    return {"id": row_id, "type0": type0, "type1": type1, "type2": type2, "comparison0": comparison,
            "object0": object0, "value0": value0}


FACTIONS = {
    "the algoroc accord".replace("the ", ""): [900],
    "wilderrun expedition": [907],
    "wilderrun campaign": [920],
}
ROWS = [
    prereq(40, 1, 0, 50),
    prereq(30, 1, 0, 50),  # lowest id wins
    prereq(31, 1, 0, 50, comparison=1),  # equality is not a >= gate
    prereq(60, 5, 900, 8000),
    prereq(61, 5, 900, 8000, type1=1),  # multi-clause rows are not equivalent
]


class ParseTests(unittest.TestCase):
    def test_level(self) -> None:
        self.assertEqual(resolver.ParsedPrerequisite("level", 50), resolver.parse_prerequisite_text("Requires Level 50"))
        self.assertEqual(15, resolver.parse_prerequisite_text("You must be level 15 to purchase a ground mount").value)

    def test_reputation(self) -> None:
        parsed = resolver.parse_prerequisite_text("Requires 8000 reputation with The Algoroc Accord")
        self.assertEqual(("reputation", 8000, "The Algoroc Accord"), (parsed.kind, parsed.value, parsed.faction_text))

    def test_compound_text_is_not_parsed(self) -> None:
        for text in ["You must be an Expert Tailor and level 50 to purchase this pattern", "Must be a Medic and level 50",
                     "Requires an Arena Rating of 1500 or higher", ""]:
            with self.subTest(text=text):
                self.assertEqual("unparsed", resolver.parse_prerequisite_text(text).kind)


CLASSES = {"medic": 4, "engineer": 2}
SIDES = {907: "exile", 920: "dominion"}
FACTIONS2 = dict(FACTIONS, **{"wilderrun expedition": [907], "wilderrun campaign": [920]})
ROWS2 = ROWS + [prereq(70, 3, 0, 4, comparison=1), prereq(71, 5, 907, 8000), prereq(72, 5, 920, 8000)]


class ClassAndPairTests(unittest.TestCase):
    def test_class_resolves_to_the_equality_row(self) -> None:
        result = resolver.resolve_text("Must be a Medic", ROWS2, FACTIONS2, CLASSES, SIDES)
        self.assertEqual((70, "semantic_equivalent_row"), (result["prerequisite_id"], result["resolution"]))

    def test_unknown_class_stays_unresolved(self) -> None:
        result = resolver.resolve_text("Must be a Esper", ROWS2, FACTIONS2, CLASSES, SIDES)
        self.assertEqual("unresolved", result["resolution"])

    def test_side_specific_pair_resolves_one_row_per_side(self) -> None:
        result = resolver.resolve_text(
            "Requires 8000 reputation with Wilderrun Expedition or The Wilderrun Campaign", ROWS2, FACTIONS2, CLASSES, SIDES)
        self.assertEqual("side_specific_pair", result["resolution"])
        self.assertEqual((71, 72, 0), (result["prerequisite_id_exile"], result["prerequisite_id_dominion"], result["prerequisite_id"]))

    def test_pair_without_side_data_stays_unresolved(self) -> None:
        result = resolver.resolve_text(
            "Requires 8000 reputation with Wilderrun Expedition or The Wilderrun Campaign", ROWS2, FACTIONS2, CLASSES, {})
        self.assertEqual("unresolved", result["resolution"])


class ResolveTests(unittest.TestCase):
    def test_level_resolves_to_the_lowest_greater_or_equal_single_clause_row(self) -> None:
        result = resolver.resolve_text("Requires Level 50", ROWS, FACTIONS)
        self.assertEqual(30, result["prerequisite_id"])
        self.assertEqual("semantic_equivalent_row", result["resolution"])

    def test_reputation_resolves_by_faction_and_value(self) -> None:
        result = resolver.resolve_text("Requires 8000 reputation with The Algoroc Accord", ROWS, FACTIONS)
        self.assertEqual(60, result["prerequisite_id"])
        self.assertEqual(900, result["faction2_id"])

    def test_side_specific_faction_pair_is_left_unresolved(self) -> None:
        result = resolver.resolve_text(
            "Requires 8000 reputation with Wilderrun Expedition or The Wilderrun Campaign", ROWS, FACTIONS)
        self.assertEqual(0, result["prerequisite_id"])
        self.assertEqual("unresolved", result["resolution"])
        self.assertIn("side-specific", result["detail"])

    def test_missing_equivalent_row_is_unresolved_not_guessed(self) -> None:
        result = resolver.resolve_text("Requires 1 reputation with The Algoroc Accord", ROWS, FACTIONS)
        self.assertEqual(0, result["prerequisite_id"])
        self.assertEqual("unresolved", result["resolution"])

    def test_output_key_is_the_raw_text(self) -> None:
        raw = "Requires Level 50"
        self.assertEqual(raw, resolver.resolve_text(raw, ROWS, FACTIONS)["prerequisite_text"])


if __name__ == "__main__":
    unittest.main()
