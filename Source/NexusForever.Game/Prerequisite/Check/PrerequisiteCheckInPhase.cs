using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Prerequisite;
using NexusForever.Game.Static.Prerequisite;

namespace NexusForever.Game.Prerequisite.Check
{
    /// <summary>
    /// Prerequisite type 24: handler table <c>Prerequisite_CheckInPhase</c> (<c>14049db50</c>) compares
    /// public-event phase on the evaluated unit. NF uses <see cref="IWorldEntity.PublicEventPhase"/>.
    /// </summary>
    [PrerequisiteCheck(PrerequisiteType.InPhase)]
    public class PrerequisiteCheckInPhase : IPrerequisiteCheck
    {
        public bool Meets(IPlayer player, PrerequisiteComparison comparison, uint value, uint objectId, IPrerequisiteParameters parameters)
        {
            IWorldEntity entity = parameters.Target as IWorldEntity ?? player;
            return PrerequisiteCompare.Compare(comparison, entity.PublicEventPhase, value);
        }
    }
}
