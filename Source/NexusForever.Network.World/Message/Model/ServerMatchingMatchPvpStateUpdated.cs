using NexusForever.Network.Message;
using NexusForever.Network.World.Message.Model.Shared;

namespace NexusForever.Network.World.Message.Model
{
    /// <summary>
    /// Native registration in <c>Network_RegisterServerOpcode_0351</c> binds opcode <c>0x05EA</c>
    /// directly to <c>MatchingPvpStateInfo_ReadPayload</c> (<c>140099130</c>), so the wire shape
    /// is exactly the inherited <see cref="StateInfo"/> payload.
    /// </summary>
    [Message(GameMessageOpcode.ServerMatchingMatchPvpStateUpdated)]
    public class ServerMatchingMatchPvpStateUpdated : StateInfo
    {
    }
}
