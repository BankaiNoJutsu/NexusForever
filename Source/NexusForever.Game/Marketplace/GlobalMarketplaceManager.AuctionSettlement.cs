using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using NexusForever.Database.Character;
using NexusForever.Game.Abstract.Account;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Entity;
using NexusForever.Game.Mail;
using NexusForever.Game.Static.Entity;
using NexusForever.Network.Session;
using NexusForever.Network.World.Message.Model.Marketplace;
using NexusForever.Network.World.Message.Static;
using NexusForever.Shared.Game.Events;

namespace NexusForever.Game.Marketplace
{
    public sealed partial class GlobalMarketplaceManager
    {
        private readonly Dictionary<ulong, AuctionSettlementOperation> auctionSettlements = [];
        private readonly EventQueue settlementEvents = new();
        private readonly Func<AuctionSettlementCommand, Task<BidOutcome>> auctionSettlementExecution;

        // Persisted auction purchases use the original actors and one owned SQL
        // operation through wallet, custody and ordinary-save reconciliation.
        public bool TryQueueAuctionPurchase(IPlayer player, ulong auctionId, uint definition, ulong amount,
            Action<GenericError, AuctionInfo> reply, Func<bool> publicationAllowed = null)
        {
            if (player is not Player buyer || TryGetCharacterDatabase() == null) return false;
            QueueAuctionPurchase(buyer, auctionId, definition, amount, reply, publicationAllowed);
            return true;
        }

        internal void QueueAuctionPurchase(Player buyer, ulong auctionId, uint definition, ulong amount,
            Action<GenericError, AuctionInfo> reply, Func<bool> publicationAllowed = null)
        {
            AuctionSettlementOperation operation;
            lock (syncRoot)
            {
                var record = auctions.SingleOrDefault(row => row.Auction.AuctionId == auctionId);
                if (record == null || record.Auction.Item2Id != definition)
                { reply(GenericError.ItemBadId, new() { AuctionId = auctionId, Item2Id = definition, CurrentBid = amount }); return; }
                var view = CloneAuction(record.Auction, record.ExpiresAtUtc);
                bool buyout = record.Auction.BuyoutPrice != 0 && amount >= record.Auction.BuyoutPrice;
                ulong accepted = buyout ? record.Auction.BuyoutPrice : amount;
                if (auctionPostings.ContainsKey(auctionId) || auctionSettlements.ContainsKey(auctionId) || !HasConsistentAuctionView(record) || record.StoredRow == null
                    || buyer == null || TryGetCharacterDatabase() == null || cleanupManager == null)
                { reply(GenericError.DbFailure, view); return; }
                if (record.Auction.OwnerCharacterId == buyer.CharacterId || accepted < record.Auction.MinimumBid
                    || accepted <= record.Auction.CurrentBid)
                { reply(GenericError.Params, view); return; }
                if (!buyout && record.Auction.TopBidderCharacterId != buyer.CharacterId
                    && CountActiveAuctionBids(buyer.CharacterId) >= MarketplaceAccountLimits.GetMaxAuctionBids(buyer))
                { reply(GenericError.AuctionTooManyBids, view); return; }
                if (record.Item is not Item item)
                { reply(GenericError.DbFailure, view); return; }
                BidItem escrow;
                try { escrow = BidItem.Capture(item.CaptureAuctionEscrowPreimage()); }
                catch (Exception error)
                { log.Warn(error, "Normal bid escrow capture failed."); reply(GenericError.DbFailure, view); return; }
                operation = new(this, buyer, record, escrow, accepted,
                    buyout ? AuctionSettlementKind.Buyout : AuctionSettlementKind.Bid, reply, publicationAllowed);
                auctionSettlements.Add(auctionId, operation);
            }
            operation.Start();
        }

        public bool TryQueueAuctionCancellation(IPlayer player, ulong auctionId, uint definition,
            Action<GenericError, AuctionInfo> reply, Func<bool> publicationAllowed = null)
        {
            if (player is not Player owner || TryGetCharacterDatabase() == null) return false;
            AuctionSettlementOperation operation;
            lock (syncRoot)
            {
                var record = auctions.SingleOrDefault(row => row.Auction.AuctionId == auctionId);
                if (record == null || record.Auction.Item2Id != definition)
                { reply(GenericError.ItemBadId, new() { AuctionId = auctionId, Item2Id = definition }); return true; }
                var view = CloneAuction(record.Auction, record.ExpiresAtUtc);
                if (record.Auction.OwnerCharacterId != owner.CharacterId)
                { reply(GenericError.Params, view); return true; }
                if (auctionPostings.ContainsKey(auctionId) || auctionSettlements.ContainsKey(auctionId) || !HasConsistentAuctionView(record) || record.StoredRow == null
                    || cleanupManager == null || record.Item is not Item item)
                { reply(GenericError.DbFailure, view); return true; }
                if (owner.Inventory.GetInventorySlotsRemaining(InventoryLocation.Inventory) == 0 && !IsMarketplaceMailAvailable())
                { reply(GenericError.ItemInventoryFull, view); return true; }
                BidItem escrow;
                try { escrow = BidItem.Capture(item.CaptureAuctionEscrowPreimage()); }
                catch (Exception error)
                { log.Warn(error, "Cancelled auction lost its loaded escrow preimage."); reply(GenericError.DbFailure, view); return true; }
                operation = new(this, owner, record, escrow, 0, AuctionSettlementKind.CancelledReturn, reply, publicationAllowed);
                auctionSettlements.Add(auctionId, operation);
            }
            operation.Start(); return true;
        }

        private bool HasPendingAuctionSettlement(MarketplaceAuction record)
        { lock (syncRoot) return auctionPostings.ContainsKey(record.Auction.AuctionId) || auctionSettlements.ContainsKey(record.Auction.AuctionId); }

        private bool TryQueueExpiredAuction(MarketplaceAuction record)
        {
            if (TryGetCharacterDatabase() == null) return false;
            AuctionSettlementOperation operation;
            lock (syncRoot)
            {
                if (auctionPostings.ContainsKey(record.Auction.AuctionId) || auctionSettlements.ContainsKey(record.Auction.AuctionId)) return true;
                if (!HasConsistentAuctionView(record) || record.StoredRow == null || record.Item is not Item item)
                    return true;
                bool winning = record.StoredRow.Bidder != 0 && record.StoredRow.Bid != 0;
                ulong receiver = winning ? record.StoredRow.Bidder : record.StoredRow.Owner;
                // Keep legacy IPlayer test/adaptor behavior. Runtime Players
                // must use the retained loaded-owner settlement path.
                var onlineReceiver = playerManager?.GetPlayer(receiver);
                var onlineSeller = playerManager?.GetPlayer(record.StoredRow.Owner);
                if (onlineReceiver != null && onlineReceiver is not Player || onlineSeller != null && onlineSeller is not Player) return false;
                if (cleanupManager == null) return true;
                BidItem escrow;
                try { escrow = BidItem.Capture(item.CaptureAuctionEscrowPreimage()); }
                catch (Exception error) { log.Warn(error, "Expired auction lost its loaded escrow preimage."); return true; }
                operation = new(this, null, record, escrow, winning ? record.StoredRow.Bid : 0,
                    winning ? AuctionSettlementKind.WinningExpiry : AuctionSettlementKind.UnsoldReturn, null, null);
                auctionSettlements.Add(record.Auction.AuctionId, operation);
            }
            operation.Start(); return true;
        }

        private sealed class AuctionSettlementOperation
        {
            private sealed record Participant(Player Player, IGameSession Session, IAccount Account);
            private sealed record Actor(Participant Owner, Player.AuctionBidActor Prepared, BidWallet Wallet);
            private readonly GlobalMarketplaceManager manager;
            private Player receiver;
            private readonly Participant initiator;
            private readonly MarketplaceAuction record;
            private readonly CharacterDatabase database;
            private readonly AuctionRow before;
            private readonly BidItem item;
            private readonly ulong amount;
            private readonly ulong bidder;
            private readonly AuctionSettlementKind kind;
            private bool TransfersItem => kind != AuctionSettlementKind.Bid;
            private bool Expiry => kind is AuctionSettlementKind.WinningExpiry or AuctionSettlementKind.UnsoldReturn;
            private bool Cancelling => kind == AuctionSettlementKind.CancelledReturn;
            private bool Returning => kind is AuctionSettlementKind.UnsoldReturn or AuctionSettlementKind.CancelledReturn;
            private bool DebitsReceiver => !Expiry && !Cancelling;
            private readonly ulong proceeds;
            private AuctionDeliverySettlement sale;
            private readonly string route;
            private readonly Action<GenericError, AuctionInfo> reply;
            private readonly Func<bool> initiatorPublicationAllowed;
            private readonly EventQueue events;
            private readonly string id = Guid.NewGuid().ToString("N"), abortedPayload;
            private readonly List<Actor> actors = [];
            private readonly HashSet<Actor> reconciled = [];
            private BidCharacter[] characters;
            private Participant[] preparing;
            private IDisposable accounts;
            private AuctionSettlementCommand command;
            private BidOutcome outcome;
            private AuctionInfo responseAuction;
            private int next;
            private bool finished, preparationFailed, reconciliationFailed, unresolvedReported;
            private GenericError preparationResult = GenericError.DbFailure;

            internal AuctionSettlementOperation(GlobalMarketplaceManager manager, Player receiver, MarketplaceAuction record,
                BidItem item, ulong amount, AuctionSettlementKind kind, Action<GenericError, AuctionInfo> reply, Func<bool> publicationAllowed)
            {
                this.manager = manager; this.receiver = receiver; this.record = record; this.item = item; this.amount = amount; this.reply = reply;
                this.kind = kind;
                before = record.StoredRow; database = manager.TryGetCharacterDatabase();
                events = Expiry ? manager.settlementEvents : receiver.Session.Events;
                initiator = Expiry ? null : new(receiver, receiver.Session, receiver.Account);
                bidder = Expiry ? Returning ? before.Owner : before.Bidder : receiver.CharacterId;
                route = record.StoredDatabase;
                initiatorPublicationAllowed = publicationAllowed;
                proceeds = TransfersItem && !Returning ? MarketplaceTransactionFee.CalculateItemAuctionSellerProceeds(amount, manager.GetGameTableManager()) : 0;
                abortedPayload = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes($"aborted-normal-bid:{id}:{before.Id}:{bidder}:{amount}")));
            }

            private void OnCoordinator(Action action) => events.EnqueueThreadSafe(new PredicateEvent(() => true, action));
            private static void OnActor(Participant owner, Action action) => owner.Session.Events.EnqueueThreadSafe(new PredicateEvent(() => true, action));

            internal void Start()
            {
                events.EnqueueThreadSafe(new TaskGenericEvent<BidCharacter[]>(Task.Run(() =>
                {
                    using var context = database.CreateContext(); CharacterDatabaseIdentity.Require(context, route);
                    ulong[] ids = new[] { before.Owner, bidder, before.Bidder }.Where(value => value != 0).Distinct().ToArray();
                    var owners = context.Character.AsNoTracking().Where(row => ids.Contains(row.Id)).ToArray();
                    if (owners.Length != ids.Length || owners.Any(row => row.DeleteTime != null || row.AccountId == 0))
                        throw new InvalidOperationException("Normal bid lost a captured Character owner.");
                    return owners.Select(row => new BidCharacter(row.Id, row.AccountId)).OrderBy(row => row.Id).ToArray();
                }), owners =>
                {
                    characters = owners;
                    if (!manager.cleanupManager.TryAcquireAccounts(owners.Select(row => row.Account).Distinct().ToArray(), out accounts))
                    { Finish(GenericError.DbFailure); return; }
                    var selectedReceiver = Expiry ? manager.playerManager?.GetPlayer(bidder) : receiver;
                    var prior = !Expiry && before.Bidder != 0 && before.Bidder != bidder ? manager.playerManager?.GetPlayer(before.Bidder) : null;
                    var seller = TransfersItem && proceeds != 0 ? manager.playerManager?.GetPlayer(before.Owner) : null;
                    if (selectedReceiver != null && selectedReceiver is not Player || prior != null && prior is not Player || seller != null && seller is not Player)
                    { Finish(GenericError.DbFailure); return; }
                    if (Expiry) receiver = selectedReceiver as Player;
                    preparing = new[] { receiver, prior as Player, seller as Player }.Where(player => player != null).Distinct().OrderBy(player => player.CharacterId)
                        .Select(player => new Participant(player, player.Session, player.Account)).ToArray();
                    PrepareNext();
                }, () => Finish(GenericError.DbFailure)));
            }

            private void PrepareNext()
            {
                if (next == preparing.Length) { Execute(); return; }
                Participant participant = preparing[next++]; Player player = participant.Player;
                OnActor(participant, () =>
                {
                    if (!ReferenceEquals(player.Session, participant.Session) || !ReferenceEquals(player.Account, participant.Account))
                    { OnCoordinator(() => { preparationFailed = true; AbortPrepared(); }); return; }
                    player.PrepareAuctionBid(this, prepared =>
                {
                    try
                    {
                        bool isReceiver = ReferenceEquals(player, receiver);
                        ulong change = isReceiver ? !DebitsReceiver ? 0 : before.Bidder == bidder ? amount - before.Bid : amount
                            : player.CharacterId == before.Owner ? proceeds : before.Bid;
                        if (prepared.Database != route || !characters.Contains(new(prepared.Character, prepared.Account)))
                            throw new InvalidOperationException("Normal bid actor changed its captured Character/account/database.");
                        if (isReceiver && DebitsReceiver && !player.CurrencyManager.CanAfford(CurrencyType.Credits, change))
                        {
                            prepared.CancelPreparation();
                            OnCoordinator(() => { preparationFailed = true; preparationResult = GenericError.VendorNotEnoughCash; AbortPrepared(); });
                            return;
                        }
                        bool subtract = isReceiver && DebitsReceiver;
                        prepared.Capture(change, subtract, silentSettlement: isReceiver && !DebitsReceiver);
                        if (TransfersItem && isReceiver) prepared.CaptureDelivery(item);
                        var actor = new Actor(participant, prepared, prepared.Wallet.Snapshot);
                        OnCoordinator(() => { actors.Add(actor); PrepareNext(); });
                    }
                    catch
                    {
                        if (prepared.Wallet == null) prepared.CancelPreparation();
                        OnCoordinator(() =>
                        {
                            if (prepared.Wallet != null) actors.Add(new(participant, prepared, prepared.Wallet.Snapshot));
                            preparationFailed = true; AbortPrepared();
                        });
                    }
                    }, () => OnCoordinator(() => { preparationFailed = true; AbortPrepared(); }));
                });
            }

            private void Execute()
            {
                try
                {
                    bool offlineRefund = !Expiry && before.Bidder != 0 && before.Bidder != bidder && before.Bid != 0
                        && actors.All(actor => actor.Wallet.Character != before.Bidder);
                    MailHeaderState refund = offlineRefund
                        ? MarketplaceMailDelivery.CreateNormalBidRefund(before.Bidder, before.Bid, manager.GetGameTableManager(), manager.assetManager) : null;
                    if (TransfersItem)
                    {
                        var delivery = actors.SingleOrDefault(actor => actor.Wallet.Character == bidder)?.Prepared;
                        var sellerMail = proceeds != 0 && actors.All(actor => actor.Wallet.Character != before.Owner)
                            ? MarketplaceMailDelivery.CreateNormalBidRefund(before.Owner, proceeds, manager.GetGameTableManager(), manager.assetManager) : null;
                        var deliveryMail = delivery == null || delivery.DeliveryByMail
                            ? Returning ? MarketplaceMailDelivery.CreateExpiryReturn(bidder, manager.GetGameTableManager(), manager.assetManager)
                                : MarketplaceMailDelivery.CreateBuyoutDelivery(bidder, manager.GetGameTableManager(), manager.assetManager) : null;
                        sale = new(proceeds, delivery?.DeliveryTarget ?? (item with { Owner = bidder,
                            Location = (ushort)InventoryLocation.None, Bag = 0 }), sellerMail, deliveryMail, kind);
                    }
                    command = new(database.CreateContext, route, id, DateTime.UtcNow, before, bidder,
                        amount, item, characters, actors.Select(actor => actor.Wallet), refund, sale);
                    events.EnqueueThreadSafe(new TaskGenericEvent<BidOutcome>(Task.Run(() => manager.auctionSettlementExecution == null
                        ? command.ExecuteAsync() : manager.auctionSettlementExecution(command)), Settle, WaitForOutcome));
                }
                catch { preparationFailed = true; AbortPrepared(); }
            }

            private void WaitForOutcome()
            {
                if (!unresolvedReported)
                { unresolvedReported = true; log.Warn("Normal bid outcome is unresolved; original actors and account/listing ownership remain held."); }
                events.EnqueueThreadSafe(new DelayEvent(TimeSpan.FromSeconds(1), () =>
                    events.EnqueueThreadSafe(new TaskGenericEvent<BidOutcome>(Task.Run(command.ResolveAsync), Settle, WaitForOutcome))));
            }

            private void Settle(BidOutcome result)
            {
                outcome = result;
                if (result.Committed)
                {
                    var accepted = Returning ? before : before with { Bid = amount, Bidder = bidder };
                    responseAuction = CloneAuction(ToAuctionInfo(accepted.Model()), accepted.Expiration);
                }
                if (!result.CurrentGraphMatches || TransfersItem && result.Committed)
                {
                    ReadCurrentAuthority();
                    return;
                }
                if (result.Committed)
                {
                    lock (manager.syncRoot)
                    {
                        record.Auction.CurrentBid = amount; record.Auction.TopBidderCharacterId = bidder;
                        record.StoredRow = TransfersItem ? null : before with { Bid = amount, Bidder = bidder };
                        record.LiveRow = TransfersItem ? null : AuctionRow.Capture(ToAuctionModel(record)); record.HasStoredOutcome = true;
                        if (TransfersItem) manager.auctions.Remove(record);
                    }
                }
                ReconcileActors(result.Committed);
            }

            private void ReadCurrentAuthority()
            {
                events.EnqueueThreadSafe(new TaskGenericEvent<BidReconciliationView>(Task.Run(command.ReadReconciliationAsync), view =>
                {
                    outcome = view.Outcome;
                    lock (manager.syncRoot)
                    {
                        manager.auctions.Remove(record);
                        if (view.Auction != null && view.Item != null && view.Item.Owner == null
                            && view.Item.Location == (ushort)InventoryLocation.None && view.Auction.Item == view.Item.Id
                            && view.Auction.Definition == view.Item.Definition && view.Auction.Quantity == view.Item.Quantity)
                        {
                            var current = new MarketplaceAuction { Auction = ToAuctionInfo(view.Auction.Model()),
                                Item = new Item(view.Item.Model(), manager.itemManager, manager.GetGameTableManager()),
                                ExpiresAtUtc = view.Auction.Expiration, StoredDatabase = route, StoredRow = view.Auction, HasStoredOutcome = true };
                            current.LiveRow = AuctionRow.Capture(ToAuctionModel(current)); manager.auctions.Add(current);
                            if (!outcome.Committed) responseAuction = CloneAuction(current.Auction, current.ExpiresAtUtc);
                        }
                        else if (!outcome.Committed) responseAuction = new() { AuctionId = before.Id, Item2Id = before.Definition };
                    }
                    ReconcileActors(outcome.Committed, view.Wallets, view.Item, view.DeliveryMatches);
                }, () =>
                {
                    if (!unresolvedReported)
                    { unresolvedReported = true; log.Warn("Bid current-state reconciliation is unavailable; original actor/account ownership remains held."); }
                    events.EnqueueThreadSafe(new DelayEvent(TimeSpan.FromSeconds(1), ReadCurrentAuthority));
                }));
            }

            private void AbortPrepared()
            {
                if (actors.Count == 0) { Finish(preparationResult); return; }
                ReconcileActors(false);
            }

            private void ReconcileActors(bool committed, IReadOnlyDictionary<ulong, BidWalletCurrent> current = null,
                BidItem currentDelivery = null, bool deliveryMatches = true)
            {
                if (actors.Count == 0) { ReconciliationEnded(); return; }
                foreach (var actor in actors)
                    OnActor(actor.Owner, () =>
                    {
                        try
                        {
                            actor.Prepared.Reconcile(committed, id, command?.PayloadSHA256 ?? abortedPayload,
                                () => OnCoordinator(() => ActorEnded(actor, true)),
                                () => OnCoordinator(() => ActorEnded(actor, false)), current?.GetValueOrDefault(actor.Wallet.Character),
                                current == null ? sale?.DeliveredItem : currentDelivery, deliveryMatches, () => CanPublish(actor), sale?.DeliveryMail);
                        }
                        catch { OnCoordinator(() => ActorEnded(actor, false)); }
                    });
            }

            private void ActorEnded(Actor actor, bool success)
            {
                if (!reconciled.Add(actor)) return;
                reconciliationFailed |= !success;
                if (reconciled.Count != actors.Count) return;
                ReconciliationEnded();
            }

            private void ReconciliationEnded()
            {
                if (preparationFailed || reconciliationFailed || outcome?.Committed != true)
                { Finish(preparationFailed && !reconciliationFailed ? preparationResult : GenericError.DbFailure); return; }
                if (TransfersItem)
                {
                    var winner = actors.SingleOrDefault(candidate => candidate.Wallet.Character == bidder);
                    if (winner == null) { NotifyPrevious(); return; }
                    OnActor(winner.Owner, () =>
                    {
                        try
                        {
                            if (CanPublish(winner))
                            {
                                if (sale.DeliveryMail != null)
                                {
                                    var model = sale.DeliveryMail.Model();
                                    model.Attachment.Add(new() { Id = model.Id, Index = 0, ItemGuid = sale.DeliveredItem.Id,
                                        Item = sale.DeliveredItem.Model() });
                                    winner.Prepared.PublishDeliveryMail(new MailItem(model, itemManager: manager.itemManager,
                                        gameTableManager: manager.GetGameTableManager()));
                                }
                                if (!Cancelling && CanPublish(winner))
                                    winner.Owner.Session.EnqueueMessageEncrypted(Returning
                                        ? new ServerAuctionExpired { Auction = responseAuction } : new ServerAuctionWon { Auction = responseAuction });
                            }
                        }
                        finally { OnCoordinator(NotifyPrevious); }
                    });
                }
                else NotifyPrevious();
            }

            private bool CanPublish(Actor actor) => !actor.Prepared.Retired
                && (actor.Wallet.Character != bidder || initiatorPublicationAllowed?.Invoke() != false)
                && ReferenceEquals(actor.Owner.Player.Session, actor.Owner.Session)
                && (actor.Owner.Session is not IPlayerSession bound || ReferenceEquals(bound.Player, actor.Owner.Player))
                && ReferenceEquals(actor.Owner.Player.Account, actor.Owner.Account) && actor.Owner.Account.Id == actor.Wallet.Account
                && ReferenceEquals(actor.Owner.Account.Session, actor.Owner.Session)
                && ReferenceEquals(manager.playerManager?.GetPlayer(actor.Wallet.Character), actor.Owner.Player)
                && actor.Owner.Session is not NetworkSession { IsDisconnecting: true }
                && actor.Owner.Player.LogoutManager?.State is not (LogoutState.Logout or LogoutState.Finished);

            private void NotifyPrevious()
            {
                if (Cancelling) { Finish(GenericError.Ok); return; }
                var previous = actors.SingleOrDefault(candidate => candidate.Wallet.Character == before.Bidder && before.Bidder != bidder);
                if (previous == null) { Finish(GenericError.Ok); return; }
                var notification = responseAuction ?? CloneAuction(record.Auction, record.ExpiresAtUtc);
                OnActor(previous.Owner, () =>
                {
                    try
                    {
                        if (CanPublish(previous)) previous.Owner.Session.EnqueueMessageEncrypted(new ServerAuctionOutbid { Auction = notification });
                    }
                    finally { OnCoordinator(() => Finish(GenericError.Ok)); }
                });
            }

            private void Finish(GenericError error)
            {
                if (finished) return; finished = true;
                AuctionInfo auction;
                lock (manager.syncRoot)
                {
                    if (!manager.auctionSettlements.TryGetValue(before.Id, out var current) || !ReferenceEquals(current, this))
                        throw new InvalidOperationException("Normal bid cannot finish another listing owner.");
                    manager.auctionSettlements.Remove(before.Id); auction = responseAuction ?? CloneAuction(record.Auction, record.ExpiresAtUtc);
                }
                accounts?.Dispose(); accounts = null;
                if (reply == null) return;
                if (actors.Any(actor => ReferenceEquals(actor.Owner.Player, receiver) && actor.Prepared.Retired)
                    || !ReferenceEquals(receiver.Session, initiator.Session) || !ReferenceEquals(receiver.Account, initiator.Account)
                    || !ReferenceEquals(initiator.Account?.Session, initiator.Session)
                    || !ReferenceEquals(manager.playerManager?.GetPlayer(bidder), receiver)) return;
                reply(error, auction);
            }
        }
    }
}
