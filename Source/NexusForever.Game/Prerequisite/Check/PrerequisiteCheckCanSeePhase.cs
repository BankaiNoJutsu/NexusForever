using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Prerequisite;
using NexusForever.Game.Static.Prerequisite;

namespace NexusForever.Game.Prerequisite.Check
{
    /// <summary>
    /// Prerequisite type 25: the same native function as <see cref="PrerequisiteCheckInPhase"/> (<c>FUN_14049e640</c>): bit <c>Value &amp; 31</c> of the local player's <c>PhasesIPerceive</c>
    /// word. The client does not compare caster and target phases and ignores the target and ObjectId.
    /// </summary>
    [PrerequisiteCheck(PrerequisiteType.CanSeePhase)]
    public class PrerequisiteCheckCanSeePhase : IPrerequisiteCheck
    {
        public bool Meets(IPlayer player, PrerequisiteComparison comparison, uint value, uint objectId, IPrerequisiteParameters parameters)
            => PrerequisiteCheckInPhase.MeetsMask(player.PhasesIPerceive, comparison, value);

        public bool MeetsUnit(IUnitEntity subject, PrerequisiteComparison comparison, uint value, uint objectId, IPrerequisiteParameters parameters)
            => PrerequisiteCheckInPhase.MeetsMask(subject is IPlayer player ? player.PhasesIPerceive : PrerequisiteCheckInPhase.DefaultPhaseMask, comparison, value);
    }
}
