using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Static.Entity;

namespace NexusForever.Game.Entity
{
    public static class DashEnergyRules
    {
        public const float ChargeCost = 100f;

        public static bool HasCharge(IUnitEntity unit)
        {
            return unit != null
                && unit.TryGetVitalValue(Vital.Resource7, out float dashEnergy)
                && dashEnergy + 0.0001f >= ChargeCost;
        }

        public static bool TryConsumeCharge(IUnitEntity unit, out float appliedAmount)
        {
            appliedAmount = 0f;
            if (!HasCharge(unit))
                return false;

            return unit.TryModifyVital(Vital.Resource7, -ChargeCost, out appliedAmount)
                && appliedAmount < -0.0001f;
        }
    }
}
