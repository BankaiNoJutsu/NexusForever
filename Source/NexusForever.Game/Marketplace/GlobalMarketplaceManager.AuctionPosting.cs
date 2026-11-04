using Microsoft.EntityFrameworkCore;
using NexusForever.Database.Character;
using NexusForever.Game.Abstract.Account;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Entity;
using NexusForever.Game.Static.Entity;
using NexusForever.Network.Session;
using NexusForever.Network.World.Message.Model;
using NexusForever.Network.World.Message.Model.Marketplace;
using NexusForever.Network.World.Message.Static;
using NexusForever.Shared.Game.Events;

namespace NexusForever.Game.Marketplace
{
    public sealed partial class GlobalMarketplaceManager
    {
        private readonly Dictionary<ulong, AuctionPostingOperation> auctionPostings = [];

        public bool TryQueueAuctionPosting(IPlayer player, ulong guid, ulong minimum, ulong buyout,
            Action<GenericError, AuctionInfo> reply, Func<bool> publicationAllowed = null)
        {
            if (player is not Player original || TryGetCharacterDatabase() == null) return false;
            var item = original.Inventory.GetItem(guid);
            if (item?.Info == null) { reply(GenericError.ItemBadId, new()); return true; }
            if (!CanPostAuctionItem(original, item)) { reply(GenericError.AuctionCannotFillOrder, new()); return true; }
            if (cleanupManager == null || minimum == 0 || buyout != 0 && buyout < minimum)
            { reply(GenericError.DbFailure, new()); return true; }
            int limit = MarketplaceAccountLimits.GetMaxAuctionSellLots(original);
            AuctionPostingOperation operation;
            lock (syncRoot)
            {
                if (auctionPostings.Values.Any(row => row.ItemGuid == guid))
                { reply(GenericError.DbFailure, new()); return true; }
                if (CountOwnedSellAuctions(original.CharacterId) >= limit)
                { reply(GenericError.AuctionTooManyOrders, new()); return true; }
                ulong id = nextAuctionId++;
                operation = new(this, id, original, guid, minimum, buyout, reply, publicationAllowed);
                auctionPostings.Add(id, operation);
            }
            operation.Start(); return true;
        }

        private sealed class AuctionPostingOperation
        {
            private readonly GlobalMarketplaceManager manager;
            private readonly ulong auctionId, minimum, buyout;
            internal ulong ItemGuid { get; }
            internal ulong Character { get; }
            private readonly Player player;
            private readonly IGameSession session;
            private readonly EventQueue events;
            private readonly IInventory inventory;
            private readonly IAccount account;
            private readonly uint accountId;
            private readonly CharacterDatabase database;
            private readonly Action<GenericError, AuctionInfo> reply;
            private readonly Func<bool> publicationAllowed;
            private readonly string id = Guid.NewGuid().ToString("N");
            private readonly string aborted = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData("aborted-auction-posting"u8));
            private IDisposable accounts;
            private Player.AuctionBidActor actor;
            private AuctionPostingCommand command;
            private AuctionRow target;
            private AuctionInfo response = new();
            private bool finished;
            private bool removalPending;
            private string route;
            internal AuctionPostingOperation(GlobalMarketplaceManager manager, ulong auctionId, Player player, ulong guid, ulong minimum, ulong buyout,
                Action<GenericError, AuctionInfo> reply, Func<bool> publicationAllowed)
            {
                this.manager = manager; this.auctionId = auctionId; this.player = player; ItemGuid = guid;
                this.minimum = minimum; this.buyout = buyout; this.reply = reply; this.publicationAllowed = publicationAllowed;
                session = player.Session; account = player.Account; accountId = account?.Id ?? 0; Character = player.CharacterId;
                events = session?.Events;
                inventory = player.Inventory;
                database = manager.TryGetCharacterDatabase();
            }
            private bool CanPublish() => !finished && IsCurrentActor();

            private bool IsCurrentActor() => actor?.Retired != true && publicationAllowed?.Invoke() != false
                && events != null && ReferenceEquals(session.Events, events)
                && player.CharacterId == Character && ReferenceEquals(player.Inventory, inventory)
                && ReferenceEquals(player.Session, session) && ReferenceEquals(player.Account, account) && account?.Id == accountId
                && ReferenceEquals(account.Session, session) && ReferenceEquals(manager.playerManager?.GetPlayer(Character), player)
                && (session is not IPlayerSession bound || ReferenceEquals(bound.Player, player))
                && session is not NetworkSession { IsDisconnecting: true }
                && player.LogoutManager?.State is not (LogoutState.Logout or LogoutState.Finished);

            internal void Start()
            {
                if (session == null || accountId == 0 || !manager.cleanupManager.TryAcquireAccounts([accountId], out accounts))
                { Finish(GenericError.DbFailure); return; }
                session.Events.EnqueueThreadSafe(new TaskGenericEvent<string>(Task.Run(() =>
                {
                    using var context = database.CreateContext();
                    var owner = context.Character.AsNoTracking().SingleOrDefault(row => row.Id == Character);
                    if (owner == null || owner.DeleteTime != null || owner.AccountId != accountId)
                        throw new InvalidOperationException("Auction posting lost its captured Character/account.");
                    return CharacterDatabaseIdentity.Get(context);
                }), value => { route = value; Prepare(); }, () => Finish(GenericError.DbFailure)));
            }

            private void Prepare()
            {
                if (!CanPublish()) { Finish(GenericError.DbFailure); return; }
                player.PrepareAuctionBid(this, prepared =>
                {
                    actor = prepared;
                    try
                    {
                        if (actor.Database != route || actor.Character != Character || actor.Account != accountId)
                            throw new InvalidOperationException("Auction posting changed its original actor route.");
                        actor.Capture(0, false, silentSettlement: true);
                        actor.CaptureAuctionPosting(ItemGuid, () => MarketplaceMailDelivery.CreateExpiryReturn(Character, manager.GetGameTableManager(), manager.assetManager));
                        var item = actor.PostingItem;
                        ulong duration = MarketplaceAuctionDuration.GetDefaultExpirationSeconds(manager.GetGameTableManager());
                        ulong expiry = (ulong)DateTimeOffset.UtcNow.AddSeconds(duration).ToUnixTimeSeconds();
                        response = new() { AuctionId = auctionId, OwnerCharacterId = Character, Item2Id = item.Definition,
                            Quantity = item.Quantity, MinimumBid = minimum, BuyoutPrice = buyout, ExpirationTime = duration };
                        var record = new MarketplaceAuction { Auction = CloneAuction(response, expiry), ExpiresAtUtc = expiry,
                            Item = new Item((item with { Owner = null, Location = (ushort)InventoryLocation.None, Bag = 0 }).Model(), manager.itemManager, manager.GetGameTableManager()) };
                        target = AuctionRow.Capture(ToAuctionModel(record));
                        command = new(database.CreateContext, route, id, DateTime.UtcNow, target, item, actor.Wallet.Snapshot);
                        session.Events.EnqueueThreadSafe(new TaskGenericEvent<BidOutcome>(Task.Run(() => manager.auctionPostingExecution == null
                            ? command.ExecuteAsync() : manager.auctionPostingExecution.Execute(command)), _ => ReadCurrent(), Resolve));
                    }
                    catch (Exception error)
                    {
                        log.Warn(error, "Auction posting preparation failed.");
                        if (actor.Wallet == null) { actor.CancelPreparation(); Finish(GenericError.DbFailure); }
                        else Reconcile(false, null);
                    }
                }, () => Finish(GenericError.DbFailure));
            }

            private void Resolve() => session.Events.EnqueueThreadSafe(new DelayEvent(TimeSpan.FromSeconds(1), () =>
                session.Events.EnqueueThreadSafe(new TaskGenericEvent<BidOutcome>(Task.Run(command.ResolveAsync), _ => ReadCurrent(), Resolve))));

            private void ReadCurrent()
            {
                session.Events.EnqueueThreadSafe(new TaskGenericEvent<AuctionPostingView>(Task.Run(command.ReadReconciliationAsync), view =>
                {
                    lock (manager.syncRoot)
                    {
                        manager.auctions.RemoveAll(row => row.Auction.AuctionId == auctionId);
                        if (view.CustodyMatches && view.Auction != null && view.Item != null && view.Item.Owner == null
                            && view.Item.Location == (ushort)InventoryLocation.None && view.Auction.Item == view.Item.Id
                            && view.Auction.Definition == view.Item.Definition && view.Auction.Quantity == view.Item.Quantity)
                        {
                            var loaded = new MarketplaceAuction { Auction = ToAuctionInfo(view.Auction.Model()),
                                Item = new Item(view.Item.Model(), manager.itemManager, manager.GetGameTableManager()),
                                ExpiresAtUtc = view.Auction.Expiration, StoredDatabase = route, StoredRow = view.Auction, HasStoredOutcome = true };
                            loaded.LiveRow = AuctionRow.Capture(ToAuctionModel(loaded)); manager.auctions.Add(loaded);
                        }
                    }
                    Reconcile(view.Outcome.Committed, view);
                }, () => session.Events.EnqueueThreadSafe(new DelayEvent(TimeSpan.FromSeconds(1), ReadCurrent))));
            }

            private void Reconcile(bool committed, AuctionPostingView view)
            {
                try
                {
                    actor.Reconcile(committed, id, command?.PayloadSHA256 ?? aborted,
                        () => Finish(committed ? GenericError.Ok : GenericError.DbFailure), () => Finish(GenericError.DbFailure),
                        view?.Wallet, view?.Item, view?.CustodyMatches ?? true, CanPublish, postingListing: target,
                        publishPostingRemoval: () => removalPending = !TryPublishRemoval());
                }
                catch (Exception error) { log.Warn(error, "Auction posting reconciliation failed."); Finish(GenericError.DbFailure); }
            }

            private void Finish(GenericError result)
            {
                if (finished) return;
                bool publish = CanPublish(); finished = true;
                lock (manager.syncRoot)
                {
                    if (!manager.auctionPostings.TryGetValue(auctionId, out var owner) || !ReferenceEquals(owner, this))
                        throw new InvalidOperationException("Auction posting cannot release another operation's ownership.");
                    manager.auctionPostings.Remove(auctionId);
                }
                accounts?.Dispose(); accounts = null;
                if (!publish) return;
                if (result != GenericError.Ok) reply(result, response);
                else if (removalPending) RetryPublication();
                else PublishCommittedResult();
            }

            private bool TryPublishRemoval()
            {
                try
                {
                    session.EnqueueMessageEncrypted(new ServerItemDelete { Guid = ItemGuid, Reason = ItemUpdateReason.Auction });
                    return true;
                }
                catch (Exception error)
                {
                    log.Warn(error, "Committed auction posting inventory notice failed.");
                    return false;
                }
            }

            private void RetryPublication() => events.EnqueueThreadSafe(new DelayEvent(TimeSpan.FromSeconds(1), PublishCommittedResult));

            private void PublishCommittedResult()
            {
                if (!IsCurrentActor()) return;
                if (removalPending)
                {
                    lock (manager.syncRoot)
                        if (manager.auctionSettlements.ContainsKey(auctionId)) { RetryPublication(); return; }
                    // The whole Item can return to this inventory in a later settlement.
                    if (inventory.GetItem(ItemGuid) == null && !TryPublishRemoval()) { RetryPublication(); return; }
                    removalPending = false;
                    if (!IsCurrentActor()) return;
                }

                lock (manager.syncRoot)
                {
                    if (manager.auctionSettlements.ContainsKey(auctionId)) { RetryPublication(); return; }
                    var current = manager.auctions.SingleOrDefault(row => row.Auction.AuctionId == auctionId);
                    if (current == null || current.StoredRow != target || current.StoredDatabase != route
                        || !manager.HasConsistentAuctionView(current)) return;
                }

                var view = CloneAuction(response, target.Expiration);
                if (view.ExpirationTime == 0) return;
                try { reply(GenericError.Ok, view); }
                catch (Exception error)
                {
                    log.Warn(error, "Committed auction post result failed; retrying on its original actor.");
                    RetryPublication();
                }
            }
        }
    }
}
