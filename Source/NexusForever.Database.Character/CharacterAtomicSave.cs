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
        public static async Task Execute(Func<CharacterContext> contexts, ulong characterId, Action<CharacterContext> enroll)
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
                try
                {
                    bool mysql = context.Database.ProviderName?.Contains("MySql", StringComparison.Ordinal) == true;
                    if (!mysql && !context.Database.IsSqlite()) throw new InvalidOperationException("Unsupported atomic Character provider.");
                    var owner = (mysql ? context.Character.FromSqlInterpolated($"SELECT * FROM `character` WHERE id={characterId} FOR UPDATE")
                        : context.Character.Where(row => row.Id == characterId)).AsNoTracking().AsEnumerable().SingleOrDefault();
                    if (owner == null || owner.DeleteTime != null) throw new InvalidOperationException("Atomic Character owner no longer exists.");
                    enroll(context);
                    CharacterSaveGraph.Seal(context);
                    return Complete();
                }
                catch { transaction.Dispose(); throw; }

                async Task Complete()
                {
                    await using (transaction)
                    {
                        await context.SaveChangesAsync();
                        await transaction.CommitAsync();
                    }
                }
            });
            await saving;
        }
    }
}
