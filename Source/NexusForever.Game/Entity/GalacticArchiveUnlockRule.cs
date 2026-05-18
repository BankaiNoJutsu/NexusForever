using System;
using System.Collections.Generic;
using System.Linq;
using NexusForever.GameTable.Model;

namespace NexusForever.Game.Entity
{
    public static class GalacticArchiveUnlockRule
    {
        public static bool IsSatisfied(
            ArchiveEntryUnlockRuleEntry rule,
            Func<uint, bool> isAchievementComplete,
            Func<uint, bool> isQuestComplete,
            Func<uint, bool> isPathMissionComplete)
        {
            ArgumentNullException.ThrowIfNull(rule);
            ArgumentNullException.ThrowIfNull(isAchievementComplete);
            ArgumentNullException.ThrowIfNull(isQuestComplete);
            ArgumentNullException.ThrowIfNull(isPathMissionComplete);

            return rule.ArchiveEntryUnlockRuleEnum switch
            {
                0u => MatchObjects(rule, isAchievementComplete),
                1u => MatchObjects(rule, isPathMissionComplete),
                2u => MatchObjects(rule, isQuestComplete),
                _  => false
            };
        }

        private static bool MatchObjects(ArchiveEntryUnlockRuleEntry rule, Func<uint, bool> isComplete)
        {
            uint[] objects = [rule.Object00, rule.Object01, rule.Object02, rule.Object03, rule.Object04, rule.Object05];
            IEnumerable<uint> activeObjects = objects.Where(o => o != 0u);
            if (!activeObjects.Any())
                return false;

            return (rule.ArchiveEntryUnlockRuleFlags & 1u) != 0u
                ? activeObjects.All(isComplete)
                : activeObjects.Any(isComplete);
        }
    }
}
