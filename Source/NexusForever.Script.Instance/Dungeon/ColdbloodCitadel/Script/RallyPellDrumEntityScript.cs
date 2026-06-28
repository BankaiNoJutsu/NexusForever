using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Static.PublicEvent;
using NexusForever.Script.Template;
using NexusForever.Script.Template.Filter;

namespace NexusForever.Script.Instance.Dungeon.ColdbloodCitadel.Script
{
    /// <summary>
    /// Build 16042 maps objective 5316 to TargetGroup 14469, whose Creature2
    /// member is the Rally Pell Drum row used by Coldblood Citadel.
    /// </summary>
    [ScriptFilterCreatureId(75706u)]
    public class RallyPellDrumEntityScript : IWorldEntityScript, IOwnedScript<IWorldEntity>
    {
        private IWorldEntity entity;
        private bool activated;

        public void OnLoad(IWorldEntity owner)
        {
            entity = owner;
        }

        public void OnActivateSuccess(IPlayer _)
        {
            if (activated)
                return;

            activated = true;
            entity.Map.PublicEventManager.UpdateObjective(
                PublicEventObjectiveType.ActivateTargetGroupChecklist,
                14469u,
                entity.QuestChecklistIdx);
        }
    }
}
