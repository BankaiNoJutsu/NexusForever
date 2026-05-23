using Microsoft.Extensions.Logging;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Map;
using NexusForever.Game.Abstract.Quest;
using NexusForever.Game.Static.Quest;
using NexusForever.Script.Template;
using NexusForever.Script.Template.Filter;

namespace NexusForever.Script.Main.Quests.NorthernWilds
{
    /// <summary>
    /// Quest 3667 (The Tower) -> grants 3486 (Empowered Tower) on completion.
    /// </summary>
    [ScriptFilterOwnerId(3667u)]
    public class Q3667TheTowerQuestScript : IQuestScript, IOwnedScript<IQuest>
    {
        private const ushort NextQuestId = 3486;
        private readonly ILogger<Q3667TheTowerQuestScript> log;
        private readonly IGlobalQuestManager globalQuestManager;
        private IQuest owner;

        public Q3667TheTowerQuestScript(ILogger<Q3667TheTowerQuestScript> log, IGlobalQuestManager globalQuestManager)
        {
            this.log = log; this.globalQuestManager = globalQuestManager;
        }

        public void OnLoad(IQuest owner) { this.owner = owner; }

        public void OnQuestStateChange(QuestState newState, QuestState oldState)
        {
            if (newState == QuestState.Completed)
                GrantNext();
        }

        private void GrantNext()
        {
            if (owner.Player.QuestManager.GetQuestState(NextQuestId) != null) return;
            IQuestInfo info = globalQuestManager.GetQuestInfo(NextQuestId);
            if (info == null) { log.LogWarning("Next quest {NextId} info missing.", NextQuestId); return; }
            owner.Player.QuestManager.QuestAdd(info);
            log.LogDebug("Granted follow-up quest {NextId} after {QuestId}.", NextQuestId, owner.Id);
        }
    }

    [ScriptFilterCreatureId(11194u)]
    public class Q3667ControlPanelEntityScript : IWorldEntityScript, IOwnedScript<ICreatureEntity>
    {
        private const ushort QuestTheTower  = 3667;
        private const uint QObjTerminal     = 4770u;

        private ICreatureEntity owner;

        public void OnLoad(ICreatureEntity owner)
        {
            this.owner = owner;
        }

        public void OnAddToMap(IBaseMap map)
        {
            owner.SetInRangeCheck(7f);
        }

        public void OnEnterRange(IGridEntity entity)
        {
            if (entity is not IPlayer player)
                return;

            if (player.QuestManager.GetQuestState(QuestTheTower) != QuestState.Accepted)
                return;

            player.QuestManager.ObjectiveUpdate(QObjTerminal, 1u);
        }
    }
}
