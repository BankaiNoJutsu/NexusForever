using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Entity;
using NexusForever.Game.Static.Entity;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.Network;
using NexusForever.Network.World.Message.Model;
using NexusForever.WorldServer.Network;
using NexusForever.WorldServer.Network.Message.Handler.Vendor;

namespace NexusForever.Game.Tests.Vendor;

public sealed class ClientBuybackItemFromVendorHandlerTests
{
    [Fact]
    public void DelayedRequestCannotRedeemAnItemSoldAfterThePreviousListWasEmptied()
    {
        var fixture = new Fixture();
        IItem first = CreateItem(100u);
        uint firstId = fixture.Sell(first);
        fixture.Redeem(firstId);
        Assert.Null(fixture.Buyback.GetItem(fixture.Player, firstId));
        Assert.Equal(firstId, Assert.Single(fixture.Messages.OfType<ServerBuybackItemRemoved>()).UniqueId);

        IItem second = CreateItem(101u);
        uint secondId = fixture.Sell(second);
        fixture.Redeem(firstId);

        Assert.Equal(85ul, fixture.Credits);
        Assert.Same(first, Assert.Single(fixture.Deliveries).Arguments[0]);
        Assert.Same(second, fixture.Buyback.GetItem(fixture.Player, secondId)?.Item);

        fixture.Redeem(secondId);
        Assert.Equal(70ul, fixture.Credits);
        Assert.Same(second, fixture.Deliveries[1].Arguments[0]);
        Assert.Null(fixture.Buyback.GetItem(fixture.Player, secondId));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void RedemptionFinishesOnTheOriginalInventoryWhenTheOwnerChangesDuringDebit(bool replaceSessionPlayer)
    {
        var fixture = new Fixture();
        IItem sold = CreateItem(100u);
        uint id = fixture.Sell(sold);
        IInventory replacement = RecordingDispatchProxy<IInventory>.Create(out var replacementBag);
        fixture.OnDebit = () =>
        {
            if (replaceSessionPlayer)
            {
                IPlayer other = RecordingDispatchProxy<IPlayer>.Create(out var actor);
                actor.SetProperty(nameof(IPlayer.Inventory), replacement);
                fixture.Network.SetProperty(nameof(IWorldSession.Player), other);
            }
            else
            {
                fixture.Actor.SetProperty(nameof(IPlayer.Inventory), replacement);
            }
        };

        fixture.Redeem(id);

        Assert.Equal(85ul, fixture.Credits);
        Assert.Same(sold, Assert.Single(fixture.Deliveries).Arguments[0]);
        Assert.Empty(replacementBag.GetInvocations(nameof(IInventory.AddItem)));
        Assert.Null(fixture.Buyback.GetItem(fixture.Player, id));
        Assert.DoesNotContain(fixture.Messages, message => message is ServerBuybackItemRemoved);
    }

    private static IItem CreateItem(uint id)
    {
        IItemInfo info = RecordingDispatchProxy<IItemInfo>.Create(out var metadata);
        metadata.SetProperty(nameof(IItemInfo.Id), id);
        IItem item = RecordingDispatchProxy<IItem>.Create(out var proxy);
        proxy.SetProperty(nameof(IItem.Info), info);
        return item;
    }

    private sealed class Fixture
    {
        public BuybackManager Buyback { get; } = new();
        public IPlayer Player { get; }
        public RecordingDispatchProxy<IPlayer> Actor { get; }
        public RecordingDispatchProxy<IWorldSession> Network { get; }
        public ulong Credits { get; private set; } = 100;
        public Action OnDebit { get; set; }
        public IEnumerable<object> Messages => Network.GetInvocations(nameof(IWorldSession.EnqueueMessageEncrypted))
            .Select(call => call.Arguments[0]);
        public IReadOnlyList<RecordingDispatchProxy<IInventory>.Invocation> Deliveries
            => inventory.GetInvocations(nameof(IInventory.AddItem));

        private readonly IWorldSession session;
        private readonly RecordingDispatchProxy<IInventory> inventory;

        public Fixture()
        {
            session = RecordingDispatchProxy<IWorldSession>.Create(out var network);
            Network = network;
            Player = RecordingDispatchProxy<IPlayer>.Create(out var actor);
            Actor = actor;
            Actor.SetProperty(nameof(IPlayer.CharacterId), 42ul);
            Actor.SetProperty(nameof(IPlayer.Session), session);
            Network.SetProperty(nameof(IWorldSession.Player), Player);
            IInventory bag = RecordingDispatchProxy<IInventory>.Create(out inventory);
            inventory.SetMethodReturn(nameof(IInventory.GetInventorySlotsRemaining), 1u);
            Actor.SetProperty(nameof(IPlayer.Inventory), bag);
            ICurrencyManager wallet = RecordingDispatchProxy<ICurrencyManager>.Create(out var balance);
            balance.SetMethodHandler(nameof(ICurrencyManager.CanAfford), args => Credits >= (ulong)args[1]);
            balance.SetMethodHandler(nameof(ICurrencyManager.CurrencySubtractAmount), args =>
            {
                Credits -= (ulong)args[1];
                OnDebit?.Invoke();
                return null;
            });
            Actor.SetProperty(nameof(IPlayer.CurrencyManager), wallet);
        }

        public uint Sell(IItem item)
        {
            Buyback.AddItem(Player, item, 1u, [(CurrencyType.Credits, 15ul)]);
            return Messages.OfType<ServerBuybackItemUpdated>().Last().BuybackItem.UniqueId;
        }

        public void Redeem(uint uniqueId)
        {
            using var stream = new MemoryStream();
            using (var writer = new GamePacketWriter(stream))
            {
                writer.Write(uniqueId);
                writer.FlushBits();
            }
            using var reader = new GamePacketReader(new MemoryStream(stream.ToArray()));
            var request = new ClientBuybackItemFromVendor();
            request.Read(reader);
            new ClientBuybackItemFromVendorHandler(Buyback).HandleMessage(session, request);
        }
    }
}
