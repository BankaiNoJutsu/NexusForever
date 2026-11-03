using System.Reflection;
using System.Runtime.CompilerServices;
using System.Data.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using NexusForever.Database;
using NexusForever.Database.Character;
using NexusForever.Database.Character.Model;
using NexusForever.Database.Configuration.Model;
using NexusForever.Game.Abstract;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Guild;
using NexusForever.Game.Abstract.Housing;
using NexusForever.Game.Abstract.Map.Instance;
using NexusForever.Game.Abstract.Map.Lock;
using NexusForever.Game.Guild;
using NexusForever.Game.Housing;
using NexusForever.Game.Static.Guild;
using NexusForever.Game.Static.Housing;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.GameTable;
using NexusForever.GameTable.Model;
using NexusForever.GameTable.Text.Filter;
using NexusForever.Network;
using NexusForever.Network.Internal;
using NexusForever.Network.World.Message.Model;
using NexusForever.Network.World.Message.Model.Shared;
using NexusForever.WorldServer.Network;
using NexusForever.WorldServer.Network.Message.Handler.Housing;

namespace NexusForever.Game.Tests.Housing;

public sealed class HousingDonationRecoveryTests : IDisposable
{
    private readonly string file = Path.Combine(Path.GetTempPath(), $"nf-housing-donation-{Guid.NewGuid():N}.sqlite");
    private DatabaseConnectionString Config => new() { Provider = DatabaseProvider.Sqlite, ConnectionString = $"Data Source={file};Foreign Keys=True" };
    private CharacterContext Context() => new(Config);

    [Fact]
    public async Task SuccessfulPublicDonationMovesOneDecorAndReturnsItsOriginalIds()
    {
        Fixture f = await Create(); Donate(f, f.Decor.DecorId); await Flush(f.Manager);
        using var verify = Context(); ResidenceDecor row = Assert.Single(await verify.ResidenceDecor.ToListAsync());
        Assert.Equal(f.Recipient.Id, row.Id); Assert.Equal(f.Decor.DecorInfoId, row.DecorInfoId);
        var notice = Assert.Single(Messages(f).OfType<ServerHousingCommunityDonateUpdate>());
        var entry = Assert.Single(notice.Entries); Assert.Equal(unchecked((uint)f.Decor.DecorId), entry.Value0); Assert.Equal(unchecked((uint)row.DecorId), entry.Value1);
    }

    [Fact]
    public async Task RawDecorMetadataSurvivesThePublicDonation()
    {
        Fixture f = await Create(); Donate(f, f.Decor.DecorId); await Flush(f.Manager);
        using var verify = Context(); ResidenceDecor row = Assert.Single(await verify.ResidenceDecor.ToListAsync());
        Assert.Equal(0x80000005u, row.DecorData); Assert.Equal(3u, row.HookBagIndex); Assert.Equal(4u, row.HookIndex);
        Assert.Equal(0x80000003u, row.PlotIndex); Assert.Equal(1.25f, row.Scale); Assert.Equal(-3.75f, row.Z);
        Assert.Equal(0.4f, row.Qw); Assert.Equal(8u, row.ActivePropUnitId); Assert.Equal(444ul, row.DecorParentId); Assert.Equal((ushort)7, row.ColourShiftId);
    }

    [Fact]
    public async Task DuplicateSourceIdsRejectBeforeAnyPartialMutation()
    {
        Fixture f = await Create();
        Assert.Throws<InvalidPacketValueException>(() => Donate(f, f.Decor.DecorId, f.Decor.DecorId));
        Assert.Same(f.Decor, f.Donor.GetDecor(f.Decor.DecorId)); Assert.False(f.Decor.PendingDelete);
        Assert.Empty(f.Recipient.GetDecor()); Assert.Empty(Messages(f).OfType<ServerHousingCommunityDonateUpdate>());
        await Flush(f.Manager); using var verify = Context(); Assert.Equal(f.Donor.Id, (await verify.ResidenceDecor.SingleAsync()).Id);
    }

    [Fact]
    public async Task ASourceAlreadyPendingDeletionCannotBeDonatedAgain()
    {
        Fixture f = await Create(); f.Decor.EnqueueDelete(true);
        Assert.Throws<InvalidPacketValueException>(() => Donate(f, f.Decor.DecorId));
        Assert.Empty(f.Recipient.GetDecor()); Assert.Empty(Messages(f).OfType<ServerHousingCommunityDonateUpdate>());
    }

    [Fact]
    public async Task SuccessfulReceiptNeverPrecedesTheCommittedPair()
    {
        Fixture f = await Create(); bool premature = false;
        f.Session.SetMethodHandler(nameof(IWorldSession.EnqueueMessageEncrypted), args =>
        {
            if (args[0] is ServerHousingCommunityDonateUpdate)
            {
                using var probe = Context();
                premature |= probe.ResidenceDecor.Any(row => row.Id == f.Donor.Id)
                    || probe.ResidenceDecor.Count(row => row.Id == f.Recipient.Id) != 1;
            }
            return null;
        });
        Donate(f, f.Decor.DecorId); await Flush(f.Manager);
        Assert.False(premature, "Donation replied before both residence changes were durable.");
        Assert.Single(Messages(f).OfType<ServerHousingCommunityDonateUpdate>());
    }

    [Fact]
    public async Task SqlContinuationCannotApplyMemoryOrPublishOutsideTheWorldUpdate()
    {
        Fixture f = await Create(); Donate(f, f.Decor.DecorId); f.Manager.Update(61); await WaitForSql(f.Manager);
        using (var verify = Context()) Assert.Equal(f.Recipient.Id, (await verify.ResidenceDecor.SingleAsync()).Id);
        Assert.Same(f.Decor, f.Donor.GetDecor(f.Decor.DecorId)); Assert.Empty(f.Recipient.GetDecor());
        Assert.Empty(Messages(f).OfType<ServerHousingCommunityDonateUpdate>());
        f.Manager.Update(0);
        Assert.Null(f.Donor.GetDecor(f.Decor.DecorId)); Assert.Single(f.Recipient.GetDecor());
        Assert.Single(Messages(f).OfType<ServerHousingCommunityDonateUpdate>());
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task FailedOneSidedWriteCannotCommitTheOtherResidence(bool failDonor)
    {
        Fixture f = await Create();
        using (var setup = Context())
            await setup.Database.ExecuteSqlRawAsync(failDonor
                ? "CREATE TRIGGER reject_donation BEFORE DELETE ON residence_decor WHEN OLD.id IN (SELECT id FROM residence WHERE ownerId IS NOT NULL) BEGIN SELECT RAISE(ABORT, 'owned donor failure'); END"
                : "CREATE TRIGGER reject_donation BEFORE INSERT ON residence_decor WHEN NEW.id IN (SELECT id FROM residence WHERE guildOwnerId IS NOT NULL AND ownerId IS NULL) BEGIN SELECT RAISE(ABORT, 'owned recipient failure'); END");
        Donate(f, f.Decor.DecorId); await Flush(f.Manager);
        using (var failed = Context())
        {
            Assert.Equal(f.Donor.Id, (await failed.ResidenceDecor.SingleAsync()).Id);
            Assert.Empty(Messages(f).OfType<ServerHousingCommunityDonateUpdate>());
            await failed.Database.ExecuteSqlRawAsync("DROP TRIGGER reject_donation");
        }
        await Flush(f.Manager);
        using var verify = Context(); Assert.Equal(f.Recipient.Id, (await verify.ResidenceDecor.SingleAsync()).Id);
        Assert.Single(Messages(f).OfType<ServerHousingCommunityDonateUpdate>());
    }

    [Fact]
    public async Task AcceptedProposalIsInvisibleAndItsSelectedSourceCannotChangeOrBeReused()
    {
        Fixture f = await Create(); Donate(f, f.Decor.DecorId);
        Assert.Same(f.Decor, f.Donor.GetDecor(f.Decor.DecorId)); Assert.Empty(f.Recipient.GetDecor());
        Assert.False(f.Decor.PendingDelete); Assert.Empty(Messages(f).OfType<ServerHousingCommunityDonateUpdate>());
        Assert.Throws<InvalidOperationException>(() => f.Decor.ColourShiftId = 99);
        Assert.Throws<InvalidOperationException>(() => f.Decor.EnqueueDelete(true));
        Assert.Throws<InvalidPacketValueException>(() => Donate(f, f.Decor.DecorId));
        await Flush(f.Manager);
        Assert.Null(f.Donor.GetDecor(f.Decor.DecorId)); Assert.False(Assert.Single(f.Recipient.GetDecor()).PendingCreate);
        using var verify = Context(); Assert.Equal(f.Recipient.Id, (await verify.ResidenceDecor.SingleAsync()).Id);
    }

    [Fact]
    public async Task AProvisionalSourceCannotEscapeThroughTheRemovalShortcut()
    {
        Fixture f = await Create(); IDecor provisional = f.Donor.DecorCreate(f.Decor.Entry);
        Donate(f, provisional.DecorId);
        Assert.Throws<InvalidOperationException>(() => f.Donor.DecorRemove(provisional));
        await Flush(f.Manager); using var verify = Context();
        Assert.Equal(2, await verify.ResidenceDecor.CountAsync());
        Assert.Equal(f.Donor.Id, (await verify.ResidenceDecor.SingleAsync(row => row.DecorId == f.Decor.DecorId)).Id);
        Assert.Single(f.Recipient.GetDecor());
    }

    [Fact]
    public async Task AFailedPairRetainsLaterIndependentFieldsAndRejectsOneSidedSaves()
    {
        Fixture f = await Create(); await RejectRecipient(); Donate(f, f.Decor.DecorId); await Flush(f.Manager);
        f.Donor.Name = "Late personal name"; f.Recipient.Music = 97;
        using (var escaped = Context()) { Assert.Throws<InvalidOperationException>(() => f.Donor.Save(escaped)); Assert.True(escaped.IsSaveRejected); }
        using (var escaped = Context()) { Assert.Throws<InvalidOperationException>(() => f.Recipient.GetPlot(0).Save(escaped)); Assert.True(escaped.IsSaveRejected); }
        using (var repair = Context()) await repair.Database.ExecuteSqlRawAsync("DROP TRIGGER reject_donation");
        await Flush(f.Manager); await Flush(f.Manager);
        using var verify = Context();
        Assert.Equal(f.Recipient.Id, (await verify.ResidenceDecor.SingleAsync()).Id);
        Assert.Equal("Late personal name", (await verify.Residence.SingleAsync(row => row.Id == f.Donor.Id)).Name);
        Assert.Equal((ushort)97, (await verify.Residence.SingleAsync(row => row.Id == f.Recipient.Id)).MusicId);
        Assert.Single(Messages(f).OfType<ServerHousingCommunityDonateUpdate>());
    }

    [Fact]
    public async Task PublicationRetryReconstructsTheReceiptAndDoesNotReplayCommittedSql()
    {
        Fixture f = await Create(); uint destination = 0; int attempts = 0;
        f.Session.SetMethodHandler(nameof(IWorldSession.EnqueueMessageEncrypted), args =>
        {
            if (args[0] is ServerHousingCommunityDonateUpdate receipt)
            {
                attempts++;
                if (attempts == 1) { destination = Assert.Single(receipt.Entries).Value1; receipt.Entries.Clear(); throw new IOException("owned publication failure"); }
                Assert.Equal(destination, Assert.Single(receipt.Entries).Value1);
                Assert.Equal(unchecked((uint)f.Decor.DecorId), Assert.Single(receipt.Entries).Value0);
            }
            return null;
        });
        Donate(f, f.Decor.DecorId); await Flush(f.Manager); Assert.Equal(1, attempts);
        using (var verify = Context())
        {
            Assert.Equal(f.Recipient.Id, (await verify.ResidenceDecor.SingleAsync()).Id);
            // If retry reopened SQL, this foreign header would invalidate the
            // complete pair. Publication-only retry must remain independent.
            await verify.Database.ExecuteSqlInterpolatedAsync($"UPDATE residence SET name={"External later name"} WHERE id={f.Recipient.Id}");
        }
        Assert.Throws<InvalidOperationException>(() => Assert.Single(f.Recipient.GetDecor()).ColourShiftId = 99);
        await Flush(f.Manager); Assert.Equal(2, attempts);
        using var final = Context(); Assert.Single(await final.ResidenceDecor.ToListAsync());
        Assert.Equal("External later name", (await final.Residence.SingleAsync(row => row.Id == f.Recipient.Id)).Name);
    }

    [Fact]
    public async Task ReentrantWorldUpdateCannotPublishTheUnacknowledgedStageAgain()
    {
        Fixture f = await Create(); int attempts = 0;
        f.Session.SetMethodHandler(nameof(IWorldSession.EnqueueMessageEncrypted), args =>
        {
            if (args[0] is ServerHousingCommunityDonateUpdate && ++attempts == 1) f.Manager.Update(0);
            return null;
        });
        Donate(f, f.Decor.DecorId); await Flush(f.Manager); await Flush(f.Manager);
        Assert.Equal(1, attempts); Assert.Single(Messages(f).OfType<ServerHousingCommunityDonateUpdate>());
        using var verify = Context(); Assert.Equal(f.Recipient.Id, (await verify.ResidenceDecor.SingleAsync()).Id);
    }

    [Fact]
    public async Task AStaleReadySnapshotCannotReleaseANewerDonationOnAnotherPair()
    {
        Fixture first = await Create(); Fixture second = await CreateSecondActor(first);
        IDecor later = second.Donor.DecorCreate(second.Decor.Entry);
        Donate(first, first.Decor.DecorId); Donate(second, second.Decor.DecorId);
        first.Manager.Update(61); await WaitForSql(first.Manager);
        first.Session.SetMethodHandler(nameof(IWorldSession.EnqueueMessageEncrypted), args =>
        {
            if (args[0] is ServerHousingCommunityDonateUpdate)
            {
                first.Manager.Update(0); // Publishes the disjoint second pair.
                Assert.Single(Messages(second).OfType<ServerHousingCommunityDonateUpdate>());
                Donate(second, later.DecorId); // Replaces its completed reservation.
            }
            return null;
        });
        first.Manager.Update(0); // Its old snapshot still contains the second command.
        Assert.Throws<InvalidOperationException>(() => later.ColourShiftId = 99);
        Assert.Throws<InvalidPacketValueException>(() => Donate(second, later.DecorId));
        await Flush(first.Manager);
        Assert.Single(Messages(first).OfType<ServerHousingCommunityDonateUpdate>());
        Assert.Equal(2, Messages(second).OfType<ServerHousingCommunityDonateUpdate>().Count());
        using var verify = Context(); Assert.Equal(3, await verify.ResidenceDecor.CountAsync());
        Assert.Equal(2, await verify.ResidenceDecor.CountAsync(row => row.Id == second.Recipient.Id));
    }

    [Fact]
    public async Task MapPublicationWaitsForCommitAndRetainsStageOrderWithFreshMetadata()
    {
        Fixture f = await Create(); var order = new List<string>(); int deletes = 0;
        IResidenceMapInstance recipientMap = RecordingDispatchProxy<IResidenceMapInstance>.Create(out var target);
        IResidenceMapInstance donorMap = RecordingDispatchProxy<IResidenceMapInstance>.Create(out var source);
        f.Recipient.Map = recipientMap; f.Donor.Map = donorMap;
        target.SetMethodHandler(nameof(IResidenceMapInstance.EnqueueToAll), args =>
        {
            using var verify = Context(); Assert.Equal(f.Recipient.Id, verify.ResidenceDecor.Single().Id);
            var row = Assert.Single(Assert.IsType<ServerHousingResidenceDecor>(args[0]).DecorData);
            Assert.Equal(0x80000005u, row.DecorData); Assert.Equal(444ul, row.ParentDecorId); order.Add("copy"); return null;
        });
        source.SetMethodHandler(nameof(IResidenceMapInstance.EnqueueToAll), args =>
        {
            var packet = Assert.IsType<ServerHousingResidenceDecor>(args[0]); var row = Assert.Single(packet.DecorData);
            Assert.Equal(0u, row.DecorInfoId); Assert.Equal(f.Decor.DecorId, row.DecorId); deletes++;
            if (deletes == 1) { packet.DecorData.Clear(); throw new IOException("owned map publication failure"); }
            order.Add("delete"); return null;
        });
        f.Session.SetMethodHandler(nameof(IWorldSession.EnqueueMessageEncrypted), args => { if (args[0] is ServerHousingCommunityDonateUpdate) order.Add("receipt"); return null; });
        Donate(f, f.Decor.DecorId); Assert.Empty(order); await Flush(f.Manager);
        Assert.Equal(new[] { "copy" }, order); Assert.Equal(1, deletes);
        await Flush(f.Manager); Assert.Equal(new[] { "copy", "delete", "receipt" }, order); Assert.Equal(2, deletes);
        Assert.Single(target.GetInvocations(nameof(IResidenceMapInstance.EnqueueToAll)));
    }

    [Fact]
    public async Task ReplacedOriginalSessionReceivesNoPrivateDonationReceipt()
    {
        Fixture f = await Create(); Donate(f, f.Decor.DecorId);
        ((RecordingDispatchProxy<IPlayer>)(object)f.Player).SetProperty(nameof(IPlayer.Session), RecordingDispatchProxy<IWorldSession>.Create(out var replacement));
        await Flush(f.Manager);
        using var verify = Context(); Assert.Equal(f.Recipient.Id, (await verify.ResidenceDecor.SingleAsync()).Id);
        Assert.Empty(Messages(f).OfType<ServerHousingCommunityDonateUpdate>());
        Assert.Empty(replacement.GetInvocations(nameof(IWorldSession.EnqueueMessageEncrypted)));
    }

    [Fact]
    public async Task ReplacedCommunityResidenceCannotReceiveTheOriginalPrivateReceipt()
    {
        Fixture f = await Create(); await RejectRecipient(); Donate(f, f.Decor.DecorId); await Flush(f.Manager);
        f.Community.Residence = null;
        using (var repair = Context()) await repair.Database.ExecuteSqlRawAsync("DROP TRIGGER reject_donation");
        await Flush(f.Manager); using var verify = Context(); Assert.Equal(f.Recipient.Id, (await verify.ResidenceDecor.SingleAsync()).Id);
        Assert.Empty(Messages(f).OfType<ServerHousingCommunityDonateUpdate>());
    }

    [Fact]
    public async Task ForeignPartialSqlCannotBeAdoptedAsACommittedDonationPair()
    {
        Fixture f = await Create(); await RejectRecipient(); Donate(f, f.Decor.DecorId); await Flush(f.Manager);
        using (var foreign = Context())
        {
            await foreign.Database.ExecuteSqlRawAsync("DROP TRIGGER reject_donation");
            await foreign.Database.ExecuteSqlInterpolatedAsync($"UPDATE residence_decor SET colourShiftId={99} WHERE id={f.Donor.Id}");
        }
        await Flush(f.Manager); using var verify = Context();
        Assert.Equal(f.Donor.Id, (await verify.ResidenceDecor.SingleAsync()).Id); Assert.Empty(f.Recipient.GetDecor());
        Assert.Empty(Messages(f).OfType<ServerHousingCommunityDonateUpdate>());
        using var escape = Context(); Assert.Throws<InvalidOperationException>(() => f.Donor.Save(escape));
    }

    [Fact]
    public async Task LostCommitAcknowledgementReconcilesTheSamePairWithoutAnotherCopy()
    {
        Fixture f = await Create(); Donate(f, f.Decor.DecorId);
        object command = typeof(Residence).GetField("donation", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(f.Donor);
        var run = command.GetType().GetMethod("Run", BindingFlags.Instance | BindingFlags.NonPublic)!;
        Func<CharacterContext> contexts = () => new FaultContext(Config, new LostAcknowledgement());
        await Record.ExceptionAsync(() => (Task)run.Invoke(command, [contexts]));
        using (var committed = Context()) Assert.Equal(f.Recipient.Id, (await committed.ResidenceDecor.SingleAsync()).Id);
        await Flush(f.Manager); using var verify = Context(); Assert.Single(await verify.ResidenceDecor.ToListAsync());
        Assert.Null(f.Donor.GetDecor(f.Decor.DecorId)); Assert.Single(f.Recipient.GetDecor());
        Assert.Single(Messages(f).OfType<ServerHousingCommunityDonateUpdate>());
    }

    [Fact]
    public async Task AcceptedDonationSurvivesRecipientRetirementBeforeItsRetry()
    {
        Fixture f = await Create(); await RejectRecipient(); Donate(f, f.Decor.DecorId); await Flush(f.Manager);
        f.Community.DisbandGuild(); Assert.Null(f.Manager.GetCommunityByOwner(f.Community.Id));
        using (var early = Context()) Assert.Throws<InvalidOperationException>(() => f.Community.Save(early));
        using (var repair = Context()) await repair.Database.ExecuteSqlRawAsync("DROP TRIGGER reject_donation");
        await Flush(f.Manager);
        using (var retirement = Context()) { f.Community.Save(retirement); await retirement.SaveChangesAsync(); }
        using var verify = Context(); Assert.Equal(f.Recipient.Id, (await verify.ResidenceDecor.SingleAsync()).Id);
        Assert.Null((await verify.Residence.SingleAsync(row => row.Id == f.Recipient.Id)).GuildOwnerId);
        Assert.NotNull((await verify.Guild.SingleAsync()).DeleteTime); Assert.Empty(Messages(f).OfType<ServerHousingCommunityDonateUpdate>());
    }

    [Fact]
    public async Task DonorCacheRetirementCannotReleaseTheAcceptedPairToAnOrdinarySave()
    {
        Fixture f = await Create(); await RejectRecipient(); Donate(f, f.Decor.DecorId); await Flush(f.Manager);
        f.Manager.RemoveResidence("Owner");
        using (var escape = Context()) Assert.Throws<InvalidOperationException>(() => f.Donor.Save(escape));
        using (var repair = Context()) await repair.Database.ExecuteSqlRawAsync("DROP TRIGGER reject_donation");
        await Flush(f.Manager); using var verify = Context(); Assert.Equal(f.Recipient.Id, (await verify.ResidenceDecor.SingleAsync()).Id);
        Assert.Empty(Messages(f).OfType<ServerHousingCommunityDonateUpdate>());
    }

    private async Task RejectRecipient()
    {
        using var setup = Context();
        await setup.Database.ExecuteSqlRawAsync("CREATE TRIGGER reject_donation BEFORE INSERT ON residence_decor WHEN NEW.id IN (SELECT id FROM residence WHERE guildOwnerId IS NOT NULL AND ownerId IS NULL) BEGIN SELECT RAISE(ABORT, 'owned recipient failure'); END");
    }

    [Fact]
    public async Task BatchDonationKeepsItsSourceOrderAndExistingUint32ReceiptNarrowing()
    {
        Fixture f = await Create();
        typeof(GlobalResidenceManager).GetField("nextDecorId", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(f.Manager, (ulong)uint.MaxValue + 5);
        IDecor second = f.Donor.DecorCreate(f.Decor.Entry);
        Donate(f, second.DecorId, f.Decor.DecorId); await Flush(f.Manager);
        using var verify = Context(); Assert.Equal(2, await verify.ResidenceDecor.CountAsync(row => row.Id == f.Recipient.Id));
        Assert.False(await verify.ResidenceDecor.AnyAsync(row => row.Id == f.Donor.Id));
        var entries = Assert.Single(Messages(f).OfType<ServerHousingCommunityDonateUpdate>()).Entries;
        Assert.Equal(new[] { unchecked((uint)second.DecorId), unchecked((uint)f.Decor.DecorId) }, entries.Select(row => row.Value0));
        Assert.All(entries, row => Assert.True(row.Value1 < 10)); Assert.Empty(f.Donor.GetDecor()); Assert.Equal(2, f.Recipient.GetDecor().Count());
    }

    [Fact]
    public async Task DonationWaitsForTheOriginalCommunityCreationUnit()
    {
        Fixture f = await Create(saveCommunity: false); Donate(f, f.Decor.DecorId); await Flush(f.Manager);
        using (var before = Context())
        {
            Assert.False(await before.Residence.AnyAsync(row => row.Id == f.Recipient.Id));
            Assert.Equal(f.Donor.Id, (await before.ResidenceDecor.SingleAsync()).Id);
        }
        Assert.Empty(Messages(f).OfType<ServerHousingCommunityDonateUpdate>());
        using (var creation = Context()) { f.Community.Save(creation); await creation.SaveChangesAsync(); }
        await Flush(f.Manager); using var verify = Context();
        Assert.Equal(f.Recipient.Id, (await verify.ResidenceDecor.SingleAsync()).Id);
        Assert.Single(Messages(f).OfType<ServerHousingCommunityDonateUpdate>());
    }

    [Fact]
    public async Task DisbandBeforePreparationCancelsOnlyTheUnattemptedDonation()
    {
        Fixture f = await Create(saveCommunity: false); Donate(f, f.Decor.DecorId); f.Community.DisbandGuild();
        await Flush(f.Manager);
        using (var retirement = Context()) { f.Community.Save(retirement); await retirement.SaveChangesAsync(); }
        using var verify = Context(); Assert.Equal(f.Donor.Id, (await verify.ResidenceDecor.SingleAsync()).Id);
        Assert.Same(f.Decor, f.Donor.GetDecor(f.Decor.DecorId)); Assert.Empty(Messages(f).OfType<ServerHousingCommunityDonateUpdate>());
        f.Decor.ColourShiftId = 9; await Flush(f.Manager);
        using var final = Context(); Assert.Equal((ushort)9, (await final.ResidenceDecor.SingleAsync()).ColourShiftId);
    }

    [Fact]
    public async Task IdenticalRowsInAnotherDatabaseCannotRetargetTheAcceptedDonation()
    {
        Fixture f = await Create(); Donate(f, f.Decor.DecorId);
        string foreignFile = Path.Combine(Path.GetTempPath(), $"nf-donation-foreign-{Guid.NewGuid():N}.sqlite");
        var foreignConfig = new DatabaseConnectionString { Provider = DatabaseProvider.Sqlite, ConnectionString = $"Data Source={foreignFile};Foreign Keys=True" };
        try
        {
            using (var original = Context())
            using (var foreign = new CharacterContext(foreignConfig))
            {
                await foreign.Database.EnsureCreatedAsync();
                foreign.Character.AddRange(await original.Character.AsNoTracking().ToArrayAsync());
                foreign.Guild.AddRange(await original.Guild.AsNoTracking().ToArrayAsync());
                foreign.Residence.AddRange(await original.Residence.AsNoTracking().Include(row => row.Plot).Include(row => row.Decor).Include(row => row.Neighbors).ToArrayAsync());
                await foreign.SaveChangesAsync();
            }
            object command = typeof(Residence).GetField("donation", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(f.Donor);
            var run = command.GetType().GetMethod("Run", BindingFlags.Instance | BindingFlags.NonPublic)!;
            Func<CharacterContext> contexts = () => new(foreignConfig);
            await Assert.ThrowsAsync<InvalidOperationException>(() => (Task)run.Invoke(command, [contexts]));
            using (var foreign = new CharacterContext(foreignConfig)) Assert.Equal(f.Donor.Id, (await foreign.ResidenceDecor.SingleAsync()).Id);
            await Flush(f.Manager); using var verify = Context(); Assert.Equal(f.Recipient.Id, (await verify.ResidenceDecor.SingleAsync()).Id);
            Assert.Single(Messages(f).OfType<ServerHousingCommunityDonateUpdate>());
        }
        finally
        {
            using var pool = new Microsoft.Data.Sqlite.SqliteConnection(foreignConfig.ConnectionString); Microsoft.Data.Sqlite.SqliteConnection.ClearPool(pool);
            if (File.Exists(foreignFile)) File.Delete(foreignFile);
        }
    }
    private sealed class FaultContext(DatabaseConnectionString config, IInterceptor interceptor) : CharacterContext(config)
    {
        protected override void OnConfiguring(DbContextOptionsBuilder builder) { base.OnConfiguring(builder); builder.AddInterceptors(interceptor); }
    }
    private sealed class LostAcknowledgement : DbTransactionInterceptor
    {
        public override Task TransactionCommittedAsync(DbTransaction transaction, TransactionEndEventData data, CancellationToken cancellationToken = default)
            => Task.FromException(new IOException("owned donation acknowledgement loss"));
    }

    private static void Donate(Fixture f, params ulong[] ids)
    {
        var request = new ClientHousingCommunityDonate();
        foreach (ulong id in ids)
        {
            var entry = new DecorInfo(); typeof(DecorInfo).GetProperty(nameof(DecorInfo.DecorId))!.GetSetMethod(true)!.Invoke(entry, [id]); request.Decor.Add(entry);
        }
        new ClientHousingCommunityDonateHandler().HandleMessage((IWorldSession)(object)f.Session, request);
    }
    private static object[] Messages(Fixture f) => f.Session.GetInvocations(nameof(IWorldSession.EnqueueMessageEncrypted)).Select(call => call.Arguments[0]).ToArray();
    private static async Task Flush(GlobalResidenceManager manager)
    {
        manager.Update(61);
        await WaitForSql(manager);
        manager.Update(0); // The next real world tick applies/publishes confirmed SQL.
        await WaitForSql(manager);
    }
    private static async Task WaitForSql(GlobalResidenceManager manager)
    {
        object gate = typeof(GlobalResidenceManager).GetField("saveGate", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(manager);
        var pending = (HashSet<ulong>)typeof(GlobalResidenceManager).GetField("savesInFlight", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(manager);
        for (int i = 0; i < 500; i++) { lock (gate) if (pending.Count == 0) return; await Task.Delay(10); }
        Assert.Fail("The donation/residence save did not settle.");
    }
    private async Task<Fixture> Create(bool saveCommunity = true)
    {
        using (var seed = Context())
        {
            await seed.Database.EnsureCreatedAsync(); seed.Character.Add(new CharacterModel { Id = 42, Name = "Owner" });
            seed.Guild.Add(new GuildModel { Id = 77, Type = (byte)GuildType.Community, Name = "Community", LeaderId = 42,
                GuildRank = [new GuildRankModel { Id = 77, Index = 0, Name = "Leader", Permission = (uint)GuildRankPermission.Leader }],
                GuildMember = [new GuildMemberModel { Id = 77, CharacterId = 42, Rank = 0 }] });
            await seed.SaveChangesAsync();
        }
        var database = new CharacterDatabase(); database.Initialise(Config);
        var databases = RecordingDispatchProxy<IDatabaseManager>.Create(out var db); db.SetMethodReturn(nameof(IDatabaseManager.GetDatabase), database);
        var tables = RecordingDispatchProxy<IGameTableManager>.Create(out var table);
        table.SetProperty(nameof(IGameTableManager.HousingPlotInfo), Table(
            new HousingPlotInfoEntry { Id = 1, HousingPropertyInfoId = (uint)PropertyInfoId.Residence, HousingPropertyPlotIndex = 0 },
            new HousingPlotInfoEntry { Id = 2, HousingPropertyInfoId = (uint)PropertyInfoId.Residence, HousingPropertyPlotIndex = 1 },
            new HousingPlotInfoEntry { Id = 3, HousingPropertyInfoId = (uint)PropertyInfoId.Community, HousingPropertyPlotIndex = 0 },
            new HousingPlotInfoEntry { Id = 4, HousingPropertyInfoId = (uint)PropertyInfoId.Community, HousingPropertyPlotIndex = 1 }));
        table.SetProperty(nameof(IGameTableManager.HousingPlugItem), Table(new HousingPlugItemEntry { Id = 531 }, new HousingPlugItemEntry { Id = 573 }));
        table.SetProperty(nameof(IGameTableManager.HousingWallpaperInfo), Table(new HousingWallpaperInfoEntry { Id = 97, Flags = 0x100 }));
        var definition = new HousingDecorInfoEntry { Id = 5 }; table.SetProperty(nameof(IGameTableManager.HousingDecorInfo), Table(definition));
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out var owner);
        owner.SetProperty(nameof(IPlayer.Level), 50u); owner.SetProperty(nameof(IPlayer.CharacterId), 42ul); owner.SetProperty(nameof(IPlayer.Name), "Owner");
        owner.SetProperty(nameof(IPlayer.Identity), new NexusForever.Game.Abstract.Identity { Id = 42, RealmId = 1 });
        IWorldSession session = RecordingDispatchProxy<IWorldSession>.Create(out var connection); connection.SetProperty(nameof(IWorldSession.Player), player);
        connection.SetProperty(nameof(IWorldSession.CanProcessIncomingPackets), true); owner.SetProperty(nameof(IPlayer.Session), session);
        owner.SetProperty(nameof(IPlayer.Map), RecordingDispatchProxy<IResidenceMapInstance>.Create(out _));
        var manager = new GlobalResidenceManager(databaseManager: databases, gameTableManager: tables); manager.Initialise();
        Residence donor = Assert.IsType<Residence>(manager.CreateResidence(player));
        var residences = RecordingDispatchProxy<IResidenceManager>.Create(out var housing); housing.SetProperty(nameof(IResidenceManager.Residence), donor); owner.SetProperty(nameof(IPlayer.ResidenceManager), residences);
        var realm = RecordingDispatchProxy<IRealmContext>.Create(out var context); context.SetProperty(nameof(IRealmContext.RealmId), (ushort)1);
        var publisher = RecordingDispatchProxy<IInternalMessagePublisher>.Create(out var broker); broker.SetMethodReturn(nameof(IInternalMessagePublisher.PublishAsync), Task.CompletedTask);
        var filter = RecordingDispatchProxy<ITextFilterManager>.Create(out var text); text.SetMethodReturn(nameof(ITextFilterManager.IsTextValid), true);
        var community = new Community(realm, publisher, manager, filter, RecordingDispatchProxy<IMapLockManager>.Create(out _),
            globalGuildManager: RecordingDispatchProxy<IGlobalGuildManager>.Create(out _));
        community.Initialise(Assert.Single(database.GetGuilds())); community.Residence = manager.CreateCommunity(community);
        var guilds = RecordingDispatchProxy<IGuildManager>.Create(out var membership); membership.SetMethodReturn(nameof(IGuildManager.GetGuild), community); owner.SetProperty(nameof(IPlayer.GuildManager), guilds);
        IDecor decor = donor.DecorCreate(definition); decor.DecorData = 0x80000005; decor.HookBagIndex = 3; decor.HookIndex = 4; decor.PlotIndex = 0x80000003;
        decor.Scale = 1.25f; decor.Position = new(1.25f, 2.5f, -3.75f); decor.Rotation = new(0.1f, 0.2f, 0.3f, 0.4f);
        decor.ActivePropUnitId = 8; decor.DecorParentId = 444; decor.ColourShiftId = 7;
        using (var initial = Context()) { donor.Save(initial); if (saveCommunity) community.Save(initial); await initial.SaveChangesAsync(); }
        return new(manager, donor, Assert.IsType<Residence>(community.Residence), community, player, connection, decor, tables);
    }

    private async Task<Fixture> CreateSecondActor(Fixture first)
    {
        using (var seed = Context())
        {
            seed.Character.Add(new CharacterModel { Id = 43, Name = "Second" });
            seed.Guild.Add(new GuildModel { Id = 78, Type = (byte)GuildType.Community, Name = "Other", LeaderId = 43,
                GuildRank = [new GuildRankModel { Id = 78, Index = 0, Name = "Leader", Permission = (uint)GuildRankPermission.Leader }],
                GuildMember = [new GuildMemberModel { Id = 78, CharacterId = 43, Rank = 0 }] });
            await seed.SaveChangesAsync();
        }
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out var owner);
        owner.SetProperty(nameof(IPlayer.Level), 50u); owner.SetProperty(nameof(IPlayer.CharacterId), 43ul); owner.SetProperty(nameof(IPlayer.Name), "Second");
        owner.SetProperty(nameof(IPlayer.Identity), new NexusForever.Game.Abstract.Identity { Id = 43, RealmId = 1 });
        IWorldSession session = RecordingDispatchProxy<IWorldSession>.Create(out var connection); connection.SetProperty(nameof(IWorldSession.Player), player);
        owner.SetProperty(nameof(IPlayer.Session), session); owner.SetProperty(nameof(IPlayer.Map), RecordingDispatchProxy<IResidenceMapInstance>.Create(out _));
        Residence donor = Assert.IsType<Residence>(first.Manager.CreateResidence(player));
        var residences = RecordingDispatchProxy<IResidenceManager>.Create(out var housing); housing.SetProperty(nameof(IResidenceManager.Residence), donor); owner.SetProperty(nameof(IPlayer.ResidenceManager), residences);
        var realm = RecordingDispatchProxy<IRealmContext>.Create(out var context); context.SetProperty(nameof(IRealmContext.RealmId), (ushort)1);
        var publisher = RecordingDispatchProxy<IInternalMessagePublisher>.Create(out var broker); broker.SetMethodReturn(nameof(IInternalMessagePublisher.PublishAsync), Task.CompletedTask);
        var filter = RecordingDispatchProxy<ITextFilterManager>.Create(out var text); text.SetMethodReturn(nameof(ITextFilterManager.IsTextValid), true);
        var community = new Community(realm, publisher, first.Manager, filter, RecordingDispatchProxy<IMapLockManager>.Create(out _),
            globalGuildManager: RecordingDispatchProxy<IGlobalGuildManager>.Create(out _));
        var database = (CharacterDatabase)typeof(GlobalResidenceManager).GetMethod("GetCharacterDatabase", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(first.Manager, null);
        community.Initialise(database.GetGuilds().Single(row => row.Id == 78)); community.Residence = first.Manager.CreateCommunity(community);
        var guilds = RecordingDispatchProxy<IGuildManager>.Create(out var membership); membership.SetMethodReturn(nameof(IGuildManager.GetGuild), community); owner.SetProperty(nameof(IPlayer.GuildManager), guilds);
        IDecor decor = donor.DecorCreate(first.Decor.Entry);
        using (var initial = Context()) { donor.Save(initial); community.Save(initial); await initial.SaveChangesAsync(); }
        return new(first.Manager, donor, Assert.IsType<Residence>(community.Residence), community, player, connection, decor, first.Tables);
    }
    private static GameTable<T> Table<T>(params T[] entries) where T : class, new()
    {
        var table = (GameTable<T>)RuntimeHelpers.GetUninitializedObject(typeof(GameTable<T>));
        uint Id(T entry) => (uint)typeof(T).GetField("Id")!.GetValue(entry);
        uint max = entries.Max(Id) + 1; int[] lookup = Enumerable.Repeat(-1, (int)max).ToArray(); for (int i = 0; i < entries.Length; i++) lookup[Id(entries[i])] = i;
        typeof(GameTable<T>).GetField("<Entries>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(table, entries);
        typeof(GameTable<T>).GetField("header", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(table, new GameTableHeader { MaxId = max });
        typeof(GameTable<T>).GetField("lookup", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(table, lookup); return table;
    }
    private sealed record Fixture(GlobalResidenceManager Manager, Residence Donor, Residence Recipient, Community Community, IPlayer Player,
        RecordingDispatchProxy<IWorldSession> Session, IDecor Decor, IGameTableManager Tables);
    public void Dispose()
    {
        using var pool = new Microsoft.Data.Sqlite.SqliteConnection(Config.ConnectionString); Microsoft.Data.Sqlite.SqliteConnection.ClearPool(pool);
        if (File.Exists(file)) File.Delete(file);
    }
}
