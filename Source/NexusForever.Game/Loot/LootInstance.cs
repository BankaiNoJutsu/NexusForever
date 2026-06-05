using System.Collections;
using Microsoft.Extensions.DependencyInjection;
using NexusForever.Game.Abstract;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Entity;
using NexusForever.Game.Static.Loot;
using NexusForever.Game.Abstract.Loot;
using NexusForever.Network.Message;
using NexusForever.Network.World.Message.Model.Loot;
using NexusForever.Shared;
using NexusForever.Shared.Game;
using NLog;
using NetworkLootItem = NexusForever.Network.World.Message.Model.Loot.LootItem;

namespace NexusForever.Game.Loot
{
    public class LootInstance : IEnumerable<LootInstanceItem>, IUpdate
    {
        private static readonly ILogger log = LogManager.GetCurrentClassLogger();

        public uint OwnerUnitId { get; }
        public uint ParentUnitId { get; }
        public LootEntityType LootEntityType { get; }
        public LooterType LooterType { get; }
        public bool Explosion { get; set; }

        public bool HasExpired => expiryTimer.HasElapsed || lootItems.Values.All(i => i.Delivered);
        public IReadOnlyCollection<ulong> LooterCharacterIds => looterGuids.Keys;

        private readonly Dictionary<ulong, uint> looterGuids = [];
        private readonly Dictionary<uint, LootInstanceItem> lootItems = [];
        private readonly Dictionary<ulong, string> lastLootNotifyFingerprintsByLooter = [];

        private readonly UpdateTimer expiryTimer = new(1800d);

        public LootInstance(uint ownerUnitId, Dictionary<ulong, uint> looterIds, LooterType looterType, LootEntityType lootEntityType)
            : this(ownerUnitId, ownerUnitId, looterIds, looterType, lootEntityType)
        {
        }

        public LootInstance(uint ownerUnitId, uint parentUnitId, Dictionary<ulong, uint> looterIds, LooterType looterType, LootEntityType lootEntityType)
        {
            OwnerUnitId    = ownerUnitId;
            ParentUnitId   = parentUnitId;
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
                if (!existingItem.TryAddToAmount(count))
                    log.Warn($"Duplicate loot item amount merge skipped: ownerUnit={OwnerUnitId}, lootUnitId={existingItem.Id}, type={type}, staticId={staticId}, existingAmount={existingItem.Amount}, addedAmount={count}.");

                return existingItem;
            }

            LootInstanceItem item = new(staticId, type, count);
            item.SetOwnerUnit(OwnerUnitId);
            lootItems.Add(item.Id, item);

            if (LootEntityType == LootEntityType.Item && LooterType == LooterType.Player)
            {
                if (looterGuids.Count == 0)
                {
                    log.Warn($"Player loot item created without eligible looters: ownerUnit={OwnerUnitId}, staticId={staticId}.");
                }
                else
                {
                    KeyValuePair<ulong, uint> looter = looterGuids.First();
                    item.SetWinner(looter.Key, looter.Value);
                }
            }

            return item;
        }

        public void SendLootNotify(IPlayer player, bool includeGrantedItems = false, bool suppressIfUnchanged = false)
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
                lastLootNotifyFingerprintsByLooter.Remove(player.CharacterId);
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

            string notifyFingerprint = BuildLootNotifyFingerprint(includeGrantedItems, networkLootItems);
            if (suppressIfUnchanged
                && lastLootNotifyFingerprintsByLooter.TryGetValue(player.CharacterId, out string lastNotifyFingerprint)
                && lastNotifyFingerprint == notifyFingerprint)
            {
                log.Trace("Suppressed duplicate loot notify for player {CharacterId}, ownerUnit={OwnerUnitId}, includeGrantedItems={IncludeGrantedItems}.",
                    player.CharacterId, OwnerUnitId, includeGrantedItems);
                return;
            }

            uint parentUnitId = ParentUnitId;
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
            lastLootNotifyFingerprintsByLooter[player.CharacterId] = notifyFingerprint;
        }

        private static string BuildLootNotifyFingerprint(bool includeGrantedItems, IEnumerable<NetworkLootItem> networkLootItems)
        {
            IEnumerable<string> itemFingerprints = networkLootItems
                .OrderBy(item => item.LootUnitId)
                .ThenBy(item => item.Type)
                .ThenBy(item => item.ItemId)
                .Select(BuildLootNotifyItemFingerprint);

            return $"{includeGrantedItems}:{string.Join("|", itemFingerprints)}";
        }

        private static string BuildLootNotifyItemFingerprint(NetworkLootItem item)
        {
            string masterList = string.Join(",", item.MasterList
                .OrderBy(identity => identity.RealmId)
                .ThenBy(identity => identity.Id)
                .Select(identity => $"{identity.RealmId}/{identity.Id}"));

            return $"{item.LootUnitId}:{(uint)item.Type}:{item.ItemId}:{item.Amount}:{(item.CanLoot ? 1 : 0)}:{(item.RequiresRoll ? 1 : 0)}:{(item.OnlyMasterLootable ? 1 : 0)}:{(item.Explosion ? 1 : 0)}:{(item.Granted ? 1 : 0)}:{item.RandomCircuitData}:{item.RandomGlyphData}:{item.ItemQuality2Id}:{masterList}";
        }

        public bool DeliverAllLoot(IPlayer player, bool sendAsGrant = false)
        {
            if (!HasLooter(player.CharacterId))
                throw new InvalidOperationException($"Character {player.CharacterId} is not permitted to loot owner {OwnerUnitId}.");

            bool deliveredAny = false;
            int failedCount = 0;
            foreach (LootInstanceItem item in lootItems.Values.Where(i => !i.Delivered).ToList())
            {
                if (item.WinnerCharacterId == 0ul)
                    item.SetWinner(player);

                if (item.DeliverItem(player, sendAsGrant))
                    deliveredAny = true;
                else
                    failedCount++;
            }

            if (failedCount > 0)
                log.Warn("DeliverAllLoot for owner {OwnerUnitId}, player {PlayerId}: {FailedCount} item(s) failed to deliver.",
                    OwnerUnitId, player.CharacterId, failedCount);

            return deliveredAny && failedCount == 0;
        }

        public void SendLootRemove(IPlayer player)
        {
            lastLootNotifyFingerprintsByLooter.Remove(player.CharacterId);
            player.Session.EnqueueMessageEncrypted(new ServerLootRemove
            {
                OwnerUnitId = OwnerUnitId
            });
        }

        public void SendLootRemoveToAllLooters()
        {
            foreach (ulong characterId in looterGuids.Keys)
            {
                IPlayer player = TryGetPlayer(characterId);
                player?.Session.EnqueueMessageEncrypted(new ServerLootRemove
                {
                    OwnerUnitId = OwnerUnitId
                });
            }
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

            bool delivered = item.DeliverItem(player);
            if (delivered)
            {
                BroadcastLootItemUpdate(item);
                BroadcastLootNotification(item, player);
            }

            return delivered;
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
            BroadcastLootItemUpdate(item);

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

            IPlayer assigneePlayer = TryGetPlayer(assignee);
            if (assigneePlayer == null)
            {
                log.Warn("Master loot assignment for loot unit {LootUnitId} on owner {OwnerUnitId}: assignee {AssigneeId} is offline; item remains on corpse.",
                    lootInstanceItemId, OwnerUnitId, assignee.Id);
                return false;
            }

            item.ResolveAssignedWinner(assignee, assigneeGuid);
            BroadcastToAudience(item, item.BuildAssignedWinnerMessage(assignee));
            BroadcastLootItemUpdate(item);

            bool delivered = item.DeliverItem(assigneePlayer);
            if (delivered)
            {
                BroadcastLootItemUpdate(item);
                BroadcastLootNotification(item, assigneePlayer);
            }

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
                ParentUnitIdRuntimeValue = ParentUnitId,
                ParentUnitIdNotes = ParentUnitId == OwnerUnitId
                    ? "ParentUnitId mirrors OwnerUnitId because this LootInstance was constructed without a distinct parent source."
                    : "ParentUnitId tracks a distinct parent source.",
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
                BroadcastLootItemUpdate(item);
                if (HasExpired)
                    BroadcastToAudience(item, new ServerLootRemove { OwnerUnitId = OwnerUnitId });
                return;
            }

            item.ResolveRollWinner(winnerIdentity, winnerGuid);
            BroadcastLootItemUpdate(item);

            IPlayer winner = TryGetPlayer(winnerIdentity);
            if (winner == null)
            {
                log.Warn("Roll winner {WinnerId} is offline for loot unit {LootUnitId} on owner {OwnerUnitId}; item remains assigned for deferred delivery.",
                    winnerIdentity.Id, item.Id, OwnerUnitId);
                return;
            }

            if (item.DeliverItem(winner))
            {
                BroadcastLootItemUpdate(item);
                BroadcastLootNotification(item, winner);
            }

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
                IPlayer player = TryGetPlayer(characterId);
                player?.Session.EnqueueMessageEncrypted(message);
            }
        }

        private static IPlayer TryGetPlayer(ulong characterId)
        {
            IPlayerManager playerManager = LegacyServiceProvider.Provider.GetService<IPlayerManager>();
            return playerManager?.GetPlayer(characterId);
        }

        private static IPlayer TryGetPlayer(Identity identity)
        {
            IPlayerManager playerManager = LegacyServiceProvider.Provider.GetService<IPlayerManager>();
            return playerManager?.GetPlayer(identity);
        }

        private void BroadcastLootItemUpdate(LootInstanceItem item)
        {
            BroadcastToAudience(item, new ServerLootItemUpdate
            {
                LootItem = item.Build()
            });
        }

        public void BroadcastLootNotification(LootInstanceItem item, IPlayer excludedLooter)
        {
            if (item.WinnerGuid == 0u)
                return;

            ServerLootNotification notification = new()
            {
                LootUnitId        = item.Id,
                ItemId            = item.StaticId,
                Amount            = item.Amount,
                LooterUnitId      = item.WinnerGuid,
                Type              = item.Type,
                RandomCircuitData = 0ul,
                RandomGlyphData   = 0u,
                ItemQuality2Id    = item.ItemQualityId
            };

            foreach (ulong characterId in looterGuids.Keys)
            {
                if (characterId == excludedLooter?.CharacterId)
                    continue;

                IPlayer player = TryGetPlayer(characterId);
                player?.Session.EnqueueMessageEncrypted(notification);
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
