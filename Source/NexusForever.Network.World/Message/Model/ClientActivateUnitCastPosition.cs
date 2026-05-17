using System;
using NexusForever.Network.Message;
using NexusForever.Network.World.Entity;

namespace NexusForever.Network.World.Message.Model
{
    [Message(GameMessageOpcode.ClientActivateUnitCastPosition)]
    public class ClientActivateUnitCastPosition : IReadable
    {
        public uint ContextToken { get; private set; }

        [Obsolete("Use ContextToken. Native sender proof shows this field carries the generated cast-context token.")]
        public uint ClientUniqueId => ContextToken;

        public byte SelectorA { get; private set; }
        public byte SelectorB { get; private set; }
        public uint TargetEntityId { get; private set; }
        public Position Position { get; } = new();

        public void Read(GamePacketReader reader)
        {
            ContextToken   = reader.ReadUInt();
            SelectorA      = reader.ReadByte(4u);
            SelectorB      = reader.ReadByte(4u);
            TargetEntityId = reader.ReadUInt();
            Position.Read(reader);
        }
    }
}