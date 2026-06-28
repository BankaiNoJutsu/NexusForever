using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Static.Quest;
using NexusForever.Script.Template;
using NexusForever.Script.Template.Filter;

namespace NexusForever.Script.Main.Quests.Galeras
{
    [ScriptFilterOwnerId(4694u)]
    [ScriptFilterCreatureId(23953u)]
    public class Q4694HoldTheLineStormwingStrikerEntityScript : IUnitScript, IOwnedScript<ICreatureEntity>
    {
        private const ushort QuestHoldTheLine = 4694;
        private const uint HoldoutEventObjectiveData = 111u;
        private bool credited;

        public void OnLoad(ICreatureEntity owner)
        {
        }

        public void OnKilled(IUnitEntity killer)
        {
            if (killer is not IPlayer player)
                return;

            if (player.QuestManager.GetQuestState(QuestHoldTheLine) != QuestState.Accepted)
                return;

            if (credited)
                return;

            credited = true;
            player.QuestManager.ObjectiveUpdate(QuestObjectiveType.CompleteEvent, HoldoutEventObjectiveData, 1u);
        }
    }
}
