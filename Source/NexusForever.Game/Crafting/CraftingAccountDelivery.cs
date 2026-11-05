using System.Data;
using Microsoft.EntityFrameworkCore;
using NexusForever.Database.Auth;
using NexusForever.Database.Auth.Model;
using NexusForever.Database.Character;
using NexusForever.Game.Account.Currency;
using NexusForever.Game.Account.Reward;
using NexusForever.Game.Entity;
using NexusForever.Game.Static.Account;
using NexusForever.Game.Static.Loot;

namespace NexusForever.Game.Crafting
{
    internal sealed record CraftingAccountSnapshot(AccountInventoryModel[] Items, LoyaltyState Loyalty);
    // Only copied source identities and private contexts cross the worker seam.
    // The Character source survives until Auth receipts prove the whole bundle.
    internal sealed class CraftingAccountDelivery(Func<CharacterContext> characters, Func<AuthContext> auth,
        ushort realm, LoyaltyCatalog loyalty = null)
    {
        internal async Task DeliverAsync(CraftingAccountIntent expected)
        {
            expected.Validate();
            string id = expected.Id;
            await using var source = Character(expected);
            var row = await source.CharacterCraftingAccountOutbox.AsNoTracking().SingleOrDefaultAsync(value => value.Id == id)
                ?? throw new InvalidOperationException("Crafting award has no committed Character source.");
            var intent = CraftingAccountIntent.Restore(row);
            if (intent.Json != expected.Json) throw new InvalidOperationException("Crafting award changed its retained source intent.");
            if (row.Cancelled) throw new InvalidOperationException("A fenced crafting reservation cannot deliver awards.");
            if (intent.LoyaltyFingerprint != null && (loyalty == null || CraftingAccountIntent.Fingerprint(loyalty) != intent.LoyaltyFingerprint))
                throw new InvalidOperationException("Crafting award requires its retained Loyalty catalog.");

            Exception failure = null;
            bool applied = await ObserveAuth(intent);
            if (!applied)
            {
                if (row.Delivered) throw new InvalidOperationException("Acknowledged crafting award lost its Auth receipt.");
                try { await ApplyAuth(intent); }
                catch (Exception error) { failure = error; }
            }
            if (!await ObserveAuth(intent))
                throw new InvalidOperationException("Crafting account delivery is still pending.", failure);
            if (row.Delivered) { await CompleteAuth(intent, cancelled: false); return; }

            // An acknowledgement failure can always be retried against the same
            // source and receipts. Deleting the Character cannot erase the award.
            await using var context = Character(intent);
            await using var tx = await context.Database.BeginTransactionAsync(IsolationLevel.Serializable);
            var stored = await (context.Database.IsSqlite()
                ? context.CharacterCraftingAccountOutbox.Where(value => value.Id == id)
                : context.CharacterCraftingAccountOutbox.FromSqlInterpolated($"SELECT * FROM character_crafting_account_outbox WHERE id={id} FOR UPDATE")).SingleAsync();
            if (CraftingAccountIntent.Restore(stored).Json != intent.Json)
                throw new InvalidOperationException("Crafting source changed before acknowledgement.");
            stored.Delivered = true;
            context.OnSaveValidation(phase =>
            {
                if (context.Entry(stored).State is EntityState.Deleted or EntityState.Detached || !stored.Delivered
                    || stored.Cancelled || CraftingAccountIntent.Restore(stored).Json != intent.Json)
                    throw new InvalidOperationException("Crafting acknowledgement changed its source identity.");
                if (phase == CharacterSaveValidationPhase.BeforeExplicitCommit)
                {
                    var values = context.Entry(stored).GetDatabaseValues();
                    if (values == null || context.Entry(stored).CurrentValues.Properties.Any(property => !Equals(values[property.Name], context.Entry(stored).CurrentValues[property.Name])))
                        throw new InvalidOperationException("Crafting acknowledgement did not reach its source transaction.");
                }
            });
            try { await context.SaveChangesAsync(); await tx.CommitAsync(); }
            catch
            {
                await tx.DisposeAsync();
                await using var fresh = Character(intent);
                var observed = await fresh.CharacterCraftingAccountOutbox.AsNoTracking().SingleOrDefaultAsync(value => value.Id == id);
                if (observed == null || !observed.Delivered || observed.Cancelled || CraftingAccountIntent.Restore(observed).Json != intent.Json) throw;
            }
            await CompleteAuth(intent, cancelled: false);
        }

        // The same Character-before-outbox lock order as the original save
        // decides whether a newer admission sees a paid source or installs an
        // irreversible cancellation fence. A late old save cannot pass that fence.
        internal async Task<bool> ResolveReservation(CraftingAccountIntent intent, bool delivered)
        {
            intent.Validate();
            await using var context = Character(intent);
            await using var tx = await context.Database.BeginTransactionAsync(IsolationLevel.Serializable);
            await context.Database.ExecuteSqlInterpolatedAsync($"UPDATE `character` SET id=id WHERE id={intent.Character}");
            var row = await (context.Database.IsSqlite() ? context.CharacterCraftingAccountOutbox.Where(value => value.Id == intent.Id)
                : context.CharacterCraftingAccountOutbox.FromSqlInterpolated($"SELECT * FROM character_crafting_account_outbox WHERE id={intent.Id} FOR UPDATE"))
                .AsNoTracking().SingleOrDefaultAsync();
            if (row == null)
            {
                if (delivered) throw new InvalidOperationException("Delivered crafting awards lost their Character history.");
                row = intent.Model(); row.Cancelled = true; context.CharacterCraftingAccountOutbox.Add(row);
                context.OnSaveValidation(phase =>
                {
                    if (context.Entry(row).State is EntityState.Deleted or EntityState.Detached || !row.Cancelled || row.Delivered
                        || CraftingAccountIntent.Restore(row).Json != intent.Json)
                        throw new InvalidOperationException("Crafting cancellation fence changed before commit.");
                    if (phase == CharacterSaveValidationPhase.BeforeExplicitCommit)
                    {
                        var values = context.Entry(row).GetDatabaseValues();
                        if (values == null || context.Entry(row).CurrentValues.Properties.Any(property => !Equals(values[property.Name], context.Entry(row).CurrentValues[property.Name])))
                            throw new InvalidOperationException("Crafting fence did not reach its source transaction.");
                    }
                });
                await context.SaveChangesAsync();
            }
            if (CraftingAccountIntent.Restore(row).Json != intent.Json)
                throw new InvalidOperationException("Crafting reservation changed its frozen Character identity.");
            try { await tx.CommitAsync(); }
            catch
            {
                await tx.DisposeAsync();
                await using var fresh = Character(intent);
                var observed = await fresh.CharacterCraftingAccountOutbox.AsNoTracking().SingleOrDefaultAsync(value => value.Id == intent.Id);
                if (observed == null || CraftingAccountIntent.Restore(observed).Json != intent.Json) throw;
                return !observed.Cancelled;
            }
            return !row.Cancelled;
        }

        internal async Task CompleteAuth(CraftingAccountIntent intent, bool cancelled)
        {
            await using var context = Auth(intent);
            AccountCraftingReceiptModel receipt = null;
            context.EnlistSaveWork(unit =>
            {
                if (receipt != null) unit.Entry(receipt).State = EntityState.Detached;
                unit.ReservePendingItemIds(intent.Account, 0);
                receipt = Receipt(unit, intent.Id).SingleOrDefault() ?? throw new InvalidOperationException("Crafting lost its Auth reservation.");
                if (CraftingAccountIntent.Restore(receipt).Json != intent.Json || receipt.Delivered == cancelled || receipt.Cancelled && !cancelled)
                    throw new InvalidOperationException("Crafting terminal receipt conflicts with its source proof.");
                unit.Attach(receipt); receipt.Cancelled = cancelled; receipt.Completed = true;
            }, _ => { }, () =>
            {
                var stored = Receipt(context, intent.Id).SingleOrDefault();
                return stored != null && CraftingAccountIntent.Restore(stored).Json == intent.Json && stored.Completed
                    && stored.Cancelled == cancelled && stored.Delivered != cancelled
                    && context.Entry(receipt).State is not (EntityState.Deleted or EntityState.Detached)
                    && CraftingAccountIntent.Restore(receipt).Json == intent.Json
                    && receipt.Completed && receipt.Cancelled == cancelled && receipt.Delivered != cancelled;
            }, intent.Account);
            try { await context.SaveChangesAsync(); }
            catch
            {
                await using var observed = Auth(intent);
                var stored = await observed.AccountCraftingReceipt.AsNoTracking().SingleOrDefaultAsync(row => row.Id == intent.Id);
                if (stored == null || CraftingAccountIntent.Restore(stored).Json != intent.Json || !stored.Completed
                    || stored.Cancelled != cancelled || stored.Delivered == cancelled) throw;
            }
        }

        internal async Task<CraftingAccountSnapshot> ReadSnapshot(CraftingAccountIntent intent, ulong epoch = 0)
        {
            await using var context = Auth(intent);
            await using var tx = await context.Database.BeginTransactionAsync(IsolationLevel.Serializable);
            context.ReservePendingItemIds(intent.Account, 0);
            if (epoch != 0 && !await context.AccountWorldSession.AnyAsync(row => row.AccountId == intent.Account && row.Epoch == epoch))
                throw new InvalidOperationException("Crafting publication lost its admitted account epoch.");
            string prefix = $"craft-award:{intent.Realm}:{intent.Id}:";
            var items = await context.AccountInventory.AsNoTracking()
                .Where(row => row.Id == intent.Account && row.CreationKey.StartsWith(prefix)).ToArrayAsync();
            LoyaltyState state = null;
            if (intent.LoyaltyFingerprint != null)
            {
                var ledger = await context.AccountLoyalty.AsNoTracking().SingleAsync(row => row.AccountId == intent.Account);
                var wallet = await context.AccountCurrency.AsNoTracking().SingleAsync(row => row.Id == intent.Account && row.CurrencyId == (byte)AccountCurrencyType.CosmicReward);
                state = new(ledger.EarnedPoints, ledger.ActiveTrackId, ledger.Revision, wallet.Amount,
                    await context.AccountLoyaltySlot.AsNoTracking().Where(row => row.AccountId == intent.Account).ToArrayAsync(),
                    context.LoadPendingItemRows(intent.Account));
            }
            await tx.CommitAsync(); return new(items, state);
        }

        private AuthContext Auth(CraftingAccountIntent intent)
        {
            var context = auth() ?? throw new InvalidOperationException("Missing crafting Auth context.");
            try
            {
                if (AccountCurrencyOperationRepository.DatabaseKey(context) != intent.AuthRoute
                    || context.Database.CurrentTransaction != null || context.ChangeTracker.Entries().Any())
                    throw new InvalidOperationException("Crafting delivery changed its owned Auth context.");
                return context;
            }
            catch { context.Dispose(); throw; }
        }

        private CharacterContext Character(CraftingAccountIntent intent)
        {
            var context = characters() ?? throw new InvalidOperationException("Missing crafting Character context.");
            try
            {
                if (context.Database.CurrentTransaction != null || context.ChangeTracker.Entries().Any()
                    || intent.Realm != realm || Item.GetRewardDatabaseKey(context) != intent.CharacterRoute)
                    throw new InvalidOperationException("Crafting changed its exclusively owned original Character context.");
                return context;
            }
            catch { context.Dispose(); throw; }
        }

        private static IQueryable<AccountCraftingReceiptModel> Receipt(AuthContext context, string id)
            => (context.Database.IsSqlite() ? context.AccountCraftingReceipt.Where(row => row.Id == id)
                : context.AccountCraftingReceipt.FromSqlInterpolated($"SELECT * FROM account_crafting_receipt WHERE id={id} FOR UPDATE")).AsNoTracking();

        private async Task ApplyAuth(CraftingAccountIntent intent)
        {
            await using var context = Auth(intent);
            List<AccountCurrencyOperationRepository> wallets = [];
            List<object> owned = [];
            List<object> cosmic = [];
            (object Model, Microsoft.EntityFrameworkCore.ChangeTracking.PropertyValues Values)[] frozen = [];
            context.EnlistSaveWork(unit =>
            {
                foreach (var wallet in wallets) wallet.Detach(); wallets.Clear();
                foreach (var model in owned) unit.Entry(model).State = EntityState.Detached; owned.Clear();
                foreach (var model in cosmic) unit.Entry(model).State = EntityState.Detached; cosmic.Clear();
                unit.ReservePendingItemIds(intent.Account, 0);
                var prior = Receipt(unit, intent.Id).SingleOrDefault();
                if (prior == null || CraftingAccountIntent.Restore(prior).Json != intent.Json || prior.Cancelled)
                    throw new InvalidOperationException("Crafting delivery lost its original Auth reservation.");
                if (prior.Delivered)
                {
                    frozen = [];
                    return;
                }
                unit.Attach(prior); prior.Delivered = true; owned.Add(prior);
                foreach (var (award, ordinal) in intent.Awards.Select((award, ordinal) => (award, ordinal)).Where(value => value.award.Type == LootItemType.AccountCurrency))
                {
                    string key = intent.Key(ordinal);
                    if (unit.AccountCurrencyOperation.AsNoTracking().Any(row => row.OperationKey == key)
                        || unit.AccountCosmicOperation.AsNoTracking().Any(row => row.AccountId == intent.Account && row.OperationKey == key))
                        throw new InvalidOperationException("A crafting currency receipt exists without its atomic bundle.");
                }
                foreach (var group in intent.Awards.Select((award, ordinal) => (award, ordinal))
                    .Where(value => value.award.Type == LootItemType.AccountCurrency).GroupBy(value => value.award.Id).OrderBy(group => group.Key))
                {
                    if (group.Key == (uint)AccountCurrencyType.CosmicReward && intent.LoyaltyFingerprint != null)
                    {
                        LoyaltyRepository.Stage(unit, intent.Account, loyalty, group.Select(value =>
                            new AccountCurrencyManager.CosmicMutation(intent.Key(value.ordinal), value.award.Amount, false, null)).ToArray(),
                            id => id == 0 || intent.LoyaltyPrerequisites.Contains(id), null, null, cosmic, out _);
                        continue;
                    }
                    var repository = new AccountCurrencyOperationRepository(unit, intent.Account, (AccountCurrencyType)group.Key,
                        group.Select(value => new AccountWalletOperation(intent.Key(value.ordinal), intent.Account,
                            (AccountCurrencyType)group.Key, value.award.Amount, false, intent.Json)).ToArray());
                    repository.Stage(); wallets.Add(repository);
                }
                ulong next = intent.FirstInventoryId;
                if (intent.InventoryCount != 0 && unit.Account.AsNoTracking().Single(row => row.Id == intent.Account).NextInventoryId < checked(next + intent.InventoryCount))
                    throw new InvalidOperationException("Crafting delivery lost its reserved inventory range.");
                foreach (var (award, ordinal) in intent.Awards.Select((award, ordinal) => (award, ordinal)).Where(value => value.award.Type == LootItemType.AccountItem))
                    for (uint index = 0; index < award.Amount; index++)
                    {
                        string key = intent.Key(ordinal, index);
                        if (unit.AccountInventoryReceipt.AsNoTracking().Any(value => value.AccountId == intent.Account && value.DeliveryKey == key))
                            throw new InvalidOperationException("A crafting account-item receipt exists without its atomic bundle.");
                        var item = new AccountInventoryModel { Id = intent.Account, InventoryId = next, AccountItemId = award.Id,
                            ClaimState = (byte)AccountItemClaimState.CanClaim, CreationKey = key,
                            CreateTime = new DateTime(DateTime.UtcNow.Ticks / TimeSpan.TicksPerSecond * TimeSpan.TicksPerSecond) };
                        var receipt = new AccountInventoryReceiptModel { AccountId = intent.Account, InventoryId = next, DeliveryKey = key };
                        next = checked(next + 1ul);
                        unit.AccountInventory.Add(item); unit.AccountInventoryReceipt.Add(receipt); owned.Add(item); owned.Add(receipt);
                    }
                // Pending Loyalty grants normally obtain this generated value
                // during flush. Give the frozen bundle its exact SQL precision
                // before validation, just as for its direct account items.
                foreach (var pending in cosmic.OfType<AccountPendingItemModel>().Where(row => row.CreateTime == default))
                    pending.CreateTime = new DateTime(DateTime.UtcNow.Ticks / TimeSpan.TicksPerSecond * TimeSpan.TicksPerSecond);
                frozen = owned.Concat(cosmic).Select(model => (model, unit.Entry(model).CurrentValues.Clone())).ToArray();
            }, _ => { }, () => Receipt(context, intent.Id).SingleOrDefault() is { } bundle
                && CraftingAccountIntent.Restore(bundle).Json == intent.Json && bundle.Delivered && !bundle.Cancelled
                && wallets.All(wallet => wallet.CanCommit()) && frozen.All(value =>
            {
                var entry = context.Entry(value.Model);
                if (entry.State is EntityState.Deleted or EntityState.Detached) return false;
                var stored = entry.GetDatabaseValues();
                return stored != null && value.Values.Properties.All(property => Equals(value.Values[property.Name], entry.CurrentValues[property.Name])
                    && Equals(value.Values[property.Name], stored[property.Name]));
            }), intent.Account);
            await context.SaveChangesAsync();
        }

        private async Task<bool> ObserveAuth(CraftingAccountIntent intent)
        {
            await using var context = Auth(intent);
            await using var tx = await context.Database.BeginTransactionAsync(IsolationLevel.Serializable);
            context.ReservePendingItemIds(intent.Account, 0);
            var bundle = await Receipt(context, intent.Id).SingleOrDefaultAsync();
            if (bundle == null || CraftingAccountIntent.Restore(bundle).Json != intent.Json || bundle.Cancelled)
                throw new InvalidOperationException("Crafting Auth receipt changed its original source.");
            if (!bundle.Delivered) return false;
            ulong nextInventoryId = intent.FirstInventoryId;
            foreach (var (award, ordinal) in intent.Awards.Select((award, ordinal) => (award, ordinal)))
            {
                string key = intent.Key(ordinal);
                if (award.Type == LootItemType.AccountCurrency)
                {
                    if (award.Id == (uint)AccountCurrencyType.CosmicReward && intent.LoyaltyFingerprint != null)
                    {
                        var operation = LoyaltyRepository.ReadOperation(context, intent.Account, key);
                        if (operation == null) throw new InvalidOperationException("Committed crafting bundle lost its Cosmic receipt.");
                        if (operation.Amount != award.Amount || operation.Subtract || !operation.Earned)
                            throw new InvalidOperationException("Crafting Cosmic receipt changed its earning identity.");
                    }
                    else
                    {
                        var operation = await context.AccountCurrencyOperation.AsNoTracking().SingleOrDefaultAsync(value => value.OperationKey == key);
                        if (operation == null) throw new InvalidOperationException("Committed crafting bundle lost its wallet receipt.");
                        AccountCurrencyOperationRepository.ValidateReceipt(operation, new(key, intent.Account, (AccountCurrencyType)award.Id, award.Amount, false, intent.Json));
                    }
                }
                else for (uint index = 0; index < award.Amount; index++)
                {
                    string itemKey = intent.Key(ordinal, index);
                    var receipt = await context.AccountInventoryReceipt.AsNoTracking().SingleOrDefaultAsync(value => value.AccountId == intent.Account && value.DeliveryKey == itemKey);
                    if (receipt == null) throw new InvalidOperationException("Committed crafting bundle lost its account-item receipt.");
                    if (receipt.InventoryId != nextInventoryId++) throw new InvalidOperationException("Crafting receipt lost its reserved item identity.");
                    var item = await context.AccountInventory.AsNoTracking().SingleOrDefaultAsync(value => value.Id == intent.Account && value.InventoryId == receipt.InventoryId);
                    // A consumed item keeps its creation receipt, never a regrant.
                    if (item != null && (item.CreationKey != itemKey || item.AccountItemId != award.Id))
                        throw new InvalidOperationException("Crafting account-item receipt changed its award identity.");
                }
            }
            await tx.CommitAsync(); return true;
        }
    }

    public static class CraftingAccountRecovery
    {
        public static async Task RecoverAtAdmissionAsync(AccountModel account, AuthDatabase auth, CharacterDatabase characters,
            ushort realm, GameTable.IGameTableManager tables)
        {
            if (account.Id == 0 || account.WorldSessionEpoch == 0 || realm == 0)
                throw new InvalidOperationException("Crafting recovery requires its admitted account/realm.");
            AccountCraftingReceiptModel[] rows;
            await using (var reader = auth.CreateContext())
            await using (var reservation = await reader.Database.BeginTransactionAsync(IsolationLevel.Serializable))
            {
                reader.ReservePendingItemIds(account.Id, 0);
                if (!await reader.AccountWorldSession.AnyAsync(row => row.AccountId == account.Id && row.Epoch == account.WorldSessionEpoch))
                    throw new InvalidOperationException("Crafting recovery lost its admitted epoch before source discovery.");
                rows = await reader.AccountCraftingReceipt.AsNoTracking()
                    .Where(row => row.AccountId == account.Id && row.RealmId == realm && !row.Completed).ToArrayAsync();
                foreach (var row in rows)
                    if (CraftingAccountIntent.Restore(row).AuthRoute != AccountCurrencyOperationRepository.DatabaseKey(reader))
                        throw new InvalidOperationException("Crafting reservation changed its original Auth route.");
                await reservation.CommitAsync();
            }
            LoyaltyCatalog catalog = null;
            foreach (var row in rows)
            {
                var intent = CraftingAccountIntent.Restore(row);
                var resolver = new CraftingAccountDelivery(characters.CreateContext, auth.CreateContext, realm);
                if (!await resolver.ResolveReservation(intent, row.Delivered))
                {
                    await resolver.CompleteAuth(intent, cancelled: true);
                    continue;
                }
                if (intent.LoyaltyFingerprint != null) catalog ??= new(tables);
                await new CraftingAccountDelivery(characters.CreateContext, auth.CreateContext, realm, catalog).DeliverAsync(intent);
            }
            await using var context = auth.CreateContext();
            await using var tx = await context.Database.BeginTransactionAsync(IsolationLevel.Serializable);
            context.ReservePendingItemIds(account.Id, 0);
            if (!await context.AccountWorldSession.AnyAsync(row => row.AccountId == account.Id && row.Epoch == account.WorldSessionEpoch))
                throw new InvalidOperationException("Crafting recovery lost its admitted epoch.");
            account.AccountCurrency = await context.AccountCurrency.AsNoTracking().Where(row => row.Id == account.Id).ToArrayAsync();
            account.AccountInventory = await context.AccountInventory.AsNoTracking().Where(row => row.Id == account.Id).ToArrayAsync();
            account.AccountPendingItem = context.LoadPendingItemRows(account.Id);
            account.Loyalty = await context.AccountLoyalty.AsNoTracking().SingleOrDefaultAsync(row => row.AccountId == account.Id);
            account.LoyaltySlots = await context.AccountLoyaltySlot.AsNoTracking().Where(row => row.AccountId == account.Id).ToArrayAsync();
            await tx.CommitAsync();
        }
    }
}
