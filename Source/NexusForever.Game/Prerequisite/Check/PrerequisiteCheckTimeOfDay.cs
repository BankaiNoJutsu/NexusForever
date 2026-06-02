using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Prerequisite;
using NexusForever.Game.Static.Prerequisite;

namespace NexusForever.Game.Prerequisite.Check
{
    /// <summary>
    /// Prerequisite type 29: handler table <c>Prerequisite_CheckTimeOfDay</c> (<c>14049dd10</c>) compares
    /// in-game time-of-day seconds. NF mirrors player <c>SendInGameTime</c> packet math.
    /// </summary>
    [PrerequisiteCheck(PrerequisiteType.TimeOfDay)]
    public class PrerequisiteCheckTimeOfDay : IPrerequisiteCheck
    {
        public bool Meets(IPlayer player, PrerequisiteComparison comparison, uint value, uint objectId, IPrerequisiteParameters parameters)
        {
            uint timeOfDay = InGameTimePrerequisiteHelper.GetTimeOfDaySeconds();
            return PrerequisiteCompare.Compare(comparison, timeOfDay, value);
        }
    }
}
