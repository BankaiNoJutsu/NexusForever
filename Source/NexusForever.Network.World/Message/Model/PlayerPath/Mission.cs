using NexusForever.Game.Static.PlayerPath;
using NexusForever.Network.Message;

namespace NexusForever.Network.World.Message.Model.PlayerPath
{
    public class Mission : IWritable
    {
        public uint PathMissionId { get; set; }
        public bool Completed { get; set; }
        // Client PathMission.GetNumCompleted reads this payload for Soldier_Assassinate,
        // Settler_Hub, and other count-style mission types.
        public uint ProgressCount { get; set; }
        // Some mission types use this alternate payload for current progress; exact
        // producer semantics are still mapped per mission type.
        public uint ProgressData { get; set; }
        public PathMissionState State { get; set; }
        public uint GiverUnitId { get; set; }

        public void Write(GamePacketWriter writer)
        {
            writer.Write(PathMissionId, 15);
            writer.Write(Completed);
            writer.Write(ProgressCount);
            writer.Write(ProgressData);
            writer.Write(State, 3);
            writer.Write(GiverUnitId);
        }
    }
}
