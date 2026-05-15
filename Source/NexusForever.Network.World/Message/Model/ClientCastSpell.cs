using System;
using NexusForever.Network.Message;

namespace NexusForever.Network.World.Message.Model
{
    [Message(GameMessageOpcode.ClientCastSpell)]
    public class ClientCastSpell : IReadable
    {
        public uint ContextToken { get; private set; }

        [Obsolete("Use ContextToken. Native sender proof shows this field carries the generated cast-context token.")]
        public uint ClientUniqueId => ContextToken;
        public ushort BagIndex { get; private set; }
        public uint PrimaryTargetId { get; private set; }

        [Obsolete("Use PrimaryTargetId. Native sender proof shows this field carries the resolved target entity id.")]
        public uint CasterId => PrimaryTargetId;
        public bool ButtonPressed { get; private set; }

        public void Read(GamePacketReader reader)
        {
            ContextToken    = reader.ReadUInt();
            BagIndex        = reader.ReadUShort();
            PrimaryTargetId = reader.ReadUInt();
            ButtonPressed   = reader.ReadBit();
        }
    }
}
