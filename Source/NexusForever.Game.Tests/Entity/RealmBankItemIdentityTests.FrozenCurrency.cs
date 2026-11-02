using Microsoft.EntityFrameworkCore;
using NexusForever.Database.Character;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Entity;
using NexusForever.Game.Quest;
using NexusForever.Game.Static.Entity;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.Network.Session;
using GameItem = NexusForever.Game.Entity.Item;

namespace NexusForever.Game.Tests.Entity;

public sealed partial class RealmBankItemIdentityTests
{
    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task FrozenCurrencyLegacyNullTokenCompatibility(bool frozen)
    {
        var f = await MailFixture(); object token = new();
        using var lease = frozen ? QuestSaveScope.AcquireFrozen(new object(), token, FrozenCurrencyDatabase(), 100) : null;
        var reservation = f.Currency.CaptureBankCurrency(f.Player, [new(CurrencyType.Credits, 10, true)], () => true, _ => { });
        using var save = new CharacterContext(Configuration);
        if (frozen)
        {
            Assert.Throws<InvalidOperationException>(() => reservation.Stage(save)); Assert.True(save.IsSaveRejected);
            reservation.Abort(); Assert.Equal(100ul, Assert.Single(f.Currency).Amount);
        }
        else
        {
            reservation.Stage(save); await save.SaveChangesAsync(); reservation.Adopt(); reservation.Publish(); reservation.Complete();
            Assert.Equal(90ul, Assert.Single(f.Currency).Amount);
        }
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task FrozenCurrencyCommitOrRollbackKeepsItsCapturedOwner(bool commit)
    {
        var f = await MailFixture(); object token = new(); string database = FrozenCurrencyDatabase();
        using (QuestSaveScope.AcquireFrozen(new object(), token, database, 100))
        {
            var reservation = FrozenCurrencyCapture(f.Currency, f.Player, token);
            using (var save = new CharacterContext(Configuration))
            {
                await using var transaction = await save.Database.BeginTransactionAsync();
                reservation.Stage(save); await save.SaveChangesAsync(); Assert.Equal(100ul, Assert.Single(f.Currency).Amount);
                Assert.Empty(BankMessages(f.Player.Session));
                if (commit) await transaction.CommitAsync(); else await transaction.RollbackAsync();
            }
            if (commit) { reservation.Adopt(); reservation.Publish(); reservation.Complete(); }
            else reservation.Abort();
        }
        using (var read = new CharacterContext(Configuration))
            Assert.Equal(commit ? 90ul : 100ul, Assert.Single(read.CharacterCurrency).Amount);
        Assert.Equal(commit ? 90ul : 100ul, Assert.Single(f.Currency).Amount);
        Assert.False(f.Currency.HasPendingContractCurrency);
        if (!commit)
        {
            object retryToken = new(); using var retryLease = QuestSaveScope.AcquireFrozen(new object(), retryToken, database, 100);
            var retry = FrozenCurrencyCapture(f.Currency, f.Player, retryToken, 5);
            using var save = new CharacterContext(Configuration); retry.Stage(save); await save.SaveChangesAsync();
            retry.Adopt(); retry.Publish(); retry.Complete(); Assert.Equal(95ul, Assert.Single(f.Currency).Amount);
        }
    }

    [Theory]
    [InlineData("missing")] [InlineData("token")] [InlineData("database")] [InlineData("character")]
    public async Task FrozenCurrencyRejectsAnUnownedScopeBeforeWriting(string fault)
    {
        var f = await MailFixture(); object token = new(); string database = FrozenCurrencyDatabase();
        using var lease = fault == "missing" ? null : QuestSaveScope.AcquireFrozen(new object(), fault == "token" ? new object() : token,
            fault == "database" ? database + ":foreign" : database, fault == "character" ? 200ul : 100ul);
        var reservation = FrozenCurrencyCapture(f.Currency, f.Player, token);
        using (var save = new CharacterContext(Configuration))
        {
            Assert.Throws<InvalidOperationException>(() => reservation.Stage(save)); Assert.True(save.IsSaveRejected);
            await Assert.ThrowsAsync<InvalidOperationException>(() => save.SaveChangesAsync());
        }
        reservation.Abort(); Assert.False(f.Currency.HasPendingContractCurrency);
        Assert.Equal(100ul, Assert.Single(f.Currency).Amount); Assert.Empty(BankMessages(f.Player.Session));
        using var read = new CharacterContext(Configuration); Assert.Equal(100ul, Assert.Single(read.CharacterCurrency).Amount);
    }

    [Fact]
    public async Task FrozenCurrencyRequiresAnExplicitTokenAndCurrentActor()
    {
        var f = await MailFixture();
        Assert.Throws<ArgumentNullException>(() => FrozenCurrencyCapture(f.Currency, f.Player, null));
        Assert.Throws<InvalidOperationException>(() => f.Currency.CaptureFrozenCurrency(f.Player, new object(),
            [new(CurrencyType.Credits, 10, true)], () => false, _ => { }));
        Assert.False(f.Currency.HasPendingContractCurrency); Assert.Equal(100ul, Assert.Single(f.Currency).Amount);
    }

    [Fact]
    public async Task FrozenCurrencyDistinctCharacterGroupsShareAnOwnedContext()
    {
        var f = await MailFixture(); var receiver = RecordingDispatchProxy<IPlayer>.Create(out var rp);
        rp.SetProperty(nameof(IPlayer.CharacterId), 200ul);
        rp.SetProperty(nameof(IPlayer.Session), RecordingDispatchProxy<IGameSession>.Create(out _));
        var other = await InstallBankCurrency(receiver); object first = new(), second = new(); string database = FrozenCurrencyDatabase();
        using var firstLease = QuestSaveScope.AcquireFrozen(new object(), first, database, 100);
        using var secondLease = QuestSaveScope.AcquireFrozen(new object(), second, database, 200);
        var debit = FrozenCurrencyCapture(f.Currency, f.Player, first);
        var credit = FrozenCurrencyCapture(other, receiver, second, 10, false);
        using (var save = new CharacterContext(Configuration))
        {
            await using var transaction = await save.Database.BeginTransactionAsync(); debit.Stage(save); credit.Stage(save);
            await save.SaveChangesAsync(); await transaction.CommitAsync();
        }
        debit.Adopt(); credit.Adopt(); debit.Publish(); credit.Publish(); debit.Complete(); credit.Complete();
        using var read = new CharacterContext(Configuration);
        Assert.Equal(new ulong[] { 90, 110 }, read.CharacterCurrency.AsEnumerable().OrderBy(r => r.Id).Select(r => r.Amount));
        Assert.Equal(90ul, Assert.Single(f.Currency).Amount); Assert.Equal(110ul, Assert.Single(other).Amount);
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task FrozenCurrencyDeferredIntentPublishesOrRequiresReconciliation(bool commit)
    {
        var f = await MailFixture(); object token = new();
        using (QuestSaveScope.AcquireFrozen(new object(), token, FrozenCurrencyDatabase(), 100))
        {
            var reservation = FrozenCurrencyCapture(f.Currency, f.Player, token);
            f.Currency.CurrencyAddAmount(CurrencyType.Credits, 5); Assert.Equal(100ul, Assert.Single(f.Currency).Amount);
            using (var save = new CharacterContext(Configuration))
            {
                await using var transaction = await save.Database.BeginTransactionAsync(); reservation.Stage(save); await save.SaveChangesAsync();
                if (commit) await transaction.CommitAsync(); else await transaction.RollbackAsync();
            }
            if (commit) { reservation.Adopt(); reservation.Publish(); reservation.Complete(); }
            else
            {
                Assert.Throws<InvalidOperationException>(() => reservation.Abort());
                Assert.True(f.Currency.HasPendingContractCurrency); Assert.True(f.Currency.RequiresContractCurrencyReload);
                reservation.Abort(); Assert.True(f.Currency.HasPendingContractCurrency);
                Assert.Empty(BankMessages(f.Player.Session));
            }
            Assert.Equal(commit ? 95ul : 100ul, Assert.Single(f.Currency).Amount);
            using var read = new CharacterContext(Configuration); Assert.Equal(commit ? 90ul : 100ul, Assert.Single(read.CharacterCurrency).Amount);
        }
        using var normal = new CharacterContext(Configuration);
        if (commit) { f.Currency.Save(normal); await normal.SaveChangesAsync(); }
        else { Assert.Throws<InvalidOperationException>(() => f.Currency.Save(normal)); Assert.True(normal.IsSaveRejected); }
        using var check = new CharacterContext(Configuration); Assert.Equal(commit ? 95ul : 100ul, Assert.Single(check.CharacterCurrency).Amount);
    }

    [Fact]
    public async Task FrozenCurrencyLegacyAbortAlsoRejectsUnresolvedLaterIntent()
    {
        var f = await MailFixture();
        var reservation = f.Currency.CaptureBankCurrency(f.Player, [new(CurrencyType.Credits, 10, true)], () => true, _ => { });
        f.Currency.CurrencyAddAmount(CurrencyType.Credits, 5);
        Assert.Throws<InvalidOperationException>(() => reservation.Abort());
        Assert.True(f.Currency.RequiresContractCurrencyReload); Assert.True(f.Currency.HasPendingContractCurrency);
        Assert.Equal(100ul, Assert.Single(f.Currency).Amount); Assert.Empty(BankMessages(f.Player.Session));
        using var normal = new CharacterContext(Configuration);
        Assert.Throws<InvalidOperationException>(() => f.Currency.Save(normal)); Assert.True(normal.IsSaveRejected);
    }

    [Fact]
    public async Task FrozenCurrencyExcludesOrdinaryManagerAndRowSaves()
    {
        var f = await MailFixture(); object token = new();
        using var lease = QuestSaveScope.AcquireFrozen(new object(), token, FrozenCurrencyDatabase(), 100);
        var reservation = FrozenCurrencyCapture(f.Currency, f.Player, token);
        using (var rejected = new CharacterContext(Configuration))
        { Assert.Throws<InvalidOperationException>(() => f.Currency.Save(rejected)); Assert.True(rejected.IsSaveRejected); }
        using (var rejected = new CharacterContext(Configuration))
        { Assert.Throws<InvalidOperationException>(() => Assert.Single(f.Currency).Save(rejected)); Assert.True(rejected.IsSaveRejected); }
        Assert.Throws<InvalidOperationException>(() => f.Currency.CaptureNormalSave(token));
        reservation.Abort(); Assert.False(f.Currency.HasPendingContractCurrency);
    }

    private string FrozenCurrencyDatabase()
    { using var context = new CharacterContext(Configuration); return GameItem.GetRewardDatabaseKey(context); }

    private static CurrencyManager.BankCurrencyReservation FrozenCurrencyCapture(CurrencyManager manager, IPlayer player, object token,
        ulong amount = 10, bool subtract = true)
        => manager.CaptureFrozenCurrency(player, token, [new(CurrencyType.Credits, amount, subtract)], () => true, _ => { });
}
