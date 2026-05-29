using NexusForever.Network.Message;

namespace NexusForever.Network.World.Message.Model.Pregame
{
    // Sent when the lua function CharacterScreenLib::RealmTransfer is called.
    // Native dispatch table strings place this beside GetRealmTransferDestinations.
    [Message(GameMessageOpcode.ClientRealmTransfer)]
    public class ClientRealmTransfer : IReadable
    {
        // Native sender 140027d80 pulls this 64-bit value from the selected character table slot.
        public ulong CharacterId { get; private set; }

        public ushort TargetRealmId { get; private set; }

        // Native sender forwards one trailing Lua boolean, but its exact UI semantics remain open.
        public bool TransferFlag { get; private set; }

        public void Read(GamePacketReader reader)
        {
            CharacterId   = reader.ReadULong();
            TargetRealmId = reader.ReadUShort(14u);
            TransferFlag  = reader.ReadBit();
        }
    }
}