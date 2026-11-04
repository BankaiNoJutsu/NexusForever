using NexusForever.Game.Abstract.Account;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Quest;
using NexusForever.Game.RealmBank;
using NexusForever.Game.Static.Entity;
using NexusForever.Network.Message;
using NexusForever.Network.Session;
using NexusForever.Network.World.Message.Model;
using NexusForever.Network.World.Message.Static;
using NexusForever.Shared;
using NexusForever.Shared.Game.Events;

namespace NexusForever.Game.Entity
{
    public partial class Inventory
    {
        private BankPublication realmBankNotifications;
        private bool realmBankPublishing;
        private HashSet<BankPublication> bankPublications;

        private BankPublication BeginRealmBankNotifications(bool needed)
        {
            if (!needed || realmBankNotifications != null || player == null || realmBankManager == null) return null;
            if (IsRealmBankPersistenceBlocked || inventorySaveLeases != 0 || HasPendingRewardDelivery || RequiresAuthoritativeReload
                || bags.Values.SelectMany(bag => bag).OfType<Item>().Any(item => item.HasPendingSave))
                throw new InvalidOperationException("A bank operation is already owned.");
            var batch = new BankPublication(this);
            if (!batch.CurrentActor()) throw new InvalidOperationException("Bank operation lost its live actor.");
            realmBankNotifications = batch;
            return batch;
        }

        private void EnqueueItemMessage(IWritable message)
        {
            if (realmBankNotifications != null) realmBankNotifications.Add(message);
            else if (!BufferCommodityMessage(message))
            {
                IGameSession session = player.Session;
                if (CanPublishToInventorySession(session))
                    session.EnqueueMessageEncrypted(message);
            }
        }

        private bool CanPublishToInventorySession(IGameSession session)
            => session is not IPlayerSession bound || (ReferenceEquals(bound.Player, player) && ReferenceEquals(player.Inventory, this));

        private void ApplyInventoryEffect(Action effect)
        {
            if (realmBankNotifications != null) realmBankNotifications.Add(effect);
            else effect();
        }

        private void AfterBankCommit(Action effect)
        {
            if (realmBankNotifications != null) realmBankNotifications.After(effect);
            else effect();
        }

        private void RefreshCollectedItem(uint id) => AfterBankCommit(() =>
        {
            if (CanPublishToInventorySession(player?.Session))
                InventoryQuestObjectiveUpdater.RefreshCollectItemForItem(player, id);
        });

        private sealed class BankPublication : IDisposable
        {
            private readonly Inventory owner;
            private readonly IGameSession session;
            private readonly IAccount account;
            private readonly uint accountId;
            private readonly ushort realm;
            private readonly EventQueue events;
            private readonly InventoryOperationSnapshot before;
            private readonly Dictionary<ulong, IItem> items = [];
            private readonly List<Item> splits = [];
            private readonly HashSet<ulong> detached = [];
            private IWritable[] failures = [new ServerItemError { ErrorCode = GenericError.DbFailure }];
            private readonly List<Action> publication = [];
            private readonly List<Action> afterPublication = [];
            private readonly List<Action> afterSavePublication = [];
            private readonly List<IBankSaveCompanion> companions = [];
            private readonly TaskCompletionSource published = new(TaskCreationOptions.RunContinuationsAsynchronously);
            private bool ready, disposed, finished;
            private Task saved;
            private uint appliedExpiration;
            private Func<bool> additionalActor;
            private readonly Player.BankActor bankActor;
            private InventoryOperationSnapshot prepared;
            private Task<RealmBankManager.BankOutcome> outcome;
            private readonly HashSet<IBankSaveCompanion> released = [];

            internal BankPublication(Inventory owner)
            {
                this.owner = owner; session = owner.player.Session; account = owner.player.Account;
                accountId = account.Id; realm = owner.player.Identity.RealmId;
                events = session?.Events ?? throw new InvalidOperationException("Bank publication requires the session actor queue.");
                before = owner.CreateOperationSnapshot();
                if (owner.player is Player player) bankActor = player.BeginBankReconciliation(this, owner);
            }

            internal bool CurrentActor() => bankActor != null ? !owner.rewardOwnerInvalidated && bankActor.Owns() && (additionalActor?.Invoke() ?? true)
                : !owner.rewardOwnerInvalidated && ReferenceEquals(owner.player.Inventory, owner)
                && ReferenceEquals(owner.player.Session, session) && ReferenceEquals(owner.player.Account, account)
                && ReferenceEquals(account.Session, session) && account.Id == accountId
                && owner.player.CharacterId == owner.characterId && owner.player.Identity.RealmId == realm
                && session is not NetworkSession { IsDisconnecting: true }
                && owner.player.LogoutManager?.State is not (LogoutState.Logout or LogoutState.Finished)
                && (additionalActor?.Invoke() ?? true);

            internal void RequireActor(Func<bool> actor) => additionalActor = actor ?? throw new ArgumentNullException(nameof(actor));

            internal void Add(IWritable packet) => publication.Add(() => session.EnqueueMessageEncrypted(packet));
            internal void Add(Action effect) => publication.Add(effect);
            internal void After(Action effect) => afterPublication.Add(effect);
            internal void AfterSave(Action effect) => afterSavePublication.Add(effect);
            internal void Attach(IBankSaveCompanion companion) => companions.Add(companion);
            internal void Stage(IItem item, Item split = null)
            {
                if (items.TryGetValue(item.Guid, out IItem prior) && !ReferenceEquals(prior, item))
                    throw new InvalidOperationException("Bank operation reused an Item identity.");
                items[item.Guid] = item;
                if (split != null && !splits.Contains(split)) splits.Add(split);
            }
            internal void StageDetached(Item item) { Stage(item, item.PendingCreate ? item : null); detached.Add(item.Guid); }
            internal void OnFailure(IWritable packet) => failures = [packet];
            internal void OnFailure(IEnumerable<IWritable> packets) => failures = packets.ToArray();
            private void PublishFailure()
            {
                foreach (var packet in failures)
                {
                    if (!CurrentActor() || bankActor?.CanPublish == false) { owner.rewardRequiresReload = true; return; }
                    session.EnqueueMessageEncrypted(packet);
                }
            }
            internal void RetainPreparation(IItem item)
            {
                if (!before.Items.TryAdd(item.Guid, new ItemOperationSnapshot(item)))
                    throw new InvalidOperationException("Incoming Item already belongs to the inventory preparation snapshot.");
            }
            internal void Complete() => ready = true;
            internal void RetainExpirationOnAbort(uint elapsed) => appliedExpiration = elapsed;

            public void Dispose()
            {
                if (disposed) return;
                disposed = true;
                owner.realmBankNotifications = null;
                if (!ready || !CurrentActor())
                {
                    AbortCompanions();
                    // SQL has not started: discard the notification batch and
                    // restore the entire prepared inventory/deletion snapshot.
                    owner.RestoreOperationSnapshot(before);
                    owner.itemExpirationAccumulator += appliedExpiration;
                    if (!CurrentActor()) owner.rewardRequiresReload = true;
                    bankActor?.End();
                    return;
                }
                if (items.Count == 0 && !companions.Any(companion => companion.CanExecuteWithoutItems))
                {
                    if (companions.Count != 0)
                    {
                        AbortCompanions(); owner.RestoreOperationSnapshot(before);
                        throw new InvalidOperationException("A bank companion requires its complete Item unit.");
                    }
                    foreach (Action effect in publication) effect();
                    foreach (Action effect in afterPublication.Concat(afterSavePublication)) effect();
                    bankActor?.End();
                    return;
                }
                if (bankActor != null)
                {
                    prepared = owner.CreateOperationSnapshot();
                    foreach (var item in items.Values) prepared.Items.TryAdd(item.Guid, new ItemOperationSnapshot(item));
                    try { outcome = owner.realmBankManager.SaveReconciledInventoryOperationAsync(items.Values.ToArray(), splits.ToArray(), owner.player,
                        context => { foreach (var companion in companions) companion.Stage(context); }, detached, companions.Any(companion => companion.CanExecuteWithoutItems)); }
                    catch (Exception error) { outcome = Task.FromException<RealmBankManager.BankOutcome>(error); }
                    saved = outcome;
                    owner.realmBankMoveSave = Task.WhenAll(saved, published.Task);
                    (owner.bankPublications ??= []).Add(this);
                    events.EnqueueEvent(new TaskEvent(saved, Reconcile, FailReconciliation));
                    _ = saved.ContinueWith(_ => { lock (owner.MutationGate) if (!CurrentActor()) Abandon(); },
                        CancellationToken.None, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);
                    return;
                }
                Task write;
                try { write = owner.realmBankManager.SaveInventoryOperationAsync(items.Values.ToArray(), splits.ToArray(), owner.player,
                    context => { foreach (var companion in companions) companion.Stage(context); }, detached, companions.Any(companion => companion.CanExecuteWithoutItems)); }
                catch (Exception error) { write = Task.FromException(error); }
                IItem first = items.Values.FirstOrDefault();
                saved = owner.ObserveRealmBankPersistence(write, first, first);
                owner.realmBankMoveSave = Task.WhenAll(saved, published.Task);
                owner.realmBankMoveSave.FireAndForgetAsync(_ => owner.realmBankSaveFailed = true);
                (owner.bankPublications ??= []).Add(this);
                events.EnqueueEvent(new TaskEvent(saved, () => Finish(true), () => Finish(false)));
                // A finalized actor may never poll its queue again. This tail
                // releases only terminal bookkeeping; it cannot publish packets.
                _ = saved.ContinueWith(_ =>
                {
                    lock (owner.MutationGate)
                        if (!CurrentActor()) Abandon();
                }, CancellationToken.None, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);
            }

            internal void Abandon()
            {
                owner.rewardRequiresReload = true;
                QuarantineCompanions();
                if (saved?.IsCompleted != true) return;
                bankActor?.Quarantine();
                finished = true;
                owner.bankPublications?.Remove(this);
                published.TrySetResult();
            }

            private void Reconcile()
            {
                lock (owner.MutationGate)
                {
                    if (finished) return;
                    try
                    {
                        if (!CurrentActor()) { Abandon(); return; }
                        var result = outcome.GetAwaiter().GetResult();
                        var payment = companions.OfType<CurrencyManager.BankCurrencyReservation>().SingleOrDefault();
                        bankActor.Reconcile(result, payment, () =>
                        {
                            if (!result.Committed)
                            {
                                owner.RestoreBankPreparation(before, prepared);
                                foreach (var companion in companions.Where(value => !ReferenceEquals(value, payment))) companion.Abort();
                                owner.itemExpirationAccumulator += appliedExpiration;
                            }
                            else
                            {
                                foreach (var companion in companions.Where(value => !ReferenceEquals(value, payment)))
                                {
                                    companion.Adopt(); if (companion.ReleaseForOrdinarySave()) released.Add(companion);
                                }
                                foreach (var effect in afterPublication) effect();
                            }
                        }, publish => CompleteReconciliation(result.Committed, publish, payment), FailReconciliation);
                    }
                    catch (Exception error) { log.Error(error, "Bank ordinary reconciliation could not retain its complete graph."); FailReconciliation(); }
                }
            }
            private void CompleteReconciliation(bool committed, bool publish, CurrencyManager.BankCurrencyReservation payment)
            {
                lock (owner.MutationGate)
                {
                    if (finished) return;
                    try
                    {
                        owner.realmBankPublishing = true;
                        if (publish)
                        {
                            if (committed) foreach (var effect in publication.Concat(afterSavePublication))
                            { if (!CurrentActor() || !bankActor.CanPublish) throw new InvalidOperationException("Bank publication lost its original actor."); effect(); }
                            else { payment?.Publish(); PublishFailure(); }
                        }
                        else payment?.SuppressPublication();
                        if (committed) foreach (var companion in companions.Where(value => !ReferenceEquals(value, payment) && !released.Contains(value))) companion.Complete();
                        payment?.Complete();
                        finished = true;
                    }
                    finally
                    {
                        owner.realmBankPublishing = false;
                        if (!finished) QuarantineCompanions();
                        owner.bankPublications?.Remove(this); published.TrySetResult();
                    }
                }
            }
            private void FailReconciliation()
            {
                lock (owner.MutationGate)
                {
                    if (finished) return;
                    bankActor?.EnterPublication();
                    try
                    {
                        if (CurrentActor() && bankActor?.CanPublish != false) try { PublishFailure(); } catch (Exception error) { log.Error(error, "Bank failure publication failed."); }
                        finished = true; owner.realmBankSaveFailed = true; QuarantineCompanions(); bankActor?.Quarantine();
                        owner.bankPublications?.Remove(this); published.TrySetResult();
                        if (session is IPlayerSession current && ReferenceEquals(current.Player, owner.player)) session.ForceDisconnect();
                    }
                    finally { bankActor?.LeavePublication(); }
                }
            }

            private void Finish(bool committed)
            {
                lock (owner.MutationGate)
                {
                    if (finished) return;
                    finished = true;
                    bool succeeded = false;
                    try
                    {
                        if (!CurrentActor()) { owner.rewardRequiresReload = true; return; }
                        owner.realmBankPublishing = true;
                        if (committed)
                        {
                            foreach (var companion in companions) companion.Adopt();
                            foreach (Action effect in publication)
                            {
                                if (!CurrentActor()) { owner.rewardRequiresReload = true; return; }
                                effect();
                            }
                            if (!CurrentActor()) { owner.rewardRequiresReload = true; return; }
                            foreach (var companion in companions) companion.Complete();
                            succeeded = true;
                        }
                        else PublishFailure();
                    }
                    catch (Exception error)
                    {
                        owner.realmBankSaveFailed = true;
                        log.Error(error, "Bank commit notification failed; inventory requires reload.");
                        if (CurrentActor())
                            try { PublishFailure(); }
                            catch (Exception failed) { log.Error(failed, "Bank failure notification also failed."); }
                    }
                    finally
                    {
                        if (!succeeded) QuarantineCompanions();
                        owner.realmBankPublishing = false;
                        owner.bankPublications?.Remove(this);
                        published.TrySetResult();
                        if (succeeded)
                            foreach (Action effect in afterPublication.Concat(afterSavePublication))
                                events.EnqueueEvent(new PredicateEvent(
                                    () => !CurrentActor() || owner.RequiresAuthoritativeReload || !owner.IsSaveBlocked,
                                    () =>
                                    {
                                        lock (owner.MutationGate)
                                        {
                                            if (!CurrentActor() || owner.RequiresAuthoritativeReload) return;
                                            try
                                            {
                                                effect();
                                                if (!CurrentActor()) owner.rewardRequiresReload = true;
                                            }
                                            catch (Exception error)
                                            {
                                                owner.rewardRequiresReload = true;
                                                log.Error(error, "Committed bank gameplay callback failed; owner requires reload.");
                                            }
                                        }
                                    }));
                    }
                }
            }

            private void AbortCompanions()
            {
                foreach (var companion in companions)
                    try { companion.Abort(); }
                    catch (Exception error)
                    {
                        owner.rewardRequiresReload = true;
                        try { companion.Quarantine(); } catch (Exception failed) { log.Error(failed, "Bank companion rollback quarantine failed."); }
                        log.Error(error, "Bank companion preparation rollback failed.");
                    }
            }
            private void QuarantineCompanions()
            {
                foreach (var companion in companions)
                    try { companion.Quarantine(); }
                    catch (Exception error) { owner.rewardRequiresReload = true; log.Error(error, "Bank companion quarantine failed."); }
            }
        }
    }
}
