using NexusForever.Game.Abstract.Cinematic;
using NexusForever.Game.Abstract.Cinematic.Cinematics;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Script.Template.Filter;

namespace NexusForever.Script.Instance.Expedition.SpaceMadness
{
    [ScriptFilterOwnerId(2149)]
    public class SpaceMadnessMapScript : EventBaseContentMapScript
    {
        public override uint PublicEventId => 390u;

        #region Dependency Injection

        private readonly ICinematicFactory cinematicFactory;

        public SpaceMadnessMapScript(
            ICinematicFactory cinematicFactory)
        {
            this.cinematicFactory = cinematicFactory;
        }

        #endregion

        /// <summary>
        /// Invoked when <see cref="IGridEntity"/> is added to map.
        /// </summary>
        public override void OnAddToMap(IGridEntity entity)
        {
            base.OnAddToMap(entity);

            if (entity is not IPlayer player)
                return;

            // WIP-guessed from LaughingWS Instances-and-more: the branch queues
            // this on-create cinematic on player entry, but the real cinematic
            // payload is still unknown and represented by an immediate placeholder.
            player.CinematicManager.QueueCinematic(cinematicFactory.CreateCinematic<ISpaceMadnessOnCreate>());
        }
    }
}
