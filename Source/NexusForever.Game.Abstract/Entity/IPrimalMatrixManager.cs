using NexusForever.Database.Character;

namespace NexusForever.Game.Abstract.Entity
{
    /// <summary>
    /// The character's saved Primal Matrix allocations: the per-node counts the client receives in the snapshot packet and prerequisite type 292 reads.
    /// </summary>
    public interface IPrimalMatrixManager
    {
        /// <summary>
        /// The saved allocation count of the supplied node, 0 when it holds none.
        /// </summary>
        byte GetAllocations(uint nodeId);

        /// <summary>
        /// Apply a save request: nodes with the number of allocations added to each. Validates the request, spends the essences and grants the rewards.
        /// Returns false, changing nothing, when the request is not allowed or cannot be afforded.
        /// </summary>
        bool TryAllocate(IReadOnlyList<(uint NodeId, byte Increment)> request);

        /// <summary>
        /// Send the saved allocations to the client. Nothing is sent for a character without any allocation.
        /// </summary>
        void SendSnapshot(bool force = false);

        /// <summary>
        /// Stage new and changed allocation rows for the next save.
        /// </summary>
        void Save(CharacterContext context);
    }
}
