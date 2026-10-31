using NexusForever.Network.Message;

namespace NexusForever.Network.World.Message.Model.Reward
{
    // Clears all existing RewardTrack data then loads it from this message
    // Triggers RewardTracksLoaded (consumer 1404d1640).
    [Message(GameMessageOpcode.ServerRewardTracksReload)]
    public class ServerRewardTracksReload : IWritable
    {
        public List<RewardTrack> RewardsTracks { get; set; } = [];

        public void Write(GamePacketWriter writer)
        {
            if (RewardsTracks == null)
                throw new InvalidOperationException("Reward track reload rows are missing.");
            foreach (RewardTrack track in RewardsTracks)
                RewardTrackWireValidation.Validate(track);
            writer.Write(RewardsTracks.Count);
            RewardsTracks.ForEach(rewardTrack => rewardTrack.Write(writer));
        }
    }
}
