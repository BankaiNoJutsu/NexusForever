using NexusForever.Game.Abstract.Entity;
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

        private IWorldEntity owner;
        private bool collected;

        protected void OnLoad(IWorldEntity owner)
        {
            this.owner = owner;
        }

        public void OnActivateSuccess(IPlayer activator)
        {
            if (collected)
                return;

            if (activator.QuestManager.GetQuestState(QuestScatteredSupplies) != QuestState.Accepted)
                return;

            collected = true;
            activator.QuestManager.ObjectiveUpdate(QuestObjectiveType.VirtualCollect, ExileSuppliesVirtualItem, 1u);
            owner.RemoveFromMap();
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
