using NexusForever.Network.Message;

namespace NexusForever.Network.World.Message.Model.Crafting
{
    /// <summary>
    /// Crafting-adjacent server output (<c>0x084B</c>, 24 bytes). Reader @ <c>1400a3af0</c>
    /// (four uint32, float, uint32); registered beside <see cref="GameMessageOpcode.ServerCraftingFinish"/>.
    /// </summary>
    /// <remarks>
    /// Native emit path from craft-finish/discovery not verified. Field names stay <c>ValueN</c> until
    /// emitting function struct offsets are proven. NexusForever does not enqueue this opcode in production.
    /// </remarks>
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
    /// Crafting-adjacent server output (<c>0x0855</c>, 12 bytes). Reader @ <c>140081df0</c>
    /// (uint32, float, float).
    /// </summary>
    /// <remarks>Emit intent blocked - modeled and shape-tested only.</remarks>
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
