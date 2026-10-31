using System.Text.Json;
using NexusForever.Database.Character.Model;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Static.Entity;

namespace NexusForever.Game.Quest.Reward
{
    internal sealed record ContractCurrencyBefore(bool Exists, ulong Amount);
    internal sealed record ContractCurrencyDelivery(uint Version, uint CurrencyId, bool BeforeExists,
        ulong BeforeAmount, ulong AfterAmount, ulong Requested, ulong Granted)
    {
        internal static ContractCurrencyDelivery Capture(ContractCharacterCurrencyAward award, ContractCurrencyBefore before)
        {
            if (award == null || before == null || !before.Exists && before.Amount != 0
                || award.CapAmount > 0 && before.Amount > award.CapAmount)
                throw new InvalidOperationException("Contract credit requires its exact valid wallet baseline.");
            ulong granted = award.CapAmount == 0 ? award.Amount : Math.Min(award.Amount, award.CapAmount - before.Amount);
            return new(1, (uint)award.Type, before.Exists, before.Amount, checked(before.Amount + granted), award.Amount, granted);
        }
        internal string Json => JsonSerializer.Serialize(this);
        internal static ContractCurrencyDelivery Restore(ContractCharacterCurrencyAward award, string json)
        {
            if (string.IsNullOrEmpty(json) || json.Length > 4096) throw new InvalidOperationException("Missing Contract credit witness.");
            var value = JsonSerializer.Deserialize<ContractCurrencyDelivery>(json) ?? throw new InvalidOperationException("Invalid Contract credit witness.");
            var expected = Capture(award, new(value.BeforeExists, value.BeforeAmount));
            if (value != expected || value.Json != json) throw new InvalidOperationException("Contract credit arithmetic or provenance changed.");
            return value;
        }
    }

    internal sealed class ContractAccountIntent
    {
        private sealed record Payload(uint Version, ushort RealmId, ulong CharacterId, uint AccountId, string ClaimId, string QuoteJson);
        private readonly Payload payload;
        internal ushort RealmId => payload.RealmId;
        internal ulong CharacterId => payload.CharacterId;
        internal uint AccountId => payload.AccountId;
        internal string ClaimId => payload.ClaimId;
        internal string QuoteJson => payload.QuoteJson;
        internal string OperationKey => $"contract:{RealmId}:{ClaimId}";
        internal string Json { get; }
        private ContractAccountIntent(Payload value) { payload = value; Json = JsonSerializer.Serialize(value); }
        internal static ContractAccountIntent Capture(ContractRewardAwardCatalog catalog, ushort realm, uint account,
            string claim, ContractRewardClaimQuote quote)
        {
            if (realm == 0 || account == 0 || !Guid.TryParseExact(claim, "N", out var id) || id == Guid.Empty || quote == null
                || ContractRewardClaimQuote.Restore(catalog, quote.Json).Award.AccountCurrency == null)
                throw new InvalidOperationException("Contract compensation requires its original source/account/claim owner.");
            return new(new(1, realm, quote.CharacterId, account, claim, quote.Json));
        }
        internal static ContractAccountIntent Restore(ContractRewardAwardCatalog catalog, string json)
        {
            if (string.IsNullOrEmpty(json) || json.Length > 262144) throw new InvalidOperationException("Missing Contract account intent.");
            var value = JsonSerializer.Deserialize<Payload>(json) ?? throw new InvalidOperationException("Invalid Contract account intent.");
            var quote = ContractRewardClaimQuote.Restore(catalog, value.QuoteJson);
            var expected = Capture(catalog, value.RealmId, value.AccountId, value.ClaimId, quote);
            if (value.Version != 1 || value.CharacterId != quote.CharacterId || expected.Json != json)
                throw new InvalidOperationException("Contract account intent changed its retained source.");
            return expected;
        }
    }

    internal sealed record ContractRewardClaimProof(string ClaimId, uint AccountId, uint Choice, DateTime CreatedUtc,
        uint QuoteVersion, string QuoteJson, uint DeliveryVersion, string DeliveryJson, string CharacterCurrencyJson,
        ushort RealmId, bool AccountDelivered)
    {
        internal static ContractRewardClaimProof FromModel(CharacterContractRewardClaimModel row, CharacterContractAccountOutboxModel outbox)
            => new(row.ClaimId, row.AccountId, row.Choice, row.CreatedUtc, row.QuoteVersion, row.QuoteJson,
                row.DeliveryVersion, row.DeliveryJson, row.CharacterCurrencyJson, outbox?.RealmId ?? 0, outbox?.Delivered ?? false);

        internal ContractRewardClaimQuote Validate(ContractRewardAwardCatalog catalog, ulong character, uint track,
            ulong cycle, byte ordinal, uint reward, string selectionJson = null)
        {
            if (!Guid.TryParseExact(ClaimId, "N", out var identity) || identity == Guid.Empty || AccountId == 0 || CreatedUtc == default
                || CreatedUtc.Ticks % 10 != 0 || QuoteVersion != 1)
                throw new InvalidOperationException("Contract receipt identity is invalid.");
            var quote = ContractRewardClaimQuote.Restore(catalog, QuoteJson); var award = quote.Award;
            if (quote.CharacterId != character || quote.CycleId != cycle || award.TrackId != track || award.Ordinal != ordinal
                || award.RewardRowId != reward || award.Choice != Choice || selectionJson != null && quote.SelectionJson != selectionJson)
                throw new InvalidOperationException("Contract receipt lost its selected source.");
            if (award.CharacterCurrency == null)
            {
                if (CharacterCurrencyJson != null) throw new InvalidOperationException("Contract receipt invented a credit award.");
            }
            else ContractCurrencyDelivery.Restore(award.CharacterCurrency, CharacterCurrencyJson);
            if (award.Item != null)
            {
                if (DeliveryVersion != 1 || RealmId != 0 || AccountDelivered) throw new InvalidOperationException("Contract Item receipt has an account delivery owner.");
                ValidateItems(character, award.Item, DeliveryJson);
            }
            else
            {
                if (DeliveryVersion != 2 || RealmId == 0) throw new InvalidOperationException("Contract compensation has no durable outbox owner.");
                var intent = ContractAccountIntent.Restore(catalog, DeliveryJson);
                if (intent.ClaimId != ClaimId || intent.CharacterId != character || intent.AccountId != AccountId || intent.RealmId != RealmId || intent.QuoteJson != QuoteJson)
                    throw new InvalidOperationException("Contract outbox and receipt disagree.");
            }
            return quote;
        }

        private static void ValidateItems(ulong character, ContractItemAward award, string json)
        {
            if (string.IsNullOrEmpty(json) || json.Length > 1_048_576) throw new InvalidOperationException("Missing Contract Item delivery.");
            using var document = JsonDocument.Parse(json); var root = document.RootElement;
            var lines = root.GetProperty("Lines").Deserialize<InventoryRewardDeliveryLine[]>();
            if (root.GetProperty("Version").GetUInt32() != 1 || root.GetProperty("CharacterId").GetUInt64() != character
                || root.GetProperty("ItemId").GetUInt32() != award.ItemId || root.GetProperty("Count").GetUInt32() != award.Count
                || lines == null || lines.Length == 0 || new InventoryRewardDeliveryPlan(character, award.ItemId, award.Count, lines).ToJson() != json)
                throw new InvalidOperationException("Contract Item delivery changed its complete plan.");
            var ids = new HashSet<ulong>(); var positions = new HashSet<uint>(); ulong quantity = 0;
            foreach (var line in lines)
            {
                var after = line?.After; var before = line?.Before;
                if (after == null || after.Guid == 0 || !ids.Add(after.Guid) || !positions.Add(after.BagIndex) || after.CharacterId != character
                    || after.ItemId != award.ItemId || after.Location != InventoryLocation.Inventory || !after.Soulbound || line.AwardedCount == 0 || after.StackCount == 0)
                    throw new InvalidOperationException("Contract Item delivery has an invalid destination.");
                if (line.ExistingLiveItem)
                {
                    if (before == null || before.Guid != after.Guid || before.CharacterId != character || before.ItemId != award.ItemId
                        || before.Location != after.Location || before.BagIndex != after.BagIndex || !before.Soulbound || before.StackCount == 0
                        || (ulong)before.StackCount + line.AwardedCount != after.StackCount)
                        throw new InvalidOperationException("Contract Item merge changed its owner or quantity.");
                }
                else if (before != null || after.StackCount != line.AwardedCount) throw new InvalidOperationException("Contract new Item has an invalid before state.");
                quantity = checked(quantity + line.AwardedCount);
            }
            if (quantity != award.Count) throw new InvalidOperationException("Contract Item quantity differs from its canonical award.");
        }
    }
}
