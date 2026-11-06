using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Map;
using NexusForever.Game.Abstract.Quest;
using NexusForever.Game.Static.Entity;
using NexusForever.Network.World.Message.Static;
using NexusForever.Game.Static.Quest;
using NexusForever.Script.Template;
using NexusForever.Script.Template.Filter;

namespace NexusForever.Script.Main.Quests.NorthernWilds
{
    /// <summary>
    /// Northern Wilds: By Leaps and Bounds.
    /// Objective 5076 is the first pure-loftite jump-through using ActivateEntity data 6952,
    /// and objective 4859 tracks collected fragments through item 6998.
    /// </summary>
    [ScriptFilterOwnerId(3777u)]
    public class Q3777ByLeapsAndBoundsQuestScript : IQuestScript, IOwnedScript<IQuest>
    {
        public void OnLoad(IQuest owner)
        {
        }
    }

    public abstract class Q3777LoftiteCrystalEntityScriptBase : IWorldEntityScript
    {
        private const ushort QuestByLeapsAndBounds = 3777;
        private const uint FirstFragmentObjective = 5076u;
        private const uint FragmentCollectionObjective = 4859u;
        private const uint PureLoftiteFragmentItem = 6998u;

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

        public void OnAddToMap(IBaseMap map)
        {
            pickup?.Owner.SetInRangeCheck(5f);
        }

        public void OnEnterRange(IGridEntity entity)
        {
            Pickup current = pickup;
            if (current == null || current.Collecting) return;
            if (current.Collected) { TryRemoveCollected(current); return; }
            if (entity is not IPlayer player || !ReferenceEquals(player.Map, current.Owner.Map)) return;

            current.Collecting = true;
            try
            {
                var manager = player.QuestManager;
                var inventory = player.Inventory;
                if (manager == null || inventory == null || manager.GetQuestState(QuestByLeapsAndBounds) != QuestState.Accepted)
                    return;
                var quest = manager.GetActiveQuests().SingleOrDefault(q => q.Id == QuestByLeapsAndBounds && ReferenceEquals(q.Player, player));
                if (quest == null || quest.GetCurrentObjectiveId() is not (FirstFragmentObjective or FragmentCollectionObjective))
                    return;
                var fragment = quest.SingleOrDefault(o => o.ObjectiveInfo.Id == FragmentCollectionObjective
                    && o.ObjectiveInfo.Type == QuestObjectiveType.CollectItem && o.ObjectiveInfo.Entry.Data == PureLoftiteFragmentItem);
                if (fragment == null || fragment.IsComplete()) return;

                uint guid = current.Owner.Guid;
                var map = current.Owner.Map;
                bool OwnsPlayer() => ReferenceEquals(player.Map, map)
                    && ReferenceEquals(player.QuestManager, manager) && ReferenceEquals(player.Inventory, inventory);
                bool OwnsAction() => ReferenceEquals(pickup, current) && current.Owner.Guid == guid
                    && ReferenceEquals(current.Owner.Map, map) && OwnsPlayer();

                if (!inventory.TryAcquireMutationLease(out var held)) return;
                using (held)
                {
                    if (!OwnsAction()) return;
                    if (quest.GetCurrentObjectiveId() == FragmentCollectionObjective)
                        SyncCollectedItems(quest, inventory, fragment);
                    if (!OwnsAction() || quest.State != QuestState.Accepted || fragment.IsComplete()) return;

                    uint before = inventory.GetItemCount(PureLoftiteFragmentItem);
                    if (before >= fragment.ObjectiveInfo.Entry.Count)
                    {
                        if (TryAdvanceFirstFragment(quest) && OwnsAction())
                            SyncCollectedItems(quest, inventory, fragment);
                        return;
                    }
                    if (!inventory.CanCreateItem(InventoryLocation.Inventory, PureLoftiteFragmentItem, 1) || !OwnsAction())
                        return;
                    current.Guid = guid; current.Map = map;
                    try { inventory.ItemCreate(InventoryLocation.Inventory, PureLoftiteFragmentItem, 1, ItemUpdateReason.Loot); }
                    catch (Exception error) { log.Warn(error, "Q3777 item creation/feedback failed; checking accepted inventory quantity."); }
                    if (inventory.GetItemCount(PureLoftiteFragmentItem) <= before) return;

                    current.Collected = true;
                    player.RequestSave();
                    try
                    {
                        if (OwnsPlayer() && TryAdvanceFirstFragment(quest) && OwnsPlayer())
                            SyncCollectedItems(quest, inventory, fragment);
                    }
                    finally { TryRemoveCollected(current); }
                }
            }
            catch (Exception error) { log.Warn(error, "Q3777 pickup did not complete its current step; retaining its applied-item state."); }
            finally { current.Collecting = false; }
        }

        private static bool TryAdvanceFirstFragment(IQuest quest)
        {
            if (quest.GetCurrentObjectiveId() == FirstFragmentObjective)
            {
                try { quest.ObjectiveUpdate(FirstFragmentObjective, 1); }
                catch (Exception error) { log.Warn(error, "Q3777 first-fragment feedback failed; checking accepted progress."); }
            }
            return quest.GetCurrentObjectiveId() == FragmentCollectionObjective;
        }

        private static void SyncCollectedItems(IQuest quest, IInventory inventory, IQuestObjective fragment)
        {
            uint owned = inventory.GetItemCount(PureLoftiteFragmentItem);
            if (owned > fragment.Progress)
                quest.ObjectiveUpdate(FragmentCollectionObjective, owned - fragment.Progress);
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
            catch (Exception error) { log.Warn(error, "Q3777 crystal removal failed; the applied pickup will not grant again."); }
        }
    }

    // 6987 is the DataMapping bridge row; 13120 is the build 16042 Q3777 TargetGroup member.
    [ScriptFilterCreatureId(6987u, 13120u)]
    public class Q3777LoftiteCrystalEntityScript : Q3777LoftiteCrystalEntityScriptBase, IOwnedScript<ICreatureEntity>
    {
        public void OnLoad(ICreatureEntity owner)
        {
            OnLoad((IWorldEntity)owner);
        }
    }

    [ScriptFilterCreatureId(6987u, 13120u)]
    public class Q3777LoftiteCrystalCollectableEntityScript : Q3777LoftiteCrystalEntityScriptBase, IOwnedScript<ICollectableUnitEntity>
    {
        public void OnLoad(ICollectableUnitEntity owner)
        {
            OnLoad((IWorldEntity)owner);
        }
    }
}
