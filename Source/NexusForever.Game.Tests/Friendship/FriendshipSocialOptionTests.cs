using Microsoft.Extensions.Logging.Abstractions;
using NexusForever.Game.Abstract;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Account;
using NexusForever.Game.Entity;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.Network;
using NexusForever.Network.Session;
using NexusForever.Network.Internal;
using NexusForever.Network.Internal.Message.Friendship;
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
    [InlineData(true)]
    [InlineData(false)]
    public void IgnoreStrangersStateHandler_UsesTheDurableAccountOwner(bool ignoreStrangers)
    {
        IWorldSession session = CreateSession(out _, out RecordingDispatchProxy<IWorldSession> sessionProxy);
        ClientFriendshipIgnoreStrangersState message = ReadIgnoreStrangersPacket(ignoreStrangers);
        var publisher = RecordingDispatchProxy<IInternalMessagePublisher>.Create(out var messages);
        messages.SetMethodReturn(nameof(IInternalMessagePublisher.PublishAsync), Task.CompletedTask);
        var handler = new ClientFriendshipIgnoreStrangersStateHandler(
            NullLogger<ClientFriendshipIgnoreStrangersStateHandler>.Instance, publisher);

        handler.HandleMessage(session, message);

        var setting = Assert.IsType<FriendshipAccountBlockUpdateMessage>(Assert.Single(messages.GetInvocations(nameof(IInternalMessagePublisher.PublishAsync))).Arguments[0]);
        Assert.Equal(ignoreStrangers, setting.BlockFriendRequests);
        Assert.Empty(GetEncryptedMessages(sessionProxy));
    }

    private static IWorldSession CreateSession(
        out IPlayer player,
        out RecordingDispatchProxy<IWorldSession> sessionProxy)
    {
        IWorldSession session = RecordingDispatchProxy<IWorldSession>.Create(out sessionProxy);
        player = RecordingDispatchProxy<IPlayer>.Create(out RecordingDispatchProxy<IPlayer> playerProxy);

        playerProxy.SetProperty(nameof(IPlayer.Guid), 4242u);
        playerProxy.SetProperty(nameof(IPlayer.Identity), new Identity { Id = 42, RealmId = 1 });
        playerProxy.SetProperty(nameof(IPlayer.CharacterId), 42ul);
        playerProxy.SetProperty(nameof(IPlayer.PresenceSessionEpoch), 1ul);
        playerProxy.SetProperty(nameof(IPlayer.PresenceAdmission), 1ul);
        playerProxy.SetProperty(nameof(IPlayer.Session), session);
        var account = RecordingDispatchProxy<IAccount>.Create(out var ap);
        ap.SetProperty(nameof(IAccount.Id), 7u); ap.SetProperty(nameof(IAccount.Session), session);
        ap.SetProperty(nameof(IAccount.WorldSessionEpoch), 1ul);
        playerProxy.SetProperty(nameof(IPlayer.Account), account);
        sessionProxy.SetProperty(nameof(IWorldSession.Account), account);
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
