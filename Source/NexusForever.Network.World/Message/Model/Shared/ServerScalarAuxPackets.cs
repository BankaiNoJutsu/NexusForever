using NexusForever.Network.Message;

namespace NexusForever.Network.World.Message.Model.Shared
{
    /// <summary>
    /// Client reader <c>ServerUInt32WideString_ReadPayload</c> @ <c>1400980f0</c>.
    /// </summary>
    public class ServerUInt32WideStringPayload : IWritable
    {
        public uint Value { get; set; }
        public string Text { get; set; } = string.Empty;

        public void Write(GamePacketWriter writer)
        {
            writer.Write(Value);
            writer.WriteStringWide(Text);
        }
    }

    /// <summary>
    /// Client reader <c>ServerUInt32_ReadPayload</c> @ <c>14007ab50</c>.
    /// </summary>
    public class ServerUInt32Payload : IWritable
    {
        public uint Value { get; set; }

        public void Write(GamePacketWriter writer)
        {
            writer.Write(Value);
        }
    }

    /// <summary>
    /// Client reader <c>ServerEmpty_ReadPayload</c> @ <c>14007d8e0</c>.
    /// </summary>
    public class ServerEmptyPayload : IWritable
    {
        public void Write(GamePacketWriter writer)
        {
        }
    }

    /// <summary>
    /// Three consecutive uint32 fields (client helper <c>FUN_1400a8a30</c>).
    /// </summary>
    public class ServerUInt32Triplet : IWritable
    {
        public uint Value0 { get; set; }
        public uint Value1 { get; set; }
        public uint Value2 { get; set; }

        public void Write(GamePacketWriter writer)
        {
            writer.Write(Value0);
            writer.Write(Value1);
            writer.Write(Value2);
        }
    }
}
