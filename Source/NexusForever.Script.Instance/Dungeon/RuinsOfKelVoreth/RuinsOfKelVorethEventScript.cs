using System.Numerics;
using NexusForever.Database.World.Model;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Map.Instance;
using NexusForever.Game.Abstract.PublicEvent;
using NexusForever.Game.Static.Entity;
using NexusForever.Game.Static.PublicEvent;
using NexusForever.Script.Template;
using NexusForever.Script.Template.Filter;

namespace NexusForever.Script.Instance.Dungeon.RuinsOfKelVoreth
{
    [ScriptFilterOwnerId(161)]
    public class RuinsOfKelVorethEventScript : IPublicEventScript, IOwnedScript<IPublicEvent>
    {
        private static readonly RuinsOfKelVorethSpawnModel GrondTheCorpsemakerSpawn = new(
            1100300045u,
            32534u,
            1u,
            new Vector3(195.41f, -899.36f, 225.65f),
            27715u,
            "GrondTheCorpsemakerEntityScript");

        private static readonly RuinsOfKelVorethSpawnModel SlavemasterDrokkSpawn = new(
            1100300046u,
            32536u,
            2u,
            new Vector3(596.36f, -882.84f, 976.53f),
            27104u,
            "SlavemasterDrokkEntityScript");

        private static readonly RuinsOfKelVorethSpawnModel ForgemasterTrogunSpawn = new(
            1100300044u,
            32531u,
            3u,
            new Vector3(-24.77f, -737.08f, 990.32f),
            29203u,
            "ForgemasterTrogunEntityScript");

        private IPublicEvent publicEvent;
        private IMapInstance mapInstance;
        private bool grondTheCorpsemakerSpawned;
        private bool slavemasterDrokkSpawned;
        private bool forgemasterTrogunSpawned;
        private bool playerDied;
        private double elapsedSeconds;

        /// <summary>
        /// Seconds allowed for the "Timer Bonus" row (3202, "Complete the Ruins of Kel Voreth within 40 minutes for bonus").
        /// The objective row carries no failure time, so the limit is tracked here.
        /// </summary>
        private const double TimerBonusSeconds = 40d * 60d;

        /// <summary>
        /// Invoked when <see cref="IScript"/> is loaded.
        /// </summary>
        public void OnLoad(IPublicEvent owner)
        {
            publicEvent = owner;
            mapInstance = publicEvent.Map as IMapInstance
                ?? throw new InvalidOperationException("Ruins of Kel Voreth requires a map instance.");

            grondTheCorpsemakerSpawned = false;
            slavemasterDrokkSpawned = false;
            forgemasterTrogunSpawned = false;
            playerDied = false;
            elapsedSeconds = 0d;
            publicEvent.SetPhase(PublicEventPhase.FightInBloodPit);
        }

        /// <summary>
        /// Invoked each world tick; tracks the elapsed run time for the Timer Bonus row.
        /// </summary>
        public void Update(double lastTick)
        {
            elapsedSeconds += lastTick;
        }

        /// <summary>
        /// Invoked when a unit dies on the map; any player death fails "Deathless in the Dungeon".
        /// </summary>
        public void OnDeath(IUnitEntity entity)
        {
            if (entity is IPlayer)
                playerDied = true;
        }

        /// <summary>
        /// Invoked when the public event phase changes.
        /// </summary>
        public void OnPublicEventPhase(uint phase)
        {
            // Tracker rows below were read from retail-era clips (see INSTANCE_KELVORETH_TRACKER_PASS_20261109.md):
            // Observed ordering only, no retail parity claim. The random optional pool is provisional.
            switch ((PublicEventPhase)phase)
            {
                case PublicEventPhase.FightInBloodPit:
                    publicEvent.ActivateObjective(PublicEventObjective.FightYourWayThroughTheBloodPit, 3u);
                    // "Deathless in the Dungeon" and "Timer Bonus" are listed from the first phase onward in
                    // both the 2015 normal run and the 2015 veteran guide.
                    publicEvent.ActivateObjective(PublicEventObjective.FinishTheEntireDungeonWithoutAnyoneDying);
                    publicEvent.ActivateObjective(PublicEventObjective.CompleteTheRuinsOfKelVorethWithin40Minutes);
                    break;
                case PublicEventPhase.GrondTheCorpsemaker:
                    publicEvent.ActivateObjective(PublicEventObjective.DefeatGrondTheCorpsemaker);
                    SpawnGrondTheCorpsemaker();
                    break;
                case PublicEventPhase.SlaveMasterDrokk:
                    publicEvent.ActivateObjective(PublicEventObjective.DefeatSlavemasterDrokk);
                    SpawnSlavemasterDrokk();
                    // "Dodging the Defense" is listed in every read of this section; the optional rows vary per run
                    // (one to three of them seen), so they stay a random pool.
                    publicEvent.ActivateObjective(PublicEventObjective.DodgingTheDefense);
                    ActivateWipOptionalObjective(PublicEventObjective.KillMechanoSlaversAndEldanConstructs);
                    ActivateWipOptionalObjective(PublicEventObjective.PutTheKelVorethSlavesOutOfTheirMisery);
                    ActivateWipOptionalObjective(PublicEventObjective.DefeatDarkwitchGurka);
                    ActivateWipOptionalObjective(PublicEventObjective.AccessTheHiddenEldanDataStorageDevices);
                    break;
                case PublicEventPhase.ForgeMasterTrogun:
                    publicEvent.ActivateObjective(PublicEventObjective.DefeatForgemasterTrogun);
                    SpawnForgemasterTrogun();
                    // "Kill Battlesworn and Darkwitch Osun" was listed in every read of this section.
                    publicEvent.ActivateObjective(PublicEventObjective.KillBattleswornAndDarkwitchOsun);
                    ActivateWipOptionalObjective(PublicEventObjective.DestroyKelVorethForges);
                    ActivateWipOptionalObjective(PublicEventObjective.BurnKelVorethWarSupplies);
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
                case PublicEventObjective.FightYourWayThroughTheBloodPit:
                    publicEvent.SetPhase(PublicEventPhase.GrondTheCorpsemaker);
                    break;
                case PublicEventObjective.DefeatGrondTheCorpsemaker:
                    publicEvent.SetPhase(PublicEventPhase.SlaveMasterDrokk);
                    break;
                case PublicEventObjective.DefeatSlavemasterDrokk:
                    publicEvent.SetPhase(PublicEventPhase.ForgeMasterTrogun);
                    break;
                case PublicEventObjective.DefeatForgemasterTrogun:
                    // The engine has no script-side way to fail an objective, so the challenge rows are credited
                    // only when their condition held; a failed row simply never completes.
                    if (!playerDied)
                        publicEvent.UpdateObjective(PublicEventObjective.FinishTheEntireDungeonWithoutAnyoneDying, 1);

                    if (elapsedSeconds <= TimerBonusSeconds)
                        publicEvent.UpdateObjective(PublicEventObjective.CompleteTheRuinsOfKelVorethWithin40Minutes, 1);

                    publicEvent.Finish(PublicEventTeam.PublicTeam);
                    break;
            }
        }

        private void SpawnGrondTheCorpsemaker()
        {
            if (grondTheCorpsemakerSpawned)
                return;

            SpawnReviewedBoss(GrondTheCorpsemakerSpawn);
            grondTheCorpsemakerSpawned = true;
        }

        private void SpawnSlavemasterDrokk()
        {
            if (slavemasterDrokkSpawned)
                return;

            SpawnReviewedBoss(SlavemasterDrokkSpawn);
            slavemasterDrokkSpawned = true;
        }

        private void SpawnForgemasterTrogun()
        {
            if (forgemasterTrogunSpawned)
                return;

            SpawnReviewedBoss(ForgemasterTrogunSpawn);
            forgemasterTrogunSpawned = true;
        }

        private void SpawnReviewedBoss(RuinsOfKelVorethSpawnModel spawn)
        {
            // Build 16042 reviewed instance rows place the Kel Voreth boss NPCs
            // into event 161 phases 1-3 with objective-credit scripts and level
            // overrides. Exact combat mechanics and door choreography remain blocked.
            INonPlayerEntity entity = publicEvent.CreateEntity<INonPlayerEntity>();
            entity.Initialise(CreateEntityModel(spawn));
            AddToMap(entity, spawn.Position);
        }

        private static EntityModel CreateEntityModel(RuinsOfKelVorethSpawnModel spawn)
        {
            return new EntityModel
            {
                Id          = spawn.EntityId,
                Type        = EntityType.NonPlayer,
                Creature    = spawn.CreatureId,
                World       = 1336,
                Area        = 0,
                X           = spawn.Position.X,
                Y           = spawn.Position.Y,
                Z           = spawn.Position.Z,
                DisplayInfo = spawn.DisplayInfo,
                Faction1    = 691,
                Faction2    = 691,
                EntityEvent = new EntityEventModel
                {
                    EventId = 161u,
                    Phase   = spawn.PublicEventPhase
                },
                EntityScript =
                {
                    new EntityScriptModel
                    {
                        ScriptName = spawn.ScriptName
                    }
                },
                EntityStat =
                {
                    new EntityStatModel
                    {
                        Stat  = (byte)Stat.Level,
                        Value = 25f
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

        private void ActivateWipOptionalObjective(PublicEventObjective objective)
        {
            // Provisional random pool. Retail-era tracker reads show the optional rows differ from run to run
            // (Drokk section: 448/450/453 in one run, only 456 in another, 448/453/456 in two more), so a random
            // choice is kept. The selection distribution, per-section counts and trigger placement are NOT known.
            if (ShouldActivateWipOptionalObjective(objective))
                publicEvent.ActivateObjective(objective);
        }

        protected virtual bool ShouldActivateWipOptionalObjective(PublicEventObjective objective)
        {
            return Random.Shared.Next(2) == 1;
        }

        private sealed record RuinsOfKelVorethSpawnModel(
            uint EntityId,
            uint CreatureId,
            uint PublicEventPhase,
            Vector3 Position,
            uint DisplayInfo,
            string ScriptName);
    }
}
