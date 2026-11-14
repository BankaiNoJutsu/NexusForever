using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Prerequisite;
using NexusForever.Game.Static.Prerequisite;

namespace NexusForever.Game.Prerequisite.Check
{
    /// <summary>
    /// Prerequisite type 40: native test <c>FUN_1404a4d90</c> (evaluator case <c>0x28</c>) returns false for a null unit and otherwise compares the unit's health PERCENTAGE as a float,
    /// <c>health * 100 / maxHealth</c> (virtual <c>+0x40</c> times the constant 100.0 over virtual <c>+0x48</c>), with <c>Value</c> through the float compare helper <c>FUN_1404a2010</c>.
    /// Retail: 406 slots, <c>Value</c> 50 (72), 30 (51), 25 (29), 75 (25), 10 (25), 100 (23), comparisons &lt; (100) and &lt;= (155) and &gt; and &gt;=. The previous implementation compared the raw health
    /// with <c>Value</c>. See Decomp/Analysis/PREREQUISITE_HEALTH_PERCENT_20261107.md.
    /// Verified native selector/comparison: the primary unit is retained; the float helper's unordered Equal/NotEqual use ZF. See Decomp/Analysis/PREREQUISITE_40_PRIMARY_SUBJECT_20261114.md.
    /// </summary>
    [PrerequisiteCheck(PrerequisiteType.Health)]
    public class PrerequisiteCheckHealth : IPrerequisiteCheck
    {
        public bool Meets(IPlayer player, PrerequisiteComparison comparison, uint value, uint objectId, IPrerequisiteParameters parameters)
            => MeetsUnit(player, comparison, value, objectId, parameters);

        public bool MeetsUnit(IUnitEntity subject, PrerequisiteComparison comparison, uint value, uint objectId, IPrerequisiteParameters parameters)
            => Meets(subject, comparison, value);

        private static bool Meets(IWorldEntity entity, PrerequisiteComparison comparison, uint value)
        {
            if (entity == null)
                return false;

            float percent = (float)entity.Health * 100f / (float)entity.MaxHealth;
            return PrerequisiteCompare.CompareNativeFloat(comparison, percent, (float)value);
        }
    }
}
