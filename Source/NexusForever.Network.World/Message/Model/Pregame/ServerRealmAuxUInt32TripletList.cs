using NexusForever.Network.Message;
using NexusForever.Network.World.Message.Model;
using NexusForever.Network.World.Message.Model.Shared;

namespace NexusForever.Network.World.Message.Model.Pregame
{
    /// <summary>
    /// Counted uint32 triplet rows between realm info and mail (0x05A1). Reader <c>FUN_140080b00</c> @ <c>140080b00</c>.
    /// Wire matches <see cref="ServerUInt32Triplet"/> / opcode <c>0x080F</c>.
    /// </summary>
    [Message(GameMessageOpcode.ServerRealmAuxUInt32TripletList)]
    public class ServerRealmAuxUInt32TripletList : ServerUInt32TripletListPayload
    {
    }
}
