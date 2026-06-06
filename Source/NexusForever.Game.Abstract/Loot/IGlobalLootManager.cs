using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract;
using NexusForever.Game.Static.Account;
using NexusForever.Game.Static.Entity;
using NexusForever.Game.Static.Loot;
using NexusForever.GameTable.Model;
using NexusForever.Shared;

namespace NexusForever.Game.Abstract.Loot
{
    public readonly record struct GeneratedLootItem(LootItemType Type, uint StaticId, uint Count);

    public interface IGlobalLootManager : IUpdate
    {
        void Initialise();

        bool DropLoot(IPlayer looter, IWorldEntity lootedEntity);
        bool HasLoot(IItem lootedItem);
        bool DropLoot(IPlayer looter, IItem lootedItem);
        bool TryUseLootBag(IPlayer looter, IItem lootedItem, out string reason);
        bool TrySalvageItem(IPlayer looter, IItem salvagedItem, out string reason);
        bool TryDeliverHarvestLoot(IPlayer harvester, IReadOnlyList<GeneratedLootItem> items, uint ownerUnitId);

        void SendLootNotify(IPlayer looter, uint ownerUnitId);
        void SendLootNotifyForVisibleOwner(IPlayer looter, IWorldEntity owner);
        bool TryGetLootRuntimeSnapshot(IPlayer looter, uint ownerUnitId, out LootRuntimeSnapshot snapshot);

        void GiveLoot(IPlayer looter, uint ownerUnitId, uint lootUnitId);
        void GiveAllLootInRange(IPlayer looter);
        void RollLoot(IPlayer looter, uint ownerUnitId, uint lootUnitId, LootRollAction action);
        void AssignMasterLoot(IPlayer master, uint ownerUnitId, uint lootUnitId, Identity assignee);

        void GiveLoot(IPlayer looter, Item2Entry entry, uint count, uint ownerUnitId);
        void GiveLoot(IPlayer looter, VirtualItemEntry entry, uint count, uint ownerUnitId);
        void GiveLoot(IPlayer looter, AccountCurrencyType accountCurrencyType, uint count, uint ownerUnitId);
        void GiveLoot(IPlayer looter, CurrencyType currencyType, uint count, uint ownerUnitId);

        bool TryGenerateLoot(uint lootGroupId, IPlayer looter, uint rollCount, out IReadOnlyList<GeneratedLootItem> items, out string reason);
        bool CanDeliverGeneratedLoot(IPlayer looter, IEnumerable<GeneratedLootItem> items, out string reason);
        void GiveGeneratedLoot(IPlayer looter, IEnumerable<GeneratedLootItem> items, uint ownerUnitId, bool sendGrantedNotify = false, uint parentUnitId = 0u);
    }
}
