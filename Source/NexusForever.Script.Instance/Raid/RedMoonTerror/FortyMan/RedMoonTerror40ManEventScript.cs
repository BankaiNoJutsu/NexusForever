using System.Numerics;
using NexusForever.Database.World.Model;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Map.Instance;
using NexusForever.Game.Abstract.PublicEvent;
using NexusForever.Game.Static.Entity;
using NexusForever.Game.Static.PublicEvent;
using NexusForever.Script.Template;
using NexusForever.Script.Template.Filter;

namespace NexusForever.Script.Instance.Raid.RedMoonTerror.FortyMan
{
    [ScriptFilterOwnerId(650)]
    public class RedMoonTerror40ManEventScript : IPublicEventScript, IOwnedScript<IPublicEvent>
    {
        private IPublicEvent publicEvent;
        private IMapInstance mapInstance;
        private bool lavekaSpawned;

        private const ushort RedMoonTerror40ManWorldId = 3102;
        private const string LavekaScriptName = "RedMoonTerror40ManLavekaEntityScript";

        private static readonly RedMoonTerror40ManSpawnModel LavekaSpawn = new(
            1100300076u,
            65997u,
            5996,
            new Vector3(-723.7178f, 186.8427f, -265.1872f),
            new Vector3(MathF.PI, 0f, 0f),
            38426u,
            1351,
            LavekaScriptName,
            1f,
            50f);

        /// <summary>
        /// Invoked when <see cref="IScript"/> is loaded.
        /// </summary>
        public void OnLoad(IPublicEvent owner)
        {
            publicEvent = owner;
            mapInstance = publicEvent.Map as IMapInstance
                ?? throw new InvalidOperationException("Red Moon Terror 40-man requires a map instance.");

            lavekaSpawned = false;
            publicEvent.SetPhase(PublicEventPhase.DefeatLaveka);
        }

        /// <summary>
        /// Invoked when the public event phase changes.
        /// </summary>
        public void OnPublicEventPhase(uint phase)
        {
            switch ((PublicEventPhase)phase)
            {
                case PublicEventPhase.DefeatLaveka:
                    publicEvent.ActivateObjective(PublicEventObjective.DefeatLavekaTheDarkHearted);
                    SpawnLaveka();
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
                case PublicEventObjective.DefeatLavekaTheDarkHearted:
                    publicEvent.Finish(PublicEventTeam.PublicTeam);
                    break;
            }
        }

        private void SpawnLaveka()
        {
            if (lavekaSpawned)
                return;

            // PublicEvent 650 exposes only the Laveka objective plus hidden
            // medbay/morgue turnstile rows. Reuse the reviewed Red Moon Terror
            // Laveka placement/model as a WIP boss anchor for the 40-man client
            // row; hidden turnstile placement and route semantics remain blocked.
            INonPlayerEntity entity = publicEvent.CreateEntity<INonPlayerEntity>();
            entity.Initialise(CreateEntityModel(LavekaSpawn));
            AddToMap(entity, LavekaSpawn.Position);
            lavekaSpawned = true;
        }

        private static EntityModel CreateEntityModel(RedMoonTerror40ManSpawnModel spawn)
        {
            return new EntityModel
            {
                Id          = spawn.EntityId,
                Type        = EntityType.NonPlayer,
                Creature    = spawn.CreatureId,
                World       = RedMoonTerror40ManWorldId,
                Area        = spawn.AreaId,
                X           = spawn.Position.X,
                Y           = spawn.Position.Y,
                Z           = spawn.Position.Z,
                Rx          = spawn.Rotation.X,
                Ry          = spawn.Rotation.Y,
                Rz          = spawn.Rotation.Z,
                DisplayInfo = spawn.DisplayInfo,
                Faction1    = spawn.FactionId,
                Faction2    = spawn.FactionId,
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
                        Stat  = (byte)Stat.Health,
                        Value = spawn.Health
                    },
                    new EntityStatModel
                    {
                        Stat  = (byte)Stat.Level,
                        Value = spawn.Level
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

        private sealed record RedMoonTerror40ManSpawnModel(
            uint EntityId,
            uint CreatureId,
            ushort AreaId,
            Vector3 Position,
            Vector3 Rotation,
            uint DisplayInfo,
            ushort FactionId,
            string ScriptName,
            float Health,
            float Level);
    }
}
