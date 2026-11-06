using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging.Abstractions;
using NexusForever.Game.Abstract;
using NexusForever.Game.Abstract.Account;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Marketplace;
using NexusForever.Game.Abstract.RBAC;
using NexusForever.Game.Entity;
using NexusForever.Game.Marketplace;
using NexusForever.Game.Static.Entity;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.Game.Tests.Transport;
using NexusForever.GameTable;
using NexusForever.GameTable.Model;
using NexusForever.Network;
using NexusForever.Network.Message;
using NexusForever.Network.World.Message.Model.Marketplace;
using NexusForever.Network.World.Message.Static;
using NexusForever.Shared.Game.Events;
using NexusForever.WorldServer.Network;
using NexusForever.WorldServer.Network.Message.Handler.Marketplace;

namespace NexusForever.Game.Tests.Marketplace;

public sealed class MarketplaceFallbackReplyTests
{
    [Theory]
    [InlineData("bid")] [InlineData("post")] [InlineData("cancel")]
    [InlineData("commodity-post")] [InlineData("commodity-cancel")]
    public async Task PublicFallbackRejectionRetriesAFreshCapturedReply(string route)
    {
        var f = new Fixture(); int attempts = 0; object failed = null, accepted = null; byte[] expected = null;
        f.SessionProxy.SetMethodHandler(nameof(IWorldSession.EnqueueMessageEncrypted), args =>
        {
            if (args[0] is ServerMarketplaceStatus) return null;
            ++attempts;
            if (attempts == 1)
            {
                failed = args[0]; expected = Wire((IWritable)failed);
                Mutate(failed);
                throw new IOException("Fallback result rejected before acceptance.");
            }
            accepted = args[0];
            return null;
        });
        Assert.Null(Record.Exception(() => f.Dispatch(route)));
        Assert.Equal(1, attempts);
        await f.Drain();
        Assert.Equal(2, attempts); Assert.NotSame(failed, accepted);
        Assert.Equal(expected, Wire((IWritable)accepted));
        f.AssertNoFinancialMutation();
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task FallbackRetryStaysWithItsOriginalPlayerAndQueue(bool replaceQueue)
    {
        var f = new Fixture(); int attempts = 0;
        f.SessionProxy.SetMethodHandler(nameof(IWorldSession.EnqueueMessageEncrypted), args =>
        {
            if (args[0] is ServerMarketplaceStatus) return null;
            ++attempts;
            if (replaceQueue) f.SessionProxy.SetProperty(nameof(IWorldSession.Events), new EventQueue());
            else f.SessionProxy.SetProperty(nameof(IWorldSession.Player), RecordingDispatchProxy<IPlayer>.Create(out _));
            throw new IOException("Fallback result rejected while actor changed.");
        });
        Assert.Null(Record.Exception(() => f.Dispatch("bid")));
        await f.Drain();
        Assert.Equal(1, attempts); f.AssertNoFinancialMutation();
    }

    [Fact]
    public void OrdinaryFallbackRejectionPublishesOnceWithoutQueueWork()
    {
        var f = new Fixture();
        f.Dispatch("cancel");
        var result = Assert.Single(f.Messages.OfType<ServerAuctionCancelResult>());
        Assert.Equal(GenericError.ItemBadId, result.Result);
        Assert.False(f.Events.PendingEvents); f.AssertNoFinancialMutation();
    }

    [Fact]
    public void SuccessfulTransientBidKeepsItsImmediateResultAndSingleDebit()
    {
        var f = new Fixture();
        var seller = RecordingDispatchProxy<IPlayer>.Create(out var owner);
        owner.SetProperty(nameof(IPlayer.CharacterId), 43ul);
        owner.SetProperty(nameof(IPlayer.Inventory), RecordingDispatchProxy<IInventory>.Create(out _));
        f.ItemProxy.SetProperty(nameof(IItem.CharacterId), (ulong?)43ul);
        f.ItemProxy.SetProperty(nameof(IItem.Soulbound), false);
        Assert.Equal(GenericError.Ok, f.Marketplace.PostAuction(seller, f.Item, 100, 250, out var auction));
        f.Currency.SetMethodReturn(nameof(ICurrencyManager.CanAfford), true);
        f.Bid(auction.AuctionId);
        var result = Assert.Single(f.Messages.OfType<ServerAuctionBidResult>());
        Assert.Equal(GenericError.Ok, result.Result);
        var debit = Assert.Single(f.Currency.GetInvocations(nameof(ICurrencyManager.CurrencySubtractAmount)));
        Assert.Equal(100ul, debit.Arguments[1]); Assert.False(f.Events.PendingEvents);
    }

    private static void Mutate(object packet)
    {
        switch (packet)
        {
            case ServerAuctionBidResult row: row.Auction.Item2Id = 999; row.Result = GenericError.Ok; break;
            case ServerAuctionPostResult row: row.Auction.MinimumBid = 999; row.Result = GenericError.Ok; break;
            case ServerAuctionCancelResult row: row.Auction.AuctionId = 999; row.Result = GenericError.Ok; break;
            case ServerCommodityOrderResult row: row.OrderPosted.Item2Id = 999; row.OrderPosted.Quantity = 999; row.Result = GenericError.Ok; break;
            default: throw new InvalidOperationException("Unexpected result packet.");
        }
    }

    private static byte[] Wire(IWritable packet)
    {
        using var bytes = new MemoryStream();
        using (var writer = new GamePacketWriter(bytes)) { packet.Write(writer); writer.FlushBits(); }
        return bytes.ToArray();
    }

    private sealed class Fixture
    {
        internal readonly Player Player = (Player)RuntimeHelpers.GetUninitializedObject(typeof(Player));
        internal readonly EventQueue Events = new();
        internal readonly IWorldSession Session;
        internal readonly RecordingDispatchProxy<IWorldSession> SessionProxy;
        internal readonly RecordingDispatchProxy<IInventory> Inventory;
        internal readonly RecordingDispatchProxy<ICurrencyManager> Currency;
        internal readonly IItem Item;
        internal readonly RecordingDispatchProxy<IItem> ItemProxy;
        internal readonly GlobalMarketplaceManager Marketplace;
        private readonly MarketplaceRequestHelper helper;
        private readonly IGameTableManager tables;
        private readonly IItemManager items;
        private readonly List<Exception> errors = [];
        internal IEnumerable<object> Messages => SessionProxy.GetInvocations(nameof(IWorldSession.EnqueueMessageEncrypted)).Select(row => row.Arguments[0]);

        internal Fixture()
        {
            Session = RecordingDispatchProxy<IWorldSession>.Create(out SessionProxy);
            var account = RecordingDispatchProxy<IAccount>.Create(out var accountProxy);
            accountProxy.SetProperty(nameof(IAccount.Id), 12u); accountProxy.SetProperty(nameof(IAccount.Session), Session);
            accountProxy.SetProperty(nameof(IAccount.RbacManager), RecordingDispatchProxy<IAccountRBACManager>.Create(out _));
            SessionProxy.SetProperty(nameof(IWorldSession.Player), Player); SessionProxy.SetProperty(nameof(IWorldSession.Events), Events);
            SessionProxy.SetProperty(nameof(IWorldSession.Account), account);
            Set(nameof(Player.Identity), new Identity { Id = 42, RealmId = 1 }); Set(nameof(Player.Account), account); Set(nameof(Player.Session), Session);
            Set(nameof(Player.Inventory), RecordingDispatchProxy<IInventory>.Create(out Inventory));
            Set(nameof(Player.CurrencyManager), RecordingDispatchProxy<ICurrencyManager>.Create(out Currency));
            var info = RecordingDispatchProxy<IItemInfo>.Create(out var itemInfo);
            itemInfo.SetProperty(nameof(IItemInfo.Id), 7001u); itemInfo.SetProperty(nameof(IItemInfo.Entry), new Item2Entry { Id = 7001 });
            Item = RecordingDispatchProxy<IItem>.Create(out ItemProxy);
            ItemProxy.SetProperty(nameof(IItem.Id), 7001u); ItemProxy.SetProperty(nameof(IItem.Guid), 900ul); ItemProxy.SetProperty(nameof(IItem.Info), info);
            ItemProxy.SetProperty(nameof(IItem.CharacterId), (ulong?)42ul); ItemProxy.SetProperty(nameof(IItem.Location), InventoryLocation.Inventory);
            ItemProxy.SetProperty(nameof(IItem.Soulbound), true); ItemProxy.SetProperty(nameof(IItem.StackCount), 1u);
            Inventory.SetMethodReturn(nameof(IInventory.GetItem), Item);
            items = RecordingDispatchProxy<IItemManager>.Create(out var itemManager); itemManager.SetMethodReturn(nameof(IItemManager.GetItemInfo), info);
            tables = RecordingDispatchProxy<IGameTableManager>.Create(out var tableProxy);
            tableProxy.SetProperty(nameof(IGameTableManager.GameFormula), PlayerBindPointTests.Table(new GameFormulaEntry { Id = 0x439, Dataint02 = 200 }));
            var players = RecordingDispatchProxy<IPlayerManager>.Create(out var playerManager);
            playerManager.SetMethodReturn(nameof(IPlayerManager.GetPlayer), Player);
            Marketplace = new GlobalMarketplaceManager(null, tables, playerManager: players, itemManager: items);
            helper = new MarketplaceRequestHelper(Marketplace);
            Events.UnhandledExceptionHandler = errors.Add;
        }

        internal void Dispatch(string route)
        {
            switch (route)
            {
                case "bid": Bid(123); break;
                case "post": new ClientAuctionSellOrderSubmitHandler(NullLogger<ClientAuctionSellOrderSubmitHandler>.Instance, helper)
                    .HandleMessage(Session, Read<ClientAuctionSellOrderSubmit>(w => { w.Write(900ul); w.Write(100ul); w.Write(250ul); })); break;
                case "cancel": new ClientAuctionCancelHandler(NullLogger<ClientAuctionCancelHandler>.Instance, items, helper)
                    .HandleMessage(Session, Read<ClientAuctionCancel>(w => { w.Write(7001u, 18u); w.Write(123ul); })); break;
                case "commodity-post": new ClientCommoditySellOrderSubmitHandler(NullLogger<ClientCommoditySellOrderSubmitHandler>.Instance, tables, items, helper)
                    .HandleMessage(Session, Read<ClientCommoditySellOrderSubmit>(w => new CommodityOrder { Item2Id = 7001, Quantity = 2, PricePerUnit = 50, Price = 100, IsBuyOrder = true }.Write(w))); break;
                case "commodity-cancel": new ClientCommodityOrderCancelHandler(NullLogger<ClientCommodityOrderCancelHandler>.Instance, items, helper)
                    .HandleMessage(Session, Read<ClientCommodityOrderCancel>(w => { w.Write(123ul); w.Write(7001u, 18u); w.Write(true); })); break;
                default: throw new InvalidOperationException("Unknown route.");
            }
        }

        internal void Bid(ulong auction) => new ClientAuctionBuyOrderSubmitHandler(NullLogger<ClientAuctionBuyOrderSubmitHandler>.Instance, items, helper)
            .HandleMessage(Session, Read<ClientAuctionBuyOrderSubmit>(w => { w.Write(7001u, 18u); w.Write(auction); w.Write(100ul); }));

        private static T Read<T>(Action<GamePacketWriter> write) where T : IReadable, new()
        {
            using var bytes = new MemoryStream();
            using (var writer = new GamePacketWriter(bytes)) { write(writer); writer.FlushBits(); }
            using var reader = new GamePacketReader(new MemoryStream(bytes.ToArray()));
            var packet = new T(); packet.Read(reader); return packet;
        }

        internal async Task Drain()
        {
            var timeout = Stopwatch.StartNew();
            while (Events.PendingEvents && timeout.Elapsed < TimeSpan.FromSeconds(5)) { Events.Update(0.01); await Task.Delay(10); }
            Assert.False(Events.PendingEvents); Assert.Empty(errors);
        }

        internal void AssertNoFinancialMutation()
        {
            Assert.Empty(Currency.GetInvocations(nameof(ICurrencyManager.CurrencySubtractAmount)));
            Assert.Empty(Currency.GetInvocations(nameof(ICurrencyManager.CurrencyAddAmount)));
            Assert.Empty(Inventory.GetInvocations(nameof(IInventory.ItemRemove)));
            Assert.Empty(Inventory.GetInvocations(nameof(IInventory.AddItem)));
        }

        private void Set(string property, object value)
        {
            var info = typeof(Player).GetProperty(property)!;
            if (info.SetMethod != null) info.SetValue(Player, value);
            else info.DeclaringType!.GetField("<" + property + ">k__BackingField", BindingFlags.NonPublic | BindingFlags.Instance)!.SetValue(Player, value);
        }
    }
}
