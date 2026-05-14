using System.Numerics;

namespace NexusForever.Game.Abstract.Entity.Movement.Force
{
    public interface IRotationKeyGenerator
    {
        /// <summary>
        /// Generate key data for rotation with the supplied parameters.
        /// </summary>
        void Calculate(Vector3 rotation, TimeSpan duration, float spin, out List<Vector3> rotations, out List<uint> times);
    }
}
