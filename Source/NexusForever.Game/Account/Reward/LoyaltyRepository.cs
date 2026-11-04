using Microsoft.EntityFrameworkCore;
using NexusForever.Database.Auth;
using NexusForever.Database.Auth.Model;
using NexusForever.Game.Account.Currency;
using NexusForever.Game.Static.Account;

namespace NexusForever.Game.Account.Reward
{
    internal sealed record LoyaltyState(ulong EarnedPoints, uint ActiveTrackId, ulong Revision, ulong Wallet,
        IReadOnlyList<AccountLoyaltySlotModel> Slots, IReadOnlyList<AccountPendingItemModel> PendingRows);

    internal static class LoyaltyRepository
    {
        internal static AccountLoyaltyModel ReadLedger(AuthContext context, uint accountId)
            => (LockingRead(context)
                ? context.AccountLoyalty.FromSqlInterpolated($"SELECT * FROM account_loyalty WHERE accountId={accountId} FOR UPDATE")
                : context.AccountLoyalty.Where(row => row.AccountId == accountId)).AsNoTracking().SingleOrDefault();

        internal static AccountCosmicOperationModel ReadOperation(AuthContext context, uint accountId, string key)
            => (LockingRead(context)
                ? context.AccountCosmicOperation.FromSqlInterpolated($"SELECT * FROM account_cosmic_operation WHERE accountId={accountId} AND operationKey={key} FOR UPDATE")
                : context.AccountCosmicOperation.Where(row => row.AccountId == accountId && row.OperationKey == key)).AsNoTracking().SingleOrDefault();

        private static bool LockingRead(AuthContext context) => context.Database.CurrentTransaction != null
            && context.Database.ProviderName?.Contains("MySql", StringComparison.OrdinalIgnoreCase) == true;

        // The vendor wallet repository already holds the account lock and
        // owns the balance change. Both receipts commit with the journal revision.
        internal static void StageVendorPayment(AuthContext context, AccountCurrencyOperationModel receipt, Action<object> capture)
        {
            var ledger = ReadLedger(context, receipt.AccountId);
            if (ledger == null)
            {
                if (!receipt.Subtract) throw new InvalidOperationException("Vendor refund lost its Loyalty ledger.");
                ledger = new AccountLoyaltyModel { AccountId = receipt.AccountId, EarnedPoints = receipt.BeforeAmount,
                    BootstrapWalletAmount = receipt.BeforeAmount, ActiveTrackId = 9 };
                context.AccountLoyalty.Add(ledger);
            }
            else context.Attach(ledger);
            ledger.Revision = checked(ledger.Revision + 1);
            capture(ledger);

            var operation = new AccountCosmicOperationModel { AccountId = receipt.AccountId, OperationKey = receipt.OperationKey,
                Amount = receipt.Amount, Subtract = receipt.Subtract, Earned = false };
            context.AccountCosmicOperation.Add(operation);
            capture(operation);
        }

        internal static void ValidateVendorPayment(AuthContext context, AccountCurrencyOperationModel receipt)
        {
            var journal = ReadOperation(context, receipt.AccountId, receipt.OperationKey);
            if (journal == null || journal.OperationKey != receipt.OperationKey || journal.Amount != receipt.Amount
                || journal.Subtract != receipt.Subtract || journal.Earned || ReadLedger(context, receipt.AccountId) == null)
                throw new InvalidOperationException("Vendor payment lost its matching non-earning Loyalty operation.");
        }

        internal static LoyaltyState Stage(AuthContext context, uint accountId, LoyaltyCatalog catalog,
            IReadOnlyList<AccountCurrencyManager.CosmicMutation> mutations, Func<uint, bool> meets,
            uint? chooseTrack, (uint Row, uint Choice)? requestedClaim, List<object> staged, out bool accepted)
        {
            foreach (object entity in staged) context.Entry(entity).State = EntityState.Detached;
            staged.Clear();
            context.ReservePendingItemIds(accountId, 0);
            bool mysql = context.Database.ProviderName?.Contains("MySql", StringComparison.OrdinalIgnoreCase) == true;
            AccountLoyaltyModel ledger = ReadLedger(context, accountId);
            AccountCurrencyModel wallet = (mysql
                ? context.AccountCurrency.FromSqlInterpolated($"SELECT * FROM account_currency WHERE id = {accountId} AND currencyId = {(byte)AccountCurrencyType.CosmicReward} FOR UPDATE")
                : context.AccountCurrency.Where(row => row.Id == accountId && row.CurrencyId == (byte)AccountCurrencyType.CosmicReward)).AsNoTracking().SingleOrDefault();
            if (wallet == null)
            {
                wallet = new AccountCurrencyModel { Id = accountId, CurrencyId = (byte)AccountCurrencyType.CosmicReward };
                context.AccountCurrency.Add(wallet);
            }
            else context.Attach(wallet);
            staged.Add(wallet);

            bool initialized = ledger == null;
            if (initialized)
            {
                ledger = new AccountLoyaltyModel { AccountId = accountId, EarnedPoints = wallet.Amount, BootstrapWalletAmount = wallet.Amount, ActiveTrackId = 9 };
                context.AccountLoyalty.Add(ledger);
            }
            else context.Attach(ledger);
            staged.Add(ledger);

            bool appliedPositiveEarning = false;
            foreach (AccountCurrencyManager.CosmicMutation mutation in mutations)
            {
                AccountCosmicOperationModel prior = ReadOperation(context, accountId, mutation.Key);
                if (prior != null)
                {
                    if (prior.Amount != mutation.Amount || prior.Subtract != mutation.Subtract)
                        throw new InvalidOperationException("A Cosmic operation key was reused with different data.");
                    continue;
                }
                if (mutation.SourceInventoryId.HasValue)
                {
                    ulong sourceId = mutation.SourceInventoryId.Value;
                    AccountInventoryModel source = (mysql
                        ? context.AccountInventory.FromSqlInterpolated($"SELECT * FROM account_inventory WHERE id = {accountId} AND inventoryId = {sourceId} FOR UPDATE")
                        : context.AccountInventory.Where(row => row.Id == accountId && row.InventoryId == sourceId)).AsNoTracking().SingleOrDefault();
                    bool matches = source != null && (source.CreationKey ?? $"legacy:{source.InventoryId}") == mutation.SourceCreationKey;
                    if (!matches)
                    {
                        bool created = (mysql
                            ? context.AccountInventoryReceipt.FromSqlInterpolated($"SELECT * FROM account_inventory_receipt WHERE accountId = {accountId} AND deliveryKey = {mutation.SourceCreationKey} FOR UPDATE")
                            : context.AccountInventoryReceipt.Where(row => row.AccountId == accountId && row.DeliveryKey == mutation.SourceCreationKey)).AsNoTracking().Any();
                        if (!mutation.SourceMayBeUnpersisted || created) continue;
                    }
                }
                if (mutation.Subtract)
                {
                    if (wallet.Amount < mutation.Amount)
                        throw new InvalidOperationException("The durable Cosmic wallet cannot afford this operation.");
                    wallet.Amount -= mutation.Amount;
                }
                else
                {
                    wallet.Amount = checked(wallet.Amount + mutation.Amount);
                    ledger.EarnedPoints = checked(ledger.EarnedPoints + mutation.Amount);
                    appliedPositiveEarning = true;
                }
                var operation = new AccountCosmicOperationModel { AccountId = accountId, OperationKey = mutation.Key, Amount = mutation.Amount, Subtract = mutation.Subtract, Earned = !mutation.Subtract };
                context.AccountCosmicOperation.Add(operation);
                staged.Add(operation);
            }

            List<AccountLoyaltySlotModel> slots = (mysql
                ? context.AccountLoyaltySlot.FromSqlInterpolated($"SELECT * FROM account_loyalty_slot WHERE accountId = {accountId} FOR UPDATE")
                : context.AccountLoyaltySlot.Where(row => row.AccountId == accountId)).AsNoTracking().ToList();
            if (slots.Any(row => catalog.GetTrack(row.TrackId)?.Slots.Any(slot => slot.Position == row.Position && slot.Entry.Id == row.RewardRowId) != true))
                throw new InvalidOperationException("Saved Loyalty slots do not match the authoritative catalog.");
            foreach (LoyaltyTrackDefinition track in catalog.Tracks)
                foreach (LoyaltyRewardSlot slot in track.Slots)
                {
                    AccountLoyaltySlotModel saved = slots.SingleOrDefault(row => row.TrackId == track.Id && row.Position == slot.Position);
                    if (saved == null)
                    {
                        saved = new AccountLoyaltySlotModel { AccountId = accountId, TrackId = track.Id, Position = slot.Position, RewardRowId = slot.Entry.Id };
                        slots.Add(saved);
                        context.AccountLoyaltySlot.Add(saved);
                    }
                    else context.Attach(saved);
                    staged.Add(saved);
                }

            accepted = true;
            bool Unlocked(LoyaltyTrackDefinition track) => meets(track.Entry.PrerequisiteId) &&
                (track.ParentId == 0 || ledger.EarnedPoints >= track.Offset);
            if (chooseTrack.HasValue)
            {
                LoyaltyTrackDefinition selected = catalog.GetTrack(chooseTrack.Value);
                accepted = selected != null && Unlocked(selected);
                if (accepted) ledger.ActiveTrackId = selected.Id;
            }

            void Claim(LoyaltyTrackDefinition track, LoyaltyRewardSlot slot, AccountLoyaltySlotModel saved)
            {
                if (saved.Claimed) return;
                string key = $"loyalty:{accountId}:{track.Id}:{slot.Position}";
                var pending = new AccountPendingItemModel
                {
                    Id = accountId, GroupName = key, AccountItemId = slot.AccountItemId,
                    ForceSoulbind = slot.ForceSoulbind
                };
                context.StagePendingItemAppend(key, pending);
                if (context.Entry(pending).State == EntityState.Added)
                {
                    staged.Add(pending);
                    staged.Add(context.AccountPendingItemDelivery.Local.Single(row => row.AccountId == accountId && row.DeliveryKey == key));
                }
                saved.Claimed = true;
                saved.Choice = 0;
                saved.DeliveryKey = key;
            }
            if (requestedClaim.HasValue)
            {
                var request = requestedClaim.Value;
                LoyaltyRewardReference reference = catalog.GetReward(request.Row);
                accepted = reference != null && request.Choice == 0 && Unlocked(reference.Track)
                    && meets(reference.Slot.Entry.PrerequisiteId)
                    && Points(ledger.EarnedPoints, reference.Track) >= reference.Slot.Cost;
                if (accepted)
                    Claim(reference.Track, reference.Slot, slots.Single(row => row.TrackId == reference.Track.Id && row.Position == reference.Slot.Position));
            }
            foreach (LoyaltyTrackDefinition track in catalog.Tracks)
                if (Unlocked(track))
                    foreach (LoyaltyRewardSlot slot in track.Slots)
                        if (slot.AutoClaim && Points(ledger.EarnedPoints, track) >= slot.Cost && meets(slot.Entry.PrerequisiteId))
                            Claim(track, slot, slots.Single(row => row.TrackId == track.Id && row.Position == slot.Position));

            // Positive earnings advance a completed active tier; an explicit choice
            // in this unit remains authoritative. No reset/repeat policy is inferred.
            if (!chooseTrack.HasValue && (initialized || appliedPositiveEarning))
            {
                LoyaltyTrackDefinition active = catalog.GetTrack(ledger.ActiveTrackId);
                if (active != null && Points(ledger.EarnedPoints, active) == active.MaximumPoints)
                    ledger.ActiveTrackId = catalog.Tracks.FirstOrDefault(track => Unlocked(track) && Points(ledger.EarnedPoints, track) < track.MaximumPoints)?.Id ?? catalog.Tracks[^1].Id;
            }
            ledger.Revision = checked(ledger.Revision + 1);
            return new LoyaltyState(ledger.EarnedPoints, ledger.ActiveTrackId, ledger.Revision, wallet.Amount,
                slots.Select(row => new AccountLoyaltySlotModel { AccountId = row.AccountId, TrackId = row.TrackId, Position = row.Position, RewardRowId = row.RewardRowId, Claimed = row.Claimed, Choice = row.Choice, DeliveryKey = row.DeliveryKey }).ToArray(),
                context.LoadPendingItemRows(accountId).Concat(staged.OfType<AccountPendingItemModel>()).GroupBy(row => row.PendingItemId).Select(group => group.Last()).ToArray());
        }

        internal static uint Points(ulong earned, LoyaltyTrackDefinition track) => earned <= track.Offset ? 0 : (uint)Math.Min(earned - track.Offset, track.MaximumPoints);
    }
}
