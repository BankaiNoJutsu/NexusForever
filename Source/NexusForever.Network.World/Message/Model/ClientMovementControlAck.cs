using NexusForever.Network.Message;

namespace NexusForever.Network.World.Message.Model
{
    [Message(GameMessageOpcode.ClientMovementControlAck)]
    public class ClientMovementControlAck : IReadable
    {
        /// <summary>
        /// Opcode 0x0635. Native client registration in <c>ClientWorldOpcodeRegister_MovementSpline</c>
        /// (<c>1400a8190</c>) binds this packet to shared
        /// <c>ClientTradeskillResetTalents_WritePayload</c> (<c>14007d010</c>), so the proven
        /// wire surface remains one raw uint32 movement-control ticket. Native sender
        /// <c>TargetSelection_SendClientMovementControlAck</c> (<c>14057a630</c>) emits the same
        /// ticket during target-selection control apply.
        /// </summary>
        public uint Ticket { get; private set; }

        public void Read(GamePacketReader reader)
        {
            Ticket = reader.ReadUInt();
        }
    }
}
