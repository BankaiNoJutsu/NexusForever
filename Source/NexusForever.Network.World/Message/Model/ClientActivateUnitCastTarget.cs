using System;
using NexusForever.Network.Message;

namespace NexusForever.Network.World.Message.Model
{
    [Message(GameMessageOpcode.ClientActivateUnitCastTarget)]
    public class ClientActivateUnitCastTarget : IReadable
    {
        public uint ContextToken { get; private set; }

        [Obsolete("Use ContextToken. Native sender proof shows this field carries the generated cast-context token.")]
        public uint ClientUniqueId => ContextToken;

        public uint TargetField { get; private set; }

        public void Read(GamePacketReader reader)
        {
            ContextToken = reader.ReadUInt();
            TargetField  = reader.ReadUInt();
        }
    }
}