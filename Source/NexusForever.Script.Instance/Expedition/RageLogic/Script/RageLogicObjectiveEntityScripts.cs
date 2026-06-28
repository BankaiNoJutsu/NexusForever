using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Spell;
using NexusForever.Game.Static.PublicEvent;
using NexusForever.GameTable;
using NexusForever.Script.Template;
using NexusForever.Script.Template.Filter;
using NexusForever.Shared;

namespace NexusForever.Script.Instance.Expedition.RageLogic.Script
{
    public abstract class RageLogicDirectObjectiveEntityScriptBase : IWorldEntityScript, IOwnedScript<IWorldEntity>
    {
        private readonly PublicEventObjective objective;
        private readonly bool removeOnActivation;

        private IWorldEntity entity;
        private bool activated;

        protected RageLogicDirectObjectiveEntityScriptBase(
            PublicEventObjective objective,
            bool removeOnActivation)
        {
            this.objective          = objective;
            this.removeOnActivation = removeOnActivation;
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
            entity.Map.PublicEventManager.UpdateObjective(objective, 1);

            if (removeOnActivation)
                entity.RemoveFromMap();
        }
    }

    public abstract class RageLogicTargetGroupEntityScriptBase : IWorldEntityScript, IOwnedScript<IWorldEntity>
    {
        private readonly PublicEventObjectiveType objectiveType;
        private readonly uint targetGroupId;
        private readonly bool removeOnActivation;

        private IWorldEntity entity;
        private bool activated;

        protected RageLogicTargetGroupEntityScriptBase(
            PublicEventObjectiveType objectiveType,
            uint targetGroupId,
            bool removeOnActivation)
        {
            this.objectiveType       = objectiveType;
            this.targetGroupId       = targetGroupId;
            this.removeOnActivation  = removeOnActivation;
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
            entity.Map.PublicEventManager.UpdateObjective(objectiveType, targetGroupId, GetObjectiveUpdateCount());

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
    /// Build 16042 objective 781 is the vehicle-choice Script objective. Its
    /// reward-pane TargetGroup 12645 lists the Rocket Launcher, EMP, and Rail Gun
    /// speeder rows.
    /// </summary>
    [ScriptFilterCreatureId(32249u, 43809u, 43810u)]
    public class RageLogicVehicleChoiceEntityScript : RageLogicDirectObjectiveEntityScriptBase
    {
        public RageLogicVehicleChoiceEntityScript()
            : base(PublicEventObjective.ChooseVehicle, false)
        {
        }
    }

    /// <summary>
    /// Build 16042 objective 784 is ActivateTargetGroupChecklist TargetGroup
    /// 3770, whose member is Asteroid Thruster Creature2 32365.
    /// </summary>
    [ScriptFilterCreatureId(32365u)]
    public class AsteroidThrusterEntityScript : RageLogicTargetGroupEntityScriptBase
    {
        public AsteroidThrusterEntityScript()
            : base(PublicEventObjectiveType.ActivateTargetGroupChecklist, 3770u, true)
        {
        }
    }

    /// <summary>
    /// Build 16042 objective 783 is a Script objective whose reward-pane
    /// TargetGroup 12644 expands to the asteroid Ragebot/freebot defender rows.
    /// </summary>
    [ScriptFilterCreatureId(
        69107u, 69097u, 32302u, 69106u, 69096u, 32322u, 69095u,
        69094u, 32323u, 44232u, 43827u, 32324u)]
    public class RagebotAsteroidDefenderEntityScript : PublicEventObjectiveCreditEntityScript
    {
        public RagebotAsteroidDefenderEntityScript(
            IFactory<ISpellParameters> spellParametersFactory,
            IGameTableManager gameTableManager)
            : base(
                spellParametersFactory,
                gameTableManager,
                (uint)PublicEventObjective.ObliterateRagebotsDefendingAsteroid)
        {
        }
    }
}
