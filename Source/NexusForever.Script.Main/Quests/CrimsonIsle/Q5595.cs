using Microsoft.Extensions.Logging;
using NexusForever.Game.Abstract.Quest;
using NexusForever.Game.Static.Quest;
using NexusForever.Script.Template;
using NexusForever.Script.Template.Filter;

namespace NexusForever.Script.Main.Quests.CrimsonIsle
{
    /// <summary>
    /// Crimson Isle: activate entities and kill creatures.
    /// Quest 5595 grants 5575 on completion.
    /// Retail chain: 5595 (root) -> 5575 -> [merge gate 5596] -> 5597 -> 5604 -> 5580 -> [merge gate 5594].
    /// </summary>
    [ScriptFilterOwnerId(5595u)]
    public class Q5595QuestScript : FollowUpQuestScript<Q5595QuestScript>
    {
        protected override ushort NextQuestId => 5575;

        public Q5595QuestScript(
            ILogger<Q5595QuestScript> log,
            IGlobalQuestManager globalQuestManager)
            : base(log, globalQuestManager)
        {
        }
    }
}
