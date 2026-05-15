using System;
using NexusForever.Network.Message;
using NexusForever.Network.World.Entity;

namespace NexusForever.Network.World.Message.Model
{
    [Message(GameMessageOpcode.ClientCastSpellPosition)]
    public class ClientCastSpellPosition : IReadable
    {
        public uint ContextToken { get; private set; }

        [Obsolete("Use ContextToken. Native sender proof shows this field carries the generated cast-context token.")]
        public uint ClientUniqueId => ContextToken;
        public ushort BagIndex { get; private set; }
        public Position Position { get; } = new();

        public void Read(GamePacketReader reader)
        {
            ContextToken = reader.ReadUInt();
            BagIndex     = reader.ReadUShort();
            Position.Read(reader);
        }
    }
}
