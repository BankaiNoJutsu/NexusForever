using System.Numerics;
using Microsoft.Extensions.Logging;
using NexusForever.Game.Abstract.Cinematic.Cinematics;
using NexusForever.Game.Abstract.Cinematic;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Entity.Trigger;
using NexusForever.Game.Abstract.Map;
using NexusForever.Game.Abstract.Map.Lock;
using NexusForever.Game.Abstract.Map.Search;
using NexusForever.Game.Abstract.Quest;
using NexusForever.Game.Static.Quest;
using NexusForever.Game.Static.Reputation;
using NexusForever.GameTable;
using NexusForever.GameTable.Model;
using NexusForever.Script.Template;
using NexusForever.Script.Template.Filter;
using static NexusForever.Game.Static.Tutorial.StarterTutorialDefinition;

namespace NexusForever.Script.Main.Tutorial
{
    [ScriptFilterOwnerId(3460)]
    public class TutorialMapScript : IMapScript, IOwnedScript<IBaseMap>
    {
        private sealed class MatchAllPlayerSearchCheck : ISearchCheck<IPlayer>
        {
            public bool CheckEntity(IPlayer entity)
            {
                return true;
            }
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

        private const uint ExileCombatFinalWorldLocationId = 51740u;
        private const uint DominionCombatMineEasyWorldLocationId = 52899u;
        private const uint DominionCombatMineMediumWorldLocationId = 52900u;
        private const uint DominionCombatMineHardWorldLocationId = 52901u;
        private const uint DominionCombatTurretWorldLocationId00 = 52902u;
        private const uint DominionCombatTurretWorldLocationId01 = 52903u;
        private const uint DominionCombatEliteWorldLocationId = 53015u;
        private const uint ExileCombatMineEasyWorldLocationId = 51662u;
        private const uint ExileCombatMineMediumWorldLocationId = 51663u;
        private const uint ExileCombatMineHardWorldLocationId = 51664u;
        private const uint ExileCombatTurretWorldLocationId00 = 51671u;
        private const uint ExileCombatTurretWorldLocationId01 = 52753u;
        private const uint ExileCombatDagunCreatureId = 73464u;
        private const uint DominionCombatDagunCreatureId = 73465u;
        private const uint ExileCombatTurretCreatureId = 73494u;
        private const uint DominionCombatTurretCreatureId = 74862u;

        // Post-combat ship deck (zone 5968): quests 10519/10522 NPCs
        private const uint ShipDeckNpcA = 73498u;
        private const uint ShipDeckNpcB = 74745u;
        private const uint ShipDeckNpcC = 74746u;
        private const uint ShipDeckNpcAWL = 52742u;
        private const uint ShipDeckNpcBWL = 52743u;
        private const uint ShipDeckNpcCWL = 52744u;

        // Ship interior (zone 5968): quests 10520/10523 kill target
        private const uint ShipInteriorCreatureId = 73499u;
        private const uint ShipInteriorCreatureWL = 51744u;

        // Cryopod deck (zone 5969): quests 10525/10526 NPCs
        private const uint CryopodNpcA = 73663u;
        private const uint CryopodNpcB = 73664u;
        private const uint CryopodNpcAWL = 51711u;
        private const uint CryopodNpcBWL = 51712u;

        // Exile departure (zone 5998): quest 10528 NPC + escape pod consoles + checklist entities
        private const uint ExileEscapePodNpcId = 73604u;
        private const uint ExileEscapePodNpcWL = 51694u;
        private const uint ExileEscapePodConsoleAWL = 51695u;
        private const uint ExileEscapePodConsoleBWL = 51696u;
        private const uint ExileEscapePodConsoleCWL = 51697u;
        private static readonly uint[] ExileEscapePodChecklistCreatureIds = [73677u, 73678u, 73679u, 73680u, 73681u, 73682u, 73683u, 73684u];

        // Dominion departure (zone 5999): quest 10530 NPC + escape pod consoles + checklist entities
        private const uint DominionEscapePodNpcId = 74778u;
        private const uint DominionEscapePodNpcWL = 52750u;
        private const uint DominionEscapePodConsoleAWL = 52747u;
        private const uint DominionEscapePodConsoleBWL = 52748u;
        private const uint DominionEscapePodConsoleCWL = 52749u;
        private static readonly uint[] DominionEscapePodChecklistCreatureIds = [74759u, 74760u, 74761u, 74762u, 74763u, 74764u, 74765u, 74766u];

        // Cryopod target-group NPCs for TalkToTargetGroup (quests 10525/10526)
        private const uint CryopodTargetGroupNpcA = 73422u;
        private const uint CryopodTargetGroupNpcB = 73662u;

        // Ship interior checklist entity (quests 10520/10523, target group 14425)
        private const uint ShipInteriorChecklistCreatureId = 73500u;

        private static readonly MatchAllPlayerSearchCheck matchAllPlayers = new();

        private IBaseMap owner;
        private bool tutorialTriggersSpawned;
        private bool exileCombatLaneSpawned;
        private bool dominionCombatLaneSpawned;
        private bool shipDeckNpcsSpawned;
        private bool shipInteriorEntitiesSpawned;
        private bool cryopodNpcsSpawned;
        private bool exileDepartureEntitiesSpawned;
        private bool dominionDepartureEntitiesSpawned;
        private readonly HashSet<uint> initialisedPlayerGuids = [];

        #region Dependency Injection

        private readonly ICinematicFactory cinematicFactory;
        private readonly IEntityFactory entityFactory;
        private readonly IGlobalQuestManager globalQuestManager;
        private readonly IGameTableManager gameTableManager;
        private readonly ILogger<TutorialMapScript> log;

        public TutorialMapScript(
            ILogger<TutorialMapScript> log,
            ICinematicFactory cinematicFactory,
            IEntityFactory entityFactory,
            IGlobalQuestManager globalQuestManager,
            IGameTableManager gameTableManager)
        {
            this.log               = log;
            this.cinematicFactory   = cinematicFactory;
            this.entityFactory      = entityFactory;
            this.globalQuestManager = globalQuestManager;
            this.gameTableManager   = gameTableManager;
        }

        #endregion

        public void OnLoad(IBaseMap owner)
        {
            this.owner = owner;
            EnsureTutorialTriggers();
            EnsureExileCombatSimulationEntities();
            EnsureDominionCombatSimulationEntities();
            EnsureShipDeckNpcs();
            EnsureShipInteriorEntities();
            EnsureCryopodNpcs();
            EnsureExileDepartureEntities();
            EnsureDominionDepartureEntities();
        }

        public void Update(double lastTick)
        {
            if (owner == null)
                return;

            foreach (IPlayer player in owner.Search(Vector3.Zero, null, matchAllPlayers))
            {
                if (!initialisedPlayerGuids.Add(player.Guid))
                    continue;

                log.LogDebug("Tutorial map scan recovered existing character {CharacterId} (guid {PlayerGuid}) on map {MapId}: position ({X}, {Y}, {Z}), starter states [{StarterQuestStates}], follow-up states [{FollowUpQuestStates}].",
                    player.CharacterId,
                    player.Guid,
                    owner.Entry.Id,
                    player.Position.X,
                    player.Position.Y,
                    player.Position.Z,
                    FormatQuestStates(player, StarterQuestIds),
                    FormatQuestStates(player, FollowUpQuestIds));

                EnsureTutorialQuests(player);
            }
        }

        public void OnAddToMap(IGridEntity entity)
        {
            if (entity is not IPlayer player)
                return;

            initialisedPlayerGuids.Add(player.Guid);
            bool hadTutorialState = HasAnyQuestState(player, StarterQuestIds)
                || HasAnyQuestState(player, FollowUpQuestIds);

            log.LogDebug("Tutorial map player add for character {CharacterId} (guid {PlayerGuid}) on map {MapId}: faction {Faction}, position ({X}, {Y}, {Z}), starter states [{StarterQuestStates}], follow-up states [{FollowUpQuestStates}].",
                player.CharacterId,
                player.Guid,
                owner?.Entry?.Id,
                player.Faction1,
                player.Position.X,
                player.Position.Y,
                player.Position.Z,
                FormatQuestStates(player, StarterQuestIds),
                FormatQuestStates(player, FollowUpQuestIds));

            EnsureTutorialQuests(player);

            if (!hadTutorialState)
            {
                player.CinematicManager.QueueCinematic(cinematicFactory.CreateCinematic<INoviceTutorialOnEnter>());
                log.LogDebug("Queued Rider's Reef intro cinematic for fresh tutorial entry on character {CharacterId} (guid {PlayerGuid}).",
                    player.CharacterId,
                    player.Guid);
            }
        }

        public void OnRemoveFromMap(IGridEntity entity)
        {
            initialisedPlayerGuids.Remove(entity.Guid);
        }

        private void EnsureTutorialTriggers()
        {
            if (tutorialTriggersSpawned || owner == null)
                return;

            foreach (uint worldLocationId in TutorialWorldLocationIds)
            {
                IWorldLocationVolumeGridTriggerEntity trigger = entityFactory.CreateEntity<IWorldLocationVolumeGridTriggerEntity>();
                trigger.Initialise(worldLocationId, 0u);

                owner.EnqueueAdd(trigger, new TutorialMapPosition
                {
                    Info = new TutorialMapInfo
                    {
                        Entry = owner.Entry
                    },
                    Position = new Vector3(trigger.Entry.Position0, trigger.Entry.Position1, trigger.Entry.Position2)
                });
            }

            tutorialTriggersSpawned = true;
            log.LogDebug("Spawned Rider's Reef tutorial triggers on map {MapId}: world locations {WorldLocationIds}.",
                owner.Entry.Id, string.Join(", ", TutorialWorldLocationIds));
        }

        private void EnsureExileCombatSimulationEntities()
        {
            if (exileCombatLaneSpawned || owner == null)
                return;

            WorldLocation2Entry start = gameTableManager.WorldLocation2.GetEntry(ExileCombatSimulationWorldLocationId);
            WorldLocation2Entry easyMine = gameTableManager.WorldLocation2.GetEntry(ExileCombatMineEasyWorldLocationId);
            WorldLocation2Entry mediumMine = gameTableManager.WorldLocation2.GetEntry(ExileCombatMineMediumWorldLocationId);
            WorldLocation2Entry hardMine = gameTableManager.WorldLocation2.GetEntry(ExileCombatMineHardWorldLocationId);
            WorldLocation2Entry turret00 = gameTableManager.WorldLocation2.GetEntry(ExileCombatTurretWorldLocationId00);
            WorldLocation2Entry turret01 = gameTableManager.WorldLocation2.GetEntry(ExileCombatTurretWorldLocationId01);
            WorldLocation2Entry final = gameTableManager.WorldLocation2.GetEntry(ExileCombatFinalWorldLocationId);

            if (start == null || easyMine == null || mediumMine == null || hardMine == null
                || turret00 == null || turret01 == null || final == null)
            {
                log.LogWarning("Unable to spawn Rider's Reef Exile combat lane on map {MapId}: one or more world locations are missing.",
                    owner.Entry.Id);
                return;
            }

            if (owner.Search(ToVector3(start), 30f, new CreatureSearchCheck(ExileCombatDagunCreatureId)).Any())
            {
                log.LogDebug("Skipping Rider's Reef Exile combat lane fallback on map {MapId}: imported combat entities are already active.",
                    owner.Entry.Id);
                exileCombatLaneSpawned = true;
                return;
            }

            SpawnTutorialEntity<INonPlayerEntity>(ExileCombatDagunCreatureId, ToVector3(start) + new Vector3(-8f, 0f, -6f));
            SpawnTutorialEntity<INonPlayerEntity>(ExileCombatDagunCreatureId, ToVector3(start) + new Vector3(12f, 0f, 4f));
            SpawnTutorialEntity<INonPlayerEntity>(ExileCombatDagunCreatureId, ToVector3(easyMine) + new Vector3(-10f, 0f, -10f));
            SpawnTutorialEntity<INonPlayerEntity>(ExileCombatDagunCreatureId, ToVector3(mediumMine) + new Vector3(12f, 0f, -14f));
            SpawnTutorialEntity<INonPlayerEntity>(ExileCombatDagunCreatureId, ToVector3(hardMine) + new Vector3(-8f, 0f, -16f));

            SpawnTutorialEntity<ISimpleCollidableEntity>(73463u, ToVector3(easyMine));
            SpawnTutorialEntity<ISimpleCollidableEntity>(73667u, ToVector3(mediumMine));
            SpawnTutorialEntity<ISimpleCollidableEntity>(73668u, ToVector3(hardMine));

            SpawnTutorialEntity<INonPlayerEntity>(ExileCombatTurretCreatureId, ToVector3(turret00));
            SpawnTutorialEntity<INonPlayerEntity>(ExileCombatTurretCreatureId, ToVector3(turret01));

            Vector3 finalPosition = ToVector3(final);
            SpawnTutorialEntity<INonPlayerEntity>(73492u, finalPosition + new Vector3(-9f, 0f, -6f));
            SpawnTutorialEntity<INonPlayerEntity>(73567u, finalPosition + new Vector3(8f, 0f, 5f));
            SpawnTutorialEntity<INonPlayerEntity>(73492u, finalPosition + new Vector3(14f, 0f, -9f));

            exileCombatLaneSpawned = true;
            log.LogDebug("Spawned Rider's Reef Exile combat lane on map {MapId}: dagun=5, mines=3, turrets=2, legionnaires=3.",
                owner.Entry.Id);
        }

        private void EnsureDominionCombatSimulationEntities()
        {
            if (dominionCombatLaneSpawned || owner == null)
                return;

            WorldLocation2Entry start = gameTableManager.WorldLocation2.GetEntry(DominionCombatSimulationWorldLocationId);
            WorldLocation2Entry easyMine = gameTableManager.WorldLocation2.GetEntry(DominionCombatMineEasyWorldLocationId);
            WorldLocation2Entry mediumMine = gameTableManager.WorldLocation2.GetEntry(DominionCombatMineMediumWorldLocationId);
            WorldLocation2Entry hardMine = gameTableManager.WorldLocation2.GetEntry(DominionCombatMineHardWorldLocationId);
            WorldLocation2Entry turret00 = gameTableManager.WorldLocation2.GetEntry(DominionCombatTurretWorldLocationId00);
            WorldLocation2Entry turret01 = gameTableManager.WorldLocation2.GetEntry(DominionCombatTurretWorldLocationId01);
            WorldLocation2Entry elite = gameTableManager.WorldLocation2.GetEntry(DominionCombatEliteWorldLocationId);

            if (start == null || easyMine == null || mediumMine == null || hardMine == null || turret00 == null || turret01 == null || elite == null)
            {
                log.LogWarning("Unable to spawn Rider's Reef Dominion combat lane on map {MapId}: one or more world locations are missing.",
                    owner.Entry.Id);
                return;
            }

            if (owner.Search(ToVector3(start), 30f, new CreatureSearchCheck(DominionCombatDagunCreatureId)).Any())
            {
                log.LogDebug("Skipping Rider's Reef Dominion combat lane fallback on map {MapId}: imported combat entities are already active.",
                    owner.Entry.Id);
                dominionCombatLaneSpawned = true;
                return;
            }

            SpawnTutorialEntity<INonPlayerEntity>(DominionCombatDagunCreatureId, ToVector3(start) + new Vector3(-8f, 0f, -6f));
            SpawnTutorialEntity<INonPlayerEntity>(DominionCombatDagunCreatureId, ToVector3(start) + new Vector3(12f, 0f, 4f));
            SpawnTutorialEntity<INonPlayerEntity>(DominionCombatDagunCreatureId, ToVector3(easyMine) + new Vector3(-10f, 0f, -10f));
            SpawnTutorialEntity<INonPlayerEntity>(DominionCombatDagunCreatureId, ToVector3(mediumMine) + new Vector3(12f, 0f, -14f));
            SpawnTutorialEntity<INonPlayerEntity>(DominionCombatDagunCreatureId, ToVector3(hardMine) + new Vector3(-8f, 0f, -16f));

            SpawnTutorialEntity<ISimpleCollidableEntity>(73463u, ToVector3(easyMine));
            SpawnTutorialEntity<ISimpleCollidableEntity>(73667u, ToVector3(mediumMine));
            SpawnTutorialEntity<ISimpleCollidableEntity>(73668u, ToVector3(hardMine));

            SpawnTutorialEntity<INonPlayerEntity>(DominionCombatTurretCreatureId, ToVector3(turret00));
            SpawnTutorialEntity<INonPlayerEntity>(DominionCombatTurretCreatureId, ToVector3(turret01));

            Vector3 elitePosition = ToVector3(elite);
            SpawnTutorialEntity<INonPlayerEntity>(73473u, elitePosition + new Vector3(-9f, 0f, -6f));
            SpawnTutorialEntity<INonPlayerEntity>(73566u, elitePosition + new Vector3(8f, 0f, 5f));
            SpawnTutorialEntity<INonPlayerEntity>(73473u, elitePosition + new Vector3(14f, 0f, -9f));

            dominionCombatLaneSpawned = true;
            log.LogDebug("Spawned Rider's Reef Dominion combat lane on map {MapId}: dagun=5, mines=3, turrets=2, finalHostiles=3.",
                owner.Entry.Id);
        }

        private void SpawnTutorialEntity<T>(uint creatureId, Vector3 position, Vector3 rotation = default) where T : class, IWorldEntity
        {
            T entity = entityFactory.CreateEntity<T>();
            if (entity == null)
            {
                log.LogWarning("Unable to spawn Rider's Reef tutorial entity {CreatureId} on map {MapId}: entity factory returned null for {EntityType}.",
                    creatureId,
                    owner?.Entry?.Id,
                    typeof(T).Name);
                return;
            }

            entity.Initialise(creatureId);
            entity.Rotation = rotation;

            owner.EnqueueAdd(entity, new TutorialMapPosition
            {
                Info = new TutorialMapInfo
                {
                    Entry = owner.Entry
                },
                Position = position
            });
        }

        private static Vector3 ToVector3(WorldLocation2Entry worldLocation)
        {
            return new Vector3(worldLocation.Position0, worldLocation.Position1, worldLocation.Position2);
        }

        private void EnsureShipDeckNpcs()
        {
            if (shipDeckNpcsSpawned || owner == null)
                return;

            WorldLocation2Entry wlA = gameTableManager.WorldLocation2.GetEntry(ShipDeckNpcAWL);
            if (wlA != null && owner.Search(ToVector3(wlA), 15f, new CreatureSearchCheck(ShipDeckNpcA)).Any())
            {
                log.LogDebug("Skipping Rider's Reef ship-deck NPC fallback on map {MapId}: imported NPCs already active.", owner.Entry.Id);
                shipDeckNpcsSpawned = true;
                return;
            }
            WorldLocation2Entry wlB = gameTableManager.WorldLocation2.GetEntry(ShipDeckNpcBWL);
            WorldLocation2Entry wlC = gameTableManager.WorldLocation2.GetEntry(ShipDeckNpcCWL);

            if (wlA == null || wlB == null || wlC == null)
            {
                log.LogWarning("Unable to spawn Rider's Reef ship-deck NPCs on map {MapId}: one or more world locations are missing.",
                    owner.Entry.Id);
                return;
            }

            SpawnTutorialEntity<INonPlayerEntity>(ShipDeckNpcA, ToVector3(wlA));
            SpawnTutorialEntity<INonPlayerEntity>(ShipDeckNpcB, ToVector3(wlB));
            SpawnTutorialEntity<INonPlayerEntity>(ShipDeckNpcC, ToVector3(wlC));

            shipDeckNpcsSpawned = true;
            log.LogDebug("Spawned Rider's Reef ship-deck NPCs on map {MapId}: NPCs {NpcA}, {NpcB}, {NpcC}.",
                owner.Entry.Id,
                ShipDeckNpcA,
                ShipDeckNpcB,
                ShipDeckNpcC);
        }

        private void EnsureShipInteriorEntities()
        {
            if (shipInteriorEntitiesSpawned || owner == null)
                return;

            WorldLocation2Entry creatureWl = gameTableManager.WorldLocation2.GetEntry(ShipInteriorCreatureWL);
            if (creatureWl != null && owner.Search(ToVector3(creatureWl), 30f, new CreatureSearchCheck(ShipInteriorCreatureId)).Any())
            {
                log.LogDebug("Skipping Rider's Reef ship interior fallback on map {MapId}: imported entities already active.", owner.Entry.Id);
                shipInteriorEntitiesSpawned = true;
                return;
            }
            if (creatureWl == null)
            {
                log.LogWarning("Unable to spawn Rider's Reef ship interior entities on map {MapId}: world location {WorldLocationId} is missing.",
                    owner.Entry.Id,
                    ShipInteriorCreatureWL);
                return;
            }

            SpawnTutorialEntity<INonPlayerEntity>(ShipInteriorCreatureId, ToVector3(creatureWl));
            SpawnTutorialEntity<INonPlayerEntity>(ShipInteriorChecklistCreatureId, ToVector3(creatureWl) + new Vector3(3f, 0f, 0f));

            shipInteriorEntitiesSpawned = true;
            log.LogDebug("Spawned Rider's Reef ship interior entities on map {MapId}: creature={CreatureId}, checklist={ChecklistId}.",
                owner.Entry.Id,
                ShipInteriorCreatureId,
                ShipInteriorChecklistCreatureId);
        }

        private void EnsureCryopodNpcs()
        {
            if (cryopodNpcsSpawned || owner == null)
                return;

            WorldLocation2Entry wlA = gameTableManager.WorldLocation2.GetEntry(CryopodNpcAWL);
            if (wlA != null && owner.Search(ToVector3(wlA), 15f, new CreatureSearchCheck(CryopodNpcA)).Any())
            {
                log.LogDebug("Skipping Rider's Reef cryopod NPC fallback on map {MapId}: imported NPCs already active.", owner.Entry.Id);
                cryopodNpcsSpawned = true;
                return;
            }
            WorldLocation2Entry wlB = gameTableManager.WorldLocation2.GetEntry(CryopodNpcBWL);

            if (wlA == null || wlB == null)
            {
                log.LogWarning("Unable to spawn Rider's Reef cryopod NPCs on map {MapId}: one or more world locations are missing.",
                    owner.Entry.Id);
                return;
            }

            SpawnTutorialEntity<INonPlayerEntity>(CryopodNpcA, ToVector3(wlA));
            SpawnTutorialEntity<INonPlayerEntity>(CryopodNpcB, ToVector3(wlB));
            SpawnTutorialEntity<INonPlayerEntity>(CryopodTargetGroupNpcA, ToVector3(wlA) + new Vector3(2f, 0f, 2f));
            SpawnTutorialEntity<INonPlayerEntity>(CryopodTargetGroupNpcB, ToVector3(wlB) + new Vector3(2f, 0f, 2f));

            cryopodNpcsSpawned = true;
            log.LogDebug("Spawned Rider's Reef cryopod NPCs on map {MapId}: talkTo={NpcA},{NpcB}, targetGroup={TgNpcA},{TgNpcB}.",
                owner.Entry.Id,
                CryopodNpcA,
                CryopodNpcB,
                CryopodTargetGroupNpcA,
                CryopodTargetGroupNpcB);
        }

        private void EnsureExileDepartureEntities()
        {
            if (exileDepartureEntitiesSpawned || owner == null)
                return;

            WorldLocation2Entry wlNpc = gameTableManager.WorldLocation2.GetEntry(ExileEscapePodNpcWL);
            if (wlNpc != null && owner.Search(ToVector3(wlNpc), 15f, new CreatureSearchCheck(ExileEscapePodNpcId)).Any())
            {
                log.LogDebug("Skipping Rider's Reef Exile departure fallback on map {MapId}: imported entities already active.", owner.Entry.Id);
                exileDepartureEntitiesSpawned = true;
                return;
            }
            WorldLocation2Entry wlA = gameTableManager.WorldLocation2.GetEntry(ExileEscapePodConsoleAWL);
            WorldLocation2Entry wlB = gameTableManager.WorldLocation2.GetEntry(ExileEscapePodConsoleBWL);
            WorldLocation2Entry wlC = gameTableManager.WorldLocation2.GetEntry(ExileEscapePodConsoleCWL);

            if (wlNpc == null || wlA == null || wlB == null || wlC == null)
            {
                log.LogWarning("Unable to spawn Rider's Reef Exile departure entities on map {MapId}: one or more world locations are missing.",
                    owner.Entry.Id);
                return;
            }

            Vector3 npcPosition = ToVector3(wlNpc);
            Vector3 checklistCenter = ToVector3(wlC);
            SpawnTutorialEntity<INonPlayerEntity>(ExileEscapePodNpcId, npcPosition);
            SpawnTutorialEntity<ISimpleCollidableEntity>(ExileEverstarGroveDepartureTerminalCreatureId, ToVector3(wlA));
            SpawnTutorialEntity<ISimpleCollidableEntity>(ExileNorthernWildsDepartureTerminalCreatureId, ToVector3(wlB));

            for (int i = 0; i < ExileEscapePodChecklistCreatureIds.Length; i++)
            {
                float angle = i * MathF.PI * 2f / ExileEscapePodChecklistCreatureIds.Length;
                Vector3 offset = new(MathF.Cos(angle) * 5f, 0f, MathF.Sin(angle) * 5f);
                SpawnTutorialEntity<ISimpleCollidableEntity>(ExileEscapePodChecklistCreatureIds[i], checklistCenter + offset);
            }

            exileDepartureEntitiesSpawned = true;
            log.LogDebug("Spawned Rider's Reef Exile departure entities on map {MapId}: NPC {NpcId}, consoles=2, checklist={ChecklistCount}.",
                owner.Entry.Id,
                ExileEscapePodNpcId,
                ExileEscapePodChecklistCreatureIds.Length);
        }

        private void EnsureDominionDepartureEntities()
        {
            if (dominionDepartureEntitiesSpawned || owner == null)
                return;

            WorldLocation2Entry wlNpc = gameTableManager.WorldLocation2.GetEntry(DominionEscapePodNpcWL);
            if (wlNpc != null && owner.Search(ToVector3(wlNpc), 15f, new CreatureSearchCheck(DominionEscapePodNpcId)).Any())
            {
                log.LogDebug("Skipping Rider's Reef Dominion departure fallback on map {MapId}: imported entities already active.", owner.Entry.Id);
                dominionDepartureEntitiesSpawned = true;
                return;
            }
            WorldLocation2Entry wlA = gameTableManager.WorldLocation2.GetEntry(DominionEscapePodConsoleAWL);
            WorldLocation2Entry wlB = gameTableManager.WorldLocation2.GetEntry(DominionEscapePodConsoleBWL);
            WorldLocation2Entry wlC = gameTableManager.WorldLocation2.GetEntry(DominionEscapePodConsoleCWL);

            if (wlNpc == null || wlA == null || wlB == null || wlC == null)
            {
                log.LogWarning("Unable to spawn Rider's Reef Dominion departure entities on map {MapId}: one or more world locations are missing.",
                    owner.Entry.Id);
                return;
            }

            Vector3 npcPosition = ToVector3(wlNpc);
            Vector3 checklistCenter = ToVector3(wlC);
            SpawnTutorialEntity<INonPlayerEntity>(DominionEscapePodNpcId, npcPosition);
            SpawnTutorialEntity<ISimpleCollidableEntity>(DominionCrimsonIsleDepartureTerminalCreatureId, ToVector3(wlA));
            SpawnTutorialEntity<ISimpleCollidableEntity>(DominionLevianBayDepartureTerminalCreatureId, ToVector3(wlB));

            for (int i = 0; i < DominionEscapePodChecklistCreatureIds.Length; i++)
            {
                float angle = i * MathF.PI * 2f / DominionEscapePodChecklistCreatureIds.Length;
                Vector3 offset = new(MathF.Cos(angle) * 5f, 0f, MathF.Sin(angle) * 5f);
                SpawnTutorialEntity<ISimpleCollidableEntity>(DominionEscapePodChecklistCreatureIds[i], checklistCenter + offset);
            }

            dominionDepartureEntitiesSpawned = true;
            log.LogDebug("Spawned Rider's Reef Dominion departure entities on map {MapId}: NPC {NpcId}, consoles=2, checklist={ChecklistCount}.",
                owner.Entry.Id,
                DominionEscapePodNpcId,
                DominionEscapePodChecklistCreatureIds.Length);
        }

        private void EnsureTutorialQuests(IPlayer player)
        {
            if (HasAnyQuestState(player, FollowUpQuestIds))
            {
                log.LogDebug("Skipping Rider's Reef tutorial ensure for character {CharacterId} (guid {PlayerGuid}): follow-up quest state already present [{FollowUpQuestStates}].",
                    player.CharacterId,
                    player.Guid,
                    FormatQuestStates(player, FollowUpQuestIds));
                return;
            }

            (ushort movementQuestId, ushort hoverboardQuestId) = player.Faction1 switch
            {
                Faction.Exile    => (ExileMovementQuestId, ExileHoverboardQuestId),
                Faction.Dominion => (DominionMovementQuestId, DominionHoverboardQuestId),
                _                => ((ushort)0, (ushort)0)
            };

            if (movementQuestId == 0)
                return;

            GrantQuestIfMissing(player, movementQuestId);
            GrantQuestIfMissing(player, hoverboardQuestId);
            SyncTutorialAreaObjectives(player);
        }

        private void GrantQuestIfMissing(IPlayer player, ushort questId)
        {
            if (player.QuestManager.GetQuestState(questId) != null)
                return;

            IQuestInfo questInfo = globalQuestManager.GetQuestInfo(questId);
            if (questInfo != null)
            {
                player.QuestManager.QuestAdd(questInfo);
                log.LogDebug("Granted Rider's Reef tutorial quest {QuestId} to character {CharacterId} (guid {PlayerGuid}) on map {MapId}.",
                    questId, player.CharacterId, player.Guid, owner?.Entry?.Id);
            }
        }

        private void SyncTutorialAreaObjectives(IPlayer player)
        {
            int furthestReachedIndex = GetFurthestReachedTutorialWorldLocationIndex(player);
            if (furthestReachedIndex < 0)
            {
                log.LogDebug("Tutorial area sync found no Rider's Reef world-location overlap for character {CharacterId} (guid {PlayerGuid}): position ({X}, {Y}, {Z}), hit radius {HitRadius}, starter states [{StarterQuestStates}].",
                    player.CharacterId,
                    player.Guid,
                    player.Position.X,
                    player.Position.Y,
                    player.Position.Z,
                    player.HitRadius,
                    FormatQuestStates(player, StarterQuestIds));
                return;
            }

            foreach (IQuest quest in player.QuestManager.GetActiveQuests().Where(q => StarterQuestIds.Contains(q.Id)))
            {
                for (int index = 0; index <= furthestReachedIndex; index++)
                {
                    uint worldLocationId = TutorialWorldLocationIds[index];
                    foreach (IQuestObjective objective in GetObjectivesToUpdate(quest, worldLocationId))
                    {
                        log.LogDebug("Tutorial area sync advanced character {CharacterId} (guid {PlayerGuid}): quest {QuestId} objective {ObjectiveId} at world location {WorldLocationId}, furthest index {FurthestIndex}, position ({X}, {Y}, {Z}).",
                            player.CharacterId, player.Guid, quest.Id, objective.ObjectiveInfo.Id, worldLocationId, furthestReachedIndex,
                            player.Position.X, player.Position.Y, player.Position.Z);
                        quest.ObjectiveUpdate(objective.ObjectiveInfo.Id, 1u);
                    }
                }
            }
        }

        private int GetFurthestReachedTutorialWorldLocationIndex(IPlayer player)
        {
            int furthestIndex = -1;
            float horizontalPadding = player.HitRadius * 0.5f;

            for (int index = 0; index < TutorialWorldLocationIds.Length; index++)
            {
                WorldLocation2Entry worldLocation = gameTableManager.WorldLocation2.GetEntry(TutorialWorldLocationIds[index]);
                if (worldLocation != null && IsInsideWorldLocation(player.Position, worldLocation, horizontalPadding))
                    furthestIndex = index;
            }

            return furthestIndex;
        }

        private static bool MatchesWorldLocation(IQuestObjective objective, uint worldLocationId)
        {
            if (objective.ObjectiveInfo.Type != QuestObjectiveType.EnterArea)
                return false;

            QuestObjectiveEntry objectiveEntry = objective.ObjectiveInfo.Entry;
            return objectiveEntry.WorldLocationsIdIndicator00 == worldLocationId
                || objectiveEntry.WorldLocationsIdIndicator01 == worldLocationId
                || objectiveEntry.WorldLocationsIdIndicator02 == worldLocationId
                || objectiveEntry.WorldLocationsIdIndicator03 == worldLocationId;
        }

        private static IEnumerable<IQuestObjective> GetObjectivesToUpdate(IQuest quest, uint worldLocationId)
        {
            List<IQuestObjective> matchingObjectives = quest
                .Where(o => MatchesWorldLocation(o, worldLocationId))
                .Where(o => !o.IsComplete())
                .ToList();

            if (matchingObjectives.Count == 0)
                return [];

            IEnumerable<IQuestObjective> objectivesToUpdate = matchingObjectives.Any(o => !o.ObjectiveInfo.IsOptional())
                ? matchingObjectives.Where(o => !o.ObjectiveInfo.IsOptional())
                : matchingObjectives;

            return objectivesToUpdate.OrderByDescending(o => o.Index).ToList();
        }

        private static bool IsInsideWorldLocation(Vector3 position, WorldLocation2Entry worldLocation, float horizontalPadding = 0f)
        {
            float horizontalDistanceSquared = Vector2.DistanceSquared(
                new Vector2(position.X, position.Z),
                new Vector2(worldLocation.Position0, worldLocation.Position2));

            float horizontalRange = worldLocation.Radius + horizontalPadding;
            if (horizontalDistanceSquared > horizontalRange * horizontalRange)
                return false;

            return worldLocation.MaxVerticalDistance <= 0f
                || MathF.Abs(position.Y - worldLocation.Position1) <= worldLocation.MaxVerticalDistance;
        }

        private static bool HasAnyQuestState(IPlayer player, IEnumerable<ushort> questIds)
        {
            return questIds.Any(questId => player.QuestManager.GetQuestState(questId) != null);
        }

        private static string FormatQuestStates(IPlayer player, IEnumerable<ushort> questIds)
        {
            return string.Join(", ", questIds.Select(questId => $"{questId}:{player.QuestManager.GetQuestState(questId)?.ToString() ?? "None"}"));
        }
    }
}
