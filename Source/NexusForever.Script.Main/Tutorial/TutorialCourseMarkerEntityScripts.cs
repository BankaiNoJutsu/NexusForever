using Microsoft.Extensions.Logging;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Script.Template;
using NexusForever.Script.Template.Filter;

namespace NexusForever.Script.Main.Tutorial
{
    public abstract class TutorialCourseMarkerEntityScriptBase<TScript> : IWorldEntityScript, IOwnedScript<ISimpleEntity>
    {
        private readonly ILogger<TScript> log;

        private ISimpleEntity owner;

        protected TutorialCourseMarkerEntityScriptBase(ILogger<TScript> log)
        {
            this.log = log;
        }

        public void OnLoad(ISimpleEntity owner)
        {
            this.owner = owner;

            log.LogDebug("Loaded Rider's Reef tutorial course marker script {ScriptName}: entity={EntityId}, creature={CreatureId}, checklist={QuestChecklistIdx}, mode={EntityMode}, activePropId={ActivePropId}.",
                GetType().Name,
                owner.EntityId,
                owner.CreatureId,
                owner.QuestChecklistIdx,
                owner.EntityMode,
                owner.ActivePropId);
        }

        public void OnActivateSuccess(IPlayer activator)
        {
            if (activator == null)
                return;

            activator.SyncStarterTutorialEntityVisibility();
            log.LogDebug("Rider's Reef tutorial course marker activation succeeded: script={ScriptName}, player={PlayerGuid}, entity={EntityGuid}, creature={CreatureId}, checklist={QuestChecklistIdx}.",
                GetType().Name,
                activator.Guid,
                owner?.Guid ?? 0u,
                owner?.CreatureId ?? 0u,
                owner?.QuestChecklistIdx ?? 0);
        }

        public void OnActivateFail(IPlayer activator)
        {
            if (activator == null)
                return;

            log.LogDebug("Rider's Reef tutorial course marker activation failed: script={ScriptName}, player={PlayerGuid}, entity={EntityGuid}, creature={CreatureId}, checklist={QuestChecklistIdx}.",
                GetType().Name,
                activator.Guid,
                owner?.Guid ?? 0u,
                owner?.CreatureId ?? 0u,
                owner?.QuestChecklistIdx ?? 0);
        }
    }

    [ScriptFilterScriptName("DirectionArrowEntityScript")]
    public class DirectionArrowEntityScript : TutorialCourseMarkerEntityScriptBase<DirectionArrowEntityScript>
    {
        public DirectionArrowEntityScript(ILogger<DirectionArrowEntityScript> log)
            : base(log)
        {
        }
    }

    [ScriptFilterScriptName("DirectionArrowPt2EntityScript")]
    public class DirectionArrowPt2EntityScript : TutorialCourseMarkerEntityScriptBase<DirectionArrowPt2EntityScript>
    {
        public DirectionArrowPt2EntityScript(ILogger<DirectionArrowPt2EntityScript> log)
            : base(log)
        {
        }
    }

    [ScriptFilterScriptName("ObjectiveRingEntityScript")]
    public class ObjectiveRingEntityScript : TutorialCourseMarkerEntityScriptBase<ObjectiveRingEntityScript>
    {
        public ObjectiveRingEntityScript(ILogger<ObjectiveRingEntityScript> log)
            : base(log)
        {
        }
    }

    [ScriptFilterScriptName("ObjectiveRingPt2EntityScript")]
    public class ObjectiveRingPt2EntityScript : TutorialCourseMarkerEntityScriptBase<ObjectiveRingPt2EntityScript>
    {
        public ObjectiveRingPt2EntityScript(ILogger<ObjectiveRingPt2EntityScript> log)
            : base(log)
        {
        }
    }
}
