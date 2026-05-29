using NexusForever.Network.Message;

namespace NexusForever.Network.World.Message.Model.Shared
{
    /// <summary>
    /// Shared 15-bit world id + 16-bit achieved-prime-level row.
    /// Native reader: <c>MatchingPrimeLevelInfo_ReadPayload</c> (<c>1400ad150</c>).
    /// Reused by matching queued-player rows and the trailing counted array in <see cref="GroupCharacter"/>.
    /// </summary>
    public class PrimeLevelInfo : IWritable
    {
        public ushort WorldId { get; set; }
        public ushort PrimeLevelAchieved { get; set; }

        public void Write(GamePacketWriter writer)
        {
            writer.Write(WorldId, 15u);
            writer.Write(PrimeLevelAchieved);
        }
    }
}
