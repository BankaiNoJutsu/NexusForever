using Microsoft.EntityFrameworkCore;
using NexusForever.Database.Friendship.Model;
using System.Runtime.CompilerServices;

namespace NexusForever.Database.Friendship.Repository
{
    public class AccountRepository
    {
        private readonly FriendshipContext _context;

        public AccountRepository(
            FriendshipContext context)
        {
            _context = context;
        }

        public void AddAccount(AccountModel account)
        {
            _context.Account.Add(account);
        }

        /// <summary>
        /// Atomically bootstrap an API account without resetting an existing account's
        /// preferences, presence, nickname or active character.
        /// </summary>
        public async Task<AccountModel> EnsureAccountAsync(AccountModel account)
        {
            bool sqlite = _context.Database.IsSqlite();
            string provider = _context.Database.ProviderName;
            string suffix = sqlite ? " ON CONFLICT (`accountId`) DO NOTHING"
                : provider is "Microting.EntityFrameworkCore.MySql" or "Pomelo.EntityFrameworkCore.MySql"
                    ? " ON DUPLICATE KEY UPDATE `accountId` = `accountId`"
                    : throw new NotSupportedException($"Unsupported friendship cache provider {provider}.");
            const string insert = "INSERT INTO `account` (`accountId`, `email`, `nickname`, `status`, `presence`, `blockAccountFriendRequests`, `invitePrivilegesSuspended`, `activeCharacterId`, `activeRealmId`, `lastOnline`) VALUES ({0}, {1}, {2}, {3}, {4}, {5}, {6}, {7}, {8}, {9})";
            await _context.Database.ExecuteSqlInterpolatedAsync(FormattableStringFactory.Create(insert + suffix,
                account.AccountId, account.Email, account.Nickname, account.Status, (byte)account.Presence,
                account.BlockAccountFriendRequests, account.InvitePrivilegesSuspended,
                account.ActiveCharacterId, account.ActiveRealmId, account.LastOnline));

            AccountModel winner;
            if (!sqlite && _context.Database.CurrentTransaction != null)
                winner = (await _context.Account.FromSqlInterpolated($"SELECT * FROM `account` WHERE `accountId` = {account.AccountId} FOR UPDATE").ToListAsync()).SingleOrDefault();
            else
                winner = await _context.Account.SingleOrDefaultAsync(a => a.AccountId == account.AccountId);

            // MySQL also reports unique email/nickname collisions through its upsert
            // branch. Never return a different account or hide that conflict.
            if (winner == null)
                throw new DbUpdateException($"API account {account.AccountId} conflicts with an existing account identity.");

            await _context.Entry(winner).Collection(a => a.Friends).LoadAsync();
            await _context.Entry(winner).Collection(a => a.FriendsInverse).LoadAsync();
            await _context.Entry(winner).Collection(a => a.FriendInvites).LoadAsync();
            await _context.Entry(winner).Collection(a => a.FriendInvitesPending).LoadAsync();
            return winner;
        }

        public async Task<AccountModel> GetAccountAsync(uint id)
        {
            return await IncludeAccount(_context.Account)
                .SingleOrDefaultAsync(a => a.AccountId == id);
        }

        public async Task<AccountModel> GetAccountAsync(string email)
        {
            return await _context.Account.FirstOrDefaultAsync(a => a.Email == email);
        }

        public async Task<AccountModel> GetAccountByNicknameAsync(string nickname)
        {
            return await _context.Account.FirstOrDefaultAsync(a => a.Nickname == nickname);
        }

        private static IQueryable<AccountModel> IncludeAccount(IQueryable<AccountModel> query)
        {
            return query
                .Include(a => a.FriendInvites)
                .Include(a => a.FriendInvitesPending)
                .Include(a => a.Friends)
                .Include(a => a.FriendsInverse);
        }
    }
}
