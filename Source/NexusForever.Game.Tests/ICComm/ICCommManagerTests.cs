using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Guild;
using NexusForever.Game.ICComm;
using NexusForever.Game.Static.ICComm;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.Network.Session;
using NexusForever.Network.World.Message.Model.ICComm;

namespace NexusForever.Game.Tests.ICComm;

public class ICCommManagerTests
{
    [Fact]
    public void Update_RemovesOfflineMemberFromTransientChannel()
    {
        var manager = new ICCommManager();
        IPlayer alice = CreatePlayer(1u, "Alice", true, out _);
        IPlayer bob = CreatePlayer(2u, "Bob", true, out RecordingDispatchProxy<IPlayer> bobProxy);

        Assert.Null(manager.Join(alice, ICCommChannelType.Global, 0ul, "zone", out ulong channelId, out _));
        Assert.Null(manager.Join(bob, ICCommChannelType.Global, 0ul, "zone", out ulong bobChannelId, out _));
        Assert.Equal(channelId, bobChannelId);

        bobProxy.SetProperty(nameof(IGridEntity.InWorld), false);

        manager.Update(1000d);

        Assert.Equal(
            ICCommMessageResult.NotInChannel,
            manager.SendMessage(bob, channelId, 11u, "still here", string.Empty));
        Assert.False(manager.RemoveClientMembership(bob, channelId));
    }

    [Fact]
    public void Update_RemovesEmptyTransientChannel()
    {
        var manager = new ICCommManager();
        IPlayer player = CreatePlayer(1u, "Alice", true, out RecordingDispatchProxy<IPlayer> playerProxy);

        Assert.Null(manager.Join(player, ICCommChannelType.Global, 0ul, "zone", out ulong originalChannelId, out _));
        playerProxy.SetProperty(nameof(IGridEntity.InWorld), false);

        manager.Update(1000d);

        playerProxy.SetProperty(nameof(IGridEntity.InWorld), true);
        Assert.Null(manager.Join(player, ICCommChannelType.Global, 0ul, "zone", out ulong newChannelId, out _));
        Assert.NotEqual(originalChannelId, newChannelId);
    }

    [Fact]
    public void SendMessage_EnqueuesResultOrderedEchoAndDirectedRecipientMessage()
    {
        var manager = new ICCommManager();
        IPlayer alice = CreatePlayer(1u, "Alice", true, out _, out RecordingDispatchProxy<IGameSession> aliceSessionProxy);
        IPlayer bob = CreatePlayer(2u, "Bob", true, out _, out RecordingDispatchProxy<IGameSession> bobSessionProxy);
        Assert.Null(manager.Join(alice, ICCommChannelType.Global, 0ul, "zone", out ulong channelId, out _));
        Assert.Null(manager.Join(bob, ICCommChannelType.Global, 0ul, "zone", out _, out _));

        ICCommMessageResult result = manager.SendMessage(alice, channelId, 77u, "hello", string.Empty);

        Assert.Equal(ICCommMessageResult.Sent, result);
        ServerICCommMessageResult messageResult = Assert.Single(GetEncryptedMessages<ServerICCommMessageResult>(aliceSessionProxy));
        Assert.Equal(channelId, messageResult.IccomId);
        Assert.Equal(77u, messageResult.MessageId);
        Assert.Equal(ICCommMessageResult.Sent, messageResult.Result);
        ServerICCommOrderedMessage orderedMessage = Assert.Single(GetEncryptedMessages<ServerICCommOrderedMessage>(aliceSessionProxy));
        Assert.Equal(channelId, orderedMessage.IccomId);
        Assert.Equal(77u, orderedMessage.MessageId);
        Assert.Equal("hello", orderedMessage.Message);
        ServerICCommDirectedMessage directedMessage = Assert.Single(GetEncryptedMessages<ServerICCommDirectedMessage>(bobSessionProxy));
        Assert.Equal(channelId, directedMessage.IccomId);
        Assert.Equal("hello", directedMessage.Message);
        Assert.Equal("Alice", directedMessage.SenderName);
    }

    [Fact]
    public void SendMessage_WithRecipientNameOnlyDeliversToMatchingOnlineMember()
    {
        var manager = new ICCommManager();
        IPlayer alice = CreatePlayer(1u, "Alice", true, out _, out RecordingDispatchProxy<IGameSession> aliceSessionProxy);
        IPlayer bob = CreatePlayer(2u, "Bob", true, out _, out RecordingDispatchProxy<IGameSession> bobSessionProxy);
        IPlayer cara = CreatePlayer(3u, "Cara", true, out _, out RecordingDispatchProxy<IGameSession> caraSessionProxy);
        Assert.Null(manager.Join(alice, ICCommChannelType.Global, 0ul, "zone", out ulong channelId, out _));
        Assert.Null(manager.Join(bob, ICCommChannelType.Global, 0ul, "zone", out _, out _));
        Assert.Null(manager.Join(cara, ICCommChannelType.Global, 0ul, "zone", out _, out _));

        ICCommMessageResult result = manager.SendMessage(alice, channelId, 78u, "private", "bOb");

        Assert.Equal(ICCommMessageResult.Sent, result);
        Assert.Single(GetEncryptedMessages<ServerICCommMessageResult>(aliceSessionProxy));
        Assert.Single(GetEncryptedMessages<ServerICCommOrderedMessage>(aliceSessionProxy));
        ServerICCommDirectedMessage directedMessage = Assert.Single(GetEncryptedMessages<ServerICCommDirectedMessage>(bobSessionProxy));
        Assert.Equal("private", directedMessage.Message);
        Assert.Empty(GetEncryptedMessages<ServerICCommDirectedMessage>(caraSessionProxy));
    }

    [Fact]
    public void SendMessage_WithMissingRecipientReturnsNotInChannelWithoutEcho()
    {
        var manager = new ICCommManager();
        IPlayer alice = CreatePlayer(1u, "Alice", true, out _, out RecordingDispatchProxy<IGameSession> aliceSessionProxy);
        Assert.Null(manager.Join(alice, ICCommChannelType.Global, 0ul, "zone", out ulong channelId, out _));

        ICCommMessageResult result = manager.SendMessage(alice, channelId, 79u, "private", "Nobody");

        Assert.Equal(ICCommMessageResult.NotInChannel, result);
        Assert.Empty(GetEncryptedMessages<ServerICCommMessageResult>(aliceSessionProxy));
        Assert.Empty(GetEncryptedMessages<ServerICCommOrderedMessage>(aliceSessionProxy));
    }

    [Fact]
    public void SendMessage_GroupSenderNoLongerInGroup_ReturnsNotInChannelAndRemovesMembership()
    {
        var manager = new ICCommManager();
        IPlayer alice = CreatePlayer(1u, "Alice", true, out RecordingDispatchProxy<IPlayer> aliceProxy, out RecordingDispatchProxy<IGameSession> aliceSessionProxy);
        aliceProxy.SetProperty(nameof(IPlayer.GroupAssociation), 9001ul);
        Assert.Null(manager.Join(alice, ICCommChannelType.Group, 0ul, "party", out ulong channelId, out _));

        aliceProxy.SetProperty(nameof(IPlayer.GroupAssociation), 0ul);

        ICCommMessageResult result = manager.SendMessage(alice, channelId, 80u, "stale", string.Empty);

        Assert.Equal(ICCommMessageResult.NotInChannel, result);
        Assert.Empty(GetEncryptedMessages<ServerICCommMessageResult>(aliceSessionProxy));
        Assert.Empty(GetEncryptedMessages<ServerICCommOrderedMessage>(aliceSessionProxy));
        Assert.False(manager.RemoveClientMembership(alice, channelId));
    }

    [Fact]
    public void SendMessage_GuildRecipientNoLongerInGuild_IsPrunedBeforeDelivery()
    {
        var manager = new ICCommManager();
        IPlayer alice = CreatePlayer(1u, "Alice", true, out _, out RecordingDispatchProxy<IGameSession> aliceSessionProxy);
        IPlayer bob = CreatePlayer(2u, "Bob", true, out _, out RecordingDispatchProxy<IGameSession> bobSessionProxy);
        SetGuild(alice, guildId: 7007ul, "Guild");
        SetGuild(bob, guildId: 7007ul, "Guild", out RecordingDispatchProxy<IGuildManager> bobGuildManagerProxy);
        Assert.Null(manager.Join(alice, ICCommChannelType.Guild, 7007ul, "ignored", out ulong channelId, out _));
        Assert.Null(manager.Join(bob, ICCommChannelType.Guild, 7007ul, "ignored", out _, out _));

        bobGuildManagerProxy.SetProperty(nameof(IGuildManager.Guild), null);

        ICCommMessageResult result = manager.SendMessage(alice, channelId, 81u, "guild", string.Empty);

        Assert.Equal(ICCommMessageResult.Sent, result);
        Assert.Single(GetEncryptedMessages<ServerICCommMessageResult>(aliceSessionProxy));
        Assert.Single(GetEncryptedMessages<ServerICCommOrderedMessage>(aliceSessionProxy));
        Assert.Empty(GetEncryptedMessages<ServerICCommDirectedMessage>(bobSessionProxy));
        Assert.False(manager.RemoveClientMembership(bob, channelId));
    }

    [Fact]
    public void Update_RemovesGuildMemberAfterLeavingGuild()
    {
        var manager = new ICCommManager();
        IPlayer player = CreatePlayer(1u, "Alice", true, out _, out _);
        SetGuild(player, guildId: 7007ul, "Guild", out RecordingDispatchProxy<IGuildManager> guildManagerProxy);
        Assert.Null(manager.Join(player, ICCommChannelType.Guild, 7007ul, "ignored", out ulong channelId, out _));

        guildManagerProxy.SetProperty(nameof(IGuildManager.Guild), null);

        manager.Update(1000d);

        Assert.Equal(ICCommMessageResult.NotInChannel, manager.SendMessage(player, channelId, 82u, "gone", string.Empty));
        Assert.False(manager.RemoveClientMembership(player, channelId));
    }

    private static IPlayer CreatePlayer(uint guid, string name, bool inWorld, out RecordingDispatchProxy<IPlayer> playerProxy)
    {
        return CreatePlayer(guid, name, inWorld, out playerProxy, out _);
    }

    private static IPlayer CreatePlayer(
        uint guid,
        string name,
        bool inWorld,
        out RecordingDispatchProxy<IPlayer> playerProxy,
        out RecordingDispatchProxy<IGameSession> sessionProxy)
    {
        IGameSession session = RecordingDispatchProxy<IGameSession>.Create(out sessionProxy);
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out playerProxy);
        playerProxy.SetProperty(nameof(IPlayer.Guid), guid);
        playerProxy.SetProperty(nameof(IPlayer.Name), name);
        playerProxy.SetProperty(nameof(IGridEntity.InWorld), inWorld);
        playerProxy.SetProperty(nameof(IPlayer.Session), session);
        return player;
    }

    private static void SetGuild(IPlayer player, ulong guildId, string guildName)
    {
        SetGuild(player, guildId, guildName, out _);
    }

    private static void SetGuild(
        IPlayer player,
        ulong guildId,
        string guildName,
        out RecordingDispatchProxy<IGuildManager> guildManagerProxy)
    {
        IGuildManager guildManager = RecordingDispatchProxy<IGuildManager>.Create(out guildManagerProxy);
        IGuild guild = RecordingDispatchProxy<IGuild>.Create(out RecordingDispatchProxy<IGuild> guildProxy);
        guildProxy.SetProperty(nameof(IGuild.Id), guildId);
        guildProxy.SetProperty(nameof(IGuild.Name), guildName);
        guildManagerProxy.SetProperty(nameof(IGuildManager.Guild), guild);

        RecordingDispatchProxy<IPlayer> playerProxy = (RecordingDispatchProxy<IPlayer>)(object)player;
        playerProxy.SetProperty(nameof(IPlayer.GuildManager), guildManager);
    }

    private static List<T> GetEncryptedMessages<T>(RecordingDispatchProxy<IGameSession> sessionProxy)
    {
        return sessionProxy.GetInvocations(nameof(IGameSession.EnqueueMessageEncrypted))
            .Where(invocation => invocation.Arguments.Length == 1)
            .Select(invocation => invocation.Arguments[0])
            .OfType<T>()
            .ToList();
    }
}
