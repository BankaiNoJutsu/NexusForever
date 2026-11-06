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

public sealed class ICCommOwnershipTests
{
    [Theory]
    [InlineData(ICCommChannelType.Global)]
    [InlineData(ICCommChannelType.Group)]
    [InlineData(ICCommChannelType.Guild)]
    public void UnjoinedReusedGuidCannotSendOrRemoveTheOriginalMembership(ICCommChannelType type)
    {
        var manager = new ICCommManager();
        var original = Peer.Create(1, type); var replacement = Peer.Create(1, type); var recipient = Peer.Create(2, type);
        ulong id = Join(manager, original, type); Join(manager, recipient, type);

        Send(manager, replacement, id);
        Assert.Equal(ICCommMessageResult.NotInChannel, Assert.Single(replacement.Packets<ServerICCommMessageResult>()).Result);
        Assert.Empty(recipient.Packets<ServerICCommDirectedMessage>());
        Leave(manager, replacement, id);
        Assert.False(manager.RemoveClientMembership(replacement.Player, id));
        Send(manager, original, id);
        Assert.Single(recipient.Packets<ServerICCommDirectedMessage>());
    }

    [Theory]
    [InlineData(ICCommChannelType.Global)]
    [InlineData(ICCommChannelType.Group)]
    [InlineData(ICCommChannelType.Guild)]
    public void OldActorCannotSendOrCleanUpANewerJoinedActorWithTheSameGuid(ICCommChannelType type)
    {
        var manager = new ICCommManager();
        var original = Peer.Create(1, type); var replacement = Peer.Create(1, type); var recipient = Peer.Create(2, type);
        ulong id = Join(manager, original, type); Join(manager, recipient, type); Assert.Equal(id, Join(manager, replacement, type));

        Send(manager, original, id);
        Assert.Equal(ICCommMessageResult.NotInChannel, Assert.Single(original.Packets<ServerICCommMessageResult>()).Result);
        Assert.Empty(recipient.Packets<ServerICCommDirectedMessage>());
        Leave(manager, original, id);
        Send(manager, replacement, id);
        Assert.Single(recipient.Packets<ServerICCommDirectedMessage>());
    }

    [Theory]
    [InlineData(ICCommChannelType.Global)]
    [InlineData(ICCommChannelType.Group)]
    [InlineData(ICCommChannelType.Guild)]
    public void ReboundSessionMustJoinBeforeUsingTheOldMembership(ICCommChannelType type)
    {
        var manager = new ICCommManager(); var sender = Peer.Create(1, type); var recipient = Peer.Create(2, type);
        ulong id = Join(manager, sender, type); Join(manager, recipient, type);
        var session = NewSession(sender.Player, out var messages);
        sender.Actor.SetProperty(nameof(IPlayer.Session), session);

        HandleSend(manager, session, id);
        Assert.Equal(ICCommMessageResult.NotInChannel, Assert.Single(Packets<ServerICCommMessageResult>(messages)).Result);
        Assert.Empty(recipient.Packets<ServerICCommDirectedMessage>());
        new ClientICCommChannelJoinHandler(NullLogger<ClientICCommChannelJoinHandler>.Instance, manager).HandleMessage(session, JoinRequest(type));
        HandleSend(manager, session, id);
        Assert.Single(recipient.Packets<ServerICCommDirectedMessage>());
        // A late packet on the old socket cannot operate through Player.Session's replacement.
        Send(manager, sender, id);
        Assert.Single(recipient.Packets<ServerICCommDirectedMessage>());
        Leave(manager, sender, id);
        HandleSend(manager, session, id);
        Assert.Equal(2, recipient.Packets<ServerICCommDirectedMessage>().Count());
    }

    [Theory]
    [InlineData(ICCommChannelType.Global)]
    [InlineData(ICCommChannelType.Group)]
    [InlineData(ICCommChannelType.Guild)]
    public void SenderReboundDuringResultStopsEchoAndFanout(ICCommChannelType type)
    {
        var manager = new ICCommManager(); var sender = Peer.Create(1, type); var recipient = Peer.Create(2, type);
        ulong id = Join(manager, sender, type); Join(manager, recipient, type);
        var replacement = NewSession(sender.Player, out var replacementMessages);
        sender.Messages.SetMethodHandler(nameof(IGameSession.EnqueueMessageEncrypted), args =>
        {
            if (args[0] is ServerICCommMessageResult) sender.Actor.SetProperty(nameof(IPlayer.Session), replacement);
            return null;
        });

        Send(manager, sender, id);

        Assert.Equal(ICCommMessageResult.Sent, Assert.Single(sender.Packets<ServerICCommMessageResult>()).Result);
        Assert.Empty(sender.Packets<ServerICCommOrderedMessage>());
        Assert.Empty(Packets<ServerICCommOrderedMessage>(replacementMessages));
        Assert.Empty(recipient.Packets<ServerICCommDirectedMessage>());
    }

    [Theory]
    [InlineData(ICCommChannelType.Global)]
    [InlineData(ICCommChannelType.Group)]
    [InlineData(ICCommChannelType.Guild)]
    public void RecipientLosingScopeDuringSenderResultIsNotDelivered(ICCommChannelType type)
    {
        var manager = new ICCommManager(); var sender = Peer.Create(1, type); var recipient = Peer.Create(2, type);
        ulong id = Join(manager, sender, type); Join(manager, recipient, type);
        sender.Messages.SetMethodHandler(nameof(IGameSession.EnqueueMessageEncrypted), args =>
        {
            if (args[0] is ServerICCommMessageResult) recipient.LoseScope(type);
            return null;
        });

        Send(manager, sender, id);

        Assert.Single(sender.Packets<ServerICCommOrderedMessage>());
        Assert.Empty(recipient.Packets<ServerICCommDirectedMessage>());
        manager.Update(0);
        Assert.False(manager.RemoveClientMembership(recipient.Player, id));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ReplacementRecipientMembershipDoesNotInheritAnOlderAcceptedMessage(bool reuseActor)
    {
        var manager = new ICCommManager(); var sender = Peer.Create(1, ICCommChannelType.Global);
        var recipient = Peer.Create(2, ICCommChannelType.Global);
        var replacement = reuseActor ? recipient : Peer.Create(2, ICCommChannelType.Global);
        ulong id = Join(manager, sender, ICCommChannelType.Global); Join(manager, recipient, ICCommChannelType.Global);
        sender.Messages.SetMethodHandler(nameof(IGameSession.EnqueueMessageEncrypted), args =>
        {
            if (args[0] is ServerICCommMessageResult)
            {
                Assert.True(manager.RemoveClientMembership(recipient.Player, id));
                Join(manager, replacement, ICCommChannelType.Global);
            }
            return null;
        });

        Send(manager, sender, id);

        Assert.Empty(recipient.Packets<ServerICCommDirectedMessage>());
        Assert.Empty(replacement.Packets<ServerICCommDirectedMessage>());
        sender.Messages.SetMethodHandler(nameof(IGameSession.EnqueueMessageEncrypted), _ => null);
        Send(manager, sender, id);
        Assert.Single(replacement.Packets<ServerICCommDirectedMessage>());
    }

    [Fact]
    public void RecipientSessionReplacementDuringSenderResultDoesNotReceiveOldTraffic()
    {
        var manager = new ICCommManager(); var sender = Peer.Create(1, ICCommChannelType.Global); var recipient = Peer.Create(2, ICCommChannelType.Global);
        ulong id = Join(manager, sender, ICCommChannelType.Global); Join(manager, recipient, ICCommChannelType.Global);
        var replacement = NewSession(recipient.Player, out var replacementMessages);
        sender.Messages.SetMethodHandler(nameof(IGameSession.EnqueueMessageEncrypted), args =>
        {
            if (args[0] is ServerICCommMessageResult) recipient.Actor.SetProperty(nameof(IPlayer.Session), replacement);
            return null;
        });

        Send(manager, sender, id);

        Assert.Empty(recipient.Packets<ServerICCommDirectedMessage>());
        Assert.Empty(Packets<ServerICCommDirectedMessage>(replacementMessages));
    }

    [Fact]
    public void SenderLeavingDuringFirstDeliveryStopsLaterRecipients()
    {
        var manager = new ICCommManager(); var sender = Peer.Create(1, ICCommChannelType.Global);
        var first = Peer.Create(2, ICCommChannelType.Global); var later = Peer.Create(3, ICCommChannelType.Global);
        ulong id = Join(manager, sender, ICCommChannelType.Global); Join(manager, first, ICCommChannelType.Global); Join(manager, later, ICCommChannelType.Global);
        first.Messages.SetMethodHandler(nameof(IGameSession.EnqueueMessageEncrypted), args =>
        {
            if (args[0] is ServerICCommDirectedMessage) Assert.True(manager.RemoveClientMembership(sender.Player, id));
            return null;
        });

        Send(manager, sender, id);

        Assert.Single(first.Packets<ServerICCommDirectedMessage>());
        Assert.Empty(later.Packets<ServerICCommDirectedMessage>());
    }

    [Fact]
    public void UnchangedMembershipKeepsAcknowledgementEchoAndSenderNameSnapshot()
    {
        var manager = new ICCommManager(); var sender = Peer.Create(1, ICCommChannelType.Global); var recipient = Peer.Create(2, ICCommChannelType.Global);
        ulong id = Join(manager, sender, ICCommChannelType.Global); Join(manager, recipient, ICCommChannelType.Global);
        sender.Messages.SetMethodHandler(nameof(IGameSession.EnqueueMessageEncrypted), args =>
        {
            if (args[0] is ServerICCommMessageResult) sender.Actor.SetProperty(nameof(IPlayer.Name), "Changed after acceptance");
            return null;
        });

        Send(manager, sender, id);

        Assert.Single(sender.Packets<ServerICCommMessageResult>());
        Assert.Single(sender.Packets<ServerICCommOrderedMessage>());
        Assert.Equal("Player1", Assert.Single(recipient.Packets<ServerICCommDirectedMessage>()).SenderName);
    }

    [Fact]
    public void RepeatedJoinByTheSameRecipientDuringAcceptanceRetainsDelivery()
    {
        var manager = new ICCommManager(); var sender = Peer.Create(1, ICCommChannelType.Global); var recipient = Peer.Create(2, ICCommChannelType.Global);
        ulong id = Join(manager, sender, ICCommChannelType.Global); Join(manager, recipient, ICCommChannelType.Global);
        sender.Messages.SetMethodHandler(nameof(IGameSession.EnqueueMessageEncrypted), args =>
        {
            if (args[0] is ServerICCommMessageResult) Assert.Equal(id, Join(manager, recipient, ICCommChannelType.Global));
            return null;
        });

        Send(manager, sender, id);

        Assert.Single(recipient.Packets<ServerICCommDirectedMessage>());
    }

    [Fact]
    public void UpdateUsesCapturedGuidAndPreservesANewerOwnersOtherChannel()
    {
        var manager = new ICCommManager(); var old = Peer.Create(1, ICCommChannelType.Global); var newer = Peer.Create(1, ICCommChannelType.Global);
        ulong oldId = Join(manager, old, ICCommChannelType.Global);
        old.Actor.SetProperty(nameof(IPlayer.Guid), 99u);
        Assert.Null(manager.Join(newer.Player, ICCommChannelType.Global, 0, "OtherAddon", out ulong newId, out _));

        manager.Update(0);

        Assert.False(manager.RemoveClientMembership(old.Player, oldId));
        Send(manager, newer, newId);
        Assert.Equal(ICCommMessageResult.Sent, Assert.Single(newer.Packets<ServerICCommMessageResult>()).Result);
        Assert.True(manager.RemoveClientMembership(newer.Player, newId));
        Assert.NotEqual(oldId, Join(manager, Peer.Create(3, ICCommChannelType.Global), ICCommChannelType.Global));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void JoinReplyIsSuppressedAfterTheSessionRebindsItsPlayer(bool rejected)
    {
        var peer = Peer.Create(1, ICCommChannelType.Global); var replacement = Peer.Create(2, ICCommChannelType.Global);
        var manager = RecordingDispatchProxy<NexusForever.Game.Abstract.ICComm.IICCommManager>.Create(out var service);
        service.SetMethodHandler(nameof(NexusForever.Game.Abstract.ICComm.IICCommManager.Join), args =>
        {
            args[4] = 123ul; args[5] = "AddonSync";
            peer.Messages.SetProperty(nameof(IWorldSession.Player), replacement.Player);
            return rejected ? ICCommJoinResult.BadName : null;
        });

        new ClientICCommChannelJoinHandler(NullLogger<ClientICCommChannelJoinHandler>.Instance, manager)
            .HandleMessage(peer.Session, JoinRequest(ICCommChannelType.Global));

        Assert.Empty(peer.Packets<ServerICCommChannelJoin>());
        Assert.Empty(peer.Packets<ServerICCommChannelJoinResult>());
        Assert.Empty(replacement.Packets<ServerICCommChannelJoin>());
    }

    private static ulong Join(ICCommManager manager, Peer peer, ICCommChannelType type)
    {
        new ClientICCommChannelJoinHandler(NullLogger<ClientICCommChannelJoinHandler>.Instance, manager).HandleMessage(peer.Session, JoinRequest(type));
        return peer.Packets<ServerICCommChannelJoin>().Last().IccomId;
    }
    private static ClientICCommChannelJoin JoinRequest(ICCommChannelType type)
    {
        var request = new ClientICCommChannelJoin(); Set(request, nameof(request.Type), type); Set(request, nameof(request.GuildId), 100ul); Set(request, nameof(request.Name), "AddonSync"); return request;
    }
    private static void Send(ICCommManager manager, Peer peer, ulong id) => HandleSend(manager, peer.Session, id);
    private static void HandleSend(ICCommManager manager, IWorldSession session, ulong id)
    {
        var request = new ClientICCommMessage(); Set(request, nameof(request.IccommId), id); Set(request, nameof(request.MessageId), 7u); Set(request, nameof(request.Message), "accepted payload");
        new ClientICCommMessageHandler(NullLogger<ClientICCommMessageHandler>.Instance, manager).HandleMessage(session, request);
    }
    private static void Leave(ICCommManager manager, Peer peer, ulong id)
    {
        var request = new ClientICCommChannelNotJoined(); Set(request, nameof(request.IccomId), id);
        new ClientICCommChannelNotJoinedHandler(NullLogger<ClientICCommChannelNotJoinedHandler>.Instance, manager).HandleMessage(peer.Session, request);
    }
    private static void Set<T>(T value, string property, object data) => typeof(T).GetProperty(property)!.SetValue(value, data);
    private static IWorldSession NewSession(IPlayer player, out RecordingDispatchProxy<IWorldSession> messages)
    {
        var session = RecordingDispatchProxy<IWorldSession>.Create(out messages); messages.SetProperty(nameof(IWorldSession.Player), player); return session;
    }
    private static IEnumerable<T> Packets<T>(RecordingDispatchProxy<IWorldSession> messages) => messages.GetInvocations(nameof(IGameSession.EnqueueMessageEncrypted)).Select(call => call.Arguments[0]).OfType<T>();
    private sealed record Peer(IPlayer Player, RecordingDispatchProxy<IPlayer> Actor, IWorldSession Session, RecordingDispatchProxy<IWorldSession> Messages, RecordingDispatchProxy<IGuildManager> Guilds)
    {
        internal IEnumerable<T> Packets<T>() => ICCommOwnershipTests.Packets<T>(Messages);
        internal void LoseScope(ICCommChannelType type)
        {
            if (type == ICCommChannelType.Group) Actor.SetProperty(nameof(IPlayer.GroupAssociation), 0ul);
            else if (type == ICCommChannelType.Guild) Guilds.SetProperty(nameof(IGuildManager.Guild), null);
            else Actor.SetProperty(nameof(IPlayer.InWorld), false);
        }
        internal static Peer Create(uint id, ICCommChannelType type)
        {
            var player = RecordingDispatchProxy<IPlayer>.Create(out var actor); var session = NewSession(player, out var messages);
            actor.SetProperty(nameof(IPlayer.Guid), id); actor.SetProperty(nameof(IPlayer.Name), $"Player{id}"); actor.SetProperty(nameof(IPlayer.Session), session); actor.SetProperty(nameof(IPlayer.InWorld), true);
            actor.SetProperty(nameof(IPlayer.GroupAssociation), type == ICCommChannelType.Group ? 100ul : 0ul);
            var guilds = RecordingDispatchProxy<IGuildManager>.Create(out var membership); var guild = RecordingDispatchProxy<IGuild>.Create(out var owner); owner.SetProperty(nameof(IGuild.Id), 100ul); membership.SetProperty(nameof(IGuildManager.Guild), guild); actor.SetProperty(nameof(IPlayer.GuildManager), guilds);
            return new(player, actor, session, messages, membership);
        }
    }
}
