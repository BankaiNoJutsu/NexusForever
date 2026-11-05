using System.Data;
using System.Data.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using NexusForever.Database.Character;
using NexusForever.Database.Character.Model;
using NexusForever.Game.Abstract.Guild;
using NexusForever.Game.Entity;
using NexusForever.Game.Guild;
using NexusForever.Game.Static.Guild;
using NexusForever.Game.Tests.TestSupport;
using Actor = NexusForever.Game.Tests.Challenges.RewardNormalSaveSnapshotTests.Actor;
using Fixture = NexusForever.Game.Tests.Quest.QuestPersistenceDurabilityTests.Fixture;

namespace NexusForever.Game.Tests.Guild;

public sealed class GuildAffiliationRecoveryTests
{
    private readonly DbContextOptions<CharacterContext> options;
    public GuildAffiliationRecoveryTests() { }
    internal GuildAffiliationRecoveryTests(DbContextOptions<CharacterContext> options) => this.options = options;

    [Theory]
    [InlineData("commit")]
    [InlineData("failed-flush")]
    [InlineData("rollback")]
    public async Task AcceptedAffiliationSurvivesAnUncommittedRealPlayerSave(string outcome)
    {
        using var f = new Fixture(options); f.Seed();
        using var actor = new Actor(f); await actor.Load();
        GuildManager manager = Install(actor);
        manager.GuildAffiliation = Guild(7);

        if (outcome == "failed-flush")
            await Assert.ThrowsAsync<IOException>(() => Persist(f, actor.Player, new RejectFlush()));
        else if (outcome == "rollback")
        {
            await using var context = f.Context();
            await using var transaction = await context.Database.BeginTransactionAsync(IsolationLevel.Serializable);
            actor.Player.Save(context); await context.SaveChangesAsync(); await transaction.RollbackAsync();
        }
        else
            await Persist(f, actor.Player);

        if (outcome != "commit")
        {
            using (var before = f.Context()) Assert.Null(before.Character.Single().GuildAffiliation);
            await Persist(f, actor.Player);
        }
        using var stored = f.Context();
        Assert.Equal(7ul, stored.Character.Single().GuildAffiliation);
    }

    [Fact]
    public async Task NormalPlayerSaveFreezesAffiliationBeforeAuthAndRetainsTheLaterSelection()
    {
        using var f = new Fixture(options); f.Seed();
        using var actor = new Actor(f); await actor.Load();
        GuildManager manager = Install(actor);
        manager.GuildAffiliation = Guild(7);
        actor.OnAuth = () => manager.GuildAffiliation = Guild(8);

        await actor.Player.SaveDirect(); await actor.Drain();
        using (var first = f.Context()) Assert.Equal(7ul, first.Character.Single().GuildAffiliation);
        Assert.Equal(8ul, manager.GuildAffiliation.Id);

        actor.OnAuth = null;
        await actor.Player.SaveDirect(); await actor.Drain();
        using var second = f.Context();
        Assert.Equal(8ul, second.Character.Single().GuildAffiliation);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ClearingALoadedAffiliationSurvivesRejectedSaves(bool reject)
    {
        using var f = new Fixture(options); f.Seed();
        using (var seed = f.Context()) { seed.Character.Single().GuildAffiliation = 7; await seed.SaveChangesAsync(); }
        using var actor = new Actor(f); await actor.Load();
        GuildManager manager = Install(actor, new CharacterModel { Id = 42, GuildAffiliation = 7 }, Guild(7));
        manager.GuildAffiliation = null;
        if (reject) await Assert.ThrowsAsync<IOException>(() => Persist(f, actor.Player, new RejectFlush()));
        await Persist(f, actor.Player);
        using var stored = f.Context(); Assert.Null(stored.Character.Single().GuildAffiliation);
    }

    [Theory]
    [InlineData("commit")]
    [InlineData("rollback")]
    [InlineData("accept-all")]
    public async Task LaterSelectionAfterFlushRemainsPendingForTheNextRealPlayerSave(string outcome)
    {
        using var f = new Fixture(options); f.Seed();
        using var actor = new Actor(f); await actor.Load();
        GuildManager manager = Install(actor); manager.GuildAffiliation = Guild(7);
        await using (var context = f.Context())
        await using (var transaction = await context.Database.BeginTransactionAsync(IsolationLevel.Serializable))
        {
            actor.Player.Save(context); await context.SaveChangesAsync(false);
            if (outcome == "accept-all") context.ChangeTracker.AcceptAllChanges();
            manager.GuildAffiliation = Guild(8);
            if (outcome == "rollback") await transaction.RollbackAsync(); else await transaction.CommitAsync();
        }
        using (var before = f.Context()) Assert.Equal(outcome == "rollback" ? null : (ulong?)7, before.Character.Single().GuildAffiliation);
        await Persist(f, actor.Player);
        using var stored = f.Context(); Assert.Equal(8ul, stored.Character.Single().GuildAffiliation);
    }

    [Fact]
    public async Task LostCommitAcknowledgementReconcilesTheStoredValueBeforeApplyingALaterSelection()
    {
        using var f = new Fixture(options); f.Seed();
        using var actor = new Actor(f); await actor.Load();
        GuildManager manager = Install(actor); manager.GuildAffiliation = Guild(7);
        await Assert.ThrowsAsync<IOException>(() => CharacterAtomicSave.Execute(() => f.Context(new LostAcknowledgement()), 42, actor.Player.Save));
        using (var committed = f.Context()) Assert.Equal(7ul, committed.Character.Single().GuildAffiliation);
        manager.GuildAffiliation = Guild(8);
        await Persist(f, actor.Player);
        using var stored = f.Context(); Assert.Equal(8ul, stored.Character.Single().GuildAffiliation);
    }

    [Fact]
    public async Task SuppressedFlushDoesNotConsumeTheAcceptedAffiliation()
    {
        using var f = new Fixture(options); f.Seed();
        using var actor = new Actor(f); await actor.Load();
        GuildManager manager = Install(actor); manager.GuildAffiliation = Guild(7);
        await Persist(f, actor.Player, new SuppressedFlush());
        using (var before = f.Context()) Assert.Null(before.Character.Single().GuildAffiliation);
        await Persist(f, actor.Player);
        using var stored = f.Context(); Assert.Equal(7ul, stored.Character.Single().GuildAffiliation);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ForeignAffiliationSqlOrTrackedIntentRejectsTheWholeContext(bool tracked)
    {
        using var f = new Fixture(options); f.Seed();
        using var actor = new Actor(f); await actor.Load();
        GuildManager manager = Install(actor); manager.GuildAffiliation = Guild(7);
        if (!tracked)
        {
            using var external = f.Context(); await external.Character.ExecuteUpdateAsync(set => set.SetProperty(row => row.GuildAffiliation, (ulong?)99));
        }
        using (var context = f.Context())
        {
            context.Character.Single().Name = "Must roll back";
            if (tracked) context.Character.Single().GuildAffiliation = 99;
            Assert.Throws<InvalidOperationException>(() => manager.Save(context));
            Assert.True(context.IsSaveRejected);
            await Assert.ThrowsAsync<InvalidOperationException>(() => context.SaveChangesAsync());
        }
        using (var read = f.Context()) Assert.NotEqual("Must roll back", read.Character.Single().Name);
        if (!tracked)
        {
            // Repair only this fixture's injected conflicting value, not the production baseline.
            using var repair = f.Context(); await repair.Character.ExecuteUpdateAsync(set => set.SetProperty(row => row.GuildAffiliation, (ulong?)null));
        }
        await Persist(f, actor.Player);
        using var stored = f.Context(); Assert.Equal(7ul, stored.Character.Single().GuildAffiliation);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task TwoLoadedManagersCannotShareAnUnfinishedAffiliationWrite(bool sameContext)
    {
        using var f = new Fixture(options); f.Seed();
        using var actor = new Actor(f); await actor.Load();
        GuildManager first = Install(actor); first.GuildAffiliation = Guild(7);
        var global = RecordingDispatchProxy<IGlobalGuildManager>.Create(out var proxy);
        proxy.SetMethodReturn(nameof(IGlobalGuildManager.GetCharacterGuilds), Array.Empty<IGuildBase>());
        var second = new GuildManager(null, globalGuildManager: global);
        second.Initialise(f.Source.Player, new CharacterModel { Id = 42 }); second.GuildAffiliation = Guild(8);
        using (var context = f.Context())
        {
            first.Save(context);
            using var other = sameContext ? null : f.Context();
            Assert.Throws<InvalidOperationException>(() => second.Save(other ?? context));
            if (sameContext) await Assert.ThrowsAsync<InvalidOperationException>(() => context.SaveChangesAsync());
            else await context.SaveChangesAsync();
        }
        await Persist(f, actor.Player);
        using var stored = f.Context(); Assert.Equal(7ul, stored.Character.Single().GuildAffiliation);
    }

    [Fact]
    public async Task FailedAffiliationCannotMoveItsIntentToAnotherDatabase()
    {
        using var f = new Fixture(options); f.Seed();
        using var actor = new Actor(f); await actor.Load();
        GuildManager manager = Install(actor); manager.GuildAffiliation = Guild(7);
        await Assert.ThrowsAsync<IOException>(() => Persist(f, actor.Player, new RejectFlush()));
        using var foreign = new Fixture(null); foreign.Seed();
        using (var context = foreign.Context())
        {
            Assert.Throws<InvalidOperationException>(() => manager.Save(context)); Assert.True(context.IsSaveRejected);
            Assert.Null(context.Character.Single().GuildAffiliation);
        }
        await Persist(f, actor.Player);
        using var stored = f.Context(); Assert.Equal(7ul, stored.Character.Single().GuildAffiliation);
    }

    [Fact]
    public async Task ReplacingTheManagerDuringAuthRejectsTheFrozenCharacterGraph()
    {
        using var f = new Fixture(options); f.Seed();
        using var actor = new Actor(f); await actor.Load();
        GuildManager manager = Install(actor); manager.GuildAffiliation = Guild(7);
        actor.Quest.Single().Progress = 2;
        actor.OnAuth = () => actor.Set(nameof(Player.GuildManager), RecordingDispatchProxy<IGuildManager>.Create(out _));
        await Assert.ThrowsAsync<InvalidOperationException>(() => actor.Player.SaveDirect());
        using var stored = f.Context(); Assert.Null(stored.Character.Single().GuildAffiliation);
        Assert.Equal(1u, stored.CharacterQuestObjective.Single().Progress);
    }

    [Fact]
    public async Task FrozenAffiliationRetryKeepsItsOriginalSelectionAndThenSavesTheLaterEdit()
    {
        using var f = new Fixture(options); f.Seed();
        using var actor = new Actor(f); await actor.Load();
        GuildManager manager = Install(actor); manager.GuildAffiliation = Guild(7);
        var snapshot = actor.Player.CaptureNormalRewardSave();
        try
        {
            await using (var context = f.Context())
            await using (var transaction = await context.Database.BeginTransactionAsync(IsolationLevel.Serializable))
            {
                snapshot.SaveGuildAffiliation(context); await context.SaveChangesAsync();
                manager.GuildAffiliation = Guild(8); await transaction.RollbackAsync();
            }
            using (var retry = f.Context()) { snapshot.SaveGuildAffiliation(retry); await retry.SaveChangesAsync(); }
            using var stored = f.Context(); Assert.Equal(7ul, stored.Character.Single().GuildAffiliation);
        }
        finally { Assert.True(snapshot.Finish(true)); }
        await Persist(f, actor.Player);
        using var later = f.Context(); Assert.Equal(8ul, later.Character.Single().GuildAffiliation);
    }

    [Fact]
    public async Task RepeatedScalarEnrollmentPreservesOtherCharacterColumnsAndUnflushedLaterValues()
    {
        using var f = new Fixture(options); f.Seed();
        using var actor = new Actor(f); await actor.Load();
        GuildManager manager = Install(actor); manager.GuildAffiliation = Guild(7);
        await using (var context = f.Context())
        await using (var transaction = await context.Database.BeginTransactionAsync(IsolationLevel.Serializable))
        {
            var row = context.Character.Single(); row.Name = "First name";
            manager.Save(context); await context.SaveChangesAsync(false);
            row.Name = "Later name"; manager.GuildAffiliation = Guild(8);
            manager.Save(context);
            Assert.True(context.Entry(row).Property(value => value.Name).IsModified);
            await context.SaveChangesAsync(); await transaction.CommitAsync();
        }
        using var stored = f.Context(); Assert.Equal("Later name", stored.Character.Single().Name);
        Assert.Equal(8ul, stored.Character.Single().GuildAffiliation);
    }

    [Fact]
    public async Task NoOpDoesNotMarkTheAffiliationColumnAndSavepointRollbackRetainsItsLaterChange()
    {
        using var f = new Fixture(options); f.Seed();
        using var actor = new Actor(f); await actor.Load(); GuildManager manager = Install(actor);
        await using (var context = f.Context())
        await using (var transaction = await context.Database.BeginTransactionAsync(IsolationLevel.Serializable))
        {
            actor.Player.Save(context);
            Assert.False(context.Entry(context.Character.Single()).Property(row => row.GuildAffiliation).IsModified);
            await transaction.CreateSavepointAsync("owned_affiliation");
            manager.GuildAffiliation = Guild(7); manager.Save(context); await context.SaveChangesAsync();
            await transaction.RollbackToSavepointAsync("owned_affiliation"); await transaction.CommitAsync();
        }
        await Persist(f, actor.Player);
        using var stored = f.Context(); Assert.Equal(7ul, stored.Character.Single().GuildAffiliation);
    }

    private static GuildManager Install(Actor actor, CharacterModel model = null, params IGuildBase[] memberships)
    {
        IGlobalGuildManager global = RecordingDispatchProxy<IGlobalGuildManager>.Create(out var proxy);
        proxy.SetMethodReturn(nameof(IGlobalGuildManager.GetCharacterGuilds), memberships);
        var manager = new GuildManager(null, globalGuildManager: global);
        manager.Initialise(actor.Player, model ?? new CharacterModel { Id = 42 });
        actor.Set(nameof(Player.GuildManager), manager);
        return manager;
    }

    private static IGuildBase Guild(ulong id)
    {
        IGuildBase guild = RecordingDispatchProxy<IGuildBase>.Create(out var proxy);
        proxy.SetProperty(nameof(IGuildBase.Id), id);
        proxy.SetProperty(nameof(IGuildBase.Name), $"Affiliation {id}");
        proxy.SetProperty(nameof(IGuildBase.Type), GuildType.Circle);
        return guild;
    }

    private static async Task Persist(Fixture f, Player player, params IInterceptor[] interceptors)
    {
        await using var context = f.Context(interceptors);
        player.Save(context);
        await context.SaveChangesAsync();
    }

    private sealed class RejectFlush : SaveChangesInterceptor
    {
        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
            DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
            => throw new IOException("Owned affiliation save rejection.");
    }

    private sealed class SuppressedFlush : SaveChangesInterceptor
    {
        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
            DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
            => new(InterceptionResult<int>.SuppressWithResult(0));
    }

    private sealed class LostAcknowledgement : DbTransactionInterceptor
    {
        public override Task TransactionCommittedAsync(DbTransaction transaction, TransactionEndEventData eventData, CancellationToken cancellationToken = default)
            => Task.FromException(new IOException("Owned affiliation commit acknowledgement loss."));
    }
}
