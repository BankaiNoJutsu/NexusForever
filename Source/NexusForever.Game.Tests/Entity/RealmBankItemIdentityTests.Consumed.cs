using Microsoft.EntityFrameworkCore;
using System.Reflection;
using NexusForever.Database;
using NexusForever.Database.Character;
using NexusForever.Database.Character.Model;
using NexusForever.Game.Abstract;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Entity;
using NexusForever.Game.Static.Entity;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.Network.Session;
using NexusForever.Network.World.Message.Model;
using GameItem = NexusForever.Game.Entity.Item;

namespace NexusForever.Game.Tests.Entity;

public sealed partial class RealmBankItemIdentityTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ConsumedBankMergePersistsSourceRemovalAndSurvivorBeforeNormalSave(bool bankFirst)
    {
        var character = CharacterRow(); var bank = BankRow(402);
        character.StackCount = bankFirst ? 17u : 3u; bank.StackCount = bankFirst ? 3u : 17u;
        await Seed(character, bank); var (manager, player, items) = Access();
        ((RecordingDispatchProxy<IItemInfo>)(object)items.GetItemInfo(7001)).SetMethodReturn(nameof(IItemInfo.IsStackable), true);
        var (inventory, carried, stored) = SwapInventory(manager, player, items, character, bank);
        inventory.ItemMove(bankFirst ? stored : carried,
            bankFirst ? InventoryLocation.Inventory : InventoryLocation.RealmBank, bankFirst ? 0u : 3u);
        await WaitForBankMove(inventory, carried, stored);
        using var read = new CharacterContext(Configuration);
        if (bankFirst) { Assert.Equal(20u, Assert.Single(read.Item).StackCount); Assert.Empty(read.RealmBankItem); }
        else { Assert.Empty(read.Item); Assert.Equal(20u, Assert.Single(read.RealmBankItem).StackCount); }
        using (var normal = new CharacterContext(Configuration)) { inventory.Save(normal); await normal.SaveChangesAsync(); }
        read.ChangeTracker.Clear();
        Assert.False(inventory.IsSaveBlocked); Assert.False(inventory.RequiresAuthoritativeReload);
        Assert.False((bankFirst ? stored : carried).HasUnsettledDeletion);
        if (bankFirst) { Assert.Equal(20u, Assert.Single(read.Item).StackCount); Assert.Empty(read.RealmBankItem); }
        else { Assert.Empty(read.Item); Assert.Equal(20u, Assert.Single(read.RealmBankItem).StackCount); }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task FailedConsumedBankMergeCannotBePartlyCommittedByLaterNormalSave(bool bankFirst)
    {
        var character = CharacterRow(); var bank = BankRow(402);
        character.StackCount = bankFirst ? 17u : 3u; bank.StackCount = bankFirst ? 3u : 17u;
        await Seed(character, bank); var (manager, player, items) = Access();
        ((RecordingDispatchProxy<IItemInfo>)(object)items.GetItemInfo(7001)).SetMethodReturn(nameof(IItemInfo.IsStackable), true);
        var (inventory, carried, stored) = SwapInventory(manager, player, items, character, bank);
        string table = bankFirst ? "item" : "realm_bank_item";
        using (var fail = new CharacterContext(Configuration))
            await fail.Database.ExecuteSqlRawAsync(Configuration.Provider == DatabaseProvider.Sqlite
                ? $"CREATE TRIGGER reject_merge BEFORE UPDATE ON {table} BEGIN SELECT RAISE(ABORT, 'merge failure'); END;"
                : $"CREATE TRIGGER reject_merge BEFORE UPDATE ON `{table}` FOR EACH ROW SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT='merge failure'");
        inventory.ItemMove(bankFirst ? stored : carried,
            bankFirst ? InventoryLocation.Inventory : InventoryLocation.RealmBank, bankFirst ? 0u : 3u);
        await WaitForBankMove(inventory, carried, stored);
        using (var rejected = new CharacterContext(Configuration))
        {
            Assert.Throws<InvalidOperationException>(() => inventory.Save(rejected));
            await Assert.ThrowsAsync<InvalidOperationException>(() => rejected.SaveChangesAsync());
        }
        using var read = new CharacterContext(Configuration);
        Assert.Equal(character.StackCount, Assert.Single(read.Item).StackCount);
        Assert.Equal(bank.StackCount, Assert.Single(read.RealmBankItem).StackCount);
        await read.Database.ExecuteSqlRawAsync("DROP TRIGGER reject_merge");
        await manager.SaveConsumedMergeAsync(bankFirst ? stored : carried, bankFirst ? carried : stored, player);
        Assert.True(inventory.RequiresAuthoritativeReload); Assert.True(inventory.IsSaveBlocked);
        Assert.False(inventory.TryAcquireSaveLease(out _)); Assert.False(inventory.TryAcquireMutationLease(out _));
        read.ChangeTracker.Clear();
        var (freshManager, freshPlayer, freshItems) = Access();
        var (fresh, _, _) = SwapInventory(freshManager, freshPlayer, freshItems, read.Item.SingleOrDefault(), read.RealmBankItem.SingleOrDefault());
        Assert.False(fresh.RequiresAuthoritativeReload); Assert.False(fresh.IsSaveBlocked);
        using (var normal = new CharacterContext(Configuration)) { fresh.Save(normal); await normal.SaveChangesAsync(); }
        read.ChangeTracker.Clear();
        if (bankFirst) { Assert.Equal(20u, Assert.Single(read.Item).StackCount); Assert.Empty(read.RealmBankItem); }
        else { Assert.Empty(read.Item); Assert.Equal(20u, Assert.Single(read.RealmBankItem).StackCount); }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task SameBankConsumedMergeCommitsBothRowsOrRetriesBothAfterFailure(bool fail)
    {
        var first = BankRow(401); first.StackCount = 3; var second = BankRow(402); second.BagIndex = 4;
        await Seed(bank: first);
        using (var seed = new CharacterContext(Configuration)) { seed.RealmBankItem.Add(second); await seed.SaveChangesAsync(); }
        var (manager, player, items) = Access();
        ((RecordingDispatchProxy<IItemInfo>)(object)items.GetItemInfo(7001)).SetMethodReturn(nameof(IItemInfo.IsStackable), true);
        var (inventory, source, destination) = SwapInventory(manager, player, items, BankItemModel(first), second, bothBank: true);
        if (fail) await MergeTrigger("realm_bank_item", "UPDATE");
        inventory.ItemMove(source, InventoryLocation.RealmBank, 4); await WaitForBankMove(inventory, source, destination);
        using var read = new CharacterContext(Configuration); Assert.Empty(read.Item);
        if (fail)
        {
            Assert.Equal(2, read.RealmBankItem.Count()); Assert.True(source.HasUnsettledDeletion);
            Assert.Equal(3u, read.RealmBankItem.Single(row => row.Id == first.Id).StackCount);
            Assert.Equal(17u, read.RealmBankItem.Single(row => row.Id == second.Id).StackCount);
            await read.Database.ExecuteSqlRawAsync("DROP TRIGGER reject_merge");
            await manager.SaveConsumedMergeAsync(source, destination, player); read.ChangeTracker.Clear();
        }
        Assert.Equal(second.Id, Assert.Single(read.RealmBankItem).Id); Assert.Equal(20u, read.RealmBankItem.Single().StackCount);
        Assert.False(source.HasUnsettledDeletion); Assert.False(source.HasPendingSave); Assert.False(destination.HasPendingSave);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task SourceDeleteFailureCannotCommitTheConsumedMergeDestination(bool bankFirst)
    {
        var character = CharacterRow(); var bank = BankRow(402);
        character.StackCount = bankFirst ? 17u : 3u; bank.StackCount = bankFirst ? 3u : 17u;
        await Seed(character, bank); var (manager, player, items) = Access();
        ((RecordingDispatchProxy<IItemInfo>)(object)items.GetItemInfo(7001)).SetMethodReturn(nameof(IItemInfo.IsStackable), true);
        var (inventory, carried, stored) = SwapInventory(manager, player, items, character, bank);
        await MergeTrigger(bankFirst ? "realm_bank_item" : "item", "DELETE");
        inventory.ItemMove(bankFirst ? stored : carried,
            bankFirst ? InventoryLocation.Inventory : InventoryLocation.RealmBank, bankFirst ? 0u : 3u);
        await WaitForBankMove(inventory, carried, stored);
        using var read = new CharacterContext(Configuration);
        Assert.Equal(character.StackCount, Assert.Single(read.Item).StackCount); Assert.Equal(bank.StackCount, Assert.Single(read.RealmBankItem).StackCount);
        await read.Database.ExecuteSqlRawAsync("DROP TRIGGER reject_merge");
        await manager.SaveConsumedMergeAsync(bankFirst ? stored : carried, bankFirst ? carried : stored, player);
        read.ChangeTracker.Clear();
        if (bankFirst) { Assert.Equal(20u, Assert.Single(read.Item).StackCount); Assert.Empty(read.RealmBankItem); }
        else { Assert.Empty(read.Item); Assert.Equal(20u, Assert.Single(read.RealmBankItem).StackCount); }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ConsumedSourceWithChangedStorageOwnerRejectsTheWholeMerge(bool bankFirst)
    {
        var character = CharacterRow(); var bank = BankRow(402);
        character.StackCount = bankFirst ? 17u : 3u; bank.StackCount = bankFirst ? 3u : 17u;
        await Seed(character, bank); var (manager, player, items) = Access();
        var source = new GameItem(bankFirst ? BankItemModel(bank) : character, items) {
            Location = InventoryLocation.None, PreviousLocation = bankFirst ? InventoryLocation.RealmBank : InventoryLocation.Inventory };
        source.EnqueueDelete(true);
        var destination = new GameItem(bankFirst ? character : BankItemModel(bank), items) {
            Location = bankFirst ? InventoryLocation.Inventory : InventoryLocation.RealmBank, StackCount = 20 };
        using (var change = new CharacterContext(Configuration))
        {
            if (bankFirst) change.RealmBankItem.Single().AccountId = 13;
            else
            {
                change.Character.Add(new CharacterModel { Id = 101, AccountId = 12, Name = "OtherOwner", OriginalName = "OtherOwner", CreateTime = DateTime.UtcNow, PathActivatedTimestamp = DateTime.UtcNow });
                change.Item.Single().OwnerId = 101;
            }
            await change.SaveChangesAsync();
        }
        await Assert.ThrowsAsync<InvalidOperationException>(() => manager.SaveConsumedMergeAsync(source, destination, player));
        Assert.False(source.HasPendingSave); Assert.False(destination.HasPendingSave);
        using var read = new CharacterContext(Configuration);
        Assert.Equal(character.StackCount, Assert.Single(read.Item).StackCount); Assert.Equal(bank.StackCount, Assert.Single(read.RealmBankItem).StackCount);
        if (bankFirst) Assert.Equal(13u, read.RealmBankItem.Single().AccountId); else Assert.Equal(101ul, read.Item.Single().OwnerId);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task PendingBankUnitBlocksNormalSaveAndLeasesUntilItSucceedsOrRequiresReload(bool fail)
    {
        var character = CharacterRow(); var bank = BankRow(402); await Seed(character, bank);
        var (manager, player, items) = Access(); var (inventory, carried, _) = SwapInventory(manager, player, items, character, bank);
        var pending = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        typeof(Inventory).GetField("realmBankMoveSave", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(inventory, pending.Task);
        Assert.True(inventory.IsSaveBlocked); Assert.False(inventory.RequiresAuthoritativeReload);
        Assert.False(inventory.TryAcquireSaveLease(out _)); Assert.False(inventory.TryAcquireMutationLease(out _));
        Assert.NotNull(inventory.CanMoveItem(carried, InventoryLocation.RealmBank, 0));
        using (var rejected = new CharacterContext(Configuration))
        {
            var unrelated = CharacterRow(); unrelated.Id = 500; rejected.Item.Add(unrelated);
            Assert.Throws<InvalidOperationException>(() => inventory.Save(rejected));
            await Assert.ThrowsAsync<InvalidOperationException>(() => rejected.SaveChangesAsync());
        }
        if (fail)
        {
            pending.SetException(new InvalidOperationException("Failed durable unit"));
            await Assert.ThrowsAsync<InvalidOperationException>(() => pending.Task);
            Assert.True(inventory.RequiresAuthoritativeReload); Assert.True(inventory.IsSaveBlocked);
            Assert.NotNull(inventory.CanMoveItem(carried, InventoryLocation.RealmBank, 0));
        }
        else
        {
            pending.SetResult(); Assert.False(inventory.IsSaveBlocked); Assert.False(inventory.RequiresAuthoritativeReload);
            Assert.True(inventory.TryAcquireSaveLease(out var lease)); lease.Dispose();
            using var normal = new CharacterContext(Configuration); inventory.Save(normal); await normal.SaveChangesAsync();
        }
        using var read = new CharacterContext(Configuration); Assert.Equal(character.Id, Assert.Single(read.Item).Id); Assert.Single(read.RealmBankItem);
    }

    [Fact]
    public async Task NeverPersistedCharacterSourceCanBeConsumedWithoutCreatingItsOwnRow()
    {
        var bank = BankRow(402); await Seed(bank: bank); var (manager, player, items) = Access();
        ((RecordingDispatchProxy<IItemManager>)(object)items).SetProperty(nameof(IItemManager.NextItemId), 205ul);
        var source = new GameItem(100, items.GetItemInfo(7001), 3, itemManager: items) { PreviousLocation = InventoryLocation.Inventory };
        source.EnqueueDelete(true);
        var destination = new GameItem(BankItemModel(bank), items) { Location = InventoryLocation.RealmBank, BagIndex = bank.BagIndex, StackCount = 20 };
        await manager.SaveConsumedMergeAsync(source, destination, player);
        Assert.False(source.PendingCreate); Assert.False(source.PendingDelete); Assert.False(source.HasUnsettledDeletion);
        using var read = new CharacterContext(Configuration); Assert.Empty(read.Item); Assert.Equal(20u, Assert.Single(read.RealmBankItem).StackCount);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task StoredDestinationRewriteRollsBackConsumedSourceAndItsSurvivor(bool bankFirst)
    {
        var character = CharacterRow(); var bank = BankRow(402);
        character.StackCount = bankFirst ? 17u : 3u; bank.StackCount = bankFirst ? 3u : 17u;
        await Seed(character, bank); var (manager, player, items) = Access();
        var source = new GameItem(bankFirst ? BankItemModel(bank) : character, items) {
            Location = InventoryLocation.None, PreviousLocation = bankFirst ? InventoryLocation.RealmBank : InventoryLocation.Inventory };
        source.EnqueueDelete(true);
        var destination = new GameItem(bankFirst ? character : BankItemModel(bank), items) {
            Location = bankFirst ? InventoryLocation.Inventory : InventoryLocation.RealmBank, StackCount = 20 };
        string table = bankFirst ? "item" : "realm_bank_item";
        using (var trigger = new CharacterContext(Configuration))
            await trigger.Database.ExecuteSqlRawAsync(Configuration.Provider == DatabaseProvider.Sqlite
                ? $"CREATE TRIGGER rewrite_merge AFTER UPDATE ON {table} BEGIN UPDATE {table} SET stackCount=19 WHERE id=NEW.id; END;"
                : $"CREATE TRIGGER rewrite_merge BEFORE UPDATE ON `{table}` FOR EACH ROW SET NEW.stackCount=19");
        await Assert.ThrowsAsync<InvalidOperationException>(() => manager.SaveConsumedMergeAsync(source, destination, player));
        using var read = new CharacterContext(Configuration);
        Assert.Equal(character.StackCount, Assert.Single(read.Item).StackCount); Assert.Equal(bank.StackCount, Assert.Single(read.RealmBankItem).StackCount);
        await read.Database.ExecuteSqlRawAsync("DROP TRIGGER rewrite_merge");
        await manager.SaveConsumedMergeAsync(source, destination, player); read.ChangeTracker.Clear();
        if (bankFirst) { Assert.Equal(20u, Assert.Single(read.Item).StackCount); Assert.Empty(read.RealmBankItem); }
        else { Assert.Empty(read.Item); Assert.Equal(20u, Assert.Single(read.RealmBankItem).StackCount); }
    }

    private async Task MergeTrigger(string table, string operation)
    {
        using var context = new CharacterContext(Configuration);
        await context.Database.ExecuteSqlRawAsync(Configuration.Provider == DatabaseProvider.Sqlite
            ? $"CREATE TRIGGER reject_merge BEFORE {operation} ON {table} BEGIN SELECT RAISE(ABORT, 'merge failure'); END;"
            : $"CREATE TRIGGER reject_merge BEFORE {operation} ON `{table}` FOR EACH ROW SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT='merge failure'");
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task PacketFailureAfterBankEnrollmentCannotRestoreAConsumedSourceWhileSqlCommits(bool bankFirst)
    {
        var character = CharacterRow(); var bank = BankRow(402);
        character.StackCount = bankFirst ? 17u : 3u; bank.StackCount = bankFirst ? 3u : 17u;
        await Seed(character, bank); var (manager, player, items) = Access();
        ((RecordingDispatchProxy<IItemInfo>)(object)items.GetItemInfo(7001)).SetMethodReturn(nameof(IItemInfo.IsStackable), true);
        var (inventory, carried, stored) = SwapInventory(manager, player, items, character, bank);
        var source = bankFirst ? stored : carried; var destination = bankFirst ? carried : stored;
        ((RecordingDispatchProxy<IGameSession>)(object)player.Session).SetMethodHandler(nameof(IGameSession.EnqueueMessageEncrypted), args =>
        {
            if (args[0] is ServerItemSwap) throw new InvalidOperationException("Swap notification failed after SQL enrollment.");
            return null;
        });
        inventory.ItemMove(source, destination.Location, destination.BagIndex); await WaitForBankMove(inventory, carried, stored);
        Assert.Null(inventory.GetItem(source.Guid)); Assert.Equal(20u, destination.StackCount);
        Assert.True(inventory.RequiresAuthoritativeReload); Assert.True(inventory.IsSaveBlocked);
        using (var rejected = new CharacterContext(Configuration)) Assert.Throws<InvalidOperationException>(() => inventory.Save(rejected));
        using var read = new CharacterContext(Configuration);
        if (bankFirst) { Assert.Equal(20u, Assert.Single(read.Item).StackCount); Assert.Empty(read.RealmBankItem); }
        else { Assert.Empty(read.Item); Assert.Equal(20u, Assert.Single(read.RealmBankItem).StackCount); }
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public async Task PacketFailureBeforeBankEnrollmentRestoresTheMoveAndAllowsFreshRetry(bool bankFirst, bool afterRemoval)
    {
        var character = CharacterRow(); var bank = BankRow(402);
        character.StackCount = bankFirst ? 17u : 3u; bank.StackCount = bankFirst ? 3u : 17u;
        await Seed(character, bank); var (manager, player, items) = Access();
        ((RecordingDispatchProxy<IItemInfo>)(object)items.GetItemInfo(7001)).SetMethodReturn(nameof(IItemInfo.IsStackable), true);
        var (inventory, carried, stored) = SwapInventory(manager, player, items, character, bank);
        var source = bankFirst ? stored : carried; var destination = bankFirst ? carried : stored;
        ((RecordingDispatchProxy<IGameSession>)(object)player.Session).SetMethodHandler(nameof(IGameSession.EnqueueMessageEncrypted), args =>
        {
            if (afterRemoval ? args[0] is ServerItemDelete : args[0] is ServerItemStackCountUpdate)
                throw new InvalidOperationException("Notification failed before SQL enrollment.");
            return null;
        });
        inventory.ItemMove(source, destination.Location, destination.BagIndex);
        Assert.Same(source, inventory.GetItem(source.Guid)); Assert.False(source.PendingDelete);
        Assert.Equal(character.StackCount, carried.StackCount); Assert.Equal(bank.StackCount, stored.StackCount);
        Assert.False(inventory.RequiresAuthoritativeReload); Assert.False(inventory.IsSaveBlocked);
        using (var read = new CharacterContext(Configuration))
        { Assert.Equal(character.StackCount, Assert.Single(read.Item).StackCount); Assert.Equal(bank.StackCount, Assert.Single(read.RealmBankItem).StackCount); }
        ((RecordingDispatchProxy<IGameSession>)(object)player.Session).SetMethodHandler(nameof(IGameSession.EnqueueMessageEncrypted), _ => null);
        inventory.ItemMove(source, destination.Location, destination.BagIndex); await WaitForBankMove(inventory, carried, stored);
        using var final = new CharacterContext(Configuration);
        if (bankFirst) { Assert.Equal(20u, Assert.Single(final.Item).StackCount); Assert.Empty(final.RealmBankItem); }
        else { Assert.Empty(final.Item); Assert.Equal(20u, Assert.Single(final.RealmBankItem).StackCount); }
    }

    private static async Task WaitForBankMove(Inventory inventory, params GameItem[] items)
    {
        await WaitForSwapSaves(items);
        var save = (Task)typeof(Inventory).GetField("realmBankMoveSave", BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(inventory);
        if (save == null) return;
        try { await save.WaitAsync(TimeSpan.FromSeconds(15)); }
        catch when (save.IsFaulted || save.IsCanceled) { /* Failure cases assert rejection and unchanged SQL below. */ }
    }
}
