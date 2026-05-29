using NexusForever.Game.Static.Matching;
using NexusForever.Network.Message;
using NexusForever.Network.World.Message.Model.Shared;

namespace NexusForever.Network.World.Message.Model
{
    /// <summary>
    /// Opcode 0x05F3. Native client writer reuses <c>ClientMatchingQueue_WritePayload</c> @ <c>140098a70</c>,
    /// so the party variant matches <see cref="ClientMatchingQueue"/>: shared <see cref="MatchingMap"/>,
    /// one 32-bit <see cref="Role"/> bitfield, and one uint32 <see cref="PrimeLevel"/>.
    /// </summary>
    [Message(GameMessageOpcode.ClientMatchingQueueParty)]
    public class ClientMatchingQueueParty : IReadable
    {
        public MatchingMap MapData { get; private set; }
        public Role Roles { get; private set; }
        public uint PrimeLevel { get; private set; }

        public void Read(GamePacketReader reader)
        {
            MapData = new MatchingMap();
            MapData.Read(reader);

            Roles      = reader.ReadEnum<Role>(32u);
            PrimeLevel = reader.ReadUInt();
        }
    }
}
