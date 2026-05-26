using Microsoft.Extensions.Logging;
using NexusForever.Game.Abstract.Quest;
using NexusForever.Game.Static.Quest;
using NexusForever.Script.Main.Tutorial;
using NexusForever.Script.Template;
using NexusForever.Script.Template.Filter;

namespace NexusForever.Script.Main.Quests.Tutorial
{
    /// <summary>
    /// Dominion ship interior exploration quest.
    /// Quest 10523 → grants 10530 on completion.
    /// Objectives: checklist, kill creature 73499, reach cryopod zone.
    /// </summary>
    [ScriptFilterOwnerId(10523u)]
    public class Q10523ShipInteriorQuestScript : FollowUpQuestScript<Q10523ShipInteriorQuestScript>
    {
        private readonly IGlobalQuestManager globalQuestManager;
        private TutorialCommunicatorSequence communicatorSequence;

        protected override ushort NextQuestId => 10530;

        public Q10523ShipInteriorQuestScript(
            ILogger<Q10523ShipInteriorQuestScript> log,
            IGlobalQuestManager globalQuestManager)
            : base(log, globalQuestManager)
        {
            this.globalQuestManager = globalQuestManager;
        }

        public override void OnLoad(IQuest owner)
        {
            base.OnLoad(owner);
            communicatorSequence = new TutorialCommunicatorSequence(owner, globalQuestManager);

            if (owner.State == QuestState.Accepted && owner.All(o => o.Progress == 0u))
                communicatorSequence.TrySend(8058u);
        }

        public void OnObjectiveUpdate(IQuestObjective objective)
        {
            communicatorSequence.TrySendWhenComplete(objective, 21307u, 8060u);
            communicatorSequence.TrySendWhenComplete(objective, 21310u, 8061u);
        }
    }
}
