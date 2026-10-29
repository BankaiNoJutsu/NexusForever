using NexusForever.Game.Static.Entity;
using NexusForever.Network;
using NexusForever.Network.Message;

namespace NexusForever.Network.World.Message.Model
{
    [Message(GameMessageOpcode.ClientResurrectAccept)]
    public class ClientResurrectAccept : IReadable
    {
        public uint UnitId { get; private set; }
        public byte Unknown1 { get; private set; } // Seven wire bits; diagnostic-only until their meaning is verified.
        public ResurrectionType RezType { get; private set; }

        public void Read(GamePacketReader reader)
        {
            UnitId   = reader.ReadUInt();
            Unknown1 = reader.ReadByte(7u);
            RezType = reader.ReadEnum<ResurrectionType>(32u);

            if (RezType is not ResurrectionType.WakeHere
                and not ResurrectionType.WakeHereServiceToken
                and not ResurrectionType.SpellCasterLocation
                and not ResurrectionType.Holocrypt)
            {
                throw new InvalidPacketValueException($"Unsupported resurrection accept type {RezType}.");
            }
        }
    }
}
