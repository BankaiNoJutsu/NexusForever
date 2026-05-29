using NexusForever.Network.Message;

namespace NexusForever.Network.World.Message.Model
{
    /// <summary>
    /// Native reader: <c>ServerMatchingReadyCounts_ReadPayload</c> (<c>140099700</c>).
    /// Reads opcode <c>0x05C4</c> as a 5-bit <see cref="Game.Static.Matching.MatchType"/>
    /// followed by two uint32 count fields.
    /// </summary>
    [Message(GameMessageOpcode.ServerMatchingMatchInProgressReady)]

    // If player is in a match, fires MatchingGamePendingUpdate event for ally count update
    // If player is not in a match, fires MatchingGameReady event for a match that is not started
    public class ServerMatchingMatchInProgressReady : IWritable
    {
        public Game.Static.Matching.MatchType MatchType { get; set; }
        public uint PendingAllies { get; set; }
        public uint CurrentAllies { get; set; }

        public void Write(GamePacketWriter writer)
        {
            writer.Write(MatchType, 5u);
            writer.Write(PendingAllies);
            writer.Write(CurrentAllies);
        }
    }
}
