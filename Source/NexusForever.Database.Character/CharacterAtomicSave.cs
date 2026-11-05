using System;
using System.Data;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace NexusForever.Database.Character
{
    public partial class CharacterContext
    {
        public ulong? AtomicCharacterId { get; private set; }
        internal bool AtomicGraphSealed { get; set; }
        internal void BindAtomicCharacter(ulong id)
        {
            if (id == 0 || AtomicCharacterId.HasValue || saveInProgress || IsSaveRejected || HasSavepointRollback
                || Database.CurrentTransaction != null || ChangeTracker.Entries().Any())
                throw new InvalidOperationException("Atomic Character save requires a fresh, single owner.");
            AtomicCharacterId = id;
        }
    }

    public static class CharacterAtomicSave
    {
        private static readonly object ownershipGate = new();
        private static readonly ConditionalWeakTable<CharacterContext, object> owners = new();
        /// <summary>
        /// Enroll actor callbacks synchronously once, then complete one whole
        /// transaction asynchronously. A failed/uncertain actor save reloads;
        /// execution strategies must never replay its gameplay callbacks.
        /// </summary>
        public static Task Execute(Func<CharacterContext> contexts, ulong characterId, Action<CharacterContext> enroll)
            => ExecuteCore(contexts, characterId, enroll, null);

        public static Task ExecuteBidReconciliation(Func<CharacterContext> contexts, ulong characterId,
            Action<CharacterContext> enroll, MarketplaceBidReconciliation receipt)
            => ExecuteReconciliation(contexts, characterId, enroll, receipt);

        public static Task ExecuteBankReconciliation(Func<CharacterContext> contexts, ulong characterId,
            Action<CharacterContext> enroll, BankSaveReconciliation receipt)
            => ExecuteReconciliation(contexts, characterId, enroll, receipt);

        public static Task ExecuteHousingRenameReconciliation(Func<CharacterContext> contexts, ulong characterId,
            Action<CharacterContext> enroll, HousingRenameSaveReconciliation receipt)
            => ExecuteReconciliation(contexts, characterId, enroll, receipt);

        public static Task ExecuteHousingDecorPurchaseReconciliation(Func<CharacterContext> contexts, ulong characterId,
            Action<CharacterContext> enroll, HousingDecorPurchaseSaveReconciliation receipt)
            => ExecuteReconciliation(contexts, characterId, enroll, receipt);

        public static Task ExecuteResourceConversionReconciliation(Func<CharacterContext> contexts, ulong characterId,
            Action<CharacterContext> enroll, ResourceConversionSaveReconciliation receipt)
            => ExecuteReconciliation(contexts, characterId, enroll, receipt);

        public static Task ExecutePublicEventQuestReconciliation(Func<CharacterContext> contexts, ulong characterId,
            Action<CharacterContext> enroll, PublicEventQuestSaveReconciliation receipt)
            => ExecuteReconciliation(contexts, characterId, enroll, receipt);

        public static Task ExecuteGuildCreationReconciliation(Func<CharacterContext> contexts, ulong characterId,
            Action<CharacterContext> enroll, GuildCreationSaveReconciliation receipt)
            => ExecuteReconciliation(contexts, characterId, enroll, receipt);

        public static Task ExecuteCommunityCreationReconciliation(Func<CharacterContext> contexts, ulong characterId,
            Action<CharacterContext> enroll, CommunityCreationSaveReconciliation receipt)
            => ExecuteReconciliation(contexts, characterId, enroll, receipt);

        private static Task ExecuteReconciliation(Func<CharacterContext> contexts, ulong characterId,
            Action<CharacterContext> enroll, CharacterSaveReconciliation receipt)
        {
            ArgumentNullException.ThrowIfNull(receipt); receipt.Validate();
            return ObserveEnrollment();

            async Task ObserveEnrollment()
            {
                bool entered = false;
                try
                {
                    await ExecuteCore(contexts, characterId, context => { entered = true; enroll(context); }, receipt);
                }
                catch (BankReconciliationConflictException) { throw; }
                catch (HousingRenameConflictException) { throw; }
                catch (HousingDecorPurchaseConflictException) { throw; }
                catch (Exception error) when (!entered)
                {
                    // The actor's persistence enrollment did not run. Context
                    // creation, owner/receipt reads and transaction start cannot
                    // have written this phase, so its frozen snapshot stays owned.
                    throw receipt.RolledBack(null, null, error, false);
                }
            }
        }

        private static async Task ExecuteCore(Func<CharacterContext> contexts, ulong characterId, Action<CharacterContext> enroll,
            CharacterSaveReconciliation receipt)
        {
            ArgumentNullException.ThrowIfNull(contexts); ArgumentNullException.ThrowIfNull(enroll);
            if (characterId == 0) throw new ArgumentOutOfRangeException(nameof(characterId));
            var candidate = contexts() ?? throw new InvalidOperationException("Character context factory returned null.");
            // A bad factory must not dispose somebody else's active context.
            lock (ownershipGate)
            {
                if (owners.TryGetValue(candidate, out _)) throw new InvalidOperationException("Atomic Character context was already owned.");
                candidate.BindAtomicCharacter(characterId); owners.Add(candidate, new object());
            }
            await using var context = candidate;
            string database = receipt == null ? null : CharacterDatabaseIdentity.Get(context);
            context.OnSaveValidation(() =>
            {
                if (!context.AtomicGraphSealed) throw new InvalidOperationException("Actor enrollment cannot flush or commit before its complete graph is sealed.");
            });
            bool entered = false;
            // Execute scopes the provider strategy synchronously. The returned
            // task retains that scope, but asynchronous failures are not retries
            // of the actor callback. No SQL await precedes actor enrollment.
            Task saving = context.Database.CreateExecutionStrategy().Execute(() =>
            {
                if (entered) throw new InvalidOperationException("Actor enrollment cannot be retried; reload its authoritative state.");
                entered = true;
                var transaction = context.Database.BeginTransaction(IsolationLevel.Serializable);
                object recoveryOwner = null;
                try
                {
                    bool mysql = context.Database.ProviderName?.Contains("MySql", StringComparison.Ordinal) == true;
                    if (!mysql && !context.Database.IsSqlite()) throw new InvalidOperationException("Unsupported atomic Character provider.");
                    var owner = (mysql ? context.Character.FromSqlInterpolated($"SELECT * FROM `character` WHERE id={characterId} FOR UPDATE")
                        : context.Character.Where(row => row.Id == characterId)).AsNoTracking().AsEnumerable().SingleOrDefault();
                    if (owner == null || owner.DeleteTime != null) throw new InvalidOperationException("Atomic Character owner no longer exists.");
                    if (receipt != null)
                    {
                        receipt.ValidateParent(context);
                        if (receipt.ReadPayload(context, receipt.Id) != null)
                            throw new InvalidOperationException("Character reconciliation identity is already in use.");
                    }
                    if (receipt != null)
                    {
                        recoveryOwner = new object();
                        // Keep partial enrollment owned as well. Unsealed flush
                        // and commit remain rejected by the validation above.
                        context.RetainUncertainSettlement(recoveryOwner, beforeEnrollment: true);
                        receipt.Retry.Begin(context);
                    }
                    try { enroll(context); }
                    catch { receipt?.Retry.Capture(context); throw; }
                    receipt?.Retry.RestoreOmittedWrites(context);
                    receipt?.Retry.Capture(context);
                    receipt?.ValidateEnrolled(context);
                    var graph = CharacterSaveGraph.Seal(context, receipt);
                    return Complete(graph.ReceiptPayload, recoveryOwner);
                }
                catch (Exception error)
                {
                    if (recoveryOwner == null) { transaction.Dispose(); throw; }
                    try { transaction.Dispose(); }
                    catch (Exception disposal) { error = new AggregateException(error, disposal); }
                    return Recover(null, recoveryOwner, error, enrollmentCompleted: false);
                }

                async Task Complete(string payload, object recoveryOwner)
                {
                    try
                    {
                        await using (transaction)
                        {
                            if (receipt != null) await receipt.AuthorizeAsync(payload);
                            await context.SaveChangesAsync();
                            await transaction.CommitAsync();
                        }
                    }
                    catch (Exception error)
                    {
                        if (receipt == null) throw;
                        await Recover(payload, recoveryOwner, error, enrollmentCompleted: true);
                    }
                }

                async Task Recover(string payload, object recoveryOwner, Exception error, bool enrollmentCompleted)
                {
                    if (context.RecoverableSaveConfirmed) return;
                    // A partial enrollment has no complete receipt payload. Any
                    // present receipt is a conflict, never evidence of success.
                    bool committed = await ProbeUntilKnown(contexts, characterId, database, receipt, payload);
                    if (!committed) await receipt.ProvenRollbackAsync();
                    context.ResolveUncertainSettlement(recoveryOwner, committed);
                    if (!committed)
                    {
                        receipt.Retry.ProvenRollback(context);
                        throw receipt.RolledBack(context, database, error, enrollmentCompleted);
                    }
                }
            });
            await saving;
        }

        private static async Task<bool> ProbeUntilKnown(Func<CharacterContext> contexts, ulong character, string database, CharacterSaveReconciliation reconciliation, string payload)
        {
            bool reported = false;
            while (true)
            {
                try
                {
                    await using var driver = Fresh();
                    return await driver.Database.CreateExecutionStrategy().ExecuteAsync(async () =>
                    {
                        await using var probe = Fresh();
                        await using var transaction = await probe.Database.BeginTransactionAsync(IsolationLevel.Serializable);
                        bool mysql = probe.Database.ProviderName?.Contains("MySql", StringComparison.Ordinal) == true;
                        if (!mysql && !probe.Database.IsSqlite()) throw new InvalidOperationException("Unsupported bid reconciliation provider.");
                        _ = await (mysql ? probe.Character.FromSqlInterpolated($"SELECT * FROM `character` WHERE id={character} FOR UPDATE")
                            : probe.Character.Where(row => row.Id == character)).AsNoTracking().SingleOrDefaultAsync();
                        var receipt = reconciliation.ReadPayload(probe, reconciliation.Id);
                        if (receipt != null && !string.Equals(receipt, payload, StringComparison.Ordinal))
                            throw new InvalidOperationException("Character reconciliation receipt conflicts with its complete frozen graph.");
                        return receipt != null;
                    });
                }
                catch (Exception error)
                {
                    // Keep the original actor/row owners and its TaskEvent live.
                    // A failed observation is not proof of rollback and must not
                    // replay the actor's enrollment or release later intent.
                    if (!reported)
                    {
                        reported = true;
                        try { NLog.LogManager.GetCurrentClassLogger().Warn(error, "Bid ordinary-save outcome remains unresolved; original ownership is retained."); }
                        catch { }
                    }
                    await Task.Delay(TimeSpan.FromSeconds(1));
                }
            }

            CharacterContext Fresh()
            {
                var next = contexts() ?? throw new InvalidOperationException("Character reconciliation probe factory returned null.");
                lock (ownershipGate)
                {
                    if (owners.TryGetValue(next, out _)) throw new InvalidOperationException("Character reconciliation probe reused an owned context.");
                    next.BindAtomicCharacter(character); owners.Add(next, new object());
                }
                try { CharacterDatabaseIdentity.Require(next, database); return next; }
                catch { next.Dispose(); throw; }
            }
        }
    }
}
