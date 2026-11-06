using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using NexusForever.Database.Character;
using NexusForever.Database.Character.Model;
using NexusForever.Game.Entity;

namespace NexusForever.Game.Achievement
{
    // Immutable intent and delivery evidence share the existing retained receipt
    // store. The two non-hex prefixes separate these keys from finance receipts.
    // Neither guild deletion nor a later membership change cancels accepted credit.
    internal static class GuildAchievementFeedbackReceipt
    {
        internal sealed record Intent(string Protocol, string Database, ulong Character, uint Account, ushort Realm,
            ulong Guild, ushort[] Parents, string Plan);
        internal sealed record Stored(string Id, string Payload, DateTime Created);
        private sealed class Unit(Stored before, Stored target, CharacterColumnSaveIntent guard)
        {
            internal readonly Stored Before = before, Target = target;
            internal readonly CharacterColumnSaveIntent Guard = guard;
            internal bool Flushed;
        }
        internal const string Protocol = "guild-character-achievement/4";
        private static readonly ConditionalWeakTable<CharacterContext, Dictionary<string, Unit>> saves = new();
        internal static string Prefix(ulong character) => $"g{character:x16}";
        internal static string DeliveryId(string id) => "h" + id[1..];
        private static bool IsOurs(string id) => id?.Length == 32 && id[0] is 'g' or 'h';
        internal static Stored Create(Intent intent)
        {
            // 60 random bits per recipient, with collision detection on insertion.
            string id = Prefix(intent.Character) + Convert.ToHexStringLower(RandomNumberGenerator.GetBytes(8))[..15];
            DateTime now = DateTime.UtcNow;
            return new(id, JsonSerializer.Serialize(intent), new DateTime(now.Ticks - now.Ticks % TimeSpan.TicksPerSecond, DateTimeKind.Utc));
        }
        internal static Intent Decode(Stored row)
        {
            Intent intent = JsonSerializer.Deserialize<Intent>(row.Payload)
                ?? throw new InvalidOperationException("Guild achievement feedback has no intent.");
            if (intent.Protocol != Protocol || !row.Id.StartsWith(Prefix(intent.Character), StringComparison.Ordinal)
                || row.Id.Length != 32 || intent.Guild == 0 || intent.Parents == null || intent.Parents.Length == 0
                || intent.Parents.Any(id => id == 0) || intent.Parents.Distinct().Count() != intent.Parents.Length || string.IsNullOrEmpty(intent.Plan))
                throw new InvalidOperationException("Guild achievement feedback changed its typed identity.");
            return intent;
        }
        private static Stored Read(CharacterContext context, string id)
            => context.BankReconciliationReceipt.AsNoTracking().Where(row => row.Id == id)
                .Select(row => new Stored(row.Id, row.Payload, row.CreatedUtc)).SingleOrDefault();
        internal static Stored[] ReadPending(Func<CharacterContext> contexts, ulong character, uint account, ushort realm, string database)
        {
            using var context = contexts();
            CharacterDatabaseIdentity.Require(context, database);
            if (!context.Character.Any(row => row.Id == character && row.AccountId == account && row.DeleteTime == null))
                throw new InvalidOperationException("Guild feedback recovery lost its Character recipient.");
            string prefix = Prefix(character);
            Stored[] rows = context.BankReconciliationReceipt.AsNoTracking().Where(row => row.Id.StartsWith(prefix))
                .OrderBy(row => row.CreatedUtc).ThenBy(row => row.Id).Select(row => new Stored(row.Id, row.Payload, row.CreatedUtc)).ToArray();
            string deliveryPrefix = "h" + prefix[1..];
            var deliveries = context.BankReconciliationReceipt.AsNoTracking().Where(row => row.Id.StartsWith(deliveryPrefix))
                .Select(row => new Stored(row.Id, row.Payload, row.CreatedUtc)).ToDictionary(row => row.Id);
            var pending = new List<Stored>();
            foreach (Stored row in rows)
            {
                Intent intent = Decode(row);
                if (intent.Character != character || intent.Account != account || intent.Realm != realm || intent.Database != database)
                    throw new InvalidOperationException("Guild feedback recovery changed its accepted recipient or database.");
                if (deliveries.TryGetValue(DeliveryId(row.Id), out Stored delivered))
                {
                    if (delivered != row with { Id = delivered.Id })
                        throw new InvalidOperationException("Guild feedback recovery found a conflicting delivery receipt.");
                }
                else pending.Add(row);
            }
            return pending.ToArray();
        }
        internal static void StageIntent(CharacterContext context, Stored row, ulong guild, ushort[] completed, Action committed)
        {
            Intent intent = Decode(row);
            CharacterDatabaseIdentity.Require(context, intent.Database);
            if (intent.Guild != guild || intent.Parents.Any(parent => !completed.Contains(parent)))
                throw new InvalidOperationException("Guild feedback lost its captured parent completion.");
            Enrol(context, row, phase =>
            {
                if (phase != CharacterSaveValidationPhase.BeforeFlush
                    && intent.Parents.Any(id => !context.GuildAchievement.Any(parent => parent.Id == guild && parent.AchievementId == id && parent.DateCompleted != null)))
                    throw new InvalidOperationException("Guild feedback cannot commit without its parent completion.");
            }, committed);
        }
        internal static void StageDelivery(CharacterContext context, Stored root, ulong character, uint account,
            ExperiencePersistence.Achievement[] achievements, Action committed)
        {
            Intent intent = Decode(root);
            CharacterDatabaseIdentity.Require(context, intent.Database);
            if (intent.Character != character || intent.Account != account || Read(context, root.Id) != root
                || !context.Character.Any(row => row.Id == character && row.AccountId == account && row.DeleteTime == null))
                throw new InvalidOperationException("Guild feedback delivery lost its committed intent or recipient.");
            // Use the experience owner's exact projected/retained SQL target.
            // Recasting an old actor timestamp under a later session mode can
            // disagree with the date that already committed.
            var expected = achievements.ToArray();
            Enrol(context, root with { Id = DeliveryId(root.Id) }, phase =>
            {
                if (Read(context, root.Id) != root || !context.Character.Any(row => row.Id == character && row.AccountId == account && row.DeleteTime == null))
                    throw new InvalidOperationException("Guild feedback delivery changed its committed intent.");
                if (phase != CharacterSaveValidationPhase.BeforeFlush)
                {
                    var stored = context.CharacterAchievement.AsNoTracking().Where(row => row.Id == character).AsEnumerable()
                        .Select(ExperiencePersistence.FromModel).OrderBy(row => row.Id).ToArray();
                    if (!stored.SequenceEqual(expected))
                        throw new InvalidOperationException("Guild feedback delivery lost its frozen Character achievement graph.");
                }
            }, committed);
        }
        private static void Enrol(CharacterContext context, Stored target, Action<CharacterSaveValidationPhase> validate, Action committed)
        {
            try
            {
                if (context.IsSaveRejected || context.HasSavepointRollback)
                    throw new InvalidOperationException("Guild feedback requires a fresh transaction context.");
                Dictionary<string, Unit> owned = saves.GetOrCreateValue(context);
                if (owned.TryGetValue(target.Id, out Unit existing))
                {
                    if (existing.Target != target) throw new InvalidOperationException("Guild feedback has conflicting save intents.");
                    return;
                }
                Stored before = Read(context, target.Id);
                if (before != null && before != target)
                    throw new InvalidOperationException("Guild feedback receipt differs from its retained immutable intent.");
                var local = context.BankReconciliationReceipt.Local.SingleOrDefault(row => row.Id == target.Id);
                if (local != null && (before == null || context.Entry(local).State != EntityState.Unchanged
                    || new Stored(local.Id, local.Payload, local.CreatedUtc) != target))
                    throw new InvalidOperationException("Guild feedback receipt belongs to another tracked intent.");
                if (local == null && before == null)
                {
                    local = new() { Id = target.Id, Payload = target.Payload, CreatedUtc = target.Created };
                    context.BankReconciliationReceipt.Add(local);
                }
                CharacterColumnSaveIntent guard = local == null ? null : new(context.Entry(local),
                    [nameof(BankReconciliationReceiptModel.Id), nameof(BankReconciliationReceiptModel.Payload), nameof(BankReconciliationReceiptModel.CreatedUtc)], false, "Guild achievement feedback");
                guard?.Capture();
                var unit = new Unit(before, target, guard); owned.Add(target.Id, unit);
                context.OnSaveValidation(phase =>
                {
                    if (context.HasSavepointRollback) return;
                    if (context.Database.CurrentTransaction == null && context.Database.AutoTransactionBehavior == AutoTransactionBehavior.Never)
                        throw new InvalidOperationException("Guild feedback cannot bypass its graph transaction.");
                    context.ChangeTracker.DetectChanges();
                    foreach (var entry in context.ChangeTracker.Entries<BankReconciliationReceiptModel>().Where(entry => IsOurs(entry.Entity.Id)))
                        if (!owned.TryGetValue(entry.Entity.Id, out Unit owner) || !ReferenceEquals(owner.Guard?.Entry.Entity, entry.Entity))
                            throw new InvalidOperationException("Guild feedback contains an unenrolled tracked receipt.");
                    guard?.Validate();
                    if (Read(context, target.Id) != (phase == CharacterSaveValidationPhase.BeforeFlush && !unit.Flushed ? before : target))
                        throw new InvalidOperationException("Guild feedback SQL differs from its captured receipt.");
                    validate(phase);
                });
                EventHandler<SavedChangesEventArgs> flushed = (_, _) =>
                {
                    if (context.HasSavepointRollback || Read(context, target.Id) != target) return;
                    unit.Flushed = true;
                    if (guard != null) { guard.Capture(); guard.Flushed = true; }
                };
                context.SavedChanges += flushed;
                context.OnSaveCompleted(success =>
                {
                    try
                    {
                        context.SavedChanges -= flushed;
                        if (success && unit.Flushed && !context.HasSavepointRollback) committed?.Invoke();
                    }
                    finally { owned.Remove(target.Id); }
                });
            }
            catch (Exception error) { context.RejectSave(error); throw; }
        }
    }
}
