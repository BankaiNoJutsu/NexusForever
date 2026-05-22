using NexusForever.Network.Message;
using NexusForever.Network.World.Message.Model.Shared;

namespace NexusForever.Network.World.Message.Model
{
    [Message(GameMessageOpcode.ClientHousingInteriorWallpaperUpdate)]
    public class ClientHousingInteriorWallpaperUpdate : IReadable
    {
        public const int SlotCount = 6;

        public List<uint> ExistingDecorFlags { get; } = new();
        public List<DecorInfo> DecorUpdates { get; } = new();

        public void Read(GamePacketReader reader)
        {
            for (uint i = 0u; i < SlotCount; i++)
                ExistingDecorFlags.Add(reader.ReadUInt());

            for (uint i = 0u; i < SlotCount; i++)
            {
                var decor = new DecorInfo();
                decor.Read(reader);
                DecorUpdates.Add(decor);
            }
        }
    }
}
