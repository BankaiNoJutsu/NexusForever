using System;
using NexusForever.Network.Message;

namespace NexusForever.Network.World.Message.Model
{
    [Message(GameMessageOpcode.ClientActivateUnit)]
    public class ClientActivateUnit : IReadable
    {
        public uint ActivateUnitId { get; private set; }

        [Obsolete("Use ActivateUnitId. Native sender proof shows this field carries the activate-unit target id.")]
        public uint UnitId => ActivateUnitId;

        public void Read(GamePacketReader reader)
        {
            ActivateUnitId = reader.ReadUInt();
        }
    }
}
