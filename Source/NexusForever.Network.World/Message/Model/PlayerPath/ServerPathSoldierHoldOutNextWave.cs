using NexusForever.Network.Message;

namespace NexusForever.Network.World.Message.Model.PlayerPath
{
    [Message(GameMessageOpcode.ServerPathSoldierHoldOutNextWave)]
    public class ServerPathSoldierHoldOutNextWave : IWritable
    {
        // Kept packet-shape only until the server-side Soldier holdout wave
        // scheduler is proven by packet capture or a mapped client consumer.
        public ushort PathSoldierEventId { get; set; }
        public uint WaveIndex { get; set; }
        public bool IsBoss { get; set; }

        public void Write(GamePacketWriter writer)
        {
            writer.Write(PathSoldierEventId, 14);
            writer.Write(WaveIndex);
            writer.Write(IsBoss);
        }
    }
}
