using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Quest;
using NexusForever.Game.Static.Quest;
using NexusForever.Script.Template;
using NexusForever.Script.Template.Filter;

namespace NexusForever.Script.Main.Quests
{
    [ScriptFilterOwnerId(10510u)]
    public class Q10510LearningToShopQuestScript : IQuestScript, IOwnedScript<IQuest>
    {
        private const uint QObjPurchaseSmartShopper  = 21267u;
        private const uint ItemTitleSmartShopper     = 86245u;
        private const ushort TitleSmartShopper       = 400;

        private IQuest owner;

        public void OnLoad(IQuest owner)
        {
            this.owner = owner;
        }

        public void OnQuestStateChange(QuestState newState, QuestState oldState)
        {
            if (newState < QuestState.Completed)
            {
                IPlayer player = owner.Player;
                // WIP/GUESSED: Questing-and-more proves the active-state item/title snapshot, but exact retail refresh timing is not live-smoked.
                if (player.Inventory.HasItemCount(ItemTitleSmartShopper, 1u) || player.TitleManager.HasTitle(TitleSmartShopper))
                    player.QuestManager.ObjectiveUpdate(QObjPurchaseSmartShopper, 1u);
            }
        }
    }
}
