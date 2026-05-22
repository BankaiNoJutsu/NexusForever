using Microsoft.Extensions.Logging.Abstractions;
using NexusForever.Game.Abstract.Character;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Housing;
using NexusForever.Game.Abstract.Map.Instance;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.Network.Session;
using NexusForever.Network.World.Message.Model;
using NexusForever.Network.World.Message.Model.Shared;
using NexusForever.Network.World.Message.Static;
using NexusForever.WorldServer.Network;
using NexusForever.WorldServer.Network.Message.Handler.Housing;

namespace NexusForever.Game.Tests.Housing;

public class ClientHousingNeighborHandlerTests
{
    private const ushort RealmId = 7;
    private const ulong InviterCharacterId = 1001ul;
    private const ulong InviteeCharacterId = 2002ul;
    private const ulong InviterResidenceId = 3003ul;
    private const ulong InviteeResidenceId = 4004ul;

    [Fact]
    public void InviteHandler_UnknownTarget_SendsDoesNotExist()
    {
        ClientHousingNeighborInviteHandler handler = CreateInviteHandler(out RecordingDispatchProxy<ICharacterManager> characterManagerProxy, out _, out _);
        IResidence inviterResidence = CreateResidence(InviterResidenceId, out _);
        TestResidenceManager inviterResidenceManager = new(inviterResidence);
        IWorldSession session = CreateWorldSession("Inviter", InviterCharacterId, inviterResidenceManager, out _, out _, out RecordingDispatchProxy<IWorldSession> sessionProxy);

        characterManagerProxy.SetMethodReturn(nameof(ICharacterManager.GetCharacterIdByName), (ulong?)0ul);

        handler.HandleMessage(session, CreateInviteRequest("MissingPlayer"));

        ServerHousingResult result = Assert.Single(GetEncryptedMessages(sessionProxy).OfType<ServerHousingResult>());
        Assert.Equal(HousingResult.Neighbor_PlayerDoesntExist, result.Result);
    }

    [Fact]
    public void InviteHandler_OnlineHomeowner_QueuesInviteAndSendsPrompt()
    {
        ClientHousingNeighborInviteHandler handler = CreateInviteHandler(out RecordingDispatchProxy<ICharacterManager> characterManagerProxy, out RecordingDispatchProxy<IPlayerManager> playerManagerProxy, out RecordingDispatchProxy<IGlobalResidenceManager> residenceManagerProxy);

        IResidence inviterResidence = CreateResidence(InviterResidenceId, out _);
        TestResidenceManager inviterResidenceManager = new(inviterResidence);
        IWorldSession session = CreateWorldSession("Inviter", InviterCharacterId, inviterResidenceManager, out _, out _, out RecordingDispatchProxy<IWorldSession> sessionProxy);

        IResidence inviteeResidence = CreateResidence(InviteeResidenceId, out _);
        TestResidenceManager inviteeResidenceManager = new(inviteeResidence);
        IWorldSession inviteeSession = CreateWorldSession("Invitee", InviteeCharacterId, inviteeResidenceManager, out IPlayer inviteePlayer, out _, out RecordingDispatchProxy<IWorldSession> inviteeSessionProxy);

        characterManagerProxy.SetMethodReturn(nameof(ICharacterManager.GetCharacterIdByName), (ulong?)InviteeCharacterId);
        playerManagerProxy.SetMethodReturn(nameof(IPlayerManager.GetPlayer), inviteePlayer);
        residenceManagerProxy.SetMethodReturn(nameof(IGlobalResidenceManager.GetResidenceByOwner), inviteeResidence);

        handler.HandleMessage(session, CreateInviteRequest("Invitee"));

        ServerHousingResult inviterResult = Assert.Single(GetEncryptedMessages(sessionProxy).OfType<ServerHousingResult>());
        Assert.Equal(HousingResult.Neighbor_Success, inviterResult.Result);
        Assert.Equal(InviteeResidenceId, inviterResult.ResidenceId);

        ServerHousingNeighborInvitePrompt inviteePrompt = Assert.Single(GetEncryptedMessages(inviteeSessionProxy).OfType<ServerHousingNeighborInvitePrompt>());
        Assert.Equal(InviterResidenceId, inviteePrompt.TargetResidence.ResidenceId);
        Assert.Equal("Inviter", inviteePrompt.PlayerName);
        Assert.NotNull(inviteeResidenceManager.GetPendingNeighborInvite());
        Assert.Equal(InviterCharacterId, inviteeResidenceManager.GetPendingNeighborInvite().InviterCharacterId);
    }

    [Fact]
    public void InviteHandler_WithoutResidence_CreatesResidenceThroughManager()
    {
        ClientHousingNeighborInviteHandler handler = CreateInviteHandler(out RecordingDispatchProxy<ICharacterManager> characterManagerProxy, out RecordingDispatchProxy<IPlayerManager> playerManagerProxy, out RecordingDispatchProxy<IGlobalResidenceManager> residenceManagerProxy);

        IResidence createdInviterResidence = CreateResidence(InviterResidenceId, out _);
        TestResidenceManager inviterResidenceManager = new(null, createdInviterResidence);
        IWorldSession session = CreateWorldSession("Inviter", InviterCharacterId, inviterResidenceManager, out _, out _, out RecordingDispatchProxy<IWorldSession> sessionProxy);

        IResidence inviteeResidence = CreateResidence(InviteeResidenceId, out _);
        TestResidenceManager inviteeResidenceManager = new(inviteeResidence);
        IWorldSession inviteeSession = CreateWorldSession("Invitee", InviteeCharacterId, inviteeResidenceManager, out IPlayer inviteePlayer, out _, out RecordingDispatchProxy<IWorldSession> inviteeSessionProxy);

        characterManagerProxy.SetMethodReturn(nameof(ICharacterManager.GetCharacterIdByName), (ulong?)InviteeCharacterId);
        playerManagerProxy.SetMethodReturn(nameof(IPlayerManager.GetPlayer), inviteePlayer);
        residenceManagerProxy.SetMethodReturn(nameof(IGlobalResidenceManager.GetResidenceByOwner), inviteeResidence);

        handler.HandleMessage(session, CreateInviteRequest("Invitee"));

        Assert.Same(createdInviterResidence, inviterResidenceManager.Residence);
        Assert.Equal(1, inviterResidenceManager.GetOrCreateResidenceCalls);

        ServerHousingResult inviterResult = Assert.Single(GetEncryptedMessages(sessionProxy).OfType<ServerHousingResult>());
        Assert.Equal(InviteeResidenceId, inviterResult.ResidenceId);

        ServerHousingNeighborInvitePrompt inviteePrompt = Assert.Single(GetEncryptedMessages(inviteeSessionProxy).OfType<ServerHousingNeighborInvitePrompt>());
        Assert.Equal(InviterResidenceId, inviteePrompt.TargetResidence.ResidenceId);
        Assert.Equal("Inviter", inviteePrompt.PlayerName);
    }

    [Fact]
    public void InviteResponseHandler_NoPendingInvite_SendsNoPendingInvite()
    {
        ClientHousingNeighborInviteResponseHandler handler = CreateInviteResponseHandler(out _, out _);
        IWorldSession session = CreateWorldSession("Invitee", InviteeCharacterId, new TestResidenceManager(null), out _, out _, out RecordingDispatchProxy<IWorldSession> sessionProxy);

        handler.HandleMessage(session, CreateInviteResponse(true));

        ServerHousingResult result = Assert.Single(GetEncryptedMessages(sessionProxy).OfType<ServerHousingResult>());
        Assert.Equal(HousingResult.Neighbor_NoPendingInvite, result.Result);
    }

    [Fact]
    public void InviteResponseHandler_Accepted_AddsNeighborAndNotifiesInviter()
    {
        ClientHousingNeighborInviteResponseHandler handler = CreateInviteResponseHandler(out RecordingDispatchProxy<IPlayerManager> playerManagerProxy, out RecordingDispatchProxy<IGlobalResidenceManager> residenceManagerProxy);

        IResidence inviteeResidence = CreateResidence(InviteeResidenceId, out _);
        TestResidenceManager inviteeResidenceManager = new(inviteeResidence);
        inviteeResidenceManager.TryQueueNeighborInvite(new ResidenceNeighborInviteInfo
        {
            InviterCharacterId = InviterCharacterId,
            InviterResidenceId = InviterResidenceId,
            InviterName = "Inviter"
        });

        IWorldSession session = CreateWorldSession("Invitee", InviteeCharacterId, inviteeResidenceManager, out _, out _, out RecordingDispatchProxy<IWorldSession> sessionProxy);

        IResidence inviterResidence = CreateResidence(InviterResidenceId, out RecordingDispatchProxy<IResidence> inviterResidenceProxy);
        inviterResidenceProxy.SetMethodReturn(nameof(IResidence.AddNeighbor), true);
        residenceManagerProxy.SetMethodReturn(nameof(IGlobalResidenceManager.GetResidence), inviterResidence);

        IWorldSession inviterSession = CreateWorldSession("Inviter", InviterCharacterId, new TestResidenceManager(inviterResidence), out IPlayer inviterPlayer, out _, out RecordingDispatchProxy<IWorldSession> inviterSessionProxy);
        playerManagerProxy.SetMethodReturn(nameof(IPlayerManager.GetPlayer), inviterPlayer);

        handler.HandleMessage(session, CreateInviteResponse(true));

        RecordingDispatchProxy<IResidence>.Invocation addNeighbor = Assert.Single(inviterResidenceProxy.GetInvocations(nameof(IResidence.AddNeighbor)));
        Assert.Equal(InviteeCharacterId, addNeighbor.Arguments[0]);
        Assert.Null(inviteeResidenceManager.GetPendingNeighborInvite());

        ServerHousingResult inviteeResult = Assert.Single(GetEncryptedMessages(sessionProxy).OfType<ServerHousingResult>());
        Assert.Equal(HousingResult.Neighbor_Success, inviteeResult.Result);

        ServerHousingNeighborInviteResult inviterResult = Assert.Single(GetEncryptedMessages(inviterSessionProxy).OfType<ServerHousingNeighborInviteResult>());
        Assert.Equal(HousingResult.Neighbor_RequestAccepted, inviterResult.Result);
        Assert.Equal(InviteeCharacterId, inviterResult.Neighbor.CharacterId);
        Assert.Equal(0ul, inviterResult.Neighbor.Reserved);
        Assert.Equal(InviteeResidenceId, inviterResult.Neighbor.TargetResidence.ResidenceId);

        ServerHousingNeighborUpdate inviterUpdate = Assert.Single(GetEncryptedMessages(inviterSessionProxy).OfType<ServerHousingNeighborUpdate>());
        Assert.Equal(HousingNeighborUpdateType.Added, inviterUpdate.UpdateType);
        Assert.Equal(InviteeCharacterId, inviterUpdate.Neighbor.CharacterId);
        Assert.Equal(0ul, inviterUpdate.Neighbor.Reserved);
        Assert.Equal(InviteeResidenceId, inviterUpdate.Neighbor.TargetResidence.ResidenceId);
    }

    [Fact]
    public void EvictHandler_RemovesNeighborAndSendsSuccess()
    {
        ClientHousingNeighborEvictHandler handler = CreateEvictHandler(out RecordingDispatchProxy<ICharacterManager> characterManagerProxy, out _);
        IResidence inviterResidence = CreateResidence(InviterResidenceId, out RecordingDispatchProxy<IResidence> inviterResidenceProxy);
        inviterResidenceProxy.SetMethodReturn(nameof(IResidence.RemoveNeighbor), true);
        IWorldSession session = CreateWorldSession("Inviter", InviterCharacterId, new TestResidenceManager(inviterResidence), out _, out _, out RecordingDispatchProxy<IWorldSession> sessionProxy);

        characterManagerProxy.SetMethodReturn(nameof(ICharacterManager.GetCharacterIdByName), (ulong?)InviteeCharacterId);

        handler.HandleMessage(session, CreateEvictRequest("Invitee"));

        RecordingDispatchProxy<IResidence>.Invocation removeNeighbor = Assert.Single(inviterResidenceProxy.GetInvocations(nameof(IResidence.RemoveNeighbor)));
        Assert.Equal(InviteeCharacterId, removeNeighbor.Arguments[0]);

        ServerHousingResult result = Assert.Single(GetEncryptedMessages(sessionProxy).OfType<ServerHousingResult>());
        Assert.Equal(HousingResult.Neighbor_Success, result.Result);

        ServerHousingNeighborUpdate update = Assert.Single(GetEncryptedMessages(sessionProxy).OfType<ServerHousingNeighborUpdate>());
        Assert.Equal(HousingNeighborUpdateType.Removed, update.UpdateType);
        Assert.Equal(InviteeCharacterId, update.Neighbor.CharacterId);
        Assert.Equal(0ul, update.Neighbor.Reserved);
        Assert.Equal(InviteeResidenceId, update.Neighbor.TargetResidence.ResidenceId);
    }

    [Fact]
    public void SetPermissionHandler_UpdatesNeighborPermissionAndSendsSuccess()
    {
        ClientHousingNeighborSetPermissionHandler handler = CreatePermissionHandler(out RecordingDispatchProxy<ICharacterManager> characterManagerProxy, out _);
        IResidence inviterResidence = CreateResidence(InviterResidenceId, out RecordingDispatchProxy<IResidence> inviterResidenceProxy);
        inviterResidenceProxy.SetMethodReturn(nameof(IResidence.TrySetNeighborPermission), true);
        IWorldSession session = CreateWorldSession("Inviter", InviterCharacterId, new TestResidenceManager(inviterResidence), out _, out _, out RecordingDispatchProxy<IWorldSession> sessionProxy);

        characterManagerProxy.SetMethodReturn(nameof(ICharacterManager.GetCharacterIdByName), (ulong?)InviteeCharacterId);

        handler.HandleMessage(session, CreatePermissionRequest("Invitee", 2u));

        RecordingDispatchProxy<IResidence>.Invocation setPermission = Assert.Single(inviterResidenceProxy.GetInvocations(nameof(IResidence.TrySetNeighborPermission)));
        Assert.Equal(InviteeCharacterId, setPermission.Arguments[0]);
        Assert.Equal((byte)2, setPermission.Arguments[1]);

        ServerHousingResult result = Assert.Single(GetEncryptedMessages(sessionProxy).OfType<ServerHousingResult>());
        Assert.Equal(HousingResult.Neighbor_Success, result.Result);

        ServerHousingNeighborUpdate update = Assert.Single(GetEncryptedMessages(sessionProxy).OfType<ServerHousingNeighborUpdate>());
        Assert.Equal(HousingNeighborUpdateType.PermissionChanged, update.UpdateType);
        Assert.Equal(InviteeCharacterId, update.Neighbor.CharacterId);
        Assert.Equal(0ul, update.Neighbor.Reserved);
        Assert.Equal(2u, update.Neighbor.Permission);
    }

    [Fact]
    public void InteriorWallpaperHandler_OnResidenceMap_DelegatesToMap()
    {
        var handler = new ClientHousingInteriorWallpaperUpdateHandler();
        IWorldSession session = RecordingDispatchProxy<IWorldSession>.Create(out RecordingDispatchProxy<IWorldSession> sessionProxy);
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out RecordingDispatchProxy<IPlayer> playerProxy);
        IResidenceMapInstance map = RecordingDispatchProxy<IResidenceMapInstance>.Create(out RecordingDispatchProxy<IResidenceMapInstance> mapProxy);
        var update = new ClientHousingInteriorWallpaperUpdate();

        playerProxy.SetProperty(nameof(IPlayer.Map), map);
        sessionProxy.SetProperty(nameof(IWorldSession.Player), player);

        handler.HandleMessage(session, update);

        RecordingDispatchProxy<IResidenceMapInstance>.Invocation invocation =
            Assert.Single(mapProxy.GetInvocations(nameof(IResidenceMapInstance.InteriorWallpaperUpdate)));
        Assert.Same(player, invocation.Arguments[0]);
        Assert.Same(update, invocation.Arguments[1]);
    }

    private static ClientHousingNeighborInviteHandler CreateInviteHandler(
        out RecordingDispatchProxy<ICharacterManager> characterManagerProxy,
        out RecordingDispatchProxy<IPlayerManager> playerManagerProxy,
        out RecordingDispatchProxy<IGlobalResidenceManager> globalResidenceManagerProxy)
    {
        ICharacterManager characterManager = RecordingDispatchProxy<ICharacterManager>.Create(out characterManagerProxy);
        IPlayerManager playerManager = RecordingDispatchProxy<IPlayerManager>.Create(out playerManagerProxy);
        IGlobalResidenceManager globalResidenceManager = RecordingDispatchProxy<IGlobalResidenceManager>.Create(out globalResidenceManagerProxy);
        return new ClientHousingNeighborInviteHandler(
            NullLogger<ClientHousingNeighborInviteHandler>.Instance,
            characterManager,
            playerManager,
            globalResidenceManager);
    }

    private static ClientHousingNeighborInviteResponseHandler CreateInviteResponseHandler(
        out RecordingDispatchProxy<IPlayerManager> playerManagerProxy,
        out RecordingDispatchProxy<IGlobalResidenceManager> globalResidenceManagerProxy)
    {
        IPlayerManager playerManager = RecordingDispatchProxy<IPlayerManager>.Create(out playerManagerProxy);
        IGlobalResidenceManager globalResidenceManager = RecordingDispatchProxy<IGlobalResidenceManager>.Create(out globalResidenceManagerProxy);
        return new ClientHousingNeighborInviteResponseHandler(
            NullLogger<ClientHousingNeighborInviteResponseHandler>.Instance,
            playerManager,
            globalResidenceManager);
    }

    private static ClientHousingNeighborEvictHandler CreateEvictHandler(
        out RecordingDispatchProxy<ICharacterManager> characterManagerProxy,
        out RecordingDispatchProxy<IGlobalResidenceManager> globalResidenceManagerProxy)
    {
        ICharacterManager characterManager = RecordingDispatchProxy<ICharacterManager>.Create(out characterManagerProxy);
        IGlobalResidenceManager globalResidenceManager = RecordingDispatchProxy<IGlobalResidenceManager>.Create(out globalResidenceManagerProxy);
        return new ClientHousingNeighborEvictHandler(
            NullLogger<ClientHousingNeighborEvictHandler>.Instance,
            characterManager,
            globalResidenceManager);
    }

    private static ClientHousingNeighborSetPermissionHandler CreatePermissionHandler(
        out RecordingDispatchProxy<ICharacterManager> characterManagerProxy,
        out RecordingDispatchProxy<IGlobalResidenceManager> globalResidenceManagerProxy)
    {
        ICharacterManager characterManager = RecordingDispatchProxy<ICharacterManager>.Create(out characterManagerProxy);
        IGlobalResidenceManager globalResidenceManager = RecordingDispatchProxy<IGlobalResidenceManager>.Create(out globalResidenceManagerProxy);
        return new ClientHousingNeighborSetPermissionHandler(
            NullLogger<ClientHousingNeighborSetPermissionHandler>.Instance,
            characterManager,
            globalResidenceManager);
    }

    private static IWorldSession CreateWorldSession(
        string name,
        ulong characterId,
        IResidenceManager residenceManager,
        out IPlayer player,
        out RecordingDispatchProxy<IPlayer> playerProxy,
        out RecordingDispatchProxy<IWorldSession> sessionProxy)
    {
        IWorldSession session = RecordingDispatchProxy<IWorldSession>.Create(out sessionProxy);
        player = RecordingDispatchProxy<IPlayer>.Create(out playerProxy);

        sessionProxy.SetProperty(nameof(IWorldSession.Player), player);
        playerProxy.SetProperty(nameof(IPlayer.CharacterId), characterId);
        playerProxy.SetProperty(nameof(IPlayer.Name), name);
        playerProxy.SetProperty(nameof(IPlayer.Identity), new NexusForever.Game.Abstract.Identity { RealmId = RealmId, Id = characterId });
        playerProxy.SetProperty(nameof(IPlayer.ResidenceManager), residenceManager);
        playerProxy.SetProperty(nameof(IPlayer.Session), session);

        return session;
    }

    private static IResidence CreateResidence(ulong id, out RecordingDispatchProxy<IResidence> proxy)
    {
        IResidence residence = RecordingDispatchProxy<IResidence>.Create(out proxy);
        proxy.SetProperty(nameof(IResidence.Id), id);
        return residence;
    }

    private static ClientHousingNeighborInvite CreateInviteRequest(string targetName)
    {
        var packet = new ClientHousingNeighborInvite();
        SetPacketProperty(packet, nameof(ClientHousingNeighborInvite.TargetName), targetName);
        packet.TargetResidence.RealmId = RealmId;
        packet.TargetResidence.ResidenceId = InviteeResidenceId;
        return packet;
    }

    private static ClientHousingNeighborInviteResponse CreateInviteResponse(bool accepted)
    {
        var packet = new ClientHousingNeighborInviteResponse();
        SetPacketProperty(packet, nameof(ClientHousingNeighborInviteResponse.Accepted), accepted);
        return packet;
    }

    private static ClientHousingNeighborEvict CreateEvictRequest(string targetName)
    {
        var packet = new ClientHousingNeighborEvict();
        SetPacketProperty(packet, nameof(ClientHousingNeighborEvict.TargetName), targetName);
        packet.TargetResidence.RealmId = RealmId;
        packet.TargetResidence.ResidenceId = InviteeResidenceId;
        return packet;
    }

    private static ClientHousingNeighborSetPermission CreatePermissionRequest(string targetName, uint permission)
    {
        var packet = new ClientHousingNeighborSetPermission();
        SetPacketProperty(packet, nameof(ClientHousingNeighborSetPermission.TargetName), targetName);
        SetPacketProperty(packet, nameof(ClientHousingNeighborSetPermission.Permission), permission);
        packet.TargetResidence.RealmId = RealmId;
        packet.TargetResidence.ResidenceId = InviteeResidenceId;
        return packet;
    }

    private static void SetPacketProperty(object packet, string propertyName, object value)
    {
        var property = packet.GetType().GetProperty(propertyName);
        property?.GetSetMethod(true)?.Invoke(packet, [value]);
    }

    private static IEnumerable<object> GetEncryptedMessages<TSession>(RecordingDispatchProxy<TSession> sessionProxy)
        where TSession : class
    {
        return sessionProxy.GetInvocations(nameof(IGameSession.EnqueueMessageEncrypted))
            .Select(invocation => invocation.Arguments[0]);
    }

    private sealed class TestResidenceManager : IResidenceManager
    {
        public TestResidenceManager(IResidence residence, IResidence createdResidence = null)
        {
            Residence = residence;
            this.createdResidence = createdResidence;
        }

        public IResidence Residence { get; private set; }

        public int GetOrCreateResidenceCalls { get; private set; }

        private ResidenceNeighborInviteInfo pendingInvite;
        private readonly IResidence createdResidence;

        public void DecorCreate(NexusForever.GameTable.Model.HousingDecorInfoEntry entry, uint quantity = 1)
        {
        }

        public void SetResidencePrivacy(NexusForever.Game.Static.Housing.ResidencePrivacyLevel privacy)
        {
        }

        public IResidence GetOrCreateResidence()
        {
            GetOrCreateResidenceCalls++;
            Residence ??= createdResidence;
            return Residence;
        }

        public ResidenceNeighborInviteInfo GetPendingNeighborInvite()
        {
            return pendingInvite;
        }

        public bool TryQueueNeighborInvite(ResidenceNeighborInviteInfo invite)
        {
            if (pendingInvite != null)
                return false;

            pendingInvite = invite;
            return true;
        }

        public void ClearPendingNeighborInvite()
        {
            pendingInvite = null;
        }

        public void SendHousingBasics()
        {
        }

        public void SendHousingNeighbors()
        {
        }
    }
}
