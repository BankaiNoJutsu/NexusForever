namespace NexusForever.Network.Packet
{
    public class ClientGamePacket
    {
        public bool IsEncrypted { get; init; }
        public byte[] Data { get; init; }
        public long QueuedTimestamp { get; init; }
    }
}
