using NexusForever.Network.Message;

namespace NexusForever.Network.World.Message.Model
{
    [Message(GameMessageOpcode.ClientPlayerMovementSpeedUpdate)]
    public class ClientPlayerMovementSpeedUpdate : IReadable
    {
        /// <summary>
        /// Native sender <c>ClientPlayerMovementSpeedUpdate_SendAndDispatch</c> (<c>1404dafb0</c>)
        /// builds opcode <c>0x063B</c> as one raw <c>uint32</c> payload and dispatches the same
        /// value through the client <c>PlayerMovementSpeedUpdate</c> event.
        /// Float-vs-int semantics remain unresolved, so keep the field structural.
        /// </summary>
        public uint Value { get; private set; }

        public void Read(GamePacketReader reader)
        {
            Value = reader.ReadUInt();
        }
    }
}