namespace NexusForever.Game.Abstract.Entity
{
    public interface IBuybackInfo : IEnumerable<IBuybackItem>
    {
        /// <summary>
        /// Return stored <see cref="IBuybackItem"/> with supplied unique id.
        /// </summary>
        IBuybackItem GetItem(uint uniqueId);

        /// <summary>
        /// Store an <see cref="IBuybackItem"/> with its assigned request identity.
        /// </summary>
        void AddItem(IBuybackItem item);

        /// <summary>
        /// Remove stored <see cref="IBuybackItem"/> with supplied unique id.
        /// </summary>
        void RemoveItem(uint uniqueId);
    }
}
