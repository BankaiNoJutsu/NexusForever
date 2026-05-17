using System.Collections;
using NexusForever.Game.Abstract;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Entity;
using NexusForever.Game.Static.Loot;
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

        public void SendLootNotify(IPlayer player)
        {
            List<NetworkLootItem> networkLootItems = [];
            foreach (LootInstanceItem item in lootItems.Values.Where(i => !i.Delivered))
            {
                NetworkLootItem networkLootItem = item.Build();
                networkLootItem.CanLoot = HasLooter(player.CharacterId) && item.CanLoot(player.CharacterId);
                networkLootItem.Explosion = Explosion;
                networkLootItems.Add(networkLootItem);
            }

            if (networkLootItems.Count == 0)
            {
                SendLootRemove(player);
                return;
            }

            player.Session.EnqueueMessageEncrypted(new ServerLootNotify
            {
                OwnerUnitId  = OwnerUnitId,
                ParentUnitId = OwnerUnitId,
                Explosion    = Explosion,
                LootItems    = networkLootItems
            });
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

            if (!item.TryRecordRoll(player, action))
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

            if (!item.CanMasterAssign(master.CharacterId) || !item.IsEligible(assignee))
                return false;

            if (!looterGuids.TryGetValue(assignee.Id, out uint assigneeGuid))
                return false;

            IPlayer assigneePlayer = PlayerManager.Instance.GetPlayer(assignee);
            if (assigneePlayer == null)
                return false;

            item.SetWinner(assignee, assigneeGuid);
            BroadcastToAudience(item, item.BuildAssignedWinnerMessage(assignee));
            bool delivered = item.DeliverItem(assigneePlayer);
            if (delivered && HasExpired)
                BroadcastToAudience(item, new ServerLootRemove { OwnerUnitId = OwnerUnitId });

            return delivered;
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

            IPlayer winner = PlayerManager.Instance.GetPlayer(winnerIdentity);
            if (winner == null)
            {
                item.MarkDeliveredWithoutWinner();
                if (HasExpired)
                    BroadcastToAudience(item, new ServerLootRemove { OwnerUnitId = OwnerUnitId });
                return;
            }

            item.SetWinner(winnerIdentity, winnerGuid);
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
