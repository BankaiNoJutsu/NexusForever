using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Map;
using NexusForever.Game.Abstract.Quest;
using NexusForever.Game.Static.Quest;
using NexusForever.Script.Template;
using NexusForever.Script.Template.Filter;

namespace NexusForever.Script.Main.Quests.NorthernWilds
{
    /// <summary>
    /// Northern Wilds: Spatial Anomaly.
    /// </summary>
    [ScriptFilterOwnerId(4526u)]
    public class Q4526SpatialAnomalyQuestScript : IQuestScript, IOwnedScript<IQuest>
    {
        public void OnLoad(IQuest owner)
        {
        }
    }

    [ScriptFilterCreatureId(12535u)]
    public class Q4526SpatialAnomalyEntityScript : IWorldEntityScript, IOwnedScript<ICreatureEntity>
    {
        private const ushort QuestSpatialAnomaly = 4526;
        private const uint CatchSpatialAnomalyObjective = 6165u;
        private const float CatchRange = 5f;

        private ICreatureEntity owner;

        public void OnLoad(ICreatureEntity owner)
        {
            this.owner = owner;
        }

        public void OnAddToMap(IBaseMap map)
        {
            owner.SetInRangeCheck(CatchRange);
        }

        public void OnEnterRange(IGridEntity entity)
        {
            if (entity is not IPlayer player)
                return;

            TryCreditCatch(player);
        }

        public void OnActivateSuccess(IPlayer activator)
        {
            TryCreditCatch(activator);
        }

        private void TryCreditCatch(IPlayer player)
        {
            IQuestManager manager = player?.QuestManager;
            if (manager?.GetQuestState(QuestSpatialAnomaly) != QuestState.Accepted)
                return;

            IQuest quest = manager.GetActiveQuests()?.SingleOrDefault(active =>
                active.Id == QuestSpatialAnomaly && ReferenceEquals(active.Player, player));
            if (quest == null || quest.State != QuestState.Accepted
                || quest.GetCurrentObjectiveId() != CatchSpatialAnomalyObjective)
                return;

            IQuestObjective objective = quest.SingleOrDefault(active =>
                active.ObjectiveInfo.Id == CatchSpatialAnomalyObjective
                && active.ObjectiveInfo.Type == QuestObjectiveType.ActivateEntity);
            if (objective == null || objective.IsComplete() || !ReferenceEquals(player.QuestManager, manager))
                return;

            // WIP/GUESSED: client rows prove the anomaly target and catch location, but exact movement/catch timing still needs client smoke.
            // The real objective owns sequential gating and repeated credit; a refused early catch must remain retryable.
            quest.ObjectiveUpdate(CatchSpatialAnomalyObjective, 1u);
        }
    }
}
