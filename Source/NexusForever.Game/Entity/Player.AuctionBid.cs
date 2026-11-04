using NexusForever.Game.Abstract.Account;
using NexusForever.Database.Character;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Quest;
using NexusForever.Game.Challenges;
using NexusForever.Game.Quest;
using NexusForever.Game.Marketplace;
using NexusForever.Game.Mail;
using NexusForever.Game.Static.Entity;
using NexusForever.Network.Session;
using NexusForever.Network.World.Message.Model;

namespace NexusForever.Game.Entity
{
    public partial class Player
    {
        private AuctionBidActor auctionBidActor;
        private bool auctionBidOrdinarySave;
        private bool AuctionBidSaveAllowed => auctionBidOrdinarySave && auctionBidActor?.CanSave == true;

        // Must be invoked on this original actor's session queue. Preflight uses
        // the existing Auth -> complete atomic Character save, including reward
        // receipts; persisting only a dirty currency would split that graph.
        internal void PrepareAuctionBid(object operation, Action<AuctionBidActor> ready, Action failed)
        {
            ArgumentNullException.ThrowIfNull(operation);
            if (auctionBidActor != null || housingNameActor != null || CurrencyManager is not CurrencyManager currencies
                || Session is NetworkSession { IsDisconnecting: true } || LogoutManager?.State is LogoutState.Logout or LogoutState.Finished)
            { failed(); return; }
            AuctionBidActor actor;
            try { actor = new(this, currencies, operation); }
            catch { failed(); return; }
            auctionBidActor = actor; Session.CanProcessIncomingPackets = false;
            try
            {
                Save(() =>
                {
                    if (!actor.Owns() || actor.Retired) { actor.End(); failed(); return; }
                    Session.CanProcessIncomingPackets = false;
                    ready(actor);
                }, () => { actor.End(); failed(); });
            }
            catch { actor.End(); throw; }
        }

        internal sealed class AuctionBidActor
        {
            private readonly Player player;
            private readonly CurrencyManager currencies;
            private readonly object operation;
            private readonly IAccount account;
            private readonly IGameSession session, accountSession;
            private readonly IInventory inventory;
            private readonly IMailManager mailbox;
            private readonly IQuestManager quests;
            private readonly ChallengeManager challenges;
            private IDisposable frozen, inventoryLease, publication;
            private object frozenToken;
            private CurrencyManager.AuctionBidCurrency wallet;
            private Inventory.AuctionDelivery delivery;
            private Inventory.AuctionEscrow escrow;
            private GlobalMarketplaceManager.AuctionRow escrowListing;
            private Inventory.CommodityDelivery commodityDelivery;
            internal Inventory.CommodityDelivery CommodityDelivery => commodityDelivery;
            private bool ended, reconciling;
            private Func<bool> publicationGuard;
            private string bidId, bidPayload;
            private bool bidCommitted;
            internal bool CanSave => reconciling && Owns() && wallet?.CanSave == true;
            internal bool Retired { get; set; }
            internal bool CleanupRequested { get; set; }
            internal bool DisposeRequested { get; set; }
            internal ulong Character { get; }
            internal uint Account { get; }
            internal string Database { get; }
            internal CurrencyManager.AuctionBidCurrency Wallet => wallet;
            internal GlobalMarketplaceManager.BidItem DeliveryTarget => delivery?.Target;
            internal bool DeliveryByMail => delivery?.ByMail == true;
            internal GlobalMarketplaceManager.BidItem PostingItem => escrow?.Before;

            internal AuctionBidActor(Player player, CurrencyManager currencies, object operation)
            {
                this.player = player; this.currencies = currencies; this.operation = operation;
                account = player.Account; session = player.Session; accountSession = account?.Session;
                inventory = player.Inventory; mailbox = player.MailManager; quests = player.QuestManager; challenges = player.ChallengeManager as ChallengeManager;
                Character = player.CharacterId; Account = account?.Id ?? 0;
                if (Character == 0 || Account == 0 || session == null || !ReferenceEquals(accountSession, session)
                    || inventory?.SupportsTransactionalSoulboundDelivery != true || quests is not NexusForever.Game.Entity.QuestManager || challenges == null)
                    throw new InvalidOperationException("Normal bid requires its complete original actor.");
                Database = currencies.GetAuctionBidDatabase(player);
                if (challenges.NormalSaveIdentity?.ExecutionDatabase != Database)
                    throw new InvalidOperationException("Normal bid requires its prepared reward persistence identity.");
            }

            internal bool Owns() => !ended && ReferenceEquals(player.auctionBidActor, this)
                && ReferenceEquals(player.Account, account) && ReferenceEquals(player.Session, session)
                && account.Id == Account && ReferenceEquals(account.Session, accountSession)
                && player.CharacterId == Character && ReferenceEquals(player.CurrencyManager, currencies)
                && ReferenceEquals(player.Inventory, inventory) && ReferenceEquals(player.QuestManager, quests)
                && ReferenceEquals(player.MailManager, mailbox)
                && ReferenceEquals(player.ChallengeManager, challenges);

            internal void Capture(ulong amount, bool subtract, bool silentSettlement = false)
            {
                if (!Owns() || Retired || wallet != null || player.IsNormalSaveInProgress)
                    throw new InvalidOperationException("Normal bid actor is not ready for capture.");
                frozenToken = new object();
                frozen = QuestSaveScope.AcquireFrozen(operation, frozenToken, Database, Character);
                try
                {
                    if (!inventory.TryAcquireSaveLease(out inventoryLease)) throw new InvalidOperationException("Normal bid lost its inventory exclusion.");
                    wallet = currencies.CaptureAuctionBid(player, amount, subtract, Owns, silentSettlement);
                }
                catch { ReleaseScopes(); throw; }
            }

            internal MarketplaceBidReconciliation CaptureReconciliationReceipt()
            {
                if (!CanSave) throw new InvalidOperationException("Normal bid has no ordinary reconciliation capture.");
                return new(System.Guid.NewGuid().ToString("N"), bidId, bidPayload, bidCommitted);
            }

            internal void CaptureDelivery(GlobalMarketplaceManager.BidItem escrow)
            {
                if (!Owns() || wallet == null || inventoryLease == null || delivery != null || inventory is not Inventory concrete)
                    throw new InvalidOperationException("Buyout requires its original captured inventory.");
                delivery = concrete.ReserveAuctionDelivery(escrow, Owns);
                if (delivery.ByMail && mailbox is not NexusForever.Game.Entity.MailManager)
                    throw new InvalidOperationException("Buyout mail requires the original bound mailbox.");
            }

            internal void CaptureAuctionPosting(ulong guid, Func<MailHeaderState> recoveryHeader)
            {
                if (!Owns() || wallet == null || inventoryLease == null || delivery != null || commodityDelivery != null
                    || escrow != null || inventory is not Inventory concrete)
                    throw new InvalidOperationException("Auction posting requires its original captured inventory.");
                escrow = concrete.ReserveAuctionPosting(guid, Database, Owns, recoveryHeader);
                commodityDelivery = escrow.Deferral;
            }

            internal void CaptureCommodityDelivery(uint definition, uint count, Func<MailHeaderState> recoveryHeader)
            {
                if (!Owns() || wallet == null || inventoryLease == null || commodityDelivery != null || delivery != null || inventory is not Inventory concrete)
                    throw new InvalidOperationException("Commodity delivery requires its original excluded inventory.");
                commodityDelivery = concrete.ReserveCommodityReturn(definition, count, Database, Owns, recoveryHeader);
                if (commodityDelivery.ByMail && mailbox is not NexusForever.Game.Entity.MailManager)
                    throw new InvalidOperationException("Commodity mail requires the original bound mailbox.");
            }

            internal void CaptureCommodityPosting(uint definition, uint consume, IReadOnlyList<GlobalMarketplaceManager.CommodityPostingDeliveryBatch> deliveries, Func<MailHeaderState> recoveryHeader)
            {
                if (!Owns() || wallet == null || inventoryLease == null || commodityDelivery != null || delivery != null || inventory is not Inventory concrete)
                    throw new InvalidOperationException("Commodity posting requires its original excluded inventory.");
                commodityDelivery = concrete.ReserveCommodityPosting(definition, consume, deliveries, Database, Owns, recoveryHeader);
                if (commodityDelivery.ByMail && mailbox is not NexusForever.Game.Entity.MailManager)
                    throw new InvalidOperationException("Commodity posting mail requires its original bound mailbox.");
            }

            internal void PublishDeliveryMail(MailItem mail)
            {
                if (Retired || (delivery?.RequiresReload == true || commodityDelivery?.RequiresReload == true) || !ReferenceEquals(player.MailManager, mailbox)
                    || !ReferenceEquals(player.Session, session) || !ReferenceEquals(player.Account, account)
                    || !ReferenceEquals(account.Session, accountSession) || mail.RecipientId != Character) return;
                if (mailbox is NexusForever.Game.Entity.MailManager original) original.EnqueueMarketplaceMail(mail, Account, Database, publicationGuard);
            }

            internal NexusForever.Game.Entity.QuestManager.NormalSaveSnapshot CaptureQuestReconciliation(NexusForever.Game.Entity.QuestManager manager, string database)
            {
                if (!CanSave || frozen == null || database != Database) throw new InvalidOperationException("Normal bid lost its Character reconciliation freeze.");
                return manager.CaptureNormalSave(database, operation, frozenToken);
            }

            internal void Reconcile(bool committed, string parentBidId, string parentPayloadSHA256, Action completed, Action failed,
                GlobalMarketplaceManager.BidWalletCurrent current = null,
                GlobalMarketplaceManager.BidItem deliveredCurrent = null, bool deliveryMatches = true, Func<bool> publicationAllowed = null,
                MailHeaderState deliveryMail = null, GlobalMarketplaceManager.CommodityReturnView commodityCurrent = null,
                GlobalMarketplaceManager.AuctionRow postingListing = null, Action publishPostingRemoval = null,
                Action<Func<ServerItemAdd>> publishAuctionDelivery = null)
            {
                if (!Owns() || wallet == null || reconciling) throw new InvalidOperationException("Normal bid has no reconcilable original actor.");
                reconciling = true;
                publicationGuard = publicationAllowed;
                bidId = parentBidId; bidPayload = parentPayloadSHA256; bidCommitted = committed;
                escrowListing = postingListing;
                try
                {
                    wallet.PrepareOrdinaryReconciliation(committed, current);
                    escrow?.Settle(committed, deliveredCurrent ?? (committed ? escrow.Target : escrow.Before), deliveryMatches);
                    delivery?.Settle(committed, deliveredCurrent, deliveryMatches, deliveryMail);
                    commodityDelivery?.Settle(committed, commodityCurrent?.Items ?? (escrow == null ? null : []), commodityCurrent?.Material,
                        (commodityCurrent?.DeliveryMatches ?? true) && escrow?.RequiresReload != true, deliveryMail,
                        commodityCurrent?.ItemMails);
                    ReleaseInventory();
                    player.auctionBidOrdinarySave = true;
                    SaveNext();
                }
                catch { Quarantine(); throw; }

                void SaveNext() => player.Save(() =>
                    {
                        try
                        {
                            bool currencyPending = wallet.PrepareNextOrdinaryReconciliation();
                            bool itemsPending = commodityDelivery?.PrepareNextReconciliation() == true;
                            if (currencyPending || itemsPending)
                            { session.CanProcessIncomingPackets = false; SaveNext(); return; }
                            bool CanPublish() => publicationAllowed?.Invoke() != false && !Retired && delivery?.RequiresReload != true && commodityDelivery?.RequiresReload != true
                                && escrow?.RequiresReload != true
                                && session is not NetworkSession { IsDisconnecting: true }
                                && player.LogoutManager?.State is not (LogoutState.Logout or LogoutState.Finished);
                            publication = (inventory as Inventory)?.BeginMarketplacePublication(Owns);
                            wallet.CompleteOrdinaryReconciliation(CanPublish(), CanPublish);
                            escrow?.Complete(CanPublish, publishPostingRemoval);
                            delivery?.Complete(CanPublish(), publishAuctionDelivery);
                            commodityDelivery?.Complete(CanPublish);
                            bool conflict = delivery?.RequiresReload == true || commodityDelivery?.RequiresReload == true || escrow?.RequiresReload == true;
                            End(); if (conflict) failed(); else completed();
                        }
                        catch { Quarantine(); failed(); }
                    }, () => { Quarantine(); failed(); });
            }

            internal void CancelPreparation()
            {
                if (wallet != null) throw new InvalidOperationException("Captured normal bid currency must reconcile its retained later intent.");
                ReleaseScopes(); End();
            }

            private void ReleaseInventory() { inventoryLease?.Dispose(); inventoryLease = null; }
            internal Inventory.AuctionDeliveryWitness CaptureDeliveryWitness() => delivery?.CaptureWitness(Database);
            internal Inventory.AuctionEscrowWitness CaptureEscrowWitness() => escrow?.CaptureWitness(Database, escrowListing);
            internal void RejectEscrowWitness() => escrow.RejectCustody();
            internal Inventory.CommodityDeliveryWitness CaptureCommodityWitness() => commodityDelivery?.CaptureWitness();
            internal void ConfirmCommodityWitness(Inventory.CommodityDeliveryWitness witness) => commodityDelivery.ConfirmWitness(witness);
            internal Inventory.CommodityDeliveryWitness RebaseCommodityMaterial(Inventory.CommodityDeliveryWitness witness, ushort? current)
                => commodityDelivery.RebaseMaterial(witness, current);
            internal IReadOnlyDictionary<ulong, uint> PreviewCommodityRecovery() => commodityDelivery.PreviewRecoveryQuantities();
            internal void ObserveCommodityRollback(NexusForever.Database.Character.BidReconciliationRolledBackException proof)
                => commodityDelivery?.ObserveRollback(proof);
            internal Inventory.CommodityDeliveryWitness RenewCommodityWitness() => commodityDelivery.CaptureWitness();
            internal void RejectCommodityWitness()
            {
                if (!CanSave) throw new InvalidOperationException("Commodity custody lost its original reconciliation owner.");
                commodityDelivery.RejectCustody();
            }
            internal void RejectDeliveryWitness()
            {
                if (!CanSave) throw new InvalidOperationException("Delivery conflict lost its original reconciliation owner.");
                delivery.RejectCustody();
            }
            private void ReleaseScopes() { delivery?.Dispose(); escrow?.Dispose(); commodityDelivery?.Dispose(); ReleaseInventory(); frozen?.Dispose(); frozen = null; publication?.Dispose(); publication = null; }
            private void Quarantine() { wallet?.Quarantine(); ReleaseScopes(); End(); }
            internal void End()
            {
                if (ended) return;
                if (wallet != null && !reconciling) throw new InvalidOperationException("Captured bid currency cannot end before reconciliation.");
                ReleaseScopes();
                ended = true;
                if (!ReferenceEquals(player.auctionBidActor, this)) throw new InvalidOperationException("Normal bid cannot retire another actor ticket.");
                player.auctionBidActor = null;
                player.auctionBidOrdinarySave = false;
                if (CleanupRequested) player.CleanupCore();
                else if (DisposeRequested) player.Dispose();
                else if (!Retired && !player.RewardStateRequiresReload && delivery?.RequiresReload != true && commodityDelivery?.RequiresReload != true
                    && escrow?.RequiresReload != true && publicationGuard?.Invoke() != false)
                    session.CanProcessIncomingPackets = true;
            }
        }
    }
}
