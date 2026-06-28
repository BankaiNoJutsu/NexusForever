using System.Linq;
using System.Numerics;
using Microsoft.Extensions.Logging;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Entity.Trigger;
using NexusForever.Game.Abstract.Map;
using NexusForever.Game.Abstract.Map.Lock;
using NexusForever.Game.Abstract.Map.Search;
using NexusForever.GameTable;
using NexusForever.GameTable.Model;
using NexusForever.Script.Template;
using NexusForever.Script.Template.Filter;

namespace NexusForever.Script.Main.Quests.Galeras
{
    /// <summary>
    /// Map script for Galeras (world 51).
    /// </summary>
    [ScriptFilterOwnerId(51)]
    public class GalerasMapScript : IMapScript, IOwnedScript<IBaseMap>
    {
        private sealed class GalerasMapInfo : IMapInfo
        {
            public required WorldEntry Entry { get; init; }
            public IMapLock MapLock { get; init; }
        }

        private sealed class GalerasMapPosition : IMapPosition
        {
            public required IMapInfo Info { get; init; }
            public Vector3 Position { get; set; }
        }

        private sealed class WorldLocationTriggerSearchCheck : ISearchCheck<IWorldLocationVolumeGridTriggerEntity>
        {
            private readonly uint worldLocationId;

            public WorldLocationTriggerSearchCheck(uint worldLocationId)
            {
                this.worldLocationId = worldLocationId;
            }

            public bool CheckEntity(IWorldLocationVolumeGridTriggerEntity entity)
            {
                return entity.Entry?.Id == worldLocationId;
            }
        }

        private const uint Q4696TempleRetreatWL = 12649u;

        private readonly ILogger<GalerasMapScript> log;
        private readonly IEntityFactory entityFactory;
        private readonly IGameTableManager gameTableManager;

        private IBaseMap owner;
        private bool entitiesSpawned;

        public GalerasMapScript(
            ILogger<GalerasMapScript> log,
            IEntityFactory entityFactory,
            IGameTableManager gameTableManager)
        {
            this.log = log;
            this.entityFactory = entityFactory;
            this.gameTableManager = gameTableManager;
        }

        public void OnLoad(IBaseMap owner)
        {
            this.owner = owner;
            EnsureMapTriggers();
        }

        private void EnsureMapTriggers()
        {
            if (entitiesSpawned || owner == null)
                return;

            SpawnWorldLocationTriggerIfAvailable(Q4696TempleRetreatWL, "Q4696 Temple of Osiric retreat trigger");

            entitiesSpawned = true;
            log.LogDebug("Galeras map triggers initialised on map {MapId}.", owner.Entry.Id);
        }

        private void SpawnWorldLocationTriggerIfAvailable(uint worldLocationId, string label)
        {
            WorldLocation2Entry wl = gameTableManager.WorldLocation2.GetEntry(worldLocationId);
            if (wl == null)
            {
                log.LogWarning("Unable to spawn {Label} on map {MapId}: world location {WorldLocationId} missing.", label, owner.Entry.Id, worldLocationId);
                return;
            }

            Vector3 position = new(wl.Position0, wl.Position1, wl.Position2);
            if (owner.Search(position, 20f, new WorldLocationTriggerSearchCheck(worldLocationId)).Any())
            {
                log.LogDebug("Skipping {Label} on map {MapId}: world location trigger {WorldLocationId} is already active near {Position}.",
                    label, owner.Entry.Id, worldLocationId, position);
                return;
            }

            IWorldLocationVolumeGridTriggerEntity trigger = entityFactory.CreateEntity<IWorldLocationVolumeGridTriggerEntity>();
            trigger.Initialise(worldLocationId, 0u);

            owner.EnqueueAdd(trigger, new GalerasMapPosition
            {
                Info = new GalerasMapInfo { Entry = owner.Entry },
                Position = position
            });

            log.LogDebug("Spawned {Label} on map {MapId} at WL {WorldLocationId}.", label, owner.Entry.Id, worldLocationId);
        }
    }
}
