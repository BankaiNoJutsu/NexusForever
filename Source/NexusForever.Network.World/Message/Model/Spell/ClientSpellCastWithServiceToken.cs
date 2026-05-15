using System;
using NexusForever.Network.Message;

namespace NexusForever.Network.World.Message.Model.Spell
{
    [Message(GameMessageOpcode.ClientSpellCastWithServiceToken)]
    public class ClientSpellCastWithServiceToken : IReadable
    {
        // Native opcode 0x00C2 registration masks this generated context token to 18 bits on the wire.
        public uint ContextToken { get; private set; }

        [Obsolete("Use ContextToken. Native packet proof shows this field carries the generated cast-context token.")]
        public uint ClientSpellCastUniqueId => ContextToken;
        public uint Spell4Id { get; private set; }

        public void Read(GamePacketReader reader)
        {
            ContextToken = reader.ReadUInt(18u);
            Spell4Id = reader.ReadUInt();
        }
    }
}