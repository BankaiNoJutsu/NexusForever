using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Static.Entity;
using NetworkBuybackItem = NexusForever.Network.World.Message.Model.Shared.BuybackItem;

namespace NexusForever.Game.Entity
{
    public class BuybackItem : IBuybackItem
    {
        public uint UniqueId { get; }
        public IItem Item { get; }
        public uint Quantity { get; }
        public List<(CurrencyType CurrencyTypeId, ulong CurrencyAmount)> CurrencyChange { get; }

        public bool HasExpired => timeToExpire <= 0d;
        private double timeToExpire = 1800d;

        private const int Available = 0, Redeeming = 1, Redeemed = 2;
        private int redemption;
        internal bool IsAvailableForRedemption => Volatile.Read(ref redemption) == Available;
        internal bool IsRedeemed => Volatile.Read(ref redemption) == Redeemed;
        internal bool TryReserveRedemption() => Interlocked.CompareExchange(ref redemption, Redeeming, Available) == Available;
        internal void ReleaseRedemption() => Interlocked.CompareExchange(ref redemption, Available, Redeeming);
        internal void ConsumeRedemption() => Interlocked.Exchange(ref redemption, Redeemed);

        public BuybackItem(uint uniqueId, IItem item, uint quantity, List<(CurrencyType CurrencyTypeId, ulong CurrencyAmount)> currencyChange)
        {
            UniqueId       = uniqueId;
            Item           = item;
            Quantity       = quantity;
            CurrencyChange = currencyChange;
        }

        public void Update(double lastTick)
        {
            timeToExpire -= lastTick;
        }

        public NetworkBuybackItem Build()
        {
            var networkBuybackItem = new NetworkBuybackItem
            {
                UniqueId = UniqueId,
                ItemId   = Item.Info.Id,
                Quantity = Quantity,
            };

            for (int i = 0; i < networkBuybackItem.CurrencyTypeId.Length; i++)
            {
                if (i >= CurrencyChange.Count)
                    continue;

                networkBuybackItem.CurrencyTypeId[i] = CurrencyChange[i].CurrencyTypeId;
                networkBuybackItem.CurrencyAmount[i] = CurrencyChange[i].CurrencyAmount;
            }

            return networkBuybackItem;
        }
    }
}
