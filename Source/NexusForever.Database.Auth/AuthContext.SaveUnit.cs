using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Storage;
using NexusForever.Database.Auth.Model;

namespace NexusForever.Database.Auth
{
    public partial class AuthContext
    {
        private readonly List<Action<AuthContext>> saveWork = [];
        private readonly List<Action<bool>> saveCompletions = [];
        private readonly List<Func<bool>> commitGuards = [];
        private bool workStaged;
        private bool owningSaveUnit;
        private bool savepointRollbackObserved;
        private uint? pendingSaveAccount;
        private string saveUnitKey;
        private AccountPendingItemDeliveryModel saveUnitReceipt;
        private bool saveUnitRejected;

        public bool IsSaveUnitRejected => saveUnitRejected;
        public bool HasSavepointRollback => savepointRollbackObserved;

        static AuthContext()
        {
            DiagnosticListener.AllListeners.Subscribe(new TransactionDisposalObserver());
        }

        /// <summary>
        /// Stage work inside the save transaction, before tracked changes are flushed.
        /// Work may run again after a provider retry and must replace its previous staging.
        /// Completion follows the actual outer transaction, including rollback/disposal.
        /// </summary>
        public void EnlistSaveWork(Action<AuthContext> stage, Action<bool> completed, Func<bool> commitAllowed = null, uint? accountId = null)
        {
            ArgumentNullException.ThrowIfNull(stage);
            ArgumentNullException.ThrowIfNull(completed);
            if (workStaged)
                throw new InvalidOperationException("Cannot enlist additional work after this save unit has been staged.");
            if (accountId.HasValue)
            {
                if (accountId == 0 || pendingSaveAccount.HasValue && pendingSaveAccount != accountId)
                    throw new InvalidOperationException("An Auth save unit must retain one account owner.");
                // A non-inventory producer also needs a whole-unit witness when
                // the execution strategy retries after an ambiguous commit.
                pendingSaveAccount = accountId;
            }
            saveUnitKey ??= $"save-unit:{Guid.NewGuid():N}";
            saveWork.Add(stage);
            saveCompletions.Add(completed);
            if (commitAllowed != null) commitGuards.Add(commitAllowed);
        }

        private void ValidateCommit()
        {
            ValidateSaveUnit();
            if (commitGuards.Any(guard => !guard()))
                throw new InvalidOperationException("A staged Auth operation was canceled before commit.");
        }

        /// <summary>
        /// Permanently reject this context after conflicting owner intent. Existing
        /// outer transactions cannot commit partial work; retries require a fresh context.
        /// </summary>
        public void RejectSaveUnit() => saveUnitRejected = true;

        private void ValidateSaveUnit()
        {
            if (saveUnitRejected)
                throw new InvalidOperationException("This Auth save context was rejected; retry with a fresh context.");
        }

        private void StageSaveWork()
        {
            if (saveUnitReceipt != null)
                Entry(saveUnitReceipt).State = EntityState.Detached;
            foreach (Action<AuthContext> work in saveWork)
                work(this);
            if (owningSaveUnit && pendingSaveAccount.HasValue)
            {
                // Pending ID zero is reserved for a unit commit witness, never a
                // client item. It also verifies unrelated tracked changes committed
                // with the unit before an ambiguous provider retry can replay them.
                saveUnitReceipt = new AccountPendingItemDeliveryModel
                {
                    AccountId = pendingSaveAccount.Value, DeliveryKey = saveUnitKey, PendingItemId = 0
                };
                AccountPendingItemDelivery.Add(saveUnitReceipt);
            }
            workStaged = true;
        }

        private bool SaveUnitAlreadyCommitted() => pendingSaveAccount.HasValue &&
            AccountPendingItemDelivery.AsNoTracking().Any(row => row.AccountId == pendingSaveAccount.Value
                && row.DeliveryKey == saveUnitKey && row.PendingItemId == 0);

        private bool ConfirmOwnedCommit(Func<bool> ownsProof)
        {
            // Use a new transaction on the original route. The owner lock waits
            // for the original writer before deciding whether its receipt exists.
            using IDbContextTransaction proof = Database.BeginTransaction(IsolationLevel.Serializable);
            uint accountId = pendingSaveAccount.Value;
            bool ownerExists = Database.IsSqlite()
                ? Account.AsNoTracking().Any(row => row.Id == accountId)
                : Account.FromSqlInterpolated($"SELECT * FROM account WHERE id = {accountId} FOR UPDATE").AsNoTracking().AsEnumerable().Any();
            if (!ownerExists || !ownsProof())
                throw new AuthCommitOutcomeUnknownException(new InvalidOperationException("The Auth save owner changed."));

            AccountPendingItemDeliveryModel receipt = AccountPendingItemDelivery.AsNoTracking()
                .SingleOrDefault(row => row.AccountId == accountId && row.DeliveryKey == saveUnitKey);
            if (receipt == null)
                return false;
            if (receipt.PendingItemId != 0)
                throw new AuthCommitOutcomeUnknownException(new InvalidOperationException("The Auth save receipt has a conflicting item identity."));

            // Existing guards also check whether the committed rows can still
            // be adopted by the live owner after the acknowledgement was lost.
            try { ValidateCommit(); }
            catch (Exception error) { throw new AuthCommitOutcomeUnknownException(error); }
            return true;
        }

        private void CompleteSaveUnit(bool committed)
        {
            if (owningSaveUnit)
                return;
            Action<bool>[] callbacks = saveCompletions.ToArray();
            saveCompletions.Clear();
            saveWork.Clear();
            commitGuards.Clear();
            workStaged = false;
            committed &= !savepointRollbackObserved;
            savepointRollbackObserved = false;
            if (saveUnitReceipt != null)
            {
                try { Entry(saveUnitReceipt).State = EntityState.Detached; }
                catch (ObjectDisposedException) { }
            }
            pendingSaveAccount = null;
            saveUnitKey = null;
            saveUnitReceipt = null;
            foreach (Action<bool> callback in callbacks)
                callback(committed);
        }

        public override int SaveChanges(bool acceptAllChangesOnSuccess)
            => SaveChanges(acceptAllChangesOnSuccess, ownsProof: null);

        /// <summary>Confirm an ambiguous owned save with its exact receipt before completing live owners.</summary>
        public int SaveChangesConfirmingCommit(Func<bool> ownsProof)
        {
            ArgumentNullException.ThrowIfNull(ownsProof);
            if (!pendingSaveAccount.HasValue || Database.CurrentTransaction != null ||
                (!Database.IsSqlite() && Database.ProviderName?.Contains("MySql", StringComparison.OrdinalIgnoreCase) != true))
                throw new InvalidOperationException("Commit confirmation requires an owned SQLite or MySQL account save.");
            return SaveChanges(acceptAllChangesOnSuccess: true, ownsProof);
        }

        private int SaveChanges(bool acceptAllChangesOnSuccess, Func<bool> ownsProof)
        {
            ValidateSaveUnit();
            if (saveWork.Count == 0)
                return base.SaveChanges(acceptAllChangesOnSuccess);
            try
            {
                if (Database.CurrentTransaction != null || !Database.IsRelational())
                {
                    if (!workStaged)
                        StageSaveWork();
                    int result = base.SaveChanges(acceptAllChangesOnSuccess);
                    if (Database.CurrentTransaction == null)
                        CompleteSaveUnit(true);
                    return result;
                }

                owningSaveUnit = true;
                int stagedResult = 0;
                int saved;
                bool commitAttempted = false;
                bool confirmCommit = ownsProof != null;
                try
                {
                    saved = Database.CreateExecutionStrategy().Execute(() =>
                    {
                        if (confirmCommit ? commitAttempted && ConfirmOwnedCommit(ownsProof) : SaveUnitAlreadyCommitted())
                            return stagedResult;
                        using IDbContextTransaction transaction = Database.BeginTransaction();
                        StageSaveWork();
                        stagedResult = base.SaveChanges(false);
                        if (confirmCommit && !SaveUnitAlreadyCommitted())
                            throw new InvalidOperationException("The Auth save is missing its commit receipt.");
                        commitAttempted = true;
                        transaction.Commit();
                        return stagedResult;
                    });
                }
                catch (Exception error) when (confirmCommit && commitAttempted && error is not AuthCommitOutcomeUnknownException)
                {
                    bool confirmed;
                    try { confirmed = Database.CreateExecutionStrategy().Execute(() => ConfirmOwnedCommit(ownsProof)); }
                    catch (Exception proofError)
                    {
                        throw new AuthCommitOutcomeUnknownException(new AggregateException(error, proofError));
                    }
                    if (!confirmed)
                        throw;
                    saved = stagedResult;
                }
                try
                {
                    if (acceptAllChangesOnSuccess)
                        ChangeTracker.AcceptAllChanges();
                    owningSaveUnit = false;
                    CompleteSaveUnit(true);
                }
                catch (Exception error) when (confirmCommit)
                {
                    throw new AuthCommitOutcomeUnknownException(error);
                }
                return saved;
            }
            catch
            {
                owningSaveUnit = false;
                if (Database.CurrentTransaction == null)
                    CompleteSaveUnit(false);
                throw;
            }
        }

        public override async Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
        {
            ValidateSaveUnit();
            if (saveWork.Count == 0)
                return await base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
            try
            {
                if (Database.CurrentTransaction != null || !Database.IsRelational())
                {
                    if (!workStaged)
                        StageSaveWork();
                    int result = await base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
                    if (Database.CurrentTransaction == null)
                        CompleteSaveUnit(true);
                    return result;
                }

                owningSaveUnit = true;
                int stagedResult = 0;
                int saved = await Database.CreateExecutionStrategy().ExecuteAsync(async () =>
                {
                    if (SaveUnitAlreadyCommitted())
                        return stagedResult;
                    await using IDbContextTransaction transaction = await Database.BeginTransactionAsync(cancellationToken);
                    StageSaveWork();
                    stagedResult = await base.SaveChangesAsync(false, cancellationToken);
                    await transaction.CommitAsync(cancellationToken);
                    return stagedResult;
                });
                if (acceptAllChangesOnSuccess)
                    ChangeTracker.AcceptAllChanges();
                owningSaveUnit = false;
                CompleteSaveUnit(true);
                return saved;
            }
            catch
            {
                owningSaveUnit = false;
                if (Database.CurrentTransaction == null)
                    CompleteSaveUnit(false);
                throw;
            }
        }

        public override void Dispose()
        {
            try { base.Dispose(); }
            finally { CompleteSaveUnit(false); }
        }

        public override async ValueTask DisposeAsync()
        {
            try { await base.DisposeAsync(); }
            finally { CompleteSaveUnit(false); }
        }

        private sealed class SaveUnitInterceptor : DbTransactionInterceptor
        {
            public static SaveUnitInterceptor Instance { get; } = new();
            public override InterceptionResult TransactionCommitting(DbTransaction transaction, TransactionEventData eventData, InterceptionResult result)
            {
                (eventData.Context as AuthContext)?.ValidateCommit();
                return result;
            }
            public override ValueTask<InterceptionResult> TransactionCommittingAsync(DbTransaction transaction, TransactionEventData eventData, InterceptionResult result, CancellationToken cancellationToken = default)
                => ValueTask.FromResult(TransactionCommitting(transaction, eventData, result));
            public override void TransactionCommitted(DbTransaction transaction, TransactionEndEventData eventData)
                => (eventData.Context as AuthContext)?.CompleteSaveUnit(true);
            public override Task TransactionCommittedAsync(DbTransaction transaction, TransactionEndEventData eventData, CancellationToken cancellationToken = default)
            {
                TransactionCommitted(transaction, eventData);
                return Task.CompletedTask;
            }
            public override void TransactionRolledBack(DbTransaction transaction, TransactionEndEventData eventData)
                => (eventData.Context as AuthContext)?.CompleteSaveUnit(false);
            public override Task TransactionRolledBackAsync(DbTransaction transaction, TransactionEndEventData eventData, CancellationToken cancellationToken = default)
            {
                TransactionRolledBack(transaction, eventData);
                return Task.CompletedTask;
            }
            public override void RolledBackToSavepoint(DbTransaction transaction, TransactionEventData eventData)
            {
                if (eventData.Context is AuthContext context && context.saveCompletions.Count != 0 && !context.owningSaveUnit)
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
                    listener.Subscribe(this, name => name == RelationalEventId.TransactionDisposed.Name);
            }
            public void OnNext(KeyValuePair<string, object> item)
            {
                if (item.Value is TransactionEventData { Context: AuthContext context })
                    context.CompleteSaveUnit(false);
            }
            public void OnCompleted() { }
            public void OnError(Exception error) { }
        }
    }
}
