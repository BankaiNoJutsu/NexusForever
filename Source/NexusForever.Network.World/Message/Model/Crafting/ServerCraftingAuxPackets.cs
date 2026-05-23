using NexusForever.Network.Message;

namespace NexusForever.Network.World.Message.Model.Crafting
{
    /// <summary>
    /// Crafting-adjacent server output (0x084B, 24 bytes). Reader <c>FUN_1400a3af0</c> @ <c>1400a3af0</c>
    /// reads four uint32 values, one float, then one uint32;
    /// registered beside <see cref="GameMessageOpcode.ServerCraftingFinish"/> / <see cref="GameMessageOpcode.ServerCraftingCurrentCraft"/>.
    /// </summary>
    [Message(GameMessageOpcode.ServerCraftingAuxFourUInt32FloatUInt32)]
    public class ServerCraftingAuxFourUInt32FloatUInt32 : IWritable
    {
        public uint Value0 { get; set; }
        public uint Value1 { get; set; }
        public uint Value2 { get; set; }
        public uint Value3 { get; set; }
        public float FloatValue4 { get; set; }
        public uint Value5 { get; set; }

        public void Write(GamePacketWriter writer)
        {
            writer.Write(Value0);
            writer.Write(Value1);
            writer.Write(Value2);
            writer.Write(Value3);
            writer.Write(FloatValue4);
            writer.Write(Value5);
        }
    }

    /// <summary>
    /// Crafting-adjacent server output (0x0855, 12 bytes). Reader <c>FUN_140081df0</c> @ <c>140081df0</c>
    /// reads one uint32 value followed by two floats.
    /// </summary>
    [Message(GameMessageOpcode.ServerCraftingAuxUInt32AndTwoFloats)]
    public class ServerCraftingAuxUInt32AndTwoFloats : IWritable
    {
        public uint Value0 { get; set; }
        public float FloatValue1 { get; set; }
        public float FloatValue2 { get; set; }

        public void Write(GamePacketWriter writer)
        {
            writer.Write(Value0);
            writer.Write(FloatValue1);
            writer.Write(FloatValue2);
        }
    }
}
