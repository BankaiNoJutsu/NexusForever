using Microsoft.EntityFrameworkCore;
using NexusForever.Database.Character;
using NexusForever.Database.Character.Model;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Quest;
using NexusForever.Game.Static.Entity;
using NexusForever.GameTable;
using NexusForever.GameTable.Model;

namespace NexusForever.Game.Entity
{
    public partial class Currency : ICurrency
    {
        /// <summary>
        /// Determines which fields need saving for <see cref="ICurrency"/> when being saved to the database.
        /// </summary>
        [Flags]
        public enum CurrencySaveMask
        {
            None   = 0x0000,
            Create = 0x0001,
            Delete = 0x0002,
            Amount = 0x0004
        }

        public CurrencyTypeEntry Entry { get; set; }
        public CurrencyType Id => (CurrencyType)Entry.Id;
        public ulong CharacterId { get; set; }

        public ulong Amount
        {
            get { lock (persistence.Gate) return amount; }
            set
            {
                lock (persistence.Gate)
                {
                    AssertIdentity();
                    persistence.AssertRewardOwner();
                    if (Entry.CapAmount > 0 && value > Entry.CapAmount)
                        throw new ArgumentOutOfRangeException();
                    amount = value;
                    persistence.Dirty((int)CurrencySaveMask.Amount);
                }
            }
        }

        private ulong amount;

        private readonly ulong owner;
        private readonly byte currencyId;
        // Reuse the commit-observed row owner and its Character-wide frozen/reward
        // exclusion. A failed flush or later actor mutation cannot clear this row.
        private readonly QuestRowPersistence<CharacterCurrencyModel, CurrencyState> persistence;
        private sealed record CurrencyState(ulong Character, byte Currency, ulong Amount);
        internal bool NeedsPersistenceSave => persistence.NeedsSave;

        private void AssertIdentity()
        {
            if (CharacterId != owner || Entry == null || Entry.Id != currencyId)
                throw new InvalidOperationException("Currency changed its loaded Character or currency identity.");
        }

        private QuestRowPersistence<CharacterCurrencyModel, CurrencyState> CreatePersistence(CurrencyState baseline)
            => new($"currency:{owner}:{currencyId}", $"{owner}:currency", baseline,
                context => context.CharacterCurrency.AsNoTracking().SingleOrDefault(row => row.Id == owner && row.CurrencyId == currencyId),
                row => row == null ? null : new(row.Id, row.CurrencyId, row.Amount),
                value => new() { Id = value.Character, CurrencyId = value.Currency, Amount = value.Amount },
                () => { AssertIdentity(); return new(CharacterId, currencyId, amount); });

        /// <summary>
        /// Create a new <see cref="ICurrency"/> from an existing database model.
        /// </summary>
        public Currency(CharacterCurrencyModel model, IGameTableManager gameTableManager)
        {
            CharacterId = model.Id;
            Entry       = gameTableManager.CurrencyType.GetEntry(model.CurrencyId);
            if (Entry == null || Entry.CapAmount > 0 && model.Amount > Entry.CapAmount)
                throw new ArgumentOutOfRangeException(nameof(model));
            amount = model.Amount; owner = model.Id; currencyId = model.CurrencyId;
            persistence = CreatePersistence(new(owner, currencyId, amount));
        }

        /// <summary>
        /// Create a new <see cref="ICurrency"/> from an <see cref="CurrencyTypeEntry"/> template.
        /// </summary>
        public Currency(ulong owner, CurrencyTypeEntry entry, ulong value = 0u)
        {
            CharacterId = owner;
            Entry       = entry;

            ArgumentNullException.ThrowIfNull(entry);
            if (entry.Id > byte.MaxValue) throw new ArgumentOutOfRangeException(nameof(entry));
            this.owner = owner; currencyId = (byte)entry.Id;
            persistence = CreatePersistence(null);
            persistence.Dirty((int)CurrencySaveMask.Create);
        }

        public void Save(CharacterContext context)
        {
            persistence.Save(context);
        }

        internal Action<CharacterContext> CaptureNormalSave(object token)
        {
            var snapshot = persistence.CaptureNormal(token);
            return context => persistence.Save(context, frozen: snapshot);
        }
    }
}
