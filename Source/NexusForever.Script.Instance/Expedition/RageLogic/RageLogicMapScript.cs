using NexusForever.Script.Template.Filter;

namespace NexusForever.Script.Instance.Expedition.RageLogic
{
    /// <summary>
    /// Build 16042 Rage Logic has a vehicle-choice public event followed by the
    /// reward-bearing expedition public event. Keep the main event as the match
    /// finisher and join players to the vehicle-choice side event as well.
    /// </summary>
    [ScriptFilterOwnerId(1627)]
    public class RageLogicMapScript : EventBaseContentMapScript
    {
        public override uint PublicEventId => 214u;
        protected override IEnumerable<uint> AdditionalPublicEventIds => [213u];
    }
}
