using NexusForever.Game.Abstract.Cinematic;
using NexusForever.Game.Abstract.Cinematic.Cinematics;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Script.Template.Filter;

namespace NexusForever.Script.Instance.WorldStory.JourneyIntoOMNICore1
{
    /// <summary>
    /// WIP-guessed map binding from LaughingWS Instances-and-more. The branch
    /// on-create cinematic hook is wired below, but its payload remains a
    /// completion-only placeholder until the real actor/camera/timing data is mapped.
    /// </summary>
    [ScriptFilterOwnerId(3045)]
    public class JourneyIntoOMNICore1MapScript : EventBaseContentMapScript
    {
        public override uint PublicEventId => 605u;

        #region Dependency Injection

        private readonly ICinematicFactory cinematicFactory;

        public JourneyIntoOMNICore1MapScript(
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

            // WIP-guessed from the branch map hook. The concrete cinematic is an
            // immediate completion-only placeholder until retail payload evidence
            // maps the actual OMNICore camera, actor, text, and visual sequence.
            player.CinematicManager.QueueCinematic(cinematicFactory.CreateCinematic<IJourneyIntoOMNICore1OnCreate>());
        }
    }
}
