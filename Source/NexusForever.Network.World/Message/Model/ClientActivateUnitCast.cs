using System;
using NexusForever.Network.Message;

namespace NexusForever.Network.World.Message.Model
{
    [Message(GameMessageOpcode.ClientActivateUnitCast)]
    public class ClientActivateUnitCast : IReadable
    {
        public uint ContextToken { get; private set; }

        [Obsolete("Use ContextToken. Native sender proof shows this field carries the generated cast-context token.")]
        public uint ClientUniqueId => ContextToken;

        public uint ActivateUnitId { get; private set; }

        public void Read(GamePacketReader reader)
        {
            ContextToken    = reader.ReadUInt();
            ActivateUnitId  = reader.ReadUInt();
        }
    }
}
