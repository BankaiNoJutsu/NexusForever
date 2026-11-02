using Microsoft.EntityFrameworkCore;
using NexusForever.Database.Character;
using NexusForever.Database.Character.Model;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Mail;
using NexusForever.Game.Marketplace;
using NexusForever.Game.Static.Entity;
using NexusForever.Network.World.Message.Model;
using NexusForever.Network.World.Message.Model.Shared;
using NexusForever.Network.World.Message.Static;

namespace NexusForever.Game.Entity
{
    public partial class Inventory
    {
        private AuctionDelivery auctionDelivery;
        internal AuctionDelivery ReserveAuctionDelivery(GlobalMarketplaceManager.BidItem escrow, Func<bool> ownsActor)
        {
            lock (MutationGate)
            {
                if (!ownsActor() || auctionDelivery != null || inventorySaveLeases != 1 || RequiresAuthoritativeReload || rewardOwnerInvalidated
                    || HasPendingRewardDelivery || player == null || escrow.Owner != null
                    || GetBag(InventoryLocation.Inventory) is not Bag bag)
                    throw new InvalidOperationException("Auction delivery requires its original excluded inventory.");
                uint? index = bag.GetFirstAvailableBagIndex();
                Bag.SlotReservation slot = null;
                if (index.HasValue && !bag.TryReserveSlots(new Dictionary<uint, IItem> { [index.Value] = null }, out slot))
                    throw new InvalidOperationException("Auction delivery lost its exact empty slot.");
                return auctionDelivery = new(this, bag, slot, escrow with { Owner = characterId,
                    Location = (ushort)(index.HasValue ? InventoryLocation.Inventory : InventoryLocation.None), Bag = index ?? 0 }, ownsActor);
            }
        }

        internal sealed class AuctionDelivery(Inventory owner, Bag bag, Bag.SlotReservation slot,
            GlobalMarketplaceManager.BidItem target, Func<bool> ownsActor) : IDisposable
        {
            private bool settled, conflict, committedDelivery;
            private Item admitted;
            private string initializedRunes;
            private MailHeaderState mail;
            internal GlobalMarketplaceManager.BidItem Target { get; } = target;
            internal bool ByMail => Target.Location == (ushort)InventoryLocation.None;
            internal bool RequiresReload => conflict;
            internal bool DefersExpiration => admitted != null && !conflict;

            internal void Settle(bool committed, GlobalMarketplaceManager.BidItem current, bool deliveryMatches, MailHeaderState deliveryMail)
            {
                lock (owner.MutationGate)
                {
                    if (settled || !ownsActor()) throw new InvalidOperationException("Auction delivery lost its original owner.");
                    committedDelivery = committed;
                    if (committed)
                    {
                        mail = deliveryMail;
                        conflict = !deliveryMatches || current != Target;
                        if (!conflict && !ByMail)
                        {
                            if (slot?.IsCurrent != true) throw new InvalidOperationException("Auction delivery lost its held bag slot.");
                            admitted = new Item(Target.Model(), owner.itemManager, owner.gameTableManager);
                            admitted.AdoptAuctionInventoryLocation();
                            ItemRuneSlotInitializer.ApplyDefaultSockets(admitted, owner.gameTableManager);
                            initializedRunes = ItemRuneSlotsCodec.Serialize(admitted.RuneSlots);
                            slot.PublishItem(admitted, Target.Bag);
                        }
                    }
                    slot?.Dispose(); settled = true;
                }
            }

            internal AuctionDeliveryWitness CaptureWitness(string database)
            {
                lock (owner.MutationGate)
                {
                    if (!settled || !ownsActor()) throw new InvalidOperationException("Auction delivery lost its save witness owner.");
                    if (conflict || !committedDelivery) return null;
                    return new(database, admitted == null ? Target : GlobalMarketplaceManager.BidItem.Capture(admitted.CaptureAuctionDeliveryPreimage()), mail,
                        initializedRunes);
                }
            }

            internal void RejectCustody()
            {
                lock (owner.MutationGate)
                {
                    if (!settled || !ownsActor()) throw new InvalidOperationException("Auction custody conflict lost its owner.");
                    if (admitted != null)
                    {
                        if (!ReferenceEquals(bag.GetItem(admitted.BagIndex), admitted) || admitted.HasPendingSave)
                            throw new InvalidOperationException("Auction custody conflict cannot detach another inventory owner.");
                        bag.RemoveItem(admitted); admitted = null;
                    }
                    conflict = true;
                }
            }

            internal void Complete(bool publish)
            {
                lock (owner.MutationGate)
                {
                    if (!settled || !ownsActor()) throw new InvalidOperationException("Auction delivery has not reconciled.");
                    if (conflict) { owner.rewardRequiresReload = true; return; }
                    if (publish && admitted != null && ReferenceEquals(bag.GetItem(Target.Bag), admitted))
                        owner.EnqueueItemMessage(new ServerItemAdd { InventoryItem = new InventoryItem
                            { Item = admitted.Build(), Reason = ItemUpdateReason.Auction } });
                }
            }

            public void Dispose()
            {
                lock (owner.MutationGate)
                {
                    slot?.Dispose();
                    if (ReferenceEquals(owner.auctionDelivery, this)) owner.auctionDelivery = null;
                }
            }
        }

        // Immutable SQL-only witness. It keeps no actor, Item, mailbox or callback.
        // The complete ordinary receipt includes these tracked unchanged rows.
        internal sealed class AuctionDeliveryWitness(string database, GlobalMarketplaceManager.BidItem target, MailHeaderState mail, string initializedRunes = null)
        {
            internal ItemModel ItemPreimage => target.Model();
            internal ItemModel ItemCodecPostimage
            {
                get
                {
                    var model = target.Model();
                    List<uint> chips = []; ItemMicrochipIdsCodec.DeserializeInto(model.MicrochipIds, chips);
                    List<ItemRuneSlot> runes = []; ItemRuneSlotsCodec.DeserializeInto(model.RuneSlots, runes);
                    model.MicrochipIds = ItemMicrochipIdsCodec.Serialize(chips);
                    model.RuneSlots = runes.Count == 0 && initializedRunes != null
                        ? initializedRunes : ItemRuneSlotsCodec.Serialize(runes);
                    return model;
                }
            }
            internal sealed class CustodyConflict(AuctionDeliveryWitness witness) : Exception("Auction delivery custody changed before ordinary reconciliation.")
            {
                internal AuctionDeliveryWitness Witness { get; } = witness;
            }

            internal void Stage(CharacterContext context)
            {
                CharacterDatabaseIdentity.Require(context, database);
                if (context.AtomicCharacterId != target.Owner || context.Database.CurrentTransaction == null)
                    throw new InvalidOperationException("Auction delivery witness requires its owned Character transaction.");
                bool mysql = context.Database.ProviderName?.Contains("MySql", StringComparison.Ordinal) == true;
                if (!mysql && !context.Database.IsSqlite()) throw new InvalidOperationException("Unsupported auction delivery provider.");
                var item = (mysql ? context.Item.FromSqlInterpolated($"SELECT * FROM `item` WHERE id={target.Id} FOR UPDATE")
                    : context.Item.Where(row => row.Id == target.Id)).AsNoTracking().SingleOrDefault();
                CharacterMailModel header = null; CharacterMailAttachmentModel[] links = [];
                if (mail != null)
                {
                    header = (mysql ? context.CharacterMail.FromSqlInterpolated($"SELECT * FROM `character_mail` WHERE id={mail.Id} FOR UPDATE")
                        : context.CharacterMail.Where(row => row.Id == mail.Id)).AsNoTracking().SingleOrDefault();
                    links = (mysql ? context.CharacterMailAttachment.FromSqlInterpolated($"SELECT * FROM `character_mail_attachment` WHERE id={mail.Id} FOR UPDATE")
                        : context.CharacterMailAttachment.Where(row => row.Id == mail.Id)).AsNoTracking().ToArray();
                }
                if (GlobalMarketplaceManager.BidItem.Capture(item) != target || !ExclusionsMatch(context)
                    || mail != null && (MailHeaderState.From(header) != mail || links.Length != 1 || links[0].Index != 0 || links[0].ItemGuid != target.Id))
                    throw new CustodyConflict(this);
                context.Attach(item);
                if (header != null) { context.Attach(header); context.Attach(links[0]); }
                context.OnSaveValidation(_ =>
                {
                    if (!ExclusionsMatch(context)) throw new InvalidOperationException("Auction delivery changed its sealed custody exclusions.");
                });
            }

            private bool ExclusionsMatch(CharacterContext context) => !context.RealmBankItem.Any(row => row.Id == target.Id)
                && !context.MarketplaceAuction.Any(row => row.ItemId == target.Id)
                && !context.CharacterMailAttachment.Any(row => row.ItemGuid == target.Id && (mail == null || row.Id != mail.Id || row.Index != 0))
                && (mail == null ? !context.Item.Any(row => row.Id != target.Id && row.OwnerId == target.Owner
                    && row.Location == target.Location && row.BagIndex == target.Bag)
                    : context.CharacterMailAttachment.Count(row => row.Id == mail.Id) == 1);
        }
    }
}
