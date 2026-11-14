using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Prerequisite;
using NexusForever.Game.Static.Entity.Movement.Command.State;
using NexusForever.Game.Static.Prerequisite;

namespace NexusForever.Game.Prerequisite.Check
{
    /// <summary>
    /// Verified native type 86: evaluator case <c>0x56</c> tests the primary unit's
    /// <c>+0x1070 &amp; 0x40</c> and fails a null primary. NF uses <see cref="StateFlags.Jump"/>.
    /// Equal returns the flag, NotEqual its negation; other comparisons fail. Value/ObjectId are unread.
    /// See Decomp/Analysis/PREREQUISITE_86_PRIMARY_SUBJECT_20261114.md.
    /// </summary>
    [PrerequisiteCheck(PrerequisiteType.Jump)]
    public class PrerequisiteCheckJump : IPrerequisiteCheck
    {
        public bool Meets(IPlayer player, PrerequisiteComparison comparison, uint value, uint objectId, IPrerequisiteParameters parameters)
            => MeetsUnit(player, comparison, value, objectId, parameters);

        public bool MeetsUnit(IUnitEntity subject, PrerequisiteComparison comparison, uint value, uint objectId, IPrerequisiteParameters parameters)
            => MeetsEntity(subject, comparison, value, objectId);

        private bool MeetsEntity(IWorldEntity entity, PrerequisiteComparison comparison, uint value, uint objectId)
        {
            if (entity == null)
                return false;

            // Native (case 0x56): the movement flag word & 0x40 through the shared boolean tail; Value is not read (PREREQUISITE_FLAG_COMPARISON_20261107.md).
            return PrerequisiteCompare.CompareFlag(comparison, (entity.MovementManager.GetState() & StateFlags.Jump) != 0);
        }
    }
}
