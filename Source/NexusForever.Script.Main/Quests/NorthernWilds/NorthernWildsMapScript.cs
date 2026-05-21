using System.Linq;
using System.Numerics;
using Microsoft.Extensions.Logging;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Map;
using NexusForever.Game.Abstract.Map.Lock;
using NexusForever.Game.Abstract.Map.Search;
using NexusForever.GameTable;
using NexusForever.GameTable.Model;
using NexusForever.Script.Template;
using NexusForever.Script.Template.Filter;

namespace NexusForever.Script.Main.Quests.NorthernWilds
{
    /// <summary>
    /// Map script for Northern Wilds (world 426).
    /// Dynamically spawns quest entities when map loads.
    /// </summary>
    [ScriptFilterOwnerId(426)]
    public class NorthernWildsMapScript : IMapScript, IOwnedScript<IBaseMap>
    {
        private sealed class TutorialMapInfo : IMapInfo
        {
            public required GameTable.Model.WorldEntry Entry { get; init; }
            public IMapLock MapLock { get; init; }
        }

        private sealed class TutorialMapPosition : IMapPosition
        {
            public required IMapInfo Info { get; init; }
            public Vector3 Position { get; set; }
        }

        private sealed class CreatureSearchCheck : ISearchCheck<IWorldEntity>
        {
            private readonly uint creatureId;

            public CreatureSearchCheck(uint creatureId)
            {
                this.creatureId = creatureId;
            }

            public bool CheckEntity(IWorldEntity entity)
            {
                return entity.CreatureId == creatureId;
            }
        }

        private const uint Q3486LoftiteCrystalId = 11205u;
        private const uint Q3486LoftiteCrystalWL = 7807u;

        private const uint Q3667ControlPanelId = 11194u;
        private const uint Q3667ControlPanelWL = 7778u;

        private const uint Q3480SettlerNpcId = 11070u;
        private const uint Q3480SettlerNpcAreaWL = 12155u;

        private readonly IEntityFactory entityFactory;
        private readonly IGameTableManager gameTableManager;
        private readonly ILogger<NorthernWildsMapScript> log;

        private IBaseMap owner;
        private bool entitiesSpawned;

        public NorthernWildsMapScript(
            ILogger<NorthernWildsMapScript> log,
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
            EnsureQuestEntities();
        }

        public void Update(double lastTick) { }

        public void OnAddToMap(IGridEntity entity) { }
        public void OnRemoveFromMap(IGridEntity entity) { }

        private void EnsureQuestEntities()
        {
            if (entitiesSpawned || owner == null)
                return;

            SpawnEntityIfMissing(Q3486LoftiteCrystalId, Q3486LoftiteCrystalWL, "Q3486 Loftite Crystal");
            SpawnEntityIfMissing(Q3667ControlPanelId, Q3667ControlPanelWL, "Q3667 Control Panel");
            SpawnEntityIfMissing(Q3480SettlerNpcId, Q3480SettlerNpcAreaWL, "Q3480 Settler NPC");

            entitiesSpawned = true;
            log.LogDebug("Northern Wilds quest entities initialised on map {MapId}.", owner.Entry.Id);
        }

        private void SpawnEntityIfMissing(uint creatureId, uint worldLocationId, string label)
        {
            WorldLocation2Entry wl = gameTableManager.WorldLocation2.GetEntry(worldLocationId);
            if (wl == null)
            {
                log.LogWarning("Unable to spawn {Label} on map {MapId}: world location {WorldLocationId} missing.", label, owner.Entry.Id, worldLocationId);
                return;
            }

            Vector3 position = new(wl.Position0, wl.Position1, wl.Position2);
            if (owner.Search(position, 20f, new CreatureSearchCheck(creatureId)).Any())
            {
                log.LogDebug("Skipping {Label} fallback on map {MapId}: creature {CreatureId} is already active near WL {WorldLocationId}.",
                    label, owner.Entry.Id, creatureId, worldLocationId);
                return;
            }

            INonPlayerEntity entity = entityFactory.CreateEntity<INonPlayerEntity>();
            entity.Initialise(creatureId);
            entity.Rotation = Vector3.Zero;

            owner.EnqueueAdd(entity, new TutorialMapPosition
            {
                Info = new TutorialMapInfo { Entry = owner.Entry },
                Position = position
            });

            log.LogDebug("Spawned {Label} (creature {CreatureId}) on map {MapId} at WL {WorldLocationId}.", label, creatureId, owner.Entry.Id, worldLocationId);
        }
    }
}
