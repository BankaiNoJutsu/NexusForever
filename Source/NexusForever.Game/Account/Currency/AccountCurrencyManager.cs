using NexusForever.Database.Auth;
using NexusForever.Database.Auth.Model;
using NexusForever.Game.Abstract.Account;
using NexusForever.Game.Abstract.Account.Currency;
using NexusForever.Game.Static.Account;
using NexusForever.GameTable;
using NexusForever.GameTable.Model;
using NexusForever.Network.World.Message.Model;
using NexusForever.Network.Session;

namespace NexusForever.Game.Account.Currency
{
    public partial class AccountCurrencyManager : IAccountCurrencyManager, IAccountCurrencyMutationScopes, IAccountCurrencySourceScopes
    {
        private readonly Dictionary<AccountCurrencyType, IAccountCurrency> currencies = new();

        private readonly IAccount account;
        private readonly IGameTableManager gameTableManager;

        /// <summary>
        /// Create a new <see cref="IAccountCurrencyManager"/> from an existing database model.
        /// </summary>
        public AccountCurrencyManager(IAccount account, AccountModel model, IGameTableManager gameTableManager)
        {
            this.account          = account;
            this.gameTableManager = gameTableManager;

            foreach (AccountCurrencyModel currencyModel in model.AccountCurrency)
                currencies.Add((AccountCurrencyType)currencyModel.CurrencyId, new AccountCurrency(account, currencyModel, gameTableManager));
            committedCosmicWallet = GetCurrencyAmount(AccountCurrencyType.CosmicReward);
            committedCosmicRevision = model.Loyalty?.Revision ?? 0;
        }

        public void Save(AuthContext context)
        {
            lock (currencyGate)
            {
                if (walletRefresh != null)
                {
                    context.RejectSaveUnit();
                    throw new InvalidOperationException("An authoritative wallet observation still owns this account.");
                }
                foreach (IAccountCurrency accountCurrency in currencies.Values)
                    if (accountCurrency.CurrencyId != AccountCurrencyType.CosmicReward || account.LoyaltyManager == null)
                        accountCurrency.Save(context);
            }
            account.LoyaltyManager?.Save(context);
        }

        /// <summary>
        /// Create a new <see cref="IAccountCurrency"/>.
        /// </summary>
        private IAccountCurrency CreateAccountCurrency(AccountCurrencyType currencyType, ulong amount = 0)
        {
            AccountCurrencyTypeEntry currencyEntry = gameTableManager.AccountCurrencyType?.GetEntry((ulong)currencyType);
            if (currencyEntry == null)
                throw new ArgumentNullException($"AccountCurrencyTypeEntry not found for currencyId {currencyType}");

            if (currencies.TryAdd(currencyType, new AccountCurrency(account, currencyType, amount, gameTableManager)))
                return currencies[currencyType];

            return null;
        }

        /// <summary>
        /// Returns the current amount of a supplied account currency type.
        /// </summary>
        public ulong GetCurrencyAmount(AccountCurrencyType currencyType)
        {
            lock (currencyGate)
                return currencies.TryGetValue(currencyType, out IAccountCurrency accountCurrency) ? accountCurrency.Amount : 0ul;
        }

        /// <summary>
        /// Returns whether the Account has enough of the currency to afford the amount.
        /// </summary>
        public bool CanAfford(AccountCurrencyType currencyType, ulong amount)
        {
            if (!currencies.TryGetValue(currencyType, out IAccountCurrency accountCurrency))
                return false;

            return accountCurrency.CanAfford(amount);
        }

        /// <summary>
        /// Add a supplied amount to an <see cref="IAccountCurrency"/>.
        /// </summary>
        public void CurrencyAddAmount(AccountCurrencyType currencyType, ulong amount, ulong reason = 0)
        {
            lock (currencyGate)
                CurrencyAddAmountCore(currencyType, amount, reason);
        }

        private void CurrencyAddAmountCore(AccountCurrencyType currencyType, ulong amount, ulong reason)
        {
            if (!currencies.TryGetValue(currencyType, out IAccountCurrency accountCurrency))
                accountCurrency = CreateAccountCurrency(currencyType, 0);

            if (accountCurrency == null)
                throw new ArgumentException($"Account Currency entry not found for currencyId {currencyType}.");

            accountCurrency.AddAmount(amount);
            if (currencyType == AccountCurrencyType.CosmicReward)
                RecordCosmicMutation(amount, subtract: false);
            SendAccountCurrencyUpdate(accountCurrency, reason);
        }

        /// <summary>
        /// Subtract a supplied amount from an <see cref="IAccountCurrency"/>.
        /// </summary>
        public void CurrencySubtractAmount(AccountCurrencyType currencyType, ulong amount, ulong reason = 0)
        {
            lock (currencyGate)
                CurrencySubtractAmountCore(currencyType, amount, reason);
        }

        /// <summary>Stage the wallet operations without changing the visible balance before commit.</summary>
        public Action EnlistFortuneSpend(AuthContext context, ulong cost, ulong bundleCredit, Func<bool> ownsDeal)
        {
            lock (currencyGate)
            {
                if (walletRefresh != null || !ownsDeal())
                    throw new InvalidOperationException("The Fortune deal lost its wallet owner.");
                if (!currencies.TryGetValue(AccountCurrencyType.FortuneCoin, out IAccountCurrency wallet))
                {
                    wallet = new AccountCurrency(account, new AccountCurrencyModel
                    {
                        Id = account.Id,
                        CurrencyId = (byte)AccountCurrencyType.FortuneCoin
                    }, gameTableManager);
                    currencies.Add(AccountCurrencyType.FortuneCoin, wallet);
                }
                ((AccountCurrency)wallet).EnlistFortuneSpend(context, cost, bundleCredit);
                IGameSession session = account.Session;
                return () => SendAccountCurrencyUpdate(wallet, 0, session, ownsDeal);
            }
        }

        public bool HasPendingFortuneChanges()
        {
            lock (currencyGate)
                return currencies.TryGetValue(AccountCurrencyType.FortuneCoin, out IAccountCurrency value)
                    && value is AccountCurrency wallet && wallet.HasPendingOperations;
        }

        private void CurrencySubtractAmountCore(AccountCurrencyType currencyType, ulong amount, ulong reason)
        {
            if (!currencies.TryGetValue(currencyType, out IAccountCurrency accountCurrency))
                accountCurrency = CreateAccountCurrency(currencyType, 0);

            if (accountCurrency == null)
                throw new ArgumentException($"Account Currency entry not found for currencyId {currencyType}.");

            if (!accountCurrency.CanAfford(amount))
                throw new ArgumentException($"Trying to remove more currency {accountCurrency.CurrencyId} than the player has!");

            accountCurrency.SubtractAmount(amount);
            if (currencyType == AccountCurrencyType.CosmicReward)
                RecordCosmicMutation(amount, subtract: true);
            SendAccountCurrencyUpdate(accountCurrency, reason);
        }

        /// <summary>
        /// Sends information about all the player's <see cref="IAccountCurrency"/> during Character Select.
        /// </summary>
        public void SendCharacterListPacket()
        {
            account.Session.EnqueueMessageEncrypted(new ServerAccountCurrencySet
            {
                AccountCurrencies = currencies.Values.Select(c => c.Build()).ToList()
            });
        }

        /// <summary>
        /// Sends information about all the player's <see cref="IAccountCurrency"/> when entering world.
        /// </summary>
        public void SendInitialPackets()
        {
            foreach (IAccountCurrency accountCurrency in currencies.Values)
                SendAccountCurrencyUpdate(accountCurrency);
        }

        /// <summary>
        /// Sends information about a player's <see cref="AccountCurrency"/>.
        /// </summary>
        private void SendAccountCurrencyUpdate(IAccountCurrency accountCurrency, ulong reason = 0)
            => SendAccountCurrencyUpdate(accountCurrency, reason, account.Session, null);

        private static void SendAccountCurrencyUpdate(IAccountCurrency accountCurrency, ulong reason, IGameSession session, Func<bool> canPublish)
        {
            if (canPublish?.Invoke() == false)
                return;
            session.EnqueueMessageEncrypted(new ServerAccountCurrencyGrant
            {
                AccountCurrency = accountCurrency.Build(),
                Reason = reason
            });
            if (canPublish?.Invoke() != false)
                session.EnqueueMessageEncrypted(new ServerWalletUpdate((uint)accountCurrency.CurrencyId));
        }
    }
}
