using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Quest;
using NexusForever.Game.Static.Quest;
using NexusForever.Script.Template;
using NexusForever.Script.Template.Filter;

namespace NexusForever.Script.Main.Quests.NorthernWilds
{
    /// <summary>
    /// Northern Wilds: Captives of the Dominion.
    /// Objective 4880 rescues Captive Exile Soldiers.
    /// </summary>
    [ScriptFilterOwnerId(3781u)]
    public class Q3781CaptivesOfTheDominionQuestScript : IQuestScript, IOwnedScript<IQuest>
    {
        public void OnLoad(IQuest owner)
        {
        }
    }

    [ScriptFilterCreatureId(12537u)]
    public class Q3781CaptiveExileSoldierEntityScript : IWorldEntityScript, IOwnedScript<ICreatureEntity>
    {
        private const ushort QuestCaptivesOfTheDominion = 3781;
        private const uint ObjectiveRescueCaptives      = 4880u;

        public void OnLoad(ICreatureEntity owner)
        {
        }

        public void OnActivateSuccess(IPlayer activator)
        {
            if (activator.QuestManager.GetQuestState(QuestCaptivesOfTheDominion) != QuestState.Accepted)
                return;

            activator.QuestManager.ObjectiveUpdate(ObjectiveRescueCaptives, 1u);
        }
    }
}
