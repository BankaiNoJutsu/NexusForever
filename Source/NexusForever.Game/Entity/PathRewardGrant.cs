using NexusForever.GameTable.Model;
using Path = NexusForever.Game.Static.PlayerPath.Path;

namespace NexusForever.Game.Entity
{
    public static class PathRewardGrant
    {
        public const uint MaxPathLevel = 30u;

        public static uint GetLevelRewardObjectId(Path path, uint level)
        {
            uint baseRewardObjectId = (uint)path * MaxPathLevel + 6u;
            if (level <= 1u)
                return baseRewardObjectId;

            uint offset = Math.Min(level - 2u, MaxPathLevel - 1u);
            return baseRewardObjectId + 1u + offset;
        }

        public static bool IsGrantableLevelReward(PathRewardEntry entry)
        {
            if (entry == null)
                throw new ArgumentNullException(nameof(entry));

            if (entry.PathRewardFlags > 0)
                return false;

            if (entry.PathRewardTypeEnum != 0)
                return false;

            return entry.Item2Id > 0
                || entry.Spell4Id > 0
                || entry.CharacterTitleId > 0
                || entry.PathScientistScanBotProfileId > 0;
        }
    }
}
