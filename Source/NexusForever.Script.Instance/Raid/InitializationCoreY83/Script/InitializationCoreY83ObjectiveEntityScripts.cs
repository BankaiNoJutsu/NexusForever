using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Static.PublicEvent;
using NexusForever.Script.Template;
using NexusForever.Script.Template.Filter;

namespace NexusForever.Script.Instance.Raid.InitializationCoreY83.Script
{
    public abstract class InitializationCoreY83TargetGroupObjectiveEntityScriptBase : IWorldEntityScript, IOwnedScript<IWorldEntity>
    {
        private IWorldEntity entity;
        private bool activated;

        protected IWorldEntity Entity => entity;

        public void OnLoad(IWorldEntity owner)
        {
            entity = owner;
        }

        public void OnActivateSuccess(IPlayer _)
        {
            if (activated)
                return;

            activated = true;
            UpdateObjective();
        }

        protected abstract void UpdateObjective();
    }

    /// <summary>
    /// Build 16042 maps objective 2681 to ActivateTargetGroup TargetGroup
    /// 12539, whose Creature2 member is panel 68824. The objective count is
    /// six, so each placed panel instance contributes one guarded activation.
    /// </summary>
    [ScriptFilterCreatureId(68824u)]
    public class QuarantineDoorPanelEntityScript : InitializationCoreY83TargetGroupObjectiveEntityScriptBase
    {
        protected override void UpdateObjective()
        {
            Entity.Map.PublicEventManager.UpdateObjective(PublicEventObjectiveType.ActivateTargetGroup, 12539u, 1);
        }
    }

    /// <summary>
    /// Build 16042 maps objective 2682 to ActivateTargetGroup TargetGroup
    /// 12462, whose Creature2 member is Quarantine Door 66047.
    /// </summary>
    [ScriptFilterCreatureId(66047u)]
    public class QuarantineDoorEntityScript : InitializationCoreY83TargetGroupObjectiveEntityScriptBase
    {
        protected override void UpdateObjective()
        {
            Entity.Map.PublicEventManager.UpdateObjective(PublicEventObjectiveType.ActivateTargetGroup, 12462u, 1);
        }
    }
}
