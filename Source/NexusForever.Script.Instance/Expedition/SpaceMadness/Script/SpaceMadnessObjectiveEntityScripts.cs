using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Spell;
using NexusForever.Game.Static.PublicEvent;
using NexusForever.GameTable;
using NexusForever.Script.Instance;
using NexusForever.Script.Template;
using NexusForever.Script.Template.Filter;
using NexusForever.Shared;

namespace NexusForever.Script.Instance.Expedition.SpaceMadness.Script
{
    public abstract class SpaceMadnessTargetGroupObjectiveEntityScriptBase : IWorldEntityScript, IOwnedScript<IWorldEntity>
    {
        private readonly PublicEventObjectiveType objectiveType;
        private readonly uint targetGroupId;
        private readonly PublicEventObjective? aggregateObjective;

        private IWorldEntity entity;
        private bool activated;

        protected SpaceMadnessTargetGroupObjectiveEntityScriptBase(
            PublicEventObjectiveType objectiveType,
            uint targetGroupId,
            PublicEventObjective? aggregateObjective = null)
        {
            this.objectiveType       = objectiveType;
            this.targetGroupId       = targetGroupId;
            this.aggregateObjective  = aggregateObjective;
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
    /// Build 16042 maps objective 1596 to TargetGroup 6384,
    /// whose Creature2 member is Datapad 45993.
    /// </summary>
    [ScriptFilterCreatureId(45993u)]
    public class CrewDatapadEntityScript : SpaceMadnessTargetGroupObjectiveEntityScriptBase
    {
        public CrewDatapadEntityScript()
            : base(PublicEventObjectiveType.ActivateTargetGroupChecklist, 6384u)
        {
        }
    }

    /// <summary>
    /// Build 16042 maps objectives 1592 and 4736 to TargetGroup 12651,
    /// whose Creature2 member is the Hazmat Control Panel 46092.
    /// </summary>
    [ScriptFilterCreatureId(46092u)]
    public class HazmatControlPanelEntityScript : SpaceMadnessTargetGroupObjectiveEntityScriptBase
    {
        public HazmatControlPanelEntityScript()
            : base(PublicEventObjectiveType.ActivateTargetGroup, 12651u)
        {
        }
    }

    /// <summary>
    /// Build 16042 maps objective 1595 to TargetGroup 6371, whose members are
    /// the Main Vent Lever 45861 and Air Scrubber Controls 46437.
    /// </summary>
    [ScriptFilterCreatureId(45861u, 46437u)]
    public class AirScrubberControlsEntityScript : SpaceMadnessTargetGroupObjectiveEntityScriptBase
    {
        public AirScrubberControlsEntityScript()
            : base(PublicEventObjectiveType.ActivateTargetGroup, 6371u)
        {
        }
    }

    /// <summary>
    /// Build 16042 maps objective 1603 to TargetGroup 6372,
    /// whose Creature2 member is the Engineering Computer 45973.
    /// </summary>
    [ScriptFilterCreatureId(45973u)]
    public class EngineeringComputerEntityScript : SpaceMadnessTargetGroupObjectiveEntityScriptBase
    {
        public EngineeringComputerEntityScript()
            : base(PublicEventObjectiveType.ActivateTargetGroup, 6372u)
        {
        }
    }

    /// <summary>
    /// Build 16042 maps objective 1656 to script object 5628 and the reviewed
    /// phase-5 Hazmat Suit Creature2 row 45981.
    /// </summary>
    [ScriptFilterCreatureId(45981u)]
    public class HazmatSuitEntityScript : IWorldEntityScript, IOwnedScript<IWorldEntity>
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
            entity.Map.PublicEventManager.UpdateObjective(PublicEventObjective.EquipAHazmatSuit, 1);
        }
    }

    /// <summary>
    /// Build 16042 maps objective 1594 to TalkTo TargetGroup 6370, whose
    /// Creature2 members are Hallucinating Venture Worker rows 45903 and 45904.
    /// </summary>
    [ScriptFilterCreatureId(45903u, 45904u)]
    public class HallucinatingVentureWorkerEntityScript : IWorldEntityScript, IOwnedScript<IWorldEntity>
    {
        private const uint AirHelmWorkerTargetGroupId = 6370u;

        private IWorldEntity entity;
        private bool activated;

        public void OnLoad(IWorldEntity owner)
        {
            entity = owner;
        }

        public void OnActivateSuccess(IPlayer player)
        {
            if (activated)
                return;

            activated = true;
            entity.Map.PublicEventManager.UpdateObjective(player, PublicEventObjectiveType.TalkTo, AirHelmWorkerTargetGroupId, 1);
        }
    }

    /// <summary>
    /// Build 16042 maps objective 1590 to nested TargetGroups 6400/6401,
    /// whose Creature2 members are the nightmare rows attacking the workers.
    /// </summary>
    [ScriptFilterCreatureId(
        46123u, 46124u, 46126u, 46127u, 46128u, 46130u, 46131u,
        46132u, 46133u, 46135u, 46137u, 46721u, 58783u, 58798u)]
    public class SavePanickedWorkersNightmareEntityScript : PublicEventObjectiveCreditEntityScript
    {
        public SavePanickedWorkersNightmareEntityScript(
            IFactory<ISpellParameters> spellParametersFactory,
            IGameTableManager gameTableManager)
            : base(spellParametersFactory, gameTableManager, (uint)PublicEventObjective.SavePanickedWorkers)
        {
        }
    }

    /// <summary>
    /// Build 16042 maps objective 4709 to TargetGroup 12595,
    /// whose Creature2 member is the Slinking Slank row 69899.
    /// </summary>
    [ScriptFilterCreatureId(69899u)]
    public class ExperimentalSlankEntityScript : SpaceMadnessTargetGroupObjectiveEntityScriptBase
    {
        public ExperimentalSlankEntityScript()
            : base(PublicEventObjectiveType.ActivateTargetGroup, 12595u, PublicEventObjective.CollectEscapedCreatures)
        {
        }
    }

    /// <summary>
    /// Build 16042 maps objective 4710 to TargetGroup 12596,
    /// whose Creature2 member is the Talking Rockmite row 69900.
    /// </summary>
    [ScriptFilterCreatureId(69900u)]
    public class ExperimentalRockmiteEntityScript : SpaceMadnessTargetGroupObjectiveEntityScriptBase
    {
        public ExperimentalRockmiteEntityScript()
            : base(PublicEventObjectiveType.ActivateTargetGroup, 12596u, PublicEventObjective.CollectEscapedCreatures)
        {
        }
    }

    /// <summary>
    /// Build 16042 maps objective 4711 to TargetGroup 12597,
    /// whose Creature2 member is the Party Down-Grazer row 69901.
    /// </summary>
    [ScriptFilterCreatureId(69901u)]
    public class PartyDowngrazerEntityScript : SpaceMadnessTargetGroupObjectiveEntityScriptBase
    {
        public PartyDowngrazerEntityScript()
            : base(PublicEventObjectiveType.ActivateTargetGroup, 12597u, PublicEventObjective.CollectEscapedCreatures)
        {
        }
    }

    /// <summary>
    /// Build 16042 maps objective 1593 to the reviewed phase-four livestock rows:
    /// Exact Change 3.0 46483 and Blazing Crewman 46714.
    /// </summary>
    [ScriptFilterCreatureId(46483u, 46714u)]
    public class HallucinatingLivestockEntityScript : PublicEventObjectiveCreditEntityScript
    {
        public HallucinatingLivestockEntityScript(
            IFactory<ISpellParameters> spellParametersFactory,
            IGameTableManager gameTableManager)
            : base(spellParametersFactory, gameTableManager, (uint)PublicEventObjective.KillHallucinatingLivestock)
        {
        }
    }
}
