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
        private const uint FirstFragmentActivateEntity = 6952u;
        private const uint PureLoftiteFragmentItem = 6998u;

        private IWorldEntity owner;
        private bool collected;

        protected void OnLoad(IWorldEntity owner)
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

            if (collected)
                return;

            collected = true;
            player.QuestManager.ObjectiveUpdate(QuestObjectiveType.ActivateEntity, FirstFragmentActivateEntity, 1u);
            player.QuestManager.ObjectiveUpdate(QuestObjectiveType.CollectItem, PureLoftiteFragmentItem, 1u);
            owner.RemoveFromMap();
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
