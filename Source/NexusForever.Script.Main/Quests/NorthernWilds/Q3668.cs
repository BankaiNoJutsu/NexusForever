using Microsoft.Extensions.Logging;
using NexusForever.Game.Abstract.Quest;
using NexusForever.Game.Static.Quest;
using NexusForever.Script.Template;
using NexusForever.Script.Template.Filter;

namespace NexusForever.Script.Main.Quests.NorthernWilds
{
    /// <summary>
    /// Northern Wilds: talk to NPC, activate entities, kill target group.
    /// Quest 3668 has no direct Quest2 follow-up in build 16042.
    /// </summary>
    [ScriptFilterOwnerId(3668u)]
    public class Q3668QuestScript : IQuestScript, IOwnedScript<IQuest>
    {
        private readonly ILogger<Q3668QuestScript> log;
        private IQuest owner;

        public Q3668QuestScript(ILogger<Q3668QuestScript> log)
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
