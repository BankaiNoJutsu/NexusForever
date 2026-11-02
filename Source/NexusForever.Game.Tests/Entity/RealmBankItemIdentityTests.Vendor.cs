using Microsoft.EntityFrameworkCore;
using NexusForever.Database.Character;
using NexusForever.Database.Character.Model;
using NexusForever.Game.Abstract;
using NexusForever.Game.Abstract.Achievement;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Entity;
using NexusForever.Game.Static.Entity;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.Game.Tests.Transport;
using NexusForever.GameTable;
using NexusForever.GameTable.Model;
using NexusForever.Network.World.Message.Model;
using NexusForever.Network.World.Message.Model.Shared;
using NexusForever.WorldServer.Network.Message.Handler.Vendor;

namespace NexusForever.Game.Tests.Entity;

public sealed partial class RealmBankItemIdentityTests
{
    [Theory]
    [InlineData(false, "none")]
    [InlineData(true, "none")]
    [InlineData(false, "bank")]
    [InlineData(true, "bank")]
    [InlineData(false, "currency")]
    [InlineData(true, "currency")]
    public async Task BankVendorSaleCommitsItsPayoutAndSourceTogetherBeforePublication(bool partial, string failure)
    {
        await Seed(bank: BankRow(401));
        var (manager, player, items) = Access();
        var (inventory, _, source) = SwapInventory(manager, player, items, null, BankRow(401));
        var info = (RecordingDispatchProxy<IItemInfo>)(object)items.GetItemInfo(7001);
        info.SetMethodHandler(nameof(IItemInfo.GetVendorSellCurrency), args => (byte)args[0] == 0 ? CurrencyType.Credits : CurrencyType.None);
        info.SetMethodHandler(nameof(IItemInfo.GetVendorSellAmount), args => (byte)args[0] == 0 ? 7u : 0u);
        ((RecordingDispatchProxy<IItemManager>)(object)items).SetProperty(nameof(IItemManager.NextItemId), 205ul);
        var currencies = await InstallBankCurrency(player);
        var vendor = RecordingDispatchProxy<IVendorInfo>.Create(out var vp); vp.SetProperty(nameof(IVendorInfo.BuyPriceMultiplier), 1f);
        ((RecordingDispatchProxy<IPlayer>)(object)player).SetProperty(nameof(IPlayer.SelectedVendorInfo), vendor);
        var buyback = new BuybackManager(); uint quantity = partial ? 3u : 17u;
        if (failure != "none") await SplitTrigger(failure == "currency" ? "character_currency" : "realm_bank_item", failure == "bank" && !partial ? "DELETE" : "UPDATE");
        Assert.True(VendorSellHelper.SellItem(player, buyback, new ItemLocation { Location = InventoryLocation.RealmBank, BagIndex = 3 }, source, quantity));
        Assert.Empty(BankMessages(player.Session)); Assert.Equal(100ul, Assert.Single(currencies).Amount);
        await WaitForBankMove(inventory, source);
        using var read = new CharacterContext(Configuration);
        if (failure == "none")
        {
            Assert.Equal(100ul + quantity * 7ul, Assert.Single(read.CharacterCurrency).Amount);
            Assert.Equal(partial ? 1 : 0, read.RealmBankItem.Count());
            if (partial) Assert.Equal(14u, read.RealmBankItem.Single().StackCount);
            Assert.Equal(quantity, buyback.GetItem(player, 0).Quantity);
            Assert.Single(BankMessages(player.Session).OfType<ServerCombatReward>());
            Assert.Single(BankMessages(player.Session).OfType<ServerBuybackItemUpdated>());
        }
        else
        {
            Assert.Equal(100ul, Assert.Single(read.CharacterCurrency).Amount); Assert.Equal(17u, Assert.Single(read.RealmBankItem).StackCount);
            Assert.Empty(BankMessages(player.Session).OfType<ServerCombatReward>());
            Assert.Empty(BankMessages(player.Session).OfType<ServerBuybackItemUpdated>());
            Assert.Null(buyback.GetItem(player, 0)); Assert.True(inventory.RequiresAuthoritativeReload);
        }
        Assert.Empty(read.Item);
    }

    private async Task<CurrencyManager> InstallBankCurrency(IPlayer player, bool exists = true, ulong cap = 1000)
    {
        var tables = RecordingDispatchProxy<IGameTableManager>.Create(out var tp);
        tp.SetProperty(nameof(IGameTableManager.CurrencyType), PlayerBindPointTests.Table(new CurrencyTypeEntry { Id = (uint)CurrencyType.Credits, CapAmount = cap }));
        var model = new CharacterCurrencyModel { Id = player.CharacterId, CurrencyId = (byte)CurrencyType.Credits, Amount = 100 };
        if (exists) { using var seed = new CharacterContext(Configuration); seed.CharacterCurrency.Add(model); await seed.SaveChangesAsync(); }
        var manager = new CurrencyManager(tables); var character = new CharacterModel(); if (exists) character.Currency.Add(model); manager.Initialise(player, character);
        ((RecordingDispatchProxy<IPlayer>)(object)player).SetProperty(nameof(IPlayer.CurrencyManager), manager);
        ((RecordingDispatchProxy<IPlayer>)(object)player).SetProperty(nameof(IPlayer.AchievementManager), RecordingDispatchProxy<ICharacterAchievementManager>.Create(out _));
        Assert.NotNull(player.AchievementManager);
        return manager;
    }
}
