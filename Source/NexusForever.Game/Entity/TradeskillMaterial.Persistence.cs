using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using NexusForever.Database.Character;
using NexusForever.Database.Character.Model;
using NexusForever.Game.Quest;

namespace NexusForever.Game.Entity
{
    public partial class TradeskillMaterial
    {
        private readonly object materialSaveGate = new();
        private static readonly object materialLeaseGate = new();
        private static readonly Dictionary<(string, ulong, ushort), MaterialSave> materialLeases = [];
        private sealed class MaterialSave(CharacterContext context, string database, object token)
        {
            internal readonly CharacterContext Context = context;
            internal readonly string Database = database;
            internal readonly object Token = token;
            internal readonly List<ushort?> Possible = [];
            internal ushort? Sql;
            internal ushort Target, FlushedTarget;
            internal long Revision, FlushedRevision;
            internal bool Flushed;
            internal CharacterColumnSaveIntent Row;
            internal IDisposable Scope;
            internal EventHandler<SavedChangesEventArgs> OnFlushed;
        }
        private long amountRevision;
        private string materialDatabase;
        private List<ushort?> uncertainAmounts;
        private MaterialSave materialSave;

        internal Action<CharacterContext> CaptureSave(object token, string database)
        {
            lock (materialSaveGate)
            {
                if (materialSave != null) throw new InvalidOperationException("Material has an unfinished save.");
                ushort target = amount; long revision = amountRevision;
                return context => SaveMaterial(context, target, revision, token, database);
            }
        }
        internal void SaveMaterial(CharacterContext context, string database)
        {
            lock (materialSaveGate) SaveMaterial(context, amount, amountRevision, null, database);
        }
        private ushort? ReadMaterial(CharacterContext context) => context.CharacterTradeskillMaterial.AsNoTracking()
            .SingleOrDefault(row => row.Id == Owner && row.MaterialId == MaterialId)?.Amount;
        private void SaveMaterial(CharacterContext context, ushort target, long revision, object token, string expectedDatabase)
        {
            lock (materialSaveGate)
            {
                try
                {
                    if (context.IsSaveRejected || context.HasSavepointRollback) throw new InvalidOperationException("Material requires a fresh save context.");
                    string route = CharacterDatabaseIdentity.Get(context);
                    if (expectedDatabase != null) CharacterDatabaseIdentity.Require(context, expectedDatabase);
                    if (materialDatabase != null) CharacterDatabaseIdentity.Require(context, materialDatabase);
                    materialDatabase ??= route;
                    if (materialSave == null && uncertainAmounts == null && commodityStoredAmount == target)
                    {
                        if (amountRevision == revision) saveMask = TradeskillMaterialSaveMask.None;
                        return;
                    }
                    if (commodityReturnOwner != null) throw new InvalidOperationException("Material belongs to a commodity return.");
                    if (materialSave != null && (!ReferenceEquals(materialSave.Context, context) || !ReferenceEquals(materialSave.Token, token)))
                        throw new InvalidOperationException("Material belongs to an unfinished save unit.");
                    MaterialSave unit = materialSave;
                    if (unit == null)
                    {
                        unit = new(context, route, token); unit.Scope = QuestSaveScope.Acquire(context, route, $"{Owner}:material:{MaterialId}", token);
                        try
                        {
                            lock (materialLeaseGate)
                            {
                                if (materialLeases.ContainsKey((route, Owner, MaterialId))) throw new InvalidOperationException("Material belongs to another loaded owner.");
                                materialLeases.Add((route, Owner, MaterialId), unit);
                            }
                        }
                        catch { unit.Scope.Dispose(); throw; }
                        materialSave = unit;
                        unit.OnFlushed = (_, _) =>
                        {
                            lock (materialSaveGate)
                            {
                                if (!ReferenceEquals(materialSave, unit) || context.HasSavepointRollback || ReadMaterial(context) != unit.Target) return;
                                unit.Sql = unit.Target; unit.FlushedTarget = unit.Target; unit.FlushedRevision = unit.Revision; unit.Flushed = true;
                                if (unit.Row != null) { unit.Row.Capture(); unit.Row.Flushed = true; }
                            }
                        };
                        context.SavedChanges += unit.OnFlushed;
                        context.OnSaveCompleted(committed => CompleteMaterial(unit, committed));
                        context.OnSaveValidation(phase =>
                        {
                            lock (materialSaveGate)
                            {
                                if (context.HasSavepointRollback) return;
                                if (!ReferenceEquals(materialSave, unit)) throw new InvalidOperationException("Material lost its save owner.");
                                context.ChangeTracker.DetectChanges(); unit.Row?.Validate();
                                if (ReadMaterial(context) != (phase == CharacterSaveValidationPhase.BeforeFlush ? unit.Sql : unit.Target))
                                    throw new InvalidOperationException("Material SQL differs from its captured target.");
                            }
                        });
                        ushort? actual = ReadMaterial(context);
                        if (uncertainAmounts != null ? !uncertainAmounts.Contains(actual) : actual != commodityStoredAmount)
                            throw new InvalidOperationException("Material differs from its loaded or uncertain amount.");
                        unit.Sql = actual; unit.Possible.Add(actual);
                    }
                    unit.Row?.Validate();
                    var tracked = context.CharacterTradeskillMaterial.Local.SingleOrDefault(row => row.Id == Owner && row.MaterialId == MaterialId);
                    if (tracked != null && (unit.Row == null || !ReferenceEquals(unit.Row.Entry.Entity, tracked)))
                        throw new InvalidOperationException("Material has foreign tracked intent.");
                    if (tracked == null && unit.Sql != target)
                    {
                        tracked = new() { Id = Owner, MaterialId = MaterialId, Amount = unit.Sql ?? target };
                        context.Entry(tracked).State = unit.Sql.HasValue ? EntityState.Unchanged : EntityState.Added;
                    }
                    if (tracked != null)
                    {
                        var entry = context.Entry(tracked); tracked.Amount = target;
                        if (unit.Sql.HasValue) { entry.Property(row => row.Amount).OriginalValue = unit.Sql.Value; entry.Property(row => row.Amount).IsModified = unit.Sql != target; }
                        context.ChangeTracker.DetectChanges(); unit.Row = new(entry, [nameof(CharacterTradeskillMaterialModel.Id), nameof(CharacterTradeskillMaterialModel.MaterialId), nameof(CharacterTradeskillMaterialModel.Amount)], false, "Material"); unit.Row.Capture();
                    }
                    unit.Target = target; unit.Revision = revision; unit.Possible.Add(target);
                    context.Database.AutoTransactionBehavior = AutoTransactionBehavior.Always;
                }
                catch (Exception error) { context.RejectSave(error); throw; }
            }
        }
        private void CompleteMaterial(MaterialSave unit, bool committed)
        {
            lock (materialSaveGate)
            {
                if (!ReferenceEquals(materialSave, unit)) return;
                try
                {
                    if (committed && unit.Flushed)
                    {
                        commodityStoredAmount = unit.FlushedTarget; uncertainAmounts = null;
                        saveMask &= ~TradeskillMaterialSaveMask.Create;
                        if (amountRevision == unit.FlushedRevision) saveMask = TradeskillMaterialSaveMask.None;
                        else saveMask |= TradeskillMaterialSaveMask.Amount;
                    }
                    else uncertainAmounts = (uncertainAmounts ?? []).Concat(unit.Possible).Append(commodityStoredAmount).ToList();
                }
                finally
                {
                    unit.Context.SavedChanges -= unit.OnFlushed; materialSave = null;
                    lock (materialLeaseGate) if (materialLeases.TryGetValue((unit.Database, Owner, MaterialId), out var held) && ReferenceEquals(held, unit)) materialLeases.Remove((unit.Database, Owner, MaterialId));
                    unit.Scope.Dispose();
                }
            }
        }
    }
}
