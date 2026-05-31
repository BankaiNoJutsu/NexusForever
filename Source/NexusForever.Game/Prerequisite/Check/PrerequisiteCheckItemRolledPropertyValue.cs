using System;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Prerequisite;
using NexusForever.Game.Static.Entity;
using NexusForever.Game.Static.Prerequisite;

namespace NexusForever.Game.Prerequisite.Check
{
    [PrerequisiteCheck(PrerequisiteType.ItemRolledPropertyValue)]
    public class PrerequisiteCheckItemRolledPropertyValue : IPrerequisiteCheck
    {
        public bool Meets(IPlayer player, PrerequisiteComparison comparison, uint value, uint objectId, IPrerequisiteParameters parameters)
        {
            if (parameters.Item == null)
                return false;

            // NF proxy: client scans 15 rolled Property slots on the item-eval blob (+0x94/+0xd0).
            // Until per-instance rolled stats are stored, compare template-calculated magnitudes.
            if (!parameters.Item.Info.Properties.TryGetValue((Property)objectId, out float magnitude))
                return EvaluateMissingSlot(comparison, value);

            uint magnitudeBits = unchecked((uint)BitConverter.SingleToInt32Bits(magnitude));
            return EvaluatePresentSlot(comparison, value, magnitudeBits);
        }

        private static bool EvaluatePresentSlot(PrerequisiteComparison comparison, uint expected, uint magnitudeBits)
        {
            return comparison switch
            {
                PrerequisiteComparison.Equal              => magnitudeBits == expected,
                PrerequisiteComparison.NotEqual           => magnitudeBits != expected,
                PrerequisiteComparison.GreaterThanOrEqual => magnitudeBits >= expected,
                PrerequisiteComparison.GreaterThan        => magnitudeBits > expected,
                PrerequisiteComparison.LessThanOrEqual    => magnitudeBits <= expected,
                PrerequisiteComparison.LessThan           => magnitudeBits < expected,
                _                                         => false
            };
        }

        private static bool EvaluateMissingSlot(PrerequisiteComparison comparison, uint expected)
        {
            return comparison switch
            {
                PrerequisiteComparison.Equal              => expected == 0,
                PrerequisiteComparison.NotEqual           => expected != 0,
                PrerequisiteComparison.GreaterThanOrEqual => expected == 0,
                PrerequisiteComparison.LessThanOrEqual    => true,
                PrerequisiteComparison.LessThan           => expected != 0,
                _                                         => false
            };
        }
    }
}
