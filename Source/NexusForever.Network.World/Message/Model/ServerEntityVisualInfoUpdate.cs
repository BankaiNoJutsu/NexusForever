using NexusForever.Network.Message;

namespace NexusForever.Network.World.Message.Model
{
    [Message(GameMessageOpcode.ServerEntityVisualInfoUpdate)]
    public class ServerEntityVisualInfoUpdate : IWritable
    {
        public uint UnitId { get; set; }
        public uint CreatureId { get; set; }
        public uint DisplayInfo { get; set; }
        public bool UnknownFlag0 { get; set; }
        public bool UnknownFlag1 { get; set; }

        public void Write(GamePacketWriter writer)
        {
            writer.Write(UnitId);
            writer.Write(CreatureId, 18u);
            writer.Write(DisplayInfo, 17u);
            writer.Write(UnknownFlag0);
            writer.Write(UnknownFlag1);
        }
    }
}
