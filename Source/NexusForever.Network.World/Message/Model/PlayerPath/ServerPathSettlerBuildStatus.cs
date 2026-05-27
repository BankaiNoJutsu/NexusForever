using NexusForever.Network.Message;

namespace NexusForever.Network.World.Message.Model.PlayerPath
{
    [Message(GameMessageOpcode.ServerPathSettlerBuildStatus)]
    public class ServerPathSettlerBuildStatus : IWritable
    {
        public ushort PathSettlerHubId { get; set; }
        public SettlerImprovementGroupStatus Status { get; set; }

        public void Write(GamePacketWriter writer)
        {
            // Client reader ServerPathSettlerBuildStatus_ReadPayload (WildStar64.exe
            // 14007a7b0) reads a 14-bit PathSettlerHubId followed by one status row.
            writer.Write(PathSettlerHubId, 14);
            Status.Write(writer);
        }
    }
}
