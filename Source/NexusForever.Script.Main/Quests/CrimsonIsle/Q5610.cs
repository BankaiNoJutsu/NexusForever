using Microsoft.Extensions.Logging;
using NexusForever.Game.Abstract.Quest;
using NexusForever.Game.Static.Quest;
using NexusForever.Script.Template;
using NexusForever.Script.Template.Filter;

namespace NexusForever.Script.Main.Quests.CrimsonIsle
{
    /// <summary>
    /// Crimson Isle: auto-complete terminal quest.
    /// Quest 5610 — final CI quest, no follow-up.
    /// </summary>
    [ScriptFilterOwnerId(5610u)]
    public class Q5610QuestScript : IQuestScript, IOwnedScript<IQuest>
    {
        private readonly ILogger<Q5610QuestScript> log;
        private IQuest owner;

        public Q5610QuestScript(ILogger<Q5610QuestScript> log) { this.log = log; }

        public void OnLoad(IQuest owner) { this.owner = owner; }
        public void OnQuestStateChange(QuestState newState, QuestState oldState)
        {
            log.LogDebug("Quest 5610 completed for character {CharacterId} — CI zone done.", owner.Player.CharacterId);
        }
    }
}
