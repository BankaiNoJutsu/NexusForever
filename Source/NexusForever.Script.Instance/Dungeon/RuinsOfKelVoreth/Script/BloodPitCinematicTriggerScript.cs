using NexusForever.Game.Abstract.Cinematic;
using NexusForever.Game.Abstract.Cinematic.Cinematics;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Entity.Trigger;
using NexusForever.Game.Abstract.Map.Instance;
using NexusForever.Script.Template;
using NexusForever.Script.Template.Filter;

namespace NexusForever.Script.Instance.Dungeon.RuinsOfKelVoreth.Script
{
    [ScriptFilterOwnerId(445)]
    public class BloodPitCinematicTriggerScript : IGridEntityScript, IOwnedScript<IGridTriggerEntity>
    {
        private readonly ICinematicFactory cinematicFactory;

        private IGridTriggerEntity trigger;

        public BloodPitCinematicTriggerScript(
            ICinematicFactory cinematicFactory)
        {
            this.cinematicFactory = cinematicFactory;
        }

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

            if (trigger.Map is not IMapInstance mapInstance)
                return;

            // WIP-guessed from LaughingWS Instances-and-more: the branch queues an
            // abstract Blood Pit entry cinematic from owner 445 but provides no real
            // payload. Keep this completion-only until Blood Pit cinematic proof lands.
            foreach (IPlayer player in mapInstance.GetPlayers())
                player.CinematicManager.QueueCinematic(cinematicFactory.CreateCinematic<IRuinsOfKelVorethEnter>());
        }
    }
}
