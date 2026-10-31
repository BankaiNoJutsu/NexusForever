using System.Text.Json;

namespace NexusForever.Game.Quest.Reward
{
    // An immutable quote for the later claim transaction/outbox. Neither a
    // serialized quote nor a cache snapshot proves sent-state ownership, a
    // durable unclaimed slot, a wallet baseline or a successful delivery.
    internal sealed class ContractRewardClaimQuote
    {
        private sealed record Payload(uint Version, ulong CharacterId, ulong CycleId, ulong Revision,
            uint Points, string SelectionJson, ContractRewardAward Award);
        private readonly Payload payload;
        internal ulong CharacterId => payload.CharacterId;
        internal ulong CycleId => payload.CycleId;
        internal ulong Revision => payload.Revision;
        internal uint Points => payload.Points;
        internal string SelectionJson => payload.SelectionJson;
        internal ContractRewardAward Award => payload.Award;
        internal string Json { get; }

        private ContractRewardClaimQuote(Payload payload)
        {
            this.payload = payload;
            Json = JsonSerializer.Serialize(payload);
        }

        internal static ContractRewardClaimQuote Capture(ContractRewardAwardCatalog catalog, ulong characterId,
            IReadOnlyList<ContractTrackState> tracks, uint trackId, ulong cycleId, ulong revision, uint rewardRowId, uint choice)
        {
            ArgumentNullException.ThrowIfNull(catalog);
            if (characterId == 0) throw new InvalidOperationException("Contract quote requires a Character owner.");
            var validated = ContractRewardSnapshot.Validate(tracks, characterId, catalog.Catalog);
            var track = validated.SingleOrDefault(row => row.TrackId == trackId);
            var award = catalog.GetAward(rewardRowId, choice);
            if (track == null || !track.Active || !track.SelectionInitialized || track.CycleId != cycleId || track.Revision != revision
                || award == null || award.TrackId != trackId || track.Points < award.RequiredPoints
                || track.Slots[award.Ordinal].RewardRowId != rewardRowId || track.Slots[award.Ordinal].Claimed)
                throw new InvalidOperationException("Contract quote requires its earned, selected current cycle and choice.");
            return new(new(1, characterId, cycleId, revision, track.Points, track.Slots[award.Ordinal].SelectionJson, award));
        }

        internal static ContractRewardClaimQuote Restore(ContractRewardAwardCatalog catalog, string json)
        {
            ArgumentNullException.ThrowIfNull(catalog);
            if (string.IsNullOrEmpty(json) || json.Length > 131072)
                throw new InvalidOperationException("Contract quote is missing or oversized.");
            var payload = JsonSerializer.Deserialize<Payload>(json);
            if (payload == null || payload.Version != 1 || payload.CharacterId == 0 || payload.Revision == 0 || payload.Award == null)
                throw new InvalidOperationException("Contract quote identity is invalid.");
            var award = catalog.GetAward(payload.Award.RewardRowId, payload.Award.Choice);
            if (award == null || award != payload.Award || payload.Points < award.RequiredPoints)
                throw new InvalidOperationException("Contract quote no longer matches its complete canonical award.");
            var selection = ContractRewardSelection.Restore(catalog.Catalog, payload.SelectionJson);
            if (selection.TrackId != award.TrackId || selection.CycleId != payload.CycleId || selection.Rewards[award.Ordinal] != award.RewardRowId)
                throw new InvalidOperationException("Contract quote lost its selected-cycle provenance.");
            var restored = new ContractRewardClaimQuote(payload);
            if (restored.Json != json) throw new InvalidOperationException("Contract quote is not canonical.");
            return restored;
        }
    }
}
