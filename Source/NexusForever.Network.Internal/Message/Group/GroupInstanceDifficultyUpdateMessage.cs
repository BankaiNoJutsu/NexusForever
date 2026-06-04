using NexusForever.Game.Static.Setting;
using NexusForever.Network.Internal.Message.Shared;

namespace NexusForever.Network.Internal.Message.Group
{
    public class GroupInstanceDifficultyUpdateMessage
    {
        public ulong GroupId { get; set; }
        public Identity Identity { get; set; }
        public WorldDifficulty Difficulty { get; set; }
    }
}
