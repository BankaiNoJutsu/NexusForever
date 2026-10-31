using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Prerequisite;
using NexusForever.Game.Static.Prerequisite;

namespace NexusForever.Game.Prerequisite.Check
{
    /// <summary>
    /// Live prerequisite type 95 compares truncated squared XYZ distance to the
    /// raw threshold squared modulo uint32. See dispatcher case 0x5f at 1404a2f9d.
    /// </summary>
    [PrerequisiteCheck(PrerequisiteType.Distance)]
    public class PrerequisiteCheckDistance : IPrerequisiteCheck
    {
        public bool Meets(IPlayer player, PrerequisiteComparison comparison, uint value, uint objectId, IPrerequisiteParameters parameters)
            => MeetsUnit(player, comparison, value, objectId, parameters);

        public bool MeetsUnit(IUnitEntity subject, PrerequisiteComparison comparison, uint value, uint objectId, IPrerequisiteParameters parameters)
        {
            if (subject == null)
                return false;

            IUnitEntity target = parameters?.Target;
            if (target == null)
                return comparison == PrerequisiteComparison.NotEqual;

            float x = target.Position.X - subject.Position.X;
            float y = target.Position.Y - subject.Position.Y;
            float z = target.Position.Z - subject.Position.Z;
            float squared = x * x + y * y + z * z;

            // CVTTSS2SI r8,xmm1 truncates to int64; the comparison helper reads
            // only r8d. Invalid conversion yields int64.MinValue (low32 zero).
            // Guard explicitly because modern .NET casts saturate instead.
            uint distanceSquared = PrerequisiteCompare.TruncateNativeScalar(squared);
            return PrerequisiteCompare.Compare(comparison, distanceSquared, unchecked(value * value));
        }
    }
}
