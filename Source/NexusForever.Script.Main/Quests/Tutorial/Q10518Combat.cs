using Microsoft.Extensions.Logging;
using NexusForever.Game.Abstract.Quest;
using NexusForever.Script.Main.Tutorial;
using NexusForever.Script.Template;
using NexusForever.Script.Template.Filter;

namespace NexusForever.Script.Main.Quests.Tutorial
{
    /// <summary>
    /// Exile combat simulation quest.
    /// Quest 10518 -> grants 10525 (cryopod conversations) on completion.
    /// Objectives: kill battle beasts (x5), activate mines (x3), kill turrets (x2), kill final wave (x3).
    /// Entities spawned by TutorialMapScript with database-fallback checks.
    /// </summary>
    [ScriptFilterOwnerId(10518u)]
    public class Q10518CombatQuestScript : FollowUpQuestScript<Q10518CombatQuestScript>
    {
        private TutorialCommunicatorSequence communicatorSequence;

        protected override ushort NextQuestId => 10525;

        private readonly IGlobalQuestManager globalQuestManager;

        public Q10518CombatQuestScript(
            ILogger<Q10518CombatQuestScript> log,
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
            communicatorSequence.TrySendWhenComplete(objective, 21286u, 7971u);
            communicatorSequence.TrySendWhenComplete(objective, 21287u, 8026u);
            communicatorSequence.TrySendWhenComplete(objective, 21340u, 8027u);
            communicatorSequence.TrySendWhenComplete(objective, 21318u, 7972u);
            communicatorSequence.TrySendWhenComplete(objective, 21288u, 8035u, 7973u);
        }
    }
}
