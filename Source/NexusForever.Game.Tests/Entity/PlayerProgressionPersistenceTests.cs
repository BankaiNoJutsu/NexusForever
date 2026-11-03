using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using System.Data;
using System.Data.Common;
using NexusForever.Database.Character;
using NexusForever.Database.Character.Model;
using NexusForever.Game.Entity;
using NexusForever.Game.Static.Crafting;
using NexusForever.Game.Tests.Transport;
using NexusForever.GameTable;
using NexusForever.GameTable.Model;
using Actor = NexusForever.Game.Tests.Challenges.RewardNormalSaveSnapshotTests.Actor;
using Fixture = NexusForever.Game.Tests.Quest.QuestPersistenceDurabilityTests.Fixture;

namespace NexusForever.Game.Tests.Entity;

public sealed class PlayerProgressionPersistenceTests
{
    private readonly DbContextOptions<CharacterContext> options;
    public PlayerProgressionPersistenceTests() { }
    internal PlayerProgressionPersistenceTests(DbContextOptions<CharacterContext> options) => this.options = options;
    private const BindingFlags Private = BindingFlags.NonPublic | BindingFlags.Static;

    [Theory]
    [InlineData("new")] [InlineData("archive")] [InlineData("xp-talents")]
    public async Task AcceptedLearningArchiveXpTalentsAndDiscoverySurviveARealPlayerFlushFailure(string action)
    {
        using var f = new Fixture(options); f.Seed(); Seed(f, action != "new");
        using var actor = new Actor(f); await actor.Load(); Configure(f, actor);
        if (action == "archive")
        {
            Assert.True(actor.Player.LearnTradeskill(TradeskillType.Weaponsmith, TradeskillType.Armorer)); actor.Player.DiscoverSchematic(501);
        }
        else
        {
            if (action == "new") Assert.True(actor.Player.LearnTradeskill(TradeskillType.Armorer, 0));
            Assert.Equal(20u, actor.Player.AddTradeskillXp(TradeskillType.Armorer, 20));
            actor.Player.EnsureTradeskillTalentPointTotal(TradeskillType.Armorer, 10);
            Assert.True(actor.Player.PickTradeskillTalent(TradeskillType.Armorer, 1, 88));
            if (action == "new") Assert.True(actor.Player.LearnSchematic(501, true));
        }
        await Assert.ThrowsAsync<IOException>(() => Persist(f, actor, true)); await Persist(f, actor, false);
        using var stored = f.Context();
        var armorer = stored.CharacterTradeskill.Single(row => row.TradeskillId == (uint)TradeskillType.Armorer);
        Assert.Equal(action == "archive" ? 0u : 1u, armorer.IsActive);
        Assert.Equal(action == "new" ? 20u : action == "archive" ? 180u : 200u, armorer.TradeskillXp);
        if (action == "archive")
        {
            Assert.Equal(1u, stored.CharacterTradeskill.Single(row => row.TradeskillId == (uint)TradeskillType.Weaponsmith).IsActive);
            Assert.Equal(7u, armorer.TalentPoints); Assert.Equal(77u, armorer.TalentTier00);
        }
        else { Assert.Equal(action == "new" ? 9u : 8u, armorer.TalentPoints); Assert.Equal(88u, armorer.TalentTier01); }
        var schematic = Assert.Single(stored.CharacterSchematic);
        Assert.Equal(action != "xp-talents", schematic.Discovered);
        Assert.Equal(action == "xp-talents" ? 0f : 18f, schematic.DiscoveryCoordinateX);
        Assert.Equal(action == "xp-talents" ? 0f : 27f, schematic.DiscoveryCoordinateY); Assert.Empty(stored.CharacterEntitlement);
    }

    [Theory]
    [InlineData("dispose")] [InlineData("rollback")] [InlineData("suppressed")] [InlineData("lost-ack")]
    public async Task AProvisionalSkillAndRecipeGraphRetriesWithoutDroppingOrDuplicatingTheAcceptedAction(string fault)
    {
        using var f = new Fixture(options); f.Seed(); using var actor = new Actor(f); await actor.Load(); Configure(f, actor);
        Assert.True(actor.Player.LearnTradeskill(TradeskillType.Armorer, 0)); Assert.True(actor.Player.LearnSchematic(501, true));
        if (fault == "lost-ack") await Assert.ThrowsAsync<IOException>(() => CharacterAtomicSave.Execute(() => f.Context(new LostAcknowledgement()), 42, actor.Player.Save));
        else
        {
            await using var context = f.Context(new SuppressedFlush(fault == "suppressed"));
            await using var transaction = await context.Database.BeginTransactionAsync(IsolationLevel.Serializable);
            actor.Player.Save(context); if (fault != "dispose") await context.SaveChangesAsync(); await transaction.RollbackAsync();
        }
        using (var read = f.Context()) { Assert.Equal(fault == "lost-ack" ? 1 : 0, read.CharacterTradeskill.Count()); Assert.Equal(fault == "lost-ack" ? 1 : 0, read.CharacterSchematic.Count()); }
        await Persist(f, actor, false); using var stored = f.Context(); Assert.Equal(1u, Assert.Single(stored.CharacterTradeskill).IsActive); Assert.True(Assert.Single(stored.CharacterSchematic).Discovered);
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task RealNormalSaveFreezesBothCollectionsBeforeAuthAndRetainsLaterProducerActions(bool existing)
    {
        using var f = new Fixture(options); f.Seed(); Seed(f, existing); using var actor = new Actor(f); await actor.Load(); Configure(f, actor);
        var currency = new CurrencyManager(f.Source.Tables); currency.Initialise(actor.Player, new CharacterModel { Id = 42 }); actor.Set(nameof(Player.CurrencyManager), currency);
        actor.OnAuth = () =>
        {
            if (existing) { actor.Player.LearnTradeskill(TradeskillType.Weaponsmith, TradeskillType.Armorer); actor.Player.DiscoverSchematic(501); }
            else { actor.Player.LearnTradeskill(TradeskillType.Armorer, 0); actor.Player.LearnSchematic(501, true); actor.Player.AddTradeskillXp(TradeskillType.Armorer, 20); }
        };
        await actor.Player.SaveDirect(); await actor.Drain();
        using (var read = f.Context())
        {
            Assert.Equal(existing ? 1 : 0, read.CharacterTradeskill.Count()); Assert.Equal(existing ? 1 : 0, read.CharacterSchematic.Count());
            if (existing) { Assert.Equal(1u, read.CharacterTradeskill.Single().IsActive); Assert.False(read.CharacterSchematic.Single().Discovered); }
        }
        actor.OnAuth = null; await actor.Player.SaveDirect(); await actor.Drain(); using var stored = f.Context();
        Assert.Equal(existing ? 2 : 1, stored.CharacterTradeskill.Count()); Assert.True(stored.CharacterSchematic.Single().Discovered);
        Assert.Equal(existing ? 0u : 1u, stored.CharacterTradeskill.Single(row => row.TradeskillId == (uint)TradeskillType.Armorer).IsActive);
        if (!existing) Assert.Equal(20u, stored.CharacterTradeskill.Single().TradeskillXp);
        Assert.Empty(stored.CharacterEntitlement);
    }

    [Fact]
    public async Task AResetAcceptedAfterFlushRemainsPendingAndClearsAllTenTalentsOnTheNextSave()
    {
        using var f = new Fixture(options); f.Seed(); Seed(f, true); using var actor = new Actor(f); await actor.Load(); Configure(f, actor);
        Assert.True(actor.Player.PickTradeskillTalent(TradeskillType.Armorer, 9, 99));
        await using (var context = f.Context()) await using (var transaction = await context.Database.BeginTransactionAsync(IsolationLevel.Serializable))
        {
            actor.Player.Save(context); await context.SaveChangesAsync(); Assert.True(actor.Player.ResetTradeskillTalents(TradeskillType.Armorer));
            using var competing = f.Context(); Assert.Throws<InvalidOperationException>(() => actor.Player.Save(competing)); await transaction.CommitAsync();
        }
        using (var read = f.Context()) { Assert.Equal(77u, read.CharacterTradeskill.Single().TalentTier00); Assert.Equal(99u, read.CharacterTradeskill.Single().TalentTier09); }
        await Persist(f, actor, false); using var stored = f.Context(); var row = stored.CharacterTradeskill.Single(); Assert.Equal(0u, row.TalentPoints);
        for (int tier = 0; tier < 10; tier++) Assert.Equal(0u, typeof(CharacterTradeskillModel).GetProperty($"TalentTier{tier:D2}")!.GetValue(row));
    }

    [Fact]
    public async Task ReactivationDuringAnEarlierArchiveCommitKeepsBothArchivedRowsAndTheirProgress()
    {
        using var f = new Fixture(options); f.Seed(); Seed(f, true); using var actor = new Actor(f); await actor.Load(); Configure(f, actor);
        actor.Player.LearnTradeskill(TradeskillType.Weaponsmith, TradeskillType.Armorer);
        await using (var context = f.Context()) await using (var transaction = await context.Database.BeginTransactionAsync(IsolationLevel.Serializable))
        { actor.Player.Save(context); await context.SaveChangesAsync(); actor.Player.LearnTradeskill(TradeskillType.Armorer, TradeskillType.Weaponsmith); await transaction.CommitAsync(); }
        await Persist(f, actor, false); using var read = f.Context(); Assert.Equal(2, read.CharacterTradeskill.Count());
        var armorer = read.CharacterTradeskill.Single(row => row.TradeskillId == (uint)TradeskillType.Armorer);
        Assert.Equal(1u, armorer.IsActive); Assert.Equal(180u, armorer.TradeskillXp); Assert.Equal(77u, armorer.TalentTier00);
        Assert.Equal(0u, read.CharacterTradeskill.Single(row => row.TradeskillId == (uint)TradeskillType.Weaponsmith).IsActive);
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task ForeignSqlOrTrackedRecipeChangesRejectTheWholeSkillAndRecipeSave(bool tracked)
    {
        using var f = new Fixture(options); f.Seed(); Seed(f, true); using var actor = new Actor(f); await actor.Load(); Configure(f, actor);
        actor.Player.AddTradeskillXp(TradeskillType.Armorer, 20); actor.Player.DiscoverSchematic(501);
        if (!tracked) { using var external = f.Context(); await external.CharacterSchematic.ExecuteUpdateAsync(set => set.SetProperty(row => row.DiscoveryCoordinateX, 900f)); }
        await Assert.ThrowsAsync<InvalidOperationException>(() => CharacterAtomicSave.Execute(() => f.Context(), 42, context =>
        { actor.Player.Save(context); if (tracked) context.CharacterSchematic.Local.Single().DiscoveryCoordinateX = 900; }));
        using var read = f.Context(); Assert.Equal(180u, read.CharacterTradeskill.Single().TradeskillXp); Assert.False(read.CharacterSchematic.Single().Discovered);
        Assert.Equal(tracked ? 0f : 900f, read.CharacterSchematic.Single().DiscoveryCoordinateX);
    }

    [Fact]
    public async Task RecoveryRejectsAMixtureOfIndividuallyMatchingBeforeAndAfterRows()
    {
        using var f = new Fixture(options); f.Seed(); Seed(f, true); using var actor = new Actor(f); await actor.Load(); Configure(f, actor);
        actor.Player.LearnTradeskill(TradeskillType.Weaponsmith, TradeskillType.Armorer); actor.Player.DiscoverSchematic(501);
        using (var unflushed = f.Context()) actor.Player.Save(unflushed);
        using (var external = f.Context()) await external.CharacterTradeskill.ExecuteUpdateAsync(set => set.SetProperty(row => row.IsActive, 0u));
        await Assert.ThrowsAsync<InvalidOperationException>(() => Persist(f, actor, false)); using var stored = f.Context();
        Assert.Single(stored.CharacterTradeskill); Assert.Equal(0u, stored.CharacterTradeskill.Single().IsActive); Assert.False(stored.CharacterSchematic.Single().Discovered);
    }

    [Fact]
    public async Task ExplicitEfAcceptanceAcknowledgesTheFlushedXpAndRetainsLaterEarning()
    {
        using var f = new Fixture(options); f.Seed(); Seed(f, true); using var actor = new Actor(f); await actor.Load(); Configure(f, actor);
        await using (var context = f.Context()) await using (var transaction = await context.Database.BeginTransactionAsync(IsolationLevel.Serializable))
        {
            actor.Player.AddTradeskillXp(TradeskillType.Armorer, 20); actor.Player.Save(context);
            await context.SaveChangesAsync(false); context.ChangeTracker.AcceptAllChanges();
            actor.Player.AddTradeskillXp(TradeskillType.Armorer, 5); await transaction.CommitAsync();
        }
        using (var stored = f.Context()) Assert.Equal(200u, stored.CharacterTradeskill.Single().TradeskillXp);
        await Persist(f, actor, false);
        using var read = f.Context(); Assert.Equal(205u, read.CharacterTradeskill.Single().TradeskillXp); Assert.Equal(7u, read.CharacterTradeskill.Single().TalentPoints);
    }

    [Fact]
    public async Task AFailedFirstAttemptCannotMoveItsProgressionIntentToAnotherDatabase()
    {
        using var f = new Fixture(options); f.Seed(); Seed(f, true); using var actor = new Actor(f); await actor.Load(); Configure(f, actor);
        actor.Player.AddTradeskillXp(TradeskillType.Armorer, 20); await Assert.ThrowsAsync<IOException>(() => Persist(f, actor, true));
        using var foreign = new Fixture(null); foreign.Seed(); Seed(foreign, true);
        await Assert.ThrowsAsync<InvalidOperationException>(() => Persist(foreign, actor, false));
        using (var read = foreign.Context()) Assert.Equal(180u, read.CharacterTradeskill.Single().TradeskillXp);
        await Persist(f, actor, false); using var stored = f.Context(); Assert.Equal(200u, stored.CharacterTradeskill.Single().TradeskillXp);
    }

    [Fact]
    public async Task NoOpsDoNotEnrollProgressionRowsAndUntouchedPayloadSurvivesAnXpWrite()
    {
        using var f = new Fixture(options); f.Seed(); Seed(f, true);
        using (var seed = f.Context())
        {
            var row = seed.CharacterTradeskill.Single(); row.PropertyProficiencyFlags = 0xf1234567;
            for (int tier = 0; tier < 10; tier++) typeof(CharacterTradeskillModel).GetProperty($"TalentTier{tier:D2}")!.SetValue(row, (uint)(100 + tier)); seed.SaveChanges();
        }
        using var actor = new Actor(f); await actor.Load(); Configure(f, actor);
        using (var context = f.Context()) { actor.Player.Save(context); Assert.Empty(context.ChangeTracker.Entries<CharacterTradeskillModel>()); Assert.Empty(context.ChangeTracker.Entries<CharacterSchematicModel>()); }
        actor.Player.AddTradeskillXp(TradeskillType.Armorer, 20); await Persist(f, actor, false);
        using var read = f.Context(); var stored = read.CharacterTradeskill.Single(); Assert.Equal(0xf1234567u, stored.PropertyProficiencyFlags); Assert.Equal(7u, stored.TalentPoints);
        for (int tier = 0; tier < 10; tier++) Assert.Equal((uint)(100 + tier), typeof(CharacterTradeskillModel).GetProperty($"TalentTier{tier:D2}")!.GetValue(stored));
    }

    private sealed class SuppressedFlush(bool suppress) : SaveChangesInterceptor
    {
        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData data, InterceptionResult<int> result, CancellationToken cancellationToken = default)
            => new(suppress ? InterceptionResult<int>.SuppressWithResult(0) : result);
    }
    private sealed class LostAcknowledgement : DbTransactionInterceptor
    {
        public override Task TransactionCommittedAsync(DbTransaction transaction, TransactionEndEventData data, CancellationToken cancellationToken = default)
            => Task.FromException(new IOException("Owned progression acknowledgement loss."));
    }

    internal static void Seed(Fixture f, bool existing)
    {
        if (!existing) return;
        using var context = f.Context(); context.CharacterTradeskill.Add(new() { Id = 42, TradeskillId = (uint)TradeskillType.Armorer, IsActive = 1, TradeskillXp = 180, TalentPoints = 7, TalentTier00 = 77 });
        context.CharacterSchematic.Add(new() { Id = 42, TradeskillSchematic2Id = 501 }); context.SaveChanges();
    }
    internal static void Configure(Fixture f, Actor actor)
    {
        var tests = typeof(PlayerTradeskillArchiveTests);
        var entry = new TradeskillSchematic2Entry { Id = 501, TradeSkillId = (uint)TradeskillType.Armorer, VectorX = 18, VectorY = 27 };
        var tables = (GameTableManager)tests.GetMethod("CreateGameTableManager", Private)!.Invoke(null, [Array.Empty<TradeskillTierEntry>(), Array.Empty<TradeskillSchematic2Entry>()])!;
        typeof(GameTableManager).GetField("<TradeskillSchematic2>k__BackingField", BindingFlags.NonPublic | BindingFlags.Instance)!.SetValue(tables, PlayerBindPointTests.Table(entry));
        typeof(Player).GetField("gameTableManager", BindingFlags.NonPublic | BindingFlags.Instance)!.SetValue(actor.Player, tables);
        using var context = f.Context();
        foreach (var row in context.CharacterTradeskill.AsNoTracking()) tests.GetMethod("AddTradeskillState", Private)!.Invoke(null, [actor.Player, row]);
        foreach (var row in context.CharacterSchematic.AsNoTracking()) tests.GetMethod("AddSchematicState", Private)!.Invoke(null, [actor.Player, row]);
    }
    private static Task Persist(Fixture f, Actor actor, bool fail)
        => CharacterAtomicSave.Execute(() => { var context = f.Context(); context.SavingChanges += (_, _) => { if (fail) throw new IOException("Owned progression flush fault."); }; return context; }, 42, actor.Player.Save);
}
