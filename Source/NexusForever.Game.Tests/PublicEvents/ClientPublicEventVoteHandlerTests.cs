using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Map;
using NexusForever.Game.Abstract.PublicEvent;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.Network;
using NexusForever.Network.World.Message.Model.PublicEvent;
using NexusForever.WorldServer.Network;
using NexusForever.WorldServer.Network.Message.Handler.Event;

namespace NexusForever.Game.Tests.PublicEvents;

public class ClientPublicEventVoteHandlerTests
{
    [Fact]
    public void HandleMessage_DelegatesVoteChoiceToPublicEventManager()
    {
        IWorldSession session = CreateSession(
            out IPlayer player,
            out RecordingDispatchProxy<IPublicEventManager> publicEventManagerProxy);

        var handler = new ClientPublicEventVoteHandler();
        handler.HandleMessage(session, CreateVote(eventId: 0x1234u, voteId: 0x2345u, teamId: 0x3456u, choice: 3u));

        RecordingDispatchProxy<IPublicEventManager>.Invocation invocation = Assert.Single(
            publicEventManagerProxy.GetInvocations(nameof(IPublicEventManager.RespondVote)));
        Assert.Same(player, invocation.Arguments[0]);
        Assert.Equal(0x1234u, invocation.Arguments[1]);
        Assert.Equal(0x2345u, invocation.Arguments[2]);
        Assert.Equal(0x3456u, invocation.Arguments[3]);
        Assert.Equal(3u, invocation.Arguments[4]);
    }

    private static IWorldSession CreateSession(
        out IPlayer player,
        out RecordingDispatchProxy<IPublicEventManager> publicEventManagerProxy)
    {
        IPublicEventManager publicEventManager = RecordingDispatchProxy<IPublicEventManager>.Create(out publicEventManagerProxy);
        IBaseMap map = RecordingDispatchProxy<IBaseMap>.Create(out RecordingDispatchProxy<IBaseMap> mapProxy);
        mapProxy.SetProperty(nameof(IBaseMap.PublicEventManager), publicEventManager);

        player = RecordingDispatchProxy<IPlayer>.Create(out RecordingDispatchProxy<IPlayer> playerProxy);
        playerProxy.SetProperty(nameof(IPlayer.Map), map);

        IWorldSession session = RecordingDispatchProxy<IWorldSession>.Create(out RecordingDispatchProxy<IWorldSession> sessionProxy);
        sessionProxy.SetProperty(nameof(IWorldSession.Player), player);
        return session;
    }

    private static ClientPublicEventVote CreateVote(uint eventId, uint voteId, uint teamId, uint choice)
    {
        byte[] packetData;
        using var stream = new MemoryStream();
        using (var writer = new GamePacketWriter(stream))
        {
            writer.Write(eventId, 14u);
            writer.Write(voteId, 14u);
            writer.Write(teamId, 14u);
            writer.Write(choice);
            writer.FlushBits();

            packetData = stream.ToArray();
        }

        using var reader = new GamePacketReader(new MemoryStream(packetData));
        var message = new ClientPublicEventVote();
        message.Read(reader);
        return message;
    }
}
