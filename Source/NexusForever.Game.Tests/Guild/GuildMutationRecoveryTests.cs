using System.Reflection;
using System.Runtime.CompilerServices;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NexusForever.Database;
using NexusForever.Database.Character;
using NexusForever.Database.Character.Model;
using NexusForever.Database.Configuration.Model;
using NexusForever.Game.Abstract;
using NexusForever.Game.Abstract.Character;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Guild;
using NexusForever.Game.Guild;
using NexusForever.Game.Static.Guild;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.GameTable;
using NexusForever.GameTable.Model;
using NexusForever.GameTable.Text.Filter;
using NexusForever.Network;
using NexusForever.Network.Internal;
using NexusForever.Network.Internal.Message.Guild;
using NexusForever.Network.World.Message.Model.Guild;
using GameGuild = NexusForever.Game.Guild.Guild;

namespace NexusForever.Game.Tests.Guild;

public sealed partial class GuildMutationRecoveryTests : IDisposable
{
    private readonly string file = Path.Combine(Path.GetTempPath(), $"nf-guild-mutations-{Guid.NewGuid():N}.sqlite");
    private readonly List<string> extraFiles = [];
    private DatabaseConnectionString Config => new() { Provider = DatabaseProvider.Sqlite, ConnectionString = $"Data Source={file};Foreign Keys=True" };
    private CharacterContext Context() => new(Config);

    [Fact]
    public async Task PublicMotdOnlyEditReachesThePeriodicSave()
    {
        using Fixture f = await Create();
        Operate(f, GuildOperation.MessageOfTheDay, "Only MOTD");
        Assert.Equal("Only MOTD", f.Guild.MessageOfTheDay);
        await Flush(f.Manager);
        using var verify = Context();
        Assert.Equal("Only MOTD", (await verify.GuildData.SingleAsync()).MessageOfTheDay);
    }

    [Fact]
    public async Task PublicTaxToggleUsesItsParsedBooleanAndPersists()
    {
        using Fixture f = await Create();
        Operate(f, GuildOperation.TaxUpdate, data: 1);
        Assert.True(f.Guild.HasFlags(GuildFlag.Taxes));
        await Flush(f.Manager);
        using (var enabled = Context()) Assert.True(((GuildFlag)(await enabled.Guild.SingleAsync()).Flags).HasFlag(GuildFlag.Taxes));
        Operate(f, GuildOperation.TaxUpdate, data: 0);
        await Flush(f.Manager);
        using var disabled = Context();
        Assert.False(((GuildFlag)(await disabled.Guild.SingleAsync()).Flags).HasFlag(GuildFlag.Taxes));
    }

    [Fact]
    public async Task DirectOrdinarySavePersistsTheCompleteAcceptedEdit()
    {
        using Fixture f = await Create();
        f.Guild.SetTaxes(true);
        Operate(f, GuildOperation.MessageOfTheDay, "Direct MOTD");
        using (var staged = Context()) { f.Guild.Save(staged); await staged.SaveChangesAsync(); }
        using var verify = Context();
        Assert.Equal("Direct MOTD", (await verify.GuildData.SingleAsync()).MessageOfTheDay);
        Assert.True(((GuildFlag)(await verify.Guild.SingleAsync()).Flags).HasFlag(GuildFlag.Taxes));
        Assert.False(f.Guild.NeedsSave);
    }

    [Fact]
    public async Task FailedPeriodicUnitRetainsAcceptedHeaderDataRankAndMemberEdits()
    {
        using Fixture f = await Create();
        f.Guild.SetTaxes(true);
        Operate(f, GuildOperation.MessageOfTheDay, "Accepted MOTD");
        Operate(f, GuildOperation.AdditionalInfo, "Accepted info");
        Operate(f, GuildOperation.RankRename, "Accepted rank", 2);
        Operate(f, GuildOperation.EditPlayerNote, "Accepted note");
        using (var setup = Context())
            await setup.Database.ExecuteSqlRawAsync("CREATE TRIGGER reject_guild_data BEFORE UPDATE ON guild_guild_data BEGIN SELECT RAISE(ABORT, 'owned guild failure'); END");
        await Flush(f.Manager);
        Assert.True(f.Guild.NeedsSave);
        using (var failed = Context())
        {
            Assert.Equal("Old MOTD", (await failed.GuildData.SingleAsync()).MessageOfTheDay);
            Assert.Equal("Unused", (await failed.GuildRank.SingleAsync(row => row.Index == 2)).Name);
            Assert.Equal("Leader note", (await failed.GuildMember.SingleAsync(row => row.CharacterId == 42)).Note);
            await failed.Database.ExecuteSqlRawAsync("DROP TRIGGER reject_guild_data");
        }
        await Flush(f.Manager);
        using var verify = Context();
        Assert.True(((GuildFlag)(await verify.Guild.SingleAsync()).Flags).HasFlag(GuildFlag.Taxes));
        Assert.Equal("Accepted MOTD", (await verify.GuildData.SingleAsync()).MessageOfTheDay);
        Assert.Equal("Accepted info", (await verify.GuildData.SingleAsync()).AdditionalInfo);
        Assert.Equal("Accepted rank", (await verify.GuildRank.SingleAsync(row => row.Index == 2)).Name);
        Assert.Equal("Accepted note", (await verify.GuildMember.SingleAsync(row => row.CharacterId == 42)).Note);
        Assert.False(f.Guild.NeedsSave);
    }

    [Fact]
    public async Task FailedRankDeletionRetainsTheOriginalMemberlessRankForRetry()
    {
        using Fixture f = await Create();
        Operate(f, GuildOperation.RankDelete, rank: 2);
        using (var setup = Context())
            await setup.Database.ExecuteSqlRawAsync("CREATE TRIGGER reject_rank_delete BEFORE DELETE ON guild_rank BEGIN SELECT RAISE(ABORT, 'owned rank failure'); END");
        await Flush(f.Manager);
        Assert.True(f.Guild.NeedsSave);
        using (var failed = Context())
        {
            Assert.True(await failed.GuildRank.AnyAsync(row => row.Index == 2));
            await failed.Database.ExecuteSqlRawAsync("DROP TRIGGER reject_rank_delete");
        }
        await Flush(f.Manager);
        using var verify = Context();
        Assert.False(await verify.GuildRank.AnyAsync(row => row.Index == 2));
    }

    [Fact]
    public async Task FailedMemberDepartureRetainsDeletionForPeriodicRetry()
    {
        using Fixture f = await Create();
        f.Guild.LeaveGuild(f.Other, GuildResult.YouQuit);
        using (var setup = Context())
            await setup.Database.ExecuteSqlRawAsync("CREATE TRIGGER reject_member_delete BEFORE DELETE ON guild_member BEGIN SELECT RAISE(ABORT, 'owned member failure'); END");
        await Flush(f.Manager);
        Assert.True(f.Guild.NeedsSave);
        using (var failed = Context())
        {
            Assert.True(await failed.GuildMember.AnyAsync(row => row.CharacterId == 43));
            await failed.Database.ExecuteSqlRawAsync("DROP TRIGGER reject_member_delete");
        }
        await Flush(f.Manager);
        using var verify = Context();
        Assert.False(await verify.GuildMember.AnyAsync(row => row.CharacterId == 43));
        Assert.Null(f.Guild.GetMember(43));
    }

    [Fact]
    public async Task PublicOfflineKickFinishesWithoutDereferencingAnAbsentPlayer()
    {
        using Fixture f = await Create();
        Exception error = Record.Exception(() => Operate(f, GuildOperation.MemberRemove, "Other"));
        Assert.Null(error);
        await Flush(f.Manager);
        using var verify = Context();
        Assert.False(await verify.GuildMember.AnyAsync(row => row.CharacterId == 43));
    }

    [Fact]
    public async Task DeparturePublishesOnlyAfterItsPeriodicCommit()
    {
        using Fixture f = await Create();
        f.Guild.LeaveGuild(f.Other, GuildResult.YouQuit);
        Assert.Empty(Removals(f));
        Assert.Equal(1u, f.Guild.MemberCount);
        await Flush(f.Manager);
        Assert.Equal(43ul, Assert.Single(Removals(f)).Member.Id);
        using var verify = Context();
        Assert.False(await verify.GuildMember.AnyAsync(row => row.CharacterId == 43));
    }

    [Fact]
    public async Task PublicationFailureRetriesTheOriginalMessageWithoutRepeatingCommittedSql()
    {
        using Fixture f = await Create();
        bool fail = true;
        f.Messages.SetMethodHandler(nameof(IInternalMessagePublisher.PublishAsync), args =>
        {
            if (args[0] is GuildMemberRemovedMessage message && fail)
            {
                message.Member = new NexusForever.Network.Internal.Message.Shared.Identity { Id = 999, RealmId = 1 }; // A publisher cannot mutate the retained retry payload.
                return Task.FromException(new IOException("owned publication failure"));
            }
            return Task.CompletedTask;
        });
        f.Guild.LeaveGuild(f.Other, GuildResult.YouQuit);
        await Flush(f.Manager);
        Assert.True(f.Guild.NeedsSave);
        using (var committed = Context())
        {
            Assert.False(await committed.GuildMember.AnyAsync(row => row.CharacterId == 43));
            await committed.Database.ExecuteSqlRawAsync("CREATE TRIGGER reject_repeat BEFORE UPDATE ON guild BEGIN SELECT RAISE(ABORT, 'must not repeat committed mutation'); END");
        }
        fail = false;
        await Flush(f.Manager);
        Assert.Equal(2, Removals(f).Length);
        Assert.Equal(43ul, Removals(f)[1].Member.Id);
        Assert.False(f.Guild.NeedsSave);
    }

    [Fact]
    public async Task LaterAcceptedEditsSurviveAnEarlierExplicitUnitAndEquivalentResave()
    {
        using Fixture f = await Create();
        f.Guild.SetTaxes(true); Operate(f, GuildOperation.MessageOfTheDay, "Earlier"); Operate(f, GuildOperation.RankRename, "Earlier rank", 2);
        using (var staged = Context())
            await WithConfiguredStrategy(staged, async () =>
            {
                await using var transaction = await staged.Database.BeginTransactionAsync();
                f.Guild.Save(staged); await staged.SaveChangesAsync();
                f.Guild.SetTaxes(false); Operate(f, GuildOperation.MessageOfTheDay, "Later"); Operate(f, GuildOperation.RankRename, "Later rank", 2);
                f.Guild.Save(staged); await staged.SaveChangesAsync();
                Assert.Equal("Earlier", (await staged.GuildData.AsNoTracking().SingleAsync()).MessageOfTheDay);
                await transaction.CommitAsync();
            });
        Assert.True(f.Guild.NeedsSave);
        await Flush(f.Manager);
        using var verify = Context();
        Assert.Equal("Later", (await verify.GuildData.SingleAsync()).MessageOfTheDay);
        Assert.Equal("Later rank", (await verify.GuildRank.SingleAsync(row => row.Index == 2)).Name);
        Assert.False(((GuildFlag)(await verify.Guild.SingleAsync()).Flags).HasFlag(GuildFlag.Taxes));
        Assert.False(f.Guild.NeedsSave);
    }

    [Fact]
    public async Task CallerRollbackRetainsTheWholeMixedEditAndDeletionGraph()
    {
        using Fixture f = await Create();
        f.Guild.SetTaxes(true); Operate(f, GuildOperation.MessageOfTheDay, "Retained"); Operate(f, GuildOperation.RankDelete, rank: 2);
        f.Guild.LeaveGuild(f.Other, GuildResult.YouQuit);
        using (var staged = Context())
            await WithConfiguredStrategy(staged, async () =>
            {
                await using var transaction = await staged.Database.BeginTransactionAsync();
                f.Guild.Save(staged); await staged.SaveChangesAsync(); await transaction.RollbackAsync();
            });
        Assert.True(f.Guild.NeedsSave); Assert.Empty(Removals(f));
        await Flush(f.Manager);
        using var verify = Context();
        Assert.Equal("Retained", (await verify.GuildData.SingleAsync()).MessageOfTheDay);
        Assert.False(await verify.GuildRank.AnyAsync(row => row.Index == 2));
        Assert.False(await verify.GuildMember.AnyAsync(row => row.CharacterId == 43));
        Assert.Single(Removals(f));
    }

    [Fact]
    public async Task UnflushedCommitCannotAcknowledgeOrPublishAMemberDeparture()
    {
        using Fixture f = await Create();
        f.Guild.LeaveGuild(f.Other, GuildResult.YouQuit);
        using (var staged = Context())
            await WithConfiguredStrategy(staged, async () =>
            {
                await using var transaction = await staged.Database.BeginTransactionAsync();
                f.Guild.Save(staged);
                await Assert.ThrowsAsync<InvalidOperationException>(() => transaction.CommitAsync());
            });
        Assert.True(f.Guild.NeedsSave); Assert.Empty(Removals(f));
        await Flush(f.Manager);
        Assert.Single(Removals(f));
    }

    [Fact]
    public async Task LateSqlTamperingRejectsTheWholeCapturedGraph()
    {
        using Fixture f = await Create();
        Operate(f, GuildOperation.MessageOfTheDay, "Captured");
        using (var staged = Context())
            await WithConfiguredStrategy(staged, async () =>
            {
                await using var transaction = await staged.Database.BeginTransactionAsync();
                f.Guild.Save(staged); await staged.SaveChangesAsync();
                await staged.Database.ExecuteSqlRawAsync("UPDATE guild_member SET note='foreign' WHERE id=77 AND characterId=42");
                await Assert.ThrowsAsync<InvalidOperationException>(() => transaction.CommitAsync());
            });
        await Flush(f.Manager);
        using var verify = Context();
        Assert.Equal("Captured", (await verify.GuildData.SingleAsync()).MessageOfTheDay);
        Assert.Equal("Leader note", (await verify.GuildMember.SingleAsync(row => row.CharacterId == 42)).Note);
    }

    [Fact]
    public async Task CaughtForeignRankEnrollmentCannotCommitOnlyTheHeader()
    {
        using Fixture f = await Create();
        f.Guild.SetTaxes(true); Operate(f, GuildOperation.RankRename, "Accepted", 2);
        using (var staged = Context())
        {
            staged.Attach(new GuildRankModel { Id = 77, Index = 2, Name = "Foreign tracked rank" });
            Assert.Throws<InvalidOperationException>(() => f.Guild.Save(staged));
            await Assert.ThrowsAsync<InvalidOperationException>(() => staged.SaveChangesAsync());
            Assert.True(staged.IsSaveRejected);
        }
        using (var unchanged = Context()) Assert.False(((GuildFlag)(await unchanged.Guild.SingleAsync()).Flags).HasFlag(GuildFlag.Taxes));
        await Flush(f.Manager);
        using var verify = Context();
        Assert.True(((GuildFlag)(await verify.Guild.SingleAsync()).Flags).HasFlag(GuildFlag.Taxes));
        Assert.Equal("Accepted", (await verify.GuildRank.SingleAsync(row => row.Index == 2)).Name);
    }

    [Fact]
    public async Task BoundDirectMemberSavePreservesSiblingRankAndGuildDataIntent()
    {
        using Fixture f = await Create();
        IGuildMember member = f.Guild.GetMember(42);
        member.Note = "Direct note"; member.Rank.Name = "Direct leader";
        Operate(f, GuildOperation.MessageOfTheDay, "Direct sibling");
        using (var staged = Context()) { member.Save(staged); await staged.SaveChangesAsync(); }
        using var verify = Context();
        Assert.Equal("Direct sibling", (await verify.GuildData.SingleAsync()).MessageOfTheDay);
        Assert.Equal("Direct leader", (await verify.GuildRank.SingleAsync(row => row.Index == 0)).Name);
        Assert.Equal("Direct note", (await verify.GuildMember.SingleAsync(row => row.CharacterId == 42)).Note);
        Assert.False(member.NeedsSave); Assert.False(member.Rank.NeedsSave);
    }

    [Fact]
    public async Task BoundChildSaveRejectsAForeignDatabaseWithIdenticalRows()
    {
        using Fixture f = await Create();
        string foreignFile = file + ".foreign"; extraFiles.Add(foreignFile);
        var foreignConfig = new DatabaseConnectionString { Provider = DatabaseProvider.Sqlite, ConnectionString = $"Data Source={foreignFile};Foreign Keys=True" };
        var original = new CharacterDatabase(); original.Initialise(Config);
        using (var foreign = new CharacterContext(foreignConfig))
        {
            await foreign.Database.EnsureCreatedAsync();
            foreign.Character.AddRange(new CharacterModel { Id = 42, Name = "Leader" }, new CharacterModel { Id = 43, Name = "Other" });
            foreign.Guild.Add(original.GetGuilds().Single()); await foreign.SaveChangesAsync();
            f.Guild.GetMember(42).Note = "Original route";
            Assert.Throws<InvalidOperationException>(() => f.Guild.GetMember(42).Save(foreign));
            Assert.True(foreign.IsSaveRejected);
            Assert.Equal("Leader note", (await foreign.GuildMember.AsNoTracking().SingleAsync(row => row.CharacterId == 42)).Note);
        }
        await Flush(f.Manager);
        using var verify = Context();
        Assert.Equal("Original route", (await verify.GuildMember.SingleAsync(row => row.CharacterId == 42)).Note);
    }

    [Fact]
    public async Task SuppressingATrackedMemberWriteCannotAcknowledgeItsAcceptedValue()
    {
        using Fixture f = await Create();
        f.Guild.GetMember(42).Note = "Retained note";
        using (var staged = Context())
        {
            f.Guild.Save(staged);
            staged.ChangeTracker.Entries<GuildMemberModel>().Single().Property(row => row.Note).IsModified = false;
            await Assert.ThrowsAsync<InvalidOperationException>(() => staged.SaveChangesAsync());
        }
        Assert.True(f.Guild.NeedsSave);
        await Flush(f.Manager);
        using var verify = Context();
        Assert.Equal("Retained note", (await verify.GuildMember.SingleAsync(row => row.CharacterId == 42)).Note);
    }

    [Fact]
    public async Task RejoinAfterAnEarlierDeleteFlushKeepsItsLaterMembershipIntent()
    {
        using Fixture f = await Create();
        IGuildMember original = f.Guild.GetMember(43);
        f.Guild.LeaveGuild(f.Other, GuildResult.YouQuit);
        using (var staged = Context())
            await WithConfiguredStrategy(staged, async () =>
            {
                await using var transaction = await staged.Database.BeginTransactionAsync();
                f.Guild.Save(staged); await staged.SaveChangesAsync();
                f.Guild.JoinGuild(f.Other);
                Assert.Same(original, f.Guild.GetMember(43));
                f.Guild.Save(staged); await staged.SaveChangesAsync();
                await transaction.CommitAsync();
            });
        Assert.True(f.Guild.NeedsSave);
        await Flush(f.Manager);
        using var verify = Context();
        Assert.True(await verify.GuildMember.AnyAsync(row => row.CharacterId == 43));
        Assert.Equal(2u, f.Guild.MemberCount); Assert.False(f.Guild.NeedsSave);
        object[] published = f.Messages.GetInvocations(nameof(IInternalMessagePublisher.PublishAsync)).Select(call => call.Arguments[0])
            .Where(message => message is GuildMemberRemovedMessage or GuildMemberAddedMessage).ToArray();
        Assert.Collection(published, message => Assert.IsType<GuildMemberRemovedMessage>(message), message => Assert.IsType<GuildMemberAddedMessage>(message));
    }

    [Fact]
    public async Task LaterDeletionOfANewRankSurvivesItsEarlierInsertCommit()
    {
        using Fixture f = await Create();
        Operate(f, GuildOperation.RankAdd, "New rank", 3);
        using (var staged = Context())
            await WithConfiguredStrategy(staged, async () =>
            {
                await using var transaction = await staged.Database.BeginTransactionAsync();
                f.Guild.Save(staged); await staged.SaveChangesAsync();
                Operate(f, GuildOperation.RankDelete, rank: 3);
                await transaction.CommitAsync();
            });
        Assert.True(f.Guild.NeedsSave);
        await Flush(f.Manager);
        using var verify = Context(); Assert.False(await verify.GuildRank.AnyAsync(row => row.Index == 3));
    }

    [Fact]
    public async Task HeldPublicationKeepsThePeriodicOriginalActorInFlight()
    {
        using Fixture f = await Create();
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        int attempts = 0;
        f.Messages.SetMethodHandler(nameof(IInternalMessagePublisher.PublishAsync), args =>
        {
            if (args[0] is not GuildMemberRemovedMessage) return Task.CompletedTask;
            Interlocked.Increment(ref attempts); entered.TrySetResult(); return release.Task;
        });
        f.Guild.LeaveGuild(f.Other, GuildResult.YouQuit); f.Manager.Update(61);
        try
        {
            await entered.Task.WaitAsync(TimeSpan.FromSeconds(10));
            Operate(f, GuildOperation.MessageOfTheDay, "After publication"); f.Manager.Update(61);
            using var held = Context(); Assert.Equal("Old MOTD", (await held.GuildData.SingleAsync()).MessageOfTheDay);
            Assert.Equal(1, Volatile.Read(ref attempts));
        }
        finally { release.TrySetResult(); }
        await Flush(f.Manager); await Flush(f.Manager);
        using var verify = Context(); Assert.Equal("After publication", (await verify.GuildData.SingleAsync()).MessageOfTheDay);
        Assert.Equal(1, attempts);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task AcceptedDisbandCannotSendABoundChildWriteIntoAForeignDatabase(bool rank)
    {
        using Fixture f = await Create();
        IGuildMember retained = f.Guild.GetMember(42);
        string foreignFile = file + ".disband-foreign"; extraFiles.Add(foreignFile);
        var config = new DatabaseConnectionString { Provider = DatabaseProvider.Sqlite, ConnectionString = $"Data Source={foreignFile};Foreign Keys=True" };
        using var foreign = new CharacterContext(config); await foreign.Database.EnsureCreatedAsync();
        var original = new CharacterDatabase(); original.Initialise(Config);
        foreign.Character.AddRange(new CharacterModel { Id = 42, Name = "Leader" }, new CharacterModel { Id = 43, Name = "Other" });
        foreign.Guild.Add(original.GetGuilds().Single()); await foreign.SaveChangesAsync();
        f.Guild.DisbandGuild();
        if (rank) retained.Rank.Name = "Foreign route attempt"; else retained.Note = "Foreign route attempt";
        Assert.Throws<InvalidOperationException>(() => { if (rank) retained.Rank.Save(foreign); else retained.Save(foreign); });
        Assert.True(foreign.IsSaveRejected);
        Assert.Equal("Leader note", (await foreign.GuildMember.AsNoTracking().SingleAsync(row => row.CharacterId == 42)).Note);
        Assert.Equal("Leader", (await foreign.GuildRank.AsNoTracking().SingleAsync(row => row.Index == 0)).Name);
    }

    [Fact]
    public async Task OrdinaryHeaderEditCannotAdmitAnUnacceptedTrackedName()
    {
        using Fixture f = await Create(); f.Guild.SetTaxes(true);
        using (var staged = Context())
        {
            (await staged.Guild.SingleAsync()).Name = "Foreign name";
            Exception error = Record.Exception(() => f.Guild.Save(staged));
            if (error == null) error = await Record.ExceptionAsync(() => staged.SaveChangesAsync());
            Assert.IsType<InvalidOperationException>(error);
        }
        using (var unchanged = Context()) Assert.Equal("Guild", (await unchanged.Guild.SingleAsync()).Name);
        Assert.Same(f.Guild, f.Manager.GetGuild(GuildType.Guild, "Guild"));
        await Flush(f.Manager);
        using var verify = Context(); Assert.True(((GuildFlag)(await verify.Guild.SingleAsync()).Flags).HasFlag(GuildFlag.Taxes));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task FailedDisbandRetainsEarlierOrdinaryEditsAndPendingCollectionDeletes(bool initialTaxes)
    {
        using Fixture f = await Create(initialTaxes);
        f.Guild.SetTaxes(!initialTaxes); Operate(f, GuildOperation.MessageOfTheDay, "Archive MOTD");
        Operate(f, GuildOperation.EditPlayerNote, "Archive note"); Operate(f, GuildOperation.RankDelete, rank: 2);
        f.Guild.LeaveGuild(f.Other, GuildResult.YouQuit); f.Guild.DisbandGuild();
        using (var setup = Context())
            await setup.Database.ExecuteSqlRawAsync("CREATE TRIGGER reject_tombstone BEFORE UPDATE ON guild BEGIN SELECT RAISE(ABORT, 'owned tombstone failure'); END");
        await Flush(f.Manager);
        using (var failed = Context())
        {
            Assert.Null((await failed.Guild.SingleAsync()).DeleteTime);
            await failed.Database.ExecuteSqlRawAsync("DROP TRIGGER reject_tombstone");
        }
        await Flush(f.Manager);
        using var verify = Context();
        Assert.NotNull((await verify.Guild.SingleAsync()).DeleteTime);
        Assert.Equal("Archive MOTD", (await verify.GuildData.SingleAsync()).MessageOfTheDay);
        Assert.Equal("Archive note", (await verify.GuildMember.SingleAsync(row => row.CharacterId == 42)).Note);
        Assert.False(await verify.GuildMember.AnyAsync(row => row.CharacterId == 43));
        Assert.False(await verify.GuildRank.AnyAsync(row => row.Index == 2));
        Assert.Equal(!initialTaxes, ((GuildFlag)(await verify.Guild.SingleAsync()).Flags).HasFlag(GuildFlag.Taxes));
        Assert.Equal(new ulong[] { 42, 43 }, Removals(f).Select(message => message.Member.Id).ToArray());
    }

    [Fact]
    public async Task PublicPromotionSkipsARankRetainedForFailedDeletion()
    {
        using Fixture f = await Create();
        Operate(f, GuildOperation.RankAdd, "Council", 1);
        Operate(f, GuildOperation.RankDelete, rank: 2);
        using (var setup = Context())
            await setup.Database.ExecuteSqlRawAsync("CREATE TRIGGER reject_rank BEFORE DELETE ON guild_rank BEGIN SELECT RAISE(ABORT, 'owned retained rank failure'); END");
        await Flush(f.Manager);
        Operate(f, GuildOperation.MemberPromote, "Other");
        Assert.Equal((byte)1, f.Guild.GetMember(43).Rank.Index);
        using (var setup = Context()) await setup.Database.ExecuteSqlRawAsync("DROP TRIGGER reject_rank");
        await Flush(f.Manager);
        using var verify = Context(); Assert.Equal((byte)1, (await verify.GuildMember.SingleAsync(row => row.CharacterId == 43)).Rank);
        Assert.False(await verify.GuildRank.AnyAsync(row => row.Index == 2));
    }

    [Fact]
    public async Task LaterAcceptedDisbandEditsPersistBeforeCleanupAndRetirement()
    {
        using Fixture f = await Create();
        IGuildMember retained = f.Guild.GetMember(42);
        f.Guild.DisbandGuild();
        using (var staged = Context())
            await WithConfiguredStrategy(staged, async () =>
            {
                await using var transaction = await staged.Database.BeginTransactionAsync();
                f.Guild.Save(staged);
                retained.Note = "Later archive note"; f.Guild.MessageOfTheDay = "Later archive MOTD";
                await staged.SaveChangesAsync(); await transaction.CommitAsync();
            });
        Assert.Empty(Removals(f));
        int cleanupCallbacks = 0;
        f.Messages.SetMethodHandler(nameof(IInternalMessagePublisher.PublishAsync), args =>
        {
            if (args[0] is GuildMemberRemovedMessage)
            {
                cleanupCallbacks++;
                Assert.Throws<InvalidOperationException>(() => retained.Note = "Too late");
                Assert.Throws<InvalidOperationException>(() => f.Guild.MessageOfTheDay = "Too late");
                Assert.Throws<InvalidOperationException>(() => f.Guild.Standard = f.Guild.Standard);
                Assert.Throws<InvalidOperationException>(() => f.Guild.SetTaxes(true));
            }
            return Task.CompletedTask;
        });
        await Flush(f.Manager);
        Assert.Equal(2, cleanupCallbacks);
        using var verify = Context();
        Assert.Equal("Later archive note", (await verify.GuildMember.SingleAsync(row => row.CharacterId == 42)).Note);
        Assert.Equal("Later archive MOTD", (await verify.GuildData.SingleAsync()).MessageOfTheDay);
        Assert.Null(f.Manager.GetGuild(77));
        Assert.Equal(new ulong[] { 42, 43 }, Removals(f).Select(message => message.Member.Id).ToArray());
    }

    [Fact]
    public async Task DisbandRejectsForeignTrackedTypeBeforeAnyFlush()
    {
        using Fixture f = await Create(); f.Guild.DisbandGuild();
        using (var staged = Context())
        {
            (await staged.Guild.SingleAsync()).Type = (byte)GuildType.Circle;
            Exception error = Record.Exception(() => f.Guild.Save(staged));
            if (error == null) error = await Record.ExceptionAsync(() => staged.SaveChangesAsync());
            Assert.IsType<InvalidOperationException>(error);
        }
        using var verify = Context();
        GuildModel row = await verify.Guild.SingleAsync();
        Assert.Equal((byte)GuildType.Guild, row.Type); Assert.Null(row.DeleteTime);
        Assert.Empty(Removals(f));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task LateDisbandRecoveryRejectsForeignTombstoneIntent(bool headerChange)
    {
        using Fixture f = await Create(); f.Guild.DisbandGuild();
        DateTime deletedAt;
        using (var first = Context())
        {
            f.Guild.Save(first); await first.SaveChangesAsync();
            deletedAt = (await first.Guild.SingleAsync()).DeleteTime!.Value;
        }
        if (headerChange) f.Guild.SetTaxes(true); else f.Guild.GetMember(42).Note = "Later accepted note";
        using (var staged = Context())
        {
            (await staged.Guild.SingleAsync()).DeleteTime = null;
            Exception error = Record.Exception(() => f.Guild.Save(staged));
            if (error == null) error = await Record.ExceptionAsync(() => staged.SaveChangesAsync());
            Assert.IsType<InvalidOperationException>(error);
        }
        using (var unchanged = Context()) Assert.Equal(deletedAt, (await unchanged.Guild.SingleAsync()).DeleteTime);
        Assert.Empty(Removals(f));
        await Flush(f.Manager);
        using var verify = Context();
        GuildModel row = await verify.Guild.SingleAsync();
        Assert.Equal(deletedAt, row.DeleteTime); Assert.Null(row.Name); Assert.Null(row.LeaderId);
        Assert.Equal("Guild", row.OriginalName); Assert.Equal(42ul, row.OriginalLeaderId);
        if (headerChange) Assert.True(((GuildFlag)row.Flags).HasFlag(GuildFlag.Taxes));
        else Assert.Equal("Later accepted note", (await verify.GuildMember.SingleAsync(member => member.CharacterId == 42)).Note);
        Assert.Null(f.Manager.GetGuild(77));
    }

    private static GuildMemberRemovedMessage[] Removals(Fixture f) => f.Messages.GetInvocations(nameof(IInternalMessagePublisher.PublishAsync))
        .Select(call => call.Arguments[0]).OfType<GuildMemberRemovedMessage>().ToArray();

    private static Task WithConfiguredStrategy(CharacterContext context, Func<Task> action)
    {
        bool entered = false;
        return context.Database.CreateExecutionStrategy().ExecuteAsync(async () =>
        {
            if (entered) throw new InvalidOperationException("The guild fixture cannot replay gameplay callbacks.");
            entered = true; await action();
        });
    }

    private async Task<Fixture> Create(bool initialTaxes = false)
    {
        using (var seed = Context())
        {
            await seed.Database.EnsureCreatedAsync();
            seed.Character.AddRange(new CharacterModel { Id = 42, Name = "Leader" }, new CharacterModel { Id = 43, Name = "Other" });
            seed.Guild.Add(new GuildModel
            {
                Id = 77, Type = (byte)GuildType.Guild, Name = "Guild", LeaderId = 42, Flags = initialTaxes ? (uint)GuildFlag.Taxes : 0,
                GuildData = new GuildDataModel { Id = 77, MessageOfTheDay = "Old MOTD", AdditionalInfo = "Old info", BackgroundIconPartId = 1, ForegroundIconPartId = 2, ScanLinesPartId = 3 },
                GuildRank = [new GuildRankModel { Id = 77, Index = 0, Name = "Leader", Permission = (uint)GuildRankPermission.Leader },
                    new GuildRankModel { Id = 77, Index = 2, Name = "Unused" }, new GuildRankModel { Id = 77, Index = 9, Name = "Member" }],
                GuildMember = [new GuildMemberModel { Id = 77, CharacterId = 42, Rank = 0, Note = "Leader note" },
                    new GuildMemberModel { Id = 77, CharacterId = 43, Rank = 9, Note = "Other note" }]
            });
            await seed.SaveChangesAsync();
        }
        var database = new CharacterDatabase(); database.Initialise(Config);
        var databases = RecordingDispatchProxy<IDatabaseManager>.Create(out var db); db.SetMethodReturn(nameof(IDatabaseManager.GetDatabase), database);
        var realm = RecordingDispatchProxy<IRealmContext>.Create(out var realmProxy); realmProxy.SetProperty(nameof(IRealmContext.RealmId), (ushort)1);
        var publisher = RecordingDispatchProxy<IInternalMessagePublisher>.Create(out var messages); messages.SetMethodReturn(nameof(IInternalMessagePublisher.PublishAsync), Task.CompletedTask);
        var filter = RecordingDispatchProxy<ITextFilterManager>.Create(out var text); text.SetMethodReturn(nameof(ITextFilterManager.IsTextValid), true);
        var tables = RecordingDispatchProxy<IGameTableManager>.Create(out var tableProxy);
        tableProxy.SetProperty(nameof(IGameTableManager.GuildStandardPart), Parts());
        var characters = RecordingDispatchProxy<ICharacterManager>.Create(out var characterProxy);
        characterProxy.SetMethodHandler(nameof(ICharacterManager.GetCharacterIdByName), args => (string)args[0] == "Other" ? (ulong?)43 : 42ul);
        characterProxy.SetMethodHandler(nameof(ICharacterManager.GetCharacter), args =>
        {
            var character = RecordingDispatchProxy<ICharacter>.Create(out var value);
            value.SetProperty(nameof(ICharacter.Name), (ulong)args[0] == 42 ? "Leader" : "Other");
            return character;
        });
        var registrations = new ServiceCollection();
        registrations.AddSingleton<IGuildFactory, GuildFactory>();
        registrations.AddSingleton<IGlobalGuildManager>(provider => new GlobalGuildManager(provider.GetRequiredService<IGuildFactory>(), databases));
        registrations.AddKeyedTransient<IGuildBase>(GuildType.Guild, (provider, _) => new GameGuild(realm, publisher, filter,
            characterManager: characters, gameTableManager: tables, globalGuildManager: provider.GetRequiredService<IGlobalGuildManager>()));
        var services = registrations.BuildServiceProvider();
        var manager = (GlobalGuildManager)services.GetRequiredService<IGlobalGuildManager>(); manager.Initialise();
        IPlayer Player(ulong id)
        {
            var player = RecordingDispatchProxy<IPlayer>.Create(out var proxy);
            proxy.SetProperty(nameof(IPlayer.CharacterId), id); proxy.SetProperty(nameof(IPlayer.Name), id == 42 ? "Leader" : "Other");
            proxy.SetProperty(nameof(IPlayer.Identity), new NexusForever.Game.Abstract.Identity { Id = id, RealmId = 1 });
            var guilds = RecordingDispatchProxy<IGuildManager>.Create(out var guildProxy);
            guildProxy.SetProperty(nameof(IGuildManager.GuildAffiliation), manager.GetGuild(77));
            proxy.SetProperty(nameof(IPlayer.GuildManager), guilds);
            proxy.SetProperty(nameof(IPlayer.Session), RecordingDispatchProxy<NexusForever.WorldServer.Network.IWorldSession>.Create(out _));
            return player;
        }
        return new(services, manager, Assert.IsType<GameGuild>(manager.GetGuild(77)), Player(42), Player(43), messages);
    }

    private static GameTable<GuildStandardPartEntry> Parts()
    {
        var table = (GameTable<GuildStandardPartEntry>)RuntimeHelpers.GetUninitializedObject(typeof(GameTable<GuildStandardPartEntry>));
        var rows = new[] { new GuildStandardPartEntry { Id = 1 }, new GuildStandardPartEntry { Id = 2 }, new GuildStandardPartEntry { Id = 3 } };
        typeof(GameTable<GuildStandardPartEntry>).GetField("<Entries>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(table, rows);
        typeof(GameTable<GuildStandardPartEntry>).GetField("header", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(table, new GameTableHeader { MaxId = 4 });
        typeof(GameTable<GuildStandardPartEntry>).GetField("lookup", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(table, new[] { -1, 0, 1, 2 });
        return table;
    }

    private static void Operate(Fixture f, GuildOperation kind, string text = "", uint rank = 0, ulong data = 0)
    {
        using var bytes = new MemoryStream();
        using (var writer = new GamePacketWriter(bytes))
        {
            new NexusForever.Network.World.Message.Model.Shared.Identity { Id = 77, RealmId = 1 }.Write(writer);
            writer.Write(rank); writer.Write(data); writer.WriteStringWide(text); writer.Write(kind, 6u); writer.FlushBits();
        }
        using var input = new MemoryStream(bytes.ToArray()); using var reader = new GamePacketReader(input);
        var operation = new ClientGuildOperation(); operation.Read(reader); f.Manager.HandleGuildOperation(f.Leader, operation);
    }

    private static async Task Flush(GlobalGuildManager manager)
    {
        manager.Update(61);
        object gate = typeof(GlobalGuildManager).GetField("cacheLock", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(manager);
        var active = (HashSet<ulong>)typeof(GlobalGuildManager).GetField("guildSavesInFlight", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(manager);
        for (int i = 0; i < 500; i++) { lock (gate) if (active.Count == 0) return; await Task.Delay(10); }
        Assert.Fail("Periodic guild save did not settle.");
    }
    private sealed record Fixture(ServiceProvider Services, GlobalGuildManager Manager, GameGuild Guild, IPlayer Leader, IPlayer Other, RecordingDispatchProxy<IInternalMessagePublisher> Messages) : IDisposable
    { public void Dispose() => Services.Dispose(); }
    public void Dispose()
    {
        using var pool = new Microsoft.Data.Sqlite.SqliteConnection(Config.ConnectionString); Microsoft.Data.Sqlite.SqliteConnection.ClearPool(pool);
        if (File.Exists(file)) File.Delete(file);
        foreach (string extra in extraFiles)
        {
            using var foreignPool = new Microsoft.Data.Sqlite.SqliteConnection($"Data Source={extra};Foreign Keys=True");
            Microsoft.Data.Sqlite.SqliteConnection.ClearPool(foreignPool);
            if (File.Exists(extra)) File.Delete(extra);
        }
    }
}
