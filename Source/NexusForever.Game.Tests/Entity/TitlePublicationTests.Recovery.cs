using System.Data.Common;
using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using NexusForever.Database.Character;
using NexusForever.Game.Abstract;
using NexusForever.Game.Abstract.Achievement;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Entity;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.Network.World.Message.Model;
using NexusForever.WorldServer.Network;

namespace NexusForever.Game.Tests.Entity;

public sealed partial class TitlePublicationTests
{
    [Fact]
    public async Task LostAcknowledgementPublishesOnlyAfterTheOriginalActorReconciles()
    {
        using var f = await Create(); f.Titles.AddTitle(21); f.Titles.ActiveTitleId = 21;
        await Assert.ThrowsAsync<IOException>(() => f.Save(new LostAcknowledgement())); f.Pump();
        Assert.Empty(f.Packets<ServerTitleUpdate>()); Assert.Empty(f.Packets<ServerTitleSet>()); Assert.Equal(0, f.EntityTitle());
        using (var read = f.Context()) Assert.Single(read.CharacterTitle);
        await f.Save(); f.Pump(); Assert.Single(f.Packets<ServerTitleUpdate>()); Assert.Single(f.Packets<ServerTitleSet>());
        Assert.Equal(21, f.EntityTitle()); await f.Save(); f.Pump(); Assert.Single(f.Packets<ServerTitleUpdate>());
    }

    [Fact]
    public async Task SavepointRollbackAndOuterCommitCannotPublishOrAcknowledgeTitles()
    {
        using var f = await Create(); f.Titles.AddTitle(21); f.Titles.ActiveTitleId = 21;
        await using (var context = f.Context())
        await using (var transaction = await context.Database.BeginTransactionAsync())
        {
            await transaction.CreateSavepointAsync("before_title");
            f.Actor.Player.Save(context); await context.SaveChangesAsync();
            await transaction.RollbackToSavepointAsync("before_title"); await transaction.CommitAsync();
        }
        f.Pump(); Assert.Empty(f.Packets<ServerTitleUpdate>()); Assert.Equal(0, f.EntityTitle());
        using (var read = f.Context()) { Assert.Empty(read.CharacterTitle); Assert.Equal(0, read.Character.Single().Title); }
        await f.Save(); f.Pump(); Assert.Single(f.Packets<ServerTitleUpdate>()); Assert.Equal(21, f.EntityTitle());
    }

    [Fact]
    public async Task SuppressedFlushCannotReleasePendingTitlePackets()
    {
        using var f = await Create(); f.Titles.AddTitle(21);
        using (var context = f.Context(new SuppressedFlush()))
        {
            f.Actor.Player.Save(context); await context.SaveChangesAsync();
        }
        f.Pump(); Assert.Empty(f.Packets<ServerTitleUpdate>());
        using (var read = f.Context()) Assert.Empty(read.CharacterTitle);
        await f.Save(); f.Pump(); Assert.Single(f.Packets<ServerTitleUpdate>());
    }

    [Fact]
    public async Task AnAlreadyOwnedNoticeCanConfirmAnUnchangedTitleGraph()
    {
        using var f = await Create(existing: true); f.Titles.AddTitle(21);
        Assert.Empty(f.Packets<ServerTitleUpdate>());
        using (var context = f.Context())
        {
            f.Titles.Save(context); Assert.Empty(context.ChangeTracker.Entries()); Assert.Equal(0, await context.SaveChangesAsync());
        }
        f.Pump(); Assert.True(Assert.Single(f.Packets<ServerTitleUpdate>()).Alreadyowned);
    }

    [Fact]
    public async Task RealNormalSavePublishesCapturedRequestsAndLeavesLaterIntentPending()
    {
        using var f = await Create();
        var currencies = new CurrencyManager(f.Source.Source.Tables); currencies.Initialise(f.Actor.Player, new() { Id = 42 }); f.Actor.Set(nameof(Player.CurrencyManager), currencies);
        f.Titles.AddTitle(21); f.Titles.ActiveTitleId = 21;
        f.Actor.OnAuth = () => { f.Titles.RevokeTitle(21); f.Titles.AddTitle(22); f.Titles.ActiveTitleId = 22; };
        await f.Actor.Player.SaveDirect(); await f.Actor.Drain(); f.Pump();
        Assert.Equal(21, Assert.Single(f.Packets<ServerTitleUpdate>()).TitleId); Assert.Equal(21, f.EntityTitle()); Assert.Equal(22, f.Titles.ActiveTitleId);
        using (var read = f.Context()) { Assert.Equal(21, read.Character.Single().Title); Assert.Equal(0, Assert.Single(read.CharacterTitle).Revoked); }
        f.Actor.OnAuth = null; await f.Actor.Player.SaveDirect(); await f.Actor.Drain(); f.Pump();
        Assert.Equal(22, f.EntityTitle()); Assert.Equal(3, f.Packets<ServerTitleUpdate>().Count());
        Assert.Equal(new ushort[] { 21, 0, 22 }, f.Packets<ServerTitleSet>().Select(row => row.Title));
    }

    [Fact]
    public async Task ExpiryKeepsThePublishedSelectionUntilItsRevocationGraphConfirms()
    {
        using var f = await Create(existing: true); f.Titles.Update(6); f.Titles.Update(.01);
        Assert.False(f.Titles.HasTitle(21)); Assert.Equal(0, f.Titles.ActiveTitleId); Assert.Equal(21, f.EntityTitle());
        Assert.Empty(f.Packets<ServerTitleUpdate>()); await f.Save(); f.Pump();
        Assert.True(Assert.Single(f.Packets<ServerTitleUpdate>()).Revoked); Assert.Equal(0, Assert.Single(f.Packets<ServerTitleSet>()).Title); Assert.Equal(0, f.EntityTitle());
    }

    [Fact]
    public async Task AReplacementManagerUsesStoredTitlesWithoutReceivingTheOldManagersPackets()
    {
        using var f = await Create(); f.Titles.AddTitle(21); await f.Save();
        using var read = f.Context(); var model = read.Character.AsNoTracking().Include(row => row.CharacterTitle).Single();
        var replacement = new TitleManager(f.Actor.Player, model, f.Tables); f.Actor.Set(nameof(Player.TitleManager), replacement);
        f.Pump(); Assert.Empty(f.Packets<ServerTitleUpdate>());
        replacement.SendTitles(); Assert.Equal(21, Assert.Single(Assert.Single(f.Packets<ServerTitles>()).Titles).TitleId);
    }

    [Fact]
    public async Task FailedVisibleRecipientRetriesWithoutRepeatingTheOwnersSelection()
    {
        using var f = await Create(existing: true); f.Titles.ActiveTitleId = 0; await f.Save(); f.Pump(); f.Messages.Invocations.Clear();
        var peer = RecordingDispatchProxy<IPlayer>.Create(out var player);
        var session = RecordingDispatchProxy<IWorldSession>.Create(out var messages);
        player.SetProperty(nameof(IPlayer.Identity), new Identity { Id = 43, RealmId = 1 }); player.SetProperty(nameof(IPlayer.Guid), 2u); player.SetProperty(nameof(IPlayer.Session), session);
        messages.SetProperty("Player", peer);
        int attempts = 0; var delivered = new List<ServerTitleSet>();
        messages.SetMethodHandler("EnqueueMessageEncrypted", args =>
        {
            if (args[0] is ServerTitleSet packet) { if (attempts++ == 0) throw new IOException("Owned visible title failure."); delivered.Add(packet); }
            return null;
        });
        var visible = (Dictionary<uint, IGridEntity>)typeof(GridEntity).GetField("visibleEntities", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(f.Actor.Player)!; visible.Add(2, peer);
        f.Titles.ActiveTitleId = 21; await f.Save(); f.Pump();
        Assert.Equal(2, attempts); Assert.Equal(21, Assert.Single(delivered).Title); Assert.Equal(21, Assert.Single(f.Packets<ServerTitleSet>()).Title);
    }

    [Theory]
    [InlineData("identity")] [InlineData("session")]
    public async Task InvalidOwnershipDropsQueuedPacketsWithoutRaisingWorldErrors(string change)
    {
        using var f = await Create(); f.Titles.AddTitle(21); await f.Save();
        Identity identity = f.Actor.Player.Identity;
        try
        {
            if (change == "identity") f.Actor.Set(nameof(Player.Identity), null);
            else f.Messages.SetProperty("Player", null);
            f.Pump(); Assert.Empty(f.Packets<ServerTitleUpdate>()); Assert.Empty(f.Actor.Errors);
        }
        finally { f.Actor.Set(nameof(Player.Identity), identity); f.Messages.SetProperty("Player", f.Actor.Player); }
    }

    [Fact]
    public async Task AchievementCallbackCanSaveTheTitleWithoutLosingItsRetainedNotice()
    {
        using var f = await Create();
        var achievements = RecordingDispatchProxy<ICharacterAchievementManager>.Create(out var callback);
        int saves = 0;
        callback.SetMethodHandler(nameof(ICharacterAchievementManager.CheckAchievements), _ =>
        {
            using var context = f.Context(); f.Actor.Player.Save(context); context.SaveChanges(); saves++; return null;
        });
        f.Actor.Set(nameof(Player.AchievementManager), achievements);
        f.Titles.AddTitle(21); Assert.Equal(1, saves);
        using (var read = f.Context()) Assert.Single(read.CharacterTitle);
        Assert.Empty(f.Packets<ServerTitleUpdate>()); f.Pump(); Assert.Single(f.Packets<ServerTitleUpdate>());
    }

    [Fact]
    public async Task BoundRowSaveCommitsTheOriginalManagerGraphBeforeReleasingItsNotices()
    {
        using var f = await Create(existing: true); f.Titles.AddTitle(22); f.Titles.ActiveTitleId = 22;
        using (var context = f.Context()) { f.Titles.Single(row => row.Entry.Id == 21).Save(context); await context.SaveChangesAsync(); }
        f.Pump();
        using var read = f.Context(); var ids = read.CharacterTitle.Select(row => row.Title).ToArray();
        foreach (var packet in f.Packets<ServerTitleUpdate>()) Assert.Contains(packet.TitleId, ids);
        Assert.Equal(2, ids.Length); Assert.Equal(22, read.Character.Single().Title); Assert.Equal(22, f.EntityTitle());
        Assert.Equal(22, Assert.Single(f.Packets<ServerTitleSet>()).Title);
    }

    [Fact]
    public async Task BulkGrantAndRevocationKeepTheirAcceptedListsAcrossOneSave()
    {
        using var f = await Create(); f.Titles.AddAllTitles(); f.Titles.RevokeAllTitles();
        Assert.Empty(f.Packets<ServerTitles>()); await f.Save(); f.Pump();
        var packets = f.Packets<ServerTitles>().ToArray(); Assert.Equal(2, packets.Length);
        Assert.All(packets[0].Titles, row => Assert.False(row.Revoked)); Assert.All(packets[1].Titles, row => Assert.True(row.Revoked));
        f.Titles.SendTitles(); Assert.All(f.Packets<ServerTitles>().Last().Titles, row => Assert.True(row.Revoked));
        using var read = f.Context(); Assert.All(read.CharacterTitle, row => Assert.Equal(1, row.Revoked));
    }

    [Fact]
    public async Task StandaloneTitleRowsKeepTheirIndependentPersistenceCompatibility()
    {
        using var f = await Create(); var title = new Title(42, f.Tables.CharacterTitle.GetEntry(21)); title.TimeRemaining = 4.5;
        using (var context = f.Context()) { title.Save(context); await context.SaveChangesAsync(); }
        using var read = f.Context(); var row = Assert.Single(read.CharacterTitle);
        Assert.Equal(21, row.Title); Assert.Equal(4u, row.TimeRemaining); Assert.Equal(0, read.Character.Single().Title);
        f.Pump(); Assert.Empty(f.Packets<ServerTitleUpdate>());
    }

    private sealed class LostAcknowledgement : DbTransactionInterceptor
    {
        public override Task TransactionCommittedAsync(DbTransaction transaction, TransactionEndEventData data, CancellationToken token = default)
            => Task.FromException(new IOException("Owned title publication acknowledgement loss."));
    }
    private sealed class SuppressedFlush : SaveChangesInterceptor
    {
        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData data, InterceptionResult<int> result, CancellationToken token = default)
            => new(InterceptionResult<int>.SuppressWithResult(0));
    }
}
