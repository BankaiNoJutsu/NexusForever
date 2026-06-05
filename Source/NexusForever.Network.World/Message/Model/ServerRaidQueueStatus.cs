using NexusForever.Network.Message;

namespace NexusForever.Network.World.Message.Model
{
    /// <summary>
    /// Native reader: <c>ServerRaidQueueStatus_ReadPayload</c> (<c>14008bf80</c>).
    /// The same 0x20-byte row reader is used by <c>ServerRaidInfoResponse_ReadPayload</c>
    /// (<c>14008c010</c>) for opcode <c>0x071A</c>, and <c>Group_DispatchRaidInfoResponse</c>
    /// (<c>1406042b0</c>) maps the row to the raid-info UI fields below.
    /// Standalone non-zero opcode <c>0x0718</c> producer timing remains evidence-gated.
    /// </summary>
    [Message(GameMessageOpcode.ServerRaidQueueStatus)]
    public class ServerRaidQueueStatus : IWritable
    {
        public ulong SavedInstanceId { get; set; }

        public ushort WorldId { get; set; }

        public ulong DateExpireUTC { get; set; }

        public float DaysUntilExpire { get; set; }

        public uint PrimeLevel { get; set; }

        public void Write(GamePacketWriter writer)
        {
            writer.Write(SavedInstanceId);
            writer.Write(WorldId, 15u);
            writer.Write(DateExpireUTC);
            writer.Write(DaysUntilExpire);
            writer.Write(PrimeLevel);
        }
    }
}
