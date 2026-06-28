using NexusForever.Script.Template.Filter;

namespace NexusForever.Script.Instance.Dungeon.Skullcano.Script
{
    [ScriptFilterOwnerId(2909)]
    public class PlatformTriggerGuidEntityScript : SkullcanoGroupObjectiveTriggerScript
    {
        // Build 16042 objective 372 is ParticipantsInTriggerVolume with
        // objectId 2909. Credit the platform gather objective directly while
        // exact platform choreography remains pending smoke.
        protected override PublicEventObjective Objective => PublicEventObjective.GatherOnThePlatform;
    }
}
