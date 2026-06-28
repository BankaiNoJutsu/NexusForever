using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Spell;
using NexusForever.Game.Static.PublicEvent;
using NexusForever.GameTable;
using NexusForever.Script.Instance;
using NexusForever.Script.Template;
using NexusForever.Script.Template.Filter;
using NexusForever.Shared;

namespace NexusForever.Script.Instance.Expedition.DeepSpaceExploration.Script
{
    public abstract class DeepSpaceTargetGroupObjectiveEntityScriptBase : IWorldEntityScript, IOwnedScript<IWorldEntity>
    {
        private readonly PublicEventObjectiveType objectiveType;
        private readonly uint targetGroupId;

        private IWorldEntity entity;
        private bool activated;

        protected DeepSpaceTargetGroupObjectiveEntityScriptBase(
            PublicEventObjectiveType objectiveType,
            uint targetGroupId)
        {
            this.objectiveType  = objectiveType;
            this.targetGroupId  = targetGroupId;
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
            entity.Map.PublicEventManager.UpdateObjective(
                objectiveType,
                targetGroupId,
                GetObjectiveUpdateCount());
        }

        private int GetObjectiveUpdateCount()
        {
            if (objectiveType is PublicEventObjectiveType.ActivateTargetGroupChecklist
                or PublicEventObjectiveType.TalkToChecklist)
                return entity.QuestChecklistIdx;

            return 1;
        }
    }

    public abstract class DeepSpaceTargetGroupChecklistEntityScriptBase : DeepSpaceTargetGroupObjectiveEntityScriptBase
    {
        protected DeepSpaceTargetGroupChecklistEntityScriptBase(uint targetGroupId)
            : base(PublicEventObjectiveType.ActivateTargetGroupChecklist, targetGroupId)
        {
        }
    }

    /// <summary>
    /// Build 16042 maps objective 1845 to ActivateTargetGroupChecklist
    /// TargetGroup 6902, whose Creature2 members are containment-cell terminals
    /// 48991, 49800, 49801, and 49802.
    /// </summary>
    [ScriptFilterCreatureId(48991u, 49800u, 49801u, 49802u)]
    public class SpecimenContainmentCellTerminalEntityScript : DeepSpaceTargetGroupChecklistEntityScriptBase
    {
        public SpecimenContainmentCellTerminalEntityScript()
            : base(6902u)
        {
        }
    }

    /// <summary>
    /// Build 16042 maps objective 1849 to ActivateTargetGroup TargetGroup
    /// 6905, whose Creature2 member is Steel Serpent Mainframe Cortex 48996.
    /// </summary>
    [ScriptFilterCreatureId(48996u)]
    public class SteelSerpentMainframeCortexEntityScript : DeepSpaceTargetGroupObjectiveEntityScriptBase
    {
        public SteelSerpentMainframeCortexEntityScript()
            : base(PublicEventObjectiveType.ActivateTargetGroup, 6905u)
        {
        }
    }

    /// <summary>
    /// Build 16042 maps objective 1846 to the phase-647 kill step for
    /// PE447 Steelfin forces aboard the Steel Serpent. These rows are the
    /// reviewed PE447 creature evidence in world 2188/zone 112.
    /// </summary>
    [ScriptFilterCreatureId(49408u, 48740u, 48702u, 49406u, 48762u)]
    public class SteelfinForcesEntityScript : PublicEventObjectiveCreditEntityScript
    {
        public SteelfinForcesEntityScript(
            IFactory<ISpellParameters> spellParametersFactory,
            IGameTableManager gameTableManager)
            : base(
                spellParametersFactory,
                gameTableManager,
                (uint)PublicEventObjective.KillSteelfinForces)
        {
        }
    }
}
