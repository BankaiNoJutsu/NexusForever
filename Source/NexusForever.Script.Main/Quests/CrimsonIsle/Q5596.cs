using Microsoft.Extensions.Logging;
using NexusForever.Game.Abstract.Quest;
using NexusForever.Game.Static.Quest;
using NexusForever.Script.Template;
using NexusForever.Script.Template.Filter;

namespace NexusForever.Script.Main.Quests.CrimsonIsle
{
    /// <summary>
    /// Crimson Isle merge gate: requires BOTH Q5573 (Powering Down) and Q5575 completed.
    /// Grants Q5597 on completion. Prerequisites handled by server quest system.
    /// </summary>
    [ScriptFilterOwnerId(5596u)]
    public class Q5596QuestScript : FollowUpQuestScript<Q5596QuestScript>
    {
        protected override ushort NextQuestId => 5597;

        public Q5596QuestScript(
            ILogger<Q5596QuestScript> log,
            IGlobalQuestManager globalQuestManager)
            : base(log, globalQuestManager)
        {
        }
    }
}
