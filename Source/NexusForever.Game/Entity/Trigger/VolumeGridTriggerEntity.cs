using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Entity.Trigger;
using NexusForever.Game.Static.PublicEvent;
using NexusForever.Script;

namespace NexusForever.Game.Entity.Trigger
{
    public class VolumeGridTriggerEntity : GridTriggerEntity, IVolumeGridTriggerEntity
    {
        private uint objectId;

        #region Dependency Injection

        public VolumeGridTriggerEntity(
            IScriptManager scriptManager)
            : base(scriptManager)
        {
        }

        #endregion

        /// <summary>
        /// Initialise volume trigger with supplied id, range and objective object id.
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
            // PublicEventObjectiveType_ParticipantsInTriggerVolume; exact branch
            // trigger rows/radii remain content-smoke gated.
            Map.PublicEventManager.UpdateObjective(PublicEventObjectiveType.ParticipantsInTriggerVolume, objectId, 1);
        }

        protected override void RemoveFromRange(IGridEntity entity)
        {
            if (objectId != 0u && entity is IPlayer)
            {
                // Keep leave semantics paired with the mapped trigger-volume objective
                // producer while broader cleanup/respawn timing remains LWS-074 evidence-gated.
                Map.PublicEventManager.UpdateObjective(PublicEventObjectiveType.ParticipantsInTriggerVolume, objectId, -1);
            }

            base.RemoveFromRange(entity);
        }
    }
}
