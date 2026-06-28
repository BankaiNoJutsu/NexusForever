using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Entity.Trigger;
using NexusForever.Script.Template;
using NexusForever.Script.Template.Filter;

namespace NexusForever.Script.Instance.Expedition.FragmentZero.Script
{
    public abstract class FragmentZeroSearchGridTriggerScriptBase : IGridEntityScript, IOwnedScript<IGridTriggerEntity>
    {
        private IGridTriggerEntity trigger;
        private bool entered;

        protected IGridTriggerEntity Trigger => trigger;

        public void OnLoad(IGridTriggerEntity owner)
        {
            trigger = owner;
        }

        public void OnEnterRange(IGridEntity entity)
        {
            if (entity is not IPlayer)
                return;

            if (entered)
                return;

            entered = true;
            OnPlayerEnterRange();
        }

        protected abstract void OnPlayerEnterRange();
    }

    /// <summary>
    /// Build 16042 maps objective 4421 to a zero-count Script row for object
    /// 7892 at WorldLocation2 48521, QuestDirection 2308.
    /// </summary>
    [ScriptFilterOwnerId(7892u)]
    public class FragmentZeroIncubationSearchGridTriggerScript : FragmentZeroSearchGridTriggerScriptBase
    {
        protected override void OnPlayerEnterRange()
        {
            Trigger.Map.PublicEventManager.UpdateObjective(PublicEventObjective.SearchInsideTheIncubationComplexForJo, 0);
        }
    }

    /// <summary>
    /// Build 16042 maps objective 4420 to a zero-count Script row for object
    /// 7919 at WorldLocation2 48732, QuestDirection 2340.
    /// </summary>
    [ScriptFilterOwnerId(7919u)]
    public class FragmentZeroBiomaticsSearchGridTriggerScript : FragmentZeroSearchGridTriggerScriptBase
    {
        protected override void OnPlayerEnterRange()
        {
            Trigger.Map.PublicEventManager.UpdateObjective(PublicEventObjective.SearchInsideTheBiomaticsChamberForSyrus, 0);
        }
    }
}
