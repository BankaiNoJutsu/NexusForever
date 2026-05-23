using Microsoft.Extensions.Logging;
using NexusForever.Game.Abstract.Quest;
using NexusForever.Script.Template;
using NexusForever.Script.Template.Filter;

namespace NexusForever.Script.Main.Quests.Tutorial
{
    /// <summary>
    /// Exile combat simulation quest.
    /// Quest 10518 -> grants 10525 (cryopod conversations) on completion.
    /// Objectives: kill dagun (x5), activate mines (x3), kill turrets (x2), kill final wave (x3).
    /// Entities spawned by TutorialMapScript with database-fallback checks.
    /// </summary>
    [ScriptFilterOwnerId(10518u)]
    public class Q10518CombatQuestScript : FollowUpQuestScript<Q10518CombatQuestScript>
    {
        protected override ushort NextQuestId => 10525;

        public Q10518CombatQuestScript(
            ILogger<Q10518CombatQuestScript> log,
            IGlobalQuestManager globalQuestManager)
            : base(log, globalQuestManager)
        {
        }
    }
}
