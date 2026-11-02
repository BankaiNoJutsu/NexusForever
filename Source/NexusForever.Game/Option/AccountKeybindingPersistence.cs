using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using NexusForever.Database.Auth;
using NexusForever.Database.Auth.Model;
using NexusForever.Game.Account.Currency;
using NexusForever.Game.Entity;

namespace NexusForever.Game.Option
{
    // Account input replaces one complete set. Auth owns the transaction and
    // its terminal callback; no binding is acknowledged while merely staged.
    internal sealed class AccountKeybindingPersistence
    {
        internal sealed record Binding(uint Owner, ushort Action, uint Device0, uint Device1, uint Device2,
            uint Code0, uint Code1, uint Code2, uint Meta0, uint Meta1, uint Meta2, uint Event0, uint Event1, uint Event2);
        internal sealed record Snapshot(Binding[] Bindings);
        private sealed class Unit(AuthContext context, string database, Snapshot target)
        {
            internal readonly AuthContext Context = context;
            internal readonly string Database = database;
            internal readonly Dictionary<object, CharacterColumnSaveIntent> Rows = new(ReferenceEqualityComparer.Instance);
            internal readonly List<Snapshot> Possible = [];
            internal Snapshot Sql, Target = target;
            internal bool Registered, Staged, Flushed;
            internal EventHandler<SavingChangesEventArgs> OnSaving;
            internal EventHandler<SavedChangesEventArgs> OnFlushed;
        }
        internal readonly object Gate = new();
        private static readonly object leaseGate = new();
        private static readonly Dictionary<(string, uint), Unit> leases = [];
        private readonly uint account;
        private readonly ulong epoch;
        private readonly Func<Snapshot> current;
        private readonly Action<Snapshot> accepted;
        private readonly Action validateOwner;
        private string database;
        private Snapshot baseline;
        private List<Snapshot> uncertain;
        private Unit active;
        private static readonly string[] keys = [nameof(AccountKeybindingModel.Id), nameof(AccountKeybindingModel.InputActionId)];
        private static readonly string[] fields = [nameof(AccountKeybindingModel.DeviceEnum00), nameof(AccountKeybindingModel.DeviceEnum01), nameof(AccountKeybindingModel.DeviceEnum02),
            nameof(AccountKeybindingModel.Code00), nameof(AccountKeybindingModel.Code01), nameof(AccountKeybindingModel.Code02),
            nameof(AccountKeybindingModel.MetaKeys00), nameof(AccountKeybindingModel.MetaKeys01), nameof(AccountKeybindingModel.MetaKeys02),
            nameof(AccountKeybindingModel.EventTypeEnum00), nameof(AccountKeybindingModel.EventTypeEnum01), nameof(AccountKeybindingModel.EventTypeEnum02)];

        internal AccountKeybindingPersistence(uint account, ulong epoch, Snapshot baseline, Func<Snapshot> current, Action<Snapshot> accepted, Action validateOwner)
        { this.account = account; this.epoch = epoch; this.baseline = baseline; this.current = current; this.accepted = accepted; this.validateOwner = validateOwner; }
        internal static Binding FromModel(AccountKeybindingModel row) => new(row.Id, row.InputActionId, row.DeviceEnum00, row.DeviceEnum01, row.DeviceEnum02,
            row.Code00, row.Code01, row.Code02, row.MetaKeys00, row.MetaKeys01, row.MetaKeys02, row.EventTypeEnum00, row.EventTypeEnum01, row.EventTypeEnum02);
        private static AccountKeybindingModel Model(Binding row) => new() { Id = row.Owner, InputActionId = row.Action, DeviceEnum00 = row.Device0, DeviceEnum01 = row.Device1, DeviceEnum02 = row.Device2,
            Code00 = row.Code0, Code01 = row.Code1, Code02 = row.Code2, MetaKeys00 = row.Meta0, MetaKeys01 = row.Meta1, MetaKeys02 = row.Meta2, EventTypeEnum00 = row.Event0, EventTypeEnum01 = row.Event1, EventTypeEnum02 = row.Event2 };
        private static bool Equal(Snapshot left, Snapshot right) => left != null && right != null && left.Bindings.SequenceEqual(right.Bindings);

        internal void Save(AuthContext context)
        {
            lock (Gate)
            {
                try
                {
                    ValidateOwner(context);
                    string route = AccountCurrencyOperationRepository.DatabaseKey(context);
                    if (database != null && database != route) throw new InvalidOperationException("Account keybindings changed their database route.");
                    database ??= route;
                    Snapshot target = current();
                    if (active != null && !ReferenceEquals(active.Context, context)) throw new InvalidOperationException("Account keybindings have an unfinished save.");
                    if (active == null && uncertain == null && Equal(target, baseline)) return;
                    if (active != null)
                    {
                        if (active.Staged) Restage(active, target);
                        else active.Target = target;
                        return;
                    }
                    var unit = new Unit(context, route, target);
                    lock (leaseGate)
                    {
                        if (leases.ContainsKey((route, account))) throw new InvalidOperationException("Account keybindings belong to another loaded owner.");
                        leases.Add((route, account), unit);
                    }
                    active = unit;
                    unit.OnSaving = (_, _) => BeforeFlush(unit);
                    unit.OnFlushed = (_, _) => Flushed(unit);
                    context.EnlistSaveWork(_ => StageAttempt(unit), committed => Complete(unit, committed), () => CanCommit(unit), account);
                    unit.Registered = true;
                    context.SavingChanges += unit.OnSaving;
                    context.SavedChanges += unit.OnFlushed;
                }
                catch
                {
                    context.RejectSaveUnit();
                    // A registration failure has no terminal callback to release it.
                    if (active != null && ReferenceEquals(context, active.Context) && !active.Registered) Complete(active, false);
                    throw;
                }
            }
        }
        private void ValidateOwner(AuthContext context)
        {
            validateOwner?.Invoke();
            if (context.IsSaveUnitRejected || context.HasSavepointRollback) throw new InvalidOperationException("Account keybindings require a fresh Auth context.");
        }
        private void ValidateEpoch(AuthContext context)
        {
            if (epoch != 0 && context.AccountWorldSession.AsNoTracking().Where(row => row.AccountId == account).Select(row => row.Epoch).SingleOrDefault() != epoch)
                throw new InvalidOperationException("Account keybindings lost their admitted world session.");
        }
        private Snapshot Read(AuthContext context)
        {
            var query = context.Database.ProviderName.Contains("MySql", StringComparison.OrdinalIgnoreCase)
                ? context.AccountKeybinding.FromSqlInterpolated($"SELECT * FROM account_keybinding WHERE id={account} FOR UPDATE")
                : context.AccountKeybinding.Where(row => row.Id == account);
            return new(query.AsNoTracking().AsEnumerable().OrderBy(row => row.InputActionId).Select(FromModel).ToArray());
        }
        private void StageAttempt(Unit unit)
        {
            lock (Gate)
            {
                try
                {
                    ValidateOwner(unit.Context);
                    if (!ReferenceEquals(active, unit) || database != AccountCurrencyOperationRepository.DatabaseKey(unit.Context)
                        || unit.Context.Database.CurrentTransaction == null) throw new InvalidOperationException("Account keybindings lost their Auth save unit.");
                    // The same root lock is used by account admission and other Auth
                    // producers. It also serializes empty input sets on SQLite.
                    unit.Context.Database.ExecuteSqlInterpolated($"UPDATE account SET id=id WHERE id={account}");
                    if (!unit.Context.Account.AsNoTracking().Any(row => row.Id == account)) throw new InvalidOperationException("Account keybinding owner no longer exists.");
                    ValidateEpoch(unit.Context);
                    unit.Context.ChangeTracker.DetectChanges(); foreach (var guard in unit.Rows.Values) guard.Validate();
                    Snapshot actual = Read(unit.Context);
                    var possible = (uncertain ?? []).Concat(unit.Possible).Append(baseline);
                    if (!possible.Any(candidate => Equal(candidate, actual))) throw new InvalidOperationException("Account keybindings differ from their loaded or uncertain set.");
                    foreach (var guard in unit.Rows.Values) guard.Entry.State = EntityState.Detached;
                    unit.Rows.Clear(); unit.Sql = actual; unit.Possible.Add(actual); unit.Staged = true; unit.Flushed = false;
                    Stage(unit, actual, unit.Target, actual); unit.Possible.Add(unit.Target);
                }
                catch { unit.Context.RejectSaveUnit(); throw; }
            }
        }
        private void Restage(Unit unit, Snapshot target)
        {
            ValidateBeforeFlush(unit);
            Stage(unit, unit.Sql, target, unit.Target); unit.Target = target; unit.Possible.Add(target);
            // Keep the verified flush for an unchanged resave. A changed target
            // still cannot commit until the SQL set matches it in CanCommit.
        }
        private static void NormalizeFlushed(Unit unit)
        {
            var stored = unit.Sql.Bindings.ToDictionary(row => row.Action);
            foreach (var guard in unit.Rows.Values.Where(row => row.Flushed && row.Entry.State is EntityState.Added or EntityState.Modified or EntityState.Deleted))
            {
                Binding row = FromModel((AccountKeybindingModel)guard.Entry.Entity);
                if (stored.TryGetValue(row.Action, out Binding sql))
                {
                    if (row != sql) throw new InvalidOperationException("Account keybindings lost their flushed tracked values.");
                    guard.Entry.State = EntityState.Unchanged;
                }
                else
                {
                    if (guard.Entry.State != EntityState.Deleted) throw new InvalidOperationException("Flushed account keybinding disappeared.");
                    guard.Entry.State = EntityState.Detached;
                }
                guard.Capture();
            }
        }
        private void Stage(Unit unit, Snapshot before, Snapshot desired, Snapshot previous)
        {
            var stored = before.Bindings.ToDictionary(row => row.Action); var prior = previous.Bindings.ToDictionary(row => row.Action); var target = desired.Bindings.ToDictionary(row => row.Action);
            foreach (Binding row in desired.Bindings)
            {
                Binding old = stored.GetValueOrDefault(row.Action);
                if (row == old && row == prior.GetValueOrDefault(row.Action)) continue;
                Write(unit, old, row);
            }
            foreach (Binding row in before.Bindings.Where(row => !target.ContainsKey(row.Action)))
            {
                var entry = Write(unit, row, row); entry.State = EntityState.Deleted; unit.Rows[entry.Entity].Capture();
            }
            foreach (Binding row in previous.Bindings.Where(row => !stored.ContainsKey(row.Action) && !target.ContainsKey(row.Action)))
            {
                var entry = unit.Context.ChangeTracker.Entries<AccountKeybindingModel>().Single(value => value.Entity.Id == row.Owner && value.Entity.InputActionId == row.Action);
                if (!unit.Rows.TryGetValue(entry.Entity, out var guard) || entry.State != EntityState.Added) throw new InvalidOperationException("Keybindings lost their pending create.");
                guard.Validate(); entry.State = EntityState.Detached; guard.Capture();
            }
        }
        private static EntityEntry<AccountKeybindingModel> Write(Unit unit, Binding before, Binding desired)
        {
            var context = unit.Context; var model = Model(desired); var identity = context.Entry(model);
            var entry = context.ChangeTracker.Entries<AccountKeybindingModel>().SingleOrDefault(row => row.Entity.Id == desired.Owner && row.Entity.InputActionId == desired.Action);
            var old = before == null ? null : context.Entry(Model(before));
            if (entry == null) { entry = old ?? identity; entry.State = before == null ? EntityState.Added : EntityState.Unchanged; }
            else if (!unit.Rows.ContainsKey(entry.Entity) && (before == null || entry.State is EntityState.Added or EntityState.Deleted or EntityState.Detached
                || fields.Any(name => entry.Property(name).IsModified || !Equals(entry.Property(name).CurrentValue, old.Property(name).CurrentValue))))
                throw new InvalidOperationException("Keybinding row has foreign tracked intent.");
            if (before != null && entry.State == EntityState.Deleted && unit.Rows.ContainsKey(entry.Entity)) entry.State = EntityState.Unchanged;
            foreach (string name in fields)
            {
                if (old != null) entry.Property(name).OriginalValue = old.Property(name).CurrentValue;
                entry.Property(name).CurrentValue = identity.Property(name).CurrentValue;
                if (old != null) entry.Property(name).IsModified = !Equals(entry.Property(name).CurrentValue, entry.Property(name).OriginalValue);
            }
            context.ChangeTracker.DetectChanges(); var guard = new CharacterColumnSaveIntent(entry, keys.Concat(fields).ToArray(), false, "Keybindings"); guard.Capture(); unit.Rows[entry.Entity] = guard;
            return entry;
        }
        private void ValidateBeforeFlush(Unit unit)
        {
            ValidateOwner(unit.Context); ValidateEpoch(unit.Context);
            if (!ReferenceEquals(active, unit)) throw new InvalidOperationException("Account keybindings lost their save owner.");
            unit.Context.ChangeTracker.DetectChanges(); foreach (var guard in unit.Rows.Values) guard.Validate();
            if (!Equal(Read(unit.Context), unit.Sql)) throw new InvalidOperationException("Account keybinding SQL changed before flush.");
            NormalizeFlushed(unit);
        }
        private void BeforeFlush(Unit unit)
        {
            lock (Gate)
            {
                try { ValidateBeforeFlush(unit); }
                catch { unit.Context.RejectSaveUnit(); throw; }
            }
        }
        private void Flushed(Unit unit)
        {
            lock (Gate)
            {
                if (!ReferenceEquals(active, unit) || unit.Context.HasSavepointRollback) return;
                Snapshot actual = Read(unit.Context);
                if (!Equal(actual, unit.Target)) return;
                unit.Sql = actual; unit.Possible.Add(actual); unit.Flushed = true;
                foreach (var row in unit.Rows.Values) { row.Capture(); row.Flushed = true; }
            }
        }
        private bool CanCommit(Unit unit)
        {
            lock (Gate)
            {
                try
                {
                    ValidateOwner(unit.Context); ValidateEpoch(unit.Context);
                    if (!ReferenceEquals(active, unit) || !unit.Flushed) throw new InvalidOperationException("Account input set has no verified flush.");
                    unit.Context.ChangeTracker.DetectChanges(); foreach (var row in unit.Rows.Values) row.Validate();
                    if (!Equal(Read(unit.Context), unit.Target)) throw new InvalidOperationException("Account keybinding SQL changed before commit.");
                    return true;
                }
                catch { unit.Context.RejectSaveUnit(); throw; }
            }
        }
        private void Complete(Unit unit, bool committed)
        {
            lock (Gate)
            {
                if (!ReferenceEquals(active, unit)) return;
                try
                {
                    if (committed && unit.Flushed) { baseline = unit.Sql; uncertain = null; accepted(baseline); }
                    else uncertain = (uncertain ?? []).Concat(unit.Possible).Append(baseline).ToList();
                }
                finally
                {
                    unit.Context.SavingChanges -= unit.OnSaving; unit.Context.SavedChanges -= unit.OnFlushed; active = null;
                    lock (leaseGate) { if (leases.TryGetValue((unit.Database, account), out var held) && ReferenceEquals(held, unit)) leases.Remove((unit.Database, account)); }
                }
            }
        }
    }
}
