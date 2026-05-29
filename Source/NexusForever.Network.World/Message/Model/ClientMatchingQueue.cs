using NexusForever.Game.Static.Matching;
using NexusForever.Network.Message;
using NexusForever.Network.World.Message.Model.Shared;

namespace NexusForever.Network.World.Message.Model
{
    /// <summary>
    /// Opcode 0x05EF. Wire layout from <c>ClientMatchingQueue_WritePayload</c> @ <c>140098a70</c>:
    /// shared <see cref="MatchingMap"/> payload, one 32-bit <see cref="Role"/> bitfield, and one uint32 <see cref="PrimeLevel"/>.
    /// </summary>
    [Message(GameMessageOpcode.ClientMatchingQueue)]
    public class ClientMatchingQueue : IReadable
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
