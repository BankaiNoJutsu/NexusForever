using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Static.PublicEvent;
using NexusForever.Script.Template;
using NexusForever.Script.Template.Filter;

namespace NexusForever.Script.Instance.Dungeon.StormtalonsLair.Script
{
    /// <summary>
    /// Build 16042 maps public-event objective 554 to TargetGroup 2548,
    /// whose Creature2 member is Tainted Flower Stem 24307.
    /// </summary>
    [ScriptFilterCreatureId(24307u)]
    public class TaintedFlowerStemEntityScript : IWorldEntityScript, IOwnedScript<IWorldEntity>
    {
        private IWorldEntity entity;

        public void OnLoad(IWorldEntity owner)
        {
            entity = owner;
        }

        public void OnActivateSuccess(IPlayer _)
        {
            entity.Map.PublicEventManager.UpdateObjective(PublicEventObjective.GatherTaintedStemSamples, 1);
        }
    }

    /// <summary>
    /// Build 16042 maps public-event objective 464 to TargetGroup 1661,
    /// whose Creature2 member is Thundercall Cage 17191.
    /// </summary>
    [ScriptFilterCreatureId(17191u)]
    public class ThundercallSacrificialCageEntityScript : IWorldEntityScript, IOwnedScript<IWorldEntity>, IOwnedScript<ISimpleCollidableEntity>
    {
        private IWorldEntity entity;

        public void OnLoad(IWorldEntity owner)
        {
            entity = owner;
        }

        public void OnLoad(ISimpleCollidableEntity owner)
        {
            entity = owner;
        }

        public void OnActivateSuccess(IPlayer _)
        {
            entity.Map.PublicEventManager.UpdateObjective(PublicEventObjective.FreeTheThundercallSacrificialPrisoners, 1);
        }
    }

    /// <summary>
    /// Build 16042 maps public-event objectives 558 and 4867 to the Improvement
    /// Construction Platform Creature2 row 27244 at WorldLocation2 20740.
    /// </summary>
    [ScriptFilterCreatureId(27244u)]
    public class ImprovementConstructionPlatformEntityScript : IWorldEntityScript, IOwnedScript<IWorldEntity>
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
            entity.Map.PublicEventManager.UpdateObjective(PublicEventObjective.ActivateImprovementConstructionPlatform, 1);
            entity.Map.PublicEventManager.UpdateObjective(PublicEventObjective.EnableInvokersHolocrypt, 1);
        }
    }

    /// <summary>
    /// Build 16042 maps public-event objective 559 to TargetGroup 3679,
    /// whose Creature2 member is Launch Pad 31587.
    /// </summary>
    [ScriptFilterCreatureId(31587u)]
    public class LaunchPadEntityScript : IWorldEntityScript, IOwnedScript<IWorldEntity>
    {
        private const uint LaunchPadTargetGroupId = 3679u;

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
                LaunchPadTargetGroupId,
                entity.QuestChecklistIdx);
        }
    }
}
