using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Spell;
using NexusForever.Game.Spell.Effect;
using NexusForever.Game.Static.Spell;
using NexusForever.GameTable.Model;
using NexusForever.Network.World.Combat;

namespace NexusForever.Game.Spell
{
    public class SpellTargetInfo : ISpellTargetInfo
    {
        public class SpellTargetEffectInfo : ISpellTargetEffectInfo
        {
            public class DamageDescription : IDamageDescription
            {
                public DamageType DamageType { get; set; }
                public uint RawDamage { get; set; }
                public uint RawScaledDamage { get; set; }
                public uint AbsorbedAmount { get; set; }
                public uint ShieldAbsorbAmount { get; set; }
                public uint AdjustedDamage { get; set; }
                public uint OverkillAmount { get; set; }
                public float ThreatMultiplier { get; set; } = 1f;
                public bool KilledTarget { get; set; }
                public CombatResult CombatResult { get; set; }
            }

            public uint EffectId { get; }
            public bool DropEffect { get; set; } = false;
            public bool LifetimeEnded { get; set; }
            public Spell4EffectsEntry Entry { get; }
            public SpellEffectInterpretation Interpretation { get; }
            public IDamageDescription Damage { get; private set; }
            public List<ICombatLog> CombatLogs { get; private set; } = [];
            public IReadOnlyCollection<IGridEntity> CreatedEntities => createdEntities;

            private readonly List<IGridEntity> createdEntities = [];

            public SpellTargetEffectInfo(uint effectId, Spell4EffectsEntry entry)
            {
                EffectId       = effectId;
                Entry          = entry;
                Interpretation = SpellEffectInterpreter.Interpret(entry);
            }

            public void AddDamage(DamageType damageType, uint damage)
            {
                Damage = new DamageDescription
                {
                    DamageType      = damageType,
                    RawDamage       = damage,
                    RawScaledDamage = damage,
                    AdjustedDamage  = damage,
                    ThreatMultiplier = 1f,
                    CombatResult    = CombatResult.Hit
                };
            }

            public void AddDamage(IDamageDescription damage)
            {
                Damage = damage;
            }

            public void AddCombatLog(ICombatLog combatLog)
            {
                CombatLogs.Add(combatLog);
            }

            public void AddCreatedEntity(IGridEntity entity)
            {
                createdEntities.Add(entity);
            }
        }

        public SpellEffectTargetFlags Flags { get; private set; }
        public IWorldEntity Entity { get; }
        public List<ISpellTargetEffectInfo> Effects { get; } = new List<ISpellTargetEffectInfo>();

        public SpellTargetInfo(SpellEffectTargetFlags flags, IWorldEntity entity)
        {
            Flags  = flags;
            Entity = entity;
        }

        public void AddFlags(SpellEffectTargetFlags flags)
        {
            Flags |= flags;
        }
    }
}
