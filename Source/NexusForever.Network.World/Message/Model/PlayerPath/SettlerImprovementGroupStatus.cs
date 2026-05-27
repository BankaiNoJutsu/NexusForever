using NexusForever.Network.Message;

namespace NexusForever.Network.World.Message.Model.PlayerPath
{
    public class SettlerImprovementGroupStatus : IWritable
    {
        public ushort PathSettlerImprovementGroupId { get; set; }
        public int Tier { get; set; }
        public uint RemainingTimeMs { get; set; }
        public uint BundleCount { get; set; }

        public void Write(GamePacketWriter writer)
        {
            // Shared status row reader (WildStar64.exe 14007a730): group id u14,
            // tier i32, remaining time u32, bundle count u32.
            writer.Write(PathSettlerImprovementGroupId, 14);
            writer.Write(Tier);
            writer.Write(RemainingTimeMs);
            writer.Write(BundleCount);
        }
    }
}
