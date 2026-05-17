using System.Numerics;
using NexusForever.Game.Static.Entity.Movement.Command.Mode;
using NexusForever.Game.Static.Entity.Movement.Command.State;

namespace NexusForever.Game.Abstract.Entity.Movement.AntiTamper
{
    public interface IClientMovementCommandValidator
    {
        /// <summary>
        /// Validate the time between the client and server to ensure the client is not tampering with the time.
        /// </summary>
        void ValidateTime(uint clientTime, uint serverTime);

        /// <summary>
        /// Validate the position from the client to ensure the client is not tampering with the position.
        /// </summary>
        void ValidatePosition(Vector3 clientPosition);

        /// <summary>
        /// Validate the mode from the client to ensure the client is not tampering with the mode.
        /// </summary>
        void ValidateMode(ModeType mode);

        /// <summary>
        /// Validate the state from the client to ensure the client is not tampering with the state.
        /// </summary>
        void ValidateState(StateFlags state);
    }
}
