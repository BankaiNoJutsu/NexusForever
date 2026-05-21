using Microsoft.Extensions.Logging;
using NexusForever.Game.Abstract.Quest;
using NexusForever.Script.Template;
using NexusForever.Script.Template.Filter;

namespace NexusForever.Script.Main.Quests.Tutorial
{
    /// <summary>
    /// Exile cryopod deck quest.
    /// Quest 10525 → grants 10540 on completion.
    /// Objectives: EnterZone 4965, TalkToTargetGroup 14368 & 14369, TalkTo 73663 & 73664.
    /// </summary>
    [ScriptFilterOwnerId(10525u)]
    public class Q10525CryopodConversationsQuestScript : FollowUpQuestScript<Q10525CryopodConversationsQuestScript>
    {
        protected override ushort NextQuestId => 10540;

        public Q10525CryopodConversationsQuestScript(
            ILogger<Q10525CryopodConversationsQuestScript> log,
            IGlobalQuestManager globalQuestManager)
            : base(log, globalQuestManager)
        {
        }
    }
}
