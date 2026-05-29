using NexusForever.Game.Abstract.Achievement;
using NexusForever.Game.Static.Achievement;

namespace NexusForever.Game.Achievement
{
    internal static class AchievementProgressRules
    {
        public static bool UsesChecklistValueProgress(IAchievementInfo info)
        {
            return info.ChecklistEntries.Count != 0
                && (AchievementType)info.Entry.AchievementTypeId == AchievementType.QuestCompleteChecklistCount;
        }

        public static uint GetRequiredProgress(uint requiredProgress)
        {
            return requiredProgress == 0u ? 1u : requiredProgress;
        }
    }
}
