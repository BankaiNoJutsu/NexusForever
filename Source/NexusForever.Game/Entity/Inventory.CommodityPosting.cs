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
        internal CommodityDelivery ReserveCommodityPosting(uint definition, uint consume, IReadOnlyList<GlobalMarketplaceManager.CommodityPostingDeliveryBatch> deliveries, string database,
            Func<bool> ownsActor, Func<MailHeaderState> recoveryHeader)
        {
            lock (MutationGate)
            {
                if (!ownsActor() || commodityDelivery != null || inventorySaveLeases != 1 || RequiresAuthoritativeReload
                    || rewardOwnerInvalidated || HasPendingRewardDelivery || GetBag(InventoryLocation.Inventory) is not Bag bag)
                    throw new InvalidOperationException("Commodity posting requires its original excluded inventory.");
                var info = itemManager?.GetItemInfo(definition) ?? throw new InvalidOperationException("Commodity posting lost its Item definition.");
                uint maximum = info.IsStackable() ? info.Entry.MaxStackCount : 1;
                uint deliver = deliveries.Aggregate(0u, (sum, row) => checked(sum + row.Quantity));
                if (maximum == 0 || consume == 0 && deliver == 0) throw new InvalidOperationException("Commodity posting has no valid Item work.");
                var snapshot = bag.CreateSnapshot();
                var before = new Dictionary<ulong, GlobalMarketplaceManager.BidItem>();
                var after = new Dictionary<ulong, GlobalMarketplaceManager.BidItem>();
                var consumed = new Dictionary<ulong, uint>(); var delivered = new Dictionary<ulong, uint>();
                var expected = new Dictionary<uint, IItem>();
                uint remaining = consume;
                foreach (var candidate in snapshot.Where(row => row?.Id == definition))
                {
                    if (remaining == 0) break;
                    if (candidate is not Item item || bag.IsSlotReserved(item.BagIndex)) throw new InvalidOperationException("Commodity sale lost an original source stack.");
                    var stored = item.CaptureCommodityMergePreimage();
                    uint take = Math.Min(remaining, stored.Quantity); if (take == 0) continue;
                    before.Add(item.Guid, stored); consumed.Add(item.Guid, take);
                    after.Add(item.Guid, take == stored.Quantity ? null : stored with { Quantity = stored.Quantity - take });
                    expected.Add(item.BagIndex, item); remaining -= take;
                }
                if (remaining != 0) throw new InvalidOperationException("Commodity sale has insufficient captured Item quantity.");
                var occupied = snapshot.Select(item => item != null).ToArray();
                var layout = new GlobalMarketplaceManager.BidItem[snapshot.Length];
                foreach (var row in before.Values)
                {
                    layout[row.Bag] = after[row.Id]; occupied[row.Bag] = after[row.Id] != null;
                }
                var held = new CommodityDelivery(this, bag, info, database, ownsActor, false, recoveryHeader) { posting = [] };
                try
                {
                    uint materialCapacity = 0;
                    bool autoStore = player.SupplySatchelManager?.TryGetAutoStoreCapacity(info, out materialCapacity) == true;
                    if (autoStore && player.SupplySatchelManager is not SupplySatchelManager)
                        throw new InvalidOperationException("Commodity posting requires its concrete satchel owner.");
                    uint materialRemaining = autoStore ? materialCapacity : 0, plannedMaterial = 0, directTotal = 0;
                    uint expiration = GetInitialExpirationTimeLeft(info);
                    foreach (var batch in deliveries)
                    {
                        if (batch.Quantity == 0) continue;
                        uint empty = (uint)occupied.Count(value => !value);
                        if (empty < ((ulong)batch.Quantity + maximum - 1) / maximum)
                        {
                            var template = new Item(characterId, info, batch.Quantity, itemManager: itemManager, gameTableManager: gameTableManager).CaptureRewardModel();
                            var target = GlobalMarketplaceManager.BidItem.Capture(template);
                            after.Add(target.Id, target); delivered.Add(target.Id, batch.Quantity);
                            held.postingMailOrder.Add((target.Id, batch.Returned)); continue;
                        }
                        directTotal = checked(directTotal + batch.Quantity);
                        uint stored = Math.Min(materialRemaining, batch.Quantity);
                        plannedMaterial += stored; materialRemaining -= stored; remaining = batch.Quantity - stored;
                        if (info.IsStackable())
                            for (uint index = 0; remaining != 0 && index < snapshot.Length; index++)
                            {
                                if (!occupied[index] || bag.IsSlotReserved(index)) continue;
                                var target = layout[index];
                                if (target == null)
                                {
                                    if (snapshot[index]?.Id != definition || snapshot[index] is not Item item) continue;
                                    target = item.CaptureCommodityMergePreimage(); layout[index] = target;
                                }
                                if (target.Definition != definition || target.Expiration != expiration) continue;
                                if (target.Quantity > maximum) throw new InvalidOperationException("Commodity posting has an invalid destination stack.");
                                uint add = Math.Min(remaining, maximum - target.Quantity); if (add == 0) continue;
                                if (snapshot[index]?.Guid == target.Id)
                                {
                                    before.TryAdd(target.Id, ((Item)snapshot[index]).CaptureCommodityMergePreimage());
                                    expected.TryAdd(index, snapshot[index]);
                                }
                                target = target with { Quantity = target.Quantity + add };
                                after[target.Id] = target; layout[index] = target;
                                delivered[target.Id] = checked(delivered.GetValueOrDefault(target.Id) + add); remaining -= add;
                            }
                        for (uint index = 0; remaining != 0 && index < snapshot.Length; index++)
                        {
                            if (occupied[index] || bag.IsSlotReserved(index)) continue;
                            uint quantity = Math.Min(remaining, maximum);
                            var template = new Item(characterId, info, quantity, itemManager: itemManager, gameTableManager: gameTableManager).CaptureRewardModel();
                            template.Location = (ushort)InventoryLocation.Inventory; template.BagIndex = index;
                            var target = GlobalMarketplaceManager.BidItem.Capture(template);
                            expected.TryAdd(index, snapshot[index]); after.Add(target.Id, target); delivered.Add(target.Id, quantity);
                            layout[index] = target; occupied[index] = true; remaining -= quantity;
                        }
                        if (remaining != 0) throw new InvalidOperationException("Commodity posting lost its per-fill destination capacity.");
                    }
                    if (directTotal != 0)
                        held.satchel = (player.SupplySatchelManager as SupplySatchelManager)?.ReserveCommodityReturn(info, directTotal, database);
                    uint materialDelta = held.satchel == null ? 0 : (uint)(held.satchel.Write.After - held.satchel.Write.Before);
                    if (materialDelta != plannedMaterial) throw new InvalidOperationException("Commodity posting material capacity changed during capture.");
                    foreach (ulong key in before.Keys.Union(after.Keys).Order())
                    {
                        before.TryGetValue(key, out var original); after.TryGetValue(key, out var target);
                        held.posting.Add(new(original, target, consumed.GetValueOrDefault(key), delivered.GetValueOrDefault(key)));
                        if (original != null) held.residents.Add(key, (Item)snapshot[original.Bag]);
                        if (target != null) held.writes.Add(new(original, target));
                    }
                    if (expected.Count != 0 && !bag.TryReserveRewardPlan(expected, out held.slots, out held.merges))
                        throw new InvalidOperationException("Commodity posting lost its exact source/destination plan.");
                    return commodityDelivery = held;
                }
                catch { held.Dispose(); throw; }
            }
        }

        internal sealed partial class CommodityDelivery
        {
            internal List<GlobalMarketplaceManager.CommodityPostingItem> posting;
            internal readonly List<(ulong Id, bool Returned)> postingMailOrder = [];
            internal IReadOnlyList<(ulong Id, bool Returned)> PostingMailItems => postingMailOrder.ToArray();
            private Dictionary<ulong, MailHeaderState> postingItemMails = [];
            internal GlobalMarketplaceManager.CommodityPostingItem[] PostingItems => posting?.ToArray();

            private void SettlePosting(bool committed, GlobalMarketplaceManager.BidItem[] current, ushort? material, bool deliveryMatches, IReadOnlyDictionary<ulong, MailHeaderState> itemMails)
            {
                lock (owner.MutationGate)
                {
                    if (settled || !ownsActor()) throw new InvalidOperationException("Commodity posting lost its original inventory actor.");
                    this.committed = committed;
                    postingItemMails = itemMails?.ToDictionary(row => row.Key, row => row.Value) ?? [];
                    conflict = !deliveryMatches || current != null && !current.SequenceEqual(writes.Select(row => committed ? row.After : row.Before));
                    if (!conflict && committed)
                    {
                        if (!postingMailOrder.Select(row => row.Id).Order().SequenceEqual(postingItemMails.Keys.Order())
                            || postingItemMails.Any(row => row.Value.RecipientId != owner.characterId))
                            throw new InvalidOperationException("Commodity posting lost its complete fixed mail ownership.");
                        if (slots != null && !slots.IsCurrent) throw new InvalidOperationException("Commodity posting lost its captured bag layout.");
                        foreach (var row in posting.Where(row => row.Before != null))
                        {
                            var item = residents[row.Id]; item.AdoptCommodityPosting(row.Before, row.After, merges[item]);
                            if (row.After == null) { slots.PublishCommodityReplacement(null, row.Before.Bag); residents.Remove(row.Id); }
                        }
                        foreach (var row in posting.Where(row => row.Before == null && row.After.Location == (ushort)InventoryLocation.Inventory))
                        {
                            var item = new Item(row.After.Model(), owner.itemManager, owner.gameTableManager); item.AdoptAuctionInventoryLocation();
                            slots.PublishCommodityReplacement(item, row.After.Bag); residents.Add(row.Id, item);
                        }
                    }
                    foreach (var merge in merges.Values) merge.Dispose(); merges.Clear(); slots?.Dispose();
                    if (committed && !conflict)
                        foreach (var item in residents.Values)
                        {
                            ItemRuneSlotInitializer.ApplyDefaultSockets(item, owner.gameTableManager);
                            initializedRunes.Add(item.Guid, ItemRuneSlotsCodec.Serialize(item.RuneSlots));
                        }
                    if (!committed && current == null && satchel != null) material = satchel.Write.Exists ? satchel.Write.Before : null;
                    satchel?.Settle(material); settled = true;
                    if (conflict) RejectCustody();
                    PrepareNextReconciliation();
                    if (!conflict && committed && !owner.player.IsLoading) owner.RefreshCollectedItem(info.Id);
                }
            }

            private void CompletePosting(Func<bool> allowed)
            {
                if (!settled || !ownsActor() || !Publishing) throw new InvalidOperationException("Commodity posting lost its publication owner.");
                if (conflict) { owner.rewardRequiresReload = true; return; }
                List<IWritable> packets = [];
                if (committed)
                    foreach (var row in posting)
                    {
                        if (row.After == null) packets.Add(new ServerItemDelete { Guid = row.Id, Reason = ItemUpdateReason.Auction });
                        else if (row.After.Location == (ushort)InventoryLocation.Inventory)
                        {
                            var item = residents[row.Id];
                            if (row.Before == null) packets.Add(new ServerItemAdd { InventoryItem = new InventoryItem { Item = item.Build(), Reason = ItemUpdateReason.Auction } });
                            else owner.SendItemStackCountUpdate(item, item.StackCount, ItemUpdateReason.Auction);
                        }
                    }
                foreach (var packet in packets.Concat(buffered).ToArray()) if (allowed()) owner.player.Session.EnqueueMessageEncrypted(packet);
                if (committed && satchel != null) satchel.Publish(allowed);
            }
        }
    }
}
