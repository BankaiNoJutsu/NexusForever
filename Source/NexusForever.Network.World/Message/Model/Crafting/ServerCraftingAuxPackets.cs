using NexusForever.Network.Message;

namespace NexusForever.Network.World.Message.Model.Crafting
{
    /// <summary>
    /// Crafting-adjacent server output (0x084B, 24 bytes). Reader <c>Server0x084B_ReadPayload</c> @ <c>1400a3af0</c>;
    /// registered beside <see cref="GameMessageOpcode.ServerCraftingFinish"/> / <see cref="GameMessageOpcode.ServerCraftingCurrentCraft"/>.
    /// </summary>
    [Message(GameMessageOpcode.ServerCraftingAuxSixUInt32)]
    public class ServerCraftingAuxSixUInt32 : IWritable
    {
        public uint Value0 { get; set; }
        public uint Value1 { get; set; }
        public uint Value2 { get; set; }
        public uint Value3 { get; set; }
        public uint Value4 { get; set; }
        public uint Value5 { get; set; }

        public void Write(GamePacketWriter writer)
        {
            writer.Write(Value0);
            writer.Write(Value1);
            writer.Write(Value2);
            writer.Write(Value3);
            writer.Write(Value4);
            writer.Write(Value5);
        }
    }

    /// <summary>
    /// Crafting-adjacent server output (0x0855, 12 bytes). Reader <c>FUN_140081df0</c> @ <c>140081df0</c>.
    /// </summary>
    [Message(GameMessageOpcode.ServerCraftingAuxThreeUInt32)]
    public class ServerCraftingAuxThreeUInt32 : IWritable
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
