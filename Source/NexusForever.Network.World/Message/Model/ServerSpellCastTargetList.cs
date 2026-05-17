using NexusForever.Network.Message;

namespace NexusForever.Network.World.Message.Model
{
    /// <summary>
    /// Opaque list-style spell broadcast follow-up packet.
    /// Current evidence suggests this replays a subset of spell-start or spell-go target data.
    /// </summary>
    [Message(GameMessageOpcode.ServerSpellCastTargetList)]
    public class ServerSpellCastTargetList : IWritable
    {
        public uint CastingId { get; set; }
        public List<uint> CasterId { get; set; } = new();

        public void Write(GamePacketWriter writer)
        {
            writer.Write(CastingId);
            writer.Write(CasterId.Count, 32u);
            CasterId.ForEach(c => writer.Write(c));
        }
    }
}
