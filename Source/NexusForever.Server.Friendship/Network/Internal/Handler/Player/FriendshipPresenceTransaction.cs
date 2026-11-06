using System.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using NexusForever.Database.Friendship;
using NexusForever.Database.Friendship.Model;
using NexusForever.Server.Friendship.Game.Character;

namespace NexusForever.Server.Friendship.Network.Internal.Handler.Player
{
    // Database ownership orders overlapping writers, not older broker events
    // delivered after a newer session. Those still require a generation fence.
    internal static class FriendshipPresenceTransaction
    {
        internal static async Task ExecuteAsync(FriendshipContext context, CharacterManager characters, Func<Task<bool>> operation)
            => await ExecuteAsync(context, characters.ResetPresenceCaches, operation);

        internal static async Task ExecuteAsync(FriendshipContext context, Game.Account.AccountManager accounts, Func<Task<bool>> operation)
            => await ExecuteAsync(context, accounts.ResetPresenceCommandCaches, operation);

        private static async Task ExecuteAsync(FriendshipContext context, Action resetCaches, Func<Task<bool>> operation)
        {
            context.ChangeTracker.DetectChanges();
            if (context.Database.CurrentTransaction != null || context.ChangeTracker.HasChanges())
                throw new InvalidOperationException("Presence handlers require their own unit without pending writes.");
            bool sqlite = context.Database.IsSqlite();
            if (!sqlite && context.Database.ProviderName is not ("Microting.EntityFrameworkCore.MySql" or "Pomelo.EntityFrameworkCore.MySql"))
                throw new NotSupportedException("Unsupported Friendship presence provider.");
            int attempts = 0;
            await context.Database.CreateExecutionStrategy().ExecuteAsync(async () =>
            {
                if (++attempts > 1)
                {
                    // A rolled-back tracker and scoped wrappers cannot become
                    // the baseline of a whole-unit provider retry.
                    context.ChangeTracker.Clear();
                    resetCaches();
                }
                await using var transaction = await context.Database.BeginTransactionAsync(sqlite
                    ? IsolationLevel.Serializable : IsolationLevel.ReadCommitted);
                if (await operation()) await transaction.CommitAsync();
            });
        }

        internal static async Task LockAndRefreshAccountAsync(FriendshipContext context, AccountModel account)
        {
            if (context.Database.CurrentTransaction == null || context.Entry(account).State != EntityState.Unchanged)
                throw new InvalidOperationException("Presence account must be unchanged within its transaction.");
            if (!context.Database.IsSqlite())
            {
                var rows = await context.Account.FromSqlInterpolated($"SELECT * FROM `account` WHERE `accountId` = {account.AccountId} FOR UPDATE")
                    .AsNoTracking().ToListAsync();
                if (rows.Count != 1) throw new InvalidOperationException("Presence account no longer exists.");
            }
            // SQLite's serializable writer transaction owns this unit. EF
            // identity resolution does not refresh a previously tracked model;
            // reload under ownership before assigning a stale original value.
            await context.Entry(account).ReloadAsync();
            if (context.Entry(account).State != EntityState.Unchanged)
                throw new InvalidOperationException("Presence account lost its loaded ownership.");
        }

        internal static async Task RefreshCharacterAsync(FriendshipContext context, CharacterModel character, uint accountId)
        {
            if (context.Database.CurrentTransaction == null || context.Entry(character).State != EntityState.Unchanged)
                throw new InvalidOperationException("Presence character must be unchanged within its transaction.");
            await context.Entry(character).ReloadAsync();
            if (context.Entry(character).State != EntityState.Unchanged || character.AccountId != accountId)
                throw new InvalidOperationException("Presence character changed its account ownership.");
        }
    }
}
