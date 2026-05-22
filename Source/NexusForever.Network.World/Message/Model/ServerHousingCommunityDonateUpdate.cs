using NexusForever.Network.Message;

namespace NexusForever.Network.World.Message.Model
{
    /// <summary>
    /// Native opcode <c>0x04FE</c>: count plus two parallel uint32 arrays (client
    /// <c>FUN_14009e930</c>).
    /// </summary>
    [Message(GameMessageOpcode.ServerHousingCommunityDonateUpdate)]
    public class ServerHousingCommunityDonateUpdate : IWritable
    {
        public class Entry
        {
            public uint Value0 { get; set; }
            public uint Value1 { get; set; }
        }

        public List<Entry> Entries { get; } = new();

        public void Write(GamePacketWriter writer)
        {
            writer.Write(Entries.Count);
            foreach (Entry entry in Entries)
                writer.Write(entry.Value0);
            foreach (Entry entry in Entries)
                writer.Write(entry.Value1);
        }
    }
}
