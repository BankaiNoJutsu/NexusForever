using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace NexusForever.Database.Character
{
    public partial class CharacterContext
    {
        private readonly List<Action<bool>> saveCompletions = [];
        private readonly List<Action<bool>> saveSettlements = [];
        private bool savepointRollbackObserved;
        private bool saveInProgress;
        private bool? deferredTerminal;
        private object recoverableSaveOwner;
        internal bool RecoverableSaveConfirmed { get; private set; }
        public bool IsProvenBidRollback { get; private set; }

        internal void RetainUncertainSettlement(object owner, bool beforeEnrollment = false)
        {
            if (owner == null || recoverableSaveOwner != null || RecoverableSaveConfirmed
                || (beforeEnrollment ? !AtomicCharacterId.HasValue || Database.CurrentTransaction == null || AtomicGraphSealed : !AtomicGraphSealed))
                throw new InvalidOperationException("Recoverable Character settlement lost its atomic transaction owner.");
            recoverableSaveOwner = owner;
        }

        internal void ResolveUncertainSettlement(object owner, bool committed)
        {
            if (!ReferenceEquals(recoverableSaveOwner, owner) || saveInProgress || committed && HasSavepointRollback)
                throw new InvalidOperationException("Recoverable Character settlement has conflicting evidence.");
            recoverableSaveOwner = null; RecoverableSaveConfirmed = committed; IsProvenBidRollback = !committed;
            CompleteSave(committed);
        }

        /// <summary>
        /// Staged owner state is uncertain after a savepoint rollback. Owners must
        /// wait for terminal completion and reconcile through a fresh context.
        /// </summary>
        public bool HasSavepointRollback => savepointRollbackObserved;

        static CharacterContext()
        {
            // Transaction disposal has a DiagnosticSource event but no interceptor hook.
            DiagnosticListener.AllListeners.Subscribe(new TransactionDisposalObserver());
        }

        /// <summary>
        /// Observe completion of staged work, including caller-owned transactions.
        /// Disposal without a commit reports failure. Savepoint rollback conservatively
        /// retains idempotent owner intent, even when an outer transaction later commits.
        /// </summary>
        public void OnSaveCompleted(Action<bool> callback) => saveCompletions.Add(callback);

        /// <summary>
        /// Observe the terminal outcome after all normal state owners have settled.
        /// Register before saving. Aggregate publication must not depend on the
        /// relative registration order of Item/Challenge completion callbacks.
        /// </summary>
        public void OnSaveSettled(Action<bool> callback)
        {
            ArgumentNullException.ThrowIfNull(callback);
            saveSettlements.Add(callback);
        }

        private void CompleteSave(bool committed)
        {
            if (saveInProgress)
            {
                // EF can commit and dispose its implicit transaction before it
                // accepts tracked changes. Preserve the commit until base returns.
                deferredTerminal = deferredTerminal.GetValueOrDefault() || committed;
                return;
            }

            // Only the bid reconciliation executor may defer a negative event.
            // It ends the physical transaction, then probes its full-graph receipt
            // through a fresh context before releasing the original row owners.
            if (recoverableSaveOwner != null)
            {
                if (!committed || savepointRollbackObserved) return;
                RecoverableSaveConfirmed = true; recoverableSaveOwner = null;
            }

            Action<bool>[] callbacks = saveCompletions.ToArray();
            Action<bool>[] settlements = saveSettlements.ToArray();
            saveCompletions.Clear();
            saveSettlements.Clear();
            saveValidations.Clear();
            committed &= !savepointRollbackObserved;
            savepointRollbackObserved = false;
            foreach (Action<bool> callback in callbacks.Concat(settlements))
            {
                try { callback(committed); }
                catch (Exception ex)
                {
                    // SQL has already reached its terminal outcome. Observer failures
                    // must not change that outcome or strand later owners' save leases.
                    try { NLog.LogManager.GetCurrentClassLogger().Warn(ex, "Character save completion observer failed (committed: {0}).", committed); }
                    catch { /* Logging must not prevent the remaining owners from settling. */ }
                }
            }
        }

        private void CompleteAfterSave(bool succeeded)
        {
            if (deferredTerminal.HasValue)
            {
                bool committed = deferredTerminal.Value;
                deferredTerminal = null;
                CompleteSave(committed);
            }
            else if (Database.CurrentTransaction == null)
                CompleteSave(succeeded);
        }

        public override int SaveChanges(bool acceptAllChangesOnSuccess)
        {
            saveInProgress = true;
            try
            {
                ThrowIfSaveRejected();
                int result = base.SaveChanges(acceptAllChangesOnSuccess);
                saveInProgress = false;
                CompleteAfterSave(true);
                return result;
            }
            catch
            {
                saveInProgress = false;
                CompleteAfterSave(false);
                throw;
            }
        }

        public override async Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
        {
            saveInProgress = true;
            try
            {
                ThrowIfSaveRejected();
                int result = await base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
                saveInProgress = false;
                CompleteAfterSave(true);
                return result;
            }
            catch
            {
                saveInProgress = false;
                CompleteAfterSave(false);
                throw;
            }
        }

        public override void Dispose()
        {
            try { base.Dispose(); }
            finally { CompleteSave(false); }
        }

        public override async ValueTask DisposeAsync()
        {
            try { await base.DisposeAsync(); }
            finally { CompleteSave(false); }
        }

        private sealed class SaveCompletionInterceptor : DbTransactionInterceptor
        {
            public static SaveCompletionInterceptor Instance { get; } = new();
            public override InterceptionResult TransactionCommitting(DbTransaction transaction, TransactionEventData eventData, InterceptionResult result)
            {
                ((CharacterContext)eventData.Context)?.ValidateSaveUnit(true);
                return result;
            }
            public override ValueTask<InterceptionResult> TransactionCommittingAsync(DbTransaction transaction, TransactionEventData eventData, InterceptionResult result, CancellationToken cancellationToken = default)
                => new(TransactionCommitting(transaction, eventData, result));
            public override void TransactionCommitted(DbTransaction transaction, TransactionEndEventData eventData)
                => ((CharacterContext)eventData.Context)?.CompleteSave(true);
            public override Task TransactionCommittedAsync(DbTransaction transaction, TransactionEndEventData eventData, CancellationToken cancellationToken = default)
            {
                TransactionCommitted(transaction, eventData);
                return Task.CompletedTask;
            }
            public override void TransactionRolledBack(DbTransaction transaction, TransactionEndEventData eventData)
                => ((CharacterContext)eventData.Context)?.CompleteSave(false);
            public override Task TransactionRolledBackAsync(DbTransaction transaction, TransactionEndEventData eventData, CancellationToken cancellationToken = default)
            {
                TransactionRolledBack(transaction, eventData);
                return Task.CompletedTask;
            }
            public override void RolledBackToSavepoint(DbTransaction transaction, TransactionEventData eventData)
            {
                if (eventData.Context is CharacterContext context && (context.saveCompletions.Count != 0 || context.saveSettlements.Count != 0))
                    context.savepointRollbackObserved = true;
            }
            public override Task RolledBackToSavepointAsync(DbTransaction transaction, TransactionEventData eventData, CancellationToken cancellationToken = default)
            {
                RolledBackToSavepoint(transaction, eventData);
                return Task.CompletedTask;
            }
        }

        private sealed class TransactionDisposalObserver : IObserver<DiagnosticListener>, IObserver<KeyValuePair<string, object>>
        {
            public void OnNext(DiagnosticListener listener)
            {
                if (listener.Name == "Microsoft.EntityFrameworkCore")
                    listener.Subscribe(this, eventName => eventName == RelationalEventId.TransactionDisposed.Name);
            }
            public void OnNext(KeyValuePair<string, object> item)
            {
                if (item.Value is TransactionEventData { Context: CharacterContext context })
                    context.CompleteSave(false);
            }
            public void OnCompleted() { }
            public void OnError(Exception error) { }
        }
    }
}
