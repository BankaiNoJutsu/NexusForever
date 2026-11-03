using NexusForever.Database.Character;
using NexusForever.Game.Abstract.Account;
using NexusForever.Game.Abstract.Challenges;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Quest;
using NexusForever.Game.Challenges;
using NexusForever.Game.Quest;
using NexusForever.Game.RealmBank;
using NexusForever.Game.Static.Entity;
using NexusForever.Network.Session;

namespace NexusForever.Game.Entity
{
    public partial class Player
    {
        private BankActor bankActor;
        private int bankPublicationDepth;
        internal bool IsBankPublicationInProgress => Volatile.Read(ref bankPublicationDepth) != 0;
        internal bool BankSaveAllowed => bankActor?.CanSave == true;
        internal object BankCurrencyToken => bankActor?.Token;
        internal bool BankPublicationAllowed => bankActor?.CanPublish == true;
        internal Inventory BankInventory => bankActor?.Inventory;
        internal BankActor BeginBankReconciliation(object operation, Inventory inventory)
        {
            if (bankActor != null || auctionBidActor != null || housingNameActor != null || IsNormalSaveInProgress)
                throw new InvalidOperationException("Bank reconciliation requires an unreserved original actor.");
            var actor = new BankActor(this, operation, inventory); bankActor = actor;
            Session.CanProcessIncomingPackets = false; return actor;
        }

        internal sealed class BankActor
        {
            private readonly Player player;
            private readonly object operation;
            private readonly IAccount account;
            private readonly IGameSession session;
            private readonly CurrencyManager currencies;
            private readonly IQuestManager quests;
            private readonly IChallengeManager challenges;
            private readonly IMailManager mailbox;
            private readonly ulong character;
            private readonly uint accountId;
            private readonly string database;
            private readonly IDisposable frozen;
            private CurrencyManager.BankCurrencyReservation wallet;
            private RealmBankManager.BankOutcome outcome;
            private bool ended, reconciling;
            internal readonly object Token = new();
            internal Inventory Inventory { get; }
            internal bool Retired, CleanupRequested, DisposeRequested;
            internal bool CanSave => reconciling && Owns() && (wallet == null || wallet.CanSave);
            internal CurrencyManager.BankCurrencyReservation Wallet => wallet;
            internal bool CanPublish => Owns() && !Retired && CurrentSessionPlayer && session is not NetworkSession { IsDisconnecting: true }
                && player.LogoutManager?.State is not (LogoutState.Logout or LogoutState.Finished);

            private bool CurrentSessionPlayer => session is IPlayerSession current && ReferenceEquals(current.Player, player);

            internal BankActor(Player player, object operation, Inventory inventory)
            {
                this.player = player; this.operation = operation; Inventory = inventory;
                account = player.Account; session = player.Session; currencies = player.CurrencyManager as CurrencyManager;
                quests = player.QuestManager; challenges = player.ChallengeManager; mailbox = player.MailManager;
                character = player.CharacterId; accountId = account?.Id ?? 0;
                if (character == 0 || accountId == 0 || !ReferenceEquals(account?.Session, session) || session == null
                    || !ReferenceEquals(player.Inventory, inventory) || quests is not NexusForever.Game.Entity.QuestManager || challenges is not ChallengeManager manager || currencies == null)
                    throw new InvalidOperationException("Bank reconciliation requires its complete original actor.");
                database = currencies.GetAuctionBidDatabase(player);
                if (manager.NormalSaveIdentity?.ExecutionDatabase != database)
                    throw new InvalidOperationException("Bank reconciliation requires its prepared ordinary-save identity.");
                frozen = QuestSaveScope.AcquireFrozen(operation, Token, database, character);
            }
            internal bool Owns() => !ended && ReferenceEquals(player.bankActor, this) && ReferenceEquals(player.Account, account)
                && ReferenceEquals(player.Session, session) && ReferenceEquals(account.Session, session) && account.Id == accountId
                && player.CharacterId == character && ReferenceEquals(player.Inventory, Inventory) && ReferenceEquals(player.CurrencyManager, currencies)
                && ReferenceEquals(player.QuestManager, quests) && ReferenceEquals(player.ChallengeManager, challenges) && ReferenceEquals(player.MailManager, mailbox);
            internal QuestManager.NormalSaveSnapshot CaptureQuests(QuestManager manager, string route)
            {
                if (!CanSave || route != database) throw new InvalidOperationException("Bank ordinary save lost its retained freeze.");
                return manager.CaptureNormalSave(route, operation, Token);
            }
            internal BankSaveReconciliation CaptureReceipt()
            {
                if (!CanSave) throw new InvalidOperationException("Bank ordinary save lost its outcome witness.");
                return new(System.Guid.NewGuid().ToString("N"), outcome.Operation, outcome.Graph.Fingerprint, outcome.Committed, outcome.ValidateParent, outcome.Guard, outcome.OwnsMailItem);
            }
            internal void Reconcile(RealmBankManager.BankOutcome result, CurrencyManager.BankCurrencyReservation currency,
                Action prepare, Action<bool> complete, Action failed)
            {
                if (!Owns() || reconciling || result.Database != database || result.Account != accountId || result.Character != character)
                    throw new InvalidOperationException("Bank reconciliation changed its original actor or SQL outcome.");
                outcome = result; wallet = currency; reconciling = true;
                try { wallet?.PrepareOrdinary(outcome); prepare(); SaveNext(); }
                catch { Quarantine(); throw; }

                void SaveNext() => player.Save(() =>
                {
                    try
                    {
                        if (!CanSave) throw new InvalidOperationException("Bank ordinary save lost its original actor.");
                        bool deferred = wallet?.PrepareNextOrdinary() == true;
                        // A wallet-free bank operation, or another currency in a
                        // paid operation, can accept intent after the snapshot.
                        // Its row revisions must settle before bank publication.
                        if (deferred || currencies.HasPendingPersistenceSave) { SaveNext(); return; }
                        EnterPublication();
                        try { complete(CanPublish); End(); }
                        finally { LeavePublication(); }
                    }
                    catch { Quarantine(); failed(); }
                }, () => { Quarantine(); failed(); });
            }
            internal void EnterPublication() => Interlocked.Increment(ref player.bankPublicationDepth);
            internal void LeavePublication()
            {
                if (Interlocked.Decrement(ref player.bankPublicationDepth) < 0)
                    throw new InvalidOperationException("Bank publication lost its original mutation guard.");
            }
            internal void Quarantine()
            {
                Retired = true; wallet?.Quarantine(); Inventory.RequireRealmBankReload();
                if (!player.IsNormalSaveInProgress) End();
            }
            internal void End()
            {
                if (ended) return;
                frozen.Dispose(); ended = true;
                if (!ReferenceEquals(player.bankActor, this)) throw new InvalidOperationException("Bank cannot retire another actor's ticket.");
                player.bankActor = null;
                if (CleanupRequested) player.CleanupCore();
                else if (DisposeRequested) player.Dispose();
                else if (!Retired && CurrentSessionPlayer && !player.RewardStateRequiresReload) session.CanProcessIncomingPackets = true;
            }
        }
    }
}
