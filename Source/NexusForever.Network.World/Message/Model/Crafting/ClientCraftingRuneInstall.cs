using NexusForever.Network;
using NexusForever.Network.Message;

namespace NexusForever.Network.World.Message.Model.Crafting
{
    [Message(GameMessageOpcode.ClientCraftingRuneInstall)]
    public class ClientCraftingRuneInstall : IReadable
    {
        public ulong ItemGuid { get; private set; }
        public uint[] RuneSlotItem2Id { get; private set; } // Item2Id of rune, arranged in order of slot

        public void Read(GamePacketReader reader)
        {
            ItemGuid = reader.ReadULong();
            uint count = reader.ReadUInt();

            if (count > reader.BytesRemaining / 4u)
                throw new InvalidPacketValueException();

            RuneSlotItem2Id = new uint[count];
            for (int i = 0; i < count; i++)
            {
                RuneSlotItem2Id[i] = reader.ReadUInt();
            }
        }
    }
}
