using NexusForever.Script.Template.Filter;

namespace NexusForever.Script.Instance.Raid.RedMoonTerror.FortyMan
{
    /// <summary>
    /// Build 16042 keeps world 3102 as a raid-typed RedMoonTerror40man map with
    /// public event 650. This is tracked separately from the released 3032/705
    /// Red Moon Terror scaffold.
    /// </summary>
    [ScriptFilterOwnerId(3102)]
    public class RedMoonTerror40ManMapScript : EventBaseContentMapScript
    {
        public override uint PublicEventId => 650u;
    }
}
