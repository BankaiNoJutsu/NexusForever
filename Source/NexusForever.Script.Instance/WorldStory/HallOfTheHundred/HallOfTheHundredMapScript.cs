using NexusForever.Script.Template.Filter;

namespace NexusForever.Script.Instance.WorldStory.HallOfTheHundred
{
    [ScriptFilterOwnerId(3009)]
    public class HallOfTheHundredMapScript : EventBaseContentMapScript
    {
        public override uint PublicEventId => 666u;
        // Build 16042 maps Vault of the Archon world 3009 to these public
        // events. Only 677/678/693/696 currently have scripted lifecycle
        // coverage; 668/669/874/875 are map-bound so live validation can prove
        // or reject their protect, medal, reward, and escape semantics without
        // guessing producers here.
        protected override IEnumerable<uint> AdditionalPublicEventIds => [668u, 669u, 677u, 678u, 693u, 696u, 874u, 875u];
    }
}
