using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Map;
using NexusForever.Game.Abstract.Quest;
using NexusForever.Game.Static.Quest;
using NexusForever.Script.Template;
using NexusForever.Script.Template.Filter;

namespace NexusForever.Script.Main.Quests.NorthernWilds
{
    /// <summary>
    /// Northern Wilds: By Leaps and Bounds.
    /// Objective 5076 is the first pure-loftite jump-through, and objective 4859 tracks collected fragments.
    /// </summary>
    [ScriptFilterOwnerId(3777u)]
    public class Q3777ByLeapsAndBoundsQuestScript : IQuestScript, IOwnedScript<IQuest>
    {
        public void OnLoad(IQuest owner)
        {
        }
    }

    [ScriptFilterCreatureId(6987u)]
    public class Q3777LoftiteCrystalEntityScript : IWorldEntityScript, IOwnedScript<ICreatureEntity>
    {
        private const ushort QuestByLeapsAndBounds = 3777;
        private const uint ObjectiveFirstFragment  = 5076u;
        private const uint PureLoftiteFragmentItem = 6998u;

        private ICreatureEntity owner;

        public void OnLoad(ICreatureEntity owner)
        {
            this.owner = owner;
        }

        public void OnAddToMap(IBaseMap map)
        {
            owner.SetInRangeCheck(5f);
        }

        public void OnEnterRange(IGridEntity entity)
        {
            if (entity is not IPlayer player)
                return;

            if (player.QuestManager.GetQuestState(QuestByLeapsAndBounds) != QuestState.Accepted)
                return;

            player.QuestManager.ObjectiveUpdate(ObjectiveFirstFragment, 1u);
            player.QuestManager.ObjectiveUpdate(QuestObjectiveType.CollectItem, PureLoftiteFragmentItem, 1u);
        }
    }
}
