using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Prerequisite;
using NexusForever.Game.Static.Prerequisite;

namespace NexusForever.Game.Prerequisite.Check
{
    /// <summary>
    /// Prerequisite type 106: live <c>Prerequisite_CheckTargetEntityLookupHit</c> (<c>14049e900</c>,
    /// vtable <c>+0x318</c>). NF proxies with map entity resolution for the prerequisite target guid.
    /// </summary>
    [PrerequisiteCheck(PrerequisiteType.TargetEntityLookupHit)]
    public class PrerequisiteCheckTargetEntityLookupHit : IPrerequisiteCheck
    {
        public bool Meets(IPlayer player, PrerequisiteComparison comparison, uint value, uint objectId, IPrerequisiteParameters parameters)
        {
            if (parameters.Target is not IWorldEntity target)
                return PrerequisiteCompare.Compare(comparison, 0u, value);

            bool resolved = player.GetVisible<IWorldEntity>(target.Guid) != null;
            uint hit = resolved ? 1u : 0u;
            return PrerequisiteCompare.Compare(comparison, hit, 1u);
        }
    }
}
