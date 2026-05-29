using NexusForever.Network.Message;

namespace NexusForever.Network.World.Message.Model
{
    /// <summary>
    /// Native registration in <c>Network_RegisterServerOpcode_0351</c> binds opcode <c>0x05CF</c>
    /// to local reader slot <c>LAB_140099110</c> with registered size <c>4</c>. Direct inspection
    /// shows that thunk reads one 4-byte field through shared helper <c>14006c090</c>. The same slot
    /// also backs <c>0x085D ServerTradeskillSigilResult</c>, whose current evidence pins a shared raw
    /// <c>uint32</c> surface, so this matching packet remains an unresolved uint32 placeholder.
    /// </summary>
    [Message(GameMessageOpcode.ServerMatching0x05CF)]
    public class ServerMatching0x05CF : ServerUnresolvedUIntPayload
    {
        public ServerMatching0x05CF(uint value = 0u) : base(32u, value) { }
    }
}