using NexusForever.Game.Abstract.Entity;
using NexusForever.Network.Session;

namespace NexusForever.Game.Entity
{
    public partial class Player
    {
        public bool TrySaveCrafting(GameSession session, Func<bool> craft)
        {
            var inventory = Inventory;
            var satchel = SupplySatchelManager;
            var quests = QuestManager as QuestManager;
            var account = Account;
            var wallet = CurrencyManager;
            var achievements = AchievementManager;
            var accountWallet = account?.CurrencyManager;
            var accountItems = account?.InventoryManager;
            var events = session.Events;
            ulong? epoch = account?.WorldSessionEpoch;
            bool Owns() => ReferenceEquals(Session, session) && ReferenceEquals(session.Events, events)
                && session is IPlayerSession bound && ReferenceEquals(bound.Player, this)
                && ReferenceEquals(Inventory, inventory) && ReferenceEquals(SupplySatchelManager, satchel)
                && ReferenceEquals(QuestManager, quests) && ReferenceEquals(CurrencyManager, wallet)
                && ReferenceEquals(AchievementManager, achievements)
                && ReferenceEquals(Account, account) && ReferenceEquals(account?.Session, session)
                && account?.WorldSessionEpoch == epoch && ReferenceEquals(account?.CurrencyManager, accountWallet)
                && ReferenceEquals(account?.InventoryManager, accountItems) && !session.IsDisconnecting;
            if (!Owns() || quests == null || RewardStateRequiresReload || inventory.IsSaveBlocked
                || !quests.CanStartTutorialQuestCompletion(this)) return false;

            bool crafted = false;
            quests.SaveOrdinaryQuestMutation(this, session, () =>
            {
                if (!Owns() || !InventoryMutationScope.TryAcquire(inventory, out var lease))
                    throw new InvalidOperationException("Crafting lost its original inventory before preparation.");
                using (lease)
                {
                    crafted = craft();
                    if (!Owns()) throw new InvalidOperationException("Crafting lost its original actor during preparation.");
                }
                return crafted;
            }, afterCommit: null, ownsMutation: Owns);
            return crafted;
        }
    }
}
