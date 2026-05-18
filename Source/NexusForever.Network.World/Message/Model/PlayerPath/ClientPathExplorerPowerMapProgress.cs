using NexusForever.Network.Message;

namespace NexusForever.Network.World.Message.Model.PlayerPath
{
    [Message(GameMessageOpcode.ClientPathExplorerPowerMapProgress)]
    public class ClientPathExplorerPowerMapProgress : IReadable
    {
        public uint PathExplorerPowerMapId { get; private set; }

        public void Read(GamePacketReader reader)
        {
            PathExplorerPowerMapId = reader.ReadUInt(14);
        }
    }
}
