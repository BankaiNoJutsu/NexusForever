using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Static.PublicEvent;
using NexusForever.Script.Template;
using NexusForever.Script.Template.Filter;

namespace NexusForever.Script.Instance.Expedition.EvilFromTheEther.Script
{
    /// <summary>
    /// Creature 71821 is the build 16042 TargetGroup 14131 member for public-event
    /// objective 5013.
    /// </summary>
    [ScriptFilterCreatureId(71821)]
    public class EthericDriveSchematicsEntityScript : IWorldEntityScript, IOwnedScript<IWorldEntity>
    {
        private IWorldEntity entity;
        private bool collected;

        public void OnLoad(IWorldEntity entity)
        {
            this.entity = entity;
        }

        public void OnActivateSuccess(IPlayer _)
        {
            if (collected)
                return;

            collected = true;
            entity.Map.PublicEventManager.UpdateObjective(PublicEventObjective.PickUpDriveSchematics, 1);
            entity.RemoveFromMap();
        }
    }

    /// <summary>
    /// Creature 75222 is the build 16042 TargetGroup 14430 member for public-event
    /// objective 4943.
    /// </summary>
    [ScriptFilterCreatureId(75222u)]
    public class DriveDiagnosticsEntityScript : IWorldEntityScript, IOwnedScript<IWorldEntity>
    {
        private IWorldEntity entity;
        private bool collected;

        public void OnLoad(IWorldEntity owner)
        {
            entity = owner;
        }

        public void OnActivateSuccess(IPlayer _)
        {
            if (collected)
                return;

            collected = true;
            entity.Map.PublicEventManager.UpdateObjective(PublicEventObjectiveType.ActivateTargetGroup, 14430u, 1);
            entity.RemoveFromMap();
        }
    }
}
