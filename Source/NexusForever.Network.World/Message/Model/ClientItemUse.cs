using System;
using NexusForever.Network.Message;
using NexusForever.Network.World.Entity;
using NexusForever.Network.World.Message.Model.Shared;

namespace NexusForever.Network.World.Message.Model
{
    [Message(GameMessageOpcode.ClientItemUse)]
    public class ClientItemUse : IReadable
    {
        public uint ContextToken { get; private set; }

        [Obsolete("Use ContextToken. Native sender proof shows this field carries the generated cast-context token.")]
        public uint CastingId => ContextToken;

        public ItemLocation Location { get; } = new();
        public uint TargetUnitId { get; private set; }
        public ItemLocation TargetLocation { get; } = new();
        public Position Position { get; } = new Position();

        public void Read(GamePacketReader reader)
        {
            ContextToken = reader.ReadUInt();
            Location.Read(reader);
            TargetUnitId = reader.ReadUInt();
            TargetLocation.Read(reader);
            Position.Read(reader);
        }
    }
}
