using Microsoft.Extensions.DependencyInjection;
using NexusForever.Game.Abstract.Account;
using NexusForever.Game.Abstract.Chat.Format;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Static.Chat;
using NexusForever.Game.Chat.Format;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.Game.Tests.Transport;
using NexusForever.GameTable;
using NexusForever.GameTable.Model;
using NexusForever.GameTable.Static;
using NexusForever.Network.Internal;
using NexusForever.Network.Internal.Message.Chat;
using NexusForever.Network.Internal.Message.Chat.Shared;
using NexusForever.Network.Internal.Message.Chat.Shared.Format;
using NexusForever.Network.Internal.Message.Chat.Shared.Format.Model;
using NexusForever.Network.Session;
using NexusForever.Network.World.Chat.Model;
using NexusForever.Network.World.Message.Model.Chat;
using NexusForever.Shared.Game.Events;
using NexusForever.WorldServer.Network;
using NexusForever.WorldServer.Network.Internal.Handler.Chat;
using GameIdentity = NexusForever.Game.Abstract.Identity;
using InternalIdentity = NexusForever.Network.Internal.Message.Shared.Identity;

namespace NexusForever.Game.Tests.Chat;

public sealed class IncomingChatOwnershipTests
{
    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task BrokerTextWaitsForItsOriginalWorldQueue(bool channel)
    {
        var f = new Fixture();
        await f.Deliver(channel);
        Assert.Empty(f.Packets);
        f.Events.Update(.01);
        Assert.Equal("Private text", Assert.Single(f.Packets).Text);
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task ReplacementDuringFormattingCannotReceiveTheEarlierText(bool channel)
    {
        var f = new Fixture();
        f.Formats.SetMethodHandler(nameof(IChatFormatManager.ToNetwork), args =>
        {
            f.Connection.SetProperty(nameof(IWorldSession.Player), RecordingDispatchProxy<IPlayer>.Create(out _));
            return Array.Empty<ChatFormat>();
        });
        await f.Deliver(channel); f.Events.Update(.01);
        Assert.Empty(f.Packets); Assert.Empty(f.AutomaticReplies);
    }

    [Fact]
    public async Task RecipientReplacementAfterDeliveryCannotSendAStaleAutomaticReply()
    {
        var f = new Fixture();
        f.Actor.SetProperty(nameof(IPlayer.PresenceState), AccountPresenceState.Away);
        f.Actor.SetProperty(nameof(IPlayer.AwayAutoResponseMessage), "Away from keyboard");
        f.Connection.SetMethodHandler(nameof(IGameSession.EnqueueMessageEncrypted), _ =>
        {
            f.Account.SetProperty(nameof(IAccount.WorldSessionEpoch), 8ul); return null;
        });
        await f.Deliver(false); f.Events.Update(.01);
        Assert.Single(f.Packets); Assert.Empty(f.AutomaticReplies);
    }

    [Theory]
    [InlineData(false, AccountPresenceState.Away)] [InlineData(true, AccountPresenceState.Away)]
    [InlineData(false, AccountPresenceState.Busy)] [InlineData(true, AccountPresenceState.Busy)]
    public async Task CurrentWhisperRetainsItsTypeRoutingAndAutomaticResponse(bool accountWhisper, AccountPresenceState state)
    {
        var f = new Fixture(); f.Actor.SetProperty(nameof(IPlayer.PresenceState), state);
        f.Actor.SetProperty(nameof(IPlayer.AwayAutoResponseMessage), "Away");
        f.Actor.SetProperty(nameof(IPlayer.BusyAutoResponseMessage), "Busy");
        await f.Deliver(false, accountWhisper: accountWhisper); f.Events.Update(.01);
        var packet = Assert.Single(f.Packets);
        Assert.Equal(accountWhisper ? ChatChannelType.AccountWhisper : ChatChannelType.Whisper, packet.Channel.ChatChannelId);
        Assert.Equal("Sender", packet.FromName); Assert.Equal("Other realm", packet.FromRealm);
        var reply = Assert.Single(f.AutomaticReplies);
        Assert.Equal(state == AccountPresenceState.Away ? "Away" : "Busy", reply.Text.Text);
        Assert.Equal(42ul, reply.Sender.Id); Assert.Equal((ushort)1, reply.Sender.RealmId);
        Assert.Equal(7ul, reply.Recipient.Id); Assert.Equal((ushort)2, reply.Recipient.RealmId);
        Assert.True(reply.AutoResponse); Assert.Equal(accountWhisper, reply.IsAccountWhisper);
    }

    [Fact]
    public async Task AutomaticWhisperDoesNotStartAnotherAutomaticReply()
    {
        var f = new Fixture(); f.Actor.SetProperty(nameof(IPlayer.PresenceState), AccountPresenceState.Away);
        f.Actor.SetProperty(nameof(IPlayer.AwayAutoResponseMessage), "Away");
        await f.Deliver(false, automatic: true); f.Events.Update(.01);
        Assert.True(Assert.Single(f.Packets).AutoResponse); Assert.Empty(f.AutomaticReplies);
    }

    [Fact]
    public async Task BlankAutomaticResponseDoesNotPublishABrokerMessage()
    {
        var f = new Fixture(); f.Actor.SetProperty(nameof(IPlayer.PresenceState), AccountPresenceState.Away);
        f.Actor.SetProperty(nameof(IPlayer.AwayAutoResponseMessage), "  ");
        await f.Deliver(false); f.Events.Update(.01);
        Assert.Single(f.Packets); Assert.Empty(f.AutomaticReplies);
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task ReplacedQueueBeforeItsTickCannotRedirectQueuedText(bool channel)
    {
        var f = new Fixture(); await f.Deliver(channel);
        var replacement = new EventQueue(); f.Connection.SetProperty(nameof(IWorldSession.Events), replacement);
        replacement.Update(.01); f.Events.Update(.01);
        Assert.Empty(f.Packets); Assert.Empty(f.AutomaticReplies);
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task ChannelBubbleUsesTheOriginalRecipientWorldView(bool visible)
    {
        var f = new Fixture(); f.Entry.Flags = ChatChannelEntryFlags.ServerSide | ChatChannelEntryFlags.ShowChatBubble;
        IPlayer sender = RecordingDispatchProxy<IPlayer>.Create(out var senderProxy); senderProxy.SetProperty(nameof(IPlayer.Guid), 321u);
        f.Actor.SetMethodReturn(nameof(IPlayer.GetVisiblePlayer), visible ? sender : null);
        await f.Deliver(true); Assert.Empty(f.Actor.GetInvocations(nameof(IPlayer.GetVisiblePlayer)));
        f.Events.Update(.01);
        var packet = Assert.Single(f.Packets); Assert.Equal(90ul, packet.Channel.ChatId);
        Assert.Equal(visible ? 321ul : 0ul, packet.UnitId);
    }

    [Fact]
    public async Task RecipientReplacementDuringBubbleLookupCannotReceiveChannelText()
    {
        var f = new Fixture(); f.Entry.Flags = ChatChannelEntryFlags.ShowChatBubble;
        f.Actor.SetMethodHandler(nameof(IPlayer.GetVisiblePlayer), args =>
        { f.Registry.SetMethodReturn(nameof(IPlayerManager.GetPlayer), null); return null; });
        await f.Deliver(true); f.Events.Update(.01); Assert.Empty(f.Packets);
    }

    [Fact]
    public async Task ChannelFanoutKeepsIndependentOriginalRecipientQueues()
    {
        var f = new Fixture(); var other = new Fixture(43);
        f.Registry.SetMethodHandler(nameof(IPlayerManager.GetPlayer), args => ((GameIdentity)args[0]).Id switch
        { 42 => f.Player, 43 => other.Player, _ => null });
        f.Connection.SetMethodHandler(nameof(IGameSession.EnqueueMessageEncrypted), args =>
        { f.Account.SetProperty(nameof(IAccount.WorldSessionEpoch), 8ul); return null; });
        await f.Deliver(true, otherRecipient: 43);
        f.Events.Update(.01); Assert.Single(f.Packets); Assert.Empty(other.Packets);
        other.Events.Update(.01); Assert.Single(other.Packets);
    }

    [Fact]
    public async Task FailedWhisperEnqueueDoesNotSendAnAutomaticReply()
    {
        var f = new Fixture(); var failures = new List<Exception>(); f.Events.UnhandledExceptionHandler = failures.Add;
        f.Actor.SetProperty(nameof(IPlayer.PresenceState), AccountPresenceState.Away);
        f.Actor.SetProperty(nameof(IPlayer.AwayAutoResponseMessage), "Away");
        f.Connection.SetMethodHandler(nameof(IGameSession.EnqueueMessageEncrypted), args => throw new IOException("Owned chat enqueue failure."));
        await f.Deliver(false); f.Events.Update(.01);
        Assert.IsType<IOException>(Assert.Single(failures)); Assert.Empty(f.AutomaticReplies);
    }

    [Fact]
    public async Task QueuedWhisperRetainsItsAdmittedEnvelope()
    {
        var f = new Fixture(); f.Actor.SetProperty(nameof(IPlayer.PresenceState), AccountPresenceState.Away);
        f.Actor.SetProperty(nameof(IPlayer.AwayAutoResponseMessage), "Away");
        await f.Deliver(false);
        f.Whisper.Text.Text = "Changed"; f.Whisper.Sender = new() { Id = 99, RealmId = 9 };
        f.Whisper.SenderName = new() { Name = "Changed", RealmName = "Changed" }; f.Whisper.AutoResponse = true;
        f.Events.Update(.01);
        Assert.Equal("Private text", Assert.Single(f.Packets).Text); Assert.Equal("Sender", Assert.Single(f.Packets).FromName);
        Assert.Equal(7ul, Assert.Single(f.AutomaticReplies).Recipient.Id);
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task QueuedFullItemLinkKeepsItsAdmittedChargeAndRuneLists(bool channel)
    {
        var services = new ServiceCollection(); services.AddGameChatFormat();
        using var provider = services.BuildServiceProvider();
        var formats = provider.GetRequiredService<IChatFormatManager>(); formats.Initialise();
        var f = new Fixture(realFormats: formats);
        var model = new ChatChannelTextItemFullFormat { Item2Id = 123, ChargeAmounts = [1.5f], RuneSlotItem2Ids = [17] };
        await f.Deliver(channel, formats: [new() { Type = ChatFormatType.ItemFull, Model = model }]);
        model.ChargeAmounts[0] = 99; model.RuneSlotItem2Ids[0] = 99;
        f.Events.Update(.01);
        var link = Assert.IsType<ChatFormatItemFull>(Assert.Single(Assert.Single(f.Packets).Formats).Model);
        Assert.Equal(new float[] { 1.5f }, link.ChargeAmounts); Assert.Equal(new uint[] { 17 }, link.RuneSlotItem2Ids);
    }

    private sealed class Fixture
    {
        internal readonly EventQueue Events = new();
        internal readonly IPlayer Player;
        internal readonly IWorldSession Session;
        internal readonly RecordingDispatchProxy<IPlayer> Actor;
        internal readonly RecordingDispatchProxy<IWorldSession> Connection;
        internal readonly RecordingDispatchProxy<IAccount> Account;
        internal readonly RecordingDispatchProxy<IPlayerManager> Registry;
        internal readonly RecordingDispatchProxy<IChatFormatManager> Formats;
        internal readonly ChatChannelEntry Entry = new() { Id = (uint)ChatChannelType.Guild };
        internal ChatWhisperTextMessage Whisper;
        private readonly ChatWhisperTextHandler whisperHandler;
        private readonly ChatChannelTextHandler channelHandler;
        private readonly RecordingDispatchProxy<IInternalMessagePublisher> publisher;
        private readonly InternalIdentity sender = new() { Id = 7, RealmId = 2 };
        private readonly InternalIdentity recipient = new() { Id = 42, RealmId = 1 };
        internal IEnumerable<ServerChat> Packets => Connection.GetInvocations(nameof(IGameSession.EnqueueMessageEncrypted))
            .Select(row => row.Arguments[0]).OfType<ServerChat>();
        internal IEnumerable<ChatWhisperTextMessage> AutomaticReplies => publisher.GetInvocations(nameof(IInternalMessagePublisher.PublishAsync))
            .Select(row => row.Arguments[0]).OfType<ChatWhisperTextMessage>();

        internal Fixture(ulong id = 42, IChatFormatManager realFormats = null)
        {
            recipient = new() { Id = id, RealmId = 1 };
            Session = RecordingDispatchProxy<IWorldSession>.Create(out Connection);
            Player = RecordingDispatchProxy<IPlayer>.Create(out Actor);
            IAccount account = RecordingDispatchProxy<IAccount>.Create(out Account);
            IPlayerManager registry = RecordingDispatchProxy<IPlayerManager>.Create(out Registry);
            IChatFormatManager formats = RecordingDispatchProxy<IChatFormatManager>.Create(out Formats);
            IInternalMessagePublisher messages = RecordingDispatchProxy<IInternalMessagePublisher>.Create(out publisher);
            IGameTableManager tables = RecordingDispatchProxy<IGameTableManager>.Create(out var tableProxy);
            Actor.SetProperty(nameof(IPlayer.Identity), new GameIdentity { Id = id, RealmId = 1 });
            Actor.SetProperty(nameof(IPlayer.CharacterId), id);
            Actor.SetProperty(nameof(IPlayer.Session), Session); Actor.SetProperty(nameof(IPlayer.Account), account);
            Connection.SetProperty(nameof(IWorldSession.Player), Player); Connection.SetProperty(nameof(IWorldSession.Events), Events);
            Account.SetProperty(nameof(IAccount.Session), Session); Account.SetProperty(nameof(IAccount.WorldSessionEpoch), 7ul);
            Registry.SetMethodReturn(nameof(IPlayerManager.GetPlayer), Player);
            Formats.SetMethodReturn(nameof(IChatFormatManager.ToNetwork), Array.Empty<ChatFormat>());
            publisher.SetMethodReturn(nameof(IInternalMessagePublisher.PublishAsync), Task.CompletedTask);
            tableProxy.SetProperty(nameof(IGameTableManager.ChatChannel), PlayerBindPointTests.Table(Entry));
            whisperHandler = new(registry, realFormats ?? formats, messages); channelHandler = new(registry, realFormats ?? formats, tables);
        }

        internal Task Deliver(bool channel, bool automatic = false, bool accountWhisper = false, ulong? otherRecipient = null,
            List<ChatChannelTextFormat> formats = null)
        {
            var senderName = new NexusForever.Network.Internal.Message.Shared.IdentityName { Name = "Sender", RealmName = "Other realm" };
            if (!channel) return whisperHandler.Handle(Whisper = new ChatWhisperTextMessage
            {
                Sender = sender, SenderName = senderName, Recipient = recipient,
                RecipientName = new() { Name = "Recipient", RealmName = "Home realm" },
                Text = new() { Text = "Private text", Format = formats ?? [] }, AutoResponse = automatic, IsAccountWhisper = accountWhisper
            });
            var members = new List<ChatChannelMember> { new() { Identity = sender }, new() { Identity = recipient } };
            if (otherRecipient.HasValue) members.Add(new() { Identity = new() { Id = otherRecipient.Value, RealmId = 1 } });
            return channelHandler.Handle(new ChatChannelTextMessage
            {
                ChatChannel = new() { Type = ChatChannelType.Guild, ChatId = 90, Members = members },
                Sender = new() { Identity = sender, Character = new() { Identity = sender, IdentityName = senderName } },
                Text = new() { Text = "Private text", Format = formats ?? [] }
            });
        }
    }
}
