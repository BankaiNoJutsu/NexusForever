using Microsoft.Extensions.Logging;
using NexusForever.Game.Abstract.Quest;
using NexusForever.Game.Static.Quest;
using NexusForever.Script.Template;
using NexusForever.Script.Template.Filter;

namespace NexusForever.Script.Main.Quests.CrimsonIsle
{
    /// <summary>
    /// Crimson Isle: activate entities and kill creatures.
    /// Quest 5595 has multiple retail follow-ups; do not force a direct grant.
    /// </summary>
    [ScriptFilterOwnerId(5595u)]
    public class Q5595QuestScript : IQuestScript, IOwnedScript<IQuest>
    {
        private readonly ILogger<Q5595QuestScript> log;
        private IQuest owner;

        public Q5595QuestScript(ILogger<Q5595QuestScript> log)
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
