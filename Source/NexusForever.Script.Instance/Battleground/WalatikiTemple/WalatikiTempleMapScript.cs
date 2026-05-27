using NexusForever.Script.Template.Filter;

namespace NexusForever.Script.Instance.Battleground.WalatikiTemple
{
    /// <summary>
    /// WIP-guessed map-only PvP binding from LaughingWS WorldDatabase map-entrance
    /// rows. Queue smoke, mask scoring, capture timing, rewards, and objective
    /// semantics remain blocked.
    /// </summary>
    [ScriptFilterOwnerId(797)]
    public class WalatikiTempleMapScript : EventBasePvpContentMapScript
    {
        public override uint PublicEventId => 217u;
        public override uint PublicSubEventId => 366u;
    }
}
