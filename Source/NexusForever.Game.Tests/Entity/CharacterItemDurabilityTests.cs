using System.Collections.Immutable;
using System.Reflection;
using Microsoft.EntityFrameworkCore;
using NexusForever.Database.Character;
using NexusForever.Database.Character.Model;
using NexusForever.Game.Abstract;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Entity;
using NexusForever.Game.Static.Crafting;
using NexusForever.Game.Static.Entity;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.GameTable.Model;
using NexusForever.Network.Session;
using NexusForever.Network.World.Message.Model.Shared;
using GameItem = NexusForever.Game.Entity.Item;

namespace NexusForever.Game.Tests.Entity;

/// <summary>
/// Persistence controls use a separate physical SQLite file per test and reload through
/// independent contexts. Foreign keys are disabled only in this synthetic item fixture;
/// no runtime database or global connection pool is used.
/// </summary>
public sealed class CharacterItemDurabilityTests : IDisposable
{
    private const ulong OwnerId = 9421;
    private const uint ItemId = 81917;
    private readonly string directory = Path.Combine(Path.GetTempPath(), "NexusForever-CharacterItem-" + Guid.NewGuid().ToString("N"));
    private readonly DbContextOptions<CharacterContext> options;
    private readonly IItemInfo info;
    private readonly IItemManager itemManager;

    public CharacterItemDurabilityTests() : this(null)
    {
    }

    internal CharacterItemDurabilityTests(DbContextOptions<CharacterContext> providerOptions)
    {
        Directory.CreateDirectory(directory);
        options = providerOptions ?? new DbContextOptionsBuilder<CharacterContext>()
            .UseSqlite($"Data Source={Path.Combine(directory, "items.sqlite")};Foreign Keys=False;Pooling=False")
            .Options;
        if (providerOptions == null)
        {
            using CharacterContext context = Context();
            context.Database.EnsureCreated();
        }

        info = RecordingDispatchProxy<IItemInfo>.Create(out var infoProxy);
        infoProxy.SetProperty(nameof(IItemInfo.Id), ItemId);
        infoProxy.SetProperty(nameof(IItemInfo.Entry), new Item2Entry { Id = ItemId, MaxStackCount = 50 });
        infoProxy.SetProperty(nameof(IItemInfo.Properties), ImmutableDictionary<Property, float>.Empty);
        infoProxy.SetMethodReturn(nameof(IItemInfo.IsStackable), true);
        infoProxy.SetMethodReturn(nameof(IItemInfo.IsEquippable), false);
        infoProxy.SetMethodReturn(nameof(IItemInfo.IsEquippableBag), false);
        itemManager = RecordingDispatchProxy<IItemManager>.Create(out var managerProxy);
        ulong nextId = 9421000;
        managerProxy.SetMethodReturnFactory("get_NextItemId", () => nextId++);
        managerProxy.SetMethodReturn(nameof(IItemManager.GetItemInfo), info);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Create_OuterRollbackOrTransactionDisposal_RetriesInFreshContext(bool disposeTransaction)
    {
        var item = NewItem(3);
        await using (CharacterContext context = Context())
        {
            var transaction = await context.Database.BeginTransactionAsync();
            item.Save(context);
            await context.SaveChangesAsync();
            if (disposeTransaction)
                await transaction.DisposeAsync();
            else
            {
                await transaction.RollbackAsync();
                await transaction.DisposeAsync();
            }
        }

        Assert.Null(await Read(item.Guid));
        await Save(item);
        ItemModel row = Assert.IsType<ItemModel>(await Read(item.Guid));
        Assert.Equal(3u, row.StackCount);
        Assert.Equal(OwnerId, row.OwnerId);
        Assert.False(item.PendingCreate);
    }

    [Fact]
    public async Task Create_ContextDisposedBeforeSql_RetainsCreateForFreshRetry()
    {
        var item = NewItem(4);
        await using (CharacterContext abandoned = Context())
            item.Save(abandoned);

        Assert.Null(await Read(item.Guid));
        await Save(item);
        Assert.Equal(4u, (await Read(item.Guid))?.StackCount);
    }

    [Fact]
    public async Task Update_OuterRollback_RetainsChangedFieldsForFreshRetry()
    {
        var item = await PersistedItem(5);
        item.StackCount = 9;
        item.MakeSoulbound();
        item.ExpirationTimeLeft = 234;
        await using (CharacterContext context = Context())
        {
            await using var transaction = await context.Database.BeginTransactionAsync();
            item.Save(context);
            await context.SaveChangesAsync();
            await transaction.RollbackAsync();
        }

        ItemModel beforeRetry = Assert.IsType<ItemModel>(await Read(item.Guid));
        Assert.Equal(5u, beforeRetry.StackCount);
        Assert.False(beforeRetry.Soulbound);
        await Save(item);
        ItemModel row = Assert.IsType<ItemModel>(await Read(item.Guid));
        Assert.Equal(9u, row.StackCount);
        Assert.True(row.Soulbound);
        Assert.Equal(234u, row.ExpirationTimeLeft);
    }

    [Fact]
    public async Task Delete_OuterRollback_RetainsDeleteForFreshRetry()
    {
        var item = await PersistedItem(2);
        item.EnqueueDelete(true);
        await using (CharacterContext context = Context())
        {
            await using var transaction = await context.Database.BeginTransactionAsync();
            item.Save(context);
            await context.SaveChangesAsync();
            await transaction.RollbackAsync();
        }

        Assert.NotNull(await Read(item.Guid));
        await Save(item);
        Assert.Null(await Read(item.Guid));
    }

    [Fact]
    public async Task LaterMutationAfterSql_OlderCommitDoesNotAcknowledgeNewerFields()
    {
        var item = await PersistedItem(2);
        item.StackCount = 6;
        await using (CharacterContext context = Context())
        {
            await using var transaction = await context.Database.BeginTransactionAsync();
            item.Save(context);
            await context.SaveChangesAsync();
            item.StackCount = 8;
            item.MakeSoulbound();
            await transaction.CommitAsync();
        }

        ItemModel captured = Assert.IsType<ItemModel>(await Read(item.Guid));
        Assert.Equal(6u, captured.StackCount);
        Assert.False(captured.Soulbound);
        await Save(item);
        ItemModel latest = Assert.IsType<ItemModel>(await Read(item.Guid));
        Assert.Equal(8u, latest.StackCount);
        Assert.True(latest.Soulbound);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task SameContextRestageBeforeSql_UsesOneTrackedRowAndLatestValues(bool creating)
    {
        var item = creating ? NewItem(2) : await PersistedItem(2);
        item.StackCount = 3;
        await using (CharacterContext context = Context())
        {
            item.Save(context);
            item.StackCount = 7;
            item.Save(context);
            Assert.Single(context.ChangeTracker.Entries<ItemModel>());
            await context.SaveChangesAsync();
        }

        Assert.Equal(7u, (await Read(item.Guid))?.StackCount);
    }

    [Fact]
    public async Task ConcurrentContexts_SameItemCannotSilentlyOmitItsWrite()
    {
        var item = await PersistedItem(2);
        item.StackCount = 4;
        await using (CharacterContext owner = Context())
        {
            item.Save(owner);
            await using (CharacterContext contender = Context())
                Assert.ThrowsAny<InvalidOperationException>(() => item.Save(contender));
            await owner.SaveChangesAsync();
        }

        Assert.Equal(4u, (await Read(item.Guid))?.StackCount);
    }

    [Fact]
    public async Task InventoryDeletion_RolledBackFlushRetainsRemovedItemTombstone()
    {
        var item = await PersistedItem(2);
        Inventory inventory = InventoryFor(item);
        Assert.Same(item, inventory.ItemDelete(new ItemLocation { Location = InventoryLocation.Inventory, BagIndex = 0 }));
        await using (CharacterContext context = Context())
        {
            await using var transaction = await context.Database.BeginTransactionAsync();
            inventory.Save(context);
            await context.SaveChangesAsync();
            await transaction.RollbackAsync();
        }

        Assert.NotNull(await Read(item.Guid));
        await Save(inventory);
        Assert.Null(await Read(item.Guid));
    }

    [Fact]
    public async Task CreateRemovedWhileInsertPending_DeletionSurvivesRollbackAndFreshRetry()
    {
        var item = NewItem(2);
        Inventory inventory = InventoryFor(item);
        await using (CharacterContext creating = Context())
        {
            await using var transaction = await creating.Database.BeginTransactionAsync();
            inventory.Save(creating);
            await creating.SaveChangesAsync();
            Assert.Same(item, inventory.ItemDelete(new ItemLocation { Location = InventoryLocation.Inventory, BagIndex = 0 }));
            await transaction.CommitAsync();
        }

        Assert.NotNull(await Read(item.Guid));
        await using (CharacterContext deleting = Context())
        {
            await using var transaction = await deleting.Database.BeginTransactionAsync();
            inventory.Save(deleting);
            await deleting.SaveChangesAsync();
            await transaction.RollbackAsync();
        }

        Assert.NotNull(await Read(item.Guid));
        await Save(inventory);
        Assert.Null(await Read(item.Guid));
    }

    [Theory]
    [InlineData("create", false)]
    [InlineData("create", true)]
    [InlineData("update", false)]
    [InlineData("update", true)]
    [InlineData("delete", false)]
    [InlineData("delete", true)]
    public async Task SavepointRollbackThenOuterCommit_ReconcilesActualRowBeforeFreshRetry(string operation, bool afterWrites)
    {
        var item = operation == "create" ? NewItem(3) : await PersistedItem(2);
        if (operation == "update") item.StackCount = 8;
        if (operation == "delete") item.EnqueueDelete(true);
        await using (CharacterContext context = Context())
        {
            await using var transaction = await context.Database.BeginTransactionAsync();
            if (!afterWrites) await transaction.CreateSavepointAsync("item_boundary");
            item.Save(context);
            await context.SaveChangesAsync();
            if (afterWrites) await transaction.CreateSavepointAsync("item_boundary");
            await transaction.RollbackToSavepointAsync("item_boundary");
            await transaction.CommitAsync();
        }

        ItemModel beforeRetry = await Read(item.Guid);
        if (operation == "create")
            Assert.Equal(afterWrites, beforeRetry != null);
        else if (operation == "delete")
            Assert.Equal(!afterWrites, beforeRetry != null);
        else
            Assert.Equal(afterWrites ? 8u : 2u, Assert.IsType<ItemModel>(beforeRetry).StackCount);

        await Save(item);
        if (operation == "delete") Assert.Null(await Read(item.Guid));
        else Assert.Equal(operation == "create" ? 3u : 8u, (await Read(item.Guid))?.StackCount);
    }

    [Theory]
    [InlineData("create")]
    [InlineData("update")]
    [InlineData("delete")]
    public async Task ImplicitSqlFailure_RetainsIntentForFreshContextRetry(string operation)
    {
        var item = operation == "create" ? NewItem(3) : await PersistedItem(2);
        if (operation == "update") item.StackCount = 7;
        if (operation == "delete") item.EnqueueDelete(true);
        string trigger = operation switch
        {
            "create" => "CREATE TRIGGER fail_item BEFORE INSERT ON item BEGIN SELECT RAISE(ABORT, 'owned fixture failure'); END;",
            "update" => "CREATE TRIGGER fail_item BEFORE UPDATE ON item BEGIN SELECT RAISE(ABORT, 'owned fixture failure'); END;",
            _ => "CREATE TRIGGER fail_item BEFORE DELETE ON item BEGIN SELECT RAISE(ABORT, 'owned fixture failure'); END;"
        };
        await using (CharacterContext setup = Context())
            await setup.Database.ExecuteSqlRawAsync(trigger);
        await using (CharacterContext failed = Context())
        {
            item.Save(failed);
            await Assert.ThrowsAsync<DbUpdateException>(() => failed.SaveChangesAsync());
        }
        await using (CharacterContext setup = Context())
            await setup.Database.ExecuteSqlRawAsync("DROP TRIGGER fail_item;");

        if (operation == "create") Assert.Null(await Read(item.Guid));
        else Assert.Equal(2u, (await Read(item.Guid))?.StackCount);
        await Save(item);
        if (operation == "delete") Assert.Null(await Read(item.Guid));
        else Assert.Equal(operation == "create" ? 3u : 7u, (await Read(item.Guid))?.StackCount);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task MutationAfterCapture_SameFieldAbaOrDifferentFieldSurvivesRollback(bool differentField)
    {
        var item = await PersistedItem(2);
        item.StackCount = 6;
        await using (CharacterContext context = Context())
        {
            await using var transaction = await context.Database.BeginTransactionAsync();
            item.Save(context);
            await context.SaveChangesAsync();
            if (differentField) item.ExpirationTimeLeft = 123;
            else { item.StackCount = 9; item.StackCount = 6; }
            await transaction.RollbackAsync();
        }

        Assert.Equal(2u, (await Read(item.Guid))?.StackCount);
        await Save(item);
        ItemModel row = Assert.IsType<ItemModel>(await Read(item.Guid));
        Assert.Equal(6u, row.StackCount);
        Assert.Equal(differentField ? 123u : 0u, row.ExpirationTimeLeft);
    }

    [Theory]
    [InlineData("before-sql")]
    [InlineData("after-flush")]
    [InlineData("after-commit")]
    public async Task DeleteCancelAndReadd_PreservesLiveItemAcrossDeleteBoundaries(string phase)
    {
        var item = await PersistedItem(2);
        Inventory inventory = InventoryFor(item);
        inventory.ItemDelete(new ItemLocation { Location = InventoryLocation.Inventory, BagIndex = 0 });
        await using (CharacterContext deleting = Context())
        {
            await using var transaction = await deleting.Database.BeginTransactionAsync();
            inventory.Save(deleting);
            if (phase != "before-sql") await deleting.SaveChangesAsync();
            if (phase == "after-commit") await transaction.CommitAsync();

            // This is the real vendor buyback route, reusing the sold object and GUID.
            inventory.AddItem(item, InventoryLocation.Inventory);
            if (phase != "after-commit")
            {
                inventory.Save(deleting);
                await deleting.SaveChangesAsync();
                await transaction.CommitAsync();
            }
        }
        await Save(inventory);
        ItemModel row = Assert.IsType<ItemModel>(await Read(item.Guid));
        Assert.Equal(OwnerId, row.OwnerId);
        Assert.Equal(2u, row.StackCount);
        Assert.Equal((ushort)InventoryLocation.Inventory, row.Location);
        Assert.False(item.PendingDelete);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task NewItemRemovedWhileInsertPending_ConvergesToNoRow(bool commitInsert)
    {
        var item = NewItem(2);
        Inventory inventory = InventoryFor(item);
        await using (CharacterContext creating = Context())
        {
            await using var transaction = await creating.Database.BeginTransactionAsync();
            inventory.Save(creating);
            await creating.SaveChangesAsync();
            inventory.ItemDelete(new ItemLocation { Location = InventoryLocation.Inventory, BagIndex = 0 });
            if (commitInsert) await transaction.CommitAsync();
            else await transaction.RollbackAsync();
        }
        Assert.Equal(commitInsert, await Read(item.Guid) != null);
        await Save(inventory);
        Assert.Null(await Read(item.Guid));
        // A second clean pass must not recreate a canceled new item or repeat DELETE.
        await Save(inventory);
        Assert.Null(await Read(item.Guid));
    }

    [Fact]
    public async Task PretrackedItemModel_IsReusedAndUnrelatedDirtyColumnIsPreserved()
    {
        var item = await PersistedItem(2);
        item.StackCount = 7;
        await using (CharacterContext context = Context())
        {
            ItemModel pretracked = await context.Item.SingleAsync(row => row.Id == item.Guid);
            pretracked.ExpirationTimeLeft = 321;
            item.Save(context);
            Assert.Same(pretracked, Assert.Single(context.ChangeTracker.Entries<ItemModel>()).Entity);
            await context.SaveChangesAsync();
        }
        ItemModel row = Assert.IsType<ItemModel>(await Read(item.Guid));
        Assert.Equal(7u, row.StackCount);
        Assert.Equal(321u, row.ExpirationTimeLeft);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task TwoItemInstancesWithSameGuid_ActiveOwnerRejectsDuplicateStaging(bool sameContext)
    {
        var owner = await PersistedItem(2);
        var contender = new GameItem(Assert.IsType<ItemModel>(await Read(owner.Guid)), itemManager);
        owner.StackCount = 4;
        contender.StackCount = 8;
        await using (CharacterContext first = Context())
        {
            owner.Save(first);
            if (sameContext)
                Assert.ThrowsAny<InvalidOperationException>(() => contender.Save(first));
            else
            {
                await using CharacterContext second = Context();
                Assert.ThrowsAny<InvalidOperationException>(() => contender.Save(second));
            }
            await first.SaveChangesAsync();
        }
        Assert.Equal(4u, (await Read(owner.Guid))?.StackCount);
    }

    [Fact]
    public async Task SequentialStaleItemOwner_CannotOverwriteAnotherOwnersCommittedStack()
    {
        var owner = await PersistedItem(2);
        var stale = new GameItem(Assert.IsType<ItemModel>(await Read(owner.Guid)), itemManager);
        owner.StackCount = 4;
        await Save(owner);
        stale.StackCount = 8;
        await using (CharacterContext context = Context())
            Assert.ThrowsAny<InvalidOperationException>(() => stale.Save(context));
        Assert.Equal(4u, (await Read(owner.Guid))?.StackCount);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ConflictingDurableOwnerOrItemType_IsRejectedWithoutOverwritingRow(bool changedOwner)
    {
        var item = await PersistedItem(2);
        await using (CharacterContext external = Context())
        {
            ItemModel row = await external.Item.SingleAsync(row => row.Id == item.Guid);
            if (changedOwner) row.OwnerId = OwnerId + 1;
            else row.ItemId = ItemId + 1;
            await external.SaveChangesAsync();
        }
        item.StackCount = 8;
        await using (CharacterContext context = Context())
            Assert.ThrowsAny<InvalidOperationException>(() => item.Save(context));
        ItemModel persisted = Assert.IsType<ItemModel>(await Read(item.Guid));
        Assert.Equal(2u, persisted.StackCount);
        Assert.Equal(changedOwner ? OwnerId + 1 : OwnerId, persisted.OwnerId);
        Assert.Equal(changedOwner ? ItemId : ItemId + 1, persisted.ItemId);
    }

    [Fact]
    public async Task ChargesExpirationBindingAndCodecs_RoundTripAfterRolledBackCreateAndUpdate()
    {
        info.Entry.MaxCharges = 12;
        info.Entry.ExpirationTimeMinutes = 3;
        var item = NewItem(5);
        Assert.Equal(12u, item.Charges);
        Assert.Equal(180u, item.ExpirationTimeLeft);
        item.Charges = 4;
        item.ExpirationTimeLeft = 91;
        item.Durability = .75f;
        item.MakeSoulbound();
        item.MicrochipIds.Add(14);
        item.MicrochipIds.Add(27);
        item.RuneSlots.Add(new ItemRuneSlot(RuneType.Air, 901));
        item.RuneSlots.Add(new ItemRuneSlot(RuneType.Fire, 902));
        item.TouchRuneSlots();
        await RollbackThenRetry(item);
        ItemModel created = Assert.IsType<ItemModel>(await Read(item.Guid));
        Assert.Equal("14,27", created.MicrochipIds);
        Assert.Equal($"{(uint)RuneType.Air}:901,{(uint)RuneType.Fire}:902", created.RuneSlots);
        Assert.Equal(4u, created.Charges);
        Assert.Equal(91u, created.ExpirationTimeLeft);
        Assert.Equal(.75f, created.Durability);
        Assert.True(created.Soulbound);

        var loaded = new GameItem(created, itemManager);
        loaded.StackCount = 8;
        loaded.Charges = 2;
        loaded.MicrochipIds.Add(39);
        loaded.RuneSlots[0].RuneItem2Id = 903;
        loaded.TouchRuneSlots();
        await RollbackThenRetry(loaded);
        ItemModel updated = Assert.IsType<ItemModel>(await Read(item.Guid));
        Assert.Equal("14,27,39", updated.MicrochipIds);
        Assert.Equal($"{(uint)RuneType.Air}:903,{(uint)RuneType.Fire}:902", updated.RuneSlots);
        Assert.Equal(8u, updated.StackCount);
        Assert.Equal(2u, updated.Charges);
        Assert.Equal(91u, updated.ExpirationTimeLeft);
        Assert.Equal(.75f, updated.Durability);
        Assert.True(updated.Soulbound);
    }

    private async Task RollbackThenRetry(GameItem item)
    {
        await using (CharacterContext context = Context())
        {
            await using var transaction = await context.Database.BeginTransactionAsync();
            item.Save(context);
            await context.SaveChangesAsync();
            await transaction.RollbackAsync();
        }
        await Save(item);
    }

    [Theory]
    [InlineData("create")]
    [InlineData("update")]
    [InlineData("delete")]
    public async Task SameContextAfterSavepointRollback_RejectsRestageThenFreshUnitConverges(string operation)
    {
        var item = operation == "create" ? NewItem(3) : await PersistedItem(2);
        if (operation == "update") item.StackCount = 8;
        if (operation == "delete") item.EnqueueDelete(true);
        await using (CharacterContext context = Context())
        {
            await using var transaction = await context.Database.BeginTransactionAsync();
            await transaction.CreateSavepointAsync("before_item_write");
            item.Save(context);
            await context.SaveChangesAsync();
            await transaction.RollbackToSavepointAsync("before_item_write");

            // This context's tracker no longer describes the database snapshot.
            // Reject before another flush can turn a missing create into an UPDATE
            // or acknowledge a DELETE which the savepoint already restored.
            Assert.ThrowsAny<InvalidOperationException>(() => item.Save(context));
            await transaction.RollbackAsync();
        }

        if (operation == "create") Assert.Null(await Read(item.Guid));
        else Assert.Equal(2u, (await Read(item.Guid))?.StackCount);
        await Save(item);
        if (operation == "delete") Assert.Null(await Read(item.Guid));
        else Assert.Equal(operation == "create" ? 3u : 8u, (await Read(item.Guid))?.StackCount);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task DeleteThenCancelInSameContext_RestagesLiveRowBeforeSqlOrAfterFlush(bool flushDelete)
    {
        var item = await PersistedItem(2);
        await using (CharacterContext context = Context())
        {
            await using var transaction = await context.Database.BeginTransactionAsync();
            item.EnqueueDelete(true);
            item.Save(context);
            if (flushDelete) await context.SaveChangesAsync();
            item.EnqueueDelete(false);
            item.StackCount = 7;
            item.Save(context);
            Assert.Single(context.ChangeTracker.Entries<ItemModel>());
            await context.SaveChangesAsync();
            await transaction.CommitAsync();
        }

        ItemModel row = Assert.IsType<ItemModel>(await Read(item.Guid));
        Assert.Equal(OwnerId, row.OwnerId);
        Assert.Equal(ItemId, row.ItemId);
        Assert.Equal(7u, row.StackCount);
        Assert.False(item.PendingDelete);
        await Save(item);
        Assert.Equal(7u, (await Read(item.Guid))?.StackCount);
    }

    [Fact]
    public async Task CreateStagedWithoutSql_OuterCommitDoesNotAcknowledgeUnflushedInsert()
    {
        var item = NewItem(3);
        await using (CharacterContext context = Context())
        {
            await using var transaction = await context.Database.BeginTransactionAsync();
            item.Save(context);
            await transaction.CommitAsync();
        }

        Assert.Null(await Read(item.Guid));
        Assert.True(item.PendingCreate);
        await Save(item);
        Assert.Equal(3u, (await Read(item.Guid))?.StackCount);
    }

    [Fact]
    public async Task UpdateStagedWithoutSql_OuterCommitRetainsChangedValueForFreshRetry()
    {
        var item = await PersistedItem(2);
        item.StackCount = 7;
        await using (CharacterContext context = Context())
        {
            await using var transaction = await context.Database.BeginTransactionAsync();
            item.Save(context);
            await transaction.CommitAsync();
        }

        Assert.Equal(2u, (await Read(item.Guid))?.StackCount);
        await Save(item);
        Assert.Equal(7u, (await Read(item.Guid))?.StackCount);
    }

    [Fact]
    public async Task FlushedUpdateThenUnflushedRestage_CommitAcknowledgesOnlyActualSqlSnapshot()
    {
        var item = await PersistedItem(2);
        await using (CharacterContext context = Context())
        {
            await using var transaction = await context.Database.BeginTransactionAsync();
            item.StackCount = 3;
            item.Save(context);
            await context.SaveChangesAsync();
            item.StackCount = 7;
            item.Save(context);
            // Commit is not a second SaveChanges: only quantity three reached SQL.
            await transaction.CommitAsync();
        }

        Assert.Equal(3u, (await Read(item.Guid))?.StackCount);
        await Save(item);
        Assert.Equal(7u, (await Read(item.Guid))?.StackCount);
    }

    [Theory]
    [InlineData("create")]
    [InlineData("update")]
    [InlineData("delete")]
    public async Task SaveChangesWithoutAcceptAllChanges_ActualCommitSettlesOnlyItsSqlWrite(string operation)
    {
        var item = operation == "create" ? NewItem(3) : await PersistedItem(2);
        if (operation == "update") item.StackCount = 3;
        if (operation == "delete") item.EnqueueDelete(true);
        await using (CharacterContext context = Context())
        {
            item.Save(context);
            await context.SaveChangesAsync(acceptAllChangesOnSuccess: false);
        }

        if (operation == "delete")
        {
            Assert.Null(await Read(item.Guid));
            await Save(item);
            Assert.Null(await Read(item.Guid));
        }
        else
        {
            Assert.Equal(3u, (await Read(item.Guid))?.StackCount);
            item.StackCount = 7;
            await Save(item);
            Assert.Equal(7u, (await Read(item.Guid))?.StackCount);
        }
    }

    private CharacterContext Context() => new(options);

    private GameItem NewItem(uint count) => new(OwnerId, info, count, itemManager: itemManager);

    private async Task<GameItem> PersistedItem(uint count)
    {
        var item = NewItem(count);
        await Save(item);
        ItemModel row = Assert.IsType<ItemModel>(await Read(item.Guid));
        return new GameItem(row, itemManager);
    }

    private async Task<ItemModel> Read(ulong id)
    {
        await using CharacterContext context = Context();
        return await context.Item.AsNoTracking().SingleOrDefaultAsync(row => row.Id == id);
    }

    private async Task Save(GameItem item)
    {
        await using CharacterContext context = Context();
        item.Save(context);
        await context.SaveChangesAsync();
    }

    private async Task Save(Inventory inventory)
    {
        await using CharacterContext context = Context();
        inventory.Save(context);
        await context.SaveChangesAsync();
    }

    private Inventory InventoryFor(GameItem item)
    {
        EnsureInventoryLocationCapacities();
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out var playerProxy);
        playerProxy.SetProperty(nameof(IPlayer.CharacterId), OwnerId);
        playerProxy.SetProperty(nameof(IPlayer.Session), RecordingDispatchProxy<IGameSession>.Create(out _));
        playerProxy.SetProperty(nameof(IPlayer.IsLoading), false);
        var inventory = new Inventory(player, new CharacterModel(), itemManager: itemManager);
        playerProxy.SetProperty(nameof(IPlayer.Inventory), inventory);
        inventory.LoadItem(item, InventoryLocation.Inventory, 0);
        return inventory;
    }

    private static void EnsureInventoryLocationCapacities()
    {
        if (AssetManager.InventoryLocationCapacities != null)
            return;

        var entries = new Dictionary<InventoryLocation, uint>();
        foreach (FieldInfo field in typeof(InventoryLocation).GetFields())
            foreach (InventoryLocationAttribute attribute in field.GetCustomAttributes<InventoryLocationAttribute>())
                entries.Add((InventoryLocation)field.GetValue(null)!, attribute.DefaultCapacity);
        typeof(AssetManager).GetProperty(nameof(AssetManager.InventoryLocationCapacities))!
            .SetValue(null, entries.ToImmutableDictionary());
    }

    public void Dispose()
    {
        // Every connection above belongs to this fixture and uses Pooling=False.
        Directory.Delete(directory, recursive: true);
    }
}
