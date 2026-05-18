using NexusForever.Game;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Achievement;
using NexusForever.Game.Static.Account;
using NexusForever.Game.Static.Entity;
using NexusForever.Game.Static.Loot;
using NexusForever.Game.Static.Quest;
using NexusForever.GameTable;
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
        private const uint ACCOUNT_CURRENCY_SHOWER_ITEM_LIMIT = 50u;

        public uint Id { get; }
        public uint OwnerUnitId { get; private set; }
        public uint StaticId { get; }
        public LootItemType Type { get; }
        public uint Amount { get; private set; }
        public uint ItemQualityId => GetItemQualityId();

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

        public bool CanMasterAssign(ulong characterId)
        {
            return !Delivered && OnlyMasterLootable && masterIdentities.ContainsKey(characterId);
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
            if (!RequiresRoll || Delivered || rollFinalised)
                return false;

            if (!Enum.IsDefined(typeof(LootRollAction), action))
                return false;

            if (!eligibleIdentities.ContainsKey(player.CharacterId))
                return false;

            if (rollRecords.ContainsKey(player.CharacterId))
                return false;

            rollRecords.Add(player.CharacterId, new LootRollRecord
            {
                Identity = player.Identity,
                Action   = action,
                Value    = GetRollValue(action)
            });

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
            rollFinalised = true;

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
                return false;

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
                    player.Inventory.ItemCreate(InventoryLocation.Inventory, StaticId, Amount, ItemUpdateReason.Loot);
                    break;
                case LootItemType.VirtualItem:
                    player.QuestManager.ObjectiveUpdate(QuestObjectiveType.VirtualCollect, StaticId, Amount);
                    break;
                default:
                    log.Warn($"Loot item type {Type} is not supported for delivery.");
                    return false;
            }

            Delivered = true;

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
            if (Type == LootItemType.AccountCurrency)
                return BuildGrantedAccountCurrencyNotificationItems();

            NetworkLootItem item = Build();
            item.LootUnitId = 0u;
            item.CanLoot    = true;
            item.Granted    = true;

            return [item];
        }

        private IEnumerable<NetworkLootItem> BuildGrantedAccountCurrencyNotificationItems()
        {
            uint itemCount = Math.Min(Amount, ACCOUNT_CURRENCY_SHOWER_ITEM_LIMIT);
            if (itemCount == 0u)
                yield break;

            uint baseAmount = Amount / itemCount;
            uint remainder  = Amount % itemCount;
            for (uint i = 0u; i < itemCount; i++)
            {
                yield return new NetworkLootItem
                {
                    LootUnitId     = 0u,
                    Type           = Type,
                    ItemId         = StaticId,
                    Amount         = baseAmount + (i < remainder ? 1u : 0u),
                    CanLoot        = true,
                    Granted        = true,
                    ItemQuality2Id = ItemQualityId
                };
            }
        }

        private uint GetItemQualityId()
        {
            return Type == LootItemType.StaticItem
                ? GameTableManager.Instance.Item.GetEntry(StaticId)?.ItemQualityId ?? 0u
                : 0u;
        }
    }
}
