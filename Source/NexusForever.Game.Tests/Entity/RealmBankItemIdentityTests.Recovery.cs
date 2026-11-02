using Microsoft.EntityFrameworkCore;
using NexusForever.Database.Character;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract;
using NexusForever.Game.Entity;
using NexusForever.Game.RealmBank;
using NexusForever.Game.Static.Entity;
using NexusForever.Game.Tests.TestSupport;
using GameItem = NexusForever.Game.Entity.Item;

namespace NexusForever.Game.Tests.Entity;

public sealed partial class RealmBankItemIdentityTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task FrozenBankCommandRetainsLaterScalarsAndCodecsForTheNextSave(bool withdrawal)
    {
        var (_, _, player, _, item) = await SplitFixture(withdrawal);
        item.Location = withdrawal ? InventoryLocation.Inventory : InventoryLocation.RealmBank;
        item.BagIndex = 6; item.StackCount = 14;
        var command = new RealmBankManager.BankSaveCommand(Database().CreateContext, [item], player);
        item.StackCount = 12; item.Charges = 2; item.MicrochipIds.Add(99);
        await command.ExecuteAsync();
        using (var read = new CharacterContext(Configuration))
        {
            Assert.Equal(14u, withdrawal ? Assert.Single(read.Item).StackCount : Assert.Single(read.RealmBankItem).StackCount);
            Assert.Equal(5u, withdrawal ? read.Item.Single().Charges : read.RealmBankItem.Single().Charges);
            Assert.DoesNotContain("99", withdrawal ? read.Item.Single().MicrochipIds : read.RealmBankItem.Single().MicrochipIds);
        }
        Assert.Equal(12u, item.StackCount); Assert.Equal(2u, item.Charges);
        await new RealmBankManager.BankSaveCommand(Database().CreateContext, [item], player).ExecuteAsync();
        using var final = new CharacterContext(Configuration);
        Assert.Equal(12u, withdrawal ? final.Item.Single().StackCount : final.RealmBankItem.Single().StackCount);
        Assert.Equal(2u, withdrawal ? final.Item.Single().Charges : final.RealmBankItem.Single().Charges);
        Assert.Contains("99", withdrawal ? final.Item.Single().MicrochipIds : final.RealmBankItem.Single().MicrochipIds);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task SplitRetryAfterLostAcknowledgementKeepsItsIdentityAndDoesNotReduceAgain(bool bankSource)
    {
        var (_, _, player, items, source) = await SplitFixture(bankSource);
        var created = PrepareCapturedSplit(source, items, !bankSource);
        int enrollments = 0, attempts = 0;
        var command = new RealmBankManager.BankSaveCommand(Database().CreateContext, [source, created], player,
            context => { ++enrollments; created.ValidateSplitCreation(context); });
        await command.ExecuteAsync(async attempt =>
        {
            try { ++attempts; await attempt(); throw new IOException("Lost acknowledgement after the real commit."); }
            catch (IOException) { ++attempts; await attempt(); }
        });
        Assert.Equal(2, attempts); Assert.Equal(1, enrollments); Assert.Equal(205ul, created.Guid);
        Assert.False(created.PendingCreate); Assert.False(source.HasPendingSave); Assert.False(created.HasPendingSave);
        using var read = new CharacterContext(Configuration);
        Assert.Equal(2, read.Item.Count() + read.RealmBankItem.Count());
        Assert.Equal(17, read.Item.Sum(row => (int)row.StackCount) + read.RealmBankItem.Sum(row => (int)row.StackCount));
        Assert.Equal(3u, bankSource ? read.Item.Single(row => row.Id == 205).StackCount : read.RealmBankItem.Single(row => row.Id == 205).StackCount);
        await Assert.ThrowsAsync<InvalidOperationException>(() => command.ExecuteAsync());
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task FreshContextRetryUsesTheCapturedSplitInsteadOfLaterLiveCounts(bool bankSource)
    {
        var (_, manager, player, items, source) = await SplitFixture(bankSource);
        var created = PrepareCapturedSplit(source, items, !bankSource);
        var command = new RealmBankManager.BankSaveCommand(Database().CreateContext, [source, created], player, created.ValidateSplitCreation);
        await SplitTrigger(bankSource ? "item" : "realm_bank_item", "INSERT");
        int attempts = 0;
        await command.ExecuteAsync(async attempt =>
        {
            try { ++attempts; await attempt(); }
            catch (DbUpdateException)
            {
                using var drop = new CharacterContext(Configuration); await drop.Database.ExecuteSqlRawAsync("DROP TRIGGER reject_split");
                source.StackCount = 12; created.StackCount = 2;
                ++attempts; await attempt();
            }
        });
        Assert.Equal(2, attempts); Assert.Equal(12u, source.StackCount); Assert.Equal(2u, created.StackCount);
        using (var read = new CharacterContext(Configuration))
            Assert.Equal(17, read.Item.Sum(row => (int)row.StackCount) + read.RealmBankItem.Sum(row => (int)row.StackCount));
        await manager.SaveInventoryOperationAsync([source, created], [], player);
        using var final = new CharacterContext(Configuration);
        Assert.Equal(14, final.Item.Sum(row => (int)row.StackCount) + final.RealmBankItem.Sum(row => (int)row.StackCount));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task FrozenDeletionAndWithdrawalRetainTheOppositeLaterIntent(bool revive)
    {
        var (_, _, player, _, item) = await SplitFixture(true);
        item.PreviousLocation = InventoryLocation.RealmBank;
        item.Location = revive ? InventoryLocation.None : InventoryLocation.Inventory;
        item.EnqueueDelete(revive);
        var command = new RealmBankManager.BankSaveCommand(Database().CreateContext, [item], player);
        item.Location = revive ? InventoryLocation.Inventory : InventoryLocation.None; item.EnqueueDelete(!revive);
        await command.ExecuteAsync();
        using (var read = new CharacterContext(Configuration))
        {
            Assert.Empty(read.RealmBankItem);
            Assert.Equal(revive ? 0 : 1, read.Item.Count());
        }
        using (var next = new CharacterContext(Configuration)) { item.Save(next); await next.SaveChangesAsync(); }
        using var final = new CharacterContext(Configuration); Assert.Equal(revive ? 1 : 0, final.Item.Count());
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task BankCommandRefusesTransferredOrDeletedCharacterOwnership(bool deleted)
    {
        var (_, _, player, _, source) = await SplitFixture(true);
        source.Location = InventoryLocation.Inventory;
        var command = new RealmBankManager.BankSaveCommand(Database().CreateContext, [source], player);
        using (var change = new CharacterContext(Configuration))
        {
            var character = change.Character.Single();
            if (deleted) character.DeleteTime = DateTime.UtcNow; else character.AccountId = 13;
            await change.SaveChangesAsync();
        }
        await Assert.ThrowsAsync<InvalidOperationException>(() => command.ExecuteAsync());
        using var read = new CharacterContext(Configuration); Assert.Empty(read.Item); Assert.Equal(401ul, Assert.Single(read.RealmBankItem).Id);
    }

    [Fact]
    public async Task RetryRefusesACharacterTransferredAfterItsFirstFailedAttempt()
    {
        var (_, _, player, _, source) = await SplitFixture(false);
        source.Location = InventoryLocation.RealmBank;
        var command = new RealmBankManager.BankSaveCommand(Database().CreateContext, [source], player);
        await SplitTrigger("realm_bank_item", "INSERT");
        await Assert.ThrowsAsync<InvalidOperationException>(() => command.ExecuteAsync(async attempt =>
        {
            try { await attempt(); }
            catch (DbUpdateException)
            {
                using var change = new CharacterContext(Configuration);
                await change.Database.ExecuteSqlRawAsync("DROP TRIGGER reject_split");
                change.Character.Single().AccountId = 13; await change.SaveChangesAsync();
                await attempt();
            }
        }));
        using var read = new CharacterContext(Configuration); Assert.Equal(401ul, Assert.Single(read.Item).Id); Assert.Empty(read.RealmBankItem);
    }

    [Fact]
    public async Task BankCommandDoesNotDisposeABadFactorysAlreadyTrackedContext()
    {
        var (_, _, player, _, source) = await SplitFixture(true);
        using var foreign = new CharacterContext(Configuration); _ = foreign.Character.Single();
        var command = new RealmBankManager.BankSaveCommand(() => foreign, [source], player);
        await Assert.ThrowsAsync<InvalidOperationException>(() => command.ExecuteAsync());
        Assert.Single(foreign.Character); Assert.Single(foreign.RealmBankItem);
    }

    [Fact]
    public async Task LaterBankDeletionSurvivesAnEarlierCapturedDeposit()
    {
        var (_, manager, player, _, item) = await SplitFixture(false);
        item.Location = InventoryLocation.RealmBank; item.BagIndex = 6;
        var command = new RealmBankManager.BankSaveCommand(Database().CreateContext, [item], player);
        item.PreviousLocation = InventoryLocation.RealmBank; item.Location = InventoryLocation.None; item.EnqueueDelete(true);
        await command.ExecuteAsync();
        Assert.True(item.PendingDelete); Assert.True(item.HasUnsettledDeletion);
        using (var read = new CharacterContext(Configuration)) { Assert.Empty(read.Item); Assert.Single(read.RealmBankItem); }
        await manager.SaveDeletedItemAsync(item, player);
        using var final = new CharacterContext(Configuration); Assert.Empty(final.Item); Assert.Empty(final.RealmBankItem);
    }

    private static GameItem PrepareCapturedSplit(GameItem source, IItemManager items, bool bankDestination)
    {
        source.StackCount = 14;
        var created = new GameItem(100, items.GetItemInfo(7001), 3, itemManager: items)
        {
            Location = bankDestination ? InventoryLocation.RealmBank : InventoryLocation.Inventory,
            BagIndex = 6, ExpirationTimeLeft = source.ExpirationTimeLeft
        };
        created.MakeSoulbound(); _ = created.Build(); return created;
    }
}
