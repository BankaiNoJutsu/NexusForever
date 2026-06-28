using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Static.PublicEvent;
using NexusForever.Script.Template;
using NexusForever.Script.Template.Filter;

namespace NexusForever.Script.Instance.Dungeon.SanctuaryOfTheSwordmaiden.Script
{
    public abstract class SanctuaryObjectiveEntityScriptBase : IWorldEntityScript, IOwnedScript<IWorldEntity>
    {
        protected IWorldEntity Entity { get; private set; }

        private bool activated;

        public void OnLoad(IWorldEntity owner)
        {
            Entity = owner;
        }

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
    /// Build 16042 maps public-event objective 481 to TargetGroup 3193, whose
    /// Creature2 members are Torine Spirit-Relics 28638, 28643, 28652, and 28644.
    /// </summary>
    [ScriptFilterCreatureId(28638u, 28643u, 28652u, 28644u)]
    public class TorineSpiritRelicEntityScript : SanctuaryObjectiveEntityScriptBase
    {
        public void OnActivateSuccess(IPlayer _)
        {
            if (!TryActivate())
                return;

            Entity.Map.PublicEventManager.UpdateObjective(
                PublicEventObjectiveType.ActivateTargetGroupChecklist,
                3193u,
                ChecklistIndex());
        }
    }

    /// <summary>
    /// Build 16042 maps public-event objective 482 to TargetGroup 3164,
    /// whose Creature2 member is Torine Spirit-Relic Holder 28459.
    /// </summary>
    [ScriptFilterCreatureId(28459u)]
    public class TorineSpiritRelicHolderEntityScript : SanctuaryObjectiveEntityScriptBase
    {
        public void OnActivateSuccess(IPlayer _)
        {
            if (!TryActivate())
                return;

            Entity.Map.PublicEventManager.UpdateObjective(
                PublicEventObjectiveType.ActivateTargetGroupChecklist,
                3164u,
                ChecklistIndex());
        }
    }

    /// <summary>
    /// Build 16042 maps public-event objective 496 to TargetGroup 5755,
    /// whose Creature2 member is Life-Weaver Tech Cluster 43171.
    /// </summary>
    [ScriptFilterCreatureId(43171u)]
    public class LifeweaverTechClusterEntityScript : SanctuaryObjectiveEntityScriptBase
    {
        public void OnActivateSuccess(IPlayer _)
        {
            if (!TryActivate())
                return;

            Entity.Map.PublicEventManager.UpdateObjective(
                PublicEventObjectiveType.ActivateTargetGroupChecklist,
                5755u,
                ChecklistIndex());
        }
    }

    /// <summary>
    /// Build 16042 maps public-event objective 497 to Script objective credit
    /// from the activatable Soul Spore / Spirit Bomb Creature2 row 70947.
    /// </summary>
    [ScriptFilterCreatureId(70947u)]
    public class SoulSporeEntityScript : SanctuaryObjectiveEntityScriptBase
    {
        public void OnActivateSuccess(IPlayer _)
        {
            if (!TryActivate())
                return;

            Entity.Map.PublicEventManager.UpdateObjective(
                PublicEventObjective.UseTheSoulSporeOnMoldwoodGorgers,
                1);
        }
    }

    /// <summary>
    /// Build 16042 maps public-event objective 499 to TargetGroup 5756,
    /// whose Creature2 member is Torine Totem of Flame 43173.
    /// </summary>
    [ScriptFilterCreatureId(43173u)]
    public class TorineTotemOfFlameEntityScript : SanctuaryObjectiveEntityScriptBase
    {
        public void OnActivateSuccess(IPlayer _)
        {
            if (!TryActivate())
                return;

            Entity.Map.PublicEventManager.UpdateObjective(
                PublicEventObjectiveType.ActivateTargetGroupChecklist,
                5756u,
                ChecklistIndex());
        }
    }
}
