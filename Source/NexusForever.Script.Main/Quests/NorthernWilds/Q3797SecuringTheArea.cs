using Microsoft.Extensions.Logging;
using NexusForever.Game.Abstract.Quest;
using NexusForever.Game.Static.Quest;
using NexusForever.Script.Template;
using NexusForever.Script.Template.Filter;

namespace NexusForever.Script.Main.Quests.NorthernWilds
{
    /// <summary>
    /// Quest 3797 (Securing the Area) - side communicator quest.
    /// Objectives: KillTargetGroup tg=1177 x8.
    /// No chain grant (end of chain).
    /// </summary>
    [ScriptFilterOwnerId(3797u)]
    public class Q3797SecuringTheAreaQuestScript : IQuestScript, IOwnedScript<IQuest>
    {
        private readonly ILogger<Q3797SecuringTheAreaQuestScript> log;
        private IQuest owner;

        public Q3797SecuringTheAreaQuestScript(ILogger<Q3797SecuringTheAreaQuestScript> log)
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
