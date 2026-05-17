namespace NexusForever.Shared.Diagnostics
{
    public sealed class ProfilingOptions
    {
        public bool Enabled { get; set; }
        public double SlowOperationThresholdMs { get; set; } = 50d;
        public bool IncludePacketPayloadSizes { get; set; }
    }
}
