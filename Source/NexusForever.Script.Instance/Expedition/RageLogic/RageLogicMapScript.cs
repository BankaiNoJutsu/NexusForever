using NexusForever.Script.Template.Filter;

namespace NexusForever.Script.Instance.Expedition.RageLogic
{
    /// <summary>
    /// WIP-guessed map-only binding from LaughingWS Instances-and-more. The branch
    /// only supplies a phase enum stub, so vehicle/objective routing stays blocked.
    /// </summary>
    [ScriptFilterOwnerId(1627)]
    public class RageLogicMapScript : EventBaseContentMapScript
    {
        public override uint PublicEventId => 213u;
    }
}
