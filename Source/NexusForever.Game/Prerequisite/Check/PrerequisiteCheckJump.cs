using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Prerequisite;
using NexusForever.Game.Static.Entity.Movement.Command.State;
using NexusForever.Game.Static.Prerequisite;

namespace NexusForever.Game.Prerequisite.Check
{
    /// <summary>
    /// Prerequisite type 86: handler table <c>Prerequisite_CheckJump</c> (<c>14049f190</c>) tests jump bit in movement
    /// state at entity <c>+0x16a0</c>. NF uses <see cref="StateFlags.Jump"/>.
    /// </summary>
    [PrerequisiteCheck(PrerequisiteType.Jump)]
    public class PrerequisiteCheckJump : IPrerequisiteCheck
    {
        public bool Meets(IPlayer player, PrerequisiteComparison comparison, uint value, uint objectId, IPrerequisiteParameters parameters)
        {
            IWorldEntity entity = parameters.Target as IWorldEntity ?? player;
            uint jumping = (entity.MovementManager.GetState() & StateFlags.Jump) != 0 ? 1u : 0u;
            return PrerequisiteCompare.Compare(comparison, jumping, value);
        }
    }
}
