using NexusForever.Network.Message;

namespace NexusForever.Network.World.Message.Model
{
    // Server opcode 0x07D3.
    // Wire format matches ServerRewardRotationContentContextArray_ReadPayload @ 14008fdc0:
    // 32-bit count followed by count rows, each serialized through ServerRewardRotationContentContext_ReadPayload @ 14008fcb0.
    [Message(GameMessageOpcode.ServerRewardRotationContentContextArray)]
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
