using System.Numerics;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Prerequisite;
using NexusForever.Game.Static.Entity.Movement.Command.State;
using NexusForever.Game.Static.Prerequisite;

namespace NexusForever.Game.Prerequisite.Check
{
    /// <summary>
    /// Prerequisite type 87: handler table <c>Prerequisite_CheckMoving</c> (<c>14049f310</c>) tests movement state list at
    /// entity <c>+0x16a0</c>. NF uses <see cref="StateFlags.Move"/> / <see cref="StateFlags.Velocity"/> and non-zero velocity/move vectors.
    /// </summary>
    [PrerequisiteCheck(PrerequisiteType.Moving)]
    public class PrerequisiteCheckMoving : IPrerequisiteCheck
    {
        public bool Meets(IPlayer player, PrerequisiteComparison comparison, uint value, uint objectId, IPrerequisiteParameters parameters)
        {
            IWorldEntity entity = parameters.Target as IWorldEntity ?? player;
            StateFlags state = entity.MovementManager.GetState();
            bool moving = (state & (StateFlags.Move | StateFlags.Velocity)) != 0
                || entity.MovementManager.GetVelocity().LengthSquared() > 0.01f
                || entity.MovementManager.GetMove().LengthSquared() > 0.01f;
            uint movingScalar = moving ? 1u : 0u;
            return PrerequisiteCompare.Compare(comparison, movingScalar, value);
        }
    }
}
