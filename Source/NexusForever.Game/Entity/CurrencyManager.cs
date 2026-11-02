using System.Collections;
using NexusForever.Database.Character;
using NexusForever.Database.Character.Model;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Quest;
using NexusForever.Game.Static.Achievement;
using NexusForever.Game.Static.Entity;
using NexusForever.GameTable;
using NexusForever.GameTable.Model;
using NexusForever.Network.World.Message.Model;

namespace NexusForever.Game.Entity
{
    public partial class CurrencyManager : ICurrencyManager
    {
        private IPlayer player;
        private CharacterLoadOrigin loadedOrigin;
        private readonly Dictionary<CurrencyType, ICurrency> currencies = [];

        #region Depenedency Injection

        private readonly IGameTableManager gameTableManager;

        public CurrencyManager(
            IGameTableManager gameTableManager)
        {
            this.gameTableManager = gameTableManager;
        }

        #endregion

        /// <summary>
        /// Initialise <see cref="ICurrencyManager"/> from <see cref="CharacterModel"/> database model.
        /// </summary>
        public void Initialise(IPlayer owner, CharacterModel model)
        {
            if (player != null)
                throw new InvalidOperationException("CurrencyManager has already been initialised!");

            var origin = CharacterLoadOrigin.Get(model);
            if (origin != null && (owner == null || origin.CharacterId != owner.CharacterId || origin.AccountId != owner.Account?.Id))
                throw new InvalidOperationException("Currency manager changed its loaded Character/account owner.");
            player = owner; loadedOrigin = origin;

            foreach (CharacterCurrencyModel currencyModel in model.Currency)
            {
                var currency = new Currency(currencyModel, gameTableManager, AssertBankPublicationMutationAllowed);
                currencies.Add(currency.Id, currency);
            }
        }

        public void Save(CharacterContext context)
        {
            AssertContractCurrencySaveAllowed(context);
            AssertLoadedCurrencyContext(context);
            foreach (ICurrency currency in currencies.Values)
                currency.Save(context);
        }

        internal bool HasPendingPersistenceSave => currencies.Values.OfType<Currency>().Any(currency => currency.NeedsPersistenceSave);

        internal Action<CharacterContext> CaptureNormalSave(object token)
        {
            AssertContractCurrencySaveAllowed();
            AssertLoadedCurrencyOwner();
            var rows = currencies.Values.Select(currency => currency is Currency concrete ? concrete.CaptureNormalSave(token)
                : throw new InvalidOperationException("Frozen currency save requires concrete currency owners.")).ToArray();
            return context => { AssertLoadedCurrencyContext(context); foreach (var row in rows) row(context); };
        }

        private bool HasLoadedCurrencyOwner() => loadedOrigin == null || player != null
            && loadedOrigin.CharacterId == player.CharacterId && loadedOrigin.AccountId == player.Account?.Id;

        private void AssertLoadedCurrencyOwner()
        {
            if (!HasLoadedCurrencyOwner()) throw new InvalidOperationException("Currency lost its loaded Character/account owner.");
        }

        private void AssertLoadedCurrencyContext(CharacterContext context)
        {
            if (loadedOrigin != null) CharacterDatabaseIdentity.Require(context, loadedOrigin.Database);
        }

        private void AssertBankPublicationMutationAllowed()
        {
            if (player is Player { IsBankPublicationInProgress: true })
                throw new InvalidOperationException("Bank publication cannot accept a reentrant currency mutation.");
        }

        private Currency CreateCurrencyOwner(CurrencyTypeEntry entry)
        {
            AssertBankPublicationMutationAllowed(); AssertLoadedCurrencyOwner();
            return new Currency(player.CharacterId, entry, loadedOrigin?.Database, AssertBankPublicationMutationAllowed);
        }

        /// <summary>
        /// Returns total amount of acquired <see cref="CurrencyType"/>.
        /// </summary>
        private ulong? GetCurrency(CurrencyType currencyId)
        {
            if (!currencies.TryGetValue(currencyId, out ICurrency currency))
                return null;
            return currency.Amount;
        }

        /// <summary>
        /// Returns if <see cref="IPlayer"/> has the supplied amount of <see cref="CurrencyType"/>.
        /// </summary>
        public bool CanAfford(CurrencyType currencyId, ulong amount)
        {
            CurrencyTypeEntry currencyEntry = gameTableManager.CurrencyType?.GetEntry((ulong)currencyId);
            if (currencyEntry == null)
                throw new ArgumentNullException();

            // it is possible to check for a currency amount of 0 without having the currency in the store
            // in this scenario we should allow the check to pass
            if (amount == 0)
                return true;

            if (ContractCurrencyBlocked(currencyId)) return false;

            return GetCurrency(currencyId) >= amount;
        }

        /// <summary>
        /// Add supplied amount of <see cref="CurrencyType"/> currency.
        /// </summary>
        public void CurrencyAddAmount(CurrencyType currencyId, ulong amount, bool isLoot = false)
        {
            CurrencyTypeEntry currencyEntry = gameTableManager.CurrencyType?.GetEntry((ulong)currencyId);
            if (currencyEntry == null)
                throw new ArgumentNullException();

            CurrencyAddAmount(currencyEntry, amount, isLoot);
        }

        private void CurrencyAddAmount(CurrencyTypeEntry currencyEntry, ulong amount, bool isLoot = false)
        {
            if (currencyEntry == null)
                throw new ArgumentNullException();

            AssertBankPublicationMutationAllowed();
            if (DeferContractCurrency((CurrencyType)currencyEntry.Id, amount, false, isLoot)) return;

            ApplyCurrencyAdd(currencyEntry, amount, isLoot);
        }

        private void ApplyCurrencyAdd(CurrencyTypeEntry currencyEntry, ulong amount, bool isLoot)
        {

            if (!currencies.TryGetValue((CurrencyType)currencyEntry.Id, out ICurrency currency))
                currency = CurrencyCreate(currencyEntry);

            amount = AddCurrencyAmount(currency.Amount, amount, currency.Entry.CapAmount);

            ulong addedAmount = amount - currency.Amount;
            CurrencyAmountUpdate(currency, amount, isLoot);

            if (addedAmount != 0ul)
            {
                player.AchievementManager.CheckAchievements(player, AchievementType.CurrencyEarned, (uint)currency.Entry.Id, count: ToAchievementCount(addedAmount));
                CurrencyQuestObjectiveUpdater.OnCurrencyAdded(player, currency.Id, addedAmount);
            }
        }

        private ICurrency CurrencyCreate(CurrencyTypeEntry currencyEntry)
        {
            if (currencyEntry == null)
                return null;

            if (currencies.ContainsKey((CurrencyType)currencyEntry.Id))
                throw new ArgumentException($"Currency {currencyEntry.Id} is already added to the player!");

            var currency = CreateCurrencyOwner(currencyEntry);
            currencies.Add(currency.Id, currency);
            return currency;
        }

        /// <summary>
        /// Remove supplied amount of <see cref="CurrencyType"/> currency.
        /// </summary>
        public void CurrencySubtractAmount(CurrencyType currencyId, ulong amount, bool isLoot = false)
        {
            CurrencyTypeEntry currencyEntry = gameTableManager.CurrencyType?.GetEntry((ulong)currencyId);
            if (currencyEntry == null)
                throw new ArgumentNullException();

            CurrencySubtractAmount(currencyEntry, amount, isLoot);
        }

        private void CurrencySubtractAmount(CurrencyTypeEntry currencyEntry, ulong amount, bool isLoot = false)
        {
            if (currencyEntry == null)
                throw new ArgumentNullException();

            AssertBankPublicationMutationAllowed();
            if (DeferContractCurrency((CurrencyType)currencyEntry.Id, amount, true, isLoot)) return;

            ApplyCurrencySubtract(currencyEntry, amount, isLoot);
        }

        private void ApplyCurrencySubtract(CurrencyTypeEntry currencyEntry, ulong amount, bool isLoot)
        {

            // it is possible to subtract a currency amount of 0 without having the currency in the store
            // in this scenario we should allow this to pass
            if (amount == 0)
                return;

            if (!currencies.TryGetValue((CurrencyType)currencyEntry.Id, out ICurrency currency))
                throw new ArgumentException($"Cannot create currency {currencyEntry.Id} with a negative amount!");
            if (currency.Amount < amount)
                throw new ArgumentException($"Trying to remove more currency {currencyEntry.Id} than the player has!");

            CurrencyAmountUpdate(currency, currency.Amount - amount, isLoot);
        }

        /// <summary>
        /// Update <see cref="ICurrency"/> with supplied amount of currency.
        /// </summary>
        private void CurrencyAmountUpdate(ICurrency currency, ulong amount, bool isLoot = false)
        {
            if (currency == null)
                throw new ArgumentNullException();

            if (isLoot)
            {
                player.Session.EnqueueMessageEncrypted(new ServerChannelUpdateLoot
                {
                    CurrencyId = currency.Id,
                    Amount = amount - currency.Amount
                });
            }

            currency.Amount = amount;

            player.Session.EnqueueMessageEncrypted(new ServerCombatReward
            {
                Stat = (byte)(currency.Id - 1),
                NewValue = currency.Amount
            });
        }

        private static uint ToAchievementCount(ulong amount)
        {
            return amount > uint.MaxValue ? uint.MaxValue : (uint)amount;
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public IEnumerator<ICurrency> GetEnumerator()
        {
            return currencies.Values.GetEnumerator();
        }
    }
}
