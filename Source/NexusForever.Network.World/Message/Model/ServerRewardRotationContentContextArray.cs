using NexusForever.Network.Message;

namespace NexusForever.Network.World.Message.Model
{
    // Retained diagnostic/service data without wire registration.
    // Opcode 0x07D3 reloads reward tracks (consumer 1404d1640).
    public class ServerRewardRotationContentContextArray : IWritable
    {
        public List<ServerRewardRotationContentContext> Entries { get; set; } = new();

        public void Write(GamePacketWriter writer)
        {
            writer.Write(Entries.Count);
            for (int i = 0; i < Entries.Count; i++)
            {
                ServerRewardRotationContentContext entry = Entries[i]
                    ?? throw new InvalidOperationException($"{nameof(ServerRewardRotationContentContextArray)} entry {i} is null.");
                entry.Write(writer);
            }
        }
    }
}
