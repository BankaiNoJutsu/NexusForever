using System;
using System.Data;
using System.Linq;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Storage;
using NexusForever.Database.Character.Model;

namespace NexusForever.Database.Character
{
    // Freezes the complete enrolled normal-save graph, including ordinary reward
    // managers. No actor callback is retained or invoked by this validation.
    internal sealed class CharacterSaveGraph
    {
        private readonly CharacterContext context;
        private readonly IDbContextTransaction transaction;
        private readonly Row[] rows;
        private bool flushed, terminal;
        internal string ReceiptPayload { get; }
        private sealed record Row(EntityEntry Entry, EntityState State, PropertyValues Values, PropertyValues Originals,
            PropertyValues Target, string[] Modified, string[] Generated);

        internal static CharacterSaveGraph Seal(CharacterContext context, CharacterSaveReconciliation receipt = null)
            => new(context, receipt);
        private CharacterSaveGraph(CharacterContext context, CharacterSaveReconciliation receipt)
        {
            this.context = context;
            Require(context.AtomicCharacterId.HasValue && !context.IsSaveRejected && !context.HasSavepointRollback
                && context.Database.CurrentTransaction?.GetDbTransaction().IsolationLevel == IsolationLevel.Serializable, "Atomic graph lost its transaction.");
            transaction = context.Database.CurrentTransaction; context.ChangeTracker.DetectChanges();
            context.EnrollTransactionParticipants();
            rows = context.ChangeTracker.Entries().Select(Capture).ToArray();
            if (receipt != null)
            {
                ReceiptPayload = System.Text.Json.JsonSerializer.Serialize(new
                {
                    Kind = receipt.Kind, Version = 1, Character = context.AtomicCharacterId.Value,
                    Database = MarketplaceBidReconciliation.Hash(CharacterDatabaseIdentity.Get(context)), Receipt = (object)receipt,
                    Participants = context.CaptureParticipantReceiptTargets(),
                    Rows = rows.Select(row => new
                    {
                        Type = row.Entry.Metadata.Name, State = row.State.ToString(), Columns = row.Modified,
                        Before = Values(row.Originals), Target = row.State == EntityState.Deleted ? null : Values(row.Target)
                    }).OrderBy(row => row.Type).ThenBy(row => System.Text.Json.JsonSerializer.Serialize(row.Before)).ToArray()
                });
                var now = DateTime.UtcNow;
                var witness = receipt.AddReceipt(context, ReceiptPayload, new DateTime(now.Ticks / 10 * 10, DateTimeKind.Utc));
                rows = rows.Append(Capture(context.Entry(witness))).ToArray();
            }
            context.OnSaveValidation(Validate); context.SavedChanges += OnFlushed;
            context.OnSaveSettled(_ => { terminal = true; context.SavedChanges -= OnFlushed; });
            context.AtomicGraphSealed = true;

            Row Capture(EntityEntry entry)
            {
                ValidateCharacterOwner(context, entry, entry.State == EntityState.Deleted);
                var values = entry.CurrentValues.Clone(); var original = entry.OriginalValues.Clone();
                var target = entry.State == EntityState.Added ? values.Clone() : entry.GetDatabaseValues();
                Require(entry.State == EntityState.Added || target != null, "An enrolled Character row disappeared.");
                var modified = entry.Properties.Where(property => property.IsModified).Select(property => property.Metadata.Name).Order().ToArray();
                if (entry.State == EntityState.Modified) foreach (string name in modified) target[name] = values[name];
                var generated = entry.Properties.Where(property => property.IsTemporary || property.Metadata.ValueGenerated == ValueGenerated.OnAddOrUpdate
                    || entry.State == EntityState.Added && property.Metadata.ValueGenerated == ValueGenerated.OnAdd
                        && Equals(property.CurrentValue, property.Metadata.Sentinel))
                    .Select(property => property.Metadata.Name).ToArray();
                if (entry.State != EntityState.Deleted) EncodeTarget(target,
                    (entry.State == EntityState.Added ? target.Properties.Select(property => property.Name) : modified).Except(generated).ToHashSet());
                return new Row(entry, entry.State, values, original, target, modified, generated);
            }
            static SortedDictionary<string, object> Values(PropertyValues values) => new(values.Properties
                .ToDictionary(property => property.Name, property => values[property.Name]), StringComparer.Ordinal);
        }
        internal static void ValidateCharacterOwner(CharacterContext context, EntityEntry entry, bool deleted)
        {
            if (context.GuildCreationOwner?.Authorizes(entry) == true) return;
            if (context.CommunityCreationOwner?.Authorizes(entry) == true) return;
            if (context.HousingRenameOwner?.Authorizes(entry) == true) return;
            if (context.HousingDecorPurchaseOwner?.Authorizes(entry) == true) return;
            if (entry.Entity is RealmFirstAchievementModel { IsGuild: false, OwnerId: {} winner })
                Require(winner == context.AtomicCharacterId.Value, "Atomic Character claim belongs to another winner.");
            // A proven bank rollback may leave an Item in its original neutral
            // mail custody. Only that retained complete parent graph can admit
            // its ordinary metadata save; general Character ownership is intact.
            if (entry.Entity is ItemModel item && item.OwnerId == null && item.Location == ushort.MaxValue
                && entry.State is EntityState.Modified or EntityState.Unchanged
                && entry.OriginalValues[nameof(ItemModel.OwnerId)] == null
                && Convert.ToUInt16(entry.OriginalValues[nameof(ItemModel.Location)]) == ushort.MaxValue
                && context.BankRowOwner?.Invoke(entry) == true)
            {
                // The callback can narrow this permission, never expand it to
                // another Character, owner deletion or unlinked/foreign mail.
                void Custody()
                {
                    var stored = context.Item.AsNoTracking().SingleOrDefault(row => row.Id == item.Id);
                    var links = context.CharacterMailAttachment.AsNoTracking().Where(row => row.ItemGuid == item.Id).ToArray();
                    Require(stored != null && stored.OwnerId == null && stored.Location == ushort.MaxValue && stored.ItemId == item.ItemId
                        && links.Length == 1 && context.CharacterMail.AsNoTracking().Any(mail => mail.Id == links[0].Id
                            && mail.RecipientId == context.AtomicCharacterId.Value)
                        && !context.RealmBankItem.AsNoTracking().Any(row => row.Id == item.Id),
                        "Bank ordinary Item lost its original neutral mail custody.");
                }
                Custody(); context.OnSaveValidation(_ => Custody()); return;
            }
            foreach (var property in CharacterKeys(entry.Metadata, []))
                Require(Convert.ToUInt64(entry.Property(property.Name).CurrentValue) == context.AtomicCharacterId.Value, "Atomic Character graph contains another character's rows.");
            Require(entry.Metadata.ClrType != typeof(CharacterModel) || !deleted, "Atomic Character save cannot delete its owner.");
        }
        // Resolve ownership through mapped FKs (including objective -> quest ->
        // character), instead of assuming every model's Id is a character Id.
        private static IEnumerable<IProperty> CharacterKeys(IEntityType type, HashSet<IEntityType> seen)
        {
            if (!seen.Add(type)) return [];
            if (type.ClrType == typeof(CharacterModel)) return [type.FindProperty(nameof(CharacterModel.Id))];
            return type.GetForeignKeys().SelectMany(foreign => CharacterKeys(foreign.PrincipalEntityType, new(seen))
                .Select(key => Array.IndexOf(foreign.PrincipalKey.Properties.ToArray(), key))
                .Where(index => index >= 0).Select(index => foreign.Properties[index])).Distinct().ToArray();
        }
        private void EncodeTarget(PropertyValues target, IReadOnlySet<string> written)
        {
            if (context.Database.ProviderName?.Contains("MySql", StringComparison.Ordinal) != true) return;
            foreach (var property in target.Properties)
            {
                if (!written.Contains(property.Name) || target[property.Name] is not DateTime instant) continue;
                string store = property.GetColumnType()?.ToLowerInvariant();
                // These are the actual mapped Character date types. The server
                // performs its own DATETIME conversion, including sql_mode.
                if (store != "datetime" && store != "datetime(6)") throw new InvalidOperationException("Unsupported atomic Character date storage.");
                target[property.Name] = store == "datetime"
                    ? context.Database.SqlQueryRaw<DateTime>("SELECT CAST({0} AS DATETIME) AS Value", instant).AsEnumerable().Single()
                    : context.Database.SqlQueryRaw<DateTime>("SELECT CAST({0} AS DATETIME(6)) AS Value", instant).AsEnumerable().Single();
            }
        }
        private void OnFlushed(object sender, SavedChangesEventArgs args)
        {
            if (args.EntitiesSavedCount > 0 || rows.All(row => row.State == EntityState.Unchanged)) flushed = true;
            foreach (var row in rows)
                foreach (string name in row.Generated)
                {
                    row.Values[name] = row.Entry.CurrentValues[name];
                    row.Target[name] = row.Entry.CurrentValues[name];
                }
        }
        private void Validate(CharacterSaveValidationPhase phase)
        {
            Require(!terminal && !context.HasSavepointRollback && ReferenceEquals(context.Database.CurrentTransaction, transaction), "Atomic Character graph changed its owner.");
            foreach (var row in rows)
            {
                var entry = row.Entry; bool deleted = row.State == EntityState.Deleted;
                Require((deleted ? entry.State == EntityState.Deleted || flushed && entry.State == EntityState.Detached
                        : entry.State != EntityState.Detached && entry.State != EntityState.Deleted)
                    && (flushed || entry.State == row.State) && Same(entry.CurrentValues, row.Values)
                    && (flushed || Same(entry.OriginalValues, row.Originals)), "Atomic Character target or ownership changed.");
                if (!flushed) Require(entry.Properties.Where(property => property.IsModified).Select(property => property.Metadata.Name).Order().SequenceEqual(row.Modified), "An atomic Character write was suppressed.");
                if (phase == CharacterSaveValidationPhase.BeforeExplicitCommit)
                {
                    Require(flushed, "Atomic Character graph was not flushed."); var stored = entry.GetDatabaseValues();
                    Require(deleted ? stored == null : stored != null && Same(stored, row.Target), "Atomic Character SQL differs from its complete enrolled target.");
                }
            }
            Require(context.ChangeTracker.Entries().All(entry => rows.Any(row => ReferenceEquals(row.Entry.Entity, entry.Entity))), "A foreign entity entered the sealed Character graph.");
        }
        private static bool Same(PropertyValues first, PropertyValues second)
            => first.Properties.All(property => property.GetValueComparer()?.Equals(first[property.Name], second[property.Name]) ?? Equals(first[property.Name], second[property.Name]));
        private static void Require(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
    }
}
