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

        public static bool TryBuildChecklistBit(uint bitIndex, out uint bit)
        {
            if (bitIndex >= 32u)
            {
                bit = 0u;
                return false;
            }

            bit = 1u << (int)bitIndex;
            return true;
        }
    }
}
