using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Static.PublicEvent;
using NexusForever.Script.Template;
using NexusForever.Script.Template.Filter;

namespace NexusForever.Script.Instance.Expedition.EvilFromTheEther.Script
{
    public abstract class MedbayObjectiveEntityScriptBase : IWorldEntityScript
    {
        private readonly PublicEventObjectiveType objectiveType;
        private readonly uint[] objectIds;
        private readonly bool removeOnActivation;

        private IWorldEntity entity;
        private bool activated;

        protected MedbayObjectiveEntityScriptBase(
            PublicEventObjectiveType objectiveType,
            uint objectId,
            bool removeOnActivation)
            : this(objectiveType, removeOnActivation, objectId)
        {
        }

        protected MedbayObjectiveEntityScriptBase(
            PublicEventObjectiveType objectiveType,
            bool removeOnActivation,
            params uint[] objectIds)
        {
            this.objectiveType       = objectiveType;
            this.removeOnActivation  = removeOnActivation;
            this.objectIds           = objectIds;
        }

        protected void SetOwner(IWorldEntity owner)
        {
            entity = owner;
        }

        public void OnActivateSuccess(IPlayer _)
        {
            if (activated)
                return;

            activated = true;
            foreach (uint objectId in objectIds)
                entity.Map.PublicEventManager.UpdateObjective(objectiveType, objectId, GetObjectiveUpdateCount());

            if (removeOnActivation)
                entity.RemoveFromMap();
        }

        private int GetObjectiveUpdateCount()
        {
            if (objectiveType is PublicEventObjectiveType.ActivateTargetGroupChecklist
                or PublicEventObjectiveType.TalkToChecklist)
                return entity.QuestChecklistIdx;

            return 1;
        }
    }

    /// <summary>
    /// Build 16042 maps objective 4937 to TargetGroup 14064, whose Creature2 member
    /// is the Medbay Door Control row imported for world 3404.
    /// </summary>
    [ScriptFilterCreatureId(71283u)]
    public class MedbayDoorControlEntityScript : MedbayObjectiveEntityScriptBase, IOwnedScript<IWorldEntity>
    {
        public MedbayDoorControlEntityScript()
            : base(PublicEventObjectiveType.ActivateTargetGroup, 14064u, false)
        {
        }

        public void OnLoad(IWorldEntity owner)
        {
            SetOwner(owner);
        }
    }

    /// <summary>
    /// Build 16042 maps objective 4956 to TargetGroup 14066, whose Creature2 member
    /// is the three Spare Parts Crate rows imported for world 3404.
    /// </summary>
    [ScriptFilterCreatureId(71322u)]
    public class SparePartsCrateEntityScript : MedbayObjectiveEntityScriptBase, IOwnedScript<ICollectableUnitEntity>
    {
        public SparePartsCrateEntityScript()
            : base(PublicEventObjectiveType.ActivateTargetGroupChecklist, 14066u, true)
        {
        }

        public void OnLoad(ICollectableUnitEntity owner)
        {
            SetOwner(owner);
        }
    }

    /// <summary>
    /// Build 16042 maps objective 4957 to TargetGroup 14069, whose Creature2 member
    /// is the repaired Medbay Door Control row imported for world 3404.
    /// </summary>
    [ScriptFilterCreatureId(71323u)]
    public class RepairedMedbayDoorControlEntityScript : MedbayObjectiveEntityScriptBase, IOwnedScript<IWorldEntity>
    {
        public RepairedMedbayDoorControlEntityScript()
            : base(PublicEventObjectiveType.ActivateTargetGroup, 14069u, false)
        {
        }

        public void OnLoad(IWorldEntity owner)
        {
            SetOwner(owner);
        }
    }

    /// <summary>
    /// Build 16042 maps objective 4938 to TargetGroup 14076, whose Creature2 member
    /// is the Medbay Generator Controls row imported for world 3404.
    /// </summary>
    [ScriptFilterCreatureId(71371u)]
    public class MedbayGeneratorControlsEntityScript : MedbayObjectiveEntityScriptBase, IOwnedScript<IWorldEntity>
    {
        public MedbayGeneratorControlsEntityScript()
            : base(PublicEventObjectiveType.ActivateTargetGroup, 14076u, false)
        {
        }

        public void OnLoad(IWorldEntity owner)
        {
            SetOwner(owner);
        }
    }

    /// <summary>
    /// Build 16042 maps objective 4961 to TargetGroup 14083 and the aggregate
    /// objective 4941 to TargetGroup 14077; both include Generator Alpha controls.
    /// </summary>
    [ScriptFilterCreatureId(71374u)]
    public class MainEngineeringGeneratorAlphaControlsEntityScript : MedbayObjectiveEntityScriptBase, IOwnedScript<IWorldEntity>
    {
        public MainEngineeringGeneratorAlphaControlsEntityScript()
            : base(PublicEventObjectiveType.ActivateTargetGroup, false, 14083u, 14077u)
        {
        }

        public void OnLoad(IWorldEntity owner)
        {
            SetOwner(owner);
        }
    }

    /// <summary>
    /// Build 16042 maps objective 4962 to TargetGroup 14084 and the aggregate
    /// objective 4941 to TargetGroup 14077; both include Generator Beta controls.
    /// </summary>
    [ScriptFilterCreatureId(71375u)]
    public class MainEngineeringGeneratorBetaControlsEntityScript : MedbayObjectiveEntityScriptBase, IOwnedScript<IWorldEntity>
    {
        public MainEngineeringGeneratorBetaControlsEntityScript()
            : base(PublicEventObjectiveType.ActivateTargetGroup, false, 14084u, 14077u)
        {
        }

        public void OnLoad(IWorldEntity owner)
        {
            SetOwner(owner);
        }
    }

    /// <summary>
    /// Build 16042 maps objective 4978 to TargetGroup 14107, whose Creature2 member
    /// is the Teleport Controls row imported for world 3404.
    /// </summary>
    [ScriptFilterCreatureId(71631u)]
    public class TeleporterControlsEntityScript : MedbayObjectiveEntityScriptBase, IOwnedScript<IWorldEntity>
    {
        public TeleporterControlsEntityScript()
            : base(PublicEventObjectiveType.ActivateTargetGroupChecklist, 14107u, false)
        {
        }

        public void OnLoad(IWorldEntity owner)
        {
            SetOwner(owner);
        }
    }
}
