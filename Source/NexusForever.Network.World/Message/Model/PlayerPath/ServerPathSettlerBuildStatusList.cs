using NexusForever.Network.Message;

namespace NexusForever.Network.World.Message.Model.PlayerPath
{
    [Message(GameMessageOpcode.ServerPathSettlerBuildStatusList)]
    public class ServerPathSettlerBuildStatusList : IWritable
    {
        public ushort PathSettlerHubId { get; set; }
        public List<SettlerImprovementGroupStatus> ImprovementGroupStatuses { get; set; } = [];

        public void Write(GamePacketWriter writer)
        {
            // Client reader ServerPathSettlerBuildStatusList_ReadPayload (WildStar64.exe
            // 14007a800) reads a 14-bit PathSettlerHubId, a uint32 row count, then
            // counted status rows.
            writer.Write(PathSettlerHubId, 14);
            writer.Write(ImprovementGroupStatuses.Count);
            ImprovementGroupStatuses.ForEach(status => status.Write(writer));
        }
    }
}
