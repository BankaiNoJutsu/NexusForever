using System.Numerics;
using NexusForever.Database.World.Model;
using NexusForever.Game.Abstract.Cinematic;
using NexusForever.Game.Abstract.Cinematic.Cinematics;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Entity.Trigger;
using NexusForever.Game.Abstract.Map.Instance;
using NexusForever.Game.Abstract.PublicEvent;
using NexusForever.Game.Static.Entity;
using NexusForever.Game.Static.PublicEvent;
using NexusForever.Script.Template;
using NexusForever.Script.Template.Filter;

namespace NexusForever.Script.Instance.Dungeon.StormtalonsLair
{
    [ScriptFilterOwnerId(145)]
    public class StormtalonsLairEventScript : IPublicEventScript, IOwnedScript<IPublicEvent>
    {
        private readonly ICinematicFactory cinematicFactory;

        private IPublicEvent publicEvent;
        private IMapInstance mapInstance;
        private uint highPriestTriggerGuid;
        private bool taintedFlowerStemPlacementsSpawned;
        private bool thundercallCagePlacementsSpawned;
        private bool improvementConstructionPlatformPlacementsSpawned;
        private bool thundercallDataAltarPlacementsSpawned;
        private bool thundercallStormTotemPlacementsSpawned;
        private bool launchPadPlacementsSpawned;

        private const uint HighPriestWorldLocationId = 12736u;
        private const uint HighPriestObjectId = 1831u;

        private const uint AethrosEntityId = 1100300047u;
        private const uint AethrosCreatureId = 32703u;
        private const ushort AethrosWorldId = 382;
        private const ushort AethrosAreaId = 271;
        private const uint AethrosPublicEventId = 145u;
        private const uint AethrosPublicEventPhase = 2u;
        private const uint AethrosDisplayInfo = 27874u;
        private const ushort AethrosFactionId = 586;
        private const string AethrosScriptName = "AethrosVeteranEntityScript";

        private const uint BladeWindEntityId = 1100300048u;
        private const uint BladeWindCreatureId = 33405u;
        private const ushort BladeWindWorldId = 382;
        private const ushort BladeWindAreaId = 271;
        private const uint BladeWindPublicEventId = 145u;
        private const uint BladeWindPublicEventPhase = 1u;
        private const uint BladeWindDisplayInfo = 23629u;
        private const ushort BladeWindFactionId = 586;
        private const string BladeWindScriptName = "BladeWindTheInvokerVeteranEntityScript";

        private const uint ThundercallCageCreatureId = 17191u;
        private const ushort ThundercallCageWorldId = 382;
        private const ushort ThundercallCageAreaId = 271;
        private const uint ThundercallCagePublicEventId = 145u;
        private const uint ThundercallCagePublicEventPhase = 2u;
        private const uint ThundercallCageDisplayInfo = 21397u;
        private const ushort ThundercallCageFactionId = 219;

        private const uint TaintedFlowerStemCreatureId = 24307u;
        private const ushort TaintedFlowerStemWorldId = 382;
        private const ushort TaintedFlowerStemAreaId = 271;
        private const uint TaintedFlowerStemPublicEventId = 145u;
        private const uint TaintedFlowerStemPublicEventPhase = 0u;
        private const uint TaintedFlowerStemDisplayInfo = 36934u;
        private const ushort TaintedFlowerStemFactionId = 219;

        private const uint ImprovementConstructionPlatformCreatureId = 27244u;
        private const ushort ImprovementConstructionPlatformWorldId = 382;
        private const ushort ImprovementConstructionPlatformAreaId = 271;
        private const uint ImprovementConstructionPlatformPublicEventId = 145u;
        private const uint ImprovementConstructionPlatformPublicEventPhase = 0u;
        private const uint ImprovementConstructionPlatformDisplayInfo = 23885u;
        private const ushort ImprovementConstructionPlatformFactionId = 219;

        private const uint ThundercallDataAltarCreatureId = 24314u;
        private const ushort ThundercallDataAltarWorldId = 382;
        private const ushort ThundercallDataAltarAreaId = 271;
        private const uint ThundercallDataAltarPublicEventId = 145u;
        private const uint ThundercallDataAltarPublicEventPhase = 1u;
        private const uint ThundercallDataAltarDisplayInfo = 22896u;
        private const ushort ThundercallDataAltarFactionId = 219;

        private const uint ThundercallStormTotemCreatureId = 27262u;
        private const ushort ThundercallStormTotemWorldId = 382;
        private const ushort ThundercallStormTotemAreaId = 271;
        private const uint ThundercallStormTotemPublicEventId = 145u;
        private const uint ThundercallStormTotemPublicEventPhase = 1u;
        private const uint ThundercallStormTotemDisplayInfo = 27702u;
        private const ushort ThundercallStormTotemFactionId = 219;
        private const float ThundercallStormTotemInterruptArmour = 2f;

        private const uint LaunchPadCreatureId = 31587u;
        private const ushort LaunchPadWorldId = 382;
        private const ushort LaunchPadAreaId = 271;
        private const uint LaunchPadPublicEventId = 145u;
        private const uint LaunchPadPublicEventPhase = 1u;
        private const uint LaunchPadDisplayInfo = 26896u;
        private const ushort LaunchPadFactionId = 219;

        private static readonly Vector3 HighPriestPosition = new(54.2672f, -11.5711f, 257.232f);
        private static readonly Vector3 AethrosPosition = new(-52.71065f, -48.68514f, 220.7862f);
        private static readonly Vector3 BladeWindPosition = new(-88.49673f, -11.51191f, 123.5562f);

        private static readonly StormtalonSimpleCollidableSpawnModel[] ThundercallCageSpawns =
        [
            // Jabbithole PE creature 837 / DataMapping source coordinates
            // 3241-3244 and 20323 map the five build 16042 TargetGroup 1661
            // / Creature2 17191 sacrificial cages for objective 464.
            new(1100038201u, new Vector3(-230f, -31f, 130f)),
            new(1100038202u, new Vector3(-149f, -32f, 177f)),
            new(1100038203u, new Vector3(-203f, -32f, 194f)),
            new(1100038204u, new Vector3(-180f, -32f, 105f)),
            new(1100038205u, new Vector3(-226f, -30f, 86f))
        ];

        private static readonly StormtalonSimpleSpawnModel[] TaintedFlowerStemSpawns =
        [
            // Jabbithole PE objective 4199 and public-event creature 1872
            // source creature 26518; DataMapping source coordinates
            // 5195326-5204915 plus 5224306 review as unique-name build 16042
            // TargetGroup 2548 / Creature2 24307 Tainted Flower Stem producers
            // for objective 554.
            new(1100038210u, new Vector3(-141f, -32f, 239f)),
            new(1100038211u, new Vector3(39f, 12f, 89f)),
            new(1100038212u, new Vector3(-128f, -33f, 183f)),
            new(1100038213u, new Vector3(35f, 0f, 149f)),
            new(1100038214u, new Vector3(-130f, -32f, 183f)),
            new(1100038215u, new Vector3(49f, 1f, 131f)),
            new(1100038216u, new Vector3(-58f, -39f, 261f)),
            new(1100038217u, new Vector3(-26f, -45f, 246f)),
            new(1100038218u, new Vector3(-120f, -7f, 159f)),
            new(1100038219u, new Vector3(-116f, -7f, 162f)),
            new(1100038220u, new Vector3(-40f, 13f, 17f)),
            new(1100038221u, new Vector3(-43f, 14f, 20f)),
            new(1100038222u, new Vector3(-15f, -47f, 235f)),
            new(1100038223u, new Vector3(-24f, -45f, 243f)),
            new(1100038224u, new Vector3(-120f, -7f, 156f)),
            new(1100038225u, new Vector3(-140f, -33f, 196f)),
            new(1100038226u, new Vector3(-139f, -35f, 234f)),
            new(1100038227u, new Vector3(-136f, -36f, 234f)),
            new(1100038228u, new Vector3(29f, -47f, 150f)),
            new(1100038229u, new Vector3(44f, 1f, 135f)),
            new(1100038230u, new Vector3(-119f, -7f, 139f)),
            new(1100038231u, new Vector3(-117f, -9f, 137f)),
            new(1100038232u, new Vector3(-52f, -43f, 254f)),
            new(1100038233u, new Vector3(218f, -29f, 221f)),
            new(1100038234u, new Vector3(84f, -45f, 159f)),
            new(1100038235u, new Vector3(85f, -45f, 155f)),
            new(1100038236u, new Vector3(27f, -48f, 151f)),
            new(1100038237u, new Vector3(24f, 10f, 48f)),
            new(1100038238u, new Vector3(39f, 16f, 49f)),
            new(1100038239u, new Vector3(41f, 12f, 89f)),
            new(1100038240u, new Vector3(212f, -31f, 218f)),
            new(1100038241u, new Vector3(76f, -45f, 161f))
        ];

        private static readonly StormtalonSimpleSpawnModel[] ImprovementConstructionPlatformSpawns =
        [
            // Jabbithole PE creature 760 / coordinate 2975 maps the build 16042
            // Creature2 27244 platform near WorldLocation2 20740 for objectives
            // 558 and 4867.
            new(1100038242u, new Vector3(-304f, 10f, 248f))
        ];

        private static readonly StormtalonSimpleSpawnModel[] ThundercallDataAltarSpawns =
        [
            // Jabbithole PE creature 824 / coordinate 3201 maps the build 16042
            // Creature2 24314 data altar for objective 555.
            new(1100038243u, new Vector3(-95f, -9f, 158f))
        ];

        private static readonly StormtalonNonPlayerSpawnModel[] ThundercallStormTotemSpawns =
        [
            // Jabbithole PE creature 1344 / coordinates 5689-5693 and
            // 22995-22997 map the eight build 16042 TargetGroup 4419 /
            // Creature2 27262 storm totem producers for objective 562.
            new(1100038244u, new Vector3(-15f, 12f, 64f)),
            new(1100038245u, new Vector3(-42f, -8f, 155f)),
            new(1100038246u, new Vector3(73f, -46f, 190f)),
            new(1100038247u, new Vector3(-321f, 7f, 200f)),
            new(1100038248u, new Vector3(126f, -18f, 338f)),
            new(1100038249u, new Vector3(-155f, -14f, 118f)),
            new(1100038250u, new Vector3(-86f, -43f, 185f)),
            new(1100038251u, new Vector3(-336f, -4f, 57f))
        ];

        private static readonly StormtalonSimpleSpawnModel[] LaunchPadSpawns =
        [
            // Jabbithole PE creature 1046 / coordinates 4296 and 4297 map the
            // two build 16042 TargetGroup 3679 / Creature2 31587 launch pads
            // for objective 559.
            new(1100038252u, new Vector3(-233f, -30f, 153f)),
            new(1100038253u, new Vector3(-232f, -28f, 74f))
        ];

        public StormtalonsLairEventScript(
            ICinematicFactory cinematicFactory)
        {
            this.cinematicFactory = cinematicFactory;
        }

        /// <summary>
        /// Invoked when <see cref="IScript"/> is loaded.
        /// </summary>
        public void OnLoad(IPublicEvent owner)
        {
            publicEvent = owner;
            mapInstance = publicEvent.Map as IMapInstance
                ?? throw new InvalidOperationException("Stormtalon's Lair requires a map instance.");

            publicEvent.SetPhase(PublicEventPhase.Enter);
            taintedFlowerStemPlacementsSpawned = false;
            thundercallCagePlacementsSpawned = false;
            improvementConstructionPlatformPlacementsSpawned = false;
            thundercallDataAltarPlacementsSpawned = false;
            thundercallStormTotemPlacementsSpawned = false;
            launchPadPlacementsSpawned = false;
        }

        /// <summary>
        /// Invoked when a <see cref="IGridEntity"/> is added to the map the public event is on.
        /// </summary>
        public void OnAddToMap(IGridEntity entity)
        {
            if (entity is IWorldLocationVolumeGridTriggerEntity worldLocationEntity
                && worldLocationEntity.Entry.Id == HighPriestWorldLocationId)
                highPriestTriggerGuid = worldLocationEntity.Guid;
        }

        /// <summary>
        /// Invoked when a <see cref="IGridEntity"/> is removed from the map the public event is on.
        /// </summary>
        public void OnRemoveFromMap(IGridEntity entity)
        {
            if (entity is IWorldLocationVolumeGridTriggerEntity worldLocationEntity
                && worldLocationEntity.Entry.Id == HighPriestWorldLocationId)
                highPriestTriggerGuid = 0;
        }

        /// <summary>
        /// Invoked when the public event phase changes.
        /// </summary>
        public void OnPublicEventPhase(uint phase)
        {
            switch ((PublicEventPhase)phase)
            {
                case PublicEventPhase.Enter:
                    publicEvent.ActivateObjective(PublicEventObjective.SurviveTheThundercallPellZealots);
                    publicEvent.ActivateObjective(PublicEventObjective.EliminateThundercallPell);
                    ActivateWipOptionalObjective(PublicEventObjective.KillThundercallPellBeforeTimeExpires);
                    if (ActivateWipOptionalObjective(PublicEventObjective.GatherTaintedStemSamples))
                        SpawnReviewedTaintedFlowerStems();
                    bool platformObjectiveActive = ActivateWipOptionalObjective(PublicEventObjective.ActivateImprovementConstructionPlatform);
                    bool holocryptObjectiveActive = ActivateWipOptionalObjective(PublicEventObjective.EnableInvokersHolocrypt);
                    if (platformObjectiveActive || holocryptObjectiveActive)
                        SpawnReviewedImprovementConstructionPlatforms();
                    break;
                case PublicEventPhase.DefeatBladeWindTheInvoker:
                    publicEvent.ActivateObjective(PublicEventObjective.DefeatBladeWindTheInvoker);
                    SpawnBladeWindTheInvoker();
                    if (ActivateWipOptionalObjective(PublicEventObjective.ObtainDataFromTheThundercallDataAltar))
                        SpawnReviewedThundercallDataAltars();
                    if (ActivateWipOptionalObjective(PublicEventObjective.HijackPowerFromTheThundercallStormTotems))
                        SpawnReviewedThundercallStormTotems();
                    if (ActivateWipOptionalObjective(PublicEventObjective.ActivateLaunchPads))
                        SpawnReviewedLaunchPads();
                    break;
                case PublicEventPhase.EliminateAethros:
                    publicEvent.ActivateObjective(PublicEventObjective.EliminateAethros);
                    SpawnAethros();
                    if (ActivateWipOptionalObjective(PublicEventObjective.FreeTheThundercallSacrificialPrisoners))
                        SpawnReviewedThundercallCages();
                    ActivateWipOptionalObjective(PublicEventObjective.UseYourGrenadesToDisableTheThundercallPell);
                    publicEvent.ActivateObjective(ShouldUseWipArcanistVariant()
                        ? PublicEventObjective.DefeatArcanistBreezeBinderForTheEncryptionKey
                        : PublicEventObjective.KillOverseerDriftCatcher);
                    break;
                case PublicEventPhase.StopTheThundercallHighPriest:
                    OnPhaseStopTheThundercallHighPriest();
                    break;
                case PublicEventPhase.DestroyStormtalon:
                    QueueWipGuessedCinematic<IStormtalonLairStormtalonReborn>();
                    publicEvent.ActivateObjective(PublicEventObjective.DestroyStormtalon);
                    break;
            }
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
                case PublicEventObjective.SurviveTheThundercallPellZealots:
                    publicEvent.SetPhase(PublicEventPhase.DefeatBladeWindTheInvoker);
                    break;
                case PublicEventObjective.DefeatBladeWindTheInvoker:
                    publicEvent.SetPhase(PublicEventPhase.EliminateAethros);
                    break;
                case PublicEventObjective.EliminateAethros:
                    publicEvent.SetPhase(PublicEventPhase.StopTheThundercallHighPriest);
                    break;
                case PublicEventObjective.StopTheThundercallHighPriest:
                    RemoveHighPriestTrigger();
                    publicEvent.SetPhase(PublicEventPhase.DestroyStormtalon);
                    break;
                case PublicEventObjective.DestroyStormtalon:
                    publicEvent.Finish(PublicEventTeam.PublicTeam);
                    break;
            }
        }

        private void SpawnAethros()
        {
            // Build 16042 reviewed instance entity 1100300047 places Aethros
            // in event 145 phase 2 with the veteran death-credit script.
            INonPlayerEntity aethros = publicEvent.CreateEntity<INonPlayerEntity>();
            aethros.Initialise(CreateAethrosEntityModel());
            AddToMap(aethros, AethrosPosition);
        }

        private static EntityModel CreateAethrosEntityModel()
        {
            return new EntityModel
            {
                Id          = AethrosEntityId,
                Type        = EntityType.NonPlayer,
                Creature    = AethrosCreatureId,
                World       = AethrosWorldId,
                Area        = AethrosAreaId,
                X           = AethrosPosition.X,
                Y           = AethrosPosition.Y,
                Z           = AethrosPosition.Z,
                DisplayInfo = AethrosDisplayInfo,
                Faction1    = AethrosFactionId,
                Faction2    = AethrosFactionId,
                EntityEvent = new EntityEventModel
                {
                    EventId = AethrosPublicEventId,
                    Phase   = AethrosPublicEventPhase
                },
                EntityScript =
                {
                    new EntityScriptModel
                    {
                        ScriptName = AethrosScriptName
                    }
                },
                EntityStat =
                {
                    new EntityStatModel
                    {
                        Stat  = (byte)Stat.Health,
                        Value = 1f
                    },
                    new EntityStatModel
                    {
                        Stat  = (byte)Stat.Level,
                        Value = 50f
                    }
                }
            };
        }

        private void SpawnBladeWindTheInvoker()
        {
            // Build 16042 reviewed instance entity 1100300048 places Blade-Wind
            // the Invoker in event 145 phase 1 with the veteran death-credit script.
            INonPlayerEntity bladeWind = publicEvent.CreateEntity<INonPlayerEntity>();
            bladeWind.Initialise(CreateBladeWindEntityModel());
            AddToMap(bladeWind, BladeWindPosition);
        }

        private static EntityModel CreateBladeWindEntityModel()
        {
            return new EntityModel
            {
                Id          = BladeWindEntityId,
                Type        = EntityType.NonPlayer,
                Creature    = BladeWindCreatureId,
                World       = BladeWindWorldId,
                Area        = BladeWindAreaId,
                X           = BladeWindPosition.X,
                Y           = BladeWindPosition.Y,
                Z           = BladeWindPosition.Z,
                DisplayInfo = BladeWindDisplayInfo,
                Faction1    = BladeWindFactionId,
                Faction2    = BladeWindFactionId,
                EntityEvent = new EntityEventModel
                {
                    EventId = BladeWindPublicEventId,
                    Phase   = BladeWindPublicEventPhase
                },
                EntityScript =
                {
                    new EntityScriptModel
                    {
                        ScriptName = BladeWindScriptName
                    }
                },
                EntityStat =
                {
                    new EntityStatModel
                    {
                        Stat  = (byte)Stat.Health,
                        Value = 1f
                    },
                    new EntityStatModel
                    {
                        Stat  = (byte)Stat.Level,
                        Value = 50f
                    }
                }
            };
        }

        private void SpawnReviewedTaintedFlowerStems()
        {
            if (taintedFlowerStemPlacementsSpawned)
                return;

            foreach (StormtalonSimpleSpawnModel spawn in TaintedFlowerStemSpawns)
            {
                ISimpleEntity stem = publicEvent.CreateEntity<ISimpleEntity>();
                stem.Initialise(CreateTaintedFlowerStemEntityModel(spawn));
                AddToMap(stem, spawn.Position);
            }

            taintedFlowerStemPlacementsSpawned = true;
        }

        private static EntityModel CreateTaintedFlowerStemEntityModel(StormtalonSimpleSpawnModel spawn)
        {
            return new EntityModel
            {
                Id          = spawn.EntityId,
                Type        = EntityType.Simple,
                Creature    = TaintedFlowerStemCreatureId,
                World       = TaintedFlowerStemWorldId,
                Area        = TaintedFlowerStemAreaId,
                X           = spawn.Position.X,
                Y           = spawn.Position.Y,
                Z           = spawn.Position.Z,
                DisplayInfo = TaintedFlowerStemDisplayInfo,
                Faction1    = TaintedFlowerStemFactionId,
                Faction2    = TaintedFlowerStemFactionId,
                EntityEvent = new EntityEventModel
                {
                    EventId = TaintedFlowerStemPublicEventId,
                    Phase   = TaintedFlowerStemPublicEventPhase
                }
            };
        }

        private void SpawnReviewedImprovementConstructionPlatforms()
        {
            if (improvementConstructionPlatformPlacementsSpawned)
                return;

            foreach (StormtalonSimpleSpawnModel spawn in ImprovementConstructionPlatformSpawns)
            {
                ISimpleEntity platform = publicEvent.CreateEntity<ISimpleEntity>();
                platform.Initialise(CreateImprovementConstructionPlatformEntityModel(spawn));
                AddToMap(platform, spawn.Position);
            }

            improvementConstructionPlatformPlacementsSpawned = true;
        }

        private static EntityModel CreateImprovementConstructionPlatformEntityModel(StormtalonSimpleSpawnModel spawn)
        {
            return new EntityModel
            {
                Id          = spawn.EntityId,
                Type        = EntityType.Simple,
                Creature    = ImprovementConstructionPlatformCreatureId,
                World       = ImprovementConstructionPlatformWorldId,
                Area        = ImprovementConstructionPlatformAreaId,
                X           = spawn.Position.X,
                Y           = spawn.Position.Y,
                Z           = spawn.Position.Z,
                DisplayInfo = ImprovementConstructionPlatformDisplayInfo,
                Faction1    = ImprovementConstructionPlatformFactionId,
                Faction2    = ImprovementConstructionPlatformFactionId,
                EntityEvent = new EntityEventModel
                {
                    EventId = ImprovementConstructionPlatformPublicEventId,
                    Phase   = ImprovementConstructionPlatformPublicEventPhase
                }
            };
        }

        private void SpawnReviewedThundercallDataAltars()
        {
            if (thundercallDataAltarPlacementsSpawned)
                return;

            foreach (StormtalonSimpleSpawnModel spawn in ThundercallDataAltarSpawns)
            {
                ISimpleEntity altar = publicEvent.CreateEntity<ISimpleEntity>();
                altar.Initialise(CreateThundercallDataAltarEntityModel(spawn));
                AddToMap(altar, spawn.Position);
            }

            thundercallDataAltarPlacementsSpawned = true;
        }

        private static EntityModel CreateThundercallDataAltarEntityModel(StormtalonSimpleSpawnModel spawn)
        {
            return new EntityModel
            {
                Id          = spawn.EntityId,
                Type        = EntityType.Simple,
                Creature    = ThundercallDataAltarCreatureId,
                World       = ThundercallDataAltarWorldId,
                Area        = ThundercallDataAltarAreaId,
                X           = spawn.Position.X,
                Y           = spawn.Position.Y,
                Z           = spawn.Position.Z,
                DisplayInfo = ThundercallDataAltarDisplayInfo,
                Faction1    = ThundercallDataAltarFactionId,
                Faction2    = ThundercallDataAltarFactionId,
                EntityEvent = new EntityEventModel
                {
                    EventId = ThundercallDataAltarPublicEventId,
                    Phase   = ThundercallDataAltarPublicEventPhase
                }
            };
        }

        private void SpawnReviewedThundercallStormTotems()
        {
            if (thundercallStormTotemPlacementsSpawned)
                return;

            foreach (StormtalonNonPlayerSpawnModel spawn in ThundercallStormTotemSpawns)
            {
                INonPlayerEntity totem = publicEvent.CreateEntity<INonPlayerEntity>();
                totem.Initialise(CreateThundercallStormTotemEntityModel(spawn));
                AddToMap(totem, spawn.Position);
            }

            thundercallStormTotemPlacementsSpawned = true;
        }

        private static EntityModel CreateThundercallStormTotemEntityModel(StormtalonNonPlayerSpawnModel spawn)
        {
            return new EntityModel
            {
                Id          = spawn.EntityId,
                Type        = EntityType.NonPlayer,
                Creature    = ThundercallStormTotemCreatureId,
                World       = ThundercallStormTotemWorldId,
                Area        = ThundercallStormTotemAreaId,
                X           = spawn.Position.X,
                Y           = spawn.Position.Y,
                Z           = spawn.Position.Z,
                DisplayInfo = ThundercallStormTotemDisplayInfo,
                Faction1    = ThundercallStormTotemFactionId,
                Faction2    = ThundercallStormTotemFactionId,
                EntityEvent = new EntityEventModel
                {
                    EventId = ThundercallStormTotemPublicEventId,
                    Phase   = ThundercallStormTotemPublicEventPhase
                },
                EntityStat =
                {
                    new EntityStatModel
                    {
                        Stat  = (byte)Stat.InterruptArmour,
                        Value = ThundercallStormTotemInterruptArmour
                    }
                }
            };
        }

        private void SpawnReviewedLaunchPads()
        {
            if (launchPadPlacementsSpawned)
                return;

            foreach (StormtalonSimpleSpawnModel spawn in LaunchPadSpawns)
            {
                ISimpleEntity launchPad = publicEvent.CreateEntity<ISimpleEntity>();
                launchPad.Initialise(CreateLaunchPadEntityModel(spawn));
                AddToMap(launchPad, spawn.Position);
            }

            launchPadPlacementsSpawned = true;
        }

        private static EntityModel CreateLaunchPadEntityModel(StormtalonSimpleSpawnModel spawn)
        {
            return new EntityModel
            {
                Id          = spawn.EntityId,
                Type        = EntityType.Simple,
                Creature    = LaunchPadCreatureId,
                World       = LaunchPadWorldId,
                Area        = LaunchPadAreaId,
                X           = spawn.Position.X,
                Y           = spawn.Position.Y,
                Z           = spawn.Position.Z,
                DisplayInfo = LaunchPadDisplayInfo,
                Faction1    = LaunchPadFactionId,
                Faction2    = LaunchPadFactionId,
                EntityEvent = new EntityEventModel
                {
                    EventId = LaunchPadPublicEventId,
                    Phase   = LaunchPadPublicEventPhase
                }
            };
        }

        private void SpawnReviewedThundercallCages()
        {
            if (thundercallCagePlacementsSpawned)
                return;

            foreach (StormtalonSimpleCollidableSpawnModel spawn in ThundercallCageSpawns)
            {
                ISimpleCollidableEntity cage = publicEvent.CreateEntity<ISimpleCollidableEntity>();
                cage.Initialise(CreateThundercallCageEntityModel(spawn));
                AddToMap(cage, spawn.Position);
            }

            thundercallCagePlacementsSpawned = true;
        }

        private static EntityModel CreateThundercallCageEntityModel(StormtalonSimpleCollidableSpawnModel spawn)
        {
            return new EntityModel
            {
                Id          = spawn.EntityId,
                Type        = EntityType.SimpleCollidable,
                Creature    = ThundercallCageCreatureId,
                World       = ThundercallCageWorldId,
                Area        = ThundercallCageAreaId,
                X           = spawn.Position.X,
                Y           = spawn.Position.Y,
                Z           = spawn.Position.Z,
                DisplayInfo = ThundercallCageDisplayInfo,
                Faction1    = ThundercallCageFactionId,
                Faction2    = ThundercallCageFactionId,
                EntityEvent = new EntityEventModel
                {
                    EventId = ThundercallCagePublicEventId,
                    Phase   = ThundercallCagePublicEventPhase
                }
            };
        }

        private void OnPhaseStopTheThundercallHighPriest()
        {
            publicEvent.ActivateObjective(PublicEventObjective.StopTheThundercallHighPriest, mapInstance.PlayerCount);

            // Build 16042 exposes objective 540 as a ParticipantsInTriggerVolume
            // row targeting object 1831 at WorldLocation2 12736.
            IWorldLocationVolumeGridTriggerEntity triggerEntity = publicEvent.CreateEntity<IWorldLocationVolumeGridTriggerEntity>();
            triggerEntity.Initialise(HighPriestWorldLocationId, HighPriestObjectId);
            AddToMap(triggerEntity, HighPriestPosition);
        }

        private void RemoveHighPriestTrigger()
        {
            IWorldLocationVolumeGridTriggerEntity triggerEntity = mapInstance.GetEntity<IWorldLocationVolumeGridTriggerEntity>(highPriestTriggerGuid);
            triggerEntity?.RemoveFromMap();
        }

        private bool ActivateWipOptionalObjective(PublicEventObjective objective)
        {
            // WIP-guessed from LaughingWS Instances-and-more. The branch randomly
            // includes these optional objectives in the Stormtalon's Lair route; exact
            // retail route weights, trigger placement, and objective availability remain blocked.
            if (!ShouldActivateWipOptionalObjective(objective))
                return false;

            publicEvent.ActivateObjective(objective);
            return true;
        }

        protected virtual bool ShouldActivateWipOptionalObjective(PublicEventObjective objective)
        {
            return Random.Shared.Next(2) == 1;
        }

        protected virtual bool ShouldUseWipArcanistVariant()
        {
            return Random.Shared.Next(2) == 1;
        }

        private void QueueWipGuessedCinematic<T>() where T : ICinematicBase
        {
            // WIP-guessed from LaughingWS Instances-and-more. The branch queues this
            // Stormtalon reborn cinematic before the final objective, but the real
            // cinematic payload and boss version selection remain blocked.
            foreach (IPlayer player in mapInstance.GetPlayers())
                player.CinematicManager.QueueCinematic(cinematicFactory.CreateCinematic<T>());
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

        private sealed record StormtalonSimpleSpawnModel(
            uint EntityId,
            Vector3 Position);

        private sealed record StormtalonSimpleCollidableSpawnModel(
            uint EntityId,
            Vector3 Position);

        private sealed record StormtalonNonPlayerSpawnModel(
            uint EntityId,
            Vector3 Position);
    }
}
