using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using NexusForever.Database.Auth;
using NexusForever.Database.Auth.Model;
using NexusForever.Game.Abstract.Account.Reward;
using NexusForever.Game.Account.Reward;
using NexusForever.Game.Tests.Storefront;
using NexusForever.Network;
using NexusForever.Network.World.Message.Model;
using NexusForever.WorldServer.Network.Message.Handler.Reward;

namespace NexusForever.Game.Tests.Reward;

public partial class ClientRewardUpdateRequestClaimTests
{
    [Theory]
    [InlineData((byte)1)] [InlineData((byte)2)] [InlineData((byte)3)]
    public void RefreshOnly_TrailingLaneCannotPersistAGrantOrPublishAnUpsert(byte lane)
    {
        const uint accountId = 42;
        var account = new ClaimStubAccount(accountId);
        var manager = new AccountRewardRotationGrantManager(account, new AccountModel { Id = accountId });
        account.AttachGrantManager(manager);
        var session = new ClaimTestWorldSession(account);
        var schedule = RefreshSchedule();
        var handler = new ClientRewardUpdateRequestHandler(NullLogger<ClientRewardUpdateRequestHandler>.Instance,
            new NoOpGlobalStorefrontManager(), new PersistedRefreshProvider(schedule, manager));
        handler.HandleMessage(session, BuildClaimRequest(1, 15, lane));
        using var connection = new SqliteConnection("Data Source=:memory:"); connection.Open();
        using var db = new AuthContext(new DbContextOptionsBuilder<AuthContext>().UseSqlite(connection).Options);
        db.Database.EnsureCreated();
        db.Account.Add(new AccountModel { Id = accountId, Email = "rotation-refresh@test.invalid", S = "", V = "" });
        db.SaveChanges();
        manager.Save(db); db.SaveChanges();
        Assert.Empty(db.AccountInventory); Assert.Empty(db.AccountCurrency);
        Assert.Empty(db.AccountRewardRotationGrant);
        Assert.Empty(manager.BuildEntryState(1).Entries);
        Assert.Empty(session.EncryptedMessages.OfType<ServerRewardRotationEntryStateUpsert>());
        Assert.Same(schedule, Assert.Single(session.EncryptedMessages.OfType<ServerRewardRotationScheduleArray>()));
    }

    [Fact]
    public void RefreshOnly_ReaderConsumesOneWordAndLeavesTheTailNeutral()
    {
        using var stream = new MemoryStream();
        using (var writer = new GamePacketWriter(stream))
        { writer.Write(2u); writer.Write(15u); writer.Write((byte)1); writer.FlushBits(); }
        using var reader = new GamePacketReader(new MemoryStream(stream.ToArray()));
        var request = new ClientRewardUpdateRequest(); request.Read(reader);
        Assert.Equal(2u, request.RewardRotationIndex); Assert.Equal(5u, reader.BytesRemaining);
        Assert.Equal(15u, reader.ReadUInt()); Assert.Equal((byte)1, reader.ReadByte());
    }

    [Fact]
    public void RefreshOnly_PlainMappedRequestConsumesTheCompleteBody()
    {
        using var reader = new GamePacketReader(new MemoryStream(BitConverter.GetBytes(6u)));
        var request = new ClientRewardUpdateRequest(); request.Read(reader);
        Assert.Equal(6u, request.RewardRotationIndex); Assert.Equal(0u, reader.BytesRemaining);
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public void RefreshOnly_ExistingGrantReadbackSurvivesWithoutANewUpsert(bool extraTail)
    {
        var account = new ClaimStubAccount(42);
        var model = new AccountModel { Id = 42, AccountRewardRotationGrant =
        [new() { AccountId = 42, RewardRotationIndex = 1, ContentId = 15, RewardKeyId = 50,
            RewardType = 1, GrantFlags = RewardRotationEntryStateBuilder.GrantFlagItemOrModifier, GrantedUtc = new DateTime(2020,1,1) }] };
        var manager = new AccountRewardRotationGrantManager(account, model); account.AttachGrantManager(manager);
        var session = new ClaimTestWorldSession(account); var schedule = RefreshSchedule();
        var handler = new ClientRewardUpdateRequestHandler(NullLogger<ClientRewardUpdateRequestHandler>.Instance,
            new NoOpGlobalStorefrontManager(), new PersistedRefreshProvider(schedule, manager));
        ClientRewardUpdateRequest request;
        if (extraTail) request = BuildClaimRequest(1, 15, 1);
        else
        {
            using var reader = new GamePacketReader(new MemoryStream(BitConverter.GetBytes(1u)));
            request = new ClientRewardUpdateRequest(); request.Read(reader);
        }
        handler.HandleMessage(session, request);
        var state = Assert.Single(session.EncryptedMessages.OfType<ServerRewardRotationEntryStateArray>());
        Assert.Equal(50u, Assert.Single(state.Entries).RewardTypeId);
        Assert.Empty(session.EncryptedMessages.OfType<ServerRewardRotationEntryStateUpsert>());
        Assert.Single(manager.BuildEntryState(1).Entries);
    }

    [Fact]
    public void RefreshOnly_UnsupportedIndexCannotUseATrailingLane()
    {
        var account = new ClaimStubAccount(42);
        var manager = new AccountRewardRotationGrantManager(account, new AccountModel { Id = 42 });
        account.AttachGrantManager(manager); var session = new ClaimTestWorldSession(account);
        new ClientRewardUpdateRequestHandler(NullLogger<ClientRewardUpdateRequestHandler>.Instance,
            new NoOpGlobalStorefrontManager(), new PersistedRefreshProvider(RefreshSchedule(), manager))
            .HandleMessage(session, BuildClaimRequest(uint.MaxValue, 15, 1));
        Assert.Empty(manager.BuildEntryState(1).Entries);
        Assert.Empty(session.EncryptedMessages);
    }

    private static ServerRewardRotationScheduleArray RefreshSchedule() => new()
    {
        Entries =
        [
            new() { ContentId = 15, RewardKeyId = 50, RewardType = 1, Duration = 2, Value = 50 },
            new() { ContentId = 15, RewardKeyId = 60, RewardType = 2, Duration = 2, Value = 60 },
            new() { ContentId = 15, RewardKeyId = 70, RewardType = 3, Duration = 2, Value = 70 }
        ]
    };

    private sealed class PersistedRefreshProvider(ServerRewardRotationScheduleArray schedule,
        IAccountRewardRotationGrantManager manager) : IRewardRotationRefreshProvider
    {
        public RewardRotationRefresh Build(uint index) => new(index,
            Array.Empty<ServerRewardRotationContentContext>(), schedule, manager.BuildEntryState(index), "refresh-only-regression");
    }
}
