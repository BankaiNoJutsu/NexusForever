using NexusForever.Database.Auth;
using NexusForever.Game.Account.Currency;
using NexusForever.Game.Entity;
using NexusForever.Game.Static.Account;
using NexusForever.Network.Message;
using NLog;

namespace NexusForever.Game.Guild
{
    // Owns the token side of the same complete Character creation receipt.
    // SQL workers retain only the immutable payment; wallet adoption is on World.
    internal sealed class CommunityTokenPayment
    {
        private static readonly Logger log = LogManager.GetCurrentClassLogger();
        private readonly Player player;
        private readonly AccountCurrencyManager manager;
        private readonly AuthDatabase auth;
        private readonly VendorAccountPurchase protocol;
        private readonly Func<bool> current;
        private readonly ulong guild, residence, epoch;
        private readonly string name;
        private readonly uint account;
        private readonly ulong character;
        private AccountCurrencyManager.VendorWallets wallets;
        private VendorAccountPurchase.Intent intent;
        private Task observation;
        private bool adopted;
        internal ulong Fee { get; }
        internal string AuthRoute { get; }

        internal CommunityTokenPayment(Player player, ulong guild, ulong residence, string name, ulong fee, Func<bool> current)
        {
            this.player = player; this.guild = guild; this.residence = residence; this.name = name; Fee = fee; this.current = current;
            account = player.Account.Id; character = player.CharacterId; epoch = player.Account.WorldSessionEpoch;
            manager = player.Account.CurrencyManager as AccountCurrencyManager
                ?? throw new InvalidOperationException("Community tokens require the original admitted wallet manager.");
            auth = player.GetAuthDatabase();
            using (var context = auth.CreateContext()) AuthRoute = AccountCurrencyOperationRepository.DatabaseKey(context);
            protocol = new(player.GetCharacterDatabase().CreateContext, auth.CreateContext, player.Identity.RealmId);
        }
        internal void Prepare()
        {
            if (!current() || !ReferenceEquals(player.Account.CurrencyManager, manager))
                throw new InvalidOperationException("Community payment lost its original payer.");
            wallets ??= manager.CaptureVendorWallets([new(AccountCurrencyType.ServiceToken, Fee)]);
        }
        internal Task AuthorizeAsync(string receipt, string payload)
        {
            if (!current() || wallets == null || adopted) throw new InvalidOperationException("Community payment lost its retained wallet reservation.");
            intent = protocol.CreateCommunity(receipt, account, character, guild, residence, name, Fee, epoch, payload);
            if (intent.AuthRoute != AuthRoute) throw new InvalidOperationException("Community payment changed its accepted Auth route.");
            return protocol.AuthorizeCommunityAsync(intent);
        }
        internal async Task RollbackAsync(string receipt)
        {
            var retained = intent;
            if (retained == null || retained.Id != receipt) return;
            bool reported = false;
            while (true)
            {
                try { await protocol.ResolveWalletAsync(retained, delivered: false); return; }
                catch (Exception error)
                {
                    if (!reported) { log.Warn(error, "Community token compensation retains the original Character outcome and payer for retry."); reported = true; }
                    await Task.Delay(250);
                }
            }
        }
        internal void AfterRollback()
        {
            wallets?.Dispose(); wallets = null; intent = null; observation = null;
        }
        internal bool ObserveCommitted()
        {
            if (adopted) return true;
            observation ??= wallets.ReadAsync(auth.CreateContext);
            if (!observation.IsCompleted) return false;
            try { observation.GetAwaiter().GetResult(); }
            catch (Exception error)
            {
                log.Warn(error, "Committed Community tokens retain their original wallet observation for retry."); observation = null; return false;
            }
            wallets.Adopt(); adopted = true; return true;
        }
        internal IEnumerable<Func<IWritable>> CapturePublication() => wallets.CapturePublication();
        internal void CancelBeforeAttempt() => wallets?.Dispose();
    }
}
