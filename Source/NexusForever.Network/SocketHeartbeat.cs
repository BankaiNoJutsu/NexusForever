using NexusForever.Shared;

namespace NexusForever.Network
{
    public class SocketHeartbeat : IUpdate
    {
        private const double DefaultTimeToFlatline = 300d;

        public bool Flatline => timeToFlatline <= 0d;
        public double SecondsUntilFlatline => timeToFlatline;
        public double TimeoutSeconds => DefaultTimeToFlatline;

        private double timeToFlatline;

        public SocketHeartbeat()
        {
            OnHeartbeat();
        }

        public void OnHeartbeat()
        {
            timeToFlatline = DefaultTimeToFlatline;
        }

        public void Update(double lastTick)
        {
            timeToFlatline -= lastTick;
        }
    }
}
