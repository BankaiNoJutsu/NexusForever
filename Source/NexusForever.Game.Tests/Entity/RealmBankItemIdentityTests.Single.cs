using Microsoft.EntityFrameworkCore;
using System.Reflection;
using NexusForever.Database.Character;
using NexusForever.Game.Entity;
using NexusForever.Game.Static.Entity;
using NexusForever.Game.Tests.TestSupport;
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
    public async Task FailedSingleBankMoveRejectsTheWholeLaterNormalSave(bool bankSource)
    {
        var (inventory, manager, player, _, source) = await SplitFixture(bankSource);
        await SplitTrigger(bankSource ? "item" : "realm_bank_item", "INSERT");
        inventory.ItemMove(source, SplitTarget(!bankSource)); await WaitForBankMove(inventory, source);
        using (var rejected = new CharacterContext(Configuration))
        {
            Assert.Throws<InvalidOperationException>(() => inventory.Save(rejected));
            await Assert.ThrowsAsync<InvalidOperationException>(() => rejected.SaveChangesAsync());
        }
        using var read = new CharacterContext(Configuration); Assert.Equal(1, read.Item.Count() + read.RealmBankItem.Count());
        Assert.Equal(17u, bankSource ? read.RealmBankItem.Single().StackCount : read.Item.Single().StackCount);
        await read.Database.ExecuteSqlRawAsync("DROP TRIGGER reject_split");
        await (bankSource ? manager.SaveCharacterItemAsync(source, player) : manager.SaveItemAsync(source, player));
        read.ChangeTracker.Clear(); Assert.Equal(1, read.Item.Count() + read.RealmBankItem.Count());
        Assert.True(inventory.RequiresAuthoritativeReload);
    }

    [Fact]
    public async Task FailedBankDeletionRetainsItsOwnedTombstoneUntilACompleteRetry()
    {
        var (inventory, manager, player, _, source) = await SplitFixture(true);
        await SplitTrigger("realm_bank_item", "DELETE");
        inventory.ItemDelete(new ItemLocation { Location = source.Location, BagIndex = source.BagIndex }); await WaitForBankMove(inventory, source);
        Assert.True(source.HasUnsettledDeletion); Assert.True(inventory.RequiresAuthoritativeReload);
        using var read = new CharacterContext(Configuration); Assert.Equal(401ul, Assert.Single(read.RealmBankItem).Id); Assert.Empty(read.Item);
        await read.Database.ExecuteSqlRawAsync("DROP TRIGGER reject_split");
        await manager.SaveDeletedItemAsync(source, player); read.ChangeTracker.Clear(); Assert.Empty(read.Item); Assert.Empty(read.RealmBankItem);
        Assert.False(source.HasUnsettledDeletion); Assert.True(inventory.RequiresAuthoritativeReload);
    }

    [Fact]
    public async Task BankDeletionRefusesAChangedAccountOwner()
    {
        var (inventory, _, _, _, source) = await SplitFixture(true);
        using (var change = new CharacterContext(Configuration)) { change.RealmBankItem.Single().AccountId = 13; await change.SaveChangesAsync(); }
        inventory.ItemDelete(new ItemLocation { Location = source.Location, BagIndex = source.BagIndex }); await WaitForBankMove(inventory, source);
        Assert.True(inventory.RequiresAuthoritativeReload);
        using var read = new CharacterContext(Configuration); Assert.Equal(13u, Assert.Single(read.RealmBankItem).AccountId); Assert.Empty(read.Item);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task SingleBankMovePacketFailureCannotRestoreThePreEnrollmentLocation(bool bankSource)
    {
        var (inventory, _, player, _, source) = await SplitFixture(bankSource);
        var target = SplitTarget(!bankSource);
        ((RecordingDispatchProxy<IGameSession>)(object)player.Session).SetMethodHandler(nameof(IGameSession.EnqueueMessageEncrypted), args =>
        {
            if (args[0] is ServerItemMove) throw new InvalidOperationException("Single move notification failed after SQL enrollment.");
            return null;
        });
        inventory.ItemMove(source, target); await WaitForBankMove(inventory, source);
        Assert.Equal(target.Location, source.Location); Assert.True(inventory.RequiresAuthoritativeReload);
        using var read = new CharacterContext(Configuration);
        if (bankSource) { Assert.Equal(401ul, Assert.Single(read.Item).Id); Assert.Empty(read.RealmBankItem); }
        else { Assert.Empty(read.Item); Assert.Equal(401ul, Assert.Single(read.RealmBankItem).Id); }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task SingleBankMoveSettlesBeforeNormalSaveWithoutRecreatingItsPreviousRow(bool bankSource)
    {
        var (inventory, _, _, _, source) = await SplitFixture(bankSource);
        inventory.ItemMove(source, SplitTarget(!bankSource)); await WaitForBankMove(inventory, source);
        Assert.False(inventory.IsSaveBlocked); Assert.False(inventory.RequiresAuthoritativeReload);
        using (var normal = new CharacterContext(Configuration)) { inventory.Save(normal); await normal.SaveChangesAsync(); }
        using var read = new CharacterContext(Configuration);
        if (bankSource) { Assert.Equal(401ul, Assert.Single(read.Item).Id); Assert.Empty(read.RealmBankItem); }
        else { Assert.Empty(read.Item); Assert.Equal(401ul, Assert.Single(read.RealmBankItem).Id); }
        Assert.Equal(17u, source.StackCount); Assert.False(source.HasPendingSave);
    }

    [Fact]
    public async Task SuccessfulBankDeleteSettlesItsTombstoneBeforeNormalSave()
    {
        var (inventory, _, _, _, source) = await SplitFixture(true);
        inventory.ItemDelete(new ItemLocation { Location = source.Location, BagIndex = source.BagIndex }); await WaitForBankMove(inventory, source);
        Assert.False(source.HasUnsettledDeletion); Assert.False(inventory.IsSaveBlocked); Assert.Null(inventory.GetItem(401ul));
        using (var normal = new CharacterContext(Configuration)) { inventory.Save(normal); await normal.SaveChangesAsync(); }
        using var read = new CharacterContext(Configuration); Assert.Empty(read.Item); Assert.Empty(read.RealmBankItem);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task BankDeletionRefusesAChangedRealmOrDefinition(bool definition)
    {
        var (inventory, _, _, _, source) = await SplitFixture(true);
        using (var change = new CharacterContext(Configuration))
        { var row = change.RealmBankItem.Single(); if (definition) row.ItemId = 7002; else row.RealmId = 3; await change.SaveChangesAsync(); }
        inventory.ItemDelete(new ItemLocation { Location = source.Location, BagIndex = source.BagIndex }); await WaitForBankMove(inventory, source);
        Assert.True(inventory.RequiresAuthoritativeReload); Assert.True(source.HasUnsettledDeletion);
        using var read = new CharacterContext(Configuration); var saved = Assert.Single(read.RealmBankItem); Assert.Empty(read.Item);
        if (definition) Assert.Equal(7002u, saved.ItemId); else Assert.Equal((ushort)3, saved.RealmId);
    }

    [Fact]
    public async Task ExpiredBankItemUsesTheOwnedDeletionUnit()
    {
        var (inventory, _, _, _, source) = await SplitFixture(true);
        inventory.Update(1234); await WaitForBankMove(inventory, source);
        Assert.Null(inventory.GetItem(401ul)); Assert.False(source.HasUnsettledDeletion); Assert.False(inventory.IsSaveBlocked);
        using var read = new CharacterContext(Configuration); Assert.Empty(read.Item); Assert.Empty(read.RealmBankItem);
    }

    [Fact]
    public async Task BankSaveDefersExpiryWithoutLosingTheElapsedTime()
    {
        var (inventory, _, _, _, source) = await SplitFixture(true);
        var pending = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        typeof(Inventory).GetField("realmBankMoveSave", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(inventory, pending.Task);
        inventory.Update(1233); Assert.Equal(1234u, source.ExpirationTimeLeft); Assert.Same(source, inventory.GetItem(401ul));
        pending.SetResult(); inventory.Update(1); await WaitForBankMove(inventory, source);
        Assert.Null(inventory.GetItem(401ul)); Assert.False(source.HasUnsettledDeletion);
        using var read = new CharacterContext(Configuration); Assert.Empty(read.RealmBankItem);
    }

    [Fact]
    public async Task QuarantinedBankInventoryCannotExpireItsStoredItems()
    {
        var (inventory, _, _, _, source) = await SplitFixture(true);
        var failed = Task.FromException(new InvalidOperationException("Durability failed"));
        typeof(Inventory).GetField("realmBankMoveSave", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(inventory, failed);
        await Assert.ThrowsAsync<InvalidOperationException>(() => failed);
        inventory.Update(2000); Assert.Same(source, inventory.GetItem(401ul)); Assert.Equal(1234u, source.ExpirationTimeLeft);
        using var read = new CharacterContext(Configuration); Assert.Equal(401ul, Assert.Single(read.RealmBankItem).Id);
    }

    [Fact]
    public async Task ASecondBankSaveCannotHideAnEarlierFailure()
    {
        var (inventory, _, _, _, source) = await SplitFixture(true);
        var failed = Task.FromException(new InvalidOperationException("First operation failed"));
        ObserveBankSave(inventory, source, failed);
        var pending = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously); ObserveBankSave(inventory, source, pending.Task);
        Assert.True(inventory.RequiresAuthoritativeReload); Assert.True(inventory.IsSaveBlocked);
        inventory.Update(2000); Assert.Same(source, inventory.GetItem(401ul));
        pending.SetResult(); await WaitForBankMove(inventory, source);
        Assert.True(inventory.RequiresAuthoritativeReload);
        using (var rejected = new CharacterContext(Configuration)) Assert.Throws<InvalidOperationException>(() => inventory.Save(rejected));
        using var read = new CharacterContext(Configuration); Assert.Single(read.RealmBankItem);
    }

    [Fact]
    public async Task ACompletedLaterBankSaveCannotReleaseTheBarrierBeforeTheFirstSettles()
    {
        var (inventory, _, _, _, source) = await SplitFixture(true);
        var first = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        ObserveBankSave(inventory, source, first.Task); ObserveBankSave(inventory, source, Task.CompletedTask);
        Assert.True(inventory.IsSaveBlocked);
        using (var rejected = new CharacterContext(Configuration)) Assert.Throws<InvalidOperationException>(() => inventory.Save(rejected));
        first.SetResult(); await WaitForBankMove(inventory, source);
        Assert.False(inventory.IsSaveBlocked); Assert.False(inventory.RequiresAuthoritativeReload);
        using var normal = new CharacterContext(Configuration); inventory.Save(normal); await normal.SaveChangesAsync();
    }

    [Fact]
    public async Task BankDeleteNotificationFailureKeepsTheDeletionAndRequiresReload()
    {
        var (inventory, _, player, _, source) = await SplitFixture(true);
        ((RecordingDispatchProxy<IGameSession>)(object)player.Session).SetMethodHandler(nameof(IGameSession.EnqueueMessageEncrypted), args =>
        { if (args[0] is ServerItemDelete) throw new InvalidOperationException("Delete notification failed"); return null; });
        Assert.Throws<InvalidOperationException>(() => inventory.ItemDelete(new ItemLocation { Location = source.Location, BagIndex = source.BagIndex }));
        await WaitForBankMove(inventory, source); Assert.Null(inventory.GetItem(401ul)); Assert.True(inventory.RequiresAuthoritativeReload);
        using var read = new CharacterContext(Configuration); Assert.Empty(read.RealmBankItem); Assert.Empty(read.Item);
    }

    [Fact]
    public async Task NeverPersistedBankDeletionRequiresNoPhantomRowAndAcknowledgesItsIntent()
    {
        await Seed(); var (manager, player, items) = Access();
        var source = new GameItem(100, items.GetItemInfo(7001), 2, itemManager: items) { PreviousLocation = InventoryLocation.RealmBank };
        source.EnqueueDelete(true); await manager.SaveDeletedItemAsync(source, player);
        Assert.False(source.PendingCreate); Assert.False(source.PendingDelete); Assert.False(source.HasUnsettledDeletion); Assert.False(source.HasPendingSave);
        using var read = new CharacterContext(Configuration); Assert.Empty(read.RealmBankItem); Assert.Empty(read.Item);
    }

    private static void ObserveBankSave(Inventory inventory, GameItem source, Task save)
        => typeof(Inventory).GetMethod("ObserveRealmBankPersistence", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(inventory, [save, source, source]);
}
