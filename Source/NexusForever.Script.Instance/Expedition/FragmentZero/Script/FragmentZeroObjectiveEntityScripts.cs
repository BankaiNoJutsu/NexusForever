using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Spell;
using NexusForever.Game.Static.PublicEvent;
using NexusForever.GameTable;
using NexusForever.Script.Template;
using NexusForever.Script.Template.Filter;
using NexusForever.Shared;

namespace NexusForever.Script.Instance.Expedition.FragmentZero.Script
{
    public abstract class FragmentZeroTargetGroupObjectiveEntityScriptBase : IWorldEntityScript, IOwnedScript<IWorldEntity>
    {
        private readonly PublicEventObjectiveType objectiveType;
        private readonly uint targetGroupId;
        private readonly PublicEventObjective? aggregateObjective;

        private IWorldEntity entity;
        private bool activated;

        protected FragmentZeroTargetGroupObjectiveEntityScriptBase(
            PublicEventObjectiveType objectiveType,
            uint targetGroupId,
            PublicEventObjective? aggregateObjective = null)
        {
            this.objectiveType      = objectiveType;
            this.targetGroupId      = targetGroupId;
            this.aggregateObjective = aggregateObjective;
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
            if (aggregateObjective.HasValue)
                entity.Map.PublicEventManager.UpdateObjective(aggregateObjective.Value, 1);
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
    /// Build 16042 maps objectives 4431 and 4684 to VirtualCollect 1176,
    /// whose objective text names Cargo Crate Creature2 67965.
    /// </summary>
    [ScriptFilterCreatureId(67965u)]
    public class CargoCrateEntityScript : IWorldEntityScript, IOwnedScript<IWorldEntity>
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
            entity.Map.PublicEventManager.UpdateObjective(PublicEventObjectiveType.VirtualCollect, 1176u, 1);
            entity.RemoveFromMap();
        }
    }

    /// <summary>
    /// Build 16042 maps objective 4632 to a Script count of 30. Jabbithole
    /// placement evidence for the Incubation Complex maps the small and large
    /// Xenobite Egg quest objects to Creature2 68811 and 68812.
    /// </summary>
    [ScriptFilterCreatureId(68811u, 68812u)]
    public class XenobiteEggEntityScript : IWorldEntityScript, IOwnedScript<IWorldEntity>
    {
        private IWorldEntity entity;
        private bool smashed;

        public void OnLoad(IWorldEntity owner)
        {
            entity = owner;
        }

        public void OnActivateSuccess(IPlayer _)
        {
            if (smashed)
                return;

            smashed = true;
            entity.Map.PublicEventManager.UpdateObjective(PublicEventObjective.SmashXenobiteEggsInsideTheIncubationComplex, 1);
            entity.RemoveFromMap();
        }
    }

    /// <summary>
    /// Build 16042 maps objectives 4416 and 4584/4585/4586 to TargetGroup 12254.
    /// TargetGroup 12254 contains the Fragment Zero Skeech horde leaves
    /// 67516/67518/67519/67520.
    /// </summary>
    [ScriptFilterCreatureId(67516u, 67518u, 67519u, 67520u)]
    public class FragmentZeroSkeechHordeEntityScript : IUnitScript, IOwnedScript<ICreatureEntity>
    {
        private ICreatureEntity entity;
        private bool credited;

        public void OnLoad(ICreatureEntity owner)
        {
            entity = owner;
        }

        public void OnDeath()
        {
            if (credited)
                return;

            credited = true;
            entity.Map.PublicEventManager.UpdateObjective(PublicEventObjective.EliminateSkeech, 1);
            entity.Map.PublicEventManager.UpdateObjective(PublicEventObjective.EliminateTheHordesOfSkeechInfestingTheArea, 1);
            entity.Map.PublicEventManager.UpdateObjective(PublicEventObjective.EliminateTheHordesOfSkeechInfestingTheArea2, 1);
            entity.Map.PublicEventManager.UpdateObjective(PublicEventObjective.EliminateTheHordesOfSkeechInfestingTheArea3, 1);
        }
    }

    /// <summary>
    /// Build 16042 maps objective 4424 to TargetGroup 12274,
    /// whose Creature2 member is Crewmate Jo's corpse 67966.
    /// </summary>
    [ScriptFilterCreatureId(67966u)]
    public class CrewmateJoCorpseEntityScript : FragmentZeroTargetGroupObjectiveEntityScriptBase
    {
        public CrewmateJoCorpseEntityScript()
            : base(PublicEventObjectiveType.ActivateTargetGroup, 12274u)
        {
        }
    }

    /// <summary>
    /// Build 16042 maps objective 4432 to TargetGroup 12264,
    /// whose Creature2 member is Crewmate Syrus's corpse 67931.
    /// </summary>
    [ScriptFilterCreatureId(67931u)]
    public class CrewmateSyrusCorpseEntityScript : FragmentZeroTargetGroupObjectiveEntityScriptBase
    {
        public CrewmateSyrusCorpseEntityScript()
            : base(PublicEventObjectiveType.ActivateTargetGroup, 12264u)
        {
        }
    }

    /// <summary>
    /// Build 16042 maps objective 4635 to TargetGroup 12461,
    /// whose Creature2 member is Facility Defense Control Panel 68856.
    /// </summary>
    [ScriptFilterCreatureId(68856u)]
    public class FacilityDefenseControlPanelEntityScript : FragmentZeroTargetGroupObjectiveEntityScriptBase
    {
        public FacilityDefenseControlPanelEntityScript()
            : base(PublicEventObjectiveType.ActivateTargetGroupChecklist, 12461u)
        {
        }
    }

    /// <summary>
    /// Build 16042 maps objective 4445 to TargetGroup 12267,
    /// whose Creature2 member is the locate-Hugo projection 67961.
    /// </summary>
    [ScriptFilterCreatureId(67961u)]
    public class CaptainHugoLocateEntityScript : FragmentZeroTargetGroupObjectiveEntityScriptBase
    {
        public CaptainHugoLocateEntityScript()
            : base(PublicEventObjectiveType.ActivateTargetGroup, 12267u)
        {
        }
    }

    /// <summary>
    /// Build 16042 maps objective 4423 to TargetGroup 12269,
    /// whose Creature2 members are Prototype Alpha 67526 and 69672.
    /// </summary>
    [ScriptFilterCreatureId(67526u, 69672u)]
    public class PrototypeAlphaEntityScript : PublicEventObjectiveCreditEntityScript
    {
        public PrototypeAlphaEntityScript(
            IFactory<ISpellParameters> spellParametersFactory,
            IGameTableManager gameTableManager)
            : base(
                spellParametersFactory,
                gameTableManager,
                (uint)PublicEventObjective.DefeatPrototypeAlphansideTheIncubationComplex)
        {
        }
    }

    /// <summary>
    /// Build 16042 maps objective 4449 to TargetGroup 12270,
    /// whose Creature2 members are Prototype Beta 67527 and 69673.
    /// </summary>
    [ScriptFilterCreatureId(67527u, 69673u)]
    public class PrototypeBetaEntityScript : PublicEventObjectiveCreditEntityScript
    {
        public PrototypeBetaEntityScript(
            IFactory<ISpellParameters> spellParametersFactory,
            IGameTableManager gameTableManager)
            : base(
                spellParametersFactory,
                gameTableManager,
                (uint)PublicEventObjective.DefeatPrototypeBeta)
        {
        }
    }

    /// <summary>
    /// Build 16042 maps objective 4450 to TargetGroup 12271,
    /// whose Creature2 members are Prototype Delta 67528 and 69674.
    /// </summary>
    [ScriptFilterCreatureId(67528u, 69674u)]
    public class PrototypeDeltaEntityScript : PublicEventObjectiveCreditEntityScript
    {
        public PrototypeDeltaEntityScript(
            IFactory<ISpellParameters> spellParametersFactory,
            IGameTableManager gameTableManager)
            : base(
                spellParametersFactory,
                gameTableManager,
                (uint)PublicEventObjective.DefeatPrototypeDelta)
        {
        }
    }

    /// <summary>
    /// Build 16042 maps objective 4417 to TargetGroup 12255,
    /// whose Creature2 members are Project "Matron" 69088 and 69664.
    /// </summary>
    [ScriptFilterCreatureId(69088u, 69664u)]
    public class ProjectMatronEntityScript : PublicEventObjectiveCreditEntityScript
    {
        public ProjectMatronEntityScript(
            IFactory<ISpellParameters> spellParametersFactory,
            IGameTableManager gameTableManager)
            : base(
                spellParametersFactory,
                gameTableManager,
                (uint)PublicEventObjective.DefeatProjectMatron)
        {
        }
    }

    /// <summary>
    /// Build 16042 maps objective 4444 to TargetGroup 12266,
    /// whose Creature2 members are Life-Overseer 67522 and 69635.
    /// </summary>
    [ScriptFilterCreatureId(67522u, 69635u)]
    public class LifeOverseerEntityScript : PublicEventObjectiveCreditEntityScript
    {
        public LifeOverseerEntityScript(
            IFactory<ISpellParameters> spellParametersFactory,
            IGameTableManager gameTableManager)
            : base(
                spellParametersFactory,
                gameTableManager,
                (uint)PublicEventObjective.DefeatTheLifeOverseer)
        {
        }
    }

    /// <summary>
    /// Build 16042 maps objective 4454 to TargetGroup 12276,
    /// whose Creature2 member is Ohmna's hidden recording 67967.
    /// </summary>
    [ScriptFilterCreatureId(67967u)]
    public class OhmnasHiddenRecordingEntityScript : FragmentZeroTargetGroupObjectiveEntityScriptBase
    {
        public OhmnasHiddenRecordingEntityScript()
            : base(
                PublicEventObjectiveType.ActivateTargetGroupChecklist,
                12276u,
                PublicEventObjective.DiscoverTheTwoLostRecordings)
        {
        }
    }

    /// <summary>
    /// Build 16042 maps objective 4455 to TargetGroup 12279,
    /// whose Creature2 member is Lucent's hidden recording 67968.
    /// </summary>
    [ScriptFilterCreatureId(67968u)]
    public class LucentsHiddenRecordingEntityScript : FragmentZeroTargetGroupObjectiveEntityScriptBase
    {
        public LucentsHiddenRecordingEntityScript()
            : base(
                PublicEventObjectiveType.ActivateTargetGroupChecklist,
                12279u,
                PublicEventObjective.DiscoverTheTwoLostRecordings)
        {
        }
    }
}
