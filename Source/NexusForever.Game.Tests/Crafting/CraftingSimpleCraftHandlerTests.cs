using System.Reflection;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NexusForever.Game.Abstract;
using NexusForever.Game.Abstract.Achievement;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Loot;
using NexusForever.Game.Abstract.Map;
using NexusForever.Game.Static.Achievement;
using NexusForever.Game.Static.Crafting;
using NexusForever.Game.Static.Entity;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.GameTable;
using NexusForever.GameTable.Configuration.Model;
using NexusForever.GameTable.Model;
using NexusForever.Network.Message;
using NexusForever.Network.Session;
using NexusForever.Network.World.Message.Model.Crafting;
using NexusForever.Network.World.Message.Model.Shared;
using NexusForever.Network.World.Message.Static;
using NexusForever.WorldServer.Crafting;
using NexusForever.WorldServer.Network;
using NexusForever.WorldServer.Network.Message.Handler.Crafting;

namespace NexusForever.Game.Tests.Crafting;

public class CraftingSimpleCraftHandlerTests
{
    private const uint SchematicId = 5001u;
    private const uint OutputItemId = 7001u;
    private const uint MaterialItemId = 8001u;
    private const uint CatalystItemId = 8002u;
    private const ushort MaterialId = 55;
    private const ushort CatalystMaterialId = 56;
    private const ulong CharacterId = 9_901_001ul;

    [Fact]
    public void SimpleCraft_WithMaterialAndOutput_ConsumesMaterialCreatesOutputAndSendsSuccess()
    {
        IWorldSession session = CreateSession(
            satchelMaterialAmount: 2,
            out RecordingDispatchProxy<IInventory> inventoryProxy,
            out RecordingDispatchProxy<ISupplySatchelManager> satchelProxy,
            out RecordingDispatchProxy<ICharacterAchievementManager> achievementProxy,
            out RecordingDispatchProxy<IWorldSession> sessionProxy,
            out IItemInfo outputInfo);
        ClientCraftingSimpleCraftHandler handler = CreateHandler(outputInfo);

        handler.HandleMessage(session, CreateRequest(SchematicId));

        RecordingDispatchProxy<ISupplySatchelManager>.Invocation materialDebit =
            Assert.Single(satchelProxy.GetInvocations(nameof(ISupplySatchelManager.RemoveAmount)));
        Assert.Equal(MaterialId, materialDebit.Arguments[0]);
        Assert.Equal(2u, materialDebit.Arguments[1]);

        RecordingDispatchProxy<IInventory>.Invocation outputCreate =
            Assert.Single(inventoryProxy.GetInvocations(nameof(IInventory.ItemCreate)));
        Assert.Equal(InventoryLocation.Inventory, outputCreate.Arguments[0]);
        Assert.Same(outputInfo, outputCreate.Arguments[1]);
        Assert.Equal(1u, outputCreate.Arguments[2]);
        Assert.Equal(ItemUpdateReason.Crafting, outputCreate.Arguments[3]);

        IReadOnlyList<RecordingDispatchProxy<ICharacterAchievementManager>.Invocation> achievementCalls =
            achievementProxy.GetInvocations(nameof(ICharacterAchievementManager.CheckAchievements));
        Assert.Collection(achievementCalls,
            call =>
            {
                Assert.Equal(AchievementType.CraftItem, (AchievementType)call.Arguments[1]);
                Assert.Equal(OutputItemId, call.Arguments[2]);
                Assert.Equal(1u, call.Arguments[4]);
            },
            call =>
            {
                Assert.Equal(AchievementType.CraftItemChecklist, (AchievementType)call.Arguments[1]);
                Assert.Equal(OutputItemId, call.Arguments[2]);
            });

        ServerCraftingFinish finish = Assert.Single(GetMessages<ServerCraftingFinish>(sessionProxy));
        Assert.True(finish.Pass);
        Assert.Equal(SchematicId, finish.TradeskillSchematic2IdCrafted);
        Assert.Equal(OutputItemId, finish.Item2IdCrafted);
        Assert.Equal(CraftingDiscovery.Success, finish.HotOrCold);
        Assert.Equal(12u, finish.EarnedXp);
    }

    [Fact]
    public void SimpleCraft_WithZeroStation_ConsumesMaterialCreatesOutputAndSendsSuccess()
    {
        IWorldSession session = CreateSession(
            satchelMaterialAmount: 2,
            out RecordingDispatchProxy<IInventory> inventoryProxy,
            out RecordingDispatchProxy<ISupplySatchelManager> satchelProxy,
            out RecordingDispatchProxy<ICharacterAchievementManager> _,
            out RecordingDispatchProxy<IWorldSession> sessionProxy,
            out IItemInfo outputInfo);
        ClientCraftingSimpleCraftHandler handler = CreateHandler(outputInfo);

        handler.HandleMessage(session, CreateRequest(SchematicId, craftingStationUnitId: 0u));

        RecordingDispatchProxy<ISupplySatchelManager>.Invocation materialDebit =
            Assert.Single(satchelProxy.GetInvocations(nameof(ISupplySatchelManager.RemoveAmount)));
        Assert.Equal(MaterialId, materialDebit.Arguments[0]);
        Assert.Equal(2u, materialDebit.Arguments[1]);

        RecordingDispatchProxy<IInventory>.Invocation outputCreate =
            Assert.Single(inventoryProxy.GetInvocations(nameof(IInventory.ItemCreate)));
        Assert.Same(outputInfo, outputCreate.Arguments[1]);

        ServerCraftingFinish finish = Assert.Single(GetMessages<ServerCraftingFinish>(sessionProxy));
        Assert.True(finish.Pass);
        Assert.Equal(SchematicId, finish.TradeskillSchematic2IdCrafted);
        Assert.Equal(OutputItemId, finish.Item2IdCrafted);
        Assert.Equal(CraftingDiscovery.Success, finish.HotOrCold);
    }

    [Fact]
    public void ComplexCraft_WithCraftStatsAndChargeCounts_PreservesFixedRecipeSuccess()
    {
        IWorldSession session = CreateSession(
            satchelMaterialAmount: 2,
            out RecordingDispatchProxy<IInventory> inventoryProxy,
            out RecordingDispatchProxy<ISupplySatchelManager> satchelProxy,
            out RecordingDispatchProxy<ICharacterAchievementManager> _,
            out RecordingDispatchProxy<IWorldSession> sessionProxy,
            out IItemInfo outputInfo);
        ClientCraftingComplexCraftHandler handler = CreateComplexHandler(outputInfo);

        handler.HandleMessage(session, CreateComplexRequest(SchematicId));

        RecordingDispatchProxy<ISupplySatchelManager>.Invocation materialDebit =
            Assert.Single(satchelProxy.GetInvocations(nameof(ISupplySatchelManager.RemoveAmount)));
        Assert.Equal(MaterialId, materialDebit.Arguments[0]);
        Assert.Equal(2u, materialDebit.Arguments[1]);

        RecordingDispatchProxy<IInventory>.Invocation outputCreate =
            Assert.Single(inventoryProxy.GetInvocations(nameof(IInventory.ItemCreate)));
        Assert.Same(outputInfo, outputCreate.Arguments[1]);

        ServerCraftingFinish finish = Assert.Single(GetMessages<ServerCraftingFinish>(sessionProxy));
        Assert.True(finish.Pass);
        Assert.Equal(SchematicId, finish.TradeskillSchematic2IdCrafted);
        Assert.Equal(OutputItemId, finish.Item2IdCrafted);
        Assert.Equal(CraftingDiscovery.Success, finish.HotOrCold);
        Assert.Equal(CraftingDirection.None, finish.Direction);
    }

    [Fact]
    public void ComplexCraft_WithCraftStatsAndChargeCounts_DoesNotEmitBlockedCurrentCraftOrAuxPackets()
    {
        IWorldSession session = CreateSession(
            satchelMaterialAmount: 2,
            out RecordingDispatchProxy<IInventory> _,
            out RecordingDispatchProxy<ISupplySatchelManager> _,
            out RecordingDispatchProxy<ICharacterAchievementManager> _,
            out RecordingDispatchProxy<IWorldSession> sessionProxy,
            out IItemInfo outputInfo);
        ClientCraftingComplexCraftHandler handler = CreateComplexHandler(outputInfo);

        handler.HandleMessage(session, CreateComplexRequest(SchematicId));

        Assert.Single(GetMessages<ServerCraftingFinish>(sessionProxy));
        Assert.Empty(GetMessages<ServerCraftingCurrentCraft>(sessionProxy));
        Assert.Empty(GetMessages<ServerCraftingAuxFourUInt32FloatUInt32>(sessionProxy));
        Assert.Empty(GetMessages<ServerCraftingAuxUInt32AndTwoFloats>(sessionProxy));
    }

    [Fact]
    public void SimpleCraft_MissingMaterial_SendsFailureWithoutOutput()
    {
        IWorldSession session = CreateSession(
            satchelMaterialAmount: 0,
            out RecordingDispatchProxy<IInventory> inventoryProxy,
            out RecordingDispatchProxy<ISupplySatchelManager> satchelProxy,
            out RecordingDispatchProxy<ICharacterAchievementManager> achievementProxy,
            out RecordingDispatchProxy<IWorldSession> sessionProxy,
            out IItemInfo outputInfo);
        ClientCraftingSimpleCraftHandler handler = CreateHandler(outputInfo);

        handler.HandleMessage(session, CreateRequest(SchematicId));

        Assert.Empty(satchelProxy.GetInvocations(nameof(ISupplySatchelManager.RemoveAmount)));
        Assert.Empty(inventoryProxy.GetInvocations(nameof(IInventory.ItemCreate)));
        Assert.Empty(achievementProxy.GetInvocations(nameof(ICharacterAchievementManager.CheckAchievements)));

        ServerCraftingFinish finish = Assert.Single(GetMessages<ServerCraftingFinish>(sessionProxy));
        Assert.False(finish.Pass);
        Assert.Equal(SchematicId, finish.TradeskillSchematic2IdCrafted);
        Assert.Equal(0u, finish.Item2IdCrafted);
    }

    [Fact]
    public void CraftItem_WithCountAndCatalyst_ConsumesScaledMaterialsCreatesScaledOutputAndSendsSuccess()
    {
        IWorldSession session = CreateSession(
            satchelMaterialAmount: 4,
            out RecordingDispatchProxy<IInventory> inventoryProxy,
            out RecordingDispatchProxy<ISupplySatchelManager> satchelProxy,
            out RecordingDispatchProxy<ICharacterAchievementManager> achievementProxy,
            out RecordingDispatchProxy<IWorldSession> sessionProxy,
            out IItemInfo outputInfo,
            catalystMaterialAmount: 2,
            addTradeskillXpReturn: 24u);
        ClientCraftingCraftItemHandler handler = CreateCraftItemHandler(outputInfo);

        handler.HandleMessage(session, CreateCraftItemRequest(SchematicId, 2u, CatalystItemId));

        IReadOnlyList<RecordingDispatchProxy<ISupplySatchelManager>.Invocation> materialDebits =
            satchelProxy.GetInvocations(nameof(ISupplySatchelManager.RemoveAmount));
        Assert.Contains(materialDebits, call => (ushort)call.Arguments[0] == CatalystMaterialId && (uint)call.Arguments[1] == 2u);
        Assert.Contains(materialDebits, call => (ushort)call.Arguments[0] == MaterialId && (uint)call.Arguments[1] == 4u);

        RecordingDispatchProxy<IInventory>.Invocation outputCreate =
            Assert.Single(inventoryProxy.GetInvocations(nameof(IInventory.ItemCreate)));
        Assert.Equal(InventoryLocation.Inventory, outputCreate.Arguments[0]);
        Assert.Same(outputInfo, outputCreate.Arguments[1]);
        Assert.Equal(2u, outputCreate.Arguments[2]);
        Assert.Equal(ItemUpdateReason.Crafting, outputCreate.Arguments[3]);

        IReadOnlyList<RecordingDispatchProxy<ICharacterAchievementManager>.Invocation> achievementCalls =
            achievementProxy.GetInvocations(nameof(ICharacterAchievementManager.CheckAchievements));
        Assert.Collection(achievementCalls,
            call =>
            {
                Assert.Equal(AchievementType.CraftItem, (AchievementType)call.Arguments[1]);
                Assert.Equal(OutputItemId, call.Arguments[2]);
                Assert.Equal(2u, call.Arguments[4]);
            },
            call =>
            {
                Assert.Equal(AchievementType.CraftItemChecklist, (AchievementType)call.Arguments[1]);
                Assert.Equal(OutputItemId, call.Arguments[2]);
            });

        ServerCraftingFinish finish = Assert.Single(GetMessages<ServerCraftingFinish>(sessionProxy));
        Assert.True(finish.Pass);
        Assert.Equal(SchematicId, finish.TradeskillSchematic2IdCrafted);
        Assert.Equal(OutputItemId, finish.Item2IdCrafted);
        Assert.Equal(CraftingDiscovery.Success, finish.HotOrCold);
        Assert.Equal(24u, finish.EarnedXp);
    }

    [Fact]
    public void SimpleCraft_WithStationForDifferentTradeskill_SendsFailureWithoutOutput()
    {
        IWorldSession session = CreateSession(
            satchelMaterialAmount: 2,
            out RecordingDispatchProxy<IInventory> inventoryProxy,
            out RecordingDispatchProxy<ISupplySatchelManager> satchelProxy,
            out RecordingDispatchProxy<ICharacterAchievementManager> achievementProxy,
            out RecordingDispatchProxy<IWorldSession> sessionProxy,
            out IItemInfo outputInfo,
            stationTradeskillId: (uint)TradeskillType.Weaponsmith);
        ClientCraftingSimpleCraftHandler handler = CreateHandler(outputInfo);

        handler.HandleMessage(session, CreateRequest(SchematicId));

        Assert.Empty(satchelProxy.GetInvocations(nameof(ISupplySatchelManager.RemoveAmount)));
        Assert.Empty(inventoryProxy.GetInvocations(nameof(IInventory.ItemCreate)));
        Assert.Empty(achievementProxy.GetInvocations(nameof(ICharacterAchievementManager.CheckAchievements)));

        ServerCraftingFinish finish = Assert.Single(GetMessages<ServerCraftingFinish>(sessionProxy));
        Assert.False(finish.Pass);
        Assert.Equal(SchematicId, finish.TradeskillSchematic2IdCrafted);
    }

    private static ClientCraftingSimpleCraftHandler CreateHandler(IItemInfo outputInfo)
    {
        IItemManager itemManager = RecordingDispatchProxy<IItemManager>.Create(out RecordingDispatchProxy<IItemManager> itemManagerProxy);
        IGlobalLootManager lootManager = RecordingDispatchProxy<IGlobalLootManager>.Create(out _);
        itemManagerProxy.SetMethodReturn(nameof(IItemManager.GetItemInfo), outputInfo);

        return new ClientCraftingSimpleCraftHandler(
            NullLogger<ClientCraftingSimpleCraftHandler>.Instance,
            CreateGameTableManager(),
            itemManager,
            lootManager,
            new CraftingModifierSessionStore());
    }

    private static ClientCraftingCraftItemHandler CreateCraftItemHandler(IItemInfo outputInfo)
    {
        IItemManager itemManager = RecordingDispatchProxy<IItemManager>.Create(out RecordingDispatchProxy<IItemManager> itemManagerProxy);
        IGlobalLootManager lootManager = RecordingDispatchProxy<IGlobalLootManager>.Create(out _);
        itemManagerProxy.SetMethodReturn(nameof(IItemManager.GetItemInfo), outputInfo);

        return new ClientCraftingCraftItemHandler(
            NullLogger<ClientCraftingCraftItemHandler>.Instance,
            CreateGameTableManager(),
            itemManager,
            lootManager,
            new CraftingModifierSessionStore());
    }

    private static ClientCraftingComplexCraftHandler CreateComplexHandler(IItemInfo outputInfo)
    {
        IItemManager itemManager = RecordingDispatchProxy<IItemManager>.Create(out RecordingDispatchProxy<IItemManager> itemManagerProxy);
        IGlobalLootManager lootManager = RecordingDispatchProxy<IGlobalLootManager>.Create(out _);
        itemManagerProxy.SetMethodReturn(nameof(IItemManager.GetItemInfo), outputInfo);

        return new ClientCraftingComplexCraftHandler(
            NullLogger<ClientCraftingComplexCraftHandler>.Instance,
            CreateGameTableManager(),
            itemManager,
            lootManager,
            new CraftingModifierSessionStore());
    }

    private static IWorldSession CreateSession(
        ushort satchelMaterialAmount,
        out RecordingDispatchProxy<IInventory> inventoryProxy,
        out RecordingDispatchProxy<ISupplySatchelManager> satchelProxy,
        out RecordingDispatchProxy<ICharacterAchievementManager> achievementProxy,
        out RecordingDispatchProxy<IWorldSession> sessionProxy,
        out IItemInfo outputInfo,
        ushort catalystMaterialAmount = 0,
        uint addTradeskillXpReturn = 12u,
        uint stationTradeskillId = (uint)TradeskillType.Armorer)
    {
        IWorldSession session = RecordingDispatchProxy<IWorldSession>.Create(out sessionProxy);
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out RecordingDispatchProxy<IPlayer> playerProxy);
        IBaseMap map = RecordingDispatchProxy<IBaseMap>.Create(out RecordingDispatchProxy<IBaseMap> mapProxy);
        IWorldEntity station = RecordingDispatchProxy<IWorldEntity>.Create(out RecordingDispatchProxy<IWorldEntity> stationProxy);
        IInventory inventory = RecordingDispatchProxy<IInventory>.Create(out inventoryProxy);
        IBag inventoryBag = RecordingDispatchProxy<IBag>.Create(out RecordingDispatchProxy<IBag> bagProxy);
        ISupplySatchelManager satchel = RecordingDispatchProxy<ISupplySatchelManager>.Create(out satchelProxy);
        ITradeskillMaterial material = RecordingDispatchProxy<ITradeskillMaterial>.Create(out RecordingDispatchProxy<ITradeskillMaterial> materialProxy);
        ICharacterAchievementManager achievementManager =
            RecordingDispatchProxy<ICharacterAchievementManager>.Create(out achievementProxy);
        outputInfo = RecordingDispatchProxy<IItemInfo>.Create(out RecordingDispatchProxy<IItemInfo> outputInfoProxy);

        sessionProxy.SetProperty(nameof(IWorldSession.Player), player);
        playerProxy.SetProperty(nameof(IPlayer.Guid), 42u);
        playerProxy.SetProperty(nameof(IPlayer.CharacterId), CharacterId + satchelMaterialAmount);
        playerProxy.SetProperty(nameof(IPlayer.Map), map);
        playerProxy.SetProperty(nameof(IPlayer.Inventory), inventory);
        playerProxy.SetProperty(nameof(IPlayer.SupplySatchelManager), satchel);
        playerProxy.SetProperty(nameof(IPlayer.AchievementManager), achievementManager);
        playerProxy.SetMethodReturn(nameof(IPlayer.HasTradeskill), true);
        playerProxy.SetMethodReturn(nameof(IPlayer.AddTradeskillXp), addTradeskillXpReturn);
        stationProxy.SetProperty(nameof(IWorldEntity.CreatureEntry), new Creature2Entry
        {
            TradeSkillIdStation = stationTradeskillId
        });
        mapProxy.SetMethodReturn(nameof(IBaseMap.GetEntity), station);

        inventoryProxy.SetMethodReturnFactory(nameof(IEnumerable<IBag>.GetEnumerator), () => new[] { inventoryBag }.AsEnumerable().GetEnumerator());
        bagProxy.SetProperty(nameof(IBag.Location), InventoryLocation.Inventory);
        bagProxy.SetProperty(nameof(IBag.SlotsRemaining), 5u);
        bagProxy.SetMethodReturn(nameof(IEnumerable<IItem>.GetEnumerator), Enumerable.Empty<IItem>().GetEnumerator());

        materialProxy.SetProperty(nameof(ITradeskillMaterial.MaterialId), MaterialId);
        materialProxy.SetProperty(nameof(ITradeskillMaterial.Amount), satchelMaterialAmount);
        ITradeskillMaterial catalystMaterial = RecordingDispatchProxy<ITradeskillMaterial>.Create(out RecordingDispatchProxy<ITradeskillMaterial> catalystMaterialProxy);
        catalystMaterialProxy.SetProperty(nameof(ITradeskillMaterial.MaterialId), CatalystMaterialId);
        catalystMaterialProxy.SetProperty(nameof(ITradeskillMaterial.Amount), catalystMaterialAmount);
        var materials = new List<ITradeskillMaterial>();
        if (satchelMaterialAmount != 0)
            materials.Add(material);
        if (catalystMaterialAmount != 0)
            materials.Add(catalystMaterial);
        satchelProxy.SetMethodReturnFactory(nameof(IEnumerable<ITradeskillMaterial>.GetEnumerator), () => materials.AsEnumerable().GetEnumerator());

        outputInfoProxy.SetProperty(nameof(IItemInfo.Id), OutputItemId);
        outputInfoProxy.SetProperty(nameof(IItemInfo.Entry), new Item2Entry
        {
            Id = OutputItemId,
            MaxStackCount = 1u
        });
        outputInfoProxy.SetMethodReturn(nameof(IItemInfo.IsStackable), false);

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
                Item2IdOutput = OutputItemId,
                OutputCount = 1u,
                Tier = 0u,
                Item2IdMaterial00 = MaterialItemId,
                MaterialCost00 = 2u
            }));
        SetAutoProperty(gameTableManager, nameof(GameTableManager.TradeskillMaterial), CreateGameTable(
            new TradeskillMaterialEntry
            {
                Id = MaterialId,
                Item2IdStatRevolution = MaterialItemId
            },
            new TradeskillMaterialEntry
            {
                Id = CatalystMaterialId,
                Item2IdStatRevolution = CatalystItemId
            }));
        SetAutoProperty(gameTableManager, nameof(GameTableManager.Item), CreateGameTable(
            new Item2Entry
            {
                Id = CatalystItemId,
                MaxStackCount = 10u
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

    private static ClientCraftingSimpleCraft CreateRequest(uint schematicId, uint craftingStationUnitId = 456u)
    {
        var request = (ClientCraftingSimpleCraft)RuntimeHelpers.GetUninitializedObject(typeof(ClientCraftingSimpleCraft));
        SetAutoProperty(request, nameof(ClientCraftingSimpleCraft.ContextToken), 123u);
        SetAutoProperty(request, nameof(ClientCraftingSimpleCraft.CraftingStationUnitId), craftingStationUnitId);
        SetAutoProperty(request, nameof(ClientCraftingSimpleCraft.TradeskillSchematic2Id), schematicId);
        return request;
    }

    private static ClientCraftingCraftItem CreateCraftItemRequest(uint schematicId, uint schematicCount, uint catalystItem2Id)
    {
        var request = (ClientCraftingCraftItem)RuntimeHelpers.GetUninitializedObject(typeof(ClientCraftingCraftItem));
        SetAutoProperty(request, nameof(ClientCraftingCraftItem.ContextToken), 123u);
        SetAutoProperty(request, nameof(ClientCraftingCraftItem.CraftingStationUnitId), 456u);
        SetAutoProperty(request, nameof(ClientCraftingCraftItem.TradeskillSchematic2Id), schematicId);
        SetAutoProperty(request, nameof(ClientCraftingCraftItem.SchematicCount), schematicCount);
        SetAutoProperty(request, nameof(ClientCraftingCraftItem.CatalystItem2Id), catalystItem2Id);
        return request;
    }

    private static ClientCraftingComplexCraft CreateComplexRequest(uint schematicId)
    {
        var request = (ClientCraftingComplexCraft)RuntimeHelpers.GetUninitializedObject(typeof(ClientCraftingComplexCraft));
        SetAutoProperty(request, nameof(ClientCraftingComplexCraft.ContextToken), 123u);
        SetAutoProperty(request, nameof(ClientCraftingComplexCraft.CraftingStationUnitId), 456u);
        SetAutoProperty(request, nameof(ClientCraftingComplexCraft.TradeskillSchematic2Id), schematicId);
        SetAutoProperty(request, nameof(ClientCraftingComplexCraft.CraftStats), new CraftStats
        {
            StatType =
            [
                Property.AssaultRating,
                Property.SupportRating,
                Property.RatingCritSeverityIncrease,
                Property.RatingArmorPierce,
                Property.RatingAvoidIncrease
            ],
            Unknown = 7,
            ApSpSplit = 8,
            CircuitComplete = 1098u | (3u << 16)
        });
        SetAutoProperty(request, nameof(ClientCraftingComplexCraft.PowerCoreItem2Id), 0u);
        SetAutoProperty(request, nameof(ClientCraftingComplexCraft.ApSpSplitDelta), 400u);
        SetAutoProperty(request, nameof(ClientCraftingComplexCraft.ChargeCounts), new[] { -1, 5 });
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

    private static GameTable<T> CreateGameTable<T>(params T[] entries) where T : class, new()
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
}
