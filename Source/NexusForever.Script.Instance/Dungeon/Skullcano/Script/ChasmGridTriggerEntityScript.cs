using NexusForever.Script.Template.Filter;

namespace NexusForever.Script.Instance.Dungeon.Skullcano.Script
{
    [ScriptFilterOwnerId(2821)]
    public class ChasmGridTriggerEntityScript : SkullcanoGroupObjectiveTriggerScript
    {
        // Build 16042 objective 329 is the chasm Turnstile row for objectId
        // 2821. Credit the active chasm objective directly so the escort row
        // with the same objectId is not touched by this trigger.
        protected override PublicEventObjective Objective => PublicEventObjective.CrossTheLavaFilledChasm;
    }
}
