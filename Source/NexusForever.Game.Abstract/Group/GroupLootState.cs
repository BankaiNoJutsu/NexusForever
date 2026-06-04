using NexusForever.Game.Static.Group;
using NexusForever.Game.Static.Setting;

namespace NexusForever.Game.Abstract.Group
{
    public class GroupLootState
    {
        public ulong GroupId { get; init; }
        public LootRule NormalRule { get; init; }
        public LootRule ThresholdRule { get; init; }
        public LootThreshold ThresholdQuality { get; init; }
        public HarvestLootRule HarvestRule { get; init; }
        public WorldDifficulty InstanceDifficulty { get; init; } = WorldDifficulty.Normal;
        public GroupFlags Flags { get; init; }
        public required Identity Leader { get; init; }
        public IReadOnlyList<GroupLootMember> Members { get; init; } = [];

        public GroupLootMember GetMember(Identity identity)
        {
            return Members.FirstOrDefault(m => m.Identity == identity);
        }

        public bool HasMember(Identity identity)
        {
            return GetMember(identity) != null;
        }

        public LootRule GetLootRule(uint itemQualityId)
        {
            return itemQualityId >= (uint)ThresholdQuality ? ThresholdRule : NormalRule;
        }

        public GroupLootState WithoutMember(Identity identity)
        {
            return new GroupLootState
            {
                GroupId          = GroupId,
                NormalRule       = NormalRule,
                ThresholdRule    = ThresholdRule,
                ThresholdQuality = ThresholdQuality,
                HarvestRule      = HarvestRule,
                InstanceDifficulty = InstanceDifficulty,
                Flags            = Flags,
                Leader           = Leader,
                Members          = Members.Where(m => m.Identity != identity).ToList()
            };
        }
    }
}
