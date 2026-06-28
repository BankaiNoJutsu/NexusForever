using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Static.Entity.Movement.Command.Mode;
using NexusForever.Game.Static.PublicEvent;
using NexusForever.Script.Template;
using NexusForever.Script.Template.Filter;

namespace NexusForever.Script.Instance.Expedition.EvilFromTheEther.Script
{
    /// <summary>
    /// Build 16042 maps objective 4924 to TargetGroup 14050, whose Creature2 member
    /// is the Etheric Drive Controls row imported for world 3404.
    /// </summary>
    [ScriptFilterCreatureId(71228u)]
    public class EthericDriveControlsEntityScript : MedbayObjectiveEntityScriptBase, IOwnedScript<IUnitEntity>
    {
        public EthericDriveControlsEntityScript()
            : base(PublicEventObjectiveType.ActivateTargetGroup, 14050u, false)
        {
        }

        /// <summary>
        /// Invoked when <see cref="IScript"/> is loaded.
        /// </summary>
        public void OnLoad(IUnitEntity owner)
        {
            SetOwner(owner);
            owner.MovementManager.SetMode(ModeType.Slide);
        }
    }
}
