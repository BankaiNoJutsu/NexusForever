using System.Numerics;
using NexusForever.Database.World.Model;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Map.Instance;
using NexusForever.Game.Abstract.PublicEvent;
using NexusForever.Game.Static.Entity;
using NexusForever.Game.Static.PublicEvent;
using NexusForever.Script.Template;
using NexusForever.Script.Template.Filter;

namespace NexusForever.Script.Instance.Dungeon.UltimateProtogames
{
    [ScriptFilterOwnerId(594)]
    public class UltimateProtogamesEventScript : IPublicEventScript, IOwnedScript<IPublicEvent>
    {
        private const uint StartButtonEntityId = 6128905u;
        private const uint StartButtonCreatureId = 65900u;
        private const ushort StartButtonWorldId = 2980;
        private const ushort StartButtonAreaId = 4330;
        private const uint StartButtonPublicEventId = 594u;
        private const uint StartButtonPublicEventPhase = 0u;
        private const uint StartButtonDisplayInfo = 25115u;
        private const ushort StartButtonFactionId = 219;
        private const string StartButtonScriptName = "UltimateProtogamesStartButtonEntityScript";

        private const uint BevORageEntityId = 1100300055u;
        private const uint BevORageCreatureId = 61463u;
        private const ushort BevORageWorldId = 2980;
        private const ushort BevORageAreaId = 0;
        private const uint BevORagePublicEventId = 594u;
        private const uint BevORagePublicEventPhase = 5u;
        private const uint BevORageDisplayInfo = 36682u;
        private const ushort BevORageFactionId = 219;
        private const string BevORageScriptName = "BevORageEntityScript";

        private const ushort TankRoomWorldId = 2980;
        private const ushort TankRoomAreaId = 4336;
        private const uint TankRoomPublicEventId = 594u;
        private const uint TankRoomPublicEventPhase = (uint)PublicEventPhase.TankRoom;
        private const ushort TankRoomFactionId = 219;
        private const string TankRoomScriptName = "MalfunctioningTankEntityScript";
        private const uint TankRoomAllJunkCount = 3u;
        private const float TankRoomTankHealth = 1563977f;
        private const float TankRoomTankLevel = 50f;
        private const float TankRoomTankShield = 0f;

        private const uint MisplacedMammothEntityId = 1100300080u;
        private const uint MisplacedMammothCreatureId = 63312u;
        private const ushort MisplacedMammothWorldId = 2980;
        private const ushort MisplacedMammothAreaId = 4351;
        private const uint MisplacedMammothPublicEventId = 594u;
        private const uint MisplacedMammothPublicEventPhase = (uint)PublicEventPhase.MisplacedMammoth;
        private const uint MisplacedMammothDisplayInfo = 26004u;
        private const ushort MisplacedMammothFactionId = 1322;
        private const string MisplacedMammothScriptName = "MisplacedMammothEntityScript";
        private const float MisplacedMammothHealth = 398594f;
        private const float MisplacedMammothLevel = 50f;
        private const float MisplacedMammothShield = 0f;
        private const float MisplacedMammothInterruptArmour = 3f;
        private const uint MondosMonstrosityEntityId = 1100300087u;
        private const uint MondosMonstrosityCreatureId = 62575u;
        private const uint MondosMonstrosityDisplayInfo = 21714u;
        private const ushort MondosMonstrosityFactionId = 1322;
        private const string MondosMonstrosityScriptName = "MondosMonstrosityEntityScript";
        private const float MondosMonstrosityHealth = 1369030f;
        private const float MondosMonstrosityLevel = 50f;
        private const float MondosMonstrosityShield = 0f;
        private const uint MondosCrateEntityId = 1100300090u;
        private const uint MondosCrateCreatureId = 62549u;
        private const uint MondosCrateDisplayInfo = 28700u;
        private const ushort MondosCrateFactionId = 1330;
        private const string MondosCrateScriptName = "MondosCrateEntityScript";
        private const float MondosCrateHealth = 1410065408f;
        private const float MondosCrateLevel = 50f;
        private const float MondosCrateShield = 0f;
        private const float MondosCrateInterruptArmour = 2f;

        private const uint RufflesEntityId = 1100300088u;
        private const uint RufflesCreatureId = 65794u;
        private const ushort RufflesWorldId = 2980;
        private const ushort RufflesAreaId = 4348;
        private const uint RufflesPublicEventId = 594u;
        private const uint RufflesPublicEventPhase = (uint)PublicEventPhase.Ruffles;
        private const uint RufflesDisplayInfo = 23396u;
        private const ushort RufflesFactionId = 1322;
        private const string RufflesScriptName = "RufflesEntityScript";
        private const float RufflesHealth = 3420014f;
        private const float RufflesLevel = 50f;
        private const float RufflesShield = 0f;
        private const float RufflesInterruptArmour = 2f;

        private const uint GildedFowlEntityId = 1100300089u;
        private const uint GildedFowlCreatureId = 63055u;
        private const ushort GildedFowlWorldId = 2980;
        private const ushort GildedFowlAreaId = 4347;
        private const uint GildedFowlPublicEventId = 594u;
        private const uint GildedFowlPublicEventPhase = (uint)PublicEventPhase.PowerPlunge;
        private const uint GildedFowlDisplayInfo = 21895u;
        private const ushort GildedFowlFactionId = 1322;
        private const string GildedFowlScriptName = "GildedFowlEntityScript";
        private const float GildedFowlHealth = 461770f;
        private const float GildedFowlLevel = 50f;
        private const float GildedFowlShield = 0f;
        private const float GildedFowlInterruptArmour = 0f;

        private const uint HutHutEntityId = 1100300091u;
        private const uint HutHutCreatureId = 61417u;
        private const ushort HutHutWorldId = 2980;
        private const ushort HutHutAreaId = 4376;
        private const uint HutHutPublicEventId = 594u;
        private const uint HutHutPublicEventPhase = (uint)PublicEventPhase.HutHut;
        private const uint HutHutDisplayInfo = 29048u;
        private const ushort HutHutFactionId = 1322;
        private const string HutHutScriptName = "HutHutEntityScript";
        private const float HutHutHealth = 2400000f;
        private const float HutHutLevel = 50f;
        private const float HutHutShield = 0f;

        private const ushort PrototentiaryWorldId = 2980;
        private const ushort PrototentiaryAreaId = 4333;
        private const uint PrototentiaryPublicEventId = 594u;
        private const uint PrototentiaryPublicEventPhase = (uint)PublicEventPhase.Prototentiary;
        private const uint PrototentiaryAggregateObjectiveCount = 2u;
        private const uint PrototentiaryConsoleObjectiveCount = 1u;
        private const uint PrototentiaryDeputyEntityId = 1100300086u;
        private const uint PrototentiaryDeputyCreatureId = 68949u;
        private const uint PrototentiaryDeputyDisplayInfo = 36776u;
        private const ushort PrototentiaryDeputyFactionId = 1322;
        private const string PrototentiaryDeputyScriptName = "DeputyEntityScript";
        private const float PrototentiaryDeputyLevel = 50f;
        private const float PrototentiaryDeputyInterruptArmour = 24f;
        private const uint PrototentiaryWardenEntityId = 1100300092u;
        private const uint PrototentiaryWardenCreatureId = 62324u;
        private const uint PrototentiaryWardenDisplayInfo = 24369u;
        private const ushort PrototentiaryWardenFactionId = 1322;
        private const string PrototentiaryWardenScriptName = "WardenEntityScript";
        private const float PrototentiaryWardenHealth = 830314f;
        private const float PrototentiaryWardenLevel = 50f;
        private const float PrototentiaryWardenShield = 90900f;
        private const float PrototentiaryWardenInterruptArmour = 5f;

        private static readonly Vector3 BevORagePosition = new(-12737.13f, -797.0872f, 1326.347f);
        private static readonly Vector3 StartButtonPosition = new(-12543f, -776f, -2766f);
        private static readonly Vector3 MisplacedMammothPosition = new(-16482f, -910f, -10996f);
        private static readonly Vector3 MondosMonstrosityPosition = new(-16501f, -910f, -10991f);
        private static readonly Vector3 MondosCratePosition = new(-16500f, -910f, -10998f);
        private static readonly Vector3 RufflesPosition = new(-16866f, -802f, 5570f);
        private static readonly Vector3 GildedFowlPosition = new(-21242f, -807f, -10806f);
        private static readonly Vector3 HutHutPosition = new(-12480f, -788f, -6870f);
        private static readonly Vector3 PrototentiaryDeputyPosition = new(-20745f, -945f, -6885f);
        private static readonly Vector3 PrototentiaryWardenPosition = new(-20718f, -945f, -6923f);

        private static readonly TankRoomSpawnModel[] TankRoomSpawns =
        [
            // Jabbithole PE 299 / objectives 8193/8182 and DataMapping source
            // coordinates 6144867, 6145453, and 6150582 place the three build
            // 16042 TargetGroup 12671 tank-room junk entities for objectives
            // 2676, 2868, 2869, and timed dynamic-max objective 2872.
            new(1100300060u, 62546u, new Vector3(-29077f, -938f, 1552f), 36577u),
            new(1100300061u, 62543u, new Vector3(-29015f, -938f, 1544f), 36579u),
            new(1100300062u, 62542u, new Vector3(-29052f, -938f, 1497f), 36578u)
        ];

        private static readonly PrototentiaryConsoleSpawnModel[] PrototentiaryConsoleSpawns =
        [
            // Build 16042 objective 2847 targets TargetGroup 10569 -> Creature2
            // 62987. Jabbithole objective 8177 and coordinate 6152099 place a
            // matching Gate Console row in the room, bridged by DataMapping to
            // Creature2 62427. Keep this as an explicit alias bridge until
            // client smoke proves the exact target/door choreography.
            new(1100300085u, 62427u, new Vector3(-20819f, -942f, -7005f), 24324u, 1322, "SneakyPrisonGateConsoleEntityScript"),
            // Jabbithole PE 299 / objective 8202 and DataMapping source
            // coordinate 6152147 place the build 16042 TargetGroup 10583 cage
            // console.
            new(1100300081u, 63037u, new Vector3(-20703f, -945f, -6926f), 24321u, 219, "SneakyPrisonCageConsoleEntityScript"),
            // Build 16042 objective 4648 targets TargetGroup 12478 -> Creature2
            // 68916. Jabbithole coordinates 6494141, 6494142, and 6845485 place
            // the reviewed alarm panels in the same Prototentiary room.
            new(1100300082u, 68916u, new Vector3(-20874f, -943f, -6893f), 24321u, 219, "SneakyPrisonAlarmPanelEntityScript"),
            new(1100300083u, 68916u, new Vector3(-20844f, -943f, -6863f), 24321u, 219, "SneakyPrisonAlarmPanelEntityScript"),
            new(1100300084u, 68916u, new Vector3(-20848f, -941f, -6971f), 24321u, 219, "SneakyPrisonAlarmPanelEntityScript")
        ];

        private IPublicEvent publicEvent;
        private IMapInstance mapInstance;
        private bool startButtonSpawned;
        private bool tankRoomSpawned;
        private bool misplacedMammothRoomSpawned;
        private bool rufflesSpawned;
        private bool gildedFowlSpawned;
        private bool hutHutSpawned;
        private bool prototentiarySpawned;

        /// <summary>
        /// Invoked when <see cref="IScript"/> is loaded.
        /// </summary>
        public void OnLoad(IPublicEvent owner)
        {
            publicEvent = owner;
            mapInstance = publicEvent.Map as IMapInstance
                ?? throw new InvalidOperationException("Ultimate Protogames requires a map instance.");

            startButtonSpawned = false;
            tankRoomSpawned = false;
            misplacedMammothRoomSpawned = false;
            rufflesSpawned = false;
            gildedFowlSpawned = false;
            hutHutSpawned = false;
            prototentiarySpawned = false;
            publicEvent.SetPhase(PublicEventPhase.Welcome);
        }

        /// <summary>
        /// Invoked when the public event phase changes.
        /// </summary>
        public void OnPublicEventPhase(uint phase)
        {
            switch ((PublicEventPhase)phase)
            {
                case PublicEventPhase.Welcome:
                    // WIP-guessed from LaughingWS Instances-and-more: the branch exposes
                    // Ultimate Protogames phase/objective ids but no event script. Keep the
                    // coarse entry gate separate from unproven room randomisation and boss routing.
                    publicEvent.ActivateObjective(PublicEventObjective.InitiateUltimateProtogames);
                    SpawnStartButton();
                    break;
                case PublicEventPhase.BevORage:
                    publicEvent.ActivateObjective(PublicEventObjective.DefeatBevORage);
                    SpawnBevORage();
                    publicEvent.ActivateObjective(PublicEventObjective.UseBevORage);
                    // Build 16042 child objectives 3206 and 3210 share
                    // WorldLocation2 45901 with the reviewed Bev-O-Rage
                    // placement. Activate the defeat-backed legs here while
                    // exact timer and 20%-damage semantics remain blocked.
                    publicEvent.ActivateObjective(PublicEventObjective.Caffeinated);
                    publicEvent.ActivateObjective(PublicEventObjective.OutOfOrder);
                    break;
                case PublicEventPhase.TankRoom:
                    OnPhaseTankRoom();
                    break;
                case PublicEventPhase.MisplacedMammoth:
                    OnPhaseMisplacedMammoth();
                    break;
                case PublicEventPhase.Prototentiary:
                    OnPhasePrototentiary();
                    break;
                case PublicEventPhase.Ruffles:
                    OnPhaseRuffles();
                    break;
                case PublicEventPhase.PowerPlunge:
                    OnPhasePowerPlunge();
                    break;
                case PublicEventPhase.HutHut:
                    OnPhaseHutHut();
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
                case PublicEventObjective.InitiateUltimateProtogames:
                    // Random room selection remains blocked, but the reviewed
                    // tank-room slice is now playable as the deterministic
                    // first WIP room instead of stopping at an empty gate.
                    publicEvent.SetPhase(PublicEventPhase.TankRoom);
                    break;
                case PublicEventObjective.GoingGreen:
                    // Full random-event sequencing is still blocked. Chain the
                    // next reviewed producer slice into the WIP route so the
                    // mapped Misplaced Mammoth objective can be exercised.
                    publicEvent.SetPhase(PublicEventPhase.MisplacedMammoth);
                    break;
                case PublicEventObjective.QuickReflexes2:
                    // Keep the deterministic WIP route moving into the reviewed
                    // Prototentiary console slice without claiming retail room
                    // selection or aggregate alarm/timer semantics.
                    publicEvent.SetPhase(PublicEventPhase.Prototentiary);
                    break;
                case PublicEventObjective.Deputy:
                    // Append the reviewed Ruffles hunt as another deterministic
                    // WIP slice after the current Prototentiary leg; exact random
                    // room selection and hunt routing remain blocked.
                    publicEvent.SetPhase(PublicEventPhase.Ruffles);
                    break;
                case PublicEventObjective.HuntRuffles:
                    // Continue to the reviewed Gilded Fowl placement without
                    // claiming the complete Power Plunge route/scoring system.
                    publicEvent.SetPhase(PublicEventPhase.PowerPlunge);
                    break;
                case PublicEventObjective.GildedFowl2:
                    // Append the reviewed Hut-Hut boss placement after the
                    // paired Power Plunge objectives finish. Football scoring,
                    // fumble, shutout, deathless, and timer semantics remain
                    // blocked.
                    publicEvent.SetPhase(PublicEventPhase.HutHut);
                    break;
            }
        }

        private void SpawnStartButton()
        {
            if (startButtonSpawned)
                return;

            // Build 16042 Creature2 65900 is named for the Ultimate Protogames
            // initiation button, and DataMapping spawn row 6128905 places it in
            // world 2980 / zone 4330. Route selection after this gate remains blocked.
            ISimpleEntity startButton = publicEvent.CreateEntity<ISimpleEntity>();
            startButton.Initialise(CreateStartButtonEntityModel());
            AddToMap(startButton, StartButtonPosition);
            startButtonSpawned = true;
        }

        private static EntityModel CreateStartButtonEntityModel()
        {
            return new EntityModel
            {
                Id          = StartButtonEntityId,
                Type        = EntityType.Simple,
                Creature    = StartButtonCreatureId,
                World       = StartButtonWorldId,
                Area        = StartButtonAreaId,
                X           = StartButtonPosition.X,
                Y           = StartButtonPosition.Y,
                Z           = StartButtonPosition.Z,
                DisplayInfo = StartButtonDisplayInfo,
                Faction1    = StartButtonFactionId,
                Faction2    = StartButtonFactionId,
                EntityEvent = new EntityEventModel
                {
                    EventId = StartButtonPublicEventId,
                    Phase   = StartButtonPublicEventPhase
                },
                EntityScript =
                {
                    new EntityScriptModel
                    {
                        ScriptName = StartButtonScriptName
                    }
                }
            };
        }

        private void SpawnBevORage()
        {
            // Build 16042 reviewed instance entity 1100300055 places Bev-O-Rage
            // in event 594 phase 5 with its objective-credit script.
            INonPlayerEntity bevORage = publicEvent.CreateEntity<INonPlayerEntity>();
            bevORage.Initialise(CreateBevORageEntityModel());
            AddToMap(bevORage, BevORagePosition);
        }

        private static EntityModel CreateBevORageEntityModel()
        {
            return new EntityModel
            {
                Id          = BevORageEntityId,
                Type        = EntityType.NonPlayer,
                Creature    = BevORageCreatureId,
                World       = BevORageWorldId,
                Area        = BevORageAreaId,
                X           = BevORagePosition.X,
                Y           = BevORagePosition.Y,
                Z           = BevORagePosition.Z,
                DisplayInfo = BevORageDisplayInfo,
                Faction1    = BevORageFactionId,
                Faction2    = BevORageFactionId,
                EntityEvent = new EntityEventModel
                {
                    EventId = BevORagePublicEventId,
                    Phase   = BevORagePublicEventPhase
                },
                EntityScript =
                {
                    new EntityScriptModel
                    {
                        ScriptName = BevORageScriptName
                    }
                },
                EntityStat =
                {
                    new EntityStatModel
                    {
                        Stat  = (byte)Stat.Level,
                        Value = 50f
                    }
                }
            };
        }

        private void OnPhaseTankRoom()
        {
            publicEvent.ActivateObjective(PublicEventObjective.DestructODerby, TankRoomAllJunkCount);
            publicEvent.ActivateObjective(PublicEventObjective.TankTrample);
            publicEvent.ActivateObjective(PublicEventObjective.CanCrusher);
            publicEvent.ActivateObjective(PublicEventObjective.GoingGreen, TankRoomAllJunkCount);
            SpawnTankRoom();
        }

        private void SpawnTankRoom()
        {
            if (tankRoomSpawned)
                return;

            foreach (TankRoomSpawnModel spawn in TankRoomSpawns)
            {
                INonPlayerEntity tank = publicEvent.CreateEntity<INonPlayerEntity>();
                tank.Initialise(CreateTankRoomEntityModel(spawn));
                AddToMap(tank, spawn.Position);
            }

            tankRoomSpawned = true;
        }

        private static EntityModel CreateTankRoomEntityModel(TankRoomSpawnModel spawn)
        {
            return new EntityModel
            {
                Id          = spawn.EntityId,
                Type        = EntityType.NonPlayer,
                Creature    = spawn.CreatureId,
                World       = TankRoomWorldId,
                Area        = TankRoomAreaId,
                X           = spawn.Position.X,
                Y           = spawn.Position.Y,
                Z           = spawn.Position.Z,
                DisplayInfo = spawn.DisplayInfo,
                Faction1    = TankRoomFactionId,
                Faction2    = TankRoomFactionId,
                EntityEvent = new EntityEventModel
                {
                    EventId = TankRoomPublicEventId,
                    Phase   = TankRoomPublicEventPhase
                },
                EntityScript =
                {
                    new EntityScriptModel
                    {
                        ScriptName = TankRoomScriptName
                    }
                },
                EntityStat =
                {
                    new EntityStatModel
                    {
                        Stat  = (byte)Stat.Health,
                        Value = TankRoomTankHealth
                    },
                    new EntityStatModel
                    {
                        Stat  = (byte)Stat.Level,
                        Value = TankRoomTankLevel
                    },
                    new EntityStatModel
                    {
                        Stat  = (byte)Stat.Shield,
                        Value = TankRoomTankShield
                    }
                }
            };
        }

        private void OnPhaseMisplacedMammoth()
        {
            publicEvent.ActivateObjective(PublicEventObjective.QuickReflexes2);
            publicEvent.ActivateObjective(PublicEventObjective.MonstrosityMassacre);
            publicEvent.ActivateObjective(PublicEventObjective.QuickReflexes);
            publicEvent.ActivateObjective(PublicEventObjective.MondosCrate, 1u);
            SpawnMisplacedMammothRoomContent();
        }

        private void SpawnMisplacedMammothRoomContent()
        {
            if (misplacedMammothRoomSpawned)
                return;

            // Build 16042 objective 4692 targets TargetGroup 12577 -> Creature2
            // 63312. Jabbithole coordinate 8469464 places the unique Misplaced
            // Mammoth spawn in world 2980 / area 4351; timer semantics remain blocked.
            INonPlayerEntity mammoth = publicEvent.CreateEntity<INonPlayerEntity>();
            mammoth.Initialise(CreateMisplacedMammothEntityModel());
            AddToMap(mammoth, MisplacedMammothPosition);

            // Build 16042 objective 2926 shares WorldLocation2 41745 with the
            // Misplaced Mammoth timed room and targets TargetGroup 10657 ->
            // Creature2 62575. Jabbithole public-event creature row 2415 and
            // coordinate 7903034 place a reviewed Mondo's Monstrosity target
            // in the same room; exact timed qualification remains blocked.
            INonPlayerEntity mondo = publicEvent.CreateEntity<INonPlayerEntity>();
            mondo.Initialise(CreateMondosMonstrosityEntityModel());
            AddToMap(mondo, MondosMonstrosityPosition);

            // Build 16042 objective 2920 is a script objective in the same
            // WorldLocation2 41745 timed room. Jabbithole objective 8190 and
            // public-event creature row 2184 link Mondo's Crate row 28277 to
            // Creature2 62549; DataMapping coordinate 6132219 is the nearest
            // reviewed placement to the mammoth/monstrosity cluster.
            INonPlayerEntity crate = publicEvent.CreateEntity<INonPlayerEntity>();
            crate.Initialise(CreateMondosCrateEntityModel());
            AddToMap(crate, MondosCratePosition);

            misplacedMammothRoomSpawned = true;
        }

        private static EntityModel CreateMisplacedMammothEntityModel()
        {
            return new EntityModel
            {
                Id          = MisplacedMammothEntityId,
                Type        = EntityType.NonPlayer,
                Creature    = MisplacedMammothCreatureId,
                World       = MisplacedMammothWorldId,
                Area        = MisplacedMammothAreaId,
                X           = MisplacedMammothPosition.X,
                Y           = MisplacedMammothPosition.Y,
                Z           = MisplacedMammothPosition.Z,
                DisplayInfo = MisplacedMammothDisplayInfo,
                Faction1    = MisplacedMammothFactionId,
                Faction2    = MisplacedMammothFactionId,
                EntityEvent = new EntityEventModel
                {
                    EventId = MisplacedMammothPublicEventId,
                    Phase   = MisplacedMammothPublicEventPhase
                },
                EntityScript =
                {
                    new EntityScriptModel
                    {
                        ScriptName = MisplacedMammothScriptName
                    }
                },
                EntityStat =
                {
                    new EntityStatModel
                    {
                        Stat  = (byte)Stat.Health,
                        Value = MisplacedMammothHealth
                    },
                    new EntityStatModel
                    {
                        Stat  = (byte)Stat.Level,
                        Value = MisplacedMammothLevel
                    },
                    new EntityStatModel
                    {
                        Stat  = (byte)Stat.Shield,
                        Value = MisplacedMammothShield
                    },
                    new EntityStatModel
                    {
                        Stat  = (byte)Stat.InterruptArmour,
                        Value = MisplacedMammothInterruptArmour
                    }
                }
            };
        }

        private static EntityModel CreateMondosMonstrosityEntityModel()
        {
            return new EntityModel
            {
                Id          = MondosMonstrosityEntityId,
                Type        = EntityType.NonPlayer,
                Creature    = MondosMonstrosityCreatureId,
                World       = MisplacedMammothWorldId,
                Area        = MisplacedMammothAreaId,
                X           = MondosMonstrosityPosition.X,
                Y           = MondosMonstrosityPosition.Y,
                Z           = MondosMonstrosityPosition.Z,
                DisplayInfo = MondosMonstrosityDisplayInfo,
                Faction1    = MondosMonstrosityFactionId,
                Faction2    = MondosMonstrosityFactionId,
                EntityEvent = new EntityEventModel
                {
                    EventId = MisplacedMammothPublicEventId,
                    Phase   = MisplacedMammothPublicEventPhase
                },
                EntityScript =
                {
                    new EntityScriptModel
                    {
                        ScriptName = MondosMonstrosityScriptName
                    }
                },
                EntityStat =
                {
                    new EntityStatModel
                    {
                        Stat  = (byte)Stat.Health,
                        Value = MondosMonstrosityHealth
                    },
                    new EntityStatModel
                    {
                        Stat  = (byte)Stat.Level,
                        Value = MondosMonstrosityLevel
                    },
                    new EntityStatModel
                    {
                        Stat  = (byte)Stat.Shield,
                        Value = MondosMonstrosityShield
                    }
                }
            };
        }

        private static EntityModel CreateMondosCrateEntityModel()
        {
            return new EntityModel
            {
                Id          = MondosCrateEntityId,
                Type        = EntityType.NonPlayer,
                Creature    = MondosCrateCreatureId,
                World       = MisplacedMammothWorldId,
                Area        = MisplacedMammothAreaId,
                X           = MondosCratePosition.X,
                Y           = MondosCratePosition.Y,
                Z           = MondosCratePosition.Z,
                DisplayInfo = MondosCrateDisplayInfo,
                Faction1    = MondosCrateFactionId,
                Faction2    = MondosCrateFactionId,
                EntityEvent = new EntityEventModel
                {
                    EventId = MisplacedMammothPublicEventId,
                    Phase   = MisplacedMammothPublicEventPhase
                },
                EntityScript =
                {
                    new EntityScriptModel
                    {
                        ScriptName = MondosCrateScriptName
                    }
                },
                EntityStat =
                {
                    new EntityStatModel
                    {
                        Stat  = (byte)Stat.Health,
                        Value = MondosCrateHealth
                    },
                    new EntityStatModel
                    {
                        Stat  = (byte)Stat.Level,
                        Value = MondosCrateLevel
                    },
                    new EntityStatModel
                    {
                        Stat  = (byte)Stat.Shield,
                        Value = MondosCrateShield
                    },
                    new EntityStatModel
                    {
                        Stat  = (byte)Stat.InterruptArmour,
                        Value = MondosCrateInterruptArmour
                    }
                }
            };
        }

        private void OnPhaseRuffles()
        {
            publicEvent.ActivateObjective(PublicEventObjective.HuntRuffles);
            SpawnRuffles();
        }

        private void SpawnRuffles()
        {
            if (rufflesSpawned)
                return;

            // Build 16042 objective 4561 targets TargetGroup 12390 -> Creature2
            // 65794. Jabbithole public-event creature row 2217 links creature
            // row 28408 to public event 594; coordinate 7930052 is the nearest
            // nonzero row-28408 observation to that room cluster's centroid in
            // world 2980 / area 4348. Exact hunt route/pathing remains blocked.
            INonPlayerEntity ruffles = publicEvent.CreateEntity<INonPlayerEntity>();
            ruffles.Initialise(CreateRufflesEntityModel());
            AddToMap(ruffles, RufflesPosition);

            rufflesSpawned = true;
        }

        private static EntityModel CreateRufflesEntityModel()
        {
            return new EntityModel
            {
                Id          = RufflesEntityId,
                Type        = EntityType.NonPlayer,
                Creature    = RufflesCreatureId,
                World       = RufflesWorldId,
                Area        = RufflesAreaId,
                X           = RufflesPosition.X,
                Y           = RufflesPosition.Y,
                Z           = RufflesPosition.Z,
                DisplayInfo = RufflesDisplayInfo,
                Faction1    = RufflesFactionId,
                Faction2    = RufflesFactionId,
                EntityEvent = new EntityEventModel
                {
                    EventId = RufflesPublicEventId,
                    Phase   = RufflesPublicEventPhase
                },
                EntityScript =
                {
                    new EntityScriptModel
                    {
                        ScriptName = RufflesScriptName
                    }
                },
                EntityStat =
                {
                    new EntityStatModel
                    {
                        Stat  = (byte)Stat.Health,
                        Value = RufflesHealth
                    },
                    new EntityStatModel
                    {
                        Stat  = (byte)Stat.Level,
                        Value = RufflesLevel
                    },
                    new EntityStatModel
                    {
                        Stat  = (byte)Stat.Shield,
                        Value = RufflesShield
                    },
                    new EntityStatModel
                    {
                        Stat  = (byte)Stat.InterruptArmour,
                        Value = RufflesInterruptArmour
                    }
                }
            };
        }

        private void OnPhasePowerPlunge()
        {
            publicEvent.ActivateObjective(PublicEventObjective.GildedFowl);
            publicEvent.ActivateObjective(PublicEventObjective.GildedFowl2);
            SpawnGildedFowl();
        }

        private void SpawnGildedFowl()
        {
            if (gildedFowlSpawned)
                return;

            // Build 16042 objectives 2862 and 4442 target TargetGroup 12263
            // -> Creature2 63055. Jabbithole public-event creature row 2185
            // links creature row 28278 to public event 594; coordinate 8011506
            // is the nearest nonzero row-28278 observation to the Power Plunge
            // cluster centroid in world 2980 / area 4347. Exact plunge scoring
            // and qualification mechanics remain blocked.
            INonPlayerEntity gildedFowl = publicEvent.CreateEntity<INonPlayerEntity>();
            gildedFowl.Initialise(CreateGildedFowlEntityModel());
            AddToMap(gildedFowl, GildedFowlPosition);

            gildedFowlSpawned = true;
        }

        private static EntityModel CreateGildedFowlEntityModel()
        {
            return new EntityModel
            {
                Id          = GildedFowlEntityId,
                Type        = EntityType.NonPlayer,
                Creature    = GildedFowlCreatureId,
                World       = GildedFowlWorldId,
                Area        = GildedFowlAreaId,
                X           = GildedFowlPosition.X,
                Y           = GildedFowlPosition.Y,
                Z           = GildedFowlPosition.Z,
                DisplayInfo = GildedFowlDisplayInfo,
                Faction1    = GildedFowlFactionId,
                Faction2    = GildedFowlFactionId,
                EntityEvent = new EntityEventModel
                {
                    EventId = GildedFowlPublicEventId,
                    Phase   = GildedFowlPublicEventPhase
                },
                EntityScript =
                {
                    new EntityScriptModel
                    {
                        ScriptName = GildedFowlScriptName
                    }
                },
                EntityStat =
                {
                    new EntityStatModel
                    {
                        Stat  = (byte)Stat.Health,
                        Value = GildedFowlHealth
                    },
                    new EntityStatModel
                    {
                        Stat  = (byte)Stat.Level,
                        Value = GildedFowlLevel
                    },
                    new EntityStatModel
                    {
                        Stat  = (byte)Stat.Shield,
                        Value = GildedFowlShield
                    },
                    new EntityStatModel
                    {
                        Stat  = (byte)Stat.InterruptArmour,
                        Value = GildedFowlInterruptArmour
                    }
                }
            };
        }

        private void OnPhaseHutHut()
        {
            publicEvent.ActivateObjective(PublicEventObjective.DefeatHutHut);
            SpawnHutHut();
        }

        private void SpawnHutHut()
        {
            if (hutHutSpawned)
                return;

            // Build 16042 objective 2675 is a direct Script row for Hut-Hut.
            // WildStar client Creature2 61417 is "[UP] e2675 - Hut-Hut -
            // Gorganoth Boss"; Jabbithole rows 28298/30591 and DataMapping
            // source_coordinate_id 8208290 place the reviewed boss in world
            // 2980 / area 4376 near the repeated Hut-Hut coordinate cluster.
            INonPlayerEntity hutHut = publicEvent.CreateEntity<INonPlayerEntity>();
            hutHut.Initialise(CreateHutHutEntityModel());
            AddToMap(hutHut, HutHutPosition);

            hutHutSpawned = true;
        }

        private static EntityModel CreateHutHutEntityModel()
        {
            return new EntityModel
            {
                Id          = HutHutEntityId,
                Type        = EntityType.NonPlayer,
                Creature    = HutHutCreatureId,
                World       = HutHutWorldId,
                Area        = HutHutAreaId,
                X           = HutHutPosition.X,
                Y           = HutHutPosition.Y,
                Z           = HutHutPosition.Z,
                DisplayInfo = HutHutDisplayInfo,
                Faction1    = HutHutFactionId,
                Faction2    = HutHutFactionId,
                EntityEvent = new EntityEventModel
                {
                    EventId = HutHutPublicEventId,
                    Phase   = HutHutPublicEventPhase
                },
                EntityScript =
                {
                    new EntityScriptModel
                    {
                        ScriptName = HutHutScriptName
                    }
                },
                EntityStat =
                {
                    new EntityStatModel
                    {
                        Stat  = (byte)Stat.Health,
                        Value = HutHutHealth
                    },
                    new EntityStatModel
                    {
                        Stat  = (byte)Stat.Level,
                        Value = HutHutLevel
                    },
                    new EntityStatModel
                    {
                        Stat  = (byte)Stat.Shield,
                        Value = HutHutShield
                    }
                }
            };
        }

        private void OnPhasePrototentiary()
        {
            // Build 16042 objective 2678 is the Prototentiary aggregate row and
            // combines the reviewed Warden death and TargetGroup 10583 Cage
            // Console legs. Stealth/no-alarm semantics remain blocked.
            publicEvent.ActivateObjective(PublicEventObjective.SneakThroughThePrototentiary, PrototentiaryAggregateObjectiveCount);
            publicEvent.ActivateObjective(PublicEventObjective.HackThecreature62987, PrototentiaryConsoleObjectiveCount);
            publicEvent.ActivateObjective(PublicEventObjective.HackThecreature63037, PrototentiaryConsoleObjectiveCount);
            publicEvent.ActivateObjective(PublicEventObjective.Deputy);
            // Build 16042 objective 2863 (Fast Hands) shares TargetGroup 10583
            // with the cage console and carries failureTimeMs 210000. Activate
            // only this timed bonus here; Ghosts/no-alarm semantics remain blocked.
            publicEvent.ActivateObjective(PublicEventObjective.FastHands, PrototentiaryConsoleObjectiveCount);
            publicEvent.ActivateObjective(PublicEventObjective.DisableTheAlarm, PrototentiaryConsoleObjectiveCount);
            SpawnPrototentiaryContent();
        }

        private void SpawnPrototentiaryContent()
        {
            if (prototentiarySpawned)
                return;

            foreach (PrototentiaryConsoleSpawnModel spawn in PrototentiaryConsoleSpawns)
            {
                ISimpleEntity console = publicEvent.CreateEntity<ISimpleEntity>();
                console.Initialise(CreatePrototentiaryConsoleEntityModel(spawn));
                AddToMap(console, spawn.Position);
            }

            // Build 16042 objective 4657 targets TargetGroup 12528 -> Creature2
            // 68949 Deputy. DataMapping source coordinate 8024837 places a
            // unique-name Deputy in world 2980 / area 4333; exact room density
            // and patrol/choreography remain blocked.
            INonPlayerEntity deputy = publicEvent.CreateEntity<INonPlayerEntity>();
            deputy.Initialise(CreatePrototentiaryDeputyEntityModel());
            AddToMap(deputy, PrototentiaryDeputyPosition);

            // Build 16042 TargetGroup 12474 contains the Warden Creature2 row
            // 62324. Jabbithole PE 299 creature row 28413 and coordinate
            // 7099765 place the reviewed Warden in world 2980 / area 4333.
            INonPlayerEntity warden = publicEvent.CreateEntity<INonPlayerEntity>();
            warden.Initialise(CreatePrototentiaryWardenEntityModel());
            AddToMap(warden, PrototentiaryWardenPosition);

            prototentiarySpawned = true;
        }

        private static EntityModel CreatePrototentiaryConsoleEntityModel(PrototentiaryConsoleSpawnModel spawn)
        {
            return new EntityModel
            {
                Id          = spawn.EntityId,
                Type        = EntityType.Simple,
                Creature    = spawn.CreatureId,
                World       = PrototentiaryWorldId,
                Area        = PrototentiaryAreaId,
                X           = spawn.Position.X,
                Y           = spawn.Position.Y,
                Z           = spawn.Position.Z,
                DisplayInfo = spawn.DisplayInfo,
                Faction1    = spawn.FactionId,
                Faction2    = spawn.FactionId,
                EntityEvent = new EntityEventModel
                {
                    EventId = PrototentiaryPublicEventId,
                    Phase   = PrototentiaryPublicEventPhase
                },
                EntityScript =
                {
                    new EntityScriptModel
                    {
                        ScriptName = spawn.ScriptName
                    }
                }
            };
        }

        private static EntityModel CreatePrototentiaryDeputyEntityModel()
        {
            return new EntityModel
            {
                Id          = PrototentiaryDeputyEntityId,
                Type        = EntityType.NonPlayer,
                Creature    = PrototentiaryDeputyCreatureId,
                World       = PrototentiaryWorldId,
                Area        = PrototentiaryAreaId,
                X           = PrototentiaryDeputyPosition.X,
                Y           = PrototentiaryDeputyPosition.Y,
                Z           = PrototentiaryDeputyPosition.Z,
                DisplayInfo = PrototentiaryDeputyDisplayInfo,
                Faction1    = PrototentiaryDeputyFactionId,
                Faction2    = PrototentiaryDeputyFactionId,
                EntityEvent = new EntityEventModel
                {
                    EventId = PrototentiaryPublicEventId,
                    Phase   = PrototentiaryPublicEventPhase
                },
                EntityScript =
                {
                    new EntityScriptModel
                    {
                        ScriptName = PrototentiaryDeputyScriptName
                    }
                },
                EntityStat =
                {
                    new EntityStatModel
                    {
                        Stat  = (byte)Stat.Level,
                        Value = PrototentiaryDeputyLevel
                    },
                    new EntityStatModel
                    {
                        Stat  = (byte)Stat.InterruptArmour,
                        Value = PrototentiaryDeputyInterruptArmour
                    }
                }
            };
        }

        private static EntityModel CreatePrototentiaryWardenEntityModel()
        {
            return new EntityModel
            {
                Id          = PrototentiaryWardenEntityId,
                Type        = EntityType.NonPlayer,
                Creature    = PrototentiaryWardenCreatureId,
                World       = PrototentiaryWorldId,
                Area        = PrototentiaryAreaId,
                X           = PrototentiaryWardenPosition.X,
                Y           = PrototentiaryWardenPosition.Y,
                Z           = PrototentiaryWardenPosition.Z,
                DisplayInfo = PrototentiaryWardenDisplayInfo,
                Faction1    = PrototentiaryWardenFactionId,
                Faction2    = PrototentiaryWardenFactionId,
                EntityEvent = new EntityEventModel
                {
                    EventId = PrototentiaryPublicEventId,
                    Phase   = PrototentiaryPublicEventPhase
                },
                EntityScript =
                {
                    new EntityScriptModel
                    {
                        ScriptName = PrototentiaryWardenScriptName
                    }
                },
                EntityStat =
                {
                    new EntityStatModel
                    {
                        Stat  = (byte)Stat.Health,
                        Value = PrototentiaryWardenHealth
                    },
                    new EntityStatModel
                    {
                        Stat  = (byte)Stat.Level,
                        Value = PrototentiaryWardenLevel
                    },
                    new EntityStatModel
                    {
                        Stat  = (byte)Stat.Shield,
                        Value = PrototentiaryWardenShield
                    },
                    new EntityStatModel
                    {
                        Stat  = (byte)Stat.InterruptArmour,
                        Value = PrototentiaryWardenInterruptArmour
                    }
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

        private sealed record TankRoomSpawnModel(
            uint EntityId,
            uint CreatureId,
            Vector3 Position,
            uint DisplayInfo);

        private sealed record PrototentiaryConsoleSpawnModel(
            uint EntityId,
            uint CreatureId,
            Vector3 Position,
            uint DisplayInfo,
            ushort FactionId,
            string ScriptName);
    }
}
