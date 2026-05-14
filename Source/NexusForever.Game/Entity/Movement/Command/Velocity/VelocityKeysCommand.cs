using System.Numerics;
using NexusForever.Game.Abstract.Entity.Movement;
using NexusForever.Game.Abstract.Entity.Movement.Command.Velocity;
using NexusForever.Game.Entity.Movement.Key;
using NexusForever.Game.Static.Entity.Movement.Command;
using NexusForever.Network.World.Entity;
using NexusForever.Network.World.Entity.Command;

namespace NexusForever.Game.Entity.Movement.Command.Velocity
{
    public class VelocityKeysCommand : IVelocityCommand
    {
        public EntityCommand Command => EntityCommand.SetVelocityKeys;

        /// <summary>
        /// Returns if the command has been finalised.
        /// </summary>
        public bool IsFinalised => velocityKeys?.IsFinalised ?? false;

        private readonly VelocityKey velocityKeys = new();

        /// <summary>
        /// Initialise command with the specified times and <see cref="Vector3"/> velocity key values.
        /// </summary>
        public void Initialise(IMovementManager movementManager, List<uint> times, List<Vector3> velocities)
        {
            velocityKeys.Initialise(movementManager, times, velocities);
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
                Model   = new SetVelocityKeysCommand
                {
                    Times      = velocityKeys.Times,
                    Velocities = velocityKeys.Values
                }
            };
        }

        /// <summary>
        /// Return the current <see cref="Vector3"/> velocity value for the entity command.
        /// </summary>
        public Vector3 GetVelocity()
        {
            return velocityKeys.GetInterpolated();
        }
    }
}
