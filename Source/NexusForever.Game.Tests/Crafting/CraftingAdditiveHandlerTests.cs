using System.Reflection;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Map;
using NexusForever.Game.Static.Crafting;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.GameTable;
using NexusForever.GameTable.Configuration.Model;
using NexusForever.GameTable.Model;
using NexusForever.Network;
using NexusForever.Network.Message;
using NexusForever.Network.Session;
using NexusForever.Network.World.Message.Model.Crafting;
using NexusForever.WorldServer.Crafting;
using NexusForever.WorldServer.Network;
using NexusForever.WorldServer.Network.Message.Handler.Crafting;

namespace NexusForever.Game.Tests.Crafting;

public class CraftingAdditiveHandlerTests
{
    private const uint StationUnitId = 456u;
    private const uint AdditiveItem2Id = 8101u;
    private const uint CatalystItem2Id = 8201u;
    private const uint AdditiveId = 9101u;
    private const uint CatalystId = 9201u;
    private const ulong CharacterId = 4_008_001ul;

    [Fact]
    public void Additive_WithZeroStation_ThrowsInvalidPacketValueException()
    {
        IWorldSession session = RecordingDispatchProxy<IWorldSession>.Create(out RecordingDispatchProxy<IWorldSession> sessionProxy);
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out _);
        sessionProxy.SetProperty(nameof(IWorldSession.Player), player);

        var handler = new ClientCraftingAdditiveHandler(
            NullLogger<ClientCraftingAdditiveHandler>.Instance,
            new GameTableManager(Options.Create(new GameTableConfig
            {
                GameTablePath = string.Empty
            })),
            new CraftingModifierSessionStore());

        Assert.Throws<InvalidPacketValueException>(() => handler.HandleMessage(session, CreateRequest(0u)));
    }

    [Fact]
    public void Additive_WithValidStation_RecordsModifierStateWithoutBlockedCurrentCraftOrAuxPackets()
    {
        GameTableManager gameTableManager = CreateGameTableManager();
        var modifierStore = new CraftingModifierSessionStore();
        IWorldSession session = CreateSession(CharacterId, out RecordingDispatchProxy<IWorldSession> sessionProxy, out RecordingDispatchProxy<IPlayer> playerProxy);
        var handler = new ClientCraftingAdditiveHandler(
            NullLogger<ClientCraftingAdditiveHandler>.Instance,
            gameTableManager,
            modifierStore);

        try
        {
            handler.HandleMessage(session, CreateRequest(StationUnitId, AdditiveItem2Id, CatalystItem2Id));

            bool result = modifierStore.TryBuildModifierItemCounts(
                session.Player,
                gameTableManager,
                CreateSchematic(),
                out IReadOnlyDictionary<uint, uint> itemCounts,
                out string reason);

            Assert.True(result, reason);
            Assert.Empty(reason);
            Assert.Equal(2, itemCounts.Count);
            Assert.Equal(1u, itemCounts[AdditiveItem2Id]);
            Assert.Equal(1u, itemCounts[CatalystItem2Id]);
            Assert.Empty(playerProxy.GetInvocations(nameof(IPlayer.SendGenericError)));
            Assert.Empty(GetMessages<ServerCraftingCurrentCraft>(sessionProxy));
            Assert.Empty(GetMessages<ServerCraftingAuxFourUInt32FloatUInt32>(sessionProxy));
            Assert.Empty(GetMessages<ServerCraftingAuxUInt32AndTwoFloats>(sessionProxy));
        }
        finally
        {
            modifierStore.ClearModifiers(session.Player);
        }
    }

    [Fact]
    public void Abandon_ClearsModifierStateWithoutBlockedCurrentCraftOrAuxPackets()
    {
        GameTableManager gameTableManager = CreateGameTableManager();
        var modifierStore = new CraftingModifierSessionStore();
        IWorldSession session = CreateSession(CharacterId + 1ul, out RecordingDispatchProxy<IWorldSession> sessionProxy, out _);
        var additiveHandler = new ClientCraftingAdditiveHandler(
            NullLogger<ClientCraftingAdditiveHandler>.Instance,
            gameTableManager,
            modifierStore);
        var abandonHandler = new ClientCraftingAbandonHandler(
            NullLogger<ClientCraftingAbandonHandler>.Instance,
            modifierStore);

        try
        {
            additiveHandler.HandleMessage(session, CreateRequest(StationUnitId, AdditiveItem2Id, CatalystItem2Id));
            abandonHandler.HandleMessage(session, new ClientCraftingAbandon());

            bool result = modifierStore.TryBuildModifierItemCounts(
                session.Player,
                gameTableManager,
                CreateSchematic(),
                out IReadOnlyDictionary<uint, uint> itemCounts,
                out string reason);

            Assert.True(result, reason);
            Assert.Empty(reason);
            Assert.Empty(itemCounts);
            Assert.Empty(GetMessages<ServerCraftingCurrentCraft>(sessionProxy));
            Assert.Empty(GetMessages<ServerCraftingAuxFourUInt32FloatUInt32>(sessionProxy));
            Assert.Empty(GetMessages<ServerCraftingAuxUInt32AndTwoFloats>(sessionProxy));
        }
        finally
        {
            modifierStore.ClearModifiers(session.Player);
        }
    }

    [Fact]
    public void ModifierCounts_WhenItemTableMissingReturnsInvalidAdditiveItem()
    {
        GameTableManager gameTableManager = CreateGameTableManager();
        SetAutoProperty(gameTableManager, nameof(GameTableManager.Item), null);
        var modifierStore = new CraftingModifierSessionStore();
        IWorldSession session = CreateSession(CharacterId + 2ul, out _, out _);
        Assert.True(modifierStore.TryAddModifier(session.Player, AdditiveItem2Id, 0u));

        bool result = modifierStore.TryBuildModifierItemCounts(
            session.Player,
            gameTableManager,
            CreateSchematic(),
            out IReadOnlyDictionary<uint, uint> itemCounts,
            out string reason);

        Assert.False(result);
        Assert.Equal($"invalid-additive-item:{AdditiveItem2Id}", reason);
        Assert.Empty(itemCounts);
    }

    [Fact]
    public void ModifierCounts_WhenAdditiveTableMissingReturnsInvalidAdditive()
    {
        GameTableManager gameTableManager = CreateGameTableManager();
        SetAutoProperty(gameTableManager, nameof(GameTableManager.TradeskillAdditive), null);
        var modifierStore = new CraftingModifierSessionStore();
        IWorldSession session = CreateSession(CharacterId + 3ul, out _, out _);
        Assert.True(modifierStore.TryAddModifier(session.Player, AdditiveItem2Id, 0u));

        bool result = modifierStore.TryBuildModifierItemCounts(
            session.Player,
            gameTableManager,
            CreateSchematic(),
            out IReadOnlyDictionary<uint, uint> itemCounts,
            out string reason);

        Assert.False(result);
        Assert.Equal($"invalid-additive:{AdditiveId}", reason);
        Assert.Empty(itemCounts);
    }

    [Fact]
    public void ModifierCounts_WhenCatalystTableMissingReturnsInvalidCatalyst()
    {
        GameTableManager gameTableManager = CreateGameTableManager();
        SetAutoProperty(gameTableManager, nameof(GameTableManager.TradeskillCatalyst), null);
        var modifierStore = new CraftingModifierSessionStore();
        IWorldSession session = CreateSession(CharacterId + 4ul, out _, out _);
        Assert.True(modifierStore.TryAddModifier(session.Player, 0u, CatalystItem2Id));

        bool result = modifierStore.TryBuildModifierItemCounts(
            session.Player,
            gameTableManager,
            CreateSchematic(),
            out IReadOnlyDictionary<uint, uint> itemCounts,
            out string reason);

        Assert.False(result);
        Assert.Equal($"invalid-catalyst:{CatalystId}", reason);
        Assert.Empty(itemCounts);
    }

    private static ClientCraftingAdditive CreateRequest(uint craftingStationUnitId, uint additiveItem2Id = 0u, uint catalystItem2Id = 0u)
    {
        var request = (ClientCraftingAdditive)RuntimeHelpers.GetUninitializedObject(typeof(ClientCraftingAdditive));
        SetAutoProperty(request, nameof(ClientCraftingAdditive.CraftingStationUnitId), craftingStationUnitId);
        SetAutoProperty(request, nameof(ClientCraftingAdditive.AdditiveItem2Id), additiveItem2Id);
        SetAutoProperty(request, nameof(ClientCraftingAdditive.CatalystItem2Id), catalystItem2Id);
        return request;
    }

    private static IWorldSession CreateSession(
        ulong characterId,
        out RecordingDispatchProxy<IWorldSession> sessionProxy,
        out RecordingDispatchProxy<IPlayer> playerProxy)
    {
        IWorldSession session = RecordingDispatchProxy<IWorldSession>.Create(out sessionProxy);
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out playerProxy);
        IBaseMap map = RecordingDispatchProxy<IBaseMap>.Create(out RecordingDispatchProxy<IBaseMap> mapProxy);
        IWorldEntity station = RecordingDispatchProxy<IWorldEntity>.Create(out RecordingDispatchProxy<IWorldEntity> stationProxy);
        IInventory inventory = RecordingDispatchProxy<IInventory>.Create(out RecordingDispatchProxy<IInventory> inventoryProxy);

        sessionProxy.SetProperty(nameof(IWorldSession.Player), player);
        playerProxy.SetProperty(nameof(IPlayer.Guid), 42u);
        playerProxy.SetProperty(nameof(IPlayer.CharacterId), characterId);
        playerProxy.SetProperty(nameof(IPlayer.Map), map);
        playerProxy.SetProperty(nameof(IPlayer.Inventory), inventory);

        stationProxy.SetProperty(nameof(IWorldEntity.CreatureEntry), new Creature2Entry
        {
            TradeSkillIdStation = (uint)TradeskillType.Armorer
        });
        mapProxy.SetMethodReturn(nameof(IBaseMap.GetEntity), station);

        inventoryProxy.SetMethodHandler(nameof(IInventory.HasItemCount), args =>
        {
            uint item2Id = (uint)args[0];
            uint count = (uint)args[1];
            return count == 1u && (item2Id == AdditiveItem2Id || item2Id == CatalystItem2Id);
        });

        return session;
    }

    private static GameTableManager CreateGameTableManager()
    {
        var gameTableManager = new GameTableManager(Options.Create(new GameTableConfig
        {
            GameTablePath = string.Empty
        }));

        SetAutoProperty(gameTableManager, nameof(GameTableManager.Item), CreateGameTable(
            new Item2Entry
            {
                Id = AdditiveItem2Id,
                TradeskillAdditiveId = AdditiveId
            },
            new Item2Entry
            {
                Id = CatalystItem2Id,
                TradeskillCatalystId = CatalystId
            }));
        SetAutoProperty(gameTableManager, nameof(GameTableManager.TradeskillAdditive), CreateGameTable(
            new TradeskillAdditiveEntry
            {
                Id = AdditiveId,
                TradeSkillId = (uint)TradeskillType.Armorer
            }));
        SetAutoProperty(gameTableManager, nameof(GameTableManager.TradeskillCatalyst), CreateGameTable(
            new TradeskillCatalystEntry
            {
                Id = CatalystId,
                TradeSkillId = (uint)TradeskillType.Armorer
            }));

        return gameTableManager;
    }

    private static TradeskillSchematic2Entry CreateSchematic()
    {
        return new TradeskillSchematic2Entry
        {
            Id = 5001u,
            TradeSkillId = (uint)TradeskillType.Armorer,
            MaxAdditives = 5u
        };
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
