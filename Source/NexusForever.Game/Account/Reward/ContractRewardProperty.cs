using NexusForever.Game.Abstract.Account.Reward;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Static.Reward;
using NexusForever.Network.World.Message.Model.Reward;

namespace NexusForever.Game.Account.Reward
{
    internal sealed class ContractRewardProperty(object gate, uint trackType)
    {
        private IPlayer actor;
        private float? rawDelta;
        private long revision;
        private readonly Dictionary<uint, (ContractRewardModifierSource Source, bool Supported)> spells = [];
        private (uint Id, float Value)[] premium = [];

        internal void Initialise(IPlayer player)
        {
            lock (gate)
                if (player != null && !ReferenceEquals(player, actor))
                {
                    actor = player; rawDelta = null; premium = []; spells.Clear(); revision++;
                }
        }
        internal bool IsActor(IPlayer player) { lock (gate) return player != null && ReferenceEquals(actor, player); }
        internal void SetPremium((uint Id, float Value)[] sources)
        {
            lock (gate)
            {
                if (premium.SequenceEqual(sources)) return;
                float before = premium.Sum(row => row.Value), after = sources.Sum(row => row.Value);
                if (rawDelta.HasValue || before != 0 || after != 0) rawDelta = (rawDelta ?? 0) - before + after;
                premium = sources.ToArray(); revision++;
            }
        }
        internal void UpdateValue(float value)
        {
            lock (gate)
            {
                float next = (rawDelta ?? 0) + value;
                if (!rawDelta.HasValue || !rawDelta.Value.Equals(next)) revision++;
                rawDelta = next;
            }
        }
        internal void SetValue(float value)
        {
            lock (gate) { if (!rawDelta.HasValue || !rawDelta.Value.Equals(value)) revision++; rawDelta = value; }
        }
        internal float? GetValue() { lock (gate) return rawDelta; }
        internal bool UpdateSpell(IPlayer player, uint effectId, uint spellId, uint sourceEffectId, float value, bool removed, bool supported)
        {
            lock (gate)
            {
                if (!ReferenceEquals(actor, player) || !float.IsFinite(value)) return false;
                if (removed)
                {
                    if (!spells.TryGetValue(effectId, out var previous) || previous.Source.SpellId != spellId
                        || previous.Source.SourceEffectId != sourceEffectId || previous.Source.Value != value) return false;
                    spells.Remove(effectId); rawDelta = (rawDelta ?? 0) - previous.Source.Value;
                }
                else
                {
                    var source = new ContractRewardModifierSource(effectId, spellId, sourceEffectId, value);
                    if (spells.TryGetValue(effectId, out var previous) && previous.Source == source && previous.Supported == supported) return false;
                    rawDelta = (rawDelta ?? 0) - (previous.Source?.Value ?? 0) + value;
                    spells[effectId] = (source, supported);
                }
                revision++; return true;
            }
        }
        internal bool Capture(uint trackId, out ContractRewardModifierSnapshot snapshot)
        {
            lock (gate)
            {
                snapshot = null;
                // The actual Contract dataset has no premium source. Unknown
                // premiums/anonymous deltas retain their API/wire behavior but
                // cannot become a newly assumed Contract credit policy.
                float owned = spells.Values.Sum(row => row.Source.Value);
                if (spells.Values.Any(row => !row.Supported) || premium.Any(row => row.Value != 0)
                    || !float.IsFinite(rawDelta ?? 0) || (rawDelta ?? 0) != owned) return false;
                snapshot = new(trackId, trackType, 1f + (rawDelta ?? 0), revision,
                    spells.OrderBy(row => row.Key).Select(row => row.Value.Source)); return true;
            }
        }
        internal IEnumerable<ServerPremiumRewards.RewardProperty> Build()
        {
            lock (gate)
            {
                if (!rawDelta.HasValue) return [];
                var owners = new List<ServerPremiumRewards.RewardProperty.RewardOwnerMultiplier>();
                foreach (var source in premium.Where(row => row.Value != 0)) owners.Add(new()
                    { OwnerType = RewardModifierOwner.RewardPropertyPremiumModifier, OwnerId = source.Id, ModifierType = RewardModifierValueType.AdditiveScalar, Value = source.Value });
                foreach (var source in spells.Values.GroupBy(row => row.Source.SpellId).OrderBy(row => row.Key))
                {
                    float value = source.Sum(row => row.Source.Value);
                    if (value != 0) owners.Add(new() { OwnerType = RewardModifierOwner.Spell, OwnerId = source.Key, ModifierType = RewardModifierValueType.AdditiveScalar, Value = value });
                }
                return [new() { RewardPropertyId = RewardPropertyType.RewardTrackType, Data = trackType,
                    ModifierType = RewardModifierValueType.AdditiveScalar, Value = 1f + rawDelta.Value, OwnerMultipliers = owners }];
            }
        }
    }
}
