using NexusForever.Script.Template.Filter;

namespace NexusForever.Script.Instance.Battleground.HallsOfTheBloodsworn
{
    /// <summary>
    /// WIP-guessed map-only PvP binding from LaughingWS Instances-and-more. Queue
    /// smoke, scoring, rewards, and round objective semantics remain blocked.
    /// </summary>
    [ScriptFilterOwnerId(3449)]
    public class HallsOfTheBloodswornMapScript : EventBasePvpContentMapScript
    {
        public override uint PublicEventId => 876u;
        public override uint PublicSubEventId => 877u;
    }
}
