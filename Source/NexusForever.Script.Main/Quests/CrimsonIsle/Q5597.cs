using Microsoft.Extensions.Logging;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Map;
using NexusForever.Game.Abstract.Quest;
using NexusForever.Game.Static.Quest;
using NexusForever.Script.Template;
using NexusForever.Script.Template.Filter;

namespace NexusForever.Script.Main.Quests.CrimsonIsle
{
    /// <summary>
    /// Crimson Isle: intermediate quest after Q5596 merge gate.
    /// Grants Q5604 (Tactical Demolitions) on completion.
    /// </summary>
    [ScriptFilterOwnerId(5597u)]
    public class Q5597QuestScript : FollowUpQuestScript<Q5597QuestScript>
    {
        protected override ushort NextQuestId => 5604;

        public Q5597QuestScript(
            ILogger<Q5597QuestScript> log,
            IGlobalQuestManager globalQuestManager)
            : base(log, globalQuestManager)
        {
        }
    }

    public abstract class Q5597ChuaExplosivesEntityScriptBase : IWorldEntityScript
    {
        private const ushort QuestDregsAndThieves = 5597;
        private const uint ChuaExplosivesVirtualItem = 364u;
        private const uint CollectExplosivesObjective = 8256u;

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
                if (manager?.GetQuestState(QuestDregsAndThieves) != QuestState.Accepted)
                    return;
                IQuest quest = manager.GetActiveQuests()?.SingleOrDefault(active =>
                    active.Id == QuestDregsAndThieves && ReferenceEquals(active.Player, activator));
                if (quest == null || quest.PendingDelete || quest.State != QuestState.Accepted)
                    return;
                IQuestObjective objective = quest.SingleOrDefault(active => active.ObjectiveInfo.Id == CollectExplosivesObjective
                    && active.ObjectiveInfo.Type == QuestObjectiveType.VirtualCollect
                    && active.ObjectiveInfo.Entry.Data == ChuaExplosivesVirtualItem);
                if (objective == null || objective.IsComplete())
                    return;

                uint guid = current.Owner.Guid;
                IBaseMap map = current.Owner.Map;
                if (!ReferenceEquals(pickup, current) || !ReferenceEquals(activator.QuestManager, manager)
                    || !ReferenceEquals(activator.Map, map))
                    return;

                uint before = objective.Progress;
                try { manager.ObjectiveUpdate(QuestObjectiveType.VirtualCollect, ChuaExplosivesVirtualItem, 1u); }
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
            catch (Exception error) { log.Warn(error, "Q5597 explosive removal failed; retaining accepted collection for removal retry."); }
        }
    }

    [ScriptFilterCreatureId(24286u)]
    public class Q5597ChuaExplosivesEntityScript : Q5597ChuaExplosivesEntityScriptBase, IOwnedScript<ICollectableUnitEntity>
    {
        public void OnLoad(ICollectableUnitEntity owner)
        {
            OnLoad((IWorldEntity)owner);
        }
    }

    [ScriptFilterCreatureId(24286u)]
    public class Q5597ChuaExplosivesCreatureEntityScript : Q5597ChuaExplosivesEntityScriptBase, IOwnedScript<ICreatureEntity>
    {
        public void OnLoad(ICreatureEntity owner)
        {
            OnLoad((IWorldEntity)owner);
        }
    }
}
