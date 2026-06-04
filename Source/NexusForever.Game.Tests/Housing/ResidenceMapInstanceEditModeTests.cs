using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NexusForever.Game.Abstract;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Housing;
using NexusForever.Game.Abstract.Map.Lock;
using NexusForever.Game.Abstract.PublicEvent;
using NexusForever.Game.Configuration.Model;
using NexusForever.Game.Map.Instance;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.GameTable;
using NexusForever.Network;
using NexusForever.Script;
using NexusForever.Shared;
using NexusForever.Shared.Configuration;

namespace NexusForever.Game.Tests.Housing;

[Collection(LegacyServiceProviderCollection.Name)]
public class ResidenceMapInstanceEditModeTests
{
    private const ushort RealmId = 7;
    private const ulong ResidenceId = 1234ul;
    private const ulong CharacterId = 5678ul;

    [Fact]
    public void SetEditMode_EnableStoresLoadedResidence()
    {
        IServiceProvider previousProvider = LegacyServiceProvider.Provider;
        try
        {
            LegacyServiceProvider.Provider = BuildConfigurationProvider();
            ResidenceMapInstance map = CreateResidenceMapInstance();
            IResidence residence = InitialiseResidence(map, ResidenceId, canModify: true, out _);
            IPlayer player = CreatePlayer(CharacterId);

            map.SetEditMode(player, residence, enabled: true);

            Assert.True(map.TryGetEditModeResidence(player, out IResidence editResidence));
            Assert.Same(residence, editResidence);
        }
        finally
        {
            LegacyServiceProvider.Provider = previousProvider;
        }
    }

    [Fact]
    public void SetEditMode_DisableClearsStoredResidence()
    {
        IServiceProvider previousProvider = LegacyServiceProvider.Provider;
        try
        {
            LegacyServiceProvider.Provider = BuildConfigurationProvider();
            ResidenceMapInstance map = CreateResidenceMapInstance();
            IResidence residence = InitialiseResidence(map, ResidenceId, canModify: true, out _);
            IPlayer player = CreatePlayer(CharacterId);

            map.SetEditMode(player, residence, enabled: true);
            map.SetEditMode(player, residence, enabled: false);

            Assert.False(map.TryGetEditModeResidence(player, out _));
        }
        finally
        {
            LegacyServiceProvider.Provider = previousProvider;
        }
    }

    [Fact]
    public void SetEditMode_UnloadedResidenceThrowsInvalidPacket()
    {
        IServiceProvider previousProvider = LegacyServiceProvider.Provider;
        try
        {
            LegacyServiceProvider.Provider = BuildConfigurationProvider();
            ResidenceMapInstance map = CreateResidenceMapInstance();
            IResidence residence = CreateResidence(ResidenceId, canModify: true, out _);
            IPlayer player = CreatePlayer(CharacterId);

            Assert.Throws<InvalidPacketValueException>(() => map.SetEditMode(player, residence, enabled: true));
            Assert.False(map.TryGetEditModeResidence(player, out _));
        }
        finally
        {
            LegacyServiceProvider.Provider = previousProvider;
        }
    }

    [Fact]
    public void SetEditMode_WithoutModifyPermissionThrowsInvalidPacket()
    {
        IServiceProvider previousProvider = LegacyServiceProvider.Provider;
        try
        {
            LegacyServiceProvider.Provider = BuildConfigurationProvider();
            ResidenceMapInstance map = CreateResidenceMapInstance();
            IResidence residence = InitialiseResidence(map, ResidenceId, canModify: false, out _);
            IPlayer player = CreatePlayer(CharacterId);

            Assert.Throws<InvalidPacketValueException>(() => map.SetEditMode(player, residence, enabled: true));
            Assert.False(map.TryGetEditModeResidence(player, out _));
        }
        finally
        {
            LegacyServiceProvider.Provider = previousProvider;
        }
    }

    private static IServiceProvider BuildConfigurationProvider()
    {
        var configuration = new SharedConfiguration(new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string>
            {
                ["Realm:Map:GridUnloadTimer"] = "600",
                ["Realm:Map:InstancePlayerLimit"] = "100"
            })
            .Build());
        configuration.Initialise<TestConfiguration>();

        return new ServiceCollection()
            .AddSingleton(configuration)
            .BuildServiceProvider();
    }

    private static ResidenceMapInstance CreateResidenceMapInstance()
    {
        IEntityFactory entityFactory = RecordingDispatchProxy<IEntityFactory>.Create(out _);
        IPublicEventManager publicEventManager = RecordingDispatchProxy<IPublicEventManager>.Create(out _);
        IMapLockManager mapLockManager = RecordingDispatchProxy<IMapLockManager>.Create(out _);
        IGlobalResidenceManager globalResidenceManager = RecordingDispatchProxy<IGlobalResidenceManager>.Create(out _);
        IGameTableManager gameTableManager = RecordingDispatchProxy<IGameTableManager>.Create(out _);
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
            scriptManager);
    }

    private static IResidence InitialiseResidence(ResidenceMapInstance map, ulong residenceId, bool canModify, out RecordingDispatchProxy<IResidence> residenceProxy)
    {
        IResidence residence = CreateResidence(residenceId, canModify, out residenceProxy);
        map.Initialise(residence);
        return residence;
    }

    private static IResidence CreateResidence(ulong residenceId, bool canModify, out RecordingDispatchProxy<IResidence> residenceProxy)
    {
        IResidence residence = RecordingDispatchProxy<IResidence>.Create(out residenceProxy);
        residenceProxy.SetProperty(nameof(IResidence.Id), residenceId);
        residenceProxy.SetMethodReturn(nameof(IResidence.GetChildren), Array.Empty<IResidenceChild>());
        residenceProxy.SetMethodReturn(nameof(IResidence.GetPlots), Array.Empty<IPlot>());
        residenceProxy.SetMethodReturn(nameof(IResidence.CanModifyResidence), canModify);
        return residence;
    }

    private static IPlayer CreatePlayer(ulong characterId)
    {
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out RecordingDispatchProxy<IPlayer> playerProxy);
        playerProxy.SetProperty(nameof(IPlayer.CharacterId), characterId);
        return player;
    }

    private sealed class TestConfiguration
    {
        public RealmConfig Realm { get; set; }
    }
}
