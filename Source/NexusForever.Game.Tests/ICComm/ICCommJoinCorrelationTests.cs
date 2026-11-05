using Microsoft.Extensions.Logging.Abstractions;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Guild;
using NexusForever.Game.ICComm;
using NexusForever.Game.Static.ICComm;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.Network.Session;
using NexusForever.Network.World.Message.Model.ICComm;
using NexusForever.WorldServer.Network;
using NexusForever.WorldServer.Network.Message.Handler.ICComm;

namespace NexusForever.Game.Tests.ICComm;

public class ICCommJoinCorrelationTests
{
    [Theory]
    [InlineData(ICCommChannelType.Global)]
    [InlineData(ICCommChannelType.Group)]
    [InlineData(ICCommChannelType.Guild)]
    public void PublicJoinRepliesWithTheExactPendingTypeAndName(ICCommChannelType type)
    {
        var manager = new ICCommManager();
        var alice = CreatePlayer(1, type);
        const string requested = "  AddonSync界  ";

        var joined = Join(manager, alice, type, requested);

        Assert.Equal(type, joined.Type);
        Assert.Equal(requested, joined.ChannelName);
        Assert.NotEqual(0ul, joined.IccomId);
        Assert.Empty(alice.Packets<ServerICCommChannelJoinResult>());
    }

    [Theory]
    [InlineData(ICCommChannelType.Group, "AddonSync", ICCommJoinResult.NoGroup)]
    [InlineData(ICCommChannelType.Guild, "AddonSync", ICCommJoinResult.NoGuild)]
    [InlineData(ICCommChannelType.Global, "     ", ICCommJoinResult.BadName)]
    public void PublicFailureStillSelectsTheRequestedPendingChannel(ICCommChannelType type, string name, ICCommJoinResult expected)
    {
        var manager = new ICCommManager();
        var alice = CreatePlayer(1, type, scope: 0);

        HandleJoin(manager, alice, type, name);

        var refused = Assert.Single(alice.Packets<ServerICCommChannelJoinResult>());
        Assert.Equal(type, refused.Type);
        Assert.Equal(name, refused.ChannelName);
        Assert.Equal(expected, refused.Result);
        Assert.Empty(alice.Packets<ServerICCommChannelJoin>());
    }

    [Fact]
    public void GlobalCanonicalAliasesShareDeliveryButEachReplyUsesItsOwnRequest()
    {
        var manager = new ICCommManager();
        var alice = CreatePlayer(1, ICCommChannelType.Global);
        var bob = CreatePlayer(2, ICCommChannelType.Global);
        var first = Join(manager, alice, ICCommChannelType.Global, "AddonSync");

        var second = Join(manager, bob, ICCommChannelType.Global, " addonsync ");

        Assert.Equal(first.IccomId, second.IccomId);
        Assert.Equal(" addonsync ", second.ChannelName);
        Send(manager, alice, first.IccomId);
        Assert.Single(bob.Packets<ServerICCommDirectedMessage>());
    }

    [Theory]
    [InlineData(ICCommChannelType.Group)]
    [InlineData(ICCommChannelType.Guild)]
    public void NamedChannelsWithinOneScopeDoNotLeakAddonMessages(ICCommChannelType type)
    {
        var manager = new ICCommManager();
        var alice = CreatePlayer(1, type);
        var bob = CreatePlayer(2, type);
        var cara = CreatePlayer(3, type);
        var first = Join(manager, alice, type, "AddonAlpha");
        var peer = Join(manager, bob, type, "AddonAlpha");
        var other = Join(manager, cara, type, "AddonBeta");

        Send(manager, alice, first.IccomId);

        Assert.Single(bob.Packets<ServerICCommDirectedMessage>());
        Assert.Empty(cara.Packets<ServerICCommDirectedMessage>());
        Assert.Equal(first.IccomId, peer.IccomId);
        Assert.NotEqual(first.IccomId, other.IccomId);
    }

    [Theory]
    [InlineData(ICCommChannelType.Group)]
    [InlineData(ICCommChannelType.Guild)]
    public void SameNameInDifferentScopesKeepsItsExistingIsolation(ICCommChannelType type)
    {
        var manager = new ICCommManager();
        var alice = CreatePlayer(1, type, 100);
        var bob = CreatePlayer(2, type, 200);
        var first = Join(manager, alice, type, "AddonSync");
        var second = Join(manager, bob, type, "AddonSync");

        Send(manager, alice, first.IccomId);

        Assert.NotEqual(first.IccomId, second.IccomId);
        Assert.Empty(bob.Packets<ServerICCommDirectedMessage>());
    }

    [Theory]
    [InlineData(ICCommChannelType.Group)]
    [InlineData(ICCommChannelType.Guild)]
    public void RepeatedJoinAndNotJoinedCleanupKeepOtherNamedMemberships(ICCommChannelType type)
    {
        var manager = new ICCommManager();
        var alice = CreatePlayer(1, type);
        var bob = CreatePlayer(2, type);
        var first = Join(manager, alice, type, "AddonAlpha");
        var repeated = Join(manager, alice, type, "AddonAlpha");
        var other = Join(manager, alice, type, "AddonBeta");
        Join(manager, bob, type, "AddonAlpha");
        Join(manager, bob, type, "AddonBeta");
        Assert.Equal(first.IccomId, repeated.IccomId);

        var notJoined = new ClientICCommChannelNotJoined();
        typeof(ClientICCommChannelNotJoined).GetProperty(nameof(notJoined.IccomId))!.SetValue(notJoined, first.IccomId);
        new ClientICCommChannelNotJoinedHandler(NullLogger<ClientICCommChannelNotJoinedHandler>.Instance, manager)
            .HandleMessage(alice.Session, notJoined);
        Send(manager, bob, first.IccomId);
        Assert.Empty(alice.Packets<ServerICCommDirectedMessage>());
        Send(manager, bob, other.IccomId);
        Assert.Single(alice.Packets<ServerICCommDirectedMessage>());
    }

    private static ServerICCommChannelJoin Join(ICCommManager manager, Peer peer, ICCommChannelType type, string name)
    {
        HandleJoin(manager, peer, type, name);
        return peer.Packets<ServerICCommChannelJoin>().Last();
    }

    private static void HandleJoin(ICCommManager manager, Peer peer, ICCommChannelType type, string name)
    {
        var request = new ClientICCommChannelJoin();
        typeof(ClientICCommChannelJoin).GetProperty(nameof(request.Type))!.SetValue(request, type);
        typeof(ClientICCommChannelJoin).GetProperty(nameof(request.GuildId))!.SetValue(request, peer.Scope);
        typeof(ClientICCommChannelJoin).GetProperty(nameof(request.Name))!.SetValue(request, name);
        new ClientICCommChannelJoinHandler(NullLogger<ClientICCommChannelJoinHandler>.Instance, manager).HandleMessage(peer.Session, request);
    }

    private static void Send(ICCommManager manager, Peer peer, ulong id)
    {
        var request = new ClientICCommMessage();
        typeof(ClientICCommMessage).GetProperty(nameof(request.IccommId))!.SetValue(request, id);
        typeof(ClientICCommMessage).GetProperty(nameof(request.MessageId))!.SetValue(request, 7u);
        typeof(ClientICCommMessage).GetProperty(nameof(request.Message))!.SetValue(request, "addon payload");
        new ClientICCommMessageHandler(NullLogger<ClientICCommMessageHandler>.Instance, manager).HandleMessage(peer.Session, request);
    }

    private static Peer CreatePlayer(uint guid, ICCommChannelType type, ulong scope = 100)
    {
        IWorldSession session = RecordingDispatchProxy<IWorldSession>.Create(out var connection);
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out var actor);
        actor.SetProperty(nameof(IPlayer.Guid), guid);
        actor.SetProperty(nameof(IPlayer.Name), $"Player{guid}");
        actor.SetProperty(nameof(IGridEntity.InWorld), true);
        actor.SetProperty(nameof(IPlayer.Session), session);
        connection.SetProperty(nameof(IWorldSession.Player), player);
        actor.SetProperty(nameof(IPlayer.GroupAssociation), type == ICCommChannelType.Group ? scope : 0ul);
        IGuildManager membership = RecordingDispatchProxy<IGuildManager>.Create(out var memberships);
        if (type == ICCommChannelType.Guild && scope != 0)
        {
            IGuild guild = RecordingDispatchProxy<IGuild>.Create(out var guildActor);
            guildActor.SetProperty(nameof(IGuild.Id), scope);
            guildActor.SetProperty(nameof(IGuild.Name), "Guild display name");
            memberships.SetProperty(nameof(IGuildManager.Guild), guild);
        }
        actor.SetProperty(nameof(IPlayer.GuildManager), membership);
        return new(session, connection, scope);
    }

    private sealed record Peer(IWorldSession Session, RecordingDispatchProxy<IWorldSession> Connection, ulong Scope)
    {
        internal IEnumerable<T> Packets<T>() => Connection.GetInvocations(nameof(IGameSession.EnqueueMessageEncrypted))
            .Select(call => call.Arguments[0]).OfType<T>();
    }
}
