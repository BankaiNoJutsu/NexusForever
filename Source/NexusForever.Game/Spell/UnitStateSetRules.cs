using NexusForever.Game.Abstract.Entity;

namespace NexusForever.Game.Spell
{
    public static class UnitStateSetRules
    {
        public const uint Block = 1u;
        public const uint Invulnerable = 6u;
        public const uint AllSpellImmunity0 = 7u;
        public const uint AllSpellImmunity1 = 8u;
        public const uint AllSpellImmunity2 = 9u;
        public const uint ImmunityShield0 = 13u;
        public const uint ImmunityShield1 = 14u;
        public const uint ImmunityShield2 = 15u;
        public const uint InvulnerabilityShield0 = 16u;
        public const uint InvulnerabilityShield1 = 17u;
        public const uint InvulnerabilityShield2 = 18u;
        public const uint Barrier = 22u;
        public const uint BurrowOrIceBlock = 23u;

        private static readonly uint[] hostileEffectImmuneStateIds =
        [
            Invulnerable,
            AllSpellImmunity0,
            AllSpellImmunity1,
            AllSpellImmunity2,
            ImmunityShield0,
            ImmunityShield1,
            ImmunityShield2,
            InvulnerabilityShield0,
            InvulnerabilityShield1,
            InvulnerabilityShield2,
            Barrier,
            BurrowOrIceBlock
        ];

        public static bool BlocksHostileEffects(uint stateId)
        {
            return stateId is Invulnerable
                or AllSpellImmunity0
                or AllSpellImmunity1
                or AllSpellImmunity2
                or ImmunityShield0
                or ImmunityShield1
                or ImmunityShield2
                or InvulnerabilityShield0
                or InvulnerabilityShield1
                or InvulnerabilityShield2
                or Barrier
                or BurrowOrIceBlock;
        }

        public static bool TryGetHostileEffectImmuneState(IUnitEntity target, out uint stateId)
        {
            foreach (uint candidate in hostileEffectImmuneStateIds)
            {
                if (!target.HasUnitState(candidate))
                    continue;

                stateId = candidate;
                return true;
            }

            stateId = 0u;
            return false;
        }

        public static string DescribeState(uint stateId)
        {
            return stateId switch
            {
                Block => "Block",
                Invulnerable => "Invulnerable",
                AllSpellImmunity0 => "AllSpellImmunity0",
                AllSpellImmunity1 => "AllSpellImmunity1",
                AllSpellImmunity2 => "AllSpellImmunity2",
                ImmunityShield0 => "ImmunityShield0",
                ImmunityShield1 => "ImmunityShield1",
                ImmunityShield2 => "ImmunityShield2",
                InvulnerabilityShield0 => "InvulnerabilityShield0",
                InvulnerabilityShield1 => "InvulnerabilityShield1",
                InvulnerabilityShield2 => "InvulnerabilityShield2",
                Barrier => "Barrier",
                BurrowOrIceBlock => "BurrowOrIceBlock",
                _ => null
            };
        }
    }
}