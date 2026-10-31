using System.Text.Json;
using NexusForever.Database.Character;
using NexusForever.Database.Character.Model;
using NexusForever.Game.Static.Entity;

namespace NexusForever.Game.Abstract.Entity
{
    public enum InventoryRewardReservationState
    {
        Prepared, Staged, Uncertain, ReadyToRetry, Committed, Published, Canceled, Quarantined
    }

    public sealed record InventoryRewardItemSnapshot(ulong Guid, ulong? CharacterId, uint ItemId,
        InventoryLocation Location, uint BagIndex, uint StackCount, uint Charges, float Durability,
        uint ExpirationTimeLeft, bool Soulbound, string MicrochipIds, string RuneSlots)
    {
        public static InventoryRewardItemSnapshot FromModel(ItemModel row) => row == null ? null : new(
            row.Id, row.OwnerId, row.ItemId, (InventoryLocation)row.Location, row.BagIndex, row.StackCount,
            row.Charges, row.Durability, row.ExpirationTimeLeft, row.Soulbound, row.MicrochipIds, row.RuneSlots);

        public ItemModel ToModel() => new()
        {
            Id = Guid, OwnerId = CharacterId, ItemId = ItemId, Location = (ushort)Location, BagIndex = BagIndex,
            StackCount = StackCount, Charges = Charges, Durability = Durability, ExpirationTimeLeft = ExpirationTimeLeft,
            Soulbound = Soulbound, MicrochipIds = MicrochipIds, RuneSlots = RuneSlots
        };
    }

    public sealed record InventoryRewardDeliveryLine(bool ExistingLiveItem, uint AwardedCount,
        InventoryRewardItemSnapshot Before, InventoryRewardItemSnapshot After);

    public sealed class InventoryRewardDeliveryPlan
    {
        public uint Version => 1;
        public ulong CharacterId { get; }
        public uint ItemId { get; }
        public uint Count { get; }
        public IReadOnlyList<InventoryRewardDeliveryLine> Lines { get; }

        public InventoryRewardDeliveryPlan(ulong characterId, uint itemId, uint count, IEnumerable<InventoryRewardDeliveryLine> lines)
        {
            ArgumentNullException.ThrowIfNull(lines);
            CharacterId = characterId; ItemId = itemId; Count = count;
            Lines = Array.AsReadOnly(lines.ToArray());
        }

        public string ToJson() => JsonSerializer.Serialize(this);
    }

    public interface IInventoryRewardReservation : IDisposable
    {
        InventoryRewardReservationState State { get; }
        InventoryRewardDeliveryPlan Plan { get; }
        ulong CharacterId => Plan?.CharacterId ?? 0;
        uint ItemId => Plan?.ItemId ?? 0;
        uint Count => Plan?.Count ?? 0;
        InventoryRewardDeliveryPlan Stage(CharacterContext context, CharacterChallengeRewardClaimModel receipt);
        InventoryRewardDeliveryPlan StageContract(CharacterContext context, CharacterContractRewardClaimModel receipt)
            => throw new NotSupportedException("This reservation does not support Contract receipt enrollment.");
        InventoryRewardReservationState ResolveOutcome(CharacterContext freshSerializableContext);
        bool PublishCommitted();
        bool Cancel();
        void InvalidateOwner() { }
    }
}
