using System.Data;
using Microsoft.EntityFrameworkCore;
using NexusForever.Database.Auth;
using NexusForever.Database.Character;
using NexusForever.Game.Account.Currency;
using NexusForever.Game.Entity;

namespace NexusForever.Game.Quest.Reward
{
    internal enum ContractAccountDeliveryOutcome { Pending, Delivered, Conflict, Uncertain }
    internal sealed record ContractAccountDeliveryResult(ContractAccountDeliveryOutcome Outcome, string ClaimId, Exception Error = null);

    // Each database owns its own transaction. A committed source intent and the
    // global Auth operation key bridge crashes/acknowledgement loss; no actor or
    // network callback runs here and no distributed transaction is assumed.
    internal sealed class ContractAccountDelivery(Func<CharacterContext> characters, Func<AuthContext> auth,
        ContractRewardAwardCatalog catalog, ushort realmId)
    {
        private string characterDatabase, authDatabase;
        internal Task<ContractAccountDeliveryResult> DeliverAsync(string claimId)
            => Task.Run(() => RunAsync(claimId));

        private CharacterContext Character()
        {
            var context = characters() ?? throw new InvalidOperationException("Missing source context.");
            try
            {
                string key = Item.GetRewardDatabaseKey(context);
                string expected = Interlocked.CompareExchange(ref characterDatabase, key, null) ?? key;
                if (key != expected || context.Database.CurrentTransaction != null || context.ChangeTracker.Entries().Any())
                    throw new InvalidOperationException("Contract outbox changed its source database or context owner.");
                return context;
            }
            catch { context.Dispose(); throw; }
        }
        private AuthContext Auth()
        {
            var context = auth() ?? throw new InvalidOperationException("Missing Auth context.");
            try
            {
                string key = AccountCurrencyOperationRepository.DatabaseKey(context);
                string expected = Interlocked.CompareExchange(ref authDatabase, key, null) ?? key;
                if (key != expected || context.Database.CurrentTransaction != null || context.ChangeTracker.Entries().Any())
                    throw new InvalidOperationException("Contract delivery changed its Auth database or context owner.");
                return context;
            }
            catch { context.Dispose(); throw; }
        }

        private async Task<(ContractAccountIntent Intent, bool Delivered)> ReadSource(string claim)
        {
            await using var strategy = Character();
            return await strategy.Database.CreateExecutionStrategy().ExecuteAsync(async () =>
            {
                await using var context = Character(); await using var tx = await context.Database.BeginTransactionAsync(IsolationLevel.Serializable);
                var row = await context.CharacterContractAccountOutbox.AsNoTracking().SingleOrDefaultAsync(value => value.ClaimId == claim);
                if (row == null) { await tx.CommitAsync(); return (null, false); }
                var intent = ContractAccountIntent.Restore(catalog, row.PayloadJson);
                var quote = ContractRewardClaimQuote.Restore(catalog, intent.QuoteJson);
                if (realmId == 0 || intent.RealmId != realmId || row.ClaimId != intent.ClaimId || row.CharacterId != intent.CharacterId
                    || row.AccountId != intent.AccountId || row.RealmId != intent.RealmId || row.TrackId != quote.Award.TrackId
                    || row.CycleId != quote.CycleId || row.Ordinal != quote.Award.Ordinal)
                    throw new InvalidOperationException("Contract outbox changed its original source/account owner.");
                await tx.CommitAsync(); return (intent, row.Delivered);
            });
        }

        private async Task<bool> ObserveAuth(AccountWalletOperation operation)
        {
            await using var strategy = Auth();
            return await strategy.Database.CreateExecutionStrategy().ExecuteAsync(async () =>
            {
                await using var context = Auth(); await using var tx = await context.Database.BeginTransactionAsync(IsolationLevel.Serializable);
                var row = await context.AccountCurrencyOperation.AsNoTracking().SingleOrDefaultAsync(value => value.OperationKey == operation.Key);
                if (row != null) AccountCurrencyOperationRepository.ValidateReceipt(row, operation);
                await tx.CommitAsync(); return row != null;
            });
        }

        private async Task ApplyAuth(AccountWalletOperation operation)
        {
            await using var context = Auth(); AccountCurrencyOperationRepository repository = null;
            context.EnlistSaveWork(unit =>
            {
                repository?.Detach(); repository = new(unit, operation.AccountId, operation.Currency, [operation]); repository.Stage();
            }, _ => { }, () => repository?.CanCommit() == true);
            await context.SaveChangesAsync();
        }

        private async Task Acknowledge(ContractAccountIntent intent)
        {
            await using var strategy = Character();
            await strategy.Database.CreateExecutionStrategy().ExecuteAsync(async () =>
            {
                await using var context = Character(); await using var tx = await context.Database.BeginTransactionAsync(IsolationLevel.Serializable);
                bool mysql = context.Database.ProviderName?.Contains("MySql", StringComparison.OrdinalIgnoreCase) == true;
                // Use the claim repository's Character-before-outbox lock order.
                var owner = await (mysql ? context.Character.FromSqlInterpolated($"SELECT * FROM `character` WHERE id={intent.CharacterId} FOR UPDATE")
                    : context.Character.Where(row => row.Id == intent.CharacterId)).AsNoTracking().SingleOrDefaultAsync();
                var row = await (mysql ? context.CharacterContractAccountOutbox.FromSqlInterpolated($"SELECT * FROM character_contract_account_outbox WHERE claimId={intent.ClaimId} FOR UPDATE")
                    : context.CharacterContractAccountOutbox.Where(value => value.ClaimId == intent.ClaimId)).AsNoTracking().SingleOrDefaultAsync();
                var quote = ContractRewardClaimQuote.Restore(catalog, intent.QuoteJson);
                if (row == null || row.PayloadJson != intent.Json || row.CharacterId != intent.CharacterId || row.AccountId != intent.AccountId || row.RealmId != realmId
                    || row.TrackId != quote.Award.TrackId || row.CycleId != quote.CycleId || row.Ordinal != quote.Award.Ordinal)
                    throw new InvalidOperationException("Retained Contract intent changed before acknowledgement.");
                if (row.Delivered) { await tx.CommitAsync(); return; }
                context.Attach(row); row.Delivered = true; var owned = new List<object> { row };
                if (owner != null && await context.CharacterContractRewardClaim.AsNoTracking().AnyAsync(value => value.Id == intent.CharacterId && value.ClaimId == intent.ClaimId))
                {
                    var track = await context.CharacterContractRewardTrack.AsNoTracking().SingleOrDefaultAsync(value => value.Id == intent.CharacterId && value.TrackId == row.TrackId);
                    if (track?.CycleId == row.CycleId)
                    {
                        context.Attach(track); track.Revision = checked(track.Revision + 1); owned.Add(track);
                    }
                }
                var frozen = owned.Select(value => (Entity: value, Values: context.Entry(value).CurrentValues.Clone())).ToArray();
                context.OnSaveValidation(phase =>
                {
                    foreach (var value in frozen)
                    {
                        var entry = context.Entry(value.Entity);
                        if (entry.State is EntityState.Deleted or EntityState.Detached || value.Values.Properties.Any(property => !Equals(value.Values[property.Name], entry.CurrentValues[property.Name])))
                            throw new InvalidOperationException("Contract acknowledgement target changed.");
                        if (phase == CharacterSaveValidationPhase.BeforeExplicitCommit)
                        {
                            var stored = entry.GetDatabaseValues();
                            if (stored == null || value.Values.Properties.Any(property => !Equals(value.Values[property.Name], stored[property.Name])))
                                throw new InvalidOperationException("Contract acknowledgement did not reach SQL.");
                        }
                    }
                });
                await context.SaveChangesAsync(); await tx.CommitAsync();
            });
        }

        private async Task<ContractAccountDeliveryResult> RunAsync(string claim)
        {
            try
            {
                if (!Guid.TryParseExact(claim, "N", out var id) || id == Guid.Empty) throw new InvalidOperationException("Invalid Contract delivery identity.");
                var source = await ReadSource(claim);
                if (source.Intent == null) return new(ContractAccountDeliveryOutcome.Pending, claim);
                var award = ContractRewardClaimQuote.Restore(catalog, source.Intent.QuoteJson).Award.AccountCurrency;
                var operation = new AccountWalletOperation(source.Intent.OperationKey, source.Intent.AccountId, award.Type, award.Amount, false, source.Intent.Json);
                bool applied = await ObserveAuth(operation); Exception failure = null;
                if (!applied)
                {
                    if (source.Delivered) throw new InvalidOperationException("Acknowledged Contract compensation lost its Auth receipt.");
                    try { await ApplyAuth(operation); } catch (Exception error) { failure = error; }
                    applied = await ObserveAuth(operation);
                    if (!applied) return new(ContractAccountDeliveryOutcome.Pending, claim, failure);
                }
                try { await Acknowledge(source.Intent); }
                catch (Exception error)
                {
                    var observed = await ReadSource(claim);
                    if (observed.Intent?.Json != source.Intent.Json) throw new InvalidOperationException("Contract source changed during acknowledgement recovery.");
                    if (!observed.Delivered) return new(ContractAccountDeliveryOutcome.Pending, claim, error);
                }
                return new(ContractAccountDeliveryOutcome.Delivered, claim, failure);
            }
            catch (Exception error)
            {
                return new(error is InvalidOperationException or System.Text.Json.JsonException or OverflowException
                    ? ContractAccountDeliveryOutcome.Conflict : ContractAccountDeliveryOutcome.Uncertain, claim, error);
            }
        }
    }
}
