using System.Numerics;
using NexusForever.Game.Abstract.Entity.Movement;
using NexusForever.Game.Abstract.Entity.Movement.Command.Position;
using NexusForever.Game.Abstract.Entity.Movement.Force;
using NexusForever.Game.Entity.Movement.Key;
using NexusForever.Game.Static.Entity.Movement.Command;
using NexusForever.Network.World.Entity;
using NexusForever.Network.World.Entity.Command;

namespace NexusForever.Game.Entity.Movement.Command.Position
{
    public class PositionProjectileCommand : IPositionCommand
    {
        public EntityCommand Command => EntityCommand.SetPositionProjectile;

        /// <summary>
        /// Returns if the command has been finalised.
        /// </summary>
        public bool IsFinalised => positionKeys?.IsFinalised ?? false;

        private readonly PositionKeys positionKeys = new();

        private Vector3 destination;
        private Vector3 rotation;
        private uint flightTime;
        private float gravity;

        #region Dependency Injection

        private readonly IProjectileKeyGenerator projectileKeyGenerator;

        public PositionProjectileCommand(
            IProjectileKeyGenerator projectileKeyGenerator)
        {
            this.projectileKeyGenerator = projectileKeyGenerator;
        }

        #endregion

        /// <summary>
        /// Initialise command with the supplied projectile destination, rotation, flight time and gravity.
        /// </summary>
        public void Initialise(IMovementManager movementManager, Vector3 position, Vector3 rotation, TimeSpan flightTime, float gravity)
        {
            ArgumentNullException.ThrowIfNull(movementManager);
            if (flightTime <= TimeSpan.Zero)
                throw new ArgumentOutOfRangeException(nameof(flightTime));

            destination     = position;
            this.rotation   = rotation;
            this.flightTime = (uint)Math.Ceiling(flightTime.TotalMilliseconds);
            this.gravity    = gravity;

            projectileKeyGenerator.Calculate(flightTime, gravity, movementManager.GetPosition(), position,
                out List<Vector3> positions, out _, out List<uint> times);

            positionKeys.Initialise(movementManager, times, positions);
        }

        /// <summary>
        /// Invoked each world tick with the delta since the previous tick occurred.
        /// </summary>
        public void Update(double lastTick)
        {
            // deliberately empty
        }

        /// <summary>
        /// Returns the <see cref="INetworkEntityCommand"/> for the entity command.
        /// </summary>
        public INetworkEntityCommand GetNetworkEntityCommand()
        {
            return new NetworkEntityCommand
            {
                Command = Command,
                Model   = new SetPositionProjectileCommand
                {
                    Position   = destination,
                    Rotation   = rotation,
                    FlightTime = flightTime,
                    Gravity    = gravity,
                    Blend      = false
                }
            };
        }

        /// <summary>
        /// Returns the current <see cref="Vector3"/> position for the entity command.
        /// </summary>
        public Vector3 GetPosition()
        {
            return positionKeys.GetInterpolated();
        }

        /// <summary>
        /// Returns the current <see cref="Vector3"/> rotation for the entity command.
        /// </summary>
        public Vector3 GetRotation()
        {
            int index = Math.Min(positionKeys.GetIndex(), positionKeys.Values.Count - 2);
            if (index < 0)
                return rotation;

            Vector3 direction = positionKeys.Values[index + 1] - positionKeys.Values[index];
            if (direction.LengthSquared() <= float.Epsilon)
                return rotation;

            direction = Vector3.Normalize(direction);
            float pitch = MathF.Asin(-direction.Y);
            float yaw = MathF.Atan2(-direction.X, -direction.Z);
            return new Vector3(yaw, pitch, 0f);
        }
    }
}
