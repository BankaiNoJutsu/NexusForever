using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Static.PublicEvent;
using NexusForever.Script.Template;
using NexusForever.Script.Template.Filter;

namespace NexusForever.Script.Instance.Dungeon.RuinsOfKelVoreth.Script
{
    public abstract class RuinsOfKelVorethObjectiveEntityScriptBase : IWorldEntityScript, IOwnedScript<IWorldEntity>
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
    /// Build 16042 maps objective 456 to TargetGroup 4422,
    /// whose Creature2 member is Eldan Schematic Data Storage 33155.
    /// </summary>
    [ScriptFilterCreatureId(33155u)]
    public class EldanSchematicDataStorageEntityScript : RuinsOfKelVorethObjectiveEntityScriptBase
    {
        public override void OnActivateSuccess(IPlayer _)
        {
            if (!TryActivate())
                return;

            Entity.Map.PublicEventManager.UpdateObjective(
                PublicEventObjectiveType.ActivateTargetGroupChecklist,
                4422u,
                ChecklistIndex());
        }
    }

    /// <summary>
    /// Build 16042 maps objective 451 to TargetGroup 3916,
    /// whose Creature2 member is Kel Voreth War Supplies 33334.
    /// </summary>
    [ScriptFilterCreatureId(33334u)]
    public class KelVorethWarSuppliesEntityScript : RuinsOfKelVorethObjectiveEntityScriptBase
    {
        public override void OnActivateSuccess(IPlayer _)
        {
            if (!TryActivate())
                return;

            Entity.Map.PublicEventManager.UpdateObjective(
                PublicEventObjectiveType.ActivateTargetGroupChecklist,
                3916u,
                ChecklistIndex());
        }
    }

    /// <summary>
    /// Build 16042 maps objective 459 to TargetGroup 4022,
    /// whose Creature2 member is Eldan Phase Monitor 33768.
    /// </summary>
    [ScriptFilterCreatureId(33768u)]
    public class EldanPhaseMonitorEntityScript : RuinsOfKelVorethObjectiveEntityScriptBase
    {
        public override void OnActivateSuccess(IPlayer _)
        {
            if (!TryActivate())
                return;

            Entity.Map.PublicEventManager.UpdateObjective(
                PublicEventObjectiveType.ActivateTargetGroupChecklist,
                4022u,
                ChecklistIndex());
        }
    }

    /// <summary>
    /// Build 16042 maps objective 461 to TargetGroup 3903,
    /// whose Creature2 member is Kel Voreth Forge 33302.
    /// </summary>
    [ScriptFilterCreatureId(33302u)]
    public class KelVorethForgeEntityScript : RuinsOfKelVorethObjectiveEntityScriptBase
    {
        public override void OnActivateSuccess(IPlayer _)
        {
            if (!TryActivate())
                return;

            Entity.Map.PublicEventManager.UpdateObjective(
                PublicEventObjectiveType.ActivateTargetGroupChecklist,
                3903u,
                ChecklistIndex());
        }
    }
}
