using Microsoft.Extensions.Logging;
using NexusForever.Game.Abstract.Quest;
using NexusForever.Game.Static.Quest;
using NexusForever.Script.Template;
using NexusForever.Script.Template.Filter;

namespace NexusForever.Script.Main.Quests.NorthernWilds
{
    /// <summary>
    /// Quest 3963 (More Important Than Revenge) - end of Shellshock! chain.
    /// Objectives: ActivateEntity targetGroup=7573 wl=45401/45402.
    /// No chain grant (end of chain).
    /// </summary>
    [ScriptFilterOwnerId(3963u)]
    public class Q3963MoreImportantThanRevengeQuestScript : IQuestScript, IOwnedScript<IQuest>
    {
        private readonly ILogger<Q3963MoreImportantThanRevengeQuestScript> log;
        private IQuest owner;

        public Q3963MoreImportantThanRevengeQuestScript(ILogger<Q3963MoreImportantThanRevengeQuestScript> log)
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
