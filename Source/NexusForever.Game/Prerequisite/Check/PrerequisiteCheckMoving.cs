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
            => MeetsEntity(parameters.Target ?? player, comparison, value, objectId);

        public bool MeetsUnit(IUnitEntity subject, PrerequisiteComparison comparison, uint value, uint objectId, IPrerequisiteParameters parameters)
            => MeetsEntity(subject, comparison, value, objectId);

        private bool MeetsEntity(IWorldEntity entity, PrerequisiteComparison comparison, uint value, uint objectId)
        {
            StateFlags state = entity.MovementManager.GetState();
            bool moving = (state & (StateFlags.Move | StateFlags.Velocity)) != 0
                || entity.MovementManager.GetVelocity().LengthSquared() > 0.01f
                || entity.MovementManager.GetMove().LengthSquared() > 0.01f;
            // Native (case 0x57): the movement flag word & 2 through the shared boolean tail; Value is not read (PREREQUISITE_FLAG_COMPARISON_20261107.md).
            return PrerequisiteCompare.CompareFlag(comparison, moving);
        }
    }
}
