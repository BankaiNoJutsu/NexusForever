using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Numerics;
using Microsoft.Extensions.Logging;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Map;
using NexusForever.Game.Static.Entity;
using NexusForever.GameTable;
using NexusForever.GameTable.Model;
using NexusForever.Script.Template;
using NexusForever.Script.Template.Filter;

namespace NexusForever.Script.Main.Transport
{
    [ScriptFilterCreatureId(
        70783u, 70382u, 67491u, 67492u, 46182u, 51202u, 62464u, 67494u, 47505u,
        70782u, 70383u, 67480u, 70385u, 67487u, 46183u, 51207u, 47459u,
        70678u, 70681u,
        27196u, 59189u, 46980u, 59199u, 70781u, 46184u, 51204u, 62465u, 45102u, 70663u, 46185u,
        32269u, 45366u, 70174u, 70172u, 30352u, 70173u,
        28454u, 25886u)]
    public class WorldLocationTeleporterEntityScript : IWorldEntityScript, IOwnedScript<ICreatureEntity>
    {
        public readonly record struct DirectTransportDestination(ushort WorldId, Vector3 Position, Vector3 Rotation);

        private const uint NorthernWildsWorldId = 426u;
        private const uint Q3963ShipControlsCreatureId = 27196u;

        private static readonly IReadOnlyDictionary<uint, uint> creatureWorldLocationIds = new ReadOnlyDictionary<uint, uint>(
            new Dictionary<uint, uint>
            {
                // Illium city transporters.
                [70783u] = 50181u, // Ivory Hall, Arcterra
                [70382u] = 49902u, // Nursery Trading Post, Blighthaven
                [67491u] = 43968u, // Crimson Command, Crimson Badlands
                [67492u] = 36743u, // Virtue's Landing, Farside
                [46182u] = 37776u, // Graylight, Halon Ring
                [51202u] = 40705u, // Shinysand Oasis, Malgrave
                [62464u] = 46311u, // Museum of Illium
                [67494u] = 32543u, // Palerock Post, Whitevale
                [47505u] = 21660u, // Fort Vigilance, Wilderrun

                // Thayd city transporters.
                [70782u] = 50182u, // The Icebox, Arcterra
                [70383u] = 49901u, // Nursery Trading Post, Blighthaven
                [67480u] = 43971u, // Bloodstone Landing, Crimson Badlands
                [70385u] = 49900u, // Hope's Dare, Defile
                [67487u] = 36742u, // Walker's Landing, Farside
                [46183u] = 37777u, // Graylight, Halon Ring
                [51207u] = 40707u, // Shinysand Oasis, Malgrave
                [47459u] = 21659u, // Fool's Hope, Wilderrun

                // Shared city and world transporters.
                [70678u] = 46631u, // Star-Comm Basin
                [70681u] = 46631u, // Star-Comm Basin
                [27196u] = 9801u,  // Tremor Ridge, Algoroc
                [59189u] = 19279u, // Datascape
                [46980u] = 20541u, // Lightreach Mission, Ellevar
                [59199u] = 24083u, // Corrupted Archives, Genetic Archives
                [70781u] = 50187u, // Illium, Arcterra
                [46184u] = 37775u, // Illium, Halon Ring
                [51204u] = 40704u, // Illium, Malgrave
                [62465u] = 46314u, // Illium Museum
                [45102u] = 1594u,  // Landing Site, Northern Wilds
                [70663u] = 1594u,  // Landing Site, Northern Wilds
                [46185u] = 37772u  // Thayd, Halon Ring
            });

        private static readonly IReadOnlyDictionary<uint, IReadOnlySet<uint>> creatureWorldLocationBlockedSourceWorlds = new ReadOnlyDictionary<uint, IReadOnlySet<uint>>(
            new Dictionary<uint, IReadOnlySet<uint>>
            {
                // Creature2 27196 is also Q3963 Ship Controls in Northern Wilds; those controls are quest targets, not the Algoroc teleporter.
                [Q3963ShipControlsCreatureId] = new HashSet<uint> { NorthernWildsWorldId }
            });

        private static readonly IReadOnlyDictionary<uint, DirectTransportDestination> directCreatureDestinations = new ReadOnlyDictionary<uint, DirectTransportDestination>(
            new Dictionary<uint, DirectTransportDestination>
            {
                // Branch coordinate routes with official-worlddb paired portal/transport placement evidence.
                [32269u] = new DirectTransportDestination(51, new Vector3(881.554f, -909.42f, -3130.06f), Vector3.Zero), // Everstar Grove -> Celestion
                [45366u] = new DirectTransportDestination(870, new Vector3(-8267.476f, -995.66176f, -239.02145f), new Vector3(-1.8304919f, 0f, 0f)), // Deradune -> Crimson Isle
                [70174u] = new DirectTransportDestination(870, new Vector3(-8267.476f, -995.66176f, -239.02145f), new Vector3(-1.8304919f, 0f, 0f)), // Levian Bay -> Crimson Isle
                [70172u] = new DirectTransportDestination(990, new Vector3(-771.823f, -904.285f, -2269.56f), new Vector3(-1.1214001f, 0f, 0f)), // Northern Wilds -> Everstar Grove
                [30352u] = new DirectTransportDestination(1387, new Vector3(-3835.34f, -980.217f, -6050.52f), new Vector3(-0.45682f, 0f, 0f)), // Ellevar -> Levian Bay
                [70173u] = new DirectTransportDestination(1387, new Vector3(-3835.34f, -980.217f, -6050.52f), new Vector3(-0.45682f, 0f, 0f)) // Crimson Isle -> Levian Bay
            });

        private static readonly IReadOnlyDictionary<uint, uint> rangeCreatureWorldLocationIds = new ReadOnlyDictionary<uint, uint>(
            new Dictionary<uint, uint>
            {
                [28454u] = 21760u, // Exo-Lab 71 entrance
                [25886u] = 19282u  // Exo-Lab 22 entrance
            });

        private readonly IGameTableManager gameTableManager;
        private readonly ILogger<WorldLocationTeleporterEntityScript> log;

        private ICreatureEntity owner;

        public static IReadOnlyDictionary<uint, uint> CreatureWorldLocationIds => creatureWorldLocationIds;
        public static IReadOnlyDictionary<uint, DirectTransportDestination> DirectCreatureDestinations => directCreatureDestinations;
        public static IReadOnlyDictionary<uint, uint> RangeCreatureWorldLocationIds => rangeCreatureWorldLocationIds;

        public WorldLocationTeleporterEntityScript(
            ILogger<WorldLocationTeleporterEntityScript> log,
            IGameTableManager gameTableManager)
        {
            this.log = log;
            this.gameTableManager = gameTableManager;
        }

        public void OnLoad(ICreatureEntity owner)
        {
            this.owner = owner;
            log.LogDebug("World-location teleporter script loaded for entity {EntityGuid}: creature={CreatureId}.",
                owner.Guid,
                owner.CreatureId);
        }

        public void OnAddToMap(IBaseMap map)
        {
            if (owner != null && rangeCreatureWorldLocationIds.ContainsKey(owner.CreatureId))
                owner.SetInRangeCheck(2f);
        }

        public void OnEnterRange(IGridEntity entity)
        {
            if (entity is not IPlayer player || owner == null)
                return;

            if (!rangeCreatureWorldLocationIds.TryGetValue(owner.CreatureId, out uint worldLocationId))
                return;

            if (owner.Position.Y - 1f > player.Position.Y)
            {
                log.LogDebug("Range world-location teleporter {EntityGuid} skipped player {PlayerGuid}: player is below the trigger plane.",
                    owner.Guid,
                    player.Guid);
                return;
            }

            TryTeleportWorldLocation(player, worldLocationId);
        }

        public void OnActivateSuccess(IPlayer activator)
        {
            if (activator == null || owner == null)
                return;

            if (TryGetCreatureWorldLocation(owner.CreatureId, out uint worldLocationId))
            {
                TryTeleportWorldLocation(activator, worldLocationId);
                return;
            }

            if (directCreatureDestinations.TryGetValue(owner.CreatureId, out DirectTransportDestination destination))
            {
                TryTeleportDirect(activator, destination);
                return;
            }

            log.LogWarning("World-location teleporter {EntityGuid} has no destination mapping for creature {CreatureId}.",
                owner.Guid,
                owner.CreatureId);
        }

        private bool TryGetCreatureWorldLocation(uint creatureId, out uint worldLocationId)
        {
            worldLocationId = 0u;
            if (!creatureWorldLocationIds.TryGetValue(creatureId, out uint mappedWorldLocationId))
                return false;

            uint? sourceWorldId = owner?.Map?.Entry?.Id;
            if (sourceWorldId.HasValue
                && creatureWorldLocationBlockedSourceWorlds.TryGetValue(creatureId, out IReadOnlySet<uint> blockedWorlds)
                && blockedWorlds.Contains(sourceWorldId.Value))
            {
                log.LogDebug("World-location teleporter {EntityGuid} skipped creature {CreatureId} on source world {WorldId}: creature row is quest-owned on this world.",
                    owner.Guid,
                    creatureId,
                    sourceWorldId.Value);
                return false;
            }

            worldLocationId = mappedWorldLocationId;
            return true;
        }

        private void TryTeleportWorldLocation(IPlayer activator, uint worldLocationId)
        {
            WorldLocation2Entry destination = gameTableManager.WorldLocation2.GetEntry(worldLocationId);
            if (destination == null)
            {
                log.LogWarning("World-location teleporter {EntityGuid} missing destination world location {WorldLocationId} for creature {CreatureId}.",
                    owner.Guid,
                    worldLocationId,
                    owner.CreatureId);
                return;
            }

            if (!activator.CanTeleport())
            {
                log.LogDebug("World-location teleporter {EntityGuid} skipped teleport for player {PlayerGuid}: teleport currently unavailable.",
                    owner.Guid,
                    activator.Guid);
                return;
            }

            activator.Rotation = ToEulerDegrees(new Quaternion(destination.Facing0, destination.Facing1, destination.Facing2, destination.Facing3));
            activator.TeleportTo((ushort)destination.WorldId, destination.Position0, destination.Position1, destination.Position2);

            log.LogDebug("World-location teleporter {EntityGuid} transported player {PlayerGuid} to world location {WorldLocationId}: world={WorldId}, position=({X}, {Y}, {Z}).",
                owner.Guid,
                activator.Guid,
                worldLocationId,
                destination.WorldId,
                destination.Position0,
                destination.Position1,
                destination.Position2);
        }

        private void TryTeleportDirect(IPlayer activator, DirectTransportDestination destination)
        {
            WorldEntry world = gameTableManager.World.GetEntry(destination.WorldId);
            if (world == null)
            {
                log.LogWarning("Direct teleporter {EntityGuid} missing destination world {WorldId} for creature {CreatureId}.",
                    owner.Guid,
                    destination.WorldId,
                    owner.CreatureId);
                return;
            }

            if (!activator.CanTeleport())
            {
                log.LogDebug("Direct teleporter {EntityGuid} skipped teleport for player {PlayerGuid}: teleport currently unavailable.",
                    owner.Guid,
                    activator.Guid);
                return;
            }

            activator.Rotation = destination.Rotation;
            activator.TeleportTo(
                (ushort)world.Id,
                destination.Position.X,
                destination.Position.Y,
                destination.Position.Z,
                reason: TeleportReason.Relocate);

            log.LogDebug("Direct teleporter {EntityGuid} transported player {PlayerGuid} to world={WorldId}, position=({X}, {Y}, {Z}).",
                owner.Guid,
                activator.Guid,
                world.Id,
                destination.Position.X,
                destination.Position.Y,
                destination.Position.Z);
        }

        private static Vector3 ToEulerDegrees(Quaternion q)
        {
            Vector3 vector = ToEuler(q);
            vector.X = ToDegrees(vector.X);
            vector.Y = ToDegrees(vector.Y);
            vector.Z = ToDegrees(vector.Z);
            return vector;
        }

        private static Vector3 ToEuler(Quaternion q)
        {
            float xx = q.X * q.X;
            float xy = q.X * q.Y;
            float xz = q.X * q.Z;
            float xw = q.X * q.W;
            float yy = q.Y * q.Y;
            float yz = q.Y * q.Z;
            float yw = q.Y * q.W;
            float zz = q.Z * q.Z;
            float zw = q.Z * q.W;

            float p = MathF.Asin(-2f * (yz - xw));
            float y = MathF.Atan2(2f * (xz + yw), 1f - 2f * (xx + yy));
            float r = MathF.Atan2(2f * (xy + zw), 1f - 2f * (xx + zz));
            return new Vector3(y, p, r);
        }

        private static float ToDegrees(float radians)
        {
            return radians * 180f / MathF.PI;
        }
    }
}
