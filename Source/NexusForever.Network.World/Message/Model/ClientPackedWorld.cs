using NexusForever.Network.Message;

using NexusForever.Network;

namespace NexusForever.Network.World.Message.Model
{
    [Message(GameMessageOpcode.ClientPackedWorld)]
    public class ClientPackedWorld : IReadable
    {
        private const uint LengthPrefixSize = 4u;
        private const byte PackedWorldEnvelopeType11 = 11;
        private const byte PackedWorldEnvelopeType19 = 19;

        // The native client sends envelope type 11 or 19 depending on the packed-world sender.
        public byte Unknown0 { get; private set; }
        public bool IsKnownEnvelopeType => Unknown0 is PackedWorldEnvelopeType11 or PackedWorldEnvelopeType19;
        public byte[] Data { get; private set; }

        public void Read(GamePacketReader reader)
        {
            Unknown0 = reader.ReadByte(5u);
            reader.ResetBits();

            uint length = reader.ReadUInt();
            if (length < LengthPrefixSize)
                throw new InvalidPacketValueException($"Packed world payload length {length} is smaller than its prefix.");

            Data = reader.ReadBytes(length - LengthPrefixSize);
        }
    }
}
