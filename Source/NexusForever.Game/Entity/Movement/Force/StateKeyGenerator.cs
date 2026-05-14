using NexusForever.Game.Abstract.Entity.Movement.Force;
using NexusForever.Game.Static.Entity.Movement.Command.State;

namespace NexusForever.Game.Entity.Movement.Force
{
    public class StateKeyGenerator : IStateKeyGenerator
    {
        /// <summary>
        /// Generate state key data with the supplied parameters.
        /// </summary>
        public void Calculate(TimeSpan flightTime, float gravity, float spin, out List<StateFlags> states, out List<uint> times)
        {
            states = [];
            times  = [];

            StateFlags flags = StateFlags.None;
            if (gravity > 0f)
            {
                flags |= StateFlags.Velocity | StateFlags.Fall;
                if (spin != 0f)
                    flags |= StateFlags.RotationWhileFalling;
            }

            states.Add(flags);
            times.Add(0u);

            states.Add(StateFlags.None);
            times.Add((uint)flightTime.TotalMilliseconds);
        }
    }
}
