using NexusForever.Game.Abstract.Account;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Map;
using NexusForever.Game.Static.Entity;
using NexusForever.Network.Message;
using NexusForever.Network.Session;
using NexusForever.Network.World.Message.Model;
using NexusForever.Network.World.Message.Static;

namespace NexusForever.Game.Entity
{
    public partial class Inventory
    {
        private LootDeliveryPublication lootPreparation;
        private Queue<LootDeliveryPublication> lootPublications;
        private bool publishingLoot;
        private List<Action> lootQuestNotices;

        internal bool RetainPhysicalLootQuestNotice(Action notice)
        {
            if (lootQuestNotices == null) return false;
            lootQuestNotices.Add(notice);
            return true;
        }

        // This is an ordinary physical mutation, not a durable loot receipt. Apply
        // its complete inventory plan before acknowledging or invoking gameplay/UI.
        internal bool TryDeliverSoulboundLoot(IPlayer recipient, IItemInfo info, uint count, Action accepted,
            Func<IWritable> grant, out GenericError? error)
        {
            error = null;
            // Map admission owns the outer gate; never invert it with Inventory.
            object admissionGate = (recipient as IPlayerMapAdmission)?.MapAdmissionSyncRoot ?? MutationGate;
            if (Monitor.IsEntered(MutationGate) && !Monitor.IsEntered(admissionGate)) return false;
            lock (admissionGate)
            lock (MutationGate)
            {
                if (!ReferenceEquals(player, recipient) || !ReferenceEquals(recipient.Inventory, this)
                    || publishingLoot || lootPreparation != null || inventoryMutationDepth != 0 || !TryAcquireMutationLease(out var lease)) return false;
                using (lease)
                {
                    var batch = new LootDeliveryPublication(this, recipient, grant);
                    if (!batch.Current) return false;
                    InventoryOperationSnapshot before = CreateOperationSnapshot();
                    bool acknowledged = false;
                    lootPreparation = batch;
                    try
                    {
                        if (!TryCreateSoulboundItem(InventoryLocation.Inventory, info, count, out var refusal, ItemUpdateReason.Loot))
                        { error = refusal; return false; }
                        if (!batch.Current) throw new InvalidOperationException("Physical loot lost its accepting inventory actor.");
                        lootPublications ??= new();
                        accepted();
                        acknowledged = true;
                        lootPublications.Enqueue(batch);
                        batch.ApplyGameplay();
                    }
                    catch
                    {
                        if (!acknowledged) RestoreOperationSnapshot(before);
                        throw;
                    }
                    finally { lootPreparation = null; }
                    PublishLootNotifications(leaseHeld: true);
                    return true;
                }
            }
        }

        private void PublishLootNotifications(bool leaseHeld = false)
        {
            lock (MutationGate)
                if (publishingLoot || lootPublications == null || lootPublications.Count == 0) return;
            object admissionGate = (player as IPlayerMapAdmission)?.MapAdmissionSyncRoot ?? MutationGate;
            if (Monitor.IsEntered(MutationGate) && !Monitor.IsEntered(admissionGate)) return;
            lock (admissionGate)
            lock (MutationGate)
            {
                IDisposable lease = null;
                if (!leaseHeld && !TryAcquireMutationLease(out lease)) return;
                using (lease) PublishOwnedLootNotifications();
            }
        }

        private void PublishOwnedLootNotifications()
        {
            if (publishingLoot || lootPublications == null) return;
            publishingLoot = true;
            try
            {
                while (lootPublications.TryPeek(out var batch))
                {
                    if (!batch.OwnsGameplay) { lootPublications.Dequeue(); continue; }
                    try
                    {
                        batch.ApplyGameplay();
                        if (batch.Current) batch.Publish();
                    }
                    catch (Exception error)
                    {
                        log.Debug(error, "Physical loot feedback retained for its original inventory actor.");
                        return;
                    }
                    lootPublications.Dequeue();
                }
            }
            finally { publishingLoot = false; }
        }

        private sealed class LootDeliveryPublication
        {
            private readonly Inventory owner;
            private readonly IPlayer actor;
            private readonly IGameSession session;
            private readonly object events, accountSession, map;
            private readonly IAccount account;
            private readonly IPlayerMapAdmission admission;
            private readonly object quests;
            private readonly Func<IWritable> grant;
            private readonly uint accountId;
            private readonly uint guid;
            private readonly ulong epoch, character;
            private readonly ulong generation;
            private readonly ushort? realm;
            private readonly List<Action> steps = [];
            private readonly List<Action> gameplay = [], questNotices = [];
            private int gameplayCursor;
            private bool gameplayComplete;
            private int cursor;

            internal LootDeliveryPublication(Inventory owner, IPlayer actor, Func<IWritable> grant)
            {
                this.owner = owner; this.actor = actor; this.grant = grant; quests = actor.QuestManager;
                character = actor.CharacterId; realm = actor.Identity?.RealmId;
                guid = actor.Guid; admission = actor as IPlayerMapAdmission; generation = admission?.MapAdmissionGeneration ?? 0;
                session = actor.Session; events = session?.Events; map = actor.Map;
                account = actor.Account; accountId = account?.Id ?? 0; epoch = account?.WorldSessionEpoch ?? 0;
                accountSession = account?.Session;
            }
            internal bool OwnsGameplay => session != null && ReferenceEquals(actor.Session, session)
                && ReferenceEquals(actor.Inventory, owner) && actor.CharacterId == character && character == owner.characterId
                && ReferenceEquals(owner.player, actor) && ReferenceEquals(actor.QuestManager, quests)
                && actor.Identity?.RealmId == realm
                && ReferenceEquals(session.Events, events) && ReferenceEquals(actor.Account, account)
                && (account == null || account.Id == accountId && account.WorldSessionEpoch == epoch && ReferenceEquals(account.Session, accountSession))
                && !owner.rewardOwnerInvalidated && !owner.RequiresAuthoritativeReload && owner.CanPublishToInventorySession(session);
            internal bool Current => OwnsGameplay && actor.Guid == guid && ReferenceEquals(actor.Map, map)
                && (admission == null || admission.IsMapAdmissionCurrent(generation));

            internal void Add(Func<IWritable> build) => steps.Add(() =>
            {
                IWritable message = build();
                if (message != null) session.EnqueueMessageEncrypted(message);
            });
            internal void Add(Action effect) => gameplay.Add(effect);
            internal void ApplyGameplay()
            {
                // Finish the collect-objective transition without invoking its
                // packet channel halfway through progress/flags/scripts/state.
                if (gameplayComplete) return;
                if (owner.lootQuestNotices != null) throw new InvalidOperationException("Nested physical loot quest publication.");
                LootDeliveryPublication previous = owner.lootPreparation;
                owner.lootPreparation = this;
                owner.lootQuestNotices = questNotices;
                try
                {
                    while (gameplayCursor < gameplay.Count) { gameplay[gameplayCursor](); ++gameplayCursor; }
                    steps.AddRange(questNotices);
                    if (grant != null) Add(grant);
                    gameplayComplete = true;
                }
                finally { owner.lootQuestNotices = null; owner.lootPreparation = previous; }
            }
            internal void Add(IWritable message)
            {
                // A refused notice is rebuilt from the still-owned live item. Later
                // removal cannot turn a retained Add into a ghost item on retry.
                switch (message)
                {
                    case ServerItemAdd add:
                        ulong added = add.InventoryItem.Item.ItemGuid;
                        ItemUpdateReason addReason = add.InventoryItem.Reason;
                        Add(() => owner.GetItem(added) is { Location: not InventoryLocation.None } item
                            ? new ServerItemAdd { InventoryItem = new() { Item = ClonePathRewardItem(item.Build()), Reason = addReason } } : null);
                        break;
                    case ServerItemStackCountUpdate stack:
                        ulong stacked = stack.Guid; ItemUpdateReason stackReason = stack.Reason;
                        Add(() => owner.GetItem(stacked) is { Location: not InventoryLocation.None } item
                            ? new ServerItemStackCountUpdate { Guid = stacked, StackCount = item.StackCount, Reason = stackReason } : null);
                        break;
                    default:
                        throw new InvalidOperationException("Unexpected feedback in physical soulbound creation.");
                }
            }
            internal void Publish()
            {
                while (cursor < steps.Count && Current)
                {
                    steps[cursor]();
                    ++cursor;
                }
            }
        }
    }
}
