using NexusForever.Database.Character;

namespace NexusForever.Game.Abstract.Entity
{
    /// <summary>
    /// The character's unlocked inlaid augmentation ids: the list the client keeps from the player-create packet (and replaces with packet 0x019C) and that prerequisite type 231 tests.
    /// </summary>
    public interface IInlaidAugmentationManager
    {
        /// <summary>
        /// The most ids the client accepts in the list.
        /// </summary>
        const int MaxIds = 54;

        /// <summary>
        /// The unlocked ids in ascending order.
        /// </summary>
        IReadOnlyList<ushort> Ids { get; }

        bool IsUnlocked(ushort augmentationId);

        /// <summary>
        /// Unlock the supplied id. Returns false when it was already unlocked, is zero or the list is full.
        /// </summary>
        bool Unlock(ushort augmentationId);

        /// <summary>
        /// Stage the ids not yet persisted for the next save.
        /// </summary>
        void Save(CharacterContext context);
    }
}
