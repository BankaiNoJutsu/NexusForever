using NexusForever.Game;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Loot;
using NexusForever.Game.Achievement;
using NexusForever.Game.Static.Account;
using NexusForever.Game.Static.Entity;
using NexusForever.Game.Static.Loot;
using NexusForever.Game.Static.Quest;
using NexusForever.GameTable;
using NexusForever.GameTable.Model;
using NexusForever.GameTable.Static;
using NexusForever.Network.World.Message.Model;
using NexusForever.Network.World.Message.Model.Loot;
using NexusForever.Network.World.Message.Static;
using NexusForever.Shared.Game;
using NLog;
using NetworkLootItem = NexusForever.Network.World.Message.Model.Loot.LootItem;
using NetworkIdentity = NexusForever.Network.World.Message.Model.Shared.Identity;
using GameIdentity = NexusForever.Game.Abstract.Identity;

namespace NexusForever.Game.Loot
{
    public class LootInstanceItem
    {
        private class LootRollRecord
        {
            public required GameIdentity Identity { get; init; }
            public LootRollAction Action { get; init; }
            public uint Value { get; init; }
        }

        private static readonly ILogger log = LogManager.GetCurrentClassLogger();

        private const double ROLL_DURATION_SECONDS = 30d;
        public uint Id { get; }
        public uint OwnerUnitId { get; private set; }
        public uint StaticId { get; }
        public LootItemType Type { get; }
        public uint Amount { get; private set; }
        public uint ItemQualityId => GetItemQualityId();
        public uint ItemQualityVisualEffectIdLoot => GetItemQualityVisualEffectIdLoot();

        public uint WinnerGuid { get; private set; }
        public ulong WinnerCharacterId { get; private set; }
        public bool Delivered { get; private set; }

        public bool RequiresRoll { get; private set; }
        public bool OnlyMasterLootable { get; private set; }
        public uint RollTime => RequiresRoll && rollTimer != null ? (uint)Math.Ceiling(rollTimer.Time) : 0u;

        private readonly Dictionary<ulong, GameIdentity> eligibleIdentities = [];
        private readonly Dictionary<ulong, GameIdentity> masterIdentities = [];
        private readonly Dictionary<ulong, GameIdentity> masterLootCandidates = [];
        private readonly Dictionary<ulong, LootRollRecord> rollRecords = [];
        private readonly HashSet<ulong> bindPickupConfirmations = [];
        private UpdateTimer rollTimer;
        private bool rollFinalised;

        public LootInstanceItem(uint staticId, LootItemType type, uint count)
        {
            Id       = GlobalLootManager.Instance.NextLootId;
            StaticId = staticId;
            Type     = type;
            Amount   = count;
        }

        public void AddToAmount(uint amount)
        {
            log.Trace($"Loot item amount merge: ownerUnit={OwnerUnitId}, lootUnitId={Id}, type={Type}, staticId={StaticId}, previousAmount={Amount}, addedAmount={amount}.");
            Amount += amount;
        }

        public void SetWinner(IPlayer player)
        {
            SetWinner(player.CharacterId, player.Guid);
        }

        public void SetWinner(GameIdentity identity, uint guid)
        {
            SetWinner(identity.Id, guid);
        }

        public void SetWinner(ulong characterId, uint guid)
        {
            WinnerGuid        = guid;
            WinnerCharacterId = characterId;
        }

        public void SetOwnerUnit(uint ownerUnitId)
        {
            OwnerUnitId = ownerUnitId;
        }

        public void ConfigureFreeForAll(IEnumerable<GameIdentity> eligible)
        {
            ClearGroupState();
            SetEligibleIdentities(eligible);
        }

        public void ConfigureAssigned(GameIdentity winner, uint guid)
        {
            ClearGroupState();
            SetEligibleIdentities([winner]);
            SetWinner(winner, guid);
        }

        public void ConfigureMaster(IEnumerable<GameIdentity> masters, IEnumerable<GameIdentity> eligible, IEnumerable<GameIdentity> candidates)
        {
            ClearGroupState();
            OnlyMasterLootable = true;
            SetEligibleIdentities(eligible);
            SetMasterLootCandidates(candidates);

            foreach (GameIdentity master in masters)
                masterIdentities[master.Id] = master;
        }

        public void ConfigureRoll(IEnumerable<GameIdentity> eligible)
        {
            ClearGroupState();
            RequiresRoll = true;
            rollTimer = new UpdateTimer(ROLL_DURATION_SECONDS);
            SetEligibleIdentities(eligible);
        }

        private void ClearGroupState()
        {
            RequiresRoll       = false;
            OnlyMasterLootable = false;
            WinnerGuid         = 0u;
            WinnerCharacterId  = 0ul;
            rollTimer          = null;
            rollFinalised      = false;

            eligibleIdentities.Clear();
            masterIdentities.Clear();
            masterLootCandidates.Clear();
            rollRecords.Clear();
            bindPickupConfirmations.Clear();
        }

        private void SetEligibleIdentities(IEnumerable<GameIdentity> eligible)
        {
            foreach (GameIdentity identity in eligible)
                eligibleIdentities[identity.Id] = identity;
        }

        private void SetMasterLootCandidates(IEnumerable<GameIdentity> candidates)
        {
            if (candidates == null)
                return;

            foreach (GameIdentity identity in candidates)
                masterLootCandidates[identity.Id] = identity;
        }

        public bool CanLoot(ulong characterId)
        {
            if (Delivered || RequiresRoll || OnlyMasterLootable)
                return false;

            if (WinnerCharacterId != 0ul)
                return WinnerCharacterId == characterId;

            return eligibleIdentities.Count == 0 || eligibleIdentities.ContainsKey(characterId);
        }

        public bool RequiresBindOnPickupConfirmation()
        {
            if (Type != LootItemType.StaticItem)
                return false;

            Item2Entry entry = ItemManager.Instance.GetItemInfo(StaticId)?.Entry;
            return entry != null && (entry.BindFlags & ItemBindFlags.BindOnPickup) != 0;
        }

        public bool HasBindOnPickupConfirmation(ulong characterId)
        {
            return bindPickupConfirmations.Contains(characterId);
        }

        public bool TryPromptBindOnPickupConfirmation(IPlayer player)
        {
            if (player == null || Delivered || !RequiresBindOnPickupConfirmation())
                return false;

            if (bindPickupConfirmations.Contains(player.CharacterId))
                return false;

            player.Session.EnqueueMessageEncrypted(new ServerLootBindOnPickup
            {
                OwnerUnitId = OwnerUnitId,
                LootUnitId  = Id
            });
            bindPickupConfirmations.Add(player.CharacterId);
            log.Trace(
                "Loot bind-on-pickup confirmation sent for player {CharacterId}, ownerUnit={OwnerUnitId}, lootUnitId={LootUnitId}, item={StaticId}.",
                player.CharacterId,
                OwnerUnitId,
                Id,
                StaticId);
            return true;
        }

        public bool CanTryRoll(ulong characterId)
        {
            return !Delivered
                && RequiresRoll
                && !rollFinalised
                && eligibleIdentities.ContainsKey(characterId);
        }

        public bool CanMasterAssign(ulong characterId)
        {
            return !Delivered && OnlyMasterLootable && masterIdentities.ContainsKey(characterId);
        }

        public bool CanTryMasterAssign(ulong masterCharacterId, GameIdentity assignee)
        {
            return CanMasterAssign(masterCharacterId) && IsEligible(assignee);
        }

        public bool IsEligible(GameIdentity identity)
        {
            return eligibleIdentities.Count == 0 || eligibleIdentities.TryGetValue(identity.Id, out GameIdentity eligible) && eligible == identity;
        }

        public IReadOnlyCollection<ulong> GetAudienceCharacterIds()
        {
            return eligibleIdentities.Keys.ToList();
        }

        public void UpdateRoll(double lastTick)
        {
            if (!RequiresRoll || Delivered || rollFinalised || rollTimer == null)
                return;

            rollTimer.Update(lastTick);
        }

        public bool ShouldFinaliseRoll()
        {
            return RequiresRoll
                && !Delivered
                && !rollFinalised
                && (rollTimer?.HasElapsed == true || eligibleIdentities.Keys.All(rollRecords.ContainsKey));
        }

        public bool TryRecordRoll(IPlayer player, LootRollAction action)
        {
            if (!CanTryRoll(player.CharacterId))
            {
                log.Trace($"Loot roll rejected for player {player?.CharacterId.ToString() ?? "none"}, lootUnitId={Id}, item={StaticId}: requiresRoll={RequiresRoll}, delivered={Delivered}, rollFinalised={rollFinalised}, eligible=[{string.Join(",", eligibleIdentities.Keys)}].");
                return false;
            }

            if (!Enum.IsDefined(typeof(LootRollAction), action))
            {
                log.Trace($"Loot roll rejected for player {player?.CharacterId.ToString() ?? "none"}, lootUnitId={Id}, item={StaticId}: invalid action {action}.");
                return false;
            }

            if (rollRecords.ContainsKey(player.CharacterId))
            {
                log.Trace($"Loot roll rejected for player {player.CharacterId}, lootUnitId={Id}, item={StaticId}: player already rolled.");
                return false;
            }

            rollRecords.Add(player.CharacterId, new LootRollRecord
            {
                Identity = player.Identity,
                Action   = action,
                Value    = GetRollValue(action)
            });
            log.Trace($"Loot roll recorded for player {player.CharacterId}, lootUnitId={Id}, item={StaticId}, action={action}, value={rollRecords[player.CharacterId].Value}.");

            return true;
        }

        public ServerLootRoll BuildRollMessage(IPlayer player, LootRollAction action)
        {
            return new ServerLootRoll
            {
                LootUnitId = Id,
                Roller     = player.Identity.ToNetworkIdentity(),
                ItemId     = StaticId,
                Action     = action
            };
        }

        public ServerLootWinner BuildRollWinnerMessage(out GameIdentity winnerIdentity)
        {
            List<LootRollRecord> allRolls = [];
            foreach (GameIdentity identity in eligibleIdentities.Values)
            {
                if (rollRecords.TryGetValue(identity.Id, out LootRollRecord record))
                {
                    allRolls.Add(record);
                    continue;
                }

                allRolls.Add(new LootRollRecord
                {
                    Identity = identity,
                    Action   = LootRollAction.Pass,
                    Value    = 0u
                });
            }

            LootRollRecord winningRoll = allRolls
                .Where(r => r.Value > 0u)
                .OrderByDescending(r => r.Value)
                .ThenBy(_ => Random.Shared.Next())
                .FirstOrDefault();

            winnerIdentity = winningRoll?.Identity;

            return new ServerLootWinner
            {
                LootUnitId  = Id,
                WinningRoll = BuildNetworkRoll(winningRoll),
                ItemId      = StaticId,
                OtherRolls  = allRolls
                    .Where(r => r != winningRoll)
                    .Select(BuildNetworkRoll)
                    .ToList()
            };
        }

        public ServerLootWinner BuildAssignedWinnerMessage(GameIdentity winner)
        {
            return new ServerLootWinner
            {
                LootUnitId = Id,
                WinningRoll = new ServerLootWinner.LootRoll
                {
                    Identity = winner.ToNetworkIdentity(),
                    Value    = uint.MaxValue
                },
                ItemId = StaticId
            };
        }

        public void ResolveRollWinner(GameIdentity identity, uint guid)
        {
            ArgumentNullException.ThrowIfNull(identity);

            rollFinalised = true;
            ClearPendingLootState();
            SetWinner(identity, guid);
        }

        public void ResolveAssignedWinner(GameIdentity identity, uint guid)
        {
            ArgumentNullException.ThrowIfNull(identity);

            ClearPendingLootState();
            SetWinner(identity, guid);
        }

        private static ServerLootWinner.LootRoll BuildNetworkRoll(LootRollRecord record)
        {
            return new ServerLootWinner.LootRoll
            {
                Identity = record?.Identity.ToNetworkIdentity() ?? new NetworkIdentity(),
                Value    = record?.Value ?? 0u
            };
        }

        private static uint GetRollValue(LootRollAction action)
        {
            return action switch
            {
                LootRollAction.Need  => (uint)Random.Shared.Next(101, 201),
                LootRollAction.Greed => (uint)Random.Shared.Next(1, 101),
                LootRollAction.Pass  => 0u,
                _                    => 0u
            };
        }

        public void MarkDeliveredWithoutWinner()
        {
            rollFinalised = true;
            ClearPendingLootState();
            Delivered = true;
        }

        public bool DeliverItem(IPlayer player, bool sendAsGrant = true)
        {
            if (player == null)
                throw new ArgumentNullException(nameof(player));

            if (WinnerGuid == 0u || WinnerCharacterId == 0u)
                throw new InvalidOperationException("Winner IDs must be set before delivering this loot item.");

            if (player.CharacterId != WinnerCharacterId)
                throw new InvalidOperationException($"Winner character {WinnerCharacterId} does not match player {player.CharacterId}.");

            if (Delivered)
            {
                log.Trace($"Loot delivery skipped for player {player.CharacterId}, lootUnitId={Id}, item={StaticId}: already delivered.");
                return false;
            }

            log.Trace($"Loot delivery started for player {player.CharacterId}, ownerUnit={OwnerUnitId}, lootUnitId={Id}, type={Type}, staticId={StaticId}, amount={Amount}, sendAsGrant={sendAsGrant}.");
            switch (Type)
            {
                case LootItemType.AccountCurrency:
                    player.Account.CurrencyManager.CurrencyAddAmount((AccountCurrencyType)StaticId, Amount);
                    AccountCurrencyAchievementUpdater.Update(player, (AccountCurrencyType)StaticId, Amount);
                    break;
                case LootItemType.AccountItem:
                    for (uint i = 0; i < Amount; i++)
                        player.Account.InventoryManager.AddItem(StaticId);
                    break;
                case LootItemType.Cash:
                    player.CurrencyManager.CurrencyAddAmount((CurrencyType)StaticId, Amount, isLoot: true);
                    break;
                case LootItemType.StaticItem:
                    if (!CanDeliverStaticItem(player, StaticId, Amount, out bool inventoryFull))
                    {
                        if (inventoryFull)
                        {
                            log.Trace($"Loot static item delivery failed for player {player.CharacterId}, ownerUnit={OwnerUnitId}, lootUnitId={Id}, item={StaticId}, amount={Amount}: inventory full.");
                            player.Session.EnqueueMessageEncrypted(new ServerItemError
                            {
                                ErrorCode = GenericError.ItemInventoryFull
                            });
                        }
                        else
                        {
                            log.Warn($"Failed to validate static loot item {StaticId} for player {player.CharacterId}.");
                        }

                        return false;
                    }

                    SoulbindDeliveredStaticItems(player, () =>
                    {
                        player.Inventory.ItemCreate(InventoryLocation.Inventory, StaticId, Amount, ItemUpdateReason.Loot);
                    });
                    break;
                case LootItemType.VirtualItem:
                    player.QuestManager.ObjectiveUpdate(QuestObjectiveType.VirtualCollect, StaticId, Amount);
                    break;
                default:
                    log.Warn($"Loot item type {Type} is not supported for delivery.");
                    return false;
            }

            Delivered = true;
            log.Trace($"Loot delivery succeeded for player {player.CharacterId}, ownerUnit={OwnerUnitId}, lootUnitId={Id}, type={Type}, staticId={StaticId}, amount={Amount}, sendAsGrant={sendAsGrant}.");

            if (sendAsGrant)
                SendGrant(player);
            return true;
        }

        private void SendGrant(IPlayer player)
        {
            player.Session.EnqueueMessageEncrypted(new ServerLootGrant
            {
                OwnerUnitId  = OwnerUnitId,
                LooterUnitId = WinnerGuid,
                LootItem     = Build()
            });
        }

        public NetworkLootItem Build()
        {
            return new NetworkLootItem
            {
                LootUnitId        = Id,
                Type              = Type,
                ItemId            = StaticId,
                Amount            = Amount,
                RequiresRoll      = RequiresRoll,
                OnlyMasterLootable = OnlyMasterLootable,
                RollTime          = RollTime,
                ItemQuality2Id    = ItemQualityId,
                MasterList        = masterLootCandidates.Values.Select(i => i.ToNetworkIdentity()).ToList()
            };
        }

        public IEnumerable<NetworkLootItem> BuildGrantedNotificationItems()
        {
            NetworkLootItem item = Build();
            item.LootUnitId = 0u;
            item.CanLoot    = true;
            item.Granted    = true;

            return [item];
        }

        public static bool CanDeliverStaticItem(IPlayer player, uint staticId, uint amount, out bool inventoryFull)
        {
            inventoryFull = false;
            if (player?.Inventory == null)
                return false;

            IBag inventoryBag = player.Inventory.SingleOrDefault(bag => bag.Location == InventoryLocation.Inventory);
            if (inventoryBag == null)
            {
                inventoryFull = true;
                return false;
            }

            IItemInfo itemInfo = ItemManager.Instance.GetItemInfo(staticId);
            if (itemInfo == null)
                return false;

            ulong remainingCount = amount;
            if (itemInfo.IsStackable())
            {
                foreach (IItem item in inventoryBag.Where(i => i.Info.Id == itemInfo.Id && i.ExpirationTimeLeft == 0u))
                {
                    if (item.StackCount >= item.Info.Entry.MaxStackCount)
                        continue;

                    remainingCount -= Math.Min(remainingCount, item.Info.Entry.MaxStackCount - item.StackCount);
                    if (remainingCount == 0ul)
                        return true;
                }
            }

            if (remainingCount == 0ul)
                return true;

            uint perNewStack = itemInfo.IsStackable() ? itemInfo.Entry.MaxStackCount : 1u;
            if (perNewStack == 0u)
                return false;

            ulong requiredSlots = (remainingCount + perNewStack - 1ul) / perNewStack;
            if (requiredSlots > inventoryBag.SlotsRemaining)
            {
                inventoryFull = true;
                return false;
            }

            return true;
        }

        public LootRuntimeSnapshotItem CreateRuntimeSnapshot(ulong viewerCharacterId, bool viewerIsTrackedLooter)
        {
            return new LootRuntimeSnapshotItem
            {
                LootUnitId = Id,
                Type = Type,
                ItemId = StaticId,
                Amount = Amount,
                Delivered = Delivered,
                ViewerCanLoot = viewerIsTrackedLooter && viewerCharacterId != 0ul && CanLoot(viewerCharacterId),
                RequiresRoll = RequiresRoll,
                OnlyMasterLootable = OnlyMasterLootable,
                RollTime = RollTime,
                ItemQuality2Id = ItemQualityId,
                ItemQualityVisualEffectIdLoot = ItemQualityVisualEffectIdLoot,
                WinnerCharacterId = WinnerCharacterId,
                WinnerGuid = WinnerGuid,
                EligibleCharacterIds = eligibleIdentities.Keys.OrderBy(id => id).ToList(),
                MasterCharacterIds = masterIdentities.Keys.OrderBy(id => id).ToList(),
                MasterCandidateCharacterIds = masterLootCandidates.Keys.OrderBy(id => id).ToList(),
                MasterListCount = (uint)masterLootCandidates.Count
            };
        }

        private void ClearPendingLootState()
        {
            RequiresRoll       = false;
            OnlyMasterLootable = false;
            rollTimer          = null;

            masterIdentities.Clear();
            masterLootCandidates.Clear();
            bindPickupConfirmations.Clear();
        }

        private void SoulbindDeliveredStaticItems(IPlayer player, Action deliver)
        {
            if (!RequiresBindOnPickupConfirmation())
            {
                deliver();
                return;
            }

            IBag inventoryBag = player.Inventory.SingleOrDefault(bag => bag.Location == InventoryLocation.Inventory);
            if (inventoryBag == null)
            {
                deliver();
                return;
            }

            HashSet<ulong> existingGuids = inventoryBag.Select(i => i.Guid).ToHashSet();
            deliver();

            foreach (IItem item in inventoryBag.Where(i => !existingGuids.Contains(i.Guid) && i.Info?.Entry.Id == StaticId && !i.Soulbound))
                item.MakeSoulbound();
        }

        private uint GetItemQualityId()
        {
            switch (Type)
            {
                case LootItemType.StaticItem:
                    return GameTableManager.Instance.Item?.GetEntry(StaticId)?.ItemQualityId ?? 0u;
                case LootItemType.VirtualItem:
                    return GameTableManager.Instance.VirtualItem?.GetEntry(StaticId)?.ItemQualityId ?? 0u;
                case LootItemType.AccountItem:
                {
                    AccountItemEntry accountItemEntry = GameTableManager.Instance.AccountItem?.GetEntry(StaticId);
                    if (accountItemEntry == null || accountItemEntry.Item2Id == 0u)
                        return 0u;

                    return GameTableManager.Instance.Item?.GetEntry(accountItemEntry.Item2Id)?.ItemQualityId ?? 0u;
                }
                default:
                    return 0u;
            }
        }

        private uint GetItemQualityVisualEffectIdLoot()
        {
            uint itemQualityId = ItemQualityId;
            if (itemQualityId == 0u)
                return 0u;

            return GameTableManager.Instance.ItemQuality?.GetEntry(itemQualityId)?.VisualEffectIdLoot ?? 0u;
        }

    }
}
