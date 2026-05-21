using Microsoft.Extensions.Logging;
using NexusForever.Game.Abstract.Quest;
using NexusForever.Game.Static.Quest;
using NexusForever.Script.Template;
using NexusForever.Script.Template.Filter;

namespace NexusForever.Script.Main.Quests.CrimsonIsle
{
    /// <summary>
    /// Crimson Isle: Venomous Intent — rescue trapped assistant.
    /// Quest 5584 has no direct Quest2 follow-up in build 16042.
    /// The entity script (Q5584TrappedAssistantEntityScript) handles the interaction.
    /// </summary>
    [ScriptFilterOwnerId(5584u)]
    public class Q5584QuestScript : IQuestScript, IOwnedScript<IQuest>
    {
        private readonly ILogger<Q5584QuestScript> log;
        private IQuest owner;

        public Q5584QuestScript(ILogger<Q5584QuestScript> log)
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
