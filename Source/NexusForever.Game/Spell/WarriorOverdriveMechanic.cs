using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Static.Entity;

namespace NexusForever.Game.Spell
{
    internal static class WarriorOverdriveMechanic
    {
        private const uint OverdriveBuffSpell4Id = 42908u;

        public static bool FreezesKineticVital(IUnitEntity unit, Vital vital)
        {
            return unit != null
                && IsKineticVital(vital)
                && unit.HasTrackedSpellState(OverdriveBuffSpell4Id);
        }

        private static bool IsKineticVital(Vital vital)
        {
            return vital is Vital.KineticCell or Vital.Resource1;
        }
    }
}
