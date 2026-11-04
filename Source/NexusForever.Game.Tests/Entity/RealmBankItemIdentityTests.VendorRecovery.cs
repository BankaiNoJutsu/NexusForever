using Microsoft.EntityFrameworkCore;
using System.Reflection;
using NexusForever.Database;
using NexusForever.Database.Character;
using NexusForever.Database.Character.Model;
using NexusForever.Game.Abstract;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Entity;
using NexusForever.Game.RealmBank;
using NexusForever.Game.Static.Entity;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.Game.Tests.Transport;
using NexusForever.GameTable;
using NexusForever.GameTable.Model;
using NexusForever.Network.Session;
using NexusForever.Network;
using NexusForever.Network.World.Message.Model;
using NexusForever.Network.World.Message.Model.Shared;
using NexusForever.WorldServer.Network.Message.Handler.Vendor;
using NexusForever.WorldServer.Network;
using GameItem = NexusForever.Game.Entity.Item;

namespace NexusForever.Game.Tests.Entity;

public sealed partial class RealmBankItemIdentityTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task BankSaleRetainsDirtyWalletAndDeferredIntentAcrossCommitReplay(bool replay)
    {
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var f = await VendorFixture(command => command.ExecuteAsync(async attempt =>
        { entered.SetResult(); await release.Task; await attempt(); if (replay) await attempt(); }));
        var wallet = Assert.Single(f.Currency); wallet.Amount = 120;
        Assert.True(Sell(f, 3)); await entered.Task.WaitAsync(TimeSpan.FromSeconds(15));
        Assert.False(f.Currency.CanAfford(CurrencyType.Credits, 1));
        Assert.Throws<InvalidOperationException>(() => wallet.Amount = 1);
        using (var denied = new CharacterContext(Configuration))
        { Assert.Throws<InvalidOperationException>(() => f.Currency.Save(denied)); Assert.Throws<InvalidOperationException>(() => denied.SaveChanges()); }
        f.Currency.CurrencyAddAmount(CurrencyType.Credits, 7); f.Currency.CurrencySubtractAmount(CurrencyType.Credits, 2);
        Assert.Equal(120ul, wallet.Amount); Assert.Empty(BankMessages(f.Player.Session));
        release.SetResult(); await WaitForBankMove(f.Inventory, f.Item);
        Assert.Equal(146ul, wallet.Amount); Assert.False(f.Inventory.RequiresAuthoritativeReload);
        using (var read = new CharacterContext(Configuration)) Assert.Equal(141ul, Assert.Single(read.CharacterCurrency).Amount);
        Assert.Equal(new ulong[] { 141, 148, 146 }, BankMessages(f.Player.Session).OfType<ServerCombatReward>().Select(packet => packet.NewValue));
        Assert.Single(BankMessages(f.Player.Session).OfType<ServerBuybackItemUpdated>());
        using (var save = new CharacterContext(Configuration)) { f.Currency.Save(save); await save.SaveChangesAsync(); }
        using var final = new CharacterContext(Configuration); Assert.Equal(146ul, Assert.Single(final.CharacterCurrency).Amount);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task BankSaleCreatesItsMissingWalletInTheSameUnitOrRollsBackBoth(bool fail)
    {
        var f = await VendorFixture(exists: false);
        if (fail) await SplitTrigger("character_currency", "INSERT");
        Assert.True(Sell(f)); Assert.Empty(f.Currency); Assert.Empty(BankMessages(f.Player.Session));
        await WaitForBankMove(f.Inventory, f.Item);
        using var read = new CharacterContext(Configuration);
        if (fail) { Assert.Empty(f.Currency); Assert.Empty(read.CharacterCurrency); Assert.Single(read.RealmBankItem); Assert.Null(f.Buyback.GetItem(f.Player, 0)); }
        else { Assert.Equal(119ul, Assert.Single(read.CharacterCurrency).Amount); Assert.Equal(119ul, Assert.Single(f.Currency).Amount); Assert.Empty(read.RealmBankItem); }
    }

    [Fact]
    public async Task BankSaleClampsTheWalletButPreservesItsOriginalBuybackPrice()
    {
        var f = await VendorFixture(cap: 110); Assert.True(Sell(f)); await WaitForBankMove(f.Inventory, f.Item);
        Assert.Equal(110ul, Assert.Single(f.Currency).Amount);
        Assert.Equal(119ul, Assert.Single(f.Buyback.GetItem(f.Player, 0).CurrencyChange).CurrencyAmount);
        using var read = new CharacterContext(Configuration); Assert.Equal(110ul, Assert.Single(read.CharacterCurrency).Amount);
    }

    [Theory]
    [InlineData("session")]
    [InlineData("inventory")]
    [InlineData("currency")]
    public async Task FinalizedBankSaleSettlesDurablePayoutWithoutPublishingToItsOldOwner(string finalization)
    {
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var f = await VendorFixture(command => command.ExecuteAsync(async attempt => { entered.SetResult(); await release.Task; await attempt(); }));
        var oldSession = f.Player.Session;
        Assert.True(Sell(f)); await entered.Task.WaitAsync(TimeSpan.FromSeconds(15));
        if (finalization == "session") ((RecordingDispatchProxy<IPlayer>)(object)f.Player).SetProperty(nameof(IPlayer.Session), RecordingDispatchProxy<IGameSession>.Create(out _));
        if (finalization == "inventory") f.Inventory.InvalidateRewardDeliveries();
        if (finalization == "currency") f.Currency.InvalidateContractCredit();
        release.SetResult();
        var completion = (Task)typeof(Inventory).GetField("realmBankMoveSave", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(f.Inventory)!;
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(15));
        while (true)
        {
            oldSession.Events.Update(0); f.Inventory.Update(0);
            using var read = new CharacterContext(Configuration);
            if (!read.RealmBankItem.Any() && f.Inventory.RequiresAuthoritativeReload && completion.IsCompleted) break;
            await Task.Delay(10, timeout.Token);
        }
        await completion;
        Assert.Empty(BankMessages(oldSession).OfType<ServerCombatReward>());
        Assert.Empty(BankMessages(oldSession).OfType<ServerBuybackItemUpdated>());
        Assert.Null(f.Buyback.GetItem(f.Player, 0));
        using var final = new CharacterContext(Configuration); Assert.Equal(219ul, Assert.Single(final.CharacterCurrency).Amount);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task BankSalePacketFailureCannotRestoreTheSourceOrRepayAgain(bool buybackPacket)
    {
        var f = await VendorFixture();
        ((RecordingDispatchProxy<IGameSession>)(object)f.Player.Session).SetMethodHandler(nameof(IGameSession.EnqueueMessageEncrypted), args =>
        { if (buybackPacket ? args[0] is ServerBuybackItemUpdated : args[0] is ServerCombatReward) throw new IOException("Publication failed."); return null; });
        Assert.True(Sell(f)); await WaitForBankMove(f.Inventory, f.Item);
        Assert.True(f.Inventory.RequiresAuthoritativeReload); Assert.True(f.Currency.RequiresContractCurrencyReload);
        using var read = new CharacterContext(Configuration); Assert.Empty(read.RealmBankItem); Assert.Equal(219ul, Assert.Single(read.CharacterCurrency).Amount);
        Assert.Equal(buybackPacket, f.Buyback.GetItem(f.Player, 0) != null);
        using var denied = new CharacterContext(Configuration);
        Assert.Throws<InvalidOperationException>(() => Assert.Single(f.Currency).Save(denied));
    }

    [Fact]
    public async Task RewrittenBankSaleCurrencyRejectsTheWholeBankTransaction()
    {
        var f = await VendorFixture();
        using (var trigger = new CharacterContext(Configuration)) await trigger.Database.ExecuteSqlRawAsync(Configuration.Provider == DatabaseProvider.Sqlite
            ? "CREATE TRIGGER rewrite_bank_cash AFTER UPDATE ON character_currency BEGIN UPDATE character_currency SET amount=NEW.amount+1 WHERE id=NEW.id AND currencyId=NEW.currencyId; END;"
            : "CREATE TRIGGER rewrite_bank_cash BEFORE UPDATE ON character_currency FOR EACH ROW SET NEW.amount=NEW.amount+1");
        Assert.True(Sell(f)); await WaitForBankMove(f.Inventory, f.Item);
        Assert.True(f.Inventory.RequiresAuthoritativeReload);
        using var read = new CharacterContext(Configuration); Assert.Equal(100ul, Assert.Single(read.CharacterCurrency).Amount); Assert.Single(read.RealmBankItem);
        Assert.Empty(BankMessages(f.Player.Session).OfType<ServerCombatReward>()); Assert.Null(f.Buyback.GetItem(f.Player, 0));
    }

    private sealed record VendorState(Inventory Inventory, IPlayer Player, GameItem Item, CurrencyManager Currency, BuybackManager Buyback);

    [Fact]
    public async Task BankSaleCannotEnrollAnotherPlayersCurrencyManager()
    {
        var f = await VendorFixture();
        var other = RecordingDispatchProxy<IPlayer>.Create(out var otherActor);
        otherActor.SetProperty(nameof(IPlayer.CharacterId), 100ul);
        var tables = RecordingDispatchProxy<IGameTableManager>.Create(out var tp);
        tp.SetProperty(nameof(IGameTableManager.CurrencyType), PlayerBindPointTests.Table(new CurrencyTypeEntry { Id = 1, CapAmount = 1000 }));
        var currency = new CurrencyManager(tables); var model = new CharacterModel();
        model.Currency.Add(new CharacterCurrencyModel { Id = 100, CurrencyId = 1, Amount = 100 }); currency.Initialise(other, model);
        otherActor.SetProperty(nameof(IPlayer.CurrencyManager), currency);
        ((RecordingDispatchProxy<IPlayer>)(object)f.Player).SetProperty(nameof(IPlayer.CurrencyManager), currency);
        Assert.False(Sell(f)); Assert.Same(f.Item, f.Inventory.GetItem(401ul)); Assert.False(f.Inventory.IsSaveBlocked);
        Assert.Null(f.Buyback.GetItem(f.Player, 0)); Assert.Empty(BankMessages(f.Player.Session).OfType<ServerCombatReward>());
        using var read = new CharacterContext(Configuration); Assert.Equal(17u, Assert.Single(read.RealmBankItem).StackCount); Assert.Equal(100ul, Assert.Single(read.CharacterCurrency).Amount);
    }

    [Fact]
    public async Task CurrencySaveWaitingForItsRowGateCannotPassANewBankOwner()
    {
        var f = await VendorFixture(); var wallet = Assert.IsType<Currency>(Assert.Single(f.Currency));
        object persistence = typeof(Currency).GetField("persistence", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(wallet)!;
        object gate = persistence.GetType().GetField("Gate", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(persistence)!;
        using var entered = new ManualResetEventSlim(); Exception error = null, rejectedFlush = null; bool rejected = false;
        var worker = new Thread(() =>
        {
            using var context = new CharacterContext(Configuration);
            context.CharacterCurrency.Add(new() { Id = 100, CurrencyId = 2, Amount = 999 });
            entered.Set();
            try { wallet.Save(context); context.SaveChanges(); }
            catch (Exception failure)
            {
                error = failure; rejected = context.IsSaveRejected;
                try { context.SaveChanges(); } catch (Exception denied) { rejectedFlush = denied; }
            }
        }) { IsBackground = true };
        Monitor.Enter(gate);
        try
        {
            worker.Start(); Assert.True(entered.Wait(TimeSpan.FromSeconds(15)));
            Assert.True(SpinWait.SpinUntil(() => (worker.ThreadState & ThreadState.WaitSleepJoin) != 0, TimeSpan.FromSeconds(15)));
            Assert.True(Sell(f));
        }
        finally { Monitor.Exit(gate); }
        Assert.True(worker.Join(TimeSpan.FromSeconds(15))); Assert.IsType<InvalidOperationException>(error);
        Assert.True(rejected); Assert.IsType<InvalidOperationException>(rejectedFlush);
        await WaitForBankMove(f.Inventory, f.Item);
        using var read = new CharacterContext(Configuration); Assert.Equal(219ul, Assert.Single(read.CharacterCurrency).Amount);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task BankSaleBuybackRestoresTheOwnedQuantityAndNormalSaveCannotDuplicateIt(bool partial)
    {
        var f = await VendorFixture();
        if (!partial) { f.Item.MicrochipIds.Add(91); f.Item.RuneSlots.Add(new ItemRuneSlot((NexusForever.Game.Static.Crafting.RuneType)10, 9001)); }
        Assert.True(Sell(f, partial ? 3u : 17u)); await WaitForBankMove(f.Inventory, f.Item);
        var sold = f.Buyback.GetItem(f.Player, 0).Item;
        var world = RecordingDispatchProxy<IWorldSession>.Create(out var wp); wp.SetProperty(nameof(IWorldSession.Player), f.Player);
        int previousMessages = BankMessages(f.Player.Session).Length;
        using var reader = new GamePacketReader(new MemoryStream(new byte[4]));
        var request = new ClientBuybackItemFromVendor(); request.Read(reader);
        new ClientBuybackItemFromVendorHandler(f.Buyback).HandleMessage(world, request);
        Assert.Equal(previousMessages, BankMessages(f.Player.Session).Length);
        Assert.Equal(100ul + (partial ? 21ul : 119ul), Assert.Single(f.Currency).Amount);
        new ClientBuybackItemFromVendorHandler(f.Buyback).HandleMessage(world, request);
        await WaitForBankMove(f.Inventory, (GameItem)sold);
        Assert.Null(f.Buyback.GetItem(f.Player, 0)); Assert.Same(sold, f.Inventory.GetItem(sold.Guid));
        Assert.Equal(100ul, Assert.Single(f.Currency).Amount);
        var notices = BankMessages(f.Player.Session).Skip(previousMessages).ToArray();
        Assert.Single(notices.OfType<ServerBuybackItemRemoved>());
        Assert.Single(notices.OfType<ServerCombatReward>());
        using (var committed = new CharacterContext(Configuration))
        {
            Assert.Equal(100ul, committed.CharacterCurrency.Single().Amount);
            Assert.Equal(sold.Guid, committed.Item.Single().Id);
        }
        using (var save = new CharacterContext(Configuration)) { f.Inventory.Save(save); f.Currency.Save(save); await save.SaveChangesAsync(); }
        using var read = new CharacterContext(Configuration); var restored = Assert.Single(read.Item);
        Assert.Equal(partial ? 205ul : 401ul, restored.Id); Assert.Equal(partial ? 3u : 17u, restored.StackCount);
        Assert.Equal(100ul, restored.OwnerId); Assert.Equal(100ul, Assert.Single(read.CharacterCurrency).Amount);
        Assert.Equal(partial ? 1 : 0, read.RealmBankItem.Count());
        if (partial) Assert.Equal(14u, read.RealmBankItem.Single().StackCount);
        else { Assert.Equal("91", restored.MicrochipIds); Assert.Equal("10:9001", restored.RuneSlots); }
    }
    private async Task<VendorState> VendorFixture(Func<RealmBankManager.BankSaveCommand, Task> execution = null, bool exists = true, ulong cap = 1000)
    {
        await Seed(bank: BankRow(401)); var (manager, player, items) = Access(execution);
        var (inventory, _, source) = SwapInventory(manager, player, items, null, BankRow(401));
        var info = (RecordingDispatchProxy<IItemInfo>)(object)items.GetItemInfo(7001);
        info.SetMethodHandler(nameof(IItemInfo.GetVendorSellCurrency), args => (byte)args[0] == 0 ? CurrencyType.Credits : CurrencyType.None);
        info.SetMethodHandler(nameof(IItemInfo.GetVendorSellAmount), args => (byte)args[0] == 0 ? 7u : 0u);
        ((RecordingDispatchProxy<IItemManager>)(object)items).SetProperty(nameof(IItemManager.NextItemId), 205ul);
        var currency = await InstallBankCurrency(player, exists, cap);
        var vendor = RecordingDispatchProxy<IVendorInfo>.Create(out var vp); vp.SetProperty(nameof(IVendorInfo.BuyPriceMultiplier), 1f);
        ((RecordingDispatchProxy<IPlayer>)(object)player).SetProperty(nameof(IPlayer.SelectedVendorInfo), vendor);
        return new(inventory, player, source, currency, new BuybackManager());
    }
    private static bool Sell(VendorState f, uint count = 17)
        => VendorSellHelper.SellItem(f.Player, f.Buyback, new ItemLocation { Location = InventoryLocation.RealmBank, BagIndex = 3 }, f.Item, count);
}
