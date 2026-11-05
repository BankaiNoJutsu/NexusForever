using Microsoft.EntityFrameworkCore;
using NexusForever.Database.Auth;
using NexusForever.Database.Character;
using NexusForever.Game.Account.Currency;
using NexusForever.Game.Account.Reward;
using NexusForever.Game.Entity;
using NexusForever.GameTable;
using NLog;

namespace NexusForever.Game.Crafting
{
    // Only committed Character sources authorize delivery. Merely discovering
    // an unpaid Auth reservation never authorizes a cancellation fence.
    public sealed class CraftingAccountOutbox(Func<CharacterContext> characters, Func<AuthContext> auth,
        ushort realm, IGameTableManager tables)
    {
        private static readonly ILogger log = LogManager.GetCurrentClassLogger();
        private readonly SemaphoreSlim gate = new(1, 1);
        private string cursor, authRoute, characterRoute;
        private LoyaltyCatalog loyalty;

        public async Task<int> DrainAsync(int limit = 32, CancellationToken cancellationToken = default)
        {
            if (realm == 0) throw new InvalidOperationException("Crafting recovery requires its own realm.");
            if (limit is < 1 or > 256) throw new ArgumentOutOfRangeException(nameof(limit));
            if (!await gate.WaitAsync(0, cancellationToken)) return 0;
            try
            {
                await using var reader = Auth();
                var query = reader.AccountCraftingReceipt.AsNoTracking().Where(row => row.RealmId == realm && !row.Completed);
                if (cursor != null) query = query.Where(row => string.Compare(row.Id, cursor) > 0);
                var rows = await query.OrderBy(row => row.Id).Take(limit).ToArrayAsync(cancellationToken);
                if (rows.Length == 0) { cursor = null; return 0; }
                int completed = 0;
                foreach (var row in rows)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    // Advance even when a malformed or unavailable source fails,
                    // so it cannot starve the next paid award in a bounded pass.
                    cursor = row.Id;
                    try
                    {
                        var intent = CraftingAccountIntent.Restore(row);
                        if (intent.AuthRoute != authRoute)
                            throw new InvalidOperationException("Crafting outbox changed its original Auth route.");
                        await using var source = Character();
                        if (intent.CharacterRoute != characterRoute)
                            throw new InvalidOperationException("Crafting outbox changed its original Character route.");
                        var paid = await source.CharacterCraftingAccountOutbox.AsNoTracking()
                            .SingleOrDefaultAsync(value => value.Id == intent.Id, cancellationToken);
                        if (paid == null)
                        {
                            if (row.Delivered) throw new InvalidOperationException("Delivered crafting award lost its committed source.");
                            continue;
                        }
                        if (CraftingAccountIntent.Restore(paid).Json != intent.Json)
                            throw new InvalidOperationException("Crafting outbox changed its committed source identity.");
                        var delivery = new CraftingAccountDelivery(Character, Auth, realm);
                        if (paid.Cancelled) await delivery.CompleteAuth(intent, cancelled: true);
                        else
                        {
                            if (intent.LoyaltyFingerprint != null) loyalty ??= new(tables);
                            await new CraftingAccountDelivery(Character, Auth, realm, loyalty).DeliverAsync(intent);
                        }
                        completed++;
                    }
                    catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
                    catch (Exception error)
                    {
                        log.Error(error, "Crafting outbox {0} remains pending in realm {1}.", row.Id, realm);
                    }
                }
                return completed;
            }
            finally { gate.Release(); }
        }

        private AuthContext Auth()
        {
            var context = auth() ?? throw new InvalidOperationException("Missing crafting outbox Auth context.");
            try
            {
                string route = AccountCurrencyOperationRepository.DatabaseKey(context);
                authRoute ??= route;
                if (route != authRoute || context.Database.CurrentTransaction != null || context.ChangeTracker.Entries().Any())
                    throw new InvalidOperationException("Crafting outbox changed its owned Auth context or route.");
                return context;
            }
            catch { context.Dispose(); throw; }
        }

        private CharacterContext Character()
        {
            var context = characters() ?? throw new InvalidOperationException("Missing crafting outbox Character context.");
            try
            {
                string route = Item.GetRewardDatabaseKey(context);
                characterRoute ??= route;
                if (route != characterRoute || context.Database.CurrentTransaction != null || context.ChangeTracker.Entries().Any())
                    throw new InvalidOperationException("Crafting outbox changed its owned Character context or route.");
                return context;
            }
            catch { context.Dispose(); throw; }
        }
    }
}
