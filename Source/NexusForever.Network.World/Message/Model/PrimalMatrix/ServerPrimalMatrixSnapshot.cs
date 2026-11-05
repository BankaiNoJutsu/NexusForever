using NexusForever.Network.Message;

namespace NexusForever.Network.World.Message.Model.PrimalMatrix
{
    /// <summary>
    /// Absolute saved-node snapshot, reader140090090/consumer1404d6af0.
    /// Omitted nodes are cleared by the client. MatrixAmpPower is the Matrix
    /// contribution, separate from earned bonus power and total AMP power.
    /// No producer cadence or save acknowledgement is inferred from this model.
    /// </summary>
    [Message(GameMessageOpcode.ServerPrimalMatrixSnapshot)]
    public class ServerPrimalMatrixSnapshot : IWritable
    {
        public List<uint> NodeIds { get; } = [];
        public byte[] SavedAllocationCounts { get; set; } = [];
        public uint MatrixAmpPower { get; set; }

        public void Write(GamePacketWriter writer) => WritePayload(writer, NodeIds, SavedAllocationCounts, MatrixAmpPower);

        internal static void WritePayload(GamePacketWriter writer, IEnumerable<uint> nodeIds, byte[] savedCounts, uint matrixAmpPower)
        {
            uint[] ids = nodeIds.ToArray();
            byte[] counts = savedCounts ?? [];
            if (counts.Length != ids.Length)
                throw new InvalidOperationException("Primal Matrix saved counts must match the node ID count.");

            writer.Write((uint)ids.Length);
            writer.WriteRetailCompositeUInt32Array(ids);
            writer.WriteRetailCompositeByteSpan(counts);
            writer.Write(matrixAmpPower);
        }
    }
}
