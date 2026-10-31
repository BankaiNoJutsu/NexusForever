using NexusForever.Network.Internal.Message.Shared;

namespace NexusForever.Network.Internal.Message.Player
{
    public class PlayerLoggedOutMessage
    {
        public Identity Identity { get; set; }
        public uint AccountId { get; set; }
        public ulong SessionEpoch { get; set; }
        public ulong Admission { get; set; }
        public DateTime? OccurredUtc { get; set; }
    }
}
