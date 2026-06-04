using NexusForever.Network.Message;

namespace NexusForever.Network.World.Message.Model
{
    /// <summary>
    /// Compact entity visual-info update (<c>0x08A8</c>).
    /// Reader: <c>ServerEntityVisualInfoUpdate_ReadPayload</c> @ <c>140098460</c>.
    /// </summary>
    /// <remarks>Flag meanings and runtime producer conditions remain blocked; do not infer from adjacent visual packets.</remarks>
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
