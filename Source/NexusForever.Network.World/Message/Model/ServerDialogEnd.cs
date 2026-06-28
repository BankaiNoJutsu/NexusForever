using NexusForever.Network.Message;

namespace NexusForever.Network.World.Message.Model
{
    /// <summary>
    /// Native opcode <c>0x0358</c> uses shared <c>ServerUInt32_ReadPayload</c> for the dialog unit id.
    /// </summary>
    [Message(GameMessageOpcode.ServerDialogEnd)]
    public class ServerDialogEnd : IWritable
    {
        public uint DialogUnitId { get; set; }

        public void Write(GamePacketWriter writer)
        {
            writer.Write(DialogUnitId);
        }
    }
}
