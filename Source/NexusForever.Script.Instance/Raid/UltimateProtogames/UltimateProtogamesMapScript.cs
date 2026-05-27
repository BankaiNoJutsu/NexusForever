using NexusForever.Script.Template.Filter;

namespace NexusForever.Script.Instance.Raid.UltimateProtogames
{
    /// <summary>
    /// WIP-guessed map-only binding from LaughingWS Instances-and-more. The branch
    /// supplies objective ids but no event script to justify raid objective routing.
    /// </summary>
    [ScriptFilterOwnerId(3041)]
    public class UltimateProtogamesMapScript : EventBaseContentMapScript
    {
        public override uint PublicEventId => 642u;
    }
}
