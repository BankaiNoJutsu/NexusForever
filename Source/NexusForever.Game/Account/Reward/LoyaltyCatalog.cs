using NexusForever.GameTable;
using NexusForever.GameTable.Model;

namespace NexusForever.Game.Account.Reward
{
    /// <summary>
    /// Validated static Loyalty contract. Cost position, claim bit and transmitted
    /// reward-list position must remain the same ordinal, even when costs repeat.
    /// </summary>
    public sealed class LoyaltyCatalog
    {
        public const uint RootTrackId = 9;
        public const uint LoyaltyTrackType = 2;

        public IReadOnlyList<LoyaltyTrackDefinition> Tracks { get; }

        private readonly Dictionary<uint, LoyaltyTrackDefinition> tracks = [];
        private readonly Dictionary<uint, LoyaltyRewardReference> rewards = [];

        public LoyaltyCatalog(IGameTableManager tables)
        {
            ArgumentNullException.ThrowIfNull(tables);
            RewardTrackEntry[] entries = tables.RewardTrack?.Entries
                ?? throw Invalid("RewardTrack table is unavailable.");
            RewardTrackRewardsEntry[] rewardEntries = tables.RewardTrackRewards?.Entries
                ?? throw Invalid("RewardTrackRewards table is unavailable.");
            if (tables.AccountItem == null)
                throw Invalid("AccountItem table is unavailable.");

            var loyalty = new Dictionary<uint, RewardTrackEntry>();
            foreach (RewardTrackEntry entry in entries.Where(e => e.RewardTrackTypeEnum == LoyaltyTrackType))
            {
                if (entry.Id == 0 || entry.Id >= 1u << 14 || entry.Flags != 1 || !loyalty.TryAdd(entry.Id, entry))
                    throw Invalid($"Invalid or duplicate linear Loyalty track {entry.Id}.");
            }
            if (!loyalty.TryGetValue(RootTrackId, out var root) || root.RewardTrackIdParent != 0)
                throw Invalid("Canonical Loyalty root9 is missing or has a parent.");
            if (entries.GroupBy(e => e.Id).Any(g => g.Count() != 1 && loyalty.ContainsKey(g.Key)))
                throw Invalid("Loyalty track identity collides with another static row.");
            if (rewardEntries.GroupBy(e => e.Id).Any(g => g.Count() != 1 && g.Any(e => loyalty.ContainsKey(e.RewardTrackId))))
                throw Invalid("A Loyalty reward row ID is duplicated.");

            var children = new Dictionary<uint, RewardTrackEntry>();
            foreach (RewardTrackEntry entry in loyalty.Values.Where(e => e.Id != RootTrackId))
            {
                if (!loyalty.ContainsKey(entry.RewardTrackIdParent) || !children.TryAdd(entry.RewardTrackIdParent, entry))
                    throw Invalid($"Loyalty track {entry.Id} has an invalid or branching parent.");
            }

            List<LoyaltyTrackDefinition> ordered = [];
            ulong offset = 0;
            for (RewardTrackEntry entry = root; entry != null; children.TryGetValue(entry.Id, out entry))
            {
                if (tracks.ContainsKey(entry.Id))
                    throw Invalid("Loyalty parent chain contains a cycle.");
                uint[] costs = GetCosts(entry);
                RewardTrackRewardsEntry[] pool = rewardEntries.Where(r => r.RewardTrackId == entry.Id).ToArray();
                LoyaltyRewardSlot[] slots = new LoyaltyRewardSlot[costs.Length];

                foreach (RewardTrackRewardsEntry reward in pool)
                {
                    uint mask = reward.RewardPointFlags;
                    if (reward.Id == 0 || reward.Id >= 1u << 14 || mask == 0 || (mask & (mask - 1)) != 0 || mask >= 1u << costs.Length)
                        throw Invalid($"Reward {reward.Id} has no unambiguous Loyalty cost position.");
                    byte position = (byte)System.Numerics.BitOperations.TrailingZeroCount(mask);
                    // Native1404d2ea0 excludes Casque from nonfinal buckets and
                    // requires it for the final bucket. Never compact a hole.
                    if (((reward.Flags & 1u) != 0) != (position == costs.Length - 1) || (reward.Flags & ~7u) != 0)
                        throw Invalid($"Reward {reward.Id} has unsupported flags or Casque position.");
                    if (slots[position] != null)
                        throw Invalid($"Loyalty track {entry.Id} has ambiguous reward position {position}.");
                    ValidateChoice(reward, tables);
                    slots[position] = new LoyaltyRewardSlot(position, costs[position], reward);
                }
                if (slots.Any(s => s == null))
                    throw Invalid($"Loyalty track {entry.Id} is missing a reward position.");

                var definition = new LoyaltyTrackDefinition(entry, offset, slots);
                tracks.Add(entry.Id, definition);
                ordered.Add(definition);
                foreach (LoyaltyRewardSlot slot in slots)
                    rewards.Add(slot.Entry.Id, new LoyaltyRewardReference(definition, slot));
                offset = checked(offset + definition.MaximumPoints);
            }
            if (ordered.Count != loyalty.Count)
                throw Invalid("Loyalty tracks are disconnected from the canonical parent chain.");
            Tracks = Array.AsReadOnly(ordered.ToArray());
        }

        public LoyaltyTrackDefinition GetTrack(uint trackId) => tracks.GetValueOrDefault(trackId);
        public LoyaltyRewardReference GetReward(uint rewardRowId) => rewards.GetValueOrDefault(rewardRowId);

        private static uint[] GetCosts(RewardTrackEntry entry)
        {
            uint[] raw = [entry.RewardPointCost00, entry.RewardPointCost01, entry.RewardPointCost02,
                entry.RewardPointCost03, entry.RewardPointCost04, entry.RewardPointCost05,
                entry.RewardPointCost06, entry.RewardPointCost07, entry.RewardPointCost08, entry.RewardPointCost09];
            List<uint> costs = [];
            bool ended = false;
            foreach (uint cost in raw)
            {
                if (cost == 0)
                {
                    ended = true;
                    continue;
                }
                if (ended || costs.Count != 0 && cost < costs[^1])
                    throw Invalid($"Loyalty track {entry.Id} has noncontiguous or decreasing costs.");
                costs.Add(cost);
            }
            if (costs.Count == 0)
                throw Invalid($"Loyalty track {entry.Id} has no cost positions.");
            return costs.ToArray();
        }

        private static void ValidateChoice(RewardTrackRewardsEntry reward, IGameTableManager tables)
        {
            if (reward.CurrencyTypeId != 0 || reward.CurrencyAmount != 0
                || reward.RewardTrackRewardTypeEnum00 != 1 || reward.RewardChoiceId00 == 0 || reward.RewardChoiceCount00 != 1
                || reward.RewardTrackRewardTypeEnum01 != 0 || reward.RewardChoiceId01 != 0 || reward.RewardChoiceCount01 != 0
                || reward.RewardTrackRewardTypeEnum02 != 0 || reward.RewardChoiceId02 != 0 || reward.RewardChoiceCount02 != 0
                || tables.AccountItem.GetEntry(reward.RewardChoiceId00) == null)
                throw Invalid($"Reward {reward.Id} is not a supported single AccountItem Loyalty reward.");
        }

        private static InvalidDataException Invalid(string message) => new(message);
    }

    public sealed class LoyaltyTrackDefinition
    {
        public RewardTrackEntry Entry { get; }
        public uint Id { get; }
        public uint ParentId { get; }
        public uint MaximumPoints { get; }
        public ulong Offset { get; }
        public IReadOnlyList<LoyaltyRewardSlot> Slots { get; }

        internal LoyaltyTrackDefinition(RewardTrackEntry entry, ulong offset, LoyaltyRewardSlot[] slots)
        {
            Entry = entry;
            Id = entry.Id;
            ParentId = entry.RewardTrackIdParent;
            MaximumPoints = slots[^1].Cost;
            Offset = offset;
            Slots = Array.AsReadOnly(slots);
        }
    }

    public sealed class LoyaltyRewardSlot
    {
        public byte Position { get; }
        public uint Cost { get; }
        public RewardTrackRewardsEntry Entry { get; }
        public uint AccountItemId { get; }
        public bool AutoClaim { get; }
        public bool ForceSoulbind { get; }

        internal LoyaltyRewardSlot(byte position, uint cost, RewardTrackRewardsEntry entry)
        {
            Position = position;
            Cost = cost;
            Entry = entry;
            AccountItemId = entry.RewardChoiceId00;
            AutoClaim = (entry.Flags & 4u) != 0;
            ForceSoulbind = (entry.Flags & 2u) != 0;
        }
    }

    public sealed record LoyaltyRewardReference(LoyaltyTrackDefinition Track, LoyaltyRewardSlot Slot);
}
