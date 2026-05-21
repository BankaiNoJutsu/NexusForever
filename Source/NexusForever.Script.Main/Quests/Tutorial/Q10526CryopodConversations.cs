using Microsoft.Extensions.Logging;
using NexusForever.Game.Abstract.Quest;
using NexusForever.Script.Template;
using NexusForever.Script.Template.Filter;

namespace NexusForever.Script.Main.Quests.Tutorial
{
    /// <summary>
    /// Dominion cryopod deck quest.
    /// Quest 10526 → grants 10541 on completion.
    /// Objectives: EnterZone 4965, TalkToTargetGroup 14368 & 14369, TalkTo 73663 & 73664.
    /// </summary>
    [ScriptFilterOwnerId(10526u)]
    public class Q10526CryopodConversationsQuestScript : FollowUpQuestScript<Q10526CryopodConversationsQuestScript>
    {
        protected override ushort NextQuestId => 10541;

        public Q10526CryopodConversationsQuestScript(
            ILogger<Q10526CryopodConversationsQuestScript> log,
            IGlobalQuestManager globalQuestManager)
            : base(log, globalQuestManager)
        {
        }
    }
}
