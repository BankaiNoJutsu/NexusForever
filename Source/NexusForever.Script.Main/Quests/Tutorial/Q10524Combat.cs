using Microsoft.Extensions.Logging;
using NexusForever.Game.Abstract.Quest;
using NexusForever.Script.Main.Tutorial;
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
        private TutorialCommunicatorSequence communicatorSequence;

        protected override ushort NextQuestId => 10526;

        private readonly IGlobalQuestManager globalQuestManager;

        public Q10524CombatQuestScript(
            ILogger<Q10524CombatQuestScript> log,
            IGlobalQuestManager globalQuestManager)
            : base(log, globalQuestManager)
        {
            this.globalQuestManager = globalQuestManager;
        }

        public override void OnLoad(IQuest owner)
        {
            base.OnLoad(owner);
            communicatorSequence = new TutorialCommunicatorSequence(owner, globalQuestManager);
        }

        public void OnObjectiveUpdate(IQuestObjective objective)
        {
            communicatorSequence.TrySendWhenComplete(objective, 21312u, 8006u);
            communicatorSequence.TrySendWhenComplete(objective, 21313u, 8010u);
            communicatorSequence.TrySendWhenComplete(objective, 21314u, 8011u);
            communicatorSequence.TrySendWhenComplete(objective, 21315u, 8018u);
            communicatorSequence.TrySendWhenComplete(objective, 21316u, 8033u, 8034u);
        }
    }
}
