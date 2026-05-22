using NexusForever.Network.Message;

namespace NexusForever.Network.World.Message.Model
{
    /// <summary>
    /// Quest share accept/decline result (0x0461, 0x10 bytes).
    /// Mirrors <see cref="ClientQuestShareResult"/> (0x045E).
    /// </summary>
    [Message(GameMessageOpcode.ServerQuestShareResult)]
    public class ServerQuestShareResult : IWritable
    {
        public ushort QuestId { get; set; }

        public bool Accepted { get; set; }

        public uint SharerUnitId { get; set; }

        public void Write(GamePacketWriter writer)
        {
            writer.Write(QuestId, 15u);
            writer.Write(Accepted);
            writer.Write(SharerUnitId);
            writer.WriteBytes(new byte[10]);
        }
    }
}
