using NexusForever.Script.Template.Filter;

namespace NexusForever.Script.Instance.Battleground.DaggerstonePass
{
    /// <summary>
    /// WIP-guessed map-only PvP binding from LaughingWS Instances-and-more. Queue
    /// smoke, scoring, rewards, and objective semantics remain blocked.
    /// </summary>
    [ScriptFilterOwnerId(2166)]
    public class DaggerstonePassMapScript : EventBasePvpContentMapScript
    {
        public override uint PublicEventId => 438u;
        public override uint PublicSubEventId => 466u;
    }
}
