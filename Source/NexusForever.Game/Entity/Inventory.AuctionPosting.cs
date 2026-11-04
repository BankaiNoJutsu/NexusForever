using Microsoft.EntityFrameworkCore;
using NexusForever.Database.Character;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Mail;
using NexusForever.Game.Marketplace;
using NexusForever.Game.Static.Entity;

namespace NexusForever.Game.Entity
{
    public partial class Inventory
    {
        private AuctionEscrow auctionEscrow;
        internal AuctionEscrow ReserveAuctionPosting(ulong guid, string database, Func<bool> ownsActor, Func<MailHeaderState> recoveryHeader)
        {
            lock (MutationGate)
            {
                if (!ownsActor() || auctionEscrow != null || commodityDelivery != null || auctionDelivery != null
                    || inventorySaveLeases != 1 || RequiresAuthoritativeReload || rewardOwnerInvalidated || HasPendingRewardDelivery
                    || GetBag(InventoryLocation.Inventory) is not Bag bag || GetItem(guid) is not Item item
                    || item.Location != InventoryLocation.Inventory || item.CharacterId != characterId || item.Soulbound
                    || item.Info.IsEquippableBag())
                    throw new InvalidOperationException("Auction posting requires its original excluded whole Item.");
                var before = item.CaptureCommodityMergePreimage();
                if (!bag.TryReserveRewardPlan(new Dictionary<uint, IItem> { [before.Bag] = item }, out var slots, out var tokens))
                    throw new InvalidOperationException("Auction posting lost its exact source bag slot.");
                IDisposable metadata = null;
                try
                {
                    // The whole Item changes custody, so metadata and cached
                    // codec/rune children must be excluded along with quantity.
                    metadata = item.BeginMarketplacePublication();
                    // Reuse deferred-grant/buffer/recovery ownership without
                    // representing any fungible consumption or delivery.
                    var deferral = new CommodityDelivery(this, bag, item.Info, database, ownsActor, false, recoveryHeader);
                    var held = new AuctionEscrow(this, item, before, slots, tokens[item], ownsActor, deferral, metadata);
                    commodityDelivery = deferral; return auctionEscrow = held;
                }
                catch
                {
                    metadata?.Dispose(); foreach (var token in tokens.Values) token.Dispose(); slots.Dispose(); throw;
                }
            }
        }

        internal sealed class AuctionEscrow(Inventory owner, Item item, GlobalMarketplaceManager.BidItem before,
            Bag.SlotReservation slots, Item.RewardMutationReservation token, Func<bool> ownsActor, CommodityDelivery deferral, IDisposable metadata) : IDisposable
        {
            private bool settled, committed, conflict, retired;
            internal CommodityDelivery Deferral => deferral;
            internal GlobalMarketplaceManager.BidItem Before => before;
            internal GlobalMarketplaceManager.BidItem Target => before with { Owner = null, Location = (ushort)InventoryLocation.None, Bag = 0 };
            internal bool RequiresReload => conflict;
            internal void Settle(bool accepted, GlobalMarketplaceManager.BidItem current, bool listingMatches)
            {
                lock (owner.MutationGate)
                {
                    if (settled || !ownsActor() || !slots.IsCurrent)
                        throw new InvalidOperationException("Auction posting lost its held whole Item.");
                    committed = accepted;
                    conflict = !listingMatches || current != (accepted ? Target : before);
                    if (accepted || conflict)
                    {
                        item.AdoptAuctionPosting(before, accepted ? Target : null, token);
                        slots.PublishCommodityReplacement(null, before.Bag);
                        retired = true;
                        owner.deferredRewardExpirations?.Remove(item);
                    }
                    token.Dispose(); slots.Dispose();
                    if (!retired)
                    {
                        metadata.Dispose();
                        // Known rollback restores inventory authority. Consume
                        // its held interval before the complete ordinary save.
                        var expired = owner.ApplyDeferredRewardExpiration(item);
                        if (expired != null) deferral.Buffer(expired);
                    }
                    settled = true;
                }
            }
            internal AuctionEscrowWitness CaptureWitness(string database, GlobalMarketplaceManager.AuctionRow listing)
                => committed && !conflict ? new(database, owner.characterId, listing, Target) : null;
            internal void RejectCustody() => conflict = true;
            internal void Complete(Func<bool> allowed, Action publishRemoval)
            {
                if (!settled || !ownsActor() || !owner.IsMarketplacePublishing)
                    throw new InvalidOperationException("Auction posting lost its publication owner.");
                if (conflict) { owner.rewardRequiresReload = true; return; }
                if (committed && allowed()) publishRemoval();
            }
            public void Dispose()
            {
                lock (owner.MutationGate)
                {
                    token.Dispose(); slots.Dispose();
                    // A detached original has permanently lost custody. Keep
                    // its metadata/child token so cached references cannot
                    // accept writes; the manager hydrates a distinct escrow Item.
                    if (!retired) metadata.Dispose();
                    if (ReferenceEquals(owner.auctionEscrow, this)) owner.auctionEscrow = null;
                }
            }
        }

        // The parent receipt contains this complete escrow Item/listing target.
        // Lock and validate it through ordinary commit. A null-owner escrow Item
        // cannot be enrolled as a Character-owned row; the listing is enrolled
        // unchanged and the parent hash binds the full Item witness.
        internal sealed class AuctionEscrowWitness(string database, ulong character, GlobalMarketplaceManager.AuctionRow listing,
            GlobalMarketplaceManager.BidItem item)
        {
            internal sealed class CustodyConflict(AuctionEscrowWitness witness) : Exception("Auction posting escrow changed before ordinary reconciliation.")
            { internal AuctionEscrowWitness Witness { get; } = witness; }
            internal void Stage(CharacterContext context)
            {
                CharacterDatabaseIdentity.Require(context, database);
                if (context.AtomicCharacterId != character || context.Database.CurrentTransaction == null || listing.Owner != character)
                    throw new InvalidOperationException("Auction posting witness lost its original Character transaction.");
                bool mysql = context.Database.ProviderName?.Contains("MySql", StringComparison.Ordinal) == true;
                if (!mysql && !context.Database.IsSqlite()) throw new InvalidOperationException("Unsupported auction posting witness provider.");
                var storedListing = (mysql ? context.MarketplaceAuction.FromSqlInterpolated($"SELECT * FROM `marketplace_auction` WHERE id={listing.Id} FOR UPDATE")
                    : context.MarketplaceAuction.Where(row => row.Id == listing.Id)).AsNoTracking().SingleOrDefault();
                _ = (mysql ? context.Item.FromSqlInterpolated($"SELECT * FROM `item` WHERE id={item.Id} FOR UPDATE")
                    : context.Item.Where(row => row.Id == item.Id)).AsNoTracking().SingleOrDefault();
                if (!Matches(context)) throw new CustodyConflict(this);
                context.Attach(storedListing);
                context.OnSaveValidation(_ => { if (!Matches(context)) throw new CustodyConflict(this); });
            }
            private bool Matches(CharacterContext context) =>
                GlobalMarketplaceManager.AuctionRow.Capture(context.MarketplaceAuction.AsNoTracking().SingleOrDefault(row => row.Id == listing.Id)) == listing
                && GlobalMarketplaceManager.BidItem.Capture(context.Item.AsNoTracking().SingleOrDefault(row => row.Id == item.Id)) == item
                && !context.RealmBankItem.Any(row => row.Id == item.Id)
                && !context.CharacterMailAttachment.Any(row => row.ItemGuid == item.Id)
                && !context.MarketplaceAuction.Any(row => row.ItemId == item.Id && row.Id != listing.Id);
        }
    }
    public partial class Item
    {
        internal void AdoptAuctionPosting(GlobalMarketplaceManager.BidItem before, GlobalMarketplaceManager.BidItem after,
            RewardMutationReservation reservation)
        {
            lock (persistenceGate)
            {
                if (!ReferenceEquals(rewardMutationReservation, reservation) || activeSave != null
                    || GlobalMarketplaceManager.BidItem.Capture(persistenceBaseline) != before
                    || after != null && after != (before with { Owner = null, Location = (ushort)InventoryLocation.None, Bag = 0 }))
                    throw new InvalidOperationException("Auction posting lost its original Item mutation capability.");
                reservation.Dispose(); persistenceBaseline = after?.Model(); uncertainStates = null;
                requiresAuthoritativeRewardReload = true; location = InventoryLocation.None;
                characterId = null; bagIndex = 0; saveMask = ItemSaveMask.None;
            }
        }
    }
}
