using System.Collections.Immutable;
using System.Text.Json;
using NexusForever.Database;
using NexusForever.Database.Auth.Model;
using NexusForever.Game.Abstract.Account;
using NexusForever.Game.Abstract.RBAC;
using NexusForever.Game.Static.RBAC;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.GameTable;
using NexusForever.Network;
using NexusForever.Network.Internal;
using NexusForever.Network.Internal.Message.Player;
using NexusForever.Network.Session;
using NexusForever.Network.World.Message.Model.Pregame;
using NexusForever.WorldServer.Network;
using NexusForever.WorldServer.Network.Message.Handler.Authentication;

namespace NexusForever.Game.Tests.Network;

public sealed class PresenceGenerationProducerTests
{
    [Theory]
    [InlineData(0ul)] [InlineData(7ul)]
    public async Task RealAccountInitializationCopiesTheEpochAndAllocatesDistinctAdmissions(ulong epoch)
    {
        var role = RecordingDispatchProxy<IRBACRole>.Create(out var rp); rp.SetProperty(nameof(IRBACRole.Role), Role.Player);
        rp.SetProperty(nameof(IRBACRole.Permissions), ImmutableDictionary<Permission, IRBACPermission>.Empty);
        var rbac = RecordingDispatchProxy<IRBACManager>.Create(out var rbacProxy); rbacProxy.SetMethodReturn(nameof(IRBACManager.GetRole), role);
        var tables = RecordingDispatchProxy<IGameTableManager>.Create(out _);
        var session = RecordingDispatchProxy<IGameSession>.Create(out _);
        var account = new NexusForever.Game.Account.Account(null, rbacManager: rbac, gameTableManager: tables);
        account.Initialise(new AccountModel { Id = 7, Email = "admission@example.test", WorldSessionEpoch = epoch }, session);
        Assert.Equal(epoch, account.WorldSessionEpoch);
        var admissions = await Task.WhenAll(Enumerable.Range(0, 8).Select(_ => Task.Run(account.NextPresenceAdmission)));
        Assert.Equal(epoch == 0 ? Enumerable.Repeat(0ul, 8) : Enumerable.Range(1, 8).Select(i => (ulong)i), admissions.Order());
        Assert.Equal(epoch == 0 ? 0ul : 9ul, account.NextPresenceAdmission());
        Assert.Throws<InvalidOperationException>(() => account.Initialise(new AccountModel { Id = 7, WorldSessionEpoch = epoch + 1 }, session));
    }

    [Fact]
    public void AnAlreadyInitializedRealmSessionCannotAllocateAnotherEpochOrReadItsCredential()
    {
        var account = RecordingDispatchProxy<IAccount>.Create(out _);
        var session = RecordingDispatchProxy<IWorldSession>.Create(out var proxy); proxy.SetProperty(nameof(IWorldSession.Account), account);
        var databases = RecordingDispatchProxy<IDatabaseManager>.Create(out var db);
        var error = Assert.Throws<InvalidPacketValueException>(() => new ClientHelloRealmHandler(databases).HandleMessage(session, new ClientHelloRealm()));
        Assert.DoesNotContain("SessionKey", error.Message); Assert.Empty(db.Invocations);
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task InternalSerializationRetainsOneCompleteGenerationWithoutCredentialFields(bool logout)
    {
        object message = logout ? new PlayerLoggedOutMessage { AccountId = 7, Identity = new() { Id = 101, RealmId = 1 },
            SessionEpoch = 9, Admission = 3, OccurredUtc = new(2026, 10, 30, 12, 0, 0, DateTimeKind.Utc) }
            : new PlayerLoggedInMessage { AccountId = 7, Identity = new() { Id = 101, RealmId = 1 },
                SessionEpoch = 9, Admission = 3, OccurredUtc = new(2026, 10, 30, 12, 0, 0, DateTimeKind.Utc) };
        var payload = await InternalMessagePayloadSerialiser.SerialiseAsync(message);
        using var doc = JsonDocument.Parse(payload.Payload);
        Assert.Equal(new[] { "AccountId", "Admission", "Identity", "OccurredUtc", "SessionEpoch" }, doc.RootElement.EnumerateObject().Select(p => p.Name).Order());
        var recovered = JsonSerializer.Deserialize(payload.Payload, message.GetType());
        var roundtrip = await InternalMessagePayloadSerialiser.SerialiseAsync(recovered);
        Assert.Equal(payload, roundtrip);
    }
}
