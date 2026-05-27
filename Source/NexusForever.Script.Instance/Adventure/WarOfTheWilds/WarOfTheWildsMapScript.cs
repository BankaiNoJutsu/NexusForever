using NexusForever.Script.Template.Filter;

namespace NexusForever.Script.Instance.Adventure.WarOfTheWilds
{
    /// <summary>
    /// WIP-guessed map binding from LaughingWS Instances-and-more. The branch maps
    /// the base public event, while faction start events and route timing stay blocked.
    /// </summary>
    [ScriptFilterOwnerId(1393)]
    public class WarOfTheWildsMapScript : EventBaseContentMapScript
    {
        public override uint PublicEventId => 158u;
    }
}
