using System.Data.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using NexusForever.Database;
using NexusForever.Database.Character;
using NexusForever.Database.Character.Model;
using NexusForever.Database.Configuration.Model;
using NexusForever.Game.Abstract.Account;
using NexusForever.Game.Abstract.Achievement;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Entity;
using NexusForever.Game.Quest;
using NexusForever.Game.Static.Entity;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.Game.Tests.Transport;
using NexusForever.GameTable;
using NexusForever.GameTable.Model;
using NexusForever.Network.Session;
using ContractFixture = NexusForever.Game.Tests.Quest.ContractClaimTransactionTests.Fixture;
using GameCurrency = NexusForever.Game.Entity.Currency;
using GameItem = NexusForever.Game.Entity.Item;

namespace NexusForever.Game.Tests.Entity;

public sealed partial class CurrencyLoadProvenanceTests
{
    public static IEnumerable<object[]> Routes()
    {
        foreach (string path in new[] { "manager", "row", "frozen", "bank", "frozen-bank" })
            foreach (string route in new[] { "same", "normalized", "foreign" })
                foreach (bool exists in new[] { false, true })
                    yield return [path, route, exists];
    }

    [Theory]
    [MemberData(nameof(Routes))]
    public async Task LoadedCurrencyCannotSaveEqualRowsInAnotherDatabase(string path, string route, bool exists)
    {
        using var f = new Fixture(exists);
        var model = Assert.Single(await f.Database.GetCharacters(12));
        var manager = f.Manager(model);
        bool bank = path.EndsWith("bank", StringComparison.Ordinal), frozen = path.StartsWith("frozen", StringComparison.Ordinal);
        GameCurrency wallet = null;
        if (!bank)
        {
            if (!exists) manager.CurrencyAddAmount(CurrencyType.Credits, 100);
            wallet = Assert.IsType<GameCurrency>(Assert.Single(manager)); wallet.Amount = 90;
        }
        var destination = route == "foreign" ? f.Second : route == "normalized" ? f.NormalizedFirst : f.First;
        f.Database.Initialise(destination);
        object token = new();
        CurrencyManager.BankCurrencyReservation reservation = null;
        bool rejected;
        Exception error;
        using (var save = f.Database.CreateContext())
        {
            using var lease = frozen ? QuestSaveScope.AcquireFrozen(manager, token, GameItem.GetRewardDatabaseKey(save), 42) : null;
            Action<CharacterContext> stage;
            if (bank)
            {
                CurrencyManager.BankCurrencyChange[] changes = [new(CurrencyType.Credits, 10, exists)];
                reservation = frozen ? manager.CaptureFrozenCurrency(f.Player, token, changes, () => true, _ => { })
                    : manager.CaptureBankCurrency(f.Player, changes, () => true, _ => { });
                stage = reservation.Stage;
            }
            else stage = path == "frozen" ? manager.CaptureNormalSave(token) : path == "row" ? wallet.Save : manager.Save;
            save.Database.AutoTransactionBehavior = AutoTransactionBehavior.Always;
            save.Character.Single(row => row.Id == 42).TimePlayedTotal = 123;
            error = await Record.ExceptionAsync(async () => { stage(save); await save.SaveChangesAsync(); });
            rejected = save.IsSaveRejected;
        }
        if (reservation != null)
        {
            if (error == null && route != "foreign") { reservation.Adopt(); reservation.Publish(); reservation.Complete(); }
            else reservation.Abort();
        }
        ulong target = bank && !exists ? 10ul : 90ul;
        ulong? original = exists ? 100ul : null;
        if (route == "foreign")
        {
            Assert.Equal(original, Fixture.Stored(f.Second));
            Assert.Equal(original, Fixture.Stored(f.First));
            Assert.IsType<InvalidOperationException>(error); Assert.True(rejected);
            using var read = new CharacterContext(f.Second); Assert.Equal(0u, read.Character.Single(row => row.Id == 42).TimePlayedTotal);
        }
        else
        {
            Assert.Null(error); Assert.False(rejected); Assert.Equal((ulong?)target, Fixture.Stored(f.First));
            using var read = new CharacterContext(f.First); Assert.Equal(123u, read.Character.Single(row => row.Id == 42).TimePlayedTotal);
        }
        Assert.False(manager.HasPendingContractCurrency);
    }

    [Fact]
    public async Task CurrencyOriginComesFromTheActualQueryNotALaterFactoryConfiguration()
    {
        using var f = new Fixture(true);
        f.Database.Initialise(new SwitchDuringConfiguration(f.First, () => f.Database.Initialise(f.Second)));
        var model = Assert.Single(await f.Database.GetCharacters(12));
        var manager = f.Manager(model); Assert.Single(manager).Amount = 90;
        using var save = f.Database.CreateContext();
        var error = await Record.ExceptionAsync(async () => { manager.Save(save); await save.SaveChangesAsync(); });
        Assert.Equal((ulong?)100, Fixture.Stored(f.First)); Assert.Equal((ulong?)100, Fixture.Stored(f.Second));
        Assert.IsType<InvalidOperationException>(error); Assert.True(save.IsSaveRejected);
    }

    private sealed class SwitchDuringConfiguration(IConnectionString original, Action switchFactory) : IConnectionString
    {
        private int entered;
        public DatabaseProvider Provider => original.Provider;
        public string ConnectionString
        {
            get { if (Interlocked.Exchange(ref entered, 1) == 0) switchFactory(); return original.ConnectionString; }
        }
    }

    [Theory]
    [InlineData(false, "same")] [InlineData(false, "normalized")] [InlineData(false, "foreign")]
    [InlineData(true, "same")] [InlineData(true, "normalized")] [InlineData(true, "foreign")]
    public async Task EmptyLoadedManagersRetainTheirDatabaseOrigin(bool frozen, string route)
    {
        using var f = new Fixture(false); var manager = f.Manager(Assert.Single(await f.Database.GetCharacters(12)));
        f.Database.Initialise(route == "foreign" ? f.Second : route == "normalized" ? f.NormalizedFirst : f.First);
        using var save = f.Database.CreateContext(); object token = new();
        using var lease = frozen ? QuestSaveScope.AcquireFrozen(manager, token, GameItem.GetRewardDatabaseKey(save), 42) : null;
        Action<CharacterContext> stage = frozen ? manager.CaptureNormalSave(token) : manager.Save;
        save.Character.Single().TimePlayedTotal = 123;
        var error = await Record.ExceptionAsync(async () => { stage(save); await save.SaveChangesAsync(); });
        if (route == "foreign")
        {
            Assert.IsType<InvalidOperationException>(error); Assert.True(save.IsSaveRejected);
            using var read = new CharacterContext(f.Second); Assert.Equal(0u, read.Character.Single().TimePlayedTotal);
        }
        else
        {
            Assert.Null(error); using var read = new CharacterContext(f.First); Assert.Equal(123u, read.Character.Single().TimePlayedTotal);
        }
        Assert.Empty(manager); Assert.Null(Fixture.Stored(f.First)); Assert.Null(Fixture.Stored(f.Second));
    }

    [Theory]
    [InlineData("character")] [InlineData("account")] [InlineData("amount")] [InlineData("currency")]
    [InlineData("row-owner")] [InlineData("replacement")] [InlineData("null")] [InlineData("extra")]
    public async Task AlteredLoadedModelsCannotManufactureAnotherCurrencyOrigin(string fault)
    {
        using var f = new Fixture(true); var model = Assert.Single(await f.Database.GetCharacters(12)); var row = Assert.Single(model.Currency);
        switch (fault)
        {
            case "character": model.Id = 43; break;
            case "account": model.AccountId = 13; break;
            case "amount": row.Amount = 101; break;
            case "currency": row.CurrencyId = 2; break;
            case "row-owner": row.Id = 43; break;
            case "replacement": model.Currency.Clear(); model.Currency.Add(new() { Id = 42, CurrencyId = 1, Amount = 100 }); break;
            case "null": model.Currency.Clear(); model.Currency.Add(null); break;
            case "extra": model.Currency.Add(new() { Id = 42, CurrencyId = 2, Amount = 1 }); break;
        }
        Assert.Throws<InvalidOperationException>(() => f.Manager(model));
        Assert.Equal((ulong?)100, Fixture.Stored(f.First)); Assert.Equal((ulong?)100, Fixture.Stored(f.Second));
    }

    [Theory]
    [InlineData("character")] [InlineData("account")]
    public async Task ALoadedModelRequiresItsOriginalActorIdentityAtInitialization(string fault)
    {
        using var f = new Fixture(true); var model = Assert.Single(await f.Database.GetCharacters(12));
        ChangeOwner(f.Player, fault); Assert.Throws<InvalidOperationException>(() => f.Manager(model));
    }

    [Theory]
    [InlineData(false, "character")] [InlineData(false, "account")]
    [InlineData(true, "character")] [InlineData(true, "account")]
    public async Task ChangedActorsCannotCreateOrReserveKnownOriginWallets(bool create, string fault)
    {
        using var f = new Fixture(!create); var manager = f.Manager(Assert.Single(await f.Database.GetCharacters(12)));
        ChangeOwner(f.Player, fault);
        if (create) Assert.Throws<InvalidOperationException>(() => manager.CurrencyAddAmount(CurrencyType.Credits, 1));
        else Assert.Throws<InvalidOperationException>(() => manager.CaptureBankCurrency(f.Player, [new(CurrencyType.Credits, 1, true)], () => true, _ => { }));
        Assert.False(manager.HasPendingContractCurrency);
        Assert.Equal(create ? null : (ulong?)100, Fixture.Stored(f.First));
    }

    private static void ChangeOwner(IPlayer player, string fault)
    {
        if (fault == "character") ((RecordingDispatchProxy<IPlayer>)(object)player).SetProperty(nameof(IPlayer.CharacterId), 43ul);
        else ((RecordingDispatchProxy<IAccount>)(object)player.Account).SetProperty(nameof(IAccount.Id), 13u);
    }

    [Fact]
    public async Task ClonedModelsStayUnqualifiedAndRetainLegacySameRouteSaving()
    {
        using var f = new Fixture(true); var original = Assert.Single(await f.Database.GetCharacters(12));
        var copy = new CharacterModel { Id = original.Id, AccountId = original.AccountId };
        copy.Currency.Add(new() { Id = 42, CurrencyId = 1, Amount = 100 });
        Assert.NotNull(CharacterLoadOrigin.Get(original)); Assert.Null(CharacterLoadOrigin.Get(copy));
        Assert.Null(CharacterLoadOrigin.GetCurrencyDatabase(Assert.Single(copy.Currency)));
        var manager = f.Manager(copy); Assert.Single(manager).Amount = 90;
        using (var save = new CharacterContext(f.First)) { manager.Save(save); await save.SaveChangesAsync(); }
        Assert.Equal((ulong?)90, Fixture.Stored(f.First)); Assert.Null(CharacterLoadOrigin.Get(copy));
    }

    [Fact]
    public async Task ASourceOnlyRowDoesNotAcquireOriginFromAnEqualSqlRowOrSuccessfulSave()
    {
        using var f = new Fixture(true); var model = new CharacterCurrencyModel { Id = 42, CurrencyId = 1, Amount = 100 };
        var wallet = new GameCurrency(model, f.Tables) { Amount = 90 };
        Assert.Null(CharacterLoadOrigin.GetCurrencyDatabase(model));
        using (var save = new CharacterContext(f.First)) { wallet.Save(save); await save.SaveChangesAsync(); }
        Assert.Equal((ulong?)90, Fixture.Stored(f.First)); Assert.Null(CharacterLoadOrigin.GetCurrencyDatabase(model));
    }

    [Theory]
    [InlineData(false, "normal")] [InlineData(false, "bank")] [InlineData(false, "frozen-bank")]
    [InlineData(true, "normal")] [InlineData(true, "bank")] [InlineData(true, "frozen-bank")]
    public async Task CaptureDoesNotReadAChangedProviderConfiguration(bool exists, string path)
    {
        using var f = new Fixture(exists); var manager = f.Manager(Assert.Single(await f.Database.GetCharacters(12)));
        using var identity = new CharacterContext(f.First); object token = new();
        using var scope = QuestSaveScope.AcquireFrozen(manager, token, GameItem.GetRewardDatabaseKey(identity), 42);
        var forbidden = new ForbiddenConfiguration(); f.Database.Initialise(forbidden);
        if (path == "normal") Assert.NotNull(manager.CaptureNormalSave(token));
        else
        {
            CurrencyManager.BankCurrencyChange[] changes = [new(CurrencyType.Credits, 1, exists)];
            var reservation = path == "bank" ? manager.CaptureBankCurrency(f.Player, changes, () => true, _ => { })
                : manager.CaptureFrozenCurrency(f.Player, token, changes, () => true, _ => { });
            reservation.Abort(); Assert.False(manager.HasPendingContractCurrency);
        }
        Assert.Equal(0, forbidden.Reads);
    }

    [Theory]
    [InlineData(false, false)] [InlineData(false, true)] [InlineData(true, false)] [InlineData(true, true)]
    public async Task ContractCreditAdmissionRetainsTheLoadedRouteForExistingAndNewWallets(bool absent, bool foreign)
    {
        using var source = new ContractFixture(null, null, 69, 0);
        using var identity = source.Character();
        if (absent) identity.CharacterCurrency.ExecuteDelete();
        var database = new CharacterDatabase();
        database.Initialise(new DatabaseConnectionString { Provider = DatabaseProvider.Sqlite, ConnectionString = identity.Database.GetDbConnection().ConnectionString });
        var manager = new CurrencyManager(source.Tables); manager.Initialise(source.Inventory.Player, Assert.Single(await database.GetCharacters(27)));
        source.Inventory.PlayerProxy.SetProperty(nameof(IPlayer.CurrencyManager), manager);
        string loaded = GameItem.GetRewardDatabaseKey(identity);
        bool admitted = manager.TryReserveContractCredit(source.Awards, source.Quote, source.Command.Identity, foreign ? loaded + ":foreign" : loaded, out var ticket);
        Assert.Equal(!foreign, admitted);
        if (admitted) Assert.True(ticket.Cancel()); else Assert.Null(ticket);
        Assert.False(manager.HasPendingContractCurrency); Assert.False(manager.RequiresContractCurrencyReload);
    }

    [Fact]
    public void SharedKeysRemovePasswordsWithoutOpeningTheProvider()
    {
        var blocked = new BlockConnections();
        using var first = KeyContext("User=origin_fixture;Password=fixture-one;Server=127.0.0.1;Database=origin_model;PersistSecurityInfo=true", blocked);
        using var second = KeyContext("Database=origin_model;Server=127.0.0.1;Password=fixture-two;User=origin_fixture;PersistSecurityInfo=true", blocked);
        string key = CharacterDatabaseIdentity.Get(first);
        Assert.Equal(key, CharacterDatabaseIdentity.Get(second));
        Assert.DoesNotContain("fixture-one", key); Assert.DoesNotContain("fixture-two", key); Assert.Contains("origin_fixture", key);
        Assert.Equal(0, blocked.Attempts);
    }

    private static CharacterContext KeyContext(string connection, BlockConnections blocked) => new(new DbContextOptionsBuilder<CharacterContext>()
        .UseMySql(connection, new MySqlServerVersion(new Version(8, 0, 36))).AddInterceptors(blocked).Options);

    private sealed class ForbiddenConfiguration : IConnectionString
    {
        internal int Reads;
        public DatabaseProvider Provider { get { Reads++; throw new InvalidOperationException("Capture configured a provider."); } }
        public string ConnectionString { get { Reads++; throw new InvalidOperationException("Capture read a database route."); } }
    }
    private sealed class BlockConnections : DbConnectionInterceptor
    {
        internal int Attempts;
        public override InterceptionResult ConnectionOpening(DbConnection connection, ConnectionEventData data, InterceptionResult result)
        { Attempts++; throw new InvalidOperationException("Key inspection must not open a provider."); }
        public override ValueTask<InterceptionResult> ConnectionOpeningAsync(DbConnection connection, ConnectionEventData data, InterceptionResult result, CancellationToken cancellationToken = default)
        { Attempts++; throw new InvalidOperationException("Key inspection must not open a provider."); }
    }

    private sealed class Fixture : IDisposable
    {
        private readonly string directory = Path.Combine(Path.GetTempPath(), "NexusForever-CurrencyOrigin-" + Guid.NewGuid().ToString("N"));
        internal readonly DatabaseConnectionString First, Second, NormalizedFirst;
        internal readonly CharacterDatabase Database = new();
        internal readonly IPlayer Player;
        internal readonly IGameTableManager Tables;
        private readonly RecordingDispatchProxy<IPlayer> player;

        internal Fixture(bool exists)
        {
            Directory.CreateDirectory(directory);
            First = Config(Path.Combine(directory, "first.sqlite")); Second = Config(Path.Combine(directory, "second.sqlite"));
            NormalizedFirst = new() { Provider = DatabaseProvider.Sqlite, ConnectionString = $"Pooling=False;Data Source={Path.Combine(directory, "first.sqlite")}" };
            foreach (var config in new[] { First, Second })
            {
                using var seed = new CharacterContext(config); seed.Database.EnsureCreated();
                seed.Character.Add(new() { Id = 42, AccountId = 12, Name = "Currency origin", OriginalName = "Currency origin" });
                if (exists) seed.CharacterCurrency.Add(new() { Id = 42, CurrencyId = 1, Amount = 100 });
                seed.SaveChanges();
            }
            Database.Initialise(First);
            Tables = RecordingDispatchProxy<IGameTableManager>.Create(out var tables);
            tables.SetProperty(nameof(IGameTableManager.CurrencyType), PlayerBindPointTests.Table(new CurrencyTypeEntry { Id = 1, CapAmount = 1000 }));
            Player = RecordingDispatchProxy<IPlayer>.Create(out player); player.SetProperty(nameof(IPlayer.CharacterId), 42ul);
            var session = RecordingDispatchProxy<IGameSession>.Create(out _);
            var account = RecordingDispatchProxy<IAccount>.Create(out var accountProxy); accountProxy.SetProperty(nameof(IAccount.Id), 12u); accountProxy.SetProperty(nameof(IAccount.Session), session);
            player.SetProperty(nameof(IPlayer.Account), account); player.SetProperty(nameof(IPlayer.Session), session);
            player.SetProperty(nameof(IPlayer.AchievementManager), RecordingDispatchProxy<ICharacterAchievementManager>.Create(out _));
        }
        internal CurrencyManager Manager(CharacterModel model)
        {
            var manager = new CurrencyManager(Tables); manager.Initialise(Player, model);
            player.SetProperty(nameof(IPlayer.CurrencyManager), manager); return manager;
        }
        private static DatabaseConnectionString Config(string path) => new() { Provider = DatabaseProvider.Sqlite, ConnectionString = $"Data Source={path};Pooling=False" };
        internal static ulong? Stored(IConnectionString config)
        {
            using var read = new CharacterContext(config);
            return read.CharacterCurrency.Where(row => row.Id == 42 && row.CurrencyId == 1).Select(row => (ulong?)row.Amount).SingleOrDefault();
        }
        public void Dispose()
        {
            string full = Path.GetFullPath(directory), parent = Path.GetFullPath(Path.GetTempPath()).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
            if (!full.StartsWith(parent, StringComparison.OrdinalIgnoreCase) || !Path.GetFileName(full).StartsWith("NexusForever-CurrencyOrigin-", StringComparison.Ordinal))
                throw new InvalidOperationException("Currency fixture escaped its temp root.");
            if (Directory.Exists(full)) TemporaryDirectoryCleanup.Delete(full);
        }
    }
}
