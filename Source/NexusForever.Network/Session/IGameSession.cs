using NexusForever.Network.Message;
using NexusForever.Network.Packet;

namespace NexusForever.Network.Session
{
    public interface IGameSession : INetworkSession
    {
        /// <summary>
        /// Determines if queued incoming packets can be processed during a world update.
        /// </summary>
        bool CanProcessIncomingPackets { get; set; }

        /// <summary>
        /// Determines if queued outgoing packets can be processed during a world update.
        /// </summary>
        bool CanProcessOutgoingPackets { get; set; }

        /// <summary>
        /// Enqueue <see cref="IWritable"/> to be sent to the client.
        /// </summary>
        void EnqueueMessage(IWritable message);

        /// <summary>
        /// Enqueue <see cref="IWritable"/> to be sent encrypted to the client.
        /// </summary>
        void EnqueueMessageEncrypted(IWritable message);

        void EnqueueMessageEncrypted(uint opcode, string hex);

        /// <summary>
        /// Serialize <see cref="IWritable"/> including the opcode, the result can be shared between sessions.
        /// </summary>
        /// <remarks>
        /// Returns null if the message can't be serialized for sharing, callers should fall back to <see cref="EnqueueMessageEncrypted(IWritable)"/>.
        /// </remarks>
        byte[] SerializeMessage(IWritable message) => null;

        /// <summary>
        /// Encrypt and enqueue a message previously serialized with <see cref="SerializeMessage"/>.
        /// </summary>
        void EnqueueSerializedMessageEncrypted(IWritable message, byte[] data) => EnqueueMessageEncrypted(message);

        void HandlePacket(ClientGamePacket packet);

        /// <summary>
        /// Flush all queued packets to the client.
        /// </summary>
        public void FlushPackets();
    }
}