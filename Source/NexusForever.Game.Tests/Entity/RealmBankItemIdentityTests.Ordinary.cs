using System.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using NexusForever.Database;
using NexusForever.Database.Character;
using NexusForever.Database.Character.Model;
using NexusForever.Game.Abstract;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Static.Entity;
using NexusForever.Game.Tests.TestSupport;
using GameItem = NexusForever.Game.Entity.Item;

namespace NexusForever.Game.Tests.Entity;

public sealed partial class RealmBankItemIdentityTests
{
    [Theory]
    [InlineData("create")] [InlineData("update")] [InlineData("delete")]
    public Task OrdinaryItemSuppressedWritesRetainIntentForAFreshSave(string operation)
        => RecoverOrdinarySuppression(operation, false, 0);

    [Theory]
    [InlineData("create")] [InlineData("update")] [InlineData("delete")]
    public Task OrdinaryItemSynchronousSuppressedWritesRetainIntentForAFreshSave(string operation)
        => RecoverOrdinarySuppression(operation, true, 0);

    [Theory]
    [InlineData("create")] [InlineData("update")] [InlineData("delete")]
    public Task OrdinaryItemAdvertisedAffectedRowsCannotAcknowledgeMissingSql(string operation)
        => RecoverOrdinarySuppression(operation, false, 1);

    private async Task RecoverOrdinarySuppression(string operation, bool synchronous, int advertisedCount)
    {
        var item = await OrdinaryItem(operation);
        using (var suppressed = OrdinarySuppressedContext(advertisedCount))
        {
            item.Save(suppressed);
            try { if (synchronous) suppressed.SaveChanges(); else await suppressed.SaveChangesAsync(); }
            catch (InvalidOperationException) when (suppressed.IsSaveRejected) { }
        }
        using (var before = new CharacterContext(Configuration))
        {
            if (operation == "create") Assert.Empty(before.Item);
            else Assert.Equal(17u, Assert.Single(before.Item).StackCount);
        }
        using (var retry = new CharacterContext(Configuration))
        { item.Save(retry); if (synchronous) retry.SaveChanges(); else await retry.SaveChangesAsync(); }
        using var read = new CharacterContext(Configuration);
        if (operation == "delete") Assert.Empty(read.Item);
        else Assert.Equal(5u, Assert.Single(read.Item).StackCount);
    }

    [Fact]
    public async Task OrdinaryItemZeroWriteCanSettleWhenTheCompleteTargetAlreadyExists()
    {
        var item = await OrdinaryItem("loaded"); item.StackCount = 5; item.StackCount = 17;
        using (var noOp = OrdinarySuppressedContext())
        {
            item.Save(noOp); Assert.Equal(0, await noOp.SaveChangesAsync());
            Assert.False(noOp.IsSaveRejected);
        }
        using var retry = new CharacterContext(Configuration); item.Save(retry);
        Assert.Empty(retry.ChangeTracker.Entries<ItemModel>());
        Assert.Equal(0, await retry.SaveChangesAsync());
        using var read = new CharacterContext(Configuration); Assert.Equal(17u, Assert.Single(read.Item).StackCount);
    }

    [Fact]
    public async Task OrdinaryItemCommitDoesNotConsumeLaterLiveStackIntent()
    {
        var item = await OrdinaryItem("update");
        using (var first = new CharacterContext(Configuration))
        {
            using var transaction = await first.Database.BeginTransactionAsync(IsolationLevel.Serializable);
            item.Save(first); await first.SaveChangesAsync(); item.StackCount = 9; await transaction.CommitAsync();
        }
        using (var before = new CharacterContext(Configuration)) Assert.Equal(5u, Assert.Single(before.Item).StackCount);
        using (var retry = new CharacterContext(Configuration)) { item.Save(retry); await retry.SaveChangesAsync(); }
        using var read = new CharacterContext(Configuration); Assert.Equal(9u, Assert.Single(read.Item).StackCount);
    }

    [Theory]
    [InlineData("update")] [InlineData("delete")]
    public async Task OrdinaryItemCannotModifyARowTransferredAfterEnrollment(string operation)
    {
        var item = await OrdinaryItem(operation);
        using var context = new CharacterContext(Configuration); item.Save(context);
        using (var transfer = new CharacterContext(Configuration))
        {
            var row = transfer.Item.Single(); row.OwnerId = null; row.Location = (ushort)InventoryLocation.None;
            await transfer.SaveChangesAsync();
        }
        await Assert.ThrowsAsync<InvalidOperationException>(() => context.SaveChangesAsync());
        Assert.True(context.IsSaveRejected);
        using var read = new CharacterContext(Configuration); var stored = Assert.Single(read.Item);
        Assert.Null(stored.OwnerId); Assert.Equal((ushort)InventoryLocation.None, stored.Location); Assert.Equal(17u, stored.StackCount);
    }

    private async Task<GameItem> OrdinaryItem(string operation)
    {
        var baseline = CharacterRow(); baseline.MicrochipIds = ""; baseline.RuneSlots = "";
        await Seed(character: operation == "create" ? null : baseline);
        var (_, player, items) = Access();
        ((RecordingDispatchProxy<IItemManager>)(object)items).SetProperty(nameof(IItemManager.NextItemId), 401ul);
        var item = operation == "create"
            ? new GameItem(player.CharacterId, items.GetItemInfo(7001), 5, itemManager: items)
            : new GameItem(baseline, itemManager: items);
        if (operation == "create") item.Location = InventoryLocation.Inventory;
        if (operation == "update") item.StackCount = 5;
        if (operation == "delete") item.EnqueueDelete(true);
        return item;
    }
    private CharacterContext OrdinarySuppressedContext(int advertisedCount = 0)
    {
        var options = new DbContextOptionsBuilder<CharacterContext>(); options.UseConfiguration(Configuration);
        options.AddInterceptors(new SuppressOrdinaryItemFlush(advertisedCount)); return new(options.Options);
    }
    private sealed class SuppressOrdinaryItemFlush(int advertisedCount) : SaveChangesInterceptor
    {
        public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
            => InterceptionResult<int>.SuppressWithResult(advertisedCount);
        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData,
            InterceptionResult<int> result, CancellationToken cancellationToken = default)
            => ValueTask.FromResult(InterceptionResult<int>.SuppressWithResult(advertisedCount));
    }
}
