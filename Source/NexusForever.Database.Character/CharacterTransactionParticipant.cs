using System;
using System.Data;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Storage;

namespace NexusForever.Database.Character
{
    public partial class CharacterContext
    {
        private CharacterContext transactionParent;
        private bool participantEnrollmentClosed;
        private readonly Dictionary<ParticipantKey, ParticipantRow> participantWrites = [];
        private sealed class ParticipantKey(Type type, object[] values)
        {
            public override bool Equals(object obj) => obj is ParticipantKey key && type == key.Type
                && StructuralComparisons.StructuralEqualityComparer.Equals(values, key.Values);
            public override int GetHashCode() => HashCode.Combine(type, StructuralComparisons.StructuralEqualityComparer.GetHashCode(values));
            private Type Type => type;
            private object[] Values => values;
        }
        private sealed record ParticipantRow(object Model, PropertyValues Target, bool Deleted);
        /// <summary>
        /// A fresh tracker on this atomic unit's existing connection/transaction.
        /// Disposing it owns neither the parent connection nor its transaction.
        /// Its writes remain provisional until the parent seals and commits.
        /// </summary>
        public CharacterContext CreateTransactionParticipant(bool readOnly = false)
        {
            if (!AtomicCharacterId.HasValue || (AtomicGraphSealed || participantEnrollmentClosed) && !readOnly || IsSaveRejected || HasSavepointRollback
                || Database.CurrentTransaction?.GetDbTransaction().IsolationLevel != IsolationLevel.Serializable)
                throw new InvalidOperationException("Transaction participants require their active unsealed atomic Character owner.");
            var connection = Database.GetDbConnection();
            var builder = new DbContextOptionsBuilder<CharacterContext>();
            if (Database.IsSqlite()) builder.UseSqlite(connection);
            else if (Database.ProviderName?.Contains("MySql", StringComparison.Ordinal) == true)
            {
                // The parent already opened this exact connection. No new
                // connection, credential lookup or server discovery is needed.
                var version = Version.Parse(new string(connection.ServerVersion.TakeWhile(c => char.IsDigit(c) || c == '.').ToArray()));
                if (connection.ServerVersion.Contains("MariaDB", StringComparison.OrdinalIgnoreCase)) builder.UseMySql(connection, new MariaDbServerVersion(version));
                else builder.UseMySql(connection, new MySqlServerVersion(version));
            }
            else throw new InvalidOperationException("Unsupported Character participant provider.");
            var participant = new CharacterContext(builder.Options);
            try
            {
                participant.Database.UseTransaction(Database.CurrentTransaction.GetDbTransaction());
                participant.Database.SetCommandTimeout(Database.GetCommandTimeout());
                participant.transactionParent = this;
                (EntityEntry Entry, EntityState State, PropertyValues Values, PropertyValues Original, string[] Modified, string[] Generated)[] staged = [];
                participant.OnSaveValidation(phase =>
                {
                    if (readOnly) throw new InvalidOperationException("A read-only Character participant cannot flush or commit.");
                    if (phase == CharacterSaveValidationPhase.BeforeExplicitCommit)
                        throw new InvalidOperationException("Only the parent may commit a Character transaction participant.");
                    foreach (var row in staged)
                        if (row.Entry.State != row.State || !SameParticipant(row.Entry.CurrentValues, row.Values) || !SameParticipant(row.Entry.OriginalValues, row.Original)
                            || !row.Entry.Properties.Where(property => property.IsModified).Select(property => property.Metadata.Name).Order().SequenceEqual(row.Modified))
                            throw new InvalidOperationException("A Character participant target or write mask changed before flush.");
                    if (participant.ChangeTracker.Entries().Any(entry => !staged.Any(row => ReferenceEquals(row.Entry.Entity, entry.Entity))))
                        throw new InvalidOperationException("A late row entered the Character participant.");
                });
                participant.SavingChanges += (_, _) =>
                {
                    if (readOnly || AtomicGraphSealed || participantEnrollmentClosed || IsSaveRejected)
                    {
                        var error = new InvalidOperationException("Atomic Character enrollment already ended.");
                        participant.RejectSave(error); throw error;
                    }
                    participant.ChangeTracker.DetectChanges();
                    staged = participant.ChangeTracker.Entries().Select(entry => (entry, entry.State, entry.CurrentValues.Clone(), entry.OriginalValues.Clone(),
                        entry.Properties.Where(property => property.IsModified).Select(property => property.Metadata.Name).Order().ToArray(),
                        entry.Properties.Where(property => property.IsTemporary || property.Metadata.ValueGenerated == ValueGenerated.OnAddOrUpdate
                            || entry.State == EntityState.Added && property.Metadata.ValueGenerated == ValueGenerated.OnAdd && Equals(property.CurrentValue, property.Metadata.Sentinel))
                            .Select(property => property.Metadata.Name).ToArray())).ToArray();
                };
                participant.SavedChanges += (_, _) =>
                {
                    foreach (var row in staged)
                    {
                        if (row.State == EntityState.Unchanged) continue;
                        var values = row.Values.Clone();
                        bool deleted = row.State == EntityState.Deleted;
                        if (!deleted)
                        {
                            foreach (string generated in row.Generated) values[generated] = row.Entry.CurrentValues[generated];
                            if (!SameParticipant(values, row.Entry.CurrentValues))
                            {
                                var error = new InvalidOperationException("A flushed Character participant changed its captured target.");
                                participant.RejectSave(error); throw error;
                            }
                        }
                        var key = new ParticipantKey(row.Entry.Metadata.ClrType,
                            row.Entry.Metadata.FindPrimaryKey().Properties.Select(property => values[property.Name]).ToArray());
                        participantWrites[key] = new(values.ToObject(), values, deleted);
                    }
                };
                participant.SaveChangesFailed += (_, args) => RejectSave(args.Exception);
                return participant;
            }
            catch { participant.Dispose(); throw; }
        }

        /// <summary>
        /// Close participant writes and protect command-owned rows through the
        /// parent commit. Only explicitly named model types may join subsequent
        /// ordinary enrollment (for example the final Character Challenge row).
        /// </summary>
        public void ProtectTransactionParticipantWrites(params Type[] mutableTypes)
        {
            if (!AtomicCharacterId.HasValue || AtomicGraphSealed || participantEnrollmentClosed || IsSaveRejected)
                throw new InvalidOperationException("Participant targets already ended their enrollment.");
            participantEnrollmentClosed = true;
            var mutable = (mutableTypes ?? []).ToHashSet();
            var targets = participantWrites.Values.Where(row => !mutable.Contains(row.Model.GetType())).ToArray();
            OnSaveValidation(phase =>
            {
                foreach (var row in targets)
                {
                    var entry = Entry(row.Model); var primary = entry.Metadata.FindPrimaryKey();
                    var tracked = ChangeTracker.Entries().SingleOrDefault(candidate => candidate.Metadata.ClrType == entry.Metadata.ClrType
                        && primary.Properties.All(property => Equals(candidate.Property(property.Name).CurrentValue, row.Target[property.Name])));
                    if (!row.Deleted && (tracked == null || tracked.State != EntityState.Unchanged || !SameParticipant(tracked.CurrentValues, row.Target))
                        || row.Deleted && tracked != null)
                        throw new InvalidOperationException("A protected Character participant changed its final target.");
                    if (phase == CharacterSaveValidationPhase.BeforeExplicitCommit)
                    {
                        var stored = entry.GetDatabaseValues();
                        if (row.Deleted ? stored != null : stored == null || !SameParticipant(stored, row.Target))
                            throw new InvalidOperationException("Protected Character participant SQL changed before commit.");
                    }
                }
            });
        }

        // Fold all successful participant writes into the final owner's graph.
        // Intermediate writes of the same key are replaced by the last target.
        internal void EnrollTransactionParticipants()
        {
            foreach (var row in participantWrites.Values)
            {
                var probe = Entry(row.Model); var stored = probe.GetDatabaseValues();
                CharacterSaveGraph.ValidateCharacterOwner(this, probe, row.Deleted);
                if (row.Deleted ? stored != null : stored == null || !SameParticipant(stored, row.Target))
                    throw new InvalidOperationException("Character participant SQL differs from its captured target.");
                var primary = probe.Metadata.FindPrimaryKey();
                var existing = ChangeTracker.Entries().SingleOrDefault(entry => entry.Metadata.ClrType == probe.Metadata.ClrType
                    && primary.Properties.All(property => Equals(entry.Property(property.Name).CurrentValue, row.Target[property.Name])));
                if (!row.Deleted)
                {
                    if (existing == null) probe.State = EntityState.Unchanged;
                }
                else if (existing == null || existing.State != EntityState.Added)
                    OnSaveValidation(phase =>
                    {
                        if (phase == CharacterSaveValidationPhase.BeforeExplicitCommit && probe.GetDatabaseValues() != null)
                            throw new InvalidOperationException("A deleted Character participant reappeared before commit.");
                    });
            }
        }

        internal object[] CaptureParticipantReceiptTargets() => participantWrites.Values.Select(row => (object)new
        {
            Type = row.Model.GetType().FullName, row.Deleted,
            Values = new SortedDictionary<string, object>(row.Target.Properties.ToDictionary(property => property.Name,
                property => row.Target[property.Name]), StringComparer.Ordinal)
        }).ToArray();
        private static bool SameParticipant(PropertyValues first, PropertyValues second)
            => first.Properties.All(property => property.GetValueComparer()?.Equals(first[property.Name], second[property.Name])
                ?? Equals(first[property.Name], second[property.Name]));
    }
}
