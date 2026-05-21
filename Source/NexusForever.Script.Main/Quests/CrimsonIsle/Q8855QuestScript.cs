using Microsoft.Extensions.Logging;
using NexusForever.Game.Abstract.Quest;
using NexusForever.Game.Static.Quest;
using NexusForever.Script.Template;
using NexusForever.Script.Template.Filter;

namespace NexusForever.Script.Main.Quests.CrimsonIsle
{
    /// <summary>
    /// Crimson Isle: Stasis Interrupted — approach Dominion soldiers.
    /// Quest 8855 has no direct Quest2 follow-up in build 16042.
    /// The entity script (Q8855DominionSoldiersEntityScript) handles enter-range interaction.
    /// </summary>
    [ScriptFilterOwnerId(8855u)]
    public class Q8855QuestScript : IQuestScript, IOwnedScript<IQuest>
    {
        private readonly ILogger<Q8855QuestScript> log;
        private IQuest owner;

        public Q8855QuestScript(ILogger<Q8855QuestScript> log)
        {
            this.log = log;
        }

        public void OnLoad(IQuest owner) { this.owner = owner; }
        public void OnQuestStateChange(QuestState newState, QuestState oldState)
        {
            log.LogDebug("Quest {QuestId} state: {OldState} -> {NewState}.", owner.Id, oldState, newState);
        }
    }
}
