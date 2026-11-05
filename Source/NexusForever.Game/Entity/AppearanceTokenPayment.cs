using NexusForever.Database.Auth;
using NexusForever.Game.Account.Currency;
using NexusForever.Game.Static.Account;
using NexusForever.Network.Message;
using NLog;

namespace NexusForever.Game.Entity
{
    internal sealed class AppearanceTokenPayment
    {
        private static readonly Logger log = LogManager.GetCurrentClassLogger();
        private readonly Player player;
        private readonly AccountCurrencyManager manager;
        private readonly AuthDatabase auth;
        private readonly VendorAccountPurchase protocol;
        private readonly Func<bool> owns;
        private readonly AppearanceChangeState target;
        private readonly ulong epoch;
        private AccountCurrencyManager.VendorWallets wallets;
        private VendorAccountPurchase.Intent intent;
        private Task observation;
        private bool adopted;
        internal ulong Fee { get; }
        internal string AuthRoute { get; }
        internal AppearanceTokenPayment(Player player, AppearanceChangeState target, ulong fee, Func<bool> owns)
        {
            this.player = player; this.target = target; Fee = fee; this.owns = owns; epoch = player.Account.WorldSessionEpoch;
            manager = player.Account.CurrencyManager as AccountCurrencyManager
                ?? throw new InvalidOperationException("Appearance tokens require their original admitted wallet.");
            auth = player.GetAuthDatabase();
            using (var context = auth.CreateContext()) AuthRoute = AccountCurrencyOperationRepository.DatabaseKey(context);
            protocol = new(player.GetCharacterDatabase().CreateContext, auth.CreateContext, player.Identity.RealmId);
        }
        internal void Prepare()
        {
            if (!owns() || !ReferenceEquals(player.Account.CurrencyManager, manager)) throw new InvalidOperationException("Appearance payment lost its original wallet.");
            wallets ??= manager.CaptureVendorWallets([new(AccountCurrencyType.ServiceToken, Fee)]);
        }
        internal Task AuthorizeAsync(string receipt, string payload)
        {
            if (!owns() || wallets == null || adopted) throw new InvalidOperationException("Appearance payment lost its retained reservation.");
            intent = protocol.CreateAppearance(receipt, player.Account.Id, player.CharacterId, target.Race, target.Sex, target.Hash, Fee, epoch, payload);
            if (intent.AuthRoute != AuthRoute) throw new InvalidOperationException("Appearance payment changed its Auth route.");
            return protocol.AuthorizeAppearanceAsync(intent);
        }
        internal async Task RollbackAsync(string receipt)
        {
            var retained = intent; if (retained == null || retained.Id != receipt) return;
            bool reported = false;
            while (true)
            {
                try { await protocol.ResolveWalletAsync(retained, delivered: false); return; }
                catch (Exception error)
                {
                    if (!reported) { log.Warn(error, "Appearance compensation retains its original payment for retry."); reported = true; }
                    await Task.Delay(250);
                }
            }
        }
        internal void AfterRollback() { wallets?.Dispose(); wallets = null; intent = null; observation = null; }
        internal bool ObserveCommitted()
        {
            if (adopted) return true;
            observation ??= wallets.ReadAsync(auth.CreateContext);
            if (!observation.IsCompleted) return false;
            try { observation.GetAwaiter().GetResult(); }
            catch (Exception error) { log.Warn(error, "Appearance wallet observation remains pending."); observation = null; return false; }
            wallets.Adopt(); adopted = true; return true;
        }
        internal IEnumerable<Func<IWritable>> CapturePublication() => wallets.CapturePublication();
        internal void CancelBeforeAttempt() => wallets?.Dispose();
    }
}
