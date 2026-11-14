using NexusForever.Network.Message;

namespace NexusForever.Network.World.Message.Model
{
    /// <summary>
    /// Removes the listed receiving units' target nodes from a spell wrapper.
    /// Verified native consumer: 0x0811 -> 1405a5aa0; destroys matching nodes and children.
    /// Registration 14006c290 binds reader 14007a550: wrapper ID, UInt32 count, then receiving-unit IDs.
    /// Original server emission policy remains unqualified; no retail parity claim.
    /// </summary>
    [Message(GameMessageOpcode.ServerSpellCastTargetList)]
    public class ServerSpellCastTargetList : IWritable
    {
        public uint CastingId { get; set; }
        // Legacy property name; these are receiving-unit IDs in the native consumer.
        public List<uint> CasterId { get; set; } = new();

        public void Write(GamePacketWriter writer)
        {
            writer.Write(CastingId);
            writer.Write(CasterId.Count, 32u);
            CasterId.ForEach(c => writer.Write(c));
        }
    }
}
