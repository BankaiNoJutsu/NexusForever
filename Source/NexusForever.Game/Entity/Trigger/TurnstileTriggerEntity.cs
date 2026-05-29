using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Entity.Trigger;
using NexusForever.Game.Static.PublicEvent;
using NexusForever.Script;

namespace NexusForever.Game.Entity.Trigger
{
    public class TurnstileTriggerEntity : GridTriggerEntity, ITurnstileGridTriggerEntity
    {
        private uint objectId;

        #region Dependency Injection

        public TurnstileTriggerEntity(
            IScriptManager scriptManager)
            : base(scriptManager)
        {
        }

        #endregion

        /// <summary>
        /// Initialise turnstile trigger with supplied id, range and objective object id.
        /// </summary>
        public void Initialise(uint id, float range, uint objectId)
        {
            Initialise(id, range);
            this.objectId = objectId;
        }

        protected override void AddToRange(IGridEntity entity)
        {
            base.AddToRange(entity);

            if (objectId == 0u || entity is not IPlayer)
                return;

            // WildStar64.exe Lua_RegisterPublicEventConstants exposes
            // PublicEventObjectiveType_Turnstile. The row placement and door
            // state transitions stay content-specific until smoke/decompile proof exists.
            Map.PublicEventManager.UpdateObjective(PublicEventObjectiveType.Turnstile, objectId, 1);
        }
    }
}
