using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Static.PublicEvent;
using NexusForever.Script.Template;
using NexusForever.Script.Template.Filter;

namespace NexusForever.Script.Instance.Dungeon.ColdbloodCitadel.Script
{
    public abstract class ColdbloodTargetGroupChecklistObjectiveEntityScriptBase : IWorldEntityScript, IOwnedScript<IWorldEntity>
    {
        protected IWorldEntity Entity { get; private set; }

        private bool activated;

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
    /// Build 16042 maps objective 5317 to TargetGroup 14473, whose Creature2
    /// members are the Soulfrost Shards rows 75731, 75732, and 75733.
    /// </summary>
    [ScriptFilterCreatureId(75731u, 75732u, 75733u)]
    public class SoulfrostShardsEntityScript : ColdbloodTargetGroupChecklistObjectiveEntityScriptBase
    {
        public override void OnActivateSuccess(IPlayer _)
        {
            if (!TryActivate())
                return;

            Entity.Map.PublicEventManager.UpdateObjective(
                PublicEventObjectiveType.ActivateTargetGroupChecklist,
                14473u,
                ChecklistIndex());
        }
    }

    /// <summary>
    /// Build 16042 maps objective 5318 to TargetGroup 14471, whose Creature2
    /// member is Soulrot Container 75708.
    /// </summary>
    [ScriptFilterCreatureId(75708u)]
    public class SoulrotCanisterEntityScript : ColdbloodTargetGroupChecklistObjectiveEntityScriptBase
    {
        public override void OnActivateSuccess(IPlayer _)
        {
            if (!TryActivate())
                return;

            Entity.Map.PublicEventManager.UpdateObjective(
                PublicEventObjectiveType.ActivateTargetGroupChecklist,
                14471u,
                ChecklistIndex());
        }
    }

    /// <summary>
    /// Build 16042 maps objective 5319 to TargetGroup 14474, whose Creature2
    /// member is the Winterfury Prisoner Cage row 75737.
    /// </summary>
    [ScriptFilterCreatureId(75737u)]
    public class WinterfuryPrisonerCageEntityScript : ColdbloodTargetGroupChecklistObjectiveEntityScriptBase
    {
        public override void OnActivateSuccess(IPlayer _)
        {
            if (!TryActivate())
                return;

            Entity.Map.PublicEventManager.UpdateObjective(
                PublicEventObjectiveType.ActivateTargetGroupChecklist,
                14474u,
                ChecklistIndex());
        }
    }

    /// <summary>
    /// Build 16042 maps objective 5320 to TargetGroup 14475, whose Creature2
    /// member is the Krovak Trap row 75747.
    /// </summary>
    [ScriptFilterCreatureId(75747u)]
    public class KrovakTrapEntityScript : ColdbloodTargetGroupChecklistObjectiveEntityScriptBase
    {
        public override void OnActivateSuccess(IPlayer _)
        {
            if (!TryActivate())
                return;

            Entity.Map.PublicEventManager.UpdateObjective(
                PublicEventObjectiveType.ActivateTargetGroupChecklist,
                14475u,
                ChecklistIndex());
        }
    }

    /// <summary>
    /// Build 16042 maps objective 5337 to TargetGroup 14476, whose Creature2
    /// member is the Liquid Soulfrost sample container row 75736.
    /// </summary>
    [ScriptFilterCreatureId(75736u)]
    public class LiquidSoulfrostSampleEntityScript : ColdbloodTargetGroupChecklistObjectiveEntityScriptBase
    {
        public override void OnActivateSuccess(IPlayer _)
        {
            if (!TryActivate())
                return;

            Entity.Map.PublicEventManager.UpdateObjective(
                PublicEventObjectiveType.ActivateTargetGroupChecklist,
                14476u,
                ChecklistIndex());
        }
    }
}
