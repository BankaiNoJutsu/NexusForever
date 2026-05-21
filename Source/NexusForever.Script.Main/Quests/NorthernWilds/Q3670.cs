using Microsoft.Extensions.Logging;
using NexusForever.Game.Abstract.Quest;
using NexusForever.Game.Static.Quest;
using NexusForever.Script.Template;
using NexusForever.Script.Template.Filter;

namespace NexusForever.Script.Main.Quests.NorthernWilds
{
    /// <summary>
    /// Northern Wilds: auto-complete transition quest.
    /// Quest 3670 has no direct Quest2 follow-up in build 16042.
    /// </summary>
    [ScriptFilterOwnerId(3670u)]
    public class Q3670QuestScript : IQuestScript, IOwnedScript<IQuest>
    {
        private readonly ILogger<Q3670QuestScript> log;
        private IQuest owner;

        public Q3670QuestScript(ILogger<Q3670QuestScript> log)
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
