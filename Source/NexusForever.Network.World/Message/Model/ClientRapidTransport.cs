using System;
using NexusForever.Network.Message;

namespace NexusForever.Network.World.Message.Model
{
    [Message(GameMessageOpcode.ClientRapidTransport)]
    public class ClientRapidTransport : IReadable
    {
        public ushort TaxiNode { get; private set; }
        // Native opcode 0x0141 registration confirms this is a raw 32-bit payload field.
        // Current reverse-engineering confidence is medium-high that this behaves as a cast-context token.
        // Keep this as opaque correlation data (not literal wall-clock time) until fully proven.
        public uint ContextToken { get; private set; }

        [Obsolete("Use ContextToken. This alias is retained for compatibility while semantics are being clarified.")]
        public uint Time => ContextToken;

        public void Read(GamePacketReader reader)
        {
            TaxiNode  = reader.ReadUShort(14u);
            ContextToken = reader.ReadUInt();
        }
    }
}
