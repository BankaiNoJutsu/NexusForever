using System.Reflection;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NexusForever.Game;
using NexusForever.Game.Abstract;
using NexusForever.Game.Abstract.Achievement;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Loot;
using NexusForever.Game.Static.Account;
using NexusForever.Game.Static.Crafting;
using NexusForever.Game.Static.Entity;
using NexusForever.Game.Static.Loot;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.GameTable;
using NexusForever.GameTable.Configuration.Model;
using NexusForever.GameTable.Model;
using NexusForever.Network.Message;
using NexusForever.Network.Session;
using NexusForever.Network.World.Message.Model.Crafting;
using NexusForever.WorldServer.Network;
using NexusForever.WorldServer.Network.Message.Handler.Crafting;

namespace NexusForever.Game.Tests.Crafting;

public class CraftingLootIdCraftHandlerTests
{
    private const uint SchematicId = 5002u;
    private const uint LootGroupId = 9001u;
    private const uint OutputItemId = 7002u;
    private const uint MaterialItemId = 8001u;
    private const ushort MaterialId = 55;
    private const ulong CharacterId = 9_901_002ul;

    [Fact]
    public void SimpleCraft_WithLootIdOutput_GeneratesAndDeliversLoot()
    {
        IWorldSession session = CreateSession(
            satchelMaterialAmount: 2,
            out RecordingDispatchProxy<IInventory> inventoryProxy,
            out RecordingDispatchProxy<ISupplySatchelManager> satchelProxy,
            out RecordingDispatchProxy<IWorldSession> sessionProxy,
            out IPlayer player);
        IReadOnlyList<GeneratedLootItem> generatedLoot =
        [
            new GeneratedLootItem(LootItemType.StaticItem, OutputItemId, 1u)
        ];
        var lootManager = new StubLootManager(generatedLoot);

        ClientCraftingSimpleCraftHandler handler = CreateHandler(lootManager);

        handler.HandleMessage(session, CreateRequest(SchematicId));

        RecordingDispatchProxy<ISupplySatchelManager>.Invocation materialDebit =
            Assert.Single(satchelProxy.GetInvocations(nameof(ISupplySatchelManager.RemoveAmount)));
        Assert.Equal(MaterialId, materialDebit.Arguments[0]);
        Assert.Equal(2u, materialDebit.Arguments[1]);
        Assert.Empty(inventoryProxy.GetInvocations(nameof(IInventory.ItemCreate)));

        Assert.Equal(LootGroupId, lootManager.LastLootGroupId);
        Assert.Same(player, lootManager.LastLooter);
        Assert.Equal(1u, lootManager.LastRollCount);
        Assert.Same(player, lootManager.LastDeliverLooter);
        Assert.Equal(generatedLoot, lootManager.LastDeliveredLoot);
        Assert.Equal(player.Guid, lootManager.LastOwnerUnitId);
        Assert.True(lootManager.LastSendGrantedNotify);

        ServerCraftingFinish finish = Assert.Single(GetMessages<ServerCraftingFinish>(sessionProxy));
        Assert.True(finish.Pass);
        Assert.Equal(SchematicId, finish.TradeskillSchematic2IdCrafted);
        Assert.Equal(OutputItemId, finish.Item2IdCrafted);
        Assert.Equal(CraftingDiscovery.Success, finish.HotOrCold);
    }

    private static ClientCraftingSimpleCraftHandler CreateHandler(IGlobalLootManager lootManager)
    {
        IItemManager itemManager = RecordingDispatchProxy<IItemManager>.Create(out RecordingDispatchProxy<IItemManager> itemManagerProxy);
        itemManagerProxy.SetMethodReturn(nameof(IItemManager.GetItemInfo), (IItemInfo)null);

        return new ClientCraftingSimpleCraftHandler(
            NullLogger<ClientCraftingSimpleCraftHandler>.Instance,
            CreateGameTableManager(),
            itemManager,
            lootManager);
    }

    private static IWorldSession CreateSession(
        ushort satchelMaterialAmount,
        out RecordingDispatchProxy<IInventory> inventoryProxy,
        out RecordingDispatchProxy<ISupplySatchelManager> satchelProxy,
        out RecordingDispatchProxy<IWorldSession> sessionProxy,
        out IPlayer player)
    {
        IWorldSession session = RecordingDispatchProxy<IWorldSession>.Create(out sessionProxy);
        player = RecordingDispatchProxy<IPlayer>.Create(out RecordingDispatchProxy<IPlayer> playerProxy);
        IInventory inventory = RecordingDispatchProxy<IInventory>.Create(out inventoryProxy);
        IBag inventoryBag = RecordingDispatchProxy<IBag>.Create(out RecordingDispatchProxy<IBag> bagProxy);
        ISupplySatchelManager satchel = RecordingDispatchProxy<ISupplySatchelManager>.Create(out satchelProxy);
        ITradeskillMaterial material = RecordingDispatchProxy<ITradeskillMaterial>.Create(out RecordingDispatchProxy<ITradeskillMaterial> materialProxy);
        ICharacterAchievementManager achievementManager =
            RecordingDispatchProxy<ICharacterAchievementManager>.Create(out _);

        sessionProxy.SetProperty(nameof(IWorldSession.Player), player);
        playerProxy.SetProperty(nameof(IPlayer.Guid), 42u);
        playerProxy.SetProperty(nameof(IPlayer.CharacterId), CharacterId);
        playerProxy.SetProperty(nameof(IPlayer.Inventory), inventory);
        playerProxy.SetProperty(nameof(IPlayer.SupplySatchelManager), satchel);
        playerProxy.SetProperty(nameof(IPlayer.AchievementManager), achievementManager);
        playerProxy.SetMethodReturn(nameof(IPlayer.HasTradeskill), true);
        playerProxy.SetMethodReturn(nameof(IPlayer.AddTradeskillXp), 12u);

        inventoryProxy.SetMethodReturnFactory(nameof(IEnumerable<IBag>.GetEnumerator), () => new[] { inventoryBag }.AsEnumerable().GetEnumerator());
        bagProxy.SetProperty(nameof(IBag.Location), InventoryLocation.Inventory);
        bagProxy.SetProperty(nameof(IBag.SlotsRemaining), 5u);
        bagProxy.SetMethodReturn(nameof(IEnumerable<IItem>.GetEnumerator), Enumerable.Empty<IItem>().GetEnumerator());

        materialProxy.SetProperty(nameof(ITradeskillMaterial.MaterialId), MaterialId);
        materialProxy.SetProperty(nameof(ITradeskillMaterial.Amount), satchelMaterialAmount);
        satchelProxy.SetMethodReturnFactory(nameof(IEnumerable<ITradeskillMaterial>.GetEnumerator), () => new[] { material }.AsEnumerable().GetEnumerator());

        return session;
    }

    private static GameTableManager CreateGameTableManager()
    {
        var gameTableManager = new GameTableManager(Options.Create(new GameTableConfig
        {
            GameTablePath = string.Empty
        }));

        SetAutoProperty(gameTableManager, nameof(GameTableManager.TradeskillSchematic2), CreateGameTable(
            new TradeskillSchematic2Entry
            {
                Id = SchematicId,
                TradeSkillId = (uint)TradeskillType.Armorer,
                LootId = LootGroupId,
                Item2IdOutput = 0u,
                OutputCount = 0u,
                Tier = 0u,
                Item2IdMaterial00 = MaterialItemId,
                MaterialCost00 = 2u
            }));
        SetAutoProperty(gameTableManager, nameof(GameTableManager.TradeskillMaterial), CreateGameTable(
            new TradeskillMaterialEntry
            {
                Id = MaterialId,
                Item2IdStatRevolution = MaterialItemId
            }));
        SetAutoProperty(gameTableManager, nameof(GameTableManager.TradeskillTier), CreateGameTable(
            new TradeskillTierEntry
            {
                Id = 1u,
                TradeSkillId = (uint)TradeskillType.Armorer,
                Tier = 1u,
                CraftXp = 12u
            }));

        return gameTableManager;
    }

    private static ClientCraftingSimpleCraft CreateRequest(uint schematicId)
    {
        var request = (ClientCraftingSimpleCraft)RuntimeHelpers.GetUninitializedObject(typeof(ClientCraftingSimpleCraft));
        SetAutoProperty(request, nameof(ClientCraftingSimpleCraft.ContextToken), 123u);
        SetAutoProperty(request, nameof(ClientCraftingSimpleCraft.CraftingStationUnitId), 456u);
        SetAutoProperty(request, nameof(ClientCraftingSimpleCraft.TradeskillSchematic2Id), schematicId);
        return request;
    }

    private static IReadOnlyList<T> GetMessages<T>(RecordingDispatchProxy<IWorldSession> sessionProxy)
        where T : class, IWritable
    {
        return sessionProxy
            .GetInvocations(nameof(IGameSession.EnqueueMessageEncrypted))
            .Where(i => i.Arguments.Length == 1)
            .Select(i => i.Arguments[0])
            .OfType<T>()
            .ToList();
    }

    private static GameTable<T> CreateGameTable<T>(params T[] entries)
        where T : class, new()
    {
        var table = (GameTable<T>)RuntimeHelpers.GetUninitializedObject(typeof(GameTable<T>));
        SetAutoProperty(table, nameof(GameTable<T>.Entries), entries);
        SetPrivateField(table, "header", new GameTableHeader
        {
            MaxId = entries.Length == 0 ? 0u : entries.Max(GetEntryId) + 1u
        });
        SetPrivateField(table, "lookup", BuildLookup(entries));
        return table;
    }

    private static void SetAutoProperty(object instance, string propertyName, object value)
    {
        FieldInfo backingField = instance.GetType()
            .GetField($"<{propertyName}>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic)!;
        backingField.SetValue(instance, value);
    }

    private static void SetPrivateField(object instance, string fieldName, object value)
    {
        FieldInfo field = instance.GetType()
            .GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic)!;
        field.SetValue(instance, value);
    }

    private static int[] BuildLookup<T>(IReadOnlyList<T> entries)
    {
        if (entries.Count == 0)
            return [];

        int[] lookup = Enumerable.Repeat(-1, (int)(entries.Max(GetEntryId) + 1u)).ToArray();
        for (int i = 0; i < entries.Count; i++)
            lookup[GetEntryId(entries[i])] = i;

        return lookup;
    }

    private static uint GetEntryId<T>(T entry)
    {
        return (uint)typeof(T).GetFields(BindingFlags.Instance | BindingFlags.Public)[0].GetValue(entry)!;
    }

    private sealed class StubLootManager : IGlobalLootManager
    {
        private readonly IReadOnlyList<GeneratedLootItem> generatedLoot;

        public StubLootManager(IReadOnlyList<GeneratedLootItem> generatedLoot)
        {
            this.generatedLoot = generatedLoot;
        }

        public uint LastLootGroupId { get; private set; }
        public IPlayer LastLooter { get; private set; }
        public uint LastRollCount { get; private set; }
        public IPlayer LastDeliverLooter { get; private set; }
        public IEnumerable<GeneratedLootItem> LastDeliveredLoot { get; private set; }
        public uint LastOwnerUnitId { get; private set; }
        public bool LastSendGrantedNotify { get; private set; }

        public void Update(double time) { }

        public void Initialise() { }

        public bool DropLoot(IPlayer looter, IWorldEntity lootedEntity) => false;
        public bool HasLoot(IItem lootedItem) => false;
        public bool DropLoot(IPlayer looter, IItem lootedItem) => false;
        public bool TryUseLootBag(IPlayer looter, IItem lootedItem, out string reason)
        {
            reason = string.Empty;
            return false;
        }

        public void SendLootNotify(IPlayer looter, uint ownerUnitId) { }
        public void SendLootNotifyForVisibleOwner(IPlayer looter, IWorldEntity owner) { }
        public bool TryGetLootRuntimeSnapshot(IPlayer looter, uint ownerUnitId, out LootRuntimeSnapshot snapshot)
        {
            snapshot = default;
            return false;
        }

        public void GiveLoot(IPlayer looter, uint ownerUnitId, uint lootUnitId) { }
        public void GiveAllLootInRange(IPlayer looter) { }
        public void RollLoot(IPlayer looter, uint ownerUnitId, uint lootUnitId, LootRollAction action) { }
        public void AssignMasterLoot(IPlayer master, uint ownerUnitId, uint lootUnitId, Identity assignee) { }
        public void GiveLoot(IPlayer looter, Item2Entry entry, uint count, uint ownerUnitId) { }
        public void GiveLoot(IPlayer looter, VirtualItemEntry entry, uint count, uint ownerUnitId) { }
        public void GiveLoot(IPlayer looter, AccountCurrencyType accountCurrencyType, uint count, uint ownerUnitId) { }
        public void GiveLoot(IPlayer looter, CurrencyType currencyType, uint count, uint ownerUnitId) { }

        public bool TryGenerateLoot(uint lootGroupId, IPlayer looter, uint rollCount, out IReadOnlyList<GeneratedLootItem> items, out string reason)
        {
            LastLootGroupId = lootGroupId;
            LastLooter = looter;
            LastRollCount = rollCount;
            items = generatedLoot;
            reason = string.Empty;
            return true;
        }

        public bool CanDeliverGeneratedLoot(IPlayer looter, IEnumerable<GeneratedLootItem> items, out string reason)
        {
            reason = string.Empty;
            return true;
        }

        public void GiveGeneratedLoot(IPlayer looter, IEnumerable<GeneratedLootItem> items, uint ownerUnitId, bool sendGrantedNotify = false)
        {
            LastDeliverLooter = looter;
            LastDeliveredLoot = items;
            LastOwnerUnitId = ownerUnitId;
            LastSendGrantedNotify = sendGrantedNotify;
        }
    }
}
