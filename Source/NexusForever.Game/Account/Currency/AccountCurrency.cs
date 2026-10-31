using Microsoft.EntityFrameworkCore.ChangeTracking;
using NexusForever.Database.Auth;
using NexusForever.Database.Auth.Model;
using NexusForever.Game.Abstract.Account;
using NexusForever.Game.Abstract.Account.Currency;
using NexusForever.Game.Static.Account;
using NexusForever.GameTable;
using NexusForever.GameTable.Model;
using ServerAccountCurrency = NexusForever.Network.World.Message.Model.Shared.AccountCurrency;

namespace NexusForever.Game.Account.Currency
{
    public partial class AccountCurrency : IAccountCurrency
    {
        [Flags]
        public enum AccountCurrencySaveMask
        {
            None   = 0x0000,
            Create = 0x0001,
            Amount = 0x0002
        }

        public AccountCurrencyType CurrencyId { get; }
        public AccountCurrencyTypeEntry Entry { get; }

        public ulong Amount
        {
            get { lock (saveGate) return amount; }
        }

        private ulong amount;

        private AccountCurrencySaveMask saveMask;

        private readonly IAccount account;

        /// <summary>
        /// Create a new <see cref="IAccountCurrency"/> from an <see cref="AccountCurrencyModel"/>.
        /// </summary>
        public AccountCurrency(IAccount account, AccountCurrencyModel model, IGameTableManager gameTableManager)
        {
            this.account = account;
            CurrencyId   = (AccountCurrencyType)model.CurrencyId;
            amount       = model.Amount;
            Entry        = gameTableManager.AccountCurrencyType?.GetEntry((ulong)CurrencyId);

            saveMask = AccountCurrencySaveMask.None;
        }

        /// <summary>
        /// Create a new <see cref="IAccountCurrency"/>.
        /// </summary>
        public AccountCurrency(IAccount account, AccountCurrencyType currencyType, ulong amount, IGameTableManager gameTableManager)
        {
            this.account = account;
            CurrencyId   = currencyType;
            this.amount  = amount;
            Entry        = gameTableManager.AccountCurrencyType?.GetEntry((ulong)CurrencyId);

            saveMask = AccountCurrencySaveMask.Create;
            operations.Add(NewOperation(amount, false));
        }

        internal void SetJournalAmount(ulong value)
        {
            lock (saveGate)
            {
                if (pendingSave != null || pendingRefresh != null) throw new InvalidOperationException("A wallet save or observation still owns this currency.");
                amount = value; operations.Clear(); saveMask = AccountCurrencySaveMask.None;
            }
        }

        /// <summary>
        /// Add a supplied amount to <see cref="IAccountCurrency"/>.
        /// </summary>
        public void AddAmount(ulong amount)
        {
            lock (saveGate)
            {
                EnsureCurrent();
                ulong after = checked(this.amount + amount);
                if (amount != 0) operations.Add(NewOperation(amount, false));
                this.amount = after;
                if (amount != 0) saveMask |= AccountCurrencySaveMask.Amount;
            }
        }

        /// <summary>
        /// Subtract a supplied amount from <see cref="IAccountCurrency"/>.
        /// </summary>
        public void SubtractAmount(ulong amount)
        {
            lock (saveGate)
            {
                EnsureCurrent();
                ulong after = checked(this.amount - amount);
                if (amount != 0) operations.Add(NewOperation(amount, true));
                this.amount = after;
                if (amount != 0) saveMask |= AccountCurrencySaveMask.Amount;
            }
        }

        /// <summary>
        /// Returns if amount can be subtracted from <see cref="IAccountCurrency"/>.
        /// </summary>
        public bool CanAfford(ulong amount)
        {
            lock (saveGate) return !requiresReload && this.amount >= amount;
        }

        public ServerAccountCurrency Build()
        {
            return new ServerAccountCurrency
            {
                AccountCurrencyType = (byte)CurrencyId,
                Amount              = Amount
            };
        }
    }
}
