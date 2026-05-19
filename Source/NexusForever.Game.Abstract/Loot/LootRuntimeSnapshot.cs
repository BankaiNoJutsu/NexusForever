using NexusForever.Game.Static.Entity;
using NexusForever.Game.Static.Loot;

namespace NexusForever.Game.Abstract.Loot
{
    public sealed class LootRuntimeSnapshot
    {
        public uint OwnerUnitId { get; init; }
        public uint ParentUnitIdRuntimeValue { get; init; }
        public string ParentUnitIdNotes { get; init; }
        public LootEntityType LootEntityType { get; init; }
        public LooterType LooterType { get; init; }
        public bool Explosion { get; init; }
        public bool HasExpired { get; init; }
        public ulong ViewerCharacterId { get; init; }
        public bool ViewerIsTrackedLooter { get; init; }
        public List<ulong> TrackedLooterCharacterIds { get; init; } = [];
        public List<LootRuntimeSnapshotItem> Items { get; init; } = [];
    }

    public sealed class LootRuntimeSnapshotItem
    {
        public uint LootUnitId { get; init; }
        public LootItemType Type { get; init; }
        public uint ItemId { get; init; }
        public uint Amount { get; init; }
        public bool Delivered { get; init; }
        public bool ViewerCanLoot { get; init; }
        public bool RequiresRoll { get; init; }
        public bool OnlyMasterLootable { get; init; }
        public uint RollTime { get; init; }
        public uint ItemQuality2Id { get; init; }
        public ulong WinnerCharacterId { get; init; }
        public uint WinnerGuid { get; init; }
        public List<ulong> EligibleCharacterIds { get; init; } = [];
        public List<ulong> MasterCharacterIds { get; init; } = [];
        public List<ulong> MasterCandidateCharacterIds { get; init; } = [];
        public uint MasterListCount { get; init; }
    }
}
