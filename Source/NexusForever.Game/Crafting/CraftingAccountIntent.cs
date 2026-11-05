using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using NexusForever.Database.Character;
using NexusForever.Database.Auth.Model;
using NexusForever.Database.Auth;
using NexusForever.Game.Account.Currency;
using NexusForever.Database.Character.Model;
using NexusForever.Game.Account.Reward;
using NexusForever.Game.Static.Loot;

namespace NexusForever.Game.Crafting
{
    internal sealed record CraftingAccountAward(LootItemType Type, uint Id, uint Amount);

    internal sealed record CraftingAccountIntent(int Version, string Id, uint Account, ulong Character, ushort Realm,
        string CharacterRoute, string AuthRoute, CraftingAccountAward[] Awards, ulong FirstInventoryId, ulong Epoch,
        string LoyaltyFingerprint, uint[] LoyaltyPrerequisites)
    {
        internal static readonly JsonSerializerOptions JsonOptions = new() { IncludeFields = true };
        internal string Json => JsonSerializer.Serialize(this, JsonOptions);
        internal string Key(int ordinal, uint item = 0) => $"craft-award:{Realm}:{Id}:{ordinal}:{item}";
        internal ulong InventoryCount => Awards.Where(award => award.Type == LootItemType.AccountItem).Aggregate(0ul, (count, award) => checked(count + award.Amount));

        internal static string Fingerprint(LoyaltyCatalog catalog)
            => Convert.ToHexString(SHA256.HashData(JsonSerializer.SerializeToUtf8Bytes(catalog.Tracks.Select(track =>
                new { track.Entry, Rewards = track.Slots.Select(slot => slot.Entry).ToArray() }).ToArray(), JsonOptions)));

        internal void Validate() => ValidateFields(false);

        private void ValidateFields(bool reserving)
        {
            if (Version != 1 || !Guid.TryParseExact(Id, "N", out var id) || id == Guid.Empty
                || Account == 0 || Character == 0 || Realm == 0 || Epoch == 0 || string.IsNullOrEmpty(CharacterRoute) || string.IsNullOrEmpty(AuthRoute)
                || Awards == null || Awards.Length == 0 || Awards.Any(award => award == null || award.Id == 0 || award.Amount == 0
                    || award.Type is not (LootItemType.AccountCurrency or LootItemType.AccountItem)
                    || award.Type == LootItemType.AccountCurrency && (!Enum.IsDefined((Static.Account.AccountCurrencyType)award.Id) || award.Id > byte.MaxValue))
                || LoyaltyPrerequisites == null || LoyaltyPrerequisites.Distinct().Count() != LoyaltyPrerequisites.Length
                || Json.Length > 131072)
                throw new InvalidOperationException("Crafting award intent lost its original bounded identity.");
            if ((!reserving && (InventoryCount == 0) != (FirstInventoryId == 0)) || FirstInventoryId > ulong.MaxValue - InventoryCount)
                throw new InvalidOperationException("Crafting award lost its reserved inventory identities.");
        }

        internal CraftingAccountIntent Reserve(AuthDatabase database)
        {
            ValidateFields(true);
            if (FirstInventoryId != 0) throw new InvalidOperationException("Crafting inventory identities were already reserved.");
            CraftingAccountIntent reserved = null;
            AccountCraftingReceiptModel anchor = null;
            database.SaveBlocking(context => context.EnlistSaveWork(unit =>
            {
                if (anchor != null) unit.Entry(anchor).State = EntityState.Detached;
                if (AccountCurrencyOperationRepository.DatabaseKey(unit) != AuthRoute)
                    throw new InvalidOperationException("Crafting reservation changed its Auth database.");
                unit.ReservePendingItemIds(Account, 0);
                var epoch = (unit.Database.IsSqlite() ? unit.AccountWorldSession.Where(row => row.AccountId == Account)
                    : unit.AccountWorldSession.FromSqlInterpolated($"SELECT * FROM account_world_session WHERE accountId={Account} FOR UPDATE"))
                    .AsNoTracking().SingleOrDefault();
                if (epoch?.Epoch != Epoch)
                    throw new InvalidOperationException("Crafting reservation lost its admitted account epoch.");
                reserved = this with { FirstInventoryId = InventoryCount == 0 ? 0 : unit.ReserveInventoryIds(Account, InventoryCount) };
                anchor = reserved.Reservation(); unit.AccountCraftingReceipt.Add(anchor);
            }, _ => { }, () =>
            {
                var entry = context.Entry(anchor);
                var stored = entry.GetDatabaseValues();
                return entry.State is not (EntityState.Deleted or EntityState.Detached) && stored != null
                    && Restore(anchor).Json == reserved.Json && !anchor.Delivered && !anchor.Cancelled && !anchor.Completed
                    && entry.CurrentValues.Properties.All(property => Equals(entry.CurrentValues[property.Name], stored[property.Name]));
            }, Account));
            return reserved ?? throw new InvalidOperationException("Crafting reservation was not observed.");
        }

        // Ordinary account mutations stage first. Check the balance they will
        // actually commit, then its SQL postimage, before spending Character
        // materials. A stale cache must not turn an already impossible credit
        // into an unrecoverable paid source.
        internal void EnlistCreditCapacity(AuthContext context)
        {
            Validate();
            void Check(bool stored)
            {
                if (AccountCurrencyOperationRepository.DatabaseKey(context) != AuthRoute)
                    throw new InvalidOperationException("Crafting capacity check changed its Auth route.");
                foreach (var group in Awards.Where(award => award.Type == LootItemType.AccountCurrency).GroupBy(award => award.Id))
                {
                    byte id = checked((byte)group.Key);
                    ulong amount = group.Aggregate(0ul, (total, award) => checked(total + award.Amount));
                    var wallet = stored ? null : context.AccountCurrency.Local.SingleOrDefault(row => row.Id == Account && row.CurrencyId == id);
                    wallet ??= (context.Database.IsSqlite() ? context.AccountCurrency.Where(row => row.Id == Account && row.CurrencyId == id)
                        : context.AccountCurrency.FromSqlInterpolated($"SELECT * FROM account_currency WHERE id={Account} AND currencyId={id} FOR UPDATE"))
                        .AsNoTracking().SingleOrDefault();
                    ulong balance = wallet?.Amount ?? 0;
                    _ = checked(balance + amount);
                    if (id == (byte)Static.Account.AccountCurrencyType.CosmicReward && LoyaltyFingerprint != null)
                    {
                        var ledger = stored ? null : context.AccountLoyalty.Local.SingleOrDefault(row => row.AccountId == Account);
                        ledger ??= LoyaltyRepository.ReadLedger(context, Account);
                        _ = checked((ledger?.EarnedPoints ?? balance) + amount);
                    }
                }
            }
            context.EnlistSaveWork(unit => { unit.ReservePendingItemIds(Account, 0); Check(false); }, _ => { },
                () => { Check(true); return true; }, Account);
        }

        internal CharacterCraftingAccountOutboxModel Model()
        {
            Validate();
            return new() { Id = Id, CharacterId = Character, AccountId = Account, RealmId = Realm, PayloadJson = Json };
        }

        internal AccountCraftingReceiptModel Reservation()
        {
            Validate();
            return new() { Id = Id, AccountId = Account, RealmId = Realm, PayloadJson = Json };
        }

        internal static CraftingAccountIntent Restore(AccountCraftingReceiptModel row)
        {
            var intent = JsonSerializer.Deserialize<CraftingAccountIntent>(row.PayloadJson, JsonOptions)
                ?? throw new InvalidOperationException("Missing crafting reservation.");
            intent.Validate();
            if (intent.Id != row.Id || intent.Account != row.AccountId || intent.Realm != row.RealmId || intent.Json != row.PayloadJson
                || row.Delivered && row.Cancelled || row.Completed && !row.Delivered && !row.Cancelled)
                throw new InvalidOperationException("Crafting reservation changed its original owner/state.");
            return intent;
        }

        internal void Stage(CharacterContext context)
        {
            if (context.Database.CurrentTransaction == null || Entity.Item.GetRewardDatabaseKey(context) != CharacterRoute
                || context.Character.AsNoTracking().SingleOrDefault(row => row.Id == Character)?.AccountId != Account)
                throw new InvalidOperationException("Crafting award requires its original atomic Character owner.");
            if (context.CharacterCraftingAccountOutbox.AsNoTracking().Any(row => row.Id == Id))
                throw new InvalidOperationException("Crafting source was already committed or fenced by a newer admission.");
            var model = Model(); context.CharacterCraftingAccountOutbox.Add(model);
            context.OnSaveValidation(phase =>
            {
                var entry = context.Entry(model);
                if (entry.State is EntityState.Deleted or EntityState.Detached || Restore(model).Json != Json || model.Delivered || model.Cancelled)
                    throw new InvalidOperationException("Crafting source changed before Character commit.");
                if (phase == CharacterSaveValidationPhase.BeforeExplicitCommit)
                {
                    var stored = context.CharacterCraftingAccountOutbox.AsNoTracking().SingleOrDefault(row => row.Id == Id);
                    if (stored == null || Restore(stored).Json != Json || stored.Delivered || stored.Cancelled)
                        throw new InvalidOperationException("Crafting source did not reach its Character transaction.");
                }
            });
        }

        internal static CraftingAccountIntent Restore(CharacterCraftingAccountOutboxModel row)
        {
            var intent = JsonSerializer.Deserialize<CraftingAccountIntent>(row.PayloadJson, JsonOptions)
                ?? throw new InvalidOperationException("Missing crafting award intent.");
            intent.Validate();
            if (intent.Id != row.Id || intent.Character != row.CharacterId || intent.Account != row.AccountId
                || intent.Realm != row.RealmId || intent.Json != row.PayloadJson || row.Delivered && row.Cancelled)
                throw new InvalidOperationException("Crafting award intent changed its source owner.");
            return intent;
        }
    }
}
