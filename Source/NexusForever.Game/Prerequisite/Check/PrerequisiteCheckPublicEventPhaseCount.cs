using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Prerequisite;
using NexusForever.Game.Abstract.PublicEvent;
using NexusForever.Game.Static.Prerequisite;

namespace NexusForever.Game.Prerequisite.Check
{
    /// <summary>
    /// Prerequisite type 120 (<c>FUN_1404a0760</c>): the evaluator forwards <c>(comparison, objectId, unit, value)</c>. The body requires the public event with id <c>ObjectId</c> to be
    /// active and then accepts any phase when <c>Value</c> is 0, otherwise requires the event's phase (virtual +0x170) to equal <c>Value</c>; Equal returns the result, NotEqual its negation,
    /// other comparisons are false. Retail rows: <c>ObjectId</c> is a PublicEvent id (41 of 42), comparison always Equal, <c>Value</c> 1 to 8.
    /// </summary>
    [PrerequisiteCheck(PrerequisiteType.PublicEvent120)]
    public class PrerequisiteCheckPublicEvent120 : IPrerequisiteCheck
    {
        public bool Meets(IPlayer player, PrerequisiteComparison comparison, uint value, uint objectId, IPrerequisiteParameters parameters)
        {
            IPublicEvent publicEvent = PublicEventPrerequisiteHelper.GetRunningEvent(player, objectId);
            bool matches = publicEvent != null && (value == 0u || publicEvent.Phase == value);
            return QuestObjectivePrerequisiteHelper.CompareCompletion(comparison, matches);
        }
    }

    /// <summary>
    /// Prerequisite type 123 (<c>FUN_1404a09d0</c>): the evaluator forwards <c>(comparison, value, objectId)</c>; the body reads the public event objective's progress count (virtual +0x98,
    /// the numerator of the type 162 percentage) and compares it with <c>Value</c> using the comparison; a missing objective has count 0. Retail rows: <c>ObjectId</c> is a PublicEventObjective
    /// id in all 184 slots, comparisons mostly greater-or-equal and equal, <c>Value</c> 0 to 5. The order of the compare helper's arguments (count versus value) is assumed from the data.
    /// </summary>
    [PrerequisiteCheck(PrerequisiteType.PublicEventObjective123)]
    public class PrerequisiteCheckPublicEventObjective123 : IPrerequisiteCheck
    {
        public bool Meets(IPlayer player, PrerequisiteComparison comparison, uint value, uint objectId, IPrerequisiteParameters parameters)
            => PrerequisiteCompare.Compare(comparison, PublicEventPrerequisiteHelper.GetObjectiveCount(player, objectId), value);
    }
}
