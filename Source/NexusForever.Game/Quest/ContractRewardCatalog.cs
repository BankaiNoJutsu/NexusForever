using System.Numerics;
using NexusForever.GameTable;
using NexusForever.GameTable.Model;

namespace NexusForever.Game.Quest
{
    /// <summary>
    /// Static Contract catalogs, including each prerequisite and Item2/AccountItem
    /// choice. Does not select a cycle, credit points, grant or consume rewards.
    /// </summary>
    public sealed class ContractRewardCatalog
    {
        private readonly Dictionary<uint, ContractRewardTrack> tracks = [];
        private readonly Dictionary<uint, ContractRewardReference> rewards = [];
        private readonly Dictionary<uint, ContractCurrencySource> currencySources = [];
        private readonly Dictionary<uint, ContractAccountItemSource> accountSources = [];
        internal ContractCurrencySource GetCurrencySource(uint id) => currencySources.GetValueOrDefault(id);
        internal ContractAccountItemSource GetAccountSource(uint id) => accountSources.GetValueOrDefault(id);
        public IReadOnlyList<ContractRewardTrack> Tracks { get; }

        public ContractRewardCatalog(IGameTableManager tables)
        {
            ArgumentNullException.ThrowIfNull(tables);
            var entries = tables.RewardTrack?.Entries ?? throw Invalid("RewardTrack table is unavailable.");
            var rows = tables.RewardTrackRewards?.Entries ?? throw Invalid("RewardTrackRewards table is unavailable.");
            var owned = entries.Where(t => t.RewardTrackTypeEnum is 0 or 1).ToArray();
            var ids = owned.Select(t => t.Id).ToHashSet();
            if (owned.GroupBy(t => t.RewardTrackTypeEnum).Any(g => g.Count() != 1)
                || entries.GroupBy(t => t.Id).Any(g => ids.Contains(g.Key) && g.Count() != 1)
                || rows.GroupBy(r => r.Id).Any(g => g.Any(r => ids.Contains(r.RewardTrackId)) && g.Count() != 1))
                throw Invalid("Contract track or reward identity is ambiguous.");

            foreach (var entry in owned.OrderBy(t => t.Id))
            {
                if (entry.Id is 0 or > 0x3FFF || entry.Flags != 0 || entry.RewardTrackIdParent != 0)
                    throw Invalid("Contract track has unsupported ownership, parent or wire identity.");
                uint[] raw = [entry.RewardPointCost00, entry.RewardPointCost01, entry.RewardPointCost02,
                    entry.RewardPointCost03, entry.RewardPointCost04, entry.RewardPointCost05,
                    entry.RewardPointCost06, entry.RewardPointCost07, entry.RewardPointCost08, entry.RewardPointCost09];
                uint[] costs = raw.TakeWhile(c => c != 0).ToArray();
                if (costs.Length == 0 || raw.Skip(costs.Length).Any(c => c != 0)
                    || !costs.SequenceEqual(costs.Order())) throw Invalid("Contract cost ordinals are invalid.");
                var buckets = costs.Select(_ => new List<ContractReward>()).ToArray();
                foreach (var row in rows.Where(r => r.RewardTrackId == entry.Id))
                {
                    uint mask = row.RewardPointFlags;
                    if (row.Id is 0 or > 0x3FFF || mask == 0 || (mask & (mask - 1)) != 0 || mask >= 1u << costs.Length)
                        throw Invalid("Contract reward has invalid wire identity or cost ordinal.");
                    byte ordinal = (byte)BitOperations.TrailingZeroCount(mask);
                    if ((row.Flags & ~3u) != 0 || (row.Flags & 2u) == 0 || ((row.Flags & 1u) != 0) != (ordinal == costs.Length - 1))
                        throw Invalid("Contract reward has unsupported flags.");
                    if ((row.CurrencyTypeId == 0) != (row.CurrencyAmount == 0)
                        || row.CurrencyTypeId != 0 && tables.CurrencyType?.GetEntry(row.CurrencyTypeId) == null)
                        throw Invalid("Contract reward has an incomplete or unknown currency source.");
                    if (row.CurrencyTypeId != 0)
                    {
                        var currency = tables.CurrencyType.GetEntry(row.CurrencyTypeId);
                        currencySources.TryAdd(row.CurrencyTypeId, new(currency.Id, currency.CapAmount));
                    }
                    uint[] types = [row.RewardTrackRewardTypeEnum00, row.RewardTrackRewardTypeEnum01, row.RewardTrackRewardTypeEnum02];
                    uint[] choiceIds = [row.RewardChoiceId00, row.RewardChoiceId01, row.RewardChoiceId02];
                    uint[] counts = [row.RewardChoiceCount00, row.RewardChoiceCount01, row.RewardChoiceCount02];
                    var choices = new List<ContractRewardChoice>(); bool ended = false;
                    for (uint index = 0; index < types.Length; ++index)
                    {
                        if (choiceIds[index] == 0 && counts[index] == 0 && types[index] == 0) { ended = true; continue; }
                        if (ended || choiceIds[index] == 0 || counts[index] == 0 || types[index] > 1
                            || (types[index] == 0 ? tables.Item?.GetEntry(choiceIds[index]) == null : tables.AccountItem?.GetEntry(choiceIds[index]) == null))
                            throw Invalid("Contract choice has unsupported type, missing data, invalid quantity or a hole.");
                        choices.Add(new(index, types[index], choiceIds[index], counts[index]));
                        if (types[index] == 1)
                        {
                            var item = tables.AccountItem.GetEntry(choiceIds[index]);
                            accountSources.TryAdd(item.Id, new(item.Id, item.Flags, item.Item2Id, item.EntitlementId, item.InstantEventEnum,
                                item.GenericUnlockSetId, item.PrerequisiteId, item.AccountItemCooldownGroupId, item.Creature2DisplayGroupIdGacha,
                                item.AccountCurrencyEnum, item.AccountCurrencyAmount, tables.AccountCurrencyType?.GetEntry(item.AccountCurrencyEnum)?.Id));
                        }
                    }
                    if (choices.Count == 0) throw Invalid("Contract reward has no choices.");
                    buckets[ordinal].Add(new(row.Id, row.PrerequisiteId, row.Flags, row.CurrencyTypeId, row.CurrencyAmount, choices.ToArray()));
                }
                if (buckets.Any(b => b.Count == 0)) throw Invalid("Contract catalog has a missing cost ordinal.");
                var track = new ContractRewardTrack(entry.Id, entry.RewardTrackTypeEnum, entry.PrerequisiteId,
                    buckets.Select((b, i) => new ContractRewardSlot((byte)i, costs[i], b.OrderBy(r => r.Id).ToArray())).ToArray());
                tracks.Add(track.Id, track);
                foreach (var slot in track.Slots) foreach (var reward in slot.Candidates) rewards.Add(reward.Id, new(track, slot, reward));
            }
            Tracks = Array.AsReadOnly(tracks.Values.OrderBy(t => t.Id).ToArray());
        }

        public ContractRewardTrack GetTrack(uint id) => tracks.GetValueOrDefault(id);
        public ContractRewardReference GetReward(uint id) => rewards.GetValueOrDefault(id);

        // The actor must capture its satisfied prerequisites. This filter does
        // not query a player/database or fall back to another class/rank/slot.
        public IReadOnlyList<ContractReward> EligibleCandidates(uint trackId, byte ordinal, IReadOnlySet<uint> satisfied)
        {
            ArgumentNullException.ThrowIfNull(satisfied);
            var track = GetTrack(trackId) ?? throw new KeyNotFoundException("Unknown Contract reward owner.");
            if (ordinal >= track.Slots.Count) throw new ArgumentOutOfRangeException(nameof(ordinal));
            if (track.PrerequisiteId != 0 && !satisfied.Contains(track.PrerequisiteId)) return Array.Empty<ContractReward>();
            return Array.AsReadOnly(track.Slots[ordinal].Candidates.Where(r => r.PrerequisiteId == 0 || satisfied.Contains(r.PrerequisiteId)).ToArray());
        }

        private static InvalidDataException Invalid(string message) => new(message);
    }

    public sealed class ContractRewardTrack
    {
        public uint Id { get; }
        public uint Type { get; }
        public uint PrerequisiteId { get; }
        public IReadOnlyList<ContractRewardSlot> Slots { get; }
        public uint MaximumPoints => Slots[^1].RequiredPoints;
        internal ContractRewardTrack(uint id, uint type, uint prerequisite, ContractRewardSlot[] slots)
        { Id = id; Type = type; PrerequisiteId = prerequisite; Slots = Array.AsReadOnly(slots); }
    }
    public sealed class ContractRewardSlot
    {
        public byte Ordinal { get; }
        public uint RequiredPoints { get; }
        public IReadOnlyList<ContractReward> Candidates { get; }
        internal ContractRewardSlot(byte ordinal, uint points, ContractReward[] candidates)
        { Ordinal = ordinal; RequiredPoints = points; Candidates = Array.AsReadOnly(candidates); }
    }
    public sealed class ContractReward
    {
        public uint Id { get; }
        public uint PrerequisiteId { get; }
        public uint Flags { get; }
        public uint CurrencyTypeId { get; }
        public uint CurrencyAmount { get; }
        public IReadOnlyList<ContractRewardChoice> Choices { get; }
        internal ContractReward(uint id, uint prerequisite, uint flags, uint currencyTypeId, uint currencyAmount, ContractRewardChoice[] choices)
        { Id = id; PrerequisiteId = prerequisite; Flags = flags; CurrencyTypeId = currencyTypeId; CurrencyAmount = currencyAmount; Choices = Array.AsReadOnly(choices); }
    }
    // Type0 identifies Item2; Type1 identifies AccountItem, not an Item2 alias.
    public sealed record ContractRewardChoice(uint Index, uint Type, uint Id, uint Count);
    public sealed record ContractRewardReference(ContractRewardTrack Track, ContractRewardSlot Slot, ContractReward Reward);
    internal sealed record ContractCurrencySource(uint Id, ulong CapAmount);
    internal sealed record ContractAccountItemSource(uint Id, uint Flags, uint Item2Id, uint EntitlementId, uint InstantEventEnum,
        uint GenericUnlockSetId, uint PrerequisiteId, uint AccountItemCooldownGroupId, uint Creature2DisplayGroupIdGacha,
        uint AccountCurrencyEnum, ulong AccountCurrencyAmount, uint? CurrencyDefinitionId);
}
