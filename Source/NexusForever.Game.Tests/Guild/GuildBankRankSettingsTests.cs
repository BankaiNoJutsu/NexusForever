using System.Diagnostics;
using System.Reflection;
using System.Data;
using System.Data.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using NexusForever.Database.Character;
using NexusForever.Game.Abstract.Account;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Guild;
using NexusForever.Game.Static.Guild;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.Network;
using NexusForever.Network.Session;
using NexusForever.Network.World.Message.Model.Guild;
using NexusForever.Shared.Game.Events;
using NexusForever.WorldServer.Network;
using NexusForever.WorldServer.Network.Message.Handler.Guild;

namespace NexusForever.Game.Tests.Guild;

public sealed partial class GuildMutationRecoveryTests
{
    [Theory]
    [InlineData(GuildOperation.BankTabPermissions, 0x0fc000000000001ful)]
    [InlineData(GuildOperation.WithdrawalLimitChange, 777ul)]
    [InlineData(GuildOperation.RepairLimitChange, 0x7ffffffffffffffful)]
    public async Task BankRankPublicSettingPersistsAndPublishesConfirmedReadback(GuildOperation operation, ulong value)
    {
        using var f = await Create(); var world = BankWorld(f);
        BankOperate(f, operation, value);
        Assert.Empty(BankPackets<ServerGuildRankChange>(f.Leader));
        using (var before = Context()) Assert.Equal(0ul, BankValue(await before.GuildRank.SingleAsync(row => row.Index == 2), operation));
        await Flush(f.Manager); await BankPump(world);
        using var stored = Context(); Assert.Equal(value, BankValue(await stored.GuildRank.SingleAsync(row => row.Index == 2), operation));
        foreach (var player in world.Players.Values)
        {
            var packet = Assert.Single(BankPackets<ServerGuildRankChange>(player));
            Assert.Equal(value, BankValue(packet.Ranks[2], operation));
            Assert.Single(BankPackets<ServerGuildResult>(player), row => row.Result == GuildResult.RankModified);
        }
    }

    [Fact]
    public async Task BankRankPendingSettingsAreNotLeakedByOrdinaryRankBroadcast()
    {
        using var f = await Create(); var world = BankWorld(f);
        BankOperate(f, GuildOperation.BankTabPermissions, 0x3f);
        Operate(f, GuildOperation.RankRename, "Public bank rank", 2);
        Assert.Empty(BankPackets<ServerGuildRankChange>(f.Leader));
        Assert.Empty(BankPackets<ServerGuildResult>(f.Leader));
        Assert.Equal(0ul, f.Guild.Build().Ranks[2].BankWithdrawalPermissions);
        await Flush(f.Manager); await BankPump(world);
        var packet = Assert.Single(BankPackets<ServerGuildRankChange>(f.Leader));
        Assert.Equal(0x3ful, packet.Ranks[2].BankWithdrawalPermissions);
        Assert.Equal("Public bank rank", packet.Ranks[2].RankName);
        Assert.Contains(BankPackets<ServerGuildResult>(f.Leader), row => row.Result == GuildResult.RankRenamed && row.TargetName == "Public bank rank");
    }

    [Fact]
    public async Task BankRankLaterOrdinaryEditRetainsItsReadbackBehindTheQueuedBankSnapshot()
    {
        using var f = await Create(); var world = BankWorld(f);
        BankOperate(f, GuildOperation.WithdrawalLimitChange, 17);
        await Flush(f.Manager); // SQL is confirmed; the original World queues have not published it.
        Operate(f, GuildOperation.RankRename, "Later ordinary rank", 2);
        await Flush(f.Manager); await BankPump(world);
        var packets = BankPackets<ServerGuildRankChange>(f.Leader).ToArray();
        Assert.Equal(2, packets.Length);
        Assert.Equal("Unused", packets[0].Ranks[2].RankName); Assert.Equal("Later ordinary rank", packets[1].Ranks[2].RankName);
        using var stored = Context(); Assert.Equal("Later ordinary rank", stored.GuildRank.Single(row => row.Index == 2).Name);
    }

    [Fact]
    public async Task BankRankNoOpIsAcknowledgedAndCoalescedSettingsUseOneConfirmedGraph()
    {
        using var f = await Create(); var world = BankWorld(f);
        BankOperate(f, GuildOperation.WithdrawalLimitChange, 0); await Flush(f.Manager); await BankPump(world);
        Assert.Single(BankPackets<ServerGuildRankChange>(f.Leader));
        BankOperate(f, GuildOperation.WithdrawalLimitChange, 20);
        BankOperate(f, GuildOperation.RepairLimitChange, 30); BankOperate(f, GuildOperation.BankTabPermissions, ulong.MaxValue);
        await Flush(f.Manager); await BankPump(world);
        var latest = BankPackets<ServerGuildRankChange>(f.Leader).Last().Ranks[2];
        Assert.Equal((20ul, 30ul, ulong.MaxValue), (latest.MoneyWithdrawalLimit, latest.RepairLimit, latest.BankWithdrawalPermissions));
        Assert.Equal(2, BankPackets<ServerGuildRankChange>(f.Leader).Count());
        Assert.Equal(2, BankPackets<ServerGuildResult>(f.Leader).Count(row => row.Result == GuildResult.RankModified));
    }

    [Fact]
    public async Task BankRankRejectedGraphRetainsAllThreeSettingsWithoutEarlySuccess()
    {
        using var f = await Create(); var world = BankWorld(f);
        using (var setup = Context()) await setup.Database.ExecuteSqlRawAsync(
            "CREATE TRIGGER reject_bank_rank BEFORE UPDATE ON guild_rank BEGIN SELECT RAISE(ABORT, 'owned bank-rank failure'); END");
        BankOperate(f, GuildOperation.BankTabPermissions, 0x3f); BankOperate(f, GuildOperation.WithdrawalLimitChange, 40);
        BankOperate(f, GuildOperation.RepairLimitChange, 50); await Flush(f.Manager); await BankPump(world);
        Assert.Empty(BankPackets<ServerGuildRankChange>(f.Leader)); Assert.Empty(BankPackets<ServerGuildResult>(f.Leader));
        using (var failed = Context())
        {
            var row = failed.GuildRank.Single(value => value.Index == 2); Assert.Equal((0ul, 0ul, 0ul), (row.BankWithdrawalPermission, row.MoneyWithdrawalLimit, row.RepairLimit));
            await failed.Database.ExecuteSqlRawAsync("DROP TRIGGER reject_bank_rank");
        }
        await Flush(f.Manager); await BankPump(world);
        using var stored = Context(); var final = stored.GuildRank.Single(value => value.Index == 2);
        Assert.Equal((0x3ful, 40ul, 50ul), (final.BankWithdrawalPermission, final.MoneyWithdrawalLimit, final.RepairLimit));
        Assert.Single(BankPackets<ServerGuildRankChange>(f.Leader));
    }

    [Fact]
    public async Task BankRankFrozenSavePublishesOnlyItsAcceptedSequenceAndRetainsTheLaterEdit()
    {
        using var f = await Create(); var world = BankWorld(f);
        BankOperate(f, GuildOperation.WithdrawalLimitChange, 10);
        await using (var context = Context()) await using (var transaction = await context.Database.BeginTransactionAsync(IsolationLevel.Serializable))
        {
            f.Guild.Save(context); await context.SaveChangesAsync(false); context.ChangeTracker.AcceptAllChanges();
            BankOperate(f, GuildOperation.WithdrawalLimitChange, 20); await transaction.CommitAsync();
        }
        await BankPump(world); Assert.Equal(10ul, Assert.Single(BankPackets<ServerGuildRankChange>(f.Leader)).Ranks[2].MoneyWithdrawalLimit);
        Assert.Equal(10ul, f.Guild.Build().Ranks[2].MoneyWithdrawalLimit);
        await Flush(f.Manager); await BankPump(world);
        Assert.Equal(new ulong[] { 10, 20 }, BankPackets<ServerGuildRankChange>(f.Leader).Select(row => row.Ranks[2].MoneyWithdrawalLimit));
    }

    [Fact]
    public async Task BankRankLostCommitAcknowledgementWaitsForStoredObservation()
    {
        using var f = await Create(); var world = BankWorld(f); BankOperate(f, GuildOperation.RepairLimitChange, 90);
        using var options = Context(); var connection = options.Database.GetConnectionString();
        await Assert.ThrowsAsync<IOException>(() => CharacterAtomicSave.Execute(() => new CharacterContext(new DbContextOptionsBuilder<CharacterContext>()
            .UseSqlite(connection).AddInterceptors(new BankLostAcknowledgement()).Options), 42, f.Guild.Save));
        await BankPump(world); Assert.Empty(BankPackets<ServerGuildRankChange>(f.Leader));
        using (var committed = Context()) Assert.Equal(90ul, committed.GuildRank.Single(row => row.Index == 2).RepairLimit);
        await Flush(f.Manager); await BankPump(world); Assert.Single(BankPackets<ServerGuildRankChange>(f.Leader));
    }

    [Theory]
    [InlineData(0u)] [InlineData(10u)] [InlineData(256u)] [InlineData(uint.MaxValue)]
    public async Task BankRankRejectsFullWidthOrNonLowerRankWithoutMutation(uint rank)
    {
        using var f = await Create(); var world = BankWorld(f);
        BankOperate(f, GuildOperation.WithdrawalLimitChange, 100, rank);
        await Flush(f.Manager); await BankPump(world);
        Assert.Empty(BankPackets<ServerGuildRankChange>(f.Leader));
        using var stored = Context(); Assert.All(stored.GuildRank, row => Assert.Equal(0ul, row.MoneyWithdrawalLimit));
        Assert.Contains(BankPackets<ServerGuildResult>(f.Leader), row => row.Result is GuildResult.InvalidRank or GuildResult.CanOnlyModifyLowerRanks);
    }

    [Fact]
    public async Task BankRankRejectsUnsignedNegativeMoneyAndUnprivilegedMember()
    {
        using var f = await Create(); var world = BankWorld(f);
        BankOperate(f, GuildOperation.WithdrawalLimitChange, ulong.MaxValue);
        BankOperate(f, GuildOperation.RepairLimitChange, 1ul << 63);
        BankOperate(f, GuildOperation.BankTabPermissions, 0x3f, actor: f.Other);
        await Flush(f.Manager); await BankPump(world);
        Assert.Empty(BankPackets<ServerGuildRankChange>(f.Leader)); Assert.Empty(BankPackets<ServerGuildRankChange>(f.Other));
        using var stored = Context(); Assert.All(stored.GuildRank, row => Assert.Equal((0ul, 0ul, 0ul), (row.BankWithdrawalPermission, row.MoneyWithdrawalLimit, row.RepairLimit)));
        Assert.Contains(BankPackets<ServerGuildResult>(f.Other), row => row.Result == GuildResult.RankLacksSufficientPermissions);
    }

    [Fact]
    public async Task BankRankSharedPermissionEditCannotAliasTheLeaderThroughAHighRank()
    {
        using var f = await Create(); var world = BankWorld(f);
        f.Guild.GetMember(43).Rank.Permissions = GuildRankPermission.EditLowerRankPermissions; await Flush(f.Manager);
        BankOperate(f, GuildOperation.RankPermissions, 0, rank: 256, actor: f.Other);
        await Flush(f.Manager); await BankPump(world);
        Assert.Equal(GuildRankPermission.Leader, f.Guild.GetMember(42).Rank.Permissions);
        using var stored = Context(); Assert.Equal((uint)GuildRankPermission.Leader, stored.GuildRank.Single(row => row.Index == 0).Permission);
    }

    [Fact]
    public async Task BankRankPublicationWaitsForEachOriginalWorldQueue()
    {
        using var f = await Create(); var world = BankWorld(f);
        BankOperate(f, GuildOperation.WithdrawalLimitChange, 66); await Flush(f.Manager);
        Assert.Empty(BankPackets<ServerGuildRankChange>(f.Leader)); Assert.Empty(BankPackets<ServerGuildRankChange>(f.Other));
        for (int i = 0; i < 10 && world.Queues[0].PendingEvents; i++) world.Queues[0].Update(.01);
        Assert.Single(BankPackets<ServerGuildRankChange>(f.Leader)); Assert.Empty(BankPackets<ServerGuildRankChange>(f.Other));
        await BankPump(world); Assert.Single(BankPackets<ServerGuildRankChange>(f.Other));
    }

    [Fact]
    public async Task BankRankStalePlayerManagerOwnerCannotApplyANewSetting()
    {
        using var f = await Create(); var world = BankWorld(f);
        world.Players[42] = RecordingDispatchProxy<IPlayer>.Create(out _);
        BankOperate(f, GuildOperation.WithdrawalLimitChange, 91); await Flush(f.Manager); await BankPump(world);
        using var stored = Context(); Assert.Equal(0ul, stored.GuildRank.Single(row => row.Index == 2).MoneyWithdrawalLimit);
        Assert.Empty(BankPackets<ServerGuildRankChange>(f.Leader));
        Assert.Contains(BankPackets<ServerGuildResult>(f.Leader), row => row.Result == GuildResult.UnableToProcess);
    }

    [Theory]
    [InlineData("rank")] [InlineData("result")]
    public async Task BankRankRejectedMutableReplyRetriesFreshWithoutReplayingAcceptedPacketsOrSql(string target)
    {
        using var f = await Create(); var world = BankWorld(f); object rejected = null; int attempts = 0;
        var session = (RecordingDispatchProxy<IWorldSession>)(object)f.Leader.Session;
        session.SetMethodHandler(nameof(IGameSession.EnqueueMessageEncrypted), args =>
        {
            bool selected = target == "rank" ? args[0] is ServerGuildRankChange : args[0] is ServerGuildResult { Result: GuildResult.RankModified };
            if (selected && ++attempts == 1)
            {
                rejected = args[0];
                if (rejected is ServerGuildRankChange rank) rank.Ranks.Clear();
                if (rejected is ServerGuildResult result) result.ReferenceId = 999;
                throw new IOException("Owned bank-rank enqueue rejection.");
            }
            return null;
        });
        BankOperate(f, GuildOperation.WithdrawalLimitChange, 12); await Flush(f.Manager); await BankPump(world);
        object accepted = target == "rank" ? BankPackets<ServerGuildRankChange>(f.Leader).Last() : BankPackets<ServerGuildResult>(f.Leader).Last();
        Assert.NotSame(rejected, accepted); Assert.Equal(2, attempts);
        Assert.Equal(10, BankPackets<ServerGuildRankChange>(f.Leader).Last().Ranks.Count);
        Assert.Equal(2u, BankPackets<ServerGuildResult>(f.Leader).Last().ReferenceId);
        Assert.Equal(target == "rank" ? 2 : 1, BankPackets<ServerGuildRankChange>(f.Leader).Count());
        Assert.Single(BankPackets<ServerGuildRankChange>(f.Other));
        using var stored = Context(); Assert.Equal(12ul, stored.GuildRank.Single(row => row.Index == 2).MoneyWithdrawalLimit);
    }

    [Theory]
    [InlineData("player")] [InlineData("account-session")] [InlineData("epoch")]
    public async Task BankRankOriginalRecipientReplacementStopsLaterPacketsButNotOtherMembers(string change)
    {
        using var f = await Create(); var world = BankWorld(f);
        var session = (RecordingDispatchProxy<IWorldSession>)(object)f.Leader.Session;
        session.SetMethodHandler(nameof(IGameSession.EnqueueMessageEncrypted), args =>
        {
            if (args[0] is not ServerGuildRankChange) return null;
            if (change == "player") session.SetProperty(nameof(IWorldSession.Player), RecordingDispatchProxy<IPlayer>.Create(out _));
            var account = (RecordingDispatchProxy<IAccount>)(object)f.Leader.Account;
            if (change == "epoch") account.SetProperty(nameof(IAccount.WorldSessionEpoch), 2ul);
            if (change == "account-session") account.SetProperty(nameof(IAccount.Session), RecordingDispatchProxy<IWorldSession>.Create(out _));
            return null;
        });
        BankOperate(f, GuildOperation.RepairLimitChange, 80); await Flush(f.Manager); await BankPump(world);
        Assert.Single(BankPackets<ServerGuildRankChange>(f.Leader)); Assert.Empty(BankPackets<ServerGuildResult>(f.Leader));
        Assert.Single(BankPackets<ServerGuildResult>(f.Other));
    }

    private sealed class BankLostAcknowledgement : DbTransactionInterceptor
    {
        public override Task TransactionCommittedAsync(DbTransaction transaction, TransactionEndEventData data, CancellationToken cancellationToken = default)
            => Task.FromException(new IOException("Owned bank-rank commit acknowledgement loss."));
    }

    private static ulong BankValue(NexusForever.Database.Character.Model.GuildRankModel rank, GuildOperation operation)
        => operation switch { GuildOperation.BankTabPermissions => rank.BankWithdrawalPermission,
            GuildOperation.WithdrawalLimitChange => rank.MoneyWithdrawalLimit, _ => rank.RepairLimit };
    private static ulong BankValue(NexusForever.Network.World.Message.Model.Guild.GuildRank rank, GuildOperation operation)
        => operation switch { GuildOperation.BankTabPermissions => rank.BankWithdrawalPermissions,
            GuildOperation.WithdrawalLimitChange => rank.MoneyWithdrawalLimit, _ => rank.RepairLimit };
    private sealed record BankSessions(Dictionary<ulong, IPlayer> Players, EventQueue[] Queues, List<Exception> Errors);
    private static BankSessions BankWorld(Fixture f)
    {
        var players = new Dictionary<ulong, IPlayer> { [42] = f.Leader, [43] = f.Other };
        var errors = new List<Exception>(); var events = new List<EventQueue>();
        foreach (var player in players.Values)
        {
            var session = (RecordingDispatchProxy<IWorldSession>)(object)player.Session;
            var queue = new EventQueue { UnhandledExceptionHandler = errors.Add }; events.Add(queue);
            session.SetProperty(nameof(IWorldSession.Events), queue); session.SetProperty(nameof(IWorldSession.Player), player);
            var account = RecordingDispatchProxy<IAccount>.Create(out var accountProxy);
            accountProxy.SetProperty(nameof(IAccount.Session), player.Session); accountProxy.SetProperty(nameof(IAccount.WorldSessionEpoch), 1ul);
            ((RecordingDispatchProxy<IPlayer>)(object)player).SetProperty(nameof(IPlayer.Account), account);
        }
        var manager = RecordingDispatchProxy<IPlayerManager>.Create(out var proxy);
        proxy.SetMethodHandler(nameof(IPlayerManager.GetPlayer), args => players.GetValueOrDefault(
            args[0] is NexusForever.Game.Abstract.Identity identity ? identity.Id : Convert.ToUInt64(args[0])));
        var flags = BindingFlags.NonPublic | BindingFlags.Instance;
        typeof(GuildBase).GetField("playerManager", flags)!.SetValue(f.Guild, manager);
        var online = (List<ulong>)typeof(GuildBase).GetField("onlineMembers", flags)!.GetValue(f.Guild)!;
        online.AddRange(players.Keys); return new(players, events.ToArray(), errors);
    }
    private static void BankOperate(Fixture f, GuildOperation kind, ulong data, uint rank = 2, IPlayer actor = null)
    {
        using var bytes = new MemoryStream();
        using (var writer = new GamePacketWriter(bytes))
        {
            new NexusForever.Network.World.Message.Model.Shared.Identity { Id = 77, RealmId = 1 }.Write(writer);
            writer.Write(rank); writer.Write(data); writer.WriteStringWide(""); writer.Write(kind, 6u); writer.FlushBits();
        }
        using var input = new MemoryStream(bytes.ToArray()); using var reader = new GamePacketReader(input);
        var operation = new ClientGuildOperation(); operation.Read(reader);
        new ClientGuildOperationHandler(f.Manager).HandleMessage((IWorldSession)(actor ?? f.Leader).Session, operation);
    }
    private static IEnumerable<T> BankPackets<T>(IPlayer player) => ((RecordingDispatchProxy<IWorldSession>)(object)player.Session)
        .GetInvocations(nameof(IGameSession.EnqueueMessageEncrypted)).Select(row => row.Arguments[0]).OfType<T>();
    private static async Task BankPump(BankSessions world)
    {
        var timer = Stopwatch.StartNew();
        do { foreach (var events in world.Queues) events.Update(.01); await Task.Delay(5); Assert.True(timer.Elapsed < TimeSpan.FromSeconds(10)); }
        while (world.Queues.Any(events => events.PendingEvents));
        Assert.Empty(world.Errors);
    }
}
