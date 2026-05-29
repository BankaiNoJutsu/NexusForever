using NexusForever.Game.Static.Matching;
using NexusForever.Network.Message;

namespace NexusForever.Network.World.Message.Model
{
    [Message(GameMessageOpcode.ClientMatchingRoleCheckResponse)]
    public class ClientMatchingRoleCheckResponse : IReadable
    {
        /// <summary>
        /// Native client registration binds opcode 0x05B2 to 140098e10, which serialises
        /// one 5-bit MatchType, one 32-bit Roles field, and one trailing response bit
        /// inside a registered 0x0C-byte object.
        /// </summary>
        public Game.Static.Matching.MatchType Type { get; private set; } // Uses the same 5-bit MatchType shape also sent as ReadyMatchType in 0x05E3
        public Role Roles { get; private set; }
        public bool Response { get; private set; }

        public void Read(GamePacketReader reader)
        {
            Type = reader.ReadEnum<Game.Static.Matching.MatchType>(5u);
            Roles = reader.ReadEnum<Role>(32u);
            Response = reader.ReadBit();
        }
    }
}
