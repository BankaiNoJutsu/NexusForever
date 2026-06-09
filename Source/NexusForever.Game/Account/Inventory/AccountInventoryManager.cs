using System.Collections;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
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
using NexusForever.Shared;
using NetworkIdentity = NexusForever.Network.World.Message.Model.Shared.Identity;

namespace NexusForever.Game.Account.Inventory
{
    public class AccountInventoryManager : IAccountInventoryManager
    {
        private const string EntitlementTableName = "Entitlement.tbl";
        private const string GenericUnlockSetTableName = "GenericUnlockSet.tbl";
        private const string GenericUnlockEntryTableName = "GenericUnlockEntry.tbl";

        private readonly Dictionary<ulong, IAccountInventoryItem> items = new();
        private readonly Dictionary<string, List<PendingAccountItem>> pendingGroups = new(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<uint, AccountItemCooldown> cooldowns = new();
        private readonly List<IAccountInventoryItem> deletedItems = [];
        private readonly HashSet<string> deletedPendingGroups = new(StringComparer.OrdinalIgnoreCase);
        private readonly HashSet<ulong> deletedPendingItemIds = [];
        private bool pendingItemsDirty;

        private readonly IAccount account;
        private readonly ILogger<AccountInventoryManager> log;
        private readonly IPendingAccountItemGroupDelivery pendingAccountItemGroupDelivery;
        private readonly DailyLoginRewardManager dailyLoginRewardManager;
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
            IServiceProvider serviceProvider = LegacyServiceProvider.Provider;
            log = serviceProvider?.GetService<ILogger<AccountInventoryManager>>()
                ?? NullLogger<AccountInventoryManager>.Instance;
            pendingAccountItemGroupDelivery = serviceProvider?.GetService<IPendingAccountItemGroupDelivery>()
                ?? new RetailPendingAccountItemGroupDelivery(
                    serviceProvider?.GetService<IAccountPendingItemRepository>() ?? new AccountPendingItemRepository(),
                    serviceProvider?.GetService<ILogger<RetailPendingAccountItemGroupDelivery>>()
                    ?? NullLogger<RetailPendingAccountItemGroupDelivery>.Instance);

            foreach (AccountInventoryModel itemModel in model.AccountInventory)
            {
                var item = new AccountInventoryItem(account, itemModel);
                items.Add(item.Id, item);
                nextInventoryId = Math.Max(nextInventoryId, item.Id + 1ul);
            }

            foreach (AccountItemCooldownModel cooldownModel in model.AccountItemCooldown)
                cooldowns.TryAdd(cooldownModel.CooldownGroupId, new AccountItemCooldown(cooldownModel));

            foreach (AccountItemCooldownGroupEntry cooldownEntry in GameTableManager.Instance.AccountItemCooldownGroup?.Entries ?? [])
                cooldowns.TryAdd(cooldownEntry.Id, new AccountItemCooldown(account.Id, cooldownEntry.Id));

            dailyLoginRewardManager = new DailyLoginRewardManager(account, model);

            foreach (AccountPendingItemModel pendingModel in model.AccountPendingItem)
            {
                if (!pendingGroups.TryGetValue(pendingModel.GroupName, out List<PendingAccountItem> groupItems))
                {
                    groupItems = [];
                    pendingGroups.Add(pendingModel.GroupName, groupItems);
                }

                groupItems.Add(new PendingAccountItem
                {
                    Id              = pendingModel.PendingItemId,
                    AccountItemId   = pendingModel.AccountItemId,
                    Group           = pendingModel.GroupName,
                    SenderAccountId = pendingModel.SenderAccountId,
                    TargetAccountId = pendingModel.SenderAccountId != 0u && pendingModel.TargetCharacterId == 0ul ? pendingModel.Id : 0ul,
                    SenderIdentity  = new NetworkIdentity
                    {
                        RealmId = pendingModel.SenderRealmId,
                        Id      = pendingModel.SenderCharacterId
                    },
                    TargetIdentity  = new NetworkIdentity
                    {
                        RealmId = pendingModel.TargetRealmId,
                        Id      = pendingModel.TargetCharacterId
                    },
                    ClaimState              = (AccountItemClaimState)pendingModel.ClaimState,
                    HasTargetPlayerIdentity = pendingModel.HasTargetPlayerIdentity
                });

                nextPendingItemId = Math.Max(nextPendingItemId, pendingModel.PendingItemId + 1ul);
            }
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

            dailyLoginRewardManager?.Save(context);

            if (!pendingItemsDirty && deletedPendingGroups.Count == 0 && deletedPendingItemIds.Count == 0)
                return;

            List<AccountPendingItemModel> existing = context.AccountPendingItem
                .Where(p => p.Id == account.Id)
                .ToList();
            if (existing.Count != 0)
                context.AccountPendingItem.RemoveRange(existing);

            foreach (List<PendingAccountItem> group in pendingGroups.Values)
            {
                foreach (PendingAccountItem pendingItem in group)
                {
                    context.AccountPendingItem.Add(new AccountPendingItemModel
                    {
                        Id                = account.Id,
                        PendingItemId     = pendingItem.Id,
                        GroupName         = pendingItem.Group,
                        AccountItemId     = pendingItem.AccountItemId,
                        SenderAccountId   = pendingItem.SenderAccountId,
                        SenderRealmId     = pendingItem.SenderIdentity?.RealmId ?? 0,
                        SenderCharacterId = pendingItem.SenderIdentity?.Id ?? 0ul,
                        TargetRealmId     = pendingItem.TargetIdentity?.RealmId ?? 0,
                        TargetCharacterId = pendingItem.TargetIdentity?.Id ?? 0ul,
                        ClaimState              = (byte)pendingItem.ClaimState,
                        HasTargetPlayerIdentity = pendingItem.HasTargetPlayerIdentity
                    });
                }
            }

            pendingItemsDirty      = false;
            deletedPendingGroups.Clear();
            deletedPendingItemIds.Clear();
        }

        public IAccountInventoryItem GetItem(ulong id)
        {
            return items.TryGetValue(id, out IAccountInventoryItem item) ? item : null;
        }

        public IAccountInventoryItem AddItem(uint accountItemId, NetworkIdentity targetPlayerIdentity = null, AccountItemClaimState claimState = AccountItemClaimState.CanClaim, bool hasTargetPlayerIdentity = false, bool notify = true)
        {
            if (!CanAddItem(accountItemId))
                throw new ArgumentException($"Account item {accountItemId} does not exist!");

            ulong inventoryId = GetNextInventoryId();
            var item = new AccountInventoryItem(account, inventoryId, accountItemId, targetPlayerIdentity, claimState, hasTargetPlayerIdentity);
            items.Add(item.Id, item);

            if (notify)
                SendItemAdd(item);

            return item;
        }

        public string AddPendingItemGroup(IEnumerable<uint> accountItemIds, NetworkIdentity senderIdentity = null, NetworkIdentity targetPlayerIdentity = null, string group = null, bool notify = true, uint senderAccountId = 0u, ulong targetAccountId = 0ul)
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
                    Id                = GetNextPendingItemId(),
                    AccountItemId     = accountItemId,
                    Group             = groupName,
                    SenderAccountId   = senderAccountId,
                    TargetAccountId   = targetAccountId,
                    SenderIdentity    = CloneIdentity(senderIdentity),
                    TargetIdentity    = CloneIdentity(targetPlayerIdentity),
                    ClaimState              = AccountItemClaimState.CanClaim,
                    HasTargetPlayerIdentity = targetPlayerIdentity?.Id != 0ul
                })
                .ToList());

            pendingItemsDirty = true;

            if (notify)
                SendPendingGroupAdded(groupName);

            return groupName;
        }

        public bool CanAddItem(uint accountItemId)
        {
            return GameTableManager.Instance.AccountItem?.GetEntry(accountItemId) != null;
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
            if (!items.TryGetValue(id, out IAccountInventoryItem item))
                return SendAccountOperationResult(AccountOperation.TakeItem, AccountOperationResult.InvalidInventoryItem);

            if (item.ClaimState != AccountItemClaimState.CanClaim)
                return SendAccountOperationResult(AccountOperation.TakeItem, AccountOperationResult.AlreadyClaimed);

            if (!IsTargetPlayer(player, item.TargetPlayerIdentity))
                return SendAccountOperationResult(AccountOperation.TakeItem, AccountOperationResult.NoCharacter);

            bool directAccountGrant = IsImmediateAccountGrant(item.Entry);
            if (directAccountGrant && HasAccountItemFlag(item.Entry, AccountItemFlag.MultiClaim))
                return SendAccountOperationResult(AccountOperation.TakeItem, AccountOperationResult.NoCharacter);

            if (!directAccountGrant && player != null && item.Entry.PrerequisiteId != 0u && !PrerequisiteEvaluation.MeetsAccountItem(player, item.Entry.PrerequisiteId))
                return SendAccountOperationResult(AccountOperation.TakeItem, AccountOperationResult.Prereq);

            if (player == null && !directAccountGrant)
                return SendAccountOperationResult(AccountOperation.TakeItem, AccountOperationResult.NoCharacter);

            List<IAccountItemGrant> grants;
            GenericError error;
            bool builtGrantPlan = directAccountGrant
                ? TryBuildImmediateAccountGrantPlan(item, out grants, out error)
                : TryBuildGrantPlan(player, item.Entry, out grants, out error);
            if (!builtGrantPlan)
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
                return SendPendingOperationResult(AccountOperation.ClaimPending, AccountOperationResult.NoCharacter);

            if (!TryGetPendingGroup(group, out List<PendingAccountItem> pendingItems))
                return SendPendingOperationResult(AccountOperation.ClaimPending, AccountOperationResult.InvalidPendingItem);

            if (pendingItems.Any(i => i.ClaimState != AccountItemClaimState.CanClaim))
                return SendPendingOperationResult(AccountOperation.ClaimPending, AccountOperationResult.AlreadyClaimed);

            if (pendingItems.Any(i => !IsTargetPlayer(player, i.TargetIdentity)))
                return SendPendingOperationResult(AccountOperation.ClaimPending, AccountOperationResult.NoCharacter);

            foreach (PendingAccountItem pendingItem in pendingItems)
            {
                if (!CanAddItem(pendingItem.AccountItemId))
                    return SendPendingOperationResult(AccountOperation.ClaimPending, AccountOperationResult.InvalidAccountItem);
            }

            foreach (PendingAccountItem pendingItem in pendingItems)
                AddItem(pendingItem.AccountItemId, pendingItem.TargetIdentity, pendingItem.ClaimState, pendingItem.HasTargetPlayerIdentity);

            RemovePendingGroup(group, notify: true);

            return SendAccountOperationResult(AccountOperation.ClaimPending, AccountOperationResult.Ok);
        }

        public AccountOperationResult ReturnPendingItemGroup(IPlayer player, string group)
        {
            if (player == null)
                return SendPendingOperationResult(AccountOperation.ReturnPending, AccountOperationResult.NoCharacter);

            if (!TryGetPendingGroup(group, out List<PendingAccountItem> pendingItems))
                return SendPendingOperationResult(AccountOperation.ReturnPending, AccountOperationResult.InvalidPendingItem);

            if (!TryGetPendingSender(pendingItems, out uint senderAccountId, out NetworkIdentity senderIdentity))
                return SendPendingOperationResult(AccountOperation.ReturnPending, AccountOperationResult.CannotReturn);

            if (senderAccountId == account.Id)
                return SendPendingOperationResult(AccountOperation.ReturnPending, AccountOperationResult.CannotReturn);

            return TransferPendingItemGroup(player, group, pendingItems, senderAccountId, senderIdentity, AccountOperation.ReturnPending, PendingAccountItemGroupTransferKind.ReturnToSender);
        }

        public AccountOperationResult GiftPendingItemGroupToCharacter(IPlayer player, string group, NetworkIdentity targetCharacter)
        {
            if (player == null)
                return SendPendingOperationResult(AccountOperation.GiftItem, AccountOperationResult.NoCharacter);

            if (!TryGetPendingGroup(group, out List<PendingAccountItem> pendingItems))
                return SendPendingOperationResult(AccountOperation.GiftItem, AccountOperationResult.InvalidPendingItem);

            if (IsGiftedGroup(pendingItems))
                return SendPendingOperationResult(AccountOperation.GiftItem, AccountOperationResult.NoRegift);

            if (targetCharacter == null || targetCharacter.Id == 0ul)
                return SendPendingOperationResult(AccountOperation.GiftItem, AccountOperationResult.NoCharacter);

            if (targetCharacter.RealmId != 0u && targetCharacter.RealmId != RealmContext.Instance.RealmId)
                return SendPendingOperationResult(AccountOperation.GiftItem, AccountOperationResult.NoCharacter);

            ICharacter character = CharacterManager.Instance.GetCharacter(targetCharacter.Id);
            if (character == null)
                return SendPendingOperationResult(AccountOperation.GiftItem, AccountOperationResult.NoCharacter);

            var targetIdentity = new NetworkIdentity
            {
                RealmId = RealmContext.Instance.RealmId,
                Id      = character.CharacterId
            };

            return GiftPendingItemGroup(player, group, pendingItems, character.AccountId, targetIdentity, PendingAccountItemGroupTransferKind.GiftToCharacter);
        }

        public AccountOperationResult GiftPendingItemGroupToAccount(IPlayer player, string group, ulong targetAccountId, NetworkIdentity senderCharacter)
        {
            if (player == null)
                return SendPendingOperationResult(AccountOperation.GiftItem, AccountOperationResult.NoCharacter);

            if (!IsCurrentPlayer(player, senderCharacter))
                return SendPendingOperationResult(AccountOperation.GiftItem, AccountOperationResult.NoCharacter);

            if (!TryGetPendingGroup(group, out List<PendingAccountItem> pendingItems))
                return SendPendingOperationResult(AccountOperation.GiftItem, AccountOperationResult.InvalidPendingItem);

            if (IsGiftedGroup(pendingItems))
                return SendPendingOperationResult(AccountOperation.GiftItem, AccountOperationResult.NoRegift);

            if (targetAccountId == 0ul || targetAccountId > uint.MaxValue)
                return SendPendingOperationResult(AccountOperation.GiftItem, AccountOperationResult.InvalidFriend);

            return GiftPendingItemGroup(player, group, pendingItems, (uint)targetAccountId, new NetworkIdentity(), PendingAccountItemGroupTransferKind.GiftToAccount);
        }

        public void SendInitialPackets()
        {
            log.LogInformation("StorefrontCatalogDiagnostics account {AccountId}: initial account packets inventoryItems={InventoryItemCount} inventory=[{InventoryItems}] pendingGroups={PendingGroupCount} pendingRows={PendingItemCount} pending=[{PendingGroups}] activeCooldowns={ActiveCooldownCount}.",
                account.Id, items.Count, FormatInventoryItems(), pendingGroups.Count, CountPendingItems(), FormatPendingGroups(), BuildActiveCooldownList().Count);

            SendInventory();
            SendPendingItems();
            SendCooldowns();
            SendDailyLoginUpdate();
        }

        public void SendDailyLoginUpdate()
        {
            dailyLoginRewardManager?.SendDailyLoginUpdate();
        }

        public AccountOperationResult ClaimDailyLoginReward()
        {
            return dailyLoginRewardManager?.TryClaimReward() ?? AccountOperationResult.GenericFail;
        }

        public uint GetDailyLoginDaysTotal()
        {
            return dailyLoginRewardManager?.GetLoginDaysTotal() ?? 0u;
        }

        public uint GetDailyLoginRewardsAvailable()
        {
            return dailyLoginRewardManager?.GetRewardsAvailable() ?? 0u;
        }

        public uint GetAccountInventoryItemCount()
        {
            return (uint)items.Count;
        }

        public void SendInventory()
        {
            log.LogInformation("StorefrontCatalogDiagnostics account {AccountId}: ServerAccountItems rows={InventoryItemCount} inventory=[{InventoryItems}].",
                account.Id, items.Count, FormatInventoryItems());

            if (items.Count == 0)
            {
                log.LogInformation("StorefrontCatalogDiagnostics account {AccountId}: skipping empty ServerAccountItems list.", account.Id);
                return;
            }

            var accountItems = items.Values
                .OrderBy(i => i.Id)
                .Select(i => i.Build())
                .ToList();

            var cacheAppend = new ServerAccountItemCacheListAppend
            {
                UnusedLeadingField = 0u
            };
            cacheAppend.AccountItems.AddRange(accountItems);

            log.LogInformation("StorefrontCatalogDiagnostics account {AccountId}: ServerAccountItemCacheListAppend rows={InventoryItemCount} inventory=[{InventoryItems}].",
                account.Id, accountItems.Count, FormatInventoryItems());
            account.Session.EnqueueMessageEncrypted(cacheAppend);

            account.Session.EnqueueMessageEncrypted(new ServerAccountItems
            {
                AccountItems = accountItems
            });
        }

        public void SendPendingItems()
        {
            log.LogInformation("StorefrontCatalogDiagnostics account {AccountId}: pending rows={PendingItemCount} groups={PendingGroupCount} pending=[{PendingGroups}].",
                account.Id, CountPendingItems(), pendingGroups.Count, FormatPendingGroups());

            List<ServerAccountItemsPending.PendingAccountItemGroup> pendingItems = pendingGroups.Values
                .SelectMany(g => g)
                .OrderBy(i => i.Group, StringComparer.OrdinalIgnoreCase)
                .ThenBy(i => i.Id)
                .Select(i => i.Build())
                .ToList();

            if (pendingItems.Count == 0)
            {
                log.LogInformation("StorefrontCatalogDiagnostics account {AccountId}: sending ServerAccountPendingItemsClear instead of empty ServerAccountItemsPending list.", account.Id);
                account.Session.EnqueueMessageEncrypted(new ServerAccountPendingItemsClear());
                return;
            }

            account.Session.EnqueueMessageEncrypted(new ServerAccountItemsPending
            {
                PendingGroups = pendingItems
            });
        }

        private void SendPendingGroupAdded(string groupName)
        {
            if (!TryGetPendingGroup(groupName, out List<PendingAccountItem> pendingItems))
                return;

            foreach (PendingAccountItem pendingItem in pendingItems)
            {
                account.Session.EnqueueMessageEncrypted(new ServerAccountPendingItemAdd
                {
                    PendingGroup = pendingItem.Build()
                });
            }
        }

        private void RemovePendingGroup(string groupName, bool notify)
        {
            if (!pendingGroups.Remove(groupName, out List<PendingAccountItem> pendingItems))
                return;

            pendingItemsDirty = true;
            deletedPendingGroups.Add(groupName);

            if (!notify)
                return;

            account.Session.EnqueueMessageEncrypted(new ServerAccountPendingItemGroupDelete
            {
                Value = groupName,
                Flag  = true
            });
        }

        public void SendCooldowns()
        {
            List<ServerAccountItemCooldowns.Cooldown> activeCooldowns = BuildActiveCooldownList();

            log.LogInformation("StorefrontCatalogDiagnostics account {AccountId}: ServerAccountItemCooldowns activeRows={CooldownCount} cooldowns=[{Cooldowns}].",
                account.Id, activeCooldowns.Count, FormatCooldowns(activeCooldowns));

            if (activeCooldowns.Count != 0)
                account.Session.EnqueueMessageEncrypted(new ServerAccountItemCooldowns(activeCooldowns));
        }

        private int CountPendingItems()
        {
            return pendingGroups.Values.Sum(group => group.Count);
        }

        private string FormatInventoryItems()
        {
            return items.Count == 0
                ? "none"
                : string.Join(",", items.Values
                    .OrderBy(item => item.Id)
                    .Select(item => $"{item.Id}:{item.AccountItemId}:{item.ClaimState}"));
        }

        private string FormatPendingGroups()
        {
            return pendingGroups.Count == 0
                ? "none"
                : string.Join(",", pendingGroups
                    .OrderBy(group => group.Key, StringComparer.OrdinalIgnoreCase)
                    .Select(group => $"{group.Key}[{string.Join("|", group.Value.OrderBy(pendingItem => pendingItem.Id).Select(pendingItem => $"{pendingItem.Id}:{pendingItem.AccountItemId}:{pendingItem.ClaimState}"))}]"));
        }

        private List<ServerAccountItemCooldowns.Cooldown> BuildActiveCooldownList()
        {
            return cooldowns.Values
                .OrderBy(cooldown => cooldown.CooldownGroupId)
                .Select(cooldown => cooldown.BuildListEntry())
                .Where(cooldown => cooldown.CooldownInSeconds != 0u)
                .ToList();
        }

        private static string FormatCooldowns(IEnumerable<ServerAccountItemCooldowns.Cooldown> activeCooldowns)
        {
            return string.Join(",", activeCooldowns.Select(cooldown => $"{cooldown.AccountItemCooldownGroup}:{cooldown.CooldownInSeconds}"));
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
            log.LogInformation("StorefrontCatalogDiagnostics account {AccountId}: sending account item add inventoryId={InventoryId} accountItemId={AccountItemId} claimState={ClaimState} hasTarget={HasTargetPlayerIdentity} target={TargetPlayerIdentity}.",
                account.Id, item.Id, item.AccountItemId, item.ClaimState, item.HasTargetPlayerIdentity, item.TargetPlayerIdentity);

            account.Session.EnqueueMessageEncrypted(new ServerAccountItemCacheAdd
            {
                UnusedLeadingField = 0u,
                AccountItem        = item.Build()
            });
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
            log.LogInformation("StorefrontCatalogDiagnostics account {AccountId}: sending ServerAccountOperationResult operation={Operation} result={Result}.",
                account.Id, operation, result);

            account.Session.EnqueueMessageEncrypted(new ServerAccountOperationResult
            {
                Operation = operation,
                Result    = result
            });

            return result;
        }

        private AccountOperationResult SendPendingOperationResult(AccountOperation operation, AccountOperationResult result)
        {
            SendPendingItems();
            return SendAccountOperationResult(operation, result);
        }

        private bool TryGetPendingGroup(string group, out List<PendingAccountItem> pendingItems)
        {
            pendingItems = null;
            return !string.IsNullOrWhiteSpace(group) && pendingGroups.TryGetValue(group, out pendingItems) && pendingItems.Count != 0;
        }

        private AccountOperationResult GiftPendingItemGroup(IPlayer player, string group, List<PendingAccountItem> pendingItems, uint targetAccountId, NetworkIdentity targetIdentity, PendingAccountItemGroupTransferKind transferKind)
        {
            if (targetAccountId == account.Id)
            {
                foreach (PendingAccountItem pendingItem in pendingItems)
                    pendingItem.TargetIdentity = CloneIdentity(targetIdentity);

                SendPendingItems();
                return SendAccountOperationResult(AccountOperation.GiftItem, AccountOperationResult.Ok);
            }

            return TransferPendingItemGroup(player, group, pendingItems, targetAccountId, targetIdentity, AccountOperation.GiftItem, transferKind);
        }

        private AccountOperationResult TransferPendingItemGroup(IPlayer player, string group, List<PendingAccountItem> pendingItems, uint targetAccountId, NetworkIdentity targetIdentity, AccountOperation operation, PendingAccountItemGroupTransferKind transferKind)
        {
            var request = new PendingAccountItemGroupDeliveryRequest
            {
                SourceGroup     = group,
                TransferKind    = transferKind,
                Operation       = operation,
                SourceAccountId = account.Id,
                TargetAccountId = targetAccountId,
                AccountItemIds  = pendingItems.Select(i => i.AccountItemId).ToList(),
                SenderIdentity  = GetPlayerIdentity(player),
                TargetIdentity  = CloneIdentity(targetIdentity)
            };
            AccountOperationResult deliveryResult = pendingAccountItemGroupDelivery.Deliver(request);
            if (deliveryResult != AccountOperationResult.Ok)
            {
                AccountRuntimeEvidenceCollector.RecordPendingGroupTransferIfArmed(player, request, deliveryResult);
                if (deliveryResult != AccountOperationResult.NoConnection)
                {
                    log.LogDebug("Pending account item group {Group} transfer {TransferKind} returned {Result} for operation {Operation}.",
                        group, transferKind, deliveryResult, operation);
                }

                return SendPendingOperationResult(operation, deliveryResult);
            }

            RemovePendingGroup(group, notify: true);

            return SendAccountOperationResult(operation, AccountOperationResult.Ok);
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

            if (player == null)
                return false;

            return targetPlayerIdentity.Id == player.Identity.Id &&
                (targetPlayerIdentity.RealmId == 0u || targetPlayerIdentity.RealmId == player.Identity.RealmId);
        }

        private static bool IsCurrentPlayer(IPlayer player, NetworkIdentity identity)
        {
            return player != null &&
                identity != null &&
                identity.Id == player.Identity.Id &&
                (identity.RealmId == 0u || identity.RealmId == player.Identity.RealmId);
        }

        private static bool IsGiftedGroup(IEnumerable<PendingAccountItem> pendingItems)
        {
            return pendingItems.Any(p => p.SenderAccountId != 0u || p.SenderIdentity?.Id != 0ul);
        }

        private static bool TryGetPendingSender(IReadOnlyList<PendingAccountItem> pendingItems, out uint senderAccountId, out NetworkIdentity senderIdentity)
        {
            senderAccountId = 0u;
            senderIdentity  = null;

            PendingAccountItem first = pendingItems.FirstOrDefault();
            if (first?.SenderAccountId == 0u || first.SenderIdentity?.Id == 0ul)
                return false;

            if (first.SenderIdentity.RealmId != 0u && first.SenderIdentity.RealmId != RealmContext.Instance.RealmId)
                return false;

            if (pendingItems.Any(p => p.SenderAccountId != first.SenderAccountId || !HasSameIdentity(p.SenderIdentity, first.SenderIdentity)))
                return false;

            senderAccountId = first.SenderAccountId;
            senderIdentity  = CloneIdentity(first.SenderIdentity);
            return true;
        }

        private static bool HasSameIdentity(NetworkIdentity left, NetworkIdentity right)
        {
            if (left == null || right == null)
                return left == right;

            if (left.Id != right.Id)
                return false;

            return left.RealmId == right.RealmId || left.RealmId == 0u || right.RealmId == 0u;
        }

        private static NetworkIdentity GetPlayerIdentity(IPlayer player)
        {
            if (player == null)
                return new NetworkIdentity();

            return new NetworkIdentity
            {
                RealmId = player.Identity.RealmId,
                Id      = player.Identity.Id
            };
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

        private bool TryBuildImmediateAccountGrantPlan(IAccountInventoryItem item, out List<IAccountItemGrant> grants, out GenericError error)
        {
            grants = [];
            error  = GenericError.Ok;

            if (item.ClaimState != AccountItemClaimState.CanClaim)
            {
                error = GenericError.Params;
                return false;
            }

            AccountItemEntry entry = item.Entry;
            if (!IsImmediateAccountGrant(entry))
            {
                error = GenericError.Params;
                return false;
            }

            if (HasAccountItemFlag(entry, AccountItemFlag.MultiClaim))
            {
                error = GenericError.Params;
                return false;
            }

            if (entry.AccountCurrencyEnum != 0u && !TryAddAccountCurrencyGrant(entry, grants, out error))
                return false;

            if (entry.EntitlementId != 0u && !TryAddImmediateAccountEntitlementGrant(account, entry, grants, out error))
                return false;

            if (grants.Count == 0)
            {
                error = GenericError.ItemBadStaticData;
                return false;
            }

            return true;
        }

        private static bool IsImmediateAccountGrant(AccountItemEntry entry)
        {
            if (entry == null)
                return false;

            if (entry.Item2Id != 0u || entry.GenericUnlockSetId != 0u || entry.InstantEventEnum != 0u || entry.AccountItemCooldownGroupId != 0u)
                return false;

            return entry.AccountCurrencyEnum != 0u || entry.EntitlementId != 0u;
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

            EntitlementEntry entitlementEntry = GameTableManager.Instance.Entitlement?.GetEntry(entry.EntitlementId);
            if (entitlementEntry == null)
            {
                ReportMissingAccountItemStaticData(
                    GameTableManager.Instance.Entitlement == null,
                    EntitlementTableName,
                    entry.EntitlementId,
                    nameof(TryAddEntitlementGrant),
                    $"accountItemId={entry.Id}");
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

        private static bool TryAddImmediateAccountEntitlementGrant(IAccount account, AccountItemEntry entry, List<IAccountItemGrant> grants, out GenericError error)
        {
            error = GenericError.Ok;

            EntitlementEntry entitlementEntry = GameTableManager.Instance.Entitlement?.GetEntry(entry.EntitlementId);
            if (entitlementEntry == null)
            {
                ReportMissingAccountItemStaticData(
                    GameTableManager.Instance.Entitlement == null,
                    EntitlementTableName,
                    entry.EntitlementId,
                    nameof(TryAddImmediateAccountEntitlementGrant),
                    $"accountItemId={entry.Id}");
                error = GenericError.ItemBadStaticData;
                return false;
            }

            var entitlementFlags = (EntitlementFlags)entitlementEntry.Flags;
            if (entitlementFlags.HasFlag(EntitlementFlags.Disabled) || entitlementFlags.HasFlag(EntitlementFlags.Character))
            {
                error = GenericError.MissingEntitlement;
                return false;
            }

            if (entry.EntitlementCount == 0u || entry.EntitlementCount > int.MaxValue)
            {
                error = GenericError.ItemBadStaticData;
                return false;
            }

            var entitlementType = (EntitlementType)entitlementEntry.Id;
            if (!Enum.IsDefined(typeof(EntitlementType), entitlementType))
            {
                error = GenericError.ItemBadStaticData;
                return false;
            }

            uint currentAmount = account.EntitlementManager.GetEntitlement(entitlementType)?.Amount ?? 0u;
            if (currentAmount + (ulong)entry.EntitlementCount > entitlementEntry.MaxCount)
            {
                error = GenericError.AccountItemMaxEntitlementCount;
                return false;
            }

            grants.Add(new EntitlementGrant(entitlementType, (int)entry.EntitlementCount, false));
            return true;
        }

        private static bool TryAddGenericUnlockGrants(IPlayer player, uint genericUnlockSetId, List<IAccountItemGrant> grants, out GenericError error)
        {
            error = GenericError.Ok;

            GenericUnlockSetEntry unlockSetEntry = GameTableManager.Instance.GenericUnlockSet?.GetEntry(genericUnlockSetId);
            if (unlockSetEntry == null)
            {
                ReportMissingAccountItemStaticData(
                    GameTableManager.Instance.GenericUnlockSet == null,
                    GenericUnlockSetTableName,
                    genericUnlockSetId,
                    nameof(TryAddGenericUnlockGrants),
                    "Cannot resolve account item generic unlock set.");
                error = GenericError.InvalidGenericUnlock;
                return false;
            }

            foreach (uint genericUnlockEntryId in GetGenericUnlockEntryIds(unlockSetEntry))
            {
                if (genericUnlockEntryId == 0u)
                    continue;

                GenericUnlockEntryEntry genericUnlockEntry = GameTableManager.Instance.GenericUnlockEntry?.GetEntry(genericUnlockEntryId);
                if (genericUnlockEntry == null || genericUnlockEntry.Id > ushort.MaxValue)
                {
                    if (genericUnlockEntry == null)
                        ReportMissingAccountItemStaticData(
                            GameTableManager.Instance.GenericUnlockEntry == null,
                            GenericUnlockEntryTableName,
                            genericUnlockEntryId,
                            nameof(TryAddGenericUnlockGrants),
                            $"genericUnlockSetId={genericUnlockSetId}");
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

        private static void ReportMissingAccountItemStaticData(bool tableMissing, string tableName, uint staticId, string context, string detail)
        {
            context = nameof(AccountInventoryManager) + "." + context;
            if (tableMissing)
            {
                MissingGameDataDiagnostics.ReportMissingTable(
                    tableName,
                    context,
                    MissingGameDataSeverity.PlayerImpacting,
                    detail);
                return;
            }

            MissingGameDataDiagnostics.ReportMissingRow(
                tableName,
                staticId,
                context,
                MissingGameDataSeverity.PlayerImpacting,
                detail);
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
                if (player != null)
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
            public uint SenderAccountId { get; init; }
            public ulong TargetAccountId { get; init; }
            public NetworkIdentity SenderIdentity { get; set; }
            public NetworkIdentity TargetIdentity { get; set; }
            public AccountItemClaimState ClaimState { get; init; }
            public bool HasTargetPlayerIdentity { get; init; }

            public ServerAccountItemsPending.PendingAccountItemGroup Build()
            {
                return new ServerAccountItemsPending.PendingAccountItemGroup
                {
                    Id                      = Id,
                    AccountItemId           = AccountItemId,
                    Group                   = Group,
                    SenderAccountId         = SenderAccountId,
                    TargetAccountId         = TargetAccountId,
                    SenderIdentity          = CloneIdentity(SenderIdentity),
                    ClaimState              = ClaimState,
                    HasTargetPlayerIdentity = HasTargetPlayerIdentity ? 1ul : 0ul,
                    TargetIdentity          = CloneIdentity(TargetIdentity)
                };
            }
        }
    }
}
