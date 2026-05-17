using NexusForever.Game.Static.Setting;
using NexusForever.Network.Message;

namespace NexusForever.Network.World.Message.Model
{
    [Message(GameMessageOpcode.ClientGroupSetInstanceDifficulty)]
    public class ClientGroupSetInstanceDifficulty : IReadable
    {
        public ulong GroupId { get; private set; }
        public WorldDifficulty Difficulty { get; private set; }

        public void Read(GamePacketReader reader)
        {
            GroupId = reader.ReadULong();
            Difficulty = reader.ReadEnum<WorldDifficulty>(2u);
        }
    }
}
