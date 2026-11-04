using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using NexusForever.Database;
using NexusForever.Database.Character;
using NexusForever.Game.Abstract;
using NexusForever.Game.Abstract.Account;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Entity;
using NexusForever.Game.Mail;
using NexusForever.Game.Static.Entity;
using NexusForever.Game.Static.Marketplace;
using NexusForever.Network.Session;
using NexusForever.Network.World.Message.Model.Marketplace;
using NexusForever.Network.World.Message.Static;
using NexusForever.Shared.Game.Events;

namespace NexusForever.Game.Marketplace
{
    public sealed partial class GlobalMarketplaceManager
    {
        private readonly Dictionary<ulong, CommodityReturnOperation> commodityReturns = [];
        private readonly Func<CommodityReturnCommand, Task<BidOutcome>> commodityReturnExecution;

        internal sealed record CommodityReturnExecution(Func<CommodityReturnCommand, Task<BidOutcome>> Execute);

        internal GlobalMarketplaceManager(IDatabaseManager databases, NexusForever.GameTable.IGameTableManager tables,
            IAssetManager assets, IPlayerManager players, IItemManager items, NexusForever.Game.Abstract.ICleanupManager cleanup,
            CommodityReturnExecution execution) : this(databases, tables, assets, players, items, cleanup)
            => commodityReturnExecution = execution.Execute;

        public bool TryQueueCommodityCancellation(IPlayer player, ulong orderId, uint definition, bool buy,
            Action<GenericError, CommodityOrder> reply, Func<bool> publicationAllowed = null)
        {
            if (player is not Player owner || TryGetCharacterDatabase() == null) return false;
            MarketplaceCommodityOrder record;
            lock (syncRoot)
            {
                record = commodityOrders.SingleOrDefault(row => row.Order.CommodityOrderId == orderId
                    && row.Order.Item2Id == definition && row.Order.IsBuyOrder == buy && row.OwnerCharacterId == owner.CharacterId);
                if (record == null) { reply(GenericError.ItemBadId, new() { CommodityOrderId = orderId, Item2Id = definition, IsBuyOrder = buy }); return true; }
            }
            if (!buy && !CanDeliverCommodityItemsToInventory(owner, definition, record.Order.Quantity, itemManager) && !IsMarketplaceMailAvailable())
            { reply(GenericError.ItemInventoryFull, CloneOrder(record.Order)); return true; }
            QueueCommodityReturn(owner, record, false, reply, publicationAllowed); return true;
        }

        private bool TryQueueExpiredCommodity(MarketplaceCommodityOrder record, LegacyCommodityBookAdmission admission)
        {
            if (TryGetCharacterDatabase() == null) return false;
            var owner = playerManager?.GetPlayer(record.OwnerCharacterId);
            if (owner != null && owner is not Player) return false;
            QueueCommodityReturn(null, record, true, null, null, admission); return true;
        }

        private bool HasPendingCommodityReturn(MarketplaceCommodityOrder record)
        { lock (syncRoot) return commodityReturns.ContainsKey(record.Order.CommodityOrderId) || commodityPostings.ContainsKey(record.Order.Item2Id); }

        private void QueueCommodityReturn(Player owner, MarketplaceCommodityOrder record, bool expiry,
            Action<GenericError, CommodityOrder> reply, Func<bool> publicationAllowed, LegacyCommodityBookAdmission admission = null)
        {
            CommodityReturnOperation operation;
            lock (syncRoot)
            {
                bool ownsBook = admission?.Owns(record.Order.Item2Id) == true;
                if (admission != null && !ownsBook)
                    throw new InvalidOperationException("Commodity expiry lost its selected book admission.");
                if (HasPendingCommodityReturn(record) || IsCommodityPostingBookOwned(record.Order.Item2Id) && !ownsBook || cleanupManager == null
                    || !commodityOrders.Contains(record) || !HasConsistentCommodityView(record) || record.StoredRow == null)
                { reply?.Invoke(GenericError.DbFailure, CloneOrder(record.Order)); return; }
                operation = new(this, owner, record, expiry, reply, publicationAllowed);
                commodityReturns.Add(record.Order.CommodityOrderId, operation);
                // Install the return owner before releasing selection admission.
                // Reentrant lookups cannot find an unowned expiry in between.
                if (ownsBook) admission.Dispose();
            }
            operation.Start();
        }

        private sealed class CommodityReturnOperation
        {
            private readonly GlobalMarketplaceManager manager;
            private readonly MarketplaceCommodityOrder record;
            private readonly CommodityRow before;
            private readonly CharacterDatabase database;
            private readonly string route, id = Guid.NewGuid().ToString("N");
            private readonly bool expiry;
            private readonly EventQueue events;
            private readonly Action<GenericError, CommodityOrder> reply;
            private readonly Func<bool> publicationAllowed;
            private Player owner;
            private IGameSession session;
            private IAccount account;
            private Player.AuctionBidActor actor;
            private BidCharacter character;
            private IDisposable admission;
            private CommodityReturnCommand command;
            private BidOutcome outcome;
            private MailHeaderState deliveryMail;
            private bool finished, confirmedOrderAbsent;

            internal CommodityReturnOperation(GlobalMarketplaceManager manager, Player owner, MarketplaceCommodityOrder record,
                bool expiry, Action<GenericError, CommodityOrder> reply, Func<bool> publicationAllowed)
            {
                this.manager = manager; this.owner = owner; this.record = record; this.expiry = expiry;
                this.reply = reply; this.publicationAllowed = publicationAllowed;
                before = record.StoredRow; route = record.StoredDatabase; database = manager.TryGetCharacterDatabase();
                events = expiry ? manager.settlementEvents : owner.Session.Events;
                if (!expiry) { session = owner.Session; account = owner.Account; }
            }
            private void OnCoordinator(Action action) => events.EnqueueThreadSafe(new PredicateEvent(() => true, action));
            private void OnActor(Action action) => session.Events.EnqueueThreadSafe(new PredicateEvent(() => true, action));

            internal void Start()
            {
                events.EnqueueThreadSafe(new TaskGenericEvent<BidCharacter>(Task.Run(() =>
                {
                    using var context = database.CreateContext(); CharacterDatabaseIdentity.Require(context, route);
                    var row = context.Character.AsNoTracking().SingleOrDefault(row => row.Id == before.Owner);
                    if (row == null || row.DeleteTime != null || row.AccountId == 0) throw new InvalidOperationException("Commodity return lost its original Character.");
                    return new BidCharacter(row.Id, row.AccountId);
                }), captured =>
                {
                    character = captured;
                    if (!manager.cleanupManager.TryAcquireAccounts([character.Account], out admission)) { Finish(GenericError.DbFailure); return; }
                    if (expiry)
                    {
                        var online = manager.playerManager?.GetPlayer(character.Id);
                        if (online != null && online is not Player) { Finish(GenericError.DbFailure); return; }
                        owner = online as Player; session = owner?.Session; account = owner?.Account;
                    }
                    if (owner == null) { Execute(); return; }
                    OnActor(() =>
                    {
                        if (!CanPublish()) { OnCoordinator(() => Finish(GenericError.DbFailure)); return; }
                        owner.PrepareAuctionBid(this, prepared =>
                        {
                            actor = prepared;
                            try
                            {
                                if (actor.Character != character.Id || actor.Account != character.Account || actor.Database != route)
                                    throw new InvalidOperationException("Commodity return changed its captured actor/database.");
                                actor.Capture(before.Buy ? before.Price : 0, false, silentSettlement: !before.Buy);
                                if (!before.Buy) actor.CaptureCommodityDelivery(before.Definition, before.Quantity,
                                    () => MarketplaceMailDelivery.CreateCommodityRecovery(before.Owner, manager.GetGameTableManager(), manager.assetManager));
                                OnCoordinator(Execute);
                            }
                            catch
                            {
                                if (actor.Wallet == null) { actor.CancelPreparation(); OnCoordinator(() => Finish(GenericError.DbFailure)); }
                                else OnCoordinator(() => Reconcile(null));
                            }
                        }, () => OnCoordinator(() => Finish(GenericError.DbFailure)));
                    });
                }, () => Finish(GenericError.DbFailure)));
            }

            private void Execute()
            {
                try
                {
                    MailHeaderState mail = null;
                    CommodityItemWrite[] items = [];
                    CommodityMaterialWrite material = null;
                    if (before.Buy)
                    {
                        if (actor == null && before.Price != 0) mail = MarketplaceMailDelivery.CreateNormalBidRefund(before.Owner, before.Price, manager.GetGameTableManager(), manager.assetManager);
                    }
                    else
                    {
                        var delivery = actor?.CommodityDelivery;
                        if (delivery == null || delivery.ByMail)
                            mail = deliveryMail = MarketplaceMailDelivery.CreateCommodityReturn(before.Owner, manager.GetGameTableManager(), manager.assetManager);
                        if (delivery != null) { items = delivery.Writes; material = delivery.Material; }
                        else
                        {
                            var definition = manager.itemManager?.GetItemInfo(before.Definition)
                                ?? throw new InvalidOperationException("Offline commodity return lost its canonical Item definition.");
                            var item = new Item(before.Owner, definition, before.Quantity, itemManager: manager.itemManager, gameTableManager: manager.GetGameTableManager());
                            items = [new(null, BidItem.Capture(item.CaptureRewardModel()))];
                        }
                    }
                    command = new(database.CreateContext, route, id, DateTime.UtcNow, before, character, expiry, actor?.Wallet.Snapshot, items, material, mail);
                    events.EnqueueThreadSafe(new TaskGenericEvent<BidOutcome>(Task.Run(() => manager.commodityReturnExecution == null
                        ? command.ExecuteAsync() : manager.commodityReturnExecution(command)), Settle, Wait));
                }
                catch { Reconcile(null); }
            }
            private void Wait() => events.EnqueueThreadSafe(new DelayEvent(TimeSpan.FromSeconds(1), () =>
                events.EnqueueThreadSafe(new TaskGenericEvent<BidOutcome>(Task.Run(command.ResolveAsync), Settle, Wait))));
            private void Settle(BidOutcome result)
            {
                outcome = result;
                events.EnqueueThreadSafe(new TaskGenericEvent<CommodityReturnView>(Task.Run(command.ReadReconciliationAsync), view =>
                {
                    outcome = view.Outcome;
                    confirmedOrderAbsent = view.Order == null;
                    lock (manager.syncRoot)
                    {
                        manager.RemoveCommodityOrderRecord(record);
                        if (view.Order != null)
                            manager.AddCommodityOrder(new() { OwnerCharacterId = view.Order.Owner, Order = ToCommodityOrder(view.Order.Model()),
                                RequiresStoredRow = true, HasStoredOutcome = true, StoredRow = view.Order, StoredDatabase = route });
                    }
                    Reconcile(view);
                }, () => events.EnqueueThreadSafe(new DelayEvent(TimeSpan.FromSeconds(1), () => Settle(result)))));
            }
            private void Reconcile(CommodityReturnView view)
            {
                if (actor?.Wallet == null) { Finish(outcome?.Committed == true ? GenericError.Ok : GenericError.DbFailure); return; }
                OnActor(() => actor.Reconcile(outcome?.Committed == true, id,
                    command?.PayloadSHA256 ?? Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes($"aborted-commodity:{id}"))),
                    () => OnCoordinator(() => PublishDelivery(outcome?.Committed == true ? GenericError.Ok : GenericError.DbFailure)),
                    () => OnCoordinator(() => Finish(GenericError.DbFailure)), view?.Wallet, publicationAllowed: CanPublish,
                    deliveryMail: deliveryMail, commodityCurrent: view));
            }
            private void PublishDelivery(GenericError result)
            {
                if (result != GenericError.Ok || deliveryMail == null || actor == null) { Finish(result); return; }
                OnActor(() =>
                {
                    try
                    {
                        if (CanPublish())
                        {
                            var mail = deliveryMail.Model();
                            var item = actor.CommodityDelivery.Writes.Single().After;
                            mail.Attachment.Add(new() { Id = mail.Id, Index = 0, ItemGuid = item.Id, Item = item.Model() });
                            actor.PublishDeliveryMail(new MailItem(mail, itemManager: manager.itemManager, gameTableManager: manager.GetGameTableManager()));
                        }
                    }
                    finally { OnCoordinator(() => Finish(result)); }
                });
            }
            private bool CanPublish() => owner != null && actor?.Retired != true
                && (character == null || account?.Id == character.Account)
                && publicationAllowed?.Invoke() != false && ReferenceEquals(owner.Session, session)
                && ReferenceEquals(owner.Account, account) && ReferenceEquals(account?.Session, session)
                && ReferenceEquals(manager.playerManager?.GetPlayer(before.Owner), owner)
                && (session is not IPlayerSession bound || ReferenceEquals(bound.Player, owner))
                && session is not NetworkSession { IsDisconnecting: true }
                && owner.LogoutManager?.State is not (LogoutState.Logout or LogoutState.Finished);
            private void Finish(GenericError result)
            {
                if (finished) return; finished = true;
                lock (manager.syncRoot)
                {
                    if (!manager.commodityReturns.TryGetValue(before.Id, out var current) || !ReferenceEquals(current, this))
                        throw new InvalidOperationException("Commodity return cannot release another order owner.");
                    manager.commodityReturns.Remove(before.Id);
                }
                admission?.Dispose(); admission = null;
                if (!CanPublish()) return;
                if (!expiry) reply(result, CloneOrder(record.Order));
                else if (result == GenericError.Ok && confirmedOrderAbsent) PublishExpiryNotice();
            }

            private void PublishExpiryNotice()
            {
                var queue = session.Events;
                bool CurrentActor() => ReferenceEquals(session.Events, queue) && CanPublish();
                void Schedule(Task delay) => queue.EnqueueThreadSafe(new PredicateEvent(
                    () => !CurrentActor() || delay.IsCompleted, Send));
                void Send()
                {
                    if (!CurrentActor()) return;
                    lock (manager.syncRoot)
                        if (manager.commodityOrders.Any(row => row.Order.CommodityOrderId == before.Id)) return;

                    try
                    {
                        session.EnqueueMessageEncrypted(new ServerCommodityAuctionRemoved
                            { OrderRemoved = ToCommodityOrder(before.Model()), Type = AuctionEventType.Expire });
                    }
                    catch (Exception error)
                    {
                        log.Warn(error, "Committed commodity expiry notice failed; retrying on its original actor.");
                        Schedule(Task.Delay(TimeSpan.FromSeconds(1)));
                    }
                }

                Schedule(Task.CompletedTask);
            }
        }
    }
}
