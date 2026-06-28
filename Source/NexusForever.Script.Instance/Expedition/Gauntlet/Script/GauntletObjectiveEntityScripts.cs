using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Static.PublicEvent;
using NexusForever.Script.Template;
using NexusForever.Script.Template.Filter;

namespace NexusForever.Script.Instance.Expedition.Gauntlet.Script
{
    public abstract class GauntletTargetGroupObjectiveEntityScriptBase : IWorldEntityScript, IOwnedScript<IWorldEntity>
    {
        private readonly PublicEventObjectiveType objectiveType;
        private readonly uint targetGroupId;

        private IWorldEntity entity;
        private bool activated;

        protected GauntletTargetGroupObjectiveEntityScriptBase(
            PublicEventObjectiveType objectiveType,
            uint targetGroupId)
        {
            this.objectiveType = objectiveType;
            this.targetGroupId = targetGroupId;
        }

        public void OnLoad(IWorldEntity owner)
        {
            entity = owner;
        }

        public void OnActivateSuccess(IPlayer _)
        {
            if (activated)
                return;

            activated = true;
            entity.Map.PublicEventManager.UpdateObjective(objectiveType, targetGroupId, 1);
        }
    }

    /// <summary>
    /// Build 16042 maps objective 1837 to TargetGroup 6831, whose members are
    /// the three electric-room Door Lock objects 48682, 48683, and 48684.
    /// </summary>
    [ScriptFilterCreatureId(48682u, 48683u, 48684u)]
    public class ElectricRoomDoorLockEntityScript : GauntletTargetGroupObjectiveEntityScriptBase
    {
        public ElectricRoomDoorLockEntityScript()
            : base(PublicEventObjectiveType.ActivateTargetGroup, 6831u)
        {
        }
    }
}
