using NexusForever.Network.Message;

namespace NexusForever.Network.World.Message.Model
{
    /// <summary>
    /// Early housing residence cluster (opcodes <c>0x00CB</c>..<c>0x00D1</c>).
    /// Wire shapes are mapped from <c>FUN_14006c290</c>; consumer intent remains blocked.
    /// </summary>
    [Message(GameMessageOpcode.ServerHousingResidenceEmpty)]
    public class ServerHousingResidenceEmpty : IWritable
    {
        public void Write(GamePacketWriter writer)
        {
        }
    }

    /// <summary>
    /// 15-bit scalar housing residence output. Reader <c>ServerUInt15_ReadPayload</c> @ <c>14007c3a0</c>.
    /// </summary>
    [Message(GameMessageOpcode.ServerHousingResidenceUInt15)]
    public class ServerHousingResidenceUInt15 : IWritable
    {
        public uint Value { get; set; }

        public void Write(GamePacketWriter writer)
        {
            writer.Write(Value, 15u);
        }
    }

    /// <summary>
    /// Second 15-bit scalar housing residence output sharing <c>ServerUInt15_ReadPayload</c> with <c>0x00CC</c>.
    /// </summary>
    [Message(GameMessageOpcode.ServerHousingResidenceUInt15Alt)]
    public class ServerHousingResidenceUInt15Alt : IWritable
    {
        public uint Value { get; set; }

        public void Write(GamePacketWriter writer)
        {
            writer.Write(Value, 15u);
        }
    }

    /// <summary>
    /// Wide-string housing residence output between the scalar pair and instance-settings UI.
    /// </summary>
    [Message(GameMessageOpcode.ServerHousingResidenceWideString)]
    public class ServerHousingResidenceWideString : IWritable
    {
        public string Text { get; set; } = string.Empty;

        public void Write(GamePacketWriter writer)
        {
            writer.WriteStringWide(Text);
        }
    }

    /// <summary>
    /// Empty housing residence follow-up before <see cref="GameMessageOpcode.ClientClosedInstanceSettings"/>.
    /// Reader <c>ServerEmpty_ReadPayload</c> @ <c>14007d8e0</c>.
    /// </summary>
    [Message(GameMessageOpcode.ServerHousingResidenceEmptyFollowUp)]
    public class ServerHousingResidenceEmptyFollowUp : IWritable
    {
        public void Write(GamePacketWriter writer)
        {
        }
    }

    /// <summary>
    /// Empty housing-basics follow-up registered beside <see cref="GameMessageOpcode.ServerHousingBasics"/>.
    /// Reader <c>ServerEmpty_ReadPayload</c> @ <c>14007d8e0</c>.
    /// </summary>
    [Message(GameMessageOpcode.ServerHousingBasicsEmpty)]
    public class ServerHousingBasicsEmpty : IWritable
    {
        public void Write(GamePacketWriter writer)
        {
        }
    }

    /// <summary>
    /// Housing-basics follow-up for opcode <c>0x0110</c>.
    /// Reader <c>ServerHousingBasicsFollowup_ReadPayload</c> @ <c>14008de70</c>: uint32, 18-bit field, uint32, 8-bit field.
    /// </summary>
    [Message(GameMessageOpcode.ServerHousingBasicsFollowup)]
    public class ServerHousingBasicsFollowup : IWritable
    {
        public uint Value0 { get; set; }
        public uint Value1 { get; set; }
        public uint Value2 { get; set; }
        public uint Value3 { get; set; }

        public void Write(GamePacketWriter writer)
        {
            writer.Write(Value0);
            writer.Write(Value1, 18u);
            writer.Write(Value2);
            writer.Write(Value3, 8u);
        }
    }
}
