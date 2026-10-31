using System.Reflection;
using Microsoft.EntityFrameworkCore;
using NexusForever.Database.Character;
using NexusForever.Database.Character.Model;
using NexusForever.Game.Abstract;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Entity;
using NexusForever.Game.Static.Entity;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.GameTable.Model;
using GameItem = NexusForever.Game.Entity.Item;

namespace NexusForever.Game.Tests.Entity;

public sealed class ItemRewardReservationTests
{
    [Theory]
    [InlineData("count")]
    [InlineData("owner")]
    [InlineData("location")]
    [InlineData("index")]
    [InlineData("charges")]
    [InlineData("expiration")]
    [InlineData("binding")]
    [InlineData("delete")]
    [InlineData("cancel-delete")]
    public void GuardRunsBeforeScalarAssignmentOrDirtyRevisionChange(string operation)
    {
        var f = new Fixture();
        var before = Snapshot(f.Item);
        Assert.True(f.Bag.TryReserveRewardMutation(f.Item, out var token));
        using (token)
        {
            Action mutation = operation switch
            {
                "count" => () => f.Item.StackCount = 5,
                "owner" => () => f.Item.CharacterId = 2,
                "location" => () => f.Item.Location = InventoryLocation.PlayerBank,
                "index" => () => f.Item.BagIndex = 3,
                "charges" => () => f.Item.Charges = 1,
                "expiration" => () => f.Item.ExpirationTimeLeft = 100,
                "binding" => () => f.Item.MakeSoulbound(),
                "delete" => () => f.Item.EnqueueDelete(true),
                "cancel-delete" => () => f.Item.EnqueueDelete(false),
                _ => throw new ArgumentException(operation)
            };
            Assert.Throws<InvalidOperationException>(mutation);
            Assert.Equal(before, Snapshot(f.Item));
            Assert.True(f.Item.HasRewardMutationReservation);
        }
        Assert.False(f.Item.HasRewardMutationReservation);
    }

    [Fact]
    public void OccupantGuardProtectsBagBeforeSlotCapabilityHasBeenAcquired()
    {
        var f = new Fixture();
        var other = f.New(12);
        f.Bag.AddItem(other, 1);
        Assert.True(f.Bag.TryReserveRewardMutation(f.Item, out var token));
        using (token)
        {
            var before = Snapshot(f.Item);
            Assert.Throws<InvalidOperationException>(() => f.Bag.RemoveItem(f.Item));
            Assert.Throws<InvalidOperationException>(() => f.Bag.MoveItem(f.Item, 2));
            Assert.Throws<InvalidOperationException>(() => f.Bag.SwapItem(other, f.Item));
            Assert.Throws<InvalidOperationException>(() => f.Bag.Resize(1));
            Assert.Throws<InvalidOperationException>(() => f.Bag.RestoreSnapshot([other, f.Item, null]));
            Assert.Equal(before, Snapshot(f.Item));
            Assert.Equal(0u, other.PreviousBagIndex);
            Assert.Equal(InventoryLocation.None, other.PreviousLocation);
            Assert.Same(f.Item, f.Bag.GetItem(0u));
            Assert.Same(other, f.Bag.GetItem(1u));
            Assert.Equal(1u, f.Bag.SlotsRemaining);
        }
    }

    [Fact]
    public void ReservedForeignSnapshotIsRejectedBeforeReplacingAnyArrayOrOtherItemPosition()
    {
        var f = new Fixture();
        var other = f.New(12);
        var destination = new Bag(InventoryLocation.PlayerBank, 2);
        destination.AddItem(other, 1);
        Assert.True(f.Bag.TryReserveRewardMutation(f.Item, out var token));
        using (token)
        {
            Assert.Throws<InvalidOperationException>(() => destination.RestoreSnapshot([other, f.Item]));
            Assert.Same(other, destination.GetItem(1u));
            Assert.Null(destination.GetItem(0u));
            Assert.Equal(1u, other.BagIndex);
            Assert.Equal(InventoryLocation.Inventory, f.Item.Location);
        }
    }

    [Fact]
    public void CleanReservedItemCannotBeSilentlySkippedByOrdinarySave()
    {
        var f = new Fixture();
        Assert.True(f.Bag.TryReserveRewardMutation(f.Item, out var token));
        using (token)
        using (var context = new CharacterContext(new DbContextOptionsBuilder<CharacterContext>().UseSqlite("Data Source=:memory:").Options))
        {
            Assert.Throws<InvalidOperationException>(() => f.Item.Save(context));
            Assert.True(context.IsSaveRejected);
            Assert.Throws<InvalidOperationException>(() => context.SaveChanges());
            Assert.False(f.Item.HasPendingSave);
        }
    }

    [Fact]
    public async Task BagRemovalAndItemReservationHaveOneWinnerWithoutAPartialMutation()
    {
        for (int round = 0; round < 100; ++round)
        {
            var f = new Fixture();
            using var start = new ManualResetEventSlim();
            GameItem.RewardMutationReservation token = null;
            bool removed = false;
            Task acquire = Task.Run(() => { start.Wait(); f.Bag.TryReserveRewardMutation(f.Item, out token); });
            Task remove = Task.Run(() =>
            {
                start.Wait();
                try { f.Bag.RemoveItem(f.Item); removed = true; }
                catch (InvalidOperationException) { /* reservation won */ }
            });
            start.Set();
            await Task.WhenAll(acquire, remove);
            using (token)
            {
                Assert.NotEqual(removed, token != null);
                if (removed)
                {
                    Assert.Null(f.Bag.GetItem(0u));
                    Assert.Equal(InventoryLocation.None, f.Item.Location);
                    Assert.Equal(3u, f.Bag.SlotsRemaining);
                }
                else
                {
                    Assert.Same(f.Item, f.Bag.GetItem(0u));
                    Assert.Equal(InventoryLocation.Inventory, f.Item.Location);
                    Assert.Equal(InventoryLocation.None, f.Item.PreviousLocation);
                    Assert.Equal(2u, f.Bag.SlotsRemaining);
                }
            }
        }
    }

    [Fact]
    public async Task PreparationPreservesEarlierOrdinaryIntentAndLaterUnrelatedDirtyFields()
    {
        var f = new Fixture();
        await using var context = await f.Context();
        f.Item.StackCount = 4;
        f.Item.Durability = .8f;
        Assert.True(f.Bag.TryReserveRewardMutation(f.Item, out var token));
        using (token)
        {
            f.Item.Durability = .6f;
            f.Item.MicrochipIds.Add(77);
            f.Item.RuneSlots.Add(new ItemRuneSlot { RuneItem2Id = 88 });
            f.Item.TouchRuneSlots();
            Assert.Equal(4u, f.Item.StackCount);
        }
        f.Item.Save(context);
        await context.SaveChangesAsync();
        ItemModel persisted = await context.Item.AsNoTracking().SingleAsync();
        Assert.Equal(4u, persisted.StackCount);
        Assert.Equal(.6f, persisted.Durability);
        Assert.Contains("77", persisted.MicrochipIds);
        Assert.Contains("88", persisted.RuneSlots);
        Assert.False(f.Item.PendingCreate);
        Assert.False(f.Item.HasPendingSave);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task PreparationRejectsUnfinishedAndUncertainSaveUnits(bool commit)
    {
        var f = new Fixture();
        await using var context = await f.Context();
        f.Item.Durability = .5f;
        f.Item.Save(context);
        Assert.False(f.Bag.TryReserveRewardMutation(f.Item, out _));
        if (commit)
        {
            await context.SaveChangesAsync();
            Assert.True(f.Bag.TryReserveRewardMutation(f.Item, out var token));
            token.Dispose();
        }
        else
        {
            // Even though no flush happened, false completion is conservative.
            await context.DisposeAsync();
            Assert.False(f.Bag.TryReserveRewardMutation(f.Item, out _));
        }
    }

    [Fact]
    public async Task FreshContextReconciliationMakesRolledBackOrdinaryIntentReservableAgain()
    {
        var f = new Fixture();
        await using var context = await f.Context();
        await using (var transaction = await context.Database.BeginTransactionAsync())
        {
            f.Item.Durability = .5f;
            f.Item.Save(context);
            await context.SaveChangesAsync();
            await transaction.RollbackAsync();
        }
        Assert.False(f.Bag.TryReserveRewardMutation(f.Item, out _));
        await using var fresh = new CharacterContext(new DbContextOptionsBuilder<CharacterContext>()
            .UseSqlite(context.Database.GetConnectionString()).Options);
        f.Item.Save(fresh);
        await fresh.SaveChangesAsync();
        Assert.Equal(.5f, (await fresh.Item.AsNoTracking().SingleAsync()).Durability);
        Assert.True(f.Bag.TryReserveRewardMutation(f.Item, out var token));
        token.Dispose();
    }

    [Fact]
    public void PreparationAllowsPendingCreateWithoutConsumingItAndOldDisposalCannotReleaseANewOwner()
    {
        var f = new Fixture();
        var item = new GameItem(42, f.Info, 3, itemManager: f.Manager);
        f.Bag.AddItem(item, 1);
        Assert.True(item.PendingCreate);
        Assert.True(f.Bag.TryReserveRewardMutation(item, out var first));
        Assert.False(f.Bag.TryReserveRewardMutation(item, out _));
        first.Dispose();
        Assert.True(f.Bag.TryReserveRewardMutation(item, out var second));
        first.Dispose();
        Assert.True(item.HasRewardMutationReservation);
        Assert.True(item.PendingCreate);
        second.Dispose();
        item.StackCount = 4;
        Assert.Equal(4u, item.StackCount);
    }

    [Theory]
    [InlineData("bank")]
    [InlineData("no-location")]
    [InlineData("no-owner")]
    [InlineData("delete")]
    [InlineData("zero-id")]
    public void UnsupportedOrDeletedItemsDoNotAcquireOwnership(string kind)
    {
        var f = new Fixture();
        var item = kind == "zero-id" ? f.New(0) : f.Item;
        if (kind == "zero-id") f.Bag.AddItem(item, 1);
        if (kind == "bank") item.Location = InventoryLocation.RealmBank;
        if (kind == "no-location") item.Location = InventoryLocation.None;
        if (kind == "no-owner") item.CharacterId = null;
        if (kind == "delete") item.EnqueueDelete(true);
        Assert.False(f.Bag.TryReserveRewardMutation(item, out var token));
        Assert.Null(token);
        Assert.False(item.HasRewardMutationReservation);
    }

    private static object Snapshot(GameItem item) => new
    {
        item.CharacterId, item.Location, item.BagIndex, item.PreviousBagIndex,
        item.PreviousLocation, item.StackCount, item.Charges, item.ExpirationTimeLeft,
        item.Soulbound, item.PendingDelete,
        Revision = typeof(GameItem).GetField("persistenceRevision", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(item)
    };

    private sealed class Fixture
    {
        public IItemInfo Info { get; }
        public IItemManager Manager { get; }
        public GameItem Item { get; }
        public Bag Bag { get; } = new(InventoryLocation.Inventory, 3);

        public Fixture()
        {
            Info = RecordingDispatchProxy<IItemInfo>.Create(out var ip);
            ip.SetProperty(nameof(IItemInfo.Id), 9011u);
            ip.SetProperty(nameof(IItemInfo.Entry), new Item2Entry { Id = 9011, MaxStackCount = 10, MaxCharges = 5 });
            Manager = RecordingDispatchProxy<IItemManager>.Create(out var mp);
            mp.SetMethodReturn(nameof(IItemManager.GetItemInfo), Info);
            mp.SetMethodReturn("get_NextItemId", 77ul);
            Item = New(11);
            Bag.AddItem(Item, 0);
        }

        public GameItem New(ulong id) => new(Model(id), Manager);

        private static ItemModel Model(ulong id) => new()
        {
            Id = id, OwnerId = 42, ItemId = 9011, Location = (ushort)InventoryLocation.Inventory,
            StackCount = 3, Charges = 2, Durability = 1, ExpirationTimeLeft = 120,
            MicrochipIds = "", RuneSlots = ""
        };

        public async Task<CharacterContext> Context()
        {
            string connectionString = "Data Source=RewardReservation-" + Guid.NewGuid().ToString("N") + ";Mode=Memory;Cache=Shared";
            var context = new CharacterContext(new DbContextOptionsBuilder<CharacterContext>().UseSqlite(connectionString).Options);
            await context.Database.OpenConnectionAsync();
            await context.Database.EnsureCreatedAsync();
            context.Character.Add(new CharacterModel { Id = 42, Name = "Reservation owner" });
            context.Item.Add(Model(11));
            await context.SaveChangesAsync();
            context.ChangeTracker.Clear();
            return context;
        }
    }
}
