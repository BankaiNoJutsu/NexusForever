using NexusForever.Game.Abstract.Quest;
using NexusForever.Script.Template.Filter;

namespace NexusForever.Script.Instance.Dungeon.RuinsOfKelVoreth.Script
{
    [ScriptFilterOwnerId(847)]
    public class DigsiteScarMessageTriggerScript : RuinsOfKelVorethMessageTriggerScriptBase
    {
        public DigsiteScarMessageTriggerScript(
            IGlobalQuestManager globalQuestManager)
            : base(globalQuestManager, CommunicatorMessage.AvraDarkos5)
        {
        }
    }
}
