using NexusForever.Game.Abstract.Quest;
using NexusForever.Script.Template.Filter;

namespace NexusForever.Script.Instance.Dungeon.RuinsOfKelVoreth.Script
{
    /// <summary>
    /// WIP-guessed owner 446 message trigger. The branch also places a Forgemaster
    /// message trigger on owner 446; that duplicate remains blocked until trigger
    /// row/timing evidence proves how to split the Drokk and Forgemaster callouts.
    /// </summary>
    [ScriptFilterOwnerId(446)]
    public class DrokkMessageTriggerScript : RuinsOfKelVorethMessageTriggerScriptBase
    {
        public DrokkMessageTriggerScript(
            IGlobalQuestManager globalQuestManager)
            : base(globalQuestManager, CommunicatorMessage.AvraDarkos6, CommunicatorMessage.ToricAntevian5)
        {
        }
    }
}
