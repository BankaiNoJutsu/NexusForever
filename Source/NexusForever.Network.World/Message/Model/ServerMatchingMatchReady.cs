using NexusForever.Network.Message;

namespace NexusForever.Network.World.Message.Model
{
    /// <summary>
    /// Native reader: <c>ServerMatchingReadyCounts_ReadPayload</c> (<c>140099700</c>).
    /// Reads opcode <c>0x05CA</c> as a 5-bit <see cref="Game.Static.Matching.MatchType"/>
    /// followed by two uint32 count fields.
    /// </summary>
    [Message(GameMessageOpcode.ServerMatchingMatchReady)]

    // Specifically sent for a match that has not started yet
    public class ServerMatchingMatchReady : IWritable
    {
        public Game.Static.Matching.MatchType MatchType { get; set; }
        public uint PendingAllies { get; set; }
        public uint PendingEnemies { get; set; }

        public void Write(GamePacketWriter writer)
        {
            writer.Write(MatchType, 5u);
            writer.Write(PendingAllies);
            writer.Write(PendingEnemies);
        }
    }
}
