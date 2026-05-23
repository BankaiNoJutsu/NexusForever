using Microsoft.Extensions.Logging;
using NexusForever.Game.Abstract.Quest;
using NexusForever.Game.Static.Quest;
using NexusForever.Script.Template;
using NexusForever.Script.Template.Filter;

namespace NexusForever.Script.Main.Quests.CrimsonIsle
{
    /// <summary>
    /// Crimson Isle: intermediate quest available through prerequisite data after Q5604.
    /// Prerequisites (Q5604) handled by server quest system.
    /// No direct grant; Q5594 becomes available when both Q5580 and Q5583 are completed.
    /// </summary>
    [ScriptFilterOwnerId(5583u)]
    public class Q5583QuestScript : IQuestScript, IOwnedScript<IQuest>
    {
        private readonly ILogger<Q5583QuestScript> log;
        private IQuest owner;

        public Q5583QuestScript(ILogger<Q5583QuestScript> log)
        {
            this.log = log;
        }

        public void OnLoad(IQuest owner)
        {
            this.owner = owner;
            log.LogDebug("Loaded quest {QuestId} for character {CharacterId}.", owner.Id, owner.Player.CharacterId);
        }

        public void OnQuestStateChange(QuestState newState, QuestState oldState)
        {
            log.LogDebug("Quest {QuestId} state: {OldState} -> {NewState}.", owner.Id, oldState, newState);
        }
    }
}
