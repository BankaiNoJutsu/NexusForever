using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using NexusForever.Database.Character.Model;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Static.Entity;

namespace NexusForever.Game.Challenges.Reward
{
    public sealed partial class CharacterChallengeRewardRepository
    {
        public CharacterChallengeRewardsState LoadState()
            => Stage(ReadStateCore);

        internal CharacterChallengeRewardsState ReadStateForExecution()
        {
            Require(!terminal && !context.IsSaveRejected && !context.HasSavepointRollback
                && ReferenceEquals(context.Database.CurrentTransaction, transaction)
                && (!started || enrolled && (flushed || owned.Count == 0)), "Reward snapshot is outside its execution unit.");
            return ReadStateCore();
        }

        private CharacterChallengeRewardsState ReadStateCore()
            {
                var tracks = context.CharacterChallengeRewardTrack.AsNoTracking().Where(row => row.Id == characterId).OrderBy(row => row.TrackId).ToList();
                Require(tracks.Count(row => row.Active) <= 1, "Character tracks have conflicting active selections.");
                var attempts = context.CharacterChallengeAttempt.AsNoTracking().Where(row => row.Id == characterId).ToList();
                var challenges = context.CharacterChallenge.AsNoTracking().Where(row => row.Id == characterId).ToDictionary(row => row.ChallengeId);
                var medals = context.CharacterChallengeMedalReceipt.AsNoTracking().Where(row => row.Id == characterId).ToList();
                foreach (var attempt in attempts)
                {
                    ValidateAttempt(attempt, Source(attempt.ChallengeId));
                    var receipts = medals.Where(row => row.AttemptId == attempt.AttemptId).ToArray();
                    uint mask = 0;
                    foreach (var receipt in receipts)
                    {
                        Require(receipt.Medal < 3, "Unknown persisted medal ordinal.");
                        ValidateMedal(receipt, attempt); mask |= 1u << receipt.Medal;
                    }
                    Require(mask == attempt.EarnedMedalMask && (attempt.RewardMode != (byte)CharacterChallengeRewardMode.LegacyDirect || mask == 0),
                        "Attempt medal mask/receipts disagree.");
                    if (!attempt.Terminal)
                        Require(challenges.TryGetValue(attempt.ChallengeId, out var current) && current.Activated
                            && current.ActiveAttemptId == attempt.AttemptId, "Nonterminal attempt lost its Challenge owner.");
                }
                foreach (var challenge in challenges.Values.Where(row => row.ActiveAttemptId != null))
                    Require(attempts.Any(row => row.AttemptId == challenge.ActiveAttemptId && row.ChallengeId == challenge.ChallengeId
                        && row.Terminal != challenge.Activated), "Challenge points at a missing or inconsistent attempt.");
                return new CharacterChallengeRewardsState(Array.AsReadOnly(tracks.Select(row => Track(row, ReadSlots(row))).ToArray()),
                    Array.AsReadOnly(attempts.OrderBy(row => row.AttemptId, StringComparer.Ordinal).Select(Attempt).ToArray()))
                {
                    Challenges = Array.AsReadOnly(challenges.Values.OrderBy(row => row.ChallengeId).Select(CharacterChallengeState.FromModel).ToArray()),
                    ContractTracks = Quest.Reward.ContractRewardSnapshot.Read(context, characterId, () => new Quest.ContractRewardCatalog(tables))
                };
            }

        public CharacterChallengeTrackState ChooseTrack(uint trackId, IReadOnlyDictionary<uint, ulong> expectedVersions)
            => Stage(() =>
            {
                var definition = catalog.GetTrack(trackId);
                Require(definition != null && Eligible(definition), "Requested Character track is unavailable.");
                var tracks = context.CharacterChallengeRewardTrack.AsNoTracking().Where(row => row.Id == characterId).ToList();
                Require(expectedVersions != null && expectedVersions.Count == tracks.Count
                    && tracks.All(row => expectedVersions.TryGetValue(row.TrackId, out ulong revision) && row.Revision == revision),
                    "Character track selection baseline changed.");
                Require(tracks.Count(row => row.Active) <= 1, "Character tracks have conflicting active selections.");
                foreach (var row in tracks) ReadSlots(row);
                var selected = tracks.SingleOrDefault(row => row.TrackId == trackId);
                if (selected == null) { selected = EnsureTrack(definition); tracks.Add(selected); }
                foreach (var row in tracks)
                {
                    bool active = row.TrackId == trackId;
                    if (row.Active == active) continue;
                    if (context.Entry(row).State == EntityState.Detached) context.Attach(row);
                    row.Active = active; row.Revision = checked(row.Revision + 1);
                    // EnsureTrack already owns an Added row, whose initial active
                    // state can be adjusted without enrolling it twice.
                    if (!owned.Any(held => ReferenceEquals(held.Entity, row))) Own(row, () => ReadTrack(row.TrackId));
                }
                return Track(selected, context.CharacterChallengeRewardSlot.Local.Where(row => row.TrackId == trackId && row.CycleId == selected.CycleId).Any()
                    ? context.CharacterChallengeRewardSlot.Local.Where(row => row.TrackId == trackId && row.CycleId == selected.CycleId).OrderBy(row => row.Ordinal).ToList()
                    : ReadSlots(selected));
            });

        public CharacterChallengeClaimState Claim(uint trackId, ulong cycleId, ulong expectedRevision,
            uint rewardRowId, uint choice, string claimId, DateTime createdUtc, IInventoryRewardReservation reservation)
            => Stage<CharacterChallengeClaimState>(() =>
            {
                Require(ValidId(claimId) && createdUtc != default && createdUtc.Kind == DateTimeKind.Utc,
                    "Invalid claim identity/time.");
                var reference = catalog.GetReward(rewardRowId);
                Require(reference != null && reference.Track.Id == trackId && Eligible(reference.Track), "Claim catalog/prerequisite mismatch.");
                var selectedChoice = reference.Reward.GetChoice(choice);
                Require(selectedChoice != null, "Claim choice is outside the canonical row.");
                var track = ReadTrack(trackId);
                Require(track != null, "Claim track has not been initialized.");
                var slots = ReadSlots(track);
                var duplicate = context.CharacterChallengeRewardClaim.AsNoTracking().SingleOrDefault(row =>
                    row.Id == characterId && row.TrackId == trackId && row.CycleId == cycleId && row.Ordinal == reference.Slot.Position);
                if (duplicate != null)
                {
                    ValidateClaim(duplicate, reference);
                    var historic = context.CharacterChallengeRewardSlot.AsNoTracking().SingleOrDefault(row => row.Id == characterId
                        && row.TrackId == trackId && row.CycleId == cycleId && row.Ordinal == reference.Slot.Position);
                    Require(duplicate.RewardRowId == rewardRowId && duplicate.Choice == choice,
                        "A different choice cannot replace an existing claim.");
                    Require(historic != null && historic.Claimed && historic.RewardRowId == rewardRowId
                        && historic.RequiredPoints == reference.Slot.Cost, "Historic claim has lost its selected-slot witness.");
                    return new(false, duplicate.ClaimId, trackId, cycleId, duplicate.Ordinal, rewardRowId, choice, Track(track, slots));
                }
                Require(track.CycleId == cycleId && track.Revision == expectedRevision, "Claim cycle/revision is obsolete.");
                var slot = slots.Single(row => row.Ordinal == reference.Slot.Position);
                Require(!slot.Claimed && slot.RewardRowId == rewardRowId && track.Points >= slot.RequiredPoints,
                    "Requested reward is not the earned selected durable slot.");
                Require(reservation != null && reservation.CharacterId == characterId && reservation.ItemId == selectedChoice.ItemId
                    && reservation.Count == selectedChoice.Count
                    && reservation.State is InventoryRewardReservationState.Prepared or InventoryRewardReservationState.ReadyToRetry,
                    "Inventory reservation does not match the canonical claim delivery.");
                Require(!context.CharacterChallengeRewardClaim.AsNoTracking().Any(row => row.ClaimId == claimId), "Claim identity was already used.");
                context.Attach(slot); slot.Claimed = true;
                context.Attach(track); track.Revision = checked(track.Revision + 1);
                var claim = new CharacterChallengeRewardClaimModel
                {
                    Id = characterId, TrackId = trackId, CycleId = cycleId, Ordinal = slot.Ordinal,
                    RewardRowId = rewardRowId, Choice = choice, ClaimId = claimId, CreatedUtc = SqlTime(createdUtc),
                    DeliveryJson = ""
                };
                context.CharacterChallengeRewardClaim.Add(claim);
                InventoryRewardDeliveryPlan plan = reservation.Stage(context, claim);
                Require(plan != null && plan.CharacterId == characterId && plan.ItemId == selectedChoice.ItemId
                    && plan.Count == selectedChoice.Count && plan.Lines.Count != 0
                    && plan.Lines.All(line => line.After.CharacterId == characterId && line.After.ItemId == selectedChoice.ItemId
                        && line.After.Soulbound && line.After.Location == InventoryLocation.Inventory)
                    && plan.Lines.Aggregate(0ul, (sum, line) => checked(sum + line.AwardedCount)) == selectedChoice.Count,
                    "Staged inventory plan does not match the canonical claim.");
                deliveryItemIds = plan.Lines.Select(line => line.After.Guid).ToHashSet();
                Require(deliveryItemIds.Count == plan.Lines.Count && !deliveryItemIds.Contains(0)
                    && claim.DeliveryVersion == plan.Version && claim.DeliveryJson == plan.ToJson(), "Incomplete inventory delivery receipt.");
                Own(slot, () => context.CharacterChallengeRewardSlot.AsNoTracking().SingleOrDefault(row =>
                    row.Id == characterId && row.TrackId == trackId && row.CycleId == cycleId && row.Ordinal == slot.Ordinal));
                Own(claim, () => context.CharacterChallengeRewardClaim.AsNoTracking().SingleOrDefault(row =>
                    row.Id == characterId && row.TrackId == trackId && row.CycleId == cycleId && row.Ordinal == slot.Ordinal));
                var selection = catalog.RestoreSelection(trackId, cycleId, slots.Select(row => row.RewardRowId));
                uint mask = slots.Aggregate(0u, (value, row) => value | (row.Claimed ? 1u << row.Ordinal : 0));
                if (catalog.TryAdvanceCycle(selection, track.Points, mask, out var next, out ulong carry, selector))
                {
                    track.CycleId = next.CycleId; track.Points = checked((uint)carry);
                    slots = AddSlots(next);
                }
                Own(track, () => ReadTrack(trackId));
                return new(true, claimId, trackId, cycleId, slot.Ordinal, rewardRowId, choice, Track(track, slots));
            });

        private CharacterChallengeRewardTrackModel EnsureTrack(CharacterChallengeTrackDefinition definition)
        {
            var track = ReadTrack(definition.Id);
            if (track != null) { ReadSlots(track); return track; }
            var selection = catalog.CreateSelection(definition.Id, 0, selector);
            Require(!context.CharacterChallengeRewardSlot.AsNoTracking().Any(row => row.Id == characterId && row.TrackId == definition.Id),
                "Orphaned Character reward slots exist.");
            int active = context.CharacterChallengeRewardTrack.AsNoTracking().Count(row => row.Id == characterId && row.Active);
            Require(active <= 1, "Character tracks have conflicting active selections.");
            track = new() { Id = characterId, TrackId = definition.Id, CycleId = 0, Revision = 1, Active = active == 0 };
            context.CharacterChallengeRewardTrack.Add(track);
            Own(track, () => ReadTrack(definition.Id));
            AddSlots(selection);
            return track;
        }

        private List<CharacterChallengeRewardSlotModel> AddSlots(CharacterChallengeRewardSelection selection)
        {
            var rows = new List<CharacterChallengeRewardSlotModel>();
            foreach (var selected in selection.Slots)
            {
                var row = new CharacterChallengeRewardSlotModel
                {
                    Id = characterId, TrackId = selection.TrackId, CycleId = selection.CycleId,
                    Ordinal = selected.Position, RequiredPoints = selected.Cost, RewardRowId = selected.Reward.Id
                };
                context.CharacterChallengeRewardSlot.Add(row);
                Own(row, () => context.CharacterChallengeRewardSlot.AsNoTracking().SingleOrDefault(value => value.Id == characterId
                    && value.TrackId == row.TrackId && value.CycleId == row.CycleId && value.Ordinal == row.Ordinal));
                rows.Add(row);
            }
            return rows;
        }

        private CharacterChallengeRewardTrackModel ReadTrack(uint id)
            => context.CharacterChallengeRewardTrack.AsNoTracking().SingleOrDefault(row => row.Id == characterId && row.TrackId == id);

        private List<CharacterChallengeRewardSlotModel> ReadSlots(CharacterChallengeRewardTrackModel track)
        {
            Require(track.Revision != 0, "Character track has no durable revision.");
            var slots = context.CharacterChallengeRewardSlot.AsNoTracking().Where(row => row.Id == characterId
                && row.TrackId == track.TrackId && row.CycleId == track.CycleId).OrderBy(row => row.Ordinal).ToList();
            var selection = catalog.RestoreSelection(track.TrackId, track.CycleId, slots.Select(row => row.RewardRowId));
            Require(slots.Count == selection.Slots.Count && slots.Select((row, ordinal) => row.Ordinal == ordinal
                && row.RequiredPoints == selection.Slots[ordinal].Cost).All(valid => valid), "Durable Challenge ordinals/costs changed.");
            var claims = context.CharacterChallengeRewardClaim.AsNoTracking().Where(row => row.Id == characterId
                && row.TrackId == track.TrackId && row.CycleId == track.CycleId).ToDictionary(row => row.Ordinal);
            foreach (var slot in slots)
            {
                bool exists = claims.TryGetValue(slot.Ordinal, out var claim);
                Require(slot.Claimed == exists && (!slot.Claimed || track.Points >= slot.RequiredPoints), "Claimed bits lack their earned durable witnesses.");
                if (exists) ValidateClaim(claim, catalog.GetReward(slot.RewardRowId));
            }
            Require(claims.Count == slots.Count(row => row.Claimed), "Unknown claim ordinal in the current cycle.");
            return slots;
        }

        private void ValidateClaim(CharacterChallengeRewardClaimModel claim, CharacterChallengeRewardReference reference)
        {
            Require(reference != null && claim.Id == characterId && claim.TrackId == reference.Track.Id
                && claim.Ordinal == reference.Slot.Position && claim.RewardRowId == reference.Reward.Id
                && ValidId(claim.ClaimId) && claim.CreatedUtc != default && claim.DeliveryVersion == 1,
                "Durable claim identity/catalog is inconsistent.");
            var choice = reference.Reward.GetChoice(claim.Choice);
            Require(choice != null, "Durable claim has an invalid choice.");
            using var json = JsonDocument.Parse(claim.DeliveryJson);
            JsonElement payload = json.RootElement;
            Require(payload.GetProperty("Version").GetUInt32() == 1 && payload.GetProperty("CharacterId").GetUInt64() == characterId
                && payload.GetProperty("ItemId").GetUInt32() == choice.ItemId && payload.GetProperty("Count").GetUInt32() == choice.Count
                && payload.GetProperty("Lines").GetArrayLength() != 0, "Durable claim payload disagrees with its canonical choice.");
            var ids = new HashSet<ulong>();
            var positions = new HashSet<uint>();
            ulong quantity = 0;
            foreach (var line in payload.GetProperty("Lines").EnumerateArray())
            {
                uint awarded = line.GetProperty("AwardedCount").GetUInt32();
                var before = line.GetProperty("Before").Deserialize<InventoryRewardItemSnapshot>();
                var after = line.GetProperty("After").Deserialize<InventoryRewardItemSnapshot>();
                Require(awarded != 0 && after != null && after.Guid != 0 && ids.Add(after.Guid)
                    && positions.Add(after.BagIndex) && after.CharacterId == characterId && after.ItemId == choice.ItemId
                    && after.Location == InventoryLocation.Inventory && after.Soulbound && after.StackCount != 0,
                    "Durable claim has an invalid Item destination/identity.");
                if (line.GetProperty("ExistingLiveItem").GetBoolean())
                    Require(before != null && before.Guid == after.Guid && before.CharacterId == characterId && before.ItemId == choice.ItemId
                        && before.Soulbound && before.Location == after.Location && before.BagIndex == after.BagIndex
                        && before.StackCount != 0 && (ulong)before.StackCount + awarded == after.StackCount,
                        "Durable claim merge quantity/ownership is inconsistent.");
                else Require(before == null && after.StackCount == awarded, "Durable new stack has an invalid before state/quantity.");
                quantity = checked(quantity + awarded);
            }
            Require(quantity == choice.Count, "Durable claim Item quantities do not equal its canonical choice.");
        }

        private static CharacterChallengeTrackState Track(CharacterChallengeRewardTrackModel row, IEnumerable<CharacterChallengeRewardSlotModel> slots)
            => new(row.TrackId, row.CycleId, row.Points, row.Revision, row.Active, Array.AsReadOnly(slots.OrderBy(slot => slot.Ordinal)
                .Select(slot => new CharacterChallengeSlotState(slot.Ordinal, slot.RewardRowId, slot.RequiredPoints, slot.Claimed)).ToArray()));
    }
}
