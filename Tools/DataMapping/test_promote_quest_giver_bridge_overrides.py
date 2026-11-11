import unittest

from promote_quest_giver_bridge_overrides import expand_target_group, resolve_by_quest_giver


class QuestGiverResolutionTests(unittest.TestCase):
    def test_single_candidate_listing_the_quest_is_chosen(self) -> None:
        result = resolve_by_quest_giver([1, 2, 3], {2: {100, 101}, 3: {999}}, {100, 555})
        self.assertEqual((2, {100}), result)

    def test_two_candidates_listing_the_quest_stay_ambiguous(self) -> None:
        self.assertIsNone(resolve_by_quest_giver([1, 2], {1: {100}, 2: {100}}, {100}))

    def test_candidates_matching_different_quests_stay_ambiguous(self) -> None:
        self.assertIsNone(resolve_by_quest_giver([1, 2], {1: {100}, 2: {101}}, {100, 101}))

    def test_no_candidate_listing_any_quest_is_left_unresolved(self) -> None:
        self.assertIsNone(resolve_by_quest_giver([1, 2], {1: {5}, 2: {6}}, {100}))

    def test_creature_without_quests_is_never_resolved(self) -> None:
        self.assertIsNone(resolve_by_quest_giver([1, 2], {1: {100}}, set()))


class TargetGroupTests(unittest.TestCase):
    def test_nested_groups_expand_to_creature_lists(self) -> None:
        groups = {1: (11, [2, 3]), 2: (1, [100, 101]), 3: (1, [102]), 4: (9, [999])}
        self.assertEqual({100, 101, 102}, expand_target_group(groups, 1))
        self.assertEqual(set(), expand_target_group(groups, 4))  # unknown group type is not guessed

    def test_cycles_terminate(self) -> None:
        self.assertEqual({7}, expand_target_group({1: (11, [2, 1]), 2: (1, [7])}, 1))


if __name__ == "__main__":
    unittest.main()
