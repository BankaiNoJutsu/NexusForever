using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Prerequisite;
using NexusForever.Game.Static.Prerequisite;

namespace NexusForever.Game.Prerequisite.Check
{
    /// <summary>
    /// Prerequisite type 117: the evaluator forwards <c>(comparison, objectId)</c> to a public event manager query on the event id (no unit, no value) and
    /// returns it for Equal or its negation for NotEqual; other comparisons are false. The query is modelled as "the event exists on the player's map and
    /// is not finished". Retail rows: ObjectId is a PublicEvent id (457 of 458), comparisons 1 and 2 only.
    /// </summary>
    [PrerequisiteCheck(PrerequisiteType.PublicEvent117)]
    public class PrerequisiteCheckPublicEvent117 : IPrerequisiteCheck
    {
        public bool Meets(IPlayer player, PrerequisiteComparison comparison, uint value, uint objectId, IPrerequisiteParameters parameters)
            => QuestObjectivePrerequisiteHelper.CompareCompletion(comparison, PublicEventPrerequisiteHelper.IsEventRunning(player, objectId));
    }

    /// <summary>
    /// Prerequisite type 118: the evaluator forwards <c>(comparison, value, objectId)</c>; the native body reads the public event objective with id
    /// <c>ObjectId</c> and compares its value for equality with <c>Value</c> (0 when the objective is not found); Equal and NotEqual only. Retail rows use Value
    /// 1 and 2 for about 97% of slots, matching <see cref="NexusForever.Game.Static.PublicEvent.PublicEventStatus"/> (Inactive 0, Active 1, Succeeded 2, Failed 3),
    /// so the objective status is compared. The native value could in principle be a count; the value distribution is what supports status.
    /// </summary>
    [PrerequisiteCheck(PrerequisiteType.PublicEventObjective188)]
    public class PrerequisiteCheckPublicEventObjective118 : IPrerequisiteCheck
    {
        public bool Meets(IPlayer player, PrerequisiteComparison comparison, uint value, uint objectId, IPrerequisiteParameters parameters)
        {
            uint status = (uint)PublicEventPrerequisiteHelper.GetObjectiveStatus(player, objectId);
            return comparison switch
            {
                PrerequisiteComparison.Equal    => status == value,
                PrerequisiteComparison.NotEqual => status != value,
                _                               => false
            };
        }
    }
}
