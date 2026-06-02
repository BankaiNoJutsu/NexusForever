using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Prerequisite;
using NexusForever.Game.Static.Prerequisite;

namespace NexusForever.Game.Prerequisite.Check
{
    /// <summary>
    /// Prerequisite type 39: handler table <c>14049e3f0</c> reads unit scalar index <c>3</c> then integer compare.
    /// </summary>
    [PrerequisiteCheck(PrerequisiteType.IsPlayer)]
    public class PrerequisiteCheckIsPlayer : IPrerequisiteCheck
    {
        public bool Meets(IPlayer player, PrerequisiteComparison comparison, uint value, uint objectId, IPrerequisiteParameters parameters)
        {
            IWorldEntity entity = parameters.Target as IWorldEntity ?? player;
            uint isPlayer = entity is IPlayer ? 1u : 0u;
            return PrerequisiteCompare.Compare(comparison, isPlayer, value);
        }
    }
}
