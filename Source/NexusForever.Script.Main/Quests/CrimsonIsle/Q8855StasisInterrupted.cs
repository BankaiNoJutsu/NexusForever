using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Map;
using NexusForever.Game.Static.Entity;
using NexusForever.Game.Static.Quest;
using NexusForever.Game.Static.Spell;
using NexusForever.Script.Template;
using NexusForever.Script.Template.Filter;

namespace NexusForever.Script.Main.Quests.CrimsonIsle
{
    [ScriptFilterCreatureId(47687u, 47688u)]
    public class Q8855DominionSoldiersEntityScript: IWorldEntityScript, IOwnedScript<ICreatureEntity>
    {
        private const ushort QuestStasisInterrupted = 8855;

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

            if (player.QuestManager.GetQuestState(QuestStasisInterrupted) != QuestState.Accepted)
                return;

            player.QuestManager.ObjectiveUpdate(QuestObjectiveType.ActivateEntity, owner.CreatureId, 1u);
            owner.ModifyHealth(owner.Health, DamageType.Physical, null);
        }
    }
}
