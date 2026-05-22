using NexusForever.Network.Message;

namespace NexusForever.Network.World.Message.Model
{
    /// <summary>
    /// Store currency-package row shape for opcode 0x098F. Registered with the storefront
    /// cluster but no mapped client event consumer; keep diagnostic-only.
    /// </summary>
    [Message(GameMessageOpcode.ServerStoreCurrencyPackageRow)]
    public class ServerStoreCurrencyPackageRow : IWritable
    {
        public uint Value0 { get; set; }
        public string StringValue { get; set; } = string.Empty;
        public uint Value2 { get; set; }
        public float FloatValue { get; set; }
        public uint Value4 { get; set; }

        public void Write(GamePacketWriter writer)
        {
            writer.Write(Value0);
            writer.WriteStringWide(StringValue);
            writer.Write(Value2);
            writer.Write(FloatValue);
            writer.Write(Value4);
        }
    }
}
