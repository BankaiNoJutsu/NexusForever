using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Account.Currency;
using NexusForever.Game.Account.Inventory;
using NexusForever.Game.Account.Reward;
using NexusForever.Game.Crafting;
using NexusForever.Game.Static.Account;
using NexusForever.Game.Static.Loot;
using NexusForever.Network.Session;
using NexusForever.Shared.Game.Events;

namespace NexusForever.Game.Entity
{
    public partial class Player
    {
        private List<CraftingAccountAward> craftingAccountAwards;
        private CraftingAccountIntent craftingAccountIntent;
        private bool craftingAccountDeliveryPending;

        internal bool RetainCraftingAccountAward(LootItemType type, uint id, uint amount)
        {
            if (craftingAccountAwards == null) return false;
            if (type == LootItemType.AccountCurrency)
            {
                ulong balance = Account.CurrencyManager.GetCurrencyAmount((AccountCurrencyType)id);
                foreach (var prior in craftingAccountAwards.Where(award => award.Type == type && award.Id == id)) balance = checked(balance + prior.Amount);
                _ = checked(balance + amount);
            }
            if (amount != 0) craftingAccountAwards.Add(new(type, id, amount));
            return true;
        }

        public bool TrySaveCrafting(GameSession session, Func<bool> craft)
            => TrySaveAccountAwardMutation(session, craft, null);

        // The existing paid-source/account-award protocol carries a generic
        // bounded award bundle. Its historical table/receipt names stay stable.
        internal bool TrySaveQuestRewards(GameSession session, Func<bool> reward, Action afterCommit)
        {
            if (ChallengeManager is not Challenges.ChallengeManager { NormalSaveIdentity: not null }) return false;
            return TrySaveAccountAwardMutation(session, reward, afterCommit);
        }

        private bool TrySaveAccountAwardMutation(GameSession session, Func<bool> craft, Action afterCommit)
        {
            var inventory = Inventory;
            var satchel = SupplySatchelManager;
            var quests = QuestManager as QuestManager;
            var account = Account;
            uint accountId = account?.Id ?? 0;
            ulong characterId = CharacterId;
            ushort realmId = Identity.RealmId;
            var wallet = CurrencyManager;
            var achievements = AchievementManager;
            var accountWallet = account?.CurrencyManager;
            var accountItems = account?.InventoryManager;
            var accountLoyalty = account?.LoyaltyManager;
            var events = session.Events;
            ulong? epoch = account?.WorldSessionEpoch;
            bool Owns() => ReferenceEquals(Session, session) && ReferenceEquals(session.Events, events)
                && CharacterId == characterId && Identity.RealmId == realmId && account?.Id == accountId
                && session is IPlayerSession bound && ReferenceEquals(bound.Player, this)
                && ReferenceEquals(Inventory, inventory) && ReferenceEquals(SupplySatchelManager, satchel)
                && ReferenceEquals(QuestManager, quests) && ReferenceEquals(CurrencyManager, wallet)
                && ReferenceEquals(AchievementManager, achievements)
                && ReferenceEquals(Account, account) && ReferenceEquals(account?.Session, session)
                && account?.WorldSessionEpoch == epoch && ReferenceEquals(account?.CurrencyManager, accountWallet)
                && ReferenceEquals(account?.LoyaltyManager, accountLoyalty)
                && ReferenceEquals(account?.InventoryManager, accountItems) && !session.IsDisconnecting;
            if (!Owns() || quests == null || RewardStateRequiresReload || inventory.IsSaveBlocked
                || craftingAccountAwards != null || craftingAccountIntent != null || craftingAccountDeliveryPending
                || !quests.CanStartTutorialQuestCompletion(this)) return false;

            bool crafted = false;
            CraftingAccountIntent awards = null;
            LoyaltyCatalog loyalty = null;
            quests.SaveOrdinaryQuestMutation(this, session, () =>
            {
                if (!Owns() || !InventoryMutationScope.TryAcquire(inventory, out var lease))
                    throw new InvalidOperationException("Crafting lost its original inventory before preparation.");
                using (lease)
                {
                    craftingAccountAwards = [];
                    try
                    {
                        crafted = craft();
                        if (!Owns()) throw new InvalidOperationException("Crafting lost its original actor during preparation.");
                        if (craftingAccountAwards.Count != 0)
                        {
                            if (!crafted || accountWallet is not AccountCurrencyManager || accountItems is not AccountInventoryManager)
                                throw new InvalidOperationException("Crafting awards require their complete durable account owners.");
                            uint[] prerequisites = [];
                            if (craftingAccountAwards.Any(value => value.Type == LootItemType.AccountCurrency && value.Id == (uint)AccountCurrencyType.CosmicReward)
                                && account.LoyaltyManager != null)
                            {
                                var context = ((AccountLoyaltyManager)account.LoyaltyManager).CaptureCraftingContext(this);
                                loyalty = context.Catalog; prerequisites = context.Prerequisites;
                            }
                            using var character = GetCharacterDatabase().CreateContext();
                            using var auth = GetAuthDatabase().CreateContext();
                            awards = new(1, System.Guid.NewGuid().ToString("N"), accountId, characterId, realmId,
                                Item.GetRewardDatabaseKey(character), AccountCurrencyOperationRepository.DatabaseKey(auth),
                                craftingAccountAwards.ToArray(), 0, epoch ?? 0, loyalty == null ? null : CraftingAccountIntent.Fingerprint(loyalty), prerequisites);
                            awards = awards.Reserve(GetAuthDatabase());
                            if (!Owns()) throw new InvalidOperationException("Crafting lost its actor while reserving account awards.");
                            craftingAccountIntent = awards;
                        }
                    }
                    finally { craftingAccountAwards = null; }
                }
                return crafted;
            }, afterCommit: afterCommit, ownsMutation: Owns, beforePublication: publish =>
            {
                if (awards == null) { publish(); return; }
                if (!Owns()) return;
                craftingAccountDeliveryPending = true;
                session.CanProcessIncomingPackets = false;
                var character = GetCharacterDatabase(); var auth = GetAuthDatabase();
                var delivery = new CraftingAccountDelivery(character.CreateContext, auth.CreateContext, awards.Realm, loyalty);
                var task = Task.Run(async () =>
                {
                    await delivery.DeliverAsync(awards);
                    return await delivery.ReadSnapshot(awards, epoch ?? 0);
                });
                void Fail()
                {
                    if (task.IsFaulted) log.Error(task.Exception, "Crafting account delivery remains pending for character {0}.", awards.Character);
                    craftingAccountDeliveryPending = false;
                    quests.RequireNormalSaveReload();
                    if (Owns()) session.ForceDisconnect();
                }
                void Complete(CraftingAccountSnapshot snapshot)
                {
                    if (!Owns()) { craftingAccountDeliveryPending = false; return; }
                    try
                    {
                        var wallets = (AccountCurrencyManager)accountWallet;
                        uint[] currencies = awards.Awards.Where(value => value.Type == LootItemType.AccountCurrency)
                            .Select(value => value.Id).Distinct().ToArray();
                        int next = 0;
                        void Refresh()
                        {
                            if (!Owns()) { craftingAccountDeliveryPending = false; return; }
                            if (next < currencies.Length)
                            {
                                var type = (AccountCurrencyType)currencies[next++];
                                if (type == AccountCurrencyType.CosmicReward && snapshot.Loyalty != null)
                                {
                                    ((AccountLoyaltyManager)account.LoyaltyManager).AdoptCraftingCredit(snapshot.Loyalty, Owns);
                                    Refresh(); return;
                                }
                                var refresh = wallets.BeginWalletRefresh(type, auth.CreateContext);
                                events.EnqueueEvent(new TaskEvent(refresh.Completion, () =>
                                {
                                    using (refresh)
                                    {
                                        if (!Owns() || refresh.Publish(Owns, allowRetry: false) != AccountCurrencyManager.WalletRefresh.PublicationOutcome.Published)
                                        { Fail(); return; }
                                    }
                                    try { Refresh(); }
                                    catch { Fail(); throw; }
                                }, () => { refresh.Dispose(); Fail(); }));
                                return;
                            }
                            ((AccountInventoryManager)accountItems).AdoptCraftingItems(snapshot.Items, session, Owns);
                            if (!Owns()) { craftingAccountDeliveryPending = false; return; }
                            craftingAccountDeliveryPending = false;
                            publish();
                            if (Owns()) session.CanProcessIncomingPackets = true;
                        }
                        Refresh();
                    }
                    catch { Fail(); throw; }
                }
                events.EnqueueEvent(new TaskGenericEvent<CraftingAccountSnapshot>(task, Complete, Fail));
            });
            return crafted;
        }
    }
}
