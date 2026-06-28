using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Spell;
using NexusForever.Game.Static.PublicEvent;
using NexusForever.GameTable;
using NexusForever.Script.Template;
using NexusForever.Script.Template.Filter;
using NexusForever.Shared;

namespace NexusForever.Script.Instance.Dungeon.Skullcano.Script
{
    public abstract class SkullcanoObjectiveEntityScriptBase : IWorldEntityScript, IOwnedScript<IWorldEntity>
    {
        private bool activated;

        protected IWorldEntity Entity { get; private set; }

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

        protected int ChecklistIndex()
        {
            return Entity.QuestChecklistIdx;
        }
    }

    /// <summary>
    /// Build 16042 maps objective 335 to TargetGroup 2627,
    /// whose Creature2 member is Grim-Grim Foe Sack 24679.
    /// </summary>
    [ScriptFilterCreatureId(24679u)]
    public class GrimGrimFoeSackEntityScript : SkullcanoObjectiveEntityScriptBase
    {
        public override void OnActivateSuccess(IPlayer _)
        {
            if (!TryActivate())
                return;

            Entity.Map.PublicEventManager.UpdateObjective(
                PublicEventObjectiveType.ActivateTargetGroupChecklist,
                2627u,
                ChecklistIndex());
        }
    }

    /// <summary>
    /// Build 16042 maps objective 333 to TargetGroup 2610,
    /// whose Creature2 member is Gold-covered Treasure 24675.
    /// </summary>
    [ScriptFilterCreatureId(24675u)]
    public class GoldCoveredTreasureEntityScript : SkullcanoObjectiveEntityScriptBase
    {
        public override void OnActivateSuccess(IPlayer _)
        {
            if (!TryActivate())
                return;

            Entity.Map.PublicEventManager.UpdateObjective(
                PublicEventObjectiveType.ActivateTargetGroupChecklist,
                2610u,
                ChecklistIndex());
        }
    }

    /// <summary>
    /// Build 16042 maps public-event objective 338 to TargetGroup 7766,
    /// whose Creature2 members are Gold-Infused Lava Node 24680 and 24921.
    /// </summary>
    [ScriptFilterCreatureId(24680u, 24921u)]
    public class GoldInfusedLavaNodeEntityScript : PublicEventObjectiveCreditEntityScript
    {
        public GoldInfusedLavaNodeEntityScript(
            IFactory<ISpellParameters> spellParametersFactory,
            IGameTableManager gameTableManager)
            : base(spellParametersFactory, gameTableManager, (uint)PublicEventObjective.MineGoldInfusedLavaCores)
        {
        }
    }

    /// <summary>
    /// Build 16042 maps objective 364 to TargetGroup 2673,
    /// whose Creature2 member is Molten Chasm Monitoring Station 25168.
    /// </summary>
    [ScriptFilterCreatureId(25168u)]
    public class MoltenChasmMonitoringStationEntityScript : SkullcanoObjectiveEntityScriptBase
    {
        public override void OnActivateSuccess(IPlayer _)
        {
            if (!TryActivate())
                return;

            Entity.Map.PublicEventManager.UpdateObjective(PublicEventObjectiveType.ActivateTargetGroup, 2673u, 1);
        }
    }

    /// <summary>
    /// Build 16042 maps objective 365 to TargetGroup 2674,
    /// whose Creature2 member is Redmoon Cluster Missile Launch Panel 25230.
    /// </summary>
    [ScriptFilterCreatureId(25230u)]
    public class RedmoonClusterMissileLaunchPanelEntityScript : SkullcanoObjectiveEntityScriptBase
    {
        public override void OnActivateSuccess(IPlayer _)
        {
            if (!TryActivate())
                return;

            Entity.Map.PublicEventManager.UpdateObjective(
                PublicEventObjectiveType.ActivateTargetGroupChecklist,
                2674u,
                ChecklistIndex());
        }
    }

    /// <summary>
    /// Build 16042 maps objective 820 to TalkTo TargetGroup 3944,
    /// whose Creature2 members are Chief Kaskalak rows 33452 and 24788.
    /// </summary>
    [ScriptFilterCreatureId(33452u, 24788u)]
    public class ChiefKaskalakEntityScript : SkullcanoObjectiveEntityScriptBase
    {
        public override void OnActivateSuccess(IPlayer activator)
        {
            if (!TryActivate())
                return;

            Entity.Map.PublicEventManager.UpdateObjective(activator, PublicEventObjectiveType.TalkTo, 3944u, 1);
        }
    }
}
