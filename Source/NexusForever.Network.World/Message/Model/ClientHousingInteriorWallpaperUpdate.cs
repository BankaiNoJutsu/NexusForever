using NexusForever.Network.Message;
using NexusForever.Network.World.Message.Model.Shared;

namespace NexusForever.Network.World.Message.Model
{
    [Message(GameMessageOpcode.ClientHousingInteriorWallpaperUpdate)]
    public class ClientHousingInteriorWallpaperUpdate : IReadable
    {
        public List<uint> SlotFlags { get; } = new();
        public List<DecorInfo> DecorUpdates { get; } = new();

        public void Read(GamePacketReader reader)
        {
            for (uint i = 0u; i < 6u; i++)
                SlotFlags.Add(reader.ReadUInt());

            for (uint i = 0u; i < 6u; i++)
            {
                var decor = new DecorInfo();
                decor.Read(reader);
                DecorUpdates.Add(decor);
            }
        }
    }
}
