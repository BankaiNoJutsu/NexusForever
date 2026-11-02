using NexusForever.GameTable;

namespace NexusForever.Game.Marketplace
{
    public sealed partial class GlobalMarketplaceManager
    {
        internal sealed record CommodityPostingOrder(CommodityRow Before, CommodityRow After);
        internal sealed record CommodityPostingCredit(ulong Character, ulong Amount);
        internal sealed record CommodityPostingFill(CommodityRow Buy, CommodityRow Sell, uint Quantity, ulong Proceeds, ulong Refund);
        internal sealed record CommodityPostingDeliveryBatch(uint Quantity, bool Returned);

        // Quote the complete submit, preserving price-level/insertion ordering and
        // per-fill fee rounding. No live wallet, inventory or order book is changed.
        internal sealed class CommodityPostingQuote
        {
            internal CommodityRow Incoming { get; }
            internal CommodityRow Remainder { get; }
            internal uint Unmatched { get; }
            internal IReadOnlyList<CommodityPostingOrder> Orders { get; }
            internal IReadOnlyList<CommodityPostingCredit> Credits { get; }
            internal IReadOnlyList<CommodityPostingFill> Fills { get; }
            internal IReadOnlyDictionary<ulong, uint> Deliveries { get; }
            internal uint Consumption => Incoming.Buy ? 0 : Incoming.Quantity;
            internal ulong Debit => Incoming.Buy ? Incoming.Price : 0;

            internal CommodityPostingQuote(CommodityRow incoming, IEnumerable<CommodityRow> candidates, IGameTableManager tables)
            {
                if (incoming == null || incoming.Id == 0 || incoming.Owner == 0 || incoming.Definition == 0
                    || incoming.Quantity == 0 || incoming.UnitPrice == 0 || incoming.Price != checked(incoming.UnitPrice * incoming.Quantity)
                    || incoming.Listed == 0 || incoming.Expiration <= incoming.Listed)
                    throw new InvalidOperationException("Commodity posting lost its complete incoming quote.");
                Incoming = incoming;
                var current = incoming;
                List<CommodityPostingOrder> orders = [];
                List<CommodityPostingCredit> credits = [];
                List<CommodityPostingFill> fills = [];
                Dictionary<ulong, uint> deliveries = [];
                HashSet<ulong> seen = [incoming.Id];
                foreach (var candidate in candidates)
                {
                    if (!seen.Add(candidate.Id) || candidate.Owner == 0 || candidate.Definition != incoming.Definition
                        || candidate.Buy == incoming.Buy || candidate.Immediate || candidate.Quantity == 0 || candidate.UnitPrice == 0
                        || candidate.Price != checked(candidate.UnitPrice * candidate.Quantity))
                        throw new InvalidOperationException("Commodity posting lost a unique compatible resting order.");
                    if (current.Quantity == 0) break;
                    var buy = incoming.Buy ? current : candidate;
                    var sell = incoming.Buy ? candidate : current;
                    if (buy.UnitPrice < sell.UnitPrice)
                        throw new InvalidOperationException("Commodity posting quote contains a noncrossing price.");
                    uint quantity = Math.Min(buy.Quantity, sell.Quantity);
                    ulong cost = checked(sell.UnitPrice * quantity);
                    var buyAfter = buy with { Quantity = buy.Quantity - quantity, Price = checked(buy.UnitPrice * (buy.Quantity - quantity)) };
                    var sellAfter = sell with { Quantity = sell.Quantity - quantity, Price = checked(sell.UnitPrice * (sell.Quantity - quantity)) };
                    ulong refund = checked(buy.Price - checked(cost + buyAfter.Price));
                    ulong proceeds = MarketplaceTransactionFee.CalculateCommoditySellerProceeds(cost, tables);
                    fills.Add(new(buyAfter, sellAfter, quantity, proceeds, refund));
                    if (proceeds != 0) credits.Add(new(sell.Owner, proceeds));
                    if (refund != 0) credits.Add(new(buy.Owner, refund));
                    deliveries[buy.Owner] = checked(deliveries.GetValueOrDefault(buy.Owner) + quantity);
                    var oppositeAfter = incoming.Buy ? sellAfter : buyAfter;
                    orders.Add(new(candidate, oppositeAfter.Quantity == 0 ? null : oppositeAfter));
                    current = incoming.Buy ? buyAfter : sellAfter;
                }
                if (incoming.Immediate && current.Quantity != 0)
                {
                    if (incoming.Buy) credits.Add(new(incoming.Owner, current.Price));
                    else deliveries[incoming.Owner] = checked(deliveries.GetValueOrDefault(incoming.Owner) + current.Quantity);
                }
                Unmatched = current.Quantity;
                Remainder = incoming.Immediate ? current with { Quantity = 0, Price = 0 } : current;
                orders.Add(new(null, incoming.Immediate || current.Quantity == 0 ? null : current));
                Orders = orders.AsReadOnly(); Credits = credits.AsReadOnly(); Fills = fills.AsReadOnly();
                Deliveries = new System.Collections.ObjectModel.ReadOnlyDictionary<ulong, uint>(deliveries);
            }

            internal CommodityPostingDeliveryBatch[] DeliveryBatches(ulong character) => Fills.Where(row => row.Buy.Owner == character)
                .Select(row => new CommodityPostingDeliveryBatch(row.Quantity, false))
                .Concat(Incoming.Immediate && !Incoming.Buy && Incoming.Owner == character && Unmatched != 0 ? [new(Unmatched, true)] : []).ToArray();

            internal ulong WalletAfter(ulong character, ulong before, ulong cap)
            {
                ulong current = character == Incoming.Owner ? checked(before - Debit) : before;
                foreach (var credit in Credits.Where(row => row.Character == character))
                    current = cap == 0 ? checked(current + credit.Amount) : current + Math.Min(credit.Amount, cap - current);
                return current;
            }
        }
    }
}
