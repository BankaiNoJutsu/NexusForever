using NexusForever.Game.Static.PublicEvent;
using NexusForever.Network.Message;

namespace NexusForever.Network.World.Message.Model.Shared
{
    // Client reader WildStar64.exe 14007b930 reads a 32-bit stat mask, 5-bit value count,
    // then count uint32 stat values.
    public class PublicEventStats : IWritable
    {
        public NetworkBitArray Mask { get; set; } = new NetworkBitArray(32, NetworkBitArray.BitOrder.LeastSignificantBit);
        public List<uint> Values { get; set; } = [];

        public void Write(GamePacketWriter writer)
        {
            writer.WriteBytes(Mask.GetBuffer());
            writer.Write(Values.Count, 5);

            foreach (uint value in Values)
                writer.Write(value);
        }
    }
}
