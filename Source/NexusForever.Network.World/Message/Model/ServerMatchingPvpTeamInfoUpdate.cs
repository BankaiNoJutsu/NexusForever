using NexusForever.Network.Message;

namespace NexusForever.Network.World.Message.Model
{
    /// <summary>
    /// Native reader: <c>ServerMatchingPvpTeamInfoUpdate_ReadPayload</c> (<c>140099290</c>).
    /// Reads two wide team-name strings followed by two uint32 rating fields.
    /// </summary>
    [Message(GameMessageOpcode.ServerMatchingPvpTeamInfoUpdate)]
    public class ServerMatchingPvpTeamInfoUpdate : IWritable
    {
        public string Team1Name { get; set; }
        public string Team2Name { get; set; }
        public uint Team1Rating { get; set; }
        public uint Team2Rating { get; set; }

        public void Write(GamePacketWriter writer)
        {
            writer.WriteStringWide(Team1Name);
            writer.WriteStringWide(Team2Name);
            writer.Write(Team1Rating);
            writer.Write(Team2Rating);
        }
    }
}
