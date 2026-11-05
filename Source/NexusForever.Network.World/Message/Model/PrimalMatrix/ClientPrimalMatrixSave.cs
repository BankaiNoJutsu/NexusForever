using NexusForever.Network.Message;

namespace NexusForever.Network.World.Message.Model.PrimalMatrix
{
    /// <summary>
    /// SavePrimalMatrix confirmation, native sender140523a60/writer14008be20.
    /// Bytes are requested allocation increments, not absolute saved counts.
    /// Decoding establishes no purchase validation or settlement policy.
    /// </summary>
    [Message(GameMessageOpcode.ClientPrimalMatrixSave)]
    public class ClientPrimalMatrixSave : IReadable
    {
        public uint[] NodeIds { get; private set; } = [];
        public byte[] AllocationIncrements { get; private set; } = [];

        public void Read(GamePacketReader reader)
        {
            uint count = reader.ReadUInt();
            // Both arrays are byte-aligned after count32. Bound the complete
            // request before allocating or multiplying its untrusted count.
            if (count > reader.BytesRemaining / 5u)
                throw new EndOfStreamException("Primal Matrix request is missing counted IDs or increments.");

            uint[] nodes = reader.ReadRetailCompositeUInt32Array((int)count);
            byte[] increments = reader.ReadRetailCompositeByteSpan((int)count);
            NodeIds = nodes;
            AllocationIncrements = increments;
        }
    }
}
