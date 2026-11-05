using NexusForever.Database.Auth;
using NexusForever.Game.Static.Account;
using NexusForever.Network.Message;
using NexusForever.Network.Session;
using NexusForever.Network.World.Message.Model;

namespace NexusForever.Game.Account.Currency
{
    public partial class AccountCurrencyManager
    {
        private WalletRefresh walletRefresh;
        internal bool IsWalletRefreshBusy(AccountCurrencyType type)
        {
            lock (currencyGate)
                return walletRefresh != null || currencies.TryGetValue(type, out var value) && value is AccountCurrency wallet && wallet.IsPersistenceBusy;
        }

        // A caller must return to the session actor to publish.
        internal WalletRefresh BeginWalletRefresh(AccountCurrencyType type, Func<AuthContext> contexts, AccountWalletOperation required = null, Task beforeRead = null)
        {
            lock (currencyGate)
            {
                if (walletRefresh != null || account.Id == 0 || account.Session == null
                    || !ReferenceEquals(account.CurrencyManager, this) || account.Session is NetworkSession { IsDisconnecting: true }
                    || type == AccountCurrencyType.CosmicReward && account.LoyaltyManager != null)
                    throw new InvalidOperationException("Wallet refresh lost its idle original account/session owner.");
                if (!currencies.TryGetValue(type, out var value)) value = CreateAccountCurrency(type);
                var wallet = value as AccountCurrency ?? throw new InvalidOperationException("Wallet refresh requires the durable currency owner.");
                var reservation = wallet.CaptureRefresh(required);
                var refresh = new WalletRefresh(this, wallet, reservation, account.Id, account.WorldSessionEpoch, account.Session);
                walletRefresh = refresh;
                try { refresh.Completion = reservation.ReadAsync(contexts, beforeRead); return refresh; }
                catch { walletRefresh = null; reservation.Dispose(); throw; }
            }
        }

        internal sealed class WalletRefresh(AccountCurrencyManager manager, AccountCurrency wallet, AccountCurrency.RefreshReservation reservation,
            uint accountId, ulong epoch, IGameSession session) : IDisposable
        {
            internal enum PublicationOutcome { Published, Retry, Failed }

            internal Task Completion { get; set; }
            private bool publishing;
            private bool adopted;
            private int nextPacket;
            private bool Live() => ReferenceEquals(manager.walletRefresh, this) && manager.account.Id == accountId
                && manager.account.WorldSessionEpoch == epoch && ReferenceEquals(manager.account.CurrencyManager, manager)
                && ReferenceEquals(manager.account.Session, session) && session is not NetworkSession { IsDisconnecting: true }
                && manager.currencies.TryGetValue(wallet.CurrencyId, out var current) && ReferenceEquals(current, wallet);

            internal bool TryPublish() => Publish(null, allowRetry: false) == PublicationOutcome.Published;

            internal PublicationOutcome Publish(Func<bool> ownsPublication, bool allowRetry)
            {
                lock (manager.currencyGate)
                {
                    // A premature poll is harmless; a completed failed read or
                    // changed owner quarantines the cache instead of guessing.
                    if (!ReferenceEquals(manager.walletRefresh, this) || publishing || Completion?.IsCompleted != true)
                        return PublicationOutcome.Failed;

                    publishing = true;
                    bool retryPending = false;
                    bool CanPublish() => Live() && (ownsPublication?.Invoke() ?? true);
                    try
                    {
                        if (!CanPublish() || (!adopted && !reservation.TryAdopt()))
                            throw new InvalidOperationException("Wallet refresh cannot adopt this completion.");
                        adopted = true;

                        while (nextPacket < 2)
                        {
                            IWritable packet = nextPacket == 0
                                ? new ServerAccountCurrencyGrant { AccountCurrency = wallet.Build() }
                                : new ServerWalletUpdate((uint)wallet.CurrencyId);
                            try { session.EnqueueMessageEncrypted(packet); }
                            catch when (allowRetry && CanPublish())
                            {
                                retryPending = true;
                                return PublicationOutcome.Retry;
                            }

                            nextPacket++;
                            if (!CanPublish())
                                throw new InvalidOperationException("Wallet publication lost its session.");
                        }
                        return PublicationOutcome.Published;
                    }
                    catch
                    {
                        reservation.Quarantine();
                        return PublicationOutcome.Failed;
                    }
                    finally
                    {
                        publishing = false;
                        if (!retryPending)
                            Release();
                    }
                }
            }

            private void Release()
            {
                reservation.Dispose();
                if (ReferenceEquals(manager.walletRefresh, this))
                    manager.walletRefresh = null;
            }

            public void Dispose()
            {
                lock (manager.currencyGate)
                {
                    if (publishing) return;
                    if (adopted && ReferenceEquals(manager.walletRefresh, this))
                        reservation.Quarantine();
                    Release();
                }
            }
        }
    }
}
