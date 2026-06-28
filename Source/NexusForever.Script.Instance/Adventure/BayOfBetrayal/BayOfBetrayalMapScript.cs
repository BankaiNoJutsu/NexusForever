using NexusForever.Script.Template.Filter;

namespace NexusForever.Script.Instance.Adventure.BayOfBetrayal
{
    /// <summary>
    /// Build 16042 MatchingGameMap 86 maps Veteran: Bay of Betrayal to world
    /// 3176. PE 673 is the adventure parent; PE 672 is the Herald Anku'mar intro.
    /// </summary>
    [ScriptFilterOwnerId(3176)]
    public class BayOfBetrayalMapScript : EventBaseContentMapScript
    {
        public override uint PublicEventId => 673u;

        protected override IEnumerable<uint> AdditionalPublicEventIds => [672u];
    }
}
