using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata;
using NexusForever.Database.Character.Model;

namespace NexusForever.Database.Character
{
    // Persistence intents only. Several legacy managers consume their dirty
    // markers during enrollment. A proven bid rollback must not lose those
    // writes when the original actor enrolls its retained snapshot again.
    internal sealed class CharacterBidSaveRetry
    {
        private sealed record Row(IEntityType Type, EntityState State, object[] Keys,
            PropertyValues Values, PropertyValues Originals, PropertyValues Before, string[] Modified);
        private readonly List<Row> rows = [];
        private string database;
        private ulong character;
        private CharacterContext attempted;
        private string provenContextId;
        private bool retry;
        private Exception captureFailure;

        internal void Begin(CharacterContext context)
        {
            string route = CharacterDatabaseIdentity.Get(context);
            if (database == null) { database = route; character = context.AtomicCharacterId.Value; }
            if (database != route || character != context.AtomicCharacterId || attempted != null)
                throw new InvalidOperationException("Bid retry lost its original Character/database attempt.");
            attempted = context;
            provenContextId = null;
        }

        internal void RestoreOmittedWrites(CharacterContext context)
        {
            Require(context);
            if (!retry) return;
            if (captureFailure != null)
                throw new InvalidOperationException("Bid retry retains an incomplete preimage capture; it cannot publish or guess missing writes.", captureFailure);
            context.ChangeTracker.DetectChanges();
            foreach (var row in rows)
            {
                var entry = context.ChangeTracker.Entries().SingleOrDefault(candidate => SameKey(candidate, row));
                bool added = row.State == EntityState.Added;
                // Inserts own a complete image even though EF reports no
                // modified columns. Overlay a newer partial update onto it.
                var modified = added ? row.Values.Properties.Where(property => !property.IsPrimaryKey()).Select(property => property.Name).ToArray() : row.Modified;
                if (entry != null)
                {
                    if (entry.State == EntityState.Deleted || entry.State == EntityState.Added) continue;
                    if (row.State == EntityState.Deleted)
                        throw new InvalidOperationException("Bid retry has conflicting deletion intent.");
                    modified = modified.Where(name => !entry.Property(name).IsModified).ToArray();
                    if (!added && modified.Length == 0) continue;
                }

                // Retain the first attempt's stored preimage, including custody
                // and concurrency columns. A later SQL read grants no new
                // authority over a replaced/moved/deleted row.
                if (!added && row.Before != null)
                    CharacterSaveGraph.ValidateCharacterOwner(context, context.Entry(row.Before.ToObject()), row.State == EntityState.Deleted);
                var probe = entry ?? context.Entry(row.Values.ToObject());
                var stored = probe.GetDatabaseValues();
                if (added ? stored != null : stored == null || row.Before == null || !Same(stored, row.Before))
                    throw new InvalidOperationException("Bid retry lost an omitted row's captured SQL preimage.");
                if (entry == null)
                {
                    entry = context.Attach(row.Values.ToObject());
                    entry.OriginalValues.SetValues(row.Originals);
                    entry.State = row.State;
                    foreach (var property in entry.Properties) property.IsModified = modified.Contains(property.Metadata.Name);
                }
                else
                {
                    if (added) entry.State = EntityState.Added;
                    foreach (string name in modified)
                    {
                        entry.Property(name).CurrentValue = row.Values[name];
                        entry.Property(name).OriginalValue = row.Originals[name];
                        entry.Property(name).IsModified = true;
                    }
                }
            }
        }

        internal void Capture(CharacterContext context)
        {
            try { CaptureRows(context); }
            catch (Exception error) { captureFailure ??= error; throw; }
        }

        private void CaptureRows(CharacterContext context)
        {
            Require(context);
            context.ChangeTracker.DetectChanges();
            foreach (var entry in context.ChangeTracker.Entries().Where(entry => entry.Entity is not (MarketplaceBidReceiptModel or BankReconciliationReceiptModel)
                && entry.State is EntityState.Added or EntityState.Modified or EntityState.Deleted))
            {
                CharacterSaveGraph.ValidateCharacterOwner(context, entry, entry.State == EntityState.Deleted);
                if (entry.Properties.Any(property => property.IsTemporary))
                    throw new InvalidOperationException("Bid retry requires retained, non-temporary row identities.");
                var keys = entry.Metadata.FindPrimaryKey().Properties.Select(property => entry.Property(property.Name).CurrentValue).ToArray();
                var modified = entry.Properties.Where(property => property.IsModified).Select(property => property.Metadata.Name).Order().ToArray();
                var previous = rows.SingleOrDefault(row => SameKey(entry, row));
                // Keep the earliest preimage for any intent carried through a
                // rollback; successful later phases get their own retry owner.
                var before = previous != null ? previous.Before : entry.State == EntityState.Added ? null : entry.GetDatabaseValues();
                var values = entry.CurrentValues.Clone(); var originals = entry.OriginalValues.Clone();
                var state = entry.State;
                if (previous != null && entry.State != EntityState.Deleted)
                {
                    if (previous.State == EntityState.Deleted)
                        throw new InvalidOperationException("Bid retry cannot replace a retained deletion with an update.");
                    if (entry.State != EntityState.Added)
                    {
                        values = previous.Values.Clone(); originals = previous.Originals.Clone();
                        foreach (string name in modified)
                        { values[name] = entry.CurrentValues[name]; originals[name] = entry.OriginalValues[name]; }
                    }
                    if (previous.State == EntityState.Added) state = EntityState.Added;
                    modified = previous.Modified.Union(modified).Order().ToArray();
                }
                if (previous != null) rows.Remove(previous);
                rows.Add(new(entry.Metadata, state, keys, values, originals, before, modified));
            }
        }

        internal void ProvenRollback(CharacterContext context)
        {
            Require(context);
            if (!context.IsProvenBidRollback) throw new InvalidOperationException("Bid retry has no terminal rollback proof.");
            retry = true; attempted = null; provenContextId = context.ContextId.ToString();
        }

        internal void RetireDeliveryItem(CharacterContext context, ItemModel expected, ItemModel allowedAfter, bool commodityQuantity = false)
        {
            if (!retry || attempted != null || context == null || !context.IsProvenBidRollback
                || provenContextId != context.ContextId.ToString() || context.AtomicCharacterId != character
                || database == null || expected?.OwnerId != character || allowedAfter?.OwnerId != character)
                throw new InvalidOperationException("Delivery intent retirement requires its exact proven rollback owner.");
            var row = rows.SingleOrDefault(candidate => candidate.Type.ClrType == typeof(ItemModel)
                && candidate.Keys.Length == 1 && Equals(candidate.Keys[0], expected.Id));
            if (row == null) return;
            if (row.State != EntityState.Modified || row.Before == null || row.Modified.Any(name => name is not
                (nameof(ItemModel.OwnerId) or nameof(ItemModel.Location) or nameof(ItemModel.BagIndex)
                    or nameof(ItemModel.MicrochipIds) or nameof(ItemModel.RuneSlots) or nameof(ItemModel.StackCount))))
                throw new InvalidOperationException("Delivery retirement cannot discard unrelated Item intent.");
            foreach (var property in row.Before.Properties)
            {
                object before = row.Before[property.Name];
                bool Equal(object first, object second) => property.GetValueComparer()?.Equals(first, second) ?? Equals(first, second);
                if (property.PropertyInfo == null || !Equal(before, property.PropertyInfo.GetValue(expected))
                    || !Equal(row.Values[property.Name], property.PropertyInfo.GetValue(allowedAfter))
                    || property.Name is not (nameof(ItemModel.MicrochipIds) or nameof(ItemModel.RuneSlots)) && !Equal(before, row.Values[property.Name])
                        && !(commodityQuantity && property.Name == nameof(ItemModel.StackCount)
                            && Convert.ToUInt32(row.Values[property.Name]) > Convert.ToUInt32(before)))
                    throw new InvalidOperationException("Delivery retirement lost its exact captured Item preimage.");
            }
            // The owned delivery forced unchanged custody columns plus the
            // established socket codecs. Other rows and gameplay changes remain.
            rows.Remove(row);
        }

        internal void RetireCommodityMaterial(CharacterContext context, ushort material, ushort? before, ushort? after)
        {
            if (!retry || attempted != null || context == null || !context.IsProvenBidRollback
                || provenContextId != context.ContextId.ToString() || context.AtomicCharacterId != character || database == null)
                throw new InvalidOperationException("Material retirement requires its exact proven rollback owner.");
            var row = rows.SingleOrDefault(candidate => candidate.Type.ClrType == typeof(CharacterTradeskillMaterialModel)
                && candidate.Keys.SequenceEqual(new object[] { character, material }));
            if (row == null) return;
            if (!after.HasValue || row.State != (before.HasValue ? EntityState.Modified : EntityState.Added)
                || row.Modified.Any(name => name != nameof(CharacterTradeskillMaterialModel.Amount))
                || !Equals(row.Values[nameof(CharacterTradeskillMaterialModel.Id)], character)
                || !Equals(row.Values[nameof(CharacterTradeskillMaterialModel.MaterialId)], material)
                || !Equals(row.Values[nameof(CharacterTradeskillMaterialModel.Amount)], after.Value)
                || before.HasValue && (row.Before == null
                    || !Equals(row.Before[nameof(CharacterTradeskillMaterialModel.Id)], character)
                    || !Equals(row.Before[nameof(CharacterTradeskillMaterialModel.MaterialId)], material)
                    || !Equals(row.Before[nameof(CharacterTradeskillMaterialModel.Amount)], before.Value)))
                throw new InvalidOperationException("Material retirement cannot discard another captured intent.");
            rows.Remove(row);
        }

        private void Require(CharacterContext context)
        {
            if (!ReferenceEquals(attempted, context) || context.AtomicCharacterId != character
                || CharacterDatabaseIdentity.Get(context) != database)
                throw new InvalidOperationException("Bid retry lost its retained enrollment owner.");
        }
        private static bool SameKey(EntityEntry entry, Row row) => entry.Metadata.Name == row.Type.Name
            && entry.Metadata.FindPrimaryKey().Properties.Select(property => entry.Property(property.Name).CurrentValue).SequenceEqual(row.Keys);
        private static bool Same(PropertyValues first, PropertyValues second) => first.Properties.All(property =>
            property.GetValueComparer()?.Equals(first[property.Name], second[property.Name]) ?? Equals(first[property.Name], second[property.Name]));
    }
}
