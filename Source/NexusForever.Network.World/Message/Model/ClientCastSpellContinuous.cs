using System;
using NexusForever.Network.Message;

namespace NexusForever.Network.World.Message.Model
{
    [Message(GameMessageOpcode.ClientCastSpellContinuous)]
    public class ClientCastSpellContinuous : IReadable
    {
        public ushort BagIndex { get; private set; }
        public uint PrimaryTargetId { get; private set; }

        [Obsolete("Use PrimaryTargetId.")]
        public uint Guid => PrimaryTargetId;
        public bool ButtonPressed { get; private set; }

        public void Read(GamePacketReader reader)
        {
            BagIndex        = reader.ReadUShort();
            PrimaryTargetId = reader.ReadUInt();
            ButtonPressed   = reader.ReadBit();
        }
    }
}
