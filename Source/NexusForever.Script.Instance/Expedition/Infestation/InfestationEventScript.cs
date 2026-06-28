using System.Numerics;
using NexusForever.Database.World.Model;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Entity.Trigger;
using NexusForever.Game.Abstract.Map.Instance;
using NexusForever.Game.Abstract.PublicEvent;
using NexusForever.Game.Static.Entity;
using NexusForever.Game.Static.PublicEvent;
using NexusForever.Script.Template;
using NexusForever.Script.Template.Filter;

namespace NexusForever.Script.Instance.Expedition.Infestation
{
    [ScriptFilterOwnerId(95)]
    public class InfestationEventScript : IPublicEventScript, IOwnedScript<IPublicEvent>
    {
        private IPublicEvent publicEvent;
        private IMapInstance mapInstance;
        private bool captainTolbenSpawned;
        private bool shipVentPlacementsSpawned;
        private bool cargoContainerPlacementsSpawned;
        private bool hullBreachPlacementsSpawned;
        private bool medicalSuppliesPlacementsSpawned;
        private bool secondMedicalSuppliesPlacementsSpawned;
        private bool lashingFiendPlacementsSpawned;
        private bool contaminatedShiphandPlacementsSpawned;

        private const uint CargoShipTurnstileTriggerId = 1005u;
        private const uint CargoShipTurnstileObjectiveObjectId = 1005u;
        private const float CargoShipTurnstileRange = 50f;
        private static readonly Vector3 CargoShipTurnstilePosition = new(-0.2391071f, -499.99f, 87.62592f);

        private const uint CaptainTolbenEntityId = 1100300016u;
        private const uint CaptainTolbenCreatureId = 71764u;
        private const ushort InfestationWorldId = 1232;
        private const uint InfestationPublicEventId = 95u;
        private const ushort CaptainTolbenAreaId = 1158;
        private const uint CaptainTolbenDisplayInfo = 28578u;
        private const ushort CaptainTolbenFactionId = 219;
        private static readonly Vector3 CaptainTolbenPosition = new(4.41241f, -500f, 14.0114f);

        private static readonly InfestationSimpleSpawnModel[] ShipVentSpawns =
        [
            // Jabbithole PE creature 1361 coordinate rows 5738-5740 map the
            // objective's three build 16042 TargetGroup 2324 / Creature2 22493 vents.
            new(1100123001u, 22493u, 1158, (uint)PublicEventPhase.CloseTheShipVents, new Vector3(11f, -500f, 67f), 27574u, 219),
            new(1100123002u, 22493u, 1158, (uint)PublicEventPhase.CloseTheShipVents, new Vector3(-5f, -500f, 30f), 27574u, 219),
            new(1100123003u, 22493u, 1158, (uint)PublicEventPhase.CloseTheShipVents, new Vector3(-10f, -500f, 78f), 27574u, 219)
        ];

        private static readonly InfestationSimpleSpawnModel[] CargoContainerSpawns =
        [
            // Jabbithole PE creature 28158 coordinate rows 6130161-6130164 and
            // 6131402-6131403 map the six TargetGroup 12593 / Creature2 69877 containers.
            new(1100123004u, 69877u, 2262, (uint)PublicEventPhase.FindTheMedicalBay, new Vector3(-120f, -498f, 180f), 27097u, 219),
            new(1100123005u, 69877u, 2262, (uint)PublicEventPhase.FindTheMedicalBay, new Vector3(-11f, -497f, 113f), 27097u, 219),
            new(1100123006u, 69877u, 2262, (uint)PublicEventPhase.FindTheMedicalBay, new Vector3(-89f, -494f, 134f), 27097u, 219),
            new(1100123007u, 69877u, 2262, (uint)PublicEventPhase.FindTheMedicalBay, new Vector3(13f, -497f, 138f), 27097u, 219),
            new(1100123008u, 69877u, 2262, (uint)PublicEventPhase.FindTheMedicalBay, new Vector3(-88f, -497f, 101f), 27097u, 219),
            new(1100123009u, 69877u, 2262, (uint)PublicEventPhase.FindTheMedicalBay, new Vector3(-22f, -515f, 131f), 27097u, 219)
        ];

        private static readonly InfestationSimpleSpawnModel[] HullBreachSpawns =
        [
            // Jabbithole PE creature 1310 has nine build-era coordinates. Promote
            // the contiguous six-row set matching objective 1007's build 16042
            // count; the three extra coordinates remain a placement-smoke blocker.
            new(1100123010u, 36844u, 2262, (uint)PublicEventPhase.SealHullBreaches, new Vector3(-141f, -510f, 119f), 29666u, 219),
            new(1100123011u, 36844u, 2262, (uint)PublicEventPhase.SealHullBreaches, new Vector3(-166f, -493f, 139f), 29666u, 219),
            new(1100123012u, 36844u, 2262, (uint)PublicEventPhase.SealHullBreaches, new Vector3(-132f, -504f, 90f), 29666u, 219),
            new(1100123013u, 36844u, 2262, (uint)PublicEventPhase.SealHullBreaches, new Vector3(-132f, -504f, 186f), 29666u, 219),
            new(1100123014u, 36844u, 2262, (uint)PublicEventPhase.SealHullBreaches, new Vector3(-83f, -508f, 160f), 29666u, 219),
            new(1100123015u, 36844u, 2262, (uint)PublicEventPhase.SealHullBreaches, new Vector3(-123f, -509f, 105f), 29666u, 219)
        ];

        private static readonly InfestationSimpleSpawnModel[] MedicalSuppliesSpawns =
        [
            // Jabbithole PE creature 1349 coordinate row 5707 maps the single
            // TargetGroup 2323 / Creature2 22489 medical-supplies interactable.
            new(1100123016u, 22489u, 1172, (uint)PublicEventPhase.FindMedicalSupplies, new Vector3(-78f, -525f, 51f), 27097u, 219)
        ];

        private static readonly InfestationSimpleSpawnModel[] SecondMedicalSuppliesSpawns =
        [
            // The objective is reset before the second supplies phase, so keep a
            // fresh phase-scoped entity with a distinct runtime id at the same row.
            new(1100123017u, 22489u, 1172, (uint)PublicEventPhase.FindMedicalSupplies2, new Vector3(-78f, -525f, 51f), 27097u, 219)
        ];

        private static readonly InfestationNonPlayerSpawnModel[] LashingFiendSpawns =
        [
            // Build 16042 objective 4698 maps to TargetGroup 12905 / Creature2
            // 69871. Jabbithole PE creature 28012 coordinate row 8251634 places
            // one veteran Lashing Fiend in the medbay for the count-one attack.
            new(1100123022u, 69871u, 1172, (uint)PublicEventPhase.DefeatTheAttackOnMedbay, new Vector3(-58f, -525f, 28f), 21629u, 0, 218)
        ];

        private static readonly InfestationNonPlayerSpawnModel[] ContaminatedShiphandSpawns =
        [
            // Jabbithole PE creature rows 1348 and 28034 both bridge to Creature2
            // 23837. Promote only the four overlapping current coordinates; the
            // stale 5706 row and the 0,0,0 row remain blocked pending smoke.
            new(1100123018u, 23837u, 2262, (uint)PublicEventPhase.HealContaminatedShiphands, new Vector3(-71f, -497f, 114f), 24148u, 8186, 843),
            new(1100123019u, 23837u, 2262, (uint)PublicEventPhase.HealContaminatedShiphands, new Vector3(-100f, -512f, 147f), 24148u, 8186, 843),
            new(1100123020u, 23837u, 2262, (uint)PublicEventPhase.HealContaminatedShiphands, new Vector3(-53f, -514f, 146f), 24148u, 8186, 843),
            new(1100123021u, 23837u, 2262, (uint)PublicEventPhase.HealContaminatedShiphands, new Vector3(3f, -497f, 111f), 24148u, 8186, 843)
        ];

        /// <summary>
        /// Invoked when <see cref="IScript"/> is loaded.
        /// </summary>
        public void OnLoad(IPublicEvent owner)
        {
            publicEvent = owner;
            mapInstance = publicEvent.Map as IMapInstance
                ?? throw new InvalidOperationException("Infestation requires a map instance.");

            captainTolbenSpawned = false;
            shipVentPlacementsSpawned = false;
            cargoContainerPlacementsSpawned = false;
            hullBreachPlacementsSpawned = false;
            medicalSuppliesPlacementsSpawned = false;
            secondMedicalSuppliesPlacementsSpawned = false;
            lashingFiendPlacementsSpawned = false;
            contaminatedShiphandPlacementsSpawned = false;
            // Build 16042 objective 4703 is a zero-count ScriptWithoutMax row
            // with an 18-minute failure timer for Gold medal eligibility.
            publicEvent.ActivateObjective(PublicEventObjective.GoldMedalTimer);
            publicEvent.SetPhase(PublicEventPhase.ProceedOntoTheCargoShip);
        }

        /// <summary>
        /// Invoked when the public event phase changes.
        /// </summary>
        public void OnPublicEventPhase(uint phase)
        {
            switch ((PublicEventPhase)phase)
            {
                case PublicEventPhase.ProceedOntoTheCargoShip:
                    publicEvent.ActivateObjective(PublicEventObjective.ProceedOntoTheCargoShip, mapInstance.PlayerCount);
                    SpawnReviewedCaptainTolben();
                    SpawnWipGuessedTurnstileTrigger(
                        CargoShipTurnstileTriggerId,
                        CargoShipTurnstileRange,
                        CargoShipTurnstileObjectiveObjectId,
                        CargoShipTurnstilePosition);
                    break;
                case PublicEventPhase.CloseTheShipVents:
                    publicEvent.ActivateObjective(PublicEventObjective.CloseTheShipVents);
                    SpawnReviewedSimplePlacements(ShipVentSpawns, ref shipVentPlacementsSpawned);
                    break;
                case PublicEventPhase.FindTheMedicalBay:
                    publicEvent.ActivateObjective(PublicEventObjective.FindTheMedicalBay, mapInstance.PlayerCount);
                    publicEvent.ActivateObjective(PublicEventObjective.TagValuableCargo);
                    SpawnReviewedSimplePlacements(CargoContainerSpawns, ref cargoContainerPlacementsSpawned);
                    break;
                case PublicEventPhase.SealHullBreaches:
                    publicEvent.ResetObjective(PublicEventObjective.FindTheMedicalBay);
                    publicEvent.ActivateObjective(PublicEventObjective.SealHullBreaches);
                    SpawnReviewedSimplePlacements(HullBreachSpawns, ref hullBreachPlacementsSpawned);
                    break;
                case PublicEventPhase.FindTheMedicalBay2:
                    publicEvent.ActivateObjective(PublicEventObjective.FindTheMedicalBay);
                    break;
                case PublicEventPhase.FindMedicalSupplies:
                    publicEvent.ActivateObjective(PublicEventObjective.FindMedicalSupplies);
                    SpawnReviewedSimplePlacements(MedicalSuppliesSpawns, ref medicalSuppliesPlacementsSpawned);
                    break;
                case PublicEventPhase.DefeatTheAttackOnMedbay:
                    publicEvent.ResetObjective(PublicEventObjective.FindMedicalSupplies);
                    publicEvent.ActivateObjective(PublicEventObjective.DefeatTheAttackOnMedbay);
                    SpawnReviewedNonPlayerPlacements(LashingFiendSpawns, ref lashingFiendPlacementsSpawned);
                    break;
                case PublicEventPhase.FindMedicalSupplies2:
                    publicEvent.ActivateObjective(PublicEventObjective.FindMedicalSupplies);
                    SpawnReviewedSimplePlacements(SecondMedicalSuppliesSpawns, ref secondMedicalSuppliesPlacementsSpawned);
                    break;
                case PublicEventPhase.HealContaminatedShiphands:
                    publicEvent.ActivateObjective(PublicEventObjective.HealContaminatedShiphand);
                    SpawnReviewedNonPlayerPlacements(ContaminatedShiphandSpawns, ref contaminatedShiphandPlacementsSpawned);
                    break;
                case PublicEventPhase.KillParasites:
                    publicEvent.ActivateObjective(PublicEventObjective.KillLumberingParasites);
                    publicEvent.ActivateObjective(PublicEventObjective.KillCyclopeanParasite);
                    break;
            }
        }

        private void SpawnWipGuessedTurnstileTrigger(uint triggerId, float range, uint objectId, Vector3 position)
        {
            // WIP-guessed from LaughingWS Instances-and-more: the branch marks this
            // cargo-ship turnstile range/position as needing proof. Keep it scoped
            // to the initial participant-gather objective until retail trigger rows
            // or manual expedition smoke confirm the exact placement.
            ITurnstileGridTriggerEntity triggerEntity = publicEvent.CreateEntity<ITurnstileGridTriggerEntity>();
            triggerEntity.Initialise(triggerId, range, objectId);
            AddToMap(triggerEntity, position);
        }

        private void SpawnReviewedCaptainTolben()
        {
            if (captainTolbenSpawned)
                return;

            // Reviewed Infestation instance rows place Captain Tolben as a static
            // opening NPC without an entity_event row; exact dialog and visibility
            // timing remain blocked pending expedition smoke.
            INonPlayerEntity captainTolben = publicEvent.CreateEntity<INonPlayerEntity>();
            captainTolben.Initialise(CreateCaptainTolbenModel());
            AddToMap(captainTolben, CaptainTolbenPosition);
            captainTolbenSpawned = true;
        }

        private static EntityModel CreateCaptainTolbenModel()
        {
            return new EntityModel
            {
                Id          = CaptainTolbenEntityId,
                Type        = EntityType.NonPlayer,
                Creature    = CaptainTolbenCreatureId,
                World       = InfestationWorldId,
                Area        = CaptainTolbenAreaId,
                X           = CaptainTolbenPosition.X,
                Y           = CaptainTolbenPosition.Y,
                Z           = CaptainTolbenPosition.Z,
                DisplayInfo = CaptainTolbenDisplayInfo,
                Faction1    = CaptainTolbenFactionId,
                Faction2    = CaptainTolbenFactionId,
                EntityStat =
                {
                    new EntityStatModel
                    {
                        Stat  = (byte)Stat.Health,
                        Value = 1f
                    }
                }
            };
        }

        private void SpawnReviewedSimplePlacements(IReadOnlyCollection<InfestationSimpleSpawnModel> spawns, ref bool spawned)
        {
            if (spawned)
                return;

            foreach (InfestationSimpleSpawnModel spawn in spawns)
            {
                ISimpleEntity entity = publicEvent.CreateEntity<ISimpleEntity>();
                entity.Initialise(CreateSimpleEntityModel(spawn));
                AddToMap(entity, spawn.Position);
            }

            spawned = true;
        }

        private static EntityModel CreateSimpleEntityModel(InfestationSimpleSpawnModel spawn)
        {
            return new EntityModel
            {
                Id          = spawn.EntityId,
                Type        = EntityType.Simple,
                Creature    = spawn.CreatureId,
                World       = InfestationWorldId,
                Area        = spawn.AreaId,
                X           = spawn.Position.X,
                Y           = spawn.Position.Y,
                Z           = spawn.Position.Z,
                DisplayInfo = spawn.DisplayInfo,
                Faction1    = spawn.FactionId,
                Faction2    = spawn.FactionId,
                EntityEvent = new EntityEventModel
                {
                    EventId = InfestationPublicEventId,
                    Phase   = spawn.Phase
                }
            };
        }

        private void SpawnReviewedNonPlayerPlacements(IReadOnlyCollection<InfestationNonPlayerSpawnModel> spawns, ref bool spawned)
        {
            if (spawned)
                return;

            foreach (InfestationNonPlayerSpawnModel spawn in spawns)
            {
                INonPlayerEntity entity = publicEvent.CreateEntity<INonPlayerEntity>();
                entity.Initialise(CreateNonPlayerEntityModel(spawn));
                AddToMap(entity, spawn.Position);
            }

            spawned = true;
        }

        private static EntityModel CreateNonPlayerEntityModel(InfestationNonPlayerSpawnModel spawn)
        {
            return new EntityModel
            {
                Id          = spawn.EntityId,
                Type        = EntityType.NonPlayer,
                Creature    = spawn.CreatureId,
                World       = InfestationWorldId,
                Area        = spawn.AreaId,
                X           = spawn.Position.X,
                Y           = spawn.Position.Y,
                Z           = spawn.Position.Z,
                DisplayInfo = spawn.DisplayInfo,
                OutfitInfo  = spawn.OutfitInfo,
                Faction1    = spawn.FactionId,
                Faction2    = spawn.FactionId,
                EntityEvent = new EntityEventModel
                {
                    EventId = InfestationPublicEventId,
                    Phase   = spawn.Phase
                }
            };
        }

        private void AddToMap(IGridEntity entity, Vector3 position)
        {
            mapInstance.EnqueueAdd(entity, new ScriptMapPosition
            {
                Info = new ScriptMapInfo
                {
                    Entry   = mapInstance.Entry,
                    MapLock = mapInstance.MapLock
                },
                Position = position
            });
        }

        /// <summary>
        /// Invoked when the <see cref="IPublicEventObjective"/> status changes.
        /// </summary>
        public void OnPublicEventObjectiveStatus(IPublicEventObjective objective)
        {
            if (objective.Status != PublicEventStatus.Succeeded)
                return;

            switch ((PublicEventObjective)objective.Entry.Id)
            {
                case PublicEventObjective.ProceedOntoTheCargoShip:
                    publicEvent.SetPhase(PublicEventPhase.CloseTheShipVents);
                    break;
                case PublicEventObjective.CloseTheShipVents:
                    publicEvent.SetPhase(PublicEventPhase.FindTheMedicalBay);
                    break;
                case PublicEventObjective.SealHullBreaches:
                    publicEvent.SetPhase(PublicEventPhase.FindTheMedicalBay2);
                    break;
                case PublicEventObjective.DefeatTheAttackOnMedbay:
                    publicEvent.SetPhase(PublicEventPhase.FindMedicalSupplies2);
                    break;
                case PublicEventObjective.FindMedicalSupplies:
                    publicEvent.SetPhase(PublicEventPhase.HealContaminatedShiphands);
                    break;
                case PublicEventObjective.HealContaminatedShiphand:
                    publicEvent.SetPhase(PublicEventPhase.KillParasites);
                    break;
                case PublicEventObjective.KillCyclopeanParasite:
                case PublicEventObjective.KillLumberingParasites:
                    publicEvent.UpdateObjective(PublicEventObjective.GoldMedalTimer, 0);
                    publicEvent.Finish(PublicEventTeam.PublicTeam);
                    break;
            }
        }

        private sealed record InfestationSimpleSpawnModel(
            uint EntityId,
            uint CreatureId,
            ushort AreaId,
            uint Phase,
            Vector3 Position,
            uint DisplayInfo,
            ushort FactionId);

        private sealed record InfestationNonPlayerSpawnModel(
            uint EntityId,
            uint CreatureId,
            ushort AreaId,
            uint Phase,
            Vector3 Position,
            uint DisplayInfo,
            ushort OutfitInfo,
            ushort FactionId);
    }
}
