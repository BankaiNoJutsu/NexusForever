using System.Data;
using System.Data.Common;
using System.Numerics;
using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Options;
using NexusForever.Database.Character;
using NexusForever.Database.Character.Model;
using NexusForever.Game.Abstract.Map;
using NexusForever.Game.Abstract.Reputation;
using NexusForever.Game.Entity;
using NexusForever.Game.Map;
using NexusForever.Game.Reputation;
using NexusForever.Game.Static.Map;
using NexusForever.Game.Static.Reputation;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.Game.Tests.Transport;
using NexusForever.GameTable;
using NexusForever.GameTable.Configuration.Model;
using NexusForever.GameTable.Model;
using Actor = NexusForever.Game.Tests.Challenges.RewardNormalSaveSnapshotTests.Actor;
using Fixture = NexusForever.Game.Tests.Quest.QuestPersistenceDurabilityTests.Fixture;

namespace NexusForever.Game.Tests.Entity;

public sealed partial class PlayerExplorationPersistenceTests
{
    private readonly DbContextOptions<CharacterContext> options;
    public PlayerExplorationPersistenceTests() { }
    internal PlayerExplorationPersistenceTests(DbContextOptions<CharacterContext> options) => this.options = options;
    private const BindingFlags Instance = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
    private static readonly Vector3 First = new(-32768f + 320f, 0, -32768f + 27.712812f * 10f);
    private static readonly Vector3 Second = new(-32768f + 736f, 0, -32768f + 27.712812f * 30f);

    [Theory]
    [InlineData("exploration")] [InlineData("reputation-new")] [InlineData("reputation-existing")]
    public async Task AcceptedRelocationOrReputationSurvivesRejectedPlayerFlushAndRetry(string action)
    {
        using var f = new Fixture(options); f.Seed();
        using (var seed = f.Context())
        {
            seed.CharacterZonemapHexgroup.Add(new() { Id = 42, ZoneMap = 1, HexGroup = 124 });
            if (action == "reputation-existing") seed.CharacterReputation.Add(new() { Id = 42, FactionId = (uint)Faction.Exile, Amount = 100 });
            seed.SaveChanges();
        }
        using var actor = new Actor(f); await actor.Load(); var (maps, reputation) = Configure(f, actor);
        if (action == "exploration") { maps.OnRelocate(First); Assert.Equal(100, maps.GetMapZoneExploredPercent(1)); }
        else { reputation.UpdateReputation(Faction.Exile, 25); Assert.Equal(action == "reputation-existing" ? 125f : 25f, reputation.GetReputation(Faction.Exile).Amount); }
        await Assert.ThrowsAsync<IOException>(() => Persist(f, actor, true)); await Persist(f, actor);
        using var stored = f.Context();
        if (action == "exploration") { Assert.Equal(new ushort[] { 123, 124 }, stored.CharacterZonemapHexgroup.OrderBy(row => row.HexGroup).Select(row => row.HexGroup)); Assert.Equal(22, Assert.Single(stored.CharacterTitle).Title); }
        else Assert.Equal(action == "reputation-existing" ? 125f : 25f, Assert.Single(stored.CharacterReputation).Amount);
        Assert.Empty(stored.CharacterEntitlement);
    }

    [Theory]
    [InlineData("dispose")] [InlineData("rollback")] [InlineData("suppressed")] [InlineData("lost-ack")]
    public async Task ExplorationReputationAndCompletionTitleRecoverAfterAnUncertainOutcome(string fault)
    {
        using var f = new Fixture(options); f.Seed(); SeedProgress(f); using var actor = new Actor(f); await actor.Load(); var (maps, reputation) = Configure(f, actor);
        maps.OnRelocate(First); reputation.UpdateReputation(Faction.Exile, 25); Assert.Equal(100, maps.GetMapZoneExploredPercent(1));
        if (fault == "lost-ack") await Assert.ThrowsAsync<IOException>(() => CharacterAtomicSave.Execute(() => f.Context(new LostAcknowledgement()), 42, actor.Player.Save));
        else
        {
            await using var context = f.Context(new SuppressedFlush(fault == "suppressed")); await using var transaction = await context.Database.BeginTransactionAsync(IsolationLevel.Serializable);
            actor.Player.Save(context); if (fault != "dispose") await context.SaveChangesAsync(); await transaction.RollbackAsync();
        }
        await Persist(f, actor);
        using (var stored = f.Context()) { Assert.Equal(2, stored.CharacterZonemapHexgroup.Count()); Assert.Equal(125f, Assert.Single(stored.CharacterReputation).Amount); Assert.Equal(22, Assert.Single(stored.CharacterTitle).Title); }
        using var reload = new Actor(f); await reload.Load(); var (replayed, _) = Configure(f, reload); replayed.OnRelocate(First); await Persist(f, reload);
        using var final = f.Context(); Assert.Equal(2, final.CharacterZonemapHexgroup.Count()); Assert.Equal(125f, Assert.Single(final.CharacterReputation).Amount); Assert.Single(final.CharacterTitle);
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task NormalSaveFreezesExplorationReputationAndTitleBeforeAuth(bool existing)
    {
        using var f = new Fixture(options); f.Seed(); if (existing) SeedProgress(f);
        using var actor = new Actor(f); await actor.Load(); var (maps, reputation) = Configure(f, actor);
        actor.OnAuth = () => { maps.OnRelocate(First); maps.OnRelocate(Second); reputation.UpdateReputation(Faction.Exile, 25); };
        await actor.Player.SaveDirect(); await actor.Drain();
        using (var first = f.Context())
        {
            Assert.Equal(existing ? 1 : 0, first.CharacterZonemapHexgroup.Count()); Assert.Equal(existing ? 1 : 0, first.CharacterReputation.Count());
            if (existing) Assert.Equal(100f, first.CharacterReputation.Single().Amount); Assert.Empty(first.CharacterTitle);
        }
        Assert.Equal(100, maps.GetMapZoneExploredPercent(1));
        actor.OnAuth = null; await actor.Player.SaveDirect(); await actor.Drain();
        using var stored = f.Context(); Assert.Equal(2, stored.CharacterZonemapHexgroup.Count()); Assert.Equal(existing ? 125f : 25f, Assert.Single(stored.CharacterReputation).Amount); Assert.Equal(22, Assert.Single(stored.CharacterTitle).Title);
    }

    [Fact]
    public async Task FlushedProgressIsAcknowledgedWhileLaterDiscoveryAndReputationRemainPending()
    {
        using var f = new Fixture(options); f.Seed(); using var actor = new Actor(f); await actor.Load(); var (maps, reputation) = Configure(f, actor);
        maps.OnRelocate(First); reputation.UpdateReputation(Faction.Exile, 25); Assert.Equal(50, maps.GetMapZoneExploredPercent(1));
        await using (var context = f.Context()) await using (var transaction = await context.Database.BeginTransactionAsync(IsolationLevel.Serializable))
        {
            actor.Player.Save(context); await context.SaveChangesAsync(false); context.ChangeTracker.AcceptAllChanges();
            maps.OnRelocate(Second); reputation.UpdateReputation(Faction.Exile, 25); await transaction.CommitAsync();
        }
        using (var first = f.Context()) { Assert.Equal(123, Assert.Single(first.CharacterZonemapHexgroup).HexGroup); Assert.Equal(25f, Assert.Single(first.CharacterReputation).Amount); Assert.Empty(first.CharacterTitle); }
        await Persist(f, actor); using var stored = f.Context(); Assert.Equal(2, stored.CharacterZonemapHexgroup.Count()); Assert.Equal(50f, Assert.Single(stored.CharacterReputation).Amount); Assert.Equal(22, Assert.Single(stored.CharacterTitle).Title);
    }

    [Fact]
    public async Task RecoveryRefusesMixedExplorationAndReputationCandidates()
    {
        using var f = new Fixture(options); f.Seed(); SeedProgress(f); using var actor = new Actor(f); await actor.Load(); var (maps, reputation) = Configure(f, actor);
        maps.OnRelocate(First); reputation.UpdateReputation(Faction.Exile, 25); await Assert.ThrowsAsync<IOException>(() => Persist(f, actor, true));
        using (var foreign = f.Context()) { foreign.CharacterZonemapHexgroup.Add(new() { Id = 42, ZoneMap = 1, HexGroup = 123 }); foreign.SaveChanges(); }
        await Assert.ThrowsAsync<InvalidOperationException>(() => Persist(f, actor)); using var stored = f.Context(); Assert.Equal(2, stored.CharacterZonemapHexgroup.Count()); Assert.Equal(100f, Assert.Single(stored.CharacterReputation).Amount); Assert.Empty(stored.CharacterTitle);
    }

    [Theory]
    [InlineData("map-sql")] [InlineData("map-ef")] [InlineData("reputation-sql")] [InlineData("reputation-ef")]
    public async Task ForeignSqlOrTrackedProgressCannotBeOverwritten(string foreignIntent)
    {
        using var f = new Fixture(options); f.Seed(); SeedProgress(f); using var actor = new Actor(f); await actor.Load(); var (maps, reputation) = Configure(f, actor);
        maps.OnRelocate(First); reputation.UpdateReputation(Faction.Exile, 25);
        using (var foreign = f.Context())
        {
            if (foreignIntent == "map-sql") { foreign.CharacterZonemapHexgroup.Add(new() { Id = 42, ZoneMap = 9, HexGroup = 777 }); foreign.SaveChanges(); }
            if (foreignIntent == "reputation-sql") await foreign.CharacterReputation.ExecuteUpdateAsync(set => set.SetProperty(row => row.Amount, 900f));
        }
        await Assert.ThrowsAsync<InvalidOperationException>(() => CharacterAtomicSave.Execute(() => f.Context(), 42, context =>
        {
            actor.Player.Save(context);
            if (foreignIntent == "map-ef") context.CharacterZonemapHexgroup.Local.Single(row => row.HexGroup == 123).HexGroup = 777;
            if (foreignIntent == "reputation-ef") context.CharacterReputation.Local.Single().Amount = 900;
        }));
        using var stored = f.Context(); Assert.False(stored.CharacterZonemapHexgroup.Any(row => row.HexGroup == 123));
        Assert.Equal(foreignIntent == "map-sql" ? 2 : 1, stored.CharacterZonemapHexgroup.Count()); Assert.Equal(foreignIntent == "reputation-sql" ? 900f : 100f, Assert.Single(stored.CharacterReputation).Amount);
    }

    [Fact]
    public async Task UnavailableMapRowsAndUnchangedFractionalReputationSurviveAnotherGrant()
    {
        using var f = new Fixture(options); f.Seed(); SeedProgress(f);
        using (var seed = f.Context()) { seed.CharacterZonemapHexgroup.Add(new() { Id = 42, ZoneMap = 9, HexGroup = 777 }); seed.CharacterReputation.Add(new() { Id = 42, FactionId = (uint)Faction.Dominion, Amount = 12.5f }); seed.SaveChanges(); }
        using var actor = new Actor(f); await actor.Load(); var (maps, reputation) = Configure(f, actor); maps.OnRelocate(First); reputation.UpdateReputation(Faction.Exile, -25);
        await Persist(f, actor); using var stored = f.Context(); Assert.True(stored.CharacterZonemapHexgroup.Any(row => row.ZoneMap == 9 && row.HexGroup == 777)); Assert.Equal(3, stored.CharacterZonemapHexgroup.Count());
        Assert.Equal(12.5f, stored.CharacterReputation.Single(row => row.FactionId == (uint)Faction.Dominion).Amount); Assert.Equal(75f, stored.CharacterReputation.Single(row => row.FactionId == (uint)Faction.Exile).Amount);
    }

    [Fact]
    public async Task DirectMapAndReputationSaveApisShareAcknowledgedProgress()
    {
        using var f = new Fixture(options); f.Seed(); using var actor = new Actor(f); await actor.Load(); var (maps, reputation) = Configure(f, actor, false);
        maps.OnRelocate(First); reputation.UpdateReputation(Faction.Exile, 25); await Persist(f, actor);
        maps.OnRelocate(Second); reputation.UpdateReputation(Faction.Exile, 25);
        await using (var context = f.Context()) { maps.Save(context); MapMember(maps).Save(context); reputation.Save(context); reputation.GetReputation(Faction.Exile).Save(context); await context.SaveChangesAsync(); }
        await Persist(f, actor); using var stored = f.Context(); Assert.Equal(2, stored.CharacterZonemapHexgroup.Count()); Assert.Equal(50f, Assert.Single(stored.CharacterReputation).Amount);
    }

    [Theory]
    [InlineData("map-manager")] [InlineData("map-member")] [InlineData("reputation-manager")] [InlineData("reputation-member")]
    public async Task ReplacedRealPlayerOwnersCannotUseLegacyWriters(string entryPoint)
    {
        using var f = new Fixture(options); f.Seed(); using var actor = new Actor(f); await actor.Load(); var (maps, reputation) = Configure(f, actor);
        maps.OnRelocate(First); reputation.UpdateReputation(Faction.Exile, 25); await Persist(f, actor); var member = MapMember(maps); var repMember = reputation.GetReputation(Faction.Exile);
        Configure(f, actor); using var context = f.Context();
        Assert.Throws<InvalidOperationException>(() =>
        {
            if (entryPoint == "map-manager") maps.Save(context); else if (entryPoint == "map-member") member.Save(context);
            else if (entryPoint == "reputation-manager") reputation.Save(context); else repMember.Save(context);
        });
        Assert.True(context.IsSaveRejected); using var stored = f.Context(); Assert.Equal(123, Assert.Single(stored.CharacterZonemapHexgroup).HexGroup); Assert.Equal(25f, Assert.Single(stored.CharacterReputation).Amount);
    }

    [Fact]
    public async Task FailedProgressSaveCannotMoveItsAcceptedGraphToAnotherDatabase()
    {
        using var f = new Fixture(options); f.Seed(); using var actor = new Actor(f); await actor.Load(); var (maps, reputation) = Configure(f, actor);
        maps.OnRelocate(First); reputation.UpdateReputation(Faction.Exile, 25); await Assert.ThrowsAsync<IOException>(() => Persist(f, actor, true));
        using var foreign = new Fixture(null); foreign.Seed(); await Assert.ThrowsAsync<InvalidOperationException>(() => Persist(foreign, actor));
        await Persist(f, actor); using var stored = f.Context(); using var other = foreign.Context(); Assert.Single(stored.CharacterZonemapHexgroup); Assert.Equal(25f, Assert.Single(stored.CharacterReputation).Amount); Assert.Empty(other.CharacterZonemapHexgroup); Assert.Empty(other.CharacterReputation);
    }

    private static IZoneMap MapMember(ZoneMapManager manager)
        => ((Dictionary<ushort, IZoneMap>)typeof(ZoneMapManager).GetField("zoneMaps", Instance)!.GetValue(manager)!)[1];
    private static void SeedProgress(Fixture f)
    {
        using var seed = f.Context(); seed.CharacterZonemapHexgroup.Add(new() { Id = 42, ZoneMap = 1, HexGroup = 124 });
        seed.CharacterReputation.Add(new() { Id = 42, FactionId = (uint)Faction.Exile, Amount = 100 }); seed.SaveChanges();
    }

    private static (ZoneMapManager, ReputationManager) Configure(Fixture f, Actor actor, bool rewards = true)
    {
        CharacterModel loaded; using (var read = f.Context()) loaded = read.Character.AsNoTracking().Include(row => row.ZonemapHexgroup).Include(row => row.Reputation).Include(row => row.CharacterTitle).Single();
        var tables = new GameTableManager(Options.Create(new GameTableConfig { GameTablePath = string.Empty }));
        void Table<T>(string name, params T[] entries) where T : class, new()
            => typeof(GameTableManager).GetField("<" + name + ">k__BackingField", Instance)!.SetValue(tables, PlayerBindPointTests.Table(entries));
        Table(nameof(GameTableManager.MapZone), new MapZoneEntry { Id = 1, WorldZoneId = 1, HexLimX = 40, HexLimY = 40 });
        Table(nameof(GameTableManager.MapZoneHexGroup), new MapZoneHexGroupEntry { Id = 123, MapZoneId = 1 }, new MapZoneHexGroupEntry { Id = 124, MapZoneId = 1 });
        Table(nameof(GameTableManager.MapZoneHexGroupEntry), new MapZoneHexGroupEntryEntry { Id = 1, MapZoneHexGroupId = 123, HexX = 7, HexY = 5 }, new MapZoneHexGroupEntryEntry { Id = 2, MapZoneHexGroupId = 124, HexX = 15, HexY = 15 });
        Table(nameof(GameTableManager.WorldZone), new WorldZoneEntry { Id = 1 });
        Table(nameof(GameTableManager.ZoneCompletion), rewards ? new[] { new ZoneCompletionEntry { Id = 1, MapZoneId = 1, ZoneCompletionFactionEnum = (uint)ZoneCompletionFaction.Exile, CharacterTitleIdReward = 22 } } : Array.Empty<ZoneCompletionEntry>());
        Table(nameof(GameTableManager.CharacterTitle), new CharacterTitleEntry { Id = 22 });
        actor.Player.Faction1 = Faction.Exile; typeof(WorldEntity).GetProperty(nameof(WorldEntity.Zone))!.SetValue(actor.Player, new WorldZoneEntry { Id = 1 });
        var nodes = new Dictionary<Faction, IFactionNode>();
        foreach (Faction id in new[] { Faction.Exile, Faction.Dominion })
        { var node = RecordingDispatchProxy<IFactionNode>.Create(out var proxy); proxy.SetProperty(nameof(IFactionNode.FactionId), id); nodes.Add(id, node); }
        var factions = RecordingDispatchProxy<IFactionManager>.Create(out var factionProxy);
        factionProxy.SetMethodHandler(nameof(IFactionManager.GetFaction), args => nodes.GetValueOrDefault((Faction)args[0]));
        var maps = new ZoneMapManager(actor.Player, loaded, tables); var reputation = new ReputationManager(actor.Player, loaded, factions);
        actor.Set(nameof(Player.ZoneMapManager), maps); actor.Set(nameof(Player.ReputationManager), reputation); actor.Set(nameof(Player.TitleManager), new TitleManager(actor.Player, loaded, tables));
        maps.OnZoneUpdate(); return (maps, reputation);
    }
    private static Task Persist(Fixture f, Actor actor, bool fail = false)
        => CharacterAtomicSave.Execute(() => { var context = f.Context(); if (fail) context.SavingChanges += (_, _) => throw new IOException("Owned exploration/reputation flush fault."); return context; }, 42, actor.Player.Save);
    private sealed class SuppressedFlush(bool suppress) : SaveChangesInterceptor
    {
        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData data, InterceptionResult<int> result, CancellationToken cancellationToken = default)
            => new(suppress ? InterceptionResult<int>.SuppressWithResult(0) : result);
    }
    private sealed class LostAcknowledgement : DbTransactionInterceptor
    {
        public override Task TransactionCommittedAsync(DbTransaction transaction, TransactionEndEventData data, CancellationToken cancellationToken = default)
            => Task.FromException(new IOException("Owned exploration/reputation acknowledgement loss."));
    }
}
