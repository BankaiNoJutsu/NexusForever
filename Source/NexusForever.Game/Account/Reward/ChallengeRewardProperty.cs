using NexusForever.Game.Abstract.Account.Reward;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Static.Reward;
using NexusForever.GameTable.Model;
using NexusForever.Network.World.Message.Model.Reward;

namespace NexusForever.Game.Account.Reward
{
    /// <summary>
    /// Keeps the legacy raw delta API while projecting the native whole multiplier
    /// for mapped Contract/Challenge keys. Other data keys keep their old wire form.
    /// </summary>
    internal sealed class ChallengeRewardProperty(RewardPropertyEntry entry, object gate) : IRewardProperty
    {
        public RewardPropertyEntry Entry { get; } = entry;
        private RewardProperty other = new(entry);
        private readonly ContractRewardProperty[] contracts = [new(gate, 0), new(gate, 1)];
        private float? rawDelta;
        private IPlayer actor;
        private readonly Dictionary<uint, (uint SpellId, float Value)> spells = [];
        private (uint Id, float Value)[] premium = [];
        private float loyalty;
        private long revision;

        public void Initialise(IPlayer player)
        {
            lock (gate)
            {
                other = new RewardProperty(Entry);
                foreach (var contract in contracts) contract.Initialise(player);
                if (player != null && !ReferenceEquals(actor, player))
                {
                    actor = player;
                    spells.Clear();
                    rawDelta = null;
                    premium = [];
                    revision++;
                }
            }
        }

        public void SetPremium((uint Id, float Value)[] sources)
        {
            lock (gate)
            {
                if (premium.SequenceEqual(sources)) return;
                float previous = premium.Sum(row => row.Value);
                float current = sources.Sum(row => row.Value);
                if (rawDelta.HasValue || previous != 0 || current != 0)
                    rawDelta = (rawDelta ?? 0) - previous + current;
                premium = sources.ToArray();
                revision++;
            }
        }

        public void SetCommittedLoyalty(uint amount)
        {
            // Actual Entitlement44's shipped description721801 specifies25%.
            // GameFormula1268 float0 corroborates it, without proving a server formula contract.
            lock (gate)
            {
                float value = Math.Min(amount, 1u) * 0.25f;
                if (loyalty == value) return;
                loyalty = value;
                revision++;
            }
        }

        public bool UpdateSpell(IPlayer player, uint effectId, uint spellId, float value, bool removed)
        {
            lock (gate)
            {
                if (!ReferenceEquals(actor, player) || !float.IsFinite(value)) return false;
                if (removed)
                {
                    if (!spells.TryGetValue(effectId, out var previous) || previous.SpellId != spellId || previous.Value != value) return false;
                    spells.Remove(effectId);
                    rawDelta = (rawDelta ?? 0) - previous.Value;
                }
                else
                {
                    float previous = spells.TryGetValue(effectId, out var source) ? source.Value : 0;
                    if (spells.ContainsKey(effectId) && source.SpellId == spellId && previous == value) return false;
                    spells[effectId] = (spellId, value);
                    rawDelta = (rawDelta ?? 0) - previous + value;
                }
                revision++;
                return true;
            }
        }

        public void UpdateValue(uint data, float value)
        {
            lock (gate)
                if (data == 3)
                {
                    float next = (rawDelta ?? 0) + value;
                    if (!rawDelta.HasValue || !rawDelta.Value.Equals(next)) revision++;
                    rawDelta = next;
                }
                else if (data < 2) contracts[data].UpdateValue(value);
                else other.UpdateValue(data, value);
        }

        public void SetValue(uint data, float value)
        {
            lock (gate)
                if (data == 3)
                {
                    if (!rawDelta.HasValue || !rawDelta.Value.Equals(value)) revision++;
                    rawDelta = value;
                }
                else if (data < 2) contracts[data].SetValue(value);
                else other.SetValue(data, value);
        }

        public float? GetValue(uint data)
        {
            lock (gate) return data == 3 ? rawDelta : data < 2 ? contracts[data].GetValue() : other.GetValue(data);
        }

        internal void SetContractPremium(uint type, (uint Id, float Value)[] sources) => contracts[type].SetPremium(sources);
        internal bool UpdateContractSpell(IPlayer player, uint effectId, uint spellId, uint sourceEffectId, uint type, float value, bool removed, bool supported)
            => type < 2 && contracts[type].UpdateSpell(player, effectId, spellId, sourceEffectId, value, removed, supported);
        internal bool CaptureContract(IPlayer player, uint trackId, uint type, out ContractRewardModifierSnapshot snapshot)
        {
            snapshot = null;
            return type < 2 && contracts[type].IsActor(player) && contracts[type].Capture(trackId, out snapshot);
        }

        private float WholeMultiplier() => 1f + (rawDelta ?? 0f) + loyalty;

        internal ChallengeRewardModifierSnapshot CaptureModifier()
        {
            lock (gate) return new ChallengeRewardModifierSnapshot(WholeMultiplier(), revision);
        }

        public IEnumerable<ServerPremiumRewards.RewardProperty> Build()
        {
            lock (gate)
            {
                var owners = new List<ServerPremiumRewards.RewardProperty.RewardOwnerMultiplier>();
                void Add(RewardModifierOwner owner, uint id, float value)
                {
                    if (value != 0) owners.Add(new()
                    {
                        OwnerType = owner, OwnerId = id,
                        ModifierType = RewardModifierValueType.AdditiveScalar, Value = value
                    });
                }
                foreach (var source in premium) Add(RewardModifierOwner.RewardPropertyPremiumModifier, source.Id, source.Value);
                foreach (var group in spells.Values.GroupBy(source => source.SpellId).OrderBy(group => group.Key))
                    Add(RewardModifierOwner.Spell, group.Key, group.Sum(source => source.Value));
                Add(RewardModifierOwner.Entitlement, 44, loyalty);
                // Anonymous legacy deltas affect the total but carry no invented owner identity.
                return other.Build().Concat(contracts.SelectMany(contract => contract.Build())).Append(new ServerPremiumRewards.RewardProperty
                {
                    RewardPropertyId = RewardPropertyType.RewardTrackType, Data = 3,
                    ModifierType = RewardModifierValueType.AdditiveScalar,
                    Value = WholeMultiplier(), OwnerMultipliers = owners
                }).ToArray();
            }
        }
    }
}
