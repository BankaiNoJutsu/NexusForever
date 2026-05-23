using Microsoft.Extensions.Logging;
using NexusForever.Game.Abstract.Quest;
using NexusForever.Script.Template;
using NexusForever.Script.Template.Filter;

namespace NexusForever.Script.Main.Quests.Tutorial
{
    /// <summary>
    /// Dominion combat simulation quest.
    /// Quest 10524 -> grants 10526 (cryopod conversations) on completion.
    /// Objectives: kill dagun (x5), activate mines (x3), kill turrets (x2), kill final wave (x3).
    /// Entities spawned by TutorialMapScript with database-fallback checks.
    /// </summary>
    [ScriptFilterOwnerId(10524u)]
    public class Q10524CombatQuestScript : FollowUpQuestScript<Q10524CombatQuestScript>
    {
        protected override ushort NextQuestId => 10526;

        public Q10524CombatQuestScript(
            ILogger<Q10524CombatQuestScript> log,
            IGlobalQuestManager globalQuestManager)
            : base(log, globalQuestManager)
        {
        }
    }
}
