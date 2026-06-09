using System.Reflection;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging.Abstractions;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Static.Entity;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.GameTable;
using NexusForever.GameTable.Model;
using NexusForever.Network.World.Message.Model;
using NexusForever.WorldServer.Network;
using NexusForever.WorldServer.Network.Message.Handler.Vendor;

namespace NexusForever.Game.Tests.Vendor;

public class ClientVendorRepairHandlerTests
{
    private const uint RepairFormulaId = 0x022Fu;

    [Fact]
    public void HandleMessage_WhenGameFormulaTableMissing_LeavesRepairableItemsUnchanged()
    {
        IItem item = CreateRepairableItem(durability: 0.5f, buyAmount: 100u);
        IWorldSession session = CreateSession([item], out RecordingDispatchProxy<ICurrencyManager> currencyProxy, out RecordingDispatchProxy<IPlayer> playerProxy);
        var handler = new ClientRepairItemVendorHandler(
            NullLogger<ClientRepairItemVendorHandler>.Instance,
            CreateGameTableManager(gameFormulaTable: null));

        handler.HandleMessage(session, new ClientRepairItemVendor());

        Assert.Equal(0.5f, item.Durability);
        Assert.Empty(currencyProxy.GetInvocations(nameof(ICurrencyManager.CanAfford)));
        Assert.Empty(currencyProxy.GetInvocations(nameof(ICurrencyManager.CurrencySubtractAmount)));
        Assert.Empty(playerProxy.GetInvocations(nameof(IPlayer.SendGenericError)));
    }

    [Fact]
    public void HandleMessage_WithFormulaBackedRepairCost_DebitsCreditsAndRepairsItems()
    {
        IItem item = CreateRepairableItem(durability: 0.75f, buyAmount: 100u);
        IWorldSession session = CreateSession([item], out RecordingDispatchProxy<ICurrencyManager> currencyProxy, out RecordingDispatchProxy<IPlayer> playerProxy);
        currencyProxy.SetMethodReturn(nameof(ICurrencyManager.CanAfford), true);
        var handler = new ClientRepairItemVendorHandler(
            NullLogger<ClientRepairItemVendorHandler>.Instance,
            CreateGameTableManager(CreateGameTable(new GameFormulaEntry
            {
                Id          = RepairFormulaId,
                Datafloat03 = 0.5f
            })));

        handler.HandleMessage(session, new ClientRepairItemVendor());

        RecordingDispatchProxy<ICurrencyManager>.Invocation canAffordCall = Assert.Single(currencyProxy.GetInvocations(nameof(ICurrencyManager.CanAfford)));
        Assert.Equal(CurrencyType.Credits, canAffordCall.Arguments[0]);
        Assert.Equal(13ul, canAffordCall.Arguments[1]);

        RecordingDispatchProxy<ICurrencyManager>.Invocation debitCall = Assert.Single(currencyProxy.GetInvocations(nameof(ICurrencyManager.CurrencySubtractAmount)));
        Assert.Equal(CurrencyType.Credits, debitCall.Arguments[0]);
        Assert.Equal(13ul, debitCall.Arguments[1]);
        Assert.Equal(1.0f, item.Durability);
        Assert.Empty(playerProxy.GetInvocations(nameof(IPlayer.SendGenericError)));
    }

    private static IWorldSession CreateSession(
        IReadOnlyList<IItem> inventoryItems,
        out RecordingDispatchProxy<ICurrencyManager> currencyProxy,
        out RecordingDispatchProxy<IPlayer> playerProxy)
    {
        IBag inventoryBag = RecordingDispatchProxy<IBag>.Create(out RecordingDispatchProxy<IBag> bagProxy);
        bagProxy.SetProperty(nameof(IBag.Location), InventoryLocation.Inventory);
        bagProxy.SetMethodHandler(nameof(IEnumerable<IItem>.GetEnumerator), _ => inventoryItems.GetEnumerator());
        IBag[] bags = [inventoryBag];

        IInventory inventory = RecordingDispatchProxy<IInventory>.Create(out RecordingDispatchProxy<IInventory> inventoryProxy);
        inventoryProxy.SetMethodHandler(nameof(IEnumerable<IBag>.GetEnumerator), _ => ((IEnumerable<IBag>)bags).GetEnumerator());

        ICurrencyManager currencyManager = RecordingDispatchProxy<ICurrencyManager>.Create(out currencyProxy);
        IVendorInfo vendorInfo = RecordingDispatchProxy<IVendorInfo>.Create(out _);

        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out playerProxy);
        playerProxy.SetProperty(nameof(IPlayer.Inventory), inventory);
        playerProxy.SetProperty(nameof(IPlayer.CurrencyManager), currencyManager);
        playerProxy.SetProperty(nameof(IPlayer.SelectedVendorInfo), vendorInfo);

        IWorldSession session = RecordingDispatchProxy<IWorldSession>.Create(out RecordingDispatchProxy<IWorldSession> sessionProxy);
        sessionProxy.SetProperty(nameof(IWorldSession.Player), player);
        return session;
    }

    private static IItem CreateRepairableItem(float durability, uint buyAmount)
    {
        IItemInfo info = RecordingDispatchProxy<IItemInfo>.Create(out RecordingDispatchProxy<IItemInfo> infoProxy);
        infoProxy.SetMethodReturn(nameof(IItemInfo.IsEquippable), true);
        infoProxy.SetMethodHandler(nameof(IItemInfo.GetVendorBuyCurrency), args => (byte)args[0] == 0 ? CurrencyType.Credits : CurrencyType.None);
        infoProxy.SetMethodHandler(nameof(IItemInfo.GetVendorBuyAmount), args => (byte)args[0] == 0 ? buyAmount : 0u);
        infoProxy.SetMethodReturn(nameof(IItemInfo.GetVendorSellCurrency), CurrencyType.None);
        infoProxy.SetMethodReturn(nameof(IItemInfo.GetVendorSellAmount), 0u);

        IItem item = RecordingDispatchProxy<IItem>.Create(out RecordingDispatchProxy<IItem> itemProxy);
        itemProxy.SetProperty(nameof(IItem.Info), info);
        itemProxy.SetProperty(nameof(IItem.Durability), durability);
        itemProxy.SetProperty(nameof(IItem.Location), InventoryLocation.Inventory);
        itemProxy.SetProperty(nameof(IItem.BagIndex), 0u);
        return item;
    }

    private static IGameTableManager CreateGameTableManager(GameTable<GameFormulaEntry> gameFormulaTable)
    {
        IGameTableManager gameTableManager = RecordingDispatchProxy<IGameTableManager>.Create(out RecordingDispatchProxy<IGameTableManager> proxy);
        proxy.SetProperty(nameof(IGameTableManager.GameFormula), gameFormulaTable);
        return gameTableManager;
    }

    private static GameTable<T> CreateGameTable<T>(params T[] entries) where T : class, new()
    {
        var table = (GameTable<T>)RuntimeHelpers.GetUninitializedObject(typeof(GameTable<T>));

        typeof(GameTable<T>)
            .GetProperty(nameof(GameTable<T>.Entries), BindingFlags.Instance | BindingFlags.Public)
            ?.SetValue(table, entries);

        FieldInfo idField = typeof(T).GetFields(BindingFlags.Instance | BindingFlags.Public)[0];
        ulong maxId = entries
            .Select(entry => Convert.ToUInt64(idField.GetValue(entry)))
            .DefaultIfEmpty(0ul)
            .Max() + 1ul;

        var lookup = Enumerable.Repeat(-1, (int)maxId).ToArray();
        int index = 0;
        foreach (T entry in entries)
        {
            ulong id = Convert.ToUInt64(idField.GetValue(entry));
            lookup[id] = index++;
        }

        typeof(GameTable<T>)
            .GetField("lookup", BindingFlags.Instance | BindingFlags.NonPublic)
            ?.SetValue(table, lookup);

        typeof(GameTable<T>)
            .GetField("header", BindingFlags.Instance | BindingFlags.NonPublic)
            ?.SetValue(table, new GameTableHeader { MaxId = maxId });

        return table;
    }
}
