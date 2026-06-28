using NexusForever.Script.Template.Filter;

namespace NexusForever.Script.Instance.Dungeon.Skullcano.Script
{
    [ScriptFilterOwnerId(362)]
    public class FindChiefGridTriggerEntityScript : SkullcanoGroupObjectiveTriggerScript
    {
        // Build 16042 objective 362 is ParticipantsInTriggerVolume with
        // objectId 3953; credit the intended public-event objective directly.
        // Trigger placement and Chief Kaskalak choreography still need smoke.
        protected override PublicEventObjective Objective => PublicEventObjective.FindChiefKaskalak;
    }
}
