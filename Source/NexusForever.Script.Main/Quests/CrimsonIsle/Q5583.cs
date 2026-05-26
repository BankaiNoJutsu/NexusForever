using Microsoft.Extensions.Logging;
using NexusForever.Game.Abstract.Quest;
using NexusForever.Game.Static.Quest;
using NexusForever.Script.Template;
using NexusForever.Script.Template.Filter;

namespace NexusForever.Script.Main.Quests.CrimsonIsle
{
    /// <summary>
    /// Crimson Isle: intermediate quest granted after Q5604.
    /// Q5594 becomes available when both Q5580 and Q5583 are completed.
    /// </summary>
    [ScriptFilterOwnerId(5583u)]
    public class Q5583QuestScript : IQuestScript, IOwnedScript<IQuest>
    {
        private readonly ILogger<Q5583QuestScript> log;
        private readonly IGlobalQuestManager globalQuestManager;
        private IQuest owner;

        public Q5583QuestScript(
            ILogger<Q5583QuestScript> log,
            IGlobalQuestManager globalQuestManager)
        {
            this.log                = log;
            this.globalQuestManager = globalQuestManager;
        }

        public void OnLoad(IQuest owner)
        {
            this.owner = owner;
            log.LogDebug("Loaded quest {QuestId} for character {CharacterId}.", owner.Id, owner.Player.CharacterId);
        }

        public void OnQuestStateChange(QuestState newState, QuestState oldState)
        {
            log.LogDebug("Quest {QuestId} state: {OldState} -> {NewState}.", owner.Id, oldState, newState);

            if (newState == QuestState.Completed)
            {
                CrimsonIsleQuestChain.GrantQuestIfPrerequisitesComplete(
                    owner,
                    globalQuestManager,
                    log,
                    CrimsonIsleQuestChain.Q5594LastResistance,
                    CrimsonIsleQuestChain.Q5580EnforcedRadioSilence,
                    CrimsonIsleQuestChain.Q5583HeavyArmor);
            }
        }
    }
}
