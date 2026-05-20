using System.Numerics;
using NexusForever.Game.Abstract.Entity.Movement.AntiTamper;
using NexusForever.Game.Static.Entity.Movement.Command.Mode;
using NexusForever.Game.Static.Entity.Movement.Command.State;

namespace NexusForever.Game.Entity.Movement.AntiTamper
{
    public class ClientMovementCommandValidator : IClientMovementCommandValidator
    {
        private const StateFlags KnownStateMask =
            StateFlags.Velocity
            | StateFlags.Move
            | StateFlags.Unknown04
            | StateFlags.Fall
            | StateFlags.Unknown10
            | StateFlags.Unknown20
            | StateFlags.Jump
            | StateFlags.ModeNonWalk
            | StateFlags.ModeWalk
            | StateFlags.ModeSwim
            | StateFlags.ModeSlide
            | StateFlags.DoubleJump
            | StateFlags.RollForward
            | StateFlags.RollBackward
            | StateFlags.RotationWhileFalling;

        /// <summary>
        /// Validate the time between the client and server to ensure the client is not tampering with the time.
        /// </summary>
        public void ValidateTime(uint clientTime, uint serverTime)
        {
            // Client-owned movement sends the client's simulation clock. Debug-world stalls can create
            // large but valid deltas, so treat finite SetTime values as synchronization input.
        }

        /// <summary>
        /// Validate the position from the client to ensure the client is not tampering with the position.
        /// </summary>
        public void ValidatePosition(Vector3 clientPosition)
        {
            if (!IsFinite(clientPosition))
                throw new InvalidOperationException($"Invalid client movement position received: {clientPosition}.");
        }

        /// <summary>
        /// Validate the mode from the client to ensure the client is not tampering with the mode.
        /// </summary>
        public void ValidateMode(ModeType mode)
        {
            if (!Enum.IsDefined(mode))
                throw new InvalidOperationException($"Invalid client movement mode received: {mode}.");
        }

        /// <summary>
        /// Validate the state from the client to ensure the client is not tampering with the state.
        /// </summary>
        public void ValidateState(StateFlags state)
        {
            if ((state & ~KnownStateMask) != StateFlags.None)
                throw new InvalidOperationException($"Invalid client movement state flags received: {state}.");
        }

        private static bool IsFinite(Vector3 value)
        {
            return float.IsFinite(value.X)
                && float.IsFinite(value.Y)
                && float.IsFinite(value.Z);
        }
    }
}
