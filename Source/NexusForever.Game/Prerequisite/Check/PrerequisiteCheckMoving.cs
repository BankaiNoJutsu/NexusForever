using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Prerequisite;
using NexusForever.Game.Static.Entity.Movement.Command.State;
using NexusForever.Game.Static.Prerequisite;

namespace NexusForever.Game.Prerequisite.Check
{
    /// <summary>
    /// Verified native type 87: evaluator case <c>0x57</c> tests only the primary unit's
    /// <c>+0x1070 &amp; 2</c> and fails a null primary. NF uses <see cref="StateFlags.Move"/>.
    /// Equal returns the flag, NotEqual its negation; other comparisons fail. Value/ObjectId are unread.
    /// See Decomp/Analysis/PREREQUISITE_87_PRIMARY_SUBJECT_20261114.md.
    /// </summary>
    [PrerequisiteCheck(PrerequisiteType.Moving)]
    public class PrerequisiteCheckMoving : IPrerequisiteCheck
    {
        public bool Meets(IPlayer player, PrerequisiteComparison comparison, uint value, uint objectId, IPrerequisiteParameters parameters)
            => MeetsUnit(player, comparison, value, objectId, parameters);

        public bool MeetsUnit(IUnitEntity subject, PrerequisiteComparison comparison, uint value, uint objectId, IPrerequisiteParameters parameters)
            => MeetsEntity(subject, comparison, value, objectId);

        private bool MeetsEntity(IWorldEntity entity, PrerequisiteComparison comparison, uint value, uint objectId)
        {
            if (entity == null)
                return false;

            StateFlags state = entity.MovementManager.GetState();
            bool moving = (state & StateFlags.Move) != 0;
            // Native (case 0x57): the movement flag word & 2 through the shared boolean tail; Value is not read (PREREQUISITE_FLAG_COMPARISON_20261107.md).
            return PrerequisiteCompare.CompareFlag(comparison, moving);
        }
    }
}
