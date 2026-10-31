using System.Runtime.CompilerServices;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Entity;
using NexusForever.Game.Loot;
using NexusForever.Game.Trade;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.Network.Session;
using NexusForever.Network.World.Message.Model;
using NexusForever.Network.World.Message.Model.Shared;
using NexusForever.WorldServer.Network;
using NexusForever.WorldServer.Network.Message.Handler.Item;
using NexusForever.WorldServer.Network.Message.Handler.Vendor;

namespace NexusForever.Game.Tests.Entity;

public sealed class InventoryMutationScopeTests
{
    [Fact]
    public void FailedSecondParticipantReleasesTheFirstLogicalLease()
    {
        var firstLease = new CountingLease();
        var first = Inventory(true, firstLease, out _);
        var second = Inventory(false, null, out _);
        Assert.False(InventoryMutationScope.TryAcquire(new[] { first, second }, out var held));
        Assert.Null(held);
        Assert.Equal(1, firstLease.Disposals);
    }

    [Fact]
    public void DuplicateParticipantIsAcquiredAndReleasedExactlyOnce()
    {
        var lease = new CountingLease();
        var inventory = Inventory(true, lease, out var proxy);
        Assert.True(InventoryMutationScope.TryAcquire(new[] { inventory, inventory }, out var held));
        held.Dispose(); held.Dispose();
        Assert.Single(proxy.GetInvocations(nameof(IInventory.TryAcquireMutationLease)));
        Assert.Equal(1, lease.Disposals);
    }

    [Fact]
    public void ThrownSecondAcquisitionStillReleasesEarlierParticipants()
    {
        var lease = new CountingLease();
        var first = Inventory(true, lease, out _);
        var second = Inventory(false, null, out var proxy);
        proxy.SetMethodHandler(nameof(IInventory.TryAcquireMutationLease), _ => throw new InvalidOperationException("Owned scope failure."));
        Assert.Throws<InvalidOperationException>(() => InventoryMutationScope.TryAcquire(new[] { first, second }, out _));
        Assert.Equal(1, lease.Disposals);
    }

    [Fact]
    public void LegacyAdapterKeepsItsExistingBehaviorWithoutOptingIntoTheNewProtocol()
    {
        IInventory legacy = RecordingDispatchProxy<IInventory>.Create(out var proxy);
        Assert.True(InventoryMutationScope.TryAcquire(legacy, out var held));
        held.Dispose();
        Assert.Empty(proxy.GetInvocations(nameof(IInventory.TryAcquireMutationLease)));
    }

    [Fact]
    public void TradeSettlementChecksBothOwnersBeforeAnyItemOrCreditTransfer()
    {
        var lease = new CountingLease();
        IInventory first = Inventory(true, lease, out var firstProxy);
        IInventory second = Inventory(false, null, out var secondProxy);
        ICurrencyManager currency = RecordingDispatchProxy<ICurrencyManager>.Create(out var cp);
        cp.SetMethodReturn(nameof(ICurrencyManager.CanAfford), true);
        IPlayer initiator = TradePlayer(1, first, currency, out _);
        IPlayer target = TradePlayer(2, second, currency, out _);
        var manager = new TradeManager();
        Assert.Null(manager.Initiate(initiator, target));
        Assert.Null(manager.Accept(target));
        Assert.Null(manager.SetMoney(initiator, 1));
        Assert.Null(manager.Commit(initiator));
        Assert.Null(manager.Commit(target));
        Assert.Single(firstProxy.GetInvocations(nameof(IInventory.TryAcquireMutationLease)));
        Assert.Single(secondProxy.GetInvocations(nameof(IInventory.TryAcquireMutationLease)));
        Assert.Empty(cp.GetInvocations(nameof(ICurrencyManager.CurrencySubtractAmount)));
        Assert.Empty(cp.GetInvocations(nameof(ICurrencyManager.CurrencyAddAmount)));
        Assert.Empty(firstProxy.GetInvocations(nameof(IInventory.ItemRemove)));
        Assert.Empty(secondProxy.GetInvocations(nameof(IInventory.AddItem)));
        Assert.Equal(1, lease.Disposals);
    }

    [Fact]
    public void ActivatedItemRejectsBeforeSpellOrConsumptionEffects()
    {
        var f = Blocked();
        IItemInfo info = RecordingDispatchProxy<IItemInfo>.Create(out _);
        IItem item = RecordingDispatchProxy<IItem>.Create(out var ip); ip.SetProperty(nameof(IItem.Info), info);
        f.InventoryProxy.SetMethodReturn(nameof(IInventory.GetItem), item);
        new ClientItemUseHandler(null).HandleMessage(f.Session, new ClientItemUse());
        Assert.Empty(f.PlayerProxy.GetInvocations(nameof(IPlayer.TryCastSpell)));
        Assert.Empty(f.InventoryProxy.GetInvocations(nameof(IInventory.ItemUse)));
    }

    [Fact]
    public void DecorRejectsBeforeResidenceCreationOrConsumption()
    {
        var f = Blocked();
        new ClientItemUseDecorHandler(null, null).HandleMessage(f.Session, new ClientItemUseDecor());
        Assert.Empty(f.PlayerProxy.GetInvocations("get_ResidenceManager"));
        Assert.Empty(f.InventoryProxy.GetInvocations(nameof(IInventory.ItemUse)));
    }

    [Fact]
    public void VendorPurchaseAndBuybackRejectBeforeStockWalletOrBuybackMutation()
    {
        var f = Blocked();
        new ClientVendorPurchaseHandler(null).HandleMessage(f.Session, new ClientVendorPurchase());
        new ClientBuybackItemFromVendorHandler(null).HandleMessage(f.Session, new ClientBuybackItemFromVendor());
        Assert.Empty(f.PlayerProxy.GetInvocations("get_SelectedVendorInfo"));
        Assert.Empty(f.PlayerProxy.GetInvocations("get_CurrencyManager"));
        Assert.Empty(f.InventoryProxy.GetInvocations(nameof(IInventory.AddItem)));
        Assert.Empty(f.InventoryProxy.GetInvocations(nameof(IInventory.ItemCreate)));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void DirectLootServicesRejectBeforeGeneratingRewardsOrConsumingTheSource(bool salvage)
    {
        var f = Blocked();
        var loot = (GlobalLootManager)RuntimeHelpers.GetUninitializedObject(typeof(GlobalLootManager));
        IItem item = RecordingDispatchProxy<IItem>.Create(out _);
        string reason;
        bool used = salvage ? loot.TrySalvageItem(f.Player, item, out reason) : loot.TryUseLootBag(f.Player, item, out reason);
        Assert.False(used);
        Assert.Equal("inventory-busy", reason);
        Assert.Empty(f.InventoryProxy.GetInvocations(nameof(IInventory.ItemCreate)));
        Assert.Empty(f.InventoryProxy.GetInvocations(nameof(IInventory.ItemUse)));
    }

    private static IInventory Inventory(bool acquire, IDisposable lease, out RecordingDispatchProxy<IInventory> proxy)
    {
        IInventory inventory = RecordingDispatchProxy<IInventory>.Create(out proxy);
        proxy.SetProperty(nameof(IInventory.SupportsTransactionalSoulboundDelivery), true);
        proxy.SetMethodHandler(nameof(IInventory.TryAcquireMutationLease), arguments => { arguments[0] = lease; return acquire; });
        return inventory;
    }

    private static IPlayer TradePlayer(uint id, IInventory inventory, ICurrencyManager currency, out RecordingDispatchProxy<IPlayer> proxy)
    {
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out proxy);
        proxy.SetProperty(nameof(IPlayer.Guid), id);
        proxy.SetProperty(nameof(IPlayer.InWorld), true); proxy.SetProperty(nameof(IPlayer.IsAlive), true);
        proxy.SetProperty(nameof(IPlayer.Inventory), inventory); proxy.SetProperty(nameof(IPlayer.CurrencyManager), currency);
        proxy.SetProperty(nameof(IPlayer.Session), RecordingDispatchProxy<IGameSession>.Create(out _));
        return player;
    }

    private static (IWorldSession Session, IPlayer Player, RecordingDispatchProxy<IPlayer> PlayerProxy, RecordingDispatchProxy<IInventory> InventoryProxy) Blocked()
    {
        IInventory inventory = Inventory(false, null, out var ip);
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out var pp); pp.SetProperty(nameof(IPlayer.Inventory), inventory);
        IWorldSession session = RecordingDispatchProxy<IWorldSession>.Create(out var sp); sp.SetProperty(nameof(IWorldSession.Player), player);
        return (session, player, pp, ip);
    }

    private sealed class CountingLease : IDisposable
    {
        public int Disposals { get; private set; }
        public void Dispose() => ++Disposals;
    }
}
