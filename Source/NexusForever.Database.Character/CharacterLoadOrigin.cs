using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using NexusForever.Database.Character.Model;

namespace NexusForever.Database.Character
{
    // Only owned Character queries/confirmed creation can register origins. Neither scalar
    // equality, a clone nor a later context can manufacture a load identity.
    public sealed class CharacterLoadOrigin
    {
        private static readonly ConditionalWeakTable<CharacterModel, CharacterLoadOrigin> characters = new();
        private static readonly ConditionalWeakTable<CharacterCurrencyModel, CurrencyOrigin> currencies = new();
        private sealed record CurrencyOrigin(CharacterLoadOrigin Owner, ulong Character, byte Currency, ulong Amount)
        {
            internal bool Matches(CharacterCurrencyModel row)
                => row.Id == Character && row.CurrencyId == Currency && row.Amount == Amount;
        }
        public string Database { get; }
        public ulong CharacterId { get; }
        public uint AccountId { get; }
        private readonly int currencyCount;

        private CharacterLoadOrigin(string database, CharacterModel model)
        {
            Database = database; CharacterId = model.Id; AccountId = model.AccountId;
            currencyCount = model.Currency.Count;
        }

        internal static void Capture(CharacterContext context, IReadOnlyCollection<CharacterModel> models)
            => Prepare(context, models)();

        // Keep candidate object identities private until the owning query or
        // creation transaction succeeds. Preparation alone grants no authority.
        internal static Action Prepare(CharacterContext context, IReadOnlyCollection<CharacterModel> models)
        {
            string database = CharacterDatabaseIdentity.Get(context);
            var candidates = models.Select(model =>
            {
                var origin = new CharacterLoadOrigin(database, model);
                return (Model: model, Origin: origin, Rows: model.Currency.Select(row =>
                    (Model: row, Origin: new CurrencyOrigin(origin, row.Id, row.CurrencyId, row.Amount))).ToArray());
            }).ToArray();
            return () =>
            {
                foreach (var candidate in candidates)
                    if (candidate.Model.Id != candidate.Origin.CharacterId || candidate.Model.AccountId != candidate.Origin.AccountId
                        || candidate.Model.Currency.Count != candidate.Rows.Length
                        || candidate.Rows.Any(row => !candidate.Model.Currency.Contains(row.Model) || !row.Origin.Matches(row.Model)))
                        throw new InvalidOperationException("Character changed before its database origin was confirmed.");
                foreach (var candidate in candidates)
                {
                    characters.Add(candidate.Model, candidate.Origin);
                    foreach (var row in candidate.Rows) currencies.Add(row.Model, row.Origin);
                }
            };
        }

        // The returned value contains scalars only and retains no EF model
        // graphs. Weak table keys supply model/row identity during validation.
        public static CharacterLoadOrigin Get(CharacterModel model)
        {
            ArgumentNullException.ThrowIfNull(model);
            if (!characters.TryGetValue(model, out var origin)) return null;
            if (model.Id != origin.CharacterId || model.AccountId != origin.AccountId || model.Currency == null
                || model.Currency.Count != origin.currencyCount
                || model.Currency.Any(row => row == null)
                || model.Currency.Select(row => row.CurrencyId).Distinct().Count() != origin.currencyCount
                || model.Currency.Any(row => !currencies.TryGetValue(row, out var retained)
                    || !ReferenceEquals(retained.Owner, origin) || !retained.Matches(row)))
                throw new InvalidOperationException("Character changed its loaded currency origin.");
            return origin;
        }

        public static string GetCurrencyDatabase(CharacterCurrencyModel model)
        {
            ArgumentNullException.ThrowIfNull(model);
            if (!currencies.TryGetValue(model, out var origin)) return null;
            if (!origin.Matches(model)) throw new InvalidOperationException("Currency changed its loaded database origin.");
            return origin.Owner.Database;
        }
    }
}
