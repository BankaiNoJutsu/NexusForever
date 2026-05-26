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

    [ScriptFilterCreatureId(12919u)]
    public class Q3741ExileSupplyCrateEntityScript : IWorldEntityScript, IOwnedScript<ICreatureEntity>
    {
        private const ushort QuestScatteredSupplies = 3741;
        private const uint ObjectiveGatherSupplies  = 4813u;

        public void OnLoad(ICreatureEntity owner)
        {
        }

        public void OnActivateSuccess(IPlayer activator)
        {
            if (activator.QuestManager.GetQuestState(QuestScatteredSupplies) != QuestState.Accepted)
                return;

            activator.QuestManager.ObjectiveUpdate(ObjectiveGatherSupplies, 1u);
        }
    }
}
