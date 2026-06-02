using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Entity.Creature;
using NexusForever.Game.Abstract.Prerequisite;
using NexusForever.Game.Static.Prerequisite;

namespace NexusForever.Game.Prerequisite.Check
{
    /// <summary>
    /// Prerequisite type 38: handler table <c>14049e3a0</c> reads unit scalar index <c>2</c> then integer compare.
    /// </summary>
    [PrerequisiteCheck(PrerequisiteType.IsCreature)]
    public class PrerequisiteCheckIsCreature : IPrerequisiteCheck
    {
        public bool Meets(IPlayer player, PrerequisiteComparison comparison, uint value, uint objectId, IPrerequisiteParameters parameters)
        {
            IWorldEntity entity = parameters.Target as IWorldEntity ?? player;
            uint isCreature = entity is ICreatureEntity ? 1u : 0u;
            return PrerequisiteCompare.Compare(comparison, isCreature, value);
        }
    }
}
