using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Static.PublicEvent;
using NexusForever.Script.Template;
using NexusForever.Script.Template.Filter;

namespace NexusForever.Script.Instance.Expedition.DeepSpaceExploration.Script
{
    public abstract class DeepSpaceTalkObjectiveEntityScriptBase : IWorldEntityScript, IOwnedScript<IWorldEntity>
    {
        private readonly PublicEventObjectiveType objectiveType;
        private readonly uint targetGroupId;

        private IWorldEntity entity;
        private bool activated;

        protected DeepSpaceTalkObjectiveEntityScriptBase(
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

        public void OnActivateSuccess(IPlayer player)
        {
            if (activated)
                return;

            activated = true;
            entity.Map.PublicEventManager.UpdateObjective(player, objectiveType, targetGroupId, GetObjectiveUpdateCount());
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
    /// Build 16042 maps objective 1844 to TargetGroup 6873,
    /// whose Creature2 member is Captain Tyrania 48900.
    /// </summary>
    [ScriptFilterCreatureId(48900u)]
    public class CaptainTyraniaEntityScript : DeepSpaceTalkObjectiveEntityScriptBase
    {
        public CaptainTyraniaEntityScript()
            : base(PublicEventObjectiveType.TalkTo, 6873u)
        {
        }
    }

    /// <summary>
    /// Build 16042 maps objective 1968 to TalkToChecklist TargetGroup 6966,
    /// whose Creature2 members are the Galactic Observer crew rows 49474-49480.
    /// </summary>
    [ScriptFilterCreatureId(49474u, 49475u, 49476u, 49477u, 49479u, 49480u)]
    public class GalacticObserverCrewMemberEntityScript : DeepSpaceTalkObjectiveEntityScriptBase
    {
        public GalacticObserverCrewMemberEntityScript()
            : base(PublicEventObjectiveType.TalkToChecklist, 6966u)
        {
        }
    }
}
