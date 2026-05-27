using System.Linq;
using System.Numerics;
using Microsoft.Extensions.Logging;
using NexusForever.Game.Abstract.Cinematic;
using NexusForever.Game.Abstract.Cinematic.Cinematics;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Map;
using NexusForever.Game.Abstract.Map.Lock;
using NexusForever.Game.Abstract.Map.Search;
using NexusForever.Game.Static.Quest;
using NexusForever.GameTable;
using NexusForever.GameTable.Model;
using NexusForever.Script.Template;
using NexusForever.Script.Template.Filter;

namespace NexusForever.Script.Main.Quests.CrimsonIsle
{
    /// <summary>
    /// Map script for Crimson Isle (world 870).
    /// Dynamically spawns quest entities when map loads.
    /// </summary>
    [ScriptFilterOwnerId(870)]
    public class CrimsonIsleMapScript : IMapScript, IOwnedScript<IBaseMap>
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

        // Q5573 Powering Down — checklist terminals
        private const uint Q5573TerminalAId = 24219u;
        private const uint Q5573TerminalAWL = 17816u;

        // Q5584 Venomous Intent — creatures
        private const uint Q5584VenomousCreatureId = 24215u;
        private const uint Q5584VenomousCreatureWL = 17796u;

        // Q5594 Last Resistance — ship controls + NPCs
        private const uint Q5594ShipControlsId = 39962u;
        private const uint Q5594ShipControlsWL = 17824u;
        private const uint Q5594NpcId = 24482u;
        private const uint Q5594NpcWL = 18013u;
        private const uint Q5594ObjectiveEntityId = 2810u;
        private const uint Q5594ObjectiveEntityWL = 18014u;

        // Q5604 Tactical Demolitions — cannons
        private const uint Q5604CannonAreaWL = 17867u;

        // Q8855 Stasis Interrupted — entities
        private const uint Q8855StasisAreaWL = 38522u;
        private const uint Q8855TargetAreaWL = 17818u;

        private const ushort Q5593MindTheMinesQuest = 5593;

        private const ushort Q5596OrdnanceRecoveryQuest = 5596;
        private const uint Q5596CrashSiteZoneId = 1611u;
        private const uint Q5596CrashSiteObjective = 8255u;

        // Q5595 — activate entities + kill creatures
        private const uint Q5595ActivationEntityId = 24251u;
        private const uint Q5595ActivationAreaWL = 17837u;
        private const uint Q5595KillCreatureId = 24054u;
        private const uint Q5595KillAreaWL = 39036u;

        private readonly IEntityFactory entityFactory;
        private readonly IGameTableManager gameTableManager;
        private readonly ICinematicFactory cinematicFactory;
        private readonly ILogger<CrimsonIsleMapScript> log;

        private IBaseMap owner;
        private bool entitiesSpawned;

        public CrimsonIsleMapScript(
            ILogger<CrimsonIsleMapScript> log,
            IEntityFactory entityFactory,
            IGameTableManager gameTableManager,
            ICinematicFactory cinematicFactory)
        {
            this.log = log;
            this.entityFactory = entityFactory;
            this.gameTableManager = gameTableManager;
            this.cinematicFactory = cinematicFactory;
        }

        public void OnLoad(IBaseMap owner)
        {
            this.owner = owner;
            EnsureQuestEntities();
        }

        public void Update(double lastTick) { }

        public void OnAddToMap(IGridEntity entity)
        {
            if (entity is not IPlayer player)
                return;

            if (player.QuestManager.GetQuestState(Q5593MindTheMinesQuest) != null)
                return;

            player.CinematicManager.QueueCinematic(cinematicFactory.CreateCinematic<ICrimsonIsleOnCreate>());
        }

        public void OnRemoveFromMap(IGridEntity entity) { }

        public void OnEnterZone(IWorldEntity entity, uint zone)
        {
            if (entity is not IPlayer player)
                return;

            if (zone != Q5596CrashSiteZoneId)
                return;

            if (player.QuestManager.GetQuestState(Q5596OrdnanceRecoveryQuest) != QuestState.Accepted)
                return;

            player.QuestManager.ObjectiveUpdate(Q5596CrashSiteObjective, 1u);
        }

        private void EnsureQuestEntities()
        {
            if (entitiesSpawned || owner == null)
                return;

            SpawnEntityIfMissing<INonPlayerEntity>(Q5573TerminalAId, Q5573TerminalAWL, "Q5573 Terminal A");
            SpawnEntityIfMissing<INonPlayerEntity>(Q5584VenomousCreatureId, Q5584VenomousCreatureWL, "Q5584 Venomous Creature");
            SpawnEntityIfMissing<INonPlayerEntity>(Q5594ShipControlsId, Q5594ShipControlsWL, "Q5594 Ship Controls");
            SpawnEntityIfMissing<INonPlayerEntity>(Q5594NpcId, Q5594NpcWL, "Q5594 NPC");
            SpawnEntityIfMissing<INonPlayerEntity>(Q5594ObjectiveEntityId, Q5594ObjectiveEntityWL, "Q5594 Objective Entity");
            SpawnEntityIfMissing<INonPlayerEntity>(Q5595ActivationEntityId, Q5595ActivationAreaWL, "Q5595 Activation Entity");
            SpawnEntityIfMissing<INonPlayerEntity>(Q5595KillCreatureId, Q5595KillAreaWL, "Q5595 Kill Creature");

            entitiesSpawned = true;
            log.LogDebug("Crimson Isle quest entities initialised on map {MapId}.", owner.Entry.Id);
        }

        private void SpawnEntityIfMissing<T>(uint creatureId, uint worldLocationId, string label) where T : class, IWorldEntity
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

            T entity = entityFactory.CreateEntity<T>();
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
