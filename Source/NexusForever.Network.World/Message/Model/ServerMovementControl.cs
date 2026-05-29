using NexusForever.Network.Message;

namespace NexusForever.Network.World.Message.Model
{
    [Message(GameMessageOpcode.ServerMovementControl)]
    public class ServerMovementControl : IWritable
    {
        /// <summary>
        /// Runtime sender <c>Player.SetControl</c> currently writes one 32-bit ticket, one
        /// trailing control-mode bit, and one 32-bit controlled unit id before the client
        /// responds with opcode <c>0x0635</c>.
        /// No WildStar64 client reader anchor has been recovered for this server packet.
        /// </summary>
        public uint Ticket { get; set; }
        public bool Immediate { get; set; }
        public uint UnitId { get; set; }

        public void Write(GamePacketWriter writer)
        {
            writer.Write(Ticket);
            writer.Write(Immediate);
            writer.Write(UnitId);
        }
    }
}
