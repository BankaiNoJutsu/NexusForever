using NexusForever.Game.Static.Entity.Movement.Command.State;

namespace NexusForever.Game.Abstract.Entity.Movement.Force
{
    public interface IStateKeyGenerator
    {
        /// <summary>
        /// Generate state key data with the supplied parameters.
        /// </summary>
        void Calculate(TimeSpan flightTime, float gravity, float spin, out List<StateFlags> states, out List<uint> times);
    }
}
