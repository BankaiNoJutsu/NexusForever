using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Map;
using NexusForever.Game.Abstract.Quest;
using NexusForever.Game.Static.Quest;
using NexusForever.Script.Template;
using NexusForever.Script.Template.Filter;

namespace NexusForever.Script.Main.Quests.NorthernWilds
{
    /// <summary>
    /// Northern Wilds: Scattered Supplies.
    /// Objective 4813 is VirtualCollect 363 from Exile Supply Crates.
    /// </summary>
    [ScriptFilterOwnerId(3741u)]
    public class Q3741ScatteredSuppliesQuestScript : IQuestScript, IOwnedScript<IQuest>
    {
        public void OnLoad(IQuest owner)
        {
        }
    }

    public abstract class Q3741ExileSupplyCrateEntityScriptBase : IWorldEntityScript
    {
        private const ushort QuestScatteredSupplies = 3741;
        private const uint ExileSuppliesVirtualItem = 363u;
        private const uint CollectSuppliesObjective = 4813u;

        private static readonly NLog.ILogger log = NLog.LogManager.GetCurrentClassLogger();
        private sealed class Pickup(IWorldEntity owner)
        {
            internal IWorldEntity Owner { get; } = owner;
            internal bool Collecting { get; set; }
            internal bool Collected { get; set; }
            internal bool RemovalRequested { get; set; }
            internal uint Guid { get; set; }
            internal IBaseMap Map { get; set; }
        }
        private Pickup pickup;

        protected void OnLoad(IWorldEntity owner)
        {
            if (!ReferenceEquals(pickup?.Owner, owner)) pickup = new(owner);
        }

        public void OnActivateSuccess(IPlayer activator)
        {
            Pickup current = pickup;
            if (current == null || current.Collecting)
                return;

            current.Collecting = true;
            try
            {
                if (current.Collected) { TryRemoveCollected(current); return; }

                IQuestManager manager = activator?.QuestManager;
                if (manager?.GetQuestState(QuestScatteredSupplies) != QuestState.Accepted)
                    return;
                IQuest quest = manager.GetActiveQuests()?.SingleOrDefault(active =>
                    active.Id == QuestScatteredSupplies && ReferenceEquals(active.Player, activator));
                if (quest == null || quest.PendingDelete || quest.State != QuestState.Accepted)
                    return;
                IQuestObjective objective = quest.SingleOrDefault(active => active.ObjectiveInfo.Id == CollectSuppliesObjective
                    && active.ObjectiveInfo.Type == QuestObjectiveType.VirtualCollect
                    && active.ObjectiveInfo.Entry.Data == ExileSuppliesVirtualItem);
                if (objective == null || objective.IsComplete())
                    return;

                uint guid = current.Owner.Guid;
                IBaseMap map = current.Owner.Map;
                if (!ReferenceEquals(pickup, current) || !ReferenceEquals(activator.QuestManager, manager)
                    || !ReferenceEquals(activator.Map, map))
                    return;

                uint before = objective.Progress;
                try { manager.ObjectiveUpdate(QuestObjectiveType.VirtualCollect, ExileSuppliesVirtualItem, 1u); }
                finally
                {
                    // An attempted update is not collection. Retain accepted credit even if later feedback throws.
                    if (objective.Progress > before)
                    {
                        current.Collected = true;
                        current.Guid = guid;
                        current.Map = map;
                        TryRemoveCollected(current);
                    }
                }
            }
            finally { current.Collecting = false; }
        }

        private static void TryRemoveCollected(Pickup current)
        {
            if (current.RemovalRequested || current.Owner.Guid != current.Guid || !ReferenceEquals(current.Owner.Map, current.Map))
                return;
            try
            {
                current.Owner.RemoveFromMap();
                current.RemovalRequested = true;
            }
            catch (Exception error) { log.Warn(error, "Q3741 supply crate removal failed; retaining accepted collection for removal retry."); }
        }
    }

    [ScriptFilterCreatureId(12919u)]
    public class Q3741ExileSupplyCrateEntityScript : Q3741ExileSupplyCrateEntityScriptBase, IOwnedScript<ICreatureEntity>
    {
        public void OnLoad(ICreatureEntity owner)
        {
            OnLoad((IWorldEntity)owner);
        }
    }

    [ScriptFilterCreatureId(12919u)]
    public class Q3741ExileSupplyCrateCollectableEntityScript : Q3741ExileSupplyCrateEntityScriptBase, IOwnedScript<ICollectableUnitEntity>
    {
        public void OnLoad(ICollectableUnitEntity owner)
        {
            OnLoad((IWorldEntity)owner);
        }
    }
}
