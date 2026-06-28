using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Static.Quest;
using NexusForever.Script.Template;
using NexusForever.Script.Template.Filter;

namespace NexusForever.Script.Main.Quests.Galeras
{
    [ScriptFilterCreatureId(17189u, 19595u)]
    public class Q4696TempleRetreatEnemyEntityScript : IUnitScript, IOwnedScript<ICreatureEntity>
    {
        private const ushort QuestLeavingTheTempleOfOsiric = 4696;

        private bool credited;
        private ICreatureEntity owner;

        public void OnLoad(ICreatureEntity owner)
        {
            this.owner = owner;
        }

        public void OnKilled(IUnitEntity killer)
        {
            if (killer is not IPlayer player)
                return;

            if (player.QuestManager.GetQuestState(QuestLeavingTheTempleOfOsiric) != QuestState.Accepted)
                return;

            if (credited)
                return;

            credited = true;

            // Q4696 objective 6508 is ActivateEntity with reward-pane target group 4323.
            player.QuestManager.ObjectiveUpdate(QuestObjectiveType.ActivateEntity, owner.CreatureId, 1u);
        }
    }
}
