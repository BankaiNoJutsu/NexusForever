using System.Collections;
using NexusForever.Database.Auth;
using NexusForever.Database.Auth.Model;
using NexusForever.Game.Abstract;
using NexusForever.Game.Abstract.Account;
using NexusForever.Game.Abstract.Account.Inventory;
using NexusForever.Game.Abstract.Character;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Achievement;
using NexusForever.Game.Character;
using NexusForever.Game.Entity;
using NexusForever.Game.Prerequisite;
using NexusForever.Game.Static.Account;
using NexusForever.Game.Static.Entity;
using NexusForever.GameTable;
using NexusForever.GameTable.Model;
using NexusForever.Network.World.Message.Model;
using NexusForever.Network.World.Message.Static;
using NetworkIdentity = NexusForever.Network.World.Message.Model.Shared.Identity;

namespace NexusForever.Game.Account.Inventory
{
    public class AccountInventoryManager : IAccountInventoryManager
    {
        private readonly Dictionary<ulong, IAccountInventoryItem> items = new();
        private readonly Dictionary<string, List<PendingAccountItem>> pendingGroups = new(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<uint, AccountItemCooldown> cooldowns = new();
        private readonly List<IAccountInventoryItem> deletedItems = [];

        private readonly IAccount account;
        private ulong nextInventoryId = 1ul;
        private ulong nextPendingItemId = 1ul;

        private static readonly IReadOnlyDictionary<uint, uint> CooldownGroupDurations = new Dictionary<uint, uint>
        {
            [1u] = 1800u,
            [2u] = 1800u,
            [3u] = 28800u
        };

        public AccountInventoryManager(IAccount account, AccountModel model)
        {
            this.account = account;

            foreach (AccountInventoryModel itemModel in model.AccountInventory)
            {
                var item = new AccountInventoryItem(account, itemModel);
                items.Add(item.Id, item);
                nextInventoryId = Math.Max(nextInventoryId, item.Id + 1ul);
            }

            foreach (AccountItemCooldownModel cooldownModel in model.AccountItemCooldown)
                cooldowns.TryAdd(cooldownModel.CooldownGroupId, new AccountItemCooldown(cooldownModel));

            foreach (AccountItemCooldownGroupEntry cooldownEntry in GameTableManager.Instance.AccountItemCooldownGroup.Entries)
                cooldowns.TryAdd(cooldownEntry.Id, new AccountItemCooldown(account.Id, cooldownEntry.Id));
        }

        public void Save(AuthContext context)
        {
            foreach (IAccountInventoryItem item in deletedItems)
                item.Save(context);
            deletedItems.Clear();

            foreach (IAccountInventoryItem item in items.Values)
                item.Save(context);

            foreach (AccountItemCooldown cooldown in cooldowns.Values)
                cooldown.Save(context);
        }

        public IAccountInventoryItem GetItem(ulong id)
        {
            return items.TryGetValue(id, out IAccountInventoryItem item) ? item : null;
        }

        public IAccountInventoryItem AddItem(uint accountItemId, NetworkIdentity targetPlayerIdentity = null, AccountItemClaimState claimState = AccountItemClaimState.CanClaim, bool unknown1 = false, bool notify = true)
        {
            if (!CanAddItem(accountItemId))
                throw new ArgumentException($"Account item {accountItemId} does not exist!");

            ulong inventoryId = GetNextInventoryId();
            var item = new AccountInventoryItem(account, inventoryId, accountItemId, targetPlayerIdentity, claimState, unknown1);
            items.Add(item.Id, item);

            if (notify)
                SendItemAdd(item);

            return item;
        }

        public string AddPendingItemGroup(IEnumerable<uint> accountItemIds, NetworkIdentity senderIdentity = null, NetworkIdentity targetPlayerIdentity = null, string group = null, bool notify = true)
        {
            ArgumentNullException.ThrowIfNull(accountItemIds);

            List<uint> accountItemIdList = accountItemIds.ToList();
            if (accountItemIdList.Count == 0)
                throw new ArgumentException("Pending account item group must contain at least one item.", nameof(accountItemIds));

            foreach (uint accountItemId in accountItemIdList)
            {
                if (!CanAddItem(accountItemId))
                    throw new ArgumentException($"Account item {accountItemId} does not exist!", nameof(accountItemIds));
            }

            string groupName = string.IsNullOrWhiteSpace(group)
                ? $"pending:{account.Id}:{Guid.NewGuid():N}"
                : group;

            if (pendingGroups.ContainsKey(groupName))
                throw new ArgumentException($"Pending account item group {groupName} already exists.", nameof(group));

            pendingGroups.Add(groupName, accountItemIdList
                .Select(accountItemId => new PendingAccountItem
                {
                    Id             = GetNextPendingItemId(),
                    AccountItemId  = accountItemId,
                    Group          = groupName,
                    SenderIdentity = CloneIdentity(senderIdentity),
                    TargetIdentity = CloneIdentity(targetPlayerIdentity),
                    ClaimState     = AccountItemClaimState.CanClaim
                })
                .ToList());

            if (notify)
                SendPendingItems();

            return groupName;
        }

        public bool CanAddItem(uint accountItemId)
        {
            return GameTableManager.Instance.AccountItem.GetEntry(accountItemId) != null;
        }

        public bool RemoveItem(ulong id)
        {
            if (!items.Remove(id, out IAccountInventoryItem item))
                return false;

            SendItemDelete(id);

            if (!item.PendingCreate)
            {
                item.EnqueueDelete(true);
                deletedItems.Add(item);
            }

            return true;
        }

        public AccountOperationResult TakeItem(IPlayer player, ulong id)
        {
            if (player == null)
                return SendAccountOperationResult(AccountOperation.TakeItem, AccountOperationResult.NoCharacter);

            if (!items.TryGetValue(id, out IAccountInventoryItem item))
                return SendAccountOperationResult(AccountOperation.TakeItem, AccountOperationResult.InvalidInventoryItem);

            if (item.ClaimState != AccountItemClaimState.CanClaim)
                return SendAccountOperationResult(AccountOperation.TakeItem, AccountOperationResult.AlreadyClaimed);

            if (!IsTargetPlayer(player, item.TargetPlayerIdentity))
                return SendAccountOperationResult(AccountOperation.TakeItem, AccountOperationResult.NoCharacter);

            if (item.Entry.PrerequisiteId != 0u && !PrerequisiteManager.Instance.Meets(player, item.Entry.PrerequisiteId))
                return SendAccountOperationResult(AccountOperation.TakeItem, AccountOperationResult.Prereq);

            if (!TryBuildGrantPlan(player, item.Entry, out List<IAccountItemGrant> grants, out GenericError error))
                return SendAccountOperationResult(AccountOperation.TakeItem, ToAccountOperationResult(error));

            uint cooldownGroupId = item.Entry.AccountItemCooldownGroupId;
            if (cooldownGroupId != 0u && IsOnCooldown(cooldownGroupId))
                return SendAccountOperationResult(AccountOperation.TakeItem, AccountOperationResult.Cooldown);

            foreach (IAccountItemGrant grant in grants)
                grant.Apply(account, player);

            if (cooldownGroupId != 0u)
                SetCooldown(cooldownGroupId);

            if (!HasAccountItemFlag(item.Entry, AccountItemFlag.MultiClaim))
                RemoveItem(id);

            return SendAccountOperationResult(AccountOperation.TakeItem, AccountOperationResult.Ok);
        }

        public AccountOperationResult ClaimPendingItemGroup(IPlayer player, string group)
        {
            if (player == null)
                return SendAccountOperationResult(AccountOperation.ClaimPending, AccountOperationResult.NoCharacter);

            if (!TryGetPendingGroup(group, out List<PendingAccountItem> pendingItems))
                return SendAccountOperationResult(AccountOperation.ClaimPending, AccountOperationResult.InvalidPendingItem);

            if (pendingItems.Any(i => i.ClaimState != AccountItemClaimState.CanClaim))
                return SendAccountOperationResult(AccountOperation.ClaimPending, AccountOperationResult.AlreadyClaimed);

            if (pendingItems.Any(i => !IsTargetPlayer(player, i.TargetIdentity)))
                return SendAccountOperationResult(AccountOperation.ClaimPending, AccountOperationResult.NoCharacter);

            foreach (PendingAccountItem pendingItem in pendingItems)
            {
                if (!CanAddItem(pendingItem.AccountItemId))
                    return SendAccountOperationResult(AccountOperation.ClaimPending, AccountOperationResult.InvalidAccountItem);
            }

            foreach (PendingAccountItem pendingItem in pendingItems)
                AddItem(pendingItem.AccountItemId, pendingItem.TargetIdentity, pendingItem.ClaimState, pendingItem.Unknown1);

            pendingGroups.Remove(group);
            SendPendingItems();

            return SendAccountOperationResult(AccountOperation.ClaimPending, AccountOperationResult.Ok);
        }

        public AccountOperationResult ReturnPendingItemGroup(string group)
        {
            if (!TryGetPendingGroup(group, out _))
                return SendAccountOperationResult(AccountOperation.ReturnPending, AccountOperationResult.InvalidPendingItem);

            pendingGroups.Remove(group);
            SendPendingItems();

            return SendAccountOperationResult(AccountOperation.ReturnPending, AccountOperationResult.Ok);
        }

        public AccountOperationResult GiftPendingItemGroupToCharacter(string group, NetworkIdentity targetCharacter)
        {
            if (!TryGetPendingGroup(group, out List<PendingAccountItem> pendingItems))
                return SendAccountOperationResult(AccountOperation.GiftItem, AccountOperationResult.InvalidPendingItem);

            if (targetCharacter == null || targetCharacter.Id == 0ul)
                return SendAccountOperationResult(AccountOperation.GiftItem, AccountOperationResult.NoCharacter);

            if (targetCharacter.RealmId != 0u && targetCharacter.RealmId != RealmContext.Instance.RealmId)
                return SendAccountOperationResult(AccountOperation.GiftItem, AccountOperationResult.NoCharacter);

            ICharacter character = CharacterManager.Instance.GetCharacter(targetCharacter.Id);
            if (character == null)
                return SendAccountOperationResult(AccountOperation.GiftItem, AccountOperationResult.NoCharacter);

            var targetIdentity = new NetworkIdentity
            {
                RealmId = RealmContext.Instance.RealmId,
                Id      = character.CharacterId
            };

            return GiftPendingItemGroup(group, pendingItems, character.AccountId, targetIdentity);
        }

        public AccountOperationResult GiftPendingItemGroupToAccount(string group, ulong targetAccountId, NetworkIdentity senderCharacter)
        {
            if (!TryGetPendingGroup(group, out List<PendingAccountItem> pendingItems))
                return SendAccountOperationResult(AccountOperation.GiftItem, AccountOperationResult.InvalidPendingItem);

            if (targetAccountId == 0ul || targetAccountId > uint.MaxValue)
                return SendAccountOperationResult(AccountOperation.GiftItem, AccountOperationResult.InvalidFriend);

            foreach (PendingAccountItem pendingItem in pendingItems)
                pendingItem.SenderIdentity = CloneIdentity(senderCharacter);

            return GiftPendingItemGroup(group, pendingItems, (uint)targetAccountId, new NetworkIdentity());
        }

        public void SendInitialPackets()
        {
            SendInventory();
            SendPendingItems();
            SendCooldowns();
        }

        public void SendInventory()
        {
            account.Session.EnqueueMessageEncrypted(new ServerAccountItems
            {
                AccountItems = items.Values
                    .OrderBy(i => i.Id)
                    .Select(i => i.Build())
                    .ToList()
            });
        }

        public void SendPendingItems()
        {
            account.Session.EnqueueMessageEncrypted(new ServerAccountItemsPending
            {
                PendingGroups = pendingGroups.Values
                    .SelectMany(g => g)
                    .OrderBy(i => i.Group, StringComparer.OrdinalIgnoreCase)
                    .ThenBy(i => i.Id)
                    .Select(i => i.Build())
                    .ToList()
            });
        }

        public void SendCooldowns()
        {
            foreach (AccountItemCooldown cooldown in cooldowns.Values.OrderBy(c => c.CooldownGroupId))
            {
                if (cooldown.GetRemainingDuration() == 0u)
                    continue;

                account.Session.EnqueueMessageEncrypted(cooldown.Build());
            }
        }

        public IEnumerator<IAccountInventoryItem> GetEnumerator()
        {
            return items.Values.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        private ulong GetNextInventoryId()
        {
            if (nextInventoryId == 0ul)
                throw new InvalidOperationException("No account inventory ids are available.");

            return nextInventoryId++;
        }

        private ulong GetNextPendingItemId()
        {
            if (nextPendingItemId == 0ul)
                throw new InvalidOperationException("No pending account item ids are available.");

            return nextPendingItemId++;
        }

        private void SendItemAdd(IAccountInventoryItem item)
        {
            account.Session.EnqueueMessageEncrypted(new ServerAccountItemAdd
            {
                AccountItem = item.Build()
            });
        }

        private void SendItemDelete(ulong id)
        {
            account.Session.EnqueueMessageEncrypted(new ServerAccountItemDelete
            {
                Id = id
            });
        }

        private AccountOperationResult SendAccountOperationResult(AccountOperation operation, AccountOperationResult result)
        {
            account.Session.EnqueueMessageEncrypted(new ServerAccountOperationResult
            {
                Operation = operation,
                Result    = result
            });

            return result;
        }

        private bool TryGetPendingGroup(string group, out List<PendingAccountItem> pendingItems)
        {
            pendingItems = null;
            return !string.IsNullOrWhiteSpace(group) && pendingGroups.TryGetValue(group, out pendingItems) && pendingItems.Count != 0;
        }

        private AccountOperationResult GiftPendingItemGroup(string group, List<PendingAccountItem> pendingItems, uint targetAccountId, NetworkIdentity targetIdentity)
        {
            if (targetAccountId == account.Id)
            {
                foreach (PendingAccountItem pendingItem in pendingItems)
                    pendingItem.TargetIdentity = CloneIdentity(targetIdentity);

                SendPendingItems();
                return SendAccountOperationResult(AccountOperation.GiftItem, AccountOperationResult.Ok);
            }

            IPlayer targetPlayer = PlayerManager.Instance.GetPlayerByAccountId(targetAccountId);
            if (targetPlayer == null)
                return SendAccountOperationResult(AccountOperation.GiftItem, AccountOperationResult.NoConnection);

            targetPlayer.Account.InventoryManager.AddPendingItemGroup(
                pendingItems.Select(i => i.AccountItemId),
                pendingItems.FirstOrDefault()?.SenderIdentity,
                targetIdentity);

            pendingGroups.Remove(group);
            SendPendingItems();

            return SendAccountOperationResult(AccountOperation.GiftItem, AccountOperationResult.Ok);
        }

        private bool IsOnCooldown(uint cooldownGroupId)
        {
            if (!cooldowns.TryGetValue(cooldownGroupId, out AccountItemCooldown cooldown))
                return false;

            return cooldown.GetRemainingDuration() > 0u;
        }

        private void SetCooldown(uint cooldownGroupId)
        {
            if (!cooldowns.TryGetValue(cooldownGroupId, out AccountItemCooldown cooldown))
            {
                cooldown = new AccountItemCooldown(account.Id, cooldownGroupId);
                cooldowns.Add(cooldownGroupId, cooldown);
            }

            cooldown.TriggerWithDuration(GetCooldownDuration(cooldownGroupId));
            if (cooldown.GetRemainingDuration() > 0u)
                account.Session.EnqueueMessageEncrypted(cooldown.Build());
        }

        private static uint GetCooldownDuration(uint cooldownGroupId)
        {
            return CooldownGroupDurations.GetValueOrDefault(cooldownGroupId);
        }

        private static bool HasAccountItemFlag(AccountItemEntry entry, AccountItemFlag flag)
        {
            return ((AccountItemFlag)entry.Flags & flag) != 0;
        }

        private static AccountOperationResult ToAccountOperationResult(GenericError error)
        {
            return error switch
            {
                GenericError.Ok                            => AccountOperationResult.Ok,
                GenericError.AccountItemMaxEntitlementCount => AccountOperationResult.MaxEntitlementCount,
                GenericError.GenericUnlockAlreadyUnlocked  => AccountOperationResult.AlreadyClaimed,
                GenericError.InvalidGenericUnlock          => AccountOperationResult.InvalidAccountItem,
                GenericError.ItemBadId                     => AccountOperationResult.InvalidInventoryItem,
                GenericError.ItemBadStaticData             => AccountOperationResult.InvalidAccountItem,
                GenericError.MissingEntitlement            => AccountOperationResult.MissingEntitlement,
                GenericError.Params                        => AccountOperationResult.GenericFail,
                _                                          => AccountOperationResult.GenericFail
            };
        }

        private static bool IsTargetPlayer(IPlayer player, NetworkIdentity targetPlayerIdentity)
        {
            if (targetPlayerIdentity == null || targetPlayerIdentity.Id == 0ul)
                return true;

            return targetPlayerIdentity.Id == player.Identity.Id &&
                (targetPlayerIdentity.RealmId == 0u || targetPlayerIdentity.RealmId == player.Identity.RealmId);
        }

        private static NetworkIdentity CloneIdentity(NetworkIdentity identity)
        {
            if (identity == null)
                return new NetworkIdentity();

            return new NetworkIdentity
            {
                RealmId = identity.RealmId,
                Id      = identity.Id
            };
        }

        private static bool TryBuildGrantPlan(IPlayer player, AccountItemEntry entry, out List<IAccountItemGrant> grants, out GenericError error)
        {
            grants = [];
            error  = GenericError.Ok;

            if (entry.Item2Id != 0u && !TryAddItemGrant(player, entry.Item2Id, grants, out error))
                return false;

            if (entry.AccountCurrencyEnum != 0u && !TryAddAccountCurrencyGrant(entry, grants, out error))
                return false;

            if (entry.EntitlementId != 0u && !TryAddEntitlementGrant(player, entry, grants, out error))
                return false;

            if (entry.GenericUnlockSetId != 0u && !TryAddGenericUnlockGrants(player, entry.GenericUnlockSetId, grants, out error))
                return false;

            if (grants.Count == 0)
            {
                error = entry.GenericUnlockSetId != 0u ? GenericError.GenericUnlockAlreadyUnlocked : GenericError.ItemBadStaticData;
                return false;
            }

            return true;
        }

        private static bool TryAddItemGrant(IPlayer player, uint item2Id, List<IAccountItemGrant> grants, out GenericError error)
        {
            error = GenericError.Ok;

            IItemInfo itemInfo = ItemManager.Instance.GetItemInfo(item2Id);
            if (itemInfo == null)
            {
                error = GenericError.ItemBadStaticData;
                return false;
            }

            if (!CanHoldItem(player, itemInfo))
            {
                error = GenericError.ItemInventoryFull;
                return false;
            }

            grants.Add(new ItemGrant(itemInfo));
            return true;
        }

        private static bool CanHoldItem(IPlayer player, IItemInfo itemInfo)
        {
            if (player.Inventory.GetInventorySlotsRemaining(InventoryLocation.Inventory) > 0u)
                return true;

            if (!itemInfo.IsStackable())
                return false;

            return player.Inventory
                .Where(b => b.Location == InventoryLocation.Inventory)
                .SelectMany(b => b)
                .Any(i => i.Info.Id == itemInfo.Id && i.StackCount < i.Info.Entry.MaxStackCount);
        }

        private static bool TryAddAccountCurrencyGrant(AccountItemEntry entry, List<IAccountItemGrant> grants, out GenericError error)
        {
            error = GenericError.Ok;

            var currencyType = (AccountCurrencyType)entry.AccountCurrencyEnum;
            if (!Enum.IsDefined(typeof(AccountCurrencyType), currencyType) || entry.AccountCurrencyAmount == 0ul)
            {
                error = GenericError.ItemBadStaticData;
                return false;
            }

            grants.Add(new AccountCurrencyGrant(currencyType, entry.AccountCurrencyAmount));
            return true;
        }

        private static bool TryAddEntitlementGrant(IPlayer player, AccountItemEntry entry, List<IAccountItemGrant> grants, out GenericError error)
        {
            error = GenericError.Ok;

            EntitlementEntry entitlementEntry = GameTableManager.Instance.Entitlement.GetEntry(entry.EntitlementId);
            if (entitlementEntry == null)
            {
                error = GenericError.ItemBadStaticData;
                return false;
            }

            var entitlementFlags = (EntitlementFlags)entitlementEntry.Flags;
            if (entitlementFlags.HasFlag(EntitlementFlags.Disabled))
            {
                error = GenericError.MissingEntitlement;
                return false;
            }

            if (entry.EntitlementCount == 0u || entry.EntitlementCount > int.MaxValue)
            {
                error = GenericError.ItemBadStaticData;
                return false;
            }

            bool characterEntitlement = entitlementFlags.HasFlag(EntitlementFlags.Character);
            var entitlementType = (EntitlementType)entitlementEntry.Id;
            uint currentAmount = characterEntitlement
                ? player.EntitlementManager.GetEntitlement(entitlementType)?.Amount ?? 0u
                : player.Account.EntitlementManager.GetEntitlement(entitlementType)?.Amount ?? 0u;

            if (currentAmount + (ulong)entry.EntitlementCount > entitlementEntry.MaxCount)
            {
                error = GenericError.AccountItemMaxEntitlementCount;
                return false;
            }

            grants.Add(new EntitlementGrant(entitlementType, (int)entry.EntitlementCount, characterEntitlement));
            return true;
        }

        private static bool TryAddGenericUnlockGrants(IPlayer player, uint genericUnlockSetId, List<IAccountItemGrant> grants, out GenericError error)
        {
            error = GenericError.Ok;

            GenericUnlockSetEntry unlockSetEntry = GameTableManager.Instance.GenericUnlockSet.GetEntry(genericUnlockSetId);
            if (unlockSetEntry == null)
            {
                error = GenericError.InvalidGenericUnlock;
                return false;
            }

            foreach (uint genericUnlockEntryId in GetGenericUnlockEntryIds(unlockSetEntry))
            {
                if (genericUnlockEntryId == 0u)
                    continue;

                GenericUnlockEntryEntry genericUnlockEntry = GameTableManager.Instance.GenericUnlockEntry.GetEntry(genericUnlockEntryId);
                if (genericUnlockEntry == null || genericUnlockEntry.Id > ushort.MaxValue)
                {
                    error = GenericError.InvalidGenericUnlock;
                    return false;
                }

                if (player.Account.GenericUnlockManager.IsUnlocked(genericUnlockEntry.GenericUnlockTypeEnum, genericUnlockEntry.UnlockObject))
                    continue;

                grants.Add(new GenericUnlockGrant((ushort)genericUnlockEntry.Id));
            }

            return true;
        }

        private static IEnumerable<uint> GetGenericUnlockEntryIds(GenericUnlockSetEntry entry)
        {
            yield return entry.GenericUnlockEntryId00;
            yield return entry.GenericUnlockEntryId01;
            yield return entry.GenericUnlockEntryId02;
            yield return entry.GenericUnlockEntryId03;
            yield return entry.GenericUnlockEntryId04;
            yield return entry.GenericUnlockEntryId05;
        }

        private interface IAccountItemGrant
        {
            void Apply(IAccount account, IPlayer player);
        }

        private readonly record struct ItemGrant(IItemInfo ItemInfo) : IAccountItemGrant
        {
            public void Apply(IAccount account, IPlayer player)
            {
                player.Inventory.ItemCreate(InventoryLocation.Inventory, ItemInfo, 1u, ItemUpdateReason.PlayerRequested);
            }
        }

        private readonly record struct AccountCurrencyGrant(AccountCurrencyType CurrencyType, ulong Amount) : IAccountItemGrant
        {
            public void Apply(IAccount account, IPlayer player)
            {
                account.CurrencyManager.CurrencyAddAmount(CurrencyType, Amount);
                AccountCurrencyAchievementUpdater.Update(player, CurrencyType, Amount);
            }
        }

        private readonly record struct EntitlementGrant(EntitlementType Type, int Amount, bool Character) : IAccountItemGrant
        {
            public void Apply(IAccount account, IPlayer player)
            {
                if (Character)
                    player.EntitlementManager.UpdateEntitlement(Type, Amount);
                else
                    account.EntitlementManager.UpdateEntitlement(Type, Amount);
            }
        }

        private readonly record struct GenericUnlockGrant(ushort GenericUnlockEntryId) : IAccountItemGrant
        {
            public void Apply(IAccount account, IPlayer player)
            {
                account.GenericUnlockManager.Unlock(GenericUnlockEntryId);
            }
        }

        private sealed class PendingAccountItem
        {
            public ulong Id { get; init; }
            public uint AccountItemId { get; init; }
            public string Group { get; init; }
            public NetworkIdentity SenderIdentity { get; set; }
            public NetworkIdentity TargetIdentity { get; set; }
            public AccountItemClaimState ClaimState { get; init; }
            public bool Unknown1 { get; init; }

            public ServerAccountItemsPending.PendingAccountItemGroup Build()
            {
                return new ServerAccountItemsPending.PendingAccountItemGroup
                {
                    Id             = Id,
                    AccountItemId  = AccountItemId,
                    Group          = Group,
                    SenderIdentity = CloneIdentity(SenderIdentity),
                    ClaimState     = ClaimState,
                    TargetIdentity = CloneIdentity(TargetIdentity)
                };
            }
        }
    }
}
