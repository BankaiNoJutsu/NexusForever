using Microsoft.EntityFrameworkCore;
using NexusForever.Database.Character;
using NexusForever.Database.Character.Model;

namespace NexusForever.Game.Entity
{
    public partial class Player
    {
        private TaxiSaveUnit taxiSave;
        private sealed record TaxiSaveRow(CharacterTaxiNodeModel Model, bool Created);
        private sealed class TaxiSaveUnit(CharacterContext context, ulong character)
        {
            internal readonly CharacterContext Context = context;
            internal readonly ulong Character = character;
            internal readonly Dictionary<ushort, TaxiSaveRow> Rows = [];
            internal HashSet<ushort> Flushed = [];
            internal EventHandler<SavedChangesEventArgs> OnFlushed;
            internal bool Enrolling, SettlementRegistered;
        }

        private void SaveTaxiPersistence(CharacterContext context, long? capturedTaxiFareVersion = null)
        {
            ArgumentNullException.ThrowIfNull(context);
            TaxiSaveUnit unit; ushort[] pending; bool created;
            lock (taxiStateLock)
            {
                if (capturedTaxiFareVersion.HasValue && capturedTaxiFareVersion.Value != TaxiFareVersion)
                    return;
                if (taxiSave != null && (!ReferenceEquals(taxiSave.Context, context) || taxiSave.Enrolling))
                {
                    var error = new InvalidOperationException("Taxi discovery belongs to an unfinished save unit.");
                    context.RejectSave(error); throw error;
                }
                pending = unsavedTaxiNodes.ToArray();
                if (pending.Length == 0) return;
                created = taxiSave == null;
                unit = taxiSave ??= new(context, CharacterId); unit.Enrolling = true;
            }
            if (created) context.OnSaveCompleted(_ =>
            {
                if (!unit.SettlementRegistered) FinishTaxiSave(unit, false);
            });
            try
            {
                if (context.IsSaveRejected || context.HasSavepointRollback || CharacterId != unit.Character)
                    throw new InvalidOperationException("Taxi discovery requires its original Character and fresh save context.");
                if (created)
                {
                    unit.OnFlushed = (_, _) => CaptureTaxiFlush(unit);
                    context.SavedChanges += unit.OnFlushed;
                    context.OnSaveValidation(phase => ValidateTaxiSave(unit, phase));
                    context.OnSaveSettled(committed => FinishTaxiSave(unit, committed));
                    unit.SettlementRegistered = true;
                }
                foreach (ushort id in pending)
                {
                    if (unit.Rows.ContainsKey(id)) continue;
                    var tracked = context.ChangeTracker.Entries<CharacterTaxiNodeModel>()
                        .SingleOrDefault(entry => entry.Entity.Id == unit.Character && entry.Entity.TaxiNodeId == id);
                    if (tracked != null)
                    {
                        if (tracked.State is not (EntityState.Added or EntityState.Unchanged))
                            throw new InvalidOperationException("Taxi discovery lost its shared-context row intent.");
                        unit.Rows.Add(id, new(tracked.Entity, tracked.State == EntityState.Added));
                    }
                    else if (TaxiRowExists(unit, id))
                        unit.Rows.Add(id, new(null, false));
                    else
                    {
                        var model = new CharacterTaxiNodeModel { Id = unit.Character, TaxiNodeId = id };
                        context.Add(model); unit.Rows.Add(id, new(model, true));
                    }
                }
            }
            catch (Exception error) { context.RejectSave(error); throw; }
            finally { lock (taxiStateLock) unit.Enrolling = false; }
        }

        private static bool TaxiRowExists(TaxiSaveUnit unit, ushort id)
            => unit.Context.CharacterTaxiNode.AsNoTracking().Any(row => row.Id == unit.Character && row.TaxiNodeId == id);

        private void ValidateTaxiSave(TaxiSaveUnit unit, CharacterSaveValidationPhase phase)
        {
            lock (taxiStateLock)
                if (!ReferenceEquals(taxiSave, unit) || unit.Enrolling || CharacterId != unit.Character)
                    throw new InvalidOperationException("Taxi discovery lost its enrolled save owner.");
            // An outer commit after a savepoint rollback may commit other work.
            // CharacterContext reports this unit as uncertain at settlement.
            if (unit.Context.HasSavepointRollback) return;
            foreach (var pair in unit.Rows)
            {
                var row = pair.Value;
                if (row.Model != null)
                {
                    var entry = unit.Context.Entry(row.Model);
                    if (row.Model.Id != unit.Character || row.Model.TaxiNodeId != pair.Key
                        || entry.State is EntityState.Detached or EntityState.Deleted
                        || row.Created && !unit.Flushed.Contains(pair.Key) && entry.State != EntityState.Added)
                        throw new InvalidOperationException("Taxi discovery row changed or its insert was suppressed.");
                }
                if (phase == CharacterSaveValidationPhase.BeforeImplicitCommit
                    || phase == CharacterSaveValidationPhase.BeforeExplicitCommit && unit.Flushed.Contains(pair.Key))
                    if (!TaxiRowExists(unit, pair.Key))
                        throw new InvalidOperationException("Taxi discovery SQL differs from its flushed presence.");
            }
        }

        private void CaptureTaxiFlush(TaxiSaveUnit unit)
        {
            if (unit.Context.HasSavepointRollback) return;
            var flushed = new HashSet<ushort>();
            foreach (var pair in unit.Rows)
            {
                if (pair.Value.Model != null && (pair.Value.Model.Id != unit.Character || pair.Value.Model.TaxiNodeId != pair.Key
                    || unit.Context.Entry(pair.Value.Model).State is EntityState.Detached or EntityState.Deleted))
                    throw new InvalidOperationException("Taxi discovery lost its flushed row identity.");
                if (!TaxiRowExists(unit, pair.Key))
                    throw new InvalidOperationException("Taxi discovery row was not written by this flush.");
                flushed.Add(pair.Key);
            }
            lock (taxiStateLock)
                if (ReferenceEquals(taxiSave, unit)) unit.Flushed = flushed;
        }

        private void FinishTaxiSave(TaxiSaveUnit unit, bool committed)
        {
            unit.Context.SavedChanges -= unit.OnFlushed;
            lock (taxiStateLock)
            {
                if (!ReferenceEquals(taxiSave, unit)) return;
                if (committed && CharacterId == unit.Character) unsavedTaxiNodes.ExceptWith(unit.Flushed);
                taxiSave = null;
            }
        }
    }
}
