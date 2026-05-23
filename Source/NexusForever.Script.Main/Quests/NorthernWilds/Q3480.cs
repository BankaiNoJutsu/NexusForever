using Microsoft.Extensions.Logging;
using NexusForever.Game.Abstract.Quest;
using NexusForever.Game.Static.Quest;
using NexusForever.Script.Template;
using NexusForever.Script.Template.Filter;

namespace NexusForever.Script.Main.Quests.NorthernWilds
{
    /// <summary>
    /// Northern Wilds: assist settlers and kill hostile creatures.
    /// Quest 3480 grants 3667 on completion.
    /// </summary>
    [ScriptFilterOwnerId(3480u)]
    public class Q3480QuestScript : FollowUpQuestScript<Q3480QuestScript>
    {
        protected override ushort NextQuestId => 3667;

        public Q3480QuestScript(ILogger<Q3480QuestScript> log, IGlobalQuestManager globalQuestManager)
            : base(log, globalQuestManager)
        {
        }
    }
}
