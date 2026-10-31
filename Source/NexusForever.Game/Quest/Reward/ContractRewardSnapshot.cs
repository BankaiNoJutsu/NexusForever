using System.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using NexusForever.Database.Character;

namespace NexusForever.Game.Quest.Reward
{
    // Copied scalar data from the same Character transaction as the reward
    // operation. The actor validates static ownership before adopting it.
    internal static class ContractRewardSnapshot
    {
        internal static bool Same(ContractTrackState first, ContractTrackState second)
            => first == null ? second == null : second != null && first.CharacterId == second.CharacterId && first.TrackId == second.TrackId
                && first.CycleId == second.CycleId && first.Points == second.Points && first.Revision == second.Revision && first.Active == second.Active
                && first.SelectionInitialized == second.SelectionInitialized && first.Slots.SequenceEqual(second.Slots);
        internal static IReadOnlyList<ContractTrackState> Validate(IReadOnlyList<ContractTrackState> tracks, ulong characterId, ContractRewardCatalog catalog)
        {
            ArgumentNullException.ThrowIfNull(tracks);
            var copy = tracks.OrderBy(track => track?.TrackId).ToArray();
            if (copy.Any(track => track == null || track.CharacterId != characterId || track.TrackId is 0 or > 0x3FFF || track.Revision == 0)
                || copy.Select(track => track.TrackId).Distinct().Count() != copy.Length)
                throw new InvalidOperationException("Contract snapshot has invalid or duplicate durable owners.");
            if (copy.Length != 0 && (catalog == null || copy.Any(track => catalog.GetTrack(track.TrackId) == null)
                || copy.Where(track => track.Active).GroupBy(track => catalog.GetTrack(track.TrackId).Type).Any(group => group.Count() > 1)))
                throw new InvalidOperationException("Contract snapshot disagrees with its static track types.");
            ContractRewardAwardCatalog awards = null;
            foreach (var track in copy)
            {
                var definition = catalog.GetTrack(track.TrackId);
                if (track.Slots == null || track.Slots.Any(slot => slot == null)
                    || !track.SelectionInitialized && track.Slots.Count != 0
                    || track.SelectionInitialized && track.Slots.Count != definition.Slots.Count)
                    throw new InvalidOperationException("Contract selected cycle is incomplete.");
                if (!track.SelectionInitialized) continue;
                var selection = ContractRewardSelection.Restore(catalog, track.Slots[0].SelectionJson);
                if (selection.TrackId != track.TrackId || selection.CycleId != track.CycleId
                    || track.Slots.Where((slot, index) => slot.Ordinal != index || slot.RewardRowId != selection.Rewards[index]
                        || slot.RequiredPoints != definition.Slots[index].RequiredPoints || slot.SelectionJson != selection.Json).Any())
                    throw new InvalidOperationException("Contract slots lost their canonical selected cycle.");
                foreach (var slot in track.Slots.Where(slot => slot.Claimed))
                {
                    awards ??= new(catalog);
                    var quote = slot.Claim.Validate(awards, characterId, track.TrackId, track.CycleId, slot.Ordinal, slot.RewardRowId, slot.SelectionJson);
                    if (quote.Revision >= track.Revision || quote.Points > track.Points || track.Points < slot.RequiredPoints)
                        throw new InvalidOperationException("Contract claimed state lost its earned revision/point witness.");
                }
            }
            return Array.AsReadOnly(copy.Select(track => track with { Slots = Array.AsReadOnly(track.Slots.ToArray()) }).ToArray());
        }

        internal static IReadOnlyList<ContractTrackState> Read(CharacterContext context, ulong characterId, Func<ContractRewardCatalog> catalogFactory = null)
        {
            if (context.IsSaveRejected || context.HasSavepointRollback || characterId == 0
                || context.Database.CurrentTransaction?.GetDbTransaction().IsolationLevel != IsolationLevel.Serializable)
                throw new InvalidOperationException("Contract snapshot requires its live serializable Character owner.");
            var tracks = context.CharacterContractRewardTrack.AsNoTracking().Where(row => row.Id == characterId).OrderBy(row => row.TrackId).ToArray();
            var slots = context.CharacterContractRewardSlot.AsNoTracking().Where(row => row.Id == characterId).OrderBy(row => row.Ordinal).ToArray();
            var claims = context.CharacterContractRewardClaim.AsNoTracking().Where(row => row.Id == characterId).ToArray();
            var outboxes = claims.Length == 0 ? [] : context.CharacterContractAccountOutbox.AsNoTracking().Where(row => row.CharacterId == characterId).ToArray();
            var proofs = new Dictionary<(uint Track, ulong Cycle, byte Ordinal), ContractRewardClaimProof>();
            if (claims.Length != 0)
            {
                var catalog = catalogFactory?.Invoke() ?? throw new InvalidOperationException("Contract claim adoption requires its frozen award metadata.");
                var awards = new ContractRewardAwardCatalog(catalog);
                foreach (var claim in claims)
                {
                    var outbox = outboxes.SingleOrDefault(row => row.ClaimId == claim.ClaimId);
                    if (outbox != null && (outbox.AccountId != claim.AccountId || outbox.TrackId != claim.TrackId || outbox.CycleId != claim.CycleId
                        || outbox.Ordinal != claim.Ordinal || outbox.PayloadJson != claim.DeliveryJson))
                        throw new InvalidOperationException("Contract receipt and durable account intent disagree.");
                    var proof = ContractRewardClaimProof.FromModel(claim, outbox);
                    var source = slots.SingleOrDefault(row => row.TrackId == claim.TrackId && row.CycleId == claim.CycleId && row.Ordinal == claim.Ordinal);
                    if (source != null && (!source.Claimed || source.RewardRowId != claim.RewardRowId))
                        throw new InvalidOperationException("Contract receipt disagrees with its selected-slot witness.");
                    proof.Validate(awards, characterId, claim.TrackId, claim.CycleId, claim.Ordinal, claim.RewardRowId, source?.SelectionJson);
                    proofs.Add((claim.TrackId, claim.CycleId, claim.Ordinal), proof);
                }
            }
            if (slots.Any(slot => slot.Claimed != proofs.ContainsKey((slot.TrackId, slot.CycleId, slot.Ordinal))))
                throw new InvalidOperationException("Contract claimed bits and receipts disagree.");
            if (slots.Any(slot => !tracks.Any(track => track.TrackId == slot.TrackId && slot.CycleId <= track.CycleId)))
                throw new InvalidOperationException("Contract selection has an orphan or future cycle.");
            return Array.AsReadOnly(tracks.Select(row => new ContractTrackState(row.Id, row.TrackId, row.CycleId, row.Points, row.Revision, row.Active)
            {
                SelectionInitialized = row.SelectionInitialized,
                Slots = Array.AsReadOnly(slots.Where(slot => slot.TrackId == row.TrackId && slot.CycleId == row.CycleId)
                    .Select(slot => new ContractRewardSlotState(slot.Ordinal, slot.RewardRowId, slot.RequiredPoints, slot.SelectionJson,
                        proofs.GetValueOrDefault((slot.TrackId, slot.CycleId, slot.Ordinal)))).ToArray())
            }).ToArray());
        }
    }
}
