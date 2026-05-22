using System.Collections;
using NexusForever.Game.Abstract;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Entity;
using NexusForever.Game.Static.Loot;
using NexusForever.Game.Abstract.Loot;
using NexusForever.Network.Message;
using NexusForever.Network.World.Message.Model.Loot;
using NexusForever.Shared;
using NexusForever.Shared.Game;
using NetworkLootItem = NexusForever.Network.World.Message.Model.Loot.LootItem;

namespace NexusForever.Game.Loot
{
    public class LootInstance : IEnumerable<LootInstanceItem>, IUpdate
    {
        public uint OwnerUnitId { get; }
        public LootEntityType LootEntityType { get; }
        public LooterType LooterType { get; }
        public bool Explosion { get; set; }

        public bool HasExpired => expiryTimer.HasElapsed || lootItems.Values.All(i => i.Delivered);

        private readonly Dictionary<ulong, uint> looterGuids = [];
        private readonly Dictionary<uint, LootInstanceItem> lootItems = [];

        private readonly UpdateTimer expiryTimer = new(1800d);

        public LootInstance(uint ownerUnitId, Dictionary<ulong, uint> looterIds, LooterType looterType, LootEntityType lootEntityType)
        {
            OwnerUnitId    = ownerUnitId;
            LooterType     = looterType;
            LootEntityType = lootEntityType;

            foreach ((ulong characterId, uint guid) in looterIds)
                looterGuids.Add(characterId, guid);
        }

        public void Update(double lastTick)
        {
            if (!expiryTimer.HasElapsed)
                expiryTimer.Update(lastTick);

            foreach (LootInstanceItem item in lootItems.Values.Where(i => !i.Delivered))
            {
                item.UpdateRoll(lastTick);
                if (item.ShouldFinaliseRoll())
                    FinaliseRoll(item);
            }
        }

        public LootInstanceItem AddLootItem(uint staticId, LootItemType type, uint count)
        {
            LootInstanceItem existingItem = lootItems.Values.SingleOrDefault(i => i.StaticId == staticId && i.Type == type && !i.Delivered);
            if (existingItem != null)
            {
                existingItem.AddToAmount(count);
                return existingItem;
            }

            LootInstanceItem item = new(staticId, type, count);
            item.SetOwnerUnit(OwnerUnitId);
            lootItems.Add(item.Id, item);

            if (LootEntityType == LootEntityType.Item && LooterType == LooterType.Player)
            {
                KeyValuePair<ulong, uint> looter = looterGuids.First();
                item.SetWinner(looter.Key, looter.Value);
            }

            return item;
        }

        public void SendLootNotify(IPlayer player, bool includeGrantedItems = false)
        {
            List<NetworkLootItem> networkLootItems = [];
            List<LootPacketDiagnostics.NotifyItemState> diagnosticItems = [];
            foreach (LootInstanceItem item in lootItems.Values)
            {
                if (item.Delivered)
                {
                    if (!includeGrantedItems)
                        continue;

                    foreach (NetworkLootItem grantedItem in item.BuildGrantedNotificationItems())
                    {
                        grantedItem.Explosion = Explosion;
                        networkLootItems.Add(grantedItem);
                        diagnosticItems.Add(LootPacketDiagnostics.CaptureNotifyItemState(item, grantedItem));
                    }

                    continue;
                }

                NetworkLootItem networkLootItem = item.Build();
                networkLootItem.CanLoot = HasLooter(player.CharacterId) && item.CanLoot(player.CharacterId);
                networkLootItem.Explosion = Explosion;
                networkLootItems.Add(networkLootItem);
                diagnosticItems.Add(LootPacketDiagnostics.CaptureNotifyItemState(item, networkLootItem));
            }

            if (networkLootItems.Count == 0)
            {
                LootPacketDiagnostics.TraceLootNotifySuppressed(
                    player.CharacterId,
                    OwnerUnitId,
                    includeGrantedItems,
                    Explosion,
                    lootItems.Count,
                    lootItems.Values.Count(item => item.Delivered));
                LootRuntimeEvidenceCollector.RecordSuppressedNotifyIfArmed(
                    player,
                    OwnerUnitId,
                    includeGrantedItems,
                    Explosion,
                    lootItems.Count,
                    lootItems.Values.Count(item => item.Delivered));
                SendLootRemove(player);
                return;
            }

            uint parentUnitId = OwnerUnitId;
            LootPacketDiagnostics.TraceLootNotify(
                player.CharacterId,
                OwnerUnitId,
                parentUnitId,
                includeGrantedItems,
                Explosion,
                diagnosticItems);
            var notify = new ServerLootNotify
            {
                OwnerUnitId  = OwnerUnitId,
                ParentUnitId = parentUnitId,
                Explosion    = Explosion,
                LootItems    = networkLootItems
            };
            LootRuntimeEvidenceCollector.RecordNotifyIfArmed(player, notify, includeGrantedItems, diagnosticItems);
            player.Session.EnqueueMessageEncrypted(notify);
        }

        public bool DeliverAllLoot(IPlayer player, bool sendAsGrant = false)
        {
            if (!HasLooter(player.CharacterId))
                throw new InvalidOperationException($"Character {player.CharacterId} is not permitted to loot owner {OwnerUnitId}.");

            bool deliveredAny = false;
            foreach (LootInstanceItem item in lootItems.Values.Where(i => !i.Delivered).ToList())
            {
                if (item.WinnerCharacterId == 0ul)
                    item.SetWinner(player);

                deliveredAny |= item.DeliverItem(player, sendAsGrant);
            }

            return deliveredAny;
        }

        public void SendLootRemove(IPlayer player)
        {
            player.Session.EnqueueMessageEncrypted(new ServerLootRemove
            {
                OwnerUnitId = OwnerUnitId
            });
        }

        public bool HasLootInstanceId(uint lootInstanceId)
        {
            return lootItems.ContainsKey(lootInstanceId);
        }

        public bool HasLooter(ulong characterId)
        {
            return looterGuids.ContainsKey(characterId);
        }

        public bool GiveLoot(IPlayer player, uint lootInstanceItemId)
        {
            if (!HasLooter(player.CharacterId))
                throw new InvalidOperationException($"Character {player.CharacterId} is not permitted to loot owner {OwnerUnitId}.");

            if (!lootItems.TryGetValue(lootInstanceItemId, out LootInstanceItem item))
                return false;

            if (item.Delivered || !item.CanLoot(player.CharacterId))
                return false;

            if (item.RequiresBindOnPickupConfirmation() && !item.HasBindOnPickupConfirmation(player.CharacterId))
            {
                item.TryPromptBindOnPickupConfirmation(player);
                return false;
            }

            if (item.WinnerCharacterId == 0ul)
                item.SetWinner(player);

            return item.DeliverItem(player);
        }

        public bool RollLoot(IPlayer player, uint lootInstanceItemId, LootRollAction action)
        {
            if (!HasLooter(player.CharacterId))
                throw new InvalidOperationException($"Character {player.CharacterId} is not permitted to roll on owner {OwnerUnitId}.");

            if (!lootItems.TryGetValue(lootInstanceItemId, out LootInstanceItem item))
                return false;

            if (!item.CanTryRoll(player.CharacterId) || !item.TryRecordRoll(player, action))
                return false;

            BroadcastToAudience(item, item.BuildRollMessage(player, action));

            if (item.ShouldFinaliseRoll())
                FinaliseRoll(item);

            return true;
        }

        public bool AssignMasterLoot(IPlayer master, uint lootInstanceItemId, Identity assignee)
        {
            if (!HasLooter(master.CharacterId))
                throw new InvalidOperationException($"Character {master.CharacterId} is not permitted to assign loot on owner {OwnerUnitId}.");

            if (!lootItems.TryGetValue(lootInstanceItemId, out LootInstanceItem item))
                return false;

            if (!item.CanTryMasterAssign(master.CharacterId, assignee))
                return false;

            if (!looterGuids.TryGetValue(assignee.Id, out uint assigneeGuid))
                return false;

            item.ResolveAssignedWinner(assignee, assigneeGuid);
            BroadcastToAudience(item, item.BuildAssignedWinnerMessage(assignee));

            IPlayer assigneePlayer = PlayerManager.Instance.GetPlayer(assignee);
            if (assigneePlayer == null)
                return true;

            bool delivered = item.DeliverItem(assigneePlayer);
            if (delivered && HasExpired)
                BroadcastToAudience(item, new ServerLootRemove { OwnerUnitId = OwnerUnitId });

            return true;
        }

        public LootRuntimeSnapshot CreateRuntimeSnapshot(IPlayer viewer)
        {
            ulong viewerCharacterId = viewer?.CharacterId ?? 0ul;

            return new LootRuntimeSnapshot
            {
                OwnerUnitId = OwnerUnitId,
                ParentUnitIdRuntimeValue = OwnerUnitId,
                ParentUnitIdNotes = "Client uses ParentUnitId as the loot visual source; current runtime mirrors OwnerUnitId because LootInstance does not yet track a distinct parent source.",
                LootEntityType = LootEntityType,
                LooterType = LooterType,
                Explosion = Explosion,
                HasExpired = HasExpired,
                ViewerCharacterId = viewerCharacterId,
                ViewerIsTrackedLooter = viewer != null && HasLooter(viewerCharacterId),
                TrackedLooterCharacterIds = looterGuids.Keys.OrderBy(id => id).ToList(),
                Items = lootItems.Values
                    .OrderBy(item => item.Id)
                    .Select(item => item.CreateRuntimeSnapshot(viewerCharacterId, viewer != null && HasLooter(viewerCharacterId)))
                    .ToList()
            };
        }

        private void FinaliseRoll(LootInstanceItem item)
        {
            ServerLootWinner winnerMessage = item.BuildRollWinnerMessage(out Identity winnerIdentity);
            BroadcastToAudience(item, winnerMessage);

            if (winnerIdentity == null || !looterGuids.TryGetValue(winnerIdentity.Id, out uint winnerGuid))
            {
                item.MarkDeliveredWithoutWinner();
                if (HasExpired)
                    BroadcastToAudience(item, new ServerLootRemove { OwnerUnitId = OwnerUnitId });
                return;
            }

            item.ResolveRollWinner(winnerIdentity, winnerGuid);

            IPlayer winner = PlayerManager.Instance.GetPlayer(winnerIdentity);
            if (winner == null)
                return;

            item.DeliverItem(winner);
            if (HasExpired)
                BroadcastToAudience(item, new ServerLootRemove { OwnerUnitId = OwnerUnitId });
        }

        private void BroadcastToAudience(LootInstanceItem item, IWritable message)
        {
            IEnumerable<ulong> audience = item.GetAudienceCharacterIds();
            if (!audience.Any())
                audience = looterGuids.Keys;

            foreach (ulong characterId in audience.Distinct())
            {
                IPlayer player = PlayerManager.Instance.GetPlayer(characterId);
                player?.Session.EnqueueMessageEncrypted(message);
            }
        }

        public IEnumerator<LootInstanceItem> GetEnumerator()
        {
            return lootItems.Values.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
