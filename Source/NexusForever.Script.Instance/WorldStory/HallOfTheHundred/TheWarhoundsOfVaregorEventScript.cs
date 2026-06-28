using System.Numerics;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Entity.Trigger;
using NexusForever.Game.Abstract.Map.Instance;
using NexusForever.Game.Abstract.PublicEvent;
using NexusForever.Game.Static.PublicEvent;
using NexusForever.Script.Template;
using NexusForever.Script.Template.Filter;

namespace NexusForever.Script.Instance.WorldStory.HallOfTheHundred
{
    /// <summary>
    /// Restores mapped Hall of the Hundred trigger and kill objectives for
    /// public event 693 ("The Warhounds of Varegor").
    /// </summary>
    [ScriptFilterOwnerId(693)]
    public class TheWarhoundsOfVaregorEventScript : IPublicEventScript, IOwnedScript<IPublicEvent>
    {
        private const uint MainPublicEventId = 666u;
        private const uint ExploreWorldLocationId = 48660u;
        private const uint ExploreObjectId = 8426u;
        private const uint ExploreFurtherWorldLocationId = 48661u;
        private const uint ExploreFurtherObjectId = 7881u;

        private static readonly Vector3 ExplorePosition = new(-295.518f, -679.848f, -1181.76f);
        private static readonly Vector3 ExploreFurtherPosition = new(-429.241f, -679.465f, -1269.96f);

        private IPublicEvent publicEvent;
        private IMapInstance mapInstance;
        private uint exploreTriggerGuid;
        private uint exploreFurtherTriggerGuid;
        private bool defeatWatchhoundPhaseStarted;
        private bool exploreFurtherPhaseStarted;

        /// <summary>
        /// Invoked when <see cref="IScript"/> is loaded.
        /// </summary>
        public void OnLoad(IPublicEvent owner)
        {
            publicEvent = owner;
            mapInstance = publicEvent.Map as IMapInstance
                ?? throw new InvalidOperationException("The Warhounds of Varegor requires a map instance.");

            publicEvent.SetPhase(PublicEventPhase.ExploreWarhoundKennel);
        }

        /// <summary>
        /// Invoked when a <see cref="IGridEntity"/> is added to the map the public event is on.
        /// </summary>
        public void OnAddToMap(IGridEntity entity)
        {
            if (entity is not IWorldLocationVolumeGridTriggerEntity worldLocationEntity)
                return;

            switch (worldLocationEntity.Entry.Id)
            {
                case ExploreWorldLocationId:
                    exploreTriggerGuid = worldLocationEntity.Guid;
                    break;
                case ExploreFurtherWorldLocationId:
                    exploreFurtherTriggerGuid = worldLocationEntity.Guid;
                    break;
            }
        }

        /// <summary>
        /// Invoked when a <see cref="IGridEntity"/> is removed from the map the public event is on.
        /// </summary>
        public void OnRemoveFromMap(IGridEntity entity)
        {
            if (entity is not IWorldLocationVolumeGridTriggerEntity worldLocationEntity)
                return;

            switch (worldLocationEntity.Entry.Id)
            {
                case ExploreWorldLocationId:
                    exploreTriggerGuid = 0;
                    break;
                case ExploreFurtherWorldLocationId:
                    exploreFurtherTriggerGuid = 0;
                    break;
            }
        }

        /// <summary>
        /// Invoked when the public event phase changes.
        /// </summary>
        public void OnPublicEventPhase(uint phase)
        {
            switch ((PublicEventPhase)phase)
            {
                case PublicEventPhase.ExploreWarhoundKennel:
                    OnPhaseExploreWarhoundKennel();
                    break;
                case PublicEventPhase.DefeatVaregorWatchhound:
                    OnPhaseDefeatVaregorWatchhound();
                    break;
                case PublicEventPhase.ExploreFurtherIntoWarhoundKennel:
                    OnPhaseExploreFurtherIntoWarhoundKennel();
                    break;
            }
        }

        private void OnPhaseExploreWarhoundKennel()
        {
            // Objective 4403 is ParticipantsInTriggerVolume object 8426 at
            // WorldLocation2 48660. Objective 5294 is the main-event Script
            // parent marker at the same location.
            mapInstance.PublicEventManager?.GetEvent(MainPublicEventId)
                ?.ActivateObjective(PublicEventObjective.ExploreWarhoundKennelParent);
            publicEvent.ActivateObjective(PublicEventObjective.ExploreWarhoundKennel);
            CreateTrigger(ExploreWorldLocationId, ExploreObjectId, ExplorePosition);
        }

        private void OnPhaseDefeatVaregorWatchhound()
        {
            // Objective 4404 is KillEventObjectiveUnit for Creature2 67884
            // (Varegor Watchhound) at WorldLocation2 51164.
            publicEvent.ActivateObjective(PublicEventObjective.DefeatVaregorWatchhound);
        }

        private void OnPhaseExploreFurtherIntoWarhoundKennel()
        {
            // Objective 4434 is ParticipantsInTriggerVolume object 7881 at
            // WorldLocation2 48661. Objective 4441 is KillClusterEventObjectiveUnit
            // object 12262, whose TargetGroup member is Creature2 67940 (Frozen
            // Lever); the named Warhound spawn/combat choreography remains blocked.
            publicEvent.ActivateObjective(PublicEventObjective.ExploreFurtherIntoWarhoundKennel);
            publicEvent.ActivateObjective(PublicEventObjective.DefeatWarhoundPack);
            CreateTrigger(ExploreFurtherWorldLocationId, ExploreFurtherObjectId, ExploreFurtherPosition);
        }

        private void CreateTrigger(uint worldLocationId, uint objectId, Vector3 position)
        {
            IWorldLocationVolumeGridTriggerEntity triggerEntity = publicEvent.CreateEntity<IWorldLocationVolumeGridTriggerEntity>();
            triggerEntity.Initialise(worldLocationId, objectId);
            mapInstance.EnqueueAdd(triggerEntity, new ScriptMapPosition
            {
                Info = new ScriptMapInfo
                {
                    Entry   = mapInstance.Entry,
                    MapLock = mapInstance.MapLock
                },
                Position = position
            });
        }

        public void OnPublicEventObjectiveStatus(IPublicEventObjective objective)
        {
            if (objective.Status != PublicEventStatus.Succeeded)
                return;

            switch ((PublicEventObjective)objective.Entry.Id)
            {
                case PublicEventObjective.ExploreWarhoundKennel:
                    RemoveTrigger(exploreTriggerGuid);
                    StartDefeatWatchhoundPhase();
                    break;
                case PublicEventObjective.DefeatVaregorWatchhound:
                    StartExploreFurtherPhase();
                    break;
                case PublicEventObjective.ExploreFurtherIntoWarhoundKennel:
                    RemoveTrigger(exploreFurtherTriggerGuid);
                    break;
            }
        }

        private void StartDefeatWatchhoundPhase()
        {
            if (defeatWatchhoundPhaseStarted)
                return;

            defeatWatchhoundPhaseStarted = true;
            publicEvent.SetPhase(PublicEventPhase.DefeatVaregorWatchhound);
        }

        private void StartExploreFurtherPhase()
        {
            if (exploreFurtherPhaseStarted)
                return;

            exploreFurtherPhaseStarted = true;
            publicEvent.SetPhase(PublicEventPhase.ExploreFurtherIntoWarhoundKennel);
        }

        private void RemoveTrigger(uint guid)
        {
            IWorldLocationVolumeGridTriggerEntity triggerEntity = mapInstance.GetEntity<IWorldLocationVolumeGridTriggerEntity>(guid);
            triggerEntity?.RemoveFromMap();
        }
    }
}
