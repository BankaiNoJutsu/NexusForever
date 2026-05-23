using Microsoft.Extensions.Logging;
using NexusForever.Game.Abstract.Quest;
using NexusForever.Game.Static.Quest;
using NexusForever.Script.Template;
using NexusForever.Script.Template.Filter;

namespace NexusForever.Script.Main.Quests.CrimsonIsle
{
    /// <summary>
    /// Crimson Isle: intermediate quest after Q5596 merge gate.
    /// Grants Q5604 (Tactical Demolitions) on completion.
    /// </summary>
    [ScriptFilterOwnerId(5597u)]
    public class Q5597QuestScript : FollowUpQuestScript<Q5597QuestScript>
    {
        protected override ushort NextQuestId => 5604;

        public Q5597QuestScript(
            ILogger<Q5597QuestScript> log,
            IGlobalQuestManager globalQuestManager)
            : base(log, globalQuestManager)
        {
        }
    }
}
