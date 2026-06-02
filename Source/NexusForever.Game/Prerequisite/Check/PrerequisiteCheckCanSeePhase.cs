using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Prerequisite;
using NexusForever.Game.Static.Prerequisite;

namespace NexusForever.Game.Prerequisite.Check
{
    /// <summary>
    /// Prerequisite type 25: handler table <c>Prerequisite_CheckCanSeePhase</c> (<c>14049db90</c>).
    /// Live client gates phase visibility between caster and target; NF proxies with matching
    /// <see cref="IWorldEntity.PublicEventPhase"/> on player and evaluated target.
    /// </summary>
    [PrerequisiteCheck(PrerequisiteType.CanSeePhase)]
    public class PrerequisiteCheckCanSeePhase : IPrerequisiteCheck
    {
        public bool Meets(IPlayer player, PrerequisiteComparison comparison, uint value, uint objectId, IPrerequisiteParameters parameters)
        {
            IWorldEntity target = parameters.Target as IWorldEntity ?? player;
            uint canSee = player.PublicEventPhase == target.PublicEventPhase ? 1u : 0u;
            return PrerequisiteCompare.Compare(comparison, canSee, value);
        }
    }
}
