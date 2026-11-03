using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using System.Data;
using System.Data.Common;
using NexusForever.Database.Character;
using NexusForever.Database.Character.Model;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Entity;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.Game.Tests.Transport;
using NexusForever.GameTable;
using NexusForever.GameTable.Model;
using NexusForever.Network.World.Message.Model;
using Actor = NexusForever.Game.Tests.Challenges.RewardNormalSaveSnapshotTests.Actor;
using Fixture = NexusForever.Game.Tests.Quest.QuestPersistenceDurabilityTests.Fixture;

namespace NexusForever.Game.Tests.Entity;

public sealed class TitlePersistenceDurabilityTests
{
    private readonly DbContextOptions<CharacterContext> options;
    public TitlePersistenceDurabilityTests() { }
    internal TitlePersistenceDurabilityTests(DbContextOptions<CharacterContext> options) => this.options = options;

    [Theory]
    [InlineData("grant")] [InlineData("expiry")] [InlineData("revoke-before-create")]
    public async Task PublicGrantExpiryAndEarlyRevocationPersistThroughTheRealPlayerSave(string action)
    {
        using var f = new Fixture(options); f.Seed(); Seed(f, action == "expiry");
        using var actor = new Actor(f); await actor.Load(); var titles = Titles(f, actor);
        ITitle runtime;
        if (action == "expiry") { runtime = Assert.Single(titles); titles.Update(6); }
        else
        {
            titles.AddTitle(21); titles.ActiveTitleId = 21;
            runtime = Assert.Single(titles);
            if (action == "revoke-before-create") titles.RevokeTitle(21);
        }
        bool revoked = action != "grant";
        Assert.Equal(!revoked, titles.HasTitle(21)); Assert.Equal(revoked ? (ushort)0 : (ushort)21, titles.ActiveTitleId);
        Assert.DoesNotContain(f.Source.SessionProxy.GetInvocations("EnqueueMessageEncrypted"), call => call.Arguments[0] is ServerTitleUpdate);
        if (action != "revoke-before-create") await Assert.ThrowsAsync<IOException>(() => Persist(f, actor, true));
        await Persist(f, actor, false);
        using var stored = f.Context(); var title = Assert.Single(stored.CharacterTitle);
        Assert.Equal(revoked ? (byte)1 : (byte)0, title.Revoked);
        Assert.Equal(action == "expiry" ? -1d : 5d, runtime.TimeRemaining);
        Assert.Equal(unchecked((uint)(runtime.TimeRemaining ?? 0d)), title.TimeRemaining);
        Assert.Equal(revoked ? (ushort)0 : (ushort)21, stored.Character.Single().Title);
        Assert.Empty(stored.CharacterEntitlement);
    }

    [Theory]
    [InlineData("dispose")] [InlineData("rollback")] [InlineData("suppressed")] [InlineData("lost-ack")]
    public async Task AProvisionalGrantAndActiveSelectionRetryTogetherWithoutDuplicateRows(string fault)
    {
        using var f = new Fixture(options); f.Seed(); Seed(f, false);
        using var actor = new Actor(f); await actor.Load(); var titles = Titles(f, actor); titles.AddTitle(21); titles.ActiveTitleId = 21;
        if (fault == "lost-ack") await Assert.ThrowsAsync<IOException>(() => CharacterAtomicSave.Execute(() => f.Context(new LostAcknowledgement()), 42, titles.Save));
        else
        {
            await using var context = f.Context(new SuppressedFlush(fault == "suppressed"));
            await using var transaction = await context.Database.BeginTransactionAsync(IsolationLevel.Serializable);
            titles.Save(context); if (fault != "dispose") await context.SaveChangesAsync(); await transaction.RollbackAsync();
        }
        using (var read = f.Context()) Assert.Equal(fault == "lost-ack" ? 1 : 0, read.CharacterTitle.Count());
        await CharacterAtomicSave.Execute(() => f.Context(), 42, titles.Save);
        using var stored = f.Context(); Assert.Equal(21, stored.Character.Single().Title); Assert.Equal(0, Assert.Single(stored.CharacterTitle).Revoked);
    }

    [Fact]
    public async Task RealNormalSaveFreezesBeforeAuthAndRetainsNewGrantsRevocationAndSelection()
    {
        using var f = new Fixture(options); f.Seed(); Seed(f, false);
        using var actor = new Actor(f); await actor.Load(); var titles = Titles(f, actor);
        var currencies = new CurrencyManager(f.Source.Tables); currencies.Initialise(actor.Player, new CharacterModel { Id = 42 }); actor.Set(nameof(Player.CurrencyManager), currencies);
        titles.AddTitle(21); titles.ActiveTitleId = 21;
        actor.OnAuth = () => { titles.RevokeTitle(21); titles.AddTitle(22); titles.ActiveTitleId = 22; };
        await actor.Player.SaveDirect(); await actor.Drain();
        using (var read = f.Context()) { Assert.Equal(0, Assert.Single(read.CharacterTitle).Revoked); Assert.Equal(21, read.Character.Single().Title); }
        actor.OnAuth = null; await actor.Player.SaveDirect(); await actor.Drain();
        using var stored = f.Context(); Assert.Equal(2, stored.CharacterTitle.Count()); Assert.Equal(1, stored.CharacterTitle.Single(row => row.Title == 21).Revoked);
        Assert.Equal(0, stored.CharacterTitle.Single(row => row.Title == 22).Revoked); Assert.Equal(22, stored.Character.Single().Title); Assert.Empty(stored.CharacterEntitlement);
    }

    [Fact]
    public async Task ARegrantedAndReequippedTitleRemainsPendingWhenAnEarlierRevocationCommits()
    {
        using var f = new Fixture(options); f.Seed(); Seed(f, true);
        using var actor = new Actor(f); await actor.Load(); var titles = Titles(f, actor); titles.RevokeTitle(21);
        await using (var context = f.Context()) await using (var transaction = await context.Database.BeginTransactionAsync(IsolationLevel.Serializable))
        {
            titles.Save(context); await context.SaveChangesAsync(); titles.AddTitle(21); titles.ActiveTitleId = 21;
            using var competing = f.Context(); Assert.Throws<InvalidOperationException>(() => titles.Save(competing)); await transaction.CommitAsync();
        }
        using (var read = f.Context()) { Assert.Equal(1, read.CharacterTitle.Single().Revoked); Assert.Equal(0, read.Character.Single().Title); }
        await CharacterAtomicSave.Execute(() => f.Context(), 42, titles.Save);
        using var stored = f.Context(); Assert.Equal(0, stored.CharacterTitle.Single().Revoked); Assert.Equal(21, stored.Character.Single().Title);
    }

    [Fact]
    public async Task ExplicitEfAcceptanceAcknowledgesOnlyTheFlushedTimerAndPartialTitleSavesCompose()
    {
        using var f = new Fixture(options); f.Seed(); Seed(f, true);
        using var actor = new Actor(f); await actor.Load(); var titles = Titles(f, actor); var title = Assert.Single(titles);
        await using (var context = f.Context()) await using (var transaction = await context.Database.BeginTransactionAsync(IsolationLevel.Serializable))
        {
            title.TimeRemaining = 4.5; title.Save(context); title.Revoked = true; title.Save(context);
            await context.SaveChangesAsync(false); context.ChangeTracker.AcceptAllChanges(); title.TimeRemaining = 3.5; await transaction.CommitAsync();
        }
        using (var read = f.Context()) { Assert.Equal(4u, read.CharacterTitle.Single().TimeRemaining); Assert.Equal(1, read.CharacterTitle.Single().Revoked); }
        await CharacterAtomicSave.Execute(() => f.Context(), 42, title.Save);
        using var stored = f.Context(); Assert.Equal(3u, stored.CharacterTitle.Single().TimeRemaining); Assert.Equal(1, stored.CharacterTitle.Single().Revoked); Assert.Equal(21, stored.Character.Single().Title);
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task ForeignSqlOrTrackedTitleChangesCannotBeOverwritten(bool tracked)
    {
        using var f = new Fixture(options); f.Seed(); Seed(f, true);
        using var actor = new Actor(f); await actor.Load(); var titles = Titles(f, actor); Assert.Single(titles).TimeRemaining = 4.5;
        if (!tracked) { using var external = f.Context(); await external.CharacterTitle.ExecuteUpdateAsync(set => set.SetProperty(row => row.Revoked, (byte)1)); }
        await Assert.ThrowsAsync<InvalidOperationException>(() => CharacterAtomicSave.Execute(() => f.Context(), 42, context =>
        { titles.Save(context); if (tracked) context.CharacterTitle.Local.Single().TimeRemaining = 900; }));
        using var stored = f.Context(); Assert.Equal(5u, stored.CharacterTitle.Single().TimeRemaining); Assert.Equal(tracked ? 0 : 1, stored.CharacterTitle.Single().Revoked);
    }

    [Fact]
    public async Task UnavailableStaticTitlesRetainRawColumnsAndNoOpsEnrollNothing()
    {
        using var f = new Fixture(options); f.Seed(); Seed(f, false);
        using (var seed = f.Context()) { seed.CharacterTitle.Add(new() { Id = 42, Title = 99, TimeRemaining = 71, Revoked = 2 }); seed.SaveChanges(); }
        using var actor = new Actor(f); await actor.Load(); var titles = Titles(f, actor); Assert.Empty(titles);
        using (var context = f.Context()) { titles.Save(context); Assert.Empty(context.ChangeTracker.Entries()); }
        titles.AddTitle(22); await Persist(f, actor, false);
        using var stored = f.Context(); var retained = stored.CharacterTitle.Single(row => row.Title == 99);
        Assert.Equal(71u, retained.TimeRemaining); Assert.Equal(2, retained.Revoked); Assert.Equal(2, stored.CharacterTitle.Count()); Assert.Equal(0, stored.Character.Single().Title);
    }

    private sealed class SuppressedFlush(bool suppress) : SaveChangesInterceptor
    {
        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData data, InterceptionResult<int> result, CancellationToken cancellationToken = default)
            => new(suppress ? InterceptionResult<int>.SuppressWithResult(0) : result);
    }
    private sealed class LostAcknowledgement : DbTransactionInterceptor
    {
        public override Task TransactionCommittedAsync(DbTransaction transaction, TransactionEndEventData data, CancellationToken cancellationToken = default)
            => Task.FromException(new IOException("Owned title acknowledgement loss."));
    }

    private static void Seed(Fixture f, bool existing)
    {
        using var context = f.Context(); context.Character.Single().Title = existing ? (ushort)21 : (ushort)0;
        if (existing) context.CharacterTitle.Add(new() { Id = 42, Title = 21, TimeRemaining = 5 }); context.SaveChanges();
    }
    private static TitleManager Titles(Fixture f, Actor actor)
    {
        typeof(GridEntity).GetField("visibleEntities", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!
            .SetValue(actor.Player, new Dictionary<uint, IGridEntity>());
        var tables = RecordingDispatchProxy<IGameTableManager>.Create(out var proxy);
        proxy.SetProperty(nameof(IGameTableManager.CharacterTitle), PlayerBindPointTests.Table(new CharacterTitleEntry { Id = 21, LifeTimeSeconds = 5 }, new CharacterTitleEntry { Id = 22 }));
        using var read = f.Context(); var model = read.Character.AsNoTracking().Include(row => row.CharacterTitle).Single();
        var titles = new TitleManager(actor.Player, model, tables); actor.Set(nameof(Player.TitleManager), titles); return titles;
    }
    private static Task Persist(Fixture f, Actor actor, bool fail)
        => CharacterAtomicSave.Execute(() => { var context = f.Context(); context.SavingChanges += (_, _) => { if (fail) throw new IOException("Owned title flush fault."); }; return context; }, 42, actor.Player.Save);
}
