using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using NexusForever.Game.Static.Account;

namespace NexusForever.Game.Account.Currency
{
    public sealed partial class VendorAccountPurchase
    {
        private const string AppearanceDebitPrefix = "appearance-change-debit:";
        internal sealed record AppearanceChange(byte Race, byte Sex, string TargetHash);

        internal Intent CreateAppearance(string id, uint account, ulong character, byte race, byte sex,
            string targetHash, ulong fee, ulong epoch, string sealedPayload)
        {
            using var source = characters(); using var payer = auth();
            RequireDurableStorage(payer.Database.GetDbConnection());
            var intent = new Intent(id, account, character, realm, CharacterRoute(source),
                AccountCurrencyOperationRepository.DatabaseKey(payer), 0, 0, PayloadHash(sealedPayload),
                [new(AccountCurrencyType.ServiceToken, fee)], epoch, Appearance: new(race, sex, targetHash));
            Validate(intent);
            if (!IsAppearanceReceipt(sealedPayload, intent))
                throw new InvalidOperationException("Appearance payment requires its exact sealed Character graph.");
            return intent;
        }
        internal Task AuthorizeAppearanceAsync(Intent intent)
        {
            Validate(intent);
            if (!IsAppearanceChange(intent)) throw new InvalidOperationException("Appearance payment changed its delivery identity.");
            return Apply(intent, subtract: true);
        }
        private static bool IsAppearanceChange(Intent intent) => intent.Appearance is { TargetHash.Length: 64 }
            && intent.Community == null && intent.CreddUsageLimit == null && intent.Epoch != 0
            && intent.Item == 0 && intent.Quantity == 0 && intent.QuoteFingerprint?.Length == 64
            && intent.Costs.Length == 1 && intent.Costs[0].Currency == AccountCurrencyType.ServiceToken;
        private static bool IsAppearanceReceipt(string payload, Intent intent)
        {
            if (PayloadHash(payload) != intent.QuoteFingerprint) return false;
            using var json = JsonDocument.Parse(payload); var root = json.RootElement; var receipt = root.GetProperty("Receipt");
            return root.GetProperty("Kind").GetString() == "normal-appearance-change"
                && root.GetProperty("Version").GetInt32() == 1
                && root.GetProperty("Database").GetString() == intent.CharacterRoute
                && root.GetProperty("Character").GetUInt64() == intent.Character
                && receipt.GetProperty("Id").GetString() == intent.Id
                && receipt.GetProperty("Character").GetUInt64() == intent.Character
                && receipt.GetProperty("Account").GetUInt32() == intent.Account
                && receipt.GetProperty("Epoch").GetUInt64() == intent.Epoch
                && receipt.GetProperty("AuthRoute").GetString() == intent.AuthRoute
                && receipt.GetProperty("Race").GetByte() == intent.Appearance.Race
                && receipt.GetProperty("Sex").GetByte() == intent.Appearance.Sex
                && receipt.GetProperty("TargetHash").GetString() == intent.Appearance.TargetHash
                && receipt.GetProperty("TokenFee").GetUInt64() == intent.Costs[0].Amount
                && receipt.GetProperty("Primary").GetBoolean();
        }
    }
}
