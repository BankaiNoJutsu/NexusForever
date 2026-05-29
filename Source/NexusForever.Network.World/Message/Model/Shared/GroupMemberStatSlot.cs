using NexusForever.Network.Message;

namespace NexusForever.Network.World.Message.Model.Shared
{
    /// <summary>
    /// One of five stat-channel rows in group/matching member stat blocks.
    /// Native reader: <c>GroupMemberStatSlot_ReadPayload</c> (<c>1400823c0</c>).
    /// Client copies five rows (0x14 bytes) from parsed offset <c>+0x22</c> via
    /// <c>Group_CopyMemberStatBlockFromPayload</c> @ <c>140607490</c>.
    /// </summary>
    public class GroupMemberStatSlot : IWritable
    {
        /// <summary>Per-row ushort; semantics blocked.</summary>
        public ushort Value { get; set; }

        /// <summary>Retail wire marker; client defaults and NF emitters use <c>48</c> (<c>0x30</c>).</summary>
        public byte WireMarker0x30 { get; set; } = 48;

        public void Write(GamePacketWriter writer)
        {
            writer.Write(Value);
            writer.Write(WireMarker0x30);
        }
    }
}
