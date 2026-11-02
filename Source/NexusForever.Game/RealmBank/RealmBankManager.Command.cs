using System.Data;
using System.Runtime.CompilerServices;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata;
using MySqlConnector;
using NexusForever.Database;
using NexusForever.Database.Character;
using NexusForever.Database.Character.Model;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract;
using NexusForever.Game.Entity;
using NexusForever.Game.Static.Entity;

namespace NexusForever.Game.RealmBank
{
    public sealed partial class RealmBankManager
    {
        private readonly Func<BankSaveCommand, Task> bankExecution;
        internal RealmBankManager(IDatabaseManager databaseManager, IItemManager itemManager, Func<BankSaveCommand, Task> execution)
            : this(databaseManager, itemManager) => bankExecution = execution;

        internal Task SaveInventoryOperationAsync(IItem[] items, Item[] splitCreations, IPlayer player, Action<CharacterContext> companion = null, IReadOnlySet<ulong> detached = null, bool allowEmpty = false)
            => SaveBankUnit(items, player, context =>
            {
                foreach (Item item in splitCreations) item.ValidateSplitCreation(context);
            }, companion, detached, allowEmpty);

        internal async Task<BankOutcome> SaveReconciledInventoryOperationAsync(IItem[] items, Item[] splitCreations, IPlayer player,
            Action<CharacterContext> companion, IReadOnlySet<ulong> detached, bool allowEmpty)
        {
            var database = GetDatabase() ?? throw new InvalidOperationException("Bank reconciliation requires its Character database.");
            var command = new BankSaveCommand(database.CreateContext, items, player,
                context => { foreach (var item in splitCreations) item.ValidateSplitCreation(context); }, companion, detached, allowEmpty);
            try
            {
                if (bankExecution == null) await command.ExecuteAsync();
                else await (bankExecution(command) ?? throw new InvalidOperationException("Bank executor returned no task."));
            }
            catch (Exception error) { NLog.LogManager.GetCurrentClassLogger().Warn(error, "Bank operation requires complete SQL outcome reconciliation."); }
            return await command.ResolveAsync();
        }

        internal sealed class BankSaveCommand
        {
            private sealed record Entry(IItem Item, Item.BankSaveSnapshot Snapshot, ItemModel Adapter,
                bool Bank, bool Consumed, bool Detached, ulong Id, uint Definition);
            private readonly Func<CharacterContext> contexts;
            private readonly Entry[] entries;
            private readonly uint account;
            private readonly ushort realm;
            private readonly ulong character;
            private readonly Action<CharacterContext> firstStage;
            private readonly Action<CharacterContext> companion;
            private static readonly object contextGate = new();
            private static readonly ConditionalWeakTable<CharacterContext, object> contextOwners = new();
            private int started, activeAttempt;
            private bool validatedCreation;
            private CommitGraph retainedGraph;
            private string retainedDatabase;
            private readonly string operation = Guid.NewGuid().ToString("N");

            // An unread COMMIT response can leave every enrolled row before or
            // after the unit. Never reconcile a mixture of those states.
            internal sealed class CommitGraph
            {
                private sealed record Row(EntityEntry Entry, EntityState State, PropertyValues Before)
                {
                    internal PropertyValues After { get; set; }
                }
                private readonly Row[] rows;
                internal CommitGraph(CharacterContext context) { rows = Capture(context); }
                private readonly object viewGate = new();
                private Dictionary<Row, PropertyValues> ordinary;
                private CommitGraph(Row[] rows) { this.rows = rows; }
                internal CommitGraph Freeze() => new(rows.Select(row => row with { Entry = null, Before = row.Before?.Clone(), After = row.After?.Clone() }).ToArray());
                internal string Fingerprint
                {
                    get
                    {
                        var payload = System.Text.Json.JsonSerializer.Serialize(rows.Select(row => new
                        {
                            Type = (row.After ?? row.Before).ToObject().GetType().FullName,
                            Before = Values(row.Before), After = Values(row.After)
                        }).OrderBy(row => row.Type).ThenBy(row => System.Text.Json.JsonSerializer.Serialize(row.Before)));
                        return Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(payload)));
                    }
                }
                private static SortedDictionary<string, object> Values(PropertyValues values) => values == null ? null
                    : new(values.Properties.ToDictionary(property => property.Name, property => values[property.Name]), StringComparer.Ordinal);
                private static Row[] Capture(CharacterContext context)
                {
                    context.ChangeTracker.DetectChanges();
                    return context.ChangeTracker.Entries().Select(entry => new Row(entry, entry.State,
                        entry.State == EntityState.Added ? null : entry.OriginalValues.Clone())).ToArray();
                }
                internal void Flushed()
                {
                    foreach (var row in rows)
                        row.After = row.State == EntityState.Deleted ? null : row.Entry.CurrentValues.Clone();
                }
                internal async Task<bool> Committed(CharacterContext probe)
                {
                    bool before = true, after = true;
                    foreach (var row in rows)
                    {
                        var actual = await probe.Entry((row.After ?? row.Before).ToObject()).GetDatabaseValuesAsync();
                        before &= Same(row.Before, actual); after &= Same(row.After, actual);
                    }
                    if (before == after) throw new InvalidOperationException("Bank outcome is mixed, foreign or indistinguishable from a no-op.");
                    return after;
                }
                internal CharacterCurrencyModel Currency(bool committed, ulong character, byte currency)
                {
                    var row = rows.SingleOrDefault(value => (value.After ?? value.Before).ToObject() is CharacterCurrencyModel model
                        && model.Id == character && model.CurrencyId == currency)
                        ?? throw new InvalidOperationException("Bank outcome lost its complete currency row.");
                    return (CharacterCurrencyModel)(committed ? row.After : row.Before)?.ToObject();
                }
                internal bool OwnsNeutralMailItem(EntityEntry entry, bool committed, ulong character)
                {
                    if (entry.Entity is not ItemModel item || item.OwnerId != null || item.Location != (ushort)InventoryLocation.None
                        || entry.State is EntityState.Added or EntityState.Deleted) return false;
                    object[] selected = rows.Select(row => (committed ? row.After : row.Before)?.ToObject()).Where(value => value != null).ToArray();
                    if (!selected.OfType<ItemModel>().Any(row => row.Id == item.Id && row.OwnerId == null && row.Location == (ushort)InventoryLocation.None)) return false;
                    return selected.OfType<CharacterMailAttachmentModel>().Any(link => link.ItemGuid == item.Id
                        && selected.OfType<CharacterMailModel>().Any(mail => mail.Id == link.Id && mail.RecipientId == character));
                }
                internal void Guard(CharacterContext probe, bool committed)
                {
                    // The normal actor save may change enrolled rows. Retain the
                    // parent state for every other bank participant as well.
                    lock (viewGate) ordinary ??= rows.ToDictionary(row => row, row => (committed ? row.After : row.Before)?.Clone());
                    var expected = rows.Select(row =>
                    {
                        PropertyValues parent; lock (viewGate) parent = ordinary[row]?.Clone();
                        var identity = row.After ?? row.Before;
                        var entity = identity.ToObject();
                        var key = probe.Entry(entity).Metadata.FindPrimaryKey();
                        var tracked = probe.ChangeTracker.Entries().SingleOrDefault(entry => entry.Metadata.ClrType == entity.GetType()
                            && key.Properties.All(property => Equals(entry.Property(property.Name).CurrentValue, identity[property.Name])));
                        return (Row: row, Identity: entity, Parent: parent, Target: tracked == null ? parent
                            : tracked.State == EntityState.Deleted ? null : tracked.CurrentValues.Clone());
                    }).ToArray();
                    probe.OnSaveValidation(phase =>
                    {
                        foreach (var row in expected)
                        {
                            var actual = probe.Entry(row.Identity).GetDatabaseValues();
                            if (!Same(phase == CharacterSaveValidationPhase.BeforeFlush ? row.Parent : row.Target, actual))
                                throw new InvalidOperationException("Bank parent or ordinary reconciliation SQL changed its complete outcome.");
                        }
                    });
                    probe.OnSaveCompleted(success =>
                    {
                        if (!success) return;
                        lock (viewGate) foreach (var row in expected) ordinary[row.Row] = row.Target?.Clone();
                    });
                }
                internal void ValidateParent(CharacterContext context, bool committed)
                {
                    lock (viewGate)
                    {
                        ordinary ??= rows.ToDictionary(row => row, row => (committed ? row.After : row.Before)?.Clone());
                        foreach (var row in rows)
                            if (!Same(ordinary[row], context.Entry((row.After ?? row.Before).ToObject()).GetDatabaseValues()))
                                throw new InvalidOperationException("Bank reconciliation lost its complete parent SQL state.");
                    }
                }
                internal async Task RequireCompleteState(CharacterContext probe)
                {
                    bool before = true, after = true;
                    foreach (var row in rows)
                    {
                        var identity = (row.After ?? row.Before).ToObject();
                        var actual = await probe.Entry(identity).GetDatabaseValuesAsync();
                        before &= Same(row.Before, actual); after &= Same(row.After, actual);
                    }
                    if (!before && !after)
                        throw new InvalidOperationException("Bank COMMIT outcome differs from its complete captured graph.");
                }
                private static bool Same(PropertyValues first, PropertyValues second) => first == null ? second == null
                    : second != null && first.Properties.All(property =>
                        property.GetValueComparer()?.Equals(first[property.Name], second[property.Name]) ?? Equals(first[property.Name], second[property.Name]));
            }

            internal BankSaveCommand(Func<CharacterContext> contexts, IItem[] items, IPlayer player, Action<CharacterContext> firstStage = null, Action<CharacterContext> companion = null, IReadOnlySet<ulong> detached = null, bool allowEmpty = false)
            {
                this.contexts = contexts ?? throw new ArgumentNullException(nameof(contexts));
                account = player.Account.Id; realm = player.Identity.RealmId; character = player.CharacterId;
                if (character == 0 || items.Length == 0 && !(allowEmpty && companion != null) || items.Select(item => item.Guid).Distinct().Count() != items.Length)
                    throw new InvalidOperationException("Bank command requires distinct Items and one Character owner.");
                this.firstStage = firstStage;
                this.companion = companion;
                entries = items.Select(item =>
                {
                    var snapshot = item is Item concrete ? new Item.BankSaveSnapshot(concrete) : null;
                    ItemModel model = snapshot?.Model ?? CaptureAdapter(item, character);
                    bool consumed = model.Location == (ushort)InventoryLocation.None && (snapshot?.Deleted ?? item.PendingDelete);
                    bool mail = detached?.Contains(model.Id) == true;
                    if (mail ? snapshot == null || model.OwnerId != null || model.Location != (ushort)InventoryLocation.None || consumed
                        : model.OwnerId != character || model.Location == (ushort)InventoryLocation.None && !consumed)
                        throw new InvalidOperationException("Bank command lost its Item owner or deletion intent.");
                    return new Entry(item, snapshot, snapshot == null ? model : null,
                        model.Location == (ushort)InventoryLocation.RealmBank, consumed, mail, model.Id, model.ItemId);
                }).ToArray();
            }

            private CharacterContext NewContext()
            {
                CharacterContext context = contexts() ?? throw new InvalidOperationException("Bank context factory returned null.");
                // Never dispose a caller's already-owned context after a bad factory.
                lock (contextGate)
                {
                    if (contextOwners.TryGetValue(context, out _) || context.Database.CurrentTransaction != null
                        || context.ChangeTracker.Entries().Any() || context.IsSaveRejected || context.HasSavepointRollback)
                        throw new InvalidOperationException("Bank retries require fresh, exclusively owned contexts.");
                    contextOwners.Add(context, new object());
                }
                return context;
            }

            // The optional execution seam admits deterministic transient/ack-loss
            // replays against real SQL. Production uses the configured EF strategy.
            internal async Task ExecuteAsync(Func<Func<Task>, Task> execution = null)
            {
                if (Interlocked.Exchange(ref started, 1) != 0)
                    throw new InvalidOperationException("A bank command can only be executed once.");
                await using var driver = NewContext();
                string database = Item.GetRewardDatabaseKey(driver);
                retainedDatabase = database;
                Exception lostAcknowledgement = null;
                CommitGraph uncertain = null;
                if (execution == null)
                {
                    var strategy = driver.Database.CreateExecutionStrategy();
                    try { await strategy.ExecuteAsync(() => Attempt(null)); }
                    catch (Exception error) when (ReferenceEquals(error, lostAcknowledgement))
                    {
                        // Connector error0 with an IO cause is not classified as
                        // transient. Resolve once through the same frozen command,
                        // under fresh transaction locks and complete graph checks.
                        await strategy.ExecuteAsync(() => Attempt(uncertain));
                    }
                }
                else await execution(() => Attempt(null));

                async Task Attempt(CommitGraph recovery)
                {
                    if (Interlocked.Exchange(ref activeAttempt, 1) != 0)
                        throw new InvalidOperationException("Bank attempts cannot overlap.");
                    try
                    {
                        lostAcknowledgement = null;
                        await using var context = NewContext();
                        if (Item.GetRewardDatabaseKey(context) != database)
                            throw new InvalidOperationException("Bank command changed its retained database.");
                        await using var transaction = await context.Database.BeginTransactionAsync(IsolationLevel.Serializable);
                        bool mysql = context.Database.ProviderName?.Contains("MySql", StringComparison.Ordinal) == true;
                        if (!mysql && !context.Database.IsSqlite()) throw new InvalidOperationException("Unsupported bank provider.");
                        var owner = await (mysql
                            ? context.Character.FromSqlInterpolated($"SELECT * FROM `character` WHERE id={character} FOR UPDATE")
                            : context.Character.Where(row => row.Id == character)).AsNoTracking().SingleOrDefaultAsync();
                        if (owner == null || owner.DeleteTime != null || owner.AccountId != account)
                            throw new InvalidOperationException("Bank Character/account owner is no longer current.");
                        if (recovery != null) await recovery.RequireCompleteState(context);
                        try
                        {
                            Stage(context);
                            var graph = new CommitGraph(context); graph.Flushed();
                            retainedGraph ??= graph;
                            await context.SaveChangesAsync();
                            graph.Flushed();
                            try { await transaction.CommitAsync(); }
                            catch (MySqlException error) when (error.Number == 0 && error.InnerException is IOException)
                            {
                                uncertain = graph; lostAcknowledgement = error; throw;
                            }
                        }
                        catch (Exception error) { context.RejectSave(error); throw; }
                    }
                    finally { Volatile.Write(ref activeAttempt, 0); }
                }
            }

            private void Stage(CharacterContext context)
            {
                if (!validatedCreation) { firstStage?.Invoke(context); validatedCreation = true; }
                foreach (Entry entry in entries)
                    if (entry.Consumed)
                        StageConsumedItem(context, entry.Item, entry.Adapter, account, realm, character, entry.Id, entry.Definition, entry.Snapshot);
                    else if (entry.Bank)
                        StageBankItem(context, entry.Item, entry.Adapter, account, realm, character, entry.Snapshot);
                    else
                        StageCharacterItem(context, entry.Item, entry.Adapter, account, realm, character, entry.Id, entry.Definition, entry.Snapshot, entry.Detached);
                companion?.Invoke(context);
            }


            internal async Task<BankOutcome> ResolveAsync()
            {
                bool reported = false;
                while (true)
                {
                    try { return await ProbeOutcomeAsync(); }
                    catch (Exception error) when (error is IOException || error is MySqlException { IsTransient: true }
                        || error is MySqlException { Number: 0, InnerException: IOException })
                    {
                        if (!reported)
                        {
                            reported = true;
                            NLog.LogManager.GetCurrentClassLogger().Warn(error, "Bank outcome observation failed; original ownership remains retained.");
                        }
                        await Task.Delay(TimeSpan.FromSeconds(1));
                    }
                }
            }

            private async Task<BankOutcome> ProbeOutcomeAsync()
            {
                if (Volatile.Read(ref activeAttempt) != 0) throw new InvalidOperationException("Cannot reconcile a running bank attempt.");
                Interlocked.Exchange(ref started, 1);
                await using var driver = NewContext();
                string database = Item.GetRewardDatabaseKey(driver);
                if (retainedDatabase != null && retainedDatabase != database) throw new InvalidOperationException("Bank outcome changed its database.");
                retainedDatabase = database;
                return await driver.Database.CreateExecutionStrategy().ExecuteAsync(async () =>
                {
                    await using var probe = NewContext();
                    if (Item.GetRewardDatabaseKey(probe) != database) throw new InvalidOperationException("Bank outcome changed its database.");
                    await using var transaction = await probe.Database.BeginTransactionAsync(IsolationLevel.Serializable);
                    bool mysql = probe.Database.ProviderName?.Contains("MySql", StringComparison.Ordinal) == true;
                    var owner = await (mysql ? probe.Character.FromSqlInterpolated($"SELECT * FROM `character` WHERE id={character} FOR UPDATE")
                        : probe.Character.Where(row => row.Id == character)).AsNoTracking().SingleOrDefaultAsync();
                    if (owner == null || owner.DeleteTime != null || owner.AccountId != account) throw new InvalidOperationException("Bank outcome lost its Character/account owner.");
                    if (retainedGraph == null)
                    {
                        // A factory/executor may fail before enrollment. Preview
                        // the same frozen command without permitting any flush.
                        probe.OnSaveValidation(() => throw new InvalidOperationException("Bank outcome preview cannot write."));
                        Stage(probe);
                        retainedGraph = new(probe); retainedGraph.Flushed();
                    }
                    bool committed = await retainedGraph.Committed(probe);
                    if (committed)
                    {
                        // Re-enroll the proven postimage on the same frozen
                        // command to settle participant owners after a lost ACK.
                        // Only persistence intent runs here, never actor effects.
                        Stage(probe); await probe.SaveChangesAsync();
                        if (!await retainedGraph.Committed(probe)) throw new InvalidOperationException("Bank postimage changed during owner confirmation.");
                        await transaction.CommitAsync();
                    }
                    return new BankOutcome(database, account, character, operation, committed, retainedGraph.Freeze());
                });
            }
        }

        internal sealed record BankOutcome(string Database, uint Account, ulong Character, string Operation, bool Committed, BankSaveCommand.CommitGraph Graph)
        {
            internal CharacterCurrencyModel Currency(byte currency) => Graph.Currency(Committed, Character, currency);
            internal void Guard(CharacterContext context)
            {
                CharacterDatabaseIdentity.Require(context, Database);
                var owner = context.Character.AsNoTracking().Single(row => row.Id == Character);
                if (owner.AccountId != Account || owner.DeleteTime != null) throw new InvalidOperationException("Bank reconciliation changed its original SQL owner.");
                Graph.Guard(context, Committed);
            }
            internal bool OwnsMailItem(EntityEntry entry) => Graph.OwnsNeutralMailItem(entry, Committed, Character);
            internal void ValidateParent(CharacterContext context)
            {
                CharacterDatabaseIdentity.Require(context, Database);
                var owner = context.Character.AsNoTracking().Single(row => row.Id == Character);
                if (owner.AccountId != Account || owner.DeleteTime != null) throw new InvalidOperationException("Bank reconciliation changed its original SQL owner.");
                Graph.ValidateParent(context, Committed);
            }
        }
    }
}
