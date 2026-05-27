using NexusForever.Game.Abstract.Quest;
using NexusForever.Script.Template.Filter;

namespace NexusForever.Script.Instance.Dungeon.RuinsOfKelVoreth.Script
{
    [ScriptFilterOwnerId(449)]
    public class TheExaniteForgesMessageTriggerScript : RuinsOfKelVorethMessageTriggerScriptBase
    {
        public TheExaniteForgesMessageTriggerScript(
            IGlobalQuestManager globalQuestManager)
            : base(globalQuestManager, CommunicatorMessage.AvraDarkos8)
        {
        }
    }
}
