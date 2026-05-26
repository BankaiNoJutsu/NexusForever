using Microsoft.Extensions.Logging;
using NexusForever.Game.Abstract.Quest;
using NexusForever.Game.Static.Quest;
using NexusForever.Script.Main.Tutorial;
using NexusForever.Script.Template;
using NexusForever.Script.Template.Filter;

namespace NexusForever.Script.Main.Quests.Tutorial
{
    /// <summary>
    /// Exile ship interior exploration quest.
    /// Quest 10520 → grants 10528 on completion.
    /// Objectives: checklist, kill creature 73499, reach cryopod zone.
    /// </summary>
    [ScriptFilterOwnerId(10520u)]
    public class Q10520ShipInteriorQuestScript : FollowUpQuestScript<Q10520ShipInteriorQuestScript>
    {
        private readonly IGlobalQuestManager globalQuestManager;
        private TutorialCommunicatorSequence communicatorSequence;

        protected override ushort NextQuestId => 10528;

        public Q10520ShipInteriorQuestScript(
            ILogger<Q10520ShipInteriorQuestScript> log,
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
                communicatorSequence.TrySend(7985u);
        }

        public void OnObjectiveUpdate(IQuestObjective objective)
        {
            communicatorSequence.TrySendWhenComplete(objective, 21294u, 7987u);
            communicatorSequence.TrySendWhenComplete(objective, 21296u, 7992u);
        }
    }
}
