using NexusForever.Script.Template.Filter;

namespace NexusForever.Script.Instance.Adventure.TheHycrestInsurrection
{
    /// <summary>
    /// Build 16042 MatchingGameMap 57 maps The Hycrest Insurrection to world 1149.
    /// PE 419 is the adventure parent; PE 418 is the drop-ship intro.
    /// </summary>
    [ScriptFilterOwnerId(1149)]
    public class TheHycrestInsurrectionMapScript : EventBaseContentMapScript
    {
        public override uint PublicEventId => 419u;

        protected override IEnumerable<uint> AdditionalPublicEventIds => [418u];
    }
}
