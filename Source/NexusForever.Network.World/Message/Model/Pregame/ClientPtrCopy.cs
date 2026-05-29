using NexusForever.Network.Message;

namespace NexusForever.Network.World.Message.Model.Pregame
{
    /// <summary>
    /// Opcode <c>0x06E8</c> is a separate native slot from
    /// <see cref="ClientInitiatePTRCharacterCopy"/> (<c>0x06E7</c>). Native registration binds
    /// size <c>1</c> with shared <c>ClientCraftingAbandon_WritePayload</c>; send sites
    /// <c>FUN_14063f540</c> and <c>FUN_140707d80</c> zero one local byte before send. Managed
    /// read stays empty like other size-1 zero-payload opcodes in this repo.
    /// </summary>
    [Message(GameMessageOpcode.ClientPtrCopy)]
    public class ClientPtrCopy : IReadable
    {
        public void Read(GamePacketReader reader)
        {
            // Native size-1 zero-payload slot; no fields on the wire.
        }
    }
}
