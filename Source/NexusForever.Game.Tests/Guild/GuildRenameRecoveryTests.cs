using System.Reflection;
using System.Runtime.CompilerServices;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NexusForever.Database;
using NexusForever.Database.Character;
using NexusForever.Database.Character.Model;
using NexusForever.Database.Configuration.Model;
using NexusForever.Game;
using NexusForever.Game.Abstract;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Guild;
using NexusForever.Game.Abstract.Housing;
using NexusForever.Game.Abstract.Map.Instance;
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
using NexusForever.Network.Session;
using NexusForever.Network.World.Message.Model;
using NexusForever.Network.World.Message.Static;
using NexusForever.WorldServer.Network.Message.Handler.Housing;
using NexusForever.WorldServer.Network;

namespace NexusForever.Game.Tests.Guild;

public sealed class GuildRenameRecoveryTests : IDisposable
{
    private readonly string file = Path.Combine(Path.GetTempPath(), $"nf-guild-rename-{Guid.NewGuid():N}.sqlite");
    private DatabaseConnectionString Config => new() { Provider = DatabaseProvider.Sqlite, ConnectionString = $"Data Source={file};Foreign Keys=True" };
    private CharacterContext Context() => new(Config);

    [Fact]
    public async Task PublicCommunityRenameReservesAcceptedNameBeforeAnotherRegistration()
    {
        using Fixture f = await Create();
        f.Handler.HandleMessage(f.Session, Request("Renamed"));
        Assert.Equal(HousingResult.Success, Result(f));
        Assert.Equal("Renamed", f.Community.Name);
        Assert.Same(f.Community, f.Manager.GetGuild(GuildType.Community, "renamed"));
        Assert.Same(f.Community, f.Manager.GetGuild(GuildType.Community, "Original")); // SQL still reserves this name.
        Assert.Single(f.Currency.GetInvocations(nameof(ICurrencyManager.CurrencySubtractAmount)));
    }

    [Fact]
    public async Task PublicCommunityRenameRejectsAnotherGuildsNameBeforeCharging()
    {
        using Fixture f = await Create();
        f.Handler.HandleMessage(f.Session, Request("taken"));
        Assert.Empty(f.Currency.GetInvocations(nameof(ICurrencyManager.CurrencySubtractAmount)));
        Assert.Equal(HousingResult.Failed, Result(f));
        Assert.Equal("Original", f.Community.Name);
        Assert.Equal("Original", f.Root.Name);
        Assert.Equal(88ul, f.Manager.GetGuild(GuildType.Community, "Taken").Id);
    }

    [Fact]
    public async Task RejectedPeriodicRenameRetainsIntentAndOldNameUntilConfirmedRetry()
    {
        using Fixture f = await Create();
        f.Community.RenameGuild("Accepted");
        using (var setup = Context())
            await setup.Database.ExecuteSqlRawAsync("CREATE TRIGGER reject_rename BEFORE UPDATE ON guild BEGIN SELECT RAISE(ABORT, 'owned rename failure'); END");
        await Flush(f.Manager);
        using (var failed = Context())
        {
            Assert.Equal("Original", (await failed.Guild.SingleAsync(row => row.Id == 77)).Name);
            await failed.Database.ExecuteSqlRawAsync("DROP TRIGGER reject_rename");
        }
        Assert.True(f.Community.NeedsSave);
        Assert.Same(f.Community, f.Manager.GetGuild(GuildType.Community, "Original"));
        await Flush(f.Manager);
        using var verify = Context();
        Assert.Equal("Accepted", (await verify.Guild.SingleAsync(row => row.Id == 77)).Name);
        Assert.Equal("Accepted", (await verify.Residence.SingleAsync(row => row.Id == 100)).Name);
        Assert.Same(f.Community, f.Manager.GetGuild(GuildType.Community, "Accepted"));
        Assert.Null(f.Manager.GetGuild(GuildType.Community, "Original"));
    }

    [Fact]
    public async Task PublicRenameUpdatesCommunityResidenceEvenWhenItsMapIsUnloaded()
    {
        using Fixture f = await Create();
        Assert.Null(f.Root.Map);
        f.Handler.HandleMessage(f.Session, Request("Unloaded"));
        Assert.Equal(HousingResult.Success, Result(f));
        Assert.Equal("Unloaded", f.Root.Name);
    }

    [Fact]
    public async Task RejectedPeriodicRenameCanBeRetriedByDirectCommunitySave()
    {
        using Fixture f = await Create();
        f.Community.RenameGuild("Accepted");
        using (var setup = Context())
            await setup.Database.ExecuteSqlRawAsync("CREATE TRIGGER reject_rename BEFORE UPDATE ON guild BEGIN SELECT RAISE(ABORT, 'owned rename failure'); END");
        await Flush(f.Manager);
        using (var setup = Context()) await setup.Database.ExecuteSqlRawAsync("DROP TRIGGER reject_rename");
        using var retry = Context();
        f.Community.Save(retry);
        await retry.SaveChangesAsync();
        using var verify = Context();
        Assert.Equal("Accepted", (await verify.Guild.SingleAsync(row => row.Id == 77)).Name);
        Assert.Equal("Accepted", (await verify.Residence.SingleAsync(row => row.Id == 100)).Name);
    }

    [Fact]
    public async Task HeldReservationRejectsReentryAndDisposalReleasesUnusedName()
    {
        using Fixture f = await Create();
        using (IDisposable reservation = f.Manager.TryReserveGuildName(f.Community, "Held"))
        {
            Assert.NotNull(reservation);
            Assert.Same(f.Community, f.Manager.GetGuild(GuildType.Community, "Held"));
            f.Handler.HandleMessage(f.Session, Request("Other"));
            Assert.Empty(f.Currency.GetInvocations(nameof(ICurrencyManager.CurrencySubtractAmount)));
            Assert.Equal(HousingResult.Failed, Result(f));
            Assert.Throws<InvalidOperationException>(() => f.Community.Name = "Unreserved");
            Assert.Equal("Original", f.Community.Name);
        }
        Assert.Null(f.Manager.GetGuild(GuildType.Community, "Held"));
        Assert.Null(f.Manager.GetGuild(GuildType.Community, "Unreserved"));
    }

    [Fact]
    public async Task FailedPaymentReleasesReservationWithoutAcceptingEitherName()
    {
        using Fixture f = await Create();
        f.Currency.SetMethodHandler(nameof(ICurrencyManager.CurrencySubtractAmount), _ => throw new InvalidOperationException("owned debit refusal"));
        Assert.Throws<InvalidOperationException>(() => f.Handler.HandleMessage(f.Session, Request("Refused")));
        Assert.Equal("Original", f.Community.Name);
        Assert.Equal("Original", f.Root.Name);
        Assert.Null(f.Manager.GetGuild(GuildType.Community, "Refused"));
        using IDisposable next = f.Manager.TryReserveGuildName(f.Community, "Next");
        Assert.NotNull(next);
    }

    [Fact]
    public async Task LaterAcceptedRenameSurvivesEarlierSaveAndKeepsItsOwnReservation()
    {
        using Fixture f = await Create();
        f.Handler.HandleMessage(f.Session, Request("First"));
        using (var staged = Context())
        {
            await WithConfiguredStrategy(staged, async () =>
            {
                await using var transaction = await staged.Database.BeginTransactionAsync();
                f.Community.Save(staged);
                f.Handler.HandleMessage(f.Session, Request("Later"));
                f.Community.Save(staged);
                await staged.SaveChangesAsync();
                await transaction.CommitAsync();
            });
        }
        using (var earlier = Context())
        {
            Assert.Equal("First", (await earlier.Guild.SingleAsync(row => row.Id == 77)).Name);
            Assert.Equal("First", (await earlier.Residence.SingleAsync(row => row.Id == 100)).Name);
        }
        Assert.True(f.Community.NeedsSave);
        Assert.Same(f.Community, f.Manager.GetGuild(GuildType.Community, "First"));
        Assert.Same(f.Community, f.Manager.GetGuild(GuildType.Community, "Later"));
        Assert.Null(f.Manager.GetGuild(GuildType.Community, "Original"));
        await Flush(f.Manager);
        using var verify = Context();
        Assert.Equal("Later", (await verify.Guild.SingleAsync(row => row.Id == 77)).Name);
        Assert.Equal("Later", (await verify.Residence.SingleAsync(row => row.Id == 100)).Name);
        Assert.Null(f.Manager.GetGuild(GuildType.Community, "First"));
        Assert.False(f.Community.NeedsSave);
    }

    [Fact]
    public async Task UnflushedCommitCannotReleaseThePersistedName()
    {
        using Fixture f = await Create();
        f.Handler.HandleMessage(f.Session, Request("Pending"));
        using (var staged = Context())
        {
            await WithConfiguredStrategy(staged, async () =>
            {
                await using var transaction = await staged.Database.BeginTransactionAsync();
                f.Community.Save(staged);
                await Assert.ThrowsAsync<InvalidOperationException>(() => transaction.CommitAsync());
            });
        }
        Assert.Same(f.Community, f.Manager.GetGuild(GuildType.Community, "Original"));
        Assert.Same(f.Community, f.Manager.GetGuild(GuildType.Community, "Pending"));
        await Flush(f.Manager);
        using var verify = Context();
        Assert.Equal("Pending", (await verify.Guild.SingleAsync(row => row.Id == 77)).Name);
        Assert.Equal("Pending", (await verify.Residence.SingleAsync(row => row.Id == 100)).Name);
        Assert.Null(f.Manager.GetGuild(GuildType.Community, "Original"));
    }

    [Fact]
    public async Task LoadedGuildNameRejectsAnotherDatabaseBeforeStaging()
    {
        using Fixture f = await Create();
        f.Handler.HandleMessage(f.Session, Request("Owned"));
        using var foreign = new CharacterContext(new DatabaseConnectionString { Provider = DatabaseProvider.Sqlite, ConnectionString = "Data Source=:memory:" });
        Assert.Throws<InvalidOperationException>(() => f.Community.Save(foreign));
        Assert.True(foreign.IsSaveRejected);
        Assert.Empty(foreign.ChangeTracker.Entries());
        await Flush(f.Manager);
        using var verify = Context();
        Assert.Equal("Owned", (await verify.Guild.SingleAsync(row => row.Id == 77)).Name);
    }

    [Fact]
    public async Task FirstConfirmedCreateReleasesItsOldAliasAndEnablesLaterRenameRecovery()
    {
        using Fixture f = await Create();
        var circle = Assert.IsType<Circle>(f.Manager.RegisterGuild(GuildType.Circle, "Draft", "Leader", "Council", "Member"));
        circle.RenameGuild("Created");
        Assert.Same(circle, f.Manager.GetGuild(GuildType.Circle, "Draft"));
        await Flush(f.Manager);
        Assert.Null(f.Manager.GetGuild(GuildType.Circle, "Draft"));
        Assert.Same(circle, f.Manager.GetGuild(GuildType.Circle, "Created"));
        circle.RenameGuild("Updated");
        await Flush(f.Manager);
        using var verify = Context();
        Assert.Equal("Updated", (await verify.Guild.SingleAsync(row => row.Id == circle.Id)).Name);
        Assert.Equal(3, await verify.GuildRank.CountAsync(row => row.Id == circle.Id));
        Assert.Null(f.Manager.GetGuild(GuildType.Circle, "Created"));
        Assert.Same(circle, f.Manager.GetGuild(GuildType.Circle, "Updated"));
    }

    [Fact]
    public async Task NewCommunityCanRenameBeforeItsFirstGuildAndResidenceSave()
    {
        using Fixture f = await Create();
        var community = Assert.IsType<Community>(f.Manager.RegisterGuild(GuildType.Community, "Draft community", "Leader", "Council", "Member"));
        community.RenameGuild("Created community");
        using (var created = Context())
        {
            community.Save(created);
            await created.SaveChangesAsync();
        }
        using var verify = Context();
        Assert.Equal("Created community", (await verify.Guild.SingleAsync(row => row.Id == community.Id)).Name);
        Assert.Equal("Created community", (await verify.Residence.SingleAsync(row => row.Id == community.Residence.Id)).Name);
        Assert.Null(f.Manager.GetGuild(GuildType.Community, "Draft community"));
    }

    [Fact]
    public async Task SameNameResidenceIntentCannotWriteAnotherDatabase()
    {
        using Fixture f = await Create();
        f.Community.RenameGuild("Original");
        var config = new DatabaseConnectionString { Provider = DatabaseProvider.Sqlite, ConnectionString = $"Data Source={file}.foreign;Foreign Keys=True" };
        try
        {
            using var foreign = new CharacterContext(config);
            await foreign.Database.EnsureCreatedAsync();
            foreign.Guild.Add(new GuildModel { Id = 77, Type = (byte)GuildType.Community, Name = "Original" });
            foreign.Residence.Add(new ResidenceModel { Id = 100, GuildOwnerId = 77, Name = "Foreign", PropertyInfoId = (byte)PropertyInfoId.Community });
            await foreign.SaveChangesAsync();
            foreign.ChangeTracker.Clear();
            Exception refusal = Record.Exception(() => f.Community.Save(foreign));
            if (refusal == null) await foreign.SaveChangesAsync();
            Assert.IsType<InvalidOperationException>(refusal);
            Assert.True(foreign.IsSaveRejected);
            using var readOnly = new CharacterContext(config);
            Assert.Equal("Foreign", (await readOnly.Residence.SingleAsync()).Name);
        }
        finally
        {
            using var pool = new Microsoft.Data.Sqlite.SqliteConnection(config.ConnectionString);
            Microsoft.Data.Sqlite.SqliteConnection.ClearPool(pool);
            if (File.Exists(file + ".foreign")) File.Delete(file + ".foreign");
        }
    }

    [Fact]
    public async Task FailedResidenceEnrollmentRejectsTheWholeRenameUnit()
    {
        using Fixture f = await Create();
        f.Community.RenameGuild("Coupled");
        using (var staged = Context())
        {
            await staged.Residence.SingleAsync(row => row.Id == 100);
            Exception enrollment = Record.Exception(() => f.Community.Save(staged));
            Assert.IsType<InvalidOperationException>(enrollment);
            Exception rejected = await Record.ExceptionAsync(() => staged.SaveChangesAsync());
            Assert.IsType<InvalidOperationException>(rejected);
            Assert.True(staged.IsSaveRejected);
        }
        using (var unchanged = Context())
        {
            Assert.Equal("Original", (await unchanged.Guild.SingleAsync(row => row.Id == 77)).Name);
            Assert.Equal("Original", (await unchanged.Residence.SingleAsync(row => row.Id == 100)).Name);
        }
        await Flush(f.Manager);
        using var verify = Context();
        Assert.Equal("Coupled", (await verify.Guild.SingleAsync(row => row.Id == 77)).Name);
        Assert.Equal("Coupled", (await verify.Residence.SingleAsync(row => row.Id == 100)).Name);
    }

    private async Task<Fixture> Create()
    {
        using (var seed = Context())
        {
            await seed.Database.EnsureCreatedAsync();
            seed.Character.AddRange(new CharacterModel { Id = 42, Name = "Leader" }, new CharacterModel { Id = 43, Name = "Other" });
            seed.Guild.AddRange(new GuildModel { Id = 77, Type = (byte)GuildType.Community, Name = "Original", LeaderId = 42 },
                new GuildModel { Id = 88, Type = (byte)GuildType.Community, Name = "Taken", LeaderId = 43 });
            seed.Residence.Add(new ResidenceModel { Id = 100, GuildOwnerId = 77, Name = "Original", PropertyInfoId = (byte)PropertyInfoId.Community });
            await seed.SaveChangesAsync();
        }
        var database = new CharacterDatabase(); database.Initialise(Config);
        var databases = RecordingDispatchProxy<IDatabaseManager>.Create(out var dbProxy);
        dbProxy.SetMethodReturn(nameof(IDatabaseManager.GetDatabase), database);
        var realm = RecordingDispatchProxy<IRealmContext>.Create(out var realmProxy);
        realmProxy.SetProperty(nameof(IRealmContext.RealmId), (ushort)1);
        var publisher = RecordingDispatchProxy<IInternalMessagePublisher>.Create(out var messages);
        messages.SetMethodReturn(nameof(IInternalMessagePublisher.PublishAsync), Task.CompletedTask);
        var residences = RecordingDispatchProxy<IGlobalResidenceManager>.Create(out var residenceProxy);
        var filter = RecordingDispatchProxy<ITextFilterManager>.Create(out var text);
        text.SetMethodReturn(nameof(ITextFilterManager.IsTextValid), true);
        var tables = RecordingDispatchProxy<IGameTableManager>.Create(out var tableProxy);
        tableProxy.SetProperty(nameof(IGameTableManager.GameFormula), Formula());
        residenceProxy.SetMethodHandler(nameof(IGlobalResidenceManager.CreateCommunity), args =>
        {
            var owner = (ICommunity)args[0];
            var created = new Residence(new ResidenceModel { Id = 501, GuildOwnerId = owner.Id, Name = owner.Name, PropertyInfoId = (byte)PropertyInfoId.Community }, gameTableManager: tables);
            // This name-lifecycle fixture has no client plot tables; use a real pending-create residence with no plots.
            FieldInfo mask = typeof(Residence).GetField("saveMask", BindingFlags.Instance | BindingFlags.NonPublic)!;
            mask.SetValue(created, Enum.Parse(mask.FieldType, "Create"));
            return created;
        });
        var registrations = new ServiceCollection();
        registrations.AddSingleton<IGuildFactory, GuildFactory>();
        registrations.AddSingleton<IGlobalGuildManager>(provider => new GlobalGuildManager(provider.GetRequiredService<IGuildFactory>(), databases));
        registrations.AddKeyedTransient<IGuildBase>(GuildType.Community, (provider, _) => new Community(realm, publisher, residences, filter, null,
            globalGuildManager: provider.GetRequiredService<IGlobalGuildManager>()));
        registrations.AddKeyedTransient<IGuildBase>(GuildType.Circle, (provider, _) => new Circle(realm, publisher, filter,
            globalGuildManager: provider.GetRequiredService<IGlobalGuildManager>()));
        ServiceProvider services = registrations.BuildServiceProvider();
        var manager = (GlobalGuildManager)services.GetRequiredService<IGlobalGuildManager>();
        manager.Initialise();
        var community = Assert.IsType<Community>(manager.GetGuild(77));
        var root = new Residence(new ResidenceModel { Id = 100, GuildOwnerId = 77, Name = "Original", PropertyInfoId = (byte)PropertyInfoId.Community }, gameTableManager: tables);
        community.Residence = root;
        IWorldSession session = RecordingDispatchProxy<IWorldSession>.Create(out var sessionProxy);
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out var playerProxy);
        IGuildManager guilds = RecordingDispatchProxy<IGuildManager>.Create(out var guildProxy);
        ICurrencyManager currency = RecordingDispatchProxy<ICurrencyManager>.Create(out var currencyProxy);
        IResidenceMapInstance map = RecordingDispatchProxy<IResidenceMapInstance>.Create(out _);
        guildProxy.SetMethodReturn(nameof(IGuildManager.GetGuild), community);
        currencyProxy.SetMethodReturn(nameof(ICurrencyManager.CanAfford), true);
        playerProxy.SetProperty(nameof(IPlayer.CharacterId), 42ul);
        playerProxy.SetProperty(nameof(IPlayer.GuildManager), guilds);
        playerProxy.SetProperty(nameof(IPlayer.CurrencyManager), currency);
        playerProxy.SetProperty(nameof(IPlayer.Map), map);
        playerProxy.SetProperty(nameof(IPlayer.Session), session);
        sessionProxy.SetProperty(nameof(IWorldSession.Player), player);
        return new(services, manager, community, root, session, sessionProxy, currencyProxy,
            new ClientHousingCommunityRenameHandler(filter, tables, realm, manager));
    }

    private static GameTable<GameFormulaEntry> Formula()
    {
        var table = (GameTable<GameFormulaEntry>)RuntimeHelpers.GetUninitializedObject(typeof(GameTable<GameFormulaEntry>));
        typeof(GameTable<GameFormulaEntry>).GetField("<Entries>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic)!
            .SetValue(table, new[] { new GameFormulaEntry { Id = 2395, Dataint0 = 1234, Dataint01 = 78 } });
        typeof(GameTable<GameFormulaEntry>).GetField("header", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(table, new GameTableHeader { MaxId = 2396 });
        int[] lookup = Enumerable.Repeat(-1, 2396).ToArray(); lookup[2395] = 0;
        typeof(GameTable<GameFormulaEntry>).GetField("lookup", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(table, lookup);
        return table;
    }

    private static ClientHousingCommunityRename Request(string name)
    {
        using var stream = new MemoryStream();
        using (var writer = new GamePacketWriter(stream))
        {
            new NexusForever.Network.World.Message.Model.Shared.Identity { Id = 999, RealmId = 1 }.Write(writer);
            writer.WriteStringWide(name); writer.Write(false); writer.FlushBits();
        }
        using var input = new MemoryStream(stream.ToArray());
        using var reader = new GamePacketReader(input);
        var request = new ClientHousingCommunityRename(); request.Read(reader); return request;
    }

    private static HousingResult Result(Fixture f) => Assert.Single(f.Messages.GetInvocations(nameof(IGameSession.EnqueueMessageEncrypted))
        .Select(call => call.Arguments[0]).OfType<ServerHousingCommunityRenameResult>()).Result;

    private static async Task Flush(GlobalGuildManager manager)
    {
        manager.Update(61);
        object gate = typeof(GlobalGuildManager).GetField("cacheLock", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(manager);
        var active = (HashSet<ulong>)typeof(GlobalGuildManager).GetField("guildSavesInFlight", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(manager);
        for (int i = 0; i < 500; i++) { lock (gate) if (active.Count == 0) return; await Task.Delay(10); }
        Assert.Fail("Periodic rename did not settle.");
    }

    private static Task WithConfiguredStrategy(CharacterContext context, Func<Task> action)
    {
        bool entered = false;
        return context.Database.CreateExecutionStrategy().ExecuteAsync(async () =>
        {
            if (entered) throw new InvalidOperationException("A configured strategy cannot replay the rename fixture's gameplay callback.");
            entered = true;
            await action();
        });
    }

    private sealed record Fixture(ServiceProvider Services, GlobalGuildManager Manager, Community Community, Residence Root,
        IWorldSession Session, RecordingDispatchProxy<IWorldSession> Messages, RecordingDispatchProxy<ICurrencyManager> Currency,
        ClientHousingCommunityRenameHandler Handler) : IDisposable
    {
        public void Dispose() => Services.Dispose();
    }

    public void Dispose()
    {
        using var pool = new Microsoft.Data.Sqlite.SqliteConnection(Config.ConnectionString);
        Microsoft.Data.Sqlite.SqliteConnection.ClearPool(pool);
        if (File.Exists(file)) File.Delete(file);
    }
}
