using System.Reflection;
using System.Data.Common;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using NexusForever.Game.Guild;
using NexusForever.Game.Abstract.Map.Lock;
using NexusForever.GameTable.Text.Filter;
using NexusForever.Network.Internal;
using System.Runtime.CompilerServices;
using Microsoft.EntityFrameworkCore;
using NexusForever.Database;
using NexusForever.Database.Character;
using NexusForever.Database.Character.Model;
using NexusForever.Database.Configuration.Model;
using NexusForever.Game.Abstract;
using NexusForever.Game.Abstract.Character;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Guild;
using NexusForever.Game.Abstract.Housing;
using NexusForever.Game.Housing;
using NexusForever.Game.Static.Guild;
using NexusForever.Game.Static.Housing;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.GameTable;
using NexusForever.GameTable.Model;

namespace NexusForever.Game.Tests.Housing;

public sealed class ResidenceRecoveryTests : IDisposable
{
    private readonly string file = Path.Combine(Path.GetTempPath(), $"nf-residence-recovery-{Guid.NewGuid():N}.sqlite");
    private DatabaseConnectionString Config => new() { Provider = DatabaseProvider.Sqlite, ConnectionString = $"Data Source={file};Foreign Keys=True" };
    private CharacterContext Context() => new(Config);

    [Fact]
    public async Task PublicCreationPersistsTheConstructionYardAndEveryDefaultPlot()
    {
        Fixture f = await Create();
        using (var save = Context()) { f.Residence.Save(save); await save.SaveChangesAsync(); }
        using var verify = Context();
        Assert.Equal(f.Residence.Id, (await verify.Residence.SingleAsync()).Id);
        ResidencePlotModel[] plots = await verify.ResidencePlot.OrderBy(row => row.Index).ToArrayAsync();
        Assert.Equal(2, plots.Length); Assert.Equal((ushort)531, plots[0].PlugItemId); Assert.Equal((ushort)2, plots[1].PlotInfoId);
    }

    [Fact]
    public async Task FailedInitialPlotInsertRetainsTheCompleteOriginalResidence()
    {
        Fixture f = await Create();
        using (var setup = Context())
            await setup.Database.ExecuteSqlRawAsync("CREATE TRIGGER reject_initial_plot BEFORE INSERT ON residence_plot BEGIN SELECT RAISE(ABORT, 'owned initial plot failure'); END");
        using (var failed = Context())
        {
            f.Residence.Save(failed); await Assert.ThrowsAsync<DbUpdateException>(() => failed.SaveChangesAsync());
        }
        using (var setup = Context()) await setup.Database.ExecuteSqlRawAsync("DROP TRIGGER reject_initial_plot");
        using (var retry = Context()) { f.Residence.Save(retry); await retry.SaveChangesAsync(); }
        using var verify = Context(); Assert.Single(await verify.Residence.ToListAsync()); Assert.Equal(2, await verify.ResidencePlot.CountAsync());
    }

    [Fact]
    public async Task CallerRollbackRetainsCreationAndLaterRootAndPlotEdits()
    {
        Fixture f = await Create();
        using (var staged = Context())
            await WithConfiguredStrategy(staged, async () =>
            {
                await using var transaction = await staged.Database.BeginTransactionAsync();
                f.Residence.Save(staged); await staged.SaveChangesAsync();
                f.Residence.Name = "Later home"; f.Residence.GetPlot(1).SetPlug(700);
                await transaction.RollbackAsync();
            });
        using (var retry = Context()) { f.Residence.Save(retry); await retry.SaveChangesAsync(); }
        using var verify = Context(); Assert.Equal("Later home", (await verify.Residence.SingleAsync()).Name);
        Assert.Equal((ushort)700, (await verify.ResidencePlot.SingleAsync(row => row.Index == 1)).PlugItemId);
        Assert.Equal(2, await verify.ResidencePlot.CountAsync());
    }

    [Theory]
    [InlineData("plot")]
    [InlineData("decor")]
    [InlineData("neighbor")]
    public async Task ChildOnlyPublicChangesReachThePeriodicOwner(string child)
    {
        Fixture f = await Create();
        using (var initial = Context()) { f.Residence.Save(initial); await initial.SaveChangesAsync(); }
        if (child == "plot") f.Residence.GetPlot(1).SetPlug(700);
        else if (child == "decor") f.Residence.DecorCreate(new HousingDecorInfoEntry { Id = 5 });
        else Assert.True(f.Residence.AddNeighbor(43, 2));
        f.Manager.Update(61);
        bool persisted = false;
        for (int i = 0; i < 500; i++)
        {
            using var probe = Context();
            persisted = child == "plot" ? await probe.ResidencePlot.AnyAsync(row => row.Id == f.Residence.Id && row.Index == 1 && row.PlugItemId == 700)
                : child == "decor" ? await probe.ResidenceDecor.AnyAsync(row => row.Id == f.Residence.Id && row.DecorInfoId == 5)
                : await probe.ResidenceNeighbor.AnyAsync(row => row.ResidenceId == f.Residence.Id && row.NeighborCharacterId == 43 && row.PermissionLevel == 2);
            if (persisted) break;
            await Task.Delay(10);
        }
        Assert.True(persisted, $"Accepted {child}-only intent did not reach the periodic residence owner.");
    }

    [Fact]
    public async Task CommunityResidenceCanRetryAfterItsInitiallyMissingGuildParentPersists()
    {
        Fixture f = await Create();
        var community = RecordingDispatchProxy<ICommunity>.Create(out var proxy);
        proxy.SetProperty(nameof(ICommunity.Id), 77ul); proxy.SetProperty(nameof(ICommunity.Name), "Community");
        IResidence residence = f.Manager.CreateCommunity(community);
        using (var failed = Context())
        {
            residence.Save(failed); await Assert.ThrowsAsync<DbUpdateException>(() => failed.SaveChangesAsync());
        }
        using (var parent = Context())
        {
            parent.Guild.Add(new GuildModel { Id = 77, Type = (byte)GuildType.Community, Name = "Community" }); await parent.SaveChangesAsync();
        }
        using (var retry = Context()) { residence.Save(retry); await retry.SaveChangesAsync(); }
        using var verify = Context();
        Assert.Equal(77ul, (await verify.Residence.SingleAsync()).GuildOwnerId);
        Assert.Equal(2, await verify.ResidencePlot.CountAsync());
        Assert.Equal((ushort)573, (await verify.ResidencePlot.SingleAsync(row => row.Index == 0)).PlugItemId);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task BoundChildSavesEnrollTheCompleteOriginalResidence(bool decorSave)
    {
        Fixture f = await Create(); f.Residence.Name = "Complete parent";
        var decor = f.Residence.DecorCreate(new HousingDecorInfoEntry { Id = 5 });
        using (var direct = Context())
        {
            if (decorSave) decor.Save(direct); else f.Residence.GetPlot(1).Save(direct);
            await direct.SaveChangesAsync();
        }
        using var verify = Context(); Assert.Equal("Complete parent", (await verify.Residence.SingleAsync()).Name);
        Assert.Equal(2, await verify.ResidencePlot.CountAsync()); Assert.Single(await verify.ResidenceDecor.ToListAsync());
        Assert.False(decor.PendingCreate); Assert.False(f.Residence.NeedsSave);
    }

    [Fact]
    public async Task LostAcknowledgementReconcilesTheCompleteGraphAndLaterEdits()
    {
        Fixture f = await Create(); f.Residence.DecorCreate(new HousingDecorInfoEntry { Id = 5 }); Assert.True(f.Residence.AddNeighbor(43, 2));
        using (var fault = new FaultContext(Config, new LostAcknowledgement()))
        {
            f.Residence.Save(fault); await Assert.ThrowsAsync<IOException>(() => fault.SaveChangesAsync());
        }
        f.Residence.Name = "Resolved later"; f.Residence.GetPlot(1).SetPlug(700);
        using (var retry = Context()) { f.Residence.Save(retry); await retry.SaveChangesAsync(); }
        using var verify = Context(); Assert.Equal("Resolved later", (await verify.Residence.SingleAsync()).Name);
        Assert.Equal(2, await verify.ResidencePlot.CountAsync()); Assert.Single(await verify.ResidenceDecor.ToListAsync());
        Assert.Single(await verify.ResidenceNeighbor.ToListAsync()); Assert.False(f.Residence.NeedsSave);
    }

    [Fact]
    public async Task ACommittedFrozenUnitPreservesLaterRootPlotAndDecorDeletionIntent()
    {
        Fixture f = await Create(); var decor = f.Residence.DecorCreate(new HousingDecorInfoEntry { Id = 5 });
        using (var staged = Context())
            await WithConfiguredStrategy(staged, async () =>
            {
                await using var transaction = await staged.Database.BeginTransactionAsync();
                f.Residence.Save(staged); await staged.SaveChangesAsync(false); staged.ChangeTracker.AcceptAllChanges();
                f.Residence.Name = "Later"; f.Residence.GetPlot(1).SetPlug(700); decor.EnqueueDelete(true);
                f.Residence.Save(staged); await staged.SaveChangesAsync();
                Assert.Equal("Owner's House", (await staged.Residence.AsNoTracking().SingleAsync()).Name);
                Assert.Single(await staged.ResidenceDecor.AsNoTracking().ToListAsync());
                await transaction.CommitAsync();
            });
        Assert.True(f.Residence.NeedsSave); Assert.True(decor.PendingDelete);
        using (var later = Context()) { f.Residence.Save(later); await later.SaveChangesAsync(); }
        using var verify = Context(); Assert.Equal("Later", (await verify.Residence.SingleAsync()).Name);
        Assert.Empty(await verify.ResidenceDecor.ToListAsync()); Assert.Equal((ushort)700, (await verify.ResidencePlot.SingleAsync(row => row.Index == 1)).PlugItemId);
        Assert.False(f.Residence.NeedsSave);
    }

    [Fact]
    public async Task FailedDecorAndNeighborDeletionsRemainOwnedForRetry()
    {
        Fixture f = await Create(); var decor = f.Residence.DecorCreate(new HousingDecorInfoEntry { Id = 5 }); Assert.True(f.Residence.AddNeighbor(43, 2));
        using (var initial = Context()) { f.Residence.Save(initial); await initial.SaveChangesAsync(); }
        decor.EnqueueDelete(true); Assert.True(f.Residence.RemoveNeighbor(43));
        using (var setup = Context())
            await setup.Database.ExecuteSqlRawAsync("CREATE TRIGGER reject_neighbor_delete BEFORE DELETE ON residence_neighbor BEGIN SELECT RAISE(ABORT, 'owned neighbor delete failure'); END");
        using (var failed = Context())
        {
            f.Residence.Save(failed); await Assert.ThrowsAsync<DbUpdateException>(() => failed.SaveChangesAsync());
        }
        using (var setup = Context()) { Assert.Single(await setup.ResidenceDecor.ToListAsync()); await setup.Database.ExecuteSqlRawAsync("DROP TRIGGER reject_neighbor_delete"); }
        using (var retry = Context()) { f.Residence.Save(retry); await retry.SaveChangesAsync(); }
        using var verify = Context(); Assert.Empty(await verify.ResidenceDecor.ToListAsync()); Assert.Empty(await verify.ResidenceNeighbor.ToListAsync());
        Assert.Null(f.Residence.GetDecor(decor.DecorId)); Assert.False(f.Residence.NeedsSave);
    }

    [Fact]
    public async Task LateForeignSqlOrTrackedRowsRejectTheWholeResidenceUnit()
    {
        Fixture f = await Create();
        using (var staged = Context())
            await WithConfiguredStrategy(staged, async () =>
            {
                await using var transaction = await staged.Database.BeginTransactionAsync();
                f.Residence.Save(staged); await staged.SaveChangesAsync();
                await staged.Database.ExecuteSqlRawAsync("UPDATE residence_plot SET buildState=99 WHERE `index`=1");
                await Assert.ThrowsAsync<InvalidOperationException>(() => transaction.CommitAsync());
            });
        using (var staged = Context())
        {
            staged.ResidenceNeighbor.Add(new ResidenceNeighborModel { ResidenceId = f.Residence.Id, NeighborCharacterId = 43, PermissionLevel = 1 });
            f.Residence.Save(staged); await Assert.ThrowsAsync<InvalidOperationException>(() => staged.SaveChangesAsync());
        }
        using (var retry = Context()) { f.Residence.Save(retry); await retry.SaveChangesAsync(); }
        using var verify = Context(); Assert.Equal(2, await verify.ResidencePlot.CountAsync()); Assert.Empty(await verify.ResidenceNeighbor.ToListAsync());
    }

    [Fact]
    public async Task UnflushedCommitCannotConsumeTheRegisteredCreation()
    {
        Fixture f = await Create();
        using (var staged = Context())
            await WithConfiguredStrategy(staged, async () =>
            {
                await using var transaction = await staged.Database.BeginTransactionAsync();
                f.Residence.Save(staged); await Assert.ThrowsAsync<InvalidOperationException>(() => transaction.CommitAsync());
            });
        using (var retry = Context()) { f.Residence.Save(retry); await retry.SaveChangesAsync(); }
        using var verify = Context(); Assert.Single(await verify.Residence.ToListAsync()); Assert.Equal(2, await verify.ResidencePlot.CountAsync());
    }

    [Fact]
    public async Task BoundChildRejectsAForeignRouteAndAnOriginalRetiredResidence()
    {
        Fixture f = await Create();
        string foreignFile = file + ".foreign";
        try
        {
            using var foreign = new CharacterContext(new DatabaseConnectionString { Provider = DatabaseProvider.Sqlite, ConnectionString = $"Data Source={foreignFile};Foreign Keys=True" });
            await foreign.Database.EnsureCreatedAsync();
            Assert.Throws<InvalidOperationException>(() => f.Residence.GetPlot(1).Save(foreign)); Assert.True(foreign.IsSaveRejected);
        }
        finally
        {
            using var connection = new Microsoft.Data.Sqlite.SqliteConnection($"Data Source={foreignFile};Foreign Keys=True"); Microsoft.Data.Sqlite.SqliteConnection.ClearPool(connection);
            if (File.Exists(foreignFile)) File.Delete(foreignFile);
        }
        f.Manager.RemoveResidence("Owner");
        Assert.Throws<InvalidOperationException>(() => f.Residence.Name = "Retired");
        Assert.Throws<InvalidOperationException>(() => f.Residence.GetPlot(1).SetPlug(700));
        using var retired = Context(); Assert.Throws<InvalidOperationException>(() => f.Residence.Save(retired)); Assert.True(retired.IsSaveRejected);
    }

    [Fact]
    public async Task RealCommunityRegistrationSavesItsRootAndDefaultPlotsWithTheGuild()
    {
        Fixture f = await Create();
        using CommunityFixture registered = CreateRegisteredCommunity(f);
        GlobalGuildManager guilds = registered.Manager; Community community = registered.Community;
        f.Manager.Update(61); await WaitForResidenceTimer(f.Manager);
        using (var before = Context()) Assert.False(await before.Residence.AnyAsync(row => row.GuildOwnerId == community.Id));
        using (var setup = Context())
            await setup.Database.ExecuteSqlRawAsync("CREATE TRIGGER reject_community_plot BEFORE INSERT ON residence_plot WHEN NEW.id IN (SELECT id FROM residence WHERE guildOwnerId IS NOT NULL) BEGIN SELECT RAISE(ABORT, 'owned community create failure'); END");
        await FlushGuilds(guilds); Assert.True(community.PendingCreate);
        using (var setup = Context())
        {
            Assert.False(await setup.Guild.AnyAsync(row => row.Id == community.Id)); await setup.Database.ExecuteSqlRawAsync("DROP TRIGGER reject_community_plot");
        }
        await FlushGuilds(guilds);
        using var verify = Context(); Assert.Equal("Real community", (await verify.Guild.SingleAsync()).Name);
        Assert.Equal(community.Residence.Id, (await verify.Residence.SingleAsync(row => row.GuildOwnerId == community.Id)).Id);
        Assert.Equal(2, await verify.ResidencePlot.CountAsync(row => row.Id == community.Residence.Id)); Assert.False(community.NeedsSave);
    }

    [Fact]
    public async Task LoadedRawFieldsAndUnresolvedPlotIdentifiersSurviveASiblingSave()
    {
        Fixture f = await Create();
        using (var initial = Context())
        {
            f.Residence.Save(initial); await initial.SaveChangesAsync();
            await initial.Database.ExecuteSqlRawAsync("UPDATE residence_plot SET plotInfoId=65000,plugItemId=64000 WHERE `index`=1");
            await initial.Database.ExecuteSqlRawAsync("UPDATE residence SET flags=65535,wallpaperId=65000");
        }
        var database = new CharacterDatabase(); database.Initialise(Config);
        var databases = RecordingDispatchProxy<IDatabaseManager>.Create(out var db); db.SetMethodReturn(nameof(IDatabaseManager.GetDatabase), database);
        var characters = RecordingDispatchProxy<ICharacterManager>.Create(out var manager);
        var character = RecordingDispatchProxy<ICharacter>.Create(out var profile); profile.SetProperty(nameof(ICharacter.Name), "Owner");
        manager.SetMethodReturn(nameof(ICharacterManager.GetCharacter), character);
        var reloaded = new GlobalResidenceManager(characterManager: characters, databaseManager: databases, gameTableManager: f.Tables); reloaded.Initialise();
        var home = Assert.IsType<Residence>(reloaded.GetResidence(f.Residence.Id)); home.Name = "Sibling only";
        using (var sibling = Context()) { home.Save(sibling); await sibling.SaveChangesAsync(); }
        using var verify = Context(); ResidenceModel root = await verify.Residence.SingleAsync();
        Assert.Equal("Sibling only", root.Name); Assert.Equal(ushort.MaxValue, root.Flags); Assert.Equal((ushort)65000, root.WallpaperId);
        ResidencePlotModel plot = await verify.ResidencePlot.SingleAsync(row => row.Index == 1);
        Assert.Equal((ushort)65000, plot.PlotInfoId); Assert.Equal((ushort)64000, plot.PlugItemId); Assert.False(home.NeedsSave);
    }

    [Fact]
    public async Task RegisteredCommunityDisbandRetainsOnlyItsOwnedRetirementSave()
    {
        Fixture f = await Create(); using CommunityFixture registered = CreateRegisteredCommunity(f);
        await FlushGuilds(registered.Manager); Community community = registered.Community;
        ulong root = community.Residence.Id; community.DisbandGuild();
        Assert.Null(f.Manager.GetResidence(root));
        Assert.Throws<InvalidOperationException>(() => community.Residence.Name = "Retired edit");
        string acceptedName = community.Name;
        Assert.Throws<InvalidOperationException>(() => community.RenameGuild("Retired rename"));
        Assert.Equal(acceptedName, community.Name);
        using (var setup = Context())
            await setup.Database.ExecuteSqlRawAsync("CREATE TRIGGER reject_retirement BEFORE UPDATE ON residence BEGIN SELECT RAISE(ABORT, 'owned retirement failure'); END");
        await FlushGuilds(registered.Manager);
        using (var failed = Context())
        {
            Assert.Null((await failed.Guild.SingleAsync()).DeleteTime);
            Assert.Equal(community.Id, (await failed.Residence.SingleAsync()).GuildOwnerId);
            await failed.Database.ExecuteSqlRawAsync("DROP TRIGGER reject_retirement");
        }
        await FlushGuilds(registered.Manager);
        using var verify = Context(); Assert.NotNull((await verify.Guild.SingleAsync()).DeleteTime);
        Assert.Null((await verify.Residence.SingleAsync()).GuildOwnerId); Assert.Equal(2, await verify.ResidencePlot.CountAsync());
        Assert.Null(registered.Manager.GetGuild(community.Id));
        using var retired = Context(); Assert.Throws<InvalidOperationException>(() => community.Residence.Save(retired));
    }

    [Fact]
    public async Task LoadedResidenceRetainsItsQueryRouteWhenConfigurationChangesDuringRegistration()
    {
        Fixture f = await Create(); using (var initial = Context()) { f.Residence.Save(initial); await initial.SaveChangesAsync(); }
        string foreignFile = file + ".foreign";
        var otherConfig = new DatabaseConnectionString { Provider = DatabaseProvider.Sqlite, ConnectionString = $"Data Source={foreignFile};Foreign Keys=True" };
        try
        {
            var original = new CharacterDatabase(); original.Initialise(Config);
            var other = new CharacterDatabase(); other.Initialise(otherConfig);
            using (var seed = new CharacterContext(otherConfig))
            {
                await seed.Database.EnsureCreatedAsync(); seed.Character.Add(new CharacterModel { Id = 42, Name = "Owner" });
                ResidenceModel model = Assert.Single(original.GetResidences()); model.Character = null; model.Guild = null;
                seed.Residence.Add(model); await seed.SaveChangesAsync();
            }
            CharacterDatabase selected = original;
            var databases = RecordingDispatchProxy<IDatabaseManager>.Create(out var db); db.SetMethodReturnFactory(nameof(IDatabaseManager.GetDatabase), () => selected);
            var characters = RecordingDispatchProxy<ICharacterManager>.Create(out var players);
            var character = RecordingDispatchProxy<ICharacter>.Create(out var profile); profile.SetProperty(nameof(ICharacter.Name), "Owner");
            players.SetMethodHandler(nameof(ICharacterManager.GetCharacter), _ => { selected = other; return character; });
            var manager = new GlobalResidenceManager(characterManager: characters, databaseManager: databases, gameTableManager: f.Tables); manager.Initialise();
            var loaded = Assert.IsType<Residence>(manager.GetResidence(f.Residence.Id)); loaded.Name = "Original route only";
            using (var foreign = new CharacterContext(otherConfig))
            {
                Assert.Throws<InvalidOperationException>(() => loaded.Save(foreign)); Assert.True(foreign.IsSaveRejected);
                Assert.Equal("Owner's House", (await foreign.Residence.AsNoTracking().SingleAsync()).Name);
            }
            selected = original; manager.Update(61); await WaitForResidenceTimer(manager);
            using var verify = Context(); Assert.Equal("Original route only", (await verify.Residence.SingleAsync()).Name);
        }
        finally
        {
            using var connection = new Microsoft.Data.Sqlite.SqliteConnection(otherConfig.ConnectionString); Microsoft.Data.Sqlite.SqliteConnection.ClearPool(connection);
            if (File.Exists(foreignFile)) File.Delete(foreignFile);
        }
    }

    [Fact]
    public async Task RegistryDoesNotExposeAResidenceBeforeItsPersistenceBinding()
    {
        Fixture f = await Create(); var database = new CharacterDatabase(); database.Initialise(Config);
        GlobalResidenceManager manager = null; bool inspect = false, observed = false;
        var databases = RecordingDispatchProxy<IDatabaseManager>.Create(out var db);
        db.SetMethodHandler(nameof(IDatabaseManager.GetDatabase), _ =>
        {
            if (inspect) { observed = true; Assert.Null(manager.GetResidence(1)); }
            return database;
        });
        manager = new GlobalResidenceManager(databaseManager: databases, gameTableManager: f.Tables); manager.Initialise();
        var player = RecordingDispatchProxy<IPlayer>.Create(out var owner); owner.SetProperty(nameof(IPlayer.Level), 50u);
        owner.SetProperty(nameof(IPlayer.CharacterId), 42ul); owner.SetProperty(nameof(IPlayer.Name), "Owner");
        owner.SetProperty(nameof(IPlayer.Identity), new NexusForever.Game.Abstract.Identity { Id = 42, RealmId = 1 });
        inspect = true; IResidence residence = manager.CreateResidence(player); inspect = false;
        Assert.True(observed); Assert.Same(residence, manager.GetResidence(1));
        using var save = Context(); residence.Save(save); await save.SaveChangesAsync();
    }

    private CommunityFixture CreateRegisteredCommunity(Fixture f)
    {
        var realm = RecordingDispatchProxy<IRealmContext>.Create(out var realmProxy); realmProxy.SetProperty(nameof(IRealmContext.RealmId), (ushort)1);
        var publisher = RecordingDispatchProxy<IInternalMessagePublisher>.Create(out var messages); messages.SetMethodReturn(nameof(IInternalMessagePublisher.PublishAsync), Task.CompletedTask);
        var filter = RecordingDispatchProxy<ITextFilterManager>.Create(out var text); text.SetMethodReturn(nameof(ITextFilterManager.IsTextValid), true);
        var databases = RecordingDispatchProxy<IDatabaseManager>.Create(out var db); var database = new CharacterDatabase(); database.Initialise(Config); db.SetMethodReturn(nameof(IDatabaseManager.GetDatabase), database);
        var services = new ServiceCollection(); services.AddSingleton<IGuildFactory, GuildFactory>();
        services.AddSingleton<IGlobalGuildManager>(provider => new GlobalGuildManager(provider.GetRequiredService<IGuildFactory>(), databases));
        services.AddKeyedTransient<IGuildBase>(GuildType.Community, (provider, key) => new Community(realm, publisher, f.Manager, filter,
            RecordingDispatchProxy<IMapLockManager>.Create(out _), globalGuildManager: provider.GetRequiredService<IGlobalGuildManager>()));
        var container = services.BuildServiceProvider(); var guilds = (GlobalGuildManager)container.GetRequiredService<IGlobalGuildManager>(); guilds.Initialise();
        var community = Assert.IsType<Community>(guilds.RegisterGuild(GuildType.Community, "Real community", "Leader", "Council", "Member"));
        return new(container, guilds, community);
    }
    private sealed record CommunityFixture(ServiceProvider Container, GlobalGuildManager Manager, Community Community) : IDisposable
    { public void Dispose() => Container.Dispose(); }

    private static async Task WaitForResidenceTimer(GlobalResidenceManager manager)
    {
        object gate = typeof(GlobalResidenceManager).GetField("saveGate", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(manager);
        var pending = (HashSet<ulong>)typeof(GlobalResidenceManager).GetField("savesInFlight", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(manager);
        for (int i = 0; i < 500; i++) { lock (gate) if (pending.Count == 0) return; await Task.Delay(10); }
        Assert.Fail("Residence timer did not settle.");
    }
    private static async Task FlushGuilds(GlobalGuildManager manager)
    {
        manager.Update(61);
        object gate = typeof(GlobalGuildManager).GetField("cacheLock", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(manager);
        var pending = (HashSet<ulong>)typeof(GlobalGuildManager).GetField("guildSavesInFlight", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(manager);
        for (int i = 0; i < 500; i++) { lock (gate) if (pending.Count == 0) return; await Task.Delay(10); }
        Assert.Fail("Guild timer did not settle.");
    }
    private sealed class FaultContext(DatabaseConnectionString config, IInterceptor interceptor) : CharacterContext(config)
    {
        protected override void OnConfiguring(DbContextOptionsBuilder builder) { base.OnConfiguring(builder); builder.AddInterceptors(interceptor); }
    }
    private sealed class LostAcknowledgement : DbTransactionInterceptor
    {
        public override Task TransactionCommittedAsync(DbTransaction transaction, TransactionEndEventData data, CancellationToken cancellationToken = default)
            => Task.FromException(new IOException("owned residence acknowledgement loss"));
    }

    private static Task WithConfiguredStrategy(CharacterContext context, Func<Task> action)
    {
        bool entered = false;
        return context.Database.CreateExecutionStrategy().ExecuteAsync(async () =>
        {
            if (entered) throw new InvalidOperationException("The residence fixture cannot replay accepted gameplay.");
            entered = true; await action();
        });
    }

    private async Task<Fixture> Create()
    {
        using (var seed = Context())
        {
            await seed.Database.EnsureCreatedAsync();
            seed.Character.AddRange(new CharacterModel { Id = 42, Name = "Owner" }, new CharacterModel { Id = 43, Name = "Neighbor" });
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
        table.SetProperty(nameof(IGameTableManager.HousingPlugItem), Table(new HousingPlugItemEntry { Id = 531 }, new HousingPlugItemEntry { Id = 573 }, new HousingPlugItemEntry { Id = 700 }));
        table.SetProperty(nameof(IGameTableManager.HousingDecorInfo), Table(new HousingDecorInfoEntry { Id = 5 }));
        var player = RecordingDispatchProxy<IPlayer>.Create(out var owner);
        owner.SetProperty(nameof(IPlayer.Level), 50u); owner.SetProperty(nameof(IPlayer.CharacterId), 42ul); owner.SetProperty(nameof(IPlayer.Name), "Owner");
        owner.SetProperty(nameof(IPlayer.Identity), new NexusForever.Game.Abstract.Identity { Id = 42, RealmId = 1 });
        var manager = new GlobalResidenceManager(databaseManager: databases, gameTableManager: tables); manager.Initialise();
        return new(manager, Assert.IsType<Residence>(manager.CreateResidence(player)), tables);
    }

    private static GameTable<T> Table<T>(params T[] entries) where T : class, new()
    {
        var table = (GameTable<T>)RuntimeHelpers.GetUninitializedObject(typeof(GameTable<T>));
        uint Id(T entry) => (uint)typeof(T).GetField("Id")!.GetValue(entry);
        uint max = entries.Max(Id) + 1; int[] lookup = Enumerable.Repeat(-1, (int)max).ToArray();
        for (int i = 0; i < entries.Length; i++) lookup[Id(entries[i])] = i;
        typeof(GameTable<T>).GetField("<Entries>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(table, entries);
        typeof(GameTable<T>).GetField("header", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(table, new GameTableHeader { MaxId = max });
        typeof(GameTable<T>).GetField("lookup", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(table, lookup);
        return table;
    }
    private sealed record Fixture(GlobalResidenceManager Manager, Residence Residence, IGameTableManager Tables);
    public void Dispose()
    {
        using var pool = new Microsoft.Data.Sqlite.SqliteConnection(Config.ConnectionString); Microsoft.Data.Sqlite.SqliteConnection.ClearPool(pool);
        if (File.Exists(file)) File.Delete(file);
    }
}