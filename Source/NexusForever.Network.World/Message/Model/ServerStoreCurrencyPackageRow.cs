using NexusForever.Game.Static.Account;
using NexusForever.Network.Message;

namespace NexusForever.Network.World.Message.Model
{
    /// <summary>
    /// Store currency-package row shape for opcode 0x098F. The client reads these rows
    /// under <see cref="ServerStoreCategories"/> and applies them to the nested currency-
    /// package cache rather than dispatching a standalone 0x098F event.
    /// </summary>
    [Message(GameMessageOpcode.ServerStoreCurrencyPackageRow)]
    public class ServerStoreCurrencyPackageRow : IWritable
    {
        public uint Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public uint Count { get; set; }
        public float Price { get; set; }
        public AccountCurrencyType CurrencyType { get; set; }

        public void Write(GamePacketWriter writer)
        {
            writer.Write(Id);
            writer.WriteStringWide(Name);
            writer.Write(Count);
            writer.Write(Price);
            writer.Write((uint)CurrencyType);
        }
    }
}
