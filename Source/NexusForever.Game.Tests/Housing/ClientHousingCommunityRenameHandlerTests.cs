using Microsoft.Extensions.Logging.Abstractions;
using NexusForever.Game.Abstract;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Guild;
using NexusForever.Game.Abstract.Map;
using NexusForever.Game.Abstract.Map.Instance;
using NexusForever.Game.Static.Guild;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.GameTable;
using NexusForever.GameTable.Text.Filter;
using NexusForever.Network;
using NexusForever.Network.Message;
using NexusForever.Network.Session;
using NexusForever.Network.World.Message.Model;
using NexusForever.Network.World.Message.Static;
using NexusForever.WorldServer.Network;
using NexusForever.WorldServer.Network.Message.Handler.Housing;
using NetworkIdentity = NexusForever.Network.World.Message.Model.Shared.Identity;

namespace NexusForever.Game.Tests.Housing;

public class ClientHousingCommunityRenameHandlerTests
{
    [Fact]
    public void HandleMessage_WhenPlayerIsNotLeader_SendsInvalidPermissions()
    {
        ClientHousingCommunityRenameHandler handler = CreateHandler();
        IWorldSession session = CreateSession(
            playerCharacterId: 100ul,
            communityLeaderId: 200ul,
            out RecordingDispatchProxy<IWorldSession> sessionProxy,
            out RecordingDispatchProxy<ICommunity> communityProxy);

        handler.HandleMessage(session, ReadRequest("New Community Name"));

        ServerHousingCommunityRename result = Assert.Single(GetEncryptedMessages(sessionProxy).OfType<ServerHousingCommunityRename>());
        Assert.Equal(HousingResult.InvalidPermissions, result.Result);
        Assert.Equal(0x5566778899AABBCCul, result.TargetGuild.Id);
        Assert.Empty(communityProxy.GetInvocations(nameof(ICommunity.RenameGuild)));
    }

    private static ClientHousingCommunityRenameHandler CreateHandler()
    {
        ITextFilterManager textFilterManager = RecordingDispatchProxy<ITextFilterManager>.Create(out _);
        IGameTableManager gameTableManager = RecordingDispatchProxy<IGameTableManager>.Create(out _);
        IRealmContext realmContext = RecordingDispatchProxy<IRealmContext>.Create(out _);
        return new ClientHousingCommunityRenameHandler(textFilterManager, gameTableManager, realmContext);
    }

    private static IWorldSession CreateSession(
        ulong playerCharacterId,
        ulong communityLeaderId,
        out RecordingDispatchProxy<IWorldSession> sessionProxy,
        out RecordingDispatchProxy<ICommunity> communityProxy)
    {
        IWorldSession session = RecordingDispatchProxy<IWorldSession>.Create(out sessionProxy);
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out RecordingDispatchProxy<IPlayer> playerProxy);
        IGuildManager guildManager = RecordingDispatchProxy<IGuildManager>.Create(out RecordingDispatchProxy<IGuildManager> guildManagerProxy);
        ICommunity community = RecordingDispatchProxy<ICommunity>.Create(out communityProxy);
        IBaseMap map = RecordingDispatchProxy<IResidenceMapInstance>.Create(out _);

        sessionProxy.SetProperty(nameof(IWorldSession.Player), player);
        playerProxy.SetProperty(nameof(IPlayer.Map), map);
        playerProxy.SetProperty(nameof(IPlayer.CharacterId), playerCharacterId);
        playerProxy.SetProperty(nameof(IPlayer.GuildManager), guildManager);
        communityProxy.SetProperty(nameof(ICommunity.LeaderId), communityLeaderId);
        communityProxy.SetProperty(nameof(ICommunity.Identity), new Identity
        {
            RealmId = 7,
            Id      = 0x5566778899AABBCCul
        });
        guildManagerProxy.SetMethodReturn(nameof(IGuildManager.GetGuild), community);

        return session;
    }

    private static ClientHousingCommunityRename ReadRequest(string name)
    {
        using var stream = new MemoryStream();
        using (var writer = new GamePacketWriter(stream))
        {
            var targetGuild = new NetworkIdentity
            {
                RealmId = 7,
                Id      = 0x5566778899AABBCCul
            };

            targetGuild.Write(writer);
            writer.WriteStringWide(name);
            writer.Write(false);
            writer.FlushBits();
        }

        byte[] packetData = stream.ToArray();
        using var packetStream = new MemoryStream(packetData);
        using var reader = new GamePacketReader(packetStream);
        var request = new ClientHousingCommunityRename();
        request.Read(reader);
        return request;
    }

    private static IEnumerable<object> GetEncryptedMessages(RecordingDispatchProxy<IWorldSession> sessionProxy)
    {
        return sessionProxy.GetInvocations(nameof(IGameSession.EnqueueMessageEncrypted))
            .Select(invocation => invocation.Arguments[0]);
    }
}
