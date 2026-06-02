using System.Numerics;
using NexusForever.Database.Character;
using NexusForever.Game.Abstract.Entity;

namespace NexusForever.Game.Abstract.Map
{
    public interface IZoneMapManager : IDatabaseCharacter
    {
        void SendInitialPackets();
        void SendZoneMaps();

        /// <summary>
        /// Invoked when <see cref="IPlayer"/> moves to a new <see cref="Vector3"/>.
        /// </summary>
        void OnRelocate(Vector3 vector);

        /// <summary>
        /// Invoked when <see cref="IPlayer"/> moves to a new zone.
        /// </summary>
        void OnZoneUpdate();

        /// <summary>
        /// Returns floored explored percent (0–100) for a <see cref="MapZoneEntry"/> id, or 0 when no hex data exists.
        /// </summary>
        byte GetMapZoneExploredPercent(ushort mapZoneId);
    }
}