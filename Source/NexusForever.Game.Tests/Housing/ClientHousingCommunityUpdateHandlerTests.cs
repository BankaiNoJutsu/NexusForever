using System.Numerics;
using Microsoft.Extensions.DependencyInjection;
using NexusForever.Game.Abstract;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Guild;
using NexusForever.Game.Abstract.Housing;
using NexusForever.Game.Abstract.Map;
using NexusForever.Game.Abstract.Map.Instance;
using NexusForever.Game.Abstract.Map.Lock;
using NexusForever.Game.Map.Lock;
using NexusForever.Game.Static.Guild;
using NexusForever.Game.Static.Housing;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.GameTable.Model;
using NexusForever.Network;
using NexusForever.Network.Session;
using NexusForever.Network.World.Message.Model;
using NexusForever.Shared;
using NexusForever.WorldServer.Network;
using NexusForever.WorldServer.Network.Message.Handler.Housing;

namespace NexusForever.Game.Tests.Housing;

[Collection(LegacyServiceProviderCollection.Name)]
public class ClientHousingCommunityUpdateHandlerTests
{
    private const ushort RealmId = 7;
    private const ulong PlayerCharacterId = 1001ul;
    private const ulong PlayerResidenceId = 2002ul;
    private const ulong CommunityResidenceId = 3003ul;

    [Fact]
    public void PlacementHandler_AfterSuccessfulMove_SendsCommunityPlacementDelta()
    {
        ClientHousingCommunityPlacementHandler handler = CreatePlacementHandler(
            out RecordingDispatchProxy<IGlobalResidenceManager> globalResidenceManagerProxy,
            out _);

        IWorldSession session = CreateCommunitySession(
            out IResidence playerResidence,
            out RecordingDispatchProxy<IResidence> playerResidenceProxy,
            out IResidence communityResidence,
            out RecordingDispatchProxy<IResidence> communityResidenceProxy,
            out _,
            out RecordingDispatchProxy<IWorldSession> sessionProxy);

        IResidenceEntrance entrance = CreateEntrance();
        globalResidenceManagerProxy.SetMethodReturn(nameof(IGlobalResidenceManager.GetResidenceEntrance), entrance);

        using LegacyProviderScope providerScope = UseMapLockProvider();

        handler.HandleMessage(session, CreatePlacementRequest(2u));

        Assert.Equal(PropertyInfoId.CommunityResidence3, playerResidence.PropertyInfoId);

        RecordingDispatchProxy<IResidence>.Invocation addChild =
            Assert.Single(communityResidenceProxy.GetInvocations(nameof(IResidence.AddChild)));
        Assert.Same(playerResidence, addChild.Arguments[0]);
        Assert.Equal(true, addChild.Arguments[1]);

        ServerHousingCommunityPlacement update =
            Assert.Single(GetEncryptedMessages(sessionProxy).OfType<ServerHousingCommunityPlacement>());
        Assert.Equal(RealmId, update.TargetResidence.RealmId);
        Assert.Equal(CommunityResidenceId, update.TargetResidence.ResidenceId);
        Assert.Equal(PlayerResidenceId, update.PlacedResidenceId);
        Assert.Equal(2u, update.PropertyIndex);
    }

    [Fact]
    public void PrivacyHandler_PrivateRequest_SendsCommunityPrivacyDelta()
    {
        ClientHousingCommunityPrivacyLevelHandler handler = CreatePrivacyHandler(
            out RecordingDispatchProxy<IGlobalResidenceManager> globalResidenceManagerProxy,
            out _);

        IWorldSession session = CreateCommunitySession(
            out _,
            out _,
            out _,
            out _,
            out RecordingDispatchProxy<ICommunity> communityProxy,
            out RecordingDispatchProxy<IWorldSession> sessionProxy);

        handler.HandleMessage(session, CreatePrivacyRequest(CommunityPrivacyLevel.Private));

        Assert.Single(globalResidenceManagerProxy.GetInvocations(nameof(IGlobalResidenceManager.DeregisterCommunityVists)));
        Assert.Empty(globalResidenceManagerProxy.GetInvocations(nameof(IGlobalResidenceManager.RegisterCommunityVisits)));

        RecordingDispatchProxy<ICommunity>.Invocation setPrivate =
            Assert.Single(communityProxy.GetInvocations(nameof(ICommunity.SetCommunityPrivate)));
        Assert.Equal(true, setPrivate.Arguments[0]);

        ServerHousingCommunityPrivacyLevelUpdate update =
            Assert.Single(GetEncryptedMessages(sessionProxy).OfType<ServerHousingCommunityPrivacyLevelUpdate>());
        Assert.Equal(RealmId, update.TargetResidence.RealmId);
        Assert.Equal(CommunityResidenceId, update.TargetResidence.ResidenceId);
        Assert.Equal(ServerHousingCommunityPrivacyLevelUpdate.CommunityEntryType, update.EntryType);
        Assert.Equal(ServerHousingCommunityPrivacyLevelUpdate.PrivateFlag, update.Flags);
        Assert.Equal(ServerHousingCommunityPrivacyLevelUpdate.PrivateLuaValue, update.PrivacyLevel);
    }

    private static ClientHousingCommunityPlacementHandler CreatePlacementHandler(
        out RecordingDispatchProxy<IGlobalResidenceManager> globalResidenceManagerProxy,
        out RecordingDispatchProxy<IRealmContext> realmContextProxy)
    {
        IGlobalResidenceManager globalResidenceManager = RecordingDispatchProxy<IGlobalResidenceManager>.Create(out globalResidenceManagerProxy);
        IRealmContext realmContext = CreateRealmContext(out realmContextProxy);
        return new ClientHousingCommunityPlacementHandler(globalResidenceManager, realmContext);
    }

    private static ClientHousingCommunityPrivacyLevelHandler CreatePrivacyHandler(
        out RecordingDispatchProxy<IGlobalResidenceManager> globalResidenceManagerProxy,
        out RecordingDispatchProxy<IRealmContext> realmContextProxy)
    {
        IGlobalResidenceManager globalResidenceManager = RecordingDispatchProxy<IGlobalResidenceManager>.Create(out globalResidenceManagerProxy);
        IRealmContext realmContext = CreateRealmContext(out realmContextProxy);
        return new ClientHousingCommunityPrivacyLevelHandler(globalResidenceManager, realmContext);
    }

    private static IWorldSession CreateCommunitySession(
        out IResidence playerResidence,
        out RecordingDispatchProxy<IResidence> playerResidenceProxy,
        out IResidence communityResidence,
        out RecordingDispatchProxy<IResidence> communityResidenceProxy,
        out RecordingDispatchProxy<ICommunity> communityProxy,
        out RecordingDispatchProxy<IWorldSession> sessionProxy)
    {
        IWorldSession session = RecordingDispatchProxy<IWorldSession>.Create(out sessionProxy);
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out RecordingDispatchProxy<IPlayer> playerProxy);
        IResidenceMapInstance map = RecordingDispatchProxy<IResidenceMapInstance>.Create(out _);
        IGuildManager guildManager = RecordingDispatchProxy<IGuildManager>.Create(out RecordingDispatchProxy<IGuildManager> guildManagerProxy);
        ICommunity community = RecordingDispatchProxy<ICommunity>.Create(out communityProxy);
        IResidenceManager residenceManager = RecordingDispatchProxy<IResidenceManager>.Create(out RecordingDispatchProxy<IResidenceManager> residenceManagerProxy);
        IGuildMember member = RecordingDispatchProxy<IGuildMember>.Create(out RecordingDispatchProxy<IGuildMember> memberProxy);
        IGuildRank rank = RecordingDispatchProxy<IGuildRank>.Create(out RecordingDispatchProxy<IGuildRank> rankProxy);

        playerResidence = CreateResidence(PlayerResidenceId, out playerResidenceProxy);
        communityResidence = CreateResidence(CommunityResidenceId, out communityResidenceProxy);

        sessionProxy.SetProperty(nameof(IWorldSession.Player), player);
        playerProxy.SetProperty(nameof(IPlayer.CharacterId), PlayerCharacterId);
        playerProxy.SetProperty(nameof(IPlayer.Name), "Tester");
        playerProxy.SetProperty(nameof(IPlayer.Map), map);
        playerProxy.SetProperty(nameof(IPlayer.GuildManager), guildManager);
        playerProxy.SetProperty(nameof(IPlayer.ResidenceManager), residenceManager);

        guildManagerProxy.SetMethodReturn(nameof(IGuildManager.GetGuild), community);
        communityProxy.SetProperty(nameof(ICommunity.Residence), communityResidence);
        communityProxy.SetMethodReturn(nameof(ICommunity.GetMember), member);
        residenceManagerProxy.SetProperty(nameof(IResidenceManager.Residence), playerResidence);
        memberProxy.SetProperty(nameof(IGuildMember.Rank), rank);
        rankProxy.SetMethodReturn(nameof(IGuildRank.HasPermission), true);

        return session;
    }

    private static IResidence CreateResidence(ulong id, out RecordingDispatchProxy<IResidence> proxy)
    {
        IResidence residence = RecordingDispatchProxy<IResidence>.Create(out proxy);
        proxy.SetProperty(nameof(IResidence.Id), id);
        return residence;
    }

    private static IRealmContext CreateRealmContext(out RecordingDispatchProxy<IRealmContext> proxy)
    {
        IRealmContext realmContext = RecordingDispatchProxy<IRealmContext>.Create(out proxy);
        proxy.SetProperty(nameof(IRealmContext.RealmId), RealmId);
        return realmContext;
    }

    private static IResidenceEntrance CreateEntrance()
    {
        IResidenceEntrance entrance = RecordingDispatchProxy<IResidenceEntrance>.Create(out RecordingDispatchProxy<IResidenceEntrance> proxy);
        proxy.SetProperty(nameof(IResidenceEntrance.Entry), new WorldEntry { Id = 42u });
        proxy.SetProperty(nameof(IResidenceEntrance.Position), new Vector3(1f, 2f, 3f));
        proxy.SetProperty(nameof(IResidenceEntrance.Rotation), Quaternion.Identity);
        return entrance;
    }

    private static ClientHousingCommunityPlacement CreatePlacementRequest(uint propertyIndex)
    {
        var packet = new ClientHousingCommunityPlacement();
        packet.TargetResidence.RealmId     = RealmId;
        packet.TargetResidence.ResidenceId = CommunityResidenceId;
        SetPacketProperty(packet, nameof(ClientHousingCommunityPlacement.PropertyIndex), propertyIndex);
        return packet;
    }

    private static ClientHousingCommunityPrivacyLevel CreatePrivacyRequest(CommunityPrivacyLevel privacyLevel)
    {
        var packet = new ClientHousingCommunityPrivacyLevel();
        packet.TargetResidence.RealmId     = RealmId;
        packet.TargetResidence.ResidenceId = CommunityResidenceId;
        SetPacketProperty(packet, nameof(ClientHousingCommunityPrivacyLevel.PrivacyLevel), privacyLevel);
        return packet;
    }

    private static LegacyProviderScope UseMapLockProvider()
    {
        IMapLockManager mapLockManager = RecordingDispatchProxy<IMapLockManager>.Create(out RecordingDispatchProxy<IMapLockManager> mapLockManagerProxy);
        IResidenceMapLock mapLock = RecordingDispatchProxy<IResidenceMapLock>.Create(out _);
        mapLockManagerProxy.SetMethodReturn(nameof(IMapLockManager.GetResidenceLock), mapLock);

        ServiceProvider provider = new ServiceCollection()
            .AddSingleton(mapLockManager)
            .BuildServiceProvider();

        return new LegacyProviderScope(provider);
    }

    private static void SetPacketProperty(object packet, string propertyName, object value)
    {
        var property = packet.GetType().GetProperty(propertyName);
        property?.GetSetMethod(true)?.Invoke(packet, [value]);
    }

    private static IEnumerable<object> GetEncryptedMessages(RecordingDispatchProxy<IWorldSession> sessionProxy)
    {
        return sessionProxy.GetInvocations(nameof(IGameSession.EnqueueMessageEncrypted))
            .Select(invocation => invocation.Arguments[0]);
    }

    private sealed class LegacyProviderScope : IDisposable
    {
        private readonly IServiceProvider previous;
        private readonly ServiceProvider provider;

        public LegacyProviderScope(ServiceProvider provider)
        {
            previous = LegacyServiceProvider.Provider;
            this.provider = provider;
            LegacyServiceProvider.Provider = provider;
        }

        public void Dispose()
        {
            LegacyServiceProvider.Provider = previous;
            provider.Dispose();
        }
    }
}
