using NexusForever.Script.Template.Filter;

namespace NexusForever.Script.Instance.Dungeon.SanctuaryOfTheSwordmaiden
{
    [ScriptFilterOwnerId(1271)]
    public class SanctuaryOfTheSwordmaidenMapScript : EventBaseContentMapScript
    {
        public override uint PublicEventId => 166u;
        // Build 16042 maps Sanctuary world 1271 to the main dungeon event and
        // Spiritual Revival. Keep Spiritual Revival map-bound only; its ritual,
        // escort, and protect producers remain blocked pending client smoke.
        protected override IEnumerable<uint> AdditionalPublicEventIds => [202u];
    }
}
