using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Prerequisite;
using NexusForever.Game.Static.Prerequisite;

namespace NexusForever.Game.Prerequisite.Check
{
    /// <summary>
    /// Prerequisite type 37: handler table <c>14049e350</c> rolls random percent before integer compare.
    /// </summary>
    [PrerequisiteCheck(PrerequisiteType.RandomPercent)]
    public class PrerequisiteCheckRandomPercent : IPrerequisiteCheck
    {
        public bool Meets(IPlayer player, PrerequisiteComparison comparison, uint value, uint objectId, IPrerequisiteParameters parameters)
        {
            uint roll = (uint)Random.Shared.Next(0, 100);
            return PrerequisiteCompare.Compare(comparison, roll, value);
        }
    }
}
