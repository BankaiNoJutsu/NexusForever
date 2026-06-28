using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Static.PublicEvent;
using NexusForever.Script.Template;
using NexusForever.Script.Template.Filter;

namespace NexusForever.Script.Instance.Adventure.TheMalgraveTrail.Script
{
    /// <summary>
    /// Build 16042 maps public-event objective 186 to ActivateTargetGroup
    /// TargetGroup 2199, whose Creature2 members are Food Crate 19918,
    /// Water Barrel 19919, and Feed Sack 19920.
    /// </summary>
    [ScriptFilterCreatureId(19918u, 19919u, 19920u)]
    public class MalgraveSupplyEntityScript : IWorldEntityScript, IOwnedScript<IWorldEntity>
    {
        private const uint SupplyTargetGroupId = 2199u;

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
                PublicEventObjectiveType.ActivateTargetGroup,
                SupplyTargetGroupId,
                1);
        }
    }
}
