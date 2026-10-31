using Microsoft.EntityFrameworkCore;
using NexusForever.Database;
using NexusForever.Database.Character;
using NexusForever.Game.Abstract;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Entity;
using NexusForever.Game.RealmBank;
using NexusForever.Game.Static.Entity;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.Network;
using NexusForever.Network.Session;
using NexusForever.Network.World.Message.Model;
using NexusForever.Network.World.Message.Model.Shared;
using GameItem = NexusForever.Game.Entity.Item;

namespace NexusForever.Game.Tests.Entity;

public sealed partial class RealmBankItemIdentityTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task BankSplitPersistsBothStacksBeforeAnOrdinarySave(bool bankSource)
    {
        var (inventory, _, player, _, source) = await SplitFixture(bankSource);
        inventory.ItemSplit(source.Guid, SplitTarget(!bankSource), 3);
        var created = Assert.IsType<GameItem>(inventory.GetItem(205ul)); await WaitForBankMove(inventory, source, created);
        using var read = new CharacterContext(Configuration);
        Assert.Equal(14u, bankSource ? read.RealmBankItem.Single(row => row.Id == 401).StackCount : read.Item.Single(row => row.Id == 401).StackCount);
        Assert.Equal(3u, bankSource ? read.Item.Single(row => row.Id == 205).StackCount : read.RealmBankItem.Single(row => row.Id == 205).StackCount);
        Assert.Equal(10u, created.Charges); Assert.Equal(1234u, created.ExpirationTimeLeft); Assert.True(created.Soulbound); Assert.Equal(1f, created.Durability);
        Assert.Equal(10u, bankSource ? read.Item.Single(row => row.Id == 205).Charges : read.RealmBankItem.Single(row => row.Id == 205).Charges);
        Assert.Equal(ItemMicrochipIdsCodec.Serialize(source.MicrochipIds), bankSource ? read.RealmBankItem.Single(row => row.Id == 401).MicrochipIds : read.Item.Single(row => row.Id == 401).MicrochipIds);
        var messages = ((RecordingDispatchProxy<IGameSession>)(object)player.Session).GetInvocations(nameof(IGameSession.EnqueueMessageEncrypted)).Select(call => call.Arguments[0]).ToArray();
        Assert.Equal(2, messages.Length); Assert.IsType<ServerItemAdd>(messages[0]); Assert.IsType<ServerItemStackCountUpdate>(messages[1]);
        using (var normal = new CharacterContext(Configuration)) { inventory.Save(normal); await normal.SaveChangesAsync(); }
        Assert.False(inventory.IsSaveBlocked); Assert.False(inventory.RequiresAuthoritativeReload);
    }

    [Fact]
    public async Task SameBankSplitCannotCommitTheNewStackIfReducingTheSourceFails()
    {
        var (inventory, manager, player, _, source) = await SplitFixture(true);
        await SplitTrigger("realm_bank_item", "UPDATE");
        inventory.ItemSplit(source.Guid, SplitTarget(true), 3);
        var created = Assert.IsType<GameItem>(inventory.GetItem(205ul)); await WaitForBankMove(inventory, source, created);
        using var read = new CharacterContext(Configuration);
        Assert.Equal(401ul, Assert.Single(read.RealmBankItem).Id); Assert.Equal(17u, read.RealmBankItem.Single().StackCount); Assert.Empty(read.Item);
        await read.Database.ExecuteSqlRawAsync("DROP TRIGGER reject_split");
        await manager.SaveSplitAsync(source, created, player); read.ChangeTracker.Clear();
        Assert.Equal(14u, read.RealmBankItem.Single(row => row.Id == 401).StackCount); Assert.Equal(3u, read.RealmBankItem.Single(row => row.Id == 205).StackCount);
    }

    [Fact]
    public async Task FailedBankSplitCannotBePartlyCommittedByNormalInventorySave()
    {
        var (inventory, manager, player, _, source) = await SplitFixture(false);
        await SplitTrigger("realm_bank_item", "INSERT");
        inventory.ItemSplit(source.Guid, SplitTarget(true), 3);
        var created = Assert.IsType<GameItem>(inventory.GetItem(205ul)); await WaitForBankMove(inventory, source, created);
        using (var rejected = new CharacterContext(Configuration))
        {
            Assert.Throws<InvalidOperationException>(() => inventory.Save(rejected));
            await Assert.ThrowsAsync<InvalidOperationException>(() => rejected.SaveChangesAsync());
        }
        using var read = new CharacterContext(Configuration);
        Assert.Equal(17u, Assert.Single(read.Item).StackCount); Assert.Empty(read.RealmBankItem);
        await read.Database.ExecuteSqlRawAsync("DROP TRIGGER reject_split");
        await manager.SaveSplitAsync(source, created, player); read.ChangeTracker.Clear();
        Assert.Equal(14u, Assert.Single(read.Item).StackCount); Assert.Equal(3u, Assert.Single(read.RealmBankItem).StackCount);
        Assert.True(inventory.RequiresAuthoritativeReload);
    }

    [Fact]
    public async Task SameBankSplitPreservesTheTotalAndBothIndependentIdentities()
    {
        var (inventory, _, _, _, source) = await SplitFixture(true);
        inventory.ItemSplit(source.Guid, SplitTarget(true), 3); var created = Assert.IsType<GameItem>(inventory.GetItem(205ul));
        await WaitForBankMove(inventory, source, created);
        using var read = new CharacterContext(Configuration); Assert.Empty(read.Item);
        Assert.Equal(2, read.RealmBankItem.Count()); Assert.Equal(17, read.RealmBankItem.Sum(row => (int)row.StackCount));
        Assert.Equal(14u, read.RealmBankItem.Single(row => row.Id == 401).StackCount); Assert.Equal(3u, read.RealmBankItem.Single(row => row.Id == 205).StackCount);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ZeroSplitRejectsWithoutCreatingAnItemOrChangingItsSource(bool bankSource)
    {
        var (inventory, _, _, _, source) = await SplitFixture(bankSource);
        Assert.Throws<InvalidPacketValueException>(() => inventory.ItemSplit(source.Guid, SplitTarget(!bankSource), 0));
        Assert.Equal(17u, source.StackCount); Assert.Null(inventory.GetItem(205ul)); Assert.False(inventory.RequiresAuthoritativeReload);
        using var read = new CharacterContext(Configuration);
        Assert.Equal(1, read.Item.Count() + read.RealmBankItem.Count());
    }

    [Fact]
    public async Task LoadingBankSplitRetainsTheExistingSuppressedAddPacketPolicy()
    {
        var (inventory, _, player, _, source) = await SplitFixture(true);
        ((RecordingDispatchProxy<IPlayer>)(object)player).SetProperty(nameof(IPlayer.IsLoading), true);
        inventory.ItemSplit(source.Guid, SplitTarget(false), 3); var created = Assert.IsType<GameItem>(inventory.GetItem(205ul));
        await WaitForBankMove(inventory, source, created);
        var messages = ((RecordingDispatchProxy<IGameSession>)(object)player.Session).GetInvocations(nameof(IGameSession.EnqueueMessageEncrypted));
        Assert.IsType<ServerItemStackCountUpdate>(Assert.Single(messages).Arguments[0]);
        using var read = new CharacterContext(Configuration); Assert.Equal(3u, Assert.Single(read.Item).StackCount); Assert.Equal(14u, Assert.Single(read.RealmBankItem).StackCount);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task SourceUpdateFailureRollsBackTheCreatedSplitAndRetainsFreshContextRetry(bool bankSource)
    {
        var (inventory, manager, player, _, source) = await SplitFixture(bankSource);
        await SplitTrigger(bankSource ? "realm_bank_item" : "item", "UPDATE");
        inventory.ItemSplit(source.Guid, SplitTarget(!bankSource), 3); var created = Assert.IsType<GameItem>(inventory.GetItem(205ul));
        await WaitForBankMove(inventory, source, created);
        using var read = new CharacterContext(Configuration);
        Assert.Equal(1, read.Item.Count() + read.RealmBankItem.Count());
        Assert.Equal(17u, bankSource ? read.RealmBankItem.Single().StackCount : read.Item.Single().StackCount);
        await read.Database.ExecuteSqlRawAsync("DROP TRIGGER reject_split");
        await manager.SaveSplitAsync(source, created, player); read.ChangeTracker.Clear();
        Assert.Equal(2, read.Item.Count() + read.RealmBankItem.Count());
        Assert.Equal(17, read.Item.Sum(row => (int)row.StackCount) + read.RealmBankItem.Sum(row => (int)row.StackCount));
        Assert.False(source.HasPendingSave); Assert.False(created.HasPendingSave);
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public async Task SplitNotificationFailureCannotRestoreEitherStackOverAnEnrolledTransaction(bool bankSource, bool stackPacket)
    {
        var (inventory, _, player, _, source) = await SplitFixture(bankSource);
        ((RecordingDispatchProxy<IGameSession>)(object)player.Session).SetMethodHandler(nameof(IGameSession.EnqueueMessageEncrypted), args =>
        {
            if (stackPacket ? args[0] is ServerItemStackCountUpdate : args[0] is ServerItemAdd)
                throw new InvalidOperationException("Split notification failed.");
            return null;
        });
        inventory.ItemSplit(source.Guid, SplitTarget(!bankSource), 3); var created = Assert.IsType<GameItem>(inventory.GetItem(205ul));
        await WaitForBankMove(inventory, source, created);
        Assert.Equal(14u, source.StackCount); Assert.Equal(3u, created.StackCount); Assert.True(inventory.RequiresAuthoritativeReload);
        using (var blocked = new CharacterContext(Configuration)) Assert.Throws<InvalidOperationException>(() => inventory.Save(blocked));
        using var read = new CharacterContext(Configuration);
        Assert.Equal(2, read.Item.Count() + read.RealmBankItem.Count());
        Assert.Equal(17, read.Item.Sum(row => (int)row.StackCount) + read.RealmBankItem.Sum(row => (int)row.StackCount));
    }

    [Fact]
    public async Task CharacterOnlySplitKeepsBothRowsInTheExistingOrdinarySave()
    {
        var (inventory, _, _, _, source) = await SplitFixture(false);
        inventory.ItemSplit(source.Guid, SplitTarget(false), 3); var created = Assert.IsType<GameItem>(inventory.GetItem(205ul));
        Assert.False(inventory.IsSaveBlocked);
        using (var before = new CharacterContext(Configuration)) Assert.Equal(17u, Assert.Single(before.Item).StackCount);
        using (var normal = new CharacterContext(Configuration)) { inventory.Save(normal); await normal.SaveChangesAsync(); }
        using var read = new CharacterContext(Configuration);
        Assert.Equal(14u, read.Item.Single(row => row.Id == 401).StackCount); Assert.Equal(3u, read.Item.Single(row => row.Id == 205).StackCount); Assert.Empty(read.RealmBankItem);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task RewrittenSplitInsertRejectsBothRowsAndRetainsTheCreatedIdentityForRetry(bool bankSource)
    {
        var (inventory, manager, player, _, source) = await SplitFixture(bankSource);
        string table = bankSource ? "item" : "realm_bank_item";
        using (var trigger = new CharacterContext(Configuration))
            await trigger.Database.ExecuteSqlRawAsync(Configuration.Provider == DatabaseProvider.Sqlite
                ? $"CREATE TRIGGER rewrite_split AFTER INSERT ON {table} BEGIN UPDATE {table} SET stackCount=2 WHERE id=NEW.id; END;"
                : $"CREATE TRIGGER rewrite_split BEFORE INSERT ON `{table}` FOR EACH ROW SET NEW.stackCount=2");
        inventory.ItemSplit(source.Guid, SplitTarget(!bankSource), 3); var created = Assert.IsType<GameItem>(inventory.GetItem(205ul));
        await WaitForBankMove(inventory, source, created);
        using var read = new CharacterContext(Configuration); Assert.Equal(1, read.Item.Count() + read.RealmBankItem.Count());
        Assert.Equal(17u, bankSource ? read.RealmBankItem.Single().StackCount : read.Item.Single().StackCount);
        await read.Database.ExecuteSqlRawAsync("DROP TRIGGER rewrite_split");
        await manager.SaveSplitAsync(source, created, player); read.ChangeTracker.Clear();
        Assert.Equal(3u, bankSource ? read.Item.Single(row => row.Id == 205).StackCount : read.RealmBankItem.Single(row => row.Id == 205).StackCount);
    }

    [Fact]
    public async Task ExistingCharacterGuidCannotBeReusedAsANewBankSplit()
    {
        var (inventory, _, _, _, source) = await SplitFixture(false);
        using (var seed = new CharacterContext(Configuration)) { var row = CharacterRow(); row.Id = 205; row.BagIndex = 9; seed.Item.Add(row); await seed.SaveChangesAsync(); }
        inventory.ItemSplit(source.Guid, SplitTarget(true), 3); var created = Assert.IsType<GameItem>(inventory.GetItem(205ul));
        await WaitForBankMove(inventory, source, created);
        using var read = new CharacterContext(Configuration); Assert.Equal(2, read.Item.Count()); Assert.Empty(read.RealmBankItem);
        Assert.Equal(17u, read.Item.Single(row => row.Id == 401).StackCount); Assert.Equal(17u, read.Item.Single(row => row.Id == 205).StackCount);
        Assert.True(inventory.RequiresAuthoritativeReload);
    }

    [Fact]
    public async Task ReusedBankGuidCannotBecomeTheNewSplitIdentity()
    {
        var (inventory, _, _, _, source) = await SplitFixture(true);
        using (var seed = new CharacterContext(Configuration))
        { var existing = BankRow(205); existing.BagIndex = 9; seed.RealmBankItem.Add(existing); await seed.SaveChangesAsync(); }
        inventory.ItemSplit(source.Guid, SplitTarget(false), 3);
        var created = Assert.IsType<GameItem>(inventory.GetItem(205ul)); await WaitForBankMove(inventory, source, created);
        using var read = new CharacterContext(Configuration);
        Assert.Equal(17u, read.RealmBankItem.Single(row => row.Id == 401).StackCount);
        Assert.Equal(17u, read.RealmBankItem.Single(row => row.Id == 205).StackCount); Assert.Empty(read.Item);
        Assert.True(inventory.RequiresAuthoritativeReload);
    }

    private async Task<(Inventory Inventory, RealmBankManager Manager, IPlayer Player, IItemManager Items, GameItem Source)> SplitFixture(bool bankSource)
    {
        await Seed(bankSource ? null : CharacterRow(), bankSource ? BankRow(401) : null);
        var (manager, player, items) = Access();
        ((RecordingDispatchProxy<IItemManager>)(object)items).SetProperty(nameof(IItemManager.NextItemId), 205ul);
        var (inventory, carried, stored) = SwapInventory(manager, player, items, bankSource ? null : CharacterRow(), bankSource ? BankRow(401) : null);
        return (inventory, manager, player, items, bankSource ? stored : carried);
    }
    private static ItemLocation SplitTarget(bool bank) => new() { Location = bank ? InventoryLocation.RealmBank : InventoryLocation.Inventory, BagIndex = 6 };
    private async Task SplitTrigger(string table, string operation)
    {
        using var context = new CharacterContext(Configuration);
        await context.Database.ExecuteSqlRawAsync(Configuration.Provider == DatabaseProvider.Sqlite
            ? $"CREATE TRIGGER reject_split BEFORE {operation} ON {table} BEGIN SELECT RAISE(ABORT, 'split failure'); END;"
            : $"CREATE TRIGGER reject_split BEFORE {operation} ON `{table}` FOR EACH ROW SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT='split failure'");
    }
}
