using NexusForever.Game.Static.Item;
using NexusForever.Network.Message;
using NexusForever.Network.World.Message.Model.Shared;

namespace NexusForever.Network.World.Message.Model.Item
{
    // The krakal/inventory branch mapped several of these shapes to opcodes that
    // this checkout now has stronger decompile labels for. Keep the payload
    // models available without registering conflicting opcodes.
    public class ServerItemCharges : IWritable
    {
        public ulong ItemGuid { get; set; }
        public uint Charges { get; set; }

        public void Write(GamePacketWriter writer)
        {
            writer.Write(ItemGuid);
            writer.Write(Charges);
        }
    }

    public class ServerItemDurability : IWritable
    {
        public ulong ItemGuid { get; set; }
        public float Durability { get; set; }

        public void Write(GamePacketWriter writer)
        {
            writer.Write(ItemGuid);
            writer.Write(Durability);
        }
    }

    public class ServerItemDyteData : IWritable
    {
        public ulong ItemGuid { get; set; }
        public uint DyeData { get; set; }

        public void Write(GamePacketWriter writer)
        {
            writer.Write(ItemGuid);
            writer.Write(DyeData);
        }
    }

    public class ServerItemFlags : IWritable
    {
        public ulong ItemGuid { get; set; }
        public ItemFlags Flags { get; set; }

        public void Write(GamePacketWriter writer)
        {
            writer.Write(ItemGuid);
            writer.Write(Flags, 8u);
        }
    }

    [Message(GameMessageOpcode.ServerItemTradePartners)]
    public class ServerItemTradePartners : IWritable
    {
        public ulong ItemGuid { get; set; }
        public List<Identity> TradingPartnerIdentities { get; } = [];
        public uint TradeTimeLeftMs { get; set; }

        public void Write(GamePacketWriter writer)
        {
            writer.Write(ItemGuid);
            // Native reader 1400a0a50 -> 1400a0960: count32, identity14+64 rows,
            // then a trailing32 time field. This differs from the nested Item list.
            writer.Write(TradingPartnerIdentities.Count);
            foreach (Identity identity in TradingPartnerIdentities)
                identity.Write(writer);
            writer.Write(TradeTimeLeftMs);
        }
    }
}
