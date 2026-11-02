using System.Reflection;
using System.Runtime.CompilerServices;
using Microsoft.EntityFrameworkCore;
using NexusForever.Database;
using NexusForever.Database.Character;
using NexusForever.Database.Character.Model;
using NexusForever.Database.Configuration.Model;
using NexusForever.Game.Abstract;
using NexusForever.Game.Abstract.Account;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Entity;
using NexusForever.Game.RealmBank;
using NexusForever.Game.Static.Entity;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.GameTable.Model;
using GameItem = NexusForever.Game.Entity.Item;

namespace NexusForever.Game.Tests.Entity;

public sealed partial class RealmBankItemIdentityTests : IDisposable
{
    private readonly string path = Path.Combine(Path.GetTempPath(), $"NexusForeverRealmBankIdentity_{Guid.NewGuid():N}.sqlite");
    private readonly IConnectionString provider;
    public RealmBankItemIdentityTests() { }
    internal RealmBankItemIdentityTests(IConnectionString provider) => this.provider = provider;
    private IConnectionString Configuration => provider ?? new DatabaseConnectionString {
        Provider = DatabaseProvider.Sqlite, ConnectionString = $"Data Source={path};Pooling=False" };

    [Theory]
    [InlineData(0ul, 0ul, 0ul)]
    [InlineData(205ul, 0ul, 205ul)]
    [InlineData(0ul, 401ul, 401ul)]
    [InlineData(205ul, 401ul, 401ul)]
    [InlineData(500ul, 401ul, 500ul)]
    public async Task RestartItemHighWaterMarkIncludesBankOnlyGuids(ulong characterId, ulong bankId, ulong maximum)
    {
        await using (var context = new CharacterContext(Configuration))
        {
            await context.Database.MigrateAsync();
            if (characterId != 0)
                context.Item.Add(new ItemModel {
                    Id = characterId, ItemId = 7001, Location = (ushort)InventoryLocation.Inventory,
                    StackCount = 1, Durability = 1, MicrochipIds = string.Empty, RuneSlots = string.Empty });
            if (bankId != 0) context.RealmBankItem.Add(BankRow(bankId));
            await context.SaveChangesAsync();
        }
        var database = Database();
        Assert.Equal(maximum, database.GetNextItemId());
        Assert.True(database.GetNextItemId() + 1 > maximum);
    }

    [Fact]
    public async Task LoadedBankItemRetainsDefinitionAndPersistedScalarValues()
    {
        await using (var context = new CharacterContext(Configuration))
        {
            await context.Database.MigrateAsync();
            context.RealmBankItem.Add(BankRow(401));
            await context.SaveChangesAsync();
        }
        var databaseManager = RecordingDispatchProxy<IDatabaseManager>.Create(out var databases);
        databases.SetMethodReturn(nameof(IDatabaseManager.GetDatabase), Database());
        var info = RecordingDispatchProxy<IItemInfo>.Create(out var infoProxy);
        infoProxy.SetProperty(nameof(IItemInfo.Id), 7001u);
        infoProxy.SetProperty(nameof(IItemInfo.Entry), new Item2Entry { Id = 7001, MaxStackCount = 20, MaxCharges = 10 });
        var items = RecordingDispatchProxy<IItemManager>.Create(out var itemProxy);
        itemProxy.SetMethodReturn(nameof(IItemManager.GetItemInfo), info);
        var account = RecordingDispatchProxy<IAccount>.Create(out var accountProxy);
        accountProxy.SetProperty(nameof(IAccount.Id), 12u);
        var player = RecordingDispatchProxy<IPlayer>.Create(out var playerProxy);
        playerProxy.SetProperty(nameof(IPlayer.Account), account);
        playerProxy.SetProperty(nameof(IPlayer.Identity), new Identity { RealmId = 2, Id = 100 });
        playerProxy.SetProperty(nameof(IPlayer.CharacterId), 100ul);
        var inventory = RecordingDispatchProxy<IInventory>.Create(out var inventoryProxy);
        playerProxy.SetProperty(nameof(IPlayer.Inventory), inventory);
        var manager = new RealmBankManager(databaseManager, items);

        manager.EnsureLoaded(player);

        var load = Assert.Single(inventoryProxy.GetInvocations(nameof(IInventory.LoadItem)));
        IItem loaded = Assert.IsType<global::NexusForever.Game.Entity.Item>(load.Arguments[0]);
        Assert.Same(info, loaded.Info); Assert.Equal(7001u, loaded.Id);
        Assert.Equal(401ul, loaded.Guid); Assert.Equal(17u, loaded.StackCount);
        Assert.Equal(5u, loaded.Charges); Assert.Equal(0.75f, loaded.Durability);
        Assert.Equal(1234u, loaded.ExpirationTimeLeft); Assert.True(loaded.Soulbound);
        Assert.Equal(InventoryLocation.RealmBank, load.Arguments[1]); Assert.Equal(3u, load.Arguments[2]);
    }

    [Fact]
    public async Task RealItemDepositAndWithdrawalShareCommitAndRetainInMemoryCodecs()
    {
        ItemModel original = CharacterRow(); await Seed(original);
        var (manager, player, items) = Access();
        var item = new GameItem(original, items) { Location = InventoryLocation.RealmBank, BagIndex = 3 };
        await manager.SaveItemAsync(item, player);
        await using (var context = new CharacterContext(Configuration))
        {
            Assert.Empty(await context.Item.ToListAsync());
            var bank = Assert.Single(await context.RealmBankItem.ToListAsync());
            Assert.Equal(401ul, bank.Id); Assert.Equal(17u, bank.StackCount); Assert.Equal(5u, bank.Charges);
            Assert.True(bank.Soulbound);
        }
        Assert.False(item.PendingCreate); Assert.False(item.HasPendingSave);
        item.Location = InventoryLocation.Inventory; item.BagIndex = 4;
        await manager.SaveCharacterItemAsync(item, player);
        await using (var context = new CharacterContext(Configuration))
        {
            Assert.Empty(await context.RealmBankItem.ToListAsync());
            ItemModel restored = Assert.Single(await context.Item.ToListAsync());
            Assert.Equal(100ul, restored.OwnerId); Assert.Equal(401ul, restored.Id);
            Assert.Equal(4u, restored.BagIndex); Assert.Equal(17u, restored.StackCount);
            Assert.Equal(5u, restored.Charges); Assert.Equal(0.75f, restored.Durability);
            Assert.Equal(1234u, restored.ExpirationTimeLeft); Assert.True(restored.Soulbound);
            Assert.Equal(original.MicrochipIds, restored.MicrochipIds); Assert.Equal(original.RuneSlots, restored.RuneSlots);
        }
    }

    [Fact]
    public async Task BankLoadedRealItemWithdrawsWithoutPretendingCharacterRowAlreadyExists()
    {
        await Seed(bank: BankRow(401));
        var (manager, player, _) = Access();
        var item = Load(manager, player);
        Assert.False(item.PendingCreate);
        item.Location = InventoryLocation.Inventory; item.BagIndex = 2;
        await manager.SaveCharacterItemAsync(item, player);
        await using var context = new CharacterContext(Configuration);
        Assert.Empty(await context.RealmBankItem.ToListAsync());
        ItemModel row = Assert.Single(await context.Item.ToListAsync());
        Assert.Equal(401ul, row.Id); Assert.Equal(100ul, row.OwnerId); Assert.Equal(7001u, row.ItemId);
        Assert.Equal((ushort)InventoryLocation.Inventory, row.Location); Assert.Equal(2u, row.BagIndex);
        Assert.False(item.PendingCreate); Assert.False(item.HasPendingSave);
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task FailedTransferRollsBackBothTablesAndRetriesWithFreshSaveUnit(bool withdrawal)
    {
        ItemModel original = CharacterRow();
        await Seed(withdrawal ? null : original, withdrawal ? BankRow(401) : null);
        var (manager, player, items) = Access();
        GameItem item = withdrawal ? Load(manager, player) : new GameItem(original, items);
        item.Location = withdrawal ? InventoryLocation.Inventory : InventoryLocation.RealmBank;
        item.BagIndex = 3;
        await using (var context = new CharacterContext(Configuration))
        {
            string failureTrigger = withdrawal
                ? "CREATE TRIGGER reject_bank_transfer BEFORE INSERT ON item BEGIN SELECT RAISE(ABORT, 'owned transfer failure'); END;"
                : "CREATE TRIGGER reject_bank_transfer BEFORE INSERT ON realm_bank_item BEGIN SELECT RAISE(ABORT, 'owned transfer failure'); END;";
            await context.Database.ExecuteSqlRawAsync(failureTrigger);
        }
        await Assert.ThrowsAsync<DbUpdateException>(() => withdrawal
            ? manager.SaveCharacterItemAsync(item, player) : manager.SaveItemAsync(item, player));
        Assert.False(item.HasPendingSave);
        await using (var context = new CharacterContext(Configuration))
        {
            Assert.Equal(withdrawal ? 0 : 1, await context.Item.CountAsync());
            Assert.Equal(withdrawal ? 1 : 0, await context.RealmBankItem.CountAsync());
            await context.Database.ExecuteSqlRawAsync("DROP TRIGGER reject_bank_transfer;");
        }
        await (withdrawal ? manager.SaveCharacterItemAsync(item, player) : manager.SaveItemAsync(item, player));
        await using (var context = new CharacterContext(Configuration))
        {
            Assert.Equal(withdrawal ? 1 : 0, await context.Item.CountAsync());
            Assert.Equal(withdrawal ? 0 : 1, await context.RealmBankItem.CountAsync());
        }
    }

    [Fact]
    public async Task PendingCharacterUnitRejectsBankDepositUntilItsActualRollback()
    {
        ItemModel original = CharacterRow(); await Seed(original);
        var (manager, player, items) = Access();
        var item = new GameItem(original, items) { Location = InventoryLocation.Inventory, StackCount = 16 };
        await using (var context = new CharacterContext(Configuration))
        {
            await using var transaction = await context.Database.BeginTransactionAsync();
            item.Save(context); await context.SaveChangesAsync();
            item.Location = InventoryLocation.RealmBank;
            await Assert.ThrowsAsync<InvalidOperationException>(() => manager.SaveItemAsync(item, player));
            Assert.True(item.HasPendingSave);
            await transaction.RollbackAsync();
        }
        Assert.False(item.HasPendingSave);
        await manager.SaveItemAsync(item, player);
        await using var reload = new CharacterContext(Configuration);
        Assert.Empty(await reload.Item.ToListAsync());
        Assert.Equal(16u, Assert.Single(await reload.RealmBankItem.ToListAsync()).StackCount);
    }

    [Fact]
    public async Task StaleDuplicateItemCannotWithdrawAnotherInstancesCommittedDeposit()
    {
        ItemModel original = CharacterRow(); await Seed(original);
        var (manager, player, items) = Access();
        var current = new GameItem(original, items) { Location = InventoryLocation.RealmBank, BagIndex = 3 };
        var stale = new GameItem(original, items) { Location = InventoryLocation.Inventory, Charges = 4 };
        await manager.SaveItemAsync(current, player);
        await Assert.ThrowsAsync<InvalidOperationException>(() => manager.SaveCharacterItemAsync(stale, player));
        await using var context = new CharacterContext(Configuration);
        Assert.Empty(await context.Item.ToListAsync());
        var bank = Assert.Single(await context.RealmBankItem.ToListAsync());
        Assert.Equal(17u, bank.StackCount); Assert.Equal(5u, bank.Charges);
    }

    [Fact]
    public async Task StaleBankDomainDuplicateCannotRestoreOldQuantityOnWithdrawal()
    {
        RealmBankItemModel baseline = BankRow(401); await Seed(bank: baseline);
        var (manager, player, items) = Access();
        var current = new GameItem(BankItemModel(baseline), items) { Location = InventoryLocation.RealmBank, BagIndex = 3 };
        var stale = new GameItem(BankItemModel(baseline), items) { Location = InventoryLocation.RealmBank, BagIndex = 3 };
        current.StackCount = 16; await manager.SaveItemAsync(current, player);
        stale.Location = InventoryLocation.Inventory;
        await Assert.ThrowsAsync<InvalidOperationException>(() => manager.SaveCharacterItemAsync(stale, player));
        await using var context = new CharacterContext(Configuration);
        Assert.Empty(await context.Item.ToListAsync());
        Assert.Equal(16u, Assert.Single(await context.RealmBankItem.ToListAsync()).StackCount);
    }

    [Fact]
    public async Task SameBankItemCanUpdateThenWithdrawItsCommittedQuantity()
    {
        RealmBankItemModel baseline = BankRow(401); await Seed(bank: baseline);
        var (manager, player, items) = Access();
        var item = new GameItem(BankItemModel(baseline), items) { Location = InventoryLocation.RealmBank, BagIndex = 3 };
        item.StackCount = 16; await manager.SaveItemAsync(item, player);
        item.Location = InventoryLocation.Inventory; item.BagIndex = 4;
        await manager.SaveCharacterItemAsync(item, player);
        await using var context = new CharacterContext(Configuration);
        Assert.Empty(await context.RealmBankItem.ToListAsync());
        ItemModel row = Assert.Single(await context.Item.ToListAsync());
        Assert.Equal(401ul, row.Id); Assert.Equal(16u, row.StackCount); Assert.Equal(4u, row.BagIndex);
    }

    [Fact]
    public async Task PeriodicInventorySaveRejectsAnItemAlreadyOwnedByAnotherBankSaveUnit()
    {
        await Seed(bank: BankRow(401));
        var (manager, player, items) = Access();
        var item = new GameItem(BankItemModel(BankRow(401)), items);
        var bag = new Bag(InventoryLocation.RealmBank, 16, items); bag.AddItem(item, 3);
        // Only the Save graph is under test; avoid mutating global inventory-capacity fixtures.
        var inventory = (Inventory)RuntimeHelpers.GetUninitializedObject(typeof(Inventory));
        void Field(string name, object value) => typeof(Inventory).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(inventory, value);
        Field("characterId", 100ul); Field("player", player); Field("realmBankManager", manager);
        Field("itemManager", items); Field("deletedItems", new List<IItem>());
        Field("bags", new Dictionary<InventoryLocation, IBag> { [InventoryLocation.RealmBank] = bag });
        ((RecordingDispatchProxy<IPlayer>)(object)player).SetProperty(nameof(IPlayer.Inventory), inventory);

        await using (var bankContext = new CharacterContext(Configuration))
        {
            await using var transaction = await bankContext.Database.BeginTransactionAsync();
            item.SaveToRealmBank(bankContext); await bankContext.SaveChangesAsync();
            Assert.True(item.HasPendingSave);
            await using (var periodic = new CharacterContext(Configuration))
            {
                Assert.Throws<InvalidOperationException>(() => inventory.Save(periodic));
                Assert.Empty(periodic.ChangeTracker.Entries<ItemModel>());
                await Assert.ThrowsAsync<InvalidOperationException>(() => periodic.SaveChangesAsync());
            }
            Assert.True(item.HasPendingSave);
            await transaction.RollbackAsync();
        }
        Assert.False(item.HasPendingSave);
        await using var reload = new CharacterContext(Configuration);
        Assert.Empty(await reload.Item.ToListAsync()); Assert.Single(await reload.RealmBankItem.ToListAsync());
    }

    [Theory]
    [InlineData("account")] [InlineData("realm")] [InlineData("item")]
    public Task DepositCannotOverwriteConflictingBankOwnership(string mismatch) =>
        AssertConflictingBankOwnership(mismatch, withdrawal: false);

    [Theory]
    [InlineData("account")] [InlineData("realm")] [InlineData("item")]
    public Task WithdrawalCannotDeleteConflictingBankOwnership(string mismatch) =>
        AssertConflictingBankOwnership(mismatch, withdrawal: true);

    private async Task AssertConflictingBankOwnership(string mismatch, bool withdrawal)
    {
        RealmBankItemModel original = BankRow(401);
        if (mismatch == "account") original.AccountId = 99;
        if (mismatch == "realm") original.RealmId = 9;
        if (mismatch == "item") original.ItemId = 99999;
        await Seed(bank: original);
        var (manager, player, items) = Access();
        // A bank-domain object has no character baseline; row identity must still be checked.
        var item = new GameItem(BankItemModel(BankRow(401)), items) {
            Location = withdrawal ? InventoryLocation.Inventory : InventoryLocation.RealmBank, BagIndex = 3 };
        await Assert.ThrowsAsync<InvalidOperationException>(() => withdrawal
            ? manager.SaveCharacterItemAsync(item, player) : manager.SaveItemAsync(item, player));
        Assert.False(item.HasPendingSave);
        await using var context = new CharacterContext(Configuration);
        Assert.Empty(await context.Item.ToListAsync());
        RealmBankItemModel retained = Assert.Single(await context.RealmBankItem.ToListAsync());
        Assert.Equal(original.AccountId, retained.AccountId); Assert.Equal(original.RealmId, retained.RealmId);
        Assert.Equal(original.ItemId, retained.ItemId); Assert.Equal(original.StackCount, retained.StackCount);
        Assert.Equal(original.Charges, retained.Charges);
    }

    private async Task Seed(ItemModel character = null, RealmBankItemModel bank = null)
    {
        await using var context = new CharacterContext(Configuration);
        await context.Database.MigrateAsync();
        context.Character.Add(new CharacterModel {
            Id = 100, AccountId = 12, Name = "RealmBankIdentity", OriginalName = "RealmBankIdentity",
            CreateTime = DateTime.UtcNow, PathActivatedTimestamp = DateTime.UtcNow });
        if (character != null) context.Item.Add(character);
        if (bank != null) context.RealmBankItem.Add(bank);
        await context.SaveChangesAsync();
    }

    private (RealmBankManager Manager, IPlayer Player, IItemManager Items) Access(Func<RealmBankManager.BankSaveCommand, Task> execution = null)
    {
        var databaseManager = RecordingDispatchProxy<IDatabaseManager>.Create(out var databases);
        databases.SetMethodReturn(nameof(IDatabaseManager.GetDatabase), Database());
        var info = RecordingDispatchProxy<IItemInfo>.Create(out var infoProxy);
        infoProxy.SetProperty(nameof(IItemInfo.Id), 7001u);
        infoProxy.SetProperty(nameof(IItemInfo.Entry), new Item2Entry { Id = 7001, MaxStackCount = 20, MaxCharges = 10 });
        var items = RecordingDispatchProxy<IItemManager>.Create(out var itemProxy);
        itemProxy.SetMethodReturn(nameof(IItemManager.GetItemInfo), info);
        var account = RecordingDispatchProxy<IAccount>.Create(out var accountProxy);
        accountProxy.SetProperty(nameof(IAccount.Id), 12u);
        var player = RecordingDispatchProxy<IPlayer>.Create(out var playerProxy);
        playerProxy.SetProperty(nameof(IPlayer.Account), account);
        playerProxy.SetProperty(nameof(IPlayer.Identity), new Identity { RealmId = 2, Id = 100 });
        playerProxy.SetProperty(nameof(IPlayer.CharacterId), 100ul);
        return (new RealmBankManager(databaseManager, items, execution), player, items);
    }

    private static GameItem Load(RealmBankManager manager, IPlayer player)
    {
        var inventory = RecordingDispatchProxy<IInventory>.Create(out var inventoryProxy);
        ((RecordingDispatchProxy<IPlayer>)(object)player).SetProperty(nameof(IPlayer.Inventory), inventory);
        GameItem loaded = null;
        inventoryProxy.SetMethodHandler(nameof(IInventory.LoadItem), args => {
            loaded = (GameItem)args[0]; loaded.Location = (InventoryLocation)args[1]; loaded.BagIndex = (uint)args[2]; return null;
        });
        manager.EnsureLoaded(player); return Assert.IsType<GameItem>(loaded);
    }

    private static ItemModel CharacterRow() => new() {
        Id = 401, OwnerId = 100, ItemId = 7001, Location = (ushort)InventoryLocation.Inventory, BagIndex = 0,
        StackCount = 17, Charges = 5, Durability = 0.75f, ExpirationTimeLeft = 1234, Soulbound = true,
        MicrochipIds = "91,92", RuneSlots = "10:9001,7:0" };

    private static ItemModel BankItemModel(RealmBankItemModel row) => new() {
        Id = row.Id, OwnerId = 100, ItemId = row.ItemId, Location = (ushort)InventoryLocation.RealmBank,
        BagIndex = row.BagIndex, StackCount = row.StackCount, Charges = row.Charges, Durability = row.Durability,
        ExpirationTimeLeft = row.ExpirationTimeLeft, Soulbound = row.Soulbound };

    private CharacterDatabase Database()
    {
        var database = new CharacterDatabase(); database.Initialise(Configuration); return database;
    }

    private static RealmBankItemModel BankRow(ulong id) => new() {
        Id = id, AccountId = 12, RealmId = 2, ItemId = 7001, BagIndex = 3,
        StackCount = 17, Charges = 5, Durability = 0.75f, ExpirationTimeLeft = 1234, Soulbound = true };

    public void Dispose()
    {
        if (File.Exists(path)) File.Delete(path);
    }
}
