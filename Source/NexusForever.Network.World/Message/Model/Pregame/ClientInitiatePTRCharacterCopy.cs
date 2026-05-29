using NexusForever.Network.Message;

namespace NexusForever.Network.World.Message.Model.Pregame
{
    /// <summary>
    /// Character-select Lua API <c>InitiatePTRCharacterCopy</c> sends opcode <c>0x06E7</c> from native
    /// sender <c>ClientInitiatePTRCharacterCopy_SendFromLuaDispatch</c> @ <c>140022270</c> with the
    /// selected character id, then sends <c>ClientEncrypted</c> <c>0x0244</c> on success. That path is
    /// separate from <see cref="ClientPtrCopy"/> (<c>0x06E8</c>) and from server
    /// <see cref="ServerPtrCharacterCopyQueued"/> (<c>0x06EA</c>).
    /// </summary>
    [Message(GameMessageOpcode.ClientInitiatePTRCharacterCopy)]
    public class ClientInitiatePTRCharacterCopy : IReadable
    {
        public ulong CharacterId { get; private set; }

        public void Read(GamePacketReader reader)
        {
            CharacterId = reader.ReadULong();
        }
    }
}
