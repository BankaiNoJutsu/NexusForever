using Microsoft.Extensions.Logging;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Map;
using NexusForever.Game.Abstract.Quest;
using NexusForever.Game.Static.Quest;
using NexusForever.Script.Template;
using NexusForever.Script.Template.Filter;

namespace NexusForever.Script.Main.Quests.NorthernWilds
{
    /// <summary>
    /// Quest 3667 (The Tower) -> grants 3486 (Empowered Tower) on completion.
    /// </summary>
    [ScriptFilterOwnerId(3667u)]
    public class Q3667TheTowerQuestScript : FollowUpQuestScript<Q3667TheTowerQuestScript>
    {
        protected override ushort NextQuestId => 3486;

        public Q3667TheTowerQuestScript(
            ILogger<Q3667TheTowerQuestScript> log,
            IGlobalQuestManager globalQuestManager)
            : base(log, globalQuestManager)
        {
        }
    }

    [ScriptFilterCreatureId(11194u)]
    public class Q3667ControlPanelEntityScript : IWorldEntityScript, IOwnedScript<ICreatureEntity>
    {
        private const ushort QuestTheTower  = 3667;
        // Objective 4770 is QuestObjective type 5 (ActivateEntity) for creature 11194 and appears in Quest2 row 3667.
        private const uint QObjTerminal     = 4770u;

        private ICreatureEntity owner;

        public void OnLoad(ICreatureEntity owner)
        {
            this.owner = owner;
        }

        public void OnAddToMap(IBaseMap map)
        {
            // WIP/GUESSED: Questing-and-more uses a 7m proximity trigger; exact retail trigger volume/timing is not live-smoked.
            owner.SetInRangeCheck(7f);
        }

        public void OnEnterRange(IGridEntity entity)
        {
            if (entity is not IPlayer player)
                return;

            TryCreditTerminal(player);
        }

        public void OnActivateSuccess(IPlayer activator)
        {
            TryCreditTerminal(activator);
        }

        private static void TryCreditTerminal(IPlayer player)
        {
            if (player.QuestManager.GetQuestState(QuestTheTower) != QuestState.Accepted)
                return;

            // Client row backs direct ActivateEntity credit; proximity fallback remains WIP/GUESSED until live-smoked.
            player.QuestManager.ObjectiveUpdate(QObjTerminal, 1u);
        }
    }
}
