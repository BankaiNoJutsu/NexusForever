using NexusForever.GameTable;
using NexusForever.GameTable.Model;

namespace NexusForever.Game.Account.Reward
{
    public readonly struct RewardRotationContentContextSources
    {
        public GameTable<RewardRotationContentEntry> RewardRotationContent { get; init; }
        public GameTable<RewardRotationItemEntry> RewardRotationItem { get; init; }
        public GameTable<RewardRotationEssenceEntry> RewardRotationEssence { get; init; }
        public GameTable<RewardRotationModifierEntry> RewardRotationModifier { get; init; }
        public GameTable<WorldZoneEntry> WorldZone { get; init; }
        public GameTable<WorldEntry> World { get; init; }
        public GameTable<PublicEventEntry> PublicEvent { get; init; }
        public GameTable<MatchTypeRewardRotationContentEntry> MatchTypeRewardRotationContent { get; init; }

        public static RewardRotationContentContextSources From(IGameTableManager gameTables)
        {
            if (gameTables == null)
                throw new ArgumentNullException(nameof(gameTables));

            return new RewardRotationContentContextSources
            {
                RewardRotationContent = gameTables.RewardRotationContent,
                RewardRotationItem = gameTables.RewardRotationItem,
                RewardRotationEssence = gameTables.RewardRotationEssence,
                RewardRotationModifier = gameTables.RewardRotationModifier,
                WorldZone = gameTables.WorldZone,
                World = gameTables.World,
                PublicEvent = gameTables.PublicEvent,
                MatchTypeRewardRotationContent = gameTables.MatchTypeRewardRotationContent
            };
        }
    }
}
