using System.Numerics;

namespace NexusForever.Game.Abstract.Entity.Movement.Force
{
    public interface IProjectileKeyGenerator
    {
        /// <summary>
        /// Generate key data for a projectile with the supplied parameters.
        /// </summary>
        void Calculate(TimeSpan flightTime, float gravity, Vector3 start, Vector3 end, out List<Vector3> positions, out List<Vector3> velocities, out List<uint> times);
    }
}
