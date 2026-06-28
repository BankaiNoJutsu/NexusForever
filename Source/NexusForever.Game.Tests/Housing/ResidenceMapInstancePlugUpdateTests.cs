using System.Reflection;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.Configuration;
using NexusForever.Game.Abstract;
using NexusForever.Game.Abstract.Achievement;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Housing;
using NexusForever.Game.Abstract.Map.Lock;
using NexusForever.Game.Abstract.PublicEvent;
using NexusForever.Game.Configuration.Model;
using NexusForever.Game.Map.Instance;
using NexusForever.Game.Static.Achievement;
using NexusForever.Game.Static.Housing;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.GameTable;
using NexusForever.GameTable.Model;
using NexusForever.Network.Message;
using NexusForever.Network.Session;
using NexusForever.Network.World.Message.Model;
using NexusForever.Network.World.Message.Static;
using NexusForever.Script;
using NexusForever.Shared.Configuration;

namespace NexusForever.Game.Tests.Housing;

public class ResidenceMapInstancePlugUpdateTests
{
    private const ushort RealmId = 7;
    private const ulong ResidenceId = 1234ul;
    private const uint PlotInfoId = 55u;
    private const byte PlotIndex = 2;
    private const uint PlotType = 3u;
    private const uint DefaultPlugId = 88u;

    [Fact]
    public void PlugUpdate_DefaultContributionBackedPlug_AllowsPlacementWithoutClientPayload()
    {
        ResidenceMapInstance map = CreateResidenceMapInstance(
            CreateSharedConfiguration(),
            out RecordingDispatchProxy<IGameTableManager> gameTableProxy,
            out RecordingDispatchProxy<IEntityFactory> entityFactoryProxy);

        HousingPlugItemEntry plugEntry = CreateDefaultPlugEntry();
        plugEntry.HousingContributionInfoId00 = 12u;
        gameTableProxy.SetProperty(nameof(IGameTableManager.HousingPlugItem), CreateGameTable(plugEntry));

        IPlugEntity plugEntity = RecordingDispatchProxy<IPlugEntity>.Create(out RecordingDispatchProxy<IPlugEntity> plugProxy);
        entityFactoryProxy.SetMethodReturnFactory(nameof(IEntityFactory.CreateEntity), () => plugEntity);

        IPlot plot = CreatePlot(plugEntry, out RecordingDispatchProxy<IPlot> plotProxy, out HousingPlotInfoEntry plotInfo);
        InitialiseResidence(map, plot, out _);
        IPlayer player = CreatePlayer(
            out RecordingDispatchProxy<ICharacterAchievementManager> achievementProxy,
            out RecordingDispatchProxy<IGameSession> sessionProxy);

        ClientHousingPlugUpdate update = CreatePlugUpdate();

        map.PlugUpdate(player, update);

        RecordingDispatchProxy<IPlot>.Invocation setPlug = Assert.Single(plotProxy.GetInvocations(nameof(IPlot.SetPlug)));
        Assert.Equal((ushort)DefaultPlugId, setPlug.Arguments[0]);

        RecordingDispatchProxy<IPlot>.Invocation setFacing = Assert.Single(plotProxy.GetInvocations("set_PlugFacing"));
        Assert.Equal(HousingPlugFacing.West, setFacing.Arguments[0]);

        RecordingDispatchProxy<IPlugEntity>.Invocation initialise = Assert.Single(plugProxy.GetInvocations(nameof(IPlugEntity.Initialise)));
        Assert.Same(plotInfo, initialise.Arguments[0]);
        Assert.Same(plugEntry, initialise.Arguments[1]);

        RecordingDispatchProxy<ICharacterAchievementManager>.Invocation achievement =
            Assert.Single(achievementProxy.GetInvocations(nameof(ICharacterAchievementManager.CheckAchievements)));
        Assert.Equal(AchievementType.HousingPlugPlace, achievement.Arguments[1]);
        Assert.Empty(GetEncryptedMessages<ServerHousingResult>(sessionProxy));
    }

    [Fact]
    public void PlugUpdate_ContributionPayloadStillBlocksPlacement()
    {
        ResidenceMapInstance map = CreateResidenceMapInstance(
            CreateSharedConfiguration(),
            out RecordingDispatchProxy<IGameTableManager> gameTableProxy,
            out RecordingDispatchProxy<IEntityFactory> entityFactoryProxy);

        HousingPlugItemEntry plugEntry = CreateDefaultPlugEntry();
        plugEntry.HousingContributionInfoId00 = 12u;
        gameTableProxy.SetProperty(nameof(IGameTableManager.HousingPlugItem), CreateGameTable(plugEntry));

        IPlot plot = CreatePlot(plugEntry, out RecordingDispatchProxy<IPlot> plotProxy, out _);
        InitialiseResidence(map, plot, out _);
        IPlayer player = CreatePlayer(
            out RecordingDispatchProxy<ICharacterAchievementManager> achievementProxy,
            out RecordingDispatchProxy<IGameSession> sessionProxy);

        ClientHousingPlugUpdate update = CreatePlugUpdate();
        SetProperty(update.Contributions[0], nameof(ClientHousingPlugUpdate.ContributionRecord.ContributionPointRequirement), 25u);

        map.PlugUpdate(player, update);

        ServerHousingResult result = Assert.Single(GetEncryptedMessages<ServerHousingResult>(sessionProxy));
        Assert.Equal(HousingResult.Plug_CannotAfford, result.Result);
        Assert.Empty(plotProxy.GetInvocations(nameof(IPlot.SetPlug)));
        Assert.Empty(entityFactoryProxy.GetInvocations(nameof(IEntityFactory.CreateEntity)));
        Assert.Empty(achievementProxy.GetInvocations(nameof(ICharacterAchievementManager.CheckAchievements)));
    }

    [Fact]
    public void PlugUpdate_UpkeepContributionCostStillBlocksPlacement()
    {
        ResidenceMapInstance map = CreateResidenceMapInstance(
            CreateSharedConfiguration(),
            out RecordingDispatchProxy<IGameTableManager> gameTableProxy,
            out RecordingDispatchProxy<IEntityFactory> entityFactoryProxy);

        HousingPlugItemEntry plugEntry = CreateDefaultPlugEntry();
        plugEntry.HousingContributionInfoId00 = 12u;
        plugEntry.HousingContributionInfoIdUpkeepCost00 = 13u;
        gameTableProxy.SetProperty(nameof(IGameTableManager.HousingPlugItem), CreateGameTable(plugEntry));

        IPlot plot = CreatePlot(plugEntry, out RecordingDispatchProxy<IPlot> plotProxy, out _);
        InitialiseResidence(map, plot, out _);
        IPlayer player = CreatePlayer(
            out RecordingDispatchProxy<ICharacterAchievementManager> achievementProxy,
            out RecordingDispatchProxy<IGameSession> sessionProxy);

        map.PlugUpdate(player, CreatePlugUpdate());

        ServerHousingResult result = Assert.Single(GetEncryptedMessages<ServerHousingResult>(sessionProxy));
        Assert.Equal(HousingResult.Plug_ModifyFailed, result.Result);
        Assert.Empty(plotProxy.GetInvocations(nameof(IPlot.SetPlug)));
        Assert.Empty(entityFactoryProxy.GetInvocations(nameof(IEntityFactory.CreateEntity)));
        Assert.Empty(achievementProxy.GetInvocations(nameof(ICharacterAchievementManager.CheckAchievements)));
    }

    private static HousingPlugItemEntry CreateDefaultPlugEntry()
    {
        return new HousingPlugItemEntry
        {
            Id                = DefaultPlugId,
            HousingPlotTypeId = PlotType
        };
    }

    private static IPlot CreatePlot(
        HousingPlugItemEntry plugEntry,
        out RecordingDispatchProxy<IPlot> plotProxy,
        out HousingPlotInfoEntry plotInfo)
    {
        plotInfo = new HousingPlotInfoEntry
        {
            Id                       = PlotInfoId,
            PlotType                 = PlotType,
            HousingPlugItemIdDefault = DefaultPlugId
        };

        IPlot plot = RecordingDispatchProxy<IPlot>.Create(out RecordingDispatchProxy<IPlot> localPlotProxy);
        plotProxy = localPlotProxy;
        plotProxy.SetProperty(nameof(IPlot.Index), PlotIndex);
        plotProxy.SetProperty(nameof(IPlot.PlotInfoEntry), plotInfo);
        plotProxy.SetProperty(nameof(IPlot.PlugItemEntry), null);
        plotProxy.SetProperty(nameof(IPlot.PlugFacing), HousingPlugFacing.North);
        plotProxy.SetProperty(nameof(IPlot.BuildState), (byte)0);
        plotProxy.SetProperty(nameof(IPlot.PlugEntity), null);
        plotProxy.SetMethodHandler(nameof(IPlot.SetPlug), args =>
        {
            localPlotProxy.SetProperty(nameof(IPlot.PlugItemEntry), plugEntry);
            localPlotProxy.SetProperty(nameof(IPlot.BuildState), (byte)4);
            return null;
        });

        return plot;
    }

    private static IResidence InitialiseResidence(
        ResidenceMapInstance map,
        IPlot plot,
        out RecordingDispatchProxy<IResidence> residenceProxy)
    {
        IResidence residence = RecordingDispatchProxy<IResidence>.Create(out residenceProxy);
        residenceProxy.SetProperty(nameof(IResidence.Id), ResidenceId);
        residenceProxy.SetMethodReturn(nameof(IResidence.GetChildren), Array.Empty<IResidenceChild>());
        residenceProxy.SetMethodReturn(nameof(IResidence.GetPlots), new[] { plot });
        residenceProxy.SetMethodReturn(nameof(IResidence.GetPlot), plot);
        residenceProxy.SetMethodReturn(nameof(IResidence.CanModifyResidence), true);
        map.Initialise(residence);
        return residence;
    }

    private static IPlayer CreatePlayer(
        out RecordingDispatchProxy<ICharacterAchievementManager> achievementProxy,
        out RecordingDispatchProxy<IGameSession> sessionProxy)
    {
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out RecordingDispatchProxy<IPlayer> playerProxy);
        ICharacterAchievementManager achievementManager = RecordingDispatchProxy<ICharacterAchievementManager>.Create(out achievementProxy);
        IGameSession session = RecordingDispatchProxy<IGameSession>.Create(out sessionProxy);

        playerProxy.SetProperty(nameof(IPlayer.AchievementManager), achievementManager);
        playerProxy.SetProperty(nameof(IPlayer.Session), session);
        playerProxy.SetProperty(nameof(IPlayer.Name), "PlugTester");
        return player;
    }

    private static ClientHousingPlugUpdate CreatePlugUpdate()
    {
        var update = new ClientHousingPlugUpdate
        {
            HousingPlotInfoId = PlotInfoId,
            HousingPlugItemId = DefaultPlugId,
            PlugFacing        = HousingPlugFacing.West,
            Operation         = ClientHousingPlugUpdate.PlugUpdateOperation.PlaceOrRotate
        };
        update.Identity.RealmId = RealmId;
        update.Identity.Id = ResidenceId;

        for (int i = 0; i < ClientHousingPlugUpdate.ContributionRecordCount; i++)
            update.Contributions.Add(new ClientHousingPlugUpdate.ContributionRecord());

        return update;
    }

    private static ResidenceMapInstance CreateResidenceMapInstance(
        ISharedConfiguration sharedConfiguration,
        out RecordingDispatchProxy<IGameTableManager> gameTableProxy,
        out RecordingDispatchProxy<IEntityFactory> entityFactoryProxy)
    {
        IEntityFactory entityFactory = RecordingDispatchProxy<IEntityFactory>.Create(out entityFactoryProxy);
        IPublicEventManager publicEventManager = RecordingDispatchProxy<IPublicEventManager>.Create(out _);
        IMapLockManager mapLockManager = RecordingDispatchProxy<IMapLockManager>.Create(out _);
        IGlobalResidenceManager globalResidenceManager = RecordingDispatchProxy<IGlobalResidenceManager>.Create(out _);
        IGameTableManager gameTableManager = RecordingDispatchProxy<IGameTableManager>.Create(out gameTableProxy);
        IRealmContext realmContext = RecordingDispatchProxy<IRealmContext>.Create(out RecordingDispatchProxy<IRealmContext> realmProxy);
        IScriptManager scriptManager = RecordingDispatchProxy<IScriptManager>.Create(out _);

        realmProxy.SetProperty(nameof(IRealmContext.RealmId), RealmId);

        return new ResidenceMapInstance(
            entityFactory,
            publicEventManager,
            mapLockManager,
            globalResidenceManager,
            gameTableManager,
            realmContext,
            scriptManager,
            sharedConfiguration: sharedConfiguration);
    }

    private static ISharedConfiguration CreateSharedConfiguration()
    {
        var configuration = new SharedConfiguration(new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string>
            {
                ["Realm:Map:GridUnloadTimer"] = "600",
                ["Realm:Map:InstancePlayerLimit"] = "100"
            })
            .Build());
        configuration.Initialise<TestConfiguration>();

        return configuration;
    }

    private static IReadOnlyList<T> GetEncryptedMessages<T>(RecordingDispatchProxy<IGameSession> sessionProxy) where T : class, IWritable
    {
        return sessionProxy.GetInvocations(nameof(IGameSession.EnqueueMessageEncrypted))
            .Select(invocation => invocation.Arguments[0])
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
        FieldInfo idField = typeof(T).GetField("Id")!;
        return (uint)idField.GetValue(entry)!;
    }

    private static void SetProperty(object instance, string propertyName, object value)
    {
        PropertyInfo property = instance.GetType().GetProperty(propertyName)!;
        property.GetSetMethod(true)!.Invoke(instance, [value]);
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

    private sealed class TestConfiguration
    {
        public RealmConfig Realm { get; set; }
    }
}
