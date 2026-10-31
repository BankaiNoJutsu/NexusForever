using NexusForever.Database.Auth;
using NexusForever.Game.Static.Account;
using NexusForever.Network.Session;

namespace NexusForever.Game.Account.Currency
{
    public partial class AccountCurrency
    {
        private readonly object saveGate = new();
        private readonly List<AccountWalletOperation> operations = [];
        private SaveRegistration pendingSave;
        private string database;
        private bool requiresReload;
        private AccountWalletOperation NewOperation(ulong value, bool subtract)
            => new($"wallet:{Guid.NewGuid():N}", account.Id, CurrencyId, value, subtract);
        private void EnsureCurrent()
        {
            if (requiresReload) throw new InvalidOperationException("Wallet owner requires authoritative reload.");
        }

        public void Save(AuthContext context)
        {
            ArgumentNullException.ThrowIfNull(context);
            if (CurrencyId == AccountCurrencyType.CosmicReward && account.LoyaltyManager != null)
            {
                // Keep the established Loyalty source/earning journal as owner.
                account.LoyaltyManager.Save(context); return;
            }
            lock (saveGate)
            {
                try
                {
                    EnsureCurrent();
                    if (pendingRefresh != null)
                        throw new InvalidOperationException("An authoritative wallet observation still owns this currency.");
                    if (pendingSave != null)
                    {
                        if (!ReferenceEquals(pendingSave.Context, context))
                            throw new InvalidOperationException("This wallet currency belongs to another pending save unit.");
                        if (pendingSave.Operations.SequenceEqual(operations)) return;
                        if (pendingSave.Staged) throw new InvalidOperationException("Wallet changed after staging; retry in a fresh unit.");
                        pendingSave.Operations = operations.ToArray(); return;
                    }
                    if (operations.Count == 0) return;
                    string actual = AccountCurrencyOperationRepository.DatabaseKey(context);
                    if (database != null && database != actual) throw new InvalidOperationException("Wallet changed its retained Auth database.");
                    database ??= actual;
                    var registration = new SaveRegistration(context, operations.ToArray(), account.Session);
                    pendingSave = registration;
                    try
                    {
                        context.EnlistSaveWork(unit =>
                        {
                            lock (saveGate)
                            {
                                try
                                {
                                    EnsureCurrent();
                                    if (!ReferenceEquals(pendingSave, registration) || AccountCurrencyOperationRepository.DatabaseKey(unit) != database)
                                        throw new InvalidOperationException("Wallet save changed owner or database.");
                                    registration.Repository?.Detach();
                                    registration.Repository = new(unit, account.Id, CurrencyId, registration.Operations);
                                    registration.Repository.Stage();
                                    registration.Staged = true;
                                }
                                catch (Exception error) when (error is InvalidOperationException or OverflowException or ArgumentException)
                                { unit.RejectSaveUnit(); throw; }
                            }
                        }, committed => Complete(registration, committed), () =>
                        {
                            lock (saveGate)
                            {
                                bool allowed = !requiresReload && ReferenceEquals(pendingSave, registration)
                                    && registration.Repository?.CanCommit() == true;
                                if (allowed)
                                {
                                    try { PreviewRemaining(registration); }
                                    catch (OverflowException) { allowed = false; }
                                }
                                if (!allowed) context.RejectSaveUnit();
                                return allowed;
                            }
                        });
                    }
                    catch { pendingSave = null; throw; }
                }
                catch { context.RejectSaveUnit(); throw; }
            }
        }

        private ulong PreviewRemaining(SaveRegistration registration)
        {
            var captured = registration.Operations.Select(operation => operation.Key).ToHashSet(StringComparer.Ordinal);
            ulong visible = registration.Repository.Amount;
            foreach (var operation in operations.Where(operation => !captured.Contains(operation.Key)))
                visible = AccountCurrencyOperationRepository.Apply(visible, operation.Amount, operation.Subtract);
            return visible;
        }

        private void Complete(SaveRegistration registration, bool committed)
        {
            bool disconnect = false;
            lock (saveGate)
            {
                if (!ReferenceEquals(pendingSave, registration)) return;
                pendingSave = null;
                if (committed && registration.Repository != null)
                {
                    try { amount = PreviewRemaining(registration); }
                    catch (OverflowException)
                    {
                        // A mutation after the final commit guard may have used
                        // a stale visible balance. Never acknowledge that spend
                        // or let it overwrite the committed wallet.
                        amount = registration.Repository.Amount;
                        requiresReload = disconnect = true;
                    }
                    var captured = registration.Operations.Select(operation => operation.Key).ToHashSet(StringComparer.Ordinal);
                    operations.RemoveAll(operation => captured.Contains(operation.Key));
                    saveMask = operations.Count == 0 ? AccountCurrencySaveMask.None : AccountCurrencySaveMask.Amount;
                }
                try { registration.Repository?.Detach(); } catch (ObjectDisposedException) { }
            }
            // Terminal callbacks must not prevent other Auth owners settling.
            if (disconnect) try { registration.Session?.ForceDisconnect(); } catch { }
        }

        private sealed class SaveRegistration(AuthContext context, AccountWalletOperation[] operations, IGameSession session)
        {
            internal AuthContext Context { get; } = context;
            internal IGameSession Session { get; } = session;
            internal AccountWalletOperation[] Operations { get; set; } = operations;
            internal AccountCurrencyOperationRepository Repository { get; set; }
            internal bool Staged { get; set; }
        }
    }
}
