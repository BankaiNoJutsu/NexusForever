using Microsoft.EntityFrameworkCore;
using NexusForever.Database.Character;
using NexusForever.Game.Entity;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Static.Crafting;
using NexusForever.Game.Static.Entity;

namespace NexusForever.Game.Tests.Entity;

public sealed partial class RealmBankItemIdentityTests
{
    [Fact]
    public async Task NormalSavePersistsTheNonExpiredBankCountdown()
    {
        var (inventory, _, _, _, source) = await SplitFixture(true);
        inventory.Update(34.5);
        Assert.Equal(1200u, source.ExpirationTimeLeft);
        using (var save = new CharacterContext(Configuration)) { inventory.Save(save); await save.SaveChangesAsync(); }
        using var read = new CharacterContext(Configuration);
        Assert.Equal(1200u, Assert.Single(read.RealmBankItem).ExpirationTimeLeft); Assert.Empty(read.Item);
    }

    [Fact]
    public async Task NormalSavePersistsBankScalarsAndPublicCodecMutationsAcrossReload()
    {
        var (inventory, _, _, _, source) = await SplitFixture(true);
        source.StackCount = 12; source.Charges = 3; source.Durability = 0.5f;
        source.MicrochipIds.Add(123); source.RuneSlots.Add(new ItemRuneSlot((RuneType)7, 9001));
        using (var save = new CharacterContext(Configuration)) { inventory.Save(save); await save.SaveChangesAsync(); }
        var (manager, player, _) = Access(); var loaded = Load(manager, player);
        Assert.Equal(12u, loaded.StackCount); Assert.Equal(3u, loaded.Charges); Assert.Equal(0.5f, loaded.Durability);
        Assert.Equal(ItemMicrochipIdsCodec.Serialize(source.MicrochipIds), ItemMicrochipIdsCodec.Serialize(loaded.MicrochipIds));
        Assert.Equal(ItemRuneSlotsCodec.Serialize(source.RuneSlots), ItemRuneSlotsCodec.Serialize(loaded.RuneSlots));
    }

    [Fact]
    public async Task CodecOnlyBankMutationIsIncludedInTheNextNormalSave()
    {
        var (inventory, _, _, _, source) = await SplitFixture(true);
        using (var first = new CharacterContext(Configuration)) { inventory.Save(first); await first.SaveChangesAsync(); }
        source.MicrochipIds.Add(123);
        using (var next = new CharacterContext(Configuration)) { inventory.Save(next); await next.SaveChangesAsync(); }
        using var read = new CharacterContext(Configuration); Assert.Equal("123", Assert.Single(read.RealmBankItem).MicrochipIds);
    }

    [Fact]
    public async Task FailedPeriodicBankWriteRollsBackTheCarriedItemAndRetriesTogether()
    {
        var character = CharacterRow(); character.Id = 402;
        await Seed(character, BankRow(401));
        var (manager, player, items) = Access();
        var (inventory, carried, bank) = SwapInventory(manager, player, items, character, BankRow(401));
        carried.StackCount = 9; bank.Charges = 2; inventory.Update(34);
        await SplitTrigger("realm_bank_item", "UPDATE");
        using (var failed = new CharacterContext(Configuration))
        {
            inventory.Save(failed);
            await Assert.ThrowsAnyAsync<Exception>(() => failed.SaveChangesAsync());
        }
        Assert.False(carried.HasPendingSave); Assert.False(bank.HasPendingSave);
        using var read = new CharacterContext(Configuration);
        Assert.Equal(17u, Assert.Single(read.Item).StackCount); Assert.Equal(1234u, Assert.Single(read.RealmBankItem).ExpirationTimeLeft);
        await read.Database.ExecuteSqlRawAsync("DROP TRIGGER reject_split");
        using (var retry = new CharacterContext(Configuration)) { inventory.Save(retry); await retry.SaveChangesAsync(); }
        read.ChangeTracker.Clear(); Assert.Equal(9u, Assert.Single(read.Item).StackCount);
        Assert.Equal(1200u, Assert.Single(read.RealmBankItem).ExpirationTimeLeft); Assert.Equal(2u, read.RealmBankItem.Single().Charges);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ExpiryWaitsForPeriodicBankSaveCommitOrRollback(bool commit)
    {
        var (inventory, _, _, _, source) = await SplitFixture(true);
        inventory.Update(1230);
        using (var save = new CharacterContext(Configuration))
        {
            inventory.Save(save); Assert.True(source.HasPendingSave);
            Assert.True(inventory.IsSaveBlocked);
            Assert.False(inventory.TryAcquireSaveLease(out _)); Assert.False(inventory.TryAcquireMutationLease(out _));
            inventory.Update(3); Assert.Equal(4u, source.ExpirationTimeLeft);
            if (commit) await save.SaveChangesAsync();
        }
        inventory.Update(1); await WaitForBankMove(inventory, source);
        Assert.Null(inventory.GetItem(401ul)); Assert.False(source.HasUnsettledDeletion);
        using var read = new CharacterContext(Configuration); Assert.Empty(read.Item); Assert.Empty(read.RealmBankItem);
    }

    [Fact]
    public async Task LaterBankFieldIntentSurvivesTheEnrolledPeriodicSave()
    {
        var (inventory, _, _, _, source) = await SplitFixture(true);
        source.Charges = 4;
        using (var first = new CharacterContext(Configuration))
        {
            inventory.Save(first); source.Charges = 2; await first.SaveChangesAsync();
        }
        using (var read = new CharacterContext(Configuration)) Assert.Equal(4u, Assert.Single(read.RealmBankItem).Charges);
        using (var next = new CharacterContext(Configuration)) { inventory.Save(next); await next.SaveChangesAsync(); }
        using var final = new CharacterContext(Configuration); Assert.Equal(2u, Assert.Single(final.RealmBankItem).Charges);
    }

    [Theory]
    [InlineData("account")]
    [InlineData("realm")]
    [InlineData("definition")]
    [InlineData("values")]
    public async Task StalePeriodicBankWriteRejectsTheEntireContext(string conflict)
    {
        var (inventory, _, _, _, source) = await SplitFixture(true);
        inventory.Update(34);
        using (var change = new CharacterContext(Configuration))
        {
            var row = change.RealmBankItem.Single();
            if (conflict == "account") row.AccountId = 13;
            else if (conflict == "realm") row.RealmId = 3;
            else if (conflict == "definition") row.ItemId = 7002;
            else row.Charges = 1;
            await change.SaveChangesAsync();
        }
        using var rejected = new CharacterContext(Configuration);
        Assert.Throws<InvalidOperationException>(() => inventory.Save(rejected));
        await Assert.ThrowsAsync<InvalidOperationException>(() => rejected.SaveChangesAsync());
        using var read = new CharacterContext(Configuration);
        Assert.Equal(1234u, Assert.Single(read.RealmBankItem).ExpirationTimeLeft); Assert.Empty(read.Item);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task PeriodicBankWriteRejectsSuppressedOrRewrittenSql(bool rewrite)
    {
        var (inventory, _, _, _, source) = await SplitFixture(true);
        source.Charges = 2;
        using (var trigger = new CharacterContext(Configuration))
        {
            string sql = Configuration.Provider == NexusForever.Database.DatabaseProvider.Sqlite
                ? rewrite
                    ? "CREATE TRIGGER alter_periodic AFTER UPDATE ON realm_bank_item BEGIN UPDATE realm_bank_item SET charges=1 WHERE id=NEW.id; END;"
                    : "CREATE TRIGGER alter_periodic BEFORE UPDATE ON realm_bank_item BEGIN SELECT RAISE(IGNORE); END;"
                : $"CREATE TRIGGER alter_periodic BEFORE UPDATE ON realm_bank_item FOR EACH ROW SET NEW.charges={(rewrite ? "1" : "OLD.charges")}";
            await trigger.Database.ExecuteSqlRawAsync(sql);
        }
        using (var save = new CharacterContext(Configuration))
        {
            inventory.Save(save); await Assert.ThrowsAnyAsync<Exception>(() => save.SaveChangesAsync());
        }
        using var read = new CharacterContext(Configuration); Assert.Equal(5u, Assert.Single(read.RealmBankItem).Charges);
        await read.Database.ExecuteSqlRawAsync("DROP TRIGGER alter_periodic");
        using (var retry = new CharacterContext(Configuration)) { inventory.Save(retry); await retry.SaveChangesAsync(); }
        read.ChangeTracker.Clear(); Assert.Equal(2u, Assert.Single(read.RealmBankItem).Charges); Assert.Empty(read.Item);
    }

    [Fact]
    public async Task FlushedPeriodicBankSaveRetainsOwnershipUntilActualRollback()
    {
        var (inventory, _, _, _, source) = await SplitFixture(true);
        source.Charges = 4;
        using (var save = new CharacterContext(Configuration))
        {
            using var transaction = await save.Database.BeginTransactionAsync();
            inventory.Save(save); await save.SaveChangesAsync(); Assert.True(source.HasPendingSave);
            source.Charges = 2; inventory.Update(10); Assert.Equal(1234u, source.ExpirationTimeLeft);
            await transaction.RollbackAsync();
        }
        Assert.False(source.HasPendingSave);
        inventory.Update(1);
        using (var retry = new CharacterContext(Configuration)) { inventory.Save(retry); await retry.SaveChangesAsync(); }
        using var read = new CharacterContext(Configuration);
        Assert.Equal(2u, Assert.Single(read.RealmBankItem).Charges); Assert.Equal(1223u, read.RealmBankItem.Single().ExpirationTimeLeft);
    }
}
