using System.Numerics;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Entity.Movement.Force;
using NexusForever.Game.Static.Entity.Movement.Command.State;

namespace NexusForever.Game.Entity.Movement.Force
{
    public class ForcedMovementGenerator : IForcedMovementGenerator
    {
        #region Dependency Injection

        private readonly IRotationKeyGenerator rotationKeyGenerator;
        private readonly IProjectileKeyGenerator projectileKeyGenerator;
        private readonly IStateKeyGenerator stateKeyGenerator;

        public ForcedMovementGenerator(
            IRotationKeyGenerator rotationKeyGenerator,
            IProjectileKeyGenerator projectileKeyGenerator,
            IStateKeyGenerator stateKeyGenerator)
        {
            this.rotationKeyGenerator   = rotationKeyGenerator;
            this.projectileKeyGenerator = projectileKeyGenerator;
            this.stateKeyGenerator      = stateKeyGenerator;
        }

        #endregion

        /// <summary>
        /// Force move <see cref="IUnitEntity"/> with supplied movement parameters.
        /// </summary>
        public void ForceMove(IUnitEntity mover, Vector3 position, Vector3 rotation, TimeSpan flightTime, float gravity, float spin)
        {
            if (flightTime <= TimeSpan.Zero)
            {
                mover.MovementManager.SetPosition(position, false);
                return;
            }

            if (gravity > 0f || mover.Position != position)
            {
                projectileKeyGenerator.Calculate(flightTime, gravity, mover.Position, position, out List<Vector3> positions, out List<Vector3> velocities, out List<uint> times);
                mover.MovementManager.SetPositionKeys(times, positions);
                mover.MovementManager.SetVelocityKeys(times, velocities);
            }

            if (spin != 0f)
            {
                rotationKeyGenerator.Calculate(rotation, flightTime, spin, out List<Vector3> rotations, out List<uint> rotationTimes);
                mover.MovementManager.SetRotationKeys(rotationTimes, rotations);
            }

            stateKeyGenerator.Calculate(flightTime, gravity, spin, out List<StateFlags> states, out List<uint> stateTimes);
            mover.MovementManager.SetStateKeys(stateTimes, states);
        }
    }
}
