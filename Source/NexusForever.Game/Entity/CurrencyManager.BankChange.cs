using NexusForever.Database.Character;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Quest;
using NexusForever.Game.RealmBank;
using NexusForever.Game.Static.Achievement;
using NexusForever.Game.Static.Entity;
using NexusForever.Network.World.Message.Model;

namespace NexusForever.Game.Entity
{
    public partial class CurrencyManager
    {
        internal readonly record struct BankCurrencyChange(CurrencyType Type, ulong Amount, bool Subtract = false);
        private BankCurrencyReservation bankCurrency;

        internal BankCurrencyReservation CaptureBankCurrency(IPlayer owner, IEnumerable<BankCurrencyChange> changes, Func<bool> currentActor, Action<Action> after)
            => CaptureCurrency(owner, (owner as Player)?.BankCurrencyToken, changes, currentActor, after);

        // The coordinator owns the matching Character-wide frozen lease until
        // settlement/publication. Stage verifies its token in QuestSaveScope.
        internal BankCurrencyReservation CaptureFrozenCurrency(IPlayer owner, object token, IEnumerable<BankCurrencyChange> changes,
            Func<bool> currentActor, Action<Action> after)
        {
            ArgumentNullException.ThrowIfNull(token);
            return CaptureCurrency(owner, token, changes, currentActor, after);
        }

        private BankCurrencyReservation CaptureCurrency(IPlayer owner, object token, IEnumerable<BankCurrencyChange> changes,
            Func<bool> currentActor, Action<Action> after)
        {
            if (!ReferenceEquals(player, owner) || HasPendingContractCurrency || contractCurrencyReload || !currentActor())
                throw new InvalidOperationException("Bank currency requires its current unreserved actor.");
            AssertLoadedCurrencyOwner();
            bankCurrency = new(this, changes.ToArray(), currentActor, after, token);
            return bankCurrency;
        }

        private bool DeferBankCurrency(CurrencyType type, ulong amount, bool subtract, bool loot)
        {
            if (bankCurrency == null || !bankCurrency.Has(type)) return false;
            bankCurrency.Defer(type, amount, subtract, loot); return true;
        }

        internal sealed class BankCurrencyReservation : IBankSaveCompanion
        {
            private sealed class Wallet(Currency currency, bool registered)
            {
                internal readonly Currency Currency = currency;
                internal bool Registered = registered;
                internal Currency.BankChangeOwner Owner;
                internal ulong Planned = currency.Amount, Rollback = currency.Amount, Commit = currency.Amount;
            }
            private sealed record Line(CurrencyType Type, ulong Before, ulong After, bool Loot, bool Credit);
            private sealed record Deferred(CurrencyType Type, ulong Amount, bool Subtract, bool Loot);
            private readonly CurrencyManager manager;
            private readonly IPlayer player;
            private readonly Func<bool> currentActor;
            private readonly Action<Action> after;
            private readonly Dictionary<CurrencyType, Wallet> wallets = [];
            private readonly List<Line> initial = [];
            private readonly List<Deferred> deferred = [];
            private volatile int state; //0 captured,1 adopted,2 published,3 ended,4 quarantined
            private bool publishing;
            private bool reconciling;
            private int applied;
            private readonly List<Line> notifications = [];
            internal bool CanSave => reconciling && state == 1;
            internal Action<CharacterContext> CaptureHousingPrimarySave(object token)
            {
                Validate();
                if (state != 0 || reconciling) throw new InvalidOperationException("Housing fee already entered reconciliation.");
                var other = manager.currencies.Where(pair => !wallets.ContainsKey(pair.Key)).Select(pair => pair.Value is Currency currency
                    ? currency.CaptureNormalSave(token) : throw new InvalidOperationException("Housing fee requires concrete currency owners.")).ToArray();
                return context =>
                {
                    Validate(); manager.AssertLoadedCurrencyContext(context);
                    foreach (var row in other) row(context);
                    Stage(context);
                };
            }
            internal void PrepareHousingOrdinary()
            {
                Validate();
                if (state != 0 || reconciling) throw new InvalidOperationException("Housing fee already reconciles.");
                foreach (var pair in wallets)
                {
                    pair.Value.Owner.AdoptForHousingReconciliation();
                    if (!pair.Value.Registered) { manager.currencies.Add(pair.Key, pair.Value.Currency); pair.Value.Registered = true; }
                }
                reconciling = true; state = 1;
                foreach (var line in initial) ApplyEffects(line);
                ApplyLater();
            }
            internal IReadOnlyList<Func<NexusForever.Network.Message.IWritable>> CaptureHousingPublication()
            {
                Validate();
                if (!CanSave || applied != deferred.Count) throw new InvalidOperationException("Housing fee has unfinished ordinary currency intent.");
                publishing = true;
                var packets = new List<Func<NexusForever.Network.Message.IWritable>>();
                foreach (var line in notifications)
                {
                    if (line.Loot) packets.Add(() => new ServerChannelUpdateLoot { CurrencyId = line.Type, Amount = unchecked(line.After - line.Before) });
                    packets.Add(() => new ServerCombatReward { Stat = (byte)(line.Type - 1), NewValue = line.After });
                }
                return packets;
            }
            internal void FinishHousingPublication()
            {
                Validate();
                if (!CanSave || !publishing || applied != deferred.Count) throw new InvalidOperationException("Housing currency publication lost its retained state.");
                state = 2; Complete();
            }
            internal void SuppressPublication()
            {
                Validate(); if (!CanSave || applied != deferred.Count) throw new InvalidOperationException("Bank currency has unfinished ordinary intent.");
                state = 2;
            }
            internal Action<CharacterContext> CaptureNormalSave(object token)
            {
                Validate(); if (!CanSave) throw new InvalidOperationException("Bank currency has no reconciled normal save.");
                var rows = manager.currencies.Select(pair => pair.Value is Currency currency
                    ? currency.CaptureNormalSave(token, null, wallets.GetValueOrDefault(pair.Key)?.Owner)
                    : throw new InvalidOperationException("Bank reconciliation requires concrete currency owners.")).ToArray();
                return context => { Validate(); manager.AssertLoadedCurrencyContext(context); foreach (var row in rows) row(context); };
            }
            internal void PrepareOrdinary(RealmBankManager.BankOutcome outcome)
            {
                Validate(); if (state != 0 || reconciling) throw new InvalidOperationException("Bank currency already reconciles.");
                foreach (var pair in wallets)
                {
                    var stored = outcome.Currency((byte)pair.Key);
                    pair.Value.Owner.Reconcile(outcome.Committed, stored);
                    if (stored != null || outcome.Committed)
                    { manager.currencies[pair.Key] = pair.Value.Currency; pair.Value.Registered = true; }
                }
                reconciling = true; state = 1;
                if (outcome.Committed) foreach (var line in initial) ApplyEffects(line);
                ApplyLater();
            }
            internal bool PrepareNextOrdinary()
            {
                Validate(); if (!CanSave) throw new InvalidOperationException("Bank currency lost its ordinary-save phase.");
                if (applied == deferred.Count) return false;
                ApplyLater(); return true;
            }
            private void ApplyLater()
            {
                while (applied < deferred.Count)
                {
                    var mutation = deferred[applied++]; var wallet = wallets[mutation.Type];
                    ulong before = wallet.Currency.Amount, next = Next(wallet, before, mutation.Amount, mutation.Subtract);
                    if (!wallet.Registered) { manager.currencies.Add(mutation.Type, wallet.Currency); wallet.Registered = true; }
                    wallet.Owner.ApplyDeferred(next); ApplyEffects(new(mutation.Type, before, next, mutation.Loot, !mutation.Subtract));
                }
            }
            private void ApplyEffects(Line line)
            {
                Validate(); notifications.Add(line);
                ulong credit = line.Credit ? line.After - line.Before : 0;
                if (credit == 0) return;
                player.AchievementManager.CheckAchievements(player, AchievementType.CurrencyEarned, (uint)line.Type, count: ToAchievementCount(credit));
                Validate(); CurrencyQuestObjectiveUpdater.OnCurrencyAdded(player, line.Type, credit);
            }

            internal BankCurrencyReservation(CurrencyManager manager, BankCurrencyChange[] changes, Func<bool> currentActor, Action<Action> after, object token)
            {
                this.manager = manager; player = manager.player; this.currentActor = currentActor; this.after = after;
                try
                {
                    foreach (var change in changes)
                    {
                        if (change.Type == CurrencyType.None) throw new InvalidOperationException("A bank currency line requires a real currency.");
                        if (!wallets.TryGetValue(change.Type, out var wallet))
                        {
                            var entry = manager.gameTableManager.CurrencyType?.GetEntry((uint)change.Type)
                                ?? throw new InvalidOperationException("Bank currency has no canonical entry.");
                            bool existed = manager.currencies.TryGetValue(change.Type, out var loaded);
                            if (existed && loaded is not Currency) throw new InvalidOperationException("Bank currency requires its concrete persistence owner.");
                            wallet = new(existed ? (Currency)loaded : manager.CreateCurrencyOwner(entry), existed);
                            if (wallet.Currency.CharacterId != manager.player.CharacterId || wallet.Currency.Entry.CapAmount != entry.CapAmount)
                                throw new InvalidOperationException("Bank currency changed its Character or cap.");
                            wallets.Add(change.Type, wallet);
                        }
                        ulong before = wallet.Planned;
                        wallet.Planned = Next(wallet, before, change.Amount, change.Subtract);
                        initial.Add(new(change.Type, before, wallet.Planned, false, !change.Subtract));
                    }
                    foreach (var wallet in wallets.Values)
                    {
                        wallet.Owner = token == null ? wallet.Currency.CaptureBankChange(wallet.Planned)
                            : wallet.Currency.CaptureFrozenBankChange(wallet.Planned, token);
                        wallet.Commit = wallet.Planned;
                    }
                }
                catch
                {
                    foreach (var wallet in wallets.Values.Where(value => value.Owner != null))
                        try { wallet.Owner.End(); } catch { manager.contractCurrencyReload = true; }
                    throw;
                }
            }
            internal bool Has(CurrencyType type) => wallets.ContainsKey(type);
            private static ulong Next(Wallet wallet, ulong before, ulong amount, bool subtract)
                => subtract ? checked(before - amount) : AddCurrencyAmount(before, amount, wallet.Currency.Entry.CapAmount);
            private void Validate()
            {
                if (state == 4 || manager.contractCurrencyReload || !ReferenceEquals(manager.bankCurrency, this)
                    || !ReferenceEquals(manager.player, player) || !ReferenceEquals(player.CurrencyManager, manager) || !currentActor())
                    throw new InvalidOperationException("Bank currency lost its original live owner.");
                foreach (var pair in wallets)
                    if (pair.Value.Registered != manager.currencies.TryGetValue(pair.Key, out var current)
                        || pair.Value.Registered && !ReferenceEquals(current, pair.Value.Currency))
                        throw new InvalidOperationException("Bank currency lost its wallet identity.");
            }
            internal void Defer(CurrencyType type, ulong amount, bool subtract, bool loot)
            {
                Validate();
                if (state != 0 && !CanSave || publishing) throw new InvalidOperationException("Bank publication cannot reenter currency mutation.");
                var wallet = wallets[type];
                if (reconciling)
                {
                    ulong shadow = wallet.Currency.Amount;
                    foreach (var later in deferred.Skip(applied).Where(value => value.Type == type)) shadow = Next(wallet, shadow, later.Amount, later.Subtract);
                    _ = Next(wallet, shadow, amount, subtract); deferred.Add(new(type, amount, subtract, loot)); return;
                }
                ulong rollback = Next(wallet, wallet.Rollback, amount, subtract), commit = Next(wallet, wallet.Commit, amount, subtract);
                deferred.Add(new(type, amount, subtract, loot)); wallet.Rollback = rollback; wallet.Commit = commit;
            }
            public void Stage(CharacterContext context)
            { manager.AssertLoadedCurrencyContext(context); foreach (var wallet in wallets.Values) wallet.Owner.Stage(context); }
            public void Adopt()
            {
                Validate(); if (state != 0) throw new InvalidOperationException("Bank currency can adopt only once.");
                foreach (var pair in wallets)
                {
                    pair.Value.Owner.Adopt();
                    if (!pair.Value.Registered) { manager.currencies.Add(pair.Key, pair.Value.Currency); pair.Value.Registered = true; }
                }
                state = 1;
            }
            internal void Publish()
            {
                Validate(); if (state != 1) throw new InvalidOperationException("Bank currency has no confirmed settlement.");
                publishing = true;
                if (reconciling)
                {
                    if (applied != deferred.Count) throw new InvalidOperationException("Bank currency has unsaved later intent.");
                    foreach (var line in notifications)
                    {
                        ValidatePublication();
                        if (line.Loot) player.Session.EnqueueMessageEncrypted(new ServerChannelUpdateLoot { CurrencyId = line.Type, Amount = unchecked(line.After - line.Before) });
                        ValidatePublication(); player.Session.EnqueueMessageEncrypted(new ServerCombatReward { Stat = (byte)(line.Type - 1), NewValue = line.After });
                    }
                    state = 2; return;
                }
                foreach (Line line in initial) Emit(line);
                foreach (var mutation in deferred)
                {
                    Validate(); var wallet = wallets[mutation.Type]; ulong before = wallet.Currency.Amount;
                    ulong next = Next(wallet, before, mutation.Amount, mutation.Subtract);
                    wallet.Owner.ApplyDeferred(next); Emit(new(mutation.Type, before, next, mutation.Loot, !mutation.Subtract));
                }
                foreach (var wallet in wallets.Values)
                    if (wallet.Currency.Amount != wallet.Commit) throw new InvalidOperationException("Bank currency replay changed its retained intent.");
                Validate(); state = 2;
            }
            private void ValidatePublication()
            {
                Validate();
                if (player is Player original && !original.BankPublicationAllowed)
                    throw new InvalidOperationException("Bank currency publication lost its original session Player.");
            }
            private void Emit(Line line)
            {
                Validate();
                if (line.Loot) manager.player.Session.EnqueueMessageEncrypted(new ServerChannelUpdateLoot { CurrencyId = line.Type, Amount = unchecked(line.After - line.Before) });
                Validate(); manager.player.Session.EnqueueMessageEncrypted(new ServerCombatReward { Stat = (byte)(line.Type - 1), NewValue = line.After });
                ulong granted = line.Credit ? line.After - line.Before : 0;
                if (granted == 0) return;
                after(() => manager.player.AchievementManager.CheckAchievements(manager.player, AchievementType.CurrencyEarned, (uint)line.Type, count: ToAchievementCount(granted)));
                after(() => CurrencyQuestObjectiveUpdater.OnCurrencyAdded(manager.player, line.Type, granted));
            }
            public void Complete()
            {
                Validate(); if (state != 2) throw new InvalidOperationException("Bank currency publication is incomplete.");
                foreach (var wallet in wallets.Values) wallet.Owner.End();
                state = 3; manager.bankCurrency = null;
            }
            public void Abort()
            {
                if (state != 0) { Quarantine(); return; }
                if (deferred.Count != 0)
                {
                    // Later mutations are independent of the failed operation.
                    // Keep them owned until a coordinator reconciles their SQL
                    // and gameplay effects; ending here would silently lose them.
                    Quarantine();
                    throw new InvalidOperationException("Currency abort requires reconciliation of deferred mutations.");
                }
                foreach (var wallet in wallets.Values) wallet.Owner.End();
                state = 3; manager.bankCurrency = null;
            }
            public void Quarantine()
            {
                state = 4; manager.contractCurrencyReload = true;
                foreach (var wallet in wallets.Values) wallet.Owner.Quarantine();
            }
        }
    }
}
