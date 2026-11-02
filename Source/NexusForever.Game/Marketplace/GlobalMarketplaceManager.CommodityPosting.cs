using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
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
        private readonly Dictionary<uint, CommodityPostingOperation> commodityPostings = [];
        private readonly Dictionary<uint, LegacyCommodityBookAdmission> legacyCommodityPostings = [];
        private readonly Func<CommodityPostingCommand, Task<BidOutcome>> commodityPostingExecution;
        internal sealed record CommodityPostingExecution(Func<CommodityPostingCommand, Task<BidOutcome>> Execute);
        internal GlobalMarketplaceManager(NexusForever.Database.IDatabaseManager databases, NexusForever.GameTable.IGameTableManager tables,
            NexusForever.Game.Abstract.IAssetManager assets, IPlayerManager players, IItemManager items, NexusForever.Game.Abstract.ICleanupManager cleanup,
            CommodityPostingExecution execution) : this(databases, tables, assets, players, items, cleanup) => commodityPostingExecution = execution.Execute;

        public bool TryQueueCommodityPosting(IPlayer player, CommodityOrder request, Action<GenericError, CommodityOrder> reply,
            Func<bool> publicationAllowed = null)
        {
            if (player is not Player owner || TryGetCharacterDatabase() == null) return false;
            CommodityPostingOperation operation;
            lock (syncRoot)
            {
                if (cleanupManager == null || IsCommodityPostingBookOwned(request.Item2Id))
                { reply(GenericError.DbFailure, CloneOrder(request)); return true; }
                if (!request.ForceImmediate && CountOwnedCommodityOrders(owner.CharacterId, request.IsBuyOrder) >=
                    (request.IsBuyOrder ? MarketplaceAccountLimits.GetMaxCommodityBuyOrders(owner) : MarketplaceAccountLimits.GetMaxCommoditySellOrders(owner)))
                { reply(GenericError.AuctionTooManyOrders, CloneOrder(request)); return true; }
                var incoming = CloneOrder(request); incoming.CommodityOrderId = nextCommodityOrderId++;
                incoming.ListTime = MarketplaceCommodityDuration.ResolveListFileTime(request.ListTime);
                incoming.ExpirationTime = MarketplaceCommodityDuration.ResolveExpirationFileTime(request.ListTime, request.ExpirationTime, GetGameTableManager());
                var source = new MarketplaceCommodityOrder { OwnerCharacterId = owner.CharacterId, Order = incoming };
                var candidates = GetOppositeCommodityOrderCandidates(source).Where(row => !HasPendingCommodityReturn(row)
                    && HasConsistentCommodityView(row) && row.StoredRow != null
                    && (row.StoredRow.Expiration == 0 || row.StoredRow.Expiration > (ulong)DateTime.UtcNow.ToFileTimeUtc())).ToArray();
                var quote = new CommodityPostingQuote(CommodityRow.Capture(ToCommodityOrderModel(source)), candidates.Select(row => row.StoredRow), GetGameTableManager());
                operation = new(this, owner, quote, candidates, reply, publicationAllowed);
                commodityPostings.Add(incoming.Item2Id, operation);
            }
            operation.Start(); return true;
        }

        private bool IsCommodityPostingBookOwned(uint definition)
        { lock (syncRoot) return commodityPostings.ContainsKey(definition) || legacyCommodityPostings.ContainsKey(definition); }

        private LegacyCommodityBookAdmission TryAcquireLegacyCommodityBook(uint definition)
        {
            lock (syncRoot)
            {
                if (IsCommodityPostingBookOwned(definition)) return null;
                var admission = new LegacyCommodityBookAdmission(this, definition);
                legacyCommodityPostings.Add(definition, admission); return admission;
            }
        }
        private sealed class LegacyCommodityBookAdmission(GlobalMarketplaceManager manager, uint definition) : IDisposable
        {
            private bool disposed;
            internal bool Owns(uint requested)
            {
                lock (manager.syncRoot)
                    return !disposed && definition == requested && manager.legacyCommodityPostings.TryGetValue(definition, out var current)
                        && ReferenceEquals(current, this);
            }
            public void Dispose()
            {
                lock (manager.syncRoot)
                {
                    if (disposed) return;
                    if (!manager.legacyCommodityPostings.TryGetValue(definition, out var current) || !ReferenceEquals(current, this))
                        throw new InvalidOperationException("Legacy commodity book cannot release another owner.");
                    manager.legacyCommodityPostings.Remove(definition); disposed = true;
                }
            }
        }

        private sealed class CommodityPostingOperation
        {
            private sealed record Participant(Player Player, IGameSession Session, IAccount Account);
            private sealed record Actor(Participant Owner, Player.AuctionBidActor Prepared);
            private readonly GlobalMarketplaceManager manager;
            private readonly CommodityPostingQuote quote;
            private readonly MarketplaceCommodityOrder[] candidates;
            private readonly Participant initiator;
            private readonly Action<GenericError, CommodityOrder> reply;
            private readonly Func<bool> publicationAllowed;
            private readonly CharacterDatabase database;
            private readonly string id = Guid.NewGuid().ToString("N");
            private readonly List<Actor> actors = [];
            private readonly HashSet<Actor> reconciled = [];
            private readonly List<CommodityPostingMail> mails = [];
            private BidCharacter[] characters;
            private Participant[] preparing;
            private string route;
            private int next;
            private IDisposable admission;
            private CommodityPostingCommand command;
            private BidOutcome outcome;
            private CommodityPostingView view;
            private bool aborting, failed, reconciliationFailed, finished;
            private GenericError preparationError = GenericError.DbFailure;

            internal CommodityPostingOperation(GlobalMarketplaceManager manager, Player owner, CommodityPostingQuote quote,
                MarketplaceCommodityOrder[] candidates, Action<GenericError, CommodityOrder> reply, Func<bool> publicationAllowed)
            {
                this.manager = manager; this.quote = quote; this.candidates = candidates;
                initiator = new(owner, owner.Session, owner.Account); this.reply = reply; this.publicationAllowed = publicationAllowed;
                database = manager.TryGetCharacterDatabase();
            }
            private void OnCoordinator(Action action) => manager.settlementEvents.EnqueueThreadSafe(new PredicateEvent(() => true, action));
            private static void OnActor(Participant owner, Action action) => owner.Session.Events.EnqueueThreadSafe(new PredicateEvent(() => true, action));
            internal void Start()
            {
                manager.settlementEvents.EnqueueThreadSafe(new TaskGenericEvent<BidCharacter[]>(Task.Run(() =>
                {
                    using var context = database.CreateContext(); route = CharacterDatabaseIdentity.Get(context);
                    if (candidates.Any(row => row.StoredDatabase != route)) throw new InvalidOperationException("Commodity posting changed its loaded order database.");
                    var ids = quote.Orders.Select(row => (row.Before ?? row.After ?? quote.Incoming).Owner).Distinct().Order().ToArray();
                    var rows = context.Character.AsNoTracking().Where(row => ids.Contains(row.Id)).ToArray();
                    if (rows.Length != ids.Length || rows.Any(row => row.DeleteTime != null || row.AccountId == 0))
                        throw new InvalidOperationException("Commodity posting lost an original Character/account.");
                    return rows.Select(row => new BidCharacter(row.Id, row.AccountId)).OrderBy(row => row.Id).ToArray();
                }), owners =>
                {
                    characters = owners;
                    if (!manager.cleanupManager.TryAcquireAccounts(owners.Select(row => row.Account).Distinct().ToArray(), out admission)) { Finish(GenericError.DbFailure); return; }
                    List<Participant> online = [];
                    foreach (var owner in owners)
                    {
                        var selected = owner.Id == quote.Incoming.Owner ? initiator.Player : manager.playerManager?.GetPlayer(owner.Id);
                        if (selected != null && selected is not Player) { Finish(GenericError.DbFailure); return; }
                        if (selected is Player player) online.Add(new(player, player.Session, player.Account));
                    }
                    preparing = online.OrderBy(row => row.Player.CharacterId).ToArray(); PrepareNext();
                }, () => Finish(GenericError.DbFailure)));
            }
            private bool CanPublish(Participant owner) => (owner.Player.CharacterId != quote.Incoming.Owner || publicationAllowed?.Invoke() != false)
                && ReferenceEquals(owner.Player.Session, owner.Session) && ReferenceEquals(owner.Player.Account, owner.Account)
                && ReferenceEquals(owner.Account?.Session, owner.Session)
                && ReferenceEquals(manager.playerManager?.GetPlayer(owner.Player.CharacterId), owner.Player)
                && (owner.Session is not IPlayerSession bound || ReferenceEquals(bound.Player, owner.Player))
                && owner.Session is not NetworkSession { IsDisconnecting: true }
                && owner.Player.LogoutManager?.State is not (LogoutState.Logout or LogoutState.Finished);
            private bool CanPublish(Actor actor) => !actor.Prepared.Retired && CanPublish(actor.Owner);
            private void PrepareNext()
            {
                if (next == preparing.Length) { Execute(); return; }
                var participant = preparing[next++];
                OnActor(participant, () =>
                {
                    try
                    {
                        if (!CanPublish(participant)) { OnCoordinator(Abort); return; }
                        participant.Player.PrepareAuctionBid(this, prepared =>
                        {
                            try
                            {
                                if (!CanPublish(participant) || prepared.Database != route || !characters.Contains(new(prepared.Character, prepared.Account)))
                                    throw new InvalidOperationException("Commodity posting lost its original prepared actor.");
                                if (prepared.Character == quote.Incoming.Owner && quote.Consumption != 0
                                    && !participant.Player.Inventory.HasItemCount(quote.Incoming.Definition, quote.Consumption))
                                {
                                    prepared.CancelPreparation(); OnCoordinator(() => { preparationError = GenericError.ItemBadId; Abort(); }); return;
                                }
                                if (prepared.Character == quote.Incoming.Owner && quote.Debit != 0
                                    && !participant.Player.CurrencyManager.CanAfford(CurrencyType.Credits, quote.Debit))
                                {
                                    prepared.CancelPreparation(); OnCoordinator(() => { preparationError = GenericError.VendorNotEnoughCash; Abort(); }); return;
                                }
                                ulong before = participant.Player.CurrencyManager.SingleOrDefault(row => row.Id == CurrencyType.Credits)?.Amount ?? 0;
                                ulong cap = manager.GetGameTableManager().CurrencyType.GetEntry((uint)CurrencyType.Credits)?.CapAmount
                                    ?? throw new InvalidOperationException("Commodity posting has no canonical Credits cap.");
                                ulong after = quote.WalletAfter(prepared.Character, before, cap);
                                prepared.Capture(after < before ? before - after : after - before, after < before, silentSettlement: after == before);
                                prepared.Wallet.CaptureCommodityLedger(prepared.Character == quote.Incoming.Owner ? quote.Debit : 0,
                                    quote.Credits.Where(row => row.Character == prepared.Character).Select(row => row.Amount));
                                uint consume = prepared.Character == quote.Incoming.Owner ? quote.Consumption : 0;
                                uint deliver = quote.Deliveries.GetValueOrDefault(prepared.Character);
                                if (consume != 0 || deliver != 0) prepared.CaptureCommodityPosting(quote.Incoming.Definition, consume, quote.DeliveryBatches(prepared.Character),
                                    () => MarketplaceMailDelivery.CreateCommodityRecovery(prepared.Character, manager.GetGameTableManager(), manager.assetManager));
                                OnCoordinator(() => { actors.Add(new(participant, prepared)); PrepareNext(); });
                            }
                            catch
                            {
                                if (prepared.Wallet == null) prepared.CancelPreparation();
                                OnCoordinator(() => { if (prepared.Wallet != null) actors.Add(new(participant, prepared)); Abort(); });
                            }
                        }, () => OnCoordinator(Abort));
                    }
                    catch { OnCoordinator(Abort); }
                });
            }
            private void Execute()
            {
                try
                {
                    List<CommodityPostingItem> items = [];
                    List<CommodityPostingMaterial> materials = [];
                    foreach (var actor in actors)
                    {
                        var inventory = actor.Prepared.CommodityDelivery;
                        if (inventory == null) continue;
                        items.AddRange(inventory.PostingItems);
                        if (inventory.Material != null) materials.Add(new(actor.Prepared.Character, inventory.Material));
                        if (inventory.ByMail)
                        {
                            foreach (var delivery in inventory.PostingMailItems)
                            {
                                var header = delivery.Returned ? MarketplaceMailDelivery.CreateCommodityReturn(actor.Prepared.Character, manager.GetGameTableManager(), manager.assetManager)
                                    : MarketplaceMailDelivery.CreateCommodityFill(actor.Prepared.Character, manager.GetGameTableManager(), manager.assetManager);
                                mails.Add(new(header, [delivery.Id]));
                            }
                        }
                    }
                    var info = manager.itemManager?.GetItemInfo(quote.Incoming.Definition) ?? throw new InvalidOperationException("Commodity posting lost its Item definition.");
                    foreach (var fill in quote.Fills.Where(row => actors.All(actor => actor.Prepared.Character != row.Buy.Owner)))
                    {
                        var item = new Item(fill.Buy.Owner, info, fill.Quantity, itemManager: manager.itemManager, gameTableManager: manager.GetGameTableManager());
                        var stored = BidItem.Capture(item.CaptureRewardModel()); items.Add(new(null, stored, 0, fill.Quantity));
                        mails.Add(new(MarketplaceMailDelivery.CreateCommodityFill(fill.Buy.Owner, manager.GetGameTableManager(), manager.assetManager), [item.Guid]));
                    }
                    foreach (var credit in quote.Credits.Where(row => actors.All(actor => actor.Prepared.Character != row.Character)))
                        mails.Add(new(MarketplaceMailDelivery.CreateNormalBidRefund(credit.Character, credit.Amount, manager.GetGameTableManager(), manager.assetManager), []));
                    command = new(database.CreateContext, route, id, DateTime.UtcNow, quote, characters,
                        actors.Select(row => row.Prepared.Wallet.Snapshot), items, materials, mails, info.IsStackable() ? info.Entry.MaxStackCount : 1);
                    manager.settlementEvents.EnqueueThreadSafe(new TaskGenericEvent<BidOutcome>(Task.Run(() => manager.commodityPostingExecution == null
                        ? command.ExecuteAsync() : manager.commodityPostingExecution(command)), Settle, Wait));
                }
                catch { Abort(); }
            }
            private void Wait() => manager.settlementEvents.EnqueueThreadSafe(new DelayEvent(TimeSpan.FromSeconds(1), () =>
                manager.settlementEvents.EnqueueThreadSafe(new TaskGenericEvent<BidOutcome>(Task.Run(command.ResolveAsync), Settle, Wait))));
            private void Settle(BidOutcome result)
            {
                outcome = result;
                manager.settlementEvents.EnqueueThreadSafe(new TaskGenericEvent<CommodityPostingView>(Task.Run(command.ReadReconciliationAsync), current =>
                {
                    view = current; outcome = current.Outcome;
                    lock (manager.syncRoot)
                    {
                        var ids = quote.Orders.Select(row => (row.Before ?? row.After ?? quote.Incoming).Id).ToArray();
                        foreach (var old in manager.commodityOrders.Where(row => ids.Contains(row.Order.CommodityOrderId)).ToArray()) manager.RemoveCommodityOrderRecord(old);
                        foreach (var row in current.Orders.Where(row => row != null)) manager.AddCommodityOrder(new()
                        {
                            OwnerCharacterId = row.Owner, Order = ToCommodityOrder(row.Model()), RequiresStoredRow = true,
                            HasStoredOutcome = true, StoredRow = row, StoredDatabase = route
                        });
                    }
                    Reconcile();
                }, () => manager.settlementEvents.EnqueueThreadSafe(new DelayEvent(TimeSpan.FromSeconds(1), () => Settle(result)))));
            }
            private void Abort()
            {
                if (aborting) return; aborting = true; failed = true; Reconcile();
            }
            private void Reconcile()
            {
                if (actors.Count == 0) { EndReconciliation(); return; }
                foreach (var actor in actors)
                    OnActor(actor.Owner, () =>
                    {
                        try
                        {
                            actor.Prepared.Reconcile(outcome?.Committed == true, id, command?.PayloadSHA256
                                ?? Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes($"aborted-posting:{id}"))),
                                () => OnCoordinator(() => ActorEnded(actor, true)), () => OnCoordinator(() => ActorEnded(actor, false)),
                                view?.Wallets.GetValueOrDefault(actor.Prepared.Character), publicationAllowed: () => CanPublish(actor),
                                commodityCurrent: view?.Inventories.GetValueOrDefault(actor.Prepared.Character));
                        }
                        catch { OnCoordinator(() => ActorEnded(actor, false)); }
                    });
            }
            private void ActorEnded(Actor actor, bool success)
            {
                if (!reconciled.Add(actor)) return; failed |= !success; reconciliationFailed |= !success;
                if (reconciled.Count == actors.Count) EndReconciliation();
            }
            private void EndReconciliation()
            {
                if (failed || outcome?.Committed != true) { Finish(aborting && !reconciliationFailed ? preparationError : GenericError.DbFailure); return; }
                PublishNext(0);
            }
            private void PublishNext(int index)
            {
                if (index == actors.Count) { Finish(GenericError.Ok); return; }
                var actor = actors[index];
                OnActor(actor.Owner, () =>
                {
                    try
                    {
                        if (CanPublish(actor))
                        {
                            foreach (var mail in mails.Where(row => row.Header.RecipientId == actor.Prepared.Character && row.Items.Count != 0))
                            {
                                if (!CanPublish(actor)) break;
                                var model = mail.Header.Model();
                                foreach (ulong key in mail.Items)
                                {
                                    var item = actor.Prepared.CommodityDelivery.PostingItems.Single(row => row.Id == key).After;
                                    model.Attachment.Add(new() { Id = model.Id, Index = (byte)model.Attachment.Count, ItemGuid = key, Item = item.Model() });
                                }
                                actor.Prepared.PublishDeliveryMail(new MailItem(model, itemManager: manager.itemManager, gameTableManager: manager.GetGameTableManager()));
                            }
                            foreach (var fill in quote.Fills)
                                foreach (var order in new[] { fill.Buy, fill.Sell }.Where(row => row.Owner == actor.Prepared.Character))
                                    if (CanPublish(actor)) actor.Owner.Session.EnqueueMessageEncrypted(new ServerCommodityAuctionFilledPartial { OrderFilled = ToCommodityOrder(order.Model()), Type = AuctionEventType.Fill });
                        }
                    }
                    finally { OnCoordinator(() => PublishNext(index + 1)); }
                });
            }
            private void Finish(GenericError result)
            {
                if (finished) return; finished = true;
                lock (manager.syncRoot)
                {
                    if (!manager.commodityPostings.TryGetValue(quote.Incoming.Definition, out var current) || !ReferenceEquals(current, this))
                        throw new InvalidOperationException("Commodity posting cannot release another order-book owner.");
                    manager.commodityPostings.Remove(quote.Incoming.Definition);
                }
                admission?.Dispose(); admission = null;
                OnActor(initiator, () =>
                {
                    if (CanPublish(initiator) && !actors.Any(row => ReferenceEquals(row.Owner.Player, initiator.Player) && row.Prepared.Retired))
                        reply(result, ToCommodityOrder((quote.Incoming.Immediate ? quote.Remainder : quote.Incoming).Model()));
                });
            }
        }
    }
}
