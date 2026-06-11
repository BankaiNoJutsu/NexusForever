using System.Numerics;
using System.Reflection;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Map;
using NexusForever.Game.Abstract.Map.Instance;
using NexusForever.Game.Abstract.PublicEvent;
using NexusForever.Game.Configuration.Model;
using NexusForever.Game.Map;
using NexusForever.Game.Map.Instance;
using NexusForever.Game.Static.Map;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.GameTable;
using NexusForever.GameTable.Configuration.Model;
using NexusForever.GameTable.Model;
using NexusForever.Network.Message;
using NexusForever.Network.Session;
using NexusForever.Network.World.Message.Model;
using NexusForever.Shared.Configuration;

namespace NexusForever.Game.Tests.Map;

public class MapInstancePendingRemovalTests
{
    [Fact]
    public void EnqueuePendingRemoval_WithMissingGameFormulaTable_UsesClientDefaultTimer()
    {
        (ISharedConfiguration sharedConfiguration, GameTableManager gameTableManager) = CreateMapDependencies();

        var map = new TestMapInstance(sharedConfiguration, gameTableManager);
        IPlayer player = CreatePlayer(51u, out RecordingDispatchProxy<IGameSession> sessionProxy);

        map.EnqueuePendingRemoval(player, WorldRemovalReason.KickedFromCommunity);

        MapInstanceRemoval removal = Assert.Single(map.GetPendingRemovals());
        Assert.Equal(51u, removal.Guid);
        Assert.Equal(WorldRemovalReason.KickedFromCommunity, removal.Reason);
        Assert.Equal(30d, removal.Timer);
        Assert.Single(GetEncryptedMessages<ServerPendingWorldRemoval>(sessionProxy));
    }

    [Fact]
    public void EnqueuePendingRemoval_WithGameFormulaRow_UsesConfiguredTimer()
    {
        (ISharedConfiguration sharedConfiguration, GameTableManager gameTableManager) = CreateMapDependencies(CreateGameTable(new GameFormulaEntry
        {
            Id       = 1123u,
            Dataint0 = 45000u
        }));

        var map = new TestMapInstance(sharedConfiguration, gameTableManager);
        IPlayer player = CreatePlayer(52u, out RecordingDispatchProxy<IGameSession> sessionProxy);

        map.EnqueuePendingRemoval(player, WorldRemovalReason.GroupMembership);

        MapInstanceRemoval removal = Assert.Single(map.GetPendingRemovals());
        Assert.Equal(45d, removal.Timer);
        Assert.Single(GetEncryptedMessages<ServerPendingWorldRemoval>(sessionProxy));
    }

    private static IPlayer CreatePlayer(uint guid, out RecordingDispatchProxy<IGameSession> sessionProxy)
    {
        IGameSession session = RecordingDispatchProxy<IGameSession>.Create(out sessionProxy);
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out RecordingDispatchProxy<IPlayer> playerProxy);
        playerProxy.SetProperty(nameof(IPlayer.Guid), guid);
        playerProxy.SetProperty(nameof(IPlayer.Session), session);
        return player;
    }

    private static IReadOnlyList<T> GetEncryptedMessages<T>(RecordingDispatchProxy<IGameSession> sessionProxy)
        where T : class, IWritable
    {
        return sessionProxy.GetInvocations(nameof(IGameSession.EnqueueMessageEncrypted))
            .Select(i => i.Arguments[0])
            .OfType<T>()
            .ToList();
    }

    private static (ISharedConfiguration SharedConfiguration, GameTableManager GameTableManager) CreateMapDependencies(GameTable<GameFormulaEntry> gameFormulaTable = null)
    {
        var configuration = new SharedConfiguration(new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string>
            {
                ["Realm:Map:GridUnloadTimer"] = "600",
                ["Realm:Map:InstancePlayerLimit"] = "100"
            })
            .Build());
        configuration.Initialise<TestConfiguration>();

        var gameTableManager = new GameTableManager(Options.Create(new GameTableConfig
        {
            GameTablePath = string.Empty
        }));

        if (gameFormulaTable != null)
            SetAutoProperty(gameTableManager, nameof(GameTableManager.GameFormula), gameFormulaTable);

        return (configuration, gameTableManager);
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

    private static void SetAutoProperty(object instance, string propertyName, object value)
    {
        FieldInfo backingField = instance.GetType().GetField($"<{propertyName}>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic)!;
        backingField.SetValue(instance, value);
    }

    private static void SetPrivateField(object instance, string fieldName, object value)
    {
        FieldInfo field = instance.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic)!;
        field.SetValue(instance, value);
    }

    private sealed class TestMapInstance : MapInstance
    {
        public TestMapInstance(ISharedConfiguration sharedConfiguration, IGameTableManager gameTableManager)
            : base(
                RecordingDispatchProxy<IEntityFactory>.Create(out _),
                RecordingDispatchProxy<IPublicEventManager>.Create(out _),
                sharedConfiguration: sharedConfiguration,
                gameTableManager: gameTableManager)
        {
        }

        public IReadOnlyList<MapInstanceRemoval> GetPendingRemovals()
        {
            var removals = (Dictionary<uint, IMapInstanceRemoval>)typeof(MapInstance)
                .GetField("instanceRemovals", BindingFlags.Instance | BindingFlags.NonPublic)!
                .GetValue(this)!;

            return removals.Values
                .Cast<MapInstanceRemoval>()
                .ToList();
        }

        protected override IMapPosition GetPlayerReturnLocation(IPlayer player)
        {
            return new MapPosition
            {
                Position = new Vector3(1f, 2f, 3f)
            };
        }
    }

    private sealed class TestConfiguration
    {
        public RealmConfig Realm { get; set; }
    }
}
