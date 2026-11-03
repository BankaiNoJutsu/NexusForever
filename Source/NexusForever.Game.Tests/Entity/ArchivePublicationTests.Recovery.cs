using System.Data.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using NexusForever.Database.Character.Model;
using NexusForever.Game.Entity;
using NexusForever.Network.World.Message.Model.GalacticArchive;

namespace NexusForever.Game.Tests.Entity;

public sealed partial class ArchivePublicationTests
{
    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task InitialSyncDoesNotDuplicateAnAlreadyRetainedUnlock(bool committedBeforeInitial)
    {
        using var f = await Create(); f.Archive.UnlockArticle(900, grantRewards: false);
        if (committedBeforeInitial) await f.Save();
        f.Archive.SendInitialPackets();
        if (!committedBeforeInitial) await f.Save();
        f.Pump();
        // One accepted unlock notice, plus the confirmed initial list only
        // when the unlock had committed before that initial request.
        Assert.Equal(committedBeforeInitial ? 2 : 1, f.Packets<ServerGalacticArchiveUpdate>().Count());
        Assert.Single(f.Packets<ServerGalacticArchiveRefresh>());
    }

    [Fact]
    public async Task LostAcknowledgementRetainsArchivePublicationUntilTheOriginalRetry()
    {
        using var f = await Create(); f.Archive.UnlockArticle(900, grantRewards: false);
        await Assert.ThrowsAsync<IOException>(() => f.Save(new LostAcknowledgement())); f.Pump(); Assert.Empty(f.Packets<ServerGalacticArchiveUpdate>());
        using (var read = f.Context()) Assert.Single(read.CharacterGalacticArchive);
        await f.Save(); f.Pump(); Assert.Single(f.Packets<ServerGalacticArchiveUpdate>());
        await f.Save(); f.Pump(); Assert.Single(f.Packets<ServerGalacticArchiveUpdate>());
    }

    [Fact]
    public async Task SavepointRollbackAndOuterCommitDoNotAcknowledgeArchivePublication()
    {
        using var f = await Create(); f.Archive.UnlockArticle(900, grantRewards: false);
        await using (var context = f.Context())
        await using (var transaction = await context.Database.BeginTransactionAsync())
        {
            await transaction.CreateSavepointAsync("before_archive"); f.Actor.Player.Save(context); await context.SaveChangesAsync();
            await transaction.RollbackToSavepointAsync("before_archive"); await transaction.CommitAsync();
        }
        f.Pump(); Assert.Empty(f.Packets<ServerGalacticArchiveUpdate>());
        using (var read = f.Context()) Assert.Empty(read.CharacterGalacticArchive);
        await f.Save(); f.Pump(); Assert.Single(f.Packets<ServerGalacticArchiveUpdate>());
    }

    [Fact]
    public async Task FrozenNormalSaveLeavesLaterViewedBitsAndNoticesPending()
    {
        using var f = await Create();
        var currencies = new CurrencyManager(f.Source.Source.Tables); currencies.Initialise(f.Actor.Player, new CharacterModel { Id = 42 }); f.Actor.Set(nameof(Player.CurrencyManager), currencies);
        f.Archive.UnlockArticle(900, grantRewards: false); f.Actor.OnAuth = () => Assert.True(f.Archive.MarkArticleViewed(900));
        await f.Actor.Player.SaveDirect(); await f.Actor.Drain(); f.Pump();
        Assert.Equal(0u, Assert.Single(f.Packets<ServerGalacticArchiveUpdate>()).ViewedFlags);
        using (var read = f.Context()) Assert.Equal(0u, Assert.Single(read.CharacterGalacticArchive).ViewedFlags);
        f.Actor.OnAuth = null; await f.Actor.Player.SaveDirect(); await f.Actor.Drain(); f.Pump();
        Assert.Equal(1u, f.Packets<ServerGalacticArchiveUpdate>().Last().ViewedFlags); Assert.Equal(2, f.Packets<ServerGalacticArchiveUpdate>().Count());
    }

    [Fact]
    public async Task FailedSecondArticleRetriesWithoutRepeatingTheFirst()
    {
        using var f = await Create(); f.Archive.UnlockArticle(900, grantRewards: false); f.Archive.UnlockLinkedArticle(910);
        int attempts = 0; var delivered = new List<uint>();
        f.Messages.SetMethodHandler("EnqueueMessageEncrypted", args =>
        {
            if (args[0] is ServerGalacticArchiveUpdate packet)
            {
                if (packet.ArchiveArticleId == 910 && attempts++ == 0) throw new IOException("Owned archive send failure.");
                delivered.Add(packet.ArchiveArticleId);
            }
            return null;
        });
        await f.Save(); f.Pump(); Assert.Equal(2, attempts); Assert.Equal(new uint[] { 900, 910 }, delivered);
    }

    [Fact]
    public async Task AReentrantInitialSendCannotApplyAnOlderRowAfterCommittedUpdates()
    {
        using var f = await Create(existing: true); bool entered = false;
        f.Messages.SetMethodHandler("EnqueueMessageEncrypted", args =>
        {
            if (args[0] is ServerGalacticArchiveRefresh && !entered)
            {
                entered = true; f.Archive.MarkArticleViewed(900);
                using var context = f.Context(); f.Actor.Player.Save(context); context.SaveChanges(); f.Actor.Events.Update(.01);
            }
            return null;
        });
        f.Archive.SendInitialPackets(); Assert.True(entered); f.Pump();
        Assert.Equal(new uint[] { 0, 1 }, f.Packets<ServerGalacticArchiveUpdate>().Select(row => row.ViewedFlags)); Assert.Empty(f.Actor.Errors);
    }

    [Fact]
    public async Task QuietRuleUpdatesStayQuietWhileTheirCommittedInitialStateAdvances()
    {
        using var f = await Create(completedAchievement: true); f.Archive.RefreshRuleUnlocks();
        Assert.Empty(f.Packets<ServerGalacticArchiveUpdate>()); await f.Save(); f.Pump(); Assert.Empty(f.Packets<ServerGalacticArchiveUpdate>());
        f.Archive.SendInitialPackets(); Assert.Equal(Flags, Assert.Single(f.Packets<ServerGalacticArchiveUpdate>()).UnlockedFlags);
    }

    [Fact]
    public async Task UnavailableArchiveRowsStayRawAndNeverEnterPublishedLists()
    {
        using var f = await Create();
        using (var seed = f.Context()) { seed.CharacterGalacticArchive.Add(new() { Id = 42, ArchiveArticleId = 999, UnlockedFlags = 0x12345678, ViewedFlags = 0x23456789 }); await seed.SaveChangesAsync(); }
        using var read = f.Context(); var model = read.Character.AsNoTracking().Include(row => row.GalacticArchive).Single();
        var archive = new GalacticArchiveManager(f.Actor.Player, model, f.Tables); f.Actor.Set(nameof(Player.GalacticArchiveManager), archive);
        archive.SendInitialPackets(); Assert.Empty(f.Packets<ServerGalacticArchiveUpdate>());
        archive.UnlockArticle(900, grantRewards: false); await f.Save(); f.Pump();
        Assert.Equal(900u, Assert.Single(f.Packets<ServerGalacticArchiveUpdate>()).ArchiveArticleId);
        using var final = f.Context(); var raw = final.CharacterGalacticArchive.Single(row => row.ArchiveArticleId == 999);
        Assert.Equal(0x12345678u, raw.UnlockedFlags); Assert.Equal(0x23456789u, raw.ViewedFlags);
    }

    [Fact]
    public async Task ReplacedManagerCannotReceiveTheOriginalManagersQueuedPackets()
    {
        using var f = await Create(); f.Archive.UnlockArticle(900, grantRewards: false); await f.Save();
        using var read = f.Context(); var model = read.Character.AsNoTracking().Include(row => row.GalacticArchive).Single();
        var replacement = new GalacticArchiveManager(f.Actor.Player, model, f.Tables); f.Actor.Set(nameof(Player.GalacticArchiveManager), replacement);
        f.Pump(); Assert.Empty(f.Packets<ServerGalacticArchiveUpdate>());
        replacement.SendInitialPackets(); Assert.Equal(900u, Assert.Single(f.Packets<ServerGalacticArchiveUpdate>()).ArchiveArticleId);
    }

    [Theory]
    [InlineData("identity")] [InlineData("session")]
    public async Task StaleOwnershipDropsArchivePacketsWithoutWorldErrors(string change)
    {
        using var f = await Create(); f.Archive.UnlockArticle(900, grantRewards: false); await f.Save(); var identity = f.Actor.Player.Identity;
        try
        {
            if (change == "identity") f.Actor.Set(nameof(Player.Identity), null); else f.Messages.SetProperty("Player", null);
            f.Pump(); Assert.Empty(f.Packets<ServerGalacticArchiveUpdate>()); Assert.Empty(f.Actor.Errors);
        }
        finally { f.Actor.Set(nameof(Player.Identity), identity); f.Messages.SetProperty("Player", f.Actor.Player); }
    }

    [Fact]
    public async Task AcceptedUnlockAndViewedPayloadsKeepTheirOrderAcrossOneSave()
    {
        using var f = await Create(); f.Archive.UnlockArticle(900, grantRewards: false); f.Archive.MarkArticleViewed(900);
        await f.Save(); f.Pump(); Assert.Equal(new uint[] { 0, 1 }, f.Packets<ServerGalacticArchiveUpdate>().Select(row => row.ViewedFlags));
        f.Archive.MarkArticleViewed(900); f.Archive.UnlockArticle(900, grantRewards: false); await f.Save(); f.Pump();
        Assert.Equal(2, f.Packets<ServerGalacticArchiveUpdate>().Count());
    }

    private sealed class LostAcknowledgement : DbTransactionInterceptor
    {
        public override Task TransactionCommittedAsync(DbTransaction transaction, TransactionEndEventData data, CancellationToken token = default)
            => Task.FromException(new IOException("Owned archive acknowledgement loss."));
    }
}
