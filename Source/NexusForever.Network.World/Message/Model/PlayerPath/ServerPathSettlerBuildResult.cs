using NexusForever.Network.Message;

namespace NexusForever.Network.World.Message.Model.PlayerPath
{
    [Message(GameMessageOpcode.ServerPathSettlerBuildResult)]
    public class ServerPathSettlerBuildResult : IWritable
    {
        // Client reader ServerPathSettlerBuildResult_ReadPayload (WildStar64.exe 14007aa70)
        // reads result, PathSettlerImprovementId, and PathSettlerImprovementGroupId.
        // Exact non-success enum values remain unmapped.
        public uint Result { get; set; }

        public uint PathSettlerImprovementId { get; set; }
        public uint PathSettlerImprovementGroupId { get; set; }

        public void Write(GamePacketWriter writer)
        {
            writer.Write(Result);
            writer.Write(PathSettlerImprovementId, 15);
            writer.Write(PathSettlerImprovementGroupId, 14);
        }
    }
}
