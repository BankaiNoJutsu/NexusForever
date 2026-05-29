using NexusForever.Network.Message;

namespace NexusForever.Network.World.Message.Model.Pregame
{
    /// <summary>
    /// Server opcode <c>0x06EA</c>. Native registration uses <c>ServerEmpty_ReadPayload</c>
    /// with registered size <c>1</c>. Client consumer <c>FUN_140020ea0</c> fires Lua event
    /// <c>PTRCharacterCopyQueued</c> @ <c>1409ed738</c>. Can be sent while the player is on
    /// the character-select screen or in game; NexusForever has no mapped emit yet.
    /// </summary>
    [Message(GameMessageOpcode.ServerPtrCharacterCopyQueued)]
    public class ServerPtrCharacterCopyQueued : IWritable
    {
        public void Write(GamePacketWriter writer)
        {
            // Native size-1 empty server payload; no fields on the wire.
        }
    }
}
