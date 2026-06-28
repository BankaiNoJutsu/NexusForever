using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Static.PublicEvent;
using NexusForever.Script.Template;
using NexusForever.Script.Template.Filter;

namespace NexusForever.Script.Instance.Dungeon.UltimateProtogames.Script
{
    public abstract class SneakyPrisonTargetGroupEntityScriptBase : IWorldEntityScript, IOwnedScript<IWorldEntity>
    {
        protected IWorldEntity Entity { get; private set; }

        private bool activated;

        public void OnLoad(IWorldEntity owner)
        {
            Entity = owner;
        }

        public abstract void OnActivateSuccess(IPlayer activator);

        protected bool TryActivate()
        {
            if (activated)
                return false;

            activated = true;
            return true;
        }
    }

    /// <summary>
    /// Build 16042 maps objective 2847 to TargetGroup 10569, whose Creature2
    /// member is the Sneaky Prison Gate Console 62987. Jabbithole/DataMapping
    /// place the matching room console as Creature2 62427, so this bridge
    /// accepts both ids while still crediting the build target group.
    /// </summary>
    [ScriptFilterCreatureId(62987u, 62427u)]
    public class SneakyPrisonGateConsoleEntityScript : SneakyPrisonTargetGroupEntityScriptBase
    {
        public override void OnActivateSuccess(IPlayer _)
        {
            if (!TryActivate())
                return;

            Entity.Map.PublicEventManager.UpdateObjective(
                PublicEventObjectiveType.ActivateTargetGroup,
                10569u,
                1);
        }
    }

    /// <summary>
    /// Build 16042 maps objective 2864 to TargetGroup 10583, whose Creature2
    /// member is the Sneaky Prison Cage Console 63037.
    /// </summary>
    [ScriptFilterCreatureId(63037u)]
    public class SneakyPrisonCageConsoleEntityScript : SneakyPrisonTargetGroupEntityScriptBase
    {
        public override void OnActivateSuccess(IPlayer _)
        {
            if (!TryActivate())
                return;

            Entity.Map.PublicEventManager.UpdateObjective(
                PublicEventObjectiveType.ActivateTargetGroup,
                10583u,
                1);
        }
    }

    /// <summary>
    /// Build 16042 maps objective 4648 to TargetGroup 12478, whose Creature2
    /// member is the Sneaky Prison Alarm Panel 68916.
    /// </summary>
    [ScriptFilterCreatureId(68916u)]
    public class SneakyPrisonAlarmPanelEntityScript : SneakyPrisonTargetGroupEntityScriptBase
    {
        public override void OnActivateSuccess(IPlayer _)
        {
            if (!TryActivate())
                return;

            Entity.Map.PublicEventManager.UpdateObjective(
                PublicEventObjectiveType.ActivateTargetGroup,
                12478u,
                1);
        }
    }
}
