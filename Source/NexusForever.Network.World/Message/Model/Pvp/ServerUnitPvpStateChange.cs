using NexusForever.Game.Static.Pvp;
using NexusForever.Network.Message;

namespace NexusForever.Network.World.Message.Model.Pvp
{
    /// <summary>
    /// Native opcode <c>0x08BD</c> reader <c>ServerUnitPvpStateChange_ReadPayload</c>
    /// (<c>140098160</c>) reads this unit id plus 3-bit PvP state payload.
    /// The client apply path <c>Entity_ApplyUnitPvpFlagsChanged</c> (<c>1403ddc60</c>)
    /// dispatches <c>UnitPvpFlagsChanged</c> after updating the entity state.
    /// </summary>
    [Message(GameMessageOpcode.ServerUnitPvpStateChange)]
    public class ServerUnitPvpStateChange : IWritable
    {
        public uint UnitId { get; set; }
        public PvpState State { get; set; }

        public void Write(GamePacketWriter writer)
        {
            writer.Write(UnitId);
            writer.Write(State, 3u);
        }
    }
}
