using NexusForever.Network.Message;

namespace NexusForever.Network.World.Message.Model
{
    [Message(GameMessageOpcode.ClientSpellClientSideInteraction)]
    public class ClientSpellClientSideInteraction : IReadable
    {
        public uint SpellCastId { get; private set; }
        public byte Action { get; private set; }
        public uint Spell4BaseIdPlusOne { get; private set; }

        public void Read(GamePacketReader reader)
        {
            SpellCastId = reader.ReadUInt();
            Action = reader.ReadByte(3u);
            Spell4BaseIdPlusOne = reader.ReadUInt();
        }
    }
}
