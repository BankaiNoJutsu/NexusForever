using System.Numerics;
using System.Security.Cryptography;
using NexusForever.GameTable;
using NexusForever.GameTable.Model;

namespace NexusForever.Game.Challenges.Reward
{
    /// <summary>
    /// Immutable, validated type3 static data. Selection is explicit server policy:
    /// sample once per ordinal per durable cycle; persistence must retain the result.
    /// This class neither credits points nor grants items or baseline entitlements.
    /// </summary>
    public sealed class CharacterChallengeRewardCatalog
    {
        public const uint ChallengeTrackType = 3;
        private readonly Dictionary<uint, CharacterChallengeTrackDefinition> tracks = [];
        private readonly Dictionary<uint, CharacterChallengeRewardReference> rewards = [];
        private readonly HashSet<uint> unsupported = [];

        public IReadOnlyList<CharacterChallengeTrackDefinition> Tracks { get; }
        public IReadOnlyList<uint> UnsupportedTrackIds { get; }

        public CharacterChallengeRewardCatalog(IGameTableManager tables)
        {
            ArgumentNullException.ThrowIfNull(tables);
            RewardTrackEntry[] entries = tables.RewardTrack?.Entries
                ?? throw Invalid("RewardTrack table is unavailable.");
            RewardTrackRewardsEntry[] rows = tables.RewardTrackRewards?.Entries
                ?? throw Invalid("RewardTrackRewards table is unavailable.");
            RewardTrackEntry[] challenge = entries.Where(row => row.RewardTrackTypeEnum == ChallengeTrackType).ToArray();
            var ids = challenge.Select(row => row.Id).ToHashSet();
            if (entries.GroupBy(row => row.Id).Any(group => ids.Contains(group.Key) && group.Count() != 1))
                throw Invalid("A Challenge track identity is duplicated.");
            if (rows.GroupBy(row => row.Id).Any(group => group.Count() != 1 && group.Any(row => ids.Contains(row.RewardTrackId))))
                throw Invalid("A Challenge reward identity is duplicated.");

            foreach (RewardTrackEntry entry in challenge.OrderBy(row => row.Id))
            {
                if (entry.Id == 0 || entry.Id > 0x3FFF || entry.Flags != 0 || entry.RewardTrackIdParent != 0)
                    throw Invalid($"Challenge track {entry.Id} has unsupported identity, flags or parent.");
                uint[] costs = Costs(entry);
                RewardTrackRewardsEntry[] pool = rows.Where(row => row.RewardTrackId == entry.Id).ToArray();
                // The actual five empty type3 catalogs remain an explicit fallback.
                // A malformed nonempty pool is never silently converted to fallback.
                if (pool.Length == 0)
                {
                    unsupported.Add(entry.Id);
                    continue;
                }
                var buckets = Enumerable.Range(0, costs.Length).Select(_ => new List<CharacterChallengeRewardDefinition>()).ToArray();
                foreach (RewardTrackRewardsEntry row in pool)
                {
                    uint mask = row.RewardPointFlags;
                    if (row.Id == 0 || row.Id > 0x3FFF || mask == 0 || (mask & (mask - 1)) != 0 || mask >= 1u << costs.Length)
                        throw Invalid($"Challenge reward {row.Id} has an invalid cost ordinal or wire identity.");
                    byte position = (byte)BitOperations.TrailingZeroCount(mask);
                    if ((row.Flags & ~3u) != 0 || (row.Flags & 2u) == 0
                        || ((row.Flags & 1u) != 0) != (position == costs.Length - 1))
                        throw Invalid($"Challenge reward {row.Id} has unsupported binding/claim/Casque flags.");
                    if (row.PrerequisiteId != 0 || row.CurrencyTypeId != 0 || row.CurrencyAmount != 0)
                        throw Invalid($"Challenge reward {row.Id} has unsupported prerequisite or currency delivery.");
                    buckets[position].Add(new CharacterChallengeRewardDefinition(row.Id, row.Flags, Choices(row)));
                }
                if (buckets.Any(bucket => bucket.Count == 0))
                    throw Invalid($"Challenge track {entry.Id} has a missing cost position.");
                var slots = buckets.Select((bucket, index) => new CharacterChallengeRewardSlot((byte)index, costs[index],
                    bucket.OrderBy(row => row.Id).ToArray())).ToArray();
                var definition = new CharacterChallengeTrackDefinition(entry.Id, entry.PrerequisiteId, slots);
                tracks.Add(definition.Id, definition);
                foreach (CharacterChallengeRewardSlot slot in slots)
                    foreach (CharacterChallengeRewardDefinition reward in slot.Candidates)
                        rewards.Add(reward.Id, new CharacterChallengeRewardReference(definition, slot, reward));
            }
            Tracks = Array.AsReadOnly(tracks.Values.OrderBy(track => track.Id).ToArray());
            UnsupportedTrackIds = Array.AsReadOnly(unsupported.OrderBy(id => id).ToArray());
        }

        public CharacterChallengeTrackDefinition GetTrack(uint trackId) => tracks.GetValueOrDefault(trackId);
        public CharacterChallengeRewardReference GetReward(uint rewardRowId) => rewards.GetValueOrDefault(rewardRowId);
        public bool TryGetTrack(uint trackId, out CharacterChallengeTrackDefinition track) => tracks.TryGetValue(trackId, out track);
        public bool TryGetReward(uint rewardRowId, out CharacterChallengeRewardReference reward) => rewards.TryGetValue(rewardRowId, out reward);

        public CharacterChallengeRewardSelection CreateSelection(uint trackId, ulong cycleId, Func<int, int> nextIndex = null)
        {
            CharacterChallengeTrackDefinition track = RequireTrack(trackId);
            nextIndex ??= RandomNumberGenerator.GetInt32;
            var selected = new CharacterChallengeSelectedSlot[track.Slots.Count];
            foreach (CharacterChallengeRewardSlot slot in track.Slots)
            {
                int index = nextIndex(slot.Candidates.Count);
                if ((uint)index >= (uint)slot.Candidates.Count)
                    throw new ArgumentOutOfRangeException(nameof(nextIndex), "Selector returned an index outside its canonical bucket.");
                selected[slot.Position] = new CharacterChallengeSelectedSlot(slot.Position, slot.Cost, slot.Candidates[index]);
            }
            return new CharacterChallengeRewardSelection(track.Id, cycleId, selected);
        }

        /// <summary>Rebuild a persisted selection without resampling or compacting ordinals.</summary>
        public CharacterChallengeRewardSelection RestoreSelection(uint trackId, ulong cycleId, IEnumerable<uint> rewardRowIds)
        {
            ArgumentNullException.ThrowIfNull(rewardRowIds);
            CharacterChallengeTrackDefinition track = RequireTrack(trackId);
            uint[] ids = rewardRowIds.ToArray();
            if (ids.Length != track.Slots.Count)
                throw Invalid("Persisted Challenge selection has an incorrect ordinal count.");
            var selected = new CharacterChallengeSelectedSlot[ids.Length];
            for (int i = 0; i < ids.Length; i++)
            {
                CharacterChallengeRewardDefinition reward = track.Slots[i].Candidates.SingleOrDefault(row => row.Id == ids[i]);
                if (reward == null)
                    throw Invalid($"Persisted reward {ids[i]} is not in track {trackId} ordinal {i}.");
                selected[i] = new CharacterChallengeSelectedSlot((byte)i, track.Slots[i].Cost, reward);
            }
            return new CharacterChallengeRewardSelection(trackId, cycleId, selected);
        }

        /// <summary>
        /// Explicit upstream-style server policy, not recovered retail reset timing.
        /// Reset only after every ordinal was claimed, subtract maximum once and carry
        /// excess. The returned blueprint must be persisted with that transition.
        /// </summary>
        public bool TryAdvanceCycle(CharacterChallengeRewardSelection current, ulong points, uint claimedMask,
            out CharacterChallengeRewardSelection next, out ulong carriedPoints, Func<int, int> nextIndex = null)
        {
            ArgumentNullException.ThrowIfNull(current);
            if (points > uint.MaxValue)
                throw new ArgumentOutOfRangeException(nameof(points), "Challenge progress exceeds its uint wire field.");
            // Revalidate a blueprint originating from another catalog/build.
            RestoreSelection(current.TrackId, current.CycleId, current.Slots.Select(slot => slot.Reward.Id));
            next = null;
            carriedPoints = points;
            if (!CharacterChallengeCyclePolicy.AllClaimed(claimedMask, current.Slots.Count))
                return false;
            uint maximum = RequireTrack(current.TrackId).MaximumPoints;
            if (points < maximum)
                throw Invalid("Fully claimed Challenge cycle has insufficient earned points.");
            next = CreateSelection(current.TrackId, checked(current.CycleId + 1), nextIndex);
            carriedPoints = points - maximum;
            return true;
        }

        private CharacterChallengeTrackDefinition RequireTrack(uint id)
        {
            if (unsupported.Contains(id)) throw new NotSupportedException($"Challenge track {id} has no reward catalog.");
            return GetTrack(id) ?? throw new KeyNotFoundException($"Challenge track {id} is unknown or belongs to another owner.");
        }

        private static uint[] Costs(RewardTrackEntry entry)
        {
            uint[] raw = [entry.RewardPointCost00, entry.RewardPointCost01, entry.RewardPointCost02,
                entry.RewardPointCost03, entry.RewardPointCost04, entry.RewardPointCost05,
                entry.RewardPointCost06, entry.RewardPointCost07, entry.RewardPointCost08, entry.RewardPointCost09];
            var costs = new List<uint>();
            bool ended = false;
            foreach (uint value in raw)
            {
                if (value == 0) { ended = true; continue; }
                if (ended || costs.Count != 0 && value < costs[^1])
                    throw Invalid($"Challenge track {entry.Id} has noncontiguous or decreasing costs.");
                costs.Add(value);
            }
            if (costs.Count == 0) throw Invalid($"Challenge track {entry.Id} has no cost positions.");
            return costs.ToArray();
        }

        private static CharacterChallengeRewardChoice[] Choices(RewardTrackRewardsEntry row)
        {
            uint[] types = [row.RewardTrackRewardTypeEnum00, row.RewardTrackRewardTypeEnum01, row.RewardTrackRewardTypeEnum02];
            uint[] ids = [row.RewardChoiceId00, row.RewardChoiceId01, row.RewardChoiceId02];
            uint[] counts = [row.RewardChoiceCount00, row.RewardChoiceCount01, row.RewardChoiceCount02];
            var result = new List<CharacterChallengeRewardChoice>();
            bool ended = false;
            for (byte i = 0; i < ids.Length; i++)
            {
                if (ids[i] == 0 && counts[i] == 0 && types[i] == 0) { ended = true; continue; }
                // This catalog supports Item2 delivery only. Counts use the existing
                // unsigned item quantity contract; zero is never normalized to one.
                if (ended || types[i] != 0 || ids[i] == 0 || counts[i] == 0)
                    throw Invalid($"Challenge reward {row.Id} has an invalid or unsupported choice at {i}.");
                result.Add(new CharacterChallengeRewardChoice(i, ids[i], counts[i]));
            }
            if (result.Count == 0) throw Invalid($"Challenge reward {row.Id} has no choices.");
            return result.ToArray();
        }

        private static InvalidDataException Invalid(string message) => new(message);
    }

    public sealed class CharacterChallengeTrackDefinition
    {
        public uint Id { get; }
        public uint PrerequisiteId { get; }
        public IReadOnlyList<CharacterChallengeRewardSlot> Slots { get; }
        public uint MaximumPoints => Slots[^1].Cost;
        internal CharacterChallengeTrackDefinition(uint id, uint prerequisiteId, CharacterChallengeRewardSlot[] slots)
        { Id = id; PrerequisiteId = prerequisiteId; Slots = Array.AsReadOnly(slots); }
    }

    public sealed class CharacterChallengeRewardSlot
    {
        public byte Position { get; }
        public uint Cost { get; }
        public IReadOnlyList<CharacterChallengeRewardDefinition> Candidates { get; }
        internal CharacterChallengeRewardSlot(byte position, uint cost, CharacterChallengeRewardDefinition[] candidates)
        { Position = position; Cost = cost; Candidates = Array.AsReadOnly(candidates); }
    }

    public sealed class CharacterChallengeRewardDefinition
    {
        public uint Id { get; }
        public bool ForceSoulbind => true;
        public bool AutoClaim => false;
        public bool IsCasque { get; }
        public IReadOnlyList<CharacterChallengeRewardChoice> Choices { get; }
        public CharacterChallengeRewardChoice GetChoice(uint index) => index < Choices.Count ? Choices[(int)index] : null;
        internal CharacterChallengeRewardDefinition(uint id, uint flags, CharacterChallengeRewardChoice[] choices)
        { Id = id; IsCasque = (flags & 1) != 0; Choices = Array.AsReadOnly(choices); }
    }

    public sealed record CharacterChallengeRewardChoice(byte Index, uint ItemId, uint Count);
    public sealed record CharacterChallengeRewardReference(CharacterChallengeTrackDefinition Track, CharacterChallengeRewardSlot Slot, CharacterChallengeRewardDefinition Reward);
    public sealed record CharacterChallengeSelectedSlot(byte Position, uint Cost, CharacterChallengeRewardDefinition Reward);

    public sealed class CharacterChallengeRewardSelection
    {
        public uint TrackId { get; }
        public ulong CycleId { get; }
        public IReadOnlyList<CharacterChallengeSelectedSlot> Slots { get; }
        internal CharacterChallengeRewardSelection(uint trackId, ulong cycleId, CharacterChallengeSelectedSlot[] slots)
        { TrackId = trackId; CycleId = cycleId; Slots = Array.AsReadOnly(slots); }
    }

    public static class CharacterChallengeCyclePolicy
    {
        public static bool AllClaimed(uint claimedMask, int slotCount)
        {
            if (slotCount < 1 || slotCount > 32) throw new ArgumentOutOfRangeException(nameof(slotCount));
            uint required = slotCount == 32 ? uint.MaxValue : (1u << slotCount) - 1;
            if ((claimedMask & ~required) != 0) throw new ArgumentOutOfRangeException(nameof(claimedMask));
            return claimedMask == required;
        }
    }
}
