using Microsoft.Extensions.Logging;
using NexusForever.Game.Abstract.Quest;
using NexusForever.Game.Static.Quest;
using NexusForever.Script.Template;
using NexusForever.Script.Template.Filter;

namespace NexusForever.Script.Main.Quests.CrimsonIsle
{
    /// <summary>
    /// Crimson Isle: Last Resistance — use ship controls, kill warbot.
    /// Quest 5594 has no direct Quest2 follow-up in build 16042.
    /// Entity scripts (Q5594ShipControlsEntityScript, Q5594WarbotEntityScript) handle interactions.
    /// </summary>
    [ScriptFilterOwnerId(5594u)]
    public class Q5594QuestScript : IQuestScript, IOwnedScript<IQuest>
    {
        private readonly ILogger<Q5594QuestScript> log;
        private IQuest owner;

        public Q5594QuestScript(ILogger<Q5594QuestScript> log)
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
