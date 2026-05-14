using System.Numerics;

namespace NexusForever.Game.Abstract.Entity.Movement.Force
{
    public interface IForcedMovementGenerator
    {
        /// <summary>
        /// Force move <see cref="IUnitEntity"/> with supplied movement parameters.
        /// </summary>
        void ForceMove(IUnitEntity mover, Vector3 position, Vector3 rotation, TimeSpan flightTime, float gravity, float spin);
    }
}
