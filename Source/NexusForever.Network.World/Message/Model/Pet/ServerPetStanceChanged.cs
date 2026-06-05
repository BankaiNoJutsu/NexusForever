using NexusForever.Game.Static.Pet;
using NexusForever.Network.Message;

namespace NexusForever.Network.World.Message.Model.Pet
{
    /// <summary>
    /// Native reader <c>ServerUInt32UInt5_ReadPayload</c> @ <c>14008ce80</c> reads one uint32
    /// pet unit id followed by one 5-bit stance field. Native apply path
    /// <c>Pet_ApplyStanceChangedPayload</c> @ <c>1403c0a80</c> updates the cached pet stance
    /// and dispatches <c>PetStanceChanged</c>. The same reader is shared with unresolved
    /// <c>Client0x0928</c>; server producer timing remains unmapped.
    /// </summary>
    [Message(GameMessageOpcode.ServerPetStanceChanged)]
    public class ServerPetStanceChanged : IWritable
    {
        public uint PetUnitId { get; set; } // 0 means all engineer pets
        public PetStance Stance { get; set; }

        public void Write(GamePacketWriter writer)
        {
            writer.Write(PetUnitId);
            writer.Write(Stance, 5u);
        }
    }
}
