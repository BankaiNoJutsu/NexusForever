using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Prerequisite;
using NexusForever.Game.Static.Prerequisite;
using NexusForever.Game.Static.PublicEvent;

namespace NexusForever.Game.Prerequisite.Check
{
    /// <summary>
    /// Prerequisite type 199 (<c>FUN_1404a0b70</c>, 14 retail slots): <c>ObjectId</c> is a PublicEvent id and the flag is true when the client's event object exists and its field
    /// <c>+0x1d0</c> is 1. That field is the Reason of the last <c>ServerPublicEventEnd</c> the client received for the event (default 2; written only by the event's init and by the End
    /// handler <c>FUN_1405f30a0</c>; a Leave packet never touches it and it lives for the client session). Equal returns the flag, NotEqual its negation, every other comparison is
    /// false; Value is not read. The server keeps the same per-event last reason on the player (<see cref="IPlayer.GetPublicEventEndReason"/>), recorded when the End is delivered.
    /// Inferred, not native: that retail's own server evaluated this with the same per-player last-result rule. See Decomp/Analysis/PUBLIC_EVENT_NATIVE_READ_20261108.md.
    /// </summary>
    [PrerequisiteCheck(PrerequisiteType.PublicEvent199)]
    public class PrerequisiteCheckPublicEvent199 : IPrerequisiteCheck
    {
        public bool Meets(IPlayer player, PrerequisiteComparison comparison, uint value, uint objectId, IPrerequisiteParameters parameters)
            => PrerequisiteCompare.CompareFlag(comparison, player.GetPublicEventEndReason(objectId) == PublicEventRemoveReason.Success);
    }
}
