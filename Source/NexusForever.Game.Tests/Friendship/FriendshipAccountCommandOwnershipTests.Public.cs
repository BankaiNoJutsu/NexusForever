using System.Reflection;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using NexusForever.Game.Abstract.Account;
using NexusForever.Game.Entity;
using NexusForever.Game.Static.Chat;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.Network;
using NexusForever.Network.Internal;
using NexusForever.Network.Internal.Message.Friendship;
using NexusForever.Network.Internal.Message.Friendship.Shared;
using NexusForever.Network.Message;
using NexusForever.Shared.Game.Events;
using NexusForever.WorldServer.Network;
using NexusForever.WorldServer.Network.Message.Handler.Friendship;
using NexusForever.Network.World.Message.Model.Friendship;
using Actor = NexusForever.Game.Tests.Challenges.RewardNormalSaveSnapshotTests.Actor;
using Store = NexusForever.Game.Tests.Quest.QuestPersistenceDurabilityTests.Fixture;
using Relationships = NexusForever.Game.Tests.Database.FriendshipPresenceTransactionTests.Fixture;

namespace NexusForever.Game.Tests.Friendship;

public partial class FriendshipAccountCommandOwnershipTests
{
    public static IEnumerable<object[]> PublicRequests() =>
        from kind in new[] { "presence", "block", "status", "nickname" }
        from outcome in new[] { "current", "newer-login", "epoch-rebound", "foreign-session", "uninitialized" }
        select new object[] { kind, outcome };

    [Theory]
    [MemberData(nameof(PublicRequests))]
    public async Task ParsedRealPlayerCommandsCarryTheirOriginalOwnerThroughSerializationAndSettlement(string kind, string outcome)
    {
        using var store = new Store(null);
        var session = RecordingDispatchProxy<IWorldSession>.Create(out var sp);
        var events = new EventQueue(); sp.SetProperty(nameof(IWorldSession.Events), events);
        using var actor = new Actor(store, actorSession: session, actorEvents: events);
        sp.SetProperty(nameof(IWorldSession.Player), actor.Player); sp.SetProperty(nameof(IWorldSession.Account), actor.Player.Account);
        var account = (RecordingDispatchProxy<IAccount>)(object)actor.Player.Account;
        account.SetProperty(nameof(IAccount.Id), 7u); account.SetProperty(nameof(IAccount.Session), session);
        account.SetProperty(nameof(IAccount.WorldSessionEpoch), 3ul);
        typeof(Player).GetField("presenceSessionEpoch", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(actor.Player, outcome == "uninitialized" ? 0ul : 3ul);
        typeof(Player).GetField("presenceAdmission", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(actor.Player, 2ul);
        if (outcome == "epoch-rebound") account.SetProperty(nameof(IAccount.WorldSessionEpoch), 4ul);
        if (outcome == "foreign-session") account.SetProperty(nameof(IAccount.Session), null);
        var publisher = RecordingDispatchProxy<IInternalMessagePublisher>.Create(out var messages);
        messages.SetMethodReturn(nameof(IInternalMessagePublisher.PublishAsync), Task.CompletedTask);
        if (kind == "presence") new ClientFriendshipAccountPersonalPresenceChangeHandler(publisher).HandleMessage(session,
            Request<ClientFriendshipAccountPersonalPresenceChange>(w => w.Write((uint)AccountPresenceState.Away, 3)));
        else if (kind == "block") new ClientFriendshipIgnoreStrangersStateHandler(NullLogger<ClientFriendshipIgnoreStrangersStateHandler>.Instance, publisher)
            .HandleMessage(session, Request<ClientFriendshipIgnoreStrangersState>(w => w.Write(false)));
        else if (kind == "status") new ClientFriendshipAccountSetPublicNoteHandler(publisher).HandleMessage(session,
            Request<ClientFriendshipAccountSetPublicNote>(w => w.WriteStringWide("New status")));
        else new ClientFriendshipAccountSetPublicDisplayNameHandler(publisher).HandleMessage(session,
            Request<ClientFriendshipAccountSetPublicDisplayName>(w => w.WriteStringWide("New name")));
        var published = messages.GetInvocations(nameof(IInternalMessagePublisher.PublishAsync));
        if (outcome is "epoch-rebound" or "foreign-session" or "uninitialized")
        {
            Assert.Empty(published); Assert.Equal(AccountPresenceState.Available, actor.Player.PresenceState); return;
        }
        object command = Assert.Single(published).Arguments[0];
        var payload = await InternalMessagePayloadSerialiser.SerialiseAsync(command);
        command = JsonSerializer.Deserialize(payload.Payload, command.GetType())!;
        var owner = (FriendshipAccountCommandOwner)command.GetType().GetProperty("PresenceOwner")!.GetValue(command)!;
        Assert.Equal(3ul, owner.SessionEpoch); Assert.Equal(2ul, owner.Admission);
        Assert.Equal(actor.Player.CharacterId, owner.Identity.Id); Assert.Equal(actor.Player.Identity.RealmId, owner.Identity.RealmId);
        Assert.DoesNotContain("SessionKey", payload.Payload);
        using var relationships = new Relationships(null); await relationships.Seed(true);
        using (var seed = relationships.Context())
        {
            var row = await seed.Account.SingleAsync();
            row.PresenceSessionEpoch = outcome == "newer-login" ? 4ul : 3ul; row.PresenceAdmission = 2;
            row.PresenceClosed = false;
            row.PresenceCharacterId = actor.Player.CharacterId; row.PresenceRealmId = 1;
            row.ActiveCharacterId = actor.Player.CharacterId; row.ActiveRealmId = 1;
            seed.Character.Add(new() { AccountId = 7, CharacterId = actor.Player.CharacterId, RealmId = 1, RealmName = "Synthetic", Name = "Setting actor" });
            await seed.SaveChangesAsync();
        }
        using (var context = relationships.Context()) await Send(context, command);
        if (outcome == "newer-login") await AssertOriginal(relationships);
        else await AssertChanged(relationships, kind);
    }

    private static T Request<T>(Action<GamePacketWriter> write) where T : IReadable, new()
    {
        using var stream = new MemoryStream(); using var writer = new GamePacketWriter(stream);
        write(writer); writer.FlushBits(); using var reader = new GamePacketReader(new MemoryStream(stream.ToArray()));
        var packet = new T(); packet.Read(reader); return packet;
    }
}
