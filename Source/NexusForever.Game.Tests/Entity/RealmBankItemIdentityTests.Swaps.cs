using System.Reflection;
using System.Runtime.CompilerServices;
using Microsoft.EntityFrameworkCore;
using NexusForever.Database;
using NexusForever.Database.Character;
using NexusForever.Database.Character.Model;
using NexusForever.Game.Abstract;
using NexusForever.Game.Abstract.Account;
using NexusForever.Game.Abstract.Account.Entitlement;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Entity;
using NexusForever.Game.RealmBank;
using NexusForever.Game.Static.Entity;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.Network.Session;
using NexusForever.Network.World.Message.Static;
using GameItem = NexusForever.Game.Entity.Item;

namespace NexusForever.Game.Tests.Entity;

public sealed partial class RealmBankItemIdentityTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task InventoryBankSwapCannotCommitOneHalfWhenTheOtherInsertFails(bool bankFirst)
    {
        var character = CharacterRow(); var bank = BankRow(402);
        await Seed(character, bank);
        var (manager, player, items) = Access();
        var (inventory, carried, stored) = SwapInventory(manager, player, items, character, bank);
        string table = bankFirst ? "realm_bank_item" : "item";
        using (var fail = new CharacterContext(Configuration))
            await fail.Database.ExecuteSqlRawAsync(Configuration.Provider == DatabaseProvider.Sqlite
                ? $"CREATE TRIGGER reject_swap BEFORE INSERT ON {table} BEGIN SELECT RAISE(ABORT, 'swap failure'); END;"
                : $"CREATE TRIGGER reject_swap BEFORE INSERT ON `{table}` FOR EACH ROW SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT='swap failure'");

        inventory.ItemMove(bankFirst ? stored : carried,
            bankFirst ? InventoryLocation.Inventory : InventoryLocation.RealmBank, bankFirst ? 0u : 3u);
        await WaitForBankMove(inventory, carried, stored);

        using var read = new CharacterContext(Configuration);
        var savedCharacter = Assert.Single(read.Item); var savedBank = Assert.Single(read.RealmBankItem);
        Assert.Equal(character.Id, savedCharacter.Id); Assert.Equal(character.BagIndex, savedCharacter.BagIndex);
        Assert.Equal(character.MicrochipIds, savedCharacter.MicrochipIds); Assert.Equal(character.RuneSlots, savedCharacter.RuneSlots);
        Assert.Equal(bank.Id, savedBank.Id); Assert.Equal(bank.BagIndex, savedBank.BagIndex);
        await read.Database.ExecuteSqlRawAsync("DROP TRIGGER reject_swap");
        await manager.SaveSwapAsync(carried, stored, player);
        read.ChangeTracker.Clear();
        Assert.Equal(bank.Id, Assert.Single(read.Item).Id);
        Assert.Equal(character.Id, Assert.Single(read.RealmBankItem).Id);
        Assert.False(carried.HasPendingSave); Assert.False(stored.HasPendingSave);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task InventoryBankSwapPreservesBothItemsAndNormalSaveCannotResurrectTheDeposit(bool bankFirst)
    {
        var character = CharacterRow(); var bank = BankRow(402);
        bank.MicrochipIds = "3,4"; bank.RuneSlots = "10:6002";
        await Seed(character, bank); var (manager, player, items) = Access();
        var (inventory, carried, stored) = SwapInventory(manager, player, items, character, bank);
        inventory.ItemMove(bankFirst ? stored : carried,
            bankFirst ? InventoryLocation.Inventory : InventoryLocation.RealmBank, bankFirst ? 0u : 3u);
        await WaitForBankMove(inventory, carried, stored);
        using (var normal = new CharacterContext(Configuration)) { inventory.Save(normal); await normal.SaveChangesAsync(); }
        using var read = new CharacterContext(Configuration);
        var savedCharacter = Assert.Single(read.Item); var savedBank = Assert.Single(read.RealmBankItem);
        Assert.Equal(bank.Id, savedCharacter.Id); Assert.Equal(character.BagIndex, savedCharacter.BagIndex);
        Assert.Equal(bank.MicrochipIds, savedCharacter.MicrochipIds); Assert.Equal(bank.RuneSlots, savedCharacter.RuneSlots);
        Assert.Equal(character.Id, savedBank.Id); Assert.Equal(bank.BagIndex, savedBank.BagIndex);
        Assert.Equal(character.MicrochipIds, savedBank.MicrochipIds); Assert.Equal(character.RuneSlots, savedBank.RuneSlots);
        Assert.Equal(character.StackCount, savedBank.StackCount); Assert.Equal(bank.Charges, savedCharacter.Charges);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task InventorySwapWithinBankPersistsBothSlotsOrRollsBothBack(bool failSecond)
    {
        var first = BankRow(401); var second = BankRow(402); second.BagIndex = 4;
        await Seed(bank: first);
        using (var seed = new CharacterContext(Configuration))
        {
            seed.RealmBankItem.Add(second); await seed.SaveChangesAsync();
            if (failSecond) await seed.Database.ExecuteSqlRawAsync(Configuration.Provider == DatabaseProvider.Sqlite
                ? "CREATE TRIGGER reject_swap BEFORE UPDATE ON realm_bank_item WHEN NEW.id=402 BEGIN SELECT RAISE(ABORT, 'swap failure'); END;"
                : "CREATE TRIGGER reject_swap BEFORE UPDATE ON realm_bank_item FOR EACH ROW BEGIN IF NEW.id=402 THEN SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT='swap failure'; END IF; END");
        }
        var (manager, player, items) = Access();
        var (inventory, left, right) = SwapInventory(manager, player, items, BankItemModel(first), second, bothBank: true);
        inventory.ItemMove(left, InventoryLocation.RealmBank, second.BagIndex); await WaitForBankMove(inventory, left, right);
        using var read = new CharacterContext(Configuration); Assert.Empty(read.Item);
        Assert.Equal(failSecond ? first.BagIndex : second.BagIndex, read.RealmBankItem.Single(row => row.Id == first.Id).BagIndex);
        Assert.Equal(failSecond ? second.BagIndex : first.BagIndex, read.RealmBankItem.Single(row => row.Id == second.Id).BagIndex);
        if (failSecond)
        {
            await read.Database.ExecuteSqlRawAsync("DROP TRIGGER reject_swap");
            await manager.SaveSwapAsync(left, right, player); read.ChangeTracker.Clear();
            Assert.Equal(second.BagIndex, read.RealmBankItem.Single(row => row.Id == first.Id).BagIndex);
            Assert.Equal(first.BagIndex, read.RealmBankItem.Single(row => row.Id == second.Id).BagIndex);
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task PartialBankStackMergePersistsBothCountsInOneUnit(bool bankFirst)
    {
        var character = CharacterRow(); var bank = BankRow(402); await Seed(character, bank);
        var (manager, player, items) = Access();
        ((RecordingDispatchProxy<IItemInfo>)(object)items.GetItemInfo(7001)).SetMethodReturn(nameof(IItemInfo.IsStackable), true);
        var (inventory, carried, stored) = SwapInventory(manager, player, items, character, bank);
        inventory.ItemMove(bankFirst ? stored : carried,
            bankFirst ? InventoryLocation.Inventory : InventoryLocation.RealmBank, bankFirst ? 0u : 3u);
        await WaitForBankMove(inventory, carried, stored);
        using var read = new CharacterContext(Configuration);
        Assert.Equal(bankFirst ? 20u : 14u, Assert.Single(read.Item).StackCount);
        Assert.Equal(bankFirst ? 14u : 20u, Assert.Single(read.RealmBankItem).StackCount);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task BankSelfMoveLeavesTheItemAndItsStackUnchanged(bool stackable)
    {
        var character = CharacterRow(); var bank = BankRow(402); await Seed(character, bank);
        var (manager, player, items) = Access();
        ((RecordingDispatchProxy<IItemInfo>)(object)items.GetItemInfo(7001)).SetMethodReturn(nameof(IItemInfo.IsStackable), stackable);
        var (inventory, _, stored) = SwapInventory(manager, player, items, character, bank);
        inventory.ItemMove(stored, InventoryLocation.RealmBank, bank.BagIndex);
        Assert.Same(stored, inventory.GetItem(stored.Guid)); Assert.Equal(bank.StackCount, stored.StackCount);
        Assert.False(stored.HasPendingSave);
        using var read = new CharacterContext(Configuration);
        Assert.Equal(bank.StackCount, Assert.Single(read.RealmBankItem).StackCount); Assert.Single(read.Item);
    }

    [Fact]
    public async Task ConflictingSecondItemSaveRejectsTheWholeSwapAndReleasesTheFirstItemForRetry()
    {
        var character = CharacterRow(); var bank = BankRow(402); await Seed(character, bank);
        var (manager, player, items) = Access();
        var carried = new GameItem(character, items) { Location = InventoryLocation.RealmBank, BagIndex = 3 };
        var stored = new GameItem(BankItemModel(bank), items) { Location = InventoryLocation.RealmBank, BagIndex = 3 };
        using (var pending = new CharacterContext(Configuration))
        {
            stored.SaveToRealmBank(pending);
            stored.Location = InventoryLocation.Inventory; stored.BagIndex = 0;
            await Assert.ThrowsAsync<InvalidOperationException>(() => manager.SaveSwapAsync(carried, stored, player));
            Assert.False(carried.HasPendingSave); Assert.True(stored.HasPendingSave);
            using var read = new CharacterContext(Configuration);
            Assert.Equal(character.Id, Assert.Single(read.Item).Id); Assert.Equal(bank.Id, Assert.Single(read.RealmBankItem).Id);
        }
        Assert.False(stored.HasPendingSave);
        await manager.SaveSwapAsync(carried, stored, player);
        using var final = new CharacterContext(Configuration);
        Assert.Equal(bank.Id, Assert.Single(final.Item).Id); Assert.Equal(character.Id, Assert.Single(final.RealmBankItem).Id);
    }

    [Fact]
    public async Task ConflictingSecondBankOwnerCannotFlushTheFirstHalfOfASwap()
    {
        var character = CharacterRow(); var bank = BankRow(402); bank.AccountId = 13; await Seed(character, bank);
        var (manager, player, items) = Access();
        var carried = new GameItem(character, items) { Location = InventoryLocation.RealmBank, BagIndex = 3 };
        var stored = new GameItem(BankItemModel(bank), items) { Location = InventoryLocation.Inventory, BagIndex = 0 };
        await Assert.ThrowsAsync<InvalidOperationException>(() => manager.SaveSwapAsync(carried, stored, player));
        Assert.False(carried.HasPendingSave); Assert.False(stored.HasPendingSave);
        using var read = new CharacterContext(Configuration);
        Assert.Equal(character.Id, Assert.Single(read.Item).Id); Assert.Equal(13u, Assert.Single(read.RealmBankItem).AccountId);
        var row = read.RealmBankItem.Single(); row.AccountId = 12; await read.SaveChangesAsync();
        await manager.SaveSwapAsync(carried, stored, player); read.ChangeTracker.Clear();
        Assert.Equal(bank.Id, Assert.Single(read.Item).Id); Assert.Equal(character.Id, Assert.Single(read.RealmBankItem).Id);
    }

    private static (Inventory Inventory, GameItem Carried, GameItem Stored) SwapInventory(
        RealmBankManager manager, IPlayer player, IItemManager items, ItemModel character, RealmBankItemModel bank, bool bothBank = false)
    {
        var entitlement = RecordingDispatchProxy<IAccountEntitlement>.Create(out var unlock);
        unlock.SetProperty(nameof(IAccountEntitlement.Amount), 1u);
        var entitlements = RecordingDispatchProxy<IAccountEntitlementManager>.Create(out var permissions);
        permissions.SetMethodHandler(nameof(IAccountEntitlementManager.GetEntitlement), arguments =>
            (EntitlementType)arguments[0] == EntitlementType.SharedRealmBankUnlock ? entitlement : null);
        ((RecordingDispatchProxy<IAccount>)(object)player.Account)
            .SetProperty(nameof(IAccount.EntitlementManager), entitlements);
        var session = RecordingDispatchProxy<IGameSession>.Create(out _);
        ((RecordingDispatchProxy<IGameSession>)(object)session).SetProperty(nameof(IGameSession.Events), new NexusForever.Shared.Game.Events.EventQueue());
        ((RecordingDispatchProxy<IAccount>)(object)player.Account).SetProperty(nameof(IAccount.Session), session);
        ((RecordingDispatchProxy<IPlayer>)(object)player).SetProperty(nameof(IPlayer.Session), session);
        var storedModel = bank == null ? null : BankItemModel(bank);
        if (storedModel != null) { storedModel.MicrochipIds = bank.MicrochipIds; storedModel.RuneSlots = bank.RuneSlots; }
        var carried = character == null ? null : new GameItem(character, items);
        var stored = storedModel == null ? null : new GameItem(storedModel, items);
        var bag = new Bag(InventoryLocation.Inventory, 16, items);
        var bankBag = new Bag(InventoryLocation.RealmBank, 16, items);
        if (stored != null) bankBag.AddItem(stored, bank.BagIndex);
        if (carried != null) (bothBank ? bankBag : bag).AddItem(carried, character.BagIndex);
        var inventory = (Inventory)RuntimeHelpers.GetUninitializedObject(typeof(Inventory));
        void Field(string name, object value) => typeof(Inventory).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(inventory, value);
        Field("characterId", 100ul); Field("player", player); Field("realmBankManager", manager);
        Field("itemManager", items); Field("deletedItems", new List<IItem>());
        Field("bags", new Dictionary<InventoryLocation, IBag> { [InventoryLocation.Inventory] = bag, [InventoryLocation.RealmBank] = bankBag });
        ((RecordingDispatchProxy<IPlayer>)(object)player).SetProperty(nameof(IPlayer.Inventory), inventory);
        return (inventory, carried, stored);
    }

    private static async Task WaitForSwapSaves(params GameItem[] items)
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(15));
        while (items.Any(item => item.HasPendingSave)) await Task.Delay(10, timeout.Token);
    }
}
