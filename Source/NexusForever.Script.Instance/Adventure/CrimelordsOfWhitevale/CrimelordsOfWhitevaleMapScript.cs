using NexusForever.Script.Template.Filter;

namespace NexusForever.Script.Instance.Adventure.CrimelordsOfWhitevale
{
    /// <summary>
    /// Build 16042 MatchingGameMap 25 and 51 map Crimelords of Whitevale to
    /// world 1323. PE 146 is the adventure parent event.
    /// </summary>
    [ScriptFilterOwnerId(1323)]
    public class CrimelordsOfWhitevaleMapScript : EventBaseContentMapScript
    {
        public override uint PublicEventId => 146u;
    }
}
