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
            publicEvent.SetPhase(PublicEventPhase.FightInBloodPit);
        }

        /// <summary>
        /// Invoked when the public event phase changes.
        /// </summary>
        public void OnPublicEventPhase(uint phase)
        {
            switch ((PublicEventPhase)phase)
            {
                case PublicEventPhase.FightInBloodPit:
                    publicEvent.ActivateObjective(PublicEventObjective.FightYourWayThroughTheBloodPit, 3u);
                    break;
                case PublicEventPhase.GrondTheCorpsemaker:
                    publicEvent.ActivateObjective(PublicEventObjective.DefeatGrondTheCorpsemaker);
                    SpawnGrondTheCorpsemaker();
                    break;
                case PublicEventPhase.SlaveMasterDrokk:
                    publicEvent.ActivateObjective(PublicEventObjective.DefeatSlavemasterDrokk);
                    SpawnSlavemasterDrokk();
                    publicEvent.ActivateObjective(PublicEventObjective.DefeatDarkwitchGurka);
                    publicEvent.ActivateObjective(PublicEventObjective.DodgingTheDefense);
                    ActivateWipOptionalObjective(PublicEventObjective.KillMechanoSlaversAndEldanConstructs);
                    ActivateWipOptionalObjective(PublicEventObjective.PutTheKelVorethSlavesOutOfTheirMisery);
                    ActivateWipOptionalObjective(PublicEventObjective.AccessTheHiddenEldanDataStorageDevices);
                    ActivateWipOptionalObjective(PublicEventObjective.DisableTheExoLabDefenses);
                    break;
                case PublicEventPhase.ForgeMasterTrogun:
                    publicEvent.ActivateObjective(PublicEventObjective.DefeatForgemasterTrogun);
                    SpawnForgemasterTrogun();
                    ActivateWipOptionalObjective(PublicEventObjective.DestroyKelVorethForges);
                    ActivateWipOptionalObjective(PublicEventObjective.KillBattleswornAndDarkwitchOsun);
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
            // WIP-guessed from LaughingWS Instances-and-more. The branch randomly
            // activates these Ruins optional objectives, but exact route weights,
            // trigger placement, door timing, and objective availability remain blocked.
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
