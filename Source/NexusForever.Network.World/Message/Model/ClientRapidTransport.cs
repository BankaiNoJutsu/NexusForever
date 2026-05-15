using System;
using NexusForever.Network.Message;

namespace NexusForever.Network.World.Message.Model
{
    [Message(GameMessageOpcode.ClientRapidTransport)]
    public class ClientRapidTransport : IReadable
    {
        public ushort TaxiNode { get; private set; }
        // Native opcode 0x0141 registration confirms this is a raw 32-bit payload field.
        // Native cast-context construction seeds the shared +0x60 field from a generated global counter,
        // while the incoming client spellcast/request id is stored separately in the same object.
        // Treat this as server-generated cast/context correlation data rather than wall-clock time.
        public uint ContextToken { get; private set; }

        [Obsolete("Use ContextToken. Native packet proof shows this field carries the generated cast-context token.")]
        public uint Time => ContextToken;

        public void Read(GamePacketReader reader)
        {
            TaxiNode  = reader.ReadUShort(14u);
            ContextToken = reader.ReadUInt();
        }
    }
}
