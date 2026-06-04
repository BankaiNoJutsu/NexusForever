using NexusForever.Network.Message;
using NexusForever.Network.World.Message.Model.Shared;

namespace NexusForever.Network.World.Message.Model
{
    public class ServerSpellUInt32TripletListRow : ServerUInt32Triplet
    {
    }

    /// <summary>
    /// Counted list of three uint32 fields per row. Client reader FUN_140095da0 at opcode 0x080F.
    /// </summary>
    [Message(GameMessageOpcode.ServerSpellUInt32TripletList)]
    public class ServerSpellUInt32TripletList : ServerUInt32TripletListPayload
    {
    }

    /// <summary>
    /// Opcode 0x0810 shares the 0x080F reader (FUN_140095da0); row semantics remain blocked.
    /// </summary>
    [Message(GameMessageOpcode.ServerSpellUInt32TripletListVariant)]
    public class ServerSpellUInt32TripletListVariant : ServerUInt32TripletListPayload
    {
    }
}
