using Microsoft.EntityFrameworkCore;
using NexusForever.Database.Auth;
using NexusForever.Database.Auth.Model;
using NexusForever.Game.Static.Account;
using NexusForever.Game.Static.Entity;

namespace NexusForever.Game.Account.Currency
{
    public sealed partial class VendorAccountPurchase
    {
        private const string CreddDebitPrefix = "credd-redeem-debit:";
        internal const ulong CreddToCreditsRatio = 1000ul;

        internal Intent CreateCreddRedemption(uint account, ulong character, ulong amount, uint usageLimit, ulong epoch)
        {
            if (epoch == 0)
                throw new InvalidOperationException("CREDD redemption requires an admitted session epoch.");
            using var source = characters();
            using var wallet = auth();
            RequireDurableStorage(wallet.Database.GetDbConnection());
            var intent = new Intent(Guid.NewGuid().ToString("N"), account, character, realm,
                CharacterRoute(source), AccountCurrencyOperationRepository.DatabaseKey(wallet),
                0, 0, null, [new(AccountCurrencyType.Credd, amount)], epoch, usageLimit);
            Validate(intent);
            return intent;
        }

        private static bool IsCreddRedemption(Intent intent)
            => intent.CreddUsageLimit > 0 && intent.Epoch != 0
                && intent.Item == 0 && intent.Quantity == 0 && intent.QuoteFingerprint == null
                && intent.Costs.Length == 1 && intent.Costs[0].Currency == AccountCurrencyType.Credd
                && intent.Costs[0].Amount <= ulong.MaxValue / CreddToCreditsRatio;

        // The wallet repository has already locked the account for this Auth
        // unit. Only a newly staged wallet receipt may change the usage count.
        private static CreddUsageWrite StageCreddUsage(AuthContext context, uint account, uint limit, bool subtract)
        {
            var row = ReadCreddUsage(context, account);
            uint before = row?.Amount ?? 0u;
            if (subtract && before >= limit || !subtract && before == 0)
                throw new InvalidOperationException("CREDD usage cannot apply the admitted change.");
            uint after = subtract ? checked(before + 1u) : before - 1u;
            if (row == null)
            {
                row = new AccountEntitlementModel { Id = account, EntitlementId = (byte)EntitlementType.CREDDUsage };
                context.AccountEntitlement.Add(row);
            }
            else
                context.AccountEntitlement.Attach(row);
            row.Amount = after;
            return new(row, after);
        }

        private static AccountEntitlementModel ReadCreddUsage(AuthContext context, uint account)
        {
            byte type = (byte)EntitlementType.CREDDUsage;
            var rows = context.Database.IsSqlite()
                ? context.AccountEntitlement.Where(row => row.Id == account && row.EntitlementId == type)
                : context.AccountEntitlement.FromSqlInterpolated(
                    $"SELECT * FROM account_entitlement WHERE id={account} AND entitlementId={type} FOR UPDATE");
            return rows.AsNoTracking().AsEnumerable().SingleOrDefault();
        }

        private sealed record CreddUsageWrite(AccountEntitlementModel Row, uint Amount)
        {
            internal bool CanCommit(AuthContext context, uint account)
                => context.Entry(Row).State is not (EntityState.Detached or EntityState.Deleted)
                    && Row.Id == account && Row.EntitlementId == (byte)EntitlementType.CREDDUsage
                    && Row.Amount == Amount && ReadCreddUsage(context, account)?.Amount == Amount;
        }
    }
}
