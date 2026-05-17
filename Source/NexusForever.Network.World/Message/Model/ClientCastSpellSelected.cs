using System;
using NexusForever.Network.Message;
using NexusForever.Network.World.Entity;

namespace NexusForever.Network.World.Message.Model
{
    [Message(GameMessageOpcode.ClientCastSpellSelected)]
    public class ClientCastSpellSelected : IReadable
    {
        public uint ContextToken { get; private set; }

        [Obsolete("Use ContextToken. Native sender proof shows this field carries the generated cast-context token.")]
        public uint ClientUniqueId => ContextToken;

        public uint SelectedEntryId { get; private set; }
        public uint TargetEntityId { get; private set; }

        [Obsolete("Use TargetEntityId. Native sender proof shows this field carries the resolved target entity id.")]
        public uint PrimaryTargetId => TargetEntityId;

        public Position Position { get; } = new();

        public void Read(GamePacketReader reader)
        {
            ContextToken   = reader.ReadUInt();
            SelectedEntryId = reader.ReadUInt();
            TargetEntityId = reader.ReadUInt();
            Position.Read(reader);
        }
    }
}