using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Entity.Trigger;
using NexusForever.Script.Template;
using NexusForever.Script.Template.Filter;

namespace NexusForever.Script.Instance.Dungeon.SanctuaryOfTheSwordmaiden.Script
{
    [ScriptFilterOwnerId(3421)]
    public class TheTempleOfTheLifeSpeakerGridTriggerScript : IGridEntityScript, IOwnedScript<IGridTriggerEntity>
    {
        private IGridTriggerEntity trigger;

        /// <summary>
        /// Invoked when <see cref="IScript"/> is loaded.
        /// </summary>
        public void OnLoad(IGridTriggerEntity owner)
        {
            trigger = owner;
        }

        /// <summary>
        /// Invoked when <see cref="IGridEntity"/> is added to range check range.
        /// </summary>
        public void OnEnterRange(IGridEntity entity)
        {
            if (entity is not IPlayer)
                return;

            // Build 16042 objective 615 is the Life Speaker temple Turnstile row
            // for objectId 3421. Credit the direct route objective; placement and
            // route timing still need Sanctuary manual smoke.
            trigger.Map.PublicEventManager.UpdateObjective(PublicEventObjective.EnterTheTempleOfTheLifeSpeaker, 1);
        }
    }
}
