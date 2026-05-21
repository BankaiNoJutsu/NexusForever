using Microsoft.Extensions.Logging.Abstractions;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.Network;
using NexusForever.Network.Session;
using NexusForever.Network.World.Message.Model.Friendship;
using NexusForever.WorldServer.Network;
using NexusForever.WorldServer.Network.Message.Handler.Friendship;

namespace NexusForever.Game.Tests.Friendship;

public class FriendshipSocialOptionTests
{
    [Fact]
    public void SetAutoResponseMessageHandler_StoresTransientPlayerMessages()
    {
        IWorldSession session = CreateSession(out IPlayer player, out _);
        ClientFriendshipSetAutoResponseMessage message = ReadAutoResponsePacket("Gone exploring", "Deep in combat");
        var handler = new ClientFriendshipSetAutoResponseMessageHandler(
            NullLogger<ClientFriendshipSetAutoResponseMessageHandler>.Instance);

        handler.HandleMessage(session, message);

        Assert.Equal("Gone exploring", player.AwayAutoResponseMessage);
        Assert.Equal("Deep in combat", player.BusyAutoResponseMessage);
    }

    [Theory]
    [InlineData(true, 2u)]
    [InlineData(false, 0u)]
    public void IgnoreStrangersStateHandler_EchoesTransientClientFlag(bool ignoreStrangers, uint expectedFlags)
    {
        IWorldSession session = CreateSession(out _, out RecordingDispatchProxy<IWorldSession> sessionProxy);
        ClientFriendshipIgnoreStrangersState message = ReadIgnoreStrangersPacket(ignoreStrangers);
        var handler = new ClientFriendshipIgnoreStrangersStateHandler(
            NullLogger<ClientFriendshipIgnoreStrangersStateHandler>.Instance);

        handler.HandleMessage(session, message);

        ServerFriendshipIgnoreStrangersState state = GetEncryptedMessages(sessionProxy)
            .OfType<ServerFriendshipIgnoreStrangersState>()
            .Single();
        Assert.Equal(expectedFlags, state.Flags);
    }

    private static IWorldSession CreateSession(
        out IPlayer player,
        out RecordingDispatchProxy<IWorldSession> sessionProxy)
    {
        IWorldSession session = RecordingDispatchProxy<IWorldSession>.Create(out sessionProxy);
        player = RecordingDispatchProxy<IPlayer>.Create(out RecordingDispatchProxy<IPlayer> playerProxy);

        playerProxy.SetProperty(nameof(IPlayer.Guid), 4242u);
        sessionProxy.SetProperty(nameof(IWorldSession.Player), player);
        return session;
    }

    private static ClientFriendshipSetAutoResponseMessage ReadAutoResponsePacket(string awayMessage, string busyMessage)
    {
        using var stream = new MemoryStream();
        using (var writer = new GamePacketWriter(stream))
        {
            writer.WriteStringWide(awayMessage);
            writer.WriteStringWide(busyMessage);
            writer.FlushBits();
        }

        using var readStream = new MemoryStream(stream.ToArray());
        using var reader = new GamePacketReader(readStream);
        var message = new ClientFriendshipSetAutoResponseMessage();
        message.Read(reader);
        return message;
    }

    private static ClientFriendshipIgnoreStrangersState ReadIgnoreStrangersPacket(bool ignoreStrangers)
    {
        using var stream = new MemoryStream();
        using (var writer = new GamePacketWriter(stream))
        {
            writer.Write(ignoreStrangers);
            writer.FlushBits();
        }

        using var readStream = new MemoryStream(stream.ToArray());
        using var reader = new GamePacketReader(readStream);
        var message = new ClientFriendshipIgnoreStrangersState();
        message.Read(reader);
        return message;
    }

    private static IEnumerable<object> GetEncryptedMessages(RecordingDispatchProxy<IWorldSession> sessionProxy)
    {
        return sessionProxy.GetInvocations(nameof(IGameSession.EnqueueMessageEncrypted))
            .Select(invocation => invocation.Arguments[0]);
    }
}
