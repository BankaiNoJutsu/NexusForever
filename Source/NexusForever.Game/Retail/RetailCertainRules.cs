using NexusForever.Game.Static.Matching;

namespace NexusForever.Game.Retail
{
    /// <summary>
    /// Build 16042 F2P-era constants from <c>RETAIL_FEATURE_EVIDENCE.md</c> (excludes Sep 2018 sunset buffs).
    /// </summary>
    public static class RetailCertainRules
    {
        public const byte HousingUnlockLevel = 14;

        public const uint CommunityCreateCostGameFormulaId = 1159u;
        public const uint GuildCreateCostGameFormulaId     = 764u;

        public const int PveDeserterBaseSeconds  = 15 * 60;
        public const int PvpDeserterBaseSeconds  = 10 * 60;

        /// <summary>Matching queue flag for dungeon "My Realm Only" (winter beta S8).</summary>
        public const MatchingQueueFlags DungeonRealmOnlyQueueFlag = MatchingQueueFlags.RealmOnly;

        public const int VoteKickCooldownInInstanceMs      = 10 * 60 * 1000;
        public const int VoteKickCooldownOfflineOrAbsentMs = 2 * 60 * 1000;

        public const int WarplotSurrenderMinElapsedMs = 10 * 60 * 1000;
        public const double WarplotSurrenderVoteRatio   = 0.60;

        public const int WarplotOnlineMembersRequiredToQueue = 10;
        public const int WarplotQueuedMembersRequiredToPop   = 10;

        public const int MaxConcurrentActiveChallenges = 2;

        /// <summary>Poor-quality items and below roll Need/Greed to the group (Strain 1.0.9).</summary>
        public const uint TrashItemMaxQualityId = 1u;

        /// <summary>Retail stuck UI cooldowns (SupportStuckAction).</summary>
        public const int StuckRecallBindCooldownMs  = 30 * 60 * 1000;
        public const int StuckRecallHouseCooldownMs = 20 * 60 * 1000;
        public const int StuckRecallDeathCooldownMs = 10 * 60 * 1000;

        /// <summary>Neighbor share percents by client harvest-split index (0-based, eight options).</summary>
        public static readonly int[] HousingNeighborHarvestSharePercents = [0, 10, 25, 33, 50, 66, 75, 100];
    }
}
