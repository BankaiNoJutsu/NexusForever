using NexusForever.Network.Message;
using NexusForever.Network.World.Message.Static;

namespace NexusForever.Network.World.Message.Model
{
    [Message(GameMessageOpcode.ServerHousingCommunityRenameResult)]
    public class ServerHousingCommunityRenameResult : IWritable
    {
        public HousingResult Result { get; set; }

        // Retail client build 16042 reads these fields but only exposes Result
        // through the CommunityRenameResult event descriptor.
        public uint Reserved0 { get; set; }
        public uint Reserved1 { get; set; }
        public uint Reserved2 { get; set; }

        public void Write(GamePacketWriter writer)
        {
            writer.Write((uint)Result);
            writer.Write(Reserved0);
            writer.Write(Reserved1);
            writer.Write(Reserved2);
        }
    }
}
