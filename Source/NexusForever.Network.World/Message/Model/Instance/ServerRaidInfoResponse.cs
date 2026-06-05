using NexusForever.Network.Message;

namespace NexusForever.Network.World.Message.Model.Instance
{
    /// <summary>
    /// Native opcode <c>0x071A</c> reads a count and 0x20-byte raid-info rows through
    /// <c>ServerRaidInfoResponse_ReadPayload</c> (<c>14008c010</c>), then dispatches
    /// <c>RaidInfoResponse</c> from <c>Group_DispatchRaidInfoResponse</c> (<c>1406042b0</c>).
    /// </summary>
    [Message(GameMessageOpcode.ServerRaidInfoResponse)]
    public class ServerRaidInfoResponse : IWritable
    {
        public class RaidInfo : IWritable
        {
            public ulong SavedInstanceId { get; set; }
            public ushort WorldId { get; set; }
            public ulong DateExpireUTC { get; set; } // Native consumer treats this as Windows FILETIME.
            public float DaysUntilExpire { get; set; } // Relative time from now the lock resets.
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

        public List<RaidInfo> Raids { get; set; } = [];

        public void Write(GamePacketWriter writer)
        {
            writer.Write(Raids.Count);
            Raids.ForEach(raid => raid.Write(writer));
        }
    }
}
