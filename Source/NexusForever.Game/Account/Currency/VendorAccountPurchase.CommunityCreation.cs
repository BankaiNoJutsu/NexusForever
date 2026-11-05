using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using NexusForever.Game.Static.Account;

namespace NexusForever.Game.Account.Currency
{
    public sealed partial class VendorAccountPurchase
    {
        private const string CommunityDebitPrefix = "community-create-debit:";
        internal sealed record CommunityCreation(ulong Guild, ulong Residence, string Name);

        internal Intent CreateCommunity(string id, uint account, ulong character, ulong guild, ulong residence, string name,
            ulong fee, ulong epoch, string sealedPayload)
        {
            using var source = characters();
            using var payer = auth();
            RequireDurableStorage(payer.Database.GetDbConnection());
            var intent = new Intent(id, account, character, realm, CharacterRoute(source),
                AccountCurrencyOperationRepository.DatabaseKey(payer), 0, 0, PayloadHash(sealedPayload),
                [new(AccountCurrencyType.ServiceToken, fee)], epoch, Community: new(guild, residence, name));
            Validate(intent);
            if (!IsCommunityReceipt(sealedPayload, intent))
                throw new InvalidOperationException("Community payment requires its complete sealed token graph.");
            return intent;
        }

        internal Task AuthorizeCommunityAsync(Intent intent)
        {
            Validate(intent);
            if (!IsCommunityCreation(intent)) throw new InvalidOperationException("Community payment lost its distinct delivery identity.");
            // The caller holds the Character owner lock and has sealed the exact
            // receipt hash. Its transaction alone may decide delivery/refund.
            return Apply(intent, subtract: true);
        }

        private static bool IsCommunityCreation(Intent intent)
            => intent.Community is { Guild: > 0, Residence: > 0, Name.Length: > 0 } && intent.Epoch != 0
                && intent.Item == 0 && intent.Quantity == 0 && intent.CreddUsageLimit == null
                && intent.QuoteFingerprint?.Length == 64 && intent.Costs.Length == 1
                && intent.Costs[0].Currency == AccountCurrencyType.ServiceToken;

        private static string PayloadHash(string payload)
            => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(payload)));

        private static bool IsCommunityReceipt(string payload, Intent intent)
        {
            if (PayloadHash(payload) != intent.QuoteFingerprint) return false;
            using var json = JsonDocument.Parse(payload);
            var root = json.RootElement;
            var receipt = root.GetProperty("Receipt");
            return root.GetProperty("Kind").GetString() == "community-creation-tokens"
                && root.GetProperty("Database").GetString() == intent.CharacterRoute
                && root.GetProperty("Version").GetInt32() == 1
                && root.GetProperty("Character").GetUInt64() == intent.Character
                && receipt.GetProperty("Id").GetString() == intent.Id
                && receipt.GetProperty("Character").GetUInt64() == intent.Character
                && receipt.GetProperty("Guild").GetUInt64() == intent.Community.Guild
                && receipt.GetProperty("Residence").GetUInt64() == intent.Community.Residence
                && receipt.GetProperty("Name").GetString() == intent.Community.Name
                && receipt.GetProperty("Account").GetUInt32() == intent.Account
                && receipt.GetProperty("AuthRoute").GetString() == intent.AuthRoute
                && receipt.GetProperty("Epoch").GetUInt64() == intent.Epoch
                && receipt.GetProperty("TokenFee").GetUInt64() == intent.Costs[0].Amount
                && receipt.GetProperty("Primary").GetBoolean();
        }
    }
}
