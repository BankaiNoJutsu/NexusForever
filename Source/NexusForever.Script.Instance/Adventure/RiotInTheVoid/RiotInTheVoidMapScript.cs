using NexusForever.Script.Template.Filter;

namespace NexusForever.Script.Instance.Adventure.RiotInTheVoid
{
    /// <summary>
    /// Build 16042 MatchingGameMap 58 maps Riot in the Void to world 1437.
    /// PE 179 is the adventure parent; PE 178 is the Running the Asylum intro.
    /// </summary>
    [ScriptFilterOwnerId(1437)]
    public class RiotInTheVoidMapScript : EventBaseContentMapScript
    {
        public override uint PublicEventId => 179u;

        protected override IEnumerable<uint> AdditionalPublicEventIds => [178u];
    }
}
