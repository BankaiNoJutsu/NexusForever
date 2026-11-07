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
            => MeetsEntity(parameters.Target ?? player, comparison, value, objectId);

        public bool MeetsUnit(IUnitEntity subject, PrerequisiteComparison comparison, uint value, uint objectId, IPrerequisiteParameters parameters)
            => MeetsEntity(subject, comparison, value, objectId);

        private bool MeetsEntity(IWorldEntity entity, PrerequisiteComparison comparison, uint value, uint objectId)
        {
            // Native (case 0x56): the movement flag word & 0x40 through the shared boolean tail; Value is not read (PREREQUISITE_FLAG_COMPARISON_20261107.md).
            return PrerequisiteCompare.CompareFlag(comparison, (entity.MovementManager.GetState() & StateFlags.Jump) != 0);
        }
    }
}
