using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace NexusForever.Database.Character
{
    public enum CharacterSaveValidationPhase
    {
        BeforeFlush, BeforeImplicitCommit, BeforeExplicitCommit
    }

    public partial class CharacterContext
    {
        private readonly List<Action<CharacterSaveValidationPhase>> saveValidations = [];
        private Exception saveRejection;
        private bool validatingSave;

        public bool IsSaveRejected => saveRejection != null;

        /// <summary>
        /// Reject the whole context after partial enrollment fails. Rejection survives
        /// rollback and disposal callbacks; use a fresh context for a new attempt.
        /// </summary>
        public void RejectSave(Exception cause)
        {
            ArgumentNullException.ThrowIfNull(cause);
            saveRejection ??= cause;
            transactionParent?.RejectSave(cause);
        }

        /// <summary>
        /// Check enrolled invariants before each flush and before an EF-owned commit.
        /// Callbacks must be synchronous and must not write or save. Registration lasts
        /// until terminal completion, including across multiple explicit-transaction flushes.
        /// Direct ADO.NET/raw SQL transactions bypass this EF contract.
        /// </summary>
        public void OnSaveValidation(Action validation)
        {
            ArgumentNullException.ThrowIfNull(validation);
            OnSaveValidation(_ => validation());
        }

        public void OnSaveValidation(Action<CharacterSaveValidationPhase> validation)
        {
            ArgumentNullException.ThrowIfNull(validation);
            ThrowIfSaveRejected();
            if (validatingSave)
            {
                var error = new InvalidOperationException("Cannot enroll validation during save validation.");
                RejectSave(error);
                throw error;
            }
            saveValidations.Add(validation);
        }

        private void ThrowIfSaveRejected()
        {
            if (saveRejection != null)
                throw new InvalidOperationException("This character save unit was rejected; use a fresh context.", saveRejection);
        }

        private void ValidateSaveUnit(bool committing = false)
        {
            ThrowIfSaveRejected();
            try
            {
                if (validatingSave)
                    throw new InvalidOperationException("Recursive character save validation is not supported.");
                validatingSave = true;
                CharacterSaveValidationPhase phase = !committing ? CharacterSaveValidationPhase.BeforeFlush
                    : saveInProgress ? CharacterSaveValidationPhase.BeforeImplicitCommit : CharacterSaveValidationPhase.BeforeExplicitCommit;
                foreach (Action<CharacterSaveValidationPhase> validate in saveValidations)
                {
                    validate(phase);
                    // A validator may explicitly reject without throwing.
                    ThrowIfSaveRejected();
                }
            }
            catch (Exception error)
            {
                RejectSave(error);
                throw;
            }
            finally { validatingSave = false; }
        }

        private sealed class SaveValidationInterceptor : SaveChangesInterceptor
        {
            public static SaveValidationInterceptor Instance { get; } = new();

            public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
            {
                // EF raises the public SavingChanges event before this interceptor.
                // Validate here as well as at commit, before even a single-statement
                // save which may not need an implicit transaction.
                ((CharacterContext)eventData.Context)?.ValidateSaveUnit();
                return result;
            }

            public override ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData,
                InterceptionResult<int> result, CancellationToken cancellationToken = default)
                => new(SavingChanges(eventData, result));
        }
    }
}
