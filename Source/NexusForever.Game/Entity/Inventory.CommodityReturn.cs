using Microsoft.EntityFrameworkCore;
using NexusForever.Database.Character;
using NexusForever.Database.Character.Model;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Mail;
using NexusForever.Game.Marketplace;
using NexusForever.Game.Static.Entity;
using NexusForever.Network.Message;
using NexusForever.Network.World.Message.Model;
using NexusForever.Network.World.Message.Model.Shared;
using NexusForever.Network.World.Message.Static;

namespace NexusForever.Game.Entity
{
    public partial class Inventory
    {
        private CommodityDelivery commodityDelivery;
        internal bool BufferCommodityMessage(IWritable message)
        {
            if (commodityDelivery == null) return false;
            commodityDelivery.Buffer(message); return true;
        }

        internal CommodityDelivery ReserveCommodityReturn(uint definition, uint count, string database, Func<bool> ownsActor, Func<MailHeaderState> recoveryHeader)
        {
            lock (MutationGate)
            {
                if (!ownsActor() || commodityDelivery != null || inventorySaveLeases != 1 || RequiresAuthoritativeReload
                    || rewardOwnerInvalidated || HasPendingRewardDelivery || GetBag(InventoryLocation.Inventory) is not Bag bag)
                    throw new InvalidOperationException("Commodity return requires its original excluded inventory.");
                var info = itemManager?.GetItemInfo(definition) ?? throw new InvalidOperationException("Commodity return lost its Item definition.");
                uint max = info.IsStackable() ? info.Entry.MaxStackCount : 1;
                if (max == 0 || count == 0) throw new InvalidOperationException("Commodity return has an invalid stack definition.");
                bool mail = bag.SlotsRemaining < ((ulong)count + max - 1) / max;
                var held = new CommodityDelivery(this, bag, info, database, ownsActor, mail, recoveryHeader);
                try
                {
                    if (mail)
                    {
                        held.writes.Add(new(null, GlobalMarketplaceManager.BidItem.Capture(new Item(characterId, info, count,
                            itemManager: itemManager, gameTableManager: gameTableManager).CaptureRewardModel())));
                    }
                    else
                    {
                        if (player.SupplySatchelManager?.TryGetAutoStoreCapacity(info, out _) == true
                            && player.SupplySatchelManager is not SupplySatchelManager)
                            throw new InvalidOperationException("Commodity return requires its concrete satchel owner.");
                        held.satchel = (player.SupplySatchelManager as SupplySatchelManager)?.ReserveCommodityReturn(info, count, database);
                        uint remaining = count - (held.satchel == null ? 0u : (uint)(held.satchel.Write.After - held.satchel.Write.Before));
                        uint expiration = GetInitialExpirationTimeLeft(info);
                        var expected = new Dictionary<uint, IItem>();
                        if (info.IsStackable())
                            foreach (var candidate in bag.Where(row => !bag.IsSlotReserved(row.BagIndex) && row.Info.Id == definition && row.ExpirationTimeLeft == expiration))
                            {
                                if (remaining == 0) break;
                                if (candidate is not Item item) throw new InvalidOperationException("Commodity return requires concrete merge Items.");
                                var before = item.CaptureCommodityMergePreimage();
                                if (before.Quantity > max) throw new InvalidOperationException("Commodity return cannot merge an invalid stored stack.");
                                uint add = Math.Min(remaining, max - before.Quantity); if (add == 0) continue;
                                expected.Add(item.BagIndex, item); held.residents.Add(item.Guid, item);
                                held.writes.Add(new(before, before with { Quantity = before.Quantity + add })); remaining -= add;
                            }
                        var snapshot = bag.CreateSnapshot();
                        for (uint index = 0; remaining != 0 && index < snapshot.Length; index++)
                        {
                            if (snapshot[index] != null || bag.IsSlotReserved(index)) continue;
                            uint quantity = Math.Min(remaining, max);
                            var template = new Item(characterId, info, quantity, itemManager: itemManager, gameTableManager: gameTableManager).CaptureRewardModel();
                            template.Location = (ushort)InventoryLocation.Inventory; template.BagIndex = index;
                            expected.Add(index, null); held.writes.Add(new(null, GlobalMarketplaceManager.BidItem.Capture(template))); remaining -= quantity;
                        }
                        if (remaining != 0) throw new InvalidOperationException("Commodity return lost its full destination capacity.");
                        if (expected.Count != 0 && !bag.TryReserveRewardPlan(expected, out held.slots, out held.merges))
                            throw new InvalidOperationException("Commodity return lost its exact merge/empty slots.");
                    }
                    held.writes.Sort((left, right) => left.After.Id.CompareTo(right.After.Id));
                    return commodityDelivery = held;
                }
                catch { held.Dispose(); throw; }
            }
        }

        internal sealed partial class CommodityDelivery : IDisposable
        {
            private readonly Inventory owner;
            private readonly Bag bag;
            private readonly IItemInfo info;
            private readonly string database;
            private readonly Func<bool> ownsActor;
            internal readonly List<GlobalMarketplaceManager.CommodityItemWrite> writes = [];
            internal readonly Dictionary<ulong, Item> residents = [];
            internal Bag.SlotReservation slots;
            internal Dictionary<Item, Item.RewardMutationReservation> merges = [];
            internal SupplySatchelManager.CommodityReturnReservation satchel;
            private readonly List<IWritable> buffered = [];
            private readonly Dictionary<ulong, string> initializedRunes = [];
            private sealed record Creation(InventoryLocation Location, IItemInfo Definition, uint Count, ItemUpdateReason Reason, uint Charges);
            private readonly Queue<Creation> laterCreation = [];
            private readonly Func<MailHeaderState> recoveryHeader;
            private readonly List<CommodityRecoveryMail> recovery = [];
            private MailHeaderState mail;
            private CommodityDeliveryWitness pendingWitness;
            private bool settled, committed, conflict, disposed, draining;
            internal bool Publishing => owner.IsMarketplacePublishing;
            private readonly bool byMail;
            internal bool ByMail => byMail || posting?.Any(row => row.After?.Location == (ushort)InventoryLocation.None) == true;
            internal bool RequiresReload => conflict;
            internal bool DefersExpiration => settled && !conflict && residents.Count != 0;
            internal GlobalMarketplaceManager.CommodityItemWrite[] Writes => writes.ToArray();
            internal GlobalMarketplaceManager.CommodityMaterialWrite Material => satchel?.Write;
            internal CommodityDelivery(Inventory owner, Bag bag, IItemInfo info, string database, Func<bool> ownsActor, bool byMail, Func<MailHeaderState> recoveryHeader)
            { this.owner = owner; this.bag = bag; this.info = info; this.database = database; this.ownsActor = ownsActor; this.byMail = byMail; this.recoveryHeader = recoveryHeader; }

            internal bool DeferCreation(InventoryLocation location, IItemInfo definition, uint count, ItemUpdateReason reason, uint charges)
            {
                if (Publishing) throw new InvalidOperationException("Commodity publication cannot reenter Item creation.");
                if (draining) return false;
                if (definition == null) throw new ArgumentNullException(nameof(definition));
                if (count != 0) laterCreation.Enqueue(new(location, definition, count, reason, charges)); return true;
            }
            internal void Buffer(IWritable message)
            {
                // The owned material publishes its final confirmed amount once.
                if (message is ServerSupplySatchelUpdate update && update.MaterialId == satchel?.Write.Id) return;
                buffered.Add(message);
            }

            internal void Settle(bool committed, GlobalMarketplaceManager.BidItem[] current, ushort? material, bool deliveryMatches, MailHeaderState mail,
                IReadOnlyDictionary<ulong, MailHeaderState> itemMails = null)
            {
                if (posting != null) { SettlePosting(committed, current, material, deliveryMatches, itemMails); return; }
                lock (owner.MutationGate)
                {
                    if (settled || !ownsActor()) throw new InvalidOperationException("Commodity delivery lost its original actor.");
                    this.committed = committed; this.mail = mail;
                    conflict = committed && (!deliveryMatches || current == null || !current.SequenceEqual(writes.Select(row => row.After)));
                    if (!committed && current != null && !current.SequenceEqual(writes.Select(row => row.Before))) conflict = true;
                    if (!conflict && committed && !ByMail)
                    {
                        if (writes.Count != 0 && slots?.IsCurrent != true) throw new InvalidOperationException("Commodity return lost its held slots.");
                        foreach (var write in writes)
                        {
                            if (write.Before != null) residents[write.After.Id].AdoptCommodityMerge(write.Before, write.After, merges[residents[write.After.Id]]);
                            else
                            {
                                var item = new Item(write.After.Model(), owner.itemManager, owner.gameTableManager); item.AdoptAuctionInventoryLocation();
                                slots.PublishItem(item, write.After.Bag); residents.Add(item.Guid, item);
                            }
                        }
                    }
                    foreach (var merge in merges.Values) merge.Dispose(); merges.Clear(); slots?.Dispose();
                    if (committed && !conflict && !ByMail)
                        foreach (var item in residents.Values)
                        {
                            ItemRuneSlotInitializer.ApplyDefaultSockets(item, owner.gameTableManager);
                            initializedRunes.Add(item.Guid, ItemRuneSlotsCodec.Serialize(item.RuneSlots));
                        }
                    if (!committed && current == null && satchel != null) material = satchel.Write.Exists ? satchel.Write.Before : null;
                    satchel?.Settle(material);
                    settled = true;
                    if (conflict) RejectCustody();
                    PrepareNextReconciliation();
                    if (!conflict && committed && !ByMail && !owner.player.IsLoading) owner.RefreshCollectedItem(info.Id);
                }
            }

            internal bool PrepareNextReconciliation()
            {
                if (!settled || !ownsActor()) throw new InvalidOperationException("Deferred creation lost its original actor.");
                bool changed = laterCreation.Count != 0;
                draining = true;
                try
                {
                    while (laterCreation.TryPeek(out var creation))
                    {
                        if (!conflict) owner.ItemCreate(creation.Location, creation.Definition, creation.Count, creation.Reason, creation.Charges);
                        else
                        {
                            uint max = creation.Definition.IsStackable() ? creation.Definition.Entry.MaxStackCount : 1;
                            if (max == 0) throw new InvalidOperationException("Deferred creation has an invalid stack definition.");
                            uint remaining = creation.Count;
                            List<GlobalMarketplaceManager.BidItem> batch = [];
                            while (remaining != 0)
                            {
                                uint count = Math.Min(remaining, max);
                                batch.Add(GlobalMarketplaceManager.BidItem.Capture(new Item(owner.characterId, creation.Definition, count, creation.Charges,
                                    owner.itemManager, owner.gameTableManager).CaptureRewardModel()));
                                remaining -= count;
                                if (batch.Count == 10 || remaining == 0)
                                {
                                    recovery.Add(new(database, recoveryHeader(), batch.ToArray())); batch.Clear();
                                }
                            }
                        }
                        laterCreation.Dequeue();
                    }
                }
                finally { draining = false; }
                return changed;
            }

            internal CommodityDeliveryWitness CaptureWitness()
            {
                if (!settled || !ownsActor()) throw new InvalidOperationException("Commodity return lost its ordinary witness owner.");
                if ((!committed || conflict) && recovery.Count == 0 && satchel == null) return null;
                return pendingWitness = new(database, owner.characterId, (conflict || !committed ? [] : writes.Select(row => new AuctionDeliveryWitness(database,
                    residents.TryGetValue(row.After.Id, out var item) ? GlobalMarketplaceManager.BidItem.Capture(item.CaptureAuctionDeliveryPreimage()) : row.After,
                    row.After.Location == (ushort)InventoryLocation.None ? posting == null ? mail : postingItemMails.GetValueOrDefault(row.After.Id) : null,
                    initializedRunes.GetValueOrDefault(row.After.Id))).ToArray()),
                    satchel?.Write.Id, satchel?.Current, satchel?.Desired, recovery.ToArray(),
                    committed && !conflict ? posting?.Where(row => row.After == null).Select(row => row.Id).ToArray() : null);
            }
            internal CommodityDeliveryWitness RebaseMaterial(CommodityDeliveryWitness witness, ushort? current)
            {
                if (!ownsActor() || !ReferenceEquals(pendingWitness, witness) || satchel == null)
                    throw new InvalidOperationException("Material conflict belongs to another delivery.");
                satchel.Rebase(current); return CaptureWitness();
            }
            internal void ConfirmWitness(CommodityDeliveryWitness witness)
            {
                if (!ownsActor() || !ReferenceEquals(pendingWitness, witness))
                    throw new InvalidOperationException("Commodity save belongs to another delivery witness.");
                if (satchel != null && witness.MaterialSettled) satchel.ConfirmSave(witness.MaterialSaved);
                recovery.RemoveAll(mail => witness.Recovery.Contains(mail));
            }
            internal IReadOnlyDictionary<ulong, uint> PreviewRecoveryQuantities()
            {
                if (!settled || !ownsActor()) throw new InvalidOperationException("Commodity recovery lost its original actor.");
                Dictionary<ulong, uint> quantities = [];
                foreach (var item in residents.Values)
                {
                    var before = item.CaptureAuctionDeliveryPreimage();
                    if (item.StackCount < before.StackCount)
                        throw new InvalidOperationException("Commodity custody cannot discard an unrelated consumption intent.");
                    if (item.StackCount > before.StackCount) quantities.Add(item.Guid, item.StackCount);
                }
                return quantities;
            }
            internal void ObserveRollback(BidReconciliationRolledBackException proof)
            {
                if (!ownsActor()) throw new InvalidOperationException("Commodity rollback lost its original actor.");
                foreach (var item in residents.Values) item.ObserveCommodityRollback(proof);
            }

            internal void RejectCustody()
            {
                foreach (var item in residents.Values)
                {
                    if (!ReferenceEquals(bag.GetItem(item.BagIndex), item) || item.HasPendingSave)
                        throw new InvalidOperationException("Commodity return cannot detach another Item owner.");
                    var before = item.CaptureAuctionDeliveryPreimage();
                    if (item.StackCount < before.StackCount)
                        throw new InvalidOperationException("Commodity custody cannot discard an unrelated consumption intent.");
                    uint quantity = item.StackCount - before.StackCount;
                    if (quantity != 0)
                    {
                        var transfer = GlobalMarketplaceManager.BidItem.Capture(before) with { Id = owner.itemManager.NextItemId,
                            Location = (ushort)InventoryLocation.None, Bag = 0, Quantity = quantity };
                        recovery.Add(new(database, recoveryHeader(), [transfer]));
                    }
                    bag.RemoveItem(item);
                }
                residents.Clear(); conflict = true;
                PrepareNextReconciliation();
            }
            internal void Complete(Func<bool> allowed)
            {
                if (posting != null) { CompletePosting(allowed); return; }
                if (!settled || !ownsActor()) throw new InvalidOperationException("Commodity delivery has not reconciled.");
                if (conflict) { owner.rewardRequiresReload = true; return; }
                List<IWritable> returned = [];
                if (committed && !ByMail)
                    foreach (var write in writes)
                    {
                        var item = residents[write.After.Id];
                        if (write.Before == null) returned.Add(new ServerItemAdd
                            { InventoryItem = new InventoryItem { Item = item.Build(), Reason = ItemUpdateReason.Auction } });
                        else owner.SendItemStackCountUpdate(item, item.StackCount, ItemUpdateReason.Auction);
                    }
                if (!Publishing) throw new InvalidOperationException("Commodity delivery requires its complete marketplace publication exclusion.");
                foreach (var packet in returned.Concat(buffered).ToArray()) if (allowed()) owner.player.Session.EnqueueMessageEncrypted(packet);
                if (committed && satchel != null) satchel.Publish(allowed);
            }
            public void Dispose()
            {
                if (disposed) return; disposed = true;
                foreach (var merge in merges.Values) merge.Dispose(); slots?.Dispose(); satchel?.Dispose();
                if (ReferenceEquals(owner.commodityDelivery, this)) owner.commodityDelivery = null;
            }
        }

        internal sealed class CommodityDeliveryWitness(string database, ulong character, AuctionDeliveryWitness[] items, ushort? materialId, ushort? amount, ushort? desired, CommodityRecoveryMail[] recovery, ulong[] consumed = null)
        {
            internal AuctionDeliveryWitness[] Items => items;
            internal CommodityRecoveryMail[] Recovery => recovery;
            internal ushort MaterialId => materialId ?? throw new InvalidOperationException("No material witness.");
            internal ushort? MaterialBefore => amount;
            internal ushort? MaterialEnrolled { get; private set; } = desired;
            internal bool MaterialSettled { get; private set; }
            internal ushort? MaterialSaved { get; private set; }
            internal sealed class CustodyConflict(CommodityDeliveryWitness witness) : Exception("Commodity custody changed before ordinary reconciliation.")
            { internal CommodityDeliveryWitness Witness { get; } = witness; }
            internal sealed class MaterialConflict(CommodityDeliveryWitness witness, ushort? current) : Exception("Commodity material changed before ordinary reconciliation.")
            {
                internal CommodityDeliveryWitness Witness { get; } = witness;
                internal ushort? Current { get; } = current;
            }
            internal void Stage(CharacterContext context)
            {
                if (consumed?.Length > 0)
                {
                    CharacterDatabaseIdentity.Require(context, database);
                    if (context.AtomicCharacterId != character || context.Database.CurrentTransaction == null)
                        throw new InvalidOperationException("Commodity consumption lost its complete Character transaction.");
                    bool mysqlConsumption = context.Database.ProviderName?.Contains("MySql", StringComparison.Ordinal) == true;
                    if (!mysqlConsumption && !context.Database.IsSqlite()) throw new InvalidOperationException("Unsupported commodity consumption provider.");
                    foreach (ulong consumedId in consumed)
                        _ = (mysqlConsumption ? context.Item.FromSqlInterpolated($"SELECT * FROM `item` WHERE id={consumedId} FOR UPDATE")
                            : context.Item.Where(row => row.Id == consumedId)).AsNoTracking().SingleOrDefault();
                    void ValidateConsumption()
                    {
                        if (context.Item.Any(row => consumed.Contains(row.Id)) || context.RealmBankItem.Any(row => consumed.Contains(row.Id))
                            || context.MarketplaceAuction.Any(row => consumed.Contains(row.ItemId)) || context.CharacterMailAttachment.Any(row => consumed.Contains(row.ItemGuid)))
                            throw new CustodyConflict(this);
                    }
                    ValidateConsumption(); context.OnSaveValidation(_ => ValidateConsumption());
                }
                try { foreach (var item in items) item.Stage(context); }
                catch (AuctionDeliveryWitness.CustodyConflict) { throw new CustodyConflict(this); }
                foreach (var mail in recovery) mail.Stage(context);
                if (!materialId.HasValue) return;
                CharacterDatabaseIdentity.Require(context, database);
                if (context.AtomicCharacterId != character || context.Database.CurrentTransaction == null)
                    throw new InvalidOperationException("Commodity material witness lost its Character transaction.");
                bool mysql = context.Database.ProviderName?.Contains("MySql", StringComparison.Ordinal) == true;
                if (!mysql && !context.Database.IsSqlite()) throw new InvalidOperationException("Unsupported commodity material provider.");
                ushort key = materialId.Value;
                var material = (mysql ? context.CharacterTradeskillMaterial.FromSqlInterpolated($"SELECT * FROM `character_tradeskill_materials` WHERE id={character} AND materialId={key} FOR UPDATE")
                    : context.CharacterTradeskillMaterial.Where(row => row.Id == character && row.MaterialId == key)).AsNoTracking().SingleOrDefault();
                if (material?.Amount != amount) throw new MaterialConflict(this, material?.Amount);
                void Observe(Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry entry)
                {
                    if (entry.Entity is CharacterTradeskillMaterialModel value && value.Id == character && value.MaterialId == key)
                        MaterialEnrolled = value.Amount;
                }
                context.ChangeTracker.Tracked += (_, eventData) => Observe(eventData.Entry);
                context.ChangeTracker.StateChanged += (_, eventData) => Observe(eventData.Entry);
                if (material != null) context.Attach(material);
                ushort? enrolled = amount;
                context.OnSaveValidation(phase =>
                {
                    if (phase == CharacterSaveValidationPhase.BeforeFlush)
                    {
                        enrolled = context.CharacterTradeskillMaterial.Local.SingleOrDefault(row => row.Id == character && row.MaterialId == key)?.Amount;
                        MaterialEnrolled = enrolled;
                    }
                });
                context.OnSaveSettled(committed =>
                {
                    if (committed) { MaterialSaved = enrolled; MaterialSettled = true; }
                });
            }
        }
    }
}
